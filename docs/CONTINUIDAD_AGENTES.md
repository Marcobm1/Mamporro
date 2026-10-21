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
5. `docs/PROGRESO_P0.md`, P0-A/B/C aprobados; P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados; cierre técnico aceptado en `docs/BLENDER_B0.md`; `docs/PROGRESO_U6.md` conserva el cierre aprobado de la migración (05/10/2026).
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

**21/10/2026, posterior al cierre de landmarks `9306e38`: únicamente planificación híbrida autorizada y entregada.** [PLAN_HIBRIDO_P0D3](PLAN_HIBRIDO_P0D3.md) contiene alternativas, pruebas y decisiones pendientes. No interpretar la recomendación A, sus bandas/cuotas ni las fases H0–H4 como aprobadas. No programar, integrar, iniciar D4/D5 ni ampliar. Siguiente paso: revisión y autorización expresa del autor; publicación/checks documentales en PROGRESO_P0.

**21/10/2026:** revalidado y preparado para publicación el trabajo local de landmarks recibido desde `27414c5`, sin cambios funcionales. Nuevas **12/12**, contadores idénticos; puerta negativa confirmada, no Puerta 2. Checkpoint, evidencias y publicación en [PROGRESO_P0](PROGRESO_P0.md). Conservar los siete ajustes locales y detenerse para revisión; no iniciar D4/D5 ni integrar.

**20/10/2026, landmarks dirigidos, posterior al preprocesado:** dos puertas autorizadas, primera evaluada y negativa por trabajo agregado. Solo instrumento offline de tests, sin servicio/caché nueva. **No iniciar Puerta 2: revisión del autor por la condición de parada**, no falta de autorización general. [Informe](EXPERIMENTO_LANDMARKS_P0D3.md); pruebas, huellas y commits en PROGRESO_P0. No confundir reducción frente a Dijkstra individual con mejora sobre búsqueda compartida. D3 bloqueado, 3000 intactos; D4/D5 y cualquier integración excluidos.

**20/10/2026, preprocesado exacto:** autorización nueva ejecutada hasta la puerta inicial. Diagnóstico offline con el kernel existente; preparación parcial >4 s en nueve muestras, matriz mundial 13,80 GiB. **No implementar prototipo de servicio: parada solicitada ante inviabilidad inicial.** [Informe](EXPERIMENTO_PREPROCESADO_P0D3.md), cierre operativo en PROGRESO_P0. No confundir los nueve tests de medición correctos con aceptación de rendimiento. D3 bloqueado; esperar revisión antes de otra arquitectura o integración. D4/D5 no iniciados, siete ajustes y nueve protecciones conservados.

**19/10/2026, demanda exacta:** experimento autorizado ejecutado y publicado (`3aaac6f`, `2940d9b`), exactitud verificada pero servicio móvil/disperso insuficiente. [Informe](EXPERIMENTO_DEMANDA_P0D3.md). **Parada para revisión, D3 bloqueado; D4/D5 no iniciados.** No integrar ni iniciar otra arquitectura hasta revisión del autor. 3000 y objetivos intactos, siete ajustes Unity preservados fuera de commits. Pruebas nuevas, evidencias, protecciones y cierre documental en el primer punto de PROGRESO_P0. La autorización de este experimento ya se ejecutó; no repetirla ni interpretarla como aprobación de la siguiente alternativa.

**18/10/2026, posterior al incremental:** el autor autorizó agrupación exacta y propagación regional por lotes; experimento ejecutado, exactitud verificada y **resultado negativo de rendimiento**. [Informe](EXPERIMENTO_LOTES_P0D3.md). No repetir la consulta de autorización del experimento ni integrarlo por su exactitud: 64 m aún requiere 1101086 unidades para el cambio vecino. D3 bloqueado y D4 sin iniciar; conservar 3000. Cierre real de regresiones, commits, huellas y siguiente paso en el primer checkpoint de [PROGRESO_P0](PROGRESO_P0.md). No iniciar una nueva arquitectura sin revisión del autor.

**18/10/2026:** comparador incremental recuperado y evaluado; exactitud correcta pero reacción insuficiente. Edit nuevo 894/894; el diagnóstico monolítico también reproduce inanición M/L. No adoptar comparador ni avanzar D4: consultar alternativas de [REGIONES_P0D](REGIONES_P0D.md). Estado de Play, commits, huellas y siguiente paso exacto en el primer checkpoint de [PROGRESO_P0](PROGRESO_P0.md). La autorización anterior de evaluar el comparador se ha ejecutado; no equivale a aprobar otra arquitectura.

**Continuación tras consulta, 17/10/2026:** el autor responde «sigue»; evaluar el comparador incremental exacto, conservando el prototipo regional y sin adopción automática. Sustituye la espera de respuesta del párrafo siguiente. Checkpoint y verificaciones en PROGRESO_P0; D3 abierto y D4 sin iniciar.

**Parada D3, 17/10/2026:** la jerarquía perezosa exacta por salida no publica en 180 s simulados en ninguno de los nueve casos HORDA750 × 16/32/64 × S/M/L, aunque cada clase recibe su cuota. Transferencias inversas y prototipo conservados; [informe](REGIONES_P0D.md). Consultada alternativa incremental exacta; no implementarla sin respuesta. D3/D4 ya autorizados en general, pero hay un incumplimiento importante concreto. Ver primer checkpoint de [PROGRESO_P0](PROGRESO_P0.md) para pruebas, publicación y siguiente paso. D3 no cerrado y D4 no iniciado.

**Relevo D3, 16/10/2026:** primera pieza de transferencias/caché/planificador publicada en `9575246`; D3 sigue abierto y D4 no iniciado. Consultar el primer punto de [PROGRESO_P0](PROGRESO_P0.md) para pruebas nuevas, resultados y siguiente paso exacto. Las cuotas sintéticas no acreditan todavía persecución útil. Mantener autorización D3/D4, techo 3000 y parada antes de D5.

A fecha 12/10/2026:

- U1 está cerrado y aprobado.
- U2 está aprobado y cerrado.
- U3 aprobada manualmente el 04/10/2026; último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`.
- U4 aprobada manualmente el 04/10/2026 (último cierre técnico `3754e31`). U5 aprobado manualmente el 04/10/2026; U6 aprobado manualmente el 05/10/2026; migración cerrada. B0.1–B0.6 cerrados y aceptados por el autor como pipeline validado técnicamente y validado para proponer adopción. No hubo revisión manual de la build ni aprobación de arte final. P0-A/B/C aprobados; P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados.
- Codex CLI y Claude Code trabajan por turnos. Leer `docs/BLENDER_B0.md`, `docs/PROGRESO_U6.md` y los cierres de U3–U5, auditar el árbol y continuar desde el checkpoint; conservar los siete ajustes locales Unity excluidos.

Punto de continuación vigente: [PROGRESO_P0](PROGRESO_P0.md) — circuito QA aislado de movilidad/verticalidad. **P0-A cerrado:** A1–A5 validados y A6 aprobado manualmente por el autor el 11/10/2026 (código `373620d`, cierre `dc8ab26`). **P0-B cerrado:** aprobado manualmente por el autor el 12/10/2026 (navegación vertical y escalada de todos los enemigos, circuito QA B con F10; código `a9cac92`, cierre técnico `5688e90`). **P0-C aprobado manualmente el 12/10/2026** (circuito QA C procedural con F10/F11, plan [PLAN_P0C](PLAN_P0C.md); código/builds `9a24b6f`, cierre técnico `369fdff`): limitaciones aceptadas provisionalmente y 3000 nodos/tick conservados. **P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados.** B0 cerrado y publicado en `0b0b68c31dda803cf9cf566b3c96f369ed893ce3`.

Plan de continuación: [P0-D — escala y navegación regional](PLAN_P0D.md), **ocho decisiones aprobadas; implementación autorizada únicamente D0–D4**. Cierre formal P0-C publicado en `b755987`; mantener 3000 nodos/tick.
