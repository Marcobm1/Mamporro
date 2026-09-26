// Mallas de los interactuables: baúles (con la tapa que se abre), mesas camilla
// con su zona de carga, tótems de cacerolas y el armario misterioso del jefe.
// Lo estático va unido en una sola malla; tapas, brillos y zonas se animan según
// el estado de la partida.
import {
  BoxGeometry,
  BufferAttribute,
  BufferGeometry,
  Color,
  CylinderGeometry,
  DoubleSide,
  DynamicDrawUsage,
  Euler,
  Group,
  IcosahedronGeometry,
  InstancedMesh,
  Matrix4,
  Mesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  Quaternion,
  Vector3,
} from 'three';
import { SHRINE_CONFIG } from '../data/run';
import { colored, mergeColored } from '../render/geometry';
import { PALETTE } from '../render/palette';
import { RENDER_ORDER } from '../render/renderOrder';
import { applyRetro } from '../render/retroMaterial';
import type { InteractableState } from '../systems/Interactables';
import type { Heightfield } from './Heightfield';
import type { InteractableSpot } from './interactables';

const RING_SEGMENTS = 48;
const RING_WIDTH = 0.35;
/** Segundos que tarda en abrirse la tapa de un baúl. */
const LID_OPEN_TIME = 0.45;
const LID_ANGLE = 1.9;
const LID_HINGE = new Vector3(0, 0.55, 0.38);

/** Matriz de colocación de un interactuable (posición y giro). */
function placement(spot: InteractableSpot): Matrix4 {
  return new Matrix4().compose(
    new Vector3(spot.x, spot.y - 0.04, spot.z),
    new Quaternion().setFromEuler(new Euler(0, spot.rotation, 0)),
    new Vector3(1, 1, 1),
  );
}

function chestBody(): BufferGeometry[] {
  const parts = [
    colored(new BoxGeometry(1.2, 0.55, 0.76).translate(0, 0.3, 0), PALETTE.chestWood),
    colored(new BoxGeometry(1.24, 0.06, 0.8).translate(0, 0.05, 0), PALETTE.chestDark),
    colored(new BoxGeometry(0.2, 0.2, 0.05).translate(0, 0.48, -0.4), PALETTE.coin),
  ];
  for (const x of [-0.38, 0.38]) parts.push(colored(new BoxGeometry(0.09, 0.57, 0.8).translate(x, 0.3, 0), PALETTE.chestBand));
  return parts;
}

/** Tapa abombada del baúl, con la bisagra en el origen. */
function chestLid(): BufferGeometry {
  const parts = [
    colored(new BoxGeometry(1.24, 0.1, 0.8).translate(0, 0.05, -0.38), PALETTE.chestWood),
    colored(new CylinderGeometry(0.38, 0.38, 1.24, 8, 1, false, 0, Math.PI).rotateZ(Math.PI / 2).rotateX(-Math.PI / 2).translate(0, 0.1, -0.38), PALETTE.chestDark),
  ];
  for (const x of [-0.38, 0.38]) {
    parts.push(colored(new CylinderGeometry(0.4, 0.4, 0.09, 8, 1, false, 0, Math.PI).rotateZ(Math.PI / 2).rotateX(-Math.PI / 2).translate(x, 0.1, -0.38), PALETTE.chestBand));
  }
  return mergeColored(parts);
}

function shrineBody(): BufferGeometry[] {
  const parts = [
    // Faldas de la mesa camilla hasta el suelo y el tablero encima.
    colored(new CylinderGeometry(0.9, 0.98, 0.7, 12).translate(0, 0.37, 0), PALETTE.shrineCloth),
    colored(new CylinderGeometry(0.99, 0.99, 0.1, 12).translate(0, 0.62, 0), PALETTE.shrineClothDark),
    colored(new CylinderGeometry(0.88, 0.88, 0.06, 12).translate(0, 0.74, 0), PALETTE.wood),
    // Tapete de ganchillo y un frutero con naranjas.
    colored(new CylinderGeometry(0.5, 0.5, 0.02, 12).translate(0, 0.78, 0), PALETTE.apron),
    colored(new CylinderGeometry(0.26, 0.16, 0.08, 8).translate(0, 0.83, 0), PALETTE.stoneLight),
  ];
  for (const [x, z] of [[-0.08, 0.02], [0.09, -0.05], [0.02, 0.1]] as const) {
    parts.push(colored(new IcosahedronGeometry(0.09, 0).translate(x, 0.92, z), PALETTE.leafOrange));
  }
  return parts;
}

function totemBody(): BufferGeometry[] {
  const parts = [colored(new CylinderGeometry(0.09, 0.12, 2.6, 6).translate(0, 1.3, 0), PALETTE.woodDark)];
  // Cacerolas ensartadas, cada una girada hacia un lado, con su mango.
  const pans: Array<[number, number, number]> = [
    [0.6, 0.38, 0],
    [1.15, 0.33, 1.7],
    [1.65, 0.3, 3.6],
    [2.1, 0.26, 5.1],
  ];
  const m = new Matrix4();
  for (const [y, r, rot] of pans) {
    m.makeRotationY(rot);
    parts.push(colored(new CylinderGeometry(r, r * 0.9, r * 0.75, 10).translate(0, y, 0).applyMatrix4(m), PALETTE.pan));
    parts.push(colored(new CylinderGeometry(r * 1.03, r * 1.03, 0.04, 10).translate(0, y + r * 0.37, 0).applyMatrix4(m), PALETTE.panDark));
    parts.push(colored(new BoxGeometry(0.08, 0.06, r + 0.35).translate(0, y + 0.1, r + 0.15).applyMatrix4(m), PALETTE.panHandle));
  }
  // Cucharón en lo alto
  parts.push(colored(new BoxGeometry(0.05, 0.6, 0.05).translate(0, 2.75, 0), PALETTE.panDark));
  parts.push(colored(new IcosahedronGeometry(0.13, 0).scale(1, 0.6, 1).translate(0, 3.05, 0), PALETTE.pan));
  return parts;
}

function portalBody(): BufferGeometry[] {
  const parts = [
    colored(new BoxGeometry(1.9, 2.55, 0.95).translate(0, 1.4, 0), PALETTE.wardrobe),
    colored(new BoxGeometry(2.05, 0.2, 1.05).translate(0, 2.76, 0), PALETTE.wardrobeDark),
    colored(new BoxGeometry(2.0, 0.12, 1.0).translate(0, 0.14, 0), PALETTE.wardrobeDark),
  ];
  for (const side of [-1, 1]) {
    // Puertas con cuarterones y pomos dorados.
    parts.push(colored(new BoxGeometry(0.84, 2.2, 0.05).translate(side * 0.46, 1.42, -0.49), PALETTE.chestWood));
    parts.push(colored(new BoxGeometry(0.6, 0.8, 0.03).translate(side * 0.46, 1.95, -0.52), PALETTE.wardrobe));
    parts.push(colored(new BoxGeometry(0.6, 0.8, 0.03).translate(side * 0.46, 0.9, -0.52), PALETTE.wardrobe));
    parts.push(colored(new IcosahedronGeometry(0.05, 0).translate(side * 0.1, 1.45, -0.54), PALETTE.coin));
    for (const x of [-0.8, 0.8]) parts.push(colored(new BoxGeometry(0.14, 0.12, 0.14).translate(x, 0.02, side * 0.35), PALETTE.wardrobeDark));
  }
  return parts;
}

/** Anillo en el suelo alrededor de (x, z), siguiendo el terreno (no indexado, por segmentos). */
function groundRing(hf: Heightfield, x: number, z: number, radius: number, width: number): BufferGeometry {
  const positions = new Float32Array(RING_SEGMENTS * 6 * 3);
  const point = (a: number, r: number, out: number[]): void => {
    const px = x + Math.cos(a) * r;
    const pz = z + Math.sin(a) * r;
    out.push(px, hf.heightAt(px, pz) + 0.12, pz);
  };
  const v: number[] = [];
  for (let k = 0; k < RING_SEGMENTS; k++) {
    const a0 = (k / RING_SEGMENTS) * Math.PI * 2;
    const a1 = ((k + 1) / RING_SEGMENTS) * Math.PI * 2;
    const inner0: number[] = [];
    const outer0: number[] = [];
    const inner1: number[] = [];
    const outer1: number[] = [];
    point(a0, radius - width, inner0);
    point(a0, radius, outer0);
    point(a1, radius - width, inner1);
    point(a1, radius, outer1);
    v.push(...inner0, ...outer0, ...inner1, ...inner1, ...outer0, ...outer1);
  }
  positions.set(v);
  const g = new BufferGeometry();
  g.setAttribute('position', new BufferAttribute(positions, 3));
  return g;
}

function ringMaterial(color: number, opacity: number): MeshBasicMaterial {
  return applyRetro(
    new MeshBasicMaterial({ color, transparent: true, opacity, depthWrite: false, side: DoubleSide, polygonOffset: true, polygonOffsetFactor: -3 }),
  );
}

export class InteractableMeshes {
  readonly group = new Group();
  private readonly lids: InstancedMesh;
  /** Brillos (brasero, tótem, rendija del armario) unidos en una malla con color por vértice. */
  private readonly glows: Mesh;
  /** Primer vértice y cuántos tiene cada brillo (mismo orden que `glowIndex`). */
  private readonly glowRanges: Array<[number, number]> = [];
  /** Posición en `spots` de cada baúl, brillo y mesa camilla (el estado de la partida va en el mismo orden). */
  private readonly chestIndex: number[] = [];
  private readonly glowIndex: number[] = [];
  private readonly shrineIndex: number[] = [];
  private readonly chestMatrices: Matrix4[] = [];
  /** Momento en que se abrió cada baúl (para animar la tapa). */
  private readonly openedAt: number[] = [];
  private readonly shrineRings: Mesh[] = [];
  private readonly shrineFills: Mesh[] = [];
  private readonly matrix = new Matrix4();
  private readonly hinge = new Matrix4();
  private readonly color = new Color();

  constructor(
    private readonly spots: readonly InteractableSpot[],
    hf: Heightfield,
  ) {
    const bodies: BufferGeometry[] = [];
    const glowParts: BufferGeometry[] = [];
    spots.forEach((spot, index) => {
      const m = placement(spot);
      const pieces =
        spot.kind === 'chest' ? chestBody() : spot.kind === 'shrine' ? shrineBody() : spot.kind === 'totem' ? totemBody() : portalBody();
      for (const piece of pieces) bodies.push(piece.applyMatrix4(m));
      if (spot.kind === 'chest') {
        this.chestIndex.push(index);
        this.chestMatrices.push(m);
        this.openedAt.push(Number.NEGATIVE_INFINITY);
      } else {
        this.glowIndex.push(index);
        glowParts.push(this.glowGeometry(spot));
      }
      if (spot.kind === 'shrine') {
        this.shrineIndex.push(index);
        const ring = new Mesh(groundRing(hf, spot.x, spot.z, SHRINE_CONFIG.radius, RING_WIDTH), ringMaterial(PALETTE.shrineRing, 0.35));
        const fill = new Mesh(groundRing(hf, spot.x, spot.z, SHRINE_CONFIG.radius - RING_WIDTH * 0.15, RING_WIDTH * 0.7), ringMaterial(PALETTE.shrineGlow, 0.85));
        for (const mesh of [ring, fill]) {
          mesh.renderOrder = RENDER_ORDER.telegraph;
          mesh.frustumCulled = false;
          this.group.add(mesh);
        }
        fill.geometry.setDrawRange(0, 0);
        this.shrineRings.push(ring);
        this.shrineFills.push(fill);
      }
    });
    const bodyMaterial = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }));
    if (bodies.length > 0) this.group.add(new Mesh(mergeColored(bodies), bodyMaterial));

    this.lids = new InstancedMesh(chestLid(), bodyMaterial, Math.max(1, this.chestIndex.length));
    this.lids.instanceMatrix.setUsage(DynamicDrawUsage);
    this.lids.count = this.chestIndex.length;
    this.lids.frustumCulled = false;
    this.group.add(this.lids);

    let offset = 0;
    for (const part of glowParts) {
      const count = part.getAttribute('position').count;
      this.glowRanges.push([offset, count]);
      offset += count;
    }
    const glowGeometry = glowParts.length > 0 ? mergeColored(glowParts) : colored(new BoxGeometry(0.01, 0.01, 0.01), 0x000000);
    this.glows = new Mesh(glowGeometry, applyRetro(new MeshBasicMaterial({ vertexColors: true })));
    this.glows.frustumCulled = false;
    this.group.add(this.glows);
    this.update(null, 0);
  }

  /** Geometría del brillo de cada tipo, ya colocada en el mundo. */
  private glowGeometry(spot: InteractableSpot): BufferGeometry {
    const m = placement(spot);
    switch (spot.kind) {
      case 'shrine':
        // La rendija de luz del brasero bajo las faldas.
        return colored(new CylinderGeometry(1.0, 1.0, 0.07, 12, 1, true).translate(0, 0.05, 0), 0xffffff).applyMatrix4(m);
      case 'totem':
        return colored(new IcosahedronGeometry(0.2, 0).translate(0, 3.3, 0), 0xffffff).applyMatrix4(m);
      default:
        // La luz que se escapa entre las puertas del armario.
        return colored(new BoxGeometry(0.07, 2.1, 0.04).translate(0, 1.42, -0.52), 0xffffff).applyMatrix4(m);
    }
  }

  /** Anima según el estado de la partida (`null`: sin partida, todo cerrado y apagado). */
  update(states: readonly InteractableState[] | null, time: number): void {
    const stateAt = (index: number): InteractableState | undefined => {
      const state = states?.[index];
      return state && state.spot === this.spots[index] ? state : undefined;
    };

    // Tapas de los baúles
    const array = this.lids.instanceMatrix.array as Float32Array;
    this.chestIndex.forEach((index, i) => {
      const state = stateAt(index);
      if (state?.used && !Number.isFinite(this.openedAt[i] as number)) this.openedAt[i] = time;
      if (!state?.used) this.openedAt[i] = Number.NEGATIVE_INFINITY;
      const t = Math.min(1, Math.max(0, (time - (this.openedAt[i] as number)) / LID_OPEN_TIME));
      const eased = 1 - (1 - t) * (1 - t);
      this.hinge.makeRotationX(LID_ANGLE * eased).setPosition(LID_HINGE);
      this.matrix.multiplyMatrices(this.chestMatrices[i] as Matrix4, this.hinge);
      this.matrix.toArray(array, i * 16);
    });
    this.lids.instanceMatrix.needsUpdate = true;

    // Brillos
    const colors = this.glows.geometry.getAttribute('color');
    const pulse = 0.75 + Math.sin(time * 4) * 0.25;
    this.glowIndex.forEach((index, k) => {
      const spot = this.spots[index] as InteractableSpot;
      const state = stateAt(index);
      const used = state?.used ?? false;
      if (spot.kind === 'shrine') {
        const charge = state?.charge ?? 0;
        this.color.setHex(used ? PALETTE.used : PALETTE.shrineGlow).multiplyScalar(used ? 0.6 : 0.55 + charge * 0.45 * pulse);
      } else if (spot.kind === 'totem') {
        this.color.setHex(used ? PALETTE.used : PALETTE.totemGlow).multiplyScalar(used ? 0.6 : pulse);
      } else {
        this.color.setHex(used ? PALETTE.used : PALETTE.portalGlow).multiplyScalar(used ? 0.5 : pulse);
      }
      const [start, count] = this.glowRanges[k] ?? [0, 0];
      for (let v = start; v < start + count; v++) colors.setXYZ(v, this.color.r, this.color.g, this.color.b);
    });
    colors.needsUpdate = true;

    // Zonas de carga de las mesas camilla
    this.shrineIndex.forEach((index, k) => {
      const state = stateAt(index);
      const ring = this.shrineRings[k];
      const fill = this.shrineFills[k];
      if (!ring || !fill) return;
      const used = state?.used ?? false;
      ring.visible = !used;
      fill.visible = !used;
      fill.geometry.setDrawRange(0, Math.round(RING_SEGMENTS * (state?.charge ?? 0)) * 6);
    });
  }
}
