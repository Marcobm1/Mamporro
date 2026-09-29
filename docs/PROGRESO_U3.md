# Checkpoint U3 — mundo y partida completa equivalentes

Estado: **AUTORIZADO por el autor el 29/09/2026; EN CURSO.** U4 no está autorizado.

Este archivo es el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U3 siga activo. Debe actualizarse después de cada paso completado y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md); reglas comunes: [`INSTRUCCIONES_PROYECTO.md`](INSTRUCCIONES_PROYECTO.md).

## Cómo retomar

- **Último commit publicado:** ver `git log -1 origin/claude/zen-pasteur-674ik0`. U2 cerrado y aprobado en `176423d`.
- **Paso actual:** 4 del plan (render del mundo en una escena nueva `U3_Partida`).
- **Terminado y verificado:** pasos 1–3 y la física del jugador del paso 5 (portada ya porque la colisión del mundo la necesita). Edit Mode 118/118 y Play Mode 10/10 el 29/09/2026 22:00. **`u3-world.json` está congelada**: la usan las pruebas C# desde las 21:59; no regenerarla.
- **A medias:** nada.
- **Sin commit a propósito:** `unity/ProjectSettings/ProjectSettings.asset` (identificador de nube), `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset` y `URPProjectSettings.asset`: no publicar nunca. Unity reescribe con espacios algunos ajustes al abrir el proyecto (`RetroPipeline.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `GraphicsSettings.asset`, `ProjectAuditorSettings.asset`): si solo cambian espacios o finales de línea, restaurarlos antes de hacer commit.
- **Siguiente paso exacto:** crear `Assets/Mamporro/U3/` (ensamblado `Mamporro.U3`) con un renderizador del mundo que construya por código el terreno (colores por altura/pendiente como `src/world/TerrainMesh.ts`), las piezas de props (`Part`: box/cylinder/cone/ico/dodeca con el orden de giro YXZ), la decoración, la cobertura del suelo y los interactuables, con el material retro de U1, y una escena `Assets/Mamporro/U3/U3_Partida.unity` generada por un `U3Project.cs` al estilo de `U2Project.cs`, sin tocar las escenas de U1 y U2.

Comprobar el estado desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch
git log --oneline -8
scripts\u2.cmd edit
scripts\u2.cmd play
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

## Alcance autorizado

Mundo y partida completa equivalentes a la web aprobada `0505b1690656d15188860157612455639820fe1f`:

- Mundo procedural: heightfield de 320 m con bancales; sites (casas, templos, granjas, pozos) con props y colisionadores; decoración con colisión; cobertura del suelo y fauna decorativa (desactivables si pesan).
- Colisiones del jugador y de la horda sobre el mundo real (pendiente máxima 48°, los enemigos suben hasta 1,6 m, reciclado lejano).
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
4. Render del mundo con el material retro de U1 en una escena nueva `Assets/Mamporro/U3/U3_Partida.unity`; U1 y U2 conservan sus escenas y builds.
5. Física del jugador y colisiones de la horda sobre el mundo real. (Física portada y probada en el paso 3; falta conectarla a la escena y a la horda.)
6. Director completo.
7. Interactuables, armario, jefe y victoria.
8. HUD, minimapa, avisos, telegrafiado y pausa.
9. Pantallas de inicio y resultados; depuración con `run.cheated`.
10. Integración: partidas deterministas contra la cronología web; Play Mode con partidas aceleradas (2 personajes × 3 duraciones) hasta resultados; victoria, derrota, enjambre y reinicio sin restos.
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

## Checkpoint al terminar U3

Registrar aquí: commits publicados; sistemas portados; decisiones nuevas; pruebas exactas y resultados; build y condiciones de rendimiento; limitaciones; cambios locales no publicados; instrucciones de prueba manual; aprobación del autor o pendientes; siguiente paso propuesto (U4), sin empezarlo.
