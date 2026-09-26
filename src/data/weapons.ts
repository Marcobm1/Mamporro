// Armas. Cada una tiene un comportamiento (weapons/) y unas estadísticas base.
// Las mejoras por nivel (qué estadísticas suben) se usarán en el hito 3.
import type { TranslationKey } from '../i18n';

export type WeaponId = 'chancla' | 'naftalina';
export type WeaponBehaviorId = 'homing' | 'aura';

export interface WeaponStats {
  /** Daño por golpe. */
  damage: number;
  /** Segundos entre disparos (o entre pulsos, en las auras). */
  cooldown: number;
  /** Proyectiles por disparo. */
  count: number;
  /** Multiplicador de tamaño (radio del aura, tamaño del proyectil...). */
  area: number;
  /** Velocidad del proyectil (m/s). */
  speed: number;
  /** Duración del proyectil (s). */
  duration: number;
  /** Enemigos extra que atraviesa cada proyectil. */
  pierce: number;
  /** Probabilidad de crítico (1 = 100 %; más de 1 da supercríticos). */
  critChance: number;
  /** Multiplicador de daño del crítico. */
  critMultiplier: number;
  /** Empuje al golpear (m/s). */
  knockback: number;
}

export interface WeaponDef {
  id: WeaponId;
  nameKey: TranslationKey;
  descriptionKey: TranslationKey;
  behavior: WeaponBehaviorId;
  base: WeaponStats;
  /** Estadísticas que pueden subir al mejorar el arma (hito 3). */
  upgradable: ReadonlyArray<keyof WeaponStats>;
}

export const WEAPONS: Readonly<Record<WeaponId, WeaponDef>> = {
  chancla: {
    id: 'chancla',
    nameKey: 'weapon.chancla',
    descriptionKey: 'weapon.chancla.desc',
    behavior: 'homing',
    base: {
      damage: 10,
      cooldown: 0.85,
      count: 1,
      area: 1,
      speed: 17,
      duration: 2.2,
      pierce: 1,
      critChance: 0.05,
      critMultiplier: 2,
      knockback: 4,
    },
    upgradable: ['damage', 'cooldown', 'count', 'area', 'speed', 'pierce', 'critChance'],
  },
  naftalina: {
    id: 'naftalina',
    nameKey: 'weapon.naftalina',
    descriptionKey: 'weapon.naftalina.desc',
    behavior: 'aura',
    base: {
      damage: 6,
      cooldown: 0.5,
      count: 0,
      area: 1,
      speed: 0,
      duration: 0,
      pierce: 0,
      critChance: 0.05,
      critMultiplier: 2,
      knockback: 1.2,
    },
    upgradable: ['damage', 'cooldown', 'area', 'critChance', 'knockback'],
  },
};

/** Radio del aura con área 1 (m). */
export const AURA_BASE_RADIUS = 3.2;
/** Radio de la chancla con área 1 (m). */
export const PROJECTILE_BASE_RADIUS = 0.35;
