using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Mamporro.U2
{
    public static class CombatText
    {
        [Serializable] public class Entry {public string key,es,en;}
        [Serializable] public class Data {public Entry[] entries;}
        static readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        static readonly Regex Placeholder=new Regex(@"\{(\w+)\}");
        public static bool English;
        public static void Load()
        {
            if(entries.Count>0)return;
            // U4Text (textos del importador U4) es opcional: las escenas U1–U2 no lo necesitan.
            foreach(string file in new[]{"WebText","U2Text","U4Text"}){var asset=Resources.Load<TextAsset>(file);if(asset==null&&file=="U4Text")continue;foreach(var e in JsonUtility.FromJson<Data>(asset.text).entries)entries[e.key]=e;}
        }
        public static string Get(string key){Load();return entries.TryGetValue(key,out var e)?English?e.en:e.es:key;}
        // Como format() de la web: sustituye {nombre}; los nombres que no se pasan se dejan tal cual.
        // Parámetros en pares nombre, valor: Format("levelup.level","from",1,"to",2).
        public static string Format(string key,params object[] pairs)
        {
            var template=Get(key);if(pairs.Length==0)return template;
            return Placeholder.Replace(template,m=>{for(int k=0;k+1<pairs.Length;k+=2)if((string)pairs[k]==m.Groups[1].Value)return Convert.ToString(pairs[k+1],CultureInfo.InvariantCulture);return m.Value;});
        }
        // Como formatNumber() de la web: separador decimal del idioma, sin agrupar miles.
        public static string Number(double value,int maxDecimals=1)
        {
            double rounded=Math.Round(value,maxDecimals,MidpointRounding.AwayFromZero);
            string text=rounded.ToString(maxDecimals>0?"0."+new string('#',maxDecimals):"0",CultureInfo.InvariantCulture);
            return English?text:text.Replace('.',',');
        }
        public static string Capitalize(string text)=>string.IsNullOrEmpty(text)?text:char.ToUpperInvariant(text[0])+text.Substring(1);
    }
}
