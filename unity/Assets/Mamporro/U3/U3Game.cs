using System;
using System.Diagnostics;
using Mamporro.Core;
using Mamporro.Core.Progress;
using Mamporro.Persistence;
using Mamporro.U2;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Controlador de la escena U3 (Game.ts): estados inicio → partida (con pausa y cartas)
    // → resultados, mundo real, física del jugador de la web, cámara, entrada y la salida
    // retro de U1 (textura interna, dithering y ajuste de vértices). La lógica trabaja en
    // coordenadas web; aquí se convierte a Unity (Z invertida).
    public sealed class U3Game : MonoBehaviour,ICombatEffects
    {
        public enum Screen { Title, Playing, Results }

        public Camera worldCamera;
        public RawImage display;
        public Text status,help;
        public Shader worldShader,skyShader;
        // Referencia serializada: conserva la variante de instancing en la build.
        public Material combatTemplate;
        // Mapa con el que arranca la escena técnica (-u3-seed lo cambia). «Nuevo mapa» sortea otro.
        public string defaultSeed="MAMPORRO";

        public WorldRenderer Renderer {get;private set;}
        public WorldData World=>Renderer?Renderer.World:null;
        public string Seed {get;private set;}
        public WorldRun Session {get;private set;}
        public CombatRun Run=>Session.Combat;
        public PlayerBody Body=>Session.Body;
        public RunRenderer CombatView {get;private set;}
        public RunCards Cards {get;private set;}
        public RunHud Hud {get;private set;}
        public RunScreens Screens {get;private set;}
        // U5: audio de la aplicación (música, efectos, foco y opciones de volumen).
        public AudioDirector Audio {get;private set;}
        // U5: receptor único de los sucesos de la partida (sonido, partículas, números, cámara).
        public RunFeedback Feedback {get;private set;}
        public Mamporro.Core.Effects.ParticleField Particles=>Feedback.Particles;
        public ParticleRenderer ParticleView {get;private set;}
        public Mamporro.Core.Effects.DamageNumbers Numbers=>Feedback.Numbers;
        public DamageNumberRenderer NumberView {get;private set;}
        public Material CombatMaterial {get;private set;}
        public Mesh CombatMesh {get;private set;}
        public int InternalHeight {get;private set;}=360;
        public bool Dither {get;private set;}=true;
        public bool Snap {get;private set;}=true;
        public bool Paused {get;private set;}=true;
        public Screen State {get;private set;}=Screen.Title;
        public long RenderedFrames {get;private set;}
        // U4: personaje de la próxima partida según el progreso permanente. Asignar uno lo
        // selecciona y lo guarda solo si está desbloqueado (MetaRules.Select); si no, se ignora.
        public CharacterDef Character
        {
            get=>Array.Find(Catalog.Characters,c=>c.id==Progress.Character);
            set{if(value!=null)Progress.Select(value.id);}
        }
        public int Minutes=10;
        // Progreso permanente (único ProgressStore de la aplicación) y liquidación de la última partida.
        public ProgressSession Progress {get;private set;}
        public Settlement LastSettlement {get;private set;}
        RunSetup runSetup;
        // Giro de cámara en convenio web (0 = mirando hacia -Z de la web).
        public double WebYaw=>-yaw*Mathf.Deg2Rad;
        // Cámara libre (comprobación visual): LateUpdate no la mueve.
        public bool FreeCamera;
        // Comprobación visual: oculta HUD y pantallas para fotografiar solo el mundo.
        public bool HideInterface;
        // Ensayo de rendimiento: las cartas se resuelven solas con la primera, sin pantalla.
        public bool AutoChoose;
        // U4: carpeta del progreso persistente. Las pruebas la sustituyen por una temporal propia
        // (nunca el guardado del autor); -u4-save-dir permite lo mismo en la build.
        public static string SaveDirectoryOverride;
        public string SaveDirectory
        {
            get
            {
                if(!string.IsNullOrEmpty(SaveDirectoryOverride))return SaveDirectoryOverride;
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-u4-save-dir");
                return i>=0&&i+1<args.Length?args[i+1]:System.IO.Path.Combine(Application.persistentDataPath,"Progress");
            }
        }
        // Idioma actual de la aplicación (el del progreso; sin guardado, el del sistema como
        // detectLanguage web: español si el sistema está en español, si no inglés).
        public string Language=>CombatText.English?"en":"es";
        // Pruebas: idioma por defecto cuando no hay guardado, independiente del sistema.
        public static string DefaultLanguageOverride;
        static string SystemLanguage=>DefaultLanguageOverride??(Application.systemLanguage==UnityEngine.SystemLanguage.Spanish?"es":"en");
        // U4 paso 8: opciones vigentes (las 14 de SettingsDto, del progreso permanente). Las que
        // se leen al usarse (sensibilidad, Ctrl, destellos, FPS, partículas, sacudida y audio)
        // salen siempre de aquí; idioma, resolución, dithering, vértices y duración se aplican
        // en ApplySettings. Volúmenes y silencio solo se guardan: el sonido llega en U5.
        public SettingsDto Settings=>Progress.Progress.settings;
        // Cambio de una opción (Game.changeSettings): ProgressSession la valida y la guarda
        // (o la deja solo para esta sesión si no puede) y aquí se aplica al momento.
        public SettingsChange ChangeSettings(Action<SettingsDto> change)
        {
            var result=Progress.ChangeSettings(change);if(result.Applied)ApplySettings();return result;
        }
        // Idioma desde el menú o la pausa: se guarda y se reconstruyen las pantallas abiertas.
        public void SetLanguage(string language)=>ChangeSettings(s=>s.language=language);
        // Duración elegida en la preparación; se guarda como la opción runMinutes de la web.
        // La partida en curso no cambia: la duración solo se lee al empezar otra.
        public void SetMinutes(int minutes)=>ChangeSettings(s=>s.runMinutes=minutes);
        // Aplica las opciones vigentes (Game.applySettings). Se llama al arrancar, tras cada
        // cambio y al adoptar un progreso importado o recuperado.
        public void ApplySettings()
        {
            var s=Settings;bool english=s.language=="en",relabel=english!=CombatText.English;
            CombatText.English=english;Minutes=s.runMinutes;
            if(target==null||InternalHeight!=s.renderHeight||Dither!=s.dithering||Snap!=s.vertexSnap)ConfigurePresentation(s.renderHeight,s.dithering,s.vertexSnap);
            // Sin textos del idioma anterior: avisos efímeros fuera y pantallas abiertas reconstruidas.
            if(relabel&&Screens!=null){Hud.ClearNotices();Screens.Rebuild();}
            Audio?.Configure();
            Feedback?.Particles.SetReduced(s.reducedParticles);
        }
        // La importación usa el mismo almacén que la partida: al confirmarla, la sesión adopta
        // el progreso importado (también sus opciones) y la siguiente partida ya lo usa, sin
        // reiniciar la aplicación.
        public ProgressImporter CreateImporter()=>new ProgressImporter(Progress.Store,Progress.Language);
        public void AdoptImported(StoreResult result){if(State==Screen.Title){Progress.Adopt(result);ApplySettings();}}
        // Recuperación confirmada desde el menú; el progreso recuperado trae sus opciones.
        public StoreResult RecoverProgress(){var result=Progress.Recover(true);ApplySettings();return result;}
        public int AutoChosen {get;private set;}
        U3Benchmark benchmark;
        public bool DebugVisible {get;private set;}
        // Tiempos del último tick de lógica y del envío de la cámara del mundo (ms).
        public double LogicMs {get;private set;}
        public double RenderMs {get;private set;}

        Transform avatar;
        Material avatarMaterial;
        RenderTexture target;
        PlayerIntent intent;
        // E pulsada desde el último tick (se usa una vez, como input.wasPressed de la web).
        bool interactPressed;
        float yaw,pitch=20,textTimer;
        int previousWidth,previousHeight;
        Vector3 previousPosition,currentPosition;
        readonly Stopwatch logicWatch=new Stopwatch(),renderWatch=new Stopwatch();
        ProfilerRecorder drawCalls,triangles;
        static readonly int SnapId=Shader.PropertyToID("_RetroSnap"),DitherId=Shader.PropertyToID("_RetroDither"),SizeId=Shader.PropertyToID("_RetroSize");

        void Awake()
        {
            Application.runInBackground=true;
            QualitySettings.SetQualityLevel(0);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            Time.fixedDeltaTime=1f/60;Time.maximumDeltaTime=.1f;
            Renderer=gameObject.AddComponent<WorldRenderer>();Renderer.worldShader=worldShader;Renderer.skyShader=skyShader;
            avatar=BuildAvatar();
            CombatText.Load();
            // Carga del progreso al arrancar (ProgressStore.Load). Sin guardado: progreso inicial en
            // memoria, que se escribe en la primera liquidación o elección. Con problemas: se
            // conserva el archivo y se informa en el menú; nunca se resetea en silencio.
            Progress=new ProgressSession(new ProgressFiles(SaveDirectory),SystemLanguage);
            CombatText.English=Settings.language=="en";Minutes=Settings.runMinutes;
            if(!combatTemplate||!combatTemplate.enableInstancing)throw new InvalidOperationException("Falta el material instanciado de combate U3.");
            CombatMaterial=new Material(combatTemplate){name="Combate U3",enableInstancing=true};
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);
            CombatMesh=Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);Destroy(primitive);
            var colors=new Color[CombatMesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;CombatMesh.colors=colors;
            CombatView=gameObject.AddComponent<RunRenderer>();CombatView.Initialize(this);
            Cards=gameObject.AddComponent<RunCards>();Cards.Session=this;Cards.Build();
            Hud=gameObject.AddComponent<RunHud>();Hud.Build(this);
            Screens=gameObject.AddComponent<RunScreens>();Screens.Build(this);
            // Audio desde el arranque (en Windows no hace falta esperar a un gesto, a diferencia de la web).
            Audio=gameObject.AddComponent<AudioDirector>();Audio.Initialize(worldCamera.gameObject,()=>Progress.Progress.settings);
            Feedback=new RunFeedback(this);ParticleView=new ParticleRenderer(CombatMaterial,CombatMesh);NumberView=new DamageNumberRenderer();
            ApplySettings();
            var args=Environment.GetCommandLineArgs();int seedArg=Array.IndexOf(args,"-u3-seed");
            LoadWorld(seedArg>=0&&seedArg+1<args.Length?NormalizeSeed(args[seedArg+1])??defaultSeed:defaultSeed);
        }

        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-u3-visual-check")>=0)gameObject.AddComponent<U3VisualCheck>().Game=this;
            else if(Array.IndexOf(args,"-u3-benchmark")>=0){benchmark=gameObject.AddComponent<U3Benchmark>();benchmark.Game=this;}
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering+=OnCameraBegin;RenderPipelineManager.endCameraRendering+=OnCameraRendered;
            drawCalls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=OnCameraBegin;RenderPipelineManager.endCameraRendering-=OnCameraRendered;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;drawCalls.Dispose();triangles.Dispose();
        }
        void OnCameraBegin(ScriptableRenderContext context,Camera camera){if(camera==worldCamera)renderWatch.Restart();}
        void OnCameraRendered(ScriptableRenderContext context,Camera camera){if(camera==worldCamera){RenderedFrames++;RenderMs=renderWatch.Elapsed.TotalMilliseconds;}}

        // ------------------------------------------------------------ estados (Game.ts)
        // Mapa nuevo con esa semilla y vuelta a la pantalla de inicio.
        public void LoadWorld(string seed)
        {
            Seed=seed;Renderer.Build(WorldData.Generate(seed),seed);
            BeginRun();State=Screen.Title;
        }
        // Partida nueva en el mapa actual con el personaje y la duración elegidos (beginRun).
        public void BeginRun()
        {
            // Cada partida recibe del progreso permanente su personaje (desbloqueado), filtros de
            // armas/objetos y usos iniciales (2 + extras). Gastarlos no toca el progreso.
            runSetup=Progress.Begin();LastSettlement=null;Progress.ForgetSettlement();
            Session=new WorldRun(World,Seed,Array.Find(Catalog.Characters,c=>c.id==runSetup.Character),this,Minutes);
            ProgressSession.Apply(runSetup,Session);Session.Combat.Feedback=Feedback;
            FreeCamera=false;DebugVisible=false;
            ResetPlayer();CombatView.Clear();Feedback.Clear();Cards.Hide();Hud.Reset(World);eventCursor=0;lastHp=Run.Hp;
            Screens.ShowResults(false);SetPaused(true);UpdateHelp();
        }
        // Jugar desde el inicio: la semilla escrita (normalizada) cambia el mapa si es otra.
        public void StartRun(string seedText)
        {
            string seed=NormalizeSeed(seedText??"");
            if(seed!=null&&seed!=Seed)LoadWorld(seed);else BeginRun();
            // El menú se cierra ya: al volver se abre en su página de inicio (UI.show web).
            State=Screen.Playing;Screens.ShowTitle(false);SetPaused(false);
        }
        public void NewMap(){if(State==Screen.Title)LoadWorld(RandomSeed());}
        public void Retry(bool newMap){if(State!=Screen.Results)return;if(newMap)LoadWorld(RandomSeed());else BeginRun();State=Screen.Playing;SetPaused(false);}
        public void BackToTitle(){BeginRun();State=Screen.Title;}
        void FinishRun()
        {
            // Una sola liquidación por partida terminada: el id de partida y lastRun la protegen
            // aunque se vuelva a llamar; abandonar (inicio, F8) nunca pasa por aquí.
            if(State==Screen.Results)return;
            LastSettlement=Progress.Settle(ProgressSession.Summary(Session,runSetup.RunId));
            // Como finishRun web: fuera los efectos pendientes y suena el desenlace.
            Audio.ClearEffects();Audio.Play(Run.Victory?"victory":"defeat");
            State=Screen.Results;SetPaused(true);Screens.ShowResults(true);
        }
        // Reintento manual del guardado si falló: guarda el mismo progreso liquidado, sin repetir premios.
        public void RetrySave(){if(State==Screen.Results&&LastSettlement!=null){LastSettlement=Progress.RetrySave();Screens.ShowResults(true);}}

        // normalizeSeed/randomSeed de la web: mayúsculas, solo A–Z y 0–9, como mucho 12.
        const string SeedAlphabet="23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        public static string NormalizeSeed(string input)
        {
            var sb=new System.Text.StringBuilder();
            foreach(char c in input.ToUpperInvariant())if(((c>='A'&&c<='Z')||(c>='0'&&c<='9'))&&sb.Length<12)sb.Append(c);
            return sb.Length>0?sb.ToString():null;
        }
        public static string RandomSeed(int length=6)
        {
            var bytes=new byte[length*4];System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);var sb=new System.Text.StringBuilder();
            for(int i=0;i<length;i++)sb.Append(SeedAlphabet[(int)(BitConverter.ToUInt32(bytes,i*4)%(uint)SeedAlphabet.Length)]);
            return sb.ToString();
        }

        // Punto de inicio de la web: (0, altura del terreno, 0).
        public void ResetPlayer()
        {
            var hf=World.Heightfield;Body.PlaceAt(0,hf.HeightAt(0,0),0);Body.Facing=0;
            currentPosition=previousPosition=WebSpace.ToUnity(Body.X,Body.Y,Body.Z);yaw=0;pitch=20;intent=default;
            ScriptedIntent=null;Session.SyncPlayer();
        }

        // Pruebas y ensayos: intención fija durante los ticks siguientes (sustituye al teclado).
        [NonSerialized] public PlayerIntent? ScriptedIntent;

        public void SetPaused(bool paused)
        {
            if(Session==null)return;
            if(!paused&&(Run.Choosing||Session.Finished||State==Screen.Results))return;
            // Desde el inicio, continuar equivale a jugar con la partida preparada.
            if(!paused&&State==Screen.Title)State=Screen.Playing;
            Paused=paused;intent=default;interactPressed=false;
            Cursor.lockState=paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=paused;
            RefreshStatus();
        }
        // Sin foco: la partida se pausa y el audio se silencia sin cola de efectos (decisión U5);
        // al volver sigue en pausa, con la música atenuada hasta Continuar.
        void OnApplicationFocus(bool focus){if(!focus&&State==Screen.Playing)SetPaused(true);Audio?.SetFocused(focus);}
        // Modo de música según el estado (Game.draw web): pausa y cartas atenúan; jefe o enjambre, intensa.
        public MusicMode CurrentMusicMode=>State==Screen.Title?MusicMode.Menu:State==Screen.Results?MusicMode.Results:
            Paused?MusicMode.Paused:Run.Boss!=null||Session.Swarm?MusicMode.Intense:MusicMode.Playing;

        public void Emit(CombatEffect effect){CombatView.Emit(effect);Feedback.Effect(in effect);}
        public void Choose(int index){if(Run.Choose(index))AfterChoice(true);}
        public void Skip(){if(Run.Skip())AfterChoice(true);}
        public void Reroll(){if(Run.Reroll())AfterChoice(false);}
        public void Banish(int index){if(Run.Banish(index))AfterChoice(false);}
        void AfterChoice(bool fresh)
        {if(Run.Choosing)Cards.Show(fresh);else{Cards.Hide();SetPaused(false);}}

        // Sucesos nuevos de la partida → avisos, objeto conseguido y destello (Game.showNotice).
        int eventCursor;double lastHp;
        void ReadEvents()
        {
            var events=Run.Events;
            for(;eventCursor<events.Count;eventCursor++){
                var e=events[eventCursor];
                switch(e.Kind){
                    case "wave":Hud.Notice(CombatText.Get(e.Detail),true);break;
                    case "elite":Hud.Notice(CombatText.Format("notice.elite","name",CombatText.Get("enemy."+e.Detail)),true);break;
                    case "boss":Hud.Notice(CombatText.Format("notice.boss","name",CombatText.Get("enemy."+e.Detail)),true);break;
                    case "noGold":Hud.Notice(CombatText.Format("notice.noGold","n",e.Detail),false);break;
                    case "swarm":case "portalFound":case "challengeStart":case "challengeDone":case "revive":Hud.Notice(CombatText.Get("notice."+e.Kind),true);break;
                    case "portalRevealed":case "shrineCharged":case "shield":Hud.Notice(CombatText.Get("notice."+e.Kind),false);break;
                    case "item":var def=Array.Find(Catalog.Items,i=>i.id==e.Detail);if(def!=null)Hud.ShowItem(def);break;
                }
            }
            // Destello rojo al recibir daño solo con «Destellos de daño» (como la web).
            if(Run.Hp<lastHp-1e-9&&Settings.flashes)Hud.FlashHurt();
            lastHp=Run.Hp;
        }
        // Cada pantalla en su estado; HUD durante la partida (también en pausa y con cartas).
        void RefreshPanels()
        {
            bool playing=State==Screen.Playing&&!Session.Finished&&!HideInterface;
            Hud.Show(playing);
            Screens.ShowTitle(State==Screen.Title&&!HideInterface);
            if(Screens.ImportVisible&&(State!=Screen.Title||HideInterface))Screens.ShowImport(false);
            if(HideInterface&&Screens.ResultsVisible)Screens.ShowResults(false);
            bool pause=playing&&Paused&&!Cards.Visible;
            if(pause!=Screens.PauseVisible)Screens.ShowPause(pause);
            if(status.transform.parent.gameObject.activeSelf!=DebugVisible)status.transform.parent.gameObject.SetActive(DebugVisible);
            if(help.transform.parent.gameObject.activeSelf)help.transform.parent.gameObject.SetActive(false);
        }

        void UpdateHelp(){help.text="";}
        // Acciones de depuración de la F3 web (1–8); todas marcan la partida con trucos.
        public void QaAction(int action)
        {
            if(Run.Choosing||Session.Over)return;
            string toast;
            switch(action){
                case 1:toast=Session.DebugToggleInvincible()?"debug.invincibleOn":"debug.invincibleOff";break;
                case 2:Session.DebugLevelUp();toast="debug.levelUp";break;
                case 3:Session.DebugSkipMinute();toast="debug.skipTime";break;
                case 4:Session.DebugSpawn(100);toast="debug.spawn";break;
                case 5:Session.DebugKillAll();toast="debug.killAll";break;
                case 6:toast=Session.DebugSummonBoss()?"debug.boss":"debug.bossBusy";break;
                case 7:Session.DebugAddGold(100);toast="debug.gold";break;
                case 8:Session.DebugRevealMap();toast="debug.reveal";break;
                default:return;
            }
            Hud.Notice(CombatText.Get(toast),false);
            if(Run.OpenChoice()){SetPaused(true);Cards.Show(true);}
        }

        Transform BuildAvatar()
        {
            // Provisional original de U1: conserje de pruebas con bata y visera.
            var root=new GameObject("Jugador provisional").transform;root.SetParent(transform,false);
            var material=avatarMaterial=new Material(worldShader){name="Jugador"};material.SetFloat("_TexMeters",1);
            void Part(string name,Vector3 position,Vector3 scale,uint color)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Destroy(go.GetComponent<UnityEngine.Collider>());
                go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
                var block=new MaterialPropertyBlock();block.SetColor("_InstanceColor",WebSpace.Linear(color));r.SetPropertyBlock(block);
            }
            Part("Bata",new Vector3(0,.65f,0),new Vector3(.65f,1,.4f),Palette.Plaid);
            Part("Cabeza",new Vector3(0,1.35f,0),new Vector3(.45f,.4f,.4f),Palette.Burlap);
            Part("Visera",new Vector3(0,1.48f,.25f),new Vector3(.65f,.12f,.38f),Palette.Hat);
            Part("Pie izquierdo",new Vector3(-.2f,.1f,.08f),new Vector3(.22f,.2f,.5f),Palette.WoodDark);
            Part("Pie derecho",new Vector3(.2f,.1f,.08f),new Vector3(.22f,.2f,.5f),Palette.WoodDark);
            return root;
        }

        void Update()
        {
            if(target==null||previousWidth!=UnityEngine.Screen.width||previousHeight!=UnityEngine.Screen.height)Resize();
            var k=Keyboard.current;var mouse=Mouse.current;
            if(k!=null){
                if(k.escapeKey.wasPressedThisFrame&&State==Screen.Playing&&!Cards.Visible)SetPaused(!Paused);
                if(k.f8Key.wasPressedThisFrame)LoadWorld(Seed);
                if(k.f3Key.wasPressedThisFrame)DebugVisible=!DebugVisible;
                // Como la web: con el panel abierto y jugando (sin pausa ni cartas).
                if(DebugVisible&&State==Screen.Playing&&!Paused&&!Cards.Visible){
                    for(int n=1;n<=8;n++)if(k[(Key)((int)Key.Digit1+n-1)].wasPressedThisFrame){QaAction(n);break;}
                }
                PresentationShortcuts(k);
            }
            if(!Paused&&k!=null){
                var axis=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                var move=Quaternion.Euler(0,yaw,0)*new Vector3(axis.x,0,axis.y);
                var dir=Vector2.ClampMagnitude(new Vector2(move.x,move.z),1);
                // De Unity a web: se invierte Z.
                intent.MoveX=dir.x;intent.MoveZ=-dir.y;
                intent.JumpPressed|=k.spaceKey.wasPressedThisFrame;intent.JumpHeld=k.spaceKey.isPressed;
                intent.SlidePressed|=SlidePressed(k,Settings.slideWithCtrl);intent.SlideHeld=SlideHeld(k,Settings.slideWithCtrl);
                interactPressed|=k.eKey.wasPressedThisFrame;
                if(mouse!=null&&Cursor.lockState==CursorLockMode.Locked)Look(mouse.delta.ReadValue());
            }
            Shader.SetGlobalFloat(SnapId,Snap?1:0);Shader.SetGlobalFloat(DitherId,Dither?1:0);
            ReadEvents();RefreshPanels();
            if(Session!=null)Audio.SetMode(CurrentMusicMode);
            textTimer-=Time.unscaledDeltaTime;
            if(textTimer<=0){textTimer=.1f;RefreshStatus();}
        }

        // Atajos de QA de U1–U3 (F1 resolución interna, F2 dithering, F9 vértices): cambian las
        // mismas opciones guardadas, nunca un valor activo distinto del guardado. F6 (ventana)
        // no es una opción. ConfigurePresentation queda para las herramientas (sin guardar).
        void PresentationShortcuts(Keyboard k)
        {
            foreach(var key in new[]{Key.F1,Key.F2,Key.F9,Key.F6})if(k[key].wasPressedThisFrame)PresentationShortcut(key);
        }
        public void PresentationShortcut(Key key)
        {
            switch(key){
                case Key.F1:ChangeSettings(s=>s.renderHeight=s.renderHeight==240?360:s.renderHeight==360?480:240);break;
                case Key.F2:ChangeSettings(s=>s.dithering=!s.dithering);break;
                case Key.F9:ChangeSettings(s=>s.vertexSnap=!s.vertexSnap);break;
                case Key.F6:UnityEngine.Screen.SetResolution(UnityEngine.Screen.width<2200?2560:1920,UnityEngine.Screen.width<2200?1440:1080,FullScreenMode.Windowed);break;
            }
        }
        // Deslizarse: Shift/C siempre; Ctrl solo con la opción activada (Input.setSlideWithCtrl).
        public static bool SlideHeld(Keyboard k,bool withCtrl)=>k.leftShiftKey.isPressed||k.cKey.isPressed||withCtrl&&(k.leftCtrlKey.isPressed||k.rightCtrlKey.isPressed);
        public static bool SlidePressed(Keyboard k,bool withCtrl)=>k.leftShiftKey.wasPressedThisFrame||k.cKey.wasPressedThisFrame||withCtrl&&(k.leftCtrlKey.wasPressedThisFrame||k.rightCtrlKey.wasPressedThisFrame);
        // CameraRig.look de la web: baseSensitivity (0,0022 rad = 0,126° por píxel) × sensibilidad.
        public const float LookDegreesPerPixel=.126f;
        public float Pitch=>pitch;
        public void Look(Vector2 delta)
        {
            float k=LookDegreesPerPixel*(float)Settings.mouseSensitivity;
            yaw+=delta.x*k;pitch=Mathf.Clamp(pitch-delta.y*k,-25,70);
        }

        // Panel F3 (Game.updateStats): rendimiento, jugador, semilla, entidades, partida y director.
        void RefreshStatus()
        {
            if(target==null||Session==null)return;
            string state=Body.Sliding?"deslizando":Body.Grounded?"suelo":Body.OnSteep?"resbalando":"aire";
            double slope=Math.Acos(Math.Min(1,Body.Normal.Y))*180/Math.PI;
            string calls=drawCalls.Valid&&drawCalls.LastValue>0?drawCalls.LastValue.ToString():"n/d",tris=triangles.Valid&&triangles.LastValue>0?triangles.LastValue.ToString():"n/d";
            status.text=$"{CombatText.Get("debug.title")} · U3 · semilla {Seed}\n"+
                $"{(1/Mathf.Max(Time.unscaledDeltaTime,.00001f)):F0} FPS · frame {Time.unscaledDeltaTime*1000:F1} ms · lógica {LogicMs:F2} ms · render {RenderMs:F2} ms · draw calls {calls} · triángulos {tris}\n"+
                $"{UnityEngine.Screen.width}×{UnityEngine.Screen.height} → {target.width}×{target.height} · x {Body.X:F1} · y {Body.Y:F1} · z {Body.Z:F1} · {Body.HorizontalSpeed:F1} m/s · {state} · {slope:F0}°\n"+
                $"Vida {Run.Hp:F0}/{Run.Stats[Stat.maxHp]:F0} · Nivel {Run.Level} · XP {Run.Xp:F0}/{Rules.XpNeeded(Run.Level)} · Oro {Run.Gold:F0} · Bajas {Run.Kills} · Enemigos {Run.Enemies.Count} · proyectiles {Run.Projectiles.Count} · gemas {Run.Gems.Count} · efectos {CombatView.EffectCount}\n"+
                $"Tiempo {Run.Time:F1} s · dificultad min {Session.Params.Minutes:F2} · ritmo {Session.Params.Rate:F1}/s · máx. {Session.Params.MaxAlive:F0} · frenado {Run.CrowdSlow:P0}{(Session.Swarm?" · ENJAMBRE":"")}\n"+
                $"{CombatText.Get("debug.keys")}\n{CombatText.Get("debug.keys2")}\n"+
                $"{(Run.Invincible?"Invencible · ":"")}{(Session.Cheated?"QA con trucos · ":"")}{State}{(Paused?" · en pausa":"")}";
        }

        // Texto de lo que se puede usar delante (baúl, tótem, armario), como promptText de la web.
        public string PromptText()
        {
            if(!Session.HasPrompt)return "";var p=Session.Prompt;
            return p.Kind=="chest"?CombatText.Format("prompt.chest","cost",p.Cost):p.Kind=="totem"?CombatText.Format("prompt.totem","s",Tuning.TotemDuration):CombatText.Get("prompt.portal");
        }

        void FixedUpdate()
        {
            if(Paused||World==null||State!=Screen.Playing)return;
            previousPosition=currentPosition;
            if(ScriptedIntent.HasValue)intent=ScriptedIntent.Value;
            logicWatch.Restart();
            Session.Step(intent,1.0/60,WebYaw,interactPressed);interactPressed=false;CombatView.Step(1.0/60);
            LogicMs=logicWatch.Elapsed.TotalMilliseconds;benchmark?.RecordTick(LogicMs);
            intent.JumpPressed=intent.SlidePressed=false;
            currentPosition=WebSpace.ToUnity(Body.X,Body.Y,Body.Z);
            if(Session.Finished){FinishRun();return;}
            if(Run.Choosing&&AutoChoose){while(Run.Choosing){Run.Choose(0);AutoChosen++;}}
            if(Run.Choosing){SetPaused(true);if(!Cards.Visible)Cards.Show(true);}
        }

        void LateUpdate()
        {
            // En el inicio la cámara gira despacio alrededor del punto de salida (TITLE_ORBIT_SPEED).
            if(State==Screen.Title&&!FreeCamera){yaw+=Time.unscaledDeltaTime*.12f*Mathf.Rad2Deg;Body.Facing=WebYaw;}
            var p=Vector3.Lerp(previousPosition,currentPosition,Paused?1:(Time.time-Time.fixedTime)/Time.fixedDeltaTime);
            avatar.position=p;
            // facing de la web: 0 = mirando hacia -Z web (= +Z de Unity).
            avatar.rotation=Quaternion.Euler(0,-(float)Body.Facing*Mathf.Rad2Deg,0);
            avatar.localScale=Body.Sliding?new Vector3(1,.6f,1.35f):Vector3.one;
            CombatView.Draw();
            if(FreeCamera){Renderer.FollowCamera(worldCamera);DrawEffects();return;}
            var pivot=p+Vector3.up*(Body.Sliding?1.2f:1.9f);
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var cameraPos=pivot-rotation*Vector3.forward*6.2f;
            // Como la web: la cámara solo choca con el terreno.
            if(World!=null)cameraPos.y=Mathf.Max(cameraPos.y,(float)World.Heightfield.HeightAt(cameraPos.x,-cameraPos.z)+.35f);
            worldCamera.transform.SetPositionAndRotation(cameraPos,Quaternion.LookRotation(pivot-cameraPos));
            Renderer.FollowCamera(worldCamera);DrawEffects();
        }
        // Partículas y números de daño (tras colocar la cámara: los números miran a ella). Solo se
        // mueven jugando; en pausa, cartas o resultados quedan quietos, como la web.
        void DrawEffects()
        {
            float effectsDt=State==Screen.Playing&&!Paused?Time.deltaTime:0;
            Particles.Update(effectsDt);ParticleView.Draw(Particles,worldCamera);
            Numbers.Update(effectsDt);NumberView.Draw(Numbers,worldCamera);
        }

        void Resize()
        {
            previousWidth=UnityEngine.Screen.width;previousHeight=UnityEngine.Screen.height;
            if(target!=null){worldCamera.targetTexture=null;target.Release();Destroy(target);}
            int width=Mathf.Max(1,Mathf.RoundToInt(InternalHeight*(float)Mathf.Max(1,UnityEngine.Screen.width)/Mathf.Max(1,UnityEngine.Screen.height)));
            target=new RenderTexture(width,InternalHeight,24,RenderTextureFormat.ARGB32){filterMode=FilterMode.Point,antiAliasing=1,name="U3 Retro"};
            target.Create();worldCamera.targetTexture=target;worldCamera.aspect=(float)width/InternalHeight;
            display.texture=target;display.GetComponent<AspectRatioFitter>().aspectRatio=worldCamera.aspect;
            Shader.SetGlobalVector(SizeId,new Vector4(width,InternalHeight,0,0));
        }

        public void ConfigurePresentation(int height,bool dither,bool snap)
        {
            if(height!=240&&height!=360&&height!=480)throw new ArgumentOutOfRangeException(nameof(height));
            InternalHeight=height;Dither=dither;Snap=snap;Resize();
        }

        void OnDestroy(){if(target!=null){target.Release();Destroy(target);}if(avatarMaterial)Destroy(avatarMaterial);if(CombatMaterial)Destroy(CombatMaterial);if(CombatMesh)Destroy(CombatMesh);ParticleView?.Destroy();NumberView?.Destroy();}
    }
}
