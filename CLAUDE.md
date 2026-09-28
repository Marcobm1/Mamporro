# MAMPORRO: guía para trabajar en el proyecto

Roguelike 3D de supervivencia contra hordas con estética PS1, hecho con
Three.js + TypeScript estricto + Vite. Todo el contenido es original y se
genera por código (no hay assets externos).

## Documentos que hay que leer antes de tocar nada

- [`docs/ESPECIFICACION.md`](docs/ESPECIFICACION.md): la especificación original
  del autor (requisitos, contenido mínimo, hitos).
- [`docs/DECISIONES.md`](docs/DECISIONES.md): todo lo acordado después. Manda
  sobre la especificación cuando la concreta o la cambia.
- [`README.md`](README.md): cómo ejecutarlo, controles, contenido del juego,
  estructura del código, detalles técnicos y cómo añadir contenido.

## Estado

- **Hechos:** hitos 1–5: base, combate, progresión en partida, mapa vivo y meta/UI.
  El historial está en `git log`. El autor confirmó el hito 4.
- **Siguiente:** esperar la prueba del hito 5 por el autor; después, hito 6 (pulido).
- El autor prueba cada hito antes de seguir. Si no ha dicho nada del último
  hito entregado, pregúntale primero qué tal y si hay que ajustar algo.

## Proceso de cada hito

1. Lee los tres documentos de arriba y el código afectado.
2. **Antes de programar**, propón un plan breve del hito. Si hay decisiones de
   diseño importantes que no cubren la especificación ni `DECISIONES.md`,
   pregúntalas todas juntas en una sola tanda y espera respuesta. Lo menor,
   decídelo tú y apúntalo.
3. Implementa respetando las convenciones de abajo.
4. **Verifica:**
   - `npm run typecheck`, `npm test` y `npm run build` sin errores.
   - Tests nuevos para la lógica nueva (junto al código, `*.test.ts`).
   - Prueba en navegador (ver "Verificación en navegador"): sin errores de
     consola, capturas en español e inglés, a 1280×720 y otra resolución.
   - Si hay algo que afecte al rendimiento, mídelo: el objetivo es 60 FPS con
     300+ enemigos, así que la lógica debe quedarse en pocos ms por tick (hay
     benchmarks en `src/systems/combat.test.ts`).
5. Actualiza el `README.md` y apunta en `docs/DECISIONES.md` lo decidido.
6. **Commits** pequeños y descriptivos, en español. Cada commit debe compilar y
   pasar los tests por sí solo (se puede comprobar el árbol preparado con
   `git write-tree` + `git archive` en una carpeta aparte). No incluir nombres
   de modelos de IA en commits ni en el código.
7. Push a la rama de trabajo. No abrir Pull Request salvo que el autor lo pida.
8. **Resumen final para el autor:** qué has hecho, cómo probarlo, qué has
   decidido por tu cuenta (para que lo confirme), qué no has podido comprobar y
   qué queda. Después **párate y espera** a que lo pruebe.

## Preferencias del autor

- Si tienes dudas, pregunta antes de hacer.
- Comprueba los hechos en fuentes fiables y no supongas; si algo no se puede
  comprobar, dilo.
- Escríbele en español, claro y sin jerga innecesaria. Usa Windows: los
  comandos que le des, que funcionen en CMD.

## Convenciones del código

- TypeScript estricto, sin `any`. El código se escribe en inglés; comentarios,
  README y commits, en español.
- **Data-driven:** armas, tomos, objetos, enemigos, personajes, oleadas y
  ajustes en `src/data/*.ts`. Añadir contenido debe ser añadir una entrada de
  datos y, si hace falta, un comportamiento.
- **Textos:** ningún texto visible en el código. Van en `src/i18n/es.ts` (el de
  referencia) y `src/i18n/en.ts` (mismas claves: lo comprueban el compilador y un
  test). La fuente pixelada (`src/ui/font/glyphs.ts`) debe tener todos los
  caracteres usados (hay un test que lo comprueba).
- **Sin assets externos:** geometría, texturas, fuente y sonido, por código.
- **Capas:** la lógica de la partida es pura, sin Three.js (`src/core/Run.ts`,
  `src/systems/`), y se prueba con Vitest. El dibujo va en `src/render/` y en las
  mallas de `src/world/`; la interfaz HTML/CSS, en `src/ui/`. `src/core/Game.ts`
  lo une todo.
- **Rendimiento:** entidades en arrays planos (sin crear objetos por tick en
  los bucles calientes), `InstancedMesh`, rejilla espacial.
- **Determinismo:** todo lo aleatorio de la partida sale del RNG con semilla
  (`src/core/rng.ts`), con un generador derivado por subsistema
  (`rng.derive('etiqueta')`).
- **Guardado:** `src/save/schema.ts`, con `SAVE_VERSION` y migraciones; los datos
  leídos siempre se validan. Al cambiar el esquema, sube la versión y añade la
  migración con su test.
- **Debug (F3):** acciones con las teclas numéricas; las partidas con trucos
  quedan marcadas (`run.cheated`) y no deben dar moneda meta ni contar para
  misiones.

## Comandos

```bash
npm install         # dependencias (Node 22.12 o superior; ver .nvmrc)
npm run dev         # servidor de desarrollo en http://localhost:5173
npm test            # tests (Vitest)
npm run typecheck   # tipos
npm run build       # tipos + build en dist/
npm run preview     # sirve dist/ en http://localhost:4173
```

## Verificación en navegador

- Con `?test` en la URL (`http://localhost:5173/?test`, o la de `preview`) el
  juego no captura el ratón y expone `window.__MAMPORRO__`: empezar partida,
  acciones de debug, teletransportar, cámara, hacer aparecer enemigos, apagar
  armas, dar objetos, usar interactuables, manejar la subida de nivel, leer el
  estado... La lista está en `TestHooks`, en `src/core/Game.ts`.
- En el entorno en la nube hay Playwright global y Chromium preinstalado. Para
  WebGL sin GPU, lanza Chromium con
  `--use-angle=swiftshader --enable-unsafe-swiftshader --ignore-gpu-blocklist`.
  Va lento (unos pocos FPS): úsalo para comprobar que todo se ve y funciona, no
  para medir FPS.
- No se puede probar automáticamente la captura real del ratón (Pointer Lock)
  ni los FPS reales en el equipo del autor: díselo para que lo compruebe él.

## Hito 5: meta y UI (implementado; pendiente de prueba del autor)

- Menú principal, preparación de partida, personajes, tienda, misiones y opciones ES/EN.
- Doña Remedios: Chancla y ralentización del 20 % a 3 m. Sir Baguette: Barra,
  modelo propio y escudo que se carga tras 8 s sin daño; bloquea un golpe.
- Calderilla del Caos: bajas, supervivencia, victoria y ocho misiones. Tienda de
  desbloqueos y ampliaciones permanentes de Reroll/Saltar/Descartar (2 a 5 usos).
- Catálogo inicial: 1 personaje, 4 armas y 8 objetos. Los filtros afectan a cartas,
  rerolls, descartes, baúles y tótems. Tomos disponibles desde el inicio.
- Guardado v2: migra opciones v1, guarda progreso, compras y selección. No guarda
  partidas en curso. Aviso si no se puede persistir. Recompensas una sola vez.
- Abandonos y partidas con trucos no conceden moneda ni progreso de misiones.
  También marcan trucos los ganchos de pruebas que dañan, teletransportan o
  desactivan las armas. Cambiar cámara o consultar estado no marca trucos.
- Volúmenes de música/efectos y silencio guardados; audio pendiente del hito 6.
- Datos: `src/data/meta.ts`; lógica pura: `src/systems/meta.ts`; pantallas:
  `src/ui/MetaScreens.ts`. Números, reparto y verificación en `docs/DECISIONES.md`.

## Hito 6: pulido

- Efectos de sonido con WebAudio, con un límite para que cientos de golpes no
  saturen; música chiptune por código con volumen separado y silencio.
- Partículas y feedback, balance básico y tests completos.
- Guía en el README para añadir un arma, un tomo, un objeto o un enemigo.
