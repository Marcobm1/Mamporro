using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    public enum StructureKind { House, Temple, Farm, Well, Tower, Wall, Stack, Tree }
    public sealed class ProceduralStructure
    {
        public StructureKind Kind;
        public int Rotation;
        public double X0,Z0,X1,Z1,BaseY;
        public readonly List<VerticalSolid> Solids=new List<VerticalSolid>();
        public bool Removed;
    }

    // P0-C3: estructuras de bloques provisionales alineadas a la rejilla (giros de 90°). Reglas que
    // evitan refugios por construcción: todo elemento escalable mide ≤ 0,7 m de ancho (nadie se
    // sostiene arriba) o ≥ 2 m (caben los enemigos pequeños); puertas y huecos ≥ 2 m; tejados
    // enrasados con los muros (sin voladizos que impidan escalar); nada flotante.
    public sealed class ProceduralWorld
    {
        public readonly ProceduralLayout Layout;
        public readonly List<ProceduralStructure> Structures=new List<ProceduralStructure>();
        public double Milliseconds;
        const double Keep=3,SpawnKeep=16;
        readonly int size;readonly bool[] flat,occupied;readonly int[] cellLevel;
        int nextId=1;Rng rng;

        public ProceduralWorld(ProceduralLayout layout)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            Layout=layout;size=(int)Math.Round(layout.Settings.Size);flat=new bool[size*size];occupied=new bool[size*size];cellLevel=new int[size*size];
            rng=new Rng(layout.Seed).Derive("p0c-estructuras-"+layout.Attempt);
            ComputeFlatness();
            // Las mayores primero (necesitan ~17×19 m llanos con márgenes) y con más intentos.
            var plan=new (StructureKind kind,int min,int max)[]{
                (StructureKind.Temple,1,2),(StructureKind.Farm,1,2),(StructureKind.House,4,6),(StructureKind.Stack,1,3),
                (StructureKind.Tower,3,5),(StructureKind.Wall,4,7),(StructureKind.Well,2,3)};
            foreach(var (kind,min,max) in plan){int count=min+rng.Int(0,max-min);for(int i=0;i<count;i++)Place(kind);}
            int trees=40+rng.Int(0,20);for(int i=0;i<trees;i++)Place(StructureKind.Tree);
            Milliseconds=watch.Elapsed.TotalMilliseconds;
        }
        public IEnumerable<VerticalSolid> Solids(){foreach(var s in Structures)if(!s.Removed)foreach(var v in s.Solids)yield return v;}
        public int Count(StructureKind kind){int n=0;foreach(var s in Structures)if(!s.Removed&&s.Kind==kind)n++;return n;}
        double Half=>Layout.Settings.Size/2;

        // Celda de 1 m llana a un nivel exacto (sus cuatro vértices a la misma altura de nivel).
        void ComputeFlatness()
        {
            var t=Layout.Terrain;double h=Layout.Settings.LevelHeight;
            for(int j=0;j<size;j++)for(int i=0;i<size;i++){
                double a=t.VertexHeight(i,j),b=t.VertexHeight(i+1,j),c=t.VertexHeight(i,j+1),d=t.VertexHeight(i+1,j+1);
                double level=a/h;bool same=a==b&&a==c&&a==d&&Math.Abs(level-Math.Round(level))<1e-6;
                int m=Layout.MacroAt(-Half+i+.5,-Half+j+.5);
                flat[j*size+i]=same&&m>=0&&Layout.RampOf[m]<0;cellLevel[j*size+i]=same?(int)Math.Round(level):-1;
            }
        }
        // Huella libre: celdas llanas al mismo nivel con 3 m de margen a bordes, rampas y otras estructuras.
        bool Fits(int x0,int z0,int w,int d,out int level)
        {
            level=-1;double cx=-Half+x0+w/2.0,cz=-Half+z0+d/2.0;
            if(JsMath.Hypot(cx,cz)<SpawnKeep+Math.Max(w,d)/2.0)return false;
            int k=(int)Keep;
            for(int z=z0-k;z<z0+d+k;z++)for(int x=x0-k;x<x0+w+k;x++){
                if(x<0||z<0||x>=size||z>=size)return false;int c=z*size+x;
                if(!flat[c]||occupied[c])return false;
                if(level<0)level=cellLevel[c];else if(cellLevel[c]!=level)return false;
                if(WorldMath.Squircle(-Half+x+.5,-Half+z+.5)>Layout.Settings.Limit-2)return false;
            }
            return true;
        }
        void Place(StructureKind kind)
        {
            int tries=kind==StructureKind.Temple||kind==StructureKind.Farm?400:120;
            for(int attempt=0;attempt<tries;attempt++){
                int rot=rng.Int(0,3);var s=new ProceduralStructure{Kind=kind,Rotation=rot};
                var parts=new List<(double x0,double y0,double z0,double x1,double y1,double z1,VerticalSurfaceKind k)>();
                Template(kind,parts,out double w,out double d);
                int fw=(int)Math.Ceiling(rot%2==0?w:d),fd=(int)Math.Ceiling(rot%2==0?d:w);
                int x0=rng.Int(0,size-fw-1),z0=rng.Int(0,size-fd-1);
                if(kind==StructureKind.Tree){if(!FitsTree(x0,z0,out int tl))continue;Commit(s,parts,w,d,x0,z0,tl,1);return;}
                if(!Fits(x0,z0,fw,fd,out int level))continue;
                Commit(s,parts,w,d,x0,z0,level,0);return;
            }
        }
        bool FitsTree(int x,int z,out int level)
        {
            level=-1;
            for(int j=z-1;j<=z+1;j++)for(int i=x-1;i<=x+1;i++){
                if(i<0||j<0||i>=size||j>=size)return false;int c=j*size+i;if(!flat[c]||occupied[c])return false;
                if(level<0)level=cellLevel[c];else if(cellLevel[c]!=level)return false;
            }
            double cx=-Half+x+.5,cz=-Half+z+.5;
            return JsMath.Hypot(cx,cz)>=SpawnKeep&&WorldMath.Squircle(cx,cz)<=Layout.Settings.Limit-2;
        }
        void Commit(ProceduralStructure s,List<(double x0,double y0,double z0,double x1,double y1,double z1,VerticalSurfaceKind k)> parts,double w,double d,int x0,int z0,int level,int margin)
        {
            double baseY=Layout.LevelY(level),ox=-Half+x0,oz=-Half+z0;
            foreach(var p in parts){
                // Giro de 90°·r dentro de la huella w×d: (x,z) → (x,z), (d−z,x), (w−x,d−z), (z,w−x).
                double ax,az,bx,bz;
                switch(s.Rotation){
                    case 1:ax=d-p.z1;bx=d-p.z0;az=p.x0;bz=p.x1;break;
                    case 2:ax=w-p.x1;bx=w-p.x0;az=d-p.z1;bz=d-p.z0;break;
                    case 3:ax=p.z0;bx=p.z1;az=w-p.x1;bz=w-p.x0;break;
                    default:ax=p.x0;bx=p.x1;az=p.z0;bz=p.z1;break;
                }
                s.Solids.Add(new VerticalSolid(nextId++,p.k,new Vec3(ox+ax,baseY+p.y0,oz+az),new Vec3(ox+bx,baseY+p.y1,oz+bz)));
            }
            int fw=(int)Math.Ceiling(s.Rotation%2==0?w:d),fd=(int)Math.Ceiling(s.Rotation%2==0?d:w);
            s.X0=ox;s.Z0=oz;s.X1=ox+fw;s.Z1=oz+fd;s.BaseY=baseY;
            for(int z=z0-margin;z<z0+fd+margin;z++)for(int x=x0-margin;x<x0+fw+margin;x++)if(x>=0&&z>=0&&x<size&&z<size)occupied[z*size+x]=true;
            Structures.Add(s);
        }

        // ------------------------------------------------------------ plantillas (coordenadas locales)
        void Template(StructureKind kind,List<(double,double,double,double,double,double,VerticalSurfaceKind)> p,out double w,out double d)
        {
            var S=VerticalSurfaceKind.Structure;
            void Box(double x0,double y0,double z0,double x1,double y1,double z1,VerticalSurfaceKind k=VerticalSurfaceKind.Structure)=>p.Add((x0,y0,z0,x1,y1,z1,k));
            switch(kind){
                case StructureKind.House:{
                    // Sin escalera exterior: sus peldaños de 0,8 m sostendrían al jugador donde los enemigos
                    // pequeños no tienen sitio (refugio). Al tejado se sube escalando.
                    w=6+rng.Int(0,2);double depth=5+rng.Int(0,2),h=3,t=.5,door=2;bool upper=rng.Chance(.3);
                    double sw=0;d=depth;
                    // Muros con puerta de 2 m en la fachada (z=0) y tejado plano enrasado, transitable.
                    Box(sw,0,0,sw+(w-door)/2,h,t);Box(sw+(w+door)/2,0,0,sw+w,h,t);Box(sw,0,depth-t,sw+w,h,depth);
                    Box(sw,0,t,sw+t,h,depth-t);Box(sw+w-t,0,t,sw+w,h,depth-t);Box(sw,h,0,sw+w,h+.4,depth);
                    if(upper)Box(sw+w/2,h+.4,depth/2,sw+w,h+2.9,depth);
                    break;}
                case StructureKind.Temple:{
                    w=9+rng.Int(0,2);double depth=9+rng.Int(0,2),pod=1.5,stair=3;d=depth+stair;
                    double sx0=w/2-2,sx1=w/2+2;
                    Box(sx0,0,0,sx1,.5,1.5);Box(sx0,0,1.5,sx1,1,3); // peldaños de 0,5 m y 1,5 m de fondo (caben los pequeños)
                    Box(0,0,stair,w,pod,d);
                    foreach(var (cx,cz) in new[]{(1.0,stair+1),(w-1.7,stair+1),(1.0,d-1.7),(w-1.7,d-1.7)})Box(cx,pod,cz,cx+.7,pod+3.5,cz+.7); // pilares finos
                    Box(w/2-2,pod,stair+depth/2-1,w/2+2,pod+3.5,stair+depth/2+3); // cella con tejado transitable
                    break;}
                case StructureKind.Farm:{
                    w=14;d=10;double f=.4,fh=1.2;
                    Box(0,0,0,6,3.5,5); // granero cerrado, tejado transitable
                    // Cerca de 1,2 m con entrada de 2,5 m; bloques de heno de 2 m.
                    Box(6,0,0,8.75,fh,f);Box(11.25,0,0,14,fh,f);Box(6,0,d-f,14,fh,d);Box(14-f,0,f,14,fh,d-f);Box(6,0,5,6+f,fh,d-f);
                    int hay=1+rng.Int(0,1);for(int i=0;i<hay;i++){double hx=8+i*2.5;Box(hx,0,6,hx+2,1.5,8);}
                    break;}
                case StructureKind.Well:{
                    w=3;d=2.4;
                    // Brocal macizo (sin tejado), dos postes finos y travesaño apoyado en ellos.
                    Box(.3,0,0,2.7,.9,2.4);Box(0,0,1.05,.3,1.9,1.35);Box(2.7,0,1.05,3,1.9,1.35);Box(0,1.9,1.05,3,2.2,1.35);
                    break;}
                case StructureKind.Tower:{
                    w=3+rng.Int(0,2);d=3+rng.Int(0,2);double h=new[]{4.5,6,7.5}[rng.Int(0,2)];Box(0,0,0,w,h,d);break;}
                case StructureKind.Wall:{
                    w=6+rng.Int(0,8);d=.6;double h=2.5+rng.Int(0,3)*.5;Box(0,0,0,w,h,d);
                    if(rng.Chance(.4)){double l=4+rng.Int(0,4);d=l;p.Clear();Box(0,0,0,w,h,.6);Box(0,0,.6,.6,h,l);} // muro en L
                    break;}
                case StructureKind.Stack:{
                    w=6;d=6;Box(0,0,0,6,3,6);Box(0,3,0,3,6,3);break;} // bloque escalonado
                default:{w=1;d=1;Box(.15,0,.15,.85,5,.85,VerticalSurfaceKind.Foliage);break;} // árbol: obstáculo no escalable
            }
        }
    }
}
