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

Pendientes de que el autor las pruebe y las confirme:

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

## Limitaciones conocidas

- La cámara solo choca con el terreno.
- El daño por contacto de los enemigos no crece con el tiempo (solo su vida).
- La captura real del ratón (Pointer Lock) y los FPS reales solo los puede
  comprobar el autor: las pruebas automáticas usan `?test` y una GPU por
  software.
