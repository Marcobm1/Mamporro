using System;
using System.Collections.Generic;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // H1b offline: un campo compartido por meta, nunca una búsqueda por enemigo.
    internal sealed class H1eMeasuredIndex
    {
        internal readonly H1eMeasuredWorld World;
        internal readonly int[] Distance, Parent, Potential, Next;
        readonly int[] arcFrom, weight, edgeArc, reverseStart, reverseEdges, macroStart, macroEdges;
        readonly int[] heap, at, queue;
        readonly bool[] local;
        int[] priority;
        int heapCount;
        internal long InitWork, MacroWork, PhysicalWork, HeapWork, RouteWork, SetupWork;
        internal long Work => InitWork + MacroWork + PhysicalWork;
        internal long Payload => 4L * (Distance.Length + Parent.Length + Potential.Length + Next.Length +
            arcFrom.Length + weight.Length + edgeArc.Length + reverseStart.Length + reverseEdges.Length +
            macroStart.Length + macroEdges.Length + heap.Length + at.Length + queue.Length) + local.Length;
        internal long SetupScratchPayload;

        internal H1eMeasuredIndex(H1eMeasuredWorld w)
        {
            World = w;
            int n = w.NodeCount, c = w.ComponentCount, a = w.ComponentTo.Length;
            Distance = new int[n]; Parent = new int[n]; Potential = new int[c]; Next = new int[c];
            arcFrom = new int[a]; weight = new int[a]; edgeArc = new int[w.EdgeCount];
            reverseStart = new int[n + 1]; reverseEdges = new int[w.EdgeCount];
            macroStart = new int[c + 1]; macroEdges = new int[a];
            heap = new int[n]; at = new int[n]; queue = new int[c]; local = new bool[n];
            var cx = new double[c]; var cy = new double[c]; var cz = new double[c]; var count = new int[c];
            for (int i = 0; i < n; i++)
            {
                int s = w.Component[i]; var p = w.Position(i);
                cx[s] += p.X; cy[s] += p.Y; cz[s] += p.Z; count[s]++; SetupWork++;
            }
            for (int s = 0; s < c; s++)
            {
                cx[s] /= count[s]; cy[s] /= count[s]; cz[s] /= count[s]; SetupWork++;
                for (int j = w.ComponentStart[s]; j < w.ComponentStart[s + 1]; j++)
                { arcFrom[j] = s; weight[j] = int.MaxValue; macroStart[w.ComponentTo[j] + 1]++; SetupWork++; }
            }
            for (int e = 0; e < w.EdgeCount; e++)
            {
                int s = w.Component[w.EdgeFrom[e]], t = w.Component[w.EdgeTo[e]];
                int j = s == t ? -1 : Array.BinarySearch(w.ComponentTo, w.ComponentStart[s],
                    w.ComponentStart[s + 1] - w.ComponentStart[s], t);
                if (s != t && j < 0) throw new InvalidOperationException("Conexión SCC perdida");
                edgeArc[e] = j; reverseStart[w.EdgeTo[e] + 1]++; SetupWork++;
                if (j < 0) continue;
                var p = w.Position(w.EdgeFrom[e]); var q = w.Position(w.EdgeTo[e]);
                int cost = checked(w.EdgeCost[e] + (int)Math.Ceiling(NavGraph.Scale *
                    (Length(p.X-cx[s], p.Y-cy[s], p.Z-cz[s]) + Length(q.X-cx[t], q.Y-cy[t], q.Z-cz[t]))));
                weight[j] = Math.Min(weight[j], Math.Max(1, cost));
            }
            for (int i = 1; i <= n; i++) { reverseStart[i] += reverseStart[i-1]; SetupWork++; }
            for (int i = 1; i <= c; i++) { macroStart[i] += macroStart[i-1]; SetupWork++; }
            var fill = (int[])reverseStart.Clone(); var macroFill = (int[])macroStart.Clone();
            for (int e = 0; e < w.EdgeCount; e++) { reverseEdges[fill[w.EdgeTo[e]]++] = e; SetupWork++; }
            for (int j = 0; j < a; j++) { macroEdges[macroFill[w.ComponentTo[j]]++] = j; SetupWork++; }
            SetupScratchPayload = 28L*c + 4L*(fill.Length + macroFill.Length);
        }

        static double Length(double x, double y, double z) => Math.Sqrt(x*x+y*y+z*z);

        internal void Build(int goal, int variant)
        {
            if (variant < 1 || variant > 3) throw new ArgumentOutOfRangeException(nameof(variant));
            var w = World;
            InitWork = MacroWork = PhysicalWork = HeapWork = RouteWork = 0;
            for (int s = 0; s < Potential.Length; s++) { Potential[s] = int.MaxValue; Next[s] = -1; at[s] = -1; InitWork++; }
            int target = w.Component[goal]; Potential[target] = 0; heapCount = 0;
            if (variant == 3)
            {
                priority = Potential; Push(target);
                while (heapCount > 0)
                {
                    int b = Pop(); MacroWork++;
                    for (int k = macroStart[b]; k < macroStart[b+1]; k++)
                    {
                        int j = macroEdges[k], a = arcFrom[j]; MacroWork++;
                        int d = checked(Potential[b] + weight[j]); MacroWork++;
                        if (d >= Potential[a]) continue;
                        Potential[a] = d; Next[a] = j; Push(a);
                    }
                }
            }
            else
            {
                int head = 0, tail = 0; queue[tail++] = target;
                while (head < tail)
                {
                    int b = queue[head++]; MacroWork++;
                    // Orden por origen igual al árbol H1; índice inverso evita barrer todos los arcos.
                    for (int k = macroStart[b]; k < macroStart[b+1]; k++)
                    {
                        int j = macroEdges[k], a = arcFrom[j]; MacroWork++;
                        if (Potential[a] != int.MaxValue) continue;
                        Potential[a] = Potential[b] + 1; Next[a] = j; queue[tail++] = a;
                    }
                }
            }
            var g = w.Position(goal);
            for (int n = 0; n < Distance.Length; n++)
            {
                Distance[n] = int.MaxValue; Parent[n] = -1; at[n] = -1;
                var p = w.Position(n); double dx = p.X-g.X, dy = p.Y-g.Y, dz = p.Z-g.Z;
                local[n] = variant >= 2 && dx*dx+dy*dy+dz*dz <= 32*32; InitWork++;
            }
            priority = Distance; heapCount = 0; Distance[goal] = 0; Push(goal);
            while (heapCount > 0)
            {
                int b = Pop(); PhysicalWork++;
                for (int k = reverseStart[b]; k < reverseStart[b+1]; k++)
                {
                    int e = reverseEdges[k], a = w.EdgeFrom[e], j = edgeArc[e]; PhysicalWork++;
                    if (j >= 0 && !(local[a] && local[b]))
                    {
                        if (variant < 3) { if (Next[w.Component[a]] != j) continue; }
                        else if (Potential[w.Component[a]] == int.MaxValue ||
                            Potential[w.Component[b]] >= Potential[w.Component[a]]) continue;
                    }
                    int d = checked(Distance[b] + w.EdgeCost[e]); PhysicalWork++;
                    if (d >= Distance[a]) continue;
                    Distance[a] = d; Parent[a] = e; Push(a);
                }
            }
        }

        internal bool Route(int source, int goal, List<int> result)
        {
            result.Clear(); RouteWork++;
            if (Distance[source] == int.MaxValue) return false;
            for (int n = source; n != goal;)
            {
                if (result.Count >= World.NodeCount) throw new InvalidOperationException("Ciclo");
                int e = Parent[n];
                if (e < 0 || World.EdgeFrom[e] != n) throw new InvalidOperationException("Padre inválido");
                result.Add(e); n = World.EdgeTo[e]; RouteWork++;
            }
            return true;
        }

        bool Before(int a, int b) { HeapWork++; return priority[a] != priority[b] ? priority[a] < priority[b] : a < b; }
        void Push(int node)
        {
            HeapWork++; int i = at[node]; if (i < 0) i = heapCount++;
            while (i > 0) { int p = (i-1)/2; if (!Before(node,heap[p])) break; heap[i]=heap[p]; at[heap[i]]=i; i=p; HeapWork++; }
            heap[i]=node; at[node]=i;
        }
        int Pop()
        {
            HeapWork++; int result=heap[0], last=heap[--heapCount]; at[result]=-1; if(heapCount==0)return result;
            int i=0; while(2*i+1<heapCount) { int c=2*i+1;
                if(c+1<heapCount && Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],last))break;heap[i]=heap[c];at[heap[i]]=i;i=c;HeapWork++;
            }
            heap[i]=last;at[last]=i;return result;
        }
    }
}
