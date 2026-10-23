using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class NavigationReuseTests
    {
        static IEnumerable<TestCaseData> Corpus()
        {
            foreach (string seed in new[]{"P0QA","MAMPORRO","Z9K2","RAMPA42","HORDA750"})
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
        static int[] VerifyPhysical(NavPortalWorld w,NavGraph full)
        {
            if(full.NodeCount!=w.NodeCount||full.EdgeCount!=w.EdgeCount)Assert.Fail("Universos diferentes");
            var inverse=new int[w.NodeCount];
            for(int n=0;n<full.NodeCount;n++){
                int r=w.Find(NavPortalWorld.NodeKey(full,n));if(r<0)Assert.Fail("Nodo perdido");inverse[r]=n;
                var g=w.Graph(r);int local=w.NodeLocal[r];
                if(g.NodeY[local]!=full.NodeY[n]||g.NodeTerrain[local]!=full.NodeTerrain[n])Assert.Fail("Capa diferente");
            }
            for(int e=0;e<w.EdgeCount;e++)ValidateEdge(w,full,inverse,e);
            return inverse;
        }

        [TestCaseSource(nameof(Corpus)),Category("ReusePreparation")]
        public void ProfilePreparation(string seed,int size)
        {
            for(int trial=0;trial<3;trial++){
                var all=Stopwatch.StartNew();var map=new ProceduralMap(seed);var circuit=new VerticalCircuit(map);
                double baseMs=all.Elapsed.TotalMilliseconds;
                Assert.That(map.Check.Passed,Is.True);
                var worlds=new NavPortalWorld[3];var probes=new AdaptiveCorridorProbe[3];double setupMs=0,worldMs=0;
                for(int k=0;k<3;k++){
                    worlds[k]=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[k],size);
                    worldMs+=worlds[k].BuildMs;var watch=Stopwatch.StartNew();probes[k]=new AdaptiveCorridorProbe(worlds[k]);setupMs+=watch.Elapsed.TotalMilliseconds;
                }
                double actualMs=all.Elapsed.TotalMilliseconds;
                double projectionMs=0,certificateMs=0;long projectionPayload=0,physicalPayload=0,topologyPayload=0,probePayload=0;
                for(int k=0;k<3;k++){
                    var w=worlds[k];var full=map.Graphs[k];var watch=Stopwatch.StartNew();
                    var projection=new NavigationProjectionProbe(full,size);projectionMs+=watch.Elapsed.TotalMilliseconds;projectionPayload+=projection.Payload;
                    watch.Restart();var inverse=VerifyPhysical(w,full);projection.Verify(w,inverse);certificateMs+=watch.Elapsed.TotalMilliseconds;
                    physicalPayload+=w.Regions.Sum(r=>RegionalBaselineTests.ArrayBytes(r.Graph));
                    topologyPayload+=RegionalBaselineTests.ArrayBytes(w)+44L*w.Portals.Length;probePayload+=probes[k].Payload;
                    Log("H1C-CLASS","case",seed,"size",size,"trial",trial,"class",k,"originalNodes",full.NodeCount,
                        "regionalNodesWithHalo",w.Regions.Sum(r=>r.Graph.NodeCount),"ownedNodes",w.NodeCount,"edges",w.EdgeCount,
                        "regions",w.Regions.Length,"attempts",w.Regions.Sum(r=>r.Attempts),"d1Ms",w.BuildMs-w.TopologyMs,"d2Ms",w.TopologyMs,
                        "regionalNodeMs",w.Regions.Sum(r=>r.Graph.NodesMs),"regionalClimbMs",w.Regions.Sum(r=>r.Graph.ClimbMs),
                        "regionalDropMs",w.Regions.Sum(r=>r.Graph.DropMs),"originalFinalGraphMs",full.BuildMs,
                        "originalNodeMs",full.NodesMs,"originalClimbMs",full.ClimbMs,"originalDropMs",full.DropMs);
                }
                Log("H1C-PREP","case",seed,"size",size,"trial",trial,"generationMs",map.GenerationMs,
                    "precheckMs",map.PreCheckMs,"originalMapsMs",map.MapsMs,"validationMs",map.ValidationMs,"mapTotalMs",map.TotalMs,
                    "mapResidualMs",map.TotalMs-map.GenerationMs-map.PreCheckMs-map.MapsMs-map.ValidationMs,
                    "circuitAndWrapperMs",baseMs-map.TotalMs,"baseMs",baseMs,"d1Ms",worlds.Sum(w=>w.BuildMs-w.TopologyMs),
                    "d2Ms",worlds.Sum(w=>w.TopologyMs),"indexMs",setupMs,"accountedMs",baseMs+worldMs+setupMs,"actualMs",actualMs,
                    "projectionMs",projectionMs,"certificateMs",certificateMs,"diagnosticWallMs",all.Elapsed.TotalMilliseconds,
                    "projectionPayload",projectionPayload,"originalGraphPayload",map.Graphs.Sum(g=>RegionalBaselineTests.ArrayBytes(g)),
                    "regionalPhysicalPayload",physicalPayload,"topologyPayload",topologyPayload,"indexPayload",probePayload,
                    "builds",map.Builds,"repairs",map.Repairs,"rejections",map.Rejections,
                    "managedObserved",GC.GetTotalMemory(false),"unityObserved",UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
            }
        }

        static double DistanceSquared(NavPortalWorld w,int a,int b)
        {var p=w.Position(a);var q=w.Position(b);double x=p.X-q.X,y=p.Y-q.Y,z=p.Z-q.Z;return x*x+y*y+z*z;}
        static double P95(List<double> values)
        {values.Sort();return values.Count==0?1:values[(int)Math.Ceiling(.95*values.Count)-1];}

        [TestCaseSource(nameof(Corpus)),Category("ReuseCorridors")]
        public void CompareLocalReuse(string seed,int size)
        {
            var map=new ProceduralMap(seed);var c=new VerticalCircuit(map);Assert.That(map.Check.Passed,Is.True);
            foreach(var body in NavClass.All){
                var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size);var full=map.Graphs[body.Index];
                var inverse=VerifyPhysical(w,full);
                var previous=new AdaptiveCorridorProbe(w);var current=new AdaptiveCorridorProbe(w);
                var timer=Stopwatch.StartNew();var local=new LocalRefinementProbe(w);double localIndexMs=timer.Elapsed.TotalMilliseconds;
                var ordered=Enumerable.Range(0,w.NodeCount).OrderBy(n=>NavPortalWorld.NodeKey(w.Graph(n),w.NodeLocal[n])).ToArray();
                var anchors=new SortedSet<int>();for(int i=0;i<4;i++)anchors.Add(ordered[i*(ordered.Length-1)/3]);
                anchors.Add(ordered.OrderByDescending(n=>w.Position(n).Y).First());
                if(seed=="Z9K2"&&size==64&&body.Index==0)anchors.Add(10514);
                var ratios=new List<double>();var freshRatios=new List<double>();var path=new List<int>(w.NodeCount);
                long reachChecks=0,routes=0,coldWork=0,fullWork=0,localWork=0,macroSaved=0,heapWork=0,routeWork=0;
                int pairs=0,changed=0;double worst=0;int worstSource=-1,worstGoal=-1,worstOld=-1;
                foreach(int old in anchors){
                    timer.Restart();previous.Build(old,3);double coldMs=timer.Elapsed.TotalMilliseconds;coldWork+=previous.Work;
                    Log("H1C-COLD","case",seed,"size",size,"class",body.Index,"old",old,"work",previous.Work,
                        "heap",previous.HeapWork,"cpuMs",coldMs);
                    if(seed=="Z9K2"&&size==64&&body.Index==0&&old==10514)
                        Assert.That(previous.Distance[62],Is.EqualTo(50),"Referencia H1b 62→10514 alterada");
                    var candidates=ordered.Where(n=>n!=old&&w.Component[n]==w.Component[old]).ToArray();
                    var goals=new SortedSet<int>();
                    foreach(double distance in new[]{.5,8.0,24.0})if(candidates.Length>0)
                        goals.Add(candidates.OrderBy(n=>Math.Abs(Math.Sqrt(DistanceSquared(w,n,old))-distance)).First());
                    if(candidates.Length>0)goals.Add(candidates.OrderByDescending(n=>DistanceSquared(w,n,old)).First());
                    int differentIndex=Array.FindIndex(ordered,n=>w.Component[n]!=w.Component[old]);
                    if(differentIndex>=0){int different=ordered[differentIndex];
                        Assert.Throws<ArgumentException>(()=>local.Build(old,different));changed++;
                    }
                    foreach(int goal in goals){
                        timer.Restart();current.Build(goal,3);double fullMs=timer.Elapsed.TotalMilliseconds;
                        if(!previous.Potential.SequenceEqual(current.Potential))Assert.Fail("Potencial cambió dentro de la misma SCC");
                        if(!previous.Next.SequenceEqual(current.Next))Assert.Fail("Árbol macro cambió dentro de la misma SCC");
                        timer.Restart();local.Build(old,goal);double localMs=timer.Elapsed.TotalMilliseconds;
                        Assert.That(local.PhysicalWork,Is.EqualTo(local.Extractions+local.ExaminedEdges+local.RelaxationAttempts));
                        var oracle=Oracle(full,inverse[goal]);long thisReach=0;
                        int maskChanged=0;
                        for(int n=0;n<w.NodeCount;n++){
                            bool reachable=oracle[inverse[n]]!=int.MaxValue;
                            if((current.Distance[n]!=int.MaxValue)!=reachable ||
                                (previous.Distance[n]!=int.MaxValue||local.Distance[n]!=int.MaxValue)!=reachable)Assert.Fail("Alcance distinto");
                            if(local.Distance[n]!=int.MaxValue&&n!=goal){
                                int e=local.Parent[n];if(e<0||w.EdgeFrom[e]!=n||w.EdgeCost[e]<=0||
                                    (long)local.Distance[w.EdgeTo[e]]+w.EdgeCost[e]!=local.Distance[n])Assert.Fail("Padre local inválido");
                            }
                            if((DistanceSquared(w,n,old)<=1024)!=(DistanceSquared(w,n,goal)<=1024))maskChanged++;
                            thisReach++;
                        }
                        var sources=new SortedSet<int>{old,goal};for(int i=0;i<24;i++)sources.Add(ordered[i*(ordered.Length-1)/23]);
                        // Conserva pares cortos adyacentes a la meta, incluido el caso H1 original.
                        for(int e=0;e<w.EdgeCount;e++)if(w.EdgeTo[e]==goal)sources.Add(w.EdgeFrom[e]);
                        if(seed=="Z9K2"&&size==64&&body.Index==0)sources.Add(62);
                        var queryRatios=new List<double>();var queryFresh=new List<double>();
                        foreach(int source in sources){
                            int optimal=oracle[inverse[source]];bool found=local.Route(previous,source,goal,path);
                            if(found!=(optimal!=int.MaxValue))Assert.Fail("Ruta/inaccesibilidad diferentes");if(!found)continue;
                            int at=source;long cost=0;foreach(int e in path){if(w.EdgeFrom[e]!=at)Assert.Fail("Discontinuidad");at=w.EdgeTo[e];cost+=w.EdgeCost[e];}
                            if(at!=goal||cost<optimal)Assert.Fail("Coste o destino inválidos");routes++;
                            if(optimal==0){if(path.Count!=0)Assert.Fail("Ruta cero no vacía");continue;}
                            double ratio=(double)cost/optimal,fresh=(double)current.Distance[source]/optimal;
                            ratios.Add(ratio);freshRatios.Add(fresh);queryRatios.Add(ratio);queryFresh.Add(fresh);
                            if(ratio>worst){worst=ratio;worstSource=source;worstGoal=goal;worstOld=old;}
                            if(ratio>2||fresh>2)Log("H1C-PAIRFAIL","case",seed,"size",size,"class",body.Index,"old",old,"goal",goal,
                                "source",source,"cost",cost,"optimal",optimal,"ratio",ratio,"freshRatio",fresh);
                        }
                        pairs++;reachChecks+=thisReach;fullWork+=current.Work;localWork+=local.Work;
                        macroSaved+=w.ComponentCount+current.MacroWork;heapWork+=local.HeapWork;routeWork+=local.RouteWork;
                        Log("H1C-MOVE","case",seed,"size",size,"class",body.Index,"old",old,"goal",goal,
                            "oldKey",NavPortalWorld.NodeKey(w.Graph(old),w.NodeLocal[old]),"goalKey",NavPortalWorld.NodeKey(w.Graph(goal),w.NodeLocal[goal]),
                            "distance",Math.Sqrt(DistanceSquared(w,old,goal)),"domain",local.DomainNodes,"nodes",w.NodeCount,"maskChanged",maskChanged,
                            "fullWork",current.Work,"fullMacro",current.MacroWork,"macroReusable",w.ComponentCount+current.MacroWork,
                            "localWork",local.Work,"localInit",local.InitWork,"localPhysical",local.PhysicalWork,"heap",local.HeapWork,
                            "extractions",local.Extractions,"examinedEdges",local.ExaminedEdges,
                            "relaxationAttempts",local.RelaxationAttempts,"improvements",local.Improvements,"fullHeap",current.HeapWork,
                            "routeWork",local.RouteWork,"fullMs",fullMs,"localMs",localMs,"samples",queryRatios.Count,
                            "p95",P95(queryRatios),"max",queryRatios.Count==0?1:queryRatios.Max(),
                            "freshP95",P95(queryFresh),"freshMax",queryFresh.Count==0?1:queryFresh.Max());
                    }
                }
                Log("H1C-REUSE","case",seed,"size",size,"class",body.Index,"pairs",pairs,"changedSccRejected",changed,
                    "reachChecks",reachChecks,"routes",routes,"samples",ratios.Count,"p95",P95(ratios),"max",worst,
                    "freshP95",P95(freshRatios),"freshMax",freshRatios.Count==0?1:freshRatios.Max(),
                    "coldWork",coldWork,"fullWork",fullWork,"localWork",localWork,"macroReusable",macroSaved,
                    "heap",heapWork,"routeWork",routeWork,"localIndexWork",local.IndexWork,"localIndexMs",localIndexMs,"localPayload",local.Payload,
                    "worstOld",worstOld,"worstGoal",worstGoal,"worstSource",worstSource);
            }
        }
    }
}
