// Comportamiento "homing" (Chancla Teledirigida): dispara proyectiles hacia los
// enemigos más cercanos; en vuelo se corrigen hacia el objetivo más próximo.
import { PROJECTILE_BASE_RADIUS } from '../data/weapons';
import type { CombatContext, WeaponBehavior, WeaponUpdate } from './types';

/** Alcance máximo para elegir objetivo al disparar (m). */
const TARGET_RANGE = 28;
/** Giro de los proyectiles hacia su objetivo (1/s). */
const HOMING_TURN = 7;
/** Si no hay a quién disparar, se vuelve a mirar pronto en vez de gastar la recarga. */
const RETRY_DELAY = 0.2;

const chosen: number[] = [];

const updateHoming: WeaponUpdate = (weapon, ctx: CombatContext, dt) => {
  weapon.timer -= dt;
  weapon.sincePulse += dt;
  if (weapon.timer > 0) return;
  const s = weapon.effective;
  const { enemies, player } = ctx;
  const count = Math.max(1, Math.round(s.count));

  chosen.length = 0;
  let firstDirX = 0;
  let firstDirZ = 0;
  for (let k = 0; k < count; k++) {
    // Objetivos distintos, del más cercano al más lejano.
    const target = enemies.grid.nearest(
      player.x,
      player.z,
      TARGET_RANGE,
      (e) => (enemies.hp[e] as number) > 0 && !chosen.includes(enemies.id[e] as number),
    );
    let dirX: number;
    let dirZ: number;
    if (target >= 0) {
      chosen.push(enemies.id[target] as number);
      dirX = (enemies.x[target] as number) - player.x;
      dirZ = (enemies.z[target] as number) - player.z;
      if (k === 0) {
        firstDirX = dirX;
        firstDirZ = dirZ;
      }
    } else if (k > 0) {
      // Menos enemigos que proyectiles: los que sobran salen en abanico.
      const spread = 0.35 * Math.ceil(k / 2) * (k % 2 === 0 ? 1 : -1);
      const c = Math.cos(spread);
      const sn = Math.sin(spread);
      dirX = firstDirX * c - firstDirZ * sn;
      dirZ = firstDirX * sn + firstDirZ * c;
    } else {
      break;
    }
    ctx.projectiles.spawn({
      x: player.x,
      y: player.y + 1.1,
      z: player.z,
      dirX,
      dirZ,
      speed: s.speed,
      life: s.duration,
      radius: PROJECTILE_BASE_RADIUS * s.area,
      pierce: Math.round(s.pierce),
      homing: HOMING_TURN,
      weapon: weapon.slot,
    });
  }
  const fired = chosen.length > 0;
  weapon.timer = fired ? s.cooldown : RETRY_DELAY;
  if (fired) weapon.sincePulse = 0;
};

export const homing: WeaponBehavior = { update: updateHoming, createState: () => ({ kind: 'none' }) };
