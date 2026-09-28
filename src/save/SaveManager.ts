// Lectura y escritura del guardado. Todo acceso a localStorage va protegido:
// en modo privado o con el almacenamiento bloqueado, el juego sigue funcionando
// (simplemente no recuerda nada entre sesiones).
import type { Language } from '../i18n';
import { parseSave, type LoadStatus, type SaveData, type Settings } from './schema';

export const SAVE_KEY = 'mamporro.save';
export const BACKUP_KEY = 'mamporro.save.backup';

export interface StorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/** Devuelve localStorage si se puede usar, o `null`. */
export function browserStorage(): StorageLike | null {
  try {
    const storage = window.localStorage;
    const probe = '__mamporro_probe__';
    storage.setItem(probe, '1');
    storage.removeItem(probe);
    return storage;
  } catch {
    return null;
  }
}

export class SaveManager {
  data: SaveData;
  canSave: boolean;
  readonly loadStatus: LoadStatus;

  constructor(
    private readonly storage: StorageLike | null,
    defaultLanguage: Language,
  ) {
    this.canSave = storage !== null;
    let raw: string | null = null;
    try {
      raw = storage?.getItem(SAVE_KEY) ?? null;
    } catch {
      raw = null;
    }
    const result = parseSave(raw, defaultLanguage);
    this.data = result.data;
    this.loadStatus = result.status;

    if (result.status === 'reset' && raw !== null) {
      // Guardamos una copia del contenido ilegible por si hiciera falta recuperarlo.
      this.write(BACKUP_KEY, raw);
    }
    if (result.status !== 'ok') this.persist();
  }

  get settings(): Settings {
    return this.data.settings;
  }

  updateSettings(patch: Partial<Settings>): Settings {
    this.data = { ...this.data, settings: { ...this.data.settings, ...patch } };
    this.persist();
    return this.data.settings;
  }

  persist(): void {
    this.write(SAVE_KEY, JSON.stringify(this.data));
  }

  private write(key: string, value: string): void {
    try {
      this.storage?.setItem(key, value);
      this.canSave = this.storage !== null;
    } catch {
      this.canSave = false;
      // Almacenamiento lleno o bloqueado: seguimos sin guardar.
    }
  }
}
