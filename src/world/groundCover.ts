// Hierba y flores: muchos elementos pequeños sin colisión que dan vida al suelo.
// Lógica pura (colocación determinista); el render va en GroundCoverMeshes.
import { createNoise2D } from 'simplex-noise';
import { DEG2RAD, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';
import { squircle, type Heightfield } from './Heightfield';

export interface CoverInstance {
  x: number;
  y: number;
  z: number;
  scale: number;
  rotation: number;
  /** Variante (color de flor), entero. */
  variant: number;
}

export interface GroundCover {
  grass: CoverInstance[];
  flowers: CoverInstance[];
}

export interface GroundCoverOptions {
  limit: number;
  /** Separación de la rejilla de candidatos (m). */
  cell?: number;
  isReserved?: (x: number, z: number) => boolean;
}

export const FLOWER_VARIANTS = 4;

export function placeGroundCover(hf: Heightfield, rng: Rng, options: GroundCoverOptions): GroundCover {
  const meadow = createNoise2D(() => rng.next());
  const cell = options.cell ?? 2.2;
  const count = Math.ceil((options.limit * 2) / cell);
  const cosMax = Math.cos(32 * DEG2RAD);
  const normal: Vec3Like = { x: 0, y: 1, z: 0 };
  const cover: GroundCover = { grass: [], flowers: [] };

  for (let gz = 0; gz < count; gz++) {
    for (let gx = 0; gx < count; gx++) {
      const jx = rng.next();
      const jz = rng.next();
      const roll = rng.next();
      const s = rng.next();
      const rotation = rng.next() * Math.PI * 2;
      const pick = rng.next();
      const x = -options.limit + (gx + jx) * cell;
      const z = -options.limit + (gz + jz) * cell;
      if (squircle(x, z) > options.limit - 2) continue;
      if (options.isReserved?.(x, z)) continue;
      hf.normalAt(x, z, normal);
      if (normal.y < cosMax) continue;
      const y = hf.heightAt(x, z);
      if (y > 34) continue; // En las montañas del borde no crece nada.
      // Praderas con flores en unas zonas, matas de hierba en casi todas.
      const bloom = meadow(x * 0.025, z * 0.025) * 0.5 + 0.5;
      if (roll < 0.1 + bloom * bloom * 0.35) {
        cover.flowers.push({ x, y, z, scale: 0.8 + s * 0.5, rotation, variant: Math.floor(pick * FLOWER_VARIANTS) });
      } else if (roll < 0.62) {
        cover.grass.push({ x, y, z, scale: 0.7 + s * 0.7, rotation, variant: 0 });
      }
    }
  }
  return cover;
}
