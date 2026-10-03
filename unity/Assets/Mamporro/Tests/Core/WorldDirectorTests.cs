using System;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // El director conectado a WorldRun/CombatRun sobre el mundo real: oleadas, élite,
    // enjambre con 750 vivos, ritmo por duración y acciones de depuración.
    public sealed class WorldDirectorTests
    {
        const double Dt=1.0/60;
        WorldData world;
        [OneTimeSetUp]public void Generate(){world=WorldData.Generate("MAMPORRO");}
        WorldRun New(int minutes)
        {
            var s=new WorldRun(world,"U3-DIRECTOR",Catalog.Characters[0],null,minutes);
            s.Combat.Invincible=true;s.Combat.WeaponsOff=true;return s;
        }
        static void Tick(WorldRun s,int count){for(int i=0;i<count;i++)s.Step(default,Dt);}
        static int CountEvents(WorldRun s,string kind,string detail=null)
        {int n=0;foreach(var e in s.Combat.Events)if(e.Kind==kind&&(detail==null||e.Detail==detail))n++;return n;}

        [Test]public void FirstTwoMinutesSpawnByTableWithStampedeAndElite()
        {
            var s=New(10);var r=s.Combat;int maxAlive=0;
            for(int t=0;t<7260;t++){s.Step(default,Dt);maxAlive=Math.Max(maxAlive,r.Enemies.Count);Assert.That(r.Enemies.Count,Is.LessThanOrEqualTo(s.Params.MaxAlive+30+1),"tope + oleada");}
            Assert.That(s.Spawned,Is.GreaterThan(150));
            Assert.That(CountEvents(s,"wave","wave.stampede"),Is.EqualTo(1),"estampida al minuto 1,5");
            Assert.That(CountEvents(s,"elite","rata"),Is.EqualTo(1),"élite al minuto 2");
            Assert.That(CountEvents(s,"swarm"),Is.Zero);
            int rats=0;for(int i=0;i<r.Enemies.Count;i++)if(r.Enemies.Type[i]==WorldSpawns.EliteType)rats++;
            Assert.That(rats,Is.EqualTo(1));Assert.That(s.TimeLeft,Is.EqualTo(600-r.Time).Within(1e-9));
            Assert.That(s.Cheated,Is.False);
        }

        [TestCase(5,2.0)]
        [TestCase(10,1.0)]
        [TestCase(15,2.0/3)]
        public void DurationScalesDifficultyXpAndGold(int minutes,double pace)
        {
            var s=New(minutes);Tick(s,600);var r=s.Combat;
            Assert.That(s.Director.Pace,Is.EqualTo(pace).Within(1e-12));Assert.That(r.Pace,Is.EqualTo(pace).Within(1e-12));
            double m=r.Time/60*pace;
            Assert.That(r.Minutes,Is.EqualTo(m).Within(1e-12));
            Assert.That(r.SpawnXp,Is.EqualTo((1+.12*m)*pace).Within(1e-12));Assert.That(r.SpawnGold,Is.EqualTo(pace).Within(1e-12));
            for(int i=0;i<r.Enemies.Count;i++)Assert.That(r.Enemies.Gold[i],Is.EqualTo((float)pace),"oro de partida escalado");
        }

        [Test]public void SwarmAfterTimeoutReaches750AliveAndNeverMore()
        {
            var s=New(5);var r=s.Combat;r.Time=295;
            int peak=0;
            for(int t=0;t<45*60;t++){s.Step(default,Dt);peak=Math.Max(peak,r.Enemies.Count);}
            Assert.That(s.Swarm,Is.True);Assert.That(CountEvents(s,"swarm"),Is.EqualTo(1));
            Assert.That(s.TimeLeft,Is.LessThan(-30));Assert.That(s.Params.MaxAlive,Is.EqualTo(750));
            Assert.That(peak,Is.EqualTo(750),"enjambre lleno");Assert.That(r.Enemies.Count,Is.EqualTo(750));
            Assert.That(CountEvents(s,"wave"),Is.EqualTo(6));Assert.That(s.Params.Rate,Is.EqualTo(Math.Min(60,Director.SpawnRate(r.Time/30)*Math.Pow(2,(r.Time-300)/20))).Within(1e-9));
        }

        [Test]public void DebugActionsMarkCheatedAndActLikeWeb()
        {
            var s=New(10);var r=s.Combat;Tick(s,30);Assert.That(s.Cheated,Is.False);
            Assert.That(s.DebugToggleInvincible(),Is.False);Assert.That(s.Cheated,Is.True);
            s.DebugSkipMinute();Assert.That(r.Time,Is.EqualTo(60.5).Within(1e-9));
            int before=r.Enemies.Count;s.DebugSpawn(100);Assert.That(r.Enemies.Count,Is.GreaterThan(before+80));
            int kills=r.Kills,alive=r.Enemies.Count;s.DebugKillAll();Assert.That(r.Enemies.Count,Is.Zero);Assert.That(r.Kills,Is.EqualTo(kills+alive));
            Assert.That(s.DebugSummonBoss(),Is.True);Assert.That(r.Boss,Is.Not.Null);Assert.That(s.DebugSummonBoss(),Is.False,"un jefe a la vez");
            s.DebugKillAll();Assert.That(r.Victory,Is.True);Assert.That(s.Over,Is.True);
            double time=r.Time;Tick(s,10);Assert.That(r.Time,Is.EqualTo(time),"la partida se detiene al ganar");
            var g=New(10);g.DebugAddGold(100);Assert.That(g.Combat.Gold,Is.EqualTo(100));Assert.That(g.Cheated,Is.True);
            var l=New(10);l.DebugLevelUp();Assert.That(l.Combat.PendingLevels,Is.EqualTo(1));Assert.That(l.Combat.Level,Is.EqualTo(2));
        }

        [Test]public void DirectorHotLoopDoesNotAllocateAfterWarmup()
        {
            // Sin bajas ni subidas: mide solo director, apariciones, reciclado y movimiento.
            var s=New(5);Tick(s,1200);
            long before=GC.GetAllocatedBytesForCurrentThread();Tick(s,600);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(s.Combat.Enemies.Count,Is.GreaterThan(20));
            Assert.That(allocated,Is.Zero,"núcleo caliente; no mide GC de UI/render ni FPS");
        }
    }
}
