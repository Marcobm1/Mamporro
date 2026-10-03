# MAMPORRO en Unity

> Estado vigente en [ESTADO_ACTUAL](../docs/ESTADO_ACTUAL.md): U1 y U2 aprobados; U3 autorizado y en curso ([PROGRESO_U3](../docs/PROGRESO_U3.md)). U4 no autorizado.

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
node scripts\unity-reference.mjs
```

El comando compara los datos actuales con la referencia aprobada. Ejecutarlo con
Unity cerrado evita el bloqueo del fichero temporal UnityLockfile por el watcher
de Vite. No regenerar referencias. Estado real de pruebas y Git en
[PROGRESO_U2](../docs/PROGRESO_U2.md).

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

## Escena U3: mundo procedural y combate controlado

Abre `Assets/Mamporro/U3/U3_Partida.unity`, o ejecuta
`unity\Builds\U3\Mamporro-U3.exe`. El paso 5 conecta el combate de U2 con el
mundo y la física de la web: WASD, ratón, Espacio, Mayús/C; Esc pausa y clic continúa.
F1 cambia 240/360/480, F2 dithering, F9 ajuste de vértices y F6 tamaño de ventana.
F8 reinicia el mundo y todo el estado de combate. Empieza sin enemigos: aún no
hay director automático, oleadas ni temporizador de partida completa (paso 6).

F3 abre el panel técnico de QA. Abrirlo no marca trucos; ejecutar sus acciones sí:

- 1: invencibilidad; 2: subir un nivel; 4: añadir hasta 100 enemigos controlados
  (los cuatro tipos normales y Rata élite) alrededor del jugador.
- 5: matar los enemigos, incluido el jefe, al avanzar el siguiente tick.
- 6: invocar Pelusa Madre delante de la cámara; 7: añadir 100 de oro.
- 3 y 8 quedan pendientes del director y descubrimiento; no hacen nada todavía.

Las cartas uGUI tienen prioridad sobre QA: 1–4/clic para elegir, R para volver
a tirar, X para saltar, B para descartar y Esc para cancelar el descarte; guardia
de entrada de 0,4 s. Pausan física y combate. Al morir, F8 permite empezar limpio.
Para comprobar la horda, pulsa F3, 4 y clic para continuar; recorre terreno y
obstáculos, observa daño/bajas/XP/oro y prueba el jefe con 6. Los modelos y avisos
siguen siendo técnicos; esta escena aún no representa la partida completa.

Con el Editor cerrado, desde la raíz en CMD:

```cmd
scripts\u3.cmd edit
scripts\u3.cmd play
scripts\u3.cmd build
scripts\u3.cmd visual
```

`visual` ejecuta la build con `-u3-visual-check -u3-output` y genera nueve capturas
en `unity/TestResults/U3/Visual/`: inicio, vista alta, casa, templo, granja, pozo,
combate, cartas y reinicio.
Comprueba que sean nuevas y contengan imagen; abre una ventana visible durante
unos segundos. También exige diferencias de píxeles al activar/desactivar el
render de combate con cámara/simulación/viento inmóviles: detecta instancing
ausente en la build. Es una comprobación visual, no una medida de rendimiento.
XML y logs quedan en `unity/TestResults/U3/`; builds y capturas no se publican.
`scripts\u3.cmd create` regenera deliberadamente solo la escena U3; no hace
falta para jugar. No existe aún el ensayo de rendimiento del paso 11.

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
autor; U2 está aprobado y U3 está autorizado. No iniciar U4.
