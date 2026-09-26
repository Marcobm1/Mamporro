import { describe, expect, it } from 'vitest';
import { CHARACTERS } from '../data/characters';
import { BOSS_CONFIG, ENEMIES } from '../data/enemies';
import { ITEM_EFFECTS, ITEMS } from '../data/items';
import { chestCost, SHRINE_CONFIG, TOTEM_CONFIG } from '../data/run';
import { PlayerBody } from '../entities/playerPhysics';
import { purseBonus } from '../systems/items';
import type { OfferCard } from '../systems/levelup';
import type { InteractableSpot } from '../world/interactables';
import { generateWorldData } from '../world/World';
import { NO_EFFECTS, Run, type RunEffects, type RunNotice } from './Run';

const DT = 1 / 60;
const SEED = 'PARTIDA-TEST';
const world = generateWorldData(SEED);
const character = CHARACTERS.remedios;

interface Recorder {
  notices: RunNotice[];
  items: string[];
  explosions: number;
  pearls: number;
  revives: number;
}

function setup(minutes: 5 | 10 | 15 = 10): { run: Run; body: PlayerBody; rec: Recorder } {
  const rec: Recorder = { notices: [], items: [], explosions: 0, pearls: 0, revives: 0 };
  const fx: RunEffects = {
    ...NO_EFFECTS,
    notice: (n) => rec.notices.push(n),
    itemGained: (item) => rec.items.push(item.id),
    explosion: () => rec.explosions++,
    pearl: () => rec.pearls++,
    revive: () => rec.revives++,
  };
  const run = new Run(world.collision, SEED, character, fx, { minutes, interactables: world.interactables });
  run.invincible = true;
  const body = new PlayerBody();
  body.placeAt(0, world.heightfield.heightAt(0, 0), 0);
  return { run, body, rec };
}

function goTo(run: Run, body: PlayerBody, x: number, z: number): void {
  body.placeAt(x, world.collision.groundHeight(x, z, Number.POSITIVE_INFINITY), z);
  run.update(DT, body, 0);
}

function first(kind: InteractableSpot['kind']): InteractableSpot {
  const spot = world.interactables.find((s) => s.kind === kind);
  if (!spot) throw new Error(`no hay ${kind}`);
  return spot;
}

describe('partida: oro y cofres', () => {
  it('abrir un baúl cuesta oro, da un objeto y encarece el siguiente', () => {
    const { run, body, rec } = setup();
    const chest = first('chest');
    goTo(run, body, chest.x + 1.6, chest.z);
    expect(run.prompt).toEqual(expect.objectContaining({ kind: 'chest', cost: chestCost(0) }));
    // Sin oro no se abre y avisa de lo que falta.
    expect(run.interact()).toBe(false);
    expect(rec.notices).toContainEqual({ kind: 'noGold', missing: chestCost(0) });
    run.debugAddGold(100);
    expect(run.interact()).toBe(true);
    expect(run.gold).toBe(100 - chestCost(0));
    expect(run.chestsOpened).toBe(1);
    expect(run.items).toHaveLength(1);
    expect(rec.items).toHaveLength(1);
    run.update(DT, body, 0);
    // Ya abierto: no se puede volver a usar.
    expect(run.prompt?.kind === 'chest' && run.interactables.list[run.prompt.index]?.spot === chest).toBe(false);
    expect(run.interactables.nextChestCost).toBe(chestCost(1));
  });

  it('el oro recogido se multiplica con el décimo de lotería', () => {
    const { run, body } = setup();
    run.addItem(ITEMS.loteria);
    run.coins.spawn(body.x, body.y + 0.5, body.z, 10);
    for (let t = 0; t < 30; t++) run.update(DT, body, 0);
    expect(run.gold).toBeCloseTo(10 * (1 + 0.4));
    expect(run.goldCollected).toBeCloseTo(run.gold);
  });

  it('el monedero pega más con cada centena de oro que se lleva', () => {
    const { run } = setup();
    run.addItem(ITEMS.monedero);
    const base = run.stats.damage;
    run.debugAddGold(250);
    expect(run.stats.damage).toBeCloseTo(base + purseBonus(250, 1));
    expect(run.weapons[0]?.effective.damage).toBeCloseTo((run.weapons[0]?.stats.damage ?? 0) * run.stats.damage);
  });
});

describe('partida: santuarios y tótems', () => {
  it('cargar la mesa camilla abre una elección de bendiciones que se quedan para siempre', () => {
    const { run, body, rec } = setup();
    const shrine = first('shrine');
    body.placeAt(shrine.x + 1.5, shrine.y, shrine.z);
    for (let t = 0; t < Math.ceil(SHRINE_CONFIG.chargeTime / DT) + 5 && run.pendingShrines === 0; t++) run.update(DT, body, 0);
    expect(run.pendingShrines).toBe(1);
    expect(rec.notices).toContainEqual({ kind: 'shrineCharged' });
    expect(run.openChoice()).toBe(true);
    expect(run.offerSource).toBe('shrine');
    const offer = run.offer as OfferCard[];
    expect(offer).toHaveLength(SHRINE_CONFIG.choices);
    expect(offer.every((c) => c.kind === 'boost')).toBe(true);
    // Sin Reroll, Saltar ni Descartar en los santuarios.
    expect(run.reroll()).toBe(false);
    expect(run.skip()).toBe(false);
    expect(run.banish(0)).toBe(false);
    const card = offer[0];
    if (card?.kind !== 'boost') throw new Error('no es una bendición');
    const stat = card.boost.effect.stat;
    const before = run.stats[stat];
    run.choose(0);
    expect(run.stats[stat]).toBeCloseTo(Math.min(before + card.amount, stat === 'choices' ? 4 : Number.POSITIVE_INFINITY));
    expect(run.offer).toBeNull();
    expect(run.boosts).toHaveLength(1);
  });

  it('el tótem: desafío con más suerte durante un rato y un objeto al superarlo', () => {
    const { run, body, rec } = setup();
    const totem = first('totem');
    goTo(run, body, totem.x + 1.5, totem.z);
    const luck = run.stats.luck;
    expect(run.interact()).toBe(true);
    expect(run.interactables.challenge).toBe(TOTEM_CONFIG.duration);
    expect(run.stats.luck).toBe(luck + TOTEM_CONFIG.luck);
    expect(rec.notices).toContainEqual({ kind: 'challengeStart' });
    for (let t = 0; t < Math.ceil(TOTEM_CONFIG.duration / DT) + 2; t++) run.update(DT, body, 0);
    expect(run.interactables.challenge).toBe(0);
    expect(run.stats.luck).toBeGreaterThanOrEqual(luck);
    expect(rec.notices).toContainEqual({ kind: 'challengeDone' });
    expect(run.items.length).toBeGreaterThanOrEqual(1);
  });
});

describe('partida: jefe, enjambre y victoria', () => {
  it('el portal invoca al jefe con más vida cuanto más avanzada va la partida, y vencerle gana', () => {
    const { run, body, rec } = setup();
    run.time = 240;
    const portal = first('portal');
    goTo(run, body, portal.x + 2, portal.z);
    expect(run.prompt?.kind).toBe('portal');
    expect(run.interact()).toBe(true);
    expect(rec.notices).toContainEqual({ kind: 'boss', enemy: BOSS_CONFIG.enemy });
    const health = run.bossHealth;
    const m = run.difficulty;
    expect(health?.maxHp).toBeCloseTo(ENEMIES.pelusaMadre.hp * (1 + BOSS_CONFIG.hpGrowth * m + BOSS_CONFIG.hpCurve * m * m), 0);
    // Solo un jefe a la vez.
    expect(run.debugSummonBoss()).toBe(false);
    run.debugKillAll();
    expect(run.victory).toBe(true);
    expect(run.bossHealth).toBeNull();
  });

  it('al acabarse el tiempo empieza el enjambre y se revela el portal', () => {
    const { run, body, rec } = setup(5);
    run.time = 5 * 60 - DT / 2;
    run.update(DT, body, 0);
    expect(run.swarm).toBe(true);
    expect(run.timeLeft).toBeLessThanOrEqual(0);
    expect(rec.notices).toContainEqual({ kind: 'swarm' });
    expect(rec.notices).toContainEqual({ kind: 'portalRevealed' });
    expect(run.interactables.find('portal')?.discovered).toBe(true);
  });

  it('la oleada especial y el élite aparecen y avisan', () => {
    const { run, body, rec } = setup();
    run.time = 2.1 * 60;
    run.update(DT, body, 0);
    expect(rec.notices.some((n) => n.kind === 'wave')).toBe(true);
    expect(rec.notices).toContainEqual({ kind: 'elite', enemy: 'rata' });
  });
});

describe('partida: objetos especiales', () => {
  it('la bata de guatiné te levanta una vez con media vida', () => {
    const { run, body, rec } = setup();
    run.invincible = false;
    run.addItem(ITEMS.bata);
    run.debugSpawnEnemy('taper', body.x + 0.5, body.z);
    run.hp = 1;
    for (let t = 0; t < 30 && rec.revives === 0; t++) run.update(DT, body, 0);
    expect(rec.revives).toBe(1);
    expect(run.dead).toBe(false);
    expect(run.hp).toBeCloseTo(run.stats.maxHp * ITEM_EFFECTS.bata.reviveHp, 0);
    expect(run.items.some((s) => s.def.id === 'bata')).toBe(false);
    // Sin bata, se cae.
    run.hp = 0.5;
    run.invulnerable = 0;
    for (let t = 0; t < 120 && !run.dead; t++) run.update(DT, body, 0);
    expect(run.dead).toBe(true);
  });

  it('la olla exprés hace explotar a algunos al morir y daña a los de alrededor', () => {
    const { run, body, rec } = setup();
    for (let k = 0; k < 10; k++) run.addItem(ITEMS.olla);
    for (let k = 0; k < 40; k++) run.debugSpawnEnemy('pelusa', body.x + 8 + (k % 8) * 0.9, body.z + Math.floor(k / 8) * 0.9);
    const before = run.enemies.count;
    run.debugKillAll();
    expect(rec.explosions).toBeGreaterThan(0);
    expect(run.enemies.count).toBeLessThan(before);
  });

  it('el collar de perlas hace saltar perlas en los críticos', () => {
    const { run, body, rec } = setup();
    for (let k = 0; k < 4; k++) run.addItem(ITEMS.perlas);
    for (let k = 0; k < 20; k++) run.addItem(ITEMS.gafas);
    for (let k = 0; k < 12; k++) run.debugSpawnEnemy('taper', body.x + 3 + (k % 4), body.z + 3 + Math.floor(k / 4));
    for (let t = 0; t < 60 * 5 && rec.pearls === 0; t++) run.update(DT, body, 0);
    expect(rec.pearls).toBeGreaterThan(0);
  });
});

describe('partida: ritmo y relleno', () => {
  it('en una partida de 5 minutos los enemigos dan el doble de experiencia que en una de 10', () => {
    const short = setup(5);
    const normal = setup(10);
    short.run.time = 60;
    normal.run.time = 120;
    short.run.debugSpawnEnemy('pelusa', 20, 20);
    normal.run.debugSpawnEnemy('pelusa', 20, 20);
    expect(short.run.enemies.xp[0]).toBeCloseTo((normal.run.enemies.xp[0] as number) * 2);
  });

  it('sin nada que ofrecer, las cartas de relleno curan o dan oro', () => {
    const { run } = setup();
    run.pendingLevelUps = 1;
    run.offer = [{ kind: 'gold', key: null, amount: 33 }];
    run.choose(0);
    expect(run.gold).toBe(33);
  });
});
