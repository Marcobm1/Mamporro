// Cómo suben las estadísticas de las armas al mejorarlas y configuración de la
// subida de nivel (huecos, usos de Reroll/Saltar/Descartar, pesos del sorteo).
import type { WeaponStats } from './weapons';

export type WeaponStatKey = keyof WeaponStats;

/**
 * Qué significa la mejora acumulada (`bonus`) de cada estadística:
 * - 'mult': valor base × (1 + bonus);
 * - 'rate': valor base ÷ (1 + bonus) (tiempos de recarga: más cadencia);
 * - 'flat': valor base + bonus.
 */
export type StatMode = 'mult' | 'rate' | 'flat';

export interface StatStep {
  mode: StatMode;
  /** Lo que sube una mejora Común (las rarezas lo multiplican). */
  amount: number;
  /** Solo enteros (cantidad, perforación): como mínimo +1 por mejora. */
  integer?: boolean;
  /** Tope de la mejora acumulada del arma; al llegar, deja de salir en las cartas. */
  maxBonus?: number;
  /** Cómo se muestra en las cartas: porcentaje o número. */
  display: 'percent' | 'number';
}

export const WEAPON_UPGRADE_STEPS: Readonly<Record<WeaponStatKey, StatStep>> = {
  damage: { mode: 'mult', amount: 0.25, display: 'percent' },
  cooldown: { mode: 'rate', amount: 0.12, maxBonus: 3, display: 'percent' },
  count: { mode: 'flat', amount: 1, integer: true, maxBonus: 8, display: 'number' },
  area: { mode: 'mult', amount: 0.15, maxBonus: 2, display: 'percent' },
  speed: { mode: 'mult', amount: 0.15, maxBonus: 2, display: 'percent' },
  duration: { mode: 'mult', amount: 0.15, maxBonus: 2, display: 'percent' },
  pierce: { mode: 'flat', amount: 1, integer: true, maxBonus: 15, display: 'number' },
  critChance: { mode: 'flat', amount: 0.05, display: 'percent' },
  critMultiplier: { mode: 'flat', amount: 0.25, display: 'percent' },
  knockback: { mode: 'mult', amount: 0.2, maxBonus: 3, display: 'percent' },
};

/**
 * Límites de las estadísticas finales (arma + jugador), por rendimiento y
 * jugabilidad. No hay tope de nivel: esto es lo único que frena el crecimiento.
 */
export const WEAPON_STAT_LIMITS = {
  minCooldown: 0.1,
  maxCount: 12,
  maxArea: 5,
  maxPierce: 30,
} as const;

export const LEVEL_UP_CONFIG = {
  maxWeapons: 4,
  maxTomes: 4,
  /** Cartas por subida de nivel (un objeto del hito 4 sumará una más, hasta 4). */
  baseChoices: 3,
  maxChoices: 4,
  /** Usos por partida (en el hito 5 se podrán ampliar en la tienda). */
  rerolls: 2,
  skips: 2,
  banishes: 2,
  /** Peso de cada tipo de carta en el sorteo. */
  weights: { newWeapon: 7, weaponUpgrade: 10, newTome: 7, tomeUpgrade: 9 },
  /** Carta de relleno si no queda nada que ofrecer: vida recuperada (fracción de la máxima). */
  fillerHeal: 0.3,
  /** Tiempo tras abrir la pantalla en el que se ignoran teclas y clics (evita elegir sin querer). */
  inputGuard: 0.4,
} as const;
