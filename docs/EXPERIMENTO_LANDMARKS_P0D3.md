# P0-D3 — A* exacto con landmarks dirigidos

## Resultado — 20/10/2026

**Revalidación del 21/10/2026:** recibido este instrumento y el informe como trabajo local sin publicar sobre `27414c5`, conservados sin cambios funcionales. Repetida la suite completa: **12/12**, 48,326 s, `unity/TestResults/P0/Regional-20261021T174638/edit.xml` y `edit.log`. Comando: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.LandmarkGateTests`. Primer intento `Regional-20261021T174624`: licencia 198, sin pruebas; repetición fuera del sandbox.

Extractor ejecutado: `python scripts/p0-d3-landmark-report.py unity/TestResults/P0/Regional-20261021T174638/edit.xml unity/TestResults/P0/D3-Landmarks-20261021/gate.json`. Comparación con `gate-first.json` del día 20: **27 preparaciones, 18 comparaciones y 18 cohortes idénticas en todos sus campos deterministas** (trabajo, consultas, extracciones, tamaños y payload; tiempos/muestras de memoria excluidos). Preparación completa nueva a 64 m, K=2/4/8: **3,697–3,716 / 3,782–3,807 / 3,954–3,986 s**; margen escaso respecto a 4 s, sin certificación de p95. Memoria observada máxima managed/Unity: **185315328 / 354866857 B**, muestras del arnés, no picos certificados. CPU nueva de la mejor cohorte dispersa K=8: **59,650 ms** offline, no CPU por tick ni FPS.

Se confirma la puerta negativa por los mismos **9889018** (dispersos K=8) y **651465** (concentrados K=4) de trabajo. No se ejecuta Puerta 2 ni se repiten las cargas del servicio anterior. Las tablas siguientes conservan las medidas del día 20. Huellas antes/después nuevas **9/9** y corpus **11/11** iguales en `D3-Landmarks-20261021`; diffs protegidos de Core/U3/src/referencias vacíos. Sin Edit completo, Play, build ni verificador histórico nuevos; las referencias ejecutadas el día 20 son evidencia heredada. Siete ajustes locales Unity intactos y excluidos de commits.

**Puerta 1 negativa por trabajo agregado; no abrir Puerta 2.** Autorización del autor desde `27414c5`: evaluar 2/4/8 referencias por clase y detenerse si la mejora no justifica el prototipo. Se ha añadido exclusivamente un instrumento offline en la asamblea de tests, sin cambios en Core, EnemyNavigation, QA activo, generador, balance, RNG, persistencia o web. Sin investigación avanzada.

Hay una mejora real: con 8 referencias las muestras dispersas extraen **19,51 veces menos nodos** que Dijkstra individual y consumen **8,32 veces menos trabajo**, incluyendo evaluar la heurística. Pero atender la cohorte de 750 dispersos para una meta vecina nueva requiere **9889018 unidades** frente a **1992760** del servicio bajo demanda compartido anterior, con los mismos orígenes y meta. Supera en **109,88 veces** las 90000 unidades disponibles en 0,5 s a 60 Hz. Incluso el mejor concentrado (4 referencias) requiere **651465**, frente a 57764 anteriores.

Esto no demuestra que toda combinación futura de A* y compartición vaya a fallar. La puerta mide búsquedas independientes con tablas compartidas y búfer reutilizado; **no hay caché de rutas ni servicio multiusuario nuevo**. Los millones de unidades no ofrecen margen para justificar construirlo ahora. No se han convertido esos contadores en latencias p95 de un scheduler inexistente. La pérdida de compartición respecto a Dijkstra inverso es precisamente el riesgo detectado.

## Contrato matemático y físico

Para referencia L, nodo v y meta t:

`h(v,t) = max(0, d(L,t)-d(L,v), d(v,L)-d(t,L))`, tomando el máximo sobre todas las referencias.

Cada término se incluye **solo si sus dos distancias son finitas**. No se resta infinito, no se presupone simetría ni se interpreta ausencia de un término como inaccesibilidad.

Por desigualdad triangular dirigida, `d(L,t) ≤ d(L,v)+d(v,t)` y `d(v,L) ≤ d(v,t)+d(t,L)`: ambos términos son cotas inferiores. Su máximo con cero sigue siendo admisible. Distancias desde L se obtienen por Dijkstra directo; hacia L por Dijkstra sobre el índice inverso, conservando el coste y el ID de cada arista original. Las diferencias usan enteros finitos; `g+h` usa `long`. Se detiene al extraer la meta; se permiten reaperturas. Omitir términos infinitos puede afectar a la consistencia en zonas sin camino a la meta, por lo que no se usa un cierre irreversible.

Cada tabla se certifica durante preparación: raíz cero, padre físico con coste exacto y distancia estrictamente menor para todo nodo finito, y desigualdad de relajación en todas las aristas. Así hay un camino testigo y ningún atajo pendiente, incluyendo cierre de los inalcanzables. Para las metas muestreadas se verifica además `h ≤ distancia monolítica` en todos los nodos alcanzables.

Se usan mundos físicos D2 separados por S/M/L. Ninguna arista, coste, capa, altura ni trayectoria Walk/Climb/Drop se sustituye. A* guarda IDs de aristas; la ruta reconstruida mantiene su dirección. Puede elegir otra ruta de igual coste por desempate, sin promesa de la misma cadena canónica del servicio anterior.

Selección determinista: primera referencia por menor identidad física; siguientes por máxima separación geométrica mínima respecto a las anteriores (XYZ, desempate por identidad física). Esta distancia geométrica **solo selecciona referencias**, nunca se usa como cota de coste. Los conjuntos 2/4 son prefijos de 8. No se afinó la selección después de ver resultados.

## Preparación completa y memoria

Nueve casos: HORDA750 × 16/32/64 × tres repeticiones. En cada caso se prepara el circuito completo (`ProceduralMap` con generación, grafos monolíticos, validación y reparaciones existentes), después los tres mundos D2; a continuación se preparan **independientemente** 2/4/8 referencias para S/M/L. No se reutilizan tablas de K anteriores ni archivos externos. El tiempo de la base se suma a cada candidato K; no se multiplica el coste por las tres alternativas como si fueran un único producto.

El cronómetro de landmarks incluye selección, reservas, índice inverso, Dijkstra en ambas direcciones, almacenamiento y certificación de tablas. El tiempo completo incluye todo el circuito y D2, no solo ese incremento.

| Región | K por clase | Landmarks S/M/L total (ms, rango) | Preparación completa (ms, rango) |
| --- | --- | --- | --- |
| 16 m | 2 | 90,762–92,580 | 4729,255–4780,854 |
| 16 m | 4 | 175,501–191,384 | 4828,086–4866,832 |
| 16 m | 8 | 329,415–352,602 | 4968,105–5040,876 |
| 32 m | 2 | 95,638–97,531 | 3980,111–4025,229 |
| 32 m | 4 | 171,866–186,386 | 4056,339–4113,129 |
| 32 m | 8 | 348,073–364,915 | 4241,674–4275,771 |
| 64 m | 2 | 90,262–93,330 | 3597,686–3662,712 |
| 64 m | 4 | 172,595–175,016 | 3679,372–3746,421 |
| 64 m | 8 | 332,337–365,327 | 3862,018–3904,787 |

Comparación con PLAN_P0D: a 64 m **incluso el tiempo completo** está por debajo de 4 s en las tres muestras de cada K; todas las alternativas quedan por debajo del máximo de 6 s y del objetivo de carga total visible de 6 s. Esto permite considerar favorable el tiempo inicial a 64 m, no certificar un p95 de producción con tres muestras. A 16/32 el total mezcla geometría y navegación: no se atribuye todo su exceso sobre 4 s exclusivamente a navegación. El campo `navigationMs` del JSON contiene **D2 + landmarks solamente**; excluye los grafos/validación monolíticos incluidos en `circuitMs`, por lo que tampoco se usa para declarar cumplimiento completo. No se eligió tamaño definitivo.

Trabajo contado de **construcción del instrumento de landmarks**, idéntico entre tamaños/repeticiones: **14397899 / 25468883 / 47610851** unidades para K=2/4/8. Incluye inicialización de arrays, índice inverso, selección, búsquedas, copias y certificados. La construcción física/generación histórica no dispone aquí de un contador de unidades; su tiempo sí está íntegramente incluido. No presentar estas cifras como contabilidad universal de construcción del mundo ni ejecutar ese trabajo gratis durante juego.

Nodos totales S/M/L: 101591. Tablas de distancias `8 × K × ΣN` B (ida/vuelta, enteros de 4 B): **1,550 / 3,100 / 6,201 MiB**. Payload retenido del instrumento, incluyendo índice inverso y seis arrays de búsqueda reutilizables: **7,202 / 8,752 / 11,852 MiB**. A 64 m, sumando arrays físicos D1 y topología/portales D2: **41,608 / 43,159 / 46,259 MiB**.

Estos recuentos **no son residencia total**: excluyen cabeceras, listas, geometría y los grafos monolíticos que conserva el circuito/arnés. Temporales explícitos vivos adicionales del constructor de una clase: hasta **518224 B** (cursor del índice y selección geométrica), sin certificar pico de transitorios retenidos hasta GC. Managed observado al final de las preparaciones **159395840–186912768 B**; Unity asignado observado hasta **355919711 B**, ambos incluyen Editor/arnés y anteriores candidatos pendientes de GC. Son muestras, no picos certificados. Por ello no se certifican globalmente los techos 160 MiB residentes/96 MiB transitorios/256 MiB managed/512 MiB Unity aunque estas observaciones no los contradigan. Asignaciones verificables: **N/D**, no se afirma cero GC.

## Exploración y metas nuevas

Consultas en HORDA750, partición 64 m, S/M/L, 2/4/8 referencias. Se conserva el muestreo de `DemandNavLoadTests`: orígenes por identidad física, concentrados en radio 8 m y altura próxima al inicio o dispersos por el mapa. Muestra por clase/distribución: 16 orígenes × cinco metas (inicio, vecino, pasos 15 y 31 del mismo recorrido de 32 nodos Walk bidireccionales, y nodo de mayor altura).

Son **instantáneas de metas que cambia el recorrido**, no un servicio siguiendo movimiento continuo. Se comparan 1440 consultas A* y 1440 Dijkstra, además de la cohorte siguiente; en todas se contrastan costes con el oráculo monolítico por cola. Rutas completas de todas las consultas A* verificadas. No hubo inalcanzables en esa muestra HORDA750; se añaden fixtures explícitas para no aparentar esa cobertura.

Ambas búsquedas usan el mismo heap indexado, inicialización perezosa por generación y desempate; Dijkstra usa h=0. Se cobra cada registro inicializado, extracción, examen de arista, intento de relajación/semilla, término dirigido de heurística y control de solicitud/publicación. Los dos términos por referencia se cobran separadamente. Las comparaciones O(log V) del heap forman parte de inserción/extracción y su CPU, como en los candidatos históricos. Oráculos y validación de rutas quedan fuera de la CPU de búsqueda.

Agregado de las 240 consultas **dispersas** por K, S/M/L:

| K | Extracciones A* | Extracciones Dijkstra | Reducción extracciones | Trabajo A* | Trabajo Dijkstra | Reducción trabajo |
| --- | --- | --- | --- | --- | --- | --- |
| 2 | 888953 | 3259581 | 3,67× | 19220201 | 56063497 | 2,92× |
| 4 | 345975 | 3259581 | 9,42× | 9328319 | 56063497 | 6,01× |
| 8 | 167068 | 3259581 | 19,51× | 6738363 | 56063497 | 8,32× |

CPU total de esas consultas: A* **165,211 / 73,674 / 44,212 ms**, Dijkstra **464,900 / 481,913 / 468,590 ms**. Una ejecución de consultas por K, no tres repeticiones de CPU ni percentiles. Con K=8, evaluar términos supone **3842960 unidades** del agregado disperso: ahorrar extracciones no equivale al mismo ahorro de presupuesto. Las concentradas agregadas mejoran 3,15/9,66/18,64 veces en trabajo, pero también incluyen la meta alta lejana.

## Comparación equivalente con demanda compartida

Cohorte de **750**, distribución S/M/L **600/149/1**, mismos orígenes y meta `walk[1]` que el cambio vecino caliente del [informe de demanda](EXPERIMENTO_DEMANDA_P0D3.md), a 64 m. Esas 4500 consultas A* adicionales (750 × dos distribuciones × tres K) verifican coste y ruta. El caso L representa la clase corporal del jefe, **no su comportamiento físico**.

| Distribución | K | Trabajo A* agregado | CPU búsquedas (ms) | Trabajo demanda compartida heredado | Cociente A*/demanda |
| --- | --- | --- | --- | --- | --- |
| Concentrados | 2 | 1770738 | 10,846 | 57764 | 30,65× |
| Concentrados | 4 | 651465 | 3,448 | 57764 | 11,28× |
| Concentrados | 8 | 914455 | 3,741 | 57764 | 15,83× |
| Dispersos | 2 | 43091681 | 356,396 | 1992760 | 21,62× |
| Dispersos | 4 | 13762039 | 102,164 | 1992760 | 6,91× |
| Dispersos | 8 | 9889018 | 54,150 | 1992760 | 4,96× |

Las referencias históricas proceden de las tres ejecuciones del 19/10, **no se han rehecho**. Los inputs son equivalentes, pero los mecanismos y desgloses difieren: demanda comparte Dijkstra inverso y cobra mantenimiento/cola; el instrumento A* no tiene cola, reutiliza solo tablas y arrays y cobra la heurística. No convertir estos cocientes en una aceleración universal ni comparar sus milisegundos de búsqueda con CPU por tick del servicio.

El mejor caso disperso suma **3297 ticks de trabajo** a 3000 si se ejecutaran esas búsquedas independientes, sin mantenimiento ni reconstrucción/publicación de rutas multiusuario; **no es una latencia observada**. El mejor concentrado suma 218. Son incompatibles con usar este esquema directo para completar la cohorte dentro de 18/30 ticks (0,3/0,5 s). Una caché de rutas podría cambiar el trabajo, pero requeriría otra evidencia de compartición exacta para metas nuevas. Alternar dos metas ya cacheadas no probaría esa propiedad.

Respecto al preprocesado regional anterior: el incremento de preparación baja de unos 0,8–2,4 s de transferencias completas a 0,09–0,37 s de landmarks, con tablas mucho menores. La representación es prometedora como información auxiliar; **no resuelve por sí sola el servicio requerido**.

## Pruebas, evidencias y límites

- **Nuevas 9/9**, 45,476 s, `unity/TestResults/P0/Regional-20261020T175249/edit.xml`: 27 preparaciones, 18 comparaciones agregadas y 18 filas de cohorte. Primer intento `175240`: licencia 198, sin pruebas; repetición fuera del sandbox. El éxito del test significa medición/contraste correctos, no aceptación de rendimiento.
- **Nuevas 3/3**, 1,942 s, `Regional-20261020T175616/edit.xml`: S/M/L con caída sin retorno, capas inaccesibles, cotas no negativas/admisibles, costes y rutas frente al monolítico; K=2/4/8. Añadidas después de la medición, sin cambiar el kernel ni el arnés de rendimiento.
- Extractor `scripts/p0-d3-landmark-report.py`: comprueba cantidad de filas, determinismo de trabajo/memoria de preparación y tamaños de cohorte; `unity/TestResults/P0/D3-Landmarks-20261020/gate-first.json`. Hashes y protecciones en esa carpeta; cierre en PROGRESO_P0.
- Referencias históricas nuevas U0/U2/U3/U4 coincidentes: instantánea `qa-results/reference-9bc6ebeabda64d769e969a4960fd9349`. No Edit completo, Play ni build nuevos: solo código de tests. Las 71 pruebas del experimento anterior y las cargas de demanda siguen siendo **heredadas**.
- Huellas antes/después **9/9** y referencias **11/11** intactas. Siete ajustes Unity excluidos de commits; diffs frente a `27414c5` vacíos en src, referencias, Core y U3. No se restauró ni eliminó ningún cambio local.

Comandos desde CMD:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.LandmarkGateTests -Category LandmarkGate
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.LandmarkGateTests -Category LandmarkCorrectness
python scripts\p0-d3-landmark-report.py unity\TestResults\P0\Regional-20261020T175249\edit.xml unity\TestResults\P0\D3-Landmarks-20261020\gate-first.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

Puerta 2 **no iniciada** por la condición de parada. No corpus completo A/B/ocho C × tres tamaños para un nuevo servicio, ni simulaciones 300/500/750 con scheduler. Cobertura vigente, pendientes, obsoletas, latencias p50/p95/p99, asignaciones verificadas y FPS de ese servicio: **N/D**. Ni los tiempos offline ni los ticks calculados prueban persecución física o justicia S/M/L. El scheduler existente y sus 3000 globales/tick permanecen intactos; no se promete cumplir reacción.

## Decisión pendiente del autor

**Recomendación: no abrir Puerta 2 de este candidato con estas cifras.** Si se decide continuar en otro experimento, debería demostrar primero compartición exacta para una cohorte y una meta nueva, conservando la cota admisible como auxiliar, antes de añadir caché/FIFO y repetir cargas largas. Alternativa: revisar selección de referencias y dirección de búsqueda, pero aumentar K por sí solo ya empeora el concentrado y no compensa repetir trabajo entre consumidores. Son propuestas sin implementar; ninguna garantiza cumplir.

No volver a los candidatos descartados ni relajar 3000, 0,3/0,5 s, física o memoria. **Detenerse para revisión, sin integración ni nuevo experimento. D3 bloqueado; D4/D5 y ampliación excluidos.**
