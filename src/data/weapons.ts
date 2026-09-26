// Armas. Cada una tiene un comportamiento (weapons/) y unas estadísticas base.
// Al mejorarlas suben 1–2 estadísticas de su lista `upgradable` (ver upgrades.ts).
import type { TranslationKey } from '../i18n';

export type WeaponId = 'chancla' | 'naftalina' | 'barra' | 'dentaduras' | 'jersey' | 'fregona';
export type WeaponBehaviorId = 'homing' | 'aura' | 'arc' | 'orbit' | 'chain' | 'trail';

export interface WeaponStats {
  /** Daño por golpe. */
  damage: number;
  /** Segundos entre disparos (o entre pulsos, en las auras). */
  cooldown: number;
  /** Proyectiles por disparo (golpes, dentaduras, cadenas... según el arma). */
  count: number;
  /** Multiplicador de tamaño (radio del aura, tamaño del proyectil...). */
  area: number;
  /** Velocidad del proyectil (o de giro, en las órbitas). */
  speed: number;
  /** Duración (s): vuelo del proyectil, tiempo activa, vida del charco... */
  duration: number;
  /** Enemigos extra que atraviesa (saltos, en la cadena). */
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
  /**
   * Intensidad del destello blanco del enemigo al recibir el golpe (0..1). Las
   * armas que golpean a muchos a la vez (auras) usan uno suave: si no, una horda
   * entera destellando a la vez se ve como una mancha blanca que tapa al jugador.
   */
  hitFlash: number;
  /** Estadísticas que pueden subir al mejorar el arma. */
  upgradable: ReadonlyArray<keyof WeaponStats>;
  /** Nombres propios de algunas estadísticas en las cartas (p. ej. "Saltos" en vez de "Perforación"). */
  statLabels?: Readonly<Partial<Record<keyof WeaponStats, TranslationKey>>>;
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
    hitFlash: 1,
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
    hitFlash: 0.35,
    upgradable: ['damage', 'cooldown', 'area', 'critChance', 'knockback'],
  },
  barra: {
    id: 'barra',
    nameKey: 'weapon.barra',
    descriptionKey: 'weapon.barra.desc',
    behavior: 'arc',
    base: {
      damage: 16,
      cooldown: 1.2,
      count: 1,
      area: 1,
      speed: 0,
      duration: 0,
      pierce: 0,
      critChance: 0.08,
      critMultiplier: 2,
      knockback: 7,
    },
    hitFlash: 1,
    upgradable: ['damage', 'cooldown', 'count', 'area', 'critChance', 'knockback'],
    statLabels: { count: 'stat.count.swings' },
  },
  dentaduras: {
    id: 'dentaduras',
    nameKey: 'weapon.dentaduras',
    descriptionKey: 'weapon.dentaduras.desc',
    behavior: 'orbit',
    base: {
      damage: 8,
      cooldown: 2.5,
      count: 2,
      area: 1,
      speed: 1,
      duration: 4,
      pierce: 0,
      critChance: 0.05,
      critMultiplier: 2,
      knockback: 2.5,
    },
    hitFlash: 0.6,
    upgradable: ['damage', 'cooldown', 'count', 'area', 'speed', 'duration', 'critChance'],
    statLabels: { count: 'stat.count.dentures', speed: 'stat.speed.spin' },
  },
  jersey: {
    id: 'jersey',
    nameKey: 'weapon.jersey',
    descriptionKey: 'weapon.jersey.desc',
    behavior: 'chain',
    base: {
      damage: 13,
      cooldown: 1.5,
      count: 1,
      area: 1,
      speed: 0,
      duration: 0,
      pierce: 3,
      critChance: 0.06,
      critMultiplier: 2.2,
      knockback: 0.6,
    },
    hitFlash: 1,
    upgradable: ['damage', 'cooldown', 'count', 'area', 'pierce', 'critChance'],
    statLabels: { count: 'stat.count.chains', pierce: 'stat.pierce.jumps' },
  },
  fregona: {
    id: 'fregona',
    nameKey: 'weapon.fregona',
    descriptionKey: 'weapon.fregona.desc',
    behavior: 'trail',
    base: {
      damage: 5,
      cooldown: 0.4,
      count: 1,
      area: 1,
      speed: 0,
      duration: 3,
      pierce: 0,
      critChance: 0.04,
      critMultiplier: 2,
      knockback: 0,
    },
    hitFlash: 0.35,
    upgradable: ['damage', 'cooldown', 'count', 'area', 'duration', 'critChance'],
    statLabels: { count: 'stat.count.puddles' },
  },
};

export const WEAPON_LIST: readonly WeaponDef[] = Object.values(WEAPONS);

/** Radio del aura con área 1 (m). */
export const AURA_BASE_RADIUS = 3.2;
/** Radio de la chancla con área 1 (m). */
export const PROJECTILE_BASE_RADIUS = 0.35;
