using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // El catálogo C# (exportado de src/data por scripts/u2-export-data.mjs) debe coincidir
    // con baseline.catalog de U0, generado de forma independiente desde la web aprobada.
    public sealed class CatalogReferenceTests
    {
        [Serializable] public class Baseline { public Catalog catalog; }
        [Serializable] public class Catalog { public Characters characters;public Weapon[] weapons;public Tome[] tomes;public Item[] items;public Enemy[] enemies;public Waves waves;public Run run;public BossConfig boss; }
        [Serializable] public class Characters { public Character remedios,baguette; }
        [Serializable] public class Character { public string id,startingWeapon;public Passive passive;public double maxHp,armor,pickupRadius; }
        [Serializable] public class Passive { public string kind;public double radius,amount,recharge; }
        [Serializable] public class Weapon { public string id,behavior;public WeaponBase @base;public double hitFlash;public string[] upgradable; }
        [Serializable] public class WeaponBase { public double damage,cooldown,count,area,speed,duration,pierce,critChance,critMultiplier,knockback; }
        [Serializable] public class Effect { public string stat,mode,display;public double amount;public bool integer; }
        [Serializable] public class Tome { public string id;public Effect[] effects; }
        [Serializable] public class Item { public string id,rarity;public Effect[] effects;public int maxStacks; }
        [Serializable] public class Gold { public double chance;public int min,max; }
        [Serializable] public class Ranged { public double preferred,range,cooldown,windup,projectileSpeed,projectileDamage,projectileRadius; }
        [Serializable] public class Charge { public double cooldown,range,windup,dashTime,dashSpeed,damageMultiplier,recover; }
        [Serializable] public class Enemy { public string id,behavior;public double hp,speed,agility,damage,radius,height,xp,mass;public Gold gold;public Ranged ranged;public Charge charge; }
        [Serializable] public class Waves { public SpawnCurve SPAWN_CURVE; }
        [Serializable] public class SpawnCurve { public double hpGrowth,hpCurve,xpGrowth; }
        [Serializable] public class Run { public GoldConfig GOLD_CONFIG;public ChestConfig CHEST_CONFIG; }
        [Serializable] public class GoldConfig { public double fillerChestFraction,fillerMin; }
        [Serializable] public class ChestConfig { public double baseCost,costStep,costCurve; }
        [Serializable] public class BossConfig { public double hpGrowth,hpCurve; }

        static Catalog Read()=>JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/baseline.json"))).catalog;
        static void Effects(Mamporro.Core.Effect[] actual,Effect[] expected,string what)
        {
            Assert.That(actual.Length,Is.EqualTo(expected.Length),what);
            for(int k=0;k<expected.Length;k++){var a=actual[k];var e=expected[k];
                Assert.That(a.stat.ToString(),Is.EqualTo(e.stat),what);Assert.That(a.amount,Is.EqualTo(e.amount),what);Assert.That(a.basis,Is.EqualTo(e.mode=="base"),what);
                Assert.That(a.integer,Is.EqualTo(e.integer),what);Assert.That(a.display,Is.EqualTo(e.display),what);}
        }

        [Test]public void CharactersMatchU0()
        {
            var c=Read().characters;
            foreach(var e in new[]{c.remedios,c.baguette}){
                var a=Array.Find(Mamporro.Core.Catalog.Characters,v=>v.id==e.id);Assert.That(a,Is.Not.Null,e.id);
                Assert.That(a.startingWeapon,Is.EqualTo(e.startingWeapon));Assert.That(a.passive,Is.EqualTo(e.passive.kind));Assert.That(a.maxHp,Is.EqualTo(e.maxHp));
                Assert.That(a.armor,Is.EqualTo(e.armor));Assert.That(a.pickupRadius,Is.EqualTo(e.pickupRadius));Assert.That(a.radius,Is.EqualTo(e.passive.radius));
                Assert.That(a.amount,Is.EqualTo(e.passive.amount));Assert.That(a.recharge,Is.EqualTo(e.passive.recharge));
            }
            Assert.That(Mamporro.Core.Catalog.Characters.Length,Is.EqualTo(2));
        }

        [Test]public void WeaponsMatchU0()
        {
            var expected=Read().weapons;Assert.That(Mamporro.Core.Catalog.Weapons.Length,Is.EqualTo(expected.Length));
            for(int i=0;i<expected.Length;i++){var a=Mamporro.Core.Catalog.Weapons[i];var e=expected[i];var b=e.@base;
                Assert.That(a.id,Is.EqualTo(e.id));Assert.That(a.behavior,Is.EqualTo(e.behavior),e.id);Assert.That(a.hitFlash,Is.EqualTo(e.hitFlash),e.id);
                CollectionAssert.AreEqual(new[]{b.damage,b.cooldown,b.count,b.area,b.speed,b.duration,b.pierce,b.critChance,b.critMultiplier,b.knockback},a.values,e.id);
                CollectionAssert.AreEqual(e.upgradable,Array.ConvertAll(a.upgradable,s=>s.ToString()),e.id);}
        }

        [Test]public void TomesAndItemsMatchU0()
        {
            var c=Read();
            Assert.That(Mamporro.Core.Catalog.Tomes.Length,Is.EqualTo(c.tomes.Length));
            for(int i=0;i<c.tomes.Length;i++){Assert.That(Mamporro.Core.Catalog.Tomes[i].id,Is.EqualTo(c.tomes[i].id));Effects(Mamporro.Core.Catalog.Tomes[i].effects,c.tomes[i].effects,c.tomes[i].id);}
            Assert.That(Mamporro.Core.Catalog.Items.Length,Is.EqualTo(c.items.Length));
            for(int i=0;i<c.items.Length;i++){var a=Mamporro.Core.Catalog.Items[i];var e=c.items[i];
                Assert.That(a.id,Is.EqualTo(e.id));Assert.That(a.rarity,Is.EqualTo(e.rarity),e.id);Assert.That(a.maxStacks,Is.EqualTo(e.maxStacks),e.id);Effects(a.effects,e.effects,e.id);}
        }

        [Test]public void EnemiesMatchU0()
        {
            var expected=Read().enemies;Assert.That(Mamporro.Core.Catalog.Enemies.Length,Is.EqualTo(expected.Length));
            for(int i=0;i<expected.Length;i++){var a=Mamporro.Core.Catalog.Enemies[i];var e=expected[i];string w=e.id;
                Assert.That(a.id,Is.EqualTo(e.id));Assert.That(a.behavior,Is.EqualTo(e.behavior),w);
                CollectionAssert.AreEqual(new[]{e.hp,e.speed,e.agility,e.damage,e.radius,e.height,e.xp,e.mass},new[]{a.hp,a.speed,a.agility,a.damage,a.radius,a.height,a.xp,a.mass},w);
                Assert.That(a.goldChance,Is.EqualTo(e.gold.chance),w);Assert.That(a.goldMin,Is.EqualTo(e.gold.min),w);Assert.That(a.goldMax,Is.EqualTo(e.gold.max),w);
                // JsonUtility crea objetos vacíos cuando faltan: alcance 0 significa «sin disparo/embestida».
                if(e.ranged==null||e.ranged.range==0)Assert.That(a.ranged,Is.Null,w);
                else{var r=e.ranged;CollectionAssert.AreEqual(new[]{r.preferred,r.range,r.cooldown,r.windup,r.projectileSpeed,r.projectileDamage,r.projectileRadius},
                    new[]{a.ranged.preferred,a.ranged.range,a.ranged.cooldown,a.ranged.windup,a.ranged.projectileSpeed,a.ranged.projectileDamage,a.ranged.projectileRadius},w);}
                if(e.charge==null||e.charge.range==0)Assert.That(a.charge,Is.Null,w);
                else{var h=e.charge;CollectionAssert.AreEqual(new[]{h.cooldown,h.range,h.windup,h.dashTime,h.dashSpeed,h.damageMultiplier,h.recover},
                    new[]{a.charge.cooldown,a.charge.range,a.charge.windup,a.charge.dashTime,a.charge.dashSpeed,a.charge.damageMultiplier,a.charge.recover},w);}
            }
        }

        [Test]public void TuningMatchesU0()
        {
            var c=Read();
            Assert.That(Tuning.SpawnHpGrowth,Is.EqualTo(c.waves.SPAWN_CURVE.hpGrowth));Assert.That(Tuning.SpawnHpCurve,Is.EqualTo(c.waves.SPAWN_CURVE.hpCurve));
            Assert.That(Tuning.SpawnXpGrowth,Is.EqualTo(c.waves.SPAWN_CURVE.xpGrowth));Assert.That(Tuning.BossHpGrowth,Is.EqualTo(c.boss.hpGrowth));Assert.That(Tuning.BossHpCurve,Is.EqualTo(c.boss.hpCurve));
            Assert.That(Tuning.FillerMinGold,Is.EqualTo(c.run.GOLD_CONFIG.fillerMin));Assert.That(Tuning.FillerChestFraction,Is.EqualTo(c.run.GOLD_CONFIG.fillerChestFraction));
            Assert.That(Tuning.ChestBaseCost,Is.EqualTo(c.run.CHEST_CONFIG.baseCost));Assert.That(Tuning.ChestCostStep,Is.EqualTo(c.run.CHEST_CONFIG.costStep));Assert.That(Tuning.ChestCostCurve,Is.EqualTo(c.run.CHEST_CONFIG.costCurve));
            Assert.That(Rules.FillerGold(0),Is.EqualTo(10));
        }
    }
}
