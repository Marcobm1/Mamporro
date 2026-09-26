// Registro de comportamientos de armas y creación de instancias.
import { WEAPONS, type WeaponBehaviorId, type WeaponId } from '../data/weapons';
import { effectiveWeaponStats, type PlayerStats } from '../systems/stats';
import { updateAura } from './aura';
import { updateHoming } from './homing';
import type { WeaponBehavior, WeaponInstance } from './types';

export const BEHAVIORS: Readonly<Record<WeaponBehaviorId, WeaponBehavior>> = {
  homing: updateHoming,
  aura: updateAura,
};

export function createWeapon(id: WeaponId, slot: number, player: PlayerStats): WeaponInstance {
  const def = WEAPONS[id];
  const stats = { ...def.base };
  return {
    def,
    slot,
    level: 1,
    stats,
    effective: effectiveWeaponStats(stats, player),
    timer: 0.3,
    sincePulse: 10,
    totalDamage: 0,
    kills: 0,
  };
}

export type { CombatContext, WeaponInstance } from './types';
