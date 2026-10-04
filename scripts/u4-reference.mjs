// Referencia independiente del port: solo la web aprobada calcula estos resultados.
// --export-once crea un archivo nuevo; sin argumentos compara y nunca escribe.
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { createServer } from 'vite';
import { referenceBytes } from './unity-reference-bytes.mjs';

const root = fileURLToPath(new URL('../', import.meta.url));
const target = new URL('../unity/Docs/Reference/u4-progress.json', import.meta.url);
const baseCommit = '0505b1690656d15188860157612455639820fe1f';
assert(process.argv.slice(2).every(x => x === '--export-once'), 'Argumento desconocido');
execFileSync('git', ['diff', '--exit-code', baseCommit, '--', 'src', 'package-lock.json'], { cwd: root, stdio: 'pipe' });
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' });
try {
  const save = await server.ssrLoadModule('/src/save/schema.ts');
  const meta = await server.ssrLoadModule('/src/systems/meta.ts');
  const definitions = await server.ssrLoadModule('/src/data/meta.ts');
  const copy = value => JSON.parse(JSON.stringify(value));
  const baseRun = { id: 'one', cheated: false, time: 300, kills: 400, chests: 3,
    shrines: 1, challenges: 0, level: 15, victory: false, usedLifeTome: false };
  const run = patch => ({ ...baseRun, ...patch });
  const scenarios = [];
  function scenario(id, coins, operations) {
    const state = meta.defaultMeta(); state.coins = coins;
    const initial = copy(state), steps = [];
    for (const operation of operations) {
      let result;
      switch (operation.kind) {
        case 'settle': result = meta.settleRun(state, operation.run); break;
        case 'purchase': result = meta.purchase(state, operation.id); break;
        case 'extra': result = meta.purchaseExtra(state, operation.action); break;
        // Game no llama a settleRun al abandonar; no inventar una regla de núcleo.
        case 'abandon': result = null; break;
        default: throw new Error('Operación desconocida');
      }
      steps.push({ operation, result: copy(result), state: copy(state) });
    }
    scenarios.push({ id, initial, steps });
    return copy(state);
  }
  const partial = scenario('defeat-repeat-victory-repeat', 0, [
    { kind: 'settle', run: run({}) }, { kind: 'settle', run: run({}) },
    { kind: 'settle', run: run({ id: 'two', kills: 600, chests: 7, shrines: 2, challenges: 1, level: 20, victory: true }) },
    { kind: 'settle', run: run({ id: 'two', victory: true }) },
  ]);
  scenario('debug-abandon-empty-id', 31, [
    { kind: 'settle', run: run({ cheated: true, victory: true }) },
    { kind: 'abandon' }, { kind: 'settle', run: run({ id: '' }) },
  ]);
  scenario('victory-life-tome', 0, [{ kind: 'settle', run: run({ victory: true, usedLifeTome: true }) }]);
  scenario('insufficient-and-repeated', 220, [
    { kind: 'purchase', id: 'unknown' }, { kind: 'purchase', id: 'baguette' },
    { kind: 'purchase', id: 'baguette' }, { kind: 'purchase', id: 'jersey' },
    { kind: 'extra', action: 'skips' },
  ]);
  scenario('reward-boundaries-and-cap', 999999995, [
    { kind: 'settle', run: run({ time: 14.9, kills: 19, level: 1, chests: 0, shrines: 0 }) },
    { kind: 'settle', run: run({ id: 'edge', time: 15, kills: 20 }) },
    { kind: 'settle', run: run({ id: 'long', time: 10000, kills: 0 }) },
  ]);
  const full = scenario('all-purchases-extras-and-missions', 5000, [
    ...definitions.SHOP.map(s => ({ kind: 'purchase', id: s.id })),
    ...['rerolls', 'skips', 'banishes'].flatMap(action => Array.from({ length: 4 }, () => ({ kind: 'extra', action }))),
    { kind: 'settle', run: run({ kills: 1000, chests: 10, shrines: 3, challenges: 1, level: 20, victory: true }) },
  ]);
  full.selected = 'baguette';
  const first = meta.defaultMeta(); meta.settleRun(first, run({}));
  const historical = {
    v1: { language: 'en', mouseSensitivity: 2, renderHeight: 240, vertexSnap: false, dithering: true,
      slideWithCtrl: true, showFps: true, runMinutes: 5 },
    v2: { musicVolume: 0.25, effectsVolume: 0.3, muted: true, language: 'en', mouseSensitivity: 2,
      renderHeight: 480, vertexSnap: false, dithering: false, slideWithCtrl: true, showFps: true, runMinutes: 15 },
  };
  const inputs = [
    { id: 'new-es', language: 'es', raw: null }, { id: 'new-en', language: 'en', raw: null },
    { id: 'v1-earliest', language: 'es', raw: JSON.stringify({ version: 1, settings: { language: 'es' } }) },
    { id: 'v1-options', language: 'es', raw: JSON.stringify({ version: 1, settings: historical.v1 }) },
    { id: 'v2-partial', language: 'es', raw: JSON.stringify({ version: 2, settings: historical.v2, meta: first }) },
    { id: 'v3-partial', language: 'es', raw: JSON.stringify({ version: 3, settings: save.defaultSettings('es'), meta: first }) },
    { id: 'v3-missions', language: 'es', raw: JSON.stringify({ version: 3, settings: save.defaultSettings('es'), meta: partial }) },
    { id: 'v3-all', language: 'es', raw: JSON.stringify({ version: 3, settings: save.defaultSettings('en'), meta: full }) },
  ];
  const files = ['src/save/schema.ts', 'src/systems/meta.ts', 'src/data/meta.ts', 'src/data/items.ts',
    'src/data/weapons.ts', 'src/data/characters.ts', 'src/core/Game.ts', 'package-lock.json'];
  const sourceHashes = {};
  for (const file of files) sourceHashes[file] = createHash('sha256').update(referenceBytes(file, await readFile(new URL(`../${file}`, import.meta.url)))).digest('hex');
  const result = { formatVersion: 1, baseCommit, sourceHashes, scenarios,
    saves: inputs.map(input => ({ ...input, result: copy(save.parseSave(input.raw, input.language)) })) };
  const json = JSON.stringify(result, null, 2) + '\n';
  if (process.argv.includes('--export-once')) {
    await writeFile(target, json, { flag: 'wx' });
    console.log('U4: referencia creada una sola vez desde la web aprobada.');
  } else {
    assert.deepEqual(JSON.parse(await readFile(target, 'utf8')), result);
    console.log(`U4: ${result.saves.length} guardados y ${scenarios.length} secuencias meta coinciden con la referencia congelada.`);
  }
} finally { await server.close(); }
