using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mamporro.U2
{
    public static class CombatText
    {
        [Serializable] public class Entry {public string key,es,en;}
        [Serializable] public class Data {public Entry[] entries;}
        static readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        public static bool English;
        public static void Load()
        {
            if(entries.Count>0)return;
            foreach(string file in new[]{"WebText","U2Text"})foreach(var e in JsonUtility.FromJson<Data>(Resources.Load<TextAsset>(file).text).entries)entries[e.key]=e;
        }
        public static string Get(string key){Load();return entries.TryGetValue(key,out var e)?English?e.en:e.es:key;}
    }
}
