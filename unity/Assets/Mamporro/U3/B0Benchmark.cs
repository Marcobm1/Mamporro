using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Mamporro.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Mamporro.U3
{
    // B0 (spike Blender), solo con -b0-benchmark: ensayo de presentación aislada sobre la escena
    // U6. Partida preparada y en pausa (sin ticks, director, RNG ni guardado de partida) con N
    // Pelusas inmóviles en una rejilla fija a altura conocida, cámara fija y la misma salida retro.
    // Mide el coste de dibujar la horda, no una partida: sus FPS no son los FPS de juego.
    // Después de U3Game: si una tecla reanuda la partida, se vuelve a pausar en el mismo
    // fotograma, antes de cualquier tick, y queda registrado.
    [DefaultExecutionOrder(1000)]
    public sealed class B0Benchmark : MonoBehaviour
    {
        const int Capacity=300000,Columns=30;const float Spacing=1.1f,Height=60;
        const double Warmup=10,Duration=30;
        static readonly int[] Loads={300,500,750};
        // A = representación U6 original (cajas); B estática instanciada; C poses discretas; D VAT.
        static readonly string[] Strategies={"base","static","poses","vat"};
        static bool refused;
        readonly double[] frames=new double[Capacity],cpu=new double[Capacity],gpu=new double[Capacity],submit=new double[Capacity];
        readonly long[] allocations=new long[Capacity],draws=new long[Capacity],batches=new long[Capacity],setPass=new long[Capacity],tris=new long[Capacity],verts=new long[Capacity];
        readonly int[] collections=new int[Capacity],instances=new int[Capacity];
        readonly double[] hordeCpu=new double[Capacity];
        IEnemyVisual visual;B0VisualLibrary library;PlayableGraph avatarGraph;bool withAvatar;
        readonly FrameTiming[] timings=new FrameTiming[1];
        U3Game game;ProfilerRecorder gc,drawCalls,batchCount,setPassCount,triangleCount,vertexCount,systemMemory;
        int count,n,lastGc,visible,inFrustum,run,repaused;string output,stamp,strategy,source;double started,previous;bool measuring;
        long renderedStart,memoryStart,monoStart,systemStart;int gcStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-b0-benchmark")<0)return;
            // Nunca el progreso personal: sin carpeta propia se usa una temporal y se aborta.
            if(Array.IndexOf(args,"-u4-save-dir")<0){refused=true;U3Game.SaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"MAMPORRO-B0-rechazado");}
            SceneManager.sceneLoaded+=Loaded;
        }
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=Loaded;
            if(refused){Debug.LogError("B0 requiere -u4-save-dir: nunca usar progreso personal.");Application.Quit(2);return;}
            var owner=FindAnyObjectByType<U3Game>();
            if(owner)owner.gameObject.AddComponent<B0Benchmark>();
            else{Debug.LogError("B0 necesita la escena U3.");Application.Quit(1);}
        }
        static string Arg(string key,string fallback=null)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        void Fail(string message){measuring=false;Debug.LogError("B0: "+message);Application.Quit(1);}

        IEnumerator Start()
        {
            game=GetComponent<U3Game>();
            if(!int.TryParse(Arg("-b0-count","300"),NumberStyles.Integer,CultureInfo.InvariantCulture,out count)||Array.IndexOf(Loads,count)<0){Fail("carga no admitida (300/500/750)");yield break;}
            strategy=Arg("-b0-strategy","base");if(Array.IndexOf(Strategies,strategy)<0){Fail("estrategia no admitida: "+strategy);yield break;}
            var dir=Arg("-b0-output");if(string.IsNullOrEmpty(dir)){Fail("falta -b0-output");yield break;}
            output=Path.GetFullPath(dir);Directory.CreateDirectory(output);
            run=int.TryParse(Arg("-b0-run","1"),NumberStyles.Integer,CultureInfo.InvariantCulture,out int r)?r:1;source=Arg("-b0-source","sin-especificar");
            withAvatar=Array.IndexOf(Environment.GetCommandLineArgs(),"-b0-avatar")>=0;
            stamp=$"{strategy}{(withAvatar?"+avatar":"")}-{count}-{Screen.width}x{Screen.height}-r{run}-{DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff",CultureInfo.InvariantCulture)}";
            yield return null;
            // B/C/D y el avatar necesitan la escena QA aditiva; A sigue sin cargarla (igual que B0.1).
            if(strategy!="base"||withAvatar){
                SceneManager.LoadScene(B0VisualLibrary.SceneName,LoadSceneMode.Additive);yield return null;
                library=FindAnyObjectByType<B0VisualLibrary>();if(!library){Fail("falta la escena B0_QA en la build");yield break;}
            }
            // Semilla por defecto; StartRun no escribe el progreso. Pausa: ningún tick de lógica.
            game.StartRun("MAMPORRO");game.SetPaused(true);game.HideInterface=true;game.FreeCamera=true;
            game.ConfigurePresentation(360,true,true);game.Audio.SetFocused(true);
            var enemies=game.Run.Enemies;enemies.Clear();
            // Misma separación para todas las cargas: más carga = más filas, igual tamaño en pantalla.
            for(int i=0;i<count;i++)enemies.Spawn(0,(i%Columns-(Columns-1)*.5f)*Spacing,Height,(i/Columns-12)*Spacing);
            enemies.Rebuild();
            if(strategy!="base"){visual=B0HordeFactory.Create(strategy,library);game.CombatView.EnemyVisual=visual;}
            if(withAvatar)PlaceAvatar();
            var cam=game.worldCamera;cam.transform.position=new Vector3(0,88,-32);cam.transform.LookAt(new Vector3(0,Height,0));cam.fieldOfView=58;
            yield return null;yield return new WaitForEndOfFrame();
            // Todas las entidades dentro del frustum (nada fuera de cámara) …
            var planes=GeometryUtility.CalculateFrustumPlanes(cam);var def=Catalog.Enemies[0];
            for(int i=0;i<enemies.Count;i++)
                if(GeometryUtility.TestPlanesAABB(planes,new Bounds(new Vector3(enemies.X[i],enemies.Y[i]+(float)def.height*.5f,-enemies.Z[i]),new Vector3((float)def.radius*1.7f,(float)def.height,(float)def.radius*1.5f))))inFrustum++;
            if(inFrustum!=count){Fail($"entidades fuera de cámara: {inFrustum}/{count}");yield break;}
            // … y prueba real de píxeles: la horda cambia la imagen, no basta con contar llamadas.
            var on=ReadPixels();game.CombatView.enabled=false;
            yield return null;yield return new WaitForEndOfFrame();
            var off=ReadPixels();game.CombatView.enabled=true;
            for(int i=0;i<on.Length;i++)if(!on[i].Equals(off[i]))visible++;
            if(visible<100){Fail("render invisible: "+visible);yield break;}
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,stamp+".png"));yield return null;
            // Primeros planos para comparar calidad (una pasada): fila 0, presente en todas las cargas,
            // vista desde delante (+Z); seis fotogramas a ~0,1 s con la animación corriendo.
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-b0-closeups")>=0){
                var savedPosition=cam.transform.position;var savedRotation=cam.transform.rotation;
                cam.transform.position=new Vector3(0,61.6f,17.2f);cam.transform.LookAt(new Vector3(0,60.45f,13.2f));
                for(int k=0;k<6;k++){yield return new WaitForSecondsRealtime(.1f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,$"{stamp}-cerca-{k}.png"));}
                yield return null;cam.transform.SetPositionAndRotation(savedPosition,savedRotation);
            }
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            systemMemory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"System Used Memory");
            drawCalls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            batchCount=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
            setPassCount=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            triangleCount=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
            vertexCount=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Vertices Count");
            previous=started=Time.realtimeSinceStartupAsDouble;lastGc=GC.CollectionCount(0);measuring=true;
        }
        Color32[] ReadPixels()
        {
            var target=game.worldCamera.targetTexture;var old=RenderTexture.active;RenderTexture.active=target;
            var copy=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{copy.ReadPixels(new Rect(0,0,target.width,target.height),0,0);copy.Apply();return copy.GetPixels32();}
            finally{RenderTexture.active=old;Destroy(copy);}
        }
        // Avatar con rig (opcional, -b0-avatar): Remedios andando delante de la horda, de cara a la cámara.
        void PlaceAvatar()
        {
            var model=Instantiate(library.remedios);model.transform.SetPositionAndRotation(new Vector3(0,Height,-15),Quaternion.Euler(0,180,0));
            foreach(var r in model.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=library.retro;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            var animator=model.GetComponent<Animator>();animator.applyRootMotion=false;
            avatarGraph=PlayableGraph.Create("B0 avatar");avatarGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput.Create(avatarGraph,"Animación",animator).SetSourcePlayable(AnimationClipPlayable.Create(avatarGraph,library.remediosWalk));
        }
        void Update()
        {
            // Reloj de presentación: la animación corre aunque la lógica esté en pausa.
            if(visual!=null)visual.Clock+=Time.deltaTime;
            if(avatarGraph.IsValid())avatarGraph.Evaluate(Time.deltaTime);
            if(game==null||game.Paused)return;
            var k=Keyboard.current;repaused++;
            Debug.LogWarning($"B0: reanudación externa anulada (Esc {k!=null&&k.escapeKey.wasPressedThisFrame}, pantalla {game.State}, tiempo {game.Run.Time})");
            game.SetPaused(true);
        }
        static long Value(ProfilerRecorder recorder)=>recorder.Valid?recorder.LastValue:-1;
        void LateUpdate()
        {
            if(!measuring)return;
            double now=Time.realtimeSinceStartupAsDouble,elapsed=now-started;
            FrameTimingManager.CaptureFrameTimings();
            if(elapsed>=Warmup){
                if(n==0){renderedStart=game.RenderedFrames;memoryStart=Profiler.GetTotalAllocatedMemoryLong();monoStart=GC.GetTotalMemory(false);systemStart=Value(systemMemory);gcStart=GC.CollectionCount(0);}
                if(n>=Capacity){Fail("capacidad de muestras excedida");return;}
                frames[n]=(now-previous)*1000;
                uint available=FrameTimingManager.GetLatestTimings(1,timings);
                cpu[n]=available>0&&timings[0].cpuFrameTime>0?timings[0].cpuFrameTime:-1;
                gpu[n]=available>0&&timings[0].gpuFrameTime>0?timings[0].gpuFrameTime:-1;
                submit[n]=game.RenderMs;instances[n]=game.CombatView.DrawnInstances+(visual?.Instances??0);hordeCpu[n]=visual?.DrawMs??-1;
                allocations[n]=Value(gc);draws[n]=Value(drawCalls);batches[n]=Value(batchCount);setPass[n]=Value(setPassCount);tris[n]=Value(triangleCount);verts[n]=Value(vertexCount);
                int currentGc=GC.CollectionCount(0);collections[n]=currentGc-lastGc;lastGc=currentGc;n++;
                if(game.Run.Enemies.Count!=count||game.Run.Time!=0){Fail($"estado lógico inesperado: enemigos {game.Run.Enemies.Count}, tiempo {game.Run.Time}, pausa {game.Paused}, pantalla {game.State}, fotograma {n}");return;}
            }else lastGc=GC.CollectionCount(0);
            previous=now;
            if(elapsed>=Warmup+Duration){measuring=false;Save();Application.Quit(0);}
        }
        // Media de los valores válidos (> 0); -1 si ninguno lo es.
        static double Mean(double[] values,int length){double sum=0;int k=0;for(int i=0;i<length;i++)if(values[i]>0){sum+=values[i];k++;}return k>0?sum/k:-1;}
        static double Mean(long[] values,int length){double sum=0;int k=0;for(int i=0;i<length;i++)if(values[i]>=0){sum+=values[i];k++;}return k>0?sum/k:-1;}
        static int Valid(double[] values,int length){int k=0;for(int i=0;i<length;i++)if(values[i]>0)k++;return k;}
        static long Max(long[] values,int length){long m=-1;for(int i=0;i<length;i++)m=Math.Max(m,values[i]);return m;}
        void Save()
        {
            var sorted=new double[n];Array.Copy(frames,sorted,n);Array.Sort(sorted);double sum=0;for(int i=0;i<n;i++)sum+=frames[i];
            var cpuSorted=new double[n];Array.Copy(cpu,cpuSorted,n);Array.Sort(cpuSorted);int cpuValid=Valid(cpu,n);
            var submitSorted=new double[n];Array.Copy(submit,submitSorted,n);Array.Sort(submitSorted);
            int over=0;foreach(double ms in sorted)if(ms>1000.0/60)over++;
            int minInstances=int.MaxValue,maxInstances=0;for(int i=0;i<n;i++){minInstances=Math.Min(minInstances,instances[i]);maxInstances=Math.Max(maxInstances,instances[i]);}
            var target=game.worldCamera.targetTexture;
            var hordeSorted=new double[n];Array.Copy(hordeCpu,hordeSorted,n);Array.Sort(hordeSorted);
            long Memory(UnityEngine.Object o)=>o?Profiler.GetRuntimeMemorySizeLong(o):0;
            long poseBytes=0;if(library&&strategy=="poses")foreach(var m in library.pelusaPoses)poseBytes+=Memory(m);
            var report=new Report{strategy=strategy,avatar=withAvatar,hordeInstances=visual?.Instances??0,
                hordeCpuMean=visual!=null?Mean(hordeCpu,n):-1,hordeCpuP95=visual!=null?Percentile(hordeSorted,.95):-1,
                vatTextureBytes=strategy=="vat"?Memory(library.pelusaVat):0,poseMeshBytes=poseBytes,pelusaMeshBytes=library?Memory(library.pelusa):0,
                pelusaVertices=library?library.pelusa.vertexCount:0,pelusaTriangles=library?(int)(library.pelusa.GetIndexCount(0)/3):0,entities=count,entitiesInFrustum=inFrustum,run=run,source=source,frames=n,seconds=sum/1000,
                p50=Percentile(sorted,.5),p95=Percentile(sorted,.95),p99=Percentile(sorted,.99),max=sorted[n-1],mean=sum/n,meanFps=n/(sum/1000),framesOver16_67=over,
                cpuFrameMean=Mean(cpu,n),cpuFrameP95=cpuValid==n?Percentile(cpuSorted,.95):-1,cpuFrameValidFrames=cpuValid,
                submitCpuMean=Mean(submit,n),submitCpuP95=Percentile(submitSorted,.95),gpuRawMean=Mean(gpu,n),gpuRawValidFrames=Valid(gpu,n),
                externalUnpauses=repaused,instancesMin=minInstances,instancesMax=maxInstances,changedPixels=visible,
                drawCallsMean=Mean(draws,n),batchesMean=Mean(batches,n),setPassMean=Mean(setPass,n),trianglesMean=Mean(tris,n),verticesMean=Mean(verts,n),
                drawCallsValid=drawCalls.Valid&&Max(draws,n)>0,batchesValid=batchCount.Valid&&Max(batches,n)>0,setPassValid=setPassCount.Valid&&Max(setPass,n)>0,trianglesValid=triangleCount.Valid&&Max(tris,n)>0,verticesValid=vertexCount.Valid&&Max(verts,n)>0,
                gcAllocValid=gc.Valid&&Debug.isDebugBuild,gcAllocMeanBytes=gc.Valid&&Debug.isDebugBuild?Mean(allocations,n):-1,gcAllocMaxBytes=gc.Valid&&Debug.isDebugBuild?Max(allocations,n):-1,gcCollections=GC.CollectionCount(0)-gcStart,
                memoryStart=memoryStart,memoryEnd=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),monoHeapStart=monoStart,monoHeapEnd=GC.GetTotalMemory(false),
                systemMemoryStart=systemStart,systemMemoryEnd=Value(systemMemory),
                outputWidth=Screen.width,outputHeight=Screen.height,internalWidth=target.width,internalHeight=game.InternalHeight,dither=game.Dither,snap=game.Snap,
                rendered=game.RenderedFrames-renderedStart,
                unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),ramMB=SystemInfo.systemMemorySize,
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],development=Debug.isDebugBuild,editor=Application.isEditor,vsync=QualitySettings.vSyncCount,fpsLimit=Application.targetFrameRate,
                fullScreenMode=Screen.fullScreenMode.ToString(),warmup=Warmup};
            report.validRender=report.rendered>=n*.9&&inFrustum==count&&visible>=100;
            File.WriteAllText(Path.Combine(output,stamp+".json"),JsonUtility.ToJson(report,true));
            var csv=new StringBuilder("frame,ms,cpuFrameMs,gpuRawMs,worldCameraSubmitCpuMs,combatInstances,hordeVisualCpuMs,gcAllocatedBytes,gen0Collections,drawCalls,batches,setPassCalls,triangles,vertices\n");
            for(int i=0;i<n;i++)csv.Append(i).Append(',').Append(frames[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(cpu[i].ToString("R",CultureInfo.InvariantCulture))
                .Append(',').Append(gpu[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(submit[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(instances[i])
                .Append(',').Append(hordeCpu[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(Debug.isDebugBuild?allocations[i]:-1).Append(',').Append(collections[i]).Append(',').Append(draws[i]).Append(',').Append(batches[i]).Append(',').Append(setPass[i]).Append(',').Append(tris[i]).Append(',').Append(verts[i]).Append('\n');
            File.WriteAllText(Path.Combine(output,stamp+".csv"),csv.ToString());
            Debug.Log($"B0 completado {stamp}: p50 {report.p50:F3} ms, p95 {report.p95:F3} ms, p99 {report.p99:F3} ms, máx {report.max:F3} ms, validRender {report.validRender}");
        }
        public static double Percentile(double[] sorted,double p)=>sorted.Length==0?-1:sorted[Math.Max(0,Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*p)-1))];
        void OnDestroy()
        {
            foreach(var recorder in new[]{gc,drawCalls,batchCount,setPassCount,triangleCount,vertexCount,systemMemory})if(recorder.Valid)recorder.Dispose();
            if(avatarGraph.IsValid())avatarGraph.Destroy();
        }
        [Serializable]sealed class Report
        {
            public string strategy,source,unity,cpu,gpu,graphics,quality,fullScreenMode;
            public string conditions="Presentación aislada (B0.5: A cajas U6, B Pelusa estática instanciada, C 8 poses discretas instanciadas, D VAT de 20 fotogramas; mismo clip Pelusa_Walk y reloj de presentación con la lógica en pausa): escena U6, mapa MAMPORRO, partida en pausa (sin ticks, director, RNG ni guardado de partida), HUD oculto, cámara fija (0,88,-32)→(0,60,0) FOV 58, Pelusas inmóviles a 60 m en rejilla de 30 columnas separadas 1,1 m, interna 360 con dithering y vértices ajustados, VSync 0, sin límite de FPS, 10 s de calentamiento + 30 s de medida, guardado propio del ensayo";
            public string notes="ms = intervalo real entre fotogramas. cpuFrame = FrameTimingManager (CPU principal). worldCameraSubmitCpu = CPU de envío de la cámara del mundo, no GPU. gpuRaw = FrameTimingManager sin validar: no se presenta como tiempo GPU. Contadores de render = totales del fotograma (todas las cámaras). -1 = no disponible. GC por fotograma solo en Development.";
            public bool avatar;public int hordeInstances,pelusaVertices,pelusaTriangles;public double hordeCpuMean,hordeCpuP95;public long vatTextureBytes,poseMeshBytes,pelusaMeshBytes;
            public int entities,entitiesInFrustum,run,externalUnpauses,frames,framesOver16_67,cpuFrameValidFrames,gpuRawValidFrames,instancesMin,instancesMax,changedPixels,gcCollections,ramMB,outputWidth,outputHeight,internalWidth,internalHeight,vsync,fpsLimit;
            public bool validRender,development,editor,dither,snap,drawCallsValid,batchesValid,setPassValid,trianglesValid,verticesValid,gcAllocValid;
            public double seconds,p50,p95,p99,max,mean,meanFps,cpuFrameMean,cpuFrameP95,submitCpuMean,submitCpuP95,gpuRawMean,drawCallsMean,batchesMean,setPassMean,trianglesMean,verticesMean,gcAllocMeanBytes,warmup;
            public long gcAllocMaxBytes,memoryStart,memoryEnd,reserved,monoHeapStart,monoHeapEnd,systemMemoryStart,systemMemoryEnd,rendered;
            public string seed="MAMPORRO",enemy="pelusa (tipo 0)",backend="Mono";
        }
    }
}
