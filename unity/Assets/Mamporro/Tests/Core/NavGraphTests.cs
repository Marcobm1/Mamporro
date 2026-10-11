using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B1: grafo multinivel del circuito B por clase de cuerpo. Las expectativas se deducen
    // de la geometría; no hay listas de IDs ni en el grafo ni en las pruebas.
    public sealed class NavGraphTests
    {
        static VerticalCircuit circuit;static NavGraph[] graphs;
        static NavGraph G(int k)
        {
            if(graphs==null){
                circuit=new VerticalCircuit(VerticalLayout.Persecucion);graphs=new NavGraph[NavClass.All.Length];
                for(int i=0;i<graphs.Length;i++)graphs[i]=new NavGraph(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[i]);
            }
            return graphs[k];
        }
        static int Node(NavGraph g,double x,double y,double z){int n=g.NodeAt(x,y,z,.05);return n>=0&&Math.Abs(g.NodeY[n]-y)<.05?n:-1;}
        // Nodos alcanzables desde start siguiendo aristas de los tipos permitidos.
        static bool[] Reach(NavGraph g,int start,params NavEdgeKind[] kinds)
        {
            var seen=new bool[g.NodeCount];var open=new Queue<int>();seen[start]=true;open.Enqueue(start);
            while(open.Count>0){int a=open.Dequeue();
                for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){int b=g.EdgeTo[e];if(seen[b]||Array.IndexOf(kinds,(NavEdgeKind)g.EdgeKind[e])<0)continue;seen[b]=true;open.Enqueue(b);}}
            return seen;
        }
        static readonly NavEdgeKind[] All={NavEdgeKind.Walk,NavEdgeKind.Climb,NavEdgeKind.Drop};

        [Test] public void BuildCostIsReportedForEveryClass()
        {
            for(int k=0;k<NavClass.All.Length;k++){var g=G(k);
                TestContext.Out.WriteLine($"clase {k} (r={g.Class.Radius}, h={g.Class.Height}): {g.NodeCount} nodos, {g.WalkEdges} caminar, {g.ClimbEdges} escalar, {g.DropEdges} caer, {g.BuildMs:F1} ms; escaladas descartadas: sin contacto {g.ClimbFailNoContact}, sin borde {g.ClimbFailNoLedge}, sin nodo arriba {g.ClimbFailNoTarget}, trayectoria con contacto {g.ClimbFailPenetration}/{g.DropFailPenetration}");
                Assert.That(g.NodeCount,Is.GreaterThan(1000));}
            Assert.That(G(0).ClimbEdges,Is.GreaterThan(50));Assert.That(G(0).DropEdges,Is.GreaterThan(50));
        }
        [Test] public void SurfacesDependOnBodySize()
        {
            // Suelo libre, tejado amplio, torre estrecha de 4 m y pasillo de 3 m.
            for(int k=0;k<3;k++){Assert.That(Node(G(k),0,0,-4),Is.GreaterThanOrEqualTo(0),$"suelo clase {k}");Assert.That(Node(G(k),0,4,16),Is.GreaterThanOrEqualTo(0),$"tejado amplio clase {k}");}
            Assert.That(Node(G(0),-21,4,10),Is.GreaterThanOrEqualTo(0));Assert.That(Node(G(1),-21,4,10),Is.GreaterThanOrEqualTo(0));
            Assert.That(Node(G(2),-21,4,10),Is.LessThan(0),"el jefe no cabe en la torre");
            Assert.That(Node(G(0),18,0,-1),Is.GreaterThanOrEqualTo(0));Assert.That(Node(G(1),18,0,-1),Is.GreaterThanOrEqualTo(0));
            Assert.That(Node(G(2),18,0,-1),Is.LessThan(0),"el jefe no cabe en el pasillo");
        }
        [Test] public void NoNodeInsideSolidsOrOnExcludedSurfaces()
        {
            var g=G(0);
            Assert.That(Node(g,11,0,-11),Is.LessThan(0),"dentro del edificio");
            Assert.That(Node(g,-1.5,3,4.5),Is.LessThan(0),"tapa de follaje");Assert.That(Node(g,2.5,2,4.5),Is.LessThan(0),"tapa de interactuable");
            Assert.That(Node(g,-20,3,-12),Is.LessThan(0),"pared con techo: no cabe el cuerpo encima");
            for(int n=0;n<g.NodeCount;n++)Assert.That(circuit.Queries.ClearAboveStep(new Vec3(g.NodeX(n),g.NodeY[n],g.NodeZ(n)),g.Class.Radius,g.Class.Height,NavGraph.Step),Is.True);
        }
        [Test] public void RampRoofIsReachedByWalkingAndClimbOnlyRoofOnlyByClimbing()
        {
            var g=G(0);int ground=Node(g,0,0,-4);
            var walk=Reach(g,ground,NavEdgeKind.Walk);var any=Reach(g,ground,All);
            Assert.That(walk[Node(g,11,4,-11)],Is.True,"tejado R por la rampa");
            Assert.That(walk[Node(g,-11,5,-11)],Is.False,"tejado C sin rampa");Assert.That(any[Node(g,-11,5,-11)],Is.True,"tejado C escalando");
            Assert.That(walk[Node(g,16,5,10)],Is.False,"meseta sin rampa");Assert.That(any[Node(g,16,5,10)],Is.True,"meseta escalando el terreno");
            Assert.That(any[Node(g,24,4,-12)],Is.True,"repisa del acantilado escalando");
            Assert.That(any[Node(g,0,4,16)],Is.True,"tejado amplio");
            // Desde arriba se baja cayendo y se vuelve al suelo.
            Assert.That(Reach(g,Node(g,-11,5,-11),All)[ground],Is.True);
        }
        [Test] public void CeilingAndOverhangSurfacesStayUnreachable()
        {
            for(int k=0;k<2;k++){var g=G(k);var any=Reach(g,Node(g,0,0,-4),All);
                int plate=Node(g,-20,4,-12),cap=Node(g,20,5.5,20);
                Assert.That(plate,Is.GreaterThanOrEqualTo(0),"la placa existe como superficie");Assert.That(cap,Is.GreaterThanOrEqualTo(0),"la seta existe como superficie");
                Assert.That(any[plate],Is.False,$"placa sobre la pared con techo, clase {k}");
                Assert.That(any[cap],Is.False,$"cima de la seta, clase {k}");}
        }
        [Test] public void BossReachesTheWideRoofButNotTheNarrowTower()
        {
            var g=G(2);var any=Reach(g,Node(g,0,0,-4),All);
            Assert.That(any[Node(g,0,4,16)],Is.True);Assert.That(any[Node(g,-11,5,-11)],Is.True,"tejado C de 6 m: cabe justo");
        }
        [Test] public void EveryClimbAndDropTrajectoryIsFreeOfSolids()
        {
            for(int k=0;k<3;k++){var g=G(k);int checkedPaths=0;
                for(int from=0;from<g.NodeCount;from++)for(int e=g.EdgeStart[from];e<g.EdgeStart[from+1];e++){
                    if(g.EdgeKind[e]==(byte)NavEdgeKind.Walk)continue;int p=g.EdgePath[e];checkedPaths++;
                    for(int i=g.PathStart[p]+1;i<g.PathStart[p+1];i++){
                        var a=new Vec3(g.PathX[i-1],g.PathY[i-1],g.PathZ[i-1]);var b=new Vec3(g.PathX[i],g.PathY[i],g.PathZ[i]);
                        double len=Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y)+(b.Z-a.Z)*(b.Z-a.Z));
                        // Muestreo independiente (0,07 m) del de construcción; el final admite escalones.
                        int steps=Math.Max(1,(int)Math.Ceiling(len/.07));
                        for(int t=0;t<=steps;t++){double f=t/(double)steps;var q=new Vec3(a.X+(b.X-a.X)*f,a.Y+(b.Y-a.Y)*f,a.Z+(b.Z-a.Z)*f);
                            bool last=i==g.PathStart[p+1]-1&&t==steps;
                            bool free=last?circuit.Queries.ClearAboveStep(q,g.Class.Radius,g.Class.Height,NavGraph.Step):circuit.Queries.Clear(q,g.Class.Radius,g.Class.Height);
                            Assert.That(free,Is.True,$"clase {k} arista {e} punto {q.X:F2},{q.Y:F2},{q.Z:F2}");}
                    }
                    // Escalar siempre sube y caer siempre baja; los extremos son nodos del grafo.
                    double dy=g.NodeY[g.EdgeTo[e]]-g.NodeY[from];
                    Assert.That(g.EdgeKind[e]==(byte)NavEdgeKind.Climb?dy>0:dy< -NavGraph.Step,Is.True,$"clase {k} arista {e} tipo {g.EdgeKind[e]} dy={dy:F2}");
                }
                Assert.That(checkedPaths,Is.GreaterThan(0));}
        }
        [Test] public void AnyPointOfATerrainNodeCellIsFreeForTheBodyAtTheRealHeight()
        {
            // Caminar sigue la altura exacta del terreno dentro de la celda: en rampas y pendientes el
            // cuerpo no debe tocar caras abruptas vecinas en ningún punto de la celda, no solo en el centro.
            for(int k=0;k<3;k++){var g=G(k);int failures=0;string first="";
                for(int n=0;n<g.NodeCount;n++){
                    if(!g.NodeTerrain[n])continue;double cx=g.NodeX(n),cz=g.NodeZ(n);
                    for(int a=0;a<5;a++)for(int b=0;b<5;b++){
                        double x=cx-NavGraph.Cell/2+NavGraph.Cell*a/4,z=cz-NavGraph.Cell/2+NavGraph.Cell*b/4;
                        if(circuit.Queries.ClearAboveStep(new Vec3(x,circuit.Terrain.HeightAt(x,z),z),g.Class.Radius,g.Class.Height,NavGraph.Step))continue;
                        if(failures++==0)first=$"nodo {n} punto ({x:F2},{circuit.Terrain.HeightAt(x,z):F3},{z:F2})";
                    }
                }
                Assert.That(failures,Is.Zero,$"clase {k}: {failures} puntos; primero {first}");}
        }
        [Test] public void WalkEdgesNeverExceedTheStepAndAreSymmetric()
        {
            var g=G(0);
            for(int a=0;a<g.NodeCount;a++)for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){
                if(g.EdgeKind[e]!=(byte)NavEdgeKind.Walk)continue;int b=g.EdgeTo[e];
                Assert.That(Math.Abs(g.NodeY[b]-g.NodeY[a]),Is.LessThanOrEqualTo(NavGraph.Step+1e-6));
                bool back=false;for(int f=g.EdgeStart[b];f<g.EdgeStart[b+1];f++)if(g.EdgeTo[f]==a&&g.EdgeKind[f]==(byte)NavEdgeKind.Walk)back=true;
                Assert.That(back,Is.True);
            }
        }
        [Test] public void ClimbDiagnosisAroundBuildingC()
        {
            // Pies de las cuatro caras del edificio C (solo se sube escalando).
            foreach(var (x,z) in new[]{(-7.25,-11.25),(-14.75,-11.25),(-11.25,-7.25),(-11.25,-14.75)})
            for(int k=0;k<3;k++){var g=G(k);int n=g.Nearest(x,0,z,6,.1,.1);
                TestContext.Out.WriteLine($"clase {k} pie ({x},{z}) nodo {n} {(n>=0?$"({g.NodeX(n)},{g.NodeZ(n)})":"")}: {(n>=0?g.DiagnoseClimbs(n):"-")}");}
        }
        [Test] public void BuildIsDeterministic()
        {
            var c=new VerticalCircuit(VerticalLayout.Persecucion);
            var a=new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,NavClass.All[0]);var b=new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,NavClass.All[0]);
            Assert.That(b.NodeY,Is.EqualTo(a.NodeY));Assert.That(b.EdgeTo,Is.EqualTo(a.EdgeTo));Assert.That(b.EdgeCost,Is.EqualTo(a.EdgeCost));Assert.That(b.PathX,Is.EqualTo(a.PathX));
        }
        [Test] public void CatalogMapsToBodyClassesBySize()
        {
            foreach(var d in Catalog.Enemies){var k=NavClass.All[NavClass.Of(d)];
                Assert.That(d.radius,Is.LessThanOrEqualTo(k.Radius+1e-9));Assert.That(d.height,Is.LessThanOrEqualTo(k.Height+1e-9));}
            Assert.That(NavClass.Of(Catalog.Enemies[5]),Is.EqualTo(2));
        }
    }
}
