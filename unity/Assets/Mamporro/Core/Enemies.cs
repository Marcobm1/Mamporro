using System;

namespace Mamporro.Core
{
    public interface IEnemyActions { void Shoot(int enemy,double dx,double dz); }
    // Float32 en los mismos límites de almacenamiento que los TypedArray web.
    // Los cálculos intermedios son double, como Number de JavaScript.
    public sealed class Enemies : ITargetFilter
    {
        public readonly int Capacity;
        public int Count,BigCount;
        public double Pressure,PushX,PushZ;
        public readonly float[] X,Y,Z,Px,Py,Pz,Vx,Vz,Kx,Kz,Hp,MaxHp,Xp,Gold,Speed,Flash,Slow,SlowTime,AttackCd,StuckTime,DetourTime,Heading,Phase,StateTime,ActionCd,AimX,AimZ,DashSpeed,HitDamage;
        public readonly int[] Type,State,Detour,Bigs=new int[8];
        public readonly uint[] Id;
        public readonly SpatialGrid Grid;
        readonly float[][] arrays;
        readonly int[] neighbors=new int[64];
        uint nextId=1;
        public Enemies(int capacity,double worldSize)
        {
            Capacity=capacity; arrays=new float[29][]; for(int j=0;j<arrays.Length;j++) arrays[j]=new float[capacity];
            X=arrays[0];Y=arrays[1];Z=arrays[2];Px=arrays[3];Py=arrays[4];Pz=arrays[5];Vx=arrays[6];Vz=arrays[7];Kx=arrays[8];Kz=arrays[9];Hp=arrays[10];MaxHp=arrays[11];Xp=arrays[12];Gold=arrays[13];Speed=arrays[14];Flash=arrays[15];Slow=arrays[16];SlowTime=arrays[17];AttackCd=arrays[18];StuckTime=arrays[19];DetourTime=arrays[20];Heading=arrays[21];Phase=arrays[22];StateTime=arrays[23];ActionCd=arrays[24];AimX=arrays[25];AimZ=arrays[26];DashSpeed=arrays[27];HitDamage=arrays[28];
            Type=new int[capacity];State=new int[capacity];Detour=new int[capacity];Id=new uint[capacity];Grid=new SpatialGrid(worldSize,4,capacity);
        }
        public bool Accept(int i) => i<Count&&Hp[i]>0;
        public EnemyDef Def(int i)=>Catalog.Enemies[Type[i]];
        public double Radius(int i)=>Def(i).radius;
        public int Spawn(int type,double x,double y,double z,double hp=1,double xp=1,double gold=1)
        {
            if(type<0||type>=Catalog.Enemies.Length||Count>=Capacity) return -1;
            var d=Catalog.Enemies[type]; int i=Count++;uint id=nextId++;
            foreach(var a in arrays) a[i]=0;
            double variation=.9+(unchecked(id*2654435761u)%1000)/1000.0*.2;
            X[i]=Px[i]=(float)x;Y[i]=Py[i]=(float)y;Z[i]=Pz[i]=(float)z;
            Hp[i]=MaxHp[i]=(float)(d.hp*hp);Xp[i]=(float)(d.xp*xp);Gold[i]=(float)gold;Speed[i]=(float)(d.speed*variation);
            Phase[i]=(float)((id*.618)%1*Math.PI*2); State[i]=Detour[i]=0;
            ActionCd[i]=(float)(d.charge!=null?d.charge.cooldown*.25:(d.ranged?.cooldown??0)*(.5+(variation-.9)*2.5));
            AimZ[i]=-1;HitDamage[i]=(float)d.damage;Type[i]=type;Id[i]=id;return i;
        }
        public void ApplySlow(int i,double amount,double seconds)
        {
            if(SlowTime[i]>0&&Slow[i]>amount+1e-6)return;
            Slow[i]=(float)Math.Max(Slow[i],amount);SlowTime[i]=(float)Math.Max(SlowTime[i],seconds);
        }
        public void SetState(int i,int state,double time) {State[i]=state;StateTime[i]=(float)time;}
        public void Remove(int i)
        {int last=--Count;if(i==last)return;foreach(var a in arrays)a[i]=a[last];Type[i]=Type[last];State[i]=State[last];Detour[i]=Detour[last];Id[i]=Id[last];}
        public void Clear() {Count=0;nextId=1;Rebuild();}
        public int IndexOf(uint id) {for(int i=0;i<Count;i++)if(Id[i]==id)return i;return -1;}
        public void Rebuild()
        {Grid.Rebuild(X,Z,Count);BigCount=0;for(int i=0;i<Count&&BigCount<8;i++)if(Radius(i)>.9)Bigs[BigCount++]=i;}
        public int Query(double x,double z,double radius,int[] output)
        {
            double pad=radius+.9;int n=Grid.Query(x,z,pad,output);
            for(int k=0;k<BigCount&&n<output.Length;k++){int b=Bigs[k];double d=Rules.Hypot(X[b]-x,Z[b]-z);if(d>pad&&d<=radius+Radius(b))output[n++]=b;}return n;
        }
        void EndDash(int i,EnemyDef d) {SetState(i,3,d.charge?.recover??.6);HitDamage[i]=(float)d.damage;}
        int Think(int i,EnemyDef d,double dist,double tx,double tz,double dy,double dt,IEnemyActions actions)
        {
            int state=State[i]; var r=d.ranged;var c=d.charge;
            if(r!=null) {
                if(state==0) {ActionCd[i]=(float)(ActionCd[i]-dt);if(ActionCd[i]<=0&&dist<=r.range&&dy<3)SetState(i,1,r.windup);}
                else if(state==1) {AimX[i]=(float)tx;AimZ[i]=(float)tz;StateTime[i]=(float)(StateTime[i]-dt);if(StateTime[i]<=0){actions.Shoot(i,tx,tz);SetState(i,0,0);ActionCd[i]=(float)(r.cooldown*(.85+(Id[i]*7%10)*.03));}}
            } else if(c!=null) {
                if(state==0) {ActionCd[i]=(float)(ActionCd[i]-dt);if(ActionCd[i]<=0&&dist<c.range&&dist>4&&dy<2){AimX[i]=(float)tx;AimZ[i]=(float)tz;SetState(i,1,c.windup);}}
                else {StateTime[i]=(float)(StateTime[i]-dt);if(StateTime[i]<=0){if(state==1){SetState(i,2,c.dashTime);DashSpeed[i]=(float)c.dashSpeed;HitDamage[i]=(float)(d.damage*c.damageMultiplier);}else if(state==2)EndDash(i,d);else{SetState(i,0,0);ActionCd[i]=(float)c.cooldown;}}}
            }
            return State[i];
        }
        static void Separate(double ox,double oz,EnemyDef a,EnemyDef b,ref double sx,ref double sz)
        {
            double d2=ox*ox+oz*oz,min=a.radius+b.radius;if(d2>=min*min||d2<1e-8)return;
            double d=Math.Sqrt(d2),push=(min-d)/min*(2*b.mass/(a.mass+b.mass));sx+=ox/d*push;sz+=oz/d*push;
        }
        public double Step(double dt,CombatPlayer p,ICombatWorld world,IEnemyActions actions)
        {
            double decay=Math.Exp(-7*dt),contact=0,pressure=0,pushX=0,pushZ=0,playerSpeed=Rules.Hypot(p.Vx,p.Vz);
            for(int i=0;i<Count;i++) {
                var d=Def(i);double x=X[i],z=Z[i];Px[i]=(float)x;Py[i]=Y[i];Pz[i]=(float)z;
                double dx=p.X-x,dz=p.Z-z,dist=Rules.Hypot(dx,dz);if(dist==0)dist=1e-4;double tx=dx/dist,tz=dz/dist;
                int state=Think(i,d,dist,tx,tz,Math.Abs(p.Y-Y[i]),dt,actions);bool still=state==1||state==3,dash=state==2;
                double dirX=tx,dirZ=tz;
                if(d.ranged!=null) {
                    double pref=d.ranged.preferred;
                    if(dist<pref-1.5){dirX=-tx;dirZ=-tz;}
                    else if(dist<=pref+1.5){double side=(Id[i]&1)==0?1:-1,k=(dist-pref)/1.5*.4;dirX=-tz*side+tx*k;dirZ=tx*side+tz*k;double l=Rules.Nonzero(Rules.Hypot(dirX,dirZ));dirX/=l;dirZ/=l;}
                }
                if(DetourTime[i]>0){DetourTime[i]=(float)(DetourTime[i]-dt);double a=1.1*Detour[i],c=Math.Cos(a),s=Math.Sin(a),rx=dirX*c-dirZ*s;dirZ=dirX*s+dirZ*c;dirX=rx;}
                double sepX=0,sepZ=0;
                if(!dash) {
                    int n=Grid.Query(x,z,d.radius+.9,neighbors);
                    for(int k=0;k<n;k++){int j=neighbors[k];if(j==i||j>=Count)continue;var other=Def(j);if(other.radius>.9)continue;Separate(x-X[j],z-Z[j],d,other,ref sepX,ref sepZ);}
                    for(int k=0;k<BigCount;k++){int j=Bigs[k];if(j==i||j>=Count)continue;var other=Def(j);if(d.radius>.9&&other.mass<d.mass)continue;Separate(x-X[j],z-Z[j],d,other,ref sepX,ref sepZ);}
                }
                double speed=Speed[i];if(SlowTime[i]>0){speed*=1-Slow[i];SlowTime[i]=(float)(SlowTime[i]-dt);if(SlowTime[i]<=0)Slow[i]=0;}
                if(dash){Vx[i]=(float)((double)AimX[i]*DashSpeed[i]);Vz[i]=(float)((double)AimZ[i]*DashSpeed[i]);}
                else{double desiredX=(still?0:dirX*speed)+sepX*4,desiredZ=(still?0:dirZ*speed)+sepZ*4,a=Math.Min(1,d.agility*(still?2:1)*dt);Vx[i]=(float)(Vx[i]+(desiredX-Vx[i])*a);Vz[i]=(float)(Vz[i]+(desiredZ-Vz[i])*a);}
                double kx=Kx[i]*decay,kz=Kz[i]*decay;Kx[i]=(float)kx;Kz[i]=(float)kz;
                double nx=x+(Vx[i]+kx)*dt,nz=z+(Vz[i]+kz)*dt;
                if(world.PushOut(ref nx,ref nz,d.radius,Y[i],.6)) {
                    if(dash&&d.charge!=null)EndDash(i,d);
                    StuckTime[i]=(float)(StuckTime[i]+dt);
                    if(StuckTime[i]>.35&&DetourTime[i]<=0){Detour[i]=(Id[i]&1)==0?1:-1;DetourTime[i]=1.4f;StuckTime[i]=0;}
                } else StuckTime[i]=(float)Math.Max(0,StuckTime[i]-dt);
                world.Clamp(ref nx,ref nz);
                double ex=nx-p.X,ez=nz-p.Z,ed=Rules.Hypot(ex,ez),minD=d.radius+CombatPlayer.Radius;
                if(ed<minD&&Math.Abs(p.Y-Y[i])<1.6+(d.height>3?d.height*.5:0)) {
                    double k=ed>1e-4?minD/ed:0;
                    if(d.mass>=20){double overlap=minD-ed;pushX-=(ed>1e-4?ex/ed:0)*overlap;pushZ-=(ed>1e-4?ez/ed:-1)*overlap;}
                    else{nx=p.X+ex*k;nz=p.Z+(ed>1e-4?ez*k:minD);}
                    if(playerSpeed>.1&&ed>1e-4){double ahead=(ex*p.Vx+ez*p.Vz)/(ed*playerSpeed);if(ahead>0)pressure+=ahead*d.mass;}
                    if(AttackCd[i]<=0){contact=Math.Max(contact,HitDamage[i]);AttackCd[i]=.9f;}
                }
                AttackCd[i]=(float)Math.Max(0,AttackCd[i]-dt);Flash[i]=(float)Math.Max(0,Flash[i]-dt*6);
                X[i]=(float)nx;Z[i]=(float)nz;Y[i]=(float)world.Height(nx,nz,Y[i]+.6);
                if(still||dash)Heading[i]=(float)Math.Atan2(-AimX[i],-AimZ[i]);
                else if((double)Vx[i]*Vx[i]+(double)Vz[i]*Vz[i]>.04)Heading[i]=(float)Math.Atan2(-Vx[i],-Vz[i]);
                Phase[i]=(float)(Phase[i]+dt*(4+speed*1.2));
            }
            Rebuild();Pressure=pressure;PushX=pushX;PushZ=pushZ;return contact;
        }
    }
}
