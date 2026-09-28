import { describe, expect, it } from 'vitest';
import { CHARACTERS, type CharacterId } from '../data/characters';
import { INITIAL_ITEMS, INITIAL_WEAPONS } from '../data/meta';
import { PlayerBody } from '../entities/playerPhysics';
import { generateWorldData } from '../world/World';
import { NO_EFFECTS, Run } from './Run';

const SEED = 'PULIDO-REFERENCIA';
const world = generateWorldData(SEED);

// Ensayo deliberadamente limitado: personaje inmóvil, orientado al norte,
// elecciones automáticas (primera carta), sin compras ni trucos. No mide habilidad.
function stationary(id: CharacterId, minutes: 5 | 10 | 15, record: boolean): { time: number; kills: number; level: number; hp: number; gold: number; shots: number } {
  let shots = 0;
  const effects = record ? { ...NO_EFFECTS, weaponFired: () => { shots++; } } : NO_EFFECTS;
  const run = new Run(world.collision, SEED, CHARACTERS[id], effects, {
    minutes, interactables: world.interactables, allowedWeapons: INITIAL_WEAPONS, allowedItems: INITIAL_ITEMS,
  });
  const body = new PlayerBody();
  body.placeAt(0, world.heightfield.heightAt(0, 0), 0);
  for (let tick = 0; tick < 120 * 60 && !run.dead; tick++) {
    run.update(1 / 60, body, 0);
    while (run.openChoice()) run.choose(0);
  }
  expect(run.cheated).toBe(false);
  return { time: run.time, kills: run.kills, level: run.level, hp: run.hp, gold: run.gold, shots };
}

describe('pulido: matriz inicial de personajes y duraciones', () => {
  for (const id of ['remedios', 'baguette'] as const) for (const minutes of [5, 10, 15] as const) {
    it(`${id}, ${minutes} minutos: audio separado de la simulación y balance reproducible`, () => {
      const audible = stationary(id, minutes, true);
      const silent = stationary(id, minutes, false);
      expect({ ...audible, shots: 0 }).toEqual(silent);
      expect(audible.shots).toBeGreaterThan(0);
      expect(audible.time).toBeGreaterThan(10);
      expect(audible.kills).toBeGreaterThan(0);
      expect(Number.isFinite(audible.gold)).toBe(true);
      console.log(`[balance] ${id} ${minutes}m: ${JSON.stringify(audible)}`);
    });
  }
});
