using System;

namespace Mamporro.Core.Audio
{
    // Presupuesto de voces (src/audio/VoiceBudget.ts): como mucho 16 efectos, 4 reservados para
    // prioridad ≥ 1 y enfriamiento por timbre. Lo rechazado no se guarda para más tarde.
    // Sin asignaciones: arrays fijos indexados por voz y por timbre. Reloj inyectado (s).
    public sealed class VoiceBudget
    {
        readonly int[] keys=new int[AudioCatalog.Voices];
        readonly double[] ends=new double[AudioCatalog.Voices];
        readonly double[] last=new double[AudioCatalog.Sounds.Length];
        int count,sequence;
        public VoiceBudget(){Clear();}
        public int Count=>count;
        // Devuelve la clave de la voz concedida (>0) o 0 si se descarta.
        public int Claim(SoundDef def,double now)
        {
            for(int i=count-1;i>=0;i--)if(ends[i]<=now)RemoveAt(i);
            if(now-last[def.Index]<def.Cooldown)return 0;
            if(count>=AudioCatalog.Voices)return 0;
            if(def.Priority==0&&count>=AudioCatalog.Voices-AudioCatalog.Reserved)return 0;
            int key=++sequence;keys[count]=key;ends[count]=now+def.Duration;count++;
            last[def.Index]=now;return key;
        }
        public void Release(int key){for(int i=0;i<count;i++)if(keys[i]==key){RemoveAt(i);return;}}
        public void Clear(){count=0;for(int i=0;i<last.Length;i++)last[i]=double.NegativeInfinity;}
        void RemoveAt(int i){count--;keys[i]=keys[count];ends[i]=ends[count];}
    }
}
