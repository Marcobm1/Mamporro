// Minimapa: el mapa entero visto desde arriba (norte arriba), con Doña Remedios,
// los interactuables ya descubiertos y el jefe. El terreno se pinta una vez por
// mapa; encima se dibujan las marcas unas diez veces por segundo.
import type { InteractableKind } from '../data/run';
import { PALETTE } from '../render/palette';
import { squircle, type Heightfield } from '../world/Heightfield';
import type { Site } from '../world/sites';
import { h } from './dom';

/** Resolución interna del minimapa (píxeles). */
const SIZE = 112;
const UPDATE_MS = 100;

export interface MinimapMarker {
  kind: InteractableKind;
  x: number;
  z: number;
  used: boolean;
}

export interface MinimapFrame {
  playerX: number;
  playerZ: number;
  /** Giro de la cámara (hacia dónde mira el jugador en pantalla). */
  yaw: number;
  markers: readonly MinimapMarker[];
  boss: { x: number; z: number } | null;
}

function css(hex: number): string {
  return `#${hex.toString(16).padStart(6, '0')}`;
}

export class Minimap {
  readonly root: HTMLCanvasElement;
  private readonly ctx: CanvasRenderingContext2D | null;
  private terrain: HTMLCanvasElement | null = null;
  private extent = 1;
  private lastDraw = 0;

  constructor() {
    this.root = h('canvas', { className: 'minimap' });
    this.root.width = SIZE;
    this.root.height = SIZE;
    this.ctx = this.root.getContext('2d');
  }

  /** Pinta el terreno de un mapa nuevo (alturas, construcciones y borde). */
  setTerrain(hf: Heightfield, sites: readonly Site[], limit: number): void {
    const canvas = document.createElement('canvas');
    canvas.width = SIZE;
    canvas.height = SIZE;
    const ctx = canvas.getContext('2d');
    this.terrain = canvas;
    this.extent = limit + 4;
    if (!ctx) return;
    const image = ctx.createImageData(SIZE, SIZE);
    const low = [PALETTE.grassLight, PALETTE.grass, PALETTE.grassDark, PALETTE.moss, PALETTE.rock, PALETTE.rockLight];
    let min = Number.POSITIVE_INFINITY;
    let max = Number.NEGATIVE_INFINITY;
    const heights = new Float32Array(SIZE * SIZE);
    for (let j = 0; j < SIZE; j++) {
      for (let i = 0; i < SIZE; i++) {
        const { x, z } = this.toWorld(i + 0.5, j + 0.5);
        const y = hf.heightAt(x, z);
        heights[j * SIZE + i] = y;
        if (squircle(x, z) <= limit) {
          min = Math.min(min, y);
          max = Math.max(max, y);
        }
      }
    }
    for (let j = 0; j < SIZE; j++) {
      for (let i = 0; i < SIZE; i++) {
        const { x, z } = this.toWorld(i + 0.5, j + 0.5);
        const y = heights[j * SIZE + i] as number;
        const inside = squircle(x, z) <= limit;
        const band = Math.min(low.length - 1, Math.max(0, Math.floor(((y - min) / Math.max(1, max - min)) * low.length)));
        const color = inside ? (low[band] as number) : 0x2a2438;
        // Sombreado: más claro hacia el noroeste, más oscuro hacia el sureste.
        const west = heights[j * SIZE + Math.max(0, i - 1)] as number;
        const north = heights[Math.max(0, j - 1) * SIZE + i] as number;
        const shade = Math.max(-0.25, Math.min(0.25, (y - west + (y - north)) * 0.12));
        const k = 4 * (j * SIZE + i);
        const r = (color >> 16) & 255;
        const g = (color >> 8) & 255;
        const b = color & 255;
        image.data[k] = Math.max(0, Math.min(255, r * (1 + shade)));
        image.data[k + 1] = Math.max(0, Math.min(255, g * (1 + shade)));
        image.data[k + 2] = Math.max(0, Math.min(255, b * (1 + shade)));
        image.data[k + 3] = 255;
      }
    }
    ctx.putImageData(image, 0, 0);
    // Construcciones: manchas de piedra.
    ctx.fillStyle = css(PALETTE.stoneDark);
    for (const site of sites) {
      const p = this.toMap(site.x, site.z);
      const r = Math.max(1.5, site.radius * this.scale * 0.6);
      ctx.fillRect(Math.round(p.x - r), Math.round(p.y - r), Math.round(r * 2), Math.round(r * 2));
    }
    this.lastDraw = 0;
  }

  private get scale(): number {
    return SIZE / (this.extent * 2);
  }

  private toMap(x: number, z: number): { x: number; y: number } {
    return { x: (x + this.extent) * this.scale, y: (z + this.extent) * this.scale };
  }

  private toWorld(px: number, py: number): { x: number; z: number } {
    return { x: px / this.scale - this.extent, z: py / this.scale - this.extent };
  }

  /** Redibuja (como mucho diez veces por segundo, salvo `force`). */
  update(frame: MinimapFrame, now: number, force = false): void {
    const ctx = this.ctx;
    if (!ctx || !this.terrain || (!force && now - this.lastDraw < UPDATE_MS)) return;
    this.lastDraw = now;
    ctx.imageSmoothingEnabled = false;
    ctx.drawImage(this.terrain, 0, 0);
    const blink = Math.floor(now / 350) % 2 === 0;
    for (const m of frame.markers) {
      const p = this.toMap(m.x, m.z);
      const x = Math.round(p.x);
      const y = Math.round(p.y);
      ctx.fillStyle = '#140f24';
      switch (m.kind) {
        case 'chest':
          ctx.fillRect(x - 2, y - 2, 5, 5);
          ctx.fillStyle = css(m.used ? PALETTE.used : PALETTE.coin);
          ctx.fillRect(x - 1, y - 1, 3, 3);
          break;
        case 'shrine':
          ctx.fillRect(x - 2, y - 3, 5, 7);
          ctx.fillRect(x - 3, y - 2, 7, 5);
          ctx.fillStyle = css(m.used ? PALETTE.used : PALETTE.shrineGlow);
          ctx.fillRect(x - 1, y - 2, 3, 5);
          ctx.fillRect(x - 2, y - 1, 5, 3);
          break;
        case 'totem':
          ctx.fillRect(x - 1, y - 4, 3, 8);
          ctx.fillStyle = css(m.used ? PALETTE.used : PALETTE.totemGlow);
          ctx.fillRect(x, y - 3, 1, 6);
          break;
        case 'portal':
          ctx.fillRect(x - 3, y - 4, 7, 9);
          ctx.fillStyle = css(m.used ? PALETTE.used : blink ? PALETTE.portalGlow : 0xffffff);
          ctx.fillRect(x - 2, y - 3, 5, 7);
          break;
      }
    }
    if (frame.boss) {
      const p = this.toMap(frame.boss.x, frame.boss.z);
      ctx.fillStyle = '#140f24';
      ctx.fillRect(Math.round(p.x) - 3, Math.round(p.y) - 3, 7, 7);
      ctx.fillStyle = blink ? css(PALETTE.telegraph) : '#ffffff';
      ctx.fillRect(Math.round(p.x) - 2, Math.round(p.y) - 2, 5, 5);
    }
    // Doña Remedios: flecha hacia donde mira la cámara.
    const p = this.toMap(frame.playerX, frame.playerZ);
    const fx = -Math.sin(frame.yaw);
    const fz = -Math.cos(frame.yaw);
    ctx.fillStyle = '#ffffff';
    ctx.strokeStyle = '#140f24';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(p.x + fx * 5, p.y + fz * 5);
    ctx.lineTo(p.x - fx * 3 - fz * 3, p.y - fz * 3 + fx * 3);
    ctx.lineTo(p.x - fx * 3 + fz * 3, p.y - fz * 3 - fx * 3);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
  }
}
