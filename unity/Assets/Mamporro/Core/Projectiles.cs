using System;

namespace Mamporro.Core
{
    public interface IProjectileHits { void Hit(int weapon,int enemy,double dx,double dz); }
    public sealed class Projectiles : ITargetFilter
    {
        public int Count;public readonly int Capacity;
        public readonly float[] X,Y,Z,Px,Py,Pz,Vx,Vz,Speed,Life,Radius,Homing,Spin,Damage,Vy;
        public readonly int[] Pierce,Weapon,HitCount,Kind;
        readonly uint[] hits; readonly float[][] arrays;readonly int[] scratch=new int[64];
        int current;Enemies enemies;
        // P0-B5: geometría vertical (nula en U3/U6).
        public IVerticalCombat Vertical;
        public Projectiles(int capacity)
        {
            Capacity=capacity;arrays=new float[15][];for(int k=0;k<15;k++)arrays[k]=new float[capacity];
            X=arrays[0];Y=arrays[1];Z=arrays[2];Px=arrays[3];Py=arrays[4];Pz=arrays[5];Vx=arrays[6];Vz=arrays[7];Speed=arrays[8];Life=arrays[9];Radius=arrays[10];Homing=arrays[11];Spin=arrays[12];Damage=arrays[13];Vy=arrays[14];
            Pierce=new int[capacity];Weapon=new int[capacity];HitCount=new int[capacity];Kind=new int[capacity];hits=new uint[capacity*8];
        }
        public int Spawn(double x,double y,double z,double dx,double dz,double speed,double life,double radius,int pierce=0,double homing=0,int weapon=0,double damage=0,int kind=0)
        {
            if(Count>=Capacity)return -1;int i=Count++;double len=Rules.Nonzero(Rules.Hypot(dx,dz));
            X[i]=Px[i]=(float)x;Y[i]=Py[i]=(float)y;Z[i]=Pz[i]=(float)z;Vx[i]=(float)(dx/len*speed);Vz[i]=(float)(dz/len*speed);
            Speed[i]=(float)speed;Life[i]=(float)life;Radius[i]=(float)radius;Pierce[i]=pierce;Homing[i]=(float)homing;Weapon[i]=weapon;HitCount[i]=0;Spin[i]=0;Damage[i]=(float)damage;Kind[i]=kind;Vy[i]=0;return i;
        }
        public void Remove(int i)
        {int last=--Count;if(i==last)return;foreach(var a in arrays)a[i]=a[last];Pierce[i]=Pierce[last];Weapon[i]=Weapon[last];HitCount[i]=HitCount[last];Kind[i]=Kind[last];Array.Copy(hits,last*8,hits,i*8,8);}
        bool Already(int i,uint id){for(int k=0;k<HitCount[i];k++)if(hits[i*8+k]==id)return true;return false;}
        void Remember(int i,uint id){int n=HitCount[i];hits[i*8+n%8]=id;if(n<8)HitCount[i]=n+1;}
        // Altura de referencia de un proyectil amistoso: sale a jugador+1,1.
        const double FriendlyLift=1.1,Band=1.8;
        // P0-B5: misma banda vertical que Barra y las dentaduras (|dy| ≤ 1,8) respecto al nivel del disparo.
        bool InBand(int shot,int enemy)=>Vertical==null||Math.Abs(enemies.Y[enemy]-(Y[shot]-FriendlyLift))<=Band;
        public bool Accept(int i)=>enemies.Accept(i)&&!Already(current,enemies.Id[i])&&InBand(current,i);
        public void Step(double dt,Enemies e,ICombatWorld world,IProjectileHits onHit)
        {
            enemies=e;
            for(int i=Count-1;i>=0;i--) {
                Life[i]=(float)(Life[i]-dt);if(Life[i]<=0){Remove(i);continue;}double x=X[i],z=Z[i];Px[i]=X[i];Py[i]=Y[i];Pz[i]=Z[i];
                if(Homing[i]>0){current=i;int target=e.Grid.Nearest(x,z,14,this);if(target>=0){double tx=e.X[target]-x,tz=e.Z[target]-z,tl=Rules.Nonzero(Rules.Hypot(tx,tz)),speed=Speed[i],k=Math.Min(1,Homing[i]*dt),vx=Vx[i]+(tx/tl*speed-Vx[i])*k,vz=Vz[i]+(tz/tl*speed-Vz[i])*k,vl=Rules.Nonzero(Rules.Hypot(vx,vz));Vx[i]=(float)(vx/vl*speed);Vz[i]=(float)(vz/vl*speed);}}
                double nx=x+Vx[i]*dt,nz=z+Vz[i]*dt;
                if(Vertical!=null){
                    // P0-B5: vuela a su altura de salida y se detiene contra sólidos y contra el suelo.
                    double y=Y[i];if(Vertical.SegmentBlocked(x,y,z,nx,y,nz,Radius[i])||y<world.Height(nx,nz)){Remove(i);continue;}
                    X[i]=(float)nx;Z[i]=(float)nz;
                } else {X[i]=(float)nx;Z[i]=(float)nz;Y[i]=(float)(world.Height(nx,nz)+1);}
                Spin[i]=(float)(Spin[i]+dt*18);
                int n=e.Query(nx,nz,Radius[i],scratch);
                for(int k=0;k<n;k++){int j=scratch[k];if(!e.Accept(j)||Already(i,e.Id[j])||!InBand(i,j))continue;double ex=e.X[j]-nx,ez=e.Z[j]-nz,reach=Radius[i]+e.Radius(j)*.9;if(ex*ex+ez*ez>reach*reach)continue;
                    double len=Rules.Nonzero(Rules.Hypot(Vx[i],Vz[i]));onHit.Hit(Weapon[i],j,Vx[i]/len,Vz[i]/len);Remember(i,e.Id[j]);if(--Pierce[i]<0){Remove(i);break;}}
            }
        }
        public double StepHostile(double dt,ICombatWorld world,CombatPlayer p)
        {
            if(Vertical!=null)return StepHostile3D(dt,world,p);
            double hit=0;
            for(int i=Count-1;i>=0;i--){Life[i]=(float)(Life[i]-dt);if(Life[i]<=0){Remove(i);continue;}Px[i]=X[i];Py[i]=Y[i];Pz[i]=Z[i];
                double nx=X[i]+Vx[i]*dt,nz=Z[i]+Vz[i]*dt,y=world.Height(nx,nz)+1;X[i]=(float)nx;Z[i]=(float)nz;Y[i]=(float)y;Spin[i]=(float)(Spin[i]+dt*9);
                double dx=nx-p.X,dz=nz-p.Z,reach=Radius[i]+CombatPlayer.Radius;
                if(dx*dx+dz*dz<reach*reach&&Math.Abs(y-1-p.Y)<1.4){hit=Math.Max(hit,Damage[i]);Remove(i);}
            }return hit;
        }
        // P0-B5: línea recta 3D desde la altura real del tirador hacia el jugador en el instante del
        // disparo (sin seguimiento posterior); se detiene en sólidos y suelo. Misma tolerancia de
        // impacto que el heredado (|y-1-jugador| < 1,4), velocidad, vida, radio y daño iguales.
        double StepHostile3D(double dt,ICombatWorld world,CombatPlayer p)
        {
            double hit=0;
            for(int i=Count-1;i>=0;i--){Life[i]=(float)(Life[i]-dt);if(Life[i]<=0){Remove(i);continue;}Px[i]=X[i];Py[i]=Y[i];Pz[i]=Z[i];
                double x=X[i],y=Y[i],z=Z[i],nx=x+Vx[i]*dt,ny=y+Vy[i]*dt,nz=z+Vz[i]*dt;
                if(Vertical.SegmentBlocked(x,y,z,nx,ny,nz,Radius[i])||ny<world.Height(nx,nz)){Remove(i);continue;}
                X[i]=(float)nx;Y[i]=(float)ny;Z[i]=(float)nz;Spin[i]=(float)(Spin[i]+dt*9);
                double dx=nx-p.X,dz=nz-p.Z,reach=Radius[i]+CombatPlayer.Radius;
                if(dx*dx+dz*dz<reach*reach&&Math.Abs(ny-1-p.Y)<1.4){hit=Math.Max(hit,Damage[i]);Remove(i);}
            }return hit;
        }
        // Velocidad 3D de módulo speed hacia (tx,ty,tz); el heredado solo usa el plano XZ.
        public void Aim3D(int i,double tx,double ty,double tz,double speed)
        {
            double dx=tx-X[i],dy=ty-Y[i],dz=tz-Z[i],l=Math.Sqrt(dx*dx+dy*dy+dz*dz);if(l<1e-9)return;
            Vx[i]=(float)(dx/l*speed);Vy[i]=(float)(dy/l*speed);Vz[i]=(float)(dz/l*speed);
        }
    }
}
