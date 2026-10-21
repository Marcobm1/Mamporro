using System;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // Instrumento offline de puerta 1. No registrado en juego ni scheduler.
    // Las unidades incluyen cada referencia examinada; no son ticks ejecutados.
    internal sealed class LandmarkGateProbe
    {
        internal readonly NavPortalWorld World;
        internal readonly int[][] From, To;
        internal readonly int[] Landmarks;
        readonly int[] reverseStart, reverseEdges, distance, parent, stamp, heap, at, heuristic;
        int generation, heapCount, goal, references;
        bool reverse;
        internal long Work, Pops, Edges, Relaxations, Initializations, Bounds, Control;
        internal long PreparationWork;
        internal long Accounted => Pops + Edges + Relaxations + Initializations + Bounds + Control;
        internal long Payload => 4L * (reverseStart.Length + reverseEdges.Length + 6L * World.NodeCount
            + Landmarks.Length + 2L * Landmarks.Length * World.NodeCount);

        internal LandmarkGateProbe(NavPortalWorld w, int count)
        {
            World = w; int n = w.NodeCount;
            reverseStart = new int[n + 1]; reverseEdges = new int[w.EdgeCount];
            distance = new int[n]; parent = new int[n]; stamp = new int[n]; heap = new int[n];
            at = new int[n]; heuristic = new int[n];
            // Temporales adicionales: fill (4*(N+1)) y nearest (8*N), fuera de Payload.
            From = new int[count][]; To = new int[count][]; Landmarks = new int[count];
            PreparationWork = 6L * n + reverseStart.Length + reverseEdges.Length + count * 3L;
            for (int e = 0; e < w.EdgeCount; e++) { reverseStart[w.EdgeTo[e] + 1]++; PreparationWork++; }
            for (int i = 1; i <= n; i++) { reverseStart[i] += reverseStart[i - 1]; PreparationWork++; }
            var fill = (int[])reverseStart.Clone(); PreparationWork += fill.Length;
            for (int e = 0; e < w.EdgeCount; e++) { reverseEdges[fill[w.EdgeTo[e]]++] = e; PreparationWork++; }
            var nearest = new double[n];
            long smallest = long.MaxValue; int selected = -1;
            for (int i = 0; i < n; i++)
            {
                nearest[i] = double.PositiveInfinity; PreparationWork++;
                long key = NavPortalWorld.NodeKey(w.Graph(i), w.NodeLocal[i]);
                if (key < smallest) { smallest = key; selected = i; }
            }
            for (int k = 0; k < count; k++)
            {
                Landmarks[k] = selected;
                Search(selected, -1, 0, false); PreparationWork += Work;
                From[k] = SnapshotAndCertify(selected, false);
                Search(selected, -1, 0, true); PreparationWork += Work;
                To[k] = SnapshotAndCertify(selected, true);
                var p = w.Position(selected); double farthest = -1; long tie = long.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var q = w.Position(i);
                    double dx = p.X - q.X, dy = p.Y - q.Y, dz = p.Z - q.Z;
                    nearest[i] = Math.Min(nearest[i], dx * dx + dy * dy + dz * dz);
                    long key = NavPortalWorld.NodeKey(w.Graph(i), w.NodeLocal[i]); PreparationWork++;
                    if (nearest[i] > farthest || (nearest[i] == farthest && key < tie))
                    { farthest = nearest[i]; selected = i; tie = key; }
                }
            }
        }
        int[] SnapshotAndCertify(int source, bool reversed)
        {
            var w = World; var result = new int[w.NodeCount]; PreparationWork += result.Length;
            for (int n = 0; n < result.Length; n++)
            {
                int d = result[n] = stamp[n] == generation ? distance[n] : int.MaxValue;
                PreparationWork++;
                if (n == source) { if (d != 0) throw new Exception("Raíz incorrecta"); continue; }
                if (d == int.MaxValue) continue;
                int e = parent[n];
                if (e < 0) throw new Exception("Padre ausente");
                int predecessor = reversed ? w.EdgeTo[e] : w.EdgeFrom[e];
                if ((reversed ? w.EdgeFrom[e] : w.EdgeTo[e]) != n ||
                    stamp[predecessor] != generation || distance[predecessor] >= d ||
                    distance[predecessor] + w.EdgeCost[e] != d) throw new Exception("Certificado de padre incorrecto");
            }
            for (int e = 0; e < w.EdgeCount; e++)
            {
                int a = reversed ? w.EdgeTo[e] : w.EdgeFrom[e], b = reversed ? w.EdgeFrom[e] : w.EdgeTo[e];
                PreparationWork++;
                if (result[a] != int.MaxValue && (long)result[a] + w.EdgeCost[e] < result[b])
                    throw new Exception("Desigualdad de distancia incorrecta");
            }
            return result;
        }
        internal int Bound(int node, int target, int count)
        {
            int h = 0;
            for (int k = 0; k < count; k++)
            {
                int a = From[k][target], b = From[k][node];
                if (a != int.MaxValue && b != int.MaxValue) h = Math.Max(h, a - b);
                a = To[k][node]; b = To[k][target];
                if (a != int.MaxValue && b != int.MaxValue) h = Math.Max(h, a - b);
            }
            return h;
        }
        void Touch(int n)
        {
            if (stamp[n] == generation) return;
            stamp[n] = generation; distance[n] = int.MaxValue; parent[n] = -1; at[n] = -1;
            heuristic[n] = references == 0 ? 0 : Bound(n, goal, references);
            Initializations++; Bounds += 2L * references;
        }
        internal int Search(int source, int target, int count, bool reversed = false)
        {
            generation = checked(generation + 1); heapCount = 0; goal = target; references = count; reverse = reversed;
            Work = Pops = Edges = Relaxations = Initializations = Bounds = 0; Control = 2;
            if (reversed && count != 0) throw new ArgumentException("Heurística solo en búsqueda directa");
            Touch(source); distance[source] = 0; Push(source); Relaxations++;
            var w = World; var starts = reverse ? reverseStart : w.EdgeStart;
            while (heapCount > 0)
            {
                int a = Pop(); Pops++;
                if (a == target) { Work = Accounted; return distance[a]; }
                for (int j = starts[a]; j < starts[a + 1]; j++)
                {
                    int e = reverse ? reverseEdges[j] : j, b = reverse ? w.EdgeFrom[e] : w.EdgeTo[e]; Edges++;
                    Touch(b); int candidate = checked(distance[a] + w.EdgeCost[e]); Relaxations++;
                    // Se permite reabrir: omitir términos infinitos puede perder consistencia
                    // en componentes sin camino a la meta, pero no admisibilidad.
                    if (candidate < distance[b]) { distance[b] = candidate; parent[b] = e; Push(b); }
                }
            }
            Work = Accounted; return int.MaxValue;
        }
        internal void VerifyRoute(int source, int target, int expected)
        {
            if (expected == int.MaxValue) return;
            int n = target, length = 0; long cost = 0;
            while (n != source)
            {
                if (++length > World.NodeCount) throw new Exception("Ciclo");
                int e = parent[n];
                if (e < 0 || World.EdgeTo[e] != n) throw new Exception("Arista no física");
                cost += World.EdgeCost[e]; n = World.EdgeFrom[e];
            }
            if (cost != expected) throw new Exception("Coste de ruta incorrecto");
        }
        bool Before(int a, int b)
        {
            long fa = (long)distance[a] + heuristic[a], fb = (long)distance[b] + heuristic[b];
            return fa != fb ? fa < fb : distance[a] != distance[b] ? distance[a] > distance[b] : a < b;
        }
        void Push(int node)
        {
            int i = at[node]; if (i < 0) i = heapCount++;
            while (i > 0) { int p = (i - 1) / 2; if (!Before(node, heap[p])) break; heap[i] = heap[p]; at[heap[i]] = i; i = p; }
            heap[i] = node; at[node] = i;
        }
        int Pop()
        {
            int result = heap[0], last = heap[--heapCount]; at[result] = -1;
            if (heapCount == 0) return result;
            int i = 0;
            while (2 * i + 1 < heapCount)
            {
                int c = 2 * i + 1; if (c + 1 < heapCount && Before(heap[c + 1], heap[c])) c++;
                if (!Before(heap[c], last)) break; heap[i] = heap[c]; at[heap[i]] = i; i = c;
            }
            heap[i] = last; at[last] = i; return result;
        }
    }
}
