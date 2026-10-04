using System;

namespace Mamporro.Core.Progress
{
    // DTO de salida validada. No deserializar entrada externa directamente aquí:
    // faltaría distinguir ausencia, tipo incorrecto y cero, además de limitar el parser.
    [Serializable] public sealed class ProgressDto
    {
        public SettingsDto settings;
        public MetaDto meta;
        public static ProgressDto New(string language) => new ProgressDto {
            settings=SettingsDto.Default(language),meta=MetaRules.New() };
    }

    [Serializable] public sealed class SettingsDto
    {
        public double musicVolume,effectsVolume,mouseSensitivity;
        public bool muted,reducedParticles,cameraShake,flashes,vertexSnap,dithering,slideWithCtrl,showFps;
        public string language;
        public int renderHeight,runMinutes;
        public static SettingsDto Default(string language)
        {
            if(language!="es"&&language!="en")throw new ArgumentException("Idioma no admitido",nameof(language));
            return new SettingsDto { musicVolume=.5,effectsVolume=.7,mouseSensitivity=1,
                cameraShake=true,flashes=true,vertexSnap=true,dithering=true,
                language=language,renderHeight=360,runMinutes=10 };
        }
    }

    [Serializable] public sealed class MetaDto
    {
        public int coins;
        public string selected,lastRun;
        public string[] characters,weapons,items,completed;
        public ExtraLevelsDto extras;
        public MissionProgressDto missions;
    }
    [Serializable] public sealed class ExtraLevelsDto
    {
        public int rerolls,skips,banishes;
        public int Get(string action)
        {
            switch(action){case "rerolls":return rerolls;case "skips":return skips;case "banishes":return banishes;
                default:throw new ArgumentException("Acción desconocida",nameof(action));}
        }
        public void Set(string action,int value)
        {
            switch(action){case "rerolls":rerolls=value;break;case "skips":skips=value;break;case "banishes":banishes=value;break;
                default:throw new ArgumentException("Acción desconocida",nameof(action));}
        }
    }
    [Serializable] public sealed class MissionProgressDto
    {
        public int first,kills,chests,shrines,challenge,level,victory,noLife;
        public int Get(string id)
        {
            switch(id){case "first":return first;case "kills":return kills;case "chests":return chests;
                case "shrines":return shrines;case "challenge":return challenge;case "level":return level;
                case "victory":return victory;case "noLife":return noLife;
                default:throw new ArgumentException("Misión desconocida",nameof(id));}
        }
        public void Set(string id,int value)
        {
            switch(id){case "first":first=value;break;case "kills":kills=value;break;case "chests":chests=value;break;
                case "shrines":shrines=value;break;case "challenge":challenge=value;break;case "level":level=value;break;
                case "victory":victory=value;break;case "noLife":noLife=value;break;
                default:throw new ArgumentException("Misión desconocida",nameof(id));}
        }
    }
    [Serializable] public sealed class MetaRun
    {
        public string id;
        public bool cheated,victory,usedLifeTome;
        public double time,kills,chests,shrines,challenges,level;
    }
    [Serializable] public sealed class MetaReceipt
    {
        public int kills,survival,victory,missions,total;
        public string[] completed=Array.Empty<string>();
    }
}
