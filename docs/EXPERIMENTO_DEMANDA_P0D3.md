# P0-D3 — consultas exactas bajo demanda

**Continuación del 20/10/2026:** el autor autorizó evaluar preprocesado regional exacto. [Puerta inicial negativa y parada antes del prototipo](EXPERIMENTO_PREPROCESADO_P0D3.md). Este documento conserva las medidas del 19/10; no se ha optimizado ni integrado el candidato de demanda.

## Alcance y estado

Experimento aislado autorizado el 19/10/2026, desde `599beec`. **Resultado negativo para desbloquear D3.** Exactitud verificada; mejora localizada con consumidores próximos y metas repetidas, pero mala escala con consumidores dispersos y metas que recorren el mapa. Con 750 dispersos, el recorrido móvil da p95 S/M/L **85,017/102,883/0,633 s**, cobertura de la meta vigente **0,429/0,222/2,722 %** y 750 pendientes al terminar. No se integra ni se optimiza este candidato después de la evaluación; parada para revisión del autor.

Código `3aaac6f`. No modifica EnemyNavigation, backend activo, generador, economía, web ni mundo. D4/D5 no iniciados. Las medidas son demandas sintéticas, no persecución física ni FPS. Presupuesto 3000 y objetivos 0,3/0,5 s intactos.

## Contrato del prototipo

`DemandNavService` pertenece a una instancia inmutable de `NavPortalWorld` y a una clase S/M/L. La caché reserva dos búsquedas por objetivo (capacidad configurable 1–4); no guarda una búsqueda independiente por consumidor. Cada búsqueda es Dijkstra inverso sobre las aristas físicas D2, sin cambiar sentido, costes ni trayectorias. Cuando un origen se extrae del heap, su distancia y su cadena de padres ya son exactas: los costes positivos garantizan que el padre está resuelto antes. Se desempata por índice de arista. No se espera a cubrir el mundo ni se publica un campo global.

Los consumidores comparten las etiquetas ya resueltas para la misma meta. La búsqueda se pausa cuando no tiene usuarios pendientes; puede reanudarse ante otro origen. No promete explorar únicamente nodos de las rutas finales: para certificar optimalidad debe explorar también alternativas. Una demanda dispersa o inalcanzable puede obligarla a agotar casi todo el grafo alcanzable.

Las entradas del arnés contienen origen/meta deseados y fecha del cambio. El servicio las muestrea dentro del presupuesto, una vez por consumidor y tick, y congela la solicitud aceptada. Cambios posteriores solo actualizan la entrada deseada; no cancelan la solicitud. Cuando termina se atiende el deseo vigente. **No se encola cada muestra del jugador a 60 Hz**: se combina la entrada más reciente mientras hay una solicitud en curso. La latencia incluye la espera desde que se solicitó esa meta, y se cuentan también publicaciones que ya no corresponden al objetivo vigente.

Estados explícitos: `Pending`, `Reachable`, `Unreachable`. Una distancia tentativa no es respuesta. Inalcanzable se publica solo al agotar la búsqueda exacta. Una respuesta incluye origen, objetivo, revisión, generación de caché, coste, siguiente arista y tiempos. La ruta es una vista con token sobre padres ya resueltos; expulsar la entrada invalida la vista y hace que la consulta vuelva a pendiente, nunca inalcanzable. Leer cada arista es O(1); no se oculta una reconstrucción/copia mundial dentro de la publicación. El recorrido completo de rutas en el oráculo se realiza fuera de la CPU medida.

Admisión FIFO de capacidad igual al número de consumidores, como máximo una solicitud pendiente por consumidor. Una entrada con usuarios pendientes no se expulsa; las libres se sustituyen por antigüedad determinista. Una consulta aceptada progresa aunque cambie la entrada. Las búsquedas de una clase se alternan por unidad y el scheduler existente garantiza cuota inicial 1000 por clase, 3000 globales. Eso evita inanición por población, pero no garantiza cumplir el plazo: una cola lenta sigue siendo un resultado negativo.

`Invalidate()` cambia la revisión visible inmediatamente y cobra el barrido de consumidores y caché en Advance; los tokens antiguos dejan de ser válidos. Es una invalidación explícita, no el mecanismo para seguir una meta móvil. La geometría inmutable no se parchea: un mundo físico nuevo requiere otro servicio con su nueva instancia. No implementa streaming/residencia D4. Las generaciones de 64 bits usan incremento comprobado, sin reutilizar etiquetas por desbordamiento.

## Trabajo y memoria

Se cobra cada registro de inicialización, registro del índice inverso, extracción, examen de arista, relajación/semilla, visita de consumidor, admisión/gestión de caché, publicación y transición administrativa. La inicialización de etiquetas es perezosa por generación y por nodo descubierto; no hay limpieza mundial por cada nueva meta. Las comparaciones del heap O(log V) quedan dentro de extracción/inserción como en los candidatos anteriores; se mide su CPU. La suma de categorías coincide exactamente con el retorno de Advance y el scheduler. Examen y relajación se ejecutan en pasos distintos y ambos se cobran; no se agrupan más operaciones para aparentar menos trabajo. Esto es conservador frente a la relajación interna fusionada del lote; no convertir la diferencia de trabajo entre arneses en un porcentaje universal de velocidad.

El índice inverso se construye una vez en Advance, cobrando también esa primera espera. Los arrays se reservan durante preparación (setup medido por separado), igual que en lotes; esa reserva y la carga física D2 no son generación de mundo medida en el tick. El payload incluye arrays retenidos de índices, dos búsquedas y consumidores, excluye grafos D1/D2, cabeceras y transitorios. El heap administrado observado incluye Editor y arnés; no representa residencia exclusiva de navegación. Asignaciones solo se informan si el contador detecta un testigo de 4096 B.

## Verificación y demandas

Específicas finales **35/35**, 141,137 s (`Regional-20261019T175306`), tras primera 34/34, 140,223 s (`174735`): comparación A/B/ocho C × 16/32/64 × S/M/L, 24 orígenes distribuidos y hasta cuatro metas, costes/alcance contra oráculo monolítico por cola, controles D2 y reconstrucción de rutas con aristas físicas y desempate canónico. Fixtures de capas desconectadas, caída/escalada y paso estrecho S/M sin L; invalidación, FIFO y determinismo. Se verifica que una solicitud admitida termina con origen/meta originales aunque cambie la entrada, que la búsqueda pausa sin usuarios y que volver a una meta reutiliza caché. Primer intento `174726`: licencia 198, sin pruebas. No se pretende cubrir todas las parejas de nodos; sí cada consulta de este corpus.

Arnés ejecutado: HORDA750, regiones 16/32/64, 300/500/750 consumidores (80 % S, resto M salvo un L), concentrados en radio 8 m y altura próxima al origen, o dispersos por el mapa. Posiciones fijas de los consumidores: demandas, no movimiento simulado. Orígenes ordenados por identidad física para comparar tamaños sin cambiar la distribución. Objetivo fijo, alternancia entre vecinos cada 12 ticks y recorrido de ida/vuelta por hasta 32 nodos Walk bidireccionales, mayor que la caché. Cada combinación empieza fría y dura 10800 ticks a 60 Hz (180 s). Oráculos, preparación de entradas y métricas quedan fuera de CPU/GC de navegación.

**Tres repeticiones completas**, 54 escenarios y 18 cambios vecinos por repetición: suites 3/3 en `Regional-20261019T180015` (33,925 s), `180059` (35,092 s) y `180142` (34,028 s). `load-final.json`: 486 filas por clase, 162 totales y 54 cambios vecinos. Ninguna discrepancia determinista entre repeticiones de la misma combinación; CPU variable. Los tamaños tienen pequeñas diferencias en contadores por orden de nodos/heap, pero reproducen los percentiles tabulados a su precisión. Este prototipo opera sobre la unión física D2, no acelera por elegir otra partición.

Primera carga 3/3 (`175626`, 33,667 s) conservada como `load-first-wrap.json`: volvía al inicio del recorrido con un salto al cerrar el ciclo. Se corrigió solo el arnés a ida/vuelta por aristas vecinas y se repitió; las tablas siguientes usan exclusivamente el recorrido corregido. No se han cambiado física, presupuesto ni algoritmo para favorecer resultados.

Se contrastan costes/alcance de cada respuesta publicada, incluso obsoleta, y rutas completas de muestras por clase/meta. Respuesta p50/p95/p99/máxima, primera respuesta, edad cronológica, cobertura del deseo vigente, tiempo sin ninguna respuesta, respuestas inalcanzables, pendientes, publicaciones obsoletas, trabajo por clase, CPU y memoria. La edad de una respuesta fija crece aunque siga siendo exacta: no confundir antigüedad con retraso. Una buena latencia entre las respuestas terminadas no permite ignorar pendientes ni baja cobertura.

## Resultados de reacción y cobertura

Cambio a un vecino **después** del ensayo fijo (índice construido, nueva meta sin caché), todas las respuestas terminadas. Valores iguales a 16/32/64 y en las tres repeticiones:

| Consumidores | Distribución | Unidades globales | Ticks hasta todos | Respuesta p95 S/M/L (s) |
| --- | --- | --- | --- | --- |
| 300 | Concentrados | 45311 | 16 | 0,217 / 0,233 / 0,033 |
| 500 | Concentrados | 48513 | 17 | 0,250 / 0,250 / 0,033 |
| 750 | Concentrados | 57764 | 20 | 0,300 / 0,250 / 0,033 |
| 300 | Dispersos | 1646893 | 550 | 8,900 / 8,667 / 0,300 |
| 500 | Dispersos | 1788510 | 597 | 9,667 / 9,033 / 0,300 |
| 750 | Dispersos | 1992760 | 665 | 10,800 / 9,350 / 0,300 |

Mejora real: el caso concentrado evita explorar todo el mundo, hasta **19,1 veces menos unidades** que las 1101086 del lote para 750. Es una demanda más acotada, no equivalencia de cobertura mundial. El disperso supera el trabajo del lote y tarda **11,083 s** hasta todos; no resuelve el bloqueo. En frío, con 750, primera respuesta p95 S/M/L: concentrados **9,950/9,750/5,900 s**, dispersos **20,450/18,800/6,183 s**. El índice inicial se cobra y no se oculta precalentándolo.

Alternancia de solo dos vecinos: p95 de respuesta **0,033–0,050 s**, dominado por hits tras llenar la caché. Con 750 dispersos todavía hay primera respuesta p95 **32,517/27,933/6,167 s** y máximos de respuesta **22,950/19,117/0,483 s**; cobertura media vigente **79,713/81,871/88,491 %**. La espera fría y los pendientes no desaparecen por tener un percentil final pequeño. La latencia por solicitud corresponde a la meta muestreada/admitida; la primera respuesta desde t=0 se informa por separado, porque durante la indexación la meta móvil ya ha cambiado.

Recorrido móvil de ida/vuelta, 750 consumidores, ejemplo 64 m (mismos percentiles a 16/32). Respuesta solo de consultas completadas, pendientes sin fingir un tiempo final:

| Distribución/clase | p50 / p95 / p99 respuesta (s) | Máximo (s) | Edad p95 (s) | Cobertura vigente | Pendientes al final |
| --- | --- | --- | --- | --- | --- |
| Concentrados S | 0,733 / 86,517 / 100,183 | 101,817 | 139,967 | 0,106 % | 600 |
| Concentrados M | 4,900 / 30,000 / 31,983 | 35,317 | 52,983 | 0,072 % | 149 |
| Concentrados L | 0,250 / 0,533 / 0,600 | 0,600 | 0,850 | 20,222 % | 1 |
| Dispersos S | 31,617 / 85,017 / 141,517 | 158,483 | 160,333 | 0,429 % | 600 |
| Dispersos M | 15,533 / 102,883 / 142,117 | 155,133 | 158,783 | 0,222 % | 149 |
| Dispersos L | 0,467 / 0,633 / 0,667 | 0,683 | 0,967 | 2,722 % | 1 |

En ese disperso se publican **703/237/460** respuestas, **671/220/451** ya para una meta distinta de la vigente. Tiempo-consumidor sin ninguna respuesta: **23,900/13,004/3,528 %**. Espera p95 de la solicitud en cola/en curso cuando está pendiente: **128,067/137,150/0,500 s**. Todos los consumidores tienen una consulta vigente pendiente al cierre; el techo se consume completo: **32400000 unidades en 180 s**, 3000 en cada tick. No se confunde progreso FIFO con servicio oportuno ni con persecución física útil.

El fallo no empieza en 750: móvil disperso p95 S/M/L **92,233/114,650/0,633 s** con 300 y **96,617/123,983/0,633 s** con 500. Las cohortes aceptadas conservan metas antiguas distintas; fijarlas evita cancelación infinita, pero la cola y las expulsiones impiden responder a la meta actual a tiempo. La demora de 750 no es necesariamente monotónica en cada percentil porque cambian orígenes, cohortes y consultas que logran terminar.

## CPU, memoria y desglose

Rangos de tres repeticiones × tres tamaños, 750 consumidores. CPU solo del servicio/scheduler, en Editor, sin consumidores físicos ni render:

| Distribución/modo | CPU total 180 s (ms) | CPU tick p95 (ms) | Mayor tick observado (ms) |
| --- | --- | --- | --- |
| Concentrados fijo | 114,470–123,510 | 0,0123–0,0160 | 0,2334 |
| Concentrados alternancia | 177,459–213,488 | 0,0484–0,0658 | 0,1780 |
| Concentrados recorrido | 393,074–419,225 | 0,0402–0,0546 | 0,2622 |
| Dispersos fijo | 143,588–150,789 | 0,0510–0,0530 | 0,1258 |
| Dispersos alternancia | 192,240–231,046 | 0,0507–0,0570 | 0,5905 |
| Dispersos recorrido | 434,831–472,147 | 0,0451–0,0636 | 1,1446 |

CPU baja por tick no compensa los segundos de retraso bajo el presupuesto de trabajo. Payload retenido con 750: **10482896 B** (S/M/L **4505052/3843248/2134596 B**), frente a 6380596 B del lote 64 m; excluye grafos/objetos/transitorios. Se reservan arrays densos por entrada aunque se inicialicen/exploren perezosamente: no se ha resuelto residencia regional. Heap administrado observado en los escenarios 750: **153575424–196030464 B**, incluye Editor y arnés, no memoria exclusiva. El contador de asignaciones falla el testigo en los **162 escenarios**: N/D, no cero GC. Setup por escenario se conserva en JSON y no se suma a CPU de tick.

Ejemplo 64 m, disperso móvil, primera repetición:

| Clase | Trabajo | Mantenimiento | Índice | Inicialización | Extracciones | Aristas | Semillas/relajaciones | Publicaciones | Control |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| S | 11179145 | 6046683 | 706249 | 290732 | 236338 | 1826026 | 1826048 | 703 | 246366 |
| M | 10949271 | 1524542 | 604820 | 533815 | 479704 | 3658150 | 3658173 | 237 | 489830 |
| L | 10271584 | 21357 | 331188 | 603773 | 530390 | 4121594 | 4121865 | 460 | 540957 |

Expulsiones S/M/L: **35/44/456**. CPU por clase y resto de combinaciones en JSON. El sondeo de 600 consumidores S por tick añade un coste importante y medido; no se excluye para mejorar artificialmente los números. No hay inanición por cancelación de una petición admitida, pero sí retrasos incompatibles con los objetivos.

## Comparación, alternativas y parada

Control de lotes: vecino 1101086 unidades globales, móvil entre dos vecinos p95 S/M/L 7,833/6,750/3,700 s. La caché de dos metas mejora mucho esa alternancia después del arranque; el lote recalculaba. Esto no permite afirmar una mejora general: en el recorrido nuevo las metas salen de caché y la cobertura cae. Ese recorrido no se había medido en el lote; no inventar una comparación directa de sus percentiles. Tampoco comparar una respuesta cercana con la publicación mundial como si certificaran igual cobertura.

**No integrar. D3 sigue bloqueado.** Recomendación para revisión: estudiar un preprocesado exacto reutilizable que reduzca la búsqueda ante nuevas metas, con puerta inicial de coste de carga/memoria antes de implementar un sistema completo. Otra opción es evaluar consultas exactas con heurística admisible y agrupación explícita de cohortes de objetivo; tendría que demostrar ventaja con 750 dispersos y no ocultar pendientes/cancelaciones. Son propuestas sin implementar ni autorizar en este cierre. No aumentar caché o presupuesto a ciegas, no relajar reacción ni pasar a aproximaciones sin decisión del autor.

## Reproducción y cierre

Desde CMD, raíz del repositorio:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.DemandNavTests -Category DemandEquivalence
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.DemandNavLoadTests -Category DemandLoad
python scripts\p0-d3-demand-report.py unity\TestResults\P0\D3-Demand-20261019\load-final.json unity\TestResults\P0\Regional-20261019T180015\edit.xml unity\TestResults\P0\Regional-20261019T180059\edit.xml unity\TestResults\P0\Regional-20261019T180142\edit.xml
scripts\u3.cmd play
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

Play nuevo **53/53**, 113,005 s; referencias U0/U2/U3/U4 nuevas coincidentes, instantánea `qa-results/reference-96f18c8e2ab840dcacbf8b10148ae889`. No se repitió Edit completo: específicas nuevas 35/35 y carga 3/3 × tres; base Edit 960/960 anterior permanece heredada. Sin build nueva. Evidencias, fuentes y huellas en `unity/TestResults/P0/D3-Demand-20261019`, XML/log específicos en sus carpetas Regional. Huellas **9/9** y corpus **11/11** intactos; siete ajustes locales Unity excluidos de commits. Publicación y siguiente paso exacto en PROGRESO_P0: revisión del autor antes de integrar o cambiar de arquitectura.
