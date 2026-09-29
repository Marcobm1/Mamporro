// Referencia complementaria de U3 (mundo y partida completa): valores esperados
// ejecutados exclusivamente con la web aprobada (0505b16). No importa código C# ni
// lee o escribe baseline.json ni u2-combat.json.
//
//   node scripts/unity-reference-u3.mjs                 comprueba que u3-world.json coincide
//   node scripts/unity-reference-u3.mjs --export-once   lo crea si no existe (nunca sobrescribe)
//
// Cada escenario guarda sus entradas (semillas, rutas, intenciones, giros de cámara)
// junto a lo que produce la web; las pruebas C# reproducen las entradas y comparan.
// Las alturas van en base64 de Float32 (little-endian), bit a bit.
import {createServer} from 'vite';
import fs from 'node:fs/promises';
import {execFileSync} from 'node:child_process';

const BASE = '0505b1690656d15188860157612455639820fe1f';
const TARGET = 'unity/Docs/Reference/u3-world.json';
execFileSync('git', ['diff', '--exit-code', BASE, '--', 'src'], {stdio: 'pipe'});
if (execFileSync('git', ['status', '--porcelain', '--', 'src'], {encoding: 'utf8'}).trim() !== '') throw new Error('src/ tiene cambios sin commit.');

const server = await createServer({server: {middlewareMode: true, watch: null}, appType: 'custom', logLevel: 'error'});
try {
  const load = (p) => server.ssrLoadModule(`/src/${p}.ts`);
  const [{Rng}, {generateWorldData}, {generateHeightfield, squircle}, config, {Director}, waves, {SpawnSystem}, {EnemySystem}, {Run, NO_EFFECTS},
    {CHARACTERS}, physics, crowd, runData, {Interactables}, enemiesData] = await Promise.all(
    ['core/rng', 'world/World', 'world/Heightfield', 'data/config', 'systems/Director', 'data/waves', 'systems/SpawnSystem', 'systems/EnemySystem', 'core/Run',
      'data/characters', 'entities/playerPhysics', 'systems/crowd', 'data/run', 'systems/Interactables', 'data/enemies'].map(load));
  const simplex = await import('simplex-noise');
  const {TERRAIN_CONFIG, PLAYER_TUNING, PLAYER_BASE_STATS} = config;
  const DT = 1 / 60;
  const b64 = (f32) => Buffer.from(f32.buffer, f32.byteOffset, f32.byteLength).toString('base64');
  const f32 = (a, n) => Array.from(a.slice(0, n));

  // ---------------------------------------------------------------- simplex-noise
  const noiseRng = new Rng('U3-SIMPLEX');
  const noise = simplex.createNoise2D(() => noiseRng.next());
  const pts = Array.from({length: 240}, (_, i) => [(i % 20) * 0.731 - 7.3 + Math.floor(i / 20) * 13.9, Math.floor(i / 20) * 0.417 - 2.1 + (i % 20) * 5.3]);
  const simplexOut = {seed: 'U3-SIMPLEX', x: pts.map((p) => p[0]), z: pts.map((p) => p[1]), value: pts.map(([x, z]) => noise(x, z))};

  // ---------------------------------------------------------------- mundos
  const seeds = ['HITO6QA', 'PULIDO-REFERENCIA', 'MAMPORRO', 'U3-MUNDO'];
  // Cobertura del suelo completa y terreno sin aplanar solo en dos semillas (tamaño del archivo).
  const fullSeeds = new Set(['MAMPORRO', 'U3-MUNDO']);
  const worldCache = new Map();
  const worlds = seeds.map((seed) => {
    const data = generateWorldData(seed);
    worldCache.set(seed, data);
    const hf = data.heightfield;
    const raw = generateHeightfield(TERRAIN_CONFIG, new Rng(seed).derive('terrain'));
    const sampler = new Rng(`U3-MUESTRAS-${seed}`);
    const normal = {x: 0, y: 1, z: 0};
    const samples = Array.from({length: 300}, () => {
      const x = sampler.range(-165, 165);
      const z = sampler.range(-165, 165);
      const h = hf.heightAt(x, z);
      hf.normalAt(x, z, normal);
      const g = {x: 0, y: 1, z: 0};
      data.collision.groundNormal(x, z, h + 0.45, g);
      return {x, z, h, nx: normal.x, ny: normal.y, nz: normal.z, squircle: squircle(x, z), inside: data.collision.isInside(x, z, 2),
        ground: data.collision.groundHeight(x, z, h + 0.45), groundHigh: data.collision.groundHeight(x, z, Number.MAX_VALUE), gnx: g.x, gny: g.y, gnz: g.z};
    });
    // Empujones de obstáculos: puntos cerca de colisionadores reales.
    const pushes = [];
    const probe = new Rng(`U3-EMPUJES-${seed}`);
    for (let k = 0; k < 150; k++) {
      const c = data.colliders[Math.floor(probe.next() * data.colliders.length)];
      const x = c.x + probe.range(-1.6, 1.6);
      const z = c.z + probe.range(-1.6, 1.6);
      const feet = c.bottom + probe.range(-0.5, 1.2);
      const pos = {x, z};
      const hit = data.collision.pushOutCircle(pos, 0.5, feet, 1.6);
      const body = new physics.PlayerBody();
      body.placeAt(x, feet, z);
      body.vx = probe.range(-5, 5);
      body.vz = probe.range(-5, 5);
      data.collision.resolveObstacles(body, PLAYER_TUNING.radius, PLAYER_TUNING.stepHeight);
      const far = {x: x * 3, z: z * 3};
      data.collision.clampInside(far);
      pushes.push({x, z, feet, hit, px: pos.x, pz: pos.z, vx0: 0, bx: body.x, bz: body.z, bvx: body.vx, bvz: body.vz, farX: far.x, farZ: far.z});
    }
    const collider = (c) => ({shape: c.shape, x: c.x, z: c.z, bottom: c.bottom, top: c.top, standable: c.standable, radius: c.radius ?? 0,
      halfX: c.halfX ?? 0, halfZ: c.halfZ ?? 0, cos: c.cos ?? 0, sin: c.sin ?? 0});
    return {
      seed, full: fullSeeds.has(seed), rawHeights: fullSeeds.has(seed) ? b64(raw.heights) : '', heights: b64(hf.heights), samples, pushes,
      sites: data.sites,
      parts: data.props.parts.map((p) => ({shape: p.shape, material: p.material, color: p.color, size: p.size, position: p.position, rotation: p.rotation, segments: p.segments})),
      propColliders: data.props.colliders.length,
      interactables: data.interactables,
      decorations: data.decorations,
      grassCount: data.groundCover.grass.length,
      flowerCount: data.groundCover.flowers.length,
      grass: fullSeeds.has(seed) ? data.groundCover.grass : data.groundCover.grass.slice(0, 50),
      flowers: fullSeeds.has(seed) ? data.groundCover.flowers : data.groundCover.flowers.slice(0, 50),
      colliders: data.colliders.map(collider),
    };
  });

  // ---------------------------------------------------------------- física del jugador
  const world = worldCache.get('U3-MUNDO');
  const physicsCases = [
    {name: 'caminar-este', start: [0, 0], ticks: 600, route: [[1, 0, 0, 0]]},
    {name: 'diagonal-saltos', start: [0, 0], ticks: 600, route: [[0.7071067811865476, -0.7071067811865476, 60, 0]]},
    {name: 'deslizar-bajada', start: [122, 18], ticks: 480, route: [[-1, 0, 0, 1]]},
    {name: 'hacia-sitio', start: [0, 0], ticks: 900, route: [[0, 0, 0, 0]], site: 0},
    {name: 'subir-montana', start: [100, -100], ticks: 600, route: [[0.7071067811865476, -0.7071067811865476, 90, 0]]},
  ];
  const physicsOut = physicsCases.map((c) => {
    const body = new physics.PlayerBody();
    let [sx, sz] = c.start;
    body.placeAt(sx, world.collision.groundHeight(sx, sz, Number.MAX_VALUE), sz);
    let [mx, mz, jumpEvery, slide] = c.route[0];
    if (c.site !== undefined) {
      const s = world.sites[c.site];
      const d = Math.hypot(s.x - sx, s.z - sz);
      mx = (s.x - sx) / d;
      mz = (s.z - sz) / d;
    }
    const samples = [];
    for (let tick = 1; tick <= c.ticks; tick++) {
      const jump = jumpEvery > 0 && tick % jumpEvery === 0;
      const intent = {moveX: mx, moveZ: mz, jumpPressed: jump, jumpHeld: jumpEvery > 0 && tick % jumpEvery < 12, slidePressed: slide === 1 && tick === 30, slideHeld: slide === 1 && tick >= 30};
      crowd.stepPlayerInCrowd(body, intent, world.collision, PLAYER_TUNING, PLAYER_BASE_STATS.moveSpeed, 0, DT);
      if (tick % 5 === 0) samples.push({tick, x: body.x, y: body.y, z: body.z, vx: body.vx, vy: body.vy, vz: body.vz, grounded: body.grounded, sliding: body.sliding, onSteep: body.onSteep});
    }
    return {name: c.name, seed: 'U3-MUNDO', startX: sx, startZ: sz, moveX: mx, moveZ: mz, jumpEvery, slide, ticks: c.ticks, samples};
  });

  // ---------------------------------------------------------------- director
  const mods = {rate: 1, hp: 1, gold: 1};
  const director = [5, 10, 15].map((minutes) => {
    const d = new Director(minutes);
    const params = {minutes: 0, rate: 0, maxAlive: 0, hp: 0, xp: 0, gold: 0};
    const samples = [];
    for (let t = 0; t <= minutes * 60 + 90; t += 5) {
      d.spawnParams(t, mods, params);
      samples.push({time: t, minutes: params.minutes, rate: params.rate, maxAlive: params.maxAlive, hp: params.hp, xp: params.xp, gold: params.gold});
    }
    const events = [];
    const run = new Director(minutes);
    const total = (minutes * 60 + 90) * 60;
    let time = 0;
    for (let tick = 1; tick <= total; tick++) {
      time += DT;
      run.update(time, {
        wave: (w) => events.push({tick, kind: 'wave', enemy: w.enemy, count: w.count, formation: w.formation}),
        elite: () => events.push({tick, kind: 'elite', enemy: '', count: 0, formation: ''}),
        swarm: () => events.push({tick, kind: 'swarm', enemy: '', count: 0, formation: ''}),
      });
    }
    return {minutes, pace: d.pace, duration: d.duration, samples, events};
  });

  // ---------------------------------------------------------------- apariciones
  const spawnWorld = worldCache.get('MAMPORRO');
  const spawnCase = (() => {
    const enemies = new EnemySystem(800, spawnWorld.heightfield.size);
    const spawned = [];
    const spawner = new SpawnSystem(new Rng('U3-APARICION'), (x, y, z) => spawned.push({x, y, z}));
    const d = new Director(10);
    const params = {minutes: 0, rate: 0, maxAlive: 0, hp: 0, xp: 0, gold: 0};
    const view = {x: 0, z: 0, yaw: 0};
    const counts = [];
    for (let tick = 1; tick <= 3600; tick++) {
      const t = 120 + tick * DT;
      d.spawnParams(t, mods, params);
      view.x = Math.sin(tick * 0.004) * 60;
      view.z = Math.cos(tick * 0.003) * 60 - 30;
      view.yaw = tick * 0.01;
      spawner.update(DT, params, enemies, view, spawnWorld.collision);
      if (tick % 60 === 0) counts.push({tick, count: enemies.count});
    }
    const formations = ['line', 'ring', 'arc'].map((formation) => {
      const e2 = new EnemySystem(800, spawnWorld.heightfield.size);
      const s2 = new SpawnSystem(new Rng(`U3-FORMACION-${formation}`));
      d.spawnParams(300, mods, params);
      s2.spawnFormation('pelusa', 20, formation, params, e2, {x: 10, z: -20, yaw: 0.7}, spawnWorld.collision);
      return {formation, count: e2.count, x: f32(e2.x, e2.count), y: f32(e2.y, e2.count), z: f32(e2.z, e2.count), type: Array.from(e2.type.slice(0, e2.count)), hp: f32(e2.hp, e2.count)};
    });
    return {seed: 'MAMPORRO', rng: 'U3-APARICION', startTime: 120, spawned: spawned.slice(0, 400), total: spawned.length, counts,
      finalX: f32(enemies.x, enemies.count), finalZ: f32(enemies.z, enemies.count), finalType: Array.from(enemies.type.slice(0, enemies.count)), formations};
  })();

  // ---------------------------------------------------------------- interactuables
  const chestCosts = Array.from({length: 20}, (_, n) => runData.chestCost(n));
  const interactablesCase = (() => {
    const data = worldCache.get('MAMPORRO');
    const system = new Interactables(data.interactables);
    const log = [];
    const shrineIndex = system.list.findIndex((i) => i.spot.kind === 'shrine');
    const shrine = system.list[shrineIndex].spot;
    // Recorrido: acercarse a cada interactuable en orden de la lista, 90 ticks cada uno;
    // en el primer santuario, 5 s dentro, 5 s fuera y dentro hasta cargar.
    const positions = [];
    for (const item of system.list) for (let k = 0; k < 90; k++) positions.push([item.spot.x + 1, item.spot.z + 1]);
    for (let k = 0; k < 300; k++) positions.push([shrine.x, shrine.z]);
    for (let k = 0; k < 300; k++) positions.push([shrine.x + 20, shrine.z]);
    for (let k = 0; k < 700; k++) positions.push([shrine.x + 1, shrine.z]);
    const events = {discovered: (item) => log.push({tick: currentTick, kind: 'discovered', index: system.list.indexOf(item)}), shrineCharged: (item) => log.push({tick: currentTick, kind: 'shrineCharged', index: system.list.indexOf(item)})};
    let currentTick = 0;
    const charge = [];
    const prompts = [];
    positions.forEach(([x, z], k) => {
      currentTick = k + 1;
      system.update(DT, x, z, events);
      if (currentTick % 30 === 0) {
        charge.push({tick: currentTick, charge: system.list[shrineIndex].charge, charging: system.charging});
        const p = system.prompt(x, z);
        prompts.push({tick: currentTick, index: p ? p.index : -1, kind: p ? p.kind : '', cost: p ? p.cost : 0});
      }
    });
    return {seed: 'MAMPORRO', shrineIndex, pathX: positions.map((p) => p[0]), pathZ: positions.map((p) => p[1]), log, charge, prompts};
  })();

  // ---------------------------------------------------------------- partidas integradas
  // Ruta: interactuables (sin el armario) en orden de vecino más cercano desde el inicio.
  // Cada tick: dirigirse al punto actual; al llegar (< 1,5 m) se pulsa interactuar una
  // vez y se pasa al siguiente (en los santuarios se espera a cargarlo o 700 ticks);
  // si en 1800 ticks no se llega, se pasa al siguiente. Giro de cámara = dirección de marcha.
  const tour = (spots, portalFirst, skipTotems) => {
    const left = spots.map((s, i) => ({...s, index: i})).filter((s) => portalFirst ? s.kind === 'portal' : s.kind !== 'portal' && !(skipTotems && s.kind === 'totem'));
    const order = [];
    let x = 0;
    let z = 0;
    while (left.length > 0) {
      let best = 0;
      let bestD = Infinity;
      left.forEach((s, i) => { const d = Math.hypot(s.x - x, s.z - z); if (d < bestD) { bestD = d; best = i; } });
      const [s] = left.splice(best, 1);
      order.push({index: s.index, kind: s.kind, x: s.x, z: s.z});
      x = s.x;
      z = s.z;
    }
    return order;
  };
  const integrated = (input) => {
    const data = generateWorldData(input.seed);
    const events = [];
    let tickNow = 0;
    const counters = {spawned: 0, killed: 0, shots: 0, chests: 0, items: 0, levelUps: 0, playerHits: 0};
    const fx = {...NO_EFFECTS,
      notice: (n) => events.push({tick: tickNow, kind: n.kind, detail: String(n.key ?? n.enemy ?? n.missing ?? '')}),
      enemySpawned: () => counters.spawned++, enemyKilled: () => counters.killed++, enemyShot: () => counters.shots++,
      chestOpened: () => { counters.chests++; events.push({tick: tickNow, kind: 'chestOpened', detail: ''}); },
      itemGained: (item) => { counters.items++; events.push({tick: tickNow, kind: 'item', detail: item.id}); },
      levelUp: (level) => { counters.levelUps++; events.push({tick: tickNow, kind: 'levelUp', detail: String(level)}); },
      bossSpawned: () => events.push({tick: tickNow, kind: 'bossSpawned', detail: ''}),
      playerHit: () => counters.playerHits++,
      revive: () => events.push({tick: tickNow, kind: 'revive', detail: ''}),
    };
    const run = new Run(data.collision, input.seed, CHARACTERS[input.character], fx, {minutes: input.minutes, interactables: data.interactables});
    run.invincible = input.invincible;
    if (input.revealPortal) run.interactables.reveal('portal');
    const body = new physics.PlayerBody();
    body.placeAt(0, data.heightfield.heightAt(0, 0), 0);
    const route = tour(data.interactables, input.portalFirst, input.skipTotems);
    let target = 0;
    let dwell = 0;
    let finishedTick = 0;
    let victoryDelay = 1.6;
    const detail = [];
    const totals = [];
    for (let tick = 1; tick <= input.ticks; tick++) {
      tickNow = tick;
      let mx = 0;
      let mz = 0;
      let interact = false;
      const wp = route[target];
      if (wp) {
        const dx = wp.x - body.x;
        const dz = wp.z - body.z;
        const d = Math.hypot(dx, dz);
        dwell++;
        if (d < 1.5) {
          const item = run.interactables.list[wp.index];
          if (wp.kind === 'shrine') {
            if (item.used || dwell > 700) { target++; dwell = 0; }
          } else {
            interact = true;
            target++;
            dwell = 0;
          }
        } else if (dwell > 1800) {
          target++;
          dwell = 0;
        } else {
          mx = dx / d;
          mz = dz / d;
        }
      }
      const yaw = mx !== 0 || mz !== 0 ? Math.atan2(-mx, -mz) : 0;
      const moveSpeed = PLAYER_BASE_STATS.moveSpeed * run.stats.moveSpeed;
      crowd.stepPlayerInCrowd(body, {moveX: mx, moveZ: mz, jumpPressed: false, jumpHeld: false, slidePressed: false, slideHeld: false}, data.collision, PLAYER_TUNING, moveSpeed, run.crowdSlow, DT);
      if (interact) run.interact();
      run.update(DT, body, yaw);
      if (run.dead) { finishedTick = tick; events.push({tick, kind: 'defeat', detail: ''}); break; }
      if (run.victory) {
        victoryDelay -= DT;
        if (victoryDelay <= 0) { finishedTick = tick; events.push({tick, kind: 'victory', detail: ''}); break; }
      } else {
        while (run.openChoice()) run.choose(0);
      }
      if (tick <= input.detailTicks && tick % 10 === 0) {
        detail.push({tick, x: body.x, y: body.y, z: body.z, hp: run.hp, level: run.level, xp: run.progress.xp, gold: run.gold, kills: run.kills, alive: run.enemies.count,
          target, ex: f32(run.enemies.x, Math.min(8, run.enemies.count)), ez: f32(run.enemies.z, Math.min(8, run.enemies.count))});
      }
      if (tick % 60 === 0) totals.push({tick, kills: run.kills, alive: run.enemies.count, level: run.level, gold: run.gold, chests: run.interactables.chestsOpened, hp: run.hp, time: run.time, swarm: run.swarm, spawned: counters.spawned});
    }
    return {...input, route, finishedTick, events, detail, totals, counters,
      final: {time: run.time, kills: run.kills, level: run.level, gold: run.gold, chests: run.interactables.chestsOpened, cheated: run.cheated, victory: run.victory, dead: run.dead,
        weapons: run.weapons.map((w) => ({id: w.def.id, level: w.level, damage: w.totalDamage, kills: w.kills})), items: run.items.map((s) => ({id: s.def.id, count: s.count}))}};
  };
  const runs = [
    {name: 'remedios-5min-invencible', seed: 'MAMPORRO', character: 'remedios', minutes: 5, invincible: true, portalFirst: false, revealPortal: false, skipTotems: false, ticks: (300 + 40) * 60, detailTicks: 7200},
    {name: 'remedios-10min-sin-totems', seed: 'U3-MUNDO', character: 'remedios', minutes: 10, invincible: false, portalFirst: false, revealPortal: false, skipTotems: true, ticks: 7200, detailTicks: 7200},
    {name: 'baguette-15min-derrota', seed: 'U3-MUNDO', character: 'baguette', minutes: 15, invincible: false, portalFirst: false, revealPortal: false, skipTotems: false, ticks: 7200, detailTicks: 7200},
    {name: 'armario-jefe-victoria', seed: 'HITO6QA', character: 'remedios', minutes: 10, invincible: true, portalFirst: true, revealPortal: true, skipTotems: false, ticks: 60 * 60 * 6, detailTicks: 3600},
  ].map(integrated);

  const value = JSON.stringify({formatVersion: 1, source: BASE, enemyTypes: enemiesData.ENEMY_LIST.map((e) => e.id), simplex: simplexOut, worlds, physics: physicsOut,
    director, spawns: spawnCase, chestCosts, interactables: interactablesCase, runs}) + '\n';
  if (process.argv.includes('--export-once')) {
    await fs.writeFile(TARGET, value, {flag: 'wx'});
    console.log(`Creado ${TARGET} (${value.length} bytes) desde la web aprobada.`);
  } else if (process.argv.includes('--dry-run')) {
    console.log(`Generado en memoria (${value.length} bytes); no se escribe nada.`);
    await fs.writeFile(process.argv[process.argv.indexOf('--dry-run') + 1], value);
  } else {
    const actual = await fs.readFile(TARGET, 'utf8');
    if (actual.replace(/\r\n/g, '\n') !== value) throw new Error(`${TARGET} no coincide con la web aprobada: revisar, no sobrescribir.`);
    console.log(`${TARGET} coincide con la web aprobada.`);
  }
} finally {
  await server.close();
}
