using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // H1c: índice de diagnóstico sobre el grafo aprobado, no adaptador de producción.
    internal sealed class NavigationProjectionProbe
    {
        internal readonly int[] Owner, Component, Start, To, Global;
        internal readonly int Count;
        internal long Payload => 4L*(Owner.Length+Component.Length+Start.Length+To.Length+Global.Length);

        internal NavigationProjectionProbe(NavGraph g,int size)
        {
            Owner=new int[g.NodeCount];int columns=(int)Math.Ceiling(g.Terrain.Size/size);
            for(int n=0;n<g.NodeCount;n++)Owner[n]=(int)((g.NodeZ(n)+g.Terrain.Half)/size)*columns+
                (int)((g.NodeX(n)+g.Terrain.Half)/size);
            // Reutiliza el kernel SCC existente sin modificar su visibilidad ni producción.
            var method=typeof(NavPortalWorld).GetMethod("StrongComponents",BindingFlags.NonPublic|BindingFlags.Static);
            if(method==null)throw new InvalidOperationException("Kernel SCC no encontrado");
            var args=new object[]{g.NodeCount,g.EdgeStart,g.EdgeTo,Owner,0};
            Component=(int[])method.Invoke(null,args);Count=(int)args[4];
            var arcs=new HashSet<long>();
            for(int e=0;e<g.EdgeCount;e++){
                int a=Component[g.EdgeFrom[e]],b=Component[g.EdgeTo[e]];
                if(a!=b)arcs.Add(((long)a<<32)|(uint)b);
            }
            var ordered=arcs.OrderBy(x=>x).ToArray();Start=new int[Count+1];To=new int[ordered.Length];
            for(int i=0;i<ordered.Length;i++){Start[(int)(ordered[i]>>32)+1]++;To[i]=(int)ordered[i];}
            for(int i=1;i<=Count;i++)Start[i]+=Start[i-1];
            args=new object[]{Count,Start,To,null,0};Global=(int[])method.Invoke(null,args);
        }

        internal void Verify(NavPortalWorld w,int[] inverse)
        {
            if(Count!=w.ComponentCount)throw new InvalidOperationException("Partición SCC diferente");
            var forward=Enumerable.Repeat(-1,Count).ToArray();var reverse=Enumerable.Repeat(-1,Count).ToArray();
            for(int n=0;n<w.NodeCount;n++){
                int p=Component[inverse[n]],c=w.Component[n];
                if(Owner[inverse[n]]!=w.NodeRegion[n] || (forward[p]>=0&&forward[p]!=c) ||
                    (reverse[c]>=0&&reverse[c]!=p))throw new InvalidOperationException("Propietario/SCC diferente");
                forward[p]=c;reverse[c]=p;
            }
            var arcs=new HashSet<long>();
            for(int a=0;a<Count;a++)for(int e=Start[a];e<Start[a+1];e++)arcs.Add(((long)forward[a]<<32)|(uint)forward[To[e]]);
            if(arcs.Count!=w.ComponentTo.Length)throw new InvalidOperationException("Arcos SCC diferentes");
            for(int a=0;a<Count;a++)for(int e=w.ComponentStart[a];e<w.ComponentStart[a+1];e++)
                if(!arcs.Contains(((long)a<<32)|(uint)w.ComponentTo[e]))throw new InvalidOperationException("Arco SCC perdido");
            // La equivalencia de arcos y SCC implica alcance macro idéntico; no necesita matriz C².
        }
    }

    // Sonda estática para comparar un prefijo antiguo con un refinamiento compartido.
    // No servicio, versiones, caché móvil, scheduler ni consumidor de juego.
    internal sealed class LocalRefinementProbe
    {
        readonly NavPortalWorld w;
        internal readonly int[] Distance, Parent;
        readonly int[] start, edges, heap, at;
        readonly bool[] domain;
        int count;
        internal long InitWork, PhysicalWork, HeapWork, RouteWork, IndexWork;
        internal long Extractions, ExaminedEdges, RelaxationAttempts, Improvements;
        internal int DomainNodes;
        internal long Work => InitWork+PhysicalWork;
        internal long Payload => 4L*(Distance.Length+Parent.Length+start.Length+edges.Length+heap.Length+at.Length)+domain.Length;
        internal LocalRefinementProbe(NavPortalWorld world)
        {
            w=world;int n=w.NodeCount;
            Distance=new int[n];Parent=new int[n];domain=new bool[n];heap=new int[n];at=new int[n];
            start=new int[n+1];edges=new int[w.EdgeCount];
            for(int e=0;e<w.EdgeCount;e++){start[w.EdgeTo[e]+1]++;IndexWork++;}
            for(int i=1;i<=n;i++){start[i]+=start[i-1];IndexWork++;}
            var fill=(int[])start.Clone();IndexWork+=fill.Length;
            for(int e=0;e<w.EdgeCount;e++){edges[fill[w.EdgeTo[e]]++]=e;IndexWork++;}
        }
        static double Squared(Vec3 a,Vec3 b){double x=a.X-b.X,y=a.Y-b.Y,z=a.Z-b.Z;return x*x+y*y+z*z;}
        internal void Build(int oldGoal,int goal)
        {
            if(w.Component[oldGoal]!=w.Component[goal])throw new ArgumentException("Cambio de SCC: reutilización no autorizada por esta sonda");
            InitWork=PhysicalWork=HeapWork=RouteWork=0;DomainNodes=0;count=0;
            Extractions=ExaminedEdges=RelaxationAttempts=Improvements=0;
            var oldPosition=w.Position(oldGoal);var position=w.Position(goal);int target=w.Component[goal];
            for(int n=0;n<w.NodeCount;n++){
                var p=w.Position(n);domain[n]=w.Component[n]==target||Squared(p,position)<=32*32||Squared(p,oldPosition)<=32*32;
                if(domain[n])DomainNodes++;Distance[n]=int.MaxValue;Parent[n]=-1;at[n]=-1;InitWork++;
            }
            Distance[goal]=0;Push(goal);
            while(count>0){int b=Pop();PhysicalWork++;Extractions++;
                for(int k=start[b];k<start[b+1];k++){
                    int e=edges[k],a=w.EdgeFrom[e];PhysicalWork++;ExaminedEdges++;if(!domain[a])continue;
                    int d=checked(Distance[b]+w.EdgeCost[e]);PhysicalWork++;RelaxationAttempts++;
                    if(d>=Distance[a])continue;Distance[a]=d;Parent[a]=e;Improvements++;Push(a);
                }
            }
            if(Distance[oldGoal]==int.MaxValue)throw new InvalidOperationException("SCC interna sin camino entre metas");
        }
        internal bool Route(AdaptiveCorridorProbe previous,int source,int goal,List<int> path)
        {
            path.Clear();RouteWork++;
            if(previous.Distance[source]==int.MaxValue&&Distance[source]==int.MaxValue)return false;
            bool joined=false;
            for(int n=source;n!=goal;){
                if(path.Count>=w.NodeCount)throw new InvalidOperationException("Ciclo de unión");
                if(Distance[n]!=int.MaxValue)joined=true;
                int e=joined?Parent[n]:previous.Parent[n];
                if(e<0||w.EdgeFrom[e]!=n)throw new InvalidOperationException("Unión sin arista física");
                path.Add(e);n=w.EdgeTo[e];RouteWork++;
            }
            return true;
        }
        bool Before(int a,int b){HeapWork++;return Distance[a]!=Distance[b]?Distance[a]<Distance[b]:a<b;}
        void Push(int node){HeapWork++;int i=at[node];if(i<0)i=count++;
            while(i>0){int p=(i-1)/2;if(!Before(node,heap[p]))break;heap[i]=heap[p];at[heap[i]]=i;i=p;HeapWork++;}
            heap[i]=node;at[node]=i;}
        int Pop(){HeapWork++;int result=heap[0],last=heap[--count];at[result]=-1;if(count==0)return result;
            int i=0;while(2*i+1<count){int c=2*i+1;if(c+1<count&&Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],last))break;heap[i]=heap[c];at[heap[i]]=i;i=c;HeapWork++;}
            heap[i]=last;at[last]=i;return result;}
    }
}
