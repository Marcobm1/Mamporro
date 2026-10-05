using System;

namespace Mamporro.Core
{
    public enum VerticalState { Ground, Air, Climbing, Ledge }
    public struct VerticalIntent
    {
        public PlayerIntent Movement;
        public bool GrabHeld;
        public double WallHorizontal,WallVertical;
    }
    public sealed class VerticalTuning
    {
        public double UpSpeed=4.5,SideSpeed=3,Reach=.18,LedgeSpeed=4.5,SeparationSpeed=5,TransitionTimeout=1.5;
    }
    // Extensión optativa P0 de la física pura. Un único PlayerBody, sin Rigidbody/root motion.
    public sealed class VerticalMotion
    {
        public readonly PlayerBody Body;
        public readonly VerticalTuning Tuning;
        public VerticalState State {get;private set;}
        public bool Armed {get;private set;}=true;
        public int Grabs,LedgeSuccess,LedgeFailures,Stalls;
        public double ClimbSeconds,DescendSeconds,Distance;
        public string LastFailure="";
        readonly IPhysicsWorld world;
        readonly VerticalQueries queries;
        readonly PlayerTuning physics;
        VerticalHit wall;
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
        void Release(bool jump)
        {
            Armed=false;State=VerticalState.Air;Body.Grounded=false;Body.Sliding=false;Body.SlideQueued=false;
            Body.Coyote=Body.JumpBuffer=0;Body.Vy=jump?physics.JumpVelocity:0;
            Body.Vx=jump?wall.Normal.X*Tuning.SeparationSpeed:0;Body.Vz=jump?wall.Normal.Z*Tuning.SeparationSpeed:0;
            transitionTime=stalled=0;
        }
        // Pausa/foco/muerte/reinicio: no conservar agarre ni entrada latente.
        public void Suspend()
        {
            if(State==VerticalState.Climbing||State==VerticalState.Ledge)Release(false);
            Armed=false;Body.JumpBuffer=0;Body.SlideQueued=false;
        }
        public void Reset()
        {
            Armed=false;State=Body.Grounded?VerticalState.Ground:VerticalState.Air;
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
        public void Step(VerticalIntent input,double dt,double moveSpeed=9.5,double crowdSlow=0)
        {
            if(dt<=0||dt>.1||double.IsNaN(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            var before=Position;if(!input.GrabHeld)Armed=true;
            if(State==VerticalState.Climbing||State==VerticalState.Ledge){
                if(!input.GrabHeld||input.Movement.JumpPressed||input.Movement.SlidePressed){Release(input.Movement.JumpPressed);return;}
                Body.Vx=Body.Vy=Body.Vz=0;Body.Grounded=false;Body.Sliding=false;
                if(State==VerticalState.Ledge){
                    transitionTime+=dt;
                    if(transitionTime>Tuning.TransitionTimeout){LedgeFailures++;LastFailure="transición agotada";Release(false);return;}
                    var target=onTopLeg?top:lift;var d=Sub(target,Position);double length=Length(d),amount=Math.Min(length,Tuning.LedgeSpeed*dt);
                    if(length>1e-8&&!Move(new Vec3(Body.X+d.X/length*amount,Body.Y+d.Y/length*amount,Body.Z+d.Z/length*amount))){
                        LedgeFailures++;LastFailure="barrido de borde bloqueado";Release(false);return;
                    }
                    if(length<=amount+1e-8){if(!onTopLeg)onTopLeg=true;else{State=VerticalState.Ground;Body.Grounded=true;Armed=false;LedgeSuccess++;}}
                }else{
                    double v=input.WallVertical,h=input.WallHorizontal,n=Math.Max(1,JsMath.Hypot(v,h));v/=n;h/=n;
                    bool atEdge=false;
                    for(int i=0;i<queries.Count;i++)if(queries.Solid(i).Id==wall.Id)atEdge=Body.Y>=queries.Solid(i).Max.Y-.2;
                    if(v>0&&atEdge&&queries.Ledge(Position,wall,physics.Radius,physics.Height,out lift,out top)){
                        State=VerticalState.Ledge;transitionTime=0;onTopLeg=false;
                    }else{
                        var target=new Vec3(Body.X-wall.Normal.Z*h*Tuning.SideSpeed*dt,Body.Y+v*Tuning.UpSpeed*dt,Body.Z+wall.Normal.X*h*Tuning.SideSpeed*dt);
                        bool moved=Move(target);
                        if(v>0)ClimbSeconds+=dt;else if(v<0)DescendSeconds+=dt;
                        if(Length(Sub(Position,before))<1e-6&&(Math.Abs(v)+Math.Abs(h)>.1)){
                            stalled+=dt;if(stalled>=.35){if(stalled-dt<.35)Stalls++;LastFailure="intención sin avance";}
                        }else stalled=0;
                        double ground=world.GroundHeight(Body.X,Body.Z,Body.Y+.01);
                        if(v<0&&Body.Y<=ground){Body.Y=ground;Release(false);Body.Grounded=true;State=VerticalState.Ground;}
                        else if(!queries.Wall(Position,new Vec3(-wall.Normal.X,0,-wall.Normal.Z),physics.Radius,physics.Height,Tuning.Reach,out var contact)){
                            LastFailure=moved?"fin de superficie":"contacto perdido";Release(false);
                        }else wall=contact;
                    }
                }
            }else{
                // Mantener exactamente PlayerPhysics cuando no está escalando; barrido adicional QA tras resolver suelo.
                PlayerPhysics.StepInCrowd(Body,input.Movement,world,physics,moveSpeed,crowdSlow,dt);
                var target=Position;Place(before);Move(target);
                if(Body.Y<target.Y-1e-6&&Body.Vy>0)Body.Vy=0;
                State=Body.Grounded?VerticalState.Ground:VerticalState.Air;
                if(input.GrabHeld&&Armed){
                    var direction=new Vec3(input.Movement.MoveX,0,input.Movement.MoveZ);
                    if(JsMath.Hypot(direction.X,direction.Z)<.01)direction=new Vec3(-Math.Sin(Body.Facing),0,-Math.Cos(Body.Facing));
                    if(queries.Wall(Position,direction,physics.Radius,physics.Height,Tuning.Reach,out wall)&&queries.Clear(Position,physics.Radius,physics.Height)){
                        State=VerticalState.Climbing;Grabs++;Body.Grounded=false;Body.Sliding=false;Body.SlideQueued=false;Body.Vx=Body.Vy=Body.Vz=0;
                    }
                }
            }
            world.Constrain(Body);Distance+=Length(Sub(Position,before));
        }
    }
}
