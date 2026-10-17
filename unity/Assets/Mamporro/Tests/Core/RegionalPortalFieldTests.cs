using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalPortalFieldTests
    {
        static int[] Oracle(NavPortalWorld w,int goal)
        {
            var incoming=new List<int>[w.NodeCount];var d=new int[w.NodeCount];var queued=new bool[w.NodeCount];
            for(int n=0;n<d.Length;n++)d[n]=int.MaxValue;
            for(int e=0;e<w.EdgeCount;e++){int b=w.EdgeTo[e];if(incoming[b]==null)incoming[b]=new List<int>();incoming[b].Add(e);}
            var queue=new Queue<int>();queue.Enqueue(goal);d[goal]=0;queued[goal]=true;
            while(queue.Count>0){int b=queue.Dequeue();queued[b]=false;if(incoming[b]==null)continue;
                foreach(int e in incoming[b]){int a=w.EdgeFrom[e],candidate=d[b]+w.EdgeCost[e];
                    if(candidate>=d[a])continue;d[a]=candidate;if(!queued[a]){queue.Enqueue(a);queued[a]=true;}
                }
            }return d;
        }
        static void Check(NavPortalWorld w,RegionalPortalField f,int goal)
        {
            var expected=Oracle(w,goal);var exits=new Dictionary<int,int>();
            foreach(var p in w.Portals){
                int cost=expected[p.To]==int.MaxValue?int.MaxValue:expected[p.To]+p.Cost;
                if(p.From==goal)cost=0;
                if(!exits.TryGetValue(p.From,out int old)||cost<old)exits[p.From]=cost;
            }
            foreach(var p in exits){Assert.That(f.TryExitCost(p.Key,out int cost),Is.True);Assert.That(cost,Is.EqualTo(p.Value),$"salida {p.Key}");}
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExactExitCostsPublishAtomicallyAndKeepPreviousGoal(int kind)
        {
            var terrain=new Heightfield(32,32,new float[33*33]);var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg);
            var w=new NavPortalWorld(q,terrain,(x,z,r)=>Math.Abs(x)+r<10&&Math.Abs(z)+r<10,NavClass.All[kind],16);
            var scheduler=new RegionalNavScheduler(5);var f=new RegionalPortalField(w,scheduler);
            int goal=w.NodeAt(4,0,4),next=w.NodeAt(-4,0,-4);Assert.That(goal,Is.GreaterThanOrEqualTo(0));
            f.Request(goal);Assert.That(f.TryExitCost(goal,out _),Is.False);
            for(int tick=0;f.Computing&&tick<100000;tick++){
                Assert.That(f.Request(next),Is.False);Assert.That(scheduler.Tick(),Is.InRange(1,3000));
            }
            Assert.That(f.Computing,Is.False);Check(w,f,goal);
            long published=f.PublishedTick;f.Request(goal);Assert.That(scheduler.Tick(),Is.Zero);Assert.That(f.PublishedTick,Is.EqualTo(published));
            f.Request(next);scheduler.Tick();Assert.That(f.Goal,Is.EqualTo(goal));Check(w,f,goal);
            for(int tick=0;f.Computing&&tick<100000;tick++)Assert.That(scheduler.Tick(),Is.InRange(1,3000));
            Assert.That(f.Computing,Is.False);Assert.That(f.Version,Is.EqualTo(2));Check(w,f,next);
        }
        [TestCase(16)] [TestCase(32)] [TestCase(64)]
        public void MeasureColdExactHierarchyWithThreeClassesFor180Seconds(int size)
        {
            var circuit=new VerticalCircuit(new ProceduralMap("HORDA750"));
            var scheduler=new RegionalNavScheduler(15);var fields=new RegionalPortalField[3];
            for(int c=0;c<3;c++){
                var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[c],size);
                fields[c]=new RegionalPortalField(w,scheduler);
                int goal=w.NodeAt(0,0,0);if(goal<0)goal=w.NodeCount/2;
                fields[c].Request(goal);
            }
            var samples=new double[10800];long total=0;
            long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];
            bool valid=GC.GetAllocatedBytesForCurrentThread()-before>=4096;GC.KeepAlive(probe);
            before=GC.GetAllocatedBytesForCurrentThread();
            for(int tick=0;tick<samples.Length;tick++){
                long timer=Stopwatch.GetTimestamp();int used=scheduler.Tick();samples[tick]=(Stopwatch.GetTimestamp()-timer)*1000.0/Stopwatch.Frequency;
                total+=used;Assert.That(used,Is.InRange(0,3000));
            }
            long allocated=valid?GC.GetAllocatedBytesForCurrentThread()-before:-1;Array.Sort(samples);
            long accounted=0;
            foreach(var f in fields){var t=f.Transfers;accounted+=f.WorkUnits+t.Initializations+t.Extractions+t.Relaxations+t.Publications;
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "D3-hierarchy size={0} class={1} exits={2} version={3} publishedTick={4} work={5} transfersStarted={6} transfersCompleted={7} hits={8} evictions={9} payload={10}",
                    size,f.ClassIndex,f.ExitCount,f.Version,f.PublishedTick,f.WorkUnits+t.Initializations+t.Extractions+t.Relaxations+t.Publications,t.Started,t.Completed,t.Hits,t.Evictions,f.PayloadBytes));
                if(f.Version>0)Check(f.World,f,f.Goal);
            }
            Assert.That(total,Is.EqualTo(accounted));
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "D3-hierarchy-total size={0} work={1} tickP50Ms={2:F4} tickP95Ms={3:F4} tickP99Ms={4:F4} tickMaxMs={5:F4} allocatedWithTestAssertions={6}",
                size,total,samples[5399],samples[10259],samples[10691],samples[10799],allocated));
        }
    }
}
