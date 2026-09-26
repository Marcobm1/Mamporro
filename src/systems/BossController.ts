// Controlador del jefe (la Pelusa Madre): persigue un rato y lanza uno de sus
// tres ataques, siempre avisando antes (rodillo, culetazo y estornudo). Mueve al
// jefe a través de los estados del sistema de enemigos.
import type { Rng } from '../core/rng';
import { BOSS_ATTACKS, BOSS_CONFIG, ENEMY_LIST, type BossAttack } from '../data/enemies';
import { ENEMY_STATE, type EnemySystem } from './EnemySystem';

export type BossPhase = 'idle' | 'windup' | 'attack' | 'recover';

/** Lo que el jefe hace en la partida al atacar. */
export interface BossHooks {
  /** Culetazo en (x, z): daña al jugador si le pilla dentro. */
  slam(x: number, y: number, z: number, radius: number, damage: number): void;
  /** Bola de polvo desde (x, z) en la dirección dada. */
  dust(x: number, z: number, dirX: number, dirZ: number): void;
  /** Pelusa hija en (x, z). */
  minion(x: number, z: number): void;
}

export interface BossTarget {
  x: number;
  z: number;
}

export class BossController {
  phase: BossPhase = 'idle';
  attack: BossAttack | null = null;
  /** Tiempo que queda de la fase actual y cuánto duraba (para el progreso del aviso). */
  timer: number;
  phaseLength: number;
  enraged = false;
  private last: BossAttack | null = null;

  constructor(
    /** id (no índice) del jefe en el sistema de enemigos. */
    readonly enemyId: number,
    private readonly rng: Rng,
  ) {
    this.timer = this.phaseLength = this.idleTime();
  }

  /** Progreso del aviso actual (0 → 1), para dibujarlo. */
  get windupProgress(): number {
    return this.phase === 'windup' && this.phaseLength > 0 ? 1 - this.timer / this.phaseLength : 0;
  }

  private get speed(): number {
    return this.enraged ? BOSS_CONFIG.enrageSpeed : 1;
  }

  private idleTime(): number {
    const [min, max] = BOSS_CONFIG.idle;
    return this.rng.range(min, max) / this.speed;
  }

  private setPhase(phase: BossPhase, time: number): void {
    this.phase = phase;
    this.timer = this.phaseLength = time;
  }

  /** Avanza un tick. Devuelve false si el jefe ya no existe. */
  update(dt: number, enemies: EnemySystem, player: BossTarget, hooks: BossHooks): boolean {
    const i = enemies.indexOfId(this.enemyId);
    if (i < 0) return false;
    this.enraged = (enemies.hp[i] as number) <= (enemies.maxHp[i] as number) * BOSS_CONFIG.enrageAt;
    this.timer -= dt;

    switch (this.phase) {
      case 'idle':
        enemies.setState(i, ENEMY_STATE.move, 0);
        if (this.timer <= 0) this.startAttack(i, enemies, player);
        break;
      case 'windup':
        // Durante el aviso del estornudo y del culetazo sigue mirando al jugador.
        if (this.attack !== 'roll') this.aimAt(i, enemies, player);
        if (this.timer <= 0) this.execute(i, enemies, hooks);
        break;
      case 'attack':
        if (this.timer <= 0) this.recover(i, enemies);
        break;
      case 'recover':
        if (this.timer <= 0) {
          this.attack = null;
          this.setPhase('idle', this.idleTime());
          enemies.setState(i, ENEMY_STATE.move, 0);
        }
        break;
    }
    return true;
  }

  private aimAt(i: number, enemies: EnemySystem, player: BossTarget): void {
    const dx = player.x - (enemies.x[i] as number);
    const dz = player.z - (enemies.z[i] as number);
    const d = Math.hypot(dx, dz) || 1;
    enemies.aimX[i] = dx / d;
    enemies.aimZ[i] = dz / d;
  }

  private startAttack(i: number, enemies: EnemySystem, player: BossTarget): void {
    const options = BOSS_ATTACKS.filter((a) => a !== this.last);
    const attack = this.rng.pick(options);
    this.attack = attack;
    this.last = attack;
    // La dirección del rodillo se fija al empezar el aviso: da tiempo a apartarse.
    this.aimAt(i, enemies, player);
    const windup = BOSS_CONFIG[attack].windup / this.speed;
    this.setPhase('windup', windup);
    enemies.setState(i, ENEMY_STATE.windup, windup);
  }

  private execute(i: number, enemies: EnemySystem, hooks: BossHooks): void {
    const x = enemies.x[i] as number;
    const y = enemies.y[i] as number;
    const z = enemies.z[i] as number;
    switch (this.attack) {
      case 'roll': {
        const r = BOSS_CONFIG.roll;
        const time = r.length / r.speed;
        this.setPhase('attack', time);
        enemies.setState(i, ENEMY_STATE.dash, time);
        enemies.dashSpeed[i] = r.speed;
        enemies.hitDamage[i] = r.damage;
        return;
      }
      case 'slam':
        hooks.slam(x, y, z, BOSS_CONFIG.slam.radius, BOSS_CONFIG.slam.damage);
        break;
      case 'sneeze': {
        const s = BOSS_CONFIG.sneeze;
        const n = this.enraged ? s.enragedProjectiles : s.projectiles;
        const a0 = this.rng.next() * Math.PI * 2;
        const radius = ENEMY_LIST[enemies.type[i] as number]?.radius ?? 2;
        for (let k = 0; k < n; k++) {
          const a = a0 + (k / n) * Math.PI * 2;
          hooks.dust(x + Math.cos(a) * radius, z + Math.sin(a) * radius, Math.cos(a), Math.sin(a));
        }
        for (let k = 0; k < s.minions; k++) {
          const a = a0 + ((k + 0.5) / s.minions) * Math.PI * 2;
          hooks.minion(x + Math.cos(a) * (radius + 1.2), z + Math.sin(a) * (radius + 1.2));
        }
        break;
      }
      default:
        break;
    }
    this.recover(i, enemies);
  }

  private recover(i: number, enemies: EnemySystem): void {
    const def = ENEMY_LIST[enemies.type[i] as number];
    const time = BOSS_CONFIG.recover / this.speed;
    this.setPhase('recover', time);
    enemies.setState(i, ENEMY_STATE.recover, time);
    enemies.hitDamage[i] = def?.damage ?? 0;
  }
}
