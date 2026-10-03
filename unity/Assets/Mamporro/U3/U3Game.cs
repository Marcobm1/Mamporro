using System;
using Mamporro.Core;
using Mamporro.U2;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mamporro.U3
{
    // Controlador de la escena U3: mundo real, física del jugador de la web y la cámara,
    // la entrada y la salida retro de U1 (textura interna, dithering y ajuste de vértices).
    // La lógica trabaja en coordenadas web; aquí se convierte a Unity (Z invertida).
    public sealed class U3Game : MonoBehaviour,ICombatEffects
    {
        public Camera worldCamera;
        public RawImage display;
        public Text status,help;
        public Shader worldShader,skyShader;
        // Referencia serializada: conserva la variante de instancing en la build.
        public Material combatTemplate;
        public string defaultSeed="MAMPORRO";

        public WorldRenderer Renderer {get;private set;}
        public WorldData World=>Renderer?Renderer.World:null;
        public string Seed {get;private set;}
        public WorldRun Session {get;private set;}
        public CombatRun Run=>Session.Combat;
        public PlayerBody Body=>Session.Body;
        public RunRenderer CombatView {get;private set;}
        public RunCards Cards {get;private set;}
        public Material CombatMaterial {get;private set;}
        public Mesh CombatMesh {get;private set;}
        public int InternalHeight {get;private set;}=360;
        public bool Dither {get;private set;}=true;
        public bool Snap {get;private set;}=true;
        public bool Paused {get;private set;}=true;
        public long RenderedFrames {get;private set;}
        // Giro de cámara en convenio web (0 = mirando hacia -Z de la web).
        public double WebYaw=>-yaw*Mathf.Deg2Rad;
        // Cámara libre (comprobación visual): LateUpdate no la mueve.
        public bool FreeCamera;
        public bool DebugVisible {get;private set;}

        Transform avatar;
        Material avatarMaterial;
        RenderTexture target;
        PlayerIntent intent;
        float yaw,pitch=20,textTimer;
        int previousWidth,previousHeight;
        Vector3 previousPosition,currentPosition;
        static readonly int SnapId=Shader.PropertyToID("_RetroSnap"),DitherId=Shader.PropertyToID("_RetroDither"),SizeId=Shader.PropertyToID("_RetroSize");

        void Awake()
        {
            Application.runInBackground=true;
            QualitySettings.SetQualityLevel(0);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            Time.fixedDeltaTime=1f/60;Time.maximumDeltaTime=.1f;
            Renderer=gameObject.AddComponent<WorldRenderer>();Renderer.worldShader=worldShader;Renderer.skyShader=skyShader;
            avatar=BuildAvatar();
            CombatText.Load();
            if(!combatTemplate||!combatTemplate.enableInstancing)throw new InvalidOperationException("Falta el material instanciado de combate U3.");
            CombatMaterial=new Material(combatTemplate){name="Combate U3",enableInstancing=true};
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);
            CombatMesh=Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);Destroy(primitive);
            var colors=new Color[CombatMesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;CombatMesh.colors=colors;
            CombatView=gameObject.AddComponent<RunRenderer>();CombatView.Initialize(this);
            Cards=gameObject.AddComponent<RunCards>();Cards.Session=this;Cards.Build();
            LoadWorld(defaultSeed);
            SetPaused(true);
            UpdateHelp();
        }

        void Start(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-u3-visual-check")>=0)gameObject.AddComponent<U3VisualCheck>().Game=this;}

        void OnEnable(){RenderPipelineManager.endCameraRendering+=OnCameraRendered;}
        void OnDisable(){RenderPipelineManager.endCameraRendering-=OnCameraRendered;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnCameraRendered(ScriptableRenderContext context,Camera camera){if(camera==worldCamera)RenderedFrames++;}

        public void LoadWorld(string seed)
        {
            Seed=seed;Renderer.Build(WorldData.Generate(seed),seed);
            Session=new WorldRun(World,seed,Catalog.Characters[0],this);
            FreeCamera=false;DebugVisible=false;
            ResetPlayer();CombatView.Clear();Cards.Hide();SetPaused(true);UpdateHelp();
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
            if(!paused&&(Run.Choosing||Session.Over))return;
            Paused=paused;intent=default;
            Cursor.lockState=paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=paused;
            RefreshStatus();
        }
        void OnApplicationFocus(bool focus){if(!focus)SetPaused(true);}

        public void Emit(CombatEffect effect)=>CombatView.Emit(effect);
        public void Choose(int index){if(Run.Choose(index))AfterChoice(true);}
        public void Skip(){if(Run.Skip())AfterChoice(true);}
        public void Reroll(){if(Run.Reroll())AfterChoice(false);}
        public void Banish(int index){if(Run.Banish(index))AfterChoice(false);}
        void AfterChoice(bool fresh)
        {if(Run.Choosing)Cards.Show(fresh);else{Cards.Hide();SetPaused(false);}}

        void UpdateHelp()
        {
            help.text="WASD · moverse | Ratón · cámara | Espacio · salto | Mayús/C · deslizarse\nEsc · pausa | Clic · continuar | F8 · reiniciar | F3 · QA | F1 · resolución | F2 · dither | F9 · vértices | F6 · ventana"+
                (DebugVisible?"\nDepuración: 1 invencible · 2 nivel · 4 +100 enemigos · 5 matar todos · 6 jefe · 7 +100 oro\n3 y 8 pendientes. Las cartas tienen prioridad.":"\nDirector activo: 10 minutos y enjambre final.");
        }
        // Acciones explícitas de QA; abrir el panel no marca trucos.
        public void QaAction(int action)
        {
            if(Run.Choosing||Session.Over)return;
            if(action!=1&&action!=2&&action!=4&&action!=5&&action!=6&&action!=7)return;
            switch(action){
                case 1:Session.DebugToggleInvincible();break;
                case 2:Session.DebugLevelUp();break;
                case 4:Session.DebugSpawn(100);break;
                case 5:Session.DebugKillAll();break;
                case 6:Session.DebugSummonBoss();break;
                case 7:Session.DebugAddGold(100);break;
            }
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
            if(target==null||previousWidth!=Screen.width||previousHeight!=Screen.height)Resize();
            var k=Keyboard.current;var mouse=Mouse.current;
            if(k!=null){
                if(k.escapeKey.wasPressedThisFrame&&!Cards.Visible)SetPaused(!Paused);
                if(k.f8Key.wasPressedThisFrame)LoadWorld(Seed);
                if(k.f3Key.wasPressedThisFrame){DebugVisible=!DebugVisible;UpdateHelp();}
                if(DebugVisible&&!Cards.Visible){
                    if(k.digit1Key.wasPressedThisFrame)QaAction(1);
                    else if(k.digit2Key.wasPressedThisFrame)QaAction(2);
                    else if(k.digit4Key.wasPressedThisFrame)QaAction(4);
                    else if(k.digit5Key.wasPressedThisFrame)QaAction(5);
                    else if(k.digit6Key.wasPressedThisFrame)QaAction(6);
                    else if(k.digit7Key.wasPressedThisFrame)QaAction(7);
                }
                if(k.f1Key.wasPressedThisFrame)ConfigurePresentation(InternalHeight==240?360:InternalHeight==360?480:240,Dither,Snap);
                if(k.f2Key.wasPressedThisFrame)Dither=!Dither;
                if(k.f9Key.wasPressedThisFrame)Snap=!Snap;
                if(k.f6Key.wasPressedThisFrame)Screen.SetResolution(Screen.width<2200?2560:1920,Screen.width<2200?1440:1080,FullScreenMode.Windowed);
            }
            if(Paused&&!Cards.Visible&&mouse!=null&&mouse.leftButton.wasPressedThisFrame)SetPaused(false);
            if(!Paused&&k!=null){
                var axis=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                var move=Quaternion.Euler(0,yaw,0)*new Vector3(axis.x,0,axis.y);
                var dir=Vector2.ClampMagnitude(new Vector2(move.x,move.z),1);
                // De Unity a web: se invierte Z.
                intent.MoveX=dir.x;intent.MoveZ=-dir.y;
                intent.JumpPressed|=k.spaceKey.wasPressedThisFrame;intent.JumpHeld=k.spaceKey.isPressed;
                intent.SlidePressed|=k.leftShiftKey.wasPressedThisFrame||k.cKey.wasPressedThisFrame;intent.SlideHeld=k.leftShiftKey.isPressed||k.cKey.isPressed;
                if(mouse!=null&&Cursor.lockState==CursorLockMode.Locked){var d=mouse.delta.ReadValue();yaw+=d.x*.126f;pitch=Mathf.Clamp(pitch-d.y*.126f,-25,70);}
            }
            Shader.SetGlobalFloat(SnapId,Snap?1:0);Shader.SetGlobalFloat(DitherId,Dither?1:0);
            textTimer-=Time.unscaledDeltaTime;
            if(textTimer<=0){textTimer=.25f;RefreshStatus();}
        }

        void RefreshStatus()
        {
            if(target==null||Session==null)return;
            status.text=$"MAMPORRO · U3 · semilla {Seed}\n{Screen.width}×{Screen.height} → {target.width}×{target.height} · {(1/Mathf.Max(Time.unscaledDeltaTime,.00001f)):F0} FPS\n"+
                    $"x {Body.X:F1} · y {Body.Y:F1} · z {Body.Z:F1} · {Body.HorizontalSpeed:F1} m/s{(Body.Sliding?" · deslizando":"")}{(Body.OnSteep?" · pendiente":"")}\n"+
                    $"Vida {Run.Hp:F0}/{Run.Stats[Stat.maxHp]:F0} · Nivel {Run.Level} · Oro {Run.Gold:F0} · Bajas {Run.Kills} · Enemigos {Run.Enemies.Count}\n"+
                    $"Tiempo {(Session.Swarm?"+":"")}{Math.Abs(Session.TimeLeft):F0} s{(Session.Swarm?" · ENJAMBRE":"")} · ritmo {Session.Params.Rate:F1}/s · tope {Session.Params.MaxAlive:F0}\n"+
                    $"Frenado {Run.CrowdSlow:P0} · {(Run.Invincible?"Invencible · ":"")}{(Session.Cheated?"QA con trucos · ":"")}{(Run.Dead?"Derrota: F8 reinicia":Run.Victory?"Victoria: F8 reinicia":Paused?"En pausa":"Combate")}";
        }

        void FixedUpdate()
        {
            if(Paused||World==null)return;
            previousPosition=currentPosition;
            if(ScriptedIntent.HasValue)intent=ScriptedIntent.Value;
            Session.Step(intent,1.0/60,WebYaw);CombatView.Step(1.0/60);
            intent.JumpPressed=intent.SlidePressed=false;
            currentPosition=WebSpace.ToUnity(Body.X,Body.Y,Body.Z);
            if(Run.Choosing){SetPaused(true);if(!Cards.Visible)Cards.Show(true);}
            if(Session.Over)SetPaused(true);
        }

        void LateUpdate()
        {
            var p=Vector3.Lerp(previousPosition,currentPosition,Paused?1:(Time.time-Time.fixedTime)/Time.fixedDeltaTime);
            avatar.position=p;
            // facing de la web: 0 = mirando hacia -Z web (= +Z de Unity).
            avatar.rotation=Quaternion.Euler(0,-(float)Body.Facing*Mathf.Rad2Deg,0);
            avatar.localScale=Body.Sliding?new Vector3(1,.6f,1.35f):Vector3.one;
            CombatView.Draw();
            if(FreeCamera){Renderer.FollowCamera(worldCamera);return;}
            var pivot=p+Vector3.up*(Body.Sliding?1.2f:1.9f);
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var cameraPos=pivot-rotation*Vector3.forward*6.2f;
            // Como la web: la cámara solo choca con el terreno.
            if(World!=null)cameraPos.y=Mathf.Max(cameraPos.y,(float)World.Heightfield.HeightAt(cameraPos.x,-cameraPos.z)+.35f);
            worldCamera.transform.SetPositionAndRotation(cameraPos,Quaternion.LookRotation(pivot-cameraPos));
            Renderer.FollowCamera(worldCamera);
        }

        void Resize()
        {
            previousWidth=Screen.width;previousHeight=Screen.height;
            if(target!=null){worldCamera.targetTexture=null;target.Release();Destroy(target);}
            int width=Mathf.Max(1,Mathf.RoundToInt(InternalHeight*(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height)));
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

        void OnDestroy(){if(target!=null){target.Release();Destroy(target);}if(avatarMaterial)Destroy(avatarMaterial);if(CombatMaterial)Destroy(CombatMaterial);if(CombatMesh)Destroy(CombatMesh);}
    }
}
