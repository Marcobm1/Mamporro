// Valores esperados ejecutados exclusivamente en la web aprobada. No importa C#.
import {createServer} from 'vite';
import fs from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
execFileSync('git',['diff','--exit-code','0505b1690656d15188860157612455639820fe1f','--','src'],{stdio:'pipe'});
const server=await createServer({server:{middlewareMode:true,watch:null},appType:'custom'});
try {
 const load=p=>server.ssrLoadModule(`/src/${p}.ts`);
 const [{Run},{CHARACTERS},{WEAPON_LIST},{Rng},{generateOffer}]=await Promise.all(['core/Run','data/characters','data/weapons','core/rng','systems/levelup'].map(load));
 const world={heightfield:{size:96,heightAt:()=>0},groundHeight:()=>0,pushOutCircle:()=>false,clampInside:p=>{p.x=Math.max(-48,Math.min(48,p.x));p.z=Math.max(-48,Math.min(48,p.z));},isInside:()=>true};
 const combat=[];
 for(const weapon of WEAPON_LIST){
  const run=new Run(world,'U2-EQUIVALENCIA',CHARACTERS.remedios);run.spawner.update=()=>{};run.weapons.length=0;run.addWeapon(weapon.id);run.invincible=true;
  for(const [x,z] of [[0,-6],[0,-7.2],[0,-8.4],[2,0],[-2,0],[0,12]])run.enemies.spawn(0,x,0,z,100,1);
  run.enemies.rebuildGrid();
  const body={x:0,y:0,z:0,vx:0,vz:0,facing:0,grounded:true};
  for(let tick=0;tick<120;tick++){body.x=tick*.02;body.vx=1.2;run.update(1/60,body,0);}
  combat.push({weapon:weapon.id,hp:Array.from(run.enemies.hp.slice(0,run.enemies.count)),x:Array.from(run.enemies.x.slice(0,run.enemies.count)),z:Array.from(run.enemies.z.slice(0,run.enemies.count)),damage:run.weapons[0].totalDamage,projectiles:run.projectiles.count});
 }
 const offers=[];
 const run=new Run(world,'U2-OFERTAS',CHARACTERS.remedios);
 for(let k=0;k<20;k++)offers.push({cards:generateOffer(run.build,3,run.offerRng).map(c=>({kind:c.kind,key:c.key,id:c.weapon??c.tome??'',rarity:c.rarity??'',changes:c.changes??[],amounts:c.amounts??[],amount:c.amount??0}))});
 const rng=['ñ😀𝄞','\u0000X','abc'].map(seed=>{const r=new Rng(seed);return{seed,nextU32:Array.from({length:16},()=>r.nextU32())};});
 const value=JSON.stringify({source:'0505b1690656d15188860157612455639820fe1f',combat,offers,rng},null,2)+'\n';
 const target='unity/Assets/Mamporro/Tests/Core/WebFixtures.json';
 if(process.argv.includes('--export-once'))await fs.writeFile(target,value,{flag:'wx'});
 else {const actual=await fs.readFile(target,'utf8');if(actual.replace(/\r\n/g,'\n')!==value)throw new Error('Diferencias en fixtures: revisar, no sobrescribir.');}
 console.log('Fixtures U2 contrastados con TypeScript aprobado.');
}finally{await server.close();}
