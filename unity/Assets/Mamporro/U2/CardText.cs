using System;
using Mamporro.Core;

namespace Mamporro.U2
{
    // Textos de una carta de subida de nivel; equivale a describeCard() de src/ui/cards.ts.
    public sealed class CardView
    {
        public string Tone,Tag,Title,Level,Description;
        public string[] Lines;
        public bool Banishable;
    }

    public static class CardText
    {
        // STAT_LABELS de la web: nombre de cada estadística del jugador.
        static string PlayerStatLabel(Stat stat)=>stat==Stat.attackSpeed?"stat.cooldown":stat==Stat.critDamage?"stat.critMultiplier":"stat."+stat;
        static string WeaponStatLabel(WeaponDef def,WStat stat)=>def.statLabels?[(int)stat]??"stat."+stat;
        static string ChangeLine(double amount,string display,string label)
        {
            bool percent=display=="percent";
            return CombatText.Format(percent?"upgrade.percent":"upgrade.number","value",CombatText.Number(percent?amount*100:amount,1),"stat",CombatText.Capitalize(label));
        }
        public static string EffectLine(Effect effect,double amount)=>ChangeLine(amount,effect.display,CombatText.Get(PlayerStatLabel(effect.stat)));

        public static CardView Describe(Card card,CombatRun run)
        {
            switch(card.Kind){
                case "newWeapon":
                    return new CardView{Tone="new",Tag=CombatText.Get("levelup.newWeapon"),Title=CombatText.Get("weapon."+card.Id),Level="",Lines=Array.Empty<string>(),
                        Description=CombatText.Get("weapon."+card.Id+".desc"),Banishable=true};
                case "weaponUpgrade":{
                    var weapon=run.Weapons.Find(w=>w.Def.id==card.Id);int level=weapon?.Level??0;var def=Array.Find(Catalog.Weapons,w=>w.id==card.Id);
                    var lines=new string[card.Changes.Length];
                    for(int k=0;k<lines.Length;k++){var stat=card.Changes[k];lines[k]=ChangeLine(card.Amounts[k],Catalog.Steps[(int)stat].display,CombatText.Get(WeaponStatLabel(def,stat)));}
                    return new CardView{Tone=card.Rarity,Tag=CombatText.Get("rarity."+card.Rarity),Title=CombatText.Get("weapon."+card.Id),
                        Level=CombatText.Format("levelup.level","from",level,"to",level+1),Lines=lines,Description="",Banishable=true};
                }
                case "tome":{
                    var tome=run.Tomes.Find(t=>t.Def.id==card.Id);int level=tome?.Level??0;var def=Array.Find(Catalog.Tomes,t=>t.id==card.Id);
                    var lines=new string[def.effects.Length];
                    for(int k=0;k<lines.Length;k++)lines[k]=EffectLine(def.effects[k],k<card.Amounts.Length?card.Amounts[k]:0);
                    return new CardView{Tone=card.Rarity,Tag=CombatText.Get("rarity."+card.Rarity),Title=CombatText.Get("tome."+card.Id),
                        Level=level==0?CombatText.Get("levelup.newTome"):CombatText.Format("levelup.level","from",level,"to",level+1),Lines=lines,
                        Description=level==0?CombatText.Get("tome."+card.Id+".desc"):"",Banishable=true};
                }
                case "heal":
                    return new CardView{Tone="heal",Tag="",Title=CombatText.Get("levelup.filler"),Level="",Lines=Array.Empty<string>(),
                        Description=CombatText.Format("levelup.filler.desc","n",Rules.Round(Tuning.FillerHeal*100)),Banishable=false};
                default:
                    return new CardView{Tone="gold",Tag="",Title=CombatText.Get("levelup.fillerGold"),Level="",Lines=Array.Empty<string>(),
                        Description=CombatText.Format("levelup.fillerGold.desc","n",card.Amount),Banishable=false};
            }
        }
    }
}
