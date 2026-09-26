// Aparición de enemigos y escalado de la dificultad con el tiempo.
// (Las oleadas especiales, élites y el enjambre final llegan en el hito 4.)
import type { EnemyId } from './enemies';

export interface SpawnEntry {
  enemy: EnemyId;
  /** Minuto a partir del cual puede aparecer. */
  fromMinute: number;
  /** Peso relativo en el sorteo de cada aparición. */
  weight: number;
}

export const SPAWN_TABLE: readonly SpawnEntry[] = [
  { enemy: 'pelusa', fromMinute: 0, weight: 10 },
  { enemy: 'cucaracha', fromMinute: 0.75, weight: 4 },
];

export const SPAWN_CURVE = {
  /** Enemigos por segundo al empezar... */
  baseRate: 0.9,
  /** ...más esto por minuto... */
  rateGrowth: 0.55,
  /** ...más esto por minuto al cuadrado. */
  rateCurve: 0.06,
  maxRate: 20,
  /** Enemigos vivos como máximo: base + crecimiento por minuto, con tope. */
  baseMaxAlive: 60,
  maxAliveGrowth: 45,
  maxAliveCap: 450,
  /** Multiplicador de vida: 1 + hpGrowth·min + hpCurve·min². */
  hpGrowth: 0.22,
  hpCurve: 0.02,
  /** Multiplicador de experiencia por minuto (los enemigos tardíos valen más). */
  xpGrowth: 0.12,
  /** Distancia al jugador a la que aparecen (m). */
  spawnDistanceMin: 26,
  spawnDistanceMax: 38,
  /** Si un enemigo se queda más lejos que esto, reaparece cerca del jugador. */
  recycleDistance: 85,
} as const;
