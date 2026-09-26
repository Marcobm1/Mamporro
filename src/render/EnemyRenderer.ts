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
import type { EnemySystem } from '../systems/EnemySystem';
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

export class EnemyRenderer {
  readonly group = new Group();
  private readonly meshes: InstancedMesh[] = [];
  private readonly flashes: InstancedBufferAttribute[] = [];
  private readonly counts: Int32Array;
  private readonly shadows: InstancedMesh;

  constructor(capacity: number, shadowTexture: Texture) {
    this.counts = new Int32Array(ENEMY_LIST.length);
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }), { flash: true });
    for (const def of ENEMY_LIST) {
      const geometry = ENEMY_MODELS[def.id]();
      const flash = new InstancedBufferAttribute(new Float32Array(capacity), 1);
      flash.setUsage(DynamicDrawUsage);
      geometry.setAttribute(FLASH_ATTRIBUTE, flash);
      const mesh = new InstancedMesh(geometry, material, capacity);
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

  update(enemies: EnemySystem, alpha: number): void {
    const counts = this.counts;
    counts.fill(0);
    const shadowArray = this.shadows.instanceMatrix.array as Float32Array;
    for (let i = 0; i < enemies.count; i++) {
      const type = enemies.type[i] as number;
      const def = ENEMY_LIST[type];
      const mesh = this.meshes[type];
      const flash = this.flashes[type];
      if (!def || !mesh || !flash) continue;
      const slot = counts[type] as number;
      counts[type] = slot + 1;

      const x = (enemies.px[i] as number) + ((enemies.x[i] as number) - (enemies.px[i] as number)) * alpha;
      const ground = (enemies.py[i] as number) + ((enemies.y[i] as number) - (enemies.py[i] as number)) * alpha;
      const z = (enemies.pz[i] as number) + ((enemies.z[i] as number) - (enemies.pz[i] as number)) * alpha;
      const phase = enemies.phase[i] as number;
      let y = ground;
      let sx = 1;
      let sy = 1;
      if (def.id === 'pelusa') {
        // Avanza a saltitos, estirándose y aplastándose.
        const hop = Math.abs(Math.sin(phase * 0.5));
        y += hop * 0.22;
        sy = 1 + (hop - 0.5) * 0.24;
        sx = 1 - (hop - 0.5) * 0.12;
      } else {
        // La cucaracha tiembla a toda velocidad.
        y += Math.abs(Math.sin(phase * 1.5)) * 0.04;
        sx = 1 + Math.sin(phase * 3) * 0.05;
      }
      writeYawMatrix(mesh.instanceMatrix.array as Float32Array, slot, x, y, z, enemies.heading[i] as number, sx, sy, sx);
      (flash.array as Float32Array)[slot] = enemies.flash[i] as number;
      const size = def.radius * 2.4;
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
}
