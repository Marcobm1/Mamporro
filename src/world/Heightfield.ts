// Terreno como rejilla de alturas. La lógica pura (generación y consultas) está
// separada del mesh para poder probarla sin WebGL.
import { createNoise2D, type NoiseFunction2D } from 'simplex-noise';
import { gain, lerp, smoothstep, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';

export interface TerrainConfig {
  /** Lado del mapa en metros. */
  size: number;
  /** Celdas por lado de la rejilla. */
  cells: number;
  /** Radio (squircle) donde empiezan a subir las montañas del borde. */
  borderStart: number;
  /** Radio de la zona despejada y llana del punto de inicio. */
  spawnRadius: number;
  /** Desnivel máximo de las colinas interiores (m). */
  relief: number;
  /** Altura de cada "piso" de mesetas (m). */
  terraceStep: number;
}

/** Exponente del "squircle": un cuadrado con esquinas redondeadas. */
export const SQUIRCLE_POWER = 5;

/** Distancia tipo squircle al centro; `squircle(k·x, k·z) = k·squircle(x, z)`. */
export function squircle(x: number, z: number): number {
  const p = SQUIRCLE_POWER;
  return Math.pow(Math.pow(Math.abs(x), p) + Math.pow(Math.abs(z), p), 1 / p);
}

export class Heightfield {
  readonly cellSize: number;
  readonly half: number;
  /** Vértices por lado (`cells + 1`). */
  readonly stride: number;

  constructor(
    readonly size: number,
    readonly cells: number,
    readonly heights: Float32Array,
  ) {
    this.cellSize = size / cells;
    this.half = size / 2;
    this.stride = cells + 1;
    if (heights.length !== this.stride * this.stride) {
      throw new Error('Heightfield: tamaño de alturas incorrecto');
    }
  }

  /** Altura de un vértice (índices fuera de rango se ajustan al borde). */
  vertexHeight(i: number, j: number): number {
    const n = this.cells;
    const ci = i < 0 ? 0 : i > n ? n : i;
    const cj = j < 0 ? 0 : j > n ? n : j;
    return this.heights[cj * this.stride + ci] as number;
  }

  /**
   * Altura exacta de la superficie dibujada en (x, z). Cada celda se divide en
   * dos triángulos por la diagonal (i, j+1)–(i+1, j), igual que el mesh.
   */
  heightAt(x: number, z: number): number {
    const gx = (x + this.half) / this.cellSize;
    const gz = (z + this.half) / this.cellSize;
    let i = Math.floor(gx);
    let j = Math.floor(gz);
    const last = this.cells - 1;
    if (i < 0) i = 0;
    else if (i > last) i = last;
    if (j < 0) j = 0;
    else if (j > last) j = last;
    let u = gx - i;
    let v = gz - j;
    u = u < 0 ? 0 : u > 1 ? 1 : u;
    v = v < 0 ? 0 : v > 1 ? 1 : v;

    const H = this.heights;
    const s = this.stride;
    const h00 = H[j * s + i] as number;
    const h10 = H[j * s + i + 1] as number;
    const h01 = H[(j + 1) * s + i] as number;
    const h11 = H[(j + 1) * s + i + 1] as number;
    if (u + v <= 1) return h00 + (h10 - h00) * u + (h01 - h00) * v;
    return h11 + (h01 - h11) * (1 - u) + (h10 - h11) * (1 - v);
  }

  /** Normal (unitaria) del triángulo del terreno en (x, z). */
  normalAt(x: number, z: number, out: Vec3Like): Vec3Like {
    const gx = (x + this.half) / this.cellSize;
    const gz = (z + this.half) / this.cellSize;
    let i = Math.floor(gx);
    let j = Math.floor(gz);
    const last = this.cells - 1;
    if (i < 0) i = 0;
    else if (i > last) i = last;
    if (j < 0) j = 0;
    else if (j > last) j = last;
    const u = gx - i;
    const v = gz - j;

    const H = this.heights;
    const s = this.stride;
    const h00 = H[j * s + i] as number;
    const h10 = H[j * s + i + 1] as number;
    const h01 = H[(j + 1) * s + i] as number;
    const h11 = H[(j + 1) * s + i + 1] as number;
    let dx: number;
    let dz: number;
    if (u + v <= 1) {
      dx = (h10 - h00) / this.cellSize;
      dz = (h01 - h00) / this.cellSize;
    } else {
      dx = (h11 - h01) / this.cellSize;
      dz = (h11 - h10) / this.cellSize;
    }
    const inv = 1 / Math.sqrt(dx * dx + 1 + dz * dz);
    out.x = -dx * inv;
    out.y = inv;
    out.z = -dz * inv;
    return out;
  }
}

function fbm(noise: NoiseFunction2D, x: number, z: number, octaves: number): number {
  let amplitude = 1;
  let frequency = 1;
  let sum = 0;
  let norm = 0;
  for (let o = 0; o < octaves; o++) {
    sum += amplitude * noise(x * frequency, z * frequency);
    norm += amplitude;
    amplitude *= 0.5;
    frequency *= 2;
  }
  return sum / norm;
}

/**
 * Genera el terreno: colinas (fBm con distorsión de dominio), mesetas en
 * terrazas cuyos bordes alternan entre rampas suaves y acantilados, una zona
 * llana de inicio y montañas en el borde que marcan el límite del mapa.
 */
export function generateHeightfield(config: TerrainConfig, rng: Rng): Heightfield {
  const random = (): number => rng.next();
  const noiseBase = createNoise2D(random);
  const noiseWarpX = createNoise2D(random);
  const noiseWarpZ = createNoise2D(random);
  const noiseRamp = createNoise2D(random);
  const noiseDetail = createNoise2D(random);
  const noiseEdge = createNoise2D(random);

  const { size, cells, relief, terraceStep, borderStart, spawnRadius } = config;
  const half = size / 2;

  const interior = (x: number, z: number): number => {
    const wx = x + 16 * noiseWarpX(x * 0.005, z * 0.005);
    const wz = z + 16 * noiseWarpZ(x * 0.005, z * 0.005);
    let h = (fbm(noiseBase, wx * 0.0075, wz * 0.0075, 4) * 0.5 + 0.5) * relief;

    // Terrazas: `sharp` bajo = rampa suave, alto = acantilado.
    const t = h / terraceStep;
    const k = Math.floor(t);
    const cliffiness = smoothstep(-0.35, 0.45, noiseRamp(x * 0.011, z * 0.011));
    const sharp = lerp(1.2, 9, cliffiness);
    const terraced = (k + gain(t - k, sharp)) * terraceStep;
    h = lerp(h, terraced, 0.9);

    return h + 0.45 * noiseDetail(x * 0.09, z * 0.09);
  };

  // La zona de inicio se aplana al nivel de terraza más cercano.
  const spawnLevel = Math.round(interior(0, 0) / terraceStep) * terraceStep;

  const stride = cells + 1;
  const heights = new Float32Array(stride * stride);
  const cellSize = size / cells;
  for (let j = 0; j < stride; j++) {
    const z = j * cellSize - half;
    for (let i = 0; i < stride; i++) {
      const x = i * cellSize - half;
      let h = interior(x, z);

      const d = Math.hypot(x, z);
      h = lerp(spawnLevel, h, smoothstep(spawnRadius * 0.6, spawnRadius * 1.9, d));

      // Montañas del borde: suben con la distancia y tienen crestas irregulares
      // (ruido "ridged"), para que el límite del mapa se vea natural.
      const r = squircle(x, z) + 7 * noiseEdge(x * 0.03, z * 0.03);
      const e = smoothstep(borderStart, half - 4, r);
      const ridge = 1 - Math.abs(noiseEdge(x * 0.022 + 50, z * 0.022 + 50));
      h += e * e * 30 + e * ridge * ridge * 34 + e * 5 * noiseEdge(x * 0.09 + 90, z * 0.09 + 90);

      heights[j * stride + i] = h;
    }
  }
  return new Heightfield(size, cells, heights);
}
