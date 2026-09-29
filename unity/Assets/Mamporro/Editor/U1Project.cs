using System.IO;
using Mamporro.U1;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Mamporro.Editor
{
    public static class U1Project
    {
        const string Root="Assets/Mamporro/Generated";
        public const string ScenePath=Root+"/U1_Patio.unity";
        [MenuItem("Mamporro/U1/Preparar escena técnica")]
        public static void Create()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            PlayerSettings.companyName="Mamporro"; PlayerSettings.productName="Mamporro U1";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth=1920; PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.enableFrameTimingStats=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            player.FindProperty("activeInputHandler").intValue=1; player.ApplyModifiedPropertiesWithoutUndo();
            EditorSettings.serializationMode=SerializationMode.ForceText;
            var renderer=Asset<UniversalRendererData>("Renderer.asset");
            var pipeline=Asset<UniversalRenderPipelineAsset>("RetroPipeline.asset");
            var pipelineSerialized=new SerializedObject(pipeline);
            var list=pipelineSerialized.FindProperty("m_RendererDataList"); list.arraySize=1; list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.msaaSampleCount=1; pipeline.renderScale=1; pipeline.supportsHDR=false;
            pipeline.supportsCameraDepthTexture=false; pipeline.supportsCameraOpaqueTexture=false;
            GraphicsSettings.defaultRenderPipeline=pipeline;
            for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline=pipeline; QualitySettings.vSyncCount=0; }
            QualitySettings.SetQualityLevel(0);
            var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            quality.FindProperty("m_QualitySettings").GetArrayElementAtIndex(0).FindPropertyRelative("name").stringValue="U1 Retro";
            quality.ApplyModifiedPropertiesWithoutUndo();
            var settings=Asset<PrototypeSettings>("PrototypeSettings.asset");
            var ground=Material("Ground",new Color(.34f,.43f,.34f));
            var stone=Material("Stone",new Color(.55f,.45f,.33f));
            var purple=Material("Enemy",new Color(.65f,.26f,.52f)); purple.enableInstancing=true;
            var coat=Material("Coat",new Color(.14f,.65f,.65f));
            var gold=Material("Gold",new Color(.98f,.7f,.22f));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("U1 · Patio técnico");
            var control=root.AddComponent<PrototypeController>(); control.settings=settings;
            var mesh=GroundMesh();
            string meshPath=Root+"/Ground.asset";
            var existingMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existingMesh) { EditorUtility.CopySerialized(mesh,existingMesh); Object.DestroyImmediate(mesh); mesh=existingMesh; }
            else AssetDatabase.CreateAsset(mesh,meshPath);
            var surface=new GameObject("Suelo · rampas y mesetas",typeof(MeshFilter),typeof(MeshRenderer));
            surface.GetComponent<MeshFilter>().sharedMesh=mesh; surface.GetComponent<MeshRenderer>().sharedMaterial=ground;
            foreach(var b in TechnicalWorld.Blocks) Cube("Obstáculo",new Vector3(b.x,1.5f,b.y),new Vector3(b.z*2,3,b.w*2),stone);
            // Pilares y marcas originales ayudan a leer velocidad, escala y horizonte.
            for(int i=-40;i<=40;i+=8) { Cube("Marca",new Vector3(i,.015f,-24),new Vector3(.12f,.03f,3),gold); }
            Cube("Límite norte",new Vector3(0,1,48),new Vector3(96,2,1),stone);
            Cube("Límite sur",new Vector3(0,1,-48),new Vector3(96,2,1),stone);
            Cube("Límite este",new Vector3(48,1,0),new Vector3(1,2,96),stone);
            Cube("Límite oeste",new Vector3(-48,1,0),new Vector3(1,2,96),stone);
            var avatar=new GameObject("Provisional original · Conserje de pruebas").transform; control.avatar=avatar;
            Part(avatar,"Bata",new Vector3(0,.65f,0),new Vector3(.65f,1,.4f),coat);
            Part(avatar,"Cabeza",new Vector3(0,1.35f,0),new Vector3(.45f,.4f,.4f),gold);
            Part(avatar,"Visera",new Vector3(0,1.48f,.25f),new Vector3(.65f,.12f,.38f),coat);
            Part(avatar,"Mochila",new Vector3(0,.85f,-.3f),new Vector3(.48f,.5f,.25f),stone);
            Part(avatar,"Pie izquierdo",new Vector3(-.2f,.1f,.08f),new Vector3(.22f,.2f,.5f),stone);
            Part(avatar,"Pie derecho",new Vector3(.2f,.1f,.08f),new Vector3(.22f,.2f,.5f),stone);
            avatar.position=new Vector3(0,0,-16);
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube); control.enemyMesh=primitive.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(primitive);
            control.enemyMaterial=purple;
            var camera=new GameObject("Cámara mundo",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.19f,.24f,.28f); camera.fieldOfView=70; camera.nearClipPlane=.1f; camera.farClipPlane=120;
            camera.transform.SetPositionAndRotation(new Vector3(0,4,-22),Quaternion.Euler(20,0,0));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false; control.worldCamera=camera;
            var output=new GameObject("Cámara salida",typeof(Camera)).GetComponent<Camera>(); output.cullingMask=0; output.depth=1; output.clearFlags=CameraClearFlags.SolidColor; output.backgroundColor=Color.black;
            var canvas=new GameObject("UI técnica",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var image=new GameObject("Salida puntual",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)); image.transform.SetParent(canvas.transform,false);
            var rect=image.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            image.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            control.display=image.GetComponent<RawImage>(); control.display.raycastTarget=false;
            control.status=Label(canvas.transform,"Estado",new Vector2(20,-20),new Vector2(1150,175),24,TextAnchor.UpperLeft);
            control.help=Label(canvas.transform,"Controles",new Vector2(20,-920),new Vector2(1750,140),22,TextAnchor.UpperLeft);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            EditorUtility.SetDirty(pipeline); EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssets();
            Debug.Log("U1 scene generated and saved");
        }
        static T Asset<T>(string name) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(Root+"/"+name);
            if(!asset) { asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,Root+"/"+name); }
            return asset;
        }
        static Material Material(string name,Color color)
        {
            var path=Root+"/"+name+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Mamporro/Retro")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color); EditorUtility.SetDirty(material); return material;
        }
        static GameObject Cube(string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.position=position; go.transform.localScale=scale;
            go.GetComponent<MeshRenderer>().sharedMaterial=material; Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material) { var part=Cube(name,position,scale,material); part.transform.SetParent(parent,false); }
        static Text Label(Transform parent,string name,Vector2 position,Vector2 size,int fontSize,TextAnchor alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=position; rect.sizeDelta=size;
            go.GetComponent<Image>().color=new Color(.035f,.045f,.06f,.88f);
            var label=new GameObject("Texto",typeof(RectTransform),typeof(Text)); label.transform.SetParent(go.transform,false);
            var lr=label.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=new Vector2(12,8); lr.offsetMax=new Vector2(-12,-8);
            var text=label.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=fontSize; text.alignment=alignment; text.color=Color.white; text.raycastTarget=false; return text;
        }
        static Mesh GroundMesh()
        {
            const int cells=96; var vertices=new Vector3[(cells+1)*(cells+1)]; var triangles=new int[cells*cells*6];
            for(int z=0;z<=cells;z++) for(int x=0;x<=cells;x++) vertices[x+z*(cells+1)]=new Vector3(x-48,TechnicalWorld.Height(x-48,z-48),z-48);
            int k=0;
            for(int z=0;z<cells;z++) for(int x=0;x<cells;x++) { int a=x+z*(cells+1),b=a+cells+1; triangles[k++]=a;triangles[k++]=b;triangles[k++]=a+1;triangles[k++]=a+1;triangles[k++]=b;triangles[k++]=b+1; }
            var mesh=new Mesh { name="Patio original",vertices=vertices,triangles=triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        [MenuItem("Mamporro/U1/Build Windows Mono")]
        public static void Build()
        {
            Directory.CreateDirectory("Builds/U1");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/U1/Mamporro-U1.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("U1 Windows x64 Mono build succeeded");
        }
    }
}
