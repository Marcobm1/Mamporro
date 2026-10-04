using System;

namespace Mamporro.Core.Audio
{
    // PCM mono original (src/audio/synth.ts): mismas operaciones en double y muestras guardadas
    // en float, como el Float32Array de la web. Sin dispositivo ni UnityEngine: comprobable
    // contra baseline.json.
    public static class AudioSynth
    {
        public static float[] Sound(SoundDef def,int sampleRate=AudioCatalog.SampleRate)
        {
            var samples=new float[(int)Math.Ceiling(def.Duration*sampleRate)];
            var rng=new Rng("sonido");double phase=0;
            for(int i=0;i<samples.Length;i++){
                double progress=(double)i/(samples.Length-1);
                phase+=(def.Frequency+(def.End-def.Frequency)*progress)/sampleRate;
                double wave=Math.Sin(phase*Math.PI*2)>=0?1:-1;
                double envelope=Math.Min(1,i/(sampleRate*.004))*Math.Pow(1-progress,2);
                samples[i]=(float)((wave*(1-def.Noise)+(rng.Next()*2-1)*def.Noise)*def.Gain*envelope);
            }
            return samples;
        }

        static double Hz(int midi)=>440*Math.Pow(2,(midi-69)/12.0);
        // Bucle de ocho compases; los dos arreglos duran lo mismo para cambiar de intensidad.
        public static float[] Music(bool intense,int sampleRate=AudioCatalog.SampleRate)
        {
            double beat=60.0/AudioCatalog.Bpm;
            var samples=new float[(int)JsRound(beat*32*sampleRate)];
            var rng=new Rng("musica");
            for(int i=0;i<samples.Length;i++){
                double time=(double)i/sampleRate;
                int step=(int)Math.Floor(time/(beat/2));
                double local=time%(beat/2);
                int note=AudioCatalog.Melody[step%AudioCatalog.Melody.Length];
                int bass=AudioCatalog.Bass[(int)Math.Floor(time/(beat*4))%AudioCatalog.Bass.Length];
                double envelope=Math.Min(1,local/.008)*Math.Max(0,1-local/(beat*.4));
                double lead=note<0?0:(Math.Sin(Hz(note)*time*Math.PI*2)>0?1:-1)*.065*envelope;
                double low=Math.Sin(Hz(bass+(step%4>=2?12:0))*time*Math.PI*2)*.09*envelope;
                double drumTime=time%beat;
                double kick=Math.Sin(2*Math.PI*(70*drumTime+2*(1-Math.Exp(-drumTime*40))))*Math.Exp(-drumTime*24)*.13;
                double hat=(rng.Next()*2-1)*Math.Exp(-local*100)*(intense?.055:.02);
                double harmony=intense&&note>=0?Math.Sin(Hz(note-12)*time*Math.PI*2)*envelope*.045:0;
                // Bordes suaves: no hay salto de amplitud al repetir el buffer.
                double edge=Math.Min(1,Math.Min(i/(sampleRate*.006),(samples.Length-1-i)/(sampleRate*.006)));
                samples[i]=(float)((lead+low+kick+hat+harmony)*edge);
            }
            return samples;
        }
        // Math.round de JavaScript (mitades hacia +∞).
        static double JsRound(double value)=>Math.Floor(value+.5);
    }
}
