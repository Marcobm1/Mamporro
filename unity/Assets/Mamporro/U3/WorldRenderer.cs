using System;
using System.Collections.Generic;
using Mamporro.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mamporro.U3
{
    // Construye por código las mallas del mundo de una semilla (equivalente a World.ts,
    // TerrainMesh.ts, PropMeshes.ts, DecorationMeshes.ts y GroundCoverMeshes.ts) con el
    // shader retro de U3. Todo queda en pocas mallas estáticas combinadas.
    public sealed class WorldRenderer : MonoBehaviour
    {
        public Shader worldShader;
        public Shader skyShader;
        public bool groundCover=true;
        public WorldData World {get;private set;}
        readonly List<GameObject> built=new List<GameObject>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        Texture2D detail,stone;
        GameObject sky,coverObject;

        static readonly int TimeId=Shader.PropertyToID("_RetroTime"),FogColorId=Shader.PropertyToID("_RetroFogColor"),FogNearId=Shader.PropertyToID("_RetroFogNear"),
            FogFarId=Shader.PropertyToID("_RetroFogFar"),SunDirId=Shader.PropertyToID("_RetroSunDir"),SkyId=Shader.PropertyToID("_RetroSky"),GroundId=Shader.PropertyToID("_RetroGround"),SunId=Shader.PropertyToID("_RetroSun");
        // Dirección hacia el sol de la web (0,45; 0,6; -0,66), pasada a Unity (Z invertida).
        public static readonly Vector3 SunDirection=new Vector3(0.45f,0.6f,0.66f).normalized;

        public static void SetGlobals()
        {
            // Lambert de three.js: hemisférica 1,4 y sol 2,4, divididas por π.
            float k=1/Mathf.PI;
            Shader.SetGlobalColor(FogColorId,WebSpace.Linear(Palette.SkyHorizon));
            Shader.SetGlobalFloat(FogNearId,(float)Tuning.RenderFogNear);Shader.SetGlobalFloat(FogFarId,(float)Tuning.RenderFogFar);
            Shader.SetGlobalVector(SunDirId,SunDirection);
            Shader.SetGlobalColor(SkyId,WebSpace.Linear(Palette.HemiSky)*1.4f*k);Shader.SetGlobalColor(GroundId,WebSpace.Linear(Palette.HemiGround)*1.4f*k);
            Shader.SetGlobalColor(SunId,WebSpace.Linear(Palette.SunLight)*2.4f*k);
        }

        void Update(){Shader.SetGlobalFloat(TimeId,Time.time);}

        Material Material(string name,Texture texture,double meters,bool worldUV,Vector4 wind=default,bool doubleSided=false)
        {
            var m=new Material(worldShader){name=name};m.SetTexture("_MainTex",texture?texture:Texture2D.whiteTexture);m.SetFloat("_TexMeters",(float)meters);
            m.SetFloat("_WorldUV",worldUV?1:0);m.SetVector("_Wind",wind);m.SetFloat("_Cull",doubleSided?0:2);owned.Add(m);return m;
        }

        GameObject Add(string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;owned.Add(mesh);built.Add(go);return go;
        }

        public void Clear()
        {
            foreach(var go in built)if(go)Destroy(go);built.Clear();
            foreach(var o in owned)if(o)Destroy(o);owned.Clear();
            World=null;
        }

        public void Build(WorldData world,string seed)
        {
            Clear();World=world;SetGlobals();
            if(!detail){detail=RetroTextures.Detail();stone=RetroTextures.Stone();}
            var rng=new Rng(seed);
            Add("Terreno",TerrainMesh(world,rng.Derive("terrain-colors")),Material("Terreno",detail,4,true));
            BuildProps(world);
            BuildDecorations(world,rng.Derive("decoration-meshes"));
            coverObject=BuildGroundCover(world);
            if(coverObject)coverObject.SetActive(groundCover);
            BuildInteractables(world);
            BuildSky();
        }

        public void SetGroundCover(bool on){groundCover=on;if(coverObject)coverObject.SetActive(on);}

        // ---------------------------------------------------------------- terreno

        static Color SiteFloor(List<Site> sites,double x,double z,out bool found)
        {
            found=true;
            foreach(var site in sites){
                double d=JsMath.Hypot(x-site.X,z-site.Z);
                if(d>site.Radius*0.95)continue;
                if(site.Kind=="farm")return WebSpace.Linear(Palette.Dirt);
                if(site.Kind=="well"&&d>2.2)return WebSpace.Linear(Palette.Dirt);
                bool checker=((long)Math.Floor(x/1.3)+(long)Math.Floor(z/1.3))%2==0;
                return WebSpace.Linear(checker?Palette.Cobble:Palette.CobbleDark);
            }
            found=false;return default;
        }

        static Mesh TerrainMesh(WorldData world,Rng rng)
        {
            var hf=world.Heightfield;var patches=new SimplexNoise2D(rng.Next);int n=hf.Stride;
            var positions=new Vector3[n*n];var colors=new Color[n*n];
            Color grassLight=WebSpace.Linear(Palette.GrassLight),grass=WebSpace.Linear(Palette.Grass),grassDark=WebSpace.Linear(Palette.GrassDark),moss=WebSpace.Linear(Palette.Moss),
                dirt=WebSpace.Linear(Palette.Dirt),sand=WebSpace.Linear(Palette.Sand),rock=WebSpace.Linear(Palette.Rock),snow=WebSpace.Linear(Palette.Snow);
            Color[] rockBands={WebSpace.Linear(Palette.RockLight),rock,WebSpace.Linear(Palette.RockDark)};
            for(int j=0;j<n;j++)for(int i=0;i<n;i++){
                int index=j*n+i;double x=i*hf.CellSize-hf.Half,z=j*hf.CellSize-hf.Half,y=hf.VertexHeight(i,j);
                positions[index]=WebSpace.ToUnity(x,y,z);
                double gx=(hf.VertexHeight(i+1,j)-hf.VertexHeight(i-1,j))/(2*hf.CellSize),gz=(hf.VertexHeight(i,j+1)-hf.VertexHeight(i,j-1))/(2*hf.CellSize);
                double ny=1/Math.Sqrt(gx*gx+1+gz*gz),patch=patches.Noise(x*0.04,z*0.04)+0.35*patches.Noise(x*0.15+30,z*0.15+30);
                var floor=SiteFloor(world.Sites,x,z,out bool onSite);Color c;
                if(onSite)c=floor;
                else if(ny<0.7)c=rockBands[Math.Abs((long)Math.Floor(y/1.6))%3];
                else if(y>48)c=snow;
                else if(y>34)c=patch>0?moss:rock;
                else if(ny<0.8)c=dirt;
                else{
                    c=patch>0.45?grassLight:patch< -0.4?grassDark:grass;
                    if(y<2.5)c=Color.Lerp(c,patch>0.2?sand:dirt,0.45f);
                }
                colors[index]=c;
            }
            // Mismo reparto de triángulos que heightAt: (a, b, d) y (b, c, d), invertidos al pasar a Unity.
            int cells=hf.Cells;var indices=new int[cells*cells*6];int k=0;
            for(int j=0;j<cells;j++)for(int i=0;i<cells;i++){
                int a=j*n+i,b=(j+1)*n+i,c=(j+1)*n+i+1,d=j*n+i+1;
                indices[k++]=a;indices[k++]=d;indices[k++]=b;indices[k++]=b;indices[k++]=d;indices[k++]=c;
            }
            var mesh=new Mesh{name="Terreno U3",indexFormat=IndexFormat.UInt32};
            mesh.vertices=positions;mesh.colors=colors;mesh.SetIndices(indices,MeshTopology.Triangles,0);mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- construcciones

        static Shape PartShape(Part p)
        {
            double a=p.Size[0],b=p.Size[1],c=p.Size[2];
            switch(p.Shape){
                case "box":return Shape.Box(a,b,c);
                case "cylinder":return Shape.Cylinder(c,a,b,p.Segments);
                case "cone":return Shape.Cylinder(0,a,b,p.Segments);
                case "ico":return Shape.Icosahedron(a).Scale(1,b,1);
                default:return Shape.Dodecahedron(a).Scale(1,b,1);
            }
        }

        static double ShadeJitter(Part p){double h=Math.Sin(p.Position[0]*12.9898+p.Position[2]*78.233)*43758.5453;return 0.92+(h-Math.Floor(h))*0.16;}

        // UV proyectadas desde el eje dominante de cada cara, en metros del mundo (projectUVs).
        static Vector2 ProjectUV(WV v,double nx,double ny,double nz,double meters)
        {
            double u,w;
            if(ny>=nx&&ny>=nz){u=v.X;w=v.Z;}else if(nx>=nz){u=v.Z;w=v.Y;}else{u=v.X;w=v.Y;}
            return new Vector2((float)(u/meters),(float)(w/meters));
        }

        static void Emit(MeshBuilder builder,Shape shape,WebTransform t,Func<int,Color> color,double meters,Vector4 wind,bool projectUV)
        {
            for(int k=0;k<shape.Triangles.Count;k+=3){
                WV a=t.Apply(shape.Vertices[shape.Triangles[k]]),b=t.Apply(shape.Vertices[shape.Triangles[k+1]]),c=t.Apply(shape.Vertices[shape.Triangles[k+2]]);
                Vector2 ua,ub,uc;
                if(projectUV){
                    double ux=b.X-a.X,uy=b.Y-a.Y,uz=b.Z-a.Z,vx=c.X-a.X,vy=c.Y-a.Y,vz=c.Z-a.Z;
                    double nx=Math.Abs(uy*vz-uz*vy),ny=Math.Abs(uz*vx-ux*vz),nz=Math.Abs(ux*vy-uy*vx);
                    ua=ProjectUV(a,nx,ny,nz,meters);ub=ProjectUV(b,nx,ny,nz,meters);uc=ProjectUV(c,nx,ny,nz,meters);
                }else{ua=new Vector2((float)(a.X/meters),(float)(a.Y/meters));ub=new Vector2((float)(b.X/meters),(float)(b.Y/meters));uc=new Vector2((float)(c.X/meters),(float)(c.Y/meters));}
                builder.Triangle(a,b,c,color(k/3),ua,ub,uc,wind);
            }
        }

        void BuildProps(WorldData world)
        {
            var stoneBuilder=new MeshBuilder();var plainBuilder=new MeshBuilder();
            foreach(var p in world.Props.Parts){
                bool isStone=p.Material=="stone";var t=WebTransform.Of(p.Position[0],p.Position[1],p.Position[2],p.Rotation[0],p.Rotation[1],p.Rotation[2]);
                var color=WebSpace.Linear(p.Color)*(float)ShadeJitter(p);color.a=1;
                Emit(isStone?stoneBuilder:plainBuilder,PartShape(p),t,_=>color,isStone?1.6:1.2,default,true);
            }
            if(stoneBuilder.VertexCount>0)Add("Construcciones · piedra",stoneBuilder.Build("Props piedra"),Material("Props piedra",stone,1,false));
            if(plainBuilder.VertexCount>0)Add("Construcciones · resto",plainBuilder.Build("Props resto"),Material("Props resto",detail,1,false));
        }

        // ---------------------------------------------------------------- vegetación

        static Vector4 WindData(double x,double z,double localY,double scale)=>new Vector4((float)x,(float)z,(float)localY,(float)scale);

        static void EmitInstanced(MeshBuilder builder,Shape shape,double x,double y,double z,double rotation,double scale,Func<int,Color> color,bool wind)
        {
            var t=WebTransform.Of(x,y,z,0,rotation,0,scale,scale,scale);
            for(int k=0;k<shape.Triangles.Count;k+=3){
                WV la=shape.Vertices[shape.Triangles[k]],lb=shape.Vertices[shape.Triangles[k+1]],lc=shape.Vertices[shape.Triangles[k+2]];
                WV a=t.Apply(la),b=t.Apply(lb),c=t.Apply(lc);
                builder.Triangle(a,b,c,color(k/3),color(k/3),color(k/3),new Vector2((float)(la.X+la.Z),(float)la.Y),new Vector2((float)(lb.X+lb.Z),(float)lb.Y),new Vector2((float)(lc.X+lc.Z),(float)lc.Y),
                    wind?WindData(x,z,la.Y,scale):default,wind?WindData(x,z,lb.Y,scale):default,wind?WindData(x,z,lc.Y,scale):default);
            }
        }

        void BuildDecorations(WorldData world,Rng rng)
        {
            uint[] canopyColors={Palette.Leaf,Palette.LeafLight,Palette.Leaf,Palette.LeafYellow,Palette.LeafOrange};
            var trunk=Shape.Cylinder(0.22,0.32,2.4,5).Translate(0,1.2,0);
            var canopy=Shape.Icosahedron(1.55).Jitter(rng,0.22).Translate(0,3.1,0);
            var pine=Shape.Cylinder(0.18,0.26,1.4,5).Translate(0,0.7,0).Tint(Palette.Bark)
                .Merge(Shape.Cylinder(0,1.45,2.4,6).Translate(0,2.3,0).Tint(Palette.Pine))
                .Merge(Shape.Cylinder(0,1.05,2,6).Translate(0,3.6,0).Tint(Palette.Pine));
            var rocks=new Shape[3];
            for(int v=0;v<3;v++)rocks[v]=Shape.Dodecahedron(1).Jitter(rng,0.18).Scale(1,Vegetation.RockHeightScale,1);
            var bush=Shape.Icosahedron(0.75).Jitter(rng,0.12).Scale(1.2,0.75,1.2).Translate(0,0.4,0);

            var bark=new MeshBuilder();var leaves=new MeshBuilder();var pines=new MeshBuilder();var rockBuilder=new MeshBuilder();var bushes=new MeshBuilder();
            Color barkColor=WebSpace.Linear(Palette.Bark),rockColor=WebSpace.Linear(Palette.Rock),bushColor=WebSpace.Linear(Palette.Bush);
            foreach(var d in world.Decorations){
                switch(d.Kind){
                    case "tree":
                        EmitInstanced(bark,trunk,d.X,d.Y,d.Z,d.Rotation,d.Scale,_=>barkColor,false);
                        var leaf=WebSpace.Linear(canopyColors[Math.Min(canopyColors.Length-1,(int)Math.Floor(d.Variant*canopyColors.Length))]);
                        EmitInstanced(leaves,canopy,d.X,d.Y,d.Z,d.Rotation,d.Scale,_=>leaf,true);break;
                    case "pine":EmitInstanced(pines,pine,d.X,d.Y,d.Z,d.Rotation,d.Scale,k=>WebSpace.Linear(pine.Colors[k]),true);break;
                    case "rock":EmitInstanced(rockBuilder,rocks[Math.Min(2,(int)Math.Floor(d.Variant*3))],d.X,d.Y,d.Z,d.Rotation,d.Scale,_=>rockColor,false);break;
                    case "bush":EmitInstanced(bushes,bush,d.X,d.Y,d.Z,d.Rotation,d.Scale,_=>bushColor,true);break;
                }
            }
            if(bark.VertexCount>0)Add("Árboles · troncos",bark.Build("Troncos"),Material("Troncos",detail,1,false));
            if(leaves.VertexCount>0)Add("Árboles · copas",leaves.Build("Copas"),Material("Copas",detail,1,false,new Vector4(0.05f,2,1.3f,0)));
            if(pines.VertexCount>0)Add("Pinos",pines.Build("Pinos"),Material("Pinos",detail,1,false,new Vector4(0.035f,1,1.1f,0)));
            if(rockBuilder.VertexCount>0)Add("Rocas",rockBuilder.Build("Rocas"),Material("Rocas",detail,1,false));
            if(bushes.VertexCount>0)Add("Arbustos",bushes.Build("Arbustos"),Material("Arbustos",detail,1,false,new Vector4(0.06f,0.1f,1.8f,0)));
        }

        static Shape Tuft()
        {
            var rng=new Rng("tuft");var s=new Shape();
            for(int i=0;i<5;i++){
                double a=(i/5.0)*Math.PI*2+rng.Range(-0.3,0.3),r=rng.Range(0.03,0.12),h=rng.Range(0.32,0.5),lean=rng.Range(0.08,0.16);
                double cx=Math.Cos(a)*r,cz=Math.Sin(a)*r,px=-Math.Sin(a)*0.06,pz=Math.Cos(a)*0.06;
                s.Vertices.Add(new WV(cx-px,0,cz-pz));s.Vertices.Add(new WV(cx+px,0,cz+pz));s.Vertices.Add(new WV(cx+Math.Cos(a)*lean,h,cz+Math.Sin(a)*lean));
                s.Triangles.Add(i*3);s.Triangles.Add(i*3+1);s.Triangles.Add(i*3+2);s.Colors.Add(0);
            }
            return s;
        }

        GameObject BuildGroundCover(WorldData world)
        {
            var root=new GameObject("Cobertura del suelo");root.transform.SetParent(transform,false);built.Add(root);
            var plants=new MeshBuilder();var heads=new MeshBuilder();
            var tuft=Tuft();Color base_=WebSpace.Linear(Palette.GrassTuft),tip=WebSpace.Linear(Palette.GrassTuftTip);
            var stem=new Shape();stem.Vertices.Add(new WV(-0.02,0,0));stem.Vertices.Add(new WV(0.02,0,0));stem.Vertices.Add(new WV(0,0.3,0));stem.Triangles.AddRange(new[]{0,1,2});stem.Colors.Add(0);
            var head=Shape.Octahedron(0.065).Translate(0,0.31,0);
            uint[] flowerColors={Palette.FlowerRed,Palette.FlowerYellow,Palette.FlowerWhite,Palette.FlowerPurple};
            foreach(var g in world.GroundCover.Grass){
                var t=WebTransform.Of(g.X,g.Y-0.02,g.Z,0,g.Rotation,0,g.Scale,g.Scale,g.Scale);
                for(int k=0;k<tuft.Triangles.Count;k+=3){
                    WV la=tuft.Vertices[tuft.Triangles[k]],lb=tuft.Vertices[tuft.Triangles[k+1]],lc=tuft.Vertices[tuft.Triangles[k+2]];
                    plants.Triangle(t.Apply(la),t.Apply(lb),t.Apply(lc),base_,base_,tip,Vector2.zero,Vector2.zero,Vector2.zero,WindData(g.X,g.Z,la.Y,g.Scale),WindData(g.X,g.Z,lb.Y,g.Scale),WindData(g.X,g.Z,lc.Y,g.Scale));
                }
            }
            foreach(var f in world.GroundCover.Flowers){
                EmitInstanced(plants,stem,f.X,f.Y-0.02,f.Z,f.Rotation,f.Scale,_=>base_,true);
                var color=WebSpace.Linear(flowerColors[f.Variant%flowerColors.Length]);
                EmitInstanced(heads,head,f.X,f.Y-0.02,f.Z,f.Rotation,f.Scale,_=>color,true);
            }
            var wind=new Vector4(0.35f,0.02f,2.4f,0);
            if(plants.VertexCount>0)Add("Hierba",plants.Build("Hierba"),Material("Hierba",null,1,false,wind,true)).transform.SetParent(root.transform,false);
            if(heads.VertexCount>0)Add("Flores",heads.Build("Flores"),Material("Flores",null,1,false,wind)).transform.SetParent(root.transform,false);
            return root;
        }

        // ---------------------------------------------------------------- interactuables (estáticos; su estado se dibuja aparte)

        void BuildInteractables(WorldData world)
        {
            var b=new MeshBuilder();
            foreach(var s in world.Interactables){
                var parts=new List<(Shape shape,double y,uint color)>();
                switch(s.Kind){
                    case "chest":parts.Add((Shape.Box(1.24,0.52,0.8),0.26,Palette.Wood));parts.Add((Shape.Box(1.3,0.26,0.86),0.65,Palette.WoodDark));parts.Add((Shape.Box(0.2,0.2,0.05),0.48,Palette.Coin));break;
                    case "shrine":parts.Add((Shape.Box(1.6,0.08,1.6),0.74,Palette.Wood));parts.Add((Shape.Cylinder(0.85,0.85,0.7,10),0.35,Palette.Plaid));break;
                    case "totem":parts.Add((Shape.Cylinder(0.38,0.42,2.7,6),1.35,Palette.StoneDark));parts.Add((Shape.Box(0.6,0.5,0.6),2.45,Palette.Marble));break;
                    default:parts.Add((Shape.Box(1.9,2.8,1),1.4,Palette.WoodDark));parts.Add((Shape.Box(0.8,2.4,0.06),1.3,Palette.Wood));break;
                }
                foreach(var p in parts){var color=WebSpace.Linear(p.color);Emit(b,p.shape,WebTransform.Of(s.X,s.Y+p.y,s.Z,0,s.Rotation,0),_=>color,1.2,default,true);}
            }
            if(b.VertexCount>0)Add("Interactuables",b.Build("Interactuables"),Material("Interactuables",detail,1,false));
        }

        void BuildSky()
        {
            if(!skyShader)return;
            var shape=Shape.Icosahedron(1);var b=new MeshBuilder();
            Emit(b,shape,WebTransform.Of(0,0,0,0,0,0),_=>Color.white,1,default,false);
            var m=new Material(skyShader){name="Cielo"};owned.Add(m);
            m.SetColor("_Zenith",WebSpace.Linear(Palette.SkyZenith));m.SetColor("_Horizon",WebSpace.Linear(Palette.SkyHorizon));m.SetColor("_SunColor",WebSpace.Linear(Palette.Sun));
            m.SetVector("_SunDir",SunDirection);
            sky=Add("Cielo",b.Build("Cielo"),m);sky.transform.localScale=Vector3.one*380;
        }

        // El cielo sigue a la cámara.
        public void FollowCamera(Camera camera){if(sky&&camera)sky.transform.position=camera.transform.position;}

        void OnDestroy(){Clear();if(detail)Destroy(detail);if(stone)Destroy(stone);}
    }
}
