// Render instanciado de proyectiles, gemas, monedas y proyectiles enemigos, y el
// efecto visual del aura.
import {
  BoxGeometry,
  CircleGeometry,
  Color,
  CylinderGeometry,
  DynamicDrawUsage,
  Group,
  InstancedMesh,
  Mesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  OctahedronGeometry,
  RingGeometry,
} from 'three';
import { clamp } from '../core/math';
import type { EnemyProjectileSystem } from '../systems/EnemyProjectileSystem';
import { gemTier, type PickupSystem } from '../systems/PickupSystem';
import type { ProjectileSystem } from '../systems/ProjectileSystem';
import { colored, mergeColored } from './geometry';
import { PALETTE } from './palette';
import { applyRetro } from './retroMaterial';
import { writeYawMatrix } from './EnemyRenderer';
import { RENDER_ORDER } from './renderOrder';

/** Chanclas voladoras girando como un frisbi. */
export class ProjectileRenderer {
  readonly mesh: InstancedMesh;

  constructor(capacity: number) {
    const geometry = mergeColored([
      colored(new BoxGeometry(0.22, 0.05, 0.5), PALETTE.chancla),
      colored(new BoxGeometry(0.2, 0.06, 0.1).translate(0, 0.05, -0.1), PALETTE.chanclaStrap),
    ]);
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }));
    this.mesh = new InstancedMesh(geometry, material, capacity);
    this.mesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.mesh.count = 0;
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.afterPlayer;
  }

  update(p: ProjectileSystem, alpha: number): void {
    const array = this.mesh.instanceMatrix.array as Float32Array;
    for (let i = 0; i < p.count; i++) {
      const x = (p.px[i] as number) + ((p.x[i] as number) - (p.px[i] as number)) * alpha;
      const y = (p.py[i] as number) + ((p.y[i] as number) - (p.py[i] as number)) * alpha;
      const z = (p.pz[i] as number) + ((p.z[i] as number) - (p.pz[i] as number)) * alpha;
      const scale = (p.radius[i] as number) / 0.35;
      writeYawMatrix(array, i, x, y, z, p.spin[i] as number, scale, scale, scale);
    }
    this.mesh.count = p.count;
    this.mesh.instanceMatrix.needsUpdate = true;
  }
}

const TIER_COLORS = [PALETTE.gemBlue, PALETTE.gemGreen, PALETTE.gemRed, PALETTE.gemPurple];
const TIER_SCALE = [1, 1.25, 1.55, 1.95];

/** Gemas de experiencia: flotan, giran y cambian de color según su valor. */
export class GemRenderer {
  readonly mesh: InstancedMesh;
  private readonly color = new Color();

  constructor(capacity: number) {
    const material = applyRetro(new MeshBasicMaterial({ color: 0xffffff }));
    this.mesh = new InstancedMesh(new OctahedronGeometry(0.17, 0), material, capacity);
    this.mesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.mesh.count = 0;
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.afterPlayer;
    // Crea el atributo de color por instancia.
    this.mesh.setColorAt(0, this.color.setHex(PALETTE.gemBlue));
  }

  update(gems: PickupSystem): void {
    const array = this.mesh.instanceMatrix.array as Float32Array;
    const colors = this.mesh.instanceColor;
    for (let i = 0; i < gems.count; i++) {
      const tier = gemTier(gems.value[i] as number);
      const phase = gems.phase[i] as number;
      const scale = TIER_SCALE[tier] ?? 1;
      writeYawMatrix(array, i, gems.x[i] as number, (gems.y[i] as number) + Math.sin(phase) * 0.08, gems.z[i] as number, phase * 0.8, scale, scale * 1.3, scale);
      this.mesh.setColorAt(i, this.color.setHex(TIER_COLORS[tier] ?? PALETTE.gemBlue));
    }
    this.mesh.count = gems.count;
    this.mesh.instanceMatrix.needsUpdate = true;
    if (colors) colors.needsUpdate = true;
  }
}

/** Nube de naftalina: disco translúcido con borde que "late" en cada pulso de daño. */
export class AuraRenderer {
  readonly group = new Group();
  private readonly disc: Mesh;
  private readonly ring: Mesh;
  private readonly discMaterial: MeshBasicMaterial;
  private readonly ringMaterial: MeshBasicMaterial;

  constructor() {
    this.discMaterial = new MeshBasicMaterial({ color: PALETTE.aura, transparent: true, opacity: 0.16, depthWrite: false });
    this.ringMaterial = new MeshBasicMaterial({ color: PALETTE.auraEdge, transparent: true, opacity: 0.5, depthWrite: false });
    this.disc = new Mesh(new CircleGeometry(1, 28).rotateX(-Math.PI / 2), applyRetro(this.discMaterial));
    this.ring = new Mesh(new RingGeometry(0.94, 1, 28).rotateX(-Math.PI / 2), applyRetro(this.ringMaterial));
    this.disc.renderOrder = RENDER_ORDER.aura;
    this.ring.renderOrder = RENDER_ORDER.aura;
    this.group.add(this.disc, this.ring);
    this.group.visible = false;
  }

  update(visible: boolean, x: number, y: number, z: number, radius: number, sincePulse: number, time: number): void {
    this.group.visible = visible;
    if (!visible) return;
    const pulse = clamp(1 - sincePulse / 0.35, 0, 1);
    const wobble = 1 + Math.sin(time * 3) * 0.02;
    this.group.position.set(x, y + 0.12, z);
    this.group.scale.setScalar(radius * wobble * (1 + pulse * 0.06));
    this.ring.rotation.y = time * 0.4;
    this.discMaterial.opacity = 0.17 + pulse * 0.16;
    this.ringMaterial.opacity = 0.4 + pulse * 0.35;
  }
}

const COIN_TIERS = [1, 5, 20] as const;
const COIN_SCALE = [1, 1.3, 1.7];

/** Monedas de oro: discos que giran y flotan (las de más valor, más grandes). */
export class CoinRenderer {
  readonly mesh: InstancedMesh;

  constructor(capacity: number) {
    const geometry = mergeColored([
      colored(new CylinderGeometry(0.27, 0.27, 0.06, 10).rotateX(Math.PI / 2), PALETTE.coin),
      colored(new CylinderGeometry(0.16, 0.16, 0.07, 8).rotateX(Math.PI / 2), PALETTE.coinDark),
    ]);
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true, emissive: 0x5a4000 }));
    this.mesh = new InstancedMesh(geometry, material, capacity);
    this.mesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.mesh.count = 0;
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.afterPlayer;
  }

  update(coins: PickupSystem): void {
    const array = this.mesh.instanceMatrix.array as Float32Array;
    for (let i = 0; i < coins.count; i++) {
      const value = coins.value[i] as number;
      let tier = 0;
      for (let t = 1; t < COIN_TIERS.length; t++) if (value >= (COIN_TIERS[t] as number)) tier = t;
      const scale = COIN_SCALE[tier] ?? 1;
      const phase = coins.phase[i] as number;
      writeYawMatrix(array, i, coins.x[i] as number, (coins.y[i] as number) + Math.sin(phase) * 0.08, coins.z[i] as number, phase * 1.6, scale, scale, scale);
    }
    this.mesh.count = coins.count;
    this.mesh.instanceMatrix.needsUpdate = true;
  }
}

/** Proyectiles enemigos: pipas de las palomas y bolas de polvo del jefe. */
export class EnemyShotRenderer {
  readonly group = new Group();
  private readonly meshes: InstancedMesh[];

  constructor(capacity: number) {
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }));
    const pipa = mergeColored([
      colored(new OctahedronGeometry(0.2, 0).scale(0.55, 0.4, 1), PALETTE.pipa),
      colored(new BoxGeometry(0.03, 0.09, 0.3).translate(0.07, 0.02, 0), PALETTE.pipaStripe),
      colored(new BoxGeometry(0.03, 0.09, 0.3).translate(-0.07, 0.02, 0), PALETTE.pipaStripe),
    ]);
    const dust = colored(new OctahedronGeometry(0.45, 1), PALETTE.dustBall);
    this.meshes = [pipa, dust].map((geometry) => {
      const mesh = new InstancedMesh(geometry, material, capacity);
      mesh.instanceMatrix.setUsage(DynamicDrawUsage);
      mesh.count = 0;
      mesh.frustumCulled = false;
      mesh.renderOrder = RENDER_ORDER.afterPlayer;
      this.group.add(mesh);
      return mesh;
    });
  }

  update(shots: EnemyProjectileSystem, alpha: number): void {
    const counts = [0, 0];
    for (let i = 0; i < shots.count; i++) {
      const kind = shots.kind[i] as number;
      const mesh = this.meshes[kind];
      if (!mesh) continue;
      const slot = counts[kind] as number;
      counts[kind] = slot + 1;
      const x = (shots.px[i] as number) + ((shots.x[i] as number) - (shots.px[i] as number)) * alpha;
      const y = (shots.py[i] as number) + ((shots.y[i] as number) - (shots.py[i] as number)) * alpha;
      const z = (shots.pz[i] as number) + ((shots.z[i] as number) - (shots.pz[i] as number)) * alpha;
      const spin = shots.spin[i] as number;
      const heading = Math.atan2(-(shots.vx[i] as number), -(shots.vz[i] as number));
      const wobble = kind === 1 ? 1 + Math.sin(spin * 2) * 0.12 : 1;
      writeYawMatrix(mesh.instanceMatrix.array as Float32Array, slot, x, y, z, kind === 1 ? spin : heading, wobble, wobble, wobble);
    }
    this.meshes.forEach((mesh, kind) => {
      mesh.count = counts[kind] as number;
      mesh.instanceMatrix.needsUpdate = true;
    });
  }
}
