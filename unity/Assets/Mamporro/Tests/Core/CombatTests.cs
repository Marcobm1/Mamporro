using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.Tests
{
    public sealed class FlatWorld : ICombatWorld
    {
        public double Height(double x,double z,double maxY=double.PositiveInfinity)=>0;
        public bool PushOut(ref double x,ref double z,double radius,double y,double step)=>false;
        public void Clamp(ref double x,ref double z){x=Math.Max(-48,Math.Min(48,x));z=Math.Max(-48,Math.Min(48,z));}
    }
    public sealed class CombatTests
    {
        [Serializable] public class Fixtures { public Combat[] combat;public Offer[] offers;public ReferenceTests.Vector[] rng; }
        [Serializable] public class Combat { public string weapon;public float[] hp,x,z;public double damage;public int projectiles; }
        [Serializable] public class Offer { public ExpectedCard[] cards; }
        [Serializable] public class ExpectedCard {public string kind,key,id,rarity;public Change[] changes;public double[] amounts;public double amount;}
        [Serializable] public class Change {public string stat;public double amount;}
        Fixtures Read()=>JsonUtility.FromJson<Fixtures>(File.ReadAllText(Path.Combine(Application.dataPath,"Mamporro/Tests/Core/WebFixtures.json")));
        CombatRun Run(string seed="U2-EQUIVALENCIA",int character=0)=>new CombatRun(new FlatWorld(),seed,Catalog.Characters[character]);
        [TestCase("chancla")][TestCase("naftalina")][TestCase("barra")][TestCase("dentaduras")][TestCase("jersey")][TestCase("fregona")]
        public void WeaponCombatMatchesWeb(string id)
        {
            var expected=Array.Find(Read().combat,v=>v.weapon==id);var r=Run();r.QaWeapons(id);r.Invincible=true;
            double[] x={0,0,0,2,-2,0},z={-6,-7.2,-8.4,0,0,12};for(int i=0;i<6;i++)r.Spawn(0,x[i],z[i],100);
            for(int tick=0;tick<120;tick++){r.Player.X=tick*.02;r.Player.Vx=1.2;r.Step(1.0/60);}
            Assert.That(r.Enemies.Count,Is.EqualTo(6));
            for(int i=0;i<6;i++){Assert.That(r.Enemies.Hp[i],Is.EqualTo(expected.hp[i]),"HP "+i);Assert.That(r.Enemies.X[i],Is.EqualTo(expected.x[i]),"X "+i);Assert.That(r.Enemies.Z[i],Is.EqualTo(expected.z[i]),"Z "+i);}
            Assert.That(r.Weapons[0].TotalDamage,Is.EqualTo(expected.damage).Within(1e-9));Assert.That(r.Projectiles.Count,Is.EqualTo(expected.projectiles));
        }
        [Test]public void OffersMatchWebIncludingRngConsumption()
        {
            var r=Run("U2-OFERTAS");var rng=new Rng("U2-OFERTAS/run/offers");
            foreach(var expected in Read().offers){var cards=Offers.Generate(r.Weapons,r.Tomes,r.Stats,r.Banished,rng,3);Assert.That(cards.Count,Is.EqualTo(expected.cards.Length));
                for(int i=0;i<cards.Count;i++){var c=cards[i];var e=expected.cards[i];Assert.That(c.Kind,Is.EqualTo(e.kind));Assert.That(c.Key,Is.EqualTo(e.key));Assert.That(c.Id??"",Is.EqualTo(e.id));Assert.That(c.Rarity??"",Is.EqualTo(e.rarity));
                    if(c.Kind=="weaponUpgrade"){Assert.That(c.Changes.Length,Is.EqualTo(e.changes.Length));for(int k=0;k<c.Changes.Length;k++){Assert.That(c.Changes[k].ToString(),Is.EqualTo(e.changes[k].stat));Assert.That(c.Amounts[k],Is.EqualTo(e.changes[k].amount).Within(1e-9));}}
                    else CollectionAssert.AreEqual(e.amounts,c.Amounts);
                }
            }
        }
        [Test]public void Utf16AndNullCharactersMatchWeb()
        {
            // JsonUtility trunca cadenas en U+0000. La entrada se expresa en C#;
            // las salidas esperadas siguen siendo las ejecutadas por TypeScript.
            string[] seeds={"ñ\uD83D\uDE00\uD834\uDD1E","\0X","abc"};var vectors=Read().rng;
            for(int i=0;i<seeds.Length;i++){var r=new Rng(seeds[i]);foreach(uint n in vectors[i].nextU32)Assert.That(r.NextU32(),Is.EqualTo(n));}
        }
        [Test]public void PausedChoiceDoesNotAdvanceCombat()
        {var r=Run();r.GainXp(100);r.OpenChoice();double time=r.Time,hp=r.Hp;for(int k=0;k<120;k++)r.Step(1.0/60);Assert.That(r.Time,Is.EqualTo(time));Assert.That(r.Hp,Is.EqualTo(hp));Assert.That(r.Choose(0),Is.True);Assert.That(r.PendingLevels,Is.GreaterThan(0));}
        [Test]public void ActionsAndFillersRespectExclusionsAndUses()
        {
            var r=Run();r.GainXp(100);r.OpenChoice();Assert.That(r.Banish(0),Is.True);string key=null;foreach(var k in r.Banished)key=k;Assert.That(r.Offer.Exists(c=>c.Key==key),Is.False);
            Assert.That(r.Reroll(),Is.True);Assert.That(r.Reroll(),Is.True);Assert.That(r.Reroll(),Is.False);Assert.That(r.Skip(),Is.True);Assert.That(r.Skip(),Is.True);Assert.That(r.Skip(),Is.False);
            foreach(var w in Catalog.Weapons)r.Banished.Add("weapon:"+w.id);foreach(var t in Catalog.Tomes)r.Banished.Add("tome:"+t.id);
            var cards=Offers.Generate(r.Weapons,r.Tomes,r.Stats,r.Banished,new Rng("x"),4);Assert.That(cards.Count,Is.EqualTo(2));Assert.That(cards[0].Kind,Is.EqualTo("heal"));Assert.That(cards[1].Kind,Is.EqualTo("gold"));
        }
        [Test]public void SlotsAndItemEffectsAreAdditive()
        {
            var r=Run();r.AddWeapon("barra");r.AddWeapon("naftalina");r.AddWeapon("jersey");Assert.That(r.AddWeapon("fregona"),Is.Null);
            r.AddTome("damage");r.AddTome("vitality");r.AddTome("magnet");r.AddTome("moveSpeed");Assert.That(r.AddTome("luck"),Is.False);
            r.AddItem("gafas");r.AddItem("gafas");r.AddItem("cojin");r.AddItem("monedero");r.GainGold(250);Assert.That(r.Stats[Stat.damage],Is.EqualTo(1.2).Within(1e-9));Assert.That(r.Stats[Stat.critChance],Is.EqualTo(.14).Within(1e-9));Assert.That(r.Hp,Is.EqualTo(120));Assert.That(r.Stats[Stat.pickupRadius],Is.EqualTo(4).Within(1e-9));
        }
        [Test]public void ShieldInvulnerabilityAndConsumableRevival()
        {
            var r=Run(character:1);r.WeaponsOff=true;for(int i=0;i<481;i++)r.Step(1.0/60);r.Hurt(20);Assert.That(r.Hp,Is.EqualTo(100));Assert.That(r.ShieldCharge,Is.Zero);
            r.Hurt(20);Assert.That(r.Hp,Is.EqualTo(100));r.Invulnerable=0;r.AddItem("bata");r.Hurt(1000);Assert.That(r.Hp,Is.EqualTo(50));Assert.That(r.Invulnerable,Is.EqualTo(2.5));Assert.That(r.ItemCount("bata"),Is.Zero);
            r.Invulnerable=0;r.Hurt(1000);Assert.That(r.Dead,Is.True);double time=r.Time;r.Step(1);Assert.That(r.Time,Is.EqualTo(time));
        }
        [Test]public void PickupPoolConservesXpWhenFull()
        {var p=new Pickups(5);for(int i=0;i<25;i++)p.Spawn(i,0,0,2);double sum=0;for(int i=0;i<p.Count;i++)sum+=p.Value[i];Assert.That(sum,Is.EqualTo(50));Assert.That(p.Count,Is.EqualTo(5));}
        [Test]public void ContactAndSlowTimers()
        {var r=Run();r.WeaponsOff=true;r.Spawn(0,.6,0);r.Step(1.0/60);Assert.That(r.Hp,Is.EqualTo(92));r.Step(1.0/60);Assert.That(r.Hp,Is.EqualTo(92));r.Enemies.ApplySlow(0,.35,.6);r.Enemies.ApplySlow(0,.2,3);Assert.That(r.Enemies.SlowTime[0],Is.EqualTo(.6f));}
        [Test]public void RangedEnemyTelegraphsAndShoots()
        {
            var r=Run();r.WeaponsOff=true;r.Invincible=true;r.Spawn(3,0,10);bool windup=false,shot=false;
            for(int i=0;i<300;i++){r.Step(1.0/60);windup|=r.Enemies.State[0]==1;shot|=r.EnemyShots.Count>0;}
            Assert.That(windup,Is.True);Assert.That(shot,Is.True);
        }
        [Test]public void ChargerKeepsAimDuringTelegraphThenRecovers()
        {
            var r=Run();r.WeaponsOff=true;r.Invincible=true;r.Spawn(4,0,12);
            for(int i=0;i<240&&r.Enemies.State[0]!=1;i++)r.Step(1.0/60);
            Assert.That(r.Enemies.State[0],Is.EqualTo(1));float aim=r.Enemies.AimX[0];r.Player.X=8;
            bool dash=false,recover=false;for(int i=0;i<90;i++){r.Step(1.0/60);dash|=r.Enemies.State[0]==2;recover|=r.Enemies.State[0]==3;if(r.Enemies.State[0]==1)Assert.That(r.Enemies.AimX[0],Is.EqualTo(aim));}
            Assert.That(dash,Is.True);Assert.That(recover,Is.True);Assert.That(r.Enemies.HitDamage[0],Is.EqualTo(20));
        }
        [Test]public void BossUsesAllAttacksWithoutImmediateRepeatsAndEnrages()
        {
            var r=Run();r.WeaponsOff=true;r.Invincible=true;r.SpawnBoss(0,10);r.Enemies.Hp[0]=1400;
            var seen=new HashSet<string>();string last=null,phase=null;bool shots=false,minions=false;
            for(int i=0;i<3600;i++){r.Step(1.0/60);var b=r.Boss;Assert.That(b,Is.Not.Null);if(b.Phase=="windup"&&phase!="windup"){Assert.That(b.Attack,Is.Not.EqualTo(last));last=b.Attack;seen.Add(last);}phase=b.Phase;shots|=r.EnemyShots.Count>0;minions|=r.Enemies.Count>1;}
            Assert.That(seen.Count,Is.EqualTo(3));Assert.That(r.Boss.Enraged,Is.True);Assert.That(shots&&minions,Is.True);
        }
        [Test]public void PearlsBounceOnceAndCreditActualDamage()
        {
            var r=Run();r.Spawn(0,2,0,100);r.Spawn(0,4,0,100);for(int i=0;i<4;i++)r.AddItem("perlas");
            var w=r.Weapons[0];w.Effective[(int)WStat.critChance]=1;r.DamageEnemy(0,w,0,0);
            Assert.That(r.Enemies.Hp[0],Is.EqualTo(1380));Assert.That(r.Enemies.Hp[1],Is.EqualTo(1390));Assert.That(w.TotalDamage,Is.EqualTo(30));
        }
        [Test]public void OllaExplosionsKillAndDropRewardsOnlyOnce()
        {
            var r=Run();r.Invincible=true;r.WeaponsOff=true;for(int i=0;i<5;i++)r.AddItem("olla");
            for(int i=0;i<100;i++)r.Spawn(0,10+i*.01,0);for(int i=0;i<50;i++)r.Enemies.Hp[i]=0;
            r.Step(1.0/60);Assert.That(r.Kills,Is.GreaterThan(50));int killed=r.Kills;double total=0;for(int i=0;i<r.Gems.Count;i++)total+=r.Gems.Value[i];Assert.That(total,Is.EqualTo(killed));
            r.Step(1.0/60);Assert.That(r.Kills,Is.EqualTo(killed));
        }
        [Test]public void FourWeaponsCombatLoopAllocatesNoManagedMemoryAfterWarmup()
        {
            var r=Run();r.Invincible=true;r.QaWeapons("chancla","naftalina","dentaduras","fregona");
            for(int i=0;i<300;i++)r.Spawn(i%4,(i%20)-10,(i/20)+5,10000);
            for(int i=0;i<180;i++){r.Player.X=Math.Sin(i*.01)*2;r.Player.Vx=1;r.Step(1.0/60);}
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++){r.Player.X=Math.Sin(i*.01)*2;r.Step(1.0/60);}long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero,"Solo núcleo, sin UI/render ni elecciones; no equivale al GC de la build.");
        }
    }
}
