// Frenado al atravesar una horda (lógica pura): los enemigos que Doña Remedios
// empuja de frente la frenan según su masa, con un tope. Ver CROWD_CONFIG.
import { CROWD_CONFIG } from '../data/config';
import {
  brakeHorizontal,
  stepPlayer,
  type PhysicsWorld,
  type PlayerBody,
  type PlayerIntent,
  type PlayerTuning,
} from '../entities/playerPhysics';

/** Frenado (0..maxSlow) que corresponde a un empuje (masa empujada de frente). */
export function crowdSlowFor(pressure: number): number {
  return Math.min(CROWD_CONFIG.maxSlow, Math.max(0, pressure) * CROWD_CONFIG.slowPerMass);
}

/** Acerca el frenado actual al objetivo de forma suave (sin saltos de un tick a otro). */
export function smoothCrowdSlow(current: number, target: number, dt: number): number {
  return current + (target - current) * Math.min(1, dt * CROWD_CONFIG.response);
}

/**
 * Paso de física del jugador con el frenado de la horda: baja la velocidad a la
 * que corre y, además, frena lo que vaya más rápido (deslizamientos, saltos).
 */
export function stepPlayerInCrowd(
  body: PlayerBody,
  intent: PlayerIntent,
  world: PhysicsWorld,
  tune: PlayerTuning,
  moveSpeed: number,
  crowdSlow: number,
  dt: number,
): void {
  const speed = moveSpeed * (1 - crowdSlow);
  stepPlayer(body, intent, world, tune, speed, dt);
  if (crowdSlow > 0) brakeHorizontal(body, speed, CROWD_CONFIG.brake * (crowdSlow / CROWD_CONFIG.maxSlow) * dt);
}
