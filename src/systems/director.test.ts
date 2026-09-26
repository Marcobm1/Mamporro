import { describe, expect, it } from 'vitest';
import { ELITE_SCHEDULE, SPECIAL_WAVES, SWARM_CONFIG, type SpecialWave } from '../data/waves';
import { enemyHpMultiplier, maxAlive, spawnRate } from './difficulty';
import { Director } from './Director';
import type { SpawnParams } from './SpawnSystem';

const NO_MODS = { rate: 1, hp: 1, gold: 1 };
const params = (): SpawnParams => ({ minutes: 0, rate: 0, maxAlive: 0, hp: 1, xp: 1, gold: 1 });

describe('director: tiempo y dificultad', () => {
  it('la curva de dificultad cabe en la duración elegida', () => {
    expect(new Director(10).difficulty(300)).toBeCloseTo(5);
    // En 5 minutos va el doble de rápido; en 15, a dos tercios.
    expect(new Director(5).difficulty(150)).toBeCloseTo(5);
    expect(new Director(15).difficulty(450)).toBeCloseTo(5);
    expect(new Director(10).timeLeft(90)).toBe(510);
    expect(new Director(5).overtime(310)).toBe(10);
    expect(new Director(5).overtime(200)).toBe(0);
  });

  it('en partidas cortas los enemigos dan más experiencia y oro (lo mismo en total)', () => {
    const short = new Director(5).spawnParams(150, NO_MODS, params());
    const normal = new Director(10).spawnParams(300, NO_MODS, params());
    // Mismo minuto de dificultad: misma vida y ritmo, el doble de experiencia y oro.
    expect(short.hp).toBeCloseTo(normal.hp);
    expect(short.rate).toBeCloseTo(normal.rate);
    expect(short.xp).toBeCloseTo(normal.xp * 2);
    expect(short.gold).toBeCloseTo(normal.gold * 2);
  });

  it('los modificadores (tótem, santuario) cambian ritmo, vida y oro', () => {
    const d = new Director(10);
    const base = d.spawnParams(240, NO_MODS, params());
    const mod = d.spawnParams(240, { rate: 2, hp: 1.5, gold: 3 }, params());
    expect(mod.rate).toBeCloseTo(base.rate * 2);
    expect(mod.hp).toBeCloseTo(base.hp * 1.5);
    expect(mod.gold).toBeCloseTo(base.gold * 3);
    expect(mod.maxAlive).toBe(base.maxAlive);
  });
});

describe('director: enjambre final', () => {
  it('sin prórroga sigue la curva normal', () => {
    const p = new Director(10).spawnParams(599, NO_MODS, params());
    const minutes = 599 / 60;
    expect(p.rate).toBeCloseTo(spawnRate(minutes));
    expect(p.maxAlive).toBe(maxAlive(minutes));
    expect(p.hp).toBeCloseTo(enemyHpMultiplier(minutes));
  });

  it('en la prórroga el ritmo se duplica cada pocos segundos, con tope, y la vida crece', () => {
    const d = new Director(10);
    const at = (over: number): SpawnParams => d.spawnParams(600 + over, NO_MODS, params());
    const s = SWARM_CONFIG;
    const ratio = at(s.doublingSeconds).rate / at(0.001).rate;
    // Se duplica (la curva normal apenas cambia en 20 s).
    expect(ratio).toBeGreaterThan(1.95);
    expect(ratio).toBeLessThan(2.2);
    expect(at(5 * s.doublingSeconds).rate).toBe(s.maxRate);
    expect(at(10).maxAlive).toBe(s.maxAlive);
    expect(at(60).hp).toBeGreaterThan(enemyHpMultiplier(11) * 1.5);
  });
});

describe('director: sucesos', () => {
  function collect(d: Director, times: number[]): { waves: SpecialWave[]; elites: number; swarm: number } {
    const out = { waves: [] as SpecialWave[], elites: 0, swarm: 0 };
    for (const time of times) {
      d.update(time, { wave: (w) => out.waves.push(w), elite: () => out.elites++, swarm: () => out.swarm++ });
    }
    return out;
  }

  it('cada oleada especial sale una vez, en orden y en su minuto', () => {
    const d = new Director(10);
    const early = collect(d, [60, 89]);
    expect(early.waves).toHaveLength(0);
    const later = collect(d, [91, 100, 400]);
    expect(later.waves.map((w) => w.at)).toEqual(SPECIAL_WAVES.filter((w) => w.at <= 400 / 60).map((w) => w.at));
    // Volver a llamar no las repite.
    expect(collect(d, [400]).waves).toHaveLength(0);
  });

  it('élites periódicos y un único aviso de enjambre al acabarse el tiempo', () => {
    const d = new Director(5);
    const times = Array.from({ length: 400 }, (_, k) => k);
    const result = collect(d, times);
    // 399 s en una partida de 5 min = 13,3 minutos de dificultad → élites en 2, 4, ..., 12.
    const expected = Math.floor((13.3 - ELITE_SCHEDULE.first) / ELITE_SCHEDULE.every) + 1;
    expect(result.elites).toBe(expected);
    expect(result.swarm).toBe(1);
    expect(d.swarm).toBe(true);
  });
});
