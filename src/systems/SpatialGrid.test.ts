import { describe, expect, it } from 'vitest';
import { Rng } from '../core/rng';
import { SpatialGrid } from './SpatialGrid';

function randomPoints(n: number, seed: string): { xs: Float32Array; zs: Float32Array } {
  const rng = new Rng(seed);
  const xs = new Float32Array(n);
  const zs = new Float32Array(n);
  for (let i = 0; i < n; i++) {
    xs[i] = rng.range(-150, 150);
    zs[i] = rng.range(-150, 150);
  }
  return { xs, zs };
}

describe('rejilla espacial de enemigos', () => {
  const n = 700;
  const { xs, zs } = randomPoints(n, 'rejilla');
  const grid = new SpatialGrid(320, 4, 1000);
  grid.rebuild(xs, zs, n);

  it('queryRadius devuelve exactamente los puntos dentro del radio', () => {
    const out = new Int32Array(1000);
    const rng = new Rng('consultas');
    for (let q = 0; q < 100; q++) {
      const x = rng.range(-160, 160);
      const z = rng.range(-160, 160);
      const r = rng.range(0.5, 20);
      const got = new Set(Array.from(out.subarray(0, grid.queryRadius(x, z, r, out))));
      for (let i = 0; i < n; i++) {
        const inside = Math.hypot((xs[i] as number) - x, (zs[i] as number) - z) <= r;
        expect(got.has(i)).toBe(inside);
      }
    }
  });

  it('nearest encuentra el más cercano (con y sin filtro), igual que la fuerza bruta', () => {
    const rng = new Rng('cercano');
    for (let q = 0; q < 100; q++) {
      const x = rng.range(-160, 160);
      const z = rng.range(-160, 160);
      const maxR = rng.range(5, 60);
      const accept = (i: number): boolean => i % 3 !== 0;
      for (const filter of [undefined, accept]) {
        let best = -1;
        let bestD = maxR;
        for (let i = 0; i < n; i++) {
          if (filter && !filter(i)) continue;
          const d = Math.hypot((xs[i] as number) - x, (zs[i] as number) - z);
          if (d < bestD) {
            bestD = d;
            best = i;
          }
        }
        expect(grid.nearest(x, z, maxR, filter)).toBe(best);
      }
    }
  });
});
