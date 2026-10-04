using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Mamporro.Core;
using Mamporro.U3;

namespace Mamporro.Tests
{
    public sealed class U3SceneTests
    {
        // U4: progreso QA aislado (todo desbloqueado) en una carpeta temporal; nunca el guardado real.
        QaSave qa;[SetUp]public void UseQaSave(){qa=QaSave.UseAllUnlocked();}[TearDown]public void ReleaseQaSave(){qa.Dispose();}
        [UnityTest]public IEnumerator WorldBuildsAndPlayerWalksOnRealTerrain()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var game=Object.FindAnyObjectByType<U3Game>();Assert.That(game,Is.Not.Null);
            Assert.That(game.Seed,Is.EqualTo("MAMPORRO"));Assert.That(game.World.Sites.Count,Is.EqualTo(12));Assert.That(game.World.Interactables.Count,Is.EqualTo(20));
            Assert.That(game.Renderer.transform.childCount,Is.GreaterThan(8),"mallas del mundo");
            Assert.That(game.Body.X,Is.EqualTo(0));Assert.That(game.Body.Y,Is.EqualTo(game.World.Heightfield.HeightAt(0,0)).Within(1e-9));
            game.ScriptedIntent=new PlayerIntent{MoveX=1};game.SetPaused(false);
            yield return new WaitForSeconds(1.2f);
            Assert.That(game.Body.X,Is.GreaterThan(5),"avanza hacia +X");Assert.That(game.Body.Grounded||game.Body.OnSteep,Is.True);
            Assert.That(game.Body.Y,Is.GreaterThanOrEqualTo(game.World.Collision.GroundHeight(game.Body.X,game.Body.Z,game.Body.Y+0.45)-1e-6),"no atraviesa el suelo");
            Assert.That(game.RenderedFrames,Is.GreaterThan(0));
            game.SetPaused(true);game.LoadWorld("U3-MUNDO");Assert.That(game.Seed,Is.EqualTo("U3-MUNDO"));Assert.That(game.Body.X,Is.EqualTo(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
