using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core.Progress;

namespace Mamporro.Tests
{
    public sealed class WebExportTests
    {
        [TestCase("new-es")][TestCase("new-en")][TestCase("v1-earliest")][TestCase("v1-options")]
        [TestCase("v2-partial")][TestCase("v3-partial")][TestCase("v3-missions")][TestCase("v3-all")]
        public void ActualWebExportMatchesFrozenProgress(string id)
        {
            string path=Path.Combine(Application.dataPath,"../TestResults/U4/Exports/"+id+".json");
            Assert.That(File.Exists(path),Is.True,"Ejecutar antes: node scripts/u4-export-fixtures.mjs");
            byte[] before=File.ReadAllBytes(path);
            var actual=ProgressValidator.Import(before,"es");
            Assert.That(actual.CanConfirm,Is.True);Assert.That(actual.Recovered,Is.False);
            var reference=(Dictionary<string,object>)ProgressJson.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/u4-progress.json")));
            Dictionary<string,object> expected=null;
            foreach(Dictionary<string,object> entry in (List<object>)reference["saves"])
                if((string)entry["id"]==id)expected=(Dictionary<string,object>)((Dictionary<string,object>)entry["result"])["data"];
            Assert.That(expected,Is.Not.Null);
            var progress=ProgressTree.Object("settings",expected["settings"],"meta",expected["meta"]);
            Assert.That(ProgressJson.Stringify(ProgressTree.From(actual.Candidate)),Is.EqualTo(ProgressJson.Stringify(progress)));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
        }
    }
}
