using System;
using System.IO;
using Mamporro.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mamporro.U3
{
    // P0-A optativo. La build QA exige una ruta de progreso aislada antes de abrir el almacén.
    public sealed class VerticalQa
    {
        public static bool RequestedForTests;
        public static bool Requested {
            get {
#if MAMPORRO_P0_QA
                return true;
#else
                return RequestedForTests||Array.IndexOf(Environment.GetCommandLineArgs(),"-p0-qa")>=0;
#endif
            }
        }
        // A: escalada (P0-A, aprobado, intacto). B: persecución vertical (P0-B). C: mapa procedural
        // validado (P0-C). F10 recorre A → B → C; -p0-circuit B|C. Los grafos se construyen una vez
        // por circuito (en C, por semilla: los de la validación) y se reutilizan entre partidas.
        readonly VerticalCircuit climbing=new VerticalCircuit();
        VerticalCircuit pursuit;NavGraph[] pursuitGraphs;
        VerticalCircuit procedural;
        // Semilla del circuito C: F11 recorre ProceduralMap.QaSeeds; -p0-seed fija cualquier texto.
        public string ProceduralSeed {get;private set;}=ProceduralMap.QaSeeds[0];
        NavGraph[] Graphs=>Circuit.Layout==VerticalLayout.Procedural?procedural.Map.Graphs:pursuitGraphs;
        public VerticalCircuit Circuit {get;private set;}
        public EnemyNavigation Navigation {get;private set;}
        // Solo QA: false repite el circuito B con la persecución heredada (línea base B0).
        public bool NavigationEnabled=true;
        public double GraphBuildMs {get;private set;}
        public VerticalMotion Motion {get;private set;}
        public VerticalIntent? ScriptedInput;
        public Func<VerticalIntent> InputProvider;
        public VerticalIntent LastInput {get;private set;}
        public int Recoveries;
        public int Falls,UnintendedGrabs,UnintendedRegrabs;
        public double Seconds,Ascent,Descent,Distance;
        public int WalkStalls;
        double blockedSeconds;
        double supportedY;
        bool fallCounted,jumpedDuringAir;
        public int Stalls=>Motion.Stalls+WalkStalls;
        public bool InvalidRoute=>Recoveries>0||UnintendedGrabs>0||UnintendedRegrabs>0;
        public bool Control;
        float cameraDistance=float.PositiveInfinity;
        readonly U3Game game;
        public VerticalQa(U3Game game)
        {
            this.game=game;Circuit=climbing;
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-p0-circuit"),s=Array.IndexOf(args,"-p0-seed");
            string circuit=i>=0&&i+1<args.Length?args[i+1]:null;
            // Mismo formato de semilla que el juego (A–Z y 0–9, hasta 12).
            string seed=s>=0&&s+1<args.Length?U3Game.NormalizeSeed(args[s+1]):null;if(seed!=null)ProceduralSeed=seed;
            if("B".Equals(circuit,StringComparison.OrdinalIgnoreCase))Select(VerticalLayout.Persecucion);
            else if("C".Equals(circuit,StringComparison.OrdinalIgnoreCase)||circuit==null&&s>=0)Select(VerticalLayout.Procedural);
        }
        public void Select(VerticalLayout layout)
        {
            if(layout==VerticalLayout.Escalada){Circuit=climbing;return;}
            if(layout==VerticalLayout.Procedural){
                if(procedural==null||procedural.Seed!=ProceduralSeed){
                    var map=new ProceduralMap(ProceduralSeed);procedural=new VerticalCircuit(map);GraphBuildMs=map.TotalMs;
                }
                Circuit=procedural;return;
            }
            if(pursuit==null){
                pursuit=new VerticalCircuit(VerticalLayout.Persecucion);var watch=System.Diagnostics.Stopwatch.StartNew();
                pursuitGraphs=EnemyNavigation.BuildGraphs(pursuit);GraphBuildMs=watch.Elapsed.TotalMilliseconds;
            }
            Circuit=pursuit;
        }
        public void ToggleLayout()=>Select(Circuit.Layout==VerticalLayout.Escalada?VerticalLayout.Persecucion:Circuit.Layout==VerticalLayout.Persecucion?VerticalLayout.Procedural:VerticalLayout.Escalada);
        // F11: siguiente semilla QA (y circuito C si no estaba activo).
        public void NextSeed()
        {
            int i=Array.IndexOf(ProceduralMap.QaSeeds,ProceduralSeed);
            if(Circuit.Layout==VerticalLayout.Procedural||i<0)ProceduralSeed=ProceduralMap.QaSeeds[(i+1)%ProceduralMap.QaSeeds.Length];
            Select(VerticalLayout.Procedural);
        }
        public void UseSeed(string seed){ProceduralSeed=U3Game.NormalizeSeed(seed)??ProceduralSeed;Select(VerticalLayout.Procedural);}
        public static void RequireIsolatedSave()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-u4-save-dir");
            string path=U3Game.SaveDirectoryOverride??(i>=0&&i+1<args.Length?args[i+1]:null);
#if MAMPORRO_P0_QA
            // También al abrir el EXE directamente: jamás arranca como entrega U6.
            if(string.IsNullOrWhiteSpace(path))path=U3Game.SaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"MamporroP0QA","ManualSave");
#endif
            if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException("P0 QA requiere -u4-save-dir aislado.");
            string full=Path.GetFullPath(path),personal=Path.GetFullPath(Application.persistentDataPath);
            if(full.Equals(personal,StringComparison.OrdinalIgnoreCase)||full.StartsWith(personal+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P0 QA no permite usar la carpeta personal.");
        }
        public void Bind()
        {
            game.Session.Automatic=false;game.Session.Cheated=true;
            Motion=new VerticalMotion(game.Body,Circuit.Collision,Circuit.Queries);Recoveries=0;ScriptedInput=null;
            // Navegación de la horda en los circuitos B y C; el A conserva la horda heredada aprobada.
            Navigation=null;
            if(Circuit.Layout!=VerticalLayout.Escalada&&NavigationEnabled){
                Navigation=new EnemyNavigation(Circuit,game.Run.Enemies.Capacity,Graphs);
                game.Run.Enemies.Navigation=Navigation;game.Run.Vertical=Navigation;
            }
            InputProvider=null;Falls=UnintendedGrabs=UnintendedRegrabs=WalkStalls=0;Seconds=Ascent=Descent=Distance=blockedSeconds=0;
            supportedY=game.Body.Y;fallCounted=jumpedDuringAir=false;
            cameraDistance=float.PositiveInfinity;
            game.Session.PhysicsStep=Step;
        }
        void Step(PlayerIntent movement,double dt,double speed,double crowd)
        {
            var k=Keyboard.current;
            // Misma guiñada que orienta WASD en suelo; derecha de cámara en coordenadas núcleo.
            double yaw=game.WebYaw;
            var input=InputProvider!=null?InputProvider():ScriptedInput??new VerticalIntent{Movement=movement,
                WallHorizontal=k==null?0:(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),
                WallVertical=k==null?0:(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0),
                CameraRightX=Math.Cos(yaw),CameraRightZ=-Math.Sin(yaw)};
            LastInput=input;
            var b=game.Body;double x=b.X,y=b.Y,z=b.Z;int grabs=Motion.Grabs;
            if(Control)PlayerPhysics.StepInCrowd(game.Body,input.Movement,Circuit.Collision,PlayerTuning.Default,speed,crowd,dt);
            else Motion.Step(input,dt,speed,crowd);
            Seconds+=dt;double dy=b.Y-y;Ascent+=Math.Max(0,dy);Descent+=Math.Max(0,-dy);
            double moved=Math.Sqrt((b.X-x)*(b.X-x)+dy*dy+(b.Z-z)*(b.Z-z));Distance+=moved;
            if(Motion.Armed&&Motion.State!=VerticalState.Climbing&&Motion.State!=VerticalState.Ledge&&moved<1e-6&&JsMath.Hypot(input.Movement.MoveX,input.Movement.MoveZ)>.1){
                blockedSeconds+=dt;if(blockedSeconds>=.35&&blockedSeconds-dt<.35)WalkStalls++;
            }else blockedSeconds=0;
            // El canto puede alternar suelo/aire antes de perder altura: una sola caída,
            // confirmada tras descender 5 cm, no un incidente por cada cambio de estado.
            if(b.Grounded||Motion.State==VerticalState.Climbing||Motion.State==VerticalState.Ledge){
                supportedY=b.Y;fallCounted=jumpedDuringAir=false;
            }else{
                jumpedDuringAir|=b.Jumped||input.Movement.JumpPressed;
                if(!fallCounted&&!jumpedDuringAir&&b.Y<supportedY-.05){Falls++;fallCounted=true;}
            }
            if(Motion.Grabs>grabs){if(!Motion.PushesIntoContact(input.Movement))UnintendedGrabs++;if(Motion.CurrentContactBlocked)UnintendedRegrabs++;}
            if(game.Body.Y< -10||!VerticalQueries.Finite(new Vec3(game.Body.X,game.Body.Y,game.Body.Z))){
                Recover();
            }
        }
        public void Recover()
        {
            Recoveries++;game.PlaceQa(Circuit.Start);Motion.Suspend();
            game.Hud.Notice("QA: recuperación; recorrido inválido, sin recompensas",false);
        }
        public void Suspend(){Motion?.Suspend();ScriptedInput=null;}
        public Vector3 CameraPivot(Vector3 player,Vector3 desired)
        {
            if(Control)return desired;
            var start=player+Vector3.up*.8f;
            var a=new Vec3(start.x,start.y-.15,-start.z);var b=new Vec3(desired.x,desired.y-.15,-desired.z);
            return Circuit.Queries.Sweep(a,b,.15,.3,out var hit)?Vector3.Lerp(start,desired,(float)Math.Max(0,hit.Fraction-.002)):desired;
        }
        public Vector3 ClipCamera(Vector3 pivot,Vector3 camera,float dt=1f/60)
        {
            if(Control)return camera;
            const double r=.15;
            var a=new Vec3(pivot.x,pivot.y-r,-pivot.z);var b=new Vec3(camera.x,camera.y-r,-camera.z);
            // Bajo un voladizo, probar el brazo horizontal libre antes de retraerlo
            // hasta dentro del avatar. No cambia el azimut ni gira la cámara sola.
            if(Circuit.Queries.Sweep(a,b,r,r*2,out var ceiling)&&ceiling.Normal.Y<-.5){
                camera.y=Mathf.Min(camera.y,pivot.y);b=new Vec3(camera.x,camera.y-r,-camera.z);
            }
            float length=Vector3.Distance(pivot,camera),allowed=length;
            if(Circuit.Queries.Sweep(a,b,r,r*2,out var h))allowed=length*(float)Math.Max(0,h.Fraction-.002);
            cameraDistance=allowed<cameraDistance?allowed:Mathf.Lerp(cameraDistance,allowed,1-Mathf.Exp(-4*dt));
            return length<.0001f?pivot:Vector3.Lerp(pivot,camera,cameraDistance/length);
        }
        public void Spawn(int count)
        {
            if(Circuit.Layout!=VerticalLayout.Escalada){SpawnPursuit(count);return;}
            // Apariciones controladas QA, sin alterar el director ni sus reglas.
            game.Run.Enemies.Clear();
            for(int i=0,sample=0;i<count;sample++){
                double angle=sample*2.399963229728653,r=5+(sample%40)*.5;
                double x=Math.Cos(angle)*r,z=Math.Sin(angle)*r-5,y=Circuit.Terrain.HeightAt(x,z);
                if(!Circuit.Collision.IsInside(x,z,1)||!Circuit.Queries.Clear(new Vec3(x,y,z),.5,1.6)
                    ||Circuit.Terrain.NormalAt(x,z).Y<.7)continue;
                game.Run.Spawn(0,x,z);i++;
            }
        }
        // Horda mixta QA de los circuitos B y C: la mitad pelusas, cucarachas, táperes y palomas, una
        // rata por cada 100 y el jefe desde 300. Puntos de terreno deterministas donde cabe el cuerpo
        // de cada tipo (nodo de su grafo); en C, repartidos por los 128 m (también sobre mesetas).
        // Sin tocar director ni reglas.
        public void SpawnPursuit(int count)
        {
            var run=game.Run;run.Enemies.Clear();if(count==0)return;var graphs=Graphs;
            int boss=count>=300?1:0,elites=count/100,placed=0;double spread=Circuit.Layout==VerticalLayout.Procedural?.75:.38;
            for(int sample=0;placed<count-boss&&sample<count*40;sample++){
                double angle=sample*2.399963229728653,r=4+(sample%60)*spread,x=Math.Cos(angle)*r,z=Math.Sin(angle)*r;
                int type=placed<elites?4:(placed%10<5?0:placed%10<7?1:placed%10<9?2:3);
                var g=graphs[NavClass.Of(Catalog.Enemies[type])];double y=Circuit.Terrain.HeightAt(x,z);
                int n=g.NodeAt(x,y+.01,z,.01);
                if(n<0||!g.NodeTerrain[n]||Math.Abs(g.SurfaceY(n,x,z)-y)>.01)continue;
                run.Spawn(type,x,z);placed++;
            }
            if(boss>0){BossStart(out double bx,out double bz);run.SpawnBoss(bx,bz);}
        }
        // B: (-12, 0) como siempre. C: nodo L de terreno con entrada y salida más cercano a (-14, 0).
        public void BossStart(out double x,out double z)
        {
            x=-12;z=0;if(Circuit.Layout!=VerticalLayout.Procedural)return;
            var g=Graphs[2];var to=ProceduralMap.Reach(g,procedural.Map.SpawnNode(2));var back=ProceduralMap.Reach(g,procedural.Map.SpawnNode(2),true);double best=double.MaxValue;
            for(int n=0;n<g.NodeCount;n++){if(!to[n]||!back[n]||!g.NodeTerrain[n]||Math.Abs(g.NodeY[n])>.01)continue;
                double dx=g.NodeX(n)+14,dz=g.NodeZ(n),d=dx*dx+dz*dz;if(d<best){best=d;x=g.NodeX(n);z=g.NodeZ(n);}}
        }
        public string Status=>Circuit.Layout==VerticalLayout.Persecucion?PursuitStatus:Circuit.Layout==VerticalLayout.Procedural?ProceduralStatus:ClimbingStatus;
        string ProceduralStatus{get{var m=procedural.Map;return
            "P0-C QA · circuito "+Circuit.Name+" · semilla "+m.Seed+" (F11 cambia) · F10 cambia de circuito · horda "+(Navigation!=null?"con navegación P0":"heredada")+"\n"+
            $"F4: 0/300/500/750 (mixta; jefe desde 300) · F7 rescate INVALIDA · F8 reinicio · mapa: intento {m.Layout.Attempt}, reparaciones {m.Repairs}, rechazos {m.Rejections}, {m.TotalMs:F0} ms\n"+
            (Navigation==null?"":$"escaladas {Navigation.ClimbStarts} · bordes {Navigation.LedgesDone} · caídas {Navigation.DropsStarted} · sueltas {Navigation.Detaches} · recálculos {Navigation.Fields[0].Recomputes+Navigation.Fields[1].Recomputes+Navigation.Fields[2].Recomputes}\n")+
            $"x {game.Body.X:F1}  y {game.Body.Y:F1}  z {game.Body.Z:F1} · enemigos {game.Run.Enemies.Count} · {Seconds:F1} s · SIN META";}}
        string PursuitStatus=>"P0-B QA · circuito "+Circuit.Name+" · F10 cambia de circuito · horda "+(Navigation!=null?"con navegación P0":"heredada")+"\n"+
            "F4: 0/300/500/750 (mixta; jefe desde 300) · F7 rescate INVALIDA · F8 reinicio\n"+
            (Navigation==null?"":$"escaladas {Navigation.ClimbStarts} · bordes {Navigation.LedgesDone} · caídas {Navigation.DropsStarted} · sueltas {Navigation.Detaches} · recálculos {Navigation.Fields[0].Recomputes+Navigation.Fields[1].Recomputes+Navigation.Fields[2].Recomputes}\n")+
            $"x {game.Body.X:F1}  y {game.Body.Y:F1}  z {game.Body.Z:F1} · enemigos {game.Run.Enemies.Count} · {Seconds:F1} s · SIN META";
        string ClimbingStatus=>"P0-A6 QA · circuito "+Circuit.Name+" · F10 cambia de circuito · Empuja pared: escalar · W/S vertical · A/D lateral · Espacio separar · Shift/C soltar\n"+
            "F4: 0/300/500/750 · F5: control U3/P0 · F7 rescate INVALIDA · F8 reinicio\n"+
            (Control?"CONTROL U3":Motion.State.ToString())+" · agarres "+Motion.Grabs+" · bordes "+Motion.LedgeSuccess+" · atascos "+Stalls+" · rescates "+Recoveries+"\n"+
            $"x {game.Body.X:F1}  y {game.Body.Y:F1}  z {game.Body.Z:F1} · {Seconds:F1} s · {Distance:F1} m · SIN META";
    }
}
