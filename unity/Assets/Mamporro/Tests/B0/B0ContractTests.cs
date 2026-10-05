using System.IO;
using System.Linq;
using Mamporro.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mamporro.Tests.B0
{
    // B0.2: la fixture asimétrica atraviesa Blender → FBX → Unity sin giros, espejo ni factor
    // 100; conserva pivote, normales duras/suaves, UV, colores, slots y GUID/fileID al reimportar.
    public sealed class B0ContractTests
    {
        const string Fixture=B0AssetImport.Root+"Fixture/B0_Fixture.fbx";
        static B0AssetImport.Sidecar Data=>B0AssetImport.Load(Fixture);
        static GameObject Root()
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(Fixture);
            Assert.That(root,Is.Not.Null,"Falta la fixture exportada: scripts\\blender.cmd export");
            return root;
        }
        static Mesh FixtureMesh()
        {
            var filters=Root().GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters.Length,Is.EqualTo(1),"una sola malla");
            return filters[0].sharedMesh;
        }
        static bool AxisAligned(Vector3 n)=>Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z))>.999f;

        [Test]
        public void ImporterFollowsContract()
        {
            var m=(ModelImporter)AssetImporter.GetAtPath(Fixture);
            Assert.That(m.globalScale,Is.EqualTo(1f));Assert.That(m.useFileScale,Is.True);Assert.That(m.bakeAxisConversion,Is.True);
            Assert.That(m.addCollider,Is.False);Assert.That(m.importCameras,Is.False);Assert.That(m.importLights,Is.False);
            Assert.That(m.materialImportMode,Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(m.animationType,Is.EqualTo(ModelImporterAnimationType.None));
            Assert.That(m.importNormals,Is.EqualTo(ModelImporterNormals.Import));
        }

        [Test]
        public void HierarchyIsUnrotatedAndWithoutForbiddenComponents()
        {
            var root=Root();
            foreach(var t in root.GetComponentsInChildren<Transform>(true)){
                Assert.That(t.localPosition.magnitude,Is.LessThan(1e-5f),t.name);
                Assert.That(Quaternion.Angle(t.localRotation,Quaternion.identity),Is.LessThan(.01f),$"{t.name} rotado: falta la conversión de ejes");
                Assert.That((t.localScale-Vector3.one).magnitude,Is.LessThan(1e-5f),$"{t.name} escalado");
            }
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Light>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Animator>(true),Is.Empty);
            Assert.That(FixtureMesh().name,Is.EqualTo("B0_Fixture"));
        }

        [Test]
        public void BoundsAreInMetersAndNotMirrored()
        {
            var d=Data;var a=B0AssetImport.FromBlender(d.boundsMin);var b=B0AssetImport.FromBlender(d.boundsMax);
            var expected=new Bounds();expected.SetMinMax(Vector3.Min(a,b),Vector3.Max(a,b));
            var bounds=FixtureMesh().bounds;
            Assert.That((bounds.min-expected.min).magnitude,Is.LessThan(d.boundsTolerance),$"min {bounds.min:F4} vs {expected.min:F4}");
            Assert.That((bounds.max-expected.max).magnitude,Is.LessThan(d.boundsTolerance),$"max {bounds.max:F4} vs {expected.max:F4}");
            // Asimetría: el brazo (derecha del personaje) llega a +1,5 en X y el frente a +1,25 en Z.
            Assert.That(bounds.max.x,Is.EqualTo(1.5f).Within(.001f));Assert.That(bounds.max.z,Is.EqualTo(1.25f).Within(.001f));
            Assert.That(bounds.min.y,Is.EqualTo(0f).Within(.001f),"pivote en el suelo");
        }

        [Test]
        public void ProbesKeepOrientationAndLinearVertexColor()
        {
            var mesh=FixtureMesh();var v=mesh.vertices;var c=mesh.colors32;
            Assert.That(c.Length,Is.EqualTo(v.Length),"color de vértice importado");
            foreach(var p in Data.probes){
                var target=B0AssetImport.FromBlender(p.position);
                int i=Enumerable.Range(0,v.Length).OrderBy(k=>(v[k]-target).sqrMagnitude).First();
                Assert.That((v[i]-target).magnitude,Is.LessThan(1e-3f),$"sonda {p.name}: ningún vértice en {target:F3}");
                // Exportado en lineal: Unity recibe el valor lineal del sRGB de la fuente (8 bits).
                var expected=new Color(p.srgb[0],p.srgb[1],p.srgb[2]).linear;Color got=c[i];
                Assert.That(Mathf.Abs(got.r-expected.r)+Mathf.Abs(got.g-expected.g)+Mathf.Abs(got.b-expected.b),Is.LessThan(4f/255),$"sonda {p.name}: color {got} vs {expected}");
            }
        }

        [Test]
        public void HardAndSoftEdgesSurvive()
        {
            var mesh=FixtureMesh();var v=mesh.vertices;var n=mesh.normals;
            Assert.That(n.Length,Is.EqualTo(v.Length));
            int soft=0;
            for(int i=0;i<v.Length;i++){
                if(AxisAligned(n[i]))continue;
                soft++;Assert.That(v[i].y,Is.GreaterThan(2f-1e-3f),$"normal promediada fuera de la antena en {v[i]}");
            }
            Assert.That(soft,Is.GreaterThanOrEqualTo(16),"la antena lisa conserva normales suaves");
        }

        [Test]
        public void TopologyUvAndSlotsMatchExport()
        {
            var d=Data;var mesh=FixtureMesh();
            Assert.That(mesh.triangles.Length/3,Is.EqualTo(d.triangles));
            Assert.That(mesh.subMeshCount,Is.EqualTo(d.materials.Length),"un submesh por slot");
            var uv=mesh.uv;Assert.That(uv.Length,Is.EqualTo(mesh.vertexCount),"UV importada");
            Assert.That(uv.All(u=>u.x>=-1e-4f&&u.x<=1+1e-4f&&u.y>=-1e-4f&&u.y<=1+1e-4f),Is.True);
            // Orden de slots: el 1 (B0_Marker) contiene frente y brazo; el 0 (B0_Body), no.
            var v=mesh.vertices;
            Bounds Sub(int s){var idx=mesh.GetIndices(s);var b=new Bounds(v[idx[0]],Vector3.zero);foreach(int k in idx)b.Encapsulate(v[k]);return b;}
            var marker=Sub(1);var body=Sub(0);
            Assert.That(marker.max.x,Is.EqualTo(1.5f).Within(.001f));Assert.That(marker.max.z,Is.EqualTo(1.25f).Within(.001f));
            Assert.That(body.max.x,Is.EqualTo(.5f).Within(.001f));Assert.That(body.max.z,Is.EqualTo(.25f).Within(.001f));
        }

        [Test]
        public void NoMaterialsTexturesOrPrefabsGenerated()
        {
            Assert.That(AssetDatabase.FindAssets("t:Material",new[]{B0AssetImport.Root.TrimEnd('/')}),Is.Empty);
            Assert.That(AssetDatabase.FindAssets("t:Texture",new[]{B0AssetImport.Root.TrimEnd('/')}),Is.Empty);
        }

        [Test]
        public void ReimportKeepsGuidAndFileIds()
        {
            var mesh=FixtureMesh();
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id),Is.True);
            string meta=File.ReadAllText(Fixture+".meta");
            for(int k=0;k<2;k++)AssetDatabase.ImportAsset(Fixture,ImportAssetOptions.ForceUpdate);
            var again=FixtureMesh();
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(again,out string guid2,out long id2),Is.True);
            Assert.That(guid2,Is.EqualTo(guid));Assert.That(id2,Is.EqualTo(id));
            // Huella para comparar entre exportaciones forzadas (los IDs internos del FBX cambian).
            TestContext.Out.WriteLine($"B0-ID {guid} {id} {Data.digest}");
            Assert.That(File.ReadAllText(Fixture+".meta"),Is.EqualTo(meta),"el .meta no cambia al reimportar");
        }
    }
}
