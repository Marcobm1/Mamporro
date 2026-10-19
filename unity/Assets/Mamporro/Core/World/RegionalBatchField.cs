using System;

namespace Mamporro.Core
{
    // Diagnóstico D3 aislado. Dijkstra local multiobjetivo y cola de regiones
    // reabribles. Solo publica cuando no quedan etiquetas pendientes en el mundo.
    public sealed class RegionalBatchField
    {
        public readonly NavPortalGroups Groups;
        public NavPortalWorld World=>Groups.World;
        int[] dist,parent,published,publishedParent;
        readonly int[] heap,at,offset,counts,queue,queued,reverseStart,reverse,fill,portalStart,portalIds,portalFill,batchSeeds;
        public readonly long[] RegionUnits,RegionBatches,RegionSeeds;
        int phase,cursor,building=-1,head,tail,queueCount,region,current,edgeCursor,edgeEnd,seedPortal;
        bool indexed;
        public bool Computing=>phase!=0;
        public int Goal {get;private set;}=-1;
        public int Version {get;private set;}
        public long WorkUnits,Initialization,Indexing,Extractions,Edges,Seeds,Propagation,Batches,Publications,Control;
        public long BatchSeedTotal;
        public int MaxBatchSeeds;
        public long PayloadBytes=>(World.NodeCount*6L+offset.Length+counts.Length+queue.Length+queued.Length+reverseStart.Length+reverse.Length+fill.Length+portalStart.Length+portalIds.Length+portalFill.Length+batchSeeds.Length)*4+(RegionUnits.Length+RegionBatches.Length+RegionSeeds.Length)*8L;
        public RegionalBatchField(NavPortalGroups groups)
        {
            Groups=groups??throw new ArgumentNullException(nameof(groups));int n=World.NodeCount,r=World.Regions.Length;
            dist=new int[n];parent=new int[n];published=new int[n];publishedParent=new int[n];heap=new int[n];at=new int[n];
            offset=new int[r+1];counts=new int[r];queue=new int[r];queued=new int[r];
            reverseStart=new int[n+1];reverse=new int[World.EdgeCount];fill=new int[n];
            portalStart=new int[r+1];portalIds=new int[World.Portals.Length];portalFill=new int[r];
            RegionUnits=new long[r];RegionBatches=new long[r];RegionSeeds=new long[r];
            batchSeeds=new int[r];
        }
        public bool Request(int goal)
        {
            if(goal<0||goal>=World.NodeCount)throw new ArgumentOutOfRangeException(nameof(goal));
            if(Computing)return goal==building;
            if(Version>0&&Goal==goal)return true;
            building=goal;cursor=0;head=tail=queueCount=0;phase=1;return true;
        }
        public bool TryDistance(int node,out int cost)
        {cost=int.MaxValue;if(Version==0||node<0||node>=published.Length)return false;cost=published[node];return true;}
        public bool TryNext(int node,out int edge)
        {edge=-1;if(Version==0||node<0||node>=published.Length)return false;edge=publishedParent[node];return true;}
        void Enqueue(int r)
        {if(queued[r]!=0)return;queued[r]=1;queue[tail]=r;tail=(tail+1)%queue.Length;queueCount++;}
        void NextPhase(int next){phase=next;cursor=0;}
        public int Advance(int budget)
        {
            int used=0;var w=World;
            while(used<budget&&Computing){
                used++;WorkUnits++;
                switch(phase){
                    case 1:
                        if(cursor<w.NodeCount){dist[cursor]=int.MaxValue;parent[cursor]=-1;at[cursor]=-1;
                            if(!indexed){reverseStart[cursor]=0;fill[cursor]=0;}RegionUnits[w.NodeRegion[cursor]]++;cursor++;Initialization++;}
                        else{NextPhase(2);Control++;}break;
                    case 2:
                        if(cursor<w.Regions.Length){int r=cursor++;counts[r]=queued[r]=batchSeeds[r]=0;
                            offset[r+1]=offset[r]+w.Regions[r].OwnedNodes;
                            if(!indexed){portalStart[r]=portalFill[r]=0;}Initialization++;RegionUnits[r]++;}
                        else{NextPhase(indexed?9:3);Control++;}break;
                    case 3:
                        if(cursor<w.EdgeCount){int e=cursor++;if(w.NodeRegion[w.EdgeFrom[e]]==w.NodeRegion[w.EdgeTo[e]])reverseStart[w.EdgeTo[e]+1]++;Indexing++;RegionUnits[w.NodeRegion[w.EdgeFrom[e]]]++;}
                        else{NextPhase(4);Control++;}break;
                    case 4:
                        if(cursor<w.NodeCount){int n=cursor++;reverseStart[n+1]+=reverseStart[n];fill[n]=reverseStart[n];Indexing++;RegionUnits[w.NodeRegion[n]]++;}
                        else{NextPhase(5);Control++;}break;
                    case 5:
                        if(cursor<w.EdgeCount){int e=cursor++;if(w.NodeRegion[w.EdgeFrom[e]]==w.NodeRegion[w.EdgeTo[e]])reverse[fill[w.EdgeTo[e]]++]=e;Indexing++;RegionUnits[w.NodeRegion[w.EdgeFrom[e]]]++;}
                        else{NextPhase(6);Control++;}break;
                    case 6:
                        if(cursor<Groups.Members.Length){var p=w.Portals[Groups.Members[cursor++]];portalStart[p.ToRegion+1]++;Indexing++;RegionUnits[p.ToRegion]++;}
                        else{NextPhase(7);Control++;}break;
                    case 7:
                        if(cursor<w.Regions.Length){int r=cursor++;portalStart[r+1]+=portalStart[r];portalFill[r]=portalStart[r];Indexing++;RegionUnits[r]++;}
                        else{NextPhase(8);Control++;}break;
                    case 8:
                        if(cursor<Groups.Members.Length){int id=Groups.Members[cursor++];portalIds[portalFill[w.Portals[id].ToRegion]++]=id;Indexing++;RegionUnits[w.Portals[id].ToRegion]++;}
                        else{indexed=true;NextPhase(9);Control++;}break;
                    case 9:
                        dist[building]=0;Push(building);Enqueue(w.NodeRegion[building]);batchSeeds[w.NodeRegion[building]]++;Seeds++;RegionSeeds[w.NodeRegion[building]]++;RegionUnits[w.NodeRegion[building]]++;NextPhase(10);break;
                    case 10:
                        if(queueCount==0){var swap=published;published=dist;dist=swap;swap=publishedParent;publishedParent=parent;parent=swap;
                            Goal=building;Version++;Publications++;phase=0;}
                        else{region=queue[head];head=(head+1)%queue.Length;queueCount--;queued[region]=0;
                            Batches++;RegionBatches[region]++;RegionUnits[region]++;
                            BatchSeedTotal+=batchSeeds[region];MaxBatchSeeds=Math.Max(MaxBatchSeeds,batchSeeds[region]);batchSeeds[region]=0;phase=11;}break;
                    case 11:
                        RegionUnits[region]++;
                        if(counts[region]>0){current=Pop(region);Extractions++;edgeCursor=reverseStart[current];edgeEnd=reverseStart[current+1];phase=12;}
                        else{edgeCursor=portalStart[region];edgeEnd=portalStart[region+1];phase=13;Control++;}break;
                    case 12:
                        RegionUnits[region]++;
                        if(edgeCursor<edgeEnd){int e=reverse[edgeCursor++];Relax(w.EdgeFrom[e],checked(dist[current]+w.EdgeCost[e]),e);Edges++;}
                        else{phase=11;Control++;}break;
                    case 13:
                        RegionUnits[region]++;
                        if(edgeCursor<edgeEnd){seedPortal=portalIds[edgeCursor++];Propagation++;
                            if(dist[w.Portals[seedPortal].To]!=int.MaxValue)phase=14;}
                        else{phase=10;Control++;}break;
                    case 14:
                        var portal=w.Portals[seedPortal];Seeds++;RegionSeeds[portal.FromRegion]++;RegionUnits[portal.FromRegion]++;
                        int globalEdge=w.EdgeStart[portal.From]+portal.LocalEdge-w.Graph(portal.From).EdgeStart[portal.LocalFrom];
                        if(Relax(portal.From,checked(dist[portal.To]+portal.Cost),globalEdge)){Enqueue(portal.FromRegion);batchSeeds[portal.FromRegion]++;}
                        phase=13;break;
                }
            }
            return used;
        }
        bool Relax(int node,int cost,int edge)
        {
            if(cost>dist[node])return false;
            if(cost==dist[node]){if(parent[node]<0||edge<parent[node])parent[node]=edge;return false;}
            dist[node]=cost;parent[node]=edge;Push(node);return true;
        }
        bool Before(int a,int b)=>dist[a]<dist[b]||(dist[a]==dist[b]&&a<b);
        void Push(int node)
        {
            int r=World.NodeRegion[node],start=offset[r],i=at[node];if(i<0)i=counts[r]++;
            while(i>0){int p=(i-1)/2;if(!Before(node,heap[start+p]))break;heap[start+i]=heap[start+p];at[heap[start+i]]=i;i=p;}
            heap[start+i]=node;at[node]=i;
        }
        int Pop(int r)
        {
            int start=offset[r],result=heap[start],last=heap[start+--counts[r]];at[result]=-1;if(counts[r]==0)return result;
            int i=0;while(2*i+1<counts[r]){int c=2*i+1;if(c+1<counts[r]&&Before(heap[start+c+1],heap[start+c]))c++;
                if(!Before(heap[start+c],last))break;heap[start+i]=heap[start+c];at[heap[start+i]]=i;i=c;}
            heap[start+i]=last;at[last]=i;return result;
        }
    }
}
