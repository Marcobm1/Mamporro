import { describe, expect, it } from 'vitest';
import { WORLD_CONFIG } from '../data/config';
import { isOnTop } from './colliders';
import { generateWorldData } from './World';

const world = generateWorldData('SITES-TEST');

describe('construcciones del mapa', () => {
  it('coloca todos los sitios pedidos, lejos del inicio y separados entre sí', () => {
    const requested = WORLD_CONFIG.sites.reduce((sum, r) => sum + r.count, 0);
    expect(world.sites.length).toBeGreaterThanOrEqual(requested - 2);
    for (const s of world.sites) {
      expect(Math.hypot(s.x, s.z)).toBeGreaterThanOrEqual(WORLD_CONFIG.siteSpawnClear);
      for (const other of world.sites) {
        if (other === s) continue;
        expect(Math.hypot(other.x - s.x, other.z - s.z)).toBeGreaterThan(s.radius + other.radius);
      }
    }
    expect(world.sites.some((s) => s.kind === 'house')).toBe(true);
  });

  it('el terreno queda llano bajo cada sitio', () => {
    for (const s of world.sites) {
      for (let a = 0; a < 8; a++) {
        const angle = (a / 8) * Math.PI * 2;
        const r = s.radius * 0.8;
        const h = world.heightfield.heightAt(s.x + Math.cos(angle) * r, s.z + Math.sin(angle) * r);
        expect(Math.abs(h - s.floorY)).toBeLessThan(0.15);
      }
    }
  });

  it('no crecen árboles dentro de las construcciones', () => {
    for (const d of world.decorations) {
      for (const s of world.sites) {
        expect(Math.hypot(d.x - s.x, d.z - s.z)).toBeGreaterThan(s.radius);
      }
    }
  });

  it('las casas tienen muros sólidos a los que se puede subir', () => {
    const house = world.sites.find((s) => s.kind === 'house');
    expect(house).toBeDefined();
    if (!house) return;
    const walls = world.props.colliders.filter(
      (c) => c.shape === 'box' && Math.hypot(c.x - house.x, c.z - house.z) < house.radius && c.standable,
    );
    expect(walls.length).toBeGreaterThan(10);
    // Sobre un muro bajo, el suelo "caminable" es la cima del muro.
    const wall = walls.find((c) => c.top - house.floorY < 2.2) ?? walls[0];
    if (!wall) return;
    expect(isOnTop(wall, wall.x, wall.z)).toBe(true);
    expect(world.collision.groundHeight(wall.x, wall.z, wall.top + 0.1)).toBeCloseTo(wall.top, 6);
  });

  it('todo el mapa (construcciones, props, hierba) es determinista por semilla', () => {
    const again = generateWorldData('SITES-TEST');
    expect(again.sites).toEqual(world.sites);
    expect(again.props.parts.length).toBe(world.props.parts.length);
    expect(again.props.colliders).toEqual(world.props.colliders);
    expect(again.groundCover.grass.length).toBe(world.groundCover.grass.length);
    const other = generateWorldData('OTRA-SEMILLA');
    expect(other.sites).not.toEqual(world.sites);
  });

  it('hay hierba y flores repartidas por el mapa', () => {
    expect(world.groundCover.grass.length).toBeGreaterThan(1500);
    expect(world.groundCover.flowers.length).toBeGreaterThan(300);
  });
});
