using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class MemoryPhaseTests
    {
        static IEnumerable<TestCaseData> Cases() {foreach(string seed in new[]{"MAMPORRO","Z9K2","RAMPA42"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        static void Collect() {GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
        [TestCaseSource(nameof(Cases)),Category("MemoryPhasesA")]
        public void DiagnosticPhases(string seed,int size)
        {
            // Dos repeticiones diagnósticas, sin pretender abrir B ni acreditar rendimiento.
            for(int trial=0;trial<2;trial++) RunPhase(seed,size,trial);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RunPhase(string seed,int size,int trial)
        {
                Collect();long baseline=GC.GetTotalMemory(false);var timer=Stopwatch.StartNew();
                using(var trace=new H1eMemoryTrace()) {
                    var map=new H1eMeasuredMap(seed);var worlds=new H1eMeasuredWorld[3];var indices=new H1eMeasuredIndex[3];
                    long mapEnd=GC.GetTotalMemory(false);
                    // Intervención causal separada: GC entre mapa y vista en la segunda repetición.
                    // No usar este tiempo para la preparación completa B.
                    if(trial==1)Collect();long beforeProjection=GC.GetTotalMemory(false);
                    for(int k=0;k<3;k++) {worlds[k]=new H1eMeasuredWorld(map.Graphs[k],size);H1eMemoryTrace.Begin("H1bIndex");indices[k]=new H1eMeasuredIndex(worlds[k]);H1eMemoryTrace.End();}
                    double workMs=timer.Elapsed.TotalMilliseconds;long beforeGC=GC.GetTotalMemory(false);Collect();long retained=GC.GetTotalMemory(false);
                    trace.Dispose();
                    long payload=0;for(int k=0;k<3;k++)payload+=RegionalBaselineTests.ArrayBytes(map.Graphs[k])+RegionalBaselineTests.ArrayBytes(worlds[k])+44L*worlds[k].Portals.Length+indices[k].Payload;
                    TestContext.Out.WriteLine(FormattableString.Invariant($"H1E-DIAG seed={seed} size={size} trial={trial} forcedBetween={trial==1} baseline={baseline} mapEnd={mapEnd} beforeProjection={beforeProjection} beforeGC={beforeGC} retained={retained} workMs={workMs} builds={map.Builds} repairs={map.Repairs} rejections={map.Rejections}"));
                    trace.Report(seed,size,trial);Assert.That(map.Check.Passed,Is.True);
                    TestContext.Out.WriteLine($"H1E-PAYLOAD seed={seed} size={size} trial={trial} bytes={payload} resident=ND");
                    long graphs=H1ePayload.Count(map.Graphs,out long capacities),views=H1ePayload.Count(worlds,out _),solver=H1ePayload.Count(indices,out _);
                    TestContext.Out.WriteLine($"H1E-OWNED seed={seed} size={size} trial={trial} graphArrays={graphs} graphListCapacity={capacities} viewArrays={views} indexArrays={solver} total={graphs+views+solver} shared=ND headers=ND resident=ND");
                    GC.KeepAlive(map);GC.KeepAlive(worlds);GC.KeepAlive(indices);
                    map=null;worlds=null;indices=null;
                }
        }
        [Test,Category("MemoryPhasesA")]
        public void RetainedCapacityCounterControl()
        {
            Assert.That(H1ePayload.Stride(typeof(int)),Is.EqualTo(4));Assert.That(H1ePayload.Stride(typeof(bool)),Is.EqualTo(1));
            Assert.That(H1ePayload.Stride(typeof(NavPortal)),Is.EqualTo(44));
            Type edge=typeof(NavGraph).GetNestedType("Edge",System.Reflection.BindingFlags.NonPublic);
            Assert.That(H1ePayload.Stride(edge),Is.EqualTo(20));
            var fixture=new CapacityFixture();fixture.Values.Add(1);fixture.Values.Clear();
            long bytes=H1ePayload.Count(new object[]{fixture},out long lists);
            Assert.That(bytes,Is.EqualTo(4096+128*4));Assert.That(lists,Is.EqualTo(128*4));
            TestContext.Out.WriteLine($"H1E-CAPACITY bytes={bytes} listBytes={lists} edgeStride=20 portalStride=44");
        }
        sealed class CapacityFixture {public readonly byte[] Bytes=new byte[4096];public readonly List<int> Values=new List<int>(128);}
        [TestCase("MAMPORRO"),TestCase("Z9K2"),TestCase("RAMPA42"),Category("MemoryPhasesA")]
        public void DiagnosticCopyMatchesOriginal(string seed)
        {
            var original=new ProceduralMap(seed);var copy=new H1eMeasuredMap(seed);
            Assert.That(copy.Check.Summary(),Is.EqualTo(original.Check.Summary()));Assert.That(copy.Log,Is.EqualTo(original.Log));
            Assert.That(copy.Builds,Is.EqualTo(original.Builds));Assert.That(copy.Repairs,Is.EqualTo(original.Repairs));Assert.That(copy.Rejections,Is.EqualTo(original.Rejections));
            for(int k=0;k<3;k++) {
                Assert.That(copy.Graphs[k].NodeCount,Is.EqualTo(original.Graphs[k].NodeCount));Assert.That(copy.Graphs[k].EdgeCount,Is.EqualTo(original.Graphs[k].EdgeCount));
                foreach(int size in new[]{16,32,64}) {
                    var a=new ReusedRegionalWorld(original.Graphs[k],size);var b=new H1eMeasuredWorld(copy.Graphs[k],size);
                    Assert.That(b.NodeLocal,Is.EqualTo(a.NodeLocal));Assert.That(b.EdgeTo,Is.EqualTo(a.EdgeTo));Assert.That(b.EdgeCost,Is.EqualTo(a.EdgeCost));
                    Assert.That(b.Component,Is.EqualTo(a.Component));Assert.That(b.GlobalStrong,Is.EqualTo(a.GlobalStrong));Assert.That(b.Portals.Length,Is.EqualTo(a.Portals.Length));
                }
            }
        }
    }
}
