# P0-B — rutas de enemigos en altura: plan propuesto

Estado: **PROPUESTA PENDIENTE DE APROBACIÓN DEL AUTOR** (11/10/2026, Claude Code). No programar nada de P0-B hasta que el autor apruebe este plan y responda a las dudas de la sección 7. P0-C/D no autorizados.

## 1. Diagnóstico del código real

- `Enemies.Step` (`Core/Enemies.cs`) persigue en línea recta en XZ hacia el jugador, con separación por `SpatialGrid` (celda 4 m), un desvío giratorio de 1,4 s cuando `PushOut` lo bloquea más de 0,35 s, y altura `Y = world.Height(x, z, Y+0,6)`. Es una réplica fiel de la web: las pruebas de equivalencia U2/U3 y las referencias congeladas dependen de este comportamiento exacto.
- **Los enemigos no buscan rutas.** Si el camino recto está tapado, se quedan pegados o desvían a ciegas. Por eso el tejado sin acceso da 30 s sin daño (sonda A4). La rampa oeste solo funciona porque está en la línea recta (A4: alcanza Y=4,25 a los 14,45 s).
- **Los enemigos suben cualquier pendiente del terreno.** `WorldCollision.GroundHeight` toma `Heightfield.HeightAt` sin límite de pendiente, y `PushOut` solo considera colliders. En el circuito P0, las paredes de la meseta (terreno a ~76°) son caminables para la horda. Equivale a una escalada no autorizada y hay que fijarlo con una prueba en B0.
- Las cajas sólidas bloquean a los enemigos salvo las de 0,6 m o menos, que suben como escalón (el jugador sube 0,45 m). Las tapas (`Standable`) son suelo si se llega a ellas desde una altura de un escalón.
- Combate vertical heredado (sondas A4):
  - El contacto ya filtra la altura (|dy| < 1,6).
  - Barra filtra (|dy| > 1,8). Naftalina y Jersey no filtran: dañan a 6 m de diferencia.
  - El proyectil amistoso impacta solo en XZ, sin altura.
  - El proyectil hostil vuela a terreno+1, atraviesa sólidos y nunca alcanza un tejado.
  - El charco de fregona sí respeta la altura.
- La simulación es centralizada, en arrays SoA (sin componentes por enemigo), con hasta 800 enemigos y el núcleo en C# puro sin UnityEngine. La escalada P0 ya es optativa mediante un gancho (`WorldRun.PhysicsStep`): sin él, U3 queda intacto.
- Tamaños: el circuito P0 mide 80 m. El mundo U3 tiene terreno de 320 m y radio jugable de 136 m.

## 2. Arquitectura propuesta

**Recomendada: campo de flujo compartido sobre un grafo de navegación multinivel (2,5D).**

1. **Grafo estático por mundo** (núcleo puro), construido una vez a partir del Heightfield y de los colliders:
   - Rejilla de 0,5 m. Cada celda puede tener varias capas transitables: suelo del terreno y tapas de sólidos (tejados, plataformas).
   - Una capa es transitable si cabe un cuerpo de 1,6 m, la pendiente es ≤ 48° (el mismo límite que el jugador) y no está dentro de un sólido.
   - Hay arista entre capas vecinas (8 direcciones) si el salto de altura es ≤ 0,6 m (el escalón actual de los enemigos) y no hay obstáculo en medio.
   - Rampas, escaleras y caminos físicos salen solos de estas reglas, sin listas de IDs. Así acepta sin cambios las geometrías conectadas de P0-C.
   - Componentes conexas precalculadas, para saber al instante si el jugador es alcanzable.
2. **Un solo campo de distancias** (Dijkstra) con el jugador como meta, compartido por toda la horda:
   - Se recalcula cuando el jugador cambia de nodo o de capa, con un límite de frecuencia.
   - El cálculo puede repartirse entre ticks, con doble búfer, sin pico de CPU.
   - Se limita a una ventana alrededor del jugador (por ejemplo, 96×96 m), de modo que el coste no crece con el mundo.
   - Sin RNG y con orden de vecinos fijo: es determinista.
3. **Cada enemigo lee la dirección de su celda** (O(1), sin buscar rutas propias). Esa dirección sustituye solo a la persecución recta. Se conservan la separación, el desvío, los comportamientos de distancia y carga y la `SpatialGrid`. Cerca del jugador y con línea libre, persecución directa como ahora.
4. **Un adaptador P0 del mundo de combate** impide a los enemigos subir pendientes de más de 48° y saltos mayores que el escalón. Deslizan como contra una pared: sin escalada, sin teletransporte, sin atravesar nada.
5. **Activación optativa:** el guía es nulo en U3 y la entrega U6, así que el comportamiento allí es idéntico byte a byte y las referencias siguen intactas. Solo se activa en el circuito QA P0.

Alternativas consideradas:
- **Grafo de sectores con A\* jerárquico y rutas cacheadas:** escala mejor a mundos enormes, pero es más complejo. Queda como evolución para P0-D si la ventana local no basta.
- **NavMesh de Unity solo para consultas (`CalculatePath`):** ata la lógica al motor, rompe el núcleo puro y las pruebas EditMode deterministas, y es difícil de compartir entre 750 enemigos. Descartada.
- **A\* por enemigo con caché:** prohibido por las restricciones y con coste lineal en enemigos. Descartada.

## 3. Archivos y sistemas afectados

- Nuevos en `Core/World/`: `NavGraph.cs` (construcción y consultas), `FlowField.cs` (campo compartido, caché y presupuesto por tick), `NavCombatWorld.cs` (adaptador con límites de pendiente y escalón).
- `Core/Enemies.cs`: un gancho optativo de dirección (`IEnemyGuide`, nulo por defecto), unas pocas líneas sin tocar el camino U3.
- `Core/World/VerticalCircuit.cs` o un circuito B nuevo (ver duda 5): escenarios con tejado y rampa, acceso por el lado contrario, dos rampas, ruta bloqueada, pasillos y un refugio inaccesible.
- `U3/VerticalQa.cs` y `U3/P0Capture.cs`: activar el guía en P0, métricas de navegación, escenarios del benchmark y evidencia.
- Pruebas nuevas en `Tests/Core` (grafo, campo, escenarios y determinismo) y en `Tests/PlayMode` (escena P0 con horda).
- Sin cambios en `src/`, referencias, catálogo, RNG, director, recompensas, guardado, controles ni escalada A6.

## 4. Subfases verificables

| Fase | Contenido | Verificación |
| --- | --- | --- |
| B0 | Fijar el diagnóstico sin cambiar nada: pruebas de pendiente de terreno caminable, tejado inaccesible y acceso alternativo no encontrado; arnés de métricas de navegación sobre el steering actual (línea base). | Pruebas que documentan el estado actual; benchmark base 0/300/500/750. |
| B1 | Grafo multinivel en el núcleo: capas, pendiente, cuerpo, escalón, conexiones y componentes. Coste de construcción y memoria medidos. | Fixtures de rampa, tejado, escalera, pasillo, foso y pendiente; mismas reglas que la colisión. |
| B2 | Campo de flujo compartido, caché, recálculo al cambiar de nodo o capa, reparto entre ticks y política de inaccesible. | Escenarios QA 1–6 y 8 en lógica pura; determinismo con semilla; cero asignaciones por tick. |
| B3 | Integración optativa en `Enemies.Step` y adaptador del mundo; U3 idéntico. | Suites completas y referencias U0–U4; equivalencia U2/U3 intacta; escenarios con horda real. |
| B4 | Escenarios en la build QA, grupos desde varios sectores, esquinas y pasillos; métricas, benchmark y evidencia. | Escenarios 7, 9 y 10; comparación con B0. |
| B5 | Combate vertical, según la duda 4. | Sondas A4 actualizadas con el resultado esperado nuevo. |
| B6 | Cierre: builds P0, documentación y guía de prueba manual; detenerse. | — |

## 5. Escenarios QA y métricas

Los diez escenarios pedidos tendrán una prueba EditMode determinista cada uno:
1. Mismo nivel.
2. Tejado con rampa directa.
3. Plataforma accesible por el lado contrario.
4. Dos rampas: se elige la de menor coste.
5. Ruta bloqueada: se elige la alternativa o se aplica la política de inaccesible.
6. Suelo ↔ tejado, con recálculo.
7. Grupos desde cuatro sectores.
8. Altura inaccesible: ni contacto ni subida de pendientes.
9. Esquinas, obstáculos y pasillos.
10. 300, 500 y 750 enemigos, con semilla fija y repetición idéntica.

Métricas en P0Capture, en CSV y JSON:
- Porcentaje de enemigos con ruta válida.
- Tiempo hasta la altura del jugador (p50/p95/máximo).
- Enemigos atascados: sin avanzar más de 2 s teniendo ruta.
- Recálculos por minuto y ticks de reutilización del campo.
- CPU de navegación aparte de la CPU de simulación.
- Memoria y GC (bytes por frame solo en Development).
- Frame y tick p50/p95/p99/máximo con 0, 300, 500 y 750 enemigos, frente a la base de B0 en las mismas condiciones.

Son medidas de lógica del arnés, no FPS del juego; la GPU no se mide (N/D).

## 6. Presupuesto de rendimiento y riesgos

Referencia actual (benchmark `20261011T203140`): CPU de tick p50 de 1,66 ms y p95 de 1,79 ms con 750 enemigos.

| Concepto | Presupuesto |
| --- | --- |
| Lectura del campo por enemigo | ≤ 0,15 ms por tick con 750 |
| Recálculo del campo | ≤ 0,5 ms p95 por tick (repartido); pico ≤ 1 ms |
| Construcción del grafo | Única, al cargar; ≤ 100 ms en el circuito |
| Memoria | Arrays preasignados (circuito ≈ 26 000 celdas, unos pocos MB) |
| Asignaciones por tick | Cero |

Riesgos:
- **Enemigos grandes** (rata 0,9 m, remedios 3 m, jefe) en pasillos estrechos. Mitigación inicial: un solo campo para radio común; los grandes conservan el desvío actual. Un campo por clase de tamaño si hace falta.
- **Atascos en la boca de una rampa** con 750 enemigos: es aglomeración, no ruta inválida. Se mide aparte.
- **Coste del recálculo si el jugador cambia de nodo constantemente.** Mitigación: límite de frecuencia y reparto entre ticks.
- **Escala al mundo grande (P0-D):** la ventana local acota el coste; si no basta, sectores jerárquicos.

## 7. Dudas que necesitan decisión del autor

1. **Alcance:** ¿P0-B solo en el circuito QA, como A6, sin activarlo en U6 ni en el mundo procedural U3? Recomendado: sí. La integración en el juego normal llegaría con C/D.
2. **Jugador inaccesible:** ¿qué hacen los enemigos?
   - (a) Recomendado: agruparse en el punto alcanzable más cercano, visibles debajo, sin daño cuerpo a cuerpo, y registrarlo como refugio a corregir con geometría.
   - (b) Rodear o patrullar alrededor.
   - (c) Otra opción.
   En ningún caso escalan, se teletransportan ni hacen daño invisible.
3. **Pendientes del terreno:** hoy los enemigos suben cualquier pendiente. ¿Se limita en modo P0 a 48°, igual que el jugador? Recomendado: sí. Es lo que evita una escalada enemiga encubierta.
4. **Combate vertical:** ¿se incluye en B5, solo en modo P0 y sin tocar daño, cadencia ni alcance?
   - Una banda vertical para Naftalina, Jersey y el proyectil amistoso, como ya hacen Barra y el charco.
   - Que el proyectil hostil choque con sólidos.
   - Que el proyectil hostil apunte a la altura del jugador. Este punto cambia la amenaza a distancia, así que decide si entra en B5 o se deja fuera.
5. **Escenarios:**
   - (a) Recomendado: un segundo circuito QA "B" en la misma build P0, seleccionable con una tecla, que deja intactos el circuito y los fixtures aprobados de A6.
   - (b) Ampliar el circuito actual.
6. **Criterio de ruta:** menor coste de recorrido (distancia más una penalización por subir), elegido de forma determinista. Repartir la horda entre varias rampas sería una mejora posterior. ¿De acuerdo?

## 8. Criterios de terminado

- En los escenarios alcanzables, el 100 % de los enemigos tiene ruta válida tras el primer cálculo del campo. Al menos el 95 % alcanza la altura del jugador en ≤ 1,5 × (longitud de la ruta / velocidad) + 3 s.
- Cero penetraciones, teletransportes y subidas de pendientes de más de 48°.
- Atascados con ruta válida ≤ 2 % con 750 enemigos.
- Con el jugador inaccesible: cero daño cuerpo a cuerpo y comportamiento según la duda 2.
- Cambio suelo ↔ tejado: el campo se recalcula en ≤ 0,25 s y los enemigos cambian de objetivo.
- Rendimiento dentro de la sección 6, con cero asignaciones por tick en navegación.
- U3 y U6 idénticos: suites completas, referencias U0–U4 y equivalencias. Escalada A6 sin cambios. Build P0 separada. U6 y los siete ajustes protegidos intactos.
- Prueba manual del autor en la build P0.
