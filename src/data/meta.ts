// Economía y objetivos: todos los números se ajustan aquí.
import type { ItemId } from './items';
import type { WeaponId } from './weapons';
import type { TranslationKey } from '../i18n';

export const INITIAL_WEAPONS: readonly WeaponId[] = ['chancla', 'naftalina', 'barra', 'dentaduras'];
export const INITIAL_ITEMS: readonly ItemId[] = ['gafas', 'zapatillas', 'termo', 'cojin', 'lupa', 'loteria', 'rulos', 'monedero'];
export type ExtraAction = 'rerolls' | 'skips' | 'banishes';
export type Unlock = { kind: 'weapon'; id: WeaponId } | { kind: 'item'; id: ItemId } | { kind: 'character'; id: 'baguette' };
export type MissionId = 'first' | 'kills' | 'chests' | 'shrines' | 'challenge' | 'level' | 'victory' | 'noLife';
export interface MissionDef { id: MissionId; nameKey: TranslationKey; target: number; coins: number; unlock?: Unlock }
export const MISSIONS: readonly MissionDef[] = [
  { id: 'first', nameKey: 'mission.first', target: 1, coins: 30 },
  { id: 'kills', nameKey: 'mission.kills', target: 1000, coins: 60, unlock: { kind: 'weapon', id: 'fregona' } },
  { id: 'chests', nameKey: 'mission.chests', target: 10, coins: 40 },
  { id: 'shrines', nameKey: 'mission.shrines', target: 3, coins: 40 },
  { id: 'challenge', nameKey: 'mission.challenge', target: 1, coins: 50, unlock: { kind: 'item', id: 'perlas' } },
  { id: 'level', nameKey: 'mission.level', target: 20, coins: 50 },
  { id: 'victory', nameKey: 'mission.victory', target: 1, coins: 80 },
  { id: 'noLife', nameKey: 'mission.noLife', target: 1, coins: 100, unlock: { kind: 'item', id: 'bata' } },
];
export interface ShopDef { id: string; price: number; unlock: Unlock }
export const SHOP: readonly ShopDef[] = [
  { id: 'baguette', price: 220, unlock: { kind: 'character', id: 'baguette' } },
  { id: 'jersey', price: 140, unlock: { kind: 'weapon', id: 'jersey' } },
  { id: 'baraja', price: 160, unlock: { kind: 'item', id: 'baraja' } },
  { id: 'olla', price: 180, unlock: { kind: 'item', id: 'olla' } },
];
export const EXTRA_PRICES = [80, 140, 220] as const;
export const META_REWARD = { killsPerCoin: 20, secondsPerCoin: 15, maxTimeCoins: 60, victory: 60 } as const;
