using System;
using NUnit.Framework;
using Mamporro.Core.Audio;

namespace Mamporro.Tests
{
    // U5 paso 2: compresor de la mezcla (aproximación del DynamicsCompressor del navegador).
    public sealed class MixCompressorTests
    {
        const int Rate=48000;
        static float[] Sine(double amplitude,int frames,int channels=2,double hz=440)
        {var data=new float[frames*channels];for(int i=0;i<frames;i++)for(int c=0;c<channels;c++)data[i*channels+c]=(float)(amplitude*Math.Sin(2*Math.PI*hz*i/Rate));return data;}
        static double Peak(float[] data,int from=0){double p=0;for(int i=from;i<data.Length;i++)p=Math.Max(p,Math.Abs(data[i]));return p;}

        [Test]public void StaticCurveFollowsThresholdKneeAndRatio()
        {
            Assert.That(MixCompressor.Curve(-60),Is.Zero,"por debajo de la rodilla no comprime");
            Assert.That(MixCompressor.Curve(-27.0001),Is.Zero);
            Assert.That(MixCompressor.Curve(-12),Is.EqualTo((1/8.0-1)*15*15/60).Within(1e-12),"en el umbral, mitad de la rodilla");
            Assert.That(MixCompressor.Curve(10),Is.EqualTo(22/8.0-22).Within(1e-12),"por encima de la rodilla, relación 8");
            double last=0;for(double db=-40;db<=12;db+=.5){double r=MixCompressor.Curve(db);Assert.That(r,Is.LessThanOrEqualTo(last+1e-12),"monótona");last=r;}
        }
        [Test]public void QuietSignalOnlyGetsTheFixedMakeupGain()
        {
            var c=new MixCompressor(Rate);Assert.That(c.Makeup,Is.GreaterThan(1).And.LessThan(3),"compensación del navegador (≈ +6 dB)");
            var data=Sine(.005,4800);var input=(float[])data.Clone();c.Process(data,2,Rate);
            for(int i=0;i<data.Length;i++)Assert.That(data[i],Is.EqualTo(input[i]*c.Makeup).Within(1e-6));
        }
        [Test]public void LoudSignalIsCompressedNeverClipsAndSettles()
        {
            var c=new MixCompressor(Rate);var data=Sine(1,Rate);c.Process(data,2,Rate);
            Assert.That(Peak(data),Is.LessThanOrEqualTo(1));
            Assert.That(c.Reduction,Is.EqualTo(MixCompressor.Curve(0)).Within(.6),"tras 1 s, reducción estable de la curva");
            var hot=Sine(4,Rate/10);c.Process(hot,2,Rate);Assert.That(Peak(hot),Is.LessThanOrEqualTo(1),"tope duro");
        }
        [Test]public void AttackIsFastAndReleaseTakesAboutAQuarterSecond()
        {
            var c=new MixCompressor(Rate);var loud=Sine(1,Rate/100);c.Process(loud,2,Rate);
            Assert.That(c.Reduction,Is.LessThan(MixCompressor.Curve(0)*.7),"en 10 ms ya la mayor parte de la reducción (ataque 3 ms)");
            var steady=Sine(1,Rate/2);c.Process(steady,2,Rate);double held=c.Reduction;
            var quiet=new float[Rate/4*2];c.Process(quiet,2,Rate);
            Assert.That(c.Reduction/held,Is.EqualTo(Math.Exp(-1)).Within(.02),"liberación de 250 ms (constante de tiempo)");
        }
        [Test]public void NonFiniteSamplesBecomeSilenceAndStateStaysFinite()
        {
            var c=new MixCompressor(Rate);var data=new float[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,.5f,1e30f,-1e30f};
            c.Process(data,2,Rate);
            foreach(float s in data){Assert.That(float.IsNaN(s)||float.IsInfinity(s),Is.False);Assert.That(Math.Abs(s),Is.LessThanOrEqualTo(1));}
            Assert.That(data[0],Is.Zero);Assert.That(data[1],Is.Zero);Assert.That(double.IsNaN(c.Reduction)||double.IsInfinity(c.Reduction),Is.False);
            c.Process(null,2,Rate);c.Process(new float[3],0,Rate);c.Process(new float[0],2,Rate);
        }
        [Test]public void ProcessingIsDeterministicAndDoesNotAllocate()
        {
            var a=Sine(.9,1024);var b=(float[])a.Clone();
            var c1=new MixCompressor(Rate);var c2=new MixCompressor(Rate);c1.Process(a,2,Rate);c2.Process(b,2,Rate);CollectionAssert.AreEqual(a,b);
            var block=Sine(.8,1024);var c=new MixCompressor(Rate);c.Process(block,2,Rate);
            long before=GC.GetTotalMemory(false);int collections=GC.CollectionCount(0);
            for(int i=0;i<5000;i++)c.Process(block,2,Rate);
            Assert.That(GC.CollectionCount(0),Is.EqualTo(collections),"sin recolecciones");Assert.That(GC.GetTotalMemory(false)-before,Is.LessThan(64*1024),"sin asignaciones por bloque");
        }
    }
}
