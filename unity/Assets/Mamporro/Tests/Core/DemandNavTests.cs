using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class DemandNavTests
    {
        static IEnumerable<TestCaseData> Corpus()
        {foreach(string seed in new[]{"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size);}
        static VerticalCircuit Circuit(string seed)=>seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
        static int[] Oracle(NavGraph g,int goal)
        {
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var inQueue=new bool[g.NodeCount];var queue=new Queue<int>();
            d[goal]=0;queue.Enqueue(goal);inQueue[goal]=true;
            while(queue.Count>0){int b=queue.Dequeue();inQueue[b]=false;
                for(int k=g.RevStart[b];k<g.RevStart[b+1];k++){int e=g.RevEdge[k],a=g.EdgeFrom[e],v=checked(d[b]+g.EdgeCost[e]);
                    if(v>=d[a])continue;d[a]=v;if(!inQueue[a]){inQueue[a]=true;queue.Enqueue(a);}}}
            return d;
        }
        static int[] Mapping(NavPortalWorld w,NavGraph full)
        {var inverse=new int[w.NodeCount];Assert.That(w.NodeCount,Is.EqualTo(full.NodeCount));for(int n=0;n<full.NodeCount;n++){int a=w.Find(NavPortalWorld.NodeKey(full,n));Assert.That(a,Is.GreaterThanOrEqualTo(0));inverse[a]=n;}return inverse;}
        static void CheckAnswer(DemandNavService s,int id,NavGraph full,int[] inverse,int[] expected,bool d2)
        {
            var w=s.World;var a=s.Answers[id];int cost=expected[inverse[a.Source]];
            Assert.That(a.Status,Is.EqualTo(cost==int.MaxValue?DemandStatus.Unreachable:DemandStatus.Reachable));
            Assert.That(a.Cost,Is.EqualTo(cost));
            if(d2)Assert.That(new NavPortalQuery(w).Distance(a.Source,a.Goal),Is.EqualTo(cost));
            if(cost==int.MaxValue)return;
            int node=a.Source,total=0,steps=0;
            while(node!=a.Goal){Assert.That(++steps,Is.LessThanOrEqualTo(w.NodeCount));Assert.That(s.TryRouteEdge(a,node,out int e),Is.True);
                Assert.That(w.EdgeFrom[e],Is.EqualTo(node));int next=w.EdgeTo[e],local=w.EdgeLocal[e];var graph=w.Graph(node);
                Assert.That(w.EdgeCost[e]+expected[inverse[next]],Is.EqualTo(expected[inverse[node]]));
                int canonical=-1;for(int k=w.EdgeStart[node];k<w.EdgeStart[node+1];k++)if(expected[inverse[w.EdgeTo[k]]]!=int.MaxValue&&w.EdgeCost[k]+expected[inverse[w.EdgeTo[k]]]==expected[inverse[node]]){canonical=k;break;}
                Assert.That(e,Is.EqualTo(canonical));
                Assert.That(graph.EdgeCost[local],Is.EqualTo(w.EdgeCost[e]));
                int matching=-1;for(int k=full.EdgeStart[inverse[node]];k<full.EdgeStart[inverse[node]+1];k++)if(full.EdgeTo[k]==inverse[next]&&full.EdgeCost[k]==w.EdgeCost[e]&&full.EdgeKind[k]==graph.EdgeKind[local]){matching=k;break;}
                Assert.That(matching,Is.GreaterThanOrEqualTo(0));
                // La ruta referencia la arista física original y todos sus puntos, sin invertirla.
                int path=graph.EdgePath[local];if(path>=0)Assert.That(graph.PathStart[path+1],Is.GreaterThan(graph.PathStart[path]));
                total+=w.EdgeCost[e];node=next;
            }
            Assert.That(total,Is.EqualTo(cost));
        }
        static void Drain(RegionalNavScheduler scheduler,DemandNavService s,int limit=20000)
        {int ticks=0;while(Enumerable.Range(0,s.Inputs.Length).Any(i=>s.Inputs[i].Enabled&&s.Status(i)==DemandStatus.Pending)&&ticks++<limit){Assert.That(scheduler.Tick(),Is.InRange(1,3000));Assert.That(s.WorkUnits,Is.EqualTo(s.Accounted));}Assert.That(ticks,Is.LessThan(limit));}
        static void Compare(NavPortalWorld w,NavGraph full,int[] goals)
        {
            var inverse=Mapping(w,full);var scheduler=new RegionalNavScheduler(1);var s=new DemandNavService(w,scheduler,24);
            foreach(int goal in goals.Distinct()){
                for(int i=0;i<s.Inputs.Length;i++)s.Inputs[i]=new NavDemand{Enabled=true,Source=i*(w.NodeCount-1)/(s.Inputs.Length-1),Goal=goal,Since=scheduler.TickIndex};
                Assert.That(s.Status(0),Is.EqualTo(DemandStatus.Pending));Drain(scheduler,s);
                var expected=Oracle(full,inverse[goal]);for(int i=0;i<s.Inputs.Length;i++)CheckAnswer(s,i,full,inverse,expected,i<2);
            }
            var old=s.Answers[0];s.Invalidate();Assert.That(s.Status(0),Is.EqualTo(DemandStatus.Pending));Assert.That(s.TryRouteEdge(old,old.Source,out _),Is.False);
            Drain(scheduler,s);Assert.That(s.Answers[0].Revision,Is.EqualTo(s.Revision));Assert.That(s.WorkUnits,Is.EqualTo(s.Accounted));
        }
        [TestCaseSource(nameof(Corpus)),Category("DemandEquivalence")]
        public void ExactDemandsMatchD2AndMonolithic(string seed,int size)
        {
            var c=Circuit(seed);foreach(var body in NavClass.All){var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,body,size);
                var full=c.Map==null?new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,body):c.Map.Graphs[body.Index];int high=0;
                for(int n=1;n<w.NodeCount;n++)if(w.Position(n).Y>w.Position(high).Y)high=n;
                Compare(w,full,new[]{w.NodeCount/2,0,high,w.NodeCount-1});}
        }
        [TestCase(16),TestCase(32),TestCase(64),Category("DemandEquivalence")]
        public void DirectedLayersAndNarrowPassages(int size)
        {
            foreach(var body in NavClass.All){var terrain=new Heightfield(80,80,new float[81*81]);Func<double,double,double,bool> inside=(x,z,r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36;
                var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)),
                    new VerticalSolid(2,new Vec3(7.75,6,7.75),new Vec3(24.25,7,24.25)),
                    new VerticalSolid(3,new Vec3(-8,13,8),new Vec3(0,14,16)));
                var w=new NavPortalWorld(q,terrain,inside,body,size);var full=new NavGraph(q,terrain,inside,body);
                Compare(w,full,new[]{w.NodeAt(16,7,16),w.NodeAt(-4,14,12),w.NodeAt(0,0,0)});
                q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                    new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-40,0,-1),new Vec3(-1.5,20,1)),
                    new VerticalSolid(2,VerticalSurfaceKind.Foliage,new Vec3(1.5,0,-1),new Vec3(40,20,1)));
                w=new NavPortalWorld(q,terrain,inside,body,size);full=new NavGraph(q,terrain,inside,body);
                Assert.That(new NavPortalQuery(w).Reachable(w.NodeAt(0,0,-10),w.NodeAt(0,0,10)),Is.EqualTo(body.Index<2));
                Compare(w,full,new[]{w.NodeAt(0,0,-10),w.NodeAt(0,0,10)});
            }
        }
        [Test,Category("DemandEquivalence")]
        public void AcceptedRequestFinishesBeforeNewGoalAndSearchPausesWithoutDemand()
        {
            var c=Circuit("B");var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,NavClass.All[0],32);
            var scheduler=new RegionalNavScheduler(1);var s=new DemandNavService(w,scheduler,1);
            s.Inputs[0]=new NavDemand{Enabled=true,Source=w.NodeCount-1,Goal=0};
            int guard=0;while(s.Extractions==0&&guard++<2000)scheduler.Tick();
            Assert.That(s.Extractions,Is.GreaterThan(0));Assert.That(s.Answers[0].Serial,Is.Zero);
            s.Inputs[0]=new NavDemand{Enabled=true,Source=w.NodeCount/2,Goal=w.NodeCount-1,Since=scheduler.TickIndex};
            while(s.Answers[0].Serial==0&&guard++<4000)scheduler.Tick();
            Assert.That(s.Answers[0].Goal,Is.Zero);Assert.That(s.Answers[0].Source,Is.EqualTo(w.NodeCount-1));
            Assert.That(s.Status(0),Is.EqualTo(DemandStatus.Pending));Drain(scheduler,s);
            long pops=s.Extractions;for(int i=0;i<20;i++)scheduler.Tick();Assert.That(s.Extractions,Is.EqualTo(pops));
            long misses=s.CacheMisses;s.Inputs[0]=new NavDemand{Enabled=true,Source=w.NodeCount-1,Goal=0,Since=scheduler.TickIndex};Drain(scheduler,s);
            Assert.That(s.CacheMisses,Is.EqualTo(misses));Assert.That(s.CacheHits,Is.GreaterThan(0));
        }
        [Test,Category("DemandEquivalence")]
        public void FifoSurvivesMovingTargetsEvictionAndInvalidationDeterministically()
        {
            var c=Circuit("A");var w=new NavPortalWorld(c.Queries,c.Terrain,c.Collision.IsInside,NavClass.All[0],32);
            var traces=new List<string>();
            for(int repeat=0;repeat<2;repeat++){
                var scheduler=new RegionalNavScheduler(1);var s=new DemandNavService(w,scheduler,9,1);var seen=new bool[9];
                for(int i=0;i<9;i++)s.Inputs[i]=new NavDemand{Enabled=true,Source=i*(w.NodeCount-1)/8,Goal=(i%3)*(w.NodeCount-1)/2};
                var first=new DemandAnswer[9];
                for(int tick=0;tick<5000&&!seen.All(x=>x);tick++){
                    scheduler.Tick();Assert.That(s.WorkUnits,Is.EqualTo(s.Accounted));
                    for(int i=0;i<9;i++){if(s.Answers[i].Serial>0&&!seen[i]){seen[i]=true;first[i]=s.Answers[i];}
                        // Solo cambia la entrada; una solicitud admitida no se cancela.
                        if(tick>3)s.Inputs[i]=new NavDemand{Enabled=true,Source=i*(w.NodeCount-1)/8,Goal=tick%2==0?0:w.NodeCount-1,Since=tick};}
                }
                Assert.That(seen.All(x=>x),Is.True);Assert.That(s.Evictions,Is.GreaterThan(0));
                foreach(var a in first)Assert.That(a.Status,Is.Not.EqualTo(DemandStatus.Pending));
                traces.Add(string.Join(";",first.Select(a=>$"{a.Goal}:{a.Cost}:{a.Published}"))+":"+s.WorkUnits);
                s.Invalidate();for(int i=0;i<9;i++)Assert.That(s.Status(i),Is.EqualTo(DemandStatus.Pending));
                for(int i=0;i<9;i++)s.Inputs[i]=new NavDemand{Enabled=true,Source=i*(w.NodeCount-1)/8,Goal=0,Since=scheduler.TickIndex};Drain(scheduler,s);
            }
            Assert.That(traces[0],Is.EqualTo(traces[1]));
        }
    }
}
