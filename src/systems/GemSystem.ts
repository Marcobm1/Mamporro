// Gemas de experiencia: caen al morir los enemigos y vuelan hacia el jugador
// cuando entra en su radio de recogida. Si hay demasiadas, se fusionan.

/** Valor a partir del cual una gema cambia de color y tamaño. */
export const GEM_TIERS = [1, 5, 20, 100] as const;

export function gemTier(value: number): number {
  let tier = 0;
  for (let t = 1; t < GEM_TIERS.length; t++) if (value >= (GEM_TIERS[t] as number)) tier = t;
  return tier;
}

/** Distancia a la que la gema se recoge al volar hacia el jugador. */
const COLLECT_DISTANCE = 0.9;
const ATTRACT_ACCEL = 70;
const MAX_ATTRACT_SPEED = 32;

export class GemSystem {
  count = 0;
  readonly x: Float32Array;
  readonly y: Float32Array;
  readonly z: Float32Array;
  readonly value: Float32Array;
  readonly speed: Float32Array;
  readonly phase: Float32Array;
  readonly attracted: Uint8Array;

  constructor(readonly capacity: number) {
    this.x = new Float32Array(capacity);
    this.y = new Float32Array(capacity);
    this.z = new Float32Array(capacity);
    this.value = new Float32Array(capacity);
    this.speed = new Float32Array(capacity);
    this.phase = new Float32Array(capacity);
    this.attracted = new Uint8Array(capacity);
  }

  /** Suelta una gema. Si no caben más, su valor se suma a la gema más cercana. */
  spawn(x: number, y: number, z: number, value: number): void {
    if (this.count >= this.capacity) {
      let best = 0;
      let bestD2 = Number.POSITIVE_INFINITY;
      for (let i = 0; i < this.count; i++) {
        const dx = (this.x[i] as number) - x;
        const dz = (this.z[i] as number) - z;
        const d2 = dx * dx + dz * dz;
        if (d2 < bestD2) {
          bestD2 = d2;
          best = i;
        }
      }
      this.value[best] = (this.value[best] as number) + value;
      return;
    }
    const i = this.count++;
    this.x[i] = x;
    this.y[i] = y;
    this.z[i] = z;
    this.value[i] = value;
    this.speed[i] = 0;
    this.phase[i] = (i * 1.37) % (Math.PI * 2);
    this.attracted[i] = 0;
  }

  private remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    this.x[i] = this.x[last] as number;
    this.y[i] = this.y[last] as number;
    this.z[i] = this.z[last] as number;
    this.value[i] = this.value[last] as number;
    this.speed[i] = this.speed[last] as number;
    this.phase[i] = this.phase[last] as number;
    this.attracted[i] = this.attracted[last] as number;
  }

  /** Atrae todas las gemas hacia el jugador (p. ej. al subir de nivel o con un imán). */
  attractAll(): void {
    this.attracted.fill(1, 0, this.count);
  }

  clear(): void {
    this.count = 0;
  }

  /** Mueve las gemas y devuelve la experiencia recogida en este paso. */
  update(dt: number, px: number, py: number, pz: number, pickupRadius: number): number {
    let collected = 0;
    const r2 = pickupRadius * pickupRadius;
    for (let i = this.count - 1; i >= 0; i--) {
      this.phase[i] = (this.phase[i] as number) + dt * 3;
      const dx = px - (this.x[i] as number);
      const dy = py + 0.8 - (this.y[i] as number);
      const dz = pz - (this.z[i] as number);
      const d2 = dx * dx + dz * dz;
      if (!this.attracted[i] && d2 < r2) this.attracted[i] = 1;
      if (!this.attracted[i]) continue;
      const d = Math.sqrt(d2 + dy * dy);
      if (d < COLLECT_DISTANCE) {
        collected += this.value[i] as number;
        this.remove(i);
        continue;
      }
      const speed = Math.min(MAX_ATTRACT_SPEED, (this.speed[i] as number) + ATTRACT_ACCEL * dt);
      this.speed[i] = speed;
      const step = Math.min(d, speed * dt);
      this.x[i] = (this.x[i] as number) + (dx / d) * step;
      this.y[i] = (this.y[i] as number) + (dy / d) * step;
      this.z[i] = (this.z[i] as number) + (dz / d) * step;
    }
    return collected;
  }
}
