# P0-D3 — viabilidad del preprocesado regional exacto

## Resultado y alcance — 20/10/2026

**Puerta inicial negativa para el candidato medido; no implementar el prototipo de servicio.** Desde `f355409`, el autor autorizó evaluar primero preparación, memoria y cambios de meta, y detenerse si no cabían en los presupuestos. Se ha ejecutado únicamente un diagnóstico offline en tests, reutilizando `RegionalTransferCache` sin modificarlo. Sin investigación avanzada, cambios de producción, integración, D4/D5 ni ampliación del mundo.

HORDA750, S/M/L sumadas, regiones 16/32/64 y tres repeticiones: construcción física/topológica más cálculo de todas las transferencias internas hacia extremos de salida requiere **4,413–4,967 s**. Todas las muestras superan el objetivo de preparación p95 ≤4 s, aun excluyendo geometría, almacenamiento de tablas y validación. No se rebasa el máximo de 6 s en estas muestras: el rechazo es por el objetivo de 4 s, no por inventar una infracción del máximo. Tres muestras por tamaño son un diagnóstico inicial, no una certificación estadística de p95.

Esto descarta continuar con este preprocesado completo usando el motor actual bajo la puerta solicitada. **No demuestra imposibilidad de cualquier arquitectura exacta**, de una implementación especializada ni de una build optimizada. Tampoco permite aceptar el candidato basándose en que su preparación parcial quede por debajo de 6 s.

## Por qué falló la demanda anterior

Inspección nueva de `DemandNavService.Queue`, `Admit`, `Scan` y `Advance`, contrastada con las medidas **heredadas** de [demanda](EXPERIMENTO_DEMANDA_P0D3.md):

- Dijkstra inverso comparte trabajo para una meta, pero certificar orígenes lejanos/dispersos exige expandir gran parte del grafo. Un origen inalcanzable exige agotar la búsqueda. No es un problema que se arregle eligiendo otro tamaño de baldosa: ese servicio recorre la unión física.
- `Queue` congela origen/meta/fecha incluso antes de conseguir una entrada de búsqueda. La FIFO puede bloquear solicitudes posteriores si sus dos entradas siguen ocupadas por usuarios de metas anteriores. Evitar cancelación asegura progreso, pero no frescura.
- Al recorrer más de dos metas, las etiquetas salen de caché; los consumidores terminan peticiones obsoletas y vuelven a pedir la vigente. Publicar más no implica mejorar cobertura vigente. El barrido de consumidores también consume presupuesto, especialmente en S.
- Las cuotas evitan monopolio de clase; no reducen el trabajo para certificar rutas. El ensayo móvil anterior consumía 3000 en todos los ticks y terminaba con 750 pendientes. CPU pequeña por tick y percentiles buenos de alternancia entre dos metas no rescatan el recorrido móvil.

No se ha afinado ni reejecutado ese candidato descartado. Sus latencias/coberturas siguen siendo resultados del 19/10, no nuevos.

## Medición de preparación

`RegionalPreprocessFeasibilityTests`, categoría `PreprocessGate`, construye los tres mundos físicos D2 y cuenta extremos únicos por nodo físico y región. No fusiona alturas, clases, SCC ni conexiones con distinto sentido. Los portales Walk/Climb/Drop se conservan y cuentan por separado en JSON. Para cada extremo de salida calcula la transferencia **inversa restringida a su región**, con el motor existente y una entrada reutilizable. Fuera de su región puede existir un camino que salga y reentre: estas distancias internas no se presentan como distancias mundiales.

El cronómetro de construcción incluye D1 y topología D2. El de transferencias incluye crear índices inversos/caché, solicitudes, scheduler y comprobaciones del arnés. No es CPU exclusiva ni un benchmark de FPS. El límite preventivo es 6 s acumulados; no se alcanzó y se completaron todas las fuentes en las nueve muestras. No se reservan ni copian matrices; tampoco se reconstruyen todas las rutas ni se valida el mundo en ese tiempo. El circuito se construye antes del cronómetro. Son tiempos **parciales y optimistas respecto al proceso completo**, con sobrecoste pequeño de instrumentación no aislado.

| Región | Portales físicos S/M/L | Extremos de salida únicos S/M/L | Construcción S/M/L total (ms) | Transferencias total (ms) | Preparación parcial total (ms) |
| --- | --- | --- | --- | --- | --- |
| 16 m | 15058 / 12737 / 7970 | 5396 / 4605 / 2715 | 3657,119–3700,816 | 785,747–804,368 | 4442,866–4492,012 |
| 32 m | 6740 / 5935 / 3839 | 2477 / 2157 / 1310 | 2908,808–2959,024 | 1504,206–1522,051 | 4413,014–4481,075 |
| 64 m | 2296 / 2093 / 1486 | 839 / 757 / 506 | 2525,551–2585,125 | 2336,143–2381,785 | 4861,694–4966,910 |

Trabajo del kernel de transferencias: **76169848 / 132871781 / 172258815 unidades** para 16/32/64; idéntico entre repeticiones. Son unidades históricas de `RegionalTransferCache` (inicialización, extracción, examen/relajación fusionada y publicación), no equiparables sin matices a las categorías separadas de `DemandNavService`. No se ha modificado su contabilidad ni la del scheduler. Cada llamada al scheduler conserva ≤3000; se ejecutan offline consecutivamente, sin fingir que todo ese trabajo cabe en un tick de juego.

## Memoria: recuentos antes de reservar

Nodos S/M/L: **43185 / 37376 / 21030**, iguales en los tres tamaños. Distancia entera de 4 B, incluyendo entradas inalcanzables; fórmulas sumadas por clase:

- Mundial nodo×nodo: `4 × Σ N²` = **14816682004 B = 13,80 GiB**. Descartada sin reservar: solo distancias superan ampliamente los **160 MiB** residentes.
- Regional entrada×salida: `4 × Σregión entradasÚnicas × salidasÚnicas`. Conserva cada extremo físico, sin portal representativo. Sirve de grafo de transferencias; no incluye acceso desde un consumidor interior ni conexión de una meta interior.
- Regional nodo×salida: `4 × Σregión nodosPropietarios × salidasÚnicas`. Facilita consultar distancias internas desde un nodo arbitrario. Una tabla adicional de siguiente arista de 4 B duplicaría este payload.

| Región | Entrada×salida, solo distancias | Nodo×salida, solo distancias | Nodo×salida con siguiente arista |
| --- | --- | --- | --- |
| 16 m | 3,946 MiB | 30,334 MiB | 60,669 MiB |
| 32 m | 3,324 MiB | 53,779 MiB | 107,559 MiB |
| 64 m | 1,490 MiB | 71,749 MiB | 143,499 MiB |

Son tamaños calculados exactos para esas representaciones, **no memoria asignada ni residencia total**. Faltan grafos, geometría, índices, cabeceras, heaps, campos y transitorios. No se rechazan todas las tablas regionales por memoria: algunas caben como payload, pero ninguna supera la puerta temporal medida. En particular, 143,499 MiB deja muy poco margen para el resto. No se reserva una matriz grande para confirmar lo que ya muestra su tamaño.

Heap administrado observado al final de cada ensayo: **153321472–165416960 B**, incluye Editor, grafos y arnés, sin las tablas propuestas. Es una muestra final, no pico ni residencia exclusiva. No se certifican presupuesto transitorio de 96 MiB, Unity total, asignaciones cero ni GC.

## Coste ante otra meta: modelo, no latencias inventadas

Una tabla interna estática sobrevive al cambio de objetivo, pero **no contiene el coste mundial hacia la nueva meta**. Hace falta conectar la meta interior, resolver los costes globales sobre portales y contestar/reconstruir desde cada origen. Cambiar altura puede cambiar nodo y componente: no vale reutilizar la misma meta por igualdad XZ.

Como diagnóstico estructural de una propagación densa, recorrer una vez todas las parejas entrada×salida más portales físicos exige:

| Región | Entradas del barrido S/M/L agregado | Ticks solo para un examen por entrada a 3000 | Segundos a 60 Hz |
| --- | --- | --- | --- |
| 16 m | 1070197 | 357 | 5,950 |
| 32 m | 887801 | 296 | 4,933 |
| 64 m | 396551 | 133 | 2,217 |

Esto **no es una cota inferior de toda consulta exacta**: una búsqueda selectiva puede evitar entradas, y las inalcanzables podrían almacenarse de otra forma. Tampoco es rendimiento medido de un solver nuevo. Sí descarta justificar un barrido denso completo como operación barata por cambio: ya sus exámenes exceden las **54000/90000 unidades** disponibles en 0,3/0,5 s, antes de extracciones, relajaciones, publicación, acceso local y consumidores. Los 3000 son globales, no por clase. Las tablas por sí solas no resuelven el objetivo móvil.

## Verificación y límites

Puerta nueva **9/9**, XML/log en `unity/TestResults/P0/Regional-20261020T170821`; el éxito de las pruebas significa diagnóstico completado, **no aceptación de rendimiento**. Recuentos deterministas 27/27 y nueve agregados comprobados por `scripts/p0-d3-preprocess-report.py`; evidencia local `unity/TestResults/P0/D3-Preprocess-20261020/gate.json`. `gate-first.json` conserva la extracción inicial, mismos datos sin las columnas derivadas del barrido.

Regresión nueva del kernel reutilizado: **71/71**, 275,949 s, `Regional-20261020T171040/edit.xml`, incluyendo transferencias directas/inversas del corpus A/B/ocho C × 16/32/64 y controles físicos/caché. Son las pruebas del motor existente, no equivalencia de un servicio precalculado nuevo. Referencias U0/U2/U3/U4 nuevas coincidentes; instantánea `qa-results/reference-168903c041f0481c9f8da344236af827`. Huellas entrada/salida **9/9** y corpus **11/11** iguales; siete ajustes excluidos de commits. No se repiten Edit completo, Play ni build porque no cambia código de producción. Sus resultados anteriores siguen siendo heredados.

No existe prototipo nuevo que contrastar con el oráculo monolítico. La equivalencia anterior no se atribuye a esta evaluación; el cierre de verificaciones del kernel reutilizado figura en PROGRESO_P0. Al fallar la puerta con HORDA750, **no se ejecuta el corpus A/B/ocho C para un solver nuevo ni la carga 300/500/750** concentrada/dispersa, fija/móvil, jefe o cambios de altura. Latencias, cobertura vigente, pendientes y CPU por tick de ese servicio: **no disponibles**. Es la parada temprana expresamente solicitada, no ensayos omitidos tras aceptar un prototipo.

Comandos nuevos, desde CMD:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalPreprocessFeasibilityTests -Category PreprocessGate
python scripts\p0-d3-preprocess-report.py unity\TestResults\P0\Regional-20261020T170821\edit.xml unity\TestResults\P0\D3-Preprocess-20261020\gate.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalTransferTests
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

## Alternativas para revisión, sin implementar

1. **Recomendación:** un experimento acotado de consultas exactas con cotas admisibles precalculadas desde pocas referencias, separadas por clase y dirección, para intentar evitar expansiones mundiales. Primero medir preparación/memoria y eficacia con metas nuevas y orígenes dispersos; después cola y frescura. Mantendría coste óptimo, pero no hay evidencia todavía de que cumpla reacción.
2. Si se quiere insistir en tablas regionales, revisar un kernel de preparación especializado y una representación dispersa de transferencias. Necesita nueva evaluación: la tabla compacta por sí sola no evita la propagación extensa tras cambiar meta. No continuar optimizando a ciegas ni dar por probada una mejora en build.
3. Preprocesado persistente fuera de la carga podría amortizar tiempo para semillas repetidas, pero introduce archivos/versiones/invalidez y no resuelve la primera semilla ni la reacción. No se adopta ni se contabiliza como carga gratuita.

**D3 sigue bloqueado. Detenerse para revisión del autor antes de cualquier integración o cambio arquitectónico.** Presupuesto 3000, reacción 0,3/0,5 s, navegación activa, generador, balance y base web intactos. Publicación, pruebas finales, nueve huellas y siete ajustes locales en [PROGRESO_P0](PROGRESO_P0.md).
