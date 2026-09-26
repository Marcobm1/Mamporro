// Escalado de la dificultad con el tiempo de partida (lógica pura).
import type { EnemyId } from '../data/enemies';
import { SPAWN_CURVE, SPAWN_TABLE, type SpawnEntry } from '../data/waves';

/** Enemigos que aparecen por segundo en el minuto `minutes`. */
export function spawnRate(minutes: number): number {
  const c = SPAWN_CURVE;
  const t = Math.max(0, minutes);
  return Math.min(c.maxRate, c.baseRate + c.rateGrowth * t + c.rateCurve * t * t);
}

/** Máximo de enemigos vivos a la vez. */
export function maxAlive(minutes: number): number {
  const c = SPAWN_CURVE;
  return Math.min(c.maxAliveCap, Math.round(c.baseMaxAlive + c.maxAliveGrowth * Math.max(0, minutes)));
}

/** Multiplicador de la vida de los enemigos que aparecen en el minuto `minutes`. */
export function enemyHpMultiplier(minutes: number): number {
  const c = SPAWN_CURVE;
  const t = Math.max(0, minutes);
  return 1 + c.hpGrowth * t + c.hpCurve * t * t;
}

/** Multiplicador de la experiencia que sueltan. */
export function enemyXpMultiplier(minutes: number): number {
  return 1 + SPAWN_CURVE.xpGrowth * Math.max(0, minutes);
}

/** Sortea qué enemigo aparece según los pesos de los disponibles en ese minuto. */
export function pickEnemy(minutes: number, random: () => number, table: readonly SpawnEntry[] = SPAWN_TABLE): EnemyId {
  const available = table.filter((e) => e.fromMinute <= minutes);
  const total = available.reduce((sum, e) => sum + e.weight, 0);
  let roll = random() * total;
  for (const entry of available) {
    roll -= entry.weight;
    if (roll < 0) return entry.enemy;
  }
  return (available[available.length - 1] ?? table[0] as SpawnEntry).enemy;
}
