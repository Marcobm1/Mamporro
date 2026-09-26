// Mapa vivo: oro, cofres, santuarios de carga, tótems de desafío y el portal del
// jefe. Cantidades, costes, tiempos y colocación en el mapa.
import type { TranslationKey } from '../i18n';
import type { BonusStat, StatEffect } from './bonuses';

export const GOLD_CONFIG = {
  /** Monedas en el suelo como máximo (si hay más, se suman a la más cercana). */
  capacity: 300,
  /** Oro de la carta de relleno: fracción del precio del siguiente cofre. */
  fillerChestFraction: 0.5,
  /** Mínimo de la carta de relleno. */
  fillerMin: 10,
} as const;

export const CHEST_CONFIG = {
  /** Precio del primer cofre; cada cofre abierto encarece el siguiente. */
  baseCost: 15,
  costStep: 12,
  costCurve: 2.5,
  /** Distancia (desde su centro) a la que se puede abrir. */
  reach: 2.4,
} as const;

/** Precio del cofre número `opened + 1`. */
export function chestCost(opened: number): number {
  const c = CHEST_CONFIG;
  const n = Math.max(0, opened);
  return Math.round(c.baseCost + c.costStep * n + c.costCurve * n * n);
}

export const SHRINE_CONFIG = {
  /** Radio de la zona de carga (m). */
  radius: 4.2,
  /** Segundos dentro de la zona para cargarlo del todo. */
  chargeTime: 9,
  /** Fuera de la zona se descarga, más despacio de lo que carga. */
  decayRate: 0.35,
  /** Mientras se carga aparecen más enemigos ("mientras te atacan"). */
  spawnMultiplier: 1.7,
  /** Bendiciones a elegir al completarlo. */
  choices: 3,
} as const;

/** Bendición de un santuario: sube una estadística (la rareza la multiplica, como a los tomos). */
export interface ShrineBoost {
  id: string;
  nameKey: TranslationKey;
  effect: StatEffect;
}

const boost = (id: string, stat: BonusStat, amount: number, display: StatEffect['display'], integer = false): ShrineBoost => ({
  id,
  nameKey: `boost.${id}` as TranslationKey,
  effect: { stat, amount, mode: 'add', display, integer },
});

export const SHRINE_BOOSTS: readonly ShrineBoost[] = [
  boost('damage', 'damage', 0.1, 'percent'),
  boost('attackSpeed', 'attackSpeed', 0.08, 'percent'),
  boost('area', 'area', 0.1, 'percent'),
  boost('maxHp', 'maxHp', 15, 'number', true),
  boost('regen', 'regen', 0.5, 'number'),
  boost('moveSpeed', 'moveSpeed', 0.06, 'percent'),
  boost('critChance', 'critChance', 0.05, 'percent'),
  boost('critDamage', 'critDamage', 0.2, 'percent'),
  boost('armor', 'armor', 5, 'number', true),
  boost('luck', 'luck', 10, 'number', true),
  boost('xpGain', 'xpGain', 0.08, 'percent'),
  boost('goldGain', 'goldGain', 0.15, 'percent'),
];

export const TOTEM_CONFIG = {
  /** Duración del desafío (s). */
  duration: 45,
  /** Durante el desafío: más enemigos, con más vida... */
  spawnMultiplier: 2.2,
  hpMultiplier: 1.25,
  /** ...pero más oro y más suerte (cartas y cofres mejores). */
  goldMultiplier: 2,
  luck: 50,
  /** Al superarlo, un objeto gratis sorteado con esta suerte extra. */
  rewardLuck: 100,
  reach: 2.4,
} as const;

export const PORTAL_CONFIG = {
  reach: 2.8,
  /** El jefe aparece a esta distancia del portal, por detrás (el lado contrario al jugador). */
  bossDistance: 4,
} as const;

/** Tipos de elementos interactuables del mapa. */
export type InteractableKind = 'chest' | 'shrine' | 'totem' | 'portal';

export interface InteractablePlacement {
  kind: InteractableKind;
  count: number;
  /** Distancia mínima al inicio y máxima (fracción del radio jugable, para el portal). */
  minSpawnDistance: number;
  /** Separación mínima con los demás interactuables. */
  spacing: number;
  /** Radio libre de obstáculos que necesita. */
  clearRadius: number;
}

export const INTERACTABLE_PLACEMENT: readonly InteractablePlacement[] = [
  // El portal, primero: el sitio más escondido (lejos del inicio).
  { kind: 'portal', count: 1, minSpawnDistance: 95, spacing: 20, clearRadius: 2.2 },
  { kind: 'shrine', count: 3, minSpawnDistance: 30, spacing: 26, clearRadius: 4.6 },
  { kind: 'totem', count: 2, minSpawnDistance: 34, spacing: 26, clearRadius: 2 },
  { kind: 'chest', count: 14, minSpawnDistance: 14, spacing: 13, clearRadius: 1.6 },
];

export const DISCOVERY_CONFIG = {
  /** Un interactuable aparece en el minimapa al acercarse a esta distancia. */
  radius: 30,
  /** El portal, escondido, hay que verlo más de cerca. */
  portalRadius: 22,
} as const;
