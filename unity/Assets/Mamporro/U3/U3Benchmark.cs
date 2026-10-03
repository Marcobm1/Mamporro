using System;
using System.Globalization;
using System.IO;
using System.Text;
using Mamporro.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mamporro.U3
{
    // Solo con -u3-benchmark: ensayo de una partida real de U3 (mapa MAMPORRO, Remedios,
    // 10 minutos, director activo). La simulación avanza sin medir hasta 10 s antes de cada
    // punto (minutos 2, 5 y 9 y enjambre final) y allí se miden 10 s de calentamiento y 30 s
    // con el juego completo dibujándose (mundo, combate y HUD). Mismo informe que U1/U2.
    public sealed class U3Benchmark : MonoBehaviour
    {
        public U3Game Game;
        const int Capacity=300000;const double Warmup=10,Duration=30;
        // Momento de partida (s) en que empieza cada medida; el enjambre, 40 s después de acabar el tiempo.
        static readonly string[] Names={"min2","min5","min9","enjambre"};
        static readonly double[] Starts={120,300,540,640};
        readonly double[] frames=new double[Capacity],ticks=new double[20000],cpu=new double[Capacity],gpu=new double[Capacity];
        readonly FrameTiming[] timing=new FrameTiming[1];
        ProfilerRecorder gc;long alloc;int gcSamples,n,nt,scenario,minEntities,maxEntities,maxProjectiles,maxEnemyShots,cards;
        double start,previous,first,measureFrom;long rendered;string output;bool captured,forwarding;
        string Stage=>Names[scenario];

        void Begin()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-u3-output");
            output=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"U3Benchmarks");Directory.CreateDirectory(output);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            Game.Character=Catalog.Characters[0];Game.Minutes=10;Game.LoadWorld("MAMPORRO");Game.StartRun("");
            // Invulnerabilidad de ensayo (como U2): sin ella la partida acabaría antes de medir.
            Game.Run.Invincible=true;Game.AutoChoose=true;Game.SetPaused(true);
            forwarding=true;scenario=0;Debug.Log("U3 benchmark: inicio");
        }
        void Start(){Begin();}

        // Circuito fijo de 8 s (2 s por lado) como U1/U2; giro de cámara 0 y cabeceo 20.
        void Route()
        {
            int side=(int)Math.Floor(Game.Run.Time/2)%4;
            Game.ScriptedIntent=new PlayerIntent{MoveX=side==0?1:side==2?-1:0,MoveZ=side==1?-1:side==3?1:0};
        }

        void Update()
        {
            if(!forwarding)return;
            // Avance sin medir: hasta 600 ticks por fotograma, con las mismas reglas que el juego.
            double target=Starts[scenario]-Warmup;var s=Game.Session;
            for(int k=0;k<600&&Game.Run.Time<target;k++){
                Route();s.Step(Game.ScriptedIntent.Value,1.0/60,0);Game.CombatView.Step(1.0/60);
                while(Game.Run.Choosing){Game.Run.Choose(0);cards++;}
                if(s.Finished)throw new InvalidOperationException("La partida de ensayo terminó antes de medir.");
            }
            if(Game.Run.Time<target)return;
            forwarding=false;Route();Game.SetPaused(false);n=nt=gcSamples=0;alloc=0;captured=false;
            minEntities=int.MaxValue;maxEntities=maxProjectiles=maxEnemyShots=0;measureFrom=Game.Run.Time;
            previous=start=Time.realtimeSinceStartupAsDouble;Debug.Log($"U3 benchmark: medida {Stage} desde t={measureFrom:F1} s");
        }
        // Tiempo de cada tick de lógica durante la medida (U3Game lo cronometra).
        public void RecordTick(double ms){if(!forwarding&&Time.realtimeSinceStartupAsDouble-start>=Warmup&&nt<ticks.Length)ticks[nt++]=ms;}

        void LateUpdate()
        {
            if(forwarding||Game==null)return;
            Route();
            double now=Time.realtimeSinceStartupAsDouble,elapsed=now-start;
            if(!captured&&elapsed>Warmup+3){captured=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,$"u3-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}-{Stage}.png"));}
            FrameTimingManager.CaptureFrameTimings();
            if(elapsed>=Warmup&&n<Capacity){
                if(n==0){first=previous;rendered=Game.RenderedFrames;}
                frames[n]=(now-previous)*1000;uint count=FrameTimingManager.GetLatestTimings(1,timing);
                cpu[n]=count>0&&timing[0].cpuFrameTime>0?timing[0].cpuFrameTime:-1;gpu[n]=count>0&&timing[0].gpuFrameTime>0?timing[0].gpuFrameTime:-1;
                if(gc.Valid){alloc+=gc.LastValue;gcSamples++;}n++;
                var r=Game.Run;minEntities=Math.Min(minEntities,r.Enemies.Count);maxEntities=Math.Max(maxEntities,r.Enemies.Count);
                maxProjectiles=Math.Max(maxProjectiles,r.Projectiles.Count);maxEnemyShots=Math.Max(maxEnemyShots,r.EnemyShots.Count);
            }
            previous=now;
            if(elapsed>=Warmup+Duration){
                Save(now);Debug.Log("U3 benchmark: fin "+Stage);scenario++;Game.SetPaused(true);
                if(scenario<Names.Length)forwarding=true;else{if(gc.Valid)gc.Dispose();Debug.Log("U3 benchmark: completo");Application.Quit(0);}
            }
        }

        static double Percentile(double[] sorted,double p)=>sorted.Length==0?-1:sorted[Math.Min(sorted.Length-1,Math.Max(0,(int)Math.Ceiling(sorted.Length*p)-1))];
        static double Mean(double[] values,int count,double max=double.MaxValue)
        {double sum=0;int k=0;for(int i=0;i<count;i++){double v=values[i];if(double.IsNaN(v)||double.IsInfinity(v)||v>max)return -1;if(v>0){sum+=v;k++;}}return k>0?sum/k:-1;}
        void Save(double now)
        {
            var sorted=new double[n];Array.Copy(frames,sorted,n);Array.Sort(sorted);var sortedTicks=new double[nt];Array.Copy(ticks,sortedTicks,nt);Array.Sort(sortedTicks);
            int over=0;foreach(double ms in sorted)if(ms>1000.0/60)over++;
            var s=Game.Session;var r=s.Combat;var target=Game.worldCamera.targetTexture;
            var report=new Report{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),ramMB=SystemInfo.systemMemorySize,
                development=Debug.isDebugBuild,editor=Application.isEditor,outputWidth=UnityEngine.Screen.width,outputHeight=UnityEngine.Screen.height,internalWidth=target.width,internalHeight=Game.InternalHeight,
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],vsync=QualitySettings.vSyncCount,fpsLimit=Application.targetFrameRate,
                scenario=Stage,runTimeFrom=measureFrom,runTimeTo=r.Time,difficultyMinutes=s.Params.Minutes,swarm=s.Swarm,maxAlive=s.Params.MaxAlive,spawnRate=s.Params.Rate,
                minEntities=minEntities,maxEntities=maxEntities,entitiesAtEnd=r.Enemies.Count,frames=n,ticks=nt,seconds=now-first,meanFps=n/(now-first),meanFrameMs=(now-first)*1000/n,
                p95=Percentile(sorted,.95),p99=Percentile(sorted,.99),max=Percentile(sorted,1),framesOverBudget=over,
                tickMean=Mean(ticks,nt),tickP95=Percentile(sortedTicks,.95),tickP99=Percentile(sortedTicks,.99),tickMax=Percentile(sortedTicks,1),
                cpuMean=Mean(cpu,n),gpuMean=Mean(gpu,n,(now-first)*1000),gcBytesPerFrame=gcSamples>0?(double)alloc/gcSamples:-1,
                memory=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),rendered=Game.RenderedFrames-rendered,
                projectiles=r.Projectiles.Count,enemyShots=r.EnemyShots.Count,maxProjectiles=maxProjectiles,maxEnemyShots=maxEnemyShots,gems=r.Gems.Count,coins=r.Coins.Count,
                kills=r.Kills,spawned=s.Spawned,effectsDropped=Game.CombatView.DroppedEffects,level=r.Level,cardsApplied=cards+Game.AutoChosen,gold=r.Gold,build=Build(r)};
            report.validRender=report.rendered>=n*.9&&n<Capacity&&nt<ticks.Length;
            string stem=$"u3-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}-{Stage}-{DateTime.UtcNow:yyyyMMddTHHmmssfff}";
            File.WriteAllText(Path.Combine(output,stem+".json"),JsonUtility.ToJson(report,true));
            var csv=new StringBuilder("frame,ms,cpu,gpu\n");
            for(int i=0;i<n;i++)csv.Append(i).Append(',').Append(frames[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(cpu[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(gpu[i].ToString("R",CultureInfo.InvariantCulture)).Append('\n');
            File.WriteAllText(Path.Combine(output,stem+".csv"),csv.ToString());
            csv.Clear().Append("tick,ms\n");for(int i=0;i<nt;i++)csv.Append(i).Append(',').Append(ticks[i].ToString("R",CultureInfo.InvariantCulture)).Append('\n');
            File.WriteAllText(Path.Combine(output,stem+"-ticks.csv"),csv.ToString());
            Debug.Log($"U3 benchmark {Stage}: {report.meanFps:F1} FPS, P95 {report.p95:F2} ms, enemigos {minEntities}-{maxEntities}, validRender {report.validRender}");
        }
        static string Build(CombatRun r)
        {
            var b=new StringBuilder();foreach(var w in r.Weapons)b.Append(w.Def.id).Append(" Nv").Append(w.Level).Append("; ");
            foreach(var t in r.Tomes)b.Append("tomo ").Append(t.Def.id).Append(" Nv").Append(t.Level).Append("; ");
            foreach(var i in r.Items)b.Append(i.Def.id).Append(" ×").Append(i.Count).Append("; ");return b.ToString();
        }
        void OnDestroy(){if(gc.Valid)gc.Dispose();}
        [Serializable]sealed class Report
        {
            public string unity,cpu,gpu,graphics,quality,scenario;
            public string backend="Mono",seed="MAMPORRO",character="remedios",runMinutes="10";
            public string conditions="Partida real con director: avance sin medir hasta 10 s antes del punto; 10 s calentamiento y 30 s de medida dibujando mundo, combate y HUD; invulnerable de ensayo; cartas: siempre la primera; circuito 8 s (2 s por lado); yaw 0 pitch 20; sin interactuables; sin guardado";
            public string unavailable="-1: no disponible/anómalo. Memoria Unity al final, no pico/VRAM. GPU se invalida si supera toda la ventana. GC release puede no existir.";
            public string build;public bool development,editor,validRender,swarm;
            public int ramMB,outputWidth,outputHeight,internalWidth,internalHeight,vsync,fpsLimit,minEntities,maxEntities,entitiesAtEnd,frames,ticks,framesOverBudget,projectiles,enemyShots,maxProjectiles,maxEnemyShots,gems,coins,kills,spawned,effectsDropped,level,cardsApplied;
            public double runTimeFrom,runTimeTo,difficultyMinutes,maxAlive,spawnRate,seconds,meanFps,meanFrameMs,p95,p99,max,tickMean,tickP95,tickP99,tickMax,cpuMean,gpuMean,gcBytesPerFrame,gold;
            public long memory,reserved,rendered;
        }
    }
}
