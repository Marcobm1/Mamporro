# Checkpoint U3 — mundo y partida completa equivalentes

Estado: **AUTORIZADO por el autor el 29/09/2026; EN CURSO.** U4 no está autorizado.

Este archivo es el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U3 siga activo. Debe actualizarse después de cada paso completado y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md); reglas comunes: [`INSTRUCCIONES_PROYECTO.md`](INSTRUCCIONES_PROYECTO.md).

## Cómo retomar

- **Último punto verificado y publicado:** paso 10, commit «Juega en escena 2 personajes × 3 duraciones hasta resultados» (consultar `git log`; el hash no se escribe aquí para no crear un commit circular). Paso 9: `443645d`; paso 8: `16543c8`; núcleo del paso 10: `db490f5`; paso 7: `59a5f7f`; paso 6: `c42e22d`. Base recibida por Claude Code el 03/10/2026: `b21160cf060ef30daa4c55759aec1e0095b0bd54`.
- **Paso actual:** 11 del plan: build Windows x64 Mono y ensayo de partida real (minutos 2, 5 y 9 y enjambre con 750 vivos, 1080p y 1440p, `validRender`), con la acción `benchmark` de vuelta en `scripts\u3.cmd`.
- **Terminado y verificado:** pasos 1–10. Las cuatro partidas de `runs` coinciden con la web de principio a fin (`IntegratedRunTests`) y en escena se juegan 2 personajes × 3 duraciones hasta resultados (`U3RunFlowTests`). El 03/10/2026 (paso 10): Edit Mode **167/167**, Play Mode **17/17**. **`u3-world.json` sigue congelada**, sin regeneración.
- **A medias:** nada. Pendientes: pasos 11 y 12. U4 no autorizado.
- **Sin commit a propósito:** `unity/ProjectSettings/ProjectSettings.asset` (configuración local de nube), `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset` y `URPProjectSettings.asset`: no publicar. También se conservan fuera del índice los cambios de espacios/EOL de `RetroPipeline.asset`, `UniversalRenderPipelineGlobalSettings.asset` y `GraphicsSettings.asset` (el autor confirmó el 03/10/2026 que se tratan igual). No limpiar ni restaurar estos archivos automáticamente.
- **Siguiente paso exacto:** paso 11. Ensayo de partida real en la build (`-u3-benchmark`): partida automática en el mapa real con el director, mediciones en los minutos 2, 5 y 9 y en el enjambre con 750 vivos, a 1920×1080 y 2560×1440, con el mismo formato e informe que U1/U2 (FrameTimingManager, `validRender`, P95, GC); añadir `benchmark` a `scripts\u3.ps1`/`u3.cmd`. Objetivo 60 FPS con 300 enemigos; si el enjambre no llega, perfilar y documentar.

Comprobar el estado desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch
git log --oneline -8
scripts\u3.cmd edit
scripts\u3.cmd play
scripts\u3.cmd build
scripts\u3.cmd visual
```

## Registro de sesiones y relevos de U3

Añadir las entradas nuevas **de más antigua a más reciente** con la plantilla de `docs/PROGRESO_U2.md` («Plantilla para cada sesión posterior»). No borrar entradas de otro agente salvo corrección factual explícita.

### 29/09/2026 — autorización de U3 y paso 1 — Claude Code

- Punto de partida/commit: `176423d` (aprobación de U2 ya publicada; no hizo falta repetirla).
- Trabajo realizado: revisión del código web de U3; dos dudas resueltas por el autor (teclas de depuración y catálogo); creación de este checkpoint; `CLAUDE.md`, `AGENTS.md`, `CONTINUIDAD_AGENTES.md`, `INSTRUCCIONES_PROYECTO.md` y `ESTADO_ACTUAL.md` apuntan a U3; decisiones en `DECISIONES.md`.
- Pruebas realmente ejecutadas y resultado: ninguna (cambio solo documental).
- Push realizado: ver el commit «Abre U3 y registra sus decisiones».
- Cambios locales no incluidos: los ajustes locales de Unity citados en «Cómo retomar».
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 2 del plan.

## Referencia u3-world (paso 2)

`scripts/unity-reference-u3.mjs` (mismo modelo que U2: guarda contra `src/`, `--export-once` solo crea, sin argumentos comprueba; `--dry-run <ruta>` genera en otra ruta para inspeccionar). Antes de crearla se hicieron pasadas de prueba en archivos temporales para ajustar tamaño y escenarios; el archivo definitivo se creó una sola vez.

Contenido (todas las cifras salen de la web aprobada):

- `simplex`: 240 valores de `createNoise2D` alimentado por `Rng('U3-SIMPLEX')`.
- `worlds`: semillas `HITO6QA`, `PULIDO-REFERENCIA`, `MAMPORRO` (las tres de `baseline.worlds`) y `U3-MUNDO`. Alturas finales en base64 de Float32; terreno sin aplanar (`rawHeights`) solo en `MAMPORRO` y `U3-MUNDO`. 300 muestras por mundo (`heightAt`, `normalAt`, `squircle`, `isInside`, `groundHeight`, `groundNormal`), 150 empujones cerca de colisionadores (`pushOutCircle`, `resolveObstacles`, `clampInside`), sites, piezas de props, colisionadores (decoración + props + interactuables, en su orden), interactuables y decoración completos; hierba y flores completas en `MAMPORRO` y `U3-MUNDO` y, en las otras dos, recuento más las 50 primeras.
- `physics`: 5 trayectorias con `stepPlayerInCrowd` sobre `U3-MUNDO` (caminar, saltos, deslizamiento cuesta abajo, hacia un sitio y subida a la montaña del borde), una muestra cada 5 ticks.
- `director`: parámetros de aparición cada 5 s y cronología de oleadas, élites y enjambre para 5, 10 y 15 minutos.
- `spawns`: 3600 ticks de `SpawnSystem` en `MAMPORRO` con vista en movimiento, más las tres formaciones.
- `chestCosts` (0–19) e `interactables`: descubrimiento, carga y descarga de un santuario y avisos de interacción siguiendo una ruta fija.
- `runs`: cuatro partidas completas con la lógica de `Game.ts` (física → interactuar → `run.update` → derrota, victoria con 1,6 s o elegir la primera carta). Ruta: interactuables por vecino más cercano; se pulsa interactuar al llegar (< 1,5 m); en santuarios se espera a cargarlo o 700 ticks; 1800 ticks máximo por punto; giro de cámara = dirección de marcha.
  - `remedios-5min-invencible` (`MAMPORRO`): 5 min + 40 s de enjambre; detalle cada 10 ticks los primeros 120 s y totales cada segundo.
  - `remedios-10min-sin-totems` (`U3-MUNDO`, vulnerable): derrota a los 87 s; detalle completo.
  - `baguette-15min-derrota` (`U3-MUNDO`, vulnerable, empieza por un tótem): derrota a los 31 s.
  - `armario-jefe-victoria` (`HITO6QA`, invencible, armario revelado): jefe a los 12 s, victoria a los 299 s.

## Núcleo del mundo (paso 3)

Código en `unity/Assets/Mamporro/Core/World/` (ensamblado `Mamporro.Core`, sin UnityEngine):

- `SimplexNoise.cs`: port de `createNoise2D` de simplex-noise 4.0.3, con el aviso MIT de Jonas Wagner.
- `JsMath.cs`: `Math.hypot` de V8 (escalado por el máximo y suma de Kahan); comprobado en Node con 2 millones de pares, 0 diferencias (con `sqrt(x²+z²)` difiere el 35 %).
- `Heightfield.cs` (+ `WorldMath`), `Sites.cs`, `Colliders.cs` (colisionadores y rejilla), `Props.cs` (casas, templos, granjas, pozos y objetos sueltos; mismo orden de consumo del RNG que la web), `Vegetation.cs` (decoración y cobertura del suelo), `WorldCollision.cs` (colocación de interactuables, colisión del mundo y `WorldData.Generate(seed)`), `PlayerPhysics.cs` (física del jugador y frenado por la horda).
- `WorldCollision` implementa `ICombatWorld` (U2) e `IPhysicsWorld`. En `ICombatWorld.Height`, `maxY` infinito significa «solo terreno» (la web usa `heightfield.heightAt` para apariciones, proyectiles y disparos) y un `maxY` finito equivale a `groundHeight`.
- `scripts/u2-export-data.mjs` exporta también las constantes del mundo, del jugador, del director y de los interactuables (clase `Tuning`, con prefijos como `Terrain`, `Player`, `PlayerSlide`, `SpawnCurve`, `Shrine`…), las tablas (`SiteRequests`, `InteractablePlacements`, `SpawnTable`, `SpecialWaves`, `ShrineBoosts`, `RunDurations`) y la paleta (`Palette`).
- `Rng.Pick` añadido (equivale a `rng.pick`).

Pruebas (`Tests/Core/WorldReferenceTests.cs`): ruido, terreno sin aplanar, alturas finales, sitios, consultas de colisión, empujones, piezas, colisionadores, interactuables, decoración, cobertura, `baseline.worlds` y las 5 trayectorias de física. Todo pasa a la primera y las alturas coinciden bit a bit.

### Matemáticas de V8 frente a Mono (medido el 29/09/2026)

Sonda temporal con 200 000 entradas exactas (base64). Diferencias de Mono 6.13 (runtime del Editor) frente a V8: `atan2` 19,5 %, `exp` 9,5 %, `sin`/`cos` ~2 %, `pow` ~0,2 %, `Hypot` portada 0 %. Además, **JsonUtility lee mal el 9,4 % de los double** (un bit). Decisión técnica: no portar las funciones de V8 mientras las pruebas pasen, porque las diferencias son de un bit y quedan dentro de 1e-6 m. Si la cronología de la partida integrada (paso 10) diverge, lo primero que hay que revisar son `atan2` y `exp`.

## Partidas integradas (paso 10, núcleo)

`Tests/Core/IntegratedRunTests.cs` reproduce las cuatro partidas de `runs` con `WorldRun` y el mismo guion que `scripts/unity-reference-u3.mjs` (ruta por vecino más cercano, E al llegar, espera en mesas camilla, giro de cámara = dirección de marcha, primera carta, victoria tras 1,6 s).

- **Resultado medido el 03/10/2026:** las cuatro coinciden con la web **de principio a fin**: mismos sucesos en el mismo tick (51, 4, 6 y 26), todas las filas de detalle (posición, vida, XP, oro, bajas, vivos y los 8 primeros enemigos) con desvío máximo de posición 1,4e-14 m, mismos totales cada segundo y el mismo final (incluido el daño de cada arma, el enjambre hasta 750 vivos, la derrota en el tick 5215/1844 y la victoria en el 17930). No ha hecho falta portar `atan2`/`exp` de V8.
- **Aserciones (tolerancia acordada):** cronología exacta en los primeros 120 s (sucesos, detalle, totales; posiciones 1e-6 m, enemigos 1e-4 m por Float32). Después, totales cada segundo con margen: bajas, apariciones y nivel ±3 % (mínimo 3/3/1), vivos y oro ±5 % (mínimo 10), mismo estado de enjambre y baúles ±1; mismo desenlace con el tick final ±60. El margen solo cubre posibles diferencias de un bit en otro runtime; hoy la coincidencia es total.
- **Comportamiento del guion web que hay que reproducir:** `while (run.openChoice()) run.choose(0)` deja abierta la segunda carta cuando hay dos subidas en el mismo tick (tras elegir, `closeChoice` abre la siguiente y `openChoice()` devuelve false), y `Run.update` no se detiene con una carta abierta (la pausa la pone `Game`). En `remedios-5min-invencible` pasa en el tick 5931 (niveles 8 y 9): desde ahí la web ya no elige más cartas. `CombatRun.HoldWhileChoosing` (true por defecto, el comportamiento de juego) separa esa pausa de la lógica de `Run.update`; el arnés lo pone a false y usa el mismo bucle que la web. No es un cambio de reglas.

## Alcance autorizado

Mundo y partida completa equivalentes a la web aprobada `0505b1690656d15188860157612455639820fe1f`:

- Mundo procedural: heightfield de 320 m con bancales; sites (casas, templos, granjas, pozos) con props y colisionadores; decoración con colisión; cobertura del suelo y fauna decorativa (desactivables si pesan).
- Colisiones del jugador y de la horda sobre el mundo real (pendiente máxima del jugador 48°, escalón enemigo 0,6 m y alcance vertical de contacto 1,6 m según la web, reciclado lejano).
- Director: `SPAWN_TABLE`/`SPAWN_CURVE`, 6 oleadas especiales, élite cada 2 min, enjambre final (hasta 750 vivos), duraciones 5/10/15 con su escalado de XP/oro y temporizador.
- Interactuables: 14 baúles con precio creciente, 3 mesas camilla, 2 tótems, armario escondido → Pelusa Madre → victoria a los 1,6 s. La carta de relleno de oro pasa a usar `chestCost(baúlesAbiertos)`, como la web.
- Minimapa con descubrimiento; avisos (oleadas, élite, enjambre, armario); telegrafiado de los ataques del jefe; pausa con semilla y estadísticas.
- Pantalla técnica de inicio (uGUI): ambos personajes, duración 5/10/15 y semilla opcional. Pantalla técnica de resultados: tiempo, bajas, daño por arma y nivel; sin Calderilla.
- Depuración equivalente a la F3 web (invencible, subir nivel, saltar un minuto, aparecer enemigos, matar a todos, invocar al jefe, oro, revelar el mapa) que marca `run.cheated`.
- Cámara: sigue chocando solo con el terreno, como la web.

**Fuera de alcance:** menús finales, Calderilla, tienda, misiones y guardado (U4); audio y pulido (U5); mundo ampliado, escalada, nueva curva de hordas/oro, arte nuevo y cámara contra estructuras. Sin rebalanceo. No se tocan `src/` ni las referencias existentes (`baseline.json`, `u2-combat.json`).

## Decisiones del autor para U3 (29/09/2026)

Detalle en `docs/DECISIONES.md`.

1. **Referencia complementaria** `scripts/unity-reference-u3.mjs` → `unity/Docs/Reference/u3-world.json`, con el mismo modelo que U2: solo desde la web aprobada, guarda contra `src/` en `0505b16`; no toca `baseline.json` ni `u2-combat.json`; los esperados nunca se recalculan con C#. Cubre: rejilla densa de alturas, sites, props y colisionadores, decoración, interactuables, posiciones de aparición, director por duración, oleadas, élites, enjambre, precios de baúl, carga y descarga de mesa, tótem, descubrimiento y una partida integrada con recorrido fijo y cronología de eventos. Una vez usada por una prueba C#, no se regenera.
2. **Tolerancias:** núcleo del mundo portado en double; igualdad exacta en lo discreto (número, tipo y orden de sites, props, interactuables, oleadas y eventos); 1e-6 m en alturas y posiciones; cronología de la partida integrada exacta en los primeros 60–120 s y, después, comparación por totales con margen documentado. Incluye un port fiel del algoritmo de la librería `simplex-noise` (4.0.3, licencia MIT; se conserva su aviso) alimentado por el RNG propio.
3. **Movimiento:** portar la física del jugador de la web (`src/entities/playerPhysics.ts`: pendientes, escalones, salto, deslizamiento cuesta abajo hasta 30 m/s) y probarla con sus casos de prueba, conservando la entrada y la cámara de U1. Las pruebas de U1 y U2 deben seguir pasando.
4. **Pantallas técnicas** de inicio y resultados como se describen en el alcance.
5. **Teclas (respuesta del autor, 29/09/2026):** F3 abre la depuración y 1–8 ejecutan sus acciones, como la web. En U3 el ajuste de vértices pasa de F3 a F9; U1 y U2 no cambian. E interactúa, como la web.
6. **Catálogo (respuesta del autor, 29/09/2026):** todo desbloqueado (6 armas y 12 objetos), como la web sin filtros de meta. La referencia y las pruebas usan ese caso; los filtros de desbloqueo se conectarán en U4 con el guardado.

## Plan

Commits pequeños que compilen y pasen sus pruebas; actualizar este checkpoint tras cada paso; `git fetch` antes de cada push (si el remoto cambió, parar y avisar al autor).

1. Este checkpoint y las entradas de documentación. **Hecho.**
2. Referencia `u3-world.json`. **Hecho.**
3. **Hecho.** Núcleo del mundo en C# puro (`Assets/Mamporro/Core`, sin UnityEngine): port de simplex-noise, heightfield, sites, props y colisionadores, decoración, cobertura del suelo, interactuables y colisión del mundo; pruebas contra `u3-world.json` y `baseline.worlds`.
4. **Hecho.** Render del mundo con variante del material retro de U1 en una escena nueva `Assets/Mamporro/U3/U3_Partida.unity`; U1 y U2 conservan sus escenas y builds.
5. **Hecho, 03/10/2026.** Física del jugador y combate/horda conectados al mundo real; apariciones controladas QA, reciclado lejano, cartas, render instanciado y reinicio probado.
6. **Hecho, 03/10/2026.** Director completo.
7. **Hecho, 03/10/2026.** Interactuables, armario, jefe y victoria.
8. **Hecho, 03/10/2026.** HUD, minimapa, avisos, telegrafiado y pausa.
9. **Hecho, 03/10/2026.** Pantallas de inicio y resultados; depuración con `run.cheated`.
10. **Hecho, 03/10/2026.** Integración: partidas deterministas contra la cronología web (ver «Partidas integradas»); Play Mode con partidas aceleradas (2 personajes × 3 duraciones) hasta resultados; victoria, derrota, enjambre y reinicio sin restos.
11. Build Windows x64 Mono ejecutada y ensayo de partida real: minutos 2, 5 y 9 y enjambre con 750 vivos, a 1080p y 1440p, con las mismas condiciones y `validRender` que U1/U2. Objetivo: 60 FPS con 300 enemigos en el mapa real; si el enjambre no llega, perfilar y documentar antes de cerrar.
12. Cierre: documentación, push y parada para la prueba manual del autor con instrucciones.

## Pruebas ejecutadas

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 29/09/2026 21:47 | `node scripts\unity-reference-u3.mjs --export-once` y `node scripts\unity-reference-u3.mjs` | creada (7,8 MB) y coincide con la web | `unity/Docs/Reference/u3-world.json` |
| 29/09/2026 21:48 | `node scripts\unity-reference.mjs`, `node scripts\unity-reference-u2.mjs`; `git diff --exit-code` de `src/`, `baseline.json` y `u2-combat.json` | U0 y U2 coinciden; sin cambios | consola |
| 29/09/2026 21:55 | Prueba temporal `TmpMathProbe` (Edit Mode, filtro) contra 200 000 valores de V8 en base64 | ver «Matemáticas de V8 frente a Mono» | `unity/TestResults/U3/probe.log` (prueba borrada después) |
| 29/09/2026 21:59 | `scripts\u2.cmd edit` | 118/118 (88 anteriores + 30 de `WorldReferenceTests`); alturas de los 4 mundos idénticas bit a bit | `unity/TestResults/U2/edit.xml` |
| 29/09/2026 22:00 | `scripts\u2.cmd play` | 10/10 | `unity/TestResults/U2/play.xml` |

### 29/09/2026 — cierre del paso 4 local de Claude Code — Codex

- Auditado antes de editar: documentación, `git fetch origin`, status completo con archivos sin seguimiento, log, remoto, diff y archivos nuevos. HEAD/remoto coincidían en `522d8de`; no se hizo pull, stash, reset ni clean.
- Conservada la implementación local de Claude Code: ensamblado `Mamporro.U3`, `MeshBuilder`/`WebSpace`, texturas, `WorldRenderer`, `U3Game`, `U3VisualCheck`, escena `U3_Partida`, generador `Editor/U3Project`, shaders `RetroWorld`/`RetroSky`, launcher y `U3SceneTests`, referencias de ensamblados y entrada de Build Settings. Todos los assets nuevos tienen `.meta` y la escena referencia sus shaders/controlador.
- Render: terreno coloreado por altura/pendiente, props YXZ, casas/templos/granjas/pozos, árboles/rocas/arbustos, hierba/flores con viento, modelos estáticos de interactuables, cielo con sol, niebla/dither/snap, 240/360/480, cámara y física portada. Cámara solo contra terreno; F9 para snap.
- Ajustes de auditoría: controles visibles, destrucción del material del avatar, intención de prueba no serializable explícita, cielo situado al plano lejano como la web. Launcher `visual` con `-u3-visual-check` y `-u3-output`, seis capturas nuevas/no uniformes y error si falta un site. Eliminada la acción `benchmark` copiada de U2 porque no estaba implementada; llegará en el paso 11. README Unity actualizado.
- Pruebas finales realmente ejecutadas (hora Europe/Madrid):

| Fecha/hora | Comando CMD | Resultado | Registro local |
| --- | --- | --- | --- |
| 29/09/2026 22:52 | `scripts\u3.cmd edit` | 118/118, exit 0 | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 29/09/2026 22:53 | `scripts\u3.cmd play` | 11/11, exit 0; incluye U1/U2 | `unity/TestResults/U3/play.xml`, `play.log` |
| 29/09/2026 22:54 | `scripts\u3.cmd build` | Success, Windows x64 Mono, exit 0 | `unity/TestResults/U3/build.log` |
| 29/09/2026 22:54 | `scripts\u3.cmd visual` | exit 0; seis PNG nuevos revisados | `unity/TestResults/U3/visual.log`, `Visual/inicio.png`, `vista-alta.png`, `sitio-house.png`, `sitio-temple.png`, `sitio-farm.png`, `sitio-well.png` |
| 29/09/2026 22:54 | `git diff --exit-code -- src unity/Docs/Reference unity/Assets/Mamporro/U2 unity/Assets/Mamporro/Generated/U1_Patio.unity` | sin diferencias | consola |

- Incidencias resueltas: primer intento Edit dentro del sandbox no conectó al servicio de licencia; se detuvo solo ese batch y se repitió fuera. Primer visual con ventana oculta produjo seis imágenes negras; no se considera válido. El launcher usa ventana visible y rechaza capturas uniformes. Sonda temporal de cielo aislado retirada del código; su PNG queda solo en el directorio de resultados ignorado.
- Límites observados: niebla densa en la panorámica y silueta marcada de las montañas del borde; degradado del cielo continuo al aislarlo. Avatar provisional, interactuables todavía estáticos, sin fauna ni combate conectado. Las capturas no prueban rendimiento ni equivalencia visual píxel a píxel. El contador instantáneo de FPS no es un benchmark.
- Estado antes del commit: solo archivos del paso 4 y documentación seleccionados. Restaurados explícitamente los cuatro ajustes con diferencias exclusivamente de espacios/EOL (`RetroPipeline`, `UniversalRenderPipelineGlobalSettings`, `GraphicsSettings`, `ProjectAuditorSettings`). Se conservan fuera del commit `ProjectSettings.asset`, `PackageManagerSettings.asset` y `URPProjectSettings.asset`; no se publican Builds, TestResults ni capturas.
- Commit de esta pieza: «Añade la escena y el render del mundo de U3»; push normal tras fetch y comprobación del remoto. El resultado del push se registra al continuar el checkpoint, sin incluir un hash propio circular.
- Siguiente paso exacto: paso 5 descrito en «Cómo retomar». Sin decisiones pendientes del autor; U4 no autorizado.

### 03/10/2026 — recuperación del WIP y cierre del paso 5 — Codex

- **Punto de partida:** `272ce95566df98d36ba60456731d16de09022852`, local y remoto iguales tras fetch (ahead 0 / behind 0). Sin commits inéditos ni archivos preparados. Se leyeron documentación, diff y todos los archivos sin seguimiento antes de editar; no se hizo pull, stash, reset, clean ni regeneración de referencias.
- **WIP recuperado:** `Core/CombatRun.cs`, `U3/U3Game.cs`, `Core/World/WorldRun.cs` (esta es su ruta real), `U3/RunCards.cs`, `U3/RunRenderer.cs` y sus `.meta`. Ya contenía el puente física/combate, tamaño de rejilla configurable, cartas y render. Compilaba y pasaba 118/118 Edit y 11/11 Play, pero no tenía apariciones en escena ni pruebas específicas. Se conservó esa implementación.
- **Completado:** conexión sobre `WorldCollision`, coordenadas web en la lógica y conversión Z solo en render; QA F3 con acciones 1/2/4/5/6/7 y marca `WorldRun.Cheated`; 3/8 reservadas sin implementar. QA 4 añade hasta 100 enemigos alternando los cuatro normales y la rata; no usa ni pretende sustituir la futura tabla del director. F8 crea una sesión nueva, vacía pools/efectos/cartas, reinicia física/cámara/HUD y conserva los recursos de render reutilizables.
- `Core/World/WorldSpawns.cs` porta **solo** búsqueda de posición y reciclado de `SpawnSystem.ts`, con RNG derivado de `seed + /run/spawn`, distancias existentes y exclusión de élite/jefe. `Enemies.Relocate` conserva ID/vida/IA y restablece posiciones anteriores/velocidades. La reconstrucción de rejilla permanece al final del movimiento, como en la web. Este código se ampliará en el paso 6, sin un spawner alternativo.
- **Presentación:** modelos técnicos de U2 adaptados a coordenadas web, proyectiles propios/hostiles, recogibles, efectos y avisos provisionales. Matrices, pools y lotes reutilizados; sin GameObject/Update/Rigidbody por enemigo. Material `U3/Combat.mat` referenciado desde escena/generador, malla de combate propia con color de vértice blanco y destrucción de recursos al cerrar. `RunCards` conserva la espera de 0,4 s y prioridad sobre las teclas QA.
- **Pruebas añadidas:** 14 casos Edit en `Tests/Core/WorldCombatTests.cs`: altura/aparición/rejilla de 320 m, persecución, muro sólido en el terreno generado, escalones 0,55/0,65 m alrededor del límite web 0,6, terreno irregular, presión/frenado/contacto/iframes, arma automática, muerte y XP/oro/cartas, disparos hostiles y paloma, empuje/culetazo del jefe, reciclado y reutilización de slots. Bucle caliente de mundo/combate sin asignaciones gestionadas tras calentamiento. No es una medición de FPS ni del GC de UI/render.
- **Play Mode:** dos casos nuevos en `U3CombatSceneTests.cs`: escena con QA, movimiento, instancias y elección con guardia; reinicio fuerte **tres veces**, con dos semillas, enemigos/jefe, ambos pools de proyectiles, daño, XP, oro, objetos/tomos/armas, descartes, cooldowns, cámara/entrada, cartas y efectos. Verifica almacenamiento independiente de la sesión anterior, rejilla vacía, HUD limpio y ausencia de acumulación de objetos de escena.
- **Incidencias resueltas:** arranque del entorno aislado bloqueado; lecturas/Unity ejecutados fuera con permisos. La primera captura de combate colocaba la cámara bajo el terreno y el HUD podía quedar atrasado tras reset: corregidos. La build inicialmente eliminaba la variante instanciada de `RetroWorld` (2 variantes antes y 1 después del stripping), pese a pasar Play Mode: `Combat.mat` serializado hace que conserve **2/2**. Las capturas anteriores no se consideran validación del combate.
- **Visual reforzado:** `U3VisualCheck` y `scripts/u3.ps1` generan nueve capturas, añadiendo combate, cartas y reinicio. Además compara los píxeles de la RenderTexture con/sin combate, con cámara, simulación y viento inmóviles y sin HUD. Falla si cambian menos de 50 píxeles. Última ejecución: **2575 píxeles distintos**, enemigos/proyectiles/efectos visibles; nueve PNG revisados. No equivale a fidelidad píxel a píxel con la web ni a benchmark.
- **Decisiones:** ninguna nueva decisión de diseño. Se corrige arriba la descripción histórica del step: `ENEMY_STEP=0.6` y `ENEMY_REACH_HEIGHT=1.6` en la web. No se rebalancea ni se portan preventivamente matemáticas de V8.

Pruebas ejecutadas el **03/10/2026**, horas Europe/Madrid; los XML/logs finales sustituyen los intentos previos:

| Hora | Comando CMD | Resultado | Registro |
| --- | --- | --- | --- |
| 18:47 | `scripts\u3.cmd edit` | 132/132, exit 0; incluye U0/U2/mundo y 14 casos nuevos | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 18:48 | `scripts\u3.cmd play` | 13/13, exit 0; incluye U1/U2 y dos integraciones nuevas | `unity/TestResults/U3/play.xml`, `play.log` |
| 18:49 | `scripts\u3.cmd build` | Success, Windows x64 Mono, exit 0; 2/2 variantes de RetroWorld conservadas | `unity/TestResults/U3/build.log` |
| 18:49 | `scripts\u3.cmd visual` | exit 0, nueve PNG nuevos revisados y comprobación de píxeles correcta; D3D11 / RTX 4070 Ti SUPER | `unity/TestResults/U3/visual.log`, `Visual/*.png` |
| 18:22–18:24 | `node scripts\unity-reference.mjs` | U0: datos y medios coinciden, exit 0 | consola de esta sesión |
| 18:22–18:24 | `node scripts\unity-reference-u2.mjs` | U2 coincide, exit 0 | consola de esta sesión |
| 18:22–18:24 | `node scripts\unity-reference-u3.mjs` | U3 coincide, exit 0; sin exportar/regenerar | consola de esta sesión |
| Durante auditoría/cierre | `git diff --exit-code 0505b1690656d15188860157612455639820fe1f -- src` y `git diff --exit-code -- src unity/Docs/Reference unity/Assets/Mamporro/U2` | sin diferencias, exit 0 | consola de esta sesión |

- Los verificadores Node se ejecutaron concurrentemente y U2/U3 avisaron del puerto de WebSocket 24678 ocupado; los tres completaron la comparación correctamente con exit 0. No se modificó su configuración ni las referencias.
- **Exclusiones locales conservadas:** los cuatro ProjectSettings prohibidos y diferencias de formato de RetroPipeline, UniversalRenderPipelineGlobalSettings y GraphicsSettings. `git diff --check` global señala espacios de ajustes Unity excluidos; se revisa por separado el índice del commit. Builds, TestResults y capturas permanecen ignorados. `src/`, referencias y escena/código U2 intactos.
- **Límites y pendientes:** escena técnica con Remedios como selección inicial, sin selector final ni director automático. Modelos/avisos provisionales; niebla y silueta marcada del borde del mundo heredadas del paso 4. Falta comparar la cronología integrada (paso 10), ensayar rendimiento real (paso 11) y validar el conjunto U3 con el autor. No se atribuye a estas pruebas una partida completa 5/10/15 ni un objetivo FPS cumplido.
- **Commit y push:** `4de718d2af996c44dc0b7f9d097e0f8bf3a076ee`, «Conecta el combate de U3 al mundo procedural». Índice revisado: 23 archivos exclusivamente del paso 5/documentación; `git diff --cached --check` correcto. Tras `git fetch origin`, remoto aún en `272ce955`; push normal correcto `272ce95..4de718d`. `git ls-remote origin refs/heads/claude/zen-pasteur-674ik0` confirmó el hash completo publicado. No hay código del paso 5 sin commit; quedan solo los ajustes Unity excluidos enumerados arriba. El presente ajuste documental registra ese resultado.
- **Siguiente paso exacto:** paso 6, director completo, sobre `WorldRun`, `WorldSpawns` y `CombatRun` existentes. No hay decisiones bloqueantes del autor. U4 no autorizado.

### 03/10/2026 — paso 6: director completo — Claude Code

- Punto de partida/commit: `b21160c` local y remoto iguales tras `git fetch`. Base comprobada antes de editar: `scripts\u3.cmd edit` 132/132 y `scripts\u3.cmd play` 13/13. Árbol: los cuatro ajustes excluidos más las diferencias de espacios/EOL de `RetroPipeline`, `UniversalRenderPipelineGlobalSettings` y `GraphicsSettings`; el autor confirmó que se dejan sin publicar ni restaurar.
- Trabajo realizado: port de `Director.ts` y `difficulty.ts` (`Core/World/Director.cs`: curva, tope de vivos, vida/XP/oro por minuto de dificultad, ritmo 5/10/15, temporizador, 6 oleadas, élite cada 2 min de dificultad, enjambre con ritmo que se duplica cada 20 s, tope 750 y +60 % de vida por minuto de prórroga; `PickEnemy` con un único sorteo). `WorldSpawns` completa `SpawnSystem.ts` sobre el mismo RNG `seed/run/spawn`: acumulador, tope, sorteo de tabla, élite en el anillo, formaciones fila/anillo/arco, ráfaga de depuración y reciclado (excluye élite y jefe por tipo). `WorldRun` hace de `Run.ts`: crea el director, fija `CombatRun.Pace`, y se engancha a `CombatRun.Step` justo después de las pasivas (`ISchedule`, mismo orden que `Run.update`: parámetros → director → apariciones/reciclado → jefe → enemigos…). Capacidad de enemigos 800 (`ENEMY_CAPACITY` web). Matar al jefe marca `Victory` solo en U3 (`BossEndsRun`); U2 no cambia. Sucesos de partida en `CombatRun.Events` (`wave`, `elite`, `swarm`, `levelUp`, `bossSpawned`, `boss`). Acciones de depuración 1/2/3/4/5/6/7 portadas en `WorldRun` (las teclas 3 y 8 se conectan en el paso 9); `DebugLevelUp` y `DebugKillAll` ahora hacen lo mismo que la web.
- Archivos/sistemas principales: `Core/World/Director.cs` (nuevo), `Core/World/WorldSpawns.cs`, `Core/World/WorldRun.cs`, `Core/CombatRun.cs`, `U3/U3Game.cs` (estado con tiempo/ritmo/tope y victoria), `Tests/Core/DirectorReferenceTests.cs` y `Tests/Core/WorldDirectorTests.cs` (nuevos), `Tests/Core/WorldCombatTests.cs` (sus pruebas de combate controlado desactivan el director con `Automatic=false`).
- Decisiones nuevas: ninguna de diseño. Técnica: `WorldRun.Automatic=false` solo para pruebas de combate aislado.
- Pruebas realmente ejecutadas y resultado: ver la tabla «Pruebas del paso 6». Coinciden con la web: parámetros cada 5 s de las tres duraciones, cronología exacta de oleadas/élites/enjambre por tick, 3600 ticks de apariciones (159 posiciones, recuentos por segundo, posiciones y tipos finales) y las tres formaciones.
- Pruebas pendientes/no ejecutadas: build y comprobación visual no se repitieron en este paso (sin cambios de render); partidas integradas, paso 10.
- Commits creados: «Añade el director completo de U3». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: la escena todavía no tiene pantalla de inicio (10 min por defecto), HUD final ni interactuables activos.
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 7 (ver «Cómo retomar»).

#### Pruebas del paso 6

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 19:45 | `scripts\u3.cmd edit` | 151/151 (132 anteriores + 19 nuevas de director/apariciones) | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 03/10/2026 19:46 | `scripts\u3.cmd play` | 13/13 | `unity/TestResults/U3/play.xml`, `play.log` |

### 03/10/2026 — paso 7: interactuables, armario, jefe y victoria — Claude Code

- Punto de partida/commit: `c42e22d` (paso 6 publicado).
- Trabajo realizado: port de `Interactables.ts` (`Core/World/Interactables.cs`: descubrimiento a 30 m y armario a 22 m, carga de mesa camilla en 9 s dentro de 4,2 m y descarga al 35 %, desafío del tótem, aviso de interacción con alcance por tipo y precio del siguiente baúl, revelar). `WorldRun` añade lo que hace `Run.ts` con ellos: E en el mismo punto del tick que la web (tras la física y con la posición del tick anterior); baúl con `chestCost`, aviso `noGold`, `rollItem` con su propio RNG `seed/run/items` y devolución del oro si no queda objeto; tótem de 45 s con modificadores al director (×2,2 ritmo, ×1,25 vida, ×2 oro) y +50 de suerte, y objeto con +100 de suerte al superarlo; mesas camilla con `generateShrineOffer` (bendiciones con rareza, sin volver a tirar/saltar/descartar) antes que las subidas de nivel; armario → Pelusa Madre a 4 m por detrás; el enjambre revela el armario; victoria con espera de 1,6 s (solo física, la partida no avanza). La carta de relleno de oro usa `chestCost(baúlesAbiertos)`. Escena: E interactúa, el estado muestra el aviso, la tapa de los baúles se dibuja aparte y se abre como en la web, anillo de carga sobre el terreno en las mesas, brillos de tótem/armario y su estado usado; las cartas muestran las bendiciones. Depuración 3 (+1 minuto) y 8 (revelar mapa) ya conectadas; todas marcan `Cheated`.
- Archivos/sistemas principales: `Core/World/Interactables.cs` (nuevo), `Core/World/WorldRun.cs`, `Core/CombatRun.cs` (bendiciones, suerte del desafío, elecciones de mesa, RNG de objetos, `CheckPurse`, sucesos `item`/`shield`/`revive`), `Core/Offers.cs` (`Shrine`, `RollItem`, relleno con baúles abiertos), `U3/RunRenderer.cs`, `U3/WorldRenderer.cs` (sin tapa estática), `U3/RunCards.cs`, `U3/U3Game.cs`, `Tests/Core/InteractableTests.cs` (nuevo).
- Decisiones nuevas: ninguna.
- Pruebas realmente ejecutadas y resultado: ver «Pruebas del paso 7». Precios 0–19 y el recorrido completo de interactuables (descubrimientos, carga, avisos y precios cada 30 ticks) coinciden con la web.
- Pruebas pendientes/no ejecutadas: partidas integradas (paso 10).
- Commits creados: «Añade los interactuables, el armario y la victoria de U3». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: HUD, minimapa, avisos visibles y pantallas técnicas llegan en los pasos 8–9; hasta entonces la escena muestra el estado en el panel de texto.
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 8 (ver «Cómo retomar»).

#### Pruebas del paso 7

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 19:53 | `scripts\u3.cmd edit` | 159/159 (8 nuevas de interactuables) | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 03/10/2026 19:55 | `scripts\u3.cmd play` | 13/13 | `unity/TestResults/U3/play.xml`, `play.log` |
| 03/10/2026 19:56 | `scripts\u3.cmd build` | Success, Windows x64 Mono | `unity/TestResults/U3/build.log` |
| 03/10/2026 19:56 | `scripts\u3.cmd visual` | 9 capturas nuevas revisadas | `unity/TestResults/U3/visual.log`, `Visual/*.png` |

### 03/10/2026 — paso 10 (núcleo, adelantado): partidas integradas — Claude Code

- Punto de partida/commit: `59a5f7f` (paso 7 publicado).
- Trabajo realizado: se adelanta la comparación de las cuatro partidas de `runs` para detectar cuanto antes cualquier divergencia del núcleo antes de la interfaz. Primera ejecución: tres coincidían enteras y `remedios-5min-invencible` divergía en el tick 6390; la causa era el guion web descrito en «Partidas integradas» (segunda carta abierta para siempre), no el port. Se añade `CombatRun.HoldWhileChoosing` y el arnés reproduce el bucle web; desde entonces las cuatro coinciden de principio a fin.
- Archivos/sistemas principales: `Tests/Core/IntegratedRunTests.cs` (nuevo), `Core/CombatRun.cs` y `Core/World/WorldRun.cs` (`HoldWhileChoosing`).
- Decisiones nuevas: ninguna de diseño. Tolerancia aplicada según lo acordado y documentada en «Partidas integradas».
- Pruebas realmente ejecutadas y resultado: ver «Pruebas del paso 10 (núcleo)».
- Pruebas pendientes/no ejecutadas: Play Mode con partidas aceleradas 2 personajes × 3 duraciones hasta resultados, victoria/derrota/enjambre/reinicio en escena (tras el paso 9).
- Commits creados: «Compara las cuatro partidas integradas con la web». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: ninguna nueva.
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 8 (ver «Cómo retomar»).

#### Pruebas del paso 10 (núcleo)

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 19:58–20:00 | Unity batch Edit Mode con `-testFilter IntegratedRunTests` (iteración) | diagnóstico: primero 3/4 completas; tras reproducir el guion web, 4/4 idénticas | `unity/TestResults/U3/filter.xml`, `filter.log` |
| 03/10/2026 20:01 | `scripts\u3.cmd edit` | 167/167 (8 nuevas: ruta y partida completa × 4) | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 03/10/2026 20:01 | `scripts\u3.cmd play` | 13/13 | `unity/TestResults/U3/play.xml`, `play.log` |

### 03/10/2026 — paso 8: HUD, minimapa, avisos, telegrafiado y pausa — Claude Code

- Punto de partida/commit: `db490f5`.
- Trabajo realizado: `U3/RunHud.cs` (uGUI construido en código, equivalente a `Hud.ts`): barra de experiencia, nivel, vida, oro, pasiva de Baguette, cuenta atrás `mm:ss`/`+mm:ss` (ámbar en los últimos 30 s, roja en el enjambre) y rótulo ENJAMBRE, bajas, barra del jefe (morada; roja enfurecido), armas, tomos (nombre corto) y objetos con su rareza, aviso de interacción, barra de mesa camilla/desafío, hasta tres avisos de 2,6 s (grandes o normales, como `Game.showNotice`), cartel del objeto conseguido 4,2 s (`describeItem`, con los porcentajes de los objetos especiales) y destello rojo al recibir daño. Minimapa de 112×112 (como el canvas web): terreno por bandas de altura con sombreado y sitios, una vez por mapa; marcas de interactuables descubiertos, jefe parpadeante y flecha del jugador según la cámara, a 10 Hz. `U3Game` lee `CombatRun.Events` para los avisos. Pausa técnica (`U3/RunScreens.cs`): continuar, semilla, estadísticas (`statLines`), objetos y controles; sin las opciones guardadas (U4). Telegrafiado fiel a `RunView`/`Telegraphs.ts`: franja de embestida de la rata durante su preparación, rodillo del jefe (franja) y culetazo (círculo que se llena), a 0,14 m sobre el terreno; se retira el anillo genérico anterior. El panel de estado técnico solo se ve con F3.
- Archivos/sistemas principales: `U3/RunHud.cs` y `U3/RunScreens.cs` (nuevos), `U3/U3Game.cs`, `U3/RunRenderer.cs`, `Tests/PlayMode/U3HudTests.cs` (nuevo).
- Decisiones nuevas: ninguna. Las opciones de la pausa web (idioma, volumen, etc.) dependen del guardado y quedan para U4.
- Pruebas realmente ejecutadas y resultado: ver «Pruebas del paso 8».
- Pruebas pendientes/no ejecutadas: revisión visual del HUD sin la pausa delante (paso 9).
- Commits creados: «Añade el HUD, el minimapa, los avisos y la pausa de U3». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: la escena aún arranca en la pausa (la pantalla de inicio llega en el paso 9). Texto con la fuente integrada de Unity; sin la fuente pixelada web (pulido U5).
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 9 (ver «Cómo retomar»).

#### Pruebas del paso 8

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 20:03 | `scripts\u3.cmd edit` | 167/167 | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 03/10/2026 20:08 | `scripts\u3.cmd build` y `scripts\u3.cmd visual` | build correcta; 9 capturas, pero con la pausa delante (se rehace en el paso 9) | `unity/TestResults/U3/build.log`, `visual.log` |
| 03/10/2026 20:09 | `scripts\u3.cmd play` | 14/14 (nueva `U3HudTests`) | `unity/TestResults/U3/play.xml`, `play.log` |

### 03/10/2026 — paso 9: pantallas de inicio y resultados y depuración F3 — Claude Code

- Punto de partida/commit: `16543c8`.
- Trabajo realizado: `U3Game` pasa a tener los estados de `Game.ts` (inicio → partida con pausa y cartas → resultados). Inicio técnico (`RunScreens`): Doña Remedios o Sir Baguette con su pasiva, duración 5/10/15, semilla escrita (normalizada como `normalizeSeed`: mayúsculas, A–Z y 0–9, máximo 12; si es otra, se genera ese mapa), «Nuevo mapa» con `randomSeed` (alfabeto web de 32 símbolos), mapa actual, «Jugar» y controles; la cámara gira despacio como en la web. Resultados: victoria o derrota con sus textos, tiempo, bajas, nivel, oro conseguido, baúles, daño por arma, objetos, semilla y aviso de partida con trucos; sin Calderilla ni misiones (U4). Reintentar (misma semilla), nuevo mapa y volver al inicio; la pausa también vuelve al inicio. La victoria espera 1,6 s antes de los resultados. F3: panel con FPS, frame, lógica, render (CPU de la cámara del mundo), draw calls y triángulos (`ProfilerRecorder`; «n/d» si la build no los da), resoluciones, posición, velocidad, estado y pendiente, semilla, entidades, partida y director, teclas, y las 8 acciones (solo jugando, como la web) con su aviso; todas marcan `Cheated`. La escena técnica arranca en el mapa `MAMPORRO` (o el de `-u3-seed`); la web sortea uno al abrir. Los paneles de pantalla completa se activan enteros (antes quedaban tres fondos translúcidos). Tapa de baúl con material de madera propio. `U3VisualCheck` y `scripts\u3.ps1 visual` pasan a 13 capturas: inicio, vista alta y cuatro sitios sin interfaz, combate con HUD, interactuables, telegrafiado del culetazo, pausa, cartas, resultados y reinicio.
- Archivos/sistemas principales: `U3/U3Game.cs`, `U3/RunScreens.cs`, `U3/RunRenderer.cs`, `U3/U3VisualCheck.cs`, `scripts/u3.ps1`, `Tests/PlayMode/U3ScreensTests.cs` (nuevo) y `U3HudTests.cs`.
- Decisiones nuevas: ninguna de diseño. Técnica: mapa inicial fijo `MAMPORRO` en la escena técnica para pruebas reproducibles.
- Pruebas realmente ejecutadas y resultado: ver «Pruebas del paso 9».
- Pruebas pendientes/no ejecutadas: partidas aceleradas por personaje y duración (paso 10); ensayo de rendimiento (paso 11).
- Commits creados: «Añade las pantallas de inicio y resultados y la depuración F3 de U3». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: draw calls/triángulos pueden no estar disponibles en la build normal; el efecto de abrir baúl es un anillo (la web usa partículas).
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: ver «Cómo retomar».

#### Pruebas del paso 9

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 20:19 | `scripts\u3.cmd edit` | 167/167 | `unity/TestResults/U3/edit.xml`, `edit.log` |
| 03/10/2026 20:19 | `scripts\u3.cmd play` | 16/16 (nuevas `U3ScreensTests` ×2) | `unity/TestResults/U3/play.xml`, `play.log` |
| 03/10/2026 20:20 | `scripts\u3.cmd build` | Success, Windows x64 Mono | `unity/TestResults/U3/build.log` |
| 03/10/2026 20:20 | `scripts\u3.cmd visual` | 13 capturas nuevas revisadas; 2335 píxeles distintos con/sin combate | `unity/TestResults/U3/visual.log`, `Visual/*.png` |

### 03/10/2026 — paso 10 (Play Mode): partidas aceleradas hasta resultados — Claude Code

- Punto de partida/commit: `443645d`.
- Trabajo realizado: `Tests/PlayMode/U3RunFlowTests.cs` juega en la escena real, desde la pantalla de inicio, Doña Remedios y Sir Baguette en 5, 10 y 15 minutos con `Time.timeScale` 10 (invencible y saltos de minuto de la depuración): 20 s con apariciones y bajas, salto al último minuto, llegada al enjambre (seis oleadas, armario revelado, tope 750, más de 200 vivos y cuenta `+mm:ss`; en Remedios 5 min se espera hasta los **750 vivos**), y final: Remedios abre el armario con E y derrota a la Pelusa Madre (victoria tras 1,6 s); Baguette pierde la invencibilidad y cae en el enjambre (derrota). Resultados con trucos marcados y, al reintentar, partida nueva del mismo personaje y duración sin restos (enemigos, proyectiles, gemas, oro, bajas, sucesos, jefe, enjambre, interactuables, efectos y objetos de escena). Seis partidas en 55 s. Corrige un fallo del render del paso 5 que destapó la prueba: `Box` sin rotación generaba una matriz inválida (el `==` de `Quaternion` no reconoce (0,0,0,0)); afectaba al jersey y a la fregona.
- Archivos/sistemas principales: `Tests/PlayMode/U3RunFlowTests.cs` (nuevo), `U3/RunRenderer.cs`.
- Decisiones nuevas: ninguna.
- Pruebas realmente ejecutadas y resultado: ver «Pruebas del paso 10 (Play Mode)».
- Pruebas pendientes/no ejecutadas: build y ensayo de rendimiento (paso 11).
- Commits creados: «Juega en escena 2 personajes × 3 duraciones hasta resultados». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: las partidas aceleradas usan la depuración (saltos de minuto e invencibilidad); la equivalencia sin trucos la cubren las cuatro partidas de `runs` en Edit Mode.
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: ver «Cómo retomar».

#### Pruebas del paso 10 (Play Mode)

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 20:23 | `scripts\u3.cmd play` (dos intentos) | 16/17: comprobación del temporizador con un fotograma de retraso (corregida en la prueba) y matriz inválida del jersey (fallo real del render, corregido) | `unity/TestResults/U3/play.log` |
| 03/10/2026 20:25 | `scripts\u3.cmd play` | 17/17 (`U3RunFlowTests` 55 s) | `unity/TestResults/U3/play.xml`, `play.log` |
| 03/10/2026 20:25 | `scripts\u3.cmd edit` | 167/167 | `unity/TestResults/U3/edit.xml`, `edit.log` |

## Checkpoint al terminar U3

Registrar aquí: commits publicados; sistemas portados; decisiones nuevas; pruebas exactas y resultados; build y condiciones de rendimiento; limitaciones; cambios locales no publicados; instrucciones de prueba manual; aprobación del autor o pendientes; siguiente paso propuesto (U4), sin empezarlo.
