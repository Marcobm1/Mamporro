// Rejilla espacial para entidades que se mueven (enemigos). Se reconstruye cada
// tick con una ordenación por conteo: coste lineal y sin reservar memoria.

export class SpatialGrid {
  private readonly dim: number;
  private readonly half: number;
  private readonly cellStart: Int32Array;
  private readonly cellCount: Int32Array;
  private readonly cellOf: Int32Array;
  private readonly items: Int32Array;
  private xs: Float32Array | null = null;
  private zs: Float32Array | null = null;

  constructor(
    worldSize: number,
    readonly cellSize: number,
    readonly capacity: number,
  ) {
    this.dim = Math.ceil(worldSize / cellSize);
    this.half = worldSize / 2;
    this.cellStart = new Int32Array(this.dim * this.dim + 1);
    this.cellCount = new Int32Array(this.dim * this.dim);
    this.cellOf = new Int32Array(capacity);
    this.items = new Int32Array(capacity);
  }

  private coord(v: number): number {
    const c = Math.floor((v + this.half) / this.cellSize);
    return c < 0 ? 0 : c >= this.dim ? this.dim - 1 : c;
  }

  /** Vuelve a repartir las `count` primeras posiciones en las celdas. */
  rebuild(xs: Float32Array, zs: Float32Array, count: number): void {
    this.xs = xs;
    this.zs = zs;
    const counts = this.cellCount;
    counts.fill(0);
    for (let i = 0; i < count; i++) {
      const cell = this.coord(zs[i] as number) * this.dim + this.coord(xs[i] as number);
      this.cellOf[i] = cell;
      counts[cell] = (counts[cell] as number) + 1;
    }
    let acc = 0;
    for (let c = 0; c < counts.length; c++) {
      this.cellStart[c] = acc;
      acc += counts[c] as number;
      counts[c] = 0;
    }
    this.cellStart[counts.length] = acc;
    for (let i = 0; i < count; i++) {
      const cell = this.cellOf[i] as number;
      this.items[(this.cellStart[cell] as number) + (counts[cell] as number)] = i;
      counts[cell] = (counts[cell] as number) + 1;
    }
  }

  /** Índices a distancia ≤ `radius` de (x, z). Devuelve cuántos ha escrito en `out`. */
  queryRadius(x: number, z: number, radius: number, out: Int32Array): number {
    const xs = this.xs;
    const zs = this.zs;
    if (!xs || !zs) return 0;
    const r2 = radius * radius;
    const x0 = this.coord(x - radius);
    const x1 = this.coord(x + radius);
    const z0 = this.coord(z - radius);
    const z1 = this.coord(z + radius);
    let n = 0;
    for (let cz = z0; cz <= z1; cz++) {
      for (let cx = x0; cx <= x1; cx++) {
        const cell = cz * this.dim + cx;
        const end = this.cellStart[cell + 1] as number;
        for (let k = this.cellStart[cell] as number; k < end; k++) {
          const i = this.items[k] as number;
          const dx = (xs[i] as number) - x;
          const dz = (zs[i] as number) - z;
          if (dx * dx + dz * dz <= r2) {
            if (n >= out.length) return n;
            out[n++] = i;
          }
        }
      }
    }
    return n;
  }

  /**
   * Índice más cercano a (x, z) dentro de `maxRadius` que cumpla `accept`, o -1.
   * Busca por anillos de celdas y para en cuanto ningún anillo puede mejorar.
   */
  nearest(x: number, z: number, maxRadius: number, accept?: (index: number) => boolean): number {
    const xs = this.xs;
    const zs = this.zs;
    if (!xs || !zs) return -1;
    const cx0 = this.coord(x);
    const cz0 = this.coord(z);
    const maxRing = Math.ceil(maxRadius / this.cellSize) + 1;
    let best = -1;
    let bestD2 = maxRadius * maxRadius;
    for (let ring = 0; ring <= maxRing; ring++) {
      const ringDist = (ring - 1) * this.cellSize;
      if (best >= 0 && ringDist > 0 && ringDist * ringDist > bestD2) break;
      for (let cz = cz0 - ring; cz <= cz0 + ring; cz++) {
        if (cz < 0 || cz >= this.dim) continue;
        for (let cx = cx0 - ring; cx <= cx0 + ring; cx++) {
          if (cx < 0 || cx >= this.dim) continue;
          // Solo el borde del anillo (el interior ya se miró).
          if (ring > 0 && cz !== cz0 - ring && cz !== cz0 + ring && cx !== cx0 - ring && cx !== cx0 + ring) continue;
          const cell = cz * this.dim + cx;
          const end = this.cellStart[cell + 1] as number;
          for (let k = this.cellStart[cell] as number; k < end; k++) {
            const i = this.items[k] as number;
            const dx = (xs[i] as number) - x;
            const dz = (zs[i] as number) - z;
            const d2 = dx * dx + dz * dz;
            if (d2 < bestD2 && (!accept || accept(i))) {
              bestD2 = d2;
              best = i;
            }
          }
        }
      }
    }
    return best;
  }
}
