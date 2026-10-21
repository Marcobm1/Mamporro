using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class LandmarkGateTests
    {
        static void Log(string tag, params object[] values)
        {
            var parts = new List<string> { tag };
            for (int i = 0; i < values.Length; i += 2)
                parts.Add(values[i] + "=" + Convert.ToString(values[i + 1], CultureInfo.InvariantCulture));
            TestContext.Out.WriteLine(string.Join(" ", parts));
        }
        static int[] Oracle(NavGraph g, int goal)
        {
            var d = Enumerable.Repeat(int.MaxValue, g.NodeCount).ToArray();
            var queued = new bool[d.Length]; var q = new Queue<int>();
            d[goal] = 0; q.Enqueue(goal); queued[goal] = true;
            while (q.Count > 0)
            {
                int b = q.Dequeue(); queued[b] = false;
                for (int k = g.RevStart[b]; k < g.RevStart[b + 1]; k++)
                {
                    int e = g.RevEdge[k], a = g.EdgeFrom[e], cost = checked(d[b] + g.EdgeCost[e]);
                    if (cost >= d[a]) continue; d[a] = cost;
                    if (!queued[a]) { queued[a] = true; q.Enqueue(a); }
                }
            }
            return d;
        }
        // Mismo recorrido y muestreo que DemandNavLoadTests; no se modifica aquel arnés.
        static int[] GoalWalk(NavPortalWorld w)
        {
            int start = w.NodeAt(0, 0, 0); if (start < 0) start = w.NodeCount / 2;
            var walk = new List<int> { start };
            for (int step = 1; step < 32; step++)
            {
                int a = walk[walk.Count - 1], best = -1;
                for (int e = w.EdgeStart[a]; e < w.EdgeStart[a + 1]; e++)
                {
                    if ((NavEdgeKind)w.Graph(a).EdgeKind[w.EdgeLocal[e]] != NavEdgeKind.Walk) continue;
                    int b = w.EdgeTo[e]; if (walk.Contains(b)) continue;
                    bool back = false;
                    for (int k = w.EdgeStart[b]; k < w.EdgeStart[b + 1]; k++) if (w.EdgeTo[k] == a) back = true;
                    if (back) { best = b; break; }
                }
                if (best < 0) break; walk.Add(best);
            }
            return walk.ToArray();
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        [Category("LandmarkCorrectness")]
        public void BoundsAndRoutesRespectOneWayDropsAndUnreachableLayers(int cls)
        {
            var terrain = new Heightfield(80, 80, new float[81*81]);
            Func<double,double,double,bool> inside = (x,z,r) => Math.Abs(x)+r<36 && Math.Abs(z)+r<36;
            var q = new VerticalQueries(terrain, Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)),
                new VerticalSolid(2,new Vec3(7.75,6,7.75),new Vec3(24.25,7,24.25)),
                new VerticalSolid(3,new Vec3(-8,13,8),new Vec3(0,14,16)));
            var w = new NavPortalWorld(q,terrain,inside,NavClass.All[cls],16);
            var full = new NavGraph(q,terrain,inside,NavClass.All[cls]);
            var inverse = new int[w.NodeCount];
            for (int n=0;n<full.NodeCount;n++) inverse[w.Find(NavPortalWorld.NodeKey(full,n))]=n;
            var p = new LandmarkGateProbe(w,8); var query = new NavPortalQuery(w);
            bool asymmetric=false, unreachable=false;
            foreach (var portal in w.Portals)
                if (portal.Kind==NavEdgeKind.Drop && !query.Reachable(portal.To,portal.From))
                {
                    foreach (int k in new[]{2,4,8})
                    {
                        int expected=query.Distance(portal.From,portal.To);
                        Assert.That(p.Search(portal.From,portal.To,k),Is.EqualTo(expected));
                        p.VerifyRoute(portal.From,portal.To,expected);
                        Assert.That(p.Search(portal.To,portal.From,k),Is.EqualTo(int.MaxValue));
                    }
                    asymmetric=true; break;
                }
            foreach (int goal in new[]{w.NodeAt(16,7,16),w.NodeAt(-4,14,12),w.NodeAt(0,0,0)})
            {
                Assert.That(goal,Is.GreaterThanOrEqualTo(0));
                var expected=Oracle(full,inverse[goal]);
                foreach (int k in new[]{2,4,8})
                {
                    for (int n=0;n<w.NodeCount;n++)
                    {
                        Assert.That(p.Bound(n,goal,k),Is.InRange(0,expected[inverse[n]]));
                        if (expected[inverse[n]]==int.MaxValue) unreachable=true;
                    }
                    for (int i=0;i<12;i++)
                    {
                        int source=i*(w.NodeCount-1)/11, cost=expected[inverse[source]];
                        Assert.That(p.Search(source,goal,k),Is.EqualTo(cost));
                        p.VerifyRoute(source,goal,cost);
                    }
                }
            }
            Assert.That(asymmetric,Is.True); Assert.That(unreachable,Is.True);
        }
        [TestCase(16, 0)] [TestCase(32, 0)] [TestCase(64, 0)]
        [TestCase(16, 1)] [TestCase(32, 1)] [TestCase(64, 1)]
        [TestCase(16, 2)] [TestCase(32, 2)] [TestCase(64, 2)]
        [Category("LandmarkGate")]
        public void MeasureFullPreparationAndDirectedSearch(int size, int repetition)
        {
            var timer = Stopwatch.StartNew();
            var circuit = new VerticalCircuit(new ProceduralMap("HORDA750"));
            double circuitMs = timer.Elapsed.TotalMilliseconds;
            var worlds = new NavPortalWorld[3]; timer.Restart();
            for (int c = 0; c < 3; c++)
                worlds[c] = new NavPortalWorld(circuit.Queries, circuit.Terrain, circuit.Collision.IsInside, NavClass.All[c], size);
            double worldMs = timer.Elapsed.TotalMilliseconds;
            long physical = worlds.Sum(w => w.Regions.Sum(r => RegionalBaselineTests.ArrayBytes(r.Graph)));
            long topology = worlds.Sum(w => RegionalBaselineTests.ArrayBytes(w) + 44L * w.Portals.Length);
            foreach (int count in new[] { 2, 4, 8 })
            {
                // Preparación independiente por K; ninguna tabla de K anterior se reutiliza.
                var probes = new LandmarkGateProbe[3]; timer.Restart();
                for (int c = 0; c < 3; c++) probes[c] = new LandmarkGateProbe(worlds[c], count);
                double landmarkMs = timer.Elapsed.TotalMilliseconds;
                Log("ALT-PREP", "size", size, "rep", repetition, "k", count,
                    "circuitMs", circuitMs, "worldMs", worldMs, "landmarkMs", landmarkMs,
                    "navigationMs", worldMs + landmarkMs, "completeMs", circuitMs + worldMs + landmarkMs,
                    "work", probes.Sum(p => p.PreparationWork), "payload", probes.Sum(p => p.Payload),
                    "physicalArrays", physical, "topologyArrays", topology,
                    "temporaryBytes", worlds.Max(w => 12L * w.NodeCount + 4),
                    "managedObserved", UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),
                    "unityAllocatedObserved", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
                // Puerta barata: consultas en una partición, preparación en las tres.
                // No es aún corpus completo ni simulación de servicio/latencias.
                if (size != 64 || repetition != 0) continue;
                for (int c = 0; c < 3; c++) CompareQueries(circuit, probes[c], count);
            }
        }
        static void CompareQueries(VerticalCircuit circuit, LandmarkGateProbe p, int count)
        {
            var w = p.World; int c = w.Class.Index; var full = circuit.Map.Graphs[c];
            var inverse = new int[w.NodeCount];
            for (int n = 0; n < full.NodeCount; n++) inverse[w.Find(NavPortalWorld.NodeKey(full, n))] = n;
            var walk = GoalWalk(w);
            Assert.That(walk.Length, Is.EqualTo(32));
            var dispersed = Enumerable.Range(0, w.NodeCount)
                .OrderBy(n => NavPortalWorld.NodeKey(w.Graph(n), w.NodeLocal[n])).ToArray();
            var center = w.Position(walk[0]);
            var concentrated = dispersed.Where(n => {
                var q = w.Position(n);
                return (q.X-center.X)*(q.X-center.X)+(q.Z-center.Z)*(q.Z-center.Z)<=64 && Math.Abs(q.Y-center.Y)<2;
            }).ToArray();
            int high = dispersed.OrderByDescending(n => w.Position(n).Y).First();
            var goals = new[] { walk[0], walk[1], walk[15], walk[31], high };
            var oracle = goals.Distinct().ToDictionary(g => g, g => Oracle(full, inverse[g]));
            foreach (int goal in goals.Distinct())
                for (int n = 0; n < w.NodeCount; n++)
                    if (oracle[goal][inverse[n]] != int.MaxValue && p.Bound(n, goal, count) > oracle[goal][inverse[n]])
                        throw new Exception("Cota no admisible");
            foreach (string distribution in new[] { "concentrated", "dispersed" })
            {
                var candidates = distribution == "concentrated" ? concentrated : dispersed;
                long altWork = 0, dijWork = 0, altPops = 0, dijPops = 0, bounds = 0;
                double altMs = 0, dijMs = 0; int unreachable = 0, queries = 0;
                foreach (int goal in goals)
                    for (int i = 0; i < 16; i++)
                    {
                        int source = candidates[(2*i+1)*candidates.Length/32], expected = oracle[goal][inverse[source]];
                        long start = Stopwatch.GetTimestamp(); int actual = p.Search(source, goal, count);
                        altMs += (Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                        altWork += p.Work; altPops += p.Pops; bounds += p.Bounds;
                        Assert.That(actual, Is.EqualTo(expected)); p.VerifyRoute(source, goal, expected);
                        start = Stopwatch.GetTimestamp(); actual = p.Search(source, goal, 0);
                        dijMs += (Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                        dijWork += p.Work; dijPops += p.Pops;
                        Assert.That(actual, Is.EqualTo(expected)); queries++;
                        if (expected == int.MaxValue) unreachable++;
                    }
                Log("ALT-PAIR", "k", count, "class", c, "distribution", distribution, "queries", queries,
                    "unreachable", unreachable, "altWork", altWork, "dijkstraWork", dijWork,
                    "altPops", altPops, "dijkstraPops", dijPops, "bounds", bounds, "altMs", altMs, "dijkstraMs", dijMs);
                // Cohorte exactamente igual al cambio vecino caliente heredado con 750.
                int consumers = c == 0 ? 600 : c == 1 ? 149 : 1;
                long work = 0, pops = 0; double cpu = 0;
                for (int i = 0; i < consumers; i++)
                {
                    int source = candidates[(2*i+1)*candidates.Length/(2*consumers)], goal = walk[1];
                    long start = Stopwatch.GetTimestamp(); int actual = p.Search(source, goal, count);
                    cpu += (Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                    work += p.Work; pops += p.Pops;
                    Assert.That(actual, Is.EqualTo(oracle[goal][inverse[source]])); p.VerifyRoute(source, goal, actual);
                }
                Log("ALT-COHORT", "k", count, "class", c, "distribution", distribution,
                    "consumers", consumers, "work", work, "pops", pops, "cpuMs", cpu);
            }
        }
    }
}
