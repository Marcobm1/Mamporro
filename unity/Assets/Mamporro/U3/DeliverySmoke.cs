using System;
using System.Collections;
using System.IO;
using Mamporro.Persistence;
using UnityEngine;

namespace Mamporro.U3
{
    // U6: solo con -u6-smoke. Comprobación rápida de una build entregada: identidad, rutas
    // resueltas (cálculo puro: no abre ni explora el guardado personal), escena, audio y una
    // partida breve hasta jugar unos segundos. Pensada para lanzarse siempre con -u4-save-dir
    // hacia una carpeta temporal. Escribe smoke-report.json en -u3-output y sale.
    public sealed class DeliverySmoke : MonoBehaviour
    {
        public U3Game Game;
        [Serializable] class Report
        {
            public string productName,companyName,version,unity,platform,exe,dataPath,persistentDataPath,saveDirectory,defaultSaveDirectory,defaultLegacyDirectory,scene,language,progressStatus;
            public bool development,overrideActive,legacyOfferedWithOverride,played;public int sampleRate,sources;public double runTime;
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-u3-output");
            string output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.temporaryCachePath,"Smoke");
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(1);
            bool windows=Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor;
            // Rutas por defecto calculadas sin argumentos: no se lee nada de esas carpetas.
            var defaults=ProgressLocations.Resolve(Application.persistentDataPath,windows,Array.Empty<string>());
            var report=new Report{productName=Application.productName,companyName=Application.companyName,version=Application.version,unity=Application.unityVersion,
                platform=Application.platform.ToString(),exe=args.Length>0?args[0]:"",dataPath=Application.dataPath,persistentDataPath=Application.persistentDataPath,
                saveDirectory=Game.SaveDirectory,defaultSaveDirectory=defaults.Current,defaultLegacyDirectory=defaults.Legacy,
                scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,language=Game.Language,progressStatus=Game.Progress.Status,
                development=Debug.isDebugBuild,overrideActive=Game.SaveDirectory!=defaults.Current,legacyOfferedWithOverride=Game.LegacySaveDirectory!=null,
                sampleRate=AudioSettings.outputSampleRate,sources=Game.GetComponentsInChildren<AudioSource>().Length};
            Game.StartRun("");yield return new WaitForSecondsRealtime(2);
            report.runTime=Game.Run.Time;report.played=report.runTime>.5;Game.SetPaused(true);
            File.WriteAllText(Path.Combine(output,"smoke-report.json"),JsonUtility.ToJson(report,true));
            Debug.Log($"U6 humo: {report.productName} {report.version}, progreso {report.saveDirectory}, por defecto {report.defaultSaveDirectory}, anterior {report.defaultLegacyDirectory}, partida {report.runTime:F1} s");
            bool ok=report.played&&!report.development&&report.overrideActive&&!report.legacyOfferedWithOverride&&report.sources==18;
            if(!ok)Debug.LogError("U6: la comprobación de humo no es correcta.");
            Application.Quit(ok?0:1);
        }
    }
}
