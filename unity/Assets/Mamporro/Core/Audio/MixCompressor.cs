using System;

namespace Mamporro.Core.Audio
{
    // Compresor de la mezcla: aproximación del DynamicsCompressorNode que usa la web (umbral
    // −12 dB, relación 8, rodilla 30 dB, ataque 3 ms, liberación 250 ms). No es idéntico al del
    // navegador: curva de rodilla suave cuadrática, detector de pico enlazado entre canales y
    // suavizado de un polo en dB. Como el navegador, aplica una ganancia de compensación fija
    // (la ganancia que falta a 0 dBFS elevada a 0,6). Termina con un tope duro en ±1.
    // Pensado para el hilo de audio: sin asignaciones, sin excepciones y sin NaN/Infinity
    // (una muestra no finita se convierte en silencio).
    public sealed class MixCompressor
    {
        public const double Threshold=-12,Ratio=8,Knee=30,Attack=.003,Release=.25;
        readonly double makeup;
        double attackCoef,releaseCoef,reduction;int rate;
        public MixCompressor(int sampleRate){makeup=Math.Pow(DbToGain(-Curve(0)),.6);SetSampleRate(sampleRate);}
        // Reducción actual (dB ≤ 0) y compensación lineal, para pruebas y diagnóstico.
        public double Reduction=>reduction;
        public double Makeup=>makeup;
        public void Reset()=>reduction=0;
        void SetSampleRate(int sampleRate)
        {
            rate=Math.Max(1,sampleRate);
            attackCoef=Math.Exp(-1/(Attack*rate));releaseCoef=Math.Exp(-1/(Release*rate));
        }
        // Reducción estática (dB) para una entrada en dB: rodilla suave alrededor del umbral.
        public static double Curve(double inputDb)
        {
            double over=inputDb-Threshold;
            if(2*over< -Knee)return 0;
            if(2*Math.Abs(over)<=Knee){double x=over+Knee/2;return (1/Ratio-1)*x*x/(2*Knee);}
            return over/Ratio-over;
        }
        static double DbToGain(double db)=>Math.Exp(db*0.11512925464970229);
        // Procesa en el sitio un bloque intercalado (frames × canales).
        public void Process(float[] data,int channels,int sampleRate)
        {
            if(data==null||channels<=0)return;
            if(sampleRate!=rate)SetSampleRate(sampleRate);
            for(int i=0;i+channels<=data.Length;i+=channels){
                double peak=0;
                for(int c=0;c<channels;c++){
                    float s=data[i+c];
                    if(float.IsNaN(s)||float.IsInfinity(s)){data[i+c]=0;continue;}
                    double a=s<0?-s:s;if(a>peak)peak=a;
                }
                double target=peak>1e-6?Curve(20*Math.Log10(peak)):0;
                reduction=target<reduction?target+(reduction-target)*attackCoef:target+(reduction-target)*releaseCoef;
                if(double.IsNaN(reduction)||double.IsInfinity(reduction))reduction=0;
                double gain=DbToGain(reduction)*makeup;
                for(int c=0;c<channels;c++){
                    double v=data[i+c]*gain;
                    data[i+c]=v>1?1f:v< -1?-1f:(float)v;
                }
            }
        }
    }
}
