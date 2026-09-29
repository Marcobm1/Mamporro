using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.Tests
{
    public sealed class ReferenceTests
    {
        [Serializable] public class Baseline { public Vector[] rng; public Formulas formulas; }
        [Serializable] public class Vector { public string seed; public uint[] hash,nextU32,terrainU32; }
        [Serializable] public class Formulas { public Xp[] xp; public Damage[] damage; public Armor[] armor; }
        [Serializable] public class Xp { public int level,needed; }
        [Serializable] public class Damage { public double @base,chance,multiplier,random; public Result result; }
        [Serializable] public class Result { public double amount; public int critLevel; }
        [Serializable] public class Armor { public double damage,armor,result; }
        Baseline Read() => JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/baseline.json")));
        [Test] public void RngMatchesIndependentU0Exactly()
        {
            foreach(var v in Read().rng) {
                CollectionAssert.AreEqual(v.hash,Rng.Hash(v.seed)); var r=new Rng(v.seed);
                foreach(uint n in v.nextU32) Assert.That(r.NextU32(),Is.EqualTo(n),v.seed);
                var derived=r.Derive("terrain"); foreach(uint n in v.terrainU32) Assert.That(derived.NextU32(),Is.EqualTo(n));
            }
        }
        [Test] public void FormulasMatchIndependentU0()
        {
            var f=Read().formulas;
            foreach(var v in f.xp) Assert.That(Rules.XpNeeded(v.level),Is.EqualTo(v.needed));
            foreach(var v in f.damage) {var damage=Rules.Damage(v.@base,v.chance,v.multiplier,v.random,out int crit);Assert.That(damage,Is.EqualTo(v.result.amount).Within(1e-9));Assert.That(crit,Is.EqualTo(v.result.critLevel));}
            foreach(var v in f.armor) Assert.That(Rules.Mitigate(v.damage,v.armor),Is.EqualTo(v.result).Within(1e-9));
        }
        [Test] public void DerivedStreamsDoNotDependOnParentConsumption()
        {var r=new Rng("ABC");uint first=r.Derive("offers").NextU32();for(int i=0;i<100;i++)r.NextU32();Assert.That(r.Derive("offers").NextU32(),Is.EqualTo(first));}
        [Test] public void ExperienceCarriesAcrossSeveralLevels()
        {int level=1;double xp=0;int amount=Rules.XpNeeded(1)+Rules.XpNeeded(2)+3;Assert.That(Rules.AddExperience(ref level,ref xp,amount),Is.EqualTo(2));Assert.That(level,Is.EqualTo(3));Assert.That(xp,Is.EqualTo(3));}
        [Test] public void BaseBonusesAddAndCapsApply()
        {
            var p=new PlayerStats();p.Reset(Catalog.Characters[0]);var basis=new PlayerStats();basis.Reset(Catalog.Characters[0]);
            var e=Catalog.Tomes[7].effects[0];p.Add(e,.5,basis);p.Add(e,.5,basis);Assert.That(p[Stat.pickupRadius],Is.EqualTo(6.4).Within(1e-9));
            p[Stat.attackSpeed]=99;p[Stat.extraProjectiles]=99;p.Cap();var w=new Weapon(Catalog.Weapons[0],0);w.Refresh(p);
            Assert.That(w[WStat.count],Is.EqualTo(7));Assert.That(w[WStat.cooldown],Is.EqualTo(.85/4).Within(1e-9));
        }
        [Test] public void GridPreservesBoundaryAndTieBehaviour()
        {
            var g=new SpatialGrid(96,4,4);g.Rebuild(new float[]{1,-1,5,0},new float[]{0,0,0,3},4);
            Assert.That(g.Nearest(0,0,1),Is.EqualTo(-1));var result=new int[4];Assert.That(g.Query(0,0,1,result),Is.EqualTo(2));
            Assert.That(g.Nearest(0,0,2),Is.EqualTo(0));
        }
    }
}
