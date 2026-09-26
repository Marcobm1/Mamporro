import * as ot from 'opentype.js';
import { describe, expect, it } from 'vitest';
import { en } from '../../i18n/en';
import { es } from '../../i18n/es';
import { buildBitmaps, buildPixelFont, loopArea, traceGlyph } from './pixelFont';

const bitmaps = buildBitmaps();

describe('fuente pixelada', () => {
  it('todos los glifos son rectangulares y solo usan "#" y "."', () => {
    for (const [char, bitmap] of bitmaps) {
      expect(bitmap.width, `ancho de "${char}"`).toBeGreaterThan(0);
      for (const row of bitmap.rows) {
        expect(row, `fila de "${char}"`).toMatch(/^[#.]+$/);
        expect(row.length, `ancho de fila de "${char}"`).toBe(bitmap.width);
      }
      // Nunca por encima del acento ni por debajo del descendente.
      expect(bitmap.top).toBeGreaterThanOrEqual(-2);
      expect(bitmap.top + bitmap.rows.length).toBeLessThanOrEqual(9);
    }
  });

  it('los contornos trazados cubren exactamente los píxeles encendidos', () => {
    for (const [char, bitmap] of bitmaps) {
      const pixels = bitmap.rows.join('').split('').filter((c) => c === '#').length;
      const area = traceGlyph(bitmap).reduce((sum, loop) => sum + loopArea(loop), 0);
      expect(area, `área de "${char}"`).toBe(pixels);
    }
  });

  it('las letras con huecos generan contornos interiores en sentido contrario', () => {
    const loops = traceGlyph(bitmaps.get('O') as NonNullable<ReturnType<typeof bitmaps.get>>);
    expect(loops).toHaveLength(2);
    const areas = loops.map(loopArea).sort((a, b) => a - b);
    expect(areas[0]).toBeLessThan(0);
    expect(areas[1]).toBeGreaterThan(0);
  });

  it('cubre todos los caracteres usados en los textos de ES y EN', () => {
    const missing = new Set<string>();
    for (const text of [...Object.values(es), ...Object.values(en)]) {
      for (const char of text) if (!bitmaps.has(char)) missing.add(char);
    }
    expect([...missing]).toEqual([]);
  });

  it('genera un binario OpenType válido que se puede volver a leer', () => {
    const buffer = buildPixelFont(ot);
    expect(buffer.byteLength).toBeGreaterThan(1000);
    const font = ot.parse(buffer);
    expect(font.glyphs.length).toBe(bitmaps.size + 1);
  });
});
