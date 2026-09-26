// Convierte las piezas de construcciones y objetos en dos mallas estáticas
// (piedra y resto): miles de piezas, solo dos llamadas de dibujo.
import {
  BoxGeometry,
  BufferAttribute,
  Color,
  ConeGeometry,
  CylinderGeometry,
  DodecahedronGeometry,
  Euler,
  Group,
  IcosahedronGeometry,
  Matrix4,
  Mesh,
  MeshLambertMaterial,
  Quaternion,
  Vector3,
  type BufferGeometry,
  type Texture,
} from 'three';
import { mergeAttributes } from '../render/geometry';
import { applyRetro } from '../render/retroMaterial';
import type { Part, PartMaterial } from './props';

/** Metros que ocupa una repetición de la textura en cada material. */
const TEXTURE_METERS: Record<PartMaterial, number> = { stone: 1.6, plain: 1.2 };

const matrix = new Matrix4();
const quaternion = new Quaternion();
const euler = new Euler();
const position = new Vector3();
const unitScale = new Vector3(1, 1, 1);
const color = new Color();

function baseGeometry(part: Part): BufferGeometry {
  const [a, b, c] = part.size;
  switch (part.shape) {
    case 'box':
      return new BoxGeometry(a, b, c);
    case 'cylinder':
      return new CylinderGeometry(c, a, b, part.segments);
    case 'cone':
      return new ConeGeometry(a, b, part.segments);
    case 'ico':
      return new IcosahedronGeometry(a, 0).scale(1, b, 1);
    case 'dodeca':
      return new DodecahedronGeometry(a, 0).scale(1, b, 1);
  }
}

/** Pequeña variación de brillo estable por pieza (que no todo sea del mismo tono). */
function shadeJitter(part: Part): number {
  const h = Math.sin(part.position[0] * 12.9898 + part.position[2] * 78.233) * 43758.5453;
  return 0.92 + (h - Math.floor(h)) * 0.16;
}

/**
 * Coordenadas de textura proyectadas desde el eje dominante de cada cara, en
 * metros del mundo: la textura mantiene su escala en piezas de cualquier tamaño.
 */
function projectUVs(g: BufferGeometry, meters: number): void {
  const pos = g.getAttribute('position');
  const uv = new Float32Array(pos.count * 2);
  for (let i = 0; i < pos.count; i += 3) {
    const ax = pos.getX(i);
    const ay = pos.getY(i);
    const az = pos.getZ(i);
    const ux = pos.getX(i + 1) - ax;
    const uy = pos.getY(i + 1) - ay;
    const uz = pos.getZ(i + 1) - az;
    const vx = pos.getX(i + 2) - ax;
    const vy = pos.getY(i + 2) - ay;
    const vz = pos.getZ(i + 2) - az;
    const nx = Math.abs(uy * vz - uz * vy);
    const ny = Math.abs(uz * vx - ux * vz);
    const nz = Math.abs(ux * vy - uy * vx);
    for (let k = i; k < i + 3; k++) {
      const x = pos.getX(k);
      const y = pos.getY(k);
      const z = pos.getZ(k);
      let u: number;
      let v: number;
      if (ny >= nx && ny >= nz) {
        u = x;
        v = z;
      } else if (nx >= nz) {
        u = z;
        v = y;
      } else {
        u = x;
        v = y;
      }
      uv[k * 2] = u / meters;
      uv[k * 2 + 1] = v / meters;
    }
  }
  g.setAttribute('uv', new BufferAttribute(uv, 2));
}

function partGeometry(part: Part): BufferGeometry {
  const indexed = baseGeometry(part);
  const g = indexed.index ? indexed.toNonIndexed() : indexed;
  euler.set(part.rotation[0], part.rotation[1], part.rotation[2], 'YXZ');
  position.set(part.position[0], part.position[1], part.position[2]);
  matrix.compose(position, quaternion.setFromEuler(euler), unitScale);
  g.applyMatrix4(matrix);
  g.computeVertexNormals();

  const count = g.getAttribute('position').count;
  const colors = new Float32Array(count * 3);
  color.setHex(part.color).multiplyScalar(shadeJitter(part));
  for (let i = 0; i < count; i++) {
    colors[i * 3] = color.r;
    colors[i * 3 + 1] = color.g;
    colors[i * 3 + 2] = color.b;
  }
  g.setAttribute('color', new BufferAttribute(colors, 3));
  projectUVs(g, TEXTURE_METERS[part.material]);
  return g;
}

export function createPropMeshes(parts: readonly Part[], textures: Record<PartMaterial, Texture>): Group {
  const group = new Group();
  group.name = 'props';
  const buckets: Record<PartMaterial, BufferGeometry[]> = { stone: [], plain: [] };
  for (const part of parts) buckets[part.material].push(partGeometry(part));

  for (const material of ['stone', 'plain'] as const) {
    const geometries = buckets[material];
    if (geometries.length === 0) continue;
    const merged = mergeAttributes(geometries, ['position', 'normal', 'color', 'uv']);
    geometries.forEach((g) => g.dispose());
    const mesh = new Mesh(
      merged,
      applyRetro(new MeshLambertMaterial({ vertexColors: true, map: textures[material], flatShading: true })),
    );
    mesh.name = `props-${material}`;
    group.add(mesh);
  }
  return group;
}
