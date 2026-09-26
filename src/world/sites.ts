// Sitios de interés del mapa (casas derruidas, templetes, granjas, pozos...).
// Se eligen zonas bastante llanas, lejos del inicio y entre sí, y se aplana el
// terreno debajo para que las construcciones asienten bien. Lógica pura.
import { lerp, smoothstep } from '../core/math';
import type { Rng } from '../core/rng';
import { squircle, type Heightfield } from './Heightfield';

export type SiteKind = 'house' | 'temple' | 'farm' | 'well';

export interface Site {
  kind: SiteKind;
  x: number;
  z: number;
  /** Radio de la zona plana (m). */
  radius: number;
  /** Orientación de la construcción (rad). */
  rotation: number;
  /** Altura del suelo aplanado. */
  floorY: number;
}

export interface SiteRequest {
  kind: SiteKind;
  count: number;
  radius: number;
}

export interface SiteOptions {
  /** Radio squircle del área jugable. */
  limit: number;
  /** Distancia mínima al punto de inicio. */
  spawnClear: number;
  /** Separación mínima entre bordes de sitios. */
  gap: number;
  /** Desnivel máximo aceptable dentro del sitio antes de aplanar (m). */
  maxRange: number;
  /** Intentos por sitio antes de rendirse. */
  attempts?: number;
}

/** Anchura de la transición entre el suelo aplanado y el terreno original. */
export const SITE_BLEND = 5;

/** Alturas del terreno en un anillo de puntos dentro del sitio. */
function sampleHeights(hf: Heightfield, x: number, z: number, radius: number): number[] {
  const samples = [hf.heightAt(x, z)];
  for (const r of [radius * 0.5, radius]) {
    for (let a = 0; a < 8; a++) {
      const angle = (a / 8) * Math.PI * 2;
      samples.push(hf.heightAt(x + Math.cos(angle) * r, z + Math.sin(angle) * r));
    }
  }
  return samples;
}

export function pickSites(hf: Heightfield, rng: Rng, requests: readonly SiteRequest[], options: SiteOptions): Site[] {
  const sites: Site[] = [];
  const attempts = options.attempts ?? 60;
  for (const request of requests) {
    for (let n = 0; n < request.count; n++) {
      for (let attempt = 0; attempt < attempts; attempt++) {
        // Consumimos siempre los mismos números por intento (determinismo robusto).
        const u = rng.next();
        const v = rng.next();
        const rotation = rng.next() * Math.PI * 2;
        const maxR = options.limit - request.radius - SITE_BLEND - 4;
        const x = (u * 2 - 1) * maxR;
        const z = (v * 2 - 1) * maxR;
        if (squircle(x, z) > maxR) continue;
        if (Math.hypot(x, z) < options.spawnClear + request.radius) continue;
        const clash = sites.some(
          (s) => Math.hypot(s.x - x, s.z - z) < s.radius + request.radius + SITE_BLEND * 2 + options.gap,
        );
        if (clash) continue;
        const heights = sampleHeights(hf, x, z, request.radius);
        const min = Math.min(...heights);
        const max = Math.max(...heights);
        if (max - min > options.maxRange) continue;
        const sorted = heights.slice().sort((a, b) => a - b);
        const floorY = sorted[Math.floor(sorted.length / 2)] as number;
        sites.push({ kind: request.kind, x, z, radius: request.radius, rotation, floorY });
        break;
      }
    }
  }
  return sites;
}

/** Aplana el terreno bajo cada sitio, con una transición suave hacia fuera. */
export function flattenSites(hf: Heightfield, sites: readonly Site[]): void {
  const cs = hf.cellSize;
  for (const site of sites) {
    const outer = site.radius + SITE_BLEND;
    const i0 = Math.max(0, Math.floor((site.x - outer + hf.half) / cs));
    const i1 = Math.min(hf.cells, Math.ceil((site.x + outer + hf.half) / cs));
    const j0 = Math.max(0, Math.floor((site.z - outer + hf.half) / cs));
    const j1 = Math.min(hf.cells, Math.ceil((site.z + outer + hf.half) / cs));
    for (let j = j0; j <= j1; j++) {
      for (let i = i0; i <= i1; i++) {
        const x = i * cs - hf.half;
        const z = j * cs - hf.half;
        const d = Math.hypot(x - site.x, z - site.z);
        if (d > outer) continue;
        const index = j * hf.stride + i;
        const t = smoothstep(site.radius, outer, d);
        hf.heights[index] = lerp(site.floorY, hf.heights[index] as number, t);
      }
    }
  }
}

/** ¿Está (x, z) dentro de algún sitio (con margen extra)? */
export function isInAnySite(sites: readonly Site[], x: number, z: number, margin = 0): boolean {
  for (const s of sites) {
    const r = s.radius + margin;
    const dx = x - s.x;
    const dz = z - s.z;
    if (dx * dx + dz * dz < r * r) return true;
  }
  return false;
}
