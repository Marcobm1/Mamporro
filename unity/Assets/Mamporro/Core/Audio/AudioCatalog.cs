using System;

namespace Mamporro.Core.Audio
{
    // Timbre original (src/data/audio.ts): frecuencia inicial/final, duración, ganancia,
    // mezcla de ruido, prioridad (0 normal, 1–2 señales importantes) y enfriamiento.
    public sealed class SoundDef
    {
        public readonly string Id;
        public readonly int Index;
        public readonly double Frequency,End,Duration,Gain,Noise,Cooldown;
        public readonly int Priority;
        internal SoundDef(string id,int index,double frequency,double end,double duration,int priority,double noise,double gain,double cooldown)
        {Id=id;Index=index;Frequency=frequency;End=end;Duration=duration;Priority=priority;Noise=noise;Gain=gain;Cooldown=cooldown;}
    }

    // Catálogo sonoro de la web aprobada (0505b16): 20 efectos, configuración y música.
    public static class AudioCatalog
    {
        public const int SampleRate=22050,Voices=16,Reserved=4,Bpm=132;
        public static readonly SoundDef[] Sounds;
        // Ocho compases, melodía sincopada y bajo saltarín; -1 es un silencio.
        public static readonly int[] Melody={72,-1,76,79,76,72,74,-1,71,74,77,-1,74,71,67,-1,
            69,-1,72,76,79,76,72,69,67,71,74,-1,79,77,74,-1};
        public static readonly int[] Bass={48,43,47,43,45,40,43,47};

        static AudioCatalog()
        {
            int n=0;
            SoundDef S(string id,double frequency,double end,double duration,int priority=0,double noise=0,double gain=.14,double cooldown=.08)
                =>new SoundDef(id,n++,frequency,end,duration,priority,noise,gain,cooldown);
            Sounds=new[]{
                S("ui",700,950,.055),
                S("chancla",320,90,.14,0,.35),
                S("naftalina",150,240,.2,0,.2,.09),
                S("barra",180,65,.16,0,.5),
                S("dentaduras",550,300,.09),
                S("jersey",950,180,.2,0,.6),
                S("fregona",220,90,.18,0,.7,.1),
                S("hit",140,65,.065,0,.65,.08,.055),
                S("critical",650,160,.12,0,.3),
                S("death",170,35,.18,0,.65,.1),
                S("hurt",240,50,.35,2,.25,.22),
                S("xp",900,1250,.07,0,0,.08,.12),
                S("gold",1300,1700,.1,0,0,.09,.14),
                S("reward",450,1400,.45,1),
                S("level",300,1600,.6,2),
                S("shield",1100,440,.35,2,.1),
                S("boss",180,40,.8,2,.4,.2),
                S("blast",100,25,.4,1,.8),
                S("victory",260,1700,1.1,2,0,.18),
                S("defeat",500,60,.9,2,.1,.18),
            };
        }
        public static SoundDef Get(string id)
        {
            var def=Array.Find(Sounds,s=>s.Id==id);
            if(def==null)throw new ArgumentException("Sonido desconocido",nameof(id));
            return def;
        }
    }
}
