using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mamporro.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mamporro.Tests.B0
{
    // B0.3: Pelusa, Doña Remedios y el módulo de pared importados según el contrato.
    public sealed class B0AssetTests
    {
        static readonly string[] Assets={"Pelusa/B0_Pelusa.fbx","Remedios/B0_Remedios.fbx","Pared/B0_ParedModulo.fbx"};
        static readonly string[] Rigged={"Pelusa/B0_Pelusa.fbx","Remedios/B0_Remedios.fbx"};
        static string Path(string relative)=>B0AssetImport.Root+relative;
        static GameObject Load(string relative)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(Path(relative));
            Assert.That(root,Is.Not.Null,$"Falta {relative}: scripts\\blender.cmd export");
            return root;
        }
        static Mesh MeshOf(GameObject root)
        {
            var skinned=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var filters=root.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(skinned.Length+filters.Length,Is.EqualTo(1),"una sola malla por asset");
            return skinned.Length==1?skinned[0].sharedMesh:filters[0].sharedMesh;
        }

        [TestCaseSource(nameof(Assets))]
        public void ImportsWithContractSettingsAndCleanHierarchy(string relative)
        {
            var data=B0AssetImport.Load(Path(relative));var m=(ModelImporter)AssetImporter.GetAtPath(Path(relative));
            Assert.That(m.bakeAxisConversion,Is.True);Assert.That(m.globalScale,Is.EqualTo(1f));Assert.That(m.addCollider,Is.False);
            Assert.That(m.materialImportMode,Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(m.animationType,Is.EqualTo(data.kind=="rigged"?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None));
            var root=Load(relative);
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Light>(true),Is.Empty);
            // Nodos que no son huesos: sin rotación, escala ni desplazamiento (conversión horneada).
            var smr=root.GetComponentInChildren<SkinnedMeshRenderer>(true);var bones=new HashSet<Transform>(smr?smr.bones:new Transform[0]);
            foreach(var t in root.GetComponentsInChildren<Transform>(true)){
                if(bones.Contains(t))continue;
                Assert.That(Quaternion.Angle(t.localRotation,Quaternion.identity),Is.LessThan(.01f),$"{t.name} rotado");
                Assert.That((t.localScale-Vector3.one).magnitude,Is.LessThan(1e-4f),$"{t.name} escalado");
                Assert.That(t.localPosition.magnitude,Is.LessThan(1e-4f),$"{t.name} desplazado");
            }
            var mesh=MeshOf(root);
            Assert.That(mesh.subMeshCount,Is.EqualTo(data.materials.Length));
            Assert.That(mesh.triangles.Length/3,Is.EqualTo(data.triangles));
            Assert.That(mesh.colors32.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
        }

        [TestCaseSource(nameof(Assets))]
        public void BindPoseBoundsAndProbesMatchSource(string relative)
        {
            var data=B0AssetImport.Load(Path(relative));var mesh=MeshOf(Load(relative));
            var a=B0AssetImport.FromBlender(data.boundsMin);var b=B0AssetImport.FromBlender(data.boundsMax);
            Assert.That((mesh.bounds.min-Vector3.Min(a,b)).magnitude,Is.LessThan(data.boundsTolerance*2),$"min {mesh.bounds.min:F4}");
            Assert.That((mesh.bounds.max-Vector3.Max(a,b)).magnitude,Is.LessThan(data.boundsTolerance*2),$"max {mesh.bounds.max:F4}");
            Assert.That(mesh.bounds.min.y,Is.EqualTo(0f).Within(.001f),"apoyado en el suelo");
            var v=mesh.vertices;var c=mesh.colors32;
            Assert.That(data.probes.Length,Is.GreaterThan(0));
            foreach(var p in data.probes){
                var target=B0AssetImport.FromBlender(p.position);
                int i=Enumerable.Range(0,v.Length).OrderBy(k=>(v[k]-target).sqrMagnitude).First();
                Assert.That((v[i]-target).magnitude,Is.LessThan(1e-3f),$"{relative} sonda {p.name}: ningún vértice en {target:F3}");
                var expected=new Color(p.srgb[0],p.srgb[1],p.srgb[2]).linear;Color got=c[i];
                Assert.That(Mathf.Abs(got.r-expected.r)+Mathf.Abs(got.g-expected.g)+Mathf.Abs(got.b-expected.b),Is.LessThan(4f/255),$"{relative} sonda {p.name}: color");
            }
        }

        [TestCaseSource(nameof(Rigged))]
        public void RigHasBoundedInfluencesAndBindPoses(string relative)
        {
            var root=Load(relative);var smr=root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(smr,Is.Not.Null);var mesh=smr.sharedMesh;
            int expected=relative.Contains("Pelusa")?4:12;
            Assert.That(smr.bones.Length,Is.EqualTo(expected),"solo huesos deformantes, sin leaf bones");
            Assert.That(mesh.bindposes.Length,Is.EqualTo(smr.bones.Length));
            Assert.That(smr.rootBone,Is.Not.Null);
            var perVertex=mesh.GetBonesPerVertex();
            Assert.That(perVertex.Length,Is.EqualTo(mesh.vertexCount),"todos los vértices con peso");
            Assert.That(perVertex.Max(),Is.LessThanOrEqualTo(4));Assert.That(perVertex.Min(),Is.GreaterThanOrEqualTo(1));
            Assert.That(smr.bones.All(t=>t.name.All(ch=>ch<128)),Is.True);
            Assert.That(root.GetComponentInChildren<Animator>(true),Is.Not.Null,"Generic con avatar propio");
        }

        [TestCaseSource(nameof(Rigged))]
        public void ClipsKeepNamesLengthLoopAndStayInPlace(string relative)
        {
            var data=B0AssetImport.Load(Path(relative));
            var clips=AssetDatabase.LoadAllAssetsAtPath(Path(relative)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Assert.That(clips.Select(c=>c.name).OrderBy(n=>n),Is.EqualTo(data.clips.Select(c=>c.name).OrderBy(n=>n)));
            foreach(var spec in data.clips){
                var clip=clips.First(c=>c.name==spec.name);
                Assert.That(clip.length,Is.EqualTo((spec.end-spec.start)/(float)data.fps).Within(1e-3f),spec.name);
                Assert.That(clip.isLooping,Is.EqualTo(spec.loop),spec.name);
                Assert.That(clip.hasMotionCurves||clip.hasRootCurves,Is.False,$"{spec.name}: sin root motion");
                // In-place: la raíz del rig no se desplaza a lo largo del clip.
                foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(bd=>bd.path.EndsWith("Root")&&bd.propertyName.StartsWith("m_LocalPosition"))){
                    var keys=AnimationUtility.GetEditorCurve(clip,binding).keys;
                    Assert.That(keys.Max(k=>k.value)-keys.Min(k=>k.value),Is.LessThan(1e-4f),$"{spec.name}: Root se mueve ({binding.propertyName})");
                }
            }
        }

        [Test]
        public void WallModuleTilesEveryFourMeters()
        {
            var mesh=MeshOf(Load("Pared/B0_ParedModulo.fbx"));var v=mesh.vertices;
            Assert.That(mesh.bounds.min.x,Is.EqualTo(0f).Within(1e-4f),"pivote en la esquina");Assert.That(mesh.bounds.max.x,Is.EqualTo(4f).Within(1e-4f));
            Assert.That(mesh.bounds.max.y,Is.EqualTo(3f).Within(1e-4f));
            // Perfil (y, z) del borde izquierdo = perfil del borde derecho: dos módulos empalman sin huecos.
            HashSet<Vector2Int> Profile(float x)=>new HashSet<Vector2Int>(v.Where(p=>Mathf.Abs(p.x-x)<1e-4f).Select(p=>new Vector2Int(Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000))));
            var left=Profile(0);var right=Profile(4);
            Assert.That(left.Count,Is.GreaterThan(0));Assert.That(left.SetEquals(right),Is.True,"los bordes del módulo no coinciden");
        }

        // B0.4: la escena QA sigue resolviendo mallas, clips, prefabs y materiales tras reimportar.
        [Test]
        public void QaSceneReferencesSurviveReimport()
        {
            string[] Ids()
            {
                var scene=EditorSceneManager.OpenScene(B0Project.ScenePath,OpenSceneMode.Additive);
                try{
                    var lib=scene.GetRootGameObjects().Select(o=>o.GetComponent<Mamporro.U3.B0VisualLibrary>()).Single(l=>l!=null);
                    Object[] refs={lib.pelusa,lib.remedios,lib.wall,lib.remediosWalk,lib.remediosIdle,lib.pelusaWalk,lib.retro,lib.retroFlash};
                    Assert.That(refs.All(r=>r!=null),Is.True,"referencia rota en B0_QA");
                    return refs.Select(r=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(r,out string g,out long id);return g+":"+id;}).ToArray();
                }finally{EditorSceneManager.CloseScene(scene,true);}
            }
            var before=Ids();
            foreach(var a in Assets)AssetDatabase.ImportAsset(Path(a),ImportAssetOptions.ForceUpdate);
            Assert.That(Ids(),Is.EqualTo(before));
        }

        [TestCaseSource(nameof(Assets))]
        public void ReimportKeepsGuidAndMeshFileId(string relative)
        {
            var mesh=MeshOf(Load(relative));
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id),Is.True);
            string meta=File.ReadAllText(Path(relative)+".meta");
            AssetDatabase.ImportAsset(Path(relative),ImportAssetOptions.ForceUpdate);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(MeshOf(Load(relative)),out string guid2,out long id2),Is.True);
            Assert.That(guid2,Is.EqualTo(guid));Assert.That(id2,Is.EqualTo(id));
            Assert.That(File.ReadAllText(Path(relative)+".meta"),Is.EqualTo(meta));
            TestContext.Out.WriteLine($"B0-ID {relative} {guid} {id} {B0AssetImport.Load(Path(relative)).digest}");
        }
    }
}
