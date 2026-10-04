using System;
using System.IO;
using Mamporro.Core.Progress;

namespace Mamporro.Persistence
{
    // Revisión de un archivo externo antes de importarlo. Solo describe; no escribe nada.
    public sealed class ImportReview
    {
        // Código del primer fallo (import.* o storage.*); null si la revisión es utilizable.
        public string Error {get;internal set;}
        // Informe del validador sobre el archivo externo (null si no pudo leerse).
        public ProgressValidation Report {get;internal set;}
        // Estado del guardado local en el momento de la revisión (la confirmación lo compara).
        public StoredSnapshot Baseline {get;internal set;}
        public PendingProgress Pending {get;internal set;}
        // El archivo contiene exactamente el progreso ya guardado: confirmar no cambia nada.
        public bool Unchanged {get;internal set;}
        public ProgressDto Incoming=>Report?.Candidate;
        // Progreso guardado actual; null si no hay uno válido (nuevo, inválido o inaccesible).
        public ProgressDto Current=>Baseline!=null&&(Baseline.Status=="loaded"||Baseline.Status=="review"||Baseline.Status=="backup")?Baseline.Candidate:null;
        public bool CanConfirm=>Error==null&&Pending!=null&&Pending.CanConfirm;
    }

    // Importación U4: leer acotado → parsear → validar/migrar → normalizar → informe →
    // confirmación → backup → sustituir → releer. Reutiliza ProgressValidator.Import y
    // ProgressStore; nunca fusiona, suma saldos ni liquida partidas, y no modifica el archivo externo.
    public sealed class ProgressImporter
    {
        public const string DefaultFileName="mamporro-progreso.json";
        readonly ProgressStore store;readonly string language;
        public ProgressImporter(ProgressStore store,string language)
        {this.store=store??throw new ArgumentNullException(nameof(store));SettingsDto.Default(language);this.language=language;}

        // Lectura de solo lectura con el mismo límite que el parser (256 KiB); no crea ni bloquea el archivo.
        public static byte[] ReadExternal(string path,out string error)
        {
            error=null;
            try{
                if(string.IsNullOrWhiteSpace(path)){error="import.missing";return null;}
                string full=Path.GetFullPath(path.Trim().Trim('"'));
                if(Directory.Exists(full)){error="import.unreadable";return null;}
                using(var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){
                    if(stream.Length>ProgressJson.MaxBytes){error="import.too-large";return null;}
                    var data=new byte[(int)stream.Length];int offset=0;
                    while(offset<data.Length){int n=stream.Read(data,offset,data.Length-offset);if(n==0)throw new EndOfStreamException();offset+=n;}
                    if(stream.ReadByte()!=-1){error="import.unreadable";return null;}
                    return data;
                }
            }
            catch(FileNotFoundException){error="import.missing";}
            catch(DirectoryNotFoundException){error="import.missing";}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is NotSupportedException||e is System.Security.SecurityException){error="import.unreadable";}
            return null;
        }

        public ImportReview Review(string path)
        {
            var data=ReadExternal(path,out string error);
            return error!=null?new ImportReview {Error=error}:Review(data);
        }

        public ImportReview Review(byte[] data)
        {
            var review=new ImportReview {Report=ProgressValidator.Import(data??Array.Empty<byte>(),language)};
            if(!review.Report.CanConfirm){review.Error="import.invalid";return review;}
            review.Baseline=store.Load();
            if(review.Baseline.Status=="unavailable"){review.Error="storage.unavailable";return review;}
            review.Pending=store.Prepare(review.Incoming,review.Baseline);
            if(review.Pending.Error!=null){review.Error=review.Pending.Error;return review;}
            // Solo comparación de representaciones canónicas: no se combinan nunca.
            review.Unchanged=review.Baseline.Status=="loaded"&&ProgressTree.Stored(review.Baseline.Candidate)==ProgressTree.Stored(review.Incoming);
            return review;
        }

        // Estado actual del guardado local, para mostrarlo antes de revisar un archivo (solo lectura).
        public StoredSnapshot LoadCurrent()=>store.Load();

        // Cancelar (confirmed=false) no escribe. Una revisión con error no puede confirmarse.
        public StoreResult Confirm(ImportReview review,bool confirmed)
        {
            if(!confirmed)return new StoreResult {Error="storage.cancelled"};
            if(review==null||!review.CanConfirm)return new StoreResult {Error=review?.Error??"storage.invalid-plan"};
            return store.Confirm(review.Pending,true);
        }
    }
}
