// Tomos: cada uno mejora una estadística global del personaje (afecta a todas las
// armas). Las mejoras se suman nivel a nivel; la rareza de la carta las multiplica.
import type { TranslationKey } from '../i18n';
import type { StatEffect } from './bonuses';

export type TomeId = 'damage' | 'attackSpeed' | 'projectiles' | 'area' | 'moveSpeed' | 'vitality' | 'luck' | 'magnet';

/** Un efecto de tomo es una bonificación de estadística como las de objetos y santuarios. */
export type TomeEffect = StatEffect;

export interface TomeDef {
  id: TomeId;
  nameKey: TranslationKey;
  /** Nombre corto para el HUD. */
  shortKey: TranslationKey;
  descriptionKey: TranslationKey;
  effects: readonly TomeEffect[];
}

export const TOMES: Readonly<Record<TomeId, TomeDef>> = {
  damage: {
    id: 'damage',
    nameKey: 'tome.damage',
    shortKey: 'tome.damage.short',
    descriptionKey: 'tome.damage.desc',
    effects: [{ stat: 'damage', amount: 0.12, mode: 'add', display: 'percent' }],
  },
  attackSpeed: {
    id: 'attackSpeed',
    nameKey: 'tome.attackSpeed',
    shortKey: 'tome.attackSpeed.short',
    descriptionKey: 'tome.attackSpeed.desc',
    effects: [{ stat: 'attackSpeed', amount: 0.1, mode: 'add', display: 'percent' }],
  },
  projectiles: {
    id: 'projectiles',
    nameKey: 'tome.projectiles',
    shortKey: 'tome.projectiles.short',
    descriptionKey: 'tome.projectiles.desc',
    effects: [{ stat: 'extraProjectiles', amount: 1, mode: 'add', integer: true, display: 'number' }],
  },
  area: {
    id: 'area',
    nameKey: 'tome.area',
    shortKey: 'tome.area.short',
    descriptionKey: 'tome.area.desc',
    effects: [{ stat: 'area', amount: 0.12, mode: 'add', display: 'percent' }],
  },
  moveSpeed: {
    id: 'moveSpeed',
    nameKey: 'tome.moveSpeed',
    shortKey: 'tome.moveSpeed.short',
    descriptionKey: 'tome.moveSpeed.desc',
    effects: [{ stat: 'moveSpeed', amount: 0.08, mode: 'add', display: 'percent' }],
  },
  vitality: {
    id: 'vitality',
    nameKey: 'tome.vitality',
    shortKey: 'tome.vitality.short',
    descriptionKey: 'tome.vitality.desc',
    effects: [
      { stat: 'maxHp', amount: 20, mode: 'add', integer: true, display: 'number' },
      { stat: 'regen', amount: 0.4, mode: 'add', display: 'number' },
    ],
  },
  luck: {
    id: 'luck',
    nameKey: 'tome.luck',
    shortKey: 'tome.luck.short',
    descriptionKey: 'tome.luck.desc',
    effects: [{ stat: 'luck', amount: 10, mode: 'add', integer: true, display: 'number' }],
  },
  magnet: {
    id: 'magnet',
    nameKey: 'tome.magnet',
    shortKey: 'tome.magnet.short',
    descriptionKey: 'tome.magnet.desc',
    effects: [
      { stat: 'pickupRadius', amount: 0.25, mode: 'base', display: 'percent' },
      { stat: 'xpGain', amount: 0.08, mode: 'add', display: 'percent' },
    ],
  },
};

export const TOME_LIST: readonly TomeDef[] = Object.values(TOMES);
