// Comportamiento "cadena" (Jersey Estático): un calambre alcanza al enemigo más
// cercano y salta al más próximo que no haya tocado, `pierce` veces. Con más
// cantidad salen varias cadenas, cada una desde un enemigo distinto.
import { ENEMY_LIST } from '../data/enemies';
import type { CombatContext, WeaponBehavior, WeaponUpdate } from './types';

/** Alcance para el primer enemigo (m). */
export const CHAIN_FIRST_RANGE = 16;
/** Alcance de cada salto con área 1 (m). */
export const CHAIN_JUMP_RANGE = 5.5;
/** Si no hay a quién electrocutar, se vuelve a mirar pronto en vez de gastar la recarga. */
const RETRY_DELAY = 0.2;
const MAX_POINTS = 64;

const hit: number[] = [];
const points = new Float32Array(MAX_POINTS * 3);

const updateChain: WeaponUpdate = (weapon, ctx: CombatContext, dt) => {
  weapon.timer -= dt;
  weapon.sincePulse += dt;
  if (weapon.timer > 0) return;
  const s = weapon.effective;
  const { enemies, player } = ctx;
  const chains = Math.max(1, Math.round(s.count));
  const jumps = Math.max(0, Math.round(s.pierce));
  const jumpRange = CHAIN_JUMP_RANGE * s.area;
  const fresh = (e: number): boolean => (enemies.hp[e] as number) > 0 && !hit.includes(enemies.id[e] as number);

  hit.length = 0;
  let fired = false;
  for (let c = 0; c < chains; c++) {
    let current = enemies.grid.nearest(player.x, player.z, CHAIN_FIRST_RANGE, fresh);
    if (current < 0) break;
    fired = true;
    points[0] = player.x;
    points[1] = player.y + 1.2;
    points[2] = player.z;
    let n = 1;
    let fromX = player.x;
    let fromZ = player.z;
    for (let j = 0; j <= jumps && current >= 0; j++) {
      const ex = enemies.x[current] as number;
      const ez = enemies.z[current] as number;
      const dx = ex - fromX;
      const dz = ez - fromZ;
      const d = Math.hypot(dx, dz) || 1;
      hit.push(enemies.id[current] as number);
      ctx.damageEnemy(current, weapon, dx / d, dz / d);
      if (n < MAX_POINTS) {
        const def = ENEMY_LIST[enemies.type[current] as number];
        points[n * 3] = ex;
        points[n * 3 + 1] = (enemies.y[current] as number) + (def?.height ?? 1) * 0.6;
        points[n * 3 + 2] = ez;
        n++;
      }
      fromX = ex;
      fromZ = ez;
      current = j < jumps ? enemies.grid.nearest(ex, ez, jumpRange, fresh) : -1;
    }
    ctx.fx.chainZap(points, n);
  }
  weapon.timer = fired ? s.cooldown : RETRY_DELAY;
  if (fired) weapon.sincePulse = 0;
};

export const chain: WeaponBehavior = { update: updateChain, createState: () => ({ kind: 'none' }) };
