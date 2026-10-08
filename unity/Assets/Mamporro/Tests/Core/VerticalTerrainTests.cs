using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class VerticalTerrainTests
    {
        static Heightfield Generated(double dx,double dz)
        {
            const int cells=40;var heights=new float[(cells+1)*(cells+1)];
            for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++)
                heights[z*(cells+1)+x]=(float)Math.Max(0,Math.Min(4,4*(dx*(x*.5-10)+dz*(z*.5-10))));
            return new Heightfield(20,cells,heights);
        }
        static VerticalMotion Motion(Heightfield terrain,double x,double z,double y,out PlayerBody body,out VerticalQueries query)
        {
            query=new VerticalQueries(terrain,48);body=new PlayerBody();body.PlaceAt(x,y,z);body.Grounded=y==terrain.HeightAt(x,z);
            return new VerticalMotion(body,new WorldCollision(terrain,new ColliderGrid(new List<Collider>(),10),9),query);
        }
        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)] [TestCase(.6,.8)] [TestCase(-.6,-.8)]
        public void GeneratedPhysicalCliffClimbsAndExitsWithoutRegistration(double dx,double dz)
        {
            var m=Motion(Generated(dx,dz),-dx*2,-dz*2,0,out var b,out var q);
            for(int i=0;i<360&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},1.0/60);
                Assert.That(q.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True,$"tick {i}: {b.X},{b.Y},{b.Z}");
            }
            Assert.That(m.Grabs,Is.GreaterThan(0));Assert.That(m.LedgeSuccess,Is.EqualTo(1),$"{b.X},{b.Y},{b.Z}, {m.State}, {m.LastFailure}");
            Assert.That(b.Y,Is.EqualTo(4.002).Within(.003));Assert.That(m.Stalls,Is.Zero);
        }
        [TestCase(-8,-7,0,1)] [TestCase(-8,7,0,-1)] [TestCase(-1,0,-1,0)]
        [TestCase(-18,-7,0,1)] [TestCase(-18,7,0,-1)]
        public void RealPlateauAndRampSidesCanBeClimbed(double x,double z,double dx,double dz)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(x,0,z);var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<360&&m.LedgeSuccess==0&&b.Y<1.8;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},1.0/60);
            Assert.That(m.Grabs,Is.GreaterThan(0));Assert.That(b.Y,Is.GreaterThanOrEqualTo(1.8),$"{b.X},{b.Y},{b.Z}, {m.State}, {m.LastFailure}");
            for(int i=0;i<240&&m.LedgeSuccess==0;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},1.0/60);
            Assert.That(m.LedgeSuccess,Is.EqualTo(1),$"{b.X},{b.Y},{b.Z}, {m.State}, {m.LastFailure}");
        }
        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)] [TestCase(.6,.8)]
        public void TerrainLeftRightUsesSignedTangentAndVerticalSpeed(double dx,double dz)
        {
            foreach(double side in new[]{-1.0,1.0}){
                var m=Motion(Generated(dx,dz),-dx*2,-dz*2,0,out var b,out var q);
                for(int i=0;i<180&&b.Y<1;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},1.0/60);
                Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
                Assert.That(q.Wall(new Vec3(b.X,b.Y,b.Z),new Vec3(dx,0,dz),.4,1.55,.18,out var hit),Is.True);
                var right=VerticalMotion.WallRight(hit.Normal);double x=b.X,y=b.Y,z=b.Z;
                m.Step(new VerticalIntent{WallHorizontal=side},1.0/60);
                Assert.That((b.X-x)*right.X+(b.Z-z)*right.Z,Is.EqualTo(side*3/60).Within(1e-6));
                y=b.Y;m.Step(new VerticalIntent{WallVertical=1},1.0/60);
                Assert.That(b.Y-y,Is.EqualTo(4.5/60).Within(1e-6));
            }
        }
        [TestCase(47.99,false)] [TestCase(48,false)] [TestCase(48.01,true)] [TestCase(90,true)] [TestCase(100,false)] [TestCase(0,false)] [TestCase(180,false)]
        public void WallSlopeThresholdExcludesFloorsAndUndersides(double degrees,bool expected)
        {
            var q=new VerticalQueries();double radians=degrees*Math.PI/180;
            Assert.That(q.IsWallNormal(new Vec3(Math.Sin(radians),Math.Cos(radians),0)),Is.EqualTo(expected));
            Assert.That(q.IsWallNormal(default),Is.False);
        }
        [Test] public void TerrainSweepCannotSkipCliffAndTangentialContactIsFree()
        {
            var q=new VerticalQueries(Generated(1,0),48);
            Assert.That(q.Sweep(new Vec3(-5,1,0),new Vec3(5,1,0),.4,1.55,out var hit),Is.True);
            Assert.That(hit.Terrain,Is.True);Assert.That(hit.Normal.X,Is.LessThan(0));
            double x=-5+10*hit.Fraction-.002;
            Assert.That(q.Clear(new Vec3(x,1,0),.4,1.55),Is.True);
            Assert.That(q.Sweep(new Vec3(x,1,0),new Vec3(x,1,1),.4,1.55,out _),Is.False);
            Assert.That(q.Sweep(new Vec3(x,1,0),new Vec3(x-1,1,0),.4,1.55,out _),Is.False);
        }
        [Test] public void SupportCanSpanAdjacentSolidTopsButNotAGap()
        {
            var left=new VerticalSolid(28,new Vec3(-2,0,-2),new Vec3(0,4,2));
            var right=new VerticalSolid(147,new Vec3(0,0,-2),new Vec3(2,4,2));
            Assert.That(new VerticalQueries(left,right).Support(new Vec3(0,4,0),.4,.003,out _),Is.True);
            right=new VerticalSolid(148,new Vec3(.1,0,-2),new Vec3(2,4,2));
            Assert.That(new VerticalQueries(left,right).Support(new Vec3(0,4,0),.4,.003,out _),Is.False);
        }
        [Test] public void TerrainContactInAirNeedsFrontalIntent()
        {
            foreach(double push in new[]{0,.2,1}){
                var m=Motion(Generated(1,0),-.152,0,1,out _,out _);
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=push,MoveZ=push==1?0:1}},1.0/60);
                Assert.That(m.Grabs,Is.EqualTo(push==1?1:0));
            }
        }
        [Test] public void TerrainJumpDoesNotReattachThroughAdjacentTriangles()
        {
            var m=Motion(Generated(1,0),-2,.24,0,out var b,out _);
            for(int i=0;i<180&&b.Y<1;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},1.0/60);
            m.Step(new VerticalIntent{Movement=new PlayerIntent{JumpPressed=true}},1.0/60);
            int grabs=m.Grabs;
            for(int i=0;i<20;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1,MoveZ=.4},WallVertical=1},1.0/60);
                Assert.That(m.Grabs,Is.EqualTo(grabs));
            }
            for(int i=0;i<300&&m.LedgeSuccess==0;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},1.0/60);
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));
        }
        [Test] public void AdjacentWallsKeepTheSameLeftRightMeaning()
        {
            var q=new VerticalQueries(new VerticalSolid(201,new Vec3(0,0,-3),new Vec3(2,4,0)),new VerticalSolid(712,new Vec3(0,0,0),new Vec3(2,4,3)));
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(-.402,1,-1);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,q);m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},1.0/60);
            for(int i=0;i<40;i++){double z=b.Z;m.Step(new VerticalIntent{WallHorizontal=1},1.0/60);Assert.That(b.Z,Is.GreaterThan(z));}
            Assert.That(b.Z,Is.GreaterThan(.9));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));Assert.That(m.Grabs,Is.EqualTo(1));
            for(int i=0;i<40;i++){double z=b.Z;m.Step(new VerticalIntent{WallHorizontal=-1},1.0/60);Assert.That(b.Z,Is.LessThan(z));}
        }
        [Test] public void PlateauRegressionDemonstratesMissingGeometryInBoxesOnlyQueries()
        {
            var c=new VerticalCircuit();var boxes=new VerticalSolid[c.Queries.Count];for(int i=0;i<boxes.Length;i++)boxes[i]=c.Queries.Solid(i);
            var feet=new Vec3(-8,1,5.152);var toward=new Vec3(0,0,-1);
            Assert.That(new VerticalQueries(boxes).Wall(feet,toward,.4,1.55,.18,out _),Is.False,"A6 anterior: terreno ausente");
            Assert.That(c.Queries.Wall(feet,toward,.4,1.55,.18,out var contact),Is.True);
            Assert.That(contact.Terrain,Is.True);Assert.That(contact.Normal.Z,Is.GreaterThan(0));
        }
        [TestCase(false)] [TestCase(true)]
        public void CornersNeverReverseLateralInputAndCanBeLeft(bool interior)
        {
            var main=new VerticalSolid(51,new Vec3(0,0,-3),new Vec3(3,4,3));
            var q=interior?new VerticalQueries(main,new VerticalSolid(82,new Vec3(-3,0,1),new Vec3(0,4,2))):new VerticalQueries(main);
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(-.402,1,0);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,q);m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},1.0/60);
            for(int i=0;i<100&&m.State==VerticalState.Climbing;i++){
                double z=b.Z;m.Step(new VerticalIntent{WallHorizontal=1},1.0/60);
                Assert.That(b.Z,Is.GreaterThanOrEqualTo(z));Assert.That(q.Clear(new Vec3(b.X,b.Y,b.Z),.4,1.55),Is.True);
            }
            if(interior){
                Assert.That(m.Stalls,Is.GreaterThan(0));double z=b.Z;
                m.Step(new VerticalIntent{WallHorizontal=-1},1.0/60);Assert.That(b.Z,Is.LessThan(z));
                m.Step(new VerticalIntent{Movement=new PlayerIntent{JumpPressed=true}},1.0/60);
            }
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));
        }
        [Test] public void UpperTerrainIsSupportAndCannotBeGrabbedAsWall()
        {
            var q=new VerticalQueries(Generated(1,0),48);var top=new Vec3(2,4.002,0);
            Assert.That(q.Support(top,.4,.003,out _),Is.True);Assert.That(q.Support(top,0,.003,out _),Is.True);
            Assert.That(q.Wall(top,new Vec3(1,0,0),.4,1.55,.18,out _),Is.False);
        }
    }
}
