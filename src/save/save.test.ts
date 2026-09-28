import { defaultMeta } from '../systems/meta';
import { describe, expect, it } from 'vitest';
import { BACKUP_KEY, SAVE_KEY, SaveManager, type StorageLike } from './SaveManager';
import { defaultSettings, parseSave, SAVE_VERSION, type Json } from './schema';

class MemoryStorage implements StorageLike {
  readonly data = new Map<string, string>();
  getItem(key: string): string | null {
    return this.data.get(key) ?? null;
  }
  setItem(key: string, value: string): void {
    this.data.set(key, value);
  }
  removeItem(key: string): void {
    this.data.delete(key);
  }
}

class BrokenStorage implements StorageLike {
  getItem(): string | null {
    throw new Error('bloqueado');
  }
  setItem(): void {
    throw new Error('bloqueado');
  }
  removeItem(): void {
    throw new Error('bloqueado');
  }
}

describe('parseSave', () => {
  it('sin datos crea un guardado nuevo con los valores por defecto', () => {
    const result = parseSave(null, 'es');
    expect(result.status).toBe('new');
    expect(result.data).toEqual({ version: SAVE_VERSION, settings: defaultSettings('es'), meta: defaultMeta() });
  });

  it('JSON corrupto o sin versión se reinicia', () => {
    expect(parseSave('{no es json', 'en').status).toBe('reset');
    expect(parseSave('[1,2,3]', 'en').status).toBe('reset');
    expect(parseSave('{"settings":{}}', 'en').status).toBe('reset');
    expect(parseSave('{"version":"1"}', 'en').status).toBe('reset');
  });

  it('un guardado de una versión futura se reinicia', () => {
    const raw = JSON.stringify({ version: SAVE_VERSION + 1, settings: {} });
    expect(parseSave(raw, 'es').status).toBe('reset');
  });

  it('valida y corrige cada ajuste por separado', () => {
    const raw = JSON.stringify({
      version: SAVE_VERSION,
      settings: {
        language: 'fr',
        mouseSensitivity: 99,
        renderHeight: 1000,
        vertexSnap: false,
        dithering: 'sí',
        slideWithCtrl: true,
        showFps: true,
        runMinutes: 7,
      },
    });
    const { data, status } = parseSave(raw, 'en');
    expect(status).toBe('ok');
    expect(data.settings).toEqual({
      musicVolume: 0.5,
      effectsVolume: 0.7,
      muted: false,
      reducedParticles: false,
      cameraShake: true,
      flashes: true,
      language: 'en',
      mouseSensitivity: 3,
      renderHeight: 360,
      vertexSnap: false,
      dithering: true,
      slideWithCtrl: true,
      showFps: true,
      runMinutes: 10,
    });
    // Una duración válida se conserva.
    const five = parseSave(JSON.stringify({ version: 1, settings: { runMinutes: 5 } }), 'es');
    expect(five.data.settings.runMinutes).toBe(5);
  });

  it('aplica las migraciones en orden hasta la versión actual', () => {
    const schema = {
      version: 3,
      migrations: {
        1: (d: Json): Json => ({ ...d, settings: { sensibilidad: 2 } }),
        2: (d: Json): Json => {
          const old = d.settings as { sensibilidad: number };
          return { ...d, settings: { mouseSensitivity: old.sensibilidad } };
        },
      },
    };
    const result = parseSave(JSON.stringify({ version: 1 }), 'es', schema);
    expect(result.status).toBe('migrated');
    expect(result.data.settings.mouseSensitivity).toBe(2);
  });

  it('si falta una migración, se reinicia en vez de romperse', () => {
    const schema = { version: 3, migrations: { 1: (d: Json): Json => d } };
    expect(parseSave(JSON.stringify({ version: 1 }), 'es', schema).status).toBe('reset');
  });
});

describe('SaveManager', () => {
  it('guarda los cambios de ajustes y los recupera al recargar', () => {
    const storage = new MemoryStorage();
    const first = new SaveManager(storage, 'es');
    first.updateSettings({ showFps: true, renderHeight: 480 });
    const second = new SaveManager(storage, 'es');
    expect(second.loadStatus).toBe('ok');
    expect(second.settings.showFps).toBe(true);
    expect(second.settings.renderHeight).toBe(480);
  });

  it('hace copia de seguridad de un guardado ilegible antes de reiniciarlo', () => {
    const storage = new MemoryStorage();
    storage.setItem(SAVE_KEY, '###basura###');
    const manager = new SaveManager(storage, 'en');
    expect(manager.loadStatus).toBe('reset');
    expect(storage.getItem(BACKUP_KEY)).toBe('###basura###');
    expect(JSON.parse(storage.getItem(SAVE_KEY) ?? '{}').version).toBe(SAVE_VERSION);
  });

  it('funciona sin almacenamiento o con uno que lanza errores', () => {
    expect(() => new SaveManager(null, 'es').updateSettings({ showFps: true })).not.toThrow();
    const manager = new SaveManager(new BrokenStorage(), 'es');
    expect(manager.loadStatus).toBe('new');
    expect(() => manager.updateSettings({ dithering: false })).not.toThrow();
    expect(manager.settings.dithering).toBe(false);
  });
});


describe('guardado v3 de pulido', () => {
  it('migra v2 sin perder monedas, compras ni opciones anteriores', () => {
    const meta = defaultMeta();
    meta.coins = 321;
    meta.characters.push('baguette');
    meta.selected = 'baguette';
    const result = parseSave(JSON.stringify({ version: 2, meta, settings: { muted: true, effectsVolume: 0.3, language: 'en' } }), 'es');
    expect(result.status).toBe('migrated');
    expect(result.data.version).toBe(3);
    expect(result.data.meta).toEqual(meta);
    expect(result.data.settings).toMatchObject({ muted: true, effectsVolume: 0.3, language: 'en', reducedParticles: false, flashes: true, cameraShake: true });
  });
  it('persiste las tres opciones y descarta valores no booleanos', () => {
    const storage = new MemoryStorage();
    const save = new SaveManager(storage, 'es');
    save.updateSettings({ reducedParticles: true, cameraShake: false, flashes: false });
    expect(new SaveManager(storage, 'es').settings).toMatchObject({ reducedParticles: true, cameraShake: false, flashes: false });
    const result = parseSave(JSON.stringify({ version: 3, settings: { reducedParticles: 1, cameraShake: null, flashes: 'no' } }), 'es');
    expect(result.data.settings).toMatchObject({ reducedParticles: false, cameraShake: true, flashes: true });
  });
});
