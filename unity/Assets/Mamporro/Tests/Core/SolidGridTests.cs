using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C1: la rejilla espacial de VerticalQueries devuelve exactamente lo mismo que el recorrido
    // lineal (resultado, sólido, fracción, normal y desempates) y reduce el coste con muchos sólidos.
    public sealed class SolidGridTests
    {
        static VerticalQueries Queries(int extra,out VerticalCircuit circuit)
        {
            circuit=new VerticalCircuit(VerticalLayout.Persecucion);var solids=new List<VerticalSolid>();
            for(int i=0;i<circuit.Queries.Count;i++)solids.Add(circuit.Queries.Solid(i));
            var rng=new Rng("p0c-c1-cajas");
            for(int i=0;i<extra;i++){
                double x=rng.Range(-36,34),z=rng.Range(-36,34),w=rng.Range(.4,6),d=rng.Range(.4,6),y=rng.Chance(.2)?rng.Range(0,5):0,h=rng.Range(.3,7);
                solids.Add(new VerticalSolid(1000+i,(VerticalSurfaceKind)(i%5),new Vec3(x,y,z),new Vec3(x+w,y+h,z+d)));
            }
            return new VerticalQueries(circuit.Terrain,Tuning.PlayerMaxSlopeDeg,solids.ToArray());
        }
        [TestCase(0)] [TestCase(300)]
        public void GridQueriesMatchTheLinearScanExactly(int extra)
        {
            var q=Queries(extra,out _);var rng=new Rng("p0c-c1-consultas-"+extra);int checks=0;
            for(int t=0;t<6000;t++){
                var a=new Vec3(rng.Range(-38,38),rng.Range(-1,8),rng.Range(-38,38));
                var b=new Vec3(a.X+rng.Range(-3,3),a.Y+rng.Range(-2,2),a.Z+rng.Range(-3,3));
                double r=rng.Range(0,2.6),h=rng.Range(.2,4.5),step=rng.Range(0,.7);
                var dir=new Vec3(rng.Range(-1,1),0,rng.Range(-1,1));
                q.UseSpatialGrid=false;
                bool clear=q.Clear(a,r,h),above=q.ClearAboveStep(a,r,h,step),sweep=q.Sweep(a,b,r,h,out var hit),support=q.Support(a,r,.05,out int id);
                bool wall=q.Wall(a,dir,r,h,.6,out var w);Vec3 lift=default,top=default;bool ledge=wall&&q.Ledge(a,w,r,h,out lift,out top);
                q.UseSpatialGrid=true;
                Assert.That(q.Clear(a,r,h),Is.EqualTo(clear),$"Clear {t}");
                Assert.That(q.ClearAboveStep(a,r,h,step),Is.EqualTo(above),$"ClearAboveStep {t}");
                Assert.That(q.Sweep(a,b,r,h,out var hit2),Is.EqualTo(sweep),$"Sweep {t}");
                Assert.That(hit2.Id,Is.EqualTo(hit.Id));Assert.That(hit2.Terrain,Is.EqualTo(hit.Terrain));Assert.That(hit2.Fraction,Is.EqualTo(hit.Fraction));
                Assert.That(hit2.Normal.X,Is.EqualTo(hit.Normal.X));Assert.That(hit2.Normal.Y,Is.EqualTo(hit.Normal.Y));Assert.That(hit2.Normal.Z,Is.EqualTo(hit.Normal.Z));
                Assert.That(q.Support(a,r,.05,out int id2),Is.EqualTo(support),$"Support {t}");Assert.That(id2,Is.EqualTo(id));
                Assert.That(q.Wall(a,dir,r,h,.6,out var w2),Is.EqualTo(wall),$"Wall {t}");Assert.That(w2.Id,Is.EqualTo(w.Id));
                if(wall){Assert.That(q.Ledge(a,w2,r,h,out var lift2,out var top2),Is.EqualTo(ledge),$"Ledge {t}");if(ledge){Assert.That(lift2.Y,Is.EqualTo(lift.Y));Assert.That(top2.X,Is.EqualTo(top.X));Assert.That(top2.Z,Is.EqualTo(top.Z));}}
                checks++;
            }
            TestContext.Out.WriteLine($"{q.Count} sólidos: {checks} consultas idénticas en las seis operaciones");
        }
        [TestCase(0)] [TestCase(300)]
        public void GridMakesNavGraphBuildIdenticalAndCheaper(int extra)
        {
            var q=Queries(extra,out var c);
            q.UseSpatialGrid=false;var watch=Stopwatch.StartNew();var linear=new NavGraph(q,c.Terrain,c.Collision.IsInside,NavClass.All[0]);double linearMs=watch.Elapsed.TotalMilliseconds;
            q.UseSpatialGrid=true;watch.Restart();var grid=new NavGraph(q,c.Terrain,c.Collision.IsInside,NavClass.All[0]);double gridMs=watch.Elapsed.TotalMilliseconds;
            TestContext.Out.WriteLine($"{q.Count} sólidos, clase S: lineal {linearMs:F0} ms, rejilla {gridMs:F0} ms; {grid.NodeCount} nodos, {grid.ClimbEdges} escalar");
            Assert.That(grid.NodeY,Is.EqualTo(linear.NodeY));Assert.That(grid.NodeCell,Is.EqualTo(linear.NodeCell));
            Assert.That(grid.EdgeTo,Is.EqualTo(linear.EdgeTo));Assert.That(grid.EdgeCost,Is.EqualTo(linear.EdgeCost));Assert.That(grid.PathX,Is.EqualTo(linear.PathX));
            if(extra>0)Assert.That(gridMs,Is.LessThan(linearMs),"la rejilla abarata la construcción con muchos sólidos");
        }
        // P0-C4: la tabla de celdas abruptas del terreno devuelve exactamente lo mismo que el recorrido
        // de celdas (consultas aleatorias en el circuito B y en un mapa procedural) y los mapas S/M/L
        // salen idénticos.
        [TestCase("B")] [TestCase("P0QA")]
        public void SteepTableMatchesTheFullTerrainScan(string source)
        {
            Heightfield terrain=source=="B"?new VerticalCircuit(VerticalLayout.Persecucion).Terrain:new ProceduralLayout(source).Terrain;
            var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg);var rng=new Rng("p0c-c4-terreno-"+source);double half=terrain.Half-3;
            for(int t=0;t<6000;t++){
                var a=new Vec3(rng.Range(-half,half),0,rng.Range(-half,half));a.Y=terrain.HeightAt(a.X,a.Z)+rng.Range(-.5,1.5);
                var b=new Vec3(a.X+rng.Range(-3,3),a.Y+rng.Range(-2,2),a.Z+rng.Range(-3,3));
                double r=rng.Range(0,2.7),h=rng.Range(.2,4.5),step=rng.Range(0,.7);var dir=new Vec3(rng.Range(-1,1),0,rng.Range(-1,1));
                q.UseSteepTable=false;
                bool clear=q.Clear(a,r,h),above=q.ClearAboveStep(a,r,h,step),sweep=q.Sweep(a,b,r,h,out var hit),foot=q.TerrainFootprintWalkable(a,r),wall=q.Wall(a,dir,r,h,.6,out var w);
                q.UseSteepTable=true;
                Assert.That(q.Clear(a,r,h),Is.EqualTo(clear),$"Clear {t}");
                Assert.That(q.ClearAboveStep(a,r,h,step),Is.EqualTo(above),$"ClearAboveStep {t}");
                Assert.That(q.Sweep(a,b,r,h,out var hit2),Is.EqualTo(sweep),$"Sweep {t}");
                Assert.That(hit2.Id,Is.EqualTo(hit.Id));Assert.That(hit2.Fraction,Is.EqualTo(hit.Fraction));
                Assert.That(q.TerrainFootprintWalkable(a,r),Is.EqualTo(foot),$"Huella {t}");
                Assert.That(q.Wall(a,dir,r,h,.6,out var w2),Is.EqualTo(wall),$"Wall {t}");Assert.That(w2.Id,Is.EqualTo(w.Id));
            }
            if(source=="B")return;
            var l=new ProceduralLayout(source);Func<double,double,double,bool> inside=(x,z,m)=>WorldMath.Squircle(x,z)<=l.Settings.Limit-m;
            var slow=new VerticalQueries(l.Terrain,Tuning.PlayerMaxSlopeDeg){UseSteepTable=false};var fast=new VerticalQueries(l.Terrain,Tuning.PlayerMaxSlopeDeg);
            for(int k=0;k<3;k++){
                var gs=new NavGraph(slow,l.Terrain,inside,NavClass.All[k]);var gf=new NavGraph(fast,l.Terrain,inside,NavClass.All[k]);
                Assert.That(gf.NodeY,Is.EqualTo(gs.NodeY));Assert.That(gf.EdgeTo,Is.EqualTo(gs.EdgeTo));Assert.That(gf.EdgeCost,Is.EqualTo(gs.EdgeCost));Assert.That(gf.PathX,Is.EqualTo(gs.PathX));
                TestContext.Out.WriteLine($"clase {k}: sin tabla {gs.BuildMs:F0} ms (nodos {gs.NodesMs:F0}), con tabla {gf.BuildMs:F0} ms (nodos {gf.NodesMs:F0})");
            }
        }
    }
}
