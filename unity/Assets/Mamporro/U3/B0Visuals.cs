using System;
using Mamporro.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mamporro.U3
{
    // B0 (spike Blender): integración visual aislada, solo QA. Dirección
    // lógica/collider/pool → VisualRoot → modelo: estas clases leen el estado del núcleo y
    // nunca lo escriben. Los assets viven en la escena aditiva B0_QA (solo en la build B0).

    // Referencias de la escena B0_QA: así los assets B0 solo entran en la build QA.
    public sealed class B0VisualLibrary : MonoBehaviour
    {
        public const string SceneName="B0_QA";
        public Mesh pelusa;
        public GameObject remedios,wall;
        public AnimationClip remediosWalk,remediosIdle,pelusaWalk;
        public Material retro,retroFlash;
        // B0.5, horneado en el Editor desde Pelusa_Walk: poses discretas (C) y VAT (D).
        public Mesh[] pelusaPoses;
        public Texture2D pelusaVat;
        public Material vat,vatFlash;
        public float clipSeconds;
    }

    // Representación alternativa de la horda: dibuja los enemigos de los tipos que acepta a partir
    // de los arrays lógicos (sin GameObject ni estado por enemigo). RunRenderer deja de dibujarlos.
    public interface IEnemyVisual
    {
        bool Draws(int type);
        void Draw(Enemies enemies,float alpha,bool flashes,Camera camera);
        int Instances {get;}
        // Reloj de presentación de la animación (s); se detiene en pausa. Sin efecto en B.
        double Clock {get;set;}
        // CPU del último Draw (preparar matrices y encolar), ms.
        double DrawMs {get;}
    }

    // Base común de las estrategias B/C/D: recorre los arrays lógicos y encola lotes de 1023.
    public abstract class B0Horde : IEnemyVisual
    {
        protected static readonly Bounds World=new Bounds(new Vector3(0,80,0),new Vector3(340,200,340));
        readonly System.Diagnostics.Stopwatch watch=new System.Diagnostics.Stopwatch();
        public int Instances {get;protected set;}
        public double Clock {get;set;}
        public double DrawMs {get;private set;}
        public bool Draws(int type)=>type==0;
        // Fase fija por enemigo derivada de su Id (no del RNG de juego): distinta y estable.
        public static float Phase(uint id)=>(float)((id*0.6180339887)%1.0);
        // Posición interpolada (web → Unity: z invertida) y rumbo web → giro Unity −heading.
        public static Matrix4x4 Matrix(Enemies e,int i,float alpha)
            =>Matrix4x4.TRS(new Vector3(Mathf.Lerp(e.Px[i],e.X[i],alpha),Mathf.Lerp(e.Py[i],e.Y[i],alpha),-Mathf.Lerp(e.Pz[i],e.Z[i],alpha)),Quaternion.Euler(0,-e.Heading[i]*Mathf.Rad2Deg,0),Vector3.one);
        // Destello al recibir un golpe, como RunRenderer (umbral 0,3, opción «Destellos»).
        public void Draw(Enemies e,float alpha,bool flashes,Camera camera)
        {
            watch.Restart();Instances=0;Begin();
            for(int i=0;i<e.Count;i++)if(e.Type[i]==0)Add(Matrix(e,i,alpha),Phase(e.Id[i]),flashes&&e.Flash[i]>.3f,camera);
            Flush(camera);DrawMs=watch.Elapsed.TotalMilliseconds;
        }
        protected abstract void Begin();
        protected abstract void Add(Matrix4x4 m,float phase,bool flash,Camera camera);
        protected abstract void Flush(Camera camera);
        protected void Submit(Mesh mesh,Material material,Matrix4x4[] batch,int count,Camera camera,MaterialPropertyBlock props=null)
        {
            if(count==0)return;
            var rp=new RenderParams(material){camera=camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=World,matProps=props};
            Graphics.RenderMeshInstanced(rp,mesh,0,batch,count);Instances+=count;
        }
    }

    // B: malla estática instanciada (bind pose de la Pelusa B0), sin animación.
    public sealed class B0StaticHorde : B0Horde
    {
        readonly Mesh mesh;readonly Material material,flash;
        readonly Matrix4x4[] normal=new Matrix4x4[1023],flashing=new Matrix4x4[1023];
        int n,f;
        public B0StaticHorde(Mesh mesh,Material material,Material flash){this.mesh=mesh;this.material=material;this.flash=flash;}
        protected override void Begin(){n=f=0;}
        protected override void Add(Matrix4x4 m,float phase,bool isFlash,Camera camera)
        {
            if(isFlash){flashing[f++]=m;if(f==1023){Submit(mesh,flash,flashing,f,camera);f=0;}}
            else{normal[n++]=m;if(n==1023){Submit(mesh,material,normal,n,camera);n=0;}}
        }
        protected override void Flush(Camera camera){Submit(mesh,material,normal,n,camera);Submit(mesh,flash,flashing,f,camera);n=f=0;}
    }

    // C: poses discretas instanciadas. Cada Pelusa usa la pose de su reloj+fase; un lote por
    // pose (y otro por pose para el destello). Sin interpolación entre poses.
    public sealed class B0PoseHorde : B0Horde
    {
        readonly Mesh[] poses;readonly Material material,flash;readonly float seconds;
        readonly Matrix4x4[][] batches;readonly int[] counts;
        public B0PoseHorde(Mesh[] poses,Material material,Material flash,float seconds)
        {
            this.poses=poses;this.material=material;this.flash=flash;this.seconds=seconds;
            batches=new Matrix4x4[poses.Length*2][];for(int k=0;k<batches.Length;k++)batches[k]=new Matrix4x4[1023];counts=new int[batches.Length];
        }
        protected override void Begin(){Array.Clear(counts,0,counts.Length);}
        protected override void Add(Matrix4x4 m,float phase,bool isFlash,Camera camera)
        {
            double t=(Clock/seconds+phase)%1.0;int pose=(int)(t*poses.Length)%poses.Length,slot=pose+(isFlash?poses.Length:0);
            batches[slot][counts[slot]++]=m;
            if(counts[slot]==1023){Submit(poses[pose],isFlash?flash:material,batches[slot],1023,camera);counts[slot]=0;}
        }
        protected override void Flush(Camera camera)
        {for(int k=0;k<batches.Length;k++){Submit(poses[k%poses.Length],k<poses.Length?material:flash,batches[k],counts[k],camera);counts[k]=0;}}
    }

    // D: vertex animation en textura (shader Mamporro/RetroWorldVAT): misma malla instanciada,
    // fase por instancia en un array del bloque de propiedades; el GPU interpola fotogramas.
    public sealed class B0VatHorde : B0Horde
    {
        static readonly int TimeId=Shader.PropertyToID("_B0AnimTime"),PhaseId=Shader.PropertyToID("_Phase");
        readonly Mesh mesh;readonly Material material,flash;
        readonly Matrix4x4[] normal=new Matrix4x4[1023],flashing=new Matrix4x4[1023];
        readonly float[] normalPhase=new float[1023],flashPhase=new float[1023];
        readonly MaterialPropertyBlock normalProps=new MaterialPropertyBlock(),flashProps=new MaterialPropertyBlock();
        int n,f;
        public B0VatHorde(Mesh mesh,Material material,Material flash){this.mesh=mesh;this.material=material;this.flash=flash;}
        protected override void Begin(){n=f=0;Shader.SetGlobalFloat(TimeId,(float)Clock);}
        protected override void Add(Matrix4x4 m,float phase,bool isFlash,Camera camera)
        {
            if(isFlash){flashing[f]=m;flashPhase[f++]=phase;if(f==1023){Send(flash,flashing,flashPhase,flashProps,f,camera);f=0;}}
            else{normal[n]=m;normalPhase[n++]=phase;if(n==1023){Send(material,normal,normalPhase,normalProps,n,camera);n=0;}}
        }
        protected override void Flush(Camera camera){Send(material,normal,normalPhase,normalProps,n,camera);Send(flash,flashing,flashPhase,flashProps,f,camera);n=f=0;}
        void Send(Material m,Matrix4x4[] batch,float[] phases,MaterialPropertyBlock props,int count,Camera camera)
        {if(count==0)return;props.SetFloatArray(PhaseId,phases);Submit(mesh,m,batch,count,camera,props);}
    }

    public static class B0HordeFactory
    {
        public static readonly string[] Strategies={"static","poses","vat"};
        public static B0Horde Create(string strategy,B0VisualLibrary l)=>strategy switch{
            "static"=>new B0StaticHorde(l.pelusa,l.retro,l.retroFlash),
            "poses"=>new B0PoseHorde(l.pelusaPoses,l.retro,l.retroFlash,l.clipSeconds),
            "vat"=>new B0VatHorde(l.pelusa,l.vat,l.vatFlash),
            _=>throw new ArgumentException("Estrategia de horda B0 desconocida: "+strategy)};
    }

    // VisualRoot del jugador (Remedios con rig y clips) y de la horda, sobre el U3Game existente.
    // Se ejecuta después de U3Game: copia el transform ya interpolado del avatar provisional, que
    // deja de dibujarse. La animación avanza con el tiempo de presentación (quieta en pausa) y
    // nunca mueve la lógica: sin root motion, sin eventos.
    [DefaultExecutionOrder(1000)]
    public sealed class B0VisualAdapter : MonoBehaviour
    {
        public U3Game Game;public B0VisualLibrary Library;
        public Transform VisualRoot {get;private set;}
        public Animator Animator {get;private set;}
        public B0Horde Horde {get;private set;}
        public double AnimationTime {get;private set;}
        public float WalkWeight {get;private set;}
        Renderer[] avatarRenderers;PlayableGraph graph;AnimationMixerPlayable mixer;WorldRun session;

        static bool refused;
        // -b0-visual (build QA B0): carga B0_QA y activa el visual. Exige guardado propio
        // (-u4-save-dir): una partida QA nunca liquida en el progreso personal.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-b0-visual")<0)return;
            if(Array.IndexOf(args,"-u4-save-dir")<0){refused=true;U3Game.SaveDirectoryOverride=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"MAMPORRO-B0-rechazado");}
            SceneManager.sceneLoaded+=Loaded;
        }
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(refused){SceneManager.sceneLoaded-=Loaded;Debug.LogError("B0 visual requiere -u4-save-dir: nunca usar progreso personal.");Application.Quit(2);return;}
            if(scene.name==B0VisualLibrary.SceneName){
                SceneManager.sceneLoaded-=Loaded;var game=UnityEngine.Object.FindAnyObjectByType<U3Game>();var adapter=Attach(game,UnityEngine.Object.FindAnyObjectByType<B0VisualLibrary>());
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-b0-visual-check")>=0){var check=game.gameObject.AddComponent<B0VisualCheck>();check.Game=game;check.Adapter=adapter;}
                return;
            }
            if(UnityEngine.Object.FindAnyObjectByType<U3Game>())SceneManager.LoadScene(B0VisualLibrary.SceneName,LoadSceneMode.Additive);
        }
        public static B0VisualAdapter Attach(U3Game game,B0VisualLibrary library,string horde=null)
        {
            if(!game||!library)throw new InvalidOperationException("B0: falta U3Game o la biblioteca B0_QA");
            if(horde==null){var args=Environment.GetCommandLineArgs();int h=Array.IndexOf(args,"-b0-horde");horde=h>=0&&h+1<args.Length?args[h+1]:"static";}
            var adapter=game.gameObject.AddComponent<B0VisualAdapter>();adapter.Game=game;adapter.Library=library;adapter.Enable(horde);
            return adapter;
        }

        // -b0-horde static|poses|vat elige la estrategia del visual (por defecto, la estática).
        void Enable(string horde)
        {
            avatarRenderers=Game.AvatarRoot.GetComponentsInChildren<Renderer>(true);
            VisualRoot=new GameObject("B0 VisualRoot jugador").transform;VisualRoot.SetParent(transform,false);
            var model=Instantiate(Library.remedios,VisualRoot,false);model.name="B0_Remedios";
            foreach(var r in model.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=Library.retro;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            Animator=model.GetComponent<Animator>();Animator.applyRootMotion=false;Animator.runtimeAnimatorController=null;
            Horde=B0HordeFactory.Create(horde,Library);
            PlaceWalls();
            enabled=true;OnEnable();
        }
        void OnEnable()
        {
            if(VisualRoot==null)return;
            foreach(var r in avatarRenderers)r.enabled=false;
            VisualRoot.gameObject.SetActive(true);walls.gameObject.SetActive(true);Game.CombatView.EnemyVisual=Horde;
            graph=PlayableGraph.Create("B0 Remedios");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,2);
            graph.Connect(AnimationClipPlayable.Create(graph,Library.remediosIdle),0,mixer,0);
            graph.Connect(AnimationClipPlayable.Create(graph,Library.remediosWalk),0,mixer,1);
            AnimationPlayableOutput.Create(graph,"Animación",Animator).SetSourcePlayable(mixer);
            ResetPose();
        }
        void OnDisable()
        {
            if(VisualRoot==null)return;
            foreach(var r in avatarRenderers)if(r)r.enabled=true;
            if(Game&&Game.CombatView&&Game.CombatView.EnemyVisual==Horde)Game.CombatView.EnemyVisual=null;
            if(graph.IsValid())graph.Destroy();
            VisualRoot.gameObject.SetActive(false);walls.gameObject.SetActive(false);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();if(VisualRoot)Destroy(VisualRoot.gameObject);if(walls)Destroy(walls.gameObject);}

        // Prop modular de QA: dos módulos seguidos 10 m delante del inicio (web z = −10), cada uno
        // apoyado en el terreno de su centro. Solo visual: sin collider (los colliders no cambian en B0).
        Transform walls;
        public Transform Walls=>walls;
        void PlaceWalls()
        {
            walls=new GameObject("B0 muro (solo visual)").transform;walls.SetParent(transform,false);
            var hf=Game.World.Heightfield;
            for(int k=0;k<2;k++){
                double x=-4+4*k,z=-10;float y=(float)Math.Min(hf.HeightAt(x,z),hf.HeightAt(x+4,z));
                var m=Instantiate(Library.wall,walls,false);m.name="B0_ParedModulo "+k;
                m.transform.position=new Vector3((float)x,y,(float)-z);
                foreach(var r in m.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=Library.retro;r.shadowCastingMode=ShadowCastingMode.Off;}
            }
        }

        // Partida nueva: clips desde el principio (sin poses residuales de la anterior).
        void ResetPose()
        {
            session=Game.Session;AnimationTime=0;WalkWeight=0;Horde.Clock=0;
            for(int k=0;k<2;k++)mixer.GetInput(k).SetTime(0);
            mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);graph.Evaluate(0);
        }

        void LateUpdate()
        {
            if(Game.Session!=session)ResetPose();
            var avatar=Game.AvatarRoot;
            VisualRoot.SetPositionAndRotation(avatar.position,avatar.rotation);VisualRoot.localScale=avatar.localScale;
            // Parpadeo de invulnerabilidad del avatar (activo/inactivo) también en el modelo.
            bool visible=avatar.gameObject.activeSelf;if(VisualRoot.gameObject.activeSelf!=visible)VisualRoot.gameObject.SetActive(visible);
            float dt=Game.Paused||Game.State!=U3Game.Screen.Playing?0:Time.deltaTime;
            float speed=(float)Game.Body.HorizontalSpeed,walk=Mathf.Clamp01(speed/1.5f);
            WalkWeight=Mathf.MoveTowards(WalkWeight,walk,dt*6);
            mixer.SetInputWeight(0,1-WalkWeight);mixer.SetInputWeight(1,WalkWeight);
            // Paso proporcional a la velocidad lógica (la base de andar del clip es ~3 m/s).
            mixer.GetInput(1).SetSpeed(Mathf.Max(.3f,speed/3f));
            AnimationTime+=dt;graph.Evaluate(dt);Horde.Clock+=dt;
        }
    }
}
