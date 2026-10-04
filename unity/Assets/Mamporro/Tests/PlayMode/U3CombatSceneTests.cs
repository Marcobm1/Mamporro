using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    public sealed class U3CombatSceneTests
    {
        // U4: progreso QA aislado (todo desbloqueado) en una carpeta temporal; nunca el guardado real.
        QaSave qa;[SetUp]public void UseQaSave(){qa=QaSave.UseAllUnlocked();}[TearDown]public void ReleaseQaSave(){qa.Dispose();}
        [UnityTest]public IEnumerator ControlledCombatMovesRendersAndOffersCards()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();var r=g.Run;
            Assert.That(g.combatTemplate,Is.Not.Null);Assert.That(g.combatTemplate.enableInstancing,Is.True);
            g.QaAction(4);Assert.That(r.Enemies.Count,Is.GreaterThan(50));Assert.That(g.Session.Cheated,Is.True);
            // Uno próximo comprueba el disparo automático sin esperar al anillo QA.
            r.Spawn(0,0,-5);g.QaAction(1);g.ScriptedIntent=new PlayerIntent{MoveX=1};g.SetPaused(false);
            long frames=g.RenderedFrames;yield return new WaitForSeconds(1.2f);
            Assert.That(g.Body.X,Is.GreaterThan(5));Assert.That(r.Time,Is.GreaterThan(1));
            Assert.That(g.RenderedFrames,Is.GreaterThan(frames));Assert.That(g.CombatView.DrawnInstances,Is.GreaterThan(100));
            Assert.That(r.Projectiles.Count+r.Kills,Is.GreaterThan(0));
            g.QaAction(2);Assert.That(g.Cards.Visible,Is.True);Assert.That(g.Paused,Is.True);
            Assert.That(g.Cards.CardCount,Is.EqualTo(3));
            double time=r.Time;g.Cards.Press(0);Assert.That(r.Choosing,Is.True,"guardia de entrada");
            yield return new WaitForSecondsRealtime(.45f);Assert.That(r.Time,Is.EqualTo(time));
            g.Cards.Press(0);Assert.That(r.Choosing,Is.False);Assert.That(g.Cards.Visible,Is.False);
            Assert.That(g.Paused,Is.False);g.SetPaused(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator ResetClearsPopulatedCombatPhysicsCardsEffectsAndRepeatedRuns()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();
            int children=g.transform.childCount;
            for(int repetition=0;repetition<3;repetition++){
                var old=g.Session;var r=g.Run;
                g.QaAction(4);g.QaAction(6);r.Hurt(23);r.GainGold(175);r.AddItem("gafas");r.AddTome("damage");
                r.AddWeapon("fregona");r.Banished.Add("tome:luck");r.Rerolls=0;r.Skips=0;r.Banishes=0;
                r.Projectiles.Spawn(2,1,0,1,0,1,10,.3);r.EnemyShots.Spawn(2,1,0,1,0,1,10,.3,damage:10);
                r.Gems.Spawn(30,1,0,5);r.Coins.Spawn(30,1,0,7);r.CrowdSlow=.3;r.Kills=12;r.Time=80;
                g.Body.Sliding=true;g.Body.Vy=10;g.Body.SlideCooldown=2;g.ScriptedIntent=new PlayerIntent{JumpHeld=true,MoveX=1};
                g.Emit(new CombatEffect{Kind="slam",Radius=5,Life=10});g.QaAction(2);
                Assert.That(r.Enemies.Count,Is.GreaterThan(0));Assert.That(r.Projectiles.Count,Is.GreaterThan(0));
                Assert.That(r.EnemyShots.Count,Is.GreaterThan(0));Assert.That(g.CombatView.EffectCount,Is.GreaterThan(0));
                Assert.That(g.Cards.Visible,Is.True);Assert.That(r.Hp,Is.LessThan(100));
                g.LoadWorld(repetition==1?"U3-MUNDO":"MAMPORRO");
                var fresh=g.Run;Assert.That(g.Session,Is.Not.SameAs(old));Assert.That(fresh,Is.Not.SameAs(r));
                Assert.That(fresh.Enemies.Count+fresh.Enemies.BigCount+fresh.Projectiles.Count+fresh.EnemyShots.Count+fresh.Gems.Count+fresh.Coins.Count,Is.Zero);
                Assert.That(g.CombatView.EffectCount+g.CombatView.DroppedEffects,Is.Zero);
                Assert.That(fresh.Boss,Is.Null);Assert.That(fresh.Offer,Is.Null);Assert.That(fresh.Choosing||fresh.Dead||fresh.Invincible||fresh.WeaponsOff,Is.False);
                Assert.That(fresh.Hp,Is.EqualTo(100));Assert.That(fresh.Level,Is.EqualTo(1));
                Assert.That(fresh.Time+fresh.Xp+fresh.Gold+fresh.GoldCollected+fresh.CrowdSlow+fresh.Invulnerable+fresh.ShieldCharge,Is.Zero);
                Assert.That(fresh.Kills+fresh.PendingLevels+fresh.Items.Count+fresh.Tomes.Count+fresh.Banished.Count,Is.Zero);
                Assert.That(fresh.Rerolls,Is.EqualTo(2));Assert.That(fresh.Skips,Is.EqualTo(2));Assert.That(fresh.Banishes,Is.EqualTo(2));
                Assert.That(fresh.Weapons.Count,Is.EqualTo(1));Assert.That(fresh.Weapons[0].Def.id,Is.EqualTo("chancla"));
                Assert.That(fresh.Weapons[0].TotalDamage,Is.Zero);Assert.That(g.Cards.Visible,Is.False);Assert.That(g.Cards.CardCount,Is.Zero);
                Assert.That(g.Body.X+g.Body.Z+g.Body.Vx+g.Body.Vy+g.Body.Vz+g.Body.SlideCooldown,Is.Zero);
                Assert.That(g.Body.Y,Is.EqualTo(g.World.Heightfield.HeightAt(0,0)));Assert.That(g.Body.Sliding,Is.False);
                Assert.That(g.ScriptedIntent,Is.Null);Assert.That(g.Paused,Is.True);Assert.That(g.Session.Cheated,Is.False);
                Assert.That(g.status.text,Does.Contain("Nivel 1").And.Contain("Enemigos 0").And.Not.Contain("QA con trucos"));
                var query=new int[8];Assert.That(fresh.Enemies.Query(0,0,100,query),Is.Zero);
                // Mutar el almacenamiento antiguo no contamina la nueva partida.
                r.Enemies.X[0]=999;r.Hp=0;r.GainGold(999);old.Body.X=999;
                g.SetPaused(false);yield return new WaitForSeconds(.15f);g.SetPaused(true);
                Assert.That(fresh.Time,Is.GreaterThan(0));Assert.That(fresh.Hp,Is.EqualTo(100));Assert.That(fresh.Gold,Is.Zero);
                Assert.That(g.Body.X,Is.Zero);Assert.That(fresh.Enemies.Count,Is.Zero);Assert.That(g.CombatView.DrawnInstances,Is.EqualTo(g.CombatView.InteractableInstances),"solo interactuables");
                yield return null;Assert.That(g.transform.childCount,Is.EqualTo(children),"sin acumulación de objetos de escena");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
