using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B3: horda real (WorldRun/CombatRun) con navegación vertical en el circuito B. En cada tick
    // se comprueba que ningún cuerpo entra en un sólido y que nadie avanza más de lo físicamente
    // posible (sin teletransporte). Velocidades, daño y cadencias son los del catálogo.
    public sealed class EnemyNavigationTests
    {
        const double Dt=1.0/60;
        static VerticalCircuit circuit;static NavGraph[] graphs;
        static WorldRun Run(double x,double y,double z,out EnemyNavigation nav)
        {
            if(circuit==null){circuit=new VerticalCircuit(VerticalLayout.Persecucion);graphs=EnemyNavigation.BuildGraphs(circuit);}
            var r=new WorldRun(circuit.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            nav=new EnemyNavigation(circuit,r.Combat.Enemies.Capacity,graphs);
            r.Combat.Enemies.Navigation=nav;r.Combat.Vertical=nav;
            r.Body.PlaceAt(x,y,z);r.SyncPlayer();r.Combat.WeaponsOff=true;return r;
        }
        // y explícita para colocar sobre un tejado; por defecto la altura del terreno.
        static int Spawn(WorldRun r,int type,double x,double y,double z)
        {int i=r.Combat.Spawn(type,x,z);if(y>circuit.Terrain.HeightAt(x,z)){r.Combat.Enemies.Y[i]=(float)y;r.Combat.Enemies.Py[i]=(float)y;}return i;}
        // Simula hasta que se cumpla la condición o se agote el tiempo; verifica cada tick.
        static double Simulate(WorldRun r,EnemyNavigation nav,double seconds,Func<bool> until=null)
        {
            var e=r.Combat.Enemies;var q=circuit.Queries;
            for(int t=0;t<seconds*60;t++){
                if(until!=null&&until())return r.Combat.Time;
                r.Step(default,Dt);if(r.Combat.Dead)return r.Combat.Time;
                for(int i=0;i<e.Count;i++){
                    var d=e.Def(i);double run=Rules.Hypot(e.X[i]-e.Px[i],e.Z[i]-e.Pz[i]),rise=e.Y[i]-e.Py[i];
                    // Velocidad propia o rodar del jefe (18), más la separación heredada (sep×4, ≤ 8 m/s) y el empuje.
                    double limit=(Math.Max(e.Speed[i],18)+8)*Dt+Rules.Hypot(e.Kx[i],e.Kz[i])*Dt+1e-3;
                    Assert.That(run,Is.LessThanOrEqualTo(limit+.02),$"tick {t} enemigo {i} ({d.id}) avance horizontal {run:F3} > {limit:F3}");
                    Assert.That(Math.Abs(rise),Is.LessThanOrEqualTo(Math.Max(NavGraph.Step,Tuning.PlayerMaxFallSpeed*Dt)+1e-3),$"tick {t} enemigo {i} salto vertical {rise:F3}");
                    // Nunca por debajo del suelo: el terreno llano no es obstáculo en las consultas de caras.
                    Assert.That(e.Y[i],Is.GreaterThanOrEqualTo(circuit.Terrain.HeightAt(e.X[i],e.Z[i])-1e-3),$"tick {t} enemigo {i} ({d.id}) bajo el terreno en {e.X[i]:F2},{e.Y[i]:F2},{e.Z[i]:F2} estado {nav.State(i)}");
                    Assert.That(q.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),d.radius,d.height,NavGraph.Step),Is.True,
                        $"tick {t} enemigo {i} ({d.id}) dentro de un sólido en {e.X[i]:F2},{e.Y[i]:F2},{e.Z[i]:F2} estado {nav.State(i)}");
                }
            }
            return r.Combat.Time;
        }
        static bool Hurt(WorldRun r,double hp)=>r.Combat.Hp<hp;

        [Test] public void CommonEnemyClimbsAWallToAClimbOnlyRoofAndAttacks()
        {
            var r=Run(-11,5,-11,out var nav);double hp=r.Combat.Hp;Spawn(r,0,-11,0,-2);
            double t=Simulate(r,nav,20,()=>Hurt(r,hp));
            TestContext.Out.WriteLine($"tejado solo escalando: daño a {t:F2} s; escaladas {nav.ClimbStarts}, bordes {nav.LedgesDone}");
            Assert.That(Hurt(r,hp),Is.True);Assert.That(r.Combat.Enemies.Y[0],Is.EqualTo(5).Within(.01));Assert.That(nav.LedgesDone,Is.EqualTo(1));
        }
        [Test] public void RampRoofIsReachedFasterThanTheLegacyDetourFromTheEast()
        {
            var r=Run(11,4,-11,out var nav);double hp=r.Combat.Hp;Spawn(r,0,17,0,-11);
            double t=Simulate(r,nav,20,()=>Hurt(r,hp));
            TestContext.Out.WriteLine($"tejado con rampa desde el este: daño a {t:F2} s (heredado 7,00 s)");
            Assert.That(t,Is.LessThan(7));
        }
        [Test] public void TerrainCliffIsClimbedNotWalked()
        {
            var r=Run(16,5,10,out var nav);double hp=r.Combat.Hp;int i=Spawn(r,0,8,0,10);
            Simulate(r,nav,20,()=>Hurt(r,hp));
            Assert.That(Hurt(r,hp),Is.True);Assert.That(nav.ClimbStarts,Is.GreaterThanOrEqualTo(1),"sube escalando la pared de la meseta");
        }
        [Test] public void EnemiesDropFromARoofWithGravityAndKeepChasing()
        {
            var r=Run(0,0,4,out var nav);double hp=r.Combat.Hp;int i=Spawn(r,0,0,4,16);double maxDrop=0;var e=r.Combat.Enemies;
            Simulate(r,nav,15,()=>{maxDrop=Math.Max(maxDrop,e.Py[0]-e.Y[0]);return Hurt(r,hp);});
            TestContext.Out.WriteLine($"bajada desde el tejado: mayor caída en un tick {maxDrop:F3} m (heredado 4,000 m); aterrizajes {nav.Landings}");
            Assert.That(Hurt(r,hp),Is.True);Assert.That(nav.DropsStarted,Is.GreaterThanOrEqualTo(1));Assert.That(maxDrop,Is.LessThan(1));
        }
        [Test] public void TrulyInaccessibleCapIsSafeAndEnemiesWaitAtTheNearestPoint()
        {
            var r=Run(20,5.5,20,out var nav);double hp=r.Combat.Hp;var e=r.Combat.Enemies;
            for(int k=0;k<6;k++)Spawn(r,k%2,-4+k*3,0,-4);
            Simulate(r,nav,20);
            var before=new double[e.Count];for(int i=0;i<e.Count;i++)before[i]=e.X[i]+1000*e.Z[i];
            double moved=0;var px=new float[e.Count];var pz=new float[e.Count];for(int i=0;i<e.Count;i++){px[i]=e.X[i];pz[i]=e.Z[i];}
            Simulate(r,nav,2);for(int i=0;i<e.Count;i++)moved=Math.Max(moved,Rules.Hypot(e.X[i]-px[i],e.Z[i]-pz[i]));
            Assert.That(r.Combat.Hp,Is.EqualTo(hp),"sin daño cuerpo a cuerpo");
            for(int i=0;i<e.Count;i++){Assert.That(e.Y[i],Is.LessThan(5),"nadie sobre la seta");Assert.That(Rules.Hypot(e.X[i]-20,e.Z[i]-20),Is.LessThan(6),$"enemigo {i} junto a la seta");}
            TestContext.Out.WriteLine($"seta inaccesible: mayor desplazamiento en los últimos 2 s {moved:F2} m");
            Assert.That(moved,Is.LessThan(1.5),"esperan sin empujar indefinidamente");
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryCommonEliteAndRangedTypeClimbs(int type)
        {
            var r=Run(-11,5,-11,out var nav);var e=r.Combat.Enemies;Spawn(r,type,-11,0,-1);bool shotWhileClimbing=false;
            Simulate(r,nav,25,()=>{if(r.Combat.EnemyShots.Count>0&&!nav.Stable(0))shotWhileClimbing=true;return e.Y[0]>4.99&&nav.Stable(0);});
            TestContext.Out.WriteLine($"{e.Def(0).id}: arriba a {r.Combat.Time:F2} s, velocidad propia {e.Speed[0]:F2} m/s");
            Assert.That(e.Y[0],Is.EqualTo(5).Within(.01),e.Def(0).id);Assert.That(shotWhileClimbing,Is.False,"no dispara agarrado ni en el borde");
        }
        [Test] public void ClimbSpeedIsEachEnemysOwnEffectiveGroundSpeed()
        {
            foreach(int type in new[]{1,2}){
                var r=Run(-11,5,-11,out var nav);var e=r.Combat.Enemies;int i=Spawn(r,type,-11,0,-6.5);double rate=0;
                Simulate(r,nav,25,()=>{if(nav.State(i)==EnemyNavigation.Climb&&e.Py[i]>.5)rate=Math.Max(rate,(e.Y[i]-e.Py[i])/Dt);return e.Y[i]>4.99;});
                TestContext.Out.WriteLine($"{e.Def(i).id}: sube a {rate:F3} m/s, velocidad efectiva en suelo {e.Speed[i]:F3} m/s");
                Assert.That(rate,Is.EqualTo(e.Speed[i]).Within(1e-3));
            }
        }
        [Test] public void BossClimbsTheWideRoofButWaitsBelowTheNarrowTower()
        {
            var r=Run(0,4,16,out var nav);var e=r.Combat.Enemies;r.Combat.SpawnBoss(-12,0);int b=e.Count-1;bool attackedWhileClimbing=false;
            var trace=new System.Text.StringBuilder();int tick=0;
            Simulate(r,nav,40,()=>{
                if(!nav.Stable(b)&&r.Combat.Boss!=null&&r.Combat.Boss.Phase=="windup")attackedWhileClimbing=true;
                if(tick++%30==0)trace.Append($"[{r.Combat.Time:F1}s {e.X[b]:F1},{e.Y[b]:F2},{e.Z[b]:F1} nav={nav.State(b)} nodo={nav.Node(b)} arista={nav.Edge(b)} ataque={e.State[b]} fase={r.Combat.Boss?.Phase}] ");
                return e.Y[b]>3.99&&nav.Stable(b);});
            TestContext.Out.WriteLine($"jefe: tejado amplio a {r.Combat.Time:F2} s; escaladas {nav.ClimbStarts}, bordes {nav.LedgesDone}, caídas {nav.DropsStarted}, aterrizajes {nav.Landings}, sueltas {nav.Detaches}, recuperaciones {nav.Recoveries}/{nav.RecoverFailures}; meta L {nav.Goal(2)}, campo L versión {nav.Fields[2].Version}, nodo del jefe {nav.Node(b)} alcanzable {nav.Reachable(b)}");
            TestContext.Out.WriteLine(trace.ToString());
            Assert.That(e.Y[b],Is.EqualTo(4).Within(.01));Assert.That(attackedWhileClimbing,Is.False,"sin iniciar ataques escalando");
            var tower=Run(-21,4,10,out var nav2);var e2=tower.Combat.Enemies;tower.Combat.SpawnBoss(-12,10);int b2=e2.Count-1;
            Simulate(tower,nav2,25);
            Assert.That(e2.Y[b2],Is.LessThan(.5),"el jefe no cabe en la torre");Assert.That(Rules.Hypot(e2.X[b2]+21,e2.Z[b2]-10),Is.LessThan(8),"espera junto a la torre");
        }
        [Test] public void StrongOutwardPushDetachesAClimberThatFallsAndLaterReclimbs()
        {
            var r=Run(-11,5,-11,out var nav);var e=r.Combat.Enemies;int i=Spawn(r,2,-11,0,-6.5);
            Simulate(r,nav,10,()=>nav.State(i)==EnemyNavigation.Climb&&e.Y[i]>2);
            Assert.That(nav.State(i),Is.EqualTo(EnemyNavigation.Climb));
            // Empuje del sistema existente (Kx/Kz) hacia fuera de la cara norte, mayor que su velocidad.
            e.Kz[i]=10;double fellAt=r.Combat.Time;
            Simulate(r,nav,3,()=>nav.State(i)==EnemyNavigation.Walk);
            Assert.That(nav.Detaches,Is.EqualTo(1));Assert.That(e.Y[i],Is.EqualTo(0).Within(.01),"cae al suelo con gravedad");
            int climbs=nav.ClimbStarts;double landed=r.Combat.Time;
            Simulate(r,nav,20,()=>nav.ClimbStarts>climbs);
            Assert.That(nav.ClimbStarts,Is.GreaterThan(climbs),"retoma la persecución escalando");
            Simulate(r,nav,20,()=>e.Y[i]>4.99);Assert.That(e.Y[i],Is.EqualTo(5).Within(.01));
        }
        [Test] public void GroupsFromFourSectorsReachTheRampRoofWithinTheirRouteTime()
        {
            // Criterio de P0-B: ≥95 % llega en ≤ 1,5 × (coste de su ruta / su velocidad) + 3 s.
            var r=Run(11,4,-11,out var nav);var e=r.Combat.Enemies;r.Combat.Invincible=true;
            foreach(var (x,z) in new[]{(-20.0,-2.0),(24.0,-14.0),(-15.0,18.0),(25.0,6.0)})for(int k=0;k<10;k++)Spawn(r,k%3,x+(k%5-2)*1.2,0,z+(k/5)*1.2);
            Simulate(r,nav,.2);
            var deadline=new double[e.Count];var arrival=new double[e.Count];double latest=0;
            for(int i=0;i<e.Count;i++){
                Assert.That(nav.Reachable(i),Is.True,$"enemigo {i} con ruta");arrival[i]=-1;
                deadline[i]=r.Combat.Time+nav.Fields[nav.ClassOf(i)].Dist(nav.Node(i))/100.0/e.Speed[i]*1.5+3;latest=Math.Max(latest,deadline[i]);
            }
            Simulate(r,nav,latest-r.Combat.Time+.1,()=>{
                bool all=true;
                for(int i=0;i<e.Count;i++){if(arrival[i]<0&&e.Y[i]>3.9&&nav.Stable(i)&&Rules.Hypot(e.X[i]-11,e.Z[i]+11)<6)arrival[i]=r.Combat.Time;all&=arrival[i]>=0;}
                return all;});
            int onTime=0;var times=new List<double>();
            for(int i=0;i<e.Count;i++){if(arrival[i]>=0&&arrival[i]<=deadline[i])onTime++;if(arrival[i]>=0)times.Add(arrival[i]);}
            times.Sort();
            for(int s=0;s<4;s++){var late=new System.Text.StringBuilder();
                for(int i=s*10;i<s*10+10;i++)if(!(arrival[i]>=0&&arrival[i]<=deadline[i]))late.Append($"[{e.Def(i).id} llega {arrival[i]:F1}/{deadline[i]:F1} s estado {nav.State(i)} pos {e.X[i]:F1},{e.Y[i]:F1},{e.Z[i]:F1}] ");
                TestContext.Out.WriteLine($"sector {s}: {late}");}
            TestContext.Out.WriteLine($"esperas de cola en pared: {nav.QueueWaits} ticks");
            TestContext.Out.WriteLine($"cuatro sectores: {onTime}/{e.Count} a tiempo; llegada p50 {(times.Count>0?times[times.Count/2]:-1):F2} s, máx {(times.Count>0?times[times.Count-1]:-1):F2} s; escaladas {nav.ClimbStarts}, caídas {nav.DropsStarted}");
            Assert.That(onTime,Is.GreaterThanOrEqualTo((int)Math.Ceiling(e.Count*.95)));
        }
        [Test] public void PlayerSwitchingLevelsRetargetsTheHorde()
        {
            var r=Run(-11,5,-11,out var nav);var e=r.Combat.Enemies;r.Combat.Invincible=true;for(int k=0;k<5;k++)Spawn(r,0,-11+k,0,-2);
            Simulate(r,nav,25,()=>{int n=0;for(int i=0;i<e.Count;i++)if(e.Y[i]>4.99)n++;return n==e.Count;});
            r.Body.PlaceAt(0,0,-4);r.SyncPlayer();
            Simulate(r,nav,25,()=>{int n=0;for(int i=0;i<e.Count;i++)if(e.Y[i]<.01&&Rules.Hypot(e.X[i],e.Z[i]+4)<4)n++;return n==e.Count;});
            for(int i=0;i<e.Count;i++)Assert.That(e.Y[i],Is.LessThan(.01),"bajan tras el jugador");
        }
        [Test] public void NavigationIsDeterministic()
        {
            float[] Once(){var r=Run(11,4,-11,out var nav);r.Combat.Invincible=true;for(int k=0;k<20;k++)Spawn(r,k%5,-18+k*2,0,6);Simulate(r,nav,10);
                var e=r.Combat.Enemies;var o=new float[e.Count*3];for(int i=0;i<e.Count;i++){o[3*i]=e.X[i];o[3*i+1]=e.Y[i];o[3*i+2]=e.Z[i];}return o;}
            Assert.That(Once(),Is.EqualTo(Once()));
        }
        [Test] public void RangedEnemyNeedsLineOfSightToShoot()
        {
            // Jugador tras el edificio C (al oeste) y paloma al este a tiro: sin línea de visión no dispara.
            var r=Run(-16,0,-11,out var nav);int i=Spawn(r,3,-6,0,-11);var e=r.Combat.Enemies;
            Assert.That(nav.LineOfSight(i,r.Combat.Player),Is.False);
            bool shotBlind=false;
            for(int t=0;t<60;t++){r.Step(default,Dt);if(r.Combat.EnemyShots.Count>0&&!nav.LineOfSight(i,r.Combat.Player))shotBlind=true;}
            Assert.That(shotBlind,Is.False);
        }
    }
}
