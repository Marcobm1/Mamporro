// Implementación de las consultas de colisión del mundo: terreno, superficies a
// las que se puede subir (rocas, muros bajos, cajas...), obstáculos y límite del mapa.
import type { Vec3Like } from '../core/math';
import type { PhysicsWorld, PlayerBody } from '../entities/playerPhysics';
import { isOnTop, pushOut, type ColliderGrid, type PushResult } from './colliders';
import { SQUIRCLE_POWER, squircle, type Heightfield } from './Heightfield';

/** Altura aproximada del cuerpo, para ignorar obstáculos que quedan por encima. */
const BODY_HEIGHT = 1.6;

export interface CirclePosition {
  x: number;
  z: number;
}

export class WorldCollision implements PhysicsWorld {
  private readonly nearby: number[] = [];
  private readonly push: PushResult = { dx: 0, dz: 0, nx: 0, nz: 0 };

  constructor(
    readonly heightfield: Heightfield,
    readonly grid: ColliderGrid,
    /** Radio squircle del muro invisible. */
    readonly limit: number,
  ) {}

  /** Cima más alta de un obstáculo escalable bajo (x, z) que no supere `maxY`; -Infinity si no hay. */
  private standableTop(x: number, z: number, maxY: number): number {
    let best = Number.NEGATIVE_INFINITY;
    const colliders = this.grid.colliders;
    for (const index of this.grid.query(x, z, 0, this.nearby)) {
      const c = colliders[index];
      if (!c || !c.standable || c.top > maxY || c.top <= best) continue;
      if (isOnTop(c, x, z)) best = c.top;
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
    const push = this.push;
    for (const index of this.grid.query(body.x, body.z, radius + 1, this.nearby)) {
      const c = colliders[index];
      if (!c) continue;
      if (body.y + stepHeight >= c.top) continue; // Por encima: se puede subir o ya está encima.
      if (body.y + BODY_HEIGHT < c.bottom) continue;
      if (!pushOut(c, body.x, body.z, radius, push)) continue;
      body.x += push.dx;
      body.z += push.dz;
      const vn = body.vx * push.nx + body.vz * push.nz;
      if (vn < 0) {
        body.vx -= vn * push.nx;
        body.vz -= vn * push.nz;
      }
    }
  }

  /**
   * Versión ligera para enemigos: saca un círculo de los obstáculos sólidos
   * (los más bajos que `step` se pueden subir). Devuelve true si ha habido contacto.
   */
  pushOutCircle(pos: CirclePosition, radius: number, feetY: number, step: number): boolean {
    const colliders = this.grid.colliders;
    const push = this.push;
    let hit = false;
    for (const index of this.grid.query(pos.x, pos.z, radius + 1, this.nearby)) {
      const c = colliders[index];
      if (!c || feetY + step >= c.top || feetY + BODY_HEIGHT < c.bottom) continue;
      if (!pushOut(c, pos.x, pos.z, radius, push)) continue;
      pos.x += push.dx;
      pos.z += push.dz;
      hit = true;
    }
    return hit;
  }

  /** ¿Está (x, z) dentro del área jugable (con margen)? */
  isInside(x: number, z: number, margin = 0): boolean {
    return squircle(x, z) <= this.limit - margin;
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

  /** Igual que `constrain`, para posiciones sueltas (enemigos, proyectiles...). */
  clampInside(pos: CirclePosition): void {
    const r = squircle(pos.x, pos.z);
    if (r <= this.limit) return;
    const k = this.limit / r;
    pos.x *= k;
    pos.z *= k;
  }
}
