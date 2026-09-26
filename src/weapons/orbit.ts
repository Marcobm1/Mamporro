// Comportamiento "órbita" (Dentaduras Orbitales): las dentaduras giran alrededor
// del jugador durante `duration` segundos, se recargan durante `cooldown` y vuelven
// a salir. Muerden a cada enemigo que tocan, como mucho una vez cada BITE_INTERVAL.
import { ENEMY_LIST } from '../data/enemies';
import type { CombatContext, OrbitState, WeaponBehavior, WeaponInstance, WeaponUpdate } from './types';

/** Radio de la órbita con área 1 (m). */
export const ORBIT_RADIUS = 2.3;
/** Radio de cada dentadura con área 1 (m). */
export const ORB_RADIUS = 0.5;
/** Velocidad de giro con velocidad 1 (rad/s). */
export const ORBIT_SPIN = 3.2;
/** Tiempo mínimo entre dos mordiscos al mismo enemigo (s). */
export const BITE_INTERVAL = 0.45;
/** Diferencia de altura máxima para morder (m). */
const REACH_HEIGHT = 1.8;
/** Cada cuánto se olvidan los mordiscos viejos (s). */
const PRUNE_INTERVAL = 2;

const inRange = new Int32Array(256);

/** Radio de la órbita y de cada dentadura para un área dada. */
export function orbitGeometry(area: number): { orbit: number; orb: number } {
  return { orbit: ORBIT_RADIUS * area, orb: ORB_RADIUS * (0.5 + 0.5 * area) };
}

function bite(state: OrbitState, ctx: CombatContext, weapon: WeaponInstance): void {
  const s = weapon.effective;
  const { enemies, player } = ctx;
  const { orbit, orb } = orbitGeometry(s.area);
  const count = Math.max(1, Math.round(s.count));
  for (let k = 0; k < count; k++) {
    const a = state.angle + (k * Math.PI * 2) / count;
    const ox = player.x + Math.cos(a) * orbit;
    const oz = player.z + Math.sin(a) * orbit;
    const n = enemies.queryRadius(ox, oz, orb, inRange);
    for (let j = 0; j < n; j++) {
      const e = inRange[j] as number;
      if (e >= enemies.count || (enemies.hp[e] as number) <= 0) continue;
      if (Math.abs((enemies.y[e] as number) - player.y) > REACH_HEIGHT) continue;
      const def = ENEMY_LIST[enemies.type[e] as number];
      const dx = (enemies.x[e] as number) - ox;
      const dz = (enemies.z[e] as number) - oz;
      if (Math.hypot(dx, dz) > orb + (def?.radius ?? 0.5)) continue;
      const id = enemies.id[e] as number;
      if (state.clock < (state.nextBite.get(id) ?? 0)) continue;
      state.nextBite.set(id, state.clock + BITE_INTERVAL);
      // Empujan hacia fuera, alejando al enemigo del jugador.
      const px = (enemies.x[e] as number) - player.x;
      const pz = (enemies.z[e] as number) - player.z;
      const pd = Math.hypot(px, pz) || 1;
      ctx.damageEnemy(e, weapon, px / pd, pz / pd);
    }
  }
}

const updateOrbit: WeaponUpdate = (weapon, ctx, dt) => {
  const state = weapon.state;
  if (state.kind !== 'orbit') return;
  const s = weapon.effective;
  state.clock += dt;
  weapon.sincePulse += dt;
  weapon.timer -= dt;
  if (state.active) {
    state.angle += ORBIT_SPIN * s.speed * dt;
    bite(state, ctx, weapon);
    if (weapon.timer <= 0) {
      state.active = false;
      weapon.timer = s.cooldown;
    }
  } else if (weapon.timer <= 0) {
    state.active = true;
    weapon.timer = s.duration;
    weapon.sincePulse = 0;
  }
  // Olvida los mordiscos que ya no limitan nada (enemigos muertos o lejos).
  if (state.clock % PRUNE_INTERVAL < dt) {
    for (const [id, until] of state.nextBite) if (until <= state.clock) state.nextBite.delete(id);
  }
};

export const orbit: WeaponBehavior = {
  update: updateOrbit,
  createState: () => ({ kind: 'orbit', angle: 0, active: false, clock: 0, nextBite: new Map() }),
};
