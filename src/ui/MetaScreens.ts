// Pantallas de progreso: el DOM presenta datos, las compras se resuelven fuera.
import { CHARACTERS, type CharacterId } from '../data/characters';
import { ITEMS } from '../data/items';
import { EXTRA_PRICES, MISSIONS, SHOP, type ExtraAction, type Unlock } from '../data/meta';
import { WEAPONS } from '../data/weapons';
import { t } from '../i18n';
import { ownsUnlock, type MetaProgress } from '../systems/meta';
import { describeItem } from './cards';
import { button, h } from './dom';

export function unlockName(u: Unlock): string {
  return t(u.kind === 'character' ? CHARACTERS[u.id].nameKey : u.kind === 'weapon' ? WEAPONS[u.id].nameKey : ITEMS[u.id].nameKey);
}
function purchaseButton(coins: number, price: number, owned: boolean, buy: () => void): HTMLButtonElement {
  const label = owned ? t('meta.owned') : coins >= price ? t('meta.buy', { n: price }) : t('meta.need', { n: price - coins });
  const b = button(label, buy); b.disabled = owned || coins < price; return b;
}
export function charactersPanel(meta: MetaProgress, select: (id: CharacterId) => void): HTMLElement {
  return h('div', { className: 'meta-grid meta-grid--characters' }, ...Object.values(CHARACTERS).map(c => {
    const unlocked = meta.characters.includes(c.id);
    const chosen = meta.selected === c.id;
    const b = button(t(chosen ? 'meta.selected' : unlocked ? 'meta.select' : 'meta.locked'), () => select(c.id));
    b.disabled = !unlocked || chosen;
    b.setAttribute('aria-pressed', String(chosen));
    return h('article', { className: `meta-card stack${chosen ? ' meta-card--selected' : ''}` },
      h('div', { className: `character-emblem character-emblem--${c.id}`, attrs: { 'aria-hidden': 'true' } }, h('span')),
      h('h3', { text: t(c.nameKey) }),
      h('p', { className: 'muted', text: t('meta.weapon', { name: t(WEAPONS[c.startingWeapon].nameKey) }) }),
      h('p', { text: t(c.passiveKey) }),
      !unlocked && h('p', { className: 'meta-gold', text: t('meta.sourceShop') }), b);
  }));
}
export function shopPanel(meta: MetaProgress, buy: (id: string) => void, extra: (action: ExtraAction) => void): HTMLElement {
  const cards = SHOP.map(s => h('article', { className: 'meta-card stack' },
    h('h3', { text: unlockName(s.unlock) }),
    h('p', { className: 'muted', text: s.unlock.kind === 'character' ? t(CHARACTERS[s.unlock.id].passiveKey) : s.unlock.kind === 'weapon' ? t(WEAPONS[s.unlock.id].descriptionKey) : describeItem(ITEMS[s.unlock.id]).description }),
    h('span', { className: 'meta-gold', text: t('meta.balance', { n: s.price }) }),
    purchaseButton(meta.coins, s.price, ownsUnlock(meta, s.unlock), () => buy(s.id))));
  const extras = (['rerolls','skips','banishes'] as const).map(action => {
    const price = EXTRA_PRICES[meta.extras[action]];
    const b = price === undefined ? button(t('meta.max'), () => {}) : purchaseButton(meta.coins, price, false, () => extra(action));
    if (price === undefined) b.disabled = true;
    return h('article', { className: 'meta-card stack' }, h('h3', { text: t(`meta.${action}`) }), h('p', { text: t('meta.uses', { n: 2 + meta.extras[action] }) }), b);
  });
  return h('div', { className: 'stack' },
    h('p', { className: 'muted', text: t('meta.shopHint') }), h('div', { className: 'meta-grid' }, ...cards),
    h('h3', { text: t('meta.extras') }), h('p', { className: 'muted', text: t('meta.extrasHint') }), h('div', { className: 'meta-grid' }, ...extras),
    h('h3', { text: t('menu.missions') }),
    ...MISSIONS.filter(m => m.unlock).map(m => h('div', { className: 'meta-unlock row' },
      h('span', { text: m.unlock ? unlockName(m.unlock) : '' }),
      h('span', { className: 'muted', text: meta.completed.includes(m.id) ? t('meta.owned') : t('meta.sourceMission', { name: t(m.nameKey) }) }))));
}
export function missionsPanel(meta: MetaProgress): HTMLElement {
  return h('div', { className: 'stack' }, h('p', { className: 'muted', text: t('meta.missionHint') }),
    h('div', { className: 'meta-grid' }, ...MISSIONS.map(m => {
      const done = meta.completed.includes(m.id);
      return h('article', { className: `meta-card stack${done ? ' meta-card--selected' : ''}` },
        h('h3', { text: t(m.nameKey) }),
        h('progress', { attrs: { max: String(m.target), value: String(meta.missions[m.id]), 'aria-label': t(m.nameKey) } }),
        h('span', { className: 'muted', text: done ? t('meta.done') : t('meta.progress', { n: meta.missions[m.id], target: m.target }) }),
        h('span', { className: 'meta-gold', text: t('meta.reward', { n: m.coins }) }),
        m.unlock && h('span', { text: t('meta.unlock', { name: unlockName(m.unlock) }) }));
    })));
}
