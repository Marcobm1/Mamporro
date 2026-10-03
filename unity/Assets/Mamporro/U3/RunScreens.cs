using System;
using System.Text;
using Mamporro.Core;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Pantallas técnicas de U3 en uGUI: pausa (semilla, estadísticas y objetos, como
    // UI.buildPause de la web, sin las opciones guardadas, que son de U4).
    public sealed class RunScreens : MonoBehaviour
    {
        U3Game game;Font font;
        GameObject pause;Text pauseSeed,pauseStats,pauseItems,pauseControls;
        public bool PauseVisible=>pause&&pause.activeSelf;
        public string PauseText=>pauseSeed.text+"\n"+pauseStats.text+"\n"+pauseItems.text;

        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}

        public void Build(U3Game owner)
        {
            game=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject=new GameObject("Pantallas U3",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            pause=Panel(canvasObject.transform,"Pausa");
            Label(pause.transform,CombatText.Get("pause.title"),60,TextAnchor.MiddleCenter,new Vector2(.05f,.86f),new Vector2(.95f,.96f));
            Button(pause.transform,CombatText.Get("pause.resume"),new Vector2(.05f,.76f),new Vector2(.3f,.84f),()=>game.SetPaused(false));
            pauseSeed=Label(pause.transform,"",28,TextAnchor.MiddleLeft,new Vector2(.33f,.76f),new Vector2(.95f,.84f));
            Label(pause.transform,CombatText.Get("pause.stats"),26,TextAnchor.UpperLeft,new Vector2(.05f,.68f),new Vector2(.36f,.73f)).color=Hex("#a49cc0");
            pauseStats=Label(pause.transform,"",26,TextAnchor.UpperLeft,new Vector2(.05f,.06f),new Vector2(.36f,.68f));
            Label(pause.transform,CombatText.Get("pause.items"),26,TextAnchor.UpperLeft,new Vector2(.38f,.68f),new Vector2(.66f,.73f)).color=Hex("#a49cc0");
            pauseItems=Label(pause.transform,"",24,TextAnchor.UpperLeft,new Vector2(.38f,.06f),new Vector2(.66f,.68f));
            Label(pause.transform,CombatText.Get("controls.title"),26,TextAnchor.UpperLeft,new Vector2(.68f,.68f),new Vector2(.95f,.73f)).color=Hex("#a49cc0");
            pauseControls=Label(pause.transform,Controls(),24,TextAnchor.UpperLeft,new Vector2(.68f,.06f),new Vector2(.95f,.68f));
            pause.SetActive(false);
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
            return panel;
        }
        Text Label(Transform parent,string text,int size,TextAnchor anchor,Vector2 min,Vector2 max)
        {
            var o=new GameObject("Texto",typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var t=o.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=anchor;t.color=Hex("#f4efe0");t.text=text;t.supportRichText=true;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        Button Button(Transform parent,string text,Vector2 min,Vector2 max,Action click)
        {
            var o=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;o.GetComponent<Image>().color=Hex("#463874");
            var b=o.GetComponent<Button>();b.onClick.AddListener(()=>click());Label(o.transform,text,32,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);return b;
        }
    }
}
