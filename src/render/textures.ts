// Texturas generadas por código en un canvas (sin archivos externos).
// Son pequeñas y se muestrean con filtro Nearest para el aspecto pixelado.
import {
  CanvasTexture,
  NearestFilter,
  NearestMipmapNearestFilter,
  RepeatWrapping,
  SRGBColorSpace,
  type Texture,
} from 'three';
import { Rng } from '../core/rng';

function createCanvas(size: number): [HTMLCanvasElement, CanvasRenderingContext2D] {
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('Canvas 2D no disponible');
  return [canvas, ctx];
}

function toTexture(canvas: HTMLCanvasElement, repeat: boolean): Texture {
  const texture = new CanvasTexture(canvas);
  texture.colorSpace = SRGBColorSpace;
  texture.magFilter = NearestFilter;
  texture.minFilter = NearestMipmapNearestFilter;
  texture.generateMipmaps = true;
  if (repeat) {
    texture.wrapS = RepeatWrapping;
    texture.wrapT = RepeatWrapping;
  }
  return texture;
}

/**
 * Textura de detalle en tonos de gris: se multiplica por el color de vértice,
 * así una sola textura sirve para hierba, roca o corteza.
 */
export function createDetailTexture(size = 32, seed = 'detail'): Texture {
  const rng = new Rng(seed);
  const [canvas, ctx] = createCanvas(size);
  const image = ctx.createImageData(size, size);
  const values = new Float32Array(size * size);

  for (let i = 0; i < values.length; i++) {
    let v = 0.86 + rng.next() * 0.1;
    const r = rng.next();
    if (r < 0.07) v = 0.7;
    else if (r > 0.95) v = 1;
    values[i] = v;
  }
  // Pequeños trazos verticales (briznas / vetas) de 2-3 píxeles.
  for (let n = 0; n < size * 1.5; n++) {
    const x = rng.int(0, size - 1);
    const y = rng.int(0, size - 1);
    const len = rng.int(2, 3);
    const shade = rng.chance(0.5) ? 0.74 : 0.98;
    for (let k = 0; k < len; k++) values[((y + k) % size) * size + x] = shade;
  }

  for (let i = 0; i < values.length; i++) {
    const c = Math.round((values[i] as number) * 255);
    image.data[i * 4] = c;
    image.data[i * 4 + 1] = c;
    image.data[i * 4 + 2] = c;
    image.data[i * 4 + 3] = 255;
  }
  ctx.putImageData(image, 0, 0);
  return toTexture(canvas, true);
}

/** Sillería en tonos de gris: bloques de piedra desfasados con juntas oscuras. */
export function createStoneTexture(size = 32, seed = 'stone'): Texture {
  const rng = new Rng(seed);
  const [canvas, ctx] = createCanvas(size);
  const image = ctx.createImageData(size, size);
  const brickW = size / 2;
  const brickH = size / 4;
  const shades = Array.from({ length: 16 }, () => 0.8 + rng.next() * 0.2);
  for (let y = 0; y < size; y++) {
    const row = Math.floor(y / brickH);
    const offset = row % 2 === 0 ? 0 : brickW / 2;
    for (let x = 0; x < size; x++) {
      const bx = Math.floor((x + offset) / brickW) % 2;
      const joint = y % brickH === 0 || (x + offset) % brickW === 0;
      let v = joint ? 0.58 : (shades[row * 2 + bx] as number) - rng.next() * 0.06;
      if (!joint && rng.chance(0.05)) v -= 0.12;
      const c = Math.round(v * 255);
      const i = (y * size + x) * 4;
      image.data[i] = c;
      image.data[i + 1] = c;
      image.data[i + 2] = c;
      image.data[i + 3] = 255;
    }
  }
  ctx.putImageData(image, 0, 0);
  return toTexture(canvas, true);
}

/** Sombra circular "de mancha" con bordes escalonados (look pixel art). */
export function createBlobShadowTexture(size = 32): Texture {
  const [canvas, ctx] = createCanvas(size);
  const image = ctx.createImageData(size, size);
  const c = (size - 1) / 2;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const d = Math.hypot(x - c, y - c) / (size / 2);
      const alpha = d >= 1 ? 0 : Math.ceil((1 - d * d) * 4) / 4;
      const i = (y * size + x) * 4;
      image.data[i] = 255;
      image.data[i + 1] = 255;
      image.data[i + 2] = 255;
      image.data[i + 3] = Math.round(alpha * 255);
    }
  }
  ctx.putImageData(image, 0, 0);
  const texture = toTexture(canvas, false);
  texture.minFilter = NearestFilter;
  texture.generateMipmaps = false;
  return texture;
}
