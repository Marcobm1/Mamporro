import { describe, expect, it } from 'vitest';
import { Rng } from '../core/rng';
import { BOSS_CONFIG, ENEMIES, enemyTypeIndex, type BossAttack } from '../data/enemies';
import { generateWorldData } from '../world/World';
import { BossController, type BossHooks } from './BossController';
import { EnemyProjectileSystem, PROJECTILE_PIPA } from './EnemyProjectileSystem';
import { ENEMY_STATE, EnemySystem } from './EnemySystem';

const DT = 1 / 60;
// Un mapa y un sitio llano y despejado cerca del inicio para las pruebas.
const world = generateWorldData('ENEMIGOS-TEST');
const collision = world.collision;
const ground = (x: number, z: number): number => world.heightfield.heightAt(x, z);

function target(x = 0, z = 0): { x: number; y: number; z: number; radius: number; vx: number; vz: number } {
  return { x, y: ground(x, z), z, radius: 0.45, vx: 0, vz: 0 };
}

function spawn(enemies: EnemySystem, id: keyof typeof ENEMIES, x: number, z: number): number {
  const i = enemies.spawn(enemyTypeIndex(id), x, ground(x, z), z, 1, 1);
  enemies.rebuildGrid();
  return i;
}

describe('paloma okupa (a distancia)', () => {
  it('se queda a su distancia y solo dispara tras avisar (quieta)', () => {
    const enemies = new EnemySystem(16, world.heightfield.size);
    spawn(enemies, 'paloma', 0, -22);
    const player = target();
    const shots: Array<{ dirX: number; dirZ: number; x: number; z: number }> = [];
    const actions = {
      shoot: (i: number, dirX: number, dirZ: number) => shots.push({ dirX, dirZ, x: enemies.x[i] as number, z: enemies.z[i] as number }),
    };
    let sawWindup = false;
    for (let t = 0; t < 60 * 8; t++) {
      enemies.update(DT, player, collision, actions);
      if (enemies.state[0] === ENEMY_STATE.windup) sawWindup = true;
    }
    const r = ENEMIES.paloma.ranged;
    const dist = Math.hypot(enemies.x[0] as number, enemies.z[0] as number);
    expect(dist).toBeGreaterThan((r?.preferred ?? 10) - 3);
    expect(dist).toBeLessThan((r?.preferred ?? 10) + 3);
    expect(sawWindup).toBe(true);
    expect(shots.length).toBeGreaterThanOrEqual(2);
    // Cada disparo apunta al jugador desde donde estaba la paloma.
    for (const s of shots) {
      const d = Math.hypot(s.x, s.z);
      expect((s.dirX * -s.x + s.dirZ * -s.z) / d).toBeGreaterThan(0.99);
    }
  });

  it('huye si el jugador se le acerca demasiado', () => {
    const enemies = new EnemySystem(16, world.heightfield.size);
    spawn(enemies, 'paloma', 0, -4);
    const player = target();
    for (let t = 0; t < 60; t++) enemies.update(DT, player, collision);
    expect(Math.hypot(enemies.x[0] as number, enemies.z[0] as number)).toBeGreaterThan(4.5);
  });
});

describe('rata de gimnasio (élite que embiste)', () => {
  it('avisa quieta, embiste en línea recta más fuerte y se queda un momento vendida', () => {
    const enemies = new EnemySystem(16, world.heightfield.size);
    spawn(enemies, 'rata', 0, -12);
    const c = ENEMIES.rata.charge;
    const player = target();
    const seen: number[] = [];
    let dashSpeed = 0;
    let dashDamage = 0;
    let windupMoved = 0;
    let lastX = 0;
    let lastZ = 0;
    for (let t = 0; t < 60 * 4; t++) {
      const before = enemies.state[0] as number;
      lastX = enemies.x[0] as number;
      lastZ = enemies.z[0] as number;
      enemies.update(DT, player, collision);
      const state = enemies.state[0] as number;
      if (seen[seen.length - 1] !== state) seen.push(state);
      if (state === ENEMY_STATE.windup && before === ENEMY_STATE.windup) windupMoved += Math.hypot((enemies.x[0] as number) - lastX, (enemies.z[0] as number) - lastZ);
      if (state === ENEMY_STATE.dash) {
        dashSpeed = Math.max(dashSpeed, Math.hypot(enemies.vx[0] as number, enemies.vz[0] as number));
        dashDamage = enemies.hitDamage[0] as number;
      }
    }
    expect(seen.slice(0, 5)).toEqual([ENEMY_STATE.move, ENEMY_STATE.windup, ENEMY_STATE.dash, ENEMY_STATE.recover, ENEMY_STATE.move]);
    expect(windupMoved).toBeLessThan(1);
    expect(dashSpeed).toBeCloseTo(c?.dashSpeed ?? 0, 0);
    expect(dashDamage).toBeCloseTo(ENEMIES.rata.damage * (c?.damageMultiplier ?? 1));
    // Al acabar, vuelve a su daño normal.
    expect(enemies.hitDamage[0]).toBe(ENEMIES.rata.damage);
  });
});

describe('enemigos enormes (el jefe)', () => {
  it('las búsquedas por radio encuentran al jefe por su borde, sin repetirlo', () => {
    const enemies = new EnemySystem(16, world.heightfield.size);
    const boss = spawn(enemies, 'pelusaMadre', 10, 10);
    spawn(enemies, 'pelusa', 13.5, 10);
    const out = new Int32Array(16);
    // A 3,5 m del centro del jefe, con radio 1,5: toca su borde (2,4 m).
    const n = enemies.queryRadius(13.5, 10, 1.5, out);
    const found = [...out.slice(0, n)];
    expect(found.filter((i) => i === boss)).toHaveLength(1);
    // Y cerca, la rejilla ya lo devuelve: no sale dos veces.
    const n2 = enemies.queryRadius(11, 10, 1, out);
    expect([...out.slice(0, n2)].filter((i) => i === boss)).toHaveLength(1);
  });

  it('no se deja empujar: es el jugador quien rebota, y las pelusas se apartan de él', () => {
    const enemies = new EnemySystem(16, world.heightfield.size);
    spawn(enemies, 'pelusaMadre', 2, 0);
    spawn(enemies, 'pelusa', 3.2, 0.3);
    const player = target();
    const bx = enemies.x[0] as number;
    enemies.setState(0, ENEMY_STATE.windup, 10);
    enemies.update(DT, player, collision);
    expect(enemies.playerPushX).toBeLessThan(0);
    expect(Math.abs((enemies.x[0] as number) - bx)).toBeLessThan(0.05);
    for (let t = 0; t < 60; t++) enemies.update(DT, { ...player, x: 50, z: 50, y: ground(50, 50) }, collision);
    const d = Math.hypot((enemies.x[1] as number) - (enemies.x[0] as number), (enemies.z[1] as number) - (enemies.z[0] as number));
    expect(d).toBeGreaterThan(ENEMIES.pelusaMadre.radius + ENEMIES.pelusa.radius - 0.3);
  });
});

describe('pelusa madre (jefe)', () => {
  function setup(): { enemies: EnemySystem; boss: BossController; calls: { slam: number; dust: number; minion: number }; hooks: BossHooks } {
    const enemies = new EnemySystem(64, world.heightfield.size);
    spawn(enemies, 'pelusaMadre', 0, -8);
    const boss = new BossController(enemies.id[0] as number, new Rng('jefe'));
    const calls = { slam: 0, dust: 0, minion: 0 };
    const hooks: BossHooks = { slam: () => calls.slam++, dust: () => calls.dust++, minion: () => calls.minion++ };
    return { enemies, boss, calls, hooks };
  }

  it('alterna sus tres ataques avisados sin repetir el mismo dos veces seguidas', () => {
    const { enemies, boss, calls, hooks } = setup();
    const player = target();
    const attacks: BossAttack[] = [];
    let windupTicks = 0;
    let lastPhase = boss.phase;
    for (let t = 0; t < 60 * 60; t++) {
      boss.update(DT, enemies, player, hooks);
      enemies.update(DT, player, collision);
      if (boss.phase === 'windup') {
        windupTicks++;
        expect(enemies.state[0]).toBe(ENEMY_STATE.windup);
        expect(boss.windupProgress).toBeGreaterThanOrEqual(0);
        expect(boss.windupProgress).toBeLessThanOrEqual(1);
      }
      if (boss.phase === 'windup' && lastPhase !== 'windup' && boss.attack) attacks.push(boss.attack);
      lastPhase = boss.phase;
    }
    expect(new Set(attacks)).toEqual(new Set(['roll', 'slam', 'sneeze']));
    for (let k = 1; k < attacks.length; k++) expect(attacks[k]).not.toBe(attacks[k - 1]);
    expect(windupTicks).toBeGreaterThan(0);
    expect(calls.slam).toBeGreaterThan(0);
    // Cada estornudo: bolas de polvo y pelusas hijas.
    const sneezes = attacks.filter((a) => a === 'sneeze').length;
    expect(calls.minion).toBe(sneezes * BOSS_CONFIG.sneeze.minions);
    expect(calls.dust).toBeGreaterThanOrEqual(sneezes * BOSS_CONFIG.sneeze.projectiles);
  });

  it('enfadado (media vida) ataca más a menudo', () => {
    const count = (enraged: boolean): number => {
      const { enemies, boss, hooks } = setup();
      if (enraged) enemies.hp[0] = (enemies.maxHp[0] as number) * 0.3;
      const player = target();
      let n = 0;
      let last = boss.phase;
      for (let t = 0; t < 60 * 30; t++) {
        boss.update(DT, enemies, player, hooks);
        enemies.update(DT, player, collision);
        if (boss.phase === 'windup' && last !== 'windup') n++;
        last = boss.phase;
      }
      return n;
    };
    expect(count(true)).toBeGreaterThan(count(false));
  });

  it('el rodillo es una embestida en la dirección avisada, con su daño', () => {
    const { enemies, boss, hooks } = setup();
    const player = target();
    for (let t = 0; t < 60 * 30 && !(boss.phase === 'attack' && boss.attack === 'roll'); t++) {
      boss.update(DT, enemies, player, hooks);
      enemies.update(DT, player, collision);
    }
    expect(boss.attack).toBe('roll');
    expect(enemies.state[0]).toBe(ENEMY_STATE.dash);
    expect(enemies.hitDamage[0]).toBe(BOSS_CONFIG.roll.damage);
    expect(enemies.dashSpeed[0]).toBe(BOSS_CONFIG.roll.speed);
  });
});

describe('proyectiles enemigos', () => {
  it('dañan al jugador al tocarle y desaparecen; los que fallan caducan', () => {
    const shots = new EnemyProjectileSystem(8);
    const hf = world.heightfield;
    shots.spawn({ x: 0, z: -6, dirX: 0, dirZ: 1, speed: 10, damage: 7, radius: 0.3, life: 3, kind: PROJECTILE_PIPA }, hf);
    shots.spawn({ x: 5, z: -6, dirX: 0, dirZ: 1, speed: 10, damage: 9, radius: 0.3, life: 1, kind: PROJECTILE_PIPA }, hf);
    const player = target();
    let hit = 0;
    for (let t = 0; t < 60 && hit === 0; t++) hit = shots.update(DT, hf, player);
    expect(hit).toBe(7);
    expect(shots.count).toBe(1);
    for (let t = 0; t < 90; t++) shots.update(DT, hf, player);
    expect(shots.count).toBe(0);
  });
});
