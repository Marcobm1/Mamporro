// Resume exclusivamente las últimas mediciones válidas; no modifica referencias.
import { readdir, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

const directory = resolve('unity/TestResults');
const reports = [];
for (const file of await readdir(directory)) {
  if (!/^u1-player-.*\.json$/.test(file)) continue;
  const data = JSON.parse(await readFile(resolve(directory, file), 'utf8'));
  if (data.validRender) reports.push({ file, ...data });
}
reports.sort((a, b) => b.file.localeCompare(a.file));
const latest = new Map();
for (const report of reports) {
  const key = `${report.outputWidth}x${report.outputHeight}/${report.entities}`;
  if (!latest.has(key)) latest.set(key, report);
}
const rows = [...latest.values()].sort((a, b) => a.outputWidth - b.outputWidth || a.entities - b.entities);
for (const report of rows) {
  const csv = await readFile(resolve(directory, report.file.replace(/\.json$/, '.csv')), 'utf8');
  const samples = csv.trim().split('\n').slice(1).map(line => line.split(',').map(Number));
  const frames = samples.map(sample => sample[1]).sort((a, b) => a - b);
  report.invalidGpuSamples = samples.filter(sample => !Number.isFinite(sample[3]) || sample[3] > report.actualMeasurementSeconds * 1000).length;
  // Si hay tiempos físicamente incompatibles con toda la ventana, no presentar
  // una media GPU fiable ni ocultar los valores: el CSV original se conserva.
  if (report.invalidGpuSamples > 0) report.gpuMeanMs = -1;
  report.p99Ms = frames[Math.ceil(frames.length * .99) - 1];
  report.maxMs = frames.at(-1);
  report.overBudgetPercent = 100 * frames.filter(frame => frame > 1000 / 60).length / frames.length;
}
const number = value => value < 0 ? 'N/D' : value.toFixed(3);
let markdown = '# U1 · resultados locales\n\n';
markdown += 'Build normal Mono instrumentada, D3D11, interna 360, VSync 0, FPS ilimitados.\n';
markdown += '10 s de calentamiento y 30 s por escenario. Solo última medida con render válido.\n\n';
markdown += '| Salida | Entidades | FPS medios | Frame ms | P95 ms | P99 ms | Máx ms | >16,67 ms (%) | CPU ms | GPU ms | Memoria Unity MiB |\n';
markdown += '| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |\n';
for (const r of rows) markdown += `| ${r.outputWidth}×${r.outputHeight} | ${r.entities} | ${number(r.meanFps)} | ${number(r.meanMs)} | ${number(r.p95Ms)} | ${number(r.p99Ms)} | ${number(r.maxMs)} | ${number(r.overBudgetPercent)} | ${number(r.cpuMeanMs)} | ${number(r.gpuMeanMs)} | ${number(r.allocatedMemory / 1048576)} |\n`;
markdown += '\nGC total por frame: N/D cuando el recorder no está disponible; no asumir cero. Memoria Unity final antes de exportar, no pico/VRAM.\n';
markdown += 'CPU/GPU son medias de muestras disponibles; pueden faltar muestras o repetirse timings por latencia.\n';
markdown += 'GPU N/D también cuando hay muestras superiores a toda la ventana de ensayo: se descarta la media de esa condición, sin filtrar ni reescribir el CSV/JSON original.\n';
markdown += 'No representa combate completo ni requisitos de equipos modestos. Una repetición por condición: sin intervalo de confianza.\n\n';
markdown += 'Simulación a 60 Hz y render sin límite: muchos frames no contienen un tick de horda. P99/máximo ayudan a ver los picos; no interpretar P95 como coste de la lógica.\n\n';
for (const r of rows) markdown += `- ${r.file}: ${r.frames} muestras, ${r.renderedFrames} frames renderizados, ${number(r.actualMeasurementSeconds)} s; ${r.cpu}; ${r.gpu}; ${r.ramMB} MB RAM; muestras GPU inválidas: ${r.invalidGpuSamples}.\n`;
await writeFile(resolve(directory, 'RESUMEN.md'), markdown, 'utf8');
process.stdout.write(markdown);
if (rows.length !== 8) { console.error('Faltan condiciones: se esperan dos resoluciones por cuatro cargas.'); process.exitCode = 1; }
