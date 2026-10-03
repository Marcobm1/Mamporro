using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;
using Collider=Mamporro.Core.Collider;

namespace Mamporro.Tests
{
    // Integración sobre los mundos generados reales. Los obstáculos controlados se
    // añaden a su WorldCollision, nunca sustituyen el terreno por un mock plano.
    public sealed class WorldCombatTests
    {
        const double Dt=1.0/60;
        WorldData world;
        [OneTimeSetUp]public void Generate(){world=WorldData.Generate("MAMPORRO");}
        WorldRun New(WorldData data=null)
        {
            var s=new WorldRun(data??world,"U3-COMBATE",Catalog.Characters[0]);
            s.Combat.WeaponsOff=true;return s;
        }
        static void Place(WorldRun s,double x,double z)
        {s.Body.PlaceAt(x,s.World.Heightfield.HeightAt(x,z),z);s.SyncPlayer();}
        static void Tick(WorldRun s,int count)
        {for(int i=0;i<count;i++)s.Step(default,Dt);}

        [Test]public void SpawnAndChaseUseTerrainAndTheFullWorldGrid()
        {
            var s=New();Place(s,95,20);var r=s.Combat;
            int i=r.Spawn(0,100,20);float start=r.Enemies.X[i];
            Assert.That(r.World,Is.SameAs(world.Collision));
            Assert.That(r.Enemies.Y[i],Is.EqualTo((float)world.Heightfield.HeightAt(100,20)));
            var found=new int[8];int n=r.Enemies.Query(100,20,.5,found);
            Assert.That(n,Is.EqualTo(1));Assert.That(found[0],Is.EqualTo(i));
            Tick(s,45);Assert.That(r.Enemies.X[i],Is.LessThan(start));
        }

        WorldData WithObstacle(double top,out Collider obstacle)
        {
            double y=world.Heightfield.HeightAt(3,0);
            obstacle=Collider.Oriented(3,0,1,8,0,y,y+top,true);
            var colliders=new List<Collider>(world.Colliders){obstacle};
            return new WorldData{Heightfield=world.Heightfield,Colliders=colliders,
                Collision=new WorldCollision(world.Heightfield,new ColliderGrid(colliders,world.Heightfield.Half),world.Collision.Limit)};
        }
        [Test]public void SolidObstacleBlocksAnEnemyOnTheRealMap()
        {
            var data=WithObstacle(4,out var box);var s=New(data);Place(s,8,0);
            int i=s.Combat.Spawn(0,0,0);var e=s.Combat.Enemies;bool blocked=false;
            for(int tick=0;tick<180;tick++){
                s.Step(default,Dt);blocked|=e.StuckTime[i]>0||e.DetourTime[i]>0;
                box.PushOut(e.X[i],e.Z[i],e.Radius(i),out double dx,out double dz,out _,out _);
                Assert.That(JsMath.Hypot(dx,dz),Is.LessThan(1e-5),"no penetra la caja sólida");
                Assert.That(e.X[i],Is.LessThan(2),"no atraviesa la pared");
            }
            Assert.That(blocked,Is.True,"WorldCollision intervino en el movimiento");
        }
        [TestCase(.55,true)]
        [TestCase(.65,false)]
        public void EnemyStepUsesWebLimitOfPointSix(double height,bool climb)
        {
            var data=WithObstacle(height,out var box);var s=New(data);Place(s,8,0);
            int i=s.Combat.Spawn(0,0,0);var e=s.Combat.Enemies;bool onTop=false;
            for(int tick=0;tick<120;tick++){
                s.Step(default,Dt);
                if(box.IsOnTop(e.X[i],e.Z[i])&&Math.Abs(e.Y[i]-box.Top)<1e-5)onTop=true;
            }
            Assert.That(onTop,Is.EqualTo(climb));
        }
        [Test]public void IrregularTerrainTracksGroundHeightEveryTick()
        {
            var s=New();Place(s,42,38);int i=s.Combat.Spawn(0,35,38);var e=s.Combat.Enemies;
            float first=e.Y[i];double change=0;
            for(int tick=0;tick<180;tick++){
                float oldY=e.Y[i];s.Step(default,Dt);
                double expected=world.Collision.GroundHeight(e.X[i],e.Z[i],oldY+.6);
                // El almacén enemigo es Float32, como el TypedArray web.
                Assert.That(e.Y[i],Is.EqualTo(expected).Within(2e-5));
                change=Math.Max(change,Math.Abs(e.Y[i]-first));
            }
            Assert.That(change,Is.GreaterThan(.1),"recorrido con desnivel real");
        }
        [Test]public void CrowdPressureBrakesMovementAndContactHonorsIframes()
        {
            var free=New();var crowded=New();var r=crowded.Combat;
            for(int i=0;i<18;i++)r.Spawn(0,.6+i*.65,0);
            double maxSlow=0;
            for(int t=0;t<45;t++){
                var intent=new PlayerIntent{MoveX=1};free.Step(intent,Dt);crowded.Step(intent,Dt);
                maxSlow=Math.Max(maxSlow,r.CrowdSlow);
                if(t==0){Assert.That(r.Hp,Is.LessThan(100));double hp=r.Hp;r.Hurt(20);Assert.That(r.Hp,Is.EqualTo(hp));}
            }
            Assert.That(maxSlow,Is.GreaterThan(.05));Assert.That(maxSlow,Is.LessThanOrEqualTo(.4));
            Assert.That(crowded.Body.X,Is.LessThan(free.Body.X-.1));
            r.Enemies.Clear();Tick(crowded,60);double before=r.Hp;r.Hurt(8);Assert.That(r.Hp,Is.LessThan(before));
        }
        [Test]public void ProjectileKillDropsAndCollectsXpGoldAndOpensCards()
        {
            var s=New();var r=s.Combat;int i=r.Spawn(4,8,0,.001);var d=r.Enemies.Def(i);
            r.Projectiles.Spawn(8,world.Heightfield.HeightAt(8,0)+1,0,1,0,0,1,.5);
            s.Step(default,Dt);
            Assert.That(r.Kills,Is.EqualTo(1));Assert.That(r.Enemies.Count,Is.Zero);
            Assert.That(r.Weapons[0].TotalDamage,Is.GreaterThan(0));
            Assert.That(r.Gems.Count,Is.EqualTo(1));Assert.That(r.Gems.Value[0],Is.EqualTo(d.xp));
            Assert.That(r.Coins.Count,Is.EqualTo(1));double gold=r.Coins.Value[0];
            Assert.That(gold,Is.InRange(d.goldMin,d.goldMax));
            Place(s,8,0);Tick(s,60);
            Assert.That(r.Gold,Is.EqualTo(gold));Assert.That(r.GoldCollected,Is.EqualTo(gold));
            Assert.That(r.Level,Is.GreaterThan(1));Assert.That(r.Choosing,Is.True);
            Assert.That(r.Offer.Count,Is.EqualTo(3));double time=r.Time;Tick(s,10);Assert.That(r.Time,Is.EqualTo(time));
            Assert.That(r.Choose(0),Is.True);while(r.Choosing)r.Choose(0);
            Tick(s,1);Assert.That(r.Time,Is.GreaterThan(time));Assert.That(r.Kills,Is.EqualTo(1));
        }
        [Test]public void AutomaticWeaponFiresAndKillsOnTerrain()
        {
            var s=New();s.Combat.WeaponsOff=false;s.Combat.Invincible=true;s.Combat.Spawn(0,0,-6);
            bool projectile=false;
            for(int t=0;t<240;t++){s.Step(default,Dt);projectile|=s.Combat.Projectiles.Count>0;}
            Assert.That(projectile,Is.True);Assert.That(s.Combat.Kills,Is.EqualTo(1));
        }
        [Test]public void HostileProjectilesFollowTerrainAndDamageOnlyOnce()
        {
            var s=New();var r=s.Combat;
            r.EnemyShots.Spawn(0,1,-2,0,1,8,4,.2,damage:10);
            Tick(s,30);Assert.That(r.Hp,Is.EqualTo(90));Assert.That(r.EnemyShots.Count,Is.Zero);
            Tick(s,30);Assert.That(r.Hp,Is.EqualTo(90));
        }
        [Test]public void RangedEnemyTelegraphsFiresAndHitsOnRealWorld()
        {
            var s=New();s.Combat.Spawn(3,0,-10);bool windup=false,shots=false;
            for(int t=0;t<360;t++){s.Step(default,Dt);windup|=s.Combat.Enemies.State[0]==1;shots|=s.Combat.EnemyShots.Count>0;}
            Assert.That(windup&&shots,Is.True);Assert.That(s.Combat.Hp,Is.LessThan(100));
        }
        [Test]public void BossPushReturnsToPhysicsAndSlamDamagesPlayer()
        {
            var s=New();var r=s.Combat;r.SpawnBoss(1,0);double start=s.Body.X;s.Step(default,Dt);
            Assert.That(s.Body.X,Is.LessThan(start));Assert.That(s.Body.X,Is.EqualTo(r.Player.X));
            Assert.That(s.Body.Z,Is.EqualTo(r.Player.Z));
            r.Invulnerable=0;r.Boss.Phase="windup";r.Boss.Attack="slam";r.Boss.Timer=0;
            double hp=r.Hp;s.Step(default,Dt);Assert.That(r.Hp,Is.EqualTo(hp-32));
        }
        [Test]public void FarRecyclingPreservesIdentityAndExcludesEliteAndBoss()
        {
            var s=New();var r=s.Combat;r.Spawn(0,110,0);r.Spawn(4,110,2);r.SpawnBoss(110,-2);
            var e=r.Enemies;uint id=e.Id[0];e.Hp[0]=3;e.Kx[0]=12;e.Vz[0]=9;
            s.Spawns.RecycleFar(r,world.Collision,0);
            double distance=JsMath.Hypot(e.X[0],e.Z[0]);Assert.That(distance,Is.InRange(25.999,38.001));
            Assert.That(e.Id[0],Is.EqualTo(id));Assert.That(e.Hp[0],Is.EqualTo(3));Assert.That(e.Count,Is.EqualTo(3));
            Assert.That(e.Kx[0]+e.Vz[0],Is.Zero);Assert.That(e.Px[0],Is.EqualTo(e.X[0]));
            Assert.That(e.Y[0],Is.EqualTo(world.Heightfield.HeightAt(e.X[0],e.Z[0])).Within(1e-5));
            Assert.That(e.X[1],Is.EqualTo(110));Assert.That(e.X[2],Is.EqualTo(110));
            s.Step(default,Dt);var output=new int[8];Assert.That(e.Query(e.X[0],e.Z[0],1,output),Is.EqualTo(1));
        }
        [Test]public void ReusedSlotsResetTransientStateAndDoNotGrowArrays()
        {
            var s=New();var r=s.Combat;var positions=r.Enemies.X;var projectiles=r.Projectiles.X;
            uint previous=0;
            for(int round=0;round<30;round++){
                int i=r.Spawn(0,5,0);Assert.That(i,Is.Zero);Assert.That(r.Enemies.Id[i],Is.GreaterThan(previous));
                previous=r.Enemies.Id[i];Assert.That(r.Enemies.SlowTime[i],Is.Zero);Assert.That(r.Enemies.Kx[i],Is.Zero);
                r.Enemies.SlowTime[i]=4;r.Enemies.Kx[i]=7;r.Enemies.Remove(i);r.Enemies.Rebuild();
                r.Projectiles.Spawn(0,1,0,1,0,1,.001,.2);s.Step(default,Dt);Assert.That(r.Projectiles.Count,Is.Zero);
            }
            Assert.That(r.Enemies.X,Is.SameAs(positions));Assert.That(r.Projectiles.X,Is.SameAs(projectiles));
        }
        [Test]public void WorldCombatHotLoopDoesNotAllocateAfterWarmup()
        {
            var s=New();var r=s.Combat;r.Invincible=true;r.WeaponsOff=false;r.QaWeapons("chancla","naftalina","dentaduras","fregona");
            for(int i=0;i<100;i++)r.Spawn(i%4,i%10-5,i/10+5,10000);
            Tick(s,180);long before=GC.GetAllocatedBytesForCurrentThread();Tick(s,180);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero,"núcleo caliente; no mide GC de UI/render ni FPS");
        }
    }
}
