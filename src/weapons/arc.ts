// Comportamiento "arco" (Barra de Pan Duro): cada cierto tiempo, un barrazo en
// abanico delante del jugador. Con más cantidad, los golpes se reparten a su
// alrededor (delante y detrás, luego en tres, cuatro... direcciones).
import { ENEMY_LIST } from '../data/enemies';
import type { CombatContext, WeaponBehavior, WeaponUpdate } from './types';

/** Radio del barrazo con área 1 (m). */
export const ARC_BASE_RADIUS = 3;
/** Mitad de la apertura del abanico (rad): 150° en total. */
export const ARC_HALF_ANGLE = (75 * Math.PI) / 180;
/** Los enemigos pegados al cuerpo reciben el golpe aunque estén algo de lado. */
const POINT_BLANK = 0.9;
/** Diferencia de altura máxima para alcanzar a un enemigo (m). */
const REACH_HEIGHT = 1.8;

const inRange = new Int32Array(1024);
const dirX: number[] = [];
const dirZ: number[] = [];

const updateArc: WeaponUpdate = (weapon, ctx: CombatContext, dt) => {
  weapon.timer -= dt;
  weapon.sincePulse += dt;
  if (weapon.timer > 0) return;
  const s = weapon.effective;
  weapon.timer = s.cooldown;
  weapon.sincePulse = 0;

  const { enemies, player } = ctx;
  const radius = ARC_BASE_RADIUS * s.area;
  const swings = Math.max(1, Math.round(s.count));
  const cosHalf = Math.cos(ARC_HALF_ANGLE);
  dirX.length = 0;
  dirZ.length = 0;
  for (let k = 0; k < swings; k++) {
    // Convenio de rotation.y: mirar con ángulo θ es ir hacia (-sen θ, -cos θ).
    const angle = player.facing + (k * Math.PI * 2) / swings;
    dirX.push(-Math.sin(angle));
    dirZ.push(-Math.cos(angle));
    ctx.fx.arcSwing(player.x, player.y + 0.9, player.z, angle, radius, ARC_HALF_ANGLE);
  }

  const n = enemies.queryRadius(player.x, player.z, radius, inRange);
  for (let k = 0; k < n; k++) {
    const e = inRange[k] as number;
    if (e >= enemies.count || (enemies.hp[e] as number) <= 0) continue;
    if (Math.abs((enemies.y[e] as number) - player.y) > REACH_HEIGHT) continue;
    const def = ENEMY_LIST[enemies.type[e] as number];
    const ex = (enemies.x[e] as number) - player.x;
    const ez = (enemies.z[e] as number) - player.z;
    const d = Math.hypot(ex, ez);
    if (d > radius + (def?.radius ?? 0.5)) continue;
    // Un enemigo recibe un solo golpe por barrazo aunque lo alcancen dos abanicos.
    let hit = d < POINT_BLANK;
    for (let w = 0; w < swings && !hit; w++) {
      if ((ex * (dirX[w] as number) + ez * (dirZ[w] as number)) / (d || 1) >= cosHalf) hit = true;
    }
    if (hit) ctx.damageEnemy(e, weapon, d > 1e-4 ? ex / d : 0, d > 1e-4 ? ez / d : 0);
  }
};

export const arc: WeaponBehavior = { update: updateArc, createState: () => ({ kind: 'none' }) };
