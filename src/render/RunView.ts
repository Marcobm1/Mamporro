// Parte visual de una partida: enemigos, proyectiles, gemas, monedas, efectos de
// las armas, avisos de ataque, partículas y números de daño. Recibe los sucesos de
// la lógica (RunEffects) y los convierte en efectos; los avisos de interfaz y la
// sacudida de cámara se delegan en `hooks`.
import { Group, type Texture } from 'three';
import type { Vec3Like } from '../core/math';
import type { Run, RunEffects, RunNotice } from '../core/Run';
import { ENEMY_CAPACITY, ENEMY_SHOT_CAPACITY, GEM_CAPACITY, PLAYER_HIT_RADIUS, PROJECTILE_CAPACITY } from '../core/Run';
import { Rng } from '../core/rng';
import { BOSS_CONFIG, ENEMY_LIST } from '../data/enemies';
import type { ItemDef } from '../data/items';
import { GOLD_CONFIG } from '../data/run';
import { ENEMY_STATE } from '../systems/EnemySystem';
import { auraRadius } from '../weapons/aura';
import { orbitGeometry } from '../weapons/orbit';
import type { OrbitState, TrailState } from '../weapons/types';
import { AuraRenderer, CoinRenderer, EnemyShotRenderer, GemRenderer, ProjectileRenderer } from './CombatRenderers';
import { DamageNumbers } from './DamageNumbers';
import { EnemyRenderer } from './EnemyRenderer';
import { PALETTE } from './palette';
import { Particles } from './Particles';
import { TelegraphRenderer } from './Telegraphs';
import { ArcRenderer, ChainRenderer, OrbitRenderer, TrailRenderer } from './WeaponEffects';

export interface RunViewHooks {
  onPlayerHit(damage: number): void;
  onNotice(notice: RunNotice): void;
  onItem(item: ItemDef): void;
  /** Sacudida de cámara (0..1). */
  onShake(amount: number): void;
}

/** Tipos de enemigo que embisten (su aviso se dibuja en el suelo). */
const CHARGERS = ENEMY_LIST.map((def) => def.behavior === 'charger');

export class RunView implements RunEffects {
  readonly group = new Group();
  private readonly enemyRenderer: EnemyRenderer;
  private readonly projectileRenderer = new ProjectileRenderer(PROJECTILE_CAPACITY);
  private readonly gemRenderer = new GemRenderer(GEM_CAPACITY);
  private readonly coinRenderer = new CoinRenderer(GOLD_CONFIG.capacity);
  private readonly shotRenderer = new EnemyShotRenderer(ENEMY_SHOT_CAPACITY);
  private readonly telegraphs = new TelegraphRenderer();
  private readonly aura = new AuraRenderer();
  private readonly arcs = new ArcRenderer();
  private readonly orbit = new OrbitRenderer();
  private readonly chains: ChainRenderer;
  private readonly trail = new TrailRenderer();
  private readonly particles: Particles;
  private readonly numbers: DamageNumbers;
  private readonly rng: Rng;
  private run: Run | null = null;
  private lastAuraPulse = Number.POSITIVE_INFINITY;
  private lastTrailPulse = Number.POSITIVE_INFINITY;

  constructor(
    shadowTexture: Texture,
    private readonly hooks: RunViewHooks,
  ) {
    // Aleatoriedad solo visual: no afecta a la partida ni a la semilla.
    const rng = new Rng('efectos');
    this.rng = rng.derive('armas');
    this.enemyRenderer = new EnemyRenderer(ENEMY_CAPACITY, shadowTexture);
    this.particles = new Particles(1500, rng.derive('particulas'));
    this.numbers = new DamageNumbers(rng.derive('numeros'));
    this.chains = new ChainRenderer(rng.derive('rayos'));
    this.group.add(
      this.enemyRenderer.group,
      this.projectileRenderer.mesh,
      this.gemRenderer.mesh,
      this.coinRenderer.mesh,
      this.shotRenderer.group,
      this.telegraphs.group,
      this.aura.group,
      this.arcs.group,
      this.orbit.mesh,
      this.chains.lines,
      this.trail.group,
      this.particles.mesh,
      this.numbers.mesh,
    );
    this.group.visible = false;
  }

  get particleCount(): number {
    return this.particles.active;
  }

  attach(run: Run): void {
    this.run = run;
    this.particles.clear();
    this.numbers.clear();
    this.arcs.clear();
    this.chains.clear();
    this.telegraphs.clear();
    this.group.visible = true;
  }

  detach(): void {
    this.run = null;
    this.group.visible = false;
  }

  /** `player` es la posición interpolada de Doña Remedios (la que se dibuja). */
  update(alpha: number, frameDt: number, time: number, player: Vec3Like): void {
    const run = this.run;
    if (!run) return;
    this.enemyRenderer.update(run.enemies, alpha, run.boss);
    this.projectileRenderer.update(run.projectiles, alpha);
    this.gemRenderer.update(run.gems);
    this.coinRenderer.update(run.coins);
    this.shotRenderer.update(run.enemyShots, alpha);
    this.updateTelegraphs(run);

    let auraShown = false;
    let orbitState: OrbitState | null = null;
    let trailState: TrailState | null = null;
    for (const weapon of run.weapons) {
      const s = weapon.effective;
      if (weapon.def.behavior === 'aura') {
        auraShown = true;
        const radius = auraRadius(s.area);
        this.aura.update(true, player.x, player.y, player.z, radius, weapon.sincePulse, time);
        // Vapores de naftalina en cada pulso.
        if (weapon.sincePulse < this.lastAuraPulse) this.auraWisps(player, radius);
        this.lastAuraPulse = weapon.sincePulse;
      } else if (weapon.state.kind === 'orbit') {
        orbitState = weapon.state;
        const { orbit, orb } = orbitGeometry(s.area);
        // Salen creciendo y se guardan encogiéndose.
        const pop = Math.min(1, weapon.sincePulse / 0.15, Math.max(0, weapon.timer) / 0.2);
        this.orbit.update(orbitState, Math.round(s.count), player.x, player.y, player.z, orbit, orb, pop, time);
      } else if (weapon.state.kind === 'trail') {
        trailState = weapon.state;
        if (weapon.sincePulse < this.lastTrailPulse) this.trailBubbles(weapon.state);
        this.lastTrailPulse = weapon.sincePulse;
      }
    }
    if (!auraShown) this.aura.update(false, 0, 0, 0, 1, 0, time);
    if (!orbitState) this.orbit.update(null, 0, 0, 0, 0, 1, 1, 0, time);
    this.trail.update(trailState);

    this.arcs.update(frameDt);
    this.chains.update(frameDt);
    this.particles.update(frameDt);
    this.numbers.update(frameDt);
  }

  /** Avisos en el suelo: embestidas de las ratas y ataques del jefe. */
  private updateTelegraphs(run: Run): void {
    const t = this.telegraphs;
    const enemies = run.enemies;
    const hf = run.world.heightfield;
    t.begin();
    for (let i = 0; i < enemies.count; i++) {
      const type = enemies.type[i] as number;
      if (!CHARGERS[type] || enemies.state[i] !== ENEMY_STATE.windup) continue;
      const def = ENEMY_LIST[type];
      const c = def?.charge;
      if (!def || !c) continue;
      const progress = 1 - (enemies.stateTime[i] as number) / c.windup;
      t.line(hf, enemies.x[i] as number, enemies.z[i] as number, enemies.aimX[i] as number, enemies.aimZ[i] as number, c.dashSpeed * c.dashTime + def.radius, def.radius * 2, progress);
    }
    const boss = run.boss;
    if (boss && boss.phase === 'windup') {
      const i = enemies.indexOfId(boss.enemyId);
      const def = ENEMY_LIST[enemies.type[i] as number];
      if (i >= 0 && def) {
        const x = enemies.x[i] as number;
        const z = enemies.z[i] as number;
        if (boss.attack === 'roll') {
          const width = (def.radius + PLAYER_HIT_RADIUS) * 2;
          t.line(hf, x, z, enemies.aimX[i] as number, enemies.aimZ[i] as number, BOSS_CONFIG.roll.length + def.radius, width, boss.windupProgress);
        } else if (boss.attack === 'slam') {
          t.circle(hf, x, z, BOSS_CONFIG.slam.radius, boss.windupProgress);
        }
      }
    }
    t.end();
  }

  private auraWisps(player: Vec3Like, radius: number): void {
    for (let k = 0; k < 3; k++) {
      const a = this.rng.next() * Math.PI * 2;
      const r = radius * this.rng.range(0.4, 1);
      this.particles.burst(player.x + Math.cos(a) * r, player.y + 0.3, player.z + Math.sin(a) * r, {
        count: 1,
        colors: [PALETTE.aura, PALETTE.auraEdge],
        speed: 0.4,
        size: 0.16,
        life: 0.9,
        lift: 1,
        gravity: -1.5,
      });
    }
  }

  /** Burbujas de jabón en algunos charcos al pasar la fregona. */
  private trailBubbles(state: TrailState): void {
    const n = Math.min(3, state.count);
    for (let k = 0; k < n; k++) {
      const i = this.rng.int(0, state.count - 1);
      this.particles.burst(state.x[i] as number, (state.y[i] as number) + 0.1, state.z[i] as number, {
        count: 1,
        colors: [PALETTE.puddleShine, PALETTE.puddle],
        speed: 0.3,
        size: 0.13,
        life: 0.8,
        lift: 0.8,
        gravity: -0.6,
      });
    }
  }

  // ------------------------------------------------------------ RunEffects

  damageNumber(x: number, y: number, z: number, amount: number, critLevel: number): void {
    this.numbers.spawn(x, y, z, amount, critLevel);
  }

  enemyKilled(x: number, y: number, z: number, type: number): void {
    const def = ENEMY_LIST[type];
    if (!def) return;
    this.particles.burst(x, y + def.height * 0.5, z, {
      count: 12,
      colors: def.debrisColors,
      speed: 5,
      size: 0.14,
      life: 0.7,
      lift: 2,
    });
  }

  enemySpawned(x: number, y: number, z: number): void {
    this.particles.burst(x, y + 0.2, z, { count: 5, colors: [PALETTE.dust], speed: 1.8, size: 0.28, life: 0.5, gravity: -1 });
  }

  playerHit(damage: number): void {
    const run = this.run;
    if (!run) return;
    this.numbers.spawn(run.player.x, run.player.y + 2, run.player.z, damage, -1);
    this.hooks.onPlayerHit(damage);
  }

  levelUp(): void {
    const run = this.run;
    if (!run) return;
    this.particles.burst(run.player.x, run.player.y + 1, run.player.z, {
      count: 24,
      colors: [PALETTE.numberCrit, PALETTE.gemBlue, 0xffffff],
      speed: 4,
      size: 0.12,
      life: 0.9,
      lift: 3,
      gravity: 6,
    });
  }

  notice(notice: RunNotice): void {
    this.hooks.onNotice(notice);
  }

  itemGained(item: ItemDef): void {
    this.hooks.onItem(item);
  }

  chestOpened(x: number, y: number, z: number): void {
    this.particles.burst(x, y, z, {
      count: 26,
      colors: [PALETTE.coin, PALETTE.numberCrit, 0xffffff],
      speed: 4,
      size: 0.12,
      life: 0.9,
      lift: 4,
      gravity: 8,
    });
  }

  enemyShot(x: number, y: number, z: number): void {
    this.particles.burst(x, y, z, { count: 3, colors: [PALETTE.pipaStripe, PALETTE.pipa], speed: 1.5, size: 0.08, life: 0.35 });
  }

  bossSpawned(x: number, y: number, z: number): void {
    this.particles.burst(x, y + 1, z, { count: 60, colors: [PALETTE.dust, PALETTE.pelusa, PALETTE.pelusaDark], speed: 7, size: 0.4, life: 1.4, lift: 3, gravity: 3 });
    this.hooks.onShake(0.8);
  }

  bossSlam(x: number, y: number, z: number, radius: number): void {
    for (let k = 0; k < 20; k++) {
      const a = (k / 20) * Math.PI * 2;
      this.particles.burst(x + Math.cos(a) * radius * 0.8, y + 0.2, z + Math.sin(a) * radius * 0.8, {
        count: 2,
        colors: [PALETTE.dust, PALETTE.pelusa],
        speed: 3,
        size: 0.35,
        life: 0.9,
        lift: 2,
        gravity: 4,
      });
    }
    const p = this.run?.player;
    const d = p ? Math.hypot(p.x - x, p.z - z) : 0;
    this.hooks.onShake(d < radius * 2.5 ? 1 : 0.4);
  }

  explosion(x: number, y: number, z: number, radius: number): void {
    this.particles.burst(x, y + 0.5, z, { count: 14, colors: [PALETTE.blast, PALETTE.numberCrit, 0xffffff], speed: radius * 2.2, size: 0.18, life: 0.45, lift: 2 });
    this.particles.burst(x, y + 0.6, z, { count: 6, colors: [PALETTE.blastSmoke], speed: 1.2, size: 0.4, life: 0.9, gravity: -1.5 });
  }

  pearl(x1: number, y1: number, z1: number, x2: number, y2: number, z2: number): void {
    // Una ristra de perlas por el aire, del primero al segundo.
    for (let k = 1; k <= 5; k++) {
      const t = k / 6;
      const arc = Math.sin(t * Math.PI) * 0.8;
      this.particles.burst(x1 + (x2 - x1) * t, y1 + (y2 - y1) * t + arc, z1 + (z2 - z1) * t, {
        count: 1,
        colors: [PALETTE.pearl],
        speed: 0.2,
        size: 0.13,
        life: 0.35,
        gravity: 0,
      });
    }
  }

  revive(x: number, y: number, z: number, radius: number): void {
    for (let k = 0; k < 28; k++) {
      const a = (k / 28) * Math.PI * 2;
      this.particles.burst(x + Math.cos(a) * 1.2, y + 0.8, z + Math.sin(a) * 1.2, {
        count: 2,
        colors: [PALETTE.shawl, PALETTE.pompom, 0xffffff],
        speed: radius * 1.2,
        size: 0.16,
        life: 0.7,
        lift: 1,
        gravity: 0,
      });
    }
    this.hooks.onShake(0.7);
  }

  arcSwing(x: number, y: number, z: number, angle: number, radius: number, halfAngle: number): void {
    this.arcs.spawn(x, y, z, angle, radius, halfAngle);
    // Migas de pan por el borde del barrazo.
    for (let k = 0; k < 4; k++) {
      const a = angle + this.rng.range(-halfAngle, halfAngle);
      const r = radius * this.rng.range(0.6, 1);
      this.particles.burst(x - Math.sin(a) * r, y, z - Math.cos(a) * r, {
        count: 1,
        colors: [PALETTE.bread, PALETTE.breadCrust],
        speed: 1.5,
        size: 0.1,
        life: 0.5,
        lift: 1,
      });
    }
  }

  chainZap(points: Float32Array, count: number): void {
    this.chains.spawn(points, count);
    // Chispas en cada enemigo alcanzado.
    for (let i = 1; i < count; i++) {
      this.particles.burst(points[i * 3] as number, points[i * 3 + 1] as number, points[i * 3 + 2] as number, {
        count: 3,
        colors: [PALETTE.zap, PALETTE.zapCore],
        speed: 3,
        size: 0.09,
        life: 0.3,
      });
    }
  }
}
