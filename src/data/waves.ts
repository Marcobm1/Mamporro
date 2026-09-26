// Aparición de enemigos y escalado de la dificultad con el tiempo, oleadas
// especiales, élites periódicos y enjambre final.
//
// Todos los minutos de este fichero son "minutos de dificultad": una partida de
// 10 minutos va a ritmo normal; en una de 5 corren el doble de rápido y en una de
// 15, más despacio. Así la curva completa cabe en la duración elegida.
import type { TranslationKey } from '../i18n';
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
  { enemy: 'taper', fromMinute: 2.5, weight: 2.5 },
  { enemy: 'paloma', fromMinute: 4, weight: 3 },
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

/** Duraciones de partida que se pueden elegir (minutos reales). */
export const RUN_DURATIONS = [5, 10, 15] as const;
export type RunMinutes = (typeof RUN_DURATIONS)[number];
/** La curva de dificultad está pensada para esta duración. */
export const REFERENCE_MINUTES = 10;

export type WaveFormation = 'line' | 'ring' | 'arc';

export interface SpecialWave {
  /** Minuto de dificultad. */
  at: number;
  enemy: EnemyId;
  count: number;
  /** Fila que entra por un lado, anillo alrededor o arco por delante. */
  formation: WaveFormation;
  noticeKey: TranslationKey;
}

export const SPECIAL_WAVES: readonly SpecialWave[] = [
  { at: 1.5, enemy: 'cucaracha', count: 30, formation: 'line', noticeKey: 'wave.stampede' },
  { at: 3, enemy: 'taper', count: 8, formation: 'ring', noticeKey: 'wave.tupperware' },
  { at: 4.5, enemy: 'paloma', count: 12, formation: 'arc', noticeKey: 'wave.pigeons' },
  { at: 6, enemy: 'cucaracha', count: 60, formation: 'line', noticeKey: 'wave.stampede' },
  { at: 7.5, enemy: 'taper', count: 14, formation: 'ring', noticeKey: 'wave.tupperware' },
  { at: 9, enemy: 'pelusa', count: 80, formation: 'ring', noticeKey: 'wave.dust' },
];

/** Élites periódicos: minutos de dificultad en los que aparece una Rata de Gimnasio. */
export const ELITE_SCHEDULE = {
  enemy: 'rata' as EnemyId,
  first: 2,
  every: 2,
} as const;

/** Enjambre final: al acabarse el tiempo sin haber vencido al jefe. */
export const SWARM_CONFIG = {
  /** El ritmo de aparición se duplica cada tantos segundos de prórroga. */
  doublingSeconds: 20,
  maxRate: 60,
  maxAlive: 750,
  /** La vida de los enemigos también crece: +x por minuto de prórroga. */
  hpPerMinute: 0.6,
} as const;
