// Dibuja a todos los enemigos: una malla instanciada por tipo (una llamada de
// dibujo por tipo, da igual cuántos haya), con animación y destello al recibir golpes.
import {
  DynamicDrawUsage,
  Group,
  InstancedBufferAttribute,
  InstancedMesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  PlaneGeometry,
  type Texture,
} from 'three';
import { ENEMY_LIST } from '../data/enemies';
import { ENEMY_MODELS } from '../entities/enemyModels';
import type { BossController } from '../systems/BossController';
import { ENEMY_STATE, type EnemySystem } from '../systems/EnemySystem';
import { RENDER_ORDER } from './renderOrder';
import { FLASH_ATTRIBUTE, applyRetro } from './retroMaterial';

/** Escribe una matriz (giro en Y + escala + posición) directamente en el array de instancias. */
export function writeYawMatrix(
  array: Float32Array,
  index: number,
  x: number,
  y: number,
  z: number,
  yaw: number,
  sx: number,
  sy: number,
  sz: number,
): void {
  const c = Math.cos(yaw);
  const s = Math.sin(yaw);
  const o = index * 16;
  array[o] = c * sx;
  array[o + 1] = 0;
  array[o + 2] = -s * sx;
  array[o + 3] = 0;
  array[o + 4] = 0;
  array[o + 5] = sy;
  array[o + 6] = 0;
  array[o + 7] = 0;
  array[o + 8] = s * sz;
  array[o + 9] = 0;
  array[o + 10] = c * sz;
  array[o + 11] = 0;
  array[o + 12] = x;
  array[o + 13] = y;
  array[o + 14] = z;
  array[o + 15] = 1;
}

/** Cuántos puede haber a la vez de los especiales (del resto, todos los que quepan). */
const SPECIAL_CAPACITY = { elite: 24, boss: 2 } as const;
/**
 * Destello de los golpes según el tamaño: a los grandes les llueven golpes sin
 * parar y, a tope, se quedarían blancos (no se vería el modelo).
 */
const FLASH_SCALE = { elite: 0.6, boss: 0.3 } as const;

/** Forma del enemigo en este frame: desplazamiento vertical y escala por ejes. */
interface Pose {
  lift: number;
  sx: number;
  sy: number;
  sz: number;
}

export class EnemyRenderer {
  readonly group = new Group();
  private readonly meshes: InstancedMesh[] = [];
  private readonly flashes: InstancedBufferAttribute[] = [];
  private readonly counts: Int32Array;
  private readonly capacities: number[] = [];
  private readonly shadows: InstancedMesh;
  private readonly pose: Pose = { lift: 0, sx: 1, sy: 1, sz: 1 };

  constructor(capacity: number, shadowTexture: Texture) {
    this.counts = new Int32Array(ENEMY_LIST.length);
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }), { flash: true });
    for (const def of ENEMY_LIST) {
      const geometry = ENEMY_MODELS[def.id]();
      const max = def.special ? Math.min(capacity, SPECIAL_CAPACITY[def.special]) : capacity;
      this.capacities.push(max);
      const flash = new InstancedBufferAttribute(new Float32Array(max), 1);
      flash.setUsage(DynamicDrawUsage);
      geometry.setAttribute(FLASH_ATTRIBUTE, flash);
      const mesh = new InstancedMesh(geometry, material, max);
      mesh.instanceMatrix.setUsage(DynamicDrawUsage);
      mesh.count = 0;
      mesh.frustumCulled = false;
      this.meshes.push(mesh);
      this.flashes.push(flash);
      this.group.add(mesh);
    }
    // Sombras de mancha bajo cada enemigo (ayudan a leer la profundidad).
    const shadowMaterial = new MeshBasicMaterial({
      color: 0x000000,
      map: shadowTexture,
      transparent: true,
      opacity: 0.4,
      depthWrite: false,
      polygonOffset: true,
      polygonOffsetFactor: -4,
    });
    this.shadows = new InstancedMesh(new PlaneGeometry(1, 1).rotateX(-Math.PI / 2), shadowMaterial, capacity);
    this.shadows.instanceMatrix.setUsage(DynamicDrawUsage);
    this.shadows.count = 0;
    this.shadows.frustumCulled = false;
    this.shadows.renderOrder = RENDER_ORDER.blobShadow;
    this.group.add(this.shadows);
  }

  /** `boss`: el controlador del jefe, para animar sus avisos (o null). */
  update(enemies: EnemySystem, alpha: number, boss: BossController | null): void {
    const counts = this.counts;
    counts.fill(0);
    const shadowArray = this.shadows.instanceMatrix.array as Float32Array;
    const pose = this.pose;
    for (let i = 0; i < enemies.count; i++) {
      const type = enemies.type[i] as number;
      const def = ENEMY_LIST[type];
      const mesh = this.meshes[type];
      const flash = this.flashes[type];
      if (!def || !mesh || !flash) continue;
      const slot = counts[type] as number;
      if (slot >= (this.capacities[type] as number)) continue;
      counts[type] = slot + 1;

      const x = (enemies.px[i] as number) + ((enemies.x[i] as number) - (enemies.px[i] as number)) * alpha;
      const ground = (enemies.py[i] as number) + ((enemies.y[i] as number) - (enemies.py[i] as number)) * alpha;
      const z = (enemies.pz[i] as number) + ((enemies.z[i] as number) - (enemies.pz[i] as number)) * alpha;
      this.animate(enemies, i, boss);
      writeYawMatrix(mesh.instanceMatrix.array as Float32Array, slot, x, ground + pose.lift, z, enemies.heading[i] as number, pose.sx, pose.sy, pose.sz);
      (flash.array as Float32Array)[slot] = (enemies.flash[i] as number) * (def.special ? FLASH_SCALE[def.special] : 1);
      // La sombra encoge si el enemigo salta.
      const size = def.radius * 2.4 * Math.max(0.4, 1 - pose.lift * 0.15);
      writeYawMatrix(shadowArray, i, x, ground + 0.04, z, 0, size, 1, size);
    }
    this.meshes.forEach((mesh, type) => {
      mesh.count = counts[type] as number;
      mesh.instanceMatrix.needsUpdate = true;
      const flash = this.flashes[type];
      if (flash) flash.needsUpdate = true;
    });
    this.shadows.count = enemies.count;
    this.shadows.instanceMatrix.needsUpdate = true;
  }

  /** Animación de cada tipo, y de sus avisos: se hinchan, tiemblan, se agachan o saltan. */
  private animate(enemies: EnemySystem, i: number, boss: BossController | null): void {
    const pose = this.pose;
    const def = ENEMY_LIST[enemies.type[i] as number];
    const phase = enemies.phase[i] as number;
    const state = enemies.state[i] as number;
    pose.lift = 0;
    pose.sx = pose.sy = pose.sz = 1;
    if (!def) return;
    switch (def.id) {
      case 'pelusa': {
        // Avanza a saltitos, estirándose y aplastándose.
        const hop = Math.abs(Math.sin(phase * 0.5));
        pose.lift = hop * 0.22;
        pose.sy = 1 + (hop - 0.5) * 0.24;
        pose.sx = pose.sz = 1 - (hop - 0.5) * 0.12;
        break;
      }
      case 'cucaracha':
        // Tiembla a toda velocidad.
        pose.lift = Math.abs(Math.sin(phase * 1.5)) * 0.04;
        pose.sx = pose.sz = 1 + Math.sin(phase * 3) * 0.05;
        break;
      case 'taper':
        // Anda como un pato, balanceándose.
        pose.lift = Math.abs(Math.sin(phase * 0.6)) * 0.05;
        pose.sx = 1 + Math.sin(phase * 0.6) * 0.05;
        pose.sy = 1 - Math.abs(Math.sin(phase * 0.6)) * 0.04;
        break;
      case 'paloma': {
        pose.lift = Math.abs(Math.sin(phase * 0.9)) * 0.06;
        // Antes de escupir se hincha como un buche.
        if (state === ENEMY_STATE.windup && def.ranged) {
          const t = 1 - (enemies.stateTime[i] as number) / def.ranged.windup;
          pose.sx = pose.sz = 1 + t * 0.35;
          pose.sy = 1 + t * 0.2;
        }
        break;
      }
      case 'rata':
        if (state === ENEMY_STATE.windup) {
          // Se agacha temblando antes de embestir.
          pose.sy = 0.84;
          pose.sx = pose.sz = 1.08 + Math.sin(phase * 9) * 0.05;
        } else if (state === ENEMY_STATE.dash) {
          pose.sz = 1.25;
          pose.sy = 0.9;
        } else {
          pose.lift = Math.abs(Math.sin(phase * 0.8)) * 0.12;
        }
        break;
      case 'pelusaMadre':
        this.animateBoss(boss, phase, state);
        break;
    }
  }

  private animateBoss(boss: BossController | null, phase: number, state: number): void {
    const pose = this.pose;
    // Respira despacio.
    const breath = Math.sin(phase * 0.25) * 0.03;
    pose.sx = pose.sz = 1 + breath;
    pose.sy = 1 - breath;
    if (!boss) return;
    const t = boss.windupProgress;
    if (state === ENEMY_STATE.windup) {
      if (boss.attack === 'slam') {
        // Se agacha y salta: cae con el culetazo al terminar el aviso.
        if (t < 0.45) {
          pose.sy = 1 - (t / 0.45) * 0.3;
          pose.sx = pose.sz = 1 + (t / 0.45) * 0.15;
        } else {
          pose.lift = Math.sin(((t - 0.45) / 0.55) * Math.PI * 0.5) * 2.6;
          pose.sy = 1.12;
          pose.sx = pose.sz = 0.92;
        }
      } else if (boss.attack === 'sneeze') {
        pose.sx = pose.sz = 1 + t * 0.28;
        pose.sy = 1 + t * 0.18;
      } else {
        // Rodillo: se inclina y tiembla cogiendo carrerilla.
        pose.sx = pose.sz = 1 + Math.sin(phase * 7) * 0.05;
        pose.sy = 0.9;
      }
    } else if (state === ENEMY_STATE.dash) {
      // Rueda: bota y se deforma.
      pose.lift = Math.abs(Math.sin(phase * 2.5)) * 0.4;
      pose.sy = 0.9 + Math.abs(Math.sin(phase * 2.5)) * 0.15;
    }
  }
}
