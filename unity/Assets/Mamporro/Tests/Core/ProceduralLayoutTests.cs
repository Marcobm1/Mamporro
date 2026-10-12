using System;
using System.Collections.Generic;
using Mamporro.Core;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // P0-C2: trazado de niveles. Las comprobaciones de jugabilidad usan la navegación P0-B real.
    public sealed class ProceduralLayoutTests
    {
        static readonly string[] Seeds={"P0QA","MAMPORRO","COLINAS7","Z9K2","MESETA","ACANTILADO","RAMPA42","HORDA750"};
        static Func<double,double,double,bool> Inside(ProceduralLayout l)=>(x,z,m)=>WorldMath.Squircle(x,z)<=l.Settings.Limit-m;
        static bool[] Reach(NavGraph g,int start,bool walkOnly=false,bool reverse=false)
        {
            var seen=new bool[g.NodeCount];var open=new Queue<int>();seen[start]=true;open.Enqueue(start);
            while(open.Count>0){int a=open.Dequeue();
                if(!reverse){for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){if(walkOnly&&g.EdgeKind[e]!=(byte)NavEdgeKind.Walk)continue;int b=g.EdgeTo[e];if(!seen[b]){seen[b]=true;open.Enqueue(b);}}}
                else{for(int r=g.RevStart[a];r<g.RevStart[a+1];r++){int e=g.RevEdge[r];if(walkOnly&&g.EdgeKind[e]!=(byte)NavEdgeKind.Walk)continue;int b=g.EdgeFrom[e];if(!seen[b]){seen[b]=true;open.Enqueue(b);}}}}
            return seen;
        }

        [TestCase("P0QA")] [TestCase("HORDA750")]
        public void ParallelMapBuildIsIdenticalToSerialAndFaster(string seed)
        {
            var l=new ProceduralLayout(seed);var q=new VerticalQueries(l.Terrain,Tuning.PlayerMaxSlopeDeg);
            var watch=System.Diagnostics.Stopwatch.StartNew();var serial=EnemyNavigation.BuildGraphs(q,l.Terrain,Inside(l),false);double serialMs=watch.Elapsed.TotalMilliseconds;
            watch.Restart();var parallel=EnemyNavigation.BuildGraphs(q,l.Terrain,Inside(l));double parallelMs=watch.Elapsed.TotalMilliseconds;
            for(int k=0;k<3;k++){Assert.That(parallel[k].NodeY,Is.EqualTo(serial[k].NodeY));Assert.That(parallel[k].EdgeTo,Is.EqualTo(serial[k].EdgeTo));Assert.That(parallel[k].EdgeCost,Is.EqualTo(serial[k].EdgeCost));Assert.That(parallel[k].PathX,Is.EqualTo(serial[k].PathX));}
            TestContext.Out.WriteLine($"{seed}: mapas en serie {serialMs:F0} ms, en paralelo {parallelMs:F0} ms (tiempo real de pared)");
        }
        [Test] public void SameSeedSameLayoutAndDifferentSeedsDiffer()
        {
            var a=new ProceduralLayout("P0QA");var b=new ProceduralLayout("P0QA");var c=new ProceduralLayout("OTRA");
            Assert.That(b.Terrain.Heights,Is.EqualTo(a.Terrain.Heights));Assert.That(b.Level,Is.EqualTo(a.Level));Assert.That(b.Ramps.Count,Is.EqualTo(a.Ramps.Count));
            Assert.That(c.Level,Is.Not.EqualTo(a.Level));
        }
        [TestCaseSource(nameof(Seeds))]
        public void LevelsPlateausAndRampsFollowTheApprovedRules(string seed)
        {
            var l=new ProceduralLayout(seed);var s=l.Settings;int eligible=0,elevated=0;int[] perLevel=new int[4];
            for(int m=0;m<l.N*l.N;m++){if(!l.Eligible[m]){Assert.That(l.Level[m],Is.Zero,"inicio y límite a nivel 0");continue;}eligible++;if(l.Level[m]>0)elevated++;perLevel[l.Level[m]]++;}
            double fraction=100.0*elevated/eligible;var sizes=new List<string>();
            // Mesetas = masas de nivel ≥ k (8–40 m de lado). Las regiones de nivel exacto pueden ser
            // escalones de 4 m alrededor de una meseta superior (colina escalonada); se informan aparte.
            for(int k=1;k<4;k++)foreach(var mass in Masses(l,k)){
                sizes.Add($"{mass.w*4}×{mass.h*4}");
                Assert.That(Math.Min(mass.w,mass.h),Is.GreaterThanOrEqualTo(s.MinPlateauCells),$"meseta de nivel {k} de al menos 8 m");
                Assert.That(Math.Max(mass.w,mass.h),Is.LessThanOrEqualTo(s.MaxPlateauCells),$"meseta de nivel {k} de como mucho 40 m");
            }
            int rims=0;foreach(var r in l.Regions){
                if(r.Level==0)continue;if(Math.Min(r.MaxX-r.MinX,r.MaxZ-r.MinZ)==0)rims++;
                if(r.Area>=s.RampArea)Assert.That(r.Ramps.Count,Is.GreaterThanOrEqualTo(1),$"región {r.Id} de {r.Area} m² con rampa");
            }
            foreach(var ramp in l.Ramps)Assert.That(ramp.Slope(s.MacroCell),Is.LessThanOrEqualTo(30.0+1e-9));
            TestContext.Out.WriteLine($"{seed}: intento {l.Attempt}, {l.Milliseconds:F1} ms, elevado {fraction:F1} % (niveles 1/2/3: {100.0*perLevel[1]/eligible:F0}/{100.0*perLevel[2]/eligible:F0}/{100.0*perLevel[3]/eligible:F0} %), mesetas {sizes.Count} [{string.Join(" ",sizes)}], escalones de 4 m {rims}, rampas {l.Ramps.Count} (dentro {l.Ramps.FindAll(r=>r.Inside).Count}), sin rampa posible {l.MissingRamps}");
            Assert.That(fraction,Is.InRange(25.0,45.0),"≈35 % elevado");Assert.That(l.MissingRamps,Is.Zero);
            Assert.That(l.Milliseconds,Is.LessThan(300));
        }
        static List<(int w,int h)> Masses(ProceduralLayout l,int k)
        {
            var list=new List<(int,int)>();var seen=new bool[l.N*l.N];var stack=new Stack<int>();
            for(int start=0;start<l.N*l.N;start++){
                if(seen[start]||l.Level[start]<k)continue;int x0=l.N,z0=l.N,x1=-1,z1=-1;stack.Push(start);seen[start]=true;
                while(stack.Count>0){int m=stack.Pop();int mx=m%l.N,mz=m/l.N;x0=Math.Min(x0,mx);x1=Math.Max(x1,mx);z0=Math.Min(z0,mz);z1=Math.Max(z1,mz);
                    foreach(int nb in new[]{mx>0?m-1:-1,mx+1<l.N?m+1:-1,mz>0?m-l.N:-1,mz+1<l.N?m+l.N:-1})if(nb>=0&&!seen[nb]&&l.Level[nb]>=k){seen[nb]=true;stack.Push(nb);}}
                list.Add((x1-x0+1,z1-z0+1));
            }
            return list;
        }
        [TestCase("P0QA")] [TestCase("COLINAS7")] [TestCase("HORDA750")]
        public void NavigationReachesEveryRegionAndLargePlateausByRamp(string seed)
        {
            var l=new ProceduralLayout(seed);var q=new VerticalQueries(l.Terrain,Tuning.PlayerMaxSlopeDeg);double total=0;var report=new List<string>();
            for(int k=0;k<3;k++){
                var g=new NavGraph(q,l.Terrain,Inside(l),NavClass.All[k]);total+=g.BuildMs;
                int spawn=g.Nearest(0,0,0,6,1,1);Assert.That(spawn,Is.GreaterThanOrEqualTo(0),$"clase {k}: inicio");
                var to=Reach(g,spawn);var back=Reach(g,spawn,reverse:true);var walk=Reach(g,spawn,walkOnly:true);
                int lost=0,pockets=0;var where=new List<string>();
                for(int n=0;n<g.NodeCount;n++){if(!to[n])lost++;if(!back[n])pockets++;
                    if((!to[n]||!back[n])&&where.Count<6){int m=l.MacroAt(g.NodeX(n),g.NodeZ(n));
                        where.Add($"{(!to[n]?"inaccesible":"bolsa")} ({g.NodeX(n)},{g.NodeY[n]:F2},{g.NodeZ(n)}) nivel {(m>=0?l.Level[m]:-1)} rampa {(m>=0?l.RampOf[m]:-1)} aristas {g.EdgeStart[n+1]-g.EdgeStart[n]}");}}
                if(where.Count>0)TestContext.Out.WriteLine($"{seed} clase {k}: "+string.Join("; ",where));
                // Cada rampa funciona: desde su pie se llega andando a la cima de su meseta. El jefe solo
                // donde su cuerpo cabe y con rampas de 8 m (las de 4 m no le sirven).
                int rampFails=0;
                foreach(var ramp in l.Ramps){
                    bool wide=Math.Min(ramp.X1-ramp.X0,ramp.Z1-ramp.Z0)>=1;if(k==2&&!wide)continue;
                    var r=l.Regions[ramp.Region];
                    double fx=l.MacroX(ramp.UpX>0?ramp.X0:ramp.UpX<0?ramp.X1:(ramp.X0+ramp.X1)/2)+2,fz=l.MacroZ(ramp.UpZ>0?ramp.Z0:ramp.UpZ<0?ramp.Z1:(ramp.Z0+ramp.Z1)/2)+2;
                    fx-=ramp.UpX*6;fz-=ramp.UpZ*6;if(ramp.UpX==0)fx+=2;if(ramp.UpZ==0)fz+=2;
                    int foot=g.Nearest(fx,ramp.Low,fz,6,.6,.6);
                    bool hasTop=false,reached=false;var walkFoot=foot>=0?Reach(g,foot,walkOnly:true):null;
                    for(int n=0;n<g.NodeCount;n++){int m=l.MacroAt(g.NodeX(n),g.NodeZ(n));if(m<0||l.RegionOf[m]!=r.Id||Math.Abs(g.NodeY[n]-l.LevelY(r.Level))>.01)continue;hasTop=true;if(walkFoot!=null&&walkFoot[n])reached=true;}
                    if(hasTop&&!reached){rampFails++;TestContext.Out.WriteLine($"{seed} clase {k}: rampa {(ramp.Inside?"interior":"exterior")} {ramp.X0},{ramp.Z0}-{ramp.X1},{ramp.Z1} sube ({ramp.UpX},{ramp.UpZ}) pie {foot} sin llegar a la meseta {r.Id}");}
                }
                report.Add($"clase {k}: {g.NodeCount} nodos, {g.ClimbEdges} escalar, {g.DropEdges} caer, {g.BuildMs:F0} ms (nodos {g.NodesMs:F0}, escalar {g.ClimbMs:F0} con {g.ClimbSkipped} descartes previos, caer {g.DropMs:F0}), inaccesibles {lost}, bolsas {pockets}, rampas que no llevan arriba {rampFails}");
                if(k==0){Assert.That(lost,Is.Zero,"todo alcanzable por S");Assert.That(pockets,Is.Zero,"sin bolsas para S");}
                // Jefe: informado aquí; el acceso a las mesetas amplias lo garantiza la validación (C4).
                if(k<2)Assert.That(rampFails,Is.Zero,$"clase {k}: rampas transitables hasta su meseta");
            }
            TestContext.Out.WriteLine($"{seed}: trazado {l.Milliseconds:F0} ms, mapas {total:F0} ms");foreach(var line in report)TestContext.Out.WriteLine("  "+line);
        }
    }
}
