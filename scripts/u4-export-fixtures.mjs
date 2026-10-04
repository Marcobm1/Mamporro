// Entradas de integración generadas por el exportador real; no son nuevos esperados.
import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { createServer } from 'vite';
const root = fileURLToPath(new URL('../', import.meta.url));
const output = new URL('../unity/TestResults/U4/Exports/', import.meta.url);
const corpus = JSON.parse(await readFile(new URL('../unity/Docs/Reference/u4-progress.json', import.meta.url), 'utf8'));
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' });
try {
  const { prepareProgressExport } = await server.ssrLoadModule('/src/save/exportProgress.ts');
  await mkdir(output, { recursive: true });
  for (const fixture of corpus.saves) {
    const input = fixture.raw === null ? fixture.result.data : JSON.parse(fixture.raw);
    const before = JSON.stringify(input), result = prepareProgressExport(() => input, fixture.language);
    assert(result.json, JSON.stringify(result.issues));
    const transfer = JSON.parse(result.json);
    assert.deepEqual(transfer.progress, { settings: fixture.result.data.settings, meta: fixture.result.data.meta });
    assert.equal(JSON.stringify(input), before);
    assert.equal(prepareProgressExport(() => input, fixture.language).json, result.json);
    await writeFile(new URL(`${fixture.id}.json`, output), result.json, 'utf8');
  }
  console.log(`${corpus.saves.length} transferencias web verificadas contra el corpus congelado en ${fileURLToPath(output)}`);
} finally { await server.close(); }
