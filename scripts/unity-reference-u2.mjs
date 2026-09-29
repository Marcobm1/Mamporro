// Referencia complementaria de U2: valores esperados ejecutados exclusivamente con la
// web aprobada (0505b16). No importa código C# ni lee o escribe baseline.json.
//
//   node scripts/unity-reference-u2.mjs                 comprueba que u2-combat.json coincide
//   node scripts/unity-reference-u2.mjs --export-once   lo crea si no existe (nunca sobrescribe)
//
// Cada escenario guarda sus entradas (semillas, posiciones, acciones) junto a lo que
// produce la web, para que las pruebas C# reproduzcan las entradas y comparen salidas.
import {createServer} from 'vite';
import fs from 'node:fs/promises';
import {execFileSync} from 'node:child_process';

const BASE = '0505b1690656d15188860157612455639820fe1f';
const TARGET = 'unity/Docs/Reference/u2-combat.json';
// Guarda: src/ debe ser idéntico a la base aprobada (también sin cambios locales).
execFileSync('git', ['diff', '--exit-code', BASE, '--', 'src'], {stdio: 'pipe'});
if (execFileSync('git', ['status', '--porcelain', '--', 'src'], {encoding: 'utf8'}).trim() !== '') throw new Error('src/ tiene cambios sin commit.');

const server = await createServer({server: {middlewareMode: true, watch: null}, appType: 'custom', logLevel: 'error'});
try {
  const load = (p) => server.ssrLoadModule(`/src/${p}.ts`);
  const [{Run}, {CHARACTERS}, {WEAPON_LIST}, {TOME_LIST, TOMES}, {ITEM_LIST, ITEMS}, {ENEMY_LIST}, {Rng}, levelup, {RARITIES}, upgrades] = await Promise.all(
    ['core/Run', 'data/characters', 'data/weapons', 'data/tomes', 'data/items', 'data/enemies', 'core/rng', 'systems/levelup', 'data/rarities', 'data/upgrades'].map(load));
  const {WEAPON_UPGRADE_STEPS} = upgrades;
  const WSTATS = ['damage', 'cooldown', 'count', 'area', 'speed', 'duration', 'pierce', 'critChance', 'critMultiplier', 'knockback'];
  const PSTATS = ['damage', 'attackSpeed', 'extraProjectiles', 'area', 'critChance', 'critDamage', 'projectileSpeed', 'duration', 'knockback', 'moveSpeed', 'maxHp', 'regen', 'armor', 'pickupRadius', 'xpGain', 'luck', 'choices', 'goldGain'];
  const DT = 1 / 60;

  // Mundo plano de 96 m, el mismo que usan las pruebas C# (FlatWorld).
  const world = {heightfield: {size: 96, heightAt: () => 0}, groundHeight: () => 0, pushOutCircle: () => false,
    clampInside: (p) => { p.x = Math.max(-48, Math.min(48, p.x)); p.z = Math.max(-48, Math.min(48, p.z)); }, isInside: () => true};
  // Partida controlada: sin director ni aparición automática (eso es U3).
  const makeRun = (seed, character) => {
    const run = new Run(world, seed, CHARACTERS[character]);
    run.spawner.update = () => {};
    run.director.update = () => {};
    return run;
  };
  const body = () => ({x: 0, y: 0, z: 0, vx: 0, vz: 0, facing: 0, grounded: true});
  const f32 = (a, n) => Array.from(a.slice(0, n));
  const card = (c) => ({kind: c.kind, key: c.key ?? '', id: c.weapon ?? c.tome ?? '', rarity: c.rarity ?? '',
    changes: (c.changes ?? []).map((v) => ({stat: v.stat, amount: v.amount})), amounts: c.amounts ?? [], amount: c.amount ?? 0});
  const playerStats = (s) => Object.fromEntries(PSTATS.map((k) => [k, s[k]]));
  const weaponState = (w) => ({id: w.def.id, level: w.level, bonus: WSTATS.map((k) => w.bonus[k]), effective: WSTATS.map((k) => w.effective[k]),
    totalDamage: w.totalDamage, kills: w.kills});
  const buildState = (run) => ({
    level: run.progress.level, xp: run.progress.xp, pending: run.pendingLevelUps, rerolls: run.rerolls, skips: run.skips, banishes: run.banishes,
    hp: run.hp, gold: run.gold, weapons: run.weapons.map(weaponState),
    tomes: run.tomes.map((t) => ({id: t.def.id, level: t.level, bonus: [...t.bonus]})),
    items: run.items.map((s) => ({id: s.def.id, count: s.count})), banished: [...run.banished].sort(),
    stats: playerStats(run.stats),
  });
  const addTome = (run, id, levels) => { for (let k = 0; k < levels; k++) run.addTomeLevel(id, TOMES[id].effects.map((e) => e.amount)); };
  const addItem = (run, id, count) => { for (let k = 0; k < count; k++) run.addItem(ITEMS[id]); };
  const typeOf = (id) => ENEMY_LIST.findIndex((e) => e.id === id);
  // Tras cada tick, las subidas de nivel pendientes se resuelven eligiendo la primera carta.
  const resolveChoices = (run) => { run.openChoice(); while (run.offer) run.choose(0); };

  // ---------------------------------------------------------------- RNG
  const rng = ['ñ😀𝄞', '\u0000X', 'abc'].map((seed) => { const r = new Rng(seed); return {seed, nextU32: Array.from({length: 16}, () => r.nextU32())}; });

  // ---------------------------------------------------------------- rarezas y escalado
  const lucks = [0, 5, 10, 25, 50, 100, 250];
  const rarity = lucks.map((luck) => {
    const r = new Rng(`U2-RAREZA-${luck}`);
    return {luck, weights: levelup.rarityWeights(luck), rolls: Array.from({length: 40}, () => levelup.rollRarity(luck, r).id)};
  });
  const scaling = {
    weaponSteps: WSTATS.flatMap((stat) => RARITIES.map((r) => ({stat, rarity: r.id, amount: levelup.scaledAmount(WEAPON_UPGRADE_STEPS[stat].amount, r.power, !!WEAPON_UPGRADE_STEPS[stat].integer)}))),
    tomes: TOME_LIST.flatMap((t) => RARITIES.map((r) => ({tome: t.id, rarity: r.id, amounts: levelup.tomeAmounts(t, r)}))),
  };
  const zeroBonus = () => Object.fromEntries(WSTATS.map((k) => [k, 0]));
  const nearCap = () => ({...zeroBonus(), cooldown: 2.95, count: 7, area: 1.9, speed: 1.9, duration: 1.95, pierce: 14, knockback: 2.9});
  const upgradesOut = WEAPON_LIST.flatMap((w) => RARITIES.flatMap((r) => [['zero', zeroBonus], ['nearCap', nearCap]].map(([preset, make]) => {
    const seed = `U2-MEJORA-${w.id}-${r.id}-${preset}`;
    const g = new Rng(seed);
    const bonus = make();
    return {weapon: w.id, rarity: r.id, preset, bonus: WSTATS.map((k) => bonus[k]), seed,
      rolls: Array.from({length: 3}, () => ({changes: levelup.rollWeaponUpgrade(w, bonus, r, g).map((c) => ({stat: c.stat, amount: c.amount}))}))};
  })));

  // ---------------------------------------------------------------- agregación de estadísticas
  const statInputs = [
    {name: 'base-remedios', character: 'remedios'},
    {name: 'base-baguette', character: 'baguette'},
    ...TOME_LIST.map((t) => ({name: `tomo-${t.id}-x3`, character: 'remedios', tomes: [[t.id, 3]]})),
    ...ITEM_LIST.map((i) => ({name: `objeto-${i.id}-x2`, character: 'remedios', items: [[i.id, 2]]})),
    {name: 'tope-cadencia', character: 'remedios', tomes: [['attackSpeed', 40]]},
    {name: 'tope-proyectiles', character: 'remedios', tomes: [['projectiles', 20]]},
    {name: 'tope-suerte-iman', character: 'baguette', tomes: [['luck', 30], ['magnet', 20]], items: [['rulos', 4]]},
    {name: 'combinado', character: 'baguette', weapons: ['chancla', 'jersey', 'fregona'], tomes: [['damage', 2], ['area', 1], ['vitality', 2], ['moveSpeed', 1]],
      items: [['gafas', 3], ['lupa', 1], ['cojin', 2], ['zapatillas', 1], ['termo', 2], ['monedero', 1], ['loteria', 1]], gold: 250},
  ];
  const stats = statInputs.map((input) => {
    const run = makeRun('U2-ESTADISTICAS', input.character);
    for (const id of input.weapons ?? []) run.addWeapon(id);
    for (const [id, n] of input.tomes ?? []) addTome(run, id, n);
    for (const [id, n] of input.items ?? []) addItem(run, id, n);
    if (input.gold) run.debugAddGold(input.gold);
    return {...input, weapons: input.weapons ?? [], tomes: (input.tomes ?? []).map(([id, levels]) => ({id, levels})),
      items: (input.items ?? []).map(([id, count]) => ({id, count})), gold: input.gold ?? 0, result: buildState(run)};
  });

  // ---------------------------------------------------------------- ofertas y acciones de subida de nivel
  const act = (run, step) => {
    const [name, arg] = step.split(':');
    const index = arg === undefined ? -1 : Number(arg);
    switch (name) {
      case 'levelup': run.debugLevelUp(); return true;
      case 'open': return run.openChoice();
      case 'choose': return run.choose(index);
      case 'reroll': return run.reroll();
      case 'skip': return run.skip();
      case 'banish': return run.banish(index);
      default: throw new Error(step);
    }
  };
  const offerInputs = [
    {name: 'remedios-acciones', seed: 'U2-OF-A', character: 'remedios',
      steps: ['levelup', 'levelup', 'levelup', 'levelup', 'open', 'choose:0', 'reroll', 'choose:1', 'banish:0', 'choose:2', 'skip',
        'levelup', 'levelup', 'levelup', 'open', 'banish:1', 'banish:0', 'reroll', 'reroll', 'choose:0', 'skip', 'skip', 'choose:0']},
    {name: 'baguette-baraja', seed: 'U2-OF-B', character: 'baguette', items: [['baraja', 1]],
      steps: ['levelup', 'levelup', 'levelup', 'levelup', 'levelup', 'open', 'choose:3', 'choose:2', 'reroll', 'choose:1', 'banish:3', 'choose:0', 'choose:0']},
    {name: 'build-completa', seed: 'U2-OF-C', character: 'remedios', weapons: ['naftalina', 'barra', 'dentaduras'], tomes: [['damage', 1], ['area', 1], ['luck', 1], ['magnet', 1]],
      steps: ['levelup', 'levelup', 'levelup', 'levelup', 'open', 'choose:0', 'choose:1', 'choose:2', 'banish:0', 'choose:0']},
    {name: 'relleno', seed: 'U2-OF-D', character: 'remedios', banishAll: true,
      steps: ['levelup', 'levelup', 'open', 'banish:0', 'choose:1', 'choose:0']},
    {name: 'suerte-alta', seed: 'U2-OF-E', character: 'baguette', items: [['rulos', 3]], tomes: [['luck', 5]],
      steps: Array.from({length: 10}, () => ['levelup', 'open', 'choose:0']).flat()},
  ];
  const offers = offerInputs.map((input) => {
    const run = makeRun(input.seed, input.character);
    for (const id of input.weapons ?? []) run.addWeapon(id);
    for (const [id, n] of input.tomes ?? []) addTome(run, id, n);
    for (const [id, n] of input.items ?? []) addItem(run, id, n);
    if (input.banishAll) { for (const w of WEAPON_LIST) run.banished.add(`weapon:${w.id}`); for (const t of TOME_LIST) run.banished.add(`tome:${t.id}`); }
    const steps = input.steps.map((step) => ({step, result: act(run, step), offer: (run.offer ?? []).map(card), state: buildState(run)}));
    return {name: input.name, seed: input.seed, character: input.character, weapons: input.weapons ?? [],
      tomes: (input.tomes ?? []).map(([id, levels]) => ({id, levels})), items: (input.items ?? []).map(([id, count]) => ({id, count})),
      banishAll: !!input.banishAll, steps};
  });
  // Veinte ofertas seguidas con el mismo generador (consumo del RNG).
  const offerRun = makeRun('U2-OFERTAS', 'remedios');
  const offerSequence = Array.from({length: 20}, () => ({cards: levelup.generateOffer(offerRun.build, 3, offerRun.offerRng).map(card)}));

  // ---------------------------------------------------------------- armas
  const weapons = WEAPON_LIST.map((weapon) => {
    const run = makeRun('U2-EQUIVALENCIA', 'remedios');
    run.weapons.length = 0; run.addWeapon(weapon.id); run.invincible = true;
    const spawns = [[0, -6], [0, -7.2], [0, -8.4], [2, 0], [-2, 0], [0, 12]];
    for (const [x, z] of spawns) run.enemies.spawn(0, x, 0, z, 100, 1);
    run.enemies.rebuildGrid();
    const b = body();
    for (let tick = 0; tick < 120; tick++) { b.x = tick * 0.02; b.vx = 1.2; run.update(DT, b, 0); }
    const n = run.enemies.count;
    return {weapon: weapon.id, hp: f32(run.enemies.hp, n), x: f32(run.enemies.x, n), z: f32(run.enemies.z, n),
      damage: run.weapons[0].totalDamage, projectiles: run.projectiles.count};
  });

  // ---------------------------------------------------------------- enemigos (armas apagadas)
  const enemySample = (run, b, tick) => ({tick, hp: run.hp, invulnerable: run.invulnerable, px: b.x, pz: b.z, count: run.enemies.count,
    ex: f32(run.enemies.x, run.enemies.count), ez: f32(run.enemies.z, run.enemies.count), ehp: f32(run.enemies.hp, run.enemies.count),
    shots: run.enemyShots.count, bossEnraged: run.boss ? run.boss.enraged : false});
  // El jefe aparece dos veces: normal y con la vida al 45 % (enfurecido) con el jugador invulnerable.
  const enemyCases = [...ENEMY_LIST.map((def, type) => ({def, type, variant: 'normal', invincible: false, hpFraction: 1})),
    {def: ENEMY_LIST[typeOf('pelusaMadre')], type: typeOf('pelusaMadre'), variant: 'enfurecido', invincible: true, hpFraction: 0.45}];
  const enemies = enemyCases.map(({def, type, variant, invincible, hpFraction}) => {
    const run = makeRun(`U2-ENEMIGO-${def.id}-${variant}`, 'remedios');
    run.weaponsOff = true;
    run.invincible = invincible;
    const b = body();
    run.update(0, b, 0);
    let spawn;
    if (def.special === 'boss') {
      if (!run.debugSummonBoss()) throw new Error('jefe');
      const i = run.enemies.indexOfId(run.boss.enemyId);
      run.enemies.hp[i] = run.enemies.maxHp[i] * hpFraction;
      spawn = {x: run.enemies.x[i], z: run.enemies.z[i], hp: run.enemies.maxHp[i] / def.hp, gold: run.enemies.gold[i], boss: true};
    } else {
      run.enemies.spawn(type, 0, 0, -10, 1, 1);
      run.enemies.rebuildGrid();
      spawn = {x: 0, z: -10, hp: 1, gold: 1, boss: false};
    }
    const ticks = def.special === 'boss' ? 1200 : 480;
    const samples = [enemySample(run, b, 0)];
    for (let tick = 1; tick <= ticks; tick++) { run.update(DT, b, 0); if (tick % 20 === 0) samples.push(enemySample(run, b, tick)); }
    return {enemy: def.id, type, variant, invincible, hpFraction, ticks, spawn, samples};
  });

  // ---------------------------------------------------------------- pasivas
  const slowSpawns = [[0, -2.5], [2, -2], [-3, 0.5], [0, 4], [5, 5]];
  const passives = ['remedios', 'baguette'].map((character) => {
    const run = makeRun('U2-PASIVA', character);
    run.weaponsOff = true;
    const b = body();
    for (const [x, z] of slowSpawns) run.enemies.spawn(0, x, 0, z, 1, 1);
    run.enemies.rebuildGrid();
    const samples = [];
    for (let tick = 1; tick <= 600; tick++) {
      run.update(DT, b, 0);
      if (tick % 10 === 0) samples.push({tick, hp: run.hp, shield: run.shieldCharge, invulnerable: run.invulnerable,
        ex: f32(run.enemies.x, run.enemies.count), ez: f32(run.enemies.z, run.enemies.count)});
    }
    return {character, spawns: slowSpawns.map(([x, z]) => ({x, z})), samples};
  });

  // ---------------------------------------------------------------- combates integrados
  const ring = (n, types, radius, hp) => Array.from({length: n}, (_, k) => {
    const a = (k / n) * Math.PI * 2;
    return {type: typeOf(types[k % types.length]), x: Math.round(Math.cos(a) * (radius + (k % 3)) * 1000) / 1000, z: Math.round(Math.sin(a) * (radius + (k % 3)) * 1000) / 1000, hp};
  });
  const integrationInputs = [
    {name: 'remedios-cuatro-armas', seed: 'U2-INTEGRADO-A', character: 'remedios', invincible: true,
      weapons: ['naftalina', 'dentaduras', 'fregona'], tomes: [['damage', 2], ['area', 1]],
      items: [['gafas', 2], ['lupa', 1], ['perlas', 2], ['olla', 1], ['loteria', 1]],
      spawns: ring(40, ['pelusa', 'cucaracha', 'taper', 'paloma'], 7, 1)},
    {name: 'baguette-bata', seed: 'U2-INTEGRADO-B', character: 'baguette', invincible: false,
      weapons: ['jersey'], tomes: [], items: [['monedero', 1], ['bata', 1], ['cojin', 1]], gold: 150,
      spawns: [...ring(24, ['taper', 'pelusa', 'paloma'], 4, 3), {type: typeOf('rata'), x: 0, z: 9, hp: 1}]},
  ];
  const integration = integrationInputs.map((input) => {
    const run = makeRun(input.seed, input.character);
    run.invincible = input.invincible;
    for (const id of input.weapons) run.addWeapon(id);
    for (const [id, n] of input.tomes) addTome(run, id, n);
    for (const [id, n] of input.items) addItem(run, id, n);
    if (input.gold) run.debugAddGold(input.gold);
    for (const s of input.spawns) run.enemies.spawn(s.type, s.x, 0, s.z, s.hp, 1);
    run.enemies.rebuildGrid();
    const b = body();
    const samples = [];
    for (let tick = 1; tick <= 900; tick++) {
      run.update(DT, b, 0);
      resolveChoices(run);
      if (tick % 30 === 0) samples.push({tick, hp: run.hp, level: run.progress.level, xp: run.progress.xp, gold: run.gold, kills: run.kills,
        count: run.enemies.count, gems: run.gems.count, coins: run.coins.count, px: b.x, pz: b.z,
        items: run.items.map((s) => ({id: s.def.id, count: s.count})), weapons: run.weapons.map(weaponState)});
    }
    return {...input, tomes: input.tomes.map(([id, levels]) => ({id, levels})), items: input.items.map(([id, count]) => ({id, count})),
      gold: input.gold ?? 0, samples, final: buildState(run)};
  });

  const value = JSON.stringify({formatVersion: 1, source: BASE, rng, rarity, scaling, upgrades: upgradesOut, stats, offers, offerSequence,
    weapons, enemies, passives, integration}, null, 1) + '\n';
  if (process.argv.includes('--export-once')) {
    await fs.writeFile(TARGET, value, {flag: 'wx'});
    console.log(`Creado ${TARGET} (${value.length} bytes) desde la web aprobada.`);
  } else {
    const actual = await fs.readFile(TARGET, 'utf8');
    if (actual.replace(/\r\n/g, '\n') !== value) throw new Error(`${TARGET} no coincide con la web aprobada: revisar, no sobrescribir.`);
    console.log(`${TARGET} coincide con la web aprobada.`);
  }
} finally {
  await server.close();
}
