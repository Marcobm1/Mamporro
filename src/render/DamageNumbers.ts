// Números de daño flotantes, dibujados con los glifos de la fuente pixelada del
// juego (con contorno negro). Cada dígito es una instancia de un quad que mira a
// la cámara; se dibujan en la imagen de baja resolución, así que salen pixelados.
import {
  CanvasTexture,
  Color,
  DynamicDrawUsage,
  InstancedBufferAttribute,
  InstancedBufferGeometry,
  Mesh,
  NearestFilter,
  PlaneGeometry,
  ShaderMaterial,
} from 'three';
import type { Rng } from '../core/rng';
import { GLYPHS } from '../ui/font/glyphs';
import { PALETTE } from './palette';
import { RENDER_ORDER } from './renderOrder';

const CHARS = '0123456789!';
const CELL_W = 7;
const CELL_H = 9;
const MAX_NUMBERS = 140;
const MAX_CHARS = 6;
const LIFE = 0.75;

const VERTEX = /* glsl */ `
attribute vec3 aOffset;
attribute vec2 aGlyph;
attribute vec4 aColor;
attribute float aScale;
uniform float uGlyphCount;
uniform vec2 uSize;
uniform float uAdvance;
varying vec2 vUv;
varying vec4 vColor;
void main() {
  vec4 mv = viewMatrix * vec4(aOffset, 1.0);
  mv.xy += vec2(position.x * uSize.x + aGlyph.y * uAdvance, position.y * uSize.y) * aScale;
  gl_Position = projectionMatrix * mv;
  vUv = vec2((aGlyph.x + uv.x) / uGlyphCount, uv.y);
  vColor = aColor;
}
`;

const FRAGMENT = /* glsl */ `
uniform sampler2D uAtlas;
varying vec2 vUv;
varying vec4 vColor;
void main() {
  vec4 texel = texture2D(uAtlas, vUv);
  if (texel.a < 0.5) discard;
  gl_FragColor = vec4(texel.rgb * vColor.rgb, vColor.a);
}
`;

/** Atlas con los dígitos en blanco y contorno negro de 1 píxel. */
function createAtlas(): CanvasTexture {
  const canvas = document.createElement('canvas');
  canvas.width = CHARS.length * CELL_W;
  canvas.height = CELL_H;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('Canvas 2D no disponible');
  [...CHARS].forEach((char, index) => {
    const rows = (GLYPHS[char] ?? '').split(' ');
    const width = rows[0]?.length ?? 0;
    const ox = index * CELL_W + 1 + Math.floor((5 - width) / 2);
    ctx.fillStyle = '#000';
    rows.forEach((row, y) =>
      [...row].forEach((c, x) => {
        if (c === '#') ctx.fillRect(ox + x - 1, 1 + y - 1, 3, 3);
      }),
    );
    ctx.fillStyle = '#fff';
    rows.forEach((row, y) =>
      [...row].forEach((c, x) => {
        if (c === '#') ctx.fillRect(ox + x, 1 + y, 1, 1);
      }),
    );
  });
  const texture = new CanvasTexture(canvas);
  texture.magFilter = NearestFilter;
  texture.minFilter = NearestFilter;
  texture.generateMipmaps = false;
  return texture;
}

interface FloatingNumber {
  x: number;
  y: number;
  z: number;
  drift: number;
  age: number;
  text: string;
  color: Color;
  scale: number;
}

export class DamageNumbers {
  readonly mesh: Mesh;
  private readonly numbers: FloatingNumber[] = [];
  private readonly offsets: InstancedBufferAttribute;
  private readonly glyphs: InstancedBufferAttribute;
  private readonly colors: InstancedBufferAttribute;
  private readonly scales: InstancedBufferAttribute;
  private readonly geometry: InstancedBufferGeometry;
  private readonly palette = {
    normal: new Color(PALETTE.numberNormal),
    crit: new Color(PALETTE.numberCrit),
    super: new Color(PALETTE.numberSuper),
    player: new Color(PALETTE.numberPlayer),
  };

  constructor(private readonly rng: Rng) {
    const capacity = MAX_NUMBERS * MAX_CHARS;
    const quad = new PlaneGeometry(1, 1);
    this.geometry = new InstancedBufferGeometry();
    this.geometry.index = quad.index;
    this.geometry.setAttribute('position', quad.getAttribute('position'));
    this.geometry.setAttribute('uv', quad.getAttribute('uv'));
    const attr = (size: number): InstancedBufferAttribute => {
      const a = new InstancedBufferAttribute(new Float32Array(capacity * size), size);
      a.setUsage(DynamicDrawUsage);
      return a;
    };
    this.offsets = attr(3);
    this.glyphs = attr(2);
    this.colors = attr(4);
    this.scales = attr(1);
    this.geometry.setAttribute('aOffset', this.offsets);
    this.geometry.setAttribute('aGlyph', this.glyphs);
    this.geometry.setAttribute('aColor', this.colors);
    this.geometry.setAttribute('aScale', this.scales);
    this.geometry.instanceCount = 0;

    const glyphHeight = 0.42;
    const material = new ShaderMaterial({
      uniforms: {
        uAtlas: { value: createAtlas() },
        uGlyphCount: { value: CHARS.length },
        uSize: { value: [glyphHeight * (CELL_W / CELL_H), glyphHeight] },
        uAdvance: { value: glyphHeight * ((CELL_W - 1) / CELL_H) },
      },
      vertexShader: VERTEX,
      fragmentShader: FRAGMENT,
      transparent: true,
      depthTest: false,
      depthWrite: false,
    });
    this.mesh = new Mesh(this.geometry, material);
    this.mesh.frustumCulled = false;
    this.mesh.renderOrder = RENDER_ORDER.damageNumbers;
  }

  get active(): number {
    return this.numbers.length;
  }

  /** Muestra un número de daño. `kind`: 0 normal, 1 crítico, 2 supercrítico, -1 daño al jugador. */
  spawn(x: number, y: number, z: number, amount: number, kind: number): void {
    if (this.numbers.length >= MAX_NUMBERS) this.numbers.shift();
    const value = Math.max(1, Math.round(amount));
    let text = String(value);
    if (kind >= 1) text += kind >= 2 ? '!!' : '!';
    const color = kind < 0 ? this.palette.player : kind >= 2 ? this.palette.super : kind === 1 ? this.palette.crit : this.palette.normal;
    this.numbers.push({
      x: x + this.rng.range(-0.25, 0.25),
      y,
      z: z + this.rng.range(-0.25, 0.25),
      drift: this.rng.range(-0.4, 0.4),
      age: 0,
      text: text.slice(0, MAX_CHARS),
      color,
      scale: kind >= 1 ? 1.45 : kind < 0 ? 1.3 : 1,
    });
  }

  clear(): void {
    this.numbers.length = 0;
    this.geometry.instanceCount = 0;
  }

  update(dt: number): void {
    let n = 0;
    const offsets = this.offsets.array as Float32Array;
    const glyphs = this.glyphs.array as Float32Array;
    const colors = this.colors.array as Float32Array;
    const scales = this.scales.array as Float32Array;
    for (let k = this.numbers.length - 1; k >= 0; k--) {
      const num = this.numbers[k] as FloatingNumber;
      num.age += dt;
      if (num.age >= LIFE) {
        this.numbers.splice(k, 1);
        continue;
      }
      const t = num.age / LIFE;
      const rise = 1.5 * t - 0.6 * t * t;
      const alpha = t < 0.65 ? 1 : 1 - (t - 0.65) / 0.35;
      const pop = num.age < 0.08 ? 1.35 - (num.age / 0.08) * 0.35 : 1;
      const len = num.text.length;
      for (let c = 0; c < len; c++) {
        offsets[n * 3] = num.x + num.drift * t;
        offsets[n * 3 + 1] = num.y + rise;
        offsets[n * 3 + 2] = num.z;
        glyphs[n * 2] = CHARS.indexOf(num.text[c] as string);
        glyphs[n * 2 + 1] = c - (len - 1) / 2;
        colors[n * 4] = num.color.r;
        colors[n * 4 + 1] = num.color.g;
        colors[n * 4 + 2] = num.color.b;
        colors[n * 4 + 3] = alpha;
        scales[n] = num.scale * pop;
        n++;
      }
    }
    this.geometry.instanceCount = n;
    this.offsets.needsUpdate = true;
    this.glyphs.needsUpdate = true;
    this.colors.needsUpdate = true;
    this.scales.needsUpdate = true;
  }
}
