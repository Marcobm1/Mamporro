# MAMPORRO

> Estado vigente: U1–U3 aprobados. U3 recibió aprobación manual el 04/10/2026. [Cierre U3](docs/PROGRESO_U3.md#checkpoint-al-terminar-u3). [U4 implementado y verificado el 04/10/2026, pendiente de la prueba manual del autor](docs/PROGRESO_U4.md#checkpoint-al-terminar-u4). El contenido siguiente describe la web aprobada; [guía Unity](unity/README.md).

Roguelike 3D de supervivencia contra hordas ("bullet heaven") con estética retro
tipo PS1, hecho con Three.js + TypeScript + Vite. Todo el contenido (geometría,
texturas, fuente, textos y audio) se genera por código: no hay archivos externos.

> **Estado: los 6 hitos web aprobados; U1 y U2 Unity aprobados.** Doña Remedios y Sir Baguette recorren un mapa procedural con colinas,
> acantilados, casas derruidas, templetes en ruinas, granjas y pozos, y se
> enfrentan a hordas de 4 tipos de enemigos, élites y un jefe. Al subir de nivel
> eliges entre cartas con rareza (6 armas, 8 tomos, Reroll, Saltar y
> Descartar). Por el mapa hay oro, baúles con 12 objetos, mesas camilla que dan
> bendiciones, tótems de desafío y un armario escondido que invoca al jefe.
> Hay menú, personajes con pasivas, tienda, ocho misiones y progreso guardado.
> Tienes 5, 10 o 15 minutos: si se acaba el tiempo sin vencer al jefe, llega el
> enjambre final.

## Requisitos

- **Node.js 22.12 o superior** (Vite 8 y Vitest 5 lo exigen). Recomendado: la
  versión LTS actual (Node 24), que es la que indica el archivo `.nvmrc`.
- Un navegador de escritorio reciente con WebGL2: Chrome, Edge o Firefox.
- Teclado y ratón.

### Comprobar tu versión de Node

```bash
node -v
```

Si ves `v22.12.0` o superior (por ejemplo `v24.x.x`), ya está. Si ves una
versión anterior o el comando no existe, instala o actualiza Node con una de
estas opciones:

**Opción A: nvm (macOS / Linux)**, recomendado si trabajas con varios proyectos:

```bash
# Instalar nvm: https://github.com/nvm-sh/nvm#installing-and-updating
nvm install   # instala la versión indicada en .nvmrc (Node 24)
nvm use       # la activa en esta terminal
```

**Opción B: nvm-windows (Windows)**, <https://github.com/coreybutler/nvm-windows/releases>.
Abre la terminal como administrador (nvm-windows lo necesita para crear enlaces):

```powershell
nvm install lts
nvm use lts
```

nvm-windows no lee el archivo `.nvmrc`, por eso se usa `lts`.

**Opción C: instalador oficial.** Descarga la versión **LTS** desde
<https://nodejs.org> e instálala. Después cierra y vuelve a abrir la terminal.

El proyecto comprueba la versión automáticamente antes de `npm install`,
`npm run dev`, `build`, `preview` y `test`. Si es demasiado antigua, verás un
aviso con estos mismos pasos.

## Cómo ejecutarlo

```bash
npm install        # instala las dependencias (solo la primera vez)
npm run dev        # servidor de desarrollo → http://localhost:5173
```

Versión optimizada (como quedaría publicada):

```bash
npm run build      # comprueba tipos y genera dist/
npm run preview    # sirve dist/ → http://localhost:4173
```

Otras órdenes:

```bash
npm test           # tests unitarios (Vitest)
npm run typecheck  # solo la comprobación de tipos de TypeScript
```

## Exportar progreso para Unity

En el menú principal web, abre **Opciones**, baja hasta **Exportar progreso para Unity**, revisa la vista previa y pulsa **Descargar progreso**. En inglés: **Options → Export progress for Unity → Download progress**. El navegador descarga `mamporro-progreso.json` (`mamporro.progress`, versión 1).

Incluye Calderilla, desbloqueos/compras, misiones, extras, selección, idioma y opciones compatibles. No guarda una partida activa ni modifica el progreso web. Si el almacenamiento está bloqueado, avisa y permite exportar el progreso cargado en memoria. Las recuperaciones secundarias se muestran antes de descargar; los errores críticos bloquean la exportación sin resetear datos.

En Unity se importa desde Opciones → «Importar progreso web» (revisión previa, sustitución con copia de seguridad, nunca suma). Guía técnica y comprobaciones reproducibles: [EXPORTACION_U4](docs/EXPORTACION_U4.md).

## Controles

| Acción       | Tecla                                   |
| ------------ | --------------------------------------- |
| Moverse      | W A S D (relativo a la cámara)          |
| Mirar        | Ratón                                   |
| Saltar       | Espacio (mantén para saltar más alto)   |
| Deslizarse   | Shift o C                               |
| Usar         | E (abrir baúles, tótem, portal)         |
| Pausa        | Esc                                     |
| Panel debug  | F3                                      |

Las armas disparan solas: tú solo te mueves, saltas y te deslizas. Doña
Remedios aparta a los enemigos al pasar, pero los que empuja de frente la
frenan (como mucho hasta el 60 % de su velocidad); nunca la encierran. Si algo
la tapa (una horda, un muro), se ve su silueta dorada a través.

- En el menú, **Jugar** abre la preparación (semilla y duración). Al pulsar
  **Jugar** en esa pantalla, el juego captura el ratón. **Esc** lo suelta y pausa; para
  volver, pulsa **Continuar**. El navegador exige un clic para volver a
  capturarlo. Chrome, además, no deja recapturarlo justo después de soltarlo:
  si falla, espera un momento y vuelve a hacer clic.
- **Deslizarse** da un impulso (con un breve tiempo de recarga) y acelera cuesta
  abajo. Si saltas mientras te deslizas conservas la inercia en el aire.
- **Ctrl** también puede servir para deslizarse si lo activas en Opciones.
  Viene desactivado porque en Chrome y Edge **Ctrl+W cierra la pestaña** y
  ninguna web puede impedirlo. Como red de seguridad, durante una partida el
  navegador pide confirmación antes de cerrar la página.
- La semilla del mapa aparece en la preparación de partida y en la pausa. Puedes
  escribir una semilla antes de jugar para repetir un mapa.
- En la preparación eliges la **duración de la partida**: 5, 10 (la
  normal) o 15 minutos. Se guarda para la próxima vez.

### Menú, personajes y progreso entre partidas

El menú permite jugar, seleccionar personaje, comprar desbloqueos, consultar
las ocho misiones y cambiar opciones. Español e inglés se pueden cambiar
antes de jugar; se recuerda la selección.

| Personaje | Arma inicial | Pasiva | Disponibilidad |
| --- | --- | --- | --- |
| Doña Remedios | Chancla Teledirigida | Ralentiza un 20 % a enemigos a 3 m; prevalece el efecto más fuerte | Desde el inicio |
| Sir Baguette, Paladín del Pan Duro | Barra de Pan Duro | Tras 8 s sin daño, bloquea un golpe y vuelve a cargar | Tienda: 220 |

La **Calderilla del Caos** es distinta del oro que gastas en los baúles. Al
morir o ganar recibes una moneda por cada 20 bajas, una por cada 15 segundos
sobrevividos (máximo 60 por tiempo) y 60 por victoria, más las misiones nuevas.
Las fracciones se redondean hacia abajo; los resultados muestran el desglose.

- Disponibles inicialmente: Chancla, Naftalina, Barra y Dentaduras; Gafas,
  Zapatillas, Termo, Cojín, Lupa, Décimo, Rulos y Monedero; los ocho tomos.
- La tienda vende a Sir Baguette (220), Jersey Estático (140), Baraja del Tute
  (160) y Olla Exprés (180). Una compra los desbloquea para futuras partidas.
- También vende tres ampliaciones de cada acción: Reroll, Saltar y Descartar.
  Cuestan 80/140/220 y elevan los usos por partida de 2 a 3/4/5 permanentemente.
- Misiones siempre activas: terminar una partida, acumular 1000 bajas, abrir
  10 baúles, completar 3 mesas camilla, superar un tótem, llegar al nivel 20,
  vencer al jefe y ganar sin adquirir el tomo de vida. Recompensas únicas de
  30/60/40/40/50/50/80/100 monedas respectivamente. Las de 1000 bajas, tótem y
  victoria sin vida desbloquean Suelo Recién Fregado, Collar y Bata.
- El contenido bloqueado no sale en cartas (incluidos rerolls y descartes),
  baúles ni recompensas de tótems. La tienda muestra cómo obtenerlo.
- **Abandonar o cerrar no concede moneda ni avance de misiones.** Las partidas
  con trucos tampoco cuentan. Abrir F3 solo para consultar métricas sí permite
  progresar, siempre que no ejecutes acciones de debug.

El guardado v3 usa `localStorage`, migra tus opciones anteriores y conserva
progreso, compras, personaje elegido e idioma. **No guarda partidas a medias**.
Si el navegador bloquea el guardado se muestra un aviso y el progreso dura
solo esa sesión. Borrar los datos del navegador borra también el progreso.

### Audio y efectos (hito 6)

- Música chiptune original sintetizada, con variante intensa para jefe/enjambre.
- Sonidos propios para las seis armas, impactos, críticos, bajas, daño, recogidas,
  niveles, compras, escudo, explosiones y resultados. Máximo 16 efectos y una
  pista musical; cuatro voces reservadas para avisos, con límites de repetición.
- El audio se activa al hacer clic o pulsar una tecla. La música baja durante
  pausa/elecciones; al ocultar la pestaña se suspende y se descartan efectos pendientes.
- Volúmenes separados de música/efectos y silencio. Sin descargas de sonido.
- Opciones de partículas reducidas, sacudidas y destellos de daño. El modo reducido
  limita partículas decorativas a 400 (64 nuevas por frame), frente a 1500 (256).
  Los avisos de embestidas y del jefe permanecen visibles.
- Críticos, muertes de élites/jefe y escudo tienen feedback adicional moderado.
  Desactivar destellos evita el parpadeo de jugador/enemigos y el flash de daño del HUD;
  el temblor de vértices PS1 tiene su propio ajuste independiente.

El guardado v3 migra v1/v2 y conserva opciones, monedas, compras y misiones.

### La partida

- **Temporizador:** arriba, en el centro, cuenta hacia atrás. La dificultad
  sube con el tiempo: más enemigos, con más vida, y tipos nuevos. En una
  partida de 5 minutos todo va el doble de rápido (y los enemigos dan el doble
  de experiencia y oro); en una de 15, más despacio.
- **Oleadas especiales:** de vez en cuando entra una estampida de cucarachas en
  fila, un anillo de tápers, un arco de palomas o una tormenta de pelusas.
- **Élites:** cada 2 minutos (de dificultad) aparece una Rata de Gimnasio.
- **Jefe:** la Pelusa Madre sale del **armario misterioso**, que está escondido
  lejos del inicio. Búscalo y ábrelo con E cuando quieras: cuanto más tarde, más
  vida tiene. Vencerla gana la partida.
- **Enjambre final:** si el tiempo llega a 0 sin haberla vencido, el ritmo de
  aparición se duplica cada 20 segundos hasta que caes o vences al jefe. Para
  que haya salida, el armario aparece en el minimapa si aún no lo habías visto.
- Al terminar (victoria o derrota) sale la pantalla de resultados: tiempo,
  bajas, nivel, oro conseguido, baúles abiertos, objetos y daño por arma.

### Enemigos

| Enemigo            | Qué hace                                                        |
| ------------------ | --------------------------------------------------------------- |
| Pelusa Rebelde     | Básico: va a por ti a saltitos                                  |
| Cucaracha Turbo    | Rápida y frágil                                                 |
| Táper Caducado     | Tanque lento: mucha vida, pega fuerte y suelta más oro          |
| Paloma Okupa       | Se queda a distancia, se hincha y te escupe pipas               |
| Rata de Gimnasio   | Élite: se agacha, marca una franja en el suelo y embiste        |
| La Pelusa Madre    | Jefe: rodillo, culetazo y estornudo, siempre avisados           |

Los ataques de la rata y del jefe se avisan antes: una **franja** naranja en el
suelo para las embestidas y el rodillo, y un **círculo** que se va llenando
para el culetazo. El estornudo se nota porque se hincha: después suelta bolas
de polvo en todas direcciones y pelusas hijas. Con media vida se enfada y
ataca más a menudo. Contra ella no se pasa: te aparta.

### Mapa vivo

| Elemento              | Cómo se usa                                                          |
| --------------------- | -------------------------------------------------------------------- |
| Baúl de la abuela     | E para abrirlo por oro: da un objeto. Cada uno cuesta más (15, 30, 49, 74...) |
| Mesa camilla          | Quédate en su círculo 9 s (vienen más enemigos): eliges 1 de 3 bendiciones |
| Tótem de cacerolas    | E: 45 s de desafío (más enemigos y con más vida, pero doble de oro y +50 de suerte) y un objeto al superarlo |
| Armario misterioso    | E: invoca al jefe                                                    |

- Hay 14 baúles, 3 mesas camilla, 2 tótems y 1 armario, colocados según la
  semilla. Al acercarte a unos 30 m aparecen en el **minimapa** (arriba a la
  derecha); el armario hay que verlo más de cerca.
- Si te sales del círculo de la mesa camilla, se descarga despacio.
- Las **bendiciones** suben una estadística para el resto de la partida y
  tienen rareza, como las cartas.
- El **oro** lo sueltan a veces los enemigos (siempre los élites) y se recoge
  como las gemas.

### Objetos

Salen de los baúles y de los tótems. No ocupan hueco: se acumulan y cada copia
vuelve a sumar su efecto.

| Objeto                    | Rareza      | Efecto                                                        |
| ------------------------- | ----------- | ------------------------------------------------------------- |
| Gafas de Culo de Vaso     | Común       | +7 % de probabilidad de crítico                               |
| Zapatillas de Velcro      | Común       | +8 % de velocidad                                             |
| Termo de Café de Puchero  | Común       | +8 % de velocidad de ataque                                   |
| Cojín de Ganchillo        | Común       | +4 de armadura                                                |
| Lupa de Leer Prospectos   | Poco común  | +25 % de daño crítico                                         |
| Décimo de Lotería         | Poco común  | +40 % de oro                                                  |
| Rulos de la Suerte        | Poco común  | +15 de suerte                                                 |
| Collar de Perlas          | Rara        | En cada crítico, 30 % de que salte una perla a otro enemigo   |
| Monedero Bien Lleno       | Rara        | +4 % de daño por cada 100 de oro que lleves (hasta +80 %)     |
| Baraja del Tute           | Rara        | Una 4.ª carta al subir de nivel (solo una vez)                |
| Olla Exprés               | Épica       | Los enemigos pueden explotar al morir (8 % por copia)         |
| Bata de Guatiné           | Legendaria  | Si caes, te levantas con media vida y apartas a todos; se gasta |

Sinergias: las gafas y la lupa con el collar de perlas (más críticos, más
perlas); la lotería con el monedero (más oro, más daño, aunque gastarlo en
baúles lo baja); la olla con el área y el daño (sus explosiones crecen con tu
área y tu daño, y pueden encadenarse).

### Subir de nivel

Al subir de nivel el juego se pausa, suelta el ratón y ofrece 3 cartas. Al
elegir, vuelve a capturarlo solo. Si subes varios niveles de golpe, las
subidas salen una detrás de otra.

| Acción                              | Tecla               |
| ----------------------------------- | ------------------- |
| Elegir carta                        | 1–4 o clic          |
| Reroll (cambiar todas las cartas)   | R                   |
| Saltar (pasar sin elegir)           | X                   |
| Descartar (y luego elegir la carta) | B, luego 1–4 o clic |
| Cancelar el descarte                | Esc o B             |

- **Cartas:** un arma nueva, la mejora de un arma que ya tienes o un tomo.
  Como mucho 4 armas y 4 tomos. No hay nivel máximo.
- **Rareza:** Común (gris), Poco común (verde), Rara (azul), Épica (morada) o
  Legendaria (dorada). La Suerte hace más probables las altas.
- **Mejoras de arma:** suben 1 o 2 estadísticas al azar de la lista del arma.
  Desde Rara, siempre 2, y cuanto más rara, más suben. La carta dice exactamente
  qué sube y cuánto.
- **Tomos:** suman a una estadística global, de forma aditiva nivel a nivel.
- **Reroll, Saltar y Descartar:** 2 usos de cada uno por partida, ampliables permanentemente
  hasta 5 en la tienda. Descartar quita esa arma o ese tomo del sorteo para el
  resto de la partida y pone otra carta en su hueco.
- Con la **Baraja del Tute** salen 4 cartas en vez de 3.
- Si ya no queda nada que ofrecer, salen cartas de relleno: una que cura y otra
  que da oro.

### Armas

| Arma                   | Qué hace                                                   |
| ---------------------- | ---------------------------------------------------------- |
| Chancla Teledirigida   | Proyectil que busca al enemigo más cercano y lo atraviesa  |
| Eau de Naftalina       | Aura que daña a todo lo que se acerca                      |
| Barra de Pan Duro      | Golpe en arco delante de ti (con más cantidad, alrededor)  |
| Dentaduras Orbitales   | Dientes postizos que giran a tu alrededor mordiendo        |
| Jersey Estático        | Calambre que salta de enemigo en enemigo                   |
| Suelo Recién Fregado   | Charcos al andar: dañan y hacen resbalar a quien los pisa  |

### Tomos

| Tomo                           | Mejora                                |
| ------------------------------ | ------------------------------------- |
| Manual del Mamporro            | Daño                                  |
| Tomo del Café Cargado          | Velocidad de ataque                   |
| Tablas de Multiplicar          | Cantidad (proyectiles, dentaduras...) |
| Catálogo de Tallas Grandes     | Área                                  |
| Manual de Marcha Nórdica       | Velocidad de movimiento               |
| Recetario del Caldito          | Vida máxima y regeneración            |
| Horóscopo del Periódico        | Suerte                                |
| Colección de Imanes de Nevera  | Radio de recogida y experiencia       |

### Panel de debug (F3)

Muestra FPS, tiempos de lógica y render, llamadas de dibujo, triángulos,
entidades (enemigos, proyectiles, gemas, partículas) y datos de la partida
(entre ellos, cuánto te frena la horda, el minuto de dificultad y el ritmo de
aparición). Con
el panel abierto y jugando, las teclas numéricas lanzan acciones de prueba:

| Tecla | Acción                                        |
| ----- | --------------------------------------------- |
| 1     | Invencible sí/no                              |
| 2     | Subir un nivel                                |
| 3     | Avanzar un minuto (más dificultad)            |
| 4     | Aparecen 100 enemigos                         |
| 5     | Eliminar a todos los enemigos (jefe incluido) |
| 6     | Invocar al jefe delante                       |
| 7     | +100 de oro                                   |
| 8     | Revelar todo el mapa en el minimapa           |

**Prueba de rendimiento:** con F3 abierto, pulsa 1 (invencible) y 4 tres veces
para tener 300 enemigos. Después mira el FPS y el tiempo de "Lógica".

Las partidas en las que se usan estas acciones quedan marcadas como "con trucos".
No dan moneda meta ni cuentan para las misiones, aunque se desactiven después.

### Pausa

Muestra las estadísticas del personaje (vida, regeneración, armadura, daño,
velocidad de ataque, cantidad, área, crítico y daño crítico, velocidad, suerte,
radio de recogida, experiencia y oro), los objetos que llevas (pasa el ratón
por encima para ver qué hacen) y las opciones: idioma (español / inglés),
sensibilidad del ratón, resolución interna
(240 / 360 / 480 px de alto), temblor de vértices estilo PS1, tramado de color
(dithering), mostrar FPS y usar Ctrl para deslizarse. Se guardan en el
navegador (`localStorage`).

## Estructura del proyecto

```
index.html              Página con el canvas y la capa de interfaz
CLAUDE.md               Proceso y normas para seguir desarrollándolo con Claude Code
docs/
  ESPECIFICACION.md     Especificación original del proyecto
  DECISIONES.md         Decisiones acordadas en cada hito
scripts/check-node.cjs  Aviso si la versión de Node es demasiado antigua
src/
  main.ts               Punto de entrada
  core/                 Bucle a paso fijo, entrada, RNG con semilla, orquestador (Game) y partida (Run)
  data/                 Contenido y ajustes en ficheros tipados:
    config.ts             mapa, construcciones, movimiento, cámara, niebla
    characters.ts         personajes (arma inicial, atributos y pasivas)
    meta.ts               catálogo inicial, misiones, precios y economía meta
    enemies.ts            enemigos (vida, velocidad, daño, oro, comportamiento) y
                          los ataques del jefe
    weapons.ts            armas (comportamiento, estadísticas y cuáles pueden mejorar)
    bonuses.ts            estadísticas del jugador mejorables y sus topes
    tomes.ts              tomos (qué estadística suben y cuánto)
    items.ts              objetos (rareza, efectos) y parámetros de los especiales
    rarities.ts           rarezas (peso, efecto de la Suerte y potencia)
    upgrades.ts           cuánto sube cada estadística de arma, topes y usos de
                          Reroll/Saltar/Descartar
    waves.ts              aparición de enemigos, escalado, duraciones, oleadas
                          especiales, élites y enjambre final
    run.ts                oro, baúles, mesas camilla (y bendiciones), tótems,
                          portal y colocación de los interactuables
  systems/              Lógica pura: enemigos (IA y estados), aparición, director
                        (tiempo, oleadas, élites, enjambre), jefe, proyectiles
                        propios y enemigos, gemas y monedas, rejilla espacial, daño
                        y críticos, experiencia, dificultad, estadísticas, subida de
                        nivel (cartas), objetos, interactuables, progreso meta y frenado en la horda
  weapons/              Comportamientos de las armas: teledirigida, aura, arco,
                        órbita, cadena y rastro
  entities/             Física del jugador y modelos de jugador y enemigos
  world/                Terreno, construcciones, vegetación, fauna, interactuables,
                        colisiones y sus mallas
  render/               Render retro, cámara, cielo, texturas, paleta, efectos de
                        combate y avisos de ataque en el suelo
  ui/                   Interfaz HTML/CSS (HUD, minimapa, pantallas, subida de nivel,
                        resultados, personajes, tienda y misiones), fuente pixelada
  i18n/                 Textos en español (es.ts) e inglés (en.ts)
  save/                 Guardado versionado en localStorage con migraciones
  styles/               CSS de la interfaz
```

Los tests (`*.test.ts`) están junto al código que prueban.

## Detalles técnicos

- **Bucle de juego:** la lógica avanza a 60 pasos fijos por segundo. El render
  va a la tasa de refresco de la pantalla e interpola entre pasos, así se ve
  fluido también en monitores de 120/144 Hz.
- **Render retro:** la escena se dibuja en un render target de baja resolución.
  Después se escala a pantalla con un factor entero y filtrado Nearest, así
  todos los píxeles miden lo mismo. Opcionalmente reduce el color a 15 bits con
  tramado Bayer 4×4 y ajusta los vértices a la rejilla de píxeles (temblor PS1).
- **Mapa procedural determinista:** el terreno sale de ruido simplex con
  distorsión de dominio. Tiene mesetas en terrazas cuyos bordes alternan rampas
  y acantilados, y montañas en el borde que marcan el límite. Las construcciones
  eligen zonas llanas y aplanan el terreno debajo:
  - casas derruidas: muros rotos con ventanas, chimenea, tejado hundido, hiedra;
  - templetes: columnas, dinteles y una estatua rota;
  - granjas: valla, almiar, carro y espantapájaros;
  - pozos.

  Todo sale de la semilla, así la misma semilla da siempre el mismo mapa.
  También se reparten árboles, rocas, troncos, setas, carteles, miles de matas
  de hierba y flores, pájaros y mariposas. La vegetación se mece con el viento.
  Se puede subir a rocas, muros bajos, cajas y vallas.
- **Combate con cientos de enemigos:**
  - Los enemigos viven en arrays planos (sin crear objetos por frame).
  - Una rejilla espacial se reconstruye cada tick con una ordenación por conteo.
    La usan la separación entre enemigos, las colisiones y la búsqueda del más
    cercano.
  - Se dibujan con `InstancedMesh`: una llamada de dibujo por tipo de enemigo,
    más una para todas sus sombras.
  - Persiguen al jugador, se separan y rodean los obstáculos cuando se atascan.
  - Suben a superficies bajas y alcanzan al jugador hasta 1,6 m por encima. Una
    roca no es un refugio; lo alto de un muro, sí, salvo de las pipas de las
    palomas.
  - Cada enemigo tiene un estado (moverse, avisar, embestir, recuperarse). Las
    palomas se mueven de lado a su distancia preferida; la rata fija la
    dirección al empezar a avisar, así que apartarse la esquiva. El jefe lo
    dirige `BossController`, que usa esos mismos estados.
  - Los enemigos enormes (el jefe) no caben en la búsqueda normal de la
    rejilla: `EnemySystem.queryRadius` los añade aparte, para que todas las
    armas los golpeen por su borde. La separación entre enemigos tiene en cuenta
    la masa: una pelusa se aparta del jefe, no al revés.
  - Al atravesarlos, cada enemigo que Doña Remedios empuja de frente la frena
    según su masa (`CROWD_CONFIG` en `src/data/config.ts`).
- **Rendimiento medido:**
  - En Node, la lógica completa (IA, armas, proyectiles, gemas) cuesta unos
    0,5–0,6 ms por tick con 500 enemigos y las armas del hito 2.
  - Con las 4 armas nuevas mejoradas con tomos y 500 enemigos alrededor, unos
    0,9 ms por tick (el presupuesto es de 16,7 ms).
  - En el enjambre final, con 750 enemigos de todos los tipos, palomas
    disparando, el jefe, la olla y las perlas: unos 1,3 ms por tick.
  - En Chromium, 0,3 ms con 300 enemigos y 0,5 ms con unos 480.
  - Unas 35 llamadas de dibujo.
  - Los FPS reales dependen de la GPU de tu equipo.
- **Subida de nivel:**
  - Las cartas salen de un sorteo con pesos entre lo que se puede ofrecer:
    mejoras de lo que tienes, y armas o tomos nuevos si queda hueco. Nunca
    sale dos veces lo mismo en una tirada, ni lo descartado.
  - La rareza pesa `peso × (1 + Suerte/100 × bonus)`, con más bonus cuanto más
    rara (`src/data/rarities.ts`).
  - Lo que sube cada estadística y sus topes están en `src/data/upgrades.ts` y
    `src/data/tomes.ts`. Una estadística al tope deja de salir en las cartas.
  - El sorteo usa su propio RNG derivado de la semilla: con la misma semilla
    y las mismas elecciones salen las mismas cartas (y los mismos objetos en los
    baúles, con otro RNG propio).
- **Estadísticas del jugador:** tomos, objetos, bendiciones y efectos del
  momento (el monedero, la suerte del tótem) se suman como bonificaciones del
  mismo tipo (`src/data/bonuses.ts`) y comparten los mismos topes.
- **Equilibrio:** se ha ajustado simulando partidas completas sin gráficos (un
  jugador que elige cartas con criterio, recoge lo que cae y abre baúles con el
  oro que tiene). En 10 minutos se abren unos 6–8 baúles. La vida del jefe
  crece con el cuadrado del minuto (`BOSS_CONFIG`), porque el daño del jugador
  también se dispara: pegado a él y sin recibir daño, las builds simuladas lo
  vencen en 15–85 s al final del temporizador.
- **Física del jugador:** es lógica pura, sin Three.js, y está cubierta por
  tests. Incluye:
  - aceleración rápida y control en el aire;
  - salto de altura variable, con margen tras salir de un borde (coyote time) y
    buffer de salto;
  - deslizamiento con impulso;
  - límite de pendiente: los acantilados no se pueden trepar y se resbala por
    ellos.
- **Fuente pixelada:** los glifos se dibujan como mapas de bits en
  `src/ui/font/glyphs.ts`. Al arrancar se convierten en una fuente OpenType real
  con `opentype.js` y se registran con la API `FontFace`. Incluyen á, é, í, ó,
  ú, ü, ñ, ¿ y ¡.
- **i18n:** ningún texto visible está escrito en el código. `en.ts` debe tener
  exactamente las claves de `es.ts`: lo comprueba el compilador y un test, que
  además verifica que la fuente tiene todos los caracteres usados.
- **Números de daño:** se dibujan con los glifos de la propia fuente pixelada
  (con contorno) en quads instanciados que miran a la cámara. Los críticos salen
  en amarillo con "!" y los supercríticos en naranja con "!!".
- **Silueta a través de obstáculos:** cada pieza de Doña Remedios lleva una
  copia con un material de profundidad invertida (`GreaterDepth`) que solo se
  dibuja donde algo ya dibujado la tapa. Lleva un tramado de tablero y un
  pequeño margen a lo largo del rayo de vista para que el suelo bajo los pies
  no la haga asomar. El orden de dibujo está en `src/render/renderOrder.ts`:
  mundo y enemigos, silueta, Doña Remedios y, al final, lo que no debe
  revelarla (hierba, fauna, partículas, gemas y proyectiles).

## Cómo añadir contenido

Los datos viven en `src/data/`; la lógica no debe depender de Three.js ni de la UI.
La lista siguiente sirve para futuras ampliaciones, **después de validar el pulido
y decidir la migración a Unity**. Mantén IDs estables para no romper guardados.

- **Un enemigo nuevo:**
  1. Añade su entrada en `src/data/enemies.ts` (y en `ENEMY_LIST`), con su
     comportamiento: `chase` (persigue), `ranged` (con `ranged`: distancia,
     recarga, aviso y proyectil) o `charger` (con `charge`: aviso y embestida).
  2. Crea su modelo en `src/entities/enemyModels.ts` y, si quieres, su
     animación en `EnemyRenderer.animate`.
  3. Escribe su nombre en `src/i18n/es.ts` y `src/i18n/en.ts`.
  4. Dale un peso y un minuto de aparición en `src/data/waves.ts` (o úsalo en
     una oleada especial o como élite).
- **Un objeto nuevo:**
  1. Añade su entrada en `src/data/items.ts` con su rareza y sus efectos (qué
     estadística sube cada copia). Si su efecto es especial, pon sus números en
     `ITEM_EFFECTS` y aplícalo en `src/core/Run.ts`.
  2. Escribe su nombre y descripción en los dos idiomas.
  3. Añádelo al catálogo inicial o a una recompensa en `src/data/meta.ts`;
     una vez desbloqueado podrá salir en baúles y tótems según su rareza.
- **Un arma nueva:**
  1. Añade su entrada en `src/data/weapons.ts`: estadísticas base, lista
     `upgradable` (lo que pueden subir sus cartas) y, si quieres, nombres
     propios para algunas estadísticas (`statLabels`).
  2. Si su comportamiento no existe, añade su nombre a `WeaponBehaviorId` (en
     `src/data/weapons.ts`), escríbelo en `src/weapons/` (con `update` y
     `createState`) y regístralo en `src/weapons/index.ts`.
  3. Escribe su nombre y descripción en los dos idiomas.
  4. Añádela al catálogo inicial o a un desbloqueo en `src/data/meta.ts`;
     aparecerá en las cartas cuando esté desbloqueada.
  5. Añade el timbre con el mismo ID en `src/data/audio.ts`. Al disparar, el
     comportamiento debe poner `sincePulse = 0`; `Run` emite `weaponFired`.
     Si necesita efectos nuevos, emite un evento de `RunEffects`, con su noop
     en `NO_EFFECTS`, y resuélvelo en `RunView`. No crees WebAudio dentro del arma.
- **Un tomo nuevo:**
  1. Añade su entrada en `src/data/tomes.ts`: qué estadística sube y cuánto por
     nivel Común (las rarezas lo multiplican).
  2. Escribe su nombre, nombre corto y descripción en los dos idiomas.

Para cualquiera de los cuatro tipos:

1. Usa estadísticas y topes del sistema existente; si introduces una estadística,
   completa su cálculo, cartas, descripción, icono/textos y casos límite.
2. Registra los IDs en las listas y tipos correspondientes. Decide disponibilidad
   inicial, compra o misión. Los guardados antiguos deben seguir siendo válidos;
   si cambia su esquema, sube la versión y añade migración y test.
3. Añade pruebas del comportamiento: daño/alcance/recarga y casos límite para
   armas/enemigos; suma, topes y sinergias para tomos/objetos. Comprueba también
   que el contenido bloqueado no aparece antes de desbloquearlo.
4. Verifica textos ES/EN, fuente, typecheck, tests y build. En `?test`, usa F3 y los
   ganchos para ejercer el caso; recuerda que las acciones de trucos excluyen meta.
5. Si hay muchas entidades o efectos, repite los benchmarks y comprueba presupuestos.
   Actualiza catálogo, README y `docs/DECISIONES.md`; commit pequeño en español.

Ejemplos de referencia: `weapons/arc.ts`, `data/tomes.ts`, efectos especiales en
`core/Run.ts` y comportamientos de enemigos en `systems/EnemySystem.ts`.

## Pruebas

`npm test` cubre la lógica pura:

- el RNG con semilla (determinismo, rangos, distribución);
- el terreno (triángulos coherentes con el mesh, normales, zona de inicio llana,
  bordes altos);
- la decoración y la rejilla de colisiones;
- la física del jugador:
  - aceleración y salto, con coyote time y buffer;
  - deslizamiento, pendientes y escalones;
  - que los acantilados no se puedan trepar;
- el guardado (valores corruptos, versiones futuras, migraciones v1/v2 a v3);
- la meta: compras, topes, recompensas únicas, misiones acumuladas, persistencia,
  bloqueo de contenido y exclusión de trucos; las pasivas de ambos personajes;
- audio PCM determinista, límites de voces, presupuestos de partículas y matriz
  inicial de ambos personajes en 5/10/15 minutos;
- la traducción;
- la fuente pixelada (contornos correctos y cobertura de caracteres);
- los colisionadores de caja y círculo;
- las construcciones: sitios separados y lejos del inicio, terreno aplanado,
  muros escalables y determinismo;
- el combate:
  - daño, críticos y supercríticos, y armadura;
  - curva de experiencia y escalado de la dificultad;
  - rejilla espacial comparada con fuerza bruta;
  - IA de enemigos: persecución, separación y contacto, y que subirse a una roca
    no proteja;
  - el frenado al atravesar una horda: solo frenan los de delante, nunca más
    del tope, y se sale incluso rodeada;
  - perforación de la chancla, radio del aura y gemas (recogida y fusión);
  - una partida simulada completa, eligiendo cartas;
  - pruebas de rendimiento con 500 enemigos (armas del hito 2 y las 4 nuevas);
- la subida de nivel:
  - rarezas: pesos, efecto de la Suerte y distribución del sorteo;
  - mejoras de arma: 1–2 estadísticas distintas (2 desde Rara), cantidades por
    rareza, enteros y topes;
  - tomos: suma aditiva, topes y tomos que dejan de salir;
  - cartas: sin repetidas, deterministas, sin armas ni tomos nuevos sin hueco,
    sin descartados y con relleno si no queda nada;
  - en la partida: cola de subidas, Reroll/Saltar/Descartar con sus usos y lo
    que hace cada tipo de carta;
- las armas nuevas: el arco solo golpea delante (o alrededor, con más
  cantidad), las dentaduras respetan el tiempo entre mordiscos y la recarga, la
  cadena no repite ni salta fuera de alcance, y el rastro solo friega andando,
  daña, hace resbalar y se seca;
- los textos de las cartas y de la pausa en español e inglés;
- el director: la dificultad según la duración elegida, el ritmo de la partida,
  las oleadas especiales (una vez cada una y en orden), los élites periódicos y
  el enjambre final (ritmo que se duplica, con tope);
- los objetos: 12 con todas las rarezas, suma por copias, topes, la cuarta
  carta de la baraja, el sorteo de los baúles con la Suerte y los números del
  collar, la olla y el monedero;
- los interactuables: colocación determinista, lejos de obstáculos y de la
  vegetación, portal lejos del inicio y con colisión; descubrirlos, cargar y
  descargar la mesa camilla, el aviso de lo que se puede usar y el desafío;
- los enemigos nuevos: la paloma se queda a su distancia y solo dispara tras
  avisar; la rata avisa quieta, embiste y se queda vendida; el jefe alterna sus
  tres ataques, se enfada a media vida y no se deja empujar; los proyectiles
  enemigos dañan y caducan;
- la partida: baúles (precio, objeto, encarecer), oro con la lotería, el
  monedero, la mesa camilla y sus bendiciones, el tótem, el portal y la vida del
  jefe, la victoria, el enjambre que revela el portal, la bata, la olla, las
  perlas y el ritmo de las partidas cortas;
- rendimiento del enjambre final con 750 enemigos;
- la silueta: una por pieza del modelo, con la prueba de profundidad invertida
  y el orden de dibujo correcto.

### Modo de pruebas automáticas

Abriendo la página con `?test` (por ejemplo `http://localhost:5173/?test`) el
juego no captura el ratón y expone `window.__MAMPORRO__` para scripts de prueba
en navegador: mover al jugador, pausar, leer su posición y la partida, lanzar
acciones de debug, medir los tiempos del bucle, manejar la subida de nivel
(elegir, Reroll, Saltar, Descartar), dar armas, tomos u objetos, usar
interactuables, ver su estado, hacer aparecer enemigos concretos, apagar las
armas y congelar los efectos visuales para fotografiarlos.

## Hoja de ruta

1. ✅ **Base:** render retro, terreno procedural, jugador, cámara, salto y
   deslizamiento.
2. ✅ **Combate:** enemigos, 2 armas automáticas, XP, vida y game over
   (objetivo: 300 enemigos a 60 FPS). Además, mundo con ruinas, casas, granjas y
   fauna.
3. ✅ **Progresión en partida:** subidas de nivel con rarezas, 6 armas, 8 tomos,
   Reroll/Saltar/Descartar. Además, estadísticas en la pausa.
4. ✅ **Mapa vivo:** oro, baúles, 12 objetos, mesas camilla, tótems, portal,
   jefe, temporizador (5/10/15 min), oleadas especiales, élites, enjambre final,
   minimapa, pantalla de resultados y 4 enemigos nuevos.
5. ✅ **Meta y UI:** menús, dos personajes con pasivas, Calderilla del Caos,
   tienda, ocho misiones, guardado v2, opciones e idiomas. Aprobado por el autor.
6. ✅ **Pulido:** audio/música procedural, partículas y opciones de efectos,
   guardado v3, revisión de balance, tests y guía de contenido. Aprobado por el
   autor. Unity dispone de build U3; véase su checkpoint para el estado del port.

### Probar el hito 6 (CMD en Windows)

Desde la carpeta del repositorio:

```cmd
git switch claude/zen-pasteur-674ik0
git pull --ff-only
npm ci
npm run dev
```

Abre `http://localhost:5173`. Haz clic para activar el audio y prueba los dos
volúmenes, silencio y las tres opciones de efectos. Recarga: deben conservarse
junto con tu progreso anterior. Juega con ambos personajes, escucha armas,
recogidas, escudo y resultados; entra en pausa y cambia de pestaña. La variante
intensa se activa con el jefe o al entrar en enjambre.

Para comprobar casos concretos abre `http://localhost:5173/?test`, activa F3 y
usa sus acciones para invocar hordas/jefe. Las partidas con trucos no conceden
moneda ni misiones. Revisa sensación del combate, mezcla de audio, captura del
ratón y FPS reales en tu equipo: estas pruebas requieren tu valoración.

Verificación local:

```cmd
npm run typecheck
npm test
npm run build
npm run preview
```

## Migración a Unity y futuro del juego

**Estado vigente (04/10/2026): U1–U3 aprobados; U4 implementado y verificado, pendiente de la prueba manual del autor.** Proyecto en
`unity/`, Editor 6000.6.3f1, Windows x64 Mono, URP 17.6.0 e Input System 1.20.0.
La licencia, importación, compilación, escena en Play Mode y generación de build
ya se han probado. [Instrucciones Unity](unity/README.md) y
[plan y checkpoint actual](docs/PROGRESO_U4.md). El historial de U0 se conserva en
los documentos de decisiones y referencia.

Los seis hitos web están aprobados. Destino inicial Unity: **Windows de
escritorio**, con futura publicación en Steam y plataformas similares. El
progreso compatible se conservará mediante el importador de un bloque posterior.

- [Plan U0–U6](docs/MIGRACION_UNITY.md).
- [Estado de la preparación y referencia](unity/Docs/U0_REFERENCIA.md).
- [Mejoras solicitadas para Unity](unity/Docs/HOJA_DE_RUTA.md): mundo/estructuras
  mayores, terreno más marcado y rampas, escalada, hordas progresivas, más monedas,
  arte e iconos propios y más contenido.

`unity/` contiene el proyecto abrible en Hub y conserva documentación y referencias.
La base web se conserva en el commit `0505b16`. Para comprobar
catálogo, RNG, fórmulas, guardados y medios de referencia desde CMD:

```cmd
node scripts\unity-reference.mjs
```

### Automatización de navegador del hito 6

`scripts/verify-browser.cjs` prueba contra Vite en desarrollo con `?test`: ES/EN,
1280×720 y 1600×900, opciones/persistencia, grafo WebAudio, presupuesto de sonidos,
pausa, combate y jefe. Requiere Playwright en el entorno de QA. Si no lo tienes:

```cmd
npm install --no-save --package-lock=false playwright
npx playwright install chromium
npm run dev
```

En otra ventana CMD, desde el repositorio:

```cmd
node scripts\verify-browser.cjs
```

Genera `qa-results/` (ignorado por git). Puedes configurar `MAMPORRO_BROWSER` con
la ruta a Chromium y `MAMPORRO_URL` con el servidor. La instrumentación temporal
solo se añade a la respuesta servida durante el test; no modifica el juego ni
el build. Las capturas detienen el render continuo para no saturar SwiftShader;
esto no mide FPS. El sonido se valida mediante PCM y WebAudio, no con una escucha
humana de altavoces. Resultados y límites: `docs/PROGRESO_HITO_6.md`.
