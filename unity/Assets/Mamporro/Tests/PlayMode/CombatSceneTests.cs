using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Mamporro.Core;
using Mamporro.U1;
using Mamporro.U2;

namespace Mamporro.Tests
{
    public sealed class CombatSceneTests
    {
        [UnityTest]public IEnumerator CombatSceneRendersPausesAndRestartsCleanly()
        {
            yield return SceneManager.LoadSceneAsync("U2_Combate");yield return null;
            var s=Object.FindFirstObjectByType<CombatSession>();Assert.That(s,Is.Not.Null);Assert.That(s.View.Horde,Is.Null);Assert.That(s.View.Benchmark,Is.Null);
            Assert.That(s.Run.Weapons[0].Def.id,Is.EqualTo("chancla"));Assert.That(s.Run.Enemies.Count,Is.EqualTo(60));
            s.View.SetPaused(false);yield return new WaitForSeconds(.4f);Assert.That(s.Run.Time,Is.GreaterThan(.2));Assert.That(s.View.RenderedFrames,Is.GreaterThan(0));
            s.Run.GainXp(100);yield return new WaitForFixedUpdate();yield return null;Assert.That(s.Run.Choosing,Is.True);Assert.That(s.View.Paused,Is.True);
            double time=s.Run.Time;var pos=s.View.Motor.Position;yield return new WaitForSeconds(.1f);Assert.That(s.Run.Time,Is.EqualTo(time));Assert.That(s.View.Motor.Position,Is.EqualTo(pos));
            var target=s.View.worldCamera.targetTexture;var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=prior;
            var pixels=image.GetPixels32();int varied=0;for(int i=1;i<pixels.Length;i++)if(!pixels[i].Equals(pixels[0]))varied++;Object.Destroy(image);Assert.That(varied,Is.GreaterThan(pixels.Length/10));
            for(int i=0;i<3;i++){
                s.Restart(i%2);Assert.That(s.Run.Level,Is.EqualTo(1));Assert.That(s.Run.Hp,Is.EqualTo(100));Assert.That(s.Run.Kills,Is.Zero);Assert.That(s.Run.Items.Count,Is.Zero);Assert.That(s.Run.Projectiles.Count,Is.Zero);Assert.That(s.Run.Gems.Count,Is.Zero);Assert.That(s.Run.Banished.Count,Is.Zero);
                Assert.That(Object.FindObjectsByType<CombatSession>(FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(Object.FindObjectsByType<CombatRenderer>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                s.Run.AddItem("bata");s.Run.GainGold(500);s.View.SetPaused(false);yield return new WaitForSeconds(.1f);
            }
            s.Run.Invulnerable=0;s.Run.Items.Clear();s.Run.RefreshStats();s.Run.Hurt(1000);yield return new WaitForFixedUpdate();yield return null;Assert.That(s.View.Paused,Is.True);
            s.Restart(1);Assert.That(s.Run.Character.id,Is.EqualTo("baguette"));Assert.That(s.Run.Weapons[0].Def.id,Is.EqualTo("barra"));Assert.That(s.Run.Gold,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
