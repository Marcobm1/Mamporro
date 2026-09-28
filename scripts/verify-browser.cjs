// QA del hito 6 contra Vite en desarrollo. No forma parte del juego ni del build.
// Requiere Playwright; MAMPORRO_BROWSER permite indicar un Chromium instalado.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const base = process.env.MAMPORRO_URL || 'http://127.0.0.1:5173';
const output = process.env.MAMPORRO_QA || path.resolve('qa-results');
fs.mkdirSync(output, { recursive: true });
(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: process.env.MAMPORRO_BROWSER || undefined,
    args: ['--no-sandbox', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const errors = [];
  const report = [];
  try {
    for (const language of ['es', 'en']) {
      const context = await browser.newContext({ locale: language, viewport: { width: 1280, height: 720 } });
      const page = await context.newPage();
      page.on('pageerror', e => errors.push(e.message));
      page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      // Instrumentación efímera: no se añade acceso a Game al código distribuido.
      await page.route('**/src/main.ts', async route => {
        const response = await route.fetch();
        const text = await response.text();
        assert(text.includes('void game.boot();'), 'Punto de instrumentación cambiado');
        await route.fulfill({ response, body: text.replace('void game.boot();', 'window.__QA_GAME__ = game; void game.boot();') });
      });
      await page.goto(`${base}/?test`, { waitUntil: 'networkidle' });
      await page.waitForFunction(() => window.__MAMPORRO__?.state() === 'title', { timeout: 60000 });
      await page.evaluate(() => window.__QA_GAME__.loop.stop());
      console.log(`[QA] ${language}: menú cargado`);
      assert.equal(await page.evaluate(() => window.__MAMPORRO__.audio().state), 'locked');
      await page.screenshot({ path: path.join(output, `${language}-menu-1280.png`) });
      await page.getByRole('button', { name: language === 'es' ? 'Opciones' : 'Options', exact: true }).click();
      await page.waitForFunction(() => window.__MAMPORRO__.audio().state === 'running');
      const names = language === 'es' ? ['Reducir partículas', 'Sacudidas de cámara', 'Destellos de daño'] : ['Reduce particles', 'Camera shake', 'Damage flashes'];
      for (const name of names) await page.getByRole('button', { name, exact: true }).click();
      await page.screenshot({ path: path.join(output, `${language}-opciones-1280.png`) });
      console.log(`[QA] ${language}: opciones`);
      const saved = await page.evaluate(() => JSON.parse(localStorage.getItem('mamporro.save')));
      assert.equal(saved.version, 3);
      assert.equal(saved.settings.reducedParticles, true);
      assert.equal(saved.settings.cameraShake, false);
      assert.equal(saved.settings.flashes, false);
      await page.getByRole('button', { name: language === 'es' ? 'Silenciar audio' : 'Mute audio', exact: true }).click();
      assert.equal(await page.evaluate(() => window.__MAMPORRO__.audio().tracks), 0);
      await page.getByRole('button', { name: language === 'es' ? 'Silenciar audio' : 'Mute audio', exact: true }).click();
      assert.equal(await page.evaluate(() => window.__MAMPORRO__.audio().tracks), 1);
      const audio = await page.evaluate(async () => {
        const game = window.__QA_GAME__;
        const { SOUNDS } = await import('/src/data/audio.ts');
        const { synthMusic } = await import('/src/audio/synth.ts');
        for (let i = 0; i < 1000; i++) for (const id of Object.keys(SOUNDS)) game.audio.play(id);
        const peakVoices = game.audio.stats.voices;
        game.audio.setHidden(true);
        await new Promise(resolve => setTimeout(resolve, 100));
        const hidden = game.audio.stats;
        game.audio.setHidden(false);
        await new Promise(resolve => setTimeout(resolve, 100));
        const resumed = game.audio.stats;
        // Render de PCM en el grafo real WebAudio, sin depender de altavoces.
        const offline = new OfflineAudioContext(1, 22050, 22050);
        const pcm = synthMusic(false);
        const buffer = offline.createBuffer(1, pcm.length, 22050);
        buffer.getChannelData(0).set(pcm);
        const source = offline.createBufferSource(); source.buffer = buffer; source.connect(offline.destination); source.start();
        const result = await offline.startRendering();
        let energy = 0; for (const sample of result.getChannelData(0)) energy += sample * sample;
        return { peakVoices, hidden, resumed, energy };
      });
      assert(audio.peakVoices <= 16); assert.equal(audio.hidden.state, 'suspended'); assert.equal(audio.hidden.voices, 0);
      assert.equal(audio.resumed.state, 'running'); assert(audio.energy > 1);
      console.log(`[QA] ${language}: audio correcto`);
      await page.reload({ waitUntil: 'networkidle' });
      await page.waitForFunction(() => window.__MAMPORRO__?.state() === 'title');
      await page.evaluate(() => window.__QA_GAME__.loop.stop());
      assert.equal(await page.evaluate(() => window.__QA_GAME__.save.settings.reducedParticles), true);
      await page.keyboard.press('Space');
      await page.evaluate(() => {
        const g = window.__QA_GAME__; g.loop.stop(); window.__MAMPORRO__.start('HITO6-QA');
        g.run.invincible = true;
        for (let i = 0; i < 6; i++) window.__MAMPORRO__.debug('spawn');
        for (const id of ['barra', 'naftalina', 'dentaduras', 'jersey', 'fregona']) window.__MAMPORRO__.addWeapon(id);
        for (let i = 0; i < 300; i++) { g.update(1 / 60); if (g.state === 'levelup') window.__MAMPORRO__.choose(0); }
        g.draw(1, 1 / 60);
      });
      console.log(`[QA] ${language}: combate`);
      const particles = await page.evaluate(() => window.__QA_GAME__.runView.particleCount);
      assert(particles <= 400);
      await page.screenshot({ path: path.join(output, `${language}-combate-1280.png`) });
      await page.evaluate(() => { window.__MAMPORRO__.debug('boss'); window.__QA_GAME__.draw(1, 1/60); });
      assert.equal(await page.evaluate(() => window.__MAMPORRO__.audio().mode), 'intense');
      await page.screenshot({ path: path.join(output, `${language}-jefe-1280.png`) });
      await page.evaluate(() => { window.__MAMPORRO__.pause(); window.__QA_GAME__.draw(1, 0); });
      assert.equal(await page.evaluate(() => window.__MAMPORRO__.audio().mode), 'paused');
      await page.setViewportSize({ width: 1600, height: 900 });
      await page.evaluate(() => window.__QA_GAME__.draw(1, 0));
      await page.screenshot({ path: path.join(output, `${language}-pausa-1600.png`) });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
      report.push({ language, audio, particles, loop: await page.evaluate(() => window.__MAMPORRO__.loopStats()) });
      console.log(`[QA] ${language}: completo`);
      await context.close();
    }
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify({ report, errors }, null, 2));
    console.log(JSON.stringify({ report, errors }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
