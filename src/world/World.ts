// El mapa de una partida: terreno, decoración, colisiones y punto de inicio.
// Todo se genera de forma determinista a partir de la semilla.
import { Group, Material, Mesh, type Texture } from 'three';
import { Rng } from '../core/rng';
import { TERRAIN_CONFIG, WORLD_CONFIG } from '../data/config';
import { createDecorationMeshes } from './DecorationMeshes';
import {
  ColliderGrid,
  decorationColliders,
  placeDecorations,
  type Decoration,
} from './decorations';
import { generateHeightfield, type Heightfield } from './Heightfield';
import { createTerrainMesh } from './TerrainMesh';
import { WorldCollision } from './WorldCollision';

export interface WorldData {
  heightfield: Heightfield;
  decorations: Decoration[];
  collision: WorldCollision;
}

/** Genera los datos del mundo (sin gráficos): útil para tests y para el juego. */
export function generateWorldData(seed: string): WorldData {
  const rng = new Rng(seed);
  const heightfield = generateHeightfield(TERRAIN_CONFIG, rng.derive('terrain'));
  const decorations = placeDecorations(heightfield, rng.derive('decorations'), {
    spawnRadius: WORLD_CONFIG.clearSpawnRadius,
    limit: WORLD_CONFIG.playableRadius,
  });
  const grid = new ColliderGrid(decorationColliders(decorations), heightfield.half);
  const collision = new WorldCollision(heightfield, grid, WORLD_CONFIG.playableRadius);
  return { heightfield, decorations, collision };
}

export class World {
  readonly heightfield: Heightfield;
  readonly decorations: Decoration[];
  readonly collision: WorldCollision;
  readonly group = new Group();

  constructor(
    readonly seed: string,
    detailTexture: Texture,
  ) {
    const data = generateWorldData(seed);
    this.heightfield = data.heightfield;
    this.decorations = data.decorations;
    this.collision = data.collision;

    const rng = new Rng(seed);
    this.group.add(createTerrainMesh(this.heightfield, detailTexture, rng.derive('terrain-colors')));
    this.group.add(createDecorationMeshes(this.decorations, detailTexture, rng.derive('decoration-meshes')));
  }

  get spawnPoint(): { x: number; y: number; z: number } {
    return { x: 0, y: this.heightfield.heightAt(0, 0), z: 0 };
  }

  /** Libera geometrías y materiales (las texturas compartidas las gestiona quien las creó). */
  dispose(): void {
    this.group.traverse((obj) => {
      if (obj instanceof Mesh) {
        obj.geometry.dispose();
        const materials: Material[] = Array.isArray(obj.material) ? obj.material : [obj.material];
        materials.forEach((m) => m.dispose());
      }
    });
    this.group.removeFromParent();
  }
}
