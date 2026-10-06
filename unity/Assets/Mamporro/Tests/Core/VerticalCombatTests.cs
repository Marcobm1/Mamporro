using System;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // Caracterización P0-A: los defectos verticales se registran, no se corrigen
    // cambiando las reglas aprobadas ni se confunden con amenaza válida.
    public sealed class VerticalCombatTests
    {
        static WorldRun Run(double x,double y,double z)
        {
            var r=new WorldRun(new VerticalCircuit().Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            r.Body.PlaceAt(x,y,z);r.SyncPlayer();r.Combat.WeaponsOff=true;return r;
        }
        static void Simulate(WorldRun r,int ticks)
        {
            for(int i=0;i<ticks;i++){r.Step(default,1.0/60);if(r.Combat.Dead)break;}
        }
        [Test] public void GroundContactStillHurtsAtSameHeight()
        {
            var r=Run(4,0,-10);double hp=r.Combat.Hp;r.Combat.Spawn(0,4.6,-10);Simulate(r,1);
            Assert.That(r.Combat.Hp,Is.LessThan(hp));
        }
        [Test] public void EnemyCanReachAccessibleRoofByDirectRamp()
        {
            var r=Run(-8,4.25,0);double hp=r.Combat.Hp;r.Combat.Spawn(0,-23,0);Simulate(r,900);
            Assert.That(r.Combat.Enemies.Y[0],Is.EqualTo(4.25).Within(.01));
            Assert.That(r.Combat.Hp,Is.LessThan(hp),"contacto real tras recorrer la rampa");
            TestContext.WriteLine($"rampa: t={r.Combat.Time:F3}; enemigoY={r.Combat.Enemies.Y[0]}; hp={r.Combat.Hp}");
        }
        [Test] public void InaccessibleRoofRemainsSafeFromGroundMeleeForThirtySeconds()
        {
            var r=Run(11,6,3);double hp=r.Combat.Hp;r.Combat.Spawn(0,6,3);Simulate(r,1800);
            Assert.That(r.Combat.Enemies.Y[0],Is.LessThan(.6));Assert.That(r.Combat.Hp,Is.EqualTo(hp));
            Assert.That(r.Combat.Time,Is.EqualTo(30).Within(1e-8));
            TestContext.WriteLine("30 s sin daño: tejado sin acceso; el steering no escala ni teletransporta.");
        }
        [TestCase("barra",false)] [TestCase("naftalina",true)] [TestCase("jersey",true)]
        public void ExistingWeaponsExposeTheirActualVerticalFilters(string weapon,bool damagesBelow)
        {
            var r=Run(8.5,6,3);var c=r.Combat;c.QaWeapons(weapon);c.WeaponsOff=false;c.Invincible=true;
            c.Spawn(0,7.5,3);float hp=c.Enemies.Hp[0];for(int i=0;i<24;i++)c.Step(1.0/60);
            Assert.That(c.Enemies.Hp[0]<hp,Is.EqualTo(damagesBelow));
            TestContext.WriteLine($"{weapon}: daño con separación vertical 6 m = {hp-c.Enemies.Hp[0]}; filtro/limitación existente.");
        }
        [Test] public void HostileProjectileFollowsTerrainAndCannotThreatenTheRoof()
        {
            var r=Run(8.5,6,3);var shots=r.Combat.EnemyShots;
            shots.Spawn(7.5,1,3,1,0,10,2,.3,damage:8);
            double hit=shots.StepHostile(.1,r.World.Collision,r.Combat.Player);
            Assert.That(hit,Is.Zero);Assert.That(shots.Y[0],Is.EqualTo(1));
            Assert.That(shots.X[0],Is.EqualTo(8.5),"atraviesa la pared, no resuelve sólidos");
            TestContext.WriteLine("Proyectil hostil: sigue heightfield+1, atraviesa paredes y no amenaza este tejado de 6 m.");
        }
        [Test] public void FriendlyProjectileCanHitAnEnemySixMetersBelow()
        {
            var r=Run(8.5,6,3);var c=r.Combat;c.Spawn(0,7.5,3);float hp=c.Enemies.Hp[0];
            c.Projectiles.Spawn(8.5,7.1,3,-1,0,10,2,.3,weapon:0);
            c.Projectiles.Step(.1,c.Enemies,r.World.Collision,c);
            Assert.That(c.Enemies.Hp[0],Is.LessThan(hp),"fallo conocido: impacto XZ sin filtro Y");
        }
        [Test] public void TrailKeepsItsHeightAndCannotDamageThroughRoof()
        {
            var r=Run(8.5,6,3);var c=r.Combat;c.QaWeapons("fregona");c.WeaponsOff=false;
            c.Spawn(0,7.5,3);float hp=c.Enemies.Hp[0];c.StateOf(0).Add(7.5,6,3,2,5);for(int i=0;i<24;i++)c.Step(1.0/60);
            Assert.That(c.Enemies.Hp[0],Is.EqualTo(hp));
        }
    }
}
