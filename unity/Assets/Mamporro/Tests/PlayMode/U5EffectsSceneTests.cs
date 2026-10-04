using System.Collections;
using Mamporro.Core;
using Mamporro.U2;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // U5 pasos 4–6: partículas, números de daño, cámara, sacudida y destellos en la escena real.
    public sealed class U5EffectsSceneTests
    {
        QaSave qa;
        [TearDown]public void Release(){AudioListener.pause=false;CombatText.English=false;qa?.Dispose();qa=null;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        static IEnumerator Play(U3Game g,float seconds)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<until){yield return null;if(g.Run.Choosing)while(g.Run.Choosing)g.Choose(0);g.SetPaused(false);}
        }

        [UnityTest]public IEnumerator ParticlesFollowTheBudgetFreezeInPauseAndClearPerRun()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;g.Audio.SetFocused(true);
            g.StartRun("");g.QaAction(1);g.QaAction(4);g.QaAction(4);
            yield return Play(g,2.5f);
            var p=g.Particles;Assert.That(p.Active,Is.GreaterThan(0),"apariciones, golpes y bajas generan partículas");Assert.That(p.Active,Is.LessThanOrEqualTo(1500));
            Assert.That(g.Feedback.Counts[(int)FeedbackKind.EnemySpawned]+g.Feedback.Counts[(int)FeedbackKind.EnemyKilled],Is.GreaterThan(0));
            yield return null;Assert.That(g.ParticleView.Drawn,Is.EqualTo(p.Active),"todas se dibujan, en lote");
            // Ráfaga enorme: nunca más de 256 nuevas en un fotograma ni 1500 vivas.
            int before=p.Active;for(int i=0;i<200;i++)p.Burst(0,1,0,24,new uint[]{0xffffff},5,.14,5);
            Assert.That(p.Active-before,Is.LessThanOrEqualTo(256));
            // Pausa: quietas.
            g.SetPaused(true);yield return Frames(2);int paused=p.Active;float y=p.Y[0];yield return Frames(5);
            Assert.That(p.Active,Is.EqualTo(paused));Assert.That(p.Y[0],Is.EqualTo(y));
            // Reducir partículas: 400 como mucho, al momento y guardado.
            g.ChangeSettings(o=>o.reducedParticles=true);Assert.That(p.Active,Is.LessThanOrEqualTo(400));Assert.That(p.Budget.Reduced,Is.True);
            Assert.That(qa.Stored.settings.reducedParticles,Is.True);
            g.SetPaused(false);yield return Play(g,1);Assert.That(p.Active,Is.LessThanOrEqualTo(400));
            // Nueva partida: nada de la anterior.
            g.SetPaused(true);g.BackToTitle();Assert.That(p.Active,Is.Zero);
            g.ChangeSettings(o=>o.reducedParticles=false);Assert.That(p.Budget.Reduced,Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator DamageNumbersShowHitsAndDamageTakenAndClearPerRun()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;g.Audio.SetFocused(true);
            g.StartRun("");g.QaAction(4);g.QaAction(4);g.Run.Invincible=true;
            yield return Play(g,2.5f);
            var n=g.Numbers;Assert.That(g.Feedback.Counts[(int)FeedbackKind.Hit],Is.GreaterThan(0));
            Assert.That(n.Active,Is.GreaterThan(0).And.LessThanOrEqualTo(140));yield return null;Assert.That(g.NumberView.DrawnGlyphs,Is.GreaterThan(0),"se dibujan");
            // Daño recibido: número rojo del jugador.
            g.Run.Invincible=false;g.Run.Invulnerable=0;g.Run.Hurt(10);
            bool player=false;for(int i=0;i<n.Active;i++)if(n.KindOf(i)<0)player=true;Assert.That(player,Is.True);
            // Pausa: quietos; nueva partida: ninguno.
            g.SetPaused(true);yield return Frames(2);int paused=n.Active;yield return new WaitForSecondsRealtime(1);Assert.That(n.Active,Is.EqualTo(paused),"no caducan en pausa");
            g.BackToTitle();Assert.That(n.Active,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
