using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    public sealed class VerticalSceneTests
    {
        QaSave save;
        [SetUp] public void Setup(){save=QaSave.UseAllUnlocked();VerticalQa.RequestedForTests=true;}
        [TearDown] public void Teardown(){VerticalQa.RequestedForTests=false;save.Dispose();}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;}
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
            var m=g.Vertical.Motion;m.Step(default,1.0/60);m.Step(new VerticalIntent{GrabHeld=true},1.0/60);Assert.That(m.State,Is.EqualTo(VerticalState.Climbing));
            g.SetPaused(true);Assert.That(m.Armed,Is.False);Assert.That(m.State,Is.EqualTo(VerticalState.Air));
            var pivot=new Vector3(7,2,2);var camera=new Vector3(16,2,2);
            var clipped=g.Vertical.ClipCamera(pivot,camera);Assert.That(clipped.x,Is.LessThan(7.86f));
            var free=g.Vertical.ClipCamera(pivot,new Vector3(0,2,2));
            Assert.That(Vector3.Distance(pivot,free),Is.GreaterThan(Vector3.Distance(pivot,clipped)));
            Assert.That(Vector3.Distance(pivot,free),Is.LessThan(2),"recuperación gradual");
            var low=g.Vertical.CameraPivot(new Vector3(7.598f,5.4f,0),new Vector3(7.598f,7.3f,0));
            Assert.That(low.y,Is.LessThan(6.86f),"el pivote tampoco entra en el techo");
            g.Vertical.Control=true;Assert.That(g.Vertical.ClipCamera(pivot,camera),Is.EqualTo(camera));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LedgeFocusDeathAndRestartKeepQaIsolated()
        {
            yield return Load();var g=Game;g.StartRun("P0QA");g.SetPaused(true);
            g.Body.PlaceAt(7.598,1,-2);g.Body.Grounded=false;g.Body.Facing=-System.Math.PI/2;
            var m=g.Vertical.Motion;m.Step(default,1.0/60);
            for(int i=0;i<140;i++){
                m.Step(new VerticalIntent{GrabHeld=true,WallVertical=1},1.0/60);
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
            LogAssert.NoUnexpectedReceived();
        }
    }
}
