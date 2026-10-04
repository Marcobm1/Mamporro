using System;
using NUnit.Framework;
using Mamporro.Core;
using Mamporro.Core.Effects;

namespace Mamporro.Tests
{
    // U5 paso 5: números de daño (DamageNumbers.ts).
    public sealed class DamageNumberTests
    {
        static DamageNumbers New()=>new DamageNumbers(new Rng("efectos").Derive("numeros"));

        [Test]public void TextFollowsTheWebRoundingCritMarksAndSixCharacters()
        {
            var n=New();
            n.Spawn(0,0,0,12.4,0);n.Spawn(0,0,0,36.5,1);n.Spawn(0,0,0,99.6,2);n.Spawn(0,0,0,.2,0);n.Spawn(0,0,0,1234567,0);n.Spawn(0,0,0,99999,1);n.Spawn(0,0,0,7,-1);
            Assert.That(n.Text(0),Is.EqualTo("12"));Assert.That(n.Text(1),Is.EqualTo("37!"),"Math.round: 36,5 → 37");Assert.That(n.Text(2),Is.EqualTo("100!!"));
            Assert.That(n.Text(3),Is.EqualTo("1"),"mínimo 1");Assert.That(n.Text(4),Is.EqualTo("123456"),"recortado a 6");Assert.That(n.Text(5),Is.EqualTo("99999!"));
            Assert.That(n.Text(6),Is.EqualTo("7"));Assert.That(n.KindOf(6),Is.EqualTo(-1));
        }
        [Test]public void AtMostOneHundredFortyNumbersAndTheOldestLeavesFirst()
        {
            var n=New();for(int i=1;i<=200;i++)n.Spawn(0,0,0,i,0);
            Assert.That(n.Active,Is.EqualTo(140));Assert.That(n.Text(0),Is.EqualTo("61"));Assert.That(n.Text(139),Is.EqualTo("200"));
            var glyphs=new DamageGlyph[DamageNumbers.MaxNumbers*DamageNumbers.MaxChars];Assert.That(n.Layout(glyphs),Is.LessThanOrEqualTo(840));
            n.Clear();Assert.That(n.Active,Is.Zero);
        }
        [Test]public void RiseFadePopAndLifetimeMatchTheWeb()
        {
            var n=New();n.Spawn(1,2,3,42,1);var g=new DamageGlyph[16];
            Assert.That(n.Layout(g),Is.EqualTo(3),"«42!»");
            Assert.That(g[0].Scale,Is.EqualTo(1.45f*1.35f).Within(1e-5),"crítico ×1,45 con «pop» inicial");Assert.That(g[0].Alpha,Is.EqualTo(1));
            Assert.That(g[0].Slot,Is.EqualTo(-1));Assert.That(g[2].Slot,Is.EqualTo(1));Assert.That(g[2].Glyph,Is.EqualTo(10),"«!»");
            float y0=g[0].Y;n.Update(0);n.Layout(g);Assert.That(g[0].Y,Is.EqualTo(y0),"dt 0 (pausa): quieto");
            n.Update(.375f);n.Layout(g);float t=.5f;Assert.That(g[0].Y,Is.EqualTo(2+1.5f*t-.6f*t*t).Within(1e-5));Assert.That(g[0].Scale,Is.EqualTo(1.45f).Within(1e-5));
            n.Update(.3f);n.Layout(g);t=.675f/.75f;Assert.That(g[0].Alpha,Is.EqualTo(1-(t-.65f)/.35f).Within(1e-4),"se desvanece al final");
            n.Update(.08f);Assert.That(n.Active,Is.Zero,"0,75 s de vida");
            n.Spawn(0,0,0,5,-1);n.Layout(g);Assert.That(g[0].Scale,Is.EqualTo(1.3f*1.35f).Within(1e-5),"daño recibido ×1,3");
        }
        [Test]public void SpawningAndLayoutDoNotAllocate()
        {
            var n=New();var g=new DamageGlyph[840];n.Spawn(0,0,0,1,0);n.Layout(g);
            long before=GC.GetTotalMemory(false);
            for(int i=0;i<5000;i++){n.Spawn(i,0,0,i*3.7,i%3);n.Update(.016f);n.Layout(g);}
            Assert.That(GC.GetTotalMemory(false)-before,Is.LessThan(64*1024));
        }
    }
}
