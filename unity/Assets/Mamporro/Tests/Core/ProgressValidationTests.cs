using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core.Progress;

namespace Mamporro.Tests
{
    public sealed class ProgressValidationTests
    {
        static Dictionary<string,object> Map(object value)=>(Dictionary<string,object>)value;
        static List<object> List(object value)=>(List<object>)value;
        static string Text(object value)=>(string)value;
        static object Clone(object value)=>ProgressJson.Parse(ProgressJson.Stringify(value));
        static Dictionary<string,object> Read(string file)=>Map(ProgressJson.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/"+file))));
        static Dictionary<string,object> Reference=>Read("u4-progress.json");
        static Dictionary<string,object> Save(string id)
        {
            foreach(var entry in List(Reference["saves"]))if(Text(Map(entry)["id"])==id)return Map(entry);
            throw new Exception("Caso web inexistente");
        }
        static Dictionary<string,object> Expected(string id)
        {
            var data=Map(Map(Save(id)["result"])["data"]);
            return ProgressTree.Object("settings",data["settings"],"meta",data["meta"]);
        }
        static void Patch(Dictionary<string,object> tree,List<object> edits)
        {
            foreach(var raw in edits){
                var edit=Map(raw);var parts=Text(edit["path"]).Split('/');var parent=tree;
                for(int i=1;i<parts.Length-1;i++)parent=Map(parent[parts[i]]);
                string key=parts[parts.Length-1];
                if(Text(edit["op"])=="remove")Assert.That(parent.Remove(key),Is.True);
                else parent[key]=Clone(edit["value"]);
            }
        }
        static object Input(Dictionary<string,object> input)
        {
            if(input.ContainsKey("raw"))return input["raw"];
            if(input.ContainsKey("recipe"))return Recipe(Map(input["recipe"]));
            string id=Text(input["base"]),encoding=Text(input["encoding"]);Dictionary<string,object> root;
            if(encoding=="web")root=Map(ProgressJson.Parse(Text(Save(id)["raw"])));
            else if(encoding=="transfer")root=ProgressTree.Object("format","mamporro.progress","version",1,
                "source",ProgressTree.Object("platform","web","saveVersion",3),"progress",Expected(id));
            else root=ProgressTree.Object("format","mamporro.unity-save","version",1,"progress",Expected(id));
            Patch(root,List(input["edits"]));return ProgressJson.Stringify(root);
        }
        static object Recipe(Dictionary<string,object> recipe)
        {
            string kind=Text(recipe["kind"]);
            int amount=recipe.ContainsKey("amount")?(int)((ProgressNumber)recipe["amount"]).Value:0;
            switch(kind){
                case "no-local-save":return null;
                case "repeat-transfer":return ProgressJson.Stringify(ProgressTree.Object("format","mamporro.progress","version",1,
                    "source",ProgressTree.Object("platform","web","saveVersion",3),"progress",Expected(Text(recipe["base"]))));
                case "utf8-bom":return "\ufeff"+Text(Save(Text(recipe["base"]))["raw"]);
                case "invalid-utf8":return new byte[]{255};
                case "utf8-bytes":return new string(' ',amount);
                case "depth":return new string('[',amount)+"0"+new string(']',amount);
                case "nodes":
                    var outer=new List<object>();for(int i=0;i<33;i++){var inner=new List<object>();for(int j=0;j<128;j++)inner.Add(0);outer.Add(inner);}return ProgressJson.Stringify(outer);
                case "object-properties":
                    var map=new Dictionary<string,object>();for(int i=0;i<amount;i++)map.Add("k"+i,0);return ProgressJson.Stringify(map);
                case "array-elements":
                    var array=new List<object>();for(int i=0;i<amount;i++)array.Add(0);return ProgressJson.Stringify(array);
                case "string-units":return "\""+new string('x',amount)+"\"";
                case "id-units":
                    var save=Map(ProgressJson.Parse(Text(Save("v3-partial")["raw"])));
                    List(Map(save["meta"])["characters"]).Add(new string('x',amount));return ProgressJson.Stringify(save);
                case "numeric-token":
                    var numeric=Map(ProgressJson.Parse(Text(Save(Text(recipe["base"]))["raw"])));
                    Map(numeric["meta"])["coins"]="__NUMBER__";return ProgressJson.Stringify(numeric).Replace("\"__NUMBER__\"",Text(recipe["token"]));
                default:throw new Exception("Receta desconocida: "+kind);
            }
        }
        static void EqualTree(object actual,object expected,string path="")
        {
            if(expected is ProgressNumber en){Assert.That(actual,Is.TypeOf<ProgressNumber>(),path);Assert.That(((ProgressNumber)actual).Value,Is.EqualTo(en.Value),path);return;}
            if(expected is Dictionary<string,object> em){
                var am=Map(actual);CollectionAssert.AreEquivalent(em.Keys,am.Keys,path);
                foreach(var pair in em)EqualTree(am[pair.Key],pair.Value,path+"/"+pair.Key);return;
            }
            if(expected is List<object> el){var al=List(actual);Assert.That(al.Count,Is.EqualTo(el.Count),path);for(int i=0;i<el.Count;i++)EqualTree(al[i],el[i],path+"/"+i);return;}
            Assert.That(actual,Is.EqualTo(expected),path);
        }
        public static IEnumerable Cases
        {
            get{foreach(var entry in List(Read("u4-import-cases.json")["cases"])){
                var item=Map(entry);yield return new TestCaseData(item).SetName("U4_Import_"+Text(item["id"]));
            }}
        }
        [TestCaseSource(nameof(Cases))]
        public void FrozenPolicyCorpus(Dictionary<string,object> item)
        {
            var input=Map(item["input"]);var expected=Map(item["expected"]);var raw=Input(input);
            if(raw==null){EqualTree(ProgressJson.Parse(ProgressJson.Stringify(ProgressTree.From(ProgressDto.New("es")))),Expected("new-es"));return;}
            bool stored=input.ContainsKey("encoding")&&Text(input["encoding"])=="unity";
            string language=Text(item["language"]);
            var result=raw is byte[] bytes?ProgressValidator.Import(bytes,language):stored?ProgressValidator.Stored((string)raw,language):ProgressValidator.Import((string)raw,language);
            string outcome=result.CanConfirm?(result.Recovered?"recovery":"valid"):"fatal";
            Assert.That(outcome,Is.EqualTo(Text(expected["outcome"])),Describe(result));
            foreach(var issueRaw in List(expected["issues"])){
                var issue=Map(issueRaw);bool found=false;
                foreach(var actual in result.Issues){
                    string path=Text(issue["path"]);
                    if(actual.Code==Text(issue["code"])&&actual.Action==Text(issue["action"])&&(path=="/"||actual.Path==path||actual.Path.StartsWith(path+"/",StringComparison.Ordinal)))found=true;
                }
                Assert.That(found,Is.True,Text(issue["code"])+" "+Text(issue["path"])+"; "+Describe(result));
            }
            if(!result.CanConfirm){Assert.That(result.Candidate,Is.Null);return;}
            var candidate=Map(expected["candidate"]);var tree=Expected(Text(candidate["base"]));Patch(tree,List(candidate["edits"]));
            EqualTree(ProgressJson.Parse(ProgressJson.Stringify(ProgressTree.From(result.Candidate))),tree);
            if(Text(item["id"])=="import-repeat"){
                var again=ProgressValidator.Import((string)raw,language);
                Assert.That(ProgressTree.Stored(again.Candidate),Is.EqualTo(ProgressTree.Stored(result.Candidate)));
            }
        }
        static string Describe(ProgressValidation result)
        {var text="";foreach(var issue in result.Issues)if(issue.Action!="preserve")text+=issue.Path+":"+issue.Code+"; ";return text;}

        [TestCase("1.00000000000000001")][TestCase("1e-99999")][TestCase("9007199254740993")]
        public void RoundedCriticalNumbersAreNotAcceptedAsIntegers(string token)
        {
            var raw=Map(ProgressJson.Parse(Text(Save("v3-partial")["raw"])));Map(raw["meta"])["coins"]="__NUMBER__";
            var result=ProgressValidator.Import(ProgressJson.Stringify(raw).Replace("\"__NUMBER__\"",token),"es");
            Assert.That(result.CanConfirm,Is.False);Assert.That(result.Candidate,Is.Null);
        }
        [TestCase("0")][TestCase("1.0")][TestCase("100e-2")][TestCase("1e9")]
        public void ExactIntegerTokensRemainValid(string token)
        {Assert.That(((ProgressNumber)ProgressJson.Parse(token)).TryInteger(MetaRules.MaxCoins,out _),Is.True);}

        [Test] public void ParserBoundariesEscapesAndCultureAreDeterministic()
        {
            string raw=Text(Save("v3-partial")["raw"]);string padded=raw+new string(' ',ProgressJson.MaxBytes-System.Text.Encoding.UTF8.GetByteCount(raw));
            Assert.That(ProgressValidator.Import(padded,"es").CanConfirm,Is.True);
            Assert.Throws<ProgressJsonException>(()=>ProgressJson.Parse(padded+" "));
            Assert.That(ProgressJson.Parse("\"\\uD83D\\uDE00\\n\""),Is.EqualTo("😀\n"));
            Assert.Throws<ProgressJsonException>(()=>ProgressJson.Parse("\"\\uD800\""));
            var culture=System.Threading.Thread.CurrentThread.CurrentCulture;
            try{
                System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("es-ES");
                string stored=ProgressTree.Stored(ProgressDto.New("es"));
                Assert.That(ProgressValidator.Stored(stored,"en").CanConfirm,Is.True);
                Assert.That(stored,Does.Contain("0.5"));
            }finally{System.Threading.Thread.CurrentThread.CurrentCulture=culture;}
        }
        [Test] public void ReportsPreserveValuesAndNeverModifySource()
        {
            string raw=Text(Save("v3-partial")["raw"]);var bytes=System.Text.Encoding.UTF8.GetBytes(raw);var copy=(byte[])bytes.Clone();
            var result=ProgressValidator.Import(bytes,"es");CollectionAssert.AreEqual(copy,bytes);
            Assert.That(result.CanConfirm,Is.True);Assert.That(result.Recovered,Is.False);
            Assert.That(result.Issues.Count,Is.GreaterThan(20));
            Assert.That(ProgressValidator.Import(ProgressTree.Stored(result.Candidate),"es").CanConfirm,Is.False);
            Assert.That(ProgressValidator.Stored(raw,"es").CanConfirm,Is.False);
        }
    }
}
