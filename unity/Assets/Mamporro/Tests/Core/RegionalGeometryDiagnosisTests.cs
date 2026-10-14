using System;
using System.Globalization;
using System.IO;
using System.Text;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalGeometryDiagnosisTests
    {
        VerticalCircuit circuit;
        [OneTimeSetUp] public void Create()=>circuit=new VerticalCircuit(new ProceduralMap("HORDA750"));
        static string F(double v)=>v.ToString("F9",CultureInfo.InvariantCulture);

        [Test]
        public void RecordsInheritedRoundingMismatchWithoutChangingRuntime()
        {
            // Reproducción mínima del tick 1270 de no-minions-fixed. Describe el defecto actual;
            // al aprobar su corrección sustituir estas aserciones por el invariante celda/posición.
            var c=circuit;var run=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false};
            var e=run.Combat.Enemies;var nav=new EnemyNavigation(c,e.Capacity,c.Map.Graphs);e.Navigation=nav;
            int i=run.Combat.Spawn(5,21.499996185302734,-21.207071304321289);
            nav.Begin(e,run.Combat.Player,1.0/60);nav.Move(i,0,0,0,0,e.Speed[i],false,false,1.0/60);
            var g=nav.Graphs[2];int node=nav.Node(i);Assert.That(node,Is.EqualTo(5088));
            double x=21.499999735302843,z=-21.207066287654744;
            Assert.That(g.CellOf(x,z),Is.EqualTo(g.NodeCell[node]));
            Assert.That(nav.TryPlace(i,x,z),Is.True);
            Assert.That(g.NodeCell[nav.Node(i)],Is.EqualTo(21930));
            Assert.That(g.CellOf(e.X[i],e.Z[i]),Is.EqualTo(21931));
            TestContext.Out.WriteLine($"TryPlace accepted x={F(x)} storedX={F(e.X[i])} nodeCell=21930 positionCell=21931; reproducción del defecto heredado, no criterio de aceptación.");
        }

        [Test]
        public void CompareStaticPhysicalRoutesBySize()
        {
            var c=circuit;var from=RegionalBaselineTests.Start(c);Assert.That(c.Map.BossPlateau(out var at),Is.True);
            var report=new StringBuilder("class,start,goal,graphCostMetres,physicalPolylineMetres,walk,climb,drop,offlineSlices3000\n");
            for(int k=0;k<3;k++){
                var g=c.Map.Graphs[k];int start=g.NodeAt(from.X,from.Y,from.Z,.01),goal=g.NodeAt(at.X,at.Y,at.Z,.01);
                Assert.That(start,Is.GreaterThanOrEqualTo(0));Assert.That(goal,Is.GreaterThanOrEqualTo(0));
                var field=new NavField(g);int slices=0;
                // Consulta estática de referencia, sin entidades ni controlador. Cada avance conserva 3000.
                while(field.Version==0&&slices++<1000)field.Update(goal,at.X,at.Y,at.Z,1.0/60,3000);
                Assert.That(field.Reachable(start),Is.True);double length=0;int n=start,walk=0,climb=0,drop=0,visited=0;
                while(n!=goal&&visited++<g.NodeCount){
                    int edge=field.Parent(n);Assert.That(edge,Is.GreaterThanOrEqualTo(0));int to=g.EdgeTo[edge];
                    if(g.EdgeKind[edge]==(byte)NavEdgeKind.Walk){walk++;length+=Distance(g.NodeX(to)-g.NodeX(n),g.NodeY[to]-g.NodeY[n],g.NodeZ(to)-g.NodeZ(n));}
                    else{
                        if(g.EdgeKind[edge]==(byte)NavEdgeKind.Climb)climb++;else drop++;
                        int p=g.EdgePath[edge];for(int j=g.PathStart[p]+1;j<g.PathStart[p+1];j++)length+=Distance(g.PathX[j]-g.PathX[j-1],g.PathY[j]-g.PathY[j-1],g.PathZ[j]-g.PathZ[j-1]);
                    }
                    n=to;
                }
                Assert.That(n,Is.EqualTo(goal));report.AppendLine($"{k},{start},{goal},{F(field.Dist(start)/(double)NavGraph.Scale)},{F(length)},{walk},{climb},{drop},{slices}");
            }
            string folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../TestResults/P0/D0/Static",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"routes.csv"),report.ToString());TestContext.Out.WriteLine(report.ToString());
        }
        static double Distance(double x,double y,double z)=>Math.Sqrt(x*x+y*y+z*z);
    }
}
