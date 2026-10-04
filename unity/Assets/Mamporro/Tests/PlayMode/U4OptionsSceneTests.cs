using System.Collections;
using System.IO;
using System.Text;
using Mamporro.Core.Progress;
using Mamporro.U2;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mamporro.Tests
{
    // Paso 8 de U4: las 14 opciones guardadas en el menú y en la pausa, aplicadas al momento y
    // conservadas al recargar (carpeta temporal QaSave; nunca el guardado del autor).
    public sealed class U4OptionsSceneTests
    {
        QaSave qa;Keyboard keyboard;
        [TearDown]public void Release()
        {
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            CombatText.English=false;qa?.Dispose();qa=null;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(){yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;yield return null;}
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        // Pulsa el primer botón visible con ese texto, como un clic.
        static void Click(Component root,string label)
        {
            foreach(var b in root.GetComponentsInChildren<Button>())
                if(b.interactable&&b.GetComponentInChildren<Text>().text==label){b.onClick.Invoke();return;}
            Assert.Fail("No hay botón «"+label+"»");
        }
        static readonly int SnapId=Shader.PropertyToID("_RetroSnap"),DitherId=Shader.PropertyToID("_RetroDither");

        [UnityTest]public IEnumerator MenuOptionsApplyImmediatelyAndSurviveARestart()
        {
            qa=QaSave.Use(ProgressDto.New("es"));yield return Load();var g=Game;var s=g.Screens;
            s.OpenPage(RunScreens.Options);yield return null;
            Assert.That(s.TitleText,Does.Contain("Idioma").And.Contain("Sensibilidad del ratón").And.Contain("Resolución interna").And.Contain("Temblor de vértices (PS1)")
                .And.Contain("Tramado de color (dithering)").And.Contain("Mostrar FPS").And.Contain("Reducir partículas").And.Contain("Sacudidas de cámara")
                .And.Contain("Destellos de daño").And.Contain("Usar Ctrl para deslizarse").And.Contain("Duración").And.Contain("Volumen de música")
                .And.Contain("Volumen de efectos").And.Contain("Silenciar audio").And.Contain("1.0").And.Contain("50 %").And.Contain("70 %"));
            // Sensibilidad: el mismo movimiento gira el doble con 2,0 (CameraRig.look).
            float before=(float)g.WebYaw;g.Look(new Vector2(100,0));float once=(float)g.WebYaw-before;
            Assert.That(g.ChangeSettings(o=>o.mouseSensitivity=2).Saved,Is.True);
            before=(float)g.WebYaw;g.Look(new Vector2(100,0));Assert.That((float)g.WebYaw-before,Is.EqualTo(once*2).Within(1e-4));
            // Resolución interna con el botón real del menú.
            Click(s,"240 px");yield return Frames(2);
            Assert.That(g.InternalHeight,Is.EqualTo(240));Assert.That(g.worldCamera.targetTexture.height,Is.EqualTo(240));Assert.That(qa.Stored.settings.renderHeight,Is.EqualTo(240));
            Click(s,"480 px");yield return Frames(2);Assert.That(g.worldCamera.targetTexture.height,Is.EqualTo(480));
            // Dithering, vértices, FPS, Ctrl, partículas, sacudida, destellos y audio.
            g.ChangeSettings(o=>{o.dithering=false;o.vertexSnap=false;o.showFps=true;o.slideWithCtrl=true;o.reducedParticles=true;o.cameraShake=false;o.musicVolume=.35;o.effectsVolume=0;o.muted=true;});
            yield return Frames(2);
            Assert.That(g.Dither,Is.False);Assert.That(g.Snap,Is.False);Assert.That(Shader.GetGlobalFloat(DitherId),Is.Zero);Assert.That(Shader.GetGlobalFloat(SnapId),Is.Zero);
            yield return new WaitForSecondsRealtime(.3f);Assert.That(s.FpsVisible,Is.True);Assert.That(s.FpsText,Does.EndWith(" FPS"));
            s.OpenPage(RunScreens.Options);yield return null;Assert.That(s.TitleText,Does.Contain("35 %").And.Contain("0 %").And.Contain("2.0"));
            // Ctrl para deslizarse: solo con la opción; Shift/C siempre.
            keyboard=InputSystem.AddDevice<Keyboard>("Teclado QA");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));InputSystem.Update();
            Assert.That(U3Game.SlideHeld(keyboard,false),Is.False);Assert.That(U3Game.SlideHeld(keyboard,true),Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));InputSystem.Update();Assert.That(U3Game.SlideHeld(keyboard,false),Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            // Atajo QA F2: cambia la misma opción guardada (sin desajuste entre activo y guardado).
            // (En batch, sin foco, wasPressedThisFrame no es fiable con eventos inyectados: se prueba la acción.)
            g.PresentationShortcut(Key.F2);yield return null;
            Assert.That(g.Dither,Is.True);Assert.That(g.Settings.dithering,Is.True);Assert.That(qa.Stored.settings.dithering,Is.True);
            g.PresentationShortcut(Key.F1);Assert.That(g.InternalHeight,Is.EqualTo(240));Assert.That(qa.Stored.settings.renderHeight,Is.EqualTo(240));
            g.PresentationShortcut(Key.F1);Assert.That(g.InternalHeight,Is.EqualTo(360));g.PresentationShortcut(Key.F1);Assert.That(g.InternalHeight,Is.EqualTo(480));
            g.PresentationShortcut(Key.F9);Assert.That(g.Snap,Is.True);g.PresentationShortcut(Key.F9);Assert.That(g.Snap,Is.False);Assert.That(qa.Stored.settings.vertexSnap,Is.False);
            InputSystem.RemoveDevice(keyboard);keyboard=null;
            var stored=qa.Stored.settings;
            Assert.That(stored.showFps&&stored.slideWithCtrl&&stored.reducedParticles&&!stored.cameraShake&&stored.muted&&!stored.vertexSnap,Is.True);
            Assert.That(stored.musicVolume,Is.EqualTo(.35));Assert.That(stored.effectsVolume,Is.Zero);Assert.That(stored.mouseSensitivity,Is.EqualTo(2));
            // Duración desde Opciones: se guarda y la usa la partida siguiente.
            Click(s,"15 min");yield return null;Assert.That(qa.Stored.settings.runMinutes,Is.EqualTo(15));
            // Reinicio: escena nueva, progreso cargado y efectos activos según lo guardado.
            yield return Load();g=Game;s=g.Screens;
            Assert.That(g.InternalHeight,Is.EqualTo(480));Assert.That(g.worldCamera.targetTexture.height,Is.EqualTo(480));Assert.That(g.Dither,Is.True);Assert.That(g.Snap,Is.False);
            Assert.That(g.Settings.mouseSensitivity,Is.EqualTo(2));Assert.That(g.Minutes,Is.EqualTo(15));
            yield return new WaitForSecondsRealtime(.3f);Assert.That(s.FpsVisible,Is.True);
            g.StartRun("");Assert.That(g.Session.Director.Minutes,Is.EqualTo(15));
            // Destellos de daño: sin la opción no hay destello rojo al recibir un golpe.
            g.ChangeSettings(o=>o.flashes=false);g.Run.Invulnerable=0;g.Run.Hurt(5);yield return null;Assert.That(g.Hud.HurtFlashing,Is.False);
            g.ChangeSettings(o=>o.flashes=true);g.Run.Invulnerable=0;g.Run.Hurt(5);yield return null;Assert.That(g.Hud.HurtFlashing,Is.True);
            g.SetPaused(true);g.BackToTitle();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator PauseOptionsKeepTheRunPausedAndLanguageRefreshesEverything()
        {
            var p=QaSave.AllUnlocked();p.meta.coins=42;qa=QaSave.Use(p);yield return Load();var g=Game;
            g.StartRun("");yield return new WaitForSeconds(.2f);
            // Perder el foco pausa la partida; recuperarlo no la reanuda.
            g.SendMessage("OnApplicationFocus",false);yield return Frames(2);Assert.That(g.Paused,Is.True);Assert.That(g.Screens.PauseVisible,Is.True);
            g.SendMessage("OnApplicationFocus",true);yield return Frames(2);Assert.That(g.Paused,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
            double time=g.Run.Time;int kills=g.Run.Kills;
            Click(g.Screens,"Opciones");yield return null;Assert.That(g.Screens.PauseOptionsVisible,Is.True);
            Assert.That(g.Screens.PauseAllText,Does.Contain("Sensibilidad del ratón").And.Contain("Volumen de música").And.Not.Contain("Duración"),"la duración no se cambia en partida");
            Click(g.Screens,"240 px");g.ChangeSettings(o=>o.slideWithCtrl=true);yield return Frames(2);
            Assert.That(g.InternalHeight,Is.EqualTo(240));Assert.That(g.Paused,Is.True);Assert.That(g.Screens.PauseOptionsVisible,Is.True);
            // Idioma durante la pausa: todo lo abierto pasa al inglés, sin restos del español.
            Click(g.Screens,"English");yield return Frames(2);
            Assert.That(g.Screens.PauseVisible,Is.True);Assert.That(g.Screens.PauseOptionsVisible,Is.True);Assert.That(g.Paused,Is.True);
            string pause=g.Screens.PauseAllText;
            Assert.That(pause,Does.Contain("Paused").And.Contain("Resume").And.Contain("Mouse sensitivity").And.Contain("Close options").And.Contain("Shift / C / Ctrl").And.Contain("Health"));
            Assert.That(pause,Does.Not.Contain("Continuar").And.Not.Contain("Sensibilidad").And.Not.Contain("Estadísticas").And.Not.Contain("Volver al inicio").And.Not.Contain("Semilla").And.Not.Contain("Objetos"));
            Assert.That(g.Hud.AllText,Does.Contain("Kills:").And.Contain("Lv ").And.Not.Contain("Bajas").And.Not.Contain("Nv "));
            // Cerrar las opciones deja la partida en pausa hasta Continuar.
            Click(g.Screens,"Close options");yield return new WaitForSeconds(.3f);
            Assert.That(g.Screens.PauseOptionsVisible,Is.False);Assert.That(g.Screens.PauseVisible,Is.True);Assert.That(g.Paused,Is.True);
            Assert.That(g.Run.Time,Is.EqualTo(time),"el tiempo no avanza");Assert.That(g.Run.Kills,Is.EqualTo(kills));
            Click(g.Screens,"Resume");yield return new WaitForSeconds(.2f);Assert.That(g.Paused,Is.False);Assert.That(g.Run.Time,Is.GreaterThan(time));
            g.SetPaused(true);yield return Frames(2);Click(g.Screens,"Back to title");Click(g.Screens,"Abandon run");yield return Frames(2);
            Assert.That(g.State,Is.EqualTo(U3Game.Screen.Title));Assert.That(g.Screens.TitleText,Does.Contain("Play").And.Not.Contain("Jugar"));
            var stored=qa.Stored;Assert.That(stored.meta.coins,Is.EqualTo(42),"las opciones no tocan la meta");Assert.That(g.LastSettlement,Is.Null);
            Assert.That(stored.settings.language,Is.EqualTo("en"));Assert.That(stored.settings.renderHeight,Is.EqualTo(240));Assert.That(stored.settings.slideWithCtrl,Is.True);
            yield return Load();Assert.That(CombatText.English,Is.True);Assert.That(Game.InternalHeight,Is.EqualTo(240));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]public IEnumerator ImportedOptionsApplyWithoutRestarting()
        {
            qa=QaSave.Use(ProgressDto.New("es"));yield return Load();var g=Game;
            var web=ProgressDto.New("es");var o=web.settings;o.language="en";o.renderHeight=480;o.mouseSensitivity=2.5;o.dithering=false;o.vertexSnap=false;o.showFps=true;o.runMinutes=5;o.slideWithCtrl=true;
            string file=Path.Combine(qa.Root,"mamporro-progreso.json");
            File.WriteAllText(file,ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,"source",ProgressTree.Object("platform","web","saveVersion",3),"progress",ProgressTree.From(web))),new UTF8Encoding(false));
            g.Screens.OpenPage(RunScreens.Options);g.Screens.ShowImport(true);g.Screens.ImportPath=file;g.Screens.ReviewImport();Assert.That(g.Screens.ImportCanConfirm,Is.True);
            g.Screens.ConfirmImport();yield return Frames(2);
            Assert.That(CombatText.English,Is.True);Assert.That(g.Screens.ImportVisible,Is.True,"la importación sigue abierta tras el cambio de idioma");
            Assert.That(g.Screens.ImportMessage,Is.EqualTo(CombatText.Get("import.done")));
            Assert.That(g.InternalHeight,Is.EqualTo(480));Assert.That(g.Dither,Is.False);Assert.That(g.Snap,Is.False);Assert.That(g.Settings.mouseSensitivity,Is.EqualTo(2.5));
            Assert.That(g.Minutes,Is.EqualTo(5));yield return new WaitForSecondsRealtime(.3f);Assert.That(g.Screens.FpsVisible,Is.True);
            g.Screens.ShowImport(false);Assert.That(g.Screens.Page,Is.EqualTo(RunScreens.Options));Assert.That(g.Screens.TitleText,Does.Contain("Internal resolution").And.Not.Contain("Resolución"));
            g.StartRun("");Assert.That(g.Session.Director.Minutes,Is.EqualTo(5));g.SetPaused(true);g.BackToTitle();
            Assert.That(qa.Stored.settings.language,Is.EqualTo("en"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
