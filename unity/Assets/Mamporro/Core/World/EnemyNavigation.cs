using System;

namespace Mamporro.Core
{
    // P0-B3: navegación vertical centralizada de la horda (solo modo P0; nula en U3/U6).
    // Un grafo y un campo compartido por clase de cuerpo; cada enemigo guarda en arrays su nodo,
    // estado y avance. Caminar solo entra en celdas conectadas del grafo (sin penetrar); escalar y
    // caer recorren trayectorias validadas a la velocidad efectiva propia de cada enemigo.
    public sealed class EnemyNavigation : IEnemyNavigation,IVerticalCombat
    {
        public const byte Walk=0,Climb=1,Ledge=2,Drop=3,Fall=4,Recover=5;
        // Holgura de 0,5 mm en caídas y recuperación: un cuerpo que toca una superficie (válido en
        // double) no puede quedar dentro de ella al redondear las posiciones float de la horda
        // (~1e-5 m). Menor que el hueco de 1 mm de la escalada para poder soltarse de la pared.
        const double Pad=.0005;
        // Nodos procesados por tick entre los tres campos al recalcular (~0,3 ms medidos).
        public int Budget=3000;
        int order;
        public readonly VerticalCircuit Circuit;
        public readonly NavGraph[] Graphs;
        public readonly NavField[] Fields;
        // Misma protección que el jugador tras soltarse de una pared (VerticalTuning, 0,35 s).
        static readonly double ReattachDelay=new VerticalTuning().JumpReattachDelay;
        static readonly double Gravity=Tuning.PlayerGravity*Tuning.PlayerFallGravityMultiplier,MaxFall=Tuning.PlayerMaxFallSpeed;
        readonly int[] node,cls,edge,seg,target,neighbors=new int[64],classCount=new int[3];
        readonly byte[] state,los;
        readonly float[] vy,blockTimer,blockNX,blockNZ,blockD,losAge,blocked;
        readonly int[] goal={-1,-1,-1};
        Enemies enemies;CombatPlayer player;
        public int ClimbStarts,LedgesDone,DropsStarted,Landings,Detaches,Recoveries,RecoverFailures,Stuck,QueueWaits;
        public long Ticks;
        public EnemyNavigation(VerticalCircuit circuit,int capacity):this(circuit,capacity,BuildGraphs(circuit)){}
        // Las tres clases son independientes: se construyen en paralelo, cada una con su copia de
        // las consultas. Mismo resultado que en serie (determinista); tiempo = el de la más lenta.
        public static NavGraph[] BuildGraphs(VerticalCircuit circuit)=>BuildGraphs(circuit.Queries,circuit.Terrain,circuit.Collision.IsInside);
        public static NavGraph[] BuildGraphs(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,bool parallel=true)
        {
            var graphs=new NavGraph[NavClass.All.Length];
            if(!parallel){for(int k=0;k<graphs.Length;k++)graphs[k]=new NavGraph(queries,terrain,inside,NavClass.All[k]);return graphs;}
            var tasks=new System.Threading.Tasks.Task[graphs.Length];
            for(int k=0;k<graphs.Length;k++){int c=k;var copy=queries.Copy();tasks[k]=System.Threading.Tasks.Task.Run(()=>graphs[c]=new NavGraph(copy,terrain,inside,NavClass.All[c]));}
            System.Threading.Tasks.Task.WaitAll(tasks);
            return graphs;
        }
        // Grafos ya construidos del mismo circuito (geometría estática): solo campos y estados nuevos.
        public EnemyNavigation(VerticalCircuit circuit,int capacity,NavGraph[] graphs)
        {
            Circuit=circuit;Graphs=graphs;Fields=new NavField[Graphs.Length];
            for(int k=0;k<Graphs.Length;k++)Fields[k]=new NavField(Graphs[k]);
            node=new int[capacity];cls=new int[capacity];edge=new int[capacity];seg=new int[capacity];target=new int[capacity];
            state=new byte[capacity];los=new byte[capacity];vy=new float[capacity];blockTimer=new float[capacity];
            blockNX=new float[capacity];blockNZ=new float[capacity];blockD=new float[capacity];losAge=new float[capacity];blocked=new float[capacity];
        }
        public double BuildMs{get{double t=0;foreach(var g in Graphs)t+=g.BuildMs;return t;}}
        public byte State(int i)=>state[i];
        public int Node(int i)=>node[i];
        public int ClassOf(int i)=>cls[i];
        public int Edge(int i)=>edge[i];

        // ------------------------------------------------------------ ciclo de vida de los huecos
        public void Spawned(Enemies e,int i)
        {
            enemies=e;cls[i]=NavClass.Of(e.Def(i));node[i]=-1;edge[i]=-1;seg[i]=0;target[i]=-1;state[i]=Walk;vy[i]=0;blockTimer[i]=0;
            los[i]=0;losAge[i]=(i%12)/60f;blocked[i]=0;
        }
        // Segundos seguidos sin poder avanzar contra la geometría teniendo intención de moverse
        // (no cuenta esperar en la cola de una pared ni detenerse en el destino sustituto).
        public double GeometryBlockedSeconds(int i)=>blocked[i];
        public void Moved(int from,int to)
        {
            node[to]=node[from];cls[to]=cls[from];edge[to]=edge[from];seg[to]=seg[from];target[to]=target[from];state[to]=state[from];
            vy[to]=vy[from];blockTimer[to]=blockTimer[from];blockNX[to]=blockNX[from];blockNZ[to]=blockNZ[from];blockD[to]=blockD[from];
            los[to]=los[from];losAge[to]=losAge[from];blocked[to]=blocked[from];
        }
        public void Cleared(){for(int k=0;k<goal.Length;k++)goal[k]=-1;}

        // ------------------------------------------------------------ campo por clase
        public void Begin(Enemies e,CombatPlayer p,double dt)
        {
            long start=System.Diagnostics.Stopwatch.GetTimestamp();enemies=e;player=p;
            classCount[0]=classCount[1]=classCount[2]=0;
            for(int i=0;i<e.Count;i++){classCount[cls[i]]++;if(blockTimer[i]>0)blockTimer[i]=(float)(blockTimer[i]-dt);losAge[i]=(float)(losAge[i]-dt);}
            // Un único presupuesto por tick para los tres campos (sin picos por coincidir), con
            // prioridad a la clase con más enemigos; orden determinista en empates.
            int remaining=Budget;
            for(int rank=0;rank<Fields.Length;rank++){
                int k=-1;for(int c=0;c<Fields.Length;c++)if((order&(1<<c))==0&&(k<0||classCount[c]>classCount[k]))k=c;
                order|=1<<k;
                if(classCount[k]==0&&Fields[k].Version>0&&!Fields[k].Computing)continue;
                goal[k]=GoalFor(k,p);remaining-=Fields[k].Update(goal[k],p.X,p.Y,p.Z,dt,Math.Max(0,remaining));
            }
            order=0;
            Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }
        public void End(){}
        // Superficie bajo el jugador; si escala o está en el aire, la más cercana por debajo.
        public int GoalFor(int k,CombatPlayer p)
        {
            var g=Graphs[k];int n=g.NodeAt(p.X,p.Y,p.Z,.3);
            if(n>=0&&p.Y-g.SurfaceY(n,p.X,p.Z)<=3)return n;
            n=g.Nearest(p.X,p.Y,p.Z,4,3,.3);if(n>=0)return n;
            return g.Nearest(p.X,p.Y,p.Z,10,60,60);
        }
        public int Goal(int k)=>goal[k];

        // ------------------------------------------------------------ consultas de comportamiento
        public bool Stable(int i)=>state[i]==Walk&&node[i]>=0;
        public bool Reachable(int i)=>node[i]>=0&&Fields[cls[i]].Version>0&&Fields[cls[i]].Reachable(node[i]);
        public bool LineOfSight(int i,CombatPlayer p)
        {
            if(losAge[i]<=0){
                losAge[i]=.2f;var e=enemies;
                los[i]=(byte)(SegmentBlocked(e.X[i],e.Y[i]+1,e.Z[i],p.X,p.Y+1,p.Z,.05)?0:1);
            }
            return los[i]!=0;
        }
        public bool SeparationOverlap(int i,int j)
        {
            if(state[i]!=Walk||state[j]!=Walk)return false;var e=enemies;
            return e.Y[i]<e.Y[j]+e.Def(j).height&&e.Y[j]<e.Y[i]+e.Def(i).height;
        }
        // Carga: línea recta transitable por el grafo hasta cerca del jugador, en la misma superficie.
        public bool StraightWalk(int i,double tx,double tz,double dist)
        {
            int n=node[i];if(n<0)return false;var g=Graphs[cls[i]];var e=enemies;
            for(double s=.4;s<dist-1;s+=.4){
                int c=g.CellOf(e.X[i]+tx*s,e.Z[i]+tz*s);if(c<0)return false;if(c==g.NodeCell[n])continue;
                int next=WalkNeighbor(g,n,c);if(next<0)return false;n=next;
            }
            return true;
        }
        static int WalkNeighbor(NavGraph g,int n,int cell)
        {
            for(int e=g.EdgeStart[n];e<g.EdgeStart[n+1];e++)if(g.EdgeKind[e]==(byte)NavEdgeKind.Walk&&g.NodeCell[g.EdgeTo[e]]==cell)return g.EdgeTo[e];
            return -1;
        }
        bool Blocked(int i,int e)
        {
            var g=Graphs[cls[i]];if(blockTimer[i]<=0||g.EdgeKind[e]!=(byte)NavEdgeKind.Climb)return false;
            int p=g.EdgePath[e];
            return g.PathNX[p]*blockNX[i]+g.PathNZ[p]*blockNZ[i]>.99&&Math.Abs(g.PathD[p]-blockD[i])<.3;
        }
        // Siguiente arista del campo, o la mejor alternativa si esa pared está protegida tras soltarse.
        int BestEdge(int i,int n)
        {
            var f=Fields[cls[i]];var g=Graphs[cls[i]];if(f.Version==0)return -1;
            int e=f.Parent(n);if(e<0||!Blocked(i,e))return e;
            int best=-1;long bestCost=long.MaxValue;
            for(int a=g.EdgeStart[n];a<g.EdgeStart[n+1];a++){
                if(Blocked(i,a))continue;int d=f.Dist(g.EdgeTo[a]);if(d==NavField.Unreached)continue;
                long c=(long)d+g.EdgeCost[a];if(c<bestCost){bestCost=c;best=a;}
            }
            return best;
        }
        public void Direction(int i,CombatPlayer p,out double dirX,out double dirZ,out bool hold)
        {
            var e=enemies;double x=e.X[i],z=e.Z[i],dx=p.X-x,dz=p.Z-z,dist=Rules.Hypot(dx,dz);if(dist==0)dist=1e-4;
            dirX=dx/dist;dirZ=dz/dist;hold=false;int n=node[i];
            if(state[i]!=Walk||n<0)return;
            var f=Fields[cls[i]];var g=Graphs[cls[i]];if(f.Version==0)return;
            int edgeIndex=BestEdge(i,n);
            if(edgeIndex<0){if(!f.Reachable(n)){hold=true;dirX=dirZ=0;}return;}
            // Cerca del jugador en su misma superficie: persecución directa como el heredado.
            if(f.Reachable(n)&&f.Dist(n)<=150&&g.EdgeKind[edgeIndex]==(byte)NavEdgeKind.Walk)return;
            double tx,tz;
            // Hacia el centro del siguiente nodo: la recta solo cruza celdas conectadas (las diagonales
            // exigen sus dos ortogonales), así nunca se intenta entrar en una celda sin arista.
            if(g.EdgeKind[edgeIndex]==(byte)NavEdgeKind.Walk){int to=g.EdgeTo[edgeIndex];tx=g.NodeX(to);tz=g.NodeZ(to);}
            else{int k=g.PathStart[g.EdgePath[edgeIndex]]+1;tx=g.PathX[k];tz=g.PathZ[k];}
            double ox=tx-x,oz=tz-z,l=Rules.Hypot(ox,oz);if(l>1e-6){dirX=ox/l;dirZ=oz/l;}
        }

        // ------------------------------------------------------------ movimiento
        public bool Move(int i,double vx,double vz,double kx,double kz,double speed,bool dash,bool still,double dt)
        {
            long start=System.Diagnostics.Stopwatch.GetTimestamp();bool blocked=false;
            switch(state[i]){
                case Walk:
                    if(node[i]<0&&!Assign(i)){state[i]=Recover;target[i]=-1;blocked=RecoverStep(i,speed,dt);break;}
                    int e=BestEdge(i,node[i]);var g=Graphs[cls[i]];
                    if(e>=0&&g.EdgeKind[e]!=(byte)NavEdgeKind.Walk&&!dash&&!still){BeginEdge(i,e);blocked=Traverse(i,speed,kx,kz,dt);}
                    else{
                        var en=enemies;double ox=en.X[i],oz=en.Z[i],want=Rules.Hypot((vx+kx)*dt,(vz+kz)*dt);
                        blocked=WalkMove(i,(vx+kx)*dt,(vz+kz)*dt);
                        bool stopped=Rules.Hypot(en.X[i]-ox,en.Z[i]-oz)<1e-4;
                        this.blocked[i]=blocked&&stopped&&want>1e-4&&!still?(float)(this.blocked[i]+dt):0;
                    }
                    break;
                case Climb:case Ledge:case Drop:blocked=Traverse(i,speed,kx,kz,dt);break;
                case Fall:blocked=FallStep(i,kx,kz,dt);break;
                default:blocked=RecoverStep(i,speed,dt);break;
            }
            Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            return blocked;
        }
        bool Assign(int i)
        {
            var g=Graphs[cls[i]];var e=enemies;int n=g.NodeAt(e.X[i],e.Y[i],e.Z[i],NavGraph.Step);
            if(n<0||Math.Abs(g.SurfaceY(n,e.X[i],e.Z[i])-e.Y[i])>NavGraph.Step)return false;
            node[i]=n;e.Y[i]=(float)g.SurfaceY(n,e.X[i],e.Z[i]);return true;
        }
        bool WalkMove(int i,double mx,double mz)
        {
            var e=enemies;double len=Rules.Hypot(mx,mz);int steps=Math.Max(1,(int)Math.Ceiling(len/.4));bool blocked=false;
            double sx=mx/steps,sz=mz/steps;
            for(int s=0;s<steps;s++){
                if(TryStep(i,e.X[i]+sx,e.Z[i]+sz))continue;
                blocked=true;
                // Deslizar a lo largo del obstáculo: primero un eje y si no el otro.
                if(!TryStep(i,e.X[i]+sx,e.Z[i])&&!TryStep(i,e.X[i],e.Z[i]+sz))break;
            }
            return blocked;
        }
        bool TryStep(int i,double x,double z)
        {
            // Validar la misma posición representable que se almacena en la horda.
            // El redondeo a float puede cruzar una frontera de celda.
            x=(float)x;z=(float)z;
            var g=Graphs[cls[i]];int n=node[i],c=g.CellOf(x,z);if(c<0)return false;
            if(c!=g.NodeCell[n]){n=WalkNeighbor(g,n,c);if(n<0)return false;}
            var e=enemies;node[i]=n;e.X[i]=(float)x;e.Z[i]=(float)z;e.Y[i]=(float)g.SurfaceY(n,x,z);return true;
        }
        public bool TryPlace(int i,double x,double z)
        {
            var e=enemies;if(state[i]!=Walk||node[i]<0)return false;
            double dx=x-e.X[i],dz=z-e.Z[i];if(dx*dx+dz*dz>.16)return false;return TryStep(i,x,z);
        }
        void BeginEdge(int i,int e)
        {
            var g=Graphs[cls[i]];edge[i]=e;seg[i]=1;vy[i]=0;
            if(g.EdgeKind[e]==(byte)NavEdgeKind.Climb){state[i]=Climb;ClimbStarts++;}else{state[i]=Drop;DropsStarted++;}
        }
        bool Traverse(int i,double speed,double kx,double kz,double dt)
        {
            var g=Graphs[cls[i]];var en=enemies;int e=edge[i],p=g.EdgePath[e],start=g.PathStart[p],end=g.PathStart[p+1];
            if(state[i]==Drop)return DropStep(i,g,p,start,speed,dt);
            // Empuje hacia fuera de la pared mayor que la velocidad propia: se suelta y cae.
            if(kx*g.PathNX[p]+kz*g.PathNZ[p]>speed&&speed>0){Detach(i,g,p);return false;}
            if(QueueBlocked(i)){QueueWaits++;return true;}
            double remaining=speed*dt,x=en.X[i],y=en.Y[i],z=en.Z[i];int ledge=g.PathLedge[p];
            while(remaining>1e-9){
                int k=start+seg[i];if(k>=end)break;
                double tx=g.PathX[k],ty=g.PathY[k],tz=g.PathZ[k],ox=tx-x,oy=ty-y,oz=tz-z,d=Math.Sqrt(ox*ox+oy*oy+oz*oz);
                if(d<=remaining){x=tx;y=ty;z=tz;remaining-=d;seg[i]++;}
                else{x+=ox/d*remaining;y+=oy/d*remaining;z+=oz/d*remaining;remaining=0;}
                state[i]=ledge>=0&&k>ledge?Ledge:Climb;
            }
            en.X[i]=(float)x;en.Y[i]=(float)y;en.Z[i]=(float)z;
            if(start+seg[i]>=end){node[i]=g.EdgeTo[e];state[i]=Walk;edge[i]=-1;LedgesDone++;en.Y[i]=(float)g.SurfaceY(node[i],x,z);}
            return false;
        }
        // Caer por una arista: avanza hacia el punto sobre el aterrizaje y cae con gravedad en
        // cuanto el cuerpo ya no se apoya: está por encima del suelo bajo su centro (terreno o tapa)
        // y bajarlo 1 cm no toca ningún sólido. Nunca baja del suelo ni entra en un sólido.
        bool DropStep(int i,NavGraph g,int p,int start,double speed,double dt)
        {
            var en=enemies;var q=Circuit.Queries;double r=g.Class.Radius+Pad,h=g.Class.Height;
            double x=en.X[i],y=en.Y[i],z=en.Z[i],ox=g.PathX[start+1],oz=g.PathZ[start+1],landY=g.PathY[start+2];
            double dx=ox-x,dz=oz-z,d=Rules.Hypot(dx,dz),step=Math.Min(d,speed*dt);
            if(d>1e-9){double nx=x+dx/d*step,nz=z+dz/d*step;
                if(Circuit.Collision.GroundHeight(nx,nz,y+.01)<=y+1e-3&&q.Clear(new Vec3(nx,y,nz),r,h)){x=nx;z=nz;}}
            double ground=Circuit.Collision.GroundHeight(x,z,y+.01);
            if(y>ground+1e-3&&q.Clear(new Vec3(x,y-.01,z),r,h)){
                vy[i]=(float)Math.Max(vy[i]-Gravity*dt,-MaxFall);double ny=Math.Max(Math.Max(landY,ground),y+vy[i]*dt);
                if(q.Clear(new Vec3(x,ny,z),r,h))y=ny;else vy[i]=0;
            }else vy[i]=0;
            en.X[i]=(float)x;en.Y[i]=(float)y;en.Z[i]=(float)z;
            if(Rules.Hypot(ox-x,oz-z)<1e-6&&y<=landY+1e-6){node[i]=g.EdgeTo[edge[i]];state[i]=Walk;edge[i]=-1;vy[i]=0;Landings++;en.Y[i]=(float)g.SurfaceY(node[i],x,z);}
            return false;
        }
        void Detach(int i,NavGraph g,int p)
        {
            blockNX[i]=g.PathNX[p];blockNZ[i]=g.PathNZ[p];blockD[i]=g.PathD[p];blockTimer[i]=(float)ReattachDelay;
            state[i]=Fall;edge[i]=-1;node[i]=-1;vy[i]=0;Detaches++;
        }
        // Caída libre tras un empuje: gravedad, empuje horizontal existente y barrido contra sólidos.
        bool FallStep(int i,double kx,double kz,double dt)
        {
            var en=enemies;var g=Graphs[cls[i]];var q=Circuit.Queries;double r=g.Class.Radius+Pad,h=g.Class.Height;
            double x=en.X[i],y=en.Y[i],z=en.Z[i];vy[i]=(float)Math.Max(vy[i]-Gravity*dt,-MaxFall);
            var from=new Vec3(x,y,z);var to=new Vec3(x+kx*dt,y,z+kz*dt);
            if(q.Sweep(from,to,r,h,out var hit)){double f=Math.Max(0,hit.Fraction-1e-5);to=new Vec3(x+kx*dt*f,y,z+kz*dt*f);}
            if(q.Clear(to,r,h)){x=to.X;z=to.Z;}
            double ny=y+vy[i]*dt,ground=Circuit.Collision.GroundHeight(x,z,y+.01);
            if(ny<=ground){ny=ground;vy[i]=0;}
            if(q.Clear(new Vec3(x,ny,z),r,h))y=ny;else vy[i]=0;
            en.X[i]=(float)x;en.Y[i]=(float)y;en.Z[i]=(float)z;
            if(y<=ground+1e-6){
                Landings++;
                if(Assign(i))state[i]=Walk;else{state[i]=Recover;target[i]=-1;}
            }
            return false;
        }
        // Fuera del grafo (aterrizaje junto a una pared, aparición sin nodo): camina con barrido
        // hacia el nodo cercano más próximo. Sin teletransporte.
        bool RecoverStep(int i,double speed,double dt)
        {
            var en=enemies;var g=Graphs[cls[i]];var q=Circuit.Queries;double r=g.Class.Radius+Pad,h=g.Class.Height;
            double x=en.X[i],y=en.Y[i],z=en.Z[i];
            if(target[i]<0){Recoveries++;target[i]=g.Nearest(x,y,z,8,NavGraph.Step,NavGraph.Step);if(target[i]<0)target[i]=g.Nearest(x,y,z,16,4,NavGraph.Step);}
            int t=target[i];if(t<0){RecoverFailures++;return true;}
            if(g.CellOf(x,z)==g.NodeCell[t]&&Math.Abs(g.SurfaceY(t,x,z)-y)<=NavGraph.Step){node[i]=t;state[i]=Walk;target[i]=-1;en.Y[i]=(float)g.SurfaceY(t,x,z);return false;}
            double dx=g.NodeX(t)-x,dz=g.NodeZ(t)-z,d=Rules.Hypot(dx,dz),step=Math.Min(d,speed*dt);if(d<1e-9)return true;
            var to=new Vec3(x+dx/d*step,y,z+dz/d*step);bool blocked=false;
            if(q.Sweep(new Vec3(x,y,z),to,r,h,out var hit)){double f=Math.Max(0,hit.Fraction-1e-5);to=new Vec3(x+dx/d*step*f,y,z+dz/d*step*f);blocked=true;}
            double ground=Circuit.Collision.GroundHeight(to.X,to.Z,y+NavGraph.Step);
            if(ground<=y+NavGraph.Step&&q.ClearAboveStep(new Vec3(to.X,ground,to.Z),r,h,NavGraph.Step)){en.X[i]=(float)to.X;en.Z[i]=(float)to.Z;en.Y[i]=(float)ground;}
            else blocked=true;
            return blocked;
        }
        // Sin trepar sobre otro escalador: espera si otro de la misma columna está justo encima.
        bool QueueBlocked(int i)
        {
            var en=enemies;int n=en.Grid.Query(en.X[i],en.Z[i],en.Radius(i)+.9,neighbors);
            for(int k=0;k<n;k++){
                int j=neighbors[k];if(j==i||j>=en.Count||(state[j]!=Climb&&state[j]!=Ledge))continue;
                double dx=en.X[j]-en.X[i],dz=en.Z[j]-en.Z[i],min=en.Radius(i)+en.Radius(j),dy=en.Y[j]-en.Y[i];
                // Solo si está prácticamente encima (no al lado): sin trepar sobre otro escalador.
                min*=.6;if(dx*dx+dz*dz<min*min&&dy>0&&dy<en.Def(i).height)return true;
            }
            return false;
        }

        // ------------------------------------------------------------ combate vertical (IVerticalCombat)
        public bool SegmentBlocked(double x0,double y0,double z0,double x1,double y1,double z1,double radius)
            =>Circuit.Queries.Sweep(new Vec3(x0,y0-radius,z0),new Vec3(x1,y1-radius,z1),radius,radius*2,out _);
        // Punto de aparición en una superficie válida cercana (esbirros del jefe sobre un tejado).
        public bool SpawnPoint(ref double x,ref double z,double nearY,out double y)
        {
            var g=Graphs[0];int n=g.Nearest(x,nearY,z,6,NavGraph.Step,NavGraph.Step);y=0;
            if(n<0)return false;x=g.NodeX(n);z=g.NodeZ(n);y=g.SurfaceY(n,x,z);return true;
        }
    }
}
