# H1e — diagnóstico y certificación de memoria

## Contrato — 24/10/2026

Autorización exclusiva del autor desde `bcdcc0f`, HEAD/origin coincidentes tras fetch. Solo tests, instrumentación, scripts y documentación. Sin investigación avanzada; consulta técnica acotada a fuentes oficiales de Unity/Mono. No modificar producción, EnemyNavigation, U3, H1b ni H1d originales, geometría, generador, validaciones, economía, guardados o web. Sin servicios, H2–H4/D4/D5 ni optimización. Conservar 3000 unidades globales/tick, siete ajustes Unity, nueve huellas y once referencias. Commit español y push normal; detenerse al terminar para revisión.

A: validar contadores con reservas conocidas, incluyendo tareas paralelas; distinguir managed vivo/basura, asignaciones acumuladas, nativo y residencia. Analizar GC forzado y resta pico menos retenido. Instrumentar fases sin alterar originales; cualquier medida no validada será N/D, nunca cero.

B condicionada a instrumentación fiable: piloto primero, después diez pares independientes por MAMPORRO/Z9K2/RAMPA42 ×16/32/64 m, orden alterno y preparación completa con todos los intentos/validaciones. Conservar objetivos p95 ≤4 s, máximo ≤6 s, navegación residente ≤160 MiB, transitorios adicionales ≤96 MiB, managed QA ≤256 MiB, Unity asignado ≤512 MiB. No interpretar payload como residencia, eventos como bytes ni muestreo como pico certificado.

C: atribuir solo lo demostrado; si no es posible, documentar la incertidumbre. Proponer optimización únicamente si se localiza exceso atribuible/reproducible, sin implementarla. No aprobación automática del híbrido.

Evidencias locales: `unity/TestResults/P0/H1e-20261024`. Manifiestos iniciales: 9/9 huellas y 11/11 referencias coincidentes con H1d.

## Puerta A: contadores y límites

Calibración final de contadores: `Regional-20261024T110109`, **6/6**. Pasar los tests significa haber ejecutado los controles; no significa que todas las APIs probadas sean válidas. La reserva se toca por páginas y se mantiene viva; el control verifica longitud y contenido.

| Mecanismo | Control ejecutado | Uso permitido |
| --- | --- | --- |
| `GC.GetAllocatedBytesForCurrentThread` | Cero bruto con 4 KiB, 1 MiB y 16 MiB reales | **N/D** para asignaciones |
| `GC.GetTotalMemory(false)` / `Profiler.GetMonoUsedSizeLong` | 8192 / 1052672 / 16781312 B ante esas reservas; liberación de 16 MiB tras GC | Heap managed observado, incluye objetos vivos y basura, sin atribución exclusiva |
| `ProfilerRecorder("GC.Alloc")` | Un evento por array; valor 0–300, no tamaño del array | Eventos/tiempo del marcador; **N/D** como bytes |
| `ProfilerRecorder("GC Allocated In Frame").CurrentValue` | Deltas **4128 / 1048608 / 16777248 B**; worker 1 MiB → **1048608 B** | Asignaciones agregadas de todos los hilos dentro de la ventana síncrona |
| El mismo contador a través de GC | 16 arrays de 1 MiB desechados, GC intermedio: **16778112 B**; esperado mínimo 16777728 B con cabeceras | No confundir asignación acumulada con heap/temporales simultáneos |
| Lectura del contador | 1000 lecturas sin reservar objetos: **0 B**, cero validado | Coste de consulta distinto del coste total del arnés |
| `LastValue` / metadatos de frames del Profiler | Sin muestras completadas útiles en este EditMode batch; marcadores propios ausentes | **N/D**; no interpretar cero como ausencia de asignaciones |
| `Process.WorkingSet64` / `PrivateMemorySize64` en Mono | Delta bruto cero ante `AllocHGlobal` de 32 MiB tocados | **N/D** en este runtime |
| Win32 `GetProcessMemoryInfo` | Working set +33554432 B, privados +33624064 B ante esa misma reserva | Memoria del proceso completo, no navegación |
| `Profiler.GetTotalAllocatedMemoryLong` | `NativeArray<byte>` de 16 MiB → +16777306 B | Asignador Unity observado; no todo el nativo del proceso |
| Ese contador ante `AllocHGlobal` | Solo +112 B de ruido, no 32 MiB | No usarlo como memoria nativa total |
| `GC.CollectionCount(0)` | Dos GC forzados registrados; liberación conocida comprobada | Colecciones agregadas del entorno, no GC exclusivo de navegación |

**Causa del contador a cero comprobada localmente:** `GetAllocatedBytesForCurrentThread` es una `InternalCall`, sin cuerpo IL. Se resuelve con `mono_lookup_internal_call` y se leen sus primeros bytes, sin parchearlos: **`33 C0 C3` = `xor eax,eax; ret`** en Windows x64. La implementación devuelve cero incondicionalmente. DLL instalada `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll`, SHA256 `B76DBD488BC9181C3E8D03C66633C90F81BA7BA00947FF2B12C558B9E86E3F06`, Unity `6000.6.3f1`. No es una asignación optimizada fuera del programa. Además, medir solo el hilo principal habría omitido los tres grafos construidos en tareas paralelas aunque esa API funcionara.

`CurrentValue` funciona antes del cierre del frame; `LastValue` no lo sustituye. Las cargas son síncronas: cada ventana comprueba que no cambió el número de muestras del recorder y que el delta es positivo. La calibración conocida se repite al principio de **cada** test comparativo. Si hay cambio de frame/fallo de control, el test falla y sus cifras no acreditan presupuestos. El contador incluye otros hilos del Editor y del observador: acredita el total de la ventana, no bytes exclusivamente causados por navegación. No se deduce un pico transitorio restando payload al total acumulado.

Fuentes oficiales consultadas de forma acotada: [opciones de ProfilerRecorder en Unity 6.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.html), [seguimiento de asignaciones y diferencias del Editor](https://docs.unity.com/en-us/engine/6000.5/manual/analysis/performance-memory/managed-memory/track-garbage-collection), [semántica de memoria managed usada](https://docs.unity.cn/6000.0/Documentation/ScriptReference/Profiling.Profiler.GetMonoUsedSizeLong.html), [bindings del Profiler](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Profiler/ScriptBindings/Profiler.bindings.cs). La compatibilidad se decide con los controles del binario instalado, no por suponer que otra versión/backend se comporta igual. No investigación avanzada.

### Memoria atribuible: corregir el payload incompleto

`NavGraph.BuildEdges` conserva la capacidad del `List<Edge>` tras `Clear()`, y también las listas de trayectorias después de copiar a arrays. El censo anterior contaba arrays primitivos directos y portales, pero no los arrays internos de esas listas. Son memoria retenida del grafo, **no temporales liberados**.

`H1ePayload` recorre arrays directos y arrays de respaldo de listas de las raíces explícitas, deduplica referencias y mide el paso real entre elementos de arrays fijados. Control: array de 4096 B + lista vaciada con capacidad de 128 `int` = **4608 B**, incluyendo los 512 B retenidos por la lista vacía. Strides comprobados: `int` 4, `bool` 1, `NavGraph.Edge` 20 y `NavPortal` 44. Los grafos originales se cuentan una vez; la vista los referencia y no los vuelve a sumar. No se recorren silenciosamente terreno/consultas compartidas como si pertenecieran exclusivamente al índice.

Censo nuevo del reutilizado (tres clases simultáneas, nueve combinaciones, dos diagnósticos por celda):

| Parte | MiB de payload retenido atribuible |
| --- | ---: |
| Arrays de grafos originales, incluidas capacidades | 44,005–44,586 |
| De lo anterior: capacidades de listas omitidas por H1d | **25,830–25,863** |
| Arrays de vista, SCC y portales | 14,967–16,685 |
| Arrays del índice H1b | 7,847–8,102 |
| Total contado por carga | **66,819–69,373** |

Es una **cota inferior reproducible de almacenamiento managed atribuible**, más completa que 40,989–43,543 MiB. No es residencia física certificada: cabeceras, alineamiento/bloques del GC, objetos no array, consultas/terreno compartidos y paginación no están completamente atribuidos. No sumar esa cifra al heap total ni a la memoria Windows: son vistas solapadas. No afirmar todavía navegación residente ≤160 MiB.

### Instrumentación por fases

Se conservan producción y H1d originales. `p0-h1e-instrument.py` genera copias exclusivas de tests: clase de mapa con las mismas validaciones/rechazos/reparaciones, vista con los mismos algoritmos y copia mecánica del índice. Solo añade límites de observación y cambia nombres/tipos necesarios. `--write` genera; sin argumentos verifica identidad textual contra la transformación definida. No optimiza ni elimina intentos. Las comparaciones de copia contra original verifican resultado de validación/log/intentos y nodos/aristas/topología en tres semillas y tres tamaños.

Generación, precomprobaciones, cada tanda de grafos originales, validaciones, proyección/portales, SCC e índice tienen observaciones de heap antes/después, máximo muestreado, asignación agregada, GC, tiempo, Unity observado y proceso Windows. **Proyección y portales están entrelazados en el constructor original:** se informa el bloque conjunto; el coste exclusivo de portales es N/D. Separarlo artificialmente requeriría reorganizar el algoritmo, fuera del diagnóstico fiel.

El heap se muestrea pidiendo 1 ms, pero eso no garantiza una muestra por milisegundo en Windows. Se registra la separación real máxima en las comparaciones. No se llama a APIs Unity sin garantía de thread-safety desde el observador: Unity se lee al finalizar etapas en el hilo principal. El máximo nativo entre etapas continúa sin certificar. Working set/privados del proceso se muestrean aparte y `processLifetimePeak` es el máximo desde el arranque del Editor, no el máximo exclusivo de esa carga.

Dos diagnósticos por celda contrastan GC habitual frente a GC forzado entre mapa y proyección. **No son tiempos de aceptación B**: el GC intermedio es una intervención causal y las copias contienen instrumentación interna. La primera implementación del bucle conservaba referencias locales entre iteraciones (`104906`, 12/12): se conserva como evidencia de contaminación, se separaron llamadas y se liberaron referencias explícitas (`105143`, 12/12). El heap inicial todavía puede variar por raíces conservadoras/entorno; nunca se resta una base fija supuesta para atribuir el resto a navegación. Censo ampliado `105436`, 13/13; repetición final con contador de asignaciones por fase pendiente.

## Puerta B: piloto y comparación

Piloto `Regional-20261024T105855`: **9/9, 18 cargas**. Los controles de asignación pasan y no hay reinicio de frame durante las ventanas. El prototipo usa 2,039–2,362 s; el control llega a **6,150 s** (MAMPORRO 16 m), incumplimiento observado del máximo en ese piloto. No se excluye la muestra. Asignaciones y censos positivos/coherentes; abre diez pares por combinación. Primera fase de diagnóstico había dejado B cerrada mientras faltaba contador; el posterior control válido de `CurrentValue` resuelve ese bloqueo instrumental, no certifica por sí solo toda la memoria.

Comparación completa en ejecución: cada llamada crea mapa/circuito/grafos/mundos/índices propios, retiene las tres clases y ejecuta todas las validaciones originales. Orden reutilizado-original en repeticiones pares e inverso en impares. Son arranques independientes de navegación dentro del Editor batch, no 180 arranques fríos de procesos. Se informa base de cada carga y no se presupone que los GC forzados eliminen todas las raíces del Editor. Preparación exterior incluye instrumentación dentro de la ventana; `diagnosticMs` añade GC previos/posteriores y censo, antes de escribir la fila de resultados.

## Descomposición del máximo histórico

Se reextrae la evidencia H1d, **no una medición nueva**, MAMPORRO 16 m, pareja 5:

| MiB | Original | Reutilizado |
| --- | ---: | ---: |
| Pico managed observado | 226,664 | 238,809 |
| Retenido tras GC | 179,410 | 126,703 |
| Indicador pico − retenido | 47,254 | **112,105** |

La diferencia del indicador es 64,852 MiB = **12,145 MiB de diferencia de picos + 52,707 MiB por diferencia de retención** (identidad comprobada en bytes por el extractor). El 81,27 % de esa diferencia proviene matemáticamente del término retenido. **No significa que se hayan atribuido 52,707 MiB del pico a basura del Editor**, ni que se pueda descontar esa cantidad del presupuesto. Muestra por qué el constructor que retiene menos puede tener un indicador mayor sin crear esos bytes adicionales de scratch. El pico sucede antes del GC final: combinar máximos y estados finales distintos no identifica objetos ni propietarios.

Conclusión y presupuestos definitivos pendientes de la comparación completa. Ninguna optimización aplicada.
