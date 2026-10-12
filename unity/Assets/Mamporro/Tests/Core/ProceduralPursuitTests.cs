using System;
using System.Collections.Generic;
using System.Text;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C5: horda real con navegación sobre el circuito C, por semilla. Mismas comprobaciones que el
    // estrés del circuito B (ningún cuerpo en un sólido ni bajo el terreno, atascados ≤ 2 %) y
    // tiempo de publicación de la ruta tras un cambio de posición o altura del jugador (objetivo 0,3 s).
    public sealed class ProceduralPursuitTests
    {
        static readonly Dictionary<string,VerticalCircuit> circuits=new Dictionary<string,VerticalCircuit>();
        static VerticalCircuit Circuit(string seed){if(!circuits.TryGetValue(seed,out var c))circuits[seed]=c=new VerticalCircuit(new ProceduralMap(seed));return c;}
        static WorldRun Run(VerticalCircuit c,Vec3 at,out EnemyNavigation nav)
        {
            var r=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            nav=new EnemyNavigation(c,r.Combat.Enemies.Capacity,c.Map.Graphs);r.Combat.Enemies.Navigation=nav;r.Combat.Vertical=nav;
            r.Combat.WeaponsOff=true;r.Combat.Invincible=true;r.Body.PlaceAt(at.X,at.Y,at.Z);r.SyncPlayer();return r;
        }
        // Mismo reparto que VerticalQa.SpawnPursuit en C (terreno donde cabe cada cuerpo) y el jefe.
        static void Spawn(VerticalCircuit c,WorldRun r,int count)
        {
            var graphs=c.Map.Graphs;int placed=0,elites=count/100;
            for(int sample=0;placed<count-1&&sample<count*40;sample++){
                double angle=sample*2.399963229728653,rad=4+(sample%60)*.75,x=Math.Cos(angle)*rad,z=Math.Sin(angle)*rad;
                int type=placed<elites?4:(placed%10<5?0:placed%10<7?1:placed%10<9?2:3);
                var g=graphs[NavClass.Of(Catalog.Enemies[type])];double y=c.Terrain.HeightAt(x,z);int n=g.NodeAt(x,y+.01,z,.01);
                if(n<0||!g.NodeTerrain[n]||Math.Abs(g.SurfaceY(n,x,z)-y)>.01)continue;r.Combat.Spawn(type,x,z);placed++;
            }
            var gl=graphs[2];var to=ProceduralMap.Reach(gl,c.Map.SpawnNode(2));var back=ProceduralMap.Reach(gl,c.Map.SpawnNode(2),true);double bx=-12,bz=0,best=double.MaxValue;
            for(int n=0;n<gl.NodeCount;n++){if(!to[n]||!back[n]||!gl.NodeTerrain[n]||Math.Abs(gl.NodeY[n])>.01)continue;double dx=gl.NodeX(n)+14,dz=gl.NodeZ(n),d=dx*dx+dz*dz;if(d<best){best=d;bx=gl.NodeX(n);bz=gl.NodeZ(n);}}
            r.Combat.SpawnBoss(bx,bz);
        }

        [TestCase("P0QA",300)] [TestCase("P0QA",750)] [TestCase("COLINAS7",300)] [TestCase("COLINAS7",750)] [TestCase("HORDA750",300)] [TestCase("HORDA750",750)]
        public void MixedHordeNeverEntersSolidsOnTheProceduralMap(string seed,int count)
        {
            var c=Circuit(seed);var spots=c.Map.KeySpots();var r=Run(c,spots[0].at,out var nav);Spawn(c,r,count);var e=r.Combat.Enemies;
            var report=new StringBuilder();int incidents=0,stuckPeak=0;var seen=new HashSet<uint>();const int SpotTicks=7*60;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int t=0;t<SpotTicks*spots.Count;t++){
                if(t%SpotTicks==0){var s=spots[t/SpotTicks].at;r.Body.PlaceAt(s.X,s.Y,s.Z);r.SyncPlayer();}
                r.Step(default,1.0/60);
                int stuck=0;for(int i=0;i<e.Count;i++)if(nav.GeometryBlockedSeconds(i)>2)stuck++;
                if(t%SpotTicks>3*60)stuckPeak=Math.Max(stuckPeak,stuck);
                for(int i=0;i<e.Count;i++){
                    var d=e.Def(i);var p=new Vec3(e.X[i],e.Y[i],e.Z[i]);
                    bool solid=!c.Queries.ClearAboveStep(p,d.radius,d.height,NavGraph.Step),below=e.Y[i]<c.Terrain.HeightAt(e.X[i],e.Z[i])-1e-3;
                    if(!solid&&!below)continue;incidents++;
                    if(seen.Add(e.Id[i])&&seen.Count<=12){
                        int edge=nav.Edge(i);var g=c.Map.Graphs[nav.ClassOf(i)];
                        report.Append($"t={t/60.0:F2}s {d.id} id={e.Id[i]} estado={nav.State(i)} nodo={nav.Node(i)} arista={edge}{(edge>=0?$"({(NavEdgeKind)g.EdgeKind[edge]})":"")} "+
                            $"pos=({e.X[i]:F2},{e.Y[i]:F3},{e.Z[i]:F2}) prev=({e.Px[i]:F2},{e.Py[i]:F3},{e.Pz[i]:F2}) sólido={solid} bajoTerreno={below}\n");
                    }
                }
            }
            TestContext.Out.WriteLine($"{seed} con {e.Count} enemigos, {spots.Count} posiciones ({string.Join(", ",spots.ConvertAll(s=>s.name))}): {watch.Elapsed.TotalSeconds:F0} s de simulación");
            TestContext.Out.WriteLine($"pico de atascados contra la geometría (>2 s con ruta): {stuckPeak}/{e.Count}; incidencias {incidents}; escaladas {nav.ClimbStarts}, bordes {nav.LedgesDone}, caídas {nav.DropsStarted}, sueltas {nav.Detaches}, recuperaciones {nav.Recoveries}/{nav.RecoverFailures}");
            TestContext.Out.WriteLine(report.ToString());
            Assert.That(incidents,Is.Zero);Assert.That(stuckPeak,Is.LessThanOrEqualTo(e.Count*.02),"≤ 2 % atascados");
        }

        // Cada cambio de posición o altura del jugador: tiempo hasta que el campo de cada clase con
        // enemigos se publica con la nueva meta, con 750 enemigos (las tres clases comparten el
        // presupuesto de nodos por tick), y coste de navegación por tick, para varios presupuestos.
        // El objetivo de 0,3 s no se cumple con el presupuesto heredado de P0-B (3000): se mide la
        // curva para que el autor decida. Aquí solo se protege contra regresiones.
        [TestCase("P0QA")] [TestCase("COLINAS7")] [TestCase("HORDA750")]
        public void RouteUpdateTimeAndNavigationCostByBudget(string seed)
        {
            var c=Circuit(seed);var spots=c.Map.KeySpots();var g=c.Map.Graphs;
            TestContext.Out.WriteLine($"{seed}: nodos S/M/L {g[0].NodeCount}/{g[1].NodeCount}/{g[2].NodeCount}");
            foreach(int budget in new[]{3000,4500,6000,9000}){
                var r=Run(c,spots[0].at,out var nav);nav.Budget=budget;Spawn(c,r,750);
                for(int t=0;t<60;t++)r.Step(default,1.0/60);
                var worst=new double[3];var navMs=new List<double>();
                for(int lap=0;lap<2;lap++)for(int s=1;s<=spots.Count;s++){
                    var at=spots[s%spots.Count].at;r.Body.PlaceAt(at.X,at.Y,at.Z);r.SyncPlayer();
                    var version=new int[3];var done=new double[]{-1,-1,-1};for(int k=0;k<3;k++)version[k]=nav.Fields[k].Version;
                    double time=0;
                    while((done[0]<0||done[1]<0||done[2]<0)&&time<3){
                        long before=nav.Ticks;r.Step(default,1.0/60);time+=1.0/60;navMs.Add((nav.Ticks-before)*1000.0/System.Diagnostics.Stopwatch.Frequency);
                        for(int k=0;k<3;k++)if(done[k]<0&&nav.Fields[k].Version!=version[k]&&nav.Fields[k].Goal==nav.GoalFor(k,r.Combat.Player))done[k]=time;
                    }
                    for(int k=0;k<3;k++)worst[k]=Math.Max(worst[k],done[k]<0?99:done[k]);
                }
                navMs.Sort();double p95=navMs[(int)(navMs.Count*.95)],max=navMs[navMs.Count-1];
                TestContext.Out.WriteLine($"  presupuesto {budget}: peor publicación S {worst[0]:F3} s, M {worst[1]:F3} s, L {worst[2]:F3} s; navegación por tick p95 {p95:F3} ms, máx {max:F3} ms (EditMode, orientativo)");
                if(budget==3000)for(int k=0;k<3;k++)Assert.That(worst[k],Is.LessThan(2),$"clase {k}: sin regresión (medido 0,5/0,9/1,3 s)");
            }
        }
    }
}
