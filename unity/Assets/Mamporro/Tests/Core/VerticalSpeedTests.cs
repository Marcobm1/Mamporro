using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // Escalada = velocidad efectiva de carrera (estadística moveSpeed y frenada de horda),
    // en las cuatro direcciones y en diagonal, y robusta hasta el tope de la estadística.
    public sealed class VerticalSpeedTests
    {
        const double Dt=1.0/60;
        static readonly PlayerTuning T=PlayerTuning.Default;
        static readonly double MaxStat=PlayerStats.Limits[Stat.moveSpeed];
        static double Speed(double stat)=>Tuning.PlayerBaseMoveSpeed*stat;
        // Agarrado a media altura de la cara oeste del edificio (x=8), lejos de bordes.
        static VerticalMotion Gripped(out PlayerBody b,out VerticalCircuit c)
        {
            c=new VerticalCircuit();b=new PlayerBody();b.PlaceAt(7.598,2,-2);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},Dt);
            Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
            return m;
        }
        static double Moved(PlayerBody b,double x,double y,double z)=>Math.Sqrt((b.X-x)*(b.X-x)+(b.Y-y)*(b.Y-y)+(b.Z-z)*(b.Z-z));
        static void AssertClear(VerticalCircuit c,PlayerBody b,string label)=>
            Assert.That(c.Queries.Clear(new Vec3(b.X,b.Y,b.Z),T.Radius,T.Height),Is.True,$"{label}: cuerpo dentro de un sólido en {b.X:F3},{b.Y:F3},{b.Z:F3}");

        [TestCase(1.0,0.0)] [TestCase(1.26,0.0)] [TestCase(1.8,0.0)] [TestCase(1.0,.3)] [TestCase(1.5,.15)]
        public void EveryDirectionMovesAtTheEffectiveRunSpeed(double stat,double crowd)
        {
            double expected=PlayerPhysics.EffectiveSpeed(Speed(stat),crowd)*Dt;
            foreach(var (v,h) in new[]{(1.0,0.0),(-1.0,0.0),(0.0,-1.0),(0.0,1.0),(1.0,1.0),(1.0,-1.0),(-1.0,1.0),(-1.0,-1.0)}){
                var m=Gripped(out var b,out _);double x=b.X,y=b.Y,z=b.Z;
                m.Step(new VerticalIntent{WallVertical=v,WallHorizontal=h},Dt,Speed(stat),crowd);
                Assert.That(Moved(b,x,y,z),Is.EqualTo(expected).Within(1e-9),$"W/S {v}, A/D {h}");
                if(v!=0&&h!=0){
                    // Diagonal normalizada: cada eje aporta 1/√2, nunca más que la carrera.
                    Assert.That(Math.Abs(b.Y-y),Is.EqualTo(expected/Math.Sqrt(2)).Within(1e-9));
                    Assert.That(Math.Abs(b.Z-z),Is.EqualTo(expected/Math.Sqrt(2)).Within(1e-9));
                }
            }
        }

        [TestCase(1.0)] [TestCase(1.8)]
        public void ClimbingMatchesTheSteadyGroundRunSpeedFromTheSameStat(double stat)
        {
            // Carrera real por PlayerPhysics hasta velocidad estable, misma estadística.
            var c=new VerticalCircuit();var ground=new PlayerBody();ground.PlaceAt(-20,0,-20);ground.Grounded=true;
            for(int i=0;i<60;i++)PlayerPhysics.StepInCrowd(ground,new PlayerIntent{MoveX=1},c.Collision,T,Speed(stat),0,Dt);
            double run=JsMath.Hypot(ground.Vx,ground.Vz);
            Assert.That(run,Is.EqualTo(Speed(stat)).Within(1e-6));
            var m=Gripped(out var b,out _);double y=b.Y;
            m.Step(new VerticalIntent{WallVertical=1},Dt,Speed(stat));
            Assert.That((b.Y-y)/Dt,Is.EqualTo(run).Within(1e-6));
        }

        [Test] public void UpgradeAppliedOrRemovedMidClimbChangesTheNextTick()
        {
            // Las mejoras llegan cada tick como moveSpeed (WorldRun); no se guarda copia.
            var m=Gripped(out var b,out _);
            foreach(double stat in new[]{1.0,1.08,1.5,MaxStat,1.2,1.0}){
                double y=b.Y;m.Step(new VerticalIntent{WallVertical=stat>1.4?-1:1},Dt,Speed(stat));
                Assert.That(Math.Abs(b.Y-y),Is.EqualTo(Speed(stat)*Dt).Within(1e-9),$"estadística {stat}");
            }
        }

        [Test] public void StatsUpgradeReachesClimbThroughTheRunPipeline()
        {
            // Fuente de verdad real: PlayerStats aplica la mejora y su tope; WorldRun pasa
            // PlayerBaseMoveSpeed*Stats[moveSpeed]. Aquí se reproduce esa misma lectura.
            var stats=new PlayerStats();stats.Reset(Catalog.Characters[0]);
            var m=Gripped(out var b,out _);
            for(int tome=0;tome<=12;tome++){
                double y=b.Y;m.Step(new VerticalIntent{WallVertical=tome%2==0?1:-1},Dt,Tuning.PlayerBaseMoveSpeed*stats[Stat.moveSpeed]);
                Assert.That(Math.Abs(b.Y-y)/Dt,Is.EqualTo(Tuning.PlayerBaseMoveSpeed*stats[Stat.moveSpeed]).Within(1e-6));
                stats[Stat.moveSpeed]=Math.Min(MaxStat,stats[Stat.moveSpeed]+.08);
            }
            Assert.That(stats[Stat.moveSpeed],Is.EqualTo(MaxStat).Within(1e-9));
        }

        [TestCase(1.0)] [TestCase(1.4)] [TestCase(1.8)]
        public void RoofLedgeCompletesFromAnyTickPhaseWithoutPenetration(double stat)
        {
            // A 17 m/s un tick sube 0,285 m (> ventana fija de 0,2 m). Desde cualquier
            // fase de partida el borde debe detectarse y terminar arriba, sin atravesar nada.
            for(double start=1;start<1.3;start+=.023){
                var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(7.598,start,-2);b.Grounded=false;
                var m=new VerticalMotion(b,c.Collision,c.Queries);
                for(int i=0;i<240&&m.LedgeSuccess==0;i++){
                    m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},Dt,Speed(stat));
                    AssertClear(c,b,$"estadística {stat}, inicio {start:F3}, tick {i}");
                }
                Assert.That(m.LedgeSuccess,Is.EqualTo(1),$"estadística {stat}, inicio {start:F3}: {m.State} {m.LastFailure} y={b.Y:F3}");
                Assert.That(b.Y,Is.EqualTo(6.002).Within(1e-6));Assert.That(m.LedgeFailures,Is.Zero);
            }
        }

        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        public void PitExitsOnEverySideAtMaximumSpeed(double dx,double dz)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(19,-6,11);b.Grounded=true;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<300&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},Dt,Speed(MaxStat));
                AssertClear(c,b,$"foso ({dx},{dz}) tick {i}");
            }
            Assert.That(m.LedgeSuccess,Is.EqualTo(1),$"{m.State} {m.LastFailure} {b.X:F2},{b.Y:F2},{b.Z:F2}");
            Assert.That(b.Y,Is.EqualTo(.002).Within(.003));
        }

        [TestCase(-8,-7,0,1)] [TestCase(-8,7,0,-1)] [TestCase(-1,0,-1,0)] [TestCase(-18,-7,0,1)]
        public void TerrainWallsCompleteAtMaximumSpeed(double x,double z,double dx,double dz)
        {
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(x,0,z);b.Grounded=true;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<300&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1},Dt,Speed(MaxStat));
                AssertClear(c,b,$"terreno ({x},{z}) tick {i}");
            }
            Assert.That(m.LedgeSuccess,Is.EqualTo(1),$"{m.State} {m.LastFailure} {b.X:F2},{b.Y:F2},{b.Z:F2}");
        }

        [Test] public void CeilingAndInteriorCornerBlockAtMaximumSpeed()
        {
            // Techo central (y=7) sobre la cara oeste: el ascenso se detiene sin entrar.
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(7.598,0,0);b.Grounded=true;
            var m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<180;i++){m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},Dt,Speed(MaxStat));AssertClear(c,b,$"techo tick {i}");}
            Assert.That(m.LedgeSuccess,Is.Zero);Assert.That(b.Y+T.Height,Is.LessThanOrEqualTo(7+1e-6));
            // Esquina interior caja 3/caja 4: el lateral rápido se detiene en la junta.
            b=new PlayerBody();b.PlaceAt(7.598,1,12);b.Grounded=false;m=new VerticalMotion(b,c.Collision,c.Queries);
            for(int i=0;i<120;i++){m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallHorizontal=1},Dt,Speed(MaxStat));AssertClear(c,b,$"esquina tick {i}");}
            Assert.That(m.Grabs,Is.GreaterThan(0));Assert.That(b.Z,Is.LessThanOrEqualTo(14-T.Radius+1e-6));
        }

        [Test] public void JointBetweenTwoBoxesIsCrossedAtMaximumSpeed()
        {
            // Pared de 6 m dividida en dos cajas: a 17 m/s el lateral cruza la junta sin soltarse.
            var q=new VerticalQueries(new VerticalSolid(201,new Vec3(0,0,-3),new Vec3(2,4,0)),new VerticalSolid(712,new Vec3(0,0,0),new Vec3(2,4,3)));
            var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(-.402,1,-2.5);b.Grounded=false;
            var m=new VerticalMotion(b,c.Collision,q);m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},Dt);
            for(int i=0;i<15;i++){double z=b.Z;m.Step(new VerticalIntent{WallHorizontal=1},Dt,Speed(MaxStat));Assert.That(b.Z,Is.GreaterThan(z),$"tick {i}");}
            Assert.That(b.Z,Is.GreaterThan(1.5));Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));Assert.That(m.Grabs,Is.EqualTo(1));
        }

        [TestCase(1.0)] [TestCase(1.8)]
        public void FastDescentStopsOnTheGroundWithoutSinking(double stat)
        {
            var m=Gripped(out var b,out var c);
            for(int i=0;i<120&&m.State==VerticalState.Climbing;i++){
                m.Step(new VerticalIntent{WallVertical=-1},Dt,Speed(stat));
                Assert.That(b.Y,Is.GreaterThanOrEqualTo(c.Terrain.HeightAt(b.X,b.Z)-1e-9),$"tick {i}");
            }
            Assert.That(m.State,Is.EqualTo(VerticalState.Ground));Assert.That(b.Y,Is.EqualTo(0).Within(1e-9));
            // Bajar hasta el suelo no rearma la escalada mientras se mantiene S.
            m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=-1},Dt,Speed(stat));
            Assert.That(m.State,Is.EqualTo(VerticalState.Ground));
        }

        [Test] public void JumpProtectionHoldsAtMaximumSpeed()
        {
            var m=Gripped(out var b,out var c);
            m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1,JumpPressed=true}},Dt,Speed(MaxStat));
            Assert.That(m.State,Is.EqualTo(VerticalState.Air));
            double t=0;int grabs=m.Grabs;
            for(int i=0;i<180&&m.Grabs==grabs;i++){m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},Dt,Speed(MaxStat));t+=Dt;AssertClear(c,b,$"tick {i}");}
            Assert.That(m.Grabs,Is.EqualTo(grabs+1),"reengancha al volver contra la pared");
            Assert.That(t,Is.GreaterThanOrEqualTo(m.Tuning.JumpReattachDelay-1e-9),"no antes de la protección");
        }

        [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        public void CameraLeftRightKeepsSignAtMaximumSpeed(double nx,double nz)
        {
            foreach(double facing in new[]{1.0,-1.0})foreach(double side in new[]{-1.0,1.0}){
                var c=new VerticalCircuit();var b=new PlayerBody();b.PlaceAt(11+nx*3.402,2,nz*4.402);b.Grounded=false;
                var m=new VerticalMotion(b,c.Collision,c.Queries);
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=-nx,MoveZ=-nz}},Dt);
                var r=VerticalMotion.WallRight(m.Contact.Normal);double cx=r.X*facing,cz=r.Z*facing,x=b.X,z=b.Z;
                m.Step(new VerticalIntent{WallHorizontal=side,CameraRightX=cx,CameraRightZ=cz},Dt,Speed(MaxStat));
                Assert.That((b.X-x)*cx+(b.Z-z)*cz,Is.EqualTo(side*Speed(MaxStat)*Dt).Within(1e-9));
            }
        }
    }
}
