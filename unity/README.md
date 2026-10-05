# MAMPORRO en Unity

> Estado vigente en [ESTADO_ACTUAL](../docs/ESTADO_ACTUAL.md): U1–U3 aprobados; aprobación manual U3 el 04/10/2026 ([cierre](../docs/PROGRESO_U3.md#checkpoint-al-terminar-u3)). [U4 aprobada manualmente el 04/10/2026](../docs/PROGRESO_U4.md#checkpoint-al-terminar-u4). [U5 aprobado manualmente el 04/10/2026](../docs/PROGRESO_U5.md#checkpoint-al-terminar-u5).

## Referencia U1 (aceptada por el autor)

**U1 aprobado el 29/09/2026.** Proyecto técnico de movimiento, presentación retro
y horda. `Assets/`, `Packages/` y `ProjectSettings/` ya existen; la referencia U0
se conserva íntegra. Sin combate completo, progresión ni guardado.

- Destino aprobado: Windows de escritorio, con objetivo de publicación en Steam
  y plataformas similares. Web queda fuera del primer destino de la migración.
- Editor del autor: Unity 6.6 (6000.6.3f1), confirmado por captura y metadatos
  locales. Soporte Windows Mono detectado; Windows IL2CPP ausente en esta instalación.
  Licencia e importación comprobadas. Windows x64 Mono, URP 17.6.0,
  Input System 1.20.0, uGUI 2.6.0 y Test Framework 1.8.0 efectivos.
- Referencia web aprobada: `0505b1690656d15188860157612455639820fe1f`.
- Mantener el progreso compatible mediante exportación/importación validada.
- Mismo repositorio y rama `claude/zen-pasteur-674ik0`, sin PR.

## Documentos

- [Estado y criterios de U0](Docs/U0_REFERENCIA.md).
- [Mejoras del juego y recomendaciones](Docs/HOJA_DE_RUTA.md).
- [Datos de referencia](Docs/Reference/baseline.json).
- [Plan de migración U0–U6](../docs/MIGRACION_UNITY.md).
- [Checkpoint del entorno local y cómo retomar](../docs/ENTORNO_LOCAL_CODEX.md).

Desde la raíz del repositorio, en CMD:

```cmd
npm ci
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

Desde U4, la web incorpora la excepción autorizada del exportador. El comando crea una instantánea aislada de la base aprobada y ejecuta las guardas originales U0/U2/U3/U4 sin relajarlas ni regenerar referencias; conserva esa instantánea en `qa-results/` para revisión. Procedimiento en [EXPORTACION_U4](../docs/EXPORTACION_U4.md), estado vigente en [PROGRESO_U4](../docs/PROGRESO_U4.md).

`scripts\u3.cmd edit` prepara además ocho transferencias usando el exportador web real y las valida en C# contra el corpus congelado. Requiere las dependencias Node instaladas. No genera ni modifica esperados.

## Abrir y jugar

En Hub, añade esta carpeta `unity/` como proyecto existente con 6000.6.3f1.
Abre `Assets/Mamporro/Generated/U1_Patio.unity`, pulsa Play y haz clic en Game.
No abras dos instancias sobre el mismo proyecto.

Build local desde la raíz del repositorio, CMD:

```cmd
unity\Builds\U1\Mamporro-U1.exe
```

Conservar toda la carpeta de build, incluidos datos y DLL.

- WASD: moverse respecto a cámara; ratón: orientar cámara.
- Espacio: salto variable; Shift o C: deslizamiento.
- Esc: pausa y libera cursor; clic: continuar. Perder foco pausa la prueba manual.
- R: reiniciar semilla. 1/2/3/4: 300/500/750/1000 enemigos.
- F1: interna 240/360/480; F2: dithering; F3: ajuste de vértices.
- F6: ventana 1080p/1440p. Alt+F4: cerrar.
- F5: ensayo de cuatro cargas en la resolución actual; Esc cancela.

Izquierda al fondo: rampa suave, meseta y bajada. Derecha: pendiente excesiva.
Bloques centrales: separación y rodeo local. La cámara evita el suelo; la
colisión completa contra estructuras sigue pendiente para otro bloque.

## Escena U2: combate controlado

Abre `Assets/Mamporro/U2/U2_Combate.unity` y pulsa Play, o ejecuta la build
`unity\Builds\U2\Mamporro-U2.exe` (se genera con `scripts\u2.cmd build`). Es una
escena de pruebas sobre el patio de U1: reposición sencilla de enemigos básicos,
sin director, mundo, baúles ni guardado (eso es U3–U4). Empieza en pausa: pulsa
«Entrar al combate».

Controles de U2 (sin conflictos con U1: en esta escena R, 1–4 y F5 de U1 no se usan):

- WASD: moverse; ratón: cámara. Espacio: salto; Mayús o C: deslizamiento.
- Esc: pausar y continuar (el menú de pausa ofrece continuar, reiniciar y QA).
  En U2 el clic no reanuda: usa Esc o «Entrar al combate».
- Subida de nivel (pantalla uGUI, simulación en pausa):
  - 1–4, teclado numérico 1–4 o clic en la carta: elegir.
  - R: volver a tirar. X: saltar la subida. B: modo descarte; después 1–4 o
    clic en la carta que no quieres volver a ver. B o Esc cancelan el descarte.
  - Durante 0,4 s tras abrirse (y tras elegir o saltar) se ignoran teclas y
    clics, como en la web. Volver a tirar y descartar no reinician esa espera.
- F1: resolución interna 240/360/480; F2: dithering; F3: ajuste de vértices;
  F6: ventana 1080p/1440p.
- F4: panel QA (armas, tomos, objetos, subir nivel, invulnerabilidad, oro,
  daño, Rata élite, jefe y demás enemigos). No se abre durante la subida de nivel
  y se oculta durante el ensayo automático.
- F7: idioma ES/EN. F8: reiniciar la partida de pruebas.

## Escena U3: mundo procedural y partida completa

Abre `Assets/Mamporro/U3/U3_Partida.unity`, o ejecuta
`unity\Builds\Windows\MAMPORRO.exe` (build de entrega desde U6; antes `Builds\U3\Mamporro-U3.exe`). Es la partida completa de la web aprobada
sobre el mundo procedural, con el progreso permanente, menús y opciones de U4 en
pantallas uGUI técnicas, y audio y efectos de U5 (el arte final llega después de U6).

- **Jugar:** Doña Remedios o Sir Baguette (si está desbloqueado), duración 5/10/15
  minutos y semilla opcional (se normaliza como en la web; vacía = mapa actual).
  «Nuevo mapa» sortea otra semilla. La escena técnica arranca en `MAMPORRO`
  (`-u3-seed X` lo cambia).
- **Importar progreso web (U4):** Opciones → «Importar progreso web» → elegir
  `mamporro-progreso.json` (ruta o «Examinar…») → «Revisar archivo» muestra el
  progreso guardado, el del archivo y los cambios → «Sustituir mi progreso» hace copia
  de seguridad y sustituye (nunca suma). Guardado en `Application.persistentDataPath/Progress`
  (`-u4-save-dir` lo cambia).
- **Menú (U4):** Inicio, Jugar (semilla, duración), Personajes, Tienda (compras y
  mejoras), Misiones y Opciones, en español o inglés.
- **Opciones (U4):** las 14 de la web, en el menú y en la pausa («Opciones»): idioma,
  sensibilidad, resolución interna 240/360/480, temblor de vértices, dithering,
  FPS, reducir partículas, sacudidas, destellos, Ctrl para deslizarse, duración
  (solo en el menú), volúmenes y silencio. Se aplican al momento y se guardan con el
  progreso; desde U5 todas tienen efecto (volúmenes, partículas, sacudidas y destellos).
- **Audio (U5):** música chiptune original (normal e intensa con jefe o enjambre) y
  efectos sintetizados como en la web; volúmenes y silencio de Opciones; sin foco se
  silencia y se pausa. `visual` mide además la señal real de salida (`audio-report.json`).
- **Efectos (U5):** partículas, números de daño, sacudida de cámara, destellos y
  parpadeo del jugador como la web, gobernados por sus opciones; cámara con brazo que se
  acorta contra el terreno y FOV por velocidad. Equivalencia en `docs/EQUIVALENCIA_U5.md`.
- **Progreso permanente (U4):** se carga al arrancar; la partida usa el personaje
  desbloqueado seleccionado, los desbloqueos de armas y objetos y los usos 2 + extras.
  Al ganar o perder se liquida una sola vez (Calderilla del Caos y misiones, como la
  web) y se guarda con copia de seguridad; las partidas con trucos y los abandonos no
  dan nada. `visual` y `benchmark` usan su propia carpeta de guardado.
- **Partida:** director de la web (tabla y curva de aparición, seis oleadas,
  Rata de Gimnasio cada 2 minutos de dificultad, enjambre final hasta 750 vivos),
  14 baúles con precio creciente, 3 mesas camilla (bendiciones), 2 tótems de
  desafío y el armario escondido que invoca a la Pelusa Madre; la victoria llega
  1,6 s después de derrotarla. HUD con vida, experiencia, oro, cuenta atrás,
  bajas, armas, tomos, objetos, minimapa con descubrimiento, barra del jefe,
  avisos y telegrafiado de sus ataques.
- **Controles:** WASD, ratón, Espacio (salto), Mayús/C (deslizarse), E (usar
  baúl, tótem o armario), Ctrl con su opción, Esc (pausa: semilla, estadísticas,
  objetos, opciones y volver al inicio con confirmación). Cartas: 1–4/clic, R volver a tirar, X saltar, B descartar.
- **Resultados:** victoria o derrota, tiempo, bajas, nivel, oro, baúles, daño por
  arma y objetos, Calderilla del Caos ganada, misiones completadas y estado del
  guardado (con reintento si falla). Reintentar, nuevo mapa o volver al inicio.
- **Técnico:** F1 resolución interna 240/360/480, F2 dithering, F9 ajuste de
  vértices (cambian y guardan esas mismas opciones), F6 ventana 1080p/1440p, F8
  vuelve al inicio con el mismo mapa.
- **F3:** panel de depuración (FPS, tiempos, draw calls, posición, director…).
  Con el panel abierto y jugando: 1 invencible, 2 +nivel, 3 +1 minuto, 4 +100
  enemigos, 5 matar todo, 6 invocar jefe, 7 +100 de oro, 8 revelar el mapa. Abrir
  el panel no marca trucos; usar una acción sí («Partida con trucos de debug»).

Con el Editor cerrado, desde la raíz en CMD:

```cmd
scripts\u3.cmd edit
scripts\u3.cmd play
scripts\u3.cmd build
scripts\u3.cmd visual
scripts\u3.cmd benchmark
```

`visual` ejecuta la build con `-u3-visual-check -u3-output` y genera diecinueve capturas
en `unity/TestResults/U3/Visual/`: inicio, tienda, misiones, opciones en español e
inglés, importación revisada (con un guardado
propio dentro de la carpeta de salida), vista alta, casa, templo, granja, pozo,
combate con HUD, interactuables, telegrafiado, pausa, opciones en pausa, cartas, resultados y
reinicio. Comprueba que sean nuevas y contengan imagen; abre una ventana visible
durante unos segundos. También exige diferencias de píxeles al activar/desactivar
el render de combate con cámara/simulación/viento inmóviles: detecta instancing
ausente en la build. Es una comprobación visual, no una medida de rendimiento. Desde U5 mide además
la señal de audio real (`audio-report.json`), termina con cinco partidas seguidas sin
trucos (`sessions-report.json`) y abre la build cuatro veces más para las 11 pantallas
en español e inglés a 1280×720 y 1920×1080 (`Visual/<idioma>-<ancho>x<alto>/`, 44 capturas).

`benchmark` ejecuta la build a pantalla completa en el monitor principal
(`-monitor 1`; 1920×1080 y 2560×1440) con
`-u3-benchmark`: una partida real (Remedios, 10 minutos, `MAMPORRO`, director
activo, invulnerable de ensayo y siempre la primera carta) que avanza sin medir
hasta 10 s antes de cada punto y mide 10 s + 30 s en los minutos 2, 5 y 9 y en
el enjambre (desde los 640 s). Escribe JSON y CSV por punto (`u3-<ancho>x<alto>-<punto>-*.json`)
con FPS, P95/P99, CPU/GPU, GC, ticks y `validRender`, y muestra un resumen. Desde U5:
dos perfiles por resolución (`-u5-profile horda|armas`: solo la Chancla o cuatro armas),
audio, partículas, números y sacudida activos, y contadores de partículas, números,
voces, sonidos y recolecciones, con el detalle de cada fotograma > 16,67 ms.
`scripts\u3.cmd devdiag` genera una build de desarrollo aparte (`Builds/U3Dev`) y hace
una pasada a 1920×1080 en `TestResults/U3/DevDiag` («diagnóstico Development»:
asignaciones, memoria y GPU si es válida; no es rendimiento final).
XML, logs, informes y capturas quedan en `unity/TestResults/U3/`; builds y
capturas no se publican. `scripts\u3.cmd create` regenera deliberadamente solo la
escena U3; no hace falta para jugar.

## Pruebas y build (CMD desde la raíz)

Con el Editor cerrado:

```cmd
scripts\u1.cmd edit
scripts\u1.cmd play
scripts\u1.cmd build
scripts\u1.cmd benchmark
node scripts\u1-report.mjs
```

El lanzador no instala herramientas ni cierra procesos. Edit/Play producen XML
y logs en `unity/TestResults/`. Build es normal, sin Development Build, con
instrumentación local. Benchmark ejecuta las dos resoluciones secuencialmente;
dura unos seis minutos y puede cambiar temporalmente el modo de pantalla.
La ventana de la build debe ser visible: el lanzamiento oculto no mide render.

`scripts\u1.cmd create` regenera deliberadamente `Generated/` y la escena desde
el generador. No hace falta para jugar; revisar antes cualquier cambio manual.

U2, también con el Editor cerrado:

```cmd
scripts\u2.cmd edit
scripts\u2.cmd play
scripts\u2.cmd build
scripts\u2.cmd benchmark
node scripts\unity-reference-u2.mjs
```

`scripts\u2.cmd edit` incluye las pruebas de U1. Los resultados quedan en
`unity/TestResults/U2/`. `node scripts\unity-reference-u2.mjs` comprueba que
`Docs/Reference/u2-combat.json` sigue coincidiendo con la web aprobada; no la
reescribe.

`scripts\u2.cmd benchmark` ejecuta la build U2 visible y en pantalla completa a
1920×1080 y 2560×1440 con 300/500/750/1000 enemigos (unos seis minutos), con las
condiciones de U1: interna 360, VSync 0, FPS sin límite, 10 s de calentamiento
y 30 s de medida. El combate es real: cuatro armas, 12 subidas de nivel previas y
las de la medida resueltas con la primera carta, proyectiles propios y de paloma.
El jugador es invulnerable (QA) para que la partida no termine antes de medir.
Los JSON/CSV y capturas quedan en `unity/TestResults/U2/`; resultados del
29/09/2026 en [PROGRESO_U2](../docs/PROGRESO_U2.md).

## Ensayo reproducible

Semilla 6741, circuito de 8 s (120 ticks por lado a 60 Hz), cámara yaw 0/pitch 20.
Cada carga reinicia jugador y horda: 10 s de calentamiento, 30 s de medición.
Interna 360, dither/snap activados por defecto, VSync 0, FPS ilimitados, D3D11,
sin sombras ni postproceso adicionales. Las condiciones reales quedan en JSON.
Capturas durante el calentamiento; CSV/JSON se escriben después de medir.

CSV: frame real, CPU/GPU si FrameTimingManager los ofrece y GC si ProfilerRecorder
está disponible; `-1` significa no disponible. JSON: media, P95, FPS medio,
memoria Unity final y condiciones. No mide pico de memoria ni VRAM. HUD y registro
forman parte del coste. No confundirlo con un benchmark aislado de lógica.

El resumen añade P99, máximo y porcentaje de frames sobre 16,67 ms. Si detecta
timings GPU superiores a toda la ventana, marca la media GPU de esa condición
como N/D y conserva los datos originales. Ver las limitaciones del último ensayo
en el checkpoint; no interpretar valores GPU anómalos como tiempos válidos.

Resultados en `unity/TestResults/` con el lanzador; al usar F5,
`Application.persistentDataPath/U1Benchmarks`. Solo locales, sin telemetría externa.
300: objetivo 60 FPS; 500/750: margen; 1000: estrés. No son topes ni balance.
El equipo del autor no define requisitos mínimos ni representa equipos modestos;
la horda provisional no garantiza el coste del futuro combate completo.

## Estructura

- `Assets/Mamporro/Runtime`: datos, mundo, motor, horda y adaptadores de escena/UI.
- `Editor/U1Project.cs`: creación reproducible y build.
- `Shaders/Retro.shader`: iluminación simple, niebla, dither y snap con instancias.
- `Tests/`: comportamiento Edit Mode e integración Play Mode.
- `Generated/`: escena, malla, datos y materiales originales con sus `.meta`.

La web y `Docs/Reference/` permanecen intactos. U1 ya ha recibido revisión favorable del
autor; U2 y U3 están aprobados. U4 está aprobada por el autor (04/10/2026). U5 está aprobado manualmente por el autor (04/10/2026); U6 implementado, verificado y aprobado manualmente por el autor (05/10/2026). Migración U0–U6 cerrada; B0 solo en planificación.
