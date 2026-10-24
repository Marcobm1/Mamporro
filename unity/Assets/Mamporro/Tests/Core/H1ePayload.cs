using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mamporro.Tests
{
    // Payload realmente respaldado por arrays retenidos; excluye cabeceras, objetos y residencia física.
    internal static class H1ePayload
    {
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        internal static int Stride(Type element)
        {
            var array=Array.CreateInstance(element,2);var pin=GCHandle.Alloc(array,GCHandleType.Pinned);
            try{return checked((int)(Marshal.UnsafeAddrOfPinnedArrayElement(array,1).ToInt64()-Marshal.UnsafeAddrOfPinnedArrayElement(array,0).ToInt64()));}
            finally{pin.Free();}
        }
        internal static long Count(object[] roots,out long listCapacityBytes)
        {
            long total=0,lists=0;var seen=new HashSet<Array>();
            foreach(object root in roots)foreach(var f in root.GetType().GetFields(Fields)) {
                object value=f.GetValue(root);
                if(value is Array a)total+=Add(a,seen);
                else if(value is IList list)foreach(var lf in list.GetType().GetFields(Fields))if(lf.GetValue(list) is Array backing) {
                    long bytes=Add(backing,seen);total+=bytes;lists+=bytes;
                }
            }
            listCapacityBytes=lists;return total;
        }
        static long Add(Array a,HashSet<Array> seen)
        {
            if(!seen.Add(a))return 0;Type element=a.GetType().GetElementType();
            if(element.IsPrimitive)return Buffer.ByteLength(a);
            if(!element.IsValueType)throw new InvalidOperationException("El censo no debe recorrer referencias compartidas: "+element);
            return a.LongLength*Stride(element);
        }
    }
}
