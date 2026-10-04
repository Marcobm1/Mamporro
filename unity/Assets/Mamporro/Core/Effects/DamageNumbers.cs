using System;

namespace Mamporro.Core.Effects
{
    // Un carácter visible de un número de daño, ya colocado (DamageNumbers.update de la web).
    public struct DamageGlyph
    {
        public float X,Y,Z;      // ancla del número (coordenadas web)
        public float Slot;       // posición del carácter respecto al centro (en avances)
        public int Glyph;        // índice en DamageNumbers.Chars
        public int Kind;         // −1 jugador, 0 normal, 1 crítico, 2 supercrítico
        public float Alpha,Scale;
    }

    // Números de daño flotantes (src/render/DamageNumbers.ts): hasta 140 números de 6 caracteres,
    // 0,75 s de vida, subida 1,5t − 0,6t², desvanecido en el último 35 %, «pop» de 0,08 s y deriva
    // lateral. Críticos con «!» o «!!» y más grandes; el daño recibido, en rojo. Sin cadenas ni
    // asignaciones: dígitos en un buffer fijo. Aleatoriedad visual propia.
    public sealed class DamageNumbers
    {
        public const int MaxNumbers=140,MaxChars=6;
        public const float LifeTime=.75f;
        public const string Chars="0123456789!";
        readonly float[] x=new float[MaxNumbers],y=new float[MaxNumbers],z=new float[MaxNumbers],drift=new float[MaxNumbers],age=new float[MaxNumbers],scale=new float[MaxNumbers];
        readonly int[] kind=new int[MaxNumbers],length=new int[MaxNumbers];
        readonly byte[] text=new byte[MaxNumbers*MaxChars];
        readonly byte[] digits=new byte[24];
        readonly Rng rng;
        int count;
        public DamageNumbers(Rng visual){rng=visual;}
        public int Active=>count;
        public void Clear()=>count=0;
        public string Text(int i){var c=new char[length[i]];for(int k=0;k<c.Length;k++)c[k]=Chars[text[i*MaxChars+k]];return new string(c);}
        public int KindOf(int i)=>kind[i];

        public void Spawn(double px,double py,double pz,double amount,int level)
        {
            if(count>=MaxNumbers)RemoveAt(0);
            int i=count++;
            // Math.round de JS y mínimo 1.
            long value=Math.Max(1,(long)Math.Floor(amount+.5));
            int n=0;do{digits[n++]=(byte)(value%10);value/=10;}while(value>0&&n<digits.Length);
            int len=0;for(int k=n-1;k>=0&&len<MaxChars;k--)text[i*MaxChars+len++]=digits[k];
            for(int bang=0;bang<(level>=2?2:level>=1?1:0)&&len<MaxChars;bang++)text[i*MaxChars+len++]=10;
            length[i]=len;
            x[i]=(float)(px+rng.Range(-.25,.25));y[i]=(float)py;z[i]=(float)(pz+rng.Range(-.25,.25));drift[i]=(float)rng.Range(-.4,.4);
            age[i]=0;kind[i]=level;scale[i]=level>=1?1.45f:level<0?1.3f:1f;
        }
        void RemoveAt(int i)
        {
            count--;
            for(int k=i;k<count;k++){
                x[k]=x[k+1];y[k]=y[k+1];z[k]=z[k+1];drift[k]=drift[k+1];age[k]=age[k+1];scale[k]=scale[k+1];kind[k]=kind[k+1];length[k]=length[k+1];
                Buffer.BlockCopy(text,(k+1)*MaxChars,text,k*MaxChars,MaxChars);
            }
        }
        // Envejece y retira; dt 0 en pausa.
        public void Update(float dt)
        {
            for(int i=count-1;i>=0;i--){age[i]+=dt;if(age[i]>=LifeTime)RemoveAt(i);}
        }
        // Coloca los caracteres visibles (del más nuevo al más viejo, como la web). Devuelve cuántos.
        public int Layout(DamageGlyph[] output)
        {
            int n=0;
            for(int i=count-1;i>=0;i--){
                float t=age[i]/LifeTime,rise=1.5f*t-.6f*t*t,alpha=t<.65f?1:1-(t-.65f)/.35f;
                float pop=age[i]<.08f?1.35f-age[i]/.08f*.35f:1;
                int len=length[i];
                for(int c=0;c<len&&n<output.Length;c++)
                    output[n++]=new DamageGlyph{X=x[i]+drift[i]*t,Y=y[i]+rise,Z=z[i],Slot=c-(len-1)/2f,Glyph=text[i*MaxChars+c],Kind=kind[i],Alpha=alpha,Scale=scale[i]*pop};
            }
            return n;
        }
    }
}
