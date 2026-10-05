using System.IO;
using System.Linq;
using Mamporro.U3;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mamporro.Editor
{
    // B0: escena aditiva B0_QA con las referencias a los assets del spike y sus materiales QA.
    // Solo la incluyen las builds B0 (BuildPlayerOptions); no se añade a EditorBuildSettings,
    // así que la entrega (U3Project.Build) no contiene nada de B0.
    public static class B0Project
    {
        public const string Root="Assets/Mamporro/QA/B0";
        public const string ScenePath=Root+"/B0_QA.unity";
        const string Art=B0AssetImport.Root;

        [MenuItem("Mamporro/B0/Crear o regenerar escena QA")]
        public static void CreateQa()
        {
            Directory.CreateDirectory(Root);
            var shader=Shader.Find("Mamporro/RetroWorld");if(!shader)throw new BuildFailedException("Falta Mamporro/RetroWorld");
            var retro=QaMaterial(Root+"/B0_Retro.mat",shader,Color.white);
            // Destello de golpe: el shader multiplica el color de vértice, así que se aclara (aprox. del blanco U6).
            var flash=QaMaterial(Root+"/B0_RetroDestello.mat",shader,new Color(2.5f,2.5f,2.5f,1));
            var setup=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var library=new GameObject("B0 visuales").AddComponent<B0VisualLibrary>();
            library.pelusa=Sub<Mesh>("Pelusa/B0_Pelusa.fbx","B0_Pelusa");
            library.remedios=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Remedios/B0_Remedios.fbx");
            library.wall=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Pared/B0_ParedModulo.fbx");
            library.remediosWalk=Sub<AnimationClip>("Remedios/B0_Remedios.fbx","Remedios_Walk");
            library.remediosIdle=Sub<AnimationClip>("Remedios/B0_Remedios.fbx","Remedios_Idle");
            library.pelusaWalk=Sub<AnimationClip>("Pelusa/B0_Pelusa.fbx","Pelusa_Walk");
            library.retro=retro;library.retroFlash=flash;
            if(!library.pelusa||!library.remedios||!library.wall||!library.remediosWalk||!library.remediosIdle||!library.pelusaWalk)
                throw new BuildFailedException("Faltan assets B0 exportados: scripts\\blender.cmd export");
            EditorSceneManager.SaveScene(setup,ScenePath);
            Debug.Log("B0 escena QA generada: "+ScenePath);
        }
        static T Sub<T>(string fbx,string name) where T:Object
            =>AssetDatabase.LoadAllAssetsAtPath(Art+fbx).OfType<T>().FirstOrDefault(a=>a.name==name);
        static Material QaMaterial(string path,Shader shader,Color color)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
            m.shader=shader;m.enableInstancing=true;m.SetColor("_BaseColor",color);m.SetFloat("_WorldUV",0);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();return m;
        }
        // Escenas de las builds B0: partida U3 + B0_QA aditiva.
        public static string[] Scenes=>new[]{U3Project.ScenePath,ScenePath};
    }
}
