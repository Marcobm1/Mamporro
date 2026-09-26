// Subida de nivel (lógica pura): sorteo de rarezas con la Suerte, generación de
// las cartas y qué mejora cada una. La partida (Run) guarda el estado y las aplica.
// Las bendiciones de los santuarios usan las mismas cartas y rarezas.
import type { Rng } from '../core/rng';
import { RARITIES, type RarityDef, type RarityId } from '../data/rarities';
import { SHRINE_BOOSTS, type ShrineBoost } from '../data/run';
import { TOME_LIST, type TomeDef, type TomeId } from '../data/tomes';
import { LEVEL_UP_CONFIG, WEAPON_UPGRADE_STEPS, type WeaponStatKey } from '../data/upgrades';
import { WEAPON_LIST, type WeaponDef, type WeaponId, type WeaponStats } from '../data/weapons';
import { isPlayerStatCapped, type PlayerStats, type TomeInstance } from './stats';

export interface StatChange {
  stat: WeaponStatKey;
  amount: number;
}

export interface NewWeaponCard {
  kind: 'newWeapon';
  key: string;
  weapon: WeaponId;
}

export interface WeaponUpgradeCard {
  kind: 'weaponUpgrade';
  key: string;
  weapon: WeaponId;
  rarity: RarityId;
  changes: StatChange[];
}

/** Tomo nuevo o mejora de uno que ya se tiene. */
export interface TomeCard {
  kind: 'tome';
  key: string;
  tome: TomeId;
  rarity: RarityId;
  /** Lo que suma a cada efecto del tomo. */
  amounts: number[];
}

/** Relleno cuando no queda nada que ofrecer: cura... */
export interface HealCard {
  kind: 'heal';
  key: null;
  /** Fracción de la vida máxima que recupera. */
  amount: number;
}

/** ...o un puñado de oro. */
export interface GoldCard {
  kind: 'gold';
  key: null;
  amount: number;
}

/** Bendición de un santuario: sube una estadística del jugador para el resto de la partida. */
export interface BoostCard {
  kind: 'boost';
  key: string;
  boost: ShrineBoost;
  rarity: RarityId;
  amount: number;
}

export type OfferCard = NewWeaponCard | WeaponUpgradeCard | TomeCard | HealCard | GoldCard | BoostCard;

/** Lo que el generador de cartas necesita saber de la partida. */
export interface BuildView {
  weapons: ReadonlyArray<{ def: WeaponDef; bonus: WeaponStats }>;
  tomes: readonly TomeInstance[];
  stats: PlayerStats;
  /** Claves descartadas para el resto de la partida. */
  banished: ReadonlySet<string>;
  /** Oro que da la carta de relleno de oro. */
  fillerGold: number;
}

export const weaponKey = (id: WeaponId): string => `weapon:${id}`;
export const tomeKey = (id: TomeId): string => `tome:${id}`;

// ------------------------------------------------------------------ rarezas

/** Peso de cada rareza (mismo orden que RARITIES) para una Suerte dada. */
export function rarityWeights(luck: number): number[] {
  const l = Math.max(0, luck) / 100;
  return RARITIES.map((r) => r.weight * (1 + l * r.luckBonus));
}

export function rollRarity(luck: number, rng: Rng): RarityDef {
  const weights = rarityWeights(luck);
  const total = weights.reduce((a, b) => a + b, 0);
  let roll = rng.next() * total;
  for (let i = 0; i < RARITIES.length; i++) {
    roll -= weights[i] as number;
    if (roll < 0) return RARITIES[i] as RarityDef;
  }
  return RARITIES[RARITIES.length - 1] as RarityDef;
}

// ------------------------------------------------------------------ mejoras

/** Cuánto sube una mejora de `amount` (Común) con la potencia de su rareza. */
export function scaledAmount(amount: number, power: number, integer = false): number {
  if (integer) return Math.max(1, Math.floor(amount * power + 1e-9));
  return Math.round(amount * power * 1000) / 1000;
}

/** Estadísticas del arma que aún pueden subir (las de su lista que no están al tope). */
export function upgradableStats(def: WeaponDef, bonus: WeaponStats): WeaponStatKey[] {
  return def.upgradable.filter((stat) => {
    const max = WEAPON_UPGRADE_STEPS[stat].maxBonus;
    return max === undefined || bonus[stat] < max - 1e-9;
  });
}

/** Sortea qué estadísticas sube una mejora de arma y cuánto (1–2; siempre 2 desde Rara). */
export function rollWeaponUpgrade(def: WeaponDef, bonus: WeaponStats, rarity: RarityDef, rng: Rng): StatChange[] {
  const pool = rng.shuffle(upgradableStats(def, bonus));
  const [min, max] = rarity.stats;
  const n = Math.min(pool.length, rng.int(min, max));
  return pool.slice(0, n).map((stat) => {
    const step = WEAPON_UPGRADE_STEPS[stat];
    let amount = scaledAmount(step.amount, rarity.power, step.integer);
    // Sin pasarse del tope: la carta enseña lo que de verdad va a subir.
    if (step.maxBonus !== undefined) amount = Math.min(amount, step.maxBonus - bonus[stat]);
    return { stat, amount };
  });
}

/** Lo que suma un nivel de tomo de esta rareza a cada uno de sus efectos. */
export function tomeAmounts(def: TomeDef, rarity: RarityDef): number[] {
  return def.effects.map((e) => scaledAmount(e.amount, rarity.power, e.integer));
}

/** Un tomo deja de salir cuando todas sus estadísticas están al tope. */
export function isTomeUseful(def: TomeDef, stats: PlayerStats): boolean {
  return def.effects.some((e) => !isPlayerStatCapped(stats, e.stat));
}

// ------------------------------------------------------------------ cartas

interface Candidate {
  key: string;
  weight: number;
  make(rng: Rng): OfferCard;
}

function candidates(build: BuildView, exclude: ReadonlySet<string>): Candidate[] {
  const out: Candidate[] = [];
  const W = LEVEL_UP_CONFIG.weights;
  const allowed = (key: string): boolean => !build.banished.has(key) && !exclude.has(key);
  const luck = build.stats.luck;

  for (const w of build.weapons) {
    const key = weaponKey(w.def.id);
    if (!allowed(key) || upgradableStats(w.def, w.bonus).length === 0) continue;
    out.push({
      key,
      weight: W.weaponUpgrade,
      make: (rng) => {
        const rarity = rollRarity(luck, rng);
        return { kind: 'weaponUpgrade', key, weapon: w.def.id, rarity: rarity.id, changes: rollWeaponUpgrade(w.def, w.bonus, rarity, rng) };
      },
    });
  }
  if (build.weapons.length < LEVEL_UP_CONFIG.maxWeapons) {
    for (const def of WEAPON_LIST) {
      const key = weaponKey(def.id);
      if (!allowed(key) || build.weapons.some((w) => w.def.id === def.id)) continue;
      out.push({ key, weight: W.newWeapon, make: () => ({ kind: 'newWeapon', key, weapon: def.id }) });
    }
  }
  const owned = new Set(build.tomes.map((t) => t.def.id));
  const canAddTome = build.tomes.length < LEVEL_UP_CONFIG.maxTomes;
  for (const def of TOME_LIST) {
    const key = tomeKey(def.id);
    const has = owned.has(def.id);
    if (!allowed(key) || (!has && !canAddTome) || !isTomeUseful(def, build.stats)) continue;
    out.push({
      key,
      weight: has ? W.tomeUpgrade : W.newTome,
      make: (rng) => {
        const rarity = rollRarity(luck, rng);
        return { kind: 'tome', key, tome: def.id, rarity: rarity.id, amounts: tomeAmounts(def, rarity) };
      },
    });
  }
  return out;
}

export function healCard(): HealCard {
  return { kind: 'heal', key: null, amount: LEVEL_UP_CONFIG.fillerHeal };
}

export function goldCard(amount: number): GoldCard {
  return { kind: 'gold', key: null, amount };
}

/** Cartas de relleno, en orden de preferencia. */
function fillerCards(build: BuildView): OfferCard[] {
  return [healCard(), goldCard(build.fillerGold)];
}

/**
 * Genera `count` cartas distintas (nunca dos del mismo arma o tomo). Si no hay
 * bastantes, completa con las de relleno (curar y oro); `exclude` deja fuera
 * claves (p. ej. las que ya están en la mesa al sustituir una carta descartada).
 */
export function generateOffer(build: BuildView, count: number, rng: Rng, exclude: ReadonlySet<string> = new Set()): OfferCard[] {
  const pool = candidates(build, exclude);
  const cards: OfferCard[] = [];
  while (cards.length < count && pool.length > 0) {
    const total = pool.reduce((sum, c) => sum + c.weight, 0);
    let roll = rng.next() * total;
    let index = pool.length - 1;
    for (let i = 0; i < pool.length; i++) {
      roll -= (pool[i] as Candidate).weight;
      if (roll < 0) {
        index = i;
        break;
      }
    }
    const [picked] = pool.splice(index, 1);
    if (picked) cards.push(picked.make(rng));
  }
  for (const filler of fillerCards(build)) {
    if (cards.length >= count) break;
    cards.push(filler);
  }
  return cards;
}

/** Una carta nueva para el hueco de una descartada (o null si ya no queda nada). */
export function replacementCard(build: BuildView, table: readonly OfferCard[], rng: Rng): OfferCard | null {
  const exclude = new Set(table.map((c) => c.key).filter((k): k is string => k !== null));
  const [card] = generateOffer(build, 1, rng, exclude);
  if (card && card.key !== null) return card;
  // Solo queda relleno: uno que no esté ya en la mesa.
  return fillerCards(build).find((f) => !table.some((c) => c.kind === f.kind)) ?? null;
}

// ------------------------------------------------------------------ santuarios

/** Una bendición es útil mientras su estadística no esté al tope. */
export function isBoostUseful(boost: ShrineBoost, stats: PlayerStats): boolean {
  return !isPlayerStatCapped(stats, boost.effect.stat);
}

/** `count` bendiciones distintas, cada una con su rareza (la Suerte ayuda). */
export function generateShrineOffer(stats: PlayerStats, count: number, rng: Rng, boosts: readonly ShrineBoost[] = SHRINE_BOOSTS): OfferCard[] {
  const pool = rng.shuffle(boosts.filter((b) => isBoostUseful(b, stats)));
  const cards: OfferCard[] = pool.slice(0, count).map((boost) => {
    const rarity = rollRarity(stats.luck, rng);
    return {
      kind: 'boost',
      key: `boost:${boost.id}`,
      boost,
      rarity: rarity.id,
      amount: scaledAmount(boost.effect.amount, rarity.power, boost.effect.integer),
    };
  });
  // Con todo al tope (casi imposible), al menos se cura.
  if (cards.length === 0) cards.push(healCard());
  return cards;
}
