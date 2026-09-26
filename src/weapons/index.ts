// Registro de comportamientos de armas y creación de instancias.
import { WEAPONS, type WeaponBehaviorId, type WeaponId } from '../data/weapons';
import { applyWeaponBonus, effectiveWeaponStats, zeroWeaponStats, type PlayerStats } from '../systems/stats';
import { arc } from './arc';
import { aura } from './aura';
import { chain } from './chain';
import { homing } from './homing';
import { orbit } from './orbit';
import { trail } from './trail';
import type { WeaponBehavior, WeaponInstance } from './types';

export const BEHAVIORS: Readonly<Record<WeaponBehaviorId, WeaponBehavior>> = {
  homing,
  aura,
  arc,
  orbit,
  chain,
  trail,
};

export function createWeapon(id: WeaponId, slot: number, player: PlayerStats, enemyCapacity: number): WeaponInstance {
  const def = WEAPONS[id];
  const bonus = zeroWeaponStats();
  const stats = applyWeaponBonus(def.base, bonus);
  return {
    def,
    slot,
    level: 1,
    bonus,
    stats,
    effective: effectiveWeaponStats(stats, player),
    timer: 0.3,
    sincePulse: 10,
    totalDamage: 0,
    kills: 0,
    state: BEHAVIORS[def.behavior].createState(enemyCapacity),
  };
}

/** Recalcula las estadísticas de un arma (tras una mejora suya o del jugador). */
export function refreshWeapon(weapon: WeaponInstance, player: PlayerStats): void {
  weapon.stats = applyWeaponBonus(weapon.def.base, weapon.bonus);
  weapon.effective = effectiveWeaponStats(weapon.stats, player);
}

export type { CombatContext, WeaponInstance } from './types';
