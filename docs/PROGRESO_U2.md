# Checkpoint U2 — núcleo y combate equivalentes

Estado: **IMPLEMENTADO Y VERIFICADO EN LOCAL el 29/09/2026; pendiente de la prueba manual y la aprobación del autor.** No empezar U3.

U1 fue probado y aprobado manualmente. U2 es el único bloque autorizado. **No empezar U3.**

Este archivo es además el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U2 siga activo. Debe actualizarse después de avances relevantes y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md).

## Cómo retomar

Actualizar esta sección después de cada paso completado.

- **Último commit publicado:** ver `git log -1 origin/claude/zen-pasteur-674ik0`. Base verificada de U2 = núcleo + integración + documentación de decisiones (pasos 1–2 del plan). Ramas locales de respaldo: `respaldo/u2-local-1e66e49` (commit original de Codex) y `respaldo/u2-antes-rebase-2` (antes del rebase sobre `dd93c81`).
- **Antes de cada push:** `git fetch origin`; si el remoto ha cambiado, parar y avisar al autor en lugar de reintentar.
- **Paso actual:** U2 cerrado a la espera del autor (plan de cierre completo, pasos 1–8).
- **Terminado y verificado:** todo el alcance de U2 (ver «Cierre de U2»). Edit Mode 88/88, Play Mode 10/10, build Windows x64 Mono, comprobación visual en build y ensayo de 8 condiciones válidas; web 202/202, typecheck y build; referencias U0 y U2 íntegras.
- **A medias:** nada del alcance U2. Falta la prueba manual del autor con teclado y ratón reales.
- **Sin commit a propósito:** `unity/ProjectSettings/ProjectSettings.asset` (identificador de proyecto en la nube y organización), `PackageManagerSettings.asset` y `URPProjectSettings.asset` (preferencias locales del Editor): no publicar nunca. `ProjectAuditorSettings.asset`: Unity lo reescribe con espacios al abrir; no publicar. Retoques de Codex en `README.md`, `docs/MIGRACION_UNITY.md`, `docs/PROGRESO_U1.md`, `unity/Docs/HOJA_DE_RUTA.md` y `unity/README.md`: se revisan en el commit documental de cierre (paso 8).
- **Copias de seguridad locales (ignoradas por Git):** `qa-results/u2-audit/` (parche completo `worktree.patch`, lista y `untracked.tar` de no seguidos, estado previo y posterior al rebase) y `qa-results/u2-inherited/` (cambios heredados de U1 con hashes). Stash `codex: cabeceras CLAUDE/ENTORNO`, sustituido por `2b67ea5`, conservado sin aplicar. Copia íntegra del repositorio anterior a la integración en `..\Mamporro-respaldo-u2`.
- **Siguiente paso exacto:** esperar la prueba y la aprobación del autor. Si pide cambios, hacerlos dentro de U2. U3 necesita autorización expresa.

Comprobar el estado desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
git log --oneline --graph -8
git stash list
scripts\u2.cmd edit
scripts\u2.cmd play
```

## Registro de sesiones y relevos de U2

Añadir las entradas nuevas **de más antigua a más reciente**. No borrar entradas de otro agente salvo corrección factual explícita.

### 29/09/2026 10:46–12:11 — núcleo, escena U2 y primeras pruebas — Codex (entrada reconstruida por Claude Code a partir de los registros locales)

- Punto de partida/commit: `eb691b5`.
- Trabajo realizado: núcleo y reglas de combate (commit local original `1e66e49`), adaptador, escena U2 e interfaz IMGUI sin commit. Detalle en «Estado real del trabajo local».
- Pruebas realmente ejecutadas y resultado: ver «Pruebas registradas (heredadas de la sesión anterior)».
- Push realizado: no.
- Estado del árbol al terminar: adaptador U2 sin commit y **sin compilar** (`CombatVisualCheck.cs`, CS1503).
- Siguiente paso que dejó anotado: Play Mode tras la espera de 0,4 s, build y ensayos gráficos.

### 29/09/2026 — preparación documental del relevo — ChatGPT

- No se implementó código de U2.
- Se estableció que Codex CLI y Claude Code trabajarán alternándose sobre la misma rama y no en paralelo.
- Se creó `docs/CONTINUIDAD_AGENTES.md` como protocolo de handoff.
- Se actualizaron las entradas de Codex/Claude y la documentación compartida para obligar a registrar trabajo, pruebas, cambios locales y siguiente paso antes del relevo.
- No se ejecutaron pruebas Unity/web porque este cambio es exclusivamente documental.
- Siguiente paso de implementación: auditar el árbol local e iniciar la base determinista/datos/pruebas de U2 siguiendo el plan autorizado de este checkpoint.

### 29/09/2026 19:50–20:35 — auditoría, integración y base verificada de U2 — Claude Code

- Punto de partida/commit: local `1e66e49` (sin publicar) sobre `eb691b5`; remoto `2b67ea5` y, durante la sesión, `dd93c81`.
- Trabajo realizado: auditoría del trabajo de Codex; copia de seguridad en `qa-results/u2-audit/`; stash de las cabeceras de Codex en `CLAUDE.md` y `docs/ENTORNO_LOCAL_CODEX.md` (sustituidas por la documentación remota); rebase del núcleo sobre `2b67ea5` y después sobre `dd93c81` (el autor publicó seis commits documentales durante la sesión); restauración de los once ajustes Unity que solo cambiaban espacios, `GraphicsSettings.asset` y `U1_Patio.unity`; corrección de la compilación; commit de la integración; decisiones del autor en `docs/DECISIONES.md` y checkpoints «Cómo retomar» en `docs/INSTRUCCIONES_PROYECTO.md`.
- Archivos/sistemas principales: `Assets/Mamporro/Core`, `Assets/Mamporro/U2`, `PrototypeSession.cs`, `PrototypeController.cs`, `PlayerMotor.cs`, `U2Project.cs`, `CombatSceneTests.cs`, `scripts/u2.*`, documentación.
- Decisiones nuevas: las del autor listadas en «Decisiones del autor para U2» y en `docs/DECISIONES.md` (29/09/2026).
- Pruebas realmente ejecutadas y resultado: `scripts\u2.cmd edit` 37/37 y `scripts\u2.cmd play` 2/2, dos veces (20:14–20:15 y 20:28–20:29).
- Pruebas pendientes/no ejecutadas: build U2, benchmark, web (sin cambios web).
- Commits creados: «Porta el núcleo y las reglas de combate de U2» (rebasado), «Integra la escena U2 de combate y corrige su compilación», «Registra las decisiones de U2 y los checkpoints de bloque» y el de este registro.
- Push realizado: se intenta tras este commit, con `git fetch` previo; el resultado se anota en la siguiente actualización.
- Estado del árbol al terminar: solo los cambios locales no incluidos (abajo).
- Cambios locales no incluidos: `ProjectSettings.asset`, `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset`, `URPProjectSettings.asset` (no publicar); retoques de Codex en `README.md`, `docs/MIGRACION_UNITY.md`, `docs/PROGRESO_U1.md`, `unity/Docs/HOJA_DE_RUTA.md`, `unity/README.md` (revisar en el cierre).
- Errores/limitaciones conocidas: avisos CS0618 por APIs obsoletas en `U2Project.cs` y `CombatSceneTests.cs`; la build `unity/Builds/U2` es antigua.
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 3 del plan de cierre.

### 29/09/2026 20:30–21:10 — referencia U2, subida de nivel uGUI, oro, ensayo y cierre — Claude Code

- Punto de partida/commit: `e50c4b1` (publicado; el push de la entrada anterior se hizo sin incidencias).
- Trabajo realizado: pasos 3–8 del plan de cierre (ver las secciones «Referencia U2 y correcciones», «Subida de nivel uGUI», «Ensayo U2» y «Cierre de U2»).
- Archivos/sistemas principales: `scripts/unity-reference-u2.mjs`, `unity/Docs/Reference/u2-combat.json`, `Tests/Core/U2ReferenceTests.cs`, `Tests/Core/CatalogReferenceTests.cs`, `Core/CombatRun.cs`, `Core/Boss.cs`, `Core/Offers.cs`, `scripts/u2-export-data.mjs`, `U2/LevelUpScreen.cs`, `U2/CardText.cs`, `U2/CombatSession.cs`, `U2/CombatMenu.cs`, `U2/CombatBenchmark.cs`, `Tests/PlayMode/LevelUpScreenTests.cs`, `Tests/PlayMode/HudTests.cs`, documentación.
- Decisiones nuevas: técnicas, en `docs/DECISIONES.md` («U2: decisiones técnicas de implementación»), pendientes de confirmar.
- Pruebas realmente ejecutadas y resultado: ver «Pruebas ejecutadas en esta integración».
- Pruebas pendientes/no ejecutadas: prueba manual con teclado y ratón reales; ensayo sin invulnerabilidad; medidas de GPU y GC (no disponibles en la build normal, como en U1).
- Commits creados: `eb9180d`, `82d2a5d`, `fc1868f`, `6774d0c`, `7948883`, `c0a47ba` y el commit de cierre documental.
- Push realizado: sí, después de `git fetch` y de comprobar que el remoto no había cambiado.
- Estado del árbol al terminar: solo los cambios locales no incluidos.
- Cambios locales no incluidos: `unity/ProjectSettings/ProjectSettings.asset`, `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset`, `URPProjectSettings.asset` (no publicar).
- Errores/limitaciones conocidas: ver «Cierre de U2».
- Decisiones pendientes del autor: aprobar U2 y confirmar las decisiones técnicas.
- Siguiente paso exacto: prueba manual del autor.

### Plantilla para cada sesión posterior

```text
### AAAA-MM-DD — <objetivo breve> — <Codex|Claude Code>

- Punto de partida/commit:
- Trabajo realizado:
- Archivos/sistemas principales:
- Decisiones nuevas:
- Pruebas realmente ejecutadas y resultado:
- Pruebas pendientes/no ejecutadas:
- Commits creados:
- Push realizado: sí/no
- Estado del árbol al terminar:
- Cambios locales no incluidos:
- Errores/limitaciones conocidas:
- Decisiones pendientes del autor:
- Siguiente paso exacto:
```

No escribir resultados previstos en el apartado de pruebas. Si no se ejecutó una comprobación, indicar expresamente que no se ejecutó.

## Decisiones del autor para U2 (29/09/2026)

Confirmadas por el autor tras la auditoría del trabajo local; detalle en `docs/DECISIONES.md`.

1. Referencia complementaria: `scripts/unity-reference-u2.mjs` exporta `unity/Docs/Reference/u2-combat.json` desde `0505b16` con guarda contra `src/`. Cubre rarezas, mejoras, agregación de estadísticas, ofertas con semillas fijas y escenarios deterministas por arma, enemigo y pasiva. Sustituye a `WebFixtures.json` como única fuente de esperados. No tocar `baseline.json` ni `src/`; nunca `--write` sobre la base.
2. Unity (Edit/Play/build/benchmark) se ejecuta en la sesión local.
3. Oro de partida incluido: caída de oro y contador con los valores actuales. Carta de relleno: fórmula web `max(fillerMin, round(chestCost(baúlesAbiertos) × 0,5))`, que da 10 sin baúles. Sin baúles.
4. Subida de nivel técnica en uGUI: 3–4 cartas, clic, 1–4 y teclado numérico, R volver a tirar, X saltar, B modo descarte, Esc sale del descarte, 0,4 s de espera, textos ES/EN de la referencia; sin acabado final. El reinicio de partida pasa a F8.
5. Jefe y Rata élite con todas sus reglas de combate, invocados desde el panel QA. Armario, calendario de élites y enjambre quedan para U3.
6. Panel QA activo en la build U2; puede seguir en IMGUI, oculto durante el benchmark.

## Plan de cierre acordado

1. Integrar `2b67ea5` (rebase del núcleo), restaurar ajustes de Unity que solo cambian espacios, `GraphicsSettings.asset` y `U1_Patio.unity`. **Hecho.**
2. Corregir la compilación; Edit Mode y Play Mode completos; commit y primer push. **Hecho.**
3. `scripts/unity-reference-u2.mjs` → `unity/Docs/Reference/u2-combat.json`; pruebas C# contra él. **Hecho.**
4. Prueba del catálogo contra `baseline.catalog`. **Hecho** (`Tests/Core/CatalogReferenceTests.cs`: personajes, armas, tomos, objetos, enemigos y constantes de `Tuning`).
5. Subida de nivel en uGUI con las teclas acordadas; reinicio en F8. **Hecho** (`U2/LevelUpScreen.cs`, `U2/CardText.cs`; idioma en F7; controles en `unity/README.md`).
6. Contador de oro en el HUD; panel QA oculto durante el benchmark. **Hecho** (`hud.gold` de la web con el oro redondeado hacia abajo, como `Hud.ts`; `CombatMenu` y `LevelUpScreen` no dibujan ni leen teclas mientras `Measuring`).
7. Benchmark en build con combate real: 300/500/750/1000 × 1080p/1440p. **Hecho** (ver «Ensayo U2»).
8. Documentación de cierre (revisar entonces los retoques de Codex en README, MIGRACION_UNITY, PROGRESO_U1, HOJA_DE_RUTA y unity/README) y entrega para prueba manual. **Hecho**: los retoques se incorporan reducidos a una línea que remite a `ESTADO_ACTUAL.md` y con el estado de cierre.

## Estado real del trabajo local (auditoría del 29/09/2026)

Una sesión anterior de Codex terminó sin cerrar. Lo que queda:

- Commit local «Porta el núcleo y las reglas de combate de U2» (original `1e66e49`): núcleo sin UnityEngine en `Assets/Mamporro/Core` (RNG, rejilla, catálogo exportado de `src/data`, seis armas, enemigos, jefe, proyectiles, recogidas, ofertas y `CombatRun`), pruebas `CombatTests` y `ReferenceTests`, `WebFixtures.json` y los scripts `u2-export-data.mjs` y `u2-web-fixtures.mjs`.
- Sin commit: el adaptador y la escena descritos en «Cómo retomar», con la carta de subida de nivel y el panel QA en IMGUI y teclas Q/E/B distintas de la web (R reiniciaba).
- Build `unity/Builds/U2` generada a las 12:03 con el código anterior a los últimos cambios; no corresponde al árbol actual.
- Cambios heredados de U1: once ajustes que solo difieren en espacios, `U1_Patio.unity` regenerada (proporción 1,333 → 2,043), `GraphicsSettings.asset` con iluminación lineal y los tres ajustes locales citados arriba.

### Pruebas registradas (heredadas de la sesión anterior)

| Fecha y hora | Prueba | Resultado | Registro |
| --- | --- | --- | --- |
| 29/09/2026 10:53 | Edit Mode núcleo | 17/17 | `unity/TestResults/u2-core-edit.xml` |
| 29/09/2026 11:05 | Edit Mode combate | 30/31 (fallo JsonUtility con U+0000, corregido en la entrada de prueba) | `unity/TestResults/u2-combat-edit.xml` |
| 29/09/2026 11:59 | Edit Mode completo (26 U2 + 11 U1) | 37/37 | `unity/TestResults/U2/edit.xml` |
| 29/09/2026 12:02 | Play Mode (U1 y U2) | 2/2 | `unity/TestResults/U2/play.xml` |
| 29/09/2026 12:03 | Build Windows x64 Mono | generada | `unity/Builds/U2` |
| 29/09/2026 12:08 | Ensayo 1080p, 300 enemigos, 4 armas, invulnerable, sin cartas | 1974 FPS medios, 0,17 ms/tick; GPU y GC no disponibles | `unity/TestResults/U2/u2-1920x1080-300-*.json` |
| 29/09/2026 12:11 | Compilación tras cambios en cuatro archivos U2 | **FALLIDA** (CS1503) | `unity/TestResults/U2/build.log` |

Los comandos exactos de la sesión anterior no quedaron registrados.

### Pruebas ejecutadas en esta integración

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| 29/09/2026 20:14 | `scripts\u2.cmd edit` | 37/37 (26 U2 + 11 U1), sin errores de compilación; avisos CS0618 por APIs obsoletas en `U2Project.cs` y `CombatSceneTests.cs` | `unity/TestResults/U2/edit.xml`, `edit.log` |
| 29/09/2026 20:15 | `scripts\u2.cmd play` | 2/2 (U1 y U2) | `unity/TestResults/U2/play.xml`, `play.log` |
| 29/09/2026 20:28 | `scripts\u2.cmd edit` (tras rebase sobre `dd93c81`) | 37/37 | `unity/TestResults/U2/edit.xml`, `edit.log` |
| 29/09/2026 20:29 | `scripts\u2.cmd play` (tras rebase sobre `dd93c81`) | 2/2 | `unity/TestResults/U2/play.xml`, `play.log` |

| 29/09/2026 20:41 | `scripts\u2.cmd edit` (primera comparación con `u2-combat.json`) | 81/83: fallan `EnemyBehaviourMatchesWeb` del jefe normal (vida de pelusa hija 14 frente a 14,378) y enfurecido (2 enemigos frente a 6) | `unity/TestResults/U2/edit.xml` (sobrescrito después) |
| 29/09/2026 20:45 | `scripts\u2.cmd edit` (tras corregir el jefe) | 83/83 | `unity/TestResults/U2/edit.xml`, `edit.log` |
| 29/09/2026 20:45 | `scripts\u2.cmd play` | 2/2 | `unity/TestResults/U2/play.xml`, `play.log` |
| 29/09/2026 20:46 | `node scripts\unity-reference-u2.mjs`, `node scripts\unity-reference.mjs`, `node --test scripts\unity-reference-bytes.test.mjs` | referencia U2 coincide con la web; U0 verificada; 4/4 | consola |
| 29/09/2026 20:47 | `scripts\u2.cmd edit` (con `CatalogReferenceTests`) | 88/88 | `unity/TestResults/U2/edit.xml`, `edit.log` |
| 29/09/2026 20:53 | `scripts\u2.cmd edit` y `scripts\u2.cmd play` (pantalla uGUI) | 88/88 y 9/9 (2 anteriores + 7 de `LevelUpScreenTests`) | `unity/TestResults/U2/edit.xml`, `play.xml` |
| 29/09/2026 20:55 | `scripts\u2.cmd edit` y `scripts\u2.cmd play` (oro en el HUD) | 88/88 y 10/10 (+ `HudTests`) | `unity/TestResults/U2/edit.xml`, `play.xml` |
| 29/09/2026 20:57 | `scripts\u2.cmd edit`, `play`, `build` (ensayo con cartas) | 88/88, 10/10, build correcta | `unity/TestResults/U2/edit.xml`, `play.xml`, `build.log` |
| 29/09/2026 20:58 | `unity\Builds\U2\Mamporro-U2.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -u2-visual-check` | capturas correctas salvo `cards-en` (cartas en español): corregido | `unity/Builds/U2/TestResults/U2/Visual/` |
| 29/09/2026 20:59 | `scripts\u2.cmd edit`, `play`, `build` y comprobación visual repetida | 88/88, 10/10 (con prueba del repintado de idioma), build correcta; cartas ES/EN, QA, pausa y derrota correctas | mismos registros; `unity/TestResults/U2/visual.log` |
| 29/09/2026 21:00–21:05 | `scripts\u2.cmd benchmark` | 8/8 condiciones con `validRender` | `unity/TestResults/U2/u2-*-2026092919*.json`, `.csv`, `.png` |
| 29/09/2026 21:06 | `npm.cmd run typecheck`, `npm.cmd test`, `npm.cmd run build` | correcto; 202/202 en 27 archivos; build correcta; sin cambios en `src/` | consola |

Cambios de la integración: `CombatVisualCheck.cs` usa `WaitForSecondsRealtime(.5f)` y guarda sus capturas en `unity/TestResults/U2/Visual` (ignorada por Git). Pendientes: build, ensayo gráfico y web (sin cambios web en esta integración).

### Referencia U2 y correcciones (paso 3)

- `scripts/unity-reference-u2.mjs` sustituye a `u2-web-fixtures.mjs`, y `unity/Docs/Reference/u2-combat.json` sustituye a `WebFixtures.json`. Guarda: `src/` idéntico a `0505b16` y sin cambios locales. `--export-once` solo crea el archivo si no existe; sin argumentos, comprueba. El archivo se regeneró dos veces **antes** de que lo usara ninguna prueba C#: una para añadir el jefe enfurecido y otra para evitar listas anidadas, que JsonUtility no lee. Desde la primera comparación con C# no se ha regenerado.
- Contenido: RNG, pesos y 40 tiradas de rareza con siete suertes, escalado de mejoras y tomos, tiradas de mejora por arma y rareza (a cero y cerca del tope), 29 casos de agregación de estadísticas, cinco secuencias de subida de nivel (elegir, volver a tirar, saltar, descartar, relleno de curación y oro), veinte ofertas seguidas, seis armas, seis enemigos más el jefe enfurecido, las dos pasivas y dos combates integrados de 900 ticks (objetos, subidas de nivel, oro, bata).
- Pruebas: `Tests/Core/U2ReferenceTests.cs`. Enteros, textos y Float32, exactos; double, 1e-9. `Offers` expone `RarityWeight`, `RollUpgrade` y `TomeAmounts` sin cambiar su comportamiento.
- Divergencias encontradas y corregidas en el port (no en la referencia):
  1. Las pelusas hijas del jefe y el propio jefe no aplicaban los multiplicadores del minuto de dificultad. Ahora `CombatRun` calcula `SpawnHp/SpawnXp/SpawnGold` (ritmo 1, duración de referencia) y la vida del jefe `1 + 0,2·m + 0,06·m²`; `SpawnBoss` ajusta la posición al mundo, como `clampInside`.
  2. El jefe descartaba pelusas con un límite fijo `|x|,|z| < 46`. Ahora pregunta al mundo (`ICombatWorld.IsInside(x, z, 2)`, como la web). `CombatWorld` usa el límite del patio U1 (±47,5).
- Apariciones QA (`SpawnScaled`): usan los multiplicadores del minuto actual, como `debugSpawnEnemy` en la web.
- Carta de relleno de oro: `Rules.FillerGold(0)` con la fórmula web; las constantes llegan a `Tuning` desde `scripts/u2-export-data.mjs` (nuevo bloque del exportador; el resto de `Catalog.cs` no cambia).

### Subida de nivel uGUI (paso 5)

- `U2/LevelUpScreen.cs` construye por código un lienzo uGUI (y un `EventSystem` con `InputSystemUIInputModule` si no existe) con 3–4 cartas y las acciones R/X/B. Reproduce `src/ui/LevelUpScreen.ts`: el título es el nivel que se está eligiendo; la espera de `Tuning.InputGuard` (0,4 s, exportado de la web) se reinicia al abrir y tras elegir o saltar, pero no tras volver a tirar o descartar; en modo descarte, las cartas de relleno no se pueden descartar.
- `U2/CardText.cs` equivale a `describeCard()` de `src/ui/cards.ts`: etiquetas propias por arma (`statLabels`, ahora exportadas), porcentaje o número según la mejora (`display`, exportado) y números con el separador del idioma. Los textos salen de `WebText.json` (traducciones de la web).
- `CombatMenu` (IMGUI) ya no dibuja cartas; solo la pausa y el panel QA. F4 no abre el panel durante la subida de nivel. Se retiran de `U2Text.json` las claves de las cartas IMGUI.
- Teclas: F7 idioma, F8 reinicio. Comprobado contra `PrototypeController`: U1 usa WASD, Espacio, Mayús, C, F1–F3, F6 y, solo sin sesión U2, R, 1–4 y F5; Esc de la pausa no interfiere porque la partida no se reanuda mientras hay elección abierta.
- Pendiente de ver en build y con teclado y ratón reales (las pruebas llaman a `Press`/`Action`, los mismos métodos que usan las teclas y los botones).

### Ensayo U2 (paso 7)

Build Windows x64 Mono normal, D3D11, interna 640×360, VSync 0, FPS sin límite; 10 s de calentamiento y 30 s de medida por condición; una repetición. Equipo: Ryzen 7 7700X, RTX 4070 Ti SUPER, 31 913 MB de RAM. Combate real: Remedios con chancla, naftalina, dentaduras y fregona; 12 subidas de nivel previas y las de la medida resueltas con la primera carta; reposición de pelusa, cucaracha, táper y paloma hasta la carga; jugador invulnerable (QA). GPU y GC «N/D»: la build normal no los da o los da inválidos, igual que en U1; no se suponen ceros.

| Salida | Enemigos | FPS medios | Frame ms | P95 | P99 | Máx ms | >16,67 ms | Tick medio ms | Tick P99 | Tick máx | Memoria MiB | Nivel final | Cartas | Proyectiles máx | Disparos enemigos máx |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1920×1080 | 300 | 1976 | 0,506 | 0,623 | 0,718 | 8,83 | 0 % | 0,134 | 0,210 | 0,402 | 208,0 | 25 | 24 | 6 | 82 |
| 1920×1080 | 500 | 1614 | 0,619 | 0,788 | 0,898 | 1,32 | 0 % | 0,233 | 0,400 | 0,520 | 208,1 | 32 | 31 | 9 | 108 |
| 1920×1080 | 750 | 1437 | 0,696 | 0,924 | 1,086 | 5,96 | 0 % | 0,371 | 0,683 | 0,919 | 208,1 | 36 | 35 | 9 | 117 |
| 1920×1080 | 1000 | 1169 | 0,855 | 1,282 | 1,499 | 6,64 | 0 % | 0,538 | 1,025 | 1,223 | 208,1 | 42 | 41 | 9 | 193 |
| 2560×1440 | 300 | 1967 | 0,508 | 0,622 | 0,711 | 3,19 | 0 % | 0,128 | 0,199 | 0,385 | 208,7 | 25 | 24 | 6 | 82 |
| 2560×1440 | 500 | 1590 | 0,629 | 0,794 | 0,894 | 9,54 | 0 % | 0,221 | 0,383 | 0,523 | 212,7 | 32 | 31 | 9 | 108 |
| 2560×1440 | 750 | 1419 | 0,705 | 0,929 | 1,092 | 3,18 | 0 % | 0,369 | 0,684 | 0,975 | 212,7 | 36 | 35 | 9 | 117 |
| 2560×1440 | 1000 | 1167 | 0,857 | 1,254 | 1,452 | 4,65 | 0 % | 0,517 | 1,007 | 1,309 | 212,8 | 42 | 41 | 9 | 193 |

- El objetivo de 60 FPS con 300 enemigos en combate se cumple con mucho margen en este equipo; 500/750/1000 son cargas de margen y estrés, no requisitos de diseño. Ninguna condición tuvo fotogramas por encima de 16,67 ms. No se debe extrapolar a equipos modestos.
- La lógica es determinista: niveles, cartas, bajas y construcción final coinciden exactamente entre las dos resoluciones.
- El coste por tick crece con la carga (0,13 → 0,54 ms de media) y queda muy por debajo del presupuesto de 16,67 ms.

## Cierre de U2 (29/09/2026, pendiente de aprobación)

**Commits publicados** sobre `dd93c81`: `b34823e` núcleo (commit de Codex rebasado), `a78fb8b` integración de la escena, `bc699b1` decisiones y checkpoints, `e50c4b1` registro de sesión, `eb9180d` referencia `u2-combat` y correcciones del jefe, `82d2a5d` prueba del catálogo, `fc1868f` checkpoint, `6774d0c` subida de nivel uGUI, `7948883` oro en el HUD, `c0a47ba` repintado de idioma y ensayo con cartas, más el commit de cierre documental.

**Sistemas portados:** RNG y derivaciones; estadísticas, daño, críticos, armadura, XP y topes; rarezas, mejoras, ofertas y acciones (elegir, volver a tirar, saltar y descartar, con relleno de curación y oro); rejilla, colisiones de combate y proyectiles propios y enemigos; 2 personajes con sus pasivas, 6 armas, 8 tomos, 12 objetos y 6 enemigos (Rata élite y Pelusa Madre con todos sus ataques, enfurecimiento y pelusas hijas); caída y contador de oro; derrota, resurrección con bata y reinicio; escena QA y panel QA; pantalla de subida de nivel uGUI.

**Equivalencia:** `u2-combat.json` (web aprobada) y `baseline.json` (U0) coinciden con el port en todas las pruebas; las dos divergencias encontradas se corrigieron en el port, no en la referencia.

**Límites conocidos:**
- La prueba con teclado y ratón reales no la he hecho: las pruebas llaman a los mismos métodos que las teclas y los botones, y la comprobación visual se hizo en build.
- El ensayo usa invulnerabilidad QA y una sola repetición; GPU y GC no están disponibles en la build normal.
- La escena usa el patio de U1: sin director, mundo procedural, baúles ni victoria (U3).
- La pausa y el panel QA siguen en IMGUI (aprobado).
- Unity reescribe `ProjectAuditorSettings.asset` con espacios al abrir el proyecto; no se publica.
- El stash `codex: cabeceras CLAUDE/ENTORNO` se conserva sin aplicar; se puede borrar cuando el autor lo confirme.

**Instrucciones de prueba manual (CMD):**

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
unity\Builds\U2\Mamporro-U2.exe
```

La build de las 20:59 ya está generada en este equipo. Para regenerarla, con el Editor cerrado: `scripts\u2.cmd build`. También se puede abrir `Assets/Mamporro/U2/U2_Combate.unity` en el Editor y pulsar Play. Qué comprobar:

1. «Entrar al combate» (o Esc): moverse, saltar y deslizarse; las armas atacan solas y el HUD muestra vida, nivel, XP, oro y bajas.
2. Al subir de nivel: durante 0,4 s no responde nada; después 1–4, el teclado numérico o un clic eligen; R vuelve a tirar; X salta; B y después 1–4 o clic descartan; Esc o B cancelan el descarte.
3. F7 cambia ES/EN, también con las cartas abiertas. F8 reinicia.
4. Esc y después QA, o F4: añadir armas, tomos y objetos (bata, perlas, olla, monedero…), subir nivel, oro, daño, invulnerabilidad; invocar la Rata élite y la Pelusa Madre (bájale la vida para verla enfurecida).
5. Morir: derrota; con la bata, resurrección con media vida. F8 reinicia sin restos.

**Siguiente paso propuesto:** U3 (mundo, estructuras, interactuables, director y partida completa), **sin empezarlo** hasta que el autor apruebe U2 y autorice U3.

## Punto de partida

- Rama: `claude/zen-pasteur-674ik0`.
- Base web de referencia: `0505b1690656d15188860157612455639820fe1f`.
- Implementación U1 publicada: `abe0a9b7f0b8c53f478b91c870341999df2ea073`.
- Checkpoint U1 publicado: `eb691b595eb247118075368430d34ed0a95735d1`.
- Unity 6000.6.3f1, Windows x64 Mono, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0.
- U1 dispone de movimiento, cámara, render retro, escena técnica y horda centralizada/instanciada.
- `unity/Docs/Reference/` y `scripts/unity-reference.mjs` conservan los valores esperados de U0.

Antes de editar, comprobar `git status --short --branch`. El cierre de U1 dejó constancia de posibles cambios locales posteriores generados por Unity (14 modificados y dos ajustes nuevos). Preservarlos; no resetear ni limpiar el árbol para iniciar U2.

Al recibir un relevo, revisar también `git log -8 --oneline`, `git fetch origin` y la entrada más reciente de este registro antes de modificar archivos. Si el árbol está limpio y solo falta avanzar, usar `git pull --ff-only`.

## Objetivo de U2

Reproducir en Unity las reglas y capacidades de **núcleo + combate** de la versión web aprobada, sin ampliar todavía el mundo ni la meta. La escena de U1 puede evolucionar o complementarse con una escena QA controlada, pero la equivalencia de reglas debe poder probarse sin depender de presentación visual.

## Alcance autorizado

1. **RNG y determinismo**
   - portar el RNG/hash y las derivaciones por subsistema;
   - conservar operaciones/IDs y vectores esperados de U0;
   - no reemplazar la lógica equivalente por `UnityEngine.Random`.

2. **Estadísticas y fórmulas**
   - estadísticas base y bonificaciones;
   - daño, crítico, supercrítico y armadura;
   - experiencia/niveles y topes vigentes;
   - rareza/suerte y reglas de ofertas necesarias para probar progresión de combate.

3. **Bucle mínimo de partida**
   - jugador con vida;
   - un enemigo y un arma conectados primero como vertical slice;
   - daño al enemigo y al jugador;
   - muerte, XP, subida de nivel, derrota y reinicio limpio;
   - ningún estado/objeto/proyectil residual al reiniciar.

4. **Catálogo de combate equivalente**
   - 2 personajes: Doña Remedios y Sir Baguette, con arma inicial y pasiva;
   - 6 armas: Chancla, Naftalina, Barra, Dentaduras, Jersey y Suelo/Fregona;
   - 8 tomos;
   - 12 objetos y sus efectos/sinergias de combate;
   - 6 definiciones de enemigos de la referencia: cuatro normales, Rata élite y Pelusa Madre jefe;
   - proyectiles propios/enemigos y reglas necesarias de sus comportamientos.

5. **Arquitectura y rendimiento**
   - reutilizar la base de U1: sistemas centralizados, arrays/pools/rejilla e instanciación donde corresponda;
   - no introducir un `Update`/`Rigidbody` por enemigo por defecto;
   - evitar asignaciones por frame en bucles calientes;
   - DOTS/ECS no es requisito; solo reconsiderarlo si un perfilado real demuestra necesidad.

6. **QA y equivalencia**
   - portar pruebas de reglas útiles de Vitest a pruebas C#;
   - comparar RNG/enteros/IDs de forma exacta y doubles con las tolerancias de U0;
   - usar los valores guardados en U0 como esperados: no recalcular el «resultado correcto» con el mismo código que se prueba;
   - disponer de una escena QA controlada para activar armas, enemigos, daño, nivel, pasivas y reinicio sin depender del futuro mapa U3.

## Fuera de alcance

No implementar como parte de U2:

- mapa procedural completo, edificios y colocación del mundo;
- baúles, mesas camilla, tótems, portal, director de oleadas completo, enjambre y bucle de partida U3;
- menús/meta, Calderilla del Caos, tienda, ocho misiones o traslado de guardados U4;
- audio final, pulido global y equivalencia completa de UI U5;
- Steamworks, logros, Steam Cloud, publicación o compras;
- mejoras posmigración: escalada libre, mundo mayor, nueva curva de hordas/oro, arte final e incremento de catálogo.

No rebalancear estadísticas, economía ni contenido para «aprovechar» el port. Registrar diferencias intencionadas y pedir decisión si una equivalencia no es razonable.

## Plan de implementación recomendado dentro del bloque ya autorizado

1. Auditar el estado local y separar cualquier cambio Unity posterior a U1.
2. Portar utilidades deterministas y datos base con pruebas contra `baseline.json`.
3. Portar estadísticas/daño/XP/ofertas y pruebas unitarias.
4. Conectar vertical slice arma + enemigo + jugador + derrota/reinicio.
5. Incorporar progresivamente armas, personajes/pasivas, tomos, objetos y enemigos, con pruebas por comportamiento.
6. Montar o ampliar escena QA sin introducir sistemas de U3.
7. Ejecutar Edit Mode y Play Mode; corregir fugas/estado residual.
8. Generar build Windows x64 Mono y probarla realmente.
9. Medir 300 enemigos con combate activo; 500/750 como margen y 1000 como estrés cuando la arquitectura lo permita. Registrar CPU/frame/memoria y GPU solo si la métrica es fiable. Comparar con U1 sin prometer igualdad de coste.
10. Verificar que la web/referencia no se ha alterado; ejecutar sus tests si se toca código web o herramientas compartidas.
11. Actualizar documentación, commits pequeños, push; detenerse para prueba manual del autor.

El bloque ya está autorizado: no volver a pedir un «sí» general para estos pasos. Sí hay que preguntar antes de una decisión de diseño importante no cubierta por la referencia o por este alcance.

## Criterios de salida

U2 puede proponerse como cerrado cuando:

- los vectores RNG/IDs y las fórmulas cubiertas coinciden con U0 dentro de sus reglas de tolerancia;
- el bucle mínimo llega a derrota y reinicia sin arrastrar entidades/efectos/estado;
- los 2 personajes, 6 armas, 8 tomos, 12 objetos y 6 enemigos tienen su comportamiento de combate portado dentro del alcance U2 y pruebas razonables;
- Edit Mode y Play Mode del bloque pasan;
- una build Windows x64 Mono se genera y ejecuta;
- el combate con 300 enemigos mantiene el objetivo de 60 FPS en el equipo de referencia o, si no lo hace, se perfila y corrige/explica antes de pasar a U3;
- 500/750/1000 se documentan como margen/estrés cuando se ensayen, no como requisito de diseño;
- no se ha regenerado la referencia U0 para ocultar divergencias;
- la base web sigue recuperable y no se han introducido cambios de U3+;
- el autor recibe instrucciones de prueba y aprueba U2 antes de iniciar U3.

## Verificaciones heredadas que no deben reinterpretarse

U1 pasó 11 Edit Mode y 1 Play Mode y ejecutó ocho condiciones de benchmark técnico. Eso valida la base técnica de U1, **no** el coste del combate U2. Los 202 tests web siguen siendo evidencia de la base web mientras no se cambie; si se modifica, repetir las comprobaciones correspondientes.

## Checkpoint al terminar U2

Registrar aquí:

- commits publicados;
- archivos/sistemas portados;
- decisiones nuevas;
- pruebas exactas ejecutadas y sus resultados;
- build y condiciones de rendimiento;
- limitaciones/errores conocidos;
- cambios locales no publicados;
- instrucciones de prueba manual;
- aprobación del autor o pendientes;
- siguiente paso propuesto (U3), sin empezarlo automáticamente.

Al cerrar U2, trasladar las decisiones permanentes a `docs/DECISIONES.md`, actualizar `docs/ESTADO_ACTUAL.md` y las referencias de entrada para que Codex y Claude Code apunten al checkpoint del siguiente bloque autorizado.
