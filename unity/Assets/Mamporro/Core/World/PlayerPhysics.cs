using System;

namespace Mamporro.Core
{
    // Física del jugador de la web (src/entities/playerPhysics.ts y src/systems/crowd.ts),
    // lógica pura: aceleración rápida, control aéreo, salto con margen (coyote y buffer),
    // deslizamiento con impulso que acelera cuesta abajo, escalones y pendientes.
    public struct PlayerIntent
    {
        public double MoveX,MoveZ;
        public bool JumpPressed,JumpHeld,SlidePressed,SlideHeld;
    }

    public interface IPhysicsWorld
    {
        double GroundHeight(double x,double z,double maxY);
        Vec3 GroundNormal(double x,double z,double maxY);
        void ResolveObstacles(PlayerBody body,double radius,double stepHeight);
        void Constrain(PlayerBody body);
    }

    public sealed class PlayerTuning
    {
        public double Radius=Tuning.PlayerRadius,Height=Tuning.PlayerHeight,GroundAccel=Tuning.PlayerGroundAccel,GroundDecel=Tuning.PlayerGroundDecel,
            AirAccel=Tuning.PlayerAirAccel,OverspeedDecel=Tuning.PlayerOverspeedDecel,Gravity=Tuning.PlayerGravity,FallGravityMultiplier=Tuning.PlayerFallGravityMultiplier,
            LowJumpGravityMultiplier=Tuning.PlayerLowJumpGravityMultiplier,MaxFallSpeed=Tuning.PlayerMaxFallSpeed,JumpVelocity=Tuning.PlayerJumpVelocity,
            CoyoteTime=Tuning.PlayerCoyoteTime,JumpBuffer=Tuning.PlayerJumpBuffer,StepHeight=Tuning.PlayerStepHeight,MaxSlopeDeg=Tuning.PlayerMaxSlopeDeg;
        public double SlideEntrySpeedMultiplier=Tuning.PlayerSlideEntrySpeedMultiplier,SlideBoost=Tuning.PlayerSlideBoost,SlideMinSpeed=Tuning.PlayerSlideMinSpeed,
            SlideFriction=Tuning.PlayerSlideFriction,SlideBrakeFriction=Tuning.PlayerSlideBrakeFriction,SlideGravityScale=Tuning.PlayerSlideGravityScale,
            SlideTurnRate=Tuning.PlayerSlideTurnRate,SlideCooldown=Tuning.PlayerSlideCooldown,SlideMaxSpeed=Tuning.PlayerSlideMaxSpeed,
            SlideJumpBoost=Tuning.PlayerSlideJumpBoost,SlideBoostSpeedCap=Tuning.PlayerSlideBoostSpeedCap;
        public static readonly PlayerTuning Default=new PlayerTuning();
    }

    public sealed class PlayerBody
    {
        public double X,Y,Z,Vx,Vy,Vz;
        public bool Grounded,OnSteep;
        // Memoria corta de contacto con pendiente empinada y tiempo atascado.
        public double SteepContact,StuckTime,StuckRefY;
        public Vec3 Normal=new Vec3(0,1,0);
        public double Coyote,JumpBuffer,SlideCooldown;
        public bool Sliding,SlideQueued;
        // Orientación del modelo (rad); 0 = mirando hacia -Z.
        public double Facing;
        public bool Jumped,Landed,SlideBoosted;
        public double LandingSpeed;

        public double HorizontalSpeed=>JsMath.Hypot(Vx,Vz);

        public void PlaceAt(double x,double y,double z)
        {
            X=x;Y=y;Z=z;Vx=Vy=Vz=0;Grounded=true;OnSteep=false;SteepContact=0;StuckTime=0;StuckRefY=y;
            Sliding=false;SlideQueued=false;Coyote=0;JumpBuffer=0;
        }
    }

    public static class PlayerPhysics
    {
        const double SteepMemory=0.15,StuckTime=0.35,StuckDrop=0.5,StuckJumpPush=5;
        const double Tau=Math.PI*2;

        public static double WrapAngle(double a){a=(a+Math.PI)%Tau;if(a<0)a+=Tau;return a-Math.PI;}
        public static double LerpAngle(double a,double b,double t)=>a+WrapAngle(b-a)*t;
        public static double Damp(double lambda,double dt)=>1-Math.Exp(-lambda*dt);

        static void Approach(PlayerBody b,double tx,double tz,double maxDelta)
        {
            double dx=tx-b.Vx,dz=tz-b.Vz,d=JsMath.Hypot(dx,dz);
            if(d<=maxDelta||d<1e-6){b.Vx=tx;b.Vz=tz;}
            else{b.Vx+=(dx/d)*maxDelta;b.Vz+=(dz/d)*maxDelta;}
        }

        static void Steer(PlayerBody b,double dirX,double dirZ,double maxAngle)
        {
            double speed=JsMath.Hypot(b.Vx,b.Vz);
            if(speed<1e-4)return;
            double current=Math.Atan2(b.Vz,b.Vx);
            double diff=WorldMath.Clamp(WrapAngle(Math.Atan2(dirZ,dirX)-current),-maxAngle,maxAngle);
            double angle=current+diff;
            b.Vx=Math.Cos(angle)*speed;b.Vz=Math.Sin(angle)*speed;
        }

        static void ScaleHorizontal(PlayerBody b,double newSpeed)
        {
            double speed=JsMath.Hypot(b.Vx,b.Vz);
            if(speed<1e-6)return;
            double k=Math.Max(0,newSpeed)/speed;b.Vx*=k;b.Vz*=k;
        }

        // Frena hacia limit como mucho maxDelta sin cambiar la dirección; nunca acelera.
        public static void BrakeHorizontal(PlayerBody b,double limit,double maxDelta)
        {
            double speed=JsMath.Hypot(b.Vx,b.Vz);
            if(speed<=limit)return;
            ScaleHorizontal(b,Math.Max(limit,speed-Math.Max(0,maxDelta)));
        }

        static void StartSlide(PlayerBody b,double dirX,double dirZ,double wishLen,double moveSpeed,PlayerTuning t)
        {
            double speed=JsMath.Hypot(b.Vx,b.Vz),dx,dz;
            if(speed>0.5){dx=b.Vx/speed;dz=b.Vz/speed;}
            else if(wishLen>0.1){dx=dirX;dz=dirZ;}
            else return;
            double newSpeed=speed;
            if(b.SlideCooldown<=0){
                double cap=moveSpeed*t.SlideBoostSpeedCap;
                newSpeed=Math.Max(Math.Max(speed,Math.Min(speed+t.SlideBoost,cap)),moveSpeed*t.SlideEntrySpeedMultiplier);
                b.SlideCooldown=t.SlideCooldown;b.SlideBoosted=true;
            }
            b.Vx=dx*newSpeed;b.Vz=dz*newSpeed;b.Sliding=true;
        }

        static bool Blocked(PlayerBody b,IPhysicsWorld world,PlayerTuning t,double cosMaxSlope,double x,double z)
        {
            double maxY=b.Y+t.StepHeight,g=world.GroundHeight(x,z,maxY);
            if(g>maxY)return true;
            if(g>b.Y+0.02&&world.GroundNormal(x,z,maxY).Y<cosMaxSlope)return true;
            return false;
        }

        // stepPlayer: avanza la física un paso de dt segundos.
        public static void Step(PlayerBody b,PlayerIntent intent,IPhysicsWorld world,PlayerTuning t,double moveSpeed,double dt)
        {
            b.Jumped=false;b.Landed=false;b.LandingSpeed=0;b.SlideBoosted=false;
            double cosMaxSlope=Math.Cos(t.MaxSlopeDeg*WorldMath.Deg2Rad);

            double wishLen=JsMath.Hypot(intent.MoveX,intent.MoveZ);
            double dirX=wishLen>1e-4?intent.MoveX/wishLen:0,dirZ=wishLen>1e-4?intent.MoveZ/wishLen:0;
            if(wishLen>1)wishLen=1;

            b.SlideCooldown=Math.Max(0,b.SlideCooldown-dt);
            b.JumpBuffer=intent.JumpPressed?t.JumpBuffer:Math.Max(0,b.JumpBuffer-dt);
            b.Coyote=b.Grounded?t.CoyoteTime:Math.Max(0,b.Coyote-dt);
            // Válvula de seguridad: atascado entre pendientes empinadas sin bajar → se permite saltar.
            b.SteepContact=b.OnSteep?SteepMemory:Math.Max(0,b.SteepContact-dt);
            if(b.Grounded||b.SteepContact<=0){b.StuckTime=0;b.StuckRefY=b.Y;}
            else{b.StuckTime+=dt;if(b.Y<b.StuckRefY-StuckDrop){b.StuckTime=0;b.StuckRefY=b.Y;}}
            bool stuck=b.StuckTime>StuckTime;

            if(!intent.SlideHeld){b.SlideQueued=false;b.Sliding=false;}
            if(b.Grounded){
                if(!b.Sliding&&intent.SlideHeld&&(intent.SlidePressed||b.SlideQueued))StartSlide(b,dirX,dirZ,wishLen,moveSpeed,t);
                b.SlideQueued=false;
            }else if(b.Sliding){b.Sliding=false;b.SlideQueued=intent.SlideHeld;}
            else if(intent.SlidePressed)b.SlideQueued=true;

            var n=b.Normal;double speed=JsMath.Hypot(b.Vx,b.Vz);
            if(b.Grounded){
                if(b.Sliding){
                    bool braking=wishLen>0.1&&speed>0.1&&(dirX*b.Vx+dirZ*b.Vz)/speed< -0.5;
                    ScaleHorizontal(b,speed-(braking?t.SlideBrakeFriction:t.SlideFriction)*dt);
                    double g=t.Gravity*t.SlideGravityScale;
                    b.Vx+=g*n.Y*n.X*dt;b.Vz+=g*n.Y*n.Z*dt;
                    if(wishLen>0.1&&!braking)Steer(b,dirX,dirZ,t.SlideTurnRate*dt);
                    speed=JsMath.Hypot(b.Vx,b.Vz);
                    if(speed>t.SlideMaxSpeed)ScaleHorizontal(b,t.SlideMaxSpeed);
                    if(speed<t.SlideMinSpeed)b.Sliding=false;
                }else{
                    double rate=wishLen<0.01?t.GroundDecel:speed>moveSpeed+0.1?t.OverspeedDecel:t.GroundAccel;
                    Approach(b,dirX*moveSpeed*wishLen,dirZ*moveSpeed*wishLen,rate*dt);
                }
            }else if(b.OnSteep||b.SteepContact>0){
                // Resbalando: la entrada solo empuja hacia los lados o hacia abajo, nunca cuesta arriba.
                double ax=dirX*wishLen*t.AirAccel*0.3,az=dirZ*wishLen*t.AirAccel*0.3,downLen=JsMath.Hypot(n.X,n.Z);
                if(downLen>1e-4){double along=(ax*n.X+az*n.Z)/downLen;if(along<0){ax-=(along*n.X)/downLen;az-=(along*n.Z)/downLen;}}
                b.Vx+=ax*dt;b.Vz+=az*dt;
            }else if(wishLen>0.01){
                double maxAir=Math.Max(moveSpeed,speed);
                Approach(b,dirX*maxAir*wishLen,dirZ*maxAir*wishLen,t.AirAccel*dt);
            }

            if(b.JumpBuffer>0&&(b.Coyote>0||stuck)){
                b.Vy=t.JumpVelocity;
                if(stuck){double hl=JsMath.Hypot(n.X,n.Z);if(hl>1e-4){b.Vx+=(n.X/hl)*StuckJumpPush;b.Vz+=(n.Z/hl)*StuckJumpPush;}b.StuckTime=0;}
                if(b.Sliding){
                    double current=JsMath.Hypot(b.Vx,b.Vz),cap=moveSpeed*t.SlideBoostSpeedCap;
                    if(current<cap)ScaleHorizontal(b,Math.Min(current+t.SlideJumpBoost,cap));
                    b.Sliding=false;
                }
                b.Grounded=false;b.OnSteep=false;b.Coyote=0;b.JumpBuffer=0;b.Jumped=true;
            }

            if(!b.Grounded){
                double multiplier=1;
                if(b.Vy<0)multiplier=t.FallGravityMultiplier;
                else if(!intent.JumpHeld)multiplier=t.LowJumpGravityMultiplier;
                b.Vy=Math.Max(b.Vy-t.Gravity*multiplier*dt,-t.MaxFallSpeed);
            }

            double nx=b.X+b.Vx*dt,nz=b.Z+b.Vz*dt;
            if(Blocked(b,world,t,cosMaxSlope,nx,nz)){
                // Quitamos la componente que va cuesta arriba y reintentamos.
                var tn=world.GroundNormal(nx,nz,b.Y+t.StepHeight);
                double ux=-tn.X,uz=-tn.Z,ul=JsMath.Hypot(ux,uz);
                if(ul>1e-4){double into=(b.Vx*ux+b.Vz*uz)/ul;if(into>0){b.Vx-=(into*ux)/ul;b.Vz-=(into*uz)/ul;}}
                nx=b.X+b.Vx*dt;nz=b.Z+b.Vz*dt;
                if(Blocked(b,world,t,cosMaxSlope,nx,nz)){nx=b.X;nz=b.Z;}
            }
            b.X=nx;b.Z=nz;
            world.ResolveObstacles(b,t.Radius,t.StepHeight);
            world.Constrain(b);

            bool wasGrounded=b.Grounded;
            b.Y+=b.Vy*dt;
            double maxY=b.Y+t.StepHeight,ground=world.GroundHeight(b.X,b.Z,maxY);
            b.Normal=world.GroundNormal(b.X,b.Z,maxY);
            bool walkable=b.Normal.Y>=cosMaxSlope;
            if(b.Y<=ground){
                b.Y=ground;
                if(walkable){
                    if(!wasGrounded&&b.Vy<0){b.Landed=true;b.LandingSpeed=-b.Vy;}
                    if(b.Vy<0)b.Vy=0;
                }else{
                    double vn=b.Vx*b.Normal.X+b.Vy*b.Normal.Y+b.Vz*b.Normal.Z;
                    if(vn<0){b.Vx-=vn*b.Normal.X;b.Vy-=vn*b.Normal.Y;b.Vz-=vn*b.Normal.Z;}
                }
                b.Grounded=walkable;b.OnSteep=!walkable;
            }else{
                // Pegarse al suelo al bajar cuestas, salvo en bordes de acantilado.
                double snap=Math.Max(0.3,JsMath.Hypot(b.Vx,b.Vz)*dt*1.25);
                if(wasGrounded&&b.Vy<=0&&walkable&&b.Y-ground<=snap){b.Y=ground;b.Vy=0;b.Grounded=true;b.OnSteep=false;}
                else{b.Grounded=false;b.OnSteep=false;}
            }

            double horizontal=JsMath.Hypot(b.Vx,b.Vz);
            if(horizontal>0.6)b.Facing=LerpAngle(b.Facing,Math.Atan2(-b.Vx,-b.Vz),Damp(14,dt));
            else if(wishLen>0.1)b.Facing=LerpAngle(b.Facing,Math.Atan2(-dirX,-dirZ),Damp(14,dt));
        }

        // stepPlayerInCrowd: baja la velocidad de carrera y frena lo que vaya más rápido.
        public static void StepInCrowd(PlayerBody b,PlayerIntent intent,IPhysicsWorld world,PlayerTuning t,double moveSpeed,double crowdSlow,double dt)
        {
            double speed=moveSpeed*(1-crowdSlow);
            Step(b,intent,world,t,speed,dt);
            if(crowdSlow>0)BrakeHorizontal(b,speed,Tuning.CrowdBrake*(crowdSlow/Tuning.CrowdMaxSlow)*dt);
        }
        public static double CrowdSlowFor(double pressure)=>Math.Min(Tuning.CrowdMaxSlow,Math.Max(0,pressure)*Tuning.CrowdSlowPerMass);
        public static double SmoothCrowdSlow(double current,double target,double dt)=>current+(target-current)*Math.Min(1,dt*Tuning.CrowdResponse);
    }
}
