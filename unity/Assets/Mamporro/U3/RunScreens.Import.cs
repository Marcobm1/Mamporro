using System.Text;
using Mamporro.Core.Progress;
using Mamporro.Persistence;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Paso 5 de U4: pantalla técnica de importación del progreso web desde la pantalla de inicio.
    // Elegir archivo → revisar (informe y vista previa) → confirmar o cancelar. Toda la lógica es
    // de ProgressImporter/ProgressStore; aquí solo se muestra. El guardado no se toca hasta abrirla.
    public sealed partial class RunScreens
    {
        GameObject import;InputField importPath;Text importMessage,importCurrent,importIncoming,importIssues,importWarning;Button importConfirm,importCancel;
        ProgressImporter importer;ImportReview review;
        public bool ImportVisible=>import&&import.activeSelf;
        public bool ImportCanConfirm=>review!=null&&review.CanConfirm;
        public ImportReview ImportReview=>review;
        public string ImportMessage=>importMessage.text;
        public string ImportText=>importCurrent.text+"\n"+importIncoming.text+"\n"+importIssues.text+"\n"+importWarning.text;
        public string ImportPath {get=>importPath.text;set=>importPath.text=value;}

        void BuildImport(Transform parent)
        {
            import=Panel(parent,"Importar");var c=Content(import);
            Label(c,CombatText.Get("import.title"),52,TextAnchor.MiddleCenter,new Vector2(.05f,.88f),new Vector2(.95f,.97f));
            Label(c,CombatText.Get("import.help"),24,TextAnchor.MiddleCenter,new Vector2(.05f,.81f),new Vector2(.95f,.88f)).color=Hex("#a49cc0");
            Label(c,CombatText.Get("import.path"),26,TextAnchor.MiddleLeft,new Vector2(.05f,.73f),new Vector2(.13f,.79f));
            importPath=Input(c,new Vector2(.14f,.73f),new Vector2(.62f,.79f));importPath.characterLimit=400;importPath.textComponent.fontSize=22;
            Button(c,CombatText.Get("import.browse"),new Vector2(.63f,.73f),new Vector2(.77f,.79f),Browse);
            Button(c,CombatText.Get("import.review"),new Vector2(.78f,.73f),new Vector2(.95f,.79f),ReviewImport);
            importMessage=Label(c,"",26,TextAnchor.MiddleLeft,new Vector2(.05f,.66f),new Vector2(.95f,.72f));
            Label(c,CombatText.Get("import.current"),26,TextAnchor.UpperLeft,new Vector2(.05f,.6f),new Vector2(.32f,.65f)).color=Hex("#a49cc0");
            importCurrent=Label(c,"",22,TextAnchor.UpperLeft,new Vector2(.05f,.34f),new Vector2(.32f,.6f));
            Label(c,CombatText.Get("import.incoming"),26,TextAnchor.UpperLeft,new Vector2(.35f,.6f),new Vector2(.62f,.65f)).color=Hex("#a49cc0");
            importIncoming=Label(c,"",22,TextAnchor.UpperLeft,new Vector2(.35f,.34f),new Vector2(.62f,.6f));
            Label(c,CombatText.Get("import.issues"),26,TextAnchor.UpperLeft,new Vector2(.65f,.6f),new Vector2(.95f,.65f)).color=Hex("#a49cc0");
            importIssues=Label(c,"",19,TextAnchor.UpperLeft,new Vector2(.65f,.16f),new Vector2(.95f,.6f));
            importWarning=Label(c,"",24,TextAnchor.UpperLeft,new Vector2(.05f,.16f),new Vector2(.62f,.33f));importWarning.color=Hex("#ffd84a");
            importConfirm=Button(c,CombatText.Get("import.confirm"),new Vector2(.05f,.04f),new Vector2(.32f,.13f),ConfirmImport);
            importCancel=Button(c,CombatText.Get("import.cancel"),new Vector2(.35f,.04f),new Vector2(.62f,.13f),CancelImport);
            Button(c,CombatText.Get("import.back"),new Vector2(.65f,.04f),new Vector2(.95f,.13f),()=>ShowImport(false));
            import.SetActive(false);
        }

        public void ShowImport(bool visible)
        {
            if(visible){
                importer=game.CreateImporter();review=null;
                if(string.IsNullOrWhiteSpace(importPath.text))importPath.text=NativeFileDialog.SuggestedPath(ProgressImporter.DefaultFileName);
                var current=importer.LoadCurrent();
                importCurrent.text=current.Status=="unavailable"?Error("storage.unavailable"):Summary(current.Status=="new"?null:current.Candidate);
                Message(CombatText.Get("import.notReviewed"),false);importIncoming.text=importIssues.text=importWarning.text="";Paint();
            } else {review=null;importer=null;}
            if(import.activeSelf!=visible)import.SetActive(visible);
        }

        void Browse()
        {
            string chosen=NativeFileDialog.OpenJson(CombatText.Get("import.title"),importPath.text);
            if(!string.IsNullOrEmpty(chosen)){importPath.text=chosen;ReviewImport();}
        }

        public void ReviewImport()
        {
            if(importer==null)return;
            review=importer.Review(importPath.text);
            var report=review.Report;var sb=new StringBuilder();int preserved=0;
            if(report!=null)foreach(var issue in report.Issues){
                if(issue.Severity=="info"){preserved++;continue;}
                sb.Append("• ").Append(issue.Path).Append(": ").Append(CombatText.Get("import.code."+issue.Code)).Append(" (").Append(CombatText.Get("import.action."+issue.Action)).Append(')');
                if(issue.Action!="migrate"&&issue.Action!="reject"&&issue.Before!=null&&issue.Before!="null")sb.Append(' ').Append(Short(issue.Before)).Append(issue.Action=="default"?" → "+Short(issue.After):"");
                sb.Append('\n');
            }
            importIssues.text=sb.Length>0?sb+CombatText.Format("import.preserved","n",preserved):report!=null&&report.CanConfirm?CombatText.Format("import.noIssues","n",preserved):"";
            importIncoming.text=review.Incoming!=null?Summary(review.Incoming):"";
            if(review.Baseline!=null)importCurrent.text=Summary(review.Current);
            if(review.Error!=null){Message(Error(review.Error),true);importWarning.text="";}
            else{
                Message("",false);
                importWarning.text=review.Unchanged?CombatText.Get("import.unchanged"):CombatText.Get("import.replace")+
                    (review.Baseline.Status=="review"||review.Baseline.Status=="backup"||review.Baseline.Status=="invalid"?"\n"+CombatText.Get("import.storageRecovery"):"");
            }
            Paint();
        }

        public void ConfirmImport()
        {
            if(importer==null||review==null||!review.CanConfirm)return;
            if(game.State!=U3Game.Screen.Title)return;
            bool same=review.Unchanged;var result=importer.Confirm(review,true);review=null;game.AdoptImported(result);OpenPage(current);
            if(result.Success){Message(CombatText.Get(same?"import.doneSame":"import.done"),false);importCurrent.text=Summary(result.Snapshot.Candidate);importWarning.text="";}
            else Message(Error(result.Error),true);
            Paint();
        }

        public void CancelImport()
        {
            if(importer==null||review==null)return;
            importer.Confirm(review,false);review=null;Message(CombatText.Get("import.cancelled"),false);
            importIncoming.text=importIssues.text=importWarning.text="";Paint();
        }

        void Paint(){importConfirm.interactable=ImportCanConfirm;importCancel.interactable=review!=null;}
        void Message(string text,bool error){importMessage.text=text;importMessage.color=error?Hex("#ff6a5a"):Hex("#9fe08a");}
        static string Short(string value)=>value.Length>40?value.Substring(0,40)+"…":value;
        static string Error(string code)
        {
            if(code==null)return "";
            string key=code.StartsWith("storage.write-failed")?"storage.write-failed":code;
            string text=CombatText.Get("import.error."+key);
            if(code.Contains("storage.recovery-required"))text+="\n"+CombatText.Get("import.error.recovery");
            return text;
        }
        static string Summary(ProgressDto p)
        {
            if(p==null)return CombatText.Get("import.none");
            var m=p.meta;int extras=m.extras.rerolls+m.extras.skips+m.extras.banishes;
            return CombatText.Format("import.summary","coins",m.coins,"characters",m.characters.Length,"selected",CombatText.Get("character."+m.selected),
                "weapons",m.weapons.Length,"items",m.items.Length,"missions",m.completed.Length,"extras",extras,"language",p.settings.language.ToUpperInvariant(),"minutes",p.settings.runMinutes);
        }
    }
}
