import { describe, expect, it } from 'vitest';
import { WORLD_CONFIG } from '../data/config';
import { ColliderGrid, decorationColliders } from './decorations';
import { squircle } from './Heightfield';
import { generateWorldData } from './World';

const world = generateWorldData('DECO-TEST');

describe('decoración', () => {
  it('es determinista para la misma semilla', () => {
    const again = generateWorldData('DECO-TEST');
    expect(again.decorations).toEqual(world.decorations);
  });

  it('coloca una cantidad razonable de cada tipo', () => {
    const count = (kind: string): number => world.decorations.filter((d) => d.kind === kind).length;
    expect(count('tree') + count('pine')).toBeGreaterThan(150);
    expect(count('rock')).toBeGreaterThan(20);
    expect(count('bush')).toBeGreaterThan(50);
  });

  it('deja libre la zona de inicio y no se sale del área jugable', () => {
    for (const d of world.decorations) {
      expect(Math.hypot(d.x, d.z)).toBeGreaterThanOrEqual(WORLD_CONFIG.clearSpawnRadius);
      expect(squircle(d.x, d.z)).toBeLessThanOrEqual(WORLD_CONFIG.playableRadius);
    }
  });

  it('solo árboles, pinos y rocas tienen colisión; solo las rocas se pueden escalar', () => {
    const colliders = decorationColliders(world.decorations);
    const solid = world.decorations.filter((d) => d.kind !== 'bush').length;
    expect(colliders).toHaveLength(solid);
    const rocks = world.decorations.filter((d) => d.kind === 'rock').length;
    expect(colliders.filter((c) => c.standable)).toHaveLength(rocks);
  });

  it('ColliderGrid.query() encuentra todo lo que toca el círculo (igual que fuerza bruta)', () => {
    const colliders = decorationColliders(world.decorations);
    const grid = new ColliderGrid(colliders, 160);
    const found: number[] = [];
    for (let n = 0; n < 300; n++) {
      const x = ((n * 97) % 260) - 130;
      const z = ((n * 53) % 260) - 130;
      const r = 1 + (n % 4);
      const result = new Set(grid.query(x, z, r, found));
      colliders.forEach((c, index) => {
        if (Math.hypot(c.x - x, c.z - z) < c.radius + r) expect(result.has(index)).toBe(true);
      });
      expect(result.size).toBe(found.length); // Sin duplicados.
    }
  });
});
