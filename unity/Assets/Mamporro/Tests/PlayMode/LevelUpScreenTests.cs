using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Mamporro.Core;
using Mamporro.U2;

namespace Mamporro.Tests
{
    public sealed class LevelUpScreenTests
    {
        [TearDown]public void Spanish()=>CombatText.English=false;

        // Textos esperados tomados de src/i18n/es.ts y en.ts con el formato de src/ui/cards.ts.
        [TestCase(false,"Épica","Barra de Pan Duro","Nv 1 → 2","Barrazos +2","Prob. de crítico +10,5 %")]
        [TestCase(true,"Epic","Stale Baguette","Lv 1 → 2","Swings +2","Crit chance +10.5%")]
        public void WeaponUpgradeCardMatchesWebText(bool english,string tag,string title,string level,string line0,string line1)
        {
            CombatText.English=english;var run=new CombatRun(new CombatWorld(),"U2-CARTAS",Catalog.Characters[1]);
            var view=CardText.Describe(new Card{Kind="weaponUpgrade",Key="weapon:barra",Id="barra",Rarity="epic",Changes=new[]{WStat.count,WStat.critChance},Amounts=new[]{2,.105}},run);
            Assert.That(view.Tone,Is.EqualTo("epic"));Assert.That(view.Tag,Is.EqualTo(tag));Assert.That(view.Title,Is.EqualTo(title));Assert.That(view.Level,Is.EqualTo(level));
            CollectionAssert.AreEqual(new[]{line0,line1},view.Lines);Assert.That(view.Description,Is.Empty);Assert.That(view.Banishable,Is.True);
        }

        [TestCase(false,"Recetario del Caldito","Tomo nuevo","Vida máxima +26","Vida por segundo +0,5")]
        [TestCase(true,"Chicken Soup Cookbook","New tome","Max health +26","Health per second +0.5")]
        public void NewTomeCardMatchesWebText(bool english,string title,string level,string line0,string line1)
        {
            CombatText.English=english;var run=new CombatRun(new CombatWorld(),"U2-CARTAS",Catalog.Characters[0]);
            var view=CardText.Describe(new Card{Kind="tome",Key="tome:vitality",Id="vitality",Rarity="rare",Amounts=new[]{26,.52}},run);
            Assert.That(view.Title,Is.EqualTo(title));Assert.That(view.Level,Is.EqualTo(level));CollectionAssert.AreEqual(new[]{line0,line1},view.Lines);
            Assert.That(view.Description,Is.EqualTo(CombatText.Get("tome.vitality.desc")));
        }

        [TestCase(false,"Recuperas un 30 %","aparecen 10 de oro")]
        [TestCase(true,"recover 30%","turns up 10 gold")]
        public void FillerCardsUseWebFormula(bool english,string heal,string gold)
        {
            CombatText.English=english;var run=new CombatRun(new CombatWorld(),"U2-CARTAS",Catalog.Characters[0]);
            var h=CardText.Describe(Offers.Heal(),run);var g=CardText.Describe(Offers.Gold(),run);
            StringAssert.Contains(heal,h.Description);StringAssert.Contains(gold,g.Description);Assert.That(h.Banishable||g.Banishable,Is.False);
            Assert.That(Offers.Gold().Amount,Is.EqualTo(10));
        }

        [UnityTest]public IEnumerator ScreenGuardsInputAndAppliesActions()
        {
            yield return SceneManager.LoadSceneAsync("U2_Combate");yield return null;
            var s=Object.FindAnyObjectByType<CombatSession>();var screen=s.Cards;var r=s.Run;
            Assert.That(screen.Visible,Is.False);
            r.GainXp(Rules.XpNeeded(1)+Rules.XpNeeded(2));s.OpenChoice();
            Assert.That(r.PendingLevels,Is.EqualTo(2));Assert.That(screen.Visible,Is.True);Assert.That(screen.CardCount,Is.EqualTo(3));Assert.That(s.View.Paused,Is.True);

            // Durante los primeros 0,4 s no se acepta nada.
            screen.Press(0);screen.Action(0);Assert.That(r.PendingLevels,Is.EqualTo(2));Assert.That(r.Rerolls,Is.EqualTo(2));
            yield return new WaitForSecondsRealtime((float)Tuning.InputGuard+.05f);
            screen.Press(0);Assert.That(r.PendingLevels,Is.EqualTo(1));Assert.That(screen.Visible,Is.True);
            // Elegir abre la siguiente como nueva: vuelve la espera.
            screen.Press(0);Assert.That(r.PendingLevels,Is.EqualTo(1));
            yield return new WaitForSecondsRealtime((float)Tuning.InputGuard+.05f);

            // Descartar: B activa el modo, la carta se sustituye y el modo se apaga; sin nueva espera.
            screen.Action(2);Assert.That(screen.BanishMode,Is.True);
            string key=r.Offer[0].Key;screen.Press(0);
            Assert.That(r.Banishes,Is.EqualTo(1));Assert.That(r.Banished.Contains(key),Is.True);Assert.That(screen.BanishMode,Is.False);Assert.That(r.PendingLevels,Is.EqualTo(1));
            screen.Action(2);screen.SetBanishMode(false);Assert.That(screen.BanishMode,Is.False);
            screen.Action(0);Assert.That(r.Rerolls,Is.EqualTo(1));Assert.That(r.Choosing,Is.True);
            // Saltar cierra la última: la pantalla se oculta y la partida continúa.
            screen.Action(1);Assert.That(r.Skips,Is.EqualTo(1));Assert.That(r.Choosing,Is.False);Assert.That(screen.Visible,Is.False);Assert.That(s.View.Paused,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
