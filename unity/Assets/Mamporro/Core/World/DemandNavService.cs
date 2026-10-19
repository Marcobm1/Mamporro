using System;

namespace Mamporro.Core
{
    public enum DemandStatus { Pending, Reachable, Unreachable }
    // Entradas del arnés, no comandos de navegación: se muestrean bajo presupuesto.
    public struct NavDemand { public bool Enabled; public int Source,Goal; public long Since; }
    public struct DemandAnswer
    {
        public DemandStatus Status;
        public int Source,Goal,Cost,Next,Slot;
        public long Revision,Generation,Requested,Published,Serial;
    }

    // Experimento aislado: Dijkstra inverso compartido, detenido sin consumidores.
    // No modifica grafos D2 ni se registra en EnemyNavigation.
    public sealed class DemandNavService : IRegionalNavWork
    {
        public readonly NavPortalWorld World;
        public readonly NavDemand[] Inputs;
        public readonly DemandAnswer[] Answers;
        readonly RegionalNavScheduler scheduler;
        readonly Search[] cache;
        readonly int[] state,source,goal,slot,fifo,revStart,revEdges,fill;
        readonly long[] requested;
        int head,tail,waiting,scan,admissions,turn,phase,indexPhase,indexCursor,publish=-1,resetCursor;
        long tick=-1,revision=1,desiredRevision=1,clock;
        bool done;
        public int ClassIndex=>World.Class.Index;
        public int Priority=>0;
        public long RequestedTick=>0;
        public bool Pending=>scheduler.TickIndex!=tick||!done;
        public long WorkUnits,Maintenance,Initialization,Indexing,Extractions,Edges,Seeds,Publications,Control;
        public long CacheHits,CacheMisses,Evictions,Completed,Invalidated;
        public long CpuTicks {get;private set;}
        public long Accounted=>Maintenance+Initialization+Indexing+Extractions+Edges+Seeds+Publications+Control;
        public long Revision=>desiredRevision;
        public int Waiting=>waiting;
        public long OutstandingSince(int id)=>state[id]==0?Inputs[id].Since:requested[id];
        public long PayloadBytes=>(revStart.LongLength+revEdges.LongLength+fill.LongLength+state.LongLength*5)*4
            +requested.LongLength*8+Inputs.LongLength*24+Answers.LongLength*64+CachePayload();
        long CachePayload(){long bytes=0;foreach(var s in cache)bytes+=s.PayloadBytes;return bytes;}

        public DemandNavService(NavPortalWorld world,RegionalNavScheduler scheduler,int consumers,int capacity=2)
        {
            if(world==null||scheduler==null||consumers<1||capacity<1||capacity>4)throw new ArgumentException();
            World=world;this.scheduler=scheduler;
            Inputs=new NavDemand[consumers];Answers=new DemandAnswer[consumers];
            state=new int[consumers];source=new int[consumers];goal=new int[consumers];slot=new int[consumers];
            fifo=new int[consumers];requested=new long[consumers];
            revStart=new int[world.NodeCount+1];fill=new int[world.NodeCount];revEdges=new int[world.EdgeCount];
            cache=new Search[capacity];for(int i=0;i<capacity;i++)cache[i]=new Search(this);
            scheduler.Register(this);
        }
        // La revisión invalida inmediatamente lectores; el barrido/caché se cobra en Advance.
        // Un cambio físico requiere otra instancia con su nuevo NavPortalWorld inmutable.
        public void Invalidate(){desiredRevision=checked(desiredRevision+1);}
        public DemandStatus Status(int id)
        {
            var a=Answers[id];var input=Inputs[id];
            if(!input.Enabled||a.Serial==0||a.Revision!=desiredRevision||a.Source!=input.Source||a.Goal!=input.Goal)return DemandStatus.Pending;
            if(a.Status==DemandStatus.Reachable&&!Valid(a))return DemandStatus.Pending;
            return a.Status;
        }
        bool Valid(DemandAnswer a)=>a.Revision==desiredRevision&&a.Generation==cache[a.Slot].Generation&&a.Goal==cache[a.Slot].Goal;
        // Vista de ruta con token; una expulsión nunca convierte pendiente en inalcanzable.
        // Lectura O(1) por arista. La copia/recorrido de una ruta pertenece al consumidor.
        public bool TryRouteEdge(DemandAnswer answer,int node,out int edge)
        {
            edge=-1;if(answer.Status!=DemandStatus.Reachable||!Valid(answer)||node<0||node>=World.NodeCount)return false;
            var s=cache[answer.Slot];if(!s.Settled(node))return false;edge=s.Parent(node);return true;
        }
        void Queue(int id)
        {
            var input=Inputs[id];
            if(input.Source<0||input.Source>=World.NodeCount||input.Goal<0||input.Goal>=World.NodeCount)throw new ArgumentException("Nodo inválido");
            source[id]=input.Source;goal[id]=input.Goal;requested[id]=input.Since;
            state[id]=1;fifo[tail]=id;tail=(tail+1)%fifo.Length;waiting++;
        }
        bool Scan(int id)
        {
            if(state[id]==2){var s=cache[slot[id]];if(s.Settled(source[id])||s.Exhausted){publish=id;return true;}}
            else if(state[id]==0&&Inputs[id].Enabled&&Status(id)==DemandStatus.Pending)Queue(id);
            return false;
        }
        void Publish(int id)
        {
            var s=cache[slot[id]];bool reachable=s.Settled(source[id]);
            Answers[id]=new DemandAnswer{Status=reachable?DemandStatus.Reachable:DemandStatus.Unreachable,
                Source=source[id],Goal=goal[id],Cost=reachable?s.Distance(source[id]):int.MaxValue,
                Next=reachable?s.Parent(source[id]):-1,Slot=slot[id],Generation=s.Generation,Revision=revision,
                Requested=requested[id],Published=scheduler.TickIndex,Serial=++Completed};
            s.Users--;s.LastUse=++clock;state[id]=0;
        }
        bool Admit()
        {
            if(waiting==0)return false;
            int id=fifo[head],chosen=-1;
            for(int i=0;i<cache.Length;i++)if(cache[i].Goal==goal[id]){chosen=i;break;}
            if(chosen>=0)CacheHits++;
            else{
                for(int i=0;i<cache.Length;i++)if(cache[i].Users==0&&(chosen<0||cache[i].LastUse<cache[chosen].LastUse))chosen=i;
                if(chosen<0)return false;
                if(cache[chosen].Goal>=0)Evictions++;
                cache[chosen].Begin(goal[id]);CacheMisses++;
            }
            cache[chosen].Users++;cache[chosen].LastUse=++clock;
            slot[id]=chosen;state[id]=2;head=(head+1)%fifo.Length;waiting--;return true;
        }
        public int Advance(int budget)
        {
            long cpuStart=System.Diagnostics.Stopwatch.GetTimestamp();int used=0;
            while(used<budget&&Pending){
                if(tick!=scheduler.TickIndex){tick=scheduler.TickIndex;done=false;scan=admissions=phase=0;}
                used++;WorkUnits++;
                if(revision!=desiredRevision){
                    // No se cancelan consultas por meta móvil. Solo una invalidación explícita.
                    if(resetCursor<Inputs.Length){int i=resetCursor++;state[i]=0;Answers[i]=default;Maintenance++;Invalidated++;}
                    else if(resetCursor<Inputs.Length+cache.Length){cache[resetCursor++-Inputs.Length].Invalidate();Maintenance++;}
                    else{revision=desiredRevision;head=tail=waiting=resetCursor=0;publish=-1;phase=scan=0;Control++;}
                    continue;
                }
                // Índice inverso construido una vez y pagado registro a registro.
                if(indexPhase<4){
                    switch(indexPhase){
                        case 0: if(indexCursor<revStart.Length){revStart[indexCursor++]=0;Initialization++;}else{indexCursor=0;indexPhase++;Control++;}break;
                        case 1: if(indexCursor<World.EdgeCount){revStart[World.EdgeTo[indexCursor++]+1]++;Indexing++;}else{indexCursor=0;indexPhase++;Control++;}break;
                        case 2: if(indexCursor<World.NodeCount){int n=indexCursor++;revStart[n+1]+=revStart[n];fill[n]=revStart[n];Indexing++;}else{indexCursor=0;indexPhase++;Control++;}break;
                        case 3: if(indexCursor<World.EdgeCount){int e=indexCursor++;revEdges[fill[World.EdgeTo[e]]++]=e;Indexing++;}else{indexPhase++;Control++;}break;
                    }continue;
                }
                if(publish>=0){Publish(publish);publish=-1;Publications++;continue;}
                if(phase==0){if(scan<Inputs.Length){Scan(scan++);Maintenance++;}else{phase=1;Control++;}continue;}
                if(phase==1){Maintenance++;if(admissions++>=Inputs.Length||!Admit())phase=2;continue;}
                bool progressed=false;
                for(int i=0;i<cache.Length;i++){
                    int c=turn;turn=(turn+1)%cache.Length;
                    if(cache[c].Users==0||cache[c].Exhausted)continue;
                    cache[c].Step();progressed=true;break;
                }
                if(!progressed){done=true;Control++;}
            }
            CpuTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-cpuStart;return used;
        }
        sealed class Search
        {
            readonly DemandNavService owner;
            readonly int[] distance,parent,heap,at;
            readonly long[] stamp,settled;
            int count,phase,current,cursor,end,pendingEdge;
            public int Goal=-1,Users;
            public long Generation,LastUse;
            public bool Exhausted;
            public long PayloadBytes=>distance.LongLength*32;
            public Search(DemandNavService owner)
            {this.owner=owner;int n=owner.World.NodeCount;distance=new int[n];parent=new int[n];heap=new int[n];at=new int[n];stamp=new long[n];settled=new long[n];}
            public void Invalidate(){Goal=-1;Users=0;Generation=checked(Generation+1);count=0;phase=0;Exhausted=false;}
            public void Begin(int goal){Goal=goal;Generation=checked(Generation+1);count=0;phase=0;Exhausted=false;}
            public bool Settled(int n)=>Goal>=0&&settled[n]==Generation;
            public int Distance(int n)=>distance[n];
            public int Parent(int n)=>parent[n];
            void Touch(int n){stamp[n]=Generation;settled[n]=0;distance[n]=int.MaxValue;parent[n]=at[n]=-1;}
            public void Step()
            {
                var w=owner.World;
                switch(phase){
                    case 0: Touch(Goal);owner.Initialization++;phase=1;break;
                    case 1: distance[Goal]=0;Push(Goal);owner.Seeds++;phase=2;break;
                    case 2:
                        if(count==0){Exhausted=true;owner.Control++;break;}
                        current=Pop();settled[current]=Generation;cursor=owner.revStart[current];end=owner.revStart[current+1];owner.Extractions++;phase=3;break;
                    case 3:
                        if(cursor==end){phase=2;owner.Control++;break;}
                        pendingEdge=owner.revEdges[cursor++];owner.Edges++;
                        phase=stamp[w.EdgeFrom[pendingEdge]]==Generation?5:4;break;
                    case 4: Touch(w.EdgeFrom[pendingEdge]);owner.Initialization++;phase=5;break;
                    case 5:
                        int n=w.EdgeFrom[pendingEdge],value=checked(distance[current]+w.EdgeCost[pendingEdge]);
                        if(value<distance[n]){distance[n]=value;parent[n]=pendingEdge;Push(n);}
                        else if(value==distance[n]&&(parent[n]<0||pendingEdge<parent[n]))parent[n]=pendingEdge;
                        owner.Seeds++;phase=3;break;
                }
            }
            bool Before(int a,int b)=>distance[a]<distance[b]||(distance[a]==distance[b]&&a<b);
            void Push(int n)
            {
                int i=at[n];if(i<0)i=count++;
                while(i>0){int p=(i-1)/2;if(!Before(n,heap[p]))break;heap[i]=heap[p];at[heap[i]]=i;i=p;}
                heap[i]=n;at[n]=i;
            }
            int Pop()
            {
                int n=heap[0],last=heap[--count];at[n]=-1;if(count==0)return n;
                int i=0;while(i*2+1<count){int c=i*2+1;if(c+1<count&&Before(heap[c+1],heap[c]))c++;
                    if(!Before(heap[c],last))break;heap[i]=heap[c];at[heap[i]]=i;i=c;}
                heap[i]=last;at[last]=i;return n;
            }
        }
    }
}
