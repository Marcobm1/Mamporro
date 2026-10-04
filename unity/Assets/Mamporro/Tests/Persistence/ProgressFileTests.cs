using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Mamporro.Core.Progress;
using Mamporro.Persistence;

namespace Mamporro.Tests
{
    // Solo esta clase escribe archivos reales, siempre en su directorio temporal único.
    public sealed class ProgressFileTests
    {
        string directory,tempRoot;ProgressFiles files;
        [SetUp] public void Setup()
        {
            tempRoot=Path.GetFullPath(Path.GetTempPath());
            directory=Path.GetFullPath(Path.Combine(tempRoot,"Mamporro-U4-tests-"+Guid.NewGuid().ToString("N")));
            files=new ProgressFiles(directory);
        }
        [TearDown] public void Cleanup()
        {
            string full=Path.GetFullPath(directory),parent=Path.GetFullPath(Path.GetDirectoryName(full));
            Assert.That(parent.TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(tempRoot.TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(full),Does.StartWith("Mamporro-U4-tests-"));
            if(Directory.Exists(full))Directory.Delete(full,true);
        }
        [Test] public void ActualWindowsFilesSaveReloadBackupAndRestore()
        {
            var store=new ProgressStore(files,"es");var first=store.Load();Assert.That(first.Status,Is.EqualTo("new"));
            var progress=ProgressDto.New("es");progress.meta.coins=90;
            Assert.That(store.Confirm(store.Prepare(progress,first),true).Success,Is.True);
            progress.meta.coins=50;Assert.That(store.Confirm(store.Prepare(progress,store.Load()),true).Success,Is.True);
            Assert.That(new ProgressStore(files,"en").Load().Candidate.meta.coins,Is.EqualTo(50));
            Assert.That(ProgressValidator.Stored(files.Read(ProgressStore.BackupName),"es").Candidate.meta.coins,Is.EqualTo(90));
            File.WriteAllText(Path.Combine(directory,ProgressStore.PrimaryName),"{broken",new UTF8Encoding(false));
            var recovery=store.Load();Assert.That(recovery.Status,Is.EqualTo("backup"));
            Assert.That(store.Confirm(store.Prepare(recovery.Candidate,recovery),true).Success,Is.True);
            Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(90));
            var originals=Directory.GetFiles(directory,"progress.corrupt.*.json");Assert.That(originals.Length,Is.EqualTo(1));
            Assert.That(File.ReadAllText(originals[0]),Is.EqualTo("{broken"));
        }
        [Test] public void ExclusiveLockAndBoundedReadsWorkOnDisk()
        {
            using(files.Lock())Assert.Throws<IOException>(()=>files.Lock());
            File.WriteAllBytes(Path.Combine(directory,ProgressStore.PrimaryName),new byte[ProgressJson.MaxBytes+1]);
            Assert.Throws<IOException>(()=>files.Read(ProgressStore.PrimaryName));
            Assert.That(new ProgressStore(files,"es").Load().Status,Is.EqualTo("unavailable"));
            Assert.That(new FileInfo(Path.Combine(directory,ProgressStore.PrimaryName)).Length,Is.EqualTo(ProgressJson.MaxBytes+1));
        }
        [Test] public void PathsCannotEscapeDirectoryAndCleanupCannotDeleteSave()
        {
            Assert.Throws<ArgumentException>(()=>files.Read("../outside.json"));
            Assert.Throws<ArgumentException>(()=>files.Read("C:\\outside.json"));
            Assert.Throws<ArgumentException>(()=>files.DeleteTemporary(ProgressStore.PrimaryName));
            using(files.Lock()){
                var bytes=Encoding.UTF8.GetBytes("test");files.WriteNew("progress-own.tmp",bytes);
                Assert.Throws<IOException>(()=>files.WriteNew("progress-own.tmp",bytes));
                files.DeleteTemporary("progress-own.tmp");Assert.That(files.Read("progress-own.tmp"),Is.Null);
            }
        }
        [Test] public void ImportingSameExternalFileTwiceKeepsIdenticalStoredBytes()
        {
            var store=new ProgressStore(files,"es");var initial=store.Load();var existing=ProgressDto.New("es");existing.meta.coins=100;
            Assert.That(store.Confirm(store.Prepare(existing,initial),true).Success,Is.True);
            var incoming=ProgressDto.New("es");incoming.meta.coins=70;
            string transfer=ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,
                "source",ProgressTree.Object("platform","web","saveVersion",3),"progress",ProgressTree.From(incoming)));
            string source=Path.Combine(directory,"external-export.json");File.WriteAllText(source,transfer,new UTF8Encoding(false));
            byte[] original=File.ReadAllBytes(source),first=null;
            for(int i=0;i<2;i++){
                var validated=ProgressValidator.Import(File.ReadAllBytes(source),"es");Assert.That(validated.CanConfirm,Is.True);
                Assert.That(store.Confirm(store.Prepare(validated.Candidate,store.Load()),true).Success,Is.True);
                byte[] saved=files.Read(ProgressStore.PrimaryName);if(i==0)first=saved;else CollectionAssert.AreEqual(first,saved);
            }
            CollectionAssert.AreEqual(original,File.ReadAllBytes(source));Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(70));
            Assert.That(ProgressValidator.Stored(files.Read(ProgressStore.BackupName),"es").Candidate.meta.coins,Is.EqualTo(100));
        }
        [Test] public void LockedPrimaryFailsWithoutDestroyingOriginalOrBackup()
        {
            var store=new ProgressStore(files,"es");var data=ProgressDto.New("es");data.meta.coins=100;
            Assert.That(store.Confirm(store.Prepare(data,store.Load()),true).Success,Is.True);
            byte[] original=files.Read(ProgressStore.PrimaryName);data.meta.coins=50;
            var pending=store.Prepare(data,store.Load());
            using(new FileStream(Path.Combine(directory,ProgressStore.PrimaryName),FileMode.Open,FileAccess.Read,FileShare.Read))
                Assert.That(store.Confirm(pending,true).Success,Is.False);
            CollectionAssert.AreEqual(original,files.Read(ProgressStore.PrimaryName));
            CollectionAssert.AreEqual(original,files.Read(ProgressStore.BackupName));
        }
    }
}
