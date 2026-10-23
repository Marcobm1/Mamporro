# H1d — reutilización exacta de grafos validados

**Cierre 23/10/2026: exactitud, rutas H1b y preparación favorables; memoria no aceptada.** A 48/48, rutas 48/48, cargas 9/9 con 180 arranques; p95 baja de 4,382–5,769 s a 1,967–2,223 s, ahorro real 54–64 %. Exceso managed observado sobre retenido final hasta 112,105 MiB, sin atribución certificada a navegación; contador de asignaciones N/D. **Detenerse para revisión, sin adopción ni H2.**

## Contrato previo — 23/10/2026

Autorización exclusiva del autor desde `4795500`, HEAD/origin comprobados tras fetch. Solo tests, scripts y documentación, sin investigación avanzada. Conservar H1b, producción, generador, geometría, validaciones, 3000 globales/tick, siete ajustes Unity, nueve protecciones y once referencias. No servicios nuevos ni H2–H4/D4/D5. Parada para revisión al terminar, incluso si favorable.

**A, antes de B:** vista regional que referencia los NavGraph originales; propiedad única y orden regional de nodos, aristas físicas con referencias a trayectorias originales, portales completos, índices y SCC. No construir NavGraph en el constructor de la vista ni depender del control D1/D2 para producir datos. Comparar contra D1/D2 contemporáneo A/B/ocho C ×16/32/64×S/M/L y seis fixtures (barrera/reentrada, capas, caída unilateral, escalada, rampa diagonal y paso por tamaño) en los tres tamaños. Igualdad biyectiva de nodos, capas/alturas, todas las aristas con multiplicidad, coste/tipo/trayectoria, propiedad, portales y particiones/arcos SCC. La igualdad del grafo dirigido certifica el mismo alcance; añadir consultas contra oráculo monolítico y controles físicos. Un fallo real de equivalencia detiene A y no abre B; no cambiar el oráculo.

**Compatibilidad H1b:** conservar fuente original. Adaptación mecánica solo del tipo de mundo en una copia de tests del solver, sin cambiar algoritmo, pesos, radio ni desempates. Verificar dicha transformación byte a byte normalizando saltos de línea. Comparar distancias/potenciales y padres físicos con H1b contemporáneo, y calidad frente a oráculo con las mismas metas/orígenes del corpus H1b, incluidos Z9K2 62→10514 y RAMPA42 12654→5393. No usar la unión local fallida de H1c.

**B, solo tras A favorable:** diez arranques completos independientes de cada alternativa por semilla crítica MAMPORRO/Z9K2/RAMPA42 ×16/32/64. Son los tres casos H1c que requieren dos tandas originales por rechazo/reparación y fallan 4 s en todos los tamaños. Alternar el orden control/prototipo por repetición. Cada arranque crea su propio mapa/circuito, validación y tres grafos; el prototipo añade propiedad/SCC/portales/índice H1b. El control añade D1/D2/índice H1b. Conservar los tres mundos/índices simultáneamente en ambos; no atribuir al prototipo los datos ya creados por el control. Comparación H1c heredada identificada aparte.

Cronómetro exterior real con generación, comprobación previa, grafos acumulados de todos los intentos, validación, proyección y solver; residual explícito. Separar mediciones instrumentales y verificaciones del coste de carga, informar ambos y no sumar CPU de thread a tiempo de pared. Recoger bytes asignados del hilo, GC, payload de arrays compartidos una sola vez, memoria managed/Unity observada y muestreo de pico managed; distinguir transitorios observados de cota/medición incompleta. No convertir muestreo en garantía del pico ni payload en residencia certificada. Objetivos 4 s p95/6 s máximo; navegación 160 MiB, transitorios 96 MiB, managed QA 256 MiB, Unity 512 MiB, instrumento 20 MiB. Si no puede certificarse una medida, marcarla sin certificar; no adoptar automáticamente.

Percentil de rango más próximo, por semilla/tamaño/alternativa; con n=10 p95 coincide con máximo observado y no es garantía poblacional. Registrar ahorros ejecutados de cargas completas, sin restar trabajo hipotético. Persisten costes mundiales por meta de H1b y no hay prueba de servicio móvil/FPS.

## Resultados

Evidencias locales: `unity/TestResults/P0/H1d-20261023`.

### Puerta A favorable

Contrato publicado `a6dd6aa`. Primera ejecución `Regional-20261023T190923`: **24/48**, 74,549 s. El comparador inicial exigía igualdad de coste por ordinal de arista y falló en las ocho semillas C. `NavGraph.BuildEdges` ordena por origen/destino, sin desempate de paralelas; el orden entre aristas físicas paralelas puede variar al ordenar el grafo mundial o regional. Se conserva ese XML y `gate-a.json`. No se alteran prototipo, generador, geometría ni oráculo para corregirlo.

Comparador corregido: biyección consumible una sola vez entre aristas con mismo origen/destino, coste, tipo y trayectoria física completa (puntos, normal, plano y salida), con igual cardinalidad y sin descartar duplicados. Los portales se emparejan usando esa misma biyección. No equivale a ignorar diferencias de coste: cada coste debe existir exactamente con su multiplicidad y trayectoria. El ordinal de arista deja de confundirse con identidad física; los padres del solver se comprueban por aristas físicas válidas y descenso de coste, no por igualdad de ordinal entre representaciones.

Repetición `Regional-20261023T191239`: **48/48**, 127,782 s, **144 evaluaciones** A/B/ocho C/seis fixtures ×16/32/64×S/M/L. **3621138 nodos, 27772185 aristas, 705479 portales** comprobados; 440 correspondencias de arista con distinto ordinal. Propiedad única, claves/capas/alturas, consultas de nodo, universos completos con multiplicidad, SCC locales/globales, componentes débiles y arcos iguales. Biyección completa del grafo dirigido: alcance idéntico para cualquier pareja; además controles de reentrada, caída sin retorno, tejados superpuestos, escalada y paso S/M frente a L. No es una simulación de cuerpos.

El prototipo `ReusedRegionalWorld` no construye `NavGraph`; crea propiedad/índices/portales sobre el grafo validado y conserva referencias a sus trayectorias. El kernel de topología se copia sin cambios de D2 a tests. `ReusedCorridorProbe` es una copia mecánica de H1b sustituyendo únicamente dos nombres de tipos. Verificación reproducible: `python scripts/p0-h1d-report.py --verify-solver`; fuente H1b original normalizada SHA256 `c63d5aecaf797f9b973cbb1b01c795ce9201a4f9452dea36668d758a2b51aa88`. Ninguna clase nueva se usa en producción.

Comando A: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.GraphReuseTests -Category GraphReuseA`. Intento `190918` sin tests por licencia 198 en sandbox, repetido fuera. Extractor: `python scripts/p0-h1d-report.py unity/TestResults/P0/Regional-20261023T191239/edit.xml unity/TestResults/P0/H1d-20261023/gate-a-bijection.json`. A favorable abre B; no acredita todavía tiempos, memoria ni rodeos de la vista.

### Puerta B

**Compatibilidad H1b favorable:** `Regional-20261023T205357`, **48/48**, 147,679 s. Mismo corpus de A y muestras H1b, añadiendo explícitamente el peor par RAMPA42 conocido. **41944944** contrastes de alcance contra el oráculo monolítico, **35771405** padres físicos descendentes y **36395** rutas reconstruidas. Distancias y potenciales iguales al H1b original contemporáneo; los ordinales de padres paralelos no son identidad física, su continuidad/coste se valida en el grafo exacto.

Peor p95 por evaluación **1,113474×**, máximo **1,894511×**, iguales al cierre H1b. Z9K2 64 m S 62→10514 conserva coste 50. La sustitución de preparación no usa la unión local de H1c ni modifica el algoritmo de rutas. Comando: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.GraphReuseTests -Category GraphReuseQuality`; extractor `python scripts/p0-h1d-report.py unity/TestResults/P0/Regional-20261023T205357/edit.xml unity/TestResults/P0/H1d-20261023/quality.json`.

Primer intento de cargas `Regional-20261023T205700`: 0/9, 0,048 s, detenido antes de cargar porque el control de `GC.GetAllocatedBytesForCurrentThread` devolvió cero para una reserva de 4096 bytes. Se conserva la evidencia; no son nueve cargas fallidas ni cero asignaciones reales. El arnés registra ahora `allocated=-1` y `allocationProbeBytes=0` cuando el contador no es verificable, sin relajar presupuestos ni alterar las cargas. La medida exacta disponible de arrays es su payload retenido, no la asignación acumulada total.

Muestreo managed cada ~1 ms durante la carga, memoria retenida con GC al final y observación Unity al final; las colecciones forzadas previas/posteriores y los recuentos están en `diagnosticMs`, fuera de `totalMs`. Cada carga ejecuta las mismas validaciones originales dentro de `totalMs`. `transientObserved` es el exceso del pico managed muestreado sobre la memoria retenida final, no un pico certificado de transitorios ni de memoria nativa. CPU del proceso incluye el observador y trabajo de Editor, no CPU exclusiva del algoritmo. Monitor externo cada 2 s en `editor-memory.csv`: working set/privados/pico del proceso, separados de asignación Unity y del presupuesto específico de navegación. No adopción ni H2.

### Preparación completa: ahorro realmente ejecutado

Ejecución final `Regional-20261023T205900`: **9/9**, 642,524 s. Cada test contiene diez pares independientes, **180 arranques completos: 90 control y 90 reutilizados**. Alternancia de orden por repetición, tres clases retenidas simultáneamente en ambas alternativas, sin datos construidos por el control que alimenten al prototipo. Mismo número de construcciones/reparaciones/rechazos por pareja: MAMPORRO dos construcciones por un rechazo; Z9K2/RAMPA42 dos por una reparación. No se elimina ninguna validación. Arranques de navegación en un Editor de tests, no 180 lanzamientos fríos del Editor ni medidas de FPS/build.

| Semilla | Región | Original p95 / máximo | Reutilizado p95 / máximo | Ahorro medio real | Reducción media |
| --- | ---: | ---: | ---: | ---: | ---: |
| MAMPORRO | 16 m | 5,769 s | 2,223 s | 3,605 s | 62,80 % |
| MAMPORRO | 32 m | 5,046 s | 2,150 s | 2,881 s | 57,50 % |
| MAMPORRO | 64 m | 4,678 s | 2,149 s | 2,518 s | 54,23 % |
| Z9K2 | 16 m | 5,660 s | 2,036 s | 3,612 s | 64,16 % |
| Z9K2 | 32 m | 4,980 s | 2,054 s | 2,888 s | 58,76 % |
| Z9K2 | 64 m | 4,527 s | 2,057 s | 2,481 s | 55,06 % |
| RAMPA42 | 16 m | 5,482 s | 1,986 s | 3,497 s | 64,18 % |
| RAMPA42 | 32 m | 4,767 s | 2,001 s | 2,754 s | 58,41 % |
| RAMPA42 | 64 m | 4,382 s | 1,967 s | 2,404 s | 55,19 % |

Percentil de rango más próximo, n=10 por celda/alternativa: p95 coincide con máximo. **90/90 reutilizados <4 s y <6 s**; mínimo 1,928 s, máximo 2,223 s. Control 90/90 >4 s, ninguno >6 s. Todas las parejas ahorran tiempo; menor ahorro individual 2,378 s. Objetivo temporal favorable en estas muestras, no garantía poblacional.

Generación/comprobación previa/grafos originales/validación/circuito se mantienen, con base media por celda **1,903–2,086 s**. Construcción regional completa media pasa de **2,436–3,656 s** a **36,4–51,5 ms**, incluyendo índices de propiedad, aristas, SCC y portales. Índice H1b reutilizado añade **7,5–11,2 ms** medios por celda. El residual exterior se conserva (hasta 3,432 ms en reutilizado). Todos los componentes originales acumulados y tiempos de topología están en cada fila `LOAD`; no se sustituyen por los tiempos de solo la última tanda de grafos.

Sumas de tiempo de carga: control **449,638 s**, reutilizado **183,249 s**. Incluyendo GC instrumental, recuentos y observador inicial/final (`diagnosticMs`): **454,415 / 188,060 s**, máximos individuales **5,831 / 2,284 s**. No se oculta la instrumentación para afirmar cumplimiento. CPU de proceso media **7,907 / 4,945 s por arranque**, rangos control 7,047–8,797 s y reutilizado 4,703–5,219 s; puede superar el tiempo de pared por los hilos de Editor/observación. No equivale a CPU exclusiva del constructor.

Comparación histórica con H1c, explícitamente **heredada**: MAMPORRO 5,851/5,119/4,815 s; Z9K2 5,756/4,982/4,582 s; RAMPA42 5,795/5,050/4,655 s a 16/32/64 (tres arranques por celda el día 22). Confirma el mismo cuello heredado; los porcentajes de ahorro superiores proceden del control contemporáneo de diez repeticiones, no de restar estas mediciones de días/arneses distintos. En H1c la proyección era incompleta; H1d sí ejecuta toda la preparación alternativa con portales e índice de rutas.

### Memoria: bloqueo de aceptación

El indicador conservador `transientObserved` supera **96 MiB en 44/90 cargas reutilizadas**: 18 de MAMPORRO y 26 de RAMPA42; 18/13/13 a 16/32/64. Máximo **117551104 B = 112,105 MiB**. El control no supera 96 MiB en ese indicador (máximo 65,176 MiB). **Puerta de memoria no aceptada**, sin elevar límites ni adoptar el prototipo.

Esta observación pertenece al heap managed completo del arnés y mide pico menos retenido final tras GC, incluyendo basura pendiente de recoger. **No demuestra que la proyección regional por sí sola reserve 112 MiB de scratch**, ni certifica el pico exacto de transitorios de navegación. La base managed fluctúa entre cargas y el prototipo retiene menos que el control: por eso un exceso sobre su retenido menor puede ser mayor aunque el pico total sea parecido. No se atribuye la causa a una fase sin perfil fiable; tanto el exceso conservador observado como la falta de certificación bloquean la aceptación de memoria.

Máximos por tamaño sobre las tres semillas, MiB:

| Región | Payload control / reutilizado | Pico managed control / reutilizado | Retenido managed control / reutilizado | Exceso sobre retenido control / reutilizado |
| --- | ---: | ---: | ---: | ---: |
| 16 m | 71,274 / 43,543 | 236,543 / 238,809 | 179,492 / 127,789 | 57,066 / 112,105 |
| 32 m | 64,546 / 42,706 | 233,426 / 233,203 | 168,254 / 123,637 | 65,176 / 110,105 |
| 64 m | 62,017 / 42,256 | 232,633 / 231,699 | 168,359 / 123,668 | 64,336 / 109,312 |

Payload cuenta arrays de grafos originales **una vez**, vista/topología/portales y solver, y añade grafos regionales físicos únicamente en el control. No incluye todas las cabeceras, capacidades de colecciones ni datos de consultas: **no certifica residencia ≤160 MiB**, aunque el payload reutilizado medido sea 40,989–43,543 MiB. Arrays del solver máximo 8495772 B (~8,10 MiB), por debajo de 20 MiB; scratch explícito máximo del índice 176332 B, no scratch total de carga. Pico managed observado máximo 250408960 B (~238,809 MiB) <256 MiB; Unity asignado observado al final máximo 355988065 B (~339,497 MiB) <512 MiB, sin pico nativo certificado.

Contador acumulado de asignaciones **no disponible en las 180 cargas** (control de 4 KiB devuelve 0); `-1` es un centinela, no bytes negativos medidos. Payload es asignación retenida de arrays comprobable, no total de asignaciones. Colecciones gen0 observadas durante carga: 2032 control, 1397 reutilizado; incluyen el entorno instrumentado, no certificar cero GC ni deducir bytes asignados de esos recuentos.

Monitor externo: Editor PID 23692, 301 muestras, peak working set máximo **1331990528 B**; worker PID 27504, 300 muestras, **625360896 B**. Es memoria de procesos de Editor, no residencia de navegación ni asignación Unity ni una build jugable. No combinarla con payload como si fueran categorías disjuntas.

### Reproducción, límites y cierre

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.GraphReuseLoadTests -Category GraphReuseLoad
python scripts\p0-h1d-report.py unity\TestResults\P0\Regional-20261023T205900\edit.xml unity\TestResults\P0\H1d-20261023\preparation.json
python scripts\p0-h1d-report.py --summary unity\TestResults\P0\H1d-20261023
python scripts\p0-h1d-report.py --verify-solver
```

`summary.json`: **exactGate PASS_SAMPLED, qualityGate PASS_SAMPLED, preparationGate PASS_SAMPLED, memoryGate FAIL_OBSERVED, overallGate NOT_ACCEPTED**. El nombre de la puerta de memoria se refiere al indicador observado definido arriba, no a una atribución certificada a navegación. Los XML fallidos iniciales y extractos se conservan; ninguna medida heredada se presenta como nueva. No Edit completo, Play, build, FPS ni carga móvil/scheduler nuevos. La búsqueda mundial por meta de H1b permanece igual; mejorar preparación no demuestra servicio viable con 3000.

**Siguiente paso exacto: revisión del autor de H1d.** Exactitud, calidad y tiempo favorables; bloqueo de memoria/transitorios y asignación no verificable. Una autorización posterior puede abordar medición fiable y atribución del exceso, conservando validaciones y casos; no optimizarlo ni integrarlo automáticamente. Sin producción, H2–H4/D4/D5 ni nueva investigación. Publicación, huellas y estado final en PROGRESO_P0.

Integridad final: **9/9 protecciones y 11/11 referencias intactas**, manifiestos antes/después y fuentes/evidencias en `H1d-20261023`. Diff vacío frente a `4795500` en Core/U3/src/referencias y fuentes H1b/H1c; **198 enlaces locales comprobados**. Siete ajustes Unity conservados fuera de commits, sin restaurar. Contrato `a6dd6aa` y prototipo/A `d1ab681` publicados; cierre en commit español y push normal, SHA localizable desde PROGRESO_P0. Evidencias locales ignoradas por Git conforme a política; código, informe y comandos versionados.
