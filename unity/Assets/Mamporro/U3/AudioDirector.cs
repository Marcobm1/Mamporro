using System;
using Mamporro.Core.Audio;
using Mamporro.Core.Progress;
using UnityEngine;

namespace Mamporro.U3
{
    // U5 paso 2: audio equivalente a AudioEngine.ts. Clips PCM originales sintetizados una vez
    // (AudioSynth), 16 fuentes de efectos con VoiceBudget (4 reservadas, enfriamientos, lo
    // descartado no se aplaza), 2 fuentes de música (normal/intensa, cambio a la misma posición
    // del reloj de audio), buses de música/efectos/maestro con rampas de 15/40 ms, silencio y modos
    // menú ×0,65, partida ×1, intensa, pausa/cartas ×0,25 y resultados ×1. Compresor y medidor en
    // la escucha (MixFilter). Diferencias intencionadas (DECISIONES, U5): suena desde el arranque
    // y, sin foco, silencio total y efectos descartados.
    public enum MusicMode { Menu, Playing, Intense, Paused, Results }

    public sealed class AudioDirector : MonoBehaviour
    {
        public const float Master=.8f;
        AudioClip[] clips;AudioClip[] tracks;
        AudioSource[] voices;readonly int[] voiceKeys=new int[AudioCatalog.Voices];
        AudioSource[] music;int trackIndex=-1;
        readonly VoiceBudget budget=new VoiceBudget();
        float masterLevel,effectsLevel,musicLevel;bool levelsReady;
        bool focused=true,configurationChanged;
        public MusicMode Mode {get;private set;}=MusicMode.Menu;
        public MixFilter Mix {get;private set;}
        public AudioListener Listener {get;private set;}
        // Recuentos para pruebas y ensayo: concedidos, descartados (presupuesto, sin fuente o sin audio).
        public int Played {get;private set;}
        public int Dropped {get;private set;}
        public int ActiveVoices{get{int n=0;foreach(var v in voices)if(v.isPlaying)n++;return n;}}
        public int BudgetCount=>budget.Count;
        public int TrackIndex=>trackIndex;
        public AudioSource CurrentTrack=>trackIndex>=0?music[trackIndex]:null;
        public float MusicLevel=>musicLevel;
        public float EffectsLevel=>effectsLevel;
        public float MasterLevel=>masterLevel;
        public bool Silenced=>!focused;
        public int Voices=>voices.Length;
        public AudioClip ClipOf(string id)=>clips[AudioCatalog.Get(id).Index];
        // Opciones vigentes: se leen siempre del progreso (el objeto cambia al guardar).
        Func<SettingsDto> source;
        SettingsDto settings=>source?.Invoke();

        public void Initialize(GameObject listenerHost,Func<SettingsDto> current)
        {
            source=current;
            // Componentes de Unity: comprobación explícita (el operador ?? no respeta su «null»).
            Listener=listenerHost.GetComponent<AudioListener>();if(!Listener)Listener=listenerHost.AddComponent<AudioListener>();
            Mix=listenerHost.GetComponent<MixFilter>();if(!Mix)Mix=listenerHost.AddComponent<MixFilter>();
            clips=new AudioClip[AudioCatalog.Sounds.Length];
            foreach(var def in AudioCatalog.Sounds)clips[def.Index]=Clip("Sonido "+def.Id,AudioSynth.Sound(def));
            tracks=new[]{Clip("Música normal",AudioSynth.Music(false)),Clip("Música intensa",AudioSynth.Music(true))};
            voices=new AudioSource[AudioCatalog.Voices];for(int i=0;i<voices.Length;i++)voices[i]=Source("Efecto "+i,false);
            music=new AudioSource[2];for(int i=0;i<2;i++){music[i]=Source("Música "+i,true);music[i].clip=tracks[i];}
            AudioSettings.OnAudioConfigurationChanged+=OnConfiguration;
        }
        static AudioClip Clip(string name,float[] samples)
        {var clip=AudioClip.Create(name,samples.Length,1,AudioCatalog.SampleRate,false);clip.SetData(samples,0);return clip;}
        AudioSource Source(string name,bool loop)
        {
            var o=new GameObject(name);o.transform.SetParent(transform,false);var s=o.AddComponent<AudioSource>();
            s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;s.dopplerLevel=0;s.volume=0;s.priority=loop?0:128;return s;
        }
        void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged-=OnConfiguration;
            if(clips!=null)foreach(var c in clips)if(c)Destroy(c);if(tracks!=null)foreach(var c in tracks)if(c)Destroy(c);
            AudioListener.pause=false;
        }
        // Cambio de dispositivo o de configuración: Unity detiene las fuentes; se rehace en Update.
        void OnConfiguration(bool deviceChanged){configurationChanged=true;}

        // Opciones vigentes (Game.applySettings → audio.configure).
        public void Configure()
        {
            if(settings.muted||settings.effectsVolume<=0)ClearEffects();
            UpdateTrack();
        }
        public void SetMode(MusicMode mode){if(mode==Mode)return;Mode=mode;UpdateTrack();}
        // Foco de la ventana: sin foco, silencio total y sin efectos pendientes; al volver no hay cola.
        public void SetFocused(bool value)
        {
            if(value==focused)return;focused=value;
            if(!focused)ClearEffects();
            AudioListener.pause=!focused;
            if(focused)UpdateTrack();
        }

        public void Play(string id)=>Play(AudioCatalog.Get(id));
        public void Play(SoundDef def)
        {
            if(!focused||settings==null||settings.muted||settings.effectsVolume<=0){Dropped++;return;}
            int free=-1;for(int i=0;i<voices.Length;i++)if(voiceKeys[i]==0){free=i;break;}
            // Como los nodos pendientes de la web: sin fuente libre no se admite nada más.
            if(free<0){Dropped++;return;}
            int key=budget.Claim(def,AudioSettings.dspTime);
            if(key==0){Dropped++;return;}
            var source=voices[free];voiceKeys[free]=key;source.clip=clips[def.Index];source.volume=masterLevel*effectsLevel;source.Play();Played++;
        }
        public void ClearEffects()
        {
            if(voices==null)return;
            for(int i=0;i<voices.Length;i++){voices[i].Stop();voiceKeys[i]=0;}
            budget.Clear();
        }

        float TargetMaster=>settings.muted?0:Master;
        float TargetMusic=>(float)settings.musicVolume*(Mode==MusicMode.Paused?.25f:Mode==MusicMode.Menu?.65f:1f);
        void UpdateTrack()
        {
            if(music==null||settings==null)return;
            int index=Mode==MusicMode.Intense?1:0;
            if(settings.muted||settings.musicVolume<=0){StopTracks();return;}
            if(index==trackIndex&&music[index].isPlaying)return;
            StopTracks();
            var source=music[index];
            // Ambas variantes comparten compás: misma posición respecto al reloj de audio (web).
            int length=source.clip.samples;source.timeSamples=(int)((long)(AudioSettings.dspTime*AudioCatalog.SampleRate)%length);
            source.volume=masterLevel*musicLevel;source.Play();trackIndex=index;
        }
        void StopTracks(){foreach(var m in music)m.Stop();trackIndex=-1;}

        void Update()
        {
            if(voices==null||settings==null)return;
            if(configurationChanged){configurationChanged=false;ClearEffects();trackIndex=-1;UpdateTrack();}
            // Rampas como setTargetAtTime (15 ms maestro/efectos, 40 ms música).
            float dt=Time.unscaledDeltaTime;
            if(!levelsReady){masterLevel=TargetMaster;effectsLevel=(float)settings.effectsVolume;musicLevel=TargetMusic;levelsReady=true;}
            else{
                masterLevel=Approach(masterLevel,TargetMaster,dt,.015f);effectsLevel=Approach(effectsLevel,(float)settings.effectsVolume,dt,.015f);
                musicLevel=Approach(musicLevel,TargetMusic,dt,.04f);
            }
            for(int i=0;i<voices.Length;i++){
                if(voiceKeys[i]==0)continue;
                if(!voices[i].isPlaying&&focused){budget.Release(voiceKeys[i]);voiceKeys[i]=0;continue;}
                voices[i].volume=masterLevel*effectsLevel;
            }
            foreach(var m in music)m.volume=masterLevel*musicLevel;
            if(focused&&trackIndex>=0&&!music[trackIndex].isPlaying)UpdateTrack();
        }
        static float Approach(float value,float target,float dt,float tau)=>target+(value-target)*Mathf.Exp(-dt/tau);
    }

    // Filtro de la escucha (hilo de audio): compresor de la mezcla y medidor de salida (RMS y pico
    // por bloque, con un máximo reciente). El medidor demuestra que hay señal, no cómo suena.
    public sealed class MixFilter : MonoBehaviour
    {
        MixCompressor compressor;int sampleRate;
        volatile float rms,peak,heldPeak;volatile int blocks;
        public float Rms=>rms;
        public float Peak=>peak;
        public float HeldPeak=>heldPeak;
        public int Blocks=>blocks;
        public double Reduction=>compressor!=null?compressor.Reduction:0;
        public void ResetPeak()=>heldPeak=0;
        void Awake(){sampleRate=AudioSettings.outputSampleRate;compressor=new MixCompressor(sampleRate>0?sampleRate:48000);}
        void OnAudioFilterRead(float[] data,int channels)
        {
            var c=compressor;if(c==null)return;
            c.Process(data,channels,sampleRate>0?sampleRate:48000);
            double sum=0;float max=0;
            for(int i=0;i<data.Length;i++){float s=data[i];sum+=s*s;float a=s<0?-s:s;if(a>max)max=a;}
            rms=data.Length>0?(float)Math.Sqrt(sum/data.Length):0;peak=max;if(max>heldPeak)heldPeak=max;blocks++;
        }
    }
}
