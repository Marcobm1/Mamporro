// Una partida: tiempo, enemigos, armas, tomos, proyectiles, gemas, experiencia,
// vida y subidas de nivel con sus cartas. Es lógica pura (sin Three.js): el
// render recibe los sucesos a través de `RunEffects`.
import type { CharacterDef } from '../data/characters';
import { ENEMY_LIST } from '../data/enemies';
import { TOMES, type TomeId } from '../data/tomes';
import { LEVEL_UP_CONFIG } from '../data/upgrades';
import type { WeaponId } from '../data/weapons';
import type { PlayerBody } from '../entities/playerPhysics';
import { mitigate, rollDamage } from '../systems/damage';
import { EnemySystem } from '../systems/EnemySystem';
import { PickupSystem } from '../systems/PickupSystem';
import { crowdSlowFor, smoothCrowdSlow } from '../systems/crowd';
import {
  generateOffer,
  healCard,
  replacementCard,
  type BuildView,
  type OfferCard,
} from '../systems/levelup';
import { addExperience, xpToNextLevel, type LevelState } from '../systems/progression';
import { ProjectileSystem } from '../systems/ProjectileSystem';
import { SpawnSystem } from '../systems/SpawnSystem';
import { computePlayerStats, tomeBonuses, type PlayerStats, type TomeInstance } from '../systems/stats';
import { BEHAVIORS, createWeapon, refreshWeapon, type CombatContext, type WeaponInstance } from '../weapons';
import type { CombatPlayer, WeaponEffects } from '../weapons/types';
import type { WorldCollision } from '../world/WorldCollision';
import { Rng } from './rng';

export const ENEMY_CAPACITY = 800;
export const PROJECTILE_CAPACITY = 256;
export const GEM_CAPACITY = 600;
/** Radio del jugador para el contacto con enemigos (m). */
export const PLAYER_HIT_RADIUS = 0.45;
/** Invulnerabilidad tras recibir un golpe (s). */
export const INVULNERABILITY_TIME = 0.7;

/** Sucesos que el render y la interfaz convierten en efectos (partículas, números, avisos...). */
export interface RunEffects extends WeaponEffects {
  damageNumber(x: number, y: number, z: number, amount: number, critLevel: number): void;
  enemyKilled(x: number, y: number, z: number, type: number): void;
  enemySpawned(x: number, y: number, z: number): void;
  playerHit(damage: number): void;
  levelUp(level: number): void;
}

export const NO_EFFECTS: RunEffects = {
  damageNumber: () => {},
  enemyKilled: () => {},
  enemySpawned: () => {},
  playerHit: () => {},
  levelUp: () => {},
  arcSwing: () => {},
  chainZap: () => {},
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
  /** Frenado actual por atravesar enemigos (0 = nada; ver CROWD_CONFIG). */
  crowdSlow = 0;
  readonly progress: LevelState = { level: 1, xp: 0 };
  /** Estadísticas del jugador (el objeto se conserva; se recalcula al cambiar los tomos). */
  readonly stats: PlayerStats;
  readonly player: CombatPlayer = { x: 0, y: 0, z: 0, facing: 0, vx: 0, vz: 0, grounded: true };
  readonly enemies: EnemySystem;
  readonly projectiles = new ProjectileSystem(PROJECTILE_CAPACITY);
  readonly gems = new PickupSystem(GEM_CAPACITY);
  readonly weapons: WeaponInstance[] = [];
  readonly tomes: TomeInstance[] = [];
  /** Subidas de nivel pendientes de elegir carta. */
  pendingLevelUps = 0;
  /** Cartas de la subida de nivel abierta (null si no hay ninguna). */
  offer: OfferCard[] | null = null;
  rerolls: number = LEVEL_UP_CONFIG.rerolls;
  skips: number = LEVEL_UP_CONFIG.skips;
  banishes: number = LEVEL_UP_CONFIG.banishes;
  /** Armas y tomos descartados para el resto de la partida. */
  readonly banished = new Set<string>();
  private readonly spawner: SpawnSystem;
  /** Sorteo de las cartas, separado del combate: con la misma semilla salen las mismas. */
  private readonly offerRng: Rng;
  private readonly rng: Rng;
  private readonly ctx: CombatContext;
  /** Objetos reutilizados cada tick (sin crear basura). */
  private readonly view = { x: 0, z: 0, yaw: 0 };
  private readonly target = { x: 0, y: 0, z: 0, radius: PLAYER_HIT_RADIUS, vx: 0, vz: 0 };

  constructor(
    private readonly world: WorldCollision,
    seed: string,
    private readonly character: CharacterDef,
    private readonly fx: RunEffects = NO_EFFECTS,
  ) {
    const rng = new Rng(`${seed}/run`);
    this.rng = rng.derive('combat');
    this.offerRng = rng.derive('offers');
    this.stats = computePlayerStats(character, tomeBonuses(this.tomes), LEVEL_UP_CONFIG.baseChoices);
    this.hp = this.stats.maxHp;
    this.enemies = new EnemySystem(ENEMY_CAPACITY, world.heightfield.size);
    this.spawner = new SpawnSystem(rng.derive('spawn'), (x, y, z) => this.fx.enemySpawned(x, y, z));
    const random = (): number => this.rng.next();
    this.ctx = {
      enemies: this.enemies,
      projectiles: this.projectiles,
      rng: this.rng,
      player: this.player,
      fx: this.fx,
      damageEnemy: (e, weapon, pushX, pushZ) => this.damageEnemy(e, weapon, pushX, pushZ, random),
      groundHeight: (x, z, maxY) => world.groundHeight(x, z, maxY),
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
    const p = this.player;
    p.x = body.x;
    p.y = body.y;
    p.z = body.z;
    p.facing = body.facing;
    p.vx = body.vx;
    p.vz = body.vz;
    p.grounded = body.grounded;
    this.view.x = body.x;
    this.view.z = body.z;
    this.view.yaw = viewYaw;
    this.target.x = body.x;
    this.target.y = body.y;
    this.target.z = body.z;
    this.target.vx = body.vx;
    this.target.vz = body.vz;

    this.spawner.update(dt, this.minutes, this.enemies, this.view, this.world);
    const contact = this.enemies.update(dt, this.target, this.world);
    if (contact > 0) this.hurtPlayer(contact);
    this.crowdSlow = smoothCrowdSlow(this.crowdSlow, crowdSlowFor(this.enemies.playerPressure), dt);

    for (const weapon of this.weapons) BEHAVIORS[weapon.def.behavior].update(weapon, this.ctx, dt);
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
    enemies.flash[e] = Math.max(enemies.flash[e] as number, weapon.def.hitFlash);
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
    this.pendingLevelUps += gained;
  }

  /** Añade un arma si no se tiene ya y queda hueco. */
  addWeapon(id: WeaponId): WeaponInstance | null {
    if (this.weapons.some((w) => w.def.id === id) || this.weapons.length >= LEVEL_UP_CONFIG.maxWeapons) return null;
    const weapon = createWeapon(id, this.weapons.length, this.stats, this.enemies.capacity);
    this.weapons.push(weapon);
    return weapon;
  }

  // ------------------------------------------------------------ subida de nivel

  private get build(): BuildView {
    return { weapons: this.weapons, tomes: this.tomes, stats: this.stats, banished: this.banished };
  }

  /** Abre la siguiente subida de nivel pendiente (si no hay ya una abierta). */
  openLevelUp(): boolean {
    if (this.offer || this.pendingLevelUps <= 0) return false;
    this.offer = generateOffer(this.build, this.stats.choices, this.offerRng);
    return true;
  }

  /** Elige una carta; si quedan subidas pendientes, abre la siguiente. */
  choose(index: number): boolean {
    const card = this.offer?.[index];
    if (!card) return false;
    this.applyCard(card);
    this.closeLevelUp();
    return true;
  }

  /** Cambia todas las cartas por otras nuevas. */
  reroll(): boolean {
    if (!this.offer || this.rerolls <= 0) return false;
    this.rerolls--;
    this.offer = generateOffer(this.build, this.stats.choices, this.offerRng);
    return true;
  }

  /** Pasa esta subida de nivel sin elegir nada. */
  skip(): boolean {
    if (!this.offer || this.skips <= 0) return false;
    this.skips--;
    this.closeLevelUp();
    return true;
  }

  /** Quita esa arma o tomo del sorteo para el resto de la partida y pone otra carta en su hueco. */
  banish(index: number): boolean {
    const offer = this.offer;
    const card = offer?.[index];
    if (!offer || !card || card.key === null || this.banishes <= 0) return false;
    this.banishes--;
    this.banished.add(card.key);
    const rest = offer.filter((_, i) => i !== index);
    const replacement = replacementCard(this.build, rest, this.offerRng);
    if (replacement) offer[index] = replacement;
    else offer.splice(index, 1);
    if (offer.length === 0) offer.push(healCard());
    return true;
  }

  private closeLevelUp(): void {
    this.offer = null;
    this.pendingLevelUps = Math.max(0, this.pendingLevelUps - 1);
    this.openLevelUp();
  }

  private applyCard(card: OfferCard): void {
    switch (card.kind) {
      case 'newWeapon':
        this.addWeapon(card.weapon);
        break;
      case 'weaponUpgrade': {
        const weapon = this.weapons.find((w) => w.def.id === card.weapon);
        if (!weapon) break;
        for (const change of card.changes) weapon.bonus[change.stat] += change.amount;
        weapon.level++;
        refreshWeapon(weapon, this.stats);
        break;
      }
      case 'tome':
        this.addTomeLevel(card.tome, card.amounts);
        break;
      case 'heal':
        this.hp = Math.min(this.stats.maxHp, this.hp + this.stats.maxHp * card.amount);
        break;
    }
  }

  /** Sube un nivel el tomo (o lo añade, si hay hueco) sumando `amounts` a sus efectos. */
  addTomeLevel(id: TomeId, amounts: readonly number[]): TomeInstance | null {
    let tome = this.tomes.find((t) => t.def.id === id);
    if (!tome) {
      if (this.tomes.length >= LEVEL_UP_CONFIG.maxTomes) return null;
      const def = TOMES[id];
      tome = { def, level: 0, bonus: def.effects.map(() => 0) };
      this.tomes.push(tome);
    }
    tome.level++;
    amounts.forEach((amount, i) => {
      if (tome) tome.bonus[i] = (tome.bonus[i] ?? 0) + amount;
    });
    this.refreshStats();
    return tome;
  }

  /** Recalcula las estadísticas del jugador y de las armas (tras cambiar los tomos). */
  private refreshStats(): void {
    const oldMax = this.stats.maxHp;
    Object.assign(this.stats, computePlayerStats(this.character, tomeBonuses(this.tomes), LEVEL_UP_CONFIG.baseChoices));
    // La vida máxima que se gana llega también a la actual.
    if (this.stats.maxHp > oldMax) this.hp += this.stats.maxHp - oldMax;
    this.hp = Math.min(this.hp, this.stats.maxHp);
    for (const weapon of this.weapons) refreshWeapon(weapon, this.stats);
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

  /** Pruebas: añade un arma directamente (sin carta). */
  debugAddWeapon(id: WeaponId): boolean {
    this.cheated = true;
    return this.addWeapon(id) !== null;
  }

  /** Pruebas: sube un nivel Común un tomo directamente (sin carta). */
  debugAddTome(id: TomeId): boolean {
    this.cheated = true;
    return this.addTomeLevel(id, TOMES[id].effects.map((e) => e.amount)) !== null;
  }
}
