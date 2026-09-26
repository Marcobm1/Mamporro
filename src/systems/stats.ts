// Estadísticas del jugador y de las armas (lógica pura). Los tomos suman a las
// del jugador; las armas combinan las suyas (con sus mejoras) con las del jugador.
import type { CharacterDef } from '../data/characters';
import { PLAYER_STAT_LIMITS, type TomeDef, type TomeStat } from '../data/tomes';
import { WEAPON_STAT_LIMITS, WEAPON_UPGRADE_STEPS, type WeaponStatKey } from '../data/upgrades';
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
  /** Cartas por subida de nivel. */
  choices: number;
}

/** Un tomo que se tiene: su nivel y lo que suma cada uno de sus efectos. */
export interface TomeInstance {
  def: TomeDef;
  level: number;
  /** Mejora acumulada de cada efecto (mismo orden que `def.effects`). */
  bonus: number[];
}

export function basePlayerStats(character: CharacterDef, choices = 3): PlayerStats {
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
    choices,
  };
}

/** Estadísticas del jugador con los tomos sumados y los topes aplicados. */
export function computePlayerStats(character: CharacterDef, tomes: readonly TomeInstance[], choices = 3): PlayerStats {
  const base = basePlayerStats(character, choices);
  const stats = { ...base };
  for (const tome of tomes) {
    tome.def.effects.forEach((effect, i) => {
      const bonus = tome.bonus[i] ?? 0;
      stats[effect.stat] += effect.mode === 'base' ? base[effect.stat] * bonus : bonus;
    });
  }
  for (const [stat, max] of Object.entries(PLAYER_STAT_LIMITS) as Array<[TomeStat, number]>) {
    stats[stat] = Math.min(stats[stat], max);
  }
  return stats;
}

/** ¿Está esta estadística del jugador ya en su tope? */
export function isPlayerStatCapped(stats: PlayerStats, stat: TomeStat): boolean {
  const max = PLAYER_STAT_LIMITS[stat];
  return max !== undefined && stats[stat] >= max - 1e-9;
}

export function zeroWeaponStats(): WeaponStats {
  return {
    damage: 0,
    cooldown: 0,
    count: 0,
    area: 0,
    speed: 0,
    duration: 0,
    pierce: 0,
    critChance: 0,
    critMultiplier: 0,
    knockback: 0,
  };
}

/** Estadísticas del arma con sus mejoras acumuladas (sin las del jugador). */
export function applyWeaponBonus(base: WeaponStats, bonus: WeaponStats): WeaponStats {
  const out = { ...base };
  for (const key of Object.keys(WEAPON_UPGRADE_STEPS) as WeaponStatKey[]) {
    const b = bonus[key];
    switch (WEAPON_UPGRADE_STEPS[key].mode) {
      case 'mult':
        out[key] = base[key] * (1 + b);
        break;
      case 'rate':
        out[key] = base[key] / (1 + b);
        break;
      case 'flat':
        out[key] = base[key] + b;
        break;
    }
  }
  return out;
}

/** Estadísticas finales de un arma: las suyas combinadas con las del jugador, con topes. */
export function effectiveWeaponStats(weapon: WeaponStats, player: PlayerStats): WeaponStats {
  const L = WEAPON_STAT_LIMITS;
  return {
    damage: weapon.damage * player.damage,
    cooldown: Math.max(L.minCooldown, weapon.cooldown / Math.max(0.1, player.attackSpeed)),
    count: weapon.count > 0 ? Math.min(L.maxCount, Math.round(weapon.count + player.extraProjectiles)) : 0,
    area: Math.min(L.maxArea, weapon.area * player.area),
    speed: weapon.speed * player.projectileSpeed,
    duration: weapon.duration * player.duration,
    pierce: Math.min(L.maxPierce, Math.round(weapon.pierce)),
    critChance: weapon.critChance + player.critChance,
    critMultiplier: weapon.critMultiplier + player.critDamage,
    knockback: weapon.knockback * player.knockback,
  };
}
