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
        static VerticalIntent Grip(double up=0,double side=0)=>new VerticalIntent{GrabHeld=true,WallVertical=up,WallHorizontal=side};
        [Test] public void PassingWallDoesNotGrabWithoutIntent()
        {
            var m=Create(out _);m.Step(default,1.0/60);Assert.That(m.Grabs,Is.Zero);
        }
        [Test] public void ClimbUsesApprovedSpeedAndHasNoDurationLimit()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);double y=b.Y;
            for(int i=0;i<30;i++)m.Step(Grip(1),1.0/60);
            Assert.That(b.Y-y,Is.EqualTo(2.25).Within(1e-8));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
            for(int i=0;i<7200;i++)m.Step(Grip(),1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
        }
        [Test] public void DiagonalIsNormalized()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);double y=b.Y,z=b.Z;
            m.Step(Grip(1,1),1.0/60);
            Assert.That(b.Y-y,Is.EqualTo(4.5/60/System.Math.Sqrt(2)).Within(1e-8));
            Assert.That(System.Math.Abs(b.Z-z),Is.EqualTo(3.0/60/System.Math.Sqrt(2)).Within(1e-8));
        }
        [Test] public void WallJumpRequiresReleaseBeforeRegrab()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);var input=Grip();input.Movement.JumpPressed=true;m.Step(input,1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));Assert.That(b.Vx,Is.LessThan(0));Assert.That(b.Vy,Is.GreaterThan(0));
            Assert.That(m.Armed,Is.False);m.Step(Grip(),1.0/60);Assert.That(m.Grabs,Is.EqualTo(1));
            m.Step(default,1.0/60);Assert.That(m.Armed,Is.True);
        }
        [Test] public void ReleaseSlideAndSuspendClearClimb()
        {
            var m=Create(out _);m.Step(Grip(),1.0/60);var input=Grip();input.Movement.SlidePressed=true;m.Step(input,1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));Assert.That(m.Armed,Is.False);
            m=Create(out _);m.Step(Grip(),1.0/60);m.Suspend();Assert.That(m.State,Is.EqualTo(VerticalState.Air));
            m.Step(Grip(),1.0/60);Assert.That(m.Grabs,Is.EqualTo(1));m.Reset();Assert.That(m.Grabs,Is.Zero);
        }
        [Test] public void FreeEdgeIsTraversedOverSeveralTicks()
        {
            var m=Create(out var b);m.Step(Grip(),1.0/60);int transitionTicks=0;
            for(int i=0;i<140;i++){m.Step(Grip(1),1.0/60);if(m.State==VerticalState.Ledge)transitionTicks++;}
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));Assert.That(transitionTicks,Is.GreaterThan(2));
            Assert.That(b.Y,Is.EqualTo(6).Within(.01));Assert.That(b.X,Is.GreaterThan(8.4));
        }
        [Test] public void BlockedEdgeDoesNotTeleportThroughCeiling()
        {
            var m=Create(out var b);b.Z=0;m.Step(Grip(),1.0/60);
            for(int i=0;i<150;i++)m.Step(Grip(1),1.0/60);
            Assert.That(m.LedgeSuccess,Is.Zero);Assert.That(b.Y+PlayerTuning.Default.Height,Is.LessThanOrEqualTo(7.00001));
            Assert.That(m.Stalls,Is.GreaterThan(0));
            var input=Grip();input.Movement.JumpPressed=true;m.Step(input,1.0/60);Assert.That(m.State,Is.EqualTo(VerticalState.Air));
        }
        [Test] public void RepeatedInputsProduceIdenticalMotion()
        {
            var a=Create(out var x);var b=Create(out var y);
            for(int i=0;i<150;i++){var input=Grip(i%50<30?1:0);a.Step(input,1.0/60);b.Step(input,1.0/60);}
            Assert.That(x.X,Is.EqualTo(y.X));Assert.That(x.Y,Is.EqualTo(y.Y));Assert.That(x.Z,Is.EqualTo(y.Z));Assert.That(a.State,Is.EqualTo(b.State));
        }
    }
}
