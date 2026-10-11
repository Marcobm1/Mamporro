# P0-B — navegación vertical y escalada de todos los enemigos: plan revisado

Estado: **PLAN REVISADO, PENDIENTE DE APROBACIÓN DEL AUTOR** (11/10/2026, Claude Code). Sustituye la propuesta inicial de `89ee76d`, preparada antes de que el autor confirmara la escalada de todos los enemigos. No programar nada de P0-B hasta que el autor apruebe este plan y responda a la sección 10. P0-C/D no autorizados.

## 0. Decisiones ya confirmadas por el autor (no volver a preguntar)

- **Todos los enemigos escalan:** cuerpo a cuerpo, a distancia, élite, jefe y especiales. Ningún tejado debe ser seguro solo porque los enemigos no sepan escalar.
- Las paredes escalables se detectan con reglas geométricas compatibles con las del jugador, sin listas de IDs.
- El límite de 48° es la referencia para caminar; las superficies más inclinadas requieren escalada.
- Corrección geométrica del combate vertical, incluidos los proyectiles hostiles dirigidos a la altura del jugador cuando haya trayectoria válida, sin seguimiento mágico ni cambios de daño, alcance, cadencia, velocidad ni balance.
- Jugador realmente inaccesible: los enemigos van al punto alcanzable más cercano, sin atravesar nada ni empujar indefinidamente; los de distancia disparan solo con trayectoria válida; el caso se registra para P0-C.
- P0-B es optativo y queda aislado en QA durante su desarrollo. Habrá un segundo circuito QA seleccionable con una tecla, con el circuito activo visible.
- Nada de NavMeshAgent, Rigidbody, Update, Animator ni GameObject por enemigo; nada de A\* por enemigo y tick; nada de teletransportes correctivos.

## 1. Diagnóstico del código real (11/10/2026)

- **Movimiento** (`Core/Enemies.cs`):
  - Persecución recta en XZ, separación con `SpatialGrid` (celdas de 4 m, sin mirar Y) y desvío giratorio de 1,4 s tras 0,35 s bloqueado.
  - Altura `Y = world.Height(x, z, Y+0,6)`, sin velocidad vertical: al salir de un borde, el enemigo baja al instante (un salto de altura) y sube cualquier pendiente del terreno, porque `GroundHeight` no limita la pendiente.
  - Las cajas bloquean salvo las de 0,6 m o menos, que funcionan como escalón.
  - Este código es la réplica de la web que fijan las referencias U2/U3, así que hay que conservarlo intacto fuera de P0.
- **Velocidad efectiva actual:** `Speed[i] = def.speed × variación (0,9–1,1, por id) × (1 − Slow[i])` durante `SlowTime`. La carga usa `DashSpeed`. El jefe acelera sus fases ×1,5 cuando está enfurecido, no su desplazamiento.
- **Catálogo real:**

  | Enemigo | Papel | Radio | Altura | Velocidad | Masa |
  | --- | --- | --- | --- | --- | --- |
  | pelusa | común | 0,5 m | 0,95 m | 4,2 m/s | 1 |
  | cucaracha | común | 0,42 m | 0,5 m | 7,2 m/s | 0,7 |
  | taper | común | 0,8 m | 1,0 m | 2,6 m/s | 4 |
  | paloma | a distancia (preferida y alcance propios) | 0,45 m | 0,8 m | 5,0 m/s | 0,8 |
  | rata | élite, carga | 0,9 m | 1,5 m | 5,2 m/s | 6 |
  | pelusaMadre | jefe; fases idle/windup/attack/recover: rodar 18 m/s, golpe de 5,5 m, estornudo con 16–24 proyectiles y 5 esbirros | 2,4 m | 4,4 m | 3,4 m/s | 50 |

- **Combate vertical:**
  - El contacto cuerpo a cuerpo filtra |dy| < 1,6 (más margen para los altos). Barra y el charco filtran altura.
  - Naftalina, Jersey y el proyectil amistoso no la filtran (sondas A4).
  - Los disparos de la paloma y el estornudo del jefe salen a `terreno + 1`, vuelan rectos en XZ, atraviesan sólidos y comparan la altura con el terreno, no con la del tirador.
  - La paloma empieza a apuntar si dy < 3 y la rata carga si dy < 2.
- **Reutilizable de A6:**
  - `VerticalQueries`: cajas y triángulos abruptos del Heightfield, contrato por exclusión de categorías, `WallMinSlopeDegrees` = 48, barridos continuos y `Clear`, `Ledge` con cuerpo completo, apoyo y dos tramos.
  - `TerrainWallGeometry` y el patrón de gancho optativo de `WorldRun.PhysicsStep`.
  - Todo admite radio y altura como parámetros, así que sirve para cuerpos de enemigo.

## 2. Arquitectura propuesta

**Un grafo de navegación multinivel con aristas de caminar, escalar y caer, más un campo de distancias compartido por cada clase de tamaño.** Cada enemigo solo lee su celda (O(1)) y ejecuta una pequeña máquina de estados física centralizada en arrays.

### 2.1 Detección de paredes escalables
Las mismas reglas que el jugador: `VerticalQueries.Wall` o `IsWallNormal`, es decir, normal con pendiente de más de 48° y sin cara inferior, sobre cajas y triángulos del terreno. Las categorías de follaje, interactuable, límite y enemigo quedan excluidas. Sin listas de IDs.

### 2.2 Grafo (construcción única por mundo y clase de tamaño)
- **Nodos de superficie:** rejilla de 0,5 m con capas por altura en cada celda (suelo del terreno y tapas de sólidos). Un nodo existe si cabe el cuerpo de la clase (radio y altura), la pendiente es ≤ 48° y no está dentro de un sólido. Así se distinguen superficies próximas pero desconectadas, como el suelo bajo un tejado y el propio tejado.
- **Aristas de caminar:** entre nodos vecinos en 8 direcciones si el salto es ≤ 0,6 m (el escalón actual de los enemigos) y el barrido del cuerpo está libre. Rampas, escaleras y pendientes transitables salen de aquí.
- **Aristas de escalar** (una sola dirección, hacia arriba):
  - Van de un nodo de pie de pared a un nodo de cima.
  - Hace falta que la cara exista sin huecos en toda la altura, comprobado con `Wall` en tramos de 0,075 m como en A6.
  - La columna del cuerpo delante de la cara tiene que estar libre (techo o voladizo invalidan la arista).
  - La cima tiene que pasar `Ledge`: cuerpo completo, apoyo y dos tramos barridos.
  - Coste = altura de escalada más un pequeño coste de transición.
  - Una pared con techo encima o una cima demasiado estrecha para la clase no genera arista.
- **Aristas de caer** (una sola dirección, hacia abajo): de un borde a la superficie de debajo, si la caída está libre de sólidos.
- **Componentes conexas dirigidas,** precalculadas para saber al instante si la meta es alcanzable desde cada nodo.
- **Clases de tamaño**, para no meter cuerpos en huecos donde no caben:
  - S (radio ≤ 0,5): pelusa, cucaracha y paloma.
  - M (≤ 0,9): taper y rata.
  - L (≤ 2,4): jefe.
  Solo se construyen y actualizan las clases que tengan enemigos vivos. El grafo L es mucho más pequeño porque casi ninguna superficie admite un cuerpo de 4,8 m.

### 2.3 Campo compartido, costes y destinos
- **Dijkstra inverso** desde el nodo del jugador por cada clase activa, con cola de cubetas sobre costes enteros: lineal en nodos y sin asignaciones.
- **Meta:**
  - Si el jugador está en el suelo, su nodo.
  - Si escala, el pie o la cima más cercanos.
  - Si está en el aire, el último nodo en el que estuvo apoyado.
- **Se recalcula** cuando el jugador cambia de nodo o capa, con un máximo de 4 por segundo y clase, en doble búfer y repartido entre ticks según el presupuesto. Los enemigos usan el campo vigente mientras se calcula el siguiente.
- **Ventana alrededor del jugador** (por ejemplo, 96×96 m): el coste no crece con el tamaño del mundo, que es lo que necesitará P0-D.
- **Inaccesible:** si el nodo del enemigo no llega a la meta, su destino es el nodo alcanzable de su componente más cercano al jugador. Se precalcula en cada recálculo y los enemigos se detienen allí, sin empujar.
- **Coste coherente:** caminar y escalar cuestan lo que miden (ver la duda 1 sobre velocidad). Entre una rampa y una pared con coste casi igual decide un desempate determinista. El orden de vecinos es fijo y no se usa RNG.

### 2.4 Estados físicos del enemigo (solo P0, arrays SoA paralelos)
- `Caminar`: la dirección del campo sustituye a la persecución recta. Se conservan separación (filtrada por altura), comportamiento de distancia y carga, y desvío. No se suben pendientes de más de 48° ni escalones de más de 0,6 m: el adaptador P0 del mundo de combate los trata como pared.
- `Escalar`: entra cuando la mejor arista de su nodo es de escalar y está frente a la cara. Sube con barrido continuo por tramos (como A6) a la velocidad acordada. Se separa lateralmente de otros escaladores de la misma cara, sin trepar sobre ellos.
- `Borde`: los dos tramos barridos de `Ledge` (ascenso y avance), sin teletransporte. Termina en `Caminar`.
- `Caer`: gravedad real al seguir una arista de caer, al soltarse o por empuje. Sustituye la bajada instantánea solo en P0.
- `Recuperar`: si pierde la arista (cara agotada, cuerpo bloqueado o meta cambiada), vuelve a leer el campo desde su posición. Sin teletransporte.

### 2.5 Integración centralizada
- `Enemies.Step` recibe un gancho optativo (`IEnemyNavigation`, nulo en U3 y U6). Con el gancho, el movimiento de cada enemigo pasa por un método P0 aparte que comparte `Think`, contacto, empuje y presión.
- Sin gancho, el código U3 es literalmente el mismo, y las referencias y equivalencias lo verifican.
- Los arrays P0 (estado, velocidad vertical, arista, cara y temporizadores) viven en el sistema de navegación, indexados por hueco, y siguen el intercambio de `Remove` o `Clear` mediante una notificación.

### 2.6 Comportamiento de cada tipo
- **Comunes:** su velocidad, agilidad, masa y separación de siempre.
- **Paloma:** se queda a su distancia preferida si tiene trayectoria válida al jugador. Si no la tiene, sigue el campo (también escalando) hasta conseguirla.
- **Rata:** carga solo en `Caminar`, con línea transitable recta comprobada en el grafo. Si no, persigue por el campo.
- **Jefe:** escala si su cuerpo cabe (clase L). Sus fases siguen el reloj; ver la duda 3. Rodar solo en `Caminar` sobre la misma superficie.
- **Esbirros:** se crean en nodos válidos del grafo S cercanos, sin aparecer dentro de sólidos.

### 2.7 Alternativas descartadas
- **NavMesh de Unity con `CalculatePath`:** ata la lógica al motor, rompe el núcleo puro y las pruebas EditMode deterministas, y no modela escalada.
- **A\* por enemigo:** prohibido y de coste lineal en enemigos.
- **Sectores jerárquicos:** quedan como evolución para P0-D si la ventana local no basta.

## 3. Combate vertical (B5)

- **Contacto:** solo desde `Caminar` o `Borde` terminado, con la banda vertical actual. Un enemigo que escala no golpea; tiene que llegar arriba.
- **Naftalina y Jersey:** solapamiento vertical entre el volumen del arma y el cuerpo del enemigo (Y a Y + altura), como Barra y el charco. Daño, área y cadencia sin cambios.
- **Proyectil amistoso:** impacto 3D (altura del proyectil frente a la extensión del enemigo). Se detiene contra sólidos.
- **Proyectiles hostiles (paloma y estornudo):**
  - Salen de la altura real del tirador (Y + altura × 0,6, el punto que ya usa el aviso visual).
  - Van en línea recta en 3D hacia el centro del jugador en el momento del disparo, sin corrección posterior.
  - Chocan con sólidos y terreno mediante barrido de segmento.
  - Impacto 3D. Velocidad, alcance, radio y daño sin cambios.
  - El estornudo conserva sus direcciones radiales horizontales, ahora a la altura del jefe.
- **Línea de visión para empezar a disparar:** segmento entre tirador y jugador sin sólidos (`VerticalQueries.Sweep` con radio de proyectil). Sustituye al filtro dy < 3 de la paloma solo en P0.
- **Estados distinguidos** en métricas y pruebas: alcanzable físicamente, con línea de visión, bloqueado por geometría, y escalando todavía sin posición de ataque.

## 4. Archivos y sistemas afectados

- Nuevos en `Core/World/`:
  - `NavGraph.cs`: construcción y consultas, por clase.
  - `NavField.cs`: campo compartido, caché y presupuesto por tick.
  - `EnemyNavigation.cs`: estados físicos y adaptador del mundo.
  - `NavCircuit.cs`: circuito QA B.
- `Core/Enemies.cs`: gancho optativo y notificación de `Remove` o `Clear`.
- `Core/CombatRun.cs`, `Core/Projectiles.cs` y `Core/WeaponBehaviors.cs`: ramas P0 de combate vertical, activas solo con el modo P0.
- `Core/Boss.cs`: sus ataques usan la altura real en P0.
- `U3/VerticalQa.cs`, `U3/P0Capture.cs` y `U3/U3Game.cs`: F10 para alternar entre el circuito A6 y el B (visible en el HUD), activación del modo P0-B, métricas, benchmark y evidencia.
- Pruebas nuevas en `Tests/Core` (grafo, campo, estados, combate, escenarios y determinismo) y `Tests/PlayMode` (escena con el circuito B y horda).
- Sin cambios en `src/`, referencias, catálogo, IDs, RNG, director, recompensas, guardado, controles ni escalada A6 del jugador.

## 5. Fases

| Fase | Contenido | Verificación |
| --- | --- | --- |
| B0 | Línea base sin cambiar nada: pruebas que fijan la persecución actual (subida de pendientes del terreno, bajada instantánea, tejado sin ruta, combate XZ) y métricas de navegación sobre el steering actual. | Benchmark base 0/300/500/750 en el circuito B. |
| B1 | Grafo S/M/L con aristas de caminar, escalar y caer y componentes conexas. Coste de construcción y memoria. | Fixtures: rampa, escalera, pared, pared con techo, cima estrecha, acantilado del terreno, meseta, hueco donde L no cabe, superficies próximas desconectadas, límite, follaje e interactuable. |
| B2 | Campo compartido, costes, desempate, ventana, recálculo y reparto entre ticks, destino inaccesible. | Rutas esperadas en todos los escenarios; determinismo con repetición idéntica; cero asignaciones por tick. |
| B3 | Estados físicos e integración en todos los tipos, incluidos jefe y esbirros; separación por altura; U3 intacto. | Suites completas y referencias U0–U4; enemigos reales escalando sin penetrar, con barrido por tick en las pruebas. |
| B4 | Circuito B (F10) con los 17 escenarios, métricas, benchmark y evidencia. | Criterios de la sección 8. |
| B5 | Combate vertical (sección 3). | Sondas A4 actualizadas con su nuevo resultado esperado, más casos de línea de visión, muro y techo. |
| B6 | Regresiones completas, documentación, builds P0, lanzador y guía de prueba manual; detenerse. | — |

## 6. Circuito QA B y pruebas

Escenarios del circuito B, todos deterministas con semilla fija y cada uno con su prueba EditMode:
1. Suelo.
2. Tejado con rampa.
3. Tejado al que solo se llega escalando.
4. Edificio con varias paredes escalables.
5. Acantilado del terreno sin rampa.
6. Meseta de paredes abruptas.
7. Rampa frente a escalada.
8. Dos paredes con recorridos distintos.
9. Subir y bajar repetidamente.
10. Palomas escalando.
11. Rata y jefe escalando.
12. Tamaños distintos en transiciones, incluido un hueco donde L no cabe.
13. Esquinas y obstáculos.
14. Pared con techo encima.
15. Zona inaccesible incluso escalando (pilar con voladizo).
16. Grupos desde cuatro sectores.
17. 300, 500 y 750 enemigos.

Pruebas negativas: límite invisible, techos, paredes sin salida, conexiones falsas (superficies próximas sin continuidad), superficies excluidas, y un enemigo que no golpea mientras escala. Las pruebas de A6 no se tocan.

## 7. Métricas y rendimiento

Métricas en P0Capture (CSV y JSON por escenario y carga):
- Construcción del grafo por clase (ms y memoria).
- Recálculos del campo, ticks de reutilización y ms por recálculo.
- Consulta por enemigo.
- Transiciones a escalar y a borde.
- Atascados: sin avanzar más de 2 s teniendo ruta.
- Rutas incorrectas: enemigo fuera del campo o con distancia creciente de forma sostenida.
- Tiempo hasta alcanzar al jugador, tiempo de escalada y tiempo de borde (p50/p95/máximo).
- CPU de navegación aparte de la de simulación.
- Memoria y GC (bytes por frame solo en Development).
- Frame y tick p50/p95/p99/máximo con 0, 300, 500 y 750 enemigos.

Son medidas de lógica del arnés, no FPS del juego; la GPU no se mide (N/D). Se comparan con la base de B0 en las mismas condiciones y el mismo equipo.

Presupuesto inicial. Son objetivos, no resultados; la referencia histórica es 1,66 ms p50 de tick con 750 enemigos (`20261011T203140`).

| Concepto | Presupuesto |
| --- | --- |
| Navegación total | ≤ 0,5 ms p95 por tick con 750 enemigos (campos repartidos más consultas) |
| Pico | ≤ 1 ms |
| Construcción del grafo | Única al cargar; ≤ 150 ms para las tres clases en el circuito |
| Asignaciones por tick | Cero |
| Memoria | Arrays preasignados; estimación de 5–15 MB en el circuito, a medir |

Si no se cumple, se informa de la causa con datos y se propone una corrección (rejilla de 1 m para L, ventana menor, frecuencia de recálculo), sin ocultar mediciones.

Riesgos principales:
- **Aglomeración en paredes y bocas de rampa con 750 enemigos.** Se mide aparte de las rutas inválidas; mitigación con separación a lo largo de la cara.
- **Coste de tres campos.** Mitigación: solo las clases vivas; L y M son pequeños.
- **Jefe en espacios estrechos:** sin arista no escala; va al punto alcanzable.
- **Empuje del arma sobre escaladores** (ver duda 4).
- **Terreno procedural de P0-C/D:** cubierto por la construcción genérica y la ventana local, pero sin verificar hasta entonces.

## 8. Criterios de terminado

- En los escenarios físicamente alcanzables, al menos el 95 % de los enemigos llega a la altura del jugador y a contacto o posición de tiro en ≤ 1,5 × (coste de la ruta / velocidad de su tipo) + 3 s, medido de forma reproducible.
- Cero penetraciones (`Clear` del cuerpo en cada tick de prueba), cero teletransportes (desplazamiento por tick ≤ velocidad × dt más empuje) y cero subidas de pendientes de más de 48° caminando.
- Atascados con ruta válida ≤ 2 % con 750 enemigos en los escenarios adecuados.
- Zona inaccesible: cero daño cuerpo a cuerpo; enemigos detenidos en el punto alcanzable más cercano; disparos solo con trayectoria válida.
- Cambio suelo ↔ tejado: el campo se actualiza en ≤ 0,25 s.
- Rendimiento dentro de la sección 7, o desviación explicada con una propuesta.
- U3 y U6 idénticos (suites, referencias y equivalencias); escalada A6 del jugador sin cambios; build P0 separada; U6 y los siete ajustes protegidos intactos.
- Prueba manual del autor.

## 9. Lo que no entra

P0-C (geometría procedural conectada), P0-D (mundo ampliado), activar P0-B en U3 o U6, balance, director, recompensas, nuevos enemigos, arte, Blender, Steam ni publicación.

## 10. Decisiones de diseño realmente pendientes

1. **Velocidad de escalada enemiga.**
   - Recomendado: igual a la velocidad efectiva de cada enemigo sobre el suelo (`Speed[i] × (1 − Slow)`), la misma regla que el jugador, sin multiplicador nuevo. Conserva la identidad de cada uno: cucaracha 7,2 m/s, taper 2,6 m/s, jefe 3,4 m/s.
   - Alternativa: un factor global de escalada (por ejemplo, ×0,6). Daría al jugador más margen vertical, pero es un ajuste de balance que necesita tu cifra.
2. **Disparar mientras escalan.** Recomendado: la paloma no dispara mientras escala; dispara al llegar a una superficie con línea de visión. Alternativa: disparo desde la pared con línea de visión.
3. **Jefe durante la escalada.** Recomendado: sus temporizadores siguen, pero los ataques que tocan durante la escalada esperan a que vuelva a caminar. Rodar solo en la misma superficie.
4. **Empuje del jugador sobre un enemigo que escala.** Recomendado: un empuje hacia fuera de la pared por encima de un umbral lo suelta y cae con gravedad, sin daño de caída. Da al jugador una herramienta vertical. Alternativa: el empuje no afecta a los escaladores.
5. **Bajar de alturas.** Recomendado: los enemigos bajan cayendo con gravedad desde los bordes (aristas de caer), sin daño de caída, como el jugador puede caer. Alternativa: bajar escalando, más lento.
