using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEditorInternal;

namespace Mamporro.Tests
{
    // H1e: controles conocidos, sin cambios en los constructores H1d/producción.
    public sealed class MemoryCertificationTests
    {
        static byte[] held;
        static long readSink;
        [DllImport("mono-2.0-bdwgc",CallingConvention=CallingConvention.Cdecl)]
        static extern IntPtr mono_lookup_internal_call(IntPtr method);
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessMemory { public uint cb,PageFaultCount; public UIntPtr PeakWorkingSet,WorkingSet,PeakPaged, Paged,PeakNonPaged,NonPaged,Pagefile,PeakPagefile,Private; }
        [DllImport("psapi.dll",SetLastError=true)]
        static extern bool GetProcessMemoryInfo(IntPtr handle,out ProcessMemory counters,uint size);
        internal static ProcessMemory ReadProcess() {
            if(!GetProcessMemoryInfo(new IntPtr(-1),out var m,(uint)Marshal.SizeOf<ProcessMemory>()))throw new System.ComponentModel.Win32Exception();return m;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Allocate(int bytes) { held=new byte[bytes]; for(int i=0;i<bytes;i+=4096)held[i]=123; }
        static void Collect() { GC.Collect();GC.WaitForPendingFinalizers();GC.Collect(); }
        static long Sum(ProfilerRecorder r) { long n=0;for(int i=0;i<r.Count;i++)n+=r.GetSample(i).Value;return n; }
        [Test,Category("MemoryCertificationA")]
        public void FrameAllocationCounterControl()
        {
            using(var r=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",16)) {
                foreach(int bytes in new[]{4096,1048576,16777216}) {
                    long before=r.CurrentValue;Allocate(bytes);long after=r.CurrentValue;
                    TestContext.Out.WriteLine($"H1E-FRAME bytes={bytes} valid={r.Valid} currentDeltaRaw={after-before} lastRaw={r.LastValue} samples={r.Count}");
                    Assert.That(r.Valid,Is.True);Assert.That(after-before,Is.EqualTo(bytes+32L));
                }
                using(var ready=new AutoResetEvent(false))using(var go=new AutoResetEvent(false))using(var done=new AutoResetEvent(false)) {
                    var t=new Thread(()=>{Allocate(4096);ready.Set();go.WaitOne();Allocate(1048576);done.Set();});t.Start();ready.WaitOne();
                    long before=r.CurrentValue;go.Set();done.WaitOne();long delta=r.CurrentValue-before;t.Join();
                    TestContext.Out.WriteLine($"H1E-FRAME-WORKER bytes=1048576 delta={delta}");Assert.That(delta,Is.InRange(1048608L,1048608L+4096));
                }
                long emptyBefore=r.CurrentValue;for(int i=0;i<1000;i++)readSink=r.CurrentValue;long empty=r.CurrentValue-emptyBefore;
                TestContext.Out.WriteLine($"H1E-FRAME-READS count=1000 allocated={empty}");
                long start=r.CurrentValue;for(int i=0;i<16;i++){Allocate(1048576);held=null;if(i==7)Collect();}long stress=r.CurrentValue-start;
                TestContext.Out.WriteLine($"H1E-FRAME-GC payload=16777216 arrays=16 allocated={stress}");Assert.That(stress,Is.InRange(16777728L,16777728L+4096));
            }
            held=null;Collect();
        }
        [Test,Category("MemoryCertificationA")]
        public void KnownManagedAllocations()
        {
            var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");
            TestContext.Out.WriteLine("H1E-RUNTIME version="+UnityEngine.Application.unityVersion+" gcIL="+BitConverter.ToString(method.GetMethodBody()?.GetILAsByteArray()??new byte[0]));
            IntPtr entry=mono_lookup_internal_call(method.MethodHandle.Value);var code=new byte[16];
            if(entry!=IntPtr.Zero){Marshal.Copy(entry,code,0,code.Length);TestContext.Out.WriteLine("H1E-ICALL bytes="+BitConverter.ToString(code));}
            Allocate(4096);held=null;Collect();
            foreach(int bytes in new[]{4096,1048576,16777216}) {
                using(var r=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",65536,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread)) {
                    long before=GC.GetAllocatedBytesForCurrentThread(), used=GC.GetTotalMemory(false),mono=Profiler.GetMonoUsedSizeLong();
                    Allocate(bytes);
                    long delta=GC.GetAllocatedBytesForCurrentThread()-before, live=GC.GetTotalMemory(false)-used, m=Profiler.GetMonoUsedSizeLong()-mono;
                    r.Stop();long sum=Sum(r);int count=r.Count;
                    TestContext.Out.WriteLine($"H1E-CAL bytes={bytes} threadRaw={delta} managedDelta={live} monoDelta={m} recorderValid={r.Valid} recorderCount={count} recorderSumRaw={sum} allocationBytes=ND");
                    Assert.That(held.Length,Is.EqualTo(bytes));Assert.That(held[bytes-4096],Is.EqualTo(123));
                    Assert.That(live,Is.InRange((long)bytes,bytes+65536L));Assert.That(m,Is.InRange((long)bytes,bytes+65536L));
                    held=null;Collect();
                }
            }
        }
        [Test,Category("MemoryCertificationA")]
        public void KnownNativeAndResidentAllocation()
        {
            const int bytes=32*1024*1024;
            using(var p=Process.GetCurrentProcess()) {
                p.Refresh();long ws=p.WorkingSet64,priv=p.PrivateMemorySize64,unity=Profiler.GetTotalAllocatedMemoryLong();var nativeBefore=ReadProcess();
                IntPtr block=Marshal.AllocHGlobal(bytes);
                try {
                    for(int i=0;i<bytes;i+=4096)Marshal.WriteByte(block,i,123);
                    p.Refresh();TestContext.Out.WriteLine($"H1E-NATIVE bytes={bytes} workingDelta={p.WorkingSet64-ws} privateDelta={p.PrivateMemorySize64-priv} unityDelta={Profiler.GetTotalAllocatedMemoryLong()-unity}");
                    var nativeAfter=ReadProcess();TestContext.Out.WriteLine($"H1E-WIN32 bytes={bytes} workingDelta={(long)nativeAfter.WorkingSet.ToUInt64()-(long)nativeBefore.WorkingSet.ToUInt64()} privateDelta={(long)nativeAfter.Private.ToUInt64()-(long)nativeBefore.Private.ToUInt64()}");
                    Assert.That((long)nativeAfter.WorkingSet.ToUInt64()-(long)nativeBefore.WorkingSet.ToUInt64(),Is.InRange(bytes-1048576L,bytes+1048576L));
                    Assert.That(Marshal.ReadByte(block,bytes-4096),Is.EqualTo(123));
                } finally {Marshal.FreeHGlobal(block);}
            }
        }
        [Test,Category("MemoryCertificationA")]
        public void RecorderAcrossWorkerThread()
        {
            using(var r=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",65536,ProfilerRecorderOptions.SumAllSamplesInFrame)) {
                var t=new Thread(()=>Allocate(1048576));t.Start();t.Join();r.Stop();
                TestContext.Out.WriteLine($"H1E-WORKER bytes=1048576 recorderValid={r.Valid} recorderCount={r.Count} recorderSum={Sum(r)}");
                Assert.That(held.Length,Is.EqualTo(1048576));held=null;Collect();
            }
        }
        [UnityTest,Category("MemoryCertificationA")]
        public IEnumerator RawAllocationMetadata()
        {
            bool enabled=Profiler.enabled,editor=ProfilerDriver.profileEditor,driver=ProfilerDriver.enabled,cpu=Profiler.GetAreaEnabled(ProfilerArea.CPU);
            try {
                ProfilerDriver.profileEditor=true;ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU,true);ProfilerDriver.enabled=true;Profiler.enabled=true;
                yield return null;yield return null;
                int first=ProfilerDriver.lastFrameIndex;
                Profiler.BeginSample("H1eKnownMain");Allocate(1048576);Profiler.EndSample();
                var t=new Thread(()=> {Profiler.BeginThreadProfiling("H1e","KnownWorker");Profiler.BeginSample("H1eKnownWorker");Allocate(16777216);Profiler.EndSample();Profiler.EndThreadProfiling();});t.Start();t.Join();
                yield return null;yield return null;
                long main=0,worker=0;int found=0;
                for(int f=Math.Max(first,ProfilerDriver.firstFrameIndex);f<=ProfilerDriver.lastFrameIndex;f++)for(int ti=0;ti<128;ti++) {
                    using(var data=ProfilerDriver.GetRawFrameDataView(f,ti)) {
                        if(!data.valid)break;
                        for(int s=0;s<data.sampleCount;s++) {
                            string name=data.GetSampleName(s);if(name!="H1eKnownMain"&&name!="H1eKnownWorker")continue;
                            found++;int end=s+data.GetSampleChildrenCountRecursive(s);
                            for(int a=s+1;a<=end;a++)if(data.GetSampleName(a)=="GC.Alloc"&&data.GetSampleMetadataCount(a)>0) {
                                long bytes=data.GetSampleMetadataAsLong(a,0);if(name=="H1eKnownMain")main+=bytes;else worker+=bytes;
                            }
                        }
                    }
                }
                TestContext.Out.WriteLine($"H1E-RAW markers={found} mainRaw={main} workerRaw={worker} first={first} last={ProfilerDriver.lastFrameIndex} driver={ProfilerDriver.enabled} allocationBytes=ND");
            } finally {Profiler.enabled=enabled;ProfilerDriver.enabled=driver;ProfilerDriver.profileEditor=editor;ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU,cpu);held=null;}
        }
        [Test,Category("MemoryCertificationA")]
        public void KnownUnityNativeAllocationAndCollection()
        {
            const int bytes=16777216;long before=Profiler.GetTotalAllocatedMemoryLong();
            var a=new Unity.Collections.NativeArray<byte>(bytes,Unity.Collections.Allocator.Persistent);
            try {
                a[bytes-1]=123;long delta=Profiler.GetTotalAllocatedMemoryLong()-before;
                TestContext.Out.WriteLine($"H1E-UNITY bytes={bytes} delta={delta}");Assert.That(a[bytes-1],Is.EqualTo(123));
                Assert.That(delta,Is.InRange((long)bytes,bytes+1048576L));
            } finally {a.Dispose();}
            Allocate(bytes);long live=GC.GetTotalMemory(false);held=null;int gc=GC.CollectionCount(0);Collect();long after=GC.GetTotalMemory(false);
            TestContext.Out.WriteLine($"H1E-RELEASE bytes={bytes} released={live-after} gcDelta={GC.CollectionCount(0)-gc}");
            Assert.That(live-after,Is.InRange(bytes-65536L,bytes+65536L));Assert.That(GC.CollectionCount(0)-gc,Is.GreaterThanOrEqualTo(2));
        }
    }
}
