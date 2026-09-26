// Aparición de enemigos alrededor del jugador, preferentemente fuera de la vista
// de la cámara, con ritmo y vida crecientes según el tiempo de partida.
import type { Rng } from '../core/rng';
import { enemyTypeIndex } from '../data/enemies';
import { SPAWN_CURVE } from '../data/waves';
import type { WorldCollision } from '../world/WorldCollision';
import { enemyHpMultiplier, enemyXpMultiplier, maxAlive, pickEnemy, spawnRate } from './difficulty';
import type { EnemySystem } from './EnemySystem';

export interface SpawnView {
  /** Posición del jugador. */
  x: number;
  z: number;
  /** Giro de la cámara: se evita que los enemigos aparezcan delante de ella. */
  yaw: number;
}

export class SpawnSystem {
  private accumulator = 0;
  private readonly spot = { x: 0, z: 0 };

  constructor(
    private readonly rng: Rng,
    private readonly onSpawn?: (x: number, y: number, z: number) => void,
  ) {}

  update(dt: number, minutes: number, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    this.recycleFar(enemies, view, world);
    const cap = maxAlive(minutes);
    this.accumulator += spawnRate(minutes) * dt;
    while (this.accumulator >= 1) {
      this.accumulator -= 1;
      if (enemies.count >= cap) {
        this.accumulator = 0;
        break;
      }
      this.spawnOne(minutes, enemies, view, world);
    }
  }

  /** Hace aparecer `count` enemigos de golpe (acciones de debug). */
  spawnBurst(count: number, minutes: number, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    for (let n = 0; n < count; n++) this.spawnOne(minutes, enemies, view, world);
  }

  private spawnOne(minutes: number, enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    if (!this.findSpot(view, world)) return;
    const type = enemyTypeIndex(pickEnemy(minutes, () => this.rng.next()));
    const y = world.heightfield.heightAt(this.spot.x, this.spot.z);
    const i = enemies.spawn(type, this.spot.x, y, this.spot.z, enemyHpMultiplier(minutes), enemyXpMultiplier(minutes));
    if (i >= 0) this.onSpawn?.(this.spot.x, y, this.spot.z);
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

  /** Los enemigos que se quedan muy atrás reaparecen cerca del jugador. */
  private recycleFar(enemies: EnemySystem, view: SpawnView, world: WorldCollision): void {
    const max2 = SPAWN_CURVE.recycleDistance * SPAWN_CURVE.recycleDistance;
    for (let i = 0; i < enemies.count; i++) {
      const dx = (enemies.x[i] as number) - view.x;
      const dz = (enemies.z[i] as number) - view.z;
      if (dx * dx + dz * dz < max2) continue;
      if (!this.findSpot(view, world)) continue;
      enemies.relocate(i, this.spot.x, world.heightfield.heightAt(this.spot.x, this.spot.z), this.spot.z);
    }
  }
}
