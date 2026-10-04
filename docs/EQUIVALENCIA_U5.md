# Equivalencia Unity ↔ web aprobada — validación final de U5

Estado: U5 implementado y verificado el 04/10/2026, pendiente de la prueba manual del autor.

Referencia: web aprobada `0505b1690656d15188860157612455639820fe1f` (hito 6) y referencias congeladas en `unity/Docs/Reference/` (`baseline.json`, `u2-combat.json`, `u3-world.json`, corpus U4). Base Unity: U1–U4 aprobadas; U5 implementado en `docs/PROGRESO_U5.md`. Este documento clasifica, por sistemas, qué es **equivalente**, qué es una **diferencia intencionada**, qué **limitación** se acepta y qué se **aplaza** al rediseño posterior a U6. No exige identidad píxel a píxel: lo importante son reglas, feedback, legibilidad, funcionalidad, opciones, audio, progresión, guardado, controles y experiencia.

## Cómo se ha comprobado

- **Automático:** pruebas Edit (reglas puras, referencias congeladas, PCM, compresor, presupuestos, números, partidas de referencia) y Play (escena real con guardado temporal), build Windows x64 Mono, comprobación visual con medida de audio real y varias partidas seguidas, capturas ES/EN, ensayo de rendimiento y diagnóstico Development. Resultados y fechas en `PROGRESO_U5.md`.
- **Manual (autor):** escucha, sensación de cámara/sacudida, legibilidad y prueba completa de U5 («Instrucciones de prueba manual»).

## Reglas, partida y progresión

| Sistema | Estado | Evidencia |
| --- | --- | --- |
| Varias partidas seguidas | Sin estado arrastrado ni progreso duplicado | 5 partidas en la build (`sessions-report.json`) y 3 en Play Mode |
| RNG, fórmulas, catálogo, textos ES/EN | Equivalente | `ReferenceTests`, `CatalogReferenceTests` contra `baseline.json` |
| Combate (armas, enemigos, proyectiles, pasivas, tomos, objetos) | Equivalente | `u2-combat.json` (U2) |
| Mundo procedural, director, oleadas, élites, enjambre, interactuables, jefe y victoria | Equivalente (tolerancias documentadas en U3) | `u3-world.json`, cuatro partidas completas idénticas en la cronología de los primeros 120 s |
| Meta (Calderilla, tienda, misiones, extras, liquidación única) | Equivalente | U4, corpus de 8 transferencias y 6 secuencias |
| Guardado y transferencia web → Unity | Equivalente en datos; formato propio de Unity | U4 (`mamporro.progress` v1, `mamporro.unity-save` v1) |
| Sucesos de feedback (U5) | Observadores puros | Las cuatro partidas de referencia son idénticas con y sin receptor (`CombatFeedbackTests`) |

## Audio

| Aspecto | Estado |
| --- | --- |
| 20 efectos y 2 músicas (22 050 Hz) | **Equivalente**: PCM C# igual a la referencia congelada (pico ±1e-6, energía ±1e-6 relativa, primeras muestras ±1e-7) |
| Arreglo intenso con jefe o enjambre, cambio a la misma posición del compás | Equivalente |
| Niveles por modo (menú ×0,65, partida ×1, pausa/cartas ×0,25, resultados ×1), maestro 0,8, rampas 15/40 ms | Equivalente |
| 16 voces, 4 reservadas para prioridad, enfriamiento por timbre, descarte sin cola | Equivalente (pruebas web portadas) |
| Silencio y volúmenes de música/efectos | Equivalente (y se guardan desde U4) |
| Sonidos de interfaz, compras, resultados y combate (los de `RunView`) | Equivalente |
| Compresor de la mezcla | **Aproximación**: mismos parámetros (−12 dB, 8:1, rodilla 30 dB, 3/250 ms) y compensación fija como el navegador; la curva exacta del `DynamicsCompressorNode` no se reproduce bit a bit |
| Inicio del audio | **Diferencia intencionada**: suena desde el arranque (la web espera un gesto por imposición del navegador) |
| Pérdida de foco | **Diferencia intencionada**: silencio total y efectos descartados; al volver sigue en pausa con la música al 25 % hasta Continuar (la web solo pausa; suspende el audio si la pestaña se oculta) |
| Cambio de dispositivo de salida | Específico de Windows: se rehacen efectos y música tras el cambio; requiere comprobación manual |

## Feedback visual y cámara

| Aspecto | Estado |
| --- | --- |
| Partículas decorativas (1500/256; reducidas 400/64 y ⌈25 %⌉) y todas las ráfagas de `RunView` | Equivalente en cantidad, colores, físicas y presupuesto |
| Baúl, escudo, bata, culetazo, olla y perla | **Equivalente desde U5**: partículas, como la web (en U3–U4 eran anillos/líneas provisionales) |
| Iluminación de partículas | Limitación menor: el shader del mundo las ilumina (la web las dibuja sin luz) |
| Números de daño (140 × 6, 0,75 s, crítico, supercrítico, daño recibido, fuente pixelada) | Equivalente |
| Cámara: brazo contra el terreno, recuperación, pivote al deslizarse, FOV por velocidad, inclinación y límites | Equivalente (solo terreno, como la web) |
| Sacudida (trauma, valores 0,45/0,8/1/0,4/0,7) y su opción | Equivalente |
| Destello blanco de enemigos, rojo del HUD y parpadeo del jugador, con su opción | Equivalente; **limitación**: el destello del jefe (mezcla del 30 % en la web) no se ve con la paleta fija del render de combate |
| Telegrafiados de embestida y del jefe | Equivalentes y siempre visibles (ninguna opción los oculta) |
| Modelos de cajas, avatar provisional, fuente integrada de Unity en menús/HUD | **Aplazado** al acabado visual posterior a U6 (aprobado así en U3/U4) |

## Interfaz, opciones y controles

| Aspecto | Estado |
| --- | --- |
| Menús, preparación, personajes, tienda, misiones, resultados, importación | Equivalentes en contenido y textos ES/EN (U4) |
| 14 opciones persistentes | Equivalentes y todas con efecto desde U5 |
| Pausa con estadísticas, objetos, semilla, opciones y abandono con confirmación | Equivalente |
| Controles (WASD, ratón, Espacio, Mayús/C, Ctrl opcional, E, Esc, F3 y cartas 1–4/R/X/B) | Equivalentes; F1/F2/F9/F6/F8 son atajos de QA propios de Unity |
| Controles por pasos (−/+) en lugar de deslizadores | Diferencia de presentación aceptada en U4 |

## Rendimiento

Objetivo: 60 FPS con margen en el equipo de referencia (Ryzen 7 7700X, RTX 4070 Ti SUPER, ~32 GB). **Cumplido con mucho margen** en la build normal con audio, partículas, números y sacudida: enjambre de 715–750 vivos a ≈ 1350 FPS (P99 1,7 ms) y cuatro armas simultáneas a ≈ 1440–1470 FPS en el enjambre, a 1920×1080 y 2560×1440; fotogramas > 16,67 ms solo aislados (2560×1440, cuatro armas). Diagnóstico Development aparte: 9–15 KB por fotograma (HUD de U3) y GPU 0,14–0,27 ms. Cifras completas en `PROGRESO_U5.md` («Checkpoint al terminar U5» y paso 7). Los miles de FPS solo indican margen.

## Aplazado a después de U6 (no son fallos de la migración)

Mundo y estructuras mayores; terreno rectangular con mesetas y rampas; escalada; menos enemigos al inicio pero más resistentes y crecimiento progresivo; más oro de partida; acabado retro profesional, animaciones, arte e iconos originales; más contenido; cámara avanzada contra estructuras; mando/remapeo; Steam. Nombre definitivo del ejecutable/producto y migración de la carpeta de guardado: U6.
