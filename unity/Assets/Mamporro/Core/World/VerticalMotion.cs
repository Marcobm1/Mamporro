using System;

namespace Mamporro.Core
{
    public enum VerticalState { Ground, Air, Climbing, Ledge }
    public struct VerticalIntent
    {
        public PlayerIntent Movement;
        public double WallHorizontal,WallVertical;
        // Derecha horizontal de la cámara en coordenadas núcleo; cero = sin cámara (base de pared).
        public double CameraRightX,CameraRightZ;
    }
    public sealed class VerticalTuning
    {
        // Sin velocidades propias: escalar usa la velocidad efectiva de carrera (PlayerPhysics.EffectiveSpeed).
        public double Reach=.18,SeparationSpeed=5,TransitionTimeout=1.5;
        public double JumpReattachDelay=.35,MinPushDot=.5;
    }
    // Extensión optativa P0 de la física pura. Un único PlayerBody, sin Rigidbody/root motion.
    public sealed class VerticalMotion
    {
        public readonly PlayerBody Body;
        public readonly VerticalTuning Tuning;
        public VerticalState State {get;private set;}
        public double ReattachRemaining {get;private set;}
        public bool Armed=>ReattachRemaining<=1e-9;
        public int Grabs,LedgeSuccess,LedgeFailures,Stalls;
        public double ClimbSeconds,DescendSeconds,Distance;
        public string LastFailure="";
        readonly IPhysicsWorld world;
        readonly VerticalQueries queries;
        readonly PlayerTuning physics;
        VerticalHit wall,detachedWall;
        public VerticalHit Contact=>wall;
        bool blockAll,descendedToGround;
        Vec3 lift,top;
        bool onTopLeg;
        double transitionTime,stalled;
        public VerticalMotion(PlayerBody body,IPhysicsWorld world,VerticalQueries queries,VerticalTuning tuning=null,PlayerTuning physics=null)
        {
            Body=body;this.world=world;this.queries=queries;Tuning=tuning??new VerticalTuning();this.physics=physics??PlayerTuning.Default;
            State=body.Grounded?VerticalState.Ground:VerticalState.Air;
        }
        Vec3 Position=>new Vec3(Body.X,Body.Y,Body.Z);
        void Place(Vec3 p){Body.X=p.X;Body.Y=p.Y;Body.Z=p.Z;}
        static double Length(Vec3 p)=>Math.Sqrt(p.X*p.X+p.Y*p.Y+p.Z*p.Z);
        static Vec3 Sub(Vec3 a,Vec3 b)=>new Vec3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        // Derecha mirando hacia la pared. El núcleo usa Z web; Unity refleja ese eje.
        // En Unity equivale a Cross(up, -normal). No depende de yaw ni pitch.
        public static Vec3 WallRight(Vec3 normal)
        {
            double length=JsMath.Hypot(normal.X,normal.Z);
            return length>1e-9?new Vec3(normal.Z/length,0,-normal.X/length):default;
        }
        // A/D siguen la pantalla. Con la cámara hacia la pared coincide con WallRight; si
        // el jugador gira la cámara hasta mirar de espaldas a la pared, la derecha visible es
        // la contraria. Histéresis: mirando a lo largo de la pared se conserva el sentido.
        public const double ScreenFlipDot=.25;
        public static bool ScreenMirrored(Vec3 normal,double cameraRightX,double cameraRightZ,bool previous)
        {
            double length=JsMath.Hypot(cameraRightX,cameraRightZ);if(length<1e-9)return false;
            var right=WallRight(normal);double dot=(right.X*cameraRightX+right.Z*cameraRightZ)/length;
            return previous?dot<ScreenFlipDot:dot< -ScreenFlipDot;
        }
        bool mirrored;
        public bool LateralMirrored=>mirrored;
        // Tangente ascendente de la cara, perpendicular a WallRight: vertical en muros rectos.
        public static Vec3 WallUp(Vec3 normal)
        {
            var u=new Vec3(-normal.X*normal.Y,normal.X*normal.X+normal.Z*normal.Z,-normal.Z*normal.Y);
            double length=Math.Sqrt(u.X*u.X+u.Y*u.Y+u.Z*u.Z);
            return length>1e-9?new Vec3(u.X/length,u.Y/length,u.Z/length):new Vec3(0,1,0);
        }
        public bool PushesIntoContact(PlayerIntent input)
        {
            double length=JsMath.Hypot(input.MoveX,input.MoveZ);
            double horizontal=JsMath.Hypot(wall.Normal.X,wall.Normal.Z);
            return length>.1&&horizontal>1e-9&&-(input.MoveX*wall.Normal.X+input.MoveZ*wall.Normal.Z)>=Tuning.MinPushDot*length*horizontal;
        }
        bool CanStartClimb(PlayerIntent input)
        {
            if(input.JumpPressed||input.SlidePressed||input.SlideHeld)return false;
            var toward=new Vec3(input.MoveX,0,input.MoveZ);
            if(!queries.Wall(Position,toward,physics.Radius,physics.Height,Tuning.Reach,out wall)||!PushesIntoContact(input))return false;
            if(IsReattachBlocked(wall))return false;
            // Los escalones transitables siguen siendo pasos de suelo, no agarres.
            if(Body.Grounded&&!wall.Terrain)for(int i=0;i<queries.Count;i++)
                if(queries.Solid(i).Id==wall.Id&&queries.Solid(i).Max.Y<=Body.Y+physics.StepHeight)return false;
            return queries.Clear(Position,physics.Radius,physics.Height);
        }
        void Release(bool jump)
        {
            detachedWall=wall;blockAll=false;
            ReattachRemaining=jump?Tuning.JumpReattachDelay:0;State=VerticalState.Air;Body.Grounded=false;Body.Sliding=false;Body.SlideQueued=false;
            Body.Jumped=jump;
            Body.Coyote=Body.JumpBuffer=0;Body.Vy=jump?physics.JumpVelocity:0;
            double horizontal=JsMath.Hypot(wall.Normal.X,wall.Normal.Z);
            Body.Vx=jump?wall.Normal.X/horizontal*Tuning.SeparationSpeed:0;Body.Vz=jump?wall.Normal.Z/horizontal*Tuning.SeparationSpeed:0;
            transitionTime=stalled=0;
        }
        // La pared puede estar dividida en cajas o triángulos: una junta coplanar no
        // termina la protección. Otra orientación o plano conserva el agarre inmediato.
        bool IsReattachBlocked(VerticalHit contact)=>!Armed&&(blockAll
            ||(contact.Terrain==detachedWall.Terrain&&contact.Id==detachedWall.Id||SameWallPlane(contact,detachedWall))
            &&contact.Normal.X*detachedWall.Normal.X+contact.Normal.Y*detachedWall.Normal.Y+contact.Normal.Z*detachedWall.Normal.Z>.99);
        static bool SameWallPlane(VerticalHit a,VerticalHit b)=>Math.Abs((a.Point.X-b.Point.X)*b.Normal.X
            +(a.Point.Y-b.Point.Y)*b.Normal.Y+(a.Point.Z-b.Point.Z)*b.Normal.Z)<.05;
        public bool CurrentContactBlocked=>IsReattachBlocked(wall);
        // Pausa/foco/muerte/reinicio: no conservar agarre ni entrada latente.
        public void Suspend()
        {
            if(State==VerticalState.Climbing||State==VerticalState.Ledge)Release(false);
            descendedToGround=false;blockAll=true;ReattachRemaining=Tuning.JumpReattachDelay;Body.JumpBuffer=0;Body.SlideQueued=false;
        }
        public void Reset()
        {
            descendedToGround=false;blockAll=true;ReattachRemaining=Tuning.JumpReattachDelay;State=Body.Grounded?VerticalState.Ground:VerticalState.Air;
            Grabs=LedgeSuccess=LedgeFailures=Stalls=0;ClimbSeconds=DescendSeconds=Distance=0;
            transitionTime=stalled=0;LastFailure="";Body.JumpBuffer=0;Body.SlideQueued=false;
        }
        bool Move(Vec3 target)
        {
            var from=Position;double fraction=1;
            if(queries.Sweep(from,target,physics.Radius,physics.Height,out var hit))fraction=Math.Max(0,hit.Fraction-1e-5);
            var p=new Vec3(from.X+(target.X-from.X)*fraction,from.Y+(target.Y-from.Y)*fraction,from.Z+(target.Z-from.Z)*fraction);
            if(!queries.Clear(p,physics.Radius,physics.Height))return false;
            Place(p);return fraction>.999;
        }
        void Walk(Vec3 target,bool wasGrounded)
        {
            var from=Position;
            if(!queries.Sweep(from,target,physics.Radius,physics.Height,out var hit)){Move(target);return;}
            // Escalón U3: probar elevación y avance completos antes de aceptar el paso.
            if(wasGrounded&&!hit.Terrain&&Math.Abs(hit.Normal.Y)<.01){
                for(int i=0;i<queries.Count;i++){
                    var s=queries.Solid(i);double rise=s.Max.Y-from.Y;
                    if(s.Id!=hit.Id||rise<=0||rise>physics.StepHeight)continue;
                    var lift=new Vec3(from.X,s.Max.Y+.002,from.Z);
                    var end=new Vec3(target.X,Math.Max(target.Y,lift.Y),target.Z);
                    if(queries.Clear(end,physics.Radius,physics.Height)&&!queries.Sweep(from,lift,physics.Radius,physics.Height,out _)
                        &&!queries.Sweep(lift,end,physics.Radius,physics.Height,out _)){Place(end);return;}
                }
            }
            Move(target);
            // Conservar la componente tangencial: al salir del tejado el cuerpo todavía
            // puede rozar su canto. Frenar todo el vector lo dejaría clavado en el borde.
            var remaining=Sub(target,Position);double into=remaining.X*hit.Normal.X+remaining.Y*hit.Normal.Y+remaining.Z*hit.Normal.Z;
            if(into<0)Move(new Vec3(Body.X+remaining.X-hit.Normal.X*into,Body.Y+remaining.Y-hit.Normal.Y*into,Body.Z+remaining.Z-hit.Normal.Z*into));
            if(hit.Normal.Y<-.5&&Body.Vy>0)Body.Vy=0;
            if(hit.Normal.Y>.5&&Body.Vy<=0){Body.Vy=0;Body.Grounded=true;}
        }
        public const double MaxClimbSubstep=4.5/60;
        // Un tramo de escalada sobre la superficie: W/S/A/D con el mismo módulo y diagonal
        // ya normalizada, también en paredes inclinadas del terreno.
        void ClimbSubstep(double v,double h,double step)
        {
            if(v>0&&queries.NearEdge(Position,wall)&&queries.Ledge(Position,wall,physics.Radius,physics.Height,out lift,out top)){
                State=VerticalState.Ledge;transitionTime=0;onTopLeg=false;return;
            }
            var right=WallRight(wall.Normal);var up=WallUp(wall.Normal);double climb=v*step;
            if(climb*up.Y<0){
                // Bajar sin hundirse en el suelo: el último tramo se detiene en él.
                double floor=world.GroundHeight(Body.X,Body.Z,Body.Y+.01);
                if(Body.Y+climb*up.Y<floor)climb=Math.Min(0,floor-Body.Y)/up.Y;
            }
            bool moved=Move(new Vec3(Body.X+right.X*h*step+up.X*climb,Body.Y+up.Y*climb,Body.Z+right.Z*h*step+up.Z*climb));
            double ground=world.GroundHeight(Body.X,Body.Z,Body.Y+.01);
            if(v<0&&Body.Y<=ground){Body.Y=ground;Release(false);descendedToGround=true;Body.Grounded=true;State=VerticalState.Ground;}
            else if(!queries.Wall(Position,new Vec3(-wall.Normal.X,0,-wall.Normal.Z),physics.Radius,physics.Height,Tuning.Reach,out var contact)){
                LastFailure=moved?"fin de superficie":"contacto perdido";Release(false);
            }else wall=contact;
        }
        public void Step(VerticalIntent input,double dt,double moveSpeed=9.5,double crowdSlow=0)
        {
            if(dt<=0||dt>.1||double.IsNaN(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            if(input.WallVertical>=0)descendedToGround=false;
            var before=Position;ReattachRemaining=Math.Max(0,ReattachRemaining-dt);
            // Misma fuente que la carrera: estadística moveSpeed (tomos, objetos, santuarios) y
            // frenada de horda. No es la velocidad horizontal instantánea, nula contra la pared.
            double speed=PlayerPhysics.EffectiveSpeed(moveSpeed,crowdSlow);
            if(State==VerticalState.Climbing||State==VerticalState.Ledge){
                if(input.Movement.JumpPressed||input.Movement.SlidePressed||input.Movement.SlideHeld){Release(input.Movement.JumpPressed);return;}
                Body.Vx=Body.Vy=Body.Vz=0;Body.Grounded=false;Body.Sliding=false;
                if(State==VerticalState.Ledge){
                    transitionTime+=dt;
                    if(transitionTime>Tuning.TransitionTimeout){LedgeFailures++;LastFailure="transición agotada";Release(false);return;}
                    var target=onTopLeg?top:lift;var d=Sub(target,Position);double length=Length(d),amount=Math.Min(length,speed*dt);
                    if(length>1e-8&&!Move(new Vec3(Body.X+d.X/length*amount,Body.Y+d.Y/length*amount,Body.Z+d.Z/length*amount))){
                        LedgeFailures++;LastFailure="barrido de borde bloqueado";Release(false);return;
                    }
                    if(length<=amount+1e-8){if(!onTopLeg)onTopLeg=true;else{State=VerticalState.Ground;Body.Grounded=true;LedgeSuccess++;}}
                }else{
                    double v=input.WallVertical,h=input.WallHorizontal,n=Math.Max(1,JsMath.Hypot(v,h));v/=n;h/=n;
                    mirrored=ScreenMirrored(wall.Normal,input.CameraRightX,input.CameraRightZ,mirrored);
                    if(mirrored)h=-h;
                    // Recorrido del tick con módulo speed, en tramos no mayores que el paso aprobado
                    // a 4,5 m/s: cada tramo repite borde, barrido y contacto, así una velocidad alta
                    // no salta la ventana del borde ni el final de una cara inclinada.
                    double travel=speed*dt;int count=Math.Max(1,(int)Math.Ceiling(travel/MaxClimbSubstep-1e-9));
                    for(int i=0;i<count&&State==VerticalState.Climbing;i++)ClimbSubstep(v,h,travel/count);
                    if(State!=VerticalState.Ledge){
                        if(v>0)ClimbSeconds+=dt;else if(v<0)DescendSeconds+=dt;
                        if(Length(Sub(Position,before))<1e-6&&(Math.Abs(v)+Math.Abs(h)>.1)){
                            stalled+=dt;if(stalled>=.35){if(stalled-dt<.35){Stalls++;if(v>0)LedgeFailures++;}LastFailure=v>0?"ascenso/borde bloqueado":"intención sin avance";}
                        }else stalled=0;
                    }
                }
            }else{
                // Mantener exactamente PlayerPhysics cuando no está escalando; barrido adicional QA tras resolver suelo.
                bool wasGrounded=Body.Grounded;
                PlayerPhysics.StepInCrowd(Body,input.Movement,world,physics,moveSpeed,crowdSlow,dt);
                var target=Position;Place(before);Walk(target,wasGrounded);
                if(Body.Y<target.Y-1e-6&&Body.Vy>0)Body.Vy=0;
                State=Body.Grounded?VerticalState.Ground:VerticalState.Air;
                if(!descendedToGround&&CanStartClimb(input.Movement)){
                    State=VerticalState.Climbing;Grabs++;mirrored=false;Body.Grounded=false;Body.Sliding=false;Body.SlideQueued=false;Body.Vx=Body.Vy=Body.Vz=0;
                }
            }
            world.Constrain(Body);Distance+=Length(Sub(Position,before));
        }
    }
}
