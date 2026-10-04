using System.Collections;
using System.IO;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.U3
{
    // Solo con -u3-visual-check: capturas locales de las pantallas y del mundo desde puntos
    // fijos, sin guardado. No es un ensayo de rendimiento.
    public sealed class U3VisualCheck : MonoBehaviour
    {
        public U3Game Game;
        public static readonly string[] Names={"inicio","importar","vista-alta","sitio-house","sitio-temple","sitio-farm","sitio-well","combate","interactuables","telegrafiado","pausa","cartas","resultados","reinicio"};
        IEnumerator Start()
        {
            string output=Path.Combine(Application.persistentDataPath,"U3Visual");
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-u3-output");
            if(index>=0&&index+1<args.Length)output=args[index+1];
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(1);
            yield return Capture(output,"inicio");
            // Importación U4 revisada (sin confirmar) sobre el guardado de la sesión; el lanzador
            // pasa -u4-save-dir para que la comprobación nunca use el guardado del autor.
            var sample=Mamporro.Core.Progress.ProgressDto.New("es");sample.meta.coins=1234;sample.meta.characters=new[]{"remedios","baguette"};
            string samplePath=Path.Combine(output,"mamporro-progreso.json");
            File.WriteAllText(samplePath,Mamporro.Core.Progress.ProgressJson.Stringify(Mamporro.Core.Progress.ProgressTree.Object("format","mamporro.progress","version",1,
                "source",Mamporro.Core.Progress.ProgressTree.Object("platform","web","saveVersion",3),"progress",Mamporro.Core.Progress.ProgressTree.From(sample))));
            Game.Screens.ShowImport(true);Game.Screens.ImportPath=samplePath;Game.Screens.ReviewImport();
            Debug.Log($"U3 visual importar antes: estado={Game.State} panel={Game.Screens.ImportVisible} inicio={Game.Screens.TitleVisible} puede={Game.Screens.ImportCanConfirm} msg={Game.Screens.ImportMessage}");
            yield return Capture(output,"importar");
            Debug.Log($"U3 visual importar después: estado={Game.State} panel={Game.Screens.ImportVisible} inicio={Game.Screens.TitleVisible}");
            Game.Screens.ShowImport(false);
            // Mundo sin interfaz: vista desde lo alto y junto a un sitio de cada tipo.
            var world=Game.World;Game.HideInterface=true;
            yield return Look(output,"vista-alta",new Vector3(0,45,-55),new Vector3(0,5,10));
            foreach(string kind in new[]{"house","temple","farm","well"}){
                var site=world.Sites.Find(s=>s.Kind==kind);
                if(site==null){Debug.LogError("Falta un sitio para la captura: "+kind);Application.Quit(1);yield break;}
                var center=WebSpace.ToUnity(site.X,site.FloorY,site.Z);
                yield return Look(output,"sitio-"+kind,center+new Vector3(9,5,-9),center+Vector3.up*1.5f);
            }
            Game.HideInterface=false;
            // Combate con HUD (Remedios, 10 min), con enemigos cerca y efectos persistentes.
            Game.StartRun("MAMPORRO");Game.QaAction(1);Game.QaAction(4);
            for(int i=0;i<8;i++)Game.Run.Spawn(i%4,-5+i*1.4,-7);
            yield return new WaitForSecondsRealtime(1.2f);Game.SetPaused(true);
            double y=Game.World.Heightfield.HeightAt(2,-3)+1;
            Game.Run.Projectiles.Spawn(2,y,-3,0,-1,0,10,.5);
            Game.Run.EnemyShots.Spawn(-2,y,-3,0,1,0,10,.5,damage:10);
            Game.Emit(new CombatEffect{Kind="aura",X=0,Y=Game.Body.Y,Z=0,Radius=3,Life=10});
            Game.HideInterface=true;
            var origin=WebSpace.ToUnity(Game.Body.X,Game.Body.Y,Game.Body.Z);
            yield return Look(output,"combate",origin+new Vector3(9,8,-12),origin+new Vector3(0,1,5),false);
            // Comparación de píxeles de la cámara, sin HUD, movimiento ni viento.
            // Detecta variantes de instancing eliminadas en build: contar entidades
            // o llamadas de dibujo no demuestra que lleguen a la imagen.
            Game.Renderer.enabled=false;
            yield return null;yield return new WaitForEndOfFrame();
            var withCombat=ReadWorld();Game.CombatView.enabled=false;
            yield return null;yield return new WaitForEndOfFrame();
            var withoutCombat=ReadWorld();int changed=0;
            for(int i=0;i<withCombat.Length;i++)if(!withCombat[i].Equals(withoutCombat[i]))changed++;
            Game.CombatView.enabled=true;Game.Renderer.enabled=true;
            if(changed<50){Debug.LogError("U3 combate no visible en build: "+changed+" píxeles distintos.");Application.Quit(1);yield break;}
            Debug.Log("U3 instancing visible: "+changed+" píxeles distintos con/sin combate.");
            // Con la partida en marcha para que se vea el HUD (en pausa lo taparía la pausa).
            Game.HideInterface=false;Game.SetPaused(false);yield return Capture(output,"combate");Game.SetPaused(true);Game.HideInterface=true;
            // Interactuables (solo mundo): un baúl abierto y una mesa camilla a medio cargar.
            Game.Run.Enemies.Clear();Game.Run.GainGold(200);
            var list=Game.Session.Interactables.List;var chest=System.Array.Find(list,i=>i.Spot.Kind=="chest");var shrine=System.Array.Find(list,i=>i.Spot.Kind=="shrine");
            Game.Body.PlaceAt(chest.Spot.X+1.5,Game.World.Heightfield.HeightAt(chest.Spot.X+1.5,chest.Spot.Z),chest.Spot.Z);Game.Session.SyncPlayer();
            Game.Session.Interact();shrine.Charge=.55;Game.QaAction(8);
            var chestPos=WebSpace.ToUnity(chest.Spot.X,chest.Spot.Y,chest.Spot.Z);
            yield return Look(output,"interactuables",chestPos+new Vector3(4,3.5f,-4),chestPos+Vector3.up*.6f);
            // Telegrafiado: culetazo del jefe a medio preparar.
            Game.FreeCamera=false;Game.QaAction(6);var r=Game.Run;int b=r.Enemies.IndexOf(r.Boss.EnemyId);
            r.Boss.Phase="windup";r.Boss.Attack="slam";r.Boss.PhaseLength=1.3;r.Boss.Timer=.6;
            var bossPos=WebSpace.ToUnity(r.Enemies.X[b],r.Enemies.Y[b],r.Enemies.Z[b]);
            yield return Look(output,"telegrafiado",bossPos+new Vector3(10,11,-10),bossPos);
            Game.FreeCamera=false;Game.HideInterface=false;yield return Capture(output,"pausa");
            Game.QaAction(2);yield return Capture(output,"cartas");
            Game.Choose(0);while(Game.Run.Choosing)Game.Choose(0);
            Game.QaAction(5);Game.SetPaused(false);yield return new WaitForSecondsRealtime(2.2f);
            if(Game.State!=U3Game.Screen.Results){Debug.LogError("U3 no llegó a los resultados tras la victoria.");Application.Quit(1);yield break;}
            yield return Capture(output,"resultados");
            Game.Retry(false);yield return Capture(output,"reinicio");Game.SetPaused(true);
            Debug.Log("U3 visual check: pantallas, mundo, combate, interactuables, telegrafiado, cartas, resultados y reinicio guardados.");Application.Quit(0);
        }
        IEnumerator Look(string output,string name,Vector3 position,Vector3 target,bool capture=true)
        {
            Game.FreeCamera=true;Game.worldCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            if(capture)yield return Capture(output,name);
        }
        Color32[] ReadWorld()
        {
            var target=Game.worldCamera.targetTexture;var previous=RenderTexture.active;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try {RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();return texture.GetPixels32();}
            finally {RenderTexture.active=previous;Destroy(texture);}
        }
        static IEnumerator Capture(string output,string name)
        {yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
    }
}
