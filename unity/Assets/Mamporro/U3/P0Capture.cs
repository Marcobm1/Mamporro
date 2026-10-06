using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mamporro.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mamporro.U3
{
    // Instrumentación local del circuito, nunca FPS generales del juego.
    [DefaultExecutionOrder(1000)]
    public sealed class P0Capture : MonoBehaviour
    {
        const double Warmup=10,Duration=30;
        readonly double[] frames=new double[300000],ticks=new double[10000];
        readonly Sample[] route=new Sample[10000];
        static readonly int[] Loads={0,300,500,750};
        U3Game game;string output;bool running,benchmark,visualValid=true;int phase,lap,n,nt,nr,load,resumes,gcStart;
        double start,previous;long rendered,allocated;int gcSamples;ProfilerRecorder gc;
        long memoryAtEnd,reservedAtEnd,monoAtEnd;int collectionsAtEnd;
        Transform volume;Material volumeMaterial;
        struct Sample { public double t,x,y,z,cpu;public int state,phase,grabs,edges,stalls,rescues,falls;public string failure; }
        [Serializable] public sealed class Report
        {
            public bool valid,development,editor;
            public string source,unity,cpu,gpu,scenario="Circuito P0: movimiento y horda central, armas desactivadas, invulnerabilidad QA",gpuTime="N/D";
            public int count,frames,ticks,resumes,outputWidth,outputHeight,internalHeight,gcCollections,edges,failedEdges,stalls,rescues,falls,unintendedGrabs,unintendedRegrabs;
            public double warmup=Warmup,duration=Duration,p50,p95,p99,max,cpuTickP50,cpuTickP95,cpuTickP99,cpuTickMax,gcBytesPerFrame;
            public double routeSeconds,distance,ascent,descent,climbSeconds,descendSeconds;
            public long allocatedMemory,reservedMemory,monoMemory,renderedFrames;
        }
        [Serializable] sealed class Evidence { public bool valid;public int images;public string description="Fixtures independientes; secuencia de borde recorrida por física. No es prueba manual."; }
        static string Arg(string key,string fallback=null){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:fallback;}
        static bool Has(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        IEnumerator Start()
        {
            game=GetComponent<U3Game>();output=Arg("-p0-output");
            if(game.Vertical==null||string.IsNullOrEmpty(output)){Debug.LogError("P0 necesita QA y salida aislada");Application.Quit(2);yield break;}
            Directory.CreateDirectory(output);benchmark=Has("-p0-benchmark");
            yield return null;
            if(benchmark){
                gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
                foreach(int count in Loads){
                    Prepare(new Vec3(7.598,0,-2));game.Vertical.Spawn(count);load=count;
                    game.Vertical.InputProvider=RouteInput;game.FaceTowards(14,-2);
                    n=nt=nr=resumes=gcSamples=0;allocated=0;gcStart=GC.CollectionCount(0);rendered=game.RenderedFrames;
                    previous=start=Time.realtimeSinceStartupAsDouble;running=true;
                    while(Time.realtimeSinceStartupAsDouble-start<Warmup+Duration)yield return null;
                    running=false;memoryAtEnd=Profiler.GetTotalAllocatedMemoryLong();reservedAtEnd=Profiler.GetTotalReservedMemoryLong();monoAtEnd=GC.GetTotalMemory(false);collectionsAtEnd=GC.CollectionCount(0)-gcStart;
                    game.SetPaused(true);game.HideInterface=true;yield return Shot("load-"+count);SaveReport();
                }
                if(gc.Valid)gc.Dispose();Application.Quit(0);
            }else{yield return EvidenceSequence();Application.Quit(0);}
        }
        void Prepare(Vec3 at)
        {
            running=false;game.HideInterface=false;game.StartRun("P0QA");game.Run.Invincible=true;game.Run.WeaponsOff=true;
            game.PlaceQa(at);game.Body.Facing=-Math.PI/2;game.AutoChoose=true;game.SetPaused(false);
            phase=lap=0;game.Vertical.Motion.Step(default,1.0/60);
        }
        VerticalIntent RouteInput()
        {
            var b=game.Body;var m=game.Vertical.Motion;
            // Subir, bajar y subir al tejado; salir por el canto oeste, caer y volver.
            // Ningún reset/teleport dentro de esta ruta. El control decide por estado real.
            if(phase==0&&m.State==VerticalState.Climbing&&b.Y>=3.5)phase=1;
            if(phase==1&&b.Y<=1)phase=2;
            if(phase==2&&m.LedgeSuccess>lap)phase=3;
            if(phase==3&&b.X<=6.5)phase=4;
            if(phase==4&&b.Grounded){lap=m.LedgeSuccess;phase=0;}
            if(phase==3)return new VerticalIntent{Movement=new PlayerIntent{MoveX=-1}};
            if(phase==4)return default;
            return new VerticalIntent{GrabHeld=true,WallVertical=phase==1?-1:1,Movement=new PlayerIntent{MoveX=1}};
        }
        void Update()
        {
            if(running&&game.Paused){resumes++;game.SetPaused(false);}
        }
        public void RecordTick(double cpu)
        {
            if(!running)return;
            if(benchmark&&Time.realtimeSinceStartupAsDouble-start>=Warmup&&nt<ticks.Length)ticks[nt++]=cpu;
            if(nr>=route.Length)return;
            var b=game.Body;var q=game.Vertical;var m=q.Motion;
            route[nr++]=new Sample{t=q.Seconds,x=b.X,y=b.Y,z=b.Z,cpu=cpu,state=(int)m.State,phase=phase,
                grabs=m.Grabs,edges=m.LedgeSuccess,stalls=q.Stalls,rescues=q.Recoveries,falls=q.Falls,failure=m.LastFailure};
        }
        void LateUpdate()
        {
            if(volume)volume.position=new Vector3((float)game.Body.X,(float)game.Body.Y,-(float)game.Body.Z);
            if(!running||!benchmark)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now-start>=Warmup&&n<frames.Length){frames[n++]=(now-previous)*1000;if(gc.Valid){allocated+=gc.LastValue;gcSamples++;}}
            previous=now;
        }
        static double[] Sorted(double[] a,int count){var b=new double[count];Array.Copy(a,b,count);Array.Sort(b);return b;}
        static double P(double[] a,double p)=>a.Length==0?-1:a[Math.Min(a.Length-1,(int)Math.Ceiling(p*a.Length)-1)];
        void SaveReport()
        {
            var f=Sorted(frames,n);var t=Sorted(ticks,nt);var q=game.Vertical;var m=q.Motion;
            var report=new Report{valid=visualValid&&n>100&&nt>1500&&game.RenderedFrames-rendered>100&&game.Run.Enemies.Count==load&&!q.InvalidRoute&&m.LedgeSuccess>=3&&q.Stalls==0,
                development=Debug.isDebugBuild,editor=Application.isEditor,source=Arg("-p0-source","WIP"),unity=Application.unityVersion,
                cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,count=load,frames=n,ticks=nt,resumes=resumes,
                outputWidth=Screen.width,outputHeight=Screen.height,internalHeight=game.InternalHeight,p50=P(f,.5),p95=P(f,.95),p99=P(f,.99),max=P(f,1),
                cpuTickP50=P(t,.5),cpuTickP95=P(t,.95),cpuTickP99=P(t,.99),cpuTickMax=P(t,1),
                gcBytesPerFrame=Debug.isDebugBuild&&gcSamples>0?(double)allocated/gcSamples:-1,gcCollections=collectionsAtEnd,
                allocatedMemory=memoryAtEnd,reservedMemory=reservedAtEnd,monoMemory=monoAtEnd,
                renderedFrames=game.RenderedFrames-rendered,routeSeconds=q.Seconds,distance=q.Distance,ascent=q.Ascent,descent=q.Descent,
                climbSeconds=m.ClimbSeconds,descendSeconds=m.DescendSeconds,edges=m.LedgeSuccess,failedEdges=m.LedgeFailures,stalls=q.Stalls,rescues=q.Recoveries,
                falls=q.Falls,unintendedGrabs=q.UnintendedGrabs,unintendedRegrabs=q.UnintendedRegrabs};
            File.WriteAllText(Path.Combine(output,"load-"+load+".json"),JsonUtility.ToJson(report,true));
            var csv=new StringBuilder("frame,ms\n");for(int i=0;i<n;i++)csv.Append(i).Append(',').Append(frames[i].ToString("R",CultureInfo.InvariantCulture)).Append('\n');
            File.WriteAllText(Path.Combine(output,"frames-"+load+".csv"),csv.ToString());SaveRoute("route-"+load);
            Debug.Log("P0 load "+load+" valid="+report.valid+" edges="+report.edges+" stalls="+report.stalls);
        }
        void SaveRoute(string name)
        {
            var csv=new StringBuilder("t,x,y,z,state,phase,grabs,edges,stalls,rescues,falls,cpuTickMs,failure\n");
            for(int i=0;i<nr;i++){var s=route[i];csv.AppendFormat(CultureInfo.InvariantCulture,"{0:R},{1:R},{2:R},{3:R},{4},{5},{6},{7},{8},{9},{10},{11:R},{12}\n",s.t,s.x,s.y,s.z,s.state,s.phase,s.grabs,s.edges,s.stalls,s.rescues,s.falls,s.cpu,s.failure);}
            File.WriteAllText(Path.Combine(output,name+".csv"),csv.ToString());
        }
        int images;
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            var colors=new HashSet<uint>();var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y+=17)for(int x=0;x<texture.width;x+=17){var c=pixels[y*texture.width+x];colors.Add((uint)(c.r<<16|c.g<<8|c.b));}
            if(colors.Count<16){visualValid=false;Debug.LogWarning("P0 captura sin imagen útil: "+name);}
            File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);images++;
        }
        void Volume()
        {
            volume=new GameObject("QA envolvente corporal (no collider)").transform;
            volumeMaterial=new Material(game.worldShader);volumeMaterial.SetColor("_BaseColor",Color.magenta);
            void Edge(Vector3 a,Vector3 b){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(go.GetComponent<UnityEngine.Collider>());
                go.transform.SetParent(volume,false);go.transform.localPosition=(a+b)*.5f;go.transform.localScale=new Vector3(Mathf.Max(.025f,Mathf.Abs(a.x-b.x)),Mathf.Max(.025f,Mathf.Abs(a.y-b.y)),Mathf.Max(.025f,Mathf.Abs(a.z-b.z)));go.GetComponent<Renderer>().sharedMaterial=volumeMaterial;}
            foreach(float x in new[]{-.4f,.4f})foreach(float z in new[]{-.4f,.4f})Edge(new Vector3(x,0,z),new Vector3(x,1.55f,z));
            foreach(float y in new[]{0,1.55f})foreach(float v in new[]{-.4f,.4f}){Edge(new Vector3(-.4f,y,v),new Vector3(.4f,y,v));Edge(new Vector3(v,y,-.4f),new Vector3(v,y,.4f));}
        }
        IEnumerator EvidenceSequence()
        {
            Prepare(new Vec3(7.598,0,-2));Volume();game.FaceTowards(14,-2);nr=0;running=true;
            game.Vertical.InputProvider=RouteInput;yield return Shot("01-antes-borde");
            bool climb=false,edge=false,top=false;double deadline=Time.realtimeSinceStartupAsDouble+15;
            while(Time.realtimeSinceStartupAsDouble<deadline&&!top){
                var m=game.Vertical.Motion;
                if(!climb&&game.Body.Y>2){climb=true;yield return Shot("02-escalada-volumen");}
                if(!edge&&m.State==VerticalState.Ledge){edge=true;yield return Shot("03-transicion-borde");}
                if(m.LedgeSuccess>0){top=true;yield return Shot("04-sobre-tejado");}
                yield return null;
            }
            yield return new WaitForSeconds(2);yield return Shot("05-caida-tejado");running=false;SaveRoute("route-edge");
            game.SetPaused(true);game.HideInterface=true;game.FreeCamera=true;game.worldCamera.transform.SetPositionAndRotation(new Vector3(0,55,0),Quaternion.Euler(90,0,0));yield return null;yield return Shot("00-esquema-circuito");
            Prepare(new Vec3(7.598,0,0));game.FaceTowards(14,0);game.Vertical.InputProvider=()=>new VerticalIntent{GrabHeld=true,WallVertical=1,Movement=new PlayerIntent{MoveX=1}};
            nr=0;running=true;yield return new WaitForSeconds(3);yield return Shot("06-techo-bloqueado");
            bool blocked=game.Vertical.Motion.Stalls>0&&game.Vertical.Motion.LedgeSuccess==0;
            game.Vertical.Recover();RecordTick(0);yield return Shot("07-rescate-invalida");bool rescued=game.Vertical.InvalidRoute;running=false;SaveRoute("route-blocked");
            Prepare(new Vec3(7.598,1,12));game.FaceTowards(12,12);game.Vertical.InputProvider=()=>new VerticalIntent{GrabHeld=true,WallHorizontal=-1,Movement=new PlayerIntent{MoveX=1}};
            yield return new WaitForSeconds(2);yield return Shot("08-esquina");
            Prepare(new Vec3(11,6,3));game.Vertical.Spawn(300);game.FaceTowards(6,3);yield return new WaitForSeconds(2);yield return Shot("09-horda-desde-tejado");
            game.SetPaused(true);
            var report=new Evidence{valid=visualValid&&climb&&edge&&top&&blocked&&rescued,images=images};
            File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(report,true));
        }
        void OnDestroy(){if(gc.Valid)gc.Dispose();if(volume)Destroy(volume.gameObject);if(volumeMaterial)Destroy(volumeMaterial);}
    }
}
