# Decisiones acordadas

Registro de lo que se ha decidido durante el desarrollo, además de la
[especificación original](ESPECIFICACION.md). Cuando esto la concreta o la
cambia, manda esto. Los números exactos viven en `src/data/` (lo de aquí es un
resumen para no perder el contexto entre sesiones).

## Antes del hito 1: respuestas del autor

1. **Hitos:** parar después de cada hito, hacer push y esperar a que lo pruebe
   antes de seguir.
2. **Deslizarse:** Shift y C por defecto. Ctrl solo si se activa en Opciones,
   con aviso (en Chrome y Edge, Ctrl+W cierra la pestaña y una web no puede
   impedirlo). Durante la partida, diálogo "¿Salir de la página?".
3. **4.ª carta al subir de nivel:** la da un objeto raro de partida, "+1 opción
   al subir de nivel", con un máximo de 4. Hecho en el hito 4: la **Baraja del
   Tute**.
4. **Sin nivel máximo** para armas y tomos. Topes solo en estadísticas que lo
   necesiten por rendimiento o jugabilidad. Cartas de relleno (oro o curación)
   si el sorteo se queda sin nada.
5. **Node:** `.nvmrc` (24), `engines` >= 22.12 en `package.json`, aviso claro si
   la versión es anterior (`scripts/check-node.cjs`) y en el README cómo
   comprobarla e instalarla. Versiones actuales de Vite (8) y Vitest (5).
6. **Tono:** aprobado tal cual. MAMPORRO, Doña Remedios con la Chancla
   Teledirigida y Sir Baguette. Seguir esa línea con el resto del contenido.

El autor aceptó además el plan de 6 hitos y estas propuestas:

- **Controles:** E para usar baúles, tótem y portal; cartas con clic o 1–4; Esc
  suelta el ratón y pausa (el navegador exige un clic para recapturarlo).
- **Duración:** 10 minutos por defecto, con selector 5/10/15. La dificultad se
  ajusta a la duración elegida. (Hecho en el hito 4.)
- **Semilla:** visible en la pausa y en los resultados; campo opcional al
  empezar.
- **Debug (F3):** funciona también en la build final. Las partidas con trucos
  no dan moneda meta ni cuentan para las misiones (`run.cheated`; lo segundo,
  en el hito 5).
- **Desbloqueos (hito 5):** al empezar, 1 personaje, 4 de las 6 armas y 8 de los
  12 objetos. Las misiones desbloquean contenido concreto y dan moneda. La
  tienda vende el resto, el 2.º personaje y usos extra de Reroll/Saltar/Descartar.
- **Personajes (hito 5):**
  - *Doña Remedios* (desbloqueada): arma inicial la Chancla Teledirigida;
    pasiva, su mirada de desaprobación ralentiza a los enemigos cercanos.
  - *Sir Baguette, Paladín del Pan Duro* (desbloqueable): arma inicial la Barra
    de Pan Duro (ataque en arco). Su pasiva está por decidir: proponerla.
- **Fuente pixelada:** glifos dibujados en código y convertidos en una fuente
  real con `opentype.js` (MIT).
- **Dependencias:** `three`, `simplex-noise` y `opentype.js`; para desarrollo,
  `typescript`, `vite`, `vitest` y `@types/three`. Playwright solo se usa fuera
  del proyecto, para probar en navegador.
- **Imagen:** 360 px de alto por defecto (240/360/480); temblor de vértices y
  dithering se pueden desactivar.
- **Música (hito 6):** bucle chiptune generado por código, con volumen separado
  para música y efectos, y silencio.
- **Navegadores:** Chrome, Edge y Firefox de escritorio recientes.
- **Idiomas:** el código en inglés; comentarios, README y commits en español. En
  el primer arranque se usa el idioma del navegador.
- **Git:** commits pequeños por hito; no abrir PR salvo que el autor lo pida.

## Hito 1: base

- Mapa de 320 m de lado, unos 270 m jugables, con montañas en el borde.
- Movimiento: no se camina por pendientes de más de 48°; salto de unos 1,8 m;
  los impulsos al deslizarse no pasan de 1,8 veces la velocidad de carrera, y
  cuesta abajo se puede llegar a 30 m/s. Al deslizarse, la abuela se echa hacia
  atrás (modo tobogán).
- Pausa automática al perder el foco; Esc funciona aunque el ratón no esté
  capturado.
- La pantalla de inicio es provisional (el menú completo llega en el hito 5);
  las opciones se adelantaron a la pausa.
- `?test` en la URL desactiva la captura del ratón para las pruebas automáticas.
- El aviso de Node también sale con 23 y 25 (no son LTS; Vitest 5 no las
  soporta oficialmente).
- Limitación conocida: la cámara solo choca con el terreno (puede atravesar
  árboles y rocas grandes).

## Hito 2: combate

Al empezarlo, el autor pidió más detalle en el personaje y en el mundo (ruinas,
casas derruidas...) y un poco más de velocidad: velocidad base 9,5 m/s.

- Pelusa: 14 de vida, 4,2 m/s y 8 de daño. Cucaracha: 7 de vida, 7,2 m/s y 5
  de daño, desde el 0:45.
- Chancla: 10 de daño cada 0,85 s, va al más cercano y atraviesa 1. Naftalina:
  6 de daño cada 0,5 s a menos de 3,2 m.
- Experiencia: 10 puntos para el nivel 2 y cada nivel cuesta más.
- Críticos: 5 % de base; por encima del 100 %, supercríticos que suman daño.
- Armadura: el daño recibido se multiplica por 100 / (100 + armadura).
- Dificultad: al principio 0,9 enemigos por segundo y 60 vivos como máximo; el
  máximo sube 45 por minuto hasta 450; la vida se multiplica por 2,6 en el
  minuto 5 y por 5,2 en el 10.
- 0,7 s de invulnerabilidad tras un golpe (Doña Remedios parpadea).
- Los enemigos suben a superficies bajas y te alcanzan hasta 1,6 m por encima.
- Después, a petición del autor:
  - los enemigos frenan "un poco" al atravesarlos: solo los que empujas de
    frente, según su masa, como mucho un 40 % (`CROWD_CONFIG`);
  - silueta dorada de Doña Remedios a través de lo que la tape;
  - "más tipos de enemigos y armas para el futuro (si ya está en un hito,
    dejarlo así)": los enemigos nuevos eran del hito 4 y ya están; las armas son
    las 6 de la especificación.

## Hito 3: progresión en partida

- Rarezas: pesos 60 / 25 / 10 / 4 / 1; la Suerte multiplica cada peso por
  (1 + Suerte/100 × bonus de esa rareza).
- Carta Común de arma: daño +25 %, cadencia +12 %, cantidad y perforación +1,
  área, velocidad y duración +15 %. Una Legendaria multiplica por 2,7.
- Carta Común de tomo: daño +12 %, cadencia +10 %, cantidad +1, área +12 %,
  velocidad +8 %, vida +20 y +0,4 por segundo, suerte +10, recogida +25 % y
  experiencia +8 %.
- Las cartas de arma nueva no tienen rareza.
- 2 usos de Reroll, Saltar y Descartar por partida. Saltar no da nada.
- Descartar quita esa arma o ese tomo del sorteo (también sus mejoras) y pone
  otra carta en su hueco.
- Teclas R, X y B (S y D son de movimiento), con 0,4 s de espera al abrir.
- La carta de relleno cura un 30 % (desde el hito 4 también hay una de oro).
- El suelo fregado ralentiza un 35 %. Esa ralentización servirá para la pasiva
  de Doña Remedios.
- Se adelantaron del hito 5 las estadísticas del personaje en la pausa.

## Hito 4: mapa vivo

Probadas y confirmadas por el autor antes de empezar el hito 5 (28/09/2026):

1. **Partidas cortas o largas:** la curva de dificultad se comprime o se estira
   a la duración elegida; en 5 minutos los enemigos dan el doble de experiencia
   y oro, y en 15, dos tercios.
2. **Jefe (la Pelusa Madre):** se invoca cuando se quiera abriendo el armario
   escondido. Su vida es 3000 × (1 + 0,2·m + 0,06·m²), con m el minuto de
   dificultad, porque el daño del jugador se dispara con la partida. Tres
   ataques avisados (rodillo, culetazo y estornudo); con media vida se enfada.
   No se le puede atravesar: aparta al jugador.
3. **Enjambre final:** el ritmo se duplica cada 20 s (hasta 60 por segundo y 750
   vivos). Al empezar, el armario se revela en el minimapa si no se había
   encontrado, para que haya salida.
4. **Baúles:** 14 por mapa; dan un objeto al azar (sin elegir) y se enseña en un
   cartel. Precios: 15, 30, 49, 74, 103...
5. **Mesa camilla (santuario):** 3 por mapa. Se carga en 9 s dentro de su
   círculo; mientras, aparecen más enemigos; fuera se descarga despacio. Da a
   elegir 1 de 3 bendiciones con rareza, sin Reroll, Saltar ni Descartar.
6. **Tótem de cacerolas:** 2 por mapa. 45 s con más enemigos (×2,2) y más vida
   (×1,25), pero el doble de oro y +50 de suerte; al superarlo, un objeto con
   +100 de suerte.
7. **Cartas de relleno:** una que cura y otra que da oro (la mitad del precio
   del siguiente baúl).
8. **Bata de Guatiné:** te levanta con media vida, aparta a los enemigos y se
   gasta.
9. **Minimapa:** el mapa entero con el norte arriba; los interactuables salen al
   acercarse a unos 30 m (el armario, a 22 m).
10. **Enemigos nuevos:** Táper Caducado (tanque), Paloma Okupa (a distancia) y
    Rata de Gimnasio (élite que embiste, cada 2 minutos de dificultad). Oleadas
    especiales en los minutos 1,5, 3, 4,5, 6, 7,5 y 9.
11. **12 objetos** con sinergias (tabla en el README); nombres y efectos en
    `src/data/items.ts`.
12. Tras vencer al jefe pasan 1,6 s antes de los resultados, para verlo
    reventar.

## Hito 5: meta y UI

El autor aprobó el plan y las siguientes decisiones antes de programar
(28/09/2026). Implementación entregada para que la pruebe.

### Personajes y contenido

- **Doña Remedios:** Chancla Teledirigida y mirada de desaprobación: ralentiza
  un 20 % a enemigos a 3 m y a menos de 2 m de diferencia de altura. No suma
  ralentizaciones; prevalece la más fuerte. La mirada no prolonga la duración
  de una ralentización superior, como la del suelo fregado.
- **Sir Baguette:** Barra de Pan Duro y Corteza defensiva. Tras 8 s sin recibir
  daño bloquea un golpe y recarga desde cero. Empieza sin escudo, el daño real
  reinicia la carga y bloquear concede la invulnerabilidad breve habitual para
  que una horda no anule el bloqueo en el mismo tick. Indicador en el HUD.
  Mismos atributos base que Remedios; modelo procedural con casco, armadura,
  escudo de hogaza y barra de pan, con animación y silueta compartidas.
- **Armas iniciales:** Chancla, Naftalina, Barra y Dentaduras.
- **Objetos iniciales:** Gafas, Zapatillas, Termo, Cojín, Lupa, Décimo, Rulos y
  Monedero. Los ocho tomos están disponibles desde el principio.
- **Por misiones:** Suelo Recién Fregado, Collar de Perlas y Bata de Guatiné.
- **Tienda:** Sir Baguette (220), Jersey Estático (140), Baraja del Tute (160)
  y Olla Exprés (180). Precios en Calderilla del Caos.
- Los desbloqueos amplían el catálogo de futuras partidas; no entregan el
  objeto directamente ni cambian la partida que acaba de terminar.

### Economía y misiones

- **Calderilla del Caos**, separada del oro de partida. Por morir o ganar:
  `floor(bajas / 20) + min(60, floor(segundos / 15)) + (victoria ? 60 : 0)`.
  A eso se suman recompensas de misiones nuevas. El resultado desglosa todo.
- Objetivo orientativo aprobado: Sir Baguette tras 2–3 partidas razonables.
  Depende del rendimiento y de las misiones; pendiente de balance jugando.
- Cada ampliación permanente de Reroll/Saltar/Descartar cuesta 80, 140 y 220.
  Cada acción comienza con 2 usos por partida y puede llegar a 5. No son
  consumibles que haya que volver a comprar. No hay mejoras globales de stats.
- Ocho misiones, siempre activas, de recompensa única y concesión automática:

| Misión | Alcance | Calderilla | Desbloqueo |
| --- | --- | ---: | --- |
| Terminar una partida | Victoria o derrota | 30 | — |
| Matar 1000 enemigos | Acumulado | 60 | Suelo Recién Fregado |
| Abrir 10 baúles | Acumulado | 40 | — |
| Completar 3 mesas camilla | Acumulado | 40 | — |
| Superar un tótem | Una vez | 50 | Collar de Perlas |
| Alcanzar el nivel 20 | En una partida | 50 | — |
| Vencer al jefe | Una vez | 80 | — |
| Ganar sin adquirir el tomo de vida | En una partida | 100 | Bata de Guatiné |

### Guardado, opciones y límites

- Guardado v2 con migración desde v1, conservando idioma y ajustes. Valida IDs,
  saldos, rangos, duplicados, misiones y selección de personajes desbloqueados.
- Guarda moneda, contenido, ampliaciones, misiones, selección y opciones.
  **No reanuda partidas en curso**. Cerrar o abandonar no concede recompensas
  ni progreso parcial. Abandonar desde pausa pide confirmación.
- `run.cheated` invalida toda la partida, aunque se apaguen luego los trucos.
  F3 para consultar métricas no invalida. Las manipulaciones de estado de los
  ganchos `?test` también marcan trucos.
- La liquidación se protege en el orquestador y mediante el ID de la última
  partida guardada. Recargar o volver a resultados no duplica el pago.
- Si localStorage no está disponible o falla al escribir, el juego continúa
  en memoria y avisa en menú/resultados. No se promete persistencia en ese caso.
- Música al 50 %, efectos al 70 %, silencio desactivado por defecto. Ajustes
  preparados para hito 6; la interfaz lo indica. Siguen las opciones retro,
  sensibilidad, FPS, Ctrl e idioma del navegador en primer arranque.

### Verificación del hito 5

- Typecheck, 188 tests de 24 ficheros y build correctos. Incluye migración v1,
  validación, compras, topes, objetivos acumulados, cobro único, exclusión de
  trucos, restricciones de ofertas/botín y las dos pasivas.
- Chromium 154 con SwiftShader y `?test`: navegación, 8 misiones, compras por
  interfaz, máximos, selección/arma inicial, abandono, victoria con trucos sin
  recompensa, opciones e idioma persistentes. Sin errores de consola.
- Capturas revisadas: español a 1280×720 e inglés a 1600×900, además de partida
  con Sir Baguette y resultados. Los paneles largos permiten desplazamiento.
- Integración de una derrota normal en navegador: simulación a 60 Hz omitiendo
  renders intermedios, sin trucos; 73,88 s, 80 bajas, 38 monedas (4 bajas + 4
  supervivencia + 30 primera misión). Repetir liquidación no cobra de nuevo y
  recargar conserva el progreso. Esta prueba no mide tiempo real ni FPS.
- Benchmarks Node: aproximadamente 0,42 ms/tick con 500 enemigos, 0,79 con las
  cuatro armas nuevas y 0,99 con 750 enemigos, jefe y proyectiles. No equivalen
  a FPS reales. Pointer Lock real y rendimiento en el equipo del autor siguen
  pendientes, igual que la comprobación manual en Edge y Firefox.

## Limitaciones conocidas

- La cámara solo choca con el terreno.
- El daño por contacto de los enemigos no crece con el tiempo (solo su vida).
- La captura real del ratón (Pointer Lock) y los FPS reales solo los puede
  comprobar el autor: las pruebas automáticas usan `?test` y una GPU por
  software.

## Después del hito 6: migración propuesta a Unity (28/09/2026)

- Solicitada por el autor: terminar y validar primero el pulido; plantear Unity
  antes de incorporar más contenido.
- Guía en [MIGRACION_UNITY.md](MIGRACION_UNITY.md), fases U0–U6 con validación
  independiente. Se conserva Three.js como referencia hasta aprobar el reemplazo.
- Es una planificación, no autorización para crear ya el proyecto Unity. Plataforma,
  versión exacta, UI y traslado del guardado se concretarán al arrancar la migración.

## Hito 6: pulido (28/09/2026)

- Plan aprobado por el autor: música arcade cómica, variante intensa para jefe y
  enjambre, efectos moderados, opciones para reducirlos y balance justificado.
- Audio original PCM a 22050 Hz, sintetizado una vez tras el primer gesto, con
  dos arreglos de ocho compases a 132 BPM. Un contexto WebAudio, una pista musical,
  volúmenes independientes y compresor. La pausa atenúa música y ocultar pestaña
  suspende el contexto y elimina efectos; no hay cola de sonidos al volver.
- Hasta 16 efectos simultáneos; 4 espacios reservados para señales importantes.
  Cada timbre tiene enfriamiento. Se limita tanto la admisión lógica como los
  nodos pendientes: el reloj de audio puede avanzar mientras JS retrasa `onended`.
  Los sonidos descartados no se reproducen más tarde.
- Los eventos nuevos `weaponFired` y `pickup` mantienen la lógica independiente de
  audio/render. Los buffers se reutilizan; la aleatoriedad sonora es independiente
  de la semilla de partida. Tests comparan simulaciones con/sin efectos.
- Partículas decorativas: 1500 activas/256 nuevas por frame; reducidas: 400/64 y
  25 % de densidad por ráfaga (redondeo hacia arriba para efectos pequeños).
  Mejora de críticos, muertes especiales y escudo, sin alterar avisos de ataques.
- Tres opciones persistentes: reducir partículas, sacudidas de cámara y destellos
  de daño. El último controla flashes de enemigos, parpadeo del jugador y HUD.
  El snap PS1 conserva su opción separada. Migración v3 conserva la meta de v2;
  las opciones nuevas arrancan con partículas normales, sacudidas/destellos activos.
- En Opciones del menú se eliminan título e idioma duplicados; la pausa mantiene
  sus encabezados. Guía ampliada de armas, tomos, objetos y enemigos en README.

### Revisión de balance y límites de la evidencia

No se cambian números de combate/economía sin evidencia de partidas representativas.
Se añade una matriz reproducible: semilla `PULIDO-REFERENCIA`, quieto mirando al
norte, primera carta de cada elección, sin compras/trucos, hasta muerte o 120 s.
Es una prueba inicial de regresión, **no una medición de equilibrio entre personajes**:
la Chancla busca objetivos y la Barra exige orientar el golpe. No sustituye jugar
partidas completas de 5/10/15 minutos.

| Personaje | Duración elegida | Supervivencia (s) | Bajas | Nivel |
| --- | --- | ---: | ---: | ---: |
| Remedios | 5 min | 69,18 | 131 | 7 |
| Remedios | 10 min | 97,50 | 152 | 5 |
| Remedios | 15 min | 112,43 | 130 | 3 |
| Baguette | 5 min | 40,25 | 42 | 5 |
| Baguette | 10 min | 21,58 | 10 | 2 |
| Baguette | 15 min | 21,77 | 10 | 1 |

Con y sin los eventos sonoros, los estados finales coinciden exactamente. La
menor supervivencia de Baguette en este ensayo señala que hay que probar orientación
y movilidad con el autor; no justifica por sí sola mejorar sus estadísticas.
Benchmarks de lógica en este entorno: ~499 enemigos 1,08 ms/tick; 500 con cuatro
armas mejoradas 2,01 ms; 750 con jefe y pipas 2,77 ms. No incluyen render ni prueban
60 FPS reales. Pruebas reproducibles en `core/polish.test.ts` y `systems/combat.test.ts`.

Fuentes de integración WebAudio: [MDN AudioContext](https://developer.mozilla.org/en-US/docs/Web/API/AudioContext),
[resume](https://developer.mozilla.org/en-US/docs/Web/API/AudioContext/resume) y
[suspend](https://developer.mozilla.org/en-US/docs/Web/API/AudioContext/suspend),
consultadas el 28/09/2026. Audio iniciado con gesto y suspendido al ocultarse.


### Verificación de entrega del hito 6

202 tests / 27 ficheros, typecheck y build correctos. Navegador Chromium con
SwiftShader y `?test`: ES/EN, 1280×720 y 1600×900, sin errores de consola. Se
verificaron opciones y persistencia v3, 16 nodos máximos de efectos, silencio,
suspensión/reanudación, PCM con OfflineAudioContext, combate, jefe y pausa.
Capturas revisadas. Script reproducible: `scripts/verify-browser.cjs`.
Pendientes del autor: escucha y mezcla en su equipo, Pointer Lock, FPS reales y
balance jugando partidas completas. Se detiene el desarrollo para esa prueba.


## U0: arranque de Unity y objetivos posteriores (28/09/2026)

Respuestas del autor tras probar y aprobar el hito 6:

1. Windows de escritorio, pensando en publicar en Steam/plataformas similares.
2. Dispone de Unity 6.6; aún no se conoce la revisión exacta mostrada por Hub.
3. Conservar todo el progreso que se pueda trasladar de forma compatible.

Base fijada: `0505b1690656d15188860157612455639820fe1f`. Referencia exportada en
`unity/Docs/Reference/baseline.json`: catálogo, i18n, RNG, fórmulas, guardados,
semillas/mundo y audio; capturas y muestras conservadas junto al informe de QA.
Comprobación: `node scripts/unity-reference.mjs`. Solo preparación U0: todavía
sin proyecto abrible en Hub ni código C# o escenas verificados.

El autor pide para Unity: mundo y estructuras mayores, colinas más cuadradas,
rampas y desniveles transitables, escalada de paredes y movilidad ágil; más
monedas; menos enemigos al principio y aumento progresivo; mejor acabado visual,
imágenes para armas/tomos/etc. y más enemigos, armas, tomos y personajes.
La hoja de ruta está **dentro de `unity/Docs/HOJA_DE_RUTA.md`**, con prioridades,
criterios de prueba y decisiones abiertas. No se aplican esos cambios al juego web.

Recomendaciones, aún no aprobadas como alcance: cámara con colisión en edificios,
mando/reasignación, introducción breve, métricas locales de balance, accesibilidad
y preparación de servicios de tienda. No se ha integrado Steamworks ni publicado.
Detalles de escalada, tipo de moneda y producción del arte se preguntarán juntos;
no asumir que la petición de iconos autoriza assets de terceros.

La guía inicial proponía LTS, pero el autor ya tiene 6.6. Se comprobará su revisión
completa sin inventar paquetes ni obligar a cambiar a LTS por defecto. Política
oficial consultada: https://unity.com/releases/unity-6/support .


## Confirmaciones y traspaso a Proyecto de ChatGPT (28/09/2026)

- **Escalada libre**, elegida expresamente frente a una trepada breve. No se ha
  decidido control, superficies o comportamiento de enemigos en altura.
- **Más oro de partida soltado por enemigos**, con aumento progresivo según
  tiempo y dificultad. No es una petición de aumentar la moneda meta.
- **Arte original en archivos autorizado** para imágenes/iconos de armas, tomos,
  objetos, etc. Amplía la restricción procedural de la especificación original
  para la versión Unity; no aprueba automáticamente assets de terceros.
- El autor quiere centralizar contexto e instrucciones en un Proyecto de ChatGPT
  para trabajar en conversaciones distintas. Se prepara un paquete Markdown.
- La primera conversación nueva será instalar/verificar **Codex CLI en Windows**
  y trabajar desde la carpeta del repositorio junto al Editor Unity. Instalación
  local todavía pendiente; no suponer acceso automático a chats desde el CLI.
- Dice adjuntar captura de Unity, pero no hay imagen recibida/legible en esta
  conversación: sigue pendiente la revisión completa del Editor.
- U1 aún no ha comenzado. No se han creado escenas, C# ni proyecto abrible en Hub.
