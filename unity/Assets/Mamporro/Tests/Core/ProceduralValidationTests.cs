using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C4: validación con la navegación P0-B real (S/M/L), reparación determinista y rechazo medido.
    public sealed class ProceduralValidationTests
    {
        static readonly string[] Seeds={"P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"};

        [TestCaseSource(nameof(Seeds))]
        public void AcceptedMapsHaveNoRefugesAndReachableMarkers(string seed)
        {
            var map=new ProceduralMap(seed);var w=map.World;
            foreach(var line in map.Log)TestContext.Out.WriteLine(line);
            foreach(var note in map.Check.Notes)TestContext.Out.WriteLine("  "+note);
            var g=map.Graphs;
            TestContext.Out.WriteLine($"{seed}: intento {map.Layout.Attempt}, previa {map.PreCheckMs:F0} ms (quita {map.PreRemoved}, recoloca {map.PreRelocated}), reparaciones {map.Repairs} (estructuras quitadas {map.RemovedStructures}, marcadores recolocados {map.RelocatedMarkers}), rechazos {map.Rejections}, construcciones {map.Builds}; "+
                $"nodos S/M/L {g[0].NodeCount}/{g[1].NodeCount}/{g[2].NodeCount}, escalar {g[0].ClimbEdges}/{g[1].ClimbEdges}/{g[2].ClimbEdges}; "+
                $"generación {map.GenerationMs:F0} ms, mapas {map.MapsMs:F0} ms, validación {map.ValidationMs:F0} ms, total {map.TotalMs:F0} ms; "+
                $"cofres {w.Count(StructureKind.Chest)}, santuarios {w.Count(StructureKind.Shrine)}, tótems {w.Count(StructureKind.Totem)}, portal {w.Count(StructureKind.Portal)} (sin sitio {w.MissingMarkers})");
            Assert.That(map.Check.Passed,Is.True,map.Check.Summary());
            Assert.That(w.MissingMarkers,Is.Zero,"todos los marcadores colocados");
            foreach(var p in Catalog.InteractablePlacements){
                var kind=p.kind=="chest"?StructureKind.Chest:p.kind=="shrine"?StructureKind.Shrine:p.kind=="totem"?StructureKind.Totem:StructureKind.Portal;
                Assert.That(w.Count(kind),Is.EqualTo(p.count),$"{p.kind}: cantidad del catálogo");
            }
        }
        // Regresión: tapas a una altura no exacta en float (0,9 m) también tienen caídas (antes, bolsa).
        [Test] public void TopsAtFloatInexactHeightsHaveDropEdges()
        {
            const int cells=40;var terrain=new Heightfield(20,cells,new float[(cells+1)*(cells+1)]);
            foreach(double height in new[]{.9,1.5,9.9-9}){
                var q=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,new VerticalSolid(1,new Vec3(-1.2,0,-1.2),new Vec3(1.2,height,1.2)));
                var g=new NavGraph(q,terrain,(x,z,m)=>Math.Abs(x)<=10-m&&Math.Abs(z)<=10-m,NavClass.All[0]);
                int start=g.Nearest(-7,0,-7,4,1,1);var to=ProceduralMap.Reach(g,start);var back=ProceduralMap.Reach(g,start,true);
                int top=0;for(int n=0;n<g.NodeCount;n++){if(g.NodeY[n]<.3)continue;top++;Assert.That(to[n]&&back[n],Is.True,$"tapa a {height} m: nodo con entrada y salida");}
                Assert.That(top,Is.GreaterThan(0),$"tapa a {height} m con nodos");
            }
        }
        [Test] public void SameSeedSameValidatedMap()
        {
            var a=new ProceduralMap("Z9K2");var b=new ProceduralMap("Z9K2");
            Assert.That(b.Layout.Attempt,Is.EqualTo(a.Layout.Attempt));Assert.That(b.Repairs,Is.EqualTo(a.Repairs));
            var sa=new List<VerticalSolid>(a.World.Solids());var sb=new List<VerticalSolid>(b.World.Solids());
            Assert.That(sb.Count,Is.EqualTo(sa.Count));for(int i=0;i<sa.Count;i++)Assert.That(sb[i].Min.X,Is.EqualTo(sa[i].Min.X));
            for(int k=0;k<3;k++){Assert.That(b.Graphs[k].NodeY,Is.EqualTo(a.Graphs[k].NodeY));Assert.That(b.Graphs[k].EdgeTo,Is.EqualTo(a.Graphs[k].EdgeTo));}
        }
    }
}
