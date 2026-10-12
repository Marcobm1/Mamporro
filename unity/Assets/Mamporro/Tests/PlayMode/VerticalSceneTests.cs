using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Reflection;

namespace Mamporro.Tests
{
    public sealed class VerticalSceneTests
    {
        QaSave save;Keyboard keyboard;
        [SetUp] public void Setup(){save=QaSave.UseAllUnlocked();VerticalQa.RequestedForTests=true;}
        [TearDown] public void Teardown(){if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}VerticalQa.RequestedForTests=false;save.Dispose();}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;}
        [UnityTest] public IEnumerator KeyboardLeftRightMatchesScreenAtEveryWallAndPitch()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            keyboard=InputSystem.AddDevice<Keyboard>("Teclado A6 QA");
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            foreach(float pitch in new[]{-15f,20f,60f})foreach(var key in new[]{Key.A,Key.D}){
                g.PlaceQa(new Vec3(11+normal.x*3.402,2,normal.z*4.402));g.Body.Grounded=false;
                g.FaceTowards(11,0);typeof(U3Game).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,pitch);
                g.Vertical.ScriptedInput=new VerticalIntent{Movement=new PlayerIntent{MoveX=-normal.x,MoveZ=-normal.z}};
                for(int i=0;i<25;i++)g.Session.PhysicsStep(default,1.0/60,9.5,0);
                Assert.That(g.Vertical.Motion.State,Is.EqualTo(VerticalState.Climbing));
                g.Vertical.ScriptedInput=null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
                var before=new Vector3((float)g.Body.X,(float)g.Body.Y,-(float)g.Body.Z);
                g.Session.PhysicsStep(default,1.0/60,9.5,0);
                var after=new Vector3((float)g.Body.X,(float)g.Body.Y,-(float)g.Body.Z);
                // Oráculo independiente de WallRight: base de cámara Unity mirando la pared.
                var facing=new Vector3(-normal.x,0,normal.z);var right=Vector3.Cross(Vector3.up,facing);
                var rotation=Quaternion.LookRotation(facing)*Quaternion.Euler(pitch,0,0);
                Assert.That(Vector3.Dot(rotation*Vector3.right,right),Is.GreaterThan(.999f));
                Assert.That(Vector3.Dot(after-before,right),Is.EqualTo(key==Key.D?9.5f/60:-9.5f/60).Within(.00001));
                var pivot=g.Vertical.CameraPivot(after,after+Vector3.up*1.9f);
                var camera=g.Vertical.ClipCamera(pivot,pivot-rotation*Vector3.forward*6,1);
                Assert.That(g.Vertical.Circuit.Queries.Clear(new Vec3(camera.x,camera.y-.15,-camera.z),.15,.3),Is.True);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator KeyboardLeftRightFollowsTheCameraWhenTurnedAwayFromTheWall()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            keyboard=InputSystem.AddDevice<Keyboard>("Teclado A6 cámara");
            var yawField=typeof(U3Game).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            foreach(float offset in new[]{0f,45f,-45f,135f,-135f,180f})foreach(var key in new[]{Key.A,Key.D}){
                g.PlaceQa(new Vec3(11+normal.x*3.402,2,normal.z*4.402));g.Body.Grounded=false;
                g.Vertical.ScriptedInput=new VerticalIntent{Movement=new PlayerIntent{MoveX=-normal.x,MoveZ=-normal.z}};
                for(int i=0;i<25;i++)g.Session.PhysicsStep(default,1.0/60,9.5,0);
                Assert.That(g.Vertical.Motion.State,Is.EqualTo(VerticalState.Climbing));
                g.Vertical.ScriptedInput=null;
                // Cámara girada con el ratón respecto a la vista frontal de la pared.
                g.FaceTowards(11,0);float yaw=(float)yawField.GetValue(g)+offset;yawField.SetValue(g,yaw);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
                var before=new Vector3((float)g.Body.X,(float)g.Body.Y,-(float)g.Body.Z);
                g.Session.PhysicsStep(default,1.0/60,9.5,0);
                var after=new Vector3((float)g.Body.X,(float)g.Body.Y,-(float)g.Body.Z);
                // Oráculo Unity: derecha de la cámara real y tangente horizontal de la pared.
                var cameraRight=Quaternion.Euler(0,yaw,0)*Vector3.right;
                var tangent=Vector3.Cross(Vector3.up,new Vector3(-normal.x,0,normal.z));
                float visible=Mathf.Abs(Vector3.Dot(cameraRight,tangent));
                Assert.That(Vector3.Dot(after-before,cameraRight),Is.EqualTo((key==Key.D?9.5f/60:-9.5f/60)*visible).Within(.0001),
                    $"pared {normal}, giro {offset}°, {key}");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ClimbSpeedFollowsMoveSpeedStatThroughTheRealRun()
        {
            // Tick real de WorldRun: PlayerBaseMoveSpeed*Stats[moveSpeed] y CrowdSlow llegan a la
            // escalada igual que a la carrera. Mejora aplicada, ampliada y retirada en caliente.
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            Assert.That(g.Run.Paused,Is.False);
            g.PlaceQa(new Vec3(11+3.402,2,0));g.Body.Grounded=false;
            g.Vertical.ScriptedInput=new VerticalIntent{Movement=new PlayerIntent{MoveX=-1}};
            for(int i=0;i<25;i++)g.Session.PhysicsStep(default,1.0/60,9.5,0);
            Assert.That(g.Vertical.Motion.State,Is.EqualTo(VerticalState.Climbing));
            // A media altura de la pared, lejos del suelo y del techo.
            g.Vertical.ScriptedInput=new VerticalIntent{WallVertical=1};
            for(int i=0;i<16;i++)g.Session.PhysicsStep(default,1.0/60,9.5,0);
            Assert.That(g.Body.Y,Is.GreaterThan(2));
            int tick=0;
            foreach(var (stat,crowd) in new[]{(1.0,0.0),(1.26,0.0),(1.8,0.0),(1.8,.25),(1.0,0.0)}){
                g.Run.Stats[Stat.moveSpeed]=stat;g.Run.CrowdSlow=crowd;
                g.Vertical.ScriptedInput=new VerticalIntent{WallVertical=tick++%2==0?1:-1};
                double y=g.Body.Y;g.Session.Step(default,1.0/60);
                Assert.That(g.Vertical.Motion.State,Is.EqualTo(VerticalState.Climbing),
                    $"estadística {stat}: {g.Body.X:F3},{g.Body.Y:F3},{g.Body.Z:F3} {g.Vertical.Motion.LastFailure}");
                Assert.That(System.Math.Abs(g.Body.Y-y),Is.EqualTo(PlayerPhysics.EffectiveSpeed(Tuning.PlayerBaseMoveSpeed*stat,crowd)/60).Within(1e-9),
                    $"estadística {stat}, horda {crowd}");
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CircuitBTogglesNavigationAndRunsTheMixedHorde()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Layout,Is.EqualTo(VerticalLayout.Escalada));Assert.That(g.Vertical.Navigation,Is.Null);
            // Lo mismo que F10: circuito B, mundo recargado y partida nueva con navegación P0.
            g.Vertical.ToggleLayout();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Layout,Is.EqualTo(VerticalLayout.Persecucion));
            Assert.That(g.Vertical.Navigation,Is.Not.Null);Assert.That(g.Run.Enemies.Navigation,Is.SameAs(g.Vertical.Navigation));
            Assert.That(g.Vertical.Status,Does.Contain("B · persecución vertical"));
            g.Vertical.Spawn(300);Assert.That(g.Run.Enemies.Count,Is.EqualTo(300));Assert.That(g.Run.Boss,Is.Not.Null,"jefe desde 300");
            g.Run.Invincible=true;g.Run.WeaponsOff=true;g.Vertical.ScriptedInput=default(VerticalIntent);
            for(int i=0;i<120;i++)g.Session.Step(default,1.0/60);
            var e=g.Run.Enemies;var q=g.Vertical.Circuit.Queries;
            for(int i=0;i<e.Count;i++)Assert.That(q.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),e.Def(i).radius,e.Def(i).height,NavGraph.Step),Is.True,$"enemigo {i}");
            // P0-C: F10 sigue al circuito C y después vuelve al A.
            g.Vertical.ToggleLayout();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Layout,Is.EqualTo(VerticalLayout.Procedural));Assert.That(g.Vertical.Navigation,Is.Not.Null);
            g.Vertical.ToggleLayout();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Layout,Is.EqualTo(VerticalLayout.Escalada));Assert.That(g.Vertical.Navigation,Is.Null);Assert.That(g.Run.Enemies.Navigation,Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CircuitCRunsTheValidatedMapAndF11CyclesSeeds()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            // Lo mismo que F11 desde el circuito A: circuito C con la primera semilla QA.
            g.Vertical.NextSeed();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            var c=g.Vertical.Circuit;
            Assert.That(c.Layout,Is.EqualTo(VerticalLayout.Procedural));Assert.That(c.Seed,Is.EqualTo("P0QA"));Assert.That(c.Map.Check.Passed,Is.True);
            Assert.That(g.World.Collision,Is.SameAs(c.Collision));Assert.That(g.Vertical.Navigation.Graphs,Is.SameAs(c.Map.Graphs),"grafos de la validación reutilizados");
            Assert.That(g.Vertical.Status,Does.Contain("C · procedural"));Assert.That(g.Vertical.Status,Does.Contain("semilla P0QA"));
            Assert.That(g.Body.X,Is.EqualTo(c.Start.X));Assert.That(g.Body.Z,Is.EqualTo(c.Start.Z));
            g.Vertical.Spawn(300);Assert.That(g.Run.Enemies.Count,Is.EqualTo(300));Assert.That(g.Run.Boss,Is.Not.Null,"jefe desde 300");
            g.Run.Invincible=true;g.Run.WeaponsOff=true;g.Vertical.ScriptedInput=default(VerticalIntent);
            for(int i=0;i<120;i++)g.Session.Step(default,1.0/60);
            var e=g.Run.Enemies;
            for(int i=0;i<e.Count;i++)Assert.That(c.Queries.ClearAboveStep(new Vec3(e.X[i],e.Y[i],e.Z[i]),e.Def(i).radius,e.Def(i).height,NavGraph.Step),Is.True,$"enemigo {i}");
            g.Vertical.NextSeed();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Seed,Is.EqualTo("MAMPORRO"));Assert.That(g.Vertical.Circuit,Is.Not.SameAs(c));Assert.That(g.Vertical.Status,Does.Contain("semilla MAMPORRO"));
            g.Vertical.ToggleLayout();g.LoadWorld(g.Seed);g.StartRun(g.Seed);g.SetPaused(true);
            Assert.That(g.Vertical.Circuit.Layout,Is.EqualTo(VerticalLayout.Escalada));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllPitSidesAndPlateauSidesCompleteWithCameraAndNoRescue()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){
                g.StartRun("P0QA");g.SetPaused(true);
                g.PlaceQa(new Vec3(19,2,11));g.Body.Grounded=false;
                for(int i=0;i<180&&!g.Body.Grounded;i++){g.Vertical.ScriptedInput=default(VerticalIntent);g.Session.PhysicsStep(default,1.0/60,9.5,0);}
                Assert.That(g.Body.Y,Is.EqualTo(-6).Within(.01));
                yield return CompleteRoute(g,direction.x,direction.z);
            }
            foreach(double side in new[]{-1.0,1.0}){
                g.StartRun("P0QA");g.SetPaused(true);
                g.PlaceQa(new Vec3(-8,0,side*7));yield return CompleteRoute(g,0,-side);
            }
            LogAssert.NoUnexpectedReceived();
        }
        static IEnumerator CompleteRoute(U3Game g,double dx,double dz)
        {
            var q=g.Vertical;
            for(int i=0;i<400&&q.Motion.LedgeSuccess==0;i++){
                q.ScriptedInput=new VerticalIntent{Movement=new PlayerIntent{MoveX=dx,MoveZ=dz},WallVertical=1};g.Session.PhysicsStep(default,1.0/60,9.5,0);
                var b=g.Body;var player=new Vector3((float)b.X,(float)b.Y,-(float)b.Z);
                var pivot=q.CameraPivot(player,player+Vector3.up*1.9f);
                var camera=q.ClipCamera(pivot,pivot+new Vector3((float)-dx*6,2,(float)dz*6));
                Assert.That(q.Circuit.Queries.Clear(new Vec3(camera.x,camera.y-.15,-camera.z),.15,.3),Is.True);
            }
            Assert.That(q.Motion.LedgeSuccess,Is.EqualTo(1));Assert.That(q.Recoveries,Is.Zero);Assert.That(q.InvalidRoute,Is.False);
            yield break;
        }
        [UnityTest] public IEnumerator CircuitIsOptInAndUsesExistingSession()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            Assert.That(g.Vertical,Is.Not.Null);Assert.That(g.Session.Automatic,Is.False);Assert.That(g.Session.Cheated,Is.True);
            Assert.That(g.World.Collision,Is.SameAs(g.Vertical.Circuit.Collision));Assert.That(g.Session.PhysicsStep,Is.Not.Null);
            g.Vertical.Spawn(300);Assert.That(g.Run.Enemies.Count,Is.EqualTo(300));
            for(int i=0;i<300;i++)Assert.That(g.Vertical.Circuit.Queries.Clear(new Vec3(g.Run.Enemies.X[i],g.Run.Enemies.Y[i],g.Run.Enemies.Z[i]),.5,1.6),Is.True);
            g.BackToTitle();Assert.That(g.Run.Enemies.Count,Is.Zero);Assert.That(g.Vertical.Motion.Grabs,Is.Zero);
            VerticalQa.RequestedForTests=false;yield return Load();
            Assert.That(Game.Vertical,Is.Null);Assert.That(Game.Session.PhysicsStep,Is.Null);Assert.That(Game.Session.Automatic,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseClearsGripAndCameraCannotCrossSolid()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            g.Body.PlaceAt(7.598,1,-2);g.Body.Grounded=false;g.Body.Facing=-System.Math.PI/2;
            var m=g.Vertical.Motion;
            for(int i=0;i<24;i++)m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1}},1.0/60);
            Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
            g.SetPaused(true);Assert.That(m.Armed,Is.False);Assert.That(m.State,Is.EqualTo(VerticalState.Air));
            var pivot=new Vector3(7,2,2);var camera=new Vector3(16,2,2);
            var clipped=g.Vertical.ClipCamera(pivot,camera);Assert.That(clipped.x,Is.LessThan(7.86f));
            var free=g.Vertical.ClipCamera(pivot,new Vector3(0,2,2));
            Assert.That(Vector3.Distance(pivot,free),Is.GreaterThan(Vector3.Distance(pivot,clipped)));
            Assert.That(Vector3.Distance(pivot,free),Is.LessThan(2),"recuperación gradual");
            var low=g.Vertical.CameraPivot(new Vector3(7.598f,5.4f,0),new Vector3(7.598f,7.3f,0));
            Assert.That(low.y,Is.LessThan(6.86f),"el pivote tampoco entra en el techo");
            var under=g.Vertical.ClipCamera(new Vector3(7.598f,6.7f,0),new Vector3(1.598f,8.7f,0),1);
            Assert.That(under.y,Is.LessThanOrEqualTo(6.7f));Assert.That(under.x,Is.LessThan(3),"brazo libre bajo el techo, sin encerrar al avatar");
            g.Vertical.Control=true;Assert.That(g.Vertical.ClipCamera(pivot,camera),Is.EqualTo(camera));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RoofDepartureCountsOneFallAfterLosingHeight()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);g.PlaceQa(new Vec3(13,6,3));
            g.Vertical.ScriptedInput=new VerticalIntent{Movement=new PlayerIntent{MoveX=1}};
            for(int i=0;i<110;i++){if(g.Body.X>=16)g.Vertical.ScriptedInput=default(VerticalIntent);g.Session.PhysicsStep(default,1.0/60,9.5,0);}
            Assert.That(g.Body.Y,Is.EqualTo(0).Within(.01));Assert.That(g.Vertical.Falls,Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PitFallFloorClimbJumpAndExitKeepCameraClear()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);g.PlaceQa(new Vec3(19,2,11));
            g.Body.Grounded=false;var q=g.Vertical;var m=q.Motion;
            for(int i=0;i<150&&!g.Body.Grounded;i++){q.ScriptedInput=default(VerticalIntent);g.Session.PhysicsStep(default,1.0/60,9.5,0);}
            Assert.That(g.Body.Y,Is.EqualTo(-6).Within(.01));Assert.That(g.Body.Grounded,Is.True);
            bool jumped=false;int cameraSamples=0;
            for(int i=0;i<400&&m.LedgeSuccess==0;i++){
                var input=new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1};
                if(!jumped&&m.State==VerticalState.Climbing&&g.Body.Y> -4){input.Movement.JumpPressed=true;jumped=true;}
                q.ScriptedInput=input;g.Session.PhysicsStep(default,1.0/60,9.5,0);
                var b=g.Body;var player=new Vector3((float)b.X,(float)b.Y,-(float)b.Z);
                var pivot=q.CameraPivot(player,player+Vector3.up*1.9f);
                var camera=q.ClipCamera(pivot,pivot+new Vector3(-6,2,0));
                Assert.That(q.Circuit.Queries.Clear(new Vec3(camera.x,camera.y-.15,-camera.z),.15,.3),Is.True);
                cameraSamples++;
            }
            Assert.That(jumped,Is.True);Assert.That(cameraSamples,Is.GreaterThan(50));Assert.That(m.LedgeSuccess,Is.EqualTo(1));
            Assert.That(q.Recoveries,Is.Zero);Assert.That(q.UnintendedRegrabs,Is.Zero);Assert.That(q.InvalidRoute,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LedgeFocusDeathAndRestartKeepQaIsolated()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            g.Body.PlaceAt(7.598,1,-2);g.Body.Grounded=false;g.Body.Facing=-System.Math.PI/2;
            var m=g.Vertical.Motion;m.Step(default,1.0/60);
            for(int i=0;i<140&&m.LedgeSuccess==0;i++){
                m.Step(new VerticalIntent{Movement=new PlayerIntent{MoveX=1},WallVertical=1},1.0/60);
                Assert.That(g.Vertical.Circuit.Queries.Clear(new Vec3(g.Body.X,g.Body.Y,g.Body.Z),.4,1.55),Is.True);
            }
            Assert.That(m.LedgeSuccess,Is.EqualTo(1));
            g.SetPaused(false);g.SendMessage("OnApplicationFocus",false);Assert.That(g.Paused,Is.True);Assert.That(m.Armed,Is.False);
            g.SendMessage("OnApplicationFocus",true);Assert.That(g.Paused,Is.True);
            g.SetPaused(false);g.Run.Hurt(10000);g.SendMessage("FixedUpdate");
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Results));Assert.That(g.LastSettlement,Is.Null);
            Assert.That(g.Vertical.Motion.Armed,Is.False);
            g.BackToTitle();Assert.That(g.Vertical.Motion.Grabs,Is.Zero);Assert.That(g.Vertical.Recoveries,Is.Zero);
            Assert.That(g.Body.X,Is.EqualTo(g.Vertical.Circuit.Start.X));
            g.Vertical.Recover();Assert.That(g.Vertical.InvalidRoute,Is.True);Assert.That(g.Vertical.Recoveries,Is.EqualTo(1));
            Assert.That(g.LastSettlement,Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
