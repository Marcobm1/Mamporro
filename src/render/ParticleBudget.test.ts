import { expect, it } from 'vitest';
import { ParticleBudget } from './ParticleBudget';
it('acota una ráfaga de muertes y no supera capacidad en varios frames', () => {
  const budget = new ParticleBudget();
  let active = 0;
  for (let frame = 0; frame < 30; frame++) {
    budget.reset();
    let emitted = 0;
    for (let i = 0; i < 1000; i++) { const n = budget.take(12, active); active += n; emitted += n; }
    expect(emitted).toBeLessThanOrEqual(256);
    expect(active).toBeLessThanOrEqual(1500);
  }
  expect(active).toBe(1500);
});
it('reduce densidad y presupuesto sin admitir partículas si ya está lleno', () => {
  const budget = new ParticleBudget();
  budget.reduced = true;
  budget.reset();
  expect(budget.take(12, 0)).toBe(3);
  expect(budget.take(1000, 3)).toBe(61);
  expect(budget.take(1, 64)).toBe(0);
  budget.reset();
  expect(budget.take(12, 399)).toBe(1);
  expect(budget.take(12, 500)).toBe(0);
});
