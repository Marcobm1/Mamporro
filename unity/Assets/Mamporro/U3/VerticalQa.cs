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
        public static bool Requested=>RequestedForTests||Array.IndexOf(Environment.GetCommandLineArgs(),"-p0-qa")>=0;
        public readonly VerticalCircuit Circuit=new VerticalCircuit();
        public VerticalMotion Motion {get;private set;}
        public VerticalIntent? ScriptedInput;
        public int Recoveries;
        public bool Control;
        float cameraDistance=float.PositiveInfinity;
        readonly U3Game game;
        public VerticalQa(U3Game game){this.game=game;}
        public static void RequireIsolatedSave()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-u4-save-dir");
            string path=U3Game.SaveDirectoryOverride??(i>=0&&i+1<args.Length?args[i+1]:null);
            if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException("P0 QA requiere -u4-save-dir aislado.");
            string full=Path.GetFullPath(path),personal=Path.GetFullPath(Application.persistentDataPath);
            if(full.Equals(personal,StringComparison.OrdinalIgnoreCase)||full.StartsWith(personal+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P0 QA no permite usar la carpeta personal.");
        }
        public void Bind()
        {
            game.Session.Automatic=false;game.Session.Cheated=true;
            Motion=new VerticalMotion(game.Body,Circuit.Collision,Circuit.Queries);Recoveries=0;ScriptedInput=null;
            cameraDistance=float.PositiveInfinity;
            game.Session.PhysicsStep=Step;
        }
        void Step(PlayerIntent movement,double dt,double speed,double crowd)
        {
            var k=Keyboard.current;
            var input=ScriptedInput??new VerticalIntent{Movement=movement,GrabHeld=Mouse.current!=null&&Mouse.current.rightButton.isPressed,
                WallHorizontal=k==null?0:(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),
                WallVertical=k==null?0:(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)};
            if(Control)PlayerPhysics.StepInCrowd(game.Body,movement,Circuit.Collision,PlayerTuning.Default,speed,crowd,dt);
            else Motion.Step(input,dt,speed,crowd);
            if(game.Body.Y< -10||!VerticalQueries.Finite(new Vec3(game.Body.X,game.Body.Y,game.Body.Z))){
                Recoveries++;game.Body.PlaceAt(Circuit.Start.X,Circuit.Start.Y,Circuit.Start.Z);Motion.Suspend();
                game.Hud.Notice("QA: recuperación; recorrido inválido",false);
            }
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
            float length=Vector3.Distance(pivot,camera),allowed=length;
            if(Circuit.Queries.Sweep(a,b,r,r*2,out var h))allowed=length*(float)Math.Max(0,h.Fraction-.002);
            cameraDistance=allowed<cameraDistance?allowed:Mathf.Lerp(cameraDistance,allowed,1-Mathf.Exp(-4*dt));
            return length<.0001f?pivot:Vector3.Lerp(pivot,camera,cameraDistance/length);
        }
        public void Spawn(int count)
        {
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
        public string Status=>"P0 QA · RMB agarrar · WASD pared · Espacio separar · Shift/C soltar\n"+
            "F4: 0/300/500/750 · F5: control U3/P0 · F8 reinicio · Esc pausa\n"+
            (Control?"CONTROL U3":Motion.State.ToString())+" · agarres "+Motion.Grabs+" · bordes "+Motion.LedgeSuccess+" · atascos "+Motion.Stalls+" · rescates "+Recoveries;
    }
}
