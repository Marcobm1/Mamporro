using System;
using System.Collections.Generic;

namespace Mamporro.Core
{
    public enum Stat { damage, attackSpeed, extraProjectiles, area, critChance, critDamage, projectileSpeed, duration, knockback, moveSpeed, maxHp, regen, armor, pickupRadius, xpGain, luck, choices, goldGain }
    public enum WStat { damage, cooldown, count, area, speed, duration, pierce, critChance, critMultiplier, knockback }
    public sealed class Effect { public Stat stat; public double amount; public bool basis, integer; public string display; }
    public sealed class WeaponDef { public string id, behavior; public double[] values; public WStat[] upgradable; public double hitFlash; }
    public sealed class TomeDef { public string id; public Effect[] effects; }
    public sealed class ItemDef { public string id,rarity; public Effect[] effects; public int maxStacks; }
    public sealed class CharacterDef { public string id,startingWeapon,passive; public double maxHp,armor,pickupRadius,radius,amount,recharge; }
    public sealed class EnemyDef
    {
        public string id,behavior; public double hp,speed,agility,damage,radius,height,xp,mass,goldChance; public int goldMin,goldMax;
        public RangedDef ranged; public ChargeDef charge;
    }
    public sealed class RangedDef { public double preferred,range,cooldown,windup,projectileSpeed,projectileDamage,projectileRadius; }
    public sealed class ChargeDef { public double cooldown,range,windup,dashTime,dashSpeed,damageMultiplier,recover; }
    public sealed class RarityDef { public string id; public double weight,luckBonus,power; public int min,max; }
    public sealed class UpgradeStep { public double amount,max=double.PositiveInfinity; public string mode; public bool integer; }
    public sealed class PlayerStats
    {
        public readonly double[] values=new double[18];
        public double this[Stat s] { get=>values[(int)s]; set=>values[(int)s]=value; }
        public static readonly Dictionary<Stat,double> Limits=new Dictionary<Stat,double> {
            {Stat.attackSpeed,4},{Stat.extraProjectiles,6},{Stat.area,3},{Stat.moveSpeed,1.8},{Stat.pickupRadius,25},
            {Stat.regen,15},{Stat.critChance,3},{Stat.armor,200},{Stat.choices,4} };
        public bool Capped(Stat s) => Limits.TryGetValue(s,out double max)&&this[s]>=max-1e-9;
        public void Reset(CharacterDef c)
        {
            Array.Clear(values,0,values.Length);
            this[Stat.damage]=this[Stat.attackSpeed]=this[Stat.area]=this[Stat.projectileSpeed]=this[Stat.duration]=this[Stat.knockback]=this[Stat.moveSpeed]=this[Stat.xpGain]=this[Stat.goldGain]=1;
            this[Stat.maxHp]=c.maxHp; this[Stat.armor]=c.armor; this[Stat.pickupRadius]=c.pickupRadius; this[Stat.choices]=3;
        }
        public void Cap() { foreach(var p in Limits) this[p.Key]=Math.Min(this[p.Key],p.Value); }
        public void Add(Effect e,double amount,PlayerStats basis) { this[e.stat]+=amount*(e.basis?basis[e.stat]:1); }
    }
    public sealed class Weapon
    {
        public readonly WeaponDef Def;
        public readonly int Slot;
        public int Level=1,Kills;
        public double Timer=.3,SincePulse=10,TotalDamage;
        public readonly double[] Bonus=new double[10],Stats=new double[10],Effective=new double[10];
        public double this[WStat s] => Effective[(int)s];
        public Weapon(WeaponDef def,int slot) { Def=def; Slot=slot; }
        public void Refresh(PlayerStats p)
        {
            for(int i=0;i<10;i++) {
                var mode=Catalog.Steps[i].mode; double b=Def.values[i],v=Bonus[i];
                Stats[i]=mode=="mult"?b*(1+v):mode=="rate"?b/(1+v):b+v; Effective[i]=Stats[i];
            }
            Effective[0]*=p[Stat.damage]; Effective[1]=Math.Max(.1,Stats[1]/Math.Max(.1,p[Stat.attackSpeed]));
            Effective[2]=Stats[2]>0?Math.Min(12,Rules.Round(Stats[2]+p[Stat.extraProjectiles])):0;
            Effective[3]=Math.Min(5,Stats[3]*p[Stat.area]); Effective[4]*=p[Stat.projectileSpeed]; Effective[5]*=p[Stat.duration];
            Effective[6]=Math.Min(30,Rules.Round(Stats[6])); Effective[7]+=p[Stat.critChance]; Effective[8]+=p[Stat.critDamage]; Effective[9]*=p[Stat.knockback];
        }
    }
    public sealed class Tome { public TomeDef Def; public int Level; public double[] Bonus; }
    public sealed class ItemStack { public ItemDef Def; public int Count; }
}
