# MAMPORRO

Roguelike 3D de supervivencia contra hordas ("bullet heaven") con estética retro
tipo PS1, hecho con Three.js + TypeScript + Vite. Todo el contenido (geometría,
texturas, fuente, textos) se genera por código: no hay archivos externos.

> **Estado: hito 2 de 6.** Doña Remedios recorre un mapa procedural con colinas,
> acantilados, casas derruidas, templetes en ruinas, granjas y pozos. Se enfrenta
> a hordas de Pelusas Rebeldes y Cucarachas Turbo con dos armas automáticas: la
> Chancla Teledirigida y Eau de Naftalina. Hay gemas de experiencia, niveles,
> vida y game over. Las mejoras al subir de nivel llegan en el hito 3.

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

## Controles

| Acción       | Tecla                                   |
| ------------ | --------------------------------------- |
| Moverse      | W A S D (relativo a la cámara)          |
| Mirar        | Ratón                                   |
| Saltar       | Espacio (mantén para saltar más alto)   |
| Deslizarse   | Shift o C                               |
| Pausa        | Esc                                     |
| Panel debug  | F3                                      |

Las armas disparan solas: tú solo te mueves, saltas y te deslizas. Doña
Remedios aparta a los enemigos al pasar, pero los que empuja de frente la
frenan (como mucho hasta el 60 % de su velocidad); nunca la encierran. Si algo
la tapa (una horda, un muro), se ve su silueta dorada a través.

- Al pulsar **Jugar** el juego captura el ratón. **Esc** lo suelta y pausa; para
  volver, pulsa **Continuar**. El navegador exige un clic para volver a
  capturarlo. Chrome, además, no deja recapturarlo justo después de soltarlo:
  si falla, espera un momento y vuelve a hacer clic.
- **Deslizarse** da un impulso (con un breve tiempo de recarga) y acelera cuesta
  abajo. Si saltas mientras te deslizas conservas la inercia en el aire.
- **Ctrl** también puede servir para deslizarse si lo activas en Opciones.
  Viene desactivado porque en Chrome y Edge **Ctrl+W cierra la pestaña** y
  ninguna web puede impedirlo. Como red de seguridad, durante una partida el
  navegador pide confirmación antes de cerrar la página.
- La semilla del mapa aparece en la pantalla de inicio y en la pausa. Puedes
  escribir una semilla antes de jugar para repetir un mapa.

### Panel de debug (F3)

Muestra FPS, tiempos de lógica y render, llamadas de dibujo, triángulos,
entidades (enemigos, proyectiles, gemas, partículas) y datos de la partida
(entre ellos, cuánto te frena la horda). Con
el panel abierto y jugando, las teclas numéricas lanzan acciones de prueba:

| Tecla | Acción                                        |
| ----- | --------------------------------------------- |
| 1     | Invencible sí/no                              |
| 2     | Subir un nivel                                |
| 3     | Avanzar un minuto (más dificultad)            |
| 4     | Aparecen 100 enemigos                         |
| 5     | Eliminar a todos los enemigos                 |

**Prueba de rendimiento:** con F3 abierto, pulsa 1 (invencible) y 4 tres veces
para tener 300 enemigos. Después mira el FPS y el tiempo de "Lógica".

Las partidas en las que se usan estas acciones quedan marcadas como "con trucos".
En el hito 5 no darán moneda meta ni contarán para las misiones.

### Opciones (en la pausa)

Idioma (español / inglés), sensibilidad del ratón, resolución interna
(240 / 360 / 480 px de alto), temblor de vértices estilo PS1, tramado de color
(dithering), mostrar FPS y usar Ctrl para deslizarse. Se guardan en el
navegador (`localStorage`).

## Estructura del proyecto

```
index.html              Página con el canvas y la capa de interfaz
scripts/check-node.cjs  Aviso si la versión de Node es demasiado antigua
src/
  main.ts               Punto de entrada
  core/                 Bucle a paso fijo, entrada, RNG con semilla, orquestador (Game) y partida (Run)
  data/                 Contenido y ajustes en ficheros tipados:
    config.ts             mapa, construcciones, movimiento, cámara, niebla
    characters.ts         personajes (arma inicial, vida...)
    enemies.ts            enemigos (vida, velocidad, daño, experiencia...)
    weapons.ts            armas (comportamiento y estadísticas)
    waves.ts              aparición de enemigos y escalado con el tiempo
  systems/              Lógica del combate: enemigos (IA), aparición, proyectiles, gemas,
                        rejilla espacial, daño y críticos, experiencia, dificultad
  weapons/              Comportamientos de las armas (teledirigida, aura...)
  entities/             Física del jugador y modelos de jugador y enemigos
  world/                Terreno, construcciones, vegetación, fauna, colisiones y sus mallas
  render/               Render retro, cámara, cielo, texturas, paleta y efectos de combate
  ui/                   Interfaz HTML/CSS (HUD, pantallas), fuente pixelada
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
    roca no es un refugio; lo alto de un muro, sí (hasta que lleguen los
    enemigos a distancia).
  - Al atravesarlos, cada enemigo que Doña Remedios empuja de frente la frena
    según su masa (`CROWD_CONFIG` en `src/data/config.ts`).
- **Rendimiento medido:**
  - En Node, la lógica completa (IA, armas, proyectiles, gemas) cuesta unos
    0,5 ms por tick con 500 enemigos.
  - En Chromium, 0,3 ms con 300 enemigos y 0,5 ms con unos 480.
  - Unas 30 llamadas de dibujo.
  - Los FPS reales dependen de la GPU de tu equipo.
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

La guía completa llegará en el hito 6. Mientras tanto:

- **Un enemigo nuevo:**
  1. Añade su entrada en `src/data/enemies.ts` (y en `ENEMY_LIST`).
  2. Crea su modelo en `src/entities/enemyModels.ts`.
  3. Escribe su nombre en `src/i18n/es.ts` y `src/i18n/en.ts`.
  4. Dale un peso y un minuto de aparición en `src/data/waves.ts`.
- **Un arma nueva:**
  1. Añade su entrada en `src/data/weapons.ts`.
  2. Si su comportamiento no existe, añade su nombre a `WeaponBehaviorId` (en
     `src/data/weapons.ts`), escríbelo en `src/weapons/` y regístralo en
     `src/weapons/index.ts`.
  3. Escribe su nombre y descripción en los dos idiomas.

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
- el guardado (valores corruptos, versiones futuras, migraciones);
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
  - una partida simulada completa y una prueba de rendimiento con 500 enemigos;
- la silueta: una por pieza del modelo, con la prueba de profundidad invertida
  y el orden de dibujo correcto.

### Modo de pruebas automáticas

Abriendo la página con `?test` (por ejemplo `http://localhost:5173/?test`) el
juego no captura el ratón y expone `window.__MAMPORRO__` para scripts de prueba
en navegador: mover al jugador, pausar, leer su posición y la partida, lanzar
acciones de debug o medir los tiempos del bucle.

## Hoja de ruta

1. ✅ **Base:** render retro, terreno procedural, jugador, cámara, salto y
   deslizamiento.
2. ✅ **Combate:** enemigos, 2 armas automáticas, XP, vida y game over
   (objetivo: 300 enemigos a 60 FPS). Además, mundo con ruinas, casas, granjas y
   fauna.
3. Progresión en partida: subidas de nivel con rarezas, 6 armas, 8 tomos,
   Reroll/Saltar/Descartar.
4. Mapa vivo: oro, cofres, objetos, santuarios, tótem, portal, jefe,
   temporizador, enjambre final y más tipos de enemigos.
5. Meta y UI: menús, personajes, moneda meta, desbloqueos, misiones, guardado
   completo e idiomas.
6. Pulido: audio, partículas, balance, tests y guía para añadir contenido.
