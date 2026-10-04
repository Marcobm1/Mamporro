using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core.Progress;
using Mamporro.Persistence;

namespace Mamporro.Tests
{
    // Paso 5 de U4: importación de un archivo externo con revisión, confirmación, copia y
    // sustitución. Archivos reales solo en un directorio temporal propio de cada prueba.
    public sealed class ProgressImportTests
    {
        string root,tempRoot,storeDir;ProgressFiles files;
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false);

        // Decorador para inyectar fallos de E/S en una operación concreta.
        sealed class FailingFiles:IProgressFiles
        {
            readonly IProgressFiles inner;public string FailOn;
            public FailingFiles(IProgressFiles inner){this.inner=inner;}
            void Check(string op,string name){if(FailOn!=null&&(op+":"+name).StartsWith(FailOn,StringComparison.Ordinal))throw new IOException("fallo inyectado "+op);}
            public IDisposable Lock()=>inner.Lock();
            public byte[] Read(string name)=>inner.Read(name);
            public bool Exists(string name)=>inner.Exists(name);
            public void WriteNew(string name,byte[] data){Check("write",name);inner.WriteNew(name,data);}
            public void Publish(string source,string destination,bool replace){Check("publish",destination);inner.Publish(source,destination,replace);}
            public void DeleteTemporary(string name)=>inner.DeleteTemporary(name);
        }

        [SetUp] public void Setup()
        {
            tempRoot=Path.GetFullPath(Path.GetTempPath());
            root=Path.GetFullPath(Path.Combine(tempRoot,"Mamporro-U4-import-"+Guid.NewGuid().ToString("N")));
            storeDir=Path.Combine(root,"Progress");Directory.CreateDirectory(root);files=new ProgressFiles(storeDir);
        }
        [TearDown] public void Cleanup()
        {
            string full=Path.GetFullPath(root),parent=Path.GetFullPath(Path.GetDirectoryName(full));
            Assert.That(parent.TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(tempRoot.TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(full),Does.StartWith("Mamporro-U4-import-"));
            if(Directory.Exists(full))Directory.Delete(full,true);
        }

        // Guardados web congelados (u4-progress.json) y su progreso normalizado esperado.
        static Dictionary<string,object> Corpus(string id)
        {
            var reference=(Dictionary<string,object>)ProgressJson.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u4-progress.json")));
            foreach(Dictionary<string,object> entry in (List<object>)reference["saves"])if((string)entry["id"]==id)return entry;
            throw new ArgumentException(id);
        }
        static string ExpectedProgress(string id)
        {var data=(Dictionary<string,object>)((Dictionary<string,object>)Corpus(id)["result"])["data"];return ProgressJson.Stringify(ProgressTree.Object("settings",data["settings"],"meta",data["meta"]));}
        static string Transfer(ProgressDto progress,int saveVersion=3)
            =>ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,"source",ProgressTree.Object("platform","web","saveVersion",saveVersion),"progress",ProgressTree.From(progress)));
        string External(string name,string text){string path=Path.Combine(root,name);File.WriteAllText(path,text,Utf8);return path;}
        string External(string name,byte[] data){string path=Path.Combine(root,name);File.WriteAllBytes(path,data);return path;}
        ProgressImporter Importer(IProgressFiles source=null,string language="es")=>new ProgressImporter(new ProgressStore(source??files,language),language);
        bool PrimaryExists=>File.Exists(Path.Combine(storeDir,ProgressStore.PrimaryName));
        static ProgressDto Rich(){var p=ProgressDto.New("es");p.meta.coins=777;p.meta.characters=new[]{"remedios","baguette"};p.meta.selected="baguette";p.meta.extras.rerolls=2;return p;}

        [TestCase("v1-earliest")][TestCase("v1-options")][TestCase("v2-partial")][TestCase("v3-partial")][TestCase("v3-missions")][TestCase("v3-all")]
        public void WebSavesOfEveryVersionImportExactlyAndLeaveTheFileIntact(string id)
        {
            string path=External(id+".json",(string)Corpus(id)["raw"]);byte[] before=File.ReadAllBytes(path);
            var importer=Importer();var review=importer.Review(path);
            Assert.That(review.Error,Is.Null);Assert.That(review.CanConfirm,Is.True);Assert.That(review.Unchanged,Is.False);
            Assert.That(review.Baseline.Status,Is.EqualTo("new"));Assert.That(review.Current,Is.Null);
            Assert.That(PrimaryExists,Is.False,"revisar no escribe");
            var result=importer.Confirm(review,true);Assert.That(result.Success,Is.True,result.Error);
            Assert.That(ProgressJson.Stringify(ProgressTree.From(result.Snapshot.Candidate)),Is.EqualTo(ExpectedProgress(id)));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(path),"el archivo exportado no se modifica");
        }

        [Test] public void TransferV1FromTheWebExporterImportsWithoutMigratingAgain()
        {
            var progress=Rich();var path=External(ProgressImporter.DefaultFileName,Transfer(progress,1));
            var importer=Importer();var review=importer.Review(path);
            Assert.That(review.CanConfirm,Is.True);Assert.That(review.Report.Recovered,Is.False);
            Assert.That(importer.Confirm(review,true).Success,Is.True);
            var stored=new ProgressStore(files,"es").Load();Assert.That(stored.Status,Is.EqualTo("loaded"));
            Assert.That(ProgressTree.Stored(stored.Candidate),Is.EqualTo(ProgressTree.Stored(progress)));
        }

        [TestCase("{\"format\":\"mamporro.progress\",\"version\":2,\"source\":{\"platform\":\"web\",\"saveVersion\":3},\"progress\":{}}","import.invalid")]
        [TestCase("{\"version\":99,\"settings\":{},\"meta\":{}}","import.invalid")]
        [TestCase("{roto","import.invalid")]
        [TestCase("","import.invalid")]
        [TestCase("{\"format\":\"otro\",\"version\":1}","import.invalid")]
        public void FatalFilesAreRejectedWithoutWriting(string text,string error)
        {
            var review=Importer().Review(External("malo.json",text));
            Assert.That(review.Error,Is.EqualTo(error));Assert.That(review.CanConfirm,Is.False);
            Assert.That(review.Report.Issues,Has.Some.Matches<ValidationIssue>(i=>i.Severity=="fatal"));
            Assert.That(Importer().Confirm(review,true).Success,Is.False);Assert.That(PrimaryExists,Is.False);
        }

        [Test] public void MissingTooLargeAndDirectoryInputsAreReadErrorsNotNewProgress()
        {
            Assert.That(Importer().Review(Path.Combine(root,"no-existe.json")).Error,Is.EqualTo("import.missing"));
            Assert.That(Importer().Review("  ").Error,Is.EqualTo("import.missing"));
            Assert.That(Importer().Review(External("grande.json",new byte[ProgressJson.MaxBytes+1])).Error,Is.EqualTo("import.too-large"));
            Assert.That(Importer().Review(root).Error,Is.EqualTo("import.unreadable"));
            Assert.That(PrimaryExists,Is.False);
        }

        [Test] public void CriticalCorruptionIsRejectedInsteadOfGuessed()
        {
            var progress=Rich();
            string text=Transfer(progress).Replace("\"coins\":777","\"coins\":-5");
            var review=Importer().Review(External("negativo.json",text));
            Assert.That(review.Error,Is.EqualTo("import.invalid"));Assert.That(review.Incoming,Is.Null);
            text=Transfer(progress).Replace("\"selected\":\"baguette\"","\"selected\":\"desconocido\"");
            Assert.That(Importer().Review(External("seleccion.json",text)).Error,Is.EqualTo("import.invalid"));
            Assert.That(PrimaryExists,Is.False);
        }

        [Test] public void SecondaryProblemsAreReportedAndNormalizedBeforeConfirming()
        {
            string text=Transfer(Rich()).Replace("\"mouseSensitivity\":1","\"mouseSensitivity\":99");
            var review=Importer().Review(External("opciones.json",text));
            Assert.That(review.CanConfirm,Is.True);Assert.That(review.Report.Recovered,Is.True);
            Assert.That(review.Report.Issues,Has.Some.Matches<ValidationIssue>(i=>i.Path=="/progress/settings/mouseSensitivity"&&i.Action=="default"));
            Assert.That(review.Incoming.settings.mouseSensitivity,Is.EqualTo(1),"default, no clamp a 3");
            Assert.That(review.Incoming.meta.coins,Is.EqualTo(777));
        }

        [Test] public void CancellingWritesNothing()
        {
            var importer=Importer();var review=importer.Review(External("p.json",Transfer(Rich())));
            var result=importer.Confirm(review,false);
            Assert.That(result.Success,Is.False);Assert.That(result.Error,Is.EqualTo("storage.cancelled"));Assert.That(PrimaryExists,Is.False);
            Assert.That(Directory.GetFiles(storeDir,"progress*.json").Length,Is.Zero);
        }

        [Test] public void ImportingTheSameFileTwiceGivesTheSameProgressNotMore()
        {
            string path=External("p.json",Transfer(Rich()));var importer=Importer();
            Assert.That(importer.Confirm(importer.Review(path),true).Success,Is.True);
            byte[] primary=File.ReadAllBytes(Path.Combine(storeDir,ProgressStore.PrimaryName));
            var again=importer.Review(path);Assert.That(again.Unchanged,Is.True);Assert.That(again.Current.meta.coins,Is.EqualTo(777));
            var result=importer.Confirm(again,true);Assert.That(result.Success,Is.True);
            Assert.That(result.Snapshot.Candidate.meta.coins,Is.EqualTo(777),"no suma Calderilla");
            Assert.That(result.Snapshot.Candidate.meta.extras.rerolls,Is.EqualTo(2),"no suma extras");
            CollectionAssert.AreEqual(primary,File.ReadAllBytes(Path.Combine(storeDir,ProgressStore.PrimaryName)));
            Assert.That(File.Exists(Path.Combine(storeDir,ProgressStore.BackupName)),Is.False,"sin rotación de copia en una reimportación idéntica");
        }

        [Test] public void ExistingProgressIsReplacedWholeWithABackupNeverMerged()
        {
            var existing=Rich();existing.meta.coins=5000;existing.meta.weapons=new[]{"chancla","naftalina","barra","dentaduras","jersey"};
            var store=new ProgressStore(files,"es");Assert.That(store.Confirm(store.Prepare(existing,store.Load()),true).Success,Is.True);
            var incoming=ProgressDto.New("es");incoming.meta.coins=10;
            var importer=Importer();var review=importer.Review(External("p.json",Transfer(incoming)));
            Assert.That(review.Current.meta.coins,Is.EqualTo(5000));Assert.That(review.Incoming.meta.coins,Is.EqualTo(10));Assert.That(review.Unchanged,Is.False);
            var result=importer.Confirm(review,true);Assert.That(result.Success,Is.True);
            var now=result.Snapshot.Candidate;
            Assert.That(now.meta.coins,Is.EqualTo(10),"sustituye el saldo, no lo suma");
            CollectionAssert.AreEqual(incoming.meta.weapons,now.meta.weapons,"no une desbloqueos");
            Assert.That(now.meta.selected,Is.EqualTo("remedios"));
            var backup=ProgressValidator.Stored(File.ReadAllBytes(Path.Combine(storeDir,ProgressStore.BackupName)),"es");
            Assert.That(backup.Candidate.meta.coins,Is.EqualTo(5000),"copia del guardado anterior");
        }

        [Test] public void APreviewMadeStaleByAnotherWriteIsNotApplied()
        {
            var importer=Importer();var review=importer.Review(External("a.json",Transfer(Rich())));
            var other=ProgressDto.New("es");other.meta.coins=42;var store=new ProgressStore(files,"es");
            Assert.That(store.Confirm(store.Prepare(other,store.Load()),true).Success,Is.True);
            var result=importer.Confirm(review,true);
            Assert.That(result.Success,Is.False);Assert.That(result.Error,Is.EqualTo("storage.stale-preview"));
            Assert.That(new ProgressStore(files,"es").Load().Candidate.meta.coins,Is.EqualTo(42));
        }

        [TestCase("write:progress-")][TestCase("publish:progress.json")][TestCase("publish:progress.backup.json")]
        public void FailureDuringReplacementKeepsTheLastValidProgress(string failOn)
        {
            var existing=ProgressDto.New("es");existing.meta.coins=300;
            var store=new ProgressStore(files,"es");Assert.That(store.Confirm(store.Prepare(existing,store.Load()),true).Success,Is.True);
            var failing=new FailingFiles(files);var importer=Importer(failing);
            var review=importer.Review(External("p.json",Transfer(Rich())));Assert.That(review.CanConfirm,Is.True);
            failing.FailOn=failOn;var result=importer.Confirm(review,true);
            Assert.That(result.Success,Is.False);Assert.That(result.Error,Does.StartWith("storage.write-failed"));
            var after=new ProgressStore(files,"es").Load();Assert.That(after.Status,Is.EqualTo("loaded"));
            Assert.That(after.Candidate.meta.coins,Is.EqualTo(300),"se conserva el último progreso válido");
        }

        [Test] public void APlanCannotBeConfirmedTwiceAndImportNeverSettlesARun()
        {
            var progress=Rich();progress.meta.lastRun="partida-anterior";
            var importer=Importer();var review=importer.Review(External("p.json",Transfer(progress)));
            Assert.That(importer.Confirm(review,true).Success,Is.True);
            Assert.That(importer.Confirm(review,true).Success,Is.False,"el plan se consume una vez");
            var stored=new ProgressStore(files,"es").Load().Candidate;
            Assert.That(stored.meta.lastRun,Is.EqualTo("partida-anterior"));Assert.That(stored.meta.coins,Is.EqualTo(777));
            Assert.That(stored.meta.completed,Is.Empty,"no cobra misiones al importar");
        }
    }
}
