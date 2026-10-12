using System.Collections.Generic;

namespace Mamporro.Core
{
    // A: escalada del jugador (P0-A, aprobado). B: persecución vertical de la horda (P0-B).
    // C: mapa procedural validado (P0-C), elegido por semilla.
    public enum VerticalLayout { Escalada, Persecucion, Procedural }
    // P0: circuitos fijos QA, no sustituyen el generador procedural U3.
    // Coordenadas web; la inversión de Z sigue perteneciendo a presentación.
    public sealed class VerticalCircuit
    {
        public readonly VerticalLayout Layout;
        public readonly Heightfield Terrain;
        public readonly VerticalQueries Queries;
        public readonly WorldCollision Collision;
        public readonly Vec3 Start;
        // Solo C: el mapa validado (trazado, estructuras, grafos S/M/L listos para reutilizar).
        public readonly ProceduralMap Map;
        public string Seed=>Map?.Seed;
        public string Name=>Layout==VerticalLayout.Escalada?"A · escalada":Layout==VerticalLayout.Persecucion?"B · persecución vertical":"C · procedural";
        public VerticalCircuit():this(VerticalLayout.Escalada){}
        public VerticalCircuit(VerticalLayout layout)
        {
            Layout=layout;
            if(layout==VerticalLayout.Procedural)throw new System.ArgumentException("El circuito C se crea con su mapa: new VerticalCircuit(new ProceduralMap(semilla))");
            if(layout==VerticalLayout.Persecucion)BuildPursuit(out Terrain,out Queries,out Start);
            else BuildClimbing(out Terrain,out Queries,out Start);
            Collision=BuildCollision(Terrain,Queries,29);
        }
        public VerticalCircuit(ProceduralMap map)
        {
            Layout=VerticalLayout.Procedural;Map=map;Terrain=map.Layout.Terrain;Queries=map.Queries;
            Start=new Vec3(0,Terrain.HeightAt(0,0),0);
            Collision=BuildCollision(Terrain,Queries,map.Layout.Settings.Limit);
        }
        static WorldCollision BuildCollision(Heightfield terrain,VerticalQueries queries,double limit)
        {
            var colliders=new List<Collider>();
            for(int i=0;i<queries.Count;i++){
                var s=queries.Solid(i);
                colliders.Add(Collider.Oriented((s.Min.X+s.Max.X)/2,(s.Min.Z+s.Max.Z)/2,
                    (s.Max.X-s.Min.X)/2,(s.Max.Z-s.Min.Z)/2,0,s.Min.Y,s.Max.Y,true));
            }
            return new WorldCollision(terrain,new ColliderGrid(colliders,terrain.Size/2),limit);
        }
        static void BuildClimbing(out Heightfield terrain,out VerticalQueries queries,out Vec3 start)
        {
            start=new Vec3(4,0,-10);
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
            terrain=new Heightfield(size,cells,heights);
            queries=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                Box(1,VerticalSurfaceKind.Structure,8,0,-4,14,6,4), // pared y tejado
                Box(2,VerticalSurfaceKind.Structure,6,7,-1,15,8,1), // techo que bloquea la salida central
                Box(3,VerticalSurfaceKind.Structure,8,0,10,12,4,16),
                Box(4,VerticalSurfaceKind.Structure,4,0,14,8,4,16), // esquina interior/exterior
                Box(5,VerticalSurfaceKind.Structure,-12,4,-4,-4,4.25,4), // soporte sobre meseta
                Box(6,VerticalSurfaceKind.Cliff,-2,0,16,4,8,20),
                Box(7,VerticalSurfaceKind.Foliage,-6,0,-12,-5,3,-11),
                Box(8,VerticalSurfaceKind.Interactable,-2,0,-12,-1,2,-11),
                Box(9,VerticalSurfaceKind.Structure,20,0,-3,20.5,5,3), // borde estrecho
                Box(10,VerticalSurfaceKind.Boundary,30,-10,-30,31,12,30),
                // Revestimiento sólido del foso: cubre las franjas interpoladas del
                // heightfield y ofrece cara vertical y apoyo completo en los cuatro lados.
                Box(11,VerticalSurfaceKind.Cliff,15,-6,7,16,0,15),
                Box(12,VerticalSurfaceKind.Cliff,22,-6,7,23,0,15),
                Box(13,VerticalSurfaceKind.Cliff,16,-6,7,22,0,8),
                Box(14,VerticalSurfaceKind.Cliff,16,-6,14,22,0,15));
        }
        // B: escenarios de persecución vertical. Terreno de celdas de 1 m: un salto de altura en
        // una celda es una pared abrupta (>48°) que se escala; la rampa (26,6°) se camina.
        static void BuildPursuit(out Heightfield terrain,out VerticalQueries queries,out Vec3 start)
        {
            start=new Vec3(0,0,-4);
            const int cells=80;const double size=80;
            var heights=new float[(cells+1)*(cells+1)];
            for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++){
                double px=x-size/2,pz=z-size/2;float h=0;
                // Rampa transitable (29,7°) que llega a y=4 un metro antes del edificio R y sigue
                // plana bajo él: la junta con el tejado queda al mismo nivel, sin caras ocultas.
                if(px>=0&&px<=13&&pz>=-13&&pz<=-9)h=(float)System.Math.Min(4,px*4/7);
                // Meseta de paredes abruptas en todos sus lados.
                if(px>=12&&px<=20&&pz>=6&&pz<=14)h=5;
                // Acantilado sin rampa: repisa elevada hasta el límite.
                if(px>=20&&pz>=-22&&pz<=-4)h=4;
                heights[z*(cells+1)+x]=h;
            }
            terrain=new Heightfield(size,cells,heights);
            queries=new VerticalQueries(terrain,Tuning.PlayerMaxSlopeDeg,
                Box(1,VerticalSurfaceKind.Structure,8,0,-14,14,4,-8),      // R: tejado con rampa al oeste y paredes
                Box(2,VerticalSurfaceKind.Structure,-14,0,-14,-8,5,-8),    // C: tejado solo escalando, cuatro paredes
                Box(3,VerticalSurfaceKind.Structure,-6,0,10,6,4,22),       // G: tejado amplio, cabe el jefe
                Box(4,VerticalSurfaceKind.Structure,-23,0,8,-19,4,12),     // T: torre estrecha, no cabe el jefe
                Box(5,VerticalSurfaceKind.Structure,14,0,-4,22,3,-2.5),    // pasillo de 3 m entre dos muros
                Box(6,VerticalSurfaceKind.Structure,14,0,.5,22,3,2),
                Box(7,VerticalSurfaceKind.Structure,-6,0,-22,2,2.5,-21),   // muro en L: esquinas interior/exterior
                Box(8,VerticalSurfaceKind.Structure,1,0,-21,2,2.5,-16),
                Box(9,VerticalSurfaceKind.Structure,-6,0,-3,-5,3,-2),      // pilares
                Box(10,VerticalSurfaceKind.Structure,5,0,1,6,3,2),
                Box(11,VerticalSurfaceKind.Structure,-22,0,-14,-18,3,-10), // pared con techo encima: sin salida
                Box(12,VerticalSurfaceKind.Structure,-23,3.6,-15,-17,4,-9),
                Box(13,VerticalSurfaceKind.Structure,19,0,19,21,5,21),     // seta: cima inaccesible incluso escalando
                Box(14,VerticalSurfaceKind.Structure,17.5,5,17.5,22.5,5.5,22.5),
                Box(15,VerticalSurfaceKind.Foliage,-2,0,4,-1,3,5),
                Box(16,VerticalSurfaceKind.Interactable,2,0,4,3,2,5),
                Box(17,VerticalSurfaceKind.Boundary,30,-10,-30,31,12,30));
        }
        public WorldData Data()
        {
            var props=new PropSet();var kinds=new Dictionary<int,StructureKind>();
            if(Map!=null)foreach(var st in Map.World.Structures)if(!st.Removed)foreach(var v in st.Solids)kinds[v.Id]=st.Kind;
            for(int i=0;i<Queries.Count;i++){
                var b=Queries.Solid(i);uint color=b.Climbable?0x957954u:0x705c80u;
                // C: árboles verdes y marcadores QA de color propio para reconocerlos en las capturas.
                if(kinds.TryGetValue(b.Id,out var kind))switch(kind){
                    case StructureKind.Tree:color=0x3d6b35u;break;case StructureKind.Chest:color=0xc9a227u;break;
                    case StructureKind.Shrine:color=0x4fb3c9u;break;case StructureKind.Totem:color=0xb5473au;break;case StructureKind.Portal:color=0x8a5cc9u;break;
                }
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
