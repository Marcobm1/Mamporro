using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalTransferTests
    {
        static IEnumerable<TestCaseData> Corpus()
        {foreach(string seed in new[]{"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        static int[] Oracle(NavPortalWorld w,int source,int region)
        {
            // Relajación por cola independiente: no usa el heap/caché del sistema.
            var d=new int[w.NodeCount];var queued=new bool[d.Length];for(int n=0;n<d.Length;n++)d[n]=int.MaxValue;
            var q=new Queue<int>();q.Enqueue(source);d[source]=0;queued[source]=true;
            while(q.Count>0){int a=q.Dequeue();queued[a]=false;
                for(int e=w.EdgeStart[a];e<w.EdgeStart[a+1];e++){
                    int b=w.EdgeTo[e];if(region>=0&&w.NodeRegion[b]!=region)continue;
                    int candidate=d[a]+w.EdgeCost[e];if(candidate>=d[b])continue;d[b]=candidate;
                    if(!queued[b]){queued[b]=true;q.Enqueue(b);}
                }
            }return d;
        }
        static void Finish(RegionalNavScheduler scheduler,RegionalTransferCache cache,int source,int region)
        {
            int ticks=0;
            while(cache.State(source,region)==RegionalTransferState.Pending){
                Assert.That(scheduler.Tick(),Is.InRange(1,3000));Assert.That(++ticks,Is.LessThan(10000));
            }
        }
        static void Check(NavPortalWorld w,RegionalTransferCache cache,int source,int region)
        {
            var expected=Oracle(w,source,region);
            for(int target=0;target<w.NodeCount;target++){
                Assert.That(cache.TryDistance(source,target,region,out int d),Is.True);Assert.That(d,Is.EqualTo(expected[target]));
                Assert.That(cache.TryParent(source,target,region,out int e),Is.True);
                if(target==source||d==int.MaxValue){Assert.That(e,Is.EqualTo(-1));continue;}
                Assert.That(w.EdgeTo[e],Is.EqualTo(target));int parent=w.EdgeFrom[e];
                Assert.That(cache.TryDistance(source,parent,region,out int pd),Is.True);
                Assert.That(pd+w.EdgeCost[e],Is.EqualTo(d));Assert.That(pd,Is.LessThan(d));
            }
        }
        [TestCaseSource(nameof(Corpus))]
        public void SlicedTransfersMatchExactDirectedCosts(string seed,int size)
        {
            var c=seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
            foreach(var body in NavClass.All){
                var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size);
                int source=w.Portals.Length>0?w.Portals[w.Portals.Length/2].To:w.NodeCount/2;
                foreach(int region in new[]{w.NodeRegion[source],-1}){
                    var scheduler=new RegionalNavScheduler(2);var cache=new RegionalTransferCache(w,scheduler,2,worldControl:region<0);
                    long before=cache.Initializations+cache.Extractions+cache.Relaxations+cache.Publications;
                    Assert.That(cache.Request(source,region),Is.True);Assert.That(cache.TryDistance(source,source,region,out _),Is.False);
                    var samples=new double[10000];
                    long probeBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];
                    bool allocationValid=GC.GetAllocatedBytesForCurrentThread()-probeBefore>=4096;GC.KeepAlive(probe);
                    long allocated=GC.GetAllocatedBytesForCurrentThread();
                    long start=scheduler.TickIndex,timer=Stopwatch.GetTimestamp(),work=0;int ticks=0;
                    while(cache.State(source,region)==RegionalTransferState.Pending&&ticks<samples.Length){
                        long t=Stopwatch.GetTimestamp();work+=scheduler.Tick();samples[ticks++]=(Stopwatch.GetTimestamp()-t)*1000.0/Stopwatch.Frequency;
                    }
                    double ms=(Stopwatch.GetTimestamp()-timer)*1000.0/Stopwatch.Frequency;
                    allocated=allocationValid?GC.GetAllocatedBytesForCurrentThread()-allocated:-1;
                    Assert.That(ticks,Is.LessThan(samples.Length));if(allocationValid)Assert.That(allocated,Is.Zero);
                    Array.Sort(samples,0,ticks);
                    Assert.That(work,Is.EqualTo(cache.Initializations+cache.Extractions+cache.Relaxations+cache.Publications-before));
                    Check(w,cache,source,region);
                    long builds=cache.Started;timer=Stopwatch.GetTimestamp();
                    for(int i=0;i<100;i++){cache.Request(source,region);cache.TryDistance(source,source,region,out _);}
                    double warmUs=(Stopwatch.GetTimestamp()-timer)*1e6/Stopwatch.Frequency/100;
                    Assert.That(cache.Started,Is.EqualTo(builds));Assert.That(scheduler.Tick(),Is.Zero);
                    TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "D3-transfer {0} size={1} class={2} region={3} nodes={4} work={5} ticks={6} cpuMs={7:F3} warmUs={8:F3} payload={9} latencySeconds60Hz={10:F3} tickP95Ms={11:F4} tickMaxMs={12:F4} allocated={13} initialized={14}",
                        seed,size,body.Index,region,w.NodeCount,work,ticks,ms,warmUs,cache.PayloadBytes,(cache.PublishedTick(source,region)-start)/60.0,
                        samples[(int)Math.Ceiling(ticks*.95)-1],samples[ticks-1],allocated,cache.Initializations));
                }
            }
        }
        static NavPortalWorld Flat(int kind=0)
        {
            var t=new Heightfield(32,32,new float[33*33]);var q=new VerticalQueries(t,Tuning.PlayerMaxSlopeDeg);
            return new NavPortalWorld(q,t,(x,z,r)=>Math.Abs(x)+r<14&&Math.Abs(z)+r<14,NavClass.All[kind],16);
        }
        [Test]
        public void CacheCoalescesWithoutRestartAndEvictsOnlyCompletedLeastRecent()
        {
            var w=Flat();var s=new RegionalNavScheduler(2);var c=new RegionalTransferCache(w,s,2,worldControl:true);
            Assert.That(c.State(-1),Is.EqualTo(RegionalTransferState.Missing));
            c.Request(0);c.Request(1);Assert.That(c.Request(2),Is.False);
            s.Tick();for(int i=0;i<750;i++)Assert.That(c.Request(0),Is.True);
            Assert.That(c.Started,Is.EqualTo(2));Assert.That(c.Coalesced,Is.EqualTo(750));
            Finish(s,c,0,-1);Finish(s,c,1,-1);Check(w,c,0,-1);Check(w,c,1,-1);
            c.Request(0);Assert.That(c.Request(2),Is.True);
            Assert.That(c.State(0),Is.EqualTo(RegionalTransferState.Ready));Assert.That(c.State(1),Is.EqualTo(RegionalTransferState.Missing));
            Assert.That(c.TryDistance(2,2,-1,out _),Is.False);Finish(s,c,2,-1);Check(w,c,2,-1);
            Assert.That(c.Evictions,Is.EqualTo(1));Assert.That(c.Rejected,Is.EqualTo(1));
        }
        [Test]
        public void CompactBuffersSurviveRegionChangesWithoutLeakingOldDistances()
        {
            var w=Flat();var s=new RegionalNavScheduler(1);var c=new RegionalTransferCache(w,s,1);
            Assert.Throws<ArgumentException>(()=>c.Request(0));
            int previous=-1;
            for(int r=0;r<w.Regions.Length;r++){
                int source=Array.FindIndex(w.NodeRegion,n=>n==r);if(source<0)continue;
                long before=c.Initializations;c.Request(source,r);Finish(s,c,source,r);Check(w,c,source,r);
                Assert.That(c.Initializations-before,Is.EqualTo(w.Regions[r].OwnedNodes+1));
                if(previous>=0){c.TryDistance(source,previous,r,out int d);Assert.That(d,Is.EqualTo(int.MaxValue));}
                previous=source;
            }
            Assert.That(c.PayloadBytes,Is.LessThan(w.NodeCount*16L));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InternalTransferDoesNotInventRouteThatRequiresLeavingAndReturning(int kind)
        {
            var t=new Heightfield(80,80,new float[81*81]);
            var q=new VerticalQueries(t,Tuning.PlayerMaxSlopeDeg,new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-17,0,-40),new Vec3(-15,20,-7)));
            var w=new NavPortalWorld(q,t,(x,z,r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36,NavClass.All[kind],16);
            int a=w.NodeAt(-21,0,-16),b=w.NodeAt(-11,0,-16),region=w.NodeRegion[a];
            Assert.That(w.NodeRegion[b],Is.EqualTo(region));
            var s=new RegionalNavScheduler(2);var c=new RegionalTransferCache(w,s,2,worldControl:true);c.Request(a,region);c.Request(a);
            Finish(s,c,a,region);Finish(s,c,a,-1);
            Assert.That(c.TryDistance(a,b,region,out int local),Is.True);Assert.That(local,Is.EqualTo(int.MaxValue));
            Assert.That(c.TryDistance(a,b,-1,out int global),Is.True);Assert.That(global,Is.LessThan(int.MaxValue));
            Check(w,c,a,region);Check(w,c,a,-1);
        }
        sealed class Work : IRegionalNavWork
        {
            public int ClassIndex {get;set;}
            public bool Pending=>Remaining>0;
            public long RequestedTick {get;set;}
            public int Priority {get;set;}
            public int Remaining=int.MaxValue;
            public long Used;
            public int Advance(int budget){int n=Math.Min(Remaining,budget);Remaining-=n;Used+=n;return n;}
        }
        [Test]
        public void EveryClassGetsItsQuotaFor180SecondsOfSustainedWork()
        {
            var s=new RegionalNavScheduler(3);var jobs=new Work[3];
            for(int c=0;c<3;c++){jobs[c]=new Work{ClassIndex=c};s.Register(jobs[c]);}
            for(int tick=0;tick<180*60;tick++){
                Assert.That(s.Tick(),Is.EqualTo(3000));
                for(int c=0;c<3;c++)Assert.That(s.ClassUsed[c],Is.EqualTo(1000));
            }
            for(int c=0;c<3;c++)Assert.That(jobs[c].Used,Is.EqualTo(10800000));
        }
        [Test]
        public void OldRequestOvertakesHighPriorityAndIdleClassesDonateBudget()
        {
            var s=new RegionalNavScheduler(2);var old=new Work{ClassIndex=2,Remaining=1};
            var urgent=new Work{ClassIndex=2,Priority=10};s.Register(old);s.Register(urgent);
            for(int tick=1;tick<RegionalNavScheduler.AgingTicks;tick++){urgent.RequestedTick=s.TickIndex;s.Tick();Assert.That(old.Used,Is.Zero);}
            urgent.RequestedTick=s.TickIndex;Assert.That(s.Tick(),Is.EqualTo(3000));Assert.That(old.Used,Is.EqualTo(1));
            Assert.That(s.ClassUsed[2],Is.EqualTo(3000));
        }
        [Test]
        public void QuotaIsRedistributedAndPublicationRemainsAtomic()
        {
            var w=Flat();var s=new RegionalNavScheduler(1);var c=new RegionalTransferCache(w,s,1,worldControl:true);
            c.Request(0);Assert.That(c.TryDistance(0,0,-1,out _),Is.False);
            Assert.That(s.Tick(),Is.EqualTo(3000));Assert.That(s.ClassUsed[0],Is.EqualTo(3000));
            Finish(s,c,0,-1);Check(w,c,0,-1);
        }
    }
}
