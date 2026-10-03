using System;
using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Mamporro.Tests
{
    // Paso 10 (Play Mode): partidas aceleradas en la escena real, 2 personajes × 3 duraciones,
    // desde el inicio hasta los resultados pasando por el enjambre final. Remedios gana por el
    // armario (jefe derrotado) y Baguette cae en el enjambre; reintentar no deja restos.
    public sealed class U3RunFlowTests
    {
        U3Game g;
        [TearDown]public void RestoreTime(){Time.timeScale=1;}

        // Espera (tiempo real) hasta que se cumpla la condición; elige la primera carta si se abre.
        IEnumerator Until(Func<bool> done,float seconds,string what)
        {
            float end=Time.realtimeSinceStartup+seconds;
            while(!done()){
                if(g.Run.Choosing){g.Choose(0);while(g.Run.Choosing)g.Choose(0);}
                if(Time.realtimeSinceStartup>end)Assert.Fail("Tiempo agotado esperando: "+what);
                yield return null;
            }
        }
        int Count(string kind){int n=0;foreach(var e in g.Run.Events)if(e.Kind==kind)n++;return n;}

        [UnityTest][Timeout(600000)]
        public IEnumerator TwoCharactersThreeDurationsReachResultsThroughTheSwarm()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            g=Object.FindAnyObjectByType<U3Game>();int children=g.transform.childCount;
            for(int c=0;c<2;c++)foreach(int minutes in Catalog.RunDurations){
                string run=$"{Catalog.Characters[c].id} {minutes} min";
                g.BackToTitle();g.Character=Catalog.Characters[c];g.Minutes=minutes;g.StartRun("");
                var s=g.Session;var r=g.Run;
                Assert.That(g.State,Is.EqualTo(U3Game.Screen.Playing),run);Assert.That(s.Director.Minutes,Is.EqualTo(minutes),run);
                Assert.That(r.Character.id,Is.EqualTo(Catalog.Characters[c].id),run);
                g.QaAction(1);Time.timeScale=10;
                // Primeros 20 s de juego real: el director hace aparecer enemigos y las armas matan.
                yield return Until(()=>r.Time>=20,15,run+" 20 s");
                Assert.That(s.Spawned,Is.GreaterThan(5),run);Assert.That(r.Kills,Is.GreaterThan(0),run);
                Assert.That(g.Hud.TimerText,Does.Match("^[0-9][0-9]:[0-9][0-9]$"),run);
                // Salto hasta el último minuto y llegada al enjambre final.
                for(int k=0;k<minutes-1;k++)s.DebugSkipMinute();
                yield return Until(()=>s.Swarm&&s.TimeLeft<=-25,30,run+" enjambre");
                Assert.That(Count("swarm"),Is.EqualTo(1),run);Assert.That(Count("wave"),Is.EqualTo(6),run+" seis oleadas");
                Assert.That(s.Interactables.Find("portal").Discovered,Is.True,run+" armario revelado");
                Assert.That(s.Params.MaxAlive,Is.EqualTo(750),run);Assert.That(r.Enemies.Count,Is.GreaterThan(200),run+" vivos en el enjambre");
                Assert.That(g.Hud.TimerText,Does.StartWith("+"),run);
                if(c==0&&minutes==5){
                    yield return Until(()=>r.Enemies.Count>=750,30,run+" 750 vivos");
                    Assert.That(r.Enemies.Count,Is.EqualTo(750),run);
                }
                if(c==0){
                    // Victoria: al armario, E, y la Pelusa Madre cae.
                    var portal=s.Interactables.Find("portal");
                    g.Body.PlaceAt(portal.Spot.X+1.6,g.World.Collision.GroundHeight(portal.Spot.X+1.6,portal.Spot.Z,double.PositiveInfinity),portal.Spot.Z);s.SyncPlayer();
                    Assert.That(s.Interact(),Is.True,run+" abrir el armario");Assert.That(portal.Used,Is.True);
                    int boss=r.Enemies.IndexOf(r.Boss.EnemyId);r.Enemies.Hp[boss]=0;
                    yield return Until(()=>g.State==U3Game.Screen.Results,10,run+" resultados");
                    Assert.That(r.Victory,Is.True,run);Assert.That(g.Screens.ResultsText,Does.Contain("¡Victoria!"),run);
                } else {
                    // Derrota: sin invencibilidad y con poca vida en mitad del enjambre.
                    g.QaAction(1);r.Hp=5;
                    yield return Until(()=>g.State==U3Game.Screen.Results,20,run+" derrota");
                    Assert.That(r.Dead,Is.True,run);Assert.That(g.Screens.ResultsText,Does.Contain("¡A la cama sin cenar!"),run);
                }
                Assert.That(g.Screens.ResultsText,Does.Contain("Partida con trucos de debug"),run);Assert.That(g.Hud.Visible,Is.False,run);
                Time.timeScale=1;
                // Reintentar: partida nueva, mismo personaje y duración, sin restos de la anterior.
                var old=s;g.Retry(false);var fresh=g.Session;var f=g.Run;
                Assert.That(fresh,Is.Not.SameAs(old),run);Assert.That(fresh.Director.Minutes,Is.EqualTo(minutes),run);Assert.That(f.Character.id,Is.EqualTo(Catalog.Characters[c].id),run);
                Assert.That(f.Enemies.Count+f.Projectiles.Count+f.EnemyShots.Count+f.Gems.Count+f.Coins.Count+f.Kills+f.Events.Count,Is.Zero,run+" sin restos");
                Assert.That(f.Boss,Is.Null);Assert.That(fresh.Swarm||fresh.Cheated||f.Victory||f.Dead,Is.False,run);Assert.That(f.Time,Is.Zero);
                foreach(var i in fresh.Interactables.List)Assert.That(i.Used||i.Discovered,Is.False,run+" interactuables nuevos");
                Assert.That(g.CombatView.EffectCount,Is.Zero,run);Assert.That(g.Screens.ResultsVisible,Is.False,run);
                yield return new WaitForSeconds(.2f);Assert.That(f.Time,Is.GreaterThan(0),run);Assert.That(f.Kills,Is.LessThan(5),run);
                g.SetPaused(true);
                yield return null;Assert.That(g.transform.childCount,Is.EqualTo(children),run+" sin objetos de escena acumulados");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
