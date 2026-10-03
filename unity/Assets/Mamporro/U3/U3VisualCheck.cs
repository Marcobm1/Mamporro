using System.Collections;
using System.IO;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.U3
{
    // Solo con -u3-visual-check: capturas locales del mundo desde puntos fijos, sin guardado.
    public sealed class U3VisualCheck : MonoBehaviour
    {
        public U3Game Game;
        IEnumerator Start()
        {
            string output=Path.Combine(Application.persistentDataPath,"U3Visual");
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-u3-output");
            if(index>=0&&index+1<args.Length)output=args[index+1];
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(1);
            yield return Capture(output,"inicio");
            // Vista desde lo alto y junto a un sitio de cada tipo.
            var world=Game.World;
            yield return Look(output,"vista-alta",new Vector3(0,45,-55),new Vector3(0,5,10));
            foreach(string kind in new[]{"house","temple","farm","well"}){
                var site=world.Sites.Find(s=>s.Kind==kind);
                if(site==null){Debug.LogError("Falta un sitio para la captura: "+kind);Application.Quit(1);yield break;}
                var center=WebSpace.ToUnity(site.X,site.FloorY,site.Z);
                yield return Look(output,"sitio-"+kind,center+new Vector3(9,5,-9),center+Vector3.up*1.5f);
            }
            Game.LoadWorld("MAMPORRO");Game.QaAction(1);Game.QaAction(4);
            for(int i=0;i<8;i++)Game.Run.Spawn(i%4,-5+i*1.4,-7);
            Game.SetPaused(false);yield return new WaitForSecondsRealtime(1.2f);Game.SetPaused(true);
            // Efectos y proyectiles persistentes durante la captura, sobre suelo real.
            double y=Game.World.Heightfield.HeightAt(2,-3)+1;
            Game.Run.Projectiles.Spawn(2,y,-3,0,-1,0,10,.5);
            Game.Run.EnemyShots.Spawn(-2,y,-3,0,1,0,10,.5,damage:10);
            Game.Emit(new CombatEffect{Kind="aura",X=0,Y=Game.Body.Y,Z=0,Radius=3,Life=10});
            var origin=WebSpace.ToUnity(Game.Body.X,Game.Body.Y,Game.Body.Z);
            yield return Look(output,"combate",origin+new Vector3(9,8,-12),origin+new Vector3(0,1,5));
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
            Game.QaAction(2);yield return Capture(output,"cartas");
            Game.LoadWorld("MAMPORRO");yield return Capture(output,"reinicio");
            Debug.Log("U3 visual check: mundo, combate, cartas y reinicio guardados.");Application.Quit(0);
        }
        IEnumerator Look(string output,string name,Vector3 position,Vector3 target)
        {
            Game.FreeCamera=true;Game.worldCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            yield return Capture(output,name);
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
