using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Mamporro.Core.Progress;
using Mamporro.Persistence;

namespace Mamporro.Tests
{
    // Fallos deterministas sin escribir al sistema de archivos.
    public sealed class ProgressStoreTests
    {
        sealed class Release:IDisposable { readonly Action action;public Release(Action action){this.action=action;}public void Dispose()=>action(); }
        sealed class MemoryFiles:IProgressFiles
        {
            public readonly Dictionary<string,byte[]> Data=new Dictionary<string,byte[]>();
            public Action<string,string> Before,After;
            public int Writes;bool locked;
            public IDisposable Lock(){Before?.Invoke("lock","");if(locked)throw new IOException("ocupado");locked=true;return new Release(()=>locked=false);}
            public bool Exists(string name){Before?.Invoke("exists",name);return Data.ContainsKey(name);}
            public byte[] Read(string name){Before?.Invoke("read",name);return Data.TryGetValue(name,out var bytes)?(byte[])bytes.Clone():null;}
            public void WriteNew(string name,byte[] bytes){Before?.Invoke("write",name);if(Data.ContainsKey(name))throw new IOException("existe");Data.Add(name,(byte[])bytes.Clone());Writes++;After?.Invoke("write",name);}
            public void Publish(string source,string destination,bool replace){
                Before?.Invoke("publish",destination);if(Data.ContainsKey(destination)!=replace)throw new IOException("destino cambió");
                Data[destination]=Data[source];Data.Remove(source);After?.Invoke("publish",destination);
            }
            public void DeleteTemporary(string name){Before?.Invoke("delete",name);Data.Remove(name);}
        }
        static ProgressDto Progress(int coins){var data=ProgressDto.New("es");data.meta.coins=coins;return data;}
        static byte[] Bytes(ProgressDto data)=>Encoding.UTF8.GetBytes(ProgressTree.Stored(data));
        static MemoryFiles Existing(int coins=100)
        {var files=new MemoryFiles();files.Data.Add(ProgressStore.PrimaryName,Bytes(Progress(coins)));return files;}
        static void ExpectCoins(byte[] bytes,int coins)
        {var result=ProgressValidator.Stored(bytes,"es");Assert.That(result.CanConfirm,Is.True);Assert.That(result.Recovered,Is.False);Assert.That(result.Candidate.meta.coins,Is.EqualTo(coins));}

        [Test] public void NoFileAndOrphanTemporaryDoNotCreateProgressOnDisk()
        {
            var files=new MemoryFiles();files.Data.Add("progress-orphan.tmp",Bytes(Progress(999)));
            var load=new ProgressStore(files,"es").Load();Assert.That(load.Status,Is.EqualTo("new"));Assert.That(load.Candidate.meta.coins,Is.Zero);
            Assert.That(files.Writes,Is.Zero);Assert.That(files.Data.ContainsKey(ProgressStore.PrimaryName),Is.False);
        }
        [Test] public void PreparationAndCancellationDoNotWriteAndPreviewIsIsolated()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var candidate=Progress(50);
            var pending=store.Prepare(candidate,store.Load());candidate.meta.coins=900;pending.Preview.meta.coins=800;
            Assert.That(pending.Preview.meta.coins,Is.EqualTo(50));Assert.That(store.Confirm(pending,false).Success,Is.False);
            Assert.That(files.Writes,Is.Zero);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
        [Test] public void SubstitutionAndRepeatedImportNeverMergeOrPayAgain()
        {
            var files=Existing();var store=new ProgressStore(files,"es");
            var first=store.Confirm(store.Prepare(Progress(70),store.Load()),true);
            Assert.That(first.Success,Is.True);ExpectCoins(files.Data[ProgressStore.PrimaryName],70);ExpectCoins(files.Data[ProgressStore.BackupName],100);
            byte[] original=(byte[])files.Data[ProgressStore.PrimaryName].Clone();int writes=files.Writes;
            var again=store.Confirm(store.Prepare(Progress(70),store.Load()),true);
            Assert.That(again.Success,Is.True);CollectionAssert.AreEqual(original,files.Data[ProgressStore.PrimaryName]);
            Assert.That(files.Writes,Is.EqualTo(writes));ExpectCoins(files.Data[ProgressStore.BackupName],100);
        }
        [Test] public void ChangedDiskInvalidatesPreviewBeforeAnyWrite()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(10),store.Load());
            files.Data[ProgressStore.PrimaryName]=Bytes(Progress(200));
            var result=store.Confirm(pending,true);Assert.That(result.Error,Is.EqualTo("storage.stale-preview"));Assert.That(files.Writes,Is.Zero);
            ExpectCoins(files.Data[ProgressStore.PrimaryName],200);
        }
        [Test] public void PlanCannotBeReusedOrConfirmedByDifferentStore()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var other=new ProgressStore(files,"es");
            var pending=store.Prepare(Progress(70),store.Load());Assert.That(other.Confirm(pending,true).Success,Is.False);
            Assert.That(store.Confirm(pending,true).Success,Is.True);Assert.That(store.Confirm(pending,true).Success,Is.False);
        }
        [TestCase(1)][TestCase(2)]
        public void WriteFailureOrPartialTemporaryPreservesLastGoodPrimary(int failAt)
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(70),store.Load());int writes=0;
            files.After=(op,name)=>{if(op=="write"&&++writes==failAt){files.Data[name]=new byte[]{1,2};throw new IOException("escritura parcial");}};
            Assert.That(store.Confirm(pending,true).Success,Is.False);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
        [TestCase(1)][TestCase(2)]
        public void CorruptTemporaryIsDetectedBeforePublishing(int corruptAt)
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(70),store.Load());int writes=0;
            files.After=(op,name)=>{if(op=="write"&&++writes==corruptAt)files.Data[name]=Encoding.UTF8.GetBytes("broken");};
            Assert.That(store.Confirm(pending,true).Success,Is.False);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
        [TestCase(ProgressStore.BackupName)][TestCase(ProgressStore.PrimaryName)]
        public void PublishFailureDoesNotDestroyLastGoodState(string destination)
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(70),store.Load());
            files.Before=(op,name)=>{if(op=="publish"&&name==destination)throw new IOException("sustitución bloqueada");};
            Assert.That(store.Confirm(pending,true).Success,Is.False);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
        [Test] public void FailedFinalVerificationRollsBackAndKeepsBackup()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(70),store.Load());bool once=true;
            files.After=(op,name)=>{if(op=="publish"&&name==ProgressStore.PrimaryName&&once){once=false;files.Data[name]=Encoding.UTF8.GetBytes("broken");}};
            Assert.That(store.Confirm(pending,true).Success,Is.False);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);ExpectCoins(files.Data[ProgressStore.BackupName],100);
        }
        [Test] public void BackupStillRecoversWhenRollbackAlsoFails()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(70),store.Load());bool failRestore=false;
            files.After=(op,name)=>{if(op=="publish"&&name==ProgressStore.PrimaryName){files.Data[name]=Encoding.UTF8.GetBytes("broken");failRestore=true;}};
            files.Before=(op,name)=>{if(op=="write"&&failRestore)throw new IOException("sin espacio al recuperar");};
            var failed=store.Confirm(pending,true);Assert.That(failed.Success,Is.False);Assert.That(failed.Error,Does.Contain("recovery-required"));
            files.Before=null;files.After=null;var recovery=store.Load();Assert.That(recovery.Status,Is.EqualTo("backup"));Assert.That(recovery.Candidate.meta.coins,Is.EqualTo(100));
            Assert.That(store.Confirm(store.Prepare(recovery.Candidate,recovery),true).Success,Is.True);
            ExpectCoins(files.Data[ProgressStore.PrimaryName],100);ExpectCoins(files.Data[ProgressStore.BackupName],100);
            Assert.That(new List<string>(files.Data.Keys).Exists(k=>k.StartsWith("progress.corrupt.",StringComparison.Ordinal)),Is.True);
        }
        [Test] public void MissingPrimaryRecoversBackupOnlyAfterConfirmation()
        {
            var files=new MemoryFiles();files.Data[ProgressStore.BackupName]=Bytes(Progress(90));var store=new ProgressStore(files,"es");
            var load=store.Load();Assert.That(load.Status,Is.EqualTo("backup"));Assert.That(files.Data.ContainsKey(ProgressStore.PrimaryName),Is.False);
            Assert.That(store.Confirm(store.Prepare(load.Candidate,load),true).Success,Is.True);ExpectCoins(files.Data[ProgressStore.PrimaryName],90);
        }
        [Test] public void ReadFailureIsNotMistakenForNewProgress()
        {
            var files=Existing();files.Before=(op,name)=>{if(op=="read")throw new IOException("sin acceso");};
            var store=new ProgressStore(files,"es");var load=store.Load();Assert.That(load.Status,Is.EqualTo("unavailable"));Assert.That(load.Candidate,Is.Null);
            Assert.That(store.Prepare(Progress(0),load).CanConfirm,Is.False);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
        [Test] public void InvalidPrimaryAndBackupAreNotResetSilently()
        {
            var files=new MemoryFiles();files.Data[ProgressStore.PrimaryName]=Encoding.UTF8.GetBytes("{broken");files.Data[ProgressStore.BackupName]=Encoding.UTF8.GetBytes("{broken");
            var load=new ProgressStore(files,"es").Load();Assert.That(load.Status,Is.EqualTo("invalid"));Assert.That(load.Candidate,Is.Null);Assert.That(files.Writes,Is.Zero);
        }
        [Test] public void LockFailureWritesNothing()
        {
            var files=Existing();var store=new ProgressStore(files,"es");var pending=store.Prepare(Progress(2),store.Load());
            files.Before=(op,name)=>{if(op=="lock")throw new IOException("ocupado");};
            Assert.That(store.Confirm(pending,true).Success,Is.False);Assert.That(files.Writes,Is.Zero);ExpectCoins(files.Data[ProgressStore.PrimaryName],100);
        }
    }
}
