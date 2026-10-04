using System;
using System.Text;
using Mamporro.Core;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Pantallas técnicas de U3 en uGUI: inicio (personaje, duración y semilla), pausa
    // (semilla, estadísticas y objetos, como UI.buildPause de la web, sin las opciones
    // guardadas) y resultados (UI.buildResults sin la Calderilla ni las misiones, que son U4).
    public sealed partial class RunScreens : MonoBehaviour
    {
        U3Game game;Font font;
        GameObject pause;Text pauseSeed,pauseStats,pauseItems,pauseControls;
        GameObject results;Text resultsTitle,resultsSubtitle,resultsStats,resultsDamage,resultsItems,resultsSeed,resultsMeta;
        Button resultsRetrySave;GameObject canvasObject;
        public bool PauseVisible=>pause&&pause.activeSelf;
        public bool ResultsVisible=>results&&results.activeSelf;
        public string ResultsText=>resultsTitle.text+"\n"+resultsSubtitle.text+"\n"+resultsStats.text+"\n"+resultsDamage.text+"\n"+resultsItems.text+"\n"+resultsSeed.text;
        public string ResultsMetaText=>resultsMeta.text;
        public bool RetrySaveVisible=>resultsRetrySave.gameObject.activeSelf;
        public bool CharacterLocked(int index)=>Array.IndexOf(game.Progress.Progress.meta.characters,Catalog.Characters[index].id)<0;
        public string PauseText=>pauseSeed.text+"\n"+pauseStats.text+"\n"+pauseItems.text;

        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}

        public void Build(U3Game owner)
        {
            game=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasObject=new GameObject("Pantallas U3",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            pause=Panel(canvasObject.transform,"Pausa");
            Label(Content(pause),CombatText.Get("pause.title"),60,TextAnchor.MiddleCenter,new Vector2(.05f,.86f),new Vector2(.95f,.96f));
            Button(Content(pause),CombatText.Get("pause.resume"),new Vector2(.05f,.76f),new Vector2(.3f,.84f),()=>game.SetPaused(false));
            pauseSeed=Label(Content(pause),"",28,TextAnchor.MiddleLeft,new Vector2(.33f,.76f),new Vector2(.66f,.84f));
            Label(Content(pause),CombatText.Get("pause.stats"),26,TextAnchor.UpperLeft,new Vector2(.05f,.68f),new Vector2(.36f,.73f)).color=Hex("#a49cc0");
            pauseStats=Label(Content(pause),"",26,TextAnchor.UpperLeft,new Vector2(.05f,.06f),new Vector2(.36f,.68f));
            Label(Content(pause),CombatText.Get("pause.items"),26,TextAnchor.UpperLeft,new Vector2(.38f,.68f),new Vector2(.66f,.73f)).color=Hex("#a49cc0");
            pauseItems=Label(Content(pause),"",24,TextAnchor.UpperLeft,new Vector2(.38f,.06f),new Vector2(.66f,.68f));
            Label(Content(pause),CombatText.Get("controls.title"),26,TextAnchor.UpperLeft,new Vector2(.68f,.68f),new Vector2(.95f,.73f)).color=Hex("#a49cc0");
            pauseControls=Label(Content(pause),Controls(),24,TextAnchor.UpperLeft,new Vector2(.68f,.06f),new Vector2(.95f,.68f));
            // Volver al inicio pide confirmación: abandonar no da Calderilla ni misiones (web).
            Button(Content(pause),CombatText.Get("pause.backToTitle"),new Vector2(.68f,.76f),new Vector2(.95f,.84f),()=>ShowAbandon(true));
            abandon=new GameObject("Abandonar",typeof(RectTransform),typeof(Image));abandon.transform.SetParent(Content(pause),false);
            var ar=(RectTransform)abandon.transform;ar.anchorMin=new Vector2(.2f,.3f);ar.anchorMax=new Vector2(.8f,.7f);ar.offsetMin=ar.offsetMax=Vector2.zero;abandon.GetComponent<Image>().color=Hex("#2a2048");
            Label(abandon.transform,CombatText.Get("meta.abandon"),30,TextAnchor.MiddleCenter,new Vector2(.05f,.45f),new Vector2(.95f,.95f));
            Button(abandon.transform,CombatText.Get("meta.cancel"),new Vector2(.05f,.08f),new Vector2(.47f,.35f),()=>ShowAbandon(false));
            Button(abandon.transform,CombatText.Get("meta.confirm"),new Vector2(.53f,.08f),new Vector2(.95f,.35f),()=>{ShowAbandon(false);game.BackToTitle();});
            abandon.SetActive(false);
            pause.SetActive(false);
            BuildMenu(canvasObject.transform);BuildResults(canvasObject.transform);BuildImport(canvasObject.transform);
        }

        // ------------------------------------------------------------ resultados
        void BuildResults(Transform parent)
        {
            results=Panel(parent,"Resultados");
            resultsTitle=Label(Content(results),"",64,TextAnchor.MiddleCenter,new Vector2(.05f,.85f),new Vector2(.95f,.96f));
            resultsSubtitle=Label(Content(results),"",28,TextAnchor.MiddleCenter,new Vector2(.05f,.79f),new Vector2(.95f,.85f));resultsSubtitle.color=Hex("#a49cc0");
            resultsStats=Label(Content(results),"",30,TextAnchor.UpperLeft,new Vector2(.05f,.45f),new Vector2(.4f,.76f));
            Label(Content(results),CombatText.Get("gameover.damage"),26,TextAnchor.UpperLeft,new Vector2(.05f,.39f),new Vector2(.4f,.45f)).color=Hex("#a49cc0");
            resultsDamage=Label(Content(results),"",28,TextAnchor.UpperLeft,new Vector2(.05f,.18f),new Vector2(.4f,.39f));
            Label(Content(results),CombatText.Get("results.items"),26,TextAnchor.UpperLeft,new Vector2(.45f,.7f),new Vector2(.95f,.76f)).color=Hex("#a49cc0");
            resultsItems=Label(Content(results),"",26,TextAnchor.UpperLeft,new Vector2(.45f,.52f),new Vector2(.95f,.7f));
            // U4: Calderilla del Caos ganada, desglose, misiones nuevas y estado del guardado.
            resultsMeta=Label(Content(results),"",24,TextAnchor.UpperLeft,new Vector2(.45f,.24f),new Vector2(.95f,.52f));
            resultsRetrySave=Button(Content(results),CombatText.Get("progress.retrySave"),new Vector2(.45f,.16f),new Vector2(.68f,.23f),()=>game.RetrySave());
            resultsSeed=Label(Content(results),"",22,TextAnchor.MiddleLeft,new Vector2(.7f,.16f),new Vector2(.95f,.24f));resultsSeed.color=Hex("#a49cc0");
            Button(Content(results),CombatText.Get("gameover.retry"),new Vector2(.05f,.04f),new Vector2(.3f,.13f),()=>game.Retry(false));
            Button(Content(results),CombatText.Get("gameover.newMap"),new Vector2(.35f,.04f),new Vector2(.6f,.13f),()=>game.Retry(true));
            Button(Content(results),CombatText.Get("gameover.backToTitle"),new Vector2(.65f,.04f),new Vector2(.95f,.13f),()=>game.BackToTitle());
            results.SetActive(false);
        }
        public void ShowResults(bool visible)
        {
            if(visible){
                var s=game.Session;var r=s.Combat;bool victory=r.Victory;
                resultsTitle.text=CombatText.Get(victory?"results.victory":"gameover.title");resultsTitle.color=victory?Hex("#ffd84a"):Hex("#ff6a5a");
                resultsSubtitle.text=CombatText.Get(victory?"results.victorySubtitle":"gameover.subtitle");
                resultsStats.text=$"{CombatText.Get("gameover.time")}: <b>{RunHud.FormatTime(r.Time)}</b>\n{CombatText.Get("gameover.kills")}: <b>{r.Kills}</b>\n{CombatText.Get("gameover.level")}: <b>{r.Level}</b>\n"+
                    $"{CombatText.Get("results.gold")}: <b>{Math.Floor(r.GoldCollected)}</b>\n{CombatText.Get("results.chests")}: <b>{s.Interactables.ChestsOpened}</b>";
                var sb=new StringBuilder();foreach(var w in r.Weapons)sb.Append(CombatText.Get("weapon."+w.Def.id)).Append(": <b>").Append(Rules.Round(w.TotalDamage)).Append("</b>\n");resultsDamage.text=sb.ToString();
                sb.Clear();foreach(var it in r.Items)sb.Append(it.Count>1?CombatText.Format("hud.itemCount","name",CombatText.Get("item."+it.Def.id),"n",it.Count):CombatText.Get("item."+it.Def.id)).Append('\n');
                resultsItems.text=r.Items.Count>0?sb.ToString():CombatText.Get("results.noItems");
                resultsSeed.text=CombatText.Format("gameover.seed","seed",game.Seed)+(s.Cheated?"\n"+CombatText.Get("gameover.cheated"):"");
                PaintSettlement(game.LastSettlement,s.Cheated);
            }
            if(results.activeSelf!=visible)results.SetActive(visible);
        }

        // Leyenda de controles (controlsLegend de la web) más las teclas técnicas de U3.
        static string Controls()
        {
            string Row(string action,string keys)=>$"<b>{CombatText.Get(keys)}</b>  {CombatText.Get(action)}";
            return string.Join("\n",Row("controls.move","controls.keys.move"),Row("controls.look","controls.keys.look"),Row("controls.jump","controls.keys.jump"),
                Row("controls.slide","controls.keys.slide"),Row("controls.interact","controls.keys.interact"),Row("controls.pause","controls.keys.pause"),Row("controls.debug","controls.keys.debug"),
                "\n<b>F1</b>  resolución interna\n<b>F2</b>  dither\n<b>F9</b>  ajuste de vértices\n<b>F6</b>  ventana 1080p/1440p");
        }

        public void ShowPause(bool visible)
        {
            if(visible){
                var r=game.Run;pauseSeed.text=CombatText.Format("pause.seed","seed",game.Seed);
                var sb=new StringBuilder();foreach(var line in StatLines(r))sb.Append(line.Label).Append(": <b>").Append(line.Value).Append("</b>\n");pauseStats.text=sb.ToString();
                sb.Clear();foreach(var s in r.Items)sb.Append(s.Count>1?CombatText.Format("hud.itemCount","name",CombatText.Get("item."+s.Def.id),"n",s.Count):CombatText.Get("item."+s.Def.id)).Append('\n');
                pauseItems.text=r.Items.Count>0?sb.ToString():CombatText.Get("pause.noItems");
            }
            if(!visible)ShowAbandon(false);
            pause.SetActive(visible);
        }
        GameObject abandon;
        public bool AbandonVisible=>abandon&&abandon.activeSelf;
        public void ShowAbandon(bool visible){if(abandon)abandon.SetActive(visible);}
        // Cambio de idioma: las pantallas se reconstruyen con los textos nuevos (como UI.refresh web).
        public void Rebuild()
        {
            bool resultsShown=ResultsVisible,menuShown=TitleVisible;string page=Page;
            if(canvasObject)Destroy(canvasObject);
            Build(game);if(menuShown){menu.SetActive(true);OpenPage(page);}if(resultsShown)ShowResults(true);
        }

        public struct StatLine { public string Label,Value; }
        // statLines de la web: estadísticas del personaje tal y como se ven en la pausa.
        public static StatLine[] StatLines(CombatRun r)
        {
            var s=r.Stats;
            string Percent(double v)=>CombatText.Format("format.percent","value",(v< -1e-9?"-":"+")+CombatText.Number(Math.Abs(v)*100,1));
            StatLine L(string key,string value)=>new StatLine{Label=CombatText.Capitalize(CombatText.Get(key)),Value=value};
            return new[]{
                L("stat.health",$"{Math.Ceiling(r.Hp)} / {Rules.Round(s[Stat.maxHp])}"),L("stat.regen",CombatText.Number(s[Stat.regen],1)),L("stat.armor",CombatText.Number(s[Stat.armor],0)),
                L("stat.damage",Percent(s[Stat.damage]-1)),L("stat.cooldown",Percent(s[Stat.attackSpeed]-1)),L("stat.extraProjectiles","+"+CombatText.Number(s[Stat.extraProjectiles],0)),
                L("stat.area",Percent(s[Stat.area]-1)),L("stat.critChance",Percent(s[Stat.critChance])),L("stat.critMultiplier",Percent(s[Stat.critDamage])),
                L("stat.moveSpeed",Percent(s[Stat.moveSpeed]-1)),L("stat.luck",CombatText.Number(s[Stat.luck],0)),L("stat.pickupRadius",CombatText.Number(s[Stat.pickupRadius],1)+" m"),
                L("stat.xpGain",Percent(s[Stat.xpGain]-1)),L("stat.goldGain",Percent(s[Stat.goldGain]-1))};
        }

        GameObject Panel(Transform parent,string name)
        {
            var back=new GameObject(name,typeof(RectTransform),typeof(Image));back.transform.SetParent(parent,false);
            var r=(RectTransform)back.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;back.GetComponent<Image>().color=new Color(.06f,.05f,.1f,.82f);
            var panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(back.transform,false);
            var p=(RectTransform)panel.transform;p.anchorMin=new Vector2(.08f,.08f);p.anchorMax=new Vector2(.92f,.92f);p.offsetMin=p.offsetMax=Vector2.zero;panel.GetComponent<Image>().color=Hex("#1d1730");
            return back;
        }
        // Panel centrado dentro del fondo a pantalla completa (lo que se activa es el fondo).
        static Transform Content(GameObject screen)=>screen.transform.GetChild(0);
        Text Label(Transform parent,string text,int size,TextAnchor anchor,Vector2 min,Vector2 max)
        {
            var o=new GameObject("Texto",typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var t=o.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=anchor;t.color=Hex("#f4efe0");t.text=text;t.supportRichText=true;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        // Recibo meta (resultados de Game.ts): Calderilla, desglose, misiones nuevas y guardado.
        void PaintSettlement(Mamporro.Persistence.Settlement settlement,bool cheated)
        {
            var sb=new StringBuilder();bool retry=false;
            if(cheated)sb.Append(CombatText.Get("meta.cheated"));
            else if(settlement!=null){
                var receipt=settlement.Receipt;
                sb.Append("<b>").Append(CombatText.Format("meta.results","n",receipt.total)).Append("</b>\n");
                sb.Append(CombatText.Format("meta.breakdown","kills",receipt.kills,"survival",receipt.survival,"victory",receipt.victory,"missions",receipt.missions));
                foreach(var id in receipt.completed)sb.Append('\n').Append(CombatText.Format("meta.newMission","name",CombatText.Get("mission."+id)));
                if(!settlement.NothingToSave){
                    if(settlement.Saved)sb.Append('\n').Append(CombatText.Get("progress.saved"));
                    else{
                        bool unavailable=settlement.Error=="storage.unavailable"||settlement.Error=="storage.recovery-pending";
                        sb.Append("\n<color=#ff6a5a>").Append(CombatText.Get(unavailable?"progress.notSavedState":"progress.notSaved")).Append("</color>");retry=!unavailable;
                    }
                }
            }
            resultsMeta.text=sb.ToString();resultsRetrySave.gameObject.SetActive(retry);
        }
        InputField Input(Transform parent,Vector2 min,Vector2 max)
        {
            var o=new GameObject("Semilla",typeof(RectTransform),typeof(Image),typeof(InputField));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;o.GetComponent<Image>().color=Hex("#0e0b18");
            var text=Label(o.transform,"",30,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one);((RectTransform)text.transform).offsetMin=new Vector2(14,0);text.supportRichText=false;
            var hint=Label(o.transform,"",30,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one);((RectTransform)hint.transform).offsetMin=new Vector2(14,0);hint.color=Hex("#6a6088");hint.fontStyle=FontStyle.Italic;
            var field=o.GetComponent<InputField>();field.textComponent=text;field.placeholder=hint;field.characterLimit=24;field.lineType=InputField.LineType.SingleLine;
            return field;
        }
        Button Button(Transform parent,string text,Vector2 min,Vector2 max,Action click)
        {
            var o=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;o.GetComponent<Image>().color=Hex("#463874");
            var b=o.GetComponent<Button>();b.onClick.AddListener(()=>click());Label(o.transform,text,32,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);return b;
        }
    }
}
