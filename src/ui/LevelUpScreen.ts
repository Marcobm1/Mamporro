// Pantalla de elegir carta: subida de nivel (con Reroll/Saltar/Descartar) y
// bendiciones de los santuarios (sin acciones). Cartas por rareza y atajos de
// teclado. No sabe nada de la partida: pinta lo que le dan y avisa.
import { LEVEL_UP_CONFIG } from '../data/upgrades';
import { t } from '../i18n';
import type { CardView } from './cards';
import { h } from './dom';

/** Usos que quedan de las acciones sobre las cartas. */
export interface LevelUpActions {
  rerolls: number;
  skips: number;
  banishes: number;
}

export interface LevelUpView {
  title: string;
  subtitle: string;
  /** Elecciones que quedan después de esta. */
  pending: number;
  cards: readonly CardView[];
  /** Reroll, Saltar y Descartar (solo al subir de nivel; null en los santuarios). */
  actions: LevelUpActions | null;
}

export interface LevelUpCallbacks {
  onChoose(index: number): void;
  onReroll(): void;
  onSkip(): void;
  onBanish(index: number): void;
}

const CARD_KEYS = ['Digit1', 'Digit2', 'Digit3', 'Digit4'];
const NUMPAD_KEYS = ['Numpad1', 'Numpad2', 'Numpad3', 'Numpad4'];

export class LevelUpScreen {
  private view: LevelUpView | null = null;
  private banishMode = false;
  /** Momento (ms) desde el que se aceptan teclas y clics. */
  private acceptFrom = 0;
  /** Contenedor estable: se repinta por dentro para que la interfaz lo pueda quitar siempre. */
  private readonly root = h('div', {});

  constructor(private readonly callbacks: LevelUpCallbacks) {}

  /**
   * Pinta la pantalla. El modo descartar se apaga siempre (un descarte por
   * activación); `fresh` = subida nueva: además se reinicia la espera anti-clics.
   */
  render(view: LevelUpView, fresh: boolean): HTMLElement {
    this.view = view;
    this.banishMode = false;
    if (fresh) this.acceptFrom = performance.now() + LEVEL_UP_CONFIG.inputGuard * 1000;
    // Las cartas entran con animación solo en una subida nueva, no al cambiar o descartar.
    this.paint(view, fresh);
    return this.root;
  }

  /** Atajos de teclado; devuelve true si la tecla era suya. */
  handleKey(code: string): boolean {
    const view = this.view;
    if (!view) return false;
    const slot = Math.max(CARD_KEYS.indexOf(code), NUMPAD_KEYS.indexOf(code));
    if (slot >= 0) {
      if (slot < view.cards.length) this.pick(slot);
      return true;
    }
    if (code === 'Escape' && this.banishMode) {
      this.setBanishMode(false);
      return true;
    }
    const actions = view.actions;
    if (!actions) return false;
    if (!this.ready()) return code === 'KeyR' || code === 'KeyX' || code === 'KeyB';
    if (code === 'KeyR' && actions.rerolls > 0) this.callbacks.onReroll();
    else if (code === 'KeyX' && actions.skips > 0) this.callbacks.onSkip();
    else if (code === 'KeyB') this.setBanishMode(!this.banishMode);
    else return false;
    return true;
  }

  private ready(): boolean {
    return performance.now() >= this.acceptFrom;
  }

  private pick(index: number): void {
    const card = this.view?.cards[index];
    if (!card || !this.ready()) return;
    if (this.banishMode) {
      if (card.banishable) this.callbacks.onBanish(index);
    } else {
      this.callbacks.onChoose(index);
    }
  }

  private setBanishMode(on: boolean): void {
    const view = this.view;
    if (!view?.actions || (on && view.actions.banishes <= 0)) return;
    this.banishMode = on;
    this.paint(view, false);
  }

  private paint(view: LevelUpView, animate: boolean): void {
    const cards = view.cards.map((card, i) =>
      h(
        'button',
        {
          className: `card card--${card.tone}${this.banishMode ? (card.banishable ? ' card--banish' : ' card--disabled') : ''}`,
          attrs: { type: 'button' },
          onClick: () => this.pick(i),
        },
        h('span', { className: 'card__key', text: String(i + 1) }),
        card.tag && h('span', { className: 'card__tag', text: card.tag }),
        h('span', { className: 'card__title', text: card.title }),
        card.level && h('span', { className: 'card__level', text: card.level }),
        card.lines.length > 0 && h('span', { className: 'card__lines' }, ...card.lines.map((l) => h('span', { text: l }))),
        card.description && h('span', { className: 'card__desc', text: card.description }),
      ),
    );
    const action = (label: string, key: string, uses: number, onClick: () => void, active = false): HTMLButtonElement => {
      const b = h(
        'button',
        {
          className: `btn btn--secondary levelup__action${active ? ' levelup__action--on' : ''}`,
          attrs: { type: 'button' },
          onClick: () => {
            if (this.ready()) onClick();
          },
        },
        h('span', { className: 'levelup__keycap', text: key }),
        label,
      );
      b.disabled = uses <= 0;
      return b;
    };
    this.root.className = `screen screen--levelup${this.banishMode ? ' screen--banish' : ''}${animate ? ' levelup--animate' : ''}`;
    const actions = view.actions;
    const children: Array<Node | false> = [
      h('h2', { className: 'levelup__title', text: view.title }),
      h('div', { className: 'levelup__subtitle', text: this.banishMode ? t('levelup.banishHint') : view.subtitle }),
      view.pending > 0 && h('div', { className: 'levelup__pending', text: t('levelup.pending', { n: view.pending }) }),
      h('div', { className: 'levelup__cards' }, ...cards),
      actions !== null &&
        h(
          'div',
          { className: 'levelup__actions' },
          action(t('levelup.reroll', { n: actions.rerolls }), 'R', actions.rerolls, () => this.callbacks.onReroll()),
          action(t('levelup.skip', { n: actions.skips }), 'X', actions.skips, () => this.callbacks.onSkip()),
          this.banishMode
            ? action(t('levelup.cancel'), 'B', 1, () => this.setBanishMode(false), true)
            : action(t('levelup.banish', { n: actions.banishes }), 'B', actions.banishes, () => this.setBanishMode(true)),
        ),
    ];
    this.root.replaceChildren(...children.filter((c): c is Node => c !== false));
  }
}
