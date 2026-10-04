using System;
using System.IO;
using System.Text;
using Mamporro.Core.Progress;

namespace Mamporro.Persistence
{
    public sealed class StoredSnapshot
    {
        public string Status {get;internal set;}
        public string Error {get;internal set;}
        public ProgressValidation Validation {get;internal set;}
        public ProgressDto Candidate {get;internal set;}
        internal byte[] Primary,Backup;
        internal bool PrimaryClean;
        internal object Owner;
    }
    public sealed class PendingProgress
    {
        internal string Payload,Language;
        internal StoredSnapshot Baseline;
        internal object Owner;
        internal bool Used;
        public string Error {get;internal set;}
        public bool CanConfirm=>Payload!=null&&Error==null&&!Used;
        // Cada vista es independiente; modificar el DTO mostrado no cambia lo preparado.
        public ProgressDto Preview=>Payload==null?null:ProgressValidator.Stored(Payload,Language).Candidate;
    }
    public sealed class StoreResult
    {
        public bool Success {get;internal set;}
        public string Error {get;internal set;}
        public StoredSnapshot Snapshot {get;internal set;}
    }

    public sealed class ProgressStore
    {
        public const string PrimaryName="progress.json",BackupName="progress.backup.json";
        readonly IProgressFiles files;readonly string language;readonly object owner=new object();
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        public ProgressStore(IProgressFiles files,string language)
        {this.files=files??throw new ArgumentNullException(nameof(files));SettingsDto.Default(language);this.language=language;}
        static bool Equal(byte[] a,byte[] b)
        {if(a==null||b==null)return a==b;if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
        static bool IsIo(Exception error)=>error is IOException||error is UnauthorizedAccessException||error is NotSupportedException||error is System.Security.SecurityException;
        static string Temporary()=>"progress-"+Guid.NewGuid().ToString("N")+".tmp";
        StoredSnapshot Snapshot(byte[] primary,byte[] backup,ProgressValidation validation,string status)
            =>new StoredSnapshot {Owner=owner,Primary=primary,Backup=backup,Validation=validation,
                PrimaryClean=status=="loaded",Status=status,Candidate=validation?.Candidate};
        public StoredSnapshot Load()
        {
            try{using(files.Lock())return LoadLocked();}
            catch(Exception error)when(IsIo(error)){return new StoredSnapshot {Owner=owner,Status="unavailable",Error=error.Message};}
        }
        StoredSnapshot LoadLocked()
        {
            byte[] primary=files.Read(PrimaryName);ProgressValidation primaryReport=null;
            if(primary!=null){
                primaryReport=ProgressValidator.Stored(primary,language);
                if(primaryReport.CanConfirm)return Snapshot(primary,null,primaryReport,primaryReport.Recovered?"review":"loaded");
            }
            byte[] backup=files.Read(BackupName);
            if(backup!=null){var report=ProgressValidator.Stored(backup,language);
                if(report.CanConfirm)return Snapshot(primary,backup,report,"backup");}
            if(primary==null&&backup==null){var snapshot=Snapshot(null,null,null,"new");snapshot.Candidate=ProgressDto.New(language);return snapshot;}
            return Snapshot(primary,backup,primaryReport,"invalid");
        }
        public PendingProgress Prepare(ProgressDto candidate,StoredSnapshot baseline)
        {
            var pending=new PendingProgress {Owner=owner,Language=language,Baseline=baseline};
            if(baseline==null||baseline.Owner!=owner||baseline.Status=="unavailable"){
                pending.Error="storage.unavailable";return pending;
            }
            if(candidate==null){pending.Error="progress.invalid";return pending;}
            try{
                var report=ProgressValidator.Stored(ProgressTree.Stored(candidate),language);
                if(!report.CanConfirm||report.Recovered){pending.Error="progress.invalid";return pending;}
                pending.Payload=ProgressTree.Stored(report.Candidate);
            }catch(Exception error)when(error is ArgumentException||error is NullReferenceException){pending.Error="progress.invalid";}
            return pending;
        }
        public StoreResult Confirm(PendingProgress pending,bool confirmed)
        {
            if(!confirmed)return new StoreResult {Error="storage.cancelled"};
            if(pending==null||pending.Owner!=owner||!pending.CanConfirm)return new StoreResult {Error="storage.invalid-plan"};
            pending.Used=true;
            try{using(files.Lock())return CommitLocked(pending);}
            catch(Exception error)when(IsIo(error)){return new StoreResult {Error="storage.write-failed: "+error.Message};}
        }
        void VerifyCopy(string name,byte[] expected,bool normalized)
        {
            var actual=files.Read(name);
            if(!Equal(actual,expected)||actual==null)throw new IOException("La relectura no coincide");
            if(normalized){var report=ProgressValidator.Stored(actual,language);
                if(!report.CanConfirm||report.Recovered)throw new IOException("El guardado escrito no es válido");}
        }
        void PublishCopy(string destination,byte[] bytes,bool normalized)
        {
            string temporary=Temporary();
            try{
                files.WriteNew(temporary,bytes);VerifyCopy(temporary,bytes,normalized);
                files.Publish(temporary,destination,files.Exists(destination));VerifyCopy(destination,bytes,normalized);
            }finally{try{files.DeleteTemporary(temporary);}catch(Exception error)when(IsIo(error)){/* Se conserva el temporal si no puede retirarse. */}}
        }
        StoreResult CommitLocked(PendingProgress pending)
        {
            var baseline=pending.Baseline;byte[] current=files.Read(PrimaryName);
            if(!Equal(current,baseline.Primary)||(baseline.Backup!=null&&!Equal(files.Read(BackupName),baseline.Backup)))
                return new StoreResult {Error="storage.stale-preview"};
            byte[] bytes=Utf8.GetBytes(pending.Payload);
            if(Equal(current,bytes))return new StoreResult {Success=true,Snapshot=LoadLocked()};
            bool replacing=false;
            try{
                if(current!=null){
                    if(baseline.PrimaryClean)PublishCopy(BackupName,current,true);
                    else{
                        // Una entrada inválida nunca sustituye una copia válida.
                        // Se conserva literalmente en un archivo distinto antes de reemplazarla.
                        string preserved="progress.corrupt."+Guid.NewGuid().ToString("N")+".json";
                        files.WriteNew(preserved,current);VerifyCopy(preserved,current,false);
                    }
                }
                // Revisar otra vez antes de sustituir; el lock coordina instancias propias.
                if(!Equal(files.Read(PrimaryName),baseline.Primary))throw new IOException("El guardado cambió durante la operación");
                string temporary=Temporary();
                try{
                    files.WriteNew(temporary,bytes);VerifyCopy(temporary,bytes,true);
                    replacing=true;files.Publish(temporary,PrimaryName,current!=null);VerifyCopy(PrimaryName,bytes,true);
                }finally{try{files.DeleteTemporary(temporary);}catch(Exception error)when(IsIo(error)){/* No publicar un temporal pendiente al arrancar. */}}
                return new StoreResult {Success=true,Snapshot=LoadLocked()};
            }catch(Exception error)when(IsIo(error)){
                string recovery="";
                if(replacing&&current!=null){
                    try{PublishCopy(PrimaryName,current,baseline.PrimaryClean);}
                    catch(Exception restoreError)when(IsIo(restoreError)){recovery="; storage.recovery-required: "+restoreError.Message;}
                }
                return new StoreResult {Error="storage.write-failed: "+error.Message+recovery};
            }
        }
    }
}
