import { describe, expect, it } from 'vitest';
import { en } from './en';
import { es } from './es';
import { detectLanguage, format, getLanguage, setLanguage, t, tIn } from './index';

const placeholders = (text: string): string[] => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1] ?? '').sort();

describe('i18n', () => {
  it('español e inglés tienen exactamente las mismas claves', () => {
    expect(Object.keys(en).sort()).toEqual(Object.keys(es).sort());
  });

  it('ningún texto está vacío y los parámetros coinciden entre idiomas', () => {
    for (const key of Object.keys(es) as Array<keyof typeof es>) {
      expect(es[key].trim().length).toBeGreaterThan(0);
      expect(en[key].trim().length).toBeGreaterThan(0);
      expect(placeholders(en[key])).toEqual(placeholders(es[key]));
    }
  });

  it('interpola parámetros y deja intactos los desconocidos', () => {
    expect(format('Hola {nombre}, nivel {n}', { nombre: 'Remedios', n: 3 })).toBe('Hola Remedios, nivel 3');
    expect(format('Falta {x}', {})).toBe('Falta {x}');
    expect(tIn('en', 'pause.seed', { seed: 'ABC' })).toBe('Seed: ABC');
  });

  it('cambia de idioma en caliente', () => {
    setLanguage('es');
    expect(t('pause.title')).toBe('Pausa');
    setLanguage('en');
    expect(getLanguage()).toBe('en');
    expect(t('pause.title')).toBe('Paused');
    setLanguage('es');
  });

  it('detecta el idioma del navegador', () => {
    expect(detectLanguage(['es-ES', 'en'])).toBe('es');
    expect(detectLanguage(['es-419'])).toBe('es');
    expect(detectLanguage(['en-GB', 'es'])).toBe('en');
    expect(detectLanguage(['fr-FR', 'es-MX'])).toBe('es');
    expect(detectLanguage(['de-DE'])).toBe('en');
    expect(detectLanguage([])).toBe('en');
  });
});
