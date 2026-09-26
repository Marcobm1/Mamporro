// Avisos de ataque en el suelo: franjas (embestidas de la rata, rodillo del jefe)
// y círculos (culetazo) que se van llenando mientras dura el aviso. Siguen la
// forma del terreno para no quedar enterrados en las cuestas.
import { BufferAttribute, BufferGeometry, DoubleSide, Group, Mesh, MeshBasicMaterial } from 'three';
import type { Heightfield } from '../world/Heightfield';
import { PALETTE } from './palette';
import { RENDER_ORDER } from './renderOrder';
import { applyRetro } from './retroMaterial';

const LINE_STEPS = 14;
const CIRCLE_RINGS = 4;
const CIRCLE_SEGMENTS = 28;
/** Altura sobre el terreno (m). */
const LIFT = 0.14;
const MAX_LINES = 6;
const MAX_CIRCLES = 3;

/** Una malla plana cuyos vértices se recolocan cada frame sobre el terreno. */
class Decal {
  readonly mesh: Mesh;
  readonly positions: Float32Array;
  private readonly attribute: BufferAttribute;

  constructor(vertices: number, indices: number[], material: MeshBasicMaterial) {
    this.positions = new Float32Array(vertices * 3);
    const geometry = new BufferGeometry();
    this.attribute = new BufferAttribute(this.positions, 3);
    geometry.setAttribute('position', this.attribute);
    geometry.setIndex(indices);
    this.mesh = new Mesh(geometry, material);
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.telegraph;
    this.mesh.visible = false;
  }

  set(k: number, x: number, y: number, z: number): void {
    this.positions[k * 3] = x;
    this.positions[k * 3 + 1] = y;
    this.positions[k * 3 + 2] = z;
  }

  commit(): void {
    this.attribute.needsUpdate = true;
    this.mesh.visible = true;
  }
}

function lineIndices(): number[] {
  const out: number[] = [];
  for (let s = 0; s < LINE_STEPS; s++) {
    const a = s * 2;
    out.push(a, a + 2, a + 1, a + 1, a + 2, a + 3);
  }
  return out;
}

function circleIndices(): number[] {
  const out: number[] = [];
  const v = (ring: number, k: number): number => 1 + ring * CIRCLE_SEGMENTS + (k % CIRCLE_SEGMENTS);
  for (let k = 0; k < CIRCLE_SEGMENTS; k++) out.push(0, v(0, k + 1), v(0, k));
  for (let r = 1; r < CIRCLE_RINGS; r++) {
    for (let k = 0; k < CIRCLE_SEGMENTS; k++) {
      out.push(v(r - 1, k), v(r - 1, k + 1), v(r, k), v(r - 1, k + 1), v(r, k + 1), v(r, k));
    }
  }
  return out;
}

function decalMaterial(color: number, opacity: number): MeshBasicMaterial {
  return applyRetro(
    new MeshBasicMaterial({
      color,
      transparent: true,
      opacity,
      depthWrite: false,
      side: DoubleSide,
      polygonOffset: true,
      polygonOffsetFactor: -3,
    }),
  );
}

/** Pareja fondo + relleno de un aviso. */
interface Telegraph {
  back: Decal;
  fill: Decal;
}

export class TelegraphRenderer {
  readonly group = new Group();
  private readonly lines: Telegraph[] = [];
  private readonly circles: Telegraph[] = [];
  private usedLines = 0;
  private usedCircles = 0;

  constructor() {
    const back = decalMaterial(PALETTE.telegraph, 0.22);
    const fill = decalMaterial(PALETTE.telegraphFill, 0.42);
    const lineIdx = lineIndices();
    const circleIdx = circleIndices();
    const lineVerts = (LINE_STEPS + 1) * 2;
    const circleVerts = 1 + CIRCLE_RINGS * CIRCLE_SEGMENTS;
    for (let i = 0; i < MAX_LINES; i++) this.lines.push(this.pair(lineVerts, lineIdx, back, fill));
    for (let i = 0; i < MAX_CIRCLES; i++) this.circles.push(this.pair(circleVerts, circleIdx, back, fill));
  }

  private pair(vertices: number, indices: number[], back: MeshBasicMaterial, fill: MeshBasicMaterial): Telegraph {
    const t = { back: new Decal(vertices, indices, back), fill: new Decal(vertices, indices, fill) };
    this.group.add(t.back.mesh, t.fill.mesh);
    return t;
  }

  /** Empieza un frame: los avisos que no se vuelvan a pedir se ocultan en `end`. */
  begin(): void {
    this.usedLines = 0;
    this.usedCircles = 0;
  }

  /** Franja desde (x, z) en la dirección dada; el relleno avanza con `progress` (0..1). */
  line(hf: Heightfield, x: number, z: number, dirX: number, dirZ: number, length: number, width: number, progress: number): void {
    const t = this.lines[this.usedLines];
    if (!t) return;
    this.usedLines++;
    this.writeLine(t.back, hf, x, z, dirX, dirZ, length, width);
    this.writeLine(t.fill, hf, x, z, dirX, dirZ, length * Math.max(0.02, Math.min(1, progress)), width * 0.8);
  }

  /** Círculo de radio `radius`; el relleno crece desde el centro con `progress`. */
  circle(hf: Heightfield, x: number, z: number, radius: number, progress: number): void {
    const t = this.circles[this.usedCircles];
    if (!t) return;
    this.usedCircles++;
    this.writeCircle(t.back, hf, x, z, radius);
    this.writeCircle(t.fill, hf, x, z, radius * Math.max(0.02, Math.min(1, progress)));
  }

  end(): void {
    for (let i = this.usedLines; i < this.lines.length; i++) this.hide(this.lines[i]);
    for (let i = this.usedCircles; i < this.circles.length; i++) this.hide(this.circles[i]);
  }

  clear(): void {
    this.begin();
    this.end();
  }

  private hide(t: Telegraph | undefined): void {
    if (!t) return;
    t.back.mesh.visible = false;
    t.fill.mesh.visible = false;
  }

  private writeLine(d: Decal, hf: Heightfield, x: number, z: number, dirX: number, dirZ: number, length: number, width: number): void {
    const px = -dirZ * width * 0.5;
    const pz = dirX * width * 0.5;
    for (let s = 0; s <= LINE_STEPS; s++) {
      const along = (s / LINE_STEPS) * length;
      const cx = x + dirX * along;
      const cz = z + dirZ * along;
      d.set(s * 2, cx + px, hf.heightAt(cx + px, cz + pz) + LIFT, cz + pz);
      d.set(s * 2 + 1, cx - px, hf.heightAt(cx - px, cz - pz) + LIFT, cz - pz);
    }
    d.commit();
  }

  private writeCircle(d: Decal, hf: Heightfield, x: number, z: number, radius: number): void {
    d.set(0, x, hf.heightAt(x, z) + LIFT, z);
    for (let r = 0; r < CIRCLE_RINGS; r++) {
      const rr = (radius * (r + 1)) / CIRCLE_RINGS;
      for (let k = 0; k < CIRCLE_SEGMENTS; k++) {
        const a = (k / CIRCLE_SEGMENTS) * Math.PI * 2;
        const vx = x + Math.cos(a) * rr;
        const vz = z + Math.sin(a) * rr;
        d.set(1 + r * CIRCLE_SEGMENTS + k, vx, hf.heightAt(vx, vz) + LIFT, vz);
      }
    }
    d.commit();
  }
}
