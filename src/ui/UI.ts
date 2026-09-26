// Capa de interfaz HTML superpuesta al canvas: pantallas (inicio, pausa, game
// over), HUD, FPS, panel de debug y avisos. No contiene lógica de juego: avisa con callbacks.
import { onLanguageChange, t, type TranslationKey } from '../i18n';
import type { Settings } from '../save/schema';
import { controlsLegend, languageSelector, optionsPanel } from './components';
import { applyUiScale, button, h } from './dom';
import { formatTime, Hud } from './Hud';

export type Screen = 'none' | 'title' | 'pause' | 'gameover';

export interface UICallbacks {
  onPlay(seedText: string): void;
  onNewMap(): void;
  onResume(): void;
  onBackToTitle(): void;
  onRetry(): void;
  onRetryNewMap(): void;
  onSettingsChange(patch: Partial<Settings>): void;
}

export interface GameOverSummary {
  time: number;
  kills: number;
  level: number;
  weapons: ReadonlyArray<{ name: string; damage: number }>;
  seed: string;
  cheated: boolean;
}

export interface DebugRunInfo {
  enemies: number;
  projectiles: number;
  gems: number;
  particles: number;
  time: number;
  hp: number;
  xp: number;
  xpNext: number;
  /** Frenado por la horda (0..1). */
  slow: number;
}

export interface UIContext {
  settings(): Settings;
  seed(): string;
}

export interface DebugInfo {
  fps: number;
  frameMs: number;
  logicMs: number;
  renderMs: number;
  drawCalls: number;
  triangles: number;
  internalWidth: number;
  internalHeight: number;
  pixelScale: number;
  x: number;
  y: number;
  z: number;
  speed: number;
  state: TranslationKey;
  slopeDeg: number;
  seed: string;
  run: DebugRunInfo | null;
}

const TOAST_MS = 3200;

export class UI {
  private screen: Screen = 'none';
  private screenEl: HTMLElement | null = null;
  private readonly fpsEl: HTMLDivElement;
  private readonly debugEl: HTMLDivElement;
  private readonly toastEl: HTMLDivElement;
  private toastTimer = 0;
  /** Lo que el jugador lleva escrito en el campo de semilla (sobrevive a cambios de idioma). */
  private seedDraft = '';
  private gameOver: GameOverSummary | null = null;
  readonly hud = new Hud();

  constructor(
    private readonly root: HTMLElement,
    private readonly ctx: UIContext,
    private readonly callbacks: UICallbacks,
  ) {
    this.fpsEl = h('div', { className: 'fps' });
    this.fpsEl.hidden = true;
    this.debugEl = h('div', { className: 'debug' });
    this.debugEl.hidden = true;
    this.toastEl = h('div', { className: 'toast toast--hidden' });
    root.append(this.hud.root, this.fpsEl, this.debugEl, this.toastEl);

    applyUiScale(root);
    window.addEventListener('resize', () => applyUiScale(root));
    onLanguageChange(() => {
      this.hud.invalidate();
      this.refresh();
    });
  }

  get currentScreen(): Screen {
    return this.screen;
  }

  show(screen: Screen): void {
    this.screen = screen;
    this.refresh();
  }

  /** Reconstruye la pantalla visible (p. ej. al cambiar de idioma o de mapa). */
  refresh(): void {
    this.screenEl?.remove();
    this.screenEl = null;
    if (this.screen === 'title') this.screenEl = this.buildTitle();
    else if (this.screen === 'pause') this.screenEl = this.buildPause();
    else if (this.screen === 'gameover' && this.gameOver) this.screenEl = this.buildGameOver(this.gameOver);
    if (this.screenEl) this.root.prepend(this.screenEl);
  }

  showGameOver(summary: GameOverSummary): void {
    this.gameOver = summary;
    this.show('gameover');
  }

  setFpsVisible(visible: boolean): void {
    this.fpsEl.hidden = !visible;
  }

  updateFps(fps: number): void {
    if (!this.fpsEl.hidden) this.fpsEl.textContent = t('hud.fps', { fps: Math.round(fps) });
  }

  get debugVisible(): boolean {
    return !this.debugEl.hidden;
  }

  setDebugVisible(visible: boolean): void {
    this.debugEl.hidden = !visible;
  }

  updateDebug(info: DebugInfo): void {
    if (this.debugEl.hidden) return;
    const n = (v: number, digits = 1): string => v.toFixed(digits);
    const lines = [
      `${t('debug.fps')}: ${Math.round(info.fps)} · ${t('debug.frame')}: ${n(info.frameMs)} ms`,
      `${t('debug.logic')}: ${n(info.logicMs, 2)} ms · ${t('debug.render')}: ${n(info.renderMs, 2)} ms`,
      `${t('debug.drawCalls')}: ${info.drawCalls} · ${t('debug.triangles')}: ${n(info.triangles / 1000)}k`,
      `${t('debug.resolution')}: ${info.internalWidth}×${info.internalHeight} ×${info.pixelScale}`,
      `${t('debug.position')}: ${n(info.x)} ${n(info.y)} ${n(info.z)}`,
      `${t('debug.speed')}: ${n(info.speed)} m/s · ${t('debug.state')}: ${t(info.state)}`,
      `${t('debug.slope')}: ${Math.round(info.slopeDeg)}° · ${t('debug.seed')}: ${info.seed}`,
    ];
    if (info.run) {
      const r = info.run;
      lines.push(
        t('debug.entities', { enemies: r.enemies, projectiles: r.projectiles, gems: r.gems, particles: r.particles }),
        t('debug.run', { time: formatTime(r.time), hp: Math.ceil(r.hp), xp: Math.floor(r.xp), next: r.xpNext, slow: Math.round(r.slow * 100) }),
        t('debug.keys'),
      );
    }
    this.debugEl.replaceChildren(h('div', { className: 'debug__title', text: t('debug.title') }), lines.join('\n'));
  }

  toast(message: string): void {
    this.toastEl.textContent = message;
    this.toastEl.classList.remove('toast--hidden');
    window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => this.toastEl.classList.add('toast--hidden'), TOAST_MS);
  }

  private buildTitle(): HTMLElement {
    const settings = this.ctx.settings();
    const seedInput = h('input', {
      className: 'input',
      attrs: {
        type: 'text',
        maxlength: '12',
        placeholder: t('title.seedPlaceholder'),
        spellcheck: 'false',
        autocomplete: 'off',
        'aria-label': t('title.seedLabel'),
      },
    });
    seedInput.value = this.seedDraft;
    seedInput.addEventListener('input', () => {
      this.seedDraft = seedInput.value;
    });
    seedInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') this.callbacks.onPlay(seedInput.value);
    });

    return h(
      'div',
      { className: 'screen screen--title' },
      h(
        'div',
        { className: 'title-lang' },
        languageSelector(settings.language, (language) => this.callbacks.onSettingsChange({ language })),
      ),
      h('h1', { className: 'logo', text: t('app.title') }),
      h('p', { className: 'tagline', text: t('app.tagline') }),
      h('div', { className: 'prototype-tag', text: t('app.prototype') }),
      button(t('title.play'), () => this.callbacks.onPlay(seedInput.value), 'btn btn--big'),
      h(
        'div',
        { className: 'panel stack' },
        h(
          'div',
          { className: 'row' },
          h('span', { text: t('title.seedLabel') }),
          seedInput,
          button(
            t('title.newMap'),
            () => {
              this.seedDraft = '';
              this.callbacks.onNewMap();
            },
            'btn btn--secondary',
          ),
        ),
        h('div', { className: 'muted', text: t('title.currentSeed', { seed: this.ctx.seed() }) }),
        controlsLegend(settings.slideWithCtrl),
      ),
      h('div', { className: 'prototype-tag', text: t('title.clickHint') }),
    );
  }

  private buildGameOver(summary: GameOverSummary): HTMLElement {
    const stat = (label: string, value: string): Node[] => [h('dt', { text: label }), h('dd', { text: value })];
    const stats = h(
      'dl',
      { className: 'stats' },
      ...stat(t('gameover.time'), formatTime(summary.time)),
      ...stat(t('gameover.kills'), String(summary.kills)),
      ...stat(t('gameover.level'), String(summary.level)),
    );
    const damage = h(
      'dl',
      { className: 'stats' },
      ...summary.weapons.flatMap((w) => stat(w.name, String(Math.round(w.damage)))),
    );
    return h(
      'div',
      { className: 'screen screen--gameover' },
      h(
        'div',
        { className: 'panel stack' },
        h('h2', { className: 'gameover__title', text: t('gameover.title') }),
        h('div', { className: 'muted', text: t('gameover.subtitle') }),
        stats,
        h('div', { className: 'muted', text: t('gameover.damage') }),
        damage,
        h('div', { className: 'muted', text: t('gameover.seed', { seed: summary.seed }) }),
        summary.cheated && h('div', { className: 'warning', text: t('gameover.cheated') }),
        h(
          'div',
          { className: 'row' },
          button(t('gameover.retry'), () => this.callbacks.onRetry(), 'btn btn--big'),
          button(t('gameover.newMap'), () => this.callbacks.onRetryNewMap(), 'btn btn--secondary'),
          button(t('gameover.backToTitle'), () => this.callbacks.onBackToTitle(), 'btn btn--secondary'),
        ),
      ),
    );
  }

  private buildPause(): HTMLElement {
    const settings = this.ctx.settings();
    const legend = h('div', {}, controlsLegend(settings.slideWithCtrl));
    const options = optionsPanel(settings, (patch) => {
      this.callbacks.onSettingsChange(patch);
      if (patch.slideWithCtrl !== undefined) legend.replaceChildren(controlsLegend(patch.slideWithCtrl));
    });
    return h(
      'div',
      { className: 'screen screen--pause' },
      h(
        'div',
        { className: 'panel panel--wide stack' },
        h('h2', { className: 'panel-title', text: t('pause.title') }),
        h(
          'div',
          { className: 'row' },
          button(t('pause.resume'), () => this.callbacks.onResume(), 'btn btn--big'),
          button(t('pause.backToTitle'), () => this.callbacks.onBackToTitle(), 'btn btn--secondary'),
        ),
        h('div', { className: 'muted', text: t('pause.seed', { seed: this.ctx.seed() }) }),
        h('div', { className: 'row pause-columns' }, options, legend),
      ),
    );
  }
}
