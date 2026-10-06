using System;

namespace Mamporro.Core
{
    public enum VerticalSurfaceKind { Structure, Cliff, Foliage, Interactable, Boundary }

    // P0: datos de colisión QA independientes del render. No modifica WorldCollision U3.
    public sealed class VerticalSolid
    {
        public readonly int Id;
        public readonly VerticalSurfaceKind Kind;
        public readonly Vec3 Min,Max;
        public bool Climbable=>Kind==VerticalSurfaceKind.Structure||Kind==VerticalSurfaceKind.Cliff;
        public VerticalSolid(int id,VerticalSurfaceKind kind,Vec3 min,Vec3 max)
        {
            if(!VerticalQueries.Finite(min)||!VerticalQueries.Finite(max)||min.X>=max.X||min.Y>=max.Y||min.Z>=max.Z)
                throw new ArgumentException("Volumen vertical inválido");
            Id=id;Kind=kind;Min=min;Max=max;
        }
    }
    public struct VerticalHit
    {
        public int Id;
        public VerticalSurfaceKind Kind;
        public Vec3 Normal;
        public double Fraction;
    }

    public sealed class VerticalQueries
    {
        const double Epsilon=1e-9;
        readonly VerticalSolid[] solids;
        public int Count=>solids.Length;
        public VerticalSolid Solid(int i)=>solids[i];
        public VerticalQueries(params VerticalSolid[] input)
        {
            if(input==null)throw new ArgumentNullException(nameof(input));
            solids=(VerticalSolid[])input.Clone();
            for(int i=0;i<solids.Length;i++){
                if(solids[i]==null)throw new ArgumentException("Superficie ausente");
                for(int j=0;j<i;j++)if(solids[j].Id==solids[i].Id)throw new ArgumentException("ID de superficie duplicado");
            }
        }
        public static bool Finite(Vec3 p)=>Valid(p.X)&&Valid(p.Y)&&Valid(p.Z);
        static bool Valid(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        static void Validate(Vec3 p,double radius,double height)
        {
            if(!Finite(p)||!Valid(radius)||!Valid(height)||radius<0||height<0)throw new ArgumentException("Consulta vertical inválida");
        }
        // Envolvente conservadora del cilindro del jugador: caja de radio r y altura h.
        // Barrido continuo por slabs: sin muestreo que pueda saltarse paredes finas.
        // El volumen es de consulta, no sustituye ni agranda los colliders del mundo.
        public bool Clear(Vec3 feet,double radius,double height)
        {
            Validate(feet,radius,height);
            foreach(var s in solids)if(feet.X>s.Min.X-radius+Epsilon&&feet.X<s.Max.X+radius-Epsilon
                &&feet.Y>s.Min.Y-height+Epsilon&&feet.Y<s.Max.Y-Epsilon
                &&feet.Z>s.Min.Z-radius+Epsilon&&feet.Z<s.Max.Z+radius-Epsilon)return false;
            return true;
        }
        static bool Slab(double start,double delta,double min,double max,Vec3 low,Vec3 high,ref double enter,ref double exit,ref Vec3 normal)
        {
            if(Math.Abs(delta)<Epsilon)return start>min+Epsilon&&start<max-Epsilon;
            double a=(min-start)/delta,b=(max-start)/delta;Vec3 n=low;
            if(a>b){double temp=a;a=b;b=temp;n=high;}
            if(a>enter){enter=a;normal=n;}exit=Math.Min(exit,b);
            return enter<exit-Epsilon;
        }
        public bool Sweep(Vec3 from,Vec3 to,double radius,double height,out VerticalHit hit)
        {
            Validate(from,radius,height);Validate(to,radius,height);
            hit=new VerticalHit{Fraction=1,Id=-1};bool found=false;
            foreach(var s in solids){
                double enter=double.NegativeInfinity,exit=double.PositiveInfinity;Vec3 normal=default;
                if(!Slab(from.X,to.X-from.X,s.Min.X-radius,s.Max.X+radius,new Vec3(-1,0,0),new Vec3(1,0,0),ref enter,ref exit,ref normal)
                    ||!Slab(from.Y,to.Y-from.Y,s.Min.Y-height,s.Max.Y,new Vec3(0,-1,0),new Vec3(0,1,0),ref enter,ref exit,ref normal)
                    ||!Slab(from.Z,to.Z-from.Z,s.Min.Z-radius,s.Max.Z+radius,new Vec3(0,0,-1),new Vec3(0,0,1),ref enter,ref exit,ref normal))continue;
                if(exit<=Epsilon||enter>1||enter>=hit.Fraction&&found)continue;
                hit=new VerticalHit{Id=s.Id,Kind=s.Kind,Normal=normal,Fraction=Math.Max(0,enter)};found=true;
            }
            return found;
        }
        public bool Wall(Vec3 feet,Vec3 toward,double radius,double height,double reach,out VerticalHit hit)
        {
            if(!Valid(reach)||reach<0||!Finite(toward))throw new ArgumentException("Alcance inválido");
            double length=JsMath.Hypot(toward.X,toward.Z);hit=default;if(length<Epsilon)return false;
            var end=new Vec3(feet.X+toward.X/length*reach,feet.Y,feet.Z+toward.Z/length*reach);
            if(!Sweep(feet,end,radius,height,out hit)||Math.Abs(hit.Normal.Y)>.01)return false;
            // El primer sólido bloquea también la consulta: nunca buscar una pared detrás del follaje sólido.
            return hit.Kind==VerticalSurfaceKind.Structure||hit.Kind==VerticalSurfaceKind.Cliff;
        }
        public bool Support(Vec3 feet,double radius,double tolerance,out int id)
        {
            Validate(feet,radius,0);if(!Valid(tolerance)||tolerance<0)throw new ArgumentException("Tolerancia inválida");
            id=-1;
            foreach(var s in solids)if(Math.Abs(feet.Y-s.Max.Y)<=tolerance&&feet.X-radius>=s.Min.X-Epsilon
                &&feet.X+radius<=s.Max.X+Epsilon&&feet.Z-radius>=s.Min.Z-Epsilon&&feet.Z+radius<=s.Max.Z+Epsilon){id=s.Id;return true;}
            return false;
        }
        // Salida con dos tramos explícitos (ascenso exterior, avance sobre el borde).
        // El consumidor debe recorrerlos; esta consulta no coloca ni teletransporta al jugador.
        public bool Ledge(Vec3 feet,VerticalHit wall,double radius,double height,out Vec3 lift,out Vec3 top)
        {
            lift=top=default;VerticalSolid solid=null;
            foreach(var s in solids)if(s.Id==wall.Id){solid=s;break;}
            if(solid==null||!solid.Climbable||Math.Abs(wall.Normal.Y)>.01)return false;
            lift=new Vec3(feet.X,solid.Max.Y+.002,feet.Z);
            // El agarre puede estar separado hasta Reach de la cara. El apoyo se
            // calcula desde esa cara, no desde la distancia circunstancial del cuerpo.
            double x=wall.Normal.X<-.5?solid.Min.X+radius+.02:wall.Normal.X>.5?solid.Max.X-radius-.02:lift.X;
            double z=wall.Normal.Z<-.5?solid.Min.Z+radius+.02:wall.Normal.Z>.5?solid.Max.Z-radius-.02:lift.Z;
            top=new Vec3(x,lift.Y,z);
            return Clear(feet,radius,height)&&Clear(lift,radius,height)&&Clear(top,radius,height)
                &&!Sweep(feet,lift,radius,height,out _)&&!Sweep(lift,top,radius,height,out _)
                &&Support(top,radius,.003,out _);
        }
    }
}
