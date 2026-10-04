using System.Collections.Generic;
using Mamporro.Core.Effects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // Dibujo de las partículas decorativas: cubos instanciados con color por instancia
    // (_InstanceColor de RetroWorld), en lotes de 1023 sobre la textura interna (pixelados, con
    // ajuste de vértices y dithering). Buffers fijos; sin GameObjects ni asignaciones por fotograma.
    public sealed class ParticleRenderer
    {
        const int Chunk=1023,Chunks=(ParticleField.MaxCapacity+Chunk-1)/Chunk;
        static readonly int ColorId=Shader.PropertyToID("_InstanceColor");
        readonly Matrix4x4[][] matrices=new Matrix4x4[Chunks][];
        readonly Vector4[][] colors=new Vector4[Chunks][];
        readonly MaterialPropertyBlock[] blocks=new MaterialPropertyBlock[Chunks];
        readonly Dictionary<uint,Vector4> linear=new Dictionary<uint,Vector4>(64);
        readonly Material material;readonly Mesh mesh;
        public int Drawn {get;private set;}
        public ParticleRenderer(Material template,Mesh cube)
        {
            material=new Material(template){name="Partículas U5",enableInstancing=true};material.SetColor("_BaseColor",Color.white);mesh=cube;
            for(int c=0;c<Chunks;c++){matrices[c]=new Matrix4x4[Chunk];colors[c]=new Vector4[Chunk];blocks[c]=new MaterialPropertyBlock();}
        }
        Vector4 Linear(uint hex){if(!linear.TryGetValue(hex,out var v)){v=WebSpace.Linear(hex);linear[hex]=v;}return v;}
        public void Draw(ParticleField field,Camera camera)
        {
            Drawn=0;int count=field.Active;
            for(int c=0;c*Chunk<count;c++){
                int n=Mathf.Min(Chunk,count-c*Chunk);var m=matrices[c];var col=colors[c];
                for(int k=0;k<n;k++){
                    int i=c*Chunk+k;float s=field.Scale(i);
                    // Coordenadas web → Unity (Z invertida); el giro es solo visual.
                    m[k]=Matrix4x4.TRS(new Vector3(field.X[i],field.Y[i],-field.Z[i]),Quaternion.Euler(0,-field.Spin[i]*Mathf.Rad2Deg,0),new Vector3(s,s,s));
                    col[k]=Linear(field.Color[i]);
                }
                blocks[c].SetVectorArray(ColorId,col);
                var rp=new RenderParams(material){camera=camera,matProps=blocks[c],shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=new Bounds(new Vector3(0,80,0),new Vector3(340,200,340))};
                Graphics.RenderMeshInstanced(rp,mesh,0,m,n);Drawn+=n;
            }
        }
        public void Destroy(){if(material)Object.Destroy(material);}
    }
}
