# Continuidad entre Codex y Claude Code

Este documento define el protocolo de relevo entre **Codex CLI** y **Claude Code** para MAMPORRO.

El autor trabajará con ambos, pero **no a la vez**. Cuando una herramienta agote su sesión/tokens o convenga cambiar de agente, la otra debe poder continuar únicamente a partir del repositorio y de la documentación versionada, sin depender del historial privado de la sesión anterior.

## Regla principal

Solo hay **un agente activo escribiendo en la rama en cada momento**.

Rama compartida: `claude/zen-pasteur-674ik0`.

Codex y Claude Code trabajan sobre el mismo historial. No crear ramas paralelas, Pull Requests o copias alternativas para el relevo salvo petición expresa del autor.

El repositorio es la memoria compartida. Una decisión, prueba, limitación o siguiente paso importante que solo exista en un chat se considera documentación incompleta.

## Orden de lectura al empezar cualquier sesión

1. `AGENTS.md` si se usa Codex, o `CLAUDE.md` si se usa Claude Code.
2. `docs/INSTRUCCIONES_PROYECTO.md`.
3. `docs/ESTADO_ACTUAL.md`.
4. Este archivo, `docs/CONTINUIDAD_AGENTES.md`.
5. El checkpoint del bloque vigente, actualmente `docs/PROGRESO_U5.md` (U5 aprobado manualmente el 04/10/2026; siguiente tarea: planificar U6).
6. `docs/DECISIONES.md` y el resto de documentación indicada por el checkpoint.

Después comprobar desde CMD:

```cmd
git status --short --branch
git log -8 --oneline
git remote -v
git fetch origin
git status --short --branch
```

Si el árbol está limpio y el remoto tiene cambios nuevos, actualizar solo mediante avance seguro (`git pull --ff-only`) antes de editar.

Si hay cambios locales, **no hacer pull destructivo, reset, clean, checkout forzado ni sobrescribirlos**. Primero identificar si pertenecen al autor, a Unity o al agente anterior y leer el último checkpoint.

## Qué debe dejar documentado cada agente

Durante el trabajo, y obligatoriamente antes de ceder el turno a la otra herramienta, actualizar el checkpoint del bloque con:

- fecha y agente que realizó la sesión (`Codex` o `Claude Code`);
- objetivo concreto trabajado;
- archivos/sistemas modificados;
- decisiones tomadas y por qué, solo cuando sean decisiones reales nuevas;
- pruebas/comandos **realmente ejecutados** y su resultado;
- pruebas pendientes o que no pudieron ejecutarse;
- errores o limitaciones conocidas;
- commits creados y si fueron subidos;
- estado del árbol local conocido al terminar;
- cambios locales deliberadamente no incluidos;
- punto exacto desde el que continuar;
- siguiente acción recomendada dentro del bloque autorizado;
- decisiones que requieren respuesta del autor antes de seguir.

No registrar como hecho algo que solo estaba planeado.

## Checkpoints durante una sesión larga

No esperar al final de todo el hito para documentar el progreso.

Cuando se complete una pieza verificable de un bloque largo:

1. ejecutar las pruebas pertinentes;
2. hacer un commit pequeño si el estado es coherente y verificable;
3. actualizar el checkpoint con el avance útil si cambia el punto de continuación;
4. hacer push cuando corresponda para que el otro agente pueda recuperar el estado.

No crear commits rotos únicamente para transferir contexto. Si un cambio todavía no puede formar un commit válido, dejarlo local y describirlo con precisión en el checkpoint antes del relevo. El siguiente agente debe preservar ese árbol local y revisarlo antes de tocar los mismos archivos.

## Relevo por fin de sesión o tokens

Si se aproxima el límite de una sesión, priorizar el estado recuperable sobre empezar trabajo nuevo.

Antes de terminar:

1. detener cambios de alcance nuevo;
2. comprobar `git status --short --branch`;
3. ejecutar las verificaciones razonables del último cambio terminado;
4. commit y push de las piezas completas que cumplan sus pruebas;
5. actualizar el checkpoint del bloque;
6. indicar claramente cualquier modificación local no publicada;
7. dejar un `Siguiente paso exacto` que pueda ejecutar el otro agente.

El agente que entra no debe asumir que el anterior terminó todo lo previsto. Debe comprobar commits, árbol, checkpoint y pruebas registradas.

## Responsabilidad documental por tipo de información

- `docs/ESTADO_ACTUAL.md`: estado global vigente, bloque activo, último punto estable y restricciones importantes.
- `docs/PROGRESO_U2.md` (o el checkpoint vigente futuro): diario técnico y operativo del bloque actual, incluidos relevos entre agentes.
- `docs/DECISIONES.md`: decisiones de diseño/arquitectura/alcance que deban sobrevivir al bloque actual.
- `docs/INSTRUCCIONES_PROYECTO.md`: reglas estables de trabajo compartidas por todas las herramientas.
- `AGENTS.md`: entrada específica de Codex.
- `CLAUDE.md`: entrada específica de Claude Code.
- `README.md` y documentación Unity: comportamiento y uso que afecten al proyecto, no notas de sesión efímeras.

No duplicar grandes bloques de información en todos los archivos. Los documentos de entrada deben apuntar a las fuentes canónicas.

## Cambio de bloque o hito

Al cerrar U2 o cualquier hito futuro:

- dejar el checkpoint completo;
- actualizar `docs/ESTADO_ACTUAL.md`;
- añadir las decisiones nuevas a `docs/DECISIONES.md`;
- actualizar README/documentación técnica afectada;
- registrar pruebas y build reales;
- hacer commits y push;
- entregar instrucciones de prueba manual al autor;
- detenerse hasta su aprobación.

Cuando el autor apruebe el siguiente bloque, crear/actualizar su checkpoint y cambiar las referencias de `AGENTS.md`, `CLAUDE.md` y `docs/ESTADO_ACTUAL.md` para que ambos agentes entren siempre por el bloque correcto.

## Prohibiciones de continuidad

Ningún agente debe:

- iniciar trabajo paralelo al otro sobre la misma rama;
- asumir cambios no visibles en Git/documentación;
- rehacer trabajo del agente anterior sin revisar primero su estado;
- borrar cambios locales porque no los reconozca;
- reinterpretar una prueba histórica como si se hubiera ejecutado en su sesión;
- regenerar referencias esperadas para ocultar divergencias;
- iniciar un bloque futuro no autorizado;
- depender de que el autor copie manualmente el historial de una herramienta a la otra.

## Estado actual del relevo

A fecha 04/10/2026:

- U1 está cerrado y aprobado.
- U2 está aprobado y cerrado.
- U3 aprobada manualmente el 04/10/2026; último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`.
- U4 aprobada manualmente el 04/10/2026 (último cierre técnico `3754e31`). U5 aprobado manualmente el 04/10/2026; U6 solo en planificación, esperar aprobación expresa de su plan antes de implementar.
- Codex CLI y Claude Code trabajan por turnos. Leer `docs/PROGRESO_U5.md` y los cierres de U3 y U4, auditar el árbol y continuar desde el checkpoint; conservar los siete ajustes locales Unity excluidos.
