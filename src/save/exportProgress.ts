// Exportación pura del estado cargado: no accede a almacenamiento ni liquida partidas.
import { CHARACTERS } from '../data/characters';
import { ITEM_LIST } from '../data/items';
import { WEAPON_LIST } from '../data/weapons';
import { EXTRA_PRICES, INITIAL_ITEMS, INITIAL_WEAPONS, MISSIONS } from '../data/meta';
import type { Language } from '../i18n';
import { defaultMeta } from '../systems/meta';
import { defaultSettings, parseSave, type SaveData } from './schema';

export type ExportReason = 'preserve' | 'default' | 'exclude' | 'migrate' | 'invalid' | 'limit' | 'read' | 'unsupported';
export interface ExportIssue { path: string; reason: ExportReason; before?: string; after?: string }
export interface ExportResult { json: string | null; issues: ExportIssue[] }
type ObjectData = Record<string, unknown>;
const object = (value: unknown): value is ObjectData => value !== null && typeof value === 'object' && !Array.isArray(value);
const pointer = (key: string): string => key.replaceAll('~', '~0').replaceAll('/', '~1');
const excerpt = (value: unknown): string => value === undefined ? '—' : JSON.stringify(value).slice(0, 160);
class Invalid extends Error {
  constructor(readonly path: string, readonly reason: ExportReason) { super(reason); }
}
function fail(path: string, reason: ExportReason = 'invalid'): never { throw new Invalid(path, reason); }

// No invocar getters/toJSON: únicamente árboles de datos, con presupuestos antes de serializar.
function snapshot(input: unknown): unknown {
  let nodes = 0;
  function copy(value: unknown, path: string, depth: number): unknown {
    if (++nodes > 4096 || depth > 16) fail(path, 'limit');
    if (typeof value === 'string') {
      if (value.length > 1024) fail(path, 'limit');
      for (let i = 0; i < value.length; i++) {
        const c = value.charCodeAt(i);
        if (c >= 0xd800 && c <= 0xdbff) { const next = value.charCodeAt(++i); if (!(next >= 0xdc00 && next <= 0xdfff)) fail(path); }
        else if (c >= 0xdc00 && c <= 0xdfff) fail(path);
      }
      return value;
    }
    if (value === null || typeof value === 'boolean' || typeof value === 'number') return value;
    if (typeof value !== 'object') fail(path);
    const array = Array.isArray(value);
    if (!array && Object.getPrototypeOf(value) !== Object.prototype && Object.getPrototypeOf(value) !== null) fail(path);
    const keys = Object.keys(value);
    if (keys.length > 128 || (array && value.length > 128)) fail(path, 'limit');
    const result: ObjectData = Object.create(null) as ObjectData;
    for (const key of keys) {
      if (key.length > 1024) fail(path, 'limit');
      const descriptor = Object.getOwnPropertyDescriptor(value, key);
      if (!descriptor || !('value' in descriptor)) fail(path);
      result[key] = copy(descriptor.value, `${path}/${pointer(key)}`, depth + 1);
    }
    if (array) {
      if (keys.length !== value.length || keys.some((key, i) => key !== String(i))) fail(path);
      return keys.map(key => result[key]);
    }
    return result;
  }
  const result = copy(input, '', 0);
  if (new TextEncoder().encode(JSON.stringify(result)).length > 262144) fail('', 'limit');
  return result;
}

export function canonicalProgressJson(value: unknown): string {
  if (Array.isArray(value)) return `[${value.map(canonicalProgressJson).join(',')}]`;
  if (object(value)) return `{${Object.keys(value).sort().map(key => `${JSON.stringify(key)}:${canonicalProgressJson(value[key])}`).join(',')}}`;
  return JSON.stringify(value);
}

function normalize(input: unknown, language: Language, issues: ExportIssue[]): SaveData {
  if (!object(input)) fail('');
  const version = input.version;
  if (typeof version !== 'number' || !Number.isInteger(version) || version < 1 || version > 3) fail('/version', 'unsupported');
  function extras(data: ObjectData, allowed: string[], path: string): void {
    for (const key of Object.keys(data)) if (!allowed.includes(key)) issues.push({ path: `${path}/${pointer(key)}`, reason: 'exclude', before: excerpt(data[key]) });
  }
  extras(input, ['version', 'settings', 'meta'], '');
  const settings = defaultSettings(language);
  const rawSettings = object(input.settings) ? input.settings : {};
  if (!object(input.settings)) issues.push({ path: '/settings', reason: 'default', before: excerpt(input.settings) });
  extras(rawSettings, Object.keys(settings), '/settings');
  for (const key of Object.keys(settings) as Array<keyof typeof settings>) {
    const value = rawSettings[key];
    let valid = typeof value === typeof settings[key];
    if (typeof settings[key] === 'number') valid = typeof value === 'number' && Number.isFinite(value);
    if (key === 'language') valid = value === 'es' || value === 'en';
    if (key === 'renderHeight') valid = [240, 360, 480].includes(value as number);
    if (key === 'runMinutes') valid = [5, 10, 15].includes(value as number);
    if (key === 'musicVolume' || key === 'effectsVolume') valid = valid && (value as number) >= 0 && (value as number) <= 1;
    if (key === 'mouseSensitivity') valid = valid && (value as number) >= .2 && (value as number) <= 3;
    if (valid) Object.assign(settings, { [key]: value });
    issues.push({ path: `/settings/${key}`, reason: valid ? 'preserve' : 'default', before: excerpt(value), after: excerpt(settings[key]) });
  }
  if (version === 1) {
    if ('meta' in input) fail('/meta');
    issues.push({ path: '/meta', reason: 'migrate' });
    return { version: 3, settings, meta: defaultMeta() };
  }
  if (!object(input.meta)) fail('/meta');
  const raw = input.meta, meta = defaultMeta();
  extras(raw, Object.keys(meta), '/meta');
  function integer(value: unknown, max: number, path: string): number {
    if (typeof value !== 'number' || !Number.isInteger(value) || value < 0 || value > max) fail(path);
    issues.push({ path, reason: 'preserve', before: excerpt(value), after: excerpt(value) }); return value;
  }
  function ids<T extends string>(value: unknown, catalog: readonly T[], required: readonly T[], path: string): T[] {
    if (!Array.isArray(value)) fail(path);
    const seen = new Set<string>();
    for (const [i, id] of value.entries()) {
      if (typeof id !== 'string') fail(`${path}/${i}`);
      if (id.length > 64) fail(`${path}/${i}`, 'limit');
      if (!catalog.includes(id as T) || seen.has(id)) issues.push({ path: `${path}/${i}`, reason: 'exclude', before: excerpt(id) });
      seen.add(id);
    }
    if (required.some(id => !seen.has(id))) fail(path);
    const result = catalog.filter(id => seen.has(id));
    issues.push({ path, reason: 'preserve', after: excerpt(result) }); return result;
  }
  meta.coins = integer(raw.coins, 1_000_000_000, '/meta/coins');
  meta.characters = ids(raw.characters, Object.keys(CHARACTERS) as Array<'remedios' | 'baguette'>, ['remedios'], '/meta/characters');
  meta.weapons = ids(raw.weapons, WEAPON_LIST.map(x => x.id), INITIAL_WEAPONS, '/meta/weapons');
  meta.items = ids(raw.items, ITEM_LIST.map(x => x.id), INITIAL_ITEMS, '/meta/items');
  meta.completed = ids(raw.completed, MISSIONS.map(x => x.id), [], '/meta/completed');
  if (typeof raw.selected !== 'string' || !meta.characters.includes(raw.selected as typeof meta.selected)) fail('/meta/selected');
  meta.selected = raw.selected as typeof meta.selected;
  if (typeof raw.lastRun !== 'string' || raw.lastRun.length > 100) fail('/meta/lastRun');
  meta.lastRun = raw.lastRun;
  if (!object(raw.extras)) fail('/meta/extras');
  if (!object(raw.missions)) fail('/meta/missions');
  extras(raw.extras, Object.keys(meta.extras), '/meta/extras');
  extras(raw.missions, MISSIONS.map(x => x.id), '/meta/missions');
  for (const key of Object.keys(meta.extras) as Array<keyof typeof meta.extras>) meta.extras[key] = integer(raw.extras[key], EXTRA_PRICES.length, `/meta/extras/${key}`);
  for (const mission of MISSIONS) {
    const path = `/meta/missions/${mission.id}`;
    meta.missions[mission.id] = integer(raw.missions[mission.id], mission.target, path);
    const done = meta.completed.includes(mission.id);
    if (done !== (meta.missions[mission.id] === mission.target)) fail(path);
    const unlock = mission.unlock;
    if (done && unlock && !(unlock.kind === 'weapon' ? meta.weapons.includes(unlock.id) : unlock.kind === 'item' ? meta.items.includes(unlock.id) : meta.characters.includes(unlock.id))) fail(path);
  }
  for (const key of ['selected', 'lastRun'] as const) issues.push({ path: `/meta/${key}`, reason: 'preserve', after: excerpt(meta[key]) });
  // Las reglas web solo reciben un candidato ya comprobado. Su saneamiento no decide recuperaciones.
  const candidate = { version: 3, settings, meta };
  const parsed = parseSave(JSON.stringify(candidate), language);
  if (parsed.status !== 'ok' || canonicalProgressJson(parsed.data) !== canonicalProgressJson(candidate)) fail('');
  if (version !== 3) issues.push({ path: '/version', reason: 'migrate', before: String(version), after: '3' });
  return candidate;
}

/** read accede al estado ya cargado; nunca debe construir SaveManager ni leer localStorage. */
export function prepareProgressExport(read: () => unknown, language: Language): ExportResult {
  const issues: ExportIssue[] = [];
  let original: unknown;
  try { original = read(); } catch { return { json: null, issues: [{ path: '', reason: 'read' }] }; }
  try {
    const input = snapshot(original);
    const data = normalize(input, language, issues);
    const sourceVersion = (input as ObjectData).version;
    const transfer = { format: 'mamporro.progress', version: 1, source: { platform: 'web', saveVersion: sourceVersion }, progress: { settings: data.settings, meta: data.meta } };
    const json = canonicalProgressJson(transfer);
    const roundtrip = JSON.parse(json) as typeof transfer;
    const checked = normalize({ version: 3, ...roundtrip.progress }, language, []);
    if (canonicalProgressJson(checked) !== canonicalProgressJson(data) || roundtrip.format !== 'mamporro.progress' || roundtrip.version !== 1) fail('');
    return { json, issues };
  } catch (error) {
    const problem = error instanceof Invalid ? error : new Invalid('', 'invalid');
    return { json: null, issues: [...issues, { path: problem.path, reason: problem.reason }] };
  }
}
