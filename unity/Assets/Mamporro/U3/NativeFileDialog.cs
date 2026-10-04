using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Mamporro.U3
{
    // Diálogo «Abrir archivo» del sistema en Windows (comdlg32). Unity no ofrece uno en tiempo
    // de ejecución. Es modal y solo se abre al pulsar «Examinar…»; nunca en pruebas automáticas.
    // Devuelve null si se cancela o si la plataforma no lo admite (se puede escribir la ruta).
    public static class NativeFileDialog
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        sealed class OpenFileName
        {
            public int structSize;public IntPtr owner,instance;public string filter,customFilter;public int maxCustomFilter,filterIndex;
            public IntPtr file;public int maxFile;public string fileTitle;public int maxFileTitle;public string initialDir,title;public int flags;
            public short fileOffset,fileExtension;public string defaultExtension;public IntPtr customData,hook;public string templateName;
            public IntPtr reservedPointer;public int reservedInt,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool GetOpenFileNameW([In,Out] OpenFileName dialog);
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        const int FileMustExist=0x1000,PathMustExist=0x800,NoChangeDir=0x8,Explorer=0x80000,Capacity=1024;
#endif
        public static string OpenJson(string title,string initialPath)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IntPtr buffer=Marshal.AllocHGlobal(Capacity*2);
            try{
                var zeros=new byte[Capacity*2];Marshal.Copy(zeros,0,buffer,zeros.Length);
                string directory=null;
                try{directory=Path.GetDirectoryName(initialPath);if(!Directory.Exists(directory))directory=null;}catch(Exception e)when(e is ArgumentException||e is PathTooLongException){directory=null;}
                var dialog=new OpenFileName{owner=GetActiveWindow(),filter="JSON (*.json)\0*.json\0\0",filterIndex=1,file=buffer,maxFile=Capacity,
                    initialDir=directory,title=title,flags=FileMustExist|PathMustExist|NoChangeDir|Explorer,defaultExtension="json"};
                dialog.structSize=Marshal.SizeOf(dialog);
                return GetOpenFileNameW(dialog)?Marshal.PtrToStringUni(buffer):null;
            }finally{Marshal.FreeHGlobal(buffer);}
#else
            return null;
#endif
        }
        // Ruta sugerida: Descargas del usuario con el nombre que propone la web.
        public static string SuggestedPath(string fileName)
            =>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads",fileName);
    }
}
