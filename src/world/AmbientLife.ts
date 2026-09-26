// Fauna de ambiente (solo decorativa): bandadas de pájaros en círculos y
// mariposas revoloteando cerca de las flores. Se anima en el render, no en la lógica.
import {
  BufferAttribute,
  BufferGeometry,
  Color,
  DoubleSide,
  Group,
  InstancedMesh,
  MeshBasicMaterial,
  Object3D,
} from 'three';
import { Rng } from '../core/rng';
import { PALETTE } from '../render/palette';
import { RENDER_ORDER } from '../render/renderOrder';
import { applyRetro } from '../render/retroMaterial';
import type { CoverInstance } from './groundCover';
import type { Heightfield } from './Heightfield';

interface Bird {
  flock: number;
  radius: number;
  speed: number;
  offset: number;
  height: number;
}

interface Flock {
  x: number;
  z: number;
  y: number;
  driftX: number;
  driftZ: number;
}

interface Butterfly {
  x: number;
  y: number;
  z: number;
  phase: number;
  speed: number;
}

/** Pájaro en "V": dos alas triangulares a lo largo del eje X (se agitan en el shader). */
function birdGeometry(): BufferGeometry {
  const g = new BufferGeometry();
  // prettier-ignore
  const p = [
    0, 0, 0.25, -0.9, 0.05, -0.05, 0, 0, -0.2,
    0, 0, 0.25, 0, 0, -0.2, 0.9, 0.05, -0.05,
  ];
  g.setAttribute('position', new BufferAttribute(new Float32Array(p), 3));
  return g;
}

function butterflyGeometry(): BufferGeometry {
  const g = new BufferGeometry();
  // prettier-ignore
  const p = [
    0, 0, 0.08, -0.16, 0, 0.12, -0.14, 0, -0.1,
    0, 0, 0.08, -0.14, 0, -0.1, 0, 0, -0.06,
    0, 0, 0.08, 0.14, 0, -0.1, 0.16, 0, 0.12,
    0, 0, 0.08, 0, 0, -0.06, 0.14, 0, -0.1,
  ];
  g.setAttribute('position', new BufferAttribute(new Float32Array(p), 3));
  return g;
}

export class AmbientLife {
  readonly group = new Group();
  private readonly flocks: Flock[] = [];
  private readonly birds: Bird[] = [];
  private readonly butterflies: Butterfly[] = [];
  private readonly birdMesh: InstancedMesh;
  private readonly butterflyMesh: InstancedMesh;
  private readonly dummy = new Object3D();

  constructor(
    private readonly hf: Heightfield,
    flowers: readonly CoverInstance[],
    seed: string,
    limit: number,
  ) {
    const rng = new Rng(`${seed}/fauna`);
    for (let f = 0; f < 3; f++) {
      const x = rng.range(-limit * 0.6, limit * 0.6);
      const z = rng.range(-limit * 0.6, limit * 0.6);
      this.flocks.push({ x, z, y: hf.heightAt(x, z) + rng.range(28, 42), driftX: rng.range(-0.6, 0.6), driftZ: rng.range(-0.6, 0.6) });
      const n = rng.int(4, 6);
      for (let i = 0; i < n; i++) {
        this.birds.push({ flock: f, radius: rng.range(10, 18), speed: rng.range(0.25, 0.4), offset: rng.next() * Math.PI * 2, height: rng.range(-2, 2) });
      }
    }
    const count = Math.min(28, flowers.length);
    for (let i = 0; i < count; i++) {
      const anchor = flowers[Math.floor(rng.next() * flowers.length)] as CoverInstance;
      this.butterflies.push({ x: anchor.x, y: anchor.y, z: anchor.z, phase: rng.next() * Math.PI * 2, speed: rng.range(0.6, 1.1) });
    }

    const birdMaterial = applyRetro(new MeshBasicMaterial({ color: PALETTE.bird, side: DoubleSide }), {
      flap: { amplitude: 0.55, speed: 9 },
    });
    this.birdMesh = new InstancedMesh(birdGeometry(), birdMaterial, Math.max(1, this.birds.length));
    this.birdMesh.count = this.birds.length;
    this.birdMesh.frustumCulled = false;

    const butterflyMaterial = applyRetro(new MeshBasicMaterial({ color: 0xffffff, side: DoubleSide }), {
      flap: { amplitude: 2.2, speed: 26 },
    });
    this.butterflyMesh = new InstancedMesh(butterflyGeometry(), butterflyMaterial, Math.max(1, this.butterflies.length));
    this.butterflyMesh.count = this.butterflies.length;
    this.butterflyMesh.frustumCulled = false;
    const colors = [PALETTE.butterflyA, PALETTE.butterflyB, PALETTE.butterflyC];
    const color = new Color();
    this.butterflies.forEach((_, i) => this.butterflyMesh.setColorAt(i, color.setHex(colors[i % colors.length] ?? 0xffffff)));

    this.birdMesh.renderOrder = RENDER_ORDER.afterPlayer;
    this.butterflyMesh.renderOrder = RENDER_ORDER.afterPlayer;
    this.group.add(this.birdMesh, this.butterflyMesh);
    this.group.name = 'fauna';
    this.update(0);
  }

  update(time: number): void {
    const d = this.dummy;
    this.birds.forEach((bird, i) => {
      const flock = this.flocks[bird.flock] as Flock;
      const cx = flock.x + Math.sin(time * 0.05 + bird.flock) * 30 * flock.driftX;
      const cz = flock.z + Math.cos(time * 0.04 + bird.flock) * 30 * flock.driftZ;
      const a = time * bird.speed + bird.offset;
      d.position.set(cx + Math.cos(a) * bird.radius, flock.y + bird.height + Math.sin(a * 2) * 1.5, cz + Math.sin(a) * bird.radius);
      // Mira en la dirección de la tangente del círculo.
      d.rotation.set(0, -a, Math.sin(a * 3) * 0.15);
      d.scale.setScalar(1.3);
      d.updateMatrix();
      this.birdMesh.setMatrixAt(i, d.matrix);
    });
    this.birdMesh.instanceMatrix.needsUpdate = true;

    this.butterflies.forEach((b, i) => {
      const t = time * b.speed + b.phase;
      const x = b.x + Math.sin(t * 0.9) * 1.6 + Math.sin(t * 2.3) * 0.3;
      const z = b.z + Math.cos(t * 0.7) * 1.6;
      const ground = this.hf.heightAt(x, z);
      d.position.set(x, ground + 0.7 + Math.sin(t * 1.7) * 0.35, z);
      d.rotation.set(0, t, 0);
      d.scale.setScalar(1);
      d.updateMatrix();
      this.butterflyMesh.setMatrixAt(i, d.matrix);
    });
    this.butterflyMesh.instanceMatrix.needsUpdate = true;
  }
}
