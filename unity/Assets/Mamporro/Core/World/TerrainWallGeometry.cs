using System;

namespace Mamporro.Core
{
    // Adaptador P0 del mismo heightfield triangular que usa la colisión histórica.
    // Solo las caras abruptas son obstáculos corporales adicionales. El suelo transitable
    // sigue resuelto por PlayerPhysics, cuyo apoyo puntual no se sustituye por una caja.
    internal sealed class TerrainWallGeometry
    {
        readonly Heightfield terrain;
        // Menor componente vertical de las normales de cada celda: una celda cuyo mínimo
        // supera el umbral no tiene caras abruptas y las consultas la saltan (mismo resultado).
        readonly float[] minNormalY;
        public TerrainWallGeometry(Heightfield terrain)
        {
            this.terrain=terrain;minNormalY=new float[terrain.Cells*terrain.Cells];
            for(int z=0;z<terrain.Cells;z++)for(int x=0;x<terrain.Cells;x++){
                Triangle(x,z,0,out _,out _,out _,out var a);Triangle(x,z,1,out _,out _,out _,out var b);
                minNormalY[z*terrain.Cells+x]=(float)Math.Min(a.Y,b.Y)-1e-6f;
            }
        }
        // P0-C4: tabla de áreas sumadas de celdas abruptas (minNormalY < wallCos) para un wallCos.
        // Si un rectángulo de celdas no tiene ninguna, Query no encuentra cara (sus bucles saltarían
        // todas) y la huella es transitable: mismo resultado sin recorrer las celdas.
        internal bool UseSteepTable=true;
        double steepCos=double.NaN;int[] steep;
        int SteepCount(int x0,int x1,int z0,int z1,double wallCos)
        {
            var table=steep;
            if(table==null||steepCos!=wallCos){
                int n=terrain.Cells,w=n+1;table=new int[w*w];
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                    table[(z+1)*w+x+1]=(minNormalY[z*n+x]<wallCos?1:0)+table[z*w+x+1]+table[(z+1)*w+x]-table[z*w+x];
                steep=table;steepCos=wallCos;
            }
            int s=terrain.Cells+1;
            return table[(z1+1)*s+x1+1]-table[z0*s+x1+1]-table[(z1+1)*s+x0]+table[z0*s+x0];
        }
        static Vec3 Sub(Vec3 a,Vec3 b)=>new Vec3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        static Vec3 Cross(Vec3 a,Vec3 b)=>new Vec3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        static double Dot(Vec3 a,Vec3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        void Bounds(Vec3 a,Vec3 b,double r,out int x0,out int x1,out int z0,out int z1)
        {
            int Cell(double v)=>Math.Max(0,Math.Min(terrain.Cells-1,(int)Math.Floor((v+terrain.Half)/terrain.CellSize)));
            x0=Cell(Math.Min(a.X,b.X)-r);x1=Cell(Math.Max(a.X,b.X)+r);
            z0=Cell(Math.Min(a.Z,b.Z)-r);z1=Cell(Math.Max(a.Z,b.Z)+r);
        }
        void Triangle(int x,int z,int face,out Vec3 a,out Vec3 b,out Vec3 c,out Vec3 normal)
        {
            double px=x*terrain.CellSize-terrain.Half,pz=z*terrain.CellSize-terrain.Half,s=terrain.CellSize;
            if(face==0){a=new Vec3(px,terrain.VertexHeight(x,z),pz);b=new Vec3(px,terrain.VertexHeight(x,z+1),pz+s);c=new Vec3(px+s,terrain.VertexHeight(x+1,z),pz);}
            else{a=new Vec3(px+s,terrain.VertexHeight(x+1,z+1),pz+s);b=new Vec3(px+s,terrain.VertexHeight(x+1,z),pz);c=new Vec3(px,terrain.VertexHeight(x,z+1),pz+s);}
            normal=Cross(Sub(b,a),Sub(c,a));double length=Math.Sqrt(Dot(normal,normal));
            normal=new Vec3(normal.X/length,normal.Y/length,normal.Z/length);
        }
        // SAT continuo: normal de cara, tres ejes de caja y nueve productos arista/eje.
        // Intervalos abiertos con tolerancia: tocar o desplazarse tangencialmente no bloquea.
        static bool Axis(Vec3 axis,Vec3 a,Vec3 b,Vec3 c,Vec3 center,Vec3 delta,Vec3 extent,ref double enter,ref double exit)
        {
            double norm=Math.Sqrt(Dot(axis,axis));if(norm<1e-10)return true;
            double pa=Dot(axis,a),pb=Dot(axis,b),pc=Dot(axis,c),p=Dot(axis,center),d=Dot(axis,delta);
            double radius=Math.Abs(axis.X)*extent.X+Math.Abs(axis.Y)*extent.Y+Math.Abs(axis.Z)*extent.Z;
            double lo=Math.Min(pa,Math.Min(pb,pc))-radius+1e-8*norm,hi=Math.Max(pa,Math.Max(pb,pc))+radius-1e-8*norm;
            if(Math.Abs(d)<1e-12)return p>lo&&p<hi;
            double t0=(lo-p)/d,t1=(hi-p)/d;if(t0>t1){double t=t0;t0=t1;t1=t;}
            enter=Math.Max(enter,t0);exit=Math.Min(exit,t1);return enter<exit;
        }
        static bool Intersects(Vec3 a,Vec3 b,Vec3 c,Vec3 normal,Vec3 from,Vec3 to,double radius,double height,out double fraction)
        {
            var center=new Vec3(from.X,from.Y+height/2,from.Z);var delta=Sub(to,from);var extent=new Vec3(radius,height/2,radius);
            double enter=double.NegativeInfinity,exit=double.PositiveInfinity;fraction=0;
            if(!Axis(normal,a,b,c,center,delta,extent,ref enter,ref exit)
                ||!Axis(new Vec3(1,0,0),a,b,c,center,delta,extent,ref enter,ref exit)
                ||!Axis(new Vec3(0,1,0),a,b,c,center,delta,extent,ref enter,ref exit)
                ||!Axis(new Vec3(0,0,1),a,b,c,center,delta,extent,ref enter,ref exit))return false;
            for(int i=0;i<3;i++){
                var edge=i==0?Sub(b,a):i==1?Sub(c,b):Sub(a,c);
                if(!Axis(new Vec3(0,edge.Z,-edge.Y),a,b,c,center,delta,extent,ref enter,ref exit)
                    ||!Axis(new Vec3(-edge.Z,0,edge.X),a,b,c,center,delta,extent,ref enter,ref exit)
                    ||!Axis(new Vec3(edge.Y,-edge.X,0),a,b,c,center,delta,extent,ref enter,ref exit))return false;
            }
            fraction=Math.Max(0,enter);return exit>0&&enter<=1;
        }
        public bool Query(Vec3 from,Vec3 to,double radius,double height,double wallCos,bool stationary,out VerticalHit hit)
        {
            hit=new VerticalHit{Id=-1,Fraction=1};bool found=false;
            Bounds(from,to,radius,out int x0,out int x1,out int z0,out int z1);
            if(UseSteepTable&&SteepCount(x0,x1,z0,z1,wallCos)==0)return false;
            var delta=Sub(to,from);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)for(int face=0;face<2;face++){
                if(minNormalY[z*terrain.Cells+x]>=wallCos)break;
                Triangle(x,z,face,out var a,out var b,out var c,out var normal);
                if(normal.Y>=wallCos)continue;
                if(!stationary&&Dot(delta,normal)>=-1e-10)continue;
                if(!Intersects(a,b,c,normal,from,to,radius,height,out double fraction)||found&&fraction>=hit.Fraction)continue;
                int id=2*(z*terrain.Cells+x)+face;
                hit=new VerticalHit{Id=id,Terrain=true,Kind=VerticalSurfaceKind.Cliff,Normal=normal,Fraction=fraction,
                    Point=new Vec3(from.X+delta.X*fraction,from.Y+delta.Y*fraction,from.Z+delta.Z*fraction),TopY=Math.Max(a.Y,Math.Max(b.Y,c.Y))};
                found=true;
            }
            return found;
        }
        // Extremos exactos del terreno lineal a trozos bajo toda la huella: vértices
        // del rectángulo recortado por celda e intersecciones con su diagonal real.
        public bool Support(Vec3 feet,double radius,double tolerance,double wallCos)=>SupportHeight(feet,radius,wallCos,out double y)&&Math.Abs(y-feet.Y)<=tolerance;
        public bool SupportHeight(Vec3 feet,double radius,double wallCos,out double y)
        {
            y=double.NegativeInfinity;
            if(feet.X-radius< -terrain.Half||feet.X+radius>terrain.Half||feet.Z-radius< -terrain.Half||feet.Z+radius>terrain.Half)return false;
            if(radius<=1e-9){y=terrain.HeightAt(feet.X,feet.Z);return terrain.NormalAt(feet.X,feet.Z).Y>=wallCos;}
            Bounds(feet,feet,radius,out int x0,out int x1,out int z0,out int z1);
            double highest=double.NegativeInfinity;
            void At(double x,double z){highest=Math.Max(highest,terrain.HeightAt(x,z));}
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
                double ox=x*terrain.CellSize-terrain.Half,oz=z*terrain.CellSize-terrain.Half,s=terrain.CellSize;
                double left=Math.Max(feet.X-radius,ox),right=Math.Min(feet.X+radius,ox+s),low=Math.Max(feet.Z-radius,oz),high=Math.Min(feet.Z+radius,oz+s);
                if(right-left<1e-9||high-low<1e-9)continue;
                At(left,low);At(left,high);At(right,low);At(right,high);
                double diagonal=ox+oz+s;
                for(int face=0;face<2;face++){
                    bool intersects=face==0?left+low<diagonal-1e-9:right+high>diagonal+1e-9;
                    if(!intersects)continue;
                    Triangle(x,z,face,out _,out _,out _,out var normal);
                    if(normal.Y<wallCos)return false;
                }
                for(int i=0;i<4;i++){
                    double px=i<2?(i==0?left:right):diagonal-(i==2?low:high);
                    double pz=diagonal-px;
                    if(px>=left&&px<=right&&pz>=low&&pz<=high)At(px,pz);
                }
            }
            y=highest;return !double.IsNegativeInfinity(y);
        }
        // Solo la condición de SupportHeight (dentro del mapa y sin cara abrupta bajo la huella), sin
        // calcular la altura: con la tabla, O(1) donde no hay celdas abruptas cerca.
        public bool FootprintWalkable(Vec3 feet,double radius,double wallCos)
        {
            if(feet.X-radius< -terrain.Half||feet.X+radius>terrain.Half||feet.Z-radius< -terrain.Half||feet.Z+radius>terrain.Half)return false;
            if(UseSteepTable&&radius>1e-9){Bounds(feet,feet,radius,out int x0,out int x1,out int z0,out int z1);if(SteepCount(x0,x1,z0,z1,wallCos)==0)return true;}
            return SupportHeight(feet,radius,wallCos,out _);
        }
        public double Height(double x,double z)=>terrain.HeightAt(x,z);
    }
}
