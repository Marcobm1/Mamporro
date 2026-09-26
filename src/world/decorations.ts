// Colocación determinista de la decoración (árboles, pinos, rocas, arbustos) y
// sus colisionadores cilíndricos. Lógica pura, sin Three.js.
import { createNoise2D } from 'simplex-noise';
import { DEG2RAD, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';
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

/** Cilindro vertical de colisión. */
export interface Collider {
  x: number;
  z: number;
  radius: number;
  bottom: number;
  top: number;
  /** Si es true, se puede estar de pie encima (rocas). */
  standable: boolean;
}

export interface DecorationOptions {
  spawnRadius: number;
  /** Radio squircle del área jugable. */
  limit: number;
  /** Separación de la rejilla de candidatos (m). */
  cellSize?: number;
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

export function decorationColliders(decorations: readonly Decoration[]): Collider[] {
  const colliders: Collider[] = [];
  for (const d of decorations) {
    switch (d.kind) {
      case 'tree':
      case 'pine':
        colliders.push({
          x: d.x,
          z: d.z,
          radius: 0.3 * d.scale + 0.05,
          bottom: d.y - 1,
          top: d.y + 12,
          standable: false,
        });
        break;
      case 'rock':
        colliders.push({
          x: d.x,
          z: d.z,
          radius: ROCK_SHAPE.radius * d.scale,
          bottom: d.y - d.scale,
          top: d.y + ROCK_SHAPE.heightScale * d.scale * 0.92,
          standable: true,
        });
        break;
      case 'bush':
        break;
    }
  }
  return colliders;
}

/** Rejilla espacial estática para consultar colliders cercanos rápidamente. */
export class ColliderGrid {
  private readonly cells: number[][];
  private readonly dim: number;
  private readonly stamps: Uint32Array;
  private stamp = 0;

  constructor(
    readonly colliders: readonly Collider[],
    private readonly half: number,
    private readonly cellSize = 8,
  ) {
    this.dim = Math.ceil((half * 2) / cellSize);
    this.cells = Array.from({ length: this.dim * this.dim }, () => [] as number[]);
    this.stamps = new Uint32Array(colliders.length);
    colliders.forEach((c, index) => {
      const x0 = this.cellCoord(c.x - c.radius);
      const x1 = this.cellCoord(c.x + c.radius);
      const z0 = this.cellCoord(c.z - c.radius);
      const z1 = this.cellCoord(c.z + c.radius);
      for (let cz = z0; cz <= z1; cz++) {
        for (let cx = x0; cx <= x1; cx++) (this.cells[cz * this.dim + cx] as number[]).push(index);
      }
    });
  }

  private cellCoord(v: number): number {
    const c = Math.floor((v + this.half) / this.cellSize);
    return c < 0 ? 0 : c >= this.dim ? this.dim - 1 : c;
  }

  /** Índices de los colliders cuyas celdas tocan el círculo (sin duplicados). */
  query(x: number, z: number, radius: number, out: number[]): number[] {
    out.length = 0;
    this.stamp = (this.stamp + 1) >>> 0;
    if (this.stamp === 0) {
      this.stamps.fill(0);
      this.stamp = 1;
    }
    const x0 = this.cellCoord(x - radius);
    const x1 = this.cellCoord(x + radius);
    const z0 = this.cellCoord(z - radius);
    const z1 = this.cellCoord(z + radius);
    for (let cz = z0; cz <= z1; cz++) {
      for (let cx = x0; cx <= x1; cx++) {
        for (const index of this.cells[cz * this.dim + cx] as number[]) {
          if (this.stamps[index] !== this.stamp) {
            this.stamps[index] = this.stamp;
            out.push(index);
          }
        }
      }
    }
    return out;
  }
}
