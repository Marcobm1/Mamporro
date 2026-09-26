// Colocación de los interactuables del mapa (cofres, santuarios, tótems y el
// portal del jefe) y sus colisionadores. Lógica pura y determinista: sale de la
// semilla del mapa. Van en sitios llanos, sin obstáculos alrededor y repartidos.
import { DEG2RAD, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';
import { INTERACTABLE_PLACEMENT, type InteractableKind, type InteractablePlacement } from '../data/run';
import { boxCollider, circleCollider, pushOut, type Collider, type ColliderGrid, type PushResult } from './colliders';
import { squircle, type Heightfield } from './Heightfield';
import { isInAnySite, type Site } from './sites';

export interface InteractableSpot {
  kind: InteractableKind;
  x: number;
  /** Altura del suelo. */
  y: number;
  z: number;
  /** Hacia dónde mira (rad). */
  rotation: number;
}

export interface InteractableOptions {
  /** Radio squircle del área jugable. */
  limit: number;
  /** Obstáculos ya colocados (construcciones y objetos sueltos). */
  obstacles: ColliderGrid;
  placements?: readonly InteractablePlacement[];
}

/** Pendiente máxima del suelo donde se coloca uno (grados). */
const MAX_SLOPE = 16;
/** Separación mínima entre interactuables de distinto tipo. */
const MIN_GAP = 9;
/** Solo los cofres pueden quedar dentro de una construcción (si cabe). */
const SITE_MARGIN = 2;

export function placeInteractables(hf: Heightfield, sites: readonly Site[], rng: Rng, options: InteractableOptions): InteractableSpot[] {
  const spots: InteractableSpot[] = [];
  const clearOf = new Map<InteractableKind, number>();
  const normal: Vec3Like = { x: 0, y: 1, z: 0 };
  const cosMax = Math.cos(MAX_SLOPE * DEG2RAD);
  const nearby: number[] = [];
  const push: PushResult = { dx: 0, dz: 0, nx: 0, nz: 0 };
  const maxR = options.limit - 10;

  for (const placement of options.placements ?? INTERACTABLE_PLACEMENT) {
    clearOf.set(placement.kind, placement.spacing);
    let placed = 0;
    for (let attempt = 0; attempt < placement.count * 120 && placed < placement.count; attempt++) {
      // Siempre los mismos números por intento (determinismo robusto).
      const u = rng.next();
      const v = rng.next();
      const rotation = rng.next() * Math.PI * 2;
      const x = (u * 2 - 1) * maxR;
      const z = (v * 2 - 1) * maxR;
      if (squircle(x, z) > maxR) continue;
      if (Math.hypot(x, z) < placement.minSpawnDistance) continue;
      if (placement.kind !== 'chest' && isInAnySite(sites, x, z, SITE_MARGIN)) continue;
      const tooClose = spots.some((s) => {
        const gap = s.kind === placement.kind ? placement.spacing : Math.max(MIN_GAP, Math.min(placement.spacing, clearOf.get(s.kind) ?? 0));
        return Math.hypot(s.x - x, s.z - z) < gap;
      });
      if (tooClose) continue;
      hf.normalAt(x, z, normal);
      if (normal.y < cosMax) continue;
      const blocked = options.obstacles
        .query(x, z, placement.clearRadius + 1, nearby)
        .some((index) => {
          const c = options.obstacles.colliders[index];
          return c !== undefined && pushOut(c, x, z, placement.clearRadius, push);
        });
      if (blocked) continue;
      spots.push({ kind: placement.kind, x, y: hf.heightAt(x, z), z, rotation });
      placed++;
    }
  }
  return spots;
}

/** Colisionador de cada interactuable (se puede subir a los bajos). */
export function interactableCollider(spot: InteractableSpot): Collider {
  const { x, y, z, rotation } = spot;
  switch (spot.kind) {
    case 'chest':
      return boxCollider(x, z, 0.62, 0.4, rotation, y, y + 0.78, true);
    case 'shrine':
      return circleCollider(x, z, 0.85, y, y + 0.8, true);
    case 'totem':
      return circleCollider(x, z, 0.42, y, y + 2.7, false);
    case 'portal':
      return boxCollider(x, z, 0.95, 0.5, rotation, y, y + 2.8, false);
  }
}

/** Radio que se deja libre de vegetación alrededor de cada interactuable. */
export function interactableClearRadius(kind: InteractableKind): number {
  return (INTERACTABLE_PLACEMENT.find((p) => p.kind === kind)?.clearRadius ?? 2) + 1.5;
}
