using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Mamporro.Core.Audio;
using Mamporro.Core.Progress;

namespace Mamporro.Tests
{
    // U5 paso 1: síntesis C# contra la referencia de audio congelada en U0 (baseline.json →
    // audio, generada con la web 0505b16) y presupuesto de voces (pruebas de audio.test.ts).
    // Tolerancia numérica: la referencia pide no usar hashes PCM como criterio.
    public sealed class AudioReferenceTests
    {
        static Dictionary<string,object> audio;
        static Dictionary<string,object> Audio()
        {
            if(audio!=null)return audio;
            string text=File.ReadAllText(Path.Combine(Application.dataPath,"../Docs/Reference/baseline.json"));
            int start=text.IndexOf("\"audio\": {",StringComparison.Ordinal);Assert.That(start,Is.GreaterThan(0),"sección audio");
            int open=text.IndexOf('{',start),depth=0,end=open;
            for(;end<text.Length;end++){if(text[end]=='{')depth++;else if(text[end]=='}'&&--depth==0)break;}
            return audio=(Dictionary<string,object>)ProgressJson.Parse(text.Substring(open,end-open+1));
        }
        static double Num(object value)=>((ProgressNumber)value).Value;
        static void Compare(string label,float[] samples,Dictionary<string,object> expected)
        {
            Assert.That(samples.Length,Is.EqualTo((int)Num(expected["frames"])),label+" muestras");
            double peak=0,sum=0;foreach(float s in samples){peak=Math.Max(peak,Math.Abs((double)s));sum+=(double)s*s;}
            Assert.That(peak,Is.EqualTo(Num(expected["peak"])).Within(1e-6),label+" pico");
            double sumRef=Num(expected["sumSquares"]);Assert.That(sum,Is.EqualTo(sumRef).Within(Math.Max(1e-9,sumRef*1e-6)),label+" energía");
            var first=(List<object>)expected["first32"];
            for(int i=0;i<first.Count;i++)Assert.That((double)samples[i],Is.EqualTo(Num(first[i])).Within(1e-7),label+" muestra "+i);
        }

        [Test]public void CatalogMatchesTheReferenceSoundsAndSampleRate()
        {
            Assert.That((int)Num(Audio()["sampleRate"]),Is.EqualTo(AudioCatalog.SampleRate));
            var sounds=(Dictionary<string,object>)Audio()["sounds"];
            CollectionAssert.AreEqual(sounds.Keys.ToArray(),AudioCatalog.Sounds.Select(s=>s.Id).ToArray(),"20 timbres en el mismo orden");
            for(int i=0;i<AudioCatalog.Sounds.Length;i++)Assert.That(AudioCatalog.Sounds[i].Index,Is.EqualTo(i));
        }
        [Test]public void TwentySoundsMatchTheFrozenWebPcm()
        {
            var sounds=(Dictionary<string,object>)Audio()["sounds"];
            foreach(var def in AudioCatalog.Sounds){
                var samples=AudioSynth.Sound(def);Compare(def.Id,samples,(Dictionary<string,object>)sounds[def.Id]);
                // Propiedades de audio.test.ts: finitos, acotados, bordes a cero, audibles y repetibles.
                Assert.That(samples[0],Is.Zero);Assert.That(samples[samples.Length-1],Is.Zero);
                double energy=0;foreach(float s in samples){Assert.That(float.IsNaN(s)||float.IsInfinity(s),Is.False);Assert.That(Math.Abs(s),Is.LessThanOrEqualTo(.25f));energy+=s*s;}
                Assert.That(energy,Is.GreaterThan(.01),def.Id);
                CollectionAssert.AreEqual(samples,AudioSynth.Sound(def),def.Id+" repetible");
            }
        }
        [Test]public void BothMusicArrangementsMatchTheFrozenWebPcm()
        {
            var normal=AudioSynth.Music(false);var intense=AudioSynth.Music(true);
            Compare("normal",normal,(Dictionary<string,object>)Audio()["normal"]);Compare("intensa",intense,(Dictionary<string,object>)Audio()["intense"]);
            Assert.That(normal.Length,Is.EqualTo(intense.Length),"mismo compás para cambiar sin perder la posición");
            CollectionAssert.AreNotEqual(normal,intense);
            foreach(var samples in new[]{normal,intense}){
                float peak=0;foreach(float s in samples){Assert.That(float.IsNaN(s)||float.IsInfinity(s),Is.False);peak=Math.Max(peak,Math.Abs(s));}
                Assert.That(peak,Is.GreaterThan(.1f));Assert.That(peak,Is.LessThan(.5f));
                Assert.That(samples[0],Is.Zero);Assert.That(samples[samples.Length-1],Is.Zero);
            }
            CollectionAssert.AreEqual(normal,AudioSynth.Music(false),"repetible");
        }

        // ------------------------------------------------------------ presupuesto de voces
        [Test]public void RepeatedHitsAreGroupedAndVoicesAreReleased()
        {
            var budget=new VoiceBudget();var hit=AudioCatalog.Get("hit");
            int key=budget.Claim(hit,0);Assert.That(key,Is.GreaterThan(0));
            for(int i=0;i<1000;i++)Assert.That(budget.Claim(hit,0),Is.Zero);
            Assert.That(budget.Claim(hit,1),Is.GreaterThan(0));Assert.That(budget.Count,Is.EqualTo(1),"la primera ya terminó");
            budget.Clear();Assert.That(budget.Count,Is.Zero);
            int next=budget.Claim(hit,1);Assert.That(next,Is.GreaterThan(0),"acepta tras limpiar");
            budget.Release(next);Assert.That(budget.Count,Is.Zero);
        }
        [Test]public void FourVoicesAreReservedAndNeverMoreThanSixteen()
        {
            var budget=new VoiceBudget();
            foreach(var def in AudioCatalog.Sounds)if(def.Priority==0)budget.Claim(def,0);
            Assert.That(budget.Count,Is.EqualTo(12));
            foreach(string id in new[]{"hurt","level","shield","boss"})Assert.That(budget.Claim(AudioCatalog.Get(id),0),Is.GreaterThan(0),id);
            Assert.That(budget.Count,Is.EqualTo(16));
            Assert.That(budget.Claim(AudioCatalog.Get("victory"),0),Is.Zero,"lleno: se descarta, no se aplaza");
            Assert.That(budget.Claim(AudioCatalog.Get("victory"),3),Is.GreaterThan(0));Assert.That(budget.Count,Is.EqualTo(1));
        }
        [Test]public void ExpiredVoicesFreeRoomAndCooldownIsPerSound()
        {
            var budget=new VoiceBudget();var xp=AudioCatalog.Get("xp");var gold=AudioCatalog.Get("gold");
            Assert.That(budget.Claim(xp,0),Is.GreaterThan(0));Assert.That(budget.Claim(gold,0),Is.GreaterThan(0),"otro timbre no comparte enfriamiento");
            Assert.That(budget.Claim(xp,.11),Is.Zero,"0,11 s < 0,12 s");Assert.That(budget.Claim(xp,.12),Is.GreaterThan(0));
            Assert.That(budget.Count,Is.EqualTo(1),"las voces anteriores (0,07 y 0,1 s) ya expiraron");
        }
    }
}
