// Proyectiles del jugador (arrays planos). Pueden ser teledirigidos (giran hacia
// el enemigo más cercano que aún no han golpeado) y atravesar varios enemigos.
import type { Heightfield } from '../world/Heightfield';
import type { EnemySystem } from './EnemySystem';

/** Enemigos que recuerda cada proyectil para no golpearlos dos veces. */
const MAX_HITS = 8;
/** Altura de vuelo sobre el suelo (m). */
const FLIGHT_HEIGHT = 1;
/** Distancia máxima a la que un proyectil teledirigido busca objetivo. */
const HOMING_RANGE = 14;

export interface ProjectileSpawn {
  x: number;
  y: number;
  z: number;
  dirX: number;
  dirZ: number;
  speed: number;
  life: number;
  radius: number;
  pierce: number;
  /** Giro hacia el objetivo (1/s); 0 = vuelo recto. */
  homing: number;
  /** Índice del arma que lo disparó (en la lista de armas de la partida). */
  weapon: number;
}

export class ProjectileSystem {
  count = 0;
  readonly x: Float32Array;
  readonly y: Float32Array;
  readonly z: Float32Array;
  readonly px: Float32Array;
  readonly py: Float32Array;
  readonly pz: Float32Array;
  readonly vx: Float32Array;
  readonly vz: Float32Array;
  readonly speed: Float32Array;
  readonly life: Float32Array;
  readonly radius: Float32Array;
  readonly homing: Float32Array;
  readonly spin: Float32Array;
  readonly pierce: Int16Array;
  readonly weapon: Uint8Array;
  readonly hitCount: Uint8Array;
  readonly hits: Uint32Array;
  private readonly floatArrays: Float32Array[];
  private readonly scratch = new Int32Array(64);

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
    this.speed = f();
    this.life = f();
    this.radius = f();
    this.homing = f();
    this.spin = f();
    this.pierce = new Int16Array(capacity);
    this.weapon = new Uint8Array(capacity);
    this.hitCount = new Uint8Array(capacity);
    this.hits = new Uint32Array(capacity * MAX_HITS);
    this.floatArrays = [this.x, this.y, this.z, this.px, this.py, this.pz, this.vx, this.vz, this.speed, this.life, this.radius, this.homing, this.spin];
  }

  spawn(p: ProjectileSpawn): number {
    if (this.count >= this.capacity) return -1;
    const i = this.count++;
    const len = Math.hypot(p.dirX, p.dirZ) || 1;
    this.x[i] = this.px[i] = p.x;
    this.y[i] = this.py[i] = p.y;
    this.z[i] = this.pz[i] = p.z;
    this.vx[i] = (p.dirX / len) * p.speed;
    this.vz[i] = (p.dirZ / len) * p.speed;
    this.speed[i] = p.speed;
    this.life[i] = p.life;
    this.radius[i] = p.radius;
    this.homing[i] = p.homing;
    this.spin[i] = 0;
    this.pierce[i] = p.pierce;
    this.weapon[i] = p.weapon;
    this.hitCount[i] = 0;
    return i;
  }

  remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    for (const a of this.floatArrays) a[i] = a[last] as number;
    this.pierce[i] = this.pierce[last] as number;
    this.weapon[i] = this.weapon[last] as number;
    this.hitCount[i] = this.hitCount[last] as number;
    this.hits.copyWithin(i * MAX_HITS, last * MAX_HITS, last * MAX_HITS + MAX_HITS);
  }

  clear(): void {
    this.count = 0;
  }

  private alreadyHit(i: number, enemyId: number): boolean {
    const base = i * MAX_HITS;
    const n = this.hitCount[i] as number;
    for (let k = 0; k < n; k++) if (this.hits[base + k] === enemyId) return true;
    return false;
  }

  private rememberHit(i: number, enemyId: number): void {
    const n = this.hitCount[i] as number;
    // Si se llena, se sobrescribe el más antiguo (con perforaciones enormes).
    this.hits[i * MAX_HITS + (n % MAX_HITS)] = enemyId;
    if (n < MAX_HITS) this.hitCount[i] = n + 1;
  }

  /**
   * Avanza los proyectiles; `onHit(proyectil, enemigo)` aplica el daño. Se recorren
   * de atrás adelante para poder eliminar sin saltarse ninguno.
   */
  update(dt: number, enemies: EnemySystem, hf: Heightfield, onHit: (projectile: number, enemy: number) => void): void {
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

      // Teledirigido: gira hacia el enemigo vivo más cercano que no haya golpeado.
      const turn = this.homing[i] as number;
      if (turn > 0) {
        const target = enemies.grid.nearest(x, z, HOMING_RANGE, (e) => (enemies.hp[e] as number) > 0 && !this.alreadyHit(i, enemies.id[e] as number));
        if (target >= 0) {
          const tx = (enemies.x[target] as number) - x;
          const tz = (enemies.z[target] as number) - z;
          const tl = Math.hypot(tx, tz) || 1;
          const speed = this.speed[i] as number;
          const k = Math.min(1, turn * dt);
          let vx = (this.vx[i] as number) + ((tx / tl) * speed - (this.vx[i] as number)) * k;
          let vz = (this.vz[i] as number) + ((tz / tl) * speed - (this.vz[i] as number)) * k;
          const vl = Math.hypot(vx, vz) || 1;
          vx = (vx / vl) * speed;
          vz = (vz / vl) * speed;
          this.vx[i] = vx;
          this.vz[i] = vz;
        }
      }

      const nx = x + (this.vx[i] as number) * dt;
      const nz = z + (this.vz[i] as number) * dt;
      this.x[i] = nx;
      this.z[i] = nz;
      this.y[i] = hf.heightAt(nx, nz) + FLIGHT_HEIGHT;
      this.spin[i] = (this.spin[i] as number) + dt * 18;

      // Colisiones con enemigos.
      const r = this.radius[i] as number;
      const n = enemies.grid.queryRadius(nx, nz, r + 0.8, this.scratch);
      for (let k = 0; k < n; k++) {
        const e = this.scratch[k] as number;
        if (e >= enemies.count || (enemies.hp[e] as number) <= 0) continue;
        const enemyId = enemies.id[e] as number;
        if (this.alreadyHit(i, enemyId)) continue;
        const ex = (enemies.x[e] as number) - nx;
        const ez = (enemies.z[e] as number) - nz;
        const reach = r + 0.45;
        if (ex * ex + ez * ez > reach * reach) continue;
        onHit(i, e);
        this.rememberHit(i, enemyId);
        this.pierce[i] = (this.pierce[i] as number) - 1;
        if ((this.pierce[i] as number) < 0) {
          this.remove(i);
          break;
        }
      }
    }
  }
}
