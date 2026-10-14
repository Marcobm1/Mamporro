# P0-D0 — diagnóstico reproducible de HORDA750

Fecha: 12/10/2026. Sesión Codex. Base de runtime: `1503780404bc3c73920f058bcd8a0909f68a820c`. Instrumentación exclusivamente en Tests; sin cambios de combate, navegación, tamaños, velocidades, RNG, IDs ni presupuesto (3000). D5–D7 e integración U3 fuera de alcance.

**Procedencia al retomar el 14/10/2026:** este informe, sus arneses y los resultados del 12/10 ya estaban en el árbol local sin publicar. Se conservan como evidencia heredada; no son ejecuciones realizadas en la sesión del 14/10. La revalidación nueva se registra por separado al final y en PROGRESO_P0.

**Corrección autorizada y verificada el 14/10/2026:** el autor aprueba expresamente la corrección propuesta de `TryStep`. Aplicada solo la conversión X/Z a float antes de validar; Edit 729/729 y Play 53/53 nuevos. Las menciones inferiores a «pendiente de aprobación» conservan el estado histórico del diagnóstico previo, no revocan esa autorización. La inanición de M/L sigue prioritaria para D3 con 3000 nodos/tick.

## Procedencia y reproducción

### Corrección verificada: comparación del 14/10/2026

Cambio de runtime: únicamente `x=(float)x; z=(float)z;` al comienzo de `TryStep`. Una celda sin conexión sigue rechazándose y se mantiene el intento de deslizamiento por ejes. Sin snap, aristas nuevas ni cambios de combate/desvío. La reproducción mínima ahora exige rechazo sin alterar posición y deslizamiento por el eje libre; ocho casos adicionales verifican fronteras X/Z positivas/negativas y las cuatro esquinas, usando solo conexiones Walk existentes.

| Control | Llegada real antes (s) | Después (s) | Recorrido antes → después (m) |
| --- | --- | --- | --- |
| Cuerpo L fijo | 27,117 | 27,117 | 92,732 → 92,732 |
| Jefe fijo | 70,867 | 69,550 | 131,441 → 156,467 |
| Jefe sin esbirros fijo | No en 180 | 67,350 | 56,374 sin llegada → 156,727 |
| Jefe ID 750 fijo | 69,383 | 67,350 | 168,881 → 172,240 |
| Jefe con 750 iniciales fijo | 71,983 | 71,983 | 178,098 → 177,088 |
| Jefe móvil | No en 180 | 70,067 | Ensayo completo de 180 s |
| Jefe sin esbirros móvil | No en 180 | 70,250 | Ensayo completo de 180 s |
| Jefe con 750 iniciales móvil | No en 180 | No en 180 | Ensayo completo de 180 s |

Tres repeticiones por control más control sin observador, deterministas en las métricas lógicas. Los 36 CSV nuevos registran **cero discrepancias nodo/celda** y cero incidentes de penetración/bajo terreno del cuerpo observado. La detención geométrica continua del jefe fijo pasa de 13,75 a 0,967 s; la del jefe sin esbirros fijo, de ~158,817 a 0,967 s. Desaparece el bloqueo persistente reproducido. El jefe fijo se mueve 60,383 s y permanece detenido 9,167 s. La corrección no garantiza la ruta más corta: sus cargas y rodeos aumentan el recorrido, aunque llega antes. No se han modificado esos comportamientos para mejorar las cifras.

**D3 prioritario, sin resolver:** con 750 iniciales y jugador móvil, S/M/L mantienen 372/3/0 publicaciones; latencia p95 0,633333/57,600000/N/D s, edad p95 0,933333/111,966667/N/D s. L no publica en 180 s y el jefe de ese control sigue sin llegar. Presupuesto 3000 intacto.

CPU Begin/Move p95 móvil 0,7416–0,7492 ms, máximos 1,5467/0,8811/0,8789 ms; Begin/Direction/Move observado p95 0,8564–0,8632 ms. No comparar como coste idéntico: ahora el jefe se desplaza 149,183 s frente a 107,4 s antes, con trayectoria diferente. Arrays 28 712 520 B; heap Editor muestreado 447 582 208–465 903 616 B; residencia del proceso 1 455 104 000–1 455 218 688 B; privados 1 660 006 400–1 663 373 312 B. Cero asignaciones en Begin/Move medidos; mismas limitaciones de alcance, muestreo y Editor descritas abajo.

Pruebas **nuevas tras corregir**: `scripts\u3.cmd edit` **729/729**, `scripts\u3.cmd play` **53/53**, incluidos A/B/C y controles D0. XML/log en `unity/TestResults/P0/D0/Correction-20261014`. Verificador histórico ejecutado: U0/U2/U3/U4 coinciden, instantánea `qa-results/reference-9e1155c37a574ba1a8d5d60719343f7e`. Integridad final 9/9 huellas iguales (U6, u3-world, siete ajustes); sin build U6 nueva. Los resultados anteriores quedan conservados en `Verified-20261014`.

Trazas posteriores a la corrección bajo `D0/Diagnosis`, prefijo `20261014T`: `153644114`, `153645481`, `153651913`, `153653550`, `153705087`, `153706611`, `153713753`, `153715093`, `153721753`, `153723422`, `153734982`, `153802244`. Ejecutado `python scripts/p0-d0-report.py <carpeta>` para las doce carpetas (36 CSV). Corrección validada sin regresiones funcionales en las suites; continuar D1 según autorización, sin declarar resueltos inanición, óptimo de recorrido ni presupuesto residente exclusivo.

Se recibió trabajo local sin publicar: autorización D0–D4 en once documentos, `RegionalBaselineTests.cs`/meta, catorce CSV/informes y XML EditMode 707/707. Son evidencias heredadas, conservadas en `unity/TestResults/P0/D0` y copiadas junto con los XML/logs a `Inherited-20261012`. Las siguientes ejecuciones del arnés original escriben en `D0/LegacyRuns/<fechaUTC>` para no sobrescribirlas.

Con el Editor cerrado, desde CMD en la raíz:

```cmd
scripts\u3.cmd edit
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-d0-geometry.ps1
scripts\u3.cmd play
python scripts\p0-d0-report.py unity\TestResults\P0\D0\Diagnosis
```

`RegionalDiagnosisTests` escribe en `D0/Diagnosis/<fechaUTC>/<control>-fixed|moving/`. Cada control tiene tres repeticiones (`0`, `1`, `2`), CSV por tick e ID estable, eventos de solicitud/cálculo/publicación por clase y topología de los nodos visitados. Una cuarta simulación sin observador debe producir el mismo resumen lógico. No usa presupuesto temporal para decidir la simulación. CPU y memoria se excluyen de la comparación determinista.

Condiciones: mapa `HORDA750` de 128 m, RNG de combate `P0QA`, personaje Remedios, director automático desactivado, armas desactivadas, invulnerabilidad QA, dt=1/60, máximo 180 s y warmup CPU de 2 s. Se conserva `WorldRun.Step` con física/combate completos. Inicio L `(-14,25; 0; -0,25)` y objetivo inicial `(42,75; 4,5; -30,25)`. El control móvil conserva el círculo cinemático heredado de radio 2 m y velocidad 3 m/s sobre la superficie; **no valida locomoción del jugador**. Las posiciones reales del jugador se registran también: el control fijo no anula empujes del combate.

Controles: cuerpo L sin controlador de ataques; jefe completo con sus esbirros normales; jefe con retirada de esbirros después del tick (solo aislamiento QA); jefe con 749 enemigos iniciales adicionales, repartidos según la misma fixture heredada. Se añaden cuerpo y jefe aislados de ID 750: se crean y retiran los 749 previos mediante APIs normales antes del ensayo, sin reiniciar IDs ni modificar sus reglas. Esto evita atribuir a la horda diferencias de velocidad o paridad del desvío. No se mantiene una población artificialmente constante: los esbirros aumentan la carga y el pico se informa.

## Qué mide cada dato

- **Altura heredada:** estable y `Y > altura inicial del objetivo − 0,2`; no exige XZ, región ni contacto.
- **Región destino:** macroregión procedural inicial, altura ±0,2 m y apoyo estable. No es todavía una región de navegación D1.
- **Llegada real:** apoyo estable en nodo, error respecto a su superficie ≤0,01 m, diferencia vertical con jugador ≤0,2 m, distancia XZ ≤ suma de radios y segmento libre entre ambos a altura de 1 m. El control fijo termina aquí; el móvil observa los 180 s completos y registra la primera llegada.
- **Tiempo:** movimiento efectivo y detención son un eje; fases de combate son otro. Un aviso puede conservar inercia o separación. Las categorías exclusivas tienen prioridad aviso → recuperación → carga → cola física → retención de campo → bloqueo geométrico sin movimiento → otra locomoción. No sumar ejes como si fueran tiempos disjuntos.
- **Bloqueo:** retorno de `Move` y ausencia efectiva de desplazamiento. La cola física del jefe se observa en su propia llamada, no mediante el contador global de esbirros. La falta de descenso de distancia de campo por tick es solo un indicador: incluso caminar dentro de una celda puede no cambiarla.
- **Límite de cola física:** `QueueWaits` detecta específicamente espera entre escaladores en `Traverse`; cero en el jefe, que camina por la rampa, no excluye contactos o empujes entre cuerpos al caminar. El arnés no mide por separado toda restricción física causada por separación. No usar ese cero como prueba de ausencia general de congestión.
- **Recorrido:** distancia 3D integrada; distancia alejándose del objetivo instantáneo; giro acumulado; pasos que invierten el rumbo previo; revisitas de nodos; transiciones de macroregión; topología y accesos. Alejarse puede ser necesario para rodear un obstáculo, no es automáticamente un fallo.
- **Campos:** solicitudes deseadas por clase, inicio del cálculo y publicación real. Latencia desde la solicitud que originó el cálculo, aunque el objetivo ya haya cambiado al publicar. Edad de la muestra de objetivo representada por el campo publicado durante movimiento continuo. Si no hay publicación, se informa como ausencia de campo, **nunca edad cero ni latencia cumplida**. Las clases sin enemigos no se interpretan como clases hambrientas.
- **CPU:** `productionNavigationBeginMoveMs` usa el contador existente (Begin/Move); `navigationBeginDirectionMoveMs` añade Direction y el coste de llamadas del observador. Ambos excluyen otras consultas de combate/steering: no equivalen a toda la navegación D3 ni a FPS. `observedStepMs` incluye el observador de campos dentro del tick; CSV, análisis y muestreo de memoria ocurren fuera del intervalo.
- **Memoria:** payload de arrays primitivos directos de EnemyNavigation, grafos y campos; no incluye cabeceras, objetos, listas ni consultas. `GC.GetTotalMemory(false)` mide heap gestionado del Editor, incluido arnés y otras fixtures. Working set y bytes privados se leen con `GetProcessMemoryInfo` de Windows a 1 Hz: proceso completo, no bytes atribuibles exclusivamente a navegación, ni pico continuo. No se declara cumplido el límite de 160 MiB con estos datos.

## Verificación intermedia

Primera ejecución nueva `scripts\u3.cmd edit`: **715/715**, XML/log conservados en `D0/FirstPass-715`. Confirmó determinismo y transparencia del observador. Su llegada usaba margen horizontal adicional de 0,1 m y no comprobaba el segmento libre; se endureció después. Su contador `Process.WorkingSet64` devolvía cero en Mono: dato inválido, sustituido por API nativa Windows con aserción de valor positivo. No usar esos ceros como memoria medida.

Un segundo arranque de EditMode terminó antes de ejecutar pruebas con código Unity 198 (licencia sin entitlement dentro del entorno restringido). Log preservado en `D0/edit-license-198.log`; se repitió fuera de ese entorno. No se cuenta ese intento como regresión ni como suite ejecutada.

## Resultados finales de llegada y recorrido

Tres repeticiones iguales en los doce controles, más doce simulaciones sin observador con el mismo resumen lógico. Cero penetraciones y cero cuerpos del jefe bajo terreno en los casos observados. Esto no prueba ausencia de fallos de persecución: varios controles no llegan.

| Control, objetivo fijo | ID | Altura heredada (s) | Región (s) | Llegada real (s) | Recorrido hasta llegada (m) | Movimiento / detenido (s) |
| --- | --- | --- | --- | --- | --- | --- |
| Cuerpo L | 1 | 26,000 | 26,150 | 27,117 | 92,732 | 27,117 / 0 |
| Jefe completo | 1 | 69,567 | 69,733 | 70,867 | 131,441 | 51,183 / 19,683 |
| Jefe sin esbirros | 1 | No | No | No en 180 s | 56,374, sin llegada | 18,317 / 161,683 |
| Cuerpo L, ID emparejado | 750 | 26,283 | 26,433 | 27,400 | 92,716 | 27,400 / 0 |
| Jefe completo, ID emparejado | 750 | 66,467 | 66,617 | 69,383 | 168,881 | 59,933 / 9,450 |
| Jefe + horda, 750 iniciales | 750 | 71,767 | 71,783 | 71,983 | 178,098 | 69,133 / 2,850 |

Al cumplir la altura heredada, el jefe ID 1 todavía está a **6,751 m horizontales** y 0,188 m verticales del objetivo. Su llegada real exige 1,300 s más. El control fijo con horda alcanza 780 cuerpos por sus esbirros; el móvil, 800. No confundir semilla HORDA750 con población inicial.

Con objetivo móvil: cuerpo ID 1 llega por primera vez a 28,150 s y cuerpo ID 750 a 28,433 s; jefe ID 750 sin horda llega a 66,867 s. Jefe ID 1 y jefe con horda **no llegan en 180 s** (distancia horizontal final 10,718 y 8,958 m). El jefe sin esbirros tampoco llega (21,950 m al final). No se demuestra una persecución válida solo porque exista un camino estático.

Consulta estática independiente al mismo punto, desde el mismo inicio, sobre los grafos aprobados:

| Clase | Longitud de polilínea del camino elegido (m) | Coste del campo /100 | Aristas caminar / escalar / caer |
| --- | --- | --- | --- |
| S | 73,476 | 74,15 | 109 / 1 / 0 |
| M | 74,233 | 74,89 | 111 / 1 / 0 |
| L | 94,347 | 94,44 | 161 / 0 / 0 |

El cuerpo L necesita ~20,9 m más que S en la ruta seleccionada por el mismo algoritmo: el tamaño y la geometría sí explican una parte del rodeo. Son caminos de mínimo **coste del grafo**, no una prueba de mínimo tiempo de combate ni de longitud geométrica continua óptima. El cuerpo aislado se detiene al entrar en alcance, antes del centro de la meta. Todos los jefes observados tienen **cero escaladas y cero caídas**: suben caminando por la rampa. La entrada del jefe fijo ID 1 en la macroregión 3 queda registrada en `(42,751; 4,5; −24,006)`. Las veinte escaladas de la prueba heredada corresponden a los esbirros.

### Separación del tiempo del jefe ID 1, objetivo fijo

| Fase | Duración (s) | Con desplazamiento (s) | Distancia recorrida (m) |
| --- | --- | --- | --- |
| Espera/persecución (`idle`) | 34,367 | 27,783 | 76,324 |
| Aviso (`windup`) | 15,933 | 12,767 | 6,867 |
| Ataque/carga (`attack`) | 6,167 | 3,300 | 46,248 |
| Recuperación de combate | 14,400 | 7,333 | 2,002 |

Combate ocupa 36,500 s; **no equivale a 36,500 s inmóvil**, porque hay carga, inercia y separación. La detención total es 19,683 s. Categorías exclusivas: 15,933 aviso + 14,400 recuperación + 6,167 carga + 6,583 bloqueo geométrico fuera de esas fases + 27,783 otra locomoción = 70,867 s. Cola física individual del jefe: 0 s. Se registran 27,928 m alejándose del objetivo y 25 revisitas de nodos; el cuerpo aislado tiene 5,301 m y cero revisitas. El jefe camina con cambio de altura durante 7,567 s y recorre 12,911 m en esos ticks (rampa, sin Climb/Ledge).

El contador indirecto de falta de descenso del campo acumula muchos ticks incluso en el cuerpo aislado que llega sin detenerse: la distancia es constante dentro de una celda. No sirve para cuantificar cola física. Los CSV y el analizador separan esos conceptos.

## Defecto funcional heredado demostrado: redondeo y nodo incoherente

`EnemyNavigation.TryStep` calcula `CellOf(x,z)` en double, valida la arista y **después** almacena X/Z como float. El redondeo puede cruzar una frontera de celda que no fue validada.

Reproducción del control `no-minions-fixed`, tick 1270 (21,183 s al terminar el tick):

- Nodo 5088, celda 21930; posición previa X=21,499996185302734.
- Candidato double X≈21,499999735302843: todavía celda 21930, aceptado.
- Almacenar float produce X=21,5: posición en celda **21931**, conservando nodo de la **21930**.
- El nodo 5088 no tiene una arista Walk hacia 21931. Su padre válido es la arista 36397 hacia nodo 5162, celda 22186, centro `(21,25; 0; −20,75)`.
- El desvío temporal gira la intención hacia el borde bloqueado. La discrepancia también bloquea el deslizamiento que mantiene X; el temporizador vuelve a armarse y el control queda inmóvil desde 21,183 s hasta terminar a 180 s. Campo L publicado desde 0,250 s, versión 1, meta 3965: **no es falta de cálculo ni cola de enemigos**.

`RegionalGeometryDiagnosisTests.RecordsInheritedRoundingMismatchWithoutChangingRuntime` reproduce la aceptación y discrepancia con una llamada directa a `TryPlace`, sin combate, horda ni esbirros. Sus aserciones describen expresamente el defecto actual; no constituyen un criterio de aceptación ni deben mantenerse esperando el defecto después de corregirlo.

El jefe completo fijo ID 1 presenta la misma incoherencia en nodo 5487 (celda 23240 frente a posición 23241, X=36,5): 13,767 s de discrepancia y 13,750 s de detención geométrica continua. Con esbirros consigue salir; al retirarlos aparece el bloqueo persistente anterior. Los controles demuestran la influencia de la interacción entre cuerpos, no un beneficio general de añadir enemigos. No se atribuye toda la diferencia de tiempo a una única causa: cambian recorrido, cargas y separación.

### Propuesta acotada, pendiente de aprobación; NO aplicada

En `TryStep`, convertir el candidato X/Z a float **antes** de calcular la celda, seleccionar el vecino y evaluar la superficie. Validar y almacenar exactamente esas mismas coordenadas. Si el redondeo aterriza en una celda sin conexión física válida, rechazar el paso y conservar el deslizamiento ya existente; sin snap de recuperación, teletransporte, aristas nuevas, cambio de tamaño ni ajustes de combate/desvío.

Pseudocódigo del cambio propuesto, al inicio de `TryStep`:

```csharp
x = (float)x;
z = (float)z;
// A partir de aquí, CellOf, WalkNeighbor, SurfaceY y almacenamiento
// deben usar las mismas coordenadas representables.
```

Validación tras aprobar: convertir el diagnóstico mínimo en aserción de coherencia `NodeCell == CellOf(X,Z)` para movimiento Walk, cubrir fronteras positivas/negativas y esquinas, y repetir controles D0 y regresiones A/B/C. Evaluar si queda otro atasco del desvío después de eliminar la discrepancia; **no prometer que este único cambio resuelva todo** ni modificar el desvío preventivamente.

## Campos: latencia e inanición con objetivo móvil

Meta fija, una publicación inicial por clase/repetición:

| Población | S (s) | M (s) | L (s) |
| --- | --- | --- | --- |
| Cuerpo/jefe ID 1 al inicio | 0,717 | 1,133 | 0,250 |
| 750 iniciales | 0,483 | 0,900 | 1,133 |

Con un solo cuerpo L las clases vacías se calculan inicialmente también; no interpretar su latencia como espera de enemigos activos. Con 750 y objetivo móvil, S y M sí tienen población activa:

| Clase | Solicitudes / publicaciones en 180 s | Latencia solicitud→publicación p95 | Edad de muestra publicada p95 / máximo |
| --- | --- | --- | --- |
| S | 1376 / 372 | 0,633 s | 0,933 / 0,950 s |
| M | 1376 / 3 | 57,600 s | 111,967 / 115,017 s |
| L | 818 / **0** | Sin publicación; espera ≥180 s | N/D: nunca hubo campo publicado |

`EnemyNavigation.Begin` sirve primero la clase con mayor población. S vuelve a iniciar trabajo continuamente y consume casi todo el presupuesto de 3000; M avanza muy lentamente y L no obtiene suficiente trabajo. No se soluciona midiendo únicamente las publicaciones que coinciden con la última solicitud: esa métrica heredada omitía campos antiguos y ocultaba la inanición.

Jefe ID 1 con sus esbirros y objetivo móvil: L publica 46 veces, latencia p95 32,483 s, edad p95 61,767 s y máximo 64,750 s. El cuerpo aislado L móvil publica 645 veces: latencia p95 0,467 s, edad p95 0,700 s y máximo 0,900 s. Incluso ese control excede 0,3 s; el tamaño del campo completo también limita la respuesta. Los 3000 se conservan.

La política justa entre clases y los campos locales previstos para D3 responden a este problema ya autorizado; no se ha aplicado un parche de planificación a C. La ausencia de campo tampoco hace que C retenga automáticamente al cuerpo: puede intentar perseguir directamente. Por eso ausencia de `hold` no demuestra ruta válida.

## CPU, asignaciones y memoria realmente medidas

Tres repeticiones EditMode, warmup 2 s, mismos datos lógicos; intervalos siguientes son el mínimo/máximo entre los tres p95:

| Control 750 iniciales | Begin/Move existente p95 (ms) | Begin/Direction/Move observado p95 (ms) |
| --- | --- | --- |
| Meta fija | 0,178–0,190 | 0,264–0,293 |
| Meta móvil | 0,616–0,628 | 0,706–0,718 |

En móvil, contador existente p99 0,683–0,708 ms, máximo 0,918–1,392 ms; observado p99 0,782–0,813 ms, máximo 1,059–1,495 ms. **No se declara aprobado el objetivo D de navegación completa:** el contador excluye parte del steering/consultas, hay rutas sin publicar y cuerpos bloqueados, y son tiempos del Editor, no FPS ni build Normal/Development. 0 bytes asignados en las llamadas medidas Begin/Move; no equivale a cero asignaciones de todo el tick ni del arnés.

En el control móvil de 750, las tres repeticiones dan:

- Arrays primitivos directos: **28 712 520 B = 27,38 MiB**, iguales en todas; recuento parcial, no memoria total de navegación.
- Heap gestionado muestreado del Editor: **452 345 856–477 577 216 B** (~431–455 MiB), incluye otras fixtures y trazas retenidas.
- Working set real del proceso Editor: **1 438 228 480–1 438 703 616 B** (~1372 MiB), Windows `GetProcessMemoryInfo` a 1 Hz.
- Bytes privados del proceso: **1 629 069 312–1 634 140 160 B** (~1554–1558 MiB).

Estos datos no aíslan residencia de navegación ni son memoria QA standalone. Ese aislamiento, transitorios y estabilización de caché siguen pendientes para la arquitectura regional/D4. No comparar el proceso Editor con el límite de 160 MiB ni extrapolar FPS de los ticks.

## Evidencias, pruebas y continuidad

- Edit completo nuevo: **719/719**, `scripts\u3.cmd edit`, 12/10/2026; XML/log archivados en `D0/FinalPass-719`. Incluye los 14 controles heredados repetidos ahora y los 12 controles nuevos, cada uno con tres repeticiones y una sin observador. La primera pasada 715/715 queda separada.
- Dos pruebas añadidas después para ruta por clase y reproducción mínima: **2/2**, `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-d0-geometry.ps1`, XML/log `D0/Geometry-20261012T190312`; consulta estática en `D0/Static/20261012T170322540/routes.csv`. No afirmar una suite completa 721/721: se ejecutaron 719 completos y después estas dos pruebas nuevas.
- Intento de geometría en sandbox: licencia 198, log `D0/Geometry-20261012T184030/edit.log`; reintento correcto fuera del entorno restringido.
- Trazas finales: carpetas UTC `20261012T163610212` (body-fixed), `163611439` (body-moving), `163616879` (boss-fixed), `163618408` (boss-moving), `163629332` (no-minions-fixed), `163631016` (no-minions-moving), `163636815` (body-id750-fixed), `163638060` (body-id750-moving), `163643483` (boss-id750-fixed), `163644959` (boss-id750-moving), `163654797` (horde750-fixed), `163716543` (horde750-moving); todos llevan prefijo de fecha `20261012T` bajo `D0/Diagnosis`.
- `scripts/p0-d0-report.py` genera los JSON de análisis desde CSV; no modifica la simulación ni las trazas fuente.

**D0 diagnosticado; D1–D4 pendientes.** Detener el avance ante el defecto heredado, presentar la corrección anterior y esperar aprobación exclusivamente para cambiar ese comportamiento C. La autorización D0–D4 no se revoca ni se vuelve a preguntar. D5–D7 siguen excluidos. Las comprobaciones de PlayMode/build/integridad/publicación se registran en el checkpoint P0 al terminar.

## Revalidación independiente del 14/10/2026

Base de ejecución `1503780404bc3c73920f058bcd8a0909f68a820c` más los arneses locales conservados. Runtime idéntico a esa base. Las tablas anteriores conservan los datos heredados del 12/10; los resultados de esta sección corresponden a la nueva ejecución y prevalecen para esta entrega.

Se reproducen exactamente los seis controles fijos de la tabla, incluida llegada real del jefe ID 1 a 70,866667 s, 131,441313 m recorridos, altura a 69,566667 s con 6,750814 m horizontales todavía pendientes, movimiento 51,183333 s y detención 19,683333 s. Las fases suman 34,366667 s de espera/persecución y 36,5 s de combate (aviso 15,933333; carga 6,166667; recuperación 14,4); son un eje distinto al movimiento. El control sin esbirros no llega en 180 s. Los campos de meta fija con 750 publican en 0,483333/0,900000/1,133333 s para S/M/L.

Con 750 y meta móvil se repiten 1376/1376/818 solicitudes y 372/3/0 publicaciones S/M/L; latencia p95 0,633333/57,600000/N/D s y edad p95 0,933333/111,966667/N/D s. L no publica durante los 180 s, por lo que su ausencia no se convierte en una edad cero. La navegación directa de fallback no demuestra una ruta disponible. El jefe móvil no llega, ni con su población normal ni con la horda inicial; el ID 750 sin horda sí llega.

El atasco por incoherencia nodo/posición, el rodeo de la clase L, los ciclos de combate y las cargas que alargan el recorrido explican causas distintas observadas. No se atribuye toda la demora a combate, colas ni presupuesto. La reproducción mínima y las trazas permiten proponer el cambio preciso de `TryStep` descrito arriba; **sigue sin aplicarse**. D1 se mantiene pendiente de esa decisión funcional, no de los procesos de otro proyecto.

Resultados nuevos CPU/memoria (tres repeticiones, mismo alcance limitado de contadores que arriba):

| 750 iniciales | Begin/Move p95 (ms) | Begin/Direction/Move observado p95 (ms) |
| --- | --- | --- |
| Meta fija | 0,1885–0,2140 | 0,2827–0,3192 |
| Meta móvil | 0,6433–0,6908 | 0,7405–0,7996 |

Móvil: Begin/Move p99 0,7483–0,7670 ms, máximo por repetición 0,8742 / **7,5127** / 1,7720 ms. Observado p99 0,8641–0,8873 ms, máximos 1,1615 / **7,6903** / 1,8662 ms. El pico aislado se conserva, sin atribución causal adicional ni declaración de cumplimiento del máximo de 2 ms. No se ha cambiado runtime ni se demuestra una regresión contra una build comparable. No equivalen a navegación total ni FPS.

En móvil: arrays 28 712 520 B (27,38 MiB); heap gestionado del Editor muestreado 447 270 912–465 416 192 B (426,55–443,86 MiB); working set Windows 1 417 711 616–1 417 928 704 B (1352,04–1352,24 MiB); bytes privados 1 618 644 992–1 623 072 768 B (1543,66–1547,88 MiB). Cero bytes asignados en las llamadas Begin/Move instrumentadas. El heap y la residencia incluyen Editor/fixtures/arnés; no prueban el presupuesto de navegación residente ni el de QA standalone. Sin nuevas builds ni baseline de render: pendientes, no se deducen de EditMode.

**Pruebas nuevas:** `scripts\u3.cmd edit`, 14/10, **721/721**, de 15:17:43Z a 15:22:34Z; XML/log en `unity/TestResults/P0/D0/Verified-20261014/edit.*`. Incluye los catorce controles heredados reejecutados, los doce nuevos con tres repeticiones observadas y una sin observador, y las dos pruebas geométricas. Se demuestra repetibilidad dentro de cada control; no se exige que un control distinto produzca la misma trayectoria. La primera llegada móvil del cuerpo ID 1 es **28,166667 s** con el criterio estricto actual (el resumen anterior indicaba 28,150); cuerpo ID 750 28,433333 s, jefe ID 750 sin horda 66,866667 s.

Trazas nuevas, bajo `D0/Diagnosis`: `20261014T151949689`, `151950994`, `151956561`, `151958142`, `152008269`, `152010071`, `152015972`, `152017341`, `152023041`, `152024645`, `152034587`, `152057712` (todos con prefijo `20261014T`). Consulta estática: `D0/Static/20261014T152218939/routes.csv`. Análisis ejecutado únicamente sobre estas carpetas mediante `python scripts/p0-d0-report.py <carpeta>`, 36 CSV procesados; evidencia previa intacta. Las pruebas adicionales de PlayMode e integridad/publicación constan en el cierre del checkpoint P0.
