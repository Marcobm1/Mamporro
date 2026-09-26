import { describe, expect, it } from 'vitest';
import { Run } from '../core/Run';
import { Rng } from '../core/rng';
import { CHARACTERS } from '../data/characters';
import { RARITIES, rarityById } from '../data/rarities';
import { TOME_LIST, TOMES } from '../data/tomes';
import { LEVEL_UP_CONFIG, WEAPON_UPGRADE_STEPS } from '../data/upgrades';
import { WEAPON_LIST, WEAPONS } from '../data/weapons';
import { generateWorldData } from '../world/World';
import {
  generateOffer,
  isTomeUseful,
  rarityWeights,
  rollRarity,
  rollWeaponUpgrade,
  tomeAmounts,
  tomeKey,
  weaponKey,
  type BuildView,
  type OfferCard,
} from './levelup';
import { computePlayerStats, tomeBonuses, zeroWeaponStats, type TomeInstance } from './stats';

const character = CHARACTERS.remedios;

function build(partial: Partial<BuildView> = {}): BuildView {
  return {
    weapons: [{ def: WEAPONS.chancla, bonus: zeroWeaponStats() }],
    tomes: [],
    stats: computePlayerStats(character, []),
    banished: new Set(),
    fillerGold: 20,
    ...partial,
  };
}

function tome(id: keyof typeof TOMES, bonus: number[]): TomeInstance {
  return { def: TOMES[id], level: 1, bonus };
}

describe('rarezas', () => {
  it('con Suerte 0 los pesos son los base; la Suerte sube la parte de las altas', () => {
    expect(rarityWeights(0)).toEqual(RARITIES.map((r) => r.weight));
    const share = (luck: number, index: number): number => {
      const w = rarityWeights(luck);
      return (w[index] as number) / w.reduce((a, b) => a + b, 0);
    };
    for (let i = 2; i < RARITIES.length; i++) {
      expect(share(50, i)).toBeGreaterThan(share(0, i));
      expect(share(100, i)).toBeGreaterThan(share(50, i));
    }
    expect(share(100, 0)).toBeLessThan(share(0, 0));
  });

  it('el sorteo sigue los pesos (20 000 tiradas)', () => {
    const rng = new Rng('rarezas');
    const counts = new Map<string, number>();
    const n = 20000;
    for (let k = 0; k < n; k++) {
      const id = rollRarity(0, rng).id;
      counts.set(id, (counts.get(id) ?? 0) + 1);
    }
    const total = RARITIES.reduce((a, r) => a + r.weight, 0);
    for (const r of RARITIES) expect((counts.get(r.id) ?? 0) / n).toBeCloseTo(r.weight / total, 1);
  });
});

describe('mejoras de armas', () => {
  it('Común y Poco común suben 1 o 2 estadísticas distintas de su lista; desde Rara, siempre 2', () => {
    const rng = new Rng('mejoras');
    for (const rarity of RARITIES) {
      const sizes = new Set<number>();
      for (let k = 0; k < 200; k++) {
        const changes = rollWeaponUpgrade(WEAPONS.chancla, zeroWeaponStats(), rarity, rng);
        const stats = changes.map((c) => c.stat);
        sizes.add(changes.length);
        expect(new Set(stats).size).toBe(stats.length);
        for (const s of stats) expect(WEAPONS.chancla.upgradable).toContain(s);
      }
      expect([...sizes].sort()).toEqual(rarity.stats[0] === rarity.stats[1] ? [2] : [1, 2]);
    }
  });

  it('las rarezas altas mejoran más; cantidad y perforación suben en enteros', () => {
    const amountOf = (rarity: string, stat: 'damage' | 'count'): number => {
      const rng = new Rng(`cantidad-${rarity}`);
      for (let k = 0; k < 500; k++) {
        const change = rollWeaponUpgrade(WEAPONS.chancla, zeroWeaponStats(), rarityById(rarity as 'common'), rng).find((c) => c.stat === stat);
        if (change) return change.amount;
      }
      throw new Error('no salió');
    };
    expect(amountOf('common', 'damage')).toBeCloseTo(WEAPON_UPGRADE_STEPS.damage.amount);
    expect(amountOf('legendary', 'damage')).toBeCloseTo(WEAPON_UPGRADE_STEPS.damage.amount * rarityById('legendary').power);
    expect(amountOf('rare', 'damage')).toBeGreaterThan(amountOf('uncommon', 'damage'));
    expect(amountOf('common', 'count')).toBe(1);
    expect(amountOf('legendary', 'count')).toBe(2);
  });

  it('una estadística en su tope deja de salir y la mejora no se pasa del tope', () => {
    const rng = new Rng('topes');
    const bonus = zeroWeaponStats();
    bonus.count = WEAPON_UPGRADE_STEPS.count.maxBonus ?? 0;
    bonus.area = (WEAPON_UPGRADE_STEPS.area.maxBonus ?? 0) - 0.05;
    for (let k = 0; k < 300; k++) {
      for (const change of rollWeaponUpgrade(WEAPONS.chancla, bonus, rarityById('legendary'), rng)) {
        expect(change.stat).not.toBe('count');
        if (change.stat === 'area') expect(change.amount).toBeCloseTo(0.05);
      }
    }
  });
});

describe('tomos', () => {
  it('se suman nivel a nivel y respetan los topes del jugador', () => {
    const stats = computePlayerStats(character, tomeBonuses([tome('damage', [0.24]), tome('moveSpeed', [5]), tome('vitality', [40, 0.8])]));
    expect(stats.damage).toBeCloseTo(1.24);
    expect(stats.moveSpeed).toBe(1.8);
    expect(stats.maxHp).toBe(character.maxHp + 40);
    expect(stats.regen).toBeCloseTo(0.8);
    // El imán suma una fracción del radio base del personaje.
    expect(computePlayerStats(character, tomeBonuses([tome('magnet', [0.5, 0.1])])).pickupRadius).toBeCloseTo(character.pickupRadius * 1.5);
  });

  it('las rarezas multiplican lo que suma un nivel, y los enteros siguen siendo enteros', () => {
    expect(tomeAmounts(TOMES.damage, rarityById('common'))).toEqual([0.12]);
    const legendary = tomeAmounts(TOMES.vitality, rarityById('legendary'));
    expect(Number.isInteger(legendary[0])).toBe(true);
    expect(legendary[0]).toBeGreaterThan(20);
    expect(tomeAmounts(TOMES.projectiles, rarityById('rare'))).toEqual([1]);
  });

  it('un tomo con todas sus estadísticas al tope deja de ofrecerse', () => {
    const capped = computePlayerStats(character, tomeBonuses([tome('moveSpeed', [5])]));
    expect(isTomeUseful(TOMES.moveSpeed, capped)).toBe(false);
    expect(isTomeUseful(TOMES.damage, capped)).toBe(true);
    const appears = (stats: BuildView['stats'], bonus: number): boolean => {
      for (let k = 0; k < 100; k++) {
        const offer = generateOffer(build({ stats, tomes: [tome('moveSpeed', [bonus])] }), 3, new Rng(`tope-${k}`));
        if (offer.some((c) => c.key === tomeKey('moveSpeed'))) return true;
      }
      return false;
    };
    expect(appears(capped, 5)).toBe(false);
    // Control: sin tope sí sale.
    expect(appears(computePlayerStats(character, tomeBonuses([tome('moveSpeed', [0.1])])), 0.1)).toBe(true);
  });
});

describe('cartas', () => {
  it('son distintas entre sí, del tamaño pedido y deterministas por semilla', () => {
    for (let k = 0; k < 100; k++) {
      const offer = generateOffer(build(), 3, new Rng(`cartas-${k}`));
      expect(offer).toHaveLength(3);
      const keys = offer.map((c) => c.key);
      expect(new Set(keys).size).toBe(3);
    }
    expect(generateOffer(build(), 4, new Rng('igual'))).toEqual(generateOffer(build(), 4, new Rng('igual')));
  });

  it('sin huecos no ofrece armas ni tomos nuevos', () => {
    const four = ['chancla', 'naftalina', 'barra', 'jersey'] as const;
    const fourTomes = TOME_LIST.slice(0, LEVEL_UP_CONFIG.maxTomes).map((d) => ({ def: d, level: 1, bonus: d.effects.map(() => 0.01) }));
    const full = build({
      weapons: four.map((id) => ({ def: WEAPONS[id], bonus: zeroWeaponStats() })),
      tomes: fourTomes,
    });
    for (let k = 0; k < 200; k++) {
      for (const card of generateOffer(full, 3, new Rng(`llenos-${k}`))) {
        expect(card.kind).not.toBe('newWeapon');
        if (card.kind === 'weaponUpgrade') expect(four).toContain(card.weapon);
        if (card.kind === 'tome') expect(fourTomes.map((t) => t.def.id)).toContain(card.tome);
      }
    }
  });

  it('lo descartado no vuelve a salir', () => {
    // Control: sin descartar, la mejora de la chancla sale a menudo.
    let seen = 0;
    for (let k = 0; k < 50; k++) if (generateOffer(build(), 3, new Rng(`fuera-${k}`)).some((c) => c.key === weaponKey('chancla'))) seen++;
    expect(seen).toBeGreaterThan(10);
    const banished = new Set([weaponKey('chancla'), weaponKey('barra'), tomeKey('luck')]);
    for (let k = 0; k < 200; k++) {
      for (const card of generateOffer(build({ banished }), 3, new Rng(`fuera-${k}`))) {
        if (card.key !== null) expect(banished.has(card.key)).toBe(false);
      }
    }
  });

  it('si no queda nada que ofrecer, sale una carta de curación (una sola)', () => {
    const banished = new Set([...WEAPON_LIST.map((w) => weaponKey(w.id)), ...TOME_LIST.map((t) => tomeKey(t.id))]);
    expect(generateOffer(build({ banished }), 3, new Rng('vacío'))).toEqual([
      { kind: 'heal', key: null, amount: LEVEL_UP_CONFIG.fillerHeal },
      { kind: 'gold', key: null, amount: 20 },
    ]);
  });
});

describe('subida de nivel en la partida', () => {
  const world = generateWorldData('LEVELUP-TEST');
  const newRun = (): Run => new Run(world.collision, 'LEVELUP-TEST', character);

  it('cada nivel deja una subida pendiente; elegir la aplica y abre la siguiente', () => {
    const run = newRun();
    run.debugLevelUp();
    run.debugLevelUp();
    expect(run.pendingLevelUps).toBe(2);
    expect(run.openChoice()).toBe(true);
    expect(run.offer).toHaveLength(LEVEL_UP_CONFIG.baseChoices);
    expect(run.choose(0)).toBe(true);
    expect(run.pendingLevelUps).toBe(1);
    expect(run.offer).not.toBeNull();
    expect(run.choose(1)).toBe(true);
    expect(run.pendingLevelUps).toBe(0);
    expect(run.offer).toBeNull();
    expect(run.openChoice()).toBe(false);
  });

  it('Reroll y Saltar gastan usos y no funcionan sin ellos', () => {
    const run = newRun();
    for (let k = 0; k < 5; k++) run.debugLevelUp();
    run.openChoice();
    for (let k = 0; k < LEVEL_UP_CONFIG.rerolls; k++) expect(run.reroll()).toBe(true);
    expect(run.reroll()).toBe(false);
    expect(run.rerolls).toBe(0);
    for (let k = 0; k < LEVEL_UP_CONFIG.skips; k++) expect(run.skip()).toBe(true);
    expect(run.skip()).toBe(false);
    expect(run.pendingLevelUps).toBe(5 - LEVEL_UP_CONFIG.skips);
  });

  it('Descartar quita la opción para siempre y pone otra carta en su hueco', () => {
    const run = newRun();
    run.debugLevelUp();
    run.openChoice();
    const offer = run.offer as OfferCard[];
    const index = offer.findIndex((c) => c.key !== null);
    const key = offer[index]?.key as string;
    expect(run.banish(index)).toBe(true);
    expect(run.banishes).toBe(LEVEL_UP_CONFIG.banishes - 1);
    expect(run.banished.has(key)).toBe(true);
    expect(run.offer).toHaveLength(LEVEL_UP_CONFIG.baseChoices);
    expect(run.offer?.some((c) => c.key === key)).toBe(false);
    const view: BuildView = { weapons: run.weapons, tomes: run.tomes, stats: run.stats, banished: run.banished, fillerGold: 20 };
    for (let k = 0; k < 100; k++) expect(generateOffer(view, 3, new Rng(`d-${k}`)).some((c) => c.key === key)).toBe(false);
  });

  it('cada tipo de carta hace lo que dice', () => {
    const run = newRun();
    const chancla = run.weapons[0];
    const choose = (card: OfferCard): void => {
      run.pendingLevelUps = 1;
      run.offer = [card];
      run.choose(0);
    };
    choose({ kind: 'weaponUpgrade', key: weaponKey('chancla'), weapon: 'chancla', rarity: 'common', changes: [{ stat: 'damage', amount: 0.25 }] });
    expect(chancla?.level).toBe(2);
    expect(chancla?.effective.damage).toBeCloseTo(WEAPONS.chancla.base.damage * 1.25);

    choose({ kind: 'tome', key: tomeKey('damage'), tome: 'damage', rarity: 'common', amounts: [0.2] });
    expect(chancla?.effective.damage).toBeCloseTo(WEAPONS.chancla.base.damage * 1.25 * 1.2);

    choose({ kind: 'newWeapon', key: weaponKey('dentaduras'), weapon: 'dentaduras' });
    expect(run.weapons.map((w) => w.def.id)).toEqual(['chancla', 'dentaduras']);
    // Las armas nuevas también reciben los tomos que ya se tienen.
    expect(run.weapons[1]?.effective.damage).toBeCloseTo(WEAPONS.dentaduras.base.damage * 1.2);

    choose({ kind: 'tome', key: tomeKey('vitality'), tome: 'vitality', rarity: 'common', amounts: [20, 0.4] });
    expect(run.stats.maxHp).toBe(character.maxHp + 20);
    expect(run.hp).toBe(character.maxHp + 20); // La vida ganada también cura.

    run.hp = 40;
    choose({ kind: 'heal', key: null, amount: 0.3 });
    expect(run.hp).toBeCloseTo(40 + 0.3 * run.stats.maxHp);
  });

  it('las cartas de una partida dependen solo de la semilla y de lo que se elige', () => {
    const a = newRun();
    const b = newRun();
    for (const run of [a, b]) {
      run.debugLevelUp();
      run.openChoice();
    }
    expect(a.offer).toEqual(b.offer);
  });
});
