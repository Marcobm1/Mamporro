using System.Collections.Generic;

namespace Mamporro.Core
{
    // P0-A: circuito fijo QA, no sustituye el generador procedural U3.
    // Coordenadas web; la inversión de Z sigue perteneciendo a presentación.
    public sealed class VerticalCircuit
    {
        public readonly Heightfield Terrain;
        public readonly VerticalQueries Queries;
        public readonly WorldCollision Collision;
        public readonly Vec3 Start=new Vec3(4,0,-10);
        public VerticalCircuit()
        {
            const int cells=80;const double size=80;
            var heights=new float[(cells+1)*(cells+1)];
            // Rampa oeste de 12 m, meseta de 4 m; no es el tamaño del mundo futuro.
            for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++){
                double px=x-size/2,pz=z-size/2;
                if(pz>=-4&&pz<=4&&px>=-24&&px<=-4)
                    heights[z*(cells+1)+x]=(float)(px<-12?(px+24)/3:4);
                // Foso de ensayo, con ruta llana alrededor de sus extremos.
                if(px>=16&&px<=22&&pz>=8&&pz<=14)heights[z*(cells+1)+x]=-6;
            }
            Terrain=new Heightfield(size,cells,heights);
            Queries=new VerticalQueries(
                Box(1,VerticalSurfaceKind.Structure,8,0,-4,14,6,4), // pared y tejado
                Box(2,VerticalSurfaceKind.Structure,6,7,-1,15,8,1), // techo que bloquea la salida central
                Box(3,VerticalSurfaceKind.Structure,8,0,10,12,4,16),
                Box(4,VerticalSurfaceKind.Structure,4,0,14,8,4,16), // esquina interior/exterior
                Box(5,VerticalSurfaceKind.Structure,-12,4,-4,-4,4.25,4), // soporte sobre meseta
                Box(6,VerticalSurfaceKind.Cliff,-2,0,16,4,8,20),
                Box(7,VerticalSurfaceKind.Foliage,-6,0,-12,-5,3,-11),
                Box(8,VerticalSurfaceKind.Interactable,-2,0,-12,-1,2,-11),
                Box(9,VerticalSurfaceKind.Structure,20,0,-3,20.5,5,3), // borde estrecho
                Box(10,VerticalSurfaceKind.Boundary,30,-10,-30,31,12,30));
            var colliders=new List<Collider>();
            for(int i=0;i<Queries.Count;i++){
                var s=Queries.Solid(i);
                colliders.Add(Collider.Oriented((s.Min.X+s.Max.X)/2,(s.Min.Z+s.Max.Z)/2,
                    (s.Max.X-s.Min.X)/2,(s.Max.Z-s.Min.Z)/2,0,s.Min.Y,s.Max.Y,true));
            }
            Collision=new WorldCollision(Terrain,new ColliderGrid(colliders,size/2),29);
        }
        public WorldData Data()
        {
            var props=new PropSet();
            for(int i=0;i<Queries.Count;i++){
                var b=Queries.Solid(i);uint color=b.Climbable?0x957954u:0x705c80u;
                props.Parts.Add(new Part{Shape="box",Material="stone",Color=color,
                    Position=new[]{(b.Min.X+b.Max.X)/2,(b.Min.Y+b.Max.Y)/2,(b.Min.Z+b.Max.Z)/2},
                    Size=new[]{b.Max.X-b.Min.X,b.Max.Y-b.Min.Y,b.Max.Z-b.Min.Z},Rotation=new double[3]});
            }
            return new WorldData{Heightfield=Terrain,Collision=Collision,Colliders=Collision.Grid.Colliders,Props=props,
                Sites=new List<Site>(),Interactables=new List<InteractableSpot>(),Decorations=new List<Decoration>(),GroundCover=new GroundCover()};
        }
        static VerticalSolid Box(int id,VerticalSurfaceKind kind,double x,double y,double z,double xx,double yy,double zz)=>
            new VerticalSolid(id,kind,new Vec3(x,y,z),new Vec3(xx,yy,zz));
    }
}
