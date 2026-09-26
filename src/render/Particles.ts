// Partículas sencillas (cubitos que salen disparados, caen y se encogen), en una
// sola malla instanciada. Son solo decorativas: se animan en el render.
import { BoxGeometry, Color, DynamicDrawUsage, InstancedMesh, MeshBasicMaterial } from 'three';
import type { Rng } from '../core/rng';
import { writeYawMatrix } from './EnemyRenderer';
import { RENDER_ORDER } from './renderOrder';
import { applyRetro } from './retroMaterial';

export interface BurstOptions {
  count: number;
  colors: readonly number[];
  /** Velocidad inicial máxima (m/s). */
  speed: number;
  /** Tamaño del cubo (m). */
  size: number;
  /** Vida (s). */
  life: number;
  /** Impulso vertical extra (m/s). */
  lift?: number;
  /** Gravedad (m/s²); negativa para que suban (humo). */
  gravity?: number;
}

export class Particles {
  readonly mesh: InstancedMesh;
  private count = 0;
  private readonly x: Float32Array;
  private readonly y: Float32Array;
  private readonly z: Float32Array;
  private readonly vx: Float32Array;
  private readonly vy: Float32Array;
  private readonly vz: Float32Array;
  private readonly life: Float32Array;
  private readonly maxLife: Float32Array;
  private readonly size: Float32Array;
  private readonly gravity: Float32Array;
  private readonly spin: Float32Array;
  private readonly colors: Float32Array;
  private readonly color = new Color();

  constructor(
    readonly capacity: number,
    private readonly rng: Rng,
  ) {
    const f = (): Float32Array => new Float32Array(capacity);
    this.x = f();
    this.y = f();
    this.z = f();
    this.vx = f();
    this.vy = f();
    this.vz = f();
    this.life = f();
    this.maxLife = f();
    this.size = f();
    this.gravity = f();
    this.spin = f();
    this.colors = new Float32Array(capacity * 3);
    this.mesh = new InstancedMesh(new BoxGeometry(1, 1, 1), applyRetro(new MeshBasicMaterial({ color: 0xffffff })), capacity);
    this.mesh.renderOrder = RENDER_ORDER.afterPlayer;
    this.mesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.mesh.setColorAt(0, this.color.setHex(0xffffff));
    this.mesh.instanceColor?.setUsage(DynamicDrawUsage);
    this.mesh.count = 0;
    this.mesh.frustumCulled = false;
  }

  get active(): number {
    return this.count;
  }

  burst(x: number, y: number, z: number, o: BurstOptions): void {
    const rng = this.rng;
    for (let n = 0; n < o.count; n++) {
      // Si está lleno, se reutiliza una partícula al azar (las viejas desaparecen antes).
      const i = this.count < this.capacity ? this.count++ : rng.int(0, this.capacity - 1);
      const angle = rng.next() * Math.PI * 2;
      const speed = rng.range(0.3, 1) * o.speed;
      this.x[i] = x;
      this.y[i] = y;
      this.z[i] = z;
      this.vx[i] = Math.cos(angle) * speed;
      this.vz[i] = Math.sin(angle) * speed;
      this.vy[i] = rng.range(0.2, 1) * o.speed * 0.8 + (o.lift ?? 0);
      const life = o.life * rng.range(0.7, 1.2);
      this.life[i] = life;
      this.maxLife[i] = life;
      this.size[i] = o.size * rng.range(0.6, 1.3);
      this.gravity[i] = o.gravity ?? 18;
      this.spin[i] = rng.next() * Math.PI;
      this.color.setHex(o.colors[n % o.colors.length] ?? 0xffffff);
      this.colors[i * 3] = this.color.r;
      this.colors[i * 3 + 1] = this.color.g;
      this.colors[i * 3 + 2] = this.color.b;
    }
  }

  private remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    this.x[i] = this.x[last] as number;
    this.y[i] = this.y[last] as number;
    this.z[i] = this.z[last] as number;
    this.vx[i] = this.vx[last] as number;
    this.vy[i] = this.vy[last] as number;
    this.vz[i] = this.vz[last] as number;
    this.life[i] = this.life[last] as number;
    this.maxLife[i] = this.maxLife[last] as number;
    this.size[i] = this.size[last] as number;
    this.gravity[i] = this.gravity[last] as number;
    this.spin[i] = this.spin[last] as number;
    this.colors.copyWithin(i * 3, last * 3, last * 3 + 3);
  }

  clear(): void {
    this.count = 0;
    this.mesh.count = 0;
  }

  update(dt: number): void {
    for (let i = this.count - 1; i >= 0; i--) {
      this.life[i] = (this.life[i] as number) - dt;
      if ((this.life[i] as number) <= 0) {
        this.remove(i);
        continue;
      }
      this.vy[i] = (this.vy[i] as number) - (this.gravity[i] as number) * dt;
      this.x[i] = (this.x[i] as number) + (this.vx[i] as number) * dt;
      this.y[i] = (this.y[i] as number) + (this.vy[i] as number) * dt;
      this.z[i] = (this.z[i] as number) + (this.vz[i] as number) * dt;
      this.spin[i] = (this.spin[i] as number) + dt * 6;
    }
    const matrices = this.mesh.instanceMatrix.array as Float32Array;
    const colors = this.mesh.instanceColor;
    for (let i = 0; i < this.count; i++) {
      const s = (this.size[i] as number) * Math.min(1, ((this.life[i] as number) / (this.maxLife[i] as number)) * 1.5);
      writeYawMatrix(matrices, i, this.x[i] as number, this.y[i] as number, this.z[i] as number, this.spin[i] as number, s, s, s);
    }
    if (colors) {
      (colors.array as Float32Array).set(this.colors.subarray(0, this.count * 3));
      colors.needsUpdate = true;
    }
    this.mesh.count = this.count;
    this.mesh.instanceMatrix.needsUpdate = true;
  }
}
