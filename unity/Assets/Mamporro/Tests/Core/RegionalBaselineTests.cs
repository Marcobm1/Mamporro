using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // D0: observación externa del runtime aprobado. Ningún cambio de navegación ni de combate.
    // Los controles sin ataques/esbirros existen exclusivamente en esta fixture QA.
    public sealed class RegionalBaselineTests
    {
        const double Dt=1.0/60;
        static readonly CultureInfo CI=CultureInfo.InvariantCulture;
        static readonly Dictionary<string,VerticalCircuit> maps=new Dictionary<string,VerticalCircuit>();
        static VerticalCircuit Map(string seed)
        {
            if(!maps.TryGetValue(seed,out var c))maps[seed]=c=new VerticalCircuit(new ProceduralMap(seed));
            return c;
        }
        static string F(double x)=>x.ToString("F6",CI);
        static double Distance(double x,double y,double z)=>Math.Sqrt(x*x+y*y+z*z);
        internal static int Region(VerticalCircuit c,double x,double z)
        {
            int m=c.Map.Layout.MacroAt(x,z);return m<0?-1:c.Map.Layout.RegionOf[m];
        }
        static int Tile(double x,double z,int size)=>((int)Math.Floor((z+64)/size))*128+(int)Math.Floor((x+64)/size);
        internal static Vec3 Start(VerticalCircuit c)
        {
            var g=c.Map.Graphs[2];var to=ProceduralMap.Reach(g,c.Map.SpawnNode(2));var back=ProceduralMap.Reach(g,c.Map.SpawnNode(2),true);
            int best=-1;double dist=double.MaxValue;
            for(int n=0;n<g.NodeCount;n++){
                if(!to[n]||!back[n]||!g.NodeTerrain[n]||Math.Abs(g.NodeY[n])>.01)continue;
                double d=Math.Pow(g.NodeX(n)+14,2)+Math.Pow(g.NodeZ(n),2);
                if(d<dist){best=n;dist=d;}
            }
            Assert.That(best,Is.GreaterThanOrEqualTo(0));return new Vec3(g.NodeX(best),g.NodeY[best],g.NodeZ(best));
        }
        internal static void Horde(VerticalCircuit c,WorldRun run,int count)
        {
            int placed=0,elites=count/100;
            for(int sample=0;placed<count&&sample<40000;sample++){
                double angle=sample*2.399963229728653,rad=4+(sample%60)*.75,x=Math.Cos(angle)*rad,z=Math.Sin(angle)*rad;
                int type=placed<elites?4:(placed%10<5?0:placed%10<7?1:placed%10<9?2:3);
                var g=c.Map.Graphs[NavClass.Of(Catalog.Enemies[type])];double y=c.Terrain.HeightAt(x,z);int n=g.NodeAt(x,y+.01,z,.01);
                if(n<0||!g.NodeTerrain[n]||Math.Abs(g.SurfaceY(n,x,z)-y)>.01)continue;
                run.Combat.Spawn(type,x,z);placed++;
            }
            Assert.That(placed,Is.EqualTo(count));
        }
        internal static long ArrayBytes(object instance)
        {
            long bytes=0;
            foreach(var f in instance.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)){
                if(!(f.GetValue(instance) is Array a)||!a.GetType().GetElementType().IsPrimitive)continue;
                bytes+=Buffer.ByteLength(a);
            }
            return bytes; // Payload solamente: no incluye cabeceras, listas retenidas ni consultas.
        }
        static double Percentile(List<double> values,double p)
        {
            if(values.Count==0)return -1;values.Sort();return values[(int)Math.Min(values.Count-1,Math.Ceiling(values.Count*p)-1)];
        }
        static string Stats(List<double> v)=>$"p50={F(Percentile(v,.5))},p95={F(Percentile(v,.95))},p99={F(Percentile(v,.99))},max={F(Percentile(v,1))}";

        [TestCase("P0QA", "boss", false)]
        [TestCase("COLINAS7", "boss", false)]
        [TestCase("HORDA750", "body", false)] [TestCase("HORDA750", "body", true)]
        [TestCase("HORDA750", "boss", false)] [TestCase("HORDA750", "boss", true)]
        [TestCase("HORDA750", "no-minions", false)] [TestCase("HORDA750", "no-minions", true)]
        [TestCase("HORDA750", "horde300", false)] [TestCase("HORDA750", "horde300", true)]
        [TestCase("HORDA750", "horde500", false)] [TestCase("HORDA750", "horde500", true)]
        [TestCase("HORDA750", "horde750", false)] [TestCase("HORDA750", "horde750", true)]
        public void TraceBossAndNavigation(string seed,string mode,bool moving)
        {
            var c=Map(seed);Assert.That(c.Map.BossPlateau(out var at),Is.True);var from=Start(c);
            var run=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            var e=run.Combat.Enemies;var nav=new EnemyNavigation(c,e.Capacity,c.Map.Graphs);
            e.Navigation=nav;run.Combat.Vertical=nav;run.Combat.Invincible=true;run.Combat.WeaponsOff=true;
            run.Body.PlaceAt(at.X,at.Y,at.Z);run.SyncPlayer();
            int load=mode.StartsWith("horde",StringComparison.Ordinal)?int.Parse(mode.Substring(5),CI):1;
            if(load>1)Horde(c,run,load-1);
            if(mode=="body")run.Combat.Spawn(5,from.X,from.Z);else run.Combat.SpawnBoss(from.X,from.Z);
            uint id=e.Id[e.Count-1];int targetRegion=Region(c,at.X,at.Z);
            var csv=new StringBuilder("tick,id,x,y,z,distance,node,edge,parent,region,tile16,tile32,tile64,goalL,publishedGoalL,versionL,requestAgeL,navState,combatState,phase,attack,count,queueWaits,geometryBlocked,px,py,pz\n");
            var edges=new HashSet<int>();var repeated=new Dictionary<int,int>();
            var navTimes=new double[6];var phases=new Dictionary<string,double>();
            var tickMs=new List<double>(7200);var navMs=new List<double>(7200);
            var latency=new[]{new List<double>(),new List<double>(),new List<double>()};
            int[] request={-2,-2,-2},versions=new int[3],superseded=new int[3],completed=new int[3];
            double[] requestedAt=new double[3],maxPending=new double[3];bool[] pending=new bool[3];
            double distance=0,stopped=0,attackStopped=0,geometryStopped=0,queueSeconds=0,firstHeight=-1,firstRegion=-1,firstContact=-1;
            int starts=0,lastEdge=-1,peakCount=e.Count,removed=0,incidents=0;long allocated=0;
            long arrays=ArrayBytes(nav);foreach(var g in nav.Graphs)arrays+=ArrayBytes(g);foreach(var f in nav.Fields)arrays+=ArrayBytes(f);
            double managedBefore=GC.GetTotalMemory(false);
            // Mantiene el ensayo histórico para el jugador quieto. El móvil es un objetivo cinemático
            // continuo (3 m/s), sin teleports, sobre el mismo suelo; no valida locomoción del jugador.
            for(int tick=0;tick<7200;tick++){
                int i=e.IndexOf(id);Assert.That(i,Is.GreaterThanOrEqualTo(0));double t=tick*Dt;
                if(moving){
                    double x=at.X+2*Math.Sin(t*1.5),z=at.Z+2*(Math.Cos(t*1.5)-1);
                    double y=c.Collision.GroundHeight(x,z,at.Y+.6);run.Body.PlaceAt(x,y,z);run.SyncPlayer();
                }
                for(int k=0;k<3;k++){
                    int goal=nav.GoalFor(k,run.Combat.Player);
                    if(goal!=request[k]){if(pending[k])superseded[k]++;request[k]=goal;requestedAt[k]=t;pending[k]=true;}
                }
                double ox=e.X[i],oy=e.Y[i],oz=e.Z[i];byte state=nav.State(i);int combat=e.State[i];
                string phase=run.Combat.Boss?.Phase??"none";int queue=nav.QueueWaits;long before=nav.Ticks;
                long alloc=GC.GetAllocatedBytesForCurrentThread(),cpu=Stopwatch.GetTimestamp();
                run.Step(default,Dt);
                long elapsed=Stopwatch.GetTimestamp()-cpu;allocated+=GC.GetAllocatedBytesForCurrentThread()-alloc;
                // Elimina solo esbirros del control diagnóstico. No cambia el controlador ni su RNG.
                if(mode=="no-minions")for(int j=e.Count-1;j>=0;j--)if(e.Id[j]!=id){e.Remove(j);removed++;}
                i=e.IndexOf(id);double moved=Distance(e.X[i]-ox,e.Y[i]-oy,e.Z[i]-oz);distance+=moved;
                navTimes[state]+=Dt;phases[phase]=phases.TryGetValue(phase,out double old)?old+Dt:Dt;
                if(moved<1e-5){stopped+=Dt;if(combat==1||combat==3)attackStopped+=Dt;if(nav.GeometryBlockedSeconds(i)>0)geometryStopped+=Dt;}
                if(nav.QueueWaits>queue)queueSeconds+=Dt; // Global; no atribuir al jefe si hay esbirros/horda.
                peakCount=Math.Max(peakCount,e.Count);
                int edge=nav.Edge(i);if(edge!=lastEdge&&edge>=0&&nav.Graphs[2].EdgeKind[edge]==(byte)NavEdgeKind.Climb){starts++;edges.Add(edge);repeated[edge]=repeated.TryGetValue(edge,out int n)?n+1:1;}
                lastEdge=edge;
                if(firstHeight<0&&e.Y[i]>at.Y-.2&&nav.Stable(i))firstHeight=t+Dt;
                if(firstRegion<0&&Region(c,e.X[i],e.Z[i])==targetRegion&&Math.Abs(e.Y[i]-at.Y)<.2&&nav.Stable(i))firstRegion=t+Dt;
                if(firstContact<0&&nav.Stable(i)&&Rules.Hypot(e.X[i]-run.Combat.Player.X,e.Z[i]-run.Combat.Player.Z)<=e.Radius(i)+CombatPlayer.Radius+.1&&Math.Abs(e.Y[i]-run.Combat.Player.Y)<1.6+e.Def(i).height*.5)firstContact=t+Dt;
                if(!c.Queries.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),e.Radius(i),e.Def(i).height,NavGraph.Step)||e.Y[i]<c.Terrain.HeightAt(e.X[i],e.Z[i])-1e-3)incidents++;
                for(int k=0;k<3;k++){
                    var f=nav.Fields[k];if(pending[k])maxPending[k]=Math.Max(maxPending[k],t+Dt-requestedAt[k]);
                    if(f.Version!=versions[k]&&f.Goal==request[k]&&pending[k]){latency[k].Add(t+Dt-requestedAt[k]);completed[k]++;pending[k]=false;}
                    versions[k]=f.Version;
                }
                if(t>=2){tickMs.Add(elapsed*1000.0/Stopwatch.Frequency);navMs.Add((nav.Ticks-before)*1000.0/Stopwatch.Frequency);}
                if(tick%6==0||edge>=0&&edge!=nav.Fields[2].Parent(Math.Max(0,nav.Node(i)))){
                    int node=nav.Node(i),parent=node<0?-1:nav.Fields[2].Parent(node);
                    csv.Append(tick).Append(',').Append(id).Append(',').Append(F(e.X[i])).Append(',').Append(F(e.Y[i])).Append(',').Append(F(e.Z[i])).Append(',').Append(F(distance)).Append(',')
                        .Append(node).Append(',').Append(edge).Append(',').Append(parent).Append(',').Append(Region(c,e.X[i],e.Z[i])).Append(',').Append(Tile(e.X[i],e.Z[i],16)).Append(',').Append(Tile(e.X[i],e.Z[i],32)).Append(',').Append(Tile(e.X[i],e.Z[i],64)).Append(',')
                        .Append(nav.Goal(2)).Append(',').Append(nav.Fields[2].Goal).Append(',').Append(nav.Fields[2].Version).Append(',').Append(F(t+Dt-requestedAt[2])).Append(',').Append(nav.State(i)).Append(',').Append(e.State[i]).Append(',')
                        .Append(run.Combat.Boss?.Phase??"none").Append(',').Append(run.Combat.Boss?.Attack??"none").Append(',').Append(e.Count).Append(',').Append(nav.QueueWaits).Append(',').Append(F(nav.GeometryBlockedSeconds(i))).Append(',')
                        .Append(F(run.Combat.Player.X)).Append(',').Append(F(run.Combat.Player.Y)).Append(',').Append(F(run.Combat.Player.Z)).Append('\n');
                }
                // Quieto: mismo final de la prueba heredada; móvil se observa durante 120 s completos.
                if(!moving&&firstHeight>=0)break;
            }
            var report=new StringBuilder();report.AppendLine($"{seed} {mode} moving={moving} id={id} budget={nav.Budget} targetRegion={targetRegion}");
            report.AppendLine($"arrivalHeight={F(firstHeight)} arrivalRegion={F(firstRegion)} contact={F(firstContact)} distance={F(distance)} climbStartsBoss={starts} distinctClimbsBoss={edges.Count} climbsAll={nav.ClimbStarts} peakCount={peakCount} removedMinions={removed} incidents={incidents}");
            report.AppendLine($"stopped={F(stopped)} attackStopped={F(attackStopped)} geometryStopped={F(geometryStopped)} anyQueueSeconds={F(queueSeconds)}");
            for(int s=0;s<6;s++)report.AppendLine($"navState{s}Seconds={F(navTimes[s])}");
            foreach(var p in phases)report.AppendLine($"phase-{p.Key}Seconds={F(p.Value)}");
            foreach(var p in repeated)report.AppendLine($"climbEdge-{p.Key}Starts={p.Value}");
            report.AppendLine("cpuTickMs "+Stats(tickMs));report.AppendLine("navigationMs "+Stats(navMs));
            report.AppendLine($"arrayPayloadBytes={arrays} managedBefore={F(managedBefore)} managedAfter={GC.GetTotalMemory(false)} stepAllocatedBytes={allocated}");
            for(int k=0;k<3;k++)report.AppendLine($"class{k} latestRequestCompleted={completed[k]} superseded={superseded[k]} pending={pending[k]} maxPending={F(maxPending[k])} completionSeconds {Stats(latency[k])}");
            // No sobrescribir las catorce trazas heredadas al repetir la suite.
            string folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../TestResults/P0/D0/LegacyRuns",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(folder);
            string name=seed+"-"+mode+(moving?"-moving":"-fixed");File.WriteAllText(Path.Combine(folder,name+".csv"),csv.ToString());File.WriteAllText(Path.Combine(folder,name+".txt"),report.ToString());
            TestContext.Out.WriteLine(report.ToString());
            Assert.That(nav.Budget,Is.EqualTo(3000));Assert.That(incidents,Is.Zero,"geometría aprobada, sin penetración del jefe");
        }
    }
}
