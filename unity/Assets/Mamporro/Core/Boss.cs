using System;

namespace Mamporro.Core
{
    public sealed class Boss
    {
        public readonly uint EnemyId;
        public string Phase="idle",Attack;
        public double Timer,PhaseLength;
        public bool Enraged;
        readonly Rng rng;string last;
        double Speed=>Enraged?1.5:1;
        public Boss(uint id,Rng rng){EnemyId=id;this.rng=rng;Timer=PhaseLength=IdleTime();}
        double IdleTime()=>rng.Range(1.6,2.6)/Speed;
        void Set(string phase,double time){Phase=phase;Timer=PhaseLength=time;}
        void Aim(int i,CombatRun r){double dx=r.Player.X-r.Enemies.X[i],dz=r.Player.Z-r.Enemies.Z[i],d=Rules.Nonzero(Rules.Hypot(dx,dz));r.Enemies.AimX[i]=(float)(dx/d);r.Enemies.AimZ[i]=(float)(dz/d);}
        public bool Step(double dt,CombatRun r)
        {
            var e=r.Enemies;int i=e.IndexOf(EnemyId);if(i<0)return false;Enraged=e.Hp[i]<=e.MaxHp[i]*.5;Timer-=dt;
            switch(Phase){
                case "idle":e.SetState(i,0,0);if(Timer<=0){
                    // Mismo orden del filtro BOSS_ATTACKS y un único sorteo.
                    int selected=rng.Int(0,last==null?2:1);string attack=null;
                    foreach(string a in Attacks)if(a!=last&&selected--==0){attack=a;break;}
                    Attack=last=attack;Aim(i,r);double windup=(attack=="roll"?.9:attack=="slam"?1.3:.8)/Speed;Set("windup",windup);e.SetState(i,1,windup);
                }break;
                case "windup":if(Attack!="roll")Aim(i,r);if(Timer<=0)Execute(i,r);break;
                case "attack":if(Timer<=0)Recover(i,e);break;
                case "recover":if(Timer<=0){Attack=null;Set("idle",IdleTime());e.SetState(i,0,0);}break;
            }
            return true;
        }
        static readonly string[] Attacks={"roll","slam","sneeze"};
        void Execute(int i,CombatRun r)
        {
            var e=r.Enemies;double x=e.X[i],y=e.Y[i],z=e.Z[i];
            if(Attack=="roll"){double time=22.0/18;Set("attack",time);e.SetState(i,2,time);e.DashSpeed[i]=18;e.HitDamage[i]=28;return;}
            if(Attack=="slam"){r.Emit("slam",x,y,z,5.5,life:.5);if(Rules.Hypot(r.Player.X-x,r.Player.Z-z)<=5.5+CombatPlayer.Radius&&Math.Abs(r.Player.Y-y)<3)r.Hurt(32);}
            else if(Attack=="sneeze"){
                int n=Enraged?24:16;double a0=rng.Next()*Math.PI*2,radius=e.Radius(i);
                for(int k=0;k<n;k++){double a=a0+k/(double)n*Math.PI*2,dx=Math.Cos(a),dz=Math.Sin(a),sx=x+dx*radius,sz=z+dz*radius;r.EnemyShots.Spawn(sx,r.World.Height(sx,sz)+1,sz,dx,dz,8,4,.45,damage:10,kind:1);}
                for(int k=0;k<5;k++){double a=a0+(k+.5)/5*Math.PI*2,sx=x+Math.Cos(a)*(radius+1.2),sz=z+Math.Sin(a)*(radius+1.2);r.SpawnMinion(sx,sz);}
            }
            Recover(i,e);
        }
        void Recover(int i,Enemies e){double time=.9/Speed;Set("recover",time);e.SetState(i,3,time);e.HitDamage[i]=(float)e.Def(i).damage;}
    }
}
