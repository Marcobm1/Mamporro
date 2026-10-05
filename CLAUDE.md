# MAMPORRO — guía para Claude Code

MAMPORRO es un roguelike 3D de supervivencia contra hordas con humor propio y estética PS1. La versión web aprobada usa Three.js + TypeScript estricto + Vite; Unity para Windows es la versión principal tras el cierre aprobado de U0–U6.

Claude Code y Codex CLI trabajan **por turnos** sobre la misma rama. No trabajan a la vez. El repositorio y la documentación son la memoria compartida entre ambos: no dependas del historial privado de una conversación para continuar trabajo previo ni para dejar contexto al siguiente agente.

## Fuente de verdad y orden de lectura

Antes de modificar código o documentación, lee:

1. [`docs/INSTRUCCIONES_PROYECTO.md`](docs/INSTRUCCIONES_PROYECTO.md).
2. [`docs/ESTADO_ACTUAL.md`](docs/ESTADO_ACTUAL.md).
3. [`docs/CONTINUIDAD_AGENTES.md`](docs/CONTINUIDAD_AGENTES.md).
4. [`docs/BLENDER_B0.md`](docs/BLENDER_B0.md) (checkpoint vigente; implementación autorizada y en curso). [`docs/PROGRESO_U6.md`](docs/PROGRESO_U6.md) conserva la migración cerrada y U6 aprobado manualmente el 05/10/2026. Cierre aprobado de U5 en [`docs/PROGRESO_U5.md`](docs/PROGRESO_U5.md) y cierres aprobados en [`docs/PROGRESO_U4.md`](docs/PROGRESO_U4.md) y [`docs/PROGRESO_U3.md`](docs/PROGRESO_U3.md).
5. [`docs/DECISIONES.md`](docs/DECISIONES.md) y [`docs/ESPECIFICACION.md`](docs/ESPECIFICACION.md).
6. [`docs/MIGRACION_UNITY.md`](docs/MIGRACION_UNITY.md).
7. [`README.md`](README.md) y [`unity/README.md`](unity/README.md).
8. Para la referencia y mejoras futuras: [`unity/Docs/U0_REFERENCIA.md`](unity/Docs/U0_REFERENCIA.md) y [`unity/Docs/HOJA_DE_RUTA.md`](unity/Docs/HOJA_DE_RUTA.md).

Las decisiones posteriores concretan o sustituyen la especificación original. Los apartados antiguos que digan «U1 no iniciado», «U2 no autorizado» o «Codex pendiente de instalar» son históricos: manda `docs/ESTADO_ACTUAL.md`.

## Estado vigente — 05/10/2026

- Repositorio: `https://github.com/Marcobm1/Mamporro`.
- Rama por defecto y de trabajo: `claude/zen-pasteur-674ik0`. Push directo permitido; sin PR salvo petición. Nunca force-push.
- Web: hitos 1–6 implementados y aprobados. Base de comparación: `0505b1690656d15188860157612455639820fe1f`.
- U0: referencia de catálogo, RNG, fórmulas, guardados, mundo, i18n y medios preparada.
- Unity: proyecto real en `unity/`, Unity 6.6 `6000.6.3f1`, Windows x64 Mono, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0.
- U1: implementado en `abe0a9b7f0b8c53f478b91c870341999df2ea073`, checkpoint `eb691b595eb247118075368430d34ed0a95735d1`; build y pruebas automáticas correctas y **aprobación manual del autor posterior al checkpoint**.
- U2: aprobado por el autor el 29/09/2026 (`docs/PROGRESO_U2.md`).
- **U3 y U4 aprobadas manualmente el 04/10/2026** (último cierre técnico U4: `3754e31`). **U5 aprobado manualmente por el autor el 04/10/2026** (`docs/PROGRESO_U5.md`); U6 aprobado manualmente el 05/10/2026; migración cerrada, B0 autorizado y en curso.
- Codex CLI ya está instalado/verificado; no repetir la instalación.
- Codex y Claude Code trabajan por turnos. El último checkpoint publicado manda sobre recuerdos de sesiones anteriores.

## U3: alcance ya aprobado (histórico)

Objetivo: mundo y partida completa equivalentes a la web aprobada (`0505b16`), sobre el núcleo y el combate de U2.

Incluye: mundo procedural (heightfield con bancales, sites con props y colisionadores, decoración, cobertura del suelo y fauna decorativa); colisiones del jugador y de la horda sobre el mundo real; física del jugador de la web; director completo (tabla y curva de aparición, oleadas especiales, élites, enjambre, duraciones 5/10/15 y temporizador); baúles, mesas camilla, tótems y armario → jefe → victoria; minimapa, avisos, telegrafiado y pausa; pantallas técnicas de inicio y resultados; depuración equivalente a F3 que marca `run.cheated`.

No incluye U4–U5: menús finales, Calderilla, tienda, misiones y guardado (U4); audio y pulido (U5); ni las mejoras posmigración (mundo mayor, escalada, nueva curva de hordas/oro, arte nuevo, cámara contra estructuras). No rebalancear.

Decisiones, plan y criterios: `docs/PROGRESO_U3.md`.

## Forma de trabajar

1. Antes de editar, comprueba rama, `git status --short --branch`, últimos commits, remoto y cambios locales. Haz `git fetch origin`; si el árbol está limpio y solo falta avanzar, usa `git pull --ff-only`. Conserva cambios del autor, de Unity y del agente anterior; no uses `reset --hard`, `git clean`, checkout destructivo ni reclonado para «arreglar» un árbol sucio.
2. Lee «Cómo retomar» de `docs/BLENDER_B0.md` y el cierre aprobado de `docs/PROGRESO_U6.md`. No asumas que Codex terminó todo lo planeado ni repitas trabajo sin comprobar commits y estado.
3. U5 está aprobado manualmente (`docs/PROGRESO_U5.md`). U6 aprobado y migración cerrada. B0 autorizado y en curso: seguir docs/BLENDER_B0.md por piezas verificables.
4. Mantén la base web y `unity/Docs/Reference/` como referencia. Nunca uses `--write` o cambies valores esperados para hacer pasar un port incorrecto.
5. Implementa por piezas verificables. Código en inglés; comentarios, documentación y commits en español. No nombres modelos de IA en código ni commits.
6. Verifica lo que realmente ejecutes. Distingue pruebas unitarias, Edit Mode, Play Mode, build y benchmark; un benchmark de lógica no equivale a FPS reales.
7. Actualiza `docs/BLENDER_B0.md` cuando cambie el punto de continuación y siempre antes de ceder el turno a Codex. Actualiza `README.md`, `docs/DECISIONES.md`, `docs/ESTADO_ACTUAL.md` y demás documentación cuando cambie el estado global o haya decisiones permanentes.
8. Commits pequeños que compilen y pasen sus pruebas por separado; push directo a la rama. Sin PR.
9. Si la sesión/tokens se acercan al límite, no empieces una pieza nueva: deja lo terminado verificado, commit/push cuando sea seguro, documenta cualquier cambio local no publicado y escribe un `Siguiente paso exacto` para Codex o para la siguiente sesión.
10. U1–U5 ya están aprobados. U6 aprobado manualmente el 05/10/2026. B0 autorizado por pasos; no adoptar masivamente el resultado ni iniciar otro bloque antes de aprobación manual.

## Relevo a Codex

Antes de terminar una sesión con trabajo relevante, el checkpoint vigente `docs/BLENDER_B0.md` debe indicar:

- que la sesión fue realizada con Claude Code;
- objetivo concreto abordado;
- archivos/sistemas modificados;
- pruebas realmente ejecutadas y sus resultados;
- commits y push realizados;
- errores/limitaciones y pruebas pendientes;
- cambios locales que sigan sin commit;
- decisión del autor pendiente, si existe;
- siguiente paso exacto y seguro.

No crear un commit roto solo para transferir contexto. Si un trabajo incompleto no puede publicarse de forma coherente, conservarlo localmente y documentarlo de forma explícita para que Codex no lo sobrescriba.

## Convenciones que se conservan del juego web

- IDs persistentes y reglas de la referencia no se cambian sin decisión explícita.
- RNG propio y determinista; no sustituirlo por `UnityEngine.Random` para reglas que deban coincidir.
- Datos separados de lógica/presentación. No guardar progreso del jugador en ScriptableObjects.
- Evitar un `Update` o `Rigidbody` por enemigo por defecto; U1 validó una horda centralizada con arrays/pool/rejilla e instanciación.
- No introducir DOTS/ECS solo porque Burst/Collections existan como dependencias transitivas.
- La base web `0505b16` y las referencias congeladas se conservan. U4 permite modificar `src/` exclusivamente para exportación JSON validada del progreso, con typecheck, tests, build y navegador cuando corresponda; sin cambios de reglas ni balance.
- Todo contenido debe ser original. En Unity están permitidos iconos e ilustraciones originales en archivos; no implica autorización automática para packs de terceros o compras.
- Guardado compatible será U4: moneda meta, desbloqueos, misiones, usos extra, selección y opciones equivalentes; no se trasladan partidas activas.

## Entorno confirmado

Raíz usada en Windows: `C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git`.

- Git `2.54.0.windows.1`.
- Node `24.21.0`.
- npm `11.19.0`.
- Codex CLI `0.158.0`.
- Editor: `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`.
- Soporte Windows Mono presente. Windows IL2CPP no estaba instalado en U1; no hace falta cambiarlo para U2 salvo una necesidad justificada.
- Hardware de referencia U1: Ryzen 7 7700X, RTX 4070 Ti SUPER, ~32 GB RAM; 1080p principal y 1440p secundario. No representa requisitos mínimos comerciales.

## Comandos de referencia — CMD

Web, solo cuando corresponda:

```cmd
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
node scripts\unity-reference.mjs
node --test scripts\unity-reference-bytes.test.mjs
```

U1 ya dispone de lanzadores reproducibles:

```cmd
scripts\u1.cmd edit
scripts\u1.cmd play
scripts\u1.cmd build
scripts\u1.cmd benchmark
node scripts\u1-report.mjs
```

Para U2 y U3, crea o amplía automatización propia solo cuando sea útil y documenta los comandos reales; no inventes que una prueba se ha ejecutado. PowerShell puede interceptar `npm.ps1` en este equipo, por eso desde CMD se prefiere `npm.cmd`.

## Rendimiento y referencia U1

U1 validó una escena técnica con 300/500/750/1000 entidades, salida 1920×1080 y 2560×1440, interna 360 (240/480 opcionales), D3D11, build Windows Mono. El objetivo de 60 FPS con 300 tuvo margen en ese equipo; también las cargas superiores, con picos aislados. No extrapolar esos datos al combate U2 ni a equipos modestos. GPU fue no fiable en varias condiciones y GC no estuvo disponible en la build normal; conservar esas limitaciones.

## Mejoras confirmadas para después de validar la migración

Mundo/estructuras mayores; mesetas y rampas más marcadas; escalada libre por paredes sin imponer trepada breve ni resistencia sin consultarlo; inicio con menos enemigos pero más resistentes y crecimiento progresivo; más oro **de partida** de enemigos según tiempo/dificultad; acabado retro más profesional; iconos/ilustraciones originales; más enemigos, armas, tomos y personajes. No introducir estas mejoras dentro de U2 salvo un prototipo aislado aprobado expresamente.

Cámara contra estructuras, mando/remapeo, métricas locales adicionales y servicios Steam siguen siendo propuestas/etapas futuras, no funciones ya terminadas.
