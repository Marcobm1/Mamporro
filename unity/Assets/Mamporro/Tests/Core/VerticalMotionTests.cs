using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class VerticalMotionTests
    {
        static VerticalMotion Create(out PlayerBody b)
        {
            var c=new VerticalCircuit();b=new PlayerBody();b.PlaceAt(7.598,1,-2);b.Grounded=false;b.Facing=-System.Math.PI/2;
            return new VerticalMotion(b,c.Collision,c.Queries);
        }
        static VerticalIntent Grip(double up=0,double side=0)=>new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=up,WallHorizontal=side};
        [TestCase(0,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(.2,1)]
        public void PassingWallDoesNotGrabWithoutPush(double x,double z)
        {
            var m=Create(out _);m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=x,MoveZ=z}},1.0/60);Assert.That(m.Grabs,Is.Zero);
        }
        [Test] public void MomentumAndFacingAloneCannotStartClimbing()
        {
            var m=Create(out var b);b.Vx=8;
            for(int i=0;i<10;i++)m.Step(default,1.0/60);
            Assert.That(m.Grabs,Is.Zero);
        }
        [TestCase(1,0)] [TestCase(1,1)]
        public void PushingIntoWallStartsWithoutAnyGrabButton(double x,double z)
        {
            var m=Create(out _);m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=x,MoveZ=z}},1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));Assert.That(m.Grabs,Is.EqualTo(1));
        }
        [Test] public void ClimbUsesApprovedSpeedAndHasNoDurationLimit()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);double y=b.Y;
            for(int i=0;i<30;i++)m.Step(Grip(1),1.0/60);
            Assert.That(b.Y-y,Is.EqualTo(2.25).Within(1e-8));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
            for(int i=0;i<7200;i++)m.Step(default,1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
        }
        [Test] public void DiagonalIsNormalized()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);double y=b.Y,z=b.Z;
            m.Step(Grip(1,1),1.0/60);
            Assert.That(b.Y-y,Is.EqualTo(4.5/60/System.Math.Sqrt(2)).Within(1e-8));
            Assert.That(System.Math.Abs(b.Z-z),Is.EqualTo(3.0/60/System.Math.Sqrt(2)).Within(1e-8));
        }
        [TestCase(.1)] [TestCase(.35)] [TestCase(.6)]
        public void WallJumpProtectsForConfiguredTimeThenAutomaticallyRegrabs(double delay)
        {
            var m=Create(out var b);m.Tuning.JumpReattachDelay=delay;m.Step(Grip(),1.0/60);var input=Grip();input.Movement.JumpPressed=true;m.Step(input,1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));Assert.That(b.Vx,Is.LessThan(0));Assert.That(b.Vy,Is.GreaterThan(0));
            Assert.That(m.ReattachRemaining,Is.EqualTo(delay));
            // Sonda de temporizador con contacto disponible; no es una ruta de gameplay.
            for(int i=1;i<=System.Math.Round(delay*60);i++){
                b.PlaceAt(7.598,1,-2);b.Grounded=false;m.Step(Grip(),1.0/60);
                if(i<delay*60-1e-8)Assert.That(m.Grabs,Is.EqualTo(1));
            }
            Assert.That(m.Grabs,Is.EqualTo(2));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
        }
        [Test] public void WallJumpReattachesOnRealTrajectoryWithoutReleasingMovement()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);var jump=Grip();jump.Movement.JumpPressed=true;m.Step(jump,1.0/60);
            for(int i=0;i<90&&m.Grabs==1;i++){
                m.Step(Grip(),1.0/60);
                if(i<19)Assert.That(m.Grabs,Is.EqualTo(1));
            }
            Assert.That(m.Grabs,Is.EqualTo(2));Assert.That(b.X,Is.LessThanOrEqualTo(7.6));
        }
        [Test] public void ReleaseSlideAndSuspendClearClimb()
        {
            var m=Create(out _);m.Step(Grip(),1.0/60);var input=Grip();input.Movement.SlidePressed=true;m.Step(input,1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));input.Movement.SlideHeld=true;
            for(int i=0;i<60;i++)m.Step(input,1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1),"Shift/C mantenido impide el agarre");
            m.Step(Grip(),1.0/60);Assert.That(m.Grabs,Is.EqualTo(2));
            m=Create(out _);m.Step(Grip(),1.0/60);m.Suspend();Assert.That(m.State,Is.EqualTo(VerticalState.Air));
            m.Step(Grip(),1.0/60);Assert.That(m.Grabs,Is.EqualTo(1));m.Reset();Assert.That(m.Grabs,Is.Zero);
        }
        [Test] public void FreeEdgeIsTraversedOverSeveralTicks()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);int transitionTicks=0;
            for(int i=0;i<140&&m.LedgeSuccess==0;i++){m.Step(Grip(1),1.0/60);if(m.State==VerticalState.Ledge)transitionTicks++;}
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(transitionTicks,Is.GreaterThan(2));
            Assert.That(b.Y,Is.EqualTo(6).Within(.01));Assert.That(b.X,Is.GreaterThan(8.4));
        }
        [Test] public void EdgeSupportsWholeBodyWhenGripStartsAtMaximumReach()
        {
            var m=Create(out var b);b.X=7.43;
            for(int i=0;i<160;i++)m.Step(Grip(1),1.0/60);
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(b.X,Is.GreaterThan(8.4));Assert.That(m.LedgeFailures,Is.Zero);
        }
        [Test] public void BlockedEdgeDoesNotTeleportThroughCeiling()
        {
            var m=Create(out var b);b.Z=0;m.Step(Grip(),1.0/60);
            for(int i=0;i<150;i++)m.Step(Grip(1),1.0/60);
            Assert.That(m.LedgeSuccess,Is.Zero);Assert.That(b.Y+PlayerTuning.Default.Height,Is.LessThanOrEqualTo(7.00001));
            Assert.That(m.Stalls,Is.GreaterThan(0));
            var input=Grip();input.Movement.JumpPressed=true;m.Step(input,1.0/60);Assert.That(m.State,Is.EqualTo(VerticalState.Air));
        }
        [Test] public void LedgeTimeoutReleasesInsteadOfStayingInTransition()
        {
            var m=Create(out _);m.Tuning.TransitionTimeout=.01;m.Step(Grip(),1.0/60);
            for(int i=0;i<100&&m.LedgeFailures==0;i++)m.Step(Grip(1),1.0/60);
            Assert.That(m.LedgeFailures,Is.EqualTo(1));Assert.That(m.State,Is.EqualTo(VerticalState.Air));
        }
        [Test] public void RepeatedInputsProduceIdenticalMotion()
        {
            var a=Create(out var x);var b=Create(out var y);
            for(int i=0;i<150;i++){var input=Grip(i%50<30?1:0);a.Step(input,1.0/60);b.Step(input,1.0/60);}
            Assert.That(x.X,Is.EqualTo(y.X));Assert.That(x.Y,Is.EqualTo(y.Y));Assert.That(x.Z,Is.EqualTo(y.Z));Assert.That(a.State,Is.EqualTo(b.State));
        }
        [Test] public void RampAndSmallStepRemainWalkableInQa()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(-25,0,0);var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<125;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},1.0/60);
            Assert.That(b.X,Is.GreaterThan(-9));Assert.That(b.Y,Is.EqualTo(4.25).Within(.01));Assert.That(m.Grabs,Is.Zero);
        }
        [Test] public void FlatGroundControlMatchesOriginalPhysicsAtFixedTicks()
        {
            var c=new VerticalCircuit();var original=new PlayerBody();var qa=new PlayerBody();original.PlaceAt(0,0,-10);qa.PlaceAt(0,0,-10);
            var motion=new VerticalMotion(qa,c.Collision,c.Queries);var input=new PlayerIntent{MoveX=1};
            for(int i=0;i<60;i++){
                PlayerPhysics.StepInCrowd(original,input,c.Collision,PlayerTuning.Default,9.5,0,1.0/60);
                motion.Step(new VerticalIntent{Movement=input},1.0/60);
                Assert.That(qa.X,Is.EqualTo(original.X));Assert.That(qa.Y,Is.EqualTo(original.Y));Assert.That(qa.Z,Is.EqualTo(original.Z));
            }
        }
        [Test] public void RoofExitFallsAndCornersAllowSeparationWithoutPenetration()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(13,6,3);var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<40;i++){m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},1.0/60);Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);}
            Assert.That(b.Y,Is.LessThan(5));Assert.That(m.Grabs,Is.Zero);
            b.PlaceAt(7.598,1,12);b.Grounded=false;b.Facing=-System.Math.PI/2;m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<100;i++)m.Step(Grip(0,-1),1.0/60);
            Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);Assert.That(m.Stalls,Is.GreaterThan(0));
            m.Step(new VerticalIntent{Movement=new PlayerIntent{JumpPressed=true}},1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));Assert.That(m.Armed,Is.False);
        }
        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        public void PitCanBeExitedByClimbingEachSolidSide(double x,double z)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(19,-6,11);
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<240&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=x,MoveZ=z},WallVertical=1},1.0/60);
                Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);
            }
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(b.Y,Is.EqualTo(.002).Within(.01));
            Assert.That(m.Stalls,Is.Zero);Assert.That(m.LedgeFailures,Is.Zero);
            Assert.That(c.Queries.Support(new Vec3(b.X,b.Y,b.Z),.4,.003,out _),Is.True);
        }

        [TestCase(0)] [TestCase(1.5)]
        public void FrontRunFromGroundOrFallingAirNeedsNoJump(double height)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(6,height,-2);b.Grounded=height==0;b.Vy=height==0?0:-3;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<60&&m.Grabs==0;i++)m.Step(Grip(1),1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1));Assert.That(b.Jumped,Is.False);
            double y=b.Y;m.Step(Grip(1),1.0/60);
            Assert.That(b.Y,Is.GreaterThan(y));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
        }
        [TestCase(.49,false)] [TestCase(.5,true)] [TestCase(.51,true)]
        public void PushAngleBoundaryIsGeometric(double dot,bool expected)
        {
            var m=Create(out _);
            m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dot,MoveZ=System.Math.Sqrt(1-dot*dot)}},1.0/60);
            Assert.That(m.Grabs>0,Is.EqualTo(expected));
        }
        [Test] public void WallJumpCanCatchAnotherWallDuringProtection()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(7.598,1,-2);b.Grounded=false;
            var q=new VerticalQueries(new VerticalSolid(1,new Vec3(8,0,-4),new Vec3(14,6,4)),
                new VerticalSolid(2,new Vec3(6,0,-4),new Vec3(6.65,6,4)));
            var m=new VerticalMotion(b,c.Collision,q);m.Step(Grip(),1.0/60);
            var jump=Grip();jump.Movement.JumpPressed=true;m.Step(jump,1.0/60);
            for(int i=0;i<15&&m.Grabs==1;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=-1}},1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(2));Assert.That(m.ReattachRemaining,Is.GreaterThan(0));
            Assert.That(m.CurrentContactBlocked,Is.False);Assert.That(q.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);
        }
        [TestCase(18,10,1,.3)] [TestCase(20,12,-1,-.3)] [TestCase(19,11,.3,1)]
        public void FallIntoPitLandThenRunAndExitWithoutRescue(double x,double z,double dx,double dz)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(x,2,z);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<180&&!b.Grounded;i++)m.Step(default,1.0/60);
            Assert.That(b.Grounded,Is.True);Assert.That(b.Y,Is.EqualTo(-6).Within(.01));
            Assert.That(m.Grabs,Is.Zero);
            for(int i=0;i<240&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},1.0/60);
                Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);
            }
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(b.Y,Is.GreaterThanOrEqualTo(0));Assert.That(m.Stalls,Is.Zero);
        }
        [TestCase(1)] [TestCase(-1)]
        public void FallingBesidePitWallCanGrabAndExit(double side)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(side>0?21.598:16.402,-2,11);b.Grounded=false;b.Vy=-5;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<180&&m.LedgeSuccess==0;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=side},WallVertical=1},1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1));Assert.That(m.LedgeSuccess,Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(.2)]
        public void ParallelAndShallowPitPassDoesNotGrab(double into)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(21.598,-6,9);
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<20;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=into,MoveZ=1}},1.0/60);
            Assert.That(m.Grabs,Is.Zero);Assert.That(b.Z,Is.GreaterThan(10));
        }

        [Test] public void DescendingToFloorDoesNotAlternateGroundAndClimb()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);
            for(int i=0;i<120;i++)m.Step(Grip(-1),1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1));Assert.That(m.State,Is.EqualTo(VerticalState.Ground));Assert.That(b.Y,Is.Zero);
            m.Step(Grip(1),1.0/60);Assert.That(m.Grabs,Is.EqualTo(2));
        }
        [Test] public void CliffClimbsFromGroundWithoutJump()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(0,0,14);
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<200&&m.LedgeSuccess==0;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveZ=1},WallVertical=1},1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1));Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(b.Y,Is.GreaterThanOrEqualTo(8));
        }
        [Test] public void PitWallJumpReturnsAndCompletesExit()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(21.598,-4,11);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,c.Queries);m.Step(Grip(),1.0/60);
            var jump=Grip();jump.Movement.JumpPressed=true;m.Step(jump,1.0/60);double far=b.X;
            for(int i=0;i<240&&m.LedgeSuccess==0;i++){
                m.Step(Grip(1),1.0/60);far=System.Math.Min(far,b.X);
                if(i<19)Assert.That(m.Grabs,Is.EqualTo(1));
                Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);
            }
            Assert.That(far,Is.LessThan(21.4));Assert.That(m.Grabs,Is.EqualTo(2));Assert.That(m.LedgeSuccess,Is.EqualTo(1));
        }
        [Test] public void StandingClimbAtGroundDoesNotOscillate()
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(7.598,0,-2);
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<120;i++)m.Step(Grip(),1.0/60);
            Assert.That(m.Grabs,Is.EqualTo(1));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
        }
        [Test] public void VerticalAndLateralSpeedsAreIndependentParameters()
        {
            var m=Create(out var b);m.Tuning.UpSpeed=2;m.Tuning.SideSpeed=1;m.Step(Grip(),1.0/60);
            double y=b.Y,z=b.Z;for(int i=0;i<30;i++)m.Step(Grip(1),1.0/60);
            Assert.That(b.Y-y,Is.EqualTo(1).Within(1e-8));
            for(int i=0;i<30;i++)m.Step(Grip(0,1),1.0/60);
            Assert.That(System.Math.Abs(b.Z-z),Is.EqualTo(.5).Within(1e-8));
        }
    }
}
