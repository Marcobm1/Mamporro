# P0-D — construcción y navegación regional

Implementación optativa autorizada D1–D4; D5 excluido. Mapas de prueba 80/128 m; sin integración en partidas normales ni cambios de generador, balance o presupuesto de 3000 nodos/tick.

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
