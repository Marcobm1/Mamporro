// Textos de las cartas de subida de nivel y de las estadísticas de la pausa
// (lógica pura de presentación: datos + traducciones, sin DOM).
import { formatNumber, t, type TranslationKey } from '../i18n';
import { rarityById, type RarityId } from '../data/rarities';
import type { BonusStat, StatEffect } from '../data/bonuses';
import { TOMES } from '../data/tomes';
import { LEVEL_UP_CONFIG, WEAPON_UPGRADE_STEPS, type WeaponStatKey } from '../data/upgrades';
import { WEAPONS, type WeaponDef } from '../data/weapons';
import type { OfferCard } from '../systems/levelup';
import type { PlayerStats } from '../systems/stats';

export type CardTone = RarityId | 'new' | 'heal';

export interface CardView {
  tone: CardTone;
  /** Rareza o "Arma nueva". */
  tag: string;
  title: string;
  /** "Nv 2 → 3", "Tomo nuevo" o vacío. */
  level: string;
  /** Lo que mejora, una línea por estadística. */
  lines: string[];
  /** Descripción con gracia (armas y tomos nuevos, relleno). */
  description: string;
  banishable: boolean;
}

/** Niveles actuales de lo que se tiene, para enseñar "Nv 2 → 3". */
export interface OwnedLevels {
  weapon(id: string): number;
  tome(id: string): number;
}

/** Nombre de cada estadística del jugador que se puede mejorar. */
export const STAT_LABELS: Readonly<Record<BonusStat, TranslationKey>> = {
  damage: 'stat.damage',
  attackSpeed: 'stat.cooldown',
  extraProjectiles: 'stat.extraProjectiles',
  area: 'stat.area',
  moveSpeed: 'stat.moveSpeed',
  maxHp: 'stat.maxHp',
  regen: 'stat.regen',
  luck: 'stat.luck',
  pickupRadius: 'stat.pickupRadius',
  xpGain: 'stat.xpGain',
  critChance: 'stat.critChance',
  critDamage: 'stat.critMultiplier',
  armor: 'stat.armor',
  goldGain: 'stat.goldGain',
  choices: 'stat.choices',
};

/** "25" para 0,25 como porcentaje; sin decimales si no hacen falta. */
function amountText(amount: number, display: 'percent' | 'number'): string {
  return formatNumber(display === 'percent' ? amount * 100 : amount, 1);
}

function capitalize(text: string): string {
  return text.charAt(0).toLocaleUpperCase() + text.slice(1);
}

/** "Daño +25 %", "Saltos +1"... (el nombre delante se lee bien en singular y en plural). */
function changeLine(amount: number, display: 'percent' | 'number', label: string): string {
  return t(display === 'percent' ? 'upgrade.percent' : 'upgrade.number', {
    value: amountText(amount, display),
    stat: capitalize(label),
  });
}

export function weaponStatLabel(def: WeaponDef, stat: WeaponStatKey): string {
  return t(def.statLabels?.[stat] ?? (`stat.${stat}` as TranslationKey));
}

/** "Prob. de crítico +7 %" para una bonificación del jugador. */
export function effectLine(effect: StatEffect, amount: number): string {
  return changeLine(amount, effect.display, t(STAT_LABELS[effect.stat]));
}

export function describeCard(card: OfferCard, owned: OwnedLevels): CardView {
  switch (card.kind) {
    case 'newWeapon': {
      const def = WEAPONS[card.weapon];
      return {
        tone: 'new',
        tag: t('levelup.newWeapon'),
        title: t(def.nameKey),
        level: '',
        lines: [],
        description: t(def.descriptionKey),
        banishable: true,
      };
    }
    case 'weaponUpgrade': {
      const def = WEAPONS[card.weapon];
      const level = owned.weapon(card.weapon);
      return {
        tone: card.rarity,
        tag: t(rarityById(card.rarity).nameKey),
        title: t(def.nameKey),
        level: t('levelup.level', { from: level, to: level + 1 }),
        lines: card.changes.map((c) => changeLine(c.amount, WEAPON_UPGRADE_STEPS[c.stat].display, weaponStatLabel(def, c.stat))),
        description: '',
        banishable: true,
      };
    }
    case 'tome': {
      const def = TOMES[card.tome];
      const level = owned.tome(card.tome);
      return {
        tone: card.rarity,
        tag: t(rarityById(card.rarity).nameKey),
        title: t(def.nameKey),
        level: level === 0 ? t('levelup.newTome') : t('levelup.level', { from: level, to: level + 1 }),
        lines: def.effects.map((effect, i) => effectLine(effect, card.amounts[i] ?? 0)),
        description: level === 0 ? t(def.descriptionKey) : '',
        banishable: true,
      };
    }
    case 'heal':
      return {
        tone: 'heal',
        tag: '',
        title: t('levelup.filler'),
        level: '',
        lines: [],
        description: t('levelup.filler.desc', { n: Math.round(LEVEL_UP_CONFIG.fillerHeal * 100) }),
        banishable: false,
      };
  }
}

// ------------------------------------------------------------------ pausa

export interface StatLine {
  label: string;
  value: string;
}

function percent(value: number): string {
  const sign = value < -1e-9 ? '-' : '+';
  return t('format.percent', { value: `${sign}${formatNumber(Math.abs(value) * 100, 1)}` });
}

/** Estadísticas del personaje tal y como se ven en la pausa. */
export function statLines(stats: PlayerStats, hp: number): StatLine[] {
  const line = (key: TranslationKey, value: string): StatLine => ({ label: capitalize(t(key)), value });
  return [
    line('stat.health', `${Math.ceil(hp)} / ${Math.round(stats.maxHp)}`),
    line('stat.regen', formatNumber(stats.regen, 1)),
    line('stat.armor', formatNumber(stats.armor, 0)),
    line('stat.damage', percent(stats.damage - 1)),
    line('stat.cooldown', percent(stats.attackSpeed - 1)),
    line('stat.extraProjectiles', `+${formatNumber(stats.extraProjectiles, 0)}`),
    line('stat.area', percent(stats.area - 1)),
    line('stat.critChance', percent(stats.critChance)),
    line('stat.moveSpeed', percent(stats.moveSpeed - 1)),
    line('stat.luck', formatNumber(stats.luck, 0)),
    line('stat.pickupRadius', `${formatNumber(stats.pickupRadius, 1)} m`),
    line('stat.xpGain', percent(stats.xpGain - 1)),
  ];
}
