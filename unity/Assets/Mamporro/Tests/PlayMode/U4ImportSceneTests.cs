using System;
using System.Collections;
using System.IO;
using System.Text;
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
    // Paso 5 de U4 en escena: importar desde la pantalla de inicio con revisión, cancelación,
    // confirmación, reimportación sin cambios y archivo inválido. Guardado en una carpeta temporal.
    public sealed class U4ImportSceneTests
    {
        string root,tempRoot,realSave;bool realExisted;

        [SetUp] public void Setup()
        {
            tempRoot=Path.GetFullPath(Path.GetTempPath());
            root=Path.GetFullPath(Path.Combine(tempRoot,"Mamporro-U4-scene-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);
            U3Game.SaveDirectoryOverride=Path.Combine(root,"Progress");U3Game.DefaultLanguageOverride="es";
            realSave=Path.Combine(Application.persistentDataPath,"Progress");realExisted=Directory.Exists(realSave);
        }
        [TearDown] public void Cleanup()
        {
            U3Game.SaveDirectoryOverride=null;U3Game.DefaultLanguageOverride=null;
            Assert.That(Directory.Exists(realSave),Is.EqualTo(realExisted),"no se toca el guardado real");
            string full=Path.GetFullPath(root);
            Assert.That(Path.GetDirectoryName(full).TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(tempRoot.TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(full),Does.StartWith("Mamporro-U4-scene-"));
            if(Directory.Exists(full))Directory.Delete(full,true);
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        string Write(string name,string text){string path=Path.Combine(root,name);File.WriteAllText(path,text,new UTF8Encoding(false));return path;}
        static string Transfer(ProgressDto progress)
            =>ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,"source",ProgressTree.Object("platform","web","saveVersion",3),"progress",ProgressTree.From(progress)));
        string Primary=>Path.Combine(root,"Progress",ProgressStore.PrimaryName);

        [UnityTest]public IEnumerator TitleImportsReviewedProgressOnlyAfterConfirmation()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();var s=g.Screens;yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Title));Assert.That(File.Exists(Primary),Is.False,"arrancar sin guardado no escribe progreso");Assert.That(g.Progress.Status,Is.EqualTo("new"));
            s.ShowImport(true);yield return Frames(1);
            Assert.That(s.ImportVisible,Is.True);Assert.That(s.ImportText,Does.Contain("Ninguno"));Assert.That(s.ImportCanConfirm,Is.False);
            var progress=ProgressDto.New("es");progress.meta.coins=1234;progress.meta.characters=new[]{"remedios","baguette"};
            string path=Write("mamporro-progreso.json",Transfer(progress));byte[] original=File.ReadAllBytes(path);
            // Revisar y cancelar: nada escrito.
            s.ImportPath=path;s.ReviewImport();
            Assert.That(s.ImportCanConfirm,Is.True);Assert.That(s.ImportText,Does.Contain("1234").And.Contain("SUSTITUYE"));
            s.CancelImport();Assert.That(s.ImportMessage,Does.Contain("cancelada"));Assert.That(File.Exists(Primary),Is.False);
            // Revisar y confirmar: se guarda exactamente lo revisado.
            s.ReviewImport();s.ConfirmImport();
            Assert.That(s.ImportMessage,Does.Contain("importado"));Assert.That(File.Exists(Primary),Is.True);
            var stored=ProgressValidator.Stored(File.ReadAllBytes(Primary),"es").Candidate;
            Assert.That(stored.meta.coins,Is.EqualTo(1234));CollectionAssert.AreEqual(new[]{"remedios","baguette"},stored.meta.characters);
            Assert.That(s.ImportText,Does.Contain("1234"));
            Assert.That(g.Progress.Progress.meta.coins,Is.EqualTo(1234),"la sesión adopta lo importado sin reiniciar");Assert.That(g.Screens.CharacterLocked(1),Is.False);
            // Reimportar el mismo archivo: sin cambios ni más progreso.
            byte[] saved=File.ReadAllBytes(Primary);s.ReviewImport();
            Assert.That(s.ImportReview.Unchanged,Is.True);Assert.That(s.ImportText,Does.Contain("no cambiará nada"));
            s.ConfirmImport();Assert.That(s.ImportMessage,Does.Contain("idéntico"));CollectionAssert.AreEqual(saved,File.ReadAllBytes(Primary));
            // Archivo inválido: error visible, no se puede confirmar ni cambia el guardado.
            s.ImportPath=Write("roto.json","{\"format\":\"mamporro.progress\",\"version\":7}");s.ReviewImport();
            Assert.That(s.ImportCanConfirm,Is.False);Assert.That(s.ImportMessage,Does.Contain("no se puede importar"));
            Assert.That(s.ImportText,Does.Contain("impide importar"));CollectionAssert.AreEqual(saved,File.ReadAllBytes(Primary));
            s.ImportPath=Path.Combine(root,"no-existe.json");s.ReviewImport();Assert.That(s.ImportMessage,Does.Contain("No se encuentra"));
            CollectionAssert.AreEqual(original,File.ReadAllBytes(path),"el archivo exportado no se modifica");
            // Fuera de la pantalla de inicio la importación no está disponible.
            g.StartRun("");yield return Frames(2);Assert.That(s.ImportVisible,Is.False);g.SetPaused(true);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
