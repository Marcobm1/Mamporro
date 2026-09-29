using System.IO;
using Mamporro.U1;
using Mamporro.U2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mamporro.Editor
{
    public static class U2Project
    {
        public const string ScenePath="Assets/Mamporro/U2/U2_Combate.unity";
        [MenuItem("Mamporro/U2/Crear escena nueva (una sola vez)")]
        public static void Create()
        {
            if(File.Exists(ScenePath))throw new BuildFailedException("U2 ya existe: no se sobrescribe. Abrir y editar la escena existente.");
            foreach(var scene in System.Linq.Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount))
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(scene).isDirty)throw new BuildFailedException("Hay una escena sin guardar. Guardarla antes de crear U2.");
            if(!AssetDatabase.CopyAsset("Assets/Mamporro/Generated/U1_Patio.unity",ScenePath))throw new BuildFailedException("No se pudo copiar la escena revisada.");
            var target=EditorSceneManager.OpenScene(ScenePath);
            var view=Object.FindFirstObjectByType<PrototypeController>();
            var session=view.gameObject.AddComponent<CombatSession>();view.session=session;
            view.gameObject.name="U2 · núcleo y combate";
            view.status.text="MAMPORRO · U2";
            EditorSceneManager.SaveScene(target);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Mamporro/Generated/U1_Patio.unity",true)};
            Debug.Log("U2 creada desde copia de U1; escena original conservada.");
        }
        [MenuItem("Mamporro/U2/Build Windows x64 Mono")]
        public static void Build()
        {
            if(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone)!=ScriptingImplementation.Mono2x)throw new BuildFailedException("U2 requiere Mono.");
            Directory.CreateDirectory("Builds/U2");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/U2/Mamporro-U2.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("U2 Windows x64 Mono: build correcta.");
        }
    }
}
