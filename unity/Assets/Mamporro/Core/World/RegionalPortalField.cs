using System;

namespace Mamporro.Core
{
    // Campo exacto sobre extremos de salida, sin matriz de parejas. Prototipo D3
    // optativo: cada extracción pide una transferencia inversa interna y relaja
    // los portales que entran en esa región. Todo avance comparte el planificador.
    public sealed class RegionalPortalField : IRegionalNavWork
    {
        public readonly NavPortalWorld World;
        public readonly RegionalTransferCache Transfers;
        readonly RegionalNavScheduler scheduler;
        readonly int[] nodeToVertex,vertices,regionStart,regionPortals;
        int[] dist,work;
        readonly int[] heap,at;
        int phase,cursor,heapCount,current,portalCursor,portalEnd,building=-1;
        long requested;
        public int ClassIndex=>World.Class.Index;
        // Mientras espera la transferencia, no compite contra el trabajo que necesita.
        public bool Pending=>phase!=0&&(phase!=3||Transfers.State(CurrentNode,World.NodeRegion[CurrentNode])==RegionalTransferState.Ready);
        public bool Computing=>phase!=0;
        public long RequestedTick=>requested;
        public int Priority=>0;
        public int Goal {get;private set;}=-1;
        public int Version {get;private set;}
        public long PublishedTick {get;private set;}=-1;
        public long WorkUnits {get;private set;}
        public int ExitCount=>vertices.Length-1;
        int CurrentNode=>current==ExitCount?building:vertices[current];
        public long PayloadBytes=>(nodeToVertex.Length+(long)vertices.Length+regionStart.Length+regionPortals.Length+dist.Length+work.Length+heap.Length+at.Length)*4+Transfers.PayloadBytes;

        public RegionalPortalField(NavPortalWorld world,RegionalNavScheduler scheduler,int cacheCapacity=4)
        {
            World=world??throw new ArgumentNullException(nameof(world));
            this.scheduler=scheduler??throw new ArgumentNullException(nameof(scheduler));
            nodeToVertex=new int[world.NodeCount];for(int n=0;n<nodeToVertex.Length;n++)nodeToVertex[n]=-1;
            foreach(var p in world.Portals)nodeToVertex[p.From]=0;
            int count=0;for(int n=0;n<nodeToVertex.Length;n++)if(nodeToVertex[n]==0)nodeToVertex[n]=count++;
            vertices=new int[count+1];vertices[count]=-1;
            for(int n=0;n<nodeToVertex.Length;n++)if(nodeToVertex[n]>=0)vertices[nodeToVertex[n]]=n;
            regionStart=new int[world.Regions.Length+1];regionPortals=new int[world.Portals.Length];
            foreach(var p in world.Portals)regionStart[p.ToRegion+1]++;
            for(int r=1;r<regionStart.Length;r++)regionStart[r]+=regionStart[r-1];
            var next=(int[])regionStart.Clone();
            for(int p=0;p<world.Portals.Length;p++)regionPortals[next[world.Portals[p].ToRegion]++]=p;
            dist=new int[count+1];work=new int[count+1];heap=new int[count+1];at=new int[count+1];
            for(int v=0;v<dist.Length;v++)dist[v]=int.MaxValue;
            Transfers=new RegionalTransferCache(world,scheduler,cacheCapacity,reverse:true);
            scheduler.Register(this);
        }
        // Contrapresión explícita: una meta nueva no cancela la versión en marcha.
        // El consumidor vuelve a pedir la meta más reciente después de publicar.
        public bool Request(int goal)
        {
            if(goal<0||goal>=World.NodeCount)throw new ArgumentOutOfRangeException(nameof(goal));
            if(Computing)return goal==building;
            if(Version>0&&Goal==goal)return true;
            building=goal;requested=scheduler.TickIndex;cursor=0;heapCount=0;phase=1;return true;
        }
        // Coste de SALIR desde node (primera arista interregional), no distancia
        // libre desde node. El campo local combinará estas semillas y la meta.
        public bool TryExitCost(int node,out int cost)
        {
            cost=int.MaxValue;
            if(Version==0||node<0||node>=nodeToVertex.Length)return false;
            if(node==Goal){cost=0;return true;}
            int v=nodeToVertex[node];if(v<0)return false;cost=dist[v];return true;
        }
        public int Advance(int budget)
        {
            int used=0;
            while(used<budget&&Pending){
                used++;WorkUnits++;
                if(phase==1){
                    if(cursor<work.Length){work[cursor]=int.MaxValue;at[cursor++]=-1;}
                    else{work[ExitCount]=0;Push(ExitCount);phase=2;}
                }else if(phase==2){
                    if(heapCount==0){var swap=dist;dist=work;work=swap;Goal=building;Version++;PublishedTick=scheduler.TickIndex;phase=0;}
                    else{current=Pop();int n=CurrentNode,r=World.NodeRegion[n];
                        if(!Transfers.Request(n,r))throw new InvalidOperationException("Caché privada ocupada inesperadamente");
                        portalCursor=regionStart[r];portalEnd=regionStart[r+1];phase=3;
                    }
                }else if(phase==3){
                    if(portalCursor==portalEnd){phase=2;continue;}
                    var p=World.Portals[regionPortals[portalCursor++]];
                    if(!Transfers.TryDistance(CurrentNode,p.To,p.ToRegion,out int inside))throw new InvalidOperationException("Transferencia ausente");
                    if(inside==int.MaxValue)continue;
                    int v=nodeToVertex[p.From],candidate=checked(work[current]+inside+p.Cost);
                    if(candidate<work[v]){work[v]=candidate;Push(v);}
                }
            }
            return used;
        }
        bool Before(int a,int b)=>work[a]<work[b]||(work[a]==work[b]&&a<b);
        void Push(int node)
        {
            int i=at[node];if(i<0)i=heapCount++;
            while(i>0){int p=(i-1)/2;if(!Before(node,heap[p]))break;heap[i]=heap[p];at[heap[i]]=i;i=p;}
            heap[i]=node;at[node]=i;
        }
        int Pop()
        {
            int result=heap[0],last=heap[--heapCount];at[result]=-1;if(heapCount==0)return result;
            int i=0;while(2*i+1<heapCount){int c=2*i+1;if(c+1<heapCount&&Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],last))break;heap[i]=heap[c];at[heap[i]]=i;i=c;}
            heap[i]=last;at[last]=i;return result;
        }
    }
}
