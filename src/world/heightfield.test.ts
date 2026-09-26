import { describe, expect, it } from 'vitest';
import { DEG2RAD, type Vec3Like } from '../core/math';
import { Rng } from '../core/rng';
import { TERRAIN_CONFIG } from '../data/config';
import { generateHeightfield, squircle } from './Heightfield';

const hf = generateHeightfield(TERRAIN_CONFIG, new Rng('TEST-MAP'));
const normal: Vec3Like = { x: 0, y: 1, z: 0 };

describe('Heightfield', () => {
  it('es determinista para la misma semilla y distinto con otra', () => {
    const again = generateHeightfield(TERRAIN_CONFIG, new Rng('TEST-MAP'));
    const other = generateHeightfield(TERRAIN_CONFIG, new Rng('OTRO-MAPA'));
    expect(again.heights).toEqual(hf.heights);
    expect(other.heights).not.toEqual(hf.heights);
  });

  it('heightAt() coincide con los vértices de la rejilla', () => {
    for (let n = 0; n < 200; n++) {
      const i = (n * 37) % hf.stride;
      const j = (n * 91) % hf.stride;
      const x = i * hf.cellSize - hf.half;
      const z = j * hf.cellSize - hf.half;
      expect(hf.heightAt(x, z)).toBeCloseTo(hf.vertexHeight(i, j), 4);
    }
  });

  it('heightAt() es continua a través de la diagonal de cada celda', () => {
    const eps = 1e-4;
    for (let n = 0; n < 100; n++) {
      const i = 10 + ((n * 13) % 200);
      const j = 10 + ((n * 29) % 200);
      const t = ((n * 7) % 10) / 10;
      // Punto sobre la diagonal u + v = 1 de la celda (i, j).
      const x = (i + t) * hf.cellSize - hf.half;
      const z = (j + 1 - t) * hf.cellSize - hf.half;
      const d = eps * hf.cellSize;
      expect(hf.heightAt(x - d, z - d)).toBeCloseTo(hf.heightAt(x + d, z + d), 2);
    }
  });

  it('normalAt() es unitaria, apunta hacia arriba y coincide con la pendiente', () => {
    for (let n = 0; n < 200; n++) {
      // Puntos en el interior de un triángulo (lejos de las aristas).
      const i = 5 + ((n * 17) % 240);
      const j = 5 + ((n * 23) % 240);
      const x = (i + 0.2) * hf.cellSize - hf.half;
      const z = (j + 0.25) * hf.cellSize - hf.half;
      hf.normalAt(x, z, normal);
      expect(Math.hypot(normal.x, normal.y, normal.z)).toBeCloseTo(1, 6);
      expect(normal.y).toBeGreaterThan(0);
      const d = 0.01;
      const dhdx = (hf.heightAt(x + d, z) - hf.heightAt(x - d, z)) / (2 * d);
      const dhdz = (hf.heightAt(x, z + d) - hf.heightAt(x, z - d)) / (2 * d);
      expect(-normal.x / normal.y).toBeCloseTo(dhdx, 3);
      expect(-normal.z / normal.y).toBeCloseTo(dhdz, 3);
    }
  });

  it('las montañas del borde son mucho más altas que el interior', () => {
    let border = 0;
    let inner = 0;
    let count = 0;
    for (let a = 0; a < Math.PI * 2; a += 0.1) {
      const dirX = Math.cos(a);
      const dirZ = Math.sin(a);
      const k = 1 / squircle(dirX, dirZ);
      border += hf.heightAt(dirX * k * 156, dirZ * k * 156);
      inner += hf.heightAt(dirX * k * 60, dirZ * k * 60);
      count++;
    }
    expect(border / count).toBeGreaterThan(inner / count + 20);
  });

  it('la zona de inicio es casi llana', () => {
    for (let x = -6; x <= 6; x += 1.5) {
      for (let z = -6; z <= 6; z += 1.5) {
        hf.normalAt(x, z, normal);
        expect(Math.acos(normal.y)).toBeLessThan(15 * DEG2RAD);
      }
    }
  });

  it('el interior tiene sobre todo suelo caminable, con algunos acantilados', () => {
    let walkable = 0;
    let steep = 0;
    let total = 0;
    for (let x = -120; x <= 120; x += 1.7) {
      for (let z = -120; z <= 120; z += 1.7) {
        if (squircle(x, z) > 120) continue;
        hf.normalAt(x, z, normal);
        const slope = Math.acos(normal.y);
        if (slope < 48 * DEG2RAD) walkable++;
        else steep++;
        total++;
      }
    }
    expect(walkable / total).toBeGreaterThan(0.7);
    expect(steep / total).toBeGreaterThan(0.01);
  });
});
