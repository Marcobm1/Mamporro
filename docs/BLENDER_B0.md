# B0 — Spike Blender / pipeline de assets 3D

Objetivos futuros confirmados por el autor el 04/10/2026. **Solo documentación: no implementar, instalar, automatizar ni integrar Blender durante U6.** B0 se propondrá como bloque aislado después del cierre y la aprobación manual de U6, antes de comprometer el rediseño artístico completo.

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
