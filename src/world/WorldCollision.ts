// Implementación de las consultas de colisión del mundo: terreno, rocas a las que
// se puede subir, troncos y el límite del área jugable.
import type { Vec3Like } from '../core/math';
import type { PhysicsWorld, PlayerBody } from '../entities/playerPhysics';
import type { ColliderGrid } from './decorations';
import { SQUIRCLE_POWER, squircle, type Heightfield } from './Heightfield';

/** Fracción del radio de una roca sobre la que se puede estar de pie. */
const STANDABLE_FRACTION = 0.8;
/** Altura aproximada del cuerpo, para ignorar obstáculos que quedan por encima. */
const BODY_HEIGHT = 1.6;

export class WorldCollision implements PhysicsWorld {
  private readonly nearby: number[] = [];

  constructor(
    readonly heightfield: Heightfield,
    readonly grid: ColliderGrid,
    /** Radio squircle del muro invisible. */
    readonly limit: number,
  ) {}

  /** Cima más alta de una roca bajo (x, z) que no supere `maxY`; -Infinity si no hay. */
  private standableTop(x: number, z: number, maxY: number): number {
    let best = Number.NEGATIVE_INFINITY;
    const colliders = this.grid.colliders;
    for (const index of this.grid.query(x, z, 0, this.nearby)) {
      const c = colliders[index];
      if (!c || !c.standable || c.top > maxY || c.top <= best) continue;
      const r = c.radius * STANDABLE_FRACTION;
      const dx = x - c.x;
      const dz = z - c.z;
      if (dx * dx + dz * dz <= r * r) best = c.top;
    }
    return best;
  }

  groundHeight(x: number, z: number, maxY: number): number {
    const terrain = this.heightfield.heightAt(x, z);
    const top = this.standableTop(x, z, maxY);
    return top > terrain ? top : terrain;
  }

  groundNormal(x: number, z: number, maxY: number, out: Vec3Like): Vec3Like {
    const terrain = this.heightfield.heightAt(x, z);
    if (this.standableTop(x, z, maxY) > terrain) {
      out.x = 0;
      out.y = 1;
      out.z = 0;
      return out;
    }
    return this.heightfield.normalAt(x, z, out);
  }

  resolveObstacles(body: PlayerBody, radius: number, stepHeight: number): void {
    const colliders = this.grid.colliders;
    for (const index of this.grid.query(body.x, body.z, radius, this.nearby)) {
      const c = colliders[index];
      if (!c) continue;
      if (body.y + stepHeight >= c.top) continue; // Por encima: se puede subir o ya está encima.
      if (body.y + BODY_HEIGHT < c.bottom) continue;
      const dx = body.x - c.x;
      const dz = body.z - c.z;
      const minDist = c.radius + radius;
      const d2 = dx * dx + dz * dz;
      if (d2 >= minDist * minDist) continue;
      const d = Math.sqrt(d2);
      const nx = d > 1e-5 ? dx / d : 1;
      const nz = d > 1e-5 ? dz / d : 0;
      body.x = c.x + nx * minDist;
      body.z = c.z + nz * minDist;
      const vn = body.vx * nx + body.vz * nz;
      if (vn < 0) {
        body.vx -= vn * nx;
        body.vz -= vn * nz;
      }
    }
  }

  constrain(body: PlayerBody): void {
    const r = squircle(body.x, body.z);
    if (r <= this.limit) return;
    const k = this.limit / r;
    body.x *= k;
    body.z *= k;
    // Normal hacia fuera del squircle (gradiente) para anular la velocidad saliente.
    const p = SQUIRCLE_POWER - 1;
    const gx = Math.sign(body.x) * Math.pow(Math.abs(body.x), p);
    const gz = Math.sign(body.z) * Math.pow(Math.abs(body.z), p);
    const gl = Math.hypot(gx, gz);
    if (gl < 1e-9) return;
    const nx = gx / gl;
    const nz = gz / gl;
    const vn = body.vx * nx + body.vz * nz;
    if (vn > 0) {
      body.vx -= vn * nx;
      body.vz -= vn * nz;
    }
  }
}
