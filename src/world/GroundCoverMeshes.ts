// Hierba y flores instanciadas que se mecen con el viento.
import {
  BufferAttribute,
  BufferGeometry,
  Color,
  DoubleSide,
  Group,
  InstancedMesh,
  MeshLambertMaterial,
  Object3D,
  OctahedronGeometry,
} from 'three';
import { Rng } from '../core/rng';
import { PALETTE } from '../render/palette';
import { applyRetro } from '../render/retroMaterial';
import type { CoverInstance, GroundCover } from './groundCover';

const FLOWER_COLORS = [PALETTE.flowerRed, PALETTE.flowerYellow, PALETTE.flowerWhite, PALETTE.flowerPurple];

/** Mata de hierba: varias briznas triangulares, oscuras abajo y claras en la punta. */
function tuftGeometry(): BufferGeometry {
  const rng = new Rng('tuft');
  const base = new Color(PALETTE.grassTuft);
  const tip = new Color(PALETTE.grassTuftTip);
  const blades = 5;
  const positions: number[] = [];
  const colors: number[] = [];
  for (let i = 0; i < blades; i++) {
    const a = (i / blades) * Math.PI * 2 + rng.range(-0.3, 0.3);
    const r = rng.range(0.03, 0.12);
    const h = rng.range(0.32, 0.5);
    const lean = rng.range(0.08, 0.16);
    const cx = Math.cos(a) * r;
    const cz = Math.sin(a) * r;
    const px = -Math.sin(a) * 0.06;
    const pz = Math.cos(a) * 0.06;
    positions.push(cx - px, 0, cz - pz, cx + px, 0, cz + pz, cx + Math.cos(a) * lean, h, cz + Math.sin(a) * lean);
    colors.push(base.r, base.g, base.b, base.r, base.g, base.b, tip.r, tip.g, tip.b);
  }
  const g = new BufferGeometry();
  g.setAttribute('position', new BufferAttribute(new Float32Array(positions), 3));
  g.setAttribute('color', new BufferAttribute(new Float32Array(colors), 3));
  g.computeVertexNormals();
  return g;
}

/** Tallo de flor (una brizna verde) — la cabeza va en otra malla para poder teñirla. */
function stemGeometry(): BufferGeometry {
  const green = new Color(PALETTE.grassTuft);
  const g = new BufferGeometry();
  g.setAttribute('position', new BufferAttribute(new Float32Array([-0.02, 0, 0, 0.02, 0, 0, 0, 0.3, 0]), 3));
  g.setAttribute('color', new BufferAttribute(new Float32Array([green.r, green.g, green.b, green.r, green.g, green.b, green.r, green.g, green.b]), 3));
  g.computeVertexNormals();
  return g;
}

function instanced(geometry: BufferGeometry, material: MeshLambertMaterial, items: readonly CoverInstance[], colorOf?: (c: CoverInstance) => number): InstancedMesh {
  const mesh = new InstancedMesh(geometry, material, Math.max(1, items.length));
  mesh.count = items.length;
  const dummy = new Object3D();
  const color = new Color();
  items.forEach((item, i) => {
    dummy.position.set(item.x, item.y - 0.02, item.z);
    dummy.rotation.set(0, item.rotation, 0);
    dummy.scale.setScalar(item.scale);
    dummy.updateMatrix();
    mesh.setMatrixAt(i, dummy.matrix);
    if (colorOf) mesh.setColorAt(i, color.setHex(colorOf(item)));
  });
  mesh.instanceMatrix.needsUpdate = true;
  mesh.frustumCulled = false;
  return mesh;
}

export function createGroundCoverMeshes(cover: GroundCover): Group {
  const group = new Group();
  group.name = 'ground-cover';
  const wind = { amplitude: 0.35, start: 0.02, speed: 2.4 };
  const plantMaterial = applyRetro(new MeshLambertMaterial({ vertexColors: true, side: DoubleSide }), { wind });
  group.add(instanced(tuftGeometry(), plantMaterial, cover.grass));
  group.add(instanced(stemGeometry(), plantMaterial, cover.flowers));
  const head = new OctahedronGeometry(0.065, 0).translate(0, 0.31, 0);
  const headMaterial = applyRetro(new MeshLambertMaterial({ color: 0xffffff }), { wind });
  group.add(instanced(head, headMaterial, cover.flowers, (f) => FLOWER_COLORS[f.variant % FLOWER_COLORS.length] ?? PALETTE.flowerRed));
  return group;
}
