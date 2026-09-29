using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    public sealed class WeaponState
    {
        public bool Active,HasLast;
        public double Clock,Angle,Distance,LastX,LastZ;
        public int Count;public uint Pulse;
        public readonly float[] X=new float[96],Y=new float[96],Z=new float[96],Radius=new float[96],Age=new float[96],Life=new float[96];
        public readonly uint[] Stamp;public readonly double[] NextBite;
        public WeaponState(int capacity){Stamp=new uint[capacity];NextBite=new double[capacity];}
        public void Add(double x,double y,double z,double radius,double life)
        {
            int i=Count;if(i>=96){i=0;for(int k=1;k<Count;k++)if(Age[k]>Age[i])i=k;}else Count++;
            X[i]=(float)x;Y[i]=(float)y;Z[i]=(float)z;Radius[i]=(float)radius;Age[i]=0;Life[i]=(float)life;
        }
        public void Remove(int i)
        {int last=--Count;if(i==last)return;X[i]=X[last];Y[i]=Y[last];Z[i]=Z[last];Radius[i]=Radius[last];Age[i]=Age[last];Life[i]=Life[last];}
    }
    public sealed partial class CombatRun
    {
        readonly List<WeaponState> states=new List<WeaponState>(4);
        readonly int[] range=new int[1024],smallRange=new int[256];
        readonly uint[] chosen=new uint[384];int chosenCount;
        FreshFilter fresh;
        sealed class FreshFilter : ITargetFilter
        {
            readonly CombatRun run; public FreshFilter(CombatRun run){this.run=run;}
            public bool Accept(int i){if(!run.Enemies.Accept(i))return false;uint id=run.Enemies.Id[i];for(int j=0;j<run.chosenCount;j++)if(run.chosen[j]==id)return false;return true;}
        }
        public WeaponState StateOf(int slot)=>states[slot];
        int FreshNearest(double x,double z,double radius)
        {if(fresh==null)fresh=new FreshFilter(this);return Enemies.Grid.Nearest(x,z,radius,fresh);}
        void StepWeapon(Weapon w,WeaponState state,double dt)
        {
            if(w.Def.behavior=="orbit"){StepOrbit(w,state,dt);return;}
            if(w.Def.behavior=="trail")PrepareTrail(w,state,dt);
            w.Timer-=dt;w.SincePulse+=dt;if(w.Timer>0)return;
            double elapsed=w.SincePulse;w.Timer=w[WStat.cooldown];w.SincePulse=0;
            double area=w[WStat.area];int count=Math.Max(1,(int)Rules.Round(w[WStat.count]));
            switch(w.Def.behavior){
                case "homing":
                    chosenCount=0;double firstX=0,firstZ=0;
                    for(int k=0;k<count;k++){
                        int target=FreshNearest(Player.X,Player.Z,28);double dx,dz;
                        if(target>=0){chosen[chosenCount++]=Enemies.Id[target];dx=Enemies.X[target]-Player.X;dz=Enemies.Z[target]-Player.Z;if(k==0){firstX=dx;firstZ=dz;}}
                        else if(k>0){double a=.35*Math.Ceiling(k/2.0)*(k%2==0?1:-1);dx=firstX*Math.Cos(a)-firstZ*Math.Sin(a);dz=firstX*Math.Sin(a)+firstZ*Math.Cos(a);}else break;
                        Projectiles.Spawn(Player.X,Player.Y+1.1,Player.Z,dx,dz,w[WStat.speed],w[WStat.duration],.35*area,(int)Rules.Round(w[WStat.pierce]),7,w.Slot);
                    }
                    if(chosenCount==0){w.Timer=.2;w.SincePulse=elapsed;}break;
                case "aura":
                    RadialWeapon(w,3.2*area,false,count);Emit("aura",Player.X,Player.Y,Player.Z,3.2*area);break;
                case "arc":
                    RadialWeapon(w,3*area,true,count);
                    for(int k=0;k<count;k++)Emit("arc",Player.X,Player.Y+.9,Player.Z,3*area,Player.Facing+k*Math.PI*2/count);break;
                case "chain":
                    chosenCount=0;bool fired=false;int jumps=Math.Max(0,(int)Rules.Round(w[WStat.pierce]));
                    for(int c=0;c<count;c++){
                        int current=FreshNearest(Player.X,Player.Z,16);if(current<0)break;fired=true;double fx=Player.X,fy=Player.Y+1.2,fz=Player.Z;
                        for(int j=0;j<=jumps&&current>=0;j++){
                            double ex=Enemies.X[current],ez=Enemies.Z[current],ey=Enemies.Y[current]+Enemies.Def(current).height*.6,dx=ex-fx,dz=ez-fz,d=Rules.Nonzero(Rules.Hypot(dx,dz));
                            chosen[chosenCount++]=Enemies.Id[current];DamageEnemy(current,w,dx/d,dz/d);Emit("chain",fx,fy,fz,x2:ex,y2:ey,z2:ez);
                            fx=ex;fy=ey;fz=ez;current=j<jumps?FreshNearest(ex,ez,5.5*area):-1;
                        }
                    }
                    if(!fired){w.Timer=.2;w.SincePulse=elapsed;}break;
                case "trail":Soak(w,state);break;
            }
        }
        void RadialWeapon(Weapon w,double radius,bool arc,int swings)
        {
            int n=Enemies.Query(Player.X,Player.Z,radius,range);
            for(int k=0;k<n;k++){
                int i=range[k];if(!Enemies.Accept(i))continue;if(arc&&Math.Abs(Enemies.Y[i]-Player.Y)>1.8)continue;
                double dx=Enemies.X[i]-Player.X,dz=Enemies.Z[i]-Player.Z,d=Rules.Hypot(dx,dz);if(d>radius+Enemies.Radius(i))continue;
                bool hit=!arc||d<.9;
                for(int j=0;j<swings&&!hit;j++){double angle=Player.Facing+j*Math.PI*2/swings;if((dx*-Math.Sin(angle)+dz*-Math.Cos(angle))/Rules.Nonzero(d)>=Math.Cos(75*Math.PI/180))hit=true;}
                if(hit)DamageEnemy(i,w,d>1e-4?dx/d:0,d>1e-4?dz/d:0);
            }
        }
        void StepOrbit(Weapon w,WeaponState s,double dt)
        {
            s.Clock+=dt;w.SincePulse+=dt;w.Timer-=dt;
            if(s.Active){
                s.Angle+=3.2*w[WStat.speed]*dt;
                double orbit=2.3*w[WStat.area],orb=.5*(.5+.5*w[WStat.area]);int count=Math.Max(1,(int)Rules.Round(w[WStat.count]));
                for(int k=0;k<count;k++){
                    double a=s.Angle+k*Math.PI*2/count,ox=Player.X+Math.Cos(a)*orbit,oz=Player.Z+Math.Sin(a)*orbit;
                    int n=Enemies.Query(ox,oz,orb,smallRange);
                    for(int j=0;j<n;j++){
                        int i=smallRange[j];if(!Enemies.Accept(i)||Math.Abs(Enemies.Y[i]-Player.Y)>1.8)continue;
                        if(Rules.Hypot(Enemies.X[i]-ox,Enemies.Z[i]-oz)>orb+Enemies.Radius(i)||s.Clock<s.NextBite[i])continue;
                        s.NextBite[i]=s.Clock+.45;double px=Enemies.X[i]-Player.X,pz=Enemies.Z[i]-Player.Z,d=Rules.Nonzero(Rules.Hypot(px,pz));DamageEnemy(i,w,px/d,pz/d);
                    }
                }
                if(w.Timer<=0){s.Active=false;w.Timer=w[WStat.cooldown];}
            } else if(w.Timer<=0){s.Active=true;w.Timer=w[WStat.duration];w.SincePulse=0;}
        }
        void PrepareTrail(Weapon w,WeaponState s,double dt)
        {
            for(int i=s.Count-1;i>=0;i--){s.Age[i]=(float)(s.Age[i]+dt);if(s.Age[i]>=s.Life[i])s.Remove(i);}
            if(!s.HasLast){s.LastX=Player.X;s.LastZ=Player.Z;s.HasLast=true;}
            double moved=Rules.Hypot(Player.X-s.LastX,Player.Z-s.LastZ);s.LastX=Player.X;s.LastZ=Player.Z;
            if(Player.Grounded&&moved<2){s.Distance+=moved;double radius=1.1*w[WStat.area];
                if(s.Distance>=.9*w[WStat.area]){
                    s.Distance=0;double speed=Rules.Nonzero(Rules.Hypot(Player.Vx,Player.Vz)),sx=-Player.Vz/speed,sz=Player.Vx/speed;int count=Math.Max(1,(int)Rules.Round(w[WStat.count]));
                    for(int k=0;k<count;k++){double offset=(k-(count-1)/2.0)*radius*1.5,x=Player.X+sx*offset,z=Player.Z+sz*offset;s.Add(x,World.Height(x,z,Player.Y+.6),z,radius,w[WStat.duration]);}
                }
            }
        }
        void Soak(Weapon w,WeaponState s)
        {
            if(s.Count==0)return;s.Pulse++;if(s.Pulse>=uint.MaxValue){Array.Clear(s.Stamp,0,s.Stamp.Length);s.Pulse=1;}
            for(int p=0;p<s.Count;p++){
                double x=s.X[p],z=s.Z[p],r=s.Radius[p];int n=Enemies.Query(x,z,r,smallRange);
                for(int k=0;k<n;k++){int i=smallRange[k];if(!Enemies.Accept(i)||s.Stamp[i]==s.Pulse||Math.Abs((double)Enemies.Y[i]-s.Y[p])>1.2)continue;
                    if(Rules.Hypot(Enemies.X[i]-x,Enemies.Z[i]-z)>r+Enemies.Radius(i)*.5)continue;
                    s.Stamp[i]=s.Pulse;DamageEnemy(i,w,0,0);Enemies.ApplySlow(i,.35,.6);
                }
            }
        }
    }
}
