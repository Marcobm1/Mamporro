// Exportación explícita del catálogo TS a C#. No lee ni escribe baseline.json.
import {createServer} from 'vite';
import fs from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
execFileSync('git',['diff','--exit-code','0505b1690656d15188860157612455639820fe1f','--','src'],{stdio:'pipe'});
const server=await createServer({server:{middlewareMode:true,watch:null},appType:'custom'});
const q=JSON.stringify, num=x=>String(x), arr=(type,a)=>`new ${type}[]{${a.join(',')}}`;
const obj=(type,fields)=>`new ${type}{${Object.entries(fields).map(([k,v])=>`${k}=${v}`).join(',')}}`;
try {
 const load=p=>server.ssrLoadModule(`/src/${p}.ts`);
 const [w,t,i,e,c,r,u,es,en,waves,run]=await Promise.all(['data/weapons','data/tomes','data/items','data/enemies','data/characters','data/rarities','data/upgrades','i18n/es','i18n/en','data/waves','data/run'].map(load));
 const keys=['damage','cooldown','count','area','speed','duration','pierce','critChance','critMultiplier','knockback'];
 const effects=a=>arr('Effect',a.map(v=>obj('Effect',{stat:'Stat.'+v.stat,amount:num(v.amount),basis:String(v.mode==='base'),integer:String(!!v.integer),display:q(v.display)})));
 const numeric=(type,v)=>v?obj(type,Object.fromEntries(Object.entries(v).map(([k,n])=>[k,num(n)]))):'null';
 const groups={
  Weapons:arr('WeaponDef',w.WEAPON_LIST.map(d=>obj('WeaponDef',{id:q(d.id),behavior:q(d.behavior),values:arr('double',keys.map(k=>num(d.base[k]))),upgradable:arr('WStat',d.upgradable.map(k=>'WStat.'+k)),hitFlash:num(d.hitFlash),statLabels:arr('string',keys.map(k=>d.statLabels?.[k]?q(d.statLabels[k]):'null'))}))),
  Tomes:arr('TomeDef',t.TOME_LIST.map(d=>obj('TomeDef',{id:q(d.id),effects:effects(d.effects)}))),
  Items:arr('ItemDef',i.ITEM_LIST.map(d=>obj('ItemDef',{id:q(d.id),rarity:q(d.rarity),effects:effects(d.effects),maxStacks:num(d.maxStacks??0)}))),
  Characters:arr('CharacterDef',Object.values(c.CHARACTERS).map(d=>obj('CharacterDef',{id:q(d.id),startingWeapon:q(d.startingWeapon),maxHp:num(d.maxHp),armor:num(d.armor),pickupRadius:num(d.pickupRadius),passive:q(d.passive.kind),radius:num(d.passive.radius??0),amount:num(d.passive.amount??0),recharge:num(d.passive.recharge??0)}))),
  Enemies:arr('EnemyDef',e.ENEMY_LIST.map(d=>obj('EnemyDef',{id:q(d.id),behavior:q(d.behavior),...Object.fromEntries(['hp','speed','agility','damage','radius','height','xp','mass'].map(k=>[k,num(d[k])])),goldChance:num(d.gold.chance),goldMin:num(d.gold.min),goldMax:num(d.gold.max),ranged:numeric('RangedDef',d.ranged),charge:numeric('ChargeDef',d.charge)}))),
  Rarities:arr('RarityDef',r.RARITIES.map(d=>obj('RarityDef',{id:q(d.id),weight:num(d.weight),luckBonus:num(d.luckBonus),power:num(d.power),min:num(d.stats[0]),max:num(d.stats[1])}))),
  Steps:arr('UpgradeStep',keys.map(k=>{const d=u.WEAPON_UPGRADE_STEPS[k];return obj('UpgradeStep',{amount:num(d.amount),mode:q(d.mode),display:q(d.display),integer:String(!!d.integer),max:d.maxBonus===undefined?'double.PositiveInfinity':num(d.maxBonus)});})),
 };
 const types={Weapons:'WeaponDef',Tomes:'TomeDef',Items:'ItemDef',Characters:'CharacterDef',Enemies:'EnemyDef',Rarities:'RarityDef',Steps:'UpgradeStep'};
 // Constantes de reglas de combate y economía de partida que usa U2.
 const L=u.LEVEL_UP_CONFIG,S=waves.SPAWN_CURVE,B=e.BOSS_CONFIG,G=run.GOLD_CONFIG,C=run.CHEST_CONFIG;
 const tuning={ReferenceMinutes:waves.REFERENCE_MINUTES,SpawnHpGrowth:S.hpGrowth,SpawnHpCurve:S.hpCurve,SpawnXpGrowth:S.xpGrowth,BossHpGrowth:B.hpGrowth,BossHpCurve:B.hpCurve,
  FillerMinGold:G.fillerMin,FillerChestFraction:G.fillerChestFraction,ChestBaseCost:C.baseCost,ChestCostStep:C.costStep,ChestCostCurve:C.costCurve,
  FillerHeal:L.fillerHeal,InputGuard:L.inputGuard};
 const text='// Generado desde src/data aprobado mediante scripts/u2-export-data.mjs.\nnamespace Mamporro.Core\n{\n    public static class Catalog\n    {\n'+Object.entries(groups).map(([k,v])=>`        public static readonly ${types[k]}[] ${k}=${v};`).join('\n')+'\n    }\n    public static class Tuning\n    {\n'+Object.entries(tuning).map(([k,v])=>`        public const double ${k}=${num(v)};`).join('\n')+'\n    }\n}\n';
 await fs.writeFile('unity/Assets/Mamporro/Core/Catalog.cs',text);
 await fs.mkdir('unity/Assets/Mamporro/U2/Resources',{recursive:true});
 await fs.writeFile('unity/Assets/Mamporro/U2/Resources/WebText.json',JSON.stringify({entries:Object.keys(es.es).map(key=>({key,es:es.es[key],en:en.en[key]}))},null,2)+'\n');
 console.log('Catálogo C# y textos ES/EN exportados desde TypeScript; referencia U0 intacta.');
} finally {await server.close();}
