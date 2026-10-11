using System;
using System.Collections.Generic;
using System.Text;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B: horda mixta grande en el circuito B con el jugador recorriendo las posiciones del
    // arnés. Verifica cada tick que ningún cuerpo entra en un sólido ni baja del terreno y
    // describe cada incidencia (tipo, estado, arista, posición) para poder corregirla.
    public sealed class PursuitStressTests
    {
        static readonly (double x,double y,double z)[] Spots={(0,0,-4),(11,4,-11),(-11,5,-11),(16,5,10),(0,4,16),(20,5.5,20)};
        [TestCase(300)] [TestCase(750)]
        public void MixedHordeNeverEntersSolidsWhileThePlayerMovesBetweenSpots(int count)
        {
            var c=new VerticalCircuit(VerticalLayout.Persecucion);var graphs=EnemyNavigation.BuildGraphs(c);
            var r=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false,Cheated=true};
            var nav=new EnemyNavigation(c,r.Combat.Enemies.Capacity,graphs);r.Combat.Enemies.Navigation=nav;r.Combat.Vertical=nav;
            r.Combat.WeaponsOff=true;r.Combat.Invincible=true;r.Body.PlaceAt(0,0,-4);r.SyncPlayer();
            var e=r.Combat.Enemies;int placed=0;
            for(int sample=0;placed<count-1&&sample<count*40;sample++){
                double angle=sample*2.399963229728653,rad=4+(sample%60)*.38,x=Math.Cos(angle)*rad,z=Math.Sin(angle)*rad;
                int type=placed<count/100?4:(placed%10<5?0:placed%10<7?1:placed%10<9?2:3);
                var g=graphs[NavClass.Of(Catalog.Enemies[type])];double y=c.Terrain.HeightAt(x,z);int n=g.NodeAt(x,y+.01,z,.01);
                if(n<0||!g.NodeTerrain[n]||Math.Abs(g.SurfaceY(n,x,z)-y)>.01)continue;r.Combat.Spawn(type,x,z);placed++;
            }
            r.Combat.SpawnBoss(-12,0);
            var report=new StringBuilder();int incidents=0,stuckPeak=0;var seen=new HashSet<uint>();
            for(int t=0;t<60*42;t++){
                if(t%(7*60)==0){var s=Spots[(t/(7*60))%Spots.Length];r.Body.PlaceAt(s.x,s.y,s.z);r.SyncPlayer();}
                r.Step(default,1.0/60);
                int stuck=0;for(int i=0;i<e.Count;i++)if(nav.GeometryBlockedSeconds(i)>2)stuck++;
                if(t%(7*60)>3*60&&(t/(7*60))%Spots.Length!=5)stuckPeak=Math.Max(stuckPeak,stuck);
                for(int i=0;i<e.Count;i++){
                    var d=e.Def(i);var p=new Vec3(e.X[i],e.Y[i],e.Z[i]);
                    bool solid=!c.Queries.ClearAboveStep(p,d.radius,d.height,NavGraph.Step),below=e.Y[i]<c.Terrain.HeightAt(e.X[i],e.Z[i])-1e-3;
                    if(!solid&&!below)continue;incidents++;
                    if(seen.Add(e.Id[i])&&seen.Count<=12){
                        int edge=nav.Edge(i);var g=graphs[nav.ClassOf(i)];
                        report.Append($"t={t/60.0:F2}s {d.id} id={e.Id[i]} estado={nav.State(i)} nodo={nav.Node(i)} arista={edge}{(edge>=0?$"({(NavEdgeKind)g.EdgeKind[edge]})":"")} "+
                            $"pos=({e.X[i]:F2},{e.Y[i]:F3},{e.Z[i]:F2}) prev=({e.Px[i]:F2},{e.Py[i]:F3},{e.Pz[i]:F2}) sólido={solid} bajoTerreno={below} ataque={e.State[i]}\n");
                    }
                }
            }
            TestContext.Out.WriteLine($"pico de atascados contra la geometría (>2 s con ruta): {stuckPeak}/{e.Count}");
            TestContext.Out.WriteLine($"incidencias {incidents}; escaladas {nav.ClimbStarts}, bordes {nav.LedgesDone}, caídas {nav.DropsStarted}, sueltas {nav.Detaches}, recuperaciones {nav.Recoveries}/{nav.RecoverFailures}, esperas de cola {nav.QueueWaits}");
            TestContext.Out.WriteLine(report.ToString());
            Assert.That(incidents,Is.Zero);Assert.That(stuckPeak,Is.LessThanOrEqualTo(e.Count*.02),"≤ 2 % atascados");
        }
    }
}
