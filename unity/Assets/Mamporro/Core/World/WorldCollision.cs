using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // Cofres, santuarios (mesas camilla), tótems y el portal (armario) del mapa.
    public sealed class InteractableSpot
    {
        public string Kind;
        public double X,Y,Z,Rotation;
    }

    public static class InteractableSpots
    {
        const double MaxSlope=16,MinGap=9,SiteMargin=2;

        // placeInteractables de src/world/interactables.ts.
        public static List<InteractableSpot> Place(Heightfield hf,List<Site> sites,Rng rng,double limit,ColliderGrid obstacles,InteractablePlacement[] placements)
        {
            var spots=new List<InteractableSpot>();var clearOf=new Dictionary<string,double>();
            double cosMax=Math.Cos(MaxSlope*WorldMath.Deg2Rad),maxR=limit-10;var nearby=new List<int>();
            foreach(var placement in placements){
                clearOf[placement.kind]=placement.spacing;int placed=0;
                for(int attempt=0;attempt<placement.count*120&&placed<placement.count;attempt++){
                    double u=rng.Next(),v=rng.Next(),rotation=rng.Next()*Math.PI*2,x=(u*2-1)*maxR,z=(v*2-1)*maxR;
                    if(WorldMath.Squircle(x,z)>maxR)continue;
                    if(JsMath.Hypot(x,z)<placement.minSpawnDistance)continue;
                    if(placement.kind!="chest"&&Sites.IsInAny(sites,x,z,SiteMargin))continue;
                    bool tooClose=false;
                    foreach(var s in spots){
                        double gap=s.Kind==placement.kind?placement.spacing:Math.Max(MinGap,Math.Min(placement.spacing,clearOf.TryGetValue(s.Kind,out double c)?c:0));
                        if(JsMath.Hypot(s.X-x,s.Z-z)<gap){tooClose=true;break;}
                    }
                    if(tooClose)continue;
                    if(hf.NormalAt(x,z).Y<cosMax)continue;
                    bool blocked=false;
                    foreach(int index in obstacles.Query(x,z,placement.clearRadius+1,nearby))
                        if(obstacles.Colliders[index].PushOut(x,z,placement.clearRadius,out _,out _,out _,out _)){blocked=true;break;}
                    if(blocked)continue;
                    spots.Add(new InteractableSpot{Kind=placement.kind,X=x,Y=hf.HeightAt(x,z),Z=z,Rotation=rotation});
                    placed++;
                }
            }
            return spots;
        }

        public static Collider ColliderOf(InteractableSpot s)
        {
            switch(s.Kind){
                case "chest":return Collider.Oriented(s.X,s.Z,0.62,0.4,s.Rotation,s.Y,s.Y+0.78,true);
                case "shrine":return Collider.Circle(s.X,s.Z,0.85,s.Y,s.Y+0.8,true);
                case "totem":return Collider.Circle(s.X,s.Z,0.42,s.Y,s.Y+2.7,false);
                default:return Collider.Oriented(s.X,s.Z,0.95,0.5,s.Rotation,s.Y,s.Y+2.8,false);
            }
        }

        // Radio libre de vegetación alrededor de cada interactuable.
        public static double ClearRadius(string kind)
        {
            foreach(var p in Catalog.InteractablePlacements)if(p.kind==kind)return p.clearRadius+1.5;
            return 3.5;
        }
    }

    // Consultas de colisión del mundo (src/world/WorldCollision.ts): terreno, superficies
    // a las que se puede subir, obstáculos y límite del mapa. También sirve de mundo de combate.
    public sealed class WorldCollision : ICombatWorld,IPhysicsWorld
    {
        const double BodyHeight=1.6;
        public readonly Heightfield Heightfield;
        public readonly ColliderGrid Grid;
        public readonly double Limit;
        readonly List<int> nearby=new List<int>();

        public WorldCollision(Heightfield heightfield,ColliderGrid grid,double limit){Heightfield=heightfield;Grid=grid;Limit=limit;}

        double StandableTop(double x,double z,double maxY)
        {
            double best=double.NegativeInfinity;var colliders=Grid.Colliders;
            foreach(int index in Grid.Query(x,z,0,nearby)){
                var c=colliders[index];
                if(!c.Standable||c.Top>maxY||c.Top<=best)continue;
                if(c.IsOnTop(x,z))best=c.Top;
            }
            return best;
        }

        public double GroundHeight(double x,double z,double maxY)
        {
            double terrain=Heightfield.HeightAt(x,z),top=StandableTop(x,z,maxY);
            return top>terrain?top:terrain;
        }

        public Vec3 GroundNormal(double x,double z,double maxY)
        {
            double terrain=Heightfield.HeightAt(x,z);
            if(StandableTop(x,z,maxY)>terrain)return new Vec3(0,1,0);
            return Heightfield.NormalAt(x,z);
        }

        // Saca el cuerpo de los obstáculos y anula la velocidad contra ellos.
        public void ResolveObstacles(PlayerBody body,double radius,double stepHeight)
        {
            var colliders=Grid.Colliders;
            foreach(int index in Grid.Query(body.X,body.Z,radius+1,nearby)){
                var c=colliders[index];
                if(body.Y+stepHeight>=c.Top)continue;
                if(body.Y+BodyHeight<c.Bottom)continue;
                if(!c.PushOut(body.X,body.Z,radius,out double dx,out double dz,out double nx,out double nz))continue;
                body.X+=dx;body.Z+=dz;
                double vn=body.Vx*nx+body.Vz*nz;
                if(vn<0){body.Vx-=vn*nx;body.Vz-=vn*nz;}
            }
        }

        // Versión ligera para enemigos: los obstáculos más bajos que step se pueden subir.
        public bool PushOutCircle(ref double x,ref double z,double radius,double feetY,double step)
        {
            var colliders=Grid.Colliders;bool hit=false;
            foreach(int index in Grid.Query(x,z,radius+1,nearby)){
                var c=colliders[index];
                if(feetY+step>=c.Top||feetY+BodyHeight<c.Bottom)continue;
                if(!c.PushOut(x,z,radius,out double dx,out double dz,out _,out _))continue;
                x+=dx;z+=dz;hit=true;
            }
            return hit;
        }

        public bool IsInside(double x,double z,double margin)=>WorldMath.Squircle(x,z)<=Limit-margin;

        public void Constrain(PlayerBody body)
        {
            double r=WorldMath.Squircle(body.X,body.Z);
            if(r<=Limit)return;
            double k=Limit/r;body.X*=k;body.Z*=k;
            double p=WorldMath.SquirclePower-1;
            double gx=Math.Sign(body.X)*Math.Pow(Math.Abs(body.X),p),gz=Math.Sign(body.Z)*Math.Pow(Math.Abs(body.Z),p);
            double gl=JsMath.Hypot(gx,gz);
            if(gl<1e-9)return;
            double nx=gx/gl,nz=gz/gl,vn=body.Vx*nx+body.Vz*nz;
            if(vn>0){body.Vx-=vn*nx;body.Vz-=vn*nz;}
        }

        public void ClampInside(ref double x,ref double z)
        {
            double r=WorldMath.Squircle(x,z);
            if(r<=Limit)return;
            double k=Limit/r;x*=k;z*=k;
        }

        // ICombatWorld: maxY infinito = solo el terreno (la web usa heightfield.heightAt para
        // apariciones, proyectiles y disparos); con maxY finito, groundHeight.
        public double Height(double x,double z,double maxY=double.PositiveInfinity)=>double.IsPositiveInfinity(maxY)?Heightfield.HeightAt(x,z):GroundHeight(x,z,maxY);
        public bool PushOut(ref double x,ref double z,double radius,double y,double step)=>PushOutCircle(ref x,ref z,radius,y,step);
        public void Clamp(ref double x,ref double z)=>ClampInside(ref x,ref z);
    }

    // Todos los datos del mundo de una semilla (generateWorldData de src/world/World.ts).
    public sealed class WorldData
    {
        public Heightfield Heightfield;
        public List<Site> Sites;
        public PropSet Props;
        public List<InteractableSpot> Interactables;
        public List<Decoration> Decorations;
        public GroundCover GroundCover;
        public List<Collider> Colliders;
        public WorldCollision Collision;

        public static WorldData Generate(string seed)
        {
            var rng=new Rng(seed);double limit=Tuning.WorldPlayableRadius;
            var hf=Heightfield.Generate(rng.Derive("terrain"));
            var sites=Mamporro.Core.Sites.Pick(hf,rng.Derive("sites"),Catalog.SiteRequests,limit,Tuning.WorldSiteSpawnClear,8,4.5);
            Mamporro.Core.Sites.Flatten(hf,sites);
            var props=Mamporro.Core.Props.Generate(hf,sites,rng.Derive("props"),limit,16);
            var interactables=InteractableSpots.Place(hf,sites,rng.Derive("interactables"),limit,new ColliderGrid(props.Colliders,hf.Half),Catalog.InteractablePlacements);
            Func<double,double,bool> nearInteractable=(x,z)=>{foreach(var s in interactables)if(JsMath.Hypot(s.X-x,s.Z-z)<InteractableSpots.ClearRadius(s.Kind))return true;return false;};
            var decorations=Vegetation.PlaceDecorations(hf,rng.Derive("decorations"),Tuning.WorldClearSpawnRadius,limit,(x,z)=>Mamporro.Core.Sites.IsInAny(sites,x,z,1.5)||nearInteractable(x,z));
            var cover=Vegetation.PlaceGroundCover(hf,rng.Derive("ground-cover"),limit,(x,z)=>Mamporro.Core.Sites.IsInAny(sites,x,z,-1));
            var colliders=new List<Collider>(Vegetation.DecorationColliders(decorations));
            colliders.AddRange(props.Colliders);
            foreach(var s in interactables)colliders.Add(InteractableSpots.ColliderOf(s));
            var collision=new WorldCollision(hf,new ColliderGrid(colliders,hf.Half),limit);
            return new WorldData{Heightfield=hf,Sites=sites,Props=props,Interactables=interactables,Decorations=decorations,GroundCover=cover,Colliders=colliders,Collision=collision};
        }
    }
}
