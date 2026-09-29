using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mamporro.U1
{
    public sealed class LocalBenchmark : MonoBehaviour
    {
        public PrototypeController controller;
        public bool Active { get; private set; }
        const double Warmup=10, Duration=30;
        const int Capacity=200000;
        readonly double[] frames=new double[Capacity];
        readonly long[] allocations=new long[Capacity];
        readonly double[] cpu=new double[Capacity], gpu=new double[Capacity];
        readonly FrameTiming[] timing=new FrameTiming[1];
        readonly int[] counts={300,500,750,1000};
        ProfilerRecorder gc;
        double start, previous;
        int samples, scenario, ticks;
        bool exitWhenDone;
        bool captured;
        long firstRenderedFrame;
        double firstSampleTime;
        string outputDirectory;
        public void ReadCommandLine()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="-u1-output" && i+1<args.Length) outputDirectory=args[++i];
                else if(args[i]=="-u1-benchmark") exitWhenDone=true;
            }
            if(exitWhenDone) Begin(true);
        }
        public void Begin(bool quit)
        {
            if(Active) return;
            exitWhenDone=quit; scenario=0; Active=true;
            if(string.IsNullOrWhiteSpace(outputDirectory)) outputDirectory=Path.Combine(Application.persistentDataPath,"U1Benchmarks");
            Directory.CreateDirectory(outputDirectory);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            Next();
        }
        void Next()
        {
            controller.ResetTrial(counts[scenario]); controller.SetPaused(false);
            ticks=0; samples=0; captured=false; previous=start=Time.realtimeSinceStartupAsDouble;
        }
        public MoveIntent ScriptedIntent()
        {
            // Circuito cerrado de 8 s en la zona central; reloj fijo independiente de FPS.
            int segment=(ticks++/120)%4;
            var directions=new Vector2();
            if(segment==0) directions=Vector2.right;
            else if(segment==1) directions=Vector2.up;
            else if(segment==2) directions=Vector2.left;
            else directions=Vector2.down;
            return new MoveIntent { direction=directions };
        }
        void LateUpdate()
        {
            if(!Active) return;
            double now=Time.realtimeSinceStartupAsDouble;
            double elapsed=now-start;
            if(!captured && elapsed>2) { captured=true; ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory,$"u1-{Screen.width}x{Screen.height}-{counts[scenario]}.png")); }
            FrameTimingManager.CaptureFrameTimings();
            if(elapsed>=Warmup && samples<Capacity)
            {
                if(samples==0) { firstRenderedFrame=controller.RenderedFrames; firstSampleTime=previous; }
                frames[samples]=(now-previous)*1000;
                allocations[samples]=gc.Valid ? gc.LastValue : -1;
                uint n=FrameTimingManager.GetLatestTimings(1,timing);
                cpu[samples]=n>0 && timing[0].cpuFrameTime>0 ? timing[0].cpuFrameTime : -1;
                gpu[samples]=n>0 && timing[0].gpuFrameTime>0 ? timing[0].gpuFrameTime : -1;
                samples++;
            }
            previous=now;
            if(elapsed>=Warmup+Duration)
            {
                Save(now); scenario++;
                if(scenario<counts.Length) Next();
                else { Cancel(); if(exitWhenDone) Application.Quit(0); }
            }
        }
        public void Cancel() { Active=false; if(gc.Valid) gc.Dispose(); controller.SetPaused(true); }
        void OnDestroy() { if(gc.Valid) gc.Dispose(); }
        static string F(double value)=>value.ToString("F4",CultureInfo.InvariantCulture);
        void Save(double endTime)
        {
            long memoryAllocated=Profiler.GetTotalAllocatedMemoryLong(), memoryReserved=Profiler.GetTotalReservedMemoryLong();
            string stem=$"u1-{(Application.isEditor?"editor":"player")}-{Screen.width}x{Screen.height}-{counts[scenario]}-{DateTime.UtcNow:yyyyMMddTHHmmssfff}";
            var csv=new StringBuilder("frame,frame_ms,cpu_ms,gpu_ms,gc_bytes\n");
            double total=0; long allocTotal=0;
            for(int i=0;i<samples;i++) { total+=frames[i]; allocTotal+=Math.Max(0,allocations[i]); csv.Append(i).Append(',').Append(F(frames[i])).Append(',').Append(F(cpu[i])).Append(',').Append(F(gpu[i])).Append(',').Append(allocations[i]).Append('\n'); }
            File.WriteAllText(Path.Combine(outputDirectory,stem+".csv"),csv.ToString());
            var sorted=new double[samples]; Array.Copy(frames,sorted,samples); Array.Sort(sorted);
            var report=new Report {
                unity=Application.unityVersion, environment=Application.isEditor?"Editor":"Windows Player", development=Debug.isDebugBuild,
                cpu=SystemInfo.processorType, gpu=SystemInfo.graphicsDeviceName, ramMB=SystemInfo.systemMemorySize,
                graphics=SystemInfo.graphicsDeviceType.ToString(), outputWidth=Screen.width, outputHeight=Screen.height,
                internalHeight=controller.InternalHeight, entities=controller.Horde.Count, seed=controller.settings.seed,
                vsync=QualitySettings.vSyncCount, fpsLimit=Application.targetFrameRate, frames=samples,
                meanMs=total/Math.Max(1,samples), p95Ms=samples>0?sorted[Mathf.Clamp((int)Math.Ceiling(samples*.95)-1,0,samples-1)]:0,
                meanFps=samples*1000/Math.Max(.001,total), gcBytesPerFrame=gc.Valid?(double)allocTotal/Math.Max(1,samples):-1,
                allocatedMemory=memoryAllocated, reservedMemory=memoryReserved,
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()], dither=controller.Dither, snap=controller.Snap
            };
            report.renderedFrames=controller.RenderedFrames-firstRenderedFrame;
            report.actualMeasurementSeconds=endTime-firstSampleTime;
            report.validRender=report.renderedFrames>=samples*.9 && samples<Capacity;
            report.cpuMeanMs=MeanAvailable(cpu,samples); report.gpuMeanMs=MeanAvailable(gpu,samples);
            File.WriteAllText(Path.Combine(outputDirectory,stem+".json"),JsonUtility.ToJson(report,true));
            Debug.Log($"U1 benchmark: {stem}; {report.meanFps:F1} FPS; P95 {report.p95Ms:F2} ms");
        }
        static double MeanAvailable(double[] values,int count)
        {
            double sum=0; int available=0;
            for(int i=0;i<count;i++) if(values[i]>=0) {sum+=values[i];available++;}
            return available>0?sum/available:-1;
        }
        [Serializable] sealed class Report
        {
            public string unity,environment,cpu,gpu,graphics,quality;
            public string backend="Mono", instrumentation="FrameTimingManager + ProfilerRecorder + reloj real; CSV escrito después de medir";
            public string unavailable="CSV: -1 significa métrica no disponible; memoria Unity al final, no pico ni VRAM";
            public string route="Circuito WASD de 8 s, 120 ticks por lado, cámara yaw 0 pitch 20, semilla fija";
            public bool development,dither,snap,validRender;
            public int ramMB,outputWidth,outputHeight,internalHeight,entities,seed,vsync,fpsLimit,frames;
            public double warmupSeconds=Warmup,measurementSeconds=Duration,meanMs,p95Ms,meanFps,gcBytesPerFrame;
            public double actualMeasurementSeconds,cpuMeanMs,gpuMeanMs;
            public long allocatedMemory,reservedMemory,renderedFrames;
        }
    }
}
