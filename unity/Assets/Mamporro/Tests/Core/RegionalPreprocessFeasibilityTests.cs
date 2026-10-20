using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // Puerta de viabilidad offline, no solver nuevo ni integración en navegación.
    // No reserva matrices: tamaños exactos y ensayo acotado de transferencias existentes.
    public sealed class RegionalPreprocessFeasibilityTests
    {
        [TestCase(16, 0)] [TestCase(32, 0)] [TestCase(64, 0)]
        [TestCase(16, 1)] [TestCase(32, 1)] [TestCase(64, 1)]
        [TestCase(16, 2)] [TestCase(32, 2)] [TestCase(64, 2)]
        [Category("PreprocessGate")]
        public void MeasurePreparationBeforeAuthorizingPrototype(int size, int repetition)
        {
            var circuit = new VerticalCircuit(new ProceduralMap("HORDA750"));
            var worlds = new NavPortalWorld[3];
            var exits = new List<int>[3];
            long globalPairs = 0, localPairs = 0, nodeExitPairs = 0;
            int totalSources = 0;
            var build = Stopwatch.StartNew();
            foreach (var body in NavClass.All)
                worlds[body.Index] = new NavPortalWorld(circuit.Queries, circuit.Terrain,
                    circuit.Collision.IsInside, body, size);
            build.Stop();
            foreach (var w in worlds)
            {
                var ins = new HashSet<int>[w.Regions.Length];
                var outs = new HashSet<int>[w.Regions.Length];
                for (int r = 0; r < ins.Length; r++) { ins[r] = new HashSet<int>(); outs[r] = new HashSet<int>(); }
                int walk = 0, climb = 0, drop = 0;
                foreach (var p in w.Portals)
                {
                    ins[p.ToRegion].Add(p.To); outs[p.FromRegion].Add(p.From);
                    if (p.Kind == NavEdgeKind.Walk) walk++;
                    else if (p.Kind == NavEdgeKind.Climb) climb++;
                    else if (p.Kind == NavEdgeKind.Drop) drop++;
                }
                var sources = exits[w.Class.Index] = new List<int>();
                long pairs = 0, dense = 0;
                for (int r = 0; r < ins.Length; r++)
                {
                    pairs += (long)ins[r].Count * outs[r].Count;
                    dense += (long)w.Regions[r].OwnedNodes * outs[r].Count;
                    sources.AddRange(outs[r]);
                }
                sources.Sort(); totalSources += sources.Count;
                localPairs += pairs; nodeExitPairs += dense;
                globalPairs += (long)w.NodeCount * w.NodeCount;
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "PRECOUNT size={0} rep={1} class={2} nodes={3} portals={4} exits={5} localPairs={6} nodeExitPairs={7} walk={8} climb={9} drop={10}",
                    size, repetition, w.Class.Index, w.NodeCount, w.Portals.Length, sources.Count, pairs, dense, walk, climb, drop));
            }
            // Tiempo realmente ejecutado: parar al superar el máximo aprobado de carga.
            // No extrapolar como medición completa. La carga geométrica y validación
            // quedan fuera: incluso esta medición parcial es optimista para aceptación.
            int completed = 0; long work = 0; bool stopped = false;
            var prep = Stopwatch.StartNew();
            foreach (var w in worlds)
            {
                var scheduler = new RegionalNavScheduler(1);
                var cache = new RegionalTransferCache(w, scheduler, 1, reverse: true);
                foreach (int source in exits[w.Class.Index])
                {
                    int region = w.NodeRegion[source];
                    Assert.That(cache.Request(source, region), Is.True);
                    do
                    {
                        int used = scheduler.Tick();
                        Assert.That(used, Is.InRange(1, 3000));
                        work += used;
                        if (build.Elapsed.TotalMilliseconds + prep.Elapsed.TotalMilliseconds > 6000)
                        { stopped = true; break; }
                    } while (cache.State(source, region) == RegionalTransferState.Pending);
                    if (stopped) break;
                    completed++;
                }
                if (stopped) break;
            }
            prep.Stop();
            Assert.That(globalPairs, Is.GreaterThan(localPairs));
            Assert.That(nodeExitPairs, Is.GreaterThanOrEqualTo(localPairs));
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "PREGATE size={0} rep={1} buildMs={2:F3} prepMs={3:F3} completed={4} sources={5} work={6} stopped={7} globalDistanceBytes={8} localDistanceBytes={9} nodeExitDistanceBytes={10} managedObserved={11}",
                size, repetition, build.Elapsed.TotalMilliseconds, prep.Elapsed.TotalMilliseconds, completed,
                totalSources, work, stopped, globalPairs * 4, localPairs * 4, nodeExitPairs * 4,
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()));
        }
    }
}
