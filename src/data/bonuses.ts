// Bonificaciones de estadísticas del jugador: las usan los tomos, los objetos y
// las bendiciones de los santuarios. Todas se suman y comparten los mismos topes.

/** Estadísticas del jugador que se pueden mejorar. */
export type BonusStat =
  | 'damage'
  | 'attackSpeed'
  | 'extraProjectiles'
  | 'area'
  | 'moveSpeed'
  | 'maxHp'
  | 'regen'
  | 'luck'
  | 'pickupRadius'
  | 'xpGain'
  | 'critChance'
  | 'critDamage'
  | 'armor'
  | 'goldGain'
  | 'choices';

export interface StatEffect {
  stat: BonusStat;
  /** Lo que suma una unidad (un nivel Común, una copia del objeto...). */
  amount: number;
  /** 'add': se suma tal cual; 'base': fracción del valor base del personaje. */
  mode: 'add' | 'base';
  /** Solo enteros: como mínimo +1. */
  integer?: boolean;
  /** Cómo se muestra en cartas y pausa. */
  display: 'percent' | 'number';
}

/**
 * Topes de las estadísticas del jugador, por rendimiento y jugabilidad (no hay
 * tope de nivel). Algo que solo sube estadísticas al tope deja de ofrecerse.
 */
export const PLAYER_STAT_LIMITS: Readonly<Partial<Record<BonusStat, number>>> = {
  attackSpeed: 4,
  extraProjectiles: 6,
  area: 3,
  moveSpeed: 1.8,
  pickupRadius: 25,
  regen: 15,
  critChance: 3,
  armor: 200,
  choices: 4,
};
