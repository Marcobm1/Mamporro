// Objetos (lógica pura): sorteo del objeto de un cofre según la Suerte, lo que
// suman a las estadísticas y los números de los que tienen efectos especiales.
import type { Rng } from '../core/rng';
import { ITEM_EFFECTS, ITEM_LIST, type ItemDef, type ItemId } from '../data/items';
import { RARITIES, type RarityId } from '../data/rarities';
import { rollRarity } from './levelup';
import { effectBonuses, isPlayerStatCapped, type AppliedBonus, type PlayerStats } from './stats';

/** Un objeto que se tiene y cuántas copias. */
export interface ItemStack {
  def: ItemDef;
  count: number;
}

/** Lo que suman a las estadísticas los objetos que se tienen. */
export function itemBonuses(items: readonly ItemStack[]): AppliedBonus[] {
  return items.flatMap((stack) => effectBonuses(stack.def.effects, stack.count));
}

export function itemCount(items: readonly ItemStack[], id: ItemId): number {
  return items.find((s) => s.def.id === id)?.count ?? 0;
}

/**
 * ¿Puede salir este objeto? No, si ya se tienen todas las copias que sirven o si
 * solo sube estadísticas que ya están al tope.
 */
export function isItemAvailable(def: ItemDef, owned: number, stats: PlayerStats): boolean {
  if (def.maxStacks !== undefined && owned >= def.maxStacks) return false;
  if (def.effects.length === 0) return true;
  return def.effects.some((e) => !isPlayerStatCapped(stats, e.stat));
}

/**
 * Sortea un objeto: primero la rareza (con la Suerte) y luego uno de esa rareza.
 * Si no queda ninguno de esa rareza, prueba con las más cercanas (antes las de abajo).
 */
export function rollItem(luck: number, rng: Rng, items: readonly ItemStack[], stats: PlayerStats, allowed?: readonly ItemId[]): ItemDef | null {
  const rarity = rollRarity(luck, rng);
  const order = RARITIES.map((r, i) => ({ id: r.id, i }));
  const start = order.findIndex((r) => r.id === rarity.id);
  order.sort((a, b) => {
    const da = Math.abs(a.i - start);
    const db = Math.abs(b.i - start);
    return da !== db ? da - db : a.i - b.i;
  });
  for (const { id } of order) {
    const pool = candidatesOf(id, items, stats, allowed);
    if (pool.length > 0) return rng.pick(pool);
  }
  return null;
}

function candidatesOf(rarity: RarityId, items: readonly ItemStack[], stats: PlayerStats, allowed?: readonly ItemId[]): ItemDef[] {
  return ITEM_LIST.filter((def) => (!allowed || allowed.includes(def.id)) && def.rarity === rarity && isItemAvailable(def, itemCount(items, def.id), stats));
}

// ------------------------------------------------------------------ efectos especiales

/** Probabilidad de que salte una perla en un crítico (cada copia suma). */
export function pearlChance(copies: number): number {
  return Math.min(1, ITEM_EFFECTS.perlas.chance * copies);
}

/** Probabilidad de que un enemigo explote al morir (cada copia suma, con tope). */
export function ollaChance(copies: number): number {
  return Math.min(ITEM_EFFECTS.olla.maxChance, ITEM_EFFECTS.olla.chance * copies);
}

/** Daño extra del monedero: por cada 100 de oro que se lleva, con tope por copia. */
export function purseBonus(gold: number, copies: number): number {
  const m = ITEM_EFFECTS.monedero;
  return Math.min(m.maxBonus, Math.floor(Math.max(0, gold) / 100) * m.damagePer100) * copies;
}
