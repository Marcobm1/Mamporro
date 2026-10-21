# P0-D — construcción y navegación regional

Implementación optativa autorizada D1–D4; D5 excluido. Mapas de prueba 80/128 m; sin integración en partidas normales ni cambios de generador, balance o presupuesto de 3000 nodos/tick.

## Landmarks dirigidos — 20/10/2026, posterior al preprocesado

Puerta 1 autorizada y evaluada: 2/4/8 referencias, A* exacto offline. Preparación completa a 64 m 3,598–3,905 s; mejora individual real, pero mejor cohorte dispersa 9889018 unidades frente a 1992760 de demanda compartida heredada. **No justifica Puerta 2: parada para revisión, D3 bloqueado.** [Informe, pruebas y límites](EXPERIMENTO_LANDMARKS_P0D3.md). Ningún servicio nuevo ni integración; D4/D5 sin iniciar, presupuesto y objetivos intactos.

## Puerta de preprocesado exacto — 20/10/2026

Experimento autorizado desde `f355409`, detenido en evaluación inicial: construcción más transferencias regionales completas S/M/L requiere 4,413–4,967 s en HORDA750 × 16/32/64 × tres repeticiones, antes de almacenar tablas y validar. Excede objetivo de 4 s; no excede máximo de 6 s. Matriz mundial de distancias 13,80 GiB, descartada sin reservar. Las tablas regionales son menores pero no superan la puerta temporal. **Sin prototipo de servicio ni integración; D3 bloqueado y parada para revisión.** [Medidas nuevas, límites y alternativas](EXPERIMENTO_PREPROCESADO_P0D3.md); pruebas/protecciones/publicación en PROGRESO_P0. No iniciar D4/D5 ni otra arquitectura.

## D3 — demanda exacta del 19/10/2026

Autorización ejecutada: búsquedas inversas por consumidor activo compartidas por clase/meta, caché acotada, FIFO y estados explícitos. **Exactitud correcta, resultado negativo de rendimiento.** [Informe reproducible completo](EXPERIMENTO_DEMANDA_P0D3.md): vecino caliente 750 concentrados 57764 unidades, dispersos 1992760; recorrido móvil disperso p95 S/M/L 85,017/102,883/0,633 s, cobertura 0,429/0,222/2,722 %, 750 consultas vigentes pendientes al cierre. 3000 intactos. Mejora localizada por reutilización/cercanía, no solución general ni persecución física. Código `3aaac6f`, medidas `2940d9b`; pruebas/protecciones en PROGRESO_P0. No integrar, no D4/D5; esperar revisión del autor antes de otra arquitectura. Las recomendaciones inferiores corresponden a experimentos anteriores ya ejecutados.

## D3 — experimento de lotes exactos del 18/10/2026

El autor autorizó la alternativa 1 propuesta más abajo. Se han verificado agrupación reversible, propagación regional multiobjetivo y scheduler aislado: **resultado negativo para reacción**. [Reglas, equivalencia, tablas 16/32/64 × S/M/L, CPU, memoria y comparación](EXPERIMENTO_LOTES_P0D3.md). A 64 m, vecino: 1101086 unidades globales y hasta 6,133 s; móvil p95 7,833/6,750/3,700 s. La exploración de campos completos sigue dominando aunque ya no se calcule una transferencia por salida. No seguir construyendo campos sobre este candidato, no integrar ni iniciar D4. Los informes anteriores se conservan como controles; el cierre de pruebas/publicación está en [PROGRESO_P0](PROGRESO_P0.md).

## D3 — evaluación incremental del 18/10/2026

Se recuperaron el comparador y su suite locales del 17/10, sin reiniciar D3. `IncrementalNavField` mantiene distancias g/rhs y un heap indexado sobre la unión física dirigida D2. Al cambiar meta repara inconsistencias hasta vaciar el heap; publica intercambiando buffers y sincroniza únicamente nodos modificados, cobrando también esas copias al planificador. La instancia pertenece a un mundo/clase inmutable; no implementa sustitución de geometría, campos locales, padres de movimiento, fallback ni integración jugable. Es el comparador autorizado, no la arquitectura adoptada.

El coste frío del prototipo regional se debe a calcular una transferencia regional por cada salida extraída: miles de salidas y repetición de exploraciones internas. La caché de cuatro entradas no reutiliza fuentes distintas durante esa pasada (cero hits). Ampliarla no reduce por sí sola ese primer cálculo y aumenta memoria; precomputar todas las parejas trasladaría el coste a carga sin demostrar que cabe. Reducir CPU por operación tampoco reduce los ticks necesarios bajo 3000 unidades. El comparador elimina esa multiplicidad de transferencias, pero cambiar la meta altera distancias a escala mundial: un desplazamiento vecino puede invalidar gran parte del campo y exigir múltiples reparaciones. Su baja CPU por tick tampoco acredita reacción rápida.

Primera suite nueva: 16/16, 63,011 s, `unity/TestResults/P0/Regional-20261018T094334/edit.xml`. Contrasta costes completos con relajación por cola independiente: A/B, ocho semillas C, S/M/L, cambios vecinos, superficies desconectadas y caídas dirigidas. Tres diagnósticos 16/32/64, dos modos de 10 800 ticks cada uno a 60 Hz. Fijo desde frío; móvil desde campo fijo completo, alternando dos vecinos Walk con retorno cada 12 ticks. Sin enemigos, movimiento, render ni comparación nueva de persecución monolítica. La igualdad del problema físico explica resultados iguales entre tamaños: el comparador mantiene un campo global.

Primera publicación S/M/L: **6,217 / 5,950 / 3,633 s**. Móvil: **7/8/14 publicaciones**, p95 **28,067 / 24,067 / 16,300 s**, trabajo global **32 400 000** por tamaño en 180 s simulados, techo 3000/tick respetado. Incumple ampliamente 0,3 s p95 / 0,5 s máximo. Exactitud y servicio de las clases no prueban persecución útil ni resuelven inanición física. Payload de buffers S/M/L: **2 708 052 / 2 330 924 / 1 293 280 B**, excluye grafos, geometría, cabeceras y transitorios; no es memoria residente total.

La primera instrumentación mezclaba la edad del campo fijo mantenido 180 s con la entrada al ensayo móvil, y los percentiles CPU fríos incluían inactividad. XML conservado. La versión de verificación separa edad móvil tras primera reparación publicada (espera inicial en latencia), CPU activa y CPU de todos los ticks, y asignaciones con testigo 4096 B. No interpretar edad cronológica de una meta que no cambia como retraso de reacción. Informe reproducible final: `python scripts/p0-d3-incremental-report.py <edit.xml> <informe.json>`; campos N/D se serializan como null.

**Verificación final nueva de EditMode:** `scripts\u3.cmd edit`, **894/894**, 1081,686 s, XML/log archivados en `unity/TestResults/P0/D3-20261018`. Incluye 16 del comparador, 71 transferencias, seis invariantes/diagnósticos de jerarquía y todas las regresiones. `incremental.json` y `hierarchy.json` se extraen de ese XML con los scripts versionados. Se repiten exactamente las latencias y 7/8/14 publicaciones del comparador y las cero publicaciones regionales en 180 s. Que las pruebas pasen valida exactitud/invariantes y captura del fallo, **no los objetivos de rendimiento**.

| Comparador, tres tamaños | S | M | L |
| --- | --- | --- | --- |
| Edad móvil p95, tras primera reparación (s) | 47,850 | 41,100 | 23,333 |
| Edad móvil máxima, tras primera reparación (s) | 49,267 | 42,400 | 24,067 |

CPU de ticks activos fríos p95 para 16/32/64: **0,0904 / 0,0877 / 0,0844 ms** (393 ticks activos de 10 800). El p95 de todos los ticks fríos es 0,0001 ms porque incluye inactividad; no usarlo como coste de cálculo. Móvil, todos activos: p95 **0,0477 / 0,0486 / 0,0507 ms**, máximos **0,0823 / 0,0883 / 0,1157 ms**. Solo `scheduler.Tick`, excluye peticiones, preparación, aserciones, movimiento y render. La igualdad del trabajo acumulado y devuelto por el planificador se exige en cada escenario. Testigo de asignaciones no detectado en los seis casos: **N/D**, no cero certificado. Memoria residente exclusiva y picos siguen pendientes.

Jerarquía regional repetida en esa misma suite: CPU tick p95 **0,0425 / 0,0469 / 0,0576 ms**, máximos **0,0914 / 0,1038 / 0,1105 ms**, cero publicaciones S/M/L y mismo trabajo/transferencias que el 17/10. Confirma el cuello de botella previo con ejecución nueva. Una pasada por tamaño y modo; no son tres repeticiones Normal/Development ni FPS.

### Alternativas que requieren decisión antes de integrar

1. **Revisar la jerarquía regional (recomendación para el siguiente experimento):** agrupar portales con garantías físicas y medir propagación regional multiobjetivo por lotes, conservando extremos/capas/direcciones, cuotas, 3000 y versiones compatibles. Contrastar alcance, coste y caminos contra el oráculo antes de integrar. No basta elegir un portal por frontera: puede perder rutas, aumentar rodeos o cerrar el paso a L. La eficiencia aún no está demostrada; una aproximación exigiría acordar explícitamente su margen.
2. **Seguir investigando reparación global:** acotar las reparaciones y medir saltos de meta/altura. El comparador actual no es candidato de integración; mantiene residencia mundial y falla incluso entre vecinos. Cambiar a publicaciones parciales o demanda por consumidores cambia los contratos de consistencia/servicio y requiere revisión.
3. **Revisar objetivos o presupuesto:** no aplicado ni recomendado como solución automática. Se conserva 3000 y los umbrales aprobados; más trabajo por tick no corrige por sí solo la explosión de transferencias.

**D3 permanece bloqueado por rendimiento; D4 no iniciado.** No se implementan campos locales encima de un coste global que aún no llega a tiempo. Pendientes tras resolver la arquitectura: invalidación/versiones completas, integración QA, persecución física 300/500/750 fijo/móvil, jefe aislado/acompañado, llegada y comparación monolítica, memoria/asignaciones verificables; después D4. No se presenta este diagnóstico como cumplimiento de esos ensayos.

### Control monolítico repetido el 18/10

La suite completa vuelve a ejecutar el arnés antiguo de 300/500/750 fijo/móvil y el diagnóstico corregido D0 (12 escenarios × tres repeticiones observadas más control sin observador). Trazas nuevas en `D0/LegacyRuns/20261018T*` y `D0/Diagnosis/20261018T*`; resumen de las 36 trazas corregidas en `D3-20261018/monolithic-diagnosis.json`. `python scripts/p0-d0-report.py <carpeta Diagnosis de esta pasada>` confirma **cero discrepancias nodo/celda**. No sustituir la llegada estable por la primera altura alcanzada del arnés antiguo.

Se reproducen llegada estable de jefe fijo sin esbirros **67,350 s**, acompañado **69,550 s** y con 750 **71,983 s**. Con 750 móvil, **ninguna llegada estable en 180 s** y publicaciones S/M/L **372/3/0**; M conserva p95 **57,600 s**. Tercera repetición: CPU de navegación de producción p95 **0,6232 ms**, p99 **0,7877 ms**, máximo **0,9054 ms** (Editor lógico, sin render). Esa CPU favorable no elimina la inanición. El comparador incremental usa otras metas, no mueve cuerpos y contabiliza trabajo más estrictamente; no calcular un porcentaje de mejora de persecución entre estos arneses. Los resultados corrigen/completan la verificación pendiente, no el comportamiento del backend activo.

### Cierre verificable de esta evaluación

Código `7344587`, push normal realizado. Play nuevo: `scripts\u3.cmd play`, **53/53**, 112,144 s; XML/log en `D3-20261018`, primer intento por licencia 198 conservado aparte. Edit nuevo 894/894 indicado arriba. Referencias U0/U2/U3/U4 correctas con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`, instantánea `qa-results/reference-2d186a4e6b1d4e198089d06b993b0b31`. Huellas finales **9/9** idénticas, incluidos U6, u3-world y siete ajustes Unity. El Edit 878/878 y Play 53/53 del 17/10 son evidencias heredadas revisadas; el Play que figuraba en curso sí había terminado. No se ejecutó build nueva ni benchmark de FPS. Documentación publicada separadamente; solo siete ajustes protegidos quedan locales. Las alternativas anteriores son propuestas para consulta, no decisiones adoptadas.

## D1: constructor físico acotado (verificado el 15/10/2026)

`NavRegionGraph` construye una baldosa de 16/32/64 m y contexto de nodos. Coordenadas absolutas, alineadas a la rejilla mundial de 0,5 m. Todas las capas de una celda pertenecen a la misma baldosa. El nodo de origen posee cada arista y toda su trayectoria; los nodos de halo no generan aristas. La unión por celda mundial/capa/clase permite reconstruir la misma topología física sin duplicar propietarios.

El halo inicial es al menos 10 m: supera la búsqueda `SurfaceBeyond` de L (8,05 m), la caída por eje (6,5 m) y los vecinos Walk. No se supone suficiente para cualquier pared inclinada. Las búsquedas de destinos de escalada que salen del recorte, pero permanecen en el mundo, marcan contexto insuficiente; se duplica el halo y se reconstruye antes de devolver la región. Como límite se usa el mundo completo. Las consultas de cuerpo, apoyo, pared y barrido siempre usan la geometría mundial, por lo que una trayectoria no se recorta en la costura.

Las tapas se enumeran mediante candidatos espaciales ordenados, copiados a un búfer local antes de consultas que reutilizan el búfer de `VerticalQueries`. La construcción secuencial comparte las consultas mundiales, sin copiar su tabla de terreno por baldosa. No es todavía un constructor paralelo ni un sistema de residencia/prefeteo. No compartir el mismo búfer de consultas concurrentemente.

El constructor monolítico público conserva su recorrido y sigue siendo el control de A/B/C. No se activa el constructor regional desde el juego. El único cambio heredado de movimiento es la corrección aprobada y publicada en `5e41406`, ver [DIAGNOSTICO_P0D0](DIAGNOSTICO_P0D0.md).

## Validación

`RegionalGraphTests` compara nodos, alturas, identidad de capa/terreno, cada arista dirigida, coste, destino y cada punto/normal de escalada o caída contra el monolítico. Exige propietario único, cobertura completa y ausencia de aristas en los nodos de contexto. Comprueba alcance desde origen tras reconstruir la unión, y el fixture de costuras también se construye en orden inverso.

Corpus: A/B a 80 m; ocho semillas aprobadas C a 128 m; tres tamaños de baldosa y tres clases. Fixture adicional con rampa diagonal, capas superpuestas, esquinas, estructura mayor que la baldosa y paso estrecho. Los payloads informados son arrays directos de los grafos (incluidos halos), no memoria residente completa; los tiempos de construcción excluyen las aserciones de comparación. Suite regional del 15/10/2026: 28/28, 268,795 s, XML/log en `unity/TestResults/P0/Regional-20261015T173655`. Regresiones completas nuevas: Edit 757/757 y Play 53/53; XML/log en `unity/TestResults/P0/D1-20261015`. Referencias históricas coincidentes y nueve huellas protegidas intactas.

## Pendientes

D2: portales dirigidos y conectividad global, sin confundir todas las capas/componentes de una baldosa. D3: campos locales, invalidación y reparto justo, con la inanición M/L de D0 como problema prioritario; nunca aumentar 3000 para ocultarla. D4: cruces físicos y residencia/prefeteo. Render/mundo ampliado e integración normal excluidos.

## Medidas D1 del 15/10/2026

Comando: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1`. Ejecución nueva en Editor, sin render. El intento heredado del 14/10 falló por licencia 198 y no generó XML; no cuenta como prueba. CSV derivado del XML: `unity/TestResults/P0/D1-20261015/regions.csv`.

| Baldosa | Construcción por clase, ocho semillas C (ms mín.–máx.) | Arrays de todas las regiones por clase (B mín.–máx.) | Mayor región (B) |
| --- | --- | --- | --- |
| 16 m | 843,286–1881,051 | 5 952 053–12 149 305 | 270 907 |
| 32 m | 720,214–1232,990 | 4 498 858–9 695 945 | 827 390 |
| 64 m | 619,110–1038,254 | 4 015 447–8 818 172 | 2 307 305 |

HORDA750, suma de S/M/L: 16 m = 3444,024 ms / 28 492 587 B; 32 m = 2737,105 ms / 22 290 275 B; 64 m = 2371,721 ms / 20 242 963 B. Control monolítico: 19 258 640 B en sus arrays directos. Los halos aumentan el total de datos aunque reducen el tamaño de cada pieza. No se afirma ahorro de residencia global, p95, coste de carga completo ni cumplimiento de D3/D4; faltan campos, portales, listas internas, geometría y transitorios. No se fija aún tamaño definitivo: las regiones menores también ofrecen campos locales menores, todavía sin medir.

La igualdad exacta de todos los nodos/aristas y puntos de trayectoria acredita equivalencia física con el oráculo en este corpus. No demuestra cobertura universal ni sustituye las pruebas dinámicas de cruces y residencia de D4. Se conservan clases, envolventes, trayectoria completa, generador y presupuesto 3000. El controlador de enemigos continúa usando el backend histórico.

## D2 — portales exactos y conectividad (verificado el 15/10/2026)

`NavPortalWorld` construye las piezas D1 sin cambiar su constructor, y asigna un índice global a cada nodo propietario con su clave de celda/capa. Une las aristas reales; un destino ausente o incompatible provoca error, no conexión por proximidad. Cada clase tiene su propia instancia y su envolvente original. No hay integración en movimiento ni cambio de 3000 nodos/tick.

Cada `NavPortal` conserva regiones origen/destino, nodos globales y locales de ambas superficies, clase, dirección, tipo Walk/Climb/Drop, coste y referencia a la arista/trayectoria de su región de origen. Los puntos, normales y salida de borde permanecen en el grafo D1; se accede a ellos mediante esa identidad, sin copiarlos ni recortarlos. La anchura/altura libre es la envolvente ya validada por D1, no una nueva anchura estimada. No se deduce una arista inversa.

La conectividad débil se etiqueta por región solo para diagnóstico. Kosaraju iterativo obtiene las componentes fuertemente conexas internas usando únicamente aristas cuyos dos extremos están en la misma región. La condensación mundial incluye las conexiones dirigidas entre esas componentes, tanto internas como interregionales; permite salir y volver a entrar. También se etiquetan las SCC mundiales. No se considera que todas las superficies de una baldosa comuniquen.

Se deduplican solo los arcos entre componentes para consultas booleanas de alcance. Todos los portales físicos se conservan: agruparlos para costes o movimiento perdería información de extremos/recorridos. `NavPortalQuery` recorre esa condensación y reutiliza el resultado por componente origen. Para entregar una ruta concreta, calcula bajo demanda Dijkstra sobre la unión física exacta y conserva una sola fuente (distancias y padres); devuelve aristas físicas y coste exacto. Memoria lineal, sin matriz de parejas de portales. La caché pertenece a una instancia mundial/clase inmutable; un mundo nuevo necesita otra consulta. Es una referencia offline D2, no un cálculo sin presupuesto dentro del tick de juego. D3 debe resolver la planificación y los campos locales antes de activarlo en movimiento.

Cierre nuevo: Edit 801/801 y Play 53/53, además de 44/44 específicos. Código `7c50d9f`. Se mantienen los resultados heredados D1 arriba. D3 sigue teniendo prioridad explícita sobre la inanición M p95 57,6 s / L sin publicaciones en 180 s, conservando 3000. D5–D7 excluidos.

Primera pasada D2: `Regional-20261015T191728`, 34/37. Los 30 casos de corpus A/B/C y la barrera fueron correctos; tres expectativas de la nueva fixture de placas suspendidas fallaron porque el monolítico tampoco crea esas bajadas. Se conserva ese caso como ausencia de conexión (sin modificar D1 ni inventar aristas). La caída unilateral se verifica con una plataforma sobre soporte y voladizo de 0,25 m: tres clases correctas en `Regional-20261015T224319` (3/3). La equivalencia D2 exige respetar también las limitaciones del oráculo; no afirma navegabilidad universal de placas suspendidas.

Instrumentación de asignaciones: la primera ejecución devolvió 0 bytes incluso al construir grafos completos. Se añadió una asignación testigo de 4096 B para validar el contador antes de usarlo; si no la detecta, asignaciones = -1 (N/D), nunca «cero asignaciones demostrado». Se mide aparte pico gestionado muestreado cada 2 ms durante construcción, incluyendo Editor y otros objetos residentes; no es residencia exclusiva de navegación ni un pico exacto. Tiempos de consulta fríos se registran sin aserciones ni formateo; la consulta caliente reutiliza origen y buffers. El muestreador se detiene antes de las comparaciones. Pasada específica y suites completas correctas, detalladas abajo.

### D2: pasada específica válida del 15/10/2026

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalPortalTests`: **44/44**, XML/log `unity/TestResults/P0/Regional-20261015T224516`. Incluye 30 combinaciones A/B/ocho C × 16/32/64, cada una con S/M/L; barrera con salida/reentrada, placas superpuestas sin conexión, caída unilateral con voladizo de 0,25 m, escalada interregional con trayectoria completa, rampa diagonal/esquina/estructura multirregión, paso solo S/M y mundo vacío. Se compara la bijección de todas las aristas físicas, y alcance a todos los destinos desde fuentes distribuidas y un representante de cada SCC mundial; costes/rutas contra relajación por cola independiente. No se afirma barrido de todas las parejas de nodos ni cobertura universal.

Informe reproducible: `python scripts/p0-d2-report.py unity\TestResults\P0\Regional-20261015T224516\edit.xml unity\TestResults\P0\D2-20261015\regional-summary.json`. El JSON contiene por clase/caso portales, SCC/débiles internas, SCC mundiales, arcos, tiempos, arrays y picos muestreados. Las tablas siguientes proceden de esa ejecución específica, no de D1.

| Baldosa | Portales por clase (rango A/B/C) | SCC internas / débiles (rangos) | Construcción D1+D2 por clase (ms) | Topología D2 adicional (ms) | Máximo consulta fría de ruta (ms) |
| --- | --- | --- | --- | --- | --- |
| 16 m | 1944–15428 | 23–97 / 21–88 | 117,011–1727,548 | 4,190–55,879 | 8,222 |
| 32 m | 1012–6896 | 10–38 / 8–30 | 92,644–1228,515 | 4,137–68,122 | 6,721 |
| 64 m | 696–2558 | 4–18 / 3–11 | 70,667–1052,178 | 5,854–62,081 | 6,749 |

Rangos entre fixtures/clases, una construcción de cada combinación; no son p95 ni FPS. Máximos de alcance frío: 0,1084 / 0,0037 / 0,0029 ms (16/32/64). Consulta caliente de alcance+distancia+ruta, media de 100 llamadas por caso: rangos 0,292–1,920 / 0,303–1,650 / 0,306–1,764 µs; tamaño de ruta variable. No extrapolar a 750 consultas frías por tick.

HORDA750, portales S/M/L: **15058/12737/7970** (16), **6740/5935/3839** (32), **2296/2093/1486** (64). SCC internas S/M/L: 64/71/90, 16/22/33, 4/5/14; débiles: 64/66/82, 16/18/30, 4/4/11. SCC mundiales: **1/1/6** en los tres tamaños; L conserva sus restricciones físicas. La región no se trata como superficie única. La condensación necesita 382/369/337, 76/98/89 y 12/15/26 arcos dirigidos por clase; esa deduplicación solo sirve a alcance, nunca fusiona los portales de movimiento.

| Baldosa | Arrays físicos D1 por clase (B mín.–máx.) | Arrays topología D2 (B mín.–máx.) | Datos portales (B mín.–máx.) | Búferes consulta (B mín.–máx.) |
| --- | --- | --- | --- | --- |
| 16 m | 1 577 068–12 149 305 | 717 456–6 805 252 | 85 536–678 832 | 77 079–703 221 |
| 32 m | 1 198 933–9 695 945 | 717 056–6 803 484 | 44 528–303 424 | 77 004–702 976 |
| 64 m | 1 036 449–8 818 172 | 716 828–6 803 080 | 30 624–112 552 | 76 964–702 916 |

Cada portal ocupa 44 B de payload; no se duplican puntos de trayectoria. El cliente de la prueba reserva además 4 × NodeCount B para la lista de aristas de salida. Estos recuentos excluyen cabeceras, referencias, listas retenidas por D1 y geometría compartida; no son memoria residente total. Pico gestionado muestreado durante construcción (Editor, incluye otras fixtures): 61 243 392–169 406 464 B (16), 62 799 872–156 639 232 B (32), 61 636 608–156 168 192 B (64). El contador de asignaciones **no detectó los 4096 B testigo en ninguno de los 108 casos**, por lo que asignaciones = **N/D**, también para consultas calientes; no se certifica cero GC. Se conservan los ceros de la primera pasada como dato invalidado, no como cumplimiento.

Recomendación D3: conservar 16/32/64 como candidatos. La consulta exacta fría D2 recorre la unión física y no debe entrar sin fraccionar en un tick. Medir campos locales compartidos, caché acotada e invalidación, con trabajo contado y reparto justo; no precomputar todas las parejas ni elegir 64 solo por construcción. Portales y SCC proporcionan conectividad, pero la condensación de alcance no aporta costes exactos de transferencia. Los resultados de presión y latencia D3 siguen pendientes; inanición M/L continúa abierta, presupuesto 3000 intacto.

Cierre D2: `scripts\u3.cmd edit` **801/801**, 713,630 s; `scripts\u3.cmd play` **53/53**, XML/log nuevos archivados en `unity/TestResults/P0/D2-20261015`. Referencias U0/U2/U3/U4 coincidentes con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`, instantánea `qa-results/reference-88830f7ef44246fd8b3e5b4325802206`. Huellas finales 9/9 iguales a entrada, JSON antes/después en la misma carpeta. Sin diferencias en src, corpus, generador U3, circuitos A/B/C, D1 ni EnemyNavigation respecto a `c01070b`. Código D2 `7c50d9f`; cierre documental separado. D3 comienza por transferencias exactas bajo demanda y su coste con caché fría/caliente; D4 después, D5 excluido.

## D3 — primera pieza: transferencias y planificación (16/10/2026)

`RegionalNavScheduler` registra trabajos durante la carga y reparte un techo fijo de 3000 unidades por tick. Cada clase con trabajo obtiene hasta 1000 antes de distribuir sobrantes; el orden de clases rota de forma determinista. Dentro de una clase, una solicitud con al menos 30 ticks de antigüedad precede a las nuevas; entre antiguas gana la más vieja. Antes de ese umbral se atiende prioridad y después antigüedad. Los empates usan el orden estable de registro. La población no interviene en las cuotas. Una solicitud equivalente no reinicia el cálculo ni renueva su antigüedad.

`RegionalTransferCache` está ligada a una instancia inmutable de `NavPortalWorld`, clase y versiones geométrica/de conectividad. La clave añade nodo físico de origen y ámbito: región concreta para costes internos o mundo completo (-1) como control fraccionado. Conserva todas las direcciones y costes físicos D2; la SCC no se usa para inventar costes. Una transferencia interna imposible puede tener ruta mundial saliendo y reentrando; se prueban ambos resultados por separado. No se crean todas las parejas de portales.

La caché reserva durante carga entre 1 y 16 entradas (4 por defecto), cada una con distancias, padres y heap indexado. Agrupa solicitudes por fuente/ámbito; solo expulsa entradas terminadas, por último acceso y orden estable. Si todas están calculándose, rechaza explícitamente la admisión para que el consumidor reintente. No cancela trabajos pendientes. No expone distancias ni padres parciales. Esta primera pieza no contiene aún la política del objetivo móvil, la retención de campos anteriores ni la precarga D4.

Contabilidad más estricta que el monolítico: cada registro inicializado, siembra, extracción del heap, arista examinada y publicación consume una unidad. La operación de heap indexado incluye sus comparaciones O(log V), acotada a V entradas; CPU completa del planificador/caché debe medirse aparte. Los buffers se reservan en carga, no durante solicitudes o ticks. No se afirma que una unidad nueva cueste lo mismo que un nodo extraído del monolítico. Se mantiene el límite numérico 3000 y se informa el desglose, sin esconder inicialización ni barridos de aristas. La primera pasada inicializaba arrays mundiales incluso para una transferencia interna. Tras medirla se limita la inicialización a los propietarios de la región y se reservan buffers para la mayor región, sin halos; el control mundial requiere habilitación explícita y buffers mundiales. La versión compacta pasa las 38 pruebas específicas.

Verificación específica con `RegionalTransferTests`: A/B y ocho semillas C × 16/32/64 × S/M/L, oráculo independiente de relajación por cola, alcance/costes/padres de todos los destinos desde una fuente física por caso, salida/reentrada, caché fría/caliente, contrapresión y reparto sintético 180 s. No equivale a todas las parejas ni a persecución física 300/500/750. El backend activo permanece monolítico; campos locales, invalidación por objetivo, pruebas de presión y D4 pendientes.

### D3, transferencias verificadas por separado (16/10/2026)

Código publicado `957524650029c18ab18e9f955278ab1e0e69222e`. Comando nuevo: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalTransferTests`. Primera pasada 37/37 en `Regional-20261016T165709`; versión compacta **38/38** en `Regional-20261016T170216`. El intento `Regional-20261016T165327` no ejecutó pruebas: licencia 198 en sandbox. Informes reproducibles mediante `python scripts/p0-d3-transfer-report.py <edit.xml> <salida.json>`, guardados en `unity/TestResults/P0/D3-20261016/transfers-first.json` y `transfers-compact.json`.

Cada fila resume 30 casos A/B/ocho C × S/M/L, una fuente física por caso. Se contrastan todos sus destinos y padres con un oráculo independiente de relajación por cola sobre la unión D2, cuya equivalencia física monolítica ya verifica D2. El control mundial aquí es el mismo problema de transferencias con ámbito completo, **no** un benchmark nuevo del backend monolítico con horda. Todos estos rangos son medidas de Editor sin render; una clase activa dispone de las 3000 unidades. CPU de una transferencia suma sus ticks y excluye construcción de grafos y aserciones.

| Baldosa | Trabajo transferencia interna (unidades) | Ticks fríos internos | CPU interna total (ms) | Arrays caché interna, dos entradas (B) | Consulta caliente interna, media 100 llamadas (µs) |
| --- | --- | --- | --- | --- | --- |
| 16 m | 958–10 143 | 1–4 | 0,009–0,106 | 25 896–33 028 | 0,012–0,055 |
| 32 m | 3849–29 722 | 2–10 | 0,025–0,390 | 57 288–118 500 | 0,011–0,020 |
| 64 m | 2866–112 932 | 1–38 | 0,017–1,384 | 134 484–371 188 | 0,012–0,016 |

La consulta caliente combina solicitud equivalente y lectura de distancia; no recalcula ni consume trabajo de grafo. Es una medición corta de microbenchmark, no p95 ni coste de cientos de consumidores. Payload incluye cuatro arrays int por entrada y el índice de regiones; excluye cabeceras/objetos, referencias, grafos D1/D2, geometría y memoria de Editor. No es residencia completa de navegación. La prueba de asignación testigo 4096 B falla en los 180 registros: **asignaciones N/D**, nunca cero certificado.

CPU del tick de transferencias, máximo observado: 0,0379 / 0,0497 / 0,0815 ms para 16/32/64. Máximo del p95 de cada consulta: 0,0379 / 0,0497 / 0,0797 ms. Las consultas cortas tienen pocos ticks, por lo que esto no sustituye percentiles de una sesión sostenida ni mide mover enemigos. La lectura incluye el planificador; no incluye movimiento, combate ni render.

Antes/después de compactar: máximos fríos internos **18→4 / 24→10 / 49→38 ticks** para 16/32/64. Máximo de arrays internos por caché de dos entradas **1 405 792→33 028 / 118 500 / 371 188 B**. Se conserva el primer informe, sin presentar una diferencia entre dos pasadas como ahorro CPU controlado. La mejora determinista es menos registros inicializados y menos capacidad reservada.

El control mundial conserva **44 799–425 188 unidades, 15–142 ticks (0,25–2,367 s a 60 Hz)**, CPU total 0,556–7,257 ms y máximo de tick 0,1289 ms. La inicialización y la exploración mundial fraccionadas siguen demasiado lentas para el objetivo de reacción: no adoptar este control como campo del jugador móvil. También la transferencia interna fría de 64 m llega a 0,633 s con una sola clase: supera por sí sola 0,5 s. No se elige aún baldosa definitiva; 16 m ofrece el menor coste local, pero falta medir cantidad de transferencias, portales y campos necesarios, caché fría/caliente real y horda dispersa.

Reparto comprobado: ensayo **sintético**, 10 800 ticks de demanda continua en las tres clases: cada clase recibe exactamente 1000 por tick, 10 800 000 unidades en 180 s simulados; total 3000 sin exceso. También pasan prioridad con envejecimiento, donación de cuotas ociosas, 750 solicitudes equivalentes sin reiniciar, contrapresión al llenar pendientes, expulsión LRU de terminados, publicación atómica y reutilización entre regiones sin distancias residuales. Esto acredita servicio del planificador, **no** publicaciones de campos útiles ni llegada física S/M/L. Los 372/3/0 de D0 siguen siendo la referencia pendiente de superar en movimiento.

**Estado: primera pieza D3 verificada; D3 no terminado y D4 no iniciado.** Falta transferencias inversas reutilizables hacia salidas, cálculo compartido de sus costes globales, campos locales multiobjetivo con dependencias e invalidación, integración optativa y matriz de persecución 0/300/500/750, jefe, metas móviles/alturas, presión 180 s y comparación monolítica del mismo escenario. No se han medido edad de campo ni llegada con este código. Siguiente implementación: invertir las consultas de transferencia respetando las aristas dirigidas (sin duplicar trayectorias), contrastarlas con el oráculo y construir el campo de salidas y los campos locales bajo el mismo planificador. Reutilizar costes geométricos por instancia/versiones; no sustituirlo por recalcular todo el mundo ni subir 3000. Si la jerarquía fría no alcanza los objetivos, presentar sus medidas antes de decidir otro algoritmo o relajar criterios.

### Regresiones y cierre de esta pieza D3 (16/10/2026)

- `scripts\u3.cmd edit`: **839/839**, 838,686 s, ejecución nueva sobre `9575246`.
- `scripts\u3.cmd play`: **53/53**, 112,009 s, ejecución nueva. Primer intento restringido: error de licencia 198, sin pruebas; log conservado como `play-first-attempt.log`.
- XML/log nuevos y huellas de entrada/salida en `unity/TestResults/P0/D3-20261016`: `edit.xml`, `edit.log`, `play.xml`, `play.log`, `integrity-before.json`, `integrity-after.json`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1`: U0/U2/U3/U4 coincidentes, instantánea `qa-results/reference-9f070972b60d4a66843fdf9ea1e29f6c`; ocho guardados y seis secuencias meta. Edit también preparó/verificó las ocho transferencias web.
- SHA-256 **9/9** iguales: U6, u3-world y siete ajustes locales. Sin diferencias respecto a `d4906e6` en src, corpus, D1/D2, EnemyNavigation ni VerticalCircuit. Sin build nueva ni cambios al backend activo; no se afirma prueba regional de combate/movimiento por pasar las regresiones históricas.

Código `9575246` publicado mediante push normal y comprobado en origin. Cierre documental separado con los cuatro documentos de seguimiento; únicamente los siete ajustes protegidos deben quedar fuera del índice. No se han completado D3 ni D4, ni se ha iniciado D5. La continuación autorizada figura en el párrafo anterior y en Cómo retomar de PROGRESO_P0; no necesita nueva autorización general.

## D3 — transferencias inversas y bloqueo de la jerarquía fría (17/10/2026)

La reanudación comprobó HEAD/origin `9575246`, fetch, los cuatro documentos preparados y los XML del 16/10. El cierre documental heredado se publicó en `3c1fb90`. Huellas de entrada 9/9 coincidentes en `unity/TestResults/P0/D3-20261017/integrity-before.json`. Ningún código local posterior quedó descartado.

`RegionalTransferCache(reverse:true)` calcula costes hacia una salida/meta conservando la dirección física: sus padres son aristas hacia delante que acercan a esa meta. El índice de entradas se construye durante carga y referencia las aristas D2; no copia ni invierte las trayectorias. La caché queda ligada al sentido de consulta, mundo/clase y versiones geométrica/de conectividad; los buffers regionales, cuotas y contrapresión existentes se conservan. El payload informado incluye ahora el índice inverso. No cambia el backend activo.

Código de transferencias `2ca9c29`: comando `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalTransferTests`, **71/71 nuevas**, 280,584 s; `unity/TestResults/P0/Regional-20261017T104657/edit.xml` y `edit.log`. Incluye las 38 existentes, 30 combinaciones inversas A/B/ocho C × 16/32/64 con S/M/L y tres pruebas de caída dirigida sin retorno. Oráculo por cola independiente, costes y siguiente arista de todos los destinos desde una meta por caso; no todas las parejas.

### Prototipo de costes de salida

`RegionalPortalField`, código `9e11d37`, aplica la jerarquía perezosa exacta sin matriz de parejas: vértices únicos de salida, Dijkstra inverso implícito, una transferencia interna hacia el vértice extraído y relajación de portales que entran en esa región. El coste de salida exige que la primera arista cruce región; **no es la distancia libre desde ese nodo**. Los futuros campos locales deben combinar esas semillas y, en la región objetivo, la meta real. No se conectó este prototipo al movimiento.

El trabajo de inicialización, siembra, extracción/solicitud, examen de portales y publicación se cuenta junto con las transferencias en el mismo `RegionalNavScheduler`. Mientras espera una transferencia no compite contra ella. Caché de cuatro entradas por clase, sin cancelación por meta nueva; rechazo explícito mientras está calculando, doble buffer y conservación de la versión publicada. El consumidor deberá reintentar la meta más reciente. No existe aún coordinador de dependencias con campos locales, ni se afirma invalidación completa del backend final.

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.RegionalPortalFieldTests`: **6/6 nuevas**, 13,728 s reales para la suite, XML/log `Regional-20261017T114240`. Tres fixtures pequeñas contrastan costes de salida contra oráculo mundial independiente, publicación atómica, contrapresión y conservación del objetivo anterior para S/M/L. Tres diagnósticos miden HORDA750 frío a 16/32/64. El intento previo `Regional-20261017T105239` terminó por licencia 198 sin ejecutar pruebas; se repitió fuera del sandbox. Que la suite pase acredita sus invariantes y la captura de medidas, **no el cumplimiento del objetivo de latencia**.

### Resultado que bloquea el avance

Una meta fija por clase, HORDA750, tres clases activas, 10 800 ticks a 60 Hz (180 s **simulados**), una pasada por tamaño, Editor sin render. Cada clase recibió exactamente **10 800 000 unidades**; total **32 400 000 por tamaño**, nunca más de 3000/tick. Se comprobó igualdad entre trabajo del planificador y todos los contadores. No hay enemigos ni movimiento en esta prueba: no es una sesión jugable de tres minutos ni mide FPS.

| Región | Salidas únicas S/M/L | Transferencias terminadas S/M/L | Publicaciones S/M/L a 180 s | CPU tick p50 / p95 / p99 / máximo (ms) |
| --- | --- | --- | --- | --- |
| 16 m | 5396 / 4605 / 2715 | 1356 / 1622 / 2676 | 0 / 0 / 0 | 0,0353 / 0,0547 / 0,0707 / 1,3418 |
| 32 m | 2477 / 2157 / 1310 | 356 / 445 / 866 | 0 / 0 / 0 | 0,0376 / 0,0474 / 0,0691 / 0,1020 |
| 64 m | 839 / 757 / 506 | 102 / 119 / 356 | 0 / 0 / 0 | 0,0484 / 0,0656 / 0,0752 / 0,1059 |

**Ninguna primera publicación en los nueve casos: latencia censurada a 180 s, no cero.** No se pueden calcular p50/p95/p99/máximo de latencia de publicaciones ni edad de campos inexistentes. Supera ampliamente el objetivo de meta fija (p95 ≤0,3 s / máximo ≤0,5 s). Hay progreso de cálculo en las tres clases, pero aún ningún resultado utilizable. No es inanición del planificador; este candidato requiere demasiadas transferencias por salida para la primera versión mundial.

Caché fría: cero hits, expulsiones S/M/L 1353/1619/2673 (16), 353/442/863 (32), 99/116/353 (64). Todas son entradas terminadas; ninguna cancelación perpetua. Arrays del campo y su caché S/M/L: 1 903 408 / 1 638 480 / 926 864 B (16), 1 956 652 / 1 690 312 / 979 008 B (32), 2 417 956 / 2 098 816 / 1 220 364 B (64). Excluyen grafos D1/D2, geometría, cabeceras y transitorios: **no son memoria residente total**. Asignaciones **N/D** en los tres tamaños: el contador no detectó el testigo de 4096 B. No certificar cero GC.

Informe reproducible: `python scripts/p0-d3-hierarchy-report.py unity\TestResults\P0\Regional-20261017T114240\edit.xml unity\TestResults\P0\D3-20261017\hierarchy.json`. Conserva todas las filas y representa la publicación ausente como null. Manifiesto en `D3-20261017/manifest.json`: meta por clase `NodeAt(0,0,0)`, o `NodeCount/2` si no existe, cuatro entradas de caché, código y rutas de evidencias. CPU corresponde únicamente al planificador/jerarquía/transferencias; faltan movimiento, combate, preparación y render, y no hay tres repeticiones Normal/Development.

D0 sigue siendo la referencia **no comparable directamente**: 372/3/0 campos en persecución móvil con 750 y M p95 57,6 s. Aquí no hay horda ni campos de movimiento. No se afirma mejorar D0 ni el monolítico. Los ensayos físicos 300/500/750, distribución concentrada/dispersa, jefe, metas móviles/alturas/inaccesibles y llegada real siguen pendientes. Tampoco se implementaron los campos locales compartidos: integrar este resultado como solución ocultaría el fallo previo de latencia.

**Parada por incumplimiento importante**, según PLAN_P0D y la instrucción del autor. D3 abierto; D4 no iniciado, D5–D7 excluidos. Se solicita evaluar un comparador de reparación incremental exacta del campo global (alternativa futura en sección 4), conservando 3000, física y prototipo; otra opción requiere revisar la compresión de portales y sus garantías. **Propuesta pendiente, no decisión adoptada ni autorización general nueva de D3/D4.** Este diagnóstico descarta el candidato perezoso por salida tal como está, no demuestra imposibilidad de toda arquitectura regional.
