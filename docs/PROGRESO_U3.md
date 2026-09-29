# Checkpoint U3 — mundo y partida completa equivalentes

Estado: **AUTORIZADO por el autor el 29/09/2026; EN CURSO.** U4 no está autorizado.

Este archivo es el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U3 siga activo. Debe actualizarse después de cada paso completado y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md); reglas comunes: [`INSTRUCCIONES_PROYECTO.md`](INSTRUCCIONES_PROYECTO.md).

## Cómo retomar

- **Último commit publicado:** ver `git log -1 origin/claude/zen-pasteur-674ik0`. U2 cerrado y aprobado en `176423d`.
- **Paso actual:** 2 del plan (referencia `u3-world.json`).
- **Terminado y verificado:** paso 1 (este checkpoint y las entradas de documentación apuntando a U3). No hay código U3 todavía.
- **A medias:** nada.
- **Sin commit a propósito:** `unity/ProjectSettings/ProjectSettings.asset` (identificador de nube), `ProjectAuditorSettings.asset`, `PackageManagerSettings.asset` y `URPProjectSettings.asset`: no publicar nunca. Unity reescribe con espacios algunos ajustes al abrir el proyecto (`RetroPipeline.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `GraphicsSettings.asset`, `ProjectAuditorSettings.asset`): si solo cambian espacios o finales de línea, restaurarlos antes de hacer commit.
- **Siguiente paso exacto:** crear `scripts/unity-reference-u3.mjs` con el mismo modelo que `scripts/unity-reference-u2.mjs` (guarda contra `src/` en `0505b16`, `--export-once` solo crea el archivo si no existe, sin argumentos comprueba) y exportar `unity/Docs/Reference/u3-world.json` con el contenido de la decisión 1.

Comprobar el estado desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch
git log --oneline -8
scripts\u2.cmd edit
scripts\u2.cmd play
```

## Registro de sesiones y relevos de U3

Añadir las entradas nuevas **de más antigua a más reciente** con la plantilla de `docs/PROGRESO_U2.md` («Plantilla para cada sesión posterior»). No borrar entradas de otro agente salvo corrección factual explícita.

### 29/09/2026 — autorización de U3 y paso 1 — Claude Code

- Punto de partida/commit: `176423d` (aprobación de U2 ya publicada; no hizo falta repetirla).
- Trabajo realizado: revisión del código web de U3; dos dudas resueltas por el autor (teclas de depuración y catálogo); creación de este checkpoint; `CLAUDE.md`, `AGENTS.md`, `CONTINUIDAD_AGENTES.md`, `INSTRUCCIONES_PROYECTO.md` y `ESTADO_ACTUAL.md` apuntan a U3; decisiones en `DECISIONES.md`.
- Pruebas realmente ejecutadas y resultado: ninguna (cambio solo documental).
- Push realizado: ver el commit «Abre U3 y registra sus decisiones».
- Cambios locales no incluidos: los ajustes locales de Unity citados en «Cómo retomar».
- Decisiones pendientes del autor: ninguna.
- Siguiente paso exacto: paso 2 del plan.

## Alcance autorizado

Mundo y partida completa equivalentes a la web aprobada `0505b1690656d15188860157612455639820fe1f`:

- Mundo procedural: heightfield de 320 m con bancales; sites (casas, templos, granjas, pozos) con props y colisionadores; decoración con colisión; cobertura del suelo y fauna decorativa (desactivables si pesan).
- Colisiones del jugador y de la horda sobre el mundo real (pendiente máxima 48°, los enemigos suben hasta 1,6 m, reciclado lejano).
- Director: `SPAWN_TABLE`/`SPAWN_CURVE`, 6 oleadas especiales, élite cada 2 min, enjambre final (hasta 750 vivos), duraciones 5/10/15 con su escalado de XP/oro y temporizador.
- Interactuables: 14 baúles con precio creciente, 3 mesas camilla, 2 tótems, armario escondido → Pelusa Madre → victoria a los 1,6 s. La carta de relleno de oro pasa a usar `chestCost(baúlesAbiertos)`, como la web.
- Minimapa con descubrimiento; avisos (oleadas, élite, enjambre, armario); telegrafiado de los ataques del jefe; pausa con semilla y estadísticas.
- Pantalla técnica de inicio (uGUI): ambos personajes, duración 5/10/15 y semilla opcional. Pantalla técnica de resultados: tiempo, bajas, daño por arma y nivel; sin Calderilla.
- Depuración equivalente a la F3 web (invencible, subir nivel, saltar un minuto, aparecer enemigos, matar a todos, invocar al jefe, oro, revelar el mapa) que marca `run.cheated`.
- Cámara: sigue chocando solo con el terreno, como la web.

**Fuera de alcance:** menús finales, Calderilla, tienda, misiones y guardado (U4); audio y pulido (U5); mundo ampliado, escalada, nueva curva de hordas/oro, arte nuevo y cámara contra estructuras. Sin rebalanceo. No se tocan `src/` ni las referencias existentes (`baseline.json`, `u2-combat.json`).

## Decisiones del autor para U3 (29/09/2026)

Detalle en `docs/DECISIONES.md`.

1. **Referencia complementaria** `scripts/unity-reference-u3.mjs` → `unity/Docs/Reference/u3-world.json`, con el mismo modelo que U2: solo desde la web aprobada, guarda contra `src/` en `0505b16`; no toca `baseline.json` ni `u2-combat.json`; los esperados nunca se recalculan con C#. Cubre: rejilla densa de alturas, sites, props y colisionadores, decoración, interactuables, posiciones de aparición, director por duración, oleadas, élites, enjambre, precios de baúl, carga y descarga de mesa, tótem, descubrimiento y una partida integrada con recorrido fijo y cronología de eventos. Una vez usada por una prueba C#, no se regenera.
2. **Tolerancias:** núcleo del mundo portado en double; igualdad exacta en lo discreto (número, tipo y orden de sites, props, interactuables, oleadas y eventos); 1e-6 m en alturas y posiciones; cronología de la partida integrada exacta en los primeros 60–120 s y, después, comparación por totales con margen documentado. Incluye un port fiel del algoritmo de la librería `simplex-noise` (4.0.3, licencia MIT; se conserva su aviso) alimentado por el RNG propio.
3. **Movimiento:** portar la física del jugador de la web (`src/entities/playerPhysics.ts`: pendientes, escalones, salto, deslizamiento cuesta abajo hasta 30 m/s) y probarla con sus casos de prueba, conservando la entrada y la cámara de U1. Las pruebas de U1 y U2 deben seguir pasando.
4. **Pantallas técnicas** de inicio y resultados como se describen en el alcance.
5. **Teclas (respuesta del autor, 29/09/2026):** F3 abre la depuración y 1–8 ejecutan sus acciones, como la web. En U3 el ajuste de vértices pasa de F3 a F9; U1 y U2 no cambian. E interactúa, como la web.
6. **Catálogo (respuesta del autor, 29/09/2026):** todo desbloqueado (6 armas y 12 objetos), como la web sin filtros de meta. La referencia y las pruebas usan ese caso; los filtros de desbloqueo se conectarán en U4 con el guardado.

## Plan

Commits pequeños que compilen y pasen sus pruebas; actualizar este checkpoint tras cada paso; `git fetch` antes de cada push (si el remoto cambió, parar y avisar al autor).

1. Este checkpoint y las entradas de documentación. **Hecho.**
2. Referencia `u3-world.json`.
3. Núcleo del mundo en C# puro (`Assets/Mamporro/Core`, sin UnityEngine): port de simplex-noise, heightfield, sites, props y colisionadores, decoración, cobertura del suelo, interactuables y colisión del mundo; pruebas contra `u3-world.json` y `baseline.worlds`.
4. Render del mundo con el material retro de U1 en una escena nueva `Assets/Mamporro/U3/U3_Partida.unity`; U1 y U2 conservan sus escenas y builds.
5. Física del jugador y colisiones de la horda sobre el mundo real.
6. Director completo.
7. Interactuables, armario, jefe y victoria.
8. HUD, minimapa, avisos, telegrafiado y pausa.
9. Pantallas de inicio y resultados; depuración con `run.cheated`.
10. Integración: partidas deterministas contra la cronología web; Play Mode con partidas aceleradas (2 personajes × 3 duraciones) hasta resultados; victoria, derrota, enjambre y reinicio sin restos.
11. Build Windows x64 Mono ejecutada y ensayo de partida real: minutos 2, 5 y 9 y enjambre con 750 vivos, a 1080p y 1440p, con las mismas condiciones y `validRender` que U1/U2. Objetivo: 60 FPS con 300 enemigos en el mapa real; si el enjambre no llega, perfilar y documentar antes de cerrar.
12. Cierre: documentación, push y parada para la prueba manual del autor con instrucciones.

## Pruebas ejecutadas

| Fecha y hora | Comando (CMD, raíz del repo) | Resultado | Registro |
| --- | --- | --- | --- |
| — | — | Ninguna todavía | — |

## Checkpoint al terminar U3

Registrar aquí: commits publicados; sistemas portados; decisiones nuevas; pruebas exactas y resultados; build y condiciones de rendimiento; limitaciones; cambios locales no publicados; instrucciones de prueba manual; aprobación del autor o pendientes; siguiente paso propuesto (U4), sin empezarlo.
