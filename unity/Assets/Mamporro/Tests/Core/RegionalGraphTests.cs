using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class RegionalGraphTests
    {
        static long Key(NavGraph g,int n)
        {
            int width=(int)Math.Round(g.Terrain.Size/NavGraph.Cell);
            int x=(int)Math.Floor((g.NodeX(n)+g.Terrain.Half)/NavGraph.Cell);
            int z=(int)Math.Floor((g.NodeZ(n)+g.Terrain.Half)/NavGraph.Cell);
            return ((long)(z*width+x)<<32)|(uint)(n-g.CellStart[g.NodeCell[n]]);
        }
        static long Payload(NavGraph g)=>RegionalBaselineTests.ArrayBytes(g);
        static bool EqualPath(NavGraph a,int ae,NavGraph b,int be)
        {
            int ap=a.EdgePath[ae],bp=b.EdgePath[be];if(ap<0||bp<0)return ap==bp;
            int ab=a.PathStart[ap],bb=b.PathStart[bp],count=a.PathStart[ap+1]-ab;
            if(count!=b.PathStart[bp+1]-bb)return false;
            if((a.PathLedge[ap]<0?-1:a.PathLedge[ap]-ab)!=(b.PathLedge[bp]<0?-1:b.PathLedge[bp]-bb))return false;
            if(a.PathNX[ap]!=b.PathNX[bp]||a.PathNZ[ap]!=b.PathNZ[bp]||a.PathD[ap]!=b.PathD[bp])return false;
            for(int p=0;p<count;p++)if(a.PathX[ab+p]!=b.PathX[bb+p]||a.PathY[ab+p]!=b.PathY[bb+p]||a.PathZ[ab+p]!=b.PathZ[bb+p])return false;
            return true;
        }
        static void SamePath(NavGraph a,int ae,NavGraph b,int be)
        {
            int ap=a.EdgePath[ae],bp=b.EdgePath[be];Assert.That(bp<0,Is.EqualTo(ap<0));if(ap<0)return;
            int ab=a.PathStart[ap],bb=b.PathStart[bp],count=a.PathStart[ap+1]-ab;
            Assert.That(b.PathStart[bp+1]-bb,Is.EqualTo(count));
            Assert.That(b.PathLedge[bp]<0?-1:b.PathLedge[bp]-bb,Is.EqualTo(a.PathLedge[ap]<0?-1:a.PathLedge[ap]-ab));
            Assert.That(b.PathNX[bp],Is.EqualTo(a.PathNX[ap]));Assert.That(b.PathNZ[bp],Is.EqualTo(a.PathNZ[ap]));Assert.That(b.PathD[bp],Is.EqualTo(a.PathD[ap]));
            for(int p=0;p<count;p++){
                Assert.That(b.PathX[bb+p],Is.EqualTo(a.PathX[ab+p]));
                Assert.That(b.PathY[bb+p],Is.EqualTo(a.PathY[ab+p]));
                Assert.That(b.PathZ[bb+p],Is.EqualTo(a.PathZ[ab+p]));
            }
        }
        static void Compare(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,NavGraph full,int size,bool reverse=false)
        {
            var lookup=new Dictionary<long,int>();for(int n=0;n<full.NodeCount;n++)lookup.Add(Key(full,n),n);
            var seen=new bool[full.NodeCount];var edges=new bool[full.EdgeCount];
            int columns=(int)Math.Ceiling(terrain.Size/size),owned=0,ownedEdges=0,crossing=0,attempts=0;
            long payload=0,maxPayload=0;double ms=0;
            for(int step=0;step<columns*columns;step++){
                int tile=reverse?columns*columns-1-step:step;
                var region=new NavRegionGraph(queries,terrain,inside,full.Class,size,tile%columns,tile/columns);var g=region.Graph;
                Assert.That(g.NeedsMoreContext,Is.False);ms+=region.BuildMs;attempts+=region.Attempts;
                long bytes=Payload(g);payload+=bytes;maxPayload=Math.Max(maxPayload,bytes);
                for(int n=0;n<g.NodeCount;n++){
                    Assert.That(lookup.TryGetValue(Key(g,n),out int a),Is.True,"Ningún nodo artificial en el halo");
                    Assert.That(g.NodeY[n],Is.EqualTo(full.NodeY[a]));Assert.That(g.NodeTerrain[n],Is.EqualTo(full.NodeTerrain[a]));
                    if(!g.Owns(n)){Assert.That(g.EdgeStart[n+1],Is.EqualTo(g.EdgeStart[n]));continue;}
                    Assert.That(seen[a],Is.False,"Propietario único");seen[a]=true;owned++;
                    Assert.That(g.EdgeStart[n+1]-g.EdgeStart[n],Is.EqualTo(full.EdgeStart[a+1]-full.EdgeStart[a]));
                    for(int e=g.EdgeStart[n];e<g.EdgeStart[n+1];e++){
                        int target=lookup[Key(g,g.EdgeTo[e])],match=-1;
                        for(int f=full.EdgeStart[a];f<full.EdgeStart[a+1];f++)
                            if(!edges[f]&&full.EdgeTo[f]==target&&full.EdgeKind[f]==g.EdgeKind[e]&&full.EdgeCost[f]==g.EdgeCost[e]&&EqualPath(full,f,g,e)){match=f;break;}
                        Assert.That(match,Is.GreaterThanOrEqualTo(0),"Mismo destino, dirección, clase, tipo y coste");
                        edges[match]=true;ownedEdges++;SamePath(full,match,g,e);
                        if(!g.Owns(g.EdgeTo[e]))crossing++;
                    }
                }
            }
            Assert.That(owned,Is.EqualTo(full.NodeCount));Assert.That(ownedEdges,Is.EqualTo(full.EdgeCount));
            foreach(bool present in seen)Assert.That(present,Is.True);foreach(bool present in edges)Assert.That(present,Is.True);
            // Igualdad de aristas dirigidas implica mismo alcance, incluidas rutas que salen y vuelven.
            // Comprobar también el recorrido desde un origen para detectar errores de indexación.
            if(full.NodeCount>0){
                var expected=ProceduralMap.Reach(full,0);var actual=new bool[full.NodeCount];var queue=new Queue<int>();actual[0]=true;queue.Enqueue(0);
                while(queue.Count>0){int a=queue.Dequeue();for(int e=full.EdgeStart[a];e<full.EdgeStart[a+1];e++)if(edges[e]&&!actual[full.EdgeTo[e]]){actual[full.EdgeTo[e]]=true;queue.Enqueue(full.EdgeTo[e]);}}
                Assert.That(actual,Is.EqualTo(expected));
            }
            TestContext.Out.WriteLine($"D1 clase={full.Class.Index} baldosa={size} nodos={owned} aristas={ownedEdges} cruces={crossing} construcciones={attempts} ms={ms:F3} payloadRegionesConHalos={payload} maxRegion={maxPayload} payloadMonolitico={Payload(full)} reverse={reverse}");
        }
        [TestCase(16)] [TestCase(32)] [TestCase(64)]
        public void CircuitsABMatchMonolithic(int size)
        {
            foreach(var layout in new[]{VerticalLayout.Escalada,VerticalLayout.Persecucion}){
                var c=new VerticalCircuit(layout);
                foreach(var body in NavClass.All){var full=new NavGraph(c.Queries,c.Terrain,c.Collision.IsInside,body);Compare(c.Queries,c.Terrain,c.Collision.IsInside,full,size);}
            }
        }
        static IEnumerable<TestCaseData> Seeds()
        {
            foreach(string seed in new[]{"P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"})
                foreach(int size in new[]{16,32,64})yield return new TestCaseData(seed,size).SetCategory("Regional-"+seed+"-"+size);
        }
        [TestCaseSource(nameof(Seeds))]
        public void Procedural128MatchesMonolithicForEveryClass(string seed,int size)
        {
            var c=new VerticalCircuit(new ProceduralMap(seed));
            foreach(var full in c.Map.Graphs)Compare(c.Queries,c.Terrain,c.Collision.IsInside,full,size);
        }
        [Test]
        public void MultilayerCornersAndSlopingSeamsMatchInEitherVisitOrder()
        {
            const int cells=80;var heights=new float[81*81];
            for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++)heights[z*81+x]=(float)(Math.Max(0,Math.Min(6,(x+z-60)*.2)));
            var terrain=new Heightfield(80,80,heights);
            var queries=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                new VerticalSolid(1,new Vec3(-14,7,-14),new Vec3(6,8,6)),
                new VerticalSolid(2,new Vec3(-12,13,-12),new Vec3(4,14,4)),
                new VerticalSolid(3,new Vec3(-6,0,10),new Vec3(-4,10,24)),
                new VerticalSolid(4,new Vec3(-1,0,10),new Vec3(1,10,24)));
            bool Inside(double x,double z,double r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36;
            foreach(var body in NavClass.All){var full=new NavGraph(queries,terrain,Inside,body);Compare(queries,terrain,Inside,full,16);Compare(queries,terrain,Inside,full,16,true);}
        }
    }
}
