// Utilidades matemáticas puras (sin dependencias de Three.js) usadas por la lógica del juego.

export interface Vec3Like {
  x: number;
  y: number;
  z: number;
}

export const TAU = Math.PI * 2;
export const DEG2RAD = Math.PI / 180;
export const RAD2DEG = 180 / Math.PI;

export function clamp(v: number, min: number, max: number): number {
  return v < min ? min : v > max ? max : v;
}

export function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

export function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

export function inverseLerp(a: number, b: number, v: number): number {
  return a === b ? 0 : (v - a) / (b - a);
}

export function smoothstep(edge0: number, edge1: number, x: number): number {
  const t = clamp01((x - edge0) / (edge1 - edge0));
  return t * t * (3 - 2 * t);
}

/**
 * Factor de suavizado independiente del framerate: `lerp(a, b, damp(k, dt))`
 * converge hacia `b` a la misma velocidad sin importar el tamaño de `dt`.
 */
export function damp(lambda: number, dt: number): number {
  return 1 - Math.exp(-lambda * dt);
}

/** Normaliza un ángulo al rango [-PI, PI). */
export function wrapAngle(a: number): number {
  a = (a + Math.PI) % TAU;
  if (a < 0) a += TAU;
  return a - Math.PI;
}

/** Interpola ángulos por el camino más corto. */
export function lerpAngle(a: number, b: number, t: number): number {
  return a + wrapAngle(b - a) * t;
}

/**
 * Curva en S con pendiente ajustable ("gain"): `s = 1` es lineal y valores
 * altos se acercan a un escalón. Se usa para crear mesetas y acantilados.
 */
export function gain(x: number, s: number): number {
  const xs = Math.pow(clamp01(x), s);
  const ys = Math.pow(clamp01(1 - x), s);
  return xs / (xs + ys);
}
