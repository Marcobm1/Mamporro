using System;
using System.Collections.Generic;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // H1d: vista inmutable de tests. Conserva el grafo validado y sus trayectorias;
    // nunca consulta geometría ni construye nodos, escaladas o caídas de nuevo.
    internal sealed class H1eMeasuredWorld
    {
        internal readonly NavGraph Source;
        public readonly NavClass Class;
        public readonly int Size,Columns,NodeCount,EdgeCount,ComponentCount,WeakCount,GlobalStrongCount;
        public readonly int[] NodeRegion,NodeLocal,EdgeStart,EdgeFrom,EdgeTo,EdgeLocal,EdgeCost;
        public readonly int[] Component,WeakComponent,ComponentRegion,ComponentStart,ComponentTo,GlobalStrong;
        public readonly NavPortal[] Portals;
        public readonly double BuildMs,TopologyMs;
        readonly long[] keys;
        readonly int[] keyNodes;
        public static long NodeKey(NavGraph g,int n)=>NavPortalWorld.NodeKey(g,n);
        public int Find(long key){int at=Array.BinarySearch(keys,key);return at<0?-1:keyNodes[at];}
        public NavGraph Graph(int node)=>Source;
        public Vec3 Position(int node){int n=NodeLocal[node];return new Vec3(Source.NodeX(n),Source.NodeY[n],Source.NodeZ(n));}
        public int NodeAt(double x,double y,double z,double tolerance=.1)
        {int n=Source.NodeAt(x,y,z,tolerance);return n<0?-1:Find(NodeKey(Source,n));}
        internal H1eMeasuredWorld(NavGraph source,int size)
        {
            if(size!=16&&size!=32&&size!=64)throw new ArgumentOutOfRangeException(nameof(size));
            H1eMemoryTrace.Begin("projectionAndPortals");var watch=System.Diagnostics.Stopwatch.StartNew();Source=source;Class=source.Class;Size=size;
            Columns=(int)Math.Ceiling(source.Terrain.Size/size);NodeCount=source.NodeCount;EdgeCount=source.EdgeCount;
            NodeRegion=new int[NodeCount];NodeLocal=new int[NodeCount];keys=new long[NodeCount];keyNodes=new int[NodeCount];
            var owners=new int[NodeCount];var starts=new int[Columns*Columns+1];
            for(int n=0;n<NodeCount;n++){
                int r=(int)((source.NodeZ(n)+source.Terrain.Half)/size)*Columns+(int)((source.NodeX(n)+source.Terrain.Half)/size);
                if(r<0||r>=Columns*Columns)throw new InvalidOperationException("Propietario fuera del mundo");
                owners[n]=r;starts[r+1]++;
            }
            for(int r=1;r<starts.Length;r++)starts[r]+=starts[r-1];
            var fill=(int[])starts.Clone();var originalToView=new int[NodeCount];
            for(int n=0;n<NodeCount;n++){
                int r=owners[n],i=fill[r]++;NodeRegion[i]=r;NodeLocal[i]=n;originalToView[n]=i;
                keys[i]=NodeKey(source,n);keyNodes[i]=i;
            }
            Array.Sort(keys,keyNodes);
            for(int i=1;i<NodeCount;i++)if(keys[i]==keys[i-1])throw new InvalidOperationException("Identidad duplicada");
            EdgeStart=new int[NodeCount+1];EdgeFrom=new int[EdgeCount];EdgeTo=new int[EdgeCount];EdgeLocal=new int[EdgeCount];EdgeCost=new int[EdgeCount];
            var portals=new List<NavPortal>();int cursor=0;
            for(int a=0;a<NodeCount;a++){
                EdgeStart[a]=cursor;int n=NodeLocal[a],r=NodeRegion[a];
                for(int e=source.EdgeStart[n];e<source.EdgeStart[n+1];e++){
                    int bn=source.EdgeTo[e],b=originalToView[bn];
                    EdgeFrom[cursor]=a;EdgeTo[cursor]=b;EdgeLocal[cursor]=e;EdgeCost[cursor]=source.EdgeCost[e];cursor++;
                    if(r!=NodeRegion[b])portals.Add(new NavPortal(r,NodeRegion[b],a,b,n,bn,e,source));
                }
            }
            if(cursor!=EdgeCount)throw new InvalidOperationException("Aristas perdidas");
            EdgeStart[NodeCount]=cursor;Portals=portals.ToArray();double physicalMs=watch.Elapsed.TotalMilliseconds;
            H1eMemoryTrace.End();H1eMemoryTrace.Begin("SCC");Component=StrongComponents(NodeCount,EdgeStart,EdgeTo,NodeRegion,out int count);ComponentCount=count;
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
            BuildMs=watch.Elapsed.TotalMilliseconds;TopologyMs=BuildMs-physicalMs;H1eMemoryTrace.End();
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

}
