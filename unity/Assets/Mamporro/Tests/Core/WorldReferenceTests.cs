using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core;

namespace Mamporro.Tests
{
    // Compara el mundo portado con unity/Docs/Reference/u3-world.json (web aprobada) y con
    // baseline.worlds de U0. Reglas de U3: lo discreto (número, tipo y orden) exacto; alturas
    // y posiciones con 1e-6 m. JsonUtility no lee todos los double bit a bit (desvío de un
    // bit en ~9 % de los valores, medido el 29/09/2026), por eso las alturas van en base64.
    public sealed class WorldReferenceTests
    {
        const double Tolerance=1e-6;

        [Serializable] public class Reference { public string source;public Simplex simplex;public WorldRef[] worlds;public PhysicsCase[] physics; }
        [Serializable] public class Simplex { public string seed;public double[] x,z,value; }
        [Serializable] public class WorldRef {
            public string seed,rawHeights,heights;public bool full;public Sample[] samples;public Push[] pushes;public SiteRef[] sites;public PartRef[] parts;
            public int propColliders,grassCount,flowerCount;public SpotRef[] interactables;public DecoRef[] decorations;public CoverRef[] grass,flowers;public ColliderRef[] colliders; }
        [Serializable] public class Sample { public double x,z,h,nx,ny,nz,squircle,ground,groundHigh,gnx,gny,gnz;public bool inside; }
        [Serializable] public class Push { public double x,z,feet,px,pz,bx,bz,bvx,bvz,farX,farZ;public bool hit; }
        [Serializable] public class SiteRef { public string kind;public double x,z,radius,rotation,floorY; }
        [Serializable] public class PartRef { public string shape,material;public uint color;public double[] size,position,rotation;public int segments; }
        [Serializable] public class SpotRef { public string kind;public double x,y,z,rotation; }
        [Serializable] public class DecoRef { public string kind;public double x,y,z,scale,rotation,variant; }
        [Serializable] public class CoverRef { public double x,y,z,scale,rotation;public int variant; }
        [Serializable] public class ColliderRef { public string shape;public double x,z,bottom,top,radius,halfX,halfZ,cos,sin;public bool standable; }
        [Serializable] public class PhysicsCase { public string name,seed;public double startX,startZ,moveX,moveZ;public int jumpEvery,slide,ticks;public PhysicsSample[] samples; }
        [Serializable] public class PhysicsSample { public int tick;public double x,y,z,vx,vy,vz;public bool grounded,sliding,onSteep; }

        [Serializable] public class Baseline { public BaseWorld[] worlds; }
        [Serializable] public class BaseWorld { public string seed;public SiteRef[] sites;public SpotRef[] interactables;public BaseHeight[] heights; }
        [Serializable] public class BaseHeight { public double x,z,height; }

        static Reference cached;
        static Reference Ref=>cached??=JsonUtility.FromJson<Reference>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u3-world.json")));
        static readonly Dictionary<string,WorldData> worlds=new Dictionary<string,WorldData>();
        static WorldData World(string seed){if(!worlds.TryGetValue(seed,out var w))worlds[seed]=w=WorldData.Generate(seed);return w;}
        static WorldRef RefWorld(string seed)=>Array.Find(Ref.worlds,w=>w.seed==seed);
        static float[] Floats(string base64){var bytes=Convert.FromBase64String(base64);var r=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,r,0,bytes.Length);return r;}
        static void Near(double actual,double expected,string what)=>Assert.That(actual,Is.EqualTo(expected).Within(Tolerance),what);
        static IEnumerable<string> Seeds(){foreach(var w in Ref.worlds)yield return w.seed;}
        static IEnumerable<string> FullSeeds(){foreach(var w in Ref.worlds)if(w.full)yield return w.seed;}

        [Test]public void ReferenceComesFromApprovedWeb()=>Assert.That(Ref.source,Is.EqualTo("0505b1690656d15188860157612455639820fe1f"));

        [Test]public void SimplexNoiseMatchesLibrary()
        {
            var rng=new Rng(Ref.simplex.seed);var noise=new SimplexNoise2D(rng.Next);
            for(int i=0;i<Ref.simplex.value.Length;i++)Near(noise.Noise(Ref.simplex.x[i],Ref.simplex.z[i]),Ref.simplex.value[i],"muestra "+i);
        }

        static void CompareHeights(float[] actual,float[] expected,string what)
        {
            Assert.That(actual.Length,Is.EqualTo(expected.Length),what);
            int exact=0;double worst=0;
            for(int i=0;i<expected.Length;i++){double d=Math.Abs(actual[i]-expected[i]);if(d==0)exact++;if(d>worst)worst=d;}
            TestContext.WriteLine($"{what}: {exact}/{expected.Length} alturas idénticas bit a bit; diferencia máxima {worst:E3} m");
            Assert.That(worst,Is.LessThanOrEqualTo(Tolerance),what);
        }

        [Test]public void RawTerrainMatchesWeb([ValueSource(nameof(FullSeeds))]string seed)
        {
            var hf=Heightfield.Generate(new Rng(seed).Derive("terrain"));
            CompareHeights(hf.Heights,Floats(RefWorld(seed).rawHeights),seed+" terreno sin aplanar");
        }

        [Test]public void FinalHeightsAndSitesMatchWeb([ValueSource(nameof(Seeds))]string seed)
        {
            var w=World(seed);var e=RefWorld(seed);
            Assert.That(w.Sites.Count,Is.EqualTo(e.sites.Length),"sitios");
            for(int i=0;i<e.sites.Length;i++){var a=w.Sites[i];var x=e.sites[i];string what=seed+" sitio "+i;
                Assert.That(a.Kind,Is.EqualTo(x.kind),what);Near(a.X,x.x,what+" x");Near(a.Z,x.z,what+" z");Near(a.Radius,x.radius,what);Near(a.Rotation,x.rotation,what+" giro");Near(a.FloorY,x.floorY,what+" suelo");}
            CompareHeights(w.Heightfield.Heights,Floats(e.heights),seed+" alturas finales");
        }

        [Test]public void QueriesMatchWeb([ValueSource(nameof(Seeds))]string seed)
        {
            var w=World(seed);var e=RefWorld(seed);var c=w.Collision;
            for(int i=0;i<e.samples.Length;i++){var s=e.samples[i];string what=seed+" muestra "+i;
                Near(w.Heightfield.HeightAt(s.x,s.z),s.h,what+" altura");var n=w.Heightfield.NormalAt(s.x,s.z);Near(n.X,s.nx,what);Near(n.Y,s.ny,what);Near(n.Z,s.nz,what);
                Near(WorldMath.Squircle(s.x,s.z),s.squircle,what+" squircle");Assert.That(c.IsInside(s.x,s.z,2),Is.EqualTo(s.inside),what+" dentro");
                Near(c.GroundHeight(s.x,s.z,s.h+0.45),s.ground,what+" suelo");Near(c.GroundHeight(s.x,s.z,double.MaxValue),s.groundHigh,what+" suelo alto");
                var g=c.GroundNormal(s.x,s.z,s.h+0.45);Near(g.X,s.gnx,what);Near(g.Y,s.gny,what);Near(g.Z,s.gnz,what);}
            for(int i=0;i<e.pushes.Length;i++){var p=e.pushes[i];string what=seed+" empujón "+i;
                double x=p.x,z=p.z;Assert.That(c.PushOutCircle(ref x,ref z,0.5,p.feet,1.6),Is.EqualTo(p.hit),what);Near(x,p.px,what+" x");Near(z,p.pz,what+" z");
                var body=new PlayerBody();body.PlaceAt(p.x,p.feet,p.z);body.Vx=p.bvx;body.Vz=p.bvz;
                // Las velocidades de entrada no se guardaron: se comprueba la posición resultante.
                c.ResolveObstacles(body,PlayerTuning.Default.Radius,PlayerTuning.Default.StepHeight);Near(body.X,p.bx,what+" cuerpo x");Near(body.Z,p.bz,what+" cuerpo z");
                double fx=p.x*3,fz=p.z*3;c.ClampInside(ref fx,ref fz);Near(fx,p.farX,what+" límite x");Near(fz,p.farZ,what+" límite z");}
        }

        [Test]public void PropsMatchWeb([ValueSource(nameof(Seeds))]string seed)
        {
            var w=World(seed);var e=RefWorld(seed);
            Assert.That(w.Props.Parts.Count,Is.EqualTo(e.parts.Length),seed+" piezas");Assert.That(w.Props.Colliders.Count,Is.EqualTo(e.propColliders),seed+" colisionadores de props");
            for(int i=0;i<e.parts.Length;i++){var a=w.Props.Parts[i];var x=e.parts[i];string what=seed+" pieza "+i;
                Assert.That(a.Shape,Is.EqualTo(x.shape),what);Assert.That(a.Material,Is.EqualTo(x.material),what);Assert.That(a.Color,Is.EqualTo(x.color),what+" color");Assert.That(a.Segments,Is.EqualTo(x.segments),what);
                for(int k=0;k<3;k++){Near(a.Size[k],x.size[k],what+" tamaño");Near(a.Position[k],x.position[k],what+" posición");Near(a.Rotation[k],x.rotation[k],what+" giro");}}
        }

        [Test]public void CollidersAndInteractablesMatchWeb([ValueSource(nameof(Seeds))]string seed)
        {
            var w=World(seed);var e=RefWorld(seed);
            Assert.That(w.Interactables.Count,Is.EqualTo(e.interactables.Length),seed+" interactuables");
            for(int i=0;i<e.interactables.Length;i++){var a=w.Interactables[i];var x=e.interactables[i];string what=seed+" interactuable "+i;
                Assert.That(a.Kind,Is.EqualTo(x.kind),what);Near(a.X,x.x,what);Near(a.Y,x.y,what);Near(a.Z,x.z,what);Near(a.Rotation,x.rotation,what);}
            Assert.That(w.Colliders.Count,Is.EqualTo(e.colliders.Length),seed+" colisionadores");
            for(int i=0;i<e.colliders.Length;i++){var a=w.Colliders[i];var x=e.colliders[i];string what=seed+" colisionador "+i;
                Assert.That(a.Box?"box":"circle",Is.EqualTo(x.shape),what);Assert.That(a.Standable,Is.EqualTo(x.standable),what);
                Near(a.X,x.x,what);Near(a.Z,x.z,what);Near(a.Bottom,x.bottom,what);Near(a.Top,x.top,what);Near(a.Radius,x.radius,what);
                Near(a.HalfX,x.halfX,what);Near(a.HalfZ,x.halfZ,what);Near(a.Cos,x.cos,what);Near(a.Sin,x.sin,what);}
        }

        [Test]public void VegetationMatchesWeb([ValueSource(nameof(Seeds))]string seed)
        {
            var w=World(seed);var e=RefWorld(seed);
            Assert.That(w.Decorations.Count,Is.EqualTo(e.decorations.Length),seed+" decoración");
            for(int i=0;i<e.decorations.Length;i++){var a=w.Decorations[i];var x=e.decorations[i];string what=seed+" decoración "+i;
                Assert.That(a.Kind,Is.EqualTo(x.kind),what);Near(a.X,x.x,what);Near(a.Y,x.y,what);Near(a.Z,x.z,what);Near(a.Scale,x.scale,what);Near(a.Rotation,x.rotation,what);Near(a.Variant,x.variant,what);}
            Assert.That(w.GroundCover.Grass.Count,Is.EqualTo(e.grassCount),seed+" hierba");Assert.That(w.GroundCover.Flowers.Count,Is.EqualTo(e.flowerCount),seed+" flores");
            void Cover(List<CoverInstance> actual,CoverRef[] expected,string kind){for(int i=0;i<expected.Length;i++){var a=actual[i];var x=expected[i];string what=seed+" "+kind+" "+i;
                Near(a.X,x.x,what);Near(a.Y,x.y,what);Near(a.Z,x.z,what);Near(a.Scale,x.scale,what);Near(a.Rotation,x.rotation,what);Assert.That(a.Variant,Is.EqualTo(x.variant),what);}}
            Cover(w.GroundCover.Grass,e.grass,"hierba");Cover(w.GroundCover.Flowers,e.flowers,"flor");
        }

        [Test]public void BaselineWorldsMatchU0()
        {
            var b=JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/baseline.json")));
            foreach(var e in b.worlds){
                var w=World(e.seed);
                Assert.That(w.Sites.Count,Is.EqualTo(e.sites.Length),e.seed);Assert.That(w.Interactables.Count,Is.EqualTo(e.interactables.Length),e.seed);
                for(int i=0;i<e.sites.Length;i++){Assert.That(w.Sites[i].Kind,Is.EqualTo(e.sites[i].kind));Near(w.Sites[i].X,e.sites[i].x,e.seed);Near(w.Sites[i].FloorY,e.sites[i].floorY,e.seed);}
                for(int i=0;i<e.interactables.Length;i++){Assert.That(w.Interactables[i].Kind,Is.EqualTo(e.interactables[i].kind));Near(w.Interactables[i].X,e.interactables[i].x,e.seed);Near(w.Interactables[i].Z,e.interactables[i].z,e.seed);}
                foreach(var h in e.heights)Near(w.Heightfield.HeightAt(h.x,h.z),h.height,e.seed+" altura");
            }
        }

        [Test]public void PlayerTrajectoriesMatchWeb([ValueSource(nameof(PhysicsNames))]string name)
        {
            var c=Array.Find(Ref.physics,v=>v.name==name);var w=World(c.seed);var body=new PlayerBody();
            body.PlaceAt(c.startX,w.Collision.GroundHeight(c.startX,c.startZ,double.MaxValue),c.startZ);
            int next=0;
            for(int tick=1;tick<=c.ticks;tick++){
                bool jump=c.jumpEvery>0&&tick%c.jumpEvery==0;
                var intent=new PlayerIntent{MoveX=c.moveX,MoveZ=c.moveZ,JumpPressed=jump,JumpHeld=c.jumpEvery>0&&tick%c.jumpEvery<12,SlidePressed=c.slide==1&&tick==30,SlideHeld=c.slide==1&&tick>=30};
                PlayerPhysics.StepInCrowd(body,intent,w.Collision,PlayerTuning.Default,Tuning.PlayerBaseMoveSpeed,0,1.0/60);
                if(next<c.samples.Length&&c.samples[next].tick==tick){var s=c.samples[next++];string what=name+" tick "+tick;
                    Assert.That(body.Grounded,Is.EqualTo(s.grounded),what+" en el suelo");Assert.That(body.Sliding,Is.EqualTo(s.sliding),what+" deslizando");Assert.That(body.OnSteep,Is.EqualTo(s.onSteep),what+" pendiente");
                    Near(body.X,s.x,what+" x");Near(body.Y,s.y,what+" y");Near(body.Z,s.z,what+" z");Near(body.Vx,s.vx,what+" vx");Near(body.Vy,s.vy,what+" vy");Near(body.Vz,s.vz,what+" vz");}
            }
        }
        static IEnumerable<string> PhysicsNames(){foreach(var c in Ref.physics)yield return c.name;}
    }
}
