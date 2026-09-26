// Parte visual de una partida: enemigos, proyectiles, gemas, efectos de las armas,
// partículas y números de daño. Recibe los sucesos de la lógica (RunEffects) y los
// convierte en efectos; los avisos de interfaz y la sacudida se delegan en `hooks`.
import { Group, type Texture } from 'three';
import type { Vec3Like } from '../core/math';
import type { Run, RunEffects } from '../core/Run';
import { ENEMY_CAPACITY, GEM_CAPACITY, PROJECTILE_CAPACITY } from '../core/Run';
import { Rng } from '../core/rng';
import { ENEMY_LIST } from '../data/enemies';
import { auraRadius } from '../weapons/aura';
import { orbitGeometry } from '../weapons/orbit';
import type { OrbitState, TrailState } from '../weapons/types';
import { AuraRenderer, GemRenderer, ProjectileRenderer } from './CombatRenderers';
import { DamageNumbers } from './DamageNumbers';
import { EnemyRenderer } from './EnemyRenderer';
import { PALETTE } from './palette';
import { Particles } from './Particles';
import { ArcRenderer, ChainRenderer, OrbitRenderer, TrailRenderer } from './WeaponEffects';

export interface RunViewHooks {
  onPlayerHit(damage: number): void;
}

export class RunView implements RunEffects {
  readonly group = new Group();
  private readonly enemyRenderer: EnemyRenderer;
  private readonly projectileRenderer = new ProjectileRenderer(PROJECTILE_CAPACITY);
  private readonly gemRenderer = new GemRenderer(GEM_CAPACITY);
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
    this.enemyRenderer.update(run.enemies, alpha);
    this.projectileRenderer.update(run.projectiles, alpha);
    this.gemRenderer.update(run.gems);

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
