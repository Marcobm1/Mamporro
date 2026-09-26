// Orquestador del juego: estados (inicio, jugando, pausa, game over), bucle,
// entrada, mundo, jugador, partida, cámara, render e interfaz.
import { Color, DirectionalLight, Fog, HemisphereLight, Scene, type Mesh } from 'three';
import { CHARACTERS } from '../data/characters';
import { PLAYER_BASE_STATS, PLAYER_TUNING, RENDER_CONFIG } from '../data/config';
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
import { loadPixelFont } from '../ui/font/pixelFont';
import { UI } from '../ui/UI';
import { World, type WorldTextures } from '../world/World';
import { GameLoop } from './GameLoop';
import { Input } from './Input';
import { damp, lerp, RAD2DEG, type Vec3Like } from './math';
import { normalizeSeed, randomSeed } from './rng';
import { Run } from './Run';

export type GameState = 'loading' | 'title' | 'playing' | 'paused' | 'gameover';

export interface GameOptions {
  /** Modo de pruebas automáticas: sin Pointer Lock y con ganchos en `window`. */
  testMode: boolean;
}

export type DebugAction = 'invincible' | 'level' | 'minute' | 'spawn' | 'kill';

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
}

/** Ganchos para las pruebas automáticas en navegador (solo con `?test`). */
export interface TestHooks {
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
}

declare global {
  interface Window {
    __MAMPORRO__?: TestHooks;
  }
}

const TITLE_ORBIT_SPEED = 0.12;
const DEBUG_SPAWN_COUNT = 100;

export class Game {
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
  private levelUpHintShown = false;

  private readonly prev = { x: 0, y: 0, z: 0 };
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
    this.runView = new RunView(shadowTexture, {
      onPlayerHit: () => {
        this.rig.shake(0.45);
        this.ui.hud.flashHurt();
      },
      onLevelUp: (level) => {
        this.ui.hud.notice(t('notice.levelUp', { n: level }), true);
        if (!this.levelUpHintShown) {
          this.levelUpHintShown = true;
          this.ui.hud.notice(t('notice.levelUpPlaceholder'));
        }
      },
      onWeaponGained: (weapon) => this.ui.hud.notice(t('notice.newWeapon', { name: t(weapon.def.nameKey) })),
    });
    this.sky = createSky();
    this.seed = randomSeed();
    this.world = new World(this.seed, this.textures);
    this.setupScene();

    this.ui = new UI(
      uiRoot,
      { settings: () => this.save.settings, seed: () => this.seed },
      {
        onPlay: (seedText) => this.startRun(seedText),
        onNewMap: () => this.regenerateWorld(randomSeed()),
        onResume: () => this.resume(),
        onBackToTitle: () => this.backToTitle(),
        onRetry: () => this.retry(false),
        onRetryNewMap: () => this.retry(true),
        onSettingsChange: (patch) => this.changeSettings(patch),
      },
    );
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
    this.ui.refresh();
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
    this.run = new Run(this.world.collision, this.seed, CHARACTERS.remedios, this.runView);
    this.runView.attach(this.run);
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

  private gameOver(): void {
    const run = this.run;
    if (!run) return;
    this.state = 'gameover';
    this.input.exitPointerLock();
    this.ui.showGameOver({
      time: run.time,
      kills: run.kills,
      level: run.level,
      weapons: run.weapons.map((w) => ({ name: t(w.def.nameKey), damage: w.totalDamage })),
      seed: this.seed,
      cheated: run.cheated,
    });
  }

  private backToTitle(): void {
    this.state = 'title';
    this.input.exitPointerLock();
    this.endRun();
    this.resetPlayer();
    this.ui.show('title');
  }

  private bindEvents(): void {
    this.input.onPointerLockChange((locked) => {
      if (locked && (this.state === 'title' || this.state === 'paused' || this.state === 'gameover')) this.enterPlaying();
      else if (!locked && this.state === 'playing') this.pause();
    });
    this.input.onPointerLockError(() => {
      if (this.state !== 'playing') this.ui.toast(t('error.pointerLock'));
    });
    this.input.onKey((code) => {
      if (code === 'F3') this.ui.setDebugVisible(!this.ui.debugVisible);
      // Con el ratón capturado, Esc lo gestiona el navegador (y pausamos al perder
      // la captura); esto cubre el caso de jugar sin captura.
      if (code === 'Escape') this.pause();
      if (this.ui.debugVisible && this.state === 'playing') this.handleDebugKey(code);
    });
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) this.pause();
    });
    window.addEventListener('blur', () => {
      if (!this.options.testMode) this.pause();
    });
    // Red de seguridad: con una partida en marcha, el navegador pide confirmación
    // antes de cerrar la pestaña (p. ej. por un Ctrl+W accidental).
    window.addEventListener('beforeunload', (event) => {
      if (this.state === 'playing' || this.state === 'paused') {
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
    }
  }

  // ---------------------------------------------------------------- ajustes

  private changeSettings(patch: Partial<Settings>): void {
    this.applySettings(this.save.updateSettings(patch));
  }

  private applySettings(settings: Settings): void {
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
        run.update(dt, this.body, this.rig.yaw);
        if (run.dead) this.gameOver();
      }
    }
    this.input.endTick();
  }

  private draw(alpha: number, frameDt: number): void {
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
    this.world.update(now);
    this.runView.update(this.state === 'playing' ? alpha : 1, this.state === 'playing' ? frameDt : 0, now);
    if (run) {
      this.ui.hud.update({
        hp: run.hp,
        maxHp: run.stats.maxHp,
        level: run.level,
        xp: run.progress.xp,
        xpNext: xpToNextLevel(run.level),
        time: run.time,
        kills: run.kills,
        weapons: run.weapons.map((w) => ({ name: t(w.def.nameKey), level: w.level })),
      });
    }
    this.renderer.render(this.scene, this.rig.camera);
    this.updateStats(frameDt);
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
            }
          : null,
      });
    }
  }

  // ---------------------------------------------------------------- pruebas

  private exposeTestHooks(): void {
    window.__MAMPORRO__ = {
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
        if (this.run) this.run.hp = Math.max(0, this.run.hp - amount);
      },
      teleport: (x, z) => {
        const y = this.world.collision.groundHeight(x, z, Number.POSITIVE_INFINITY);
        this.body.placeAt(x, y, z);
        this.prev.x = x;
        this.prev.y = y;
        this.prev.z = z;
      },
    };
  }
}
