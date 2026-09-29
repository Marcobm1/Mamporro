using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Mamporro.U1
{
    public sealed class PrototypeController : MonoBehaviour
    {
        public PrototypeSettings settings;
        public Camera worldCamera;
        public Transform avatar;
        public Mesh enemyMesh;
        public Material enemyMaterial;
        public RawImage display;
        public Text status;
        public Text help;
        public PlayerMotor Motor { get; private set; }
        public HordeSimulation Horde { get; private set; }
        public int InternalHeight { get; private set; }
        public bool Dither { get; private set; } = true;
        public bool Snap { get; private set; } = true;
        public bool Paused { get; private set; } = true;
        public long RenderedFrames { get; private set; }
        public LocalBenchmark Benchmark { get; private set; }
        readonly Matrix4x4[] matrices = new Matrix4x4[1023];
        RenderTexture target;
        MoveIntent intent;
        float yaw, pitch=20, textTimer;
        int previousWidth, previousHeight;
        Vector3 previousPosition;
        static readonly int SnapId=Shader.PropertyToID("_RetroSnap"), DitherId=Shader.PropertyToID("_RetroDither"), SizeId=Shader.PropertyToID("_RetroSize");

        void Awake()
        {
            Application.runInBackground=true;
            QualitySettings.SetQualityLevel(0);
            QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
            Time.fixedDeltaTime=1f/60; Time.maximumDeltaTime=.1f;
            Motor=new PlayerMotor(settings);
            Horde=new HordeSimulation(4096); Horde.Reset(settings.initialEnemies,settings.seed);
            InternalHeight=settings.internalHeight;
            Benchmark=gameObject.AddComponent<LocalBenchmark>(); Benchmark.controller=this;
            previousPosition=Motor.Position;
            help.text=TechnicalText.Controls;
            SetPaused(true);
        }
        void Start() { Benchmark.ReadCommandLine(); }
        void OnEnable() { RenderPipelineManager.endCameraRendering+=OnCameraRendered; }
        void OnCameraRendered(ScriptableRenderContext context, Camera camera) { if(camera==worldCamera) RenderedFrames++; }
        public void ResetTrial(int count)
        {
            Motor.Reset(); Horde.Reset(count,settings.seed); previousPosition=Motor.Position;
            yaw=0; pitch=20; intent=default;
        }
        public void SetPaused(bool paused)
        {
            Paused=paused; intent=default;
            Cursor.lockState=paused || (Benchmark && Benchmark.Active) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible=Cursor.lockState!=CursorLockMode.Locked;
        }
        void OnApplicationFocus(bool focus) { if (!focus && !(Benchmark && Benchmark.Active)) SetPaused(true); }
        void OnDisable() { RenderPipelineManager.endCameraRendering-=OnCameraRendered; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        void Update()
        {
            if (target==null || previousWidth!=Screen.width || previousHeight!=Screen.height) Resize();
            var k=Keyboard.current; var mouse=Mouse.current;
            if (k!=null && k.escapeKey.wasPressedThisFrame)
            {
                if(Benchmark.Active) Benchmark.Cancel();
                else SetPaused(!Paused);
            }
            if (!Benchmark.Active && k!=null)
            {
                if(k.f1Key.wasPressedThisFrame) ConfigurePresentation(InternalHeight==240 ? 360 : InternalHeight==360 ? 480 : 240,Dither,Snap);
                if(k.f2Key.wasPressedThisFrame) Dither=!Dither;
                if(k.f3Key.wasPressedThisFrame) Snap=!Snap;
                if(k.f6Key.wasPressedThisFrame) Screen.SetResolution(Screen.width<2200 ? 2560 : 1920,Screen.width<2200 ? 1440 : 1080,FullScreenMode.Windowed);
                if(k.rKey.wasPressedThisFrame) ResetTrial(Horde.Count);
                if(k.digit1Key.wasPressedThisFrame) ResetTrial(300);
                if(k.digit2Key.wasPressedThisFrame) ResetTrial(500);
                if(k.digit3Key.wasPressedThisFrame) ResetTrial(750);
                if(k.digit4Key.wasPressedThisFrame) ResetTrial(1000);
                if(k.f5Key.wasPressedThisFrame) Benchmark.Begin(false);
            }
            if(Paused && mouse!=null && mouse.leftButton.wasPressedThisFrame && !Benchmark.Active) SetPaused(false);
            if(!Paused && !Benchmark.Active && k!=null)
            {
                var axis=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                var move=Quaternion.Euler(0,yaw,0)*new Vector3(axis.x,0,axis.y);
                intent.direction=Vector2.ClampMagnitude(new Vector2(move.x,move.z),1);
                intent.jumpPressed |= k.spaceKey.wasPressedThisFrame; intent.jumpHeld=k.spaceKey.isPressed;
                intent.slidePressed |= k.leftShiftKey.wasPressedThisFrame || k.cKey.wasPressedThisFrame;
                intent.slideHeld=k.leftShiftKey.isPressed || k.cKey.isPressed;
                if(mouse!=null && Cursor.lockState==CursorLockMode.Locked)
                { var d=mouse.delta.ReadValue(); yaw+=d.x*.126f; pitch=Mathf.Clamp(pitch-d.y*.126f,-25,70); }
            }
            Shader.SetGlobalFloat(SnapId,Snap?1:0); Shader.SetGlobalFloat(DitherId,Dither?1:0);
            textTimer-=Time.unscaledDeltaTime;
            if(textTimer<=0)
            {
                textTimer=.25f;
                status.text=$"{TechnicalText.Title}\n{(Benchmark.Active?TechnicalText.Measuring:Paused?TechnicalText.Paused:TechnicalText.Running)}\n{Screen.width}×{Screen.height} → {target.width}×{target.height} · {Horde.Count} entidades · {(1/Mathf.Max(Time.unscaledDeltaTime,.00001f)):F0} FPS\nDither {(Dither?"ON":"OFF")} · Snap {(Snap?"ON":"OFF")} · Mono · VSync 0";
            }
        }
        void FixedUpdate()
        {
            if(Paused) return;
            previousPosition=Motor.Position;
            if(Benchmark.Active) intent=Benchmark.ScriptedIntent();
            Motor.Step(intent,Time.fixedDeltaTime);
            intent.jumpPressed=intent.slidePressed=false;
            Horde.Step(Motor.Position,settings.enemySpeed,Time.fixedDeltaTime);
        }
        void LateUpdate()
        {
            var p=Vector3.Lerp(previousPosition,Motor.Position,Paused?1:(Time.time-Time.fixedTime)/Time.fixedDeltaTime);
            avatar.position=p;
            var flat=new Vector3(Motor.Velocity.x,0,Motor.Velocity.z);
            if(flat.sqrMagnitude>.1f) avatar.rotation=Quaternion.LookRotation(flat);
            avatar.localScale=Motor.Sliding?new Vector3(1,.6f,1.35f):Vector3.one;
            var pivot=p+Vector3.up*(Motor.Sliding?1.2f:1.9f);
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var cameraPos=pivot-rotation*Vector3.forward*6.2f;
            cameraPos.y=Mathf.Max(cameraPos.y,TechnicalWorld.Height(cameraPos.x,cameraPos.z)+.35f);
            worldCamera.transform.SetPositionAndRotation(cameraPos,Quaternion.LookRotation(pivot-cameraPos));
            for(int offset=0;offset<Horde.Count;offset+=matrices.Length)
            {
                int count=Mathf.Min(matrices.Length,Horde.Count-offset);
                for(int i=0;i<count;i++) matrices[i]=Matrix4x4.TRS(Horde.Positions[offset+i]+Vector3.up*.55f,Quaternion.identity,new Vector3(.8f,1.1f,.8f));
                var rp=new RenderParams(enemyMaterial) { camera=worldCamera, shadowCastingMode=ShadowCastingMode.Off, receiveShadows=false, worldBounds=new Bounds(Vector3.zero,new Vector3(110,30,110)) };
                Graphics.RenderMeshInstanced(rp,enemyMesh,0,matrices,count);
            }
        }
        void Resize()
        {
            previousWidth=Screen.width; previousHeight=Screen.height;
            if(target!=null) { worldCamera.targetTexture=null; target.Release(); Destroy(target); }
            int width=Mathf.Max(1,Mathf.RoundToInt(InternalHeight*(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height)));
            target=new RenderTexture(width,InternalHeight,24,RenderTextureFormat.ARGB32) { filterMode=FilterMode.Point, antiAliasing=1, name="U1 Retro" };
            target.Create(); worldCamera.targetTexture=target; worldCamera.aspect=(float)width/InternalHeight;
            display.texture=target;
            display.GetComponent<AspectRatioFitter>().aspectRatio=worldCamera.aspect;
            Shader.SetGlobalVector(SizeId,new Vector4(width,InternalHeight,0,0));
        }
        public void ConfigurePresentation(int height,bool dither,bool snap)
        {
            if(height!=240 && height!=360 && height!=480) throw new System.ArgumentOutOfRangeException(nameof(height));
            InternalHeight=height; Dither=dither; Snap=snap; Resize();
        }
        void OnDestroy() { if(target!=null) { target.Release(); Destroy(target); } }
    }
}
