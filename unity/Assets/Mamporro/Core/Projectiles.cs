using System;

namespace Mamporro.Core
{
    public interface IProjectileHits { void Hit(int weapon,int enemy,double dx,double dz); }
    public sealed class Projectiles : ITargetFilter
    {
        public int Count;public readonly int Capacity;
        public readonly float[] X,Y,Z,Px,Py,Pz,Vx,Vz,Speed,Life,Radius,Homing,Spin,Damage;
        public readonly int[] Pierce,Weapon,HitCount,Kind;
        readonly uint[] hits; readonly float[][] arrays;readonly int[] scratch=new int[64];
        int current;Enemies enemies;
        public Projectiles(int capacity)
        {
            Capacity=capacity;arrays=new float[14][];for(int k=0;k<14;k++)arrays[k]=new float[capacity];
            X=arrays[0];Y=arrays[1];Z=arrays[2];Px=arrays[3];Py=arrays[4];Pz=arrays[5];Vx=arrays[6];Vz=arrays[7];Speed=arrays[8];Life=arrays[9];Radius=arrays[10];Homing=arrays[11];Spin=arrays[12];Damage=arrays[13];
            Pierce=new int[capacity];Weapon=new int[capacity];HitCount=new int[capacity];Kind=new int[capacity];hits=new uint[capacity*8];
        }
        public int Spawn(double x,double y,double z,double dx,double dz,double speed,double life,double radius,int pierce=0,double homing=0,int weapon=0,double damage=0,int kind=0)
        {
            if(Count>=Capacity)return -1;int i=Count++;double len=Rules.Nonzero(Rules.Hypot(dx,dz));
            X[i]=Px[i]=(float)x;Y[i]=Py[i]=(float)y;Z[i]=Pz[i]=(float)z;Vx[i]=(float)(dx/len*speed);Vz[i]=(float)(dz/len*speed);
            Speed[i]=(float)speed;Life[i]=(float)life;Radius[i]=(float)radius;Pierce[i]=pierce;Homing[i]=(float)homing;Weapon[i]=weapon;HitCount[i]=0;Spin[i]=0;Damage[i]=(float)damage;Kind[i]=kind;return i;
        }
        public void Remove(int i)
        {int last=--Count;if(i==last)return;foreach(var a in arrays)a[i]=a[last];Pierce[i]=Pierce[last];Weapon[i]=Weapon[last];HitCount[i]=HitCount[last];Kind[i]=Kind[last];Array.Copy(hits,last*8,hits,i*8,8);}
        bool Already(int i,uint id){for(int k=0;k<HitCount[i];k++)if(hits[i*8+k]==id)return true;return false;}
        void Remember(int i,uint id){int n=HitCount[i];hits[i*8+n%8]=id;if(n<8)HitCount[i]=n+1;}
        public bool Accept(int i)=>enemies.Accept(i)&&!Already(current,enemies.Id[i]);
        public void Step(double dt,Enemies e,ICombatWorld world,IProjectileHits onHit)
        {
            enemies=e;
            for(int i=Count-1;i>=0;i--) {
                Life[i]=(float)(Life[i]-dt);if(Life[i]<=0){Remove(i);continue;}double x=X[i],z=Z[i];Px[i]=X[i];Py[i]=Y[i];Pz[i]=Z[i];
                if(Homing[i]>0){current=i;int target=e.Grid.Nearest(x,z,14,this);if(target>=0){double tx=e.X[target]-x,tz=e.Z[target]-z,tl=Rules.Nonzero(Rules.Hypot(tx,tz)),speed=Speed[i],k=Math.Min(1,Homing[i]*dt),vx=Vx[i]+(tx/tl*speed-Vx[i])*k,vz=Vz[i]+(tz/tl*speed-Vz[i])*k,vl=Rules.Nonzero(Rules.Hypot(vx,vz));Vx[i]=(float)(vx/vl*speed);Vz[i]=(float)(vz/vl*speed);}}
                double nx=x+Vx[i]*dt,nz=z+Vz[i]*dt;X[i]=(float)nx;Z[i]=(float)nz;Y[i]=(float)(world.Height(nx,nz)+1);Spin[i]=(float)(Spin[i]+dt*18);
                int n=e.Query(nx,nz,Radius[i],scratch);
                for(int k=0;k<n;k++){int j=scratch[k];if(!e.Accept(j)||Already(i,e.Id[j]))continue;double ex=e.X[j]-nx,ez=e.Z[j]-nz,reach=Radius[i]+e.Radius(j)*.9;if(ex*ex+ez*ez>reach*reach)continue;
                    double len=Rules.Nonzero(Rules.Hypot(Vx[i],Vz[i]));onHit.Hit(Weapon[i],j,Vx[i]/len,Vz[i]/len);Remember(i,e.Id[j]);if(--Pierce[i]<0){Remove(i);break;}}
            }
        }
        public double StepHostile(double dt,ICombatWorld world,CombatPlayer p)
        {
            double hit=0;
            for(int i=Count-1;i>=0;i--){Life[i]=(float)(Life[i]-dt);if(Life[i]<=0){Remove(i);continue;}Px[i]=X[i];Py[i]=Y[i];Pz[i]=Z[i];
                double nx=X[i]+Vx[i]*dt,nz=Z[i]+Vz[i]*dt,y=world.Height(nx,nz)+1;X[i]=(float)nx;Z[i]=(float)nz;Y[i]=(float)y;Spin[i]=(float)(Spin[i]+dt*9);
                double dx=nx-p.X,dz=nz-p.Z,reach=Radius[i]+CombatPlayer.Radius;
                if(dx*dx+dz*dz<reach*reach&&Math.Abs(y-1-p.Y)<1.4){hit=Math.Max(hit,Damage[i]);Remove(i);}
            }return hit;
        }
    }
}
