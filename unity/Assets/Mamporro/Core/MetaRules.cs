using System;
using System.Collections.Generic;

namespace Mamporro.Core.Progress
{
    public sealed class MissionDefinition
    {
        public readonly string Id,UnlockKind,UnlockId;
        public readonly int Target,Coins;
        public MissionDefinition(string id,int target,int coins,string kind=null,string unlock=null)
        {Id=id;Target=target;Coins=coins;UnlockKind=kind;UnlockId=unlock;}
    }
    public sealed class ShopDefinition
    {
        public readonly string Id,UnlockKind,UnlockId;
        public readonly int Price;
        public ShopDefinition(string id,int price,string kind,string unlock)
        {Id=id;Price=price;UnlockKind=kind;UnlockId=unlock;}
    }

    // Reglas de la web sobre estado propio válido, sin almacenamiento ni UnityEngine.
    // Las normalizaciones de números de partida son de settleRun, NO del importador.
    public static class MetaRules
    {
        public const int MaxCoins=1000000000,BaseExtraUses=2;
        public static readonly IReadOnlyList<int> ExtraPrices=Array.AsReadOnly(new[]{80,140,220});
        public static readonly IReadOnlyList<MissionDefinition> Missions=Array.AsReadOnly(new[]{
            new MissionDefinition("first",1,30),new MissionDefinition("kills",1000,60,"weapon","fregona"),
            new MissionDefinition("chests",10,40),new MissionDefinition("shrines",3,40),
            new MissionDefinition("challenge",1,50,"item","perlas"),new MissionDefinition("level",20,50),
            new MissionDefinition("victory",1,80),new MissionDefinition("noLife",1,100,"item","bata")});
        public static readonly IReadOnlyList<ShopDefinition> Shop=Array.AsReadOnly(new[]{
            new ShopDefinition("baguette",220,"character","baguette"),new ShopDefinition("jersey",140,"weapon","jersey"),
            new ShopDefinition("baraja",160,"item","baraja"),new ShopDefinition("olla",180,"item","olla")});

        public static MetaDto New()=>new MetaDto { selected="remedios",lastRun="",
            characters=new[]{"remedios"},weapons=new[]{"chancla","naftalina","barra","dentaduras"},
            items=new[]{"gafas","zapatillas","termo","cojin","lupa","loteria","rulos","monedero"},
            completed=Array.Empty<string>(),extras=new ExtraLevelsDto(),missions=new MissionProgressDto() };

        static bool Has(string[] values,string id)=>Array.IndexOf(values,id)>=0;
        static void Add(ref string[] values,string id)
        {
            if(Has(values,id))return;
            Array.Resize(ref values,values.Length+1);values[values.Length-1]=id;
        }
        public static bool Owns(MetaDto state,string kind,string id)
        {
            switch(kind){case "weapon":return Has(state.weapons,id);case "item":return Has(state.items,id);
                case "character":return Has(state.characters,id);default:return false;}
        }
        static void Grant(MetaDto state,string kind,string id)
        {
            switch(kind){case "weapon":Add(ref state.weapons,id);break;case "item":Add(ref state.items,id);break;
                case "character":Add(ref state.characters,id);break;}
        }
        public static bool Select(MetaDto state,string id)
        {
            if((id!="remedios"&&id!="baguette")||!Has(state.characters,id))return false;
            state.selected=id;return true;
        }
        public static bool Purchase(MetaDto state,string id)
        {
            foreach(var entry in Shop){
                if(entry.Id!=id)continue;
                if(Owns(state,entry.UnlockKind,entry.UnlockId)||state.coins<entry.Price)return false;
                state.coins-=entry.Price;Grant(state,entry.UnlockKind,entry.UnlockId);return true;
            }
            return false;
        }
        public static bool PurchaseExtra(MetaDto state,string action)
        {
            if(action!="rerolls"&&action!="skips"&&action!="banishes")return false;
            int level=state.extras.Get(action);
            if(level<0||level>=ExtraPrices.Count||state.coins<ExtraPrices[level])return false;
            state.coins-=ExtraPrices[level];state.extras.Set(action,level+1);return true;
        }
        public static int InitialUses(MetaDto state,string action)=>BaseExtraUses+state.extras.Get(action);
        // Copias por sesión: una compra posterior no altera los filtros de una partida activa.
        public static HashSet<string> AllowedWeapons(MetaDto state)=>new HashSet<string>(state.weapons,StringComparer.Ordinal);
        public static HashSet<string> AllowedItems(MetaDto state)=>new HashSet<string>(state.items,StringComparer.Ordinal);

        static int RunInteger(double value)
        {
            if(double.IsNaN(value)||double.IsInfinity(value))return 0;
            return (int)Math.Min(MaxCoins,Math.Max(0,Math.Floor(value)));
        }
        public static MetaReceipt Settle(MetaDto state,MetaRun run)
        {
            var receipt=new MetaReceipt();
            if(string.IsNullOrEmpty(run.id)||run.id==state.lastRun||run.cheated)return receipt;
            state.lastRun=run.id;
            receipt.kills=RunInteger(run.kills)/20;
            receipt.survival=Math.Min(60,RunInteger(run.time)/15);
            receipt.victory=run.victory?60:0;
            // Los contadores ya están limitados al objetivo, así que la suma cabe en int.
            var values=new MissionProgressDto {
                first=state.missions.first+1,kills=state.missions.kills+RunInteger(run.kills),
                chests=state.missions.chests+RunInteger(run.chests),shrines=state.missions.shrines+RunInteger(run.shrines),
                challenge=state.missions.challenge+RunInteger(run.challenges),level=Math.Max(state.missions.level,RunInteger(run.level)),
                victory=state.missions.victory+(run.victory?1:0),noLife=state.missions.noLife+(run.victory&&!run.usedLifeTome?1:0) };
            foreach(var mission in Missions){
                int progress=Math.Min(mission.Target,values.Get(mission.Id));state.missions.Set(mission.Id,progress);
                if(progress<mission.Target||Has(state.completed,mission.Id))continue;
                Add(ref state.completed,mission.Id);Add(ref receipt.completed,mission.Id);receipt.missions+=mission.Coins;
                if(mission.UnlockId!=null)Grant(state,mission.UnlockKind,mission.UnlockId);
            }
            receipt.total=receipt.kills+receipt.survival+receipt.victory+receipt.missions;
            state.coins=(int)Math.Min(MaxCoins,(long)state.coins+receipt.total);
            return receipt;
        }
    }
}
