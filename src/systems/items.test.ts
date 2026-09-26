import { describe, expect, it } from 'vitest';
import { Rng } from '../core/rng';
import { CHARACTERS } from '../data/characters';
import { ITEM_EFFECTS, ITEM_LIST, ITEMS } from '../data/items';
import { RARITIES } from '../data/rarities';
import { isItemAvailable, itemBonuses, ollaChance, pearlChance, purseBonus, rollItem, type ItemStack } from './items';
import { computePlayerStats } from './stats';

const character = CHARACTERS.remedios;
const baseStats = computePlayerStats(character, []);

describe('objetos: datos', () => {
  it('hay 12, con nombre y descripción, y de todas las rarezas', () => {
    expect(ITEM_LIST).toHaveLength(12);
    for (const r of RARITIES) expect(ITEM_LIST.some((i) => i.rarity === r.id)).toBe(true);
    for (const item of ITEM_LIST) {
      expect(item.nameKey).toBe(`item.${item.id}`);
      expect(item.descriptionKey).toBe(`item.${item.id}.desc`);
    }
  });

  it('la baraja es la mejora que da una cuarta opción al subir de nivel (una sola vez)', () => {
    const stats = computePlayerStats(character, itemBonuses([{ def: ITEMS.baraja, count: 1 }]), 3);
    expect(stats.choices).toBe(4);
    // Aunque se tuvieran dos, el tope es 4.
    expect(computePlayerStats(character, itemBonuses([{ def: ITEMS.baraja, count: 2 }]), 3).choices).toBe(4);
    expect(isItemAvailable(ITEMS.baraja, 1, stats)).toBe(false);
  });
});

describe('objetos: estadísticas', () => {
  it('cada copia suma su efecto y se acumulan con los demás', () => {
    const items: ItemStack[] = [
      { def: ITEMS.gafas, count: 2 },
      { def: ITEMS.lupa, count: 1 },
      { def: ITEMS.loteria, count: 1 },
      { def: ITEMS.cojin, count: 3 },
    ];
    const stats = computePlayerStats(character, itemBonuses(items));
    expect(stats.critChance).toBeCloseTo(0.14);
    expect(stats.critDamage).toBeCloseTo(0.25);
    expect(stats.goldGain).toBeCloseTo(1.4);
    expect(stats.armor).toBe(character.armor + 12);
  });

  it('un objeto que solo sube estadísticas al tope deja de salir; los especiales siempre salen', () => {
    const capped = computePlayerStats(character, itemBonuses([{ def: ITEMS.gafas, count: 60 }]));
    expect(capped.critChance).toBe(3);
    expect(isItemAvailable(ITEMS.gafas, 60, capped)).toBe(false);
    expect(isItemAvailable(ITEMS.lupa, 0, capped)).toBe(true);
    expect(isItemAvailable(ITEMS.olla, 25, capped)).toBe(true);
  });
});

describe('objetos: sorteo de los cofres', () => {
  it('sigue los pesos de rareza y la Suerte sube las altas', () => {
    const share = (luck: number): Map<string, number> => {
      const rng = new Rng(`cofres-${luck}`);
      const counts = new Map<string, number>();
      const n = 6000;
      for (let k = 0; k < n; k++) {
        const item = rollItem(luck, rng, [], baseStats);
        if (item) counts.set(item.rarity, (counts.get(item.rarity) ?? 0) + 1 / n);
      }
      return counts;
    };
    const plain = share(0);
    expect(plain.get('common')).toBeGreaterThan(0.55);
    expect(plain.get('common')).toBeLessThan(0.65);
    expect(plain.get('legendary') ?? 0).toBeLessThan(0.02);
    const lucky = share(150);
    expect(lucky.get('legendary') ?? 0).toBeGreaterThan((plain.get('legendary') ?? 0) * 2);
    expect(lucky.get('common') ?? 0).toBeLessThan(plain.get('common') ?? 0);
  });

  it('es determinista con la semilla y nunca da lo que ya no sirve', () => {
    const a = new Rng('igual');
    const b = new Rng('igual');
    for (let k = 0; k < 50; k++) expect(rollItem(30, a, [], baseStats)).toBe(rollItem(30, b, [], baseStats));
    const owned: ItemStack[] = [{ def: ITEMS.baraja, count: 1 }];
    const rng = new Rng('sin-baraja');
    for (let k = 0; k < 3000; k++) expect(rollItem(200, rng, owned, baseStats)?.id).not.toBe('baraja');
  });
});

describe('objetos: efectos especiales', () => {
  it('perlas y olla: probabilidad por copia, con tope', () => {
    expect(pearlChance(1)).toBeCloseTo(ITEM_EFFECTS.perlas.chance);
    expect(pearlChance(10)).toBe(1);
    expect(ollaChance(2)).toBeCloseTo(ITEM_EFFECTS.olla.chance * 2);
    expect(ollaChance(50)).toBe(ITEM_EFFECTS.olla.maxChance);
  });

  it('monedero: más daño por cada 100 de oro, con tope por copia', () => {
    const m = ITEM_EFFECTS.monedero;
    expect(purseBonus(99, 1)).toBe(0);
    expect(purseBonus(250, 1)).toBeCloseTo(2 * m.damagePer100);
    expect(purseBonus(250, 2)).toBeCloseTo(4 * m.damagePer100);
    expect(purseBonus(1e6, 1)).toBeCloseTo(m.maxBonus);
    expect(purseBonus(-50, 1)).toBe(0);
  });
});
