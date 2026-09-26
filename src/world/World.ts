// El mapa de una partida: terreno, construcciones, vegetación, fauna, colisiones
// y punto de inicio. Todo se genera de forma determinista a partir de la semilla.
import { Group, Material, Mesh, type Texture } from 'three';
import { Rng } from '../core/rng';
import { TERRAIN_CONFIG, WORLD_CONFIG } from '../data/config';
import { AmbientLife } from './AmbientLife';
import { ColliderGrid, type Collider } from './colliders';
import { createDecorationMeshes } from './DecorationMeshes';
import { decorationColliders, placeDecorations, type Decoration } from './decorations';
import { placeGroundCover, type GroundCover } from './groundCover';
import { createGroundCoverMeshes } from './GroundCoverMeshes';
import { generateHeightfield, type Heightfield } from './Heightfield';
import type { InteractableState } from '../systems/Interactables';
import { InteractableMeshes } from './InteractableMeshes';
import { interactableClearRadius, interactableCollider, placeInteractables, type InteractableSpot } from './interactables';
import { createPropMeshes } from './PropMeshes';
import { generateProps, type PropSet } from './props';
import { flattenSites, isInAnySite, pickSites, type Site } from './sites';
import { createTerrainMesh } from './TerrainMesh';
import { WorldCollision } from './WorldCollision';

export interface WorldData {
  heightfield: Heightfield;
  sites: Site[];
  props: PropSet;
  /** Cofres, santuarios, tótems y el portal del jefe. */
  interactables: InteractableSpot[];
  decorations: Decoration[];
  groundCover: GroundCover;
  colliders: Collider[];
  collision: WorldCollision;
}

export interface WorldTextures {
  detail: Texture;
  stone: Texture;
}

/** Genera los datos del mundo (sin gráficos): útil para tests y para el juego. */
export function generateWorldData(seed: string): WorldData {
  const rng = new Rng(seed);
  const limit = WORLD_CONFIG.playableRadius;
  const heightfield = generateHeightfield(TERRAIN_CONFIG, rng.derive('terrain'));

  // Primero las construcciones: eligen sitio y aplanan el terreno debajo.
  const sites = pickSites(heightfield, rng.derive('sites'), WORLD_CONFIG.sites, {
    limit,
    spawnClear: WORLD_CONFIG.siteSpawnClear,
    gap: 8,
    maxRange: 4.5,
  });
  flattenSites(heightfield, sites);
  const props = generateProps(heightfield, sites, rng.derive('props'), { limit, spawnClear: 16 });

  // Los interactuables, en sitios libres de obstáculos.
  const interactables = placeInteractables(heightfield, sites, rng.derive('interactables'), {
    limit,
    obstacles: new ColliderGrid(props.colliders, heightfield.half),
  });
  const nearInteractable = (x: number, z: number): boolean =>
    interactables.some((spot) => Math.hypot(spot.x - x, spot.z - z) < interactableClearRadius(spot.kind));

  // Después la vegetación, fuera de las construcciones y sin tapar interactuables.
  const decorations = placeDecorations(heightfield, rng.derive('decorations'), {
    spawnRadius: WORLD_CONFIG.clearSpawnRadius,
    limit,
    isReserved: (x, z) => isInAnySite(sites, x, z, 1.5) || nearInteractable(x, z),
  });
  const groundCover = placeGroundCover(heightfield, rng.derive('ground-cover'), {
    limit,
    isReserved: (x, z) => isInAnySite(sites, x, z, -1),
  });

  const colliders: Collider[] = [...decorationColliders(decorations), ...props.colliders, ...interactables.map(interactableCollider)];
  const grid = new ColliderGrid(colliders, heightfield.half);
  const collision = new WorldCollision(heightfield, grid, limit);
  return { heightfield, sites, props, interactables, decorations, groundCover, colliders, collision };
}

export class World {
  readonly heightfield: Heightfield;
  readonly sites: Site[];
  readonly decorations: Decoration[];
  readonly interactables: InteractableSpot[];
  readonly collision: WorldCollision;
  readonly group = new Group();
  private readonly fauna: AmbientLife;
  private readonly interactableMeshes: InteractableMeshes;

  constructor(
    readonly seed: string,
    textures: WorldTextures,
  ) {
    const data = generateWorldData(seed);
    this.heightfield = data.heightfield;
    this.sites = data.sites;
    this.decorations = data.decorations;
    this.interactables = data.interactables;
    this.collision = data.collision;

    const rng = new Rng(seed);
    this.group.add(createTerrainMesh(this.heightfield, textures.detail, rng.derive('terrain-colors'), this.sites));
    this.group.add(createDecorationMeshes(this.decorations, textures.detail, rng.derive('decoration-meshes')));
    this.group.add(createPropMeshes(data.props.parts, { stone: textures.stone, plain: textures.detail }));
    this.group.add(createGroundCoverMeshes(data.groundCover));
    this.fauna = new AmbientLife(this.heightfield, data.groundCover.flowers, seed, WORLD_CONFIG.playableRadius);
    this.group.add(this.fauna.group);
    this.interactableMeshes = new InteractableMeshes(this.interactables, this.heightfield);
    this.group.add(this.interactableMeshes.group);
  }

  get spawnPoint(): { x: number; y: number; z: number } {
    return { x: 0, y: this.heightfield.heightAt(0, 0), z: 0 };
  }

  /** Animaciones: fauna y estado de los interactuables (`null` sin partida). */
  update(time: number, interactables: readonly InteractableState[] | null): void {
    this.fauna.update(time);
    this.interactableMeshes.update(interactables, time);
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
