# B0 — Spike Blender / pipeline de assets 3D

Objetivos confirmados por el autor el 04/10/2026; plan concretado el 05/10/2026, después de aprobar U6. **SOLO PLANIFICACIÓN AUTORIZADA. IMPLEMENTACIÓN NO AUTORIZADA.** No abrir Blender, crear modelos/FBX/scripts, modificar assets Unity, instalar plugins ni activar LFS todavía. El spike precede a cualquier adopción artística masiva.

## Cómo retomar

- **Base:** migración U0–U6 cerrada; cierre formal publicado en `39602c8aaba3ca163cde39c654b005f23f9e5f2f`. Cierre técnico anterior `c84b62d`; código de entrega `daaa626` y paquete conservados en [PROGRESO_U6](PROGRESO_U6.md).
- **Paso actual:** plan preparado; esperar autorización expresa del autor y resolver las dos propuestas de la última sección. Ningún paso B0 implementado.
- **Terminado:** alcance, dependencias, pasos, criterios y protocolo de evidencia propuestos. No hay modelos ni automatización nuevos, instalación Blender no comprobada.
- **Sin commit deliberadamente:** los siete ajustes Unity protegidos enumerados en PROGRESO_U6; no publicar ni restaurar. Product Name ya es MAMPORRO y no se vuelve a cambiar.
- **Siguiente acción tras autorización:** B0.1, detectar instalación real desde CMD y registrar versión/ruta, sin instalar por inferencia. Si no existe o hay incompatibilidad, informar antes de elegir otra versión o instalar.
- **Continuidad:** una pieza verificable por commit español, pruebas reales, checkpoint aquí, fetch antes de push normal a `claude/zen-pasteur-674ik0`. Sin PR. Parar ante nueva decisión importante y al cierre para revisión artística/manual.

Auditoría inicial futura, desde CMD en la raíz del repositorio:

```cmd
git fetch origin
git status --short --branch --untracked-files=all
git rev-parse HEAD origin/claude/zen-pasteur-674ik0
```

## Responsabilidades

Blender: producción de personajes, enemigos, armas, props y piezas modulares de estructuras; UV, vertex colors, rigging, skinning y animaciones. Unity: gameplay, combate, colliders/física, mundo y generación procedural, pools/rejilla/hordas, ensamblaje de escenarios, shaders/materiales e iluminación/render final, integración y optimización runtime. No reconstruir en Blender lo que convenga mantener procedural en Unity.

Referencia prevista: **Blender 5.2.2 LTS**, designación facilitada por el autor, no instalación comprobada. Al empezar B0 verificar desde CMD la versión y ruta reales de Windows antes de crear scripts; fijar entonces una versión exacta soportada para exportaciones reproducibles. No presuponer rutas ni instalar plugins de terceros por defecto.

## Pipeline a validar

`.blend fuente → exportación automatizada → FBX → Unity`. Los `.blend` son fuentes editables; Unity consume los FBX exportados si el spike confirma el enfoque. La producción no dependerá de la importación directa de `.blend`. No introducir glTF, plugins ni otro formato principal sin necesidad demostrada.

B0 definirá convenciones para escala/unidades, ejes, transforms, pivotes/orígenes, triangulación, normales, UV, vertex colors, rigs, nombres, materiales, clips de animación, rutas de exportación y reimportación estable. Automatización con Blender en background y Python, compatible con CMD; detectar/configurar instalación sin rutas absolutas locales versionadas.

Nombres orientativos **no existentes ni funcionales todavía**: `scripts\blender.cmd verify`, `scripts\blender.cmd export`, `scripts\blender.cmd test`. Solo podrán documentarse como comandos utilizables tras implementarlos y probarlos en B0. Los agentes podrán ayudar mediante scripts a generar, modificar, validar y exportar assets; la revisión visual/artística humana seguirá siendo obligatoria.

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

## Plan ejecutable propuesto y dependencias

Todo lo siguiente es futuro, no una lista de tareas ejecutadas. Cada fila termina con pruebas, evidencia y checkpoint antes del commit/push. Dependencia secuencial salvo el diseño de convenciones, que puede prepararse con la base ya medida; no producir el catálogo completo.

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

**No ejecutada en esta planificación.** Primero buscar instalaciones; la ausencia en PATH no demuestra que no esté instalado. Búsquedas acotadas, sin recorrer ni modificar todo el disco:

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

Carpetas propuestas: `art/blender/b0/` para .blend y fuentes de texturas; `art/blender/b0/manifest.json` para catálogo y parámetros; `scripts/blender/` para Python y configuración versionada sin rutas locales; `unity/Assets/Mamporro/Art/B0/` para FBX, materiales/texturas runtime y sus .meta; `unity/Assets/Mamporro/QA/B0/` para prefabs/escena de comparación. Informes voluminosos en `unity/TestResults/B0/` (ignorado), resumen técnico y huellas en documentación versionada. No crear carpetas ni archivos de implementación ahora.

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
- Propuesta de suelo conocido: conservar 60 FPS con 300 entidades en el equipo de referencia (p95 ≤ 16,67 ms como criterio operativo propuesto), y usar 500/750 para comparar margen, no como cifras de balance ni requisitos comerciales. No basta con estar por debajo de ese suelo: si se pierde mucho margen, se presentan el coste y la ganancia visual, sin adopción masiva automática. Ningún presupuesto extra en ms/% está aprobado todavía; la elección final requiere revisión del informe.

## Pruebas y aceptación final de B0

1. **Exportador:** fixtures de contrato válidos/rotos; repetición; independencia de selección/configuración personal; fuente intacta; ruta con espacios; error de proceso/escritura y preservación de última salida buena. `verify/export/test` solo se anuncian como funcionales cuando existan, pasen y tengan ayuda CMD.
2. **Unity Edit Mode:** dimensiones/ejes/pivotes, normales/UV/colores/slots, rig y clips, referencias/GUID estables, ausencia de colliders generados y configuración de root motion. Comparación determinista del núcleo con visual on/off, mismo RNG y resultados; no regenerar corpus.
3. **Play Mode:** movimiento/pausa/reinicio, pool/reciclado sin poses residuales, daño y avisos conservados, sin duplicar entidades ni materiales. Escena QA y un caso integrado sobre mundo real; guardado aislado con `-u4-save-dir` o override temporal.
4. **Regresión:** al cerrar, `scripts\u3.cmd edit`, `scripts\u3.cmd play`, `scripts\u3.cmd build` y visual pertinente; verificador histórico aislado cuando se toque el punto de comparación determinista. No cambiar src ni referencias. Se parte de los 405 Edit / 39 Play registrados en U6, que son históricos hasta ejecutar de nuevo; anotar total real y nuevos casos. Web no requiere batería completa por editar un FBX si no ha cambiado su código/herramientas.
5. **Rendimiento:** condiciones A–D repetidas con datos reproducibles, diagnóstico separado y sin allocations por entidad/frame introducidas en estado estable; detectar coste inicial y crecimiento tras reinicios. Si falta una métrica, declararla N/D; si no puede compararse la carga, esa condición no valida la estrategia.
6. **Autor:** revisión de los tres assets y del clip, lectura a resolución retro y en horda, originalidad y utilidad del flujo de edición/reexportación. Capturas de fuente y Unity, ejes/collider/VisualRoot, primer/último frame y comparación A–D; ninguna captura está hecha en esta planificación.

B0 puede cerrar como **spike concluido sin adopción** si descubre límites importantes. Declarar **pipeline validado para proponer adopción** exige exportación y reimportación reproducibles, tres casos correctos, lógica/guardado intactos, regresión aprobada, coste explícito aceptable y aprobación visual/manual del autor. Ningún resultado autoriza sustituir el catálogo completo.

Medir tamaños de fuentes y exportados, crecimiento tras varias revisiones y ruido de reexportación antes de proponer Git normal/LFS. No activar LFS en este bloque sin decisión posterior informada. Resumen y tablas versionados; logs/CSV/capturas voluminosos locales con ubicación y huellas, sin datos personales ni telemetría externa.

## Decisiones propuestas al autor — una sola tanda

1. **Contenido del spike:** ¿Pelusa común + Doña Remedios con rig/locomoción + módulo de pared son los tres casos iniciales adecuados? Recomendados por cubrir horda, avatar y estructura sin introducir contenido nuevo. Son pruebas originales representativas, no diseños finales del catálogo.
2. **Criterio de rendimiento:** ¿mantener el objetivo conocido de 60 FPS/300 como suelo (operativamente p95 ≤ 16,67 ms), usar 500/750 para comparar margen y decidir la adopción con el informe de coste/calidad, sin fijar ahora un presupuesto extra arbitrario? Ninguna estrategia se da por elegida antes de medir.

No se vuelven a preguntar pipeline FBX, fuentes fuera de Assets, separación lógica/visual, arte original, ausencia de plugins, preservación de hordas ni Git LFS preventivo: ya están resueltos. Versión exacta/instalación solo se concretará al inspeccionar lo que hay tras autorización. Si aparecen incompatibilidades, cambio de formato/paquetes o necesidad de tocar reglas/colliders, son decisiones nuevas y requieren consulta.

## Registro de planificación — 05/10/2026, Codex

Cierre formal U6 `39602c8aaba3ca163cde39c654b005f23f9e5f2f` publicado y remoto verificado antes de este plan. Lectura de BLENDER_B0, hoja de ruta, decisiones, arquitectura de render y automatización existentes. Esta sesión solo modifica documentación: pasos/dependencias/criterios, comandos futuros CMD, contrato propuesto, comparación y decisiones; revisión de enlaces y diff, siete excluidos con huellas conservadas. No se abrió Blender/Unity ni se ejecutaron pruebas de assets, suites, build o benchmark. No se crearon scripts, modelos, FBX ni carpetas de implementación.

Commit del plan: «Planifica el spike Blender tras cerrar la migración»; localizarlo con `git log --all --grep="Planifica el spike Blender tras cerrar la migración"`. Publicar con fetch/push normal y verificar remoto. Siguiente paso: esperar respuesta del autor; implementación B0 no autorizada.
