using System;
using System.IO;
using Mamporro.Core.Progress;

namespace Mamporro.Persistence
{
    public interface IProgressFiles
    {
        IDisposable Lock();
        byte[] Read(string name);
        bool Exists(string name);
        void WriteNew(string name,byte[] data);
        void Publish(string source,string destination,bool replace);
        void DeleteTemporary(string name);
    }

    // El adaptador de aplicación pasará Path.Combine(Application.persistentDataPath,"Progress").
    // El núcleo recibe la ruta; las pruebas usan un directorio temporal propio.
    public sealed class ProgressFiles:IProgressFiles
    {
        public readonly string DirectoryPath;
        public ProgressFiles(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("Directorio requerido",nameof(directory));
            DirectoryPath=Path.GetFullPath(directory);
        }
        string FilePath(string name)
        {
            if(string.IsNullOrEmpty(name)||name=="."||name==".."||Path.GetFileName(name)!=name||name.IndexOfAny(new[]{'/', '\\', ':'})>=0)
                throw new ArgumentException("Se requiere un nombre local",nameof(name));
            return Path.Combine(DirectoryPath,name);
        }
        public IDisposable Lock()
        {
            Directory.CreateDirectory(DirectoryPath);
            return new FileStream(FilePath("progress.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        public bool Exists(string name)
        {
            try{
                var attributes=File.GetAttributes(FilePath(name));
                if((attributes&FileAttributes.Directory)!=0)throw new IOException("El guardado es un directorio");
                return true;
            }catch(FileNotFoundException){return false;}catch(DirectoryNotFoundException){return false;}
        }
        public byte[] Read(string name)
        {
            try{
                using(var stream=new FileStream(FilePath(name),FileMode.Open,FileAccess.Read,FileShare.Read)){
                    if(stream.Length>ProgressJson.MaxBytes)throw new IOException("El archivo supera el límite de lectura");
                    var data=new byte[(int)stream.Length];int offset=0;
                    while(offset<data.Length){int n=stream.Read(data,offset,data.Length-offset);if(n==0)throw new EndOfStreamException();offset+=n;}
                    if(stream.ReadByte()!=-1)throw new IOException("El archivo cambió durante la lectura");return data;
                }
            }catch(FileNotFoundException){return null;}catch(DirectoryNotFoundException){return null;}
        }
        public void WriteNew(string name,byte[] data)
        {
            if(data==null||data.Length>ProgressJson.MaxBytes)throw new IOException("Tamaño de guardado no admitido");
            using(var stream=new FileStream(FilePath(name),FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                stream.Write(data,0,data.Length);stream.Flush(true);
            }
        }
        public void Publish(string source,string destination,bool replace)
        {
            // No hay fallback borrar+renombrar: si Replace no está soportado, falla
            // conservando la copia verificada y se informa, sin prometer atomicidad universal.
            if(replace)File.Replace(FilePath(source),FilePath(destination),null);
            else File.Move(FilePath(source),FilePath(destination));
        }
        public void DeleteTemporary(string name)
        {
            if(!name.StartsWith("progress-",StringComparison.Ordinal)||!name.EndsWith(".tmp",StringComparison.Ordinal))
                throw new ArgumentException("Solo se eliminan temporales propios",nameof(name));
            File.Delete(FilePath(name));
        }
    }
}
