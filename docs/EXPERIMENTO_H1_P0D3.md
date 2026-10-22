# H0–H1 híbrido P0-D3

**Continuación posterior autorizada, 22/10/2026:** [H1b](EXPERIMENTO_H1B_P0D3.md) conserva este experimento y corrige sus rodeos en el corpus, pero falla preparación p95; detenido antes de H2. Los resultados H1 inferiores no se sustituyen ni reinterpretan como aceptados.

## H0: contrato fijado antes de medir — 21/10/2026

Base `563f05e`, HEAD/origin coincidentes tras fetch; nueve protecciones y once referencias comprobadas. Autoriza exclusivamente H0–H1. Arquitectura A, alcance/física exactos y coste aproximado con rodeo p95 ≤1,5× y máximo ≤2×. Bandas 24/32 y 48/60 m, jefe crítico, reacción experimental 0,3/0,5; 0,6/1; 1,5/2 s aprobadas como contrato experimental; comparar también 0,3/0,5 originales. Cuotas y criterios de persecución/colas solo aceptados para estudiar, no validados. H1 no implementa bandas, scheduler, movimiento ni refinamiento cercano (H2+).

Prototipo exclusivamente en ensamblado de tests sobre NavPortalWorld. Grafo de SCC locales existente, todos sus arcos incluidos los internos de región. Árbol inverso por número de saltos, desempate por ID de componente; cada conexión conserva su lista íntegra de aristas originales. Primer testigo: mediana de IDs de arista de esa conexión. Si falla rodeo, segunda variante conserva ese testigo y añade el de mayor distancia XYZ desde él, desempate por ID; selección fijada antes de resultados. No cambiar después de ver medidas.

Reconstrucción offline: Dijkstra inverso limitado a aristas internas de SCC y testigos que siguen el árbol; obtiene simultáneamente rutas físicas desde todos los nodos. Es una herramienta H1 para comprobar el corredor, **no servicio local H2 ni trabajo gratuito durante juego**. Todas las búsquedas y reconstrucciones son offline, sin tick simulado, FPS ni afirmación de cumplir reacción; scheduler/3000 de producción intactos. Una variante de dos testigos solo se ejecuta si la primera incumple rodeo en el caso, posibilidad expresamente autorizada. Si tampoco cumple, detener candidato al terminar diagnóstico/documentación, no optimizar más.

Corpus fijado: A/B y ocho C × 16/32/64, S/M/L. Metas distribuidas por identidad física, una por SCC mundial y metas vecinas de cruces para detectar rodeos de frontera. Para cada meta, alcance de **todos los nodos** contra oráculo monolítico inverso por cola; rutas desde 24 orígenes distribuidos y origen de cruce adyacente. Comparar cada arista recorrida con la identidad física monolítica (clase/capa/coste/tipo/trayectoria). Fixtures adicionales de barrera con salida/reentrada, placas desconectadas, caída unilateral con voladizo, escalada y rampa/paso corporal. Percentil por rango más próximo, sin excluir rutas cortas; coste cero exige ruta vacía. Reportar variante/tamaño/clase por separado y peor par.

Preparación separa circuito original, D1/D2 e índice/corredor H1; no incluir oráculo/aserciones en CPU del solver. Memoria: arrays físicos, topología/portales y arrays propios explícitos; muestras managed/Unity incluyen arnés y no certifican picos/residencia. Carga y memoria se contrastan con PLAN_P0D, sin afirmar cumplimiento global si faltan mediciones. Pruebas de diagnóstico pueden pasar al capturar correctamente una puerta de rendimiento negativa; el informe debe distinguir ambos resultados.

**Alcance comprobado H0→H1:** únicamente topología, rutas offline y evaluación; no EnemyNavigation, H2–H4, D4/D5, mundo mayor, web ni referencias. Detenerse antes de H2 incluso si H1 pasa.

## Resultados

Primera ejecución: `Regional-20261021T201247`, **33/36**, 223,137 s. Tres casos HORDA750 fallaron en el comparador: seleccionaba la primera arista monolítica con mismos extremos/coste/tipo aunque existían trayectorias diferentes. Corregido el emparejamiento para buscar también identidad de puntos, normal, plano y salida de borde (incluido centinela de ausencia). No se modificó el prototipo ni se relajó la igualdad física. XML/log original conservado; nueva ejecución pendiente de cierre. Primer intento en sandbox `195842` sin pruebas por licencia 198.

Los casos completados ya muestran rodeos superiores a 2× aun con dos testigos. **Candidato detenido para mejoras: solo terminar verificación, registrar medidas y publicar. No H2.** El éxito posterior del arnés no cambiará ese fallo de puerta.

## Cierre: puerta negativa por calidad de ruta

Suite final **36/36**, 164,855 s, `unity/TestResults/P0/Regional-20261021T225439/edit.xml` y `edit.log`. Comando real: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.HybridCorridorTests`. Incluye 30 combinaciones A/B/ocho C × 16/32/64, cada una S/M/L, y seis fixtures × S/M/L a 16 m: **108 evaluaciones por variante**, una y dos referencias físicas por conexión. La segunda se activó en todas porque la primera incumplía rodeo. El éxito de la suite certifica las aserciones de exactitud y la captura del diagnóstico, **no la puerta de rendimiento/calidad**.

Extractor ejecutado: `python scripts/p0-h1-report.py unity/TestResults/P0/Regional-20261021T225439/edit.xml unity/TestResults/P0/H1-Hybrid-20261021/report.json`. Conserva 108 preparaciones, 216 resultados de calidad, desglose de CPU/trabajo/payload, peor par e indicadores separados: `qualityGate=FAIL`, `overallGate=FAIL`.

### Exactitud comprobada

**69461240 comparaciones de alcance** desde todos los nodos hacia las metas seleccionadas y **55358 reconstrucciones físicas**, sumando ambas variantes (no son pares únicos independientes). Cero discrepancias de alcance, coste reconstruido o continuidad en esa pasada. Todas las aristas del universo D2 usado se contrastan con el monolítico por extremos físicos, clase, coste, tipo y trayectoria completa, normal/plano/salida incluidos. No se exige igualdad entre coste aproximado y óptimo; sí que nunca sea menor que el óptimo ni use aristas inventadas. Coste cero produce ruta vacía.

La prueba de barrera exige salida/reentrada regional real; las fixtures verifican que hay caída sin retorno, escalada interregional y tejados desconectados, y que el paso estrecho admite S/M y excluye L. El alcance se contrasta en todas las metas muestreadas, incluyendo representantes de cada SCC mundial. No es un barrido de todas las parejas ni una simulación de cuerpos: no acredita llegada, ausencia de penetraciones durante movimiento, atasco, caudal, prioridad del jefe o frescura móvil. Esos ensayos pertenecen a fases no autorizadas aquí.

### Rodeos: no se supera la puerta ni con dos cruces

| Variante | Evaluaciones fallidas / total | Peor p95 por evaluación | Peor cociente individual |
| --- | --- | --- | --- |
| Un testigo | 108/108 | 4,06694× | 394,26× |
| Dos testigos | 108/108 | 2,52091× | 173,82× |

Los límites aprobados son **p95 ≤1,5× y máximo ≤2×**. No sustituir el máximo por un promedio ni excluir pares adyacentes. Sobre el corpus A/B/ocho C, con dos testigos:

| Región | Rango de p95 entre clases/mapas | Peor máximo |
| --- | --- | --- |
| 16 m | 1,32020–1,81294× | 30,42× |
| 32 m | 1,25659–2,31617× | 62,92× |
| 64 m | 1,16496–2,52091× | 173,82× |

Peor caso final: **Z9K2, 64 m, S, origen 62 → meta 10514**, coste **8691** frente a óptimo **50**, ratio 173,82. IDs registrados en `worstFrom/worstGoal` del JSON, vinculados a esa instancia física y orden; no usados para seleccionar testigos después del resultado. HORDA750 64 m con dos testigos: p95 S/M/L **1,59109/1,73389/1,16496×**, máximos **150,32/148,32/40,42×**. Aunque L pasa ese p95, falla su máximo.

Causa estructural observada: el corredor obliga a pasar por uno de dos puntos fijos de una conexión amplia incluso si origen/meta son vecinos junto a otro cruce físico válido. El árbol por saltos tampoco minimiza coste físico. Dos testigos reducen algunos rodeos, pero no los acotan a 2×. No se cambia el árbol, la selección ni se añade un acceso directo para ocultar estos casos. Este resultado rechaza **esta política concreta**; no demuestra imposibilidad de toda arquitectura híbrida. Cualquier rediseño posterior requiere revisión, no iniciar H2 para compensarlo.

### Preparación, trabajo y memoria

Tiempo registrado = circuito original (una vez) + D1/D2 S/M/L + construcción del índice H1 S/M/L. El oráculo monolítico extra de A/B/fixtures, aserciones, recorridos de validación y consultas de muestra se excluyen del cronómetro; en C la construcción original ya incluye generación/validación/grafos. **No es preparación de un servicio H2 ni certificación del tiempo total de todas las validaciones H1.** Una muestra por combinación, sin p95 temporal certificado.

| HORDA750 | Preparación registrada | Arrays del instrumento | Arrays instrumento + físicos/topología |
| --- | --- | --- | --- |
| 16 m | 4702,778 ms | 8210252 B | 53859815 B |
| 32 m | 3934,812 ms | 8198504 B | 46793563 B |
| 64 m | 3555,030 ms | 8195408 B | 44273623 B |

Máxima preparación registrada del corpus: **5902,057 ms**, MAMPORRO 16 m. No se observa >6 s en esas muestras, pero no se certifica p95 ≤4 s; varios casos exceden 4 s. El incremento del índice H1 por clase/caso va de **0,2482 a 18,888 ms**. Las tablas de muestras se conservan completas; no elegir el mejor tamaño solo por carga.

CPU offline de construcción de corredores para HORDA750 64 m, dos testigos: S **71,790 ms / 10 metas**, M **62,719 ms / 10 metas**, L **27,746 ms / 15 metas**; trabajo agregado por clase **7471990 / 6401750 / 3613845**. Son totales para muchas metas del diagnóstico, no una solicitud individual, ticks consumidos ni FPS. Se cuenta inicialización, barrido de arcos macro, extracciones, aristas examinadas y relajaciones de la reconstrucción; no es contabilidad completa de un servicio (ni de preparación inicial, salida/copia de rutas o mantenimiento). No comparar directamente con unidades históricas de otra instrumentación.

Payload del instrumento HORDA750 ~7,82 MiB; a 64 m, todos los arrays contados ~42,22 MiB. **No son residencia total:** excluyen cabeceras, listas de construcción y miembros, geometría, listas retenidas por D1 y oráculos/arnés. Muestras managed **47448064–149950464 B**, Unity asignado **354550211–356840066 B**, tomadas con arnés/oráculos vivos; no son picos certificados ni memoria exclusiva de navegación. Transitorios, asignaciones y GC verificable **N/D**. No certificar los techos residentes/transitorios con estos datos parciales. La puerta ya falla por rodeos y no se amplía el trabajo para simular una aprobación de memoria.

### Protección, límites y siguiente paso

Huellas antes/después **9/9** y referencias **11/11** iguales, JSON en `H1-Hybrid-20261021`. Sin cambios a Core, EnemyNavigation, U3, src ni referencias. Los cinco ajustes Unity modificados y dos no seguidos permanecen fuera del índice. Sin suites Edit completas, Play, build, verificador histórico ni FPS nuevos; no se necesitan para presentar como negativo un instrumento exclusivo de tests. Pruebas anteriores se conservan como heredadas.

H0 cumplido; H1 implementado y evaluado con **puerta negativa**, no aceptado. No H2–H4, D4/D5, scheduler nuevo, integración ni mundo mayor. 3000 globales/tick y contratos físicos sin cambios de producción. Bandas/reacción quedan aprobadas como contrato experimental, sin implementación ni validación dinámica. **Detenerse para revisión del autor antes de cualquier rediseño adicional o H2.**
