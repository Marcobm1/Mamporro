// Colisionadores estáticos del mapa (lógica pura): cilindros verticales y cajas
// orientadas (giradas sobre el eje Y), más una rejilla espacial para consultarlos.

interface ColliderBase {
  x: number;
  z: number;
  bottom: number;
  top: number;
  /** Si es true, se puede estar de pie encima (rocas, muros bajos, cajas...). */
  standable: boolean;
}

export interface CircleCollider extends ColliderBase {
  shape: 'circle';
  radius: number;
}

/**
 * Caja girada `rotation` radianes sobre Y (mismo convenio que `Object3D.rotation.y`):
 * el eje X local apunta a (cos, 0, -sin) y el Z local a (sin, 0, cos).
 */
export interface BoxCollider extends ColliderBase {
  shape: 'box';
  halfX: number;
  halfZ: number;
  cos: number;
  sin: number;
}

export type Collider = CircleCollider | BoxCollider;

/** Fracción del radio de un cilindro sobre la que se puede estar de pie (bordes redondeados). */
export const STANDABLE_CIRCLE_FRACTION = 0.8;

export function circleCollider(
  x: number,
  z: number,
  radius: number,
  bottom: number,
  top: number,
  standable: boolean,
): CircleCollider {
  return { shape: 'circle', x, z, radius, bottom, top, standable };
}

export function boxCollider(
  x: number,
  z: number,
  halfX: number,
  halfZ: number,
  rotation: number,
  bottom: number,
  top: number,
  standable: boolean,
): BoxCollider {
  return { shape: 'box', x, z, halfX, halfZ, cos: Math.cos(rotation), sin: Math.sin(rotation), bottom, top, standable };
}

export function boundingRadius(c: Collider): number {
  return c.shape === 'circle' ? c.radius : Math.hypot(c.halfX, c.halfZ);
}

/** ¿Está (x, z) sobre la superficie superior del collider? */
export function isOnTop(c: Collider, x: number, z: number): boolean {
  const dx = x - c.x;
  const dz = z - c.z;
  if (c.shape === 'circle') {
    const r = c.radius * STANDABLE_CIRCLE_FRACTION;
    return dx * dx + dz * dz <= r * r;
  }
  const lx = dx * c.cos - dz * c.sin;
  const lz = dx * c.sin + dz * c.cos;
  return Math.abs(lx) <= c.halfX && Math.abs(lz) <= c.halfZ;
}

export interface PushResult {
  /** Desplazamiento necesario para salir del collider. */
  dx: number;
  dz: number;
  /** Normal (hacia fuera) de la superficie tocada. */
  nx: number;
  nz: number;
}

/**
 * Si un círculo de radio `r` en (px, pz) se solapa con el collider, rellena
 * `out` con el empujón para sacarlo y devuelve true.
 */
export function pushOut(c: Collider, px: number, pz: number, r: number, out: PushResult): boolean {
  const dx = px - c.x;
  const dz = pz - c.z;
  if (c.shape === 'circle') {
    const minDist = c.radius + r;
    const d2 = dx * dx + dz * dz;
    if (d2 >= minDist * minDist) return false;
    const d = Math.sqrt(d2);
    out.nx = d > 1e-6 ? dx / d : 1;
    out.nz = d > 1e-6 ? dz / d : 0;
    out.dx = out.nx * (minDist - d);
    out.dz = out.nz * (minDist - d);
    return true;
  }

  // Pasamos el punto al espacio local de la caja.
  const lx = dx * c.cos - dz * c.sin;
  const lz = dx * c.sin + dz * c.cos;
  const qx = Math.max(-c.halfX, Math.min(c.halfX, lx));
  const qz = Math.max(-c.halfZ, Math.min(c.halfZ, lz));
  const ex = lx - qx;
  const ez = lz - qz;
  const d2 = ex * ex + ez * ez;
  if (d2 >= r * r) return false;

  let nlx: number;
  let nlz: number;
  let penetration: number;
  if (d2 > 1e-10) {
    const d = Math.sqrt(d2);
    nlx = ex / d;
    nlz = ez / d;
    penetration = r - d;
  } else {
    // El centro está dentro: salimos por el lado más cercano.
    const penX = c.halfX - Math.abs(lx);
    const penZ = c.halfZ - Math.abs(lz);
    if (penX < penZ) {
      nlx = lx >= 0 ? 1 : -1;
      nlz = 0;
      penetration = penX + r;
    } else {
      nlx = 0;
      nlz = lz >= 0 ? 1 : -1;
      penetration = penZ + r;
    }
  }
  // Normal local → mundo.
  out.nx = nlx * c.cos + nlz * c.sin;
  out.nz = -nlx * c.sin + nlz * c.cos;
  out.dx = out.nx * penetration;
  out.dz = out.nz * penetration;
  return true;
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
      const r = boundingRadius(c);
      const x0 = this.cellCoord(c.x - r);
      const x1 = this.cellCoord(c.x + r);
      const z0 = this.cellCoord(c.z - r);
      const z1 = this.cellCoord(c.z + r);
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
