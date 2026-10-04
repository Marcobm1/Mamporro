using System;
using System.Collections.Generic;
using Mamporro.Core.Progress;

namespace Mamporro.Persistence
{
    // Configuración de una partida normal sacada del progreso permanente (copias; no se comparten).
    public sealed class RunSetup
    {
        public string Character,RunId;
        public HashSet<string> AllowedWeapons,AllowedItems;
        public int Rerolls,Skips,Banishes;
    }
    // Lo que la partida aporta a la liquidación (MetaRun de la web).
    public sealed class Settlement
    {
        public string RunId;
        public MetaReceipt Receipt;
        // true solo si el progreso liquidado quedó escrito y verificado.
        public bool Saved;
        // Código de fallo de guardado (storage.*), o el motivo por el que no se guarda.
        public string Error;
        // No había nada que guardar (partida con trucos o ya liquidada).
        public bool NothingToSave;
        internal ProgressDto Pending;
    }
    // Resultado de un cambio de opciones: aplicado (en memoria) y, si se pudo, guardado.
    public sealed class SettingsChange
    {
        public bool Applied,Saved;
        // value.invalid (rechazado, nada cambia) o el fallo de guardado (storage.*).
        public string Error;
    }

    // Progreso permanente de la aplicación sobre un único ProgressStore: carga al arrancar,
    // selección, configuración de partida y liquidación única (MetaRules.Settle + lastRun),
    // siempre guardada con Prepare → Confirm. Nunca resetea en silencio ni escribe JSON aparte.
    public sealed class ProgressSession
    {
        public readonly ProgressStore Store;
        public readonly string Language;
        // Último estado de Load/Confirm: loaded, new, review, backup, invalid, unavailable.
        public string Status {get;private set;}
        public StoredSnapshot Baseline {get;private set;}
        // Progreso vigente: el guardado válido, el inicial si no hay guardado, o el candidato que
        // espera confirmación de recuperación. Nunca contiene una liquidación no guardada.
        public ProgressDto Progress {get;private set;}
        public string LastError {get;private set;}
        Settlement settled;

        public ProgressSession(IProgressFiles files,string language)
        {Store=new ProgressStore(files,language);Language=language;Reload();}

        // Guardado utilizable sin confirmación previa. review/backup necesitan Recover;
        // invalid/unavailable conservan el archivo y juegan con progreso inicial en memoria sin guardar.
        public bool CanSave=>Status=="loaded"||Status=="new";
        public bool NeedsRecovery=>Status=="review"||Status=="backup";
        public IReadOnlyList<ValidationIssue> Issues=>(IReadOnlyList<ValidationIssue>)Baseline?.Validation?.Issues??Array.Empty<ValidationIssue>();

        public void Reload()
        {
            Baseline=Store.Load();Status=Baseline.Status;LastError=Baseline.Error;SettingsSessionOnly=false;
            Progress=Baseline.Candidate!=null?Copy(Baseline.Candidate):ProgressDto.New(Language);
        }
        ProgressDto Copy(ProgressDto source)=>ProgressValidator.Stored(ProgressTree.Stored(source),Language).Candidate;

        // Guardado seguro de un progreso completo; si falla, se conserva el anterior en memoria.
        StoreResult Save(ProgressDto next)
        {
            if(!CanSave)return new StoreResult {Error=NeedsRecovery?"storage.recovery-pending":"storage.unavailable"};
            var result=Store.Confirm(Store.Prepare(next,Baseline),true);
            if(result.Success)Adopt(result);else LastError=result.Error;
            return result;
        }
        // Estado nuevo tras un guardado o una importación confirmada con este mismo almacén.
        public void Adopt(StoreResult result)
        {
            if(result==null||!result.Success)return;
            Baseline=result.Snapshot;Status=Baseline.Status;LastError=null;SettingsSessionOnly=false;Progress=Copy(Baseline.Candidate);
        }

        // Recuperación confirmada por el usuario del candidato revisado (review/backup).
        public StoreResult Recover(bool confirmed)
        {
            if(!NeedsRecovery)return new StoreResult {Error="storage.invalid-plan"};
            var result=Store.Confirm(Store.Prepare(Baseline.Candidate,Baseline),confirmed);
            if(result.Success)Adopt(result);else if(confirmed)LastError=result.Error;
            return result;
        }

        // Selección persistida solo entre personajes desbloqueados (MetaRules.Select).
        public bool Select(string character)
        {
            if(Progress.meta.selected==character)return MetaRules.Owns(Progress.meta,"character",character);
            return Change(next=>MetaRules.Select(next.meta,character));
        }

        // Tienda (MetaRules.Purchase / PurchaseExtra) y opciones, guardadas igual que la selección.
        // Sin guardado disponible se aplican solo en memoria, como la web sin almacenamiento.
        public bool Purchase(string id)=>Change(next=>MetaRules.Purchase(next.meta,id));
        public bool PurchaseExtra(string action)=>Change(next=>MetaRules.PurchaseExtra(next.meta,action));
        public bool SetLanguage(string language)=>ChangeSettings(s=>s.language=language).Applied;
        public bool SetRunMinutes(int minutes)=>ChangeSettings(s=>s.runMinutes=minutes).Applied;

        // Cambio de opciones (las 14 de SettingsDto). Se valida con las mismas reglas que el
        // guardado (ProgressValidator): un valor fuera del contrato no se aplica. Si el guardado
        // falla o no está disponible, el cambio se queda en memoria solo para esta sesión (como
        // la web sin almacenamiento) y se conserva el último guardado válido.
        public SettingsChange ChangeSettings(Action<SettingsDto> apply)
        {
            var next=Copy(Progress);apply(next.settings);
            if(!ValidSettings(next))return new SettingsChange {Error="value.invalid"};
            if(ProgressTree.Stored(next)==ProgressTree.Stored(Progress))return new SettingsChange {Applied=true,Saved=CanSave&&!SettingsSessionOnly};
            if(!CanSave){Progress=next;return new SettingsChange {Applied=true,Error=NeedsRecovery?"storage.recovery-pending":"storage.unavailable"};}
            var result=Save(next);
            if(result.Success)return new SettingsChange {Applied=true,Saved=true};
            Progress=next;SettingsSessionOnly=true;
            return new SettingsChange {Applied=true,Error=result.Error};
        }
        // Hay opciones aplicadas que no se pudieron guardar (se escribirán con el próximo guardado correcto).
        public bool SettingsSessionOnly {get;private set;}
        bool ValidSettings(ProgressDto progress)
        {
            string json;try{json=ProgressTree.Stored(progress);}catch(ArgumentException){return false;}
            var validation=ProgressValidator.Stored(json,Language);
            if(validation.Candidate==null)return false;
            foreach(var issue in validation.Issues)if(issue.Code=="option.default")return false;
            return true;
        }
        bool Change(Func<ProgressDto,bool> apply)
        {
            var next=Copy(Progress);if(!apply(next))return false;
            if(!CanSave){Progress=next;return true;}
            return Save(next).Success;
        }

        // Personaje permitido para la próxima partida: el seleccionado si está desbloqueado.
        public string Character=>MetaRules.Owns(Progress.meta,"character",Progress.meta.selected)?Progress.meta.selected:"remedios";

        public RunSetup Begin()=>new RunSetup {
            Character=Character,RunId=Guid.NewGuid().ToString("N"),
            AllowedWeapons=MetaRules.AllowedWeapons(Progress.meta),AllowedItems=MetaRules.AllowedItems(Progress.meta),
            Rerolls=MetaRules.InitialUses(Progress.meta,"rerolls"),Skips=MetaRules.InitialUses(Progress.meta,"skips"),
            Banishes=MetaRules.InitialUses(Progress.meta,"banishes")};

        // Liquidación única de una partida terminada (victoria o derrota). Repetir con la misma
        // partida devuelve el mismo resultado y solo reintenta guardar el mismo progreso liquidado.
        public Settlement Settle(MetaRun run)
        {
            if(run==null||string.IsNullOrEmpty(run.id))throw new ArgumentException("Partida sin identidad",nameof(run));
            if(settled!=null&&settled.RunId==run.id)return RetrySave();
            var next=Copy(Progress);var receipt=MetaRules.Settle(next.meta,run);
            settled=new Settlement {RunId=run.id,Receipt=receipt};
            // Con trucos o ya liquidada (lastRun), MetaRules no cambia nada: no hay nada que guardar.
            if(ProgressTree.Stored(next)==ProgressTree.Stored(Progress)){settled.NothingToSave=true;settled.Saved=true;return settled;}
            settled.Pending=next;return RetrySave();
        }
        // Reintento manual del guardado de la última liquidación (idempotente por lastRun).
        public Settlement RetrySave()
        {
            if(settled==null||settled.NothingToSave||settled.Saved&&settled.Pending==null)return settled;
            var result=Save(settled.Pending);
            settled.Saved=result.Success;settled.Error=result.Success?null:result.Error;
            if(result.Success)settled.Pending=null;
            return settled;
        }
        // Configura una partida nueva con lo permanente: filtros (copias) y usos iniciales.
        public static void Apply(RunSetup setup,Mamporro.Core.WorldRun s)
        {
            var r=s.Combat;r.AllowedWeapons=new HashSet<string>(setup.AllowedWeapons,StringComparer.Ordinal);r.AllowedItems=new HashSet<string>(setup.AllowedItems,StringComparer.Ordinal);
            r.Rerolls=setup.Rerolls;r.Skips=setup.Skips;r.Banishes=setup.Banishes;
        }
        // MetaRun de Game.finishRun con los contadores reales de la partida (sin duplicarlos).
        public static MetaRun Summary(Mamporro.Core.WorldRun s,string runId)
        {
            var r=s.Combat;
            return new MetaRun {id=runId,cheated=s.Cheated,time=r.Time,kills=r.Kills,chests=s.Interactables.ChestsOpened,
                shrines=s.ShrinesCompleted,challenges=s.ChallengesCompleted,level=r.Level,victory=r.Victory,usedLifeTome=r.UsedLifeTome};
        }
        public Settlement LastSettlement=>settled;
        // Al empezar otra partida se descarta una liquidación que no pudo guardarse:
        // se conserva la última copia válida y nunca se aplica dos veces.
        public void ForgetSettlement(){settled=null;}
    }
}
