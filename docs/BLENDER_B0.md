# B0 — Spike Blender / pipeline de assets 3D

Objetivos confirmados por el autor el 04/10/2026; plan concretado el 05/10/2026, después de aprobar U6. **IMPLEMENTACIÓN AUTORIZADA Y EN CURSO (05/10/2026), pasos B0.1–B0.6.** Pelusa, Doña Remedios y módulo de pared aprobados. Suelo 300: p95 ≤ 16,67 ms; 500/750 obligatorios para comparar coste/calidad, sin umbral rígido de 60 FPS. No instalar plugins ni activar LFS por anticipación. El spike precede a cualquier adopción artística masiva.

## Cómo retomar

- **Base:** migración U0–U6 cerrada; cierre formal publicado en `39602c8aaba3ca163cde39c654b005f23f9e5f2f`. Cierre técnico anterior `c84b62d`; código de entrega `daaa626` y paquete conservados en [PROGRESO_U6](PROGRESO_U6.md).
- **Paso actual:** B0.1–B0.4 **terminados**. B0.5 **en curso**: estrategias B/C/D implementadas y probadas (commit intermedio); batería de medida 300/500/750 pendiente de ejecutar o de resumir.
- **Terminado:** instalación real 5.2.2 LTS, background/Python/FBX, GUI capturada y cerrada sin guardar; `scripts\blender.cmd verify` implementado y ejecutado. Arnés QA `-b0-benchmark` (`unity/Assets/Mamporro/U3/B0Benchmark.cs`), builds QA separadas `U3Project.BuildB0`/`BuildB0Development` y lanzador `scripts\b0.cmd build|devbuild|benchmark|devdiag|summary`; base A medida (resultados abajo). Contrato B0.2: manifiesto `art/blender/b0/manifest.json`, scripts `scripts/blender/b0_*.py`, importador `Editor/B0AssetImport.cs`, fixture QA y pruebas Blender 22/22 + Unity 8/8. B0.3: fuentes `art/blender/b0/{pelusa,remedios,pared}`, FBX en `Art/B0/{Pelusa,Remedios,Pared}`, pruebas Blender 25/25 + Unity 22/22 y capturas locales (`scriptslender.cmd capture`). B0.4: escena aditiva `unity/Assets/Mamporro/QA/B0/B0_QA.unity` (solo builds B0), `U3/B0Visuals.cs` (biblioteca, horda instanciada, `VisualRoot`), `U3/B0VisualCheck.cs`, gancho `RunRenderer.EnemyVisual` y `U3Game.AvatarRoot`; `scripts0.cmd create|play|visual|jugar`.
- **Sin commit deliberadamente:** los siete ajustes Unity protegidos enumerados en PROGRESO_U6; no publicar ni restaurar. Product Name ya es MAMPORRO y no se vuelve a cambiar.
- **Siguiente acción exacta:** B0.5 — estrategias de horda en el arnés `-b0-benchmark` (A base U6, B Pelusa estática instanciada, C poses discretas instanciadas, D vertex animation en textura) con el mismo enemigo/clip/cámara, 300/500/750, 1080p/1440p, 3 pasadas alternadas + Development; capturas comparables; decidir solo con datos (puede ser ninguna).
- **Continuidad:** una pieza verificable por commit español, pruebas reales, checkpoint aquí, fetch antes de push normal a `claude/zen-pasteur-674ik0`. Sin PR. Parar ante nueva decisión importante y al cierre para revisión artística/manual.

Auditoría inicial futura, desde CMD en la raíz del repositorio:

```cmd
git fetch origin
git status --short --branch --untracked-files=all
git rev-parse HEAD origin/claude/zen-pasteur-674ik0
```

## Responsabilidades

Blender: producción de personajes, enemigos, armas, props y piezas modulares de estructuras; UV, vertex colors, rigging, skinning y animaciones. Unity: gameplay, combate, colliders/física, mundo y generación procedural, pools/rejilla/hordas, ensamblaje de escenarios, shaders/materiales e iluminación/render final, integración y optimización runtime. No reconstruir en Blender lo que convenga mantener procedural en Unity.

Versión comprobada y soportada para B0: **Blender 5.2.2 LTS**, build `d13f752e3b9c`, Python integrado `3.13.13`. Ejecutable encontrado bajo `%ProgramFiles%\Blender Foundation\Blender 5.2\blender.exe`; ruta absoluta real solo en informes locales. `scripts\blender.cmd verify` detecta instalación única o respeta `MAMPORRO_BLENDER`, sin cambiar PATH ni instalar. No actualizar versión ni añadir plugins por defecto.

## Pipeline a validar

`.blend fuente → exportación automatizada → FBX → Unity`. Los `.blend` son fuentes editables; Unity consume los FBX exportados si el spike confirma el enfoque. La producción no dependerá de la importación directa de `.blend`. No introducir glTF, plugins ni otro formato principal sin necesidad demostrada.

B0 definirá convenciones para escala/unidades, ejes, transforms, pivotes/orígenes, triangulación, normales, UV, vertex colors, rigs, nombres, materiales, clips de animación, rutas de exportación y reimportación estable. Automatización con Blender en background y Python, compatible con CMD; detectar/configurar instalación sin rutas absolutas locales versionadas.

**Implementado y probado:** `scripts\blender.cmd verify`, `author` (crea la fuente inicial de un asset solo si no existe; `-Force` la regenera), `export` (todos o `-Asset <id>`; `-Force` reescribe aunque el contenido no cambie) y `test` (contrato y exportador en Blender); `scripts\b0.cmd edit` ejecuta las pruebas Unity del contrato. Resultados en la sesión B0.2. Los agentes podrán ayudar mediante scripts a generar, modificar, validar y exportar assets; la revisión visual/artística humana seguirá siendo obligatoria.

## Separación visual y rendimiento

Dirección: `lógica/collider/pool → VisualRoot → modelo/render`. El modelo no es fuente de verdad del combate. Sustituir cajas provisionales no debe cambiar por sí solo IDs, RNG, daño/vida, movimiento lógico, hitboxes/colliders, spawn, drops, director, resultados, guardado ni determinismo de las referencias. Cambios de colliders requieren decisión expresa.

**No asignar un Animator + SkinnedMeshRenderer a cada uno de cientos de enemigos comunes sin medir.** Preservar arquitectura centralizada, pools, grid e instancing mientras sea ventajosa. Comparar animación compatible con instancing, poses horneadas, vertex animation, meshes/poses discretas u otra solución masiva justificada; ninguna está elegida antes del benchmark. Jugador, jefes y posiblemente élites pueden justificar rigs esqueléticos más completos por su menor número de instancias, siempre medido.

## Spike mínimo y criterio de aceptación

- Un enemigo común low-poly original representativo.
- Un personaje/avatar provisional mejorado con un caso representativo de rig/animación.
- Una pieza modular de estructura/prop y al menos una animación.
- Exportación repetible, importación/reimportación Unity y separación VisualRoot/collider/lógica.
- Prueba de materiales, vertex colors y texturas pequeñas cuando corresponda.
- Benchmark de horda representativa, incluyendo 300/500/750 entidades cuando tenga sentido, comparado con la base anterior y con condiciones registradas.

No adoptar masivamente el pipeline hasta demostrar mejora de producción/visual sin romper estabilidad, equivalencia lógica ni presupuesto de rendimiento. No confundir benchmark lógico con FPS reales; revisión humana obligatoria.

## Git, binarios y autoría

No activar Git LFS preventivamente. Medir primero tamaños reales de `.blend`, `.fbx`, texturas y otros binarios fuente; proponer después Git normal o LFS según ventaja demostrada. Separar fuentes editables de exportados y evitar reexportaciones masivas sin necesidad.

Todo arte nuevo original de MAMPORRO. Megabonk u otros juegos pueden orientar escala, verticalidad, legibilidad y dirección general, sin copiar modelos, texturas, animaciones, mapas, iconos, assets, personajes ni diseños concretos. No comprar/descargar packs o assets de terceros por defecto. Sin telemetría externa.

## Relación con objetivos posteriores

B0 prepara acabado retro profesional, mejores modelos, animaciones de jugador/enemigos/ataques/impactos/muertes/jefe/mundo, estructuras mayores, mundo geométrico con mesetas/rampas/verticalidad, escalada, más personajes/enemigos/armas/tomos e iconos/ilustraciones originales. No sustituye ni implementa por adelantado estos bloques.

Balance separado: menos enemigos iniciales, individualmente más resistentes, crecimiento progresivo y bastante más oro **de partida**, no Calderilla del Caos. Sin cifras nuevas. La hoja de ruta canónica sigue en [HOJA_DE_RUTA](../unity/Docs/HOJA_DE_RUTA.md).

## Plan autorizado y dependencias

El plan está autorizado; el registro de sesión distingue lo ejecutado de lo pendiente. Cada fila termina con pruebas, evidencia y checkpoint antes del commit/push. Dependencia secuencial salvo el diseño de convenciones, que puede prepararse con la base ya medida; no producir el catálogo completo.

| Paso | Trabajo y dependencia | Aceptación de la pieza | Evidencia / commit propuesto |
| --- | --- | --- | --- |
| B0.1 — entorno y base | Tras autorización, detectar Blender real, GUI, background y Python; fijar versión exacta tras verificarla. Conservar U6 y medir de nuevo su base visual en condiciones del spike antes de añadir modelos. | Ruta/versionado registrados localmente, sin ruta personal en archivos versionados; comandos terminan correctamente. Base normal arrancable, perfiles de 300/500/750 con recuento comprobado y configuración reproducible; ninguna escritura en progreso personal. | Informe de entorno, logs, captura de GUI y CSV base con commit/hardware/condiciones. «Documenta el entorno y la base del spike 3D». |
| B0.2 — contrato de assets | Con B0.1 resuelto, definir carpetas, manifiesto y convenciones de la tabla siguiente; preparar validación y casos mínimos válidos/inválidos. No crear aún arte de catálogo. | Una muestra de escala/ejes/origen atraviesa Blender→FBX→Unity sin giros ni factor 100 inesperados. Errores de contrato detectados con explicación, sin cambiar fuentes en silencio. | Contrato, fixture geométrica de QA, pruebas de dimensiones/orientación, comparación de normales/colores. «Define el contrato de assets 3D». |
| B0.3 — tres fuentes y exportador | Depende del contrato. Pelusa, Doña Remedios y un módulo de pared/prop como propuesta de contenido; solo representativos. Rig y clip de locomoción en el avatar, deformación/poses de prueba del enemigo para comparar animación masiva. Exportación background/Python con manifiesto explícito. | Fuentes editables fuera de Assets; originalidad, escala, rig/pesos y clip revisables; exportar dos veces no modifica los .blend ni acumula acciones/objetos. Exportación fallida conserva la última salida válida. | .blend/FBX de esos tres assets, logs, inventario de polígonos/huesos/materiales/tamaños, turntable y clip. «Añade los assets de prueba y su exportación». |
| B0.4 — reimportación e integración aislada | Reutiliza exportador. Escena/prefabs QA separadas; adaptador visual sobre el estado existente, sin nueva simulación. Avatar con rig, prop y enemigo estático instanciado como control intermedio. No sustituir por defecto la partida aprobada. | Reimportar mantiene GUID, referencias, materiales, clips y transform; ningún collider automático ni root motion que mueva la lógica. Reinicio/pool limpio; mismos estados lógicos con visual activado/desactivado. | Edit/Play, captura de VisualRoot y collider superpuestos, prueba ida/vuelta y recarga, build QA. «Integra la prueba visual sin cambiar la simulación». |
| B0.5 — animación de horda | Depende de B0.4. Comparar al menos dos candidatos masivos con el mismo enemigo/clip/cámara: poses/meshes discretos instanciados y deformación/poses horneadas en datos para shader (por ejemplo vertex animation). Estudiar alternativas solo con motivo medido; no elegir la ganadora por anticipado. | Arrays/pools/grid preservados, fases por instancia sin RNG de gameplay, sin Update/Rigidbody por enemigo. Medición de coste real, memoria y limitaciones visuales; resultado puede ser no adoptar ninguna estrategia. | CSV/JSON por fotograma, capturas/vídeo comparables, informe normal separado de Development. «Compara la animación masiva del spike». |
| B0.6 — regresión y conclusión | Tras comparación, probar exportación/importación desde entorno documentado, build y sesiones QA; inventariar binarios para propuesta Git/LFS. Presentar resultados y limitaciones al autor. | Criterios técnicos satisfechos o fallos claramente declarados; referencias inalteradas. Autor revisa originalidad, silueta, movimiento y legibilidad. No adopción masiva ni siguiente bloque automático. | Resumen versionado, ubicación y huellas de pruebas, guía manual, recomendación razonada y checkpoint. «Cierra el spike 3D para revisión del autor». |

Dependencias externas: Blender instalado y funcional (por comprobar), exportador FBX disponible en esa instalación, Unity/Mono del proyecto con licencia, GPU para la build y Node/dependencias para los verificadores heredados. No actualizar Unity/URP ni añadir paquetes para resolver supuestos problemas. Si FBX requiere componentes que no estén disponibles, diagnosticar antes de proponer instalación o cambio de formato.

## Comprobación futura del entorno desde CMD

Secuencia del plan, ejecutada en B0.1 según registro inferior. Primero buscar instalaciones; la ausencia en PATH no demuestra que no esté instalado. Búsquedas acotadas, sin recorrer ni modificar todo el disco:

```cmd
where blender.exe
if exist "%ProgramFiles%\Blender Foundation" where /r "%ProgramFiles%\Blender Foundation" blender.exe
if exist "%LOCALAPPDATA%\Programs" where /r "%LOCALAPPDATA%\Programs" blender.exe
reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" /s /f Blender /d
reg query "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" /s /f Blender /d
```

Si hay varias instalaciones, registrar candidatas y elegir explícitamente la que se validará; si no aparece ninguna, revisar accesos directos/instalaciones portables con el autor antes de pedir instalar. La referencia «5.2.2 LTS» no demuestra ni disponibilidad local ni compatibilidad. Tras encontrar el ejecutable, introducir su ruta real solo en la sesión CMD (no versionarla):

```cmd
set /p "MAMPORRO_BLENDER=Ruta completa real a blender.exe, sin comillas: "
if exist "%MAMPORRO_BLENDER%" echo Ejecutable localizado
"%MAMPORRO_BLENDER%" --version
"%MAMPORRO_BLENDER%" --help
start "" "%MAMPORRO_BLENDER%" --factory-startup --disable-autoexec
```

Comprobar visualmente el arranque normal y cerrar sin guardar preferencias ni escenas. Después, prueba background/Python integrado, sin generar modelos ni guardar archivos:

```cmd
"%MAMPORRO_BLENDER%" --background --factory-startup --disable-autoexec --python-exit-code 1 --python-expr "import bpy,sys; print('BLENDER',bpy.app.version_string); print('BIN',bpy.app.binary_path); print('PYTHON',sys.version); print('BACKGROUND',bpy.app.background); assert bpy.app.background"
echo %ERRORLEVEL%
```

Las opciones CLI están descritas en el [manual oficial de Blender](https://docs.blender.org/manual/en/4.3/advanced/command_line/arguments.html); se comprobarán contra `--help` del binario realmente instalado, no se asume que esa documentación fija la versión del proyecto. GUI visible solo para la comprobación solicitada; automatización posterior en background, sin cambiar configuración personal ni instalar complementos.

## Convenciones propuestas para validar, no decisiones ya implementadas

Carpetas propuestas: `art/blender/b0/` para .blend y fuentes de texturas; `art/blender/b0/manifest.json` para catálogo y parámetros; `scripts/blender/` para Python y configuración versionada sin rutas locales; `unity/Assets/Mamporro/Art/B0/` para FBX, materiales/texturas runtime y sus .meta; `unity/Assets/Mamporro/QA/B0/` para prefabs/escena de comparación. Informes voluminosos en `unity/TestResults/B0/` (ignorado), resumen técnico y huellas en documentación versionada. Aplicar por pasos, sin crear todo el catálogo.

| Convención | Propuesta / prueba que debe resolverla |
| --- | --- |
| Escala/unidades | 1 unidad lógica Unity = 1 metro; fuente en metros. Patrón asimétrico de tamaño conocido, comparación de bounds importados con tolerancia explícita fijada en el contrato antes de medir candidatos. No escalar colliders para acomodar una exportación incorrecta. |
| Ejes/transforms | Blender Z arriba; Unity Y arriba. Fijar orientación frontal tras importar un marcador asimétrico y comprobar handedness, sin prometer un preset FBX antes de ensayarlo. Aplicar transforms de mallas con cuidado antes del rig; no aplicar a ciegas transforms después de animar. La conversión de coordenadas web sigue siendo presentación, sin mover la lógica. |
| Pivotes | Avatar/enemigo a nivel de suelo y centrados respecto a su anclaje lógico; módulo en una esquina/base documentada para ensamblaje. Comprobar posición al intercambiar visual y al reusar el pool. |
| Topología/normales | Triangulación controlada en salida, fuente editable conservada; comparación de triángulos y normales tras reexportar. Hard/soft edges deliberados y coherentes con la silueta retro; comprobar seams/splits y que no dependan de un cálculo diferente del importador. |
| UV/vertex colors | Una muestra con UV y otra con paleta/vertex colors; nombres de canales explícitos y sin atributos perdidos. No presuponer traducción automática de nodos/materiales Blender. |
| Materiales/texturas | Pocos slots compartidos y materiales finales Unity; probar shader retro, iluminación, dither/snap y destellos. Textura pequeña original solo si aporta al ensayo; fijar dimensiones/filtro/espacio de color tras comparar legibilidad. Sin materiales duplicados por instancia. |
| Nombres | IDs de assets estables y distintos de IDs persistentes de gameplay; nombres ASCII claros para objetos/mallas/huesos/materiales/clips. Manifiesto enumera exactamente los objetos que se exportan: no depender de selección activa ni exportar cámaras/luces por accidente. |
| Rig/skinning | Rig mínimo representativo, pesos normalizados, sin vértices sin asignar; medir huesos/influencias reales. Clip in-place, sin root motion lógico, constraints horneadas si hacen falta; no depender de drivers/addons externos. |
| Clips | Nombres, rangos, frecuencia de muestreo, duración y bucle explícitos en manifiesto. Probar inicio/fin, pausa, reinicio y varias fases; animación no determina impactos ni sincroniza la simulación mediante eventos. |
| Repetibilidad | Exportación a temporal y validación antes de sustituir la salida. Comparar hashes y contenido semántico normalizado de mallas/rig/clips; si FBX contiene metadatos variables, registrarlos sin afirmar igualdad binaria. No ocultar cambios visuales bajo una normalización demasiado amplia. |
| Reimportación | Conservar .meta/GUID y referencias tras dos reimportaciones y reapertura de escena. Probar rutas con espacios, fuente ausente/contrato inválido/fallo de exportación; ningún caso pisa fuentes ni deja una salida parcial como válida. |

`VisualRoot` es un punto de separación conceptual: para jugador/prop puede ser un Transform; para horda instanciada, un registro visual/pool y matrices de render centralizadas. No exige un GameObject por enemigo. Un cambio de modelo nunca modifica los datos del núcleo. El prototipo visual permanecerá seleccionable solo en QA hasta que se autorice su adopción.

## Protocolo de comparación de rendimiento propuesto

Cuatro condiciones mínimas: **A** representación U6 original; **B** nueva malla estática instanciada (separa coste geométrico del animado); **C/D** dos estrategias animadas sobre el mismo contenido. Avatar rigged medido por separado y después junto a la horda; no añadir un jefe/élite nuevo al catálogo para probarlo.

- Build Windows x64 Mono normal, mismo commit del arnés, semilla, cámara/recorrido, lógica a paso fijo, enemigos vivos estables 300/500/750 y misma distribución/pantalla ocupada. Apariciones controladas de QA sin modificar director ni balance. Medir primero presentación aislada y luego un perfil integrado de combate existente con audio/efectos iguales. Comprobar que la geometría se dibuja de verdad, no todo culled/fuera de cámara.
- Equipo documentado sin identificadores personales, salida 1920×1080 y 2560×1440, interna 360, mismas opciones retro, VSync desactivado, sin límite para medir margen. Documentar foco, build, resolución efectiva y calentamiento. Propuesta: 10 s de calentamiento + 30 s de captura por condición, tres repeticiones alternando A/B/C/D; anotar variabilidad, no seleccionar solo la mejor pasada.
- CSV por fotograma: tiempo de frame, p50/p95/p99/máximo del resumen, conteo de entidades/instancias realmente dibujadas, eventos de GC y picos. Registrar CPU de lógica y envío/render/animación mediante marcadores válidos; no llamar tiempo GPU al tiempo de envío CPU. Draw calls, batches, vértices/triángulos y materiales; memoria total y buffers/poses/texturas añadidos, incluidos costes de carga y tamaño en disco.
- Allocations/GC y atribución mediante diagnóstico Development separado cuando las APIs sean válidas. El HUD ya asigna memoria en U5: comparar con la misma base y distinguir el incremento visual, sin atribuirle toda la asignación previa. GPU o contador no fiable = **N/D**, conservando el dato bruto y la razón. No transformar FPS de lógica ni medidas Development en FPS de entrega.
- Comparar deltas absolutos y relativos y dispersión por carga/resolución; coste animación C/D frente a B y coste total frente a A. Capturas idénticas más vídeo corto de movimiento, pausas y reinicio para juzgar calidad, popping, deformaciones y legibilidad.
- Suelo aprobado: con 300 entidades p95 ≤ 16,67 ms en las condiciones del benchmark. Medir obligatoriamente 500/750 para comparar margen, no como cifras de balance ni requisitos comerciales. No basta con estar por debajo de ese suelo: si se pierde mucho margen, se presentan el coste y la ganancia visual, sin adopción masiva automática. Ningún presupuesto extra en ms/% está aprobado todavía; la elección final requiere revisión del informe.

## Pruebas y aceptación final de B0

1. **Exportador:** fixtures de contrato válidos/rotos; repetición; independencia de selección/configuración personal; fuente intacta; ruta con espacios; error de proceso/escritura y preservación de última salida buena. `verify/export/test` solo se anuncian como funcionales cuando existan, pasen y tengan ayuda CMD.
2. **Unity Edit Mode:** dimensiones/ejes/pivotes, normales/UV/colores/slots, rig y clips, referencias/GUID estables, ausencia de colliders generados y configuración de root motion. Comparación determinista del núcleo con visual on/off, mismo RNG y resultados; no regenerar corpus.
3. **Play Mode:** movimiento/pausa/reinicio, pool/reciclado sin poses residuales, daño y avisos conservados, sin duplicar entidades ni materiales. Escena QA y un caso integrado sobre mundo real; guardado aislado con `-u4-save-dir` o override temporal.
4. **Regresión:** al cerrar, `scripts\u3.cmd edit`, `scripts\u3.cmd play`, `scripts\u3.cmd build` y visual pertinente; verificador histórico aislado cuando se toque el punto de comparación determinista. No cambiar src ni referencias. Se parte de los 405 Edit / 39 Play registrados en U6, que son históricos hasta ejecutar de nuevo; anotar total real y nuevos casos. Web no requiere batería completa por editar un FBX si no ha cambiado su código/herramientas.
5. **Rendimiento:** condiciones A–D repetidas con datos reproducibles, diagnóstico separado y sin allocations por entidad/frame introducidas en estado estable; detectar coste inicial y crecimiento tras reinicios. Si falta una métrica, declararla N/D; si no puede compararse la carga, esa condición no valida la estrategia.
6. **Autor:** revisión de los tres assets y del clip, lectura a resolución retro y en horda, originalidad y utilidad del flujo de edición/reexportación. Capturas de fuente y Unity, ejes/collider/VisualRoot, primer/último frame y comparación A–D; ninguna captura está hecha en esta planificación.

B0 puede cerrar como **spike concluido sin adopción** si descubre límites importantes. Declarar **pipeline validado para proponer adopción** exige exportación y reimportación reproducibles, tres casos correctos, lógica/guardado intactos, regresión aprobada, coste explícito aceptable y aprobación visual/manual del autor. Ningún resultado autoriza sustituir el catálogo completo.

Medir tamaños de fuentes y exportados, crecimiento tras varias revisiones y ruido de reexportación antes de proponer Git normal/LFS. No activar LFS en este bloque sin decisión posterior informada. Resumen y tablas versionados; logs/CSV/capturas voluminosos locales con ubicación y huellas, sin datos personales ni telemetría externa.

## Decisiones del autor resueltas — 05/10/2026

1. Pelusa común, Doña Remedios con rig/locomoción y módulo de pared aprobados como casos representativos, no arte final completo.
2. 300 entidades: p95 ≤ 16,67 ms. Mediciones obligatorias 500/750 para coste, escalabilidad, CPU, memoria, animación, render, GC fiable y legibilidad. Sin 60 FPS rígidos para 500/750; ni adopción automática por superar el suelo de 300.

B0.1–B0.6 autorizados consecutivamente, checkpoint/commit/push por pieza. No hay decisiones bloqueantes. No repetir consultas resueltas; detenerse solo ante cambio importante no cubierto y al cierre técnico para aprobación visual/manual. LFS, formato distinto, plugins, cambios de reglas/colliders o adopción de producción requieren decisión expresa.

## Registro de planificación — 05/10/2026, Codex

Cierre formal U6 `39602c8aaba3ca163cde39c654b005f23f9e5f2f` publicado y remoto verificado antes de este plan. Lectura de BLENDER_B0, hoja de ruta, decisiones, arquitectura de render y automatización existentes. Esta sesión solo modifica documentación: pasos/dependencias/criterios, comandos futuros CMD, contrato propuesto, comparación y decisiones; revisión de enlaces y diff, siete excluidos con huellas conservadas. No se abrió Blender/Unity ni se ejecutaron pruebas de assets, suites, build o benchmark. No se crearon scripts, modelos, FBX ni carpetas de implementación.

Commit del plan: «Planifica el spike Blender tras cerrar la migración»; localizarlo con `git log --all --grep="Planifica el spike Blender tras cerrar la migración"`. Publicar con fetch/push normal y verificar remoto. Siguiente paso: esperar respuesta del autor; implementación B0 no autorizada.


## Sesión 05/10/2026 — B0.1a, entorno real (Codex)

- Partida: `24df07733bb264e6c88323a7302ae8fe6d444785`, rama correcta, local/remoto 0/0 tras fetch; solo siete ajustes Unity protegidos.
- Autor autoriza B0 completo y resuelve las dos propuestas anteriores. Se actualizan entradas de continuidad; migración U0–U6 permanece cerrada.
- Instalación encontrada por registro Windows y búsqueda acotada desde CMD; `where blender.exe` no la encontró en PATH, lo que no impedía usarla. Consulta CMD `call "%MAMPORRO_BLENDER%" --version`: 5.2.2 LTS, build d13f752e3b9c, Windows Release. Background + `--python-expr` confirmó Python 3.13.13 y exportador FBX. No instalar ni cambiar versión.
- GUI: arranque normal con factory-startup/disable-autoexec; título Blender 5.2.2 LTS, respondió. Primera captura de escritorio minimizada descartada; repetida desde `bpy.ops.screen.screenshot` en una ventana 1280×800, revisada visualmente, cierre automático con salida 0. Solo escena inicial en memoria, sin guardar .blend ni preferencias.
- Automatización: `scripts/blender.cmd`, `scripts/blender.ps1`, `scripts/blender/verify.py`. Detección única por variable/PATH/registro/carpeta estándar, versión exacta y registro del operador FBX mediante RNA; ningún path personal versionado.
- Comando ejecutado: `scripts\blender.cmd verify` → salida 0, Blender/Python/FBX válidos. Evidencias locales en `unity/TestResults/B0/Environment/`: version.log, background.log, verify.log, verified.json, gui.json y blender-gui.png. GUI y background comprobados; no es una prueba de exportación de assets.
- Pendiente dentro de B0.1: base de rendimiento nueva 300/500/750, aún no ejecutada. No se han creado assets, probado exportación/importación Unity ni ejecutado suites/build/benchmarks en esta pieza.
- Commit: «Verifica Blender y registra la autorización de B0»; fetch y push normal tras comprobar remoto, verificar SHA antes de continuar. Árbol fuera del commit: mismos siete ajustes protegidos.
- Siguiente paso exacto: arnés QA de comparación, base U6 y medidas de B0.1; después contrato B0.2. No dar B0.1 completo hasta tener la base medida.

## Sesión 05/10/2026 — B0.1b, base de rendimiento (Claude Code)

**Relevo recibido.** HEAD = `origin/claude/zen-pasteur-674ik0` = `56bd557`, 0/0 tras fetch. Árbol: los siete ajustes protegidos (mismo número de líneas de diff que en U6) y trabajo **sin commit** de Codex: `U3Project.BuildB0()` (10 líneas, build normal en `Builds/B0`) y `unity/Assets/Mamporro/U3/B0Benchmark.cs` (130 líneas, sin `.meta`). Ningún otro archivo B0; `unity/TestResults/B0` solo tenía `Environment/`. No se descartó nada: se revisó y completó.

**Revisión del arnés de Codex.** Válido y conservado: escena U6 real con `RunRenderer`, partida preparada y **en pausa** (sin ticks, director, RNG, drops ni guardado de partida; `Run.Time` debe seguir en 0), Pelusas (`Catalog.Enemies[0]`) inmóviles en rejilla fija a 60 m, cámara fija, salida retro 360 con dithering/snap, prueba real de píxeles con la horda dibujada y oculta antes de medir, 10 s + 30 s, CSV por fotograma, build QA en carpeta propia sin tocar `Builds/Windows`. Corregido/completado:

- sin `-u4-save-dir` lanzaba una excepción pero la escena seguía cargando con el progreso personal; ahora fuerza una carpeta temporal antes de cargar la escena y sale con código 2;
- errores de argumentos terminaban en excepción con el jugador abierto; ahora salen con código 1;
- comprobación de **frustum** de cada entidad (debe ser N/N) además de los píxeles;
- contadores añadidos: instancias de combate dibujadas, SetPass, triángulos, vértices, draw calls y batches (con validez), memoria del sistema, heap Mono, RAM, calidad, modo de pantalla; parámetros `-b0-strategy` (solo `base` por ahora; B/C/D en B0.5), `-b0-run` y `-b0-source`;
- una reanudación externa (Esc/«Continuar» con la ventana en primer plano) abortó una pasada al despausar la partida; el arnés se ejecuta ahora después de `U3Game` (`DefaultExecutionOrder(1000)`), vuelve a pausar en el mismo fotograma, antes de cualquier tick, y cuenta `externalUnpauses` (0 en las 21 pasadas válidas). `Run.Time == 0` sigue siendo obligatoria;
- `BuildB0()` limpia su carpeta; nueva `BuildB0Development()` en `Builds/B0Dev` solo para diagnóstico.

No cambia reglas, gameplay, RNG, guardado, settings, `productName` ni la entrega. El ensayo solo existe con `-b0-benchmark`.

**Pruebas ejecutadas (05/10/2026).**

- `scripts\b0.cmd build` → correcta (`unity\Builds\B0\MAMPORRO-B0.exe`, log `unity\TestResults\B0\Benchmark\build.log`); compila todo el proyecto.
- Humo manual 300/1080p (`TestResults\B0\Benchmark\Smoke`): correcto; captura revisada (horda visible sobre el mundo real), guardado aislado con solo `progress.lock`.
- `scripts\b0.cmd benchmark -Runs 1 -First 1|2|3` (tres órdenes, cada pasada recorre las seis condiciones antes de repetir) → 18/18 informes válidos, fuente `56bd557+local` (arnés sin commit en el momento de medir; mismo código que este commit). Resumen `TestResults\B0\Benchmark\Normal\agregado-20261005T131448.csv`.
- `scripts\b0.cmd devbuild` + `devdiag` → 3/3 válidos en `Builds\B0Dev` (Development), `TestResults\B0\Benchmark\DevDiag\agregado-20261005T131733.csv`.
- `scripts\u3.cmd edit` → **405/405** (`unity\TestResults\U3\edit.xml`). `scripts\u3.cmd play` → **39/39** (`unity\TestResults\U3\play.xml`). Mismos totales que U6; B0.1 no añade pruebas.
- `MAMPORRO.exe` de entrega intacto: SHA-256 `96b492cb…0873`, igual que en U6.

**Condiciones.** Build Windows x64 Mono normal (`Builds\B0`), D3D11, Unity 6000.6.3f1, calidad «U1 Retro», VSync 0, sin límite de FPS; AMD Ryzen 7 7700X, NVIDIA RTX 4070 Ti SUPER, 32 GB; salida 1920×1080 y 2560×1440 pedida en exclusiva (Unity informa `FullScreenWindow`), interna 640×360 (1080p y 1440p), dithering y snap activos; mapa MAMPORRO, cámara (0,88,−32)→(0,60,0) FOV 58, rejilla de 30 columnas a 1,1 m (300 = 10 filas, 750 = 25); 10 s de calentamiento + 30 s; 3 pasadas alternadas.

**Base A — representación U6 (build normal, mediana de 3 pasadas; ms por fotograma).**

| Entidades | Salida | p50 | p95 (rango) | p99 | máx. (peor) | >16,67 ms | CPU fotograma | envío cámara mundo (CPU) | instancias | SetPass | triángulos |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 300 | 1920×1080 | 0,369 | 0,463 (0,458–0,466) | 0,549 | 1,641 | 0 | 0,384 | 0,113 | 764 | 15 | 227 337 |
| 500 | 1920×1080 | 0,415 | 0,504 (0,502–0,506) | 0,582 | 1,008 | 0 | 0,430 | 0,116 | 1 164 | 15 | 232 137 |
| 750 | 1920×1080 | 0,473 | 0,558 (0,547–0,561) | 0,629 | 1,224 | 0 | 0,485 | 0,114 | 1 664 | 15 | 238 137 |
| 300 | 2560×1440 | 0,373 | 0,482 (0,480–0,487) | 0,561 | 1,110 | 0 | 0,389 | 0,114 | 764 | 15 | 227 337 |
| 500 | 2560×1440 | 0,416 | 0,504 (0,503–0,504) | 0,579 | 1,039 | 0 | 0,430 | 0,116 | 1 164 | 15 | 232 137 |
| 750 | 2560×1440 | 0,471 | 0,561 (0,557–0,561) | 0,635 | 1,095 | 0 | 0,485 | 0,114 | 1 664 | 15 | 238 137 |

Suelo 300 (p95 ≤ 16,67 ms): **cumple** en las tres pasadas y ambas salidas. Coste marginal medido de 300→750 Pelusas: ≈ +0,1 ms de p95 (dos cajas instanciadas por Pelusa; las 164 instancias restantes son interactuables y efectos fijos del mapa). Memoria Unity asignada ≈ 226 MB, memoria del sistema usada ≈ 284 MB, reservada 538 MB; 12–15 recolecciones gen0 por condición en 90 s medidos. Diagnóstico Development (no es rendimiento de entrega): p95 0,545/0,583/0,601 ms y **≈ 97–100 B asignados por fotograma, constantes entre 300 y 750** (asignación de base de la escena/HUD, no por entidad).

**Limitaciones.** Es **presentación aislada**: ni partida, ni simulación, ni FPS de juego. Frame time = intervalo real entre fotogramas; «envío cámara mundo» es CPU de la cámara del mundo, no GPU. **N/D:** tiempo GPU (FrameTimingManager devuelve valores absurdos, ~5·10⁹ ms, y no se valida), draw calls y batches (los contadores devuelven 0 también en Development con `RenderMeshInstanced` en Unity 6.6; se usa SetPass/triángulos/vértices), GC por fotograma en la build normal (solo Development). Los contadores de render son totales del fotograma (todas las cámaras). Equipo de gama alta, sin valor de requisito mínimo. Un pico aislado de 53,6 ms en el humo inicial tuvo CPU de fotograma normal (0,37 ms): pausa externa al trabajo del fotograma; no apareció en las 18 pasadas válidas (peor máximo 1,64 ms). Resultados voluminosos (CSV por fotograma, JSON, capturas, logs) locales e ignorados en `unity\TestResults\B0\Benchmark\`.

**Comandos para repetirlo (CMD, raíz del repositorio, Editor cerrado):**

```cmd
scripts\b0.cmd build
scripts\b0.cmd benchmark -Runs 1 -First 1
scripts\b0.cmd benchmark -Runs 1 -First 2
scripts\b0.cmd benchmark -Runs 1 -First 3
scripts\b0.cmd summary
scripts\b0.cmd devbuild
scripts\b0.cmd devdiag
```

Cada `benchmark -Runs 1` abre la build B0 a pantalla completa 6 veces (~5 min); no usar el equipo durante la medida.

**Commit B0.1:** «Documenta el entorno y la base del spike 3D» (arnés, builds QA, lanzador y este checkpoint). Fuera del commit: los siete protegidos (SHA-256 iniciales: RetroPipeline `f88523da…`, URP global `71b8ad93…`, GraphicsSettings `35dd86b4…`, ProjectAuditorSettings `9e68444b…`, ProjectSettings `4505cd00…`, PackageManagerSettings `7698172a…`, URPProjectSettings `68d75e5e…`) y el borrador B0.2 `scripts/blender/b0_contract.py`, todavía sin probar.

## Sesión 05/10/2026 — B0.2, contrato de assets (Claude Code)

**Estructura.** Fuentes editables en `art/blender/b0/<asset>/*.blend` (fuera de `unity/Assets`) y catálogo en `art/blender/b0/manifest.json` (id, tipo `static|modular|rigged`, fuente, salida, objetos exportados exactos, slots de material en orden, bounds esperados, sondas/colores de QA, clips). Scripts versionados sin rutas locales en `scripts/blender/`: `b0_contract.py` (contrato y ajustes FBX), `b0_author.py` (fuentes iniciales por código), `b0_export.py` (exportador), `b0_test.py` (pruebas). Salida Unity en `unity/Assets/Mamporro/Art/B0/<Carpeta>/<Id>.fbx` + `<Id>.b0.json` (gemelo derivado del manifiesto, sin fechas) y sus `.meta`. Importador `unity/Assets/Mamporro/Editor/B0AssetImport.cs`; pruebas `unity/Assets/Mamporro/Tests/B0/` (asamblea `Mamporro.B0Tests`, solo Editor). Informes locales ignorados en `unity/TestResults/B0/{Author,Export,Test}`.

**Contrato cerrado (versión 1), comprobado con la fixture `B0_Fixture`.**

| Convención | Regla | Cómo se comprueba |
| --- | --- | --- |
| Escala | Escena métrica, `scale_length` 1; 1 m Blender = 1 unidad Unity. FBX `FBX_SCALE_ALL`; Unity `globalScale` 1, `useFileScale`. | Bounds Blender y Unity con tolerancia 0,001 m; caso roto «factor 100» rechazado. |
| Ejes y orientación | Blender Z arriba; **frente del asset hacia +Y y su derecha hacia +X**. Exportación con ejes nativos (`axis_forward=Y`, `axis_up=Z`); Unity `bakeAxisConversion` hornea Z→Y en mallas/animaciones: Blender (x,y,z) → Unity (x,z,y), frente +Z, sin espejo. | Sondas asimétricas (frente, derecha, arriba) en su vértice exacto; jerarquía sin rotación. |
| Transforms y pivote | Objetos raíz en el origen, sin rotación y escala 1; pivote = origen de la fuente (personajes/enemigos a nivel de suelo y centrados; módulos en la esquina/base documentada por asset). | Contrato rechaza escala, rotación y pivote desplazados; Unity sin transform residual, `min.y` = 0. |
| Triangulación | Fuente editable con quads/n-gons; triangulación en la salida (`use_triangles`). | Triángulos fuente = reimportación Blender = Unity (64). |
| Normales | Aristas duras/suaves de la fuente (caras lisas/planas, sharp edges); se exportan las normales calculadas (`mesh_smooth_type=OFF`) y Unity las importa (`importNormals=Import`). El shader retro actual usa normales planas, pero se conservan. | Cajas con normales alineadas a ejes; solo la antena lisa tiene normales promediadas. |
| UV | Exactamente una capa `UVMap`. | Contrato rechaza sin UV; Unity importa UV. |
| Color de vértice | Exactamente un atributo `Col`, `BYTE_COLOR` en esquinas, autorizado en sRGB; exportado en **lineal** (`colors_type=LINEAR`) porque el proyecto Unity es lineal y el shader lo usa directamente. | Contrato rechaza capas extra; sondas: Unity recibe el lineal del sRGB fuente (±4/255). |
| Materiales | Slots con nombres ASCII en el orden del manifiesto; Unity **no** importa ni genera materiales (`materialImportMode=None`): los asigna Unity por índice de submesh, con shaders propios. Sin texturas por defecto. | Submesh por slot y orden verificado; ningún `.mat`/textura bajo `Art/B0`. |
| Nombres | IDs `B0_*` de asset distintos de IDs de gameplay; objetos, mallas (= nombre del objeto), materiales, huesos y clips `^[A-Za-z][A-Za-z0-9_]*$` (sin `.001`, sin no ASCII). Se exportan solo los objetos del manifiesto, sin depender de selección/visibilidad; nunca cámaras ni luces. | Casos rotos; prueba con cámara ajena seleccionada y fixture oculta. |
| Rig/skinning | Una armadura por asset `rigged`; solo huesos deformantes, sin leaf bones; ≤ 4 influencias, pesos normalizados, ningún vértice sin peso; malla con un único Armature y transform local identidad. Unity: `Generic`, avatar propio, sin root motion. | Validador implementado; se prueba con Remedios en B0.3. |
| Clips | Acciones con nombre/rango del manifiesto, escena a 30 fps, bucle explícito; acciones no declaradas = error; in-place (root bloqueado en Unity). | Caso roto «acción fuera del manifiesto»; clips reales en B0.3. |
| Repetibilidad | Validar → exportar a temporal fuera de `Assets` → reimportar en Blender (objetos, materiales, UV, color, triángulos, bounds) → reemplazo atómico (`.tmp` que Unity ignora). Fuente nunca guardada (SHA-256 antes/después). Si el resumen semántico no cambia, no se reescribe nada. | El FBX **no es binariamente reproducible**: cambian la marca de tiempo y los IDs internos FBX (51 bytes); el resumen semántico sí es idéntico. |
| Reimportación | GUID y fileID estables. | Dos reimportaciones forzadas y una reexportación forzada con bytes nuevos: GUID `d3bb2dec…`, fileID `576099132018298866` y `.meta` sin cambios. |

**Orientación: hallazgo medido.** Las pruebas Unity fallaron primero con los ejes por defecto del exportador (−Z/Y): el nodo quedó girado 90° y la malla en Z arriba, porque el FBX ya se declara Y arriba y `bakeAxisConversion` no tiene nada que hornear. Con ejes nativos llegó sin giro pero mirando a −Z. Declarar −Y como frente sin «space transform» rompió la ida y vuelta: el exportador lo detectó en su reimportación y **conservó la salida válida anterior**. Se descartó «Apply Transform» (`bake_space_transform`), que Blender marca como experimental y roto con armaduras/animaciones. Resultado: ejes nativos + frente +Y en Blender, válido también para rigs.

**Pruebas ejecutadas (05/10/2026).**

- `scripts\blender.cmd author -Force` → fuente de la fixture regenerada y validada (`art/blender/b0/fixture/b0_fixture.blend`, 541 631 bytes sin comprimir).
- `scripts\blender.cmd test` → **22/22** (`unity\TestResults\B0\Test\test-report.json`): fuente válida; 14 casos rotos rechazados con su explicación (escala, rotación, pivote, unidades cm, sufijo `.001`, no ASCII, sin UV, color extra, slot cambiado, malla con otro nombre, modificador sin aplicar, acción no declarada, cámara en el manifiesto, factor 100); exportación a ruta con espacios con reimportación; repetición «sin cambios»; reexportación forzada con el mismo resumen; fuente intacta; independencia de selección/visibilidad/objetos ajenos; fallo de contrato y fuente ausente sin tocar la última salida ni dejar temporales.
- `scripts\blender.cmd export` → `B0_Fixture.fbx` (18 060 bytes, 64 triángulos, 40 vértices fuente) + gemelo.
- `scripts\b0.cmd edit` → **8/8** (`unity\TestResults\B0\Benchmark\edit-b0.xml`): importador según contrato; jerarquía sin rotación/escala ni colliders/cámaras/luces/Animator; bounds en metros sin espejo; sondas con orientación y color lineal; aristas duras/suaves; triángulos/UV/slots; sin materiales/texturas generados; GUID/fileID/`.meta` estables. Ejecutado antes y después de `scripts\blender.cmd export -Force` (FBX con bytes distintos): mismos GUID/fileID.

## Sesión 05/10/2026 — B0.3, assets representativos y exportador (Claude Code)

Contenido original de MAMPORRO, derivado de las siluetas y la paleta de los modelos web del propio juego (`src/entities/PlayerView.ts`, `src/entities/enemyModels.ts`); nada de otros juegos ni de packs. Son **representativos del spike**, no arte final. Las fuentes iniciales las crea `scripts\blender.cmd author` (código en `scripts/blender/b0_author.py`); a partir de ahí el `.blend` es la fuente editable.

| Asset | Tipo | Contenido | Triángulos | Vértices | Huesos | Clips (30 fps, en bucle, in-place) | .blend | FBX |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `B0_Pelusa` | rigged | Bola de polvo facetada, 10 mechones, ojos con cejas en V, boca, patitas. Visual 1,14 × 0,94 × 1,03 m (el collider lógico sigue siendo radio 0,5 / alto 0,95). | 268 | 174 | 4 (Root sin pesos, Body, Foot_L, Foot_R) | `Pelusa_Walk` 0–20 (0,667 s): bote, squash/stretch, contoneo, pasos | 573 614 B | 70 604 B |
| `B0_Remedios` | rigged | Vestido con franja, delantal con bolsillo, rebeca con botones, toquilla, permanente gris/lavanda, gafas, coloretes, pendientes, zapatillas con pompón, chancla (derecha) y bolso (izquierda). Alto 1,62 m. | 784 | 494 | 12 (Root, Hips, Spine, Head, brazos ×4, piernas ×4) | `Remedios_Walk` 0–24 (0,8 s), `Remedios_Idle` 0–60 (2 s) | 755 935 B | 265 644 B |
| `B0_ParedModulo` | modular | Muro de sillería 4 × 3 × 0,5 m: zócalo, cuerpo, albardilla y sillares en relieve a matajunta en ambas caras; pivote en la esquina inferior izquierda sobre el eje (x 0…4, y ±0,25 sin relieve, z 0…3). | 348 | 232 | — | — | 535 611 B | 21 900 B |

Un material/slot por asset (`B0_Pelusa`, `B0_Remedios`, `B0_Piedra`); la paleta va en color de vértice. Pesos rígidos por pieza (1 influencia por vértice). Gemelos `.b0.json` de 1–1,5 KB. Los `.blend` tienen ≈ 530 KB fijos de datos de interfaz de Blender aunque la malla sea mínima (sin comprimir).

**Hallazgos durante la pieza.** (1) La reimportación de comprobación medía la Pelusa con la primera pose del clip aplicada (escala de squash 1,08) y rechazaba la exportación: ahora mide en reposo, como la fuente. (2) La prueba Unity de empalme detectó que el muro no se podía repetir: relieve aleatorio en los sillares del borde y dos sillares que se unían en uno de 2 m en la junta; se corrigió en la autoría (relieve fijo en bordes, media junta en filas pares, medios sillares que se completan con el vecino). (3) Las capturas Workbench salían con el color del material: el atributo `Col` no estaba marcado como activo/de render; se marca al crear la fuente (los datos exportados no cambian: «sin cambios»).

**Pruebas ejecutadas (05/10/2026).**

- `scripts\blender.cmd author -Asset <id> -Force` para los tres assets: fuentes válidas al primer intento contra el contrato.
- `scripts\blender.cmd test` → **25/25** (las 22 anteriores + fuente válida de cada asset nuevo).
- `scripts\blender.cmd export` → cuatro assets correctos con reimportación (triángulos, materiales, UV, color, bounds en reposo y tomas `Pelusa_Walk`, `Remedios_Walk`, `Remedios_Idle`); repetición «sin cambios».
- `scripts\b0.cmd edit` → **22/22** (`unity\TestResults\B0\Benchmark\edit-b0.xml`): las 8 de la fixture y 14 nuevas de `B0AssetTests` — ajustes de importación por tipo y jerarquía limpia; bounds en bind pose y sondas de orientación/color (ojo y pie de la Pelusa; nariz, chancla derecha y bolso izquierdo de Remedios; esquinas del muro); rig con 4/12 huesos sin leaf bones, bindposes, 1–4 influencias por vértice, Animator Generic; clips con nombre, duración, bucle, sin root motion y raíz quieta; empalme del muro cada 4 m; GUID/fileID/.meta estables al reimportar.
- `scripts\blender.cmd capture` → vistas frente/¾/derecha/espalda y 6 fotogramas por clip en `unity\TestResults\B0\Capture\<id>\` (local, ignorado). Revisadas: orientación (frente +Y), chancla en la mano derecha y bolso en la izquierda, cejas en V, paso con rodilla y brazos en contrafase, aparejo del muro.

**Limitaciones.** Revisión artística pendiente del autor (silueta, proporciones, legibilidad a 360 p y originalidad). Los clips usan interpolación Bézier sobre 7–9 claves; sin pruebas aún de mezcla/transición en Unity (B0.4). Pesos rígidos: sin deformación suave en codos/rodillas, coherente con el estilo facetado pero a valorar.

## Sesión 05/10/2026 — B0.4, integración aislada (Claude Code)

**Arquitectura.** `lógica/collider/pool → VisualRoot → modelo/render`, solo lectura del núcleo:

- **Separación de la entrega:** los assets B0 viven en la escena aditiva `Assets/Mamporro/QA/B0/B0_QA.unity` (componente `B0VisualLibrary` con malla de la Pelusa, prefabs de Remedios y muro, tres clips y dos materiales QA `B0_Retro`/`B0_RetroDestello` con `Mamporro/RetroWorld`). Solo la incluyen `U3Project.BuildB0/BuildB0Development` mediante `BuildPlayerOptions`; no se añadió a `EditorBuildSettings` y `U3Project.Build` (entrega) no cambia. `scripts\b0.cmd create` regenera escena y materiales.
- **Activación:** `-b0-visual` en la build B0 carga `B0_QA` y añade `B0VisualAdapter`; exige `-u4-save-dir` (si falta, carpeta temporal y salida con código 2), igual que el ensayo.
- **Jugador:** `VisualRoot` con el prefab de Remedios (Animator Generic sin controlador, `applyRootMotion=false`) que copia cada fotograma, después de `U3Game`, el transform ya interpolado del avatar provisional (posición, giro, escala de deslizamiento y parpadeo de invulnerabilidad); los renderers del provisional se apagan. Mezcla andar/inactiva con Playables en modo manual: peso por velocidad lógica, ritmo proporcional a la velocidad, tiempo 0 en pausa/cartas/resultados y reinicio de poses al empezar otra partida.
- **Horda:** `RunRenderer.EnemyVisual` (nulo = dibujo U6 idéntico). `B0StaticHorde` dibuja las Pelusas (tipo 0) con `RenderMeshInstanced` desde los arrays lógicos (posición interpolada, rumbo web → giro Unity), lotes de 1023, sin GameObject ni estado por enemigo; destello de golpe con material aclarado (aproximación del blanco U6, porque el shader multiplica el color de vértice). Telegrafiados, proyectiles y demás enemigos siguen en `RunRenderer`. Es el punto de enchufe de las estrategias de B0.5.
- **Prop modular:** dos módulos de muro seguidos 10 m delante del inicio, apoyados en el terreno, **solo visuales, sin collider** (los colliders no cambian en B0; la jugadora los atraviesa).
- Cambios en código aprobado: solo `U3Game.AvatarRoot` (lectura) y el gancho nulo de `RunRenderer`. Sin cambios de reglas, RNG, colliders, director, guardado ni resultados.

**Pruebas ejecutadas (05/10/2026).**

- `scripts\b0.cmd create` → escena y materiales QA generados; `git status` sin efectos colaterales (ni `EditorBuildSettings` ni escena U3).
- `scripts\b0.cmd edit` → **23/23** (22 anteriores + `QaSceneReferencesSurviveReimport`: las 8 referencias de `B0_QA` resuelven con los mismos GUID/fileID tras reimportar los tres FBX).
- `scripts\b0.cmd play` → **3/3** (`unity\TestResults\B0\Benchmark\play-b0.xml`): `LogicIsIdenticalWithVisualOnAndOff` (900 ticks con recorrido, horda y cartas; huella completa de tiempo, jugador, vida, bajas, oro, XP, nivel, director y cada enemigo — id, tipo, posición, vida — idéntica en dos partidas sin visual y en la tercera con visual, que dibujó Pelusas); `VisualRootFollowsLogicPausesAndResetsCleanly` (VisualRoot en la posición/giro interpolados del jugador con error < 1e-5, anda al moverse, sin root motion ni colliders en jugador ni muro, avatar provisional oculto, instancias = Pelusas vivas tras F3·4, pausa sin avance de lógica ni animación, reinicio con tiempo de animación 0 y sin instancias residuales); `DisablingVisualRestoresTheApprovedRepresentation` (al desactivar vuelve el avatar U6 y las cajas de las Pelusas; al reactivar, el visual).
- `scripts\b0.cmd build` + `scripts\b0.cmd visual` → build QA normal con `B0_QA`; capturas 1920×1080 en `unity\TestResults\B0\Visual\` (local): `b0-juego`, `b0-colliders` (cilindros lógicos del jugador r 0,4/h 1,55 y de cada enemigo como cajas en alambre), `b0-u6-mismo-instante`, `b0-andar-0…5` (frente), `b0-inactiva`, `b0-reinicio`. Revisadas: modelo dentro de su collider, muro modular en el mundo real, Pelusas instanciadas.
- Regresión completa sobre el código B0.4: `scripts\u3.cmd edit` → **428/428** (405 + 23 B0) y `scripts\u3.cmd play` → **42/42** (39 + 3 B0). Después se refactorizó `B0Visuals.cs` (clase base común `B0Horde` y las clases de C/D de B0.5, todavía sin usar en el juego); sobre ese estado exacto, el que se publica, se repitieron `scripts\b0.cmd edit` 23/23, `play` 3/3, `build` y `visual`.

**Limitaciones.** El muro QA no tiene collider (decisión de B0: no tocar colliders). El destello de golpe del visual es una aproximación (material aclarado). La horda solo sustituye a la Pelusa; el resto de enemigos siguen en cajas. `-b0-horde` existe pero en B0.4 solo se usa `static`.

## Sesión 05/10/2026 — B0.5 (en curso), estrategias de horda (Claude Code)

**Implementado (commit intermedio, sin resultados todavía).** Estrategias sobre el gancho `RunRenderer.EnemyVisual`, mismo enemigo (`B0_Pelusa`, 268 triángulos), mismo clip (`Pelusa_Walk`, 20 fotogramas a 30 fps), mismo reloj de presentación y fase fija por `Id` (no RNG): **B** `static` (bind pose instanciada), **C** `poses` (8 poses horneadas con `BakeMesh`, un lote por pose, sin interpolar), **D** `vat` (posiciones por vértice y fotograma en textura `RGBAHalf` 1×20 por vértice, shader `Mamporro/RetroWorldVAT` que interpola fotogramas en el vértice, fase por instancia en `MaterialPropertyBlock`). Horneado en `B0Project.CreateQa` (`scripts0.cmd create`) a `Assets/Mamporro/QA/B0/Generated/` conservando GUID al regenerar. Arnés `-b0-strategy base|static|poses|vat`, `-b0-avatar` (Remedios con rig andando delante de la horda) y `-b0-closeups` (6 primeros planos de la fila 0); el CSV añade `hordeVisualCpuMs` (CPU del visual: matrices + encolado). Lanzador: `scripts0.cmd benchmark -Strategies base,static,poses,vat -Sizes 1080|1440 -Runs 1 -First N [-Avatar]`, cuatro estrategias alternadas dentro de cada carga.

**Pruebas ejecutadas.** `scripts0.cmd create` (horneado; shader VAT compilado sin errores); `scripts0.cmd edit` → **25/25** (+ `B0HordeBakeTests`: 8 poses con la topología, colores y UV de la malla importada; VAT de vértices × 20 fotogramas, fotograma 0 = pose 0 y 10 = pose 4 con error < 2·10⁻³, material VAT instanciado); `scripts0.cmd play` → **3/3**; `scripts0.cmd build`; humo `vat` y `poses` con 300 a 1080p: válidos, primeros planos revisados (caras al frente, fases desfasadas).

**Siguiente paso exacto.** Ejecutar la batería B0.5 (pasadas 1–3 × 1080p/1440p, `-Strategies base,static,poses,vat`), avatar (`base,vat -Avatar`, 1080p, 3 pasadas) y diagnóstico Development (`devbuild` + `devdiag -Strategies base,static,poses,vat`); resumir con `scripts0.cmd summary -Strategies base,static,poses,vat -Source <fuente>` y documentar tabla A/B/C/D.
