using System;
using System.IO;

namespace Mamporro.Persistence
{
    // Resolución pura: no abre archivos. Los overrides excluyen incluso la búsqueda histórica.
    public sealed class ProgressLocations
    {
        public string Current {get;private set;}
        public string Legacy {get;private set;}
        public static ProgressLocations Resolve(string persistentPath,bool windows,string[] args,string testOverride=null)
        {
            if(!string.IsNullOrEmpty(testOverride))return new ProgressLocations {Current=Path.GetFullPath(testOverride)};
            int index=Array.IndexOf(args,"-u4-save-dir");
            if(index>=0){
                if(index+1>=args.Length||string.IsNullOrWhiteSpace(args[index+1])||args[index+1].StartsWith("-",StringComparison.Ordinal))
                    throw new ArgumentException("-u4-save-dir requiere una carpeta; no se usará el progreso personal.");
                return new ProgressLocations {Current=Path.GetFullPath(args[index+1])};
            }
            var product=new DirectoryInfo(Path.GetFullPath(persistentPath));
            var result=new ProgressLocations {Current=Path.Combine(product.FullName,"Progress")};
            if(windows&&product.Name=="MAMPORRO"&&product.Parent?.Name=="Mamporro")
                result.Legacy=Path.Combine(product.Parent.FullName,"Mamporro U1","Progress");
            return result;
        }
    }
}
