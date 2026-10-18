using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class IncrementalNavFieldTests
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
        static void Check(IncrementalNavField f)
        {
            var expected=Oracle(f.World,f.Goal);
            for(int n=0;n<expected.Length;n++){Assert.That(f.TryDistance(n,out int d),Is.True);Assert.That(d,Is.EqualTo(expected[n]),$"nodo {n}, meta {f.Goal}");}
        }
        static void Finish(RegionalNavScheduler s,IncrementalNavField f)
        {
            for(int i=0;f.Computing&&i<100000;i++)Assert.That(s.Tick(),Is.InRange(1,3000));
            Assert.That(f.Computing,Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void GoalChangesRepairExactCostsIncludingDirectedAndDisconnectedSurfaces(int kind)
        {
            var terrain=new Heightfield(48,48,new float[49*49]);
            var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(0,0,0),new Vec3(12,6,12)),
                new VerticalSolid(2,new Vec3(-.25,6,-.25),new Vec3(12.25,7,12.25)),
                new VerticalSolid(3,new Vec3(-16,12,-16),new Vec3(-8,13,-8)));
            var w=new NavPortalWorld(q,terrain,(x,z,r)=>Math.Abs(x)+r<21&&Math.Abs(z)+r<21,NavClass.All[kind],16);
            var s=new RegionalNavScheduler(1);var f=new IncrementalNavField(w,s);
            var goals=new List<int>{w.NodeAt(-4,0,-4),w.NodeAt(-3.5,0,-4),w.NodeAt(6,7,6),w.NodeAt(-12,13,-12),w.NodeAt(-4,0,-4)};
            foreach(int goal in goals){
                Assert.That(goal,Is.GreaterThanOrEqualTo(0));int version=f.Version,oldGoal=f.Goal;
                Assert.That(f.Request(goal),Is.True);s.Tick();
                if(f.Version==version&&version>0){Assert.That(f.Goal,Is.EqualTo(oldGoal));Check(f);}
                Finish(s,f);Assert.That(f.Goal,Is.EqualTo(goal));Check(f);
                long work=f.WorkUnits;Assert.That(f.Request(goal),Is.True);Assert.That(s.Tick(),Is.Zero);Assert.That(f.WorkUnits,Is.EqualTo(work));
            }
        }
        static IEnumerable<TestCaseData> Corpus()
        {foreach(string seed in new[]{"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})yield return new TestCaseData(seed);}
        [TestCaseSource(nameof(Corpus))]
        public void FullCorpusAndSmallGoalShiftMatchIndependentOracle(string seed)
        {
            var circuit=seed=="A"?new VerticalCircuit(VerticalLayout.Escalada):seed=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(seed));
            foreach(var body in NavClass.All){
                var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,body,16);
                var s=new RegionalNavScheduler(1);var f=new IncrementalNavField(w,s);int goal=w.NodeCount/2;
                f.Request(goal);Finish(s,f);Check(f);
                int next=w.EdgeStart[goal]<w.EdgeStart[goal+1]?w.EdgeTo[w.EdgeStart[goal]]:(goal+1)%w.NodeCount;
                f.Request(next);Finish(s,f);Check(f);
            }
        }
        static double Percentile(List<double> values,double p)
        {if(values.Count==0)return -1;values.Sort();return values[(int)Math.Ceiling(values.Count*p)-1];}
        [TestCase(16)] [TestCase(32)] [TestCase(64)]
        public void MeasureExactIncrementalFieldsFor180Seconds(int size)
        {
            var circuit=new VerticalCircuit(new ProceduralMap("HORDA750"));var s=new RegionalNavScheduler(3);
            var fields=new IncrementalNavField[3];var goals=new int[3,2];var requestedAt=new long[3];
            var latency=new List<double>[3];var ages=new List<double>[3];var versions=new int[3];
            for(int c=0;c<3;c++){
                var w=new NavPortalWorld(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[c],size);
                fields[c]=new IncrementalNavField(w,s);latency[c]=new List<double>();ages[c]=new List<double>();
                int a=w.NodeAt(0,0,0);if(a<0)a=w.NodeCount/2;int b=-1;
                for(int e=w.EdgeStart[a];e<w.EdgeStart[a+1];e++){
                    if((NavEdgeKind)w.Graph(a).EdgeKind[w.EdgeLocal[e]]!=NavEdgeKind.Walk)continue;
                    int candidate=w.EdgeTo[e];
                    for(int back=w.EdgeStart[candidate];back<w.EdgeStart[candidate+1];back++)if(w.EdgeTo[back]==a)b=candidate;
                    if(b>=0)break;
                }
                Assert.That(b,Is.GreaterThanOrEqualTo(0));goals[c,0]=a;goals[c,1]=b;
            }
            foreach(string mode in new[]{"cold-fixed","warm-moving"}){
                // La pasada móvil parte de un campo fijo completo; este calentamiento se
                // mide por separado arriba, nunca se oculta como primera publicación rápida.
                if(mode=="warm-moving")for(int c=0;c<3;c++)Finish(s,fields[c]);
                var cpu=new List<double>(10800);var activeCpu=new List<double>(10800);long workBefore=0,scheduled=0,allocated=0;
                long probeBefore=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[4096];
                bool allocationValid=GC.GetAllocatedBytesForCurrentThread()-probeBefore>=4096;GC.KeepAlive(probe);
                for(int c=0;c<3;c++){latency[c].Clear();ages[c].Clear();versions[c]=fields[c].Version;workBefore+=fields[c].WorkUnits;requestedAt[c]=s.TickIndex;}
                for(int tick=0;tick<10800;tick++){
                    int which=mode=="cold-fixed"?0:(tick/12+1)%2;
                    for(int c=0;c<3;c++){
                        var f=fields[c];int wanted=goals[c,which];
                        if(!f.Computing&&(f.Version==0||f.Goal!=wanted)){requestedAt[c]=s.TickIndex;f.Request(wanted);}
                        else if(f.Computing)f.Request(wanted); // Solicitud repetida o contrapresión; sin cancelar.
                    }
                    long allocationBefore=GC.GetAllocatedBytesForCurrentThread();
                    long timer=Stopwatch.GetTimestamp();int used=s.Tick();double elapsed=(Stopwatch.GetTimestamp()-timer)*1000.0/Stopwatch.Frequency;
                    allocated+=GC.GetAllocatedBytesForCurrentThread()-allocationBefore;
                    cpu.Add(elapsed);if(used>0)activeCpu.Add(elapsed);scheduled+=used;
                    Assert.That(used,Is.InRange(0,3000));
                    for(int c=0;c<3;c++){var f=fields[c];
                        if(f.Version!=versions[c]){latency[c].Add((f.PublishedTick-requestedAt[c])/60.0);versions[c]=f.Version;}
                        // En móvil se mide desde la primera reparación publicada. La espera
                        // inicial figura en latencia; no arrastrar los 180 s de meta fija.
                        if(mode=="warm-moving"&&latency[c].Count>0)ages[c].Add((s.TickIndex-f.PublishedRequestedTick)/60.0);
                    }
                }
                long total=-workBefore;
                for(int c=0;c<3;c++){var f=fields[c];total+=f.WorkUnits;
                    TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "D3-incremental size={0} mode={1} class={2} publications={3} latencyP50={4:F3} latencyP95={5:F3} latencyP99={6:F3} latencyMax={7:F3} ageP95={8:F3} ageMax={9:F3} workLifetime={10} changedLifetime={11} copiedLifetime={12} payload={13} pending={14} ageP50={15:F3} ageP99={16:F3}",
                        size,mode,c,latency[c].Count,Percentile(latency[c],.5),Percentile(latency[c],.95),Percentile(latency[c],.99),Percentile(latency[c],1),Percentile(ages[c],.95),Percentile(ages[c],1),f.WorkUnits,f.Changes,f.Copies,f.PayloadBytes,f.Computing?1:0,Percentile(ages[c],.5),Percentile(ages[c],.99)));
                }
                Assert.That(total,Is.EqualTo(scheduled));
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "D3-incremental-total size={0} mode={1} work={2} tickP50Ms={3:F4} tickP95Ms={4:F4} tickP99Ms={5:F4} tickMaxMs={6:F4} activeTicks={7} activeP95Ms={8:F4} allocatedTicks={9}",size,mode,total,Percentile(cpu,.5),Percentile(cpu,.95),Percentile(cpu,.99),Percentile(cpu,1),activeCpu.Count,Percentile(activeCpu,.95),allocationValid?allocated:-1));
            }
            foreach(var f in fields){Finish(s,f);Check(f);}
        }
    }
}
