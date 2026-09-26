import { describe, expect, it } from 'vitest';
import { WORLD_CONFIG } from '../data/config';
import { chestCost, DISCOVERY_CONFIG, INTERACTABLE_PLACEMENT, SHRINE_CONFIG } from '../data/run';
import { pushOut, type PushResult } from '../world/colliders';
import { interactableClearRadius, type InteractableSpot } from '../world/interactables';
import { generateWorldData } from '../world/World';
import { Interactables, type InteractableEvents, type InteractableState } from './Interactables';

const DT = 1 / 60;

function spot(kind: InteractableSpot['kind'], x: number, z: number): InteractableSpot {
  return { kind, x, y: 0, z, rotation: 0 };
}

function events(): InteractableEvents & { found: InteractableState[]; charged: InteractableState[] } {
  const found: InteractableState[] = [];
  const charged: InteractableState[] = [];
  return { found, charged, discovered: (i) => found.push(i), shrineCharged: (i) => charged.push(i) };
}

describe('interactuables: colocación en el mapa', () => {
  const seeds = ['MAPA-1', 'MAPA-2', 'OTRO', 'XL4H96'];

  it('salen todos, siempre en el mismo sitio para la misma semilla', () => {
    for (const seed of seeds) {
      const spots = generateWorldData(seed).interactables;
      for (const p of INTERACTABLE_PLACEMENT) expect(spots.filter((s) => s.kind === p.kind)).toHaveLength(p.count);
    }
    expect(generateWorldData('MAPA-1').interactables).toEqual(generateWorldData('MAPA-1').interactables);
  });

  it('el portal está lejos del inicio y todos respetan distancias y separaciones', () => {
    for (const seed of seeds) {
      const spots = generateWorldData(seed).interactables;
      for (const s of spots) {
        const rule = INTERACTABLE_PLACEMENT.find((p) => p.kind === s.kind);
        expect(Math.hypot(s.x, s.z)).toBeGreaterThanOrEqual(rule?.minSpawnDistance ?? 0);
        for (const o of spots) {
          if (o !== s && o.kind === s.kind) expect(Math.hypot(o.x - s.x, o.z - s.z)).toBeGreaterThanOrEqual(rule?.spacing ?? 0);
        }
      }
      const portal = spots.find((s) => s.kind === 'portal');
      expect(Math.hypot(portal?.x ?? 0, portal?.z ?? 0)).toBeGreaterThan(90);
    }
  });

  it('no se meten en obstáculos ni los tapa la vegetación, y tienen colisión', () => {
    const push: PushResult = { dx: 0, dz: 0, nx: 0, nz: 0 };
    for (const seed of seeds) {
      const world = generateWorldData(seed);
      for (const s of world.interactables) {
        // Ningún colisionador de construcciones u objetos sueltos dentro de su radio libre...
        for (const c of world.props.colliders) expect(pushOut(c, s.x, s.z, (INTERACTABLE_PLACEMENT.find((p) => p.kind === s.kind)?.clearRadius ?? 1) - 0.01, push)).toBe(false);
        // ...ni árboles o rocas encima.
        for (const d of world.decorations) expect(Math.hypot(d.x - s.x, d.z - s.z)).toBeGreaterThanOrEqual(interactableClearRadius(s.kind) - 1e-6);
        // Y el suyo propio bloquea el paso.
        const pos = { x: s.x + 0.05, z: s.z };
        expect(world.collision.pushOutCircle(pos, 0.4, s.y - 0.1, 0.2)).toBe(true);
        expect(world.collision.isInside(s.x, s.z, 5)).toBe(true);
      }
    }
    expect(WORLD_CONFIG.playableRadius).toBeGreaterThan(100);
  });
});

describe('interactuables: estado en la partida', () => {
  it('se descubren al acercarse (el portal, más de cerca)', () => {
    const list = new Interactables([spot('chest', 0, 25), spot('portal', 0, -25)]);
    const ev = events();
    list.update(DT, 0, 0, ev);
    expect(ev.found.map((i) => i.spot.kind)).toEqual(['chest']);
    list.update(DT, 0, -25 + DISCOVERY_CONFIG.portalRadius - 0.1, ev);
    expect(ev.found.map((i) => i.spot.kind)).toEqual(['chest', 'portal']);
    // Solo avisa una vez.
    list.update(DT, 0, -25, ev);
    expect(ev.found).toHaveLength(2);
  });

  it('la mesa camilla se carga estando dentro y se descarga (más despacio) fuera', () => {
    const list = new Interactables([spot('shrine', 0, 0)]);
    const ev = events();
    const shrine = list.list[0] as InteractableState;
    const ticks = (seconds: number): number => Math.round(seconds / DT);
    for (let k = 0; k < ticks(SHRINE_CONFIG.chargeTime / 2); k++) list.update(DT, 1, 1, ev);
    expect(shrine.charge).toBeCloseTo(0.5, 1);
    expect(list.charging).toBe(0);
    for (let k = 0; k < ticks(1); k++) list.update(DT, SHRINE_CONFIG.radius + 1, 0, ev);
    expect(list.charging).toBe(-1);
    expect(shrine.charge).toBeCloseTo(0.5 - SHRINE_CONFIG.decayRate / SHRINE_CONFIG.chargeTime, 2);
    for (let k = 0; k < ticks(SHRINE_CONFIG.chargeTime); k++) list.update(DT, 0, 0, ev);
    expect(shrine.used).toBe(true);
    expect(ev.charged).toHaveLength(1);
  });

  it('lo que se puede usar delante: el más cercano al alcance, con el precio del cofre', () => {
    const list = new Interactables([spot('chest', 0, 0), spot('chest', 1.5, 0), spot('totem', 20, 0), spot('shrine', -10, 0)]);
    expect(list.prompt(1.2, 0)).toEqual({ index: 1, kind: 'chest', cost: chestCost(0) });
    (list.list[1] as InteractableState).used = true;
    list.chestsOpened = 1;
    expect(list.prompt(1.2, 0)).toEqual({ index: 0, kind: 'chest', cost: chestCost(1) });
    expect(list.prompt(-10, 0)).toBeNull();
    expect(list.prompt(19, 0)?.kind).toBe('totem');
    list.challenge = 10;
    expect(list.prompt(19, 0)).toBeNull();
  });

  it('el desafío cuenta atrás y avisa una sola vez al acabar', () => {
    const list = new Interactables([]);
    list.challenge = 1;
    const ev = events();
    let ended = 0;
    for (let k = 0; k < 120; k++) if (list.update(DT, 0, 0, ev)) ended++;
    expect(ended).toBe(1);
    expect(list.challenge).toBe(0);
  });

  it('los cofres encarecen y se puede revelar el mapa', () => {
    for (let n = 0; n < 10; n++) expect(chestCost(n + 1)).toBeGreaterThan(chestCost(n));
    const list = new Interactables([spot('chest', 50, 50), spot('portal', -90, 90)]);
    expect(list.reveal('portal')).toBe(1);
    expect(list.reveal('portal')).toBe(0);
    expect(list.reveal()).toBe(1);
    expect(list.list.every((i) => i.discovered)).toBe(true);
  });
});
