using System;
using System.Collections.Generic;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // Diagnóstico offline H1. No scheduler, caché de servicio ni consumidor de juego.
    internal sealed class HybridCorridorProbe
    {
        internal readonly NavPortalWorld World;
        internal readonly int[] First, Second, Rank, Next, Distance, Parent;
        readonly int[] arcFrom, edgeArc, reverseStart, reverseEdges, queue, heap, at;
        int heapCount;
        internal long Work;
        internal long Payload => 4L * (First.Length + Second.Length + Rank.Length + Next.Length +
            Distance.Length + Parent.Length + arcFrom.Length + edgeArc.Length + reverseStart.Length +
            reverseEdges.Length + queue.Length + heap.Length + at.Length);

        internal HybridCorridorProbe(NavPortalWorld w)
        {
            World = w; int n = w.NodeCount, c = w.ComponentCount, a = w.ComponentTo.Length;
            First = new int[a]; Second = new int[a]; arcFrom = new int[a]; edgeArc = new int[w.EdgeCount];
            Rank = new int[c]; Next = new int[c]; queue = new int[c];
            Distance = new int[n]; Parent = new int[n]; heap = new int[n]; at = new int[n];
            reverseStart = new int[n + 1]; reverseEdges = new int[w.EdgeCount];
            var members = new List<int>[a];
            for (int s = 0; s < c; s++) for (int j = w.ComponentStart[s]; j < w.ComponentStart[s + 1]; j++)
            { arcFrom[j] = s; members[j] = new List<int>(); }
            for (int e = 0; e < w.EdgeCount; e++)
            {
                int s = w.Component[w.EdgeFrom[e]], t = w.Component[w.EdgeTo[e]];
                int j = s == t ? -1 : Array.BinarySearch(w.ComponentTo, w.ComponentStart[s],
                    w.ComponentStart[s + 1] - w.ComponentStart[s], t);
                if (s != t && j < 0) throw new InvalidOperationException("Conexión SCC perdida");
                edgeArc[e] = j; if (j >= 0) members[j].Add(e);
                reverseStart[w.EdgeTo[e] + 1]++;
            }
            for (int i = 1; i <= n; i++) reverseStart[i] += reverseStart[i - 1];
            var fill = (int[])reverseStart.Clone();
            for (int e = 0; e < w.EdgeCount; e++) reverseEdges[fill[w.EdgeTo[e]]++] = e;
            for (int j = 0; j < a; j++)
            {
                var list = members[j]; if (list.Count == 0) throw new InvalidOperationException("Arco sin testigo");
                First[j] = list[list.Count / 2]; Second[j] = First[j];
                var p = w.Position(w.EdgeFrom[First[j]]); double farthest = -1;
                foreach (int e in list)
                {
                    var q = w.Position(w.EdgeFrom[e]); double dx = p.X-q.X, dy = p.Y-q.Y, dz = p.Z-q.Z;
                    double d = dx*dx+dy*dy+dz*dz;
                    if (d > farthest) { farthest = d; Second[j] = e; }
                }
            }
        }

        internal void Build(int goal, int witnesses)
        {
            var w = World; Work = 0; heapCount = 0;
            for (int i = 0; i < Rank.Length; i++) { Rank[i] = -1; Next[i] = -1; Work++; }
            int target = w.Component[goal], head = 0, tail = 0;
            Rank[target] = 0; queue[tail++] = target;
            while (head < tail)
            {
                int b = queue[head++]; Work++;
                // Barrido explícito contado: diagnóstico pequeño, no coste oculto de scheduler.
                for (int j = 0; j < arcFrom.Length; j++)
                {
                    Work++; if (w.ComponentTo[j] != b) continue; int a = arcFrom[j];
                    if (Rank[a] >= 0) continue;
                    Rank[a] = Rank[b] + 1; Next[a] = j; queue[tail++] = a;
                }
            }
            for (int i = 0; i < Distance.Length; i++) { Distance[i] = int.MaxValue; Parent[i] = -1; at[i] = -1; Work++; }
            Distance[goal] = 0; Push(goal);
            while (heapCount > 0)
            {
                int b = Pop(); Work++;
                for (int k = reverseStart[b]; k < reverseStart[b + 1]; k++)
                {
                    int e = reverseEdges[k], a = w.EdgeFrom[e], j = edgeArc[e]; Work++;
                    if (j >= 0 && (Next[w.Component[a]] != j ||
                        (e != First[j] && (witnesses != 2 || e != Second[j])))) continue;
                    int d = checked(Distance[b] + w.EdgeCost[e]); Work++;
                    if (d >= Distance[a]) continue;
                    Distance[a] = d; Parent[a] = e; Push(a);
                }
            }
        }

        internal bool Route(int source, int goal, List<int> result)
        {
            result.Clear(); if (Distance[source] == int.MaxValue) return false;
            for (int n = source; n != goal;)
            {
                if (result.Count >= World.NodeCount) throw new InvalidOperationException("Ciclo");
                int e = Parent[n];
                if (e < 0 || World.EdgeFrom[e] != n) throw new InvalidOperationException("Padre inválido");
                result.Add(e); n = World.EdgeTo[e];
            }
            return true;
        }
        bool Before(int a, int b) => Distance[a] != Distance[b] ? Distance[a] < Distance[b] : a < b;
        void Push(int node)
        {
            int i = at[node]; if (i < 0) i = heapCount++;
            while (i > 0) { int p = (i-1)/2; if (!Before(node,heap[p])) break; heap[i]=heap[p]; at[heap[i]]=i; i=p; }
            heap[i]=node; at[node]=i;
        }
        int Pop()
        {
            int result=heap[0], last=heap[--heapCount]; at[result]=-1; if(heapCount==0)return result;
            int i=0; while(2*i+1<heapCount) { int c=2*i+1;
                if(c+1<heapCount && Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],last))break;heap[i]=heap[c];at[heap[i]]=i;i=c;
            }
            heap[i]=last;at[last]=i;return result;
        }
    }
}
