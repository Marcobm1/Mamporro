using System.Collections;
using System.IO;
using UnityEngine;

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
            Debug.Log("U3 visual check: capturas del mundo guardadas.");Application.Quit(0);
        }
        IEnumerator Look(string output,string name,Vector3 position,Vector3 target)
        {
            Game.FreeCamera=true;Game.worldCamera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            yield return Capture(output,name);
        }
        static IEnumerator Capture(string output,string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
    }
}
