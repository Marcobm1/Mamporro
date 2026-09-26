// Mesh del terreno a partir del heightfield: colores de vértice según altura y
// pendiente (paleta limitada) y una textura de detalle pixelada.
import {
  BufferAttribute,
  BufferGeometry,
  Color,
  Mesh,
  MeshLambertMaterial,
  type Texture,
} from 'three';
import { createNoise2D } from 'simplex-noise';
import type { Rng } from '../core/rng';
import { PALETTE } from '../render/palette';
import { applyRetro } from '../render/retroMaterial';
import type { Heightfield } from './Heightfield';
import type { Site } from './sites';

/** Metros que ocupa una repetición de la textura de detalle. */
const TEXTURE_METERS = 4;

/** Color del suelo dentro de un sitio (empedrado, tierra...) o null si (x, z) está fuera. */
function siteFloor(sites: readonly Site[], x: number, z: number, palette: Record<'cobble' | 'cobbleDark' | 'dirt', Color>): Color | null {
  for (const site of sites) {
    const d = Math.hypot(x - site.x, z - site.z);
    if (d > site.radius * 0.95) continue;
    if (site.kind === 'farm') return palette.dirt;
    if (site.kind === 'well' && d > 2.2) return palette.dirt;
    // Empedrado en damero irregular.
    const checker = (Math.floor(x / 1.3) + Math.floor(z / 1.3)) % 2 === 0;
    return checker ? palette.cobble : palette.cobbleDark;
  }
  return null;
}

export function createTerrainMesh(hf: Heightfield, detail: Texture, rng: Rng, sites: readonly Site[] = []): Mesh {
  const patches = createNoise2D(() => rng.next());
  const n = hf.stride;
  const positions = new Float32Array(n * n * 3);
  const colors = new Float32Array(n * n * 3);
  const uvs = new Float32Array(n * n * 2);
  const color = new Color();
  const palette = {
    grassLight: new Color(PALETTE.grassLight),
    grass: new Color(PALETTE.grass),
    grassDark: new Color(PALETTE.grassDark),
    moss: new Color(PALETTE.moss),
    dirt: new Color(PALETTE.dirt),
    sand: new Color(PALETTE.sand),
    rockLight: new Color(PALETTE.rockLight),
    rock: new Color(PALETTE.rock),
    rockDark: new Color(PALETTE.rockDark),
    snow: new Color(PALETTE.snow),
    cobble: new Color(PALETTE.cobble),
    cobbleDark: new Color(PALETTE.cobbleDark),
  };
  const rockBands = [palette.rockLight, palette.rock, palette.rockDark];

  for (let j = 0; j < n; j++) {
    for (let i = 0; i < n; i++) {
      const index = j * n + i;
      const x = i * hf.cellSize - hf.half;
      const z = j * hf.cellSize - hf.half;
      const y = hf.vertexHeight(i, j);
      positions[index * 3] = x;
      positions[index * 3 + 1] = y;
      positions[index * 3 + 2] = z;
      uvs[index * 2] = x / TEXTURE_METERS;
      uvs[index * 2 + 1] = z / TEXTURE_METERS;

      // Pendiente por diferencias centrales.
      const gx = (hf.vertexHeight(i + 1, j) - hf.vertexHeight(i - 1, j)) / (2 * hf.cellSize);
      const gz = (hf.vertexHeight(i, j + 1) - hf.vertexHeight(i, j - 1)) / (2 * hf.cellSize);
      const ny = 1 / Math.sqrt(gx * gx + 1 + gz * gz);
      const patch = patches(x * 0.04, z * 0.04) + 0.35 * patches(x * 0.15 + 30, z * 0.15 + 30);

      const floor = siteFloor(sites, x, z, palette);
      if (floor) {
        color.copy(floor);
      } else if (ny < 0.7) {
        // Roca a franjas (estratos) en los acantilados.
        color.copy(rockBands[Math.abs(Math.floor(y / 1.6)) % 3] as Color);
      } else if (y > 48) {
        color.copy(palette.snow);
      } else if (y > 34) {
        color.copy(patch > 0 ? palette.moss : palette.rock);
      } else if (ny < 0.8) {
        color.copy(palette.dirt);
      } else {
        if (patch > 0.45) color.copy(palette.grassLight);
        else if (patch < -0.4) color.copy(palette.grassDark);
        else color.copy(palette.grass);
        if (y < 2.5) color.lerp(patch > 0.2 ? palette.sand : palette.dirt, 0.45);
      }
      colors[index * 3] = color.r;
      colors[index * 3 + 1] = color.g;
      colors[index * 3 + 2] = color.b;
    }
  }

  // Mismo orden de triángulos que Heightfield.heightAt: (a, b, d) y (b, c, d).
  const cells = hf.cells;
  const indices = new Uint32Array(cells * cells * 6);
  let k = 0;
  for (let j = 0; j < cells; j++) {
    for (let i = 0; i < cells; i++) {
      const a = j * n + i;
      const b = (j + 1) * n + i;
      const c = (j + 1) * n + i + 1;
      const d = j * n + i + 1;
      indices[k++] = a;
      indices[k++] = b;
      indices[k++] = d;
      indices[k++] = b;
      indices[k++] = c;
      indices[k++] = d;
    }
  }

  const geometry = new BufferGeometry();
  geometry.setAttribute('position', new BufferAttribute(positions, 3));
  geometry.setAttribute('color', new BufferAttribute(colors, 3));
  geometry.setAttribute('uv', new BufferAttribute(uvs, 2));
  geometry.setIndex(new BufferAttribute(indices, 1));
  geometry.computeVertexNormals();
  geometry.computeBoundingSphere();

  const material = applyRetro(
    new MeshLambertMaterial({ vertexColors: true, map: detail, flatShading: true }),
  );
  const mesh = new Mesh(geometry, material);
  mesh.name = 'terrain';
  return mesh;
}
