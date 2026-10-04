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
        public static double RarityWeight(RarityDef r,double luck) => r.weight*(1+Math.Max(0,luck)/100*r.luckBonus);
        public static RarityDef RollRarity(double luck,Rng rng)
        {
            double total=0;
            foreach(var r in Catalog.Rarities) total+=RarityWeight(r,luck);
            double roll=rng.Next()*total;
            foreach(var r in Catalog.Rarities) { roll-=RarityWeight(r,luck); if(roll<0) return r; }
            return Catalog.Rarities[4];
        }
        public static List<WStat> Upgradable(Weapon w) => Upgradable(w.Def,w.Bonus);
        public static List<WStat> Upgradable(WeaponDef def,double[] bonus)
        {
            var result=new List<WStat>();
            foreach(var stat in def.upgradable) if(bonus[(int)stat]<Catalog.Steps[(int)stat].max-1e-9) result.Add(stat);
            return result;
        }
        // Equivale a rollWeaponUpgrade de la web: baraja, sortea cuántas y recorta al tope.
        public static void RollUpgrade(WeaponDef def,double[] bonus,RarityDef rarity,Rng rng,out WStat[] changes,out double[] amounts)
        {
            var pool=Upgradable(def,bonus); rng.Shuffle(pool);
            int n=Math.Min(pool.Count,rng.Int(rarity.min,rarity.max));
            changes=pool.GetRange(0,n).ToArray(); amounts=new double[n];
            for(int k=0;k<n;k++) {int s=(int)changes[k]; var step=Catalog.Steps[s]; amounts[k]=Math.Min(Rules.Scaled(step.amount,rarity.power,step.integer),step.max-bonus[s]);}
        }
        public static double[] TomeAmounts(TomeDef def,RarityDef rarity)
        {
            var amounts=new double[def.effects.Length];
            for(int k=0;k<amounts.Length;k++) amounts[k]=Rules.Scaled(def.effects[k].amount,rarity.power,def.effects[k].integer);
            return amounts;
        }
        public static bool Useful(Effect[] effects,PlayerStats stats)
        { foreach(var e in effects) if(!stats.Capped(e.stat)) return true; return false; }
        public static Card Heal() => new Card{Kind="heal",Amount=Tuning.FillerHeal};
        // Carta de relleno de oro: mitad del precio del siguiente baúl (U2 no abre baúles: 0).
        public static Card Gold(int chestsOpened=0) => new Card{Kind="gold",Amount=Rules.FillerGold(chestsOpened)};
        public static List<Card> Generate(List<Weapon> weapons,List<Tome> tomes,PlayerStats stats,HashSet<string> banished,Rng rng,int count,HashSet<string> exclude=null,HashSet<string> allowed=null,int chestsOpened=0)
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
                    if(picked.Kind=="weaponUpgrade") RollUpgrade(picked.Weapon.Def,picked.Weapon.Bonus,rarity,rng,out card.Changes,out card.Amounts);
                    else card.Amounts=TomeAmounts(picked.Tome,rarity);
                }
                cards.Add(card);
            }
            if(cards.Count<count) cards.Add(Heal());
            if(cards.Count<count) cards.Add(Gold(chestsOpened));
            return cards;
        }
        public static Card Replace(List<Weapon> weapons,List<Tome> tomes,PlayerStats stats,HashSet<string> banished,Rng rng,List<Card> rest,int chestsOpened=0,HashSet<string> allowed=null)
        {
            var exclude=new HashSet<string>(); foreach(var c in rest) if(c.Key!=null) exclude.Add(c.Key);
            var card=Generate(weapons,tomes,stats,banished,rng,1,exclude,allowed,chestsOpened)[0];
            if(card.Key!=null) return card;
            if(!rest.Exists(c=>c.Kind=="heal")) return Heal();
            if(!rest.Exists(c=>c.Kind=="gold")) return Gold(chestsOpened);
            return null;
        }
        public static bool ItemAvailable(ItemDef d,int owned,PlayerStats stats) => (d.maxStacks==0||owned<d.maxStacks)&&(d.effects.Length==0||Useful(d.effects,stats));

        // generateShrineOffer: `count` bendiciones distintas, cada una con su rareza.
        public static List<Card> Shrine(PlayerStats stats,int count,Rng rng)
        {
            var pool=new List<ShrineBoost>();
            foreach(var b in Catalog.ShrineBoosts) if(!stats.Capped(b.effect.stat)) pool.Add(b);
            rng.Shuffle(pool);
            var cards=new List<Card>();
            for(int i=0;i<pool.Count&&i<count;i++) {
                var b=pool[i]; var rarity=RollRarity(stats[Stat.luck],rng);
                cards.Add(new Card{Kind="boost",Key="boost:"+b.id,Id=b.id,Rarity=rarity.id,Amount=Rules.Scaled(b.effect.amount,rarity.power,b.effect.integer)});
            }
            if(cards.Count==0) cards.Add(Heal());
            return cards;
        }
        public static ShrineBoost Boost(string id) => Array.Find(Catalog.ShrineBoosts,b=>b.id==id);

        // rollItem: primero la rareza (con la Suerte) y luego un objeto de esa rareza;
        // si no queda ninguno, las rarezas más cercanas (antes las de abajo).
        // allowed: objetos desbloqueados en la meta (null = catálogo completo, escenas QA y referencias).
        public static ItemDef RollItem(double luck,Rng rng,List<ItemStack> items,PlayerStats stats,HashSet<string> allowed=null)
        {
            var rarity=RollRarity(luck,rng); int n=Catalog.Rarities.Length,start=Array.IndexOf(Catalog.Rarities,rarity);
            var order=new List<int>(n); for(int i=0;i<n;i++) order.Add(i);
            order.Sort((a,b)=>{int da=Math.Abs(a-start),db=Math.Abs(b-start);return da!=db?da-db:a-b;});
            var pool=new List<ItemDef>();
            foreach(int r in order) {
                pool.Clear(); string id=Catalog.Rarities[r].id;
                foreach(var d in Catalog.Items) {
                    int owned=0; foreach(var s in items) if(s.Def.id==d.id) owned=s.Count;
                    if((allowed==null||allowed.Contains(d.id))&&d.rarity==id&&ItemAvailable(d,owned,stats)) pool.Add(d);
                }
                if(pool.Count>0) return rng.Pick(pool);
            }
            return null;
        }
    }
}
