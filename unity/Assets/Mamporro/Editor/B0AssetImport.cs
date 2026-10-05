using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mamporro.Editor
{
    // B0 (spike Blender): ajustes de importación del contrato para los FBX exportados desde
    // Blender bajo Art/B0. Los datos de cada asset salen de su JSON gemelo (<id>.b0.json),
    // generado por scripts/blender/b0_export.py a partir del manifiesto. Sin colliders,
    // cámaras, luces ni materiales generados: los materiales los pone Unity por índice de slot.
    public sealed class B0AssetImport : AssetPostprocessor
    {
        public const string Root="Assets/Mamporro/Art/B0/";
        // Subir al cambiar los ajustes: Unity reimporta los modelos afectados.
        public override uint GetVersion()=>1;

        [Serializable] public sealed class Probe {public string name;public float[] position,srgb;}
        [Serializable] public sealed class Clip {public string name,take;public int start,end;public bool loop;}
        [Serializable] public sealed class Sidecar
        {
            public int contract,fps,triangles,vertices;public string id,kind,purpose,digest,fbxSha256;
            public string[] materials,objects;public Clip[] clips;public Probe[] probes;
            public float[] boundsMin,boundsMax;public float boundsTolerance;
        }
        public static bool Applies(string path)=>path.StartsWith(Root,StringComparison.Ordinal)&&path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase);
        public static string SidecarPath(string fbx)=>fbx.Substring(0,fbx.Length-4)+".b0.json";
        public static Sidecar Load(string fbx)
        {
            var path=SidecarPath(fbx);
            if(!File.Exists(path))throw new FileNotFoundException("Falta el JSON gemelo del contrato B0",path);
            var data=JsonUtility.FromJson<Sidecar>(File.ReadAllText(path));
            if(data.contract!=1)throw new InvalidDataException($"{path}: contrato {data.contract} no admitido");
            return data;
        }
        // Contrato: Blender (Z arriba, frente +Y, derecha +X) → Unity (Y arriba, frente +Z, derecha +X).
        public static Vector3 FromBlender(float[] p)=>new Vector3(p[0],p[2],p[1]);

        void OnPreprocessModel()
        {
            if(!Applies(assetPath))return;
            Sidecar data;
            try{data=Load(assetPath);}catch(Exception e){Debug.LogError($"B0: {assetPath} sin contrato válido: {e.Message}");return;}
            var m=(ModelImporter)assetImporter;
            m.globalScale=1;m.useFileScale=true;m.bakeAxisConversion=true;
            m.importCameras=false;m.importLights=false;m.importVisibility=false;m.importBlendShapes=false;
            m.addCollider=false;m.meshCompression=ModelImporterMeshCompression.Off;m.isReadable=false;
            m.importNormals=ModelImporterNormals.Import;m.importTangents=ModelImporterTangents.None;
            m.materialImportMode=ModelImporterMaterialImportMode.None;
            bool rigged=data.kind=="rigged";
            m.animationType=rigged?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
            m.importAnimation=rigged&&data.clips!=null&&data.clips.Length>0;
            if(rigged){m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;m.motionNodeName="";m.optimizeGameObjects=false;}
        }

        // Clips con nombre, rango y bucle del manifiesto; sin root motion (in-place).
        void OnPreprocessAnimation()
        {
            if(!Applies(assetPath))return;
            var m=(ModelImporter)assetImporter;if(!m.importAnimation)return;
            Sidecar data;try{data=Load(assetPath);}catch{return;}
            var takes=m.defaultClipAnimations;
            m.clipAnimations=data.clips.Select(c=>{
                var take=takes.FirstOrDefault(t=>t.takeName==c.take||t.takeName.EndsWith("|"+c.take,StringComparison.Ordinal));
                if(take==null){Debug.LogError($"B0: {assetPath} no contiene la toma {c.take}");return null;}
                take.name=c.name;take.firstFrame=c.start;take.lastFrame=c.end;take.loopTime=c.loop;take.loopPose=false;
                take.lockRootRotation=take.lockRootHeightY=take.lockRootPositionXZ=true;take.keepOriginalOrientation=take.keepOriginalPositionY=take.keepOriginalPositionXZ=true;
                return take;
            }).Where(c=>c!=null).ToArray();
        }

        // Detecta (no corrige) lo que el contrato prohíbe en la jerarquía importada.
        void OnPostprocessModel(GameObject root)
        {
            if(!Applies(assetPath))return;
            foreach(var c in root.GetComponentsInChildren<Component>(true))
                if(c is Collider||c is Camera||c is Light)Debug.LogError($"B0: {assetPath} contiene {c.GetType().Name} en {c.name}");
        }
    }
}
