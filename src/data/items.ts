// Objetos: efectos pasivos que se consiguen en cofres, tótems y otras recompensas.
// No ocupan huecos y se acumulan: cada copia suma su efecto otra vez. Hay
// sinergias (críticos, oro, bajas) entre ellos y con los tomos.
import type { TranslationKey } from '../i18n';
import type { StatEffect } from './bonuses';
import type { RarityId } from './rarities';

export type ItemId =
  | 'gafas'
  | 'zapatillas'
  | 'termo'
  | 'cojin'
  | 'lupa'
  | 'loteria'
  | 'rulos'
  | 'perlas'
  | 'monedero'
  | 'baraja'
  | 'olla'
  | 'bata';

export interface ItemDef {
  id: ItemId;
  nameKey: TranslationKey;
  descriptionKey: TranslationKey;
  rarity: RarityId;
  /** Lo que suma cada copia a las estadísticas. */
  effects: readonly StatEffect[];
  /** Copias que aún sirven de algo (deja de salir al llegar). */
  maxStacks?: number;
}

export const ITEMS: Readonly<Record<ItemId, ItemDef>> = {
  // Comunes
  gafas: {
    id: 'gafas',
    nameKey: 'item.gafas',
    descriptionKey: 'item.gafas.desc',
    rarity: 'common',
    effects: [{ stat: 'critChance', amount: 0.07, mode: 'add', display: 'percent' }],
  },
  zapatillas: {
    id: 'zapatillas',
    nameKey: 'item.zapatillas',
    descriptionKey: 'item.zapatillas.desc',
    rarity: 'common',
    effects: [{ stat: 'moveSpeed', amount: 0.08, mode: 'add', display: 'percent' }],
  },
  termo: {
    id: 'termo',
    nameKey: 'item.termo',
    descriptionKey: 'item.termo.desc',
    rarity: 'common',
    effects: [{ stat: 'attackSpeed', amount: 0.08, mode: 'add', display: 'percent' }],
  },
  cojin: {
    id: 'cojin',
    nameKey: 'item.cojin',
    descriptionKey: 'item.cojin.desc',
    rarity: 'common',
    effects: [{ stat: 'armor', amount: 4, mode: 'add', integer: true, display: 'number' }],
  },
  // Poco comunes
  lupa: {
    id: 'lupa',
    nameKey: 'item.lupa',
    descriptionKey: 'item.lupa.desc',
    rarity: 'uncommon',
    effects: [{ stat: 'critDamage', amount: 0.25, mode: 'add', display: 'percent' }],
  },
  loteria: {
    id: 'loteria',
    nameKey: 'item.loteria',
    descriptionKey: 'item.loteria.desc',
    rarity: 'uncommon',
    effects: [{ stat: 'goldGain', amount: 0.4, mode: 'add', display: 'percent' }],
  },
  rulos: {
    id: 'rulos',
    nameKey: 'item.rulos',
    descriptionKey: 'item.rulos.desc',
    rarity: 'uncommon',
    effects: [{ stat: 'luck', amount: 15, mode: 'add', integer: true, display: 'number' }],
  },
  // Raros
  perlas: {
    id: 'perlas',
    nameKey: 'item.perlas',
    descriptionKey: 'item.perlas.desc',
    rarity: 'rare',
    effects: [],
  },
  monedero: {
    id: 'monedero',
    nameKey: 'item.monedero',
    descriptionKey: 'item.monedero.desc',
    rarity: 'rare',
    effects: [],
  },
  baraja: {
    id: 'baraja',
    nameKey: 'item.baraja',
    descriptionKey: 'item.baraja.desc',
    rarity: 'rare',
    effects: [{ stat: 'choices', amount: 1, mode: 'add', integer: true, display: 'number' }],
    maxStacks: 1,
  },
  // Épico
  olla: {
    id: 'olla',
    nameKey: 'item.olla',
    descriptionKey: 'item.olla.desc',
    rarity: 'epic',
    effects: [],
  },
  // Legendario
  bata: {
    id: 'bata',
    nameKey: 'item.bata',
    descriptionKey: 'item.bata.desc',
    rarity: 'legendary',
    effects: [],
  },
};

export const ITEM_LIST: readonly ItemDef[] = Object.values(ITEMS);

/** Parámetros de los objetos con efectos especiales (por copia, salvo que se diga). */
export const ITEM_EFFECTS = {
  /** Collar de Perlas: al hacer un crítico, probabilidad de que una perla salte al enemigo más cercano. */
  perlas: { chance: 0.3, damageFraction: 0.5, range: 7 },
  /** Monedero Bien Lleno: +daño por cada 100 de oro que llevas, con tope. */
  monedero: { damagePer100: 0.04, maxBonus: 0.8 },
  /**
   * Olla Exprés: al morir un enemigo, probabilidad de que explote dañando a los de
   * alrededor: `damage` (× tu daño) más una fracción de la vida máxima del que explota.
   * El radio crece con tu área.
   */
  olla: { chance: 0.08, maxChance: 0.4, damage: 16, hpFraction: 0.5, radius: 3.2 },
  /** Bata de Guatiné: cada copia te salva una vez de la muerte. */
  bata: { reviveHp: 0.5, invulnerability: 2.5, shockwaveRadius: 8, shockwavePush: 14 },
} as const;
