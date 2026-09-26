// Cámara en tercera persona: orbita alrededor del jugador con el ratón, se acerca
// cuando el terreno se interpone y abre el FOV con la velocidad.
import { PerspectiveCamera, Vector3 } from 'three';
import { clamp, damp, lerp, type Vec3Like } from '../core/math';
import { CAMERA_CONFIG } from '../data/config';
import type { Heightfield } from '../world/Heightfield';

const COLLISION_STEPS = 16;

export interface CameraFollowState {
  sliding: boolean;
  speed: number;
  moveSpeed: number;
}

export class CameraRig {
  readonly camera: PerspectiveCamera;
  /** Giro horizontal (rad); 0 = mirando hacia -Z. */
  yaw = 0;
  /** Inclinación (rad); negativo = mirando hacia abajo. */
  pitch = -0.3;
  /** Multiplicador de sensibilidad elegido en Opciones. */
  sensitivity = 1;
  /** Multiplicador de la distancia al jugador (solo lo usan las pruebas automáticas). */
  distanceScale = 1;

  private distance: number = CAMERA_CONFIG.distance;
  private pivotHeight: number = CAMERA_CONFIG.pivotHeight;
  private fovExtra = 0;
  private readonly pivot = new Vector3();
  private readonly dir = new Vector3();

  constructor(aspect: number) {
    this.camera = new PerspectiveCamera(CAMERA_CONFIG.fov, aspect, CAMERA_CONFIG.near, CAMERA_CONFIG.far);
  }

  /** Aplica el movimiento del ratón (en píxeles). */
  look(dx: number, dy: number): void {
    const k = CAMERA_CONFIG.baseSensitivity * this.sensitivity;
    this.yaw -= dx * k;
    this.pitch = clamp(this.pitch - dy * k, CAMERA_CONFIG.minPitch, CAMERA_CONFIG.maxPitch);
  }

  /** Dirección "adelante" de la cámara proyectada en el suelo. */
  forward(out: Vec3Like): Vec3Like {
    out.x = -Math.sin(this.yaw);
    out.y = 0;
    out.z = -Math.cos(this.yaw);
    return out;
  }

  /** Dirección "derecha" de la cámara proyectada en el suelo. */
  right(out: Vec3Like): Vec3Like {
    out.x = Math.cos(this.yaw);
    out.y = 0;
    out.z = -Math.sin(this.yaw);
    return out;
  }

  setAspect(aspect: number): void {
    this.camera.aspect = aspect;
    this.camera.updateProjectionMatrix();
  }

  /** Coloca la cámara directamente (sin suavizado), p. ej. al empezar una partida. */
  snap(): void {
    this.distance = CAMERA_CONFIG.distance;
  }

  update(target: Vec3Like, follow: CameraFollowState, hf: Heightfield, dt: number): void {
    const cfg = CAMERA_CONFIG;
    const desiredPivot = follow.sliding ? cfg.pivotHeightSliding : cfg.pivotHeight;
    this.pivotHeight = lerp(this.pivotHeight, desiredPivot, damp(8, dt));
    this.pivot.set(target.x, target.y + this.pivotHeight, target.z);

    const cp = Math.cos(this.pitch);
    this.dir.set(-Math.sin(this.yaw) * cp, Math.sin(this.pitch), -Math.cos(this.yaw) * cp);

    // Recorremos el brazo de la cámara desde el jugador hacia atrás: si el terreno
    // lo corta, la cámara se acerca de golpe y se aleja de nuevo con suavidad.
    const maxDistance = cfg.distance * this.distanceScale;
    let allowed: number = maxDistance;
    const step = maxDistance / COLLISION_STEPS;
    for (let i = 1; i <= COLLISION_STEPS; i++) {
      const t = step * i;
      const px = this.pivot.x - this.dir.x * t;
      const py = this.pivot.y - this.dir.y * t;
      const pz = this.pivot.z - this.dir.z * t;
      if (hf.heightAt(px, pz) + cfg.collisionMargin > py) {
        allowed = Math.max(cfg.minDistance, t - step);
        break;
      }
    }
    this.distance = allowed < this.distance ? allowed : lerp(this.distance, allowed, damp(4, dt));

    const cam = this.camera.position;
    cam.set(
      this.pivot.x - this.dir.x * this.distance,
      this.pivot.y - this.dir.y * this.distance,
      this.pivot.z - this.dir.z * this.distance,
    );
    const floor = hf.heightAt(cam.x, cam.z) + cfg.collisionMargin;
    if (cam.y < floor) cam.y = floor;
    this.camera.lookAt(this.pivot.x + this.dir.x, this.pivot.y + this.dir.y, this.pivot.z + this.dir.z);

    // Sensación de velocidad: el FOV se abre al superar la velocidad normal.
    const over = clamp((follow.speed - follow.moveSpeed) / (follow.moveSpeed * 1.5), 0, 1);
    this.fovExtra = lerp(this.fovExtra, over * cfg.fovBoost, damp(4, dt));
    this.camera.fov = cfg.fov + this.fovExtra;
    this.camera.updateProjectionMatrix();
  }
}
