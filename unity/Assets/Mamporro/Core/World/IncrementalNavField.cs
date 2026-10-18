using System;

namespace Mamporro.Core
{
    // Comparador D3, no backend adoptado. Reparación exacta de distancias mediante
    // consistencia g/rhs; sin heurística ni aproximación, sobre aristas dirigidas.
    // Publicación atómica y sincronización posterior solo de los nodos cambiados.
    public sealed class IncrementalNavField : IRegionalNavWork
    {
        public readonly NavPortalWorld World;
        readonly RegionalNavScheduler scheduler;
        int[] g,published;
        readonly int[] rhs,heap,at,dirty,dirtyEpoch,incomingStart,incoming;
        int phase,cursor,heapCount,dirtyCount,epoch,building=-1,current;
        int predCursor,predEnd,updateNode,updateCursor,updateEnd,updateBest,afterUpdate;
        bool initialized,lowering;
        int oldValue;
        public int ClassIndex=>World.Class.Index;
        public bool Pending=>phase!=0;
        public bool Computing=>phase!=0;
        public long RequestedTick {get;private set;}
        public int Priority=>0;
        public int Goal {get;private set;}=-1;
        public int Version {get;private set;}
        public long PublishedTick {get;private set;}=-1;
        public long PublishedRequestedTick {get;private set;}=-1;
        public long WorkUnits,Initialized,EdgeScans,Extractions,Predecessors,Copies,Changes;
        public long PayloadBytes=>(World.NodeCount*7L+incomingStart.Length+incoming.Length)*4;

        public IncrementalNavField(NavPortalWorld world,RegionalNavScheduler scheduler)
        {
            World=world??throw new ArgumentNullException(nameof(world));
            this.scheduler=scheduler??throw new ArgumentNullException(nameof(scheduler));
            int n=world.NodeCount;g=new int[n];published=new int[n];rhs=new int[n];
            heap=new int[n];at=new int[n];dirty=new int[n];dirtyEpoch=new int[n];
            incomingStart=new int[n+1];incoming=new int[world.EdgeCount];
            for(int e=0;e<world.EdgeCount;e++)incomingStart[world.EdgeTo[e]+1]++;
            for(int i=1;i<incomingStart.Length;i++)incomingStart[i]+=incomingStart[i-1];
            var next=(int[])incomingStart.Clone();
            for(int e=0;e<world.EdgeCount;e++)incoming[next[world.EdgeTo[e]]++]=e;
            scheduler.Register(this);
        }
        public bool Request(int goal)
        {
            if(goal<0||goal>=World.NodeCount)throw new ArgumentOutOfRangeException(nameof(goal));
            if(Computing)return goal==building;
            if(Version>0&&Goal==goal)return true;
            building=goal;RequestedTick=scheduler.TickIndex;dirtyCount=0;epoch++;cursor=0;
            phase=initialized?2:1;return true;
        }
        public bool TryDistance(int node,out int distance)
        {
            distance=int.MaxValue;if(Version==0||node<0||node>=published.Length)return false;
            distance=published[node];return true;
        }
        void SetG(int node,int value)
        {
            if(g[node]==value)return;g[node]=value;Changes++;
            if(dirtyEpoch[node]!=epoch){dirtyEpoch[node]=epoch;dirty[dirtyCount++]=node;}
        }
        void Update(int node,int after)
        {
            updateNode=node;afterUpdate=after;updateBest=node==building?0:int.MaxValue;
            updateCursor=World.EdgeStart[node];updateEnd=node==building?updateCursor:World.EdgeStart[node+1];phase=6;
        }
        public int Advance(int budget)
        {
            int used=0;
            while(used<budget&&Pending){
                used++;WorkUnits++;
                switch(phase){
                    case 1:
                        if(cursor<g.Length){g[cursor]=published[cursor]=rhs[cursor]=int.MaxValue;at[cursor++]=-1;Initialized++;}
                        else{initialized=true;phase=2;}break;
                    case 2:
                        if(Goal>=0)Update(Goal,3);else phase=3;break;
                    case 3: Update(building,4);break;
                    case 4:
                        if(heapCount==0){
                            var swap=published;published=g;g=swap;Goal=building;Version++;PublishedTick=scheduler.TickIndex;
                            PublishedRequestedTick=RequestedTick;cursor=0;phase=7;break;
                        }
                        current=Pop();Extractions++;predCursor=incomingStart[current];predEnd=incomingStart[current+1];
                        oldValue=g[current];lowering=g[current]>rhs[current];
                        if(lowering){SetG(current,rhs[current]);phase=5;}
                        else{SetG(current,int.MaxValue);Update(current,5);}break;
                    case 5:
                        if(predCursor<predEnd){
                            int edge=incoming[predCursor++],from=World.EdgeFrom[edge];Predecessors++;
                            if(from==building)break;
                            // Un descenso permite relajar directamente. Un aumento solo
                            // invalida rhs si esta arista contribuía al mínimo anterior.
                            if(lowering){int candidate=checked(g[current]+World.EdgeCost[edge]);
                                if(candidate<rhs[from]){rhs[from]=candidate;Fix(from);}
                            }else if(oldValue!=int.MaxValue&&rhs[from]==checked(oldValue+World.EdgeCost[edge]))Update(from,5);
                        }
                        else phase=4;break;
                    case 6:
                        if(updateCursor<updateEnd){
                            int e=updateCursor++,to=World.EdgeTo[e];EdgeScans++;
                            if(g[to]!=int.MaxValue)updateBest=Math.Min(updateBest,checked(g[to]+World.EdgeCost[e]));
                        }else{rhs[updateNode]=updateBest;Fix(updateNode);phase=afterUpdate;}
                        break;
                    case 7:
                        // El lector ya ve la versión completa. Antes de reparar otra meta,
                        // sincronizar el buffer de trabajo, cobrando cada copia al tick.
                        if(cursor<dirtyCount){int n=dirty[cursor++];g[n]=published[n];Copies++;}
                        else phase=0;break;
                }
            }
            return used;
        }
        int Key(int node)=>Math.Min(g[node],rhs[node]);
        bool Before(int a,int b)=>Key(a)<Key(b)||(Key(a)==Key(b)&&a<b);
        void Fix(int node)
        {
            int i=at[node];if(i>=0)Remove(i);
            if(g[node]==rhs[node])return;
            i=heapCount++;heap[i]=node;at[node]=i;Up(i);
        }
        void Up(int i)
        {
            int node=heap[i];
            while(i>0){int p=(i-1)/2;if(!Before(node,heap[p]))break;heap[i]=heap[p];at[heap[i]]=i;i=p;}
            heap[i]=node;at[node]=i;
        }
        void Down(int i)
        {
            int node=heap[i];
            while(2*i+1<heapCount){int c=2*i+1;if(c+1<heapCount&&Before(heap[c+1],heap[c]))c++;
                if(!Before(heap[c],node))break;heap[i]=heap[c];at[heap[i]]=i;i=c;}
            heap[i]=node;at[node]=i;
        }
        void Remove(int i)
        {
            at[heap[i]]=-1;int last=heap[--heapCount];if(i==heapCount)return;
            heap[i]=last;at[last]=i;
            if(i>0&&Before(last,heap[(i-1)/2]))Up(i);else Down(i);
        }
        int Pop(){int result=heap[0];Remove(0);return result;}
    }
}
