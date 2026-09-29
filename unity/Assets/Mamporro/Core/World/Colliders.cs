using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Colisionadores estáticos (src/world/colliders.ts): cilindros verticales y cajas
    // giradas sobre Y (eje X local = (cos, 0, -sin), eje Z local = (sin, 0, cos)).
    public sealed class Collider
    {
        public bool Box;
        public double X,Z,Bottom,Top,Radius,HalfX,HalfZ,Cos,Sin;
        public bool Standable;
        public const double StandableCircleFraction=0.8;

        public static Collider Circle(double x,double z,double radius,double bottom,double top,bool standable)=>
            new Collider{X=x,Z=z,Radius=radius,Bottom=bottom,Top=top,Standable=standable};
        public static Collider Oriented(double x,double z,double halfX,double halfZ,double rotation,double bottom,double top,bool standable)=>
            new Collider{Box=true,X=x,Z=z,HalfX=halfX,HalfZ=halfZ,Cos=Math.Cos(rotation),Sin=Math.Sin(rotation),Bottom=bottom,Top=top,Standable=standable};

        public double BoundingRadius=>Box?JsMath.Hypot(HalfX,HalfZ):Radius;

        public bool IsOnTop(double x,double z)
        {
            double dx=x-X,dz=z-Z;
            if(!Box){double r=Radius*StandableCircleFraction;return dx*dx+dz*dz<=r*r;}
            double lx=dx*Cos-dz*Sin,lz=dx*Sin+dz*Cos;
            return Math.Abs(lx)<=HalfX&&Math.Abs(lz)<=HalfZ;
        }

        // Si un círculo de radio r en (px, pz) se solapa, devuelve el empujón y la normal hacia fuera.
        public bool PushOut(double px,double pz,double r,out double outDx,out double outDz,out double outNx,out double outNz)
        {
            outDx=outDz=outNx=outNz=0;
            double dx=px-X,dz=pz-Z;
            if(!Box){
                double minDist=Radius+r,d2=dx*dx+dz*dz;
                if(d2>=minDist*minDist)return false;
                double d=Math.Sqrt(d2);
                outNx=d>1e-6?dx/d:1;outNz=d>1e-6?dz/d:0;
                outDx=outNx*(minDist-d);outDz=outNz*(minDist-d);
                return true;
            }
            double lx=dx*Cos-dz*Sin,lz=dx*Sin+dz*Cos;
            double qx=Math.Max(-HalfX,Math.Min(HalfX,lx)),qz=Math.Max(-HalfZ,Math.Min(HalfZ,lz));
            double ex=lx-qx,ez=lz-qz,e2=ex*ex+ez*ez;
            if(e2>=r*r)return false;
            double nlx,nlz,penetration;
            if(e2>1e-10){double d=Math.Sqrt(e2);nlx=ex/d;nlz=ez/d;penetration=r-d;}
            else{
                // El centro está dentro: salimos por el lado más cercano.
                double penX=HalfX-Math.Abs(lx),penZ=HalfZ-Math.Abs(lz);
                if(penX<penZ){nlx=lx>=0?1:-1;nlz=0;penetration=penX+r;}
                else{nlx=0;nlz=lz>=0?1:-1;penetration=penZ+r;}
            }
            outNx=nlx*Cos+nlz*Sin;outNz=-nlx*Sin+nlz*Cos;
            outDx=outNx*penetration;outDz=outNz*penetration;
            return true;
        }
    }

    // Rejilla espacial estática de colisionadores (sin duplicados por consulta).
    public sealed class ColliderGrid
    {
        public readonly List<Collider> Colliders;
        readonly List<int>[] cells;
        readonly int dim;readonly double half,cellSize;
        readonly uint[] stamps;uint stamp;

        public ColliderGrid(List<Collider> colliders,double half,double cellSize=8)
        {
            Colliders=colliders;this.half=half;this.cellSize=cellSize;
            dim=(int)Math.Ceiling(half*2/cellSize);
            cells=new List<int>[dim*dim];for(int k=0;k<cells.Length;k++)cells[k]=new List<int>();
            stamps=new uint[colliders.Count];
            for(int index=0;index<colliders.Count;index++){
                var c=colliders[index];double r=c.BoundingRadius;
                int x0=CellCoord(c.X-r),x1=CellCoord(c.X+r),z0=CellCoord(c.Z-r),z1=CellCoord(c.Z+r);
                for(int cz=z0;cz<=z1;cz++)for(int cx=x0;cx<=x1;cx++)cells[cz*dim+cx].Add(index);
            }
        }

        int CellCoord(double v){int c=(int)Math.Floor((v+half)/cellSize);return c<0?0:c>=dim?dim-1:c;}

        // Índices de los colisionadores cuyas celdas tocan el círculo, en el orden de la web.
        public List<int> Query(double x,double z,double radius,List<int> output)
        {
            output.Clear();
            unchecked{stamp++;}
            if(stamp==0){Array.Clear(stamps,0,stamps.Length);stamp=1;}
            int x0=CellCoord(x-radius),x1=CellCoord(x+radius),z0=CellCoord(z-radius),z1=CellCoord(z+radius);
            for(int cz=z0;cz<=z1;cz++)for(int cx=x0;cx<=x1;cx++)
                foreach(int index in cells[cz*dim+cx])if(stamps[index]!=stamp){stamps[index]=stamp;output.Add(index);}
            return output;
        }
    }
}
