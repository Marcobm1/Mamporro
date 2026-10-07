# P0 — movilidad y verticalidad antes de ampliar el mundo

Estado: **P0-A1–A5 base técnica validada manualmente; P0-A6 EN CURSO** (07/10/2026, Codex). El autor probó P0-A y señaló que técnicamente funciona bien; solicitó A6 antes de P0-B. La movilidad no se declara definitivamente cerrada hasta su prueba manual de A6. P0-B/C/D NO autorizados. No mezcla P1 artístico ni P2 contenido. La clasificación histórica de balance en P0 no lo incluye en este prototipo.

## Cómo retomar

- **07/10, A6 activo:** HEAD/remoto iniciales `ccd45c0c01e127e7d96f1fa87b52174133ae416c`, 0/0 tras fetch. WIP recibido en VerticalMotion, VerticalCircuit, sus tests, VerticalSceneTests, VerticalQa y P0Capture: conservado y revisado; no estaba registrado en el cierre A5.
- U6 y las siete huellas protegidas coinciden exactamente con la tabla de integridad inferior antes de editar.
- Siguiente paso vigente: completar/verificar A6 (automática, reenganche por superficie, foso, integración/evidencia/medidas/build); detenerse para prueba manual del autor. Las instrucciones de agarre con botón del historial A1–A5 quedan sustituidas por A6.


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
