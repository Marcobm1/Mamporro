// Colocación determinista de la decoración (árboles, pinos, rocas, arbustos) y
// sus colisionadores cilíndricos. Lógica pura, sin Three.js.
import { createNoise2D } from 'simplex-noise';
import { DEG2RAD, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';
import { circleCollider, type CircleCollider } from './colliders';
import { squircle, type Heightfield } from './Heightfield';

export type DecorationKind = 'tree' | 'pine' | 'rock' | 'bush';

export interface Decoration {
  kind: DecorationKind;
  x: number;
  /** Altura de la base del modelo. */
  y: number;
  z: number;
  scale: number;
  /** Giro sobre el eje vertical (radianes). */
  rotation: number;
  /** Variante visual (modelo de roca, color de copa...), en [0, 1). */
  variant: number;
}

export interface DecorationOptions {
  spawnRadius: number;
  /** Radio squircle del área jugable. */
  limit: number;
  /** Separación de la rejilla de candidatos (m). */
  cellSize?: number;
  /** Zonas reservadas (ruinas, granjas...) donde no se coloca vegetación. */
  isReserved?: (x: number, z: number) => boolean;
}

/** Proporciones de los modelos de roca: el collider y el mesh las comparten. */
export const ROCK_SHAPE = { heightScale: 0.85, sink: 0.2, radius: 0.85 } as const;

export function placeDecorations(hf: Heightfield, rng: Rng, options: DecorationOptions): Decoration[] {
  const forest = createNoise2D(() => rng.next());
  const cell = options.cellSize ?? 5.5;
  const count = Math.ceil((options.limit * 2) / cell);
  const start = -options.limit;
  const cosTree = Math.cos(28 * DEG2RAD);
  const cosBush = Math.cos(34 * DEG2RAD);
  const cosRock = Math.cos(50 * DEG2RAD);
  const normal: Vec3Like = { x: 0, y: 1, z: 0 };
  const result: Decoration[] = [];

  for (let gz = 0; gz < count; gz++) {
    for (let gx = 0; gx < count; gx++) {
      // Siempre se consumen los mismos números por celda: el resultado solo
      // depende de la semilla, no de qué ramas se tomen.
      const jx = rng.next();
      const jz = rng.next();
      const roll = rng.next();
      const s = rng.next();
      const rotation = rng.next() * Math.PI * 2;
      const variant = rng.next();

      const x = start + (gx + 0.15 + jx * 0.7) * cell;
      const z = start + (gz + 0.15 + jz * 0.7) * cell;
      if (squircle(x, z) > options.limit - 3) continue;
      if (Math.hypot(x, z) < options.spawnRadius) continue;
      if (options.isReserved?.(x, z)) continue;

      const ground = hf.heightAt(x, z);
      hf.normalAt(x, z, normal);
      const density = forest(x * 0.018, z * 0.018) * 0.5 + 0.5;
      const treeChance = density * density * 0.7;
      const rockChance = normal.y < Math.cos(30 * DEG2RAD) ? 0.08 : 0.035;
      const bushChance = 0.1;

      if (normal.y > cosTree && roll < treeChance) {
        const pine = ground > 17 || variant < 0.22;
        result.push({
          kind: pine ? 'pine' : 'tree',
          x,
          y: ground - 0.15,
          z,
          scale: pine ? 0.9 + s * 0.5 : 0.8 + s * 0.5,
          rotation,
          variant,
        });
      } else if (normal.y > cosRock && roll > 1 - rockChance) {
        // La mayoría de rocas se pueden escalar de un salto; unas pocas son enormes.
        const scale = s > 0.95 ? 3.4 : 0.8 + s * s * 1.8;
        result.push({ kind: 'rock', x, y: ground - ROCK_SHAPE.sink * scale, z, scale, rotation, variant });
      } else if (normal.y > cosBush && roll > 1 - rockChance - bushChance) {
        result.push({ kind: 'bush', x, y: ground - 0.1, z, scale: 0.7 + s * 0.5, rotation, variant });
      }
    }
  }
  return result;
}

export function decorationColliders(decorations: readonly Decoration[]): CircleCollider[] {
  const colliders: CircleCollider[] = [];
  for (const d of decorations) {
    switch (d.kind) {
      case 'tree':
      case 'pine':
        colliders.push(circleCollider(d.x, d.z, 0.3 * d.scale + 0.05, d.y - 1, d.y + 12, false));
        break;
      case 'rock':
        colliders.push(
          circleCollider(
            d.x,
            d.z,
            ROCK_SHAPE.radius * d.scale,
            d.y - d.scale,
            d.y + ROCK_SHAPE.heightScale * d.scale * 0.92,
            true,
          ),
        );
        break;
      case 'bush':
        break;
    }
  }
  return colliders;
}
