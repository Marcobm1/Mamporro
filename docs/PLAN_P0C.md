# P0-C — geometría procedural conectada: plan histórico aprobado

Estado vigente: **P0-C aprobado manualmente el 12/10/2026** («Sí, todo bien, autorizamos y seguimos»). C0–C6 implementados; código/builds `9a24b6f`, cierre técnico `369fdff`. Limitaciones aceptadas provisionalmente y 3000 nodos/tick conservados. P0-D solo en planificación autorizada.

El resto conserva la propuesta y diagnóstico anteriores a C; no son pendientes actuales. Las diez decisiones están resueltas en [DECISIONES](DECISIONES.md); resultados y cierre formal en [PROGRESO_P0](PROGRESO_P0.md).

Base: P0-A y P0-B cerrados y aprobados (escalada del jugador y de todos los enemigos, navegación compartida por tamaños, combate vertical). Límites aceptados de P0-B que condicionan este plan: navegación 0,61 ms p95 / 1,49 ms de pico con 750 enemigos, 648 ms para construir los mapas del circuito B (80 m), ~2 KB/frame más en Development, aglomeración al pie de paredes y necesidad futura de navegación por regiones.

## 1. Diagnóstico del generador actual (código real)

### 1.1 Mundo jugable U3 (`WorldData.Generate(seed)`, `Core/World/WorldCollision.cs`)

Orden fijo con flujos de RNG derivados de la semilla (`Rng.Derive("terrain"|"sites"|"props"|...)`):

1. **Terreno** (`Heightfield.Generate`, `Core/World/Heightfield.cs`): rejilla de 256×256 celdas sobre 320 m (celdas de 1,25 m). fBm de 4 octavas con distorsión, relieve 26 m y terrazas de 4,5 m cuya dureza varía con un ruido de "acantilado" (`Gain` con exponente 1,2–9). Zona de inicio llana en un radio de 14 m. Montañas en el borde a partir de 128 m (límite jugable squircle de 136 m). Ya existen escalones de terraza abruptos, pero no se diseñan: no hay garantía de conectividad, de anchura de las terrazas ni de accesos.
2. **Sitios** (`Sites.Pick/Flatten`): 5 casas, 2 templos, 2 granjas y 3 pozos en zonas casi llanas, alejados del inicio y entre sí, con el suelo aplanado bajo ellos.
3. **Estructuras** (`Props.Generate`, `Core/World/Props.cs`): ruinas por sitio (muros de 2,7–3,3 m con hueco de puerta, columnas, tejados **solo decorativos**) y objetos sueltos (troncos, tocones, setas, carteles, muros). Los colliders son **cajas orientadas con cualquier rotación** y **círculos** (`Collider`), con bandera `Standable`.
4. **Interactuables** (`InteractableSpots.Place`): cofres, santuarios, tótems y portal en terreno de pendiente ≤ 16°, sin colliders encima.
5. **Vegetación** (`Vegetation`): árboles y pinos (círculo no apoyable), rocas (círculo apoyable), hierba y flores visuales.
6. **Colisión** (`WorldCollision` + `ColliderGrid`): altura del terreno sin límite de pendiente y empuje circular contra colliders. Es la física histórica de U3 (réplica web).

Apariciones (`WorldSpawns`): anillos, líneas y arcos alrededor del jugador con `Heightfield.HeightAt` y `IsInside`, sin comprobar estructuras ni alturas. Recogibles (`Pickups`): se recogen en planta, sin altura (limitación aceptada de P0-B).

**Restricción decisiva:** `unity/Docs/Reference/u3-world.json` congela, para cuatro semillas, alturas, muestras, sitios, piezas, colliders, interactuables, decoración y hierba de `WorldData.Generate`, y `DirectorReferenceTests` genera mundos con él. **El generador U3 no puede cambiar**: P0-C tiene que ser un generador nuevo y aparte.

### 1.2 Circuitos QA P0 (`VerticalCircuit`)

Circuitos A y B de 80 m: terreno de celdas de 1 m escrito a mano y sólidos `VerticalSolid` **alineados a ejes** (cajas AABB con categoría). De ellos salen las consultas de escalada (`VerticalQueries`) y los colliders U3 (`Collider.Oriented` con rotación 0). Es geometría fija de prueba, no un generador.

### 1.3 Navegación P0-B (reutilizable)

- `NavGraph`: construcción genérica desde `Heightfield` y `VerticalQueries`, sin IDs, para cualquier geometría expresable como terreno más cajas AABB. Ya acepta mesetas, acantilados del terreno, rampas, tejados y paredes con techo.
- `NavField`, `EnemyNavigation`, combate vertical: independientes del trazado.
- `VerticalQueries` recorre **todos** los sólidos en cada consulta (O(sólidos)). Con 17 sólidos da igual; con cientos de estructuras procedurales sería el cuello de botella de la construcción de mapas y de la línea de visión.
- La construcción de mapas escala con el número de celdas: circuito de 80 m ≈ 25 600 celdas por clase en 648 ms en total. Un mundo U3 completo (320 m, 409 600 celdas por clase) costaría del orden de 16 veces más en tiempo y memoria, inviable sin navegación por regiones, que es P0-D.

### 1.4 Qué se reutiliza y qué cambia

| Pieza | Reutilización |
| --- | --- |
| `Heightfield`, `Rng`, `SimplexNoise2D`, `WorldMath` | Tal cual (nuevos flujos derivados para P0-C) |
| `Heightfield.Generate`, `Sites`, `Props`, `InteractableSpots`, `Vegetation` (U3) | **Sin cambios** (referencias congeladas). P0-C puede inspirarse en sus formas, pero genera aparte |
| `VerticalSolid`/`VerticalQueries` | Reutilizados; necesitan una rejilla espacial de sólidos para escalar (mismo resultado, más rápido) |
| `NavGraph`/`NavField`/`EnemyNavigation`/`IVerticalCombat` | Reutilizados; medir y ajustar presupuestos con mapas mayores |
| `VerticalCircuit` (A, B) | Sin cambios; P0-C añade un tercer trazado generado (C) |
| `WorldSpawns` (U3) | Sin cambios; en P0-C las apariciones QA usan nodos del grafo (como el circuito B) |
| Render (`Renderer.Build(WorldData)`) | Reutilizado mediante un `WorldData` construido desde el trazado generado, como hace `VerticalCircuit.Data()` |

## 2. Problemas que resuelve P0-C

- El relieve actual sale de ruido con terrazas: formas redondeadas, terrazas estrechas o irregulares y acantilados sin intención ni garantía de acceso.
- No existe noción de **conectividad**: nada asegura que una zona elevada se pueda alcanzar, por rampa o escalando, ni que no haya bolsas que atrapen hordas.
- Las estructuras U3 son ruinas a ras de suelo con tejados decorativos y rotaciones libres, no aprovechables por la escalada P0 (consultas solo AABB).
- Los circuitos QA son fijos: no prueban la variedad entre partidas.

## 3. Objetivos y exclusiones

**Objetivos:**
- Generador determinista por semilla de trazados jugables con relieve "cuadrado": niveles, mesetas amplias, acantilados escalables, rampas, corredores y zonas abiertas.
- Estructuras de varios niveles alineadas a la rejilla.
- Conectividad verificada con la propia navegación P0-B para los tres tamaños. Reparación determinista y rechazo medido.
- Apariciones válidas.
- Validación masiva por semillas y medidas de generación, navegación y rendimiento.

**Exclusiones:**
- Ampliar el tamaño del mundo o navegar por regiones (P0-D).
- Sustituir el generador U3 o activar P0-C en la entrega U6.
- Arte final o renovación visual.
- Balance, director, enemigos, armas, recompensas, progreso o guardado.
- Rediseño de cámara.
- Corregir la recogida en planta (pendiente aparte, salvo decisión del autor).

## 4. Arquitectura propuesta

Un **generador por capas sobre una rejilla de macroceldas**, validado con la navegación real:

1. **Trazado de niveles.** Rejilla de macroceldas (propuesta: 4 m) sobre un mapa cuadrado. A cada macrocelda se le asigna un nivel entero (propuesta: 0–3, un nivel = 4,5 m, el mismo escalón de terraza que U3). Se usa ruido de baja frecuencia cuantizado y un filtrado de formas (eliminar islas de una celda, rellenar huecos, imponer una anchura mínima de meseta). El resultado son mesetas de bordes rectos y esquinas reconocibles, con algo de irregularidad por ruido de borde (desplazar algunos bordes una celda) para que no parezca una cuadrícula perfecta.
2. **Bordes y acantilados.** Entre macroceldas de distinto nivel, el terreno sube en una sola celda del heightfield (celdas de 1 m): pared abrupta de más de 48° que se escala. Como regla de partida, las diferencias de dos niveles o más se mantienen en una sola pared.
3. **Rampas y accesos.** Sobre el grafo de regiones (componentes de igual nivel) se colocan rampas de pendiente transitable (propuesta: ≤ 30°) entre regiones contiguas, según reglas de la sección 6. Las rampas se esculpen en el heightfield como en el circuito B, de modo que la navegación las descubre sin datos aparte.
4. **Estructuras** (`VerticalSolid` alineados a ejes, rotaciones de 90°): bloques con tejado plano transitable, torres, muros, pasarelas apoyadas y escaleras de bloques de ≤ 0,6 m. Plantillas paramétricas con variación por semilla (ancho, alto, huecos), colocadas en mesetas y en el suelo respetando separaciones para la horda.
5. **Validación y reparación con la navegación P0-B:**
   - Se construyen los grafos S, M y L y un campo desde la zona de inicio.
   - Si una región transitable queda inaccesible para S, se repara con un número limitado de pasos deterministas: añadir rampa, rebajar o ensanchar un borde, quitar un voladizo, separar estructuras.
   - Si no se consigue, la semilla se rechaza y se reintenta de forma determinista, contando y registrando los rechazos.
6. **Salida:** un `VerticalLayout` generado con heightfield, sólidos, inicio, puntos de aparición y puntos clave, del que salen `VerticalQueries`, `WorldCollision` y `WorldData` para el render, exactamente como en los circuitos A y B.

Cambios técnicos necesarios en lo existente, sin alterar resultados:
- `VerticalQueries` con rejilla espacial de sólidos: las consultas solo miran los sólidos cercanos. Mismo resultado comprobado contra la versión lineal en pruebas.
- `NavGraph` con perfil de coste.

## 5. Alternativas consideradas

| Alternativa | Ventaja | Inconveniente | Veredicto |
| --- | --- | --- | --- |
| Endurecer el ruido U3 (más terrazas, cuantización) | Poco código | Sin control de conectividad ni de formas; rompería las referencias si se toca el generador U3 | Descartada como base |
| Macroceldas con niveles + validación por navegación (propuesta) | Formas cuadradas controladas, conectividad verificable con la física real, reproducible | Más lógica de reparación; coste de construir mapas en cada mundo | **Recomendada** |
| Módulos diseñados a mano ensamblados (chunks) | Coherencia y calidad | Menos variedad; exige diseñar módulos | Complemento futuro para estructuras, no como base |
| Wave Function Collapse | Variedad con reglas locales | Difícil garantizar conectividad global y depurar semillas; coste | Descartada para P0-C |
| NavMesh de Unity para validar | Herramienta estándar | Rompe el núcleo puro y determinista; no modela escalada | Descartada |

## 6. Reglas de generación y conectividad (propuestas para discutir)

1. **Zona de inicio:** llana, amplia y despejada (propuesta: radio ≥ 12 m, como U3) a nivel 0, con espacio para la horda.
2. **Mesetas:** anchura mínima de una meseta transitable (propuesta: ≥ 8 m, para que quepan S, M y el jefe en las grandes). Las menores que el mínimo se eliminan o se funden.
3. **Paredes:** escalables por defecto (más de 48°, apoyo arriba con cuerpo completo). Prohibidos los voladizos sobre paredes que parecen escalables. Una pared sin cima alcanzable cuenta como error de generación, no como diseño.
4. **Rampas:** al menos una por cada región elevada mayor que un umbral (propuesta: ≥ 200 m²); las pequeñas solo escalando. Rampas de anchura ≥ 4 m para que no sean cuellos de botella extremos.
5. **Corredores y pasos estrechos:** algunos pasos de 3–5 m entre mesetas o estructuras (decisiones de movimiento), nunca como única conexión de una región grande para la clase M.
6. **Caídas:** cualquier borde permite bajar cayendo (P0-B). No se generan fosos cerrados sin salida escalable.
7. **Estructuras:** separación mínima entre ellas y respecto a paredes (propuesta: ≥ 3 m) para que la horda circule. Tejados transitables si se pueden alcanzar. Nada de huecos donde el jugador quepa y el enemigo S no, salvo lo intencionado y registrado.
8. **Aparición:** jugador en el inicio. Enemigos solo sobre nodos válidos de su clase. Interactuables (si se incluyen) sobre nodos alcanzables por S.
9. **Verificación (criterio de jugabilidad):**
   - Toda región transitable es alcanzable por S desde el inicio.
   - El suelo principal es alcanzable por M y L.
   - Ninguna región de más de un umbral queda inaccesible para M.
   - Cero bolsas cerradas.
   - Cero sólidos flotantes sin apoyo, salvo pasarelas diseñadas.
   - Cero penetraciones entre sólidos y terreno.

## 7. Compatibilidad con la escalada y la navegación de P0-B

- La geometría se limita a lo que P0-A y P0-B ya validan: heightfield y cajas AABB con categoría. Los acantilados de una celda se escalan con la misma física que el terreno del circuito B.
- `NavGraph` descubre caminar, escalar y caer sin datos extra. Las conexiones falsas se evitan por construcción: los nodos exigen cuerpo libre y apoyo, las aristas de escalada exigen cara continua, columna libre y `Ledge`, y toda trayectoria se valida por muestreo.
- Tamaños: el generador mide la accesibilidad para S, M y L por separado y aplica las reglas de la sección 6.
- Mapa completo por clase (sin regiones) mientras el tamaño de P0-C lo permita. La navegación regional queda para P0-D: el plan deja la construcción preparada para trocearse (estructuras y nodos con coordenadas absolutas), sin implementarla.

## 8. Semillas y determinismo

- Flujos nuevos `new Rng(seed).Derive("p0c-…")`, independientes de los flujos U3 (`terrain`, `sites`, etc.): las referencias congeladas no se tocan.
- Generación pura en el núcleo C# (sin UnityEngine), con orden fijo, reparaciones deterministas y reintentos con subsemillas derivadas y contadas (`p0c-attempt-n`).
- Lista fija de semillas de prueba: semillas normales, semillas límite (máximo relieve, mínimo relieve, muchas estructuras) y las que fallen durante el desarrollo, que pasan a ser casos de regresión.
- Reproducción de una semilla fallida: una prueba EditMode parametrizada por semilla y un argumento de la build QA (`-p0-seed`) que vuelca un informe del trazado (regiones, rampas, paredes sin cima, inaccesibles) y una vista cenital.
- Sin cambios en progreso, guardado, IDs ni catálogo.

## 9. Archivos y sistemas afectados

- **Nuevos** en `Core/World/`:
  - `ProceduralLayout.cs`: niveles, mesetas, bordes y rampas.
  - `ProceduralStructures.cs`: plantillas y colocación.
  - `ProceduralValidation.cs`: grafo de regiones, reparación e informe.
  - `SolidGrid.cs`: rejilla espacial para `VerticalQueries`.
- **Modificados:**
  - `VerticalCircuit.cs`: tercer trazado C generado con semilla.
  - `VerticalQueries.cs`: usa `SolidGrid`, con resultado idéntico.
  - `U3/VerticalQa.cs`, `U3/U3Game.cs`: F10 alterna A, B y C; cambio de semilla en QA.
  - `U3/P0Capture.cs` / `P0PursuitCapture.cs`: medidas y evidencias de C por semilla.
  - `scripts/p0.ps1`: variantes `*c`.
- **Pruebas nuevas** en `Tests/Core` (generación, conectividad, semillas, rendimiento lógico) y `Tests/PlayMode` (circuito C en escena).
- **Sin cambios:** `src/`, `unity/Docs/Reference/`, generador U3 (`Heightfield.Generate`, `Sites`, `Props`, `Vegetation`, `InteractableSpots`, `WorldSpawns`), U6, catálogo, balance, director, progreso y guardado.

## 10. Subfases

| Fase | Contenido | Verificación |
| --- | --- | --- |
| C0 | Diagnóstico medido sin cambiar nada: construir los mapas P0-B sobre recortes del terreno U3 de varias semillas (coste, nodos, aristas, regiones inaccesibles) y fijar la referencia de coste. | Informe por semilla; sin cambios de comportamiento |
| C1 | Rejilla espacial de sólidos en `VerticalQueries` y perfil de construcción de mapas. | Resultados idénticos a la versión lineal en todas las pruebas P0-A/B; coste medido con 17 y con cientos de sólidos |
| C2 | Trazado de niveles: macroceldas, mesetas cuadradas, acantilados, zona de inicio y rampas esculpidas en el heightfield. | Determinismo por semilla; anchura mínima; pendientes de rampa ≤ objetivo; paredes de una celda |
| C3 | Estructuras alineadas a la rejilla: bloques con tejado transitable, torres, muros, pasarelas apoyadas, escaleras de bloques. | Sin penetraciones ni sólidos flotantes; separaciones; tejados con cima válida |
| C4 | Validación y reparación con `NavGraph` S/M/L, puntos de aparición y puntos clave, rechazo medido. | Criterios de la sección 6.9 en N semillas; tasa de reparación y rechazo |
| C5 | Circuito C en la build QA (F10 y semilla), horda mixta, benchmark 0/300/500/750 en varias semillas, evidencias. | Métricas de la sección 12; sin regresión de A y B |
| C6 | Regresiones completas, documentación, builds P0 y guía de prueba manual; detenerse. | — |

## 11. Pruebas y criterios de aceptación

| Nivel | Qué verifica |
| --- | --- |
| Lógica (EditMode, núcleo puro) | Generación idéntica por semilla. Mesetas de distintas alturas y anchura mínima. Rampas transitables (pendiente y anchura). Paredes escalables con cima (`NavGraph` crea la arista). Zonas elevadas con y sin rampa. Inaccesibles detectados. Sin penetraciones ni flotantes. Apariciones de los tres tamaños sobre nodos válidos. Estrés de 300/750 enemigos por semilla (sin penetraciones ni bajar del terreno, atascados ≤ 2 %, llegada ≥ 95 % en escenarios alcanzables). Coste de generación y de mapas. Asignaciones |
| Unity Editor (PlayMode) | Circuito C en la escena, cambio A/B/C, render del trazado, cámara contra acantilados sin rediseño |
| Build QA | `evidencec` (capturas por semilla con vista cenital), `benchmarkc` (0/300/500/750 en varias semillas, heredado frente a navegación), `devdiagc` (GC y memoria) |
| FPS reales | No se miden aquí; los benchmarks son de lógica del arnés (GPU N/D) |
| Regresión | Circuitos A y B (pruebas, evidencias, benchmark), suites completas, referencias U0–U4 congeladas, U6 SHA-256 y los siete ajustes protegidos |

**Criterios de terminado:**
- En el conjunto de semillas de prueba, el 100 % de los trazados aceptados cumple la sección 6.9.
- La tasa de reparación y de rechazo queda medida y documentada.
- Cero penetraciones y teletransportes en los estrés.
- Rendimiento dentro de lo que se acuerde en la sección 12, o desviación explicada con propuesta.
- U3, U6, las referencias y los circuitos A y B sin cambios.
- Prueba manual del autor.

## 12. Presupuesto y riesgos de rendimiento (propuestas a confirmar, no resultados)

Referencia medida en P0-B (circuito de 80 m, 17 sólidos): mapas en 648 ms; navegación 0,61 ms p95 con 750 enemigos.

- **Tamaño:** el coste crece con el número de celdas. Con un mapa P0-C de ~128 m de lado (2,6 veces el área del circuito B) se estiman 1,5–2 s de construcción de los tres mapas si la rejilla de sólidos evita el coste lineal con el número de estructuras. **Hay que medirlo en C0/C1**, no es una promesa.
- **Presupuestos propuestos:**
  - Generación del trazado (sin mapas): ≤ 300 ms.
  - Mapas S/M/L: ≤ 2 s al cargar.
  - Navegación en juego: no empeorar los límites aceptados de P0-B en más de un 10 %.
  - Publicación de un cambio de meta: ≤ 0,3 s (el campo crece con el mapa y comparte presupuesto por tick).
- **Riesgos y medidas:**
  - **Campo mayor:** más nodos significan más ticks para publicar una meta. Medida: medir; si no basta, subir el presupuesto de forma medida o adelantar con el autor una ventana local (frontera con P0-D).
  - **Muchas estructuras:** la rejilla espacial de C1.
  - **Más acantilados:** más escaladas y más aglomeración al pie de paredes. Medida: las rampas anchas de la regla 6.4 y las métricas de aglomeración ya existentes.
  - **Memoria:** arrays proporcionales a las celdas (estimación: decenas de MB para 128 m y tres clases), medidos en Development.

## 13. Riesgos técnicos y protecciones

- **Romper U3 o las referencias:** generador separado, flujos RNG nuevos, suites y `verify-historical-reference.ps1` en cada fase.
- **Conexiones falsas o paredes imposibles:** solo geometría que P0-A/B validan, comprobación con `NavGraph` real, informe de paredes sin cima.
- **Semillas patológicas:** reparación limitada, rechazo determinista y semillas fallidas convertidas en regresiones.
- **Coste de carga:** presupuesto explícito y medidas por semilla.
- **Alcance:** no ampliar el mundo (P0-D), no arte final, no tocar balance ni cámara.
- **Protecciones de siempre:** U6 SHA-256, siete ajustes fuera de commits, sin `src/` ni referencias, sin reset, clean, force-push ni Pull Requests.

## 14. Decisiones que necesita tomar el autor

1. **Dónde vive P0-C.** Recomendado: solo en la build QA como circuito C con semilla (F10 alterna A, B y C), sin sustituir el mundo U3 ni la entrega U6. Alternativa: sustituir ya el mundo del juego, lo que rompería las referencias congeladas y mezcla con P0-D.
2. **Tamaño del mapa de P0-C.** Recomendado: ~128 m de lado (mayor que los circuitos, menor que el mundo U3 de 320 m) para no necesitar navegación por regiones, que es P0-D. Alternativa: 160–200 m, con riesgo de coste de carga y de publicación del campo.
3. **Rotación de estructuras.** Recomendado: alineadas a ejes (giros de 90°), que es lo que la escalada valida hoy. Alternativa: rotación libre, que exige ampliar las consultas a cajas orientadas (más código y coste).
4. **Relieve.** Propuesta para discutir: 4 niveles (0–3) de 4,5 m (el escalón de terraza U3), un 30–40 % del área elevada, mesetas de 8–40 m de lado, macroceldas de 4 m y bordes con algo de irregularidad. ¿Qué cifras quieres ajustar?
5. **Rampas.** Propuesta: al menos una rampa (≥ 4 m de ancho, ≤ 30°) por cada región elevada de ≥ 200 m²; las pequeñas, solo escalando. ¿Más o menos rampas?
6. **Estructuras.** Propuesta: tejados planos transitables, torres, muros, pasarelas apoyadas y escaleras de bloques, con estilo de bloque provisional (no arte final). ¿Incluimos versiones de casa, templo, granja y pozo como bloques con tejado transitable, o solo formas genéricas en esta fase?
7. **Contenido del mapa QA.** ¿Incluir interactuables (cofres, santuarios, tótems, portal) y vegetación (árboles como obstáculo no escalable) en el circuito C para probar su colocación sobre nodos alcanzables, sin cambiar sus reglas? Recomendado: sí a la vegetación como obstáculo y a puntos de interactuables solo como marcadores QA.
8. **Política de inaccesibles.** Recomendado: ninguna región transitable inaccesible para S; el jefe (L) puede no alcanzar mesetas pequeñas; cero bolsas cerradas. ¿De acuerdo, o quieres permitir algún punto elevado intencionadamente seguro?
9. **Semillas en QA.** Propuesta: la semilla del juego (mismo formato de texto) y una tecla libre (F11) para pasar a la siguiente semilla de una lista fija, visible en el HUD. ¿Te sirve?
10. **Presupuestos.** ¿Aceptas como objetivo inicial los de la sección 12 (trazado ≤ 300 ms, mapas ≤ 2 s al cargar, navegación ≤ +10 % sobre los límites de P0-B, meta ≤ 0,3 s)?
