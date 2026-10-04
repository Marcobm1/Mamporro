using System;

namespace Mamporro.Core.Effects
{
    // Presupuesto decorativo (ParticleBudget.ts): no se aplica a proyectiles ni a avisos de ataques.
    // Normal: 256 nuevas por fotograma y 1500 activas; reducido: 64/400 y el 25 % de cada ráfaga
    // (redondeo hacia arriba, para que los efectos pequeños no desaparezcan).
    public sealed class ParticleBudget
    {
        int remaining=256;
        public bool Reduced;
        public int PerFrame=>Reduced?64:256;
        public int Capacity=>Reduced?400:1500;
        public void Reset()=>remaining=PerFrame;
        public int Take(int requested,int active)
        {
            int wanted=Reduced?(int)Math.Ceiling(requested*.25):requested;
            int granted=Math.Max(0,Math.Min(wanted,Math.Min(remaining,Capacity-active)));
            remaining-=granted;return granted;
        }
    }

    // Partículas decorativas (Particles.ts): cubitos que salen disparados, caen y se encogen.
    // Arrays por campo, sin objetos por partícula ni asignaciones al emitir. Aleatoriedad solo
    // visual (Rng propia), nunca la de la partida. Coordenadas web.
    public sealed class ParticleField
    {
        public const int MaxCapacity=1500;
        public readonly float[] X=new float[MaxCapacity],Y=new float[MaxCapacity],Z=new float[MaxCapacity];
        readonly float[] vx=new float[MaxCapacity],vy=new float[MaxCapacity],vz=new float[MaxCapacity];
        public readonly float[] Life=new float[MaxCapacity],MaxLife=new float[MaxCapacity],Size=new float[MaxCapacity],Spin=new float[MaxCapacity];
        readonly float[] gravity=new float[MaxCapacity];
        public readonly uint[] Color=new uint[MaxCapacity];
        readonly ParticleBudget budget=new ParticleBudget();
        readonly Rng rng;
        int count;
        public ParticleField(Rng visual){rng=visual;}
        public int Active=>count;
        public ParticleBudget Budget=>budget;
        // Opción «Reducir partículas»: aplica el presupuesto y recorta lo que sobre al momento.
        public void SetReduced(bool reduced){budget.Reduced=reduced;budget.Reset();count=Math.Min(count,budget.Capacity);}
        public void Clear(){count=0;budget.Reset();}

        public void Burst(double x,double y,double z,int requested,uint[] colors,double speed,double size,double life,double lift=0,double gravityValue=18)
        {
            int n=budget.Take(requested,count);
            const int capacity=MaxCapacity;
            for(int k=0;k<n;k++){
                // Si está lleno, se reutiliza una al azar (las viejas desaparecen antes).
                int i=count<capacity?count++:rng.Int(0,capacity-1);
                double angle=rng.Next()*Math.PI*2,velocity=rng.Range(.3,1)*speed;
                X[i]=(float)x;Y[i]=(float)y;Z[i]=(float)z;
                vx[i]=(float)(Math.Cos(angle)*velocity);vz[i]=(float)(Math.Sin(angle)*velocity);
                vy[i]=(float)(rng.Range(.2,1)*speed*.8+lift);
                float l=(float)(life*rng.Range(.7,1.2));Life[i]=l;MaxLife[i]=l;
                Size[i]=(float)(size*rng.Range(.6,1.3));gravity[i]=(float)gravityValue;Spin[i]=(float)(rng.Next()*Math.PI);
                Color[i]=colors[k%colors.Length];
            }
        }
        void Remove(int i)
        {
            int last=--count;if(i==last)return;
            X[i]=X[last];Y[i]=Y[last];Z[i]=Z[last];vx[i]=vx[last];vy[i]=vy[last];vz[i]=vz[last];
            Life[i]=Life[last];MaxLife[i]=MaxLife[last];Size[i]=Size[last];gravity[i]=gravity[last];Spin[i]=Spin[last];Color[i]=Color[last];
        }
        // Una vez por fotograma (con dt 0 en pausa: el presupuesto se renueva y nada se mueve).
        public void Update(float dt)
        {
            budget.Reset();
            for(int i=count-1;i>=0;i--){
                Life[i]-=dt;if(Life[i]<=0){Remove(i);continue;}
                vy[i]-=gravity[i]*dt;X[i]+=vx[i]*dt;Y[i]+=vy[i]*dt;Z[i]+=vz[i]*dt;Spin[i]+=dt*6;
            }
        }
        // Tamaño dibujado: se encoge en el último tercio de la vida.
        public float Scale(int i)=>Size[i]*Math.Min(1f,Life[i]/MaxLife[i]*1.5f);
    }
}
