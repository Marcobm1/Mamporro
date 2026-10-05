using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using Mamporro.Core;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mamporro.Tests
{
    // B0.4: la integración visual (VisualRoot de Remedios, horda instanciada y muro) solo lee la
    // lógica: misma partida con el visual activado o desactivado, sin colliders ni root motion.
    public sealed class B0VisualSceneTests
    {
        QaSave qa;[SetUp]public void UseQaSave(){qa=QaSave.UseAllUnlocked();}[TearDown]public void ReleaseQaSave(){qa.Dispose();}

        static IEnumerator LoadScenes()
        {
            yield return SceneManager.LoadSceneAsync("U3_Partida");
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Mamporro/QA/B0/B0_QA.unity",new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            yield return null;
        }
        static U3Game Game=>Object.FindAnyObjectByType<U3Game>();
        static B0VisualLibrary Library=>Object.FindAnyObjectByType<B0VisualLibrary>();

        // Huella de la partida: tiempo, jugador, horda (id, posición, vida), progreso y director.
        static string Digest(U3Game g)
        {
            var r=g.Run;var e=r.Enemies;var sb=new StringBuilder();string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
            sb.Append(F(r.Time)).Append('|').Append(F(g.Body.X)).Append(',').Append(F(g.Body.Y)).Append(',').Append(F(g.Body.Z)).Append('|').Append(F(r.Hp))
              .Append('|').Append(r.Kills).Append('|').Append(F(r.Gold)).Append('|').Append(F(r.Xp)).Append('|').Append(r.Level).Append('|').Append(g.Session.Spawned)
              .Append('|').Append(r.Projectiles.Count).Append('|').Append(r.Gems.Count).Append('|').Append(e.Count).Append('#');
            for(int i=0;i<e.Count;i++)sb.Append(e.Id[i]).Append(':').Append(e.Type[i]).Append(':').Append(F(e.X[i])).Append(':').Append(F(e.Z[i])).Append(':').Append(F(e.Hp[i])).Append(';');
            return sb.ToString();
        }
        // 900 ticks (15 s) con el mismo recorrido; se dibuja un fotograma cada 3 ticks.
        static IEnumerator Run(U3Game g,string[] digest)
        {
            g.StartRun("MAMPORRO");g.SetPaused(true);g.Run.Invincible=true;
            var s=g.Session;
            for(int k=0;k<900;k++){
                int side=k/150%4;var intent=new PlayerIntent{MoveX=side==0?1:side==2?-1:0,MoveZ=side==1?-1:side==3?1:0};
                s.Step(intent,1.0/60,0);g.CombatView.Step(1.0/60);
                while(g.Run.Choosing)g.Run.Choose(0);
                if(k%3==0)yield return null;
            }
            digest[0]=Digest(g);
        }

        [UnityTest]
        public IEnumerator LogicIsIdenticalWithVisualOnAndOff()
        {
            yield return LoadScenes();var g=Game;
            string[] a={""},b={""},c={""};
            yield return Run(g,a);yield return Run(g,b);
            Assert.That(b[0],Is.EqualTo(a[0]),"la partida de referencia no es reproducible");
            var adapter=B0VisualAdapter.Attach(g,Library);
            yield return Run(g,c);
            Assert.That(adapter.Horde.Instances,Is.GreaterThan(0),"el visual B0 dibujó Pelusas durante la partida");
            Assert.That(c[0],Is.EqualTo(a[0]),"el visual B0 cambió la lógica");
            Assert.That(a[0].Split('#')[0].Split('|')[10],Is.Not.EqualTo("0"),"hubo horda en la comparación");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator VisualRootFollowsLogicPausesAndResetsCleanly()
        {
            yield return LoadScenes();var g=Game;var adapter=B0VisualAdapter.Attach(g,Library);
            g.StartRun("MAMPORRO");g.Run.Invincible=true;g.ScriptedIntent=new PlayerIntent{MoveX=1};g.SetPaused(false);
            yield return new WaitForSeconds(.8f);
            var avatar=g.AvatarRoot;
            Assert.That((adapter.VisualRoot.position-avatar.position).magnitude,Is.LessThan(1e-5f),"VisualRoot en la posición interpolada del jugador");
            Assert.That(Quaternion.Angle(adapter.VisualRoot.rotation,avatar.rotation),Is.LessThan(.01f));
            Assert.That(adapter.WalkWeight,Is.GreaterThan(.5f),"anda al moverse");Assert.That(adapter.AnimationTime,Is.GreaterThan(0));
            Assert.That(adapter.Animator.applyRootMotion,Is.False);
            Assert.That(adapter.VisualRoot.GetComponentsInChildren<UnityEngine.Collider>(true),Is.Empty,"sin colliders en el visual");
            Assert.That(adapter.Walls.GetComponentsInChildren<UnityEngine.Collider>(true),Is.Empty,"muro solo visual");
            Assert.That(avatar.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),Is.True,"el avatar provisional no se dibuja");
            // Horda: las Pelusas las dibuja el visual; el resto sigue en RunRenderer.
            g.QaAction(4);yield return null;yield return null;
            int pelusas=0;var e=g.Run.Enemies;for(int i=0;i<e.Count;i++)if(e.Type[i]==0)pelusas++;
            Assert.That(pelusas,Is.GreaterThan(0));Assert.That(adapter.Horde.Instances,Is.EqualTo(pelusas));
            // Pausa: ni lógica ni animación avanzan.
            g.SetPaused(true);yield return null;double t=adapter.AnimationTime,time=g.Run.Time;
            for(int k=0;k<10;k++)yield return null;
            Assert.That(adapter.AnimationTime,Is.EqualTo(t));Assert.That(g.Run.Time,Is.EqualTo(time));
            // Reinicio: partida nueva sin poses ni instancias residuales.
            g.BackToTitle();yield return null;
            Assert.That(adapter.AnimationTime,Is.EqualTo(0));Assert.That(adapter.WalkWeight,Is.EqualTo(0));
            Assert.That(adapter.Horde.Instances,Is.EqualTo(0));
            Assert.That((adapter.VisualRoot.position-avatar.position).magnitude,Is.LessThan(1e-5f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DisablingVisualRestoresTheApprovedRepresentation()
        {
            yield return LoadScenes();var g=Game;var adapter=B0VisualAdapter.Attach(g,Library);
            g.StartRun("MAMPORRO");g.SetPaused(true);g.QaAction(4);yield return null;yield return null;
            int withVisual=g.CombatView.DrawnInstances;
            adapter.enabled=false;yield return null;yield return null;
            Assert.That(g.CombatView.EnemyVisual,Is.Null);
            Assert.That(g.AvatarRoot.GetComponentsInChildren<Renderer>(true).All(r=>r.enabled),Is.True,"vuelve el avatar U6");
            Assert.That(adapter.VisualRoot.gameObject.activeSelf,Is.False);Assert.That(adapter.Walls.gameObject.activeSelf,Is.False);
            Assert.That(g.CombatView.DrawnInstances,Is.GreaterThan(withVisual),"las Pelusas vuelven a dibujarse como cajas");
            adapter.enabled=true;yield return null;
            Assert.That(g.CombatView.EnemyVisual,Is.SameAs(adapter.Horde));Assert.That(adapter.VisualRoot.gameObject.activeSelf,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
