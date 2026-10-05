using System;
using System.Collections;
using System.IO;
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
    public sealed class U6LegacySceneTests
    {
        string root,source,destination;
        [SetUp]public void Setup()
        {
            root=Path.Combine(Path.GetTempPath(),"Mamporro-U6-scene-"+Guid.NewGuid().ToString("N"));source=Path.Combine(root,"Old");destination=Path.Combine(root,"New");
            Directory.CreateDirectory(source);U3Game.SaveDirectoryOverride=destination;U3Game.DefaultLanguageOverride="es";
        }
        [TearDown]public void Cleanup()
        {
            U3Game.SaveDirectoryOverride=null;U3Game.DefaultLanguageOverride=null;
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(root)),Is.EqualTo(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(root),Does.StartWith("Mamporro-U6-scene-"));Directory.Delete(root,true);
        }
        [UnityTest]public IEnumerator LegacyReviewCancellationConfirmationAndRestartUseOnlyTemporaryProgress()
        {
            var p=ProgressDto.New("en");p.meta.coins=456;p.meta.characters=new[]{"remedios","baguette"};p.meta.selected="baguette";
            string json=ProgressTree.Stored(p),path=Path.Combine(source,ProgressStore.PrimaryName);File.WriteAllText(path,json);
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();var s=g.Screens;
            Assert.That(g.LegacySaveDirectory,Is.Null,"el override impide explorar el progreso personal");Assert.That(s.ImportVisible,Is.False);
            s.ShowLegacyImport(source);Assert.That(s.ImportVisible,Is.True);Assert.That(s.ImportCanConfirm,Is.True);
            Assert.That(s.ImportText,Does.Contain("456"));s.CancelImport();Assert.That(File.Exists(Path.Combine(destination,ProgressStore.PrimaryName)),Is.False);
            s.ReviewImport();s.ConfirmImport();yield return null;
            Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(456));Assert.That(g.Language,Is.EqualTo("en"));Assert.That(g.Character.id,Is.EqualTo("baguette"));
            Assert.That(s.ImportMessage,Does.Contain("imported"));Assert.That(File.ReadAllText(path),Is.EqualTo(json));
            s.ShowLegacyImport(source);Assert.That(s.ImportReview.Unchanged,Is.True);s.ConfirmImport();yield return null;
            Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(456));
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            g=Object.FindAnyObjectByType<U3Game>();Assert.That(g.Progress.Status,Is.EqualTo("loaded"));Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(456));
            Assert.That(g.Screens.ImportVisible,Is.False);Assert.That(Directory.GetFiles(source).Length,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator InvalidLegacyNeverReplacesDestinationAndBackupIsClearlyIdentified()
        {
            File.WriteAllText(Path.Combine(source,ProgressStore.PrimaryName),"{broken");
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();g.Screens.ShowLegacyImport(source);
            Assert.That(g.Screens.ImportCanConfirm,Is.False);g.Screens.ConfirmImport();Assert.That(File.Exists(Path.Combine(destination,ProgressStore.PrimaryName)),Is.False);
            File.WriteAllText(Path.Combine(source,ProgressStore.BackupName),ProgressTree.Stored(ProgressDto.New("es")));
            g.Screens.ReviewImport();Assert.That(g.Screens.ImportCanConfirm,Is.True);Assert.That(g.Screens.ImportText,Does.Contain("progress.backup.json"));
            g.Screens.ConfirmImport();Assert.That(g.Progress.Status,Is.EqualTo("loaded"));Assert.That(File.ReadAllText(Path.Combine(source,ProgressStore.PrimaryName)),Is.EqualTo("{broken"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
