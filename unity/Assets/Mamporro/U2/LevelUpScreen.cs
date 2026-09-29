using System;
using Mamporro.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Mamporro.U2
{
    // Pantalla técnica de subida de nivel en uGUI; equivale a src/ui/LevelUpScreen.ts.
    // 1–4 o teclado numérico eligen (o descartan en modo descarte), R vuelve a tirar,
    // X salta, B activa el descarte y Esc lo cancela. Espera Tuning.InputGuard tras
    // abrir o tras elegir/saltar; volver a tirar y descartar no la reinician.
    public sealed class LevelUpScreen : MonoBehaviour
    {
        public CombatSession Session;
        public bool Visible=>root&&root.activeSelf;
        public bool BanishMode {get;private set;}
        public double AcceptFrom {get;private set;}
        public int CardCount {get;private set;}
        public string CardLabel(int index)=>cardTexts[index].text;
        public string Title=>title.text;
        GameObject root;Text title,subtitle,pending;
        readonly Image[] cards=new Image[4];readonly Text[] cardTexts=new Text[4];
        readonly Button[] actions=new Button[3];readonly Text[] actionTexts=new Text[3];
        Font font;

        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
        static Color Tone(string tone,bool dark)
        {
            switch(tone){
                case "common":return Hex(dark?"#3b3450":"#c8c0dc");
                case "uncommon":return Hex(dark?"#24401a":"#8cc84b");
                case "rare":return Hex(dark?"#16304e":"#4aa8ff");
                case "epic":return Hex(dark?"#3c1c5a":"#c070ff");
                case "legendary":return Hex(dark?"#5a3a08":"#ffb020");
                case "heal":return Hex(dark?"#1c4440":"#7fd8c8");
                case "gold":return Hex(dark?"#4a3808":"#f6c63a");
                case "banish":return Hex(dark?"#501818":"#e0524a");
                case "disabled":return Hex(dark?"#1a1528":"#5a5470");
                default:return Hex(dark?"#4a4458":"#f4efe0");
            }
        }

        public void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!FindAnyObjectByType<EventSystem>()){var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.transform.SetParent(transform,false);}
            var canvasObject=new GameObject("Subida de nivel U2",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root=Panel(canvasObject.transform,"Fondo",new Color(.08f,.06f,.14f,.96f),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            title=Label(root.transform,"Título",56,TextAnchor.MiddleCenter,new Vector2(0,.84f),new Vector2(1,.95f));
            subtitle=Label(root.transform,"Subtítulo",30,TextAnchor.MiddleCenter,new Vector2(0,.78f),new Vector2(1,.84f));
            pending=Label(root.transform,"Pendientes",26,TextAnchor.MiddleCenter,new Vector2(0,.74f),new Vector2(1,.78f));
            for(int i=0;i<4;i++){
                int slot=i;var card=Panel(root.transform,"Carta "+(i+1),Color.white,new Vector2(.05f+.225f*i,.22f),new Vector2(.05f+.225f*i+.205f,.72f),Vector2.zero,Vector2.zero);
                cards[i]=card.GetComponent<Image>();var button=card.AddComponent<Button>();button.targetGraphic=cards[i];button.onClick.AddListener(()=>Press(slot));
                cardTexts[i]=Label(card.transform,"Texto",26,TextAnchor.UpperLeft,new Vector2(.06f,.04f),new Vector2(.94f,.96f));
            }
            for(int i=0;i<3;i++){
                int action=i;var panel=Panel(root.transform,"Acción "+i,Hex("#463874"),new Vector2(.2f+.21f*i,.08f),new Vector2(.2f+.21f*i+.18f,.16f),Vector2.zero,Vector2.zero);
                actions[i]=panel.AddComponent<Button>();actions[i].targetGraphic=panel.GetComponent<Image>();actions[i].onClick.AddListener(()=>Action(action));
                actionTexts[i]=Label(panel.transform,"Texto",28,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one);
            }
            root.SetActive(false);
        }
        GameObject Panel(Transform parent,string name,Color color,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;o.GetComponent<Image>().color=color;return o;
        }
        Text Label(Transform parent,string name,int size,TextAnchor anchor,Vector2 min,Vector2 max)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var t=o.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=anchor;t.color=Hex("#f4efe0");t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }

        double Now=>UnityEngine.Time.unscaledTimeAsDouble;
        public bool Ready=>Now>=AcceptFrom;
        // fresh: subida nueva (abrir, o siguiente tras elegir/saltar); reinicia la espera.
        public void Show(bool fresh)
        {
            BanishMode=false;if(fresh)AcceptFrom=Now+Tuning.InputGuard;
            root.SetActive(true);Paint();
        }
        public void Hide(){BanishMode=false;if(root)root.SetActive(false);}
        public void Paint()
        {
            var r=Session.Run;if(r.Offer==null){Hide();return;}
            CardCount=r.Offer.Count;
            title.text=CombatText.Format("levelup.title","n",r.Level-r.PendingLevels+1);
            subtitle.text=BanishMode?CombatText.Get("levelup.banishHint"):CombatText.Get("levelup.subtitle");
            int left=r.PendingLevels-1;pending.text=left>0?CombatText.Format("levelup.pending","n",left):"";
            for(int i=0;i<4;i++){
                bool shown=i<CardCount;cards[i].gameObject.SetActive(shown);if(!shown)continue;
                var view=CardText.Describe(r.Offer[i],r);string tone=BanishMode?(view.Banishable?"banish":"disabled"):view.Tone;
                cards[i].color=Tone(tone,true);
                var text=new System.Text.StringBuilder();text.Append("<b>").Append(i+1).Append("</b>");
                if(view.Tag.Length>0)text.Append("   <color=#").Append(ColorUtility.ToHtmlStringRGB(Tone(tone,false))).Append('>').Append(view.Tag).Append("</color>");
                text.Append("\n\n<size=32><b>").Append(view.Title).Append("</b></size>");
                if(view.Level.Length>0)text.Append('\n').Append(view.Level);
                if(view.Lines.Length>0){text.Append('\n');foreach(var line in view.Lines)text.Append('\n').Append(line);}
                if(view.Description.Length>0)text.Append("\n\n<i>").Append(view.Description).Append("</i>");
                cardTexts[i].text=text.ToString();
            }
            ActionLabel(0,"R",CombatText.Format("levelup.reroll","n",r.Rerolls),r.Rerolls>0,false);
            ActionLabel(1,"X",CombatText.Format("levelup.skip","n",r.Skips),r.Skips>0,false);
            if(BanishMode)ActionLabel(2,"B",CombatText.Get("levelup.cancel"),true,true);
            else ActionLabel(2,"B",CombatText.Format("levelup.banish","n",r.Banishes),r.Banishes>0,false);
        }
        void ActionLabel(int i,string key,string label,bool enabled,bool active)
        {
            actions[i].interactable=enabled;actions[i].targetGraphic.color=active?Tone("banish",false):Hex("#463874");
            actionTexts[i].text="["+key+"]  "+label;actionTexts[i].color=enabled?Hex("#f4efe0"):Hex("#7a7090");
        }

        // Clic o tecla sobre una carta.
        public void Press(int index)
        {
            var r=Session.Run;if(!Visible||r.Offer==null||index<0||index>=r.Offer.Count||!Ready)return;
            if(BanishMode){if(r.Offer[index].Key!=null)Session.Banish(index);}
            else Session.Choose(index);
        }
        // 0 volver a tirar, 1 saltar, 2 descartar o cancelar el descarte.
        public void Action(int action)
        {
            var r=Session.Run;if(!Visible||r.Offer==null||!Ready)return;
            if(action==0&&r.Rerolls>0)Session.Reroll();
            else if(action==1&&r.Skips>0)Session.Skip();
            else if(action==2)SetBanishMode(!BanishMode);
        }
        public void SetBanishMode(bool on){if(on&&Session.Run.Banishes<=0)return;BanishMode=on;Paint();}

        void Update()
        {
            var k=Keyboard.current;if(!Visible||k==null||Session.Measuring)return;
            if(k.digit1Key.wasPressedThisFrame||k.numpad1Key.wasPressedThisFrame)Press(0);
            else if(k.digit2Key.wasPressedThisFrame||k.numpad2Key.wasPressedThisFrame)Press(1);
            else if(k.digit3Key.wasPressedThisFrame||k.numpad3Key.wasPressedThisFrame)Press(2);
            else if(k.digit4Key.wasPressedThisFrame||k.numpad4Key.wasPressedThisFrame)Press(3);
            else if(k.escapeKey.wasPressedThisFrame&&BanishMode)SetBanishMode(false);
            else if(k.rKey.wasPressedThisFrame)Action(0);
            else if(k.xKey.wasPressedThisFrame)Action(1);
            else if(k.bKey.wasPressedThisFrame)Action(2);
        }
    }
}
