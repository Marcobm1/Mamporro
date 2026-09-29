# Checkpoint U1

> Estado vigente en [ESTADO_ACTUAL](ESTADO_ACTUAL.md): U1 aceptado; U2 aprobado por el autor ([PROGRESO_U2](PROGRESO_U2.md)). No comenzar U3.

## Autorización y alcance — 29/09/2026

U1 aprobado expresamente por el autor. Sustituye los estados anteriores de
«pendiente de aprobación», que se conservan como historial. U2 no está autorizado.
Proyecto en unity/, Windows x64 Mono, Unity 6000.6.3f1; prototipo de movimiento,
imagen retro y horda técnica. Sin combate completo, guardado, escalada, Steam ni
contenido de fases posteriores. Base web y referencias intactas.

Plan: verificar Editor/paquetes; implementar escena y pruebas; build y ensayos;
verificar referencia web, documentar, commits pequeños y push directo sin PR.

## Comprobación inicial

- Árbol limpio en b060862262c45c9cc26b2104b4297ba421679947.
- Rama claude/zen-pasteur-674ik0.
- CPU consultada: AMD Ryzen 7 7700X 8-Core Processor.
- GPU: NVIDIA GeForce RTX 4070 Ti SUPER; también AMD Radeon(TM) Graphics integrada.
- RAM visible al sistema: 33463676928 bytes (aproximadamente 32 GB instalados).
- No había procesos Unity.exe. Consultas solo de modelos/capacidad, sin identificadores.
- Catálogo del Editor: URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0.
  Pendiente comprobar resolución efectiva/importación.
- El equipo del autor no representa requisitos mínimos comerciales.

## Decisiones del ensayo

1080p principal y 1440p secundario; altura interna 360 por defecto, 240/480 opcionales.
300 entidades: objetivo 60 FPS; 500/750: margen; 1000: estrés sin promesa de FPS.
No son topes del juego ni balance. Medidas solo locales, sin telemetría externa.
Separar Editor/build, lógica/frame completo e instrumentación/build normal.

## Implementado

- Proyecto mínimo creado directamente sin aplicar una plantilla sobre `unity/`.
- Escena `Assets/Mamporro/Generated/U1_Patio.unity`, malla, materiales, personaje
  original y datos editables; sus `.meta` generados por Unity.
- Movimiento/cámara, salto variable y deslizamiento con valores de la referencia,
  pausa/foco/cursor. Colisión analítica compartida con el patio triangular.
- URP con RenderTexture puntual, interna 240/360/480, niebla, dither/snap
  conmutables y UI técnica uGUI a resolución de salida.
- Horda centralizada en arrays y rejilla espacial, pool reutilizable y render
  por instancias. No hay Rigidbody ni Update por enemigo; no se introduce ECS.
- Ensayo local automatizado, cuatro cargas y dos resoluciones; CSV/JSON/capturas.
- Lanzadores CMD: `scripts\u1.cmd edit`, `play`, `build`, `benchmark`.

## Versiones efectivas y arranque

Unity 6000.6.3f1 (45d8eee7de74), URP/Core/ShaderGraph 17.6.0, Input System 1.20.0,
uGUI 2.6.0, Test Framework 1.8.0. Las fija `Packages/packages-lock.json`.
uGUI 2.0.0 solicitado inicialmente se resolvió a la versión integrada 2.6.0;
el manifiesto se alineó con ella. No se instaló software adicional.
Burst/Collections son dependencias transitivas de URP, no una adopción de DOTS.

La primera ejecución aislada no pudo escribir cachés en AppData y quedó detenida
en inicialización de licencia. Se terminó solo esa instancia propia. Fuera del
aislamiento: licencia operativa, paquetes resueltos e importación con salida 0.
No se cerraron sesiones ajenas ni se leyeron credenciales.

## Pruebas nuevas de U1 (separadas de U0)

- Creación de escena y compilación C#: correcta, salida 0 (`unity-create.log`).
- Primer Edit Mode: 9/9 correctas (`unity/TestResults/edit.xml`).
- Primer Play Mode con gráficos: 1/1 correcta, carga de escena, RenderTexture,
  movimiento de horda, pausa y reinicio a 1000 (`unity/TestResults/play.xml`).
- Primera build Windows x64 Mono normal: correcta, 36,5 s según Unity
  (`unity-build.log`). Lanzada realmente con D3D11 y RTX 4070 Ti SUPER.
- `npm.cmd run typecheck`: correcto.
- `npm.cmd test`: 202/202 tests, 27/27 ficheros.
- `npm.cmd run build`: correcto; mismos nombres de assets que la base web.
- `node --test scripts/unity-reference-bytes.test.mjs`: 4/4 correctas.
- `node scripts/unity-reference.mjs`: correcto, datos y medios coinciden.
- No se repitieron pruebas de navegador: no cambió la base web.

Incidencias de comandos: PowerShell bloqueó `npm.ps1`; se usó `npm.cmd` sin cambiar
la política del sistema. El primer verificador coincidió con el Editor abierto
y falló al vigilar UnityLockfile (EBUSY). Repetido sin ese bloqueo, pasó. No se
cambió el verificador, no se regeneró la referencia y no se usó `--write`.

## Rendimiento — ensayo completado

Primer lanzamiento de build con ventana oculta: **inválido**. Capturas negras,
sin render efectivo y buffer de muestras lleno. Descartar los JSON 1080p con
sellos `20260929T003628121` y `20260929T003708396`; no prueban FPS del juego.
Se corrigió el lanzador para crear ventana gráfica visible. El segundo lanzamiento
fue exploratorio. La pasada definitiva incorpora un contador de frames realmente
renderizados y rechazo de ensayos sin render o con buffer agotado.

**Ocho condiciones definitivas completadas**, una pasada por condición: cargas
300/500/750/1000 a 1920×1080 y 2560×1440, ambas salidas confirmadas realmente.
Build normal Mono con instrumentación local, D3D11 en RTX 4070 Ti SUPER, calidad
U1 Retro, interna 640×360, dither/snap activados, VSync 0 y FPS ilimitados.
Semilla 6741, circuito de 8 s a paso fijo 60 Hz, cámara yaw 0/pitch 20; 10 s de
calentamiento y 30 s medidos por condición. XML/logs/CSV/JSON/PNG solo locales.

Informe completo: `unity/TestResults/RESUMEN.md`, regenerable mediante
`node scripts/u1-report.mjs`. Solo acepta registros con `validRender=true`.
La pasada definitiva corresponde a los sellos UTC desde `20260929T004839732`
hasta `20260929T005324256`. No mezclarla con los ensayos anteriores.

Conclusión limitada: el objetivo de 60 FPS con 300 tiene margen en este patio y
equipo; las cargas mayores también mantienen margen. Hay picos aislados por
encima de 16,67 ms. El informe incluye media, P95, P99, máximo, proporción de
frames sobre presupuesto, CPU, GPU cuando es fiable y memoria Unity final.
Simulación fija a 60 Hz/render ilimitado: muchos frames no contienen tick de horda;
P95 no equivale al coste de actualizar enemigos. Sin intervalos de confianza.

Limitaciones de métricas: ProfilerRecorder de GC no disponible en esta build
normal (N/D, no cero). GPU ofrece valores físicamente imposibles en 1440p con
300/750/1000: se descarta la media GPU completa de esas condiciones en el resumen.
CSV/JSON originales se conservan sin corregir ni filtrar. No se midió VRAM,
pico de memoria ni rendimiento del Editor. Play Mode valida integración, no FPS.
No extrapolar a equipos modestos ni al futuro combate completo.

Capturas revisadas de build: 1080p/300 y 1440p/1000 muestran escenario, personaje,
horda, niebla, dither e interfaz legible. La valoración estética/jugable es del autor.

## Verificación final y artefactos

- Configuración regenerada con calidad U1 Retro; compilación sin errores ni
  avisos C# en los logs finales.
- Edit Mode: **11/11**. Incluye determinismo 300/500/750/1000, obstáculos,
  movimiento/salto/deslizamiento, alturas/pendientes y cero asignaciones
  gestionadas en el bucle aislado de horda tras calentamiento.
- Play Mode: **1/1**. Incluye escena ejecutándose con render real no uniforme,
  opciones 240/360/480, relación de aspecto, pausa/reinicio y pool.
- Build Windows x64 Mono normal reconstruida y ejecutada en los ocho ensayos;
  el lanzador terminó con salida 0. `unity/TestResults/build.log` conserva el detalle.
- `.meta` comprobados: ninguno ausente. No hay cambios en `src/`, lockfile web
  ni `unity/Docs/Reference/`. Servicios Unity Analytics/Ads/diagnóstico desactivados.
- `git diff --check` y `git diff --cached --check`: correctos; índice con 102
  archivos de U1/documentación y sin cambios fuera de él al cerrar la revisión.
  No quedan instancias de Editor ni build lanzadas por estas pruebas.
- Espacios finales de YAML/.meta generados limpiados sin modificar valores;
  no se vuelven a atribuir pruebas Unity por esta limpieza textual.
- SHA-256 del ensamblado de la build `Mamporro.Runtime.dll`:
  `C7178EAE107C6F37572C89C9F456185CE57A134899E02EA74FF39BD8232F6942`.
- Build: `unity/Builds/U1/Mamporro-U1.exe`, conservar toda su carpeta.
- Guía de apertura/controles/ensayo: `unity/README.md`.

## Siguiente paso exacto

Revisión manual del autor de U1. Abrir el proyecto o la build siguiendo
`unity/README.md`; registrar incidencias de controles y aspecto antes de dar
U1 por aprobado. No comenzar U2.

## Git y revisión humana

Rama: `claude/zen-pasteur-674ik0`. El bloqueo anterior por falta de identidad se
resolvió cuando el autor indicó su nombre y correo. Identidad configurada solo
en este repositorio, sin modificar la global.

- Commit de implementación: `abe0a9b7f0b8c53f478b91c870341999df2ea073`,
  «Implementa U1: movimiento, render retro y horda técnica».
- Push correcto a `origin/claude/zen-pasteur-674ik0`, desde `b060862` hasta
  `abe0a9b`, sin PR ni force-push. Este checkpoint se entrega en un commit documental
  posterior, identificable por «Registra la entrega de U1 y el estado local».
- Se publicó el índice validado de 102 archivos. Al retomar había modificaciones
  posteriores de Unity fuera del índice: 14 archivos modificados y dos ajustes
  nuevos (`PackageManagerSettings.asset` y `URPProjectSettings.asset`). Se
  conservaron sin sobrescribir ni incluir en el commit de U1. El árbol local
  no está limpio por esas modificaciones; revisar su intención por separado.
- Revisión del índice con `git diff --cached --check` correcta antes del commit.
  No se repitieron pruebas de juego en esta entrega Git; los resultados anteriores
  corresponden a la implementación preparada, no a los cambios locales posteriores.
- Los resultados, la build y las cachés siguen ignorados y no se publican.

Pendiente del autor: tacto de controles, cursor/foco, legibilidad, aspecto y
recorrido por rampas/deslizamiento. No se dará U1 por aprobado visualmente por
haber pasado pruebas automáticas. No iniciar U2.
