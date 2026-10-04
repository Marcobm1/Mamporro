using System.Collections;
using Mamporro.U2;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // U5 paso 8: inicio → partida → resultado → nueva partida… tres veces, sin trucos.
    public sealed class U5SessionsSceneTests
    {
        QaSave qa;
        [TearDown]public void Release(){AudioListener.pause=false;CombatText.English=false;qa?.Dispose();qa=null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();

        [UnityTest]public IEnumerator ConsecutiveRunsDoNotCarryAudioEffectsPauseOrDuplicateProgress()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;var a=g.Audio;a.SetFocused(true);
            g.ChangeSettings(o=>{o.musicVolume=.4;o.reducedParticles=true;});
            string settings=qa.Stored.settings.musicVolume+"/"+qa.Stored.settings.reducedParticles;
            int coins=qa.Stored.meta.coins,receipts=0;
            for(int k=0;k<3;k++){
                if(k==0)g.StartRun("");else g.Retry(false);
                yield return null;
                Assert.That(g.Paused,Is.False,"sin pausa arrastrada");Assert.That(g.Particles.Active,Is.Zero);Assert.That(g.Numbers.Active,Is.Zero);
                Assert.That(g.Trauma,Is.Zero);Assert.That(a.ActiveVoices,Is.LessThanOrEqualTo(16));
                yield return new WaitForSecondsRealtime(1);
                g.Run.Invulnerable=0;g.Run.Hurt(100000);float until=Time.realtimeSinceStartup+4;
                while(g.State!=U3Game.Screen.Results&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(g.State,Is.EqualTo(U3Game.Screen.Results));var settled=g.LastSettlement;
                Assert.That(settled.Saved,Is.True);Assert.That(settled.NothingToSave,Is.False);receipts+=settled.Receipt.total;
                Assert.That(a.Mode,Is.EqualTo(MusicMode.Results));
                int playing=0;foreach(var s in g.GetComponentsInChildren<AudioSource>())if(s.loop&&s.isPlaying)playing++;
                Assert.That(playing,Is.LessThanOrEqualTo(1),"una sola música");
                Assert.That(g.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(18));
                Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            }
            Assert.That(qa.Stored.meta.coins,Is.EqualTo(coins+receipts),"una liquidación por partida, sin duplicados");
            Assert.That(qa.Stored.settings.musicVolume+"/"+qa.Stored.settings.reducedParticles,Is.EqualTo(settings),"opciones intactas");
            Assert.That(g.Particles.Budget.Reduced,Is.True);
            g.BackToTitle();yield return null;Assert.That(a.Mode,Is.EqualTo(MusicMode.Menu));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
