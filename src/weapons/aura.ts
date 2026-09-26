// Comportamiento "aura" (Eau de Naftalina): cada cierto tiempo daña a todos los
// enemigos dentro de un radio alrededor del jugador y los aparta un poco.
import { AURA_BASE_RADIUS } from '../data/weapons';
import type { CombatContext, WeaponBehavior } from './types';

const inRange = new Int32Array(512);

export function auraRadius(area: number): number {
  return AURA_BASE_RADIUS * area;
}

export const updateAura: WeaponBehavior = (weapon, ctx: CombatContext, dt) => {
  weapon.timer -= dt;
  weapon.sincePulse += dt;
  if (weapon.timer > 0) return;
  weapon.timer = weapon.effective.cooldown;
  weapon.sincePulse = 0;

  const { enemies, player } = ctx;
  const radius = auraRadius(weapon.effective.area);
  const n = enemies.grid.queryRadius(player.x, player.z, radius + 0.6, inRange);
  for (let k = 0; k < n; k++) {
    const e = inRange[k] as number;
    if (e >= enemies.count || (enemies.hp[e] as number) <= 0) continue;
    const dx = (enemies.x[e] as number) - player.x;
    const dz = (enemies.z[e] as number) - player.z;
    const d = Math.hypot(dx, dz);
    if (d > radius + 0.5) continue;
    ctx.damageEnemy(e, weapon, d > 1e-4 ? dx / d : 0, d > 1e-4 ? dz / d : 0);
  }
};
