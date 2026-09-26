// Tipos compartidos por los comportamientos de las armas.
import type { Rng } from '../core/rng';
import type { WeaponDef, WeaponStats } from '../data/weapons';
import type { EnemySystem } from '../systems/EnemySystem';
import type { ProjectileSystem } from '../systems/ProjectileSystem';

/** Lo que un comportamiento necesita recordar entre ticks. */
export type BehaviorState = NoState | OrbitState | TrailState;

export interface NoState {
  kind: 'none';
}

/** Dentaduras en órbita: salen un rato, se recargan y vuelven a salir. */
export interface OrbitState {
  kind: 'orbit';
  /** Ángulo de giro actual (rad). */
  angle: number;
  /** ¿Están fuera? Si no, se están recargando. */
  active: boolean;
  /** Reloj propio (s) para el tiempo entre mordiscos a un mismo enemigo. */
  clock: number;
  /** id de enemigo → momento (según `clock`) en que se le puede volver a morder. */
  nextBite: Map<number, number>;
}

/** Rastro de charcos recién fregados, en arrays planos (los más viejos se reciclan). */
export interface TrailState {
  kind: 'trail';
  x: Float32Array;
  y: Float32Array;
  z: Float32Array;
  radius: Float32Array;
  age: Float32Array;
  life: Float32Array;
  count: number;
  /** Metros recorridos desde el último charco. */
  distance: number;
  lastX: number;
  lastZ: number;
  hasLast: boolean;
  /** Marca por índice de enemigo para no dañarle dos veces en el mismo pulso. */
  stamp: Uint32Array;
  pulse: number;
}

export interface WeaponInstance {
  def: WeaponDef;
  /** Posición en la lista de armas de la partida (la usan los proyectiles). */
  slot: number;
  level: number;
  /** Mejoras acumuladas por estadística (su significado, en WEAPON_UPGRADE_STEPS). */
  bonus: WeaponStats;
  /** Estadísticas del arma con sus mejoras (sin las del jugador). */
  stats: WeaponStats;
  /** Estadísticas finales (arma + jugador, con los topes aplicados). */
  effective: WeaponStats;
  /** Cuenta atrás hasta el siguiente disparo o pulso. */
  timer: number;
  /** Segundos desde el último pulso (para efectos visuales). */
  sincePulse: number;
  totalDamage: number;
  kills: number;
  state: BehaviorState;
}

/** Lo que las armas saben del jugador en cada tick. */
export interface CombatPlayer {
  x: number;
  y: number;
  z: number;
  /** Hacia dónde mira (rad, convenio de rotation.y de Three). */
  facing: number;
  vx: number;
  vz: number;
  grounded: boolean;
}

/** Efectos visuales de un instante que las armas piden al render. */
export interface WeaponEffects {
  arcSwing(x: number, y: number, z: number, angle: number, radius: number, halfAngle: number): void;
  /** Rayo entre varios puntos: `points` son x, y, z seguidos; `count` cuántos puntos hay. */
  chainZap(points: Float32Array, count: number): void;
}

/** Lo que un arma necesita de la partida para funcionar. */
export interface CombatContext {
  readonly enemies: EnemySystem;
  readonly projectiles: ProjectileSystem;
  readonly rng: Rng;
  readonly player: CombatPlayer;
  readonly fx: WeaponEffects;
  /** Aplica el daño del arma al enemigo, con empuje en la dirección dada. */
  damageEnemy(enemy: number, weapon: WeaponInstance, pushX: number, pushZ: number): void;
  /** Altura del suelo (terreno y superficies a las que se puede subir) bajo (x, z). */
  groundHeight(x: number, z: number, maxY: number): number;
}

export type WeaponUpdate = (weapon: WeaponInstance, ctx: CombatContext, dt: number) => void;

export interface WeaponBehavior {
  update: WeaponUpdate;
  /** Estado inicial (recibe la capacidad de enemigos por si necesita marcas por enemigo). */
  createState(enemyCapacity: number): BehaviorState;
}
