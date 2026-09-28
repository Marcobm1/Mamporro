// Progreso persistente puro: ninguna recompensa depende del render.
import { EXTRA_PRICES, INITIAL_ITEMS, INITIAL_WEAPONS, META_REWARD, MISSIONS, SHOP, type ExtraAction, type MissionId, type Unlock } from '../data/meta';
import { ITEM_LIST, type ItemId } from '../data/items';
import { WEAPON_LIST, type WeaponId } from '../data/weapons';

export interface MetaProgress {
  coins: number;
  selected: 'remedios' | 'baguette';
  characters: Array<'remedios' | 'baguette'>;
  weapons: WeaponId[];
  items: ItemId[];
  extras: Record<ExtraAction, number>;
  missions: Record<MissionId, number>;
  completed: MissionId[];
  lastRun: string;
}
export interface MetaRun {
  id: string; cheated: boolean; time: number; kills: number; chests: number;
  shrines: number; challenges: number; level: number; victory: boolean; usedLifeTome: boolean;
}
export interface MetaReceipt { kills: number; survival: number; victory: number; missions: number; total: number; completed: MissionId[] }
export function defaultMeta(): MetaProgress {
  return { coins: 0, selected: 'remedios', characters: ['remedios'], weapons: [...INITIAL_WEAPONS], items: [...INITIAL_ITEMS],
    extras: { rerolls: 0, skips: 0, banishes: 0 },
    missions: { first: 0, kills: 0, chests: 0, shrines: 0, challenge: 0, level: 0, victory: 0, noLife: 0 }, completed: [], lastRun: '' };
}
const object = (v: unknown): Record<string, unknown> => typeof v === 'object' && v !== null && !Array.isArray(v) ? v as Record<string, unknown> : {};
const integer = (v: unknown, max = 1_000_000_000): number => typeof v === 'number' && Number.isFinite(v) ? Math.min(max, Math.max(0, Math.floor(v))) : 0;
export function sanitizeMeta(raw: unknown): MetaProgress {
  const r = object(raw), m = defaultMeta();
  m.coins = integer(r.coins);
  if (Array.isArray(r.characters) && r.characters.includes('baguette')) m.characters.push('baguette');
  m.selected = r.selected === 'baguette' && m.characters.includes('baguette') ? 'baguette' : 'remedios';
  m.weapons = WEAPON_LIST.filter(d => INITIAL_WEAPONS.includes(d.id) || (Array.isArray(r.weapons) && r.weapons.includes(d.id))).map(d => d.id);
  m.items = ITEM_LIST.filter(d => INITIAL_ITEMS.includes(d.id) || (Array.isArray(r.items) && r.items.includes(d.id))).map(d => d.id);
  const extras = object(r.extras), progress = object(r.missions);
  for (const key of ['rerolls', 'skips', 'banishes'] as const) m.extras[key] = integer(extras[key], EXTRA_PRICES.length);
  for (const mission of MISSIONS) {
    const done = Array.isArray(r.completed) && r.completed.includes(mission.id);
    m.missions[mission.id] = done ? mission.target : integer(progress[mission.id], mission.target);
    if (done) { m.completed.push(mission.id); if (mission.unlock) grantUnlock(m, mission.unlock); }
  }
  m.lastRun = typeof r.lastRun === 'string' ? r.lastRun.slice(0, 100) : '';
  return m;
}
export function ownsUnlock(m: MetaProgress, u: Unlock): boolean {
  return u.kind === 'weapon' ? m.weapons.includes(u.id) : u.kind === 'item' ? m.items.includes(u.id) : m.characters.includes(u.id);
}
function grantUnlock(m: MetaProgress, u: Unlock): void {
  if (ownsUnlock(m, u)) return;
  if (u.kind === 'weapon') m.weapons.push(u.id);
  else if (u.kind === 'item') m.items.push(u.id);
  else m.characters.push(u.id);
}
export function purchase(m: MetaProgress, id: string): boolean {
  const entry = SHOP.find(s => s.id === id);
  if (!entry || ownsUnlock(m, entry.unlock) || m.coins < entry.price) return false;
  m.coins -= entry.price; grantUnlock(m, entry.unlock); return true;
}
export function purchaseExtra(m: MetaProgress, action: ExtraAction): boolean {
  const price = EXTRA_PRICES[m.extras[action]];
  if (price === undefined || m.coins < price) return false;
  m.coins -= price; m.extras[action]++; return true;
}
/** Una única liquidación por partida; el orquestador solo llama al morir o ganar. */
export function settleRun(m: MetaProgress, r: MetaRun): MetaReceipt {
  const receipt: MetaReceipt = { kills: 0, survival: 0, victory: 0, missions: 0, total: 0, completed: [] };
  if (!r.id || r.id === m.lastRun || r.cheated) return receipt;
  m.lastRun = r.id;
  receipt.kills = Math.floor(integer(r.kills) / META_REWARD.killsPerCoin);
  receipt.survival = Math.min(META_REWARD.maxTimeCoins, Math.floor(integer(r.time) / META_REWARD.secondsPerCoin));
  receipt.victory = r.victory ? META_REWARD.victory : 0;
  const values: Record<MissionId, number> = {
    first: m.missions.first + 1, kills: m.missions.kills + integer(r.kills), chests: m.missions.chests + integer(r.chests),
    shrines: m.missions.shrines + integer(r.shrines), challenge: m.missions.challenge + integer(r.challenges),
    level: Math.max(m.missions.level, integer(r.level)), victory: m.missions.victory + Number(r.victory),
    noLife: m.missions.noLife + Number(r.victory && !r.usedLifeTome),
  };
  for (const mission of MISSIONS) {
    m.missions[mission.id] = Math.min(mission.target, values[mission.id]);
    if (m.missions[mission.id] < mission.target || m.completed.includes(mission.id)) continue;
    m.completed.push(mission.id); receipt.completed.push(mission.id); receipt.missions += mission.coins;
    if (mission.unlock) grantUnlock(m, mission.unlock);
  }
  receipt.total = receipt.kills + receipt.survival + receipt.victory + receipt.missions;
  m.coins = integer(m.coins + receipt.total);
  return receipt;
}
