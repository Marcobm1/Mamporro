# Checkpoint U5 — audio, pulido y validación de equivalencia

Estado: **IMPLEMENTACIÓN AUTORIZADA Y EN CURSO desde el 04/10/2026.** Plan aprobado por el autor con las 11 dudas resueltas («Resolución del autor»). U4 aprobada manualmente el 04/10/2026; último cierre técnico `3754e31be16904207fd2ee25340bfa1ca095f181`. No iniciar U6 sin la aprobación manual de U5.

## Cómo retomar

- **Punto de partida:** `3754e31` (U4 aprobada). Plan en `31711bf` («Registra la aprobación de U4 y planifica U5»). Paso 1 `e7c9933` («Porta la síntesis de audio y el presupuesto de voces de U5»). Paso 2 `6e154ec` («Añade el motor de audio de U5»). Paso 3 `85f7a1b` («Conecta los sucesos de la partida al sonido de U5»). Paso 4 `22821d0` («Añade las partículas decorativas de U5»). Paso 5 en «Añade los números de daño de U5» (hash: `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Core/Effects/DamageNumbers.cs`).
- **Paso actual:** 6, cámara, sacudida y destellos (sin empezar).
- **Terminado:** pasos 1–5. Último Edit Mode 392/392, Play Mode 35/35, build y visual con medida de audio (04/10/2026).
- **A medias:** nada.
- **Sin commit a propósito:** siete ajustes Unity de «Cambios locales excluidos».
- **Siguiente paso exacto:** paso 6: `CameraRig` de la web en `U3Game.LateUpdate` (brazo de 6,2 m recorrido en pasos contra el terreno con margen 0,35 y mínimo 1,1, acercamiento inmediato y recuperación suave `damp(4)`; pivote 1,9/1,2 con `damp(8)` al deslizarse; FOV 70 + 12 × exceso de velocidad con `damp(4)`; sacudida: trauma 0–1, desplazamiento ±trauma²·0,3 m, decae 2,5/s, desactivada y a 0 sin la opción; golpe recibido 0,45, aparición del jefe 0,8, culetazo 1 cerca (< 2,5 radios) o 0,4, bata 0,7); escala de destello de enemigos élite 0,6 y jefe 0,3; parpadeo del jugador a 16 Hz mientras es invulnerable, sujeto a «Destellos de daño»; leer `src/render/CameraRig.ts` y `src/data/config.ts` (`CAMERA_CONFIG`).
- **Autorización:** pasos 1–9 seguidos, con checkpoint, pruebas, commit y push tras cada pieza. Detenerse solo ante una decisión nueva importante de diseño/arquitectura (o si el compresor propio resulta inestable, con latencia o coste inesperado) y al terminar U5.

Comprobación desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch --untracked-files=all
git log --oneline --decorate -12
git remote -v
```

Sin pull/stash automáticos ni limpieza. Un paso verificable cada vez; checkpoint, commit pequeño en español, fetch y push normal. Si el remoto cambia inesperadamente, detenerse e informar. Sin PR. Al terminar U5, detenerse para la prueba manual del autor; no empezar U6 antes.

## Alcance y límites

Según [MIGRACION_UNITY](MIGRACION_UNITY.md): síntesis y reproducción de audio (música normal e intensa, efectos), presupuesto de voces y de partículas, cámara y sacudida, destellos, opciones de reducción de efectos (las guardadas en U4 pasan a tener efecto), avisos de combate legibles, feedback visual equivalente a la web, pruebas ES/EN, pausa/foco, varias partidas seguidas, persistencia de opciones, rendimiento representativo en build y lista final de equivalencia antes de U6.

**No incluye** (después de U6 salvo autorización expresa): menos enemigos iniciales o más vida, más oro de partida, precios, mundo mayor, terreno nuevo, escalada, animaciones finales, personajes/enemigos/armas/tomos/mapas nuevos, overhaul visual, Steam (Input, Cloud, logros) ni telemetría. No es el rediseño artístico posterior. Sin assets comprados ni packs de terceros; todo el sonido sigue siendo original y procedural.

No modificar `src/` (la excepción de U4 era solo el exportador), ni `baseline.json`, `u2-combat.json`, `u3-world.json` o el corpus U4. No cambiar el contrato de guardado: las opciones ya existen en `SettingsDto`.

## Auditoría — referencia web (`0505b16`)

### Audio (`src/audio/`, `src/data/audio.ts`; decisiones del hito 6)

- **Síntesis PCM mono a 22 050 Hz**, una vez al desbloquear el audio. 20 efectos (`ui`, seis armas, `hit`, `critical`, `death`, `hurt`, `xp`, `gold`, `reward`, `level`, `shield`, `boss`, `blast`, `victory`, `defeat`): onda cuadrada con barrido de frecuencia, mezcla de ruido de `Rng('sonido')`, ataque de 4 ms y caída cuadrática. Cada uno tiene ganancia, prioridad (0–2) y enfriamiento.
- **Música:** dos arreglos de ocho compases a 132 BPM (melodía, bajo, bombo y charles con `Rng('musica')`; el intenso añade armonía y más charles), de igual duración (320 727 muestras, unos 14,5 s), en bucle y con bordes suaves.
- **Referencia congelada:** `unity/Docs/Reference/baseline.json` → `audio` guarda, por cada efecto y por las dos músicas, número de muestras, pico, suma de cuadrados y las 32 primeras muestras. `music-normal.wav`/`music-intense.wav`: 2 s de cada una. La propia referencia pide tolerancia numérica, sin usar hashes PCM como criterio.
- **Motor (`AudioEngine`):** un contexto; buses de efectos y música → maestro (0,8; 0 en silencio) → compresor (umbral −12 dB, relación 8; resto por defecto del navegador) → salida. Música por modo: menú ×0,65, partida ×1, **intensa** (jefe o enjambre: cambia al arreglo intenso conservando la posición del compás), pausa y cartas ×0,25, resultados ×1. Rampas de volumen de 15 ms (efectos/maestro) y 40 ms (música). Silencio o volumen 0 paran la pista y vacían los efectos.
- **Voces (`VoiceBudget`):** máximo 16 efectos; 4 reservadas para prioridad ≥ 1; enfriamiento por efecto; también se limita el número de nodos pendientes. Lo descartado no se reproduce más tarde.
- **Activación y foco:** la web exige un gesto para iniciar audio (limitación del navegador). Perder el foco (`blur`) pausa la partida; ocultar la pestaña suspende el contexto y elimina los efectos: al volver no hay cola de sonidos.
- **Sucesos:** clic en botones → `ui`; compras → `reward`; resultados → vacía efectos y `victory`/`defeat`; partida (`RunView`): disparo de cada arma, recoger XP/oro (uno por tick como mucho), golpe/crítico, muerte (jefe → `blast`), golpe al jugador (`hurt`), subida de nivel, escudo, objeto/baúl (`reward`), aparición del jefe (`boss`), culetazo y olla (`blast`), bata (`shield`).

### Efectos visuales (`src/render/`)

- **Partículas (`Particles.ts`, `ParticleBudget.ts`):** cubos instanciados con color; 1500 activas y 256 nuevas por fotograma; reducidas 400/64 y 25 % de cada ráfaga (redondeo hacia arriba). Llenas: se reutiliza una al azar. Aleatoriedad visual propia (`Rng('efectos')`), independiente de la partida. Ráfagas: crítico (3), muerte (12; especial 24; jefe 80), aparición (5 de polvo), subida de nivel (24), escudo (18), baúl (26), pipa (3), aparición del jefe (60), culetazo (20×2), olla (14 + 6 de humo), perla (5), bata (28×2), barrazo (4 migas), rayo (3 por enemigo), naftalina (3 por pulso), fregona (≤3 burbujas). No afectan a proyectiles ni a avisos de ataque.
- **Números de daño (`DamageNumbers.ts`):** hasta 140 números de 6 caracteres con los glifos de la fuente pixelada (contorno), en la imagen de baja resolución; críticos con color/tamaño propios y daño recibido por el jugador.
- **Sacudida (`CameraRig`):** «trauma» 0–1, temblor proporcional a su cuadrado (×0,3 m), decae 2,5/s. Golpe recibido 0,45; aparición del jefe 0,8; culetazo 1 cerca (0,4 lejos); bata 0,7. La opción la desactiva y pone el trauma a 0.
- **Cámara (`CameraRig`):** brazo de 6,2 m que se acorta si el terreno lo corta (mínimo 1,1 m) y vuelve suavemente; pivote que baja al deslizarse con suavizado; FOV 70 + hasta 12 al ir más rápido que la velocidad normal.
- **Destellos:** con la opción, destello rojo del HUD, destello blanco de enemigos golpeados (escala 0,6 en élites y 0,3 en el jefe) y **parpadeo del jugador** invulnerable (16 Hz). Los telegrafiados no dependen de ninguna opción.

## Auditoría — Unity actual (`3754e31`)

- **Audio:** no existe nada: sin `AudioListener` en la escena, sin `AudioSource`, sin clips. Opciones de volumen/silencio solo guardadas (U4). `AudioManager.asset`: salida 48 kHz, 32 voces reales.
- **Sucesos del núcleo C#:** `CombatRun.Emit` produce efectos persistentes de armas (`aura`, `arc`, `chain`, `pearl`, `slam`, `blast`, `shield`, `revive`, `chest`) y `Events` de avisos (`levelUp`, `bossSpawned`, `chestOpened`, `item`, oleadas…). **Faltan** los equivalentes de `weaponFired`, `pickup` (xp/oro), `damageNumber` (con nivel de crítico), `enemyKilled`, `enemySpawned`, `playerHit` y `enemyShot`.
- **Efectos:** `RunRenderer` dibuja enemigos, proyectiles, recogidas, interactuables, telegrafiados y efectos de armas como anillos/líneas con 13 materiales de color fijo (no hay color por instancia). El baúl abierto es un anillo (la web usa partículas). **No hay partículas decorativas ni números de daño.**
- **Cámara:** distancia fija de 6,2 m, solo se eleva sobre el terreno, sin sacudida, sin suavizado del pivote y sin aumento de FOV.
- **Destellos:** destello rojo del HUD y blanco de enemigos golpeados (umbral 0,3, sin la escala de élite/jefe), ambos sujetos a la opción. Falta el parpadeo del jugador.
- **Pausa y foco:** `OnApplicationFocus(false)` pausa la partida; `Application.runInBackground = true`. Sin audio, no hay nada que suspender.
- **Recursos reutilizables:** `Rng` con `Derive` (mismo algoritmo que la web), shader `RetroWorld` con `_InstanceColor` instanciado (sirve para partículas de color), `RenderMeshInstanced` por lotes, `QaSave` y lanzadores `scripts\u3.cmd`.
- **Rendimiento (ensayo del cierre U4, 04/10):** 1920×1080 y 2560×1440, de unos 1600 FPS (enjambre, 750 vivos) a 2680 FPS; **ningún fotograma por encima de 16,7 ms** en los ocho puntos (máximo 7,7 ms). Los picos aislados del minuto 5 de U3 no se repitieron. GPU y GC por fotograma siguen sin datos en la build normal.

## Qué falta exactamente

1. Síntesis C# de los 20 efectos y las 2 músicas, comprobada contra `baseline.json`.
2. Motor de audio Unity: escucha, clips, 16 voces con reserva y enfriamientos, música por modos con cambio sin perder el compás, buses y volúmenes de las opciones, silencio, compresión, pausa y foco, cambio de dispositivo.
3. Sucesos del núcleo que faltan, sin cambiar la simulación.
4. Sonidos de interfaz, compras, resultados y partida.
5. Partículas decorativas con presupuesto y opción reducida; ráfaga del baúl en lugar del anillo.
6. Números de daño.
7. Sacudida (con su opción), cámara equivalente si se aprueba, escala de destello de élite/jefe y parpadeo del jugador.
8. Ensayo de rendimiento representativo con audio, partículas y números; contadores nuevos; observación de picos.
9. Pruebas de sesiones seguidas, capturas ES/EN y lista final de equivalencia.

## Arquitectura propuesta

- **Datos y síntesis puros (`Mamporro.Core`, sin UnityEngine):** `AudioCatalog` (los 20 timbres, música, 22 050 Hz, 16/4 voces, 132 BPM), `AudioSynth.Sound/Music` → `float[]` (cálculo en `double`, almacenado en `float` como la web) y `VoiceBudget` con reloj inyectado. Se comparan con `baseline.json` con tolerancia.
- **Motor (`U5/AudioDirector`, MonoBehaviour único):** `AudioListener` añadido en código a la cámara del mundo (sin regenerar la escena). Clips `AudioClip.Create` + `SetData` una vez al arrancar (unos 0,7 M de muestras). Un grupo fijo de 16 `AudioSource` 2D para efectos y 2 para música (normal/intensa, cambio a la misma posición con `timeSamples`). Reloj de voces con `AudioSettings.dspTime`. Volúmenes = opción × modo × maestro 0,8, con rampas de 15/40 ms en `Update`. Sin plugins: la API estándar basta en 6000.6.3f1.
- **Compresión:** Unity no tiene un componente compresor fuera del `AudioMixer`, y un `AudioMixer` solo puede crearse a mano en el Editor. Propuesta: compresor/limitador propio en `OnAudioFilterRead` sobre la escucha (sin asignaciones, parámetros del compresor web: umbral −12 dB, relación 8, rodilla 30 dB, ataque 3 ms, liberación 250 ms). Aproxima el del navegador; no será idéntico. **Duda 3.**
- **Sucesos del núcleo:** canal tipado (`CombatFeedback`: tipo enumerado + posición + valor), sin cadenas ni asignaciones por suceso, con un receptor vacío por defecto. Se emite donde lo hace `Run.ts` y no consume la RNG de la partida. Prueba: las partidas de referencia dan el mismo estado con receptor y sin él, y las pruebas de equivalencia U2/U3 no cambian.
- **Despachador (`U5/RunFeedback`):** traduce cada suceso a sonido, partículas, números, sacudida y destello, como `RunView`. Un único punto, fácil de probar.
- **Partículas (`U5/Particles`):** arrays por campo (SoA) para 1500, `ParticleBudget` puro portado, `Rng('efectos').Derive('particulas')` visual, dibujo con `Graphics.RenderMeshInstanced` y color por instancia (`_InstanceColor` de `RetroWorld`), así que salen pixeladas y con vértices/dither PS1. No se usa el `ParticleSystem` de Unity ni un GameObject por partícula: así el presupuesto y la reducción son exactos.
- **Números de daño (`U5/DamageNumbers`):** atlas con los glifos de `0123456789!` de la fuente pixelada web (portados como datos), quads instanciados orientados a la cámara, 140 × 6 como máximo, dibujados en la textura interna.
- **Cámara (`U5/CameraRig`):** sacudida web (aleatoriedad visual propia) y, si se aprueba, brazo con colisión, suavizado del pivote y FOV extra. **Duda 4.**
- **Opciones:** `reducedParticles` → presupuesto; `cameraShake` → trauma; `flashes` → HUD, enemigos (con escala élite/jefe) y parpadeo; volúmenes y silencio → buses. Sin cambios en `SettingsDto`.

## Plan propuesto por pasos/commits verificables

| Paso | Contenido | Verificación |
| --- | --- | --- |
| 1. Síntesis y voces | `AudioCatalog`, `AudioSynth`, `VoiceBudget` en C# puro. | Edit: 20 efectos y 2 músicas contra `baseline.json` (muestras exactas, pico/energía/32 primeras con tolerancia), bordes a cero, finitos, sin saturar, repetibles; voces (las cuatro pruebas web: agrupar golpes, liberar, reservar 4, nunca más de 16). |
| 2. Motor de audio | `AudioDirector`: escucha, clips, efectos, música por modos, volúmenes/silencio desde opciones, compresor, pausa/foco/resultados, botones y compras, cambio de dispositivo. | Play: un solo `AudioListener` y 18 fuentes; modo y arreglo correctos en menú/partida/pausa/cartas/jefe/enjambre/resultados; posición conservada al pasar a intensa; volúmenes efectivos; silencio y volumen 0 paran; foco limpia efectos sin cola; voces ≤ 16. Build: señal medida a la salida (ver «Pruebas de audio»). |
| 3. Sucesos del núcleo y sonidos de partida | Canal `CombatFeedback` y sonidos de combate. | Edit: partidas de referencia idénticas con y sin receptor; recuento de sucesos por tipo en una partida fija; sin asignaciones por suceso. Play: horda real con voces acotadas. |
| 4. Partículas | `ParticleBudget`, `Particles`, todas las ráfagas de `RunView`, baúl con partículas, opción reducida. | Edit: presupuesto (pruebas web). Play: límites 1500/256 y 400/64/25 %; reducir al momento recorta; nueva partida vacía; sin crecimiento de memoria entre partidas. Visual. |
| 5. Números de daño | Atlas de glifos y números instanciados. | Play: tope 140, críticos y daño recibido, limpieza por partida. Visual ES/EN. |
| 6. Cámara, sacudida y destellos | Sacudida con opción; cámara equivalente si se aprueba; escala de destello élite/jefe; parpadeo del jugador. | Play: desactivar sacudida → trauma 0 y cámara estable; destellos off → sin rojo/blanco/parpadeo, telegrafiados intactos; curvas de cámara con casos fijos. |
| 7. Rendimiento representativo | Ensayo con audio, partículas, números, varias armas, disparos enemigos y recogidas; contadores de voces/partículas/números; build de desarrollo solo para asignaciones; observación de picos. | `scripts\u3.cmd benchmark` ampliado; informes JSON/CSV con condiciones. |
| 8. Sesiones, capturas y equivalencia | Ciclo de varias partidas en build, capturas ES/EN, `docs/EQUIVALENCIA_U5.md`. | Ensayo de varias partidas en build; Play de ciclo completo; capturas revisadas. |
| 9. Cierre U5 | Regresión completa, build final, guía de prueba manual (con escucha) y limitaciones. | Todas las pruebas; detenerse para prueba manual y aprobación. |

## Pruebas automáticas que se añadirán

- **Edit (puras):** síntesis contra la referencia; `VoiceBudget`; `ParticleBudget`; determinismo de la partida con/sin sucesos; recuento de sucesos; selección de modo de música; mapeo opción → ganancias; compresor (respuesta estática a niveles fijos, sin NaN).
- **Play (escena real con `QaSave`):** fuentes y escucha únicas; modos de música en todo el flujo; cambio a intensa con jefe y enjambre (QA F3); voces ≤ 16 bajo horda; silencio/volúmenes al momento y tras recargar; pausa y foco (`OnApplicationFocus`) sin cola; partículas y números con su tope y limpieza; sacudida y destellos según opciones; telegrafiados siempre visibles; ciclo inicio → partida → resultado ×3 sin fuentes, escuchas, partículas ni efectos acumulados, sin pausa arrastrada, opciones conservadas y liquidación una vez por partida.
- **Build:** ensayo de rendimiento; ensayo de varias partidas (`-u5-sessions`: N partidas aceleradas con QA hasta resultados, memoria y recuentos por ciclo); capturas; medida de señal de audio.

## Pruebas de audio en Windows

**Automático (demuestra que la señal existe, no cómo suena):**

- PCM idéntico (con tolerancia) a la referencia web.
- Un medidor en `OnAudioFilterRead` de la escucha registra RMS y pico reales a la salida de Unity: en la build, música en menú > 0; silencio → ~0; volumen de música 0 → solo efectos; efectos 0 → solo música; pausa → nivel de música ×0,25 aprox.; jefe/enjambre → arreglo intenso; tras perder el foco, sin efectos pendientes. Recuento de voces y descartes con 750 enemigos.
- Recuperación tras `AudioSettings.OnAudioConfigurationChanged` (simulado con `AudioSettings.Reset`).

**Solo con escucha del autor en la build:** que la música y cada efecto suenen bien y como en la web; mezcla y saturación con horda y enjambre; que los avisos importantes (golpe, nivel, escudo, jefe) se oigan sobre la horda; mute y volúmenes de oído; pausa atenuada; Alt+Tab real y vuelta sin ráfaga de sonidos; varias partidas seguidas sin músicas superpuestas; cambio de dispositivo de salida (auriculares/altavoces) con la build abierta. No se dirá «audio verificado» solo porque una fuente recibió un clip.

## Rendimiento

- **Condiciones:** build Windows x64 Mono, pantalla completa exclusiva en el monitor principal, 1920×1080 y 2560×1440, interna 360, dither/vértices activos, VSync 0, FPS ilimitados; Ryzen 7 7700X, RTX 4070 Ti SUPER, ~32 GB. Partida real con director, Remedios, 10 min, `MAMPORRO`, invulnerable de ensayo y primera carta automática, **con audio, partículas (normales y reducidas), números de daño y sacudida activos**; al empezar se conceden por QA varias armas para tener armas simultáneas; disparos de paloma, gemas y monedas aparecen solos en la partida. Puntos: min2, min5 (~300 vivos), min9 (~500) y enjambre (750).
- **Registro:** salida, interna, hardware, entidades, proyectiles propios/enemigos, recogidas, partículas activas, números, voces activas y descartadas, FPS medio, P95/P99/máximo, fotogramas > 16,7 ms, tick lógico, CPU, memoria. GPU solo si la medida es válida (duda 7). GC por fotograma solo en una build de desarrollo separada y etiquetada (duda 8), sin mezclar sus FPS con los de la build normal.
- **Criterio:** 60 FPS en el equipo de referencia con margen. Los miles de FPS actuales solo indican margen: no se sacrificará calidad para conservarlos, pero se explicará cualquier caída grande.
- **Picos:** comparar fotogramas > 16,7 ms con el ensayo del cierre U4 (ninguno). Si reaparecen, instrumentación acotada (marcas de partículas, números, audio y cartas en el CSV); sin optimizaciones especulativas ni investigación abierta si no se reproducen.

## Capturas ES/EN

Propuesta sin redundancia:

- **1280×720** (la más exigente para la legibilidad, la del plan oficial) **y 1920×1080** (la resolución principal del autor), **en español y en inglés**: inicio, preparación, partida normal con números/partículas, horda, jefe con telegrafiado, cartas, pausa, opciones, tienda, misiones y resultados (11 × 2 × 2 = 44).
- Las vistas del mundo sin interfaz (vista alta, cuatro sitios) solo una vez, a 1920×1080: no dependen del idioma.
- 2560×1440 queda cubierta por las capturas del ensayo de rendimiento.

## Resolución del autor (04/10/2026)

Plan aprobado e implementación autorizada (pasos 1–9 seguidos). Respuestas:

1. **Foco:** al perderlo, pausar, descartar efectos pendientes y silenciar todo. Al volver, sigue en pausa, sin cola de efectos, música al 25 % hasta pulsar Continuar. Diferencia intencionada con la web.
2. **Inicio del audio:** música del menú desde el arranque, sin esperar a un clic.
3. **Compresor:** propio, pequeño, aislado y sin asignaciones en el hilo de audio; umbral −12 dB, relación 8, rodilla 30 dB, ataque 3 ms, liberación 250 ms. Aproximación documentada, sin NaN/Infinity, con pruebas deterministas. Si resulta inestable o caro, detenerse antes de cambiar de arquitectura.
4. **Cámara:** equivalencia web (brazo contra el terreno, recuperación suave, pivote suavizado al deslizarse, FOV extra a gran velocidad y sacudida). No autoriza cámara nueva contra estructuras.
5. **Números de daño:** portarlos equivalentes (fuente pixelada, crítico, daño recibido, 140, limpieza entre partidas).
6. **Parpadeo del jugador:** sí, sujeto a «Destellos de daño»; los telegrafiados siempre visibles. Sacudida con valores web (0,45 / 0,8 / 1 / 0,4 / 0,7), trauma 0–1 al cuadrado, la opción la desactiva y pone el trauma a 0.
7. **GPU:** no publicar cambios de `ProjectSettings.asset`. Ensayo principal en build normal con GPU «n/d» si no hay medida válida; intento aparte en **diagnóstico Development**, nunca presentado como rendimiento final.
8. **Asignaciones:** pasada separada «diagnóstico Development» (GC, memoria, GPU si es válida), sin mezclar sus FPS.
9. **Capturas:** 11 pantallas × 1280×720 y 1920×1080 × ES/EN (44), mundo sin interfaz una vez a 1920×1080, 2560×1440 solo en el ensayo. Revisarlas de verdad.
10. **Nombre:** no cambiar `Mamporro-U3.exe` ni el producto «Mamporro U1» en U5; se decide en U6 con migración segura.
11. **Ejecución:** pasos 1–9 seguidos dentro del alcance; sin balance nuevo ni contenido.

## Dudas planteadas al autor (resueltas arriba)

1. **Audio al perder el foco (Alt+Tab):** la web pausa al perder el foco (la música sigue al 25 %) y solo suspende el audio si la pestaña se oculta. En escritorio, una ventana sin foco suele estar tapada. **Recomendado:** al perder el foco, pausar, vaciar efectos y silenciar todo; al volver, seguir en pausa con la música al 25 %. Alternativa: igual que la web (música al 25 % en segundo plano).
2. **Inicio del audio:** la web espera un primer clic/tecla por imposición del navegador. **Recomendado:** en Windows, música del menú desde el arranque (diferencia intencionada documentada).
3. **Compresión de la mezcla:** **recomendado** el compresor propio en `OnAudioFilterRead` con los parámetros web (aproximado). Alternativas: crear tú un `AudioMixer` en el Editor, o prescindir del compresor y confiar en el presupuesto de voces.
4. **Cámara equivalente a la web:** la cámara Unity aprobada en U3/U4 es más simple. **Recomendado** portar el brazo que se acorta contra el terreno, el pivote suavizado al deslizarse y el FOV extra a velocidad alta, porque son la sensación aprobada en la web. **Cambia la sensación respecto a la build que acabas de aprobar.** Alternativa: solo la sacudida y dejar la cámara actual.
5. **Números de daño:** existen en la web y no en Unity. **Recomendado** portarlos tal cual (glifos pixelados de la fuente web).
6. **Parpadeo del jugador invulnerable:** se aplicaría al avatar provisional de cajas. **Recomendado** sí, como la web, sujeto a «Destellos de daño».
7. **GPU en el ensayo:** requiere activar «Frame Timing Stats» en `ProjectSettings.asset`, que tiene cambios locales excluidos. Opciones: (a) GPU sigue «n/d»; (b) autorizas publicar solo esa línea de ajuste, sin el resto de cambios locales; (c) medir la GPU a mano con una herramienta externa. **Recomendado** (b); si prefieres no tocar ese archivo, (a).
8. **Asignaciones por fotograma:** solo se miden en una build de desarrollo. **Recomendado** una pasada separada y etiquetada con build de desarrollo, que no sustituye a la de rendimiento.
9. **Capturas:** ¿te vale 1280×720 + 1920×1080 en ES y EN (44 capturas), con 2560×1440 solo en el ensayo?
10. **Nombre de la build y del producto:** sigue siendo `Mamporro-U3.exe` y «Mamporro U1». Cambiar el producto cambiaría la carpeta del guardado (`LocalLow\Mamporro\Mamporro U1`). **Recomendado** no tocarlo en U5 y decidirlo en U6 con migración de carpeta.
11. **Autorización:** si apruebas el plan, ¿puedo hacer los pasos 1–9 seguidos, deteniéndome solo ante una decisión nueva importante, como en U4?

## Decisiones que pueden afectar a la sensación del juego

Duda 1 (silencio al perder el foco), duda 4 (cámara: brazo, FOV y suavizado), duda 5 (números de daño en pantalla) y la intensidad de la sacudida (se propone la de la web: 0,45/0,8/1/0,7). El resto es equivalencia técnica.

## Cambios locales excluidos

Conservar sin publicar ni restaurar:

- `unity/ProjectSettings/ProjectSettings.asset` (identificadores locales de nube).
- `unity/ProjectSettings/ProjectAuditorSettings.asset` (espacios/EOL).
- `unity/ProjectSettings/PackageManagerSettings.asset` (sin seguimiento).
- `unity/ProjectSettings/URPProjectSettings.asset` (sin seguimiento).
- Espacios/EOL en `unity/Assets/Mamporro/Generated/RetroPipeline.asset`, `unity/Assets/UniversalRenderPipelineGlobalSettings.asset` y `unity/ProjectSettings/GraphicsSettings.asset`.

## Registro de sesiones y relevos

### 04/10/2026 — aprobación de U4 y planificación de U5 — Claude Code

- **Punto de partida:** `3754e31be16904207fd2ee25340bfa1ca095f181`, remoto igual; solo los siete ajustes Unity excluidos.
- **Trabajo:** registro de la aprobación manual de U4 (04/10/2026) y de las dos mejoras futuras de balance del autor (menos enemigos al inicio pero más resistentes, con crecimiento progresivo; más oro **de partida**); estado y hoja de ruta al día; auditoría web/Unity de audio, efectos, cámara, destellos, pausa/foco y rendimiento; este plan.
- **Pruebas ejecutadas:** ninguna nueva (cambio solo documental). Se revisaron los JSON del ensayo final de U4 (ningún fotograma > 16,7 ms).
- **Commit/push:** «Registra la aprobación de U4 y planifica U5»; fetch previo y push normal si el remoto sigue en `3754e31`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** esperar respuesta del autor a las dudas; no programar U5 antes.

### 04/10/2026 — paso 1: síntesis y presupuesto de voces — Claude Code

- **Punto de partida:** `31711bfc0240dca833803cbcae7e6aad47365e64` (plan publicado); solo los siete ajustes Unity excluidos.
- **Trabajo:** `Core/Audio/AudioCatalog.cs` (20 timbres de `data/audio.ts` en el mismo orden, 22 050 Hz, 16/4 voces, 132 BPM, melodía y bajo), `Core/Audio/AudioSynth.cs` (`Sound` y `Music` portados de `synth.ts`: cálculo en double, muestras en float, `Rng("sonido")`/`Rng("musica")`, `Math.round` de JS para la longitud) y `Core/Audio/VoiceBudget.cs` (16 voces, 4 reservadas para prioridad ≥ 1, enfriamiento por timbre, lo rechazado se descarta; arrays fijos, sin asignaciones, reloj inyectado). C# puro en `Mamporro.Core`, sin UnityEngine.
- **Pruebas nuevas (Edit, 6, `Tests/Core/AudioReferenceTests.cs`):** catálogo igual a la referencia (IDs, orden y 22 050 Hz); los 20 efectos contra `baseline.json` → `audio` (muestras exactas, pico ±1e-6, energía ±1e-6 relativa, 32 primeras muestras ±1e-7) más las propiedades de `audio.test.ts` (finitos, ≤ 0,25, bordes a cero, audibles, repetibles); las dos músicas contra la referencia (320 727 muestras, igual duración, distintas, pico 0,1–0,5, bordes a cero); las cuatro comprobaciones de `VoiceBudget` web (agrupar golpes y liberar; 4 reservadas y nunca más de 16, sin aplazar) y expiración/enfriamiento por timbre. La sección `audio` se extrae del JSON congelado y se lee con `ProgressJson`; no se regenera nada.
- **Pruebas ejecutadas (04/10/2026, Europe/Madrid):** `scripts\u3.cmd edit` **373/373** (367 + 6), a la primera.
- **No ejecutado:** Play/build (sin cambios de escena ni runtime).
- **Commit/push:** «Porta la síntesis de audio y el presupuesto de voces de U5»; fetch previo y push normal si el remoto sigue en `31711bf`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 2 (ver «Cómo retomar»).

### 04/10/2026 — paso 2: motor de audio — Claude Code

- **Punto de partida:** `e7c9933` (paso 1 publicado); solo los siete ajustes Unity excluidos.
- **Trabajo:** `U3/AudioDirector.cs`: escucha añadida en código a la cámara del mundo (sin regenerar la escena); 20 clips y 2 pistas `AudioClip.Create` desde `AudioSynth` (22 050 Hz, mono); 16 fuentes 2D de efectos con `VoiceBudget` (reloj `AudioSettings.dspTime`; sin fuente libre o sin presupuesto se descarta, nunca se aplaza) y 2 de música; modos menú ×0,65, partida ×1, intensa (jefe o enjambre: arreglo intenso a la misma posición del reloj de audio, como la web), pausa/cartas ×0,25 (con el arreglo normal, como la web) y resultados ×1; maestro 0,8 (0 en silencio) y rampas exponenciales de 15 ms (maestro/efectos) y 40 ms (música); volumen de música 0 o silencio paran la pista; volumen de efectos 0 o silencio vacían los efectos; foco (resolución 1): sin foco, `AudioListener.pause` y efectos descartados, al volver pausa con música al 25 % hasta Continuar; cambio de dispositivo (`OnAudioConfigurationChanged`) rehace efectos y pista. `MixFilter` en la escucha: `Core/Audio/MixCompressor.cs` (puro, sin asignaciones, rodilla cuadrática, detector de pico enlazado, ataque/liberación de un polo en dB, compensación fija como el navegador ≈ +6,4 dB, tope ±1, NaN → silencio) y medidor RMS/pico. Sonidos de interfaz: «ui» en cualquier botón de pantallas y cartas, «reward» en compras correctas, «victory»/«defeat» en resultados tras vaciar efectos. Audio desde el arranque (resolución 2).
- **Medida en build:** la comprobación visual mide la salida real tras el compresor y escribe `audio-report.json`; el lanzador lo exige. Resultado (48 kHz): menú RMS **0,0123**, silencio **0,0**, música 0 sin efectos **0,0**, un «level» **0,0518**, restaurado **0,0135**, pico **0,114**. Demuestra señal y silencio en el dispositivo, no la calidad: eso es la escucha del autor.
- **Pruebas nuevas:** Edit (6, `MixCompressorTests`): curva estática, solo compensación con señal baja, compresión estable sin superar ±1, ataque rápido y liberación ≈ 250 ms (e⁻¹), muestras no finitas → silencio, determinismo y sin recolecciones en 5000 bloques. Play (3, `U5AudioSceneTests`): una escucha y 18 fuentes, clips con la longitud de la síntesis, niveles exactos por modo (menú, partida, pausa, jefe/intensa, cartas, resultados), arreglo normal en pausa; ráfagas con ≤ 16 voces y descartes; volumen de música/efectos 0 y silencio al momento, guardado y tras recargar; botón con «ui»; pérdida de foco (silencio, efectos descartados, nada admitido) y vuelta en pausa a 0,125 y 0,5 al continuar.
- **Pruebas ejecutadas (04/10/2026):** `scripts\u3.cmd edit` **379/379**; `scripts\u3.cmd play` **32/32** (una pasada 31/32 por un `Debug.Log` de diagnóstico que `LogAssert` tomó como inesperado; el modo batch tiene salida real 48 kHz estéreo); `scripts\u3.cmd build` correcta; `scripts\u3.cmd visual` 19 capturas + medida de audio correcta.
- **Limitaciones:** los efectos de combate llegan en el paso 3. El compresor es una aproximación del navegador; la compensación de ganancia fija lo hace sonar más fuerte que sin compresor, como en la web. Sin escucha humana todavía.
- **Commit/push:** «Añade el motor de audio de U5»; fetch previo y push normal si el remoto sigue en `e7c9933`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 3 (ver «Cómo retomar»).

### 04/10/2026 — paso 3: sucesos de la partida y sonidos — Claude Code

- **Punto de partida:** `6e154ec` (paso 2 publicado); solo los siete ajustes Unity excluidos.
- **Trabajo:** `Core/CombatFeedback.cs`: `FeedbackKind` (disparo de arma, recogida de XP/oro, golpe con nivel de crítico, baja con tipo, aparición, golpe al jugador con daño, pipa, nivel, escudo, objeto, baúl, jefe, culetazo, olla, perla, bata), `CombatFeedback` (struct) e `ICombatFeedback` (`in`, sin asignaciones). `CombatRun.Feedback`/`Notify` emiten en los mismos puntos que `fx.*` de `Run.ts`, siempre después del hecho y sin consumir RNG: arma tras `StepWeapon` si `SincePulse==0`; recogidas antes de ganar XP/oro; golpe dentro de `HitEnemy` (con crítico; perla y olla con 0); baja en `RemoveDead`; golpe al jugador tras la mitigación y antes de la bata; pipa en `Shoot`; nivel por cada nivel ganado; escudo, bata, objeto, jefe, culetazo (`Boss.cs`), olla (`FlushDead`), baúl (`WorldRun.OpenChest`) y aparición del director (`WorldSpawns.OnSpawn`). `U3/RunFeedback.cs`: receptor único con los sonidos de `RunView` (arma → su timbre, `xp`, `gold`, `hit`/`critical`, `death` o `blast` si es el jefe, `hurt`, `level`, `shield` con escudo y bata, `reward` con objeto y baúl, `boss`, `blast` con culetazo y olla) y recuento por tipo. `U3Game` lo conecta a cada partida.
- **Pruebas nuevas:** Edit (4, `CombatFeedbackTests`): las cuatro partidas de `u3-world.json` jugadas con y sin observador dan **exactamente** la misma cronología, detalle por tick (posiciones de enemigos incluidas), totales, armas y resultado; recuentos coherentes (una baja por muerte, un nivel por subida, baúles, apariciones del director, objetos, jefe), sin valores no finitos. Ejemplo: `remedios-5min-invencible` → 5444 golpes, 1412 bajas, 2162 apariciones, 2429 pipas, 31 niveles; `armario-jefe-victoria` → 1 jefe, 23 culetazos. Play (1): partida real con horda: armas y golpes suenan, una baja por muerte, descartes por enfriamiento y nunca más de 16 voces. `IntegratedRunTests.Play` acepta un observador opcional; sus expectativas no cambian.
- **Pruebas ejecutadas (04/10/2026):** `scripts\u3.cmd edit` **383/383**; `scripts\u3.cmd play` **33/33**; `scripts\u3.cmd build` correcta; `scripts\u3.cmd visual` 19 capturas + audio correcto (menú 0,0125; silencio 0; efecto 0,052).
- **Limitaciones:** la prueba Play de horda es corta (pocas bajas en 3 s); la carga alta de voces se medirá en el ensayo del paso 7. Partículas, números y cámara llegan en los pasos 4–6 por el mismo receptor.
- **Commit/push:** «Conecta los sucesos de la partida al sonido de U5»; fetch previo y push normal si el remoto sigue en `6e154ec`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 4 (ver «Cómo retomar»).

### 04/10/2026 — paso 4: partículas — Claude Code

- **Punto de partida:** `85f7a1b` (paso 3 publicado); solo los siete ajustes Unity excluidos.
- **Trabajo:** `Core/Effects/Particles.cs` (puro): `ParticleBudget` (256/1500; reducido 64/400 y ⌈25 %⌉ de cada ráfaga) y `ParticleField` (arrays por campo para 1500, ráfagas como `Particles.burst`: dirección, velocidad 0,3–1, subida 0,2–1 ×0,8 + impulso, vida ×0,7–1,2, tamaño ×0,6–1,3, gravedad 18 por defecto, giro; reutiliza una al azar si está lleno; actualización con gravedad, giro 6/s y encogimiento en el último tercio; reducir recorta al momento). `U3/ParticleRenderer.cs`: cubos instanciados con color por instancia (`_InstanceColor` de `RetroWorld`, el material de combate ya incluido en la build) en lotes de 1023, buffers fijos. `RunFeedback`: todas las ráfagas de `RunView` con colores de la paleta y restos de cada enemigo de la web (crítico, muerte 12/24/80, aparición, pipa, nivel, escudo, baúl, aparición del jefe, culetazo, olla con humo, perla, bata) y, desde los efectos de armas, migas del barrazo, chispas del rayo, vapores de la naftalina y burbujas de la fregona (RNG visual «efectos/armas» y «efectos/particulas»). Se mueven solo jugando (en pausa, cartas y resultados quedan quietas, como la web); cada partida empieza sin partículas. **Diferencia de presentación retirada:** baúl, escudo, bata, culetazo, olla y perla ya no se dibujan como anillos/líneas (eran sustitutos de U3); como en la web, son partículas. Los telegrafiados no cambian. Partículas iluminadas por el shader del mundo (la web las dibuja sin luz): colores algo más apagados en sombra.
- **Pruebas nuevas:** Edit (5, `ParticleTests`): las dos pruebas de `ParticleBudget.test.ts` y la de ⌈25 %⌉; campo con límites, recorte al reducir, vidas, colores y escala; movimiento, pausa con dt 0 y encogimiento; aleatoriedad visual determinista y sin asignaciones. Play (1, `U5EffectsSceneTests`): partida real con horda (partículas presentes y todas dibujadas), ráfaga enorme ≤ 256 nuevas, pausa quieta, opción reducida ≤ 400 al momento y guardada, nueva partida vacía.
- **Pruebas ejecutadas (04/10/2026):** `scripts\u3.cmd edit` **388/388**; `scripts\u3.cmd play` **34/34**; `scripts\u3.cmd build` correcta; `scripts\u3.cmd visual` 19 capturas + audio correcto; en «combate» se ven las partículas en la build.
- **Commit/push:** «Añade las partículas decorativas de U5»; fetch previo y push normal si el remoto sigue en `85f7a1b`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 5 (ver «Cómo retomar»).

### 04/10/2026 — paso 5: números de daño — Claude Code

- **Punto de partida:** `22821d0` (paso 4 publicado); solo los siete ajustes Unity excluidos.
- **Trabajo:** `Core/Effects/DamageNumbers.cs` (puro): hasta 140 números de 6 caracteres (el más viejo se va primero), `Math.round` con mínimo 1, «!» crítico y «!!» supercrítico, vida 0,75 s, subida 1,5t − 0,6t², desvanecido en el último 35 %, «pop» de 0,08 s, deriva y dispersión de ±0,25 con RNG visual «efectos/numeros», escala 1,45 crítico y 1,3 daño recibido; dígitos en buffer fijo, sin cadenas. `U3/DamageNumberRenderer.cs`: atlas generado con los glifos de `src/ui/font/glyphs.ts` (celdas 7×9, contorno negro de 1 píxel), malla dinámica de quads orientados a la cámara (altura 0,42 m, avance 6/9), colores de `palette.ts` (blanco, amarillo, naranja, rojo para el jugador), sin prueba de profundidad y dibujada en la textura interna. Sombreador propio `U3/Resources/DamageNumbers.shader` (en `Resources` para entrar en la build sin regenerar la escena). `RunFeedback`: número en cada golpe (con su nivel de crítico) y en cada daño recibido (2 m sobre el jugador). Quietos en pausa, limpios en cada partida. Partículas y números se dibujan ahora tras colocar la cámara.
- **Pruebas nuevas:** Edit (4, `DamageNumberTests`): texto (redondeo JS, mínimo 1, «!»/«!!», recorte a 6), tope de 140 retirando el más viejo, subida/desvanecido/«pop»/vida/escalas con los valores web, sin asignaciones. Play (1): horda real con números visibles y dibujados, número rojo al recibir daño, quietos en pausa y ninguno en la partida siguiente.
- **Pruebas ejecutadas (04/10/2026):** `scripts\u3.cmd edit` **392/392**; `scripts\u3.cmd play` **35/35** (una pasada 28/35: la malla validaba los vértices nuevos contra los índices del fotograma anterior; corregido vaciando antes los índices); `scripts\u3.cmd build` correcta; `scripts\u3.cmd visual` 19 capturas + audio correcto; números visibles en «combate» (pequeños en esa vista lejana: a 360 de altura interna un glifo ocupa unos 5 píxeles, como en la web).
- **Commit/push:** «Añade los números de daño de U5»; fetch previo y push normal si el remoto sigue en `22821d0`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 6 (ver «Cómo retomar»).

## Checkpoint al terminar U5

Pendiente: U5 en implementación.
