using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C3: estructuras de bloques alineadas a la rejilla. Reglas geométricas sin navegación;
    // la jugabilidad completa se comprueba en C4 con los mapas P0-B.
    public sealed class ProceduralStructuresTests
    {
        static readonly string[] Seeds={"P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"};
        static bool Overlap(VerticalSolid a,VerticalSolid b,double e=1e-6)=>a.Min.X<b.Max.X-e&&b.Min.X<a.Max.X-e&&a.Min.Y<b.Max.Y-e&&b.Min.Y<a.Max.Y-e&&a.Min.Z<b.Max.Z-e&&b.Min.Z<a.Max.Z-e;

        [Test] public void SameSeedSameStructures()
        {
            var a=new List<VerticalSolid>(new ProceduralWorld(new ProceduralLayout("P0QA")).Solids());var b=new List<VerticalSolid>(new ProceduralWorld(new ProceduralLayout("P0QA")).Solids());
            Assert.That(b.Count,Is.EqualTo(a.Count));
            for(int i=0;i<a.Count;i++){Assert.That(b[i].Min.X,Is.EqualTo(a[i].Min.X));Assert.That(b[i].Max.Y,Is.EqualTo(a[i].Max.Y));Assert.That(b[i].Kind,Is.EqualTo(a[i].Kind));}
        }
        [TestCaseSource(nameof(Seeds))]
        public void StructuresAreGroundedSeparatedAndFreeOfAmbiguousTops(string seed)
        {
            var layout=new ProceduralLayout(seed);var world=new ProceduralWorld(layout);var solids=new List<VerticalSolid>(world.Solids());var t=layout.Terrain;
            for(int i=0;i<solids.Count;i++)for(int j=i+1;j<solids.Count;j++)Assert.That(Overlap(solids[i],solids[j]),Is.False,$"{seed}: sólidos {solids[i].Id} y {solids[j].Id} se solapan");
            foreach(var s in solids){
                // Apoyo: en el terreno llano o sobre otro sólido (nada flotante ni hundido).
                double ground=t.HeightAt((s.Min.X+s.Max.X)/2,(s.Min.Z+s.Max.Z)/2);
                foreach(var (x,z) in new[]{(s.Min.X+.01,s.Min.Z+.01),(s.Max.X-.01,s.Min.Z+.01),(s.Min.X+.01,s.Max.Z-.01),(s.Max.X-.01,s.Max.Z-.01)})
                    Assert.That(t.HeightAt(x,z),Is.LessThanOrEqualTo(s.Min.Y+1e-6),$"{seed}: sólido {s.Id} hundido en el terreno");
                bool grounded=Math.Abs(ground-s.Min.Y)<1e-6;
                if(!grounded)foreach(var o in solids)if(o!=s&&Math.Abs(o.Max.Y-s.Min.Y)<1e-6&&o.Min.X<s.Max.X&&s.Min.X<o.Max.X&&o.Min.Z<s.Max.Z&&s.Min.Z<o.Max.Z){grounded=true;break;}
                Assert.That(grounded,Is.True,$"{seed}: sólido {s.Id} flotante a {s.Min.Y} sobre terreno {ground}");
                Assert.That(JsMath.Hypot((s.Min.X+s.Max.X)/2,(s.Min.Z+s.Max.Z)/2),Is.GreaterThan(layout.Settings.SpawnRadius),"zona de inicio despejada");
                // Tapas escalables descubiertas: o nadie se sostiene (≤ 0,7 m) o caben los pequeños (≥ 1 m).
                if(!s.Climbable)continue;bool covered=false;
                foreach(var o in solids)if(o!=s&&Math.Abs(o.Min.Y-s.Max.Y)<1e-6&&o.Min.X<=s.Min.X+1e-6&&o.Max.X>=s.Max.X-1e-6&&o.Min.Z<=s.Min.Z+1e-6&&o.Max.Z>=s.Max.Z-1e-6){covered=true;break;}
                double narrow=Math.Min(s.Max.X-s.Min.X,s.Max.Z-s.Min.Z);
                if(!covered)Assert.That(narrow<=.7+1e-9||narrow>=1-1e-9,Is.True,$"{seed}: tapa ambigua de {narrow:F2} m en el sólido {s.Id}");
            }
            double total=layout.Milliseconds+world.Milliseconds;
            TestContext.Out.WriteLine($"{seed}: casas {world.Count(StructureKind.House)}, templos {world.Count(StructureKind.Temple)}, granjas {world.Count(StructureKind.Farm)}, pozos {world.Count(StructureKind.Well)}, torres {world.Count(StructureKind.Tower)}, muros {world.Count(StructureKind.Wall)}, escalonados {world.Count(StructureKind.Stack)}, árboles {world.Count(StructureKind.Tree)}; {solids.Count} sólidos; trazado+estructuras {total:F1} ms");
            Assert.That(world.Count(StructureKind.House)+world.Count(StructureKind.Temple)+world.Count(StructureKind.Farm)+world.Count(StructureKind.Well),Is.GreaterThanOrEqualTo(5),"prototipos reconocibles presentes");
            Assert.That(total,Is.LessThan(300));
        }
    }
}
