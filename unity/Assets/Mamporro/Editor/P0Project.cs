using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mamporro.Editor
{
    // P0-A: escena U3 reutilizada con opción QA, nunca sobrescribe la entrega U6.
    public static class P0Project
    {
        public static void Build()=>BuildQa(false);
        public static void Development()=>BuildQa(true);
        static void BuildQa(bool development)
        {
            if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)!=ScriptingImplementation.Mono2x)throw new BuildFailedException("P0 requiere Mono");
            string folder=development?"Builds/P0Dev":"Builds/P0";Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{U3Project.ScenePath},locationPathName=folder+"/MAMPORRO-P0.exe",
                target=BuildTarget.StandaloneWindows64,extraScriptingDefines=new[]{"MAMPORRO_P0_QA"},options=development?BuildOptions.Development:BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("P0 QA build correcta: "+folder);
        }
    }
}
