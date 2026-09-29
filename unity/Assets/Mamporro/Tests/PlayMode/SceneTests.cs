using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.U1.Tests
{
    public sealed class SceneTests
    {
        [UnityTest] public IEnumerator SceneRunsPausesAndKeepsPool()
        {
            yield return SceneManager.LoadSceneAsync("U1_Patio");
            yield return null;
            var control=Object.FindAnyObjectByType<PrototypeController>();
            Assert.That(control,Is.Not.Null);
            Assert.That(control.worldCamera.targetTexture.height,Is.EqualTo(360));
            Assert.That(control.worldCamera.targetTexture.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(control.Horde.Count,Is.EqualTo(300));
            control.ConfigurePresentation(240,false,false);
            yield return null;
            Assert.That(control.worldCamera.targetTexture.height,Is.EqualTo(240));
            Assert.That(control.Dither,Is.False); Assert.That(control.Snap,Is.False);
            control.ConfigurePresentation(480,true,true);
            yield return null;
            Assert.That(control.worldCamera.aspect,Is.EqualTo((float)Screen.width/Screen.height).Within(.003f));
            control.ConfigurePresentation(360,true,true);
            var before=control.Horde.Positions[0];
            control.SetPaused(false);
            yield return new WaitForSeconds(.2f);
            Assert.That(control.Horde.Positions[0],Is.Not.EqualTo(before));
            Assert.That(control.RenderedFrames,Is.GreaterThan(0));
            var render=control.worldCamera.targetTexture;
            var prior=RenderTexture.active;
            RenderTexture.active=render;
            var pixels=new Texture2D(render.width,render.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,render.width,render.height),0,0);
            pixels.Apply(); RenderTexture.active=prior;
            var colors=pixels.GetPixels32(); int varied=0;
            for(int i=1;i<colors.Length;i++) if(!colors[i].Equals(colors[0])) varied++;
            Object.Destroy(pixels);
            Assert.That(varied,Is.GreaterThan(colors.Length/10),"La escena no debe producir una imagen uniforme.");
            control.SetPaused(true); before=control.Horde.Positions[0];
            yield return new WaitForSeconds(.1f);
            Assert.That(control.Horde.Positions[0],Is.EqualTo(before));
            control.ResetTrial(1000); Assert.That(control.Horde.Count,Is.EqualTo(1000));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
