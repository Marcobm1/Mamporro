using NUnit.Framework;
using UnityEngine;

namespace Mamporro.U1.Tests
{
    public sealed class SimulationTests
    {
        PrototypeSettings settings;
        [SetUp] public void Setup()=>settings=ScriptableObject.CreateInstance<PrototypeSettings>();
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(settings);
        [Test] public void WalkAndBrake()
        {
            var motor=new PlayerMotor(settings);
            for(int i=0;i<60;i++) motor.Step(new MoveIntent {direction=Vector2.right},1f/60);
            Assert.That(motor.Position.x,Is.InRange(8f,10f));
            for(int i=0;i<30;i++) motor.Step(default,1f/60);
            Assert.That(motor.Velocity.magnitude,Is.LessThan(.01f));
        }
        [Test] public void ReleasedJumpIsLowerAndBothLand()
        {
            float Jump(bool held) {
                var motor=new PlayerMotor(settings); float peak=0;
                for(int i=0;i<180;i++) { motor.Step(new MoveIntent {jumpPressed=i==0,jumpHeld=held},1f/60); peak=Mathf.Max(peak,motor.Position.y); }
                Assert.That(motor.Grounded,Is.True); Assert.That(motor.Position.y,Is.EqualTo(0).Within(.001)); return peak;
            }
            Assert.That(Jump(true),Is.GreaterThan(Jump(false)+.5f));
        }
        [Test] public void SlideBoostCannotBeStacked()
        {
            var motor=new PlayerMotor(settings);
            for(int i=0;i<300;i++) motor.Step(new MoveIntent {direction=Vector2.right,slidePressed=true,slideHeld=true},1f/60);
            Assert.That(motor.Velocity.magnitude,Is.LessThanOrEqualTo(settings.moveSpeed*1.8f+.01f));
        }
        [Test] public void ObstaclesAndBoundaryContainMovement()
        {
            var p=new Vector3(-8,0,0);
            for(int i=0;i<100;i++) p=TechnicalWorld.Move(p,new Vector3(0,0,.1f),.4f,.45f);
            Assert.That(p.z,Is.LessThanOrEqualTo(2.6f));
            Assert.That(TechnicalWorld.Move(new Vector3(47,0,0),Vector3.right*5,.4f,.45f).x,Is.LessThanOrEqualTo(47.6f));
        }
        [TestCase(300)] [TestCase(500)] [TestCase(750)] [TestCase(1000)]
        public void HordeIsDeterministicAndStaysOutsideBlocks(int count)
        {
            var a=new HordeSimulation(1200); var b=new HordeSimulation(1200);
            a.Reset(count,6741); b.Reset(count,6741);
            for(int tick=0;tick<120;tick++) { a.Step(new Vector3(0,0,-16),4.2f,1f/60); b.Step(new Vector3(0,0,-16),4.2f,1f/60); }
            Assert.That(a.Count,Is.EqualTo(count));
            for(int i=0;i<count;i++) { Assert.That(a.Positions[i],Is.EqualTo(b.Positions[i])); Assert.That(TechnicalWorld.Blocked(a.Positions[i],.44f),Is.False); }
        }
        [Test] public void HordeResetReusesStorage()
        {
            var h=new HordeSimulation(1000); var storage=h.Positions;
            h.Reset(1000,1); h.Reset(300,1); Assert.That(h.Positions,Is.SameAs(storage)); Assert.That(h.Count,Is.EqualTo(300));
        }
        [Test] public void HordeHotLoopDoesNotAllocateManagedMemory()
        {
            var h=new HordeSimulation(1000); h.Reset(1000,6741);
            for(int i=0;i<5;i++) h.Step(Vector3.zero,4.2f,1f/60);
            long before=System.GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<20;i++) h.Step(Vector3.zero,4.2f,1f/60);
            long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.EqualTo(0));
        }
        [Test] public void RampHeightMatchesTriangleInterpolation()
        {
            Assert.That(TechnicalWorld.Height(-10,20),Is.EqualTo(2));
            Assert.That(TechnicalWorld.Height(-10,29),Is.EqualTo(3.5f));
            Assert.That(TechnicalWorld.Normal(-10,20).y,Is.GreaterThan(Mathf.Cos(48*Mathf.Deg2Rad)));
            Assert.That(TechnicalWorld.Normal(10,19.5f).y,Is.LessThan(Mathf.Cos(48*Mathf.Deg2Rad)));
        }
    }
}
