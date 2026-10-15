using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalPortalTests
    {
        sealed class MemorySample : IDisposable
        {
            volatile bool stop;
            readonly System.Threading.Thread thread;
            public long Peak;
            public MemorySample(){Peak=GC.GetTotalMemory(false);thread=new System.Threading.Thread(()=>{while(!stop){Peak=Math.Max(Peak,GC.GetTotalMemory(false));System.Threading.Thread.Sleep(2);}});thread.IsBackground=true;thread.Start();}
            public void Dispose(){stop=true;thread.Join();Peak=Math.Max(Peak,GC.GetTotalMemory(false));}
        }
        static bool AllocationCounterWorks()
        {long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];long after=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(probe);return after-before>=4096;}
        static readonly string[] Seeds={"P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"};
        static IEnumerable<TestCaseData> Corpus()
        {foreach(string seed in new[]{"A","B"}.Concat(Seeds))foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        static int[] Distances(NavGraph g,int source)
        {
            // Oráculo independiente: relajación por cola (sin usar SCC ni heap regional).
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var queued=new bool[g.NodeCount];
            var q=new Queue<int>();q.Enqueue(source);queued[source]=true;d[source]=0;
            while(q.Count>0){int a=q.Dequeue();queued[a]=false;for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){
                int b=g.EdgeTo[e],cost=d[a]+g.EdgeCost[e];if(cost>=d[b])continue;d[b]=cost;if(!queued[b]){q.Enqueue(b);queued[b]=true;}
            }}return d;
        }
        static NavPortalWorld Compare(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,NavGraph full,int size,string label)
        {
            bool allocationCounter=AllocationCounterWorks();var sample=new MemorySample();
            long allocated=GC.GetAllocatedBytesForCurrentThread(),managedBefore=GC.GetTotalMemory(false);
            NavPortalWorld w;
            try{w=new NavPortalWorld(queries,terrain,inside,full.Class,size);}finally{sample.Dispose();}
            long buildAllocated=GC.GetAllocatedBytesForCurrentThread()-allocated,managedAfter=GC.GetTotalMemory(false);
            if(!allocationCounter)buildAllocated=-1;
            Assert.That(w.NodeCount,Is.EqualTo(full.NodeCount));Assert.That(w.EdgeCount,Is.EqualTo(full.EdgeCount));
            var map=new int[full.NodeCount];var inverse=new int[w.NodeCount];
            for(int a=0;a<full.NodeCount;a++){map[a]=w.Find(NavPortalWorld.NodeKey(full,a));Assert.That(map[a],Is.GreaterThanOrEqualTo(0));inverse[map[a]]=a;}
            // Bijección de TODAS las aristas, no solo vecinos XZ. Evita rutas falsas/perdidas aunque no se muestreen.
            int expectedPortals=0;
            for(int a=0;a<full.NodeCount;a++){
                int ra=map[a];Assert.That(w.EdgeStart[ra+1]-w.EdgeStart[ra],Is.EqualTo(full.EdgeStart[a+1]-full.EdgeStart[a]));
                var expected=new List<string>();var actual=new List<string>();
                for(int e=full.EdgeStart[a];e<full.EdgeStart[a+1];e++){
                    expected.Add($"{map[full.EdgeTo[e]]}:{full.EdgeCost[e]}:{full.EdgeKind[e]}");
                    if(w.NodeRegion[ra]!=w.NodeRegion[map[full.EdgeTo[e]]])expectedPortals++;
                }
                for(int e=w.EdgeStart[ra];e<w.EdgeStart[ra+1];e++){var g=w.Graph(ra);actual.Add($"{w.EdgeTo[e]}:{w.EdgeCost[e]}:{g.EdgeKind[w.EdgeLocal[e]]}");}
                expected.Sort();actual.Sort();Assert.That(actual,Is.EqualTo(expected));
            }
            Assert.That(w.Portals.Length,Is.EqualTo(expectedPortals));
            foreach(var p in w.Portals){var g=w.Regions[p.FromRegion].Graph;
                Assert.That(p.FromRegion,Is.Not.EqualTo(p.ToRegion));Assert.That(p.ClassIndex,Is.EqualTo(full.Class.Index));
                Assert.That(p.LocalFrom,Is.EqualTo(g.EdgeFrom[p.LocalEdge]));Assert.That(p.Cost,Is.EqualTo(g.EdgeCost[p.LocalEdge]));
                Assert.That(p.Path,Is.EqualTo(g.EdgePath[p.LocalEdge]));Assert.That((byte)p.Kind,Is.EqualTo(g.EdgeKind[p.LocalEdge]));
                Assert.That(w.Find(NavPortalWorld.NodeKey(g,g.EdgeTo[p.LocalEdge])),Is.EqualTo(p.To));
                Assert.That(p.LocalTo,Is.EqualTo(w.NodeLocal[p.To]));
            }
            var query=new NavPortalQuery(w);var path=new List<int>(w.NodeCount);double coldReachMs=0,coldRouteMs=0;
            // Fuentes distribuidas y un representante de cada SCC GLOBAL: incluyen islas/sumideros.
            var sources=new HashSet<int>();for(int k=0;k<8;k++)sources.Add(k*(full.NodeCount-1)/7);
            var globalSeen=new HashSet<int>();for(int a=0;a<full.NodeCount;a++)if(globalSeen.Add(w.GlobalStrong[w.Component[map[a]]]))sources.Add(a);
            foreach(int source in sources){
                var expected=Distances(full,source);int rs=map[source];long begin=Stopwatch.GetTimestamp();
                query.Reachable(rs,map[full.NodeCount-1]);coldReachMs=Math.Max(coldReachMs,(Stopwatch.GetTimestamp()-begin)*1000.0/Stopwatch.Frequency);
                begin=Stopwatch.GetTimestamp();query.Distance(rs,rs);coldRouteMs=Math.Max(coldRouteMs,(Stopwatch.GetTimestamp()-begin)*1000.0/Stopwatch.Frequency);
                // Todas las parejas desde cada fuente elegida, con positivos y negativos.
                for(int target=0;target<full.NodeCount;target++)if(query.Reachable(rs,map[target])!=(expected[target]!=int.MaxValue))Assert.Fail($"{label} clase={full.Class.Index} {source}->{target}");
                for(int k=0;k<8;k++){
                    int t=k*(full.NodeCount-1)/7,rt=map[t];Assert.That(query.Distance(rs,rt),Is.EqualTo(expected[t]));
                    bool found=query.Route(rs,rt,path);Assert.That(found,Is.EqualTo(expected[t]!=int.MaxValue));
                    int at=rs,cost=0;foreach(int e in path){Assert.That(w.EdgeFrom[e],Is.EqualTo(at));cost+=w.EdgeCost[e];at=w.EdgeTo[e];}
                    if(found){Assert.That(at,Is.EqualTo(rt));Assert.That(cost,Is.EqualTo(expected[t]));}
                }
            }
            // Medición sin aserciones/strings y caché precalentada; ninguna asignación en consultas.
            int start=map[0],end=map[full.NodeCount-1];query.Reachable(start,end);query.Distance(start,end);query.Route(start,end,path);
            long before=GC.GetAllocatedBytesForCurrentThread(),ticks=Stopwatch.GetTimestamp();
            for(int i=0;i<100;i++){query.Reachable(start,end);query.Distance(start,end);query.Route(start,end,path);}
            long elapsed=Stopwatch.GetTimestamp()-ticks,queryAllocated=GC.GetAllocatedBytesForCurrentThread()-before;
            if(allocationCounter)Assert.That(queryAllocated,Is.Zero);else queryAllocated=-1;
            Assert.That(query.Reachable(-1,end),Is.False);Assert.That(query.Reachable(start,w.NodeCount),Is.False);
            long physical=w.Regions.Sum(r=>RegionalBaselineTests.ArrayBytes(r.Graph));
            long topology=RegionalBaselineTests.ArrayBytes(w);long portalBytes=(long)w.Portals.Length*System.Runtime.InteropServices.Marshal.SizeOf<NavPortal>();
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "D2 {0} class={1} size={2} nodes={3} edges={4} portals={5} localScc={6} localWeak={7} globalScc={8} arcs={9} buildMs={10:F3} topologyMs={11:F3} physicalArrays={12} topologyArrays={13} portalBytes={14} queryArrays={15} buildAllocated={16} managedBefore={17} managedAfter={18} warmQueryUs={19:F3} queryAllocated={20} sources={21}",
                label,full.Class.Index,size,w.NodeCount,w.EdgeCount,w.Portals.Length,w.ComponentCount,w.WeakCount,w.GlobalStrongCount,w.ComponentTo.Length,w.BuildMs,w.TopologyMs,physical,topology,portalBytes,RegionalBaselineTests.ArrayBytes(query),buildAllocated,managedBefore,managedAfter,elapsed*1e6/Stopwatch.Frequency/100,queryAllocated,sources.Count));
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,"D2-query {0} class={1} size={2} coldReachMaxMs={3:F4} coldRouteMaxMs={4:F4} managedSampledPeak={5} allocationCounterValid={6}",label,full.Class.Index,size,coldReachMs,coldRouteMs,sample.Peak,allocationCounter));
            return w;
        }
        [TestCaseSource(nameof(Corpus))]
        public void CorpusMatchesPhysicalOracle(string seed,int size)
        {
            var c=seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
            foreach(var body in NavClass.All){var full=c.Map==null?new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,body):c.Map.Graphs[body.Index];Compare(c.Queries,c.Terrain,c.Collision.IsInside,full,size,seed);}
        }

        static Heightfield Flat()=>new Heightfield(80,80,new float[81*81]);
        static bool Inside(double x,double z,double r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36;
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MustLeaveRegionAndReturnAroundBarrier(int kind)
        {
            var terrain=Flat();var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-17,0,-40),new Vec3(-15,20,-7)));
            var full=new NavGraph(q,terrain,Inside,NavClass.All[kind]);var w=Compare(q,terrain,Inside,full,16,"barrier");
            int a=w.NodeAt(-21,0,-16),b=w.NodeAt(-11,0,-16);Assert.That(a,Is.GreaterThanOrEqualTo(0));Assert.That(b,Is.GreaterThanOrEqualTo(0));
            Assert.That(w.NodeRegion[a],Is.EqualTo(w.NodeRegion[b]));Assert.That(w.Component[a],Is.Not.EqualTo(w.Component[b]));Assert.That(w.WeakComponent[a],Is.Not.EqualTo(w.WeakComponent[b]));
            var path=new List<int>();Assert.That(new NavPortalQuery(w).Route(a,b,path),Is.True);
            Assert.That(path.Count(e=>w.NodeRegion[w.EdgeFrom[e]]!=w.NodeRegion[w.EdgeTo[e]]),Is.GreaterThanOrEqualTo(2));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FloatingLayersDoNotInventConnections(int kind)
        {
            var terrain=Flat();var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(8,6,8),new Vec3(24,7,24)),
                new VerticalSolid(2,new Vec3(10,13,10),new Vec3(22,14,22)));
            var full=new NavGraph(q,terrain,Inside,NavClass.All[kind]);var w=Compare(q,terrain,Inside,full,16,"roofs");var query=new NavPortalQuery(w);
            int roof=w.NodeAt(16,7,16),ground=w.NodeAt(16,0,16),upper=w.NodeAt(16,14,16);
            Assert.That(Math.Min(roof,Math.Min(ground,upper)),Is.GreaterThanOrEqualTo(0));
            Assert.That(w.NodeRegion[roof],Is.EqualTo(w.NodeRegion[ground]));Assert.That(w.Component[roof],Is.Not.EqualTo(w.Component[ground]));
            // El oráculo heredado no crea bajada en esta placa suspendida: D2 no debe inventarla.
            Assert.That(query.Reachable(roof,ground),Is.False);Assert.That(query.Reachable(ground,roof),Is.False);
            Assert.That(query.Reachable(ground,upper),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void DirectedPhysicalDropDoesNotCreateReturn(int kind)
        {
            var terrain=Flat();bool found=false;
            const double overhang=.25;
            {
                var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)),
                    new VerticalSolid(2,new Vec3(8-overhang,6,8-overhang),new Vec3(24+overhang,7,24+overhang)));
                var full=new NavGraph(q,terrain,Inside,NavClass.All[kind]);var w=new NavPortalWorld(q,terrain,Inside,NavClass.All[kind],16);var query=new NavPortalQuery(w);
                foreach(var p in w.Portals)if(p.Kind==NavEdgeKind.Drop&&!query.Reachable(p.To,p.From)){
                    int from=full.NodeAt(w.Position(p.From).X,w.Position(p.From).Y,w.Position(p.From).Z,.01);
                    int to=full.NodeAt(w.Position(p.To).X,w.Position(p.To).Y,w.Position(p.To).Z,.01);
                    Assert.That(ProceduralMap.Reach(full,from)[to],Is.True);Assert.That(ProceduralMap.Reach(full,to)[from],Is.False);
                    Assert.That(query.Reachable(p.From,p.To),Is.True);found=true;
                    TestContext.Out.WriteLine($"Caída unidireccional clase={kind} voladizo={overhang} portal={p.FromRegion}->{p.ToRegion}");break;
                }
            }
            Assert.That(found,Is.True,"La fixture debe contener al menos una caída física sin retorno");
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ClimbAcrossBoundaryPreservesFullTrajectory(int kind)
        {
            var terrain=Flat();var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)));
            var w=new NavPortalWorld(q,terrain,Inside,NavClass.All[kind],16);
            var climbs=w.Portals.Where(p=>p.Kind==NavEdgeKind.Climb).ToArray();Assert.That(climbs.Length,Is.GreaterThan(0));
            foreach(var p in climbs){var g=w.Regions[p.FromRegion].Graph;int begin=g.PathStart[p.Path],end=g.PathStart[p.Path+1];
                Assert.That(end-begin,Is.GreaterThan(2));Assert.That(g.PathLedge[p.Path],Is.InRange(begin,end-1));
                var target=w.Position(p.To);Assert.That(g.PathX[end-1],Is.EqualTo((float)target.X));Assert.That(g.PathY[end-1],Is.EqualTo((float)target.Y));Assert.That(g.PathZ[end-1],Is.EqualTo((float)target.Z));
            }
        }
        [Test]
        public void EmptyWorldRejectsQueriesAndInvalidRegionSize()
        {
            var terrain=Flat();var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg);
            var w=new NavPortalWorld(q,terrain,(x,z,r)=>false,NavClass.All[0],32);
            Assert.That(w.NodeCount,Is.Zero);Assert.That(w.Portals,Is.Empty);Assert.That(w.ComponentCount,Is.Zero);
            var query=new NavPortalQuery(w);Assert.That(query.Reachable(0,0),Is.False);Assert.That(query.Distance(0,0),Is.EqualTo(int.MaxValue));
            Assert.That(query.Route(0,0,new List<int>()),Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(()=>new NavPortalWorld(q,terrain,Inside,NavClass.All[0],24));
        }
        [Test]
        public void RampCornerLargeStructureAndNarrowPassRespectClasses()
        {
            var heights=new float[81*81];for(int z=0;z<=80;z++)for(int x=0;x<=80;x++)heights[z*81+x]=(float)Math.Max(0,Math.Min(6,(x+z-60)*.2));
            var terrain=new Heightfield(80,80,heights);var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(-14,7,-14),new Vec3(6,8,6)),
                new VerticalSolid(2,new Vec3(-6,0,10),new Vec3(-4,10,24)),
                new VerticalSolid(3,new Vec3(-1,0,10),new Vec3(1,10,24)));
            foreach(var body in NavClass.All){var full=new NavGraph(q,terrain,Inside,body);foreach(int size in new[]{16,32,64})Compare(q,terrain,Inside,full,size,"ramp-corner");}
            // Paso de 3 m entre sólidos: S/M caben, L no. Aislado para no confundirlo con el relieve.
            terrain=Flat();q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-40,0,-1),new Vec3(-1.5,20,1)),
                new VerticalSolid(2,VerticalSurfaceKind.Foliage,new Vec3(1.5,0,-1),new Vec3(40,20,1)));
            foreach(var body in NavClass.All){var full=new NavGraph(q,terrain,Inside,body);var w=Compare(q,terrain,Inside,full,16,"pass");
                Assert.That(new NavPortalQuery(w).Reachable(w.NodeAt(0,0,-10),w.NodeAt(0,0,10)),Is.EqualTo(body.Index<2));}
        }
    }
}
