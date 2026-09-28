import { describe, expect, it } from 'vitest';
import { NO_EFFECTS, Run } from './Run';
import { CHARACTERS } from '../data/characters';
import { defaultMeta } from '../systems/meta';
import { PlayerBody } from '../entities/playerPhysics';
import { generateWorldData } from '../world/World';
import { enemyTypeIndex } from '../data/enemies';
import { rollItem } from '../systems/items';
import { Rng } from './rng';
const world = generateWorldData('META');
function setup(knight = false) {
  const meta = defaultMeta();
  const run = new Run(world.collision, 'META', knight ? CHARACTERS.baguette : CHARACTERS.remedios, NO_EFFECTS, {
    allowedWeapons: meta.weapons, allowedItems: meta.items, extras: { rerolls: 3, skips: 1, banishes: 2 },
  });
  const body = new PlayerBody(); body.placeAt(0, world.heightfield.heightAt(0,0), 0);
  run.weaponsOff = true;
  return { run, body, meta };
}
describe('personajes y restricciones meta en partida', () => {
  it('filtra armas también en reroll y descarte, y objetos de cualquier rareza', () => {
    const { run, meta } = setup();
    expect(run.rerolls).toBe(5); expect(run.skips).toBe(3); expect(run.banishes).toBe(4);
    const rng = new Rng('ITEMS');
    for (let i = 0; i < 300; i++) {
      run.pendingLevelUps++; run.openChoice();
      for (const card of run.offer ?? []) if (card.kind === 'newWeapon') expect(meta.weapons).toContain(card.weapon);
      if (i < 4) run.banish(0);
      run.reroll();
      for (const card of run.offer ?? []) if (card.kind === 'newWeapon') expect(meta.weapons).toContain(card.weapon);
      run.choose(0);
      const item = rollItem(500, rng, [], run.stats, meta.items);
      if (item) expect(meta.items).toContain(item.id);
    }
    expect(rollItem(0, rng, [], run.stats, [])).toBeNull();
  });
  it('Remedios ralentiza cerca y no perpetúa el efecto más fuerte del suelo fregado', () => {
    const { run, body } = setup();
    const i = run.enemies.spawn(enemyTypeIndex('pelusa'), 2, body.y, 0, 1, 1);
    run.enemies.applySlow(i, 0.35, 0.02);
    run.update(1/60, body, 0); expect(run.enemies.slow[i]).toBeCloseTo(0.35);
    for (let n=0;n<4;n++) run.update(1/60,body,0);
    expect(run.enemies.slow[i]).toBeCloseTo(0.2);
    run.enemies.relocate(i,20,body.y,0);
    for (let n=0;n<4;n++) run.update(1/60,body,0);
    expect(run.enemies.slow[i]).toBe(0);
  });
  it('Sir Baguette carga el escudo, bloquea una vez y recibe el siguiente golpe', () => {
    const { run, body } = setup(true);
    expect(run.weapons[0]?.def.id).toBe('barra');
    for (let n=0;n<481;n++) { run.enemies.count = 0; run.enemies.rebuildGrid(); run.update(1/60,body,0); }
    expect(run.shieldCharge).toBe(8);
    run.enemies.count = 0; run.enemies.rebuildGrid(); run.enemies.spawn(enemyTypeIndex('pelusa'),0.6,body.y,0,1,1);
    run.update(1/60,body,0); expect(run.hp).toBe(100); expect(run.shieldCharge).toBe(0);
    run.invulnerable=0; run.enemies.count = 0; run.enemies.rebuildGrid(); run.enemies.spawn(enemyTypeIndex('pelusa'),0.6,body.y,0,1,1);
    run.update(1/60,body,0); expect(run.hp).toBeLessThan(100); expect(run.shieldCharge).toBe(0);
  });
  it('recuerda adquirir vida aunque se descarte su oferta después', () => {
    const { run } = setup(); expect(run.usedLifeTome).toBe(false);
    run.addTomeLevel('vitality',[20,0.4]); run.banished.add('tome:vitality'); expect(run.usedLifeTome).toBe(true);
  });
});
