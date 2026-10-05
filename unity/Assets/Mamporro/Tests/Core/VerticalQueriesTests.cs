using System;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class VerticalQueriesTests
    {
        static VerticalSolid Box(int id,double x0,double y0,double z0,double x1,double y1,double z1,VerticalSurfaceKind kind=VerticalSurfaceKind.Structure)=>
            new VerticalSolid(id,kind,new Vec3(x0,y0,z0),new Vec3(x1,y1,z1));
        static VerticalQueries WallWorld()=>new VerticalQueries(Box(1,0,0,-3,3,4,3));
        [Test] public void SweepStopsAtThinWallEvenWithHighSpeed()
        {
            var w=new VerticalQueries(Box(7,0,0,-2,.01,8,2));
            Assert.That(w.Sweep(new Vec3(-10,1,0),new Vec3(10,1,0),.4,1.55,out var h),Is.True);
            Assert.That(h.Fraction,Is.EqualTo(.48).Within(1e-10));Assert.That(h.Normal.X,Is.EqualTo(-1));Assert.That(h.Id,Is.EqualTo(7));
        }
        [Test] public void TangentialMotionAndMovingAwayDoNotStick()
        {
            var w=WallWorld();var p=new Vec3(-.4,1,0);
            Assert.That(w.Sweep(p,new Vec3(-.4,2,0),.4,1.55,out _),Is.False);
            Assert.That(w.Sweep(p,new Vec3(-2,1,0),.4,1.55,out _),Is.False);
            Assert.That(w.Sweep(p,new Vec3(1,1,0),.4,1.55,out var h),Is.True);Assert.That(h.Fraction,Is.Zero);
        }
        [Test] public void CeilingBlocksWholeBody()
        {
            var w=new VerticalQueries(Box(3,-2,2,-2,2,2.2,2));
            Assert.That(w.Sweep(new Vec3(0,0,0),new Vec3(0,4,0),.4,1.55,out var h),Is.True);
            Assert.That(h.Fraction,Is.EqualTo(.1125).Within(1e-10));Assert.That(h.Normal.Y,Is.EqualTo(-1));
        }
        [TestCase(VerticalSurfaceKind.Structure,true)] [TestCase(VerticalSurfaceKind.Cliff,true)]
        [TestCase(VerticalSurfaceKind.Foliage,false)] [TestCase(VerticalSurfaceKind.Interactable,false)] [TestCase(VerticalSurfaceKind.Boundary,false)]
        public void ClimbableKindsAreExplicit(VerticalSurfaceKind kind,bool expected)
        {
            var w=new VerticalQueries(Box(1,0,0,-2,2,4,2,kind));
            Assert.That(w.Wall(new Vec3(-.5,1,0),new Vec3(1,0,0),.4,1.55,.3,out _),Is.EqualTo(expected));
        }
        [Test] public void CannotGrabThroughFirstNonClimbableSolid()
        {
            var w=new VerticalQueries(Box(2,-.2,0,-2,-.1,4,2,VerticalSurfaceKind.Boundary),Box(1,0,0,-2,2,4,2));
            Assert.That(w.Wall(new Vec3(-1,1,0),new Vec3(1,0,0),.4,1.55,2,out _),Is.False);
        }
        [Test] public void LedgeRequiresFreeBodyAndCompleteRoute()
        {
            var w=WallWorld();var p=new Vec3(-.402,3,0);
            Assert.That(w.Wall(p,new Vec3(1,0,0),.4,1.55,.2,out var h),Is.True);
            Assert.That(w.Ledge(p,h,.4,1.55,out var lift,out var top),Is.True);
            Assert.That(lift.Y,Is.EqualTo(4.002));Assert.That(top.X,Is.GreaterThan(.4));
            var blocked=new VerticalQueries(w.Solid(0),Box(2,-1,5,-2,3,5.2,2));
            Assert.That(blocked.Ledge(p,h,.4,1.55,out _,out _),Is.False);
        }
        [Test] public void NarrowTopDoesNotSupportFullFootprint()
        {
            var w=new VerticalQueries(Box(1,0,0,0,.5,4,.5));
            Assert.That(w.Support(new Vec3(.25,4,.25),.4,.01,out _),Is.False);
        }
        [Test] public void InteriorCornerBlocksLateralSweep()
        {
            var w=new VerticalQueries(Box(1,0,0,-3,3,4,3),Box(2,-3,0,1,0,4,2));
            Assert.That(w.Sweep(new Vec3(-.4,1,0),new Vec3(-.4,1,3),.4,1.55,out var h),Is.True);
            Assert.That(h.Id,Is.EqualTo(2));Assert.That(h.Normal.Z,Is.EqualTo(-1));
        }
        [Test] public void ExteriorCornerCannotSupportAnExitInEmptySpace()
        {
            var w=WallWorld();var p=new Vec3(-.402,3,3.3);
            Assert.That(w.Wall(p,new Vec3(1,0,0),.4,1.55,.2,out var h),Is.True);
            Assert.That(w.Ledge(p,h,.4,1.55,out _,out _),Is.False);
        }
        [Test] public void CircuitHasConnectedRampAndDistinctSurfaceKinds()
        {
            var c=new VerticalCircuit();Assert.That(c.Terrain.HeightAt(-24,0),Is.Zero);
            Assert.That(c.Terrain.HeightAt(-18,0),Is.EqualTo(2));Assert.That(c.Terrain.HeightAt(-8,0),Is.EqualTo(4));
            Assert.That(c.Queries.Count,Is.EqualTo(10));
            Assert.That(c.Collision.GroundHeight(10,0,6.01),Is.EqualTo(6));
            Assert.That(c.Collision.GroundHeight(10,0,0.45),Is.Zero,"suelo debajo del techo, sin colocar arriba");
            Assert.That(c.Queries.Clear(c.Start,.4,1.55),Is.True);
            Assert.That(c.Collision.IsInside(c.Start.X,c.Start.Z,1),Is.True);
        }
        [Test] public void InvalidAndAmbiguousInputIsRejected()
        {
            Assert.Throws<ArgumentException>(()=>new VerticalQueries(Box(1,0,0,0,1,1,1),Box(1,2,0,0,3,1,1)));
            Assert.Throws<ArgumentException>(()=>WallWorld().Clear(new Vec3(double.NaN,0,0),.4,1.55));
            Assert.Throws<ArgumentException>(()=>WallWorld().Sweep(default,default,-1,1,out _));
        }
    }
}
