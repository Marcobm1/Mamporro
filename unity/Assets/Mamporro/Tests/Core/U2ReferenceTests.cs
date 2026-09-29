using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // Compara el port con unity/Docs/Reference/u2-combat.json, generado solo con la web
    // aprobada (scripts/unity-reference-u2.mjs). Las entradas de cada escenario salen del
    // JSON; las salidas esperadas nunca se recalculan con C#. Enteros, textos y valores
    // Float32 se comparan exactos; los double, con tolerancia absoluta 1e-9 (regla de U0).
    public sealed class U2ReferenceTests
    {
        const double Tolerance=1e-9;
        const double Dt=1.0/60;

        [Serializable] public class Reference {
            public string source;public RngVector[] rng;public RarityCase[] rarity;public Scaling scaling;public UpgradeCase[] upgrades;
            public StatCase[] stats;public OfferCase[] offers;public OfferList[] offerSequence;public WeaponCase[] weapons;
            public EnemyCase[] enemies;public PassiveCase[] passives;public IntegrationCase[] integration; }
        [Serializable] public class RngVector { public uint[] nextU32; }
        [Serializable] public class RarityCase { public double luck;public double[] weights;public string[] rolls; }
        [Serializable] public class Scaling { public StepScale[] weaponSteps;public TomeScale[] tomes; }
        [Serializable] public class StepScale { public string stat,rarity;public double amount; }
        [Serializable] public class TomeScale { public string tome,rarity;public double[] amounts; }
        [Serializable] public class UpgradeCase { public string weapon,rarity,preset,seed;public double[] bonus;public UpgradeRoll[] rolls; }
        [Serializable] public class UpgradeRoll { public Change[] changes; }
        [Serializable] public class Change { public string stat;public double amount; }
        [Serializable] public class Level { public string id;public int levels; }
        [Serializable] public class Count { public string id;public int count; }
        [Serializable] public class StatCase { public string name,character;public string[] weapons;public Level[] tomes;public Count[] items;public double gold;public State result; }
        [Serializable] public class State {
            public int level,pending,rerolls,skips,banishes;public double xp,hp,gold;public WeaponState[] weapons;public TomeState[] tomes;
            public Count[] items;public string[] banished;public Stats stats; }
        [Serializable] public class WeaponState { public string id;public int level,kills;public double[] bonus,effective;public double totalDamage; }
        [Serializable] public class TomeState { public string id;public int level;public double[] bonus; }
        [Serializable] public class Stats {
            public double damage,attackSpeed,extraProjectiles,area,critChance,critDamage,projectileSpeed,duration,knockback,moveSpeed,
                maxHp,regen,armor,pickupRadius,xpGain,luck,choices,goldGain; }
        [Serializable] public class ExpectedCard { public string kind,key,id,rarity;public Change[] changes;public double[] amounts;public double amount; }
        [Serializable] public class OfferList { public ExpectedCard[] cards; }
        [Serializable] public class OfferStep { public string step;public bool result;public ExpectedCard[] offer;public State state; }
        [Serializable] public class OfferCase { public string name,seed,character;public string[] weapons;public Level[] tomes;public Count[] items;public bool banishAll;public OfferStep[] steps; }
        [Serializable] public class WeaponCase { public string weapon;public float[] hp,x,z;public double damage;public int projectiles; }
        [Serializable] public class Spawn { public double x,z,hp,gold;public bool boss; }
        [Serializable] public class EnemySample { public int tick,count,shots;public double hp,invulnerable,px,pz;public float[] ex,ez,ehp;public bool bossEnraged; }
        [Serializable] public class EnemyCase { public string enemy,variant;public int type,ticks;public bool invincible;public double hpFraction;public Spawn spawn;public EnemySample[] samples; }
        [Serializable] public class Point { public double x,z; }
        [Serializable] public class PassiveSample { public int tick;public double hp,shield,invulnerable;public float[] ex,ez; }
        [Serializable] public class PassiveCase { public string character;public Point[] spawns;public PassiveSample[] samples; }
        [Serializable] public class SpawnInput { public int type;public double x,z,hp; }
        [Serializable] public class IntegrationSample {
            public int tick,level,kills,count,gems,coins;public double hp,xp,gold,px,pz;public Count[] items;public WeaponState[] weapons; }
        [Serializable] public class IntegrationCase {
            public string name,seed,character;public bool invincible;public string[] weapons;public Level[] tomes;public Count[] items;public double gold;
            public SpawnInput[] spawns;public IntegrationSample[] samples;public State final; }

        static Reference cached;
        static Reference Ref=>cached??=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u2-combat.json")));
        static CharacterDef Character(string id)=>Array.Find(Catalog.Characters,c=>c.id==id);
        static RarityDef Rarity(string id)=>Array.Find(Catalog.Rarities,r=>r.id==id);
        static CombatRun Run(string seed,string character)=>new CombatRun(new FlatWorld(),seed,Character(character));
        static void Near(double actual,double expected,string what)=>Assert.That(actual,Is.EqualTo(expected).Within(Tolerance),what);
        static void Setup(CombatRun r,string[] weapons,Level[] tomes,Count[] items,double gold)
        {
            foreach(var id in weapons??Array.Empty<string>())r.AddWeapon(id);
            foreach(var t in tomes??Array.Empty<Level>())for(int k=0;k<t.levels;k++)r.AddTome(t.id);
            foreach(var i in items??Array.Empty<Count>())for(int k=0;k<i.count;k++)r.AddItem(i.id);
            if(gold!=0)r.GainGold(gold);
        }
        static void ResolveChoices(CombatRun r){r.OpenChoice();while(r.Choosing)r.Choose(0);}

        static void AssertWeapon(Weapon w,WeaponState e,string what)
        {
            Assert.That(w.Def.id,Is.EqualTo(e.id),what+" id");Assert.That(w.Level,Is.EqualTo(e.level),what+" nivel");Assert.That(w.Kills,Is.EqualTo(e.kills),what+" bajas");
            for(int k=0;k<10;k++){Near(w.Bonus[k],e.bonus[k],what+" mejora "+(WStat)k);Near(w.Effective[k],e.effective[k],what+" efectiva "+(WStat)k);}
            Near(w.TotalDamage,e.totalDamage,what+" daño total");
        }
        static void AssertItems(List<ItemStack> items,Count[] expected,string what)
        {
            Assert.That(items.Count,Is.EqualTo(expected.Length),what+" objetos");
            for(int k=0;k<expected.Length;k++){Assert.That(items[k].Def.id,Is.EqualTo(expected[k].id),what);Assert.That(items[k].Count,Is.EqualTo(expected[k].count),what+" "+expected[k].id);}
        }
        static void AssertStats(PlayerStats s,Stats e,string what)
        {
            double[] expected={e.damage,e.attackSpeed,e.extraProjectiles,e.area,e.critChance,e.critDamage,e.projectileSpeed,e.duration,e.knockback,e.moveSpeed,
                e.maxHp,e.regen,e.armor,e.pickupRadius,e.xpGain,e.luck,e.choices,e.goldGain};
            for(int k=0;k<expected.Length;k++)Near(s[(Stat)k],expected[k],what+" "+(Stat)k);
        }
        static void AssertState(CombatRun r,State e,string what)
        {
            Assert.That(r.Level,Is.EqualTo(e.level),what+" nivel");Near(r.Xp,e.xp,what+" XP");Assert.That(r.PendingLevels,Is.EqualTo(e.pending),what+" pendientes");
            Assert.That(r.Rerolls,Is.EqualTo(e.rerolls),what+" rerolls");Assert.That(r.Skips,Is.EqualTo(e.skips),what+" saltos");Assert.That(r.Banishes,Is.EqualTo(e.banishes),what+" descartes");
            Near(r.Hp,e.hp,what+" vida");Near(r.Gold,e.gold,what+" oro");
            Assert.That(r.Weapons.Count,Is.EqualTo(e.weapons.Length),what+" armas");for(int k=0;k<e.weapons.Length;k++)AssertWeapon(r.Weapons[k],e.weapons[k],what+" arma "+k);
            Assert.That(r.Tomes.Count,Is.EqualTo(e.tomes.Length),what+" tomos");
            for(int k=0;k<e.tomes.Length;k++){var t=r.Tomes[k];var x=e.tomes[k];Assert.That(t.Def.id,Is.EqualTo(x.id),what);Assert.That(t.Level,Is.EqualTo(x.level),what+" nivel "+x.id);
                for(int j=0;j<x.bonus.Length;j++)Near(t.Bonus[j],x.bonus[j],what+" tomo "+x.id);}
            AssertItems(r.Items,e.items,what);
            var banished=new List<string>(r.Banished);banished.Sort(StringComparer.Ordinal);CollectionAssert.AreEqual(e.banished,banished,what+" descartados");
            AssertStats(r.Stats,e.stats,what);
        }
        static void AssertCard(Card c,ExpectedCard e,string what)
        {
            Assert.That(c.Kind,Is.EqualTo(e.kind),what+" tipo");Assert.That(c.Key??"",Is.EqualTo(e.key),what+" clave");Assert.That(c.Id??"",Is.EqualTo(e.id),what+" id");
            Assert.That(c.Rarity??"",Is.EqualTo(e.rarity),what+" rareza");Near(c.Amount,e.amount,what+" cantidad");
            if(c.Kind=="weaponUpgrade"){
                Assert.That(c.Changes.Length,Is.EqualTo(e.changes.Length),what+" cambios");
                for(int k=0;k<c.Changes.Length;k++){Assert.That(c.Changes[k].ToString(),Is.EqualTo(e.changes[k].stat),what);Near(c.Amounts[k],e.changes[k].amount,what+" "+e.changes[k].stat);}
            } else {Assert.That(c.Amounts.Length,Is.EqualTo(e.amounts.Length),what+" cantidades");for(int k=0;k<c.Amounts.Length;k++)Near(c.Amounts[k],e.amounts[k],what);}
        }
        static void AssertOffer(List<Card> offer,ExpectedCard[] expected,string what)
        {
            int count=offer?.Count??0;Assert.That(count,Is.EqualTo(expected.Length),what+" cartas");
            for(int k=0;k<count;k++)AssertCard(offer[k],expected[k],what+" carta "+k);
        }

        [Test]public void ReferenceComesFromApprovedWeb()=>Assert.That(Ref.source,Is.EqualTo("0505b1690656d15188860157612455639820fe1f"));

        [Test]public void RngMatchesUtf16AndNullSeeds()
        {
            // JsonUtility trunca cadenas en U+0000: las semillas se escriben en C#.
            string[] seeds={"ñ😀𝄞","\0X","abc"};
            for(int i=0;i<seeds.Length;i++){var r=new Rng(seeds[i]);foreach(uint n in Ref.rng[i].nextU32)Assert.That(r.NextU32(),Is.EqualTo(n));}
        }

        [Test]public void RarityWeightsAndRolls()
        {
            foreach(var c in Ref.rarity){
                for(int k=0;k<Catalog.Rarities.Length;k++)Near(Offers.RarityWeight(Catalog.Rarities[k],c.luck),c.weights[k],"peso suerte "+c.luck);
                var rng=new Rng("U2-RAREZA-"+c.luck.ToString(System.Globalization.CultureInfo.InvariantCulture));
                foreach(var id in c.rolls)Assert.That(Offers.RollRarity(c.luck,rng).id,Is.EqualTo(id),"suerte "+c.luck);
            }
        }

        [Test]public void UpgradeAndTomeScaling()
        {
            foreach(var e in Ref.scaling.weaponSteps){var step=Catalog.Steps[(int)Enum.Parse(typeof(WStat),e.stat)];Near(Rules.Scaled(step.amount,Rarity(e.rarity).power,step.integer),e.amount,e.stat+" "+e.rarity);}
            foreach(var e in Ref.scaling.tomes){var t=Array.Find(Catalog.Tomes,v=>v.id==e.tome);CollectionAssert.AreEqual(e.amounts,Offers.TomeAmounts(t,Rarity(e.rarity)),e.tome+" "+e.rarity);}
        }

        [Test]public void WeaponUpgradeRolls()
        {
            foreach(var c in Ref.upgrades){
                var def=Array.Find(Catalog.Weapons,w=>w.id==c.weapon);var rng=new Rng(c.seed);string what=c.weapon+" "+c.rarity+" "+c.preset;
                foreach(var roll in c.rolls){
                    Offers.RollUpgrade(def,c.bonus,Rarity(c.rarity),rng,out var changes,out var amounts);
                    Assert.That(changes.Length,Is.EqualTo(roll.changes.Length),what);
                    for(int k=0;k<changes.Length;k++){Assert.That(changes[k].ToString(),Is.EqualTo(roll.changes[k].stat),what);Near(amounts[k],roll.changes[k].amount,what);}
                }
            }
        }

        [Test]public void StatAggregationMatchesWeb([ValueSource(nameof(StatNames))]string name)
        {
            var c=Array.Find(Ref.stats,v=>v.name==name);var r=Run("U2-ESTADISTICAS",c.character);
            Setup(r,c.weapons,c.tomes,c.items,c.gold);AssertState(r,c.result,name);
        }
        static IEnumerable<string> StatNames(){foreach(var c in Ref.stats)yield return c.name;}

        [Test]public void OfferActionsMatchWeb([ValueSource(nameof(OfferNames))]string name)
        {
            var c=Array.Find(Ref.offers,v=>v.name==name);var r=Run(c.seed,c.character);Setup(r,c.weapons,c.tomes,c.items,0);
            if(c.banishAll){foreach(var w in Catalog.Weapons)r.Banished.Add("weapon:"+w.id);foreach(var t in Catalog.Tomes)r.Banished.Add("tome:"+t.id);}
            for(int k=0;k<c.steps.Length;k++){
                var s=c.steps[k];string what=name+" paso "+k+" ("+s.step+")";
                Assert.That(Act(r,s.step),Is.EqualTo(s.result),what+" resultado");AssertOffer(r.Offer,s.offer,what);AssertState(r,s.state,what);
            }
        }
        static IEnumerable<string> OfferNames(){foreach(var c in Ref.offers)yield return c.name;}
        static bool Act(CombatRun r,string step)
        {
            var parts=step.Split(':');int index=parts.Length>1?int.Parse(parts[1]):-1;
            switch(parts[0]){
                case "levelup":r.GainXp(Rules.XpNeeded(r.Level)-r.Xp);return true;
                case "open":return r.OpenChoice();
                case "choose":return r.Choose(index);
                case "reroll":return r.Reroll();
                case "skip":return r.Skip();
                case "banish":return r.Banish(index);
            }
            throw new ArgumentException(step);
        }

        [Test]public void OfferSequenceConsumesRngLikeWeb()
        {
            var r=Run("U2-OFERTAS","remedios");var rng=new Rng("U2-OFERTAS/run/offers");
            for(int k=0;k<Ref.offerSequence.Length;k++)AssertOffer(Offers.Generate(r.Weapons,r.Tomes,r.Stats,r.Banished,rng,3),Ref.offerSequence[k].cards,"oferta "+k);
        }

        [Test]public void WeaponCombatMatchesWeb([ValueSource(nameof(WeaponNames))]string id)
        {
            var e=Array.Find(Ref.weapons,v=>v.weapon==id);var r=Run("U2-EQUIVALENCIA","remedios");r.QaWeapons(id);r.Invincible=true;
            double[] x={0,0,0,2,-2,0},z={-6,-7.2,-8.4,0,0,12};for(int i=0;i<6;i++)r.Spawn(0,x[i],z[i],100);
            for(int tick=0;tick<120;tick++){r.Player.X=tick*.02;r.Player.Vx=1.2;r.Step(Dt);}
            Assert.That(r.Enemies.Count,Is.EqualTo(e.hp.Length));
            for(int i=0;i<e.hp.Length;i++){Assert.That(r.Enemies.Hp[i],Is.EqualTo(e.hp[i]),"vida "+i);Assert.That(r.Enemies.X[i],Is.EqualTo(e.x[i]),"x "+i);Assert.That(r.Enemies.Z[i],Is.EqualTo(e.z[i]),"z "+i);}
            Near(r.Weapons[0].TotalDamage,e.damage,"daño");Assert.That(r.Projectiles.Count,Is.EqualTo(e.projectiles),"proyectiles");
        }
        static IEnumerable<string> WeaponNames(){foreach(var c in Ref.weapons)yield return c.weapon;}

        [Test]public void EnemyBehaviourMatchesWeb([ValueSource(nameof(EnemyNames))]string name)
        {
            var c=Array.Find(Ref.enemies,v=>v.enemy+"-"+v.variant==name);
            var r=Run("U2-ENEMIGO-"+name,"remedios");r.WeaponsOff=true;r.Invincible=c.invincible;r.Step(0);
            if(c.spawn.boss){Assert.That(r.SpawnBoss(c.spawn.x,c.spawn.z),Is.True);int i=r.Enemies.IndexOf(r.Boss.EnemyId);r.Enemies.Hp[i]=(float)(r.Enemies.MaxHp[i]*c.hpFraction);}
            else r.Spawn(c.type,c.spawn.x,c.spawn.z,c.spawn.hp);
            Assert.That(r.Enemies.MaxHp[0],Is.EqualTo((float)(Catalog.Enemies[c.type].hp*c.spawn.hp)),"vida inicial");Assert.That(r.Enemies.Gold[0],Is.EqualTo((float)c.spawn.gold),"oro");
            int next=0;
            for(int tick=0;tick<=c.ticks;tick++){
                if(tick>0)r.Step(Dt);
                if(next<c.samples.Length&&c.samples[next].tick==tick){var e=c.samples[next++];string what=name+" tick "+tick;
                    Near(r.Hp,e.hp,what+" vida");Near(r.Invulnerable,e.invulnerable,what+" invulnerable");Near(r.Player.X,e.px,what+" px");Near(r.Player.Z,e.pz,what+" pz");
                    Assert.That(r.Enemies.Count,Is.EqualTo(e.count),what+" enemigos");Assert.That(r.EnemyShots.Count,Is.EqualTo(e.shots),what+" disparos");
                    Assert.That(r.Boss!=null&&r.Boss.Enraged,Is.EqualTo(e.bossEnraged),what+" enfurecido");
                    for(int i=0;i<e.count;i++){Assert.That(r.Enemies.X[i],Is.EqualTo(e.ex[i]),what+" x"+i);Assert.That(r.Enemies.Z[i],Is.EqualTo(e.ez[i]),what+" z"+i);Assert.That(r.Enemies.Hp[i],Is.EqualTo(e.ehp[i]),what+" vida"+i);}
                }
            }
            Assert.That(next,Is.EqualTo(c.samples.Length),"muestras");
        }
        static IEnumerable<string> EnemyNames(){foreach(var c in Ref.enemies)yield return c.enemy+"-"+c.variant;}

        [Test]public void PassivesMatchWeb([Values("remedios","baguette")]string character)
        {
            var c=Array.Find(Ref.passives,v=>v.character==character);var r=Run("U2-PASIVA",character);r.WeaponsOff=true;
            foreach(var s in c.spawns)r.Spawn(0,s.x,s.z);
            int next=0;
            for(int tick=1;tick<=600;tick++){
                r.Step(Dt);
                if(next<c.samples.Length&&c.samples[next].tick==tick){var e=c.samples[next++];string what=character+" tick "+tick;
                    Near(r.Hp,e.hp,what+" vida");Near(r.ShieldCharge,e.shield,what+" escudo");Near(r.Invulnerable,e.invulnerable,what+" invulnerable");
                    Assert.That(r.Enemies.Count,Is.EqualTo(e.ex.Length),what);
                    for(int i=0;i<e.ex.Length;i++){Assert.That(r.Enemies.X[i],Is.EqualTo(e.ex[i]),what+" x"+i);Assert.That(r.Enemies.Z[i],Is.EqualTo(e.ez[i]),what+" z"+i);}
                }
            }
        }

        [Test]public void IntegratedCombatMatchesWeb([ValueSource(nameof(IntegrationNames))]string name)
        {
            var c=Array.Find(Ref.integration,v=>v.name==name);var r=Run(c.seed,c.character);r.Invincible=c.invincible;
            Setup(r,c.weapons,c.tomes,c.items,c.gold);
            foreach(var s in c.spawns)r.Spawn(s.type,s.x,s.z,s.hp);
            int next=0;
            for(int tick=1;tick<=900;tick++){
                r.Step(Dt);ResolveChoices(r);
                if(next<c.samples.Length&&c.samples[next].tick==tick){var e=c.samples[next++];string what=name+" tick "+tick;
                    Near(r.Hp,e.hp,what+" vida");Assert.That(r.Level,Is.EqualTo(e.level),what+" nivel");Near(r.Xp,e.xp,what+" XP");Near(r.Gold,e.gold,what+" oro");
                    Assert.That(r.Kills,Is.EqualTo(e.kills),what+" bajas");Assert.That(r.Enemies.Count,Is.EqualTo(e.count),what+" enemigos");
                    Assert.That(r.Gems.Count,Is.EqualTo(e.gems),what+" gemas");Assert.That(r.Coins.Count,Is.EqualTo(e.coins),what+" monedas");
                    Near(r.Player.X,e.px,what+" px");Near(r.Player.Z,e.pz,what+" pz");AssertItems(r.Items,e.items,what);
                    Assert.That(r.Weapons.Count,Is.EqualTo(e.weapons.Length),what+" armas");for(int k=0;k<e.weapons.Length;k++)AssertWeapon(r.Weapons[k],e.weapons[k],what+" arma "+k);
                }
            }
            AssertState(r,c.final,name+" final");
        }
        static IEnumerable<string> IntegrationNames(){foreach(var c in Ref.integration)yield return c.name;}
    }
}
