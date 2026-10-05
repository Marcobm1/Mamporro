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
            // B0.5: poses discretas (C) y VAT (D) horneadas del mismo clip y la misma malla.
            BakePelusa(library);
            var vatShader=Shader.Find("Mamporro/RetroWorldVAT");if(!vatShader)throw new BuildFailedException("Falta Mamporro/RetroWorldVAT");
            library.vat=VatMaterial(Root+"/B0_PelusaVAT.mat",vatShader,Color.white,library);
            library.vatFlash=VatMaterial(Root+"/B0_PelusaVATDestello.mat",vatShader,new Color(2.5f,2.5f,2.5f,1),library);
            EditorSceneManager.SaveScene(setup,ScenePath);
            Debug.Log("B0 escena QA generada: "+ScenePath);
        }

        public const int Poses=8;
        // Muestrea Pelusa_Walk sobre una instancia del modelo y hornea con BakeMesh (mismo orden
        // de vértices que la malla importada): 8 mallas de pose y una textura RGBAHalf
        // (vértice × fotograma) con la posición local de cada vértice en cada fotograma.
        static void BakePelusa(B0VisualLibrary library)
        {
            Directory.CreateDirectory(Root+"/Generated");
            var clip=library.pelusaWalk;int frames=Mathf.RoundToInt(clip.length*clip.frameRate);
            var instance=(GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Pelusa/B0_Pelusa.fbx"));
            instance.hideFlags=HideFlags.HideAndDontSave;
            try{
                var smr=instance.GetComponentInChildren<SkinnedMeshRenderer>();int count=smr.sharedMesh.vertexCount;
                Mesh Sample(float t){clip.SampleAnimation(instance,t);var m=new Mesh();smr.BakeMesh(m,true);return m;}
                var vat=new Texture2D(count,frames,TextureFormat.RGBAHalf,false,true){name="B0_Pelusa_VAT",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[count*frames];
                for(int f=0;f<frames;f++){var m=Sample(f/clip.frameRate);var v=m.vertices;for(int i=0;i<count;i++)pixels[f*count+i]=new Color(v[i].x,v[i].y,v[i].z,1);Object.DestroyImmediate(m);}
                vat.SetPixels(pixels);vat.Apply(false,false);
                library.pelusaVat=Save(vat,Root+"/Generated/B0_Pelusa_VAT.asset");
                library.pelusaPoses=new Mesh[Poses];
                for(int k=0;k<Poses;k++){var m=Sample(clip.length*k/Poses);m.name="B0_Pelusa_Pose_"+k;m.RecalculateBounds();library.pelusaPoses[k]=Save(m,Root+$"/Generated/B0_Pelusa_Pose_{k}.asset");}
                library.clipSeconds=clip.length;
            }finally{Object.DestroyImmediate(instance);}
            AssetDatabase.SaveAssets();
        }
        // Regenerar conserva el GUID del asset (y las referencias de la escena).
        static T Save<T>(T value,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing){EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
        static Material VatMaterial(string path,Shader shader,Color color,B0VisualLibrary library)
        {
            var m=QaMaterial(path,shader,color);
            m.SetTexture("_VatTex",library.pelusaVat);m.SetFloat("_VatFrames",library.pelusaVat.height);m.SetFloat("_VatFps",library.pelusaWalk.frameRate);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();return m;
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
