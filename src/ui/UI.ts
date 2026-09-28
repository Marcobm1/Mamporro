// Capa de interfaz HTML superpuesta al canvas: pantallas (inicio, pausa, subida de
// nivel, game over), HUD, FPS, panel de debug y avisos. No contiene lógica de juego:
// avisa con callbacks.
import { CHARACTERS, type CharacterId } from '../data/characters';
import { MISSIONS, type ExtraAction } from '../data/meta';
import type { MetaProgress, MetaReceipt } from '../systems/meta';
import { charactersPanel, missionsPanel, shopPanel } from './MetaScreens';
import { RUN_DURATIONS } from '../data/waves';
import { formatNumber, onLanguageChange, t, type TranslationKey } from '../i18n';
import type { Settings } from '../save/schema';
import type { CardTone, StatLine } from './cards';
import { controlsLegend, languageSelector, optionsPanel } from './components';
import { applyUiScale, button, h, segmented } from './dom';
import { formatTime, Hud } from './Hud';
import { LevelUpScreen, type LevelUpCallbacks, type LevelUpView } from './LevelUpScreen';

export type Screen = 'none' | 'title' | 'pause' | 'levelup' | 'gameover';

export interface UICallbacks extends LevelUpCallbacks {
  onSelectCharacter(id: CharacterId): void;
  onPurchase(id: string): void;
  onPurchaseExtra(action: ExtraAction): void;
  onPlay(seedText: string): void;
  onNewMap(): void;
  onResume(): void;
  onBackToTitle(): void;
  onRetry(): void;
  onRetryNewMap(): void;
  onSettingsChange(patch: Partial<Settings>): void;
}

/** Un objeto tal y como se enseña en la pausa y en los resultados. */
export interface ItemLine {
  name: string;
  count: number;
  tone: CardTone;
  description: string;
}

/** Pantalla de resultados, al ganar (jefe derrotado) o al caer. */
export interface ResultsSummary {
  meta: MetaReceipt;
  victory: boolean;
  time: number;
  kills: number;
  level: number;
  gold: number;
  chests: number;
  items: readonly ItemLine[];
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
  /** Minuto de dificultad, ritmo de aparición y máximo de enemigos. */
  difficulty: number;
  spawnRate: number;
  maxAlive: number;
  gold: number;
}

/** Lo que la pausa enseña de la partida en curso (las armas y tomos ya están en el HUD). */
export interface PauseRunInfo {
  stats: StatLine[];
  items: ItemLine[];
}

export interface UIContext {
  meta(): MetaProgress;
  canSave(): boolean;
  settings(): Settings;
  seed(): string;
  runInfo(): PauseRunInfo | null;
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
  private menuPage: 'home' | 'setup' | 'characters' | 'shop' | 'missions' | 'options' = 'home';
  private confirmingAbandon = false;
  private screenEl: HTMLElement | null = null;
  private readonly fpsEl: HTMLDivElement;
  private readonly debugEl: HTMLDivElement;
  private readonly toastEl: HTMLDivElement;
  private toastTimer = 0;
  /** Lo que el jugador lleva escrito en el campo de semilla (sobrevive a cambios de idioma). */
  private seedDraft = '';
  private results: ResultsSummary | null = null;
  private levelUpView: LevelUpView | null = null;
  private readonly levelUp: LevelUpScreen;
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
    this.levelUp = new LevelUpScreen(callbacks);
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
    if (screen === 'title' && this.screen !== 'title') this.menuPage = 'home';
    this.confirmingAbandon = false;
    this.screen = screen;
    this.refresh();
  }

  /** Reconstruye la pantalla visible (p. ej. al cambiar de idioma o de mapa). */
  refresh(): void {
    this.screenEl?.remove();
    this.screenEl = null;
    if (this.screen === 'title') this.screenEl = this.buildTitle();
    else if (this.screen === 'pause') this.screenEl = this.buildPause();
    else if (this.screen === 'levelup' && this.levelUpView) this.screenEl = this.levelUp.render(this.levelUpView, false);
    else if (this.screen === 'gameover' && this.results) this.screenEl = this.buildResults(this.results);
    if (this.screenEl) this.root.prepend(this.screenEl);
  }

  /** Muestra (o actualiza) la subida de nivel; `fresh` = subida nueva, no un Reroll o un Descartar. */
  showLevelUp(view: LevelUpView, fresh: boolean): void {
    this.levelUpView = view;
    this.screen = 'levelup';
    this.screenEl?.remove();
    this.screenEl = this.levelUp.render(view, fresh);
    this.root.prepend(this.screenEl);
  }

  /** Atajos de la subida de nivel; devuelve true si la tecla era suya. */
  levelUpKey(code: string): boolean {
    return this.screen === 'levelup' && this.levelUp.handleKey(code);
  }

  showResults(summary: ResultsSummary): void {
    this.results = summary;
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
        t('debug.director', { minutes: formatNumber(r.difficulty, 1), rate: formatNumber(r.spawnRate, 1), max: r.maxAlive, gold: Math.floor(r.gold) }),
        t('debug.keys'),
        t('debug.keys2'),
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

  private openMenu(page: typeof this.menuPage): void {
    this.menuPage = page;
    this.refresh();
  }

  private buildTitle(): HTMLElement {
    if (this.menuPage === 'setup') return this.buildSetup();
    const meta = this.ctx.meta();
    const page = this.menuPage;
    const heading = page === 'characters' ? 'menu.characters' : page === 'shop' ? 'menu.shop' : page === 'missions' ? 'menu.missions' : 'options.title';
    const header = h('div', { className: 'meta-header row' },
      page !== 'home' && button(t('menu.back'), () => this.openMenu('home'), 'btn btn--secondary'),
      h('span', { className: 'meta-gold', text: t('meta.balance', { n: meta.coins }) }),
      languageSelector(this.ctx.settings().language, language => this.callbacks.onSettingsChange({ language })));
    let content: HTMLElement;
    if (page === 'home') {
      content = h('div', { className: 'menu-home' },
        h('div', { className: 'menu-hero stack' },
          h('span', { className: 'prototype-tag', text: t('app.prototype') }),
          h('h1', { className: 'logo', text: t('app.title') }),
          h('h2', { text: t('meta.hero') }), h('p', { className: 'muted', text: t('meta.heroText') }),
          h('p', { text: t('meta.current', { name: t(CHARACTERS[meta.selected].nameKey) }) })),
        h('nav', { className: 'menu-buttons stack', attrs: { 'aria-label': t('app.title') } },
          button(t('title.play'), () => this.openMenu('setup'), 'btn btn--big'),
          button(t('menu.characters'), () => this.openMenu('characters')),
          button(t('menu.shop'), () => this.openMenu('shop')),
          button(t('menu.missions'), () => this.openMenu('missions')),
          button(t('options.title'), () => this.openMenu('options'))));
    } else {
      const panel = page === 'characters' ? charactersPanel(meta, id => this.callbacks.onSelectCharacter(id))
        : page === 'shop' ? shopPanel(meta, id => this.callbacks.onPurchase(id), action => this.callbacks.onPurchaseExtra(action))
        : page === 'missions' ? missionsPanel(meta)
        : optionsPanel(this.ctx.settings(), patch => this.callbacks.onSettingsChange(patch));
      content = h('div', { className: 'stack' }, h('h2', { className: 'panel-title', text: t(heading) }), panel);
    }
    return h('div', { className: 'screen screen--menu' }, h('div', { className: 'panel menu-panel stack' }, header,
      !this.ctx.canSave() && h('p', { className: 'warning', text: t('meta.saveWarning') }), content));
  }

  private buildSetup(): HTMLElement {
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
      button(t('menu.back'), () => this.openMenu('home'), 'btn btn--secondary'),
      h('h2', { className: 'panel-title', text: t('title.play') }),
      button(t('meta.current', { name: t(CHARACTERS[this.ctx.meta().selected].nameKey) }), () => this.openMenu('characters'), 'btn btn--secondary'),
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
        h(
          'div',
          { className: 'row' },
          h('span', { text: t('title.duration') }),
          segmented(
            RUN_DURATIONS.map((n) => ({ value: n, label: t('title.minutes', { n }) })),
            settings.runMinutes,
            (runMinutes) => this.callbacks.onSettingsChange({ runMinutes }),
          ),
        ),
        controlsLegend(settings.slideWithCtrl),
      ),
      h('div', { className: 'prototype-tag', text: t('title.clickHint') }),
    );
  }

  private buildResults(summary: ResultsSummary): HTMLElement {
    const stat = (label: string, value: string): Node[] => [h('dt', { text: label }), h('dd', { text: value })];
    const stats = h(
      'dl',
      { className: 'stats' },
      ...stat(t('gameover.time'), formatTime(summary.time)),
      ...stat(t('gameover.kills'), String(summary.kills)),
      ...stat(t('gameover.level'), String(summary.level)),
      ...stat(t('results.gold'), String(Math.floor(summary.gold))),
      ...stat(t('results.chests'), String(summary.chests)),
    );
    const damage = h(
      'dl',
      { className: 'stats' },
      ...summary.weapons.flatMap((w) => stat(w.name, String(Math.round(w.damage)))),
    );
    const victory = summary.victory;
    return h(
      'div',
      { className: `screen screen--gameover${victory ? ' screen--victory' : ''}` },
      h(
        'div',
        { className: 'panel panel--wide stack' },
        h('h2', { className: `gameover__title${victory ? ' gameover__title--victory' : ''}`, text: t(victory ? 'results.victory' : 'gameover.title') }),
        h('div', { className: 'muted', text: t(victory ? 'results.victorySubtitle' : 'gameover.subtitle') }),
        h(
          'div',
          { className: 'row results-columns' },
          h('div', { className: 'stack' }, stats, h('div', { className: 'muted', text: t('gameover.damage') }), damage),
          h('div', { className: 'stack' }, h('div', { className: 'muted', text: t('results.items') }), this.itemList(summary.items, t('results.noItems'))),
        ),
        h('div', { className: 'muted', text: t('gameover.seed', { seed: summary.seed }) }),
        h('div', { className: 'meta-gold', text: t('meta.results', { n: summary.meta.total }) }),
        h('div', { className: 'muted', text: t('meta.breakdown', { kills: summary.meta.kills, survival: summary.meta.survival, victory: summary.meta.victory, missions: summary.meta.missions }) }),
        ...summary.meta.completed.map(id => {
          const mission = MISSIONS.find(m => m.id === id);
          return h('div', { text: t('meta.newMission', { name: mission ? t(mission.nameKey) : '' }) });
        }),
        !this.ctx.canSave() && h('div', { className: 'warning', text: t('meta.saveWarning') }),
        summary.cheated && h('div', { className: 'warning', text: t('meta.cheated') }),
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

  /** Objetos en chips con el color de su rareza (la descripción, al pasar el ratón). */
  private itemList(items: readonly ItemLine[], empty: string): HTMLElement {
    if (items.length === 0) return h('div', { className: 'muted', text: empty });
    return h(
      'div',
      { className: 'item-list' },
      ...items.map((item) =>
        h('div', {
          className: `chip chip--item chip--${item.tone}`,
          text: item.count > 1 ? t('hud.itemCount', { name: item.name, n: item.count }) : item.name,
          attrs: { title: item.description },
        }),
      ),
    );
  }

  private buildPause(): HTMLElement {
    if (this.confirmingAbandon) return h('div', { className: 'screen screen--pause' },
      h('div', { className: 'panel stack', attrs: { role: 'alertdialog', 'aria-label': t('meta.abandon') } },
        h('p', { text: t('meta.abandon') }),
        button(t('meta.cancel'), () => { this.confirmingAbandon = false; this.refresh(); }),
        button(t('meta.confirm'), () => this.callbacks.onBackToTitle(), 'btn btn--secondary')));

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
          button(t('pause.backToTitle'), () => { this.confirmingAbandon = true; this.refresh(); }, 'btn btn--secondary'),
          h('div', { className: 'muted', text: t('pause.seed', { seed: this.ctx.seed() }) }),
        ),
        h('div', { className: 'row pause-columns' }, ...this.buildRunInfo(), h('div', { className: 'stack' }, options, legend)),
      ),
    );
  }

  /** Estadísticas y objetos del personaje (solo con una partida en marcha), en dos columnas. */
  private buildRunInfo(): HTMLElement[] {
    const info = this.ctx.runInfo();
    if (!info) return [];
    const stat = (label: string, value: string): Node[] => [h('dt', { text: label }), h('dd', { text: value })];
    return [
      h(
        'div',
        { className: 'pause-run stack' },
        h('div', { className: 'muted', text: t('pause.stats') }),
        h('dl', { className: 'stats stats--compact' }, ...info.stats.flatMap((l) => stat(l.label, l.value))),
      ),
      h('div', { className: 'pause-items stack' }, h('div', { className: 'muted', text: t('pause.items') }), this.itemList(info.items, t('pause.noItems'))),
    ];
  }
}
