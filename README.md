# MAMPORRO

Roguelike 3D de supervivencia contra hordas ("bullet heaven") con estética retro
tipo PS1, hecho con Three.js + TypeScript + Vite. Todo el contenido (geometría,
texturas, fuente, textos) se genera por código: no hay archivos externos.

> **Estado: hito 1 de 6.** Por ahora te puedes mover por un mapa procedural con
> colinas, mesetas y acantilados: correr, saltar y deslizarte. El combate llega
> en el hito 2.

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
  core/                 Bucle a paso fijo, entrada (teclado/ratón), RNG con semilla, orquestador (Game)
  data/config.ts        Parámetros ajustables: tamaño del mapa, movimiento, cámara, niebla
  entities/             Física del jugador (lógica pura) y su modelo/animación
  world/                Terreno (heightfield), decoración, colisiones y sus mallas
  render/               Render retro a baja resolución, parche PS1, cámara, cielo, texturas y paleta
  ui/                   Interfaz HTML/CSS, fuente pixelada generada en tiempo de ejecución
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
  y acantilados, y montañas en el borde que marcan el límite. Árboles, pinos,
  rocas y arbustos se colocan desde una semilla, así la misma semilla da siempre
  el mismo mapa. A las rocas pequeñas se puede subir de un salto.
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
- la fuente pixelada (contornos correctos y cobertura de caracteres).

### Modo de pruebas automáticas

Abriendo la página con `?test` (por ejemplo `http://localhost:5173/?test`) el
juego no captura el ratón y expone `window.__MAMPORRO__` para scripts de prueba
en navegador (mover al jugador, pausar, leer su posición...).

## Hoja de ruta

1. ✅ **Base:** render retro, terreno procedural, jugador, cámara, salto y
   deslizamiento.
2. Combate: enemigos, 2 armas automáticas, XP, vida y game over (300 enemigos a
   60 FPS).
3. Progresión en partida: subidas de nivel con rarezas, 6 armas, 8 tomos,
   Reroll/Saltar/Descartar.
4. Mapa vivo: oro, cofres, objetos, santuarios, tótem, portal, jefe,
   temporizador y enjambre final.
5. Meta y UI: menús, personajes, moneda meta, desbloqueos, misiones, guardado
   completo e idiomas.
6. Pulido: audio, partículas, balance, tests y guía para añadir contenido.
