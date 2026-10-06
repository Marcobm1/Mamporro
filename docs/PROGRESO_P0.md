# P0 — movilidad y verticalidad antes de ampliar el mundo

Estado: **P0-A1–P0-A5 AUTORIZADOS Y EN CURSO**. Preparación del 05/10/2026 (Codex). No mezcla P1 artístico ni P2 contenido. La clasificación histórica de balance en P0 no lo incluye en este primer prototipo.

## Cómo retomar

- Cierre formal B0 publicado: `0b0b68c31dda803cf9cf566b3c96f369ed893ce3`, «Aprueba B0 y fija el pipeline 3D», sobre cierre técnico `ab9e619`. Pipeline aceptado para proponer adopción; no hubo revisión manual de la build por el autor. D/VAT candidata, C fallback; prototipos técnicos, no arte final.
- U0–U6 cerrados y aprobados. Entrega `unity/Builds/Windows/MAMPORRO.exe` intacta; SHA-256 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`.
- Trabajo publicado: A1 `508aa0f849eb536068359a90a6fc883a117d5937` y A2 `303b01c2a09a2d6bccb3f742f3f20ad5d8c97fe3`. Al retomar el 06/10 había WIP runtime A3 y lanzadores/build P0 sin publicar; conservado y revisado.
- Siete ajustes Unity protegidos intactos y excluidos, según BLENDER_B0/PROGRESO_U6. No reset/clean/stash ni publicación accidental.
- **Siguiente paso exacto:** A5, build QA, medición/evidencias y cierre para prueba manual. A3 `beb32093cd56b59bba2223199a287bea13a2c6ab` publicado; A4 probado (464 Edit, 45 Play). Lanzadores y constructor de build heredados siguen locales y sin publicar hasta probarlos. Las cuatro decisiones están aprobadas; no volver a preguntarlas. P0-B/C/D no autorizados.
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
