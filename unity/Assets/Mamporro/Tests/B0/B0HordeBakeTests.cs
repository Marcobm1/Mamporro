using System.Linq;
using Mamporro.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mamporro.Tests.B0
{
    // B0.5: poses discretas (C) y VAT (D) horneadas del mismo clip y la misma malla de la Pelusa.
    public sealed class B0HordeBakeTests
    {
        const string Generated=B0Project.Root+"/Generated/";
        static Mesh Source=>AssetDatabase.LoadAllAssetsAtPath(B0AssetImport.Root+"Pelusa/B0_Pelusa.fbx").OfType<Mesh>().First(m=>m.name=="B0_Pelusa");
        static Mesh Pose(int k)=>AssetDatabase.LoadAssetAtPath<Mesh>(Generated+$"B0_Pelusa_Pose_{k}.asset");
        static Texture2D Vat=>AssetDatabase.LoadAssetAtPath<Texture2D>(Generated+"B0_Pelusa_VAT.asset");

        [Test]
        public void PosesKeepTopologyColorsAndUv()
        {
            var source=Source;
            for(int k=0;k<B0Project.Poses;k++){
                var pose=Pose(k);Assert.That(pose,Is.Not.Null,$"falta la pose {k}: scripts\\b0.cmd create");
                Assert.That(pose.vertexCount,Is.EqualTo(source.vertexCount));Assert.That(pose.triangles,Is.EqualTo(source.triangles));
                Assert.That(pose.colors32,Is.EqualTo(source.colors32),"mismos colores de vértice");Assert.That(pose.uv,Is.EqualTo(source.uv));
                Assert.That(pose.bounds.min.y,Is.GreaterThan(-.05f),$"pose {k} sin hundirse en el suelo");
            }
            Assert.That(Pose(0).vertices,Is.Not.EqualTo(Pose(2).vertices),"las poses cambian");
        }

        [Test]
        public void VatMatchesPosesAndClip()
        {
            var vat=Vat;var source=Source;
            Assert.That(vat,Is.Not.Null);Assert.That(vat.width,Is.EqualTo(source.vertexCount));Assert.That(vat.height,Is.EqualTo(20),"Pelusa_Walk: 20 fotogramas a 30 fps");
            Assert.That(vat.format,Is.EqualTo(TextureFormat.RGBAHalf));Assert.That(vat.filterMode,Is.EqualTo(FilterMode.Point));
            void Same(int frame,int pose)
            {
                var v=Pose(pose).vertices;float worst=0;
                for(int i=0;i<v.Length;i++){var c=vat.GetPixel(i,frame);worst=Mathf.Max(worst,(new Vector3(c.r,c.g,c.b)-v[i]).magnitude);}
                Assert.That(worst,Is.LessThan(2e-3f),$"fotograma {frame} = pose {pose} (precisión half)");
            }
            Same(0,0);Same(10,4);
            var material=AssetDatabase.LoadAssetAtPath<Material>(B0Project.Root+"/B0_PelusaVAT.mat");
            Assert.That(material.shader.name,Is.EqualTo("Mamporro/RetroWorldVAT"));Assert.That(material.enableInstancing,Is.True);
            Assert.That(material.GetTexture("_VatTex"),Is.SameAs(vat));Assert.That(material.GetFloat("_VatFrames"),Is.EqualTo(20));
        }
    }
}
