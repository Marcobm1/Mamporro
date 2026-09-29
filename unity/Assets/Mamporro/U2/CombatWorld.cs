using System;
using Mamporro.Core;
using Mamporro.U1;

namespace Mamporro.U2
{
    // Escena técnica U1; no sustituye al mundo procedural reservado a U3.
    public sealed class CombatWorld : ICombatWorld
    {
        public double Height(double x,double z,double maxY=double.PositiveInfinity)=>TechnicalWorld.Height((float)x,(float)z);
        public void Clamp(ref double x,ref double z){x=Math.Max(-47.5,Math.Min(47.5,x));z=Math.Max(-47.5,Math.Min(47.5,z));}
        public bool IsInside(double x,double z,double margin)=>Math.Abs(x)<=47.5-margin&&Math.Abs(z)<=47.5-margin;
        public bool PushOut(ref double x,ref double z,double radius,double y,double step)
        {
            if(y+step>=3||y+1.6<0)return false;bool hit=false;
            foreach(var b in TechnicalWorld.Blocks){
                double minX=b.x-b.z,maxX=b.x+b.z,minZ=b.y-b.w,maxZ=b.y+b.w;
                double qx=Math.Max(minX,Math.Min(maxX,x)),qz=Math.Max(minZ,Math.Min(maxZ,z)),dx=x-qx,dz=z-qz,d2=dx*dx+dz*dz;
                if(d2>=radius*radius)continue;
                if(d2>1e-10){double d=Math.Sqrt(d2),push=radius-d;x+=dx/d*push;z+=dz/d*push;}
                else{double lx=x-b.x,lz=z-b.y,penX=b.z-Math.Abs(lx),penZ=b.w-Math.Abs(lz);if(penX<penZ)x+=(lx>=0?1:-1)*(penX+radius);else z+=(lz>=0?1:-1)*(penZ+radius);}
                hit=true;
            }return hit;
        }
    }
}
