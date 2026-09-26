// Una partida: tiempo y dificultad, enemigos, armas, tomos, objetos, oro,
// proyectiles, gemas, experiencia, vida, subidas de nivel, interactuables del
// mapa, oleadas, élites, enjambre final y jefe. Es lógica pura (sin Three.js): el
// render y la interfaz reciben los sucesos a través de `RunEffects`.
import type { CharacterDef } from '../data/characters';
import { BOSS_CONFIG, ENEMY_LIST, enemyTypeIndex, type EnemyId } from '../data/enemies';
import { ITEM_EFFECTS, ITEMS, type ItemDef, type ItemId } from '../data/items';
import { chestCost, GOLD_CONFIG, PORTAL_CONFIG, SHRINE_CONFIG, TOTEM_CONFIG } from '../data/run';
import { TOMES, type TomeId } from '../data/tomes';
import { LEVEL_UP_CONFIG } from '../data/upgrades';
import { ELITE_SCHEDULE, REFERENCE_MINUTES, type RunMinutes } from '../data/waves';
import type { WeaponId } from '../data/weapons';
import type { PlayerBody } from '../entities/playerPhysics';
import type { TranslationKey } from '../i18n';
import { BossController, type BossHooks } from '../systems/BossController';
import { crowdSlowFor, smoothCrowdSlow } from '../systems/crowd';
import { mitigate, rollDamage } from '../systems/damage';
import { Director, type DirectorEvents, type SpawnModifiers } from '../systems/Director';
import { EnemyProjectileSystem, PROJECTILE_DUST, PROJECTILE_PIPA } from '../systems/EnemyProjectileSystem';
import { EnemySystem, type EnemyActions } from '../systems/EnemySystem';
import { Interactables, type InteractableEvents, type InteractableState, type InteractPrompt } from '../systems/Interactables';
import { itemBonuses, itemCount, ollaChance, pearlChance, purseBonus, rollItem, type ItemStack } from '../systems/items';
import {
  generateOffer,
  generateShrineOffer,
  healCard,
  replacementCard,
  type BuildView,
  type OfferCard,
} from '../systems/levelup';
import { PickupSystem } from '../systems/PickupSystem';
import { addExperience, xpToNextLevel, type LevelState } from '../systems/progression';
import { ProjectileSystem } from '../systems/ProjectileSystem';
import { SpawnSystem, type SpawnParams } from '../systems/SpawnSystem';
import { computePlayerStats, tomeBonuses, type AppliedBonus, type PlayerStats, type TomeInstance } from '../systems/stats';
import { BEHAVIORS, createWeapon, refreshWeapon, type CombatContext, type WeaponInstance } from '../weapons';
import type { CombatPlayer, WeaponEffects } from '../weapons/types';
import type { InteractableSpot } from '../world/interactables';
import type { WorldCollision } from '../world/WorldCollision';
import { Rng } from './rng';

export const ENEMY_CAPACITY = 800;
export const PROJECTILE_CAPACITY = 256;
export const ENEMY_SHOT_CAPACITY = 256;
export const GEM_CAPACITY = 600;
/** Radio del jugador para el contacto con enemigos (m). */
export const PLAYER_HIT_RADIUS = 0.45;
/** Invulnerabilidad tras recibir un golpe (s). */
export const INVULNERABILITY_TIME = 0.7;
/** Rondas de explosiones de olla encadenadas que se resuelven en un tick. */
const MAX_BLAST_ROUNDS = 6;
/** Distancia a la que aparece el jefe invocado con el debug. */
const DEBUG_BOSS_DISTANCE = 14;

/** Avisos de la partida (la interfaz pone el texto). */
export type RunNotice =
  | { kind: 'wave'; key: TranslationKey }
  | { kind: 'elite'; enemy: EnemyId }
  | { kind: 'swarm' }
  | { kind: 'portalFound' }
  | { kind: 'portalRevealed' }
  | { kind: 'boss'; enemy: EnemyId }
  | { kind: 'noGold'; missing: number }
  | { kind: 'challengeStart' }
  | { kind: 'challengeDone' }
  | { kind: 'shrineCharged' }
  | { kind: 'revive' };

/** Sucesos que el render y la interfaz convierten en efectos (partículas, números, avisos...). */
export interface RunEffects extends WeaponEffects {
  damageNumber(x: number, y: number, z: number, amount: number, critLevel: number): void;
  enemyKilled(x: number, y: number, z: number, type: number): void;
  enemySpawned(x: number, y: number, z: number): void;
  playerHit(damage: number): void;
  levelUp(level: number): void;
  notice(notice: RunNotice): void;
  itemGained(item: ItemDef): void;
  chestOpened(x: number, y: number, z: number): void;
  /** Una paloma escupe una pipa. */
  enemyShot(x: number, y: number, z: number): void;
  bossSpawned(x: number, y: number, z: number): void;
  bossSlam(x: number, y: number, z: number, radius: number): void;
  /** Explosión de una olla exprés. */
  explosion(x: number, y: number, z: number, radius: number): void;
  /** Una perla salta de un enemigo a otro. */
  pearl(x1: number, y1: number, z1: number, x2: number, y2: number, z2: number): void;
  /** La bata te salva: onda que aparta a los enemigos. */
  revive(x: number, y: number, z: number, radius: number): void;
}

export const NO_EFFECTS: RunEffects = {
  damageNumber: () => {},
  enemyKilled: () => {},
  enemySpawned: () => {},
  playerHit: () => {},
  levelUp: () => {},
  notice: () => {},
  itemGained: () => {},
  chestOpened: () => {},
  enemyShot: () => {},
  bossSpawned: () => {},
  bossSlam: () => {},
  explosion: () => {},
  pearl: () => {},
  revive: () => {},
  arcSwing: () => {},
  chainZap: () => {},
};

export interface RunOptions {
  /** Duración de la partida (minutos). */
  minutes?: RunMinutes;
  /** Interactuables del mapa. */
  interactables?: readonly InteractableSpot[];
}

/** Qué está abierto para elegir: una subida de nivel o las bendiciones de un santuario. */
export type ChoiceSource = 'levelup' | 'shrine';

export class Run {
  /** Segundos de partida. */
  time = 0;
  kills = 0;
  hp: number;
  invulnerable = 0;
  /** Debug: el jugador no recibe daño. */
  invincible = false;
  /** Pruebas: las armas no atacan (para ver a los enemigos de cerca). */
  weaponsOff = false;
  /** Se han usado trucos de debug (la partida no contará para la meta del hito 5). */
  cheated = false;
  /** Frenado actual por atravesar enemigos (0 = nada; ver CROWD_CONFIG). */
  crowdSlow = 0;
  gold = 0;
  /** Oro conseguido en toda la partida (para los resultados). */
  goldCollected = 0;
  /** Jefe derrotado: partida ganada. */
  victory = false;
  readonly progress: LevelState = { level: 1, xp: 0 };
  /** Estadísticas del jugador (el objeto se conserva; se recalcula al cambiar la build). */
  readonly stats: PlayerStats;
  readonly player: CombatPlayer = { x: 0, y: 0, z: 0, facing: 0, vx: 0, vz: 0, grounded: true };
  readonly enemies: EnemySystem;
  readonly projectiles = new ProjectileSystem(PROJECTILE_CAPACITY);
  readonly enemyShots = new EnemyProjectileSystem(ENEMY_SHOT_CAPACITY);
  readonly gems = new PickupSystem(GEM_CAPACITY);
  readonly coins = new PickupSystem(GOLD_CONFIG.capacity);
  readonly weapons: WeaponInstance[] = [];
  readonly tomes: TomeInstance[] = [];
  readonly items: ItemStack[] = [];
  /** Bendiciones de los santuarios, acumuladas. */
  readonly boosts: AppliedBonus[] = [];
  readonly director: Director;
  readonly interactables: Interactables;
  boss: BossController | null = null;
  /** Lo que el jugador tiene delante para usar (tras el último tick). */
  prompt: InteractPrompt | null = null;
  /** Subidas de nivel y santuarios pendientes de elegir carta. */
  pendingLevelUps = 0;
  pendingShrines = 0;
  /** Cartas abiertas (null si no hay ninguna elección abierta) y de dónde vienen. */
  offer: OfferCard[] | null = null;
  offerSource: ChoiceSource = 'levelup';
  rerolls: number = LEVEL_UP_CONFIG.rerolls;
  skips: number = LEVEL_UP_CONFIG.skips;
  banishes: number = LEVEL_UP_CONFIG.banishes;
  /** Armas y tomos descartados para el resto de la partida. */
  readonly banished = new Set<string>();
  private readonly spawner: SpawnSystem;
  /** Sorteos separados del combate: con la misma semilla salen las mismas cartas y objetos. */
  private readonly offerRng: Rng;
  private readonly itemRng: Rng;
  private readonly rng: Rng;
  private readonly ctx: CombatContext;
  /** Copias de los objetos con efecto especial (se actualizan al cambiar los objetos). */
  private pearls = 0;
  private ollas = 0;
  private purses = 0;
  /** Centenas de oro con las que se calculó el daño del monedero. */
  private purseStep = 0;
  /** Explosiones de olla pendientes: x, y, z, daño y radio seguidos. */
  private readonly blasts: number[] = [];
  private readonly nearby = new Int32Array(256);
  /** Objetos reutilizados cada tick (sin crear basura). */
  private readonly view = { x: 0, z: 0, yaw: 0 };
  private readonly target = { x: 0, y: 0, z: 0, radius: PLAYER_HIT_RADIUS, vx: 0, vz: 0 };
  private readonly spawnParams: SpawnParams = { minutes: 0, rate: 0, maxAlive: 0, hp: 1, xp: 1, gold: 1 };
  private readonly mods: SpawnModifiers = { rate: 1, hp: 1, gold: 1 };
  private readonly directorEvents: DirectorEvents;
  private readonly interactableEvents: InteractableEvents;
  private readonly enemyActions: EnemyActions;
  private readonly bossHooks: BossHooks;

  constructor(
    readonly world: WorldCollision,
    seed: string,
    private readonly character: CharacterDef,
    private readonly fx: RunEffects = NO_EFFECTS,
    options: RunOptions = {},
  ) {
    const rng = new Rng(`${seed}/run`);
    this.rng = rng.derive('combat');
    this.offerRng = rng.derive('offers');
    this.itemRng = rng.derive('items');
    this.director = new Director(options.minutes ?? REFERENCE_MINUTES);
    this.interactables = new Interactables(options.interactables ?? []);
    this.stats = computePlayerStats(character, [], LEVEL_UP_CONFIG.baseChoices);
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
    this.directorEvents = {
      wave: (wave) => {
        this.spawner.spawnFormation(wave.enemy, wave.count, wave.formation, this.spawnParams, this.enemies, this.view, this.world);
        this.enemies.rebuildGrid();
        this.fx.notice({ kind: 'wave', key: wave.noticeKey });
      },
      elite: () => {
        if (this.spawner.spawnSpecial(ELITE_SCHEDULE.enemy, this.spawnParams.hp, this.spawnParams, this.enemies, this.view, this.world) < 0) return;
        this.enemies.rebuildGrid();
        this.fx.notice({ kind: 'elite', enemy: ELITE_SCHEDULE.enemy });
      },
      swarm: () => {
        this.fx.notice({ kind: 'swarm' });
        // Para tener una salida: el portal del jefe se revela si aún no se había encontrado.
        if (this.interactables.reveal('portal') > 0) this.fx.notice({ kind: 'portalRevealed' });
      },
    };
    this.interactableEvents = {
      discovered: (item) => {
        if (item.spot.kind === 'portal') this.fx.notice({ kind: 'portalFound' });
      },
      shrineCharged: () => {
        this.pendingShrines++;
        this.fx.notice({ kind: 'shrineCharged' });
      },
    };
    this.enemyActions = { shoot: (i, dirX, dirZ) => this.enemyShoot(i, dirX, dirZ) };
    this.bossHooks = {
      slam: (x, y, z, radius, damage) => {
        this.fx.bossSlam(x, y, z, radius);
        const d = Math.hypot(this.player.x - x, this.player.z - z);
        if (d <= radius + PLAYER_HIT_RADIUS && Math.abs(this.player.y - y) < 3) this.hurtPlayer(damage);
      },
      dust: (x, z, dirX, dirZ) => {
        const s = BOSS_CONFIG.sneeze;
        this.enemyShots.spawn({ x, z, dirX, dirZ, speed: s.speed, damage: s.damage, radius: s.radius, life: 4, kind: PROJECTILE_DUST }, this.world.heightfield);
      },
      minion: (x, z) => {
        if (!this.world.isInside(x, z, 2)) return;
        this.spawner.place(enemyTypeIndex('pelusa'), x, z, this.spawnParams, this.enemies, this.world);
      },
    };
    this.addWeapon(character.startingWeapon);
  }

  /** Minutos de dificultad (la curva avanza más deprisa en partidas cortas). */
  get difficulty(): number {
    return this.director.difficulty(this.time);
  }

  /** Segundos que quedan de partida (negativo durante el enjambre final). */
  get timeLeft(): number {
    return this.director.timeLeft(this.time);
  }

  get swarm(): boolean {
    return this.director.swarm;
  }

  get dead(): boolean {
    return this.hp <= 0;
  }

  get level(): number {
    return this.progress.level;
  }

  get chestsOpened(): number {
    return this.interactables.chestsOpened;
  }

  /** Vida del jefe si está en juego (o null). */
  get bossHealth(): { hp: number; maxHp: number; enraged: boolean } | null {
    if (!this.boss) return null;
    const i = this.enemies.indexOfId(this.boss.enemyId);
    if (i < 0) return null;
    return { hp: this.enemies.hp[i] as number, maxHp: this.enemies.maxHp[i] as number, enraged: this.boss.enraged };
  }

  /** Avanza la partida un paso de lógica. */
  update(dt: number, body: PlayerBody, viewYaw: number): void {
    if (this.dead || this.victory) return;
    this.time += dt;
    this.syncPlayer(body, viewYaw);

    const params = this.director.spawnParams(this.time, this.modifiers(), this.spawnParams);
    this.director.update(this.time, this.directorEvents);
    this.spawner.update(dt, params, this.enemies, this.view, this.world);
    if (this.boss && !this.boss.update(dt, this.enemies, this.player, this.bossHooks)) this.boss = null;
    const contact = this.enemies.update(dt, this.target, this.world, this.enemyActions);
    // Contra el jefe no se pasa: es él quien aparta a Doña Remedios.
    if (this.enemies.playerPushX !== 0 || this.enemies.playerPushZ !== 0) {
      body.x += this.enemies.playerPushX;
      body.z += this.enemies.playerPushZ;
      this.syncPlayer(body, viewYaw);
    }
    if (contact > 0) this.hurtPlayer(contact);
    this.crowdSlow = smoothCrowdSlow(this.crowdSlow, crowdSlowFor(this.enemies.playerPressure), dt);

    if (!this.weaponsOff) for (const weapon of this.weapons) BEHAVIORS[weapon.def.behavior].update(weapon, this.ctx, dt);
    this.projectiles.update(dt, this.enemies, this.world.heightfield, (p, e) => {
      const weapon = this.weapons[this.projectiles.weapon[p] as number];
      if (!weapon) return;
      const vx = this.projectiles.vx[p] as number;
      const vz = this.projectiles.vz[p] as number;
      const len = Math.hypot(vx, vz) || 1;
      this.ctx.damageEnemy(e, weapon, vx / len, vz / len);
    });
    const shot = this.enemyShots.update(dt, this.world.heightfield, this.target);
    if (shot > 0) this.hurtPlayer(shot);
    this.flushDead();

    const xp = this.gems.update(dt, body.x, body.y, body.z, this.stats.pickupRadius);
    if (xp > 0) this.gainExperience(xp);
    const gold = this.coins.update(dt, body.x, body.y, body.z, this.stats.pickupRadius);
    if (gold > 0) this.gainGold(gold * this.stats.goldGain);

    if (this.interactables.update(dt, body.x, body.z, this.interactableEvents)) this.finishChallenge();
    this.prompt = this.interactables.prompt(body.x, body.z);

    this.invulnerable = Math.max(0, this.invulnerable - dt);
    if (this.stats.regen > 0 && this.hp > 0) this.hp = Math.min(this.stats.maxHp, this.hp + this.stats.regen * dt);
  }

  private syncPlayer(body: PlayerBody, viewYaw: number): void {
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
  }

  /** Modificadores del ritmo de aparición: desafío del tótem y santuario cargándose. */
  private modifiers(): SpawnModifiers {
    const m = this.mods;
    m.rate = 1;
    m.hp = 1;
    m.gold = 1;
    if (this.interactables.challenge > 0) {
      m.rate *= TOTEM_CONFIG.spawnMultiplier;
      m.hp *= TOTEM_CONFIG.hpMultiplier;
      m.gold *= TOTEM_CONFIG.goldMultiplier;
    }
    if (this.interactables.charging >= 0) m.rate *= SHRINE_CONFIG.spawnMultiplier;
    return m;
  }

  // ------------------------------------------------------------ daño

  private damageEnemy(e: number, weapon: WeaponInstance, pushX: number, pushZ: number, random: () => number): void {
    const s = weapon.effective;
    const roll = rollDamage(s.damage, s.critChance, s.critMultiplier, random);
    const before = this.enemies.hp[e] as number;
    const dealt = this.hitEnemy(e, roll.amount, roll.critLevel, weapon.def.hitFlash, pushX * s.knockback, pushZ * s.knockback);
    weapon.totalDamage += dealt;
    if (before > 0 && (this.enemies.hp[e] as number) <= 0) weapon.kills++;
    // Collar de perlas: un crítico puede hacer saltar una perla al enemigo más cercano.
    if (roll.critLevel > 0 && this.pearls > 0 && random() < pearlChance(this.pearls)) {
      weapon.totalDamage += this.pearlBounce(e, roll.amount * ITEM_EFFECTS.perlas.damageFraction);
    }
  }

  /** Aplica daño y empuje a un enemigo; devuelve el daño que de verdad le ha quitado. */
  private hitEnemy(e: number, amount: number, critLevel: number, flash: number, pushX: number, pushZ: number): number {
    const enemies = this.enemies;
    const def = ENEMY_LIST[enemies.type[e] as number];
    if (!def) return 0;
    const before = enemies.hp[e] as number;
    enemies.hp[e] = before - amount;
    enemies.flash[e] = Math.max(enemies.flash[e] as number, flash);
    enemies.kx[e] = (enemies.kx[e] as number) + pushX / def.mass;
    enemies.kz[e] = (enemies.kz[e] as number) + pushZ / def.mass;
    this.fx.damageNumber(enemies.x[e] as number, (enemies.y[e] as number) + def.height + 0.35, enemies.z[e] as number, amount, critLevel);
    return before > 0 ? Math.min(before, amount) : 0;
  }

  private pearlBounce(from: number, amount: number): number {
    const enemies = this.enemies;
    const fx = enemies.x[from] as number;
    const fz = enemies.z[from] as number;
    const fromId = enemies.id[from] as number;
    const target = enemies.grid.nearest(fx, fz, ITEM_EFFECTS.perlas.range, (j) => enemies.id[j] !== fromId && (enemies.hp[j] as number) > 0);
    if (target < 0) return 0;
    const fy = (enemies.y[from] as number) + 0.8;
    this.fx.pearl(fx, fy, fz, enemies.x[target] as number, (enemies.y[target] as number) + 0.8, enemies.z[target] as number);
    return this.hitEnemy(target, amount, 0, 0.5, 0, 0);
  }

  /** Quita a los muertos y resuelve las explosiones de olla (que pueden encadenarse). */
  private flushDead(): void {
    this.enemies.flushDead((i) => this.onEnemyDeath(i));
    for (let round = 0; round < MAX_BLAST_ROUNDS && this.blasts.length > 0; round++) {
      const batch = this.blasts.splice(0);
      for (let b = 0; b < batch.length; b += 5) {
        this.blast(batch[b] as number, batch[b + 1] as number, batch[b + 2] as number, batch[b + 3] as number, batch[b + 4] as number);
      }
      this.enemies.flushDead((i) => this.onEnemyDeath(i));
    }
    this.blasts.length = 0;
  }

  private blast(x: number, y: number, z: number, damage: number, radius: number): void {
    const enemies = this.enemies;
    this.fx.explosion(x, y, z, radius);
    const n = enemies.queryRadius(x, z, radius, this.nearby);
    for (let k = 0; k < n; k++) {
      const e = this.nearby[k] as number;
      if (e >= enemies.count || (enemies.hp[e] as number) <= 0) continue;
      const dx = (enemies.x[e] as number) - x;
      const dz = (enemies.z[e] as number) - z;
      const d = Math.hypot(dx, dz);
      if (d > radius + enemies.radiusOf(e)) continue;
      this.hitEnemy(e, damage, 0, 0.6, d > 1e-4 ? (dx / d) * 6 : 0, d > 1e-4 ? (dz / d) * 6 : 0);
    }
  }

  private onEnemyDeath(i: number): void {
    const e = this.enemies;
    const def = ENEMY_LIST[e.type[i] as number];
    const x = e.x[i] as number;
    const y = e.y[i] as number;
    const z = e.z[i] as number;
    this.kills++;
    if ((e.xp[i] as number) > 0) this.gems.spawn(x, y + 0.4, z, e.xp[i] as number);
    if (def) {
      const g = def.gold;
      if (g.chance > 0 && this.rng.next() < g.chance) {
        // Redondeo al azar: con multiplicadores fraccionarios la media sale exacta.
        const value = Math.floor(this.rng.int(g.min, g.max) * (e.gold[i] as number) + this.rng.next());
        if (value > 0) this.coins.spawn(x, y + 0.5, z, value);
      }
      if (this.ollas > 0 && this.rng.next() < ollaChance(this.ollas)) {
        const o = ITEM_EFFECTS.olla;
        this.blasts.push(x, y, z, o.damage * this.stats.damage + o.hpFraction * (e.maxHp[i] as number), o.radius * this.stats.area);
      }
      if (def.special === 'boss') {
        this.victory = true;
        this.boss = null;
      }
    }
    this.fx.enemyKilled(x, y, z, e.type[i] as number);
  }

  private enemyShoot(i: number, dirX: number, dirZ: number): void {
    const e = this.enemies;
    const def = ENEMY_LIST[e.type[i] as number];
    const r = def?.ranged;
    if (!def || !r) return;
    const x = (e.x[i] as number) + dirX * def.radius;
    const z = (e.z[i] as number) + dirZ * def.radius;
    this.enemyShots.spawn(
      {
        x,
        z,
        dirX,
        dirZ,
        speed: r.projectileSpeed,
        damage: r.projectileDamage,
        radius: r.projectileRadius,
        life: r.range / r.projectileSpeed + 0.6,
        kind: PROJECTILE_PIPA,
      },
      this.world.heightfield,
    );
    this.fx.enemyShot(x, (e.y[i] as number) + def.height * 0.6, z);
  }

  private hurtPlayer(amount: number): void {
    if (this.invincible || this.invulnerable > 0 || this.hp <= 0) return;
    const damage = mitigate(amount, this.stats.armor);
    this.hp = Math.max(0, this.hp - damage);
    this.invulnerable = INVULNERABILITY_TIME;
    this.fx.playerHit(damage);
    if (this.hp <= 0) this.tryRevive();
  }

  /** Bata de guatiné: gasta una copia para volver con media vida y apartar a los enemigos. */
  private tryRevive(): void {
    const stack = this.items.find((s) => s.def.id === 'bata');
    if (!stack) return;
    stack.count--;
    if (stack.count <= 0) this.items.splice(this.items.indexOf(stack), 1);
    this.refreshStats();
    const b = ITEM_EFFECTS.bata;
    this.hp = this.stats.maxHp * b.reviveHp;
    this.invulnerable = b.invulnerability;
    const p = this.player;
    const enemies = this.enemies;
    const n = enemies.queryRadius(p.x, p.z, b.shockwaveRadius, this.nearby);
    for (let k = 0; k < n; k++) {
      const e = this.nearby[k] as number;
      if (e >= enemies.count) continue;
      const dx = (enemies.x[e] as number) - p.x;
      const dz = (enemies.z[e] as number) - p.z;
      const d = Math.hypot(dx, dz) || 1;
      const mass = ENEMY_LIST[enemies.type[e] as number]?.mass ?? 1;
      enemies.kx[e] = (enemies.kx[e] as number) + ((dx / d) * b.shockwavePush) / mass;
      enemies.kz[e] = (enemies.kz[e] as number) + ((dz / d) * b.shockwavePush) / mass;
    }
    this.enemyShots.clear();
    this.fx.revive(p.x, p.y, p.z, b.shockwaveRadius);
    this.fx.notice({ kind: 'revive' });
  }

  private gainExperience(amount: number): void {
    const before = this.progress.level;
    const gained = addExperience(this.progress, amount * this.stats.xpGain);
    for (let k = 1; k <= gained; k++) this.fx.levelUp(before + k);
    this.pendingLevelUps += gained;
  }

  private gainGold(amount: number): void {
    this.gold += amount;
    this.goldCollected += amount;
    this.checkPurse();
  }

  /** El monedero pega más con cada centena de oro: se recalcula al cruzar una. */
  private checkPurse(): void {
    if (this.purses > 0 && Math.floor(this.gold / 100) !== this.purseStep) this.refreshStats();
  }

  /** Añade un arma si no se tiene ya y queda hueco. */
  addWeapon(id: WeaponId): WeaponInstance | null {
    if (this.weapons.some((w) => w.def.id === id) || this.weapons.length >= LEVEL_UP_CONFIG.maxWeapons) return null;
    const weapon = createWeapon(id, this.weapons.length, this.stats, this.enemies.capacity);
    this.weapons.push(weapon);
    return weapon;
  }

  /** Añade una copia de un objeto. */
  addItem(def: ItemDef): void {
    const stack = this.items.find((s) => s.def.id === def.id);
    if (stack) stack.count++;
    else this.items.push({ def, count: 1 });
    this.refreshStats();
    this.fx.itemGained(def);
  }

  // ------------------------------------------------------------ interactuables

  /** Usa lo que el jugador tenga delante (tecla de interactuar). */
  interact(): boolean {
    const prompt = this.interactables.prompt(this.player.x, this.player.z);
    if (!prompt || this.dead || this.victory) return false;
    const item = this.interactables.list[prompt.index];
    if (!item) return false;
    switch (prompt.kind) {
      case 'chest':
        return this.openChest(item, prompt.cost);
      case 'totem':
        return this.startChallenge(item);
      case 'portal':
        return this.openPortal(item);
      case 'shrine':
        return false;
    }
  }

  private openChest(item: InteractableState, cost: number): boolean {
    if (this.gold < cost) {
      this.fx.notice({ kind: 'noGold', missing: Math.ceil(cost - this.gold) });
      return false;
    }
    this.gold -= cost;
    this.interactables.chestsOpened++;
    item.used = true;
    this.fx.chestOpened(item.spot.x, item.spot.y + 0.9, item.spot.z);
    const def = rollItem(this.stats.luck, this.itemRng, this.items, this.stats);
    if (def) this.addItem(def);
    else this.gainGold(cost);
    this.checkPurse();
    return true;
  }

  private startChallenge(item: InteractableState): boolean {
    if (this.interactables.challenge > 0) return false;
    item.used = true;
    this.interactables.challenge = TOTEM_CONFIG.duration;
    this.refreshStats();
    this.fx.notice({ kind: 'challengeStart' });
    return true;
  }

  /** Desafío superado: vuelve la calma y cae un objeto con mucha suerte. */
  private finishChallenge(): void {
    this.refreshStats();
    this.fx.notice({ kind: 'challengeDone' });
    const def = rollItem(this.stats.luck + TOTEM_CONFIG.rewardLuck, this.itemRng, this.items, this.stats);
    if (def) this.addItem(def);
  }

  private openPortal(item: InteractableState): boolean {
    // Sale por detrás del armario (el lado contrario al jugador) y rodea para ir a por él.
    const p = this.player;
    const dx = item.spot.x - p.x;
    const dz = item.spot.z - p.z;
    const d = Math.hypot(dx, dz) || 1;
    const x = item.spot.x + (dx / d) * PORTAL_CONFIG.bossDistance;
    const z = item.spot.z + (dz / d) * PORTAL_CONFIG.bossDistance;
    if (!this.summonBoss(x, z)) return false;
    item.used = true;
    return true;
  }

  /** Invoca al jefe en (x, z); su vida depende de lo avanzada que esté la partida. */
  private summonBoss(x: number, z: number): boolean {
    if (this.boss || this.victory) return false;
    const enemies = this.enemies;
    // Siempre cabe: si no hay hueco, se va el último enemigo normal.
    if (enemies.count >= enemies.capacity) enemies.remove(enemies.count - 1);
    const m = this.difficulty;
    const hp = 1 + BOSS_CONFIG.hpGrowth * m + BOSS_CONFIG.hpCurve * m * m;
    const pos = { x, z };
    this.world.clampInside(pos);
    const i = this.spawner.place(enemyTypeIndex(BOSS_CONFIG.enemy), pos.x, pos.z, this.spawnParams, enemies, this.world, hp);
    if (i < 0) return false;
    enemies.xp[i] = 0;
    enemies.rebuildGrid();
    this.boss = new BossController(enemies.id[i] as number, this.rng.derive(`boss-${this.time.toFixed(2)}`));
    this.fx.bossSpawned(pos.x, enemies.y[i] as number, pos.z);
    this.fx.notice({ kind: 'boss', enemy: BOSS_CONFIG.enemy });
    return true;
  }

  // ------------------------------------------------------------ elecciones (subida de nivel y santuarios)

  private get build(): BuildView {
    return {
      weapons: this.weapons,
      tomes: this.tomes,
      stats: this.stats,
      banished: this.banished,
      fillerGold: Math.max(GOLD_CONFIG.fillerMin, Math.round(chestCost(this.interactables.chestsOpened) * GOLD_CONFIG.fillerChestFraction)),
    };
  }

  /** Abre la siguiente elección pendiente (primero los santuarios), si no hay ya una abierta. */
  openChoice(): boolean {
    if (this.offer) return false;
    if (this.pendingShrines > 0) {
      this.offerSource = 'shrine';
      this.offer = generateShrineOffer(this.stats, SHRINE_CONFIG.choices, this.offerRng);
      return true;
    }
    if (this.pendingLevelUps <= 0) return false;
    this.offerSource = 'levelup';
    this.offer = generateOffer(this.build, this.stats.choices, this.offerRng);
    return true;
  }

  /** Elige una carta; si quedan elecciones pendientes, abre la siguiente. */
  choose(index: number): boolean {
    const card = this.offer?.[index];
    if (!card) return false;
    this.applyCard(card);
    this.closeChoice();
    return true;
  }

  /** Cambia todas las cartas por otras nuevas (solo al subir de nivel). */
  reroll(): boolean {
    if (!this.offer || this.offerSource !== 'levelup' || this.rerolls <= 0) return false;
    this.rerolls--;
    this.offer = generateOffer(this.build, this.stats.choices, this.offerRng);
    return true;
  }

  /** Pasa esta subida de nivel sin elegir nada. */
  skip(): boolean {
    if (!this.offer || this.offerSource !== 'levelup' || this.skips <= 0) return false;
    this.skips--;
    this.closeChoice();
    return true;
  }

  /** Quita esa arma o tomo del sorteo para el resto de la partida y pone otra carta en su hueco. */
  banish(index: number): boolean {
    const offer = this.offer;
    const card = offer?.[index];
    if (!offer || this.offerSource !== 'levelup' || !card || card.key === null || this.banishes <= 0) return false;
    this.banishes--;
    this.banished.add(card.key);
    const rest = offer.filter((_, i) => i !== index);
    const replacement = replacementCard(this.build, rest, this.offerRng);
    if (replacement) offer[index] = replacement;
    else offer.splice(index, 1);
    if (offer.length === 0) offer.push(healCard());
    return true;
  }

  private closeChoice(): void {
    this.offer = null;
    if (this.offerSource === 'shrine') this.pendingShrines = Math.max(0, this.pendingShrines - 1);
    else this.pendingLevelUps = Math.max(0, this.pendingLevelUps - 1);
    this.openChoice();
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
      case 'gold':
        this.gainGold(card.amount);
        break;
      case 'boost':
        this.boosts.push({ stat: card.boost.effect.stat, mode: card.boost.effect.mode, amount: card.amount });
        this.refreshStats();
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

  /** Todo lo que suma a las estadísticas: tomos, objetos, bendiciones y efectos del momento. */
  private bonuses(): AppliedBonus[] {
    const out = [...tomeBonuses(this.tomes), ...itemBonuses(this.items), ...this.boosts];
    const purses = itemCount(this.items, 'monedero');
    if (purses > 0) out.push({ stat: 'damage', mode: 'add', amount: purseBonus(this.gold, purses) });
    if (this.interactables.challenge > 0) out.push({ stat: 'luck', mode: 'add', amount: TOTEM_CONFIG.luck });
    return out;
  }

  /** Recalcula las estadísticas del jugador y de las armas (tras cambiar la build). */
  private refreshStats(): void {
    const oldMax = this.stats.maxHp;
    Object.assign(this.stats, computePlayerStats(this.character, this.bonuses(), LEVEL_UP_CONFIG.baseChoices));
    // La vida máxima que se gana llega también a la actual.
    if (this.stats.maxHp > oldMax) this.hp += this.stats.maxHp - oldMax;
    this.hp = Math.min(this.hp, this.stats.maxHp);
    this.pearls = itemCount(this.items, 'perlas');
    this.ollas = itemCount(this.items, 'olla');
    this.purses = itemCount(this.items, 'monedero');
    this.purseStep = Math.floor(this.gold / 100);
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
    this.spawner.spawnBurst(count, this.director.spawnParams(this.time, this.modifiers(), this.spawnParams), this.enemies, this.view, this.world);
    this.enemies.rebuildGrid();
  }

  debugKillAll(): void {
    this.cheated = true;
    this.enemies.hp.fill(0, 0, this.enemies.count);
    this.flushDead();
  }

  /** Invoca al jefe delante del jugador (sin buscar el portal). */
  debugSummonBoss(): boolean {
    this.cheated = true;
    this.director.spawnParams(this.time, this.modifiers(), this.spawnParams);
    const p = this.player;
    return this.summonBoss(p.x - Math.sin(p.facing) * DEBUG_BOSS_DISTANCE, p.z - Math.cos(p.facing) * DEBUG_BOSS_DISTANCE);
  }

  debugAddGold(amount: number): void {
    this.cheated = true;
    this.gainGold(amount);
  }

  /** Descubre todos los interactuables del mapa. */
  debugRevealMap(): void {
    this.cheated = true;
    this.interactables.reveal();
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

  /** Pruebas: hace aparecer un enemigo concreto en (x, z). */
  debugSpawnEnemy(id: EnemyId, x: number, z: number): number {
    this.cheated = true;
    const params = this.director.spawnParams(this.time, this.modifiers(), this.spawnParams);
    const i = this.spawner.place(enemyTypeIndex(id), x, z, params, this.enemies, this.world);
    this.enemies.rebuildGrid();
    return i;
  }

  /** Pruebas: da una copia de un objeto. */
  debugAddItem(id: ItemId): void {
    this.cheated = true;
    this.addItem(ITEMS[id]);
  }
}
