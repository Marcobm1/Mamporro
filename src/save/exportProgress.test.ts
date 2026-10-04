import { afterEach, describe, expect, it, vi } from 'vitest';
import corpus from '../../unity/Docs/Reference/u4-progress.json';
import { canonicalProgressJson, prepareProgressExport } from './exportProgress';
import { defaultSave } from './schema';
import { downloadProgress } from '../ui/ProgressExport';
import type { Language } from '../i18n';

describe('exportación pura U4', () => {
  for (const fixture of corpus.saves) it(`conserva íntegramente el corpus web: ${fixture.id}`, () => {
    const input = fixture.raw === null ? fixture.result.data : JSON.parse(fixture.raw);
    const before = structuredClone(input);
    const result = prepareProgressExport(() => input, fixture.language as Language);
    expect(result.json, JSON.stringify(result.issues)).not.toBeNull();
    const actual = JSON.parse(result.json!);
    const expected = fixture.result.data;
    expect(actual).toEqual({ format: 'mamporro.progress', version: 1,
      source: { platform: 'web', saveVersion: input.version }, progress: { settings: expected.settings, meta: expected.meta } });
    expect(input).toEqual(before);
    expect(prepareProgressExport(() => input, fixture.language as Language).json).toBe(result.json);
  });

  it('no toca almacenamiento, recibo, liquidación, partida ni el estado leído', () => {
    const state = { save: defaultSave('es'), run: { hp: 50, xp: 81, gold: 92, seed: 'TEST', time: 42 },
      receipt: { total: 14 }, runSettled: false, saved: JSON.stringify(defaultSave('es')) };
    const before = structuredClone(state);
    const blocked = vi.fn(() => { throw new Error('almacenamiento no disponible'); });
    vi.stubGlobal('localStorage', { getItem: blocked, setItem: blocked, removeItem: blocked });
    const result = prepareProgressExport(() => state.save, 'es');
    expect(result.json).not.toBeNull(); expect(blocked).not.toHaveBeenCalled(); expect(state).toEqual(before);
    expect(Object.keys(JSON.parse(result.json!))).toEqual(['format', 'progress', 'source', 'version']);
    expect(Object.keys(JSON.parse(result.json!).progress)).toEqual(['meta', 'settings']);
  });

  it('rechaza fallo de lectura sin crear progreso ni llamar otra vez', () => {
    const read = vi.fn(() => { throw new Error('lectura fallida'); });
    expect(prepareProgressExport(read, 'es')).toEqual({ json: null, issues: [{ path: '', reason: 'read' }] });
    expect(read).toHaveBeenCalledTimes(1);
  });

  for (const [name, change] of Object.entries<(x: ReturnType<typeof defaultSave>) => void>({
    coinsNegative: x => { x.meta.coins = -1; }, coinsFraction: x => { x.meta.coins = 1.2; },
    coinsHigh: x => { x.meta.coins = 1_000_000_001; }, coinsNaN: x => { x.meta.coins = NaN; },
    extra: x => { x.meta.extras.rerolls = 4; }, missingMission: x => { Reflect.deleteProperty(x.meta.missions, 'kills'); },
    contradictoryMission: x => { x.meta.missions.kills = 1000; }, missingReward: x => { x.meta.missions.kills = 1000; x.meta.completed.push('kills'); },
    missingInitial: x => { x.meta.weapons.shift(); }, selectedLocked: x => { x.meta.selected = 'baguette'; },
    longLastRun: x => { x.meta.lastRun = 'a'.repeat(101); }, future: x => { x.version = 99; },
    missingVersion: x => { Reflect.deleteProperty(x, 'version'); }, contradictoryV1: x => { x.version = 1; },
  })) it(`rechaza datos ambiguos: ${name}`, () => {
    const input = defaultSave('es'); change(input); const before = structuredClone(input);
    expect(prepareProgressExport(() => input, 'es').json).toBeNull(); expect(input).toEqual(before);
  });

  it('informa defaults, exclusiones y duplicados sin mutar el original', () => {
    const input = { ...defaultSave('es'), activeRun: { seed: 'NO-EXPORTAR' } };
    input.settings.mouseSensitivity = 99; input.meta.weapons.push('chancla');
    (input.meta.items as string[]).push('unknown');
    const before = structuredClone(input), result = prepareProgressExport(() => input, 'es');
    expect(result.json).not.toBeNull();
    expect(JSON.parse(result.json!).progress).toEqual({ settings: defaultSave('es').settings, meta: defaultSave('es').meta });
    expect(result.issues.filter(x => x.reason === 'exclude')).toHaveLength(3);
    expect(result.issues).toContainEqual(expect.objectContaining({ path: '/settings/mouseSensitivity', reason: 'default' }));
    expect(input).toEqual(before); expect(result.json).not.toContain('NO-EXPORTAR');
  });

  it('recupera un contenedor de opciones roto sin perder progreso', () => {
    const input = { ...defaultSave('es'), settings: null }; input.meta.coins = 321;
    const result = prepareProgressExport(() => input, 'en');
    expect(JSON.parse(result.json!).progress).toEqual({ settings: defaultSave('en').settings, meta: input.meta });
  });

  it('rechaza ciclos, colecciones enormes, getters y cadenas inválidas sin ejecutarlos', () => {
    const getter = vi.fn(() => 1); const accessor = Object.defineProperty({}, 'version', { enumerable: true, get: getter });
    const cyclic: Record<string, unknown> = {}; cyclic.self = cyclic;
    for (const value of [accessor, cyclic, Array(129).fill(1), { value: '\ud800' }, { value: 'a'.repeat(1025) }])
      expect(prepareProgressExport(() => value, 'es').json).toBeNull();
    expect(getter).not.toHaveBeenCalled();
  });

  it('serializa de forma estable sin depender del orden de las propiedades', () => {
    expect(canonicalProgressJson({ b: 2, a: { y: 3, x: 4 } })).toBe(canonicalProgressJson({ a: { x: 4, y: 3 }, b: 2 }));
  });
});

describe('preparación de descarga', () => {
  it('informa fallo de Blob/URL sin acceder al progreso', () => {
    vi.stubGlobal('URL', { createObjectURL: () => { throw new Error('bloqueado'); } });
    expect(() => downloadProgress('{}')).toThrow('bloqueado');
  });
  it('limpia enlace y URL si falla el click', () => {
    vi.useFakeTimers(); const revoke = vi.fn(), remove = vi.fn();
    vi.stubGlobal('URL', { createObjectURL: () => 'blob:test', revokeObjectURL: revoke });
    vi.stubGlobal('document', { body: { append: vi.fn() }, createElement: () => ({ remove, click: () => { throw new Error('click'); } }) });
    expect(() => downloadProgress('{}')).toThrow('click'); expect(remove).toHaveBeenCalledOnce();
    vi.runAllTimers(); expect(revoke).toHaveBeenCalledWith('blob:test');
  });
});
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers(); });
