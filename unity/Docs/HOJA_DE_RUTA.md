# Mejoras de MAMPORRO en Unity

> Estado vigente en [ESTADO_ACTUAL](../../docs/ESTADO_ACTUAL.md): U1 aceptado; U2 implementado y pendiente de la prueba manual del autor ([PROGRESO_U2](../../docs/PROGRESO_U2.md)). No comenzar U3.

**Historial — U1 autorizado el 29/09/2026 y posteriormente aceptado:** prototipo técnico de movimiento, imagen y horda;
seguimiento en [PROGRESO_U1](../../docs/PROGRESO_U1.md). La escalada libre, mundo
ampliado, economía y contenido de esta hoja permanecen para bloques posteriores.
300/500/750/1000 son cargas de ensayo, no límites del juego ni balance aprobado.

Registradas el 28/09/2026 a petición del autor. **Son objetivos del proyecto, no
funciones ya implementadas.** Este documento acompañará al proyecto Unity y debe
actualizarse al aprobar cada bloque. Las propuestas del asistente se distinguen
de las peticiones del autor.

## Dirección del juego acordada

Juego de escritorio para una futura publicación en Steam/plataformas similares.
Conservar humor e identidad propios, supervivencia contra hordas, progresión y
estética retro. Megabonk sirve de referencia expresada por el autor para escala,
verticalidad y agilidad; no se han analizado sus sistemas en esta fase ni se
presupone reproducirlos. MAMPORRO mantiene nombres, arte y diseño propios.

## Peticiones del autor y orden propuesto

| Prioridad | Petición | Trabajo propuesto | Criterio para darlo por bueno |
| --- | --- | --- | --- |
| P0 | Movimiento más ágil; subir, bajar y escalar paredes | Escalada libre confirmada por el autor (no limitarla a una trepada breve), junto con rampas, salto, deslizamiento y transición a bordes. Concretar controles y superficies. | Recorrido completo sin atascos, cámara legible y control al aterrizar; no quedar a salvo indefinidamente de la horda. |
| P0 | Mundo y estructuras más grandes | Medir recorridos y densidad de lugares interesantes; ampliar mapa y edificios con rutas por suelo, tejados y desniveles. | Explorar ofrece decisiones y recompensas; el tamaño no añade largos trayectos vacíos. |
| P0 | Colinas más cuadradas y rampas | Terreno low-poly con mesetas y cambios de nivel más marcados, rampas conectadas y bajadas claras. No asumir que debe ser un mundo de vóxeles/cubos. | Rutas de subida/bajada identificables; jugador y enemigos navegan desniveles sin atravesar geometría. |
| P0 | Menos enemigos al principio; aumento progresivo | Separar introducción, crecimiento, presión alta y enjambre. Ajustar aparición, variedad y vida por separado, con límites de densidad. | Primeros minutos dejan aprender y explorar; aumenta la presión de forma perceptible sin picos involuntarios. Probar 5/10/15 minutos. |
| P1 | Más monedas | Más oro de los enemigos durante la partida, confirmado por el autor; aumentar la recompensa progresivamente con tiempo y dificultad. Revisar frecuencia y cantidad frente al precio de baúles. | Se abren más oportunidades de compra sin que todas sean automáticas; medir tiempo al primer baúl y baúles por partida. |
| P1 | Visual más profesional | Guía de arte: paleta, siluetas, iluminación, materiales, animaciones, composición y jerarquía del HUD. | Personaje, enemigos, proyectiles y recompensas se distinguen en movimiento y con horda; coherencia ES/EN y entre resoluciones. |
| P1 | Imágenes para tomos, armas, etc. | Familia original de iconos para armas, tomos, objetos, personajes y recompensas. El autor autoriza ilustraciones e iconos originales en archivos; se amplía para Unity la restricción de todo por código. | Se reconoce cada elemento en cartas y HUD; rareza no depende solo del color; estilo coherente y legibilidad a tamaño pequeño. |
| P2 | Más enemigos | Añadir roles distintos: enemigos que cierren rutas, obliguen a desplazarse o amenacen alturas, con avisos y debilidades. | Cada tipo cambia decisiones y tiene respuesta clara; no se limita a cambiar vida o color. |
| P2 | Más armas y tomos | Definir huecos entre patrones y sinergias; introducir un lote pequeño y comprobar combinaciones y desbloqueos. | Nuevas builds viables sin anular el catálogo existente ni saturar el render. |
| P2 | Más personajes | Arma inicial, pasiva, silueta y modo de jugar diferenciados; condiciones de desbloqueo claras. | Se sienten distintos en movimiento y combate; ninguno necesita compras permanentes obligatorias para funcionar. |

Están confirmados escalada libre, oro de partida progresivo e imágenes originales
en archivos. No hay cantidades de contenido, tamaño del mapa ni porcentajes
de oro fijados aún. Se decidirán a partir del prototipo y las pruebas; no se
convierten ejemplos del plan en compromisos de lanzamiento.

## Recomendaciones adicionales (pendientes de aprobación)

1. **Cámara que resuelva obstáculos y combates verticales.** Antes de agrandar
   edificios, evitar que paredes/techos oculten al jugador o que la cámara entre
   en ellos. Probar espacios estrechos, tejados y cambios de altura.
2. **Mando y reasignación de controles**, incluida navegación completa por menús,
   sensibilidad, inversión de cámara y señales según dispositivo. Tiene sentido
   para el destino de escritorio; Steam Deck será una prueba específica posterior,
   no una compatibilidad que se dé por supuesta.
3. **Primer minuto claro.** Enseñar movimiento y objetivo mediante acciones y
   avisos breves; un primer baúl alcanzable ayuda a explicar la economía sin
   cargar al jugador con un tutorial largo.
4. **Medidas locales para balance.** Registrar en pruebas tiempo al primer nivel/
   baúl, daño por arma, causa de muerte y minutos sin recoger recompensas. Sin
   enviar datos de jugadores a servicios externos en esta fase.
5. **Ajustes de legibilidad y comodidad.** Tamaño de UI, intensidad de efectos,
   distancia/FOV de cámara y límites de sacudidas; avisos de ataques por forma y
   movimiento, además de color. Mantener las opciones del hito 6.
6. **Preparación de publicación.** Build autónoma y guardados robustos antes de
   integrar servicios de tienda. Después valorar logros, Steam Cloud, demo,
   página de tienda y materiales promocionales. Mantener el juego utilizable
   sin depender de la conexión a una plataforma para una partida local.

Steam Input y Steam Cloud tienen documentación propia: no aparecen simplemente
por compilar con Unity. Su integración se planificará cuando exista una build
estable y el autor disponga de la configuración necesaria de Steamworks.
Fuentes oficiales consultadas el 28/09/2026:
[Steam Input](https://partner.steamgames.com/doc/features/steam_controller) y
[Steam Cloud](https://partner.steamgames.com/doc/features/cloud).

## Relación con la migración

- **U0:** conservar referencia, decisiones y este listado. Ninguna mejora altera
  el juego web aprobado.
- **U1:** prototipo técnico de movimiento, render y 300+ enemigos. Diseñarlo para
  poder evaluar después las rampas/escalada; cualquier variante de movimiento
  requiere su plan concreto y aceptación, conservando una escena de referencia.
- **U2 implementado, pendiente de aprobación del autor:** núcleo y combate equivalentes en escenario controlado.
- **U3–U6, todavía no autorizados:** trasladar y validar los sistemas restantes. Las diferencias
  intencionadas se documentarán en vez de hacer pasar un cambio de diseño por
  un fallo del port. No ampliar el catálogo durante la migración.
- **Después de la base Unity aprobada:** bloques P0, luego P1 y lotes pequeños P2.
  Se podrá adelantar un prototipo aislado si el autor lo aprueba; no reconstruir
  un mundo grande antes de validar cómo se mueve el jugador por él.

## Decisiones abiertas antes de sus bloques

- Escalada libre confirmada: concretar controles, superficies permitidas, transición
  a bordes y comportamiento de enemigos en altura. No introducir una trepada breve
  obligatoria ni un límite de resistencia sin discutir ese cambio con el autor.
- Oro confirmado: oro de partida soltado por enemigos, aumentando con tiempo y
  dificultad. Ajustar la curva y su relación con precios; no se ha pedido aumentar
  la moneda meta.
- Arte confirmado: se permiten imágenes e iconos originales en archivos. Definir
  estilo y tamaños; no implica comprar o usar packs de terceros automáticamente.
- Tamaño del mundo y cantidad de contenido: fijar tras medir movilidad, densidad
  de objetivos, presupuesto de memoria y rendimiento en el equipo de referencia.
