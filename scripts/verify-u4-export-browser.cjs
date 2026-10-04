// Navegador real, descargas reales. Playwright solo es herramienta externa de QA.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const corpus = require(path.join(root, 'unity/Docs/Reference/u4-progress.json'));
const output = path.join(root, 'unity/TestResults/U4/BrowserExport');
fs.mkdirSync(output, { recursive: true });
const base = process.env.MAMPORRO_URL || 'http://127.0.0.1:5173';
(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: process.env.MAMPORRO_BROWSER || undefined,
    args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const errors = [], report = [];
  try {
    for (const language of ['es', 'en']) {
      const expected = structuredClone(corpus.saves.find(s => s.id === (language === 'es' ? 'v3-partial' : 'v3-all')).result.data);
      expected.settings.language = language;
      const context = await browser.newContext({ locale: language, acceptDownloads: true, viewport: { width: 1280, height: 900 } });
      await context.addInitScript(data => localStorage.setItem('mamporro.save', JSON.stringify(data)), expected);
      const page = await context.newPage();
      page.on('pageerror', error => errors.push(error.message));
      await page.route('**/src/main.ts', async route => {
        const response = await route.fetch(), text = await response.text();
        assert(text.includes('void game.boot();'));
        await route.fulfill({ response, body: text.replace('void game.boot();', 'window.__QA_GAME__ = game; void game.boot();') });
      });
      await page.goto(`${base}/?test`, { waitUntil: 'networkidle' });
      await page.waitForFunction(() => window.__MAMPORRO__?.state() === 'title', { timeout: 60000 });
      await page.evaluate(() => window.__QA_GAME__.loop.stop());
      await page.getByRole('button', { name: language === 'es' ? 'Opciones' : 'Options', exact: true }).click();
      const action = () => page.getByRole('button', { name: language === 'es' ? 'Exportar progreso para Unity' : 'Export progress for Unity', exact: true });
      const downloadButton = () => page.getByRole('button', { name: language === 'es' ? 'Descargar progreso' : 'Download progress', exact: true });
      const state = () => page.evaluate(() => {
        const g = window.__QA_GAME__;
        return { data: structuredClone(g.save.data), storage: { ...localStorage }, state: g.state,
          run: g.run === null ? null : { time: g.run.time, hp: g.run.hp, gold: g.run.gold }, runSettled: g.runSettled };
      });
      const before = await state(); let first;
      for (let i = 0; i < 2; i++) {
        await action().click();
        const event = page.waitForEvent('download'); await downloadButton().click(); const download = await event;
        assert.equal(download.suggestedFilename(), 'mamporro-progreso.json');
        const destination = path.join(output, `${language}-${i}.json`);
        await download.saveAs(destination); assert.equal(await download.failure(), null);
        const bytes = fs.readFileSync(destination, 'utf8'), transfer = JSON.parse(bytes);
        assert.deepEqual(transfer, { format: 'mamporro.progress', version: 1,
          source: { platform: 'web', saveVersion: 3 }, progress: { settings: expected.settings, meta: expected.meta } });
        if (i === 0) first = bytes; else assert.equal(bytes, first);
        assert.deepEqual(await state(), before);
      }
      await page.screenshot({ path: path.join(output, `${language}-export.png`), fullPage: true });
      // No almacenamiento disponible: se conserva la vía explícita desde memoria.
      await page.evaluate(() => {
        window.__QA_GAME__.save.canSave = false;
        window.__QA_STORAGE__ = Object.getOwnPropertyDescriptor(window, 'localStorage');
        Object.defineProperty(window, 'localStorage', { configurable: true, get() { throw new Error('QA bloqueado'); } });
      });
      await action().click();
      assert(await page.getByText(language === 'es' ? /Se exportará la copia cargada/ : /The loaded copy in memory/).isVisible());
      const memoryEvent = page.waitForEvent('download'); await downloadButton().click();
      const memory = await memoryEvent; await memory.saveAs(path.join(output, `${language}-memory.json`));
      assert.equal(fs.readFileSync(path.join(output, `${language}-memory.json`), 'utf8'), first);
      await page.evaluate(() => { Object.defineProperty(window, 'localStorage', window.__QA_STORAGE__); window.__QA_GAME__.save.canSave = true; });
      assert.deepEqual(await state(), before);
      // Fallo de preparación de descarga visible; ningún cambio del save.
      await page.evaluate(() => { window.__QA_URL__ = URL.createObjectURL; URL.createObjectURL = () => { throw new Error('QA Blob'); }; });
      await downloadButton().click();
      assert(await page.getByText(language === 'es' ? /No se pudo preparar la descarga/ : /The download could not be prepared/).isVisible());
      await page.evaluate(() => { URL.createObjectURL = window.__QA_URL__; });
      assert.deepEqual(await state(), before);
      // Progreso crítico inválido: no hay botón de descarga ni saneamiento del estado.
      await page.evaluate(() => { window.__QA_GAME__.save.data.meta.coins = -1; });
      const corrupt = await state(); await action().click(); assert.equal(await downloadButton().count(), 0);
      assert.deepEqual(await state(), corrupt);
      report.push({ language, downloads: 3, repeatedBytes: true, unchanged: true, memoryWithoutStorage: true, failuresReported: true });
      await context.close();
    }
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify({ date: new Date().toISOString(), browser: browser.version(), report, errors }, null, 2));
    console.log(JSON.stringify(report));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
