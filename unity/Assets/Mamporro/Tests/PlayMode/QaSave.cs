using System;
using System.IO;
using System.Linq;
using Mamporro.Core.Progress;
using Mamporro.Persistence;
using Mamporro.U3;
using NUnit.Framework;
using UnityEngine;

namespace Mamporro.Tests
{
    // Guardado aislado para las pruebas de escena: cada prueba usa una carpeta temporal propia
    // (U3Game.SaveDirectoryOverride) con un progreso conocido y, al terminar, se comprueba que
    // el guardado real del autor (Application.persistentDataPath/Progress) no ha cambiado.
    public sealed class QaSave
    {
        public readonly string Root,Directory;
        readonly string real,realState;
        static string TempRoot=>Path.GetFullPath(Path.GetTempPath());
        static string State(string dir)
            =>!System.IO.Directory.Exists(dir)?"(ausente)":string.Join("|",System.IO.Directory.GetFiles(dir).OrderBy(f=>f,StringComparer.Ordinal)
                .Select(f=>Path.GetFileName(f)+":"+new FileInfo(f).Length+":"+File.GetLastWriteTimeUtc(f).Ticks));

        // progress=null: sin guardado. Por defecto, QA con todo desbloqueado (catálogo completo).
        public static QaSave Use(ProgressDto progress)=>new QaSave(progress);
        public static QaSave UseAllUnlocked()=>new QaSave(AllUnlocked());
        QaSave(ProgressDto progress)
        {
            real=Path.Combine(Application.persistentDataPath,"Progress");realState=State(real);
            Root=Path.Combine(TempRoot,"Mamporro-U4-play-"+Guid.NewGuid().ToString("N"));Directory=Path.Combine(Root,"Progress");
            System.IO.Directory.CreateDirectory(Root);
            if(progress!=null){var store=new ProgressStore(new ProgressFiles(Directory),"es");
                Assert.That(store.Confirm(store.Prepare(progress,store.Load()),true).Success,Is.True,"progreso QA guardado");}
            U3Game.SaveDirectoryOverride=Directory;U3Game.DefaultLanguageOverride="es";
        }
        public ProgressDto Stored=>new ProgressStore(new ProgressFiles(Directory),"es").Load().Candidate;
        public void Dispose()
        {
            U3Game.SaveDirectoryOverride=null;U3Game.DefaultLanguageOverride=null;
            Assert.That(State(real),Is.EqualTo(realState),"el guardado real no se toca");
            string full=Path.GetFullPath(Root);
            Assert.That(Path.GetDirectoryName(full).TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(TempRoot.TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
            Assert.That(Path.GetFileName(full),Does.StartWith("Mamporro-U4-play-"));
            if(System.IO.Directory.Exists(full))System.IO.Directory.Delete(full,true);
        }
        public static ProgressDto AllUnlocked()
        {
            var p=ProgressDto.New("es");var m=p.meta;
            m.characters=new[]{"remedios","baguette"};m.weapons=new[]{"chancla","naftalina","barra","dentaduras","jersey","fregona"};
            m.items=new[]{"gafas","zapatillas","termo","cojin","lupa","loteria","rulos","perlas","monedero","baraja","olla","bata"};
            return p;
        }
    }
}
