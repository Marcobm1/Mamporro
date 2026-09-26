// Director de la partida (lógica pura): el temporizador, la dificultad según el
// tiempo, las oleadas especiales, los élites periódicos y el enjambre final.
import { ELITE_SCHEDULE, REFERENCE_MINUTES, SPECIAL_WAVES, SWARM_CONFIG, type RunMinutes, type SpecialWave } from '../data/waves';
import { enemyHpMultiplier, enemyXpMultiplier, maxAlive, spawnRate } from './difficulty';
import type { SpawnParams } from './SpawnSystem';

/** Cambios temporales al ritmo de aparición (tótem de desafío, santuario cargándose...). */
export interface SpawnModifiers {
  rate: number;
  hp: number;
  gold: number;
}

/** Lo que el director pide a la partida en un tick. */
export interface DirectorEvents {
  wave(wave: SpecialWave): void;
  elite(): void;
  swarm(): void;
}

export class Director {
  /** Duración de la partida (s). */
  readonly duration: number;
  /**
   * Ritmo: en una partida corta la curva de dificultad avanza más deprisa, y los
   * enemigos dan más experiencia y oro para compensar (igual en total).
   */
  readonly pace: number;
  swarm = false;
  private nextWave = 0;
  private nextElite: number = ELITE_SCHEDULE.first;

  constructor(readonly minutes: RunMinutes) {
    this.duration = minutes * 60;
    this.pace = REFERENCE_MINUTES / minutes;
  }

  /** Minutos de dificultad a los `time` segundos de partida. */
  difficulty(time: number): number {
    return (Math.max(0, time) / 60) * this.pace;
  }

  /** Segundos que quedan (negativo durante el enjambre final). */
  timeLeft(time: number): number {
    return this.duration - time;
  }

  /** Segundos de prórroga (enjambre final). */
  overtime(time: number): number {
    return Math.max(0, time - this.duration);
  }

  /** Cómo aparecen los enemigos a los `time` segundos, con los modificadores dados. */
  spawnParams(time: number, mods: SpawnModifiers, out: SpawnParams): SpawnParams {
    const minutes = this.difficulty(time);
    const over = this.overtime(time);
    out.minutes = minutes;
    out.hp = enemyHpMultiplier(minutes) * mods.hp;
    out.xp = enemyXpMultiplier(minutes) * this.pace;
    out.gold = mods.gold * this.pace;
    if (over > 0) {
      // Enjambre final: el ritmo se duplica cada pocos segundos y la vida sigue creciendo.
      const s = SWARM_CONFIG;
      out.rate = Math.min(s.maxRate, spawnRate(minutes) * Math.pow(2, over / s.doublingSeconds)) * mods.rate;
      out.maxAlive = s.maxAlive;
      out.hp *= 1 + (s.hpPerMinute * over) / 60;
    } else {
      out.rate = spawnRate(minutes) * mods.rate;
      out.maxAlive = maxAlive(minutes);
    }
    return out;
  }

  /** Avanza hasta `time` segundos y lanza las oleadas, élites y enjambre que tocan. */
  update(time: number, events: DirectorEvents): void {
    const minutes = this.difficulty(time);
    while (this.nextWave < SPECIAL_WAVES.length && (SPECIAL_WAVES[this.nextWave] as SpecialWave).at <= minutes) {
      events.wave(SPECIAL_WAVES[this.nextWave] as SpecialWave);
      this.nextWave++;
    }
    while (this.nextElite <= minutes) {
      events.elite();
      this.nextElite += ELITE_SCHEDULE.every;
    }
    if (!this.swarm && time >= this.duration) {
      this.swarm = true;
      events.swarm();
    }
  }
}
