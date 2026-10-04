using System;
using NUnit.Framework;
using Mamporro.Core;
using Mamporro.Core.Effects;

namespace Mamporro.Tests
{
    // U5 paso 4: presupuesto (ParticleBudget.test.ts) y campo de partículas decorativas.
    public sealed class ParticleTests
    {
        static readonly uint[] Colors={0xff0000,0x00ff00,0x0000ff};

        [Test]public void BurstOfDeathsIsBoundedPerFrameAndByCapacity()
        {
            var budget=new ParticleBudget();int active=0;
            for(int frame=0;frame<30;frame++){
                budget.Reset();int emitted=0;
                for(int i=0;i<1000;i++){int n=budget.Take(12,active);active+=n;emitted+=n;}
                Assert.That(emitted,Is.LessThanOrEqualTo(256));Assert.That(active,Is.LessThanOrEqualTo(1500));
            }
            Assert.That(active,Is.EqualTo(1500));
        }
        [Test]public void ReducedDensityAndBudgetNeverAdmitWhenFull()
        {
            var budget=new ParticleBudget{Reduced=true};budget.Reset();
            Assert.That(budget.Take(12,0),Is.EqualTo(3));Assert.That(budget.Take(1000,3),Is.EqualTo(61));Assert.That(budget.Take(1,64),Is.Zero);
            budget.Reset();Assert.That(budget.Take(12,399),Is.EqualTo(1));Assert.That(budget.Take(12,500),Is.Zero);
            budget.Reset();Assert.That(budget.Take(1,0),Is.EqualTo(1),"⌈25 %⌉: una partícula sigue saliendo");
        }
        [Test]public void FieldRespectsBudgetsLifetimesAndReducedTrimming()
        {
            var f=new ParticleField(new Rng("efectos").Derive("particulas"));
            for(int frame=0;frame<10;frame++){f.Update(0);for(int i=0;i<100;i++)f.Burst(0,1,0,12,Colors,5,.14,10,2);Assert.That(f.Active,Is.LessThanOrEqualTo(1500));}
            Assert.That(f.Active,Is.EqualTo(1500),"lleno con el presupuesto normal");
            f.SetReduced(true);Assert.That(f.Active,Is.EqualTo(400),"reducir recorta al momento");
            f.Update(0);f.Burst(0,1,0,12,Colors,5,.14,10);Assert.That(f.Active,Is.EqualTo(400),"lleno: no se admite nada");
            f.Clear();f.Update(0);f.Burst(0,1,0,12,Colors,5,.14,.5);Assert.That(f.Active,Is.EqualTo(3));
            for(int i=0;i<f.Active;i++){Assert.That(f.Scale(i),Is.GreaterThan(0));Assert.That(Array.IndexOf(Colors,f.Color[i]),Is.GreaterThanOrEqualTo(0));}
            f.Update(.7f);Assert.That(f.Active,Is.Zero,"vida 0,5 × 0,7–1,2: todas terminan antes de 0,7 s");
        }
        [Test]public void MotionShrinkAndPauseFollowTheWeb()
        {
            var f=new ParticleField(new Rng("x"));f.Update(0);f.Burst(0,0,0,1,Colors,0,.2,1,0,10);
            float y0=f.Y[0];f.Update(0);Assert.That(f.Y[0],Is.EqualTo(y0),"dt 0 (pausa): no se mueve");
            f.Update(.1f);Assert.That(f.Y[0],Is.LessThan(y0),"cae con gravedad");
            float full=f.Size[0];Assert.That(f.Scale(0),Is.EqualTo(full),"tamaño completo al principio");
            f.Life[0]=f.MaxLife[0]*.2f;Assert.That(f.Scale(0),Is.EqualTo(full*.3f).Within(1e-5),"se encoge en el último tercio");
        }
        [Test]public void VisualRandomnessIsIndependentAndDeterministic()
        {
            var a=new ParticleField(new Rng("efectos").Derive("particulas"));var b=new ParticleField(new Rng("efectos").Derive("particulas"));
            a.Update(0);b.Update(0);a.Burst(1,2,3,20,Colors,4,.1,1);b.Burst(1,2,3,20,Colors,4,.1,1);
            for(int i=0;i<20;i++){Assert.That(a.X[i],Is.EqualTo(b.X[i]));Assert.That(a.Size[i],Is.EqualTo(b.Size[i]));}
            long before=GC.GetTotalMemory(false);for(int i=0;i<2000;i++){a.Update(.016f);a.Burst(0,0,0,12,Colors,5,.14,.7,2);}
            Assert.That(GC.GetTotalMemory(false)-before,Is.LessThan(64*1024),"sin asignaciones al emitir ni actualizar");
        }
    }
}
