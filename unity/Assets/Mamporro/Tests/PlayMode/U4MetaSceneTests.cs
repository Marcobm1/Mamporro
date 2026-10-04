using System;
using System.Collections;
using System.IO;
using System.Text;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.Persistence;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Mamporro.Tests
{
    // Paso 6 de U4 en escena: progreso permanente → partida → resultados → liquidación única →
    // guardado → recarga. Cada prueba usa su carpeta temporal (QaSave); nunca el guardado real.
    public sealed class U4MetaSceneTests
    {
        QaSave qa;
        [TearDown]public void Release(){Time.timeScale=1;qa?.Dispose();qa=null;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        // Victoria sin acciones F3: jefe invocado por la API de combate y derrotado.
        static IEnumerator Win(U3Game g)
        {
            var r=g.Run;Assert.That(r.SpawnBoss(g.Body.X+6,g.Body.Z),Is.True);r.Enemies.Hp[r.Enemies.IndexOf(r.Boss.EnemyId)]=0;
            g.SetPaused(false);float end=Time.realtimeSinceStartup+10;
            while(g.State!=U3Game.Screen.Results){if(r.Choosing){while(r.Choosing)g.Choose(0);}Assert.That(Time.realtimeSinceStartup,Is.LessThan(end),"resultados");yield return null;}
        }

        [UnityTest]public IEnumerator NormalRunSettlesOncePersistsAndFeedsTheNextRun()
        {
            qa=QaSave.Use(null);yield return Load();var g=Game;
            Assert.That(g.Progress.Status,Is.EqualTo("new"));Assert.That(File.Exists(Path.Combine(qa.Directory,ProgressStore.PrimaryName)),Is.False);
            Assert.That(g.Screens.CharacterLocked(1),Is.True);g.Character=Catalog.Characters[1];
            Assert.That(g.Character.id,Is.EqualTo("remedios"),"Baguette bloqueado no se selecciona");
            g.StartRun("");var r=g.Run;
            Assert.That(r.Character.id,Is.EqualTo("remedios"));Assert.That(r.Rerolls,Is.EqualTo(2));Assert.That(r.Skips,Is.EqualTo(2));Assert.That(r.Banishes,Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[]{"chancla","naftalina","barra","dentaduras"},r.AllowedWeapons);Assert.That(r.AllowedItems.Contains("bata"),Is.False);
            r.Kills=400;yield return Win(g);
            var settlement=g.LastSettlement;Assert.That(settlement.Saved,Is.True);Assert.That(g.Session.Cheated,Is.False);
            CollectionAssert.AreEquivalent(new[]{"first","victory","noLife"},settlement.Receipt.completed);
            string text=g.Screens.ResultsMetaText;
            Assert.That(text,Does.Contain("Calderilla ganada: "+settlement.Receipt.total).And.Contain("Termina una partida").And.Contain("Progreso guardado"));
            Assert.That(g.Screens.RetrySaveVisible,Is.False);
            int coins=settlement.Receipt.total;Assert.That(qa.Stored.meta.coins,Is.EqualTo(coins));Assert.That(qa.Stored.meta.items,Does.Contain("bata"));
            // Repetir pantalla, reintentar guardado y volver al inicio no vuelven a pagar.
            g.Screens.ShowResults(true);g.RetrySave();g.Screens.ShowResults(true);Assert.That(qa.Stored.meta.coins,Is.EqualTo(coins));
            g.BackToTitle();yield return Frames(2);Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(coins));
            // Recarga desde disco (escena nueva): la recompensa existe una sola vez.
            yield return Load();g=Game;Assert.That(g.Progress.Status,Is.EqualTo("loaded"));Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(coins));
            Assert.That(g.Screens.TitleText,Does.Contain("Calderilla del Caos: "+coins));
            // Otra partida usa lo nuevo (bata desbloqueada por noLife) con contadores limpios.
            g.StartRun("");r=g.Run;Assert.That(r.AllowedItems.Contains("bata"),Is.True);Assert.That(r.Kills+r.Gold+r.Time,Is.Zero);Assert.That(r.Rerolls,Is.EqualTo(2));
            g.SetPaused(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator DebugRunGivesNoRewardAndWritesNothing()
        {
            qa=QaSave.Use(null);yield return Load();var g=Game;g.StartRun("");
            g.QaAction(7);Assert.That(g.Session.Cheated,Is.True);g.Run.Kills=5000;yield return Win(g);
            Assert.That(g.LastSettlement.NothingToSave,Is.True);Assert.That(g.LastSettlement.Receipt.total,Is.Zero);
            Assert.That(g.Screens.ResultsMetaText,Does.Contain("Partida con trucos"));
            Assert.That(File.Exists(Path.Combine(qa.Directory,ProgressStore.PrimaryName)),Is.False);Assert.That(g.Progress.Progress.meta.coins,Is.Zero);
            // Abrir F3 solo para mirar no marca trucos.
            g.Retry(false);yield return Frames(1);Assert.That(g.Session.Cheated,Is.False,"reintento limpio");
            g.SetPaused(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator AbandoningGivesNothingAndKeepsPermanentProgress()
        {
            var p=ProgressDto.New("es");p.meta.coins=50;qa=QaSave.Use(p);yield return Load();var g=Game;
            byte[] before=File.ReadAllBytes(Path.Combine(qa.Directory,ProgressStore.PrimaryName));
            g.StartRun("");g.Run.Kills=300;g.Run.GainGold(200);yield return new WaitForSeconds(.2f);g.SetPaused(true);
            g.BackToTitle();yield return Frames(2);
            Assert.That(g.LastSettlement,Is.Null);Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(50));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(Path.Combine(qa.Directory,ProgressStore.PrimaryName)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator UnlockedBaguetteStartsAndUsesPermanentExtras()
        {
            var p=ProgressDto.New("es");p.meta.characters=new[]{"remedios","baguette"};p.meta.selected="baguette";p.meta.extras.rerolls=3;p.meta.extras.banishes=1;
            qa=QaSave.Use(p);yield return Load();var g=Game;
            Assert.That(g.Character.id,Is.EqualTo("baguette"));Assert.That(g.Screens.CharacterLocked(1),Is.False);
            g.StartRun("");var r=g.Run;Assert.That(r.Character.id,Is.EqualTo("baguette"));Assert.That(r.Weapons[0].Def.id,Is.EqualTo("barra"));
            Assert.That(r.Rerolls,Is.EqualTo(5));Assert.That(r.Skips,Is.EqualTo(2));Assert.That(r.Banishes,Is.EqualTo(3));
            r.Rerolls=0;g.BackToTitle();g.Character=Catalog.Characters[0];Assert.That(qa.Stored.meta.selected,Is.EqualTo("remedios"),"selección guardada");
            g.StartRun("");Assert.That(g.Run.Character.id,Is.EqualTo("remedios"));Assert.That(g.Run.Rerolls,Is.EqualTo(5),"usos restaurados");
            g.SetPaused(true);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator InvalidSaveIsKeptShownAndNeverOverwritten()
        {
            qa=QaSave.Use(null);Directory.CreateDirectory(qa.Directory);string primary=Path.Combine(qa.Directory,ProgressStore.PrimaryName);
            File.WriteAllText(primary,"{roto",new UTF8Encoding(false));
            yield return Load();var g=Game;
            Assert.That(g.Progress.Status,Is.EqualTo("invalid"));Assert.That(g.Screens.TitleText,Does.Contain("no es válido"));
            g.StartRun("");yield return Win(g);
            Assert.That(g.LastSettlement.Saved,Is.False);Assert.That(g.Screens.ResultsMetaText,Does.Contain("no se ha guardado"));
            Assert.That(File.ReadAllText(primary),Is.EqualTo("{roto"),"el archivo se conserva");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
