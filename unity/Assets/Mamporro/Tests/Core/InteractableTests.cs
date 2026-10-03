using System;
using System.Collections.Generic;
using System.IO;
using Mamporro.Core;
using NUnit.Framework;
using UnityEngine;

namespace Mamporro.Tests
{
    // Interactuables: secciones chestCosts e interactables de u3-world.json (web aprobada) y
    // su uso en WorldRun sobre el mundo real (baúles, tótem, mesas camilla, armario y jefe).
    public sealed class InteractableTests
    {
        const double Dt=1.0/60;
        [Serializable] public class Reference { public double[] chestCosts;public InteractablesRef interactables; }
        [Serializable] public class InteractablesRef { public string seed;public int shrineIndex;public double[] pathX,pathZ;public LogRef[] log;public ChargeRef[] charge;public PromptRef[] prompts; }
        [Serializable] public class LogRef { public int tick,index;public string kind; }
        [Serializable] public class ChargeRef { public int tick,charging;public double charge; }
        [Serializable] public class PromptRef { public int tick,index;public string kind;public double cost; }
        static Reference cached;
        static Reference Ref=>cached??=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u3-world.json")));

        WorldData world;
        [OneTimeSetUp]public void Generate(){world=WorldData.Generate("MAMPORRO");}

        [Test]public void ChestCostsMatchWeb()
        {
            Assert.That(Ref.chestCosts.Length,Is.EqualTo(20));
            for(int n=0;n<20;n++)Assert.That(Rules.ChestCost(n),Is.EqualTo(Ref.chestCosts[n]),"baúl "+(n+1));
        }

        sealed class Log : IInteractableEvents
        {
            public readonly List<string> Items=new List<string>();public int Tick;public Interactables System;
            public void Discovered(InteractableState item)=>Items.Add($"{Tick} discovered {System.IndexOf(item)}");
            public void ShrineCharged(InteractableState item)=>Items.Add($"{Tick} shrineCharged {System.IndexOf(item)}");
        }
        [Test]public void DiscoveryChargeAndPromptsFollowTheWebRoute()
        {
            var e=Ref.interactables;Assert.That(e.seed,Is.EqualTo("MAMPORRO"));
            var system=new Interactables(world.Interactables);var log=new Log{System=system};
            Assert.That(Array.FindIndex(system.List,i=>i.Spot.Kind=="shrine"),Is.EqualTo(e.shrineIndex));
            int c=0;
            for(int k=0;k<e.pathX.Length;k++){
                log.Tick=k+1;system.Update(Dt,e.pathX[k],e.pathZ[k],log);
                if(log.Tick%30!=0)continue;
                var charge=e.charge[c];var prompt=e.prompts[c];c++;
                Assert.That(charge.tick,Is.EqualTo(log.Tick));
                Assert.That(system.List[e.shrineIndex].Charge,Is.EqualTo(charge.charge).Within(1e-9),"carga en el tick "+log.Tick);
                Assert.That(system.Charging,Is.EqualTo(charge.charging),"cargando en el tick "+log.Tick);
                bool has=system.Prompt(e.pathX[k],e.pathZ[k],out var p);
                Assert.That(has?p.Index:-1,Is.EqualTo(prompt.index),"aviso en el tick "+log.Tick);
                Assert.That(has?p.Kind:"",Is.EqualTo(prompt.kind));Assert.That(has?p.Cost:0,Is.EqualTo(prompt.cost));
            }
            Assert.That(c,Is.EqualTo(e.charge.Length));
            var expected=new List<string>();foreach(var x in e.log)expected.Add($"{x.tick} {x.kind} {x.index}");
            Assert.That(log.Items,Is.EqualTo(expected));
        }

        WorldRun New(int minutes=10)
        {
            var s=new WorldRun(world,"U3-INTERACTUABLES",Catalog.Characters[0],null,minutes){Automatic=false};
            s.Combat.WeaponsOff=true;s.Combat.Invincible=true;return s;
        }
        static int Index(WorldRun s,string kind,int nth=0)
        {for(int i=0;i<s.Interactables.List.Length;i++)if(s.Interactables.List[i].Spot.Kind==kind&&nth--==0)return i;return -1;}
        static void Stand(WorldRun s,int index,double dx=1,double dz=0)
        {var spot=s.Interactables.List[index].Spot;s.Body.PlaceAt(spot.X+dx,s.World.Collision.GroundHeight(spot.X+dx,spot.Z+dz,double.PositiveInfinity),spot.Z+dz);s.SyncPlayer();}
        static int Count(WorldRun s,string kind){int n=0;foreach(var e in s.Combat.Events)if(e.Kind==kind)n++;return n;}

        [Test]public void ChestsChargeGrowingPricesAndGiveItems()
        {
            var s=New();var r=s.Combat;int a=Index(s,"chest"),b=Index(s,"chest",1);
            Stand(s,a);Assert.That(s.Interact(),Is.False,"sin oro");Assert.That(Count(s,"noGold"),Is.EqualTo(1));
            Assert.That(s.Combat.Events.Find(e=>e.Kind=="noGold").Detail,Is.EqualTo("15"));
            r.GainGold(100);Assert.That(s.Interact(),Is.True);
            Assert.That(s.Interactables.List[a].Used,Is.True);Assert.That(s.Interactables.ChestsOpened,Is.EqualTo(1));Assert.That(r.ChestsOpened,Is.EqualTo(1));
            Assert.That(r.Items.Count,Is.EqualTo(1),"objeto del baúl");Assert.That(r.Gold,Is.EqualTo(85));
            Assert.That(Count(s,"chestOpened"),Is.EqualTo(1));Assert.That(Count(s,"item"),Is.EqualTo(1));
            Assert.That(s.Interact(),Is.False,"ya abierto");
            Stand(s,b);s.Step(default,Dt);Assert.That(s.HasPrompt,Is.True);Assert.That(s.Prompt.Cost,Is.EqualTo(30));
            Assert.That(s.Interact(),Is.True);Assert.That(r.Gold,Is.EqualTo(55));
        }

        [Test]public void FillerGoldCardUsesNextChestCost()
        {
            Assert.That(Offers.Gold(0).Amount,Is.EqualTo(10),"mínimo 10");
            Assert.That(Offers.Gold(3).Amount,Is.EqualTo(37),"mitad de 74");
            var s=New();var r=s.Combat;r.ChestsOpened=5;
            var cards=Offers.Generate(r.Weapons,r.Tomes,r.Stats,r.Banished,new Rng("U3-RELLENO"),40,null,null,r.ChestsOpened);
            var gold=cards.Find(c=>c.Kind=="gold");Assert.That(gold,Is.Not.Null);
            Assert.That(gold.Amount,Is.EqualTo(Math.Max(10,Rules.Round(Rules.ChestCost(5)*.5))));
        }

        [Test]public void TotemChallengeBoostsSpawnsLuckAndRewardsAnItem()
        {
            var s=New();s.Automatic=true;var r=s.Combat;int t=Index(s,"totem");Stand(s,t);
            double luck=r.Stats[Stat.luck];Assert.That(s.Interact(),Is.True);
            Assert.That(s.ChallengeLeft,Is.EqualTo(45));Assert.That(r.Stats[Stat.luck],Is.EqualTo(luck+50));
            s.Step(default,Dt);
            var p=new SpawnParams();s.Director.Params(r.Time,SpawnModifiers.None,ref p);
            Assert.That(s.Params.Rate,Is.EqualTo(p.Rate*2.2).Within(1e-12));Assert.That(s.Params.Hp,Is.EqualTo(p.Hp*1.25).Within(1e-12));Assert.That(s.Params.Gold,Is.EqualTo(p.Gold*2).Within(1e-12));
            Assert.That(s.Interact(),Is.False,"un desafío a la vez");
            for(int i=0;i<45*60+5&&s.ChallengeLeft>0;i++){s.Step(default,Dt);while(r.Choosing)r.Choose(0);}
            Assert.That(s.ChallengeLeft,Is.Zero);Assert.That(s.ChallengesCompleted,Is.EqualTo(1));
            Assert.That(Count(s,"challengeStart"),Is.EqualTo(1));Assert.That(Count(s,"challengeDone"),Is.EqualTo(1));
            Assert.That(Count(s,"item"),Is.EqualTo(1),"recompensa del tótem");Assert.That(r.ChallengeLuck,Is.Zero);
        }

        [Test]public void ShrineChargesWhileInsideAndOffersBlessings()
        {
            var s=New();var r=s.Combat;int i=Index(s,"shrine");Stand(s,i,.5,0);
            for(int k=0;k<300;k++)s.Step(default,Dt);
            var shrine=s.Interactables.List[i];Assert.That(shrine.Charge,Is.EqualTo(300/540.0).Within(1e-9));Assert.That(s.Interactables.Charging,Is.EqualTo(i));
            Stand(s,i,20,0);s.Step(default,Dt);Assert.That(s.Interactables.Charging,Is.EqualTo(-1));Assert.That(shrine.Charge,Is.LessThan(300/540.0));
            Stand(s,i,.5,0);for(int k=0;k<600&&!r.Choosing;k++)s.Step(default,Dt);
            Assert.That(shrine.Used,Is.True);Assert.That(s.ShrinesCompleted,Is.EqualTo(1));Assert.That(Count(s,"shrineCharged"),Is.EqualTo(1));
            Assert.That(r.Choosing,Is.True);Assert.That(r.ShrineOffer,Is.True);Assert.That(r.Offer.Count,Is.EqualTo(3));
            foreach(var c in r.Offer)Assert.That(c.Kind,Is.EqualTo("boost"));
            Assert.That(r.Reroll(),Is.False);Assert.That(r.Skip(),Is.False);Assert.That(r.Banish(0),Is.False);
            var card=r.Offer[0];var stat=Offers.Boost(card.Id).effect.stat;double before=r.Stats[stat];
            Assert.That(r.Choose(0),Is.True);Assert.That(r.Choosing,Is.False);Assert.That(r.Boosts.Count,Is.EqualTo(1));
            Assert.That(r.Stats[stat],Is.GreaterThan(before));
        }

        [Test]public void WardrobeSummonsMotherDustBehindAndVictoryWaitsOnePointSixSeconds()
        {
            var s=New();var r=s.Combat;int w=Index(s,"portal");Stand(s,w,1.5,0);
            Assert.That(s.Interact(),Is.True);Assert.That(r.Boss,Is.Not.Null);Assert.That(s.Interactables.List[w].Used,Is.True);
            int boss=r.Enemies.IndexOf(r.Boss.EnemyId);var spot=s.Interactables.List[w].Spot;
            Assert.That(JsMath.Hypot(r.Enemies.X[boss]-spot.X,r.Enemies.Z[boss]-spot.Z),Is.EqualTo(4).Within(1e-3),"a 4 m del armario");
            Assert.That(r.Enemies.X[boss],Is.LessThan(spot.X),"por detrás, lado contrario al jugador");
            Assert.That(Count(s,"bossSpawned"),Is.EqualTo(1));Assert.That(Count(s,"boss"),Is.EqualTo(1));
            Assert.That(s.Interact(),Is.False);
            r.Enemies.Hp[boss]=0;s.Step(default,Dt);
            Assert.That(r.Victory,Is.True);Assert.That(s.Finished,Is.False);double time=r.Time;
            int ticks=0;while(!s.Finished){s.Step(new PlayerIntent{MoveX=1},Dt);ticks++;Assert.That(ticks,Is.LessThan(200));}
            Assert.That(ticks,Is.InRange(95,97),"1,6 s");Assert.That(r.Time,Is.EqualTo(time),"la partida no avanza tras la victoria");
            Assert.That(r.OpenChoice(),Is.False);
        }

        [Test]public void SwarmRevealsTheWardrobeAndDebugRevealsAll()
        {
            var s=New(5);s.Automatic=true;var r=s.Combat;r.Time=299.9;
            for(int k=0;k<12;k++)s.Step(default,Dt);
            Assert.That(s.Swarm,Is.True);Assert.That(Count(s,"portalRevealed"),Is.EqualTo(1));
            Assert.That(s.Interactables.Find("portal").Discovered,Is.True);Assert.That(s.Cheated,Is.False);
            s.DebugRevealMap();Assert.That(s.Cheated,Is.True);
            foreach(var i in s.Interactables.List)Assert.That(i.Discovered,Is.True);
        }
    }
}
