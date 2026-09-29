using System;

namespace Mamporro.Core
{
    public interface ITargetFilter { bool Accept(int index); }
    public sealed class SpatialGrid
    {
        readonly int dim; readonly double half,cellSize;
        readonly int[] start,counts,cellOf,items;
        float[] xs,zs;
        public SpatialGrid(double worldSize,double cellSize,int capacity)
        {
            this.cellSize=cellSize; half=worldSize/2; dim=(int)Math.Ceiling(worldSize/cellSize);
            start=new int[dim*dim+1]; counts=new int[dim*dim]; cellOf=new int[capacity]; items=new int[capacity];
        }
        int Coord(double v) => Math.Max(0,Math.Min(dim-1,(int)Math.Floor((v+half)/cellSize)));
        public void Rebuild(float[] x,float[] z,int count)
        {
            xs=x; zs=z; Array.Clear(counts,0,counts.Length);
            for(int i=0;i<count;i++) {int cell=Coord(z[i])*dim+Coord(x[i]);cellOf[i]=cell;counts[cell]++;}
            int acc=0; for(int c=0;c<counts.Length;c++) {start[c]=acc;acc+=counts[c];counts[c]=0;} start[counts.Length]=acc;
            for(int i=0;i<count;i++) {int cell=cellOf[i];items[start[cell]+counts[cell]++]=i;}
        }
        public int Query(double x,double z,double radius,int[] output)
        {
            if(xs==null) return 0; int n=0; double r2=radius*radius;
            int x0=Coord(x-radius),x1=Coord(x+radius),z0=Coord(z-radius),z1=Coord(z+radius);
            for(int cz=z0;cz<=z1;cz++) for(int cx=x0;cx<=x1;cx++) {
                int cell=cz*dim+cx;
                for(int k=start[cell];k<start[cell+1];k++) {int i=items[k];double dx=xs[i]-x,dz=zs[i]-z;if(dx*dx+dz*dz<=r2) {if(n>=output.Length) return n;output[n++]=i;}}
            }
            return n;
        }
        public int Nearest(double x,double z,double radius,ITargetFilter filter=null)
        {
            if(xs==null) return -1; int cx0=Coord(x),cz0=Coord(z),maxRing=(int)Math.Ceiling(radius/cellSize)+1,best=-1; double bestD2=radius*radius;
            for(int ring=0;ring<=maxRing;ring++) {
                double rd=(ring-1)*cellSize; if(best>=0&&rd>0&&rd*rd>bestD2) break;
                for(int cz=cz0-ring;cz<=cz0+ring;cz++) {
                    if(cz<0||cz>=dim) continue;
                    for(int cx=cx0-ring;cx<=cx0+ring;cx++) {
                        if(cx<0||cx>=dim) continue;
                        if(ring>0&&cz!=cz0-ring&&cz!=cz0+ring&&cx!=cx0-ring&&cx!=cx0+ring) continue;
                        int cell=cz*dim+cx;
                        for(int k=start[cell];k<start[cell+1];k++) {int i=items[k];double dx=xs[i]-x,dz=zs[i]-z,d2=dx*dx+dz*dz;if(d2<bestD2&&(filter==null||filter.Accept(i))) {bestD2=d2;best=i;}}
                    }
                }
            }
            return best;
        }
    }
    public interface ICombatWorld
    {
        double Height(double x,double z,double maxY=double.PositiveInfinity);
        bool PushOut(ref double x,ref double z,double radius,double y,double step);
        void Clamp(ref double x,ref double z);
        bool IsInside(double x,double z,double margin);
    }
    public sealed class CombatPlayer { public double X,Y,Z,Vx,Vz,Facing; public bool Grounded=true; public const double Radius=.45; }
}
