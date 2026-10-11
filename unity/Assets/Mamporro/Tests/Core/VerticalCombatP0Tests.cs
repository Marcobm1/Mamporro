using System;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B5: combate en altura con navegación P0 (circuito B). Las sondas A4 de VerticalCombatTests
    // siguen documentando el heredado sin Vertical. Daño, alcance, cadencia y velocidad sin cambios:
    // cada filtro tiene su control positivo a la misma altura.
    public sealed class VerticalCombatP0Tests
    {
        static VerticalCircuit circuit;static NavGraph[] graphs;
        static WorldRun Run(double x,double y,double z,out EnemyNavigation nav)
        {
            if(circuit==null){circuit=new VerticalCircuit(VerticalLayout.Persecucion);graphs=EnemyNavigation.BuildGraphs(circuit);}
            var r=new WorldRun(circuit.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            nav=new EnemyNavigation(circuit,r.Combat.Enemies.Capacity,graphs);r.Combat.Enemies.Navigation=nav;r.Combat.Vertical=nav;
            r.Body.PlaceAt(x,y,z);r.SyncPlayer();r.Combat.WeaponsOff=true;return r;
        }
        static int Spawn(WorldRun r,int type,double x,double y,double z){int i=r.Combat.Spawn(type,x,z);r.Combat.Enemies.Y[i]=(float)y;r.Combat.Enemies.Py[i]=(float)y;return i;}

        [TestCase("barra")] [TestCase("naftalina")] [TestCase("jersey")]
        public void MeleeAndAreaWeaponsHitTheirLevelButNotFiveMetersBelow(string weapon)
        {
            foreach(bool sameLevel in new[]{false,true}){
                var r=Run(-8.6,5,-11,out _);var c=r.Combat;c.QaWeapons(weapon);c.WeaponsOff=false;c.Invincible=true;
                int i=sameLevel?Spawn(r,0,-9.4,5,-11):Spawn(r,0,-7.3,0,-11);c.Enemies.Speed[i]=0;float hp=c.Enemies.Hp[i];
                // La Barra golpea en el cono de 75° hacia donde mira el jugador: mirando al enemigo.
                c.Player.Facing=sameLevel?Math.PI/2:-Math.PI/2;
                for(int t=0;t<40;t++)c.Step(1.0/60);
                TestContext.Out.WriteLine($"{weapon} {(sameLevel?"misma altura":"5 m debajo")}: daño {hp-c.Enemies.Hp[i]}");
                Assert.That(c.Enemies.Hp[i]<hp,Is.EqualTo(sameLevel),$"{weapon}, misma altura={sameLevel}");
            }
        }
        [Test] public void FriendlyProjectileHitsItsLevelAndStopsAtWalls()
        {
            var r=Run(-8.6,5,-11,out _);var c=r.Combat;
            int below=Spawn(r,0,-7.3,0,-11);c.Enemies.Speed[below]=0;float hp=c.Enemies.Hp[below];
            c.Projectiles.Spawn(-8.6,6.1,-11,1,0,10,2,.3,weapon:0);for(int t=0;t<12;t++)c.Projectiles.Step(1.0/60,c.Enemies,r.World.Collision,c);
            Assert.That(c.Enemies.Hp[below],Is.EqualTo(hp),"pasa por encima del enemigo 5 m debajo");
            var level=Run(-8.6,5,-11,out _);var l=level.Combat;int same=Spawn(level,0,-10.4,5,-11);l.Enemies.Speed[same]=0;hp=l.Enemies.Hp[same];
            l.Projectiles.Spawn(-8.6,6.1,-11,-1,0,10,2,.3,weapon:0);for(int t=0;t<12;t++)l.Projectiles.Step(1.0/60,l.Enemies,level.World.Collision,l);
            Assert.That(l.Enemies.Hp[same],Is.LessThan(hp),"impacta a su altura");
            // Desde el oeste hacia un enemigo tras el edificio C: el muro detiene el proyectil.
            var wall=Run(-17,0,-11,out _);var w=wall.Combat;int hidden=Spawn(wall,0,-6,0,-11);w.Enemies.Speed[hidden]=0;hp=w.Enemies.Hp[hidden];
            w.Projectiles.Spawn(-17,1.1,-11,1,0,10,2,.3,weapon:0);for(int t=0;t<120&&w.Projectiles.Count>0;t++)w.Projectiles.Step(1.0/60,w.Enemies,wall.World.Collision,w);
            Assert.That(w.Projectiles.Count,Is.Zero,"se detuvo");Assert.That(w.Enemies.Hp[hidden],Is.EqualTo(hp),"no atraviesa el muro");
        }
        [Test] public void HostileShotReachesARoofWithLineOfSightAndStopsAtWallsWithoutHoming()
        {
            // Paloma en el suelo al norte del edificio C; jugador en su tejado: el disparo sube en línea recta.
            var r=Run(-11,5,-8.6,out _);var c=r.Combat;int i=Spawn(r,3,-11,0,-1);
            c.Shoot(i,0,-1);var s=c.EnemyShots;Assert.That(s.Count,Is.EqualTo(1));
            Assert.That(s.Y[0],Is.EqualTo(1).Within(1e-4),"sale a la altura real del tirador");Assert.That(s.Vy[0],Is.GreaterThan(0),"apunta a la altura del jugador");
            float vx=s.Vx[0],vy=s.Vy[0],vz=s.Vz[0];double hit=0;
            r.Body.PlaceAt(-12,5,-9.5);r.SyncPlayer();// moverse tras el disparo: sin seguimiento
            for(int t=0;t<120&&s.Count>0&&hit==0;t++){if(s.Count>0){Assert.That(s.Vx[0],Is.EqualTo(vx));Assert.That(s.Vy[0],Is.EqualTo(vy));Assert.That(s.Vz[0],Is.EqualTo(vz));}hit=s.StepHostile(1.0/60,r.World.Collision,c.Player);}
            var aligned=Run(-11,5,-8.6,out _);var a=aligned.Combat;int j=Spawn(aligned,3,-11,0,-1);a.Shoot(j,0,-1);hit=0;
            for(int t=0;t<120&&a.EnemyShots.Count>0&&hit==0;t++)hit=a.EnemyShots.StepHostile(1.0/60,aligned.World.Collision,a.Player);
            Assert.That(hit,Is.GreaterThan(0),"alcanza al jugador del tejado con línea de visión");
            var blocked=Run(-11,0,-17,out _);var b=blocked.Combat;int k=Spawn(blocked,3,-11,0,-3);b.Shoot(k,0,-1);hit=0;
            for(int t=0;t<120&&b.EnemyShots.Count>0;t++)hit=Math.Max(hit,b.EnemyShots.StepHostile(1.0/60,blocked.World.Collision,b.Player));
            Assert.That(hit,Is.Zero,"el edificio C detiene el disparo");Assert.That(b.EnemyShots.Count,Is.Zero);
        }
        [Test] public void BossMinionsAppearOnTheBossSurface()
        {
            var r=Run(0,0,-4,out _);var c=r.Combat;c.SpawnMinion(2,16,4);var e=c.Enemies;
            Assert.That(e.Count,Is.EqualTo(1));Assert.That(e.Y[0],Is.EqualTo(4).Within(1e-4),"sobre el tejado del jefe");
            Assert.That(circuit.Queries.ClearAboveStep(new Vec3(e.X[0],e.Y[0],e.Z[0]),e.Def(0).radius,e.Def(0).height,NavGraph.Step),Is.True);
        }
        [Test] public void ClimbingEnemiesDoNotHitUntilTheyStandOnTheRoof()
        {
            // Jugador a 0,4 m del borde este del tejado C; el enemigo sube por esa cara.
            var r=Run(-8.4,5,-11,out var nav);var c=r.Combat;double hp=c.Hp;int i=Spawn(r,0,-4,0,-11);bool hurtClimbing=false;
            for(int t=0;t<900&&c.Hp==hp;t++){double before=c.Hp;r.Step(default,1.0/60);if(c.Hp<before&&!nav.Stable(i))hurtClimbing=true;}
            Assert.That(hurtClimbing,Is.False,"sin contacto desde la pared ni en el borde");Assert.That(c.Hp,Is.LessThan(hp),"golpea al llegar arriba");
            Assert.That(c.Enemies.Y[i],Is.EqualTo(5).Within(.01));
        }
    }
}
