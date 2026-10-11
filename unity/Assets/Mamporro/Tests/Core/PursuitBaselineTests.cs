using System;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B0: línea base del steering heredado (réplica web) en el circuito B, sin navegación.
    // Fija los defectos que P0-B corrige solo en modo P0; U3 conserva este comportamiento.
    public sealed class PursuitBaselineTests
    {
        static WorldRun Run(double x,double y,double z)
        {
            var r=new WorldRun(new VerticalCircuit(VerticalLayout.Persecucion).Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            r.Body.PlaceAt(x,y,z);r.SyncPlayer();r.Combat.WeaponsOff=true;return r;
        }
        [Test] public void LegacyEnemyWalksStraightUpAnAbruptTerrainCliff()
        {
            // Meseta de 5 m con paredes de ~79°: el heredado sigue la altura del terreno sin
            // límite de pendiente: una escalada encubierta a velocidad de carrera.
            var r=Run(16,5,10);r.Combat.Spawn(0,8,10);double steepest=0;var e=r.Combat.Enemies;
            for(int i=0;i<600&&e.Y[0]<4.9;i++){
                double x=e.X[0],y=e.Y[0],z=e.Z[0];r.Step(default,1.0/60);
                double run=Math.Sqrt((e.X[0]-x)*(e.X[0]-x)+(e.Z[0]-z)*(e.Z[0]-z));if(run>1e-6)steepest=Math.Max(steepest,(e.Y[0]-y)/run);
            }
            Assert.That(e.Y[0],Is.GreaterThan(4.9));
            double degrees=Math.Atan(steepest)*180/Math.PI;
            TestContext.Out.WriteLine($"heredado: sube la meseta caminando por una pendiente de {degrees:F1}°");
            Assert.That(degrees,Is.GreaterThan(Tuning.PlayerMaxSlopeDeg),"camina por encima del límite de 48°");
        }
        [Test] public void LegacyEnemyDropsOffARoofInOneTick()
        {
            var r=Run(0,0,6);r.Combat.Spawn(0,0,12);r.Combat.Enemies.Y[0]=4;double maxDrop=0;
            for(int i=0;i<600&&r.Combat.Enemies.Z[0]>9;i++){double y=r.Combat.Enemies.Y[0];r.Step(default,1.0/60);maxDrop=Math.Max(maxDrop,y-r.Combat.Enemies.Y[0]);}
            TestContext.Out.WriteLine($"heredado: mayor bajada en un tick {maxDrop:F3} m");
            Assert.That(maxDrop,Is.GreaterThan(3.9),"bajada instantánea de todo el tejado");
        }
        [Test] public void LegacyHordeCannotReachAClimbOnlyRoof()
        {
            var r=Run(-11,5,-11);double hp=r.Combat.Hp;r.Combat.Spawn(0,-11,-2);
            for(int i=0;i<1200;i++)r.Step(default,1.0/60);
            Assert.That(r.Combat.Hp,Is.EqualTo(hp));Assert.That(r.Combat.Enemies.Y[0],Is.LessThan(.6));
        }
        [Test] public void LegacyHordeOnlyFindsAnOffLineRampByBlindDetours()
        {
            // Desde el oeste la línea recta coincide con la rampa. Desde el este el edificio tapa
            // la línea: sin búsqueda de ruta, solo los desvíos giratorios ciegos la acaban
            // encontrando. Se registra el coste como referencia de la navegación P0-B.
            double west=TimeToHurt(-4),east=TimeToHurt(20);
            TestContext.Out.WriteLine($"heredado: oeste {west:F2} s, este {east:F2} s (20 s = no llega)");
            Assert.That(west,Is.LessThan(20));Assert.That(east,Is.GreaterThan(west*1.5));
        }
        static double TimeToHurt(double x)
        {
            var r=Run(11,4,-11);double hp=r.Combat.Hp;r.Combat.Spawn(0,x,-11);
            for(int i=0;i<1200;i++){r.Step(default,1.0/60);if(r.Combat.Hp<hp)return r.Combat.Time;}
            return 20;
        }
    }
}
