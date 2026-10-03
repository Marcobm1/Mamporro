using System.IO;
using Mamporro.U3;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Mamporro.Editor
{
    // Genera la escena de U3 sin tocar las de U1 ni U2. El mundo se construye en tiempo de
    // ejecución a partir de la semilla; la escena solo lleva cámaras, salida retro y UI.
    public static class U3Project
    {
        const string Root="Assets/Mamporro/U3";
        public const string ScenePath=Root+"/U3_Partida.unity";

        [MenuItem("Mamporro/U3/Crear o regenerar escena")]
        public static void Create()
        {
            foreach(var index in System.Linq.Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount))
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)throw new BuildFailedException("Hay una escena sin guardar. Guardarla antes de generar U3.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("U3 · mundo y partida");
            var game=root.AddComponent<U3Game>();
            game.worldShader=Shader.Find("Mamporro/RetroWorld");game.skyShader=Shader.Find("Mamporro/RetroSky");
            game.combatTemplate=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Combat.mat");
            if(!game.combatTemplate||!game.combatTemplate.enableInstancing)throw new BuildFailedException("Falta Combat.mat con instancing activado.");
            if(!game.worldShader||!game.skyShader)throw new BuildFailedException("Faltan los shaders Mamporro/RetroWorld o Mamporro/RetroSky.");
            var camera=new GameObject("Cámara mundo",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=WebSpace.Linear(Mamporro.Core.Palette.SkyHorizon).gamma;
            camera.fieldOfView=(float)Mamporro.Core.Tuning.CameraFov;camera.nearClipPlane=(float)Mamporro.Core.Tuning.CameraNear;camera.farClipPlane=(float)Mamporro.Core.Tuning.CameraFar;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;game.worldCamera=camera;
            var output=new GameObject("Cámara salida",typeof(Camera)).GetComponent<Camera>();output.cullingMask=0;output.depth=1;output.clearFlags=CameraClearFlags.SolidColor;output.backgroundColor=Color.black;
            var canvas=new GameObject("UI técnica",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var image=new GameObject("Salida puntual",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(canvas.transform,false);
            var rect=image.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            image.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            game.display=image.GetComponent<RawImage>();game.display.raycastTarget=false;
            game.status=Label(canvas.transform,"Estado",new Vector2(20,-20),new Vector2(1150,175),24,TextAnchor.UpperLeft);
            game.help=Label(canvas.transform,"Controles",new Vector2(20,-920),new Vector2(1750,140),22,TextAnchor.UpperLeft);
            Directory.CreateDirectory(Root);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!scenes.Exists(s=>s.path==ScenePath))scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
            Debug.Log("U3 escena generada: "+ScenePath);
        }

        static Text Label(Transform parent,string name,Vector2 position,Vector2 size,int fontSize,TextAnchor alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            go.GetComponent<Image>().color=new Color(.035f,.045f,.06f,.88f);
            var label=new GameObject("Texto",typeof(RectTransform),typeof(Text));label.transform.SetParent(go.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(12,8);lr.offsetMax=new Vector2(-12,-8);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;return text;
        }

        [MenuItem("Mamporro/U3/Build Windows x64 Mono")]
        public static void Build()
        {
            if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)!=ScriptingImplementation.Mono2x)throw new BuildFailedException("U3 requiere Mono.");
            Directory.CreateDirectory("Builds/U3");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/U3/Mamporro-U3.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("U3 Windows x64 Mono: build correcta.");
        }
    }
}
