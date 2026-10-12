using System;

namespace Mamporro.Core
{
    public enum VerticalSurfaceKind { Structure, Cliff, Foliage, Interactable, Boundary, Enemy }

    // P0: datos de colisión QA independientes del render. No modifica WorldCollision U3.
    public sealed class VerticalSolid
    {
        public readonly int Id;
        public readonly VerticalSurfaceKind Kind;
        public readonly Vec3 Min,Max;
        // Contrato por exclusión: los sólidos del mundo ofrecen paredes por defecto.
        public static bool IsWorldWall(VerticalSurfaceKind kind)=>kind!=VerticalSurfaceKind.Foliage
            &&kind!=VerticalSurfaceKind.Interactable&&kind!=VerticalSurfaceKind.Boundary&&kind!=VerticalSurfaceKind.Enemy;
        public bool Climbable=>IsWorldWall(Kind);
        public VerticalSolid(int id,Vec3 min,Vec3 max):this(id,VerticalSurfaceKind.Structure,min,max){}
        public VerticalSolid(int id,VerticalSurfaceKind kind,Vec3 min,Vec3 max)
        {
            if(!VerticalQueries.Finite(min)||!VerticalQueries.Finite(max)||min.X>=max.X||min.Y>=max.Y||min.Z>=max.Z)
                throw new ArgumentException("Volumen vertical inválido");
            Id=id;Kind=kind;Min=min;Max=max;
        }
    }
    public struct VerticalHit
    {
        public int Id;
        public VerticalSurfaceKind Kind;
        public Vec3 Normal;
        public double Fraction;
        public bool Terrain;
        public Vec3 Point;
        public double TopY;
    }

    public sealed class VerticalQueries
    {
        const double Epsilon=1e-9;
        readonly VerticalSolid[] solids;
        readonly TerrainWallGeometry terrain;
        public readonly double WallMinSlopeDegrees;
        double WallCos=>Math.Cos(WallMinSlopeDegrees*WorldMath.Deg2Rad);
        public bool IsWallNormal(Vec3 normal)
        {
            double length=Math.Sqrt(normal.X*normal.X+normal.Y*normal.Y+normal.Z*normal.Z);
            return Finite(normal)&&length>1e-9&&normal.Y>=0&&normal.Y/length<WallCos-1e-10;
        }
        public int Count=>solids.Length;
        public VerticalSolid Solid(int i)=>solids[i];
        public VerticalQueries(params VerticalSolid[] input):this(null,Tuning.PlayerMaxSlopeDeg,input){}
        public VerticalQueries(Heightfield heightfield,double wallMinSlopeDegrees,params VerticalSolid[] input)
        {
            if(!Valid(wallMinSlopeDegrees)||wallMinSlopeDegrees<=0||wallMinSlopeDegrees>=90)throw new ArgumentException("Umbral de pared inválido");
            WallMinSlopeDegrees=wallMinSlopeDegrees;terrain=heightfield==null?null:new TerrainWallGeometry(heightfield);
            if(input==null)throw new ArgumentNullException(nameof(input));
            solids=(VerticalSolid[])input.Clone();
            var ids=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<solids.Length;i++){
                if(solids[i]==null)throw new ArgumentException("Superficie ausente");
                if(!ids.Add(solids[i].Id))throw new ArgumentException("ID de superficie duplicado");
            }
            BuildGrid();
        }

        // ------------------------------------------------------------ rejilla espacial (P0-C1)
        // Celdas de 4 m con los índices de los sólidos que tocan. Cada consulta reúne solo los
        // candidatos de su zona y los recorre en orden de índice, como la versión lineal: mismos
        // resultados y desempates. UseSpatialGrid=false vuelve al recorrido lineal (pruebas).
        public bool UseSpatialGrid=true;
        const double GridCell=4;
        double gridMinX,gridMinZ;int gridW,gridH;int[] gridStart,gridItems,stamp,candidates;int stampValue;
        void BuildGrid()
        {
            stamp=new int[solids.Length];candidates=new int[solids.Length];
            if(solids.Length==0){gridW=gridH=0;gridStart=new int[1];gridItems=new int[0];return;}
            double minX=double.MaxValue,minZ=double.MaxValue,maxX=double.MinValue,maxZ=double.MinValue;
            foreach(var s in solids){minX=Math.Min(minX,s.Min.X);minZ=Math.Min(minZ,s.Min.Z);maxX=Math.Max(maxX,s.Max.X);maxZ=Math.Max(maxZ,s.Max.Z);}
            gridMinX=minX;gridMinZ=minZ;gridW=Math.Max(1,(int)Math.Ceiling((maxX-minX)/GridCell)+1);gridH=Math.Max(1,(int)Math.Ceiling((maxZ-minZ)/GridCell)+1);
            var counts=new int[gridW*gridH+1];
            for(int i=0;i<solids.Length;i++){
                var s=solids[i];CellRange(s.Min.X,s.Min.Z,s.Max.X,s.Max.Z,out int x0,out int z0,out int x1,out int z1);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)counts[z*gridW+x+1]++;
            }
            for(int c=0;c<gridW*gridH;c++)counts[c+1]+=counts[c];
            gridStart=(int[])counts.Clone();gridItems=new int[counts[gridW*gridH]];var fill=(int[])counts.Clone();
            for(int i=0;i<solids.Length;i++){
                var s=solids[i];CellRange(s.Min.X,s.Min.Z,s.Max.X,s.Max.Z,out int x0,out int z0,out int x1,out int z1);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)gridItems[fill[z*gridW+x]++]=i;
            }
        }
        void CellRange(double ax,double az,double bx,double bz,out int x0,out int z0,out int x1,out int z1)
        {
            x0=Math.Max(0,Math.Min(gridW-1,(int)Math.Floor((ax-gridMinX)/GridCell)));x1=Math.Max(0,Math.Min(gridW-1,(int)Math.Floor((bx-gridMinX)/GridCell)));
            z0=Math.Max(0,Math.Min(gridH-1,(int)Math.Floor((az-gridMinZ)/GridCell)));z1=Math.Max(0,Math.Min(gridH-1,(int)Math.Floor((bz-gridMinZ)/GridCell)));
        }
        // Índices de los sólidos que pueden tocar el rectángulo XZ, sin duplicados y ordenados.
        int Gather(double ax,double az,double bx,double bz)
        {
            if(!UseSpatialGrid||gridW==0){for(int i=0;i<solids.Length;i++)candidates[i]=i;return solids.Length;}
            const double pad=1e-6;
            CellRange(ax-pad,az-pad,bx+pad,bz+pad,out int x0,out int z0,out int x1,out int z1);
            if(++stampValue==int.MaxValue){Array.Clear(stamp,0,stamp.Length);stampValue=1;}
            int n=0;
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
                int c=z*gridW+x;
                for(int k=gridStart[c];k<gridStart[c+1];k++){int i=gridItems[k];if(stamp[i]==stampValue)continue;stamp[i]=stampValue;candidates[n++]=i;}
            }
            if(n>1)Array.Sort(candidates,0,n);
            return n;
        }
        public static bool Finite(Vec3 p)=>Valid(p.X)&&Valid(p.Y)&&Valid(p.Z);
        static bool Valid(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        static void Validate(Vec3 p,double radius,double height)
        {
            if(!Finite(p)||!Valid(radius)||!Valid(height)||radius<0||height<0)throw new ArgumentException("Consulta vertical inválida");
        }
        // Envolvente conservadora del cilindro del jugador: caja de radio r y altura h.
        // Barrido continuo por slabs: sin muestreo que pueda saltarse paredes finas.
        // El volumen es de consulta, no sustituye ni agranda los colliders del mundo.
        public bool Clear(Vec3 feet,double radius,double height)
        {
            Validate(feet,radius,height);
            int n=Gather(feet.X-radius,feet.Z-radius,feet.X+radius,feet.Z+radius);
            for(int k=0;k<n;k++){var s=solids[candidates[k]];if(feet.X>s.Min.X-radius+Epsilon&&feet.X<s.Max.X+radius-Epsilon
                &&feet.Y>s.Min.Y-height+Epsilon&&feet.Y<s.Max.Y-Epsilon
                &&feet.Z>s.Min.Z-radius+Epsilon&&feet.Z<s.Max.Z+radius-Epsilon)return false;}
            return terrain==null||!terrain.Query(feet,feet,radius,height,WallCos,true,out _);
        }
        // P0-B: como Clear, pero los sólidos cuya tapa queda a un escalón o menos de los pies no
        // cuentan como obstáculo: se suben andando (regla heredada de los enemigos, +0,6).
        public bool ClearAboveStep(Vec3 feet,double radius,double height,double step)
        {
            Validate(feet,radius,height);
            int n=Gather(feet.X-radius,feet.Z-radius,feet.X+radius,feet.Z+radius);
            for(int k=0;k<n;k++){var s=solids[candidates[k]];if(s.Max.Y>feet.Y+step&&feet.X>s.Min.X-radius+Epsilon&&feet.X<s.Max.X+radius-Epsilon
                &&feet.Y>s.Min.Y-height+Epsilon&&feet.Y<s.Max.Y-Epsilon
                &&feet.Z>s.Min.Z-radius+Epsilon&&feet.Z<s.Max.Z+radius-Epsilon)return false;}
            return terrain==null||!terrain.Query(feet,feet,radius,height,WallCos,true,out _);
        }
        // P0-B: ninguna cara abrupta del terreno bajo la huella (sin voladizo sobre un acantilado).
        public bool TerrainFootprintWalkable(Vec3 feet,double radius)=>terrain==null||terrain.SupportHeight(feet,radius,WallCos,out _);
        static bool Slab(double start,double delta,double min,double max,Vec3 low,Vec3 high,ref double enter,ref double exit,ref Vec3 normal)
        {
            if(Math.Abs(delta)<Epsilon)return start>min+Epsilon&&start<max-Epsilon;
            double a=(min-start)/delta,b=(max-start)/delta;Vec3 n=low;
            if(a>b){double temp=a;a=b;b=temp;n=high;}
            if(a>enter){enter=a;normal=n;}exit=Math.Min(exit,b);
            return enter<exit-Epsilon;
        }
        public bool Sweep(Vec3 from,Vec3 to,double radius,double height,out VerticalHit hit)
        {
            Validate(from,radius,height);Validate(to,radius,height);
            hit=new VerticalHit{Fraction=1,Id=-1};bool found=false;
            int n=Gather(Math.Min(from.X,to.X)-radius,Math.Min(from.Z,to.Z)-radius,Math.Max(from.X,to.X)+radius,Math.Max(from.Z,to.Z)+radius);
            for(int k=0;k<n;k++){var s=solids[candidates[k]];
                double enter=double.NegativeInfinity,exit=double.PositiveInfinity;Vec3 normal=default;
                if(!Slab(from.X,to.X-from.X,s.Min.X-radius,s.Max.X+radius,new Vec3(-1,0,0),new Vec3(1,0,0),ref enter,ref exit,ref normal)
                    ||!Slab(from.Y,to.Y-from.Y,s.Min.Y-height,s.Max.Y,new Vec3(0,-1,0),new Vec3(0,1,0),ref enter,ref exit,ref normal)
                    ||!Slab(from.Z,to.Z-from.Z,s.Min.Z-radius,s.Max.Z+radius,new Vec3(0,0,-1),new Vec3(0,0,1),ref enter,ref exit,ref normal))continue;
                if(exit<=Epsilon||enter>1||enter>=hit.Fraction&&found)continue;
                double fraction=Math.Max(0,enter);
                hit=new VerticalHit{Id=s.Id,Kind=s.Kind,Normal=normal,Fraction=fraction,TopY=s.Max.Y,
                    Point=new Vec3(from.X+(to.X-from.X)*fraction,from.Y+(to.Y-from.Y)*fraction,from.Z+(to.Z-from.Z)*fraction)};found=true;
            }
            if(terrain!=null&&terrain.Query(from,to,radius,height,WallCos,false,out var th)&&(!found||th.Fraction<hit.Fraction)){hit=th;found=true;}
            return found;
        }
        public bool Wall(Vec3 feet,Vec3 toward,double radius,double height,double reach,out VerticalHit hit)
        {
            if(!Valid(reach)||reach<0||!Finite(toward))throw new ArgumentException("Alcance inválido");
            double length=JsMath.Hypot(toward.X,toward.Z);hit=default;if(length<Epsilon)return false;
            var end=new Vec3(feet.X+toward.X/length*reach,feet.Y,feet.Z+toward.Z/length*reach);
            if(!Sweep(feet,end,radius,height,out hit)||!IsWallNormal(hit.Normal))return false;
            // El primer sólido bloquea también la consulta: nunca buscar una pared detrás del follaje sólido.
            return VerticalSolid.IsWorldWall(hit.Kind);
        }
        public bool Support(Vec3 feet,double radius,double tolerance,out int id)
        {
            Validate(feet,radius,0);if(!Valid(tolerance)||tolerance<0)throw new ArgumentException("Tolerancia inválida");
            id=-1;
            int n=Gather(feet.X-radius,feet.Z-radius,feet.X+radius,feet.Z+radius);
            for(int k=0;k<n;k++){var s=solids[candidates[k]];if(Math.Abs(feet.Y-s.Max.Y)<=tolerance&&feet.X-radius>=s.Min.X-Epsilon
                &&feet.X+radius<=s.Max.X+Epsilon&&feet.Z-radius>=s.Min.Z-Epsilon&&feet.Z+radius<=s.Max.Z+Epsilon){id=s.Id;return true;}}
            if(terrain!=null&&terrain.Support(feet,radius,tolerance,WallCos))return true;
            // Cobertura por unión de tapas coplanares, incluso al cruzar colliders (mismos candidatos).
            if(radius>0&&Covered(feet.X-radius,feet.X+radius,feet.Z-radius,feet.Z+radius,feet.Y,tolerance,0,n))return true;
            return false;
        }
        bool Covered(double x0,double x1,double z0,double z1,double y,double tolerance,int start,int count)
        {
            if(x1-x0<=Epsilon||z1-z0<=Epsilon)return true;
            for(int i=start;i<count;i++){
                var s=solids[candidates[i]];if(Math.Abs(y-s.Max.Y)>tolerance)continue;
                double a=Math.Max(x0,s.Min.X),b=Math.Min(x1,s.Max.X),c=Math.Max(z0,s.Min.Z),d=Math.Min(z1,s.Max.Z);
                if(b-a<=Epsilon||d-c<=Epsilon)continue;
                return Covered(x0,a,z0,z1,y,tolerance,i+1,count)&&Covered(b,x1,z0,z1,y,tolerance,i+1,count)
                    &&Covered(a,b,z0,c,y,tolerance,i+1,count)&&Covered(a,b,d,z1,y,tolerance,i+1,count);
            }
            return false;
        }
        public bool NearEdge(Vec3 feet,VerticalHit wall)=>feet.Y>=wall.TopY-.2;
        // Salida con dos tramos explícitos (ascenso exterior, avance sobre el borde).
        // El consumidor debe recorrerlos; esta consulta no coloca ni teletransporta al jugador.
        public bool Ledge(Vec3 feet,VerticalHit wall,double radius,double height,out Vec3 lift,out Vec3 top)
        {
            lift=top=default;VerticalSolid solid=null;
            if(!VerticalSolid.IsWorldWall(wall.Kind)||!IsWallNormal(wall.Normal))return false;
            if(wall.Terrain){
                if(terrain==null)return false;
                double length=JsMath.Hypot(wall.Normal.X,wall.Normal.Z);
                // Buscar apoyo cercano a la altura alcanzada; ningún salto a la cima.
                for(double distance=radius;distance<=radius*2+.4;distance+=.05){
                    double tx=feet.X-wall.Normal.X/length*distance,tz=feet.Z-wall.Normal.Z/length*distance;
                    double y=terrain.Height(tx,tz)+.002;
                    if(terrain.SupportHeight(new Vec3(tx,y,tz),radius,WallCos,out double supported))y=supported+.002;
                    foreach(var cap in solids)if(tx>=cap.Min.X&&tx<=cap.Max.X&&tz>=cap.Min.Z&&tz<=cap.Max.Z
                        &&cap.Max.Y+.002>y&&cap.Max.Y<=feet.Y+PlayerTuning.Default.StepHeight)y=cap.Max.Y+.002;
                    if(y<feet.Y-.01||y>feet.Y+PlayerTuning.Default.StepHeight+.002)continue;
                    top=new Vec3(tx,y,tz);lift=new Vec3(feet.X,y,feet.Z);
                    if(ValidLedge(feet,lift,top,radius,height))return true;
                }
                return false;
            }
            foreach(var s in solids)if(s.Id==wall.Id){solid=s;break;}
            if(solid==null||!solid.Climbable||Math.Abs(wall.Normal.Y)>.01)return false;
            lift=new Vec3(feet.X,solid.Max.Y+.002,feet.Z);
            // El agarre puede estar separado hasta Reach de la cara. El apoyo se
            // calcula desde esa cara, no desde la distancia circunstancial del cuerpo.
            double x=wall.Normal.X<-.5?solid.Min.X+radius+.02:wall.Normal.X>.5?solid.Max.X-radius-.02:lift.X;
            double z=wall.Normal.Z<-.5?solid.Min.Z+radius+.02:wall.Normal.Z>.5?solid.Max.Z-radius-.02:lift.Z;
            top=new Vec3(x,lift.Y,z);
            return ValidLedge(feet,lift,top,radius,height);
        }
        bool ValidLedge(Vec3 feet,Vec3 lift,Vec3 top,double radius,double height)
        {
            return Clear(feet,radius,height)&&Clear(lift,radius,height)&&Clear(top,radius,height)
                &&!Sweep(feet,lift,radius,height,out _)&&!Sweep(lift,top,radius,height,out _)
                &&Support(top,radius,.003,out _);
        }
    }
}
