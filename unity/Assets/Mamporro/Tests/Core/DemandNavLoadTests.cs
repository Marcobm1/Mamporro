using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class DemandNavLoadTests
    {
        sealed class Histogram
        {
            readonly long[] bins=new long[40001];public long Count;
            public void Add(long ticks){if(ticks<0||ticks>=bins.Length)throw new Exception("Histograma fuera de rango");bins[ticks]++;Count++;}
            public double Q(double p){if(Count==0)return -1;long target=(long)Math.Ceiling(Count*p),n=0;for(int i=0;i<bins.Length;i++){n+=bins[i];if(n>=target)return i/60.0;}throw new Exception();}
        }
        sealed class Sample
        {
            public readonly Histogram Response=new Histogram(),Age=new Histogram(),PendingAge=new Histogram(),QueueAge=new Histogram(),First=new Histogram();
            public long Coverage,NoAnswer,Unreachable,Publications,StalePublications;
            public int PendingMax,PendingEnd;
            public readonly long[] Seen;
            public Sample(int n){Seen=new long[n];}
        }
        static int[] Oracle(NavGraph g,int goal)
        {
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var seen=new bool[g.NodeCount];var q=new Queue<int>();
            d[goal]=0;q.Enqueue(goal);seen[goal]=true;
            while(q.Count>0){int b=q.Dequeue();seen[b]=false;for(int k=g.RevStart[b];k<g.RevStart[b+1];k++){int e=g.RevEdge[k],a=g.EdgeFrom[e],v=checked(d[b]+g.EdgeCost[e]);if(v>=d[a])continue;d[a]=v;if(!seen[a]){seen[a]=true;q.Enqueue(a);}}}return d;
        }
        static int[] GoalWalk(NavPortalWorld w)
        {
            int start=w.NodeAt(0,0,0);if(start<0)start=w.NodeCount/2;var walk=new List<int>{start};
            for(int step=1;step<32;step++){
                int a=walk[walk.Count-1],best=-1;
                for(int e=w.EdgeStart[a];e<w.EdgeStart[a+1];e++){
                    if((NavEdgeKind)w.Graph(a).EdgeKind[w.EdgeLocal[e]]!=NavEdgeKind.Walk)continue;
                    int b=w.EdgeTo[e];if(walk.Contains(b))continue;
                    bool back=false;for(int k=w.EdgeStart[b];k<w.EdgeStart[b+1];k++)if(w.EdgeTo[k]==a)back=true;
                    if(back){best=b;break;}
                }
                if(best<0)break;walk.Add(best);
            }
            Assert.That(walk.Count,Is.GreaterThanOrEqualTo(3));return walk.ToArray();
        }
        static double Percentile(double[] values,double p){var sorted=(double[])values.Clone();Array.Sort(sorted);return sorted[(int)Math.Ceiling(sorted.Length*p)-1];}
        static void VerifyRoute(DemandNavService service,DemandAnswer answer,int[] expected,int[] inverse)
        {
            if(answer.Status!=DemandStatus.Reachable)return;
            int node=answer.Source,total=0,count=0;var w=service.World;
            while(node!=answer.Goal){if(++count>w.NodeCount||!service.TryRouteEdge(answer,node,out int edge))throw new Exception("Ruta no disponible al validar");
                if(w.EdgeFrom[edge]!=node||expected[inverse[node]]!=w.EdgeCost[edge]+expected[inverse[w.EdgeTo[edge]]])throw new Exception("Ruta no óptima");
                total+=w.EdgeCost[edge];node=w.EdgeTo[edge];}
            if(total!=answer.Cost)throw new Exception("Coste de ruta distinto");
        }
        [TestCase(16),TestCase(32),TestCase(64),Category("DemandLoad")]
        public void MeasureActiveConsumersFor180Seconds(int size)
        {
            var circuit=new VerticalCircuit(new ProceduralMap("HORDA750"));var worlds=new NavPortalWorld[3];var walks=new int[3][];var inverse=new int[3][];
            var expected=new Dictionary<int,int[]>[3];var concentrated=new int[3][];var dispersed=new int[3][];
            for(int c=0;c<3;c++){
                var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[c],size);worlds[c]=w;walks[c]=GoalWalk(w);
                var full=circuit.Map.Graphs[c];inverse[c]=new int[w.NodeCount];for(int n=0;n<full.NodeCount;n++)inverse[c][w.Find(NavPortalWorld.NodeKey(full,n))]=n;
                expected[c]=new Dictionary<int,int[]>();foreach(int goal in walks[c])expected[c][goal]=Oracle(full,inverse[c][goal]);
                dispersed[c]=Enumerable.Range(0,w.NodeCount).OrderBy(n=>NavPortalWorld.NodeKey(w.Graph(n),w.NodeLocal[n])).ToArray();
                var center=w.Position(walks[c][0]);concentrated[c]=dispersed[c].Where(n=>{var p=w.Position(n);return (p.X-center.X)*(p.X-center.X)+(p.Z-center.Z)*(p.Z-center.Z)<=64&&Math.Abs(p.Y-center.Y)<2;}).ToArray();
                Assert.That(concentrated[c].Length,Is.GreaterThan(0));
            }
            foreach(int population in new[]{300,500,750})foreach(string distribution in new[]{"concentrated","dispersed"})foreach(string mode in new[]{"fixed","alternating","travel"}){
                var scheduler=new RegionalNavScheduler(3);var services=new DemandNavService[3];var samples=new Sample[3];int[] counts={population*4/5,population-population*4/5-1,1};
                double setup=0;long payload=0;
                for(int c=0;c<3;c++){
                    long start=Stopwatch.GetTimestamp();services[c]=new DemandNavService(worlds[c],scheduler,counts[c]);setup+=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                    payload+=services[c].PayloadBytes;samples[c]=new Sample(counts[c]);var candidates=distribution=="concentrated"?concentrated[c]:dispersed[c];
                    for(int i=0;i<counts[c];i++)services[c].Inputs[i]=new NavDemand{Enabled=true,Source=candidates[(2*i+1)*candidates.Length/(2*counts[c])],Goal=walks[c][0],Since=0};
                }
                var cpu=new double[10800];long used=0,allocated=0;long allocationBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];bool validAllocation=GC.GetAllocatedBytesForCurrentThread()-allocationBefore>=4096;GC.KeepAlive(probe);
                long managedBefore=GC.GetTotalMemory(false),managedPeak=managedBefore;
                var verified=new HashSet<string>();
                for(int tick=0;tick<10800;tick++){
                    if(mode!="fixed"&&tick%12==0)for(int c=0;c<3;c++){
                        int step=(tick/12)%(2*(walks[c].Length-1));
                        // Ida y vuelta por aristas Walk bidireccionales; sin salto al cerrar el ciclo.
                        int next=walks[c][mode=="alternating"?(tick/12)%2:Math.Min(step,2*(walks[c].Length-1)-step)];
                        for(int i=0;i<counts[c];i++){var input=services[c].Inputs[i];if(input.Goal==next)continue;input.Goal=next;input.Since=scheduler.TickIndex;services[c].Inputs[i]=input;}
                    }
                    allocationBefore=GC.GetAllocatedBytesForCurrentThread();long start=Stopwatch.GetTimestamp();int work=scheduler.Tick();cpu[tick]=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;allocated+=GC.GetAllocatedBytesForCurrentThread()-allocationBefore;
                    if(work>3000)throw new Exception("Techo global excedido");used+=work;
                    for(int c=0;c<3;c++){
                        var s=services[c];var sample=samples[c];int pending=0;
                        for(int i=0;i<counts[c];i++){
                            var answer=s.Answers[i];var status=s.Status(i);
                            if(status==DemandStatus.Pending){pending++;sample.PendingAge.Add(scheduler.TickIndex-s.Inputs[i].Since);sample.QueueAge.Add(scheduler.TickIndex-s.OutstandingSince(i));}else{sample.Coverage++;if(status==DemandStatus.Unreachable)sample.Unreachable++;}
                            if(answer.Serial==0)sample.NoAnswer++;else sample.Age.Add(scheduler.TickIndex-answer.Requested);
                            if(answer.Serial==0||answer.Serial==sample.Seen[i])continue;
                            if(sample.Seen[i]==0)sample.First.Add(scheduler.TickIndex);
                            sample.Seen[i]=answer.Serial;sample.Publications++;sample.Response.Add(answer.Published-answer.Requested);
                            if(answer.Goal!=s.Inputs[i].Goal)sample.StalePublications++;
                            var oracle=expected[c][answer.Goal];int cost=oracle[inverse[c][answer.Source]];
                            if(answer.Cost!=cost||answer.Status!=(cost==int.MaxValue?DemandStatus.Unreachable:DemandStatus.Reachable))throw new Exception("Discrepancia de respuesta/oráculo");
                            // Costes de TODAS las respuestas, rutas completas de muestras por clase/meta.
                            string key=c+":"+answer.Goal;if(status==DemandStatus.Reachable&&verified.Add(key))VerifyRoute(s,answer,oracle,inverse[c]);
                        }
                        sample.PendingMax=Math.Max(sample.PendingMax,pending);sample.PendingEnd=pending;
                        if(s.Accounted!=s.WorkUnits)throw new Exception("Contadores incompletos");
                    }
                    if(tick%60==0)managedPeak=Math.Max(managedPeak,GC.GetTotalMemory(false));
                }
                Assert.That(services.Sum(s=>s.WorkUnits),Is.EqualTo(used));
                for(int c=0;c<3;c++){
                    var s=services[c];var m=samples[c];double total=(double)counts[c]*10800;
                    TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "D3-demand size={0} population={1} distribution={2} mode={3} class={4} consumers={5} publications={6} stale={7} p50={8:F6} p95={9:F6} p99={10:F6} max={11:F6} firstP95={12:F6} ageP50={13:F6} ageP95={14:F6} ageP99={15:F6} coverage={16:F6} noAnswer={17:F6} unreachable={18:F6} pendingMax={19} pendingEnd={20} pendingAgeP95={21:F6} work={22} maintenance={23} init={24} index={25} pops={26} edges={27} seeds={28} publish={29} control={30} hits={31} misses={32} evictions={33} payload={34} cpuMs={35:F6} queueAgeP95={36:F6}",
                        size,population,distribution,mode,c,counts[c],m.Publications,m.StalePublications,m.Response.Q(.5),m.Response.Q(.95),m.Response.Q(.99),m.Response.Q(1),m.First.Q(.95),m.Age.Q(.5),m.Age.Q(.95),m.Age.Q(.99),m.Coverage/total,m.NoAnswer/total,m.Unreachable/total,m.PendingMax,m.PendingEnd,m.PendingAge.Q(.95),s.WorkUnits,s.Maintenance,s.Initialization,s.Indexing,s.Extractions,s.Edges,s.Seeds,s.Publications,s.Control,s.CacheHits,s.CacheMisses,s.Evictions,s.PayloadBytes,s.CpuTicks*1000.0/Stopwatch.Frequency,m.QueueAge.Q(.95)));
                }
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "D3-demand-total size={0} population={1} distribution={2} mode={3} work={4} cpuTotalMs={5:F6} cpuP50Ms={6:F6} cpuP95Ms={7:F6} cpuP99Ms={8:F6} cpuMaxMs={9:F6} setupMs={10:F6} payload={11} allocated={12} managedBefore={13} managedPeak={14}",
                    size,population,distribution,mode,used,cpu.Sum(),Percentile(cpu,.5),Percentile(cpu,.95),Percentile(cpu,.99),cpu.Max(),setup,payload,validAllocation?allocated:-1,managedBefore,managedPeak));
                if(mode=="fixed"){
                    long before=services.Sum(s=>s.WorkUnits),begin=scheduler.TickIndex;var latencies=new Histogram[3];
                    for(int c=0;c<3;c++){latencies[c]=new Histogram();for(int i=0;i<counts[c];i++){var input=services[c].Inputs[i];input.Goal=walks[c][1];input.Since=begin;services[c].Inputs[i]=input;}}
                    int ticks=0;bool pending;
                    do{int work=scheduler.Tick();Assert.That(work,Is.InRange(1,3000));pending=false;for(int c=0;c<3;c++)for(int i=0;i<counts[c];i++)if(services[c].Status(i)==DemandStatus.Pending)pending=true;}while(pending&&++ticks<10800);
                    Assert.That(pending,Is.False);
                    for(int c=0;c<3;c++)for(int i=0;i<counts[c];i++){
                        var answer=services[c].Answers[i];latencies[c].Add(answer.Published-begin);
                        Assert.That(answer.Cost,Is.EqualTo(expected[c][answer.Goal][inverse[c][answer.Source]]));
                    }
                    TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "D3-demand-neighbor size={0} population={1} distribution={2} work={3} ticks={4} sP95={5:F6} mP95={6:F6} lP95={7:F6}",
                        size,population,distribution,services.Sum(s=>s.WorkUnits)-before,scheduler.TickIndex-begin,latencies[0].Q(.95),latencies[1].Q(.95),latencies[2].Q(.95)));
                }
            }
        }
    }
}
