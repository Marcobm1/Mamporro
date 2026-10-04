using System;
using System.Collections.Generic;

namespace Mamporro.Core.Progress
{
    public sealed class ValidationIssue
    {
        public string Path,Code,Severity,Action,Before,After;
    }
    public sealed class ProgressValidation
    {
        public ProgressDto Candidate {get;internal set;}
        readonly List<ValidationIssue> issues=new List<ValidationIssue>();
        public IReadOnlyList<ValidationIssue> Issues=>issues.AsReadOnly();
        public bool CanConfirm=>Candidate!=null;
        public bool Recovered=>issues.Exists(i=>i.Severity=="recovery");
        internal void Add(string path,string code,string action,object before=null,object after=null)
        {
            string Brief(object value){string text=ProgressJson.Stringify(value);return text.Length>256?text.Substring(0,256)+"…":text;}
            issues.Add(new ValidationIssue {Path=string.IsNullOrEmpty(path)?"/":path,Code=code,Action=action,
                Severity=action=="reject"?"fatal":action=="preserve"?"info":"recovery",Before=Brief(before),After=Brief(after)});
        }
    }

    // Separación obligatoria: todas estas funciones son puras respecto a IO.
    // El candidato solo se publica después de comprobar la meta completa.
    public static class ProgressValidator
    {
        public static ProgressValidation Import(string json,string language)=>Validate(()=>ProgressJson.Parse(json),language,false);
        public static ProgressValidation Import(byte[] json,string language)=>Validate(()=>ProgressJson.Parse(json),language,false);
        public static ProgressValidation Stored(string json,string language)=>Validate(()=>ProgressJson.Parse(json),language,true);
        public static ProgressValidation Stored(byte[] json,string language)=>Validate(()=>ProgressJson.Parse(json),language,true);
        static ProgressValidation Validate(Func<object> parse,string language,bool stored)
        {
            // Un idioma de contexto inválido es error de programación, no del archivo.
            SettingsDto.Default(language);var report=new ProgressValidation();
            try{
                var reader=new Reader(report,language);report.Candidate=reader.Read(parse(),stored);
                var tree=ProgressTree.From(report.Candidate);reader.Preserved(tree,"");
            }catch(ProgressJsonException error){report.Candidate=null;report.Add(error.Path,error.Code,"reject");}
            return report;
        }
        sealed class Reader
        {
            readonly ProgressValidation report;readonly string language;
            public Reader(ProgressValidation report,string language){this.report=report;this.language=language;}
            static void Fail(string path,string code="value.invalid"){throw new ProgressJsonException(path,code);}
            static Dictionary<string,object> Map(object raw,string path)
            {if(!(raw is Dictionary<string,object> map)){Fail(path);return null;}return map;}
            static object Required(Dictionary<string,object> map,string key,string path)
            {if(!map.TryGetValue(key,out var value))Fail(path+"/"+key,"progress.required");return value;}
            static int Integer(object raw,int max,string path)
            {if(raw is ProgressNumber number&&number.TryInteger(max,out int value))return value;Fail(path);return 0;}
            void Unknown(Dictionary<string,object> map,string path,params string[] known)
            {
                foreach(var entry in map)if(Array.IndexOf(known,entry.Key)<0)
                    report.Add(path+"/"+ProgressJson.Pointer(entry.Key),"field.unknown","exclude",entry.Value);
            }
            static string String(object raw,string path,int max)
            {if(!(raw is string text)){Fail(path);return null;}if(text.Length>max)Fail(path,max==ProgressJson.MaxId?"input.limit":"value.invalid");return text;}
            public ProgressDto Read(object raw,bool stored)
            {
                var root=raw as Dictionary<string,object>;if(root==null)Fail("/","input.invalid");
                Dictionary<string,object> progress;string prefix="";int webVersion=3;
                if(root.TryGetValue("format",out var format)){
                    string expected=stored?"mamporro.unity-save":"mamporro.progress";
                    if(!(format is string kind)||kind!=expected)Fail("/format","format.invalid");
                    if(!root.TryGetValue("version",out var version))Fail("/version","format.required");
                    if(!(version is ProgressNumber n)||!n.TryInteger(1,out int v)||v!=1)Fail("/version","format.invalid");
                    if(!stored){
                        if(!root.TryGetValue("source",out var source))Fail("/source","format.required");
                        var origin=Map(source,"/source");
                        if(!origin.TryGetValue("platform",out var platform)||!(platform is string p)||p!="web")Fail("/source/platform","format.invalid");
                        if(!origin.TryGetValue("saveVersion",out var sourceVersion)||!(sourceVersion is ProgressNumber sn)||!sn.TryInteger(3,out int sv)||sv<1)Fail("/source/saveVersion","format.invalid");
                        Unknown(origin,"/source","platform","saveVersion");
                    }
                    progress=Map(Required(root,"progress",""),"/progress");prefix="/progress";
                    Unknown(root,"",stored?new[]{"format","version","progress"}:new[]{"format","version","source","progress"});
                    Unknown(progress,prefix,"settings","meta");
                }else{
                    if(stored)Fail("/format","format.required");
                    if(!root.TryGetValue("version",out var version)||!(version is ProgressNumber n)||!n.TryInteger(3,out webVersion)||webVersion<1)Fail("/","input.invalid");
                    progress=root;Unknown(root,"","version","settings","meta");
                    if(webVersion==1&&root.ContainsKey("meta"))Fail("/meta","version.conflict");
                    if(webVersion<3)report.Add("/version","web.migrate","migrate",webVersion,3);
                }
                var settings=Settings(progress,prefix);
                MetaDto meta;
                if(webVersion==1){meta=MetaRules.New();report.Add("/meta","web.initial-meta","migrate",null,ProgressTree.Meta(meta));}
                else meta=Meta(Required(progress,"meta",prefix),prefix+"/meta");
                return new ProgressDto {settings=settings,meta=meta};
            }
            SettingsDto Settings(Dictionary<string,object> progress,string prefix)
            {
                var defaults=SettingsDto.Default(language);
                if(!progress.TryGetValue("settings",out var raw)||!(raw is Dictionary<string,object> map)){
                    report.Add(prefix+"/settings","option.default","default",raw,ProgressTree.Settings(defaults));return defaults;
                }
                var expected=ProgressTree.Settings(defaults);Unknown(map,prefix+"/settings",new List<string>(expected.Keys).ToArray());
                var result=new Dictionary<string,object>(expected,StringComparer.Ordinal);
                foreach(var pair in expected){
                    string key=pair.Key;bool present=map.TryGetValue(key,out var value),valid=false;
                    if(pair.Value is bool)valid=value is bool;
                    else if(key=="language")valid=value is string lang&&(lang=="es"||lang=="en");
                    else if(value is ProgressNumber number){
                        if(key=="renderHeight")valid=number.TryInteger(480,out int height)&&(height==240||height==360||height==480);
                        else if(key=="runMinutes")valid=number.TryInteger(15,out int minutes)&&(minutes==5||minutes==10||minutes==15);
                        else valid=number.Value>=(key=="mouseSensitivity" ? .2 : 0)&&number.Value<=(key=="mouseSensitivity"?3:1);
                    }
                    if(present&&valid)result[key]=value is ProgressNumber numeric?(object)numeric.Value:value;
                    else report.Add(prefix+"/settings/"+key,"option.default","default",value,pair.Value);
                }
                return new SettingsDto { language=(string)result["language"],musicVolume=Convert.ToDouble(result["musicVolume"]),
                    effectsVolume=Convert.ToDouble(result["effectsVolume"]),mouseSensitivity=Convert.ToDouble(result["mouseSensitivity"]),
                    renderHeight=Convert.ToInt32(result["renderHeight"]),runMinutes=Convert.ToInt32(result["runMinutes"]),
                    muted=(bool)result["muted"],reducedParticles=(bool)result["reducedParticles"],cameraShake=(bool)result["cameraShake"],
                    flashes=(bool)result["flashes"],vertexSnap=(bool)result["vertexSnap"],dithering=(bool)result["dithering"],
                    slideWithCtrl=(bool)result["slideWithCtrl"],showFps=(bool)result["showFps"] };
            }
            string[] Ids(object raw,string[] known,string path)
            {
                if(!(raw is List<object> values)){Fail(path);return null;}
                var accepted=new HashSet<string>(StringComparer.Ordinal);
                for(int i=0;i<values.Count;i++){
                    string child=path+"/"+i,id=String(values[i],child,ProgressJson.MaxId);
                    if(Array.IndexOf(known,id)<0)report.Add(child,"id.unknown","exclude",id);
                    else if(!accepted.Add(id))report.Add(child,"id.duplicate","exclude",id);
                }
                var result=new List<string>();foreach(var id in known)if(accepted.Contains(id))result.Add(id);return result.ToArray();
            }
            MetaDto Meta(object raw,string path)
            {
                var map=Map(raw,path);Unknown(map,path,"coins","selected","characters","weapons","items","extras","missions","completed","lastRun");
                var meta=new MetaDto {coins=Integer(Required(map,"coins",path),MetaRules.MaxCoins,path+"/coins"),
                    selected=String(Required(map,"selected",path),path+"/selected",ProgressJson.MaxId),
                    lastRun=String(Required(map,"lastRun",path),path+"/lastRun",100),extras=new ExtraLevelsDto(),missions=new MissionProgressDto()};
                meta.characters=Ids(Required(map,"characters",path),new[]{"remedios","baguette"},path+"/characters");
                meta.weapons=Ids(Required(map,"weapons",path),Array.ConvertAll(Catalog.Weapons,d=>d.id),path+"/weapons");
                meta.items=Ids(Required(map,"items",path),Array.ConvertAll(Catalog.Items,d=>d.id),path+"/items");
                var missionIds=new List<string>();foreach(var def in MetaRules.Missions)missionIds.Add(def.Id);
                meta.completed=Ids(Required(map,"completed",path),missionIds.ToArray(),path+"/completed");
                var initial=MetaRules.New();
                void Includes(string[] values,string[] required,string key){foreach(var id in required)if(Array.IndexOf(values,id)<0)Fail(path+"/"+key);}
                Includes(meta.characters,initial.characters,"characters");Includes(meta.weapons,initial.weapons,"weapons");Includes(meta.items,initial.items,"items");
                if(Array.IndexOf(meta.characters,meta.selected)<0)Fail(path+"/selected");
                var extras=Map(Required(map,"extras",path),path+"/extras");Unknown(extras,path+"/extras","rerolls","skips","banishes");
                foreach(var action in new[]{"rerolls","skips","banishes"})meta.extras.Set(action,Integer(Required(extras,action,path+"/extras"),3,path+"/extras/"+action));
                var missions=Map(Required(map,"missions",path),path+"/missions");Unknown(missions,path+"/missions",missionIds.ToArray());
                foreach(var def in MetaRules.Missions){
                    int value=Integer(Required(missions,def.Id,path+"/missions"),def.Target,path+"/missions/"+def.Id);meta.missions.Set(def.Id,value);
                    bool completed=Array.IndexOf(meta.completed,def.Id)>=0;
                    if(completed&&value!=def.Target)Fail(path+"/missions/"+def.Id,"mission.conflict");
                    if(!completed&&value==def.Target)Fail(path+"/completed","mission.conflict");
                    if(completed&&def.UnlockId!=null&&!MetaRules.Owns(meta,def.UnlockKind,def.UnlockId))
                        Fail(path+(def.UnlockKind=="weapon"?"/weapons":"/items"),"unlock.conflict");
                }
                return meta;
            }
            public void Preserved(Dictionary<string,object> map,string path)
            {
                foreach(var pair in map){string child=path+"/"+pair.Key;
                    if(pair.Value is Dictionary<string,object> nested){Preserved(nested,child);continue;}
                    bool changed=false;foreach(var issue in report.Issues){
                        string normalized=issue.Path.StartsWith("/progress/",StringComparison.Ordinal)?issue.Path.Substring(9):issue.Path;
                        if(normalized==child||normalized.StartsWith(child+"/",StringComparison.Ordinal)||child.StartsWith(normalized+"/",StringComparison.Ordinal)){changed=true;break;}
                    }
                    if(!changed)report.Add(child,"value.preserved","preserve",pair.Value,pair.Value);
                }
            }
        }
    }

    public static class ProgressTree
    {
        public static Dictionary<string,object> Object(params object[] pairs)
        {var result=new Dictionary<string,object>(StringComparer.Ordinal);for(int i=0;i<pairs.Length;i+=2)result.Add((string)pairs[i],pairs[i+1]);return result;}
        public static Dictionary<string,object> Settings(SettingsDto s)=>Object(
            "musicVolume",s.musicVolume,"effectsVolume",s.effectsVolume,"muted",s.muted,"reducedParticles",s.reducedParticles,
            "cameraShake",s.cameraShake,"flashes",s.flashes,"language",s.language,"mouseSensitivity",s.mouseSensitivity,
            "renderHeight",s.renderHeight,"vertexSnap",s.vertexSnap,"dithering",s.dithering,"slideWithCtrl",s.slideWithCtrl,"showFps",s.showFps,"runMinutes",s.runMinutes);
        public static Dictionary<string,object> Meta(MetaDto m)
        {
            var missions=new Dictionary<string,object>(StringComparer.Ordinal);foreach(var def in MetaRules.Missions)missions.Add(def.Id,m.missions.Get(def.Id));
            return Object("coins",m.coins,"selected",m.selected,"characters",m.characters,"weapons",m.weapons,"items",m.items,
                "extras",Object("rerolls",m.extras.rerolls,"skips",m.extras.skips,"banishes",m.extras.banishes),
                "missions",missions,"completed",m.completed,"lastRun",m.lastRun);
        }
        public static Dictionary<string,object> From(ProgressDto progress)=>Object("settings",Settings(progress.settings),"meta",Meta(progress.meta));
        public static string Stored(ProgressDto progress)=>ProgressJson.Stringify(Object("format","mamporro.unity-save","version",1,"progress",From(progress)));
    }
}
