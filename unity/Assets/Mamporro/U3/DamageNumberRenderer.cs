using Mamporro.Core.Effects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // Dibujo de los números de daño: una malla dinámica de quads orientados a la cámara (como
    // el vertex shader web), atlas de los glifos de la fuente pixelada (src/ui/font/glyphs.ts,
    // celdas 7×9 con contorno negro de 1 píxel) y color por tipo. Buffers fijos; dibujada en la
    // textura interna, así que sale pixelada.
    public sealed class DamageNumberRenderer
    {
        const int CellW=7,CellH=9,MaxGlyphs=DamageNumbers.MaxNumbers*DamageNumbers.MaxChars;
        const float GlyphHeight=.42f;
        // Glifos de src/ui/font/glyphs.ts (filas separadas por espacios).
        static readonly string[] Glyphs={
            ".###. #...# #..## #.#.# ##..# #...# .###.",".#. ##. .#. .#. .#. .#. ###",".###. #...# ....# ...#. ..#.. .#... #####",
            "####. ....# ....# .###. ....# ....# ####.","...#. ..##. .#.#. #..#. ##### ...#. ...#.","##### #.... ####. ....# ....# #...# .###.",
            ".###. #.... #.... ####. #...# #...# .###.","##### ....# ...#. ..#.. ..#.. ..#.. ..#..",".###. #...# #...# .###. #...# #...# .###.",
            ".###. #...# #...# .#### ....# ....# .###.","# # # # # . #"};
        readonly Mesh mesh;readonly Material material;readonly Texture2D atlas;
        readonly DamageGlyph[] glyphs=new DamageGlyph[MaxGlyphs];
        readonly Vector3[] vertices=new Vector3[MaxGlyphs*4];readonly Vector2[] uvs=new Vector2[MaxGlyphs*4];readonly Color[] colors=new Color[MaxGlyphs*4];
        readonly int[] indices=new int[MaxGlyphs*6];
        // Colores de palette.ts: normal, crítico, supercrítico y daño recibido (lineales).
        readonly Color normal=WebSpace.Linear(0xffffff),crit=WebSpace.Linear(0xffd23f),super=WebSpace.Linear(0xff7a2f),player=WebSpace.Linear(0xff4a4a);
        public int DrawnGlyphs {get;private set;}

        public DamageNumberRenderer()
        {
            atlas=new Texture2D(DamageNumbers.Chars.Length*CellW,CellH,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Atlas números de daño"};
            var pixels=new Color32[atlas.width*atlas.height];
            void Fill(int px,int py,Color32 c){if(px<0||py<0||px>=atlas.width||py>=CellH)return;pixels[(CellH-1-py)*atlas.width+px]=c;}
            for(int pass=0;pass<2;pass++)for(int g=0;g<Glyphs.Length;g++){
                var rows=Glyphs[g].Split(' ');int width=rows[0].Length,ox=g*CellW+1+(5-width)/2;
                for(int ry=0;ry<rows.Length;ry++)for(int rx=0;rx<rows[ry].Length;rx++){
                    if(rows[ry][rx]!='#')continue;
                    if(pass==0){for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)Fill(ox+rx+dx,1+ry+dy,new Color32(0,0,0,255));}
                    else Fill(ox+rx,1+ry,new Color32(255,255,255,255));
                }
            }
            atlas.SetPixels32(pixels);atlas.Apply(false,true);
            material=new Material(Resources.Load<Shader>("DamageNumbers")){name="Números de daño",mainTexture=atlas,renderQueue=4000};
            mesh=new Mesh{name="Números de daño"};mesh.MarkDynamic();
            for(int q=0;q<MaxGlyphs;q++){int v=q*4,i=q*6;indices[i]=v;indices[i+1]=v+1;indices[i+2]=v+2;indices[i+3]=v;indices[i+4]=v+2;indices[i+5]=v+3;}
            mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetIndices(indices,MeshTopology.Triangles,0,false);
            mesh.bounds=new Bounds(new Vector3(0,80,0),new Vector3(400,300,400));
        }
        public void Draw(DamageNumbers numbers,Camera camera)
        {
            int n=numbers.Layout(glyphs);DrawnGlyphs=n;
            if(n==0)return;
            var right=camera.transform.right;var up=camera.transform.up;
            float w=GlyphHeight*CellW/CellH,h=GlyphHeight,advance=GlyphHeight*(CellW-1)/CellH,du=1f/DamageNumbers.Chars.Length;
            for(int q=0;q<n;q++){
                var g=glyphs[q];var center=new Vector3(g.X,g.Y,-g.Z);float s=g.Scale;
                var c=g.Kind<0?player:g.Kind>=2?super:g.Kind==1?crit:normal;c.a=g.Alpha;
                float left=(g.Slot*advance-w*.5f)*s,rightX=(g.Slot*advance+w*.5f)*s,bottom=-h*.5f*s,top=h*.5f*s;
                int v=q*4;
                vertices[v]=center+right*left+up*bottom;vertices[v+1]=center+right*left+up*top;vertices[v+2]=center+right*rightX+up*top;vertices[v+3]=center+right*rightX+up*bottom;
                float u0=g.Glyph*du,u1=u0+du;uvs[v]=new Vector2(u0,0);uvs[v+1]=new Vector2(u0,1);uvs[v+2]=new Vector2(u1,1);uvs[v+3]=new Vector2(u1,0);
                colors[v]=colors[v+1]=colors[v+2]=colors[v+3]=c;
            }
            // Primero sin índices: si no, Unity valida los vértices nuevos contra los del fotograma anterior.
            mesh.SetIndices(indices,0,0,MeshTopology.Triangles,0,false);
            mesh.SetVertices(vertices,0,n*4,MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices);
            mesh.SetUVs(0,uvs,0,n*4,MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices);
            mesh.SetColors(colors,0,n*4,MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndices(indices,0,n*6,MeshTopology.Triangles,0,false);
            var rp=new RenderParams(material){camera=camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=mesh.bounds};
            Graphics.RenderMesh(rp,mesh,0,Matrix4x4.identity);
        }
        public void Destroy(){if(material)Object.Destroy(material);if(atlas)Object.Destroy(atlas);if(mesh)Object.Destroy(mesh);}
    }
}
