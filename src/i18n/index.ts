// Sistema de traducción mínimo: `t('clave', { param })` con interpolación `{param}`.
import { en } from './en';
import { es, type TranslationKey } from './es';

export type { TranslationKey };
export type Language = 'es' | 'en';
export const LANGUAGES: readonly Language[] = ['es', 'en'];

export type TranslationParams = Readonly<Record<string, string | number>>;

const dictionaries: Readonly<Record<Language, Readonly<Record<TranslationKey, string>>>> = {
  es,
  en,
};

let current: Language = 'es';
const listeners = new Set<(lang: Language) => void>();

export function isLanguage(value: unknown): value is Language {
  return value === 'es' || value === 'en';
}

/** Idioma inicial según el navegador: español si empieza por "es", inglés en otro caso. */
export function detectLanguage(browserLanguages: readonly string[]): Language {
  for (const lang of browserLanguages) {
    const code = lang.toLowerCase();
    if (code.startsWith('es')) return 'es';
    if (code.startsWith('en')) return 'en';
  }
  return 'en';
}

export function getLanguage(): Language {
  return current;
}

export function setLanguage(lang: Language): void {
  if (lang === current) return;
  current = lang;
  if (typeof document !== 'undefined') document.documentElement.lang = lang;
  listeners.forEach((l) => l(lang));
}

export function onLanguageChange(listener: (lang: Language) => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function format(template: string, params?: TranslationParams): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? String(params[name]) : match,
  );
}

export function t(key: TranslationKey, params?: TranslationParams): string {
  return format(dictionaries[current][key], params);
}

/** Traducción en un idioma concreto (útil en tests). */
export function tIn(lang: Language, key: TranslationKey, params?: TranslationParams): string {
  return format(dictionaries[lang][key], params);
}

export function dictionaryKeys(lang: Language): string[] {
  return Object.keys(dictionaries[lang]);
}
