using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mamporro.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mamporro.U3
{
    // P0-C5: medidas y evidencias del circuito C en varias semillas. Misma instrumentación que el
    // circuito B (frames, lógica y navegación por tick, GC, memoria, penetraciones), con las
    // posiciones tomadas de los puntos clave de cada mapa y, además, el tiempo de publicación de la
    // ruta de cada clase tras cada cambio de posición del jugador. Medidas de lógica del arnés,
    // nunca FPS generales del juego; la GPU no se mide.
    public sealed partial class P0Capture
    {
        List<(string name,Vec3 at)> procSpots;
        readonly List<double>[] publishTimes={new List<double>(),new List<double>(),new List<double>()};
        readonly int[] publishVersion=new int[3];readonly bool[] publishPending=new bool[3];double publishStart;int publishMissed;
        static string[] ProceduralSeeds()=>(Arg("-p0-seeds")??"P0QA,COLINAS7,HORDA750").Split(',');
        [Serializable] public sealed class ProceduralReport
        {
            public bool valid,development;
            public string source,unity,cpu,gpu,seed,scenario="Circuito C: horda mixta (jefe desde 300) persiguiendo al jugador por los puntos clave del mapa, armas desactivadas, invulnerabilidad QA",gpuTime="N/D";
            public int count,frames,ticks,navigationTicks,resumes,gcCollections,recomputes,climbs,ledges,drops,landings,detaches,recoverFailures,penetrations,belowTerrain,penetrationChecks;
            public int attempt,repairs,rejections,nodesS,nodesM,nodesL,publishMissed;
            public double warmup=Warmup,duration=Duration,p50,p95,p99,max,cpuTickP50,cpuTickP95,cpuTickP99,cpuTickMax,navTickP50,navTickP95,navTickP99,navTickMax,gcBytesPerFrame;
            public double generationMs,mapsMs,totalMapMs,reachablePct,stuckPeakPct,crowdedPeakPct,publishMaxS,publishMaxM,publishMaxL;
            public long allocatedMemory,reservedMemory,monoMemory;
            public string[] spots,atPlayerHeightBySpot;
        }
        void UseProceduralSeed(string seed)
        {
            game.Vertical.UseSeed(seed);game.LoadWorld(game.Seed);procSpots=game.Vertical.Circuit.Map.KeySpots();
        }
        IEnumerator ProceduralBenchmark()
        {
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            foreach(var seed in ProceduralSeeds()){
                UseProceduralSeed(seed);
                foreach(int count in Loads){
                    game.Vertical.NavigationEnabled=true;PreparePursuit(count);
                    var first=procSpots[0];spotName=first.name;game.PlaceQa(first.at);BeginPublish();
                    while(Time.realtimeSinceStartupAsDouble-start<Warmup+Duration){
                        double elapsed=Time.realtimeSinceStartupAsDouble-start;
                        int spot=Math.Min(procSpots.Count-1,(int)(elapsed/((Warmup+Duration)/procSpots.Count)));
                        if(spot!=spotIndex){RecordSpotHeight();spotIndex=spot;var s=procSpots[spot];spotName=s.name;spotReachable=true;game.PlaceQa(s.at);BeginPublish();}
                        TrackPublish();
                        if(elapsed-lastSample>=1){lastSample=elapsed;SamplePursuit(elapsed);}
                        yield return null;
                    }
                    RecordSpotHeight();running=false;pursuit=false;
                    memoryAtEnd=Profiler.GetTotalAllocatedMemoryLong();reservedAtEnd=Profiler.GetTotalReservedMemoryLong();monoAtEnd=GC.GetTotalMemory(false);collectionsAtEnd=GC.CollectionCount(0)-gcStart;
                    game.SetPaused(true);game.HideInterface=true;yield return Shot($"c-{seed}-{count}");SaveProceduralReport(seed,count);
                }
            }
            if(gc.Valid)gc.Dispose();
        }
        // Al cambiar de posición: espera la publicación de cada clase con enemigos (tiempo de partida).
        void BeginPublish()
        {
            var nav=game.Vertical.Navigation;if(nav==null)return;
            for(int k=0;k<3;k++){if(publishPending[k])publishMissed++;publishPending[k]=ClassCount(k)>0;publishVersion[k]=nav.Fields[k].Version;}
            publishStart=game.Run.Time;
        }
        void TrackPublish()
        {
            var nav=game.Vertical.Navigation;if(nav==null)return;
            bool measuring=Time.realtimeSinceStartupAsDouble-start>=Warmup;
            for(int k=0;k<3;k++){
                if(!publishPending[k]||nav.Fields[k].Version==publishVersion[k]||nav.Fields[k].Goal!=nav.GoalFor(k,game.Run.Player))continue;
                publishPending[k]=false;if(measuring)publishTimes[k].Add(game.Run.Time-publishStart);
            }
        }
        int ClassCount(int k){var e=game.Run.Enemies;var nav=game.Vertical.Navigation;int c=0;for(int i=0;i<e.Count;i++)if(nav.ClassOf(i)==k)c++;return c;}
        static double Max(List<double> list){double m=list.Count==0?-1:0;foreach(var v in list)m=Math.Max(m,v);return m;}
        void SaveProceduralReport(string seed,int count)
        {
            var f=Sorted(frames,n);var t=Sorted(ticks,nt);var v=Sorted(navTicks,nnav);var nav=game.Vertical.Navigation;var map=game.Vertical.Circuit.Map;
            var report=new ProceduralReport{
                seed=seed,development=Debug.isDebugBuild,source=Arg("-p0-source","WIP"),unity=Application.unityVersion,
                cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,count=count,frames=n,ticks=nt,navigationTicks=nnav,resumes=resumes,
                p50=P(f,.5),p95=P(f,.95),p99=P(f,.99),max=P(f,1),cpuTickP50=P(t,.5),cpuTickP95=P(t,.95),cpuTickP99=P(t,.99),cpuTickMax=P(t,1),
                navTickP50=P(v,.5),navTickP95=P(v,.95),navTickP99=P(v,.99),navTickMax=P(v,1),
                gcBytesPerFrame=Debug.isDebugBuild&&gcSamples>0?(double)allocated/gcSamples:-1,gcCollections=collectionsAtEnd,
                allocatedMemory=memoryAtEnd,reservedMemory=reservedAtEnd,monoMemory=monoAtEnd,
                attempt=map.Layout.Attempt,repairs=map.Repairs,rejections=map.Rejections,generationMs=map.GenerationMs,mapsMs=map.MapsMs,totalMapMs=map.TotalMs,
                nodesS=map.Graphs[0].NodeCount,nodesM=map.Graphs[1].NodeCount,nodesL=map.Graphs[2].NodeCount,
                penetrations=penetrations,belowTerrain=belowTerrain,penetrationChecks=penetrationChecks,
                reachablePct=reachableSamples>0?reachableSum/reachableSamples:-1,stuckPeakPct=stuckPeak,crowdedPeakPct=crowdedPeak,
                publishMaxS=Max(publishTimes[0]),publishMaxM=Max(publishTimes[1]),publishMaxL=Max(publishTimes[2]),publishMissed=publishMissed,
                spots=procSpots.ConvertAll(s=>s.name).ToArray(),atPlayerHeightBySpot=heightBySpot.ToArray()};
            if(nav!=null){report.recomputes=nav.Fields[0].Recomputes+nav.Fields[1].Recomputes+nav.Fields[2].Recomputes;report.climbs=nav.ClimbStarts;report.ledges=nav.LedgesDone;
                report.drops=nav.DropsStarted;report.landings=nav.Landings;report.detaches=nav.Detaches;report.recoverFailures=nav.RecoverFailures;}
            report.valid=n>100&&nt>1500&&game.Run.Enemies.Count>=count&&penetrations==0&&belowTerrain==0&&report.recoverFailures==0&&(count==0||nnav>1500);
            File.WriteAllText(Path.Combine(output,$"load-{seed}-{count}.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"P0-C {seed} {count} valid={report.valid} penetraciones={penetrations} navP95={report.navTickP95:F3} publicación S/M/L {report.publishMaxS:F2}/{report.publishMaxM:F2}/{report.publishMaxL:F2}");
            for(int k=0;k<3;k++){publishTimes[k].Clear();publishPending[k]=false;}publishMissed=0;
        }

        // ------------------------------------------------------------ evidencias del circuito C
        IEnumerator ProceduralEvidence()
        {
            bool all=true;var notes=new List<string>();
            foreach(var seed in ProceduralSeeds()){
                UseProceduralSeed(seed);var map=game.Vertical.Circuit.Map;
                // Vista cenital del mapa completo (trazado, estructuras, árboles y marcadores).
                PursuitFixture(0,game.Vertical.Circuit.Start.Y,0,0,10);game.SetPaused(true);game.HideInterface=true;game.FreeCamera=true;
                game.worldCamera.transform.SetPositionAndRotation(new Vector3(0,118,0),Quaternion.Euler(90,0,0));
                yield return Shot($"C-{seed}-00-vista-cenital");game.FreeCamera=false;

                // La horda sube a la meseta más alta disponible desde el suelo cercano (rampa o escalada).
                (string name,Vec3 at) top=procSpots[0];foreach(var s in procSpots)if(s.name.StartsWith("meseta")&&s.at.Y>top.at.Y)top=s;
                FindGroundBelow(map,top.at,out double gx,out double gz);
                PursuitFixture(top.at.X,top.at.Y,top.at.Z,gx,gz);SpawnGroup(12,gx,gz);
                yield return Until(()=>CountAbove(top.at.Y-.1)>0,$"{seed}: horda en {top.name}",30);
                game.FaceTowards(gx,gz);yield return Shot($"C-{seed}-01-horda-sube-a-{top.name}");bool climbed=CountAbove(top.at.Y-.1)>0;

                // El jefe llega a la meseta amplia que alcanza (cabe su cuerpo; rampa de 8 m o escalada).
                bool bossUp=false;
                if(map.BossPlateau(out var plateau)){
                    PursuitFixture(plateau.X,plateau.Y,plateau.Z,0,0);game.Vertical.BossStart(out double bx,out double bz);
                    var e=game.Run.Enemies;game.Run.SpawnBoss(bx,bz);int boss=e.Count-1;
                    yield return Until(()=>e.Y[boss]>plateau.Y-.2&&game.Vertical.Navigation.Stable(boss),$"{seed}: jefe en la meseta",45);
                    game.FaceTowards(e.X[boss],e.Z[boss]);yield return Shot($"C-{seed}-02-jefe-en-la-meseta");bossUp=e.Y[boss]>plateau.Y-.2;
                }

                // Marcadores QA junto al jugador (posición y volumen; sus reglas no cambian).
                foreach(var st in map.World.Structures){
                    if(st.Removed||st.Kind!=StructureKind.Shrine)continue;var s=st.Solids[0];
                    double cx=(s.Min.X+s.Max.X)/2,cz=(s.Min.Z+s.Max.Z)/2;
                    PursuitFixture(cx+4,s.Min.Y,cz,cx,cz);yield return new WaitForSeconds(.5f);yield return Shot($"C-{seed}-03-santuario");break;
                }
                notes.Add($"{seed}: horda en {top.name} {climbed}, jefe en meseta {bossUp}, intento {map.Layout.Attempt}, reparaciones {map.Repairs}, rechazos {map.Rejections}");
                all&=climbed&&bossUp;
            }
            game.SetPaused(true);
            var report=new Evidence{valid=visualValid&&all,images=images,
                description="Circuito C: vista cenital, horda subiendo a la meseta más alta, jefe en la meseta amplia y marcadores, por semilla. No es prueba manual. "+string.Join(" | ",notes)};
            File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(report,true));
        }
        // Nodo S de terreno con entrada y salida, al menos 4 m por debajo y lo más cerca posible.
        void FindGroundBelow(ProceduralMap map,Vec3 target,out double x,out double z)
        {
            var g=map.Graphs[0];var to=ProceduralMap.Reach(g,map.SpawnNode(0));var back=ProceduralMap.Reach(g,map.SpawnNode(0),true);
            x=0;z=0;double best=double.MaxValue;
            for(int n=0;n<g.NodeCount;n++){
                if(!to[n]||!back[n]||!g.NodeTerrain[n]||g.NodeY[n]>target.Y-4)continue;
                double dx=g.NodeX(n)-target.X,dz=g.NodeZ(n)-target.Z,d=dx*dx+dz*dz;if(d<best&&d>4){best=d;x=g.NodeX(n);z=g.NodeZ(n);}
            }
        }
    }
}
