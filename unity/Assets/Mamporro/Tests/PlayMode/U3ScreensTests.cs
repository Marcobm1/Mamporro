using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // Paso 9: pantalla técnica de inicio (personaje, duración, semilla), resultados de
    // derrota y victoria (sin Calderilla), reintentar, nuevo mapa, volver al inicio y la
    // marca de trucos de la depuración F3.
    public sealed class U3ScreensTests
    {
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}

        [Test]public void SeedsAreNormalizedLikeTheWeb()
        {
            Assert.That(U3Game.NormalizeSeed("  u3-mundo! 7 "),Is.EqualTo("U3MUNDO7"));
            Assert.That(U3Game.NormalizeSeed("áé -- "),Is.Null);
            Assert.That(U3Game.NormalizeSeed("ABCDEFGHIJKLMNOP"),Is.EqualTo("ABCDEFGHIJKL"),"como mucho 12");
            var seed=U3Game.RandomSeed();Assert.That(seed.Length,Is.EqualTo(6));
            foreach(char c in seed)Assert.That("23456789ABCDEFGHJKLMNPQRSTUVWXYZ".IndexOf(c),Is.GreaterThanOrEqualTo(0));
        }

        [UnityTest]public IEnumerator TitleStartsTheChosenRunAndResultsCloseIt()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Title));Assert.That(g.Screens.TitleVisible,Is.True);
            Assert.That(g.Screens.TitleText,Does.Contain("MAMPORRO"));
            // Baguette, 5 minutos y semilla escrita a mano.
            g.Character=Catalog.Characters[1];g.Minutes=5;g.StartRun(" u3-mundo ");yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing));Assert.That(g.Seed,Is.EqualTo("U3MUNDO"));
            Assert.That(g.Session.Director.Minutes,Is.EqualTo(5));Assert.That(g.Run.Character.id,Is.EqualTo("baguette"));
            Assert.That(g.Run.Weapons[0].Def.id,Is.EqualTo("barra"));Assert.That(g.Screens.TitleVisible,Is.False);Assert.That(g.Hud.Visible,Is.True);
            Assert.That(g.Paused,Is.False);yield return new WaitForSeconds(.3f);Assert.That(g.Run.Time,Is.GreaterThan(.1));
            // Derrota → resultados sin trucos.
            g.Run.Hurt(10000);Assert.That(g.Run.Dead,Is.True);yield return new WaitForSeconds(.15f);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Results));Assert.That(g.Screens.ResultsVisible,Is.True);Assert.That(g.Hud.Visible,Is.False);
            string text=g.Screens.ResultsText;
            Assert.That(text,Does.Contain("¡A la cama sin cenar!").And.Contain("Tiempo").And.Contain("Bajas").And.Contain("Nivel").And.Contain("U3MUNDO"));
            Assert.That(text,Does.Contain("Barra"),"daño por arma");Assert.That(text,Does.Not.Contain("trucos").And.Not.Contain("Calderilla"));
            g.SetPaused(false);Assert.That(g.Paused,Is.True,"no se reanuda desde resultados");
            // Reintentar: misma semilla, partida limpia.
            var old=g.Session;g.Retry(false);yield return Frames(2);
            Assert.That(g.Session,Is.Not.SameAs(old));Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing));Assert.That(g.Seed,Is.EqualTo("U3MUNDO"));
            Assert.That(g.Screens.ResultsVisible,Is.False);Assert.That(g.Run.Hp,Is.GreaterThan(0));Assert.That(g.Run.Kills,Is.Zero);
            // Victoria con depuración: se marca la partida con trucos.
            g.QaAction(1);g.QaAction(6);Assert.That(g.Run.Boss,Is.Not.Null);g.QaAction(5);
            Assert.That(g.Run.Victory,Is.True);Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing),"1,6 s antes de los resultados");
            yield return new WaitForSeconds(1.9f);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Results));text=g.Screens.ResultsText;
            Assert.That(text,Does.Contain("¡Victoria!").And.Contain("Partida con trucos de debug"));
            // Nuevo mapa desde resultados y vuelta al inicio desde la pausa.
            g.Retry(true);yield return Frames(2);Assert.That(g.Seed,Is.Not.EqualTo("U3MUNDO"));Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing));
            g.SetPaused(true);yield return Frames(2);Assert.That(g.Screens.PauseVisible,Is.True);
            g.BackToTitle();yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Title));Assert.That(g.Screens.TitleVisible,Is.True);Assert.That(g.Screens.PauseVisible,Is.False);
            Assert.That(g.Session.Cheated,Is.False);Assert.That(g.Run.Time,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
