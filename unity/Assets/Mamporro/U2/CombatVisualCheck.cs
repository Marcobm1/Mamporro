using System.Collections;
using System.IO;
using UnityEngine;

namespace Mamporro.U2
{
    // Solo con argumento explícito: captura local de estados UI, sin guardados.
    public sealed class CombatVisualCheck : MonoBehaviour
    {
        public CombatSession Session;
        IEnumerator Start()
        {
            string output=Path.Combine(Application.dataPath,"../TestResults/U2/Visual");Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(1);yield return Capture(output,"pause-es");
            Session.Run.GainXp(12);Session.OpenChoice();yield return new WaitForSecondsRealtime(.5f);yield return Capture(output,"cards-es");
            Session.SetEnglish(true);yield return Capture(output,"cards-en");
            // El panel QA no se abre durante la subida de nivel: primero se elige una carta.
            Session.Choose(0);while(Session.Run.Choosing){yield return new WaitForSecondsRealtime(.5f);Session.Choose(0);}
            Session.QaOpen=true;Session.View.SetPaused(true);yield return Capture(output,"qa-en");Session.CloseQa();
            Session.Restart(1);Session.Run.Hurt(1000);yield return Capture(output,"defeat-en");
            Debug.Log("U2 visual check: pausa, cartas ES/EN, QA y derrota capturadas.");Application.Quit(0);
        }
        static IEnumerator Capture(string output,string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
    }
}
