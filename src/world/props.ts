// Construcciones y objetos del mapa (casas derruidas, templetes, granjas, pozos,
// troncos, setas, carteles...). Se describen como piezas simples con color y,
// cuando hace falta, colisionador. Lógica pura: el mesh se construye aparte.
import { createNoise2D } from 'simplex-noise';
import { clamp, DEG2RAD, lerp, type Vec3Like } from '../core/math';
import type { Rng } from '../core/rng';
import { PALETTE } from '../render/palette';
import { boxCollider, circleCollider, type Collider } from './colliders';
import { squircle, type Heightfield } from './Heightfield';
import { isInAnySite, type Site } from './sites';

export type PartShape = 'box' | 'cylinder' | 'cone' | 'ico' | 'dodeca';
/** "stone" usa la textura de sillería; "plain" la de detalle. */
export type PartMaterial = 'stone' | 'plain';

export interface Part {
  shape: PartShape;
  material: PartMaterial;
  color: number;
  /** box: ancho/alto/fondo · cylinder: radio abajo/alto/radio arriba · cone: radio/alto/- · ico/dodeca: radio/escala Y/- */
  size: [number, number, number];
  /** Centro de la pieza en el mundo. */
  position: [number, number, number];
  /** Rotación (orden YXZ: primero la inclinación local, después el giro). */
  rotation: [number, number, number];
  segments: number;
}

export interface PropSet {
  parts: Part[];
  colliders: Collider[];
}

interface PartOptions {
  material?: PartMaterial;
  rotX?: number;
  rotY?: number;
  rotZ?: number;
  /** Añade colisionador (solo si la pieza no está inclinada). */
  collide?: boolean;
  standable?: boolean;
  segments?: number;
}

const STONES = [PALETTE.stoneLight, PALETTE.stone, PALETTE.stoneDark] as const;

/** Coloca piezas en el espacio local de un sitio (suelo en y = 0, fachada hacia -Z). */
export class PropBuilder {
  private cos = 1;
  private sin = 0;
  private ox = 0;
  private oy = 0;
  private oz = 0;
  private rot = 0;

  constructor(private readonly out: PropSet) {}

  /** Cambia el origen (posición y giro) de las piezas siguientes. */
  at(x: number, y: number, z: number, rotation: number): this {
    this.ox = x;
    this.oy = y;
    this.oz = z;
    this.rot = rotation;
    this.cos = Math.cos(rotation);
    this.sin = Math.sin(rotation);
    return this;
  }

  private world(lx: number, lz: number): [number, number] {
    return [this.ox + lx * this.cos + lz * this.sin, this.oz - lx * this.sin + lz * this.cos];
  }

  private add(shape: PartShape, lx: number, ly: number, lz: number, size: Part['size'], color: number, o: PartOptions): Part {
    const [wx, wz] = this.world(lx, lz);
    const rotY = this.rot + (o.rotY ?? 0);
    const part: Part = {
      shape,
      material: o.material ?? 'plain',
      color,
      size,
      position: [wx, this.oy + ly, wz],
      rotation: [o.rotX ?? 0, rotY, o.rotZ ?? 0],
      segments: o.segments ?? 7,
    };
    this.out.parts.push(part);
    return part;
  }

  /** Caja con su centro en (lx, ly, lz). */
  box(lx: number, ly: number, lz: number, sx: number, sy: number, sz: number, color: number, o: PartOptions = {}): void {
    const p = this.add('box', lx, ly, lz, [sx, sy, sz], color, o);
    if (o.collide && !o.rotX && !o.rotZ) {
      this.out.colliders.push(
        boxCollider(p.position[0], p.position[2], sx / 2, sz / 2, p.rotation[1], p.position[1] - sy / 2, p.position[1] + sy / 2, o.standable ?? true),
      );
    }
  }

  /** Cilindro vertical con su centro en (lx, ly, lz). */
  cylinder(
    lx: number,
    ly: number,
    lz: number,
    radius: number,
    height: number,
    color: number,
    o: PartOptions & { radiusTop?: number } = {},
  ): void {
    const p = this.add('cylinder', lx, ly, lz, [radius, height, o.radiusTop ?? radius], color, o);
    if (o.collide && !o.rotX && !o.rotZ) {
      this.out.colliders.push(
        circleCollider(p.position[0], p.position[2], Math.max(radius, o.radiusTop ?? radius), p.position[1] - height / 2, p.position[1] + height / 2, o.standable ?? true),
      );
    }
  }

  /** Cilindro tumbado a lo largo del eje X local (troncos, columnas caídas...). */
  lyingCylinder(lx: number, ly: number, lz: number, radius: number, length: number, color: number, o: PartOptions = {}): void {
    const p = this.add('cylinder', lx, ly, lz, [radius, length, radius], color, { ...o, rotZ: Math.PI / 2, collide: false });
    if (o.collide) {
      this.out.colliders.push(
        boxCollider(p.position[0], p.position[2], length / 2, radius * 0.9, p.rotation[1], p.position[1] - radius, p.position[1] + radius, o.standable ?? true),
      );
    }
  }

  /** Solo colisión, sin pieza visible (p. ej. el volumen de una valla o de un carro). */
  solidBox(lx: number, ly: number, lz: number, sx: number, sy: number, sz: number, o: { rotY?: number; standable?: boolean } = {}): void {
    const [wx, wz] = this.world(lx, lz);
    this.out.colliders.push(
      boxCollider(wx, wz, sx / 2, sz / 2, this.rot + (o.rotY ?? 0), this.oy + ly - sy / 2, this.oy + ly + sy / 2, o.standable ?? true),
    );
  }

  cone(lx: number, ly: number, lz: number, radius: number, height: number, color: number, o: PartOptions = {}): void {
    this.add('cone', lx, ly, lz, [radius, height, 0], color, o);
  }

  blob(lx: number, ly: number, lz: number, radius: number, color: number, o: PartOptions & { shape?: 'ico' | 'dodeca'; scaleY?: number } = {}): void {
    this.add(o.shape ?? 'dodeca', lx, ly, lz, [radius, o.scaleY ?? 1, 0], color, o);
  }
}

// ------------------------------------------------------------------ piezas

function stone(rng: Rng): number {
  return rng.pick(STONES);
}

/**
 * Muro en ruinas de (x0, z0) a (x1, z1): columnas de ~1 m con alturas irregulares,
 * tramos derrumbados, ventanas y un hueco de puerta opcional.
 */
function ruinedWall(
  b: PropBuilder,
  rng: Rng,
  x0: number,
  z0: number,
  x1: number,
  z1: number,
  height: number,
  options: { door?: boolean; thickness?: number; windows?: boolean } = {},
): void {
  const thickness = options.thickness ?? 0.45;
  const dx = x1 - x0;
  const dz = z1 - z0;
  const length = Math.hypot(dx, dz);
  const n = Math.max(2, Math.round(length / 0.95));
  const colW = length / n;
  const rotY = Math.atan2(-dz / length, dx / length);
  for (let k = 0; k < n; k++) {
    const t = (k + 0.5) / n;
    const edge = 1 - 2 * Math.min(t, 1 - t); // 1 en las esquinas, 0 en el centro
    let h = lerp(0.4, 1, Math.pow(edge, 0.7)) + rng.range(-0.18, 0.18);
    const collapsed = rng.chance(0.2) && edge < 0.7;
    const window = rng.chance(0.35);
    const shade = stone(rng);
    if (options.door && Math.abs(t - 0.5) * length < 0.75) continue;
    if (collapsed) h *= 0.3;
    h = clamp(h, 0.25, 1) * height;
    const cx = x0 + dx * t;
    const cz = z0 + dz * t;
    if (options.windows !== false && window && h > 2.3 && edge < 0.6) {
      b.box(cx, 0.45, cz, colW + 0.02, 0.9, thickness, shade, { material: 'stone', rotY, collide: true });
      const top = h - 1.8;
      b.box(cx, 1.8 + top / 2, cz, colW + 0.02, top, thickness, shade, { material: 'stone', rotY, collide: true });
    } else {
      b.box(cx, h / 2, cz, colW + 0.02, h, thickness, shade, { material: 'stone', rotY, collide: true });
    }
  }
}

function rubble(b: PropBuilder, rng: Rng, count: number, spreadX: number, spreadZ: number, cx = 0, cz = 0): void {
  for (let i = 0; i < count; i++) {
    const r = rng.range(0.12, 0.32);
    b.blob(cx + rng.range(-spreadX, spreadX), r * 0.5, cz + rng.range(-spreadZ, spreadZ), r, stone(rng), { scaleY: 0.7 });
  }
}

function barrel(b: PropBuilder, x: number, z: number, y = 0): void {
  b.cylinder(x, y + 0.475, z, 0.38, 0.95, PALETTE.wood, { radiusTop: 0.34, collide: true });
  b.cylinder(x, y + 0.25, z, 0.395, 0.06, PALETTE.woodDark);
  b.cylinder(x, y + 0.72, z, 0.37, 0.06, PALETTE.woodDark);
}

function crate(b: PropBuilder, rng: Rng, x: number, z: number, y = 0, size = 0.8): void {
  b.box(x, y + size / 2, z, size, size, size, PALETTE.woodLight, { rotY: rng.range(-0.4, 0.4), collide: true });
}

// ------------------------------------------------------------------ sitios

function buildHouse(b: PropBuilder, rng: Rng): void {
  const w = rng.range(6.2, 8);
  const d = rng.range(4.6, 6);
  const t = 0.45;
  const h = rng.range(2.7, 3.3);
  // Fachada (con puerta) y trasera a lo ancho; laterales entre ellas.
  ruinedWall(b, rng, -w / 2 - t / 2, -d / 2, w / 2 + t / 2, -d / 2, h, { door: true });
  ruinedWall(b, rng, w / 2 + t / 2, d / 2, -w / 2 - t / 2, d / 2, h);
  ruinedWall(b, rng, w / 2, -d / 2 + t / 2, w / 2, d / 2 - t / 2, h);
  ruinedWall(b, rng, -w / 2, d / 2 - t / 2, -w / 2, -d / 2 + t / 2, h);

  // Chimenea en una esquina trasera
  const side = rng.chance(0.5) ? 1 : -1;
  b.box(side * (w / 2 - 0.35), (h + 1.3) / 2, d / 2 - 0.35, 0.9, h + 1.3, 0.9, PALETTE.stoneDark, { material: 'stone', collide: true });

  // Tejado hundido apoyado en la pared trasera y vigas caídas
  b.box(rng.range(-0.8, 0.8), h * 0.42, d * 0.12, w * 0.55, 0.12, d * 0.7, PALETTE.roof, { rotX: -0.62, rotY: rng.range(-0.2, 0.2) });
  b.box(rng.range(-1, 1), 0.09, rng.range(-0.8, 0.8), 0.16, 0.16, w * 0.85, PALETTE.woodDark, { rotY: Math.PI / 2 + rng.range(-0.5, 0.5) });
  b.box(-side * rng.range(0.5, 1.5), h * 0.35, 0, 0.16, 0.16, d * 0.9, PALETTE.woodDark, { rotX: 0.5, rotY: rng.range(-0.3, 0.3) });

  // Mesa coja (le falta una pata, así que está inclinada)
  const tx = rng.range(-w / 4, w / 4);
  const tz = rng.range(-d / 5, d / 6);
  b.box(tx, 0.55, tz, 1.2, 0.08, 0.7, PALETTE.wood, { rotZ: 0.35 });
  b.box(tx + 0.5, 0.35, tz, 0.08, 0.7, 0.08, PALETTE.woodDark);

  // La naturaleza reclama la casa: hiedra en los muros y un arbusto dentro
  for (let i = 0; i < 3; i++) {
    const onFront = rng.chance(0.5);
    const sign = rng.pick([-1, 1]);
    const inset = rng.range(0.35, 0.8);
    const ix = onFront ? sign * (w / 2 - inset) : sign * (w / 2 + t / 2 + 0.02);
    const iz = onFront ? -d / 2 - t / 2 - 0.02 : rng.pick([-1, 1]) * (d / 2 - inset);
    b.box(ix, rng.range(0.6, 1.2), iz, onFront ? 0.9 : 0.04, rng.range(0.7, 1.3), onFront ? 0.04 : 0.9, PALETTE.ivy);
  }
  b.blob(rng.range(-w / 4, w / 4), 0.35, rng.range(0, d / 4), 0.55, PALETTE.bush, { shape: 'ico', scaleY: 0.75 });
  rubble(b, rng, 9, w / 2 + 0.8, d / 2 + 0.8);

  // Un barril o una caja olvidados en la entrada
  if (rng.chance(0.6)) barrel(b, rng.range(-w / 2, -1), -d / 2 - 1.1, 0);
  else crate(b, rng, rng.range(1, w / 2), -d / 2 - 1.1);
}

function buildTemple(b: PropBuilder, rng: Rng): void {
  const r = rng.range(4.4, 5.2);
  const platform = 0.45;
  b.cylinder(0, platform / 2, 0, r + 1.2, platform, PALETTE.stoneLight, { material: 'stone', collide: true, segments: 12 });

  const n = 8;
  const intact: boolean[] = [];
  const angle0 = rng.next() * Math.PI;
  for (let k = 0; k < n; k++) {
    const a = angle0 + (k / n) * Math.PI * 2;
    const x = Math.cos(a) * r;
    const z = Math.sin(a) * r;
    const missing = rng.chance(0.22);
    const broken = rng.chance(0.45);
    const h = broken ? rng.range(0.8, 2.3) : 4.2;
    intact.push(!missing && !broken);
    if (missing) {
      rubble(b, rng, 3, 0.5, 0.5, x, z);
      continue;
    }
    b.box(x, platform + 0.12, z, 0.85, 0.25, 0.85, PALETTE.stone, { material: 'stone', rotY: -a });
    b.cylinder(x, platform + h / 2, z, 0.34, h, PALETTE.stoneLight, { material: 'stone', collide: true, standable: broken });
    if (!broken) b.box(x, platform + h + 0.15, z, 0.9, 0.3, 0.9, PALETTE.stone, { material: 'stone', rotY: -a });
  }
  // Dinteles entre columnas intactas contiguas
  for (let k = 0; k < n; k++) {
    const next = (k + 1) % n;
    if (!intact[k] || !intact[next]) continue;
    const a0 = angle0 + (k / n) * Math.PI * 2;
    const a1 = angle0 + (next / n) * Math.PI * 2;
    const x0 = Math.cos(a0) * r;
    const z0 = Math.sin(a0) * r;
    const x1 = Math.cos(a1) * r;
    const z1 = Math.sin(a1) * r;
    const len = Math.hypot(x1 - x0, z1 - z0) + 0.8;
    b.box((x0 + x1) / 2, platform + 4.55, (z0 + z1) / 2, len, 0.45, 0.8, PALETTE.stone, {
      material: 'stone',
      rotY: Math.atan2(-(z1 - z0), x1 - x0),
      collide: true,
    });
  }
  // Estatua rota en el centro: solo quedan las piernas
  b.box(0, platform + 0.6, 0, 1.1, 1.2, 1.1, PALETTE.stone, { material: 'stone', collide: true });
  b.box(-0.18, platform + 1.65, 0, 0.24, 0.9, 0.3, PALETTE.marble);
  b.box(0.18, platform + 1.65, 0, 0.24, 0.9, 0.3, PALETTE.marble);
  b.box(0, platform + 2.2, 0, 0.6, 0.25, 0.34, PALETTE.marble, { rotZ: 0.1 });
  // Columna caída fuera de la plataforma, partida en dos a lo largo de su eje
  const fa = rng.next() * Math.PI * 2;
  const axis = fa + Math.PI / 2;
  const fx = Math.cos(fa) * (r + 3);
  const fz = Math.sin(fa) * (r + 3);
  b.lyingCylinder(fx, 0.34, fz, 0.34, 2.4, PALETTE.stoneLight, { material: 'stone', rotY: axis, collide: true });
  b.lyingCylinder(fx + Math.cos(axis) * 2.2, 0.34, fz - Math.sin(axis) * 2.2, 0.34, 1.3, PALETTE.stoneLight, {
    material: 'stone',
    rotY: axis + 0.35,
    collide: true,
  });
  rubble(b, rng, 6, r + 2, r + 2);
}

function buildFarm(b: PropBuilder, rng: Rng): void {
  const size = rng.range(8, 9.5);
  const half = size / 2;
  const posts = Math.round(size / 1.6);
  const step = size / posts;
  // Valla con postes torcidos, tramos rotos y una entrada en la fachada
  const sides: Array<[number, number, number, number]> = [
    [-half, -half, half, -half],
    [half, -half, half, half],
    [half, half, -half, half],
    [-half, half, -half, -half],
  ];
  sides.forEach(([x0, z0, x1, z1], sideIndex) => {
    const dirX = (x1 - x0) / size;
    const dirZ = (z1 - z0) / size;
    const rotY = Math.atan2(-dirZ, dirX);
    for (let k = 0; k < posts; k++) {
      const px = x0 + dirX * step * k;
      const pz = z0 + dirZ * step * k;
      b.box(px, 0.55, pz, 0.14, 1.1, 0.14, PALETTE.woodDark, { rotX: rng.range(-0.12, 0.12), rotZ: rng.range(-0.12, 0.12) });
      const gate = sideIndex === 0 && k === Math.floor(posts / 2);
      if (gate || rng.chance(0.18)) continue;
      const mx = px + (dirX * step) / 2;
      const mz = pz + (dirZ * step) / 2;
      b.box(mx, 0.45, mz, step, 0.1, 0.06, PALETTE.wood, { rotY });
      b.box(mx, 0.85, mz, step, 0.1, 0.06, PALETTE.wood, { rotY, rotX: rng.chance(0.2) ? 0.25 : 0 });
      // Colisión de la valla (se puede saltar por encima)
      b.solidBox(mx, 0.5, mz, step, 1, 0.2, { rotY });
    }
  });
  // Almiar
  b.cylinder(-half / 2, 0.6, half / 3, 1.1, 1.2, PALETTE.straw, { collide: true });
  b.cone(-half / 2, 1.6, half / 3, 1.18, 0.8, PALETTE.strawDark);
  // Carro roto con una rueda suelta
  b.box(half / 3, 0.62, -half / 4, 1.8, 0.45, 1.1, PALETTE.wood, { rotZ: -0.14, rotY: 0.3 });
  b.cylinder(half / 3 + 0.2, 0.42, -half / 4 - 0.65, 0.42, 0.1, PALETTE.woodDark, { rotX: Math.PI / 2, rotY: 0.3, segments: 8 });
  b.cylinder(half / 3 + 1.4, 0.05, -half / 4 + 0.9, 0.42, 0.1, PALETTE.woodDark, { segments: 8 });
  b.solidBox(half / 3, 0.5, -half / 4, 1.8, 1, 1.1, { rotY: 0.3 });
  // Espantapájaros con sombrero
  const sx = rng.range(-1, 1);
  const sz = -half / 3;
  b.box(sx, 1.1, sz, 0.12, 2.2, 0.12, PALETTE.woodDark, { collide: true, standable: false });
  b.box(sx, 1.62, sz, 1.6, 0.1, 0.1, PALETTE.woodDark);
  b.box(sx, 1.42, sz, 0.62, 0.7, 0.32, PALETTE.plaid);
  b.blob(sx, 2.02, sz, 0.23, PALETTE.burlap, { shape: 'ico' });
  b.cylinder(sx, 2.2, sz, 0.36, 0.04, PALETTE.hat, { segments: 8 });
  b.cylinder(sx, 2.33, sz, 0.2, 0.24, PALETTE.hat, { segments: 8 });
  // Barriles y cajas apiladas (sirven de escalera)
  barrel(b, half - 0.8, half - 0.9);
  barrel(b, half - 1.7, half - 0.7);
  crate(b, rng, -half + 0.9, -half + 0.9);
  crate(b, rng, -half + 1.8, -half + 0.9);
  crate(b, rng, -half + 0.9, -half + 0.9, 0.8);
}

function buildWell(b: PropBuilder, rng: Rng): void {
  b.cylinder(0, 0.42, 0, 0.95, 0.85, PALETTE.stone, { material: 'stone', collide: true, segments: 10 });
  b.cylinder(0, 0.86, 0, 0.75, 0.02, PALETTE.water, { segments: 10 });
  b.box(-0.85, 1.6, 0, 0.15, 1.6, 0.15, PALETTE.woodDark);
  b.box(0.85, 1.6, 0, 0.15, 1.6, 0.15, PALETTE.woodDark);
  b.cylinder(0, 2.05, 0, 0.06, 1.8, PALETTE.wood, { rotZ: Math.PI / 2 });
  b.box(-0.42, 2.6, 0, 1.15, 0.08, 1.4, PALETTE.roof, { rotZ: 0.62 });
  b.box(0.42, 2.6, 0, 1.15, 0.08, 1.4, PALETTE.roof, { rotZ: -0.62 });
  b.cylinder(0.2, 1.35, 0, 0.17, 0.25, PALETTE.woodLight, { radiusTop: 0.2 });
  const n = rng.int(1, 3);
  for (let i = 0; i < n; i++) {
    const a = rng.next() * Math.PI * 2;
    if (rng.chance(0.5)) barrel(b, Math.cos(a) * 2.2, Math.sin(a) * 2.2);
    else crate(b, rng, Math.cos(a) * 2.2, Math.sin(a) * 2.2);
  }
}

// ------------------------------------------------------------------ objetos sueltos

function buildLog(b: PropBuilder, rng: Rng): void {
  const r = rng.range(0.32, 0.48);
  b.lyingCylinder(0, r, 0, r, rng.range(3, 4.8), PALETTE.bark, { collide: true });
  if (rng.chance(0.5)) b.blob(rng.range(-1, 1), r * 2 + 0.05, 0, 0.12, PALETTE.mushroomRed, { shape: 'ico', scaleY: 0.5 });
}

function buildStump(b: PropBuilder, rng: Rng): void {
  const r = rng.range(0.35, 0.5);
  const h = rng.range(0.45, 0.7);
  b.cylinder(0, h / 2, 0, r, h, PALETTE.bark, { radiusTop: r * 0.92, collide: true });
  b.cylinder(0, h + 0.01, 0, r * 0.9, 0.02, PALETTE.woodLight);
}

function buildMushrooms(b: PropBuilder, rng: Rng): void {
  const n = rng.int(3, 5);
  const red = rng.chance(0.6);
  for (let i = 0; i < n; i++) {
    const x = rng.range(-0.6, 0.6);
    const z = rng.range(-0.6, 0.6);
    const s = rng.range(0.7, 1.3);
    b.cylinder(x, 0.09 * s, z, 0.045 * s, 0.18 * s, PALETTE.mushroomStem, { segments: 5 });
    b.blob(x, 0.2 * s, z, 0.14 * s, red ? PALETTE.mushroomRed : PALETTE.mushroomBrown, { shape: 'ico', scaleY: 0.55 });
  }
}

function buildSignpost(b: PropBuilder, rng: Rng): void {
  b.box(0, 0.9, 0, 0.12, 1.8, 0.12, PALETTE.woodDark, { collide: true, standable: false });
  b.box(0.3, 1.5, 0, 0.8, 0.2, 0.05, PALETTE.woodLight, { rotY: rng.range(-0.4, 0.4) });
  b.box(-0.25, 1.15, 0, 0.7, 0.2, 0.05, PALETTE.woodLight, { rotY: Math.PI + rng.range(-0.6, 0.6) });
}

function buildWallFragment(b: PropBuilder, rng: Rng): void {
  const len = rng.range(2.5, 4.5);
  ruinedWall(b, rng, -len / 2, 0, len / 2, 0, rng.range(1.4, 2.6), { windows: false });
  rubble(b, rng, 4, len / 2 + 0.5, 1);
}

// ------------------------------------------------------------------ montaje

export interface PropOptions {
  limit: number;
  spawnClear: number;
}

/** Genera todas las construcciones de los sitios y los objetos sueltos del mapa. */
export function generateProps(hf: Heightfield, sites: readonly Site[], rng: Rng, options: PropOptions): PropSet {
  const set: PropSet = { parts: [], colliders: [] };
  const b = new PropBuilder(set);

  sites.forEach((site, i) => {
    const siteRng = rng.derive(`site-${i}`);
    b.at(site.x, site.floorY, site.z, site.rotation);
    switch (site.kind) {
      case 'house':
        buildHouse(b, siteRng);
        break;
      case 'temple':
        buildTemple(b, siteRng);
        break;
      case 'farm':
        buildFarm(b, siteRng);
        break;
      case 'well':
        buildWell(b, siteRng);
        break;
    }
  });

  // Objetos sueltos: más troncos, tocones y setas donde hay bosque. Los ids fijos
  // (no el nombre de la función, que cambia al minificar) mantienen la semilla estable.
  const forest = createNoise2D(() => rng.next());
  const normal: Vec3Like = { x: 0, y: 1, z: 0 };
  const kinds = [
    { id: 'log', build: buildLog, count: 18, forest: true, maxSlope: 10 },
    { id: 'stump', build: buildStump, count: 24, forest: true, maxSlope: 22 },
    { id: 'mushrooms', build: buildMushrooms, count: 30, forest: true, maxSlope: 22 },
    { id: 'signpost', build: buildSignpost, count: 7, forest: false, maxSlope: 20 },
    { id: 'wall', build: buildWallFragment, count: 10, forest: false, maxSlope: 12 },
  ];
  for (const kind of kinds) {
    const cosMax = Math.cos(kind.maxSlope * DEG2RAD);
    let placed = 0;
    for (let attempt = 0; attempt < kind.count * 30 && placed < kind.count; attempt++) {
      const x = rng.range(-options.limit, options.limit);
      const z = rng.range(-options.limit, options.limit);
      const rotation = rng.next() * Math.PI * 2;
      const roll = rng.next();
      if (squircle(x, z) > options.limit - 6) continue;
      if (Math.hypot(x, z) < options.spawnClear) continue;
      if (isInAnySite(sites, x, z, 6)) continue;
      hf.normalAt(x, z, normal);
      if (normal.y < cosMax) continue;
      if (kind.forest && forest(x * 0.018, z * 0.018) * 0.5 + 0.5 < roll * 0.8) continue;
      b.at(x, hf.heightAt(x, z) - 0.05, z, rotation);
      kind.build(b, rng.derive(`prop-${kind.id}-${placed}`));
      placed++;
    }
  }
  return set;
}
