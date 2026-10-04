using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;
using Mamporro.Core.Progress;

namespace Mamporro.Tests
{
    public sealed class MetaProgressTests
    {
        [Serializable] public class Reference { public Scenario[] scenarios;public Save[] saves; }
        [Serializable] public class Scenario { public string id;public MetaDto initial;public Step[] steps; }
        [Serializable] public class Step { public Operation operation;public Result result;public MetaDto state; }
        [Serializable] public class Operation { public string kind,id,action;public MetaRun run; }
        [Serializable] public class Result { public bool success;public int kills,survival,victory,missions,total;public string[] completed; }
        [Serializable] public class Save { public string id;public SaveResult result; }
        [Serializable] public class SaveResult { public ProgressDto data; }
        [Serializable] public class Baseline { public CatalogData catalog; }
        [Serializable] public class CatalogData { public MetaData meta; }
        [Serializable] public class MetaData { public Mission[] MISSIONS;public Shop[] SHOP;public int[] EXTRA_PRICES;public string[] INITIAL_WEAPONS,INITIAL_ITEMS; }
        [Serializable] public class Mission { public string id;public int target,coins;public Unlock unlock; }
        [Serializable] public class Shop { public string id;public int price;public Unlock unlock; }
        [Serializable] public class Unlock { public string kind,id; }

        static string Read(string name)=>File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/"+name));
        static Reference ReadReference()
        {
            // JsonUtility no admite un campo unión objeto/bool/null. Solo se adapta
            // esa representación del fixture: ni los esperados ni sus valores cambian.
            string json=Regex.Replace(Read("u4-progress.json"),"\"result\": (true|false|null)",
                m=>m.Groups[1].Value=="null"?"\"result\": {}":"\"result\": {\"success\": "+m.Groups[1].Value+"}");
            return JsonUtility.FromJson<Reference>(json);
        }
        static void Same(MetaDto actual,MetaDto expected,string context)
            =>Assert.That(JsonUtility.ToJson(actual),Is.EqualTo(JsonUtility.ToJson(expected)),context);

        [TestCase("defeat-repeat-victory-repeat")]
        [TestCase("debug-abandon-empty-id")]
        [TestCase("victory-life-tome")]
        [TestCase("insufficient-and-repeated")]
        [TestCase("reward-boundaries-and-cap")]
        [TestCase("all-purchases-extras-and-missions")]
        public void RulesMatchFrozenWebSequence(string id)
        {
            var scenario=Array.Find(ReadReference().scenarios,s=>s.id==id);
            Assert.That(scenario,Is.Not.Null);
            var state=scenario.initial;
            foreach(var step in scenario.steps){
                var op=step.operation;
                switch(op.kind){
                    case "settle":
                        var receipt=MetaRules.Settle(state,op.run);var expected=step.result;
                        Assert.That(receipt.kills,Is.EqualTo(expected.kills));Assert.That(receipt.survival,Is.EqualTo(expected.survival));
                        Assert.That(receipt.victory,Is.EqualTo(expected.victory));Assert.That(receipt.missions,Is.EqualTo(expected.missions));
                        Assert.That(receipt.total,Is.EqualTo(expected.total));CollectionAssert.AreEqual(expected.completed,receipt.completed);
                        break;
                    case "purchase":Assert.That(MetaRules.Purchase(state,op.id),Is.EqualTo(step.result.success));break;
                    case "extra":Assert.That(MetaRules.PurchaseExtra(state,op.action),Is.EqualTo(step.result.success));break;
                    case "abandon":break; // La aplicación no liquida un abandono.
                    default:Assert.Fail("Operación desconocida");break;
                }
                Same(state,step.state,id+" "+op.kind);
            }
        }

        [TestCase("es")][TestCase("en")]
        public void NewProgressMatchesFrozenWebDefaults(string language)
        {
            var expected=Array.Find(ReadReference().saves,s=>s.id=="new-"+language).result.data;
            var actual=ProgressDto.New(language);Same(actual.meta,expected.meta,"meta inicial");
            foreach(var field in typeof(SettingsDto).GetFields()){
                if(field.FieldType==typeof(double))Assert.That((double)field.GetValue(actual.settings),Is.EqualTo((double)field.GetValue(expected.settings)).Within(1e-12),field.Name);
                else Assert.That(field.GetValue(actual.settings),Is.EqualTo(field.GetValue(expected.settings)),field.Name);
            }
        }

        [Test] public void MetaDefinitionsMatchFrozenU0()
        {
            var data=JsonUtility.FromJson<Baseline>(Read("baseline.json")).catalog.meta;
            Assert.That(MetaRules.Missions.Count,Is.EqualTo(data.MISSIONS.Length));
            for(int i=0;i<data.MISSIONS.Length;i++){
                var a=MetaRules.Missions[i];var e=data.MISSIONS[i];
                Assert.That(a.Id,Is.EqualTo(e.id));Assert.That(a.Target,Is.EqualTo(e.target));Assert.That(a.Coins,Is.EqualTo(e.coins));
                if(!string.IsNullOrEmpty(e.unlock?.id)){Assert.That(a.UnlockId,Is.EqualTo(e.unlock.id));Assert.That(a.UnlockKind,Is.EqualTo(e.unlock.kind));}
                else Assert.That(a.UnlockId,Is.Null);
            }
            Assert.That(MetaRules.Shop.Count,Is.EqualTo(data.SHOP.Length));
            for(int i=0;i<data.SHOP.Length;i++){
                var a=MetaRules.Shop[i];var e=data.SHOP[i];Assert.That(a.Id,Is.EqualTo(e.id));Assert.That(a.Price,Is.EqualTo(e.price));
                Assert.That(a.UnlockId,Is.EqualTo(e.unlock.id));Assert.That(a.UnlockKind,Is.EqualTo(e.unlock.kind));
            }
            CollectionAssert.AreEqual(data.EXTRA_PRICES,MetaRules.ExtraPrices);
            var state=MetaRules.New();CollectionAssert.AreEqual(data.INITIAL_WEAPONS,state.weapons);CollectionAssert.AreEqual(data.INITIAL_ITEMS,state.items);
        }

        [Test] public void SelectionRequiresOwnedCharacterAndDoesNotChargeAgain()
        {
            var state=MetaRules.New();Assert.That(MetaRules.Select(state,"baguette"),Is.False);
            Assert.That(MetaRules.Select(state,"other"),Is.False);Assert.That(state.selected,Is.EqualTo("remedios"));
            state.coins=220;Assert.That(MetaRules.Purchase(state,"baguette"),Is.True);
            Assert.That(MetaRules.Select(state,"baguette"),Is.True);Assert.That(state.coins,Is.Zero);
            Assert.That(MetaRules.Select(state,"remedios"),Is.True);Assert.That(state.characters.Length,Is.EqualTo(2));
        }

        [TestCase("rerolls")][TestCase("skips")][TestCase("banishes")]
        public void ExtraUsesAreIndependentAndBounded(string action)
        {
            var state=MetaRules.New();state.coins=440;
            Assert.That(MetaRules.InitialUses(state,action),Is.EqualTo(2));
            for(int level=1;level<=3;level++){
                Assert.That(MetaRules.PurchaseExtra(state,action),Is.True);
                Assert.That(MetaRules.InitialUses(state,action),Is.EqualTo(2+level));
            }
            Assert.That(state.coins,Is.Zero);Assert.That(MetaRules.PurchaseExtra(state,action),Is.False);
            Assert.That(MetaRules.PurchaseExtra(state,"other"),Is.False);
            foreach(var other in new[]{"rerolls","skips","banishes"})if(other!=action)Assert.That(state.extras.Get(other),Is.Zero);
        }

        [Test] public void SessionFiltersAreCopiesAndOffersExcludeLockedWeapons()
        {
            var state=MetaRules.New();var weapons=MetaRules.AllowedWeapons(state);var items=MetaRules.AllowedItems(state);
            state.coins=500;MetaRules.Purchase(state,"jersey");MetaRules.Purchase(state,"baraja");
            Assert.That(weapons.Contains("jersey"),Is.False);Assert.That(items.Contains("baraja"),Is.False);
            Assert.That(MetaRules.AllowedWeapons(state).Contains("jersey"),Is.True);
            Assert.That(MetaRules.AllowedItems(state).Contains("baraja"),Is.True);
            var character=Array.Find(Catalog.Characters,c=>c.id=="remedios");
            var tomes=new List<Tome>();var owned=new List<Weapon>();
            var stats=new PlayerStats();stats.Reset(character);int checkedWeapons=0;
            for(int seed=0;seed<50;seed++){
                var cards=Offers.Generate(owned,tomes,stats,new HashSet<string>(),new Rng("meta-"+seed),4,allowed:weapons);
                foreach(var card in cards)if(card.Kind=="newWeapon"){
                    Assert.That(weapons.Contains(card.Id),Is.True);checkedWeapons++;
                }
            }
            Assert.That(checkedWeapons,Is.GreaterThan(0));
        }

        [Test] public void NewStatesShareNoMutableProgress()
        {
            var first=ProgressDto.New("es");var second=ProgressDto.New("es");
            first.meta.coins=500;MetaRules.Purchase(first.meta,"baguette");MetaRules.PurchaseExtra(first.meta,"rerolls");
            first.settings.muted=true;first.meta.weapons[0]="other";
            Same(second.meta,MetaRules.New(),"sin referencias compartidas");Assert.That(second.settings.muted,Is.False);
        }

        [Test] public void LastRunPreventsDuplicateAfterDtoRoundTrip()
        {
            var state=MetaRules.New();var run=new MetaRun { id="persisted-run",time=300,kills=400,level=15 };
            Assert.That(MetaRules.Settle(state,run).total,Is.EqualTo(70));
            var loaded=JsonUtility.FromJson<MetaDto>(JsonUtility.ToJson(state));
            Assert.That(MetaRules.Settle(loaded,run).total,Is.Zero);Same(loaded,state,"no paga tras recargar DTO");
        }

        [Test] public void TrustedRunNumbersFollowWebRulesWithoutOverflow()
        {
            var state=MetaRules.New();state.coins=MetaRules.MaxCoins;
            var receipt=MetaRules.Settle(state,new MetaRun { id="extreme",time=double.PositiveInfinity,kills=double.MaxValue,
                chests=double.NaN,shrines=-1,challenges=0,level=1 });
            Assert.That(receipt.kills,Is.EqualTo(50000000));Assert.That(receipt.survival,Is.Zero);
            Assert.That(state.coins,Is.EqualTo(MetaRules.MaxCoins));Assert.That(state.missions.chests,Is.Zero);
            Assert.That(state.missions.shrines,Is.Zero);
        }
    }
}
