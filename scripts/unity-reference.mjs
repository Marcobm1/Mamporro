// Exporta la referencia aprobada para comparar el futuro port C#.
// Vite carga los módulos TS sin añadir dependencias ni tocar el juego.
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { createServer } from 'vite';

const root = fileURLToPath(new URL('../', import.meta.url));
const target = new URL('../unity/Docs/Reference/baseline.json', import.meta.url);
const baseCommit = '0505b1690656d15188860157612455639820fe1f';
const write = process.argv.includes('--write');
// Impide atribuir resultados nuevos al commit aprobado por accidente.
execFileSync('git', ['diff', '--exit-code', baseCommit, '--', 'src', 'package-lock.json'], { cwd: root, stdio: 'pipe' });
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' });
try {
  const load = path => server.ssrLoadModule(`/src/${path}.ts`);
  const modules = await Promise.all([
    load('core/rng'), load('systems/damage'), load('systems/progression'), load('systems/Director'),
    load('save/schema'), load('systems/meta'), load('world/World'), load('data/run'),
    load('data/characters'), load('data/weapons'), load('data/tomes'), load('data/items'),
    load('data/enemies'), load('data/meta'), load('data/waves'), load('data/config'),
    load('i18n/es'), load('i18n/en'), load('audio/synth'), load('data/audio'),
  ]);
  const [rng, damage, xp, director, save, meta, world, runData, characters, weapons, tomes, items, enemies, metaData, waves, config, es, en, synth, audio] = modules;
  const seeds = ['HITO6QA', 'PULIDO-REFERENCIA', 'MAMPORRO', ''];
  const rngVectors = seeds.map(seed => {
    const generator = new rng.Rng(seed);
    const derived = generator.derive('terrain');
    return { seed, hash: rng.hashString(seed), nextU32: Array.from({ length: 16 }, () => generator.nextU32()),
      terrainU32: Array.from({ length: 8 }, () => derived.nextU32()) };
  });
  const progressed = meta.defaultMeta();
  progressed.coins = 500;
  meta.purchase(progressed, 'baguette');
  progressed.selected = 'baguette';
  const rawSaves = [
    { id: 'new', raw: null }, { id: 'corrupt', raw: '{broken' },
    { id: 'v1-options', raw: JSON.stringify({ version: 1, settings: { language: 'en', musicVolume: 0.25, runMinutes: 5 } }) },
    { id: 'v2-progress', raw: JSON.stringify({ version: 2, settings: { language: 'es' }, meta: progressed }) },
    { id: 'v3-progress', raw: JSON.stringify({ version: 3, settings: save.defaultSettings('en'), meta: progressed }) },
    { id: 'future', raw: JSON.stringify({ version: 999, settings: {}, meta: progressed }) },
  ];
  const constantData = module => Object.fromEntries(Object.entries(module).filter(([key, value]) => /^[A-Z_]+$/.test(key) && typeof value !== 'function'));
  const catalog = {
    characters: characters.CHARACTERS, weapons: weapons.WEAPON_LIST, tomes: tomes.TOME_LIST,
    items: items.ITEM_LIST, enemies: enemies.ENEMY_LIST, meta: constantData(metaData),
    waves: constantData(waves), worldAndPlayer: constantData(config), run: constantData(runData),
    itemEffects: items.ITEM_EFFECTS, boss: enemies.BOSS_CONFIG,
  };
  assert.equal(catalog.weapons.length, 6);
  assert.equal(catalog.tomes.length, 8);
  assert.equal(catalog.items.length, 12);
  assert.equal(catalog.enemies.length, 6);
  assert.deepEqual(Object.keys(es.es).sort(), Object.keys(en.en).sort());
  const files = execFileSync('git', ['ls-tree', '-r', '--name-only', baseCommit, '--', 'src', 'package-lock.json'], { cwd: root, encoding: 'utf8' }).trim().split('\n');
  const sourceHashes = {};
  for (const file of files) sourceHashes[file] = createHash('sha256').update(await readFile(new URL(`../${file}`, import.meta.url))).digest('hex');
  const worldSamples = seeds.slice(0, 3).map(seed => {
    const data = world.generateWorldData(seed);
    return { seed, sites: data.sites, interactables: data.interactables,
      heights: [-100, -50, 0, 50, 100].flatMap(x => [-100, -50, 0, 50, 100].map(z => ({ x, z, height: data.heightfield.heightAt(x, z) }))) };
  });
  const pcmSummary = samples => {
    let sumSquares = 0, peak = 0;
    for (const sample of samples) { sumSquares += sample * sample; peak = Math.max(peak, Math.abs(sample)); }
    return { frames: samples.length, peak, sumSquares, first32: Array.from(samples.slice(0, 32)) };
  };
  const reference = {
    formatVersion: 1, baseCommit, sourceHashes,
    comparison: { rng: 'Exact uint32; UTF-16, overflow unchecked, unsigned shifts and 12 warmup steps.',
      doubles: 'Absolute tolerance 1e-9 for formulas; preserve JS rounding for XP.',
      world: 'Baseline reference only: agree tolerances before comparing float meshes or changing terrain.',
      audio: 'Numeric reference; allow platform floating point tolerance, do not use PCM hashes as gameplay gates.' },
    catalog, translations: { es: es.es, en: en.en }, rng: rngVectors,
    formulas: {
      xp: [1, 2, 5, 10, 20, 50, 100].map(level => ({ level, needed: xp.xpToNextLevel(level) })),
      damage: [0, 0.05, 1, 1.5, 2.5].flatMap(chance => [0, 0.049, 0.5, 0.99].map(random => ({ base: 10, chance, multiplier: 2, random, result: damage.rollDamage(10, chance, 2, () => random) }))),
      armor: [-10, 0, 50, 100, 200].map(armor => ({ damage: 20, armor, result: damage.mitigate(20, armor) })),
      chest: Array.from({ length: 14 }, (_, opened) => ({ opened, cost: runData.chestCost(opened) })),
      director: [5, 10, 15].flatMap(minutes => [0, 0.25, 0.5, 1, 1.1].map(fraction => {
        const time = minutes * 60 * fraction;
        const value = new director.Director(minutes);
        return { minutes, time, result: value.spawnParams(time, { rate: 1, hp: 1, gold: 1 }, {}) };
      })),
    },
    saves: rawSaves.map(({ id, raw }) => ({ id, raw, result: save.parseSave(raw, 'es') })),
    worlds: worldSamples,
    audio: { sampleRate: audio.AUDIO_CONFIG.sampleRate, sounds: Object.fromEntries(Object.entries(audio.SOUNDS).map(([id, def]) => [id, pcmSummary(synth.synthSound(def))])),
      normal: pcmSummary(synth.synthMusic(false)), intense: pcmSummary(synth.synthMusic(true)) },
  };
  const json = JSON.stringify(reference, null, 2) + '\n';
  if (write) {
    await mkdir(new URL('../unity/Docs/Reference/', import.meta.url), { recursive: true });
    await writeFile(target, json);
    if (process.argv.includes('--media')) {
      // Muestras WAV PCM16 de dos segundos, originales y reproducibles.
      for (const [name, intense] of [['music-normal', false], ['music-intense', true]]) {
        const pcm = synth.synthMusic(intense).slice(0, audio.AUDIO_CONFIG.sampleRate * 2);
        const wav = Buffer.alloc(44 + pcm.length * 2);
        wav.write('RIFF', 0); wav.writeUInt32LE(wav.length - 8, 4); wav.write('WAVEfmt ', 8);
        wav.writeUInt32LE(16, 16); wav.writeUInt16LE(1, 20); wav.writeUInt16LE(1, 22);
        wav.writeUInt32LE(audio.AUDIO_CONFIG.sampleRate, 24); wav.writeUInt32LE(audio.AUDIO_CONFIG.sampleRate * 2, 28);
        wav.writeUInt16LE(2, 32); wav.writeUInt16LE(16, 34); wav.write('data', 36); wav.writeUInt32LE(pcm.length * 2, 40);
        for (let i = 0; i < pcm.length; i++) wav.writeInt16LE(Math.round(Math.max(-1, Math.min(1, pcm[i])) * 32767), 44 + i * 2);
        await writeFile(new URL(`../unity/Docs/Reference/${name}.wav`, import.meta.url), wav);
      }
    }
    console.log('Referencia U0 exportada desde el código aprobado.');
  } else {
    assert.equal(await readFile(target, 'utf8'), json, 'La referencia difiere: no regenerar para ocultar una regresión.');
    const manifest = JSON.parse(await readFile(new URL('../unity/Docs/Reference/media.json', import.meta.url), 'utf8'));
    for (const [name, sha] of Object.entries(manifest.files)) {
      const bytes = await readFile(new URL(`../unity/Docs/Reference/${name}`, import.meta.url));
      assert.equal(createHash('sha256').update(bytes).digest('hex'), sha, `Medio alterado: ${name}`);
    }
    console.log('Referencia U0 verificada: datos y medios coinciden.');
  }
} finally { await server.close(); }
