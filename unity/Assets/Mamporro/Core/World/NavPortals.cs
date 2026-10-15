using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Identidad física exacta: (instancia mundial, región origen, arista local).
    // La arista conserva superficie/capa, envolvente de clase y trayectoria D1 completa.
    public readonly struct NavPortal
    {
        public readonly int FromRegion,ToRegion,From,To,LocalFrom,LocalTo,LocalEdge,Cost,Path,ClassIndex;
        public readonly NavEdgeKind Kind;
        public NavPortal(int fromRegion,int toRegion,int from,int to,int localFrom,int localTo,int edge,NavGraph g)
        {FromRegion=fromRegion;ToRegion=toRegion;From=from;To=to;LocalFrom=localFrom;LocalTo=localTo;LocalEdge=edge;
         Cost=g.EdgeCost[edge];Path=g.EdgePath[edge];ClassIndex=g.Class.Index;Kind=(NavEdgeKind)g.EdgeKind[edge];}
    }

    // D2 optativo: unión exacta de D1 y condensación dirigida de sus SCC internas.
    // Sin campos de movimiento, fallback ni planificación por tick. Inmutable tras construir;
    // cada consumidor usa su propio NavPortalQuery (búferes no compartidos entre hilos).
    public sealed class NavPortalWorld
    {
        public readonly NavRegionGraph[] Regions;
        public readonly NavClass Class;
        public readonly int Size,Columns,NodeCount,EdgeCount,ComponentCount,WeakCount,GlobalStrongCount;
        public readonly int[] NodeRegion,NodeLocal,EdgeStart,EdgeFrom,EdgeTo,EdgeLocal,EdgeCost;
        public readonly int[] Component,WeakComponent,ComponentRegion,ComponentStart,ComponentTo,GlobalStrong;
        public readonly NavPortal[] Portals;
        public readonly double BuildMs,TopologyMs;
        readonly long[] keys;
        readonly int[] keyNodes;

        public static long NodeKey(NavGraph g,int n)
        {
            int width=(int)Math.Round(g.Terrain.Size/NavGraph.Cell);
            int x=(int)Math.Floor((g.NodeX(n)+g.Terrain.Half)/NavGraph.Cell);
            int z=(int)Math.Floor((g.NodeZ(n)+g.Terrain.Half)/NavGraph.Cell);
            return ((long)(z*width+x)<<32)|(uint)(n-g.CellStart[g.NodeCell[n]]);
        }
        public int Find(long key){int at=Array.BinarySearch(keys,key);return at<0?-1:keyNodes[at];}
        public NavGraph Graph(int node)=>Regions[NodeRegion[node]].Graph;
        public Vec3 Position(int node){var g=Graph(node);int n=NodeLocal[node];return new Vec3(g.NodeX(n),g.NodeY[n],g.NodeZ(n));}
        public int NodeAt(double x,double y,double z,double tolerance=.1)
        {
            var terrain=Regions[0].Graph.Terrain;
            if(x< -terrain.Half||z< -terrain.Half||x>=terrain.Half||z>=terrain.Half)return -1;
            int r=(int)((z+terrain.Half)/Size)*Columns+(int)((x+terrain.Half)/Size);
            var g=Regions[r].Graph;int local=g.NodeAt(x,y,z,tolerance);
            return local<0||!g.Owns(local)?-1:Find(NodeKey(g,local));
        }
        public NavPortalWorld(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,NavClass body,int size)
        {
            if(size!=16&&size!=32&&size!=64)throw new ArgumentOutOfRangeException(nameof(size));
            var watch=System.Diagnostics.Stopwatch.StartNew();Class=body;Size=size;Columns=(int)Math.Ceiling(terrain.Size/size);
            Regions=new NavRegionGraph[Columns*Columns];
            for(int r=0;r<Regions.Length;r++){
                Regions[r]=new NavRegionGraph(queries,terrain,inside,body,size,r%Columns,r/Columns);
                NodeCount+=Regions[r].OwnedNodes;EdgeCount+=Regions[r].Graph.EdgeCount;
            }
            double physicalMs=watch.Elapsed.TotalMilliseconds;
            NodeRegion=new int[NodeCount];NodeLocal=new int[NodeCount];keys=new long[NodeCount];keyNodes=new int[NodeCount];
            int cursor=0;
            for(int r=0;r<Regions.Length;r++){var g=Regions[r].Graph;for(int n=0;n<g.NodeCount;n++)if(g.Owns(n)){
                NodeRegion[cursor]=r;NodeLocal[cursor]=n;keys[cursor]=NodeKey(g,n);keyNodes[cursor]=cursor;cursor++;
            }}
            Array.Sort(keys,keyNodes);
            for(int n=1;n<NodeCount;n++)if(keys[n]==keys[n-1])throw new InvalidOperationException("Propietario duplicado");
            EdgeStart=new int[NodeCount+1];EdgeFrom=new int[EdgeCount];EdgeTo=new int[EdgeCount];EdgeLocal=new int[EdgeCount];EdgeCost=new int[EdgeCount];
            var portals=new List<NavPortal>();cursor=0;
            for(int a=0;a<NodeCount;a++){
                EdgeStart[a]=cursor;int r=NodeRegion[a],n=NodeLocal[a];var g=Regions[r].Graph;
                for(int e=g.EdgeStart[n];e<g.EdgeStart[n+1];e++){
                    int b=Find(NodeKey(g,g.EdgeTo[e]));
                    if(b<0)throw new InvalidOperationException("Destino físico sin propietario");
                    var target=Graph(b);int bn=NodeLocal[b];
                    if(g.NodeY[g.EdgeTo[e]]!=target.NodeY[bn]||g.NodeTerrain[g.EdgeTo[e]]!=target.NodeTerrain[bn])throw new InvalidOperationException("Capas de halo incompatibles");
                    EdgeFrom[cursor]=a;EdgeTo[cursor]=b;EdgeLocal[cursor]=e;EdgeCost[cursor]=g.EdgeCost[e];cursor++;
                    if(r!=NodeRegion[b])portals.Add(new NavPortal(r,NodeRegion[b],a,b,n,bn,e,g));
                }
            }
            EdgeStart[NodeCount]=cursor;Portals=portals.ToArray();
            Component=StrongComponents(NodeCount,EdgeStart,EdgeTo,NodeRegion,out int count);ComponentCount=count;
            // Conectividad débil interna: útil para diagnosticar una caída sin retorno, nunca para afirmar alcance.
            var parent=new int[NodeCount];for(int n=0;n<NodeCount;n++)parent[n]=n;
            for(int e=0;e<EdgeCount;e++){int a=EdgeFrom[e],b=EdgeTo[e];if(NodeRegion[a]==NodeRegion[b])parent[Root(parent,b)]=Root(parent,a);}
            WeakComponent=new int[NodeCount];var weakIds=new Dictionary<int,int>();
            for(int n=0;n<NodeCount;n++){int root=Root(parent,n);if(!weakIds.TryGetValue(root,out int id)){id=weakIds.Count;weakIds.Add(root,id);}WeakComponent[n]=id;}WeakCount=weakIds.Count;
            ComponentRegion=new int[count];for(int n=0;n<NodeCount;n++)ComponentRegion[Component[n]]=NodeRegion[n];
            // Solo deduplicar arcos de alcance entre SCC; los portales físicos NO se fusionan.
            var arcs=new HashSet<long>();
            for(int e=0;e<EdgeCount;e++){int a=Component[EdgeFrom[e]],b=Component[EdgeTo[e]];if(a!=b)arcs.Add(((long)a<<32)|(uint)b);}
            var ordered=new long[arcs.Count];arcs.CopyTo(ordered);Array.Sort(ordered);
            ComponentStart=new int[count+1];ComponentTo=new int[ordered.Length];
            for(int e=0;e<ordered.Length;e++){ComponentStart[(int)(ordered[e]>>32)+1]++;ComponentTo[e]=(int)ordered[e];}
            for(int n=0;n<count;n++)ComponentStart[n+1]+=ComponentStart[n];
            GlobalStrong=StrongComponents(count,ComponentStart,ComponentTo,null,out int global);GlobalStrongCount=global;
            BuildMs=watch.Elapsed.TotalMilliseconds;TopologyMs=BuildMs-physicalMs;
        }
        static int Root(int[] p,int n){while(p[n]!=n){p[n]=p[p[n]];n=p[n];}return n;}

        // Kosaraju iterativo O(V+E), sin recursión ni matriz de parejas. group limita SCC a cada región.
        internal static int[] StrongComponents(int count,int[] start,int[] to,int[] group,out int components)
        {
            var seen=new bool[count];var stack=new int[count];var next=new int[count];var order=new int[count];int used=0;
            for(int seed=0;seed<count;seed++)if(!seen[seed]){
                int top=0;stack[0]=seed;next[0]=start[seed];seen[seed]=true;
                while(top>=0){int a=stack[top],e=next[top];
                    if(e==start[a+1]){order[used++]=a;top--;continue;}
                    next[top]++;int b=to[e];if(seen[b]||(group!=null&&group[a]!=group[b]))continue;
                    seen[b]=true;stack[++top]=b;next[top]=start[b];
                }
            }
            var reverseStart=new int[count+1];
            for(int a=0;a<count;a++)for(int e=start[a];e<start[a+1];e++)if(group==null||group[a]==group[to[e]])reverseStart[to[e]+1]++;
            for(int a=0;a<count;a++)reverseStart[a+1]+=reverseStart[a];
            var reverse=new int[reverseStart[count]];var fill=(int[])reverseStart.Clone();
            for(int a=0;a<count;a++)for(int e=start[a];e<start[a+1];e++)if(group==null||group[a]==group[to[e]])reverse[fill[to[e]]++]=a;
            var result=new int[count];for(int a=0;a<count;a++)result[a]=-1;components=0;
            for(int i=used-1;i>=0;i--)if(result[order[i]]<0){
                int top=0;stack[0]=order[i];result[order[i]]=components;
                while(top>=0){int a=stack[top--];for(int e=reverseStart[a];e<reverseStart[a+1];e++){int b=reverse[e];if(result[b]>=0)continue;result[b]=components;stack[++top]=b;}}
                components++;
            }
            return result;
        }
    }

    // Consultas offline D2. Caché acotada a un origen por instancia: sin tabla cuadrática.
    // Invalidación por instancia mundial inmutable; nunca reutilizar con otra geometría/clase.
    public sealed class NavPortalQuery
    {
        public readonly NavPortalWorld World;
        readonly bool[] reached;
        readonly int[] queue,dist,parent,heap,heapAt;
        int reachSource=-1,routeSource=-1,heapCount;
        public int RouteBuilds {get;private set;}
        public NavPortalQuery(NavPortalWorld world)
        {World=world;reached=new bool[world.ComponentCount];queue=new int[reached.Length];
         int n=world.NodeCount;dist=new int[n];parent=new int[n];heap=new int[n];heapAt=new int[n];}
        bool Valid(int n)=>n>=0&&n<World.NodeCount;
        public bool Reachable(int from,int to)
        {
            if(!Valid(from)||!Valid(to))return false;
            int source=World.Component[from],target=World.Component[to];
            if(source!=reachSource){
                Array.Clear(reached,0,reached.Length);int head=0,tail=0;queue[tail++]=source;reached[source]=true;
                while(head<tail){int a=queue[head++];for(int e=World.ComponentStart[a];e<World.ComponentStart[a+1];e++){
                    int b=World.ComponentTo[e];if(reached[b])continue;reached[b]=true;queue[tail++]=b;
                }}reachSource=source;
            }
            return reached[target];
        }
        public int Distance(int from,int to)
        {if(!Reachable(from,to))return int.MaxValue;BuildRoutes(from);return dist[to];}
        // Salida en aristas globales: EdgeLocal + región origen recuperan la trayectoria física D1.
        // El llamador puede reservar NodeCount para garantizar cero asignaciones durante la consulta.
        public bool Route(int from,int to,List<int> edges)
        {
            edges.Clear();if(!Reachable(from,to))return false;BuildRoutes(from);
            for(int n=to;n!=from;n=World.EdgeFrom[parent[n]]){if(parent[n]<0)throw new InvalidOperationException("Alcance sin ruta física");edges.Add(parent[n]);}
            edges.Reverse();return true;
        }
        void BuildRoutes(int source)
        {
            if(routeSource==source)return;
            for(int n=0;n<dist.Length;n++){dist[n]=int.MaxValue;parent[n]=-1;heapAt[n]=-1;}
            heapCount=0;dist[source]=0;PushOrDecrease(source);
            while(heapCount>0){int a=Pop();for(int e=World.EdgeStart[a];e<World.EdgeStart[a+1];e++){
                int b=World.EdgeTo[e],cost=checked(dist[a]+World.EdgeCost[e]);if(cost>=dist[b])continue;
                dist[b]=cost;parent[b]=e;PushOrDecrease(b);
            }}routeSource=source;RouteBuilds++;
        }
        bool Before(int a,int b)=>dist[a]<dist[b]||(dist[a]==dist[b]&&a<b);
        void PushOrDecrease(int n)
        {
            int i=heapAt[n];if(i<0)i=heapCount++;
            while(i>0){int p=(i-1)/2;if(!Before(n,heap[p]))break;heap[i]=heap[p];heapAt[heap[i]]=i;i=p;}
            heap[i]=n;heapAt[n]=i;
        }
        int Pop()
        {
            int result=heap[0],last=heap[--heapCount];heapAt[result]=-1;if(heapCount==0)return result;
            int i=0;while(2*i+1<heapCount){int c=2*i+1;if(c+1<heapCount&&Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],last))break;heap[i]=heap[c];heapAt[heap[i]]=i;i=c;}
            heap[i]=last;heapAt[last]=i;return result;
        }
    }
}
