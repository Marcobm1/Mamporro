# P0-D3 — agrupación exacta y propagación regional por lotes

## Veredicto del experimento (18/10/2026)

**Negativo para los objetivos de D3.** La representación conserva todos los portales y el propagador conserva equivalencia exacta en las pruebas. Sin embargo, el mejor cambio vecino medido necesita **1 101 086 unidades agregadas**, frente a 54 000–90 000 disponibles en 0,3–0,5 s. A 64 m tarda hasta **6,133 s** con S/M/L activas. La reparación móvil p95 es **7,833 / 6,750 / 3,700 s**. No continuar construyendo sobre este candidato, no integrarlo en movimiento QA ni normal y no comenzar D4. El presupuesto permanece en 3000.

Se ejecutó la autorización del autor por puertas: A, reconstrucción exacta de grupos; B, equivalencia contra monolítico; C, scheduler y medidas. La evaluación incremental anterior y la jerarquía por salida siguen descartadas; este resultado tampoco resuelve la inanición física del jefe. No se amplió el mundo ni se cambiaron reglas, geometría, movimiento, RNG o persistencia.

## A. Representación sin pérdida

`NavPortalGroups` añade dos arrays (`Start`, `Members`) sobre el `NavPortalWorld` inmutable. Cada miembro es el índice de un portal original D2, sin deduplicación. Expandir los grupos recupera exactamente una vez cada portal y, por referencia, sus extremos, coste, clase, tipo, arista local y trayectoria. Ningún centro o representante sustituye a otro cruce.

Firma determinista: clase, región origen/destino, SCC interna origen/destino, Walk/Climb/Drop, signos X/Y/Z del desplazamiento, ordinal de capa en ambas celdas y soporte terreno/no terreno. Orden de grupos por firma ordinal; miembros por índice original. **Son lotes, no una afirmación de continuidad o equivalencia geométrica entre sus miembros.** No se fusionan superficies ni se deduce alcance entre miembros. Esto permite incluir cruces no contiguos conservando cada uno. Para Walk se mantiene esta opción conservadora de lote en lugar de convertir una ventana contigua en arista; para Climb/Drop se aplica la misma conservación íntegra. Altura y trayectoria exactas permanecen en D2, aunque no formen parte de la clave del lote.

Prueba nueva A: **30/30**, 123,778 s, `Regional-20261018T171314/edit.xml`. Incluye 90 combinaciones: A/B y ocho semillas C × 16/32/64 × S/M/L. Comprueba cobertura sin omisiones/duplicados, firma homogénea, identidad física y repetición determinista. Primer intento `171215`: licencia 198, sin pruebas. Informe: `unity/TestResults/P0/D3-Batch-20261018/groups.json`.

| Región | Portales → grupos, suma del corpus | Máximo construcción de grupos por caso | Máximo payload adicional |
| --- | --- | --- | --- |
| 16 m | 312 421 → 24 833 | 39,526 ms | 66 316 B |
| 32 m | 141 040 → 6 926 | 27,764 ms | 28 668 B |
| 64 m | 52 109 → 1 435 | 16,565 ms | 10 504 B |

HORDA750; p50/p95/máximo son miembros por lote, no tiempos:

| Región/clase | Portales → grupos | Miembros p50/p95/máx. | Grupos Walk/Climb/Drop | Portales Walk/Climb/Drop |
| --- | --- | --- | --- | --- |
| 16 S | 15058 → 1046 | 15/31/32 | 838/49/159 | 13928/213/917 |
| 16 M | 12737 → 1009 | 11/31/32 | 816/43/150 | 11816/160/761 |
| 16 L | 7970 → 732 | 8/31/32 | 692/6/34 | 7850/16/104 |
| 32 S | 6740 → 257 | 34/53/58 | 174/17/66 | 6244/76/420 |
| 32 M | 5935 → 280 | 19/47/54 | 210/13/57 | 5606/38/291 |
| 32 L | 3839 → 221 | 10/42/49 | 208/0/13 | 3816/0/23 |
| 64 S | 2296 → 54 | 12/102/106 | 34/4/16 | 2144/22/130 |
| 64 M | 2093 → 56 | 8/95/99 | 34/5/17 | 1986/12/95 |
| 64 L | 1486 → 60 | 28/51/53 | 58/0/2 | 1482/0/4 |

El JSON conserva construcción, payload, distribución y tipos de **cada** combinación, no solo HORDA750. El payload excluye objetos/listas temporales de construcción y el corpus D2 preexistente; no es memoria residente del proceso.

## B. Propagador exacto aislado

`RegionalBatchField` mantiene un heap indexado por región, distancias y siguientes aristas físicas por nodo. Solo se siembra inicialmente el nodo real de la meta. Una región recibe juntas las mejoras pendientes en sus extremos y propaga inversamente **aristas internas dirigidas reales**. Al vaciar su heap, recorre los portales entrantes y envía a los orígenes vecinos la etiqueta `D(destino) + coste(portal)`. No ejecuta una transferencia por cada salida. Los lotes estructurales sirven para construir el índice de portales; el lote de búsqueda reúne todas las semillas pendientes de una región, incluso de diferentes grupos.

Una cola FIFO determinista vuelve a abrir regiones cuando llega una mejora. Por ello no se declara que una única pasada regional baste: ciclos y rutas de salida/reentrada pueden requerir nuevas pasadas. La cola y todos los heaps deben estar vacíos antes de intercambiar los buffers publicados. Durante un cálculo el lector ve la versión anterior íntegra. Cada cambio de meta reconstruye etiquetas; **no es reparación incremental g/rhs**. Repetir exactamente la meta publicada no genera trabajo. La geometría/clase pertenece a una instancia inmutable; no hay invalidación geométrica ni política de residencia D4.

Justificación de exactitud: cada etiqueta finita se obtiene concatenando aristas reales hasta la meta; nunca introduce una ruta inexistente. Las mejoras reabren el origen de cada cruce, y cada procesamiento local aplica las relajaciones internas hasta agotar el heap. Al terminar no queda ninguna arista que pueda mejorar una etiqueta. Con costes positivos, estas etiquetas son las distancias mínimas; los padres óptimos descienden estrictamente de coste y terminan en la meta. Esto no depende de conectividad única por baldosa, simetría de caídas o un grid de una sola capa.

Prueba nueva B: **33/33**, 290,033 s, `Regional-20261018T173419/edit.xml`; intento `171813` sin pruebas por licencia 198. Las 30 combinaciones del corpus se prueban con las tres clases y varias metas (distribuidas, vecinas y de mayor altura). Se compara **cada distancia/alcance** contra relajación por cola independiente sobre el grafo monolítico. Cada siguiente arista debe ser la primera arista canónica entre las opciones óptimas del oráculo; se aceptan empates de coste mediante ese desempate explícito, sin exigir la selección accidental del heap histórico. Se comprueba identidad de arista/trayectoria D2, coste estrictamente decreciente y se reconstruyen además rutas completas desde 16 orígenes distribuidos por meta. Las fixtures prueban reentrada obligatoria, componentes distintos, superficie suspendida inaccesible, varias alturas por XZ, escalada/caída físicas, rampa diagonal y paso S/M cerrado a L. D1/D2 conservan además sus pruebas exhaustivas de geometría/aristas en la regresión completa.

## C. Scheduler, trabajo y latencia

El adaptador `Scheduled` vive **solo en `RegionalBatchTests`**. No hay referencia desde `EnemyNavigation`, QA ni partidas normales. Reparte las tres clases mediante el `RegionalNavScheduler` existente, 3000 globales/tick, 60 Hz simulados. La CPU mide peticiones y scheduler; el desglose por clase mide `Advance`. Las aserciones/oráculo y el volcado del informe quedan fuera de la medida CPU.

Se cobra una unidad por registro de inicialización, registro de construcción del índice inverso, extracción, arista interna examinada, semilla, portal examinado/propagación, apertura de lote, publicación y transición administrativa. Las semillas interregionales tienen su paso separado; no se ocultan dentro de otro trabajo. El heap incluye sus comparaciones O(log V) dentro de la operación, igual que el contrato previo; se mide su CPU. No hay copias mundiales al publicar: intercambio de cuatro referencias, una unidad. `WorkUnits` coincide exactamente con la suma de categorías y con el trabajo devuelto por el scheduler. El JSON conserva unidades/búsquedas/semillas por región; el pequeño control global restante no se atribuye artificialmente a una región.

**Límite de la medida fría:** mundo físico D2, agrupación estructural y reserva de buffers se preparan en carga. La construcción de grupos se mide en A y la reserva del solver en `setupMs`; esos tiempos no se presentan como trabajo de tick. A diferencia de los controles previos, la construcción de índices inversos del solver sí se ejecuta y cobra en su primera solicitud. La latencia fría tabulada empieza con ese mundo residente, no incluye cargar/generar el mundo. Añadir la preparación excluida solo empeoraría el resultado negativo; no se usa para certificar cumplimiento de una primera visita completa.

Mediciones nuevas C: primera pasada **3/3**, 62,106 s (`Regional-20261018T195217`); versión con CPU por clase **3/3**, 59,853 s (`Regional-20261018T195506`). Tres repeticiones por tamaño; contadores y latencias deterministas, CPU variable. Seis modos: frío, misma meta (cero trabajo), vecino, altura máxima, retorno, móvil durante 10800 ticks. Móvil alterna dos vecinos Walk con retorno cada 12 ticks, como el control incremental; se termina la versión en curso y luego se atiende la meta vigente. Las esperas posteriores para verificar la última versión pendiente no se mezclan con los 180 s. No hay enemigos, combate, render ni FPS.

| Región/clase | Unidades primera publicación | Latencia fría (s) | Unidades vecino | Latencia vecino (s) | Móvil unidades p95 por publicación | Móvil latencia p95 (s) |
| --- | --- | --- | --- | --- | --- | --- |
| 16 S | 1262785 | 16,800 | 597374 | 7,883 | 597374 | 9,967 |
| 16 M | 1113529 | 16,633 | 520563 | 7,767 | 520563 | 8,683 |
| 16 L | 647273 | 10,800 | 300687 | 5,017 | 300687 | 5,017 |
| 32 S | 1267684 | 16,283 | 504005 | 6,517 | 547933 | 9,050 |
| 32 M | 1097783 | 16,083 | 440632 | 6,467 | 481071 | 8,017 |
| 32 L | 565451 | 9,433 | 226607 | 3,783 | 226607 | 3,783 |
| 64 S | 1182291 | 15,300 | 472367 | 6,133 | 472367 | 7,833 |
| 64 M | 1014301 | 15,000 | 406858 | 6,017 | 406858 | 6,750 |
| 64 L | 555932 | 9,267 | 221861 | 3,700 | 221861 | 3,700 |

| Región | Frío agregado | Vecino agregado | Altura agregada / máximo hasta publicar | Móvil publicaciones S/M/L | Móvil edad p95 S/M/L (s) |
| --- | --- | --- | --- | --- | --- |
| 16 | 3023587 | 1418624 | 1185806 / 6,600 s | 19/21/36 | 18,183/16,250/9,717 |
| 32 | 2930918 | 1171244 | 1054394 / 5,867 s | 20/23/47 | 16,800/14,983/7,350 |
| 64 | 2752524 | 1101086 | 896051 / 4,983 s | 23/26/47 | 15,117/13,050/7,217 |

Móvil utiliza **32 400 000 unidades agregadas por tamaño y repetición**: todos los ticks consumen 3000. L publica el cambio de altura en 0,367 s, pero esa meta está en una componente muy pequeña para L (dos lotes); no demuestra reacción global ni cumplimiento de S/M/L. La edad móvil se mide después de la primera reparación publicada, sin arrastrar el tiempo del ensayo fijo.

## CPU, memoria y coste restante

CPU del arnés con desglose final, tres repeticiones (`195506`); intervalos de CPU total y mayor p95/máximo de tick observado:

| Región | Frío CPU total (ms) | Vecino CPU total (ms) | Móvil CPU total 180 s (ms) | Móvil tick p95 máximo / máximo (ms) |
| --- | --- | --- | --- | --- |
| 16 | 29,611–30,105 | 22,018–22,369 | 477,521–578,177 | 0,0771 / 0,9051 |
| 32 | 29,182–34,214 | 19,747–21,593 | 527,918–531,906 | 0,0663 / 0,2274 |
| 64 | 27,941–29,007 | 20,770–20,942 | 592,679–602,892 | 0,0742 / 0,1354 |

CPU por clase de la primera repetición (las tres se conservan en JSON), y payload retenido de solver + grupos:

| Región/clase | CPU fría / vecino / móvil (ms) | Payload (B) |
| --- | --- | --- |
| 16 S | 12,807 / 9,686 / 199,793 | 2836040 |
| 16 M | 10,536 / 8,406 / 199,425 | 2440196 |
| 16 L | 5,765 / 4,183 / 175,646 | 1363308 |
| 32 S | 13,933 / 8,834 / 194,672 | 2763844 |
| 32 M | 11,687 / 7,367 / 179,279 | 2380368 |
| 32 L | 5,085 / 3,472 / 152,907 | 1325720 |
| 64 S | 12,731 / 9,079 / 209,483 | 2726856 |
| 64 M | 11,020 / 7,983 / 207,431 | 2348112 |
| 64 L | 5,075 / 3,809 / 173,686 | 1305628 |

La memoria excluye geometría/grafos D1/D2, cabeceras, transitorios y memoria de Unity; no certifica residencia total. **Asignaciones N/D:** el contador no detecta el testigo de 4096 B, en los 54 escenarios; no afirmar cero GC. La consulta a la misma meta tiene cero unidades, pero esa reutilización no ayuda cuando cambia la meta. El solver retiene los índices al recalcular; no hay caché de soluciones para objetivos distintos.

Perfil vecino a 64 m: inicializaciones S/M/L **43189/37380/21034**; extracciones **43786/38001/21002**; aristas internas **333633/286237/153469**; semillas **3975/3608/2436**; portales examinados **3974/3607/2891**; búsquedas regionales **7/7/8**. A 16 m son **111/100/111** búsquedas, a 32 m **24/24/25**. El índice frío añade **710845/609010/334164** unidades a 64 m; no está escondido en el constructor.

Semillas aceptadas acumuladas por lote (puede mejorar varias veces el mismo extremo antes de procesarlo): frío 16 m **5139/4812/3413** en 94/90/111 lotes; 32 m **2780/2558/1278** en 23/25/25; 64 m **867/745/559** en 6/6/7. El JSON conserva la distribución por región y máximo acumulado de semillas de un lote (`batchSeedMaxLifetime`, no percentil). Intentos de siembra y mejoras aceptadas son contadores distintos.

La causa restante es reconstruir campos completos: solo inicializar los **101591 nodos S/M/L** ya excede 90000 unidades con este diseño; las aristas internas dominan después. Se ha eliminado la explosión por salida, pero no el coste mundial. 64 m gana en trabajo de este ensayo, no queda elegido para producción.

## Comparación y límites de aceptación

- `RegionalPortalField`: evidencia anterior, cero publicaciones en 180 s, 10800000 unidades por clase. El lote ahora publica todas las clases, pero sigue muy lejos del límite. No atribuirle una aceleración de una latencia previa censurada como si fuera un tiempo final medido.
- `IncrementalNavField`: primera publicación anterior 6,217/5,950/3,633 s; móvil p95 28,067/24,067/16,300 s. El nuevo frío incluye indexación que aquel construía en carga, por lo que no es comparación homogénea del arranque. En móvil, el mejor 64 m mejora latencia aproximadamente **3,58×/3,57×/4,41×**, insuficiente. Tampoco alcanza la mejora de un orden de magnitud frente a ese control.
- Oráculo monolítico: comparación nueva de alcance, coste y trayectoria sobre los mismos mapas/metas. El control de producción con 750 móvil (M p95 57,6 s, L sin publicaciones) usa enemigos/metas y contabilidad distintos: **no calcular una mejora de persecución ni CPU relativa entre ambos arneses**. Se conserva como evidencia de bloqueo, no solución.

Cumplido: conservación física, determinismo probado, equivalencia del corpus, publicación atómica, servicio de las tres clases en el arnés y techo global 3000. Incumplido: reacción 0,3/0,5 s y edad móvil, tanto por clase como agregadas. No verificado por este experimento: persecución física, integración, residencia completa y cero asignaciones. D3 permanece bloqueado; D4 no iniciado. No se propone convertirlo en campos locales definitivos.

## Alternativas para revisión del autor

El resultado descarta este candidato, no demuestra que toda solución exacta sea imposible. No se implementa ninguna alternativa en este cierre:

1. **Consulta exacta bajo demanda para consumidores activos:** evitar reconstruir todas las distancias del mundo ante cada meta; compartir búsquedas y certificar únicamente las rutas solicitadas. Es la propuesta preferida para estudiar, con riesgo de multiplicar trabajo con 750 consumidores dispersos. Requiere acordar el contrato de publicación y medir cobertura, espera máxima por clase y exactitud antes de integrar; una respuesta todavía no calculada no puede interpretarse como ausencia de ruta.
2. **Preprocesado exacto de distancias regionales:** estudiar una representación reutilizable de transferencias que reduzca trabajo por cambio de meta. Debe medir primero tamaño, tiempo de carga y coste de actualización; una matriz completa de extremos puede ser demasiado grande. No se presupone que comprimir portales permita eliminar extremos físicos equivalentes solo en apariencia.
3. **Revisar el contrato de reacción o trabajo:** decisión explícita del autor, no aplicada. La CPU baja del arnés no autoriza subir 3000, cambiar qué cuenta como unidad ni relajar 0,3/0,5 s. Las aproximaciones también necesitarían una tolerancia aprobada y nueva validación física.

Siguiente paso: revisión del autor y elección de un experimento acotado. No integrar este candidato ni empezar D4/D5.

## Reproducción y cierre

Cierre del 19/10: código `9922599` publicado sin cambios funcionales al recuperar el trabajo local. Edit completo del 18/10 **960/960** comprobado como heredado, con 66/66 del experimento; su informe reproduce exactamente los contadores y latencias de las 162 filas específicas. Play **nuevo 53/53**, referencias U0/U2/U3/U4 nuevas coincidentes, protecciones 9/9 y corpus de referencia 11/11 intactos. Evidencias archivadas en `unity/TestResults/P0/D3-Batch-Cierre-20261019`; comandos, tiempos y publicación documental en PROGRESO_P0. Los siete ajustes Unity quedan fuera de los commits. No hay build nueva ni integración; parada para revisión.

Desde CMD en la raíz:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalBatchTests -Category BatchGrouping
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalBatchTests -Category BatchEquivalence
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalBatchTests -Category BatchBudget
python scripts\p0-d3-batch-report.py unity\TestResults\P0\D3-Batch-20261018\budget-specific.json unity\TestResults\P0\Regional-20261018T195506\edit.xml
```

Los XML/logs y JSON se conservan localmente bajo `unity/TestResults/P0`; los scripts y esta síntesis quedan versionados. Evidencias previas preservadas. Regresión completa, referencias, integridad y commits: consultar el primer punto vigente de [PROGRESO_P0](PROGRESO_P0.md). Solo se declararán terminados con sus resultados reales.
