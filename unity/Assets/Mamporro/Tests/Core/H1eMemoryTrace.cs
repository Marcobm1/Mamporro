using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine.Profiling;
using Unity.Profiling;
using NUnit.Framework;

namespace Mamporro.Tests
{
    // Serie de observaciones de heap/proceso, nunca bytes atribuibles ni pico certificado.
    internal sealed class H1eMemoryTrace : IDisposable
    {
        internal struct Row { internal string Phase;internal long Start,End,Peak,Unity,Working,Private,Allocated;internal int GC;internal double Ms; }
        internal static H1eMemoryTrace Active;
        readonly Row[] rows=new Row[64];
        readonly Thread thread;
        volatile bool running=true;
        volatile int current=-1;
        int count;
        ProfilerRecorder allocated;
        long allocationStart;int frameSamples;
        long ticks;int collections;
        internal H1eMemoryTrace() { allocated=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",16);Active=this;thread=new Thread(Sample){IsBackground=true};thread.Start(); }
        void Sample() { while(running) {int at=current;if(at>=0) {long value=GC.GetTotalMemory(false);long old=Interlocked.Read(ref rows[at].Peak);if(value>old)Interlocked.Exchange(ref rows[at].Peak,value);}Thread.Sleep(1);} }
        internal static void Begin(string phase) {var a=Active;if(a==null)return;int n=a.count++;a.rows[n].Phase=phase;a.rows[n].Start=GC.GetTotalMemory(false);a.rows[n].Peak=a.rows[n].Start;a.collections=GC.CollectionCount(0);a.frameSamples=a.allocated.Count;a.allocationStart=a.allocated.CurrentValue;a.ticks=Stopwatch.GetTimestamp();a.current=n;}
        internal static void End() {var a=Active;if(a==null)return;int n=a.current;a.current=-1;a.rows[n].Allocated=a.allocated.CurrentValue-a.allocationStart;if(a.allocated.Count!=a.frameSamples||a.rows[n].Allocated<0)throw new InvalidOperationException("Frame cambiado durante fase");a.rows[n].Ms=(Stopwatch.GetTimestamp()-a.ticks)*1000.0/Stopwatch.Frequency;a.rows[n].End=GC.GetTotalMemory(false);a.rows[n].Peak=Math.Max(a.rows[n].Peak,a.rows[n].End);a.rows[n].GC=GC.CollectionCount(0)-a.collections;a.rows[n].Unity=Profiler.GetTotalAllocatedMemoryLong();var p=MemoryCertificationTests.ReadProcess();a.rows[n].Working=(long)p.WorkingSet.ToUInt64();a.rows[n].Private=(long)p.Private.ToUInt64();}
        internal void Report(string seed,int size,int trial) {for(int i=0;i<count;i++){var r=rows[i];TestContext.Out.WriteLine(FormattableString.Invariant($"H1E-PHASE seed={seed} size={size} trial={trial} phase={r.Phase} start={r.Start} end={r.End} peak={r.Peak} unity={r.Unity} working={r.Working} private={r.Private} gc={r.GC} ms={r.Ms} allocated={r.Allocated} attributable=ND"));}}
        public void Dispose() {if(!running)return;running=false;thread.Join();Active=null;allocated.Dispose();}
    }
}
