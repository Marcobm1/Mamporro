// Rarezas de las cartas de mejora. La Suerte desplaza el sorteo hacia las altas,
// que mejoran más (y, en las armas, siempre dos estadísticas).
import type { TranslationKey } from '../i18n';

export type RarityId = 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';

export interface RarityDef {
  id: RarityId;
  nameKey: TranslationKey;
  /** Peso en el sorteo con Suerte 0. */
  weight: number;
  /** Cuánto crece ese peso por cada 100 de Suerte (1 = el doble). */
  luckBonus: number;
  /** Multiplicador de lo que mejora la carta. */
  power: number;
  /** Estadísticas que sube una mejora de arma: [mínimo, máximo]. */
  stats: readonly [number, number];
}

/** De la más común a la más rara. */
export const RARITIES: readonly RarityDef[] = [
  { id: 'common', nameKey: 'rarity.common', weight: 60, luckBonus: 0, power: 1, stats: [1, 2] },
  { id: 'uncommon', nameKey: 'rarity.uncommon', weight: 25, luckBonus: 1.5, power: 1.3, stats: [1, 2] },
  { id: 'rare', nameKey: 'rarity.rare', weight: 10, luckBonus: 3, power: 1.65, stats: [2, 2] },
  { id: 'epic', nameKey: 'rarity.epic', weight: 4, luckBonus: 5, power: 2.1, stats: [2, 2] },
  { id: 'legendary', nameKey: 'rarity.legendary', weight: 1, luckBonus: 8, power: 2.7, stats: [2, 2] },
];

export function rarityById(id: RarityId): RarityDef {
  const def = RARITIES.find((r) => r.id === id);
  if (!def) throw new Error(`Rareza desconocida: ${id}`);
  return def;
}
