// Enemigos en arrays planos (estructura de arrays): cientos de enemigos sin crear
// objetos por frame. Persiguen al jugador (o mantienen la distancia y disparan),
// se separan entre sí, rodean obstáculos cuando se atascan, embisten tras avisar y
// hacen daño por contacto. Al jefe lo dirige systems/BossController.ts.
import { ENEMY_LIST, type EnemyDef } from '../data/enemies';
import type { WorldCollision } from '../world/WorldCollision';
import { SpatialGrid } from './SpatialGrid';

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
/** Margen alrededor de la distancia preferida en la que un tirador se mueve de lado. */
const KITE_BAND = 1.5;
/** Más cerca que esto no embiste (ya está encima: muerde). */
const MIN_CHARGE_DISTANCE = 4;
/** A partir de esta masa, el enemigo no se aparta: es el jugador quien rebota (el jefe). */
const PLAYER_PUSH_MASS = 20;
/** Altura que un enemigo sube sin problema (plataformas bajas, escalones). */
export const ENEMY_STEP = 0.6;
/**
 * Diferencia de altura hasta la que un enemigo alcanza al jugador (salta a
 * morder): subirse a una roca o a unas cajas no te pone a salvo.
 */
export const ENEMY_REACH_HEIGHT = 1.6;

/**
 * Enemigos "pequeños": todos menos los enormes (el jefe). Las búsquedas por radio
 * amplían el radio con el mayor de ellos; los enormes se miran aparte (hay pocos).
 */
const SMALL_ENEMY_RADIUS = Math.max(...ENEMY_LIST.filter((d) => d.radius <= 1).map((d) => d.radius));
const MAX_BIG = 8;

/** Qué está haciendo cada enemigo. */
export const ENEMY_STATE = {
  /** Persiguiendo (o colocándose para disparar). */
  move: 0,
  /** Avisando de un ataque: quieto, mirando a donde va a atacar. */
  windup: 1,
  /** Embistiendo en línea recta. */
  dash: 2,
  /** Recuperándose tras atacar: quieto. */
  recover: 3,
} as const;

export interface PlayerTarget {
  x: number;
  /** Altura de los pies del jugador. */
  y: number;
  z: number;
  radius: number;
  /** Velocidad horizontal del jugador (para saber a quién empuja de frente). */
  vx: number;
  vz: number;
}

/** Lo que los enemigos piden a la partida. */
export interface EnemyActions {
  /** El enemigo `i` (un tirador) dispara en la dirección (dirX, dirZ). */
  shoot(i: number, dirX: number, dirZ: number): void;
}

const NO_ACTIONS: EnemyActions = { shoot: () => {} };

export class EnemySystem {
  count = 0;
  /**
   * Empuje del último `update`: masa de los enemigos que el jugador aparta de
   * frente (1 = una pelusa justo delante). Con él se calcula cuánto le frenan.
   */
  playerPressure = 0;
  /** Desplazamiento del jugador del último `update` por chocar con enemigos enormes. */
  playerPushX = 0;
  playerPushZ = 0;
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
  /** Multiplicador del oro que suelta (tótems, ritmo de la partida). */
  readonly gold: Float32Array;
  readonly speed: Float32Array;
  readonly flash: Float32Array;
  /** Ralentización (0..1: fracción de velocidad perdida) y cuánto le queda (s). */
  readonly slow: Float32Array;
  readonly slowTime: Float32Array;
  readonly attackCd: Float32Array;
  readonly stuckTime: Float32Array;
  readonly detourTime: Float32Array;
  readonly heading: Float32Array;
  readonly phase: Float32Array;
  /** Tiempo que le queda en su estado (aviso, embestida, recuperación). */
  readonly stateTime: Float32Array;
  /** Recarga de su ataque especial (disparo o embestida). */
  readonly actionCd: Float32Array;
  /** Dirección de su ataque (unitaria). */
  readonly aimX: Float32Array;
  readonly aimZ: Float32Array;
  readonly dashSpeed: Float32Array;
  /** Daño que hace al tocar al jugador ahora mismo (más al embestir). */
  readonly hitDamage: Float32Array;
  readonly detour: Int8Array;
  readonly state: Uint8Array;
  readonly type: Uint8Array;
  /** Identificador único (los índices cambian al eliminar; el id no). */
  readonly id: Uint32Array;

  readonly grid: SpatialGrid;
  /** Índices de los enemigos enormes (se recalcula con la rejilla). */
  readonly bigs = new Int32Array(MAX_BIG);
  bigCount = 0;
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
    this.gold = f();
    this.speed = f();
    this.flash = f();
    this.slow = f();
    this.slowTime = f();
    this.attackCd = f();
    this.stuckTime = f();
    this.detourTime = f();
    this.heading = f();
    this.phase = f();
    this.stateTime = f();
    this.actionCd = f();
    this.aimX = f();
    this.aimZ = f();
    this.dashSpeed = f();
    this.hitDamage = f();
    this.detour = new Int8Array(capacity);
    this.state = new Uint8Array(capacity);
    this.type = new Uint8Array(capacity);
    this.id = new Uint32Array(capacity);
    this.grid = new SpatialGrid(worldSize, 4, capacity);
    this.neighbors = new Int32Array(64);
    this.floatArrays = [
      this.x, this.y, this.z, this.px, this.py, this.pz, this.vx, this.vz, this.kx, this.kz,
      this.hp, this.maxHp, this.xp, this.gold, this.speed, this.flash, this.slow, this.slowTime, this.attackCd,
      this.stuckTime, this.detourTime, this.heading, this.phase, this.stateTime, this.actionCd, this.aimX, this.aimZ,
      this.dashSpeed, this.hitDamage,
    ];
  }

  /** Crea un enemigo y devuelve su índice (o -1 si no cabe). */
  spawn(typeIndex: number, x: number, y: number, z: number, hpMultiplier: number, xpMultiplier: number, goldMultiplier = 1): number {
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
    this.gold[i] = goldMultiplier;
    this.speed[i] = def.speed * variation;
    this.flash[i] = 0;
    this.slow[i] = this.slowTime[i] = 0;
    this.attackCd[i] = 0;
    this.stuckTime[i] = this.detourTime[i] = 0;
    this.detour[i] = 0;
    this.heading[i] = 0;
    this.phase[i] = (id * 0.618) % 1 * Math.PI * 2;
    this.state[i] = ENEMY_STATE.move;
    this.stateTime[i] = 0;
    // El primer disparo llega tras una recarga repartida (para que no disparen a la vez);
    // la primera embestida, pronto: el élite avisa en cuanto se acerca.
    this.actionCd[i] = def.charge ? def.charge.cooldown * 0.25 : (def.ranged?.cooldown ?? 0) * (0.5 + (variation - 0.9) * 2.5);
    this.aimX[i] = 0;
    this.aimZ[i] = -1;
    this.dashSpeed[i] = 0;
    this.hitDamage[i] = def.damage;
    this.type[i] = typeIndex;
    this.id[i] = id;
    return i;
  }

  radiusOf(i: number): number {
    return ENEMY_LIST[this.type[i] as number]?.radius ?? 0.5;
  }

  /** Ralentiza al enemigo `i` (se queda con la mayor de las ralentizaciones activas). */
  applySlow(i: number, amount: number, seconds: number): void {
    // Una fuente débil no prolonga indefinidamente una ralentización más fuerte.
    if ((this.slowTime[i] as number) > 0 && (this.slow[i] as number) > amount + 1e-6) return;
    this.slow[i] = Math.max(this.slow[i] as number, amount);
    this.slowTime[i] = Math.max(this.slowTime[i] as number, seconds);
  }

  /** Mueve un enemigo a otra posición al instante (reaparecer cerca del jugador). */
  relocate(i: number, x: number, y: number, z: number): void {
    this.x[i] = this.px[i] = x;
    this.y[i] = this.py[i] = y;
    this.z[i] = this.pz[i] = z;
    this.vx[i] = this.vz[i] = this.kx[i] = this.kz[i] = 0;
  }

  /** Cambia el estado de un enemigo (lo usa también el controlador del jefe). */
  setState(i: number, state: number, time: number): void {
    this.state[i] = state;
    this.stateTime[i] = time;
  }

  /** Elimina el enemigo `i` moviendo el último a su hueco (O(1)). */
  remove(i: number): void {
    const last = --this.count;
    if (i === last) return;
    for (const a of this.floatArrays) a[i] = a[last] as number;
    this.detour[i] = this.detour[last] as number;
    this.state[i] = this.state[last] as number;
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
    this.bigCount = 0;
    for (let i = 0; i < this.count && this.bigCount < MAX_BIG; i++) {
      if (this.radiusOf(i) > SMALL_ENEMY_RADIUS) this.bigs[this.bigCount++] = i;
    }
  }

  /**
   * Enemigos que pueden tocar un círculo de radio `radius` en (x, z): los de la
   * rejilla (ampliada con el radio de los pequeños) y los enormes que llegan.
   * Quien llama comprueba la distancia exacta con `radiusOf`.
   */
  queryRadius(x: number, z: number, radius: number, out: Int32Array): number {
    const pad = radius + SMALL_ENEMY_RADIUS;
    let n = this.grid.queryRadius(x, z, pad, out);
    for (let k = 0; k < this.bigCount && n < out.length; k++) {
      const b = this.bigs[k] as number;
      const d = Math.hypot((this.x[b] as number) - x, (this.z[b] as number) - z);
      // Los que ya están dentro de `pad` los ha devuelto la rejilla.
      if (d > pad && d <= radius + this.radiusOf(b)) out[n++] = b;
    }
    return n;
  }

  /** Índice del enemigo con ese id (o -1). Lineal: para buscar uno concreto (el jefe). */
  indexOfId(id: number): number {
    for (let i = 0; i < this.count; i++) if (this.id[i] === id) return i;
    return -1;
  }

  /**
   * Mueve a todos los enemigos un paso. Devuelve el daño de contacto más alto de
   * los enemigos que tocan al jugador y pueden golpear en este tick (0 si ninguno).
   */
  update(dt: number, player: PlayerTarget, world: WorldCollision, actions: EnemyActions = NO_ACTIONS): number {
    const knockDecay = Math.exp(-KNOCKBACK_DECAY * dt);
    let contactDamage = 0;
    let pressure = 0;
    let pushX = 0;
    let pushZ = 0;
    const playerSpeed = Math.hypot(player.vx, player.vz);
    const pos = this.pos;

    for (let i = 0; i < this.count; i++) {
      const def = ENEMY_LIST[this.type[i] as number];
      if (!def) continue;
      const x = this.x[i] as number;
      const z = this.z[i] as number;
      this.px[i] = x;
      this.py[i] = this.y[i] as number;
      this.pz[i] = z;

      // Dirección hacia el jugador.
      const dx = player.x - x;
      const dz = player.z - z;
      const dist = Math.hypot(dx, dz) || 1e-4;
      const toX = dx / dist;
      const toZ = dz / dist;
      const state = this.think(i, def, dist, toX, toZ, Math.abs(player.y - (this.y[i] as number)), dt, actions);
      const still = state === ENEMY_STATE.windup || state === ENEMY_STATE.recover;
      const dashing = state === ENEMY_STATE.dash;

      // Hacia dónde quiere ir (los tiradores se quedan a su distancia), girada si rodea un obstáculo.
      let dirX = toX;
      let dirZ = toZ;
      if (def.behavior === 'ranged' && def.ranged) {
        const preferred = def.ranged.preferred;
        if (dist < preferred - KITE_BAND) {
          dirX = -toX;
          dirZ = -toZ;
        } else if (dist <= preferred + KITE_BAND) {
          // De lado (cada uno hacia un lado fijo), corrigiendo un poco la distancia.
          const side = ((this.id[i] as number) & 1) === 0 ? 1 : -1;
          const k = ((dist - preferred) / KITE_BAND) * 0.4;
          dirX = -toZ * side + toX * k;
          dirZ = toX * side + toZ * k;
          const l = Math.hypot(dirX, dirZ) || 1;
          dirX /= l;
          dirZ /= l;
        }
      }
      if ((this.detourTime[i] as number) > 0) {
        this.detourTime[i] = (this.detourTime[i] as number) - dt;
        const a = DETOUR_ANGLE * (this.detour[i] as number);
        const c = Math.cos(a);
        const s = Math.sin(a);
        const rx = dirX * c - dirZ * s;
        dirZ = dirX * s + dirZ * c;
        dirX = rx;
      }

      // Separación: se apartan de los vecinos con los que se solapan (los ligeros más que los pesados).
      let sepX = 0;
      let sepZ = 0;
      if (!dashing) {
        const small = def.radius <= SMALL_ENEMY_RADIUS;
        const n = this.grid.queryRadius(x, z, def.radius + SMALL_ENEMY_RADIUS, this.neighbors);
        for (let k = 0; k < n; k++) {
          const j = this.neighbors[k] as number;
          if (j === i || j >= this.count) continue;
          const other = ENEMY_LIST[this.type[j] as number];
          // Los enormes se miran después (la rejilla no llega a todos los que tocan).
          if (!other || other.radius > SMALL_ENEMY_RADIUS) continue;
          const push = separation(x - (this.x[j] as number), z - (this.z[j] as number), def, other);
          sepX += push.x;
          sepZ += push.z;
        }
        for (let k = 0; k < this.bigCount; k++) {
          const j = this.bigs[k] as number;
          const other = ENEMY_LIST[this.type[j] as number];
          if (j === i || j >= this.count || !other || (!small && other.mass < def.mass)) continue;
          const push = separation(x - (this.x[j] as number), z - (this.z[j] as number), def, other);
          sepX += push.x;
          sepZ += push.z;
        }
      }

      let speed = this.speed[i] as number;
      if ((this.slowTime[i] as number) > 0) {
        speed *= 1 - (this.slow[i] as number);
        this.slowTime[i] = (this.slowTime[i] as number) - dt;
        if ((this.slowTime[i] as number) <= 0) this.slow[i] = 0;
      }
      if (dashing) {
        // Embestida: en línea recta y sin frenar.
        this.vx[i] = (this.aimX[i] as number) * (this.dashSpeed[i] as number);
        this.vz[i] = (this.aimZ[i] as number) * (this.dashSpeed[i] as number);
      } else {
        const desiredX = (still ? 0 : dirX * speed) + sepX * SEPARATION_SPEED;
        const desiredZ = (still ? 0 : dirZ * speed) + sepZ * SEPARATION_SPEED;
        const a = Math.min(1, def.agility * (still ? 2 : 1) * dt);
        this.vx[i] = (this.vx[i] as number) + (desiredX - (this.vx[i] as number)) * a;
        this.vz[i] = (this.vz[i] as number) + (desiredZ - (this.vz[i] as number)) * a;
      }

      const kx = (this.kx[i] as number) * knockDecay;
      const kz = (this.kz[i] as number) * knockDecay;
      this.kx[i] = kx;
      this.kz[i] = kz;
      pos.x = x + ((this.vx[i] as number) + kx) * dt;
      pos.z = z + ((this.vz[i] as number) + kz) * dt;

      // Obstáculos: si chocan un rato seguido, rodean por un lado fijo (según su id).
      // Una embestida que choca contra algo se acaba.
      if (world.pushOutCircle(pos, def.radius, this.y[i] as number, ENEMY_STEP)) {
        if (dashing && def.behavior === 'charger') this.endDash(i, def);
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

      // No atraviesan al jugador: los aparta (y le frenan si van de frente), se
      // quedan pegados y le hacen daño si está a su alcance en altura. A los
      // enormes no los mueve: es el jugador quien rebota.
      const ex = pos.x - player.x;
      const ez = pos.z - player.z;
      const ed = Math.hypot(ex, ez);
      const minD = def.radius + player.radius;
      if (ed < minD && Math.abs(player.y - (this.y[i] as number)) < ENEMY_REACH_HEIGHT + (def.height > 3 ? def.height * 0.5 : 0)) {
        const k = ed > 1e-4 ? minD / ed : 0;
        if (def.mass >= PLAYER_PUSH_MASS) {
          const overlap = minD - ed;
          pushX -= (ed > 1e-4 ? ex / ed : 0) * overlap;
          pushZ -= (ed > 1e-4 ? ez / ed : -1) * overlap;
        } else {
          pos.x = player.x + ex * k;
          pos.z = player.z + (ed > 1e-4 ? ez * k : minD);
        }
        if (playerSpeed > 0.1 && ed > 1e-4) {
          // 1 si está justo delante de hacia donde va el jugador, 0 de lado o detrás.
          const ahead = (ex * player.vx + ez * player.vz) / (ed * playerSpeed);
          if (ahead > 0) pressure += ahead * def.mass;
        }
        if ((this.attackCd[i] as number) <= 0) {
          contactDamage = Math.max(contactDamage, this.hitDamage[i] as number);
          this.attackCd[i] = ATTACK_COOLDOWN;
        }
      }
      this.attackCd[i] = Math.max(0, (this.attackCd[i] as number) - dt);
      this.flash[i] = Math.max(0, (this.flash[i] as number) - dt * 6);

      this.x[i] = pos.x;
      this.z[i] = pos.z;
      // Siguen el suelo, incluidas superficies bajas a las que pueden subir.
      this.y[i] = world.groundHeight(pos.x, pos.z, (this.y[i] as number) + ENEMY_STEP);
      if (still || dashing) {
        this.heading[i] = Math.atan2(-(this.aimX[i] as number), -(this.aimZ[i] as number));
      } else {
        const vx = this.vx[i] as number;
        const vz = this.vz[i] as number;
        if (vx * vx + vz * vz > 0.04) this.heading[i] = Math.atan2(-vx, -vz);
      }
      this.phase[i] = (this.phase[i] as number) + dt * (4 + speed * 1.2);
    }
    this.rebuildGrid();
    this.playerPressure = pressure;
    this.playerPushX = pushX;
    this.playerPushZ = pushZ;
    return contactDamage;
  }

  /**
   * Decide los ataques especiales de tiradores y embestidores (el jefe tiene su
   * propio controlador) y devuelve el estado con el que se mueve en este tick.
   */
  private think(i: number, def: EnemyDef, dist: number, toX: number, toZ: number, dy: number, dt: number, actions: EnemyActions): number {
    const state = this.state[i] as number;
    if (def.behavior === 'ranged' && def.ranged) {
      const r = def.ranged;
      if (state === ENEMY_STATE.move) {
        this.actionCd[i] = (this.actionCd[i] as number) - dt;
        if ((this.actionCd[i] as number) <= 0 && dist <= r.range && dy < 3) this.setState(i, ENEMY_STATE.windup, r.windup);
      } else if (state === ENEMY_STATE.windup) {
        // Apunta durante todo el aviso y dispara al acabarlo.
        this.aimX[i] = toX;
        this.aimZ[i] = toZ;
        this.stateTime[i] = (this.stateTime[i] as number) - dt;
        if ((this.stateTime[i] as number) <= 0) {
          actions.shoot(i, toX, toZ);
          this.setState(i, ENEMY_STATE.move, 0);
          this.actionCd[i] = r.cooldown * (0.85 + (((this.id[i] as number) * 7) % 10) * 0.03);
        }
      }
    } else if (def.behavior === 'charger' && def.charge) {
      const c = def.charge;
      if (state === ENEMY_STATE.move) {
        this.actionCd[i] = (this.actionCd[i] as number) - dt;
        if ((this.actionCd[i] as number) <= 0 && dist < c.range && dist > MIN_CHARGE_DISTANCE && dy < 2) {
          // La dirección se fija al empezar el aviso: se puede esquivar apartándose.
          this.aimX[i] = toX;
          this.aimZ[i] = toZ;
          this.setState(i, ENEMY_STATE.windup, c.windup);
        }
      } else {
        this.stateTime[i] = (this.stateTime[i] as number) - dt;
        if ((this.stateTime[i] as number) <= 0) {
          if (state === ENEMY_STATE.windup) {
            this.setState(i, ENEMY_STATE.dash, c.dashTime);
            this.dashSpeed[i] = c.dashSpeed;
            this.hitDamage[i] = def.damage * c.damageMultiplier;
          } else if (state === ENEMY_STATE.dash) {
            this.endDash(i, def);
          } else {
            this.setState(i, ENEMY_STATE.move, 0);
            this.actionCd[i] = c.cooldown;
          }
        }
      }
    }
    return this.state[i] as number;
  }

  private endDash(i: number, def: EnemyDef): void {
    this.setState(i, ENEMY_STATE.recover, def.charge?.recover ?? 0.6);
    this.hitDamage[i] = def.damage;
  }
}

const sepOut = { x: 0, z: 0 };

/** Empuje de separación de `a` respecto a `b` (separados por ox, oz): el ligero se aparta más. */
function separation(ox: number, oz: number, a: EnemyDef, b: EnemyDef): { x: number; z: number } {
  sepOut.x = 0;
  sepOut.z = 0;
  const d2 = ox * ox + oz * oz;
  const minD = a.radius + b.radius;
  if (d2 >= minD * minD || d2 < 1e-8) return sepOut;
  const d = Math.sqrt(d2);
  const push = ((minD - d) / minD) * ((2 * b.mass) / (a.mass + b.mass));
  sepOut.x = (ox / d) * push;
  sepOut.z = (oz / d) * push;
  return sepOut;
}
