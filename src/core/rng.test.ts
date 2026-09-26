import { describe, expect, it } from 'vitest';
import { normalizeSeed, randomSeed, Rng, SEED_MAX_LENGTH } from './rng';

describe('Rng', () => {
  it('es determinista para una misma semilla', () => {
    const a = new Rng('ABC123');
    const b = new Rng('ABC123');
    for (let i = 0; i < 200; i++) expect(a.next()).toBe(b.next());
  });

  it('semillas distintas producen secuencias distintas', () => {
    const a = new Rng('SEMILLA-A');
    const b = new Rng('SEMILLA-B');
    const same = Array.from({ length: 50 }, () => a.next() === b.next()).filter(Boolean).length;
    expect(same).toBeLessThan(2);
  });

  it('next() siempre está en [0, 1)', () => {
    const rng = new Rng('rango');
    for (let i = 0; i < 20000; i++) {
      const v = rng.next();
      expect(v).toBeGreaterThanOrEqual(0);
      expect(v).toBeLessThan(1);
    }
  });

  it('int() respeta los límites e incluye ambos extremos', () => {
    const rng = new Rng('enteros');
    const seen = new Set<number>();
    for (let i = 0; i < 5000; i++) {
      const v = rng.int(3, 7);
      expect(v).toBeGreaterThanOrEqual(3);
      expect(v).toBeLessThanOrEqual(7);
      seen.add(v);
    }
    expect([...seen].sort()).toEqual([3, 4, 5, 6, 7]);
  });

  it('tiene una distribución aproximadamente uniforme', () => {
    const rng = new Rng('uniforme');
    const buckets = new Array<number>(10).fill(0);
    const samples = 100000;
    for (let i = 0; i < samples; i++) {
      const index = Math.floor(rng.next() * 10);
      buckets[index] = (buckets[index] ?? 0) + 1;
    }
    for (const count of buckets) {
      expect(Math.abs(count - samples / 10)).toBeLessThan(samples / 10 * 0.05);
    }
  });

  it('derive() solo depende de la semilla y la etiqueta', () => {
    const rng = new Rng('BASE');
    const first = rng.derive('mapa').next();
    rng.next();
    rng.next();
    expect(rng.derive('mapa').next()).toBe(first);
    expect(rng.derive('ofertas').next()).not.toBe(first);
  });

  it('shuffle() devuelve una permutación de la lista', () => {
    const rng = new Rng('baraja');
    const items = Array.from({ length: 30 }, (_, i) => i);
    const shuffled = rng.shuffle(items.slice());
    expect(shuffled).not.toEqual(items);
    expect(shuffled.slice().sort((a, b) => a - b)).toEqual(items);
  });

  it('pick() falla con una lista vacía', () => {
    expect(() => new Rng('x').pick([])).toThrow();
  });
});

describe('semillas', () => {
  it('randomSeed() genera semillas legibles del tamaño pedido', () => {
    for (let i = 0; i < 50; i++) {
      const seed = randomSeed(8);
      expect(seed).toMatch(/^[2-9A-HJ-NP-Z]{8}$/);
    }
  });

  it('normalizeSeed() limpia lo que escribe el jugador', () => {
    expect(normalizeSeed('  abc-12 3 ')).toBe('ABC123');
    expect(normalizeSeed('ñandú')).toBe('AND');
    expect(normalizeSeed('   ')).toBeNull();
    expect(normalizeSeed('X'.repeat(40))).toHaveLength(SEED_MAX_LENGTH);
  });
});
