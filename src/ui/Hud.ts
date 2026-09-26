// HUD de la partida: vida, experiencia y nivel, oro, cuenta atrás (y enjambre
// final), bajas, minimapa, barra del jefe, armas, tomos, objetos, lo que se puede
// usar delante, avisos, el objeto recién conseguido y el destello rojo al recibir
// un golpe. Se actualiza tocando solo lo que cambia.
import { t } from '../i18n';
import type { CardTone, ItemView } from './cards';
import { h } from './dom';
import { Minimap } from './Minimap';

export interface HudWeapon {
  name: string;
  level: number;
}

export interface HudItem {
  name: string;
  count: number;
  tone: CardTone;
}

export interface HudBoss {
  name: string;
  hp: number;
  maxHp: number;
  enraged: boolean;
}

export interface HudData {
  hp: number;
  maxHp: number;
  level: number;
  xp: number;
  xpNext: number;
  gold: number;
  /** Segundos que quedan (negativo: prórroga del enjambre final). */
  timeLeft: number;
  swarm: boolean;
  kills: number;
  weapons: readonly HudWeapon[];
  /** Tomos, con su nombre corto. */
  tomes: readonly HudWeapon[];
  items: readonly HudItem[];
  boss: HudBoss | null;
  /** Lo que se puede usar ahora mismo (texto ya traducido) o null. */
  prompt: string | null;
  /** Barra de progreso de la acción en curso (santuario, desafío) o null. */
  progress: { label: string; value: number } | null;
}

const NOTICE_MS = 2600;
const ITEM_BANNER_MS = 4200;

/** mm:ss */
export function formatTime(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`;
}

/** Cuenta atrás: "mm:ss" y, durante el enjambre final, "+mm:ss". */
export function formatCountdown(timeLeft: number): string {
  return timeLeft >= 0 ? formatTime(Math.ceil(timeLeft)) : `+${formatTime(-timeLeft)}`;
}

function emptyLast() {
  return {
    hp: -1,
    maxHp: -1,
    level: -1,
    xpPct: -1,
    gold: -1,
    timer: '',
    swarm: false,
    kills: -1,
    weapons: '',
    tomes: '',
    items: '',
    boss: '',
    prompt: '',
    progress: '',
  };
}

export class Hud {
  readonly root: HTMLDivElement;
  readonly minimap = new Minimap();
  private readonly xpFill: HTMLDivElement;
  private readonly level: HTMLDivElement;
  private readonly hpFill: HTMLDivElement;
  private readonly hpText: HTMLDivElement;
  private readonly goldEl: HTMLDivElement;
  private readonly timer: HTMLDivElement;
  private readonly swarmLabel: HTMLDivElement;
  private readonly bossEl: HTMLDivElement;
  private readonly bossName: HTMLDivElement;
  private readonly bossFill: HTMLDivElement;
  private readonly kills: HTMLDivElement;
  private readonly weapons: HTMLDivElement;
  private readonly tomes: HTMLDivElement;
  private readonly items: HTMLDivElement;
  private readonly promptEl: HTMLDivElement;
  private readonly progressEl: HTMLDivElement;
  private readonly progressLabel: HTMLDivElement;
  private readonly progressFill: HTMLDivElement;
  private readonly notices: HTMLDivElement;
  private readonly banner: HTMLDivElement;
  private readonly hurt: HTMLDivElement;
  private bannerTimer = 0;
  private last = emptyLast();

  constructor() {
    this.xpFill = h('div', { className: 'xpbar__fill' });
    this.level = h('div', { className: 'hud__level' });
    this.hpFill = h('div', { className: 'hpbar__fill' });
    this.hpText = h('div', { className: 'hpbar__text' });
    this.goldEl = h('div', { className: 'hud__gold' });
    this.timer = h('div', { className: 'hud__timer' });
    this.swarmLabel = h('div', { className: 'hud__swarm' });
    this.bossName = h('div', { className: 'bossbar__name' });
    this.bossFill = h('div', { className: 'bossbar__fill' });
    this.bossEl = h('div', { className: 'bossbar' }, this.bossName, h('div', { className: 'bossbar__track' }, this.bossFill));
    this.kills = h('div', { className: 'hud__kills' });
    this.weapons = h('div', { className: 'hud__weapons' });
    this.tomes = h('div', { className: 'hud__tomes' });
    this.items = h('div', { className: 'hud__items' });
    this.promptEl = h('div', { className: 'hud__prompt' });
    this.progressLabel = h('div', { className: 'hud__progress-label' });
    this.progressFill = h('div', { className: 'hud__progress-fill' });
    this.progressEl = h('div', { className: 'hud__progress' }, this.progressLabel, h('div', { className: 'hud__progress-track' }, this.progressFill));
    this.notices = h('div', { className: 'hud__notices' });
    this.banner = h('div', { className: 'item-banner' });
    this.hurt = h('div', { className: 'hud__hurt' });
    this.bossEl.hidden = true;
    this.promptEl.hidden = true;
    this.progressEl.hidden = true;
    this.banner.hidden = true;
    this.root = h(
      'div',
      { className: 'hud' },
      this.hurt,
      h('div', { className: 'xpbar' }, this.xpFill),
      h('div', { className: 'hud__topleft' }, this.level, h('div', { className: 'hpbar' }, this.hpFill, this.hpText), this.goldEl),
      h('div', { className: 'hud__top' }, this.timer, this.swarmLabel, this.bossEl),
      h('div', { className: 'hud__topright' }, this.minimap.root, this.kills),
      h('div', { className: 'hud__bottomleft' }, this.items, h('div', { className: 'hud__build' }, this.weapons, this.tomes)),
      h('div', { className: 'hud__bottom' }, this.progressEl, this.promptEl),
      this.banner,
      this.notices,
    );
    this.root.hidden = true;
  }

  setVisible(visible: boolean): void {
    this.root.hidden = !visible;
    if (!visible) {
      this.banner.hidden = true;
      this.notices.replaceChildren();
    }
  }

  /** Fuerza a redibujar todo en la próxima actualización (p. ej. al cambiar de idioma). */
  invalidate(): void {
    this.last = emptyLast();
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
    const gold = Math.floor(d.gold);
    if (gold !== last.gold) {
      last.gold = gold;
      this.goldEl.textContent = t('hud.gold', { n: gold });
    }
    const timer = formatCountdown(d.timeLeft);
    if (timer !== last.timer || d.swarm !== last.swarm) {
      last.timer = timer;
      last.swarm = d.swarm;
      this.timer.textContent = timer;
      this.timer.classList.toggle('hud__timer--swarm', d.swarm);
      this.timer.classList.toggle('hud__timer--low', !d.swarm && d.timeLeft <= 30);
      this.swarmLabel.textContent = d.swarm ? t('hud.swarm') : '';
    }
    if (d.kills !== last.kills) {
      last.kills = d.kills;
      this.kills.textContent = t('hud.kills', { n: d.kills });
    }
    const bossKey = d.boss ? `${d.boss.name}:${Math.ceil(d.boss.hp)}:${d.boss.enraged}` : '';
    if (bossKey !== last.boss) {
      last.boss = bossKey;
      this.bossEl.hidden = !d.boss;
      if (d.boss) {
        this.bossName.textContent = d.boss.name;
        this.bossFill.style.width = `${Math.max(0, Math.min(100, (d.boss.hp / d.boss.maxHp) * 100))}%`;
        this.bossEl.classList.toggle('bossbar--enraged', d.boss.enraged);
      }
    }
    const weaponsKey = d.weapons.map((w) => `${w.name}:${w.level}`).join('|');
    if (weaponsKey !== last.weapons) {
      last.weapons = weaponsKey;
      this.weapons.replaceChildren(
        ...d.weapons.map((w) => h('div', { className: 'chip', text: t('hud.weaponLevel', { name: w.name, n: w.level }) })),
      );
    }
    const tomesKey = d.tomes.map((w) => `${w.name}:${w.level}`).join('|');
    if (tomesKey !== last.tomes) {
      last.tomes = tomesKey;
      this.tomes.replaceChildren(
        ...d.tomes.map((w) => h('div', { className: 'chip chip--tome', text: t('hud.weaponLevel', { name: w.name, n: w.level }) })),
      );
    }
    const itemsKey = d.items.map((i) => `${i.name}:${i.count}`).join('|');
    if (itemsKey !== last.items) {
      last.items = itemsKey;
      this.items.replaceChildren(
        ...d.items.map((i) =>
          h('div', { className: `chip chip--item chip--${i.tone}`, text: i.count > 1 ? t('hud.itemCount', { name: i.name, n: i.count }) : i.name }),
        ),
      );
    }
    const prompt = d.prompt ?? '';
    if (prompt !== last.prompt) {
      last.prompt = prompt;
      this.promptEl.hidden = !d.prompt;
      this.promptEl.textContent = prompt;
    }
    const progressKey = d.progress ? `${d.progress.label}:${Math.round(d.progress.value * 100)}` : '';
    if (progressKey !== last.progress) {
      last.progress = progressKey;
      this.progressEl.hidden = !d.progress;
      if (d.progress) {
        this.progressLabel.textContent = d.progress.label;
        this.progressFill.style.width = `${Math.round(Math.max(0, Math.min(1, d.progress.value)) * 100)}%`;
      }
    }
  }

  notice(text: string, highlight = false): void {
    const el = h('div', { className: highlight ? 'notice notice--big' : 'notice', text });
    this.notices.append(el);
    window.setTimeout(() => el.remove(), NOTICE_MS);
    // Como mucho tres avisos a la vez.
    while (this.notices.childElementCount > 3) this.notices.firstElementChild?.remove();
  }

  /** Enseña el objeto recién conseguido (rareza, nombre, efecto y descripción). */
  showItem(item: ItemView): void {
    this.banner.className = `item-banner card--${item.tone}`;
    this.banner.replaceChildren(
      h('div', { className: 'item-banner__tag', text: item.tag }),
      h('div', { className: 'item-banner__title', text: item.title }),
      ...item.lines.map((line) => h('div', { className: 'item-banner__line', text: line })),
      h('div', { className: 'item-banner__desc', text: item.description }),
    );
    this.banner.hidden = false;
    // Reinicia la animación de entrada.
    this.banner.classList.remove('item-banner--in');
    void this.banner.offsetWidth;
    this.banner.classList.add('item-banner--in');
    window.clearTimeout(this.bannerTimer);
    this.bannerTimer = window.setTimeout(() => {
      this.banner.hidden = true;
    }, ITEM_BANNER_MS);
  }

  flashHurt(): void {
    this.hurt.classList.remove('hud__hurt--on');
    // Fuerza el reinicio de la animación CSS.
    void this.hurt.offsetWidth;
    this.hurt.classList.add('hud__hurt--on');
  }
}
