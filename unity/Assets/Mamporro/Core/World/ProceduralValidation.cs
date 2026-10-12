using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Resultado de comprobar un mapa con la navegación P0-B (sección 6.9 del plan y decisión 8).
    public sealed class ProceduralCheck
    {
        public readonly int[] Lost=new int[3],Pockets=new int[3],LargestLost=new int[3];
        public int TopsWithoutAccess,MarkersWithoutAccess,GroundLostL,Tolerated;
        public readonly HashSet<ProceduralStructure> Culprits=new HashSet<ProceduralStructure>();
        public int Unattributed;
        public readonly List<string> Notes=new List<string>();
        // S: todo alcanzable y sin bolsas. M: ningún grupo inaccesible mayor que el umbral. L: el suelo
        // principal y las mesetas amplias (grupos mayores que su umbral) alcanzables, sin bolsas.
        public bool Passed=>Lost[0]==0&&Pockets[0]==0&&TopsWithoutAccess==0&&MarkersWithoutAccess==0
            &&LargestLost[1]<=ProceduralMap.MaxLostNodesM&&Pockets[1]==0
            &&LargestLost[2]<=ProceduralMap.MaxLostNodesL&&Pockets[2]==0;
        public string Summary()=>$"S {Lost[0]}/{Pockets[0]}, M {Lost[1]}/{Pockets[1]} (mayor {LargestLost[1]}), L {Lost[2]}/{Pockets[2]} (mayor {LargestLost[2]}, en suelo {GroundLostL}), tolerados {Tolerated}, tapas sin acceso {TopsWithoutAccess}, marcadores sin acceso {MarkersWithoutAccess}";
    }

    // P0-C4: mapa procedural validado. Construye trazado, estructuras y los grafos S/M/L, comprueba la
    // jugabilidad y repara de forma determinista: primero quita las estructuras culpables; si el fallo
    // es del terreno o persiste, rechaza el intento y genera el siguiente de la serie. Los grafos
    // aceptados se reutilizan en juego (no se reconstruyen).
    public sealed class ProceduralMap
    {
        public const int MaxRepairRounds=2,MaxRejections=4;
        // Umbrales en nodos de 0,25 m²: grupos inaccesibles de hasta 32 m² para M y L (huecos donde el
        // cuerpo apenas cabe; el del jefe ocupa ~23 m²). El suelo principal y las mesetas amplias superan
        // con mucho ese tamaño, así que deben ser alcanzables.
        public const int MaxLostNodesM=128,MaxLostNodesL=128;
        // Grupos diminutos (≤ 1 m²) tolerados si un nodo válido de la misma clase está a distancia de
        // contacto con el jugador (la regla de EnemiesNav: radios sumados y diferencia de altura
        // < 1,6 m, más media altura en los altos): el enemigo lo alcanza desde ahí, no es refugio.
        public const int TinyGroup=4;
        public readonly string Seed;
        public ProceduralLayout Layout {get;private set;}
        public ProceduralWorld World {get;private set;}
        public VerticalQueries Queries {get;private set;}
        public NavGraph[] Graphs {get;private set;}
        public ProceduralCheck Check {get;private set;}
        public int Repairs,Rejections,Builds,RemovedStructures,RelocatedMarkers,PreRemoved,PreRelocated;
        public double GenerationMs,MapsMs,ValidationMs,PreCheckMs,TotalMs;
        public readonly List<string> Log=new List<string>();
        public Func<double,double,double,bool> Inside=>(x,z,m)=>WorldMath.Squircle(x,z)<=Layout.Settings.Limit-m;

        public ProceduralMap(string seed,ProceduralSettings settings=null)
        {
            var total=System.Diagnostics.Stopwatch.StartNew();Seed=seed;int first=0;
            for(Rejections=0;;Rejections++){
                var watch=System.Diagnostics.Stopwatch.StartNew();
                Layout=new ProceduralLayout(seed,settings,first);World=new ProceduralWorld(Layout);
                GenerationMs+=watch.Elapsed.TotalMilliseconds;
                PreRepair();
                bool accepted=false;
                for(int round=0;;round++){
                    Build();Check=Validate();
                    Log.Add($"intento {Layout.Attempt}, ronda {round}: {Check.Summary()}; culpables {Check.Culprits.Count}, sin atribuir {Check.Unattributed}");
                    if(Check.Passed){accepted=true;break;}
                    foreach(var note in Check.Notes)Log.Add("    "+note);
                    if(round>=MaxRepairRounds||Check.Culprits.Count==0||Check.Unattributed>0)break;
                    // En el orden de la lista (determinista): recolocar consume números aleatorios.
                    foreach(var s in new List<ProceduralStructure>(World.Structures)){
                        if(!Check.Culprits.Contains(s))continue;
                        if(ProceduralWorld.IsMarker(s.Kind)){if(World.Relocate(s)){RelocatedMarkers++;Log.Add($"  recoloca {s.Kind} de ({s.X0:F0},{s.Z0:F0})");}}
                        else{s.Removed=true;RemovedStructures++;Log.Add($"  quita {s.Kind} en ({s.X0:F0},{s.Z0:F0})");}
                    }
                    Repairs++;
                }
                if(accepted||Rejections>=MaxRejections)break;
                first=Layout.Attempt+1;
            }
            TotalMs=total.Elapsed.TotalMilliseconds;
        }
        // Comprobación previa barata del paso del jefe (rejilla de 1 m, milisegundos) para no pagar una
        // reconstrucción de los mapas (~1 s) por cada obstáculo que cierra un paso. Celdas bloqueadas:
        // acantilados (salto > 0,6 m en la celda), fuera del límite y huellas de obstáculos; libres las
        // que quedan a 2,65 m (radio del jefe con margen) de toda celda bloqueada; conexión entre celdas
        // libres vecinas con escalón ≤ 0,6 m. Los obstáculos que separan del inicio una zona libre de
        // más de 32 m² se quitan (o se recolocan, si son marcadores). Es una aproximación conservadora:
        // la validación con NavGraph sigue decidiendo.
        void PreRepair()
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();var t=Layout.Terrain;var st=Layout.Settings;
            int n=(int)Math.Round(st.Size);double half=st.Size/2,clear=NavClass.All[2].Radius+NavGraph.Margin;int R=(int)Math.Ceiling(clear+.5);
            var h=new double[n*n];var wall=new bool[n*n];
            for(int j=0;j<n;j++)for(int i=0;i<n;i++){
                double a=t.VertexHeight(i,j),b=t.VertexHeight(i+1,j),c=t.VertexHeight(i,j+1),d=t.VertexHeight(i+1,j+1);
                double lo=Math.Min(Math.Min(a,b),Math.Min(c,d)),hi=Math.Max(Math.Max(a,b),Math.Max(c,d));
                h[j*n+i]=(a+b+c+d)/4;wall[j*n+i]=hi-lo>NavGraph.Step||WorldMath.Squircle(-half+i+.5,-half+j+.5)>st.Limit;
            }
            for(int round=0;round<6;round++){
                var blocked=(bool[])wall.Clone();
                foreach(var s in World.Structures){if(s.Removed)continue;
                    for(int z=(int)Math.Floor(s.Z0+half);z<(int)Math.Ceiling(s.Z1+half);z++)for(int x=(int)Math.Floor(s.X0+half);x<(int)Math.Ceiling(s.X1+half);x++)
                        if(x>=0&&z>=0&&x<n&&z<n)blocked[z*n+x]=true;}
                var free=new bool[n*n];for(int k=0;k<n*n;k++)free[k]=true;
                for(int bj=0;bj<n;bj++)for(int bi=0;bi<n;bi++){
                    if(!blocked[bj*n+bi])continue;
                    for(int dj=-R;dj<=R;dj++)for(int di=-R;di<=R;di++){
                        int i=bi+di,j=bj+dj;if(i<0||j<0||i>=n||j>=n)continue;
                        double dx=Math.Max(0,Math.Abs(di)-.5),dz=Math.Max(0,Math.Abs(dj)-.5);if(dx*dx+dz*dz<clear*clear)free[j*n+i]=false;
                    }
                }
                // Componentes de celdas libres conectadas; la del inicio es la alcanzable.
                var comp=new int[n*n];for(int k=0;k<n*n;k++)comp[k]=-1;var sizes=new List<int>();var stack=new Stack<int>();
                for(int k=0;k<n*n;k++){
                    if(!free[k]||comp[k]>=0)continue;int id=sizes.Count,count=0;comp[k]=id;stack.Push(k);
                    while(stack.Count>0){int a=stack.Pop();count++;int ax=a%n,az=a/n;
                        foreach(int b in new[]{ax>0?a-1:-1,ax+1<n?a+1:-1,az>0?a-n:-1,az+1<n?a+n:-1})
                            if(b>=0&&free[b]&&comp[b]<0&&Math.Abs(h[b]-h[a])<=NavGraph.Step){comp[b]=id;stack.Push(b);}}
                    sizes.Add(count);
                }
                int spawn=-1;double best=double.MaxValue;
                for(int k=0;k<n*n;k++)if(free[k]){double x=-half+k%n+.5,z=-half+k/n+.5,d=x*x+z*z;if(d<best){best=d;spawn=comp[k];}}
                if(spawn<0)break;
                var blockers=new List<ProceduralStructure>();double grow=clear+1.5;
                foreach(var s in World.Structures){if(s.Removed)continue;bool nearLost=false,nearMain=false;
                    for(int z=(int)Math.Floor(s.Z0-grow+half);z<(int)Math.Ceiling(s.Z1+grow+half)&&!(nearLost&&nearMain);z++)
                        for(int x=(int)Math.Floor(s.X0-grow+half);x<(int)Math.Ceiling(s.X1+grow+half);x++){
                            if(x<0||z<0||x>=n||z>=n)continue;int c=comp[z*n+x];if(c<0)continue;
                            if(c==spawn)nearMain=true;else if(sizes[c]>=32)nearLost=true;
                        }
                    if(nearLost&&nearMain)blockers.Add(s);
                }
                if(blockers.Count==0)break;
                foreach(var s in blockers){
                    if(ProceduralWorld.IsMarker(s.Kind)){if(World.Relocate(s))PreRelocated++;}else{s.Removed=true;PreRemoved++;}
                }
            }
            if(PreRemoved+PreRelocated>0)Log.Add($"comprobación previa: quita {PreRemoved}, recoloca {PreRelocated}");
            PreCheckMs+=watch.Elapsed.TotalMilliseconds;
        }
        void Build()
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            Queries=new VerticalQueries(Layout.Terrain,Tuning.PlayerMaxSlopeDeg,new List<VerticalSolid>(World.Solids()).ToArray());
            Graphs=EnemyNavigation.BuildGraphs(Queries,Layout.Terrain,Inside);Builds++;
            MapsMs+=watch.Elapsed.TotalMilliseconds;
        }
        public int SpawnNode(int k)=>Graphs[k].Nearest(0,0,0,6,1,1);

        public static bool[] Reach(NavGraph g,int start,bool reverse=false)
        {
            var seen=new bool[g.NodeCount];if(start<0)return seen;var open=new Queue<int>();seen[start]=true;open.Enqueue(start);
            while(open.Count>0){int a=open.Dequeue();
                if(!reverse){for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){int b=g.EdgeTo[e];if(!seen[b]){seen[b]=true;open.Enqueue(b);}}}
                else{for(int r=g.RevStart[a];r<g.RevStart[a+1];r++){int b=g.EdgeFrom[g.RevEdge[r]];if(!seen[b]){seen[b]=true;open.Enqueue(b);}}}}
            return seen;
        }
        // Estructura cuya huella (ampliada) contiene el punto; null si el fallo es del terreno.
        ProceduralStructure Owner(double x,double z,double y,double grow)
        {
            ProceduralStructure best=null;double bestD=double.MaxValue;
            foreach(var s in World.Structures){
                if(s.Removed||x<s.X0-grow||x>s.X1+grow||z<s.Z0-grow||z>s.Z1+grow)continue;
                double dx=Math.Max(0,Math.Max(s.X0-x,x-s.X1)),dz=Math.Max(0,Math.Max(s.Z0-z,z-s.Z1)),d=dx*dx+dz*dz;
                if(d<bestD){bestD=d;best=s;}
            }
            return best;
        }
        // Grupos de nodos fallidos conectados por aristas del grafo (en cualquier sentido).
        static List<List<int>> Groups(NavGraph g,bool[] bad)
        {
            var groups=new List<List<int>>();var seen=new bool[g.NodeCount];var stack=new Stack<int>();
            for(int n=0;n<g.NodeCount;n++){
                if(!bad[n]||seen[n])continue;var list=new List<int>();seen[n]=true;stack.Push(n);
                while(stack.Count>0){int a=stack.Pop();list.Add(a);
                    for(int e=g.EdgeStart[a];e<g.EdgeStart[a+1];e++){int b=g.EdgeTo[e];if(bad[b]&&!seen[b]){seen[b]=true;stack.Push(b);}}
                    for(int r=g.RevStart[a];r<g.RevStart[a+1];r++){int b=g.EdgeFrom[g.RevEdge[r]];if(bad[b]&&!seen[b]){seen[b]=true;stack.Push(b);}}}
                groups.Add(list);
            }
            return groups;
        }
        void Blame(ProceduralCheck c,NavGraph g,List<int> group,string what,bool[] good)
        {
            // Culpables: los obstáculos que cierran el paso, es decir, a alcance del cuerpo de la clase
            // tanto de un nodo del grupo como de un nodo válido. Si no hay, la estructura que contiene
            // el grupo (huella ampliada 2 m). Si tampoco, el fallo es del terreno.
            double reach=g.Class.Radius+NavGraph.Margin+1;var blockers=new List<ProceduralStructure>();
            foreach(var s in World.Structures){
                if(s.Removed)continue;bool nearGroup=false,nearGood=false;
                foreach(int n in group)if(Near(s,g,n,reach)){nearGroup=true;break;}
                if(!nearGroup)continue;
                int x0=g.CellOf(s.X0-reach,s.Z0-reach),x1=g.CellOf(s.X1+reach,s.Z1+reach);
                if(x0>=0&&x1>=0)for(int cz=x0/g.W;cz<=x1/g.W&&!nearGood;cz++)for(int cx=x0%g.W;cx<=x1%g.W&&!nearGood;cx++){
                    int cell=cz*g.W+cx;for(int m=g.CellStart[cell];m<g.CellStart[cell+1];m++)if(good[m]&&Near(s,g,m,reach)){nearGood=true;break;}}
                if(nearGood)blockers.Add(s);
            }
            if(blockers.Count>0){
                foreach(var s in blockers)c.Culprits.Add(s);
                int b0=group[0];
                if(c.Notes.Count<30)c.Notes.Add($"clase {g.Class.Index}: {what} de {group.Count} nodos en ({g.NodeX(b0):F1},{g.NodeY[b0]:F2},{g.NodeZ(b0):F1}) → paso cerrado por {string.Join(", ",blockers.ConvertAll(s=>s.Kind.ToString()))}");
                return;
            }
            var counts=new Dictionary<ProceduralStructure,int>();
            foreach(int n in group){var s=Owner(g.NodeX(n),g.NodeZ(n),g.NodeY[n],2);if(s!=null)counts[s]=counts.TryGetValue(s,out int v)?v+1:1;}
            ProceduralStructure worst=null;int most=0;foreach(var kv in counts)if(kv.Value>most){most=kv.Value;worst=kv.Key;}
            int n0=group[0];
            if(worst!=null)c.Culprits.Add(worst);else c.Unattributed++;
            if(c.Notes.Count<30)c.Notes.Add($"clase {g.Class.Index}: {what} de {group.Count} nodos en ({g.NodeX(n0):F1},{g.NodeY[n0]:F2},{g.NodeZ(n0):F1}) → {(worst!=null?worst.Kind.ToString():"terreno")}");
        }

        static bool Touchable(NavGraph g,List<int> group,bool[] to,bool[] back)
        {
            if(group.Count>TinyGroup)return false;
            double reach=g.Class.Radius+Tuning.PlayerRadius,height=1.6+(g.Class.Height>3?g.Class.Height*.5:0);int span=(int)Math.Ceiling(reach/NavGraph.Cell);
            foreach(int n in group){
                bool ok=false;int c=g.NodeCell[n],cx=c%g.W,cz=c/g.W;
                for(int dz=-span;dz<=span&&!ok;dz++)for(int dx=-span;dx<=span&&!ok;dx++){
                    int ix=cx+dx,iz=cz+dz;if(ix<0||iz<0||ix>=g.W||iz>=g.H)continue;int cell=iz*g.W+ix;
                    if((dx*dx+dz*dz)*NavGraph.Cell*NavGraph.Cell>reach*reach)continue;
                    for(int m=g.CellStart[cell];m<g.CellStart[cell+1];m++)if(to[m]&&back[m]&&Math.Abs(g.NodeY[m]-g.NodeY[n])<height){ok=true;break;}
                }
                if(!ok)return false;
            }
            return true;
        }
        static bool Near(ProceduralStructure s,NavGraph g,int n,double reach)
        {
            double x=g.NodeX(n),z=g.NodeZ(n),y=g.NodeY[n];
            if(y<s.BaseY-1||y>s.BaseY+8)return false;
            double dx=Math.Max(0,Math.Max(s.X0-x,x-s.X1)),dz=Math.Max(0,Math.Max(s.Z0-z,z-s.Z1));
            return dx*dx+dz*dz<=reach*reach;
        }
        public ProceduralCheck Validate()
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();var c=new ProceduralCheck();
            var toS=default(bool[]);var backS=default(bool[]);
            for(int k=0;k<3;k++){
                var g=Graphs[k];int spawn=SpawnNode(k);var to=Reach(g,spawn);var back=Reach(g,spawn,true);
                if(k==0){toS=to;backS=back;}
                var lost=new bool[g.NodeCount];var pocket=new bool[g.NodeCount];var good=new bool[g.NodeCount];
                for(int n=0;n<g.NodeCount;n++)good[n]=to[n]&&back[n];
                for(int n=0;n<g.NodeCount;n++){if(!to[n])lost[n]=true;else if(!back[n])pocket[n]=true;}
                foreach(var group in Groups(g,lost))if(Touchable(g,group,to,back)){c.Tolerated+=group.Count;foreach(int n in group)lost[n]=false;}
                foreach(var group in Groups(g,pocket))if(Touchable(g,group,to,back)){c.Tolerated+=group.Count;foreach(int n in group)pocket[n]=false;}
                for(int n=0;n<g.NodeCount;n++){if(lost[n])c.Lost[k]++;if(pocket[n])c.Pockets[k]++;}
                foreach(var group in Groups(g,lost)){
                    c.LargestLost[k]=Math.Max(c.LargestLost[k],group.Count);
                    bool ground=false;if(k==2)foreach(int n in group)if(g.NodeTerrain[n]&&Math.Abs(g.NodeY[n])<.01){ground=true;break;}
                    if(k==2&&ground)c.GroundLostL+=group.Count;
                    // Mismo criterio que Passed: solo los grupos que hacen fallar el mapa tienen culpables.
                    bool fail=k==0||(k==1&&group.Count>MaxLostNodesM)||(k==2&&group.Count>MaxLostNodesL);
                    if(fail)Blame(c,g,group,"inaccesible",good);
                }
                foreach(var group in Groups(g,pocket))Blame(c,g,group,"bolsa",good);
            }
            // Tapas escalables descubiertas donde cabe el jugador (> 0,7 m): necesitan nodos S con salida.
            var gS=Graphs[0];
            foreach(var st in World.Structures){
                if(st.Removed)continue;
                foreach(var s in st.Solids){
                    if(!s.Climbable||Math.Min(s.Max.X-s.Min.X,s.Max.Z-s.Min.Z)<=.7+1e-9)continue;
                    bool covered=false;foreach(var o in st.Solids)if(o!=s&&Math.Abs(o.Min.Y-s.Max.Y)<1e-6&&o.Min.X<=s.Min.X+1e-6&&o.Max.X>=s.Max.X-1e-6&&o.Min.Z<=s.Min.Z+1e-6&&o.Max.Z>=s.Max.Z-1e-6)covered=true;
                    if(covered)continue;
                    bool ok=false;
                    for(int n=0;n<gS.NodeCount&&!ok;n++){double x=gS.NodeX(n),z=gS.NodeZ(n);
                        if(x>=s.Min.X&&x<=s.Max.X&&z>=s.Min.Z&&z<=s.Max.Z&&Math.Abs(gS.NodeY[n]-s.Max.Y)<.01&&toS[n]&&backS[n])ok=true;}
                    if(!ok){c.TopsWithoutAccess++;c.Culprits.Add(st);if(c.Notes.Count<30)c.Notes.Add($"tapa de {st.Kind} a {s.Max.Y:F2} m sin nodos S con salida");}
                }
            }
            // Marcadores: un nodo S alcanzable y con salida a menos de 1,5 m de su volumen, a su altura.
            foreach(var st in World.Structures){
                if(st.Removed||!ProceduralWorld.IsMarker(st.Kind))continue;var s=st.Solids[0];
                int near=-1;double cx=(s.Min.X+s.Max.X)/2,cz=(s.Min.Z+s.Max.Z)/2;
                for(int n=0;n<gS.NodeCount&&near<0;n++){double x=gS.NodeX(n),z=gS.NodeZ(n);
                    double dx=Math.Max(0,Math.Max(s.Min.X-x,x-s.Max.X)),dz=Math.Max(0,Math.Max(s.Min.Z-z,z-s.Max.Z));
                    if(dx*dx+dz*dz<=2.25&&Math.Abs(gS.SurfaceY(n,x,z)-s.Min.Y)<.3&&toS[n]&&backS[n])near=n;}
                if(near<0){c.MarkersWithoutAccess++;c.Unattributed++;if(c.Notes.Count<30)c.Notes.Add($"marcador {st.Kind} en ({cx:F1},{cz:F1}) sin acceso");}
            }
            ValidationMs+=watch.Elapsed.TotalMilliseconds;
            return c;
        }
    }
}
