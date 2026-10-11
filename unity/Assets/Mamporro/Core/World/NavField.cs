using System;

namespace Mamporro.Core
{
    // P0-B2: campo de distancias compartido por toda la horda de una clase de cuerpo.
    // Dijkstra inverso desde el nodo del jugador sobre aristas de caminar, escalar y caer, con
    // doble búfer y presupuesto de nodos por tick: los enemigos usan el campo publicado mientras
    // se calcula el siguiente. Sin asignaciones tras construir y sin RNG (orden fijo de aristas).
    public sealed class NavField
    {
        public const int Unreached=int.MaxValue;
        // Destinos de quien no puede llegar: el punto alcanzable más cercano al jugador. Su coste
        // parte de este desplazamiento para no confundirse nunca con una ruta real.
        public const int Fallback=1<<28;
        // Intervalo mínimo entre recálculos (máximo 5 por segundo y clase).
        public const double MinInterval=.2;
        // Peso de la distancia al jugador frente al coste de caminar en el destino sustituto:
        // acercarse un metro compensa recorrer hasta ocho. Sin él, la desigualdad triangular haría
        // que nadie se moviera.
        public const int FallbackWeight=8;
        public readonly NavGraph Graph;
        int[] dist,parent,work,workParent;
        readonly int[] heapKey,heapNode;
        int heapCount,phase,building=-1;
        double cooldown,playerX,playerY,playerZ;
        public int Goal {get;private set;}=-1;
        public int Version {get;private set;}
        public int Recomputes,Pops,Relaxations;
        public long LastTicks,TotalTicks;
        public bool Computing=>phase!=0;
        public NavField(NavGraph graph)
        {
            Graph=graph;int n=graph.NodeCount;
            dist=new int[n];parent=new int[n];work=new int[n];workParent=new int[n];
            heapKey=new int[graph.EdgeCount+2*n+1];heapNode=new int[heapKey.Length];
            for(int i=0;i<n;i++){dist[i]=Unreached;parent[i]=-1;}
        }
        public int Dist(int node)=>dist[node];
        // Arista hacia delante (desde node) que sigue el camino más corto, o -1 si es meta o destino.
        public int Parent(int node)=>parent[node];
        public bool Reachable(int node)=>dist[node]<Fallback;

        // Pide (si cambió) y avanza el cálculo. budget = nodos extraídos como máximo en este tick.
        public void Update(int goal,double px,double py,double pz,double dt,int budget)
        {
            cooldown-=dt;
            if(phase==0&&goal>=0&&(goal!=Goal||Version==0)&&cooldown<=0){
                building=goal;playerX=px;playerY=py;playerZ=pz;cooldown=MinInterval;
                for(int i=0;i<work.Length;i++){work[i]=Unreached;workParent[i]=-1;}
                heapCount=0;work[goal]=0;Push(0,goal);phase=1;
            }
            if(phase!=0)Advance(budget);
        }
        // Completa el cálculo en curso sin límite (pruebas y arranque).
        public void Complete(int goal,double px,double py,double pz)
        {
            cooldown=0;if(phase==0)Update(goal,px,py,pz,0,0);while(phase!=0)Advance(int.MaxValue);
        }
        void Advance(int budget)
        {
            long start=System.Diagnostics.Stopwatch.GetTimestamp();var g=Graph;
            while(phase!=0&&budget>0){
                if(heapCount==0){
                    if(phase==1&&SeedFallback())continue;
                    var t=dist;dist=work;work=t;t=parent;parent=workParent;workParent=t;
                    Goal=building;Version++;Recomputes++;phase=0;break;
                }
                int key=heapKey[0],u=heapNode[0];Pop();if(key!=work[u])continue;
                budget--;Pops++;
                for(int r=g.RevStart[u];r<g.RevStart[u+1];r++){
                    int e=g.RevEdge[r],v=g.EdgeFrom[e];
                    if(phase==2&&work[v]<Fallback)continue;
                    int candidate=key+g.EdgeCost[e];
                    if(candidate<work[v]){work[v]=candidate;workParent[v]=e;Push(candidate,v);Relaxations++;}
                }
            }
            LastTicks=System.Diagnostics.Stopwatch.GetTimestamp()-start;TotalTicks+=LastTicks;
        }
        // Nodos sin ruta: su coste propio es la distancia al jugador ponderada; propagado entre
        // ellos, cada uno se dirige al punto de su región más cercano al jugador y allí se detiene.
        bool SeedFallback()
        {
            var g=Graph;bool any=false;
            for(int n=0;n<work.Length;n++){
                if(work[n]!=Unreached)continue;
                double dx=g.NodeX(n)-playerX,dy=g.NodeY[n]-playerY,dz=g.NodeZ(n)-playerZ;
                work[n]=Fallback+(int)Math.Round(Math.Sqrt(dx*dx+dy*dy+dz*dz)*NavGraph.Scale*FallbackWeight);Push(work[n],n);any=true;
            }
            phase=2;return any;
        }
        void Push(int key,int node)
        {
            int i=heapCount++;
            while(i>0){int p=(i-1)>>1;if(heapKey[p]<=key)break;heapKey[i]=heapKey[p];heapNode[i]=heapNode[p];i=p;}
            heapKey[i]=key;heapNode[i]=node;
        }
        void Pop()
        {
            int key=heapKey[--heapCount],node=heapNode[heapCount],i=0;
            while(true){
                int c=2*i+1;if(c>=heapCount)break;
                if(c+1<heapCount&&heapKey[c+1]<heapKey[c])c++;
                if(heapKey[c]>=key)break;heapKey[i]=heapKey[c];heapNode[i]=heapNode[c];i=c;
            }
            heapKey[i]=key;heapNode[i]=node;
        }
    }
}
