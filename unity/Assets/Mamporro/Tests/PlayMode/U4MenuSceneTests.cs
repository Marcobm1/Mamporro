using System.Collections;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.U2;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // Paso 7 de U4: menús completos ES/EN sobre el progreso permanente (carpeta temporal QaSave).
    public sealed class U4MenuSceneTests
    {
        QaSave qa;
        [TearDown]public void Release(){CombatText.English=false;qa?.Dispose();qa=null;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();

        [UnityTest]public IEnumerator NavigationShopAndLanguageWorkAndPersist()
        {
            var p=ProgressDto.New("es");p.meta.coins=500;p.meta.missions.kills=400;qa=QaSave.Use(p);yield return Load();var g=Game;var s=g.Screens;
            Assert.That(s.TitleVisible,Is.True);Assert.That(s.Page,Is.EqualTo(RunScreens.Home));
            Assert.That(s.TitleText,Does.Contain("Jugar").And.Contain("Personajes").And.Contain("Tienda").And.Contain("Misiones").And.Contain("Opciones").And.Contain("Calderilla del Caos: 500"));
            // Misiones: ocho, con progreso parcial.
            s.OpenPage(RunScreens.Missions);yield return null;
            Assert.That(s.TitleText,Does.Contain("Acumula 1000 bajas").And.Contain("400 / 1000").And.Contain("Derrota a la Pelusa Madre"));
            // Tienda: compras, repetición e insuficiencia, guardadas con el sistema seguro.
            s.OpenPage(RunScreens.Shop);yield return null;Assert.That(s.TitleText,Does.Contain("Comprar · 220").And.Contain("Comprar · 80"));
            Assert.That(g.Progress.Purchase("baguette"),Is.True);Assert.That(g.Progress.Purchase("baguette"),Is.False,"compra repetida");
            Assert.That(g.Progress.Purchase("jersey"),Is.True);Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(140));
            Assert.That(g.Progress.Purchase("baraja"),Is.False,"saldo insuficiente");s.OpenPage(RunScreens.Shop);yield return null;
            Assert.That(s.TitleText,Does.Contain("Faltan 20").And.Contain("Desbloqueado"));
            Assert.That(g.Progress.PurchaseExtra("rerolls"),Is.True);Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(60));
            var stored=qa.Stored;Assert.That(stored.meta.coins,Is.EqualTo(60));CollectionAssert.Contains(stored.meta.characters,"baguette");
            CollectionAssert.Contains(stored.meta.weapons,"jersey");Assert.That(stored.meta.extras.rerolls,Is.EqualTo(1));
            // Personajes: Baguette ya se puede elegir y la partida lo usa con sus usos.
            s.OpenPage(RunScreens.Characters);yield return null;Assert.That(s.CharacterLocked(1),Is.False);
            g.Character=Catalog.Characters[1];Assert.That(qa.Stored.meta.selected,Is.EqualTo("baguette"));
            g.StartRun("");Assert.That(g.Run.Character.id,Is.EqualTo("baguette"));Assert.That(g.Run.Rerolls,Is.EqualTo(3));Assert.That(g.Run.AllowedWeapons.Contains("jersey"),Is.True);
            g.BackToTitle();yield return Frames(2);Assert.That(s.Page,Is.EqualTo(RunScreens.Home));
            // Idioma: inglés inmediato en el menú abierto, guardado y conservado al recargar.
            s.OpenPage(RunScreens.Shop);yield return null;g.SetLanguage("en");yield return Frames(2);s=g.Screens;
            Assert.That(s.Page,Is.EqualTo(RunScreens.Shop));Assert.That(s.TitleText,Does.Contain("Shop / Unlocks").And.Contain("Chaos Change: 60").And.Not.Contain("Tienda"));
            Assert.That(qa.Stored.settings.language,Is.EqualTo("en"));
            yield return Load();g=Game;s=g.Screens;Assert.That(CombatText.English,Is.True);Assert.That(s.TitleText,Does.Contain("Play").And.Contain("Characters"));
            g.SetLanguage("es");yield return Frames(2);Assert.That(g.Screens.TitleText,Does.Contain("Jugar"));Assert.That(qa.Stored.settings.language,Is.EqualTo("es"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator SixCharacterDurationCombinationsStartFromTheSetupPage()
        {
            qa=QaSave.UseAllUnlocked();yield return Load();var g=Game;g.Screens.OpenPage(RunScreens.Setup);yield return null;
            Assert.That(g.Screens.TitleText,Does.Contain("5 min").And.Contain("Mapa actual"));
            foreach(var character in Catalog.Characters)foreach(int minutes in Catalog.RunDurations){
                g.Character=character;g.SetMinutes(minutes);g.StartRun("");
                Assert.That(g.Run.Character.id,Is.EqualTo(character.id));Assert.That(g.Session.Director.Minutes,Is.EqualTo(minutes));
                g.SetPaused(false);yield return new WaitForSeconds(.1f);Assert.That(g.Run.Time,Is.GreaterThan(0));g.SetPaused(true);g.BackToTitle();yield return null;
            }
            Assert.That(qa.Stored.settings.runMinutes,Is.EqualTo(15),"duración guardada");Assert.That(qa.Stored.meta.selected,Is.EqualTo("baguette"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator AbandonAsksForConfirmationAndGivesNothing()
        {
            var p=ProgressDto.New("es");p.meta.coins=30;qa=QaSave.Use(p);yield return Load();var g=Game;
            g.StartRun("");g.Run.Kills=500;g.SetPaused(true);yield return Frames(2);
            Assert.That(g.Screens.PauseVisible,Is.True);g.Screens.ShowAbandon(true);Assert.That(g.Screens.AbandonVisible,Is.True);
            g.Screens.ShowAbandon(false);Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing),"cancelar sigue en la partida");
            g.Screens.ShowAbandon(true);g.BackToTitle();yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Title));Assert.That(g.LastSettlement,Is.Null);Assert.That(qa.Stored.meta.coins,Is.EqualTo(30));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
