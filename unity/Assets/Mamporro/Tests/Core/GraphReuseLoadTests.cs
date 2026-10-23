using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // H1d: cargas independientes. El muestreador observa memoria managed; no es servicio de juego.
    public sealed class GraphReuseLoadTests
    {
        static IEnumerable<TestCaseData> Critical()
        {foreach(string seed in new[]{"MAMPORRO","Z9K2","RAMPA42"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        sealed class MemorySample : IDisposable
        {
            volatile bool running=true;
            readonly Thread thread;
            internal long Peak;internal int Samples;
            internal MemorySample(long baseline)
            {Peak=baseline;thread=new Thread(()=>{while(running){Peak=Math.Max(Peak,GC.GetTotalMemory(false));Samples++;Thread.Sleep(1);}}){IsBackground=true};thread.Start();}
            public void Dispose(){running=false;thread.Join();}
        }
        static void Log(params object[] pairs)
        {var text=new List<string>{"H1D-LOAD"};for(int i=0;i<pairs.Length;i+=2)text.Add(pairs[i]+"="+Convert.ToString(pairs[i+1],CultureInfo.InvariantCulture));TestContext.Out.WriteLine(string.Join(" ",text));}
        [TestCaseSource(nameof(Critical)),Category("GraphReuseLoad")]
        public void IndependentCompleteStarts(string seed,int size)
        {
            long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];
            long probeBytes=GC.GetAllocatedBytesForCurrentThread()-before;GC.KeepAlive(probe);
            for(int trial=0;trial<10;trial++){
                // Alterna el orden; cada llamada construye su propio mapa y retiene las tres clases.
                Measure(seed,size,trial,trial%2==0,probeBytes);Measure(seed,size,trial,trial%2!=0,probeBytes);
            }
        }
        static void Measure(string seed,int size,int trial,bool reuse,long probeBytes)
        {
            var diagnostic=Stopwatch.StartNew();
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            long managedBefore=GC.GetTotalMemory(false);
            using(var sampler=new MemorySample(managedBefore))
            using(var process=Process.GetCurrentProcess()){
                double cpuBefore=process.TotalProcessorTime.TotalMilliseconds;
                long allocationBefore=GC.GetAllocatedBytesForCurrentThread();
                int gcBefore=GC.CollectionCount(0);
                var timer=Stopwatch.StartNew();
                var map=new ProceduralMap(seed);var circuit=new VerticalCircuit(map);
                if(!map.Check.Passed)throw new InvalidOperationException("Mapa no validado");
                double baseMs=timer.Elapsed.TotalMilliseconds,worldMs=0,indexMs=0,topologyMs=0;
                var worlds=new object[3];var solvers=new object[3];
                for(int k=0;k<3;k++){
                    if(reuse){
                        var w=new ReusedRegionalWorld(map.Graphs[k],size);worlds[k]=w;worldMs+=w.BuildMs;topologyMs+=w.TopologyMs;
                        var watch=Stopwatch.StartNew();solvers[k]=new ReusedCorridorProbe(w);indexMs+=watch.Elapsed.TotalMilliseconds;
                    }else{
                        var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[k],size);
                        worlds[k]=w;worldMs+=w.BuildMs;topologyMs+=w.TopologyMs;
                        var watch=Stopwatch.StartNew();solvers[k]=new AdaptiveCorridorProbe(w);indexMs+=watch.Elapsed.TotalMilliseconds;
                    }
                }
                timer.Stop();double totalMs=timer.Elapsed.TotalMilliseconds;
                long allocated=probeBytes>=4096?GC.GetAllocatedBytesForCurrentThread()-allocationBefore:-1;
                double cpuMs=process.TotalProcessorTime.TotalMilliseconds-cpuBefore;
                int collections=GC.CollectionCount(0)-gcBefore;
                sampler.Dispose();long peak=Math.Max(sampler.Peak,GC.GetTotalMemory(false));
                long payload=0,instrument=0,scratch=0;
                for(int k=0;k<3;k++){
                    payload+=RegionalBaselineTests.ArrayBytes(map.Graphs[k]);
                    if(reuse){var w=(ReusedRegionalWorld)worlds[k];var s=(ReusedCorridorProbe)solvers[k];
                        payload+=RegionalBaselineTests.ArrayBytes(w)+44L*w.Portals.Length+s.Payload;instrument+=s.Payload;scratch=Math.Max(scratch,s.SetupScratchPayload);
                    }else{var w=(NavPortalWorld)worlds[k];var s=(AdaptiveCorridorProbe)solvers[k];
                        foreach(var region in w.Regions)payload+=RegionalBaselineTests.ArrayBytes(region.Graph);
                        payload+=RegionalBaselineTests.ArrayBytes(w)+44L*w.Portals.Length+s.Payload;instrument+=s.Payload;scratch=Math.Max(scratch,s.SetupScratchPayload);}
                }
                // La colección y recuentos pertenecen al diagnóstico, fuera de la carga, y se informan.
                GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();long retained=GC.GetTotalMemory(false);
                long unity=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                Log("case",seed,"size",size,"trial",trial,"mode",reuse?"reused":"original",
                    "totalMs",totalMs,"baseMs",baseMs,"worldMs",worldMs,"topologyMs",topologyMs,"indexMs",indexMs,
                    "residualMs",totalMs-baseMs-worldMs-indexMs,"generationMs",map.GenerationMs,"precheckMs",map.PreCheckMs,
                    "graphsMs",map.MapsMs,"validationMs",map.ValidationMs,"mapTotalMs",map.TotalMs,
                    "builds",map.Builds,"repairs",map.Repairs,"rejections",map.Rejections,"cpuProcessMs",cpuMs,
                    "allocated",allocated,"allocationProbeBytes",probeBytes,"gcCollections",collections,"payload",payload,"instrumentPayload",instrument,"indexScratchPayload",scratch,
                    "managedBefore",managedBefore,"managedPeak",peak,"managedRetained",retained,"transientObserved",Math.Max(0,peak-retained),
                    "memorySamples",sampler.Samples,"unityObserved",unity,"diagnosticMs",diagnostic.Elapsed.TotalMilliseconds);
                GC.KeepAlive(worlds);GC.KeepAlive(solvers);GC.KeepAlive(map);GC.KeepAlive(circuit);
            }
        }
    }
}
