# P0 — movilidad y verticalidad antes de ampliar el mundo

Estado: **PLAN PROPUESTO, NO IMPLEMENTADO NI AUTORIZADO PARA PROGRAMAR**. Preparación del 05/10/2026 (Codex). No mezcla P1 artístico ni P2 contenido. La clasificación histórica de balance en P0 no lo incluye en este primer prototipo.

## Cómo retomar

- Cierre formal B0 publicado: `0b0b68c31dda803cf9cf566b3c96f369ed893ce3`, «Aprueba B0 y fija el pipeline 3D», sobre cierre técnico `ab9e619`. Pipeline aceptado para proponer adopción; no hubo revisión manual de la build por el autor. D/VAT candidata, C fallback; prototipos técnicos, no arte final.
- U0–U6 cerrados y aprobados. Entrega `unity/Builds/Windows/MAMPORRO.exe` intacta; SHA-256 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`.
- Trabajo realizado: auditoría de código/documentación y plan. Ningún código, asset, script, escena o build P0 creado. No hay WIP runtime.
- Siete ajustes Unity protegidos intactos y excluidos, según BLENDER_B0/PROGRESO_U6. No reset/clean/stash ni publicación accidental.
- **Siguiente paso exacto:** recibir respuesta a las cuatro decisiones de abajo y autorización del primer bloque P0-A. Solo entonces crear el circuito QA y las pruebas del contrato de superficies. La aprobación de este plan no debe confundirse con resultados de pruebas.
- Publicación de este plan: commit «Planifica el prototipo de movilidad y verticalidad», localizable con `git log --oneline -- docs/PROGRESO_P0.md`; fetch/push normal y comprobar remoto. Un paso verificable por commit durante implementación futura.

Auditoría CMD antes de continuar:

```cmd
git fetch origin
git status --short --branch --untracked-files=all
git rev-parse HEAD origin/claude/zen-pasteur-674ik0
```

## Lo que ya está decidido

Escalada libre por paredes, sin resistencia/stamina ni límite artificial de trepada breve; mayor agilidad y verticalidad; mundo/estructuras considerablemente mayores, terreno geométrico/rectangular, mesetas y rampas. Windows escritorio, identidad propia, Megabonk solo referencia general. No copiar contenido. Primero movilidad, después escala del mundo. No modificar web/corpus para ocultar diferencias.

## Diagnóstico del código actual

- `Core/World/PlayerPhysics.cs`: lógica pura, `PlayerIntent`, `PlayerBody`, `IPhysicsWorld` y `StepInCrowd`; aceleración, control aéreo, coyote/buffer, salto variable, deslizamiento y protección contra atasco en pendientes. No hay estado de escalada ni consulta de pared/borde. Reutilizar esta física y su contrato; no crear otro controlador Unity que compita por la posición.
- `Core/Catalog.cs`: velocidad base 9,5 m/s, step del jugador 0,45 m, pendiente máxima 48°. Se conservan como referencia y como control inicial del ensayo; no se aumenta todo para simular agilidad.
- `WorldCollision.cs`/`Colliders.cs`: terreno más superficies `Standable` por `maxY`, cilindros/cajas orientadas y resolución horizontal con rango vertical; límite squircle. No es un resolvedor general de cápsula contra techos y bordes. La escalada necesita consultas explícitas de contacto, volumen libre y trayecto barrido, no deducir colisiones de un modelo visual.
- `WorldRun.Step` llama directamente a `PlayerPhysics.StepInCrowd` y reutiliza `CombatRun`; el prototipo debe añadir un punto de extensión optativo y probado con la ruta U3 como predeterminada. Mantener coordenadas web en el núcleo y conversión en presentación.
- `U3Game`: Espacio salta; Shift/C deslizan (Ctrl opcional), E interactúa. La cámara resuelve el brazo contra el heightfield, no contra edificios. Mantener pausa, pérdida de foco, cartas y reinicio. El botón derecho no tiene una acción de gameplay en esta entrada actual.
- `Enemies.Step`: persecución XZ, separación local/grid y desvío por atasco, obstáculos con step 0,6 m y altura `world.Height(...,Y+0.6)`. No hay navegación por plantas ni búsqueda de rutas. El contacto comprueba diferencia vertical; no basta poner una rampa para que sepan encontrarla. Disparos/áreas tienen que probarse explícitamente a distintas alturas, sin suponer que resuelven por sí solos una zona inaccesible.
- `WorldData.Generate` en `WorldCollision.cs` compone `Heightfield`, `Sites`, `Props`, interactuables, vegetación y grid; esta ruta U3 seguirá siendo referencia. Un heightfield no representa varias plantas/voladizos: estructuras y superficies transitables deberán complementar el terreno.

## Primer bloque propuesto: P0-A, circuito aislado de movilidad vertical

Escena QA propia, geometría sencilla generada en Unity, sin rehacer el mundo ni usar obligatoriamente modelos B0. Circuito compacto: llano de control, rampa y meseta, pared/tejado, esquina interior/exterior, borde estrecho, techo que impide subir, hueco de caída y una ruta alternativa por suelo. Dimensiones de ensayo configurables y registradas, no tamaño definitivo del mundo. Sin recompensas/meta; guardado temporal aislado y build separada de la entrega U6.

| Paso/commit | Trabajo propuesto | Aceptación y dependencia |
| --- | --- | --- |
| A1 — circuito y consultas | Adaptador optativo de superficies QA, normales/ID/tipo escalable, volumen libre y barridos para evitar atravesar paredes/techos; recorrido U3 como control. | Edit: contactos, esquinas, huecos, borde, techo y límites; sin cambios de U3 con capacidad desactivada. Depende de aprobar controles/superficies. |
| A2 — escalada | Estados explícitos suelo/aire/escalando/salida de borde; entrada, descenso/lateral, separación y salto desde pared. Reutilizar PlayerBody/física, sin root motion ni Rigidbody controlador paralelo. | Trayectorias deterministas con entrada grabada; entrada/salida voluntaria, sin bucles de reenganche, sin stamina. No tocar estadísticas de combate. |
| A3 — transiciones y cámara | Borde→tejado solo con volumen y recorrido libres; vuelta al aire, salto/deslizamiento, cámara contra sólidos QA si se aprueba. | Play: no penetraciones, temblores persistentes, pérdida del jugador ni estados retenidos tras pausa/foco/reinicio. Depende de A2 y decisión de cámara. |
| A4 — convivencia con combate | Enemigos controlados reutilizando CombatRun/grid/pools, sin cambiar director ni curvas; rampa accesible, tejado accesible e inaccesible, proyectiles y contacto a distintas alturas. | Identificar explícitamente dónde el steering actual no alcanza; no infligir contacto a través de forjados ni fingir amenaza en lugares inaccesibles. Sin navegación masiva nueva dentro de A4. |
| A5 — medición y cierre | Regresión, build QA, recorridos grabados/capturas, comparación de ruta y coste. Guía para prueba manual del autor. | Prototipo revisable, limitaciones concretas, U3/entrega/guardados intactos. Detenerse para aceptación antes de P0-B. |

### Propuesta de control y comportamiento (pendiente de aprobación)

- **Mantener botón derecho** para agarrarse al tocar una pared escalable próxima; no enganchar al pasar cerca sin intención. W/S sube/baja y A/D desplaza por la pared, independientes de la inclinación de cámara. Soltar baja al estado aéreo; Espacio salta separándose de la pared. No usar E para evitar conflicto con interactuables.
- Después de salto/separación, exigir soltar y volver a pulsar agarre para evitar reenganche involuntario. Shift/C suelta pared; deslizamiento solo al tocar suelo con la regla existente. Acercarse deslizando no cambia a escalada salvo intención de agarre. Conservar salto variable/coyote fuera de la escalada.
- Superficies propuestas: paredes sólidas de estructuras y caras de acantilado, definidas en datos de colisión; no follaje, interactuables, enemigos, límites artificiales ni techos invertidos. No imponer material especial ni lista arbitraria de edificios. Esquinas: continuar solo si contacto y barrido son válidos, sin enganchar por detrás.
- Borde→tejado automático al seguir ascendiendo si cabe todo el cuerpo y el recorrido es libre; no teletransportar sobre un techo bloqueado. Desde tejado, agarrar al descender con el mismo botón cuando se detecte pared próxima; sin arrastre automático al vacío.
- **Punto inicial de ensayo:** 4,5 m/s vertical y 3 m/s lateral, diagonal normalizada. Parámetros QA separados, no balance definitivo; inicialmente independientes de mejoras de velocidad para aislar la prueba. Mantener 9,5 m/s de suelo como control. Ajustar sensación con recorrido y revisión, no cifras de enemigos/oro.
- Sin desgaste por tiempo agarrado, caída dañina nueva ni invulnerabilidad por escalar. Contacto/ataques reales siguen funcionando. No añadir empuje nuevo al recibir daño: conservar las reglas existentes y verificar su interacción con el agarre.
- Cámara propuesta: conservar ratón/FOV/opciones y añadir retracción por paredes/techos SOLO en QA P0, recuperación suave y posición libre cerca del pivote. No autoorientación/cinemática ni cámara nueva en U3. Comparar mismo trayecto antes/después.
- Antiatasco: detectar ausencia de avance con intención, permitir separación/salto, validar todo el cuerpo y su trayectoria en bordes, no intentar mantle indefinidamente. Sin snap a través de obstáculos. QA con reinicio manual al último punto seguro y contador de incidentes; si se cae fuera del volumen de ensayo, recuperación señalada solo QA, sin escribir progreso ni dar premios. Un rescate no cuenta como ruta completada.

## Enemigos y alturas: siguiente bloque P0-B, no ocultarlo dentro de P0-A

Recomendación: enemigos comunes llegan por rutas transitables (rampas/pasos), con búsqueda de ruta compartida/centralizada por sectores cuando el desvío local resulte insuficiente. No Animator/Update/Rigidbody por enemigo, ni escalar paredes o teletransportarse automáticamente. Mantener pools/grid; no adoptar NavMeshAgent individual por inercia.

En P0-A, una azotea sin acceso puede dejar al jugador fuera del alcance del melee: es una limitación QA visible, no un estado apto para publicar como gameplay definitivo. Probar enemigos a distancia existentes, alturas de impactos y bloqueos; no prometer que solucionan todo ni añadir tipos, daño, frecuencia o spawns para disimularlo. Registrar tiempo sin amenaza y causas.

Antes de integrar al juego normal, P0-B debe garantizar rutas terrestres a las zonas transitables relevantes y comprobar una amenaza efectiva en lugares de permanencia. Si un lugar sin acceso sigue siendo seguro indefinidamente, rediseñar conexión/geometría o volver al autor con una propuesta específica; no castigar con stamina, daño invisible, teleport o escalada de enemigos no aprobada. El autor debe decidir esta política ahora; el algoritmo concreto se concretará con evidencia de A4.

## Después: P0-C, muestra procedural; P0-D, escala del mundo

Solo tras validar movilidad y rutas: variante explícita del generador con mesetas amplias/rectangulares y rampas conectadas, estructuras/tejados y consultas de soporte por altura. Revisar `Heightfield`, `Sites`/aplanado, `Props`/colliders, ubicaciones de interactuables, `WorldSpawns`, minimapa, grid y culling. Verificar conectividad, límites, semillas repetibles y que no se generen objetivos inaccesibles. Conservar generador U3 y corpus; crear pruebas nuevas para la variante, nunca regenerar expected históricos.

Ampliar tamaño/densidad por medición de tiempos entre destinos y presupuesto de memoria/render, después; sin fijar dimensiones definitivas, cantidades, curvas, vida u oro ahora. Blender puede aportar módulos; terreno/procedural, colliders y gameplay siguen en Unity. P1 arte/VAT y P2 contenido son bloques distintos. Balance pendiente aparte: menos enemigos iniciales más resistentes, crecimiento progresivo y mucho más oro de partida, no Calderilla.

## Pruebas y evidencia que deberá producir P0-A

- Edit: suelo/aire sin escalada conserva resultados U3; detección de caras, esquinas y techos; transición válida/rechazada, salto/separación, límite del mundo; repetición del mismo input y simulación a ticks fijos; barridos a velocidad alta sin atravesar sólidos.
- Play: controles y cámara en circuito, pausa/cartas/foco, muerte/reinicio, liberación de entradas; jugador y enemigos sobre rampas/tejados, contacto solo con alcance válido, disparos y efectos; ningún residuo ni escritura en progreso real.
- Regresión: `scripts\u3.cmd edit` y `scripts\u3.cmd play` después de integración. Conservar y verificar referencias congeladas mediante el procedimiento histórico ya existente, sin regenerar. Comprobar que `src/` no cambia.
- Build normal QA Windows Mono en carpeta independiente y diagnóstico Development separado. Preparar lanzador P0 y documentarlo SOLO cuando exista y esté probado; no ejecutar `scripts\u3.cmd build` para sobrescribir inadvertidamente la entrega U6. Comparar SHA antes/después de U6 y siete ajustes protegidos.
- CSV local: duración del recorrido, tiempo de ascenso/descenso, éxito de salida a borde, entradas de escalada involuntarias, atascos/recuperaciones, caídas, tiempo sin amenaza y causa, distancia real recorrida. Mismo input/seed/tick en control y variante donde sean comparables. La sensación de agilidad requiere prueba manual, no solo cronómetro.
- Frame time p50/p95/p99/máximo con circuito vacío y 300/500/750 enemigos controlados para detectar regresión por consultas/cámara; CPU/memoria y GC solo cuando medibles. Normal y Development separados, mismo equipo/resolución/interna/recorrido/warmup/duración. El suelo B0 no se convierte automáticamente en umbral de gameplay P0; comparar deltas y reportar limitaciones, GPU N/D si no fiable.
- Capturas: esquema del circuito, collider/volumen y VisualRoot superpuestos, antes/durante/después del borde, techo bloqueado, esquina, cámara en espacio estrecho y enemigos desde tejado. Clip corto de ruta completa y fallo recuperable; si no hay captura fiable de vídeo, secuencia de imágenes, sin fingir validación jugable.
- Aceptación A: ninguna penetración/atasco irrecuperable ni agarre involuntario en casos fijados; transiciones completas, pausa/reset limpios, equivalencia U3 preservada y medidas registradas. Limitaciones de horda explícitas; aprobación manual del circuito antes de ampliar mundo. No se declara P0 completo por cerrar A.

## Decisiones importantes del autor — una sola tanda

1. **Control:** ¿mantener botón derecho para agarrarse, WASD sobre pared, Espacio para separarse saltando y soltar para caer? Recomendado por ser explícito y no interferir con E/salto/deslizamiento existentes. Alternativa: agarre automático al saltar hacia pared, con mayor riesgo de enganches involuntarios.
2. **Superficies y transición:** ¿paredes sólidas de estructuras/acantilados, sin techos/follaje/interactuables, con salida automática al tejado al seguir ascendiendo si cabe el cuerpo? Incluir en el ensayo las velocidades iniciales propuestas (4,5 vertical / 3 lateral), ajustables por sensación; no son cifras de balance final.
3. **Cámara:** ¿autorizar en este prototipo la colisión del brazo contra paredes y techos QA, preservando la cámara U3 de referencia? Es una ampliación concreta necesaria para evaluar espacios verticales, aún no aprobada como cámara general.
4. **Horda:** ¿enemigos comunes por rampas/rutas transitables, sin escalada automática ni teleport, y no integrar alturas en producción hasta demostrar acceso/amenaza real? En A se documentarán refugios inaccesibles; resolverlos mediante rutas/geometría en B antes de ampliar el mundo.

No hay otras decisiones bloqueantes detectadas. Tras respuesta, registrar las resoluciones sin volver a preguntar escalada libre, stamina, plataforma, identidad o tamaño definitivo.

## Registro de preparación — 05/10/2026

Agente: Codex. Solo lectura de código y documentación; no se ejecutaron suites/build/benchmark nuevos. Revisadas evidencias históricas B0 y SHA de entrega/excluidos en el cierre formal. Este commit contiene exclusivamente planificación y enlaces de continuidad. Siguiente paso: esperar respuesta a las cuatro decisiones y autorización P0-A; no iniciar P1/P2, balance, mundo ampliado, Steam, mando ni telemetría.
