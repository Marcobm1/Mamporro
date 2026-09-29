using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Árboles, pinos, rocas y arbustos (src/world/decorations.ts).
    public sealed class Decoration
    {
        public string Kind;
        public double X,Y,Z,Scale,Rotation,Variant;
    }

    // Matas de hierba y flores (src/world/groundCover.ts).
    public sealed class CoverInstance
    {
        public double X,Y,Z,Scale,Rotation;
        public int Variant;
    }

    public sealed class GroundCover
    {
        public readonly List<CoverInstance> Grass=new List<CoverInstance>(),Flowers=new List<CoverInstance>();
    }

    public static class Vegetation
    {
        public const double RockHeightScale=0.85,RockSink=0.2,RockRadius=0.85;
        public const int FlowerVariants=4;

        public static List<Decoration> PlaceDecorations(Heightfield hf,Rng rng,double spawnRadius,double limit,Func<double,double,bool> isReserved,double cell=5.5)
        {
            var forest=new SimplexNoise2D(rng.Next);
            int count=(int)Math.Ceiling(limit*2/cell);double start=-limit;
            double cosTree=Math.Cos(28*WorldMath.Deg2Rad),cosBush=Math.Cos(34*WorldMath.Deg2Rad),cosRock=Math.Cos(50*WorldMath.Deg2Rad),cos30=Math.Cos(30*WorldMath.Deg2Rad);
            var result=new List<Decoration>();
            for(int gz=0;gz<count;gz++)for(int gx=0;gx<count;gx++){
                // Siempre los mismos números por celda.
                double jx=rng.Next(),jz=rng.Next(),roll=rng.Next(),s=rng.Next(),rotation=rng.Next()*Math.PI*2,variant=rng.Next();
                double x=start+(gx+0.15+jx*0.7)*cell,z=start+(gz+0.15+jz*0.7)*cell;
                if(WorldMath.Squircle(x,z)>limit-3)continue;
                if(JsMath.Hypot(x,z)<spawnRadius)continue;
                if(isReserved!=null&&isReserved(x,z))continue;
                double ground=hf.HeightAt(x,z);var normal=hf.NormalAt(x,z);
                double density=forest.Noise(x*0.018,z*0.018)*0.5+0.5;
                double treeChance=density*density*0.7,rockChance=normal.Y<cos30?0.08:0.035,bushChance=0.1;
                if(normal.Y>cosTree&&roll<treeChance){
                    bool pine=ground>17||variant<0.22;
                    result.Add(new Decoration{Kind=pine?"pine":"tree",X=x,Y=ground-0.15,Z=z,Scale=pine?0.9+s*0.5:0.8+s*0.5,Rotation=rotation,Variant=variant});
                }else if(normal.Y>cosRock&&roll>1-rockChance){
                    double scale=s>0.95?3.4:0.8+s*s*1.8;
                    result.Add(new Decoration{Kind="rock",X=x,Y=ground-RockSink*scale,Z=z,Scale=scale,Rotation=rotation,Variant=variant});
                }else if(normal.Y>cosBush&&roll>1-rockChance-bushChance){
                    result.Add(new Decoration{Kind="bush",X=x,Y=ground-0.1,Z=z,Scale=0.7+s*0.5,Rotation=rotation,Variant=variant});
                }
            }
            return result;
        }

        public static List<Collider> DecorationColliders(List<Decoration> decorations)
        {
            var colliders=new List<Collider>();
            foreach(var d in decorations){
                if(d.Kind=="tree"||d.Kind=="pine")colliders.Add(Collider.Circle(d.X,d.Z,0.3*d.Scale+0.05,d.Y-1,d.Y+12,false));
                else if(d.Kind=="rock")colliders.Add(Collider.Circle(d.X,d.Z,RockRadius*d.Scale,d.Y-d.Scale,d.Y+RockHeightScale*d.Scale*0.92,true));
            }
            return colliders;
        }

        public static GroundCover PlaceGroundCover(Heightfield hf,Rng rng,double limit,Func<double,double,bool> isReserved,double cell=2.2)
        {
            var meadow=new SimplexNoise2D(rng.Next);
            int count=(int)Math.Ceiling(limit*2/cell);double cosMax=Math.Cos(32*WorldMath.Deg2Rad);
            var cover=new GroundCover();
            for(int gz=0;gz<count;gz++)for(int gx=0;gx<count;gx++){
                double jx=rng.Next(),jz=rng.Next(),roll=rng.Next(),s=rng.Next(),rotation=rng.Next()*Math.PI*2,pick=rng.Next();
                double x=-limit+(gx+jx)*cell,z=-limit+(gz+jz)*cell;
                if(WorldMath.Squircle(x,z)>limit-2)continue;
                if(isReserved!=null&&isReserved(x,z))continue;
                if(hf.NormalAt(x,z).Y<cosMax)continue;
                double y=hf.HeightAt(x,z);
                if(y>34)continue;
                double bloom=meadow.Noise(x*0.025,z*0.025)*0.5+0.5;
                if(roll<0.1+bloom*bloom*0.35)cover.Flowers.Add(new CoverInstance{X=x,Y=y,Z=z,Scale=0.8+s*0.5,Rotation=rotation,Variant=(int)Math.Floor(pick*FlowerVariants)});
                else if(roll<0.62)cover.Grass.Add(new CoverInstance{X=x,Y=y,Z=z,Scale=0.7+s*0.7,Rotation=rotation,Variant=0});
            }
            return cover;
        }
    }
}
