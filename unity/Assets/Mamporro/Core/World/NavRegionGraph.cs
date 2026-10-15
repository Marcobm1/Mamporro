using System;

namespace Mamporro.Core
{
    // D1 optativo. Coordenadas mundiales; el origen posee cada arista y toda su trayectoria.
    // Los nodos de halo solo ofrecen contexto/destinos, nunca aristas de otra región.
    public sealed class NavRegionGraph
    {
        public readonly int X,Z,Size,Columns;
        public readonly NavGraph Graph;
        public readonly int Attempts,OwnedNodes;
        public readonly double BuildMs;
        public int Id=>Z*Columns+X;
        public NavRegionGraph(VerticalQueries queries,Heightfield terrain,Func<double,double,double,bool> inside,NavClass body,int size,int x,int z)
        {
            if(size!=16&&size!=32&&size!=64)throw new ArgumentOutOfRangeException(nameof(size));
            Size=size;X=x;Z=z;Columns=(int)Math.Ceiling(terrain.Size/size);
            if(x<0||z<0||x>=Columns||z>=Columns)throw new ArgumentOutOfRangeException(nameof(x));
            int world=(int)Math.Round(terrain.Size/NavGraph.Cell),cells=(int)(size/NavGraph.Cell);
            int x0=x*cells,z0=z*cells,x1=Math.Min(world,x0+cells),z1=Math.Min(world,z0+cells);
            var watch=System.Diagnostics.Stopwatch.StartNew();
            // Mayor que SurfaceBeyond de L (8,05 m), caída (6,5 m/eje) y vecindad Walk.
            // Paredes inclinadas pueden salir más lejos: ampliar al detectar la consulta, sin
            // truncar la trayectoria ni aceptar un resultado incompleto por falta de contexto.
            int halo=(int)Math.Ceiling(Math.Max(10,NavGraph.Margin+NavGraph.Cell+2*body.Radius+3)/NavGraph.Cell);
            while(true){
                Attempts++;
                Graph=new NavGraph(queries,terrain,inside,body,Math.Max(0,x0-halo),Math.Max(0,z0-halo),Math.Min(world,x1+halo),Math.Min(world,z1+halo),x0,z0,x1,z1);
                if(!Graph.NeedsMoreContext)break;
                if(halo>=world)throw new InvalidOperationException("Contexto mundial insuficiente");
                halo=Math.Min(world,halo*2);
            }
            for(int n=0;n<Graph.NodeCount;n++)if(Graph.Owns(n))OwnedNodes++;
            BuildMs=watch.Elapsed.TotalMilliseconds;
        }
    }
}
