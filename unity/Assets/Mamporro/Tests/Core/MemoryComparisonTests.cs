using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mamporro.Core;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace Mamporro.Tests
{
    public sealed class MemoryComparisonTests
    {
        static IEnumerable<TestCaseData> Cases() {foreach(string seed in new[]{"MAMPORRO","Z9K2","RAMPA42"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        sealed class Sample : IDisposable
        {
            readonly Thread thread;volatile bool run=true;
            internal long Peak,Working,Private,ProcessLifetimePeak;internal int Count;internal double MaxGap;
            internal Sample(){thread=new Thread(()=>{long last=Stopwatch.GetTimestamp();while(run){long now=Stopwatch.GetTimestamp();MaxGap=Math.Max(MaxGap,(now-last)*1000.0/Stopwatch.Frequency);last=now;Peak=Math.Max(Peak,GC.GetTotalMemory(false));var p=MemoryCertificationTests.ReadProcess();Working=Math.Max(Working,(long)p.WorkingSet.ToUInt64());Private=Math.Max(Private,(long)p.Private.ToUInt64());ProcessLifetimePeak=(long)p.PeakWorkingSet.ToUInt64();Count++;Thread.Sleep(1);}}){IsBackground=true};thread.Start();}
            public void Dispose(){if(!run)return;run=false;thread.Join();}
        }
        [TestCaseSource(nameof(Cases)),Category("MemoryPilotB")]
        public void Pilot(string seed,int size) {Pairs(seed,size,1);}
        [TestCaseSource(nameof(Cases)),Category("MemoryComparisonB")]
        public void TenPairs(string seed,int size) {Pairs(seed,size,10);}
        static void Pairs(string seed,int size,int trials)
        {
            // El control se repite dentro de cada test/proceso que publica datos.
            new MemoryCertificationTests().FrameAllocationCounterControl();
            for(int trial=0;trial<trials;trial++) {
                Measure(seed,size,trial,trial%2==0);Measure(seed,size,trial,trial%2!=0);
            }
        }
        static void Collect(){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Measure(string seed,int size,int trial,bool reuse)
        {
            var full=Stopwatch.StartNew();Collect();
            using(var counter=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",16))
            using(var sampler=new Sample()) {
                long baseline=GC.GetTotalMemory(false),allocationBefore=counter.CurrentValue;int frameSamples=counter.Count,gcBefore=GC.CollectionCount(0);
                var watch=Stopwatch.StartNew();var map=new ProceduralMap(seed);var circuit=new VerticalCircuit(map);
                double mapMs=watch.Elapsed.TotalMilliseconds;long mapAllocated=counter.CurrentValue-allocationBefore,afterMap=GC.GetTotalMemory(false);
                var worlds=new object[3];var indices=new object[3];long worldAllocated=0,indexAllocated=0;double worldMs=0,indexMs=0;long unityObserved=Profiler.GetTotalAllocatedMemoryLong();
                for(int k=0;k<3;k++) {
                    long a=counter.CurrentValue;long t=Stopwatch.GetTimestamp();
                    if(reuse)worlds[k]=new ReusedRegionalWorld(map.Graphs[k],size);
                    else worlds[k]=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[k],size);
                    worldMs+=(Stopwatch.GetTimestamp()-t)*1000.0/Stopwatch.Frequency;worldAllocated+=counter.CurrentValue-a;
                    a=counter.CurrentValue;t=Stopwatch.GetTimestamp();
                    if(reuse)indices[k]=new ReusedCorridorProbe((ReusedRegionalWorld)worlds[k]);else indices[k]=new AdaptiveCorridorProbe((NavPortalWorld)worlds[k]);
                    indexMs+=(Stopwatch.GetTimestamp()-t)*1000.0/Stopwatch.Frequency;indexAllocated+=counter.CurrentValue-a;
                    unityObserved=Math.Max(unityObserved,Profiler.GetTotalAllocatedMemoryLong());
                }
                double totalMs=watch.Elapsed.TotalMilliseconds;long allocated=counter.CurrentValue-allocationBefore;int gc=GC.CollectionCount(0)-gcBefore;
                long beforeGC=GC.GetTotalMemory(false);sampler.Dispose();long peak=Math.Max(beforeGC,sampler.Peak);
                Assert.That(counter.Count,Is.EqualTo(frameSamples),"No aceptar diferencia entre frames distintos");Assert.That(allocated,Is.GreaterThan(0));
                Collect();long retained=GC.GetTotalMemory(false);unityObserved=Math.Max(unityObserved,Profiler.GetTotalAllocatedMemoryLong());
                long owned=H1ePayload.Count(map.Graphs,out long capacities);
                for(int k=0;k<3;k++) {
                    if(reuse)owned+=H1ePayload.Count(new[]{worlds[k],indices[k]},out _);
                    else {
                        var w=(NavPortalWorld)worlds[k];foreach(var region in w.Regions){owned+=H1ePayload.Count(new object[]{region.Graph},out long c);capacities+=c;}
                        owned+=RegionalBaselineTests.ArrayBytes(w)+44L*w.Portals.Length+((AdaptiveCorridorProbe)indices[k]).Payload;
                    }
                }
                double fullMs=full.Elapsed.TotalMilliseconds;
                TestContext.Out.WriteLine(FormattableString.Invariant($"H1E-LOAD seed={seed} size={size} trial={trial} mode={(reuse?"reused":"original")} totalMs={totalMs} diagnosticMs={fullMs} mapMs={mapMs} worldMs={worldMs} indexMs={indexMs} generationMs={map.GenerationMs} precheckMs={map.PreCheckMs} graphsMs={map.MapsMs} validationMs={map.ValidationMs} builds={map.Builds} repairs={map.Repairs} rejections={map.Rejections} allocated={allocated} mapAllocated={mapAllocated} worldAllocated={worldAllocated} indexAllocated={indexAllocated} gc={gc} baseline={baseline} afterMap={afterMap} beforeGC={beforeGC} peak={peak} retained={retained} excess={Math.Max(0,peak-retained)} ownedArrayBytes={owned} listCapacityBytes={capacities} unityObserved={unityObserved} workingObserved={sampler.Working} privateObserved={sampler.Private} processLifetimePeak={sampler.ProcessLifetimePeak} samples={sampler.Count} maxGapMs={sampler.MaxGap} transientAttributable=ND residentAttributable=ND"));
                Assert.That(map.Check.Passed,Is.True);GC.KeepAlive(map);GC.KeepAlive(circuit);GC.KeepAlive(worlds);GC.KeepAlive(indices);
            }
        }
    }
}
