using System;

namespace Mamporro.Core
{
    public enum RegionalTransferState { Missing,Pending,Ready }

    // D3, primera pieza: transferencias dirigidas exactas desde una fuente física.
    // region >= 0 limita la transferencia al interior; -1 es el control mundial
    // fraccionado. No confundir una transferencia interna imposible con falta de
    // ruta global: esta puede salir y volver. Sin matriz de parejas de portales.
    // Instancia ligada a mundo/clase/versiones inmutables; sustituir ante geometría nueva.
    public sealed class RegionalTransferCache
    {
        public readonly NavPortalWorld World;
        public readonly int GeometryVersion,ConnectivityVersion;
        readonly RegionalNavScheduler scheduler;
        readonly Entry[] entries;
        readonly int[] regionStart;
        readonly int[] incomingStart,incomingEdges;
        public readonly bool Reverse;
        readonly bool worldControl;
        readonly int bufferLength;
        long access;
        public long Requests,Hits,Coalesced,Rejected,Started,Completed,Evictions;
        public long Initializations,Extractions,Relaxations,Publications;
        public int Capacity=>entries.Length;
        public long PayloadBytes=>(long)entries.Length*bufferLength*4*4+regionStart.Length*4L+(incomingStart==null?0:(incomingStart.Length+(long)incomingEdges.Length)*4);
        public RegionalTransferCache(NavPortalWorld world,RegionalNavScheduler scheduler,int capacity=4,int geometryVersion=1,int connectivityVersion=1,bool worldControl=false,bool reverse=false)
        {
            if(world==null||scheduler==null)throw new ArgumentNullException();
            if(capacity<1||capacity>16)throw new ArgumentOutOfRangeException(nameof(capacity));
            World=world;this.scheduler=scheduler;GeometryVersion=geometryVersion;ConnectivityVersion=connectivityVersion;
            Reverse=reverse;
            // Índice construido en carga: conserva IDs y dirección física de aristas.
            // En modo inverso Source es la salida/meta; Parent indica el siguiente
            // paso físico HACIA ella, nunca una arista inventada en sentido contrario.
            if(reverse){
                incomingStart=new int[world.NodeCount+1];incomingEdges=new int[world.EdgeCount];
                for(int e=0;e<world.EdgeCount;e++)incomingStart[world.EdgeTo[e]+1]++;
                for(int n=1;n<incomingStart.Length;n++)incomingStart[n]+=incomingStart[n-1];
                var next=(int[])incomingStart.Clone();
                for(int e=0;e<world.EdgeCount;e++)incomingEdges[next[world.EdgeTo[e]]++]=e;
            }
            this.worldControl=worldControl;regionStart=new int[world.Regions.Length+1];
            // D2 ordena los propietarios por región. Índice de carga, sin barridos
            // mundiales dentro de Request/Tick ni memoria proporcional a parejas.
            for(int r=0;r<world.Regions.Length;r++){
                int n=world.Regions[r].OwnedNodes;regionStart[r+1]=regionStart[r]+n;bufferLength=Math.Max(bufferLength,n);
            }
            if(regionStart[world.Regions.Length]!=world.NodeCount)throw new InvalidOperationException("Propietarios incompatibles");
            if(worldControl)bufferLength=world.NodeCount;
            entries=new Entry[capacity];
            for(int i=0;i<capacity;i++){entries[i]=new Entry(this);scheduler.Register(entries[i]);}
        }
        void Validate(int source,int region)
        {
            if(source<0||source>=World.NodeCount)throw new ArgumentOutOfRangeException(nameof(source));
            if(region< -1||region>=World.Regions.Length||(region>=0&&World.NodeRegion[source]!=region))throw new ArgumentOutOfRangeException(nameof(region));
            if(region<0&&!worldControl)throw new ArgumentException("Control mundial no habilitado");
        }
        Entry Find(int source,int region)
        {foreach(var e in entries)if(source>=0&&e.Source==source&&e.Region==region)return e;return null;}
        // False = capacidad ocupada por cálculos pendientes: contrapresión explícita.
        // No se cancela un cálculo por cambios rápidos de meta ni se mueve su antigüedad.
        public bool Request(int source,int region=-1,int priority=0)
        {
            Validate(source,region);Requests++;var entry=Find(source,region);
            if(entry!=null){entry.Access=++access;if(entry.Pending)Coalesced++;else Hits++;return true;}
            foreach(var e in entries)if(!e.Pending&&(entry==null||e.Source<0||e.Access<entry.Access)){entry=e;if(e.Source<0)break;}
            if(entry==null){Rejected++;return false;}
            if(entry.Source>=0)Evictions++;
            entry.Begin(source,region,scheduler.TickIndex,priority,++access);Started++;return true;
        }
        public RegionalTransferState State(int source,int region=-1)
        {var e=Find(source,region);return e==null?RegionalTransferState.Missing:e.Pending?RegionalTransferState.Pending:RegionalTransferState.Ready;}
        // Solo resultados terminados. MaxValue es inaccesible dentro del ámbito elegido.
        public bool TryDistance(int source,int target,int region,out int distance)
        {
            distance=int.MaxValue;if(target<0||target>=World.NodeCount)return false;
            var e=Find(source,region);if(e==null||e.Pending)return false;
            e.Access=++access;distance=e.Distance(target);return true;
        }
        public bool TryParent(int source,int target,int region,out int edge)
        {
            edge=-1;if(target<0||target>=World.NodeCount)return false;
            var e=Find(source,region);if(e==null||e.Pending)return false;
            e.Access=++access;edge=e.ParentEdge(target);return true;
        }
        public long PublishedTick(int source,int region=-1)
        {var e=Find(source,region);return e==null||e.Pending?-1:e.CompletedTick;}

        sealed class Entry : IRegionalNavWork
        {
            readonly RegionalTransferCache owner;
            public readonly int[] Dist,Parent;
            readonly int[] heap,at;
            int cursor,heapCount,edgeCursor=-1,edgeEnd,current,phase;
            int offset,length;
            public int Source=-1,Region=-1;
            public long Access,CompletedTick;
            public int ClassIndex=>owner.World.Class.Index;
            public bool Pending=>phase!=0;
            public long RequestedTick {get;private set;}
            public int Priority {get;private set;}
            public Entry(RegionalTransferCache owner)
            {this.owner=owner;int n=owner.bufferLength;Dist=new int[n];Parent=new int[n];heap=new int[n];at=new int[n];}
            public void Begin(int source,int region,long tick,int priority,long access)
            {
                Source=source;Region=region;RequestedTick=tick;Priority=priority;Access=access;cursor=0;heapCount=0;edgeCursor=-1;phase=1;
                offset=region<0?0:owner.regionStart[region];length=region<0?owner.World.NodeCount:owner.regionStart[region+1]-offset;
            }
            public int Distance(int node)=>node<offset||node>=offset+length?int.MaxValue:Dist[node-offset];
            public int ParentEdge(int node)=>node<offset||node>=offset+length?-1:Parent[node-offset];
            public int Advance(int budget)
            {
                int used=0;var w=owner.World;
                while(used<budget&&Pending){
                    // Cada registro inicializado, extracción, arista examinada, siembra
                    // y publicación consume una unidad. El heap indexado está acotado
                    // a V; sus O(log V) comparaciones forman parte de la operación y CPU.
                    used++;
                    if(phase==1){
                        if(cursor<length){Dist[cursor]=int.MaxValue;Parent[cursor]=-1;at[cursor]=-1;cursor++;owner.Initializations++;}
                        else{Dist[Source-offset]=0;Push(Source-offset);phase=2;owner.Initializations++;}
                        continue;
                    }
                    if(edgeCursor>=0){
                        int e=owner.Reverse?owner.incomingEdges[edgeCursor++]:edgeCursor++;
                        int b=owner.Reverse?w.EdgeFrom[e]:w.EdgeTo[e];owner.Relaxations++;
                        if(Region<0||w.NodeRegion[b]==Region){
                            b-=offset;
                            int candidate=checked(Dist[current]+w.EdgeCost[e]);
                            if(candidate<Dist[b]){Dist[b]=candidate;Parent[b]=e;Push(b);}
                        }
                        if(edgeCursor==edgeEnd)edgeCursor=-1;
                    }else if(heapCount>0){
                        current=Pop();owner.Extractions++;
                        var starts=owner.Reverse?owner.incomingStart:w.EdgeStart;
                        edgeCursor=starts[current+offset];edgeEnd=starts[current+offset+1];
                        if(edgeCursor==edgeEnd)edgeCursor=-1;
                    }else{phase=0;CompletedTick=owner.scheduler.TickIndex;owner.Completed++;owner.Publications++;}
                }
                return used;
            }
            bool Before(int a,int b)=>Dist[a]<Dist[b]||(Dist[a]==Dist[b]&&a<b);
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
}
