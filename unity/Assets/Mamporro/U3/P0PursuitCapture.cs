using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mamporro.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mamporro.U3
{
    // P0-B4: medidas y evidencias del circuito B. Misma instrumentación local que el A (frames,
    // lógica por tick, GC y memoria), más el coste de navegación por tick y métricas de ruta.
    // Compara en la misma build la persecución heredada (línea base B0) y la navegación P0.
    // Son medidas de lógica del arnés, nunca FPS generales del juego; la GPU no se mide.
    public sealed partial class P0Capture
    {
        bool pursuit;bool pursuitNav;int spotIndex;double lastSample;
        // Posición actual del arnés (B: Spots; C: puntos clave del mapa).
        string spotName="suelo";bool spotReachable=true;
        readonly double[] navTicks=new double[10000];int nnav;long navLast;
        readonly List<string> heightBySpot=new List<string>();
        readonly float[] lastDist=new float[WorldRun.EnemyCapacity];readonly double[] lastImprove=new double[WorldRun.EnemyCapacity];
        int penetrations,belowTerrain,penetrationChecks,frameCounter;double reachableSum,stuckPeak,crowdedPeak;int reachableSamples;
        // Seis posiciones del jugador (fixtures QA, no gameplay) recorridas durante los 40 s.
        static readonly (string name,double x,double y,double z,bool reachable)[] Spots={
            ("suelo",0,0,-4,true),("tejado-rampa",11,4,-11,true),("tejado-solo-escalando",-11,5,-11,true),
            ("meseta-terreno",16,5,10,true),("tejado-amplio",0,4,16,true),("seta-inaccesible",20,5.5,20,false)};
        [Serializable] public sealed class PursuitReport
        {
            public bool valid,development,navigation;
            public string source,unity,cpu,gpu,mode,scenario="Circuito B: horda mixta (jefe desde 300) persiguiendo al jugador por seis posiciones, armas desactivadas, invulnerabilidad QA",gpuTime="N/D";
            public int count,frames,ticks,navigationTicks,resumes,gcCollections,recomputes,climbs,ledges,drops,landings,detaches,recoverFailures,queueWaits,penetrations,belowTerrain,penetrationChecks;
            public double warmup=Warmup,duration=Duration,p50,p95,p99,max,cpuTickP50,cpuTickP95,cpuTickP99,cpuTickMax,navTickP50,navTickP95,navTickP99,navTickMax,gcBytesPerFrame,graphBuildMs,reachablePct,stuckPeakPct,crowdedPeakPct;
            public long allocatedMemory,reservedMemory,monoMemory;
            public string[] atPlayerHeightBySpot;
        }
        IEnumerator PursuitBenchmark()
        {
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            foreach(bool nav in new[]{false,true})foreach(int count in Loads){
                game.Vertical.NavigationEnabled=nav;PreparePursuit(count);
                while(Time.realtimeSinceStartupAsDouble-start<Warmup+Duration){
                    double elapsed=Time.realtimeSinceStartupAsDouble-start;
                    int spot=Math.Min(Spots.Length-1,(int)(elapsed/((Warmup+Duration)/Spots.Length)));
                    if(spot!=spotIndex){RecordSpotHeight();spotIndex=spot;var s=Spots[spot];spotName=s.name;spotReachable=s.reachable;game.PlaceQa(new Vec3(s.x,s.y,s.z));}
                    if(elapsed-lastSample>=1){lastSample=elapsed;SamplePursuit(elapsed);}
                    yield return null;
                }
                RecordSpotHeight();running=false;pursuit=false;
                memoryAtEnd=Profiler.GetTotalAllocatedMemoryLong();reservedAtEnd=Profiler.GetTotalReservedMemoryLong();monoAtEnd=GC.GetTotalMemory(false);collectionsAtEnd=GC.CollectionCount(0)-gcStart;
                game.SetPaused(true);game.HideInterface=true;yield return Shot($"b-{(nav?"navegacion":"heredado")}-{count}");SavePursuitReport(count);
            }
            if(gc.Valid)gc.Dispose();
        }
        void PreparePursuit(int count)
        {
            running=false;game.HideInterface=false;game.StartRun("P0QA");game.Run.Invincible=true;game.Run.WeaponsOff=true;
            var s=Spots[0];game.PlaceQa(new Vec3(s.x,s.y,s.z));game.AutoChoose=true;game.SetPaused(false);
            game.Vertical.InputProvider=()=>default;game.Vertical.Spawn(count);load=count;pursuitNav=game.Vertical.Navigation!=null;
            n=nt=nnav=resumes=gcSamples=0;allocated=0;gcStart=GC.CollectionCount(0);rendered=game.RenderedFrames;spotIndex=0;lastSample=0;
            spotName=Spots[0].name;spotReachable=true;heightBySpot.Clear();penetrations=belowTerrain=penetrationChecks=frameCounter=0;reachableSum=stuckPeak=crowdedPeak=0;reachableSamples=0;
            for(int i=0;i<lastDist.Length;i++){lastDist[i]=float.MaxValue;lastImprove[i]=0;}
            navLast=pursuitNav?game.Vertical.Navigation.Ticks:0;
            previous=start=Time.realtimeSinceStartupAsDouble;pursuit=true;running=true;
        }
        void RecordPursuitTick(double cpu)
        {
            bool measuring=Time.realtimeSinceStartupAsDouble-start>=Warmup;
            if(measuring&&nt<ticks.Length)ticks[nt++]=cpu;
            if(!pursuitNav)return;long now=game.Vertical.Navigation.Ticks;
            if(measuring&&nnav<navTicks.Length)navTicks[nnav++]=(now-navLast)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            navLast=now;
            // Comprobación de penetración fuera del tiempo medido de lógica: cada 15 ticks.
            if(++frameCounter%15==0)CheckBodies();
        }
        void CheckBodies()
        {
            var e=game.Run.Enemies;var q=game.Vertical.Circuit.Queries;var terrain=game.Vertical.Circuit.Terrain;
            for(int i=0;i<e.Count;i++){
                var d=e.Def(i);penetrationChecks++;
                if(!q.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),d.radius,d.height,NavGraph.Step))penetrations++;
                if(e.Y[i]<terrain.HeightAt(e.X[i],e.Z[i])-1e-3)belowTerrain++;
            }
        }
        // Por segundo, en posiciones alcanzables: % con ruta válida; atascados = con ruta y bloqueados
        // contra la geometría más de 2 s; aglomerados = sin acortar su distancia de campo en 2 s
        // caminando a más de 3 m de su meta (esperan tras otros enemigos o en la cola de una pared).
        void SamplePursuit(double elapsed)
        {
            var nav=game.Vertical.Navigation;var e=game.Run.Enemies;if(nav==null||e.Count==0||!spotReachable)return;
            int reachable=0,stuck=0,crowded=0;
            for(int i=0;i<e.Count;i++){
                if(!nav.Reachable(i))continue;reachable++;
                int d=nav.Fields[nav.ClassOf(i)].Dist(nav.Node(i));
                if(d<lastDist[i]-20){lastDist[i]=d;lastImprove[i]=elapsed;}
                else if(d>300&&elapsed-lastImprove[i]>2&&nav.State(i)==EnemyNavigation.Walk)crowded++;
                if(nav.GeometryBlockedSeconds(i)>2)stuck++;
            }
            if(elapsed>=Warmup){reachableSum+=100.0*reachable/e.Count;reachableSamples++;stuckPeak=Math.Max(stuckPeak,100.0*stuck/e.Count);crowdedPeak=Math.Max(crowdedPeak,100.0*crowded/e.Count);}
        }
        void RecordSpotHeight()
        {
            var e=game.Run.Enemies;var b=game.Body;int near=0;
            for(int i=0;i<e.Count;i++)if(Math.Abs(e.Y[i]-b.Y)<1&&Rules.Hypot(e.X[i]-b.X,e.Z[i]-b.Z)<20)near++;
            heightBySpot.Add($"{spotName}={(e.Count==0?0:100.0*near/e.Count):F1}%");
            for(int i=0;i<lastDist.Length;i++){lastDist[i]=float.MaxValue;lastImprove[i]=Time.realtimeSinceStartupAsDouble-start;}
        }
        void SavePursuitReport(int count)
        {
            var f=Sorted(frames,n);var t=Sorted(ticks,nt);var v=Sorted(navTicks,nnav);var nav=game.Vertical.Navigation;
            var report=new PursuitReport{
                navigation=pursuitNav,mode=pursuitNav?"navegacion":"heredado",development=Debug.isDebugBuild,source=Arg("-p0-source","WIP"),unity=Application.unityVersion,
                cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,count=count,frames=n,ticks=nt,navigationTicks=nnav,resumes=resumes,
                p50=P(f,.5),p95=P(f,.95),p99=P(f,.99),max=P(f,1),cpuTickP50=P(t,.5),cpuTickP95=P(t,.95),cpuTickP99=P(t,.99),cpuTickMax=P(t,1),
                navTickP50=P(v,.5),navTickP95=P(v,.95),navTickP99=P(v,.99),navTickMax=P(v,1),
                gcBytesPerFrame=Debug.isDebugBuild&&gcSamples>0?(double)allocated/gcSamples:-1,gcCollections=collectionsAtEnd,
                allocatedMemory=memoryAtEnd,reservedMemory=reservedAtEnd,monoMemory=monoAtEnd,graphBuildMs=game.Vertical.GraphBuildMs,
                penetrations=penetrations,belowTerrain=belowTerrain,penetrationChecks=penetrationChecks,
                reachablePct=reachableSamples>0?reachableSum/reachableSamples:-1,stuckPeakPct=pursuitNav?stuckPeak:-1,crowdedPeakPct=pursuitNav?crowdedPeak:-1,atPlayerHeightBySpot=heightBySpot.ToArray()};
            if(nav!=null){report.recomputes=nav.Fields[0].Recomputes+nav.Fields[1].Recomputes+nav.Fields[2].Recomputes;report.climbs=nav.ClimbStarts;report.ledges=nav.LedgesDone;
                report.drops=nav.DropsStarted;report.landings=nav.Landings;report.detaches=nav.Detaches;report.recoverFailures=nav.RecoverFailures;report.queueWaits=nav.QueueWaits;}
            // Los esbirros del estornudo del jefe pueden sumar enemigos a la carga pedida.
            report.valid=n>100&&nt>1500&&game.Run.Enemies.Count>=count&&(!pursuitNav||penetrations==0&&belowTerrain==0&&report.recoverFailures==0&&nnav>1500);
            File.WriteAllText(Path.Combine(output,$"load-{report.mode}-{count}.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"P0-B {report.mode} {count} valid={report.valid} penetraciones={penetrations} navP95={report.navTickP95:F3}");
        }

        // ------------------------------------------------------------ evidencias del circuito B
        IEnumerator PursuitEvidence()
        {
            // Cada fixture crea una partida nueva: la lista de enemigos se lee tras prepararla.
            PursuitFixture(-11,5,-8.6,-11,0);SpawnGroup(12,-11,-1.5);
            yield return Until(()=>AnyIn(EnemyNavigation.Climb),"escalada de pared",12);
            game.FaceTowards(-11,0);yield return Shot("B01-escalan-la-pared");
            yield return Until(()=>CountAbove(4.9)>0,"llegada al tejado",15);
            yield return Shot("B02-llegan-al-tejado");bool roof=CountAbove(4.9)>0;

            PursuitFixture(11,4,-11,20,-11);SpawnGroup(8,18,-11);SpawnGroup(8,-4,-11);
            yield return Until(()=>CountAbove(3.9)>=4,"tejado con rampa y pared",15);
            yield return Shot("B03-rampa-y-pared");bool ramp=CountAbove(3.9)>=4;

            PursuitFixture(16,5,10,8,10);SpawnGroup(10,9.5,10);
            yield return Until(()=>AnyIn(EnemyNavigation.Climb),"pared de la meseta",12);yield return Shot("B04-meseta-de-terreno");
            yield return Until(()=>CountAbove(4.9)>0,"cima de la meseta",15);bool plateau=CountAbove(4.9)>0;

            PursuitFixture(0,4,16,0,4);var e=game.Run.Enemies;game.Run.SpawnBoss(-12,0);int boss=e.Count-1;
            yield return Until(()=>game.Vertical.Navigation.State(boss)==EnemyNavigation.Climb,"jefe escalando",25);
            game.FaceTowards(e.X[boss],e.Z[boss]);yield return Shot("B05-jefe-escalando");
            yield return Until(()=>e.Y[boss]>3.9&&game.Vertical.Navigation.Stable(boss),"jefe en el tejado",25);
            yield return Shot("B06-jefe-en-el-tejado");bool bossUp=e.Y[boss]>3.9;

            PursuitFixture(0,4,16,0,4);e=game.Run.Enemies;for(int k=0;k<8;k++){int i=game.Run.Spawn(k%3,-3+k*.9,14);e.Y[i]=4;e.Py[i]=4;}
            game.PlaceQa(new Vec3(0,0,4));
            yield return Until(()=>AnyIn(EnemyNavigation.Drop),"bajada desde el tejado",8);game.FaceTowards(0,12);yield return Shot("B07-bajan-cayendo");
            yield return Until(()=>CountBelow(.1)>=4,"aterrizaje",8);bool drop=CountBelow(.1)>=4;

            // Sin palomas: aquí se comprueba que el cuerpo a cuerpo no alcanza una cima inaccesible.
            PursuitFixture(20,5.5,20,10,10);game.Run.Invincible=false;double hp=game.Run.Hp;
            for(int k=0;k<10;k++)game.Run.Spawn(k%3,8+(k%4-1.5)*1.1,4+(k/4)*1.1);
            yield return new WaitForSeconds(8);game.FaceTowards(10,10);yield return Shot("B08-seta-inaccesible-esperan");
            bool safe=game.Run.Hp>=hp&&CountAbove(5)==0;game.Run.Invincible=true;

            PursuitFixture(-11,5,-8.6,-11,0);for(int k=0;k<4;k++)game.Run.Spawn(3,-13+k*1.5,-1);
            yield return Until(()=>game.Run.EnemyShots.Count>0,"disparo de paloma hacia el tejado",15);
            yield return Shot("B09-disparo-hacia-el-tejado");bool shot=game.Run.EnemyShots.Count>0;

            game.SetPaused(true);game.HideInterface=true;game.FreeCamera=true;
            game.worldCamera.transform.SetPositionAndRotation(new Vector3(0,58,0),Quaternion.Euler(90,0,0));
            yield return Shot("B00-esquema-circuito-b");
            var report=new Evidence{valid=visualValid&&roof&&ramp&&plateau&&bossUp&&drop&&safe&&shot,images=images,
                description="Circuito B: fixtures independientes de persecución vertical con la horda real. No es prueba manual."};
            File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(report,true));
        }
        void PursuitFixture(double x,double y,double z,double faceX,double faceZ)
        {
            running=false;game.HideInterface=false;game.FreeCamera=false;game.StartRun("P0QA");game.Run.Invincible=true;game.Run.WeaponsOff=true;
            game.PlaceQa(new Vec3(x,y,z));game.FaceTowards(faceX,faceZ);game.AutoChoose=true;game.SetPaused(false);game.Vertical.InputProvider=()=>default;
            pursuit=false;running=true;
        }
        void SpawnGroup(int count,double x,double z){for(int k=0;k<count;k++)game.Run.Spawn(k%4,x+(k%4-1.5)*1.1,z+(k/4)*1.1);}
        bool AnyIn(byte state){var nav=game.Vertical.Navigation;for(int i=0;i<game.Run.Enemies.Count;i++)if(nav.State(i)==state)return true;return false;}
        int CountAbove(double y){int c=0;var e=game.Run.Enemies;for(int i=0;i<e.Count;i++)if(e.Y[i]>y)c++;return c;}
        int CountBelow(double y){int c=0;var e=game.Run.Enemies;for(int i=0;i<e.Count;i++)if(e.Y[i]<y)c++;return c;}
    }
}
