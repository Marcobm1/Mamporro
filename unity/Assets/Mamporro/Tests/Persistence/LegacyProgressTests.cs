using System;
using System.IO;
using System.Text;
using Mamporro.Core.Progress;
using Mamporro.Persistence;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class LegacyProgressTests
    {
        string root,source,destination;ProgressStore store;ProgressImporter importer;
        [SetUp]public void Setup()
        {
            root=Path.Combine(Path.GetTempPath(),"Mamporro-U6-"+Guid.NewGuid().ToString("N"));
            source=Path.Combine(root,"Mamporro U1","Progress");destination=Path.Combine(root,"MAMPORRO","Progress");
            Directory.CreateDirectory(source);store=new ProgressStore(new ProgressFiles(destination),"es");importer=new ProgressImporter(store,"es");
        }
        [TearDown]public void Cleanup()
        {
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(root)),Is.EqualTo(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(root),Does.StartWith("Mamporro-U6-"));Directory.Delete(root,true);
        }
        static string Save(int coins=123){var p=ProgressDto.New("es");p.meta.coins=coins;p.meta.lastRun="already-settled";p.meta.characters=new[]{"remedios","baguette"};p.meta.selected="baguette";p.meta.extras.rerolls=2;return ProgressTree.Stored(p);}
        string Write(string json,string file=ProgressStore.PrimaryName){string path=Path.Combine(source,file);File.WriteAllText(path,json,new UTF8Encoding(false));return path;}
        [Test]public void PathsArePureAndOnlyTheNewWindowsIdentityDiscoversLegacy()
        {
            string product=Path.Combine(root,"Mamporro","MAMPORRO");
            var paths=ProgressLocations.Resolve(product,true,Array.Empty<string>());
            Assert.That(paths.Current,Is.EqualTo(Path.Combine(product,"Progress")));
            Assert.That(paths.Legacy,Is.EqualTo(Path.Combine(root,"Mamporro","Mamporro U1","Progress")));
            Assert.That(Directory.Exists(product),Is.False);
            Assert.That(ProgressLocations.Resolve(product,false,Array.Empty<string>()).Legacy,Is.Null);
            Assert.That(ProgressLocations.Resolve(Path.Combine(root,"Mamporro","Mamporro U1"),true,Array.Empty<string>()).Legacy,Is.Null);
        }
        [TestCase(true)][TestCase(false)]public void OverridesNeverResolveThePersonalSource(bool staticOverride)
        {
            string product=Path.Combine(root,"Mamporro","MAMPORRO");
            var paths=ProgressLocations.Resolve(product,true,staticOverride?Array.Empty<string>():new[]{"app","-u4-save-dir",destination},staticOverride?destination:null);
            Assert.That(paths.Current,Is.EqualTo(destination));Assert.That(paths.Legacy,Is.Null);
        }
        [TestCase("")][TestCase("-another-option")]
        public void MalformedOverrideCannotFallBackToPersonalProgress(string value)
        {Assert.Throws<ArgumentException>(()=>ProgressLocations.Resolve(root,true,new[]{"-u4-save-dir",value}));Assert.Throws<ArgumentException>(()=>ProgressLocations.Resolve(root,true,new[]{"-u4-save-dir"}));}
        [Test]public void ReviewCancelConfirmAndRetryPreserveOriginalAndEveryProgressField()
        {
            string original=Save(),path=Write(original);var review=importer.ReviewLegacy(source);
            Assert.That(review.CanConfirm,Is.True);Assert.That(review.SourcePath,Is.EqualTo(path));Assert.That(review.UsedBackup,Is.False);
            Assert.That(File.Exists(Path.Combine(destination,ProgressStore.PrimaryName)),Is.False);
            Assert.That(importer.Confirm(review,false).Success,Is.False);
            // Una revisión es una instantánea inmutable: ni mutar la vista previa inventa progreso.
            review.Incoming.meta.coins=999;
            Assert.That(importer.Confirm(review,true).Success,Is.True);
            Assert.That(ProgressTree.Stored(store.Load().Candidate),Is.EqualTo(original));
            var retry=importer.ReviewLegacy(source);Assert.That(retry.Unchanged,Is.True);Assert.That(importer.Confirm(retry,true).Success,Is.True);
            Assert.That(ProgressTree.Stored(store.Load().Candidate),Is.EqualTo(original));
            Assert.That(File.ReadAllText(path),Is.EqualTo(original));Assert.That(Directory.GetFiles(source).Length,Is.EqualTo(1),"ni lock ni backup nuevos en origen");
        }
        [Test]public void ExistingDestinationRequiresConfirmationAndIsBackedUpWithoutMerging()
        {
            var baseline=store.Load();var existing=ProgressValidator.Stored(Save(432),"es").Candidate;
            Assert.That(store.Confirm(store.Prepare(existing,baseline),true).Success,Is.True);
            Write(Save(123));var review=importer.ReviewLegacy(source);
            Assert.That(review.Current.meta.coins,Is.EqualTo(432));Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(432));
            Assert.That(importer.Confirm(review,true).Success,Is.True);Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(123));
            Assert.That(File.ReadAllText(Path.Combine(destination,ProgressStore.BackupName)),Is.EqualTo(Save(432)));
        }
        [TestCase("{broken")][TestCase("{\"format\":\"mamporro.unity-save\",\"version\":99}")]
        public void InvalidSourceIsRejectedAndItsBackupIsOfferedExplicitly(string bad)
        {
            string path=Write(bad);var invalid=importer.ReviewLegacy(source);Assert.That(invalid.CanConfirm,Is.False);
            Write(Save(),ProgressStore.BackupName);var recovered=importer.ReviewLegacy(source);
            Assert.That(recovered.CanConfirm,Is.True);Assert.That(recovered.UsedBackup,Is.True);Assert.That(recovered.SourcePath,Does.EndWith(ProgressStore.BackupName));
            Assert.That(importer.Confirm(recovered,true).Success,Is.True);Assert.That(File.ReadAllText(path),Is.EqualTo(bad));
            Assert.That(File.ReadAllText(Path.Combine(source,ProgressStore.BackupName)),Is.EqualTo(Save()));
        }
        [Test]public void MissingSourceIsNotAnInitialProgressCandidate()
        {var review=importer.ReviewLegacy(source);Assert.That(review.CanConfirm,Is.False);Assert.That(review.Error,Is.EqualTo("import.missing"));Assert.That(Directory.Exists(destination),Is.False);}
        [Test]public void SourceAboveParserLimitCannotBeImported()
        {Write(new string(' ',ProgressJson.MaxBytes+1));Assert.That(importer.ReviewLegacy(source).Error,Is.EqualTo("import.too-large"));Assert.That(Directory.Exists(destination),Is.False);}
        [Test]public void DestinationChangedAfterReviewCannotBeOverwritten()
        {
            Write(Save());var review=importer.ReviewLegacy(source);var initial=store.Load();
            Assert.That(store.Confirm(store.Prepare(ProgressValidator.Stored(Save(432),"es").Candidate,initial),true).Success,Is.True);
            Assert.That(importer.Confirm(review,true).Success,Is.False);Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(432));
        }
        [Test]public void WriteFailureKeepsBothCopiesAndCanBeRetried()
        {
            Write(Save());var baseState=store.Load();store.Confirm(store.Prepare(ProgressValidator.Stored(Save(432),"es").Candidate,baseState),true);
            var failing=new ProgressImporter(new ProgressStore(new FailWrites(new ProgressFiles(destination)),"es"),"es");
            Assert.That(failing.Confirm(failing.ReviewLegacy(source),true).Success,Is.False);
            Assert.That(store.Load().Candidate.meta.coins,Is.EqualTo(432));Assert.That(File.ReadAllText(Path.Combine(source,ProgressStore.PrimaryName)),Is.EqualTo(Save()));
            Assert.That(importer.Confirm(importer.ReviewLegacy(source),true).Success,Is.True);
        }
        sealed class FailWrites:IProgressFiles
        {
            readonly IProgressFiles inner;public FailWrites(IProgressFiles files){inner=files;}
            public IDisposable Lock()=>inner.Lock();public byte[] Read(string name)=>inner.Read(name);public bool Exists(string name)=>inner.Exists(name);
            public void WriteNew(string name,byte[] data)=>throw new IOException("Fallo de escritura inyectado");
            public void Publish(string source,string destination,bool replace)=>inner.Publish(source,destination,replace);
            public void DeleteTemporary(string name)=>inner.DeleteTemporary(name);
        }
    }
}
