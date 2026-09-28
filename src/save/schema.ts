// Esquema del guardado en localStorage, con versión y migraciones.
// Regla: los datos leídos nunca se usan "a pelo"; siempre se validan y se
// completan con valores por defecto, así un guardado corrupto no rompe el juego.
import { defaultMeta, sanitizeMeta, type MetaProgress } from '../systems/meta';
import { RUN_DURATIONS, type RunMinutes } from '../data/waves';
import { isLanguage, type Language } from '../i18n';

export const SAVE_VERSION = 3;

export const RENDER_HEIGHTS = [240, 360, 480] as const;
export type RenderHeight = (typeof RENDER_HEIGHTS)[number];

export const SENSITIVITY_MIN = 0.2;
export const SENSITIVITY_MAX = 3;

export interface Settings {
  musicVolume: number;
  effectsVolume: number;
  muted: boolean;
  reducedParticles: boolean;
  cameraShake: boolean;
  flashes: boolean;
  language: Language;
  /** Multiplicador de la sensibilidad base del ratón. */
  mouseSensitivity: number;
  /** Altura de la imagen interna antes de escalarla a pantalla. */
  renderHeight: RenderHeight;
  vertexSnap: boolean;
  dithering: boolean;
  /** Ctrl como tecla de deslizarse (desactivado por defecto por Ctrl+W). */
  slideWithCtrl: boolean;
  showFps: boolean;
  /** Duración de la partida (minutos). */
  runMinutes: RunMinutes;
}

export interface SaveData {
  version: number;
  settings: Settings;
  meta: MetaProgress;
}

export function defaultSettings(language: Language): Settings {
  return {
    musicVolume: 0.5,
    effectsVolume: 0.7,
    muted: false,
    reducedParticles: false,
    cameraShake: true,
    flashes: true,
    language,
    mouseSensitivity: 1,
    renderHeight: 360,
    vertexSnap: true,
    dithering: true,
    slideWithCtrl: false,
    showFps: false,
    runMinutes: 10,
  };
}

export function defaultSave(language: Language): SaveData {
  return { version: SAVE_VERSION, settings: defaultSettings(language), meta: defaultMeta() };
}

export type Json = Record<string, unknown>;

function isObject(value: unknown): value is Json {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readBool(obj: Json, key: string, fallback: boolean): boolean {
  const v = obj[key];
  return typeof v === 'boolean' ? v : fallback;
}

function readNumber(obj: Json, key: string, fallback: number, min: number, max: number): number {
  const v = obj[key];
  if (typeof v !== 'number' || !Number.isFinite(v)) return fallback;
  return Math.min(max, Math.max(min, v));
}

function readRenderHeight(obj: Json, key: string, fallback: RenderHeight): RenderHeight {
  const v = obj[key];
  return (RENDER_HEIGHTS as readonly unknown[]).includes(v) ? (v as RenderHeight) : fallback;
}

function readRunMinutes(obj: Json, key: string, fallback: RunMinutes): RunMinutes {
  const v = obj[key];
  return (RUN_DURATIONS as readonly unknown[]).includes(v) ? (v as RunMinutes) : fallback;
}

export function sanitizeSettings(raw: unknown, language: Language): Settings {
  const d = defaultSettings(language);
  if (!isObject(raw)) return d;
  return {
    musicVolume: readNumber(raw, 'musicVolume', d.musicVolume, 0, 1),
    effectsVolume: readNumber(raw, 'effectsVolume', d.effectsVolume, 0, 1),
    muted: readBool(raw, 'muted', d.muted),
    reducedParticles: readBool(raw, 'reducedParticles', d.reducedParticles),
    cameraShake: readBool(raw, 'cameraShake', d.cameraShake),
    flashes: readBool(raw, 'flashes', d.flashes),
    language: isLanguage(raw.language) ? raw.language : d.language,
    mouseSensitivity: readNumber(raw, 'mouseSensitivity', d.mouseSensitivity, SENSITIVITY_MIN, SENSITIVITY_MAX),
    renderHeight: readRenderHeight(raw, 'renderHeight', d.renderHeight),
    vertexSnap: readBool(raw, 'vertexSnap', d.vertexSnap),
    dithering: readBool(raw, 'dithering', d.dithering),
    slideWithCtrl: readBool(raw, 'slideWithCtrl', d.slideWithCtrl),
    showFps: readBool(raw, 'showFps', d.showFps),
    runMinutes: readRunMinutes(raw, 'runMinutes', d.runMinutes),
  };
}

/**
 * Migraciones: la función en la clave `n` convierte datos de la versión `n`
 * a la `n + 1`. Al cambiar el esquema se sube SAVE_VERSION y se añade aquí.
 */
export const MIGRATIONS: Readonly<Record<number, (data: Json) => Json>> = {
  1: (data) => ({ ...data, meta: defaultMeta() }),
  // Los ajustes nuevos se completan en sanitizeSettings; la meta v2 se conserva.
  2: (data) => ({ ...data }),
};

export type LoadStatus = 'new' | 'ok' | 'migrated' | 'reset';

export interface LoadResult {
  data: SaveData;
  status: LoadStatus;
}

export interface SchemaDefinition {
  version: number;
  migrations: Readonly<Record<number, (data: Json) => Json>>;
}

const CURRENT_SCHEMA: SchemaDefinition = { version: SAVE_VERSION, migrations: MIGRATIONS };

/**
 * Convierte el contenido crudo del almacenamiento en un guardado válido.
 * `schema` solo se cambia en los tests, para probar migraciones ficticias.
 */
export function parseSave(
  raw: string | null,
  language: Language,
  schema: SchemaDefinition = CURRENT_SCHEMA,
): LoadResult {
  if (raw === null) return { data: defaultSave(language), status: 'new' };

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return { data: defaultSave(language), status: 'reset' };
  }
  if (!isObject(parsed) || typeof parsed.version !== 'number' || !Number.isInteger(parsed.version)) {
    return { data: defaultSave(language), status: 'reset' };
  }

  let version = parsed.version;
  // Un guardado de una versión futura (o absurda) no se puede interpretar: se reinicia.
  if (version > schema.version || version < 1) {
    return { data: defaultSave(language), status: 'reset' };
  }

  let data: Json = parsed;
  let migrated = false;
  while (version < schema.version) {
    const migrate = schema.migrations[version];
    if (!migrate) return { data: defaultSave(language), status: 'reset' };
    data = migrate(data);
    version++;
    migrated = true;
  }

  return {
    data: { version: SAVE_VERSION, settings: sanitizeSettings(data.settings, language), meta: sanitizeMeta(data.meta) },
    status: migrated ? 'migrated' : 'ok',
  };
}
