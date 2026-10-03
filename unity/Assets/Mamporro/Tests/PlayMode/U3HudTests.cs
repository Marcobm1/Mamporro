using System.Collections;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // Paso 8: HUD, minimapa con descubrimiento, avisos, objeto conseguido, jefe y su
    // telegrafiado, aviso de interacción y pausa con semilla y estadísticas.
    public sealed class U3HudTests
    {
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}

        [UnityTest]public IEnumerator HudMinimapNoticesBossAndPause()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");yield return null;
            var g=Object.FindAnyObjectByType<U3Game>();var s=g.Session;var r=g.Run;
            yield return Frames(2);
            Assert.That(g.Hud.Visible,Is.True);Assert.That(g.Hud.TimerText,Is.EqualTo("10:00"));
            Assert.That(g.Screens.PauseVisible,Is.True,"empieza en pausa");
            Assert.That(g.Screens.PauseText,Does.Contain(g.Seed).And.Contain("Vida"));
            // Minimapa: terreno pintado y sin marcas hasta descubrir; la depuración 8 lo revela todo.
            var pixels=g.Hud.Map.Texture.GetPixels32();var first=pixels[0];bool varied=false;foreach(var p in pixels)if(!p.Equals(first)){varied=true;break;}
            Assert.That(varied,Is.True,"terreno del minimapa");
            g.Hud.Map.Update(s,0,Time.unscaledTime,true);Assert.That(g.Hud.Map.Markers,Is.Zero);
            g.QaAction(8);g.Hud.Map.Update(s,0,Time.unscaledTime,true);
            Assert.That(g.Hud.Map.Markers,Is.EqualTo(s.Interactables.List.Length));Assert.That(s.Cheated,Is.True);
            // Avisos: +2 minutos → estampida y élite en el siguiente tick.
            g.QaAction(1);g.QaAction(3);g.QaAction(3);g.SetPaused(false);
            yield return new WaitForSeconds(.2f);
            Assert.That(g.Screens.PauseVisible,Is.False);
            string notices=g.Hud.NoticeText(0)+"|"+g.Hud.NoticeText(1)+"|"+g.Hud.NoticeText(2);
            Assert.That(notices,Does.Contain("Estampida").And.Contain("Rata de Gimnasio"));
            Assert.That(g.Hud.TimerText,Is.EqualTo(RunHud.FormatCountdown(s.TimeLeft)));
            // Objeto conseguido y destello por golpe.
            r.AddItem("gafas");yield return Frames(2);
            Assert.That(g.Hud.BannerVisible,Is.True);Assert.That(g.Hud.BannerText,Does.Contain("Gafas"));
            // Jefe: barra visible y telegrafiado del culetazo sobre el terreno.
            g.QaAction(6);g.SetPaused(true);yield return Frames(2);
            Assert.That(g.Hud.BossVisible,Is.True);g.Hud.Map.Update(s,0,Time.unscaledTime,true);Assert.That(g.Hud.Map.BossShown,Is.True,"jefe en el minimapa");
            int before=g.CombatView.DrawnInstances;
            r.Boss.Phase="windup";r.Boss.Attack="slam";r.Boss.Timer=r.Boss.PhaseLength=1.3;
            yield return Frames(2);
            Assert.That(g.CombatView.DrawnInstances,Is.GreaterThan(before+100),"círculo de aviso del culetazo");
            // Aviso de interacción junto a un baúl.
            var chest=System.Array.Find(s.Interactables.List,i=>i.Spot.Kind=="chest"&&!i.Used);
            g.Body.PlaceAt(chest.Spot.X+1,g.World.Collision.GroundHeight(chest.Spot.X+1,chest.Spot.Z,double.PositiveInfinity),chest.Spot.Z);
            r.Enemies.Clear();g.SetPaused(false);yield return new WaitForSeconds(.1f);g.SetPaused(true);yield return Frames(2);
            Assert.That(g.Hud.PromptText,Does.Contain("[E]"));
            Assert.That(g.Screens.PauseVisible,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
