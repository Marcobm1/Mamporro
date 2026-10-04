# Checkpoint U3 — mundo y partida completa equivalentes

Estado: **IMPLEMENTADO, VERIFICADO Y APROBADO MANUALMENTE por el autor el 04/10/2026 como base funcional de la migración.** U4 autorizado para implementación el 04/10/2026. Continuidad en [PROGRESO_U4](PROGRESO_U4.md).

Este archivo es el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U3 siga activo. Debe actualizarse después de cada paso completado y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md); reglas comunes: [`INSTRUCCIONES_PROYECTO.md`](INSTRUCCIONES_PROYECTO.md).

## Cómo retomar

- **Último cierre técnico previo a la aprobación:** `83246ebfd873b0c5d23611a1d557f83096180217`, «Documenta el diagnóstico de los picos de U3». Cierre documental: `121b0c2`; última implementación: `d09b5d0`. Aprobación manual del autor recibida el 04/10/2026.
- **Paso actual:** ninguno. U3 cerrado (pasos 1–12) y aprobado manualmente el 04/10/2026. Las instrucciones manuales se conservan para regresión.
- **Terminado y verificado:** pasos 1–12 (ensayo de partida real en la build: ver «Ensayo de rendimiento U3»). Las cuatro partidas de `runs` coinciden con la web de principio a fin (`IntegratedRunTests`) y en escena se juegan 2 personajes × 3 duraciones hasta resultados (`U3RunFlowTests`). El 03/10/2026 (paso 11): Edit Mode **167/167**, Play Mode **17/17**, build Windows x64 Mono y ensayo 8/8 puntos con `validRender`. **`u3-world.json` sigue congelada**, sin regeneración.
- **A medias:** nada. Las seis propuestas técnicas en `DECISIONES.md` no han recibido confirmación individual; su clasificación para U4 está en `PROGRESO_U4.md`.
- **Revalidación del relevo, 04/10/2026:** Edit Mode 167/167, Play Mode 17/17 y verificadores U0/U2/U3 correctos, sin cambios de código. Cierre publicado; diagnóstico de CSV completado sin atribuir causa a los picos ni aplicar una corrección especulativa.
- **Sin commit a propósito:** `unity/ProjectSettings/ProjectSettings.asset` (configuración local de nube), `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset` y `URPProjectSettings.asset`: no publicar. También se conservan fuera del índice los cambios de espacios/EOL de `RetroPipeline.asset`, `UniversalRenderPipelineGlobalSettings.asset` y `GraphicsSettings.asset` (el autor confirmó el 03/10/2026 que se tratan igual). No limpiar ni restaurar estos archivos automáticamente.
- **Siguiente paso exacto:** continuar U4 según `PROGRESO_U4.md`; plan y política de importación ya aprobados.

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

## Ensayo de rendimiento U3 (paso 11)

`scripts\u3.cmd benchmark` (build con `-u3-benchmark`, pantalla completa exclusiva, D3D11, Mono, sin desarrollo, sin vsync ni límite de FPS, interna 360): partida real Remedios, 10 min, `MAMPORRO`, director activo, invulnerable de ensayo, primera carta siempre, circuito de 8 s; avanza sin medir hasta 10 s antes de cada punto y mide 10 s + 30 s dibujando mundo, combate y HUD. Equipo: Ryzen 7 7700X, RTX 4070 Ti SUPER, ~32 GB. Ejecutado el 03/10/2026 20:28–20:34.

| Salida | Punto (tiempo de partida) | FPS medios | P95 ms | P99 ms | Máx. ms | >16,7 ms | Enemigos | Tick medio ms | CPU ms |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1920×1080 | min2 (110–148 s) | 2558,6 | 0,57 | 0,75 | 12,31 | 0 | 11–19 | 0,048 | 0,39 |
| 1920×1080 | min5 (290–329 s) | 2099,5 | 0,70 | 0,88 | 61,9 | 4 | 272–307 | 0,302 | 0,48 |
| 1920×1080 | min9 (530–570 s) | 1932,3 | 0,79 | 1,05 | 5,02 | 0 | 323–530 | 0,428 | 0,52 |
| 1920×1080 | enjambre (630–670 s) | 1615,9 | 0,94 | 1,65 | 5,36 | 0 | 746–750 | 1,037 | 0,62 |
| 2560×1440 | min2 | 2721,7 | 0,46 | 0,61 | 6,07 | 0 | 11–19 | 0,045 | 0,37 |
| 2560×1440 | min5 | 2157,9 | 0,67 | 0,85 | 105,54 | 2 | 262–307 | 0,296 | 0,46 |
| 2560×1440 | min9 | 1900,5 | 0,73 | 1,11 | 6,02 | 0 | 481–529 | 0,591 | 0,53 |
| 2560×1440 | enjambre | 1667,9 | 0,85 | 1,57 | 6,51 | 0 | 747–750 | 0,986 | 0,60 |

- **Objetivo cumplido en este equipo:** 60 FPS con ~300 enemigos en el mapa real (minuto 5: >2000 FPS) y también en el enjambre con 750 vivos (>1600 FPS, P99 <1,7 ms). No hizo falta perfilar.
- **Picos aislados:** en el minuto 5, 2–4 fotogramas superan 16,7 ms (máximo 62 y 106 ms) de unos 60 000; los demás puntos no tienen ninguno. Diagnóstico del 04/10 en la entrada inferior: ningún tick medido llega a esos tiempos; no coinciden con oleadas/élite ni directamente con la captura. Causa sin aislar; GC/interfaz siguen siendo hipótesis, no conclusiones. No bloquea el cierre.
- **Límites:** GPU no disponible (`-1`) y GC por fotograma no disponible en la build normal, como en U1/U2. Memoria Unity al final, no pico. Un único equipo potente; no representa requisitos mínimos. La invulnerabilidad y la primera carta automática son condiciones de ensayo. Capturas `u3-<salida>-<punto>.png` revisadas (la del enjambre muestra 750 vivos, HUD y «+00:43 ENJAMBRE»).
- Informes: `unity/TestResults/U3/u3-<ancho>x<alto>-<punto>-*.json`, `.csv` y `-ticks.csv`; logs `player-1920x1080.log`, `player-2560x1440.log`.

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
11. **Hecho, 03/10/2026.** Build Windows x64 Mono ejecutada y ensayo de partida real: minutos 2, 5 y 9 y enjambre con 750 vivos, a 1080p y 1440p, con las mismas condiciones y `validRender` que U1/U2. Objetivo: 60 FPS con 300 enemigos en el mapa real; si el enjambre no llega, perfilar y documentar antes de cerrar.
12. **Cierre documental completado y base verificada el 04/10/2026.** Borrador del 03/10 recuperado; publicación y parada registradas en la entrada de cierre de esta sesión.

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

### 03/10/2026 — paso 11: build y ensayo de partida real — Claude Code

- Punto de partida/commit: `19562c2`.
- Trabajo realizado: `U3/U3Benchmark.cs` (`-u3-benchmark`, `-u3-output`) y acción `benchmark` en `scripts\u3.ps1` (vuelve al lanzador), con el mismo formato de informe que U1/U2. `U3Game` resuelve cartas solas en el ensayo (`AutoChoose`) y registra el tiempo de cada tick. README de Unity actualizado con la partida completa, controles, pantallas y comandos. Propuestas de decisiones técnicas de U3 añadidas a `DECISIONES.md`, pendientes de confirmación del autor. Se corrige una comprobación frágil de `U3HudTests` (un baúl puede descubrirse en el primer tick).
- Archivos/sistemas principales: `U3/U3Benchmark.cs` (nuevo), `U3/U3Game.cs`, `scripts/u3.ps1`, `unity/README.md`, `docs/DECISIONES.md`, `Tests/PlayMode/U3HudTests.cs`.
- Decisiones nuevas: ninguna de diseño; técnicas propuestas en `DECISIONES.md`.
- Pruebas realmente ejecutadas y resultado: ver «Ensayo de rendimiento U3» y la tabla siguiente.
- Pruebas pendientes/no ejecutadas: prueba manual del autor.
- Commits creados: «Añade el ensayo de partida real de U3». Push: ver «Cómo retomar».
- Estado del árbol al terminar: solo los ajustes Unity excluidos.
- Errores/limitaciones conocidas: picos aislados en el minuto 5 sin causa aislada; GPU/GC no disponibles en la build.
- Decisiones pendientes del autor: confirmar las decisiones técnicas propuestas de U3.
- Siguiente paso exacto: paso 12.

#### Pruebas del paso 11

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 03/10/2026 20:28 | `scripts\u3.cmd build` | Success, Windows x64 Mono | `unity/TestResults/U3/build.log` |
| 03/10/2026 20:28–20:34 | `scripts\u3.cmd benchmark` | 8/8 puntos con `validRender` y resolución correcta (tabla de arriba) | `unity/TestResults/U3/u3-*.json`, `player-*.log` |
| 03/10/2026 20:36 | `scripts\u3.cmd edit` | 167/167 | `unity/TestResults/U3/edit.xml` |
| 03/10/2026 20:36 | `scripts\u3.cmd play` | 16/17: comprobación frágil del minimapa (corregida) | `unity/TestResults/U3/play.log` |
| 03/10/2026 20:38 | `scripts\u3.cmd play` | 17/17 | `unity/TestResults/U3/play.xml`, `play.log` |

### 03/10/2026 — paso 12: borrador local de cierre de U3 — Claude Code

- Punto de partida/commit: `d09b5d0`.
- Trabajo registrado por la sesión anterior: verificación de que `src/` coincide con `0505b16` y de que referencias y código U2 no cambiaron; verificadores U0/U2/U3 correctos. Borrador de checkpoint de cierre con instrucciones de prueba manual y cambios en `ESTADO_ACTUAL.md`, `CLAUDE.md` y `AGENTS.md`. La auditoría del 04/10 confirma que README raíz todavía no se había actualizado.
- Pruebas realmente ejecutadas y resultado: `git diff --exit-code 0505b16 -- src` y `git diff --exit-code b21160c -- unity/Docs/Reference unity/Assets/Mamporro/U2` sin diferencias; `node scripts\unity-reference.mjs`, `node scripts\unity-reference-u2.mjs` y `node scripts\unity-reference-u3.mjs` coinciden (03/10/2026 20:40). Sin cambios de código en este paso.
- Corrección factual de la auditoría del 04/10: **no se creó ni publicó el commit de cierre**. HEAD local y remoto seguían en `d09b5d0`; este borrador y los otros tres documentos estaban sin commit, además de los ajustes Unity excluidos. Las pruebas de esta entrada son históricas, no ejecuciones de la sesión de relevo.
- Decisiones pendientes del autor: aprobación de U3 y confirmación de las decisiones técnicas propuestas.
- Siguiente paso exacto: esperar al autor; no empezar U4.

### 04/10/2026 — recuperación y cierre documental del paso 12 — Codex

- **Punto de partida:** `d09b5d09a091c02b3a02e2a0f88a9a696e5a3972`, HEAD local y origin iguales tras fetch; ahead 0 / behind 0. Sin commits inéditos. Se leyeron el diff completo y los dos archivos sin seguimiento antes de editar. Se conserva intacto el stash antiguo `codex: cabeceras CLAUDE/ENTORNO (sustituidas por 2b67ea5)`; no se aplica ni se borra.
- **Trabajo heredado y conservado:** borrador del paso 12 en `AGENTS.md`, `CLAUDE.md`, `docs/ESTADO_ACTUAL.md` y este checkpoint. No había WIP de código. El borrador decía que había commit/push y README actualizado; Git demuestra que aún no. Se corrigen esas afirmaciones manteniendo el registro histórico de pruebas atribuido a la sesión anterior.
- **Trabajo completado:** checkpoint final con commits, alcance, pruebas diferenciadas por fecha, condiciones de build/ensayo, limitaciones e instrucciones manuales en CMD sin pull. Se actualizan las entradas de los dos agentes, estado global, README raíz/Unity y encabezados de migración/hoja de ruta que todavía prohibían empezar U3. Se conserva la implementación de los pasos 1–11.
- **Decisiones nuevas:** ninguna. Las seis propuestas técnicas de `DECISIONES.md` permanecen pendientes de confirmación del autor; no se modifica ese archivo. U4 no autorizado.
- **Pruebas nuevas realmente ejecutadas:** tabla inferior. Build, visual y benchmark del 03/10 se revisan como evidencia histórica, sin atribuirlos a esta sesión ni a una prueba jugable manual.
- **Pruebas pendientes:** prueba manual y aprobación del autor. Después de publicar este cierre se revisarán con tiempo limitado los CSV del minuto 5; no bloquea la entrega.
- **Commit y push realizados:** `121b0c26b30f94a5e2008cb02bc34072c1514ecd`, «Completa el cierre documental de U3», exclusivamente ocho Markdown. `git diff --cached --check` correcto; el check global solo señala espacios en los ajustes Unity excluidos. Tras `git fetch origin`, remoto seguía en `d09b5d0`; push normal `d09b5d0..121b0c2` y `git ls-remote origin refs/heads/claude/zen-pasteur-674ik0` confirmaron el hash publicado. Árbol posterior: únicamente los siete ajustes Unity excluidos (cinco modificados y dos sin seguimiento).
- **Estado local/exclusiones:** cuatro ajustes ProjectSettings enumerados en «Cómo retomar» y espacios/EOL de los tres assets allí enumerados, conservados sin publicar ni restaurar. `Builds` y `TestResults` ignorados. Ningún cambio de código, `src/` ni referencias.
- **Limitaciones:** picos aislados del minuto 5 sin causa confirmada; no se altera el balance ni se infiere una causa de GC sin medición. Aprobación y decisiones técnicas pendientes del autor.
- **Siguiente paso exacto:** publicar el cierre, diagnóstico acotado del minuto 5 y detenerse para la prueba manual; no empezar U4.

| Fecha/hora (Europe/Madrid) | Comando CMD desde la raíz | Resultado | Registro |
| --- | --- | --- | --- |
| 04/10/2026 01:09 | `scripts\u3.cmd edit` | 167/167, 0 fallos, exit 0 | `unity/TestResults/U3/edit.xml`, `edit.log` (XML: 23:09:10–23:09:23 UTC del 03/10) |
| 04/10/2026 01:09–01:10 | `scripts\u3.cmd play` | 17/17, 0 fallos, exit 0 | `unity/TestResults/U3/play.xml`, `play.log` (XML: 23:09:50–23:10:55 UTC del 03/10) |
| 04/10/2026 01:11–01:13 | `node scripts\unity-reference.mjs` | datos y medios U0 coinciden, exit 0 | consola de la sesión; referencia leída `unity/Docs/Reference/baseline.json` |
| 04/10/2026 01:13–01:16 | `node scripts\unity-reference-u2.mjs` | U2 coincide con la web aprobada, exit 0 | consola de la sesión; referencia leída `unity/Docs/Reference/u2-combat.json` |
| 04/10/2026 01:16–01:18 | `node scripts\unity-reference-u3.mjs` | U3 coincide con la web aprobada, exit 0; sin regeneración | consola de la sesión; referencia leída `unity/Docs/Reference/u3-world.json` |
| 04/10/2026 01:10 | `git diff --exit-code 0505b1690656d15188860157612455639820fe1f -- src` y `git diff --exit-code b21160c -- unity/Docs/Reference unity/Assets/Mamporro/U2` | sin diferencias | consola de la sesión |

### 04/10/2026 — diagnóstico acotado de los picos del minuto 5 — Codex

- **Punto de partida:** cierre `121b0c2` ya publicado y verificado. Diagnóstico después del cierre, dentro del límite de 30–40 minutos solicitado; revisión de datos/código y registro 01:19–01:26, sin modificar runtime.
- **Fuentes leídas:** los ocho informes originales `unity/TestResults/U3/u3-*-20261003T*.json` y sus CSV por fotograma/tick, ambos `player-*.log`; `U3Benchmark.cs`, `U3Game.cs`, `RunHud.cs`, `Core/World/Director.cs` y `Core/Catalog.cs`.
- **Comando ejecutado:** `node unity\TestResults\U3\diagnostico-min5-20261004.cjs` (04/10/2026 01:20, exit 0). Script y salida `unity/TestResults/U3/diagnostico-min5-20261004.json` son artefactos locales ignorados, conservados. El análisis lee los CSV originales sin alterarlos: acumula `ms / 1000` por fila para situar cada fotograma, selecciona `ms > 1000/60`, ordena los ticks por duración y calcula el máximo entre 2,9–3,5 s para contrastar la captura. No es una nueva ejecución del ensayo.

| Resolución / CSV del 03/10 | Fotograma (índice CSV) | Intervalo desde inicio de medición (s) | Duración (ms) |
| --- | --- | --- | --- |
| 1080p / `u3-1920x1080-min5-20261003T183024349.csv` | 62126 | 29,4508–29,4754 | 24,6271 |
| mismo | 62130 | 29,4781–29,5400 | 61,8965 |
| mismo | 62133 | 29,5649–29,5864 | 21,5521 |
| mismo | 62134 | 29,5864–29,6055 | 19,0328 |
| 1440p / `u3-2560x1440-min5-20261003T183316584.csv` | 10930 | 5,0228–5,1283 | 105,5353 |
| mismo | 10933 | 5,1471–5,1661 | 18,9627 |

- **Tick largo:** en 1080p hay 62 987 fotogramas y 1766 ticks; máximo de tick **0,5520 ms** (índice 1432). En 1440p, 64 737 fotogramas y 1800 ticks; máximo **1,5301 ms** (índice 370). Ningún intervalo cronometrado `Session.Step + CombatView.Step` explica por sí solo los picos. `Run.Choose` automático y UI están fuera de ese cronómetro: no se descartan por esta prueba. Los otros seis puntos no tienen fotogramas >16,67 ms.
- **Oleada/élite:** no corresponde una especial ni élite nueva dentro del intervalo de partida 290–330,1 s del punto min5 en duración 10. Las especiales anterior/siguiente son 270/360 s; élites 240/360 s. No hubo saltos de depuración ni interactuables durante ese intervalo. Quedan apariciones y combate ordinarios.
- **Captura:** `CaptureScreenshot` se solicita a los 3 s de medición. En 2,9–3,5 s, máximos **3,6846 ms (1080p)** y **6,4195 ms (1440p)**; los picos grandes ocurren bastante después y en instantes distintos entre resoluciones. Se descarta la coincidencia directa; no se prueba ausencia de trabajo diferido.
- **Nivel/carta/interfaz:** el ensayo elige automáticamente y no abre el panel de cartas (`AutoChoose`); no se registra el instante de cada nivel/elección. `RunHud` crea cadenas y tres `StringBuilder` por fotograma mediante `Chips`, aunque evita asignar `Text.text` si el contenido no cambia. `RefreshStatus` construye el texto de F3 cada 0,1 s incluso con el panel oculto. Son costes observables por lectura, pero **no hay evidencia temporal que atribuya los picos a ellos o al GC**. Los textos de HUD se crean en `Build`, antes de medir; actualizar cadenas/mallas es otra operación.
- **Límite de los CSV:** fotogramas solo tienen índice/ms/CPU/GPU; ticks, índice/ms. No comparten frame ID ni tiempo de partida ni lista de eventos. `runTimeFrom=290` incluye calentamiento, mientras los CSV empiezan después. No se debe convertir el índice de tick en un frame exacto ni alinear CPU/GPU por fila: los timings de Unity pueden llegar con retraso. En estos datos los máximos CPU aparecen varios fotogramas después. GC no disponible y GPU agregada invalidada en los informes originales.
- **Decisión y resultado:** causa sin aislar con la evidencia disponible. Se conserva como limitación conocida, sin optimización especulativa ni cambio de reglas. No se repiten Edit/Play/build/min5 porque no se ha modificado código; siguen siendo nuevas de esta sesión 167/167 y 17/17, con verificadores U0/U2/U3 correctos. No se modifica `src/`, referencias, scripts ni `DECISIONES.md`.
- **Para una investigación posterior, si se solicita:** instrumentar en una build de perfilado la relación frame/tick/tiempo y eventos de nivel/elección; medir por separado `Choose`, actualización de HUD, canvas y GC, y reproducir min5 en las dos resoluciones. Los CSV actuales no permiten resolver esa atribución retrospectivamente. No bloquea la prueba manual de U3 ni autoriza U4.
- **Entrega:** documentación de este diagnóstico en commit «Documenta el diagnóstico de los picos de U3», tras revisar el índice; fetch antes del push normal y comprobación del remoto. Solo se versiona este Markdown; artefactos del diagnóstico y siete ajustes Unity permanecen locales. El hash propio se consulta en Git para evitar un registro circular.
- **Siguiente paso exacto:** detenerse y esperar la prueba manual/aprobación del autor y su confirmación de las seis propuestas técnicas. Ninguna pieza de código queda a medias.

### 04/10/2026 — aprobación manual y apertura de planificación U4 — Codex

- **Punto de partida:** local y remoto `83246ebfd873b0c5d23611a1d557f83096180217`, ahead 0 / behind 0; solo siete ajustes Unity excluidos, sin WIP ni commits inéditos.
- **Aprobación del autor:** U3 está bien por ahora y queda aprobada como base funcional de la migración. No implica confirmar individualmente las seis propuestas técnicas.
- **Trabajo:** cierre formal en documentación y apertura de `PROGRESO_U4.md` solo para planificación. Revisados save v3, migraciones v1/v2, meta, tienda, misiones, opciones, i18n, selección y componentes Unity reutilizables.
- **Archivos:** checkpoints, estado, decisiones, migración, hoja de ruta, README y guías de entrada/continuidad. Sin cambios de runtime.
- **Pruebas:** ninguna suite nueva; auditoría Git y revisión documental. Las pruebas detalladas en el cierre técnico son históricas, no repetidas en esta sesión.
- **Limitaciones:** mejoras visuales/de diseño aplazadas después de la migración, no fallos bloqueantes de U3. Picos aislados del minuto 5 aún sin causa aislada; se conserva el diagnóstico y no bloquea avanzar.
- **Decisiones:** U4 solo planificación; excepción futura a `src/` limitada al exportador validado. Seis propuestas clasificadas, sin atribuir confirmación individual.
- **Commit/push:** unidad «Registra la aprobación de U3 y prepara el plan de U4»; hash consultable con `git log -1 --format="%H %s" -- docs/PROGRESO_U4.md`. Publicación normal tras fetch y verificación del remoto.
- **Árbol/exclusiones:** conservar sin publicar/restaurar los siete ajustes Unity de «Cómo retomar».
- **Siguiente paso exacto:** esperar la respuesta del autor al plan U4 y a la única duda sobre importación parcialmente inválida. No implementar todavía.

## Checkpoint al terminar U3

Cierre documental recuperado del borrador de Claude Code del 03/10/2026 y completado por Codex el 04/10/2026. **Implementado y verificado; aprobado manualmente por el autor el 04/10/2026.**

- **Commits publicados de U3 al recibir el relevo:** `4bce54d` (apertura), `525fb1e` (referencia), `522d8de` (núcleo del mundo), `272ce95` (escena y render), `4de718d` (combate en el mundo), `b21160c` (checkpoint del paso 5), `c42e22d` (director), `59a5f7f` (interactuables, armario y victoria), `db490f5` (partidas integradas), `16543c8` (HUD, minimapa, avisos y pausa), `443645d` (inicio, resultados y F3), `19562c2` (partidas aceleradas en escena), `d09b5d0` (ensayo de rendimiento). Publicación del cierre: entrada del 04/10.
- **Sistemas portados:** mundo procedural, física del jugador, combate sobre el mundo, director completo (tabla, curva, oleadas, élites, enjambre, 5/10/15), interactuables (baúles, mesas camilla, tótems, armario → Pelusa Madre → victoria), HUD, minimapa, avisos, telegrafiado, pausa, inicio, resultados y depuración F3.
- **Equivalencia:** las cuatro partidas de `runs` coinciden con la web de principio a fin (ver «Partidas integradas»); director, apariciones, formaciones, precios e interactuables coinciden con sus secciones de `u3-world.json`. `src/`, `baseline.json`, `u2-combat.json` y `u3-world.json` sin cambios (verificadores U0/U2/U3 correctos el 03/10/2026 20:40).
- **Pruebas nuevas del relevo (04/10):** Edit Mode 167/167 y Play Mode 17/17; comandos, horas y rutas en la entrada de sesión inferior. **Evidencia histórica del 03/10:** `scripts\u3.cmd build` a las 20:28, Success, Windows x64 Mono (`unity/TestResults/U3/build.log`); `scripts\u3.cmd visual` a las 20:20, 13 capturas revisadas y 2335 píxeles con/sin combate (`unity/TestResults/U3/visual.log`, `Visual/*.png`); `scripts\u3.cmd benchmark` 20:28–20:34, 8/8 puntos (`unity/TestResults/U3/u3-*.json`, `.csv`, `-ticks.csv`, `player-*.log`). No se repiten build/visual/ensayo para este cambio exclusivamente documental. El visual es anterior al ajuste del jersey/fregona del paso 10; la build y el ensayo son posteriores.
- **Build y condiciones:** `unity/Builds/U3/Mamporro-U3.exe` con toda su carpeta de datos/DLL. Unity 6000.6.3f1, Windows x64 Mono normal, D3D11, pantalla completa exclusiva 1920×1080 y 2560×1440, interna 360, dither/snap, VSync 0 y FPS ilimitados. Ryzen 7 7700X, RTX 4070 Ti SUPER, ~32 GB. Remedios, 10 min, `MAMPORRO`, director activo, invulnerable, primera carta automática, circuito de 8 s; avance sin medir y 10 s de calentamiento + 30 s de medida por punto. No es una partida manual ni acredita rendimiento en otros equipos.
- **Rendimiento:** objetivo de 60 FPS con 300 enemigos cumplido con mucho margen en el equipo de referencia; enjambre de 750 vivos >1600 FPS (ver «Ensayo de rendimiento U3»).
- **Decisiones:** ninguna nueva de diseño; decisiones técnicas propuestas en `DECISIONES.md` («U3: decisiones técnicas de implementación»), **pendientes de confirmación del autor**.
- **Limitaciones:** presentación técnica (modelos de cajas, avatar provisional, fuente integrada, efecto de baúl como anillo, sin partículas ni audio); sin menús finales, Calderilla, tienda, misiones ni guardado (U4); picos aislados en el minuto 5 del ensayo; GPU/GC no disponibles en la build; la escena técnica arranca en el mapa `MAMPORRO`.
- **Cambios locales no publicados:** solo los ajustes Unity excluidos de «Cómo retomar».

### Instrucciones de prueba manual (autor)

Con el Editor de Unity cerrado, desde la raíz en CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
start "" "unity\Builds\U3\Mamporro-U3.exe" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080
```

La build local ya existe. Si falta o necesitas reconstruirla, ejecuta primero `scripts\u3.cmd build` con el Editor cerrado y después el comando `start`. Conserva toda la carpeta `Builds\U3`, no solo el EXE. No hace falta hacer pull ni limpiar el árbol para probar. Alt+F4 cierra la build.

Opcional: `scripts\u3.cmd edit`, `scripts\u3.cmd play`, `scripts\u3.cmd visual` y `scripts\u3.cmd benchmark` repiten las comprobaciones automáticas; el ensayo tarda unos 6 minutos y abre la build a pantalla completa. Los resultados quedan en `unity\TestResults\U3`.

1. **Inicio, ambos personajes y tres duraciones:** inicia las seis combinaciones Remedios/Baguette × 5/10/15. Comprueba selección, arma inicial (Chancla/Barra), cuenta 05:00/10:00/15:00 y mapa. Para pasar a la siguiente combinación, Esc → volver al inicio. Remedios ralentiza cerca; Baguette carga su escudo tras 8 s sin daño.
2. **Semilla:** escribe `prueba 1` (se normaliza a `PRUEBA1`); confirma en pausa y resultados. Repite con la misma semilla: terreno e interactuables en los mismos sitios. «Nuevo mapa» cambia semilla y mapa; vacía conserva el mapa actual, inicialmente `MAMPORRO`.
3. **Movimiento y combate:** WASD/ratón, Espacio mantenido para salto, Mayús/C para deslizarse. Recorre terreno, pendientes y obstáculos durante el combate: enemigos persiguen, chocan y frenan al atravesarlos; armas automáticas dañan/matan, se recoge XP/oro y se sube de nivel. Comprueba las pipas de paloma y el daño de contacto.
4. **Cartas:** la partida se detiene; elige con clic o 1–4 tras la espera inicial de 0,4 s. Prueba R (volver a tirar), X (saltar), B y una carta (descartar); verifica usos limitados y que continúa el combate. Para provocar una elección usa F3, 2; cierra F3 antes de seguir jugando si no necesitas trucos.
5. **Baúles y minimapa:** descubre marcas al explorar (aprox. 30 m; armario a 22 m). Acércate a un baúl y pulsa E: sin oro avisa; con oro entrega objeto, abre tapa y sube el próximo precio (15, 30, 49…). F3, 7 añade oro para QA; F3, 8 revela el mapa para localizar el resto.
6. **Mesas camilla:** entra en el círculo; observa progreso, sal para comprobar descarga y vuelve hasta completar 9 s de carga. Elige una de tres bendiciones; no debe ofrecer Reroll/Saltar/Descartar. La mesa usada no concede otra recompensa.
7. **Tótems:** pulsa E; comprueba aviso, desafío de 45 s, presión adicional y recompensa de objeto al completarlo. No puede activarse otra vez. Con invencibilidad (F3, 1) puedes revisar el recorrido sin morir.
8. **Oleadas, élite y avisos:** en 10 min, especiales a 1:30/3:00/4:30/6:00/7:30/9:00 transcurridos y rata élite cada 2 min de dificultad; en 5 min ocurren al doble de ritmo y en 15 a dos tercios. Observa avisos, formaciones y franja previa de embestida. F3, 3 adelanta un minuto de partida si quieres acelerar QA; no sustituye jugar a ritmo normal.
9. **Pausa:** Esc detiene tiempo/combate y muestra semilla, estadísticas y objetos. Continúa; alterna fuera y dentro de la ventana para comprobar la pausa al perder foco. La pausa técnica no tiene aún las opciones persistentes de U4.
10. **Armario, jefe y victoria:** encuentra el armario (o revela con F3, 8) y pulsa E. Pelusa Madre aparece detrás: barra de vida, rodillo con franja, culetazo con círculo y estornudo; con media vida se enfurece. Derrotarla lleva a victoria tras 1,6 s. F3, 6 invoca un jefe para QA, pero no prueba abrir el armario; F3, 5 permite comprobar resultados rápidamente, pero no sustituye combatirlo.
11. **Enjambre y derrota:** en otra partida deja acabar 5/10/15 sin matar al jefe (puedes adelantar con F3, 3 e invencibilidad). Aparecen ENJAMBRE, cuenta `+mm:ss`, aviso y armario revelado; crece la horda, hasta 750 vivos. Desactiva invencibilidad y recibe daño hasta la derrota. Comprueba resultados de ambos desenlaces: tiempo, bajas, nivel, oro, baúles, objetos y daño por arma; sin Calderilla.
12. **F3:** abrir solo el panel no marca trucos. Con él abierto, jugando y sin carta/pausa, 1 invencible, 2 nivel, 3 +minuto, 4 +100 enemigos, 5 matar todo, 6 jefe, 7 oro y 8 revelar. Una acción marca toda esa partida incluso después de apagar el truco; resultados indican «Partida con trucos de debug».
13. **Reinicio fuerte:** tras acumular enemigos, disparos, daño, objetos y niveles, prueba Reintentar desde resultados: mismo personaje/duración/semilla, vida y nivel iniciales, oro/bajas a cero, sin jefe ni efectos anteriores; el director empieza de nuevo (pueden aparecer enemigos nuevos enseguida). Repite tras victoria y derrota, prueba Nuevo mapa y Volver al inicio; F8 vuelve al inicio con el mapa actual. No debe arrastrarse invencibilidad, carta, desafío, avisos ni objetos de la sesión anterior.
14. **Presentación técnica:** F1 interna 240/360/480, F2 dithering, F9 ajuste de vértices, F6 ventana 1080p/1440p; comprueba legibilidad de cartas/HUD y que se dibujan enemigos, armas y proyectiles. Los FPS instantáneos de F3 no sustituyen el ensayo registrado.

La prueba manual del autor aprueba U3 como base funcional. No equivale a confirmar individualmente las seis propuestas técnicas.

- **Aprobación del autor:** recibida el 04/10/2026.
- **Siguiente bloque:** U4 (meta, UI, opciones y traslado de guardados), autorizado para implementación el 04/10/2026; véase `PROGRESO_U4.md`.
