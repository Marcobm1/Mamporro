using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Clase de cuerpo para navegar: radio y altura del mayor enemigo que la usa.
    public sealed class NavClass
    {
        public readonly int Index;
        public readonly double Radius,Height;
        public NavClass(int index,double radius,double height){Index=index;Radius=radius;Height=height;}
        // S: pelusa, cucaracha, paloma. M: táper, rata. L: jefe. Deducido del catálogo, sin IDs.
        public static readonly NavClass[] All={new NavClass(0,.5,.95),new NavClass(1,.9,1.5),new NavClass(2,2.4,4.4)};
        public static int Of(EnemyDef d)
        {
            for(int k=0;k<All.Length;k++)if(d.radius<=All[k].Radius+1e-9&&d.height<=All[k].Height+1e-9)return k;
            return All.Length-1;
        }
    }
    public enum NavEdgeKind : byte { Walk, Climb, Drop }

    // P0-B: grafo multinivel de superficies transitables con aristas de caminar, escalar y caer.
    // Construido una vez por mundo y clase con las mismas consultas físicas que la escalada del
    // jugador (VerticalQueries). Nodos: rejilla de 0,5 m con capas por altura (terreno y tapas).
    public sealed class NavGraph
    {
        public const double Cell=.5;
        // Margen de media celda: el cuerpo cabe en cualquier punto de la celda, no solo en su centro.
        public const double Margin=.25;
        // Escalón que ya suben los enemigos (Enemies.Step: PushOut y Height con +0,6).
        public const double Step=.6;
        public const int TransitionCost=50,Scale=100;
        const double ClimbStep=.15,EdgeWindow=.35,Reach=.18,ContactGap=.001;

        public readonly NavClass Class;
        public readonly VerticalQueries Queries;
        public readonly Heightfield Terrain;
        public readonly double MinX,MinZ;
        public readonly int W,H;
        public int NodeCount,EdgeCount,PathCount;
        public int[] CellStart,NodeCell,EdgeStart,EdgeFrom,EdgeTo,EdgeCost,EdgePath,RevStart,RevEdge,PathStart,PathLedge;
        public float[] NodeY,PathX,PathY,PathZ,PathNX,PathNZ,PathD;
        public bool[] NodeTerrain;
        public byte[] EdgeKind;
        public double BuildMs;
        public int ClimbEdges,DropEdges,WalkEdges;
        // Diagnóstico de construcción: intentos de escalada descartados y por qué.
        public int ClimbFailNoContact,ClimbFailNoLedge,ClimbFailNoTarget,ClimbFailPenetration,DropFailPenetration;

        public NavGraph(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,NavClass navClass)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            Queries=queries;Terrain=terrain;Class=navClass;
            MinX=MinZ=-terrain.Half;W=H=(int)Math.Round(terrain.Size/Cell);
            BuildNodes(inside);BuildEdges();
            BuildMs=watch.Elapsed.TotalMilliseconds;
        }
        public double CellX(int cell)=>MinX+(cell%W+.5)*Cell;
        public double CellZ(int cell)=>MinZ+(cell/W+.5)*Cell;
        public double NodeX(int n)=>CellX(NodeCell[n]);
        public double NodeZ(int n)=>CellZ(NodeCell[n]);
        public int CellOf(double x,double z)
        {
            int cx=(int)Math.Floor((x-MinX)/Cell),cz=(int)Math.Floor((z-MinZ)/Cell);
            return cx<0||cz<0||cx>=W||cz>=H?-1:cz*W+cx;
        }
        // Altura de la superficie del nodo en un punto de su celda.
        public double SurfaceY(int n,double x,double z)=>NodeTerrain[n]?Terrain.HeightAt(x,z):NodeY[n];

        void BuildNodes(Func<double,double,double,bool> inside)
        {
            double r=Class.Radius+Margin,h=Class.Height,wallCos=Math.Cos(Queries.WallMinSlopeDegrees*WorldMath.Deg2Rad);
            var cellStart=new int[W*H+1];var ys=new List<float>();var terrainFlags=new List<bool>();var cells=new List<int>();
            var candidates=new List<double>();var fromTerrain=new List<bool>();
            for(int c=0;c<W*H;c++){
                cellStart[c]=ys.Count;double x=CellX(c),z=CellZ(c);
                if(!inside(x,z,r))continue;
                candidates.Clear();fromTerrain.Clear();
                // Primero las tapas: a igual altura prevalece la tapa sobre el terreno oculto bajo ella.
                for(int i=0;i<Queries.Count;i++){
                    var s=Queries.Solid(i);
                    // Tapas de sólidos del mundo; follaje, interactuables y límites no son suelo de la horda.
                    if(!s.Climbable||x<s.Min.X||x>s.Max.X||z<s.Min.Z||z>s.Max.Z)continue;
                    candidates.Add(s.Max.Y);fromTerrain.Add(false);
                }
                if(Terrain.NormalAt(x,z).Y>=wallCos){candidates.Add(Terrain.HeightAt(x,z));fromTerrain.Add(true);}
                for(int a=0;a<candidates.Count;a++){
                    bool duplicate=false;for(int b=0;b<a;b++)if(Math.Abs(candidates[a]-candidates[b])<.05)duplicate=true;
                    var feet=new Vec3(x,candidates[a],z);
                    // Cuerpo libre en toda la celda (los escalones no cuentan) y huella apoyada: sin
                    // sobresalir de un tejado ni de un acantilado. En terreno inclinado los pies van de
                    // la altura mínima a la máxima de la celda: se comprueba ese volumen completo.
                    if(duplicate)continue;
                    if(fromTerrain[a]){
                        CellHeightRange(x,z,out double low,out double high);
                        if(!Queries.ClearAboveStep(new Vec3(x,low,z),r,h+(high-low),Step))continue;
                    }else if(!Queries.ClearAboveStep(feet,r,h,Step))continue;
                    if(fromTerrain[a]?!Queries.TerrainFootprintWalkable(feet,Class.Radius):!Queries.Support(feet,Class.Radius,.01,out _))continue;
                    ys.Add((float)candidates[a]);terrainFlags.Add(fromTerrain[a]);cells.Add(c);
                }
                // Capas de la celda ordenadas de abajo arriba.
                int start=cellStart[c];
                for(int a=start+1;a<ys.Count;a++)for(int b=a;b>start&&ys[b]<ys[b-1];b--){
                    float t=ys[b];ys[b]=ys[b-1];ys[b-1]=t;bool f=terrainFlags[b];terrainFlags[b]=terrainFlags[b-1];terrainFlags[b-1]=f;
                }
            }
            cellStart[W*H]=ys.Count;
            CellStart=cellStart;NodeY=ys.ToArray();NodeTerrain=terrainFlags.ToArray();NodeCell=cells.ToArray();NodeCount=NodeY.Length;
        }
        // Alturas extremas del terreno lineal a trozos dentro de la celda: esquinas, centro y
        // vértices del heightfield que caen dentro (las celdas de 0,5 m contienen como mucho uno).
        void CellHeightRange(double x,double z,out double low,out double high)
        {
            double lo=double.MaxValue,hi=double.MinValue,hc=Cell/2;
            void At(double px,double pz){double y=Terrain.HeightAt(px,pz);if(y<lo)lo=y;if(y>hi)hi=y;}
            At(x-hc,z-hc);At(x+hc,z-hc);At(x-hc,z+hc);At(x+hc,z+hc);At(x,z);
            double s=Terrain.CellSize;
            for(double vx=Math.Ceiling((x-hc+Terrain.Half)/s)*s-Terrain.Half;vx<=x+hc;vx+=s)
                for(double vz=Math.Ceiling((z-hc+Terrain.Half)/s)*s-Terrain.Half;vz<=z+hc;vz+=s)At(vx,vz);
            low=lo;high=hi;
        }
        // Nodo de la celda cuya superficie está más alta sin superar y+tolerancia.
        public int NodeAt(double x,double y,double z,double tolerance)
        {
            int c=CellOf(x,z);if(c<0)return -1;int best=-1;
            for(int n=CellStart[c];n<CellStart[c+1];n++)if(SurfaceY(n,x,z)<=y+tolerance)best=n;
            return best;
        }
        // Nodo más cercano (3D) con la superficie entre y-below e y+above, en un radio de celdas.
        public int Nearest(double x,double y,double z,int radius,double below,double above)
        {
            int c=CellOf(x,z);if(c<0)return -1;int cx=c%W,cz=c/W,best=-1;double bestD=double.MaxValue;
            for(int dz=-radius;dz<=radius;dz++)for(int dx=-radius;dx<=radius;dx++){
                int ix=cx+dx,iz=cz+dz;if(ix<0||iz<0||ix>=W||iz>=H)continue;int cell=iz*W+ix;
                for(int n=CellStart[cell];n<CellStart[cell+1];n++){
                    double ny=NodeY[n];if(ny<y-below||ny>y+above)continue;
                    double ex=CellX(cell)-x,ez=CellZ(cell)-z,ey=ny-y,d=ex*ex+ez*ez+ey*ey;
                    if(d<bestD){bestD=d;best=n;}
                }
            }
            return best;
        }
        int Layer(int cell,double y,double tolerance)
        {
            int best=-1;double bestD=tolerance;
            for(int n=CellStart[cell];n<CellStart[cell+1];n++){double d=Math.Abs(NodeY[n]-y);if(d<=bestD){bestD=d;best=n;}}
            return best;
        }

        struct Edge{public int From,To,Cost,Path;public NavEdgeKind Kind;}
        readonly List<Edge> edges=new List<Edge>();
        readonly List<float> px=new List<float>(),py=new List<float>(),pz=new List<float>(),pnx=new List<float>(),pnz=new List<float>(),pd=new List<float>();
        readonly List<int> pathStart=new List<int>(),pathLedge=new List<int>();

        void BuildEdges()
        {
            var normals=new List<Vec3>();
            for(int a=0;a<NodeCount;a++){
                int c=NodeCell[a],cx=c%W,cz=c/W;double ya=NodeY[a];int connected=0;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
                    if(dx==0&&dz==0)continue;int ix=cx+dx,iz=cz+dz;if(ix<0||iz<0||ix>=W||iz>=H)continue;int c2=iz*W+ix;
                    // Diagonal solo si los dos ortogonales también conectan: sin cortar esquinas.
                    if(dx!=0&&dz!=0&&(Layer(cz*W+ix,ya,Step)<0||Layer(iz*W+cx,ya,Step)<0))continue;
                    for(int b=CellStart[c2];b<CellStart[c2+1];b++){
                        double dy=NodeY[b]-ya;if(Math.Abs(dy)>Step)continue;connected++;
                        double run=Cell*Math.Sqrt(dx*dx+dz*dz);
                        edges.Add(new Edge{From=a,To=b,Cost=(int)Math.Round(Math.Sqrt(run*run+dy*dy)*Scale),Path=-1,Kind=NavEdgeKind.Walk});WalkEdges++;
                    }
                }
                // Nodo interior (conectado en las 8 direcciones): ninguna pared ni borde a su alcance.
                if(connected>=8)continue;
                AddClimbs(a,normals);AddDrops(a);
            }
            // CSR hacia delante (por origen) e inversa (por destino) para el campo.
            edges.Sort((p,q)=>p.From!=q.From?p.From.CompareTo(q.From):p.To.CompareTo(q.To));
            EdgeCount=edges.Count;EdgeStart=new int[NodeCount+1];EdgeFrom=new int[EdgeCount];EdgeTo=new int[EdgeCount];EdgeCost=new int[EdgeCount];EdgePath=new int[EdgeCount];EdgeKind=new byte[EdgeCount];
            for(int e=0;e<EdgeCount;e++){var x=edges[e];EdgeStart[x.From+1]++;EdgeFrom[e]=x.From;EdgeTo[e]=x.To;EdgeCost[e]=x.Cost;EdgePath[e]=x.Path;EdgeKind[e]=(byte)x.Kind;}
            for(int n=0;n<NodeCount;n++)EdgeStart[n+1]+=EdgeStart[n];
            RevStart=new int[NodeCount+1];RevEdge=new int[EdgeCount];
            for(int e=0;e<EdgeCount;e++)RevStart[EdgeTo[e]+1]++;
            for(int n=0;n<NodeCount;n++)RevStart[n+1]+=RevStart[n];
            var fill=(int[])RevStart.Clone();
            for(int e=0;e<EdgeCount;e++)RevEdge[fill[EdgeTo[e]]++]=e;
            PathCount=pathStart.Count;PathStart=new int[PathCount+1];pathStart.CopyTo(PathStart);PathStart[PathCount]=px.Count;PathLedge=pathLedge.ToArray();
            PathX=px.ToArray();PathY=py.ToArray();PathZ=pz.ToArray();PathNX=pnx.ToArray();PathNZ=pnz.ToArray();PathD=pd.ToArray();
            edges.Clear();
        }
        int BeginPath(double nx,double nz,double d)
        {pathStart.Add(px.Count);pathLedge.Add(-1);pnx.Add((float)nx);pnz.Add((float)nz);pd.Add((float)d);return pathStart.Count-1;}
        void Point(Vec3 p){px.Add((float)p.X);py.Add((float)p.Y);pz.Add((float)p.Z);}
        void CancelPath(int path)
        {int start=pathStart[path];px.RemoveRange(start,px.Count-start);py.RemoveRange(start,py.Count-start);pz.RemoveRange(start,pz.Count-start);
         pathStart.RemoveAt(path);pathLedge.RemoveAt(path);pnx.RemoveAt(path);pnz.RemoveAt(path);pd.RemoveAt(path);}

        // Escalar: misma física que el jugador con el cuerpo de la clase. Cara continua en toda
        // la altura, columna libre (un techo o voladizo invalida la arista) y cima con Ledge
        // (cuerpo completo, apoyo y dos tramos barridos). La trayectoria se guarda para seguirla.
        void AddClimbs(int a,List<Vec3> normals)
        {
            double r=Class.Radius,h=Class.Height;var feet=new Vec3(NodeX(a),NodeY[a],NodeZ(a));normals.Clear();
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
                if(dx==0&&dz==0)continue;double l=Math.Sqrt(dx*dx+dz*dz);
                if(!Queries.Wall(feet,new Vec3(dx/l,0,dz/l),r,h,Margin+Cell,out var hit))continue;
                double hl=JsMath.Hypot(hit.Normal.X,hit.Normal.Z);var n=new Vec3(hit.Normal.X/hl,0,hit.Normal.Z/hl);
                bool seen=false;foreach(var m in normals)if(m.X*n.X+m.Z*n.Z>.99)seen=true;
                if(seen)continue;normals.Add(n);TryClimb(a,feet,n);
            }
        }
        void TryClimb(int a,Vec3 feet,Vec3 n)
        {
            double r=Class.Radius,h=Class.Height;var dir=new Vec3(-n.X,0,-n.Z);double reach=Margin+Cell;
            var end=new Vec3(feet.X+dir.X*reach,feet.Y,feet.Z+dir.Z*reach);
            if(!Queries.Sweep(feet,end,r,h,out var hit)){ClimbFailNoContact++;return;}
            // Contacto a 1 mm de la cara: holgura muy superior al error de redondeo de las posiciones
            // float de la horda (~1e-5 m), para que la escalada nunca quede dentro de la pared.
            double f=Math.Max(0,hit.Fraction-ContactGap/reach);var c=new Vec3(feet.X+dir.X*reach*f,feet.Y,feet.Z+dir.Z*reach*f);
            if(!Queries.Wall(c,dir,r,h,Reach,out var wall)){ClimbFailNoContact++;return;}
            int path=BeginPath(n.X,n.Z,n.X*c.X+n.Z*c.Z);Point(feet);Point(c);
            Vec3 lift=default,top=default;bool ok=false;Vec3 lastDir=default;
            for(int i=0;i<600;i++){
                if(c.Y>=wall.TopY-EdgeWindow&&Queries.Ledge(c,wall,r,h,out lift,out top)){ok=true;break;}
                var up=VerticalMotion.WallUp(wall.Normal);var t=new Vec3(c.X+up.X*ClimbStep,c.Y+up.Y*ClimbStep,c.Z+up.Z*ClimbStep);
                if(Queries.Sweep(c,t,r,h,out _)){LastClimbFailure="techo o voladizo en la columna";break;}
                if(!Queries.Clear(t,r,h)){LastClimbFailure="cuerpo sin espacio en la columna";break;}
                // Solo se guardan los cambios de dirección: muros rectos quedan en dos puntos.
                if(i>0&&up.X*lastDir.X+up.Y*lastDir.Y+up.Z*lastDir.Z>.9999){px[px.Count-1]=(float)t.X;py[py.Count-1]=(float)t.Y;pz[pz.Count-1]=(float)t.Z;}
                else Point(t);
                lastDir=up;c=t;
                double wl=JsMath.Hypot(wall.Normal.X,wall.Normal.Z);
                if(!Queries.Wall(c,new Vec3(-wall.Normal.X/wl,0,-wall.Normal.Z/wl),r,h,Reach,out var contact)){LastClimbFailure=$"cara agotada en y={c.Y:F2} (tope {wall.TopY:F2})";break;}
                wall=contact;
            }
            int b=-1;Vec3 finalMid=default;if(!ok)ClimbFailNoLedge++;
            if(ok)LastClimbFailure="sin nodo de destino junto al borde";
            if(ok)for(int k=0;k<=8&&b<0;k++){
                double x=top.X+dir.X*k*Cell*.5,z=top.Z+dir.Z*k*Cell*.5;int cell=CellOf(x,z);if(cell<0)continue;
                int candidate=Layer(cell,top.Y,.1);if(candidate<0||candidate==a)continue;
                var center=new Vec3(NodeX(candidate),NodeY[candidate],NodeZ(candidate));
                var mid=center.Y<top.Y?new Vec3(center.X,top.Y,center.Z):new Vec3(top.X,center.Y,top.Z);
                // El último tramo termina apoyado en la superficie: se barre hasta 2 mm por encima.
                var rest=new Vec3(center.X,center.Y+.002,center.Z);
                if(!Queries.Sweep(top,mid,r,h,out _)&&(Math.Abs(mid.Y-rest.Y)<.003||!Queries.Sweep(mid,rest,r,h,out _))&&Queries.Clear(mid,r,h)){b=candidate;finalMid=mid;}
            }
            if(b<0){ClimbFailNoTarget+=ok?1:0;CancelPath(path);return;}
            pathLedge[path]=px.Count;Point(lift);Point(top);Point(finalMid);Point(new Vec3(NodeX(b),NodeY[b],NodeZ(b)));
            if(!PathIsFree(path)){ClimbFailPenetration++;LastClimbFailure="la trayectoria toca un sólido";CancelPath(path);return;}
            int cost=TransitionCost;for(int k=pathStart[path]+1;k<px.Count;k++)cost+=(int)Math.Round(Dist(k-1,k)*Scale);
            edges.Add(new Edge{From=a,To=b,Cost=cost,Path=path,Kind=NavEdgeKind.Climb});ClimbEdges++;LastClimbFailure="";
        }
        public string LastClimbFailure="";
        // Diagnóstico QA: repite la detección de paredes de un nodo y devuelve el resultado de
        // cada intento, sin modificar el grafo construido.
        public string DiagnoseClimbs(int node)
        {
            var report=new System.Text.StringBuilder();var normals=new List<Vec3>();double r=Class.Radius,h=Class.Height;
            var feet=new Vec3(NodeX(node),NodeY[node],NodeZ(node));int before=edges.Count;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
                if(dx==0&&dz==0)continue;double l=Math.Sqrt(dx*dx+dz*dz);
                if(!Queries.Wall(feet,new Vec3(dx/l,0,dz/l),r,h,Margin+Cell,out var hit))continue;
                double hl=JsMath.Hypot(hit.Normal.X,hit.Normal.Z);var n=new Vec3(hit.Normal.X/hl,0,hit.Normal.Z/hl);
                bool seen=false;foreach(var m in normals)if(m.X*n.X+m.Z*n.Z>.99)seen=true;
                if(seen)continue;normals.Add(n);int paths=pathStart.Count;LastClimbFailure="sin contacto";TryClimb(node,feet,n);
                report.Append($"normal ({n.X:F2},{n.Z:F2}): {(LastClimbFailure==""?"arista creada":LastClimbFailure)}; ");
                if(pathStart.Count>paths){CancelPath(pathStart.Count-1);}
            }
            while(edges.Count>before)edges.RemoveAt(edges.Count-1);
            return report.Length==0?"sin paredes al alcance":report.ToString();
        }
        // Validación por muestreo (0,1 m) de toda la trayectoria con el cuerpo de la clase: el barrido
        // ignora caras de las que se aleja y no detecta un solape ya existente. El punto final
        // admite escalones, como los nodos.
        bool PathIsFree(int path)
        {
            double r=Class.Radius,h=Class.Height;int start=pathStart[path],end=px.Count;
            for(int k=start+1;k<end;k++){
                double len=Dist(k-1,k);int steps=Math.Max(1,(int)Math.Ceiling(len/.1));
                for(int i=0;i<=steps;i++){double f=i/(double)steps;
                    var q=new Vec3(px[k-1]+(px[k]-px[k-1])*f,py[k-1]+(py[k]-py[k-1])*f,pz[k-1]+(pz[k]-pz[k-1])*f);
                    bool last=k==end-1&&i==steps;
                    if(last?!Queries.ClearAboveStep(q,r,h,Step):!Queries.Clear(q,r,h))return false;}
            }
            return true;
        }
        double Dist(int i,int j)=>Math.Sqrt((px[j]-px[i])*(px[j]-px[i])+(py[j]-py[i])*(py[j]-py[i])+(pz[j]-pz[i])*(pz[j]-pz[i]));

        // Caer: desde un borde hacia la primera superficie más baja en esa dirección, con el
        // tramo horizontal y la columna de caída libres de sólidos. Sin saltar huecos.
        void AddDrops(int a)
        {
            double r=Class.Radius,h=Class.Height,ya=NodeY[a];int c=NodeCell[a],cx=c%W,cz=c/W;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
                if(dx==0&&dz==0)continue;
                // Alcance horizontal según el cuerpo: borde apoyado en el tejado y suelo libre junto a la pared.
                int reach=(int)Math.Ceiling((2*(r+Margin)+1)/Cell);
                for(int k=1;k<=reach;k++){
                    int ix=cx+dx*k,iz=cz+dz*k;if(ix<0||iz<0||ix>=W||iz>=H)break;int ck=iz*W+ix;
                    if(Layer(ck,ya,Step)>=0)break; // misma altura: es un paso o un hueco, no un borde
                    int b=-1;for(int n=CellStart[ck];n<CellStart[ck+1];n++)if(NodeY[n]<ya-Step)b=n;
                    if(b<0)continue;
                    var from=new Vec3(NodeX(a),ya,NodeZ(a));var over=new Vec3(NodeX(b),ya,NodeZ(b));var land=new Vec3(NodeX(b),NodeY[b],NodeZ(b));
                    if(Queries.Sweep(from,over,r,h,out _)||Queries.Sweep(over,new Vec3(land.X,land.Y+.01,land.Z),r,h,out _))break;
                    int path=BeginPath(0,0,0);Point(from);Point(over);Point(land);
                    if(!PathIsFree(path)){DropFailPenetration++;CancelPath(path);break;}
                    double run=JsMath.Hypot(over.X-from.X,over.Z-from.Z),fall=ya-NodeY[b];
                    edges.Add(new Edge{From=a,To=b,Cost=TransitionCost+(int)Math.Round((run+fall*.25)*Scale),Path=path,Kind=NavEdgeKind.Drop});DropEdges++;
                    break;
                }
            }
        }
    }
}
