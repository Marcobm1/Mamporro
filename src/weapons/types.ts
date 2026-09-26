// Tipos compartidos por los comportamientos de las armas.
import type { Rng } from '../core/rng';
import type { WeaponDef, WeaponStats } from '../data/weapons';
import type { EnemySystem } from '../systems/EnemySystem';
import type { ProjectileSystem } from '../systems/ProjectileSystem';

export interface WeaponInstance {
  def: WeaponDef;
  /** Posición en la lista de armas de la partida (la usan los proyectiles). */
  slot: number;
  level: number;
  /** Estadísticas del arma con sus mejoras (sin las del jugador). */
  stats: WeaponStats;
  /** Estadísticas finales (arma + jugador). */
  effective: WeaponStats;
  /** Cuenta atrás hasta el siguiente disparo o pulso. */
  timer: number;
  /** Segundos desde el último pulso (para efectos visuales). */
  sincePulse: number;
  totalDamage: number;
  kills: number;
}

/** Lo que un arma necesita de la partida para funcionar. */
export interface CombatContext {
  readonly enemies: EnemySystem;
  readonly projectiles: ProjectileSystem;
  readonly rng: Rng;
  readonly player: { x: number; y: number; z: number };
  /** Aplica el daño del arma al enemigo, con empuje en la dirección dada. */
  damageEnemy(enemy: number, weapon: WeaponInstance, pushX: number, pushZ: number): void;
}

export type WeaponBehavior = (weapon: WeaponInstance, ctx: CombatContext, dt: number) => void;
