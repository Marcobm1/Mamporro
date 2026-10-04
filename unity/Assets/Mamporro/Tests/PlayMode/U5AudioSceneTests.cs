using System.Collections;
using Mamporro.Core.Audio;
using Mamporro.Core.Progress;
using Mamporro.U2;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mamporro.Tests
{
    // U5 paso 2: motor de audio en la escena real (guardado temporal QaSave). Comprueba estado,
    // fuentes, clips, modos, niveles, presupuesto y foco; que el sonido llegue al dispositivo se
    // mide en la build (comprobación visual, audio-report.json) y se escucha en la prueba manual.
    public sealed class U5AudioSceneTests
    {
        QaSave qa;
        [TearDown]public void Release(){AudioListener.pause=false;CombatText.English=false;qa?.Dispose();qa=null;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest]public IEnumerator OneListenerSynthesizedClipsAndMusicModesFollowTheGame()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;var a=g.Audio;a.SetFocused(true);
            TestContext.WriteLine($"Audio U5 en pruebas: salida {AudioSettings.outputSampleRate} Hz, altavoces {AudioSettings.speakerMode}");
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1),"una sola escucha");
            Assert.That(g.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(18),"16 efectos + 2 pistas");
            foreach(var def in AudioCatalog.Sounds){var clip=a.ClipOf(def.Id);Assert.That(clip.samples,Is.EqualTo(AudioSynth.Sound(def).Length));Assert.That(clip.frequency,Is.EqualTo(22050));}
            // Menú: pista normal al 65 % del volumen de música (0,5) y maestro 0,8.
            yield return Settle();
            Assert.That(a.Mode,Is.EqualTo(MusicMode.Menu));Assert.That(a.TrackIndex,Is.EqualTo(0));
            Assert.That(a.MusicLevel,Is.EqualTo(.5f*.65f).Within(.01f));Assert.That(a.MasterLevel,Is.EqualTo(.8f).Within(.01f));Assert.That(a.EffectsLevel,Is.EqualTo(.7f).Within(.01f));
            Assert.That(a.CurrentTrack.volume,Is.EqualTo(.8f*.5f*.65f).Within(.01f));
            // Partida, pausa, jefe (intensa a la misma posición del compás), cartas y resultados.
            g.StartRun("");yield return Settle();Assert.That(a.Mode,Is.EqualTo(MusicMode.Playing));Assert.That(a.MusicLevel,Is.EqualTo(.5f).Within(.01f));
            g.SetPaused(true);yield return Settle();Assert.That(a.Mode,Is.EqualTo(MusicMode.Paused));Assert.That(a.MusicLevel,Is.EqualTo(.125f).Within(.01f));Assert.That(a.TrackIndex,Is.EqualTo(0));
            g.SetPaused(false);g.QaAction(6);yield return Frames(3);
            Assert.That(a.Mode,Is.EqualTo(MusicMode.Intense));Assert.That(a.TrackIndex,Is.EqualTo(1),"arreglo intenso con el jefe");
            Assert.That(a.CurrentTrack.clip.samples,Is.EqualTo(AudioSynth.Music(true).Length));
            g.SetPaused(true);yield return Frames(2);Assert.That(a.TrackIndex,Is.EqualTo(0),"en pausa vuelve el arreglo normal, como la web");
            g.SetPaused(false);g.QaAction(2);yield return Frames(3);Assert.That(g.Cards.Visible,Is.True);Assert.That(a.Mode,Is.EqualTo(MusicMode.Paused),"cartas = pausa");
            while(g.Run.Choosing)g.Choose(0);
            int played=a.Played;g.QaAction(5);g.SetPaused(false);float until=Time.realtimeSinceStartup+5;
            while(g.State!=U3Game.Screen.Results&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Results));yield return Frames(2);
            Assert.That(a.Mode,Is.EqualTo(MusicMode.Results));Assert.That(a.Played,Is.GreaterThan(played),"suena la victoria");
            g.BackToTitle();yield return Frames(2);Assert.That(a.Mode,Is.EqualTo(MusicMode.Menu));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator VolumeOptionsMuteAndVoiceBudgetApplyImmediately()
        {
            qa=QaSave.Use(ProgressDto.New("es"));yield return Load();var g=Game;var a=g.Audio;a.SetFocused(true);yield return Settle();
            // Presupuesto: ráfaga de golpes y todas las señales importantes; nunca más de 16 voces.
            for(int i=0;i<500;i++)a.Play("hit");
            foreach(var def in AudioCatalog.Sounds)a.Play(def);
            Assert.That(a.BudgetCount,Is.LessThanOrEqualTo(16));Assert.That(a.ActiveVoices,Is.LessThanOrEqualTo(16));Assert.That(a.Dropped,Is.GreaterThanOrEqualTo(499),"los golpes repetidos se descartan, no se aplazan");
            // Volumen de música 0: se para la pista; efectos siguen.
            g.ChangeSettings(o=>o.musicVolume=0);yield return Settle();Assert.That(a.TrackIndex,Is.EqualTo(-1));Assert.That(a.CurrentTrack,Is.Null);
            a.ClearEffects();int played=a.Played;yield return new WaitForSecondsRealtime(.2f);a.Play("level");Assert.That(a.Played,Is.EqualTo(played+1));
            // Efectos 0: se vacían y no se admiten; música vuelve.
            g.ChangeSettings(o=>{o.effectsVolume=0;o.musicVolume=.8;});yield return Settle();
            Assert.That(a.ActiveVoices,Is.Zero);Assert.That(a.TrackIndex,Is.EqualTo(0));Assert.That(a.MusicLevel,Is.EqualTo(.8f*.65f).Within(.01f));
            played=a.Played;a.Play("reward");Assert.That(a.Played,Is.EqualTo(played),"sin efectos con volumen 0");
            // Silencio: maestro 0, sin pista ni efectos; guardado y conservado al recargar.
            g.ChangeSettings(o=>{o.effectsVolume=.7;o.muted=true;});yield return Settle();
            Assert.That(a.MasterLevel,Is.LessThan(.01f));Assert.That(a.TrackIndex,Is.EqualTo(-1));a.Play("ui");Assert.That(a.Played,Is.EqualTo(played));
            Assert.That(qa.Stored.settings.muted,Is.True);
            yield return Load();a=Game.Audio;yield return Settle();Assert.That(a.TrackIndex,Is.EqualTo(-1),"silencio tras recargar");
            Game.ChangeSettings(o=>o.muted=false);yield return Settle();Assert.That(a.TrackIndex,Is.EqualTo(0));Assert.That(a.MasterLevel,Is.EqualTo(.8f).Within(.01f));
            // Botones de la interfaz suenan («ui»).
            played=a.Played;yield return new WaitForSecondsRealtime(.1f);
            foreach(var b in Game.Screens.GetComponentsInChildren<Button>())if(b.interactable&&b.GetComponentInChildren<Text>().text==CombatText.Get("menu.shop")){b.onClick.Invoke();break;}
            Assert.That(a.Played,Is.EqualTo(played+1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator FocusLossSilencesDropsEffectsAndReturnsPausedAtQuarterMusic()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;var a=g.Audio;a.SetFocused(true);
            g.StartRun("");yield return Settle();for(int i=0;i<6;i++)a.Play(AudioCatalog.Sounds[1+i]);Assert.That(a.BudgetCount,Is.GreaterThan(0));
            g.SendMessage("OnApplicationFocus",false);yield return Frames(2);
            Assert.That(g.Paused,Is.True);Assert.That(a.Silenced,Is.True);Assert.That(AudioListener.pause,Is.True,"silencio total sin foco");
            Assert.That(a.ActiveVoices,Is.Zero);Assert.That(a.BudgetCount,Is.Zero,"efectos descartados");
            int played=a.Played;a.Play("hurt");Assert.That(a.Played,Is.EqualTo(played),"sin foco no se admite nada (ni se guarda para después)");
            g.SendMessage("OnApplicationFocus",true);yield return Settle();
            Assert.That(a.Silenced,Is.False);Assert.That(AudioListener.pause,Is.False);Assert.That(g.Paused,Is.True,"sigue en pausa");
            Assert.That(a.ActiveVoices,Is.Zero,"sin cola de efectos al volver");Assert.That(a.Mode,Is.EqualTo(MusicMode.Paused));Assert.That(a.MusicLevel,Is.EqualTo(.125f).Within(.01f));
            g.SetPaused(false);yield return Settle();Assert.That(a.MusicLevel,Is.EqualTo(.5f).Within(.01f),"volumen normal solo al continuar");
            g.SetPaused(true);g.BackToTitle();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
