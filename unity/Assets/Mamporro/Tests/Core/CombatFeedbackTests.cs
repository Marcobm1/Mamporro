using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // U5 paso 3: el canal CombatFeedback solo observa. Las cuatro partidas de referencia
    // (u3-world.json) dan exactamente lo mismo con observador y sin él, y los recuentos de
    // sucesos cuadran con lo que la partida registra por su cuenta.
    public sealed class CombatFeedbackTests
    {
        sealed class Recorder:ICombatFeedback
        {
            public readonly int[] Counts=new int[Enum.GetValues(typeof(FeedbackKind)).Length];
            public int Crits,BossKills;public double PlayerDamage;public bool NonFinite;
            public void Feedback(in CombatFeedback f)
            {
                Counts[(int)f.Kind]++;
                if(f.Kind==FeedbackKind.Hit&&f.Code>0)Crits++;
                if(f.Kind==FeedbackKind.EnemyKilled&&Catalog.Enemies[f.Code].behavior=="boss")BossKills++;
                if(f.Kind==FeedbackKind.PlayerHit)PlayerDamage+=f.Value;
                foreach(double v in new[]{f.X,f.Y,f.Z,f.Value,f.Radius})if(double.IsNaN(v)||double.IsInfinity(v))NonFinite=true;
            }
            public int this[FeedbackKind k]=>Counts[(int)k];
        }
        static string Fingerprint(IntegratedRunTests.Played p)
        {
            var r=p.Run.Combat;
            return string.Join("|",p.Events.Select(e=>$"{e.tick} {e.kind} {e.detail}"))+"#"+
                string.Join("|",p.Detail.Select(d=>$"{d.tick} {d.x:R} {d.y:R} {d.z:R} {d.hp:R} {d.xp:R} {d.gold:R} {d.kills} {d.alive} {string.Join(",",d.ex)} {string.Join(",",d.ez)}"))+"#"+
                string.Join("|",p.Totals.Select(t=>$"{t.tick} {t.kills} {t.alive} {t.level} {t.gold:R} {t.hp:R} {t.spawned}"))+"#"+
                $"{p.FinishedTick} {r.Time:R} {r.Kills} {r.Level} {r.Gold:R} {r.Hp:R} {r.Victory} {r.Dead} {string.Join(",",r.Weapons.Select(w=>$"{w.Def.id}:{w.Level}:{w.Kills}:{w.TotalDamage:R}"))}";
        }
        static IEnumerable<string> Names()=>IntegratedRunTests.References.Select(r=>r.name);

        [Test]public void ReferenceRunsAreIdenticalWithAndWithoutObserver([ValueSource(nameof(Names))]string name)
        {
            var input=Array.Find(IntegratedRunTests.References,r=>r.name==name);
            var plain=IntegratedRunTests.Play(input);var recorder=new Recorder();var observed=IntegratedRunTests.Play(input,recorder);
            Assert.That(Fingerprint(observed),Is.EqualTo(Fingerprint(plain)),"misma RNG, cronología, daño, apariciones, botín y resultado");
            var r=observed.Run.Combat;var s=observed.Run;
            Assert.That(recorder[FeedbackKind.EnemyKilled],Is.EqualTo(r.Kills),"una baja por muerte");
            Assert.That(recorder[FeedbackKind.LevelUp],Is.EqualTo(r.Level-1),"una por nivel");
            Assert.That(recorder[FeedbackKind.ChestOpened],Is.EqualTo(s.Interactables.ChestsOpened));
            Assert.That(recorder[FeedbackKind.EnemySpawned],Is.EqualTo(s.Spawns.Spawned),"apariciones del director");
            Assert.That(recorder[FeedbackKind.WeaponFired],Is.GreaterThan(0));Assert.That(recorder[FeedbackKind.Hit],Is.GreaterThan(r.Kills));
            Assert.That(recorder[FeedbackKind.PickupXp],Is.GreaterThan(0));
            Assert.That(recorder[FeedbackKind.ItemGained],Is.EqualTo(observed.Events.Count(e=>e.kind=="item")),"uno por objeto concedido");
            Assert.That(recorder.BossKills,Is.EqualTo(r.Victory?1:0));
            Assert.That(recorder[FeedbackKind.BossSpawned],Is.EqualTo(observed.Events.Count(e=>e.kind=="bossSpawned")));
            Assert.That(recorder.NonFinite,Is.False);
            TestContext.WriteLine(name+": "+string.Join(", ",Enum.GetNames(typeof(FeedbackKind)).Select((k,i)=>k+" "+recorder.Counts[i])));
        }
    }
}
