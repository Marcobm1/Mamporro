using System;
using System.IO;
using System.Globalization;
using System.Text;
using Mamporro.U1;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

namespace Mamporro.U2
{
    public sealed class CombatBenchmark : MonoBehaviour
    {
        public CombatSession Session;public bool Active {get;private set;}
        const int Capacity=300000;const double Warmup=10,Duration=30;
        readonly double[] frames=new double[Capacity],ticks=new double[10000],cpu=new double[Capacity],gpu=new double[Capacity];
        readonly int[] loads={300,500,750,1000};readonly FrameTiming[] timing=new FrameTiming[1];
        ProfilerRecorder gc;long alloc;int gcSamples,n,nt,scenario,routeTick,minEntities,maxEntities;
        double start,previous,first;long rendered;string output;bool captured;
        public void ReadCommandLine()
        {
            bool requested=false;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++){if(args[i]=="-u2-benchmark")requested=true;if(args[i]=="-u2-output"&&i+1<args.Length)output=args[++i];}
            if(!requested)return;
            if(string.IsNullOrEmpty(output))output=Path.Combine(Application.dataPath,"../U2Benchmarks");Directory.CreateDirectory(output);
            Active=true;gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");Next();
        }
        void Next()
        {
            Debug.Log("U2 benchmark: inicio carga "+loads[scenario]);
            Session.TargetEnemies=loads[scenario];Session.Restart(0);Session.Run.QaWeapons("chancla","naftalina","dentaduras","fregona");Session.Run.Invincible=true;
            // QA: selección automática desactivada en el ensayo; las gemas y XP sí se procesan.
            Session.View.SetPaused(false);n=nt=routeTick=gcSamples=0;alloc=0;captured=false;minEntities=int.MaxValue;maxEntities=0;
            previous=start=Time.realtimeSinceStartupAsDouble;
        }
        public MoveIntent ScriptedIntent()
        {int segment=(routeTick++/120)%4;return new MoveIntent{direction=segment==0?Vector2.right:segment==1?Vector2.up:segment==2?Vector2.left:Vector2.down};}
        public void RecordTick(double ms)
        {if(Active&&Time.realtimeSinceStartupAsDouble-start>=Warmup&&nt<ticks.Length)ticks[nt++]=ms;}
        void LateUpdate()
        {
            if(!Active)return;double now=Time.realtimeSinceStartupAsDouble,elapsed=now-start;
            if(!captured&&elapsed>3){captured=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,$"u2-{Screen.width}x{Screen.height}-{loads[scenario]}.png"));}
            FrameTimingManager.CaptureFrameTimings();
            if(elapsed>=Warmup&&n<Capacity){if(n==0){first=previous;rendered=Session.View.RenderedFrames;}frames[n]=(now-previous)*1000;
                uint count=FrameTimingManager.GetLatestTimings(1,timing);cpu[n]=count>0&&timing[0].cpuFrameTime>0?timing[0].cpuFrameTime:-1;gpu[n]=count>0&&timing[0].gpuFrameTime>0?timing[0].gpuFrameTime:-1;
                if(gc.Valid){alloc+=gc.LastValue;gcSamples++;}n++;
                minEntities=Math.Min(minEntities,Session.Run.Enemies.Count);maxEntities=Math.Max(maxEntities,Session.Run.Enemies.Count);
            }
            previous=now;if(elapsed>=Warmup+Duration){Save(now);Debug.Log("U2 benchmark: fin carga "+loads[scenario]);scenario++;if(scenario<loads.Length)Next();else{Active=false;if(gc.Valid)gc.Dispose();Application.Quit(0);}}
        }
        static double Percentile(double[] sorted,double p)=>sorted.Length==0?-1:sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*p)-1)];
        static double Mean(double[] values,int count,double max=double.MaxValue)
        {double sum=0;int n=0;for(int i=0;i<count;i++){double v=values[i];if(double.IsNaN(v)||double.IsInfinity(v)||v>max)return -1;if(v>0){sum+=v;n++;}}return n>0?sum/n:-1;}
        void Save(double now)
        {
            long memory=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong();
            var sorted=new double[n];Array.Copy(frames,sorted,n);Array.Sort(sorted);var sortedTicks=new double[nt];Array.Copy(ticks,sortedTicks,nt);Array.Sort(sortedTicks);
            int over=0;foreach(double ms in sorted)if(ms>1000.0/60)over++;
            var v=Session.View;var report=new Report{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),ramMB=SystemInfo.systemMemorySize,development=Debug.isDebugBuild,editor=Application.isEditor,
                outputWidth=Screen.width,outputHeight=Screen.height,internalWidth=v.worldCamera.targetTexture.width,internalHeight=v.InternalHeight,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],vsync=QualitySettings.vSyncCount,fpsLimit=Application.targetFrameRate,
                entities=loads[scenario],minEntities=minEntities,maxEntities=maxEntities,frames=n,ticks=nt,seconds=now-first,meanFps=n/(now-first),meanFrameMs=(now-first)*1000/n,p95=Percentile(sorted,.95),p99=Percentile(sorted,.99),max=Percentile(sorted,1),framesOverBudget=over,
                tickMean=Mean(ticks,nt),tickP95=Percentile(sortedTicks,.95),tickP99=Percentile(sortedTicks,.99),tickMax=Percentile(sortedTicks,1),cpuMean=Mean(cpu,n),gpuMean=Mean(gpu,n,(now-first)*1000),gcBytesPerFrame=gcSamples>0?(double)alloc/gcSamples:-1,memory=memory,reserved=reserved,rendered=v.RenderedFrames-rendered,
                projectiles=Session.Run.Projectiles.Count,enemyShots=Session.Run.EnemyShots.Count,gems=Session.Run.Gems.Count,coins=Session.Run.Coins.Count,kills=Session.Run.Kills,effectsDropped=Session.Presenter.DroppedEffects};
            report.validRender=report.rendered>=n*.9&&n<Capacity&&nt<ticks.Length;
            string stem=$"u2-{Screen.width}x{Screen.height}-{loads[scenario]}-{DateTime.UtcNow:yyyyMMddTHHmmssfff}";
            File.WriteAllText(Path.Combine(output,stem+".json"),JsonUtility.ToJson(report,true));var csv=new StringBuilder("frame,ms,cpu,gpu\n");
            for(int i=0;i<n;i++)csv.Append(i).Append(',').Append(frames[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(cpu[i].ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(gpu[i].ToString("R",CultureInfo.InvariantCulture)).Append('\n');File.WriteAllText(Path.Combine(output,stem+".csv"),csv.ToString());
            csv.Clear().Append("tick,ms\n");for(int i=0;i<nt;i++)csv.Append(i).Append(',').Append(ticks[i].ToString("R",CultureInfo.InvariantCulture)).Append('\n');File.WriteAllText(Path.Combine(output,stem+"-ticks.csv"),csv.ToString());
        }
        void OnDestroy(){if(gc.Valid)gc.Dispose();}
        void OnApplicationQuit(){Debug.Log("U2 benchmark: cierre, escenario="+scenario+", activo="+Active);}
        [Serializable]sealed class Report
        {
            public string unity,cpu,gpu,graphics,quality;
            public string backend="Mono",seed=CombatSession.Seed,loadout="chancla + naftalina + dentaduras + fregona, nivel 1; Remedios; sin tomos ni objetos";
            public string conditions="10 s calentamiento, 30 s medida; QA invulnerable y sin selección de cartas; reposición hasta carga; circuito 8 s, 120 ticks/lado; yaw 0 pitch 20; sin guardado";
            public string unavailable="-1: no disponible/anómalo. Memoria Unity al final, no pico/VRAM. GPU se invalida si supera toda la ventana. GC release puede no existir.";
            public bool development,editor,validRender;public int ramMB,outputWidth,outputHeight,internalWidth,internalHeight,vsync,fpsLimit,entities,minEntities,maxEntities,frames,ticks,framesOverBudget,projectiles,enemyShots,gems,coins,kills,effectsDropped;
            public double seconds,meanFps,meanFrameMs,p95,p99,max,tickMean,tickP95,tickP99,tickMax,cpuMean,gpuMean,gcBytesPerFrame;public long memory,reserved,rendered;
        }
    }
}
