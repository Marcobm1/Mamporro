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

        [UnityTest]public IEnumerator CameraArmFovShakeAndFlashesFollowTheWebAndTheOptions()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;g.Audio.SetFocused(true);
            g.StartRun("");yield return Frames(3);g.SetPaused(true);yield return Frames(2);
            Assert.That(g.Pitch,Is.EqualTo(U3Game.DefaultPitch).Within(.01f),"−0,3 rad web al empezar");Assert.That(g.ArmDistance,Is.EqualTo(6.2f).Within(.01f));
            // Sacudida: 0,45 al recibir un golpe y decae; sin la opción, nada y trauma a 0.
            g.Run.Invulnerable=0;g.Run.Hurt(5);Assert.That(g.Trauma,Is.EqualTo(.45f).Within(.001f));
            yield return new WaitForSecondsRealtime(.3f);Assert.That(g.Trauma,Is.LessThan(.45f));
            g.ChangeSettings(o=>o.cameraShake=false);yield return null;Assert.That(g.Trauma,Is.Zero);
            g.Shake(1);Assert.That(g.Trauma,Is.Zero,"desactivada no acumula");g.ChangeSettings(o=>o.cameraShake=true);g.Shake(2);Assert.That(g.Trauma,Is.EqualTo(1),"tope 1");
            // Brazo: mirando hacia arriba el terreno corta el brazo y la cámara se acerca de golpe; vuelve con suavidad.
            g.Look(new Vector2(0,10000));yield return Frames(2);Assert.That(g.Pitch,Is.EqualTo(U3Game.MinPitch).Within(.01f),"límite web 0,55 rad hacia arriba");
            float near=g.ArmDistance;Assert.That(near,Is.LessThan(6.2f));Assert.That(near,Is.GreaterThanOrEqualTo(1.1f));
            g.Look(new Vector2(0,-10000));yield return null;Assert.That(g.Pitch,Is.EqualTo(U3Game.MaxPitch).Within(.01f));
            float first=g.ArmDistance;Assert.That(first,Is.LessThan(6.2f),"no salta de vuelta");
            yield return new WaitForSecondsRealtime(1.5f);Assert.That(g.ArmDistance,Is.GreaterThan(first));
            // FOV: más abierto al superar la velocidad normal.
            Assert.That(g.worldCamera.fieldOfView,Is.EqualTo(70).Within(.5f));g.Body.Vx=40;yield return new WaitForSecondsRealtime(1);
            Assert.That(g.worldCamera.fieldOfView,Is.GreaterThan(78).And.LessThanOrEqualTo(82.01f));g.Body.Vx=0;
            // Parpadeo del jugador invulnerable con «Destellos de daño»; sin la opción, siempre visible.
            var avatar=g.transform.Find("Jugador provisional").gameObject;g.Run.Invulnerable=5;bool hidden=false,shown=false;
            float blinkUntil=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<blinkUntil){yield return null;if(avatar.activeSelf)shown=true;else hidden=true;}
            Assert.That(hidden&&shown,Is.True,"parpadea");
            g.ChangeSettings(o=>o.flashes=false);for(int i=0;i<10;i++){yield return null;Assert.That(avatar.activeSelf,Is.True);}
            g.Run.Invulnerable=0;g.Run.Hurt(5);yield return null;Assert.That(g.Hud.HurtFlashing,Is.False,"sin destello rojo");
            g.SetPaused(true);g.BackToTitle();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
