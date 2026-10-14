using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // D0: observador exclusivamente QA. Delega cada llamada una vez y conserva sus argumentos.
    // No modifica velocidades, decisiones, RNG, combate ni campos de producción.
    public sealed class RegionalDiagnosisTests
    {
        const double Dt=1.0/60;
        static readonly CultureInfo CI=CultureInfo.InvariantCulture;
        static string F(double v)=>v.ToString("F6",CI);
        static double Length(double x,double y,double z)=>Math.Sqrt(x*x+y*y+z*z);
        [StructLayout(LayoutKind.Sequential)]
        struct MemoryCounters
        {
            public uint Size,PageFaults;
            public UIntPtr PeakWorkingSet,WorkingSet,PeakPaged,Paged,PeakNonPaged,NonPaged,Pagefile,PeakPagefile,Private;
        }
        [DllImport("psapi.dll",SetLastError=true)]
        static extern bool GetProcessMemoryInfo(IntPtr process,out MemoryCounters counters,uint size);
        static void Memory(out long resident,out long privateBytes)
        {
            // Process.WorkingSet64 devuelve cero en este Mono; obtener el contador real de Windows.
            bool ok=GetProcessMemoryInfo(new IntPtr(-1),out var counters,(uint)Marshal.SizeOf<MemoryCounters>());
            Assert.That(ok,Is.True,$"GetProcessMemoryInfo error={Marshal.GetLastWin32Error()}");
            resident=(long)counters.WorkingSet.ToUInt64();privateBytes=(long)counters.Private.ToUInt64();
            Assert.That(resident,Is.GreaterThan(0));
        }
        static string Stats(List<double> v)
        {
            if(v.Count==0)return "n=0";
            v.Sort();return $"n={v.Count} p50={F(v[(int)Math.Ceiling(v.Count*.5)-1])} p95={F(v[(int)Math.Ceiling(v.Count*.95)-1])} p99={F(v[(int)Math.Ceiling(v.Count*.99)-1])} max={F(v[v.Count-1])}";
        }
        sealed class FieldWatch
        {
            static readonly FieldInfo Building=typeof(NavField).GetField("building",BindingFlags.Instance|BindingFlags.NonPublic);
            public int Desired=-2,Requests,Publications,UnpublishedChanges;
            public double RequestAt,BuildAt,BuildRequestAt,PublishedSourceAt=-1;
            public readonly List<double> Latency=new List<double>(),Calculation=new List<double>(),Age=new List<double>();
            public void Request(int desired,double t)
            {
                if(desired==Desired)return;
                if(Desired!=-2)UnpublishedChanges++;
                Desired=desired;RequestAt=t;Requests++;
            }
            public void Observe(NavField f,bool wasComputing,int version,double t,int k,StringBuilder events)
            {
                if(!wasComputing&&(f.Computing||f.Version!=version)){
                    BuildAt=t;BuildRequestAt=RequestAt;
                    events.AppendLine($"{F(t)},start,{k},{Building.GetValue(f)},{f.Version},{F(BuildRequestAt)}");
                }
                if(f.Version!=version){
                    Publications++;PublishedSourceAt=BuildAt;
                    Latency.Add(t+Dt-BuildRequestAt);Calculation.Add(t+Dt-BuildAt);
                    if(f.Goal==Desired)UnpublishedChanges=0;
                    events.AppendLine($"{F(t+Dt)},publish,{k},{f.Goal},{f.Version},{F(BuildRequestAt)}");
                }
                // Antigüedad de la muestra representada durante movimiento continuo; se conserva
                // aunque vuelva al mismo nodo. En meta fija solo se informa latencia de publicación.
                if(PublishedSourceAt>=0)Age.Add(t+Dt-PublishedSourceAt);
            }
        }
        sealed class Observer : IEnemyNavigation
        {
            public readonly EnemyNavigation Nav;
            public readonly FieldWatch[] Fields={new FieldWatch(),new FieldWatch(),new FieldWatch()};
            public readonly StringBuilder Events=new StringBuilder("time,event,class,goal,version,requestAt\n");
            public readonly List<double> Cpu=new List<double>();
            public uint Id;public double Time,MoveDistance,RequestedDistance,DirX,DirZ;
            public bool Geometry,Queue,Hold,Dash,Still;public int State,Node,Parent;public byte Motion;
            public long CpuTicks,Allocated;public int Samples;
            Enemies enemies;
            public Observer(EnemyNavigation nav){Nav=nav;}
            public void Begin(Enemies e,CombatPlayer p,double dt)
            {
                enemies=e;MoveDistance=0;Geometry=Queue=Hold=false;CpuTicks=0;
                for(int k=0;k<3;k++)Fields[k].Request(Nav.GoalFor(k,p),Time);
                bool a=Nav.Fields[0].Computing,b=Nav.Fields[1].Computing,c=Nav.Fields[2].Computing;
                int va=Nav.Fields[0].Version,vb=Nav.Fields[1].Version,vc=Nav.Fields[2].Version;
                long alloc=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();
                Nav.Begin(e,p,dt);CpuTicks+=Stopwatch.GetTimestamp()-start;Allocated+=GC.GetAllocatedBytesForCurrentThread()-alloc;
                Fields[0].Observe(Nav.Fields[0],a,va,Time,0,Events);
                Fields[1].Observe(Nav.Fields[1],b,vb,Time,1,Events);
                Fields[2].Observe(Nav.Fields[2],c,vc,Time,2,Events);
            }
            public void End(){Nav.End();if(Time>=2)Cpu.Add(CpuTicks*1000.0/Stopwatch.Frequency);}
            public void Direction(int i,CombatPlayer p,out double x,out double z,out bool hold)
            {
                long start=Stopwatch.GetTimestamp();Nav.Direction(i,p,out x,out z,out hold);CpuTicks+=Stopwatch.GetTimestamp()-start;
                if(enemies.Id[i]==Id){DirX=x;DirZ=z;Hold=hold;}
            }
            public bool Move(int i,double vx,double vz,double kx,double kz,double speed,bool dash,bool still,double dt)
            {
                bool tracked=enemies.Id[i]==Id;double x=enemies.X[i],y=enemies.Y[i],z=enemies.Z[i];int queue=Nav.QueueWaits;
                if(tracked){State=enemies.State[i];Motion=Nav.State(i);Node=Nav.Node(i);Parent=Node<0?-1:Nav.Fields[2].Parent(Node);Dash=dash;Still=still;RequestedDistance=Length((vx+kx)*dt,0,(vz+kz)*dt);}
                long alloc=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();
                bool result=Nav.Move(i,vx,vz,kx,kz,speed,dash,still,dt);
                CpuTicks+=Stopwatch.GetTimestamp()-start;Allocated+=GC.GetAllocatedBytesForCurrentThread()-alloc;
                if(tracked){Samples++;MoveDistance=Length(enemies.X[i]-x,enemies.Y[i]-y,enemies.Z[i]-z);Queue=Nav.QueueWaits>queue;Geometry=result&&!Queue;}
                return result;
            }
            public bool Stable(int i)=>Nav.Stable(i);
            public bool LineOfSight(int i,CombatPlayer p)=>Nav.LineOfSight(i,p);
            public bool StraightWalk(int i,double x,double z,double d)=>Nav.StraightWalk(i,x,z,d);
            public bool TryPlace(int i,double x,double z)=>Nav.TryPlace(i,x,z);
            public bool SeparationOverlap(int i,int j)=>Nav.SeparationOverlap(i,j);
            public void Spawned(Enemies e,int i){enemies=e;Nav.Spawned(e,i);}
            public void Moved(int from,int to)=>Nav.Moved(from,to);
            public void Cleared()=>Nav.Cleared();
        }

        [TestCase("body",false)] [TestCase("body",true)]
        [TestCase("boss",false)] [TestCase("boss",true)]
        [TestCase("no-minions",false)] [TestCase("no-minions",true)]
        [TestCase("body-id750",false)] [TestCase("body-id750",true)]
        [TestCase("boss-id750",false)] [TestCase("boss-id750",true)]
        [TestCase("horde750",false)] [TestCase("horde750",true)]
        public void DiagnoseArrivalAndFieldAge(string mode,bool moving)
        {
            var c=new VerticalCircuit(new ProceduralMap("HORDA750"));
            string root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../TestResults/P0/D0/Diagnosis",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"),mode+(moving?"-moving":"-fixed")));
            Directory.CreateDirectory(root);string signature=null;
            for(int repeat=0;repeat<3;repeat++){
                string actual=Run(c,mode,moving,root,repeat,true);
                if(signature==null)signature=actual;else Assert.That(actual,Is.EqualTo(signature),"Repetición lógica determinista, excluye CPU/memoria");
            }
            // Control de transparencia del decorador con las mismas entradas y duración.
            Assert.That(Run(c,mode,moving,root,3,false),Is.EqualTo(signature),"El observador no altera el resultado lógico");
        }
        static string Run(VerticalCircuit c,string mode,bool moving,string folder,int repeat,bool observed)
        {
            Assert.That(c.Map.BossPlateau(out var at),Is.True);var from=RegionalBaselineTests.Start(c);
            var run=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            var e=run.Combat.Enemies;var nav=new EnemyNavigation(c,e.Capacity,c.Map.Graphs);var observer=new Observer(nav);
            e.Navigation=observed?(IEnemyNavigation)observer:nav;run.Combat.Vertical=nav;
            run.Combat.Invincible=true;run.Combat.WeaponsOff=true;
            run.Body.PlaceAt(at.X,at.Y,at.Z);run.SyncPlayer();
            if(mode=="horde750"||mode.EndsWith("-id750",StringComparison.Ordinal)){
                RegionalBaselineTests.Horde(c,run,749);
                // Control con el mismo ID, velocidad y paridad que la horda; Remove no reinicia IDs.
                if(mode.EndsWith("-id750",StringComparison.Ordinal)){for(int j=e.Count-1;j>=0;j--)e.Remove(j);e.Rebuild();}
            }
            if(mode.StartsWith("body",StringComparison.Ordinal))run.Combat.Spawn(5,from.X,from.Z);else run.Combat.SpawnBoss(from.X,from.Z);
            uint id=e.Id[e.Count-1];observer.Id=id;
            var csv=new StringBuilder("tick,id,x,y,z,px,py,pz,horizontal,vertical,surfaceError,stable,region,node,parent,edge,navState,combatState,phase,attack,moved,totalDistance,geometry,queue,hold,detourTime,stuckTime,dirX,dirZ,goal,version,fieldDistance,ageL,count,nodeCell,positionCell,vx,vz\n");
            var topology=new StringBuilder("node,cell,x,y,z,edge,to,toCell,toX,toY,toZ,kind,cost\n");var dumped=new HashSet<int>();
            var visits=new Dictionary<int,int>();var phaseSeconds=new Dictionary<string,double>();
            double[] categories=new double[7],motion=new double[6];
            double firstHeight=-1,firstRegion=-1,arrival=-1,distance=0,away=0,backtrack=0,lastDx=0,lastDz=0,turn=0;
            double geometry=0,queue=0,noProgress=0,maxBlocked=0,consecutiveBlocked=0;
            double stopAtHeightDistance=-1,heightHorizontal=-1,heightVertical=-1;
            int steps=0,incidents=0,revisits=0,lastNode=-1,lastVersion=-1,lastDist=-1,peak=e.Count,climbs=0,lastEdge=-1,detourBlocked=0;
            long payload=RegionalBaselineTests.ArrayBytes(nav);foreach(var g in nav.Graphs)payload+=RegionalBaselineTests.ArrayBytes(g);foreach(var f in nav.Fields)payload+=RegionalBaselineTests.ArrayBytes(f);
            var cpu=new List<double>();var productionCpu=new List<double>();long managedPeak=GC.GetTotalMemory(false),workingPeak=0,privatePeak=0;
            for(int tick=0;tick<10800;tick++){
                double t=tick*Dt;observer.Time=t;int i=e.IndexOf(id);Assert.That(i,Is.GreaterThanOrEqualTo(0));
                // Misma trayectoria cinemática del control heredado. Con objetivo fijo se conserva
                // WorldRun completo: cualquier empuje del jugador se registra, no se anula.
                if(moving){double x=at.X+2*Math.Sin(t*1.5),z=at.Z+2*(Math.Cos(t*1.5)-1);run.Body.PlaceAt(x,c.Collision.GroundHeight(x,z,at.Y+.6),z);run.SyncPlayer();}
                double ox=e.X[i],oy=e.Y[i],oz=e.Z[i],px=run.Combat.Player.X,pz=run.Combat.Player.Z;
                long beforeNav=nav.Ticks,start=Stopwatch.GetTimestamp();run.Step(default,Dt);long elapsed=Stopwatch.GetTimestamp()-start;
                if(t>=2)cpu.Add(elapsed*1000.0/Stopwatch.Frequency);
                if(t>=2)productionCpu.Add((nav.Ticks-beforeNav)*1000.0/Stopwatch.Frequency);
                if(mode=="no-minions")for(int j=e.Count-1;j>=0;j--)if(e.Id[j]!=id)e.Remove(j);
                i=e.IndexOf(id);double dx=e.X[i]-ox,dy=e.Y[i]-oy,dz=e.Z[i]-oz,moved=Length(dx,dy,dz);distance+=moved;steps++;peak=Math.Max(peak,e.Count);
                double horizontal=Rules.Hypot(e.X[i]-run.Combat.Player.X,e.Z[i]-run.Combat.Player.Z),vertical=Math.Abs(e.Y[i]-run.Combat.Player.Y);
                int node=nav.Node(i),edge=nav.Edge(i),region=RegionalBaselineTests.Region(c,e.X[i],e.Z[i]);
                double surface=node<0?double.PositiveInfinity:Math.Abs(e.Y[i]-nav.Graphs[2].SurfaceY(node,e.X[i],e.Z[i]));
                bool stable=nav.Stable(i)&&surface<=.01;
                if(firstHeight<0&&e.Y[i]>at.Y-.2&&stable){firstHeight=t+Dt;stopAtHeightDistance=distance;heightHorizontal=horizontal;heightVertical=vertical;}
                if(firstRegion<0&&region==RegionalBaselineTests.Region(c,at.X,at.Z)&&Math.Abs(e.Y[i]-at.Y)<=.2&&stable)firstRegion=t+Dt;
                bool reached=stable&&horizontal<=e.Radius(i)+CombatPlayer.Radius&&vertical<=.2
                    &&!nav.SegmentBlocked(e.X[i],e.Y[i]+1,e.Z[i],run.Combat.Player.X,run.Combat.Player.Y+1,run.Combat.Player.Z,.05);
                if(arrival<0&&reached)arrival=t+Dt;
                if(dx*(px-ox)+dz*(pz-oz)<-1e-7)away+=moved;
                double len=Rules.Hypot(dx,dz),prev=Rules.Hypot(lastDx,lastDz);
                if(len>1e-5&&prev>1e-5){double dot=Math.Max(-1,Math.Min(1,(dx*lastDx+dz*lastDz)/(len*prev)));turn+=Math.Acos(dot);if(dot<0)backtrack+=moved;}
                if(len>1e-5){lastDx=dx;lastDz=dz;}
                if(node!=lastNode&&node>=0){if(visits.ContainsKey(node))revisits++;visits[node]=tick;lastNode=node;}
                if(edge>=0&&edge!=lastEdge&&nav.Graphs[2].EdgeKind[edge]==(byte)NavEdgeKind.Climb)climbs++;lastEdge=edge;
                string phase=run.Combat.Boss?.Phase??"none",attack=run.Combat.Boss?.Attack??"none";
                phaseSeconds[phase]=phaseSeconds.TryGetValue(phase,out var old)?old+Dt:Dt;
                int fieldDist=node<0?NavField.Unreached:nav.Fields[2].Dist(node),version=nav.Fields[2].Version;
                if(version==lastVersion&&fieldDist>=lastDist&&fieldDist>300)noProgress+=Dt;
                lastVersion=version;lastDist=fieldDist;
                if(observed){
                    // Categorías exclusivas por prioridad. Movimiento y fases son ejes independientes.
                    int category=observer.State==1?0:observer.State==3?1:observer.Dash?2:observer.Queue?3:observer.Hold?4:observer.Geometry&&moved<1e-5?5:6;
                    categories[category]+=Dt;motion[observer.Motion]+=Dt;
                    if(observer.Geometry){geometry+=Dt;if(e.DetourTime[i]>0)detourBlocked++;}
                    if(observer.Queue)queue+=Dt;
                    consecutiveBlocked=observer.Geometry&&moved<1e-5?consecutiveBlocked+Dt:0;maxBlocked=Math.Max(maxBlocked,consecutiveBlocked);
                }
                if(!c.Queries.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),e.Radius(i),e.Def(i).height,NavGraph.Step)||e.Y[i]<c.Terrain.HeightAt(e.X[i],e.Z[i])-1e-3)incidents++;
                if(tick%60==0){managedPeak=Math.Max(managedPeak,GC.GetTotalMemory(false));Memory(out long ws,out long priv);workingPeak=Math.Max(workingPeak,ws);privatePeak=Math.Max(privatePeak,priv);}
                if(observed){
                    var g=nav.Graphs[2];
                    csv.AppendLine($"{tick},{id},{F(e.X[i])},{F(e.Y[i])},{F(e.Z[i])},{F(run.Combat.Player.X)},{F(run.Combat.Player.Y)},{F(run.Combat.Player.Z)},{F(horizontal)},{F(vertical)},{F(surface)},{stable},{region},{node},{(node<0?-1:nav.Fields[2].Parent(node))},{edge},{nav.State(i)},{observer.State},{phase},{attack},{F(moved)},{F(distance)},{observer.Geometry},{observer.Queue},{observer.Hold},{F(e.DetourTime[i])},{F(e.StuckTime[i])},{F(observer.DirX)},{F(observer.DirZ)},{nav.Fields[2].Goal},{version},{fieldDist},{F(observer.Fields[2].PublishedSourceAt<0?-1:t+Dt-observer.Fields[2].PublishedSourceAt)},{e.Count},{(node<0?-1:g.NodeCell[node])},{g.CellOf(e.X[i],e.Z[i])},{F(e.Vx[i])},{F(e.Vz[i])}");
                    if(node>=0&&dumped.Add(node))for(int a=g.EdgeStart[node];a<g.EdgeStart[node+1];a++){
                        int dest=g.EdgeTo[a];topology.AppendLine($"{node},{g.NodeCell[node]},{F(g.NodeX(node))},{F(g.NodeY[node])},{F(g.NodeZ(node))},{a},{dest},{g.NodeCell[dest]},{F(g.NodeX(dest))},{F(g.NodeY[dest])},{F(g.NodeZ(dest))},{g.EdgeKind[a]},{g.EdgeCost[a]}");
                    }
                }
                if(!moving&&arrival>=0)break;
            }
            string logical=$"ticks={steps} height={F(firstHeight)} region={F(firstRegion)} arrival={F(arrival)} distance={F(distance)} away={F(away)} turnsRadians={F(turn)} reverseStepDistance={F(backtrack)} revisits={revisits} climbsBoss={climbs} peakCount={peak} incidents={incidents}";
            if(observed){
                var report=new StringBuilder();report.AppendLine($"HORDA750 {mode} moving={moving} repeat={repeat} id={id} dt={F(Dt)} budget={nav.Budget} durationMax=180 warmup=2");
                report.AppendLine($"start={F(from.X)}/{F(from.Y)}/{F(from.Z)} target={F(at.X)}/{F(at.Y)}/{F(at.Z)}");report.AppendLine(logical);
                report.AppendLine($"atHeight distance={F(stopAtHeightDistance)} horizontal={F(heightHorizontal)} vertical={F(heightVertical)}");
                string[] names={"windup","recover","dash","physicalQueue","fieldHold","geometryStopped","otherLocomotion"};
                for(int k=0;k<categories.Length;k++)report.AppendLine($"exclusive-{names[k]}={F(categories[k])}");
                foreach(var p in phaseSeconds)report.AppendLine($"phase-{p.Key}={F(p.Value)}");
                for(int k=0;k<motion.Length;k++)report.AppendLine($"motion{k}={F(motion[k])}");
                report.AppendLine($"geometryAnySeconds={F(geometry)} geometryWithDetourSeconds={F(detourBlocked*Dt)} longestGeometryStop={F(maxBlocked)} physicalQueueBossSeconds={F(queue)} fieldNonProgressSeconds={F(noProgress)}");
                report.AppendLine("observedStepMs "+Stats(cpu));report.AppendLine("navigationBeginDirectionMoveMs "+Stats(observer.Cpu));
                report.AppendLine("productionNavigationBeginMoveMs "+Stats(productionCpu));
                report.AppendLine($"primitiveArrayPayloadBytes={payload} editorManagedSampledPeakBytes={managedPeak} editorWorkingSetSampledPeakBytes={workingPeak} editorPrivateSampledPeakBytes={privatePeak} navigationBeginMoveAllocatedBytes={observer.Allocated}");
                for(int k=0;k<3;k++){var f=observer.Fields[k];report.AppendLine($"class{k} requests={f.Requests} publications={f.Publications} requestToPublishSeconds {Stats(f.Latency)} calculationSeconds {Stats(f.Calculation)} sourceAgeSeconds {(moving?Stats(f.Age):"N/A-fixed-target")}");}
                File.WriteAllText(Path.Combine(folder,$"{repeat}.csv"),csv.ToString());File.WriteAllText(Path.Combine(folder,$"{repeat}-fields.csv"),observer.Events.ToString());File.WriteAllText(Path.Combine(folder,$"{repeat}-topology.csv"),topology.ToString());File.WriteAllText(Path.Combine(folder,$"{repeat}.txt"),report.ToString());TestContext.Out.WriteLine(report.ToString());
                Assert.That(observer.Samples,Is.EqualTo(steps));double sum=0;foreach(double s in categories)sum+=s;Assert.That(sum,Is.EqualTo(steps*Dt).Within(1e-7));
            }
            Assert.That(incidents,Is.Zero);Assert.That(nav.Budget,Is.EqualTo(3000));
            return logical;
        }
    }
}
