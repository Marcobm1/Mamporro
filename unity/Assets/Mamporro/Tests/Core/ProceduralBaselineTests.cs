using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C0: diagnóstico medido sin cambiar nada. Mapas P0-B sobre recortes de 128 m del terreno U3
    // (solo terreno: las estructuras U3 son cajas orientadas y círculos, fuera de las consultas
    // AABB de escalada) y coste de construcción frente al número de sólidos.
    public sealed class ProceduralBaselineTests
    {
        static Heightfield Crop(Heightfield hf,double size)
        {
            int cells=(int)Math.Floor(size/hf.CellSize),stride=cells+1,i0=(hf.Cells-cells)/2,j0=(hf.Cells-cells)/2;
            var heights=new float[stride*stride];
            for(int j=0;j<stride;j++)for(int i=0;i<stride;i++)heights[j*stride+i]=(float)hf.VertexHeight(i0+i,j0+j);
            return new Heightfield(cells*hf.CellSize,cells,heights);
        }
        static bool[] Reach(NavGraph g,int start)
        {
            var seen=new bool[g.NodeCount];var open=new Queue<int>();seen[start]=true;open.Enqueue(start);
            while(open.Count>0){int a=open.Dequeue();for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){int b=g.EdgeTo[e];if(!seen[b]){seen[b]=true;open.Enqueue(b);}}}
            return seen;
        }
        [TestCase("P0QA")] [TestCase("MAMPORRO")] [TestCase("COLINAS7")] [TestCase("Z9K2")]
        public void U3TerrainCropNavigationCostAndConnectivity(string seed)
        {
            var world=Heightfield.Generate(new Rng(seed).Derive("terrain"));var hf=Crop(world,128);
            var q=new VerticalQueries(hf,Tuning.PlayerMaxSlopeDeg);double limit=hf.Half-2;
            Func<double,double,double,bool> inside=(x,z,m)=>WorldMath.Squircle(x,z)<=limit-m;
            int steep=0;for(int j=0;j<hf.Cells;j++)for(int i=0;i<hf.Cells;i++){var n=hf.NormalAt(-hf.Half+(i+.5)*hf.CellSize,-hf.Half+(j+.5)*hf.CellSize);if(n.Y<Math.Cos(Tuning.PlayerMaxSlopeDeg*WorldMath.Deg2Rad))steep++;}
            double total=0;var report=new List<string>();
            for(int k=0;k<3;k++){
                var g=new NavGraph(q,hf,inside,NavClass.All[k]);total+=g.BuildMs;
                int start=g.Nearest(0,hf.HeightAt(0,0),0,8,2,2);var reach=start>=0?Reach(g,start):new bool[g.NodeCount];int r=0;foreach(bool b in reach)if(b)r++;
                report.Add($"clase {k}: {g.NodeCount} nodos, {g.WalkEdges} caminar, {g.ClimbEdges} escalar, {g.DropEdges} caer, {g.BuildMs:F0} ms, alcanzables desde el inicio {100.0*r/Math.Max(1,g.NodeCount):F1} %");
            }
            TestContext.Out.WriteLine($"semilla {seed}: recorte {hf.Size:F1} m ({hf.Cells} celdas de {hf.CellSize} m), celdas abruptas {100.0*steep/(hf.Cells*hf.Cells):F1} %, mapas {total:F0} ms");
            foreach(var line in report)TestContext.Out.WriteLine("  "+line);
            Assert.That(total,Is.GreaterThan(0));
        }
        // Coste de construcción con el terreno del circuito B y N cajas de 2×3×2 m repartidas.
        [TestCase(0)] [TestCase(100)] [TestCase(300)]
        public void NavGraphBuildCostGrowsWithSolidCount(int extra)
        {
            var b=new VerticalCircuit(VerticalLayout.Persecucion);var solids=new List<VerticalSolid>();
            for(int i=0;i<b.Queries.Count;i++)solids.Add(b.Queries.Solid(i));
            var rng=new Rng("p0c-c0-cajas");
            for(int i=0;i<extra;i++){double x=rng.Range(-34,34),z=rng.Range(-34,34);solids.Add(new VerticalSolid(1000+i,new Vec3(x,0,z),new Vec3(x+2,3,z+2)));}
            var q=new VerticalQueries(b.Terrain,Tuning.PlayerMaxSlopeDeg,solids.ToArray());
            var watch=Stopwatch.StartNew();var g=new NavGraph(q,b.Terrain,b.Collision.IsInside,NavClass.All[0]);
            TestContext.Out.WriteLine($"{solids.Count} sólidos: clase S en {watch.Elapsed.TotalMilliseconds:F0} ms, {g.NodeCount} nodos, {g.ClimbEdges} escalar");
            Assert.That(g.NodeCount,Is.GreaterThan(0));
        }
    }
}
