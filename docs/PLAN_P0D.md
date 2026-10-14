# P0-D — ampliación del mundo y navegación regional

**Plan aprobado por el autor el 12/10/2026: ocho decisiones resueltas; implementación autorizada exclusivamente D0–D4. Detenerse antes de D5.** P0-C aprobado manualmente; cierre formal `b755987`, cierre técnico `369fdff`, código/builds QA `9a24b6f`. Mantener 3000 nodos/tick. No reemplazar U3, ampliar mapas ni generar una build final D5–D6 con esta autorización.

Las cifras de C son mediciones heredadas de Claude, contrastadas con documentos, XML e informes locales; no se han repetido pruebas. Las cifras para D son estimaciones u objetivos propuestos, no resultados ni compromisos aprobados. Investigación limitada al repositorio y evidencias existentes; sin Deep Research.

## 1. Diagnóstico técnico de P0-C y U3

### Geometría y escala reales

| Propiedad | U3 histórico | Circuito C aprobado |
| --- | --- | --- |
| Terreno | 320 × 320 m, 256² celdas de 1,25 m | 128 × 128 m, celdas de 1 m |
| Límite horizontal | Squircle de potencia 5, radio 136 m | Misma forma, radio 60 m |
| Diámetro por los ejes | 272 m | 120 m |
| Área horizontal dentro del límite | ~70 296 m² | ~13 682 m² |
| Relieve | Ruido, relieve nominal 26 m, terrazas de 4,5 m; borde montañoso adicional | Niveles 0/4,5/9/13,5 m, macroceldas de 4 m, mesetas de 8–40 m |
| Lugares | Solicitudes de 5 casas, 2 templos, 2 granjas, 3 pozos | Solicitudes de 1–2 templos/granjas, 4–6 casas, 1–3 bloques, 3–5 torres, 4–7 muros, 2–3 pozos; no son cantidades finales garantizadas |
| Interactuables | 14 cofres, 3 santuarios, 2 tótems, 1 portal; reglas jugables | Marcadores QA con esas cantidades, distancia mínima al inicio limitada a 40 m |
| Acceso | Sin validación vertical P0; tejados U3 decorativos y colliders rotados | Grafos S/M/L validan entrada/salida, tejados físicos AABB y paredes escalables |

Área calculada de la planta ideal: `4 Γ(1,2)² / Γ(1,4) · límite² ≈ 3,80060 · límite²`. No resta obstáculos, margen corporal ni zonas inaccesibles: medir área transitable por clase, superficie elevada y área visible por separado. No confundir lado del heightfield con superficie jugable. La fracción elevada de C se calibra sobre macroceldas elegibles, excluyendo inicio y franja perimetral; no garantiza literalmente 35 % de toda la planta.

Fuentes: `Core/Catalog.cs`, `World/Heightfield.cs` (`WorldMath.Squircle`), `WorldCollision.cs` (`WorldData.Generate` e `InteractableSpots`), `Sites.cs`, `ProceduralLayout.cs`, `ProceduralStructures.cs`, `ProceduralValidation.cs`. U3 es una referencia congelada, no el terreno sobre el que activar automáticamente navegación C.

A 9,5 m/s, cruzar C por un eje exige al menos 12,6 s; U3, 28,6 s. Son distancias rectas sin aceleración, obstáculos, combate o subida; medir recorridos navegables y tiempos reales. Mantener solo las cantidades actuales al ampliar C reduciría mucho su densidad. Tampoco multiplicar cofres/recompensas por área: cambiaría la economía sin autorización. Los nuevos hitos QA deben ser geometría reconocible sin premios.

### Navegación, consultas y costes actuales

- `NavGraph`: tres grafos completos, rejilla de 0,5 m con capas; S radio/altura 0,5/0,95 m, M 0,9/1,5, L 2,4/4,4. Margen de celda 0,25 m, escalón 0,6 m. Aristas dirigidas Walk/Climb/Drop y trayectorias físicas. Su constructor fija extensión al heightfield entero: todavía no existe constructor regional.
- `NavField`: Dijkstra inverso compartido, cuatro arrays de distancia/padre y heap, doble búfer; publica al terminar. Intervalo mínimo 0,2 s; no garantiza cinco actualizaciones efectivas por segundo. Inicializa arrays completos sin fraccionar; el barrido de destinos sustitutos recorre todos los nodos. Extracciones obsoletas del heap y relajaciones no equivalen uno a uno al presupuesto de nodos: 3000 no es un límite de CPU garantizado.
- `EnemyNavigation.Begin`: presupuesto 3000 compartido, prioridad fija según población de clase; una clase puede consumir lo disponible antes de M/L. Termina la meta en curso antes de recalcular la nueva. Un jugador continuamente móvil requiere ensayos de antigüedad del campo, no solo saltos entre puntos donde permanece quieto.
- `VerticalQueries`: rejilla de sólidos de 4 m y tabla de terreno abrupto; consultas locales ya optimizadas en C. **`NavGraph.BuildNodes` todavía enumera todos los sólidos para buscar tapas en cada celda**. Mayor densidad puede romper una estimación simplemente proporcional al área. Las copias de consultas para construir S/M/L también tienen coste de memoria.
- `ProceduralMap`: valida grafos completos, elimina obstáculos/recoloca marcadores y reconstruye; rechaza terreno fallido. Mantener esta reparación global en un mapa grande multiplica el coste. El generador tiene cantidades y algunos límites fijos (arrays para cuatro niveles, bucle de cortes con tope 64, distancias de marcadores), no basta cambiar `Size`.
- Render: `WorldRenderer` crea una malla completa de terreno; niebla no significa que deje de dibujarse. `RunRenderer` y `ParticleRenderer` usan bounds de 340 m, números de daño 400 m. Ampliar sin revisar esos límites puede causar desapariciones o culling ineficiente. Particionar render es una necesidad técnica posible, no autorización de arte nuevo.
- `WorldSpawns`: normales a 26–38 m, reciclado a 85 m (exceptúa especiales). Esto no es navegación entre regiones. El QA D debe demostrar recorridos largos sin reciclado; una integración futura debe decidir explícitamente cómo coexistir con la política histórica, sin tocar director o balance por accidente.

Medido en C: ~101–102 mil nodos S+M+L; generación/validación/mapas ~0,97–2,09 s; navegación 750 p95 0,615/0,714/0,825 ms según semilla; memoria gestionada ~80 MB frente a 35 MB en B; Development ~11–12,3 KB/frame, ya ~11 KB sin enemigos. Esta diferencia no es una medición aislada de bytes de navegación. Lógica del arnés, no FPS. C tiene 693/693 Edit y 53/53 Play heredados; aprobación no implica presupuestos cumplidos.

## 2. Objetivos y exclusiones

Proponer un mundo sensiblemente mayor con lugares próximos y reconocibles, grandes estructuras y verticalidad transitable, manteniendo persecución física de 300/500/750 enemigos. Validar primero continuidad regional, costes y reacción en pequeño, después aumentar escala.

Alcance recomendado para la futura autorización: **circuito QA D aislado** y arquitectura reutilizable. La integración jugable se diseña aquí, pero queda tras una segunda decisión de adopción. Conservar A/B/C seleccionables y el backend completo como control de regresión.

Fuera: reemplazar `WorldData.Generate`, modificar `src/` o corpus, U6, balance/director/economía, inventario/guardados/IDs, nuevas reglas de combate, P1/P2, arte final, Steam, streaming infinito, destrucción dinámica, colisiones arbitrarias de meshes y rotación libre de estructuras. No introducir NavMeshAgent, Rigidbody, Update ni A* individual por enemigo.

## 3. Alternativas de tamaño del mundo

Propuesta comparativa: borde de terreno de 12 m fuera del límite; límite `lado/2 − 12`. Conservar además corredores interiores de borde. C usa otra proporción, por eso se dan áreas explícitas.

| Opción | Terreno / límite | Área nominal | Área frente a C / U3 | Cruce axial mínimo a 9,5 m/s | Ventajas y límites |
| --- | --- | --- | --- | --- | --- |
| A, ensayo intermedio | 256 m / 116 m | ~51 141 m² | ×3,74 / ×0,73 | 24,4 s | Salto claro desde C, riesgo menor; sigue siendo menor que U3 en planta |
| B, conservadora | 320 m / 148 m | ~83 248 m² | ×6,08 / ×1,18 | 31,2 s | Más espacio útil que U3, pero incremento de escala moderado |
| C, ampliación marcada | 384 m / 180 m | ~123 139 m² | ×9 / ×1,75 | 37,9 s | Aumento claro frente a U3; exige más control de densidad, memoria, carga y render |

**Recomendación pendiente:** validar arquitectura a 128 m, ensayo a 256 m y usar 384 m como candidato final QA solo si pasa los presupuestos. 320 m es alternativa si se prefiere menor riesgo. No se fija ninguno automáticamente ni se crean los tres como entregas obligatorias.

Estimación de advertencia si se escalara C completo sin regionalizar: trabajo de rejilla ×4/6,25/9; ~4–8,4 / 6,3–13,1 / 9–18,9 s extrapolando 1–2,1 s actuales. No es predicción fiable: más sólidos, reparación, portales y paralelismo pueden empeorarla. Usar 45 MB de diferencia B→C como aproximación grosera daría 180/281/405 MB incrementales, **no memoria medida del futuro grafo**. La arquitectura debe medirse antes de prometer ahorro.

Propuesta de densidad QA: nodos de interés geométrico separados por 35–55 m de **ruta real** (4–6 s ideales; objetivo observado 6–12 s con relieve), sin tramos obligatorios vacíos mayores de 15 s. Como punto de ensayo, 12–18 / 18–26 / 26–36 conjuntos reconocibles para A/B/C, con 1–3 estructuras cada uno, no cantidades de recompensas. Medir distancias p50/p95, zonas vacías y líneas de visión; no distribuir por cuadrícula uniforme.

Mesetas principales propuestas 24–64 m, pequeñas 8–24 m; estructuras de 12–24 m de huella, conjuntos de hasta 32–40 m, tejados amplios de 9–18 m de altura. Primera prueba conserva cuatro niveles de C (máximo 13,5 m); 18/22,5 m solo como variante posterior aprobada y después de eliminar la suposición de cuatro niveles en D. No escalar radios/velocidades de cuerpos ni engordar todo proporcionalmente.

Con 750 enemigos importa su concentración local, no solo la superficie total. Ensayar todos concentrados en una región y repartidos por todo el mapa: son extremos distintos de congestión y caché. Conservar 750 como carga QA, no nueva curva de población.

## 4. Alternativas de navegación regional

| Alternativa | Beneficio | Problema | Propuesta |
| --- | --- | --- | --- |
| Grafo/campo completo más presupuesto | Poco cambio estructural | Coste y latencia crecen con área; 9000 no mejora uniformemente; no autorizado | Control, no solución D |
| Ventana móvil única alrededor del jugador | Campos pequeños | No resuelve enemigos lejanos, barreras que exigen salir de ventana ni continuidad del jefe | Insuficiente por sí sola |
| Regiones conectadas + campos locales compartidos | Conectividad global y trabajo local reutilizable | Portales, invalidación, caché y costuras requieren pruebas | Recomendada |
| Reparación incremental del campo global | Reutiliza trabajo al cambiar meta vecina | Sigue manteniendo todo el mapa; saltos de altura y fallback pueden ser caros | Comparador futuro si el prototipo regional no compensa |

No equiparar región de terreno a región de navegación. Una baldosa XZ puede contener varias superficies y componentes sin comunicación; las caídas son dirigidas. Tampoco agrupar todos los portales de una baldosa como si estuvieran conectados.

## 5. Arquitectura recomendada

Todo este apartado es propuesta. Mantener física, tamaños, arrays centrales y contratos de combate; introducir backend regional optativo para D, sin cambiar resultados A/B/C.

1. **Geometría estática global y partición estable.** Terreno/estructuras se generan una vez. Baldosas XZ candidatas de 32 m, comparar 16/32/64 m en el mapa C; todas las capas Y pertenecen al mismo índice XZ, con identidades de superficie distintas. Coordenadas absolutas y nodos `(baldosa, celda global, superficie, clase)`. No recentrar cuerpos al cruzar.
2. **Constructor acotado.** Extraer capacidad de construir nodos/aristas en un rectángulo, usando consultas sobre geometría mundial. Tapas mediante candidatos espaciales ordenados. Compartir datos inmutables; buffers temporales propios por tarea. No clonar la tabla mundial del terreno por cada baldosa. Mantener constructor monolítico como oráculo en 80/128 m.
3. **Costuras físicas.** Asignar propietario único por nodo/arista. Un halo no puede ser solo media celda: L requiere huella de 2,65 m y `SurfaceBeyond` busca hasta 8,05 m desde origen; barridos y salidas inclinadas pueden exigir más. Determinar halo por la envolvente real del recorrido; consultar globalmente cuando la rebasa. Incluir trayectorias que crucen esquinas o varias baldosas, toda su altura y destino. No truncar escaladas ni crear apoyos artificiales en el borde del recorte.
4. **Conectividad global por clase.** Portales dirigidos derivados de aristas físicas válidas entre regiones. Conservar sus extremos, anchura y superficies. Agrupar solo tramos continuos equivalentes y mantener alternativas relevantes; registrar componentes fuertemente conexas y conexiones dirigidas. Evitar falsa conectividad entre capas. Comparar accesibilidad con el grafo completo antes de comprimir portales.
5. **Jerarquía de rutas compartida.** Un campo de costes sobre portales por clase hacia la región/meta del jugador; costes de transferencia internos calculados sobre el grafo físico y reutilizables. No generar todas las parejas de portales sin medir: crecimiento cuadrático y múltiples Dijkstra pueden costar más que C. Empezar con fronteras exactas en pequeño; probar compresión conservadora y transferencias perezosas con caché. Ruta global válida primero; optimalidad aproximada solo dentro del margen acordado.
6. **Campos locales con múltiples salidas.** Para cada región ocupada y clase, Dijkstra inverso sembrado en las salidas con su coste global; en la región destino, desde la meta real. Usan el mismo campo todos los enemigos compatibles. Clave de caché incluye versión de geometría y vector de costes de salida, no solo región del jugador. Si cambia la meta dentro de una región pueden cambiar las mejores salidas lejanas: invalidación explícita, nunca asumir reutilización eterna.
7. **Publicación y planificación.** Presupuesto compartido conservado en 3000; reparto determinista por turnos con cuotas mínimas entre clases activas y antigüedad de solicitud, no monopolio por población. Priorizar regiones ocupadas y próximas a cruzarse, sin abandonar lejanas. Solicitudes repetidas se agrupan; no cancelar continuamente hasta impedir terminar. Publicar versiones completas compatibles de campo global/local; conservar la versión previa válida mientras se calcula la nueva. Medir edad máxima cuando el jugador no se detiene. Cuotas y política exactas se validan en D3; no se modifican ahora.
8. **Movimiento continuo y caché.** Un escalador conserva trayectoria, región origen/destino y referencias hasta acabar; ambas quedan fijadas en caché. Precargar vecinas antes de llegar al portal. Si faltan datos, espera explícita medible en nodo seguro, nunca teleport, snap ni persecución recta a través de pared. La espera debe desaparecer en condiciones aceptadas, no convertirse en mecanismo normal de ocultación.
9. **Carga y residencia.** Primera versión construye cada región una vez durante carga, extrae conectividad global y conserva datos suficientes para cualquier enemigo lejano. Comparar grafos detallados residentes con caché acotada; reducir campos no implica reducir grafos. Solo descargar detalle si reconstrucción/almacenamiento y prefeteo caben en presupuesto. Nunca descartar regiones ocupadas ni congelar enemigos fuera de cámara. Si 750 ocupan casi todas las regiones, medir y soportar ese peor caso o detenerse antes de aumentar tamaño.

El reparto por unidades de trabajo da reproducibilidad; usar tiempo de CPU para métricas, no para decidir el orden lógico de publicación. Contabilizar también inicialización de arrays, semillas fallback, relajaciones y portales; cualquier cambio de significado del presupuesto se documenta y compara con el límite actual, sin presentarlo como una subida oculta.

## 6. Integración del generador procedural

**Etapa QA:** perfil D separado, con RNG derivados `p0d-*` y parámetros propios. No cambiar defaults de C ni sus semillas aceptadas. Reutilizar primitivas geométricas/validadores; separar trazado de niveles, red de caminos, hitos y detalles. Primero conectar plazas/mesetas principales con rutas para L y al menos dos accesos independientes; después colocar estructuras, vegetación y marcadores.

Introducir estructuras grandes con patios, tejados planos transitables y escaleras compatibles con 0,6 m, giros de 90°. Conservar la semántica de exclusiones de escalada. Rampas principales propuestas 8–12 m, ≤30°, con aproximaciones y desembocaduras despejadas, sin multiplicar agujeros decorativos que no atraviesa el jefe. Mantener accesos solo por escalada en zonas pequeñas y variedad de alturas, no solo corredores planos.

Validación primero barata por regiones y luego con navegación física. Reparaciones locales reconstruyen solo baldosas afectadas y halo/dependencias de aristas; invalidan portales y campos relacionados. Un cambio local puede alterar conectividad global: volver a comprobarla. Límite de intentos determinista y rechazo explícito; nunca devolver un mundo fallido como aceptado al agotar reintentos.

**Etapa de integración futura, separada:** adaptador nuevo de `WorldData` y modo de partida optativo; conectar movilidad/cámara/combate P0, colocación física de interactuables y spawns por tamaño, minimapa/render y flujo de victoria. Los marcadores C no son interactuables jugables. Conservar cantidades, IDs, fórmulas, RNG históricos y guardados; no inferir nuevas recompensas de la densidad de lugares. Revisar reciclado de 85 m como decisión separada antes de integrar. Validar las duraciones/personajes del juego y solo entonces proponer adopción predeterminada.

**Referencia:** `WorldData.Generate` U3 y `unity/Docs/Reference/u3-world.json` permanecen intactos, con entrada explícita en pruebas históricas. No regenerar corpus para que el nuevo mundo coincida; QA D tendrá sus propias firmas y manifiestos. No sobrescribir la build U6.

## 7. Conectividad, escalada y rutas del jefe

Mantener contrato C: toda zona transitable por jugador amenazable por S; zonas principales/mesetas amplias por L, sin encoger cuerpos. Verificar caminos dirigidos de ida y vuelta, soporte total, salida de borde, capas superpuestas, pasos estrechos y caída. Una región conectada con un solo acceso no prueba capacidad suficiente.

### Investigación estática del caso HORDA750

Evidencia heredada: `ProceduralPursuitTests.BossReachesTheWidestPlateauItFits`, XML U3 Edit; destino aproximado `(42,8; 4,5; −30,3)`, inicio `(−14,3; 0; −0,3)`, 69,6 s. El ensayo empieza con un jefe y jugador inmóvil; `Boss` puede crear cinco esbirros por estornudo. `ClimbStarts=20` suma todos los cuerpos. El fin comprueba altura/estabilidad, no región ni contacto; el campo impreso al final no mide la longitud total recorrida. La descripción «unos 70 m» no sustituye una traza de recorrido real.

Hechos del código: `Boss.Step` alterna espera, aviso, ataque y recuperación; `EnemiesNav` marca estados 1/3 como quietos y las cargas usan dirección fijada. `EnemyNavigation.Move` no inicia una arista de escalada cuando hay carga o quietud. Estos tiempos y movimientos pueden añadir demora aunque la ruta exista. Los costes de aristas modelan distancia y penalización de transición, no ciclos de ataques ni colas. Por tanto, no está demostrado que el camino elegido minimice el tiempo real del jefe.

Hipótesis a distinguir: rodeo por huella de L; sucesión de transiciones; interrupciones por ataques/cargas; separación/esbirros; desvío tras bloqueo; latencia del campo. La meta fija y ausencia de horda inicial debilitan la explicación por recalculado continuo o saturación de 750, pero no aíslan causalidad. No cambiar presupuesto ni balance a partir de esta hipótesis.

Primer trabajo futuro D0: traza por ID estable del jefe (no índice reutilizable), posición, nodo/arista/región, meta y versión de campo, fase/ataque, velocidad, longitud acumulada, espera física y de cálculo, tiempo Walk/Climb/Ledge/Drop/Recover, esbirros y acceso elegido. Repetir misma fixture y controles exclusivos de diagnóstico: cuerpo L sin controlador de ataques, jefe completo, jefe con horda; jugador fijo y móvil. No alterar las reglas de producción. Verificar llegada a región objetivo y alcance de ataque, no solo altura. No exigir idénticos tiempos al quitar ataques: descomponer la diferencia. Si revela un error de C, presentar corrección acotada separada, sin reabrir toda su aprobación.

## 8. Riesgos de aglomeración y presión

Separar medidas, con categorías temporales exclusivas cuando sea posible:

| Causa | Medida propuesta | Ensayo que la distingue |
| --- | --- | --- |
| Capacidad física | Entradas/s por acceso y clase, anchura útil, longitud/tiempo de cola | Misma meta estable y campo precalculado, 300/500/750 |
| Elección de ruta | Uso de cada acceso, distancia efectiva frente al óptimo geométrico | Dos accesos equivalentes o alternativos con diferente congestión |
| Congestión local | Vecinos que bloquean, separación, espera de columna | Campo idéntico, cuerpo aislado frente a horda |
| Latencia | Solicitud→publicación, versión usada, edad de meta | Cambios discretos y movimiento continuo entre alturas |
| Transición | Tiempo detenido, reintentos, caídas/sueltas por ID y arista | Pared/rampa aislada y costuras regionales |
| Tamaño L y combate | Accesos descartados, fases del jefe, llegada/ataque | Comparación de cuerpos y controlador completo |

Corregir instrumentación futura: reiniciar/etiquetar distancia base cuando cambia la versión/meta y reconocer retirada u órbita intencionada de enemigos a distancia, avisos y recuperación. El indicador C de falta de mejora durante 2 s no identifica colas por sí solo; conservarlo para comparación y añadir causas.

No añadir automáticamente costes dinámicos por multitud: pueden producir oscilaciones o cambiar continuamente campos. Primera intervención recomendada si falta caudal: geometría y distribución de accesos; después evaluar selección de rutas con histéresis determinista solo si se demuestra infrautilización. Comparar presión cerca del jugador, porcentaje de llegadas y tiempo hasta primer ataque. No compensar un mundo vacío aumentando spawns, velocidades o daño sin autorización de balance.

## 9. Rendimiento y presupuestos propuestos

Objetivos iniciales aprobados el 12/10/2026, sobre Ryzen 7 7700X / RTX 4070 Ti SUPER del autor. No son resultados garantizados ni requisitos mínimos comerciales. Medir Normal y Development por separado, warmup y tres repeticiones; p50/p95/p99/máximo, no solo media. Carga de mundo grande y FPS corresponden a D5–D6, todavía no autorizados.

| Métrica | Propuesta para aceptar el candidato D |
| --- | --- |
| Generación geométrica sin navegación | p95 ≤500 ms en el tamaño elegido |
| Construcción inicial y validación navegación | p95 ≤4 s; máximo ≤6 s, incluidas reparaciones; carga total QA visible ≤6 s p95 / 8 s máximo |
| Trabajo de reconstrucción/prefeteo durante juego | Ninguna reconstrucción global; ≤1 ms p95 / 2 ms máximo de trabajo de preparación en hilo principal por frame, medido aparte del tick |
| Navegación completa, incluyendo mover cuerpos | 300/500/750: p95 ≤0,6/0,8/1,0 ms por tick; p99 ≤1,5 ms, máximo ≤2 ms |
| Publicación hacia meta fija nueva | S/M/L p95 ≤0,3 s, máximo ≤0,5 s; medir cercano y lejano por separado |
| Jugador continuamente móvil | Edad del objetivo representado p95 ≤0,3 s, máximo ≤0,5 s; sin solicitudes perpetuamente pospuestas |
| Cambio de región | 0 teleports, 0 pérdida de ruta; pausa por datos ausentes ≤1 tick en precalentado y ≤0,1 s en primera visita |
| Memoria | Datos navegación residentes ≤160 MiB; transitorios adicionales ≤96 MiB; managed total QA ≤256 MiB y Unity total asignado ≤512 MiB, medir picos y estabilización |
| Asignaciones | 0 B/tick en navegación estable; cambios de región con buffers reutilizados, sin GC atribuible a navegación durante recorrido de 10 min |
| Build real | 1080p, interna 360, Normal; objetivo 60 FPS sostenidos con 750: frame p95 ≤16,7 ms, p99 ≤25 ms, ningún parón repetible >50 ms atribuible al recorrido regional |

Estos objetivos aprobados **relajan** el objetivo CPU relativo de C a cambio de exigir reacción mejor; no constituyen aprobación de 6000/9000 nodos. Si no se cumplen con 3000, presentar perfiles y alternativas, no aumentar silenciosamente el límite. Presupuesto de región no es un coste adicional permitido sin revisar frame total.

El objetivo de memoria requiere contar por separado geometría, nodos, aristas, trayectorias, tablas de portales, campos, heaps, cachés y copias temporales. Calcular a partir de longitudes de arrays y verificar con Development; no extrapolar los 80 MB como memoria puramente navegacional. Registrar techo y expulsiones de caché; probar horda dispersa y jefe distante.

Para FPS reales: escena QA renderizada sin captura de PNG en la ventana medida; cámara siguiendo recorrido humano/grabado, horda, y una variante de combate/efectos representativa. VSync/cap, resolución y foco fijos; CPU/GPU cuando el contador sea válido y N/D si no. Comparar 0/300/500/750 con captura desactivada y separar HUD/arnés. No concluir FPS a partir del tick lógico de ~1,4 ms.

## 10. Determinismo y semillas

Conservar P0QA, MAMPORRO, COLINAS7, Z9K2, MESETA, ACANTILADO, RAMPA42 y HORDA750. Derivaciones D por versión del generador, fase y coordenada estable; nunca por orden de visita, de carga o finalización de threads. Sólidos/portales con IDs estables, orden canónico de inserción/desempate y publicación en ticks predeterminados. Flujos D separados de U3/C y del RNG de combate.

Manifiesto futuro: commit, semilla original/normalizada, versión, tamaño/límite, parámetros, intento/reparaciones, hashes de terreno/sólidos/grafos/portales, cuerpos, presupuesto, entrada grabada, tick del fallo, región/nodo/arista e ID de enemigo. Reproducción propuesta con parámetros QA de tamaño/backend y semilla; **esos parámetros D aún no existen**, no documentarlos como comandos ejecutables.

Comparar construcción serie/paralela y orden distinto de visitas para igual geometría, conectividad y recorrido lógico. Separar determinismo lógico con dt fijo de variación de frame/CPU. Seed que falle entra en corpus D con manifest y criterio concreto; no regenerar referencia U3 ni ocultar fallos cambiando expectativa.

## 11. Archivos y sistemas afectados en una futura implementación

| Sistema | Candidatos y cambio propuesto |
| --- | --- |
| Regiones | Nuevos `Core/World/NavRegionGraph.cs`, `NavPortals.cs`, `RegionalNavField.cs`, `RegionalEnemyNavigation.cs` (nombres propuestos) |
| Constructor físico | `NavGraph.cs`: rango acotado, costuras y enumeración espacial; ruta histórica conservada |
| Consultas | `VerticalQueries.cs`, `TerrainWallGeometry.cs`: datos inmutables compartibles y candidatos; equivalencia geométrica |
| Horda | `IEnemyNavigation` en `EnemiesNav.cs` y adaptación regional compatible; estados/arrays centrales preservados |
| Generación | Nuevos perfil/layout/validación D; reutilización explícita de primitivas C sin cambiar sus defaults |
| QA | `VerticalCircuit.cs`, `VerticalQa.cs`, `U3Game.cs`, captura D y `scripts/p0.ps1`: modo optativo, manifiesto y mediciones |
| Render | Adaptación optativa de `WorldRenderer`, bounds en `RunRenderer`/partículas/números; bloques visibles si las medidas lo requieren |
| Pruebas | `Tests/Core` regionales y de presión, `Tests/PlayMode` de costuras y carga, scripts de informes |
| Integración posterior | Nuevo adaptador `WorldData` y selección de modo; revisión separada de apariciones/interactuables, sin cambiar U3 histórico |

No existe aún ninguno de los nuevos componentes. No modificar catálogo, director, recompensas, persistencia ni `WorldData.Generate` para habilitar el experimento.

## 12. Subfases de implementación propuestas

**Checkpoint 14/10/2026:** D0 diagnosticado y corrección acotada C autorizada expresamente: [DIAGNOSTICO_P0D0](DIAGNOSTICO_P0D0.md). Convertir X/Z a float antes de validar en `TryStep`; continuar D1 tras verificar sin regresiones, luego D2–D4 secuencialmente. Inanición M/L prioritaria para D3, manteniendo 3000 nodos/tick. CPU/residencia de Editor medidas; residencia exclusiva de navegación y render standalone pendientes, sin declaración de cumplimiento.

D0–D4 autorizadas; D5–D7 no autorizadas. Un resultado verificable por commit y checkpoint; una subfase puede requerir varios commits pequeños. No avanzar al tamaño mayor si la arquitectura no pasa el ensayo reducido.

| Fase | Pieza | Condición de salida |
| --- | --- | --- |
| D0 | Instrumentación y aislamiento HORDA750; baseline de memoria, campos y render | Demoras clasificadas por ID/causa, coste y métrica de llegada fiables; presentar cualquier corrección C aparte |
| D1 | Constructor regional físico en 80/128 m | Equivalencia de nodos/aristas y alcance contra monolítico; costuras L/alturas/esquinas sin penetración |
| D2 | Portales dirigidos y conectividad mundial | Sin falsas conexiones ni bolsas; prueba de caminos que salen de la ventana y vuelven; coste/memoria medidos |
| D3 | Campos locales, caché y reparto entre clases | Persecución 300/500/750, metas móviles y lejanas; reacción medida con 3000 y sin hambre de M/L |
| D4 | Cruces físicos y residencia/prefeteo | Escalador en costura, caída entre regiones, jefe lejano, horda dispersa; cero teleport y pausas dentro de presupuesto |
| D5a | Generador D QA a 256 m y red de hitos/accesos | Densidad, conectividad, carga, memoria y caudal aceptables antes de ampliar |
| D5b | Tamaño final elegido y estructuras grandes | Solo tras puerta de rendimiento D5a; medir 320 o 384 según decisión, no elegir silenciosamente |
| D6 | Build QA y render medido, regresiones y cierre | Edit/Play/QA y FPS reales, manifiestos/guía; prueba manual del autor y parada |
| D7, separado | Propuesta de integración jugable/adopción | Nueva autorización; pruebas de partida completa, progreso intacto y referencia U3 conservada |

Recomendación: autorización inicial D0–D4, revisar los resultados y habilitar D5–D6 después. Alternativa: autorizar D0–D6 con puertas cuantitativas explícitas y detenerse ante incumplimiento. D7 nunca implícito en aceptar el circuito D.

## 13. Pruebas y criterios de aceptación

**Lógica/EditMode:** fixtures mínimos (dos regiones y una pared, rampa diagonal de frontera, tejado multicapa, caída unidireccional, esquina de cuatro regiones, estructura mayor que una baldosa, paso permitido S/M y prohibido L). Equivalencia física con monolítico en A/B/C sin regenerar referencias. Portales por tamaño, metas inaccesibles/fallback y caminos lejanos que rodean una barrera. Frontera cruzada durante Climb/Ledge/Drop, empuje/suelta, expulsión de caché y reentrada. Verificar por tick cero penetraciones/bajo terreno/teleports; cero regiones cerradas no declaradas y ausencia de refugios para S.

**Semillas:** ocho fijas en 128 m y tamaño elegido; 256 intermedio. Propuesta de barrido nocturno determinista de 100 semillas generadas desde un prefijo fijo, más casos mínimos/máximos de relieve/densidad. No afirmar cobertura universal. Registrar tasa de reparación/rechazo y motivos; propuesta ≤5 % de intentos rechazados y ninguna semilla sin resultado válido dentro del tope; si se excede, informar y detener aumento de escala.

**Presión:** 300/500/750, repartidos y concentrados; objetivos altos, suelo, tejados y cambios continuos. Separar cuerpo aislado, horda y jefe completo. Propuesta: ≥95 % de cuerpos de prueba llegan a región/alcance en `1,5 × tiempo aislado de la misma ruta + 3 s`, descontando solo estados deliberados de ataque que se midan; geometría bloqueada >2 s ≤2 %, pero ninguna bolsa persistente. Primer ataque y caudal no empeoran más de 10 % frente al backend completo sobre idéntico mapa pequeño. Para mapa grande comparar a igual distancia de ruta; 750 repartidos por toda su área no es comparable a 750 cerca del jugador. Cola tolerable propuesta p95 ≤3 s por acceso; cualquier espera >10 s se revisa aunque las medias pasen. Son objetivos a confirmar, no cifras ya obtenidas.

**PlayMode:** selección A/B/C/D, alta/baja de mundo y cachés, pausa/foco/reinicio, cámara frente a paredes grandes, render en bordes más allá de bounds antiguos, colores/altura coherentes, objetos destruidos sin retener grafos. No escritura de progreso personal desde QA.

**Build QA:** Normal y Development separadas, misma semilla/entrada/configuración y manifiesto. Tres pasadas temporizadas, recorridos de 10 min y varios cambios de semilla para detectar crecimiento de memoria. Todas las métricas de sección 9; capturas fuera de ventana de rendimiento. Futuro comando D por documentar cuando exista, no ejecutar `u3.cmd build` sobre U6.

**Regresiones:** A/B/C (movilidad, escalada, combate vertical, estrés y control de publicación), suites Edit/Play completas, verificadores históricos U0/U2/U3/U4 y contratos de U5/U6, U6 ejecutable/paquete preservados, SHA de u3-world y 7/7 ajustes. No existe un supuesto único comando de referencia U0–U6: usar los verificadores realmente existentes y los checkpoints de cada hito; no reabrir la migración.

**Manual:** variedad/escala, orientación y ausencia de recorridos vacíos; entrar/salir de mesetas y grandes edificios, cruzar regiones a máxima velocidad y mientras se escala, persecución S/M/L, jefe HORDA750, accesos alternativos, ataques a distancia, ausencia de refugios, tirones/cargas y límites visuales. Aprobación explícita del autor al cerrar; no deducirla de pruebas automáticas.

## 14. Riesgos y protecciones

- Jerarquía mal comprimida: rutas falsas o pérdida de alternativas. Mantener dirección/capa/clase y contraste con oráculo.
- Explosión de portales/transferencias y caché: medir antes de D5; no prometer coste independiente de área cuando la horda ocupa todo el mapa.
- Latencia por invalidaciones continuas o campos incoherentes: versiones completas, cuotas y prueba de jugador móvil. Mayor presupuesto no sustituye este diagnóstico.
- Cruces largos y halos insuficientes: consultas geométricas mundiales y ownership; nunca recortar una trayectoria en la costura.
- Mundo grande pero vacío o sin amenaza: rutas entre hitos y pruebas de llegada, no densidad visual o conectividad como única aceptación.
- Jefe: ancho geométrico más envolvente y fases de ataque; evitar alterar su balance para ocultar rutas deficientes.
- Coste de reparación/carga y render: instrumentar por fase; geometría y grafos residentes siguen creciendo aunque el campo sea local. Niebla no es culling.
- Mantener `src/`, generador U3, RNG/IDs/formatos de progreso, A/B/C, `unity/Docs/Reference/` y especialmente `u3-world.json` sin regenerar. U6 SHA `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`; u3-world SHA `8ab0185cd3f29ea4bbd5e6e5d21fe09b6d32179f5061391ee765780904959787`.
- Siete ajustes Unity fuera del índice, huellas de PROGRESO_P0; sin reset/clean/force-push/PR. Build/progreso QA aislados. Sin P1/P2, servicios o compras.

## 15. Decisiones originales y resolución del autor (12/10/2026)

**Resueltas:** el autor aprueba las ocho decisiones: D0–D4 y parada antes de D5; candidato futuro 384 m pasando por 256, revisable a 320 si las medidas lo aconsejan; regiones 16/32/64 y arquitectura propuesta con 3000; geometría futura indicada y relieve inicial 13,5; densidad 6–12 s sin vacíos >15 s, reducir primero detalles secundarios y después tamaño antes de perder lugares interesantes; presupuestos sección 9 como objetivos; validación sección 13, 100 semillas/FPS reservados a D5–D6 en su equipo a 1080p/interna 360; QA primero sin integración U3. No preguntar otra vez. La lista siguiente conserva las preguntas históricas:

1. **Alcance y puerta de autorización:** ¿D0–D4 primero y revisión antes de escalar (recomendado), o D0–D6 con parada obligatoria si falla una puerta? D7/adopción jugable separado en ambos casos.
2. **Tamaño candidato:** ¿384 m / límite 180, pasando por 256 m (recomendado para ampliación marcada), 320/148 más conservador, o solo 256/116 en este bloque? Ninguna opción reemplaza U3.
3. **Arquitectura:** ¿aprobar el prototipo de regiones/portales dirigidos y campos locales compartidos, comparando baldosas 16/32/64 m, con 3000 como techo compartido y cuotas sin hambre de M/L?
4. **Geometría:** ¿aceptar mesetas principales 24–64 m, estructuras 12–24 m, conjuntos hasta 32–40 m, rampas principales 8–12 m y dos accesos independientes en zonas grandes? ¿Mantener 13,5 m de relieve inicialmente y dejar 18/22,5 para otra revisión (recomendado)?
5. **Densidad y sensación:** ¿objetivo de 6–12 s entre hitos y sin tramos obligatorios vacíos >15 s, con cantidades QA de sección 3 y sin aumentar recompensas? ¿Qué prefieres priorizar si la densidad eleva coste: menos tamaño o menos conjuntos?
6. **Rendimiento:** ¿aceptar presupuestos propuestos de sección 9 (incluida navegación p95 hasta 1 ms con 750, carga total p95 6 s y 160 MiB residentes de navegación), manteniendo 3000? Son nuevos objetivos, no continuidad automática de los aceptados en C.
7. **Presión y muestreo:** ¿aceptar umbrales de llegada/colas de sección 13, barrido de 100 semillas y objetivo de 60 FPS con 750 en el equipo de referencia? ¿Debe otro equipo/resolución formar parte de aceptación ahora?
8. **Integración posterior:** ¿confirmar que D termina en QA, con marcadores y sin cambiar spawns/director/reciclado de U3? Recomendado. Si se quiere partida jugable dentro del mismo bloque, primero ampliar el plan y resolver interacción real, reciclado de 85 m y persistencia; no asumir esa autorización.

**Siguiente paso vigente: ejecutar D0–D4 con pruebas/checkpoint/commit/push por pieza. Consultar ante defecto heredado que requiera corregir C, incumplimiento importante, regresión o nueva decisión arquitectónica. No implementar D5–D7.**
