// Convierte los glifos pixelados en una fuente OpenType real en tiempo de
// ejecución (con opentype.js) y la registra con la API FontFace. Así toda la UI
// HTML/CSS usa la fuente pixelada sin ningún archivo externo.
import type * as OpenType from 'opentype.js';
import { ACCENTS, COMPOSED, GLYPHS } from './glyphs';

export const FONT_FAMILY = 'MamporroPixel';
/** Unidades de fuente por píxel del glifo. */
const PX = 100;
/** Píxeles por encima de la línea base (7 de mayúscula + 2 de acento). */
const ASCENT = 9;
const DESCENT = 2;
/** Un "em" son 11 píxeles de glifo: con font-size = 11·n cada píxel mide n. */
export const FONT_PIXELS_PER_EM = ASCENT + DESCENT;

export interface GlyphBitmap {
  width: number;
  /** Fila (0 = parte superior de las mayúsculas) de la primera fila del mapa. */
  top: number;
  rows: string[];
}

export type Loop = Array<[number, number]>;

function parseRows(definition: string): string[] {
  return definition.split(' ');
}

/** Mapas de bits de todos los glifos, incluidas las letras acentuadas compuestas. */
export function buildBitmaps(): Map<string, GlyphBitmap> {
  const bitmaps = new Map<string, GlyphBitmap>();
  for (const [char, definition] of Object.entries(GLYPHS)) {
    const rows = parseRows(definition);
    const width = (rows[0] ?? '').length;
    bitmaps.set(char, { width, top: 0, rows });
  }
  for (const [char, [baseChar, accentName]] of Object.entries(COMPOSED)) {
    const base = bitmaps.get(baseChar);
    if (!base) throw new Error(`Glifo base inexistente: ${baseChar}`);
    const accent = parseRows(ACCENTS[accentName]);
    const isUpper = baseChar !== baseChar.toLowerCase();
    if (isUpper) {
      bitmaps.set(char, { width: base.width, top: -2, rows: [...accent, ...base.rows] });
    } else {
      // En minúsculas el acento ocupa las dos filas vacías sobre la altura de la x.
      const rows = base.rows.slice();
      rows[0] = accent[0] ?? '';
      rows[1] = accent[1] ?? '';
      bitmaps.set(char, { width: base.width, top: 0, rows });
    }
  }
  return bitmaps;
}

/**
 * Contornos del glifo en píxeles (x a la derecha, y hacia arriba desde la línea
 * base). Se unen los bordes exteriores de los píxeles en bucles cerrados en
 * sentido antihorario; los huecos quedan en sentido horario automáticamente.
 */
export function traceGlyph(bitmap: GlyphBitmap): Loop[] {
  const height = bitmap.rows.length;
  const on = (c: number, k: number): boolean =>
    k >= 0 && k < height && c >= 0 && c < bitmap.width && bitmap.rows[k]?.[c] === '#';

  const edges: Array<[number, number, number, number]> = [];
  for (let k = 0; k < height; k++) {
    for (let c = 0; c < bitmap.width; c++) {
      if (!on(c, k)) continue;
      const row = bitmap.top + k;
      const x0 = c;
      const x1 = c + 1;
      const y0 = 6 - row;
      const y1 = 7 - row;
      if (!on(c, k + 1)) edges.push([x0, y0, x1, y0]);
      if (!on(c + 1, k)) edges.push([x1, y0, x1, y1]);
      if (!on(c, k - 1)) edges.push([x1, y1, x0, y1]);
      if (!on(c - 1, k)) edges.push([x0, y1, x0, y0]);
    }
  }

  const key = (x: number, y: number): string => `${x},${y}`;
  const byStart = new Map<string, number[]>();
  edges.forEach((e, i) => {
    const k = key(e[0], e[1]);
    const list = byStart.get(k);
    if (list) list.push(i);
    else byStart.set(k, [i]);
  });

  // La regla de giro empareja cada borde de llegada con uno de salida, así que
  // seguirla desde cualquier borde siempre vuelve a ese mismo borde.
  const used = new Array<boolean>(edges.length).fill(false);
  const loops: Loop[] = [];
  for (let start = 0; start < edges.length; start++) {
    if (used[start]) continue;
    const loop: Loop = [];
    let index: number | undefined = start;
    let guard = edges.length;
    do {
      used[index] = true;
      const e: readonly [number, number, number, number] = edges[index] as [number, number, number, number];
      loop.push([e[0], e[1]]);
      index = nextEdge(edges, byStart.get(key(e[2], e[3])), e);
    } while (index !== undefined && index !== start && --guard > 0);
    loops.push(simplifyLoop(loop));
  }
  return loops;
}

/**
 * Elige el siguiente borde del contorno. Donde dos píxeles se tocan solo por
 * una esquina hay dos opciones: giramos a la derecha para unirlos en el mismo
 * contorno (así una "O" da exactamente un contorno exterior y un hueco).
 */
function nextEdge(
  edges: ReadonlyArray<readonly [number, number, number, number]>,
  candidates: readonly number[] | undefined,
  incoming: readonly [number, number, number, number],
): number | undefined {
  if (!candidates) return undefined;
  const inX = incoming[2] - incoming[0];
  const inY = incoming[3] - incoming[1];
  let best: number | undefined;
  let bestCross = Number.POSITIVE_INFINITY;
  for (const j of candidates) {
    const e = edges[j] as readonly [number, number, number, number];
    // Producto vectorial < 0 = giro a la derecha.
    const cross = inX * (e[3] - e[1]) - inY * (e[2] - e[0]);
    if (cross < bestCross) {
      bestCross = cross;
      best = j;
    }
  }
  return best;
}

/** Elimina puntos intermedios en tramos rectos. */
function simplifyLoop(loop: Loop): Loop {
  if (loop.length < 3) return loop;
  const result: Loop = [];
  const n = loop.length;
  for (let i = 0; i < n; i++) {
    const prev = loop[(i - 1 + n) % n] as [number, number];
    const cur = loop[i] as [number, number];
    const next = loop[(i + 1) % n] as [number, number];
    const cross = (cur[0] - prev[0]) * (next[1] - cur[1]) - (cur[1] - prev[1]) * (next[0] - cur[0]);
    if (cross !== 0) result.push(cur);
  }
  return result;
}

/** Área con signo (positiva en sentido antihorario). */
export function loopArea(loop: Loop): number {
  let area = 0;
  for (let i = 0; i < loop.length; i++) {
    const a = loop[i] as [number, number];
    const b = loop[(i + 1) % loop.length] as [number, number];
    area += a[0] * b[1] - b[0] * a[1];
  }
  return area / 2;
}

function glyphName(code: number): string {
  return `uni${code.toString(16).toUpperCase().padStart(4, '0')}`;
}

/** Construye el binario OpenType de la fuente. */
export function buildPixelFont(ot: typeof OpenType): ArrayBuffer {
  const glyphs: OpenType.Glyph[] = [
    new ot.Glyph({ name: '.notdef', advanceWidth: 4 * PX, path: new ot.Path() }),
  ];
  for (const [char, bitmap] of buildBitmaps()) {
    const path = new ot.Path();
    for (const loop of traceGlyph(bitmap)) {
      loop.forEach(([x, y], i) => {
        if (i === 0) path.moveTo(x * PX, y * PX);
        else path.lineTo(x * PX, y * PX);
      });
      path.close();
    }
    const code = char.codePointAt(0) as number;
    glyphs.push(
      new ot.Glyph({ name: glyphName(code), unicode: code, advanceWidth: (bitmap.width + 1) * PX, path }),
    );
  }
  const font = new ot.Font({
    familyName: FONT_FAMILY,
    styleName: 'Regular',
    unitsPerEm: FONT_PIXELS_PER_EM * PX,
    ascender: ASCENT * PX,
    descender: -DESCENT * PX,
    glyphs,
  });
  return font.toArrayBuffer();
}

/** Genera y registra la fuente. Si falla, la UI usa la monoespaciada del sistema. */
export async function loadPixelFont(): Promise<boolean> {
  if (typeof FontFace === 'undefined') return false;
  try {
    const ot = await import('opentype.js');
    const face = new FontFace(FONT_FAMILY, buildPixelFont(ot));
    await face.load();
    document.fonts.add(face);
    return true;
  } catch (err) {
    console.warn('[MAMPORRO] No se pudo generar la fuente pixelada:', err);
    return false;
  }
}
