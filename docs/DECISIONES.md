# Decisiones acordadas

## Preparación de B0 tras cerrar U6 (05/10/2026)

Solo planificación autorizada: [BLENDER_B0](BLENDER_B0.md) concreta seis pasos con dependencias, aceptación, evidencias y pruebas. Fuentes .blend fuera de unity/Assets; FBX como intercambio a validar. Instalación real primero cuando se autorice; ninguna instalación, ejecución Blender, modelo o integración ahora. Dos propuestas para el autor: trío representativo Pelusa/Remedios/módulo de pared y criterio de comparación de rendimiento; no son decisiones aprobadas. No adoptar animación masiva, cambiar colliders ni activar LFS por anticipación.

## Aprobación manual de U6 y cierre de la migración (05/10/2026)

El autor confirma expresamente que U6 está probado y aprobado manualmente. Último cierre técnico previo: `c84b62dfedabdcbc5ce7c1da09916282fe1c67e0`. **U0–U6 terminados; migración web → Unity formalmente cerrada.** Unity es la versión principal; entrega Windows `MAMPORRO.exe` y transición segura del progreso histórico ya implementadas. Web ejecutable congelada, recuperable y con exportador; historial y corpus intactos. Se conservan limitaciones y diferencias aceptadas, sin inventar pruebas nuevas ni certificaciones individuales.

B0 queda autorizado **solo para planificación**, no para implementación. Revisar [BLENDER_B0](BLENDER_B0.md), presentar pasos, aceptación y dudas reales, y esperar autorización expresa. Las mejoras posteriores no pertenecen a la migración cerrada. Los siete ajustes locales Unity siguen protegidos; productName ya publicado, ninguna otra excepción.

## Cierre técnico de U6 (05/10/2026)

U6 implementado y verificado; **pendiente de la prueba manual y la aprobación final del autor** ([PROGRESO_U6](PROGRESO_U6.md#checkpoint-al-terminar-u6)). Decisiones técnicas de implementación dentro de lo autorizado: la única build normal es la de entrega (`scripts\u3.cmd build` → `unity\Builds\Windows\MAMPORRO.exe`; `Builds\U3` deja de generarse y la de diagnóstico Development sigue en `Builds\U3Dev`); `scripts\mamporro.cmd package|verify` crea y comprueba el paquete local (carpeta, `LEEME.txt`, `MANIFIESTO.json` con SHA-256 por archivo y ZIP determinista) sin publicar nada; el progreso anterior (`Mamporro U1`) se ofrece para revisar y copiar con la pantalla de importación de U4 (al arrancar solo si el destino nuevo está vacío; si no, desde Opciones), sin moverlo, modificarlo ni fusionarlo; `-u6-smoke` es una comprobación rápida de la build con guardado temporal; la comprobación visual activa el foco del audio solo durante su medida cuando Windows no da el primer plano. Sin cambios de reglas, balance, contenido ni referencias. B0 sigue solo documentado.

## U6 autorizado y B0 Blender futuro (04/10/2026)

El autor autoriza consecutivamente los pasos 1–6 de U6; no repetir permisos generales. Web ejecutable congelada con exportador en el repositorio, sin gameplay nuevo ni publicación/despliegue web. Unity pasa a principal. Identidad aprobada: `MAMPORRO.exe`, producto `MAMPORRO`, empresa `Mamporro` (ya existente), build normal en `unity/Builds/Windows`, Development separado.

Excepción mínima: versionar únicamente `productName: Mamporro U1` → `productName: MAMPORRO` en `ProjectSettings.asset`; el resto de diferencias locales y los otros seis archivos excluidos siguen protegidos. Aislar y comprobar el diff antes/después; detenerse si no puede hacerse sin pérdida/publicación accidental. Probar primero transición del guardado, sin tocar el personal: validación y revisión U4, confirmación, backup/escritura/verificación, sin fusión ni duplicación, original intacto y override QA totalmente aislado.

B0 — Spike Blender / pipeline de assets 3D queda confirmado como **trabajo futuro documentado**, después de cerrar y aprobar U6, no como implementación actual. Objetivos canónicos en [BLENDER_B0](BLENDER_B0.md) y hoja de ruta. Referencia prevista por el autor Blender 5.2.2 LTS, instalación no comprobada; verificar ruta/versión en CMD al empezar B0. `.blend → FBX → Unity`, automatización background/Python, separación VisualRoot/lógica, comparación medida de animación masiva, binarios/LFS solo tras medir y arte original con revisión humana. Sin instalación/plugins/assets de terceros ni telemetría ahora.

Al terminar U6, detenerse para prueba manual; no iniciar B0, balance ni otra mejora posmigración.

## Aprobación manual de U5 y preparación de U6 (04/10/2026)

El autor comunica que la prueba manual ha ido bien y **aprueba U5**. Último cierre técnico previo: `5ef42b6083ea15251308dcebac60cb851ca766ea`. La aprobación es global; no se inventan mediciones nuevas ni resultados individuales de dispositivos. Las diferencias y limitaciones de `EQUIVALENCIA_U5.md` se conservan como parte de la base aprobada, sin reabrir funciones ya portadas.

U6 queda autorizado **solo para planificación**. Presentar alcance, pasos y decisiones importantes y esperar respuesta antes de implementar. Las mejoras de balance, mundo, escalada, arte/animación y contenido siguen después del cierre de la migración. Sin cifras nuevas; más oro significa oro de partida, no Calderilla. Cámara contra estructuras, mando/remapeo, métricas adicionales y Steam no pasan automáticamente a U6.

Los picos finales conservados son 30,8 y 41,9 ms; 58 ms corresponde a una pasada anterior. Los benchmarks solo describen sus condiciones medidas, no rendimiento universal. En esta aprobación no se repiten suites ni se modifica runtime.

**Estado (histórico, 04/10/2026):** U1–U4 aprobados; U4 aprobada manualmente el 04/10/2026. U5 aprobado manualmente por el autor el 04/10/2026; U6 entonces solo en planificación (implementado el 05/10/2026, ver la entrada superior); véase [PROGRESO_U5](PROGRESO_U5.md) y [EQUIVALENCIA_U5](EQUIVALENCIA_U5.md). Las autorizaciones anteriores son históricas.

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


## 29/09/2026 — revisión del Editor confirmada

- Captura del autor leída: **Unity 6.6 (6000.6.3f1)**; Hub muestra Compatible y Descarga completa.
- Sustituye el pendiente anterior de recibir la versión exacta. Quedan ruta, módulos, compatibilidad de paquetes y ejecución local.
- No acredita una build ni un proyecto Unity abierto. U1 sigue sin iniciar; el siguiente bloque es preparar Codex CLI en Windows.

## 29/09/2026 — entorno local comprobado y checkpoint de preparación

- Codex CLI 0.158.0 ha arrancado en Windows y leído los documentos del repositorio.
  Git 2.54.0.windows.1, Node 24.21.0 y npm 11.19.0 responden a sus consultas de versión.
- Editor localizado en
  `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`, sin arrancarlo.
  Sus metadatos confirman `6000.6.3f1_45d8eee7de74`, coherente con la captura.
- Archivos de soporte Windows Mono detectados para x86, x64 y ARM64.
  Windows IL2CPP ausente entre las variantes instaladas; su entrada en el catálogo
  de módulos y las herramientas IL2CPP generales no acreditan ese soporte.
- Herramientas C++ y Windows SDK no detectados en PATH, rutas habituales y
  registros consultados. No se descartan instalaciones personalizadas sin registrar.
- Pendientes: arranque del Editor, licencia operativa, importación de paquetes,
  escenas, compilación C# y build. No se han ejecutado pruebas Unity ni navegador
  en este bloque; las evidencias anteriores conservan su fecha y alcance.
- Se añade `AGENTS.md` como entrada breve y
  [ENTORNO_LOCAL_CODEX.md](ENTORNO_LOCAL_CODEX.md) con rutas, versiones, límites,
  fuentes oficiales y pasos para retomar desde CMD. Sustituye los pendientes
  históricos de revisión del Editor, localización y arranque de Codex.
- **Propuesta de U1 no aprobada:** Windows x64 Mono, URP 17.6 e Input System 1.20.0.
  No se fija una revisión de URP sin comprobarla. U1 no ha comenzado; no se crean
  `Assets/`, `Packages/` ni `ProjectSettings/`. Se mantiene la aprobación por bloques.
- Comprobaciones locales de este bloque: typecheck, 202 tests/27 ficheros y build
  web correctos. `node scripts\unity-reference.mjs` falla con salida 1 y
  `AssertionError [ERR_ASSERTION]: La referencia difiere: no regenerar para ocultar una regresión.`
  La salida muestra diferencias CRLF/LF y de hashes, aún sin diagnóstico completo.
  No se modifica la referencia ni se usa `--write`; commit y push detenidos por
  instrucción del autor hasta resolver el fallo. Detalle en el checkpoint.

## 29/09/2026 — corrección del verificador de U0 en Windows

- Diagnóstico confirmado: `core.autocrlf=true` convertía los textos locales a
  CRLF. Coincidían 0/127 hashes fuente sin transformar y 127/127 tras normalizar
  solo CRLF a LF, idénticos también a los blobs de la base aprobada.
- La referencia calculada en memoria tenía únicamente esas 127 diferencias de
  hashes: catálogo, traducciones, RNG, fórmulas, guardados, mundos y audio sin
  diferencias, tampoco numéricas. El texto de baseline contenía 5022 CRLF.
- Las cuatro huellas binarias del manifiesto coincidían. La de `report.json`
  coincidía tras la misma normalización de finales de línea.
- Corrección autorizada en U0: normalizar solo CRLF a LF en fuentes `.ts`/`.css`
  bajo `src/`, `package-lock.json`, el texto de baseline y la huella de report,
  con rutas explícitas para los JSON. Los demás archivos permanecen byte a byte.
  Se conservan el control contra el commit base y los hashes de archivos locales.
- Sin reformatear JSON, quitar espacios, introducir tolerancias, cambiar Git,
  regenerar referencias o alterar medios. No se usa `--write`.
- Cuatro pruebas aisladas con buffers sintéticos pasan: equivalencia LF/CRLF,
  cambios reales detectados, conservación de otros bytes/formato y detección de
  alteraciones binarias. Ejecución: `node --test scripts\unity-reference-bytes.test.mjs`.
- Verificaciones posteriores correctas, todas con salida 0: typecheck, 202 tests
  de 27 ficheros, build web, referencia U0 (datos y medios coinciden) y
  `git diff --check` (solo avisos de conversión LF/CRLF). Sin reinstalar dependencias.
  El fallo inicial queda resuelto y permite la entrega autorizada tras revisión
  del diff y del remoto. No se atribuyen nuevas pruebas de navegador o Unity;
  U1 continúa sin empezar.

## 29/09/2026 — autorización e implementación exclusiva de U1

- El autor aprueba expresamente U1 y continuar tras un plan breve sin volver a
  pedir permiso por el alcance. Sustituye las menciones históricas de aprobación
  pendiente. Rama actual, commits y push directos, sin PR; U2 no autorizado.
- Proyecto en `unity/`, conservando documentación y referencias. Windows x64,
  Mono, Editor 6000.6.3f1. Catálogo local e importación confirman URP 17.6.0 e
  Input System 1.20.0. uGUI 2.6.0 y Test Framework 1.8.0 efectivos del Editor.
- Prototipo original: patio con rampas, pendientes y obstáculos, personaje
  provisional, tercera persona, teclado/ratón, salto y deslizamiento con valores
  de referencia web. Escalada libre y cámara con colisión completa quedan fuera.
- Motor de movimiento separado del adaptador de entrada/presentación; datos en
  ScriptableObject. Colisiones analíticas sobre la misma malla triangular del
  patio. No se afirma equivalencia completa del controlador web.
- Horda: arrays reutilizables, actualización central a 60 Hz, rejilla espacial,
  persecución/separación y rodeo local. Render instanciado; sin Rigidbody/Update
  por enemigo. Capacidad técnica de almacenamiento ampliable, no límite de diseño.
  Burst/Collections aparecen como dependencias transitivas de URP; no se usa DOTS/ECS.
- Presentación: RenderTexture puntual, relación de aspecto de salida, UI uGUI a
  resolución de pantalla; interna 360 con 240/480. Niebla e iluminación sencillas,
  shader propio con dither y ajuste de vértices conmutables. Sin assets de terceros.
- Ensayos aprobados: 300 habitual con objetivo 60 FPS en el equipo de referencia;
  500/750 margen; 1000 estrés sin promesa. Salidas 1920×1080 y 2560×1440. No son
  balance, máximos del juego ni requisitos comerciales. Semilla/recorrido fijos,
  calentamiento 10 s y medición 30 s por carga, solo archivos locales.
- Hardware consultado sin números de serie: Ryzen 7 7700X, RTX 4070 Ti SUPER,
  AMD integrada, aproximadamente 32 GB. Sustituye las denominaciones iniciales
  «Ryzen 3600k» y «4070 Ti». No extrapolar a equipos modestos.
- La base web y los medios permanecen intactos. Progreso compatible, escalada,
  mundo ampliado, ritmo, oro e iconos originales conservan sus acuerdos para los
  bloques posteriores. No se implementan importador, Steam, mando ni compras.
- Resultados efectivos y límites, incidencias de ejecución y siguiente paso en
  [PROGRESO_U1.md](PROGRESO_U1.md); medidas detalladas locales, no telemetría.

## U1 aceptado y U2 autorizado (29/09/2026)

El autor confirma funcionamiento de U1 en build y Editor y acepta el control básico.
Autoriza U2 completo: núcleo y combate equivalentes a TypeScript, seis armas, ocho
tomos, doce objetos, dos personajes y seis comportamientos enemigos en escenarios
controlados. Incluye ofertas, progresión dentro de partida y QA sin persistencia.
No incluye director/mundo/interactuables/victoria U3 ni meta U4. No rebalancear.
Los antiguos pendientes de revisión U1 y autorización U2 quedan sustituidos.
Plan, cambios heredados, pruebas y siguiente paso: [PROGRESO_U2](PROGRESO_U2.md).

## U2: decisiones del autor tras auditar el trabajo local (29/09/2026)

Tomadas por el autor tras revisar la auditoría del trabajo de una sesión anterior de
Codex que quedó sin terminar. Detalle y estado en [PROGRESO_U2](PROGRESO_U2.md).

- **Referencia complementaria:** `scripts/unity-reference-u2.mjs` exporta
  `unity/Docs/Reference/u2-combat.json` desde `0505b16`, con guarda contra cambios en
  `src/`. Cubre rarezas, mejoras, agregación de estadísticas, ofertas con semillas fijas
  y escenarios deterministas por arma, enemigo y pasiva. Es la única fuente de valores
  esperados de U2 (sustituye a `WebFixtures.json`). No se tocan `baseline.json` ni
  `src/`; nunca `--write` sobre la base; no se recalculan esperados con el código C#.
- **Unity** (Edit/Play/build/benchmark) se ejecuta en la sesión local.
- **Oro de partida** incluido: caída de oro y contador con los valores actuales. Carta de
  relleno con la fórmula web `max(fillerMin, round(chestCost(baúlesAbiertos) × 0,5))`,
  que da 10 sin baúles. Sin baúles en U2.
- **Subida de nivel técnica en uGUI:** 3–4 cartas, clic, 1–4 y teclado numérico,
  R volver a tirar, X saltar, B modo descarte, Esc sale del descarte, 0,4 s de espera,
  textos ES/EN de las traducciones de la referencia; sin acabado final. El reinicio de
  partida pasa a F8.
- **Jefe y Rata élite** con todas sus reglas de combate, invocados desde el panel QA.
  Armario, calendario de élites y enjambre quedan para U3.
- **Panel QA** activo en la build U2; puede seguir en IMGUI, oculto durante el benchmark.
- **Benchmark U2** en build con combate real (varias armas, cartas aplicadas,
  proyectiles): 300/500/750/1000 × 1080p/1440p, mismas condiciones y `validRender` que
  U1; la invulnerabilidad se documenta si se usa.
- **Integración:** rebase del commit local del núcleo sobre `2b67ea5` (sin force-push).
  Se restauran los ajustes de Unity que solo difieren en espacios, `GraphicsSettings.asset`
  y `U1_Patio.unity`. No se publican nunca `ProjectSettings.asset` (identificador de
  nube), `PackageManagerSettings.asset` ni `URPProjectSettings.asset`.
- **Controles U2:** reinicio de partida en F8 e idioma en F7 (aprobado por el autor);
  todos los controles se documentan en `unity/README.md`.
- **Relevo:** solo escribe en la rama el agente activo. Antes de cada push, `git fetch`;
  si el remoto ha cambiado, parar y avisar al autor en lugar de reintentar.
- **Documentación de relevo:** `INSTRUCCIONES_PROYECTO.md` es la fuente de las reglas
  comunes y `CONTINUIDAD_AGENTES.md` desarrolla el protocolo de relevo al que remite;
  la norma de checkpoints «Cómo retomar» está en INSTRUCCIONES sin duplicar CONTINUIDAD.

## U2: decisiones técnicas de implementación (29/09/2026, propuestas por Claude Code y confirmadas por el autor)

Tomadas dentro del alcance autorizado, sin cambiar reglas ni balance de la web.

- **Multiplicadores del minuto de dificultad.** La web aplica a las pelusas hijas
  del jefe, a la vida del jefe y a las apariciones de depuración los multiplicadores
  del director según el tiempo de partida. U2 porta solo esa parte, con ritmo 1
  (duración de referencia de 10 min). El calendario de oleadas, élites y enjambre
  sigue siendo de U3.
- **Límites del mundo.** El jefe pregunta al mundo si una pelusa cabe
  (`IsInside`, como la web). En la escena U2, el mundo es el patio de U1 (±47,5 m).
- **Constantes exportadas.** `scripts/u2-export-data.mjs` exporta además `Tuning`
  (curvas, jefe, oro, baúles, relleno y espera de 0,4 s), las etiquetas de
  estadística de cada arma y cómo se muestra cada mejora. `CatalogReferenceTests`
  las contrasta con `baseline.catalog`.
- **Pantalla de subida de nivel** construida por código en uGUI, con su propio
  `EventSystem` si la escena no lo tiene. F4 (QA) no se abre durante la subida de
  nivel.
- **Ensayo U2**: combate real con 12 subidas de nivel previas y las de la medida
  resueltas con la primera carta. El jugador es invulnerable (QA) para poder medir
  1000 enemigos.
- **Referencia `u2-combat.json`**: regenerada dos veces antes de su primer uso por
  C# (añadir el jefe enfurecido; quitar listas anidadas que JsonUtility no lee).
  Desde la primera comparación con C#, no se ha tocado.

## U2 aprobado (29/09/2026)

El autor probó U2 y lo aprobó. Confirmó las tres decisiones técnicas que había
presentado: multiplicadores del minuto de dificultad solo para el jefe y las
apariciones QA, con ritmo 1; ensayo con invulnerabilidad QA y resolviendo las
subidas de nivel con la primera carta; y regeneración de `u2-combat.json` antes de su
primer uso por C#. U3 sigue sin autorizar. Cierre y resultados en
[PROGRESO_U2](PROGRESO_U2.md).

## U3 autorizado (29/09/2026)

El autor autoriza U3: mundo y partida completa equivalentes a la web aprobada
`0505b16`. U4 no está autorizado. Alcance, exclusiones y plan en
[PROGRESO_U3](PROGRESO_U3.md). Decisiones del autor:

1. **Referencia complementaria** `scripts/unity-reference-u3.mjs` →
   `unity/Docs/Reference/u3-world.json`, con el mismo modelo que U2: solo desde la
   web aprobada, con guarda contra `src/`; no toca `baseline.json` ni
   `u2-combat.json`; nunca se recalculan esperados con C#. Una vez usada por una
   prueba C#, no se regenera.
2. **Tolerancias:** mundo portado en double; igualdad exacta en lo discreto; 1e-6 m
   en alturas y posiciones; cronología de la partida integrada exacta en los primeros
   60–120 s y después por totales con margen documentado. Port fiel de
   `simplex-noise` alimentado por el RNG propio.
3. **Movimiento:** port de la física del jugador de la web con sus casos de prueba,
   conservando la entrada y la cámara de U1; las pruebas de U1 y U2 siguen pasando.
4. **Pantallas técnicas uGUI** de inicio (ambos personajes, duración 5/10/15 y
   semilla opcional) y de resultados (tiempo, bajas, daño por arma y nivel; sin
   Calderilla).
5. **Teclas:** F3 abre la depuración y 1–8 ejecutan sus acciones, como la web; en
   U3 el ajuste de vértices pasa a F9 (U1 y U2 no cambian); E interactúa.
6. **Catálogo:** todo desbloqueado (6 armas y 12 objetos), como la web sin filtros
   de meta; los filtros se conectarán en U4 con el guardado.

## U3: decisiones técnicas de implementación (03/10/2026, propuestas por Claude Code; pendientes de confirmación del autor)

Tomadas dentro del alcance autorizado, sin cambiar reglas ni balance de la web. Detalle y pruebas en [PROGRESO_U3](PROGRESO_U3.md).

- **Pausa por carta separada de la lógica.** `Run.update` de la web no se detiene con una carta abierta (la pausa la pone `Game`). `CombatRun.HoldWhileChoosing` (activado en el juego) separa esa pausa para poder reproducir el guion de `u3-world.json`, que deja abierta la segunda carta de una subida doble.
- **Margen de los totales tras 120 s** en las partidas integradas: bajas, apariciones y nivel ±3 %; vivos y oro ±5 %; desenlace igual con ±1 s. Hoy la coincidencia es total; el margen solo cubre diferencias de un bit en otro runtime.
- **Mapa inicial fijo** `MAMPORRO` en la escena técnica (la web sortea uno al abrir), para pruebas reproducibles; «Nuevo mapa» y la semilla escrita funcionan como en la web.
- **Pausa sin opciones**: las opciones de la pausa web (idioma, volumen, etc.) dependen del guardado y quedan para U4.
- **Ensayo U3**: partida real con director que avanza sin medir hasta cada punto (minutos 2, 5, 9 y enjambre) y mide allí 10 s + 30 s; invulnerable de ensayo y primera carta, como en U2.
- **Presentación técnica**: telegrafiado, tapas de baúl, anillos de las mesas y brillos con el render instanciado de combate; el efecto de abrir baúl es un anillo (la web usa partículas) y el texto usa la fuente integrada de Unity. Pulido en U5.


## Aprobación funcional de U3 y planificación U4 (04/10/2026)

El autor ha realizado la prueba manual y aprueba U3 como base funcional de la migración. Último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`. Las limitaciones visuales/de diseño son mejoras futuras; los picos aislados documentados siguen sin causa aislada y no bloquean el avance.

U4 queda autorizado **solo para planificación**. No implementar hasta la siguiente respuesta del autor. Plan, revisión del save real, pasos y pruebas en [PROGRESO_U4](PROGRESO_U4.md). U4 conserva IDs y progreso compatible; no guarda partidas activas ni fusiona progresos. Guardado Unity existente: validar, copiar y pedir sustitución explícita; nunca sumar saldos o recompensas. El original exportado no se modifica.

Excepción acotada a `src/`: U4 necesita exportación JSON validada del progreso web. No autoriza modificar reglas/balance. Base funcional `0505b16` y referencias U0/U2/U3 congeladas.

**Las seis propuestas técnicas anteriores no se consideran confirmadas individualmente.** Clasificación recomendada: HoldWhileChoosing, márgenes ya existentes y metodología del ensayo pueden cerrarse como registro técnico; mapa fijo y pausa sin opciones son provisionales sustituidos en U4; presentación técnica se pule en U5. Ninguna necesita una decisión bloqueante para U4. No ampliar tolerancias.

Dirección futura confirmada: después de terminar y aprobar U4–U6, bloques separados para mundo y estructuras considerablemente mayores; terreno rectangular/geométrico, grandes mesetas, cambios de altura, rampas y verticalidad; escalada libre; acabado retro profesional y animaciones de jugador, enemigos, ataques, impactos, muertes, jefe y mundo; después expansión de contenido e iconos/ilustraciones originales. Megabonk es referencia de escala, verticalidad y lectura espacial, sin copiar arte, mapas, assets ni diseño exacto. No ejecutar estas mejoras todavía.


## U4 autorizado para implementación y política estricta de transferencia (04/10/2026)

Decisión del autor: plan U4 aprobado, pasos 1–9 autorizados sin confirmación entre piezas. Recuperación revisable solo cuando es inequívoca; errores críticos ambiguos, JSON ilegible y versiones desconocidas fatales. Candidato normalizado e informe en memoria antes de escribir. Mostrar conservación, exclusiones, defaults y motivos. Sin clamps silenciosos ni progreso inventado.

Validar → vista previa → confirmar → backup del guardado actual → escritura segura → relectura y validación. Sustitución completa, nunca fusión; original exportado intacto; importación repetida sin recompensas. Probar temporal, backup y recuperación; no afirmar atomicidad sin garantía real. Separar tests puros de IO. Las seis propuestas U3 mantienen su clasificación, sin exigir confirmación individual.

Concreción técnica del paso 1 (Codex, dentro del plan autorizado): tres formatos separados; transferencia `mamporro.progress` v1 y persistencia `mamporro.unity-save` v1, DTO explícito y adaptadores web v1/v2/v3. Límites defensivos y reglas reales diferenciados. Detalle y corpus en [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md). No modifica reglas web ni referencias previas.

## U4: decisiones técnicas de implementación de los pasos 7–8 (04/10/2026)

Aceptadas por el autor las del paso 7; las del paso 8 las toma Claude Code dentro de lo que el autor dejó a su criterio. No cambian reglas ni balance.

- **Duración 5/10/15** (paso 7, aceptada): se guarda en `settings.runMinutes` en cuanto se elige, como la web; la partida en curso no cambia.
- **Idioma del primer arranque sin guardado** (paso 7, aceptada): español si Windows está en español; inglés en otro caso (equivalente a `detectLanguage` web).
- **Atajos QA F1/F2/F9** (paso 8): cambian las mismas opciones guardadas (resolución interna, dithering, vértices), sin valores activos distintos de los guardados. F3, F6 y F8 no son opciones y siguen igual. Las pruebas usan carpetas temporales.
- **Fallo al guardar una opción** (paso 8): el cambio se aplica solo en la sesión, se avisa y el último guardado válido no se toca, como la web sin almacenamiento. Las compras siguen sin aplicarse si no se guardan.
- **Efectos pendientes de U5** (paso 8): volúmenes y silencio, «Reducir partículas» y «Sacudidas de cámara» se guardan y se muestran, pero Unity aún no tiene audio, partículas decorativas ni sacudidas que gobernar. Los destellos sí gobiernan los efectos existentes: el rojo del HUD y el blanco de enemigos golpeados.

## Aprobación manual de U4 y mejoras futuras de balance (04/10/2026)

Decisión del autor tras la prueba manual: **U4 aprobada** el 04/10/2026; la build funciona correctamente para su alcance. Último cierre técnico `3754e31`. U5 (audio, pulido y validación de equivalencia) pasa a ser el siguiente bloque propuesto; queda en planificación ([PROGRESO_U5](PROGRESO_U5.md)) y no está autorizado ni implementado.

Dos mejoras de balance pedidas por el autor durante la prueba. **No son fallos de U4 y no se implementan en U5** salvo autorización expresa; se decidirán en el bloque posterior de balance mediante pruebas:

- **Enemigos al principio:** menos enemigos al inicio de la partida, pero **más resistentes individualmente**; después, crecimiento progresivo de la cantidad. Objetivo: menos saturación en los primeros minutos y más espacio para explorar y aprender, sin que los enemigos iniciales sean irrelevantes. Sin cifras: no se fijan número de enemigos, multiplicador de vida, curva ni tiempos.
- **Oro de partida:** los enemigos sueltan demasiado poco oro. Más adelante deben soltar bastante más, con cantidad/frecuencia que pueda crecer con el tiempo y la dificultad, y más oportunidades reales de abrir baúles. Es **oro de la partida, no Calderilla del Caos**. No cambiar todavía probabilidades, cantidades, precios ni curvas.

Dirección futura completa confirmada para después de terminar la migración (U6): mundo y estructuras notablemente mayores; terreno más rectangular/geométrico con grandes mesetas, rampas y más verticalidad; escalada libre; menos enemigos al inicio y más resistentes, con aumento progresivo; más oro de partida; acabado retro bastante más profesional; animaciones; arte e iconos originales; más enemigos, armas, tomos y personajes. Megabonk es referencia de escala, verticalidad, lectura espacial/geometría y agilidad; no copiar arte, mapas, modelos, assets, contenido ni diseño exacto. MAMPORRO mantiene identidad propia.

## U5 autorizado: resolución de las dudas del plan (04/10/2026)

Decisión del autor: plan de U5 aprobado e implementación autorizada (pasos 1–9 seguidos, con checkpoint, pruebas, commit y push por pieza; detenerse solo ante una decisión nueva importante y al terminar U5; no iniciar U6 sin aprobación).

- **Foco (diferencia intencionada con la web):** al perder el foco se pausa, se descartan efectos y se silencia todo; al volver, sigue en pausa sin cola de efectos y con la música al 25 % hasta Continuar.
- **Inicio del audio:** música del menú desde el arranque en Windows.
- **Compresor propio** (aproximación del `DynamicsCompressor` del navegador): umbral −12 dB, relación 8, rodilla 30 dB, ataque 3 ms, liberación 250 ms; sin asignaciones en el hilo de audio ni NaN/Infinity. Si es inestable o caro, detenerse antes de cambiar de arquitectura.
- **Cámara:** equivalencia con la web (brazo, recuperación, pivote, FOV y sacudida); no autoriza cámara nueva contra estructuras.
- **Números de daño y parpadeo del jugador** equivalentes a la web; el parpadeo y los destellos dependen de «Destellos de daño»; los telegrafiados nunca se ocultan. Sacudida con valores web (0,45/0,8/1/0,4/0,7), sin rebalancear.
- **Medidas:** no publicar `ProjectSettings.asset`. GPU «n/d» en el ensayo principal si no hay medida válida; GC, memoria y GPU (si es válida) en una pasada «diagnóstico Development» separada, nunca presentada como rendimiento final.
- **Capturas:** 1280×720 y 1920×1080 en ES/EN (44), mundo sin interfaz una vez, 2560×1440 solo en el ensayo.
- **Nombre:** `Mamporro-U3.exe` y «Mamporro U1» se conservan en U5; nombre definitivo y migración de la carpeta de guardado en U6.
- U5 sigue sin incluir las mejoras de balance pedidas tras U4 ni contenido, mundo, escalada, animaciones finales, overhaul, Steam ni telemetría.
