using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalBatchTests
    {
        static IEnumerable<TestCaseData> Corpus()
        {foreach(string seed in new[]{"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        static VerticalCircuit Circuit(string seed)=>seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
        static NavPortalGroups CheckGroups(NavPortalWorld w,string label)
        {
            var watch=Stopwatch.StartNew();var groups=new NavPortalGroups(w);double ms=watch.Elapsed.TotalMilliseconds;
            var again=new NavPortalGroups(w);Assert.That(groups.Start,Is.EqualTo(again.Start));Assert.That(groups.Members,Is.EqualTo(again.Members));
            var seen=new bool[w.Portals.Length];var sizes=new List<int>();var kinds=new int[3];var physical=new int[3];
            foreach(var p in w.Portals)physical[(int)p.Kind]++;
            for(int g=0;g<groups.Count;g++){
                int begin=groups.Start[g],end=groups.Start[g+1];Assert.That(end,Is.GreaterThan(begin));sizes.Add(end-begin);
                var first=w.Portals[groups.Members[begin]];kinds[(int)first.Kind]++;
                for(int i=begin;i<end;i++){
                    int id=groups.Members[i];Assert.That(seen[id],Is.False);seen[id]=true;
                    var p=groups.World.Portals[id];Assert.That(p,Is.EqualTo(w.Portals[id]));
                    Assert.That(NavPortalGroups.Signature(w,p),Is.EqualTo(NavPortalGroups.Signature(w,first)));
                    Assert.That(p.LocalEdge,Is.InRange(w.Regions[p.FromRegion].Graph.EdgeStart[p.LocalFrom],w.Regions[p.FromRegion].Graph.EdgeStart[p.LocalFrom+1]-1));
                }
            }
            Assert.That(seen.All(x=>x),Is.True);sizes.Sort();
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "D3-groups seed={0} size={1} class={2} physical={3} groups={4} p50={5} p95={6} max={7} walk={8} climb={9} drop={10} physicalWalk={11} physicalClimb={12} physicalDrop={13} buildMs={14:F4} payload={15}",
                label,w.Size,w.Class.Index,w.Portals.Length,groups.Count,Quantile(sizes,.5),Quantile(sizes,.95),Quantile(sizes,1),kinds[0],kinds[1],kinds[2],physical[0],physical[1],physical[2],ms,groups.PayloadBytes));
            return groups;
        }
        static int Quantile(List<int> a,double p)=>a.Count==0?0:a[(int)Math.Ceiling(a.Count*p)-1];
        [TestCaseSource(nameof(Corpus)),Category("BatchGrouping")]
        public void GroupsExpandExactlyToD2Corpus(string seed,int size)
        {
            var c=Circuit(seed);
            foreach(var body in NavClass.All)CheckGroups(new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size),seed);
        }

        static void Finish(RegionalBatchField f)
        {for(int i=0;f.Computing&&i<100000;i++)Assert.That(f.Advance(3000),Is.InRange(1,3000));Assert.That(f.Computing,Is.False);}
        static int[] Oracle(NavGraph g,int goal)
        {
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var queued=new bool[g.NodeCount];var q=new Queue<int>();
            d[goal]=0;q.Enqueue(goal);queued[goal]=true;
            while(q.Count>0){int b=q.Dequeue();queued[b]=false;
                for(int k=g.RevStart[b];k<g.RevStart[b+1];k++){int e=g.RevEdge[k],a=g.EdgeFrom[e],v=checked(d[b]+g.EdgeCost[e]);
                    if(v>=d[a])continue;d[a]=v;if(!queued[a]){q.Enqueue(a);queued[a]=true;}}}
            return d;
        }
        static void CheckField(RegionalBatchField f,NavGraph full)
        {
            var w=f.World;var map=new int[full.NodeCount];var inverse=new int[w.NodeCount];
            Assert.That(w.NodeCount,Is.EqualTo(full.NodeCount));
            for(int n=0;n<map.Length;n++){map[n]=w.Find(NavPortalWorld.NodeKey(full,n));Assert.That(map[n],Is.GreaterThanOrEqualTo(0));inverse[map[n]]=n;}
            var expected=Oracle(full,inverse[f.Goal]);
            for(int n=0;n<w.NodeCount;n++){
                Assert.That(f.TryDistance(n,out int d),Is.True);Assert.That(d,Is.EqualTo(expected[inverse[n]]),$"nodo={n}, meta={f.Goal}");
                Assert.That(f.TryNext(n,out int edge),Is.True);
                if(n==f.Goal||d==int.MaxValue){Assert.That(edge,Is.EqualTo(-1));continue;}
                // Desempate canónico entre todas las transiciones óptimas del oráculo.
                int best=-1;
                for(int e=w.EdgeStart[n];e<w.EdgeStart[n+1];e++){
                    int next=expected[inverse[w.EdgeTo[e]]];if(next!=int.MaxValue&&next+w.EdgeCost[e]==d){best=e;break;}}
                Assert.That(edge,Is.EqualTo(best));Assert.That(best,Is.GreaterThanOrEqualTo(0));
                Assert.That(w.EdgeCost[edge],Is.GreaterThan(0));
                var g=w.Graph(n);int local=w.EdgeLocal[edge];
                Assert.That(w.Find(NavPortalWorld.NodeKey(g,g.EdgeTo[local])),Is.EqualTo(w.EdgeTo[edge]));
                Assert.That(g.EdgeCost[local],Is.EqualTo(w.EdgeCost[edge]));
                // La arista original recupera PathStart y toda la trayectoria física D2.
                int path=g.EdgePath[local];if(path>=0)Assert.That(g.PathStart[path+1],Is.GreaterThan(g.PathStart[path]));
            }
            // Reconstrucción explícita además de la prueba decreciente en TODOS los nodos.
            for(int k=0;k<16;k++){int a=k*(w.NodeCount-1)/15;f.TryDistance(a,out int cost);if(cost==int.MaxValue)continue;
                int total=0,steps=0;while(a!=f.Goal){Assert.That(++steps,Is.LessThanOrEqualTo(w.NodeCount));f.TryNext(a,out int e);total+=w.EdgeCost[e];a=w.EdgeTo[e];}
                Assert.That(total,Is.EqualTo(cost));}
        }
        static void CheckGoals(NavPortalWorld w,NavGraph full,params int[] goals)
        {
            var f=new RegionalBatchField(new NavPortalGroups(w));
            foreach(int goal in goals.Distinct()){
                Assert.That(goal,Is.GreaterThanOrEqualTo(0));int version=f.Version,previous=f.Goal;
                Assert.That(f.Request(goal),Is.True);f.Advance(1);
                if(version>0){Assert.That(f.Version,Is.EqualTo(version));Assert.That(f.Goal,Is.EqualTo(previous));CheckField(f,full);}
                Finish(f);Assert.That(f.Goal,Is.EqualTo(goal));CheckField(f,full);
                long units=f.WorkUnits;Assert.That(f.Request(goal),Is.True);Assert.That(f.Advance(3000),Is.Zero);Assert.That(f.WorkUnits,Is.EqualTo(units));
            }
        }
        [TestCaseSource(nameof(Corpus)),Category("BatchEquivalence")]
        public void BatchedFieldsMatchMonolithicOracle(string seed,int size)
        {
            var c=Circuit(seed);
            foreach(var body in NavClass.All){var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size);
                var full=c.Map==null?new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,body):c.Map.Graphs[body.Index];
                int goal=w.NodeCount/2,next=w.EdgeStart[goal]<w.EdgeStart[goal+1]?w.EdgeTo[w.EdgeStart[goal]]:0,high=0;
                for(int n=1;n<w.NodeCount;n++)if(w.Position(n).Y>w.Position(high).Y)high=n;
                CheckGoals(w,full,goal,next,high,0);
            }
        }
        [TestCase(16),TestCase(32),TestCase(64),Category("BatchEquivalence")]
        public void PhysicalFixturesRetainLayersDirectionsAndSizeRestrictions(int size)
        {
            foreach(var body in NavClass.All){
                var terrain=new Heightfield(80,80,new float[81*81]);
                Func<double,double,double,bool> inside=(x,z,r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36;
                var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-17,0,-40),new Vec3(-15,20,-7)),
                    new VerticalSolid(2,new Vec3(8,0,8),new Vec3(24,6,24)),
                    new VerticalSolid(3,new Vec3(7.75,6,7.75),new Vec3(24.25,7,24.25)),
                    new VerticalSolid(4,new Vec3(-8,13,8),new Vec3(0,14,16)));
                var w=new NavPortalWorld(q,terrain,inside,body,size);var full=new NavGraph(q,terrain,inside,body);
                CheckGoals(w,full,w.NodeAt(-21,0,-16),w.NodeAt(-11,0,-16),w.NodeAt(16,7,16),w.NodeAt(-4,14,12));
                var path=new List<int>();Assert.That(new NavPortalQuery(w).Route(w.NodeAt(-21,0,-16),w.NodeAt(-11,0,-16),path),Is.True);
                if(size==16)Assert.That(path.Count(e=>w.NodeRegion[w.EdgeFrom[e]]!=w.NodeRegion[w.EdgeTo[e]]),Is.GreaterThanOrEqualTo(2));
                var heights=new float[81*81];for(int z=0;z<=80;z++)for(int x=0;x<=80;x++)heights[z*81+x]=(float)Math.Max(0,Math.Min(6,(x+z-60)*.2));
                terrain=new Heightfield(80,80,heights);q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,new Vec3(-14,7,-14),new Vec3(6,8,6)));
                w=new NavPortalWorld(q,terrain,inside,body,size);full=new NavGraph(q,terrain,inside,body);
                CheckGoals(w,full,w.NodeCount/2,0,w.NodeCount-1);
                terrain=new Heightfield(80,80,new float[81*81]);q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-40,0,-1),new Vec3(-1.5,20,1)),
                    new VerticalSolid(2,VerticalSurfaceKind.Foliage,new Vec3(1.5,0,-1),new Vec3(40,20,1)));
                w=new NavPortalWorld(q,terrain,inside,body,size);full=new NavGraph(q,terrain,inside,body);
                int a=w.NodeAt(0,0,-10),b=w.NodeAt(0,0,10);CheckGoals(w,full,a,b);
                Assert.That(new NavPortalQuery(w).Reachable(a,b),Is.EqualTo(body.Index<2));
            }
        }

        // Adaptador exclusivo de pruebas: nunca registrado por el movimiento QA.
        sealed class Scheduled : IRegionalNavWork
        {
            public readonly RegionalBatchField Field;
            readonly RegionalNavScheduler scheduler;
            public int ClassIndex=>Field.World.Class.Index;
            public bool Pending=>Field.Computing;
            public long RequestedTick {get;private set;}
            public long RequestedWork {get;private set;}
            public long CpuTicks {get;private set;}
            public double SetupMs {get;private set;}
            public int Priority=>0;
            public Scheduled(NavPortalGroups groups,RegionalNavScheduler scheduler)
            {long start=Stopwatch.GetTimestamp();Field=new RegionalBatchField(groups);this.scheduler=scheduler;scheduler.Register(this);SetupMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;}
            public void Request(int goal)
            {if(!Pending&&(Field.Version==0||Field.Goal!=goal)){RequestedTick=scheduler.TickIndex;RequestedWork=Field.WorkUnits;}Field.Request(goal);}
            public int Advance(int budget){long start=Stopwatch.GetTimestamp();int used=Field.Advance(budget);CpuTicks+=Stopwatch.GetTimestamp()-start;return used;}
        }
        static double P(List<double> a,double p){if(a.Count==0)return -1;a.Sort();return a[(int)Math.Ceiling(a.Count*p)-1];}
        static long[] Counters(RegionalBatchField f)=>new[]{f.WorkUnits,f.Initialization,f.Indexing,f.Extractions,f.Edges,f.Seeds,f.Propagation,f.Batches,f.Publications,f.Control,f.BatchSeedTotal};
        static void Measure(RegionalNavScheduler scheduler,Scheduled[] jobs,int[,] goals,int size,int repeat,string mode)
        {
            var before=jobs.Select(j=>Counters(j.Field)).ToArray();
            var cpuBefore=jobs.Select(j=>j.CpuTicks).ToArray();
            var regionBefore=jobs.Select(j=>(long[])j.Field.RegionUnits.Clone()).ToArray();
            var batchesBefore=jobs.Select(j=>(long[])j.Field.RegionBatches.Clone()).ToArray();
            var seedsBefore=jobs.Select(j=>(long[])j.Field.RegionSeeds.Clone()).ToArray();
            var latencies=new List<double>[3];var publicationWork=new List<double>[3];var ages=new List<double>[3];var publishedRequest=new long[3];
            var versions=jobs.Select(j=>j.Field.Version).ToArray();
            for(int c=0;c<3;c++){latencies[c]=new List<double>();publicationWork[c]=new List<double>();ages[c]=new List<double>();}
            var cpu=new List<double>();long used=0,allocated=0;double cpuTotal=0;
            long allocationBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];bool valid=GC.GetAllocatedBytesForCurrentThread()-allocationBefore>=4096;GC.KeepAlive(probe);
            for(int tick=0;tick<10800;tick++){
                allocationBefore=GC.GetAllocatedBytesForCurrentThread();long start=Stopwatch.GetTimestamp();
                for(int c=0;c<3;c++)jobs[c].Request(goals[c,mode=="moving"?(tick/12+1)%2:mode=="neighbor"?1:mode=="height"?2:0]);
                int work=scheduler.Tick();double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                allocated+=GC.GetAllocatedBytesForCurrentThread()-allocationBefore;cpu.Add(ms);cpuTotal+=ms;used+=work;
                Assert.That(work,Is.InRange(0,3000));
                for(int c=0;c<3;c++){var job=jobs[c];var f=job.Field;
                    if(f.Version!=versions[c]){latencies[c].Add((scheduler.TickIndex-job.RequestedTick)/60.0);publicationWork[c].Add(f.WorkUnits-job.RequestedWork);versions[c]=f.Version;publishedRequest[c]=job.RequestedTick;}
                    if(mode=="moving"&&latencies[c].Count>0)ages[c].Add((scheduler.TickIndex-publishedRequest[c])/60.0);
                }
                if(mode!="moving"&&jobs.All(j=>!j.Pending))break;
            }
            Assert.That(jobs.Sum(j=>j.Field.WorkUnits)-before.Sum(a=>a[0]),Is.EqualTo(used));
            for(int c=0;c<3;c++){
                var f=jobs[c].Field;var delta=Counters(f);for(int i=0;i<delta.Length;i++)delta[i]-=before[c][i];
                Assert.That(delta[0],Is.EqualTo(delta.Skip(1).Take(9).Sum()));
                int regions=0;for(int r=0;r<f.World.Regions.Length;r++){
                    long batches=f.RegionBatches[r]-batchesBefore[c][r];if(batches>0)regions++;
                    TestContext.Out.WriteLine($"D3-batch-region size={size} repeat={repeat} mode={mode} class={c} region={r} work={f.RegionUnits[r]-regionBefore[c][r]} batches={batches} seeds={f.RegionSeeds[r]-seedsBefore[c][r]}");
                }
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "D3-batch size={0} repeat={1} mode={2} class={3} publications={4} latencyP50={5:F6} latencyP95={6:F6} latencyMax={7:F6} unitsP50={8:F0} unitsP95={9:F0} unitsMax={10:F0} work={11} init={12} index={13} pops={14} edges={15} seeds={16} propagation={17} batches={18} control={19} regions={20} batchSeedTotal={21} batchSeedMaxLifetime={22} physical={23} groups={24} payload={25} pending={26} ageP95={27:F6} ageMax={28:F6} cpuMs={29:F6} setupMs={30:F6}",
                    size,repeat,mode,c,delta[8],P(latencies[c],.5),P(latencies[c],.95),P(latencies[c],1),P(publicationWork[c],.5),P(publicationWork[c],.95),P(publicationWork[c],1),delta[0],delta[1],delta[2],delta[3],delta[4],delta[5],delta[6],delta[7],delta[9],regions,delta[10],f.MaxBatchSeeds,f.World.Portals.Length,f.Groups.Count,f.PayloadBytes+f.Groups.PayloadBytes,f.Computing?1:0,P(ages[c],.95),P(ages[c],1),(jobs[c].CpuTicks-cpuBefore[c])*1000.0/Stopwatch.Frequency,jobs[c].SetupMs));
            }
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "D3-batch-total size={0} repeat={1} mode={2} work={3} ticks={4} cpuTotalMs={5:F6} tickP50Ms={6:F6} tickP95Ms={7:F6} tickP99Ms={8:F6} tickMaxMs={9:F6} allocated={10}",
                size,repeat,mode,used,cpu.Count,cpuTotal,P(cpu,.5),P(cpu,.95),P(cpu,.99),P(cpu,1),valid?allocated:-1));
        }
        [TestCase(16),TestCase(32),TestCase(64),Category("BatchBudget")]
        public void MeasureThreeClassesUnderGlobalBudget(int size)
        {
            var circuit=Circuit("HORDA750");var groups=new NavPortalGroups[3];var goals=new int[3,3];
            for(int c=0;c<3;c++){
                var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[c],size);groups[c]=new NavPortalGroups(w);
                int a=w.NodeAt(0,0,0);if(a<0)a=w.NodeCount/2;int b=-1,high=a;
                for(int e=w.EdgeStart[a];e<w.EdgeStart[a+1];e++){
                    if((NavEdgeKind)w.Graph(a).EdgeKind[w.EdgeLocal[e]]!=NavEdgeKind.Walk)continue;
                    int candidate=w.EdgeTo[e];for(int back=w.EdgeStart[candidate];back<w.EdgeStart[candidate+1];back++)if(w.EdgeTo[back]==a)b=candidate;
                    if(b>=0)break;
                }
                for(int n=0;n<w.NodeCount;n++)if(w.Position(n).Y>w.Position(high).Y)high=n;
                Assert.That(b,Is.GreaterThanOrEqualTo(0));goals[c,0]=a;goals[c,1]=b;goals[c,2]=high;
            }
            for(int repeat=0;repeat<3;repeat++){
                var scheduler=new RegionalNavScheduler(3);var jobs=groups.Select(g=>new Scheduled(g,scheduler)).ToArray();
                foreach(string mode in new[]{"cold","same","neighbor","height","return","moving"}){
                    Measure(scheduler,jobs,goals,size,repeat,mode);
                    for(int i=0;jobs.Any(j=>j.Pending)&&i<100000;i++)Assert.That(scheduler.Tick(),Is.InRange(1,3000));
                    foreach(var j in jobs){Assert.That(j.Pending,Is.False);CheckField(j.Field,circuit.Map.Graphs[j.ClassIndex]);}
                }
            }
        }
    }
}
