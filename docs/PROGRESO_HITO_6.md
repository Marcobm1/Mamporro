# Hito 6: punto de continuidad

Hito probado y aprobado por el autor el 28/09/2026. Continuación: `unity/Docs/U0_REFERENCIA.md`.

Actualizado: 28/09/2026. Rama: `claude/zen-pasteur-674ik0`. Sin PR.
Base aprobada por el autor: `5e7b95a` (hito 5 completo).

## Acuerdos aprobados

- Música arcade cómica y enérgica, con variante intensa para jefe/enjambre.
- Efectos de intensidad moderada; opciones para reducir partículas y desactivar
  sacudidas y destellos. Mantener legibles los avisos de ataques.
- Autorizados ajustes de balance justificados por pruebas y documentados.
- Todo procedural, sin assets externos, TypeScript estricto y textos ES/EN.
- Comentar avances en la conversación y guardar puntos estables con commits.
- Tras terminar y validar este pulido, **antes de añadir más contenido**, plantear
  migración a Unity. Preparar `docs/MIGRACION_UNITY.md` con plan y fuentes oficiales;
  no crear aún proyecto Unity ni empezar a portar el juego.

## Bloques

| Bloque | Estado | Próximo paso |
| --- | --- | --- |
| Acuerdos y continuidad | Registrados | Esperar comentarios del autor |
| Audio y música | Completo | Timbres, dos arreglos, límites y suspensión probados |
| Pulido visual y opciones | Completo | Partículas, feedback, opciones y guardado v3 |
| Balance | Revisión inicial completa | Sin cambios numéricos; falta valoración jugando |
| Verificación final | Correcta | 202 tests, typecheck/build, navegador ES/EN |
| Documentación y Unity | Completa | Guía de contenido y migración U0–U6; no iniciada |

## Cómo retomar

1. Leer `CLAUDE.md`, este archivo y `docs/DECISIONES.md`.
2. Comprobar `git status` y `git log` y comparar con la rama remota antes de editar.
3. Conservar cambios en curso; ejecutar los tests del bloque que se esté retomando.
4. Seguir el primer bloque pendiente. El hito está autorizado: no volver a pedir
   aprobación por decisiones ya recogidas aquí.
5. Tras cada bloque estable, actualizar esta tabla y las pruebas, hacer commit
   pequeño en español y subirlo. Cada commit debe compilar y pasar los tests.
6. Al entregar el hito, detenerse para que el autor lo pruebe. Unity es el siguiente
   proyecto propuesto, no una ampliación automática del alcance de este hito.

## Entorno y subida

- Node 24; `npm ci`, `npm run typecheck`, `npm test`, `npm run build`.
- Usar `?test` y Chromium con SwiftShader. La captura real del ratón y los FPS
  del equipo del autor requieren su comprobación.
- El push por terminal puede carecer de credenciales. En el hito 5 se usó la
  conexión GitHub para árboles/commits y avance de rama sin force. Comparar el
  SHA del árbol con el local antes de publicar y verificar luego la rama.
- No depender de `/tmp`: es temporal. Los resultados necesarios para retomar
  deben quedar resumidos aquí y los scripts reutilizables en el repositorio.

## Verificación registrada

- Base aprobada: 188 tests / 24 ficheros en hito 5.
- Hito 6: **202 tests / 27 ficheros**, `npm run typecheck` y `npm run build`
  correctos. Los commits intermedios también compilan y pasan sus tests.
- Navegador Chromium/SwiftShader, `?test`, ES/EN: menú, opciones, persistencia v3,
  silencio, saturación de efectos, suspensión/reanudación de audio, PCM mediante
  OfflineAudioContext, combate, jefe y pausa. **Sin errores de consola**.
- Capturas por idioma: menú/opciones/combate/jefe a 1280×720, pausa a 1600×900.
  Revisión visual realizada; menú de opciones sin encabezados duplicados.
- Pico de nodos de efectos en la prueba de estrés: **16**; al suspender: **0**.
  Contexto vuelve a `running`; una sola pista. La energía PCM renderizada es
  positiva (59,59 en el segundo de muestra), sin exigir dispositivo de salida.
- Combate con partículas reducidas: 64 emitidas en el frame de estrés;
  presupuesto máximo 400. Los avisos de ataques no usan este presupuesto.
- Benchmarks de lógica: ~499 enemigos 1,08 ms/tick; 500 con cuatro armas 2,01 ms;
  750 con jefe/proyectiles 2,77 ms. No son mediciones de FPS/GPU reales.
- Matriz inicial: dos personajes × 5/10/15 minutos, misma semilla, sin movimiento,
  primera carta automática, hasta muerte/120 s. Resultados en `DECISIONES.md`;
  no mide partidas completas ni justifica rebalancear por sí sola.
- Corrección descubierta en navegador: `onended` puede llegar tarde mientras
  avanza el reloj de audio. Además del presupuesto temporal, se limita el número
  de nodos pendientes, evitando acumulación aunque JS se bloquee.

## Para continuar tras esta entrega

1. Completado: el autor ha probado y aprobado el hito 6.
2. Recoger su valoración de mezcla/estilo musical, movilidad de Baguette, balance
   de partidas completas, Pointer Lock y FPS en su Windows. No se han podido
   comprobar la escucha humana ni estas condiciones físicas desde aquí.
3. Corregir lo que encuentre dentro del pulido; no añadir contenido nuevo todavía.
4. U0 autorizado: Windows para Steam/similares, Unity 6.6 (revisión pendiente),
   conservar progreso. Preparación en `unity/Docs/`; proyecto de Editor no creado.

Para regenerar las pruebas de navegador, ver los comandos CMD del README. Los
resultados de `qa-results/` son temporales y se ignoran en git; este resumen y el
script guardan lo necesario para reproducirlos sin depender de una sesión antigua.
