# H1c — preparación y reutilización de navegación

## Contrato previo a medir — 22/10/2026

Base real `b5b343d`, HEAD/origin coincidentes tras fetch. Autoriza exclusivamente diagnóstico técnico, instrumentación, pruebas y scripts. H1b se conserva íntegro como referencia de calidad; sin investigación avanzada, cambios de producción/generador/EnemyNavigation/U3/web/economía/guardados, servicio nuevo, H2–H4 ni D4/D5. 3000 globales/tick, siete ajustes, nueve huellas y once referencias intactos. Parada al finalizar H1c incluso si hay resultados positivos.

**Preparación:** P0QA, MAMPORRO, HORDA750, Z9K2 y RAMPA42 ×16/32/64×tres arranques. Usar los cronómetros existentes de ProceduralMap (generación, comprobación previa, grafos, validación, total) y NavPortalWorld/NavGraph (D1 físico, D2 topología, nodos/escalada/caída). Medir envoltura de circuito e índice H1b. No omitir reparaciones, intentos rechazados ni validaciones. Registrar suma de componentes, residual y total real. Tres muestras describen variación; no certifican p95 de producción. Mantener objetivos 4 s p95 / 6 s máximo.

Diagnóstico de duplicación: nodos de halo, intentos regionales, trayectorias y correspondencia completa de aristas con los grafos originales ya validados. Construir solo en tests un índice de región/SCC/arcos sobre el grafo original, sin reconstruir geometría ni eliminar validaciones; contrastar su partición con D2. No crear adaptador de producción ni sustituir NavPortalWorld. Tiempo de índice/certificación medido aparte. Cualquier total que sustituya D1/D2 por esta proyección es **estimación de una preparación futura incompleta**, no ahorro de carga ejecutado; informar también todo el trabajo real del diagnóstico, incluido el control D1/D2.

**Reutilización:** mismos cinco mapas y tamaños, S/M/L. Anclas distribuidas por identidad física, ancla elevada y peor par Z9K2; metas vecinas, a ~8/~24 m y lejanas dentro de la misma SCC. Controles de cambio de SCC obligan a invalidar reutilización. Comparar el potencial macro completo H1b antes/después, sin modificar H1b. El potencial depende solo de la SCC objetivo y la revisión geométrica; la máscara local de 32 m depende además del nodo real.

Sonda offline de refinamiento compartido: dominio = SCC objetivo completa más nodos a ≤32 m XYZ de cualquiera de las dos metas. Dijkstra inverso solo con aristas originales internas al dominio. Conectar el prefijo del campo H1b anterior con el primer nodo resuelto del campo local nuevo; no seguir hasta el ancla si antes hay una unión válida. Prefijo y sufijo usan aristas físicas, sin línea recta nueva. Dentro de una misma SCC, la meta anterior debe llegar a la nueva por aristas internas; comprobarlo. Si cambia SCC, no usar la unión como fallback universal. No publicar campos/versiones ni ejecutar scheduler/cuerpos: es un cálculo diagnóstico por pareja, no servicio H2.

Cobrar creación del índice inverso, barrido/inicialización mundial del diagnóstico local, aristas examinadas, extracciones, relajaciones, heap y reconstrucción por separado. Incluir el campo frío anterior, no llamarlo gratuito. Contrastar alcance y rutas contra oráculo monolítico, calidad p95 ≤1,5× / máximo ≤2× por caso/clase, conservar fallos, rutas cortas y metas imposibles. Comparación con H1b actualizado contemporáneo. El ahorro del solver local es medido; ahorro por evitar máscaras/listas/índices mediante caché no implementada es estimado. No convertir operaciones offline en unidades scheduler, latencias o FPS.

## Resultados

### Resultado y procedencia — 23/10/2026, Codex

**El potencial macro ponderado se puede reutilizar dentro de la misma SCC; eso no permite conservar sin más el campo físico hacia la meta antigua. Preparación sin optimizar: objetivo de 4 s incumplido. La unión local ensayada falla calidad a 64 m. H1c termina para revisión, sin H2.**

Se retoma HEAD/origin `318b27b`, posterior al último commit conocido `b5b343d`, con el contrato publicado y fuentes/documentos H1c locales. Se conservan y revisan esos cambios. Las fuentes iniciales coinciden 3/3 con `sources-initial.json`; protecciones 9/9 y referencias 11/11 coinciden con los manifiestos del día 22. H1b y producción no se modifican. Sin investigación avanzada.

Evidencias locales en `unity/TestResults/P0/H1c-20261022`. **Preparación heredada**, no ejecutada de nuevo el día 23: `Regional-20261022T220312/edit.xml`, 15/15 tests, 245,028 s, tres arranques por semilla/tamaño, 45 en total. Reextracción del XML con el script inicial reproduce exactamente `preparation.json` en `preparation-recheck-20261023.json`. El extractor final añade clasificación y produce `preparation-reviewed-20261023.json`. La ampliación posterior de contadores solo afecta reutilización, no el método de preparación ni la proyección.

### Preparación completa y desglose

Cada arranque crea el mapa procedural con sus validaciones, rechazos y reparaciones, envuelve el circuito, construye D1/D2 S/M/L y el índice H1b. `actualMs` mide ese trabajo real de principio a fin; `accountedMs` suma componentes y conserva el residual. Después se ejecutan proyección, certificación física/topológica, recuentos y registro: **`diagnosticWallMs` incluye también todo ese diagnóstico**, sin descontar el control D1/D2.

| Semilla | 16 m: p95 empírico / máximo | 32 m | 64 m |
| --- | --- | --- | --- |
| P0QA | 4,949 s | 4,100 s | 3,729 s |
| MAMPORRO | 5,851 s | 5,119 s | 4,815 s |
| HORDA750 | 4,914 s | 4,127 s | 3,736 s |
| Z9K2 | 5,756 s | 4,982 s | 4,582 s |
| RAMPA42 | 5,795 s | 5,050 s | 4,655 s |

Con tres muestras, el percentil de rango más próximo coincide con el máximo; **no certifica p95 de producción**. 39/45 arranques exceden 4 s, ninguno excede 6 s en `actualMs`. El diagnóstico completo tarda **4,305–6,552 s y supera 6 s en 9/45 muestras**. No presentar la ejecución completa como inferior a 6 s: la certificación adicional cuesta 596–697 ms y está registrada aparte.

Medias por caso/tamaño, S/M/L sumadas, en milisegundos:

| Semilla/tamaño | Generación | Grafos originales acumulados | Validación | D1 físico | D2 | Índice H1b |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| P0QA 16/32/64 | 19,7 / 9,2 / 10,8 | 953,6 / 949,6 / 951,6 | 16,2 / 15,3 / 15,6 | 3723,1 / 2968,3 / 2608,8 | 113,1 / 106,9 / 95,5 | 16,8 / 23,6 / 8,2 |
| MAMPORRO 16/32/64 | 18,8 / 18,6 / 26,1 | 2015,0 / 2018,4 / 2027,6 | 32,8 / 31,9 / 33,9 | 3605,0 / 2882,6 / 2578,8 | 113,5 / 101,2 / 96,3 | 15,5 / 15,9 / 9,5 |
| HORDA750 16/32/64 | 8,4 / 8,5 / 9,2 | 998,0 / 1002,5 / 1000,8 | 15,8 / 16,1 / 17,3 | 3716,7 / 2936,4 / 2570,9 | 121,4 / 111,3 / 97,9 | 16,4 / 23,4 / 19,1 |
| Z9K2 16/32/64 | 7,0 / 7,3 / 7,8 | 1929,5 / 1950,8 / 1931,9 | 30,9 / 30,8 / 30,9 | 3586,9 / 2841,0 / 2480,7 | 108,2 / 118,4 / 102,2 | 20,3 / 15,7 / 11,4 |
| RAMPA42 16/32/64 | 10,2 / 9,4 / 16,0 | 1909,9 / 1925,0 / 1932,1 | 34,5 / 34,5 / 34,8 | 3672,1 / 2883,4 / 2497,4 | 116,8 / 134,2 / 103,1 | 14,0 / 16,3 / 31,2 |

La tabla no reemplaza el total: los JSON incluyen comprobación previa (medias 6,5–23,0 ms), residual del mapa, envoltura del circuito y residual entre cronómetros. `originalMapsMs` incluye todas las construcciones y la preparación de consultas; los tiempos `originalFinalGraphMs`/nodos/escalada/caída describen solo los grafos finales y **no se usan para borrar los intentos anteriores**.

### Duplicación y oportunidades seguras

- P0QA y HORDA750 construyen una tanda original; MAMPORRO construye dos por un rechazo; Z9K2 y RAMPA42 construyen dos por una reparación. Estas segundas construcciones responden a geometrías distintas: no se pueden suprimir ni reutilizar indiscriminadamente. No se cambia el generador ni su validación.
- `VerticalCircuit(map)` reutiliza terreno/consultas y construye colisión; no reconstruye los grafos originales. El coste duplicado importante aparece después: `NavPortalWorld` crea nuevos `NavRegionGraph` y estos vuelven a ejecutar `NavGraph` sobre geometría ya validada, con halos solapados. Los universos finales de nodos/aristas coinciden con los originales, incluidos coste, tipo y trayectoria completa en el corpus.
- Nodos regionales almacenados con halo / originales: **4,820× / 2,265× / 1,427×** a 16/32/64. En las 135 filas de clase, intentos = regiones: 2880/720/180 totales por tamaño, sin ampliaciones de halo. No hay reintentos regionales que eliminar en estas muestras.
- Tiempo de nodos regionales agregado sobre las 15 preparaciones de cada tamaño: 23,445 / 12,056 / 6,401 s; escalada 23,070 / 23,224 / 23,267 s; caída 6,505 / 6,537 / 6,537 s. Son subcomponentes de D1, no costes adicionales a sumar otra vez. Aumentar región reduce especialmente contexto/nodos, pero no evita reconstruir escaladas y caídas.
- La proyección de tests sobre el grafo original construye propiedad regional, SCC y arcos sin geometría nueva: **12,4–24,5 ms de media por caso/tamaño**, con partición/arcos iguales a D2. Es una oportunidad concreta para evitar D1 duplicado en una implementación futura. No implementa `NavPortalWorld`, portales físicos, índices completos ni un adaptador H1b; usa reflexión sobre el kernel SCC existente. No acredita una preparación alternativa lista para jugar.
- Conservar los grafos finales ya validados y derivar sus índices inmutables una vez por clase/tamaño/revisión parece la vía de mayor impacto. Reutilizar índices inversos también evitaría construcciones repetidas del arnés por solver. Ambas son **oportunidades, no ahorro ejecutado**: preparación optimizada medida = ninguna; ahorro medido en arranque = 0 ms. No se descuenta D1/D2 del tiempo real ni se certifican 4 s con una suma parcial hipotética.

### Persistencia del corredor macro

En H1b, los pesos se fijan en el constructor a partir de centroides, extremos de portales y costes físicos. `Build(goal,3)` calcula el potencial inverso desde `Component[goal]`. Si clase, grafo/costes, tamaño y SCC objetivo no cambian, el problema macro es idéntico: se conservan potencial, árbol determinista y conjunto de arcos descendentes. La clave conceptual de reutilización debe incluir **identidad/revisión del grafo, clase, tamaño y SCC objetivo**; el ID de SCC por sí solo no identifica datos de otro mundo.

La SCC aquí es la **componente dirigida interna a una región de D2**, no toda la región ni la SCC global. Cambiar de SCC local exige invalidar/recalcular esta reutilización, incluso si ambas pertenecen a una SCC global. Cambiar geometría, costes, clase o partición también la invalida. No hay servicio ni caché implementados.

La posición real sí cambia la máscara XYZ de 32 m y el campo físico `Distance/Parent`. Mantener el potencial no garantiza que los padres antiguos terminen en la nueva meta, ni que unirlos después conserve calidad. El diagnóstico compara ambos potenciales completos y, en la repetición final, también `Next`; contrasta alcance con oráculo y rechaza cambios de SCC.

### Refinamiento compartido offline y calidad

Dominio prefijado: SCC objetivo completa más radios XYZ de 32 m alrededor de ambas metas. Búsqueda inversa con aristas originales; cada ruta sigue el prefijo anterior hasta el primer nodo resuelto por el campo nuevo y continúa por su sufijo físico. Sin segmentos inventados ni modificación de radio/pesos tras medir. Anclas distribuidas por identidad, elevada y Z9K2; metas vecinas, próximas a 8/24 m y lejanas dentro de SCC. Las distancias reales y cambios de máscara figuran en cada `MOVE`; las metas se deduplican, por eso no hay cuatro consultas en todas las anclas.

Primer ensayo nuevo: **15/15**, 95,741 s, `Regional-20261023T175043/edit.xml`, `reuse-20261023.json`. Repetición final con desglose de contadores, comparación `Next` y control Z9K2 62→10514 = 50: **15/15**, 96,121 s, `Regional-20261023T175455/edit.xml`, `reuse-final-20261023.json`. Contadores y calidad comunes idénticos entre ambas pasadas (`determinism-20261023.json`); no son muestras independientes. **817 movimientos**, 226 controles de cambio de SCC, **27870972** contrastes de alcance, **25797** rutas reconstruidas por pasada. Cero discrepancias de alcance o continuidad. El comparador H1b actualizado conserva calidad en todas las muestras: peor p95 por consulta 1,381742×, máximo 1,980255×.

| Región | Movimientos | Peor p95 por caso/clase | Peor p95 por consulta | Máximo | Casos / consultas que fallan |
| --- | ---: | ---: | ---: | ---: | --- |
| 16 m | 222 | 1,095460× | 1,405075× | 1,625815× | 0/15; 0/222 |
| 32 m | 294 | 1,193005× | 1,496331× | 1,674620× | 0/15; 0/294 |
| 64 m | 301 | 1,352194× | 2,585450× | **3,204259×** | **13/15; 45/301** |

Peor caso: HORDA750 64 m L, meta antigua 18366 → nueva 16555, origen 7355, coste **13240 frente a 4132** óptimo; H1b actualizado obtiene el óptimo. La validez del prefijo antiguo no garantiza calidad hacia otra meta de la misma SCC. No descartar estas consultas ni aceptar 64 m por su menor preparación. 16/32 m son resultados favorables en estas muestras, no aceptación del sistema completo ni prueba dinámica.

### Trabajo: ahorro medido y límites

| Región | Trabajo H1b actualizado | Trabajo local | Reducción búsqueda | Reducción incluyendo campo frío e índice local | Fracción macro reutilizable |
| --- | ---: | ---: | ---: | ---: | ---: |
| 16 m | 128213883 | 29648494 | 76,88 % | 52,48 % | 0,1493 % |
| 32 m | 170524346 | 43796480 | 74,32 % | 55,23 % | 0,0366 % |
| 64 m | 175557381 | 60769104 | 65,39 % | 48,31 % | 0,0079 % |

Estos son contadores de búsqueda comparables, **no instrucciones CPU ni unidades del scheduler**. Se cobran inicialización mundial y examen del dominio local: no hay listas espaciales ni inicialización perezosa gratuitas. Se añaden **130107401** unidades de campos fríos y **26188287** de construcción de índices locales al escenario que los necesita. Heap local **141344501**, reconstrucción **2686163** pasos, informados aparte; no están incluidos en los porcentajes de búsqueda. No confundir la fracción macro evitable con ahorro de todo el solver: es menor del 0,15 % y por sí sola no resuelve el coste por meta.

Desglose final local: **27870972** inicializaciones de nodo, **6593881** extracciones, **50092964** aristas examinadas y **49656261** intentos de relajación (de ellos **6731824** mejoras). Su suma, sin sumar las mejoras por segunda vez, reproduce **134214078** unidades locales. Heap de H1b actualizado **657943529** y de campos fríos **179666245**, separados. CPU máxima observada por consulta final: H1b actualizado **12,8612 ms**, local **5,1806 ms**; excluye oráculo/aserciones y no es latencia de servicio. Ambos tiempos incluyen inicialización de su búsqueda, y el índice local se mide aparte.

No se implementan caché, versiones, publicación, cuotas, cuerpos ni mantenimiento por tick. Las metas son parejas estáticas contra un campo frío por ancla; no una persecución móvil continua. Los índices H1b comunes pertenecen a la preparación; el índice local adicional se cobra por separado. Los constructores y asignaciones son costes CPU/memoria observados, no operaciones elementales completas del scheduler. Memoria residente, picos y GC no certificados; arrays/managed/Unity del arnés en JSON. No atribuir FPS ni latencias de reacción a estos resultados.

### Reproducción y cierre

Desde CMD, en la raíz del repositorio:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.NavigationReuseTests -Category ReusePreparation
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1 -Filter Mamporro.Tests.NavigationReuseTests -Category ReuseCorridors
python scripts\p0-h1c-report.py unity\TestResults\P0\Regional-20261022T220312\edit.xml unity\TestResults\P0\H1c-20261022\preparation-reviewed-20261023.json
python scripts\p0-h1c-report.py unity\TestResults\P0\Regional-20261023T175455\edit.xml unity\TestResults\P0\H1c-20261022\reuse-final-20261023.json
```

El primer comando describe la medición heredada del día 22; no se ejecutó otra preparación el día 23. Intento nuevo `Regional-20261023T154307` sin tests por licencia 198 dentro del sandbox; repetición fuera del sandbox. El extractor exige suites correctas y corpus completo, pero **NUnit Passed no significa aceptación de rendimiento ni calidad**.

Integridad final: **9/9 protecciones, 11/11 referencias iguales** antes/después; manifiestos `integrity-after-20261023.json`, `references-after-20261023.json` y `sources-final-20261023.json`. Diff vacío frente a `b5b343d` en Core/U3/src/referencias y fuentes H1/H1b; **189 enlaces locales comprobados**. Sin Edit completo, Play, build ni verificador histórico nuevos. Los siete ajustes Unity permanecen intactos y fuera del commit. Evidencias ignoradas por Git según la política del repositorio; fuentes, informe y comandos sí versionados.

**Siguiente paso exacto:** revisión del autor de la reutilización macro demostrada, la duplicación de preparación y el fallo de la unión local a 64 m. No optimizar otro candidato, integrar, eliminar validaciones ni iniciar H2–H4/D4/D5 automáticamente. 3000 globales/tick, geometría, H1b, producción y siete ajustes Unity conservados. Checkpoint, repetición final de contadores, integridad y publicación en PROGRESO_P0.
