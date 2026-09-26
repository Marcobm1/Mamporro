// Comportamiento "rastro" (Suelo Recién Fregado): al andar por el suelo, el
// jugador va dejando charcos. Cada `cooldown` segundos dañan a quien los pisa y le
// hacen resbalar (se mueve más despacio un momento). Premia no quedarse quieto.
import { ENEMY_LIST } from '../data/enemies';
import type { CombatContext, TrailState, WeaponBehavior, WeaponInstance, WeaponUpdate } from './types';

/** Radio de cada charco con área 1 (m). */
export const PUDDLE_RADIUS = 1.1;
/** Metros andados entre dos charcos (con área 1). */
export const DROP_SPACING = 0.9;
export const MAX_PUDDLES = 96;
/** Frenado de quien resbala (0,35 = 35 % más lento) y cuánto dura (s). */
export const SLIP_SLOW = 0.35;
export const SLIP_TIME = 0.6;
/** Diferencia de altura máxima entre charco y enemigo (m). */
const REACH_HEIGHT = 1.2;

const inRange = new Int32Array(256);

function addPuddle(state: TrailState, x: number, y: number, z: number, radius: number, life: number): void {
  let i = state.count;
  if (i >= MAX_PUDDLES) {
    // Sin sitio: se recicla el más viejo.
    i = 0;
    for (let k = 1; k < state.count; k++) if ((state.age[k] as number) > (state.age[i] as number)) i = k;
  } else {
    state.count++;
  }
  state.x[i] = x;
  state.y[i] = y;
  state.z[i] = z;
  state.radius[i] = radius;
  state.age[i] = 0;
  state.life[i] = life;
}

function removePuddle(state: TrailState, i: number): void {
  const last = --state.count;
  if (i === last) return;
  for (const a of [state.x, state.y, state.z, state.radius, state.age, state.life]) a[i] = a[last] as number;
}

function dropPuddles(state: TrailState, ctx: CombatContext, radius: number, life: number, count: number): void {
  const { player } = ctx;
  const speed = Math.hypot(player.vx, player.vz) || 1;
  // En fila, de lado respecto a hacia dónde va.
  const sideX = -player.vz / speed;
  const sideZ = player.vx / speed;
  for (let k = 0; k < count; k++) {
    const offset = (k - (count - 1) / 2) * radius * 1.5;
    const x = player.x + sideX * offset;
    const z = player.z + sideZ * offset;
    addPuddle(state, x, ctx.groundHeight(x, z, player.y + 0.6), z, radius, life);
  }
}

function soak(state: TrailState, ctx: CombatContext, weapon: WeaponInstance): void {
  const { enemies } = ctx;
  // Marca nueva por pulso: un enemigo sobre dos charcos solo recibe un golpe.
  state.pulse++;
  if (state.pulse >= 0xffffffff) {
    state.stamp.fill(0);
    state.pulse = 1;
  }
  for (let p = 0; p < state.count; p++) {
    const px = state.x[p] as number;
    const pz = state.z[p] as number;
    const r = state.radius[p] as number;
    const n = enemies.grid.queryRadius(px, pz, r + 0.6, inRange);
    for (let k = 0; k < n; k++) {
      const e = inRange[k] as number;
      if (e >= enemies.count || (enemies.hp[e] as number) <= 0 || state.stamp[e] === state.pulse) continue;
      if (Math.abs((enemies.y[e] as number) - (state.y[p] as number)) > REACH_HEIGHT) continue;
      const def = ENEMY_LIST[enemies.type[e] as number];
      const d = Math.hypot((enemies.x[e] as number) - px, (enemies.z[e] as number) - pz);
      if (d > r + (def?.radius ?? 0.5) * 0.5) continue;
      state.stamp[e] = state.pulse;
      ctx.damageEnemy(e, weapon, 0, 0);
      enemies.applySlow(e, SLIP_SLOW, SLIP_TIME);
    }
  }
}

const updateTrail: WeaponUpdate = (weapon, ctx, dt) => {
  const state = weapon.state;
  if (state.kind !== 'trail') return;
  const s = weapon.effective;
  const { player } = ctx;

  for (let i = state.count - 1; i >= 0; i--) {
    state.age[i] = (state.age[i] as number) + dt;
    if ((state.age[i] as number) >= (state.life[i] as number)) removePuddle(state, i);
  }

  // Solo andando por el suelo se friega.
  if (!state.hasLast) {
    state.lastX = player.x;
    state.lastZ = player.z;
    state.hasLast = true;
  }
  const moved = Math.hypot(player.x - state.lastX, player.z - state.lastZ);
  state.lastX = player.x;
  state.lastZ = player.z;
  if (player.grounded && moved < 2) {
    state.distance += moved;
    const radius = PUDDLE_RADIUS * s.area;
    if (state.distance >= DROP_SPACING * s.area) {
      state.distance = 0;
      dropPuddles(state, ctx, radius, s.duration, Math.max(1, Math.round(s.count)));
    }
  }

  weapon.timer -= dt;
  weapon.sincePulse += dt;
  if (weapon.timer > 0) return;
  weapon.timer = s.cooldown;
  weapon.sincePulse = 0;
  if (state.count > 0) soak(state, ctx, weapon);
};

export const trail: WeaponBehavior = {
  update: updateTrail,
  createState: (enemyCapacity) => {
    const f = (): Float32Array => new Float32Array(MAX_PUDDLES);
    return {
      kind: 'trail',
      x: f(),
      y: f(),
      z: f(),
      radius: f(),
      age: f(),
      life: f(),
      count: 0,
      distance: 0,
      lastX: 0,
      lastZ: 0,
      hasLast: false,
      stamp: new Uint32Array(enemyCapacity),
      pulse: 0,
    };
  },
};
