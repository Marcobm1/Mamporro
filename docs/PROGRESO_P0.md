# P0 — movilidad y verticalidad antes de ampliar el mundo

Estado: **P0-A1–A5 base técnica validada manualmente; P0-A6 corregido y verificado, pendiente de prueba manual** (11/10/2026, Codex; refinado por Claude Code el mismo día en `dc4739c`). **Actualización 11/10 noche:** el autor probó A6 y aprueba todo salvo la velocidad; escalada a velocidad efectiva de carrera en `373620d`, pendiente de su prueba. La movilidad no se declara aceptada hasta la nueva prueba del autor. P0-B/C/D NO autorizados. No mezcla P1 artístico ni P2 contenido. La clasificación histórica de balance en P0 no lo incluye en este prototipo.

## Cómo retomar

- **Punto vigente 11/10 noche (Claude Code):** A6 probado manualmente y aprobado salvo velocidad. Nueva decisión confirmada: escalar a la velocidad efectiva de carrera, mejoras incluidas. Implementado en `373620d`; builds Normal/Development de ese commit; Edit 583/583, Play 51/51. Detalle en «Velocidad de escalada = velocidad efectiva» al final. **Siguiente paso: el autor prueba la velocidad con `scripts\p0.cmd jugar`; detenerse. No iniciar P0-B/C/D.**

- **Punto vigente 11/10 tarde (Claude Code):** builds Normal/Development de `dc4739c2517f206496876527afeac8b33f2b79b7`: A/D siguen la cámara también de espaldas a la pared y barrido exhaustivo de caras. Edit 555/555, Play 50/50, evidencia válida `unity/TestResults/P0/Evidence/20261011T193637`. Detalle en «Refinamiento A6 — Claude Code» al final. **Siguiente paso: prueba manual del autor con `scripts\p0.cmd jugar`; detenerse. No iniciar P0-B/C/D.**

- **Punto vigente 11/10:** correcciones A/D y terreno conservadas y verificadas; protección de pared compuesta añadida en `38d29a9ced593bc94e3a55638363b70978db822c`. Edit 542/542 y Play 49/49; builds Normal/Development y evidencia final de 41 PNG/15 CSV correspondientes a ese código. Cierre completo, métricas, integridad y publicación al final de este documento. **Siguiente paso: el autor prueba `scripts\p0.cmd jugar`; detenerse y esperar su confirmación de A/D, paredes y salida de fosos. No iniciar P0-B/C/D.** Los puntos del 07/10 y 08/10 siguientes son historial de reanudación, no tareas pendientes actuales.

- **11/10, reanudación real de A6 (Codex):** HEAD recibido `452c956f2c72dd8c9cb15bed5a0000305b6aefbb`, remoto tras fetch `603a225dbf4ecc89e5359528bdf54d24c6dbd49d`, ahead/behind 3/0, índice vacío. Conservados los tres commits locales `53658e4`, `bb726db`, `452c956`, nueve documentos WIP y siete ajustes protegidos. Builds Normal/Development ya corresponden a `452c956`; evidencia de 41 PNG/15 CSV y benchmark Normal `20261008T181133` existentes. Falta diagnóstico Development de esa corrección y cierre/publicación. Integridad al retomar en `unity/TestResults/P0/A6Correccion/integrity-resume-before.json`: U6 y 7/7 huellas iguales al checkpoint. Auditoría adicional: la protección tras salto se debe extender también a piezas coplanares de colliders compuestos; regresión en curso. No iniciar P0-B/C/D.

- **08/10, corrección A6 tras feedback manual (Codex):** auditado HEAD/remoto `603a225dbf4ecc89e5359528bdf54d24c6dbd49d`, 0/0. A6.1 `0698f25` y A6.2 `603a225` ya publicados; A6.3 dejó WIP en P0Capture y nueve documentos, conservado. XML históricos 500 Edit/47 Play, 17 PNG y ocho informes de cargas válidos comprobados. U6 y siete huellas coinciden. El autor identifica los muros omitidos como meseta/rampa: Heightfield participa en colisión pero no en las consultas de cajas de escalada. A/D invertido por tangente incompatible con la reflexión Z de presentación; los tests anteriores solo verificaban magnitud absoluta.
- Corrección A/D en commit local `53658e4`, Edit 506/506 ejecutado. Adaptador optativo de triángulos del terreno completado: verificación final de código `scripts\u3.cmd edit` **538/538**, `scripts\u3.cmd play` **49/49**, 08/10. Copias XML/log en `unity/TestResults/P0/A6Correccion`. Pasadas intermedias 532/534 Edit y 49 Play también correctas. Referencias U0/U2/U3/U4 verificadas con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`, instantánea `qa-results/reference-298452535d744484b9401c1a26e0d674`. En curso: evidencia/build/medidas nuevas; el arnés conserva el WIP anterior y añade A/D, meseta y cuatro caras de foso. No interpretar este punto intermedio como cierre ni aprobación manual. No iniciar P0-B.

- **07/10, A6 activo:** HEAD/remoto iniciales `ccd45c0c01e127e7d96f1fa87b52174133ae416c`, 0/0 tras fetch. WIP recibido en VerticalMotion, VerticalCircuit, sus tests, VerticalSceneTests, VerticalQa y P0Capture: conservado y revisado; no estaba registrado en el cierre A5.
- U6 y las siete huellas protegidas coinciden exactamente con la tabla de integridad inferior antes de editar.
- Paso pendiente al retomar el 07/10 (resuelto el 11/10): completar/verificar A6 (automática, reenganche por superficie, foso, integración/evidencia/medidas/build); detenerse para prueba manual del autor. Las instrucciones de agarre con botón del historial A1–A5 quedan sustituidas por A6.


- Cierre formal B0 publicado: `0b0b68c31dda803cf9cf566b3c96f369ed893ce3`, «Aprueba B0 y fija el pipeline 3D», sobre cierre técnico `ab9e619`. Pipeline aceptado para proponer adopción; no hubo revisión manual de la build por el autor. D/VAT candidata, C fallback; prototipos técnicos, no arte final.
- U0–U6 cerrados y aprobados. Entrega `unity/Builds/Windows/MAMPORRO.exe` intacta; SHA-256 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`.
- Trabajo publicado: A1 `508aa0f849eb536068359a90a6fc883a117d5937` y A2 `303b01c2a09a2d6bccb3f742f3f20ad5d8c97fe3`. Al retomar el 06/10 había WIP runtime A3 y lanzadores/build P0 sin publicar; conservado y revisado.
- Siete ajustes Unity protegidos intactos y excluidos, según BLENDER_B0/PROGRESO_U6. No reset/clean/stash ni publicación accidental.
- **Siguiente paso exacto:** el autor prueba `scripts\p0.cmd jugar`; esperar su revisión manual y autorización posterior. A3 `beb3209`, A4 `42509c2` y A5 `2cbb299525990e3af809ee8fd4cf46fcd6063b67` publicados. 467 Edit/46 Play, builds/lanzador/evidencias y ocho medidas válidas. Informe completo al final de este archivo. No volver a preguntar las cuatro decisiones aprobadas ni iniciar P0-B/C/D.
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

### Propuesta original de control y comportamiento (aprobada en el registro inferior)

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

## Propuestas del plan inicial (resueltas por la autorización inferior)

1. **Control:** ¿mantener botón derecho para agarrarse, WASD sobre pared, Espacio para separarse saltando y soltar para caer? Recomendado por ser explícito y no interferir con E/salto/deslizamiento existentes. Alternativa: agarre automático al saltar hacia pared, con mayor riesgo de enganches involuntarios.
2. **Superficies y transición:** ¿paredes sólidas de estructuras/acantilados, sin techos/follaje/interactuables, con salida automática al tejado al seguir ascendiendo si cabe el cuerpo? Incluir en el ensayo las velocidades iniciales propuestas (4,5 vertical / 3 lateral), ajustables por sensación; no son cifras de balance final.
3. **Cámara:** ¿autorizar en este prototipo la colisión del brazo contra paredes y techos QA, preservando la cámara U3 de referencia? Es una ampliación concreta necesaria para evaluar espacios verticales, aún no aprobada como cámara general.
4. **Horda:** ¿enemigos comunes por rampas/rutas transitables, sin escalada automática ni teleport, y no integrar alturas en producción hasta demostrar acceso/amenaza real? En A se documentarán refugios inaccesibles; resolverlos mediante rutas/geometría en B antes de ampliar el mundo.

No hay otras decisiones bloqueantes detectadas. Tras respuesta, registrar las resoluciones sin volver a preguntar escalada libre, stamina, plataforma, identidad o tamaño definitivo.

## Registro de preparación — 05/10/2026

Agente: Codex. Solo lectura de código y documentación; no se ejecutaron suites/build/benchmark nuevos. Revisadas evidencias históricas B0 y SHA de entrega/excluidos en el cierre formal. Este commit contiene exclusivamente planificación y enlaces de continuidad. Siguiente paso: esperar respuesta a las cuatro decisiones y autorización P0-A; no iniciar P1/P2, balance, mundo ampliado, Steam, mando ni telemetría.

## Autorización P0-A — 05/10/2026

El autor autoriza A1–A5 consecutivos y confirma las cuatro decisiones del plan: agarre mantenido con botón derecho, WASD independiente de cámara, Espacio separa, soltar/Shift/C suelta, rearmado tras soltar antes de reenganchar. Sin stamina ni duración máxima. E conserva su función.

Estructuras y acantilados sólidos escalables; follaje, interactuables, enemigos, límites y caras inferiores de techos no. La superficie superior/tejado sí puede alcanzarse por borde con soporte, cuerpo y barrido completos libres, sin teleport ni adaptar colliders. Velocidades parametrizadas de ensayo 4,5 vertical / 3 lateral, diagonal normalizada; suelo 9,5 como control. Cámara QA contra sólidos autorizada, sin reemplazar la U3.

Horda existente sin escalada, teleport, NavMeshAgent ni componentes individuales. Refugios inaccesibles visibles y medidos como limitación QA: se resolverán en P0-B, que NO entra aquí. Sin cambios de balance, director, spawns, oro o progreso. Build QA separada, entrega U6 y siete ajustes protegidos intactos. Detenerse al cerrar técnicamente A5 para prueba manual del autor; no iniciar B/C/D, P1/P2 ni adopción VAT.

## Sesión 05/10/2026 — A1, consultas y datos del circuito (Codex)

Base `e49fb9f`, local/remoto iguales y siete ajustes protegidos. Implementados `Core/World/VerticalQueries.cs`, `VerticalCircuit.cs` y 15 casos NUnit en `Tests/Core/VerticalQueriesTests.cs`. Datos QA fijos de rampa/meseta, tejado, techo bloqueante, esquinas, borde estrecho y tipos no escalables. No se modifica el generador, PlayerPhysics ni WorldCollision U3. La escena visible y su lanzador se conectarán con A2/A3; esta pieza valida el circuito lógico, no declara todavía una build jugable.

Barrido continuo contra cajas (sin muestreo que atraviese sólidos finos); normal/ID/tipo, exclusiones, soporte de huella completa, validación de dos tramos de salida y espacio corporal. Envolvente de consulta conservadora cuadrada alrededor del cilindro: puede rechazar un hueco muy justo en diagonal; no cambia colliders ni agranda el cuerpo lógico. Debe mostrarse en QA y revisarse en la integración, sin ocultar rechazos.

Pruebas nuevas realmente ejecutadas: `scripts\u3.cmd edit`, 05/10/2026, primera pasada 444/444 y segunda final **445/445**, `unity/TestResults/U3/edit.xml` y `edit.log`; incluye las 430 regresiones anteriores y las 15 nuevas. El primer intento restringido no llegó a ejecutar Unity por licencia/entorno; la ejecución fuera del entorno restringido terminó correctamente. No Play/build/visual todavía porque no hay integración runtime. Sin cambios en `src/` ni referencias. Entrega U6 no regenerada.

Commit de esta pieza: «Añade las consultas y el circuito lógico de movilidad vertical». Fetch/push normal; siguiente pieza A2: estados y movimiento sobre PlayerBody, después integración visible A3. No iniciar P0-B. Los siete ajustes locales siguen excluidos; no hay otros cambios heredados.

## Sesión 05/10/2026 — A2, estados de movimiento (Codex)

A1 publicado en `508aa0f849eb536068359a90a6fc883a117d5937`. Añadido `VerticalMotion.cs`: extensión optativa sobre un único PlayerBody con estados suelo/aire/escalada/borde, controles e intención separados, velocidades parametrizadas, rearmado tras soltar, barridos, transición recorrida a velocidad finita, timeout y contadores. No cambia PlayerPhysics U3. Ocho pruebas nuevas cubren ausencia de agarre pasivo, velocidad/diagonal, espera de 120 s sin límite, salto/rearme, suspensión/reset, salida de borde en varios ticks, techo bloqueante y repetición determinista.

`scripts\u3.cmd edit` ejecutado el 05/10/2026: **453/453**, `unity/TestResults/U3/edit.xml`/`edit.log`. Aún no hay entrada de ratón/cámara QA ni escena visible: corresponde a A3. Commit «Añade los estados de escalada y salida de borde». Siguiente: conectar optativamente circuito/movimiento al runtime existente, sin segunda simulación de combate, e integrar cámara QA. Play/build/medidas pendientes. Siete ajustes excluidos.

## Sesión 06/10/2026 — A3, integración y revisión del WIP (Codex)

Inicio real tras fetch: HEAD/remoto `303b01c2a09a2d6bccb3f742f3f20ad5d8c97fe3`, 0/0. WIP recibido y conservado: VerticalCircuit/Data, WorldRun/PhysicsStep, U3Game, VerticalQa, VerticalSceneTests, P0Project y scripts/p0. Los siete protegidos coinciden con las huellas históricas B0; entrega U6 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`.

Integración optativa `-p0-qa`: circuito visible sobre U3, misma sesión/PlayerBody/combate, director desactivado solo QA; exige progreso aislado y omite liquidación. Ratón derecho y WASD de pared, salto/soltar/rearme aprobados. Cámara QA con barrido de brazo y pivote contra sólidos, retracción inmediata y recuperación exponencial (4/s); U3 intacta con opción desactivada. Foso de 6 m añadido al terreno, rodeable por suelo. F4 carga controlada, F5 control U3/P0. Las esquinas bloqueadas detienen el avance y permiten separación; no hay giro automático alrededor de una esquina sin contacto válido.

Pruebas reales: primera `scripts\u3.cmd play` 43/44 (la prueba heredada no soltaba agarre después de pausa); corregida la secuencia de entrada, segunda **45/45**, incluidos circuito optativo, cámara/pivote, borde, foco, muerte/reinicio y guardado personal intacto. `scripts\u3.cmd edit` **453/453**, después dos pruebas nuevas detectaron atasco real en escalón y salida de tejado (**453/455**). Corregido exclusivamente el barrido P0: escalón con elevación+avance libres y conservación de componente tangencial al tocar canto. Última pasada **455/455**. XML/log en `unity/TestResults/U3`; la regresión Play final se repetirá después de A4/A5.

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coinciden; instantánea conservada en `qa-results/reference-367eb89e22634e9a9f7068d7d36abf27`. Sin regenerar referencias ni editar src. No se ha creado ni ejecutado todavía una build P0. Los lanzadores heredados y P0Project permanecen WIP fuera de este commit hasta probarlos en A5. A4 pendiente: caracterizar steering XZ, filtros de altura y proyectiles sin modificar reglas de combate.

## Sesión 06/10/2026 — A4, combate y límites medidos (Codex)

Nueve casos nuevos `VerticalCombatTests`, sin cambios en reglas, estadísticas, director ni pools. Apariciones manuales F4 evitan cuerpos dentro de sólidos y pendientes no transitables; no se añade navegación ni escalada enemiga. `scripts\u3.cmd edit` **464/464**, `scripts\u3.cmd play` **45/45**, XML/log U3. La primera pasada Edit dio 461/464: las sondas de armas no esperaban su retardo inicial de 0,3 s y una hipótesis sobre `Height` infinito era incorrecta; las sondas nuevas se corrigieron conforme al código real, sin modificar esperados históricos ni combate.

Resultados (tick 1/60, semilla P0QA, geometría fija):

- Igual altura: contacto causa daño conservando valores originales.
- Rampa directa oeste → cubierta de meseta: enemigo alcanza Y=4,25 y mata al jugador inmóvil a los 14,45 s. Esta ruta directa funciona; no demuestra búsqueda de rutas alternativas.
- Tejado oriental a Y=6 sin acceso: **30,000 s sin daño de melee**, enemigo permanece en suelo. Es refugio QA inaccesible, no aceptable como solución definitiva de amenaza.
- Barra no daña con 6 m de diferencia. Naftalina inflige 6 y Jersey 13 en la sonda de 0,4 s: daño XZ sin oclusión/filtro vertical correspondiente. Son defectos heredados, no amenaza válida ni justificación para ampliar el mundo.
- Proyectil amistoso puede impactar un enemigo 6 m debajo (XZ). Proyectil hostil sigue `heightfield + 1`, atraviesa el sólido y **no** alcanza el tejado de 6 m; no se adapta balísticamente a su altura. No se cambian trayectorias en A4.
- Charco de fregona conserva altura y no daña al enemigo bajo el tejado.

Pendiente de P0-B (no autorizado): rutas compartidas/geometría y política de amenaza; los filtros/oclusión de ataques requieren propuesta explícita, no parche de daño, oro, spawns ni teleport. A5 medirá coste y recorridos del circuito, no resolverá estos defectos.

## A5 — instrumentación, builds y lanzador verificados, 06/10/2026 (Codex)

A4 publicado en `42509c28b7994c820bf2d4c98339e7e3fafe35ad`. Añadidos P0Capture (CSV de ruta/fotogramas, 10+30 s por carga, capturas con validación de píxeles), P0Project con define exclusivo `MAMPORRO_P0_QA`, scripts/p0.cmd/ps1, métricas y rescate F7 que invalida sin liquidación. La build QA entra siempre en P0; el código de arranque sin argumentos usa `%TEMP%/MamporroP0QA/ManualSave` (esa modalidad no se ha probado manualmente). El lanzador probado usa `unity/TestResults/P0/ManualSave`.

Correcciones descubiertas por las pruebas: apoyo de borde calculado desde la cara sólida, también al agarrarse al máximo alcance; brazo horizontal libre bajo techo sin cambiar azimut; contador de caída confirmado tras perder 5 cm, evitando contar varias veces la alternancia suelo/aire al abandonar un canto. No cambia el movimiento por corregir ese contador. Añadidas pruebas de timeout, paridad de llano, máximo alcance, cámara y caída única. **Edit 467/467 y Play 46/46**, `unity/TestResults/P0/edit-final.xml` y `play-final.xml`; suites completas U3 ejecutadas realmente. Referencias U0/U2/U3/U4 verificadas, sin regenerar esperados ni tocar src.

`scripts\p0.cmd build` y `devbuild` completados: Windows x64 Mono normal en `unity/Builds/P0/MAMPORRO-P0.exe`, Development en `unity/Builds/P0Dev/MAMPORRO-P0.exe`. `scripts\p0.cmd jugar` probado desde la raíz: salida 0, PID 25652, ruta QA confirmada en `manual.log`, ventana cerrada con `CloseMainWindow=True`, progreso personal idéntico en `personal-before.json`/`personal-after.json`. No equivale a aprobación manual de sensación/controles. Entrega U6 conserva su SHA y los siete ajustes protegidos sus huellas históricas.

Evidencia final: `unity/TestResults/P0/Evidence/20261006T172001`, 10 PNG reales, JSON válido y CSV de borde/techo. Revisadas las imágenes de esquema, volumen, transición, tejado y techo. Fixtures independientes salvo la secuencia física del borde; no se presentan como un recorrido manual continuo. `scripts\p0.cmd evidence` probado; `benchmark` y `devdiag` usan el mismo arnés con 0/300/500/750, ventanas visibles y rechazo de otra instancia P0.

Intentos descartados, conservados localmente: build restringida falló por licencia; player restringido falló antes de obtener persistentDataPath y su proceso residual se cerró antes de medir. Evidencia oculta `114853` dio PNG negros y fue rechazada aunque el arnés inicial aceptaba la física; se añadió validación visual. `115116` reveló menú sobre el esquema y cámara bajo techo demasiado próxima, corregidos. Medida `Normal/20261006T120043` interrumpida por atasco real de reenganche a máxima distancia y proceso residual: no se usa. `Normal/20261006T171212` recorre bien las cuatro cargas, pero sobrerregistra caídas: sustituida por la pasada final tras corregir y probar el contador.

**Siguiente paso de esta pieza:** terminar medidas finales normal/Development, registrar tablas y cierre técnico; no modificar más gameplay ni iniciar P0-B. Los binarios actuales indican `42509c2…+P0-WIP`: contienen el código de esta pieza antes de su commit; el cierre identificará el commit equivalente y las huellas de assemblies.

## Cierre técnico P0-A — 06/10/2026 (Codex)

**A1–A5 terminados y verificados; pendiente de prueba manual del autor. DETENERSE. P0-B/C/D NO autorizados.** No se declara P0 completo ni se integra esta escalada en la entrega U6.

### Publicación y procedencia

| Pieza | Commit |
| --- | --- |
| A1 — consultas/circuito | `508aa0f849eb536068359a90a6fc883a117d5937` |
| A2 — estados/escalada | `303b01c2a09a2d6bccb3f742f3f20ad5d8c97fe3` |
| A3 — integración/cámara | `beb32093cd56b59bba2223199a287bea13a2c6ab` |
| A4 — combate/altura | `42509c28b7994c820bf2d4c98339e7e3fafe35ad` |
| A5 — builds/lanzador/instrumentación | `2cbb299525990e3af809ee8fd4cf46fcd6063b67` |

Todos publicados por push normal en `claude/zen-pasteur-674ik0`, sin PR ni force. Tras A5, fetch confirmó el remoto esperado A4 antes de publicar. Este cierre añade solo documentación; su HEAD se obtiene con `git log -1 --format=%H -- docs/PROGRESO_P0.md`. Confirmar igualdad HEAD/remoto al entregar, sin publicar los siete ajustes.

Las builds se generaron antes del commit A5 y conservan procedencia honesta `42509c28b7994c820bf2d4c98339e7e3fafe35ad+P0-WIP`; su código es exactamente el publicado en `2cbb299` (diff de Core/U3/P0Project vacío). Manifiesto local de assemblies normal/Development: `unity/TestResults/P0/build-assemblies-sha256.json`. SHA-256 normal Core: `BE9EAAB5AA9D78B35C3D7B169AFA905F316995CB1F7749122BC260E09A2B6B73`; U3: `12513735F5ED752762D91F4AB34884C4940E2512F860861DCDEAED1C83FB715A`.

### Qué probar y cómo abrirlo

Desde la raíz del repositorio en **CMD**:

```cmd
scripts\p0.cmd jugar
```

Comando realmente probado, ventana abierta y cerrada correctamente, log con carpeta QA. Iniciar una partida desde el menú. Build normal `unity\Builds\P0\MAMPORRO-P0.exe`; conservar toda la carpeta. Development separado: `unity\Builds\P0Dev\MAMPORRO-P0.exe`. Progreso manual aislado en `unity\TestResults\P0\ManualSave`, sin liquidación/recompensas; huellas del progreso personal antes/después iguales. No sobrescribir `unity\Builds\Windows\MAMPORRO.exe`.

Controles: mantener botón derecho para agarrarse; W/S sube/baja y A/D desplaza lateralmente; Espacio salta separándose; soltar botón o Shift/C libera. Después de separarse, soltar y volver a pulsar antes de reenganchar. E conserva su función anterior. Sin agarre por mera proximidad, stamina ni duración máxima. Velocidades reales: **4,5 m/s vertical, 3,0 lateral, diagonal normalizada; suelo 9,5 m/s** sin rebalance. Transición de borde a 4,5 m/s; separación horizontal al saltar 5 m/s.

F4 alterna 0/300/500/750 enemigos controlados; F5 compara física/cámara U3 y P0 sobre la misma geometría QA; F7 rescata al inicio y marca recorrido inválido; F8 reinicia. El control F5 no sustituye las regresiones del mundo U3 original. Esc pausa; perder foco pausa y al volver requiere continuar.

Circuito fijo, coordenadas del núcleo (presentación invierte Z), inicio (4,0,-10):

| Caso | Ubicación/dimensión |
| --- | --- |
| Llano y ruta alternativa por suelo | Suelo alrededor de las estaciones; límite lógico 29 m |
| Rampa/meseta/tejado accesible | Oeste: X -24 a -12 asciende a 4 m; cubierta X -12 a -4 a Y 4,25 |
| Pared y tejado sin rampa | X 8..14, Z -4..4, altura 6 m |
| Techo que bloquea salida | X 6..15, Z -1..1, cara inferior Y 7 |
| Esquinas interior/exterior | Dos sólidos unidos: X 8..12/Z 10..16 y X 4..8/Z 14..16, altura 4 m |
| Acantilado sólido | X -2..4, Z 16..20, altura 8 m |
| Borde estrecho | X 20..20,5, Z -3..3, altura 5 m; no cabe la huella corporal completa |
| Hueco/caída | X 16..22, Z 8..14, profundidad 6 m; rodeable por suelo |
| Tipos excluidos | Follaje e interactuable en Z -12..-11; límite artificial oriental |

Escalables: caras verticales de estructuras y acantilados sólidos definidos en colisión. Excluidos: follaje, interactuables, enemigos, límites y caras inferiores de techos. Consultas explícitas de contacto/normal/ID/tipo, escalabilidad, espacio corporal, soporte completo, barrido continuo, borde y techo. Un solo PlayerBody/PlayerPhysics, sin Rigidbody paralelo ni root motion.

El borde se recorre en dos tramos físicos finitos (elevación y avance) tras comprobar cuerpo, soporte y barridos completos; no hay teleport. El destino se calcula desde la cara para admitir todo el alcance del agarre. Salir andando del tejado vuelve a aire. Bajo el techo central se detiene a Y≈5,45 y permite soltar/saltar. Las esquinas bloqueadas frenan o liberan al perder contacto; no hay giro automático sin barrido/contacto válidos.

Cámara U3 como referencia; P0 barre pivote y brazo contra paredes/techos, retrae inmediatamente y recupera suavemente (exponencial 4/s). Bajo techo prueba brazo horizontal con el mismo azimut, sin autoorientación/cinemática. Su sensación requiere la revisión manual del autor.

Antiatasco: intención sin avance ≥0,35 s contada; salto/separación siempre disponibles; transición con timeout 1,5 s y salida a aire/rearme si falla; barridos completos y soporte de toda la huella; pausa, foco, muerte y reinicio suspenden agarre/entradas. F7 o posición inválida/Y<-10 recuperan **solo QA al inicio**, con aviso, contador, ruta inválida y sin meta. No es recuperación al último punto seguro ni una finalización válida.

### Pruebas y evidencias ejecutadas

- Suites completas `scripts\u3.cmd edit`: **467/467**; `scripts\u3.cmd play`: **46/46**. Copias finales: `unity/TestResults/P0/edit-final.xml` y `play-final.xml`; logs originales en U3. No se inventa validación manual de controles/sensación.
- Casos: determinismo/tick fijo, llano equivalente a U3, agarre explícito/diagonal/120 s sin límite, salto/rearme, separación, rampa/escalón, borde válido/rechazado/máximo alcance/timeout, tejado→aire, esquinas, cuerpo completo, cámaras, pausa/foco, muerte/reinicio/rescate, guardado QA y combate/alturas. P0 desactivado deja la ruta original U3 sin delegado físico.
- `scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 iguales. Instantánea `qa-results/reference-367eb89e22634e9a9f7068d7d36abf27`. Diff frente al plan en `src/` y `unity/Docs/Reference` vacío; no se regeneraron referencias.
- Builds `scripts\p0.cmd build` / `devbuild`, apertura `jugar`, captura `evidence` y medidas `benchmark` / `devdiag` realmente ejecutadas. Editor cerrado durante las medidas y sin otras instancias P0.
- **Evidencia final local:** `unity/TestResults/P0/Evidence/20261006T172001`, 10 PNG + `evidence.json` válido. `00-esquema-circuito`, `01-antes-borde`, `02-escalada-volumen`, `03-transicion-borde`, `04-sobre-tejado`, `05-caida-tejado`, `06-techo-bloqueado`, `07-rescate-invalida`, `08-esquina`, `09-horda-desde-tejado`. Volumen conservador magenta superpuesto al visual, no collider nuevo.
- Secuencia de PNG en lugar de vídeo; CSV `route-edge.csv` y `route-blocked.csv`. La evidencia de techo registra atasco a 1,583 s en Y=5,449999; rescate a 3 s, conserva el atasco y suma 1 recuperación. Causa: «ascenso/borde bloqueado». Ese recorrido es inválido.
- Artefactos binarios/CSV/PNG locales excluidos de Git, como las entregas anteriores. No se publican datos ni telemetría externa.

### Métricas de recorrido

Ruta automatizada repetida: subir hasta 3,5 m, bajar hasta 1 m, volver a subir, completar tejado, salir por el canto oeste, caer y regresar por suelo. Semilla P0QA y tick 1/60; el control decide por estado físico real, sin reposicionamientos dentro de la ruta. **No es un tour continuo por todas las estaciones del circuito.**

En vacío, primera salida al tejado a **2,700 s**, primer ciclo terminado tras aterrizar a **3,417 s**; ciclos siguientes ~4,2 s por incluir regreso desde el aterrizaje. Cada una de las ocho pasadas completa **9 bordes y 9 caídas**, con **0 fallos de borde, 0 agarres/reenganches involuntarios, 0 atascos y 0 rescates**. Duración/distancia incluyen warmup y el último ciclo parcial; el coste por fotograma solo usa la ventana posterior de 30 s. Ascenso/descenso en metros incluye cualquier movimiento vertical; los tiempos de subir/bajar cuentan intención de escalada, sin los tramos del borde.

| Build | Enemigos | Ruta s | Distancia m | Ascenso m | Descenso m | Subiendo s | Bajando s |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 39.98 | 257.23 | 84.32 | 79.52 | 18.43 | 5.67 |
| Normal | 300 | 40.10 | 242.22 | 76.97 | 76.97 | 16.80 | 5.10 |
| Normal | 500 | 40.10 | 241.61 | 76.97 | 76.97 | 16.80 | 5.10 |
| Normal | 750 | 40.10 | 241.38 | 76.97 | 76.97 | 16.80 | 5.10 |
| Development | 0 | 39.97 | 257.15 | 84.24 | 79.52 | 18.42 | 5.67 |
| Development | 300 | 40.10 | 242.22 | 76.97 | 76.97 | 16.80 | 5.10 |
| Development | 500 | 40.10 | 241.61 | 76.97 | 76.97 | 16.80 | 5.10 |
| Development | 750 | 40.10 | 241.38 | 76.97 | 76.97 | 16.80 | 5.10 |

Tiempo sin amenaza se mide en las sondas de A4, **no** en este benchmark invulnerable: tejado inaccesible 30 s sin daño melee; la ruta directa por rampa alcanza la cubierta y mata al jugador inmóvil a 14,45 s. Contacto a la misma altura funciona. Naftalina/Jersey y proyectiles amistosos conservan fallos XZ de altura/oclusión; proyectiles hostiles siguen heightfield+1, atraviesan sólidos y no alcanzan el tejado de 6 m. Barra y charco de fregona conservan sus filtros de altura. Sin arreglos encubiertos de P0-B, escalada/teleport de enemigos ni cambios de vida/daño/oro/director.

### Rendimiento del circuito QA, no FPS generales del juego

Equipo: Ryzen 7 7700X, RTX 4070 Ti SUPER; Unity 6000.6.3f1, Windows x64 Mono, D3D11, ventana 1920×1080, altura interna 360, VSync desactivado y sin límite de fotogramas. Circuito fijo, horda centralizada de cantidad exacta, armas desactivadas e invulnerabilidad **solo en arnés** para mantener las cargas. Normal y Development separados, una pasada por carga, 10 s de warmup + 30 s de muestreo, sin auto-reanudaciones por foco en las ocho medidas. HUD e instrumentación QA incluidos. No es coste aislado de consultas ni equivalencia de un combate real completo.

Fuentes: `unity/TestResults/P0/Normal/20261006T172056` y `unity/TestResults/P0/Development/20261006T172419`; cuatro `load-N.json`, `frames-N.csv`, `route-N.csv` y PNG por carpeta. Todos los informes válidos. No se usan intentos anteriores. Frame time de reloj real entre LateUpdate; CPU medida de lógica/combate/presentación del tick, no CPU total del proceso. **GPU: N/D** (el nombre del dispositivo no es una medida temporal).

| Build | Enemigos | Frame p50 ms | p95 ms | p99 ms | Máx ms | Frames / ticks medidos |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.310 | 0.603 | 1.010 | 8.548 | 85749 / 1800 |
| Normal | 300 | 0.391 | 0.816 | 1.110 | 5.596 | 67228 / 1800 |
| Normal | 500 | 0.446 | 0.996 | 1.534 | 11.971 | 57620 / 1801 |
| Normal | 750 | 0.519 | 1.528 | 2.364 | 28.279 | 45467 / 1801 |
| Development | 0 | 0.392 | 0.841 | 1.749 | 25.329 | 64497 / 1800 |
| Development | 300 | 0.473 | 1.045 | 1.576 | 5.644 | 54359 / 1800 |
| Development | 500 | 0.531 | 1.395 | 1.767 | 4.175 | 47429 / 1800 |
| Development | 750 | 0.587 | 1.600 | 2.303 | 13.453 | 42098 / 1800 |

| Build | Enemigos | CPU tick p50 ms | p95 ms | p99 ms | Máx ms |
| --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.009 | 0.013 | 0.015 | 0.038 |
| Normal | 300 | 0.565 | 0.607 | 0.659 | 0.757 |
| Normal | 500 | 1.032 | 1.094 | 1.344 | 1.550 |
| Normal | 750 | 1.639 | 1.938 | 2.205 | 3.706 |
| Development | 0 | 0.010 | 0.014 | 0.018 | 0.038 |
| Development | 300 | 0.570 | 0.626 | 0.763 | 0.815 |
| Development | 500 | 1.036 | 1.139 | 1.362 | 1.472 |
| Development | 750 | 1.617 | 1.710 | 1.930 | 2.321 |

Memoria es una instantánea al final antes de PNG/serialización, no pico ni RSS; asignada/reservada por Unity y memoria gestionada Mono. GC collections mide gen0 durante ~40 s incluidos warmup; B/frame se obtiene del ProfilerRecorder solo en Development durante los 30 s medidos. Normal B/frame **N/D**. Las colecciones son elevadas con el bucle sin límite e interfaz/instrumentación; estos datos no justifican afirmar «cero GC» ni comparar directamente las reservas de ambas builds.

| Build | Enemigos | Asignada MiB | Reservada MiB | Mono MiB | GC gen0 / pasada | GC B/frame |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 236.31 | 282.25 | 19.47 | 662 | N/D |
| Normal | 300 | 237.13 | 538.31 | 19.43 | 515 | N/D |
| Normal | 500 | 237.69 | 538.31 | 18.84 | 442 | N/D |
| Normal | 750 | 238.25 | 538.31 | 19.32 | 355 | N/D |
| Development | 0 | 154.47 | 543.56 | 19.22 | 511 | 9181.1 |
| Development | 300 | 154.75 | 543.56 | 19.25 | 417 | 9314.1 |
| Development | 500 | 154.86 | 543.56 | 19.35 | 372 | 9399.1 |
| Development | 750 | 154.84 | 543.56 | 19.59 | 331 | 9535.1 |

### Integridad, límites y diferencias respecto al plan

SHA-256 de la entrega U6 **antes = después**:
`96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`.
No se ejecutó build U3 para sobrescribirla. Progreso personal sin cambios; solo carpetas QA.

Siete ajustes locales protegidos: exactamente los mismos cinco modificados y dos sin seguimiento, ni publicados ni revertidos. Huellas antes/después iguales:

| Estado | Ruta | SHA-256 |
| --- | --- | --- |
| M | unity/Assets/Mamporro/Generated/RetroPipeline.asset | F88523DA037CF80DB06027F1675AC19CDCC930CEE570FA51BE80A5945A62CE0D |
| M | unity/Assets/UniversalRenderPipelineGlobalSettings.asset | 71B8AD93611C4B9C94444409A5CBA080B8B7A903EF1E797F1B5CAD37D38E2ADE |
| M | unity/ProjectSettings/GraphicsSettings.asset | 35DD86B42C01873514B78C0784AB29078F557769A17097371198CDCE6AFDE5F2 |
| M | unity/ProjectSettings/ProjectAuditorSettings.asset | 9E68444B9BDA981E2927A98DF9EDF69EA88362F0C6B7F3C81B39B9D515D2A416 |
| M | unity/ProjectSettings/ProjectSettings.asset | 4505CD0097FE03D0BEF99BA42F8E0742A0660C197CF037634908654A9A1F45DB |
| ?? | unity/ProjectSettings/PackageManagerSettings.asset | 7698172AB4C40EEB7B992D9255CE6AFDF39F9288C1C3A31C58A96A474DC43753 |
| ?? | unity/ProjectSettings/URPProjectSettings.asset | 68D75E5E1502DD7A4C18B7306430916B5EE8DADFDB0FBB588893FE38EAA2C056 |

Diferencias concretas: escena U3 reutilizada con circuito/datos optativos y define exclusivo en la build, evitando un segundo controlador o duplicar combate. Consultas conservadoras por cajas alrededor del cuerpo: pueden rechazar diagonales muy justas; no son geometría general ni navegación definitiva. Esquinas sin giro automático; salida estrecha rechazada; foso puede requerir F7. Rescate al inicio en vez de último punto seguro; siempre inválido y visible. Cámara puede probar brazo horizontal bajo techo manteniendo azimut. Secuencia de imágenes sustituye vídeo. Subruta repetida medida, resto de estaciones mediante casos automatizados/fixtures y posterior revisión manual. No hay medida GPU fiable ni GC B/frame normal. Build sin argumentos protegida por código, apertura realmente probada mediante el CMD documentado.

No se observaron penetraciones ni atascos irrecuperables en los casos fijados; eso no sustituye la prueba manual de todos los ángulos, esquinas y sensación. Refugios inaccesibles, ausencia de búsqueda de rutas alternativas y ataques XZ heredados siguen siendo limitaciones reales. Sin balance ni ampliación del mundo, cambios de src/esperados históricos, NavMeshAgent, Rigidbody/Update por enemigo, P1/P2 ni adopción VAT.

**Siguiente paso exacto: el autor ejecuta `scripts\p0.cmd jugar` y revisa escalada, pared→tejado, salto/soltar/rearme, esquinas, techo y cámara. Esperar su valoración y autorización expresa. No comenzar P0-B.**

## A6.1 — entrada automática y contrato de paredes (07/10/2026, Codex)

WIP recibido integrado: sin GrabHeld ni lectura de botón derecho; entrada con input XZ > 0,1, dot(input normalizado, -normal pared) >= 0,5, barrido corporal Reach 0,18 m y volumen libre. Escalones <= 0,45 m conservan paso de suelo. Contrato VerticalSolid por exclusiones (follaje/interactuable/límite/enemigo); constructor sin categoría crea sólido normal escalable. Solo caras verticales del primer sólido alcanzado; no atravesar obstáculos para buscar otro.

Espacio separa a 5 m/s y usa salto existente; protección inicial ensayada de 0,35 s sobre mismo ID y normal de cara (dot > 0,99), otra cara/pared válida puede entrar antes. Pausa/foco/reinicio aplican protección global breve y limpian input. Velocidades 4,5/3/9,5 conservadas. Foso revestido por cuatro sólidos físicos con apoyo superior completo, sin lista de IDs en detección.

Pruebas: scripts\u3.cmd edit, 07/10/2026, **497/497** en unity/TestResults/U3/edit.xml y edit.log. Incluye umbral .49/.50/.51, suelo sin salto, aire, paralelo/roce, pared nueva sin configuración, cuatro caras de foso, caída desde arriba/fondo/salida, caída junto a pared, separación y contacto con otra pared durante protección. Primer ensayo: dos tests de trayecto antiguo sobrepasaban su destino; acotados al borde/salida que verifican, sin cambiar referencias históricas. Play/build/evidencia/medidas A6 pendientes. U6 y siete protegidos conservados; sin src.

Pieza: «Activa la escalada automática por paredes sólidas». Siguiente: completar antiatasco, pruebas de integración y evidencia A6; no P0-B.

## A6.2 — antiatasco e integración del foso (07/10/2026, Codex)

A6.1 publicado y remoto verificado en `0698f25ffc16f8f36bdfd542b62c9fdd62e4f57c`. Al bajar al suelo se conserva ese estado mientras siga la intención vertical negativa; al dejar de bajar se habilita automáticamente la entrada. Evita alternar suelo/escalada sin pedir rearme ni cambiar velocidad. Pruebas de acantilado desde suelo y salto/retorno/salida del foso añadidas.

**Pruebas reales:** `scripts\u3.cmd edit` **500/500**; `scripts\u3.cmd play` **47/47**, XML/log en unity/TestResults/U3, 07/10/2026. Play incluye foso completo con salto, cámara fuera de sólidos en cada tick muestreado, cero rescates/reenganches indebidos; conserva pausa/foco, muerte/reinicio y P0 desactivado. La primera pasada Play detectó otro recorrido antiguo que llegaba a la segunda pared; la prueba de caída se acotó antes de ella. Un primer intento Edit de esta pieza encontró una comilla perdida en el test nuevo; corregida antes de las suites válidas. Corregida también codificación UTF-8 de textos tocados; ninguna referencia esperada histórica modificada.

Regresión histórica ejecutada: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`, U0/U2/U3/U4 coinciden. Instantánea conservada en `qa-results/reference-44ca3847f55848aba8edec1d01ee767c`; 8 guardados y 6 secuencias meta correctos. Sin cambios en src ni corpus.

Pieza: «Protege el descenso y verifica la salida del foso». Siguiente: evidencia A6, build QA normal/Development y medidas comparables, cierre para prueba manual. Sin P0-B.

## A6.3 — evidencia, builds y medidas (07/10/2026, Codex)

A6.2 publicado y remoto verificado en `603a225dbf4ecc89e5359528bdf54d24c6dbd49d` (SHA corregido al auditar el 08/10). Se amplió exclusivamente el arnés P0Capture: 17 PNG y seis trazas CSV, fixtures separados y rutas continuas dentro de cada fixture. Compilación y ejecución reales con `scripts\p0.cmd build`, `evidence`, `benchmark`, `devbuild`, `devdiag`. Informes válidos: 1/1 evidencia y 8/8 cargas. Builds normal `unity/Builds/P0/MAMPORRO-P0.exe` y Development `unity/Builds/P0Dev/MAMPORRO-P0.exe`; fuente registrada `603a225+P0-WIP`: el arnés quedó sin commit al agotarse la sesión anterior. Los artefactos existen, pero esta pieza no había sido publicada; conservada e integrada en la corrección del 08/10.

Evidencia: `unity/TestResults/P0/Evidence/20261007T174932`. Revisadas imágenes de entrada, salto, transición, caída, fondo, salida, paralelo, techo y esquina; se conserva volumen magenta de consulta, sin collider adicional. PNG 01–06: aproximación frontal, entrada automática, ascenso, salto/separación, transición y tejado; 07–12: caída al foso, fondo, escalada y salida; 13: paralelo sin agarre; 14: entrada aérea; 15–16: techo bloqueado/esquina; 00: vista del circuito. No vídeo: secuencia fiable con infraestructura existente, no validación de sensación manual.

Traza continua del foso: caída desde (19,2,11), fondo y=-6 a 0,617 s, entrada desde fondo a 0,933 s, borde a 2,250 s, salida completa con apoyo a 2,517 s en (22,42; 0,002; 11), cero rescates/atascos/reenganches indebidos. Tejado: entrada a 0,333 s, salto a 0,800 s, reenganche a 1,150 s (0,350 s), borde a 2,050 s y apoyo completo a 2,283 s. Son tiempos de estos fixtures automatizados, no tiempos universales ni prueba humana.

Lanzador probado realmente con shell `cmd.exe`: `scripts\p0.cmd jugar`, salida 0, ventana/proceso respondiente desde la ruta P0 y log `unity/TestResults/P0/manual.log`, guardado `unity/TestResults/P0/ManualSave`. Cerrado después de comprobar arranque. No se escribe progreso personal ni se sustituye U6.

### Método y resultados A6

Misma metodología A5: Ryzen 7 7700X / RTX 4070 Ti SUPER, Mono Windows x64 D3D11, 1920x1080, interna 360, VSync 0, sin límite de frames, 10 s warmup + 30 s medidos por carga, tick 1/60, semilla P0QA, horda exacta centralizada, armas off e invulnerabilidad solo QA. Se conserva el recorrido de subir/bajar/tejado/caída/regreso; cambia la entrada a automática y se añaden cuatro sólidos del foso. Las ocho pasadas completan 9 bordes y 9 caídas, con 0 fallos de borde, atascos, rescates, agarres indebidos y reanudaciones por foco. No es coste aislado de consultas ni FPS generales.

Fuentes: `unity/TestResults/P0/Normal/20261007T175005` y `unity/TestResults/P0/Development/20261007T175340`; load-N.json, frames-N.csv, route-N.csv y PNG. Comparación con carpetas A5 20261006T172056 (Normal) y 20261006T172419 (Development), sin regenerarlas.

| Build | Enemigos | Frame p50 ms | p95 | p99 | Máximo | Delta p95 vs A5 ms |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.306 | 0.518 | 0.979 | 26.610 | -0.085 |
| Normal | 300 | 0.402 | 0.984 | 1.385 | 5.346 | +0.168 |
| Normal | 500 | 0.447 | 1.084 | 1.602 | 5.573 | +0.088 |
| Normal | 750 | 0.515 | 1.177 | 2.254 | 8.208 | -0.351 |
| Development | 0 | 0.384 | 0.800 | 1.071 | 6.370 | -0.041 |
| Development | 300 | 0.472 | 0.983 | 1.244 | 3.836 | -0.062 |
| Development | 500 | 0.524 | 1.129 | 1.634 | 4.437 | -0.267 |
| Development | 750 | 0.586 | 1.315 | 2.308 | 3.697 | -0.285 |

CPU es el tiempo de lógica/combate/presentación del tick, no CPU total del proceso. GPU **N/D**.

| Build | Enemigos | CPU p50 ms | p95 | p99 | Máximo | Delta p95 vs A5 ms |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.010 | 0.015 | 0.019 | 0.095 | +0.002 |
| Normal | 300 | 0.580 | 0.697 | 0.791 | 0.983 | +0.090 |
| Normal | 500 | 1.038 | 1.137 | 1.375 | 2.193 | +0.043 |
| Normal | 750 | 1.642 | 1.806 | 2.115 | 2.716 | -0.131 |
| Development | 0 | 0.009 | 0.013 | 0.015 | 0.037 | -0.001 |
| Development | 300 | 0.572 | 0.619 | 0.710 | 0.840 | -0.007 |
| Development | 500 | 1.034 | 1.094 | 1.279 | 1.391 | -0.046 |
| Development | 750 | 1.624 | 1.715 | 1.853 | 2.144 | +0.005 |

| Build | Enemigos | Asignada MiB | Reservada MiB | Mono MiB | GC gen0 | GC B/frame |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 244.47 | 282.25 | 19.25 | 674 | N/D |
| Normal | 300 | 245.28 | 538.31 | 19.23 | 479 | N/D |
| Normal | 500 | 245.72 | 538.31 | 19.54 | 438 | N/D |
| Normal | 750 | 250.34 | 538.31 | 19.16 | 374 | N/D |
| Development | 0 | 154.26 | 543.56 | 19.51 | 540 | 9514.6 |
| Development | 300 | 154.35 | 543.56 | 19.15 | 428 | 9650.4 |
| Development | 500 | 154.51 | 543.56 | 19.45 | 383 | 9776.6 |
| Development | 750 | 154.42 | 543.56 | 19.71 | 336 | 9959.7 |

Memoria: instantánea final Unity/Mono, no pico/RSS/VRAM; GC gen0 durante ~40 s, B/frame del recorder solo fiable en Development. HUD/instrumentación están incluidos. Una pasada por carga: no atribuir ruido o picos aislados exclusivamente a detección. En Normal, CPU p95 cambia +0,002/+0,090/+0,043/-0,131 ms; no aparece crecimiento sostenido causado por la entrada automática. El vacío tiene un pico real de 26,610 ms (1/87033 frames >16,67 ms); 300/500/750 no tienen frames >16,67 ms. No se oculta el pico ni se promete ausencia de regresiones en otros equipos. Las reservas de memoria se mantienen comparables; asignación final Normal aumenta aproximadamente 8–12 MiB frente a A5.

Integridad tras builds: U6 SHA idéntico al inicial y 7/7 protegidos coinciden byte a byte con la tabla histórica. Informe `unity/TestResults/P0/A6/integrity.json`; XML/log Edit y Play archivados allí. Sin src ni corpus, sin P0-B/C/D. Siguiente: cierre documental y detenerse para la prueba manual A6.

## Corrección A6 — feedback A/D y meseta/rampa (08/10/2026, Codex)

### Estado recibido y piezas conservadas

Retomado tras fetch en `603a225dbf4ecc89e5359528bdf54d24c6dbd49d`, igual a origin, 0/0. Dos commits A6 existentes reutilizados sin duplicarlos: `0698f25` y `603a225`. Diez archivos WIP válidos (P0Capture y nueve documentos), además de los siete protegidos; índice vacío. A6.3 sí había generado builds, evidencia y ocho medidas, pero su código/documentación seguían sin commit. El SHA erróneo y la afirmación de publicación de ese apartado se han corregido, conservando las métricas históricas. No se descartó WIP.

Commits de implementación nuevos: `53658e4` («Corrige izquierda y derecha sobre todas las caras de pared»), `bb726db` («Incluye las paredes del terreno en la escalada automática»), `452c956` («Amplía la evidencia de controles, meseta y salidas del foso»). Publicación final y cierre se registran debajo.

### Causas y comportamiento corregido

**A/D:** el núcleo empleaba `(-normal.Z, 0, normal.X)` como derecha, sin compensar la reflexión Z hacia Unity. D apuntaba a la izquierda visible. Las pruebas de velocidad lateral usaban valor absoluto y no detectaban el signo. Ahora `WallRight` normaliza la proyección horizontal y devuelve `(normal.Z, 0, -normal.X)`: derecha del jugador erguido mirando hacia la pared, equivalente a `Cross(up, forwardHaciaPared)` en Unity. No cambia con pitch/yaw. A es negativo y D positivo.

| Normal exterior (coordenadas núcleo) | A izquierda | D derecha |
| --- | --- | --- |
| +X | +Z | -Z |
| -X | -Z | +Z |
| +Z | -X | +X |
| -Z | +X | -X |

**Muros omitidos:** el autor confirmó meseta/rampa. Su pared física está en Heightfield, pero VerticalQueries solo recibía las 14 cajas del circuito. No era un permiso `climbable` por muro ni un umbral de intención demasiado estricto. Prueba de regresión explícita: mismas cajas sin terreno no detectan la pared en (-8,1,5.152); con el adaptador del terreno sí. Corregidos ambos laterales y el extremo elevado de meseta y los laterales de rampa, sin modificar el visual ni añadir colliders particulares a sus IDs.

**Contrato:** semántica sólida permitida y normal de cara con pendiente >48° y `n.Y >= 0`; parámetro central `WallMinSlopeDegrees`. Caras abruptas triangulares reales del Heightfield y cajas comparten contacto/barrido/volumen/soporte P0. Se recorren solo celdas de la envolvente, usando SAT continuo; no hay whitelist ni alta manual por pared. Datos generados con ±X/±Z y direcciones oblicuas usan el mismo adaptador. IDs solo identifican contactos. Suelo/pendientes transitables, tapas y caras inferiores no son paredes; follaje, interactuables, enemigos y límites quedan excluidos por semántica. Las superficies superiores siguen ofreciendo soporte para borde.

Entrada frontal automática desde suelo/aire; dot horizontal >=0,5 y Reach 0,18 conservados, sin botón/salto previo. Paralelo y roce siguen sin enganchar. W/S vertical, A/D lateral, diagonal normalizada, 4,5/3/9,5 m/s conservados. Las paredes inclinadas acompañan la pendiente manteniendo 4,5 m/s verticales y movimiento barrido.

Espacio conserva separación de 5 m/s y 0,35 s de protección automática. En terreno la protección reconoce triángulos coplanares contiguos para que cambiar de ID no la evada. Otra cara válida conserva continuidad. Descenso, salto existente y rearme sin soltar/re-pulsar se mantienen.

Borde conserva ascenso exterior y avance barridos, cuerpo completo y apoyo válido. El apoyo puede abarcar tapas coplanares contiguas o toda la huella sobre terreno transitable; en rampa se usa su altura máxima bajo la huella para no introducir el cuerpo. Esquinas interiores bloqueadas detienen el avance; A permite retroceder y Espacio separar. En exterior se pierde agarre al agotarse la superficie: no hay giro automático alrededor del canto ni teleport. Cambiar de contacto recalcula la misma base lateral, sin invertir input. Cámara jugable sin rediseño; únicamente incorpora la geometría del terreno en sus consultas P0.

### Verificación ejecutada y evidencia

- `scripts\u3.cmd edit`: **538/538**; `scripts\u3.cmd play`: **49/49**, 08/10. XML y logs finales en `unity/TestResults/P0/A6Correccion/{edit,play}.{xml,log}`. Pasadas intermedias también correctas; no se cambiaron expected históricos para lograrlo.
- A/D con signo en ±X/±Z, base oblicua y desplazamiento real sobre terreno oblicuo, magnitudes/diagonal. Play inyecta teclas A/D: 24 combinaciones de cuatro paredes, pitch -15/20/60 y dos teclas, con oráculo independiente de base Unity. Cámara fuera de sólidos.
- Contrato generado sin registro de IDs, categorías permitidas/excluidas, umbral 47,99/48/48,01/90/100/0/180°, barrido de alta velocidad, tangencia, caras superiores, apoyo con/sin hueco entre colliders, cruce lateral entre colliders y esquinas interior/exterior. Meseta y rampa completan borde; aire frontal funciona, paralelo/roce no engancha.
- Foso: caída, aterrizaje, entrada y borde por las cuatro orientaciones; cámara libre, cero rescates. Salto/retorno/salida y protección entre triángulos del terreno. Conservadas pausa/foco, muerte/reinicio, combate QA y P0 desactivado.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coinciden, 8 guardados y 6 secuencias meta; instantánea `qa-results/reference-298452535d744484b9401c1a26e0d674`. Sin modificaciones de src ni referencias.
- `scripts\p0.cmd build`, `devbuild` y `evidence` ejecutados. Ambas builds finales registran **452c956f2c72dd8c9cb15bed5a0000305b6aefbb**, sin WIP runtime. Un intento de recompilación dentro del sandbox terminó por licencia Unity (198); reintentado fuera del sandbox, build correcta. No fue un fallo de compilación del código.
- Evidencia final: **`unity/TestResults/P0/Evidence/20261008T181005`**, informe válido, **41 PNG y 15 CSV**. La pasada anterior `20261008T180719` también fue válida y se conserva. La final mejora legibilidad de A/D con cámara fija solo en esos fixtures y jugador a media altura; los demás recorridos usan cámara jugable. CSV incluye input lateral/vertical, normal y posición por tick.
- PNG 17/18/19 de cada normal: antes, A izquierda y D derecha; 20–22 meseta ±Z: entrada, ascenso, salida; 23/24 foso: caras oeste/norte/sur además de la este ya mostrada en 07–12. Se conservan entrada aérea, salto/reenganche, paralelo, techo bloqueado, esquina y tejado. Imágenes representativas inspeccionadas; **evidencia automatizada, no aprobación manual**.

Limitación de alcance: adaptador de las cajas y Heightfield físicos del circuito, no importador de meshes arbitrarios Unity ni activación de escalada en el mundo U6. Las oblicuas se verifican mediante triángulos del terreno; las cajas actuales siguen alineadas a ejes. No hay nueva geometría de casas procedurales, navegación enemiga, mundo ampliado ni otros bloques. El mapa y las pendientes existentes se conservan.

## Cierre técnico A6 — 11/10/2026 (Codex)

### Estado conservado y última corrección

Reanudación desde `452c956f2c72dd8c9cb15bed5a0000305b6aefbb`, tres commits por delante de origin `603a225` tras fetch, nueve documentos modificados y siete ajustes protegidos; índice vacío. Se conservaron los commits A/D `53658e4`, terreno `bb726db` y evidencia `452c956`, sus pruebas, builds y medidas. Los documentos WIP se completan en este cierre. Sin reset, clean, stash, cambios de rama ni modificaciones de src/corpus.

La auditoría encontró una omisión adicional: `IsReattachBlocked` extendía la protección entre triángulos del terreno, pero dos cajas de una misma pared seguían dependiendo del ID. Corregido en `38d29a9ced593bc94e3a55638363b70978db822c`, «Protege el salto entre piezas de una misma pared»: misma comprobación de plano/normal para cualquier contacto, sin cambiar los 0,35 s ni el agarre a una cara distinta. Cuatro regresiones ±X/±Z fuerzan contacto disponible en la segunda pieza durante la protección; son sondas del contrato, no una trayectoria de gameplay. Fallaban antes de la corrección (538/542) y pasan después. Un primer fixture no alcanzaba la junta y se corrigió antes de reproducir el defecto; ambos resultados fallidos están conservados y no se presentan como pruebas válidas.

### Comportamiento final y cobertura

- A izquierda y D derecha mirando hacia la pared. Tangente normalizada `(n.Z, 0, -n.X)` en coordenadas núcleo, equivalente a `Cross(up, haciaPared)` tras reflejar Z en Unity. Ver tabla de las cuatro normales en la sección anterior. Play inyecta A/D en 24 combinaciones (cuatro paredes, pitch −15/20/60, dos teclas) con oráculo Unity independiente. Oblicuas del terreno y cambios entre colliders también pasan.
- Paredes sólidas de estructura/acantilado por defecto, sin permisos por ID. Las consultas reciben cajas y triángulos físicos del Heightfield: la omisión del terreno era la causa de meseta/rampa no escalables. Normal no inferior y pendiente >48°; suelo transitable y tapas son apoyo, no escalada. Follaje, interactuables, enemigos y límites artificiales excluidos por categoría. El borde estrecho tiene colisión válida, pero no permite terminar arriba porque no cabe la huella completa; el techo central bloquea el ascenso deliberadamente. No se alteró el visual para ocultar una colisión defectuosa.
- Correr frontalmente entra desde suelo o aire, sin salto ni botón especial. Dot horizontal ≥0,5 y alcance 0,18 m conservados; paralelo y roce leve no agarran. W/S vertical, A/D lateral, diagonal normalizada, velocidades 4,5/3,0/9,5 m/s. Sin stamina, límite temporal ni coste.
- Foso: caída al fondo, aproximación, ascenso y salida con apoyo completo por las cuatro caras, cero F7; también entrada aérea junto a pared, salto/retorno y rechazo del movimiento paralelo. No es aprobación manual.
- Espacio separa y protege automáticamente la misma superficie, incluidas juntas coplanares. Borde con cuerpo completo, soporte y dos tramos barridos; techo inferior bloquea, sin teleport ni penetración en las pruebas. Cámara aprobada conservada; pausa, foco, muerte/reinicio y ruta histórica sin P0 pasan.
- Esquinas: interior bloquea y permite retroceder/separarse; exterior termina el agarre al agotarse la cara. No hay giro automático alrededor del canto. No se introduce soporte de meshes arbitrarios ni se activa escalada en la entrega U6; integración de mundo/procedural sigue fuera de A6.

### Verificación final del código

Ejecutados el 11/10 sobre el contenido final de `38d29a9`:

| Comando desde CMD | Resultado | Evidencia local |
| --- | --- | --- |
| `scripts\u3.cmd edit` | 542/542, 0 fallos | `unity/TestResults/P0/A6Cierre/edit.xml`, `edit.log` |
| `scripts\u3.cmd play` | 49/49, 0 fallos | `unity/TestResults/P0/A6Cierre/play.xml`, `play.log` |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` | U0/U2/U3/U4 coinciden; 8 guardados y 6 secuencias meta | `qa-results/reference-a50b90a8e5dc4b63b3360150710e1f31` |
| `scripts\p0.cmd build` | Normal correcta | `unity/TestResults/P0/build.log`, `unity/Builds/P0/P0-source.txt` |
| `scripts\p0.cmd devbuild` | Development correcta | `unity/TestResults/P0/devbuild.log`, `unity/Builds/P0Dev/P0-source.txt` |
| `scripts\p0.cmd evidence` | Informe válido, 41 PNG y 15 CSV | `unity/TestResults/P0/Evidence/20261011T174056` |

El primer intento Edit en sandbox terminó por licencia Unity (198), antes de ejecutar pruebas. Fuera del sandbox se completaron las suites. Regresión roja real archivada como `edit-contact-red.xml/log`, primer fixture insuficiente como `edit-red.xml/log`. No se cambiaron referencias esperadas históricas.

Ambas builds registran `38d29a9ced593bc94e3a55638363b70978db822c`, sin WIP runtime. La evidencia final vuelve a ejecutar todos los fixtures sobre esa build; se inspeccionaron capturas A/D, salida de meseta y foso. Las capturas A/D usan cámara fija exclusivamente para hacer visible el signo del movimiento; los demás recorridos conservan cámara jugable. Son capturas automatizadas, no vídeo ni prueba humana. Se conservan las evidencias del 07/10 y 08/10 como historial.

### Rendimiento final

Nuevas pasadas del código final: `scripts\p0.cmd benchmark` → `unity/TestResults/P0/Normal/20261011T174138`; `scripts\p0.cmd devdiag` → `unity/TestResults/P0/Development/20261011T174438`. Ambas registran `38d29a9`, informes válidos para 0/300/500/750. Cada pasada: 9 bordes y 9 caídas, cero fallos de borde, atascos, rescates, agarres/reenganches indebidos y reanudaciones por foco.

Mismo equipo Ryzen 7 7700X / RTX 4070 Ti SUPER, Windows x64 Mono D3D11, 1920×1080, interna 360, VSync 0, ilimitado, semilla P0QA, tick 1/60, 10 s de warmup + 30 s medidos por carga; armas desactivadas e invulnerabilidad QA. Sin builds ni suites simultáneas. Una pasada por carga; no es coste aislado de consultas ni FPS generales del juego. Las medidas del 07/10 y Normal del 08/10 se conservan como comparación histórica; estas tablas corresponden al código final.

| Build | Enemigos | Frame p50 ms | p95 | p99 | Máximo |
| --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.325 | 0.693 | 1.281 | 8.152 |
| Normal | 300 | 0.409 | 0.885 | 1.224 | 8.069 |
| Normal | 500 | 0.458 | 1.044 | 1.578 | 4.225 |
| Normal | 750 | 0.520 | 1.297 | 2.247 | 7.586 |
| Development | 0 | 0.407 | 0.636 | 1.139 | 12.940 |
| Development | 300 | 0.491 | 0.955 | 1.204 | 8.972 |
| Development | 500 | 0.546 | 1.058 | 1.668 | 4.128 |
| Development | 750 | 0.582 | 1.317 | 2.281 | 5.731 |

CPU del tick de lógica/combate/presentación, no CPU total del proceso. GPU N/D.

| Build | Enemigos | CPU p50 ms | p95 | p99 | Máximo |
| --- | --- | --- | --- | --- | --- |
| Normal | 0 | 0.013 | 0.019 | 0.022 | 0.042 |
| Normal | 300 | 0.578 | 0.687 | 0.761 | 0.877 |
| Normal | 500 | 1.050 | 1.157 | 1.351 | 1.503 |
| Normal | 750 | 1.649 | 1.834 | 2.035 | 2.365 |
| Development | 0 | 0.013 | 0.019 | 0.022 | 0.047 |
| Development | 300 | 0.587 | 0.673 | 0.768 | 0.812 |
| Development | 500 | 1.061 | 1.174 | 1.360 | 1.471 |
| Development | 750 | 1.624 | 1.785 | 2.005 | 2.219 |

| Build | Enemigos | Asignada MiB | Reservada MiB | Mono MiB | GC gen0 | GC B/frame |
| --- | --- | --- | --- | --- | --- | --- |
| Normal | 0 | 244.406 | 282.250 | 20.293 | 585 | N/D |
| Normal | 300 | 245.406 | 538.313 | 19.758 | 462 | N/D |
| Normal | 500 | 245.906 | 538.313 | 20.414 | 403 | N/D |
| Normal | 750 | 246.469 | 538.313 | 20.332 | 343 | N/D |
| Development | 0 | 154.675 | 543.563 | 20.004 | 474 | 9532.159 |
| Development | 300 | 154.792 | 543.563 | 20.090 | 394 | 9670.752 |
| Development | 500 | 154.974 | 543.563 | 20.359 | 350 | 9815.187 |
| Development | 750 | 154.912 | 543.563 | 20.020 | 313 | 9945.483 |

Memoria es instantánea final Unity/Mono, no pico/RSS/VRAM. GC gen0 abarca unos 40 s; bytes/frame solo fiables en Development. HUD y arnés incluidos. Normal p95 final frente a la corrección del 08/10: ver JSON originales para los deltas; no atribuir variaciones entre sesiones exclusivamente al cambio de reenganche. Máximos y colas se conservan completos; no se extrapola a equipos modestos.

### Lanzador, integridad y entrega

`scripts\p0.cmd jugar` ejecutado realmente desde CMD el 11/10: salida 0; PID 30908, ventana MAMPORRO respondiente, handle 527136 y ejecutable `unity/Builds/P0/MAMPORRO-P0.exe`. Log confirma progreso `unity/TestResults/P0/ManualSave`. Cerrado normalmente con `CloseMainWindow=True`. Registro `unity/TestResults/P0/A6Cierre/launcher.json` y copia de `manual.log`. No equivale a prueba manual de controles.

U6 SHA-256 antes y después: `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`. Integridad inicial `unity/TestResults/P0/A6Correccion/integrity-resume-before.json`, final `unity/TestResults/P0/A6Cierre/integrity-after.json`. Progreso personal: un archivo encontrado en Progress, SHA idéntico antes/después (`personal-before.json` y `personal-after.json` locales).

Los siete ajustes protegidos conservan estas huellas antes y después, byte a byte; quedan fuera de todos los commits. Cinco modificados y dos no seguidos. Los avisos de espacios finales de `git diff --check` global pertenecen a estos ajustes previos, que no se limpian. La comprobación del índice de entrega se realiza por separado.

| Archivo protegido | SHA-256 antes = después |
| --- | --- |
| `unity/Assets/Mamporro/Generated/RetroPipeline.asset` | `f88523da037cf80db06027f1675ac19cdcc930cee570fa51be80a5945a62ce0d` |
| `unity/Assets/UniversalRenderPipelineGlobalSettings.asset` | `71b8ad93611c4b9c94444409a5cba080b8b7a903ef1e797f1b5cad37d38e2ade` |
| `unity/ProjectSettings/GraphicsSettings.asset` | `35dd86b42c01873514b78c0784ab29078f557769a17097371198cdce6afde5f2` |
| `unity/ProjectSettings/ProjectAuditorSettings.asset` | `9e68444b9bda981e2927a98df9edf69ea88362f0c6b7f3c81b39b9d515d2a416` |
| `unity/ProjectSettings/ProjectSettings.asset` | `4505cd0097fe03d0bef99ba42f8e0742a0660c197cf037634908654a9a1f45db` |
| `unity/ProjectSettings/PackageManagerSettings.asset` | `7698172ab4c40eeb7b992d9255ce6afdf39f9288c1c3a31c58a96a474dc43753` |
| `unity/ProjectSettings/URPProjectSettings.asset` | `68d75e5e1502dd7a4c18b7306430916b5ee8dadfdb0fbb588893fe38eaa2c056` |

### Git y siguiente paso

Publicados mediante push normal, tras fetch y comprobación 4/0, los commits `53658e4`, `bb726db`, `452c956` y `38d29a9`. Último runtime publicado: `38d29a9ced593bc94e3a55638363b70978db822c`; las builds/evidencias corresponden a él. El cierre documental se publica por separado como «Cierra la verificación de A6 para prueba manual»; localizar su SHA con `git log -1 --format=%H -- docs/PROGRESO_P0.md`. Resultado final remoto/local en `unity/TestResults/P0/A6Cierre/git-final.json`. Sin PR ni force-push.

Para probar desde CMD en la raíz del repositorio:

```cmd
scripts\p0.cmd jugar
```

Camina contra las paredes de las cuatro caras del edificio, meseta/rampa y foso; comprueba A/D, pitch de cámara, entrada frontal y rechazo del paralelo, salto y salida completa del foso sin F7. W/S subir/bajar, A/D lateral, Espacio separa; Shift/C permite caer. Build y progreso QA independientes.

**Detenerse aquí para la prueba manual del autor.** No iniciar P0-B/C/D hasta que confirme A/D, todas las paredes apropiadas del circuito y salida de fosos. Limitaciones restantes: esquina sin giro automático, cajas alineadas a ejes y oblicuas mediante terreno; sin importador de meshes arbitrarios ni integración en el mundo U6. Horda/amenaza y refugios mantienen las limitaciones históricas de A4; no se han ampliado ni rebalanceado.

## Refinamiento A6 — Claude Code (11/10/2026, tarde)

### Estado recibido

Tras fetch: HEAD = origin = `38d29a9ced593bc94e3a55638363b70978db822c`, 0/0. Nueve documentos de cierre de Codex sin commit (el apartado anterior decía publicarlos aparte; no se había hecho): publicados intactos en `18dc151` «Cierra la verificación de A6 para prueba manual». U6 y las siete huellas protegidas coincidían con la tabla anterior antes de editar. Sin reset, clean, stash ni force-push.

El autor volvió a describir A/D invertido y paredes no escalables. Las builds del 11/10 17:40 ya incluían las correcciones de Codex (`53658e4` A/D, `bb726db` terreno, `38d29a9` juntas), y el único `manual.log` posterior es el de la comprobación automática del lanzador (17:54). No hay constancia de una sesión manual con esa build: probablemente el informe corresponde a la build del 07/10. No se presupone; se revisaron las dos causas desde cero.

### A/D: verificación y hueco restante

La tangente de Codex `(n.Z, 0, -n.X)` (núcleo, Z reflejada respecto a Unity) es correcta: recalculada como `Cross(up, haciaPared)` en Unity y comprobada contra el mapeo real de WASD en suelo (`U3Game.Update`, `MoveZ=-dir.y`). Pero solo coincide con la pantalla **si la cámara mira hacia la pared**. El ratón sigue girando la cámara durante la escalada: girada más allá del costado (de espaldas a la pared, cámara retraída junto al avatar), la derecha visible es la contraria y A/D vuelven a percibirse invertidos.

Cambio `dc4739c` «Hace que A y D sigan la cámara también de espaldas a la pared»: `VerticalIntent` lleva la derecha horizontal de la cámara (misma guiñada que orienta WASD en suelo). `VerticalMotion.ScreenMirrored` invierte el signo lateral solo si `dot(derechaCámara, WallRight) < -0,25`, y lo restaura al superar `+0,25`; mirando a lo largo de la pared se conserva el sentido (sin parpadeo). Cada agarre parte de la base de pared. Sin cámara (entradas guionizadas del arnés/evidencia) el comportamiento es exactamente el anterior. Con la cámara hacia la pared, idéntico al de Codex. Velocidades 4,5/3,0/9,5 m/s sin cambios; no se ha invertido ningún signo global.

### Paredes: barrido exhaustivo

Causa ya corregida por Codex (consultas sin el Heightfield). Para comprobar que no quedan paredes fuera del contrato, `2ac58bc` añade `VerticalCoverageTests`, que deduce las caras de la geometría del circuito (nunca de IDs): cada cara de caja expuesta que se alza desde suelo transitable más de un escalón, y cada triángulo abrupto del Heightfield, debe engancharse al correr contra ella desde el suelo; follaje, interactuables y límite no. Muestras reales: **68 caras escalables, 24 excluidas, 85 triángulos abruptos** (meseta, rampa, esquinas oblicuas del terreno). Todas correctas; no apareció ninguna pared omitida.

### Pruebas ejecutadas

| Comando (CMD, raíz) | Resultado |
| --- | --- |
| `scripts\u3.cmd edit` (línea base) | 542/542 |
| `scripts\u3.cmd edit` (final) | **555/555** |
| `scripts\u3.cmd play` | **50/50** |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` | U0/U2/U3/U4 coinciden; 8 guardados y 6 secuencias meta; `qa-results/reference-c1fcb238836d403280d4ee01660894ef` |
| `scripts\p0.cmd build` / `devbuild` | correctas; ambas `dc4739c2517f206496876527afeac8b33f2b79b7` |
| `scripts\p0.cmd evidence` | válida: `unity/TestResults/P0/Evidence/20261011T193637`, 41 PNG / 15 CSV |
| `scripts\p0.cmd jugar` | salida 0, ventana MAMPORRO respondiendo, `unity/Builds/P0/MAMPORRO-P0.exe`, progreso `unity/TestResults/P0/ManualSave`; cerrada con `CloseMainWindow` |

XML/log en `unity/TestResults/P0/A6Claude`. Nuevas pruebas (13 Edit + 1 Play): A/D según cámara en ±X/±Z con cámara hacia/de espaldas a la pared; histéresis y banda a lo largo de la pared; barrido de cámara 0–540° con D siempre a la derecha visible fuera de la banda y exactamente dos cambios; agarre nuevo desde base de pared; barridos de cajas y terreno; roce/paralelo en las cuatro caras del edificio; límite invisible. Play: teclado real A/D en cuatro paredes con giros 0/±45/±135/180°, oráculo con la derecha de la cámara Unity.

**Mutaciones comprobadas** (temporales, revertidas antes de commit; restaurado verificado con diff): quitar el terreno de `Wall()` hace fallar el barrido de terreno y las pruebas de Codex; desactivar la inversión por cámara hace fallar las cuatro caras de espaldas y el barrido a 195°. Las pruebas nuevas detectan ambas regresiones.

Primera pasada de evidencia `20261011T193440` **no válida**: en la pared +Z el agarre se soltó a 0,9 m sin fallo de escalada y no se reenganchó. Mecanismo: pérdida de foco de Windows → `SetPaused(true)` → `Suspend()` suelta el agarre; `P0Capture` reanuda (línea 81) pero ese fixture solo envía entrada vertical. Repetida sin cambios de código: válida, 93 ticks idénticos a la pasada de Codex. Se conserva la no válida como registro.

Rendimiento: no se repiten benchmarks. El cambio añade un producto escalar por tick solo durante la escalada y no toca enemigos, director ni colisiones; las medidas de `38d29a9` (sección anterior) siguen siendo la referencia. No son FPS generales del juego.

### Integridad

U6 SHA-256 antes y después `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`; siete ajustes protegidos con las mismas huellas de la tabla anterior, fuera de todos los commits (`unity/TestResults/P0/A6Claude/integrity-after.json`). Sin cambios en `src/` ni en `unity/Docs/Reference/`.

### Prueba manual pendiente

`scripts\p0.cmd jugar`. Casos: (1) corre de frente contra cada cara del edificio, meseta, rampa, acantilado y foso: debe engancharse sin saltar ni pulsar nada; (2) con la cámara hacia la pared, A izquierda y D derecha en pantalla; (3) gira el ratón mientras escalas hasta mirar de espaldas a la pared: A/D siguen yendo a izquierda/derecha de la pantalla; (4) corre en paralelo pegado a una pared: no engancha; (5) Espacio separa y no reengancha de inmediato; (6) cae al foso y sal por cualquier lado sin F7; (7) llega arriba del edificio por una cara sin techo encima. Límites conocidos sin cambios: esquina exterior suelta el agarre (sin giro automático), cajas alineadas a ejes, oblicuas solo en terreno, sin escalada en U6. **Detenerse; no iniciar P0-B/C/D.**

## Velocidad de escalada = velocidad efectiva (11/10/2026, noche, Claude Code)

Partida: HEAD = origin = `4b2fbc7`, 0/0, solo los siete ajustes protegidos sin commit. El autor probó A6 manualmente: todo correcto salvo la velocidad. Decisión confirmada: escalar a la velocidad efectiva de movimiento en todas las direcciones, mejoras incluidas.

### Fuente y fórmula

`WorldRun.Step` ya pasa a la física `Tuning.PlayerBaseMoveSpeed * Combat.Stats[Stat.moveSpeed]` (9,5 m/s × estadística de tomos, objetos y santuarios, tope ×1,8) y `Combat.CrowdSlow`; la carrera aplica `moveSpeed·(1−crowdSlow)` en `StepInCrowd`. Esa expresión se extrae a `PlayerPhysics.EffectiveSpeed` (resultado idéntico para la carrera) y la escalada la usa cada tick:

`velocidad de escalada = PlayerPhysics.EffectiveSpeed(9,5 · Stats[moveSpeed], CrowdSlow)`

No es la velocidad horizontal instantánea (nula contra la pared) ni una copia de la estadística: una mejora aplicada o retirada cambia el siguiente tick. Se retiran `UpSpeed` 4,5, `SideSpeed` 3,0 y `LedgeSpeed` 4,5; no hay estadísticas, mejoras ni balance nuevos. La frenada de horda afecta igual que en la carrera.

Desplazamiento por tick: `velocidad · dt · (v·t_arriba + h·t_derecha) / max(1, |(v,h)|)`. `t_derecha` es la base A/D de `dc4739c` (cámara incluida); `t_arriba` (`VerticalMotion.WallUp`) es la tangente ascendente de la cara: vertical en muros, inclinada en paredes del terreno. W/S/A/D tienen el mismo módulo y las diagonales no lo superan, también en pendiente (antes la subida vertical se mantenía y el módulo total crecía). La transición al tejado recorre sus dos tramos barridos a la misma velocidad.

### Seguridad a velocidad alta

Hasta 17,1 m/s (tope ×1,8) un tick avanza 0,285 m. El barrido ya era continuo (sin atravesar), pero la ventana de borde de 0,2 m y el final de caras inclinadas sí podían saltarse: en pruebas, a 9,5 m/s ya fallaban la salida de la rampa y de acantilados oblicuos («fin de superficie»). Corrección: el recorrido del tick se divide en tramos de como mucho 0,075 m (`MaxClimbSubstep`, el paso aprobado a 4,5 m/s) y cada tramo repite comprobación de borde, barrido y contacto, igual que la versión probada. Al bajar, el último tramo se detiene en el suelo (antes podía hundirse hasta un paso y recolocarse). Reenganche: los 0,35 s son tiempo, independientes de la velocidad.

### Pruebas ejecutadas

| Comando (CMD, raíz) | Resultado |
| --- | --- |
| `scripts\u3.cmd edit` | **583/583** (antes 555) |
| `scripts\u3.cmd play` | **51/51** (antes 50) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` | U0/U2/U3/U4 coinciden; 8 guardados y 6 secuencias meta |
| `scripts\p0.cmd build` / `devbuild` | correctas; ambas `373620d7b83bc57cdc4540ef8b734e7c58b9dd97` |
| `scripts\p0.cmd evidence` | válida, `unity/TestResults/P0/Evidence/20261011T203110`, 41 PNG / 15 CSV |
| `scripts\p0.cmd benchmark` | válido 0/300/500/750, `unity/TestResults/P0/Normal/20261011T203140` |
| `scripts\p0.cmd jugar` | salida 0, ventana respondiendo, `unity/Builds/P0/MAMPORRO-P0.exe`, progreso `ManualSave`; cerrada normalmente |

XML/log en `unity/TestResults/P0/A6Velocidad`. `VerticalSpeedTests` (nuevo): módulo en W/S/A/D y cuatro diagonales con estadística 1,0/1,26/1,5/1,8 y horda 0/0,15/0,3; igualdad con la velocidad estable de carrera real de `PlayerPhysics` con la misma estadística; mejora aplicada, ampliada y retirada en plena escalada; cadena de `PlayerStats` con tomos hasta el tope; tejado desde 14 fases de tick a 1,0/1,4/1,8 sin penetración; foso por las cuatro caras, meseta, rampa y extremo de meseta a 17,1 m/s; techo y esquina interior sin penetración; junta entre cajas; descenso rápido sin hundirse; protección de salto; A/D con cámara hacia/de espaldas a 17,1 m/s. Play: tick real de `WorldRun.Step` con `Stats[moveSpeed]` 1,0→1,26→1,8, horda 0,25 y retirada a 1,0.

Las pruebas de A/D, diagonal y pared inclinada se actualizaron al nuevo módulo (decisión nueva, no expectativas históricas). Dos pruebas de dirección que recorren una cara acotada (junta entre cajas y barrido de cámara 0–540°) pasan una velocidad explícita de 3 m/s para seguir probando la junta y la cámara, no la velocidad. Mutación comprobada: sin tramos fallan 6 pruebas (tejado a 1,4/1,8, rampa, acantilados oblicuos); restaurado.

Benchmark Normal (lógica y frame del arnés, no FPS generales del juego; mismo equipo y condiciones que la sección de rendimiento A6, una pasada por carga): bordes 14/13/13/12 en 30 s (antes 9, por escalar más rápido), cero fallos de borde, atascos, rescates y agarres/reenganches indebidos. CPU de tick p50 0,019/0,575/1,061/1,660 ms; p95 0,022/0,645/1,200/1,795 ms. Con 0 enemigos +0,006 ms p50 por los tramos y consultas; con horda, dentro de la variación previa. Frame p95 0,675/0,840/0,977/1,125 ms; máximos aislados 5,0/4,2/11,6/14,1 ms, no atribuibles exclusivamente al cambio.

### Integridad y prueba manual

U6 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873` antes y después; siete ajustes protegidos con las huellas de la tabla A6, fuera de commits (`unity/TestResults/P0/A6Velocidad/integrity-after.json`). Sin cambios en `src/` ni en `unity/Docs/Reference/`.

Prueba: `scripts\p0.cmd jugar`. Escalar W/S/A/D y diagonales a la misma rapidez que corriendo; tejado, meseta, rampa y foso sin atascos; con F4 la horda frena igual que en carrera. El circuito QA no ofrece mejoras de velocidad durante la partida; la integración con la estadística está cubierta por las pruebas automáticas. **Detenerse; no iniciar P0-B/C/D.**
