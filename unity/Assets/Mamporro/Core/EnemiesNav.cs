using System;

namespace Mamporro.Core
{
    // P0-B: navegación vertical optativa de la horda. Nula en U3/U6: Enemies.Step conserva
    // literalmente el camino heredado (réplica web) cuando no hay navegación asignada.
    public interface IEnemyNavigation
    {
        void Begin(Enemies e,CombatPlayer p,double dt);
        void End();
        void Direction(int i,CombatPlayer p,out double dirX,out double dirZ,out bool hold);
        bool Stable(int i);
        bool LineOfSight(int i,CombatPlayer p);
        bool StraightWalk(int i,double tx,double tz,double dist);
        bool Move(int i,double vx,double vz,double kx,double kz,double speed,bool dash,bool still,double dt);
        bool TryPlace(int i,double x,double z);
        bool SeparationOverlap(int i,int j);
        void Spawned(Enemies e,int i);
        void Moved(int from,int to);
        void Cleared();
    }
    // P0-B5: combate geométricamente coherente en altura. Nulo en U3/U6.
    public interface IVerticalCombat
    {
        bool Stable(int enemy);
        bool SegmentBlocked(double x0,double y0,double z0,double x1,double y1,double z1,double radius);
        bool SpawnPoint(ref double x,ref double z,double nearY,out double y);
    }

    public sealed partial class Enemies
    {
        public IEnemyNavigation Navigation;
        // Igual que Think salvo dos condiciones de geometría: disparar exige estar estable (no
        // agarrado ni en el borde) y línea de visión real; cargar exige estar estable y una línea
        // recta transitable. Distancias, ventanas, cadencias y daño son los del catálogo.
        int ThinkNavigated(int i,EnemyDef d,double dist,double tx,double tz,double dy,double dt,IEnemyActions actions,bool stable,CombatPlayer p)
        {
            int state=State[i];var r=d.ranged;var c=d.charge;var nav=Navigation;
            if(r!=null){
                if(state==0){ActionCd[i]=(float)(ActionCd[i]-dt);if(ActionCd[i]<=0&&dist<=r.range&&stable&&nav.LineOfSight(i,p))SetState(i,1,r.windup);}
                else if(state==1){
                    // Un empuje que lo saca de la superficie durante el aviso cancela el disparo.
                    if(!stable){SetState(i,0,0);ActionCd[i]=0;}
                    else{AimX[i]=(float)tx;AimZ[i]=(float)tz;StateTime[i]=(float)(StateTime[i]-dt);if(StateTime[i]<=0){actions.Shoot(i,tx,tz);SetState(i,0,0);ActionCd[i]=(float)(r.cooldown*(.85+(Id[i]*7%10)*.03));}}
                }
            } else if(c!=null){
                if(state==0){ActionCd[i]=(float)(ActionCd[i]-dt);if(ActionCd[i]<=0&&dist<c.range&&dist>4&&dy<2&&stable&&nav.StraightWalk(i,tx,tz,dist)){AimX[i]=(float)tx;AimZ[i]=(float)tz;SetState(i,1,c.windup);}}
                else {StateTime[i]=(float)(StateTime[i]-dt);if(StateTime[i]<=0){if(state==1){SetState(i,2,c.dashTime);DashSpeed[i]=(float)c.dashSpeed;HitDamage[i]=(float)(d.damage*c.damageMultiplier);}else if(state==2)EndDash(i,d);else{SetState(i,0,0);ActionCd[i]=(float)c.cooldown;}}}
            }
            return State[i];
        }
        // Mismo orden y fórmulas que Step; cambian la dirección (campo compartido), el movimiento
        // (grafo, escalada y caída físicas), la separación (solo con solape vertical) y el
        // contacto (solo estable). Sin teletransporte ni penetración.
        double StepNavigated(double dt,CombatPlayer p,ICombatWorld world,IEnemyActions actions)
        {
            var nav=Navigation;nav.Begin(this,p,dt);
            double decay=Math.Exp(-7*dt),contact=0,pressure=0,pushX=0,pushZ=0,playerSpeed=Rules.Hypot(p.Vx,p.Vz);
            for(int i=0;i<Count;i++){
                var d=Def(i);double x=X[i],z=Z[i];Px[i]=(float)x;Py[i]=Y[i];Pz[i]=(float)z;
                double dx=p.X-x,dz=p.Z-z,dist=Rules.Hypot(dx,dz);if(dist==0)dist=1e-4;double tx=dx/dist,tz=dz/dist;
                bool stable=nav.Stable(i);
                int state=ThinkNavigated(i,d,dist,tx,tz,Math.Abs(p.Y-Y[i]),dt,actions,stable,p);bool still=state==1||state==3,dash=state==2;
                nav.Direction(i,p,out double dirX,out double dirZ,out bool hold);
                if(d.ranged!=null&&stable&&!hold&&nav.LineOfSight(i,p)){
                    double pref=d.ranged.preferred;
                    if(dist<pref-1.5){dirX=-tx;dirZ=-tz;}
                    else if(dist<=pref+1.5){double side=(Id[i]&1)==0?1:-1,k=(dist-pref)/1.5*.4;dirX=-tz*side+tx*k;dirZ=tx*side+tz*k;double l=Rules.Nonzero(Rules.Hypot(dirX,dirZ));dirX/=l;dirZ/=l;}
                }
                if(DetourTime[i]>0){DetourTime[i]=(float)(DetourTime[i]-dt);double a=1.1*Detour[i],c=Math.Cos(a),s=Math.Sin(a),rx=dirX*c-dirZ*s;dirZ=dirX*s+dirZ*c;dirX=rx;}
                double sepX=0,sepZ=0;
                if(!dash&&stable){
                    int n=Grid.Query(x,z,d.radius+.9,neighbors);
                    for(int k=0;k<n;k++){int j=neighbors[k];if(j==i||j>=Count)continue;var other=Def(j);if(other.radius>.9||!nav.SeparationOverlap(i,j))continue;Separate(x-X[j],z-Z[j],d,other,ref sepX,ref sepZ);}
                    for(int k=0;k<BigCount;k++){int j=Bigs[k];if(j==i||j>=Count)continue;var other=Def(j);if(d.radius>.9&&other.mass<d.mass||!nav.SeparationOverlap(i,j))continue;Separate(x-X[j],z-Z[j],d,other,ref sepX,ref sepZ);}
                }
                double speed=Speed[i];if(SlowTime[i]>0){speed*=1-Slow[i];SlowTime[i]=(float)(SlowTime[i]-dt);if(SlowTime[i]<=0)Slow[i]=0;}
                bool idle=still||hold;
                if(dash){Vx[i]=(float)((double)AimX[i]*DashSpeed[i]);Vz[i]=(float)((double)AimZ[i]*DashSpeed[i]);}
                else{double desiredX=(idle?0:dirX*speed)+sepX*4,desiredZ=(idle?0:dirZ*speed)+sepZ*4,a=Math.Min(1,d.agility*(still?2:1)*dt);Vx[i]=(float)(Vx[i]+(desiredX-Vx[i])*a);Vz[i]=(float)(Vz[i]+(desiredZ-Vz[i])*a);}
                double kx=Kx[i]*decay,kz=Kz[i]*decay;Kx[i]=(float)kx;Kz[i]=(float)kz;
                // Escalar y caer usan la velocidad efectiva propia (Speed con ralentización).
                if(nav.Move(i,Vx[i],Vz[i],kx,kz,speed,dash,idle,dt)){
                    if(dash&&d.charge!=null)EndDash(i,d);
                    StuckTime[i]=(float)(StuckTime[i]+dt);
                    if(StuckTime[i]>.35&&DetourTime[i]<=0){Detour[i]=(Id[i]&1)==0?1:-1;DetourTime[i]=1.4f;StuckTime[i]=0;}
                } else StuckTime[i]=(float)Math.Max(0,StuckTime[i]-dt);
                double nx=X[i],nz=Z[i],ex=nx-p.X,ez=nz-p.Z,ed=Rules.Hypot(ex,ez),minD=d.radius+CombatPlayer.Radius;
                if(nav.Stable(i)&&ed<minD&&Math.Abs(p.Y-Y[i])<1.6+(d.height>3?d.height*.5:0)) {
                    double k=ed>1e-4?minD/ed:0;
                    if(d.mass>=20){double overlap=minD-ed;pushX-=(ed>1e-4?ex/ed:0)*overlap;pushZ-=(ed>1e-4?ez/ed:-1)*overlap;}
                    else nav.TryPlace(i,p.X+ex*k,p.Z+(ed>1e-4?ez*k:minD));
                    if(playerSpeed>.1&&ed>1e-4){double ahead=(ex*p.Vx+ez*p.Vz)/(ed*playerSpeed);if(ahead>0)pressure+=ahead*d.mass;}
                    if(AttackCd[i]<=0){contact=Math.Max(contact,HitDamage[i]);AttackCd[i]=.9f;}
                }
                AttackCd[i]=(float)Math.Max(0,AttackCd[i]-dt);Flash[i]=(float)Math.Max(0,Flash[i]-dt*6);
                if(still||dash)Heading[i]=(float)Math.Atan2(-AimX[i],-AimZ[i]);
                else if((double)Vx[i]*Vx[i]+(double)Vz[i]*Vz[i]>.04)Heading[i]=(float)Math.Atan2(-Vx[i],-Vz[i]);
                Phase[i]=(float)(Phase[i]+dt*(4+speed*1.2));
            }
            Rebuild();Pressure=pressure;PushX=pushX;PushZ=pushZ;nav.End();return contact;
        }
    }
}
