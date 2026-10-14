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
        public void RoundedStepRejectsDisconnectedCellAndPreservesSliding()
        {
            // Reproducción mínima del tick 1270 de no-minions-fixed, ahora regresión.
            var c=circuit;var run=new WorldRun(c.Data(),"P0QA",Catalog.Characters[0]){Automatic=false};
            var e=run.Combat.Enemies;var nav=new EnemyNavigation(c,e.Capacity,c.Map.Graphs);e.Navigation=nav;
            int i=run.Combat.Spawn(5,21.499996185302734,-21.207071304321289);
            nav.Begin(e,run.Combat.Player,1.0/60);nav.Move(i,0,0,0,0,e.Speed[i],false,false,1.0/60);
            var g=nav.Graphs[2];int node=nav.Node(i);Assert.That(node,Is.EqualTo(5088));
            double x=21.499999735302843,z=-21.207066287654744;
            Assert.That(g.CellOf(x,z),Is.EqualTo(g.NodeCell[node]));
            float oldX=e.X[i],oldY=e.Y[i],oldZ=e.Z[i];
            Assert.That(nav.TryPlace(i,x,z),Is.False);
            Assert.That(e.X[i],Is.EqualTo(oldX));Assert.That(e.Y[i],Is.EqualTo(oldY));Assert.That(e.Z[i],Is.EqualTo(oldZ));
            Assert.That(g.NodeCell[nav.Node(i)],Is.EqualTo(21930));
            Assert.That(g.CellOf(e.X[i],e.Z[i]),Is.EqualTo(g.NodeCell[nav.Node(i)]));
            Assert.That(nav.TryPlace(i,oldX,z),Is.True,"El intento por el eje libre conserva el deslizamiento");
            Assert.That(g.CellOf(e.X[i],e.Z[i]),Is.EqualTo(g.NodeCell[nav.Node(i)]));
            Assert.That(e.Y[i],Is.EqualTo((float)g.SurfaceY(nav.Node(i),e.X[i],e.Z[i])));
        }

        [TestCase(10,.25,true,false)] [TestCase(-10,.25,true,false)]
        [TestCase(.25,10,false,true)] [TestCase(.25,-10,false,true)]
        [TestCase(10,10,true,true)] [TestCase(-10,-10,true,true)]
        [TestCase(10,-10,true,true)] [TestCase(-10,10,true,true)]
        public void RoundedConnectedBordersAndCornersKeepNodeAndPositionCoherent(double x,double z,bool crossX,bool crossZ)
        {
            var terrain=new Heightfield(80,80,new float[81*81]);
            var queries=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg);
            var g=new NavGraph(queries,terrain,(px,pz,r)=>Math.Abs(px)+r<30&&Math.Abs(pz)+r<30,NavClass.All[2]);
            var run=new WorldRun(circuit.Data(),"P0QA",Catalog.Characters[0]){Automatic=false};
            var e=run.Combat.Enemies;var nav=new EnemyNavigation(circuit,e.Capacity,new[]{g,g,g});e.Navigation=nav;
            int i=run.Combat.Spawn(5,x-(crossX?.000004:0),z-(crossZ?.000004:0));e.Y[i]=0;
            nav.Begin(e,run.Combat.Player,1.0/60);nav.Move(i,0,0,0,0,e.Speed[i],false,false,1.0/60);
            int before=nav.Node(i);Assert.That(before,Is.GreaterThanOrEqualTo(0));
            double candidateX=x-(crossX?.0000001:0),candidateZ=z-(crossZ?.0000001:0);
            Assert.That(g.CellOf(candidateX,candidateZ),Is.EqualTo(g.NodeCell[before]));
            Assert.That(g.CellOf((float)candidateX,(float)candidateZ),Is.Not.EqualTo(g.NodeCell[before]));
            Assert.That(nav.TryPlace(i,candidateX,candidateZ),Is.True);
            Assert.That(e.X[i],Is.EqualTo((float)candidateX));Assert.That(e.Z[i],Is.EqualTo((float)candidateZ));
            Assert.That(g.NodeCell[nav.Node(i)],Is.EqualTo(g.CellOf(e.X[i],e.Z[i])));
            Assert.That(e.Y[i],Is.EqualTo((float)g.SurfaceY(nav.Node(i),e.X[i],e.Z[i])));
            bool connected=false;for(int a=g.EdgeStart[before];a<g.EdgeStart[before+1];a++)
                if(g.EdgeTo[a]==nav.Node(i)&&g.EdgeKind[a]==(byte)NavEdgeKind.Walk)connected=true;
            Assert.That(connected,Is.True,"Solo se acepta una conexión física existente");
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
