# P0-D3 — propuesta de navegación híbrida

**23/10/2026: H1d cerrado, no aceptado por memoria.** [Contrato y resultados](EXPERIMENTO_H1D_P0D3.md). Exactitud/rutas favorables y preparación medida 1,967–2,223 s p95, ahorro real 54–64 %; exceso managed observado >96 MiB y asignación no verificable. No cambia H1b ni producción, no abre H2–H4/D4/D5. Detenerse para revisión.

**23/10/2026: H1c completado; parada para revisión.** Macro reutilizable dentro de SCC; preparación sin optimizar y unión local fallida a 64 m. [Informe](DIAGNOSTICO_H1C_P0D3.md). No se acepta H1b ni se abre H2; sin cambios de producción o validaciones.

**22/10/2026, posterior a H1b: H1c autorizado exclusivamente para diagnóstico de preparación/reutilización.** [Contrato y resultados](DIAGNOSTICO_H1C_P0D3.md). No servicio nuevo ni cambios de producción; H1b se conserva, H2–H4/D4/D5 excluidos. Revisión al terminar incluso con resultados positivos.

**Cierre H1b, 22/10/2026:** alcance y rodeos favorables en el corpus (peor p95 1,113474×, máximo 1,894511×), pero p95 de preparación MAMPORRO 5,772/5,043/4,695 s supera 4 s. **Puerta completa negativa; detenerse para revisión antes de H2.** [Informe](EXPERIMENTO_H1B_P0D3.md). No cambiar producción ni relajar límites.

**22/10/2026: H1b autorizado como experimento aislado.** Cruces adaptativos, conexiones físicas locales y corredores ponderados; A pequeña antes de B completa. [Contrato y resultados](EXPERIMENTO_H1B_P0D3.md). H1 rechazado se conserva. Sin H2–H4/D4/D5 ni integración; detenerse ante fallo y siempre para revisión antes de H2.

## Estado y alcance — 21/10/2026

**Cierre H0–H1:** contrato `8bc8203`, prototipo/informe `dc720a1`; pruebas finales 36/36, **puerta H1 negativa por rodeos**, no aceptada. Uno/dos testigos conservan alcance en las muestras pero exceden 1,5×/2×. [Resultados](EXPERIMENTO_H1_P0D3.md). Detenerse para revisión, sin H2 ni nuevo rediseño automático. Las fases posteriores siguen excluidas.

**Actualización posterior a `563f05e`: autoriza implementar solo H0–H1.** Aprobadas arquitectura A, física/alcance exactos con rodeos p95 ≤1,5× / máximo ≤2×, bandas 24/32 y 48/60, jefe crítico y reacción experimental 0,3/0,5; 0,6/1; 1,5/2 s, conservando comparación original. Cuotas/criterios se aceptan para estudio, no como validados. Contrato y resultados en [EXPERIMENTO_H1_P0D3](EXPERIMENTO_H1_P0D3.md). H2–H4, D4/D5 e integración excluidos; parada ante incumplimiento y revisión antes de H2. Las notas de propuesta inferiores conservan su estado histórico y quedan subordinadas a esta autorización acotada.

**Solo planificación autorizada. Ninguna arquitectura, cifra nueva ni fase de implementación de este documento está aprobada.** Base comprobada: `9306e38`, rama `claude/zen-pasteur-674ik0`, origin coincidente tras fetch. Este plan responde a la autorización de diseñar navegación para 300–750 enemigos sin exigir optimalidad mundial a cada actualización. No reabre automáticamente los experimentos descartados.

La autorización actual termina en documentos, revisión y push normal. No programar, integrar en `EnemyNavigation`, iniciar D4/D5, ampliar el mundo ni modificar web, referencias, generador, física, balance o persistencia. Conservar siete ajustes locales Unity y entrega U6. Sin investigación avanzada: razonamiento basado en código/documentación y medidas locales heredadas; ninguna medida nueva de rendimiento.

Siguen vigentes **3000 unidades globales/tick**, reacción p95 ≤0,3 s / máximo ≤0,5 s y los demás objetivos de [PLAN_P0D](PLAN_P0D.md). Las bandas de reacción y tolerancias inferiores requieren aprobación expresa. La autorización histórica D0–D4 no autoriza implementar este rediseño ni iniciar D4 en esta sesión.

## 1. Qué enseñaron los experimentos

| Evidencia heredada | Resultado que condiciona el diseño |
| --- | --- |
| [Jerarquía por salida e incremental](REGIONES_P0D.md) | Transferencias repetidas por miles de salidas: ninguna publicación en 180 s. Reparación mundial incremental: p95 móvil S/M/L 28,067/24,067/16,300 s. Una CPU baja por tick no arregla el retraso. |
| [Lotes exactos](EXPERIMENTO_LOTES_P0D3.md) | Mejor vecino: 1101086 unidades; móvil p95 7,833/6,750/3,700 s. Agrupar sin reducir el problema de exactitud mundial no basta. |
| [Demanda compartida](EXPERIMENTO_DEMANDA_P0D3.md) | Vecino 750 concentrados 57764 unidades; dispersos 1992760. Recorrido móvil disperso p95 85,017/102,883/0,633 s. Compartir ayuda, pero el dominio y las versiones pendientes siguen siendo demasiado grandes. |
| [Preprocesado exacto](EXPERIMENTO_PREPROCESADO_P0D3.md) | Preparación parcial 4,413–4,967 s; matriz mundial 13,80 GiB. No trasladar todas las parejas a carga. |
| [Landmarks](EXPERIMENTO_LANDMARKS_P0D3.md) | K=8 reduce trabajo individual 8,32×, pero cohorte dispersa 9889018 unidades. Revalidación 12/12 del 21/10; no es una prueba nueva de esta planificación. No elegir A* por enemigo como solución general. |

Las cifras pertenecen a arneses distintos: no sumar sus tiempos ni interpretar cocientes como aceleraciones universales. Los ensayos D3 de demanda no mueven cuerpos. D0 sí mostró jefe móvil sin llegada e inanición L en 180 s: es un problema jugable aún abierto. Las pruebas de exactitud correctas no certifican rendimiento ni persecución.

**Hipótesis propuesta:** compartir decisiones de recorrido estables a escala de componentes, resolver detalle solo donde hay consumidores y cambiar la frecuencia de planificación, conservando movimiento y colisiones a cada tick. No asumir que esta hipótesis cumple 3000 antes de medirla.

## 2. Tres alternativas

En las fórmulas, N/E son nodos/aristas físicos sumados de S/M/L; C/A son componentes locales/arcos entre ellas; F es la suma de nodos almacenados en campos activos, contando versiones; Q son demandas agrupadas. Los costes expresan orden de crecimiento y payload, no tiempos medidos.

| Alternativa | Organización | Preparación y memoria | Riesgos y escala |
| --- | --- | --- | --- |
| **A. Componentes dirigidas + corredores compartidos + detalle cercano (recomendada)** | Árbol inverso de componentes hacia el componente del jugador por clase; cruces físicos testigo; campo local compartido a cada salida; cerca, refinamiento hacia la posición real. | Reutiliza D1/D2 y sus SCC locales; topología adicional O(C+A), campos O(F), consumidores O(750). Sin tablas de todas las parejas. Preparación completa y validación aún por medir. | Rodeos y cuellos en el cruce elegido; proliferación de campos con dispersión. Trabajo global ligado a C/A y cambios de componente, no a cada celda del jugador. Detalle físico sigue creciendo con área; no promete residencia mundial acotada. |
| **B. Campos compartidos con metas cuantizadas y frecuencias por banda** | Demandas inversas como referencia conceptual; lejos se usa un ancla estable de superficie, cerca meta real. Cachear pocas anclas por clase. | Menor cambio conceptual; O(E+K·N+750), K limitado. Menos reconstrucciones si el jugador permanece cerca de un ancla. | Al recorrer anclas nuevas reaparece expansión mundial y deuda de versiones; lejos en varias alturas puede necesitar muchas anclas. Buena alternativa de control, débil para mundos mayores. Cuantizar XZ sin superficie es incorrecto. |
| **C. Rutas de cohorte con búsqueda jerárquica y reparación de prefijos** | Una ruta por grupo de orígenes compatibles, con seguidores y conexiones locales validadas; búsquedas individuales solo excepcionales. | O(C+A+longitud de rutas+F+750); preparación topológica como A, opcional landmarks después de justificar su coste. | Cohortes se fragmentan en cruces/alturas; seguidores no pueden copiar desplazamientos del líder. En dispersión Q puede tender a 750 y reaparecer A* repetido. Más lógica de unión/separación y riesgo de oscilación. |

Recomendar **A**, con B como comparador acotado solo si se autoriza y A falla por preparación o complejidad. C no se recomienda como primera implementación. No implementar las tres para decidir a posteriori ni mezclar sus costes. Mantener landmarks fuera de la primera fase: podrían acelerar una excepción medida, pero el resultado anterior no justifica tablas adicionales por defecto.

## 3. Contrato de precisión y conectividad de A

### Qué se conserva exactamente

Geometría, radios/alturas S/M/L, identificación de capa, soporte, barrido, pendientes, aristas dirigidas y trayectorias completas Walk/Climb/Drop. Mantener escalada libre aprobada, sin stamina ni límite artificial de altura o duración. No inferir un retorno de una caída ni unir tejados por proximidad. Ningún cambio de banda desplaza, encoge o teletransporta cuerpos.

Una componente propuesta es una **SCC interna de región y clase** ya definida por D2, no toda una baldosa ni una componente débil. Cada arco del grafo reducido conserva al menos un cruce físico original y su identidad. Debe incluir también los arcos dirigidos entre SCC de la misma región. Si se conserva cada conexión entre SCC, el alcance puede mantenerse aunque no se conserve el coste óptimo: dentro de cada SCC existe un recorrido dirigido entre entrada y salida. Esto se verifica contra el oráculo, no se presupone por el nombre de la estructura.

Para una meta fija se propone un árbol inverso por número de saltos de componente, con desempate estable. Cada salto reduce un rango entero. En el componente destino, el campo llega al nodo físico real o al ancla explícitamente identificada. Las conexiones internas y los cruces deben reconstruir una cadena real; una arista abstracta nunca ordena movimiento directo en línea recta.

Primera selección de cruce: determinista entre cruces físicos de la conexión elegida. Elegir pocos testigos puede crear rodeos aunque preserve alcance. Retener la lista íntegra de cruces D2 para inspección y alternativas; no reinterpretar los lotes del experimento anterior como continuidad geométrica. Medir inicialmente un testigo y, solo si hace falta y cabe, hasta dos alternativas por conexión; no precalcular transferencias nodo×cada salida.

### Qué deja de exigirse, si el autor lo aprueba

Optimalidad mundial y misma cadena canónica de padres en cada actualización. Lejos se permite seguir un corredor físicamente válido hacia una meta anterior compatible durante un plazo acotado. Cerca se busca la meta real con coste exacto **dentro del dominio refinado**; eso no garantiza el óptimo mundial. Para el jefe, precisión significa ruta física correcta, refinamiento y prioridad, no una garantía oculta de optimalidad mundial.

Propuesta de calidad para metas fijas y costes positivos: cociente coste de ruta / óptimo mundial p95 ≤1,5 y máximo ≤2 en el corpus; coste cero exige ruta vacía. Medir también metros reales y número de escaladas: el coste histórico no es tiempo de combate. Son umbrales de aceptación de corpus, no garantía matemática universal del algoritmo. Si un caso los supera, se considera fallo para revisión; no se elimina la muestra. El oráculo corre fuera del solver y de sus tiempos medidos.

### Campos y refinamiento

Lejos: un campo inverso por `(clase, revisión geométrica, SCC, salida física)` sirve a todos sus consumidores. No cambia porque el jugador se mueva dentro de la misma SCC destino; el tramo final sí depende de la meta. El cálculo se fracciona y publica etiquetas resueltas con una cadena válida completa hasta la salida, sin exponer padres tentativos. No obliga a terminar toda la región para atender un origen ya resuelto.

Cerca: agrupar consumidores por clase y dominio conectado hacia la meta real. Dominio inicial formado por componentes cercanas y su corredor testigo, incluyendo rodeos que salgan del radio. Refinar con búsqueda inversa compartida y ampliación acotada si hace falta. Un límite espacial o de trabajo agotado produce **pendiente**, nunca «inalcanzable». El corredor anterior sigue disponible si conserva validez física. No construir una ventana circular que convierta paredes en refugios por recortar su rodeo.

Probar 16/32/64 m sobre los mapas actuales; no fijar 64 por su mejor carga anterior: una región mayor puede encarecer cada campo cercano. La elección debe minimizar conjuntamente carga, campos activos, reacción, rodeo y memoria. Esta es una decisión técnica experimental dentro del futuro plan, no una ampliación de mundo.

## 4. Bandas, amenaza y presupuestos propuestos

Clasificación por distancia XZ conservadora más superficie y ruta conocida. La altura sola no degrada prioridad: un enemigo justo debajo de un tejado requiere respuesta cercana. Una estimación de tiempo de contacto solo puede **promover**, no degradar; la distancia euclídea no garantiza una ruta corta. Amenaza importante: jefe siempre, enemigo en acción de ataque existente o con contacto físicamente posible próximo; no introducir nuevas reglas de daño/agro ni priorizar por visibilidad de cámara.

| Banda propuesta | Entrada / salida con histéresis | Muestreo de deseo máximo | Respuesta a meta fija nueva p95 / máximo; edad móvil p95 / máximo |
| --- | --- | --- | --- |
| Crítica | Jefe a cualquier distancia; amenaza inmediata mientras dure, retención 1 s tras cesar | 0,10 s | 0,3 / 0,5 s |
| Cercana | Entra ≤24 m; sale >32 m durante 1 s, sin amenaza | 0,10 s | 0,3 / 0,5 s |
| Intermedia | Entra desde lejos ≤48 m; sale >60 m durante 1 s | 0,25 s | 0,6 / 1,0 s |
| Lejana | Resto, sin amenaza | 0,50 s | 1,5 / 2,0 s |

**Toda esta tabla necesita aprobación.** Muestreo no significa publicación garantizada. Plazos incluyen muestreo, admisión, cola, cálculo y publicación, también en frío tras cargar el mundo. Arranque completo se mide por separado. El jefe no recibe menor frecuencia por estar lejos. Una promoción conserva la fecha de la demanda anterior y añade un plazo cercano desde la promoción; informar ambas esperas.

En móvil, medir tanto edad de la posición representada como última validación del corredor, error espacial de meta y compatibilidad de componente. Revalidar el mismo corredor no rejuvenece la posición antigua. Refrescar el dato de meta solo cuenta si la ruta sigue conectando con el tramo final correspondiente. Una ruta segura obsoleta no cumple por eso el objetivo de frescura. Publicar en paralelo resultados contra **los objetivos originales 0,3/0,5 s para todos**, para mostrar con claridad qué se propone relajar.

Todos los cuerpos se mueven y colisionan en cada tick físico; solo planificación/muestreo cambian de frecuencia. No congelar lejos ni limitar artificialmente a 100 cercanos: 750 concentrados deben poder ser cercanos. Si esa carga no cabe, el candidato falla; no degradar consumidores para mejorar percentiles.

## 5. Planificador, versiones y transición entre modos

Propuesta inicial de reparto, dentro de **3000 globales/tick**, con tareas fraccionables:

| Reserva | Unidades máximas reservadas por tick |
| --- | --- |
| Registro de deseos, clasificación, cola, publicación y mantenimiento | 300 |
| Piso por clase activa S/M/L | 600 por clase, total 1800 |
| Refuerzo de tareas del jefe o crítica más antigua | 600 |
| Bolsa por plazo y antigüedad, préstamo de reservas no usadas | 300 más sobrantes |

Dentro de cada piso de 600, reservar 150 a intermedios/lejanos pendientes; resto por plazo, antigüedad e ID estable. Si no existen, prestar. Reservas no son sumas adicionales al techo. Las tareas compartidas del jefe pueden beneficiar a otros L sin duplicarlas. Ninguna cuota garantiza terminar un trabajo demasiado grande: medir latencia y envejecimiento incluso cuando el reparto es justo.

Cobrar inicializaciones, visitas de consumidor, aristas examinadas, relajaciones, heurísticas si las hubiera, extracciones, reconstrucción/copias, publicaciones, clasificación, invalidación y mantenimiento. No esconder un recorrido completo en una unidad ni financiar preparación global fuera del tick durante juego. Mantener el criterio previo para operaciones de heap y medir su CPU. Las 300 unidades administrativas son una hipótesis a validar, no permiso para procesar 750 consumidores gratis: barrido repartido y eventos contados, con su demora incluida. El movimiento/colisión físico conserva su contrato de tick; reportar su CPU junto con planificación, sin fingir que ya estaba incluido en los contadores históricos de búsqueda.

Una cola por clase con agrupación por clave, máximo una tarea en curso y un deseo más reciente por clave; no una cola de posiciones del jugador a 60 Hz. No completar una larga secuencia de versiones antiguas: terminar la pieza local admitida, publicarla solo si es útil y tomar el último deseo en el siguiente límite de pieza. Medir trabajo descartado y tiempo de progreso; si las metas nuevas impiden publicar, falla la prueba móvil. No reiniciar la fecha de espera al agrupar o renovar deseos.

Transición propuesta: `lejano/intermedio → promoción pendiente → cercano/crítico`; degradación solo tras histéresis. Conservar el corredor válido mientras llega el refinamiento; publicación atómica de versión y conexión desde el nodo real del consumidor. Si se ha movido, conectar mediante aristas verificadas del campo o mantener pendiente; no aplicar una ruta cuyo origen ya no corresponde.

Durante Climb/Drop, fijar trayectoria y referencias hasta terminar la transición física; preparar después el nuevo tramo. No invertir a mitad de pared ni cambiar la salida por cambiar de banda. En Walk, sustituir en un apoyo válido y registrar la unión entre versiones. Cada versión reduce rango entre componentes y coste hasta su salida; no mezclar padres de dos árboles. Si cambia el objetivo, esa monotonía reinicia con la nueva versión y debe seguir habiendo progreso físico medido, no solo rangos descendentes.

Mantener hasta dos versiones en uso por dominio; consumidores que aún escalan fijan las dependencias necesarias. Si el límite de memoria no permite otra versión, espera contabilizada sin expulsar datos activos. Sin streaming, prefeteo regional ni política D4: en este prototipo futuro el grafo físico permanece residente. Una revisión geométrica incompatible invalida inmediatamente el uso, y el cuerpo espera en apoyo seguro; no autoriza editar geometría en juego.

Estados separados: ruta actual, ruta obsoleta físicamente válida, pendiente de cálculo, esperando paso, sin conexión demostrada y fallo de presupuesto. Solo la conectividad dirigida exacta puede certificar inaccesibilidad. Para L en un tejado imposible: declarar ese resultado, dirigirlo si procede a un apoyo alcanzable de aproximación, y no contar esa aproximación como llegada al jugador. S debe seguir amenazando las zonas que el contrato C exige.

## 6. Movimiento local y aglomeraciones

Primera versión futura reutilizaría las reglas físicas validadas mediante un consumidor QA separado, sin editar `EnemyNavigation`. Guiado por siguiente transición, no por perseguir directamente coordenadas atravesando paredes. Desvíos Walk con cuerpo completo, superficie compatible, segmento barrido y conservación de nodo/posición tras conversión float. Climb/Drop siguen la trayectoria física original. No bajar frecuencia de colisión ni eliminar obstáculos para resolver atasco.

Reutilizar la rejilla espacial para vecinos. Propuesta de cola estable por punto de acceso estrecho con antigüedad; no reservar una región entera ni toda una ruta. Mantener un solo recurso local por cuerpo, liberar al despejar y evitar esperas circulares. El jefe prioriza cálculo, **no empuja ni salta la cola atravesando cuerpos**. Considerar otro cruce físico solo tras espera persistente y si la ruta alternativa es válida; usar histéresis para no alternar salidas cada tick.

Separar ausencia de ruta, bloqueo geométrico, separación entre cuerpos, cola de escalada, fase de combate y espera presupuestaria. Un cambio de ruta no puede corregir por sí solo un cuello sin capacidad. No prometer simultáneamente llegada de 750 al mismo punto: medir caudal y rotación de la primera línea; las pruebas de llegada masiva usarán sumideros QA alcanzables con salida libre tras registrar cada llegada, sin teletransportar cuerpos.

## 7. Memoria, preparación y mundos mayores

Se mantienen los objetivos de PLAN_P0D: navegación residente ≤160 MiB, transitorios adicionales ≤96 MiB, managed QA ≤256 MiB, Unity asignado ≤512 MiB; preparación navegación p95 ≤4 s / máximo ≤6 s y carga visible p95 ≤6 s / máximo ≤8 s. No basta medir arrays ni tres muestras para certificar p95.

Presupuesto interno propuesto para A: hasta 16 MiB de payload de campos/versiones y 4 MiB de metadatos/colas/corredores, **dentro**, no además, de los 160 MiB. Ejemplo de planificación: 24 bytes por nodo de campo → 16 MiB permiten unas 699000 entradas sumando clases y versiones; no es memoria medida ni incluye cabeceras. Derivar el coste real del diseño antes de reservar. No guardar un campo mundial por consumidor.

Contar grafos físicos/halos, topología, índices inversos, trayectorias, campos, heaps, listas, tablas de consumidores, oráculos del arnés y copias. Separar memoria de producto y arnés; medir también total del proceso. Registrar picos durante construcción y coexistencia de versiones; GC tras estabilización no debe esconder picos. Contador de asignaciones con testigo: si falla, N/D, no cero.

Los datos heredados a 64 m rondan 34,4 MiB en arrays D1/D2 antes del instrumento de landmarks; no son residencia completa. El rediseño podría ahorrar campos globales y tablas, pero conserva O(N+E) físico. Para mundos mayores, el trabajo de rutas macro crece con C/A y campos con ocupación; 750 dispersos pueden ocupar muchas regiones. Estimar con los recuentos actuales crecimiento de área ×4/6,25/9 y peor ocupación, etiquetando cada cifra como modelo. **No crear mapas 256/320/384 m ni implementar descarga/prefeteo D4 ahora.** Si el modelo excede memoria, dejar una dependencia futura explícita; no declarar escalabilidad certificada.

## 8. Pruebas propuestas y métricas

### Matriz y controles

Todas son pruebas futuras, ninguna ejecutada en esta sesión. Control contemporáneo con backend C aprobado y mismos inputs; los ensayos D3 descartados solo son referencias históricas. Igual semilla, distribución por identidad física, velocidad, geometría, cuerpo, duración y trayectoria del jugador. Comparar exactitud de alcance y validez de aristas; no exigir igualdad de coste global al candidato aproximado si se aprueba ese contrato.

| Nivel | Casos y salida exigida |
| --- | --- |
| Topología | A/B y ocho semillas C aprobadas × 16/32/64 × S/M/L. Comparar alcance de SCC y rutas testigo contra monolítico. Fixtures de caída unilateral, reentrada en región, rampa diagonal, escalada que cruza partición, varias alturas en XZ, tejados y superficie desconectada, paso S/M excluido de L. Cero falsas conexiones; un límite de búsqueda nunca cuenta como inaccesible. |
| Planificación | 300/500/750, mezcla 80 % S/resto M/un L, más control equilibrado y clase minoritaria. Concentrados físicamente colocados sin solapamiento y dispersos. Fijo cercano/lejano/altura; móvil por más metas que entradas de caché; alternancia en bordes de bandas y de SCC. 180 s, tres repeticiones; frío y caliente separados. Registrar número real que cabe en la zona, no forzar 750 en radio 8 m si se penetran. |
| Persecución real | Cuerpos realmente móviles, pendientes, colisión/separación y transiciones; 300/500/750 concentrados/dispersos en ocho C. Jugador recorre suelo, rampa, pared, tejado y vuelve, con pausas de captura y trayecto continuo. Jefe aislado sin combate para aislar navegación, jefe completo con esbirros y jefe con horda; no confundir L sintético con jefe real. 180 s y estrés de 10 min para versiones/colas/GC. |
| Adversarios | Todos cercanos, todos lejos, jefe remoto, S dominante con M/L minoritarios, cambios de deseo durante escalada, promoción masiva, cola de acceso único y dos salidas, ruta que sale del radio, meta imposible para L, alternancia de superficies. Sin reciclado de enemigos, rescate, teleports ni reducción oculta de carga. |
| Build real | QA independiente en mapas actuales, Windows Normal con render y variante de combate/efectos, 0/300/500/750. 1080p/interna 360, hardware de referencia, cámara/entrada reproducible, VSync/cap/foco registrados. 30 s de calentamiento y 180 s medidos × tres; recorrido largo 10 min. Development separado para perfiles/memoria; sin capturas PNG durante la ventana. Esta fase necesita autorización futura específica del arnés, no sobrescribe U6. |

Ocho C: P0QA, MAMPORRO, COLINAS7, Z9K2, MESETA, ACANTILADO, RAMPA42, HORDA750. En fases iniciales usar HORDA750, P0QA y fixtures; solo ampliar al corpus completo tras las puertas baratas. Preparación: diez arranques por semilla en la fase final, informar p95 empírico/máximo y tamaño muestral; nunca una garantía universal. Mantener mismos casos fallidos y logs, aunque se detenga una fase.

### Métricas operativas

- **Servicio:** espera desde deseo original, admisión, publicación y primer paso útil; edad de meta real, error espacial, cobertura vigente por consumidor-tiempo, obsoletas, pendientes al cierre, máximo sin servicio y trabajo descartado. Desglosar por S/M/L, banda, jefe, carga, distribución, frío/caliente; no promediar minorías dentro de S.
- **Progreso:** metros recorridos, coste restante de oráculo muestreado fuera del solver, avance sobre corredor y distancia a meta real. Para jugador móvil usar también una meta congelada de control por tramo; distancia al jugador puede crecer por su movimiento y no demuestra atasco.
- **Llegada:** mismo apoyo/superficie objetivo, diferencia vertical ≤0,2 m, distancia XZ dentro del umbral de contacto acordado con radios existentes, segmento libre y estabilidad ≥0,5 s. Contar contacto/ataque real por separado. Altura alcanzada o proximidad bajo un tejado no es llegada. Informar tiempo hasta primera llegada, porcentaje por plazo, no llegados/censurados y jefe por ID.
- **Atasco/colas:** candidato a atasco si demanda de avance y sin progreso útil durante 2 s; clasificar causa con traza, sin atribuir toda quietud a colisión. Máximo continuo, porcentaje de tiempo, cola por acceso, caudal y equidad. Quietud por combate se informa por separado sin borrarla del tiempo de llegada real.
- **Coste:** unidades por categoría y tick, CPU planificación/movimiento/colisión/total y preparación; p50/p95/p99/máximo de ticks activos y de todos los ticks. No deducir tiempo de CPU ni latencia de solo dividir unidades por 3000.
- **Memoria y render:** payload, residencia, picos, asignaciones verificadas, GC, frame time y FPS reales, GPU si contador válido. Simulación acelerada, PlayMode o CPU lógica nunca sustituyen FPS de build.

### Criterios propuestos para aceptar el candidato

1. Cero aristas inventadas, penetraciones estáticas, cuerpos bajo terreno, incoherencias nodo/posición, teleports o falsas llegadas en el corpus. Cero pérdida de conectividad respecto al oráculo por compresión. Mantener tolerancias físicas heredadas; no rebajarlas para pasar.
2. ≤3000 unidades en cada tick y suma exacta de categorías; ninguna clase con trabajo pendiente queda sin cuota. Cumplir los plazos por banda si se aprueban; máximo de espera incluye solicitudes que no terminan. En sobrecarga informar fallo, sin vaciar colas al acabar para mejorar la estadística.
3. Metas fijas sin congestión: todos los cuerpos con camino deben llegar antes de `1,5 × tiempo de control + 2 s`, con control medido de cuerpo equivalente y ruta óptima sin latencia de cálculo. Si el control no termina dentro de 180 s, caso inconcluso y extensión identificada, nunca aprobado por exclusión. Jefe completo y horda: llegada no peor que 1,25× el control C contemporáneo cuando este llega; si C no llega, exigir llegada del candidato en los tramos de captura físicamente factibles. Un jugador más rápido en huida continua no exige captura imposible: se exige progreso/servicio y captura en las pausas diseñadas.
4. Cero bloqueos algorítmicos continuos >2 s con camino libre y servicio disponible; colas físicas con frente avanzando, antigüedad y caudal informados. Para el fixture de acceso único, caudal ≥90 % del control físico sin coste de planificación y ningún consumidor permanentemente pospuesto en el ensayo de 10 min. En sumideros despejados, todos los alcanzables llegan dentro de 180 s; no aplicar ese umbral a 750 disputando un único punto ocupado.
5. Ruta fija: p95 de rodeo en coste ≤1,5×, máximo ≤2×; jefe además sin regresión >25 % en tiempo de llegada comparable. Mantener todas las muestras y explicar escaladas/combate/colas. No certificar estas tolerancias hasta ejecutarlas.
6. CPU completa de navegación p95 ≤0,6/0,8/1,0 ms con 300/500/750; p99 ≤1,5 ms, máximo ≤2 ms. Memoria/carga según sección 7. Asignaciones estables 0 B/tick verificables y sin GC atribuible a navegación en 10 min; N/D deja el criterio pendiente.
7. Build Normal: objetivo 60 FPS con 750; frame p95 ≤16,7 ms, p99 ≤25 ms, sin parón repetible >50 ms atribuible a navegación. Registrar FPS/frame times con y sin limitador si este oculta margen; identificar CPU/GPU/HUD. Si falla render independiente, no declarar aprobado el conjunto ni atribuirlo automáticamente a navegación.

Los criterios 2–5 y el ensayo de build pequeña son **propuestas para aprobar**, no reinterpretaciones de la aceptación C ni autorizaciones D4/D5. La publicación de métricas parciales positivas no cierra una puerta con fallos de seguridad, inanición, reacción o presupuesto.

## 9. Fases pequeñas, después de aprobación expresa

| Fase futura | Pieza verificable y puerta de salida |
| --- | --- |
| H0 — contrato y medición | Fijar bandas, calidad, contabilidad y fixtures; manifiesto comparable de inputs/control. Sin servicio activo. Revisión de alcance antes de H1. |
| H1 — topología y testigos | Árbol compartido sobre SCC, reconstrucción física y control de rodeos; 16/32/64 en mapas actuales. Puerta: alcance exacto, rutas válidas, preparación/memoria compatibles. Sin scheduler ni enemigos todavía. |
| H2 — servicio compartido | Campos locales, refinamiento, cuotas, deseos/versiones y transición de bandas en arnés aislado. Puerta: 300/500/750 fijo/móvil/disperso y todos cercanos con 3000, sin inanición ni deuda creciente. No integrar si solo pasa concentrado. |
| H3 — consumidor físico QA | Adaptador de pruebas separado del backend aprobado, que use los mismos contratos físicos y no modifique `EnemyNavigation`. Jefe, colas, escalada, llegada y regresiones A/B/C. Si requiere extraer/modificar producción, detenerse y pedir autorización de ese cambio concreto. |
| H4 — build QA pequeña y revisión | Solo tras autorizar el arnés: build separada, FPS reales, perfiles y 10 min; repetir control y protecciones. Entregar resultados positivos y negativos. Integración normal y D4/D5 siguen excluidos aun si pasa. |

Cada fase: commit pequeño en español, evidencias, checkpoint y push normal. Si falla una puerta, documentar causa y detener ese candidato para revisión; no cambiar de alternativa, subir presupuesto, relajar física o ampliar alcance automáticamente. H3/H4 prueban cuerpos en grafo residente, **no** implementan descarga, streaming o prefeteo D4. Una aprobación del documento debe indicar hasta qué fase autoriza trabajar.

## 10. Decisiones pendientes del autor

| Decisión | Recomendación para aprobar o corregir |
| --- | --- |
| Arquitectura y precisión | A; preservar alcance y física exactos, aceptar coste global no óptimo y los límites de rodeo de corpus 1,5× p95 / 2× máximo. No prometer óptimo global para el jefe. |
| Distancias y frescura | 24/32 m cercano y 48/60 m intermedio; jefe siempre crítico. Cercano/crítico 0,3/0,5 s; intermedio 0,6/1 s; lejos 1,5/2 s. Registrar también cumplimiento de objetivos originales. |
| Prioridad y saturación | Aprobar la cuota inicial de sección 5 como hipótesis de ensayo, prioridad de cálculo del jefe sin prioridad física para atravesar colas, y fallo explícito si 750 cercanos no caben. |
| Calidad de persecución | Aprobar criterios de llegada, rodeo y colas de sección 8; distinguir aproximación a meta inaccesible de llegada real, y progreso móvil de captura imposible. |
| Alcance de la siguiente autorización | Recomiendo autorizar primero **H0–H1**, revisar coste/alcance/rodeos antes de H2; H3–H4 y cualquier cambio de producción requieren decisión posterior explícita. No D4/D5. |

No necesitan nueva decisión los siete ajustes protegidos, web congelada, S/M/L, física/escalada libre, 3000, ausencia de teleports ni exclusión de integración normal: se conservan. Tamaño de región, límites de arrays y desempates son opciones técnicas a contrastar dentro de la fase autorizada, sin solicitar aprobación para cada constante.

**Siguiente paso: revisión del autor y aprobación/corrección de esta tabla. No programar por el hecho de haber publicado el plan.**
