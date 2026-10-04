# Exportación web de progreso — U4, paso 4

## Uso y contenido

Menú principal → Opciones → Exportar progreso para Unity → revisar → Descargar progreso. ES/EN disponibles. Nombre sugerido estable: `mamporro-progreso.json`; el navegador puede añadir un sufijo al descargar repetidamente. La aplicación no incluye fecha, rutas, identificadores del ordenador ni telemetría.

Se usa exactamente `mamporro.progress` v1: `source: {platform: "web", saveVersion: 3}` para el estado cargado actual, y `progress: {settings, meta}` normalizado según [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md). `lastRun` forma parte del contrato para conservar la protección de liquidación; no es una partida activa. No se exportan semilla, HP, XP, oro de partida, enemigos, inventario activo ni recibos.

## Arquitectura y ausencia de efectos laterales

`Game` expone a la UI una lectura de `save.data` ya cargado. `prepareProgressExport` copia un árbol de datos acotado, valida los campos críticos y las coherencias, normaliza opciones/conjuntos con informe y construye la transferencia. Obtiene IDs, misiones, límites de extras y defaults de las definiciones web existentes. No copia resultados C# ni modifica reglas de juego.

Tras validar estrictamente, contrasta el candidato con `parseSave`, función pura de la propia web; un saneamiento inesperado se rechaza. Serializa con propiedades ordenadas, vuelve a parsear y validar el resultado y congela el texto para la descarga. El mismo estado/contexto produce bytes idénticos. Las colecciones siguen orden de catálogo. V1/v2 se prueban desde el corpus mediante migraciones explícitas; la UI usa el estado cargado v3 y no interpreta JSON crudo de almacenamiento.

No construye `SaveManager`, llama a `persist`, lee/escribe `localStorage`, liquida partidas ni concede recompensas. La carga normal del juego preexistente sigue intacta: el exportador no recupera un archivo histórico que esa carga hubiera saneado anteriormente. Un fallo al obtener la instantánea es un error, no progreso inicial. Almacenamiento no disponible no impide exportar la copia en memoria, con aviso explícito de que podría no estar guardada en el navegador.

El informe muestra campos conservados, defaults, exclusiones y migraciones. Los errores críticos no permiten descargar. El usuario confirma la descarga de la copia revisada; nunca aplica recuperaciones al estado web. Solo existe en opciones del menú principal, no en la pausa de una partida. La UI construye texto mediante `textContent`; no interpreta datos como HTML.

La descarga usa Blob y enlace temporal, retira el enlace y revoca la URL. El mensaje indica «descarga solicitada»: no afirma que el usuario la haya guardado, algo que el enlace no permite confirmar. No se añaden dependencias al juego.

## Alcance exacto del diff web

- Nuevos: `src/save/exportProgress.ts`, `exportProgress.test.ts`, `src/ui/ProgressExport.ts`.
- `src/core/Game.ts`: solo lector de progreso en el contexto de UI.
- `src/ui/UI.ts`: tipo del lector, import del panel y colocación en opciones del título.
- `src/i18n/es.ts`, `en.ts`: 17 textos del exportador por idioma.

No cambian `schema.ts`, `SaveManager.ts`, meta, precios, misiones, catálogos, RNG, combate, generación ni lógica de partida. No exigir igualdad de toda `src/` frente a la base después de esta excepción; revisar estas rutas y exigir igualdad de las restantes. Referencias congeladas y hashes esperados intactos.

## Verificación reproducible

Desde la raíz, CMD:

```cmd
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
node --test scripts\u4-contract.test.mjs
node scripts\u4-export-fixtures.mjs
scripts\u3.cmd edit
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

Las 30 pruebas nuevas comprueban los ocho saves congelados, progreso exacto, no mutación profunda de estado/partida/recibo/almacenamiento, repetición, errores críticos, recuperaciones secundarias, límites y fallo de lectura/descarga. El script de fixtures genera **entradas**, no esperados, en `unity/TestResults/U4/Exports/`. `WebExportTests` pasa esos archivos reales a `ProgressValidator.Import` y compara los ocho candidatos con `u4-progress.json` sin modificarlo. `u3.cmd edit` ejecuta esa preparación automáticamente para evitar entradas ausentes/obsoletas.

El verificador histórico crea un worktree **detached** en una ruta única bajo `qa-results/reference-<guid>` apuntando a `0505b1690656d15188860157612455639820fe1f`. Copia los verificadores y referencias actuales sin modificarlos; comparte dependencias instaladas mediante junction `node_modules`. Ejecuta secuencialmente `unity-reference.mjs`, `unity-reference-u2.mjs`, `unity-reference-u3.mjs` y `u4-reference.mjs`, con sus guardas originales. Conserva ruta y logs, no limpia ni cambia la rama activa. No pasar flags de regeneración. La instantánea prueba la referencia histórica; las pruebas del exportador prueban la funcionalidad nueva.

### Navegador real

Playwright es herramienta externa de QA, no dependencia del proyecto. En este equipo se instaló en `%LOCALAPPDATA%\MamporroQA`, utilizando Edge ya instalado. Con Vite iniciado mediante `npm.cmd run dev -- --host 127.0.0.1`, ejecutar en otra consola CMD:

```cmd
set NODE_PATH=%LOCALAPPDATA%\MamporroQA\node_modules
set MAMPORRO_BROWSER=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
node scripts\verify-u4-export-browser.cjs
```

Si falta la herramienta: `npm.cmd install --prefix "%LOCALAPPDATA%\MamporroQA" --no-audit --no-fund playwright`. También puede usarse otra instalación de Playwright y un Chromium mediante esas variables; no modificar el lockfile del juego.

La prueba abre `?test` en contextos aislados ES/EN, pulsa la acción real, espera el evento de descarga y guarda/lee los archivos. Comprueba identidad/versión/contenido exacto, dos exportaciones idénticas y una desde memoria con acceso a almacenamiento bloqueado. Verifica estado y JSON guardado antes/después; prueba errores de Blob y progreso crítico inválido. No usa el perfil personal del navegador. La instrumentación de acceso al juego se limita a una respuesta interceptada por QA, sin añadir globals al código distribuido. Manejo de descargas conforme a la [documentación de Playwright](https://playwright.dev/docs/downloads).

Evidencias locales: `unity/TestResults/U4/BrowserExport/` contiene seis JSON descargados, capturas ES/EN y `report.json` con versión de navegador/fecha. Comandos y resultados de esta sesión en [PROGRESO_U4](PROGRESO_U4.md). La importación desde la interfaz Unity sigue pendiente del paso 5.
