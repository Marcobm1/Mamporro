# H1b — cruces adaptativos y corredores ponderados

**Cierre 22/10/2026: exactitud y rodeos favorables, puerta completa negativa por preparación p95 >4 s.** A 7/7, B 30/30 y preparación 3/3 (30 arranques); el éxito del arnés no acepta el candidato. **Detenido para revisión antes de H2**, sin cambios de producción. Contrato `1545ec6`, prototipo y A `58307fc`, publicados con push normal.

## Contrato previo a medir — 22/10/2026

Autorización exclusiva H1b desde `9154ec8`. H1 permanece conservado y rechazado. No investigación avanzada, producción, EnemyNavigation, generador, H2–H4, D4/D5 ni ampliación. Techo de juego intacto: 3000 unidades globales/tick. Nueve huellas y once referencias verificadas; siete ajustes locales fuera de commits.

Instrumento offline en tests sobre D1/D2. Tres variantes acumulativas fijadas antes de resultados:

1. **Adaptativa:** conserva árbol por saltos H1, pero permite todos los portales físicos de cada conexión elegida. Dijkstra inverso restringido calcula el coste real desde cada origen; el cruce elegido depende del origen y la meta, sin testigos fijos.
2. **Local:** añade las aristas originales cuyos dos extremos están a ≤32 m XYZ de la meta. Es un dominio físico local, no una conexión recta inventada; las rutas pueden seguir usando el corredor fuera del dominio. No presume que un radio certifique inaccesibilidad.
3. **Ponderada + local:** potencial macro inverso con peso por conexión = mínimo de distancia centroide-origen de portal + coste original + distancia destino de portal-centroide, escalado con NavGraph.Scale y mínimo 1. Centros medios por SCC y clase. Permite todos los arcos que descienden estrictamente el potencial, además del dominio local. La estimación no es una cota admisible ni una distancia física garantizada; solo selecciona corredores, cuyo coste se resuelve después con aristas originales.

Alcance: existe al menos el árbol de descenso hacia la meta para toda SCC alcanzable; las SCC conservan sus recorridos internos. Verificar contra monolítico, incluyendo arcos internos de región, alturas, S/M/L y trayectorias completas. El campo físico final tiene padres de coste descendente incluso si el dominio local permite subir potencial macro. Sin matrices de transferencias ni consulta global por consumidor: una búsqueda inversa por clase/meta comparte resultado entre todos los orígenes. Es instrumentación mundial offline O(N+E), no servicio H2, caché, cuotas ni movimiento. No ocultar su coste ni afirmar que está amortizado con metas móviles.

**Puerta A:** Z9K2 64 m S/M/L (reproducción explícita S 62→10514: H1 dos testigos 8691, óptimo 50), más seis fixtures H1 a 16 m (barrera/reentrada, tejados/capas, caída unilateral, escalada, rampa diagonal, paso S/M excluido de L). Mismas metas/orígenes H1, añadiendo el peor par; control contemporáneo H1 dos testigos. Contrastar alcance desde todos los nodos para cada meta y reconstruir las rutas muestreadas, incluidas las adyacentes, cero y desconectadas. No eliminar resultados negativos de las variantes.

A favorable exige exactitud, variante completa con p95 ≤1,5× y máximo ≤2× en cada evaluación pequeña, peor par corregido, sin exceder preparación máxima 6 s ni arrays contados de navegación 160 MiB o instrumento 20 MiB. Comparar trabajo/CPU con H1 contemporáneo; un incremento >2× del trabajo por caso impide abrir B. Es un filtro conservador de viabilidad, no certificación de los presupuestos completos. Mantener objetivos p95 preparación ≤4 s, residencia ≤160 MiB, transitorios ≤96 MiB, managed QA ≤256 MiB, Unity ≤512 MiB; muestras parciales no certifican residencia ni picos. Ante fallo de A, completar diagnóstico de esa matriz pequeña y detenerse, sin modificar pesos/radio después de medir para forzar aprobación.

**Puerta B, solo si A favorable:** A/B/ocho C ×16/32/64×S/M/L; mismo oráculo, percentiles y muestras H1, conservando fixtures y peor par. Exigir 1,5×/2×, preparación/memoria compatibles y estudiar compartición real. Certificación de p95 temporal exige repeticiones suficientes; no convertir una muestra en garantía. Parada ante fallo y siempre antes de H2.

Registrar preparación original, D1/D2 e índice por separado; arrays propios, físicos/topología y memoria observada del arnés. Trabajo: inicialización, arcos, extracciones, aristas físicas, relajaciones y operaciones/comparaciones de heap separadas; reconstrucción/copias separadas. Oráculos/aserciones fuera del tiempo solver. Ningún cociente trabajo/3000 se presentará como latencia o FPS.

## Resultados

Evidencias en `unity/TestResults/P0/H1b-20261022`.

### Puerta A favorable

Comando real: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.AdaptiveCorridorTests -Category AdaptiveGateA`. **7/7**, 80,131 s, `Regional-20261022T182040/edit.xml`. Intento `180156` sin pruebas por licencia 198; repetición fuera del sandbox. Extractor: `python scripts/p0-h1b-report.py unity/TestResults/P0/Regional-20261022T182040/edit.xml unity/TestResults/P0/H1b-20261022/gate-a.json A`.

21 evaluaciones por variante (Z9K2 S/M/L y seis fixtures S/M/L). Control H1: 21/21 fallan, peor p95 2,42×, máximo 173,82×. Adaptativa: 1/21 falla, peor p95 1,46308×, máximo 2,60597×. Con local: 0/21 fallan, peor p95 1,28717×, máximo 1,93920×. Completa ponderada/local: **0/21 fallan, peor p95 1×, máximo 1,49005×**. Peor par anterior reproducido: H1 8691 frente a óptimo 50; las tres variantes obtienen 50. Las variantes parciales son controles de contribución, no candidatos adoptados por separado.

Cero discrepancias de alcance/identidad de arista/continuidad. Cada padre alcanzable también certifica coste descendente por arista original, no solo las rutas reconstruidas. Máximo cociente de trabajo completo/H1 por evaluación: **1,003584×**, bajo el filtro 2×. Contabilidad comparable de visitas/extracciones/relajaciones; operaciones de heap de H1 no instrumentadas, H1b sí las informa aparte: no interpretar ese cociente como coste total de instrucciones o aceleración CPU.

Preparación registrada Z9K2: **4686,645 ms** (original + D1/D2 + índice S/M/L), arrays del instrumento **8272793 B**, arrays físicos/topología/instrumento **44369052 B**, scratch explícito máximo por construcción de índice **173216 B**. Managed observado máximo 130605056 B, Unity 356156867 B; incluyen arnés/control/oráculos. Sin exceso de los filtros duros observados de A, pero **no certifica p95 ≤4 s, residencia, picos ni GC**. Fixtures excluyen el constructor monolítico usado solo como oráculo; no son carga jugable comparable a C.

Se abre B con el algoritmo y muestras prefijados; no se alteran pesos/radio ni umbrales. Los costes mundiales offline siguen siendo elevados: Z9K2 S, variante completa, hasta **749390** visitas/extracciones/relajaciones por meta, además de heap y salida de rutas. Compartir todos los orígenes evita repetir búsquedas por enemigo, pero no demuestra reacción a 3000 ni servicio móvil viable.

### Puerta B: calidad favorable; preparación negativa según confirmación inferior

Comando real: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.AdaptiveCorridorTests -Category AdaptiveGateB`. **30/30**, 538,084 s, `Regional-20261022T182314/edit.xml`. A/B y ocho semillas C ×16/32/64×S/M/L: 90 evaluaciones por variante, 360 en total. Extractor `python scripts/p0-h1b-report.py unity/TestResults/P0/Regional-20261022T182314/edit.xml unity/TestResults/P0/H1b-20261022/gate-b.json B`. Su salida `FAVORABLE_NOT_CERTIFIED` se refiere al filtro inicial de calidad/máximos observados, **no a aceptación de todos los presupuestos**; falta cerrar preparación p95 y memoria completa.

| Variante | Fallos de calidad / 90 | Peor p95 por evaluación | Máximo |
| --- | --- | --- | --- |
| H1, dos testigos (control actual) | 90 | 2,520913× | 173,82× |
| Adaptativa sobre árbol por saltos | 33 | 1,827866× | 5,572135× |
| Adaptativa + conexiones locales | 7 | 1,724573× | 2,418881× |
| Adaptativa + local + ponderada | **0** | **1,113474×** | **1,894511×** |

La variante completa cumple 1,5×/2× en las 90 evaluaciones y las 21 pequeñas. No excluir pares cortos, negativos o desconectados. Rango p95 por tamaño: 16 m 1–1,021959×; 32 m 1–1,065731×; 64 m 1–1,113474×. Máximos respectivos **1,217413/1,403672/1,894511×**. Peor par final de B: RAMPA42, 64 m, L, **12654→5393**, coste **10147** frente a **5356**. No es el peor par H1 original: ese se corrige a 50/50.

**Exactitud B:** por variante, **31123458 contrastes de alcance**, **23299 rutas reconstruidas**, **26073752 padres comprobados**. Cero diferencias de alcance o continuidad; el universo completo de aristas se empareja con el monolítico por identidad de nodo/capa, coste, tipo y trayectoria completa. Todos los padres finitos reducen coste con una arista real positiva, certificando ausencia de ciclos y continuidad hacia la meta para todos los nodos alcanzables de esas consultas. No acredita todas las parejas posibles, movimiento físico, penetraciones de cuerpos, llegada, colas o frescura. Sumando A+B, la variante completa tiene **35960639** comparaciones de alcance, **28449** rutas y **30331015** padres; hay solapamiento de Z9K2 entre A y B. No presentar las cuatro variantes como muestras independientes.

### Trabajo y compartición

El grafo macro y sus pesos son comunes a todos los orígenes de la misma clase y SCC destino. La variante ponderada permite varias conexiones descendentes; el campo físico decide desde cada nodo qué portal usar según coste restante hacia la meta real. El índice ocupa O(N+E+C+A), sin tabla de parejas, matriz nodo×salida ni búsqueda mundial por enemigo. Una vez calculado, cualquier origen consulta su siguiente arista en O(1), y reconstruir su ruta cuesta su longitud (salida medida aparte). 750 orígenes con la misma meta pueden consumir el mismo campo; esto es una propiedad de la estructura, **no una carga 750 móvil ejecutada ni aceleración medida de 750×**.

La selección ponderada macro es barata: máximo **874** unidades macro por consulta en B; HORDA750 64 m S/M/L: 28/35/hasta 64. El trabajo dominante es la reconstrucción inversa mundial compartida. HORDA750 64 m, completa:

| Clase | Metas | Trabajo total de búsqueda | Máximo por meta | CPU total offline | CPU por meta |
| --- | --- | --- | --- | --- | --- |
| S | 10 | 7486599 | 748931 | 96,474 ms | 8,614–10,233 ms |
| M | 10 | 6414402 | 641739 | 78,203 ms | 7,254–8,359 ms |
| L | 15 | 3620378 | 351712 | 38,710 ms | 0,423–4,157 ms |

Hay metas inaccesibles para gran parte de L, explicando el mínimo bajo; no borrarlas ni interpretarlo como servicio instantáneo. Máximo CPU individual B: **12,2195 ms**, P0QA 32 m S. El máximo cociente de trabajo completa/H1 por evaluación es **1,010047×**, no una reducción del trabajo global. B completa suma 451153073 visitas/extracciones/relajaciones, **616429136** operaciones/comparaciones/movimientos de heap y **2540384** pasos de consulta/reconstrucción. Las categorías son instrumentación diagnóstica, no contabilidad implementada del scheduler. El contador H1 de heap es N/D (el cero de su fila agregada es ausencia de instrumentación, no coste cero); no sumar o comparar como si ambas instrumentaciones fueran completas.

Al moverse la meta, el potencial macro podría reutilizarse mientras conserve SCC, pero el campo físico actual depende del nodo exacto y se recalcula entero. No se implementan caché, demandas móviles, cuotas ni refinamiento local de servicio. El coste mundial medido **no demuestra viabilidad con 3000/tick**, aunque los corredores y siguientes pasos sean compartibles. No convertir unidades en latencia ni CPU de este arnés en FPS. H2 continúa excluido.

### Preparación y memoria observadas en B

Máximo de preparación registrada: MAMPORRO, **5901,376/5110,856/4722,846 ms** a 16/32/64. Una muestra por caso/tamaño: sin exceder 6 s en esa pasada, pero muchas por encima del objetivo de 4 s. Para confirmar, se ejecutan **diez arranques independientes de MAMPORRO por tamaño** con categoría `AdaptivePreparation`, sin modificar el algoritmo. Caso seleccionado por ser el máximo observado en los tres tamaños; sirve para detectar incumplimiento, no para certificar las demás semillas. Las pruebas A/B del solver se ejecutaron con las fuentes de `58307fc`; el ensayo temporal añadido no cambia esos métodos ni `AdaptiveCorridorProbe`.

En MAMPORRO B, el circuito original consume ~2,12 s e incluye sus grafos aprobados; D1/D2 añade **3,763/2,960/2,583 s**, índice H1b **15,077/25,652/24,243 ms** sumando S/M/L. Es el contrato de preparación original + regional + índice heredado de H1; no atribuir toda esa carga al índice nuevo, ni quitar silenciosamente mapas/validaciones de producción del cronómetro. No son FPS ni carga visible medida.

HORDA750 16/32/64: preparación **4589,820/3906,478/3523,513 ms**, arrays instrumento **8312755/8300391/8297103 B**, arrays físicos/topología/instrumento **53962318/46895450/44375318 B**. Máximo arrays instrumento B: **8495772 B (~8,10 MiB)**; máximo combinado **55069252 B (~52,52 MiB)**. Índice por clase/caso **0,3377–21,8672 ms**. Arrays scratch explícitos del índice no incluyen transitorios D1/D2, cabeceras ni colecciones; preparación `SetupWork` cuenta recorridos, no todas las comparaciones internas de BinarySearch ni cada operación aritmética.

Managed observado máximo B **137469952 B**, Unity asignado **355948967 B**, incluyen arnés/control/oráculos. Working set de Editor observado en terminal a las 18:31:59: **1139904512 B**, worker **618446848 B**; transcripción en `editor-process-transcript.json`. El intento automático posterior encontró procesos ya cerrados (`editor-process-observed.json` vacío), **no memoria cero**. Estos valores no son residencia de navegación ni picos certificados. Sin afirmar cumplimiento completo de 160/96 MiB, cero GC o asignaciones por tick. No hay servicio activo ni build nueva.

### Confirmación de preparación y parada

Comando real: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.AdaptiveCorridorTests -Category AdaptivePreparation`. **3/3**, 153,484 s, `Regional-20261022T183342/edit.xml`; cada test ejecuta diez arranques completos de MAMPORRO, sin reaprovechar el mundo anterior. Extractor: `python scripts/p0-h1b-report.py unity/TestResults/P0/Regional-20261022T183342/edit.xml unity/TestResults/P0/H1b-20261022/preparation.json L`.

| Región | Arranques | Mínimo | p95 empírico / máximo | Objetivo p95 ≤4 s / máximo ≤6 s |
| --- | --- | --- | --- | --- |
| 16 m | 10 | 5649,857 ms | **5771,898 ms** | **Falla p95** / cumple máximo observado |
| 32 m | 10 | 4926,483 ms | **5042,562 ms** | **Falla p95** / cumple máximo observado |
| 64 m | 10 | 4608,974 ms | **4694,685 ms** | **Falla p95** / cumple máximo observado |

Percentil por rango más próximo: con n=10, p95 coincide con máximo. **Las 30 muestras exceden 4 s**; no hace falta certificar todas las semillas para constatar este contraejemplo. No se supera 6 s. Medias del desglose original/D1-D2/índice: 16 m **2067,032/3637,169/10,700 ms**; 32 m **2061,050/2898,782/13,622 ms**; 64 m **2083,643/2555,593/13,331 ms**. El cuello está en la preparación heredada medida junto con el índice, no en el coste añadido de seleccionar pesos. Managed observado máximo en esta repetición **164978688 B**, Unity **355433501 B**; arnés, no residencia certificada.

Resumen reproducible: `python scripts/p0-h1b-report.py --summary unity/TestResults/P0/H1b-20261022/gate-a.json unity/TestResults/P0/H1b-20261022/gate-b.json unity/TestResults/P0/H1b-20261022/preparation.json unity/TestResults/P0/H1b-20261022/summary.json`: **qualityGate PASS, preparationGate FAIL, memoryGate NOT_CERTIFIED, overallGate FAIL, STOP_REVIEW_BEFORE_H2**. Los JSON A/B conservan su clasificación parcial favorable y no sustituyen este cierre.

**Resultado:** las tres mejoras juntas corrigen el defecto de calidad H1 en el corpus muestreado, pero **H1b no queda aceptado** por incumplimiento de preparación. Además, la búsqueda física mundial sigue siendo costosa y no acredita servicio bajo 3000. Se detiene el experimento sin optimizar el constructor, quitar validaciones, ampliar dominios, bajar frecuencias, elevar presupuesto ni iniciar H2. No es un rechazo matemático de toda arquitectura híbrida ni una regresión atribuida exclusivamente al índice H1b.

Para revisión futura, alternativas **no implementadas ni autorizadas por este cierre**: aislar la duplicación de grafos aprobados y regionales en la carga del arnés antes de proponer una preparación exclusiva; perfilar D1/D2 y justificar una optimización acotada sin tocar geometría; estudiar posteriormente cómo reutilizar el potencial macro y limitar el detalle a consumidores sin perder las garantías medidas. Ninguna permite descontar trabajo ya medido o abrir H2 automáticamente. No ampliar la medición de residencia/picos para aparentar aceptación de una puerta ya fallida.

### Integridad, publicación y relevo

Huellas antes/después **9/9**, referencias **11/11**; manifiestos y hashes de fuentes/XML/JSON en `H1b-20261022`. Siete ajustes Unity conservados fuera del índice. Diff vacío frente a `9154ec8` en Core, U3, src, referencias y fuentes H1 originales. Sin cambios de producción, EnemyNavigation, scheduler, generador, economía ni guardados. No Edit completo, Play, build, FPS, verificador histórico ni investigación avanzada nuevos; pruebas de esta sesión son exclusivamente las **40** de H1b descritas arriba. No afirmar validación dinámica ni cero GC.

Comprobaciones finales: extractores A/B/L reproducen exactamente los tres JSON con la versión final del script; **179 enlaces Markdown locales válidos** y `git diff --check` sin errores en archivos propios. Las diferencias de formato preexistentes de ajustes Unity no se limpian ni se incluyen.

Publicación en commits pequeños en español y push normal a `claude/zen-pasteur-674ik0`, sin PR ni force-push. Checkpoint y SHA del cierre en PROGRESO_P0. **Siguiente paso exacto: revisión del autor del resultado mixto (calidad favorable, preparación negativa), antes de cualquier nuevo experimento o H2.**
