import { describe, expect, it } from 'vitest';
import { Rng } from '../core/rng';
import { CHARACTERS } from '../data/characters';
import { enemyTypeIndex } from '../data/enemies';
import { EnemySystem } from '../systems/EnemySystem';
import { ProjectileSystem } from '../systems/ProjectileSystem';
import { computePlayerStats, type PlayerStats } from '../systems/stats';
import { ARC_BASE_RADIUS } from './arc';
import { CHAIN_JUMP_RANGE } from './chain';
import { BEHAVIORS, createWeapon, refreshWeapon } from './index';
import { BITE_INTERVAL, ORBIT_RADIUS } from './orbit';
import { DROP_SPACING, SLIP_SLOW } from './trail';
import type { CombatContext, WeaponInstance } from './types';

const DT = 1 / 60;
const PELUSA = enemyTypeIndex('pelusa');
const WORLD_SIZE = 320;

interface Arena {
  enemies: EnemySystem;
  ctx: CombatContext;
  stats: PlayerStats;
  /** ids de los enemigos golpeados, en orden. */
  hits: number[];
  /** Instante de cada golpe (según `time`). */
  times: number[];
  zaps: number[];
  time: number;
}

/** Suelo llano en y = 0, jugador en el origen mirando hacia -z. */
function arena(): Arena {
  const enemies = new EnemySystem(128, WORLD_SIZE);
  const a: Arena = {
    enemies,
    stats: computePlayerStats(CHARACTERS.remedios, []),
    hits: [],
    times: [],
    zaps: [],
    time: 0,
    ctx: undefined as unknown as CombatContext,
  };
  a.ctx = {
    enemies,
    projectiles: new ProjectileSystem(16),
    rng: new Rng('armas'),
    player: { x: 0, y: 0, z: 0, facing: 0, vx: 0, vz: 0, grounded: true },
    fx: { arcSwing: () => {}, chainZap: (_points, count) => a.zaps.push(count) },
    damageEnemy: (e) => {
      a.hits.push(enemies.id[e] as number);
      a.times.push(a.time);
    },
    groundHeight: () => 0,
  };
  return a;
}

function spawnAt(a: Arena, x: number, z: number): number {
  const i = a.enemies.spawn(PELUSA, x, 0, z, 1, 1);
  return a.enemies.id[i] as number;
}

function weaponFor(a: Arena, id: Parameters<typeof createWeapon>[0], count?: number): WeaponInstance {
  const w = createWeapon(id, 0, a.stats, a.enemies.capacity);
  if (count !== undefined) {
    w.bonus.count = count - w.def.base.count;
    refreshWeapon(w, a.stats);
  }
  return w;
}

function tick(a: Arena, w: WeaponInstance, seconds: number): void {
  const n = Math.round(seconds / DT);
  for (let k = 0; k < n; k++) {
    a.enemies.rebuildGrid();
    BEHAVIORS[w.def.behavior].update(w, a.ctx, DT);
    a.time += DT;
  }
}

describe('Barra de Pan Duro (arco)', () => {
  it('golpea a los de delante dentro del radio, no a los de detrás ni a los lejanos', () => {
    const a = arena();
    const front = spawnAt(a, 0, -2);
    const side = spawnAt(a, 1.5, -1.5); // 45° a un lado: dentro del abanico de 150°.
    spawnAt(a, 0, 2); // Detrás.
    spawnAt(a, 0, -(ARC_BASE_RADIUS + 2)); // Delante, pero lejos.
    const w = weaponFor(a, 'barra');
    w.timer = 0;
    tick(a, w, DT);
    expect(a.hits.sort()).toEqual([front, side].sort());
  });

  it('con cantidad 2 golpea también hacia atrás, y a cada enemigo una sola vez', () => {
    const a = arena();
    const front = spawnAt(a, 0, -2);
    const back = spawnAt(a, 0, 2);
    const w = weaponFor(a, 'barra', 2);
    w.timer = 0;
    tick(a, w, DT);
    expect(a.hits.sort()).toEqual([front, back].sort());
  });

  it('respeta hacia dónde mira el jugador', () => {
    const a = arena();
    const east = spawnAt(a, 2, 0);
    spawnAt(a, 0, -2);
    a.ctx.player.facing = -Math.PI / 2; // Mirando hacia +x.
    const w = weaponFor(a, 'barra');
    w.timer = 0;
    tick(a, w, DT);
    expect(a.hits).toEqual([east]);
  });
});

describe('Dentaduras Orbitales (órbita)', () => {
  it('muerden a quien está en su órbita, dejando tiempo entre mordiscos, y no a los lejanos', () => {
    const a = arena();
    const onRing = spawnAt(a, ORBIT_RADIUS, 0);
    spawnAt(a, 6, 6);
    const w = weaponFor(a, 'dentaduras');
    tick(a, w, 3);
    expect(a.hits.length).toBeGreaterThan(1);
    expect(new Set(a.hits)).toEqual(new Set([onRing]));
    for (let k = 1; k < a.times.length; k++) {
      expect((a.times[k] as number) - (a.times[k - 1] as number)).toBeGreaterThanOrEqual(BITE_INTERVAL - 1e-6);
    }
  });

  it('mientras se recargan no muerden', () => {
    const a = arena();
    spawnAt(a, ORBIT_RADIUS, 0);
    const w = weaponFor(a, 'dentaduras');
    const s = w.effective;
    tick(a, w, 0.3 + s.duration + 0.05); // Salen, giran todo su tiempo y se guardan.
    const state = w.state;
    expect(state.kind === 'orbit' && state.active).toBe(false);
    const before = a.hits.length;
    tick(a, w, s.cooldown - 0.2);
    expect(a.hits.length).toBe(before);
  });
});

describe('Jersey Estático (cadena)', () => {
  it('salta de enemigo en enemigo sin repetir, tantas veces como su perforación', () => {
    const a = arena();
    const line = [3, 6, 9, 12, 15].map((x) => spawnAt(a, x, 0));
    const w = weaponFor(a, 'jersey');
    w.timer = 0;
    tick(a, w, DT);
    const jumps = Math.round(w.effective.pierce);
    expect(a.hits).toEqual(line.slice(0, jumps + 1));
    expect(a.zaps).toEqual([jumps + 2]); // Jugador + enemigos alcanzados.
  });

  it('no salta a un enemigo fuera de su alcance', () => {
    const a = arena();
    const first = spawnAt(a, 3, 0);
    spawnAt(a, 3 + CHAIN_JUMP_RANGE + 1.5, 0);
    const w = weaponFor(a, 'jersey');
    w.timer = 0;
    tick(a, w, DT);
    expect(a.hits).toEqual([first]);
  });

  it('con cantidad 2 salen dos cadenas desde enemigos distintos', () => {
    const a = arena();
    spawnAt(a, 3, 0);
    spawnAt(a, -3, 0);
    const w = weaponFor(a, 'jersey', 2);
    w.timer = 0;
    tick(a, w, DT);
    expect(new Set(a.hits).size).toBe(2);
    expect(a.zaps).toHaveLength(2);
  });
});

describe('Suelo Recién Fregado (rastro)', () => {
  it('solo deja charcos al moverse por el suelo', () => {
    const a = arena();
    const w = weaponFor(a, 'fregona');
    const p = a.ctx.player;
    tick(a, w, 1); // Quieta.
    expect(w.state.kind === 'trail' && w.state.count).toBe(0);
    p.vx = 6;
    for (let k = 0; k < 60; k++) {
      p.x += 6 * DT; // 6 m en un segundo.
      tick(a, w, DT);
    }
    const count = w.state.kind === 'trail' ? w.state.count : 0;
    expect(count).toBeGreaterThanOrEqual(Math.floor(6 / DROP_SPACING) - 1);
    expect(count).toBeLessThanOrEqual(Math.ceil(6 / DROP_SPACING) + 1);
    p.grounded = false; // En el aire no se friega.
    for (let k = 0; k < 30; k++) {
      p.x += 6 * DT;
      tick(a, w, DT);
    }
    expect(w.state.kind === 'trail' && w.state.count).toBe(count);
  });

  it('daña y hace resbalar a quien pisa un charco; los charcos se secan', () => {
    const a = arena();
    const w = weaponFor(a, 'fregona');
    const p = a.ctx.player;
    p.vx = 6;
    for (let k = 0; k < 30; k++) {
      p.x += 6 * DT;
      tick(a, w, DT);
    }
    const victim = spawnAt(a, 1, 0);
    spawnAt(a, 1, 8); // Lejos del rastro.
    tick(a, w, 1);
    expect(a.hits.length).toBeGreaterThan(0);
    expect(new Set(a.hits)).toEqual(new Set([victim]));
    expect(a.enemies.slow[0]).toBeCloseTo(SLIP_SLOW);
    tick(a, w, w.effective.duration + 0.5);
    expect(w.state.kind === 'trail' && w.state.count).toBe(0);
  });

  it('quien resbala avanza más despacio', () => {
    const enemies = new EnemySystem(8, WORLD_SIZE);
    enemies.spawn(PELUSA, 10, 0, 0, 1, 1);
    enemies.spawn(PELUSA, -10, 0, 0, 1, 1);
    enemies.applySlow(0, SLIP_SLOW, 5);
    const flat = {
      groundHeight: () => 0,
      pushOutCircle: () => false,
      clampInside: () => {},
    } as unknown as Parameters<EnemySystem['update']>[2];
    const far = { x: 0, y: 0, z: 40, radius: 0.45, vx: 0, vz: 0 };
    enemies.rebuildGrid();
    for (let k = 0; k < 60; k++) enemies.update(DT, far, flat);
    // Cada enemigo tiene una pequeña variación de velocidad propia: se compara lo
    // avanzado respecto a su velocidad base.
    const pace = (i: number, x0: number): number =>
      Math.hypot((enemies.x[i] as number) - x0, enemies.z[i] as number) / (enemies.speed[i] as number);
    expect(pace(0, 10) / pace(1, -10)).toBeCloseTo(1 - SLIP_SLOW, 1);
  });
});
