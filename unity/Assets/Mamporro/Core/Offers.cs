using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    public sealed class Card
    {
        public string Kind,Key,Id,Rarity;
        public WStat[] Changes=Array.Empty<WStat>();
        public double[] Amounts=Array.Empty<double>();
        public double Amount;
    }
    // Las reservas ocurren al abrir una elección, nunca en el bucle de combate.
    public static class Offers
    {
        struct Candidate { public string Kind,Key,Id; public double Weight; public Weapon Weapon; public TomeDef Tome; }
        public static RarityDef RollRarity(double luck,Rng rng)
        {
            double total=0,l=Math.Max(0,luck)/100;
            foreach(var r in Catalog.Rarities) total+=r.weight*(1+l*r.luckBonus);
            double roll=rng.Next()*total;
            foreach(var r in Catalog.Rarities) { roll-=r.weight*(1+l*r.luckBonus); if(roll<0) return r; }
            return Catalog.Rarities[4];
        }
        public static List<WStat> Upgradable(Weapon w)
        {
            var result=new List<WStat>();
            foreach(var stat in w.Def.upgradable) if(w.Bonus[(int)stat]<Catalog.Steps[(int)stat].max-1e-9) result.Add(stat);
            return result;
        }
        public static bool Useful(Effect[] effects,PlayerStats stats)
        { foreach(var e in effects) if(!stats.Capped(e.stat)) return true; return false; }
        public static Card Heal() => new Card{Kind="heal",Amount=.3};
        public static List<Card> Generate(List<Weapon> weapons,List<Tome> tomes,PlayerStats stats,HashSet<string> banished,Rng rng,int count,HashSet<string> exclude=null,HashSet<string> allowed=null)
        {
            var pool=new List<Candidate>();
            bool Include(string key)=>!banished.Contains(key)&&(exclude==null||!exclude.Contains(key));
            foreach(var w in weapons) {
                string key="weapon:"+w.Def.id;
                if(Include(key)&&Upgradable(w).Count>0) pool.Add(new Candidate{Kind="weaponUpgrade",Key=key,Id=w.Def.id,Weight=10,Weapon=w});
            }
            if(weapons.Count<4) foreach(var d in Catalog.Weapons) {
                string key="weapon:"+d.id;
                if(Include(key)&&(allowed==null||allowed.Contains(d.id))&&!weapons.Exists(w=>w.Def.id==d.id)) pool.Add(new Candidate{Kind="newWeapon",Key=key,Id=d.id,Weight=7});
            }
            foreach(var d in Catalog.Tomes) {
                string key="tome:"+d.id; bool has=tomes.Exists(t=>t.Def.id==d.id);
                if(Include(key)&&(has||tomes.Count<4)&&Useful(d.effects,stats)) pool.Add(new Candidate{Kind="tome",Key=key,Id=d.id,Weight=has?9:7,Tome=d});
            }
            var cards=new List<Card>();
            while(cards.Count<count&&pool.Count>0) {
                double total=0; foreach(var c in pool) total+=c.Weight;
                double roll=rng.Next()*total; int index=pool.Count-1;
                for(int i=0;i<pool.Count;i++) { roll-=pool[i].Weight; if(roll<0) {index=i; break;} }
                var picked=pool[index]; pool.RemoveAt(index);
                var card=new Card{Kind=picked.Kind,Key=picked.Key,Id=picked.Id};
                if(picked.Kind!="newWeapon") {
                    var rarity=RollRarity(stats[Stat.luck],rng); card.Rarity=rarity.id;
                    if(picked.Kind=="weaponUpgrade") {
                        var statsPool=Upgradable(picked.Weapon); rng.Shuffle(statsPool);
                        int n=Math.Min(statsPool.Count,rng.Int(rarity.min,rarity.max));
                        card.Changes=statsPool.GetRange(0,n).ToArray(); card.Amounts=new double[n];
                        for(int k=0;k<n;k++) {int s=(int)card.Changes[k]; var step=Catalog.Steps[s]; card.Amounts[k]=Math.Min(Rules.Scaled(step.amount,rarity.power,step.integer),step.max-picked.Weapon.Bonus[s]);}
                    } else {
                        var effects=picked.Tome.effects; card.Amounts=new double[effects.Length];
                        for(int k=0;k<effects.Length;k++) card.Amounts[k]=Rules.Scaled(effects[k].amount,rarity.power,effects[k].integer);
                    }
                }
                cards.Add(card);
            }
            if(cards.Count<count) cards.Add(Heal());
            // U2 no abre cofres: chestCost(0)=15, relleno max(10,round(15*.5))=10.
            if(cards.Count<count) cards.Add(new Card{Kind="gold",Amount=10});
            return cards;
        }
        public static Card Replace(List<Weapon> weapons,List<Tome> tomes,PlayerStats stats,HashSet<string> banished,Rng rng,List<Card> rest)
        {
            var exclude=new HashSet<string>(); foreach(var c in rest) if(c.Key!=null) exclude.Add(c.Key);
            var card=Generate(weapons,tomes,stats,banished,rng,1,exclude)[0];
            if(card.Key!=null) return card;
            if(!rest.Exists(c=>c.Kind=="heal")) return Heal();
            if(!rest.Exists(c=>c.Kind=="gold")) return new Card{Kind="gold",Amount=10};
            return null;
        }
        public static bool ItemAvailable(ItemDef d,int owned,PlayerStats stats) => (d.maxStacks==0||owned<d.maxStacks)&&(d.effects.Length==0||Useful(d.effects,stats));
    }
}
