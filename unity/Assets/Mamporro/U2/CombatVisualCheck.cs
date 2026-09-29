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
            Session.Run.GainXp(12);Session.Run.OpenChoice();Session.AfterChoice();yield return new WaitForSecondsRealtime(.5f);yield return Capture(output,"cards-es");
            CombatText.English=true;Session.RefreshHud();yield return Capture(output,"cards-en");
            Session.QaOpen=true;yield return Capture(output,"qa-en");Session.QaOpen=false;
            Session.Restart(1);Session.Run.Hurt(1000);yield return Capture(output,"defeat-en");
            Debug.Log("U2 visual check: pausa, cartas ES/EN, QA y derrota capturadas.");Application.Quit(0);
        }
        static IEnumerator Capture(string output,string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
    }
}
