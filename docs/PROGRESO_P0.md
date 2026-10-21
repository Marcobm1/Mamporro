# P0 — movilidad y verticalidad antes de ampliar el mundo

Estado vigente: **P0-A/B/C cerrados y aprobados manualmente. P0-C aprobado el 12/10/2026. P0-D0–D4 autorizados el 12/10/2026; detenerse antes de D5.** Presupuesto conservado: 3000 nodos/tick.

<details>
<summary>Estado histórico de A6, sustituido por los cierres posteriores</summary>

Estado: **P0-A1–A5 base técnica validada manualmente; P0-A6 corregido y verificado, pendiente de prueba manual** (11/10/2026, Codex; refinado por Claude Code el mismo día en `dc4739c`). **Actualización 11/10 noche:** el autor probó A6 y aprueba todo salvo la velocidad; escalada a velocidad efectiva de carrera en `373620d`. **Cierre 11/10: P0-A6 aprobado manualmente por el autor («Todo bien»), velocidad incluida. P0-A cerrado. Siguiente bloque: P0-B, solo plan hasta su aprobación.** La movilidad no se declara aceptada hasta la nueva prueba del autor. P0-B/C/D NO autorizados. No mezcla P1 artístico ni P2 contenido. La clasificación histórica de balance en P0 no lo incluye en este prototipo.

</details>

## Cómo retomar

- **H0–H1 autorizado, 21/10/2026, en curso:** base HEAD/origin `563f05e`, solo siete ajustes Unity, 9/9 protecciones y 11/11 referencias intactas; evidencias en `unity/TestResults/P0/H1-Hybrid-20261021`. [Contrato H0](EXPERIMENTO_H1_P0D3.md) fijado antes de medir. Aprobadas decisiones experimentales descritas en DECISIONES; alcance comprobado solo H0–H1. Siguiente: implementar y evaluar topología/rutas offline; detenerse ante fallo y siempre antes de H2. Sin resultados nuevos todavía, ni integración/H2–H4/D4/D5.

- **Plan híbrido, 21/10/2026, posterior a `9306e38`: solo documentación autorizada y preparada.** Git real HEAD/origin `9306e38` coincidentes tras fetch; recibidos únicamente los siete ajustes Unity protegidos, conservados. Leídos AGENTS/CLAUDE, plan, decisiones, checkpoints e informes D3. Sin investigación avanzada ni código nuevo.
  - [PLAN_HIBRIDO_P0D3](PLAN_HIBRIDO_P0D3.md): tres alternativas; recomendación pendiente de componentes dirigidas/corredores compartidos con refinamiento cercano. Contiene contratos físicos, bandas con histéresis, jefe, cuotas con techo 3000, versiones, atasco, memoria/preparación, fases H0–H4 y pruebas propuestas 300/500/750 con persecución física y FPS reales en build pequeña separada.
  - **No aprobados:** arquitectura nueva, optimalidad aproximada/tolerancia de rodeos, bandas/plazos, cuotas y fases. Objetivos 0,3/0,5 s anteriores siguen vigentes. Recomendación para siguiente autorización: H0–H1 primero; no ejecutarlos todavía. Sin EnemyNavigation, D4/D5, ampliación, balance, web o referencias.
  - Verificación de esta sesión: estado/log/remoto/fetch y HEAD/origin; lectura de informes; SHA-256 **9/9** protecciones y **11/11** corpus iguales al cierre de landmarks; **169 enlaces Markdown locales válidos**, revisión documental y `git diff --check` de documentos sin errores. Diff frente a `9306e38` vacío en src, referencias, Core, Tests, U3 y scripts. **Sin suites, benchmark, build, FPS ni nuevas medidas de navegación**: todas las cifras de experimentos anteriores son heredadas. No confundir pruebas diseñadas con ejecutadas.
  - Plan publicado en **`995df30`**, «Propone el rediseño híbrido de navegación P0-D3», push normal confirmado desde `9306e38`. Cierre documental separado «Actualiza el relevo de D3 a la espera de aprobar el plan híbrido», identificable con `git log -1 --format="%H %s" -- docs/PROGRESO_P0.md`; comprobar HEAD/origin después del push normal. Quedan fuera los cinco ajustes Unity modificados y dos no seguidos; no incluirlos ni restaurarlos.
  - **Siguiente paso exacto:** el autor revisa la tabla de decisiones de la sección 10 del plan y aprueba/corrige arquitectura, bandas, calidad y alcance de fases. Detenerse; no inferir autorización de implementación por publicar este plan ni por permisos históricos D0–D4.

- **Revalidación y cierre de landmarks, 21/10/2026:** rama `claude/zen-pasteur-674ik0`, HEAD/origin `27414c5` coincidentes tras fetch. Recibidos seis archivos nuevos ya en índice, seis documentos modificados y siete ajustes Unity; preservados y revisados. No cambios funcionales al instrumento heredado. **Puerta 1 negativa confirmada; Puerta 2 no iniciada; detenerse para revisión.**
  - Prueba nueva: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.LandmarkGateTests`, **12/12**, 48,326 s, XML/log `unity/TestResults/P0/Regional-20261021T174638/edit.*`. Intento `174624` sin pruebas por licencia 198; repetición fuera del sandbox. Incluye 9 mediciones y 3 controles dirigidos S/M/L.
  - Extractor nuevo: `python scripts/p0-d3-landmark-report.py unity/TestResults/P0/Regional-20261021T174638/edit.xml unity/TestResults/P0/D3-Landmarks-20261021/gate.json`. Comparación de los campos deterministas contra el día 20: **27/27 preparaciones, 18/18 comparaciones, 18/18 cohortes idénticas**. K=8 reduce 19,51× extracciones y 8,32× trabajo frente a Dijkstra individual, pero 750 dispersos suman **9889018** unidades frente a **1992760 heredadas** del servicio compartido. Mejor concentrado K=4: **651465**, frente a **57764 heredadas**.
  - Preparación completa a 64 m K=2/4/8 **3,697–3,716 / 3,782–3,807 / 3,954–3,986 s**. Payload del instrumento **7,202/8,752/11,852 MiB**; memoria residente/picos/GC no certificados. [Informe y límites](EXPERIMENTO_LANDMARKS_P0D3.md). Medición offline, sin FPS ni latencias de servicio. No cargas nuevas 300/500/750 con objetivo móvil: dependen de Puerta 2 y esta no se abre.
  - Protecciones nuevas antes/después **9/9**, corpus **11/11**, JSON en `D3-Landmarks-20261021`. Core, U3, src y referencias sin diferencias frente a `27414c5`; siete ajustes Unity conservados fuera de commits. Sin Edit completo, Play, build ni verificación histórica ejecutada nuevamente; evidencias anteriores identificadas como heredadas.
  - Instrumento/informe publicado en **`2214b87`**, «Evalúa A* con landmarks dirigidos y registra la puerta negativa», push normal confirmado desde `27414c5`. Cierre documental separado «Cierra la revalidación de landmarks y deja D3 detenido», localizable con `git log -1 --format="%H %s" -- docs/PROGRESO_P0.md`; comprobar HEAD/origin tras su push. **Siguiente paso exacto: revisión del autor.** No integración, nueva arquitectura, D4/D5 ni ampliación; 3000 globales/tick, reacción 0,3/0,5 s y protecciones intactos. La parada aplica por resultado negativo, no por falta de autorización de las dos puertas.

- **Landmarks dirigidos, 20/10/2026 (Codex): Puerta 1 negativa; Puerta 2 no iniciada.** Base real `27414c5`, HEAD/origin coincidentes tras fetch; siete ajustes Unity recibidos, nueve huellas iguales al cierre anterior. Nueva autorización del autor ejecutada sin investigación avanzada. [Informe, prueba matemática, comparaciones y límites](EXPERIMENTO_LANDMARKS_P0D3.md).
  - Solo código de tests: `LandmarkGateProbe` y `LandmarkGateTests`; extractor `scripts/p0-d3-landmark-report.py`. Tablas exactas desde/hacia 2/4/8 referencias por S/M/L, selección determinista, certificados de distancias, A* con reapertura y términos dirigidos finitos. No se modifica Core, scheduler, navegación activa, generador, RNG, balance, persistencia ni web.
  - Preparación **nueva 9/9**, 45,476 s, `Regional-20261020T175249/edit.xml`: HORDA750 × 16/32/64 × tres repeticiones, 27 preparaciones independientes de landmarks. Primera ejecución `175240` sin pruebas por licencia 198; repetición fuera del sandbox. Preparación completa incluye circuito/generación/validación/grafos existentes, D2, selección, índices, tablas y certificación. A 64 m: **3,598–3,663 / 3,679–3,746 / 3,862–3,905 s** con 2/4/8 referencias. No certificar p95 de producción con tres muestras.
  - 1440 consultas A* frente a 1440 Dijkstra individual, más 4500 A* de cohorte; costes contra monolítico y rutas A* comprobadas. Muestras de metas inicio/vecino/recorrido/altura, no servicio móvil continuo. Con 8 referencias y dispersión: extracciones **19,51× menores**, trabajo **8,32× menor** que Dijkstra individual. Pero cohorte 750 vecino **9889018 unidades**, frente a **1992760 heredadas** de demanda compartida; mejor concentrado K=4 **651465**, frente a **57764 heredadas**. La comparación conserva inputs, distingue algoritmos/contadores y no rehace el experimento anterior.
  - **Parada por mejora agregada insuficiente**, no por afirmar que las tablas no caben: payload instrumento 7,202/8,752/11,852 MiB; muestras de memoria y recuentos parciales en informe, sin certificación de picos/residencia completa ni cero GC. No caché de rutas/servicio nuevo, ni integración, D4/D5 o mundo mayor. Presupuesto 3000 y reacción 0,3/0,5 s intactos. No simular latencias a partir del trabajo offline.
  - Controles **nuevos 3/3**, 1,942 s, `Regional-20261020T175616/edit.xml`: caída unilateral, capas inaccesibles, cotas/costes/rutas para S/M/L y 2/4/8. Comandos exactos y extractor en informe; evidencias `unity/TestResults/P0/D3-Landmarks-20261020`. Categorías `LandmarkGate` y `LandmarkCorrectness` con lanzador regional. Las 71/71 de transferencias y las cargas anteriores siguen siendo heredadas.
  - Referencias nuevas con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes, instantánea `qa-results/reference-9bc6ebeabda64d769e969a4960fd9349`. No Edit completo, Play ni build nuevos (producción intacta). Huellas antes/después **9/9**, corpus **11/11**, JSON y hashes de fuentes/evidencias en `D3-Landmarks-20261020`. Diffs protegidos frente a `27414c5` vacíos en src, referencias, Core y U3; comprobación de espacios de archivos propios sin errores. Siete ajustes Unity fuera de commits, cinco modificados y dos no seguidos.
  - **Siguiente paso exacto: revisión del autor antes de cualquier integración o nuevo experimento.** Recomendación: no abrir Puerta 2 con estas cifras; si se decide otro experimento, demostrar primero compartición exacta de trabajo para cohortes y metas nuevas. No implementada ni autorizada automáticamente por este cierre. No solicitar reautorización de ambas puertas como si faltara permiso: se ha activado la condición expresa de parada de Puerta 1.

- **Preprocesado exacto, 20/10/2026 (Codex): puerta inicial negativa.** Base real `f355409`, rama/remoto comprobados con fetch; siete ajustes locales Unity y nueve protecciones recibidos intactos. Autorización nueva del autor ejecutada como diagnóstico offline, sin investigación avanzada. [Informe, fórmulas y alternativas](EXPERIMENTO_PREPROCESADO_P0D3.md).
  - Nuevo únicamente `RegionalPreprocessFeasibilityTests` y extractor `scripts/p0-d3-preprocess-report.py`. No se modifica Core ni se crea un solver: reutiliza transferencias inversas regionales existentes, conserva aristas dirigidas, alturas, costes, S/M/L y Walk/Climb/Drop. Matrices calculadas sin reservarlas.
  - Puerta **nueva 9/9**, 50,807 s, `Regional-20261020T170821/edit.xml`: HORDA750 × 16/32/64 × tres repeticiones, tres clases por caso. Construcción + todas las transferencias: **4,443–4,492 / 4,413–4,481 / 4,862–4,967 s** respectivamente. Todas superan el objetivo de preparación de 4 s, aun sin guardar tablas ni validación; ninguna supera 6 s. No convertir tres repeticiones en certificación estadística ni estos tiempos de Editor en build.
  - Memoria calculada: matriz mundial de distancias **13,80 GiB**, descartada. Entrada×salida regional **3,946/3,324/1,490 MiB**, nodo×salida **30,334/53,779/71,749 MiB**, solo distancias; las regionales no se descartan todas por memoria, pero falla la puerta temporal. Barrido denso por nueva meta: **1070197/887801/396551 entradas**, modelo estructural, no latencia medida ni cota inferior de cualquier consulta exacta.
  - Evidencia local `unity/TestResults/P0/D3-Preprocess-20261020/gate.json`; extractor comprueba 27 recuentos y nueve agregados, sin diferencias deterministas. El primer JSON conserva los mismos resultados antes de añadir columnas derivadas.
  - **Parada conforme a la condición del autor:** no prototipo de servicio, integración, D4/D5 ni ampliación. No se ejecutan equivalencia A/B/ocho C ni carga 300/500/750 para un servicio inexistente; latencias, cobertura, pendientes y CPU de ese servicio N/D. La equivalencia/carga de demanda del 19/10 siguen siendo heredadas. Objetivos 0,3/0,5 s y 3000 intactos.
  - Regresión **nueva 71/71**, 275,949 s: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalTransferTests`, XML/log `Regional-20261020T171040`. Comprueba el kernel existente sobre A/B/ocho C × 16/32/64 y controles, no un servicio nuevo. Referencias históricas nuevas con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes, instantánea `qa-results/reference-168903c041f0481c9f8da344236af827`. No Edit completo, Play ni build nuevos: no cambia producción.
  - Huellas antes/después **9/9**, corpus **11/11**, JSON y hashes de fuentes/XML en `D3-Preprocess-20261020`. Diff protegido frente a `f355409` vacío en src, referencias, Core y U3. Se conservan fuera de commits los cinco ajustes modificados y dos no seguidos de Unity. `git diff --check` general detecta espacios heredados de esos ajustes; no se limpian. Comprobación de los archivos propios sin errores.
  - Publicación: **`00dffae`**, «Evalúa la viabilidad del preprocesado regional exacto», push normal confirmado desde `f355409`. Cierre documental separado **«Documenta la parada inicial del preprocesado D3»**, localizable con `git log -1 --format="%H %s" -- docs/PROGRESO_P0.md`; comprobar HEAD/origin al retomar. Comandos reproducibles y limitaciones en el informe. Sin PR, force-push ni operaciones destructivas.
  - **Siguiente paso exacto: detenerse para revisión del autor.** Recomendación: evaluar en otro experimento consultas exactas con pocas cotas admisibles dirigidas, previa autorización. Ni esa alternativa ni un kernel especializado, persistencia de tablas o nueva arquitectura están implementados/autorizados por este cierre. No integrar ni seguir afinando los candidatos fallidos.

- **Cierre de demanda exacta, 19/10/2026:** publicados con push normal **`3aaac6f`** (servicio y pruebas) y **`2940d9b`** (carga e informe), desde `599beec`, misma rama. **Resultado negativo para D3; parada para revisión del autor.** [Contrato, tablas, comparación y alternativas](EXPERIMENTO_DEMANDA_P0D3.md). No integrar ni continuar afinando este candidato, no iniciar D4/D5 ni ampliar mundo. Las notas siguientes de «en curso» son historial.
  - Implementado únicamente `DemandNavService`: consultas exactas compartidas por clase/meta, dos entradas de caché, pausa sin usuarios, FIFO, solicitudes admitidas sin cancelación por meta móvil, estados pendiente/alcanzable/inalcanzable y tokens de revisión/generación. Todo trabajo de índice, inicialización, búsqueda y mantenimiento contado con scheduler existente; **3000 globales/tick y objetivos 0,3/0,5 s intactos**. Sin referencias desde EnemyNavigation ni QA activo.
  - Pruebas **nuevas del 19/10**: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.DemandNavTests -Category DemandEquivalence`, **35/35**, 141,137 s, XML/log `Regional-20261019T175306`; primera 34/34 en `174735`, intento `174726` sin pruebas por licencia 198. Corpus A/B/ocho C × 16/32/64 × S/M/L, oráculos D2/monolítico, rutas físicas, capas, restricciones de cuerpo, invalidación, determinismo, cola, caché y ausencia de cancelación de solicitud admitida.
  - Carga nueva: mismo lanzador con `-Filter Mamporro.Tests.DemandNavLoadTests -Category DemandLoad`, **3/3 por repetición**, `Regional-20261019T180015` (33,925 s), `180059` (35,092 s), `180142` (34,028 s). Cada repetición: 300/500/750 concentrados/dispersos × fijo/alternancia/recorrido × 16/32/64, 180 s simulados y 18 cambios vecinos adicionales. Primera carga `175626` conservada aparte; tenía salto de cierre del recorrido, corregido a ida/vuelta por vecinos antes de las tres medidas finales. **Sin cambios al solver para favorecer el resultado.**
  - `python scripts/p0-d3-demand-report.py unity/TestResults/P0/D3-Demand-20261019/load-final.json unity/TestResults/P0/Regional-20261019T180015/edit.xml unity/TestResults/P0/Regional-20261019T180059/edit.xml unity/TestResults/P0/Regional-20261019T180142/edit.xml`: **486 filas por clase, 162 totales y 54 vecinos**, categorías de trabajo y techo comprobados, ninguna discrepancia determinista entre repeticiones. Contadores/latencias/edad/cobertura/pendientes, CPU por clase/global y payload conservados. Es simulación de demandas con orígenes fijos, no persecución ni FPS.
  - Mejora localizada: vecino caliente con 750 concentrados **57764 unidades / 20 ticks**, p95 S/M/L **0,300/0,250/0,033 s**; frente a 1101086 unidades del lote, pero solo cubre consumidores cercanos. Dispersos: **1992760 unidades / 665 ticks**, p95 **10,800/9,350/0,300 s**. Recorrido móvil 750 dispersos: p95 **85,017/102,883/0,633 s**, cobertura vigente **0,429/0,222/2,722 %**, **750 pendientes al cierre**. Todos los ticks móviles consumen 3000; no se ocultan solicitudes atrasadas con el buen p95 de alternar dos metas cacheadas. Arranque frío también incumple. Payload total con 750 **10482896 B**, parcial; asignaciones N/D en 162 escenarios; residencia exclusiva no certificada.
  - `scripts\u3.cmd play`: **53/53**, 113,005 s; XML/log archivados en `unity/TestResults/P0/D3-Demand-20261019/play.*`. `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes, 8 guardados y 6 secuencias, instantánea `qa-results/reference-96f18c8e2ab840dcacbf8b10148ae889`. **No se repitió Edit completo**: 960/960 anterior sigue siendo evidencia heredada, no atribuirla a esta sesión. Sin build nueva.
  - Protección antes/después **9/9** (U6, u3-world y siete ajustes), corpus completo **11/11**, JSON en `D3-Demand-20261019`; hashes de fuentes y evidencias también archivados. Diffs protegidos frente a `599beec` vacíos. No se tocaron backend, generador, geometría, economía, RNG, persistencia ni src. Quedan fuera de commits exactamente los siete ajustes Unity originales (cinco modificados y dos no seguidos), sin restaurarlos ni borrarlos.
  - Cierre documental separado: **«Documenta la parada de D3 tras las consultas bajo demanda»**, localizable con `git log -1 --format="%H %s" -- docs/PROGRESO_P0.md`; push normal y confirmar HEAD/origin al terminar. **Siguiente paso exacto: esperar revisión del autor**, que elija si estudiar preprocesado exacto reutilizable con límites iniciales de carga/memoria u otro experimento acotado. Heurísticas admisibles/cohortes son alternativas propuestas, no implementadas ni autorizadas aquí. No nueva arquitectura, integración, D4/D5, relajación de objetivos o presupuesto por iniciativa propia.

- **Demanda: exactitud verificada, 19/10/2026:** específicas **35/35**, 141,137 s (`Regional-20261019T175306`), tras primera 34/34, 140,223 s (`174735`). Incluye solicitud admitida sin cancelación, pausa sin consumidores, caché, invalidación y FIFO determinista. Primera carga **3/3**, 33,667 s (`175626`), 54 escenarios y 18 cambios vecinos; informe local `D3-Demand-20261019/load.json`. Vecino caliente 750: concentrado 57764 unidades y 20 ticks, disperso 1992760 y 665 ticks; no acredita servicio móvil. **Resultado móvil preliminar negativo**; repetir arnés con recorrido de ida/vuelta por vecinos, eliminando el salto de cierre de la primera trayectoria, antes del veredicto final. Play en ejecución; referencias nuevas coincidentes (`qa-results/reference-96f18c8e2ab840dcacbf8b10148ae889`). Sin integración ni D4/D5; cierre/publicación pendientes.

- **Consultas exactas bajo demanda autorizadas, 19/10/2026, en curso:** HEAD/origin `599beec` comprobados; solo siete ajustes Unity recibidos, huellas 9/9 iguales y corpus protegido en `unity/TestResults/P0/D3-Demand-20261019/*-before.json`. Nuevo prototipo aislado `DemandNavService`: búsqueda inversa compartida por clase/meta, pausa sin usuarios, caché de dos metas, cola FIFO sin cancelar por cambios de objetivo, tres estados y tokens de revisión/generación para rutas. Índice inverso e inicialización perezosa cobrados, scheduler existente 3000. Nueva suite `DemandNavTests` en ejecución (`Regional-20261019T174735`); primer intento `174726` sin pruebas por licencia 198. **No hay resultados certificados ni commits nuevos todavía.** Siguiente: resolver equivalencia antes de medir 300/500/750 concentrados/dispersos, fijo/móvil, 180 s; después regresiones/referencias/publicación y parada. Sin movimiento, D4/D5, generador, economía ni web; autorización nueva sustituye la espera de decisión del cierre inferior.

- **Cierre del experimento de lotes, 19/10/2026:** código **`9922599` publicado con push normal** sobre `ea5d81b`, rama `claude/zen-pasteur-674ik0`. Se recuperaron y conservaron los seis documentos modificados, informe, solver, agrupación, suite y extractor locales del 18/10, además de los siete ajustes Unity. No se rehízo el experimento ni se integró en movimiento. **Resultado negativo por rendimiento; D3 bloqueado, D4/D5 no iniciados. Detenerse para revisión del autor.** [Informe, comparación y alternativas](EXPERIMENTO_LOTES_P0D3.md).
  - Evidencia **heredada comprobada**, no pruebas nuevas del 19/10: específicas A 30/30, B 33/33, C 3/3 con CPU por clase (`Regional-20261018T195506`); `scripts\u3.cmd edit` sí había terminado el 18/10: **960/960**, 1630,505 s, con 66/66 de `RegionalBatchTests`. XML/log archivados sin sustituir originales en `unity/TestResults/P0/D3-Batch-Cierre-20261019/edit.*`. Los fuentes locales son anteriores a esa ejecución y se publican sin cambios funcionales.
  - Pruebas **nuevas del 19/10**: `scripts\u3.cmd play`, **53/53**, 112,543 s, XML/log en `D3-Batch-Cierre-20261019/play.*`; `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`, U0/U2/U3/U4 coincidentes (8 guardados y 6 secuencias), instantánea `qa-results/reference-bccc556717e949f5b89a1af359e32a15`. `python scripts/p0-d3-batch-report.py unity/TestResults/P0/D3-Batch-Cierre-20261019/inherited-edit-summary.json unity/TestResults/P0/D3-Batch-Cierre-20261019/edit.xml`: 90 registros de grupos, 162 por clase/modo, 54 totales y 4536 regionales. Comparación de las 162 filas con `budget-specific.json`: ninguna discrepancia determinista; CPU/setup excluidos de esa igualdad.
  - Resultado: mejor vecino agregado **1101086 unidades**, hasta **6,133 s** a 64 m; móvil p95 S/M/L **7,833/6,750/3,700 s**, mejora de ~3,58/3,57/4,41 veces frente al comparador incremental, todavía lejos de 0,3/0,5 s. Publicaciones móviles 23/26/47 en 180 s simulados. CPU/payload y límites en el informe; asignaciones N/D, sin FPS ni persecución física certificada. **3000 unidades globales/tick y objetivos intactos.**
  - Protección nueva antes/después: **9/9** (U6, u3-world y siete ajustes), más **11/11** archivos de `unity/Docs/Reference`; JSON `integrity-before/after.json` y `references-before/after.json` en el directorio de cierre. Diff frente a `ea5d81b` vacío en src, referencias y código Core preexistente. Sin build, cambios de backend, generador, balance, RNG o guardados. Solo los siete ajustes protegidos deben quedar locales tras publicar la documentación; no borrarlos ni incluirlos.
  - Publicación documental separada: **«Cierra el experimento D3 por lotes y documenta sus límites»**, identificable con `git log -1 --format="%H %s" -- docs/PROGRESO_P0.md`; comprobar HEAD/origin con los comandos inferiores. **Siguiente paso exacto: revisión del autor y elección de otro experimento acotado** entre consultas exactas bajo demanda o preprocesado regional reutilizable. Son propuestas, no autorizaciones. No ampliar este candidato, integrar enemigos, iniciar D4/D5 ni revisar presupuesto/objetivos por iniciativa propia. Las notas inferiores son el historial anterior al cierre.

- **Puerta C: resultado negativo, 18/10/2026:** específicas nuevas **3/3**, 62,106 s (`Regional-20261018T195217/edit.xml`), tres repeticiones × 16/32/64 × S/M/L × seis modos; presupuesto y suma de contadores correctos, oráculo comprobado después de cada modo. A 64 m, vecino requiere **1101086 unidades agregadas**, hasta **6,133 s**; móvil p95 S/M/L **7,833/6,750/3,700 s**. A 16/32 también falla. Informe reproducible `D3-Batch-20261018/budget-first.json`. No seguir construyendo campos locales ni integrar. Solo completar desglose CPU por clase (repetición específica `Regional-20261018T195506`), regresiones Edit/Play, referencias, huellas y publicación del experimento negativo. Arquitectura detenida; D3 bloqueado, D4 sin iniciar. No se ha subido 3000 ni relajado objetivos.

- **Puerta B verificada, 18/10/2026:** específicas nuevas **33/33**, 290,033 s (`Regional-20261018T173419/edit.xml`). Oráculo monolítico independiente por cola, costes/alcance de todos los nodos y siguiente arista óptima canónica, reconstrucción física, A/B/ocho C × 16/32/64 × S/M/L con metas vecinas/altura, fixtures de reentrada, capas desconectadas, rampa y corredor S/M sin L. Publicación anterior intacta durante el cálculo. Adaptador del scheduler únicamente en pruebas; C en ejecución, tras fallo de licencia `Regional-20261018T174125`. Contadores desglosados y semillas separadas de propagaciones, inicialización/índices bajo presupuesto; sin cambios de backend activo. No hay clasificación de rendimiento todavía. Siguiente: revisar C, corregir solo instrumentación si procede y cerrar regresiones/documentación/publicación antes de detenerse para revisión.

- **Puerta A verificada, 18/10/2026:** específicas nuevas **30/30**, 123,778 s (`Regional-20261018T171314/edit.xml`), 90 registros S/M/L. Corpus completo: portales→lotes 312421→24833 / 141040→6926 / 52109→1435 para 16/32/64. Reconstrucción exacta y determinismo correctos; informe `D3-Batch-20261018/groups.json`, script `scripts/p0-d3-batch-report.py`. `RegionalBatchField` local implementado sin scheduler ni movimiento; puerta B en ejecución (`Regional-20261018T173419`, primer intento `171813` licencia 198). No certificar todavía equivalencia B ni rendimiento. Solver: heaps regionales multiobjetivo, envío de semillas tras vaciar cada heap, cola FIFO reabrible, publicación por convergencia mundial y doble buffer. Índices inversos construidos en Advance y contabilizados, inicialización en cada solicitud. Siguiente: resolver resultado B antes de añadir el adaptador de diagnóstico del scheduler.

- **Experimento de lotes autorizado, 18/10/2026 (Codex, en curso):** fetch, rama y HEAD/origin `ea5d81b` comprobados; solo siete ajustes Unity recibidos. Integridad de entrada **9/9**, `unity/TestResults/P0/D3-Batch-20261018/integrity-before.json`. Autoriza A agrupación reversible → B equivalencia multiobjetivo → C scheduler/medidas, sin integración QA ni D4. Primera pieza local `NavPortalGroups` y `RegionalBatchTests`: lotes con todos los IDs físicos, firma clase/regiones/SCC/tipo/dirección/capa/soporte; no ventanas representativas. Específicas de agrupación en curso (`Regional-20261018T171314`); primer intento `171215` falló por licencia 198 sin pruebas. No hay resultados nuevos certificados todavía ni commits nuevos. Siguiente: comprobar las 30 combinaciones del corpus antes de pasar al propagador. Las esperas de autorización inferiores son históricas y quedan sustituidas por la decisión del autor de esta sesión.

- **Cierre de evaluación D3, 18/10/2026 (Codex):** código `7344587` publicado mediante push normal desde `9e11d37`; comparador incremental recuperado, instrumentación corregida e informe reproducible añadidos. **D3 NO cerrado: parada por rendimiento para consulta arquitectónica; D4 NO iniciado.** Nuevas pruebas: específicas 16/16 (`Regional-20261018T094334`, 63,011 s); `scripts\u3.cmd edit` **894/894**, 1081,686 s; `scripts\u3.cmd play` **53/53**, 112,144 s. Primeros intentos de específicas/Play sin pruebas por licencia 198, conservados; repetidos fuera del sandbox. XML/log de cierre en `unity/TestResults/P0/D3-20261018`, informes `incremental.json`, `hierarchy.json`, `monolithic-diagnosis.json`; referencias U0/U2/U3/U4 nuevas coincidentes con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` (`qa-results/reference-2d186a4e6b1d4e198089d06b993b0b31`). Edit/Play del 17/10 revisados como **heredados**, no contados como nuevos.
  - Resultado: comparador primera publicación S/M/L 6,217/5,950/3,633 s y móvil p95 28,067/24,067/16,300 s; jerarquía regional vuelve a publicar **cero** en 180 s simulados. Cuotas/contabilidad ≤3000 verificadas, pero servicio útil insuficiente. Asignaciones N/D, memoria de buffers parcial, sin FPS. Control monolítico repetido: 36 trazas D0, cero discrepancias nodo/celda y 372/3/0 publicaciones con 750 móvil; M p95 57,6 s. [Resultados, límites y alternativas](REGIONES_P0D.md).
  - Protecciones: huellas finales **9/9** iguales a entrada, JSON antes/después archivados. Sin cambios a src, corpus, U6, generador, movimiento activo, RNG, balance, guardados ni circuitos A/B/C. Solo los siete ajustes locales Unity deben quedar sin commit al publicar el cierre documental; ningún código local D3 queda pendiente. Sin build nueva, PR, reset, clean ni force-push.
  - **Siguiente paso exacto:** consultar la alternativa 1 de REGIONES_P0D (experimento de agrupación de portales y propagación regional multiobjetivo por lotes, con equivalencia física y presupuesto intactos), frente a seguir reparando globalmente o revisar objetivos. **No es una aprobación ya concedida ni petición de reautorizar D3/D4.** No integrar ninguno de los dos candidatos fallidos ni iniciar D4 mientras falte resolver este bloqueo. Campos locales, versiones/invalidation completas y persecución regional 300/500/750 con jefe siguen pendientes. Si se aprueba un experimento, medirlo y contrastarlo antes de integrar; detenerse al terminar D4, D5–D7 excluidos. Cierre documental: «Documenta el bloqueo de D3 y las alternativas medidas», push normal y verificar HEAD/origin al retomar.

- **Reanudación D3, 18/10/2026 (Codex), verificación en curso:** rama/HEAD/origin `9e11d37` comprobados con fetch. Conservados cinco documentos locales, `IncrementalNavField.cs` y su suite sin publicar, además de los siete ajustes protegidos. Huellas de entrada 9/9 iguales a 17/10 (`D3-20261018/integrity-before.json`). Play heredado sí terminó: `D3-20261017/play.xml`, 53/53, 112,426 s; Edit heredado 878/878. Suite incremental **nueva 16/16**, 63,011 s, `Regional-20261018T094334`; intento anterior `094231` sin pruebas por licencia 198. Exactitud correcta, rendimiento insuficiente: primera publicación S/M/L 6,217/5,950/3,633 s; móvil entre dos vecinos p95 28,067/24,067/16,300 s. No hay movimiento físico ni FPS. Se corrige únicamente instrumentación: edad móvil tras primera reparación (sin arrastrar 180 s fijos), CPU de ticks activos, testigo de asignaciones y igualdad de contabilidad. Edit completo en curso sobre esa versión; referencias históricas nuevas correctas en `qa-results/reference-2d186a4e6b1d4e198089d06b993b0b31`. **No adoptar comparador ni iniciar D4:** preparar alternativas ante incumplimiento, cerrar regresiones/publicación y consultar arquitectura. No subir 3000 ni relajar objetivos. Los puntos inferiores son historial.

- **Continuación autorizada del comparador, 17/10/2026:** el autor responde «sigue» a la consulta sobre evaluar reparación incremental exacta. Se evalúa, sin adopción automática ni cambio de presupuesto/partidas normales; decisión registrada en DECISIONES. Código regional previo `9e11d37`: regresión Edit nueva **878/878**, 1008,078 s, copiada a `D3-20261017/edit.xml` y `edit.log`. Play nuevo en curso fuera del sandbox tras error de licencia 198 (log conservado `play-first-attempt.log`). Siguiente paso: terminar Play y huellas, publicar el diagnóstico; construir y contrastar el comparador exacto con inicialización, reparaciones y publicación dentro de 3000 antes de decidir el siguiente paso D3. La parada descrita debajo es el estado anterior a esta respuesta; D4 sigue sin iniciar.

- **Parada D3 por resultado medido, 17/10/2026 (Codex):** entrada HEAD/remoto `957524650029c18ab18e9f955278ab1e0e69222e`, fetch comprobado. Cuatro documentos heredados conservados/revisados/publicados en `3c1fb90`. Transferencias inversas `2ca9c29`: **71/71 nuevas**, 280,584 s, XML/log `Regional-20261017T104657`. Prototipo exacto de costes de salida `9e11d37`: **6/6 invariantes/diagnósticos nuevos**, XML/log `Regional-20261017T114240`; primer intento falló por licencia 198 sin pruebas (`Regional-20261017T105239`). Ambos commits publicados, HEAD/origin/remoto `9e11d37d1a438bc774a6922ec833780d93e3b1eb` comprobados. **HORDA750 frío: cero publicaciones S/M/L en 180 s simulados para 16/32/64 m, aunque cada clase recibe 10 800 000 unidades; ningún tick supera 3000.** [Informe y límites](REGIONES_P0D.md); JSON `D3-20261017/hierarchy.json`. No es persecución ni FPS. Asignaciones N/D; latencia censurada a 180 s, no cero. Campos locales y movimiento no implementados; D3 sigue abierto y D4 no iniciado. Incumplimiento importante: solicitada decisión sobre evaluar un comparador incremental exacto (alternativa futura del plan), sin implementarlo. **Siguiente paso exacto: resolver esa decisión con el autor antes de avanzar funcionalmente**; no repetir autorización general de D3/D4. Regresión Edit completa en curso; referencias históricas nuevas coinciden (`qa-results/reference-e3ed61b9d62744b399ee9780a6ed40bc`). Huellas 9/9 comprobadas antes/después de las específicas; siete ajustes excluidos e intactos, sin cambios a U6, src, generador ni circuitos. Documentación de este cierre todavía local mientras terminan regresiones.

- **D3 primera pieza verificada, 16/10/2026 (Codex):** código `957524650029c18ab18e9f955278ab1e0e69222e`, push normal realizado y HEAD/origin coincidentes. Transferencias internas exactas con buffers regionales, caché acotada y planificador 3000 con cuotas S/M/L. Pruebas específicas **38/38** nuevas en `Regional-20261016T170216`; primera pasada 37/37 conservada. Referencias U0/U2/U3/U4 coinciden (`qa-results/reference-9f070972b60d4a66843fdf9ea1e29f6c`). Edit completo nuevo **839/839**, 838,686 s, archivado en `D3-20261016`; Play nuevo **53/53**, 112,009 s, fuera del sandbox tras un primer intento fallido por licencia 198; XML/log y huellas finales 9/9 archivados en la misma carpeta. [Medidas y límites](REGIONES_P0D.md): 16 m reduce máximo interno de 18 a 4 ticks, arrays de dos entradas hasta 33 028 B; asignaciones N/D. Reparto 1000/clase durante 180 s solo sintético, sin acreditar persecución real. **D3 no terminado; D4 no iniciado.** Próximo paso exacto: transferencias inversas dirigidas, costes globales de salidas y campos locales compartidos/versionados dentro del mismo presupuesto; después integración optativa y presión con movimiento físico. No adoptar el control mundial (hasta 142 ticks) como solución a objetivo móvil. Sin decisión nueva del autor requerida para ese siguiente paso autorizado. Los siete ajustes Unity permanecen excluidos e intactos. Checkpoint documental separado: «Documenta las transferencias verificadas y el siguiente paso de D3», con push normal; comprobar HEAD/origin al retomar. No queda código local sin publicar.

- **D3 en curso, 16/10/2026 (Codex):** entrada HEAD/origin `d4906e6e4f2f5140df57f93aa9f27d31117fde6a`, fetch comprobado; solo siete ajustes Unity protegidos recibidos, sin código posterior a D2. Integridad 9/9 en `unity/TestResults/P0/D3-20261016/integrity-before.json`. Primera pieza local: `RegionalNavScheduler` (3000 compartidos, cuota 1000 por clase, antigüedad y prioridad) y `RegionalTransferCache` (fuente física exacta, ámbito interno o mundial de control, fraccionamiento, agrupación, caché acotada y contrapresión). Suite `RegionalTransferTests` nueva en ejecución; primer intento restringido terminó por licencia 198 sin XML (`Regional-20261016T165327`); segundo intento fuera del sandbox en `Regional-20261016T165709`. **Sin resultados nuevos certificados todavía.** Sin integración en movimiento, campos locales ni cierre D3; D4 pendiente y D5 excluido. Trabajo local conservado tras interrupción del servidor. Siguiente paso: comprobar XML/log de esta ejecución, corregir y medir esta pieza antes de construir los campos locales. No interpretar las cuotas sintéticas como solución ya demostrada de la persecución M/L.

- **D2 verificado, 15/10/2026 (Codex):** código `7c50d9f`, portales físicos exactos por clase/superficie, SCC internas y mundiales, componentes débiles y consultas/rutas con caché acotada a un origen. Específicas nuevas **44/44**, Edit **801/801**, Play **53/53**; archivos `Regional-20261015T224516` y `D2-20261015`. Referencias históricas e integridad 9/9 correctas. [Arquitectura, medidas 16/32/64 y límites](REGIONES_P0D.md). No hubo discrepancias regional/monolítico; primer fallo fue una expectativa de fixture suspendida, conservada como caso sin conexión y sustituida para caída unilateral por voladizo físico 0,25 m. Contador de asignaciones sin capacidad de detectar testigo: N/D, no prueba de cero GC. D0 llegada/inanición intactas. Publicación: código y cierre documental con push normal; confirmar SHA remoto. **Siguiente paso: D3 autorizado, medir transferencias exactas bajo demanda, caché fría/caliente y reparto justo dentro de 3000; resolver inanición M p95 57,6 s / L sin publicar en 180 s. D4 después; detenerse antes de D5.** Solo siete ajustes Unity protegidos quedan fuera de los commits.


- **D2 código verificado específicamente, 15/10/2026:** commit local `7c50d9f` (todavía sin push al escribir esta nota). Suite nueva 44/44 en `Regional-20261015T224516`; Edit completo en curso. Informe por clase/tamaño en `D2-20261015/regional-summary.json` y `REGIONES_P0D`. Primera pasada 34/37 documentada: expectativa de caída inválida en una fixture suspendida, sin discrepancia con monolítico ni cambio de física; voladizo unilateral 0,25 m validado después. Contador de asignaciones invalidado por testigo 4096 B: N/D, no certificar cero GC. D3 todavía sin código.


- **D2 en implementación, 15/10/2026 (Codex):** base local/remota `c01070be3e71f1e2a31cf24fb91a41522b479643`, solo siete ajustes protegidos recibidos, huellas 9/9 comprobadas (`unity/TestResults/P0/D2-20261015/integrity-before.json`). Nuevo `NavPortals.cs`: portales exactos desde D1, SCC internas, componentes débiles, condensación dirigida mundial y rutas físicas bajo demanda con caché de un origen. Suite `RegionalPortalTests` en curso; aún no declarar D2 validado. Backend y 3000 intactos. Autor reitera continuar D3 tras validar/publicar D2, luego D4 secuencialmente; parada antes de D5.


- **D1 verificado, 15/10/2026 (Codex):** constructor regional optativo validado en A/B 80 m, ocho semillas C 128 m, S/M/L y baldosas 16/32/64 m. Igualdad exacta de nodos, aristas dirigidas, costes y trayectorias; fixture multicapa/esquinas y orden inverso. Suite regional nueva 28/28; Edit completo nuevo **757/757**, Play **53/53**. Referencias U0/U2/U3/U4 coincidentes y huellas protegidas 9/9 iguales. Repetición D0: 36 CSV sin discrepancias nodo/posición; sin esbirros fijo 67,350 s, móvil 70,250 s. Inanición M p95 57,6 s / L sin publicar en 180 s reproducida, prioridad D3 con 3000. Detalle y medidas en [REGIONES_P0D](REGIONES_P0D.md). Commit previsto «Valida el constructor regional físico en los circuitos pequeños»; comprobar SHA y push con los comandos inferiores. Solo siete ajustes Unity protegidos deben quedar fuera del commit. **Siguiente paso exacto: D2, portales dirigidos derivados de aristas físicas y conectividad por clase/capa, contrastados con el monolítico; después D3 y D4 secuencialmente. D5 no autorizado.** D1 no activa backend regional ni demuestra residencia, FPS o cruces dinámicos D4.


- **Reanudación D1, 15/10/2026 (Codex):** HEAD/origin `5e4140633c4087d28d8ddbee36be383b5b89dd59`, corrección D0 ya publicada y XML heredados comprobados (729/729 Edit, 53/53 Play); no atribuirlos a una ejecución nueva. Recibidos y preservados el constructor regional, suite, script y documento D1 locales indicados abajo. Huellas protegidas 9/9 iguales. El intento regional del 14/10 terminó por licencia 198 sin XML; nuevo intento fuera del sandbox en `unity/TestResults/P0/Regional-20261015T173655`, en curso. D1 aún sin validar ni publicar; D2–D4 pendientes, D5 excluido. Inanición M p95 57,6 s / L sin publicación en 180 s sigue prioritaria D3, techo 3000 intacto.

- **D1 en implementación, 14/10/2026:** corrección D0 publicada `5e4140633c4087d28d8ddbee36be383b5b89dd59`, remoto confirmó el mismo SHA. Constructor regional optativo `NavRegionGraph`: baldosas 16/32/64 m, contexto de nodos con halo ≥10 m y ampliación si una consulta de destino sale del recorte; consultas físicas mundiales, aristas poseídas por su origen y trayectorias completas. Candidatos espaciales de tapas solo en modo regional, constructor completo conservado. Nueva suite `RegionalGraphTests` contrasta todos los nodos/aristas/costes/trayectorias/capas por clase en A/B y las ocho semillas C, más fixture de rampas/capas/esquinas y orden inverso. En verificación, no declarar D1 terminado. Sin selección QA ni integración normal; D2–D4 pendientes, D5 excluido.

- **Corrección D0 verificada, 14/10/2026:** Edit 729/729 y Play 53/53 nuevos; arnés D0 repetido, cero discrepancias nodo/celda. Jefe sin esbirros: de no llegar en 180 s a 67,350 s fijo / 70,250 s móvil. Jefe fijo completo 69,550 s, horda fija 71,983 s; horda móvil aún sin llegada/campo L. Informe antes/después en [DIAGNOSTICO_P0D0](DIAGNOSTICO_P0D0.md), XML/log `D0/Correction-20261014`, huellas 9/9 iguales, referencias históricas coincidentes. Cambio solo conversión a float antes de validar en TryStep, sin modificaciones de combate/desvío. Publicar como «Corrige la coherencia de celda al redondear el paso de enemigos» y continuar D1 autorizado. Inanición M/L sigue prioritaria para D3, 3000 conservados; D5 excluido. Las notas inferiores de aprobación pendiente son históricas y están resueltas.

- **Corrección D0 autorizada, 14/10/2026, en verificación:** el autor aprueba expresamente convertir X/Z a float antes de validar en `TryStep`. Cambio mínimo aplicado y reproducción convertida en regresión; ocho casos añadidos de fronteras positivas/negativas y esquinas. Edit completo en marcha; aún no declarar corregido el bloqueo ni avanzar D1 hasta ver resultados. La autorización incluye continuar D1–D4 si pasa sin regresiones; D5 excluido. Inanición M/L prioritaria para D3, 3000 conservados. Base `b263f7633926a03b1e90e0e1c22a0c956c4bf35f`; solo siete ajustes locales recibidos, conservados.

- **Cierre diagnóstico D0, 14/10/2026 (Codex):** Edit **721/721** y Play **53/53** ejecutados en esta sesión; referencias U0/U2/U3/U4 coincidentes. [Informe reproducible](DIAGNOSTICO_P0D0.md), evidencias nuevas `unity/TestResults/P0/D0/Verified-20261014` y trazas `D0/Diagnosis/20261014T*`. Autorización preservada en `32ef0cb`, arneses en `427ca82`. Defecto heredado confirmado de redondeo en `TryStep`; propuesta concreta no aplicada. **Siguiente paso exacto: decisión del autor sobre convertir X/Z a float antes de validar celda/vecino/superficie; después, corregir con pruebas de frontera y repetir controles D0 antes de D1.** D1–D4 siguen autorizados pero no iniciados por esta decisión funcional; D5–D7 excluidos. Solo siete ajustes Unity protegidos deben quedar fuera del índice al cerrar. Publicación y límites en el cierre inferior. Las notas de reanudación siguientes son historial.

- **Revalidación D0, 14/10/2026 (Codex, en curso):** HEAD/origin comprobados `1503780404bc3c73920f058bcd8a0909f68a820c`. Además del relevo descrito por el autor, se encontraron conservados `RegionalDiagnosisTests`, `RegionalGeometryDiagnosisTests`, dos scripts e informe `DIAGNOSTICO_P0D0.md` con resultados del 12/10 (715, 719 y 2 pruebas). Son heredados respecto a esta sesión, igual que 707/707; no atribuirlos a la ejecución del 14/10. XML/logs anteriores e integridad de entrada preservados en `unity/TestResults/P0/D0/Resume-20261014`. Suite Edit completa nueva en marcha. Posible defecto de redondeo double→float en `TryStep`, con propuesta acotada ya documentada, pendiente de reproducción nueva y decisión antes de cambiar C. Los procesos Claude 20700/20264 son ajenos a MAMPORRO por confirmación expresa del autor: no tocarlos ni bloquear por su existencia.

- **Reanudación D0, 12/10/2026 (Codex, en curso):** el autor confirma que Claude Code PID 20700/20264 pertenece a otro proyecto; no tocar esos procesos ni bloquear por su existencia. HEAD local/origin `1503780404bc3c73920f058bcd8a0909f68a820c`. Recibidos y conservados cambios documentales de autorización, `RegionalBaselineTests.cs`/meta y catorce trazas D0; XML 707/707 heredado, no ejecutado por esta sesión. Copia de evidencias/XML/logs previos en `unity/TestResults/P0/D0/Inherited-20261012`. Nuevo observador QA `RegionalDiagnosisTests` en verificación: llegada horizontal/vertical estable, tiempos por causa/ID, campos, tres repeticiones y control sin observador. Sin cambios de runtime. Detalle y resultados nuevos al final al cerrar; no interpretar esta nota como D0 terminado.

- **Punto vigente D0–D4, 12/10/2026 (Codex):** autor aprobó las ocho decisiones del plan. Inicio HEAD/remoto `1503780`, solo siete ajustes protegidos, huellas 7/7 y U6/u3-world correctas. D0 en curso: diagnóstico externo del jefe y referencias; sin cambios de runtime. D1–D4 autorizados después de verificar cada pieza. D5–D7 y ampliación no autorizados. Resultados reales se registran al cerrar la subfase.

- **Punto vigente, 12/10/2026 (Codex):** el autor probó C y confirma «Sí, todo bien, autorizamos y seguimos». C aprobado; cierre técnico publicado `369fdff`, código/builds `9a24b6f`. Limitaciones aceptadas provisionalmente y presupuesto 3000 conservado. Cierre formal publicado `b755987`. Plan [PLAN_P0D](PLAN_P0D.md) listo para revisión; siguiente paso exacto: el autor resuelve sus ocho decisiones y autoriza subfases, sin implementar hasta entonces. Cierre formal y auditoría al final.
- Los siguientes puntos fechados son **historial**, no autorizaciones ni tareas vigentes.

- **Punto vigente (P0-C cerrado técnicamente, 12/10/2026, Claude Code):** C0–C6 hechos; circuito C en la build QA (F10 A → B → C, F11 semillas, `-p0-seed`). **Pendiente: prueba manual del autor del circuito C y su decisión sobre el presupuesto de navegación (publicación de ruta 0,5/0,9/1,3 s frente al objetivo de 0,3 s).** Detalle en «P0-C — implementación» al final. No sustituir U3, no tocar U6, **no empezar P0-D**.

- **Punto vigente (cierre de P0-B, 12/10/2026):** P0-B aprobado manualmente por el autor; código `a9cac92`, cierre técnico `5688e90` y este cierre documental. Siguiente: plan de P0-C entregado en [PLAN_P0C](PLAN_P0C.md), con diez decisiones abiertas en su sección 14; **no programar P0-C hasta su aprobación**; D no autorizado. Los puntos siguientes son historial.

- **Punto vigente (P0-B, 11/10/2026):** navegación vertical y escalada de todos los enemigos implementadas y verificadas técnicamente en el circuito QA B (F10); cierre técnico `a9cac92`. Edit 635/635, Play 52/52, referencias coincidentes, benchmark/evidencias válidos. Detalle, desviaciones (p95 0,61 ms y pico 1,49 ms frente a 0,5/1 ms) y guía en «P0-B — … cierre técnico» al final. **Siguiente paso: prueba manual del autor; detenerse. No iniciar P0-C/D.**

- **Punto vigente (cierre P0-A, 11/10/2026):** A6 aprobado manualmente. Código `373620d`, cierre documental `dc8ab26` y este cierre. Siguiente: plan de P0-B (rutas de enemigos en altura) en [PLAN_P0B](PLAN_P0B.md), **revisado el 11/10 tras la decisión de que todos los enemigos escalen**, con cinco dudas abiertas en su sección 10; **no programar P0-B hasta su aprobación**; C/D no autorizados. Los puntos siguientes son historial.

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

## Cierre de P0-A6 — aprobación manual del autor (11/10/2026)

El autor probó la build P0 de `373620d` y aprueba A6 sin reservas («Todo bien»). Queda aprobado:

- Escalada automática al correr contra una pared sólida, sin botón ni salto previo; sin enganche al rozar o correr en paralelo.
- Todas las paredes físicas apropiadas (cajas y triángulos abruptos del terreno) por contrato de exclusión, sin listas de IDs; follaje, interactuables, enemigos, límites, suelos, tapas y caras inferiores excluidos.
- A/D según la pantalla, con la cámara hacia o de espaldas a la pared (`dc4739c`).
- W/S/A/D y diagonal normalizada a la velocidad efectiva de carrera: `PlayerPhysics.EffectiveSpeed(Tuning.PlayerBaseMoveSpeed * Stats[moveSpeed], CrowdSlow)`, misma fuente que la carrera, mejoras incluidas, en tramos de 0,075 m con borde/barrido/contacto (`373620d`).
- Transición a tejados y bordes, salida de fosos sin F7, salto con 0,35 s de protección por plano, sin resistencia ni límite de tiempo, cámara con retracción.

Últimos resultados verificados (11/10, sobre `373620d`): Edit 583/583, Play 51/51, referencias U0/U2/U3/U4 coincidentes, evidencia `20261011T203110` válida, benchmark Normal `20261011T203140` válido 0/300/500/750 con cero fallos. Este cierre es documental: no se han repetido pruebas ni builds; el código no cambia.

Integridad al cerrar: U6 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873` y las siete huellas protegidas iguales a la tabla A6.

Limitaciones conocidas aceptadas, no bloqueantes: en una esquina exterior el agarre se suelta al agotarse la cara (sin giro automático); cajas del circuito alineadas a ejes, paredes oblicuas solo en el terreno; sin importador de meshes arbitrarios ni escalada en la entrega U6; refugio en tejado sin acceso fuera del alcance de la horda (limitación A4, materia de P0-B).

**Continuación: P0-B (rutas de enemigos en altura). Primero plan para el autor; no programar hasta su aprobación. P0-C/D no autorizados.**

## P0-B — navegación vertical y escalada de todos los enemigos: cierre técnico (11/10/2026, Claude Code)

Plan aprobado: [PLAN_P0B](PLAN_P0B.md) (`ba33333`) con las cinco decisiones de [DECISIONES](DECISIONES.md). **Pendiente de la prueba manual del autor.** Solo en el circuito QA B; U3 y la entrega U6 conservan literalmente el comportamiento heredado.

### Commits

| Fase | Commit |
| --- | --- |
| Registro de la aprobación | `0384a2b` |
| B0 — circuito B y persecución heredada fijada | `dc9988d` |
| B1 — grafo multinivel por tamaño | `96ed2bd` |
| B2 — campo de distancias compartido | `9737f9b` |
| B3 — navegación en todos los enemigos | `86b8e87` |
| B5 — combate vertical (antes que B4 para medir el coste final) | `0ba69ab` |
| B4 — circuito B en la build QA (F10) y su arnés | `9e02220`, `9d1a56a` |
| Correcciones tras el benchmark (penetraciones, picos) | `a77db33`, `a9cac92` |

### Qué hace

- **Circuito B** (F10 alterna con el A; el HUD indica el activo): tejado con rampa, tejado solo escalando de cuatro paredes, tejado amplio donde cabe el jefe, torre estrecha donde no cabe, meseta y acantilado del terreno sin rampa, pasillo de 3 m, muro en L, pilares, pared con techo encima (sin salida) y seta inaccesible incluso escalando. F4: horda mixta 0/300/500/750 (jefe desde 300).
- **Grafo** (`NavGraph`), uno por clase de cuerpo deducida del catálogo (S pelusa/cucaracha/paloma, M táper/rata, L jefe): rejilla de 0,5 m con capas por altura, nodos con cuerpo libre en toda la celda (escalones de 0,6 m permitidos; en pendiente se comprueba el volumen entre la altura mínima y la máxima de la celda) y huella apoyada. Aristas de caminar (≤48°, escalón, sin cortar esquinas), escalar (misma física que el jugador: cara continua, columna libre, Ledge) y caer. Toda trayectoria se valida por muestreo con el cuerpo de la clase. Sin listas de IDs.
- **Campo compartido** (`NavField`): Dijkstra inverso desde el jugador por clase, doble búfer, un único presupuesto de 3000 nodos por tick entre las tres clases (prioridad a la más numerosa), máximo cinco recálculos por segundo y clase. Inaccesible: cada enemigo va al punto de su región más cercano al jugador y se detiene.
- **Movimiento** (`EnemyNavigation`, arrays por hueco): caminar solo por celdas conectadas del grafo; escalar y borde por trayectorias validadas a la **velocidad efectiva propia** de cada enemigo; caer con gravedad (la del jugador) apoyándose en el suelo real; un empuje existente hacia fuera mayor que su velocidad lo suelta (0,35 s sin reengancharse a ese plano); recuperación con barrido. Contacto a 1 mm de la cara y holgura de 0,5 mm para que el redondeo float no meta un cuerpo en un sólido.
- **Comportamientos**: la paloma dispara solo estable y con línea de visión; la rata carga solo en línea transitable; el jefe deja como máximo un ataque pendiente mientras no está estable y sus esbirros aparecen sobre su superficie; nadie golpea cuerpo a cuerpo escalando.
- **Combate vertical (modo P0)**: disparo hostil desde la altura real del tirador, en línea recta 3D hacia el jugador en el disparo, sin seguimiento, detenido por sólidos y suelo; estornudo a la altura del jefe; proyectil amistoso a su altura, detenido por muros y con la banda de 1,8 m de la Barra, igual que Naftalina y el Jersey. Daño, alcance, cadencia y velocidad sin cambios.

### Pruebas ejecutadas (11/10, código final `a9cac92`)

| Comando (CMD, raíz) | Resultado |
| --- | --- |
| `scripts\u3.cmd edit` | **635/635** (583 al empezar P0-B) |
| `scripts\u3.cmd play` | **52/52** (51 al empezar) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` | U0/U2/U3/U4 coinciden; 8 guardados y 6 secuencias meta |
| `scripts\p0.cmd build` / `devbuild` | correctas, ambas `a9cac92e99deb4db2cf8cac1c3004de4b329f3a1` |
| `scripts\p0.cmd benchmarkb` | válido (8 informes), `unity/TestResults/P0/PursuitNormal/20261011T224227` |
| `scripts\p0.cmd devdiagb` | válido, `unity/TestResults/P0/PursuitDevelopment/20261011T224827` |
| `scripts\p0.cmd evidenceb` | válida, 10 PNG, `unity/TestResults/P0/PursuitEvidence/20261011T225353` |
| `scripts\p0.cmd evidence` / `benchmark` (circuito A, regresión) | válidos, `Evidence/20261011T225430`, `Normal/20261011T225451`: 12–14 bordes, 0 fallos, atascos y rescates |
| `scripts\p0.cmd jugar` | salida 0, ventana respondiendo, `unity/Builds/P0/MAMPORRO-P0.exe`, progreso `ManualSave` |

Pruebas nuevas: `PursuitBaselineTests` (4), `NavGraphTests` (12), `NavFieldTests` (9), `EnemyNavigationTests` (18), `VerticalCombatP0Tests` (7), `PursuitStressTests` (315 y 765 enemigos recorriendo las seis posiciones, verificando cada tick cuerpos dentro de sólidos o bajo el terreno) y un PlayMode del cambio de circuito. XML/log en `unity/TestResults/P0/B`. Las sondas A4 (`VerticalCombatTests`) siguen documentando el heredado. Resultados destacados:

- Tejado solo escalando: un enemigo común golpea a los 3,27 s (heredado: nunca). Rampa desde el este: 2,35 s (heredado 7,00 s por desvíos ciegos). Bajada desde un tejado: 0,29 m por tick con gravedad (heredado 4 m de golpe). Los cinco tipos comunes, élite y a distancia escalan; el jefe sube al tejado amplio y espera junto a la torre estrecha. La escalada va exactamente a la velocidad propia (cucaracha 7,58 m/s, táper 2,74 m/s en esa semilla).
- Cuatro sectores con 40 enemigos: 40/40 llegan dentro de 1,5 × (coste de ruta / su velocidad) + 3 s (p50 7,2 s). Seta inaccesible: sin daño, esperan junto a ella (0,02 m de movimiento en 2 s). Estrés 765: 0 incidencias, pico de atascados 5/765 (0,65 %).

### Rendimiento (lógica del arnés, no FPS del juego; GPU N/D)

Ryzen 7 7700X / RTX 4070 Ti SUPER, Windows x64 Mono D3D11, 1920×1080 interna 360, VSync 0, 10 s + 30 s por carga, armas desactivadas e invulnerabilidad QA, horda mixta con jefe desde 300, jugador recorriendo seis posiciones. Misma build y condiciones para heredado y navegación.

| Carga | Tick p50/p95 heredado (ms) | Tick p50/p95 navegación | Navegación p50/p95/p99/máx | Penetraciones | Atascados (pico) | Aglomerados (pico) |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0,012/0,015 | 0,013/0,017 | 0,000/0,001/0,001/0,001 | 0 | 0 % | 0 % |
| 300 | 0,319/0,447 | 0,371/0,482 | 0,133/0,228/0,578/1,053 | 0 de 49 025 | 0 % | 50,6 % |
| 500 | 0,611/0,876 | 0,626/0,827 | 0,283/0,402/0,695/1,465 | 0 de 80 975 | 0,2 % | 37,8 % |
| 750 | 1,051/1,420 | 0,979/1,251 | 0,443/0,608/0,899/1,490 | 0 de 120 855 | 0,4 % | 31,9 % |

Enemigos a la altura del jugador al final de cada posición (≈6,7 s), heredado → navegación, 300: tejado solo escalando 0 → 13 %, tejado amplio 0 → 48 %, rampa 68 → 57 %, meseta 24 → 17 % (el heredado sube la pared de 79° caminando), seta 3 → 0 %. 750: tejado solo escalando 0 → 8,5 %, tejado amplio 0 → 31 %.

Construcción de los tres grafos: 648 ms, una vez al cargar el circuito B (objetivo estimado 150 ms; no se ha optimizado). Memoria Unity asignada 248 MiB en ambos modos. Development: GC 8,0–8,1 KB/frame heredado frente a 9,4–10,6 KB/frame con navegación (+1,4 KB sin enemigos, atribuible al texto del HUD QA del circuito B; ~1 B por enemigo y frame más, origen sin aislar; el cálculo del campo asigna 0 B medidos).

**Objetivos no cumplidos, sin ocultar:** navegación p95 con 750 = 0,608 ms (objetivo 0,5) y máximo aislado 1,49 ms (objetivo de pico 1 ms; p99 0,90). La primera medición (`20261011T220942`) tenía picos de 2,6–2,8 ms porque los tres campos podían recalcularse en el mismo tick; el presupuesto compartido los redujo a la mitad a cambio de repartir el trabajo en más ticks (p95 0,53 → 0,61). Contexto: esta cifra incluye el movimiento que el heredado también paga (empuje contra colliders); el tick total con navegación es menor que el heredado desde 500 enemigos. Propuesta de corrección si el autor lo pide: reducir el coste por enemigo (~0,59 µs, la mayor parte del p50) y recalcular el campo de forma incremental cuando el jugador solo se desplaza a nodos vecinos.

Primer benchmark no válido por dos defectos reales, corregidos y verificados: penetraciones (66/68/1 cuerpos en sólidos por redondeo float del contacto de escalada) y un umbral de validez que no admitía los esbirros del jefe.

### Limitaciones conocidas

- Aglomeración: con cientos de enemigos persiguiendo un tejado, la mayoría espera su turno al pie de las paredes; es capacidad física de la pared, no ruta inválida. Una cola impide trepar sobre otro escalador.
- El mapa de cada clase cubre el circuito entero (80 m); la ventana local alrededor del jugador prevista para mundos grandes no se ha implementado (P0-D).
- Paredes oblicuas solo del terreno; el circuito usa cajas alineadas a ejes, como A6.
- La captura B01 de la evidencia no encuadra bien a los escaladores (la cámara mira desde el tejado); el HUD confirma la escalada.
- Recogida de gemas y monedas sigue siendo en planta (fuera de alcance).

### Integridad

U6 SHA-256 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873` antes y después; siete ajustes protegidos con las huellas de la tabla A6, fuera de todos los commits (`unity/TestResults/P0/B/integrity-after.json`). Sin cambios en `src/` ni en `unity/Docs/Reference/`.

### Prueba manual

`scripts\p0.cmd jugar`, F10 para el circuito B, F4 para cargar la horda. Subir al tejado solo escalando (−11, −11), al de la rampa (11, −11), a la meseta (16, 10) y al tejado amplio (0, 16) y comprobar que la horda escala, entra arriba y ataca; bajar y ver que caen y siguen. La seta (20, 20) y la placa sobre la pared con techo no son alcanzables para nadie (pruebas automáticas). Observar palomas disparando solo con línea de visión y la rata y el jefe escalando. **Detenerse: no iniciar P0-C/D.**

## Cierre de P0-B — aprobación manual del autor (12/10/2026)

El autor probó la build P0 de `a9cac92` y aprueba P0-B definitivamente. Queda aprobado: escalada física de paredes, edificios, tejados y acantilados por todos los enemigos (comunes, a distancia, rata, jefe y esbirros) a su velocidad efectiva propia; navegación compartida por tres tamaños con caminar, escalar, borde y caída con gravedad; combate vertical y colisiones de proyectiles corregidos; circuito QA B con F10, compatible con el circuito A y con las referencias históricas.

Últimos resultados técnicos (11/10, sobre `a9cac92`, ya registrados arriba): Edit 635/635, Play 52/52, referencias U0/U2/U3/U4 coincidentes, estrés de 765 enemigos sin penetraciones (atascados ≤ 0,65 %), benchmark/diagnóstico/evidencias válidos. Este cierre es documental: no se han repetido pruebas ni builds y el código no cambia.

Limitaciones aceptadas por el autor (no son objetivos cumplidos): navegación con 750 enemigos 0,61 ms p95 y 1,49 ms de pico (objetivos 0,5/1 ms); construcción de los tres mapas 648 ms al cargar el circuito; unos 2 KB más por frame en Development sin causa totalmente aislada; aglomeración de enemigos al pie de las paredes cuando cientos persiguen un tejado; mapas que cubren el circuito entero, por lo que un mundo mayor necesitará navegación por regiones (P0-D); recogida de gemas y monedas sin comprobación de altura, pendiente y sin corregir. Los benchmarks de lógica no equivalen a FPS reales. No optimizar P0-B sin una razón concreta y aprobación.

Integridad al cerrar: U6 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873` y las siete huellas protegidas iguales a la tabla A6.

**P0-B cerrado. Continuación: plan de P0-C ([PLAN_P0C](PLAN_P0C.md)); no programar hasta su aprobación. P0-D no autorizado.**

## P0-C — implementación (12/10/2026, Claude Code)

Plan [PLAN_P0C](PLAN_P0C.md) aprobado con las diez decisiones del autor. Circuito C solo en la build QA, mapa de 128 m, giros de 90°. Semillas de prueba: P0QA, MAMPORRO, COLINAS7, Z9K2, MESETA, ACANTILADO, RAMPA42 y HORDA750.

### Checkpoint C0 — línea base (`32c4a18`)
Recortes de 128 m del terreno U3: ~1 s los tres mapas, casi ninguna arista de escalada (el terreno U3 no tiene paredes útiles). Coste lineal con el número de sólidos: 17 → 153 ms, 117 → 502 ms, 317 → 1369 ms (clase S).

### Checkpoint C1 — rejilla espacial de sólidos (`936719d`)
`VerticalQueries` consulta solo los sólidos cercanos (celdas de 4 m, candidatos en orden de índice). 6000 consultas aleatorias y los mapas, idénticos al recorrido lineal; con 317 sólidos, 688 → 119 ms.

### Checkpoint C2 — trazado de niveles (`a4403b8`)
Macroceldas de 4 m, niveles 0/4,5/9/13,5 m calibrados a ~35 % elevado, mesetas de 8–40 m, rampas ≤ 30° de 8 m (4 m solo si no cabe otra), intentos deterministas. Mapas: descarte previo de escaladas, paso de 0,3 m y tres clases en paralelo (idéntico al serie): 5,8 s → 1,1 s.

### Checkpoint C3 — estructuras (`fea5149`)
Casas, templos, granjas, pozos, torres, muros, bloques escalonados y árboles, alineados a la rejilla. Reglas contra refugios por construcción (tapas ≤ 0,7 m o ≥ 1 m, tejados enrasados, nada flotante).

### Checkpoint C4 — validación y reparación con la navegación P0-B
`ProceduralMap` construye trazado, estructuras y los grafos S/M/L, comprueba y repara de forma determinista; los grafos aceptados quedan listos para reutilizarse en juego.

- **Criterio (decisión 8 y sección 6.9):** S alcanza todo desde el inicio y vuelve (cero inaccesibles y cero bolsas); toda tapa escalable donde cabe el jugador tiene nodos S con salida; los marcadores tienen un nodo S con salida al lado; M y L sin bolsas y sin grupos inaccesibles de más de 32 m² (el suelo principal y las mesetas amplias son mucho mayores, así que quedan cubiertos). Grupos de hasta 1 m² se toleran solo si un enemigo de esa clase toca al jugador desde un nodo válido (regla de contacto de `EnemiesNav`: radios sumados y diferencia de altura < 1,6 m).
- **Reparación:** comprobación previa barata del paso del jefe (rejilla de 1 m, 6–30 ms) que quita obstáculos o recoloca marcadores que cierran pasos; después, con `NavGraph`, se quitan los obstáculos culpables (a alcance del cuerpo de la clase tanto del grupo fallido como de la zona válida) y se recolocan los marcadores. Si el fallo es del terreno, se rechaza el intento y se genera el siguiente de la serie.
- **Cambios de generación para que el jefe circule:** pasillos entre mesetas de 8 m, relleno de canales de suelo de 4 m y de esquinas en diagonal, ruido de borde que solo erosiona las mesetas, franja exterior de 12 m a nivel 0, 6 m entre obstáculos (3 m hasta acantilados y rampas; 6 m para los árboles), puertas de 2,5 m (entra M). Marcadores QA con las cantidades del catálogo (14 cofres, 3 santuarios, 2 tótems, 1 portal), distancia al inicio limitada a 40 m.
- **Corrección en `NavGraph` (P0-B):** las caídas desde tapas cuya altura no es exacta en float (0,9 m, 9,9 m) no se generaban: `NodeY` (float) queda ~2e-8 m bajo la tapa y el barrido chocaba con ella. Ahora la caída sale 1 mm por encima (la holgura de contacto que ya usa la escalada). Regresión añadida; los circuitos A y B siguen pasando sus pruebas.
- **Tabla de celdas abruptas del terreno:** `Query` y la huella transitable responden en O(1) donde no hay caras abruptas cerca. Mismo resultado en 12 000 consultas aleatorias y en los mapas; mapa L 1254 → 908 ms.
- **Resultados (8 semillas, EditMode 681/681):** todas aceptadas; 14/3/2/1 marcadores en todas; 1–2 templos y 1–2 granjas. Tiempo total de generación, validación y mapas: 0,97 s sin reparación (P0QA, MESETA, HORDA750), 1,91–1,95 s con una reparación (COLINAS7, Z9K2, ACANTILADO, RAMPA42) y 2,09 s con un rechazo (MAMPORRO). Objetivo de 2 s cumplido en 7 de 8, con poco margen; la causa del resto es la reconstrucción completa de los mapas tras reparar.
- **Coste del criterio del jefe:** la separación de 6 m deja 18–28 árboles antes de validar (la plantilla pedía 40–60) y la comprobación previa quita además 8–25 obstáculos por mapa.

### Checkpoint C5 — circuito C en la build QA (`e2e4d36`, `9a24b6f`)
- `VerticalLayout.Procedural`: el circuito se crea desde el mapa validado (límite de 60 m) y la navegación reutiliza sus grafos S/M/L, sin reconstruirlos. Árboles verdes y marcadores QA de color propio (cofre dorado, santuario cian, tótem rojo, portal violeta).
- **Controles QA:** F10 recorre A → B → C; F11 pasa a la siguiente semilla QA (P0QA, MAMPORRO, COLINAS7, Z9K2, MESETA, ACANTILADO, RAMPA42, HORDA750) y, desde A o B, entra en C. Argumentos `-p0-circuit C` y `-p0-seed TEXTO` (mismo formato de semilla que el juego: A–Z y 0–9, hasta 12). F11 no lo usaba nada. El HUD muestra circuito, semilla, intento, reparaciones, rechazos y tiempo del mapa.
- **Horda QA:** mixta como en B, repartida por los 128 m (también sobre mesetas); el jefe aparece en el nodo L de suelo más cercano a (−14, 0).
- **Arnés:** `scripts\p0.cmd benchmarkc|devdiagc|evidencec` en P0QA, COLINAS7 y HORDA750 dentro de una ejecución; posiciones del jugador tomadas de los puntos clave de cada mapa (inicio, meseta mayor de cada nivel, un tejado, suelo lejano).

### Checkpoint C6 — verificación y cierre técnico
- **Pruebas:** EditMode **693/693**, PlayMode **53/53** (incluye `CircuitCRunsTheValidatedMapAndF11CyclesSeeds` y el recorrido A → B → C → A).
- **Estrés en lógica (EditMode), 3 semillas × 300/750:** cero penetraciones, cero cuerpos bajo el terreno, pico de atascados ≤ 1/765.
- **Builds QA** Normal y Development de `9a24b6f` (`Builds/P0`, `Builds/P0Dev`, `P0-source.txt`).
- **evidencec** válida (`unity/TestResults/P0/ProceduralEvidence/20261012T040154`, 12 capturas): en las tres semillas la horda sube a la meseta de nivel 3 y el jefe llega a la meseta amplia; vista cenital y santuario. La vista cenital queda lavada por la niebla de distancia del render (no se tocó).
- **benchmarkc** válido (`unity/TestResults/P0/ProceduralNormal/20261012T040404`, 12 informes). Medidas de lógica del arnés, no FPS:

| Semilla | Carga | Navegación p95 / máx (ms) | Tick lógico p95 (ms) | Publicación S/M/L (s) | Penetraciones / bajo terreno | Atascados / aglomerados (pico) |
| --- | --- | --- | --- | --- | --- | --- |
| P0QA | 300 | 0,568 / 1,499 | 0,78 | 0,48 / 0,90 / 1,13 | 0 / 0 | 0 % / 85,7 % |
| P0QA | 750 | 0,714 / 1,673 | 1,31 | 0,48 / 0,90 / 1,13 | 0 / 0 | 0 % / 86,5 % |
| COLINAS7 | 300 | 0,473 / 1,472 | 0,74 | 0,50 / 0,92 / 1,15 | 0 / 0 | 0 % / 92,7 % |
| COLINAS7 | 750 | 0,615 / 1,648 | 1,35 | 0,50 / 0,92 / 1,15 | 0 / 0 | 0,1 % / 89,3 % |
| HORDA750 | 300 | 0,593 / 1,303 | 0,77 | 0,48 / 0,90 / 1,13 | 0 / 0 | 0 % / 81,3 % |
| HORDA750 | 750 | 0,825 / 1,506 | 1,40 | 0,48 / 0,90 / 1,13 | 0 / 0 | 0 % / 77,7 % |

  Con ruta válida: 99,9–100 % en todas las cargas. Cargas de 500 en el informe.
- **devdiagc** válido (`unity/TestResults/P0/ProceduralDevelopment/20261012T041244`): 11,0–12,3 KB de GC por frame (B: 10,6 KB con 750; con 0 enemigos C ya da 11 KB, sobre todo HUD y arnés); memoria gestionada ~80 MB (B: 35 MB, la diferencia son los grafos del mapa de 128 m); total asignado 165 MB.
- **Integridad:** U6 `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873` y los siete ajustes protegidos con las huellas de la tabla A6; `src/` y `unity/Docs/Reference` sin cambios desde `6019a1a`. Circuitos A y B: sus pruebas siguen pasando (incluido el estrés de B).

### Objetivos de presupuesto (decisión 10)
| Objetivo | Resultado | Estado |
| --- | --- | --- |
| Trazado ≤ 300 ms | 3–9 ms (con estructuras, 6–12 ms) | Cumplido |
| Mapas ≤ 2 s (QA) | Generación + validación + mapas: ~1,0 s sin reparación; 1,9 s con una; 2,1 s con un rechazo (7 de 8 semillas ≤ 2 s) | Cumplido con poco margen, salvo MAMPORRO |
| Navegación ≤ +10 % sobre P0-B (0,61 ms p95 con 750) | 0,615 / 0,714 / 0,825 ms | Solo COLINAS7; P0QA +17 %, HORDA750 +35 % |
| Ruta tras cambio de posición/altura ≤ 0,3 s | S 0,48–0,50 s, M 0,90–0,92 s, L 1,13–1,15 s | **No cumplido** |

**Causa de la publicación lenta:** los tres mapas de C suman ~100 000 nodos (B: ~10 000) y comparten el presupuesto heredado de 3000 nodos por tick; se calculan uno tras otro (primero la clase con más enemigos) y cada campo termina su cálculo en curso antes de empezar la nueva meta. Curva medida en EditMode (orientativa, 750 enemigos): presupuesto 4500 → 0,33/0,60/0,77 s y navegación p95 ~0,8 ms; 6000 → 0,25/0,45/0,57 s y ~0,9 ms; 9000 → 0,17/0,30/0,38 s y ~1,3 ms. Ningún presupuesto cumple a la vez los 0,3 s y el +10 %. Se deja 3000 (comportamiento de P0-B intacto) y **la decisión es del autor**; alternativas posibles: subir el presupuesto solo en C, aceptar la latencia, o una ventana de navegación local (frontera con P0-D, no iniciada).

### Hallazgos para la prueba manual
- **Aglomeración:** pico de 78–93 % de enemigos esperando (B: 32 %). Es la limitación aceptada de P0-B (colas al pie de las paredes) amplificada: cada posición es una meseta o tejado con una o dos rampas o paredes de acceso y hasta 750 enemigos repartidos por 128 m. Casi nadie queda atascado (≤ 0,2 %).
- **Jefe lento en HORDA750:** llega a la meseta amplia en 69,6 s con 20 inicios de escalada para una ruta de unos 70 m (P0QA 18,7 s, COLINAS7 15,3 s). No es refugio, pero la ruta es ineficiente.
- **Menos árboles** (18–28 antes de validar) por la separación de 6 m que necesita el jefe.
- La recogida de gemas y monedas sigue ignorando la altura (pendiente aceptado en P0-B).

### Cómo probar el circuito C
1. `scripts\p0.cmd jugar` (build QA Normal; progreso solo QA).
2. F10 hasta «C · procedural» (o lanzar con `-p0-circuit C`); F11 cambia de semilla; F4 0/300/500/750 enemigos (jefe desde 300); F7 rescate (invalida); F8 reinicio.
3. Comprobar: mesetas de varios niveles con bordes rectos e irregularidad moderada, rampas (≤ 30°), escalada de acantilados y estructuras, tejados transitables, pozos sin tejado, árboles como obstáculo no escalable, marcadores sobre superficies alcanzables, que ninguna zona sirva de refugio y que el jefe llegue a las zonas amplias.

**P0-C cerrado técnicamente. Pendiente: prueba manual del autor del circuito C y su decisión sobre el presupuesto de navegación. No empezar P0-D.**

## Cierre formal de P0-C — aprobación manual (12/10/2026, Codex)

El autor ha probado C y confirma **«Sí, todo bien, autorizamos y seguimos»**. P0-C aprobado manualmente y cerrado; no volver a pedir su aprobación. Autoriza solo la planificación de P0-D. Mantener **3000 nodos/tick**, sin subirlos por defecto. Aceptar provisionalmente limitaciones no convierte objetivos incumplidos en cumplidos.

Procedencia: cierre técnico publicado `369fdffbc56dcd2687bef9815bf043b00fb051d6`; código/builds QA Normal/Development `9a24b6f67e39fc4ffdbc8d711fe9fc6d13c1e2cc`. Resultados **heredados de Claude**, no repetidos por Codex: EditMode 693/693, PlayMode 53/53, ocho semillas válidas, estrés sin penetraciones ni cuerpos bajo terreno, 99,9–100 % con ruta, circuitos A/B conservados. XML locales `unity/TestResults/U3/edit.xml` y `play.xml`; informes `P0/ProceduralNormal/20261012T040404` y evidencia `P0/ProceduralEvidence/20261012T040154`.

### Limitaciones aceptadas y correcciones de interpretación

- MAMPORRO: ~2,09 s de generación, validación y mapas, sobre el objetivo de 2 s.
- Navegación con 750: hasta 0,825 ms p95 en HORDA750; no cumple el +10 % sobre B en todas las semillas. Lógica del arnés, no FPS reales.
- Campos lentos: S/M/L ~0,48–0,50/0,90–0,92/1,13–1,15 s en Normal; L ~1,27–1,28 s en EditMode. Objetivo 0,3 s incumplido.
- **Corrección Codex del XML heredado:** a 9000 nodos, HORDA750 registra **0,167/1,083/1,217 s S/M/L**. La mejora ~0,17/0,30/0,38 s resumida antes no es uniforme. No fundamentar una subida en ese resumen; no es un ensayo nuevo.
- Picos de aglomeración 78–93 %: indicador de enemigos en Walk, con ruta, a más de 3 m de coste de su meta y sin reducir la distancia del campo durante 2 s; **no mide exclusivamente colas físicas** ni atribuye porcentajes a una causa. Conectividad no equivale a caudal o presión jugable.
- HORDA750: jefe 69,6 s frente a 18,7/15,3 s. El ensayo empieza con el jefe, **no con 750 enemigos**; puede incorporar sus esbirros. Las 20 escaladas son globales, no exclusivas del jefe. El criterio automático usa altura y estabilidad; no demuestra por sí solo llegada al polígono concreto o ataque al jugador. Causa exacta sin aislar; no atribuirla al presupuesto o colas masivas sin diagnóstico.
- Capturas cenitales lavadas por niebla; recogida de gemas/monedas sin comprobación vertical, pendiente aparte.

### Auditoría y continuidad

Inicio HEAD/remoto `369fdff`, índice vacío, solo siete ajustes Unity protegidos (cinco modificados y dos sin seguimiento). Fetch, revisión de Git/código/documentos, lectura de evidencias existentes y SHA-256; **sin suites, builds ni benchmarks nuevos**. U6 conserva `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`; 7/7 ajustes coinciden con la tabla A6; `u3-world.json` conserva `8ab0185cd3f29ea4bbd5e6e5d21fe09b6d32179f5061391ee765780904959787`. Sin cambios en runtime, referencias, src, balance o guardados. Procesos Claude abiertos no prueban escritura concurrente; no apareció WIP ajeno nuevo en Git.

Actualizados documentos de entrada, estado, continuidad, ambos README, plan histórico C, decisiones y hoja de ruta; historial fechado conservado. Publicado con push normal en `b755987` («Aprueba P0-C y actualiza el estado de continuidad»), remoto confirmado tras el push. **Siguiente paso exacto: preparar PLAN_P0D, comparar tamaños y navegación regional; esperar decisiones del autor antes de programar.**

## Plan P0-D preparado — relevo (12/10/2026, Codex)

Cierre formal de C publicado en `b755987`; aprobación manual registrada sin repetir pruebas. Creado [PLAN_P0D](PLAN_P0D.md), propuesta sin implementación: diagnóstico de U3/C, área jugable real, alternativas 256/320/384 m, navegación jerárquica por portales dirigidos y campos locales, cuerpos en costuras, memoria/caché, geometría/densidad, presión y presupuestos. Recomendación pendiente: D0–D4 en 128 m, revisar antes de 256 m y candidato final 384 m; integración jugable posterior separada. No cambia 3000 nodos/tick.

Investigación estática HORDA750 ampliada: `Boss.Step` tiene fases de aviso/recuperación, `EnemiesNav` las trata como quietud, y `EnemyNavigation.Move` no inicia escalada en quietud/carga. Puede haber esbirros del jefe; contador global y criterio de altura no aíslan la causa. Propuesta D0 de traza por ID y controles aislados, todavía no ejecutada. También identificados enumeración de tapas de todos los sólidos en `NavGraph.BuildNodes`, reciclado U3 a 85 m y bounds de render fijos: riesgos concretos para ampliar, no cambios autorizados ahora.

Comprobaciones reales en esta sesión: `git status --short --branch`, `git log -8 --oneline`, `git remote -v`, `git fetch origin`, lectura de código/XML/documentos, cálculo geométrico de áreas/tiempos ideales, SHA-256 de U6/u3-world/siete ajustes, revisión de diferencias documentales, enlaces locales y `git diff --check` limitado a Markdown. La comprobación global de whitespace detectó diferencias preexistentes en ajustes protegidos; no se corrigieron ni publicaron. **Sin tests Unity/web, benchmark ni builds nuevos.**

Solo documentos editados. Quedan fuera del índice los mismos siete ajustes protegidos; no se alteran src, referencias, runtime, balance ni guardados. El plan y estos enlaces se publican en el commit documental «Propone P0-D por regiones y compara la escala del mundo» (localizable con `git log -1 --format=%H -- docs/PLAN_P0D.md`; comprobar HEAD/remoto tras push). No interpretar estimaciones como resultados ni propuestas como decisiones aceptadas.

**Siguiente paso exacto para autor/Claude/Codex:** leer sección 15 de PLAN_P0D, recibir decisiones y autorización de las subfases elegidas. No implementar D ni integrar en U3 hasta entonces; C ya aprobado, no volver a solicitar su aprobación. P1/P2 excluidos.

## Cierre del diagnóstico P0-D0 — 14/10/2026

El autor confirma que Claude 20700/20264 pertenece a otro proyecto. No se cerró, suspendió ni modificó ninguno de esos procesos. HEAD/origin inicial `1503780404bc3c73920f058bcd8a0909f68a820c`, fetch normal verificado; trabajo local íntegro conservado. Además de 707/707 y catorce trazas, se encontraron dos arneses nuevos, scripts e informe con 715/715, 719/719 y 2/2 del 12/10: **todo heredado respecto a esta sesión**. Copia de XML/log previos en `D0/Resume-20261014`; los ensayos escriben carpetas nuevas, sin reemplazar trazas anteriores.

### Hallazgos y decisión pendiente

Detalle y condiciones en [DIAGNOSTICO_P0D0](DIAGNOSTICO_P0D0.md), especialmente su revalidación del 14/10. HORDA750/P0QA, dt 1/60, 128 m, 3000 nodos/tick, tres repeticiones por control más una sin observador. Cero incidentes geométricos observados no implica persecución correcta.

- Jefe fijo: altura 69,566667 s aún a 6,750814 m horizontales; llegada real 70,866667 s y recorrido 131,441313 m. Apoyo estable, error de superficie ≤0,01 m, diferencia vertical ≤0,2 m, distancia XZ dentro de suma de radios y segmento libre. Cuerpo L aislado: 27,116667 s y 92,732440 m. Todos los jefes suben por rampa, cero escaladas; las veinte heredadas eran de otros cuerpos.
- Jefe fijo: 51,183333 s con desplazamiento y 19,683333 s detenido. Fases, eje distinto: 34,366667 s espera/persecución y 36,5 s combate (15,933333 aviso, 6,166667 carga, 14,4 recuperación). Ruta L estática 94,347159 m frente a 73,475592 m S; jefe recorre además cargas/rodeos y revisita 25 nodos. No atribuir toda la diferencia a combate.
- Defecto mínimo reproducido: X double 21,499999735 pasa la validación de celda 21930 y se almacena float 21,5 en celda 21931, sin arista válida y con nodo anterior. El jefe sin esbirros queda inmóvil desde 21,183 s hasta 180 s; no falta campo ni hay horda. Jefe completo también presenta discrepancia y detención geométrica continua de 13,75 s.
- Propuesta **pendiente, no aplicada**: en `EnemyNavigation.TryStep`, convertir X/Z a float antes de `CellOf`, `WalkNeighbor` y `SurfaceY`, usando las mismas coordenadas para validar y almacenar. Rechazar el paso inválido con el deslizamiento existente; sin teleports, cambios de desvío ni balance. Tras decisión: pruebas de fronteras positivas/negativas/esquinas, coherencia nodo/posición y repetición D0/A/B/C. La prueba diagnóstica que espera el defecto deberá convertirse en una regresión que exija coherencia.
- Horda móvil: S/M/L 372/3/0 publicaciones en 180 s; latencias p95 0,633333/57,6/N/D s; edad p95 0,933333/111,966667/N/D s. La prioridad por población deja sin servicio suficiente a M/L. D3 contempla reparto y campos locales, sin parche a C ni aumento de presupuesto ahora.
- CPU Begin/Move con horda móvil p95 0,6433–0,6908 ms; máximo aislado 7,5127 ms. Arrays 27,38 MiB; heap Editor 426,55–443,86 MiB; residencia Windows del proceso 1352,04–1352,24 MiB. Contadores parciales y proceso completo claramente separados; no son FPS ni prueba de presupuesto residente de navegación. `QueueWaits` solo mide espera entre escaladores: no convertir cero en ausencia general de contactos al caminar.

**D0 cerrado como diagnóstico con límites explícitos; puerta de avance a D1 detenida por la corrección funcional heredada.** No se declara cumplido el rendimiento regional ni se anticipa D1. La autorización D1–D4 permanece; D5 y ampliación no iniciados. No se cambió runtime, balance ni navegación de producción.

### Pruebas realmente ejecutadas el 14/10

| Comando | Resultado nuevo | Evidencia |
| --- | --- | --- |
| `scripts\u3.cmd edit` | **721/721**, incluye diagnóstico mínimo y 12 controles × (3 observados + 1 sin observador) | `unity/TestResults/P0/D0/Verified-20261014/edit.xml`, `edit.log` |
| `scripts\u3.cmd play` | Primer intento: licencia Unity 198, sin pruebas; reintento fuera de sandbox **53/53** | mismo directorio: `play-first-attempt.log`, `play.xml`, `play.log` |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1` | U0/U2/U3/U4 coinciden; 8 guardados y 6 secuencias meta | `qa-results/reference-c8c403b5bc544ca9a788dd7923e9fea7` |
| `python scripts\p0-d0-report.py <carpeta D0/Diagnosis/20261014T…>` | 36 CSV de las 12 carpetas analizados; originales intactos | JSON de análisis junto a los CSV; `Verified-20261014/traces-sha256.json` |
| SHA-256 antes/después y `git diff --name-only -- src unity/Docs/Reference unity/Assets/Mamporro/Core` | 9/9 huellas iguales; diff vacío | `Resume-20261014/integrity-before.json`, `Verified-20261014/integrity-after.json` |

Sin build nueva: U6 intacto, no se ejecuta `u3.cmd build`. Sin benchmark de render nuevo. Memoria residente exclusiva de navegación, transitorios/caché y render standalone quedan pendientes; tampoco se demuestra una descomposición completa de contactos/separación al caminar. El pico CPU aislado queda registrado sin causa atribuida. No se usan métricas incompletas para aprobar la puerta regional.

### Commits, protección y continuación

`32ef0cb`: autorización D0–D4 y protecciones documentales recibidas. `427ca82`: arneses diagnósticos y scripts verificados. Este cierre documental se publica como «Documenta D0 reproducido y la corrección heredada pendiente». Push normal a `claude/zen-pasteur-674ik0`, sin PR ni force-push; comprobar HEAD remoto al terminar y registrar el resultado en la entrega.

Fuera del índice: exactamente los siete ajustes Unity protegidos (cinco modificados, dos no seguidos), con huellas idénticas a entrada y checkpoint. U6 SHA `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`; u3-world SHA `8ab0185cd3f29ea4bbd5e6e5d21fe09b6d32179f5061391ee765780904959787`. Src, generador U3, A/B/C, IDs y persistencia sin cambios.

**Siguiente paso exacto:** recibir decisión sobre la corrección concreta de `TryStep`, exigida por el autor antes de cambiar comportamiento aprobado C. No volver a pedir autorización general D0–D4. Tras aprobarla, aplicar solo esa corrección y validar antes de iniciar D1. No avanzar ante el defecto ni detenerse por los procesos Claude del otro proyecto.

## Cierre D1 — constructor regional físico (15/10/2026, Codex)

Base local/remota `5e4140633c4087d28d8ddbee36be383b5b89dd59`: corrección D0 publicada y evidencias heredadas 729/729 + 53/53 comprobadas. Se recibió implementación D1 local en `NavGraph`, `VerticalQueries`, `NavRegionGraph`, `RegionalGraphTests`, script y documento; conservada, revisada y validada en esta sesión. Sin cambios adicionales de movimiento, combate, tamaños, velocidades, desvío ni límite 3000. No se regeneró ninguna referencia ni build U6.

Pruebas nuevas ejecutadas el 15/10:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1`: **28/28**, 268,795 s; XML/log `unity/TestResults/P0/Regional-20261015T173655`. El intento previo del 14/10 falló por licencia 198; se repitió fuera del sandbox, sin cambiar licencia ni configuración.
- `scripts\u3.cmd edit`: **757/757**, 545,048 s; XML/log archivados en `unity/TestResults/P0/D1-20261015/edit.*`. Incluye regresiones A/B/C, D0 y regionales.
- `scripts\u3.cmd play`: **53/53**, XML/log en `unity/TestResults/P0/D1-20261015/play.*`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes; instantánea conservada `qa-results/reference-de5fc8c90d7d4f5dbcc627cc6b53dfb5`. El lanzador Edit también verificó las ocho transferencias web.
- `python scripts/p0-d0-report.py <carpeta>` sobre las doce carpetas nuevas `D0/Diagnosis/20261015T*`: **36 CSV**, cero discrepancias nodo/celda; controles repetibles y llegada sin esbirros 67,350/70,250 s fijo/móvil. Horda fija 71,983 s; móvil no llega, M p95 57,6 s y L sin publicaciones durante 180 s. No se oculta ni corrige la inanición en este bloque.
- Integridad final **9/9** igual a entrada y cierre D0: U6, u3-world y siete ajustes; JSON antes/después en `D1-20261015`. `git diff --check` del alcance y revisión del índice antes de publicar. Espacios heredados de ajustes Unity quedan intactos fuera del índice.

Medidas por baldosa/clase, límites y comparación de arrays en [REGIONES_P0D](REGIONES_P0D.md). Los halos aumentan el total de arrays respecto al monolítico; no son medidas completas de memoria residente ni FPS. No se elige aún 16/32/64 como tamaño final. Los cruces dinámicos, expulsión, prefeteo y pausas corresponden a D4, pendientes. No se implementaron D2–D4 en este cierre; permanecen autorizados, por orden, empezando por portales dirigidos sin falsas conexiones. D5–D7 siguen excluidos.

Publicación: commit pequeño «Valida el constructor regional físico en los circuitos pequeños», push normal a `claude/zen-pasteur-674ik0`; SHA comprobable por `git log -1` y `git rev-parse HEAD origin/claude/zen-pasteur-674ik0`. Al terminar deben quedar únicamente los siete ajustes Unity protegidos, sin incluirlos ni limpiarlos.

## Cierre D2 — 15/10/2026 (Codex)

Base inicial local/remota `c01070be3e71f1e2a31cf24fb91a41522b479643`; solo siete ajustes locales Unity recibidos, conservados. Código nuevo optativo en `NavPortals.cs` y pruebas `RegionalPortalTests.cs`, con metadatos, informe reproducible `scripts/p0-d2-report.py`. No se modificó D1 ni el backend activo. Arquitectura, mediciones, fallos intermedios y sus causas en REGIONES_P0D.

Comandos realmente ejecutados:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalPortalTests`: primera pasada 34/37 (expectativa de fixture no satisfecha tampoco por monolítico); nueva pasada **44/44** en `unity/TestResults/P0/Regional-20261015T224516`. Control unilateral separado 3/3 en `Regional-20261015T224319`.
- `scripts\u3.cmd edit`: **801/801**, 713,630 s. `scripts\u3.cmd play`: **53/53**. XML/log nuevos archivados en `unity/TestResults/P0/D2-20261015`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes; instantánea conservada `qa-results/reference-88830f7ef44246fd8b3e5b4325802206`.
- `python scripts/p0-d2-report.py unity\TestResults\P0\Regional-20261015T224516\edit.xml unity\TestResults\P0\D2-20261015\regional-summary.json`: informe de 108 casos por clase (90 de corpus A/B/C y 18 de fixtures), sin convertir rangos en percentiles ni lógica en FPS.
- Huellas entrada/salida **9/9** iguales. `git diff --exit-code c01070b -- src unity/Docs/Reference unity/Assets/Mamporro/Core/World/WorldCollision.cs unity/Assets/Mamporro/Core/World/VerticalCircuit.cs unity/Assets/Mamporro/Core/World/NavGraph.cs unity/Assets/Mamporro/Core/World/NavRegionGraph.cs unity/Assets/Mamporro/Core/World/EnemyNavigation.cs`: sin diferencias. Diff/índice revisados; ajustes excluidos.

Código: `7c50d9f`, «Añade portales físicos dirigidos y conectividad regional exacta». Cierre documental separado y push normal a la rama compartida. No se eligió tamaño definitivo ni se certificaron asignaciones, residencia completa o FPS. Los ceros históricos de asignación D0 están matizados por el fallo del contador testigo, sin ocultar los resultados originales. Siguiente D3: campo regional exacto y transferencias reutilizables con trabajo contado, sin monopolio de S; no introducir consulta offline síncrona en el tick. D3/D4 autorizados, D5–D7 excluidos.
