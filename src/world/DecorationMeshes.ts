// Modelos low-poly de la decoración, dibujados con InstancedMesh (una llamada de
// dibujo por tipo de pieza, sin importar cuántos árboles haya).
import {
  Color,
  ConeGeometry,
  CylinderGeometry,
  DodecahedronGeometry,
  Group,
  IcosahedronGeometry,
  InstancedMesh,
  MeshLambertMaterial,
  Object3D,
  type BufferGeometry,
  type Texture,
} from 'three';
import type { Rng } from '../core/rng';
import { colored, jitterVertices, mergeColored } from '../render/geometry';
import { PALETTE } from '../render/palette';
import { applyRetro, type WindOptions } from '../render/retroMaterial';
import { ROCK_SHAPE, type Decoration } from './decorations';

const ROCK_VARIANTS = 3;
const CANOPY_COLORS = [PALETTE.leaf, PALETTE.leafLight, PALETTE.leaf, PALETTE.leafYellow, PALETTE.leafOrange];

function lambert(
  options: { color?: number; map?: Texture; vertexColors?: boolean },
  wind?: WindOptions,
): MeshLambertMaterial {
  return applyRetro(new MeshLambertMaterial({ ...options, flatShading: true }), wind ? { wind } : {});
}

function buildInstanced(
  geometry: BufferGeometry,
  material: MeshLambertMaterial,
  items: readonly Decoration[],
  place: (d: Decoration, dummy: Object3D) => void,
  colorOf?: (d: Decoration) => number,
): InstancedMesh {
  const mesh = new InstancedMesh(geometry, material, Math.max(1, items.length));
  mesh.count = items.length;
  const dummy = new Object3D();
  const color = new Color();
  items.forEach((d, i) => {
    dummy.position.set(d.x, d.y, d.z);
    dummy.rotation.set(0, d.rotation, 0);
    dummy.scale.setScalar(d.scale);
    place(d, dummy);
    dummy.updateMatrix();
    mesh.setMatrixAt(i, dummy.matrix);
    if (colorOf) mesh.setColorAt(i, color.setHex(colorOf(d)));
  });
  mesh.instanceMatrix.needsUpdate = true;
  if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
  // Las instancias cubren todo el mapa: el recorte por frustum del conjunto no ayuda.
  mesh.frustumCulled = false;
  return mesh;
}

export function createDecorationMeshes(decorations: readonly Decoration[], detail: Texture, rng: Rng): Group {
  const group = new Group();
  group.name = 'decorations';
  const byKind = (kind: Decoration['kind']): Decoration[] => decorations.filter((d) => d.kind === kind);
  const trees = byKind('tree');
  const pines = byKind('pine');
  const rocks = byKind('rock');
  const bushes = byKind('bush');
  const noPlacement = (): void => {};

  // Árbol: tronco + copa facetada.
  const trunkGeometry = new CylinderGeometry(0.22, 0.32, 2.4, 5).translate(0, 1.2, 0);
  const canopyGeometry = jitterVertices(new IcosahedronGeometry(1.55, 0), rng, 0.22).translate(0, 3.1, 0);
  const trunkMaterial = lambert({ color: PALETTE.bark, map: detail });
  group.add(buildInstanced(trunkGeometry, trunkMaterial, trees, noPlacement));
  group.add(
    buildInstanced(canopyGeometry, lambert({ map: detail }, { amplitude: 0.05, start: 2, speed: 1.3 }), trees, noPlacement, (d) =>
      CANOPY_COLORS[Math.floor(d.variant * CANOPY_COLORS.length)] ?? PALETTE.leaf,
    ),
  );

  // Pino: tronco corto + dos conos, en una sola geometría con color de vértice.
  const pineGeometry = mergeColored([
    colored(new CylinderGeometry(0.18, 0.26, 1.4, 5).translate(0, 0.7, 0), PALETTE.bark),
    colored(new ConeGeometry(1.45, 2.4, 6).translate(0, 2.3, 0), PALETTE.pine),
    colored(new ConeGeometry(1.05, 2, 6).translate(0, 3.6, 0), PALETTE.pine),
  ]);
  group.add(
    buildInstanced(pineGeometry, lambert({ vertexColors: true, map: detail }, { amplitude: 0.035, start: 1, speed: 1.1 }), pines, noPlacement),
  );

  // Rocas: tres variantes deformadas; la forma coincide con su collider.
  const rockMaterial = lambert({ color: PALETTE.rock, map: detail });
  for (let v = 0; v < ROCK_VARIANTS; v++) {
    const geometry = jitterVertices(new DodecahedronGeometry(1, 0), rng, 0.18);
    geometry.scale(1, ROCK_SHAPE.heightScale, 1);
    const items = rocks.filter((d) => Math.floor(d.variant * ROCK_VARIANTS) === v);
    group.add(buildInstanced(geometry, rockMaterial, items, noPlacement));
  }

  // Arbustos: bolas achatadas.
  const bushGeometry = jitterVertices(new IcosahedronGeometry(0.75, 0), rng, 0.12);
  bushGeometry.scale(1.2, 0.75, 1.2).translate(0, 0.4, 0);
  group.add(
    buildInstanced(bushGeometry, lambert({ color: PALETTE.bush, map: detail }, { amplitude: 0.06, start: 0.1, speed: 1.8 }), bushes, noPlacement),
  );

  return group;
}
