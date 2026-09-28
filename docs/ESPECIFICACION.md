# Especificación original de MAMPORRO

> Documento de partida que dio el autor del proyecto al empezar, copiado tal
> cual. Lo que se ha ido acordando después (respuestas a dudas, ajustes y
> decisiones de cada hito) está en [DECISIONES.md](DECISIONES.md) y manda sobre
> este texto cuando lo concreta o lo cambia. El estado actual y el proceso de
> trabajo están en [../CLAUDE.md](../CLAUDE.md).

---

Prompt para Claude Code — Roguelike 3D de hordas (inspirado en Megabonk)
Copia todo lo que hay debajo de la línea y pégalo en Claude Code dentro de una carpeta vacía.
Rol y objetivo
Eres un desarrollador senior de videojuegos web. Vas a construir desde cero un MVP jugable de un roguelike 3D de supervivencia contra hordas ("bullet heaven" / Vampire Survivors-like en 3D), inspirado en el juego Megabonk (Vedinad, 2025). Todo el contenido (nombres, personajes, armas, arte, textos, humor) debe ser original: no copies nombres, assets ni textos de Megabonk ni de ningún otro juego o franquicia.
Antes de escribir código:

1. Lee todo este documento.
2. Si algo es ambiguo o contradictorio, pregúntame antes de empezar (agrupa las dudas en una sola tanda).
3. Propón un plan por fases (ver "Hitos") y espera mi confirmación.

Stack y restricciones técnicas

* Three.js + TypeScript (strict) + Vite. Sin motores adicionales. Puedes añadir dependencias pequeñas solo si las justificas (p. ej. `simplex-noise` para el terreno).
* Ejecución solo en local (`npm run dev` / `npm run build` + `npm run preview`). Sin backend.
* Plataforma: PC, teclado + ratón. Nada de soporte móvil ni mando por ahora.
* Persistencia con `localStorage` (con versión de esquema y migración/reset si cambia).
* Sin assets externos: toda la geometría, texturas, UI y sonido se generan por código (primitivas, texturas procedurales en canvas, WebAudio). Así el proyecto es autocontenido.
* Rendimiento objetivo: 60 FPS con 300+ enemigos en pantalla en un portátil medio.
* Diseño data-driven: armas, tomos, objetos, enemigos, personajes y oleadas se definen en ficheros de datos tipados (`src/data/*.ts`), no hardcodeados en la lógica. Añadir un arma nueva debe ser añadir una entrada de datos + (si hace falta) un comportamiento.

Estilo visual y sonoro

* Estética retro tipo PS1 / low-poly pixelado:
   * Renderizar a baja resolución (p. ej. 320–480 px de alto) en un render target y escalar a pantalla con filtrado `NearestFilter`.
   * Texturas pequeñas procedurales con `NearestFilter`, paleta de colores limitada, niebla de distancia (que además oculta el pop-in).
   * Opcional (configurable): vertex snapping / jitter estilo PS1 y dithering.
* UI con fuente pixelada (generada o de sistema monospace con escalado entero), números de daño flotantes, feedback claro (flash al recibir golpe, screen shake suave, partículas al morir enemigos).
* Sonido: efectos generados con WebAudio (disparos, golpes, subir de nivel, recoger XP, jefe). Música opcional muy simple y enérgica, con control de volumen y mute.
* Tono: humor absurdo / de memes, pero propio: nombres de personajes, armas, objetos y descripciones con humor original. Nada de marcas, personas reales ni referencias con copyright.

Internacionalización

* Sistema i18n sencillo con español e inglés (`src/i18n/es.ts`, `src/i18n/en.ts`), selector en el menú de opciones, idioma guardado en `localStorage`. Ningún texto visible hardcodeado fuera de estos ficheros.

Bucle de juego (la partida)
Controles y movimiento

* Cámara en tercera persona detrás del jugador, controlada con el ratón (Pointer Lock), con colisión básica contra el terreno para no atravesarlo.
* WASD para moverse relativo a la cámara, Espacio para saltar, Shift/Ctrl para deslizarse (slide con impulso, especialmente cuesta abajo), Esc para pausa.
* El movimiento debe sentirse ágil: aceleración rápida, control en el aire, velocidad como estadística mejorable.
* Ataque automático: el jugador nunca pulsa para atacar; las armas disparan solas según su patrón.

Mapa

* Un mapa procedural por partida (semilla visible en la pausa para reproducir partidas): terreno con verticalidad (colinas, rampas, mesetas) generado con ruido, límites del mapa claros, decoración (árboles/rocas low-poly) con colisión simple.
* Elementos interactuables repartidos por el mapa:
   * Cofres: cuestan oro (el coste sube con cada cofre abierto) y dan un objeto.
   * Santuarios de carga: quedarse dentro de su zona X segundos mientras te atacan → recompensa (mejora de stat a elegir).
   * Tótem de desafío: al activarlo aumenta la dificultad temporalmente a cambio de mejor botín.
   * Portal del jefe: escondido en el mapa; hay que encontrarlo e interactuar para invocar al jefe.
* Minimapa o indicadores simples en pantalla para interactuables ya descubiertos.

Tiempo, oleadas y dificultad

* Temporizador de 10 minutos (configurable). La dificultad escala con el tiempo: más enemigos, más vida, tipos nuevos, oleadas especiales y mini-jefes (élites) periódicos.
* Si el temporizador llega a 0 sin haber derrotado al jefe, empieza el "enjambre final": spawn de enemigos que escala de forma exponencial hasta que el jugador muere o vence al jefe.
* Derrotar al jefe = victoria de la partida → pantalla de resultados.
* Enemigos (mínimo 4 tipos + 1 élite + 1 jefe), por ejemplo: básico que persigue, rápido y frágil, tanque lento, a distancia que dispara proyectiles, y el jefe con 2–3 patrones de ataque telegrafiados.
* Los enemigos deben poder subir/bajar pendientes y rodear obstáculos de forma simple (steering + separación, sin pathfinding costoso).

Progresión dentro de la partida

* Los enemigos sueltan gemas de XP y a veces oro. Radio de recogida mejorable.
* Al subir de nivel se pausa el juego y se ofrecen 3 opciones aleatorias (4 con cierta mejora) con rareza (Común, Poco común, Rara, Épica, Legendaria) influida por la Suerte:
   * Armas: nueva arma o mejora de una existente. Máximo 4 armas a la vez. Cada mejora de arma sube 1–2 de sus stats elegidos al azar de su lista (daño, cantidad de proyectiles, tamaño, velocidad de ataque, duración, penetración, crítico...). Las rarezas altas mejoran más y siempre 2 stats.
   * Tomos: cada tomo mejora una sola estadística global del personaje (afecta a todas las armas). Máximo 4 tomos. Mejoras aditivas.
* Acciones sobre las ofertas con usos limitados por partida: Reroll (nuevas opciones), Saltar y Descartar (eliminar esa opción del pool para el resto de la partida).
* Objetos (de cofres y recompensas): efectos pasivos que se acumulan sin límite de huecos, con rarezas. Deben existir sinergias (p. ej. crítico con objetos que potencian críticos).

Contenido mínimo del MVP (todo con nombres y humor originales)

* 2 personajes jugables, cada uno con arma inicial y una pasiva única (uno desbloqueado al inicio, el otro desbloqueable).
* 6 armas con patrones distintos, por ejemplo:
   1. Cuerpo a cuerpo en arco delante del jugador.
   2. Proyectil que apunta al enemigo más cercano.
   3. Aura de daño continuo alrededor del jugador.
   4. Orbes que orbitan al jugador.
   5. Rayo/cadena que salta entre enemigos.
   6. Rastro de daño que deja el jugador al moverse (premia el movimiento).
* 8 tomos: Daño, Velocidad de ataque, Cantidad de proyectiles, Tamaño/área, Velocidad de movimiento, Vida máxima (+regeneración), Suerte, Radio de recogida/XP.
* 12 objetos repartidos por rarezas.
* Estadísticas del personaje visibles en el menú de pausa (vida, armadura, daño, crítico, velocidad, suerte, etc.).

Meta-progresión (entre partidas)

* Moneda meta (nombre original) que se gana al terminar cada partida según rendimiento.
* Se gasta en: desbloquear armas, objetos y el segundo personaje, y en ampliar usos de Reroll/Saltar/Descartar. Sin grandes mejoras permanentes de estadísticas (la gracia está en la build de cada partida).
* Misiones/logros sencillos (p. ej. "mata a 1000 enemigos", "gana sin tomo de vida") que desbloquean contenido. Mínimo 8.
* Guardado en `localStorage`.

Pantallas / UI

* Menú principal (Jugar, Personajes, Tienda/Desbloqueos, Misiones, Opciones, idioma).
* Selección de personaje.
* HUD: vida, barra de XP y nivel, temporizador, oro, armas y tomos con su nivel, avisos (élite, enjambre final, portal encontrado).
* Pantalla de subida de nivel con cartas de rareza por color.
* Pausa con stats, semilla y opciones.
* Pantalla de resultados: tiempo, kills, daño por arma, nivel, moneda meta ganada.
* Opciones: volumen, sensibilidad del ratón, idioma, efectos retro on/off, mostrar FPS.

Arquitectura esperada

* Separar claramente: `core/` (bucle de juego con paso fijo para la lógica y render interpolado), `systems/` (spawn, combate, colisiones, XP, level-up, IA de enemigos), `entities/`, `weapons/` (comportamientos), `data/`, `ui/` (HTML/CSS superpuesto al canvas), `audio/`, `i18n/`, `save/`.
* Rendimiento: `InstancedMesh` para enemigos, proyectiles y gemas; object pooling; rejilla espacial (spatial hash) para colisiones y búsqueda del enemigo más cercano; límite de entidades con fusión de gemas de XP cuando haya demasiadas.
* RNG con semilla (determinista) para mapa y ofertas.
* Modo debug activable (tecla F3): FPS, nº de entidades, invencibilidad, dar XP, saltar tiempo, invocar jefe.

Calidad

* TypeScript estricto, sin `any` salvo justificado.
* Tests con Vitest para la lógica pura: cálculo de daño y críticos, generación de ofertas y rarezas, aplicación de mejoras, escalado de dificultad, guardado/migración.
* `README.md` con cómo ejecutar, controles, estructura del proyecto y cómo añadir un arma/tomo/objeto/enemigo nuevo.
* Commits pequeños y descriptivos por hito.

Hitos (trabaja por fases y verifica cada una antes de seguir)

1. Base: proyecto Vite+TS+Three, render retro a baja resolución, terreno procedural, jugador con cámara en 3ª persona, movimiento, salto y slide. Criterio: me muevo con fluidez por un mapa con colinas.
2. Combate: enemigos básicos con spawn escalado, 2 armas automáticas, daño, muerte, gemas de XP, vida del jugador y game over. Criterio: 300 enemigos a 60 FPS.
3. Progresión en partida: level-up con ofertas y rarezas, las 6 armas, los 8 tomos, reroll/saltar/descartar.
4. Mapa vivo: oro, cofres, objetos, santuarios, tótem, portal, jefe, temporizador y enjambre final, pantalla de resultados.
5. Meta y UI: menús, personajes, moneda meta, desbloqueos, misiones, guardado, i18n ES/EN, opciones.
6. Pulido: audio, partículas, feedback, balance básico, tests, README.

Al terminar cada hito, resume qué has hecho, qué queda y cualquier decisión que hayas tomado por tu cuenta. Si una decisión de diseño importante no está cubierta aquí, pregúntame antes en lugar de asumir.
