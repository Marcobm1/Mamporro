using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    public sealed class GraphReuseTests
    {
        static readonly string[] Seeds={"A","B","P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750",
            "barrier","roofs","drop","climb","ramp","pass"};
        static IEnumerable<TestCaseData> Corpus(){foreach(string s in Seeds)foreach(int size in new[]{16,32,64})yield return new TestCaseData(s,size);}
        static IEnumerable<TestCaseData> Critical(){foreach(string s in new[]{"MAMPORRO","Z9K2","RAMPA42"})foreach(int size in new[]{16,32,64})yield return new TestCaseData(s,size);}
        static void Log(string tag,params object[] pairs)
        {var text=new List<string>{tag};for(int i=0;i<pairs.Length;i+=2)text.Add(pairs[i]+"="+Convert.ToString(pairs[i+1],CultureInfo.InvariantCulture));TestContext.Out.WriteLine(string.Join(" ",text));}
        static bool Inside(double x,double z,double r)=>Math.Abs(x)+r<36&&Math.Abs(z)+r<36;
        sealed class Input
        {
            internal VerticalQueries Queries;internal Heightfield Terrain;internal Func<double,double,double,bool> Inside;
            internal NavGraph[] Graphs;internal ProceduralMap Map;
            internal Input(string label)
            {
                if(Array.IndexOf(Seeds,label)<10){
                    var c=label=="A"?new VerticalCircuit(VerticalLayout.Escalada):label=="B"?new VerticalCircuit(VerticalLayout.Persecucion):new VerticalCircuit(new ProceduralMap(label));
                    Queries=c.Queries;Terrain=c.Terrain;Inside=c.Collision.IsInside;Map=c.Map;
                    if(Map!=null){if(!Map.Check.Passed)throw new InvalidOperationException("Mapa no validado");Graphs=Map.Graphs;}
                }else{
                    var h=new float[81*81];if(label=="ramp")for(int z=0;z<=80;z++)for(int x=0;x<=80;x++)h[z*81+x]=(float)Math.Max(0,Math.Min(6,(x+z-60)*.2));
                    Terrain=new Heightfield(80,80,h);VerticalSolid[] solids;
                    switch(label){
                        case "barrier":solids=new[]{new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-17,0,-40),new Vec3(-15,20,-7))};break;
                        case "roofs":solids=new[]{new VerticalSolid(1,new Vec3(8,6,8),new Vec3(24,7,24)),new VerticalSolid(2,new Vec3(10,13,10),new Vec3(22,14,22))};break;
                        case "drop":solids=new[]{new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24)),new VerticalSolid(2,new Vec3(7.75,6,7.75),new Vec3(24.25,7,24.25))};break;
                        case "climb":solids=new[]{new VerticalSolid(1,new Vec3(8,0,8),new Vec3(24,6,24))};break;
                        case "pass":solids=new[]{new VerticalSolid(1,VerticalSurfaceKind.Foliage,new Vec3(-40,0,-1),new Vec3(-1.5,20,1)),new VerticalSolid(2,VerticalSurfaceKind.Foliage,new Vec3(1.5,0,-1),new Vec3(40,20,1))};break;
                        default:solids=new[]{new VerticalSolid(1,new Vec3(-14,7,-14),new Vec3(6,8,6))};break;
                    }
                    Queries=new VerticalQueries(Terrain,Tuning.PlayerMaxSlopeDeg,solids);Inside=GraphReuseTests.Inside;
                }
                if(Graphs==null)Graphs=NavClass.All.Select(b=>new NavGraph(Queries,Terrain,Inside,b)).ToArray();
            }
        }
        static bool SamePath(NavGraph a,int p,NavGraph b,int q)
        {
            if(p<0||q<0)return p<0&&q<0;
            int start=a.PathStart[p],end=a.PathStart[p+1],other=b.PathStart[q];
            if(end-start!=b.PathStart[q+1]-other||a.PathNX[p]!=b.PathNX[q]||a.PathNZ[p]!=b.PathNZ[q]||a.PathD[p]!=b.PathD[q]||
                (a.PathLedge[p]<0?-1:a.PathLedge[p]-start)!=(b.PathLedge[q]<0?-1:b.PathLedge[q]-other))return false;
            for(int i=0;i<end-start;i++)if(a.PathX[start+i]!=b.PathX[other+i]||a.PathY[start+i]!=b.PathY[other+i]||a.PathZ[start+i]!=b.PathZ[other+i])return false;
            return true;
        }
        static void Equal(int[] a,int[] b,string label){if(!a.SequenceEqual(b))Assert.Fail(label);}
        static void Verify(NavPortalWorld w,ReusedRegionalWorld r,string label)
        {
            var g=r.Source;
            Assert.That(r.NodeCount,Is.EqualTo(w.NodeCount));Assert.That(r.EdgeCount,Is.EqualTo(w.EdgeCount));
            var nodes=new bool[g.NodeCount];var edges=new bool[g.EdgeCount];
            Equal(w.NodeRegion,r.NodeRegion,"Propietarios");Equal(w.EdgeStart,r.EdgeStart,"Inicio aristas");
            Equal(w.EdgeFrom,r.EdgeFrom,"Origen dirigido");Equal(w.EdgeTo,r.EdgeTo,"Destino dirigido");
            var matched=new bool[w.EdgeCount];var edgeMap=new int[r.EdgeCount];int reordered=0;
            for(int n=0;n<r.NodeCount;n++){
                int rn=r.NodeLocal[n],wn=w.NodeLocal[n];var wg=w.Graph(n);
                if(nodes[rn])Assert.Fail("Propietario duplicado");nodes[rn]=true;
                long key=NavPortalWorld.NodeKey(g,rn);
                if(key!=NavPortalWorld.NodeKey(wg,wn)||w.Find(key)!=n||r.Find(key)!=n||g.NodeY[rn]!=wg.NodeY[wn]||
                    g.NodeTerrain[rn]!=wg.NodeTerrain[wn]||g.NodeX(rn)!=wg.NodeX(wn)||g.NodeZ(rn)!=wg.NodeZ(wn))Assert.Fail("Identidad/capa/altura");
                var p=r.Position(n);if(r.NodeAt(p.X,p.Y,p.Z)!=n||w.NodeAt(p.X,p.Y,p.Z)!=n)Assert.Fail("Consulta de frontera/capa");
            }
            for(int e=0;e<r.EdgeCount;e++){
                int re=r.EdgeLocal[e],a=r.EdgeFrom[e],found=-1;var wg=w.Graph(a);
                if(edges[re])Assert.Fail("Arista original duplicada");edges[re]=true;
                // Sort de NavGraph no desempata aristas paralelas. El ordinal no es identidad física.
                for(int k=w.EdgeStart[a];k<w.EdgeStart[a+1];k++){
                    int we=w.EdgeLocal[k];
                    if(!matched[k]&&r.EdgeTo[e]==w.EdgeTo[k]&&r.EdgeCost[e]==w.EdgeCost[k]&&g.EdgeKind[re]==wg.EdgeKind[we]&&
                        SamePath(g,g.EdgePath[re],wg,wg.EdgePath[we])){found=k;break;}}
                if(found<0)Assert.Fail("Arista sin pareja exacta: "+label+" size="+r.Size+" class="+r.Class.Index+" edge="+e);
                matched[found]=true;edgeMap[e]=found;if(found!=e)reordered++;
            }
            if(nodes.Any(x=>!x)||edges.Any(x=>!x))Assert.Fail("Universo original incompleto");
            Equal(w.Component,r.Component,"SCC local");Equal(w.ComponentStart,r.ComponentStart,"Inicio SCC");Equal(w.ComponentTo,r.ComponentTo,"Arcos SCC");
            Equal(w.ComponentRegion,r.ComponentRegion,"Región SCC");Equal(w.WeakComponent,r.WeakComponent,"Componente débil");Equal(w.GlobalStrong,r.GlobalStrong,"SCC global");
            Assert.That(r.Portals.Length,Is.EqualTo(w.Portals.Length));
            var portals=w.Portals.ToDictionary(p=>((long)p.From<<32)|(uint)p.LocalEdge);
            for(int i=0;i<r.Portals.Length;i++){
                var a=r.Portals[i];int re=r.EdgeStart[a.From]+a.LocalEdge-g.EdgeStart[a.LocalFrom],we=edgeMap[re];
                long key=((long)a.From<<32)|(uint)w.EdgeLocal[we];
                if(!portals.TryGetValue(key,out var b))Assert.Fail("Portal sin pareja");portals.Remove(key);var bg=w.Graph(b.From);
                if(a.From!=b.From||a.To!=b.To||a.FromRegion!=b.FromRegion||a.ToRegion!=b.ToRegion||a.Kind!=b.Kind||a.Cost!=b.Cost||a.ClassIndex!=b.ClassIndex||
                    a.LocalFrom!=r.NodeLocal[a.From]||a.LocalTo!=r.NodeLocal[a.To]||!SamePath(g,a.Path,bg,b.Path))Assert.Fail("Portal físico diferente");
            }
            Assert.That(portals,Is.Empty);
            Log("H1D-ORDER","case",label,"size",r.Size,"class",r.Class.Index,"reordered",reordered);
            // La biyección completa de aristas implica alcance exacto de TODAS las parejas.
            // Los controles siguientes conservan además las expectativas de los fixtures históricos.
            var query=new NavPortalQuery(w);
            if(label=="barrier"&&w.Size==16){int a=w.NodeAt(-21,0,-16),b=w.NodeAt(-11,0,-16);var path=new List<int>();
                Assert.That(w.NodeRegion[a],Is.EqualTo(w.NodeRegion[b]));Assert.That(w.Component[a],Is.Not.EqualTo(w.Component[b]));
                Assert.That(query.Route(a,b,path),Is.True);Assert.That(path.Count(e=>w.NodeRegion[w.EdgeFrom[e]]!=w.NodeRegion[w.EdgeTo[e]]),Is.GreaterThanOrEqualTo(2));}
            if(label=="drop")Assert.That(Enumerable.Range(0,w.EdgeCount).Any(e=>w.Graph(w.EdgeFrom[e]).EdgeKind[w.EdgeLocal[e]]==(byte)NavEdgeKind.Drop&&!query.Reachable(w.EdgeTo[e],w.EdgeFrom[e])),Is.True);
            if(label=="climb")Assert.That(g.ClimbEdges,Is.GreaterThan(0));
            if(label=="roofs")Assert.That(query.Reachable(w.NodeAt(16,0,16),w.NodeAt(16,14,16)),Is.False);
            if(label=="pass")Assert.That(query.Reachable(w.NodeAt(0,0,-10),w.NodeAt(0,0,10)),Is.EqualTo(g.Class.Index<2));
        }
        [TestCaseSource(nameof(Corpus)),Category("GraphReuseA")]
        public void ExactRegionalProjection(string label,int size)
        {
            var input=new Input(label);
            foreach(var body in NavClass.All){var g=input.Graphs[body.Index];var r=new ReusedRegionalWorld(g,size);
                var w=new NavPortalWorld(input.Queries,input.Terrain,input.Inside,body,size);Verify(w,r,label);
                Log("H1D-EXACT","case",label,"size",size,"class",body.Index,"nodes",r.NodeCount,"edges",r.EdgeCount,"portals",r.Portals.Length,"components",r.ComponentCount);}
        }
        static int[] Oracle(NavGraph g,int goal)
        {
            var d=Enumerable.Repeat(int.MaxValue,g.NodeCount).ToArray();var queued=new bool[d.Length];var q=new Queue<int>();
            d[goal]=0;q.Enqueue(goal);queued[goal]=true;
            while(q.Count>0){int b=q.Dequeue();queued[b]=false;
                for(int j=g.RevStart[b];j<g.RevStart[b+1];j++){int e=g.RevEdge[j],a=g.EdgeFrom[e],v=checked(d[b]+g.EdgeCost[e]);
                    if(v>=d[a])continue;d[a]=v;if(!queued[a]){queued[a]=true;q.Enqueue(a);}}
            }return d;
        }
        [TestCaseSource(nameof(Corpus)),Category("GraphReuseQuality")]
        public void PreservesH1bRoutes(string label,int size)
        {
            var input=new Input(label);
            foreach(var body in NavClass.All){
                var g=input.Graphs[body.Index];var r=new ReusedRegionalWorld(g,size);var w=new NavPortalWorld(input.Queries,input.Terrain,input.Inside,body,size);
                var original=new AdaptiveCorridorProbe(w);var reused=new ReusedCorridorProbe(r);
                var ordered=Enumerable.Range(0,w.NodeCount).OrderBy(n=>NavPortalWorld.NodeKey(w.Graph(n),w.NodeLocal[n])).ToArray();
                var goals=new SortedSet<int>();for(int i=0;i<4;i++)goals.Add(ordered[i*(ordered.Length-1)/3]);
                var global=new HashSet<int>();foreach(int n in ordered)if(global.Add(w.GlobalStrong[w.Component[n]]))goals.Add(n);
                var cross=Enumerable.Range(0,w.EdgeCount).Where(e=>w.Component[w.EdgeFrom[e]]!=w.Component[w.EdgeTo[e]]).ToArray();
                var adjacent=new Dictionary<int,List<int>>();
                for(int i=0;i<Math.Min(6,cross.Length);i++){
                    int e=cross[i*(cross.Length-1)/Math.Max(1,Math.Min(6,cross.Length)-1)],goal=w.EdgeTo[e];goals.Add(goal);
                    if(!adjacent.ContainsKey(goal))adjacent[goal]=new List<int>();adjacent[goal].Add(w.EdgeFrom[e]);}
                if(label=="Z9K2"&&size==64&&body.Index==0){goals.Add(10514);if(!adjacent.ContainsKey(10514))adjacent[10514]=new List<int>();adjacent[10514].Add(62);}
                if(label=="RAMPA42"&&size==64&&body.Index==2){goals.Add(5393);if(!adjacent.ContainsKey(5393))adjacent[5393]=new List<int>();adjacent[5393].Add(12654);}
                var ratios=new List<double>();var path=new List<int>();long checks=0,routes=0,parents=0;
                foreach(int goal in goals){
                    original.Build(goal,3);reused.Build(goal,3);Equal(original.Distance,reused.Distance,"Distancias H1b");Equal(original.Potential,reused.Potential,"Potencial H1b");
                    var expected=Oracle(g,r.NodeLocal[goal]);
                    for(int n=0;n<r.NodeCount;n++){
                        if((reused.Distance[n]!=int.MaxValue)!=(expected[r.NodeLocal[n]]!=int.MaxValue))Assert.Fail("Alcance contra oráculo");checks++;
                        if(n!=goal&&reused.Distance[n]!=int.MaxValue){int e=reused.Parent[n];if(e<0||r.EdgeFrom[e]!=n||r.EdgeCost[e]<=0||
                            (long)r.EdgeCost[e]+reused.Distance[r.EdgeTo[e]]!=reused.Distance[n])Assert.Fail("Padre físico no descendente");parents++;}}
                    var sources=new SortedSet<int>();for(int i=0;i<24;i++)sources.Add(ordered[i*(ordered.Length-1)/23]);sources.Add(goal);
                    if(adjacent.ContainsKey(goal))foreach(int n in adjacent[goal])sources.Add(n);
                    foreach(int source in sources){int optimal=expected[r.NodeLocal[source]];bool found=reused.Route(source,goal,path);
                        Assert.That(found,Is.EqualTo(optimal!=int.MaxValue));if(!found)continue;int at=source;long cost=0;
                        foreach(int e in path){if(r.EdgeFrom[e]!=at)Assert.Fail("Ruta discontinua");at=r.EdgeTo[e];cost+=r.EdgeCost[e];}
                        Assert.That(at,Is.EqualTo(goal));Assert.That(cost,Is.EqualTo(reused.Distance[source]));Assert.That(cost,Is.GreaterThanOrEqualTo((long)optimal));routes++;
                        if(optimal==0){Assert.That(path,Is.Empty);continue;}ratios.Add((double)cost/optimal);
                        if(label=="Z9K2"&&size==64&&body.Index==0&&source==62&&goal==10514)Assert.That(cost,Is.EqualTo(50));
                    }
                }
                ratios.Sort();double p95=ratios[(int)Math.Ceiling(.95*ratios.Count)-1],max=ratios.Max();
                Log("H1D-QUALITY","case",label,"size",size,"class",body.Index,"checks",checks,"parents",parents,"routes",routes,"samples",ratios.Count,"p95",p95,"max",max);
                Assert.That(p95,Is.LessThanOrEqualTo(1.5));Assert.That(max,Is.LessThanOrEqualTo(2));
            }
        }
    }
}
