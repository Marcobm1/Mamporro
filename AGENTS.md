# Entrada para agentes — MAMPORRO

Este archivo es la puerta de entrada para Codex CLI y otros agentes que respeten `AGENTS.md`.
No uses el historial de una conversación como fuente principal: el estado vigente está versionado en el repositorio.

## Lectura obligatoria antes de tocar archivos

1. [`docs/INSTRUCCIONES_PROYECTO.md`](docs/INSTRUCCIONES_PROYECTO.md) — reglas compartidas de trabajo.
2. [`docs/ESTADO_ACTUAL.md`](docs/ESTADO_ACTUAL.md) — punto real de continuación.
3. [`docs/PROGRESO_U2.md`](docs/PROGRESO_U2.md) — bloque actualmente autorizado.
4. [`docs/DECISIONES.md`](docs/DECISIONES.md) — decisiones históricas; las posteriores prevalecen.
5. [`docs/MIGRACION_UNITY.md`](docs/MIGRACION_UNITY.md) — plan U0–U6.
6. [`README.md`](README.md) y [`unity/README.md`](unity/README.md) — uso de la web y del proyecto Unity.
7. Para referencia: [`docs/ESPECIFICACION.md`](docs/ESPECIFICACION.md), [`unity/Docs/U0_REFERENCIA.md`](unity/Docs/U0_REFERENCIA.md) y [`unity/Docs/HOJA_DE_RUTA.md`](unity/Docs/HOJA_DE_RUTA.md).

## Estado que manda

- Rama de trabajo y por defecto: `claude/zen-pasteur-674ik0`.
- Base web aprobada e inmovilizada para comparar el port: `0505b1690656d15188860157612455639820fe1f`.
- U0 preparado y conservado como referencia.
- **U1 implementado, probado por el autor y aprobado el 29/09/2026.** Implementación publicada en `abe0a9b7f0b8c53f478b91c870341999df2ea073`; checkpoint publicado en `eb691b595eb247118075368430d34ed0a95735d1`.
- **U2 está autorizado. U3 no.** Trabaja exclusivamente el alcance descrito en `docs/PROGRESO_U2.md`.
- Unity confirmado: 6.6 / `6000.6.3f1`, Windows x64 Mono, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0 y Test Framework 1.8.0.
- Codex CLI ya está instalado y verificado en el equipo del autor. No vuelvas a tratar su instalación como pendiente.

## Antes de modificar nada

Ejecuta o comprueba primero `git status --short --branch` y conserva cualquier cambio local. El cierre de U1 registró cambios posteriores generados por Unity fuera de los commits publicados; **no uses `reset --hard`, `clean`, force-push ni reclonado para ocultarlos**. Si hay conflicto real con el trabajo de U2, informa y resuélvelo sin borrar trabajo del autor.

Trabaja un bloque cada vez. No abras Pull Request salvo petición expresa. Los commits van en español, pequeños y verificables. No empieces U3, las mejoras posmigración ni servicios Steam dentro de U2.
