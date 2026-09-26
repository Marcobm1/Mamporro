// Enemigos en arrays planos (estructura de arrays): cientos de enemigos sin crear
// objetos por frame. Persiguen al jugador, se separan entre sí, rodean obstáculos
// cuando se atascan y hacen daño por contacto.
import { ENEMY_LIST } from '../data/enemies';
import type { WorldCollision } from '../world/WorldCollision';
import { SpatialGrid } from './SpatialGrid';

/** Radio de búsqueda de vecinos para separarse (≈ el doble del radio mayor). */
const NEIGHBOR_RADIUS = 1.2;
/** Velocidad máxima de separación entre enemigos solapados (m/s). */
const SEPARATION_SPEED = 4;
/** Amortiguación del empuje por golpes (1/s). */
const KNOCKBACK_DECAY = 7;
/** Tiempo chocando contra un obstáculo antes de empezar a rodearlo. */
const STUCK_TIME = 0.35;
/** Cuánto dura el rodeo y cuánto se desvía de la línea recta (rad). */
const DETOUR_TIME = 1.4;
const DETOUR_ANGLE = 1.1;
/** Tiempo entre dos golpes de un mismo enemigo al jugador. */
const ATTACK_COOLDOWN = 0.9;
/** Altura que un enemigo sube sin problema (plataformas bajas, escalones). */
export const ENEMY_STEP = 0.6;
/**
 * Diferencia de altura hasta la que un enemigo alcanza al jugador (salta a
 * morder): subirse a una roca o a unas cajas no te pone a salvo.
 */
export const ENEMY_REACH_HEIGHT = 1.6;

export interface PlayerTarget {
  x: number;
  /** Altura de los pies del jugador. */
  y: number;
  z: number;
  radius: number;
}

export class EnemySystem {
  count = 0;
  // Estado
  readonly x: Float32Array;
  readonly y: Float32Array;
  readonly z: Float32Array;
  /** Posición del tick anterior (para interpolar en el render). */
  readonly px: Float32Array;
  readonly py: Float32Array;
  readonly pz: Float32Array;
  readonly vx: Float32Array;
  readonly vz: Float32Array;
  /** Empuje por golpes (se suma a la velocidad y se amortigua). */
  readonly kx: Float32Array;
  readonly kz: Float32Array;
  readonly hp: Float32Array;
  readonly maxHp: Float32Array;
  readonly xp: Float32Array;
  readonly speed: Float32Array;
  readonly flash: Float32Array;
  readonly attackCd: Float32Array;
  readonly stuckTime: Float32Array;
  readonly detourTime: Float32Array;
  readonly heading: Float32Array;
  readonly phase: Float32Array;
  readonly detour: Int8Array;
  readonly type: Uint8Array;
  /** Identificador único (los índices cambian al eliminar; el id no). */
  readonly id: Uint32Array;

  readonly grid: SpatialGrid;
  private readonly neighbors: Int32Array;
  /** Todos los arrays de números reales, para copiarlos de golpe al eliminar. */
  private readonly floatArrays: Float32Array[];
  private readonly pos = { x: 0, z: 0 };
  private nextId = 1;

  constructor(
    readonly capacity: number,
    worldSize: number,
  ) {
    const f = (): Float32Array => new Float32Array(capacity);
    this.x = f();
    this.y = f();
    this.z = f();
    this.px = f();
    this.py = f();
    this.pz = f();
    this.vx = f();
    this.vz = f();
    this.kx = f();
    this.kz = f();
    this.hp = f();
    this.maxHp = f();
    this.xp = f();
    this.speed = f();
    this.flash = f();
    this.attackCd = f();
    this.stuckTime = f();
    this.detourTime = f();
    this.heading = f();
    this.phase = f();
    this.detour = new Int8Array(capacity);
    this.type = new Uint8Array(capacity);
    this.id = new Uint32Array(capacity);
    this.grid = new SpatialGrid(worldSize, 4, capacity);
    this.neighbors = new Int32Array(64);
    this.floatArrays = [
      this.x, this.y, this.z, this.px, this.py, this.pz, this.vx, this.vz, this.kx, this.kz,
      this.hp, this.maxHp, this.xp, this.speed, this.flash, this.attackCd, this.stuckTime,
      this.detourTime, this.heading, this.phase,
    ];
  }

  /** Crea un enemigo y devuelve su índice (o -1 si no cabe). */
  spawn(typeIndex: number, x: number, y: number, z: number, hpMultiplier: number, xpMultiplier: number): number {
    const def = ENEMY_LIST[typeIndex];
    if (!def || this.count >= this.capacity) return -1;
    const i = this.count++;
    const id = this.nextId++;
    // Pequeña variación estable de velocidad para que no avancen en bloque.
    const variation = 0.9 + ((Math.imul(id, 2654435761) >>> 0) % 1000) / 1000 * 0.2;
    this.x[i] = this.px[i] = x;
    this.y[i] = this.py[i] = y;
    this.z[i] = this.pz[i] = z;
    this.vx[i] = this.vz[i] = this.kx[i] = this.kz[i] = 0;
    this.hp[i] = this.maxHp[i] = def.hp * hpMultiplier;
    this.xp[i] = def.xp * xpMultiplier;
    this.speed[i] = def.speed * variation;
    this.flash[i] = 0;
    this.attackCd[i] = 0;
    this.stuckTime[i] = this.detourTime[i] = 0;
    this.detour[i] = 0;
    this.heading[i] = 0;
    this.phase[i] = (id * 0.618) % 1 * Math.PI * 2;
    this.type[i] = typeIndex;
    this.id[i] = id;
    return i;
  }

  /** Mueve un enemigo a otra posición al instante (reaparecer cerca del jugador). */
  relocate(i: number, x: number, y: number, z: number): void {
    this.x[i] = this.px[i] = x;
    this.y[i] = this.py[i] = y;
    this.z[i] = this.pz[i] = z;
    this.vx[i] = this.vz[i] = this.kx[i] = this.kz[i] = 0;
  }

  /** Elimina el enemigo `i` moviendo el último a su hueco (O(1)). */
  remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    for (const a of this.floatArrays) a[i] = a[last] as number;
    this.detour[i] = this.detour[last] as number;
    this.type[i] = this.type[last] as number;
    this.id[i] = this.id[last] as number;
  }

  /**
   * Elimina a los enemigos con vida ≤ 0 (llamando antes a `onDeath`) y rehace la
   * rejilla. Se hace al final del tick para que los índices no cambien mientras
   * las armas los usan.
   */
  flushDead(onDeath: (index: number) => void): void {
    for (let i = this.count - 1; i >= 0; i--) {
      if ((this.hp[i] as number) > 0) continue;
      onDeath(i);
      this.remove(i);
    }
    this.rebuildGrid();
  }

  rebuildGrid(): void {
    this.grid.rebuild(this.x, this.z, this.count);
  }

  /**
   * Mueve a todos los enemigos un paso. Devuelve el daño de contacto más alto de
   * los enemigos que tocan al jugador y pueden golpear en este tick (0 si ninguno).
   */
  update(dt: number, player: PlayerTarget, world: WorldCollision): number {
    const knockDecay = Math.exp(-KNOCKBACK_DECAY * dt);
    let contactDamage = 0;
    const pos = this.pos;

    for (let i = 0; i < this.count; i++) {
      const def = ENEMY_LIST[this.type[i] as number];
      if (!def) continue;
      const x = this.x[i] as number;
      const z = this.z[i] as number;
      this.px[i] = x;
      this.py[i] = this.y[i] as number;
      this.pz[i] = z;

      // Dirección hacia el jugador (girada si está rodeando un obstáculo).
      const dx = player.x - x;
      const dz = player.z - z;
      const dist = Math.hypot(dx, dz) || 1e-4;
      let dirX = dx / dist;
      let dirZ = dz / dist;
      if ((this.detourTime[i] as number) > 0) {
        this.detourTime[i] = (this.detourTime[i] as number) - dt;
        const a = DETOUR_ANGLE * (this.detour[i] as number);
        const c = Math.cos(a);
        const s = Math.sin(a);
        const rx = dirX * c - dirZ * s;
        dirZ = dirX * s + dirZ * c;
        dirX = rx;
      }

      // Separación: se apartan de los vecinos con los que se solapan.
      let sepX = 0;
      let sepZ = 0;
      const n = this.grid.queryRadius(x, z, NEIGHBOR_RADIUS, this.neighbors);
      for (let k = 0; k < n; k++) {
        const j = this.neighbors[k] as number;
        if (j === i || j >= this.count) continue;
        const ox = x - (this.x[j] as number);
        const oz = z - (this.z[j] as number);
        const d2 = ox * ox + oz * oz;
        const minD = def.radius + (ENEMY_LIST[this.type[j] as number]?.radius ?? def.radius);
        if (d2 >= minD * minD || d2 < 1e-8) continue;
        const d = Math.sqrt(d2);
        const push = (minD - d) / minD;
        sepX += (ox / d) * push;
        sepZ += (oz / d) * push;
      }

      const speed = this.speed[i] as number;
      const desiredX = dirX * speed + sepX * SEPARATION_SPEED;
      const desiredZ = dirZ * speed + sepZ * SEPARATION_SPEED;
      const a = Math.min(1, def.agility * dt);
      this.vx[i] = (this.vx[i] as number) + (desiredX - (this.vx[i] as number)) * a;
      this.vz[i] = (this.vz[i] as number) + (desiredZ - (this.vz[i] as number)) * a;

      const kx = (this.kx[i] as number) * knockDecay;
      const kz = (this.kz[i] as number) * knockDecay;
      this.kx[i] = kx;
      this.kz[i] = kz;
      pos.x = x + ((this.vx[i] as number) + kx) * dt;
      pos.z = z + ((this.vz[i] as number) + kz) * dt;

      // Obstáculos: si chocan un rato seguido, rodean por un lado fijo (según su id).
      if (world.pushOutCircle(pos, def.radius, this.y[i] as number, ENEMY_STEP)) {
        this.stuckTime[i] = (this.stuckTime[i] as number) + dt;
        if ((this.stuckTime[i] as number) > STUCK_TIME && (this.detourTime[i] as number) <= 0) {
          this.detour[i] = ((this.id[i] as number) & 1) === 0 ? 1 : -1;
          this.detourTime[i] = DETOUR_TIME;
          this.stuckTime[i] = 0;
        }
      } else {
        this.stuckTime[i] = Math.max(0, (this.stuckTime[i] as number) - dt);
      }
      world.clampInside(pos);

      // No atraviesan al jugador: se quedan pegados y le hacen daño (si está a su alcance en altura).
      const ex = pos.x - player.x;
      const ez = pos.z - player.z;
      const ed = Math.hypot(ex, ez);
      const minD = def.radius + player.radius;
      if (ed < minD && Math.abs(player.y - (this.y[i] as number)) < ENEMY_REACH_HEIGHT) {
        const k = ed > 1e-4 ? minD / ed : 0;
        pos.x = player.x + ex * k;
        pos.z = player.z + (ed > 1e-4 ? ez * k : minD);
        if ((this.attackCd[i] as number) <= 0) {
          contactDamage = Math.max(contactDamage, def.damage);
          this.attackCd[i] = ATTACK_COOLDOWN;
        }
      }
      this.attackCd[i] = Math.max(0, (this.attackCd[i] as number) - dt);
      this.flash[i] = Math.max(0, (this.flash[i] as number) - dt * 6);

      this.x[i] = pos.x;
      this.z[i] = pos.z;
      // Siguen el suelo, incluidas superficies bajas a las que pueden subir.
      this.y[i] = world.groundHeight(pos.x, pos.z, (this.y[i] as number) + ENEMY_STEP);
      const vx = this.vx[i] as number;
      const vz = this.vz[i] as number;
      if (vx * vx + vz * vz > 0.04) this.heading[i] = Math.atan2(-vx, -vz);
      this.phase[i] = (this.phase[i] as number) + dt * (4 + speed * 1.2);
    }
    this.rebuildGrid();
    return contactDamage;
  }
}
