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
    public sealed class RunScreens : MonoBehaviour
    {
        U3Game game;Font font;
        GameObject pause;Text pauseSeed,pauseStats,pauseItems,pauseControls;
        GameObject title,results;Text titleMap,titlePassive,resultsTitle,resultsSubtitle,resultsStats,resultsDamage,resultsItems,resultsSeed;
        InputField seedInput;readonly Image[] characterButtons=new Image[2],durationButtons=new Image[3];
        public bool PauseVisible=>pause&&pause.activeSelf;
        public bool TitleVisible=>title&&title.activeSelf;
        public bool ResultsVisible=>results&&results.activeSelf;
        public string ResultsText=>resultsTitle.text+"\n"+resultsSubtitle.text+"\n"+resultsStats.text+"\n"+resultsDamage.text+"\n"+resultsItems.text+"\n"+resultsSeed.text;
        public string TitleText=>titleMap.text+"\n"+titlePassive.text;
        public string SeedInput {get=>seedInput.text;set=>seedInput.text=value;}
        public string PauseText=>pauseSeed.text+"\n"+pauseStats.text+"\n"+pauseItems.text;

        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}

        public void Build(U3Game owner)
        {
            game=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject=new GameObject("Pantallas U3",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
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
            Button(Content(pause),CombatText.Get("pause.backToTitle"),new Vector2(.68f,.76f),new Vector2(.95f,.84f),()=>game.BackToTitle());
            pause.SetActive(false);
            BuildTitle(canvasObject.transform);BuildResults(canvasObject.transform);
        }

        // ------------------------------------------------------------ inicio
        void BuildTitle(Transform parent)
        {
            title=Panel(parent,"Inicio");
            Label(Content(title),"MAMPORRO · U3",72,TextAnchor.MiddleCenter,new Vector2(.05f,.86f),new Vector2(.95f,.97f));
            Label(Content(title),"Pantalla técnica de inicio",26,TextAnchor.MiddleCenter,new Vector2(.05f,.81f),new Vector2(.95f,.86f)).color=Hex("#a49cc0");
            for(int i=0;i<2;i++){
                int slot=i;var c=Catalog.Characters[i];
                characterButtons[i]=Button(Content(title),CombatText.Get("character."+c.id),new Vector2(.05f+.31f*i,.66f),new Vector2(.34f+.31f*i,.77f),()=>{game.Character=Catalog.Characters[slot];PaintTitle();}).GetComponent<Image>();
            }
            titlePassive=Label(Content(title),"",24,TextAnchor.UpperLeft,new Vector2(.05f,.56f),new Vector2(.65f,.65f));
            Label(Content(title),CombatText.Get("title.duration"),28,TextAnchor.MiddleLeft,new Vector2(.05f,.47f),new Vector2(.2f,.54f));
            for(int i=0;i<3;i++){
                int minutes=Catalog.RunDurations[i];
                durationButtons[i]=Button(Content(title),CombatText.Format("title.minutes","n",minutes),new Vector2(.21f+.15f*i,.47f),new Vector2(.34f+.15f*i,.54f),()=>{game.Minutes=minutes;PaintTitle();}).GetComponent<Image>();
            }
            Label(Content(title),CombatText.Get("title.seedLabel"),28,TextAnchor.MiddleLeft,new Vector2(.05f,.37f),new Vector2(.2f,.44f));
            seedInput=Input(Content(title),new Vector2(.21f,.37f),new Vector2(.49f,.44f));
            Button(Content(title),CombatText.Get("title.newMap"),new Vector2(.51f,.37f),new Vector2(.68f,.44f),()=>{game.NewMap();PaintTitle();});
            titleMap=Label(Content(title),"",26,TextAnchor.MiddleLeft,new Vector2(.05f,.29f),new Vector2(.65f,.35f));
            Button(Content(title),CombatText.Get("title.play"),new Vector2(.05f,.12f),new Vector2(.34f,.24f),()=>game.StartRun(seedInput.text));
            Label(Content(title),CombatText.Get("title.clickHint"),22,TextAnchor.MiddleLeft,new Vector2(.36f,.12f),new Vector2(.68f,.24f)).color=Hex("#a49cc0");
            Label(Content(title),CombatText.Get("controls.title"),26,TextAnchor.UpperLeft,new Vector2(.7f,.66f),new Vector2(.95f,.72f)).color=Hex("#a49cc0");
            Label(Content(title),Controls(),24,TextAnchor.UpperLeft,new Vector2(.7f,.06f),new Vector2(.95f,.66f));
            title.SetActive(false);
        }
        public void ShowTitle(bool visible){if(visible)PaintTitle();if(title.activeSelf!=visible)title.SetActive(visible);}
        void PaintTitle()
        {
            for(int i=0;i<2;i++)characterButtons[i].color=game.Character==Catalog.Characters[i]?Hex("#8a6cd8"):Hex("#463874");
            for(int i=0;i<3;i++)durationButtons[i].color=game.Minutes==Catalog.RunDurations[i]?Hex("#8a6cd8"):Hex("#463874");
            titlePassive.text=CombatText.Get("character."+game.Character.id+".passive");
            titleMap.text=CombatText.Format("title.currentSeed","seed",game.Seed);
            ((Text)seedInput.placeholder).text=CombatText.Get("title.seedPlaceholder");
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
            resultsItems=Label(Content(results),"",26,TextAnchor.UpperLeft,new Vector2(.45f,.24f),new Vector2(.95f,.7f));
            resultsSeed=Label(Content(results),"",24,TextAnchor.MiddleLeft,new Vector2(.45f,.16f),new Vector2(.95f,.24f));resultsSeed.color=Hex("#a49cc0");
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
            pause.SetActive(visible);
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
