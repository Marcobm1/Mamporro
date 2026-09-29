# MAMPORRO en Unity

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
[PROGRESO_U1](../docs/PROGRESO_U1.md).

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

La web y `Docs/Reference/` permanecen intactos. No iniciar U2 sin revisión del
autor y una nueva autorización.
