// Proyectiles de los enemigos (pipas de las palomas, bolas de polvo del jefe) en
// arrays planos. Vuelan a ras de suelo y dañan al jugador al tocarle.
import type { Heightfield } from '../world/Heightfield';

/** Qué se dibuja (y de qué color son sus partículas). */
export type EnemyProjectileKind = 0 | 1;
export const PROJECTILE_PIPA: EnemyProjectileKind = 0;
export const PROJECTILE_DUST: EnemyProjectileKind = 1;

/** Altura de vuelo sobre el suelo (m): a la altura del pecho de Doña Remedios. */
const FLIGHT_HEIGHT = 1;
/** Diferencia de altura hasta la que alcanza al jugador. */
const HIT_HEIGHT = 1.4;

export interface EnemyShot {
  x: number;
  z: number;
  dirX: number;
  dirZ: number;
  speed: number;
  damage: number;
  radius: number;
  life: number;
  kind: EnemyProjectileKind;
}

export interface ShotTarget {
  x: number;
  /** Altura de los pies. */
  y: number;
  z: number;
  radius: number;
}

export class EnemyProjectileSystem {
  count = 0;
  readonly x: Float32Array;
  readonly y: Float32Array;
  readonly z: Float32Array;
  readonly px: Float32Array;
  readonly py: Float32Array;
  readonly pz: Float32Array;
  readonly vx: Float32Array;
  readonly vz: Float32Array;
  readonly damage: Float32Array;
  readonly radius: Float32Array;
  readonly life: Float32Array;
  readonly spin: Float32Array;
  readonly kind: Uint8Array;
  private readonly floatArrays: Float32Array[];

  constructor(readonly capacity: number) {
    const f = (): Float32Array => new Float32Array(capacity);
    this.x = f();
    this.y = f();
    this.z = f();
    this.px = f();
    this.py = f();
    this.pz = f();
    this.vx = f();
    this.vz = f();
    this.damage = f();
    this.radius = f();
    this.life = f();
    this.spin = f();
    this.kind = new Uint8Array(capacity);
    this.floatArrays = [this.x, this.y, this.z, this.px, this.py, this.pz, this.vx, this.vz, this.damage, this.radius, this.life, this.spin];
  }

  spawn(shot: EnemyShot, hf: Heightfield): number {
    if (this.count >= this.capacity) return -1;
    const i = this.count++;
    const len = Math.hypot(shot.dirX, shot.dirZ) || 1;
    const y = hf.heightAt(shot.x, shot.z) + FLIGHT_HEIGHT;
    this.x[i] = this.px[i] = shot.x;
    this.y[i] = this.py[i] = y;
    this.z[i] = this.pz[i] = shot.z;
    this.vx[i] = (shot.dirX / len) * shot.speed;
    this.vz[i] = (shot.dirZ / len) * shot.speed;
    this.damage[i] = shot.damage;
    this.radius[i] = shot.radius;
    this.life[i] = shot.life;
    this.spin[i] = i * 0.7;
    this.kind[i] = shot.kind;
    return i;
  }

  remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    for (const a of this.floatArrays) a[i] = a[last] as number;
    this.kind[i] = this.kind[last] as number;
  }

  clear(): void {
    this.count = 0;
  }

  /**
   * Mueve los proyectiles. Devuelve el daño del proyectil más fuerte que ha tocado
   * al jugador en este paso (0 si ninguno); los que tocan desaparecen.
   */
  update(dt: number, hf: Heightfield, target: ShotTarget): number {
    let hit = 0;
    for (let i = this.count - 1; i >= 0; i--) {
      this.life[i] = (this.life[i] as number) - dt;
      if ((this.life[i] as number) <= 0) {
        this.remove(i);
        continue;
      }
      const x = this.x[i] as number;
      const z = this.z[i] as number;
      this.px[i] = x;
      this.py[i] = this.y[i] as number;
      this.pz[i] = z;
      const nx = x + (this.vx[i] as number) * dt;
      const nz = z + (this.vz[i] as number) * dt;
      const y = hf.heightAt(nx, nz) + FLIGHT_HEIGHT;
      this.x[i] = nx;
      this.z[i] = nz;
      this.y[i] = y;
      this.spin[i] = (this.spin[i] as number) + dt * 9;
      const dx = nx - target.x;
      const dz = nz - target.z;
      const reach = (this.radius[i] as number) + target.radius;
      if (dx * dx + dz * dz < reach * reach && Math.abs(y - FLIGHT_HEIGHT - target.y) < HIT_HEIGHT) {
        hit = Math.max(hit, this.damage[i] as number);
        this.remove(i);
      }
    }
    return hit;
  }
}
