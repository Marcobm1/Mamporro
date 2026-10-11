using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-B2: campo compartido sobre el grafo del circuito B (clase S salvo indicación).
    public sealed class NavFieldTests
    {
        static VerticalCircuit circuit;static NavGraph[] graphs;
        static NavGraph G(int k=0)
        {
            if(graphs==null){
                circuit=new VerticalCircuit(VerticalLayout.Persecucion);graphs=new NavGraph[NavClass.All.Length];
                for(int i=0;i<graphs.Length;i++)graphs[i]=new NavGraph(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside,NavClass.All[i]);
            }
            return graphs[k];
        }
        static int Node(NavGraph g,double x,double y,double z)=>g.Nearest(x,y,z,3,.2,.2);
        static NavField Field(NavGraph g,double x,double y,double z){var f=new NavField(g);f.Complete(Node(g,x,y,z),x,y,z);return f;}
        // Sigue las aristas del campo desde start; devuelve los tipos usados y el nodo final.
        static List<NavEdgeKind> Follow(NavField f,int start,out int end)
        {
            var kinds=new List<NavEdgeKind>();int n=start;
            for(int i=0;i<10000;i++){int e=f.Parent(n);if(e<0)break;kinds.Add((NavEdgeKind)f.Graph.EdgeKind[e]);
                Assert.That(f.Dist(f.Graph.EdgeTo[e])+f.Graph.EdgeCost[e],Is.EqualTo(f.Dist(n)),"coste coherente a lo largo de la ruta");n=f.Graph.EdgeTo[e];}
            end=n;return kinds;
        }

        [Test] public void SameLevelRouteIsPureWalking()
        {
            var g=G();var f=Field(g,0,0,-4);var kinds=Follow(f,Node(g,-12,0,4),out int end);
            Assert.That(end,Is.EqualTo(f.Goal));Assert.That(kinds,Has.All.EqualTo(NavEdgeKind.Walk));
        }
        [Test] public void RampRoofUsesTheRampFromTheWestAndAWallFromTheEast()
        {
            var g=G();var f=Field(g,11,4,-11);
            var west=Follow(f,Node(g,-4,0,-11),out int a);Assert.That(a,Is.EqualTo(f.Goal));Assert.That(west,Has.All.EqualTo(NavEdgeKind.Walk),"rampa");
            var east=Follow(f,Node(g,17,0,-11),out int b);Assert.That(b,Is.EqualTo(f.Goal));Assert.That(east,Has.Member(NavEdgeKind.Climb),"pared este, más corta que rodear");
        }
        [Test] public void ClimbOnlyRoofPlateauAndCliffAreReachedByClimbing()
        {
            var g=G();
            foreach(var (x,y,z) in new[]{(-11.0,5.0,-11.0),(16.0,5.0,10.0),(24.0,4.0,-12.0)}){
                var f=Field(g,x,y,z);var kinds=Follow(f,Node(g,0,0,-4),out int end);
                Assert.That(end,Is.EqualTo(f.Goal),$"meta ({x},{y},{z})");Assert.That(kinds.FindAll(k=>k==NavEdgeKind.Climb).Count,Is.EqualTo(1),$"una escalada ({x},{y},{z})");
            }
        }
        [Test] public void DescendingFromARoofUsesADrop()
        {
            var g=G();var f=Field(g,0,0,-4);var kinds=Follow(f,Node(g,-11,5,-11),out int end);
            Assert.That(end,Is.EqualTo(f.Goal));Assert.That(kinds,Has.Member(NavEdgeKind.Drop));Assert.That(kinds,Has.No.Member(NavEdgeKind.Climb));
        }
        [Test] public void TwoWallsAreChosenByTheShorterRoute()
        {
            // Desde el norte del edificio C se sube por la cara norte; desde el sur, por la sur.
            var g=G();var f=Field(g,-11,5,-11);
            Follow(f,Node(g,-11,0,-3),out _);int n=Node(g,-11,0,-3);int climbFromNorth=FirstClimb(f,n);
            int s=Node(g,-11,0,-19);int climbFromSouth=FirstClimb(f,s);
            Assert.That(g.PathNZ[g.EdgePath[climbFromNorth]],Is.EqualTo(1).Within(1e-3));Assert.That(g.PathNZ[g.EdgePath[climbFromSouth]],Is.EqualTo(-1).Within(1e-3));
        }
        static int FirstClimb(NavField f,int n){for(int i=0;i<10000;i++){int e=f.Parent(n);if(e<0)break;if(f.Graph.EdgeKind[e]==(byte)NavEdgeKind.Climb)return e;n=f.Graph.EdgeTo[e];}Assert.Fail("sin escalada");return -1;}
        [Test] public void InaccessibleCapSendsEveryoneToTheNearestReachablePoint()
        {
            var g=G();var f=new NavField(g);int cap=g.Nearest(20,5.5,20,2,.1,.1);f.Complete(cap,20,5.5,20);
            int start=Node(g,0,0,-4);Assert.That(f.Reachable(start),Is.False);
            Follow(f,start,out int end);
            double dx=g.NodeX(end)-20,dz=g.NodeZ(end)-20;
            TestContext.Out.WriteLine($"seta: destino ({g.NodeX(end)},{g.NodeY[end]},{g.NodeZ(end)}), a {Math.Sqrt(dx*dx+dz*dz):F2} m en planta");
            Assert.That(g.NodeY[end],Is.LessThan(5),"no está sobre la seta");Assert.That(Math.Sqrt(dx*dx+dz*dz),Is.LessThan(4.5),"junto a la seta");
            Assert.That(f.Parent(end),Is.EqualTo(-1),"se detiene: no empuja");
        }
        [Test] public void GoalChangeIsPublishedWithinAQuarterSecondUnderTheTickBudget()
        {
            var g=G();var f=new NavField(g);int ground=Node(g,0,0,-4),roof=Node(g,11,4,-11);
            double t=0;while(f.Version==0){f.Update(ground,0,0,-4,1.0/60,EnemyNavigationBudget);t+=1.0/60;}
            // Cambio inmediato a tejado y vuelta: cada publicación llega en ≤ 0,25 s.
            foreach(var (goal,x,y,z) in new[]{(roof,11.0,4.0,-11.0),(ground,0.0,0.0,-4.0),(roof,11.0,4.0,-11.0)}){
                int v=f.Version;double elapsed=0;
                while(f.Version==v||f.Goal!=goal){f.Update(goal,x,y,z,1.0/60,EnemyNavigationBudget);elapsed+=1.0/60;Assert.That(elapsed,Is.LessThan(1));}
                TestContext.Out.WriteLine($"meta publicada en {elapsed:F3} s; último tramo {f.LastTicks*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} ms");
                Assert.That(elapsed,Is.LessThanOrEqualTo(.25+1e-9));
            }
        }
        const int EnemyNavigationBudget=3000;
        [Test] public void FieldIsDeterministicAndAllocationFree()
        {
            var g=G();var a=new NavField(g);var b=new NavField(g);int goal=Node(g,11,4,-11);
            a.Complete(goal,11,4,-11);
            long before=GC.GetAllocatedBytesForCurrentThread();
            b.Complete(goal,11,4,-11);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            for(int n=0;n<g.NodeCount;n++){Assert.That(b.Dist(n),Is.EqualTo(a.Dist(n)));Assert.That(b.Parent(n),Is.EqualTo(a.Parent(n)));}
            TestContext.Out.WriteLine($"asignado durante un cálculo completo: {allocated} B");
            Assert.That(allocated,Is.LessThan(512));
        }
        [Test] public void FullComputeCostIsReportedForEveryClass()
        {
            for(int k=0;k<3;k++){var g=G(k);var f=new NavField(g);int goal=Node(g,0,4,16);
                f.Complete(goal,0,4,16);double ms=f.TotalTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
                TestContext.Out.WriteLine($"clase {k}: cálculo completo {ms:F3} ms, {f.Pops} extracciones, {f.Relaxations} relajaciones");}
        }
    }
}
