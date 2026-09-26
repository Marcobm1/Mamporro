// Una partida: tiempo, enemigos, armas, proyectiles, gemas, experiencia y vida.
// Es lógica pura (sin Three.js): el render recibe los sucesos a través de `RunEffects`.
import type { CharacterDef } from '../data/characters';
import { ENEMY_LIST } from '../data/enemies';
import type { WeaponId } from '../data/weapons';
import type { PlayerBody } from '../entities/playerPhysics';
import { mitigate, rollDamage } from '../systems/damage';
import { EnemySystem } from '../systems/EnemySystem';
import { GemSystem } from '../systems/GemSystem';
import { addExperience, xpToNextLevel, type LevelState } from '../systems/progression';
import { ProjectileSystem } from '../systems/ProjectileSystem';
import { SpawnSystem } from '../systems/SpawnSystem';
import { basePlayerStats, type PlayerStats } from '../systems/stats';
import { BEHAVIORS, createWeapon, type CombatContext, type WeaponInstance } from '../weapons';
import type { WorldCollision } from '../world/WorldCollision';
import { Rng } from './rng';

export const ENEMY_CAPACITY = 800;
export const PROJECTILE_CAPACITY = 256;
export const GEM_CAPACITY = 600;
export const MAX_WEAPONS = 4;
/** Radio del jugador para el contacto con enemigos (m). */
export const PLAYER_HIT_RADIUS = 0.45;
/** Invulnerabilidad tras recibir un golpe (s). */
export const INVULNERABILITY_TIME = 0.7;
/** Nivel al que se consigue la segunda arma (regla temporal hasta las ofertas del hito 3). */
export const SECOND_WEAPON_LEVEL = 2;

/** Sucesos que el render y la interfaz convierten en efectos (partículas, números, avisos...). */
export interface RunEffects {
  damageNumber(x: number, y: number, z: number, amount: number, critLevel: number): void;
  enemyKilled(x: number, y: number, z: number, type: number): void;
  enemySpawned(x: number, y: number, z: number): void;
  playerHit(damage: number): void;
  levelUp(level: number): void;
  weaponGained(weapon: WeaponInstance): void;
}

export const NO_EFFECTS: RunEffects = {
  damageNumber: () => {},
  enemyKilled: () => {},
  enemySpawned: () => {},
  playerHit: () => {},
  levelUp: () => {},
  weaponGained: () => {},
};

export class Run {
  /** Segundos de partida. */
  time = 0;
  kills = 0;
  hp: number;
  invulnerable = 0;
  /** Debug: el jugador no recibe daño. */
  invincible = false;
  /** Se han usado trucos de debug (la partida no contará para la meta del hito 5). */
  cheated = false;
  readonly progress: LevelState = { level: 1, xp: 0 };
  readonly stats: PlayerStats;
  readonly player = { x: 0, y: 0, z: 0 };
  readonly enemies: EnemySystem;
  readonly projectiles = new ProjectileSystem(PROJECTILE_CAPACITY);
  readonly gems = new GemSystem(GEM_CAPACITY);
  readonly weapons: WeaponInstance[] = [];
  private readonly spawner: SpawnSystem;
  private readonly rng: Rng;
  private readonly ctx: CombatContext;
  /** Objetos reutilizados cada tick (sin crear basura). */
  private readonly view = { x: 0, z: 0, yaw: 0 };
  private readonly target = { x: 0, y: 0, z: 0, radius: PLAYER_HIT_RADIUS };

  constructor(
    private readonly world: WorldCollision,
    seed: string,
    character: CharacterDef,
    private readonly fx: RunEffects = NO_EFFECTS,
  ) {
    const rng = new Rng(`${seed}/run`);
    this.rng = rng.derive('combat');
    this.stats = basePlayerStats(character);
    this.hp = this.stats.maxHp;
    this.enemies = new EnemySystem(ENEMY_CAPACITY, world.heightfield.size);
    this.spawner = new SpawnSystem(rng.derive('spawn'), (x, y, z) => this.fx.enemySpawned(x, y, z));
    const random = (): number => this.rng.next();
    this.ctx = {
      enemies: this.enemies,
      projectiles: this.projectiles,
      rng: this.rng,
      player: this.player,
      damageEnemy: (e, weapon, pushX, pushZ) => this.damageEnemy(e, weapon, pushX, pushZ, random),
    };
    this.addWeapon(character.startingWeapon);
  }

  get minutes(): number {
    return this.time / 60;
  }

  get dead(): boolean {
    return this.hp <= 0;
  }

  get level(): number {
    return this.progress.level;
  }

  /** Avanza la partida un paso de lógica. */
  update(dt: number, body: PlayerBody, viewYaw: number): void {
    if (this.dead) return;
    this.time += dt;
    this.player.x = body.x;
    this.player.y = body.y;
    this.player.z = body.z;
    this.view.x = body.x;
    this.view.z = body.z;
    this.view.yaw = viewYaw;
    this.target.x = body.x;
    this.target.y = body.y;
    this.target.z = body.z;

    this.spawner.update(dt, this.minutes, this.enemies, this.view, this.world);
    const contact = this.enemies.update(dt, this.target, this.world);
    if (contact > 0) this.hurtPlayer(contact);

    for (const weapon of this.weapons) BEHAVIORS[weapon.def.behavior](weapon, this.ctx, dt);
    this.projectiles.update(dt, this.enemies, this.world.heightfield, (p, e) => {
      const weapon = this.weapons[this.projectiles.weapon[p] as number];
      if (!weapon) return;
      const vx = this.projectiles.vx[p] as number;
      const vz = this.projectiles.vz[p] as number;
      const len = Math.hypot(vx, vz) || 1;
      this.ctx.damageEnemy(e, weapon, vx / len, vz / len);
    });
    this.enemies.flushDead((i) => this.onEnemyDeath(i));

    const xp = this.gems.update(dt, body.x, body.y, body.z, this.stats.pickupRadius);
    if (xp > 0) this.gainExperience(xp);

    this.invulnerable = Math.max(0, this.invulnerable - dt);
    if (this.stats.regen > 0) this.hp = Math.min(this.stats.maxHp, this.hp + this.stats.regen * dt);
  }

  private damageEnemy(e: number, weapon: WeaponInstance, pushX: number, pushZ: number, random: () => number): void {
    const enemies = this.enemies;
    const def = ENEMY_LIST[enemies.type[e] as number];
    if (!def) return;
    const s = weapon.effective;
    const roll = rollDamage(s.damage, s.critChance, s.critMultiplier, random);
    const before = enemies.hp[e] as number;
    enemies.hp[e] = before - roll.amount;
    enemies.flash[e] = 1;
    enemies.kx[e] = (enemies.kx[e] as number) + (pushX * s.knockback) / def.mass;
    enemies.kz[e] = (enemies.kz[e] as number) + (pushZ * s.knockback) / def.mass;
    weapon.totalDamage += Math.min(before, roll.amount);
    if (before > 0 && (enemies.hp[e] as number) <= 0) weapon.kills++;
    this.fx.damageNumber(
      enemies.x[e] as number,
      (enemies.y[e] as number) + def.height + 0.35,
      enemies.z[e] as number,
      roll.amount,
      roll.critLevel,
    );
  }

  private onEnemyDeath(i: number): void {
    const e = this.enemies;
    this.kills++;
    this.gems.spawn(e.x[i] as number, (e.y[i] as number) + 0.4, e.z[i] as number, e.xp[i] as number);
    this.fx.enemyKilled(e.x[i] as number, e.y[i] as number, e.z[i] as number, e.type[i] as number);
  }

  private hurtPlayer(amount: number): void {
    if (this.invincible || this.invulnerable > 0) return;
    const damage = mitigate(amount, this.stats.armor);
    this.hp = Math.max(0, this.hp - damage);
    this.invulnerable = INVULNERABILITY_TIME;
    this.fx.playerHit(damage);
  }

  private gainExperience(amount: number): void {
    const before = this.progress.level;
    const gained = addExperience(this.progress, amount * this.stats.xpGain);
    for (let k = 1; k <= gained; k++) this.fx.levelUp(before + k);
    // Regla temporal del hito 2: la segunda arma llega sola al subir de nivel.
    if (this.progress.level >= SECOND_WEAPON_LEVEL) this.addWeapon('naftalina');
  }

  /** Añade un arma si no se tiene ya y queda hueco. */
  addWeapon(id: WeaponId): WeaponInstance | null {
    if (this.weapons.some((w) => w.def.id === id) || this.weapons.length >= MAX_WEAPONS) return null;
    const weapon = createWeapon(id, this.weapons.length, this.stats);
    this.weapons.push(weapon);
    this.fx.weaponGained(weapon);
    return weapon;
  }

  // ------------------------------------------------------------ debug

  debugToggleInvincible(): boolean {
    this.cheated = true;
    this.invincible = !this.invincible;
    return this.invincible;
  }

  debugLevelUp(): void {
    this.cheated = true;
    this.gainExperience(xpToNextLevel(this.progress.level) - this.progress.xp);
  }

  debugSkipMinute(): void {
    this.cheated = true;
    this.time += 60;
  }

  debugSpawn(count: number): void {
    this.cheated = true;
    this.spawner.spawnBurst(count, this.minutes, this.enemies, this.view, this.world);
    this.enemies.rebuildGrid();
  }

  debugKillAll(): void {
    this.cheated = true;
    this.enemies.hp.fill(0, 0, this.enemies.count);
    this.enemies.flushDead((i) => this.onEnemyDeath(i));
  }
}
