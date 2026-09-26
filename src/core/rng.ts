// Generador de números aleatorios determinista con semilla (sfc32 + hash cyrb128).
// La misma semilla produce siempre el mismo mapa y las mismas ofertas.

/** Alfabeto de semillas sin caracteres ambiguos (sin 0/O ni 1/I). */
const SEED_ALPHABET = '23456789ABCDEFGHJKLMNPQRSTUVWXYZ';
export const SEED_MAX_LENGTH = 12;

/** Hash de 128 bits de una cadena, repartido en cuatro enteros de 32 bits. */
export function hashString(str: string): [number, number, number, number] {
  let h1 = 1779033703;
  let h2 = 3144134277;
  let h3 = 1013904242;
  let h4 = 2773480762;
  for (let i = 0; i < str.length; i++) {
    const k = str.charCodeAt(i);
    h1 = h2 ^ Math.imul(h1 ^ k, 597399067);
    h2 = h3 ^ Math.imul(h2 ^ k, 2869860233);
    h3 = h4 ^ Math.imul(h3 ^ k, 951274213);
    h4 = h1 ^ Math.imul(h4 ^ k, 2716044179);
  }
  h1 = Math.imul(h3 ^ (h1 >>> 18), 597399067);
  h2 = Math.imul(h4 ^ (h2 >>> 22), 2869860233);
  h3 = Math.imul(h1 ^ (h3 >>> 17), 951274213);
  h4 = Math.imul(h2 ^ (h4 >>> 19), 2716044179);
  h1 ^= h2 ^ h3 ^ h4;
  h2 ^= h1;
  h3 ^= h1;
  h4 ^= h1;
  return [h1 >>> 0, h2 >>> 0, h3 >>> 0, h4 >>> 0];
}

export class Rng {
  readonly seed: string;
  private a: number;
  private b: number;
  private c: number;
  private d: number;

  constructor(seed: string) {
    this.seed = seed;
    [this.a, this.b, this.c, this.d] = hashString(seed);
    // Descartamos las primeras salidas para mezclar bien el estado inicial.
    for (let i = 0; i < 12; i++) this.nextU32();
  }

  /** Entero sin signo de 32 bits. */
  nextU32(): number {
    const t = (((this.a + this.b) | 0) + this.d) | 0;
    this.d = (this.d + 1) | 0;
    this.a = this.b ^ (this.b >>> 9);
    this.b = (this.c + (this.c << 3)) | 0;
    this.c = (this.c << 21) | (this.c >>> 11);
    this.c = (this.c + t) | 0;
    return t >>> 0;
  }

  /** Real en [0, 1). */
  next(): number {
    return this.nextU32() / 4294967296;
  }

  /** Real en [min, max). */
  range(min: number, max: number): number {
    return min + (max - min) * this.next();
  }

  /** Entero en [min, max] (ambos incluidos). */
  int(min: number, max: number): number {
    return min + Math.floor(this.next() * (max - min + 1));
  }

  chance(probability: number): boolean {
    return this.next() < probability;
  }

  pick<T>(items: readonly T[]): T {
    if (items.length === 0) throw new Error('Rng.pick: lista vacía');
    return items[Math.floor(this.next() * items.length)] as T;
  }

  /** Baraja la lista en el sitio (Fisher-Yates) y la devuelve. */
  shuffle<T>(items: T[]): T[] {
    for (let i = items.length - 1; i > 0; i--) {
      const j = Math.floor(this.next() * (i + 1));
      const tmp = items[i] as T;
      items[i] = items[j] as T;
      items[j] = tmp;
    }
    return items;
  }

  /**
   * Crea un generador independiente para un subsistema (mapa, ofertas...).
   * Solo depende de la semilla y la etiqueta, no de cuántos números se hayan consumido.
   */
  derive(label: string): Rng {
    return new Rng(`${this.seed}/${label}`);
  }
}

/** Genera una semilla legible aleatoria (no determinista). */
export function randomSeed(length = 6): string {
  const values = new Uint32Array(length);
  if (typeof crypto !== 'undefined' && typeof crypto.getRandomValues === 'function') {
    crypto.getRandomValues(values);
  } else {
    for (let i = 0; i < length; i++) values[i] = Math.floor(Math.random() * 4294967296);
  }
  let out = '';
  for (let i = 0; i < length; i++) {
    out += SEED_ALPHABET[(values[i] as number) % SEED_ALPHABET.length];
  }
  return out;
}

/** Limpia una semilla escrita por el jugador. Devuelve `null` si queda vacía. */
export function normalizeSeed(input: string): string | null {
  const cleaned = input
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '')
    .slice(0, SEED_MAX_LENGTH);
  return cleaned.length > 0 ? cleaned : null;
}
