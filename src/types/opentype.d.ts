// Declaraciones mínimas de opentype.js (el paquete no incluye tipos).
// Solo cubren lo que usamos para generar la fuente pixelada en tiempo de ejecución.
declare module 'opentype.js' {
  export class Path {
    constructor();
    moveTo(x: number, y: number): void;
    lineTo(x: number, y: number): void;
    close(): void;
  }

  export interface GlyphOptions {
    name: string;
    unicode?: number;
    advanceWidth: number;
    path: Path;
  }

  export class Glyph {
    constructor(options: GlyphOptions);
    name: string | null;
    unicode: number | undefined;
    advanceWidth: number;
  }

  export interface FontOptions {
    familyName: string;
    styleName: string;
    unitsPerEm: number;
    ascender: number;
    descender: number;
    glyphs: Glyph[];
  }

  export interface GlyphSet {
    length: number;
    get(index: number): Glyph;
  }

  export class Font {
    constructor(options: FontOptions);
    glyphs: GlyphSet;
    toArrayBuffer(): ArrayBuffer;
  }

  export function parse(buffer: ArrayBuffer): Font;
}
