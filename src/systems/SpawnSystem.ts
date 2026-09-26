// Aparición de enemigos alrededor del jugador, preferentemente fuera de la vista
// de la cámara, con el ritmo y la vida que marque el director. También hace
// aparecer oleadas en formación, élites y al jefe.
import type { Rng } from '../core/rng';
import { ENEMY_LIST, enemyTypeIndex, type EnemyId } from '../data/enemies';
import { SPAWN_CURVE, type WaveFormation } from '../data/waves';
import type { WorldCollision } from '../world/WorldCollision';
import { pickEnemy } from './difficulty';
import type { EnemySystem } from './EnemySystem';

export interface SpawnView {
  /** Posición del jugador. */
  x: number;
  z: number;
  /** Giro de la cámara: se evita que los enemigos aparezcan delante de ella. */
  yaw: number;
}

/** Cómo aparecen los enemigos en este momento (lo decide el director). */
export interface SpawnParams {
  /** Minuto de dificultad: qué enemigos pueden salir. */
  minutes: number;
  /** Enemigos por segundo. */
  rate: number;
  /** Máximo de enemigos vivos. */
  maxAlive: number;
  /** Multiplicadores de vida, experiencia y oro de los que aparecen. */
  hp: number;
  xp: number;
  gold: number;
}

/** Distancias de las formaciones (m). */
const RING_RADIUS = 24;
const LINE_DISTANCE = 32;
const LINE_LENGTH = 44;
const ARC_DISTANCE = 26;
const ARC_HALF_ANGLE = 1.2;

export class SpawnSystem {
  private accumulator = 0;
  private readonly spot = { x: 0, z: 0 };

  constructor(
    private readonly rng: Rng,
    private readonly onSpawn?: (x: number, y: number, z: number) => void,
  ) {}

  update(dt: number, params: SpawnParams, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    this.recycleFar(enemies, view, world);
    this.accumulator += params.rate * dt;
    while (this.accumulator >= 1) {
      this.accumulator -= 1;
      if (enemies.count >= params.maxAlive) {
        this.accumulator = 0;
        break;
      }
      this.spawnOne(params, enemies, view, world);
    }
  }

  /** Hace aparecer `count` enemigos de golpe (acciones de debug). */
  spawnBurst(count: number, params: SpawnParams, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    for (let n = 0; n < count; n++) this.spawnOne(params, enemies, view, world);
  }

  private spawnOne(params: SpawnParams, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    if (!this.findSpot(view, world)) return;
    const type = enemyTypeIndex(pickEnemy(params.minutes, () => this.rng.next()));
    this.place(type, this.spot.x, this.spot.z, params, enemies, world);
  }

  /** Crea un enemigo en (x, z) sobre el terreno; devuelve su índice o -1. */
  place(type: number, x: number, z: number, params: SpawnParams, enemies: EnemySystem, world: WorldCollision, hp = params.hp): number {
    const y = world.heightfield.heightAt(x, z);
    const i = enemies.spawn(type, x, y, z, hp, params.xp, params.gold);
    if (i >= 0) this.onSpawn?.(x, y, z);
    return i;
  }

  /** Un enemigo concreto (élite, jefe...) en el anillo de aparición; devuelve su índice o -1. */
  spawnSpecial(id: EnemyId, hp: number, params: SpawnParams, enemies: EnemySystem, view: SpawnView, world: WorldCollision): number {
    if (!this.findSpot(view, world)) return -1;
    return this.place(enemyTypeIndex(id), this.spot.x, this.spot.z, params, enemies, world, hp);
  }

  /** Oleada especial: `count` enemigos a la vez en fila, en anillo o en arco por delante. */
  spawnFormation(
    id: EnemyId,
    count: number,
    formation: WaveFormation,
    params: SpawnParams,
    enemies: EnemySystem,
    view: SpawnView,
    world: WorldCollision,
  ): number {
    const type = enemyTypeIndex(id);
    const angle0 = this.rng.next() * Math.PI * 2;
    const fx = -Math.sin(view.yaw);
    const fz = -Math.cos(view.yaw);
    let spawned = 0;
    for (let k = 0; k < count; k++) {
      const t = count > 1 ? k / (count - 1) : 0.5;
      let x = view.x;
      let z = view.z;
      if (formation === 'ring') {
        const a = angle0 + (k / count) * Math.PI * 2;
        const r = RING_RADIUS + this.rng.range(-1.5, 1.5);
        x += Math.cos(a) * r;
        z += Math.sin(a) * r;
      } else if (formation === 'arc') {
        const a = Math.atan2(fx, fz) + (t - 0.5) * 2 * ARC_HALF_ANGLE;
        x += Math.sin(a) * ARC_DISTANCE;
        z += Math.cos(a) * ARC_DISTANCE;
      } else {
        // Fila perpendicular a una dirección al azar; si son muchos, en varias filas.
        const rows = Math.ceil(count / 30);
        const row = k % rows;
        const along = (Math.floor(k / rows) / Math.max(1, Math.ceil(count / rows) - 1) - 0.5) * LINE_LENGTH;
        const dx = Math.cos(angle0);
        const dz = Math.sin(angle0);
        const dist = LINE_DISTANCE + row * 1.6;
        x += dx * dist - dz * along;
        z += dz * dist + dx * along;
      }
      if (!world.isInside(x, z, 3)) continue;
      if (this.place(type, x, z, params, enemies, world) >= 0) spawned++;
    }
    return spawned;
  }

  /** Busca un punto en el anillo de aparición, dentro del mapa y a ser posible fuera de cámara. */
  private findSpot(view: SpawnView, world: WorldCollision): boolean {
    const fx = -Math.sin(view.yaw);
    const fz = -Math.cos(view.yaw);
    for (let attempt = 0; attempt < 10; attempt++) {
      const angle = this.rng.next() * Math.PI * 2;
      const avoidView = this.rng.next() < 0.85;
      const distance = this.rng.range(SPAWN_CURVE.spawnDistanceMin, SPAWN_CURVE.spawnDistanceMax);
      const dirX = Math.sin(angle);
      const dirZ = Math.cos(angle);
      if (avoidView && dirX * fx + dirZ * fz > 0.45) continue;
      const x = view.x + dirX * distance;
      const z = view.z + dirZ * distance;
      if (!world.isInside(x, z, 3)) continue;
      this.spot.x = x;
      this.spot.z = z;
      return true;
    }
    return false;
  }

  /** Los enemigos normales que se quedan muy atrás reaparecen cerca del jugador (élites y jefe no). */
  private recycleFar(enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    const max2 = SPAWN_CURVE.recycleDistance * SPAWN_CURVE.recycleDistance;
    for (let i = 0; i < enemies.count; i++) {
      const dx = (enemies.x[i] as number) - view.x;
      const dz = (enemies.z[i] as number) - view.z;
      if (dx * dx + dz * dz < max2) continue;
      if (ENEMY_LIST[enemies.type[i] as number]?.special) continue;
      if (!this.findSpot(view, world)) continue;
      enemies.relocate(i, this.spot.x, world.heightfield.heightAt(this.spot.x, this.spot.z), this.spot.z);
    }
  }
}
