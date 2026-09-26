// Parámetros ajustables del juego. Cambiar el "feel" o el tamaño del mapa
// debería requerir tocar solo este fichero.
import type { PlayerTuning } from '../entities/playerPhysics';
import type { TerrainConfig } from '../world/Heightfield';

export const TERRAIN_CONFIG: TerrainConfig = {
  size: 320,
  cells: 256,
  borderStart: 128,
  spawnRadius: 14,
  relief: 26,
  terraceStep: 4.5,
};

export const WORLD_CONFIG = {
  /** Radio squircle del muro invisible (ya en la subida de las montañas del borde). */
  playableRadius: 136,
  /** Zona sin decoración alrededor del punto de inicio. */
  clearSpawnRadius: 12,
} as const;

export const PLAYER_TUNING: PlayerTuning = {
  radius: 0.4,
  height: 1.55,
  groundAccel: 75,
  groundDecel: 55,
  airAccel: 30,
  overspeedDecel: 9,
  gravity: 27,
  fallGravityMultiplier: 1.55,
  lowJumpGravityMultiplier: 2.3,
  maxFallSpeed: 42,
  jumpVelocity: 9.8,
  coyoteTime: 0.1,
  jumpBuffer: 0.13,
  stepHeight: 0.45,
  maxSlopeDeg: 48,
  slide: {
    entrySpeedMultiplier: 1.45,
    boost: 3.5,
    minSpeed: 3.2,
    friction: 4,
    brakeFriction: 14,
    gravityScale: 1.25,
    turnRate: 2.4,
    cooldown: 0.6,
    maxSpeed: 30,
    jumpBoost: 1.5,
    boostSpeedCap: 1.8,
  },
};

/** Estadísticas base del jugador (luego las modificarán personajes, tomos y objetos). */
export const PLAYER_BASE_STATS = {
  moveSpeed: 8,
} as const;

export const CAMERA_CONFIG = {
  distance: 6.2,
  minDistance: 1.1,
  /** Altura del punto al que mira la cámara, sobre los pies del jugador. */
  pivotHeight: 1.9,
  pivotHeightSliding: 1.2,
  fov: 70,
  /** Grados de FOV extra a máxima velocidad (sensación de velocidad). */
  fovBoost: 12,
  minPitch: -1.25,
  maxPitch: 0.55,
  /** Radianes por píxel de ratón con sensibilidad 1. */
  baseSensitivity: 0.0022,
  collisionMargin: 0.35,
  near: 0.1,
  /** Suficiente para ver las montañas del borde desde cualquier punto sin recortes. */
  far: 420,
} as const;

export const RENDER_CONFIG = {
  fogNear: 30,
  fogFar: 170,
} as const;
