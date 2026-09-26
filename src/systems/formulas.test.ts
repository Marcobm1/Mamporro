import { describe, expect, it } from 'vitest';
import { Rng } from '../core/rng';
import { mitigate, rollDamage } from './damage';
import { enemyHpMultiplier, maxAlive, pickEnemy, spawnRate } from './difficulty';
import { addExperience, xpToNextLevel } from './progression';

describe('daño y críticos', () => {
  it('sin probabilidad de crítico, nunca hay crítico', () => {
    const rng = new Rng('a');
    for (let i = 0; i < 1000; i++) {
      const r = rollDamage(10, 0, 2, () => rng.next());
      expect(r.critLevel).toBe(0);
      expect(r.amount).toBe(10);
    }
  });

  it('con 100 % siempre es crítico y multiplica el daño', () => {
    const r = rollDamage(10, 1, 2.5, () => 0.99);
    expect(r.critLevel).toBe(1);
    expect(r.amount).toBe(25);
  });

  it('la frecuencia de críticos coincide con la probabilidad', () => {
    const rng = new Rng('frecuencia');
    let crits = 0;
    const n = 20000;
    for (let i = 0; i < n; i++) if (rollDamage(1, 0.3, 2, () => rng.next()).critLevel > 0) crits++;
    expect(crits / n).toBeGreaterThan(0.28);
    expect(crits / n).toBeLessThan(0.32);
  });

  it('por encima del 100 % hay supercríticos (niveles que suman daño)', () => {
    const rng = new Rng('super');
    const levels = [0, 0, 0];
    for (let i = 0; i < 10000; i++) {
      const r = rollDamage(10, 1.5, 2, () => rng.next());
      levels[r.critLevel] = (levels[r.critLevel] ?? 0) + 1;
      expect(r.amount).toBe(10 * (1 + r.critLevel));
    }
    expect(levels[0]).toBe(0);
    expect((levels[2] ?? 0) / 10000).toBeGreaterThan(0.47);
    expect((levels[2] ?? 0) / 10000).toBeLessThan(0.53);
  });

  it('la armadura reduce el daño de forma decreciente', () => {
    expect(mitigate(100, 0)).toBe(100);
    expect(mitigate(100, 100)).toBe(50);
    expect(mitigate(100, -50)).toBe(100);
    expect(mitigate(100, 20)).toBeGreaterThan(mitigate(100, 40));
  });
});

describe('experiencia y niveles', () => {
  it('cada nivel cuesta más que el anterior', () => {
    for (let l = 1; l < 60; l++) expect(xpToNextLevel(l + 1)).toBeGreaterThan(xpToNextLevel(l));
    expect(xpToNextLevel(1)).toBe(10);
  });

  it('se pueden subir varios niveles de golpe y el sobrante se conserva', () => {
    const state = { level: 1, xp: 0 };
    const total = xpToNextLevel(1) + xpToNextLevel(2) + 3;
    expect(addExperience(state, total)).toBe(2);
    expect(state.level).toBe(3);
    expect(state.xp).toBe(3);
  });
});

describe('escalado de la dificultad', () => {
  it('aparecen más enemigos, más resistentes, según avanza la partida', () => {
    for (let m = 0; m < 12; m++) {
      expect(spawnRate(m + 1)).toBeGreaterThanOrEqual(spawnRate(m));
      expect(maxAlive(m + 1)).toBeGreaterThanOrEqual(maxAlive(m));
      expect(enemyHpMultiplier(m + 1)).toBeGreaterThan(enemyHpMultiplier(m));
    }
    expect(enemyHpMultiplier(0)).toBe(1);
    expect(maxAlive(100)).toBe(450);
    expect(spawnRate(1000)).toBe(20);
  });

  it('solo aparecen los enemigos desbloqueados en cada minuto, según su peso', () => {
    const rng = new Rng('sorteo');
    for (let i = 0; i < 200; i++) expect(pickEnemy(0.2, () => rng.next())).toBe('pelusa');
    let roaches = 0;
    const n = 7000;
    for (let i = 0; i < n; i++) if (pickEnemy(5, () => rng.next()) === 'cucaracha') roaches++;
    // Pesos 10 y 4 → 4/14 ≈ 28,6 %.
    expect(roaches / n).toBeGreaterThan(0.26);
    expect(roaches / n).toBeLessThan(0.31);
  });
});
