using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    // cyrb128 + sfc32: mismas operaciones uint32 y unidades UTF-16 que TypeScript.
    public sealed class Rng
    {
        public readonly string Seed;
        uint a, b, c, d;
        public static uint[] Hash(string seed)
        {
            unchecked
            {
                uint h1=1779033703, h2=3144134277, h3=1013904242, h4=2773480762;
                foreach(char k in seed)
                {
                    h1=h2^((h1^k)*597399067); h2=h3^((h2^k)*2869860233);
                    h3=h4^((h3^k)*951274213); h4=h1^((h4^k)*2716044179);
                }
                h1=(h3^(h1>>18))*597399067; h2=(h4^(h2>>22))*2869860233;
                h3=(h1^(h3>>17))*951274213; h4=(h2^(h4>>19))*2716044179;
                h1^=h2^h3^h4; h2^=h1; h3^=h1; h4^=h1;
                return new[]{h1,h2,h3,h4};
            }
        }
        public Rng(string seed)
        {
            Seed=seed; var h=Hash(seed); a=h[0]; b=h[1]; c=h[2]; d=h[3];
            for(int i=0;i<12;i++) NextU32();
        }
        public uint NextU32()
        {
            unchecked { uint t=a+b+d; d++; a=b^(b>>9); b=c+(c<<3); c=(c<<21)|(c>>11); c+=t; return t; }
        }
        public double Next() => NextU32()/4294967296.0;
        public double Range(double min,double max) => min+(max-min)*Next();
        public int Int(int min,int max) => min+(int)Math.Floor(Next()*(max-min+1));
        public bool Chance(double probability) => Next()<probability;
        public T Pick<T>(IList<T> items) => items[(int)Math.Floor(Next()*items.Count)];
        public Rng Derive(string label) => new Rng(Seed+"/"+label);
        public void Shuffle<T>(IList<T> values)
        { for(int i=values.Count-1;i>0;i--) { int j=Int(0,i); T t=values[i]; values[i]=values[j]; values[j]=t; } }
    }
    public static class Rules
    {
        public static double Round(double x) => Math.Floor(x+.5);
        public static double Hypot(double x,double z) => Math.Sqrt(x*x+z*z);
        public static double ChestCost(int opened) {double n=Math.Max(0,opened);return Round(Tuning.ChestBaseCost+Tuning.ChestCostStep*n+Tuning.ChestCostCurve*n*n);}
        // Carta de relleno de oro: fracción del precio del siguiente baúl (U2 no abre baúles).
        public static double FillerGold(int chestsOpened) => Math.Max(Tuning.FillerMinGold,Round(ChestCost(chestsOpened)*Tuning.FillerChestFraction));
        public static double Nonzero(double x) => x==0?1:x;
        public static int XpNeeded(int level) => (int)Round(5+4*level+.9*Math.Pow(level,1.6));
        public static int AddExperience(ref int level,ref double xp,double amount)
        { xp+=amount; int gained=0; while(xp>=XpNeeded(level)) { xp-=XpNeeded(level++); gained++; } return gained; }
        public static double Damage(double damage,double chance,double multiplier,double random,out int crit)
        { chance=Math.Max(0,chance); int guaranteed=(int)Math.Floor(chance); crit=guaranteed+(random<chance-guaranteed?1:0); return damage*(1+crit*(multiplier-1)); }
        public static double Mitigate(double damage,double armor) => damage*100/(100+Math.Max(0,armor));
        public static double PearlChance(int n) => Math.Min(1,.3*n);
        public static double OllaChance(int n) => Math.Min(.4,.08*n);
        public static double PurseBonus(double gold,int n) => Math.Min(.8,Math.Floor(Math.Max(0,gold)/100)*.04)*n;
        public static double Scaled(double amount,double power,bool integer) => integer?Math.Max(1,Math.Floor(amount*power+1e-9)):Round(amount*power*1000)/1000;
    }
}
