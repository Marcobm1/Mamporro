// Comprueba el corpus congelado; no sustituye los futuros tests del importador.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const read = name => JSON.parse(readFileSync(new URL(`../unity/Docs/Reference/${name}`, import.meta.url), 'utf8'));
const baseline = read('baseline.json');
const reference = read('u4-progress.json');
const corpus = read('u4-import-cases.json');
const saves = new Map(reference.saves.map(s => [s.id, s]));
const meta = baseline.catalog.meta;
const weaponIds = baseline.catalog.weapons.map(x => x.id);
const itemIds = baseline.catalog.items.map(x => x.id);
const missionIds = meta.MISSIONS.map(x => x.id);
const copy = value => JSON.parse(JSON.stringify(value));

function patch(object, edits) {
  for (const edit of edits) {
    assert(['replace', 'remove'].includes(edit.op));
    const keys = edit.path.split('/').slice(1);
    assert(keys.length > 0 && keys.every(k => k && !['__proto__', 'constructor', 'prototype'].includes(k)));
    const key = keys.pop();
    const parent = keys.reduce((o, k) => { assert(o && typeof o === 'object' && Object.hasOwn(o, k)); return o[k]; }, object);
    if (edit.op === 'remove') { assert(Object.hasOwn(parent, key)); delete parent[key]; }
    else parent[key] = copy(edit.value);
  }
  return object;
}

function integer(value, max) {
  assert(Number.isInteger(value) && value >= 0 && value <= max);
}
function collection(values, ids) {
  assert(Array.isArray(values));
  assert.equal(new Set(values).size, values.length);
  assert(values.every(v => ids.includes(v)));
}
function validateCandidate(data) {
  const s = data.settings, m = data.meta;
  assert.deepEqual(Object.keys(s).sort(), Object.keys(saves.get('new-es').result.data.settings).sort());
  assert(['es', 'en'].includes(s.language));
  assert([240, 360, 480].includes(s.renderHeight));
  assert([5, 10, 15].includes(s.runMinutes));
  for (const key of ['musicVolume', 'effectsVolume']) assert(Number.isFinite(s[key]) && s[key] >= 0 && s[key] <= 1);
  assert(Number.isFinite(s.mouseSensitivity) && s.mouseSensitivity >= 0.2 && s.mouseSensitivity <= 3);
  for (const key of ['muted', 'reducedParticles', 'cameraShake', 'flashes', 'vertexSnap', 'dithering', 'slideWithCtrl', 'showFps']) assert.equal(typeof s[key], 'boolean');
  assert.deepEqual(Object.keys(m).sort(), ['coins', 'selected', 'characters', 'weapons', 'items', 'extras', 'missions', 'completed', 'lastRun'].sort());
  integer(m.coins, 1000000000);
  collection(m.characters, ['remedios', 'baguette']);
  collection(m.weapons, weaponIds); collection(m.items, itemIds); collection(m.completed, missionIds);
  assert(m.characters.includes('remedios') && m.characters.includes(m.selected));
  assert(meta.INITIAL_WEAPONS.every(id => m.weapons.includes(id)));
  assert(meta.INITIAL_ITEMS.every(id => m.items.includes(id)));
  assert.deepEqual(Object.keys(m.extras).sort(), ['banishes', 'rerolls', 'skips']);
  for (const value of Object.values(m.extras)) integer(value, meta.EXTRA_PRICES.length);
  assert.deepEqual(Object.keys(m.missions).sort(), [...missionIds].sort());
  for (const mission of meta.MISSIONS) {
    integer(m.missions[mission.id], mission.target);
    assert.equal(m.completed.includes(mission.id), m.missions[mission.id] === mission.target);
    if (m.completed.includes(mission.id) && mission.unlock) {
      const target = mission.unlock.kind === 'weapon' ? m.weapons : m.items;
      assert(target.includes(mission.unlock.id));
    }
  }
  assert.equal(typeof m.lastRun, 'string'); assert(m.lastRun.length <= 100);
}

test('procedencia: mismas fuentes aprobadas que U0, sin esperados de C#', () => {
  assert.equal(reference.baseCommit, baseline.baseCommit);
  assert.equal(reference.formatVersion, 1);
  for (const [file, hash] of Object.entries(reference.sourceHashes)) assert.equal(hash, baseline.sourceHashes[file], file);
  assert.equal(corpus.reference, 'u4-progress.json');
});

test('todos los guardados web congelados satisfacen el contrato de progreso', () => {
  assert.equal(saves.size, reference.saves.length);
  for (const save of saves.values()) validateCandidate(save.result.data);
  assert.equal(saves.get('v1-options').result.data.meta.coins, 0);
  assert.equal(saves.get('v2-partial').result.data.meta.coins, 70);
  assert.equal(saves.get('v3-all').result.data.meta.selected, 'baguette');
});

test('secuencias web: liquidación única, debug, abandono, compras y límites', () => {
  const find = id => reference.scenarios.find(s => s.id === id);
  const repeated = find('defeat-repeat-victory-repeat').steps;
  assert.equal(repeated[0].result.total, 70);
  assert.equal(repeated[1].result.total, 0);
  assert.deepEqual(repeated[0].state, repeated[1].state);
  assert.deepEqual(repeated[2].state, repeated[3].state);
  const debug = find('debug-abandon-empty-id');
  for (const step of debug.steps) assert.deepEqual(step.state, debug.initial);
  const life = find('victory-life-tome').steps[0];
  assert(!life.state.completed.includes('noLife'));
  const all = find('all-purchases-extras-and-missions').steps;
  assert.equal(all.filter(s => s.operation.kind === 'extra' && s.result === false).length, 3);
  assert.equal(all.at(-1).state.completed.length, 8);
  assert.equal(find('reward-boundaries-and-cap').steps.at(-1).state.coins, 1000000000);
});

test('casos contractuales: recetas válidas y ningún candidato recuperable contradictorio', () => {
  assert.equal(new Set(corpus.cases.map(c => c.id)).size, corpus.cases.length);
  for (const c of corpus.cases) {
    assert(['valid', 'recovery', 'fatal'].includes(c.expected.outcome), c.id);
    if (c.input.base) {
      const save = saves.get(c.input.base); assert(save, c.id);
      let input = JSON.parse(save.raw);
      if (c.input.encoding !== 'web') {
        const { settings, meta } = save.result.data;
        input = c.input.encoding === 'transfer'
          ? { format: 'mamporro.progress', version: 1, source: { platform: 'web', saveVersion: 3 }, progress: copy({ settings, meta }) }
          : { format: 'mamporro.unity-save', version: 1, progress: copy({ settings, meta }) };
      }
      const original = copy(input); patch(input, c.input.edits);
      if (c.input.edits.length) assert.notDeepEqual(input, original, c.id);
    }
    if (c.expected.outcome === 'fatal') {
      assert(!Object.hasOwn(c.expected, 'candidate'), c.id);
      assert(c.expected.issues.some(i => i.action === 'reject'), c.id);
    } else {
      const candidate = c.expected.candidate;
      assert(saves.has(candidate.base), c.id);
      validateCandidate(patch(copy(saves.get(candidate.base).result.data), candidate.edits));
      if (c.expected.outcome === 'recovery') assert(c.expected.issues.length > 0, c.id);
    }
  }
});

test('matriz mínima: versiones, progreso crítico, recuperación y presupuestos', () => {
  const ids = new Set(corpus.cases.map(c => c.id));
  for (const id of ['new-local', 'v1-options', 'v2-partial', 'v3-partial', 'v3-all', 'future', 'corrupt',
    'no-version', 'duplicate-key', 'v1-with-meta', 'claimed-mission-below-target', 'mission-target-unclaimed',
    'claimed-without-unlock', 'unknown-id', 'duplicate-id', 'unknown-field', 'settings-missing', 'import-repeat',
    'transfer-origin-v1-is-already-normalized', 'utf8-invalid', 'numeric-overflow', 'utf8-bom']) assert(ids.has(id), id);
  assert.equal(corpus.cases.filter(c => c.id.startsWith('limit-')).length, 7);
  assert(corpus.cases.filter(c => c.expected.outcome === 'fatal').length > 50);
});
