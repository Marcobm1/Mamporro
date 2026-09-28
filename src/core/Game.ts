import { AudioEngine } from '../audio/AudioEngine';
// Orquestador del juego: estados (inicio, jugando, elegir carta, pausa y
// resultados), bucle, entrada, mundo, jugador, partida, cámara, render e interfaz.
import { Color, DirectionalLight, Fog, HemisphereLight, Scene, type Mesh } from 'three';
import { purchase, purchaseExtra, settleRun } from '../systems/meta';
import { CHARACTERS } from '../data/characters';
import { PLAYER_BASE_STATS, PLAYER_TUNING, RENDER_CONFIG, WORLD_CONFIG } from '../data/config';
import { BOSS_CONFIG, ENEMIES, ENEMY_LIST, type EnemyId } from '../data/enemies';
import { ITEMS, type ItemId } from '../data/items';
import { TOTEM_CONFIG } from '../data/run';
import { TOMES, type TomeId } from '../data/tomes';
import type { WeaponId } from '../data/weapons';
import { PlayerBody, type PlayerIntent } from '../entities/playerPhysics';
import { PlayerView, type PlayerFrameEvents } from '../entities/PlayerView';
import { detectLanguage, setLanguage, t, type TranslationKey } from '../i18n';
import { CameraRig } from '../render/CameraRig';
import { PALETTE } from '../render/palette';
import { RetroRenderer } from '../render/RetroRenderer';
import { retroUniforms } from '../render/retroMaterial';
import { RunView } from '../render/RunView';
import { createSky, SUN_DIRECTION } from '../render/Sky';
import { createBlobShadowTexture, createDetailTexture, createStoneTexture } from '../render/textures';
import { browserStorage, SaveManager } from '../save/SaveManager';
import type { Settings } from '../save/schema';
import { stepPlayerInCrowd } from '../systems/crowd';
import { xpToNextLevel } from '../systems/progression';
import type { ItemStack } from '../systems/items';
import { describeCard, describeItem, statLines } from '../ui/cards';
import { loadPixelFont } from '../ui/font/pixelFont';
import { formatTime } from '../ui/Hud';
import type { MinimapMarker } from '../ui/Minimap';
import { UI, type ItemLine, type PauseRunInfo } from '../ui/UI';
import { World, type WorldTextures } from '../world/World';
import { GameLoop } from './GameLoop';
import { Input } from './Input';
import { damp, lerp, RAD2DEG, type Vec3Like } from './math';
import { normalizeSeed, randomSeed } from './rng';
import { Run, type RunNotice } from './Run';

export type GameState = 'loading' | 'title' | 'playing' | 'levelup' | 'paused' | 'gameover';

export interface GameOptions {
  /** Modo de pruebas automáticas: sin Pointer Lock y con ganchos en `window`. */
  testMode: boolean;
}

export type DebugAction = 'invincible' | 'level' | 'minute' | 'spawn' | 'kill' | 'boss' | 'gold' | 'reveal';

export interface RunInfo {
  time: number;
  kills: number;
  level: number;
  hp: number;
  enemies: number;
  projectiles: number;
  gems: number;
  weapons: string[];
  dead: boolean;
  /** Frenado actual por la horda (0..1). */
  crowdSlow: number;
  /** Velocidad horizontal del jugador (m/s). */
  speed: number;
  gold: number;
  timeLeft: number;
  swarm: boolean;
  victory: boolean;
  items: Array<{ id: string; count: number }>;
  chestsOpened: number;
  boss: { hp: number; maxHp: number; phase: string; attack: string | null } | null;
  prompt: { kind: string; cost: number } | null;
  challenge: number;
  enemyShots: number;
  coins: number;
  /** Cuántos enemigos hay de cada tipo. */
  enemyTypes: Record<string, number>;
}

/** Ganchos para las pruebas automáticas en navegador (solo con `?test`). */
export interface TestHooks {
  audio(): { state: string; voices: number; mode: string; tracks: number };
  state(): GameState;
  seed(): string;
  start(seed?: string): void;
  pause(): void;
  resume(): void;
  retry(): void;
  press(code: string, down: boolean): void;
  setView(yaw: number, pitch: number, distanceScale?: number): void;
  setDebug(visible: boolean): void;
  debug(action: DebugAction): void;
  player(): { x: number; y: number; z: number; speed: number; grounded: boolean; sliding: boolean };
  run(): RunInfo | null;
  loopStats(): { frameMs: number; updateMs: number; renderMs: number; ticks: number; drawCalls: number };
  sites(): Array<{ kind: string; x: number; z: number; rotation: number }>;
  teleport(x: number, z: number): void;
  /** Quita vida al jugador (para probar el game over sin esperar). */
  hurtPlayer(amount: number): void;
  /** Elección abierta (subida de nivel o santuario) o null. */
  levelUp(): { source: string; pending: number; cards: Array<{ kind: string; key: string | null; rarity?: string }> } | null;
  choose(index: number): void;
  reroll(): void;
  skip(): void;
  banish(index: number): void;
  addWeapon(id: WeaponId): boolean;
  addTome(id: TomeId): boolean;
  addItem(id: ItemId): void;
  /** Usa lo que el jugador tenga delante (como la tecla E). */
  interact(): boolean;
  /** Interactuables del mapa con su estado. */
  interactables(): Array<{ kind: string; x: number; y: number; z: number; discovered: boolean; used: boolean; charge: number }>;
  /** Hace aparecer un enemigo a (dx, dz) del jugador. */
  spawnEnemy(id: EnemyId, dx: number, dz: number): boolean;
  /** Enemigos vivos con su tipo, posición, estado y vida. */
  enemies(): Array<{ id: string; x: number; z: number; state: number; hp: number }>;
  /** Activa o desactiva las armas del jugador. */
  setWeapons(on: boolean): void;
  /** Congela los efectos visuales (no envejecen) para poder fotografiar los de un instante. */
  freezeEffects(frozen: boolean): void;
}

declare global {
  interface Window {
    __MAMPORRO__?: TestHooks;
  }
}

const ENEMY_IDS = ENEMY_LIST.map((def) => def.id);
const TITLE_ORBIT_SPEED = 0.12;
const DEBUG_SPAWN_COUNT = 100;
/** Segundos entre derrotar al jefe y la pantalla de resultados (para verlo reventar). */
const VICTORY_DELAY = 1.6;

export class Game {
  private readonly audio = new AudioEngine();
  private readonly save: SaveManager;
  private readonly input: Input;
  private readonly renderer: RetroRenderer;
  private readonly rig: CameraRig;
  private readonly body = new PlayerBody();
  private readonly scene = new Scene();
  private readonly loop: GameLoop;
  private readonly ui: UI;
  private readonly textures: WorldTextures;
  private readonly playerView: PlayerView;
  private readonly runView: RunView;
  private readonly sky: Mesh;
  private world: World;
  private seed: string;
  private state: GameState = 'loading';
  private run: Run | null = null;
  /** Solo pruebas: los efectos visuales no envejecen. */
  private effectsFrozen = false;
  private victoryDelay = VICTORY_DELAY;
  private runId = '';
  private runSettled = false;

  private readonly prev = { x: 0, y: 0, z: 0 };
  /** Posición interpolada del jugador en el frame actual (la que se dibuja). */
  private readonly drawPos = { x: 0, y: 0, z: 0 };
  private readonly mouse = { dx: 0, dy: 0 };
  private readonly forward: Vec3Like = { x: 0, y: 0, z: 0 };
  private readonly right: Vec3Like = { x: 0, y: 0, z: 0 };
  private readonly groundNormal: Vec3Like = { x: 0, y: 1, z: 0 };
  private readonly frameEvents: PlayerFrameEvents = { landed: false, landingSpeed: 0 };
  private fps = 60;
  private fpsTimer = 0;
  private debugTimer = 0;

  constructor(
    canvas: HTMLCanvasElement,
    uiRoot: HTMLElement,
    private readonly options: GameOptions,
  ) {
    this.save = new SaveManager(browserStorage(), detectLanguage(navigator.languages ?? [navigator.language]));
    setLanguage(this.save.settings.language);

    this.renderer = new RetroRenderer(canvas);
    this.input = new Input(canvas);
    this.rig = new CameraRig(this.renderer.aspect);
    this.textures = { detail: createDetailTexture(), stone: createStoneTexture() };
    const shadowTexture = createBlobShadowTexture();
    this.playerView = new PlayerView(shadowTexture);
    this.playerView.setCharacter(this.save.data.meta.selected);
    this.runView = new RunView(shadowTexture, {
      onSound: (id) => this.audio.play(id),
      onPlayerHit: () => {
        this.rig.shake(0.45);
        this.ui.hud.flashHurt();
      },
      onNotice: (notice) => this.showNotice(notice),
      onItem: (item) => this.ui.hud.showItem(describeItem(item)),
      onShake: (amount) => this.rig.shake(amount),
    });
    this.sky = createSky();
    this.seed = randomSeed();
    this.world = new World(this.seed, this.textures);
    this.setupScene();

    this.ui = new UI(
      uiRoot,
      { meta: () => this.save.data.meta, canSave: () => this.save.canSave, settings: () => this.save.settings, seed: () => this.seed, runInfo: () => this.pauseRunInfo() },
      {
        onSelectCharacter: (id) => {
          if (this.state !== 'title' || !this.save.data.meta.characters.includes(id)) return;
          this.save.data.meta.selected = id;
          this.playerView.setCharacter(id);
          this.save.persist();
          this.ui.refresh();
        },
        onPurchase: (id) => {
          if (this.state !== 'title') return;
          if (purchase(this.save.data.meta, id)) { this.save.persist(); this.audio.play('reward'); }
          this.ui.refresh();
        },
        onPurchaseExtra: (action) => {
          if (this.state !== 'title') return;
          if (purchaseExtra(this.save.data.meta, action)) { this.save.persist(); this.audio.play('reward'); }
          this.ui.refresh();
        },
        onPlay: (seedText) => this.startRun(seedText),
        onNewMap: () => this.regenerateWorld(randomSeed()),
        onResume: () => this.resume(),
        onBackToTitle: () => this.backToTitle(),
        onRetry: () => this.retry(false),
        onRetryNewMap: () => this.retry(true),
        onSettingsChange: (patch) => this.changeSettings(patch),
        onChoose: (index) => this.levelUpAction((run) => run.choose(index), true),
        onReroll: () => this.levelUpAction((run) => run.reroll(), false),
        onSkip: () => this.levelUpAction((run) => run.skip(), true),
        onBanish: (index) => this.levelUpAction((run) => run.banish(index), false),
      },
    );
    this.updateMinimapTerrain();
    this.loop = new GameLoop({
      update: (dt) => this.update(dt),
      render: (alpha, frameDt) => this.draw(alpha, frameDt),
    });

    this.bindEvents();
    this.applySettings(this.save.settings);
    this.resetPlayer();
  }

  async boot(): Promise<void> {
    this.loop.start();
    await loadPixelFont();
    this.state = 'title';
    this.ui.show('title');
    if (this.options.testMode) this.exposeTestHooks();
  }

  // ---------------------------------------------------------------- escena

  private setupScene(): void {
    const fogColor = new Color(PALETTE.skyHorizon);
    this.scene.background = fogColor;
    this.scene.fog = new Fog(fogColor, RENDER_CONFIG.fogNear, RENDER_CONFIG.fogFar);

    const hemi = new HemisphereLight(PALETTE.hemiSky, PALETTE.hemiGround, 1.4);
    const sun = new DirectionalLight(PALETTE.sunLight, 2.4);
    sun.position.copy(SUN_DIRECTION).multiplyScalar(100);
    this.scene.add(hemi, sun, sun.target, this.sky, this.world.group, this.runView.group, ...this.playerView.objects);
  }

  private regenerateWorld(seed: string): void {
    this.world.dispose();
    this.seed = seed;
    this.world = new World(seed, this.textures);
    this.scene.add(this.world.group);
    this.resetPlayer();
    this.updateMinimapTerrain();
    this.ui.refresh();
  }

  private updateMinimapTerrain(): void {
    this.ui.hud.minimap.setTerrain(this.world.heightfield, this.world.sites, WORLD_CONFIG.playableRadius);
  }

  private resetPlayer(): void {
    const spawn = this.world.spawnPoint;
    this.body.placeAt(spawn.x, spawn.y, spawn.z);
    this.body.facing = this.rig.yaw;
    this.prev.x = spawn.x;
    this.prev.y = spawn.y;
    this.prev.z = spawn.z;
    this.rig.snap();
  }

  // ---------------------------------------------------------------- estados

  /** Prepara una partida nueva en el mapa actual. */
  private beginRun(): void {
    this.resetPlayer();
    const meta = this.save.data.meta;
    this.runId = crypto.randomUUID();
    this.runSettled = false;
    this.playerView.setCharacter(meta.selected);
    this.run = new Run(this.world.collision, this.seed, CHARACTERS[meta.selected], this.runView, {
      allowedWeapons: meta.weapons,
      allowedItems: meta.items,
      extras: meta.extras,
      minutes: this.save.settings.runMinutes,
      interactables: this.world.interactables,
    });
    this.runView.attach(this.run);
    this.victoryDelay = VICTORY_DELAY;
    this.ui.hud.invalidate();
  }

  private endRun(): void {
    this.run = null;
    this.runView.detach();
    this.ui.hud.setVisible(false);
  }

  private startRun(seedText: string): void {
    if (this.state !== 'title') return;
    const seed = normalizeSeed(seedText);
    if (seed && seed !== this.seed) this.regenerateWorld(seed);
    this.beginRun();
    this.capturePointer();
  }

  private retry(newMap: boolean): void {
    if (this.state !== 'gameover') return;
    if (newMap) this.regenerateWorld(randomSeed());
    this.beginRun();
    this.capturePointer();
  }

  /** Captura el ratón; al conseguirlo se entra en juego (en modo pruebas, directamente). */
  private capturePointer(): void {
    if (this.options.testMode) {
      this.input.freeLook = true;
      this.enterPlaying();
      return;
    }
    void this.input.requestPointerLock();
  }

  private enterPlaying(): void {
    this.state = 'playing';
    this.ui.show('none');
    this.ui.hud.setVisible(true);
  }

  private pause(): void {
    if (this.state !== 'playing') return;
    this.state = 'paused';
    this.ui.show('pause');
    this.input.exitPointerLock();
  }

  private resume(): void {
    if (this.state !== 'paused') return;
    this.capturePointer();
  }

  /** Pausa la partida para elegir carta; el ratón se suelta para poder hacer clic. */
  private enterLevelUp(): void {
    this.state = 'levelup';
    // Soltarlo desde el juego (y no con Esc) permite recapturarlo al elegir sin esperas.
    this.input.exitPointerLock();
    this.showLevelUp(true);
  }

  private showLevelUp(fresh: boolean): void {
    const run = this.run;
    if (!run?.offer) return;
    const owned = {
      weapon: (id: string) => run.weapons.find((w) => w.def.id === id)?.level ?? 0,
      tome: (id: string) => run.tomes.find((tm) => tm.def.id === id)?.level ?? 0,
    };
    const shrine = run.offerSource === 'shrine';
    const pending = run.pendingLevelUps + run.pendingShrines - 1;
    this.ui.showLevelUp(
      {
        title: shrine ? t('shrine.title') : t('levelup.title', { n: run.level - run.pendingLevelUps + 1 }),
        subtitle: shrine ? t('shrine.subtitle') : t('levelup.subtitle'),
        pending,
        cards: run.offer.map((card) => describeCard(card, owned)),
        actions: shrine ? null : { rerolls: run.rerolls, skips: run.skips, banishes: run.banishes },
      },
      fresh,
    );
  }

  /**
   * Elegir, saltar, cambiar o descartar (solo con la subida de nivel abierta).
   * Después: la siguiente subida pendiente o la vuelta al juego. `closes` indica
   * si la acción cierra esta subida (elegir, saltar) o solo cambia sus cartas.
   */
  private levelUpAction(action: (run: Run) => boolean, closes: boolean): void {
    const run = this.run;
    if (!run || this.state !== 'levelup' || !action(run)) return;
    if (run.offer) {
      this.showLevelUp(closes);
      return;
    }
    this.ui.show('none');
    this.capturePointer();
  }

  /** Estadísticas y objetos del personaje para la pausa. */
  private pauseRunInfo(): PauseRunInfo | null {
    const run = this.run;
    return run ? { stats: statLines(run.stats, run.hp), items: this.itemLines(run.items) } : null;
  }

  private itemLines(items: readonly ItemStack[]): ItemLine[] {
    return items.map((stack) => {
      const view = describeItem(stack.def);
      return { name: view.title, count: stack.count, tone: view.tone, description: [...view.lines, view.description].join(' · ') };
    });
  }

  /** Fin de la partida: resultados de la victoria (jefe derrotado) o de la derrota. */
  private finishRun(victory: boolean): void {
    const run = this.run;
    if (!run || this.runSettled) return;
    this.runSettled = true;
    const receipt = settleRun(this.save.data.meta, {
      id: this.runId, cheated: run.cheated, time: run.time, kills: run.kills, chests: run.chestsOpened,
      shrines: run.shrinesCompleted, challenges: run.challengesCompleted, level: run.level, victory, usedLifeTome: run.usedLifeTome,
    });
    this.save.persist();
    this.audio.clearEffects();
    this.audio.play(victory ? 'victory' : 'defeat');
    this.state = 'gameover';
    this.input.exitPointerLock();
    this.ui.hud.setVisible(false);
    this.ui.showResults({
      meta: receipt,
      victory,
      time: run.time,
      kills: run.kills,
      level: run.level,
      gold: run.goldCollected,
      chests: run.chestsOpened,
      items: this.itemLines(run.items),
      weapons: run.weapons.map((w) => ({ name: t(w.def.nameKey), damage: w.totalDamage })),
      seed: this.seed,
      cheated: run.cheated,
    });
  }

  /** Texto de cada aviso de la partida (y si va en grande). */
  private showNotice(notice: RunNotice): void {
    const hud = this.ui.hud;
    switch (notice.kind) {
      case 'wave':
        hud.notice(t(notice.key), true);
        break;
      case 'elite':
        hud.notice(t('notice.elite', { name: t(ENEMIES[notice.enemy].nameKey) }), true);
        break;
      case 'swarm':
        hud.notice(t('notice.swarm'), true);
        break;
      case 'portalFound':
        hud.notice(t('notice.portalFound'), true);
        break;
      case 'portalRevealed':
        hud.notice(t('notice.portalRevealed'));
        break;
      case 'boss':
        hud.notice(t('notice.boss', { name: t(ENEMIES[notice.enemy].nameKey) }), true);
        break;
      case 'noGold':
        hud.notice(t('notice.noGold', { n: notice.missing }));
        break;
      case 'challengeStart':
        hud.notice(t('notice.challengeStart'), true);
        break;
      case 'challengeDone':
        hud.notice(t('notice.challengeDone'), true);
        break;
      case 'shrineCharged':
        hud.notice(t('notice.shrineCharged'));
        break;
      case 'shield':
        hud.notice(t('notice.shield'));
        break;
      case 'revive':
        hud.notice(t('notice.revive'), true);
        break;
    }
  }

  private backToTitle(): void {
    this.state = 'title';
    this.input.exitPointerLock();
    this.endRun();
    this.resetPlayer();
    this.ui.show('title');
  }

  private bindEvents(): void {
    document.addEventListener('pointerdown', () => this.audio.unlock());
    document.addEventListener('keydown', () => this.audio.unlock());
    document.addEventListener('click', (event) => {
      if (event.target instanceof Element && event.target.closest('button')) this.audio.play('ui');
    });
    this.input.onPointerLockChange((locked) => {
      if (locked && (this.state === 'title' || this.state === 'paused' || this.state === 'gameover')) this.enterPlaying();
      else if (locked && this.state === 'levelup' && !this.run?.offer) this.enterPlaying();
      else if (!locked && this.state === 'playing') this.pause();
    });
    this.input.onPointerLockError(() => {
      if (this.state !== 'playing') this.ui.toast(t('error.pointerLock'));
      // Si no se pudo recapturar tras elegir carta, se pasa a la pausa para volver con un clic.
      if (this.state === 'levelup' && !this.run?.offer) {
        this.state = 'paused';
        this.ui.show('pause');
      }
    });
    this.input.onKey((code) => {
      if (code === 'F3') this.ui.setDebugVisible(!this.ui.debugVisible);
      if (this.state === 'levelup') {
        this.ui.levelUpKey(code);
        return;
      }
      // Con el ratón capturado, Esc lo gestiona el navegador (y pausamos al perder
      // la captura); esto cubre el caso de jugar sin captura.
      if (code === 'Escape') this.pause();
      if (this.ui.debugVisible && this.state === 'playing') this.handleDebugKey(code);
    });
    document.addEventListener('visibilitychange', () => {
      this.audio.setHidden(document.hidden);
      if (document.hidden) this.pause();
    });
    window.addEventListener('blur', () => {
      if (!this.options.testMode) this.pause();
    });
    // Red de seguridad: con una partida en marcha, el navegador pide confirmación
    // antes de cerrar la pestaña (p. ej. por un Ctrl+W accidental).
    window.addEventListener('beforeunload', (event) => {
      if (this.state === 'playing' || this.state === 'levelup' || this.state === 'paused') {
        event.preventDefault();
        event.returnValue = '';
      }
    });
  }

  private handleDebugKey(code: string): void {
    const actions: Record<string, DebugAction> = {
      Digit1: 'invincible',
      Digit2: 'level',
      Digit3: 'minute',
      Digit4: 'spawn',
      Digit5: 'kill',
      Digit6: 'boss',
      Digit7: 'gold',
      Digit8: 'reveal',
    };
    const action = actions[code];
    if (action) this.debugAction(action);
  }

  private debugAction(action: DebugAction): void {
    const run = this.run;
    if (!run) return;
    switch (action) {
      case 'invincible':
        this.ui.toast(t(run.debugToggleInvincible() ? 'debug.invincibleOn' : 'debug.invincibleOff'));
        break;
      case 'level':
        run.debugLevelUp();
        this.ui.toast(t('debug.levelUp'));
        break;
      case 'minute':
        run.debugSkipMinute();
        this.ui.toast(t('debug.skipTime'));
        break;
      case 'spawn':
        run.debugSpawn(DEBUG_SPAWN_COUNT);
        this.ui.toast(t('debug.spawn'));
        break;
      case 'kill':
        run.debugKillAll();
        this.ui.toast(t('debug.killAll'));
        break;
      case 'boss':
        this.ui.toast(t(run.debugSummonBoss() ? 'debug.boss' : 'debug.bossBusy'));
        break;
      case 'gold':
        run.debugAddGold(100);
        this.ui.toast(t('debug.gold'));
        break;
      case 'reveal':
        run.debugRevealMap();
        this.ui.toast(t('debug.reveal'));
        break;
    }
  }

  // ---------------------------------------------------------------- ajustes

  private changeSettings(patch: Partial<Settings>): void {
    this.applySettings(this.save.updateSettings(patch));
  }

  private applySettings(settings: Settings): void {
    this.audio.configure(settings);
    this.renderer.setTargetHeight(settings.renderHeight);
    this.rig.setAspect(this.renderer.aspect);
    this.renderer.setVertexSnap(settings.vertexSnap);
    this.renderer.setDithering(settings.dithering);
    this.rig.sensitivity = settings.mouseSensitivity;
    this.input.setSlideWithCtrl(settings.slideWithCtrl);
    this.ui.setFpsVisible(settings.showFps);
    setLanguage(settings.language);
  }

  // ---------------------------------------------------------------- bucle

  private get moveSpeed(): number {
    return PLAYER_BASE_STATS.moveSpeed * (this.run?.stats.moveSpeed ?? 1);
  }

  private readIntent(): PlayerIntent {
    const f = this.rig.forward(this.forward);
    const r = this.rig.right(this.right);
    let x = 0;
    let z = 0;
    if (this.input.isDown('forward')) {
      x += f.x;
      z += f.z;
    }
    if (this.input.isDown('back')) {
      x -= f.x;
      z -= f.z;
    }
    if (this.input.isDown('right')) {
      x += r.x;
      z += r.z;
    }
    if (this.input.isDown('left')) {
      x -= r.x;
      z -= r.z;
    }
    const len = Math.hypot(x, z);
    if (len > 1) {
      x /= len;
      z /= len;
    }
    return {
      moveX: x,
      moveZ: z,
      jumpPressed: this.input.wasPressed('jump'),
      jumpHeld: this.input.isDown('jump'),
      slidePressed: this.input.wasPressed('slide'),
      slideHeld: this.input.isDown('slide'),
    };
  }

  private update(dt: number): void {
    if (this.state === 'playing') {
      this.prev.x = this.body.x;
      this.prev.y = this.body.y;
      this.prev.z = this.body.z;
      // La horda que está atravesando la frena un poco (ver CROWD_CONFIG).
      const crowdSlow = this.run?.crowdSlow ?? 0;
      stepPlayerInCrowd(this.body, this.readIntent(), this.world.collision, PLAYER_TUNING, this.moveSpeed, crowdSlow, dt);
      const ev = this.body.events;
      if (ev.landed) {
        this.frameEvents.landed = true;
        this.frameEvents.landingSpeed = Math.max(this.frameEvents.landingSpeed, ev.landingSpeed);
      }
      const run = this.run;
      if (run) {
        if (this.input.wasPressed('interact')) run.interact();
        run.update(dt, this.body, this.rig.yaw);
        if (run.dead) {
          this.finishRun(false);
        } else if (run.victory) {
          this.victoryDelay -= dt;
          if (this.victoryDelay <= 0) this.finishRun(true);
        } else if (run.openChoice()) {
          this.enterLevelUp();
        }
      }
    }
    this.input.endTick();
  }

  private draw(alpha: number, frameDt: number): void {
    this.audio.setMode(this.state === 'playing' ? (this.run?.boss || this.run?.swarm ? 'intense' : 'playing') :
      this.state === 'paused' || this.state === 'levelup' ? 'paused' : this.state === 'gameover' ? 'results' : 'menu');
    if (this.renderer.resize()) this.rig.setAspect(this.renderer.aspect);

    this.input.consumeMouse(this.mouse);
    if (this.state === 'playing') {
      this.rig.look(this.mouse.dx, this.mouse.dy);
    } else if (this.state === 'title' || this.state === 'loading') {
      this.rig.yaw += frameDt * TITLE_ORBIT_SPEED;
      this.rig.pitch = lerp(this.rig.pitch, -0.22, damp(2, frameDt));
      this.body.facing = this.rig.yaw;
    }

    // Posición interpolada entre los dos últimos pasos de lógica.
    const a = this.state === 'playing' ? alpha : 1;
    const x = lerp(this.prev.x, this.body.x, a);
    const y = lerp(this.prev.y, this.body.y, a);
    const z = lerp(this.prev.z, this.body.z, a);
    const collision = this.world.collision;
    const groundY = collision.groundHeight(x, z, y + 0.5);
    collision.groundNormal(x, z, y + 0.5, this.groundNormal);
    const now = performance.now() / 1000;

    this.playerView.update(
      { x, y, z, facing: this.body.facing, groundY, groundNormal: this.groundNormal },
      this.body,
      this.frameEvents,
      frameDt,
    );
    this.frameEvents.landed = false;
    this.frameEvents.landingSpeed = 0;
    // Parpadea mientras es invulnerable tras un golpe.
    const run = this.run;
    this.playerView.root.visible = !run || run.invulnerable <= 0 || Math.floor(now * 16) % 2 === 0;

    this.rig.update(
      { x, y, z },
      { sliding: this.body.sliding, speed: this.body.horizontalSpeed, moveSpeed: this.moveSpeed },
      this.world.heightfield,
      frameDt,
    );
    this.sky.position.copy(this.rig.camera.position);
    retroUniforms.uTime.value = now;
    this.world.update(now, run?.interactables.list ?? null);
    this.drawPos.x = x;
    this.drawPos.y = y;
    this.drawPos.z = z;
    const effectsDt = this.state === 'playing' && !this.effectsFrozen ? frameDt : 0;
    this.runView.update(this.state === 'playing' ? alpha : 1, effectsDt, now, this.drawPos);
    if (run) this.updateHud(run, x, z, now);
    this.renderer.render(this.scene, this.rig.camera);
    this.updateStats(frameDt);
  }

  private updateHud(run: Run, x: number, z: number, now: number): void {
    const boss = run.bossHealth;
    const passive = CHARACTERS[this.save.data.meta.selected].passive;
    this.ui.hud.update({
      passive: passive.kind === 'shield' ? (run.shieldCharge >= passive.recharge ? t('passive.shieldReady') : t('passive.shieldCharge', { n: Math.ceil(passive.recharge - run.shieldCharge) })) : '',
      hp: run.hp,
      maxHp: run.stats.maxHp,
      level: run.level,
      xp: run.progress.xp,
      xpNext: xpToNextLevel(run.level),
      gold: run.gold,
      timeLeft: run.timeLeft,
      swarm: run.swarm,
      kills: run.kills,
      weapons: run.weapons.map((w) => ({ name: t(w.def.nameKey), level: w.level })),
      tomes: run.tomes.map((tm) => ({ name: t(tm.def.shortKey), level: tm.level })),
      items: run.items.map((stack) => ({ name: t(stack.def.nameKey), count: stack.count, tone: stack.def.rarity })),
      boss: boss ? { name: t(ENEMIES[BOSS_CONFIG.enemy].nameKey), hp: boss.hp, maxHp: boss.maxHp, enraged: boss.enraged } : null,
      prompt: this.promptText(run),
      progress: this.progressInfo(run),
    });
    const markers: MinimapMarker[] = [];
    for (const item of run.interactables.list) {
      if (item.discovered) markers.push({ kind: item.spot.kind, x: item.spot.x, z: item.spot.z, used: item.used });
    }
    let bossPos: { x: number; z: number } | null = null;
    if (run.boss) {
      const i = run.enemies.indexOfId(run.boss.enemyId);
      if (i >= 0) bossPos = { x: run.enemies.x[i] as number, z: run.enemies.z[i] as number };
    }
    this.ui.hud.minimap.update({ playerX: x, playerZ: z, yaw: this.rig.yaw, markers, boss: bossPos }, now * 1000);
  }

  /** Texto de lo que se puede usar delante (baúl, tótem, portal). */
  private promptText(run: Run): string | null {
    const prompt = run.prompt;
    switch (prompt?.kind) {
      case 'chest':
        return t('prompt.chest', { cost: prompt.cost });
      case 'totem':
        return t('prompt.totem', { s: TOTEM_CONFIG.duration });
      case 'portal':
        return t('prompt.portal');
      default:
        return null;
    }
  }

  /** Barra de progreso: la mesa camilla cargándose o el desafío del tótem en marcha. */
  private progressInfo(run: Run): { label: string; value: number } | null {
    const shrine = run.interactables.list[run.interactables.charging];
    if (shrine) return { label: t('hud.shrine', { n: Math.floor(shrine.charge * 100) }), value: shrine.charge };
    const left = run.interactables.challenge;
    if (left > 0) return { label: t('hud.challenge', { time: formatTime(Math.ceil(left)) }), value: left / TOTEM_CONFIG.duration };
    return null;
  }

  private updateStats(frameDt: number): void {
    if (frameDt > 0) this.fps = lerp(this.fps, 1 / frameDt, 0.08);
    this.fpsTimer += frameDt;
    if (this.fpsTimer >= 0.25) {
      this.fpsTimer = 0;
      this.ui.updateFps(this.fps);
    }
    this.debugTimer += frameDt;
    if (this.debugTimer >= 0.1 && this.ui.debugVisible) {
      this.debugTimer = 0;
      const b = this.body;
      let stateKey: TranslationKey = 'debug.state.airborne';
      if (b.sliding) stateKey = 'debug.state.sliding';
      else if (b.grounded) stateKey = 'debug.state.grounded';
      else if (b.onSteep) stateKey = 'debug.state.steep';
      const info = this.renderer.gl.info.render;
      const run = this.run;
      this.ui.updateDebug({
        fps: this.fps,
        frameMs: this.loop.frameMs,
        logicMs: this.loop.updateMs,
        renderMs: this.loop.renderMs,
        drawCalls: info.calls,
        triangles: info.triangles,
        internalWidth: this.renderer.internalWidth,
        internalHeight: this.renderer.internalHeight,
        pixelScale: this.renderer.pixelScale,
        x: b.x,
        y: b.y,
        z: b.z,
        speed: b.horizontalSpeed,
        state: stateKey,
        slopeDeg: Math.acos(Math.min(1, b.normal.y)) * RAD2DEG,
        seed: this.seed,
        run: run
          ? {
              enemies: run.enemies.count,
              projectiles: run.projectiles.count,
              gems: run.gems.count,
              particles: this.runView.particleCount,
              time: run.time,
              hp: run.hp,
              xp: run.progress.xp,
              xpNext: xpToNextLevel(run.level),
              slow: run.crowdSlow,
              ...this.directorInfo(run),
            }
          : null,
      });
    }
  }

  private directorInfo(run: Run): { difficulty: number; spawnRate: number; maxAlive: number; gold: number } {
    const params = run.director.spawnParams(run.time, { rate: 1, hp: 1, gold: 1 }, { minutes: 0, rate: 0, maxAlive: 0, hp: 1, xp: 1, gold: 1 });
    return { difficulty: run.difficulty, spawnRate: params.rate, maxAlive: params.maxAlive, gold: run.gold };
  }

  // ---------------------------------------------------------------- pruebas

  private exposeTestHooks(): void {
    window.__MAMPORRO__ = {
      audio: () => this.audio.stats,
      state: () => this.state,
      seed: () => this.seed,
      start: (seed = '') => this.startRun(seed),
      pause: () => this.pause(),
      resume: () => this.resume(),
      retry: () => this.retry(false),
      press: (code, down) => this.input.simulateKey(code, down),
      setView: (yaw, pitch, distanceScale = 1) => {
        this.rig.yaw = yaw;
        this.rig.pitch = pitch;
        this.rig.distanceScale = distanceScale;
      },
      setDebug: (visible) => this.ui.setDebugVisible(visible),
      debug: (action) => this.debugAction(action),
      player: () => ({
        x: this.body.x,
        y: this.body.y,
        z: this.body.z,
        speed: this.body.horizontalSpeed,
        grounded: this.body.grounded,
        sliding: this.body.sliding,
      }),
      run: () => {
        const run = this.run;
        if (!run) return null;
        return {
          time: run.time,
          kills: run.kills,
          level: run.level,
          hp: run.hp,
          enemies: run.enemies.count,
          projectiles: run.projectiles.count,
          gems: run.gems.count,
          weapons: run.weapons.map((w) => w.def.id),
          dead: run.dead,
          crowdSlow: run.crowdSlow,
          speed: this.body.horizontalSpeed,
          gold: run.gold,
          timeLeft: run.timeLeft,
          swarm: run.swarm,
          victory: run.victory,
          items: run.items.map((s) => ({ id: s.def.id, count: s.count })),
          chestsOpened: run.chestsOpened,
          boss: (() => {
            const health = run.bossHealth;
            return health && run.boss ? { hp: health.hp, maxHp: health.maxHp, phase: run.boss.phase, attack: run.boss.attack } : null;
          })(),
          prompt: run.prompt ? { kind: run.prompt.kind, cost: run.prompt.cost } : null,
          challenge: run.interactables.challenge,
          enemyShots: run.enemyShots.count,
          coins: run.coins.count,
          enemyTypes: (() => {
            const counts: Record<string, number> = {};
            for (let i = 0; i < run.enemies.count; i++) {
              const id = ENEMY_IDS[run.enemies.type[i] as number] ?? '?';
              counts[id] = (counts[id] ?? 0) + 1;
            }
            return counts;
          })(),
        };
      },
      loopStats: () => ({
        frameMs: this.loop.frameMs,
        updateMs: this.loop.updateMs,
        renderMs: this.loop.renderMs,
        ticks: this.loop.ticksThisFrame,
        drawCalls: this.renderer.gl.info.render.calls,
      }),
      sites: () => this.world.sites.map((site) => ({ kind: site.kind, x: site.x, z: site.z, rotation: site.rotation })),
      hurtPlayer: (amount) => {
        if (this.run) { this.run.cheated = true; this.run.hp = Math.max(0, this.run.hp - amount); }
      },
      teleport: (x, z) => {
        if (this.run) this.run.cheated = true;
        const y = this.world.collision.groundHeight(x, z, Number.POSITIVE_INFINITY);
        this.body.placeAt(x, y, z);
        this.prev.x = x;
        this.prev.y = y;
        this.prev.z = z;
      },
      levelUp: () => {
        const run = this.run;
        if (!run?.offer || this.state !== 'levelup') return null;
        return {
          source: run.offerSource,
          pending: run.pendingLevelUps + run.pendingShrines - 1,
          cards: run.offer.map((c) => ({ kind: c.kind, key: c.key, rarity: 'rarity' in c ? c.rarity : undefined })),
        };
      },
      choose: (index) => this.levelUpAction((run) => run.choose(index), true),
      reroll: () => this.levelUpAction((run) => run.reroll(), false),
      skip: () => this.levelUpAction((run) => run.skip(), true),
      banish: (index) => this.levelUpAction((run) => run.banish(index), false),
      addWeapon: (id) => this.run?.debugAddWeapon(id) ?? false,
      addTome: (id) => (TOMES[id] ? (this.run?.debugAddTome(id) ?? false) : false),
      addItem: (id) => {
        if (ITEMS[id]) this.run?.debugAddItem(id);
      },
      interact: () => this.run?.interact() ?? false,
      interactables: () =>
        (this.run?.interactables.list ?? []).map((item) => ({
          kind: item.spot.kind,
          x: item.spot.x,
          y: item.spot.y,
          z: item.spot.z,
          discovered: item.discovered,
          used: item.used,
          charge: item.charge,
        })),
      spawnEnemy: (id, dx, dz) => (this.run ? this.run.debugSpawnEnemy(id, this.body.x + dx, this.body.z + dz) >= 0 : false),
      enemies: () => {
        const run = this.run;
        if (!run) return [];
        const e = run.enemies;
        const out: Array<{ id: string; x: number; z: number; state: number; hp: number }> = [];
        for (let i = 0; i < e.count; i++) {
          out.push({ id: ENEMY_IDS[e.type[i] as number] ?? '?', x: e.x[i] as number, z: e.z[i] as number, state: e.state[i] as number, hp: e.hp[i] as number });
        }
        return out;
      },
      setWeapons: (on) => {
        if (this.run) { this.run.cheated = true; this.run.weaponsOff = !on; }
      },
      freezeEffects: (frozen) => {
        this.effectsFrozen = frozen;
      },
    };
  }
}
