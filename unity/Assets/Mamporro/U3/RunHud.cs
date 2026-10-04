using System;
using System.Text;
using Mamporro.Core;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // HUD técnico de la partida en uGUI; equivale a src/ui/Hud.ts: vida, experiencia y nivel,
    // oro, cuenta atrás y enjambre, bajas, minimapa, barra del jefe, armas, tomos y objetos,
    // aviso de interacción, barra de mesa camilla/desafío, avisos, objeto conseguido y
    // destello al recibir un golpe. Solo reescribe los textos que cambian.
    public sealed class RunHud : MonoBehaviour
    {
        public const float NoticeSeconds=2.6f,ItemSeconds=4.2f;
        U3Game game;
        GameObject root;
        Font font;
        Image xpFill,hpFill,bossFill,progressFill,hurt;
        Text level,hpText,gold,passive,timer,swarm,bossName,kills,weapons,tomes,items,prompt,progressLabel,bannerText;
        GameObject boss,progress,banner;
        readonly Text[] notices=new Text[3];readonly float[] noticeLeft=new float[3];readonly bool[] noticeBig=new bool[3];
        float bannerLeft,hurtLeft;
        string lastWeapons,lastTomes,lastItems;
        public Minimap Map {get;private set;}
        public bool Visible=>root&&root.activeSelf;
        public string TimerText=>timer.text;
        public string PromptText=>prompt.gameObject.activeSelf?prompt.text:"";
        public bool BossVisible=>boss.activeSelf;
        public bool BannerVisible=>banner.activeSelf;
        public string BannerText=>bannerText.text;
        // Avisos visibles, del más antiguo al más reciente.
        public string NoticeText(int i)=>noticeLeft[i]>0?notices[i].text:"";
        // Todo el texto activo del HUD (pruebas de idioma).
        public string AllText{get{var sb=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Text>())sb.Append(t.text).Append('\n');return sb.ToString();}}

        static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}

        public void Build(U3Game owner)
        {
            game=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject=new GameObject("HUD U3",typeof(Canvas),typeof(CanvasScaler));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root=new GameObject("Raíz",typeof(RectTransform));root.transform.SetParent(canvasObject.transform,false);Stretch(root);
            hurt=Box(root.transform,"Golpe",new Color(.85f,.05f,.05f,0),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            // Barra de experiencia (borde superior).
            var xp=Box(root.transform,"Experiencia",Hex("#140f24"),new Vector2(0,1),new Vector2(1,1),new Vector2(0,-12),Vector2.zero);
            xpFill=Fill(xp.transform,Hex("#6ad0ff"));
            // Arriba a la izquierda: nivel, vida, oro y pasiva.
            level=Label(root.transform,"Nivel",34,TextAnchor.UpperLeft,new Vector2(0,1),new Vector2(24,-24),new Vector2(200,44));
            var hp=Box(root.transform,"Vida",Hex("#2a1020"),new Vector2(0,1),new Vector2(0,1),new Vector2(24,-110),new Vector2(424,-76));
            hpFill=Fill(hp.transform,Hex("#d84040"));hpText=Label(hp.transform,"Texto",24,TextAnchor.MiddleCenter,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(hpText.gameObject);
            gold=Label(root.transform,"Oro",28,TextAnchor.UpperLeft,new Vector2(0,1),new Vector2(24,-120),new Vector2(400,40));gold.color=Hex("#f6c63a");
            passive=Label(root.transform,"Pasiva",22,TextAnchor.UpperLeft,new Vector2(0,1),new Vector2(24,-160),new Vector2(500,32));
            // Arriba en el centro: cuenta atrás, enjambre y jefe.
            timer=Label(root.transform,"Tiempo",52,TextAnchor.UpperCenter,new Vector2(.5f,1),new Vector2(0,-20),new Vector2(300,64));
            swarm=Label(root.transform,"Enjambre",30,TextAnchor.UpperCenter,new Vector2(.5f,1),new Vector2(0,-84),new Vector2(400,40));swarm.color=Hex("#ff4a3a");
            boss=new GameObject("Jefe",typeof(RectTransform));boss.transform.SetParent(root.transform,false);Place(boss,new Vector2(.5f,1),new Vector2(0,-130),new Vector2(720,64));
            bossName=Label(boss.transform,"Nombre",26,TextAnchor.UpperCenter,new Vector2(0,1),new Vector2(0,0),new Vector2(720,32));
            var track=Box(boss.transform,"Barra",Hex("#2a1020"),Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,26));bossFill=Fill(track.transform,Hex("#b070ff"));
            // Arriba a la derecha: minimapa y bajas.
            Map=new Minimap();var map=new GameObject("Minimapa",typeof(RectTransform),typeof(RawImage));map.transform.SetParent(root.transform,false);
            Place(map,new Vector2(1,1),new Vector2(-24,-24),new Vector2(224,224));var raw=map.GetComponent<RawImage>();raw.texture=Map.Texture;raw.raycastTarget=false;
            kills=Label(root.transform,"Bajas",26,TextAnchor.UpperRight,new Vector2(1,1),new Vector2(-24,-256),new Vector2(400,36));
            // Abajo a la izquierda: objetos, armas y tomos.
            items=Label(root.transform,"Objetos",22,TextAnchor.LowerLeft,Vector2.zero,new Vector2(24,236),new Vector2(560,200));
            weapons=Label(root.transform,"Armas",24,TextAnchor.LowerLeft,Vector2.zero,new Vector2(24,24),new Vector2(320,200));
            tomes=Label(root.transform,"Tomos",24,TextAnchor.LowerLeft,Vector2.zero,new Vector2(352,24),new Vector2(320,200));tomes.color=Hex("#9fd8ff");
            // Abajo en el centro: barra de progreso y aviso de interacción.
            progress=new GameObject("Progreso",typeof(RectTransform));progress.transform.SetParent(root.transform,false);Place(progress,new Vector2(.5f,0),new Vector2(0,150),new Vector2(520,60));
            progressLabel=Label(progress.transform,"Texto",24,TextAnchor.UpperCenter,new Vector2(0,1),Vector2.zero,new Vector2(520,30));
            var bar=Box(progress.transform,"Barra",Hex("#2a1020"),Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,22));progressFill=Fill(bar.transform,Hex("#ff8a2a"));
            prompt=Label(root.transform,"Interactuar",30,TextAnchor.MiddleCenter,new Vector2(.5f,0),new Vector2(0,90),new Vector2(960,48));
            // Avisos y objeto recién conseguido.
            for(int i=0;i<3;i++){notices[i]=Label(root.transform,"Aviso "+i,32,TextAnchor.MiddleCenter,new Vector2(.5f,.5f),new Vector2(0,200-i*62),new Vector2(1600,60));notices[i].gameObject.SetActive(false);}
            banner=Box(root.transform,"Objeto",new Color(.08f,.06f,.14f,.92f),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-484,-140),new Vector2(-24,120)).gameObject;
            bannerText=Label(banner.transform,"Texto",24,TextAnchor.UpperLeft,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(bannerText.gameObject,14);
            banner.SetActive(false);prompt.gameObject.SetActive(false);progress.SetActive(false);boss.SetActive(false);
            foreach(var t in new[]{level,hpText,gold,passive,timer,swarm,bossName,kills,weapons,tomes,items,prompt,progressLabel})t.gameObject.AddComponent<Outline>().effectColor=Hex("#140f24");
            root.SetActive(false);
        }

        public void Show(bool visible){if(root)root.SetActive(visible);if(!visible)ClearTransient();}
        // Nueva partida: sin avisos, objeto ni destello, y minimapa del mapa actual.
        public void Reset(WorldData world)
        {
            ClearTransient();lastWeapons=lastTomes=lastItems=null;
            Map.SetTerrain(world.Heightfield,world.Sites,world.Collision.Limit);
        }
        void ClearTransient()
        {
            for(int i=0;i<3;i++){noticeLeft[i]=0;if(notices[i])notices[i].gameObject.SetActive(false);}
            bannerLeft=hurtLeft=0;if(banner)banner.SetActive(false);if(hurt)hurt.color=new Color(.85f,.05f,.05f,0);
        }

        public void Notice(string text,bool big)
        {
            // Como mucho tres a la vez: se desplazan y el más antiguo se va.
            for(int i=0;i<2;i++){notices[i].text=notices[i+1].text;noticeLeft[i]=noticeLeft[i+1];noticeBig[i]=noticeBig[i+1];}
            notices[2].text=text;noticeLeft[2]=NoticeSeconds;noticeBig[2]=big;
            for(int i=0;i<3;i++){notices[i].fontSize=noticeBig[i]?44:30;notices[i].color=noticeBig[i]?Hex("#ffd84a"):Hex("#f4efe0");notices[i].gameObject.SetActive(noticeLeft[i]>0);}
        }
        public void ShowItem(ItemDef def)
        {
            var sb=new StringBuilder();
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Rarity(def.rarity))).Append('>').Append(CombatText.Get("rarity."+def.rarity)).Append("</color>\n");
            sb.Append("<size=32><b>").Append(CombatText.Get("item."+def.id)).Append("</b></size>");
            foreach(var e in def.effects)sb.Append('\n').Append(CardText.EffectLine(e,e.amount));
            sb.Append("\n<i>").Append(ItemDescription(def)).Append("</i>");
            bannerText.text=sb.ToString();banner.SetActive(true);bannerLeft=ItemSeconds;
        }
        public void FlashHurt(){hurtLeft=.45f;}
        public bool HurtFlashing=>hurtLeft>0;
        // Cambio de idioma: fuera los avisos y el objeto conseguido (textos ya escritos).
        public void ClearNotices()=>ClearTransient();
        static Color Rarity(string r)=>r=="uncommon"?Hex("#8cc84b"):r=="rare"?Hex("#4aa8ff"):r=="epic"?Hex("#c070ff"):r=="legendary"?Hex("#ffb020"):Hex("#c8c0dc");
        // itemDescriptionParams de la web (porcentajes de los objetos con efecto especial).
        public static string ItemDescription(ItemDef def)
        {
            string Pct(double v)=>CombatText.Number(v*100,1);
            switch(def.id){
                case "perlas":return CombatText.Format("item.perlas.desc","n",Pct(.3),"dmg",Pct(.5));
                case "monedero":return CombatText.Format("item.monedero.desc","n",Pct(.04),"max",Pct(.8));
                case "olla":return CombatText.Format("item.olla.desc","n",Pct(.08));
                case "bata":return CombatText.Format("item.bata.desc","n",Pct(.5));
                default:return CombatText.Get("item."+def.id+".desc");
            }
        }

        // mm:ss; durante el enjambre, +mm:ss.
        public static string FormatTime(double seconds){int s=(int)Math.Max(0,Math.Floor(seconds));return $"{s/60:00}:{s%60:00}";}
        public static string FormatCountdown(double timeLeft)=>timeLeft>=0?FormatTime(Math.Ceiling(timeLeft)):"+"+FormatTime(-timeLeft);

        void LateUpdate()
        {
            if(!Visible||game==null||game.Session==null)return;
            float dt=Time.unscaledDeltaTime;var s=game.Session;var r=s.Combat;
            for(int i=0;i<3;i++)if(noticeLeft[i]>0){noticeLeft[i]-=dt;if(noticeLeft[i]<=0)notices[i].gameObject.SetActive(false);}
            if(bannerLeft>0){bannerLeft-=dt;if(bannerLeft<=0)banner.SetActive(false);}
            if(hurtLeft>0){hurtLeft=Mathf.Max(0,hurtLeft-dt);hurt.color=new Color(.85f,.05f,.05f,hurtLeft/.45f*.35f);}
            double maxHp=r.Stats[Stat.maxHp];
            SetFill(xpFill,r.Xp/Rules.XpNeeded(r.Level));SetFill(hpFill,r.Hp/maxHp);
            Set(hpText,$"{Math.Ceiling(r.Hp)} / {Rules.Round(maxHp)}");Set(level,CombatText.Format("hud.level","n",r.Level));
            Set(gold,CombatText.Format("hud.gold","n",Math.Floor(r.Gold)));
            Set(passive,r.Character.passive=="shield"?(r.ShieldCharge>=r.Character.recharge?CombatText.Get("passive.shieldReady"):CombatText.Format("passive.shieldCharge","n",Math.Ceiling(r.Character.recharge-r.ShieldCharge))):"");
            Set(timer,FormatCountdown(s.TimeLeft));timer.color=s.Swarm?Hex("#ff4a3a"):s.TimeLeft<=30?Hex("#ffb020"):Hex("#f4efe0");
            Set(swarm,s.Swarm?CombatText.Get("hud.swarm"):"");Set(kills,CombatText.Format("hud.kills","n",r.Kills));
            int bi=r.Boss!=null?r.Enemies.IndexOf(r.Boss.EnemyId):-1;boss.SetActive(bi>=0);
            if(bi>=0){Set(bossName,CombatText.Get("enemy.pelusaMadre"));SetFill(bossFill,r.Enemies.Hp[bi]/r.Enemies.MaxHp[bi]);bossFill.color=r.Boss.Enraged?Hex("#ff4a3a"):Hex("#b070ff");}
            string w=Chips(r,0),t=Chips(r,1),it=Chips(r,2);
            if(w!=lastWeapons){lastWeapons=w;weapons.text=w;}if(t!=lastTomes){lastTomes=t;tomes.text=t;}if(it!=lastItems){lastItems=it;items.text=it;}
            string p=game.PromptText();prompt.gameObject.SetActive(p.Length>0);Set(prompt,p);
            // La mesa camilla cargándose o el desafío del tótem en marcha.
            var list=s.Interactables;bool showProgress=false;
            if(list.Charging>=0){var shrine=list.List[list.Charging];Set(progressLabel,CombatText.Format("hud.shrine","n",Math.Floor(shrine.Charge*100)));SetFill(progressFill,shrine.Charge);showProgress=true;}
            else if(list.Challenge>0){Set(progressLabel,CombatText.Format("hud.challenge","time",FormatTime(Math.Ceiling(list.Challenge))));SetFill(progressFill,list.Challenge/Tuning.TotemDuration);showProgress=true;}
            progress.SetActive(showProgress);
            Map.Update(s,game.WebYaw,Time.unscaledTime);
        }
        // 0 armas, 1 tomos (nombre corto), 2 objetos con copias.
        static string Chips(CombatRun r,int kind)
        {
            var sb=new StringBuilder();
            if(kind==0)foreach(var w in r.Weapons)sb.Append(CombatText.Format("hud.weaponLevel","name",CombatText.Get("weapon."+w.Def.id),"n",w.Level)).Append('\n');
            else if(kind==1)foreach(var t in r.Tomes)sb.Append(CombatText.Format("hud.weaponLevel","name",CombatText.Get("tome."+t.Def.id+".short"),"n",t.Level)).Append('\n');
            else foreach(var s in r.Items){string name=CombatText.Get("item."+s.Def.id);sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Rarity(s.Def.rarity))).Append('>').Append(s.Count>1?CombatText.Format("hud.itemCount","name",name,"n",s.Count):name).Append("</color>\n");}
            return sb.ToString();
        }
        static void Set(Text t,string value){if(t.text!=value)t.text=value;}
        static void SetFill(Image fill,double value){var rt=(RectTransform)fill.transform;float v=Mathf.Clamp01((float)value);if(rt.anchorMax.x!=v)rt.anchorMax=new Vector2(v,1);}

        static void Stretch(GameObject o,float pad=0){var r=(RectTransform)o.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(pad,pad);r.offsetMax=new Vector2(-pad,-pad);}
        static void Place(GameObject o,Vector2 anchor,Vector2 position,Vector2 size){var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;}
        Image Box(Transform parent,string name,Color color,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;var i=o.GetComponent<Image>();i.color=color;i.raycastTarget=false;return i;
        }
        Image Fill(Transform parent,Color color){var f=Box(parent,"Relleno",color,Vector2.zero,new Vector2(0,1),Vector2.zero,Vector2.zero);return f;}
        Text Label(Transform parent,string name,int size,TextAnchor anchor,Vector2 anchorAt,Vector2 position,Vector2 box)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);Place(o,anchorAt,position,box);
            var t=o.GetComponent<Text>();t.font=font;t.fontSize=size;t.alignment=anchor;t.color=Hex("#f4efe0");t.supportRichText=true;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;
        }
        void OnDestroy(){Map?.Dispose();}
    }

    // Minimapa (src/ui/Minimap.ts): el mapa entero desde arriba (norte arriba), con el
    // jugador, los interactuables descubiertos y el jefe. El terreno se pinta una vez por
    // mapa; las marcas se redibujan como mucho diez veces por segundo.
    public sealed class Minimap : IDisposable
    {
        public const int Size=112;
        public readonly Texture2D Texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Minimapa U3"};
        readonly Color32[] terrain=new Color32[Size*Size],pixels=new Color32[Size*Size];
        double extent=1;float lastDraw=-1;
        public int Markers {get;private set;}
        public bool BossShown {get;private set;}
        double Scale=>Size/(extent*2);
        static Color32 C(uint hex)=>new Color32((byte)(hex>>16),(byte)(hex>>8),(byte)hex,255);
        static readonly Color32 Ink=C(0x140f24),White=C(0xffffff),Outside=C(0x2a2438);

        public void SetTerrain(Heightfield hf,System.Collections.Generic.List<Site> sites,double limit)
        {
            extent=limit+4;var low=new[]{Palette.GrassLight,Palette.Grass,Palette.GrassDark,Palette.Moss,Palette.Rock,Palette.RockLight};
            var heights=new double[Size*Size];double min=double.PositiveInfinity,max=double.NegativeInfinity;
            for(int j=0;j<Size;j++)for(int i=0;i<Size;i++){
                double x=(i+.5)/Scale-extent,z=(j+.5)/Scale-extent,y=hf.HeightAt(x,z);heights[j*Size+i]=y;
                if(WorldMath.Squircle(x,z)<=limit){min=Math.Min(min,y);max=Math.Max(max,y);}
            }
            for(int j=0;j<Size;j++)for(int i=0;i<Size;i++){
                double x=(i+.5)/Scale-extent,z=(j+.5)/Scale-extent,y=heights[j*Size+i];bool inside=WorldMath.Squircle(x,z)<=limit;
                int band=Math.Min(low.Length-1,Math.Max(0,(int)Math.Floor((y-min)/Math.Max(1,max-min)*low.Length)));
                var color=inside?C(low[band]):Outside;
                // Sombreado: más claro hacia el noroeste, más oscuro hacia el sureste.
                double west=heights[j*Size+Math.Max(0,i-1)],north=heights[Math.Max(0,j-1)*Size+i],shade=Math.Max(-.25,Math.Min(.25,(y-west+(y-north))*.12));
                byte S(byte v)=>(byte)Math.Max(0,Math.Min(255,v*(1+shade)));
                terrain[Index(i,j)]=new Color32(S(color.r),S(color.g),S(color.b),255);
            }
            // Construcciones: manchas de piedra.
            foreach(var site in sites){double r=Math.Max(1.5,site.Radius*Scale*.6),px=(site.X+extent)*Scale,py=(site.Z+extent)*Scale;Rect(terrain,(int)Math.Round(px-r),(int)Math.Round(py-r),(int)Math.Round(r*2),(int)Math.Round(r*2),C(Palette.StoneDark));}
            Array.Copy(terrain,pixels,pixels.Length);Texture.SetPixels32(pixels);Texture.Apply(false);lastDraw=-1;
        }
        // Fila 0 de la textura es la de abajo: el norte (z negativa) queda arriba como en el canvas.
        static int Index(int x,int y)=>(Size-1-y)*Size+x;
        static void Rect(Color32[] target,int x,int y,int w,int h,Color32 c)
        {for(int j=Math.Max(0,y);j<Math.Min(Size,y+h);j++)for(int i=Math.Max(0,x);i<Math.Min(Size,x+w);i++)target[Index(i,j)]=c;}

        public void Update(WorldRun run,double yaw,float now,bool force=false)
        {
            if(!force&&lastDraw>=0&&now-lastDraw<.1f)return;
            lastDraw=now;Array.Copy(terrain,pixels,pixels.Length);bool blink=Mathf.FloorToInt(now*1000/350)%2==0;Markers=0;
            foreach(var m in run.Interactables.List){
                if(!m.Discovered)continue;Markers++;
                int x=(int)Math.Round((m.Spot.X+extent)*Scale),y=(int)Math.Round((m.Spot.Z+extent)*Scale);
                switch(m.Spot.Kind){
                    case "chest":Rect(pixels,x-2,y-2,5,5,Ink);Rect(pixels,x-1,y-1,3,3,C(m.Used?Palette.Used:Palette.Coin));break;
                    case "shrine":Rect(pixels,x-2,y-3,5,7,Ink);Rect(pixels,x-3,y-2,7,5,Ink);var g=C(m.Used?Palette.Used:Palette.ShrineGlow);Rect(pixels,x-1,y-2,3,5,g);Rect(pixels,x-2,y-1,5,3,g);break;
                    case "totem":Rect(pixels,x-1,y-4,3,8,Ink);Rect(pixels,x,y-3,1,6,C(m.Used?Palette.Used:Palette.TotemGlow));break;
                    default:Rect(pixels,x-3,y-4,7,9,Ink);Rect(pixels,x-2,y-3,5,7,m.Used?C(Palette.Used):blink?C(Palette.PortalGlow):White);break;
                }
            }
            var r=run.Combat;int bi=r.Boss!=null?r.Enemies.IndexOf(r.Boss.EnemyId):-1;BossShown=bi>=0;
            if(bi>=0){int x=(int)Math.Round((r.Enemies.X[bi]+extent)*Scale),y=(int)Math.Round((r.Enemies.Z[bi]+extent)*Scale);Rect(pixels,x-3,y-3,7,7,Ink);Rect(pixels,x-2,y-2,5,5,blink?C(Palette.Telegraph):White);}
            // Jugador: flecha hacia donde mira la cámara.
            double px=(run.Body.X+extent)*Scale,py=(run.Body.Z+extent)*Scale,fx=-Math.Sin(yaw),fz=-Math.Cos(yaw);
            Triangle(px+fx*5,py+fz*5,px-fx*3-fz*3,py-fz*3+fx*3,px-fx*3+fz*3,py-fz*3-fx*3);
            Texture.SetPixels32(pixels);Texture.Apply(false);
        }
        void Triangle(double ax,double ay,double bx,double by,double cx,double cy)
        {
            int x0=(int)Math.Floor(Math.Min(ax,Math.Min(bx,cx)))-1,x1=(int)Math.Ceiling(Math.Max(ax,Math.Max(bx,cx)))+1,y0=(int)Math.Floor(Math.Min(ay,Math.Min(by,cy)))-1,y1=(int)Math.Ceiling(Math.Max(ay,Math.Max(by,cy)))+1;
            double E(double x1_,double y1_,double x2,double y2,double x,double y)=>(x2-x1_)*(y-y1_)-(y2-y1_)*(x-x1_);
            for(int j=Math.Max(0,y0);j<=Math.Min(Size-1,y1);j++)for(int i=Math.Max(0,x0);i<=Math.Min(Size-1,x1);i++){
                double x=i+.5,y=j+.5,e0=E(ax,ay,bx,by,x,y),e1=E(bx,by,cx,cy,x,y),e2=E(cx,cy,ax,ay,x,y);
                bool inside=(e0>=0&&e1>=0&&e2>=0)||(e0<=0&&e1<=0&&e2<=0);
                if(inside)pixels[Index(i,j)]=White;
                else{
                    // Contorno oscuro de un píxel alrededor de la flecha.
                    for(int k=0;k<4&&!inside;k++){double ox=x+(k==0?1:k==1?-1:0),oy=y+(k==2?1:k==3?-1:0);double f0=E(ax,ay,bx,by,ox,oy),f1=E(bx,by,cx,cy,ox,oy),f2=E(cx,cy,ax,ay,ox,oy);if((f0>=0&&f1>=0&&f2>=0)||(f0<=0&&f1<=0&&f2<=0)){pixels[Index(i,j)]=Ink;inside=true;}}
                }
            }
        }
        public void Dispose(){if(Texture)UnityEngine.Object.Destroy(Texture);}
    }
}
