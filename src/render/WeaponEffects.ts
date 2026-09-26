// Efectos visuales de las armas del hito 3: barrazos de pan, dentaduras en
// órbita, calambres en zigzag y charcos recién fregados.
import {
  BoxGeometry,
  BufferAttribute,
  BufferGeometry,
  CircleGeometry,
  Color,
  DynamicDrawUsage,
  Group,
  InstancedMesh,
  LineBasicMaterial,
  LineSegments,
  Mesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  RingGeometry,
} from 'three';
import type { Rng } from '../core/rng';
import type { OrbitState, TrailState } from '../weapons/types';
import { writeYawMatrix } from './EnemyRenderer';
import { colored, mergeColored } from './geometry';
import { PALETTE } from './palette';
import { RENDER_ORDER } from './renderOrder';
import { applyRetro } from './retroMaterial';

// ------------------------------------------------------------------ barrazos

const SWING_LIFE = 0.22;
const MAX_SWINGS = 16;

interface Swing {
  mesh: Mesh;
  material: MeshBasicMaterial;
  age: number;
  radius: number;
}

/** Abanicos que se abren y se desvanecen en cada barrazo. */
export class ArcRenderer {
  readonly group = new Group();
  private readonly swings: Swing[] = [];
  private next = 0;
  private readonly geometries = new Map<number, BufferGeometry>();

  constructor() {
    for (let i = 0; i < MAX_SWINGS; i++) {
      const material = new MeshBasicMaterial({ color: PALETTE.bread, transparent: true, depthWrite: false });
      const mesh = new Mesh(undefined, applyRetro(material));
      mesh.visible = false;
      mesh.renderOrder = RENDER_ORDER.aura;
      this.group.add(mesh);
      this.swings.push({ mesh, material, age: SWING_LIFE, radius: 1 });
    }
  }

  /** Abanico de radio 1 centrado hacia -z (lo que mira un objeto con rotation.y = 0). */
  private geometryFor(halfAngle: number): BufferGeometry {
    const key = Math.round(halfAngle * 1000);
    let g = this.geometries.get(key);
    if (!g) {
      g = new RingGeometry(0.35, 1, 18, 1, Math.PI / 2 - halfAngle, halfAngle * 2).rotateX(-Math.PI / 2);
      this.geometries.set(key, g);
    }
    return g;
  }

  spawn(x: number, y: number, z: number, angle: number, radius: number, halfAngle: number): void {
    const swing = this.swings[this.next] as Swing;
    this.next = (this.next + 1) % MAX_SWINGS;
    swing.mesh.geometry = this.geometryFor(halfAngle);
    swing.mesh.position.set(x, y, z);
    swing.mesh.rotation.y = angle;
    swing.radius = radius;
    swing.age = 0;
    swing.mesh.visible = true;
  }

  update(dt: number): void {
    for (const s of this.swings) {
      if (!s.mesh.visible) continue;
      s.age += dt;
      if (s.age >= SWING_LIFE) {
        s.mesh.visible = false;
        continue;
      }
      const t = s.age / SWING_LIFE;
      s.mesh.scale.setScalar(s.radius * (0.75 + 0.25 * t));
      s.material.opacity = 0.75 * (1 - t);
    }
  }

  clear(): void {
    for (const s of this.swings) s.mesh.visible = false;
  }
}

// ------------------------------------------------------------------ dentaduras

const MAX_ORBS = 16;

/** Dentadura postiza: encía rosa con dos filas de dientes. */
function dentureGeometry(): BufferGeometry {
  const parts: BufferGeometry[] = [];
  for (const jaw of [1, -1]) {
    const y = jaw * 0.13;
    parts.push(colored(new BoxGeometry(0.62, 0.12, 0.36).translate(0, y + jaw * 0.08, -0.04), PALETTE.gums));
    for (let k = 0; k < 5; k++) {
      const x = (k - 2) * 0.12;
      const back = Math.abs(k - 2) * 0.05;
      parts.push(colored(new BoxGeometry(0.1, 0.13, 0.1).translate(x, y, 0.1 - back), PALETTE.teeth));
    }
  }
  return mergeColored(parts);
}

/** Dentaduras girando alrededor del jugador y castañeteando. */
export class OrbitRenderer {
  readonly mesh: InstancedMesh;

  constructor() {
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }));
    this.mesh = new InstancedMesh(dentureGeometry(), material, MAX_ORBS);
    this.mesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.mesh.count = 0;
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.afterPlayer;
  }

  /**
   * `pop` (0..1) hace que aparezcan y desaparezcan encogiéndose; `orbit` y `orb`
   * son los radios de la órbita y de cada dentadura.
   */
  update(state: OrbitState | null, count: number, x: number, y: number, z: number, orbit: number, orb: number, pop: number, time: number): void {
    if (!state || !state.active || pop <= 0) {
      this.mesh.count = 0;
      return;
    }
    const n = Math.min(MAX_ORBS, Math.max(1, count));
    const array = this.mesh.instanceMatrix.array as Float32Array;
    const size = orb * 1.8 * pop;
    for (let k = 0; k < n; k++) {
      const a = state.angle + (k * Math.PI * 2) / n;
      const ox = x + Math.cos(a) * orbit;
      const oz = z + Math.sin(a) * orbit;
      // Enseñan los dientes hacia fuera (su +z apunta lejos del jugador) y castañetean.
      const yaw = Math.PI / 2 - a;
      const chatter = 1 + Math.sin(time * 26 + k * 1.7) * 0.18;
      writeYawMatrix(array, k, ox, y + 0.85, oz, yaw, size, size * chatter, size);
    }
    this.mesh.count = n;
    this.mesh.instanceMatrix.needsUpdate = true;
  }
}

// ------------------------------------------------------------------ calambres

const ZAP_LIFE = 0.16;
const MAX_ZAPS = 24;
const MAX_ZAP_POINTS = 64;
const ZAP_SUBDIV = 6;
/** Temblor de los puntos intermedios del zigzag (m). */
const ZAP_JITTER = 0.45;
/** Dos hebras por rayo (azulada y amarilla), cada una con su propio zigzag. */
const ZAP_STRANDS = 2;
const MAX_LINE_VERTICES = MAX_ZAPS * MAX_ZAP_POINTS * ZAP_SUBDIV * 2 * ZAP_STRANDS;

interface Zap {
  points: Float32Array;
  count: number;
  age: number;
}

/** Rayos en zigzag entre los enemigos alcanzados; el zigzag cambia en cada frame. */
export class ChainRenderer {
  readonly lines: LineSegments;
  private readonly zaps: Zap[] = [];
  private readonly positions: Float32Array;
  private readonly colors: Float32Array;
  private readonly geometry: BufferGeometry;
  private readonly strandColors: ReadonlyArray<readonly [number, number, number]>;

  constructor(private readonly rng: Rng) {
    this.positions = new Float32Array(MAX_LINE_VERTICES * 3);
    this.colors = new Float32Array(MAX_LINE_VERTICES * 3);
    this.geometry = new BufferGeometry();
    const attr = new BufferAttribute(this.positions, 3);
    attr.setUsage(DynamicDrawUsage);
    const colorAttr = new BufferAttribute(this.colors, 3);
    colorAttr.setUsage(DynamicDrawUsage);
    this.geometry.setAttribute('position', attr);
    this.geometry.setAttribute('color', colorAttr);
    this.geometry.setDrawRange(0, 0);
    const rgb = (hex: number): readonly [number, number, number] => {
      const c = new Color(hex);
      return [c.r, c.g, c.b];
    };
    this.strandColors = [rgb(PALETTE.zap), rgb(PALETTE.zapCore)];
    this.lines = new LineSegments(this.geometry, applyRetro(new LineBasicMaterial({ vertexColors: true })));
    this.lines.frustumCulled = false;
    this.lines.renderOrder = RENDER_ORDER.afterPlayer;
    for (let i = 0; i < MAX_ZAPS; i++) this.zaps.push({ points: new Float32Array(MAX_ZAP_POINTS * 3), count: 0, age: ZAP_LIFE });
  }

  spawn(points: Float32Array, count: number): void {
    // Reutiliza el más viejo.
    let zap = this.zaps[0] as Zap;
    for (const z of this.zaps) if (z.age > zap.age) zap = z;
    zap.count = Math.min(count, MAX_ZAP_POINTS);
    zap.points.set(points.subarray(0, zap.count * 3));
    zap.age = 0;
  }

  update(dt: number): void {
    const out = this.positions;
    const col = this.colors;
    let v = 0;
    for (const zap of this.zaps) {
      if (zap.age >= ZAP_LIFE) continue;
      zap.age += dt;
      const p = zap.points;
      for (let strand = 0; strand < ZAP_STRANDS; strand++) {
        const [r, g, b] = this.strandColors[strand] ?? [1, 1, 1];
        const jitter = strand === 0 ? ZAP_JITTER : ZAP_JITTER * 0.6;
        for (let i = 0; i + 1 < zap.count; i++) {
          const ax = p[i * 3] as number;
          const ay = p[i * 3 + 1] as number;
          const az = p[i * 3 + 2] as number;
          const bx = p[i * 3 + 3] as number;
          const by = p[i * 3 + 4] as number;
          const bz = p[i * 3 + 5] as number;
          let px = ax;
          let py = ay;
          let pz = az;
          // Puntos intermedios repartidos por el tramo con un temblor al azar; el
          // último cae justo en el enemigo.
          for (let s = 1; s <= ZAP_SUBDIV && v + 2 <= MAX_LINE_VERTICES; s++) {
            const last = s === ZAP_SUBDIV;
            const t = s / ZAP_SUBDIV;
            const nx = last ? bx : ax + (bx - ax) * t + this.rng.range(-jitter, jitter);
            const ny = last ? by : ay + (by - ay) * t + this.rng.range(-jitter, jitter);
            const nz = last ? bz : az + (bz - az) * t + this.rng.range(-jitter, jitter);
            out[v * 3] = px;
            out[v * 3 + 1] = py;
            out[v * 3 + 2] = pz;
            out[v * 3 + 3] = nx;
            out[v * 3 + 4] = ny;
            out[v * 3 + 5] = nz;
            for (let k = 0; k < 2; k++) {
              col[(v + k) * 3] = r;
              col[(v + k) * 3 + 1] = g;
              col[(v + k) * 3 + 2] = b;
            }
            v += 2;
            px = nx;
            py = ny;
            pz = nz;
          }
        }
      }
    }
    this.geometry.setDrawRange(0, v);
    (this.geometry.getAttribute('position') as BufferAttribute).needsUpdate = true;
    (this.geometry.getAttribute('color') as BufferAttribute).needsUpdate = true;
  }

  clear(): void {
    for (const z of this.zaps) z.age = ZAP_LIFE;
    this.geometry.setDrawRange(0, 0);
  }
}

// ------------------------------------------------------------------ charcos

const MAX_PUDDLE_INSTANCES = 128;

/** Charcos del suelo recién fregado: disco azulado con un brillo, que se encoge al secarse. */
export class TrailRenderer {
  readonly group = new Group();
  private readonly water: InstancedMesh;
  private readonly shine: InstancedMesh;

  constructor() {
    const flat = (m: MeshBasicMaterial): MeshBasicMaterial => {
      m.transparent = true;
      m.depthWrite = false;
      m.polygonOffset = true;
      m.polygonOffsetFactor = -3;
      return applyRetro(m);
    };
    this.water = new InstancedMesh(
      new CircleGeometry(1, 14).rotateX(-Math.PI / 2),
      flat(new MeshBasicMaterial({ color: PALETTE.puddle, opacity: 0.5 })),
      MAX_PUDDLE_INSTANCES,
    );
    this.shine = new InstancedMesh(
      new RingGeometry(0.55, 0.7, 10, 1, 0.3, 1.6).rotateX(-Math.PI / 2),
      flat(new MeshBasicMaterial({ color: PALETTE.puddleShine, opacity: 0.7 })),
      MAX_PUDDLE_INSTANCES,
    );
    for (const m of [this.water, this.shine]) {
      m.instanceMatrix.setUsage(DynamicDrawUsage);
      m.count = 0;
      m.frustumCulled = false;
      m.renderOrder = RENDER_ORDER.blobShadow;
    }
    this.group.add(this.water, this.shine);
  }

  update(state: TrailState | null): void {
    if (!state) {
      this.water.count = 0;
      this.shine.count = 0;
      return;
    }
    const n = Math.min(state.count, MAX_PUDDLE_INSTANCES);
    const water = this.water.instanceMatrix.array as Float32Array;
    const shine = this.shine.instanceMatrix.array as Float32Array;
    for (let i = 0; i < n; i++) {
      const age = state.age[i] as number;
      const left = (state.life[i] as number) - age;
      // Aparece de golpe y se seca encogiéndose al final.
      const grow = Math.min(1, age / 0.12);
      const dry = Math.min(1, left / 0.5);
      const r = (state.radius[i] as number) * grow * dry;
      const x = state.x[i] as number;
      const y = (state.y[i] as number) + 0.05;
      const z = state.z[i] as number;
      writeYawMatrix(water, i, x, y, z, i * 1.3, r, 1, r);
      writeYawMatrix(shine, i, x, y + 0.01, z, i * 1.3, r, 1, r);
    }
    this.water.count = n;
    this.shine.count = n;
    this.water.instanceMatrix.needsUpdate = true;
    this.shine.instanceMatrix.needsUpdate = true;
  }
}
