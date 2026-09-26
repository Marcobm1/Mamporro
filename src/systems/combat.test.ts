import { describe, expect, it } from 'vitest';
import { Run, type RunEffects } from '../core/Run';
import { CHARACTERS } from '../data/characters';
import { enemyTypeIndex } from '../data/enemies';
import { AURA_BASE_RADIUS, WEAPONS } from '../data/weapons';
import { PlayerBody } from '../entities/playerPhysics';
import { generateWorldData } from '../world/World';
import { EnemySystem } from './EnemySystem';
import { GemSystem } from './GemSystem';

const DT = 1 / 60;
const world = generateWorldData('COMBAT-TEST');
const collision = world.collision;
const PELUSA = enemyTypeIndex('pelusa');

function standingPlayer(): PlayerBody {
  const body = new PlayerBody();
  body.placeAt(0, world.heightfield.heightAt(0, 0), 0);
  return body;
}

function newRun(fx?: Partial<RunEffects>): Run {
  const effects: RunEffects = {
    damageNumber: () => {},
    enemyKilled: () => {},
    enemySpawned: () => {},
    playerHit: () => {},
    levelUp: () => {},
    weaponGained: () => {},
    ...fx,
  };
  return new Run(collision, 'COMBAT-TEST', CHARACTERS.remedios, effects);
}

describe('enemigos', () => {
  it('persiguen al jugador', () => {
    const enemies = new EnemySystem(64, world.heightfield.size);
    enemies.spawn(PELUSA, 20, 0, 3, 1, 1);
    enemies.rebuildGrid();
    const target = { x: 0, y: world.heightfield.heightAt(0, 0), z: 0, radius: 0.45 };
    for (let t = 0; t < 180; t++) enemies.update(DT, target, collision);
    expect(Math.hypot(enemies.x[0] as number, enemies.z[0] as number)).toBeLessThan(10);
  });

  it('se separan entre sí en lugar de amontonarse', () => {
    const enemies = new EnemySystem(64, world.heightfield.size);
    for (let i = 0; i < 20; i++) enemies.spawn(PELUSA, 4 + (i % 5) * 0.05, 0, (i / 5) * 0.05, 1, 1);
    enemies.rebuildGrid();
    const far = { x: 200, y: 0, z: 200, radius: 0.45 };
    for (let t = 0; t < 90; t++) enemies.update(DT, far, collision);
    let minDist = Infinity;
    for (let i = 0; i < enemies.count; i++) {
      for (let j = i + 1; j < enemies.count; j++) {
        minDist = Math.min(minDist, Math.hypot((enemies.x[i] as number) - (enemies.x[j] as number), (enemies.z[i] as number) - (enemies.z[j] as number)));
      }
    }
    expect(minDist).toBeGreaterThan(0.5);
  });

  it('golpean por contacto y respetan su tiempo de recarga', () => {
    const enemies = new EnemySystem(8, world.heightfield.size);
    const ground = world.heightfield.heightAt(0, 0);
    enemies.spawn(PELUSA, 0.6, ground, 0, 1, 1);
    enemies.rebuildGrid();
    const target = { x: 0, y: ground, z: 0, radius: 0.45 };
    expect(enemies.update(DT, target, collision)).toBeGreaterThan(0);
    expect(enemies.update(DT, target, collision)).toBe(0);
  });

  it('subirse a una roca o a unas cajas no protege; a lo alto de un muro, sí', () => {
    const ground = world.heightfield.heightAt(0, 0);
    const contactAt = (height: number): number => {
      const enemies = new EnemySystem(8, world.heightfield.size);
      enemies.spawn(PELUSA, 0.6, ground, 0, 1, 1);
      enemies.rebuildGrid();
      return enemies.update(DT, { x: 0, y: ground + height, z: 0, radius: 0.45 }, collision);
    };
    expect(contactAt(1.2)).toBeGreaterThan(0);
    expect(contactAt(3)).toBe(0);
  });

  it('eliminar en mitad del array conserva los datos del resto (ids estables)', () => {
    const enemies = new EnemySystem(8, world.heightfield.size);
    for (let i = 0; i < 4; i++) enemies.spawn(PELUSA, i * 3, 0, 0, 1, 1);
    const lastId = enemies.id[3];
    enemies.hp[1] = 0;
    const dead: number[] = [];
    enemies.flushDead((i) => dead.push(enemies.id[i] as number));
    expect(dead).toEqual([2]);
    expect(enemies.count).toBe(3);
    expect(enemies.id[1]).toBe(lastId);
    expect(enemies.x[1]).toBe(9);
  });
});

describe('gemas', () => {
  it('se recogen al acercarse y suman su valor', () => {
    const gems = new GemSystem(10);
    gems.spawn(2, 0, 0, 3);
    gems.spawn(40, 0, 0, 5);
    let total = 0;
    for (let t = 0; t < 120; t++) total += gems.update(DT, 0, 0, 0, 3.2);
    expect(total).toBe(3);
    expect(gems.count).toBe(1);
  });

  it('si no caben más, se fusionan sin perder experiencia', () => {
    const gems = new GemSystem(5);
    for (let i = 0; i < 25; i++) gems.spawn(i, 0, 0, 2);
    expect(gems.count).toBe(5);
    let sum = 0;
    for (let i = 0; i < gems.count; i++) sum += gems.value[i] as number;
    expect(sum).toBe(50);
  });
});

describe('partida', () => {
  it('la chancla sale hacia el enemigo más cercano y perfora exactamente uno extra', () => {
    const run = newRun();
    const body = standingPlayer();
    const y = world.heightfield.heightAt(0, 0);
    // Tres pelusas en fila delante del jugador. El primer disparo sale a los 0,3 s y
    // el segundo a los 1,15 s: al segundo 1 solo ha volado una chancla (perforación 1).
    for (let k = 0; k < 3; k++) run.enemies.spawn(PELUSA, 0, y, -6 - k * 1.2, 50, 1);
    run.enemies.rebuildGrid();
    for (let t = 0; t < 60; t++) run.update(DT, body, 0);
    const chancla = run.weapons[0];
    expect(chancla?.def.id).toBe('chancla');
    const hurt = Array.from({ length: run.enemies.count }, (_, i) => (run.enemies.hp[i] as number) < (run.enemies.maxHp[i] as number)).filter(Boolean).length;
    expect(hurt).toBe(2);
    expect(chancla?.totalDamage).toBeGreaterThan(0);
  });

  it('el aura daña a los enemigos cercanos y no a los lejanos', () => {
    const run = newRun();
    run.weapons.length = 0; // Solo el aura, para aislar su efecto.
    const aura = run.addWeapon('naftalina');
    expect(aura?.slot).toBe(0);
    const body = standingPlayer();
    const y = world.heightfield.heightAt(0, 0);
    const near = run.enemies.spawn(PELUSA, AURA_BASE_RADIUS - 1, y, 0, 100, 1);
    const far = run.enemies.spawn(PELUSA, 0, y, 25, 100, 1);
    const nearId = run.enemies.id[near];
    const farId = run.enemies.id[far];
    run.enemies.rebuildGrid();
    const indexOf = (id: number | undefined): number => Array.from(run.enemies.id.subarray(0, run.enemies.count)).indexOf(id ?? -1);
    let maxFlash = 0;
    for (let t = 0; t < 40; t++) {
      run.update(DT, body, 0);
      maxFlash = Math.max(maxFlash, run.enemies.flash[indexOf(nearId)] as number);
    }
    const n = indexOf(nearId);
    const f = indexOf(farId);
    expect(run.enemies.hp[n]).toBeLessThan(run.enemies.maxHp[n] as number);
    expect(run.enemies.hp[f]).toBe(run.enemies.maxHp[f]);
    expect(aura?.totalDamage).toBeGreaterThan(0);
    // El destello del aura es suave (golpea a muchos a la vez) y más flojo que el de la chancla.
    expect(maxFlash).toBeGreaterThan(0);
    expect(maxFlash).toBeLessThanOrEqual(WEAPONS.naftalina.hitFlash);
    expect(WEAPONS.naftalina.hitFlash).toBeLessThan(WEAPONS.chancla.hitFlash);
  });

  it('una partida simulada avanza: mata, sube de nivel y consigue la segunda arma', () => {
    const levels: number[] = [];
    const run = newRun({ levelUp: (l) => levels.push(l) });
    run.invincible = true;
    const body = standingPlayer();
    for (let t = 0; t < 60 * 60; t++) run.update(DT, body, t * 0.01);
    expect(run.kills).toBeGreaterThan(20);
    expect(run.level).toBeGreaterThanOrEqual(2);
    expect(levels[0]).toBe(2);
    expect(run.weapons.map((w) => w.def.id)).toEqual(['chancla', 'naftalina']);
    for (let i = 0; i < run.enemies.count; i++) expect(Number.isFinite(run.enemies.x[i] as number)).toBe(true);
  });

  it('sin invencibilidad, los enemigos acaban con una abuela quieta', () => {
    let hits = 0;
    const run = newRun({ playerHit: () => hits++ });
    const body = standingPlayer();
    for (let t = 0; t < 60 * 60 * 4 && !run.dead; t++) run.update(DT, body, 0);
    expect(hits).toBeGreaterThan(0);
    expect(run.dead).toBe(true);
    expect(run.hp).toBe(0);
  });

  it('rendimiento: la lógica con 500 enemigos cabe de sobra en un tick de 16 ms', () => {
    const run = newRun();
    run.invincible = true;
    const body = standingPlayer();
    run.addWeapon('naftalina');
    run.debugSpawn(500);
    for (let t = 0; t < 30; t++) run.update(DT, body, 0); // Calentamiento del JIT.
    const ticks = 240;
    const start = performance.now();
    for (let t = 0; t < ticks; t++) run.update(DT, body, t * 0.01);
    const perTick = (performance.now() - start) / ticks;
    console.log(`[bench] lógica con ~${run.enemies.count} enemigos: ${perTick.toFixed(3)} ms por tick`);
    expect(run.enemies.count).toBeGreaterThan(300);
    expect(perTick).toBeLessThan(8);
  });
});
