// Estadísticas globales del jugador. Los tomos y objetos (hitos 3 y 4) las
// modificarán; las armas combinan las suyas con estas.
import type { CharacterDef } from '../data/characters';
import type { WeaponStats } from '../data/weapons';

export interface PlayerStats {
  /** Multiplicador de daño. */
  damage: number;
  /** Multiplicador de cadencia (divide los tiempos de recarga). */
  attackSpeed: number;
  /** Proyectiles extra por disparo. */
  extraProjectiles: number;
  /** Multiplicador de tamaño/área. */
  area: number;
  /** Probabilidad de crítico extra (se suma). */
  critChance: number;
  /** Multiplicador de crítico extra (se suma). */
  critDamage: number;
  projectileSpeed: number;
  duration: number;
  knockback: number;
  moveSpeed: number;
  maxHp: number;
  /** Vida recuperada por segundo. */
  regen: number;
  armor: number;
  pickupRadius: number;
  /** Multiplicador de experiencia. */
  xpGain: number;
  luck: number;
}

export function basePlayerStats(character: CharacterDef): PlayerStats {
  return {
    damage: 1,
    attackSpeed: 1,
    extraProjectiles: 0,
    area: 1,
    critChance: 0,
    critDamage: 0,
    projectileSpeed: 1,
    duration: 1,
    knockback: 1,
    moveSpeed: 1,
    maxHp: character.maxHp,
    regen: 0,
    armor: character.armor,
    pickupRadius: character.pickupRadius,
    xpGain: 1,
    luck: 0,
  };
}

/** Estadísticas finales de un arma: las suyas combinadas con las del jugador. */
export function effectiveWeaponStats(weapon: WeaponStats, player: PlayerStats): WeaponStats {
  return {
    damage: weapon.damage * player.damage,
    cooldown: weapon.cooldown / Math.max(0.1, player.attackSpeed),
    count: weapon.count > 0 ? weapon.count + player.extraProjectiles : 0,
    area: weapon.area * player.area,
    speed: weapon.speed * player.projectileSpeed,
    duration: weapon.duration * player.duration,
    pierce: weapon.pierce,
    critChance: weapon.critChance + player.critChance,
    critMultiplier: weapon.critMultiplier + player.critDamage,
    knockback: weapon.knockback * player.knockback,
  };
}
