import { describe, expect, it } from 'vitest';
import { defaultMeta, purchase, purchaseExtra, sanitizeMeta, settleRun, type MetaRun } from './meta';
import { parseSave } from '../save/schema';
import { SaveManager, SAVE_KEY } from '../save/SaveManager';
const run = (patch: Partial<MetaRun> = {}): MetaRun => ({ id: 'one', cheated: false, time: 300, kills: 400, chests: 3, shrines: 1, challenges: 0, level: 15, victory: false, usedLifeTome: false, ...patch });
describe('progreso meta', () => {
  it('migra el guardado real v1 conservando opciones y crea el catálogo inicial', () => {
    const result = parseSave(JSON.stringify({ version: 1, settings: { language: 'en', mouseSensitivity: 2, runMinutes: 5, vertexSnap: false } }), 'es');
    expect(result.status).toBe('migrated');
    expect(result.data.settings).toMatchObject({ language: 'en', mouseSensitivity: 2, runMinutes: 5, vertexSnap: false });
    expect(result.data.meta.weapons).toHaveLength(4); expect(result.data.meta.items).toHaveLength(8);
    expect(result.data.meta.characters).toEqual(['remedios']);
  });
  it('valida IDs, límites, duplicados, selección bloqueada y datos no finitos', () => {
    const m = sanitizeMeta({ coins: NaN, selected: 'baguette', weapons: ['fake','jersey','jersey'], extras: { rerolls: 99, skips: -1 }, missions: { kills: Infinity }, completed: ['fake'] });
    expect(m.coins).toBe(0); expect(m.selected).toBe('remedios'); expect(m.weapons).toHaveLength(5);
    expect(m.extras).toEqual({ rerolls: 3, skips: 0, banishes: 0 }); expect(m.missions.kills).toBe(0);
  });
  it('no permite compras sin fondos, repetidas ni superar tres ampliaciones', () => {
    const m = defaultMeta(); expect(purchase(m, 'baguette')).toBe(false);
    m.coins = 1000; expect(purchase(m, 'baguette')).toBe(true); expect(m.coins).toBe(780);
    expect(purchase(m, 'baguette')).toBe(false); expect(purchase(m, 'fake')).toBe(false);
    for (let i = 0; i < 3; i++) expect(purchaseExtra(m, 'rerolls')).toBe(true);
    expect(purchaseExtra(m, 'rerolls')).toBe(false); expect(m.coins).toBe(340);
  });
  it('acumula objetivos, concede contenido una vez y conserva liquidación al recargar', () => {
    const m = defaultMeta(); const first = settleRun(m, run());
    expect(first.total).toBe(70); expect(first.completed).toEqual(['first']);
    expect(settleRun(m, run()).total).toBe(0);
    const next = sanitizeMeta(JSON.parse(JSON.stringify(m)));
    expect(settleRun(next, run()).total).toBe(0);
    const receipt = settleRun(next, run({ id: 'two', kills: 600, chests: 7, shrines: 2, challenges: 1, level: 20, victory: true }));
    expect(receipt.completed).toHaveLength(7); expect(next.weapons).toContain('fregona');
    expect(next.items).toContain('perlas'); expect(next.items).toContain('bata');
    expect(settleRun(next, run({ id: 'three', victory: true })).missions).toBe(0);
  });
  it('excluye absolutamente las partidas con trucos y las victorias con tomo de vida', () => {
    const m = defaultMeta(); expect(settleRun(m, run({ cheated: true, victory: true })).total).toBe(0);
    expect(m).toEqual(defaultMeta());
    settleRun(m, run({ victory: true, usedLifeTome: true })); expect(m.completed).not.toContain('noLife');
  });
  it('guarda compras y progreso junto a opciones sin perderlos al cambiar ajustes', () => {
    const map = new Map<string,string>();
    const storage = { getItem: (key: string) => map.get(key) ?? null, setItem: (key: string, val: string) => { map.set(key,val); }, removeItem: (key: string) => { map.delete(key); } };
    const a = new SaveManager(storage,'es'); settleRun(a.data.meta,run()); a.updateSettings({ muted: true });
    expect(map.has(SAVE_KEY)).toBe(true);
    const b = new SaveManager(storage,'en'); expect(b.data.meta.coins).toBe(70); expect(b.settings.muted).toBe(true);
  });
});
