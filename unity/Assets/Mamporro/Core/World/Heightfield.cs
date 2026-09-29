using System;

namespace Mamporro.Core
{
    // Utilidades de src/core/math.ts con las mismas operaciones que la web.
    public static class WorldMath
    {
        public const double Deg2Rad=Math.PI/180;
        public static double Clamp(double v,double min,double max)=>v<min?min:v>max?max:v;
        public static double Clamp01(double v)=>v<0?0:v>1?1:v;
        public static double Lerp(double a,double b,double t)=>a+(b-a)*t;
        public static double Smoothstep(double e0,double e1,double x){double t=Clamp01((x-e0)/(e1-e0));return t*t*(3-2*t);}
        public static double Gain(double x,double s){double xs=Math.Pow(Clamp01(x),s),ys=Math.Pow(Clamp01(1-x),s);return xs/(xs+ys);}
        public const double SquirclePower=5;
        // Distancia tipo squircle (cuadrado de esquinas redondeadas) al centro.
        public static double Squircle(double x,double z)=>Math.Pow(Math.Pow(Math.Abs(x),SquirclePower)+Math.Pow(Math.Abs(z),SquirclePower),1/SquirclePower);
    }

    public struct Vec3 { public double X,Y,Z; public Vec3(double x,double y,double z){X=x;Y=y;Z=z;} }

    // Terreno como rejilla de alturas Float32 (src/world/Heightfield.ts).
    public sealed class Heightfield
    {
        public readonly double Size,CellSize,Half;
        public readonly int Cells,Stride;
        public readonly float[] Heights;

        public Heightfield(double size,int cells,float[] heights)
        {
            Size=size;Cells=cells;Heights=heights;CellSize=size/cells;Half=size/2;Stride=cells+1;
            if(heights.Length!=Stride*Stride)throw new ArgumentException("Heightfield: tamaño de alturas incorrecto");
        }

        public double VertexHeight(int i,int j){int n=Cells;int ci=i<0?0:i>n?n:i,cj=j<0?0:j>n?n:j;return Heights[cj*Stride+ci];}

        // Altura exacta de la superficie en (x, z); diagonal (i, j+1)–(i+1, j), como el mesh.
        public double HeightAt(double x,double z)
        {
            double gx=(x+Half)/CellSize,gz=(z+Half)/CellSize;
            int i=(int)Math.Floor(gx),j=(int)Math.Floor(gz),last=Cells-1;
            if(i<0)i=0;else if(i>last)i=last;
            if(j<0)j=0;else if(j>last)j=last;
            double u=gx-i,v=gz-j;
            u=u<0?0:u>1?1:u;v=v<0?0:v>1?1:v;
            int s=Stride;
            double h00=Heights[j*s+i],h10=Heights[j*s+i+1],h01=Heights[(j+1)*s+i],h11=Heights[(j+1)*s+i+1];
            if(u+v<=1)return h00+(h10-h00)*u+(h01-h00)*v;
            return h11+(h01-h11)*(1-u)+(h10-h11)*(1-v);
        }

        public Vec3 NormalAt(double x,double z)
        {
            double gx=(x+Half)/CellSize,gz=(z+Half)/CellSize;
            int i=(int)Math.Floor(gx),j=(int)Math.Floor(gz),last=Cells-1;
            if(i<0)i=0;else if(i>last)i=last;
            if(j<0)j=0;else if(j>last)j=last;
            double u=gx-i,v=gz-j;
            int s=Stride;
            double h00=Heights[j*s+i],h10=Heights[j*s+i+1],h01=Heights[(j+1)*s+i],h11=Heights[(j+1)*s+i+1],dx,dz;
            if(u+v<=1){dx=(h10-h00)/CellSize;dz=(h01-h00)/CellSize;}
            else{dx=(h11-h01)/CellSize;dz=(h11-h10)/CellSize;}
            double inv=1/Math.Sqrt(dx*dx+1+dz*dz);
            return new Vec3(-dx*inv,inv,-dz*inv);
        }

        static double Fbm(SimplexNoise2D noise,double x,double z,int octaves)
        {
            double amplitude=1,frequency=1,sum=0,norm=0;
            for(int o=0;o<octaves;o++){sum+=amplitude*noise.Noise(x*frequency,z*frequency);norm+=amplitude;amplitude*=0.5;frequency*=2;}
            return sum/norm;
        }

        // generateHeightfield: colinas fBm con distorsión, terrazas, zona de inicio llana y montañas del borde.
        public static Heightfield Generate(Rng rng)
        {
            Func<double> random=rng.Next;
            var noiseBase=new SimplexNoise2D(random);var noiseWarpX=new SimplexNoise2D(random);var noiseWarpZ=new SimplexNoise2D(random);
            var noiseRamp=new SimplexNoise2D(random);var noiseDetail=new SimplexNoise2D(random);var noiseEdge=new SimplexNoise2D(random);
            double size=Tuning.TerrainSize,relief=Tuning.TerrainRelief,terraceStep=Tuning.TerrainTerraceStep,borderStart=Tuning.TerrainBorderStart,spawnRadius=Tuning.TerrainSpawnRadius;
            int cells=(int)Tuning.TerrainCells;double half=size/2;
            Func<double,double,double> interior=(x,z)=>{
                double wx=x+16*noiseWarpX.Noise(x*0.005,z*0.005),wz=z+16*noiseWarpZ.Noise(x*0.005,z*0.005);
                double h=(Fbm(noiseBase,wx*0.0075,wz*0.0075,4)*0.5+0.5)*relief;
                double t=h/terraceStep,k=Math.Floor(t);
                double cliffiness=WorldMath.Smoothstep(-0.35,0.45,noiseRamp.Noise(x*0.011,z*0.011));
                double sharp=WorldMath.Lerp(1.2,9,cliffiness);
                double terraced=(k+WorldMath.Gain(t-k,sharp))*terraceStep;
                h=WorldMath.Lerp(h,terraced,0.9);
                return h+0.45*noiseDetail.Noise(x*0.09,z*0.09);
            };
            double spawnLevel=Rules.Round(interior(0,0)/terraceStep)*terraceStep;
            int stride=cells+1;var heights=new float[stride*stride];double cellSize=size/cells;
            for(int j=0;j<stride;j++){
                double z=j*cellSize-half;
                for(int i=0;i<stride;i++){
                    double x=i*cellSize-half,h=interior(x,z),d=JsMath.Hypot(x,z);
                    h=WorldMath.Lerp(spawnLevel,h,WorldMath.Smoothstep(spawnRadius*0.6,spawnRadius*1.9,d));
                    double r=WorldMath.Squircle(x,z)+7*noiseEdge.Noise(x*0.03,z*0.03);
                    double e=WorldMath.Smoothstep(borderStart,half-4,r);
                    double ridge=1-Math.Abs(noiseEdge.Noise(x*0.022+50,z*0.022+50));
                    h+=e*e*30+e*ridge*ridge*34+e*5*noiseEdge.Noise(x*0.09+90,z*0.09+90);
                    heights[j*stride+i]=(float)h;
                }
            }
            return new Heightfield(size,cells,heights);
        }
    }
}
