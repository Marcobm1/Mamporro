// Parte visual de una partida: enemigos, proyectiles, gemas, aura, partículas y
// números de daño. Recibe los sucesos de la lógica (RunEffects) y los convierte
// en efectos; los avisos de interfaz y la sacudida se delegan en `hooks`.
import { Group, type Texture } from 'three';
import type { Run, RunEffects } from '../core/Run';
import { ENEMY_CAPACITY, GEM_CAPACITY, PROJECTILE_CAPACITY } from '../core/Run';
import { Rng } from '../core/rng';
import { ENEMY_LIST } from '../data/enemies';
import { auraRadius } from '../weapons/aura';
import type { WeaponInstance } from '../weapons';
import { AuraRenderer, GemRenderer, ProjectileRenderer } from './CombatRenderers';
import { DamageNumbers } from './DamageNumbers';
import { EnemyRenderer } from './EnemyRenderer';
import { PALETTE } from './palette';
import { Particles } from './Particles';

export interface RunViewHooks {
  onPlayerHit(damage: number): void;
  onLevelUp(level: number): void;
  onWeaponGained(weapon: WeaponInstance): void;
}

export class RunView implements RunEffects {
  readonly group = new Group();
  private readonly enemyRenderer: EnemyRenderer;
  private readonly projectileRenderer = new ProjectileRenderer(PROJECTILE_CAPACITY);
  private readonly gemRenderer = new GemRenderer(GEM_CAPACITY);
  private readonly aura = new AuraRenderer();
  private readonly particles: Particles;
  private readonly numbers: DamageNumbers;
  private run: Run | null = null;
  private lastAuraPulse = Number.POSITIVE_INFINITY;

  constructor(
    shadowTexture: Texture,
    private readonly hooks: RunViewHooks,
  ) {
    // Aleatoriedad solo visual: no afecta a la partida ni a la semilla.
    const rng = new Rng('efectos');
    this.enemyRenderer = new EnemyRenderer(ENEMY_CAPACITY, shadowTexture);
    this.particles = new Particles(1500, rng.derive('particulas'));
    this.numbers = new DamageNumbers(rng.derive('numeros'));
    this.group.add(
      this.enemyRenderer.group,
      this.projectileRenderer.mesh,
      this.gemRenderer.mesh,
      this.aura.group,
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
    this.group.visible = true;
  }

  detach(): void {
    this.run = null;
    this.group.visible = false;
  }

  update(alpha: number, frameDt: number, time: number): void {
    const run = this.run;
    if (!run) return;
    this.enemyRenderer.update(run.enemies, alpha);
    this.projectileRenderer.update(run.projectiles, alpha);
    this.gemRenderer.update(run.gems);

    const auraWeapon = run.weapons.find((w) => w.def.behavior === 'aura');
    if (auraWeapon) {
      const radius = auraRadius(auraWeapon.effective.area);
      this.aura.update(true, run.player.x, run.player.y, run.player.z, radius, auraWeapon.sincePulse, time);
      // Vapores de naftalina en cada pulso.
      if (auraWeapon.sincePulse < this.lastAuraPulse) {
        for (let k = 0; k < 3; k++) {
          const a = Math.random() * Math.PI * 2;
          const r = radius * (0.4 + Math.random() * 0.6);
          this.particles.burst(run.player.x + Math.cos(a) * r, run.player.y + 0.3, run.player.z + Math.sin(a) * r, {
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
      this.lastAuraPulse = auraWeapon.sincePulse;
    } else {
      this.aura.update(false, 0, 0, 0, 1, 0, time);
    }

    this.particles.update(frameDt);
    this.numbers.update(frameDt);
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

  levelUp(level: number): void {
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
    this.hooks.onLevelUp(level);
  }

  weaponGained(weapon: WeaponInstance): void {
    // El arma inicial se añade al crear la partida, antes de conectarla: sin aviso.
    if (this.run) this.hooks.onWeaponGained(weapon);
  }
}
