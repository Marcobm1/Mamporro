using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // P0-C: parámetros iniciales de prueba aprobados por el autor (12/10/2026). No son cifras
    // definitivas de generación ni de balance.
    public sealed class ProceduralSettings
    {
        public double Size=128,MacroCell=4,LevelHeight=4.5,SpawnRadius=12,Limit=60;
        public int Levels=4;
        // Fracciones acumuladas del área jugable con nivel ≥1, ≥2 y ≥3 (≈35 % elevado).
        public double Elevated=.35,Level2=.15,Level3=.05;
        // Mesetas de 8–40 m de lado: 2–10 macroceldas.
        public int MinPlateauCells=2,MaxPlateauCells=10;
        // Rampas: una desde 200 m², dos desde 800 m². 8 m de ancho (cabe el jefe, 2,4 m de radio:
        // envolvente de 5,3 m más apoyo), 12 m para un nivel (20,6°) y 16 m para dos (29,4°).
        public double RampArea=200,SecondRampArea=800;
        public int RampWidthCells=2,RampLengthCells=3,RampLengthCellsDouble=4;
        // Rampas de 4 m: solo si no cabe ninguna de 8 m (no sirven al jefe).
        // Irregularidad moderada de los bordes: desplazamientos de 1 m en tramos elegidos por ruido.
        public double EdgeJitter=.35;
        public static readonly ProceduralSettings Default=new ProceduralSettings();
    }
    public struct ProceduralRamp
    {
        // Rectángulo de macroceldas (inclusivo), dirección de subida y alturas de los extremos.
        public int X0,Z0,X1,Z1,UpX,UpZ,Region;
        public double Low,High;
        public bool Inside;
        public double Slope(double macroCell)=>Math.Atan((High-Low)/(((UpX!=0?X1-X0:Z1-Z0)+1)*macroCell))*180/Math.PI;
    }
    public sealed class ProceduralRegion
    {
        public int Id,Level,Cells,MinX,MinZ,MaxX,MaxZ;
        public double Area;
        public readonly List<int> Ramps=new List<int>();
    }

    // P0-C2: trazado de niveles sobre macroceldas. Mesetas de bordes rectos, acantilados de una
    // celda de 1 m (escalables con la física P0-A/B), rampas esculpidas en el heightfield y zona
    // de inicio despejada. Determinista por semilla con flujos propios (no toca los flujos U3).
    public sealed class ProceduralLayout
    {
        public readonly string Seed;
        public readonly ProceduralSettings Settings;
        public readonly int N;
        public readonly int[] Level,RampOf,RegionOf;
        public readonly bool[] Eligible;
        public readonly List<ProceduralRamp> Ramps=new List<ProceduralRamp>();
        public readonly List<ProceduralRegion> Regions=new List<ProceduralRegion>();
        public Heightfield Terrain {get;private set;}
        public double Milliseconds;
        public int MissingRamps;
        // Intentos deterministas (subsemillas derivadas) hasta cumplir las reglas del trazado.
        public int Attempt {get;private set;}
        public const int MaxAttempts=8;

        // firstAttempt: primer intento (el rechazo por validación de C4 continúa la serie determinista).
        public ProceduralLayout(string seed,ProceduralSettings settings=null,int firstAttempt=0)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            Seed=seed;Settings=settings??ProceduralSettings.Default;var s=Settings;
            N=(int)Math.Round(s.Size/s.MacroCell);Level=new int[N*N];RampOf=new int[N*N];RegionOf=new int[N*N];Eligible=new bool[N*N];
            for(Attempt=firstAttempt;Attempt<firstAttempt+MaxAttempts;Attempt++){
                for(int m=0;m<N*N;m++){RampOf[m]=-1;Level[m]=0;}
                Ramps.Clear();MissingRamps=0;
                var rng=new Rng(seed).Derive(Attempt==0?"p0c-layout":"p0c-layout-"+Attempt);
                AssignLevels(rng.Derive("niveles"));
                FindRegions();
                PlaceRamps(rng.Derive("rampas"));
                if(MissingRamps==0)break;
            }
            if(Attempt==firstAttempt+MaxAttempts)Attempt--;
            Terrain=BuildTerrain(new Rng(seed).Derive("p0c-layout-bordes-"+Attempt));
            Milliseconds=watch.Elapsed.TotalMilliseconds;
        }
        public double MacroX(int mx)=>-Settings.Size/2+mx*Settings.MacroCell;
        public double MacroZ(int mz)=>-Settings.Size/2+mz*Settings.MacroCell;
        public int MacroAt(double x,double z)
        {
            int mx=(int)Math.Floor((x+Settings.Size/2)/Settings.MacroCell),mz=(int)Math.Floor((z+Settings.Size/2)/Settings.MacroCell);
            return mx<0||mz<0||mx>=N||mz>=N?-1:mz*N+mx;
        }
        public double LevelY(int level)=>level*Settings.LevelHeight;

        // ------------------------------------------------------------ niveles
        void AssignLevels(Rng rng)
        {
            var s=Settings;var noise=new SimplexNoise2D(rng.Next);var values=new double[N*N];var eligibleValues=new List<double>();
            for(int mz=0;mz<N;mz++)for(int mx=0;mx<N;mx++){
                int m=mz*N+mx;double cx=MacroX(mx)+s.MacroCell/2,cz=MacroZ(mz)+s.MacroCell/2;
                // Zona de inicio y franja de 12 m junto al límite: nivel 0 siempre (espacio para la horda,
                // paso del jefe alrededor de las mesetas y sin bolsas entre una meseta y el borde).
                Eligible[m]=WorldMath.Squircle(cx,cz)<=s.Limit-3*s.MacroCell&&JsMath.Hypot(cx,cz)>s.SpawnRadius+s.MacroCell*.75;
                double v=0,amp=1,freq=1/7.0,norm=0;
                for(int o=0;o<3;o++){v+=amp*noise.Noise(mx*freq,mz*freq);norm+=amp;amp*=.5;freq*=2;}
                values[m]=v/norm;if(Eligible[m])eligibleValues.Add(values[m]);
            }
            eligibleValues.Sort();
            double Threshold(double fraction)=>eligibleValues.Count==0?double.MaxValue:eligibleValues[Math.Min(eligibleValues.Count-1,(int)Math.Floor((1-fraction)*eligibleValues.Count))];
            var masks=new bool[4][];double[] fractions={1,s.Elevated,s.Level2,s.Level3};int eligible=eligibleValues.Count;
            for(int k=1;k<s.Levels;k++){
                masks[k]=new bool[N*N];
                // La forma (anchura mínima, anidado) quita celdas: se calibra el umbral de cada nivel,
                // de forma determinista, hasta acercarse a la fracción pedida tras darle forma.
                // Las mesetas de más de 40 m se dividen con un pasillo de dos macroceldas (8 m, cabe el
                // jefe) dentro de la calibración, para que la fracción medida sea la final.
                double input=fractions[k];
                for(int iteration=0;iteration<10;iteration++){
                    double t=Threshold(Math.Min(1,input));
                    for(int m=0;m<N*N;m++)masks[k][m]=Eligible[m]&&values[m]>=t&&(k==1||masks[k-1][m]);
                    Shape(masks[k],k>1?masks[k-1]:null,k==1);
                    var cuts=rng.Derive("cortes-"+k+"-"+iteration);
                    for(int guard=0;guard<64;guard++)if(!SplitLargest(masks[k],cuts))break;
                    Shape(masks[k],k>1?masks[k-1]:null,k==1);
                    int count=0;for(int m=0;m<N*N;m++)if(masks[k][m])count++;
                    double actual=eligible==0?0:(double)count/eligible;
                    if(Math.Abs(actual-fractions[k])<=.01||input>=1)break;
                    input*=actual>0?fractions[k]/actual:1.5;
                }
            }
            for(int m=0;m<N*N;m++){int level=0;for(int k=1;k<s.Levels;k++)if(masks[k][m])level=k;Level[m]=level;}
        }
        // Apertura 2×2 (anchura mínima de 8 m), relleno de huecos de una celda y anidado de niveles.
        // En el nivel 1 también se abre el suelo: los canales de nivel 0 de una sola macrocelda (4 m,
        // donde no cabe el jefe) se rellenan, así el suelo entre mesetas mide al menos 8 m.
        void Shape(bool[] mask,bool[] below,bool openGround=false)
        {
            for(int pass=0;pass<2;pass++){
                var keep=new bool[N*N];
                for(int mz=0;mz+1<N;mz++)for(int mx=0;mx+1<N;mx++){
                    int m=mz*N+mx;if(mask[m]&&mask[m+1]&&mask[m+N]&&mask[m+N+1]){keep[m]=keep[m+1]=keep[m+N]=keep[m+N+1]=true;}
                }
                for(int m=0;m<N*N;m++)mask[m]=keep[m];
                for(int mz=1;mz+1<N;mz++)for(int mx=1;mx+1<N;mx++){
                    int m=mz*N+mx;if(mask[m]||!Eligible[m])continue;
                    int around=(mask[m-1]?1:0)+(mask[m+1]?1:0)+(mask[m-N]?1:0)+(mask[m+N]?1:0);
                    if(around>=3)mask[m]=true;
                }
                if(openGround){
                    var low=new bool[N*N];
                    for(int mz=0;mz+1<N;mz++)for(int mx=0;mx+1<N;mx++){
                        int m=mz*N+mx;if(!mask[m]&&!mask[m+1]&&!mask[m+N]&&!mask[m+N+1]){low[m]=low[m+1]=low[m+N]=low[m+N+1]=true;}
                    }
                    for(int m=0;m<N*N;m++)if(!mask[m]&&!low[m]&&Eligible[m])mask[m]=true;
                    // Esquinas en diagonal (dos mesetas que se tocan por un vértice): el paso entre las
                    // dos celdas bajas mide cero. Se rellenan.
                    for(int mz=0;mz+1<N;mz++)for(int mx=0;mx+1<N;mx++){
                        int a=mz*N+mx,b=a+1,c=a+N,d=a+N+1;
                        if(mask[a]&&mask[d]&&!mask[b]&&!mask[c]){if(Eligible[b])mask[b]=true;if(Eligible[c])mask[c]=true;}
                        else if(mask[b]&&mask[c]&&!mask[a]&&!mask[d]){if(Eligible[a])mask[a]=true;if(Eligible[d])mask[d]=true;}
                    }
                }
                if(below!=null)for(int m=0;m<N*N;m++)mask[m]&=below[m];
            }
        }
        bool SplitLargest(bool[] mask,Rng rng)
        {
            var seen=new bool[N*N];var stack=new Stack<int>();var cells=new List<int>();
            for(int start=0;start<N*N;start++){
                if(!mask[start]||seen[start])continue;
                cells.Clear();stack.Push(start);seen[start]=true;int x0=N,z0=N,x1=-1,z1=-1;
                while(stack.Count>0){int m=stack.Pop();cells.Add(m);int mx=m%N,mz=m/N;x0=Math.Min(x0,mx);x1=Math.Max(x1,mx);z0=Math.Min(z0,mz);z1=Math.Max(z1,mz);
                    foreach(int nb in Neighbors(m))if(mask[nb]&&!seen[nb]){seen[nb]=true;stack.Push(nb);}}
                int w=x1-x0+1,h=z1-z0+1,max=Settings.MaxPlateauCells;
                if(w<=max&&h<=max)continue;
                // Corte perpendicular al lado más largo, en una posición que deja piezas ≥ mínimo.
                bool alongX=w>=h;int length=alongX?w:h,lo=Settings.MinPlateauCells,hi=length-Settings.MinPlateauCells-2;
                int cut=(alongX?x0:z0)+(hi>lo?lo+rng.Int(0,hi-lo):length/2-1);
                foreach(int m in cells){int c=alongX?m%N:m/N;if(c==cut||c==cut+1)mask[m]=false;}
                return true;
            }
            return false;
        }
        IEnumerable<int> Neighbors(int m)
        {
            int mx=m%N,mz=m/N;
            if(mx>0)yield return m-1;if(mx+1<N)yield return m+1;if(mz>0)yield return m-N;if(mz+1<N)yield return m+N;
        }

        // ------------------------------------------------------------ regiones
        void FindRegions()
        {
            Regions.Clear();for(int m=0;m<N*N;m++)RegionOf[m]=-1;var stack=new Stack<int>();
            for(int start=0;start<N*N;start++){
                if(RegionOf[start]>=0)continue;
                var region=new ProceduralRegion{Id=Regions.Count,Level=Level[start],MinX=N,MinZ=N,MaxX=-1,MaxZ=-1};
                stack.Push(start);RegionOf[start]=region.Id;
                while(stack.Count>0){
                    int m=stack.Pop();region.Cells++;int mx=m%N,mz=m/N;
                    region.MinX=Math.Min(region.MinX,mx);region.MaxX=Math.Max(region.MaxX,mx);region.MinZ=Math.Min(region.MinZ,mz);region.MaxZ=Math.Max(region.MaxZ,mz);
                    foreach(int nb in Neighbors(m))if(RegionOf[nb]<0&&Level[nb]==region.Level){RegionOf[nb]=region.Id;stack.Push(nb);}
                }
                region.Area=region.Cells*Settings.MacroCell*Settings.MacroCell;Regions.Add(region);
            }
        }

        // ------------------------------------------------------------ rampas
        void PlaceRamps(Rng rng)
        {
            var s=Settings;
            foreach(var region in Regions){
                if(region.Level==0||region.Area<s.RampArea)continue;
                int wanted=region.Area>=s.SecondRampArea?2:1;
                for(int n=0;n<wanted;n++){
                    // 8 m (sirve al jefe) con 8 m de meseta tras la cima; si no cabe, 4 m (mínimo aprobado).
                    var candidates=Candidates(region,s.RampWidthCells,2);
                    if(candidates.Count==0&&n==0)candidates=Candidates(region,1,1);
                    // La segunda rampa en otro lado de la meseta: alternativa física de acceso.
                    if(region.Ramps.Count>0)candidates.RemoveAll(c=>Distance(c,Ramps[region.Ramps[0]])<4);
                    if(candidates.Count==0){if(n==0)MissingRamps++;break;}
                    var ramp=candidates[rng.Int(0,candidates.Count-1)];ramp.Region=region.Id;
                    region.Ramps.Add(Ramps.Count);
                    for(int z=ramp.Z0;z<=ramp.Z1;z++)for(int x=ramp.X0;x<=ramp.X1;x++)RampOf[z*N+x]=Ramps.Count;
                    Ramps.Add(ramp);
                }
            }
        }
        static double Distance(ProceduralRamp a,ProceduralRamp b)=>Math.Abs((a.X0+a.X1)-(b.X0+b.X1))/2.0+Math.Abs((a.Z0+a.Z1)-(b.Z0+b.Z1))/2.0;
        bool Free(int mx,int mz,int level)
        {
            if(mx<0||mz<0||mx>=N||mz>=N)return false;int m=mz*N+mx;
            // Pie y aproximación dentro del límite jugable: una rampa al borde del mapa no sirve.
            double cx=MacroX(mx)+Settings.MacroCell/2,cz=MacroZ(mz)+Settings.MacroCell/2;
            return Level[m]==level&&RampOf[m]<0&&(Eligible[m]||level==0&&WorldMath.Squircle(cx,cz)<=Settings.Limit-Settings.MacroCell);
        }
        // Posiciones válidas: fuera (sobre el nivel inferior, junto al borde) o dentro (tallada en
        // la propia meseta). Siempre con apoyo al pie y continuidad arriba para entrar y salir andando.
        List<ProceduralRamp> Candidates(ProceduralRegion region,int w,int beyond)
        {
            var list=new List<ProceduralRamp>();var s=Settings;int k=region.Level;
            int[] dx={1,-1,0,0},dz={0,0,1,-1};
            for(int mz=region.MinZ;mz<=region.MaxZ;mz++)for(int mx=region.MinX;mx<=region.MaxX;mx++){
                if(RegionOf[mz*N+mx]!=region.Id)continue;
                for(int d=0;d<4;d++){
                    int px=dz[d]!=0?1:0,pz=dx[d]!=0?1:0; // perpendicular
                    for(int drop=1;drop<=2&&k-drop>=0;drop++){
                        int length=drop==1?s.RampLengthCells:s.RampLengthCellsDouble,low=k-drop;
                        // Fuera: celdas del nivel inferior desde el borde hacia fuera.
                        bool outside=true;
                        for(int a=0;a<w&&outside;a++){
                            int tx=mx+px*a,tz=mz+pz*a;
                            // Meseta tras la cima (hacia dentro) para poder entrar andando con el cuerpo completo.
                            for(int b=0;b<beyond&&outside;b++){int ix=tx-dx[d]*b,iz=tz-dz[d]*b;if(!(Free(ix,iz,k)&&RegionOf[iz*N+ix]==region.Id))outside=false;}
                            for(int l=1;l<=length+1&&outside;l++)if(!Free(tx+dx[d]*l,tz+dz[d]*l,low))outside=false;
                        }
                        if(outside)list.Add(Rect(mx+dx[d],mz+dz[d],dx[d],dz[d],px,pz,length,w,-dx[d],-dz[d],LevelY(low),LevelY(k),false));
                        // Dentro: tallada en la meseta, bajando hacia el borde.
                        bool inside=true;
                        for(int a=0;a<w&&inside;a++){
                            int tx=mx+px*a,tz=mz+pz*a;
                            if(!Free(tx+dx[d],tz+dz[d],low))inside=false;
                            for(int l=0;l<length+beyond&&inside;l++){int ix=tx-dx[d]*l,iz=tz-dz[d]*l;if(!(Free(ix,iz,k)&&RegionOf[iz*N+ix]==region.Id))inside=false;}
                        }
                        if(inside&&region.Cells-length*w>=4)list.Add(Rect(mx,mz,-dx[d],-dz[d],px,pz,length,w,-dx[d],-dz[d],LevelY(low),LevelY(k),true));
                    }
                }
            }
            return list;
        }
        static ProceduralRamp Rect(int sx,int sz,int ax,int az,int px,int pz,int length,int width,int upX,int upZ,double low,double high,bool inside)
        {
            int ex=sx+ax*(length-1)+px*(width-1),ez=sz+az*(length-1)+pz*(width-1);
            return new ProceduralRamp{X0=Math.Min(sx,ex),X1=Math.Max(sx,ex),Z0=Math.Min(sz,ez),Z1=Math.Max(sz,ez),UpX=upX,UpZ=upZ,Low=low,High=high,Inside=inside};
        }

        // ------------------------------------------------------------ heightfield
        Heightfield BuildTerrain(Rng rng)
        {
            var s=Settings;int cells=(int)Math.Round(s.Size),stride=cells+1;var heights=new float[stride*stride];
            var jitter=new SimplexNoise2D(rng.Next);var jitterZ=new SimplexNoise2D(rng.Next);double half=s.Size/2;
            for(int j=0;j<stride;j++)for(int i=0;i<stride;i++){
                double x=-half+i,z=-half+j;
                // Cada vértice toma el nivel de la celda situada a su +x/+z: los saltos ocupan una celda.
                int m=MacroAt(x+.5,z+.5);if(m<0)m=MacroAt(Math.Min(x,half-.5),Math.Min(z,half-.5));
                if(!NearRamp(m)){
                    double ox=Quantize(jitter.Noise(x/12,z/12)),oz=Quantize(jitterZ.Noise(x/12+40,z/12+40));
                    // Solo erosiona: el borde puede retroceder hacia la meseta, nunca invadir el nivel
                    // inferior (los pasos de 8 m conservan ≥ 7 m llanos y el jefe sigue cabiendo).
                    int jm=MacroAt(x+.5+ox,z+.5+oz);if(jm>=0&&!NearRamp(jm)&&Level[jm]<Level[m])m=jm;
                }
                double h=LevelY(Level[m]);
                int r=RampOf[m];if(r>=0)h=RampHeight(Ramps[r],x,z);
                heights[j*stride+i]=(float)h;
            }
            return new Heightfield(s.Size,cells,heights);
        }
        double Quantize(double v)=>v>Settings.EdgeJitter?1:v< -Settings.EdgeJitter?-1:0;
        bool NearRamp(int m)
        {
            int mx=m%N,mz=m/N;
            for(int z=Math.Max(0,mz-1);z<=Math.Min(N-1,mz+1);z++)for(int x=Math.Max(0,mx-1);x<=Math.Min(N-1,mx+1);x++)if(RampOf[z*N+x]>=0)return true;
            return false;
        }
        double RampHeight(ProceduralRamp ramp,double x,double z)
        {
            double a,b,u;
            if(ramp.UpX!=0){a=MacroX(ramp.X0);b=MacroX(ramp.X1+1);u=x;}else{a=MacroZ(ramp.Z0);b=MacroZ(ramp.Z1+1);u=z;}
            double t=(ramp.UpX+ramp.UpZ)>0?(u-a)/(b-a):(b-u)/(b-a);
            return ramp.Low+(ramp.High-ramp.Low)*WorldMath.Clamp01(t);
        }
    }
}
