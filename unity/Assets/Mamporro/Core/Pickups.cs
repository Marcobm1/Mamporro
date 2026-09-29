using System;

namespace Mamporro.Core
{
    public sealed class Pickups
    {
        public int Count; public readonly int Capacity;
        public readonly float[] X,Y,Z,Value,Speed,Phase; public readonly bool[] Attracted;
        readonly float[][] arrays;
        public Pickups(int capacity)
        {Capacity=capacity;arrays=new float[6][];for(int k=0;k<6;k++)arrays[k]=new float[capacity];X=arrays[0];Y=arrays[1];Z=arrays[2];Value=arrays[3];Speed=arrays[4];Phase=arrays[5];Attracted=new bool[capacity];}
        public void Spawn(double x,double y,double z,double value)
        {
            if(Count>=Capacity){int best=0;double bestD=double.PositiveInfinity;for(int j=0;j<Count;j++){double dx=X[j]-x,dz=Z[j]-z,d=dx*dx+dz*dz;if(d<bestD){bestD=d;best=j;}}Value[best]=(float)(Value[best]+value);return;}
            int i=Count++;X[i]=(float)x;Y[i]=(float)y;Z[i]=(float)z;Value[i]=(float)value;Speed[i]=0;Phase[i]=(float)((i*1.37)%(Math.PI*2));Attracted[i]=false;
        }
        void Remove(int i) {int last=--Count;if(i==last)return;foreach(var a in arrays)a[i]=a[last];Attracted[i]=Attracted[last];}
        public double Step(double dt,CombatPlayer p,double radius)
        {
            double collected=0;
            for(int i=Count-1;i>=0;i--){Phase[i]=(float)(Phase[i]+dt*3);double dx=p.X-X[i],dy=p.Y+.8-Y[i],dz=p.Z-Z[i],d2=dx*dx+dz*dz;
                if(!Attracted[i]&&d2<radius*radius)Attracted[i]=true;if(!Attracted[i])continue;
                double d=Math.Sqrt(d2+dy*dy);if(d<.9){collected+=Value[i];Remove(i);continue;}
                double speed=Math.Min(32,Speed[i]+70*dt);Speed[i]=(float)speed;double step=Math.Min(d,speed*dt);
                X[i]=(float)(X[i]+dx/d*step);Y[i]=(float)(Y[i]+dy/d*step);Z[i]=(float)(Z[i]+dz/d*step);
            }return collected;
        }
    }
}
