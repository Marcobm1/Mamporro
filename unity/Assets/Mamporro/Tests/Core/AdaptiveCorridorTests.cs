using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class AdaptiveCorridorTests
    {
        static IEnumerable<TestCaseData> Corpus()
        {
            foreach (string seed in new[]{"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})
                foreach(int size in new[]{16,32,64}) yield return new TestCaseData(seed,size);
        }
        static int[] Oracle(NavGraph g,int goal)
        {
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var queued=new bool[d.Length];var q=new Queue<int>();
            d[goal]=0;q.Enqueue(goal);queued[goal]=true;
            while(q.Count>0){int b=q.Dequeue();queued[b]=false;
                for(int j=g.RevStart[b];j<g.RevStart[b+1];j++){
                    int e=g.RevEdge[j],a=g.EdgeFrom[e],v=checked(d[b]+g.EdgeCost[e]);
                    if(v>=d[a])continue;d[a]=v;if(!queued[a]){queued[a]=true;q.Enqueue(a);}
                }
            }return d;
        }
        static void Log(string tag,params object[] pairs)
        {
            var line=new List<string>{tag};for(int i=0;i<pairs.Length;i+=2)
                line.Add(pairs[i]+"="+Convert.ToString(pairs[i+1],CultureInfo.InvariantCulture));
            TestContext.Out.WriteLine(string.Join(" ",line));
        }
        static void ValidateEdge(NavPortalWorld w,NavGraph full,int[] inverse,int e)
        {
            int a=w.EdgeFrom[e],b=w.EdgeTo[e],fa=inverse[a],fb=inverse[b];var g=w.Graph(a);int local=w.EdgeLocal[e],found=-1;
            for(int k=full.EdgeStart[fa];k<full.EdgeStart[fa+1];k++)
                if(full.EdgeTo[k]==fb && full.EdgeCost[k]==w.EdgeCost[e] && full.EdgeKind[k]==g.EdgeKind[local]
                    && SamePath(g,g.EdgePath[local],full,full.EdgePath[k])){found=k;break;}
            Assert.That(found,Is.GreaterThanOrEqualTo(0),"Arista sin identidad monolítica");
        }
        static bool SamePath(NavGraph g,int p,NavGraph full,int fp)
        {
            if(p<0 || fp<0)return p<0 && fp<0;
            int start=g.PathStart[p],end=g.PathStart[p+1],fs=full.PathStart[fp];
            if(end-start!=full.PathStart[fp+1]-fs || g.PathNX[p]!=full.PathNX[fp] ||
                g.PathNZ[p]!=full.PathNZ[fp] || g.PathD[p]!=full.PathD[fp] ||
                (g.PathLedge[p]<0?-1:g.PathLedge[p]-start)!=(full.PathLedge[fp]<0?-1:full.PathLedge[fp]-fs))return false;
            for(int i=0;i<end-start;i++)if(g.PathX[start+i]!=full.PathX[fs+i] ||
                g.PathY[start+i]!=full.PathY[fs+i] || g.PathZ[start+i]!=full.PathZ[fs+i])return false;
            return true;
        }
        static void Measure(NavPortalWorld w,NavGraph full,string label,double baseMs)
        {
            var watch=Stopwatch.StartNew();var probe=new AdaptiveCorridorProbe(w);double setup=watch.Elapsed.TotalMilliseconds;
            var baseline=new HybridCorridorProbe(w);
            var inverse=new int[w.NodeCount];
            for(int n=0;n<full.NodeCount;n++){int r=w.Find(NavPortalWorld.NodeKey(full,n));Assert.That(r,Is.GreaterThanOrEqualTo(0));inverse[r]=n;}
            Assert.That(full.NodeCount,Is.EqualTo(w.NodeCount));
            // Valida la identidad física de todo el universo; las rutas no pueden inventar geometría.
            for(int e=0;e<w.EdgeCount;e++)ValidateEdge(w,full,inverse,e);
            var ordered=Enumerable.Range(0,w.NodeCount).OrderBy(n=>NavPortalWorld.NodeKey(w.Graph(n),w.NodeLocal[n])).ToArray();
            var goals=new SortedSet<int>();for(int i=0;i<4;i++)goals.Add(ordered[i*(ordered.Length-1)/3]);
            var global=new HashSet<int>();foreach(int n in ordered)if(global.Add(w.GlobalStrong[w.Component[n]]))goals.Add(n);
            var cross=Enumerable.Range(0,w.EdgeCount).Where(e=>w.Component[w.EdgeFrom[e]]!=w.Component[w.EdgeTo[e]]).ToArray();
            var adjacent=new Dictionary<int,List<int>>();
            for(int i=0;i<Math.Min(6,cross.Length);i++){
                int e=cross[i*(cross.Length-1)/Math.Max(1,Math.Min(6,cross.Length)-1)],goal=w.EdgeTo[e];goals.Add(goal);
                if(!adjacent.ContainsKey(goal))adjacent[goal]=new List<int>();adjacent[goal].Add(w.EdgeFrom[e]);
            }
            if(label=="Z9K2" && w.Size==64 && w.Class.Index==0){
                goals.Add(10514);if(!adjacent.ContainsKey(10514))adjacent[10514]=new List<int>();adjacent[10514].Add(62);
                baseline.Build(10514,2);Assert.That(baseline.Distance[62],Is.EqualTo(8691),"H1 reproduction");
                Assert.That(Oracle(full,inverse[10514])[inverse[62]],Is.EqualTo(50),"Optimal reproduction");
            }
            var oracle=goals.ToDictionary(g=>g,g=>Oracle(full,inverse[g]));
            long physical=w.Regions.Sum(r=>RegionalBaselineTests.ArrayBytes(r.Graph));
            Log("H1B-PREP","case",label,"size",w.Size,"class",w.Class.Index,"baseMs",baseMs,"worldMs",w.BuildMs,
                "setupMs",setup,"payload",probe.Payload,"physical",physical,"topology",RegionalBaselineTests.ArrayBytes(w)+44L*w.Portals.Length,
                "managedObserved",GC.GetTotalMemory(false),"unityObserved",UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                "setupWork",probe.SetupWork,"scratch",probe.SetupScratchPayload,"nodes",w.NodeCount,"arcs",w.ComponentTo.Length,"components",w.ComponentCount);
            var path=new List<int>(w.NodeCount);
            for(int witnesses=0;witnesses<=3;witnesses++){
                var ratios=new List<double>();long work=0,reachChecks=0,routes=0,heapWork=0,routeWork=0,maxWork=0,parentChecks=0;double cpu=0,max=0;int worstFrom=-1,worstGoal=-1,worstCost=0,worstOptimal=0;
                foreach(int goal in goals){
                    watch.Restart();if(witnesses==0)baseline.Build(goal,2);else probe.Build(goal,witnesses);
                    double queryMs=watch.Elapsed.TotalMilliseconds;cpu+=queryMs;
                    long queryWork=witnesses==0?baseline.Work:probe.Work;work+=queryWork;maxWork=Math.Max(maxWork,queryWork);
                    if(witnesses>0)heapWork+=probe.HeapWork;
                    var distance=witnesses==0?baseline.Distance:probe.Distance;
                    var parent=witnesses==0?baseline.Parent:probe.Parent;
                    Log("H1B-QUERY","case",label,"size",w.Size,"class",w.Class.Index,"k",witnesses,"goal",goal,"work",queryWork,"cpuMs",queryMs,
                        "init",witnesses==0?-1:probe.InitWork,"macro",witnesses==0?-1:probe.MacroWork,"physical",witnesses==0?-1:probe.PhysicalWork,
                        "heap",witnesses==0?-1:probe.HeapWork);
                    for(int n=0;n<w.NodeCount;n++)if(n!=goal && distance[n]!=int.MaxValue){
                        int e=parent[n];Assert.That(e,Is.GreaterThanOrEqualTo(0));Assert.That(w.EdgeFrom[e],Is.EqualTo(n));
                        Assert.That(w.EdgeCost[e],Is.GreaterThan(0));
                        Assert.That(distance[w.EdgeTo[e]]+(long)w.EdgeCost[e],Is.EqualTo(distance[n]));parentChecks++;
                    }
                    var expected=oracle[goal];
                    for(int n=0;n<w.NodeCount;n++){
                        if((distance[n]!=int.MaxValue)!=(expected[inverse[n]]!=int.MaxValue))Assert.Fail("Conectividad perdida o inventada");reachChecks++;
                    }
                    var sources=new SortedSet<int>();for(int i=0;i<24;i++)sources.Add(ordered[i*(ordered.Length-1)/23]);
                    sources.Add(goal);if(adjacent.ContainsKey(goal))foreach(int n in adjacent[goal])sources.Add(n);
                    foreach(int source in sources){
                        int optimal=expected[inverse[source]];bool found=witnesses==0?baseline.Route(source,goal,path):probe.Route(source,goal,path);
                        Assert.That(found,Is.EqualTo(optimal!=int.MaxValue));if(!found)continue;
                        int current=source;long cost=0;
                        foreach(int e in path){Assert.That(w.EdgeFrom[e],Is.EqualTo(current));cost+=w.EdgeCost[e];current=w.EdgeTo[e];}
                        Assert.That(current,Is.EqualTo(goal));Assert.That(cost,Is.EqualTo(distance[source]));Assert.That(cost,Is.GreaterThanOrEqualTo((long)optimal));routes++;
                        if(optimal==0){Assert.That(path,Is.Empty);continue;}
                        double ratio=(double)cost/optimal;ratios.Add(ratio);
                        if(label=="Z9K2" && w.Size==64 && w.Class.Index==0 && source==62 && goal==10514)
                            Log("H1B-WORST","k",witnesses,"cost",cost,"optimal",optimal,"ratio",ratio);
                        if(ratio>2)Log("H1B-FAILPAIR","case",label,"size",w.Size,"class",w.Class.Index,"k",witnesses,"source",source,"goal",goal,"cost",cost,"optimal",optimal,"ratio",ratio);
                        if(ratio>max){max=ratio;worstFrom=source;worstGoal=goal;worstCost=(int)cost;worstOptimal=optimal;}
                    }
                    if(witnesses>0)routeWork+=probe.RouteWork;
                }
                ratios.Sort();double p95=ratios.Count==0?1:ratios[(int)Math.Ceiling(.95*ratios.Count)-1];
                bool pass=p95<=1.5 && max<=2;
                Log("H1B-GATE","case",label,"size",w.Size,"class",w.Class.Index,"k",witnesses,"goals",goals.Count,
                    "reachChecks",reachChecks,"routes",routes,"samples",ratios.Count,"p95",p95,"max",max,"pass",pass?1:0,
                    "work",work,"maxWork",maxWork,"heapWork",heapWork,"routeWork",routeWork,"parentChecks",parentChecks,"cpuMs",cpu,"worstFrom",worstFrom,"worstGoal",worstGoal,"cost",worstCost,"optimal",worstOptimal);
                // Todas las variantes quedan registradas, incluso si fallan.
            }
        }
        [TestCaseSource(nameof(Corpus))]
        [Category("AdaptiveGateB")]
        public void CorpusMeasuresDirectedCorridors(string seed,int size)
        {
            var timer=Stopwatch.StartNew();
            var c=seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
            double circuitMs=timer.Elapsed.TotalMilliseconds;
            foreach(var body in NavClass.All){
                var full=c.Map==null?new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,body):c.Map.Graphs[body.Index];
                var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size);
                Measure(w,full,seed,circuitMs);
            }
        }
        [Test, Category("AdaptiveGateA")]
        public void KnownWorstCaseAndVerticalMap(){CorpusMeasuresDirectedCorridors("Z9K2",64);}
        static bool Inside(double x,double z,double r)=>Math.Abs(x)+r<36 && Math.Abs(z)+r<36;
        [TestCase("barrier")] [TestCase("roofs")] [TestCase("drop")] [TestCase("climb")] [TestCase("ramp")] [TestCase("pass")]
        [Category("AdaptiveGateA")]
        public void DifficultPhysicalCases(string label)
        {
            var h=new float[81*81];if(label=="ramp")for(int z=0;z<=80;z++)for(int x=0;x<=80;x++)h[z*81+x]=(float)Math.Max(0,Math.Min(6,(x+z-60)*.2));
            var terrain=new Heightfield(80,80,h);VerticalSolid[] solids;
            switch(label){
                case "barrier":solids=new[]{new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-17,0,-40),new Vec3(-15,20,-7))};break;
                case "roofs":solids=new[]{new VerticalSolid(1,new Vec3(8,6,8),new Vec3(24,7,24)),new VerticalSolid(2,new Vec3(10,13,10),new Vec3(22,14,22))};break;
                case "drop":solids=new[]{new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)),new VerticalSolid(2,new Vec3(7.75,6,7.75),new Vec3(24.25,7,24.25))};break;
                case "climb":solids=new[]{new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24))};break;
                case "pass":solids=new[]{new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-40,0,-1),new Vec3(-1.5,20,1)),new VerticalSolid(2,VerticalSurfaceKind.Foliage,new Vec3(1.5,0,-1),new Vec3(40,20,1))};break;
                default:solids=new[]{new VerticalSolid(1,new Vec3(-14,7,-14),new Vec3(6,8,6))};break;
            }
            var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,solids);
            foreach(var body in NavClass.All){
                var full=new NavGraph(q,terrain,Inside,body);var w=new NavPortalWorld(q,terrain,Inside,body,16);var reference=new NavPortalQuery(w);
                if(label=="barrier"){
                    int a=w.NodeAt(-21,0,-16),b=w.NodeAt(-11,0,-16);Assert.That(w.NodeRegion[a],Is.EqualTo(w.NodeRegion[b]));Assert.That(w.Component[a],Is.Not.EqualTo(w.Component[b]));
                    var probe=new HybridCorridorProbe(w);probe.Build(b,1);var route=new List<int>();Assert.That(probe.Route(a,b,route),Is.True);
                    Assert.That(route.Count(e=>w.NodeRegion[w.EdgeFrom[e]]!=w.NodeRegion[w.EdgeTo[e]]),Is.GreaterThanOrEqualTo(2));
                }
                if(label=="drop")Assert.That(w.Portals.Any(p=>p.Kind==NavEdgeKind.Drop && !reference.Reachable(p.To,p.From)),Is.True);
                if(label=="climb")Assert.That(w.Portals.Any(p=>p.Kind==NavEdgeKind.Climb),Is.True);
                if(label=="roofs")Assert.That(reference.Reachable(w.NodeAt(16,0,16),w.NodeAt(16,14,16)),Is.False);
                if(label=="pass")Assert.That(reference.Reachable(w.NodeAt(0,0,-10),w.NodeAt(0,0,10)),Is.EqualTo(body.Index<2));
                Measure(w,full,label,0);
            }
        }
    }
}
