// HUD de la partida: vida, experiencia y nivel, tiempo, bajas, armas, avisos y
// destello rojo al recibir un golpe. Se actualiza tocando solo lo que cambia.
import { t } from '../i18n';
import { h } from './dom';

export interface HudWeapon {
  name: string;
  level: number;
}

export interface HudData {
  hp: number;
  maxHp: number;
  level: number;
  xp: number;
  xpNext: number;
  /** Segundos de partida. */
  time: number;
  kills: number;
  weapons: readonly HudWeapon[];
}

const NOTICE_MS = 2600;

/** mm:ss */
export function formatTime(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;
}

export class Hud {
  readonly root: HTMLDivElement;
  private readonly xpFill: HTMLDivElement;
  private readonly level: HTMLDivElement;
  private readonly hpFill: HTMLDivElement;
  private readonly hpText: HTMLDivElement;
  private readonly timer: HTMLDivElement;
  private readonly kills: HTMLDivElement;
  private readonly weapons: HTMLDivElement;
  private readonly notices: HTMLDivElement;
  private readonly hurt: HTMLDivElement;
  private last = { hp: -1, maxHp: -1, level: -1, xpPct: -1, time: -1, kills: -1, weapons: '' };

  constructor() {
    this.xpFill = h('div', { className: 'xpbar__fill' });
    this.level = h('div', { className: 'hud__level' });
    this.hpFill = h('div', { className: 'hpbar__fill' });
    this.hpText = h('div', { className: 'hpbar__text' });
    this.timer = h('div', { className: 'hud__timer' });
    this.kills = h('div', { className: 'hud__kills' });
    this.weapons = h('div', { className: 'hud__weapons' });
    this.notices = h('div', { className: 'hud__notices' });
    this.hurt = h('div', { className: 'hud__hurt' });
    this.root = h(
      'div',
      { className: 'hud' },
      this.hurt,
      h('div', { className: 'xpbar' }, this.xpFill),
      h('div', { className: 'hud__topleft' }, this.level, h('div', { className: 'hpbar' }, this.hpFill, this.hpText)),
      this.timer,
      this.kills,
      this.weapons,
      this.notices,
    );
    this.root.hidden = true;
  }

  setVisible(visible: boolean): void {
    this.root.hidden = !visible;
  }

  /** Fuerza a redibujar todo en la próxima actualización (p. ej. al cambiar de idioma). */
  invalidate(): void {
    this.last = { hp: -1, maxHp: -1, level: -1, xpPct: -1, time: -1, kills: -1, weapons: '' };
  }

  update(d: HudData): void {
    const last = this.last;
    const hp = Math.ceil(d.hp);
    if (hp !== last.hp || d.maxHp !== last.maxHp) {
      last.hp = hp;
      last.maxHp = d.maxHp;
      this.hpFill.style.width = `${Math.max(0, Math.min(100, (d.hp / d.maxHp) * 100))}%`;
      this.hpText.textContent = `${hp} / ${Math.round(d.maxHp)}`;
    }
    const xpPct = Math.round((d.xp / d.xpNext) * 1000) / 10;
    if (xpPct !== last.xpPct) {
      last.xpPct = xpPct;
      this.xpFill.style.width = `${Math.min(100, xpPct)}%`;
    }
    if (d.level !== last.level) {
      last.level = d.level;
      this.level.textContent = t('hud.level', { n: d.level });
    }
    const time = Math.floor(d.time);
    if (time !== last.time) {
      last.time = time;
      this.timer.textContent = formatTime(time);
    }
    if (d.kills !== last.kills) {
      last.kills = d.kills;
      this.kills.textContent = t('hud.kills', { n: d.kills });
    }
    const weaponsKey = d.weapons.map((w) => `${w.name}:${w.level}`).join('|');
    if (weaponsKey !== last.weapons) {
      last.weapons = weaponsKey;
      this.weapons.replaceChildren(
        ...d.weapons.map((w) => h('div', { className: 'chip', text: t('hud.weaponLevel', { name: w.name, n: w.level }) })),
      );
    }
  }

  notice(text: string, highlight = false): void {
    const el = h('div', { className: highlight ? 'notice notice--big' : 'notice', text });
    this.notices.append(el);
    window.setTimeout(() => el.remove(), NOTICE_MS);
    // Como mucho tres avisos a la vez.
    while (this.notices.childElementCount > 3) this.notices.firstElementChild?.remove();
  }

  flashHurt(): void {
    this.hurt.classList.remove('hud__hurt--on');
    // Fuerza el reinicio de la animación CSS.
    void this.hurt.offsetWidth;
    this.hurt.classList.add('hud__hurt--on');
  }
}
