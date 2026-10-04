using System;
using System.Text;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Paso 7 de U4: menú principal ES/EN equivalente a UI.buildTitle/buildSetup y MetaScreens de la
    // web: inicio, preparación (Jugar), personajes, tienda, misiones y opciones (idioma e
    // importación). Las reglas son de MetaRules/ProgressSession; aquí solo se presenta.
    public sealed partial class RunScreens
    {
        public const string Home="home",Setup="setup",Characters="characters",Shop="shop",Missions="missions",Options="options";
        GameObject menu;RectTransform page;Text balance,saveState;Button back,recover;readonly Button[] languages=new Button[2];
        string current=Home,seedDraft="";
        public string Page=>current;
        public bool TitleVisible=>menu&&menu.activeSelf;
        public bool RecoverVisible=>recover&&recover.gameObject.activeSelf;
        // Todo el texto visible del menú (pruebas): cabecera y página actual.
        public string TitleText{get{var sb=new StringBuilder();foreach(var t in menu.GetComponentsInChildren<Text>())sb.Append(t.text).Append('\n');return sb.ToString();}}
        public string SeedInput {get=>seedDraft;set=>seedDraft=value??"";}

        void BuildMenu(Transform parent)
        {
            menu=Panel(parent,"Menú");var c=Content(menu);
            back=Button(c,CombatText.Get("menu.back"),new Vector2(.02f,.9f),new Vector2(.14f,.97f),()=>OpenPage(Home));
            balance=Label(c,"",30,TextAnchor.MiddleLeft,new Vector2(.16f,.9f),new Vector2(.6f,.97f));balance.color=Hex("#f6c63a");
            for(int i=0;i<2;i++){string lang=i==0?"es":"en";languages[i]=Button(c,CombatText.Get("lang."+lang),new Vector2(.7f+.14f*i,.9f),new Vector2(.83f+.14f*i,.97f),()=>game.SetLanguage(lang));}
            saveState=Label(c,"",22,TextAnchor.UpperLeft,new Vector2(.02f,.82f),new Vector2(.72f,.89f));saveState.color=Hex("#ffd84a");
            recover=Button(c,CombatText.Get("progress.recover"),new Vector2(.74f,.83f),new Vector2(.97f,.89f),()=>{var r=game.RecoverProgress();OpenPage(current);if(r.Success)saveState.text=CombatText.Get("progress.recovered");});
            var area=new GameObject("Página",typeof(RectTransform));area.transform.SetParent(c,false);page=(RectTransform)area.transform;
            page.anchorMin=new Vector2(.02f,.03f);page.anchorMax=new Vector2(.98f,.81f);page.offsetMin=page.offsetMax=Vector2.zero;
            menu.SetActive(false);
        }
        // Al volver al menú desde otra pantalla se abre el inicio (UI.show('title') web).
        public void ShowTitle(bool visible)
        {
            if(visible&&!menu.activeSelf){menu.SetActive(true);OpenPage(Home);}
            else if(!visible&&menu.activeSelf)menu.SetActive(false);
        }
        public void OpenPage(string name)
        {
            current=name??Home;
            for(int i=page.childCount-1;i>=0;i--)Destroy(page.GetChild(i).gameObject);
            page.DetachChildren();
            var p=game.Progress;var meta=p.Progress.meta;
            back.gameObject.SetActive(current!=Home);
            balance.text=CombatText.Format("meta.balance","n",meta.coins);
            for(int i=0;i<2;i++)languages[i].targetGraphic.color=(i==0)!=CombatText.English?Hex("#8a6cd8"):Hex("#463874");
            saveState.text=p.Status=="loaded"?"":CombatText.Get("progress.state."+p.Status);recover.gameObject.SetActive(p.NeedsRecovery);
            switch(current){
                case Setup:PageSetup();break;case Characters:PageCharacters(meta);break;case Shop:PageShop(meta);break;
                case Missions:PageMissions(meta);break;case Options:PageOptions();break;default:current=Home;PageHome(meta);break;
            }
        }

        // ------------------------------------------------------------ páginas
        void PageHome(MetaDto meta)
        {
            Label(page,CombatText.Get("app.prototype"),22,TextAnchor.MiddleLeft,new Vector2(0,.88f),new Vector2(.6f,.96f)).color=Hex("#a49cc0");
            Label(page,CombatText.Get("app.title"),110,TextAnchor.MiddleLeft,new Vector2(0,.62f),new Vector2(.6f,.88f)).color=Hex("#f6c63a");
            Label(page,CombatText.Get("meta.hero"),40,TextAnchor.UpperLeft,new Vector2(0,.46f),new Vector2(.6f,.6f));
            Label(page,CombatText.Get("meta.heroText"),26,TextAnchor.UpperLeft,new Vector2(0,.3f),new Vector2(.6f,.46f)).color=Hex("#a49cc0");
            Label(page,CombatText.Format("meta.current","name",CombatText.Get("character."+game.Character.id)),28,TextAnchor.UpperLeft,new Vector2(0,.18f),new Vector2(.6f,.28f));
            string[] pages={Setup,Characters,Shop,Missions,Options};string[] keys={"title.play","menu.characters","menu.shop","menu.missions","options.title"};
            for(int i=0;i<pages.Length;i++){string target=pages[i];var b=Button(page,CombatText.Get(keys[i]),new Vector2(.66f,.82f-.17f*i),new Vector2(1,.96f-.17f*i),()=>OpenPage(target));
                if(i==0)b.targetGraphic.color=Hex("#8a6cd8");}
        }
        void PageSetup()
        {
            Label(page,CombatText.Get("title.play"),56,TextAnchor.MiddleLeft,new Vector2(0,.86f),new Vector2(.6f,1));
            Button(page,CombatText.Format("meta.current","name",CombatText.Get("character."+game.Character.id)),new Vector2(0,.74f),new Vector2(.6f,.84f),()=>OpenPage(Characters));
            Label(page,CombatText.Get("app.tagline"),26,TextAnchor.MiddleLeft,new Vector2(0,.66f),new Vector2(.6f,.73f)).color=Hex("#a49cc0");
            var play=Button(page,CombatText.Get("title.play"),new Vector2(0,.5f),new Vector2(.6f,.64f),()=>game.StartRun(seedDraft));play.targetGraphic.color=Hex("#8a6cd8");
            Label(page,CombatText.Get("title.seedLabel"),28,TextAnchor.MiddleLeft,new Vector2(0,.38f),new Vector2(.14f,.46f));
            var seed=Input(page,new Vector2(.15f,.38f),new Vector2(.42f,.46f));seed.characterLimit=12;seed.text=seedDraft;((Text)seed.placeholder).text=CombatText.Get("title.seedPlaceholder");
            seed.onValueChanged.AddListener(v=>seedDraft=v);
            Button(page,CombatText.Get("title.newMap"),new Vector2(.43f,.38f),new Vector2(.6f,.46f),()=>{seedDraft="";game.NewMap();OpenPage(Setup);});
            Label(page,CombatText.Format("title.currentSeed","seed",game.Seed),24,TextAnchor.MiddleLeft,new Vector2(0,.3f),new Vector2(.6f,.37f)).color=Hex("#a49cc0");
            Label(page,CombatText.Get("title.duration"),28,TextAnchor.MiddleLeft,new Vector2(0,.18f),new Vector2(.14f,.27f));
            for(int i=0;i<3;i++){int minutes=Catalog.RunDurations[i];
                var b=Button(page,CombatText.Format("title.minutes","n",minutes),new Vector2(.15f+.15f*i,.18f),new Vector2(.29f+.15f*i,.27f),()=>{game.SetMinutes(minutes);OpenPage(Setup);});
                b.targetGraphic.color=game.Minutes==minutes?Hex("#8a6cd8"):Hex("#463874");}
            Label(page,CombatText.Get("title.clickHint"),22,TextAnchor.MiddleLeft,new Vector2(0,.04f),new Vector2(.6f,.14f)).color=Hex("#a49cc0");
            Label(page,CombatText.Get("controls.title"),26,TextAnchor.UpperLeft,new Vector2(.66f,.9f),new Vector2(1,.97f)).color=Hex("#a49cc0");
            var controls=Label(page,Controls(),20,TextAnchor.UpperLeft,new Vector2(.66f,.1f),new Vector2(1,.9f));controls.verticalOverflow=VerticalWrapMode.Overflow;
        }
        void PageCharacters(MetaDto meta)
        {
            Label(page,CombatText.Get("menu.characters"),48,TextAnchor.MiddleLeft,new Vector2(0,.88f),new Vector2(1,1));
            for(int i=0;i<Catalog.Characters.Length;i++){
                var def=Catalog.Characters[i];bool unlocked=Array.IndexOf(meta.characters,def.id)>=0,chosen=meta.selected==def.id;
                var card=Card(new Vector2(.02f+.5f*i,.04f),new Vector2(.48f+.5f*i,.84f),chosen);
                Label(card,CombatText.Get("character."+def.id),34,TextAnchor.UpperLeft,new Vector2(.05f,.78f),new Vector2(.95f,.96f));
                Label(card,CombatText.Format("meta.weapon","name",CombatText.Get("weapon."+def.startingWeapon)),24,TextAnchor.UpperLeft,new Vector2(.05f,.68f),new Vector2(.95f,.77f)).color=Hex("#a49cc0");
                Label(card,CombatText.Get("character."+def.id+".passive"),24,TextAnchor.UpperLeft,new Vector2(.05f,.34f),new Vector2(.95f,.66f));
                if(!unlocked)Label(card,CombatText.Get("meta.sourceShop"),24,TextAnchor.UpperLeft,new Vector2(.05f,.24f),new Vector2(.95f,.32f)).color=Hex("#f6c63a");
                int index=i;var b=Button(card,CombatText.Get(chosen?"meta.selected":unlocked?"meta.select":"meta.locked"),new Vector2(.05f,.04f),new Vector2(.95f,.18f),()=>{game.Character=Catalog.Characters[index];OpenPage(Characters);});
                b.interactable=unlocked&&!chosen;
            }
        }
        void PageShop(MetaDto meta)
        {
            Label(page,CombatText.Get("menu.shop"),48,TextAnchor.MiddleLeft,new Vector2(0,.9f),new Vector2(.5f,1));
            Label(page,CombatText.Get("meta.shopHint"),20,TextAnchor.MiddleLeft,new Vector2(.38f,.9f),new Vector2(1,1)).color=Hex("#a49cc0");
            for(int i=0;i<MetaRules.Shop.Count;i++){
                var s=MetaRules.Shop[i];bool owned=MetaRules.Owns(meta,s.UnlockKind,s.UnlockId);
                var card=Card(new Vector2(.25f*i,.5f),new Vector2(.25f*i+.24f,.89f),owned);
                Label(card,UnlockName(s.UnlockKind,s.UnlockId),22,TextAnchor.UpperLeft,new Vector2(.05f,.76f),new Vector2(.95f,.97f));
                Label(card,UnlockDescription(s.UnlockKind,s.UnlockId),18,TextAnchor.UpperLeft,new Vector2(.05f,.36f),new Vector2(.95f,.75f)).color=Hex("#a49cc0");
                Label(card,CombatText.Format("meta.balance","n",s.Price),20,TextAnchor.MiddleLeft,new Vector2(.05f,.25f),new Vector2(.95f,.35f)).color=Hex("#f6c63a");
                string id=s.Id;PurchaseButton(card,meta.coins,s.Price,owned,()=>{if(game.Progress.Purchase(id))game.Audio?.Play("reward");OpenPage(Shop);});
            }
            Label(page,CombatText.Get("meta.extras"),30,TextAnchor.MiddleLeft,new Vector2(0,.42f),new Vector2(.4f,.49f));
            Label(page,CombatText.Get("meta.extrasHint"),20,TextAnchor.MiddleLeft,new Vector2(.4f,.42f),new Vector2(1,.49f)).color=Hex("#a49cc0");
            string[] actions={"rerolls","skips","banishes"};
            for(int i=0;i<3;i++){
                string action=actions[i];int level=meta.extras.Get(action);
                var card=Card(new Vector2(.25f*i,.1f),new Vector2(.25f*i+.24f,.41f),false);
                Label(card,CombatText.Get("meta."+action),26,TextAnchor.UpperLeft,new Vector2(.05f,.7f),new Vector2(.95f,.96f));
                Label(card,CombatText.Format("meta.uses","n",MetaRules.InitialUses(meta,action)),22,TextAnchor.UpperLeft,new Vector2(.05f,.45f),new Vector2(.95f,.7f));
                if(level>=MetaRules.ExtraPrices.Count){var max=Button(card,CombatText.Get("meta.max"),new Vector2(.05f,.06f),new Vector2(.95f,.36f),()=>{});max.interactable=false;}
                else PurchaseButton(card,meta.coins,MetaRules.ExtraPrices[level],false,()=>{if(game.Progress.PurchaseExtra(action))game.Audio?.Play("reward");OpenPage(Shop);},new Vector2(.05f,.06f),new Vector2(.95f,.36f));
            }
            // Desbloqueos que llegan por misión (no se compran).
            var sb=new StringBuilder();
            foreach(var m in MetaRules.Missions){if(m.UnlockId==null)continue;
                sb.Append(UnlockName(m.UnlockKind,m.UnlockId)).Append(" · ").Append(Array.IndexOf(meta.completed,m.Id)>=0?CombatText.Get("meta.owned"):CombatText.Format("meta.sourceMission","name",CombatText.Get("mission."+m.Id))).Append('\n');}
            Label(page,CombatText.Get("menu.missions"),24,TextAnchor.UpperLeft,new Vector2(.76f,.34f),new Vector2(1,.41f)).color=Hex("#a49cc0");
            Label(page,sb.ToString(),20,TextAnchor.UpperLeft,new Vector2(.76f,.0f),new Vector2(1,.34f));
        }
        void PageMissions(MetaDto meta)
        {
            Label(page,CombatText.Get("menu.missions"),48,TextAnchor.MiddleLeft,new Vector2(0,.9f),new Vector2(.4f,1));
            Label(page,CombatText.Get("meta.missionHint"),22,TextAnchor.MiddleLeft,new Vector2(.3f,.9f),new Vector2(1,1)).color=Hex("#a49cc0");
            for(int i=0;i<MetaRules.Missions.Count;i++){
                var m=MetaRules.Missions[i];bool done=Array.IndexOf(meta.completed,m.Id)>=0;int progress=meta.missions.Get(m.Id);
                float x=.25f*(i%4),y=i<4?.46f:.02f;var card=Card(new Vector2(x,y),new Vector2(x+.24f,y+.42f),done);
                Label(card,CombatText.Get("mission."+m.Id),22,TextAnchor.UpperLeft,new Vector2(.05f,.66f),new Vector2(.95f,.96f));
                var bar=new GameObject("Barra",typeof(RectTransform),typeof(Image));bar.transform.SetParent(card,false);var br=(RectTransform)bar.transform;
                br.anchorMin=new Vector2(.05f,.52f);br.anchorMax=new Vector2(.95f,.6f);br.offsetMin=br.offsetMax=Vector2.zero;bar.GetComponent<Image>().color=Hex("#0e0b18");bar.GetComponent<Image>().raycastTarget=false;
                var fill=new GameObject("Relleno",typeof(RectTransform),typeof(Image));fill.transform.SetParent(bar.transform,false);var fr=(RectTransform)fill.transform;
                fr.anchorMin=Vector2.zero;fr.anchorMax=new Vector2(Mathf.Clamp01((float)progress/m.Target),1);fr.offsetMin=fr.offsetMax=Vector2.zero;fill.GetComponent<Image>().color=done?Hex("#9fe08a"):Hex("#8a6cd8");fill.GetComponent<Image>().raycastTarget=false;
                Label(card,done?CombatText.Get("meta.done"):CombatText.Format("meta.progress","n",progress,"target",m.Target),20,TextAnchor.UpperLeft,new Vector2(.05f,.36f),new Vector2(.95f,.5f)).color=Hex("#a49cc0");
                Label(card,CombatText.Format("meta.reward","n",m.Coins),20,TextAnchor.UpperLeft,new Vector2(.05f,.2f),new Vector2(.95f,.34f)).color=Hex("#f6c63a");
                if(m.UnlockId!=null)Label(card,CombatText.Format("meta.unlock","name",UnlockName(m.UnlockKind,m.UnlockId)),20,TextAnchor.UpperLeft,new Vector2(.05f,.02f),new Vector2(.95f,.2f));
            }
        }
        // Las 14 opciones guardadas e importación del progreso web (RunScreens.Options).
        void PageOptions()
        {
            Label(page,CombatText.Get("options.title"),48,TextAnchor.MiddleLeft,new Vector2(0,.9f),new Vector2(1,1));
            var grid=new GameObject("Opciones",typeof(RectTransform));grid.transform.SetParent(page,false);var r=(RectTransform)grid.transform;
            r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,.89f);r.offsetMin=r.offsetMax=Vector2.zero;
            PaintOptions(r,false);
        }

        // ------------------------------------------------------------ piezas
        Transform Card(Vector2 min,Vector2 max,bool highlight)
        {
            var o=new GameObject("Tarjeta",typeof(RectTransform),typeof(Image));o.transform.SetParent(page,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=o.GetComponent<Image>();image.color=highlight?Hex("#3a2c66"):Hex("#2a2048");image.raycastTarget=false;
            return o.transform;
        }
        // purchaseButton web: comprado, comprar · precio o faltan n; deshabilitado si no procede.
        void PurchaseButton(Transform card,int coins,int price,bool owned,Action buy,Vector2? min=null,Vector2? max=null)
        {
            string label=owned?CombatText.Get("meta.owned"):coins>=price?CombatText.Format("meta.buy","n",price):CombatText.Format("meta.need","n",price-coins);
            var b=Button(card,label,min??new Vector2(.05f,.04f),max??new Vector2(.95f,.22f),buy);b.interactable=!owned&&coins>=price;
            b.GetComponentInChildren<Text>().fontSize=24;
        }
        static string UnlockName(string kind,string id)=>CombatText.Get((kind=="character"?"character.":kind=="weapon"?"weapon.":"item.")+id);
        static string UnlockDescription(string kind,string id)
        {
            if(kind=="character")return CombatText.Get("character."+id+".passive");
            if(kind=="weapon")return CombatText.Get("weapon."+id+".desc");
            var def=Array.Find(Catalog.Items,i=>i.id==id);return def!=null?RunHud.ItemDescription(def):"";
        }
    }
}
