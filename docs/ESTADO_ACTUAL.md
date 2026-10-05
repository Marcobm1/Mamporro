# MAMPORRO — estado actual y punto de continuación

Corte vigente: **05/10/2026**, U3 **aprobada manualmente por el autor como base funcional**. Último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`. U4 **aprobada manualmente por el autor el 04/10/2026** (último cierre técnico `3754e31`); [cierre](PROGRESO_U4.md#checkpoint-al-terminar-u4). U5 **aprobado manualmente por el autor el 04/10/2026**, tras el cierre técnico `5ef42b6`; cierre, guía de escucha y prueba manual en [PROGRESO_U5](PROGRESO_U5.md#checkpoint-al-terminar-u5); equivalencia en [EQUIVALENCIA_U5](EQUIVALENCIA_U5.md). U6 **implementado, verificado y aprobado manualmente por el autor el 05/10/2026; migración U0–U6 formalmente cerrada**; entrega `unity\Builds\Windows\MAMPORRO.exe`, paquete y guía en [PROGRESO_U6](PROGRESO_U6.md#checkpoint-al-terminar-u6). Después: B0 Blender solo en planificación, implementación no autorizada ([BLENDER_B0](BLENDER_B0.md)), no iniciado.

Las limitaciones visuales son mejoras futuras; los picos aislados sin causa aislada siguen documentados y no bloquean el avance. Las seis propuestas técnicas de U3 no han recibido confirmación individual. Las actualizaciones siguientes son históricas.

> **Actualización 03/10/2026:** U3 **implementado y verificado** (mundo y partida completa; las cuatro partidas de referencia coinciden con la web; ensayo de rendimiento en la build). **Pendiente de la prueba manual y la aprobación del autor. U4 no autorizado.** Instrucciones en «Checkpoint al terminar U3» de [PROGRESO_U3](PROGRESO_U3.md).
>
> **Actualización 29/09/2026 (noche):** U2 **aprobado por el autor**. **U3 autorizado y en curso; U4 no autorizado.** El punto exacto de continuación está en la sección «Cómo retomar» de [PROGRESO_U3](PROGRESO_U3.md); el cierre de U2, en [PROGRESO_U2](PROGRESO_U2.md). Estas notas mandan sobre las frases de este documento que digan que U2 no está implementado o que U3 no está autorizado. Relevo entre herramientas: [CONTINUIDAD_AGENTES](CONTINUIDAD_AGENTES.md) y checkpoints de bloque en [INSTRUCCIONES_PROYECTO](INSTRUCCIONES_PROYECTO.md).

## Resumen ejecutivo

- Web: hitos 1–6 implementados y aprobados.
- Base web de referencia: `0505b1690656d15188860157612455639820fe1f`.
- U0: referencia reproducible preparada y conservada.
- U1: **implementado, verificado y aprobado por el autor**.
- U2: **implementado, probado y aprobado por el autor el 29/09/2026**; cierre en `docs/PROGRESO_U2.md`.
- U3: **implementado y verificado; aprobado manualmente por el autor el 04/10/2026**; cierre e instrucciones en `docs/PROGRESO_U3.md`.
- U4: implementada, verificada y **aprobada manualmente el 04/10/2026** (pasos 1–9; último cierre técnico `3754e31`).
- U5: **implementado y verificado el 04/10/2026** (pasos 1–9: audio, sucesos, partículas, números, cámara/sacudida/destellos, rendimiento, sesiones/capturas/equivalencia, cierre); aprobado manualmente por el autor.
- U6: implementado y verificado el 05/10/2026 (Unity principal, transición del guardado, identidad `MAMPORRO`, build `unity\Builds\Windows\MAMPORRO.exe`, paquete con manifiesto); aprobado manualmente por el autor. Unity es la versión principal; web histórica ejecutable, congelada y recuperable con exportador. B0 solo en planificación; mejoras posteriores fuera de la migración cerrada.
- Rama de trabajo y por defecto: `claude/zen-pasteur-674ik0`.
- Implementación U1: `abe0a9b7f0b8c53f478b91c870341999df2ea073`.
- Checkpoint U1: `eb691b595eb247118075368430d34ed0a95735d1`.
- Contexto compartido para U2: `2b67ea53a54c2e0e07659562e334d423006d89f7` y documentación posterior de continuidad.
- **Codex CLI y Claude Code son herramientas de trabajo válidas por turnos sobre la misma rama. No deben trabajar simultáneamente.**

## Continuidad entre agentes

El proyecto ya no debe depender del historial de una sola conversación o herramienta.

Codex CLI y Claude Code se alternarán cuando termine la sesión/tokens de uno. El repositorio es la memoria compartida y cada agente debe dejar un relevo suficiente para que el otro continúe sin reconstruir contexto manualmente.

Norma canónica: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md).

El checkpoint de continuación es [BLENDER_B0](BLENDER_B0.md), **solo planificación; esperar autorización expresa antes de implementar**. Cierre aprobado U6 en [`PROGRESO_U6.md`](PROGRESO_U6.md), publicado en `39602c8aaba3ca163cde39c654b005f23f9e5f2f`. U5 aprobado y publicado en `4fcbcdf7707f4b22594164ae27552e62629228ac`; cierre en [`PROGRESO_U5.md`](PROGRESO_U5.md). [`PROGRESO_U4.md`](PROGRESO_U4.md) y [`PROGRESO_U3.md`](PROGRESO_U3.md) conservan los cierres aprobados, pruebas e instrucciones manuales. Actualizar el checkpoint vigente antes de cada relevo.

Cada relevo debe registrar trabajo realmente realizado, pruebas ejecutadas, commits/push, limitaciones, cambios locales sin publicar y siguiente paso exacto. No registrar planes como si fueran resultados.

## Qué cambió desde los documentos de traspaso inicial

Los Markdown del traspaso del 28/29 de septiembre decían que Codex CLI estaba pendiente y que U1 no había empezado. Eso ya no es cierto.

El entorno local se comprobó, Codex CLI se instaló/verificó y U1 se implementó realmente en Unity. Unity importó el proyecto, compiló C#, ejecutó Edit/Play Mode, generó una build Windows x64 Mono y la build se ejecutó con D3D11. Tras esa entrega, el autor probó U1 y confirmó que estaba bien; a continuación autorizó U2.

Por tanto, cualquier frase histórica como «no iniciar U2» dentro de un checkpoint anterior debe interpretarse en su fecha, no como restricción vigente. La migración queda cerrada en `docs/PROGRESO_U6.md`; bloque de continuación `docs/BLENDER_B0.md`, solo planificación.

## Repositorio y protección del trabajo local

Repositorio: `https://github.com/Marcobm1/Mamporro`.

Rama: `claude/zen-pasteur-674ik0`. Push directo permitido; sin PR salvo petición. Nunca force-push.

Al final del checkpoint U1 se observó que Unity había producido modificaciones locales posteriores al índice publicado: **14 archivos modificados y 2 ajustes nuevos** (`PackageManagerSettings.asset` y `URPProjectSettings.asset`). No se incluyeron en los commits de U1 porque aparecieron después del índice validado. No se sabe desde GitHub si siguen presentes en el PC del autor.

Antes de U2 o de cualquier relevo, ejecutar `git status --short --branch` y conservarlos. No usar reset/clean/reclonado para hacer desaparecer el estado local. Si esos cambios resultan ser ajustes legítimos generados por el Editor, decidir su inclusión de forma separada y documentada.

Al cambiar de Codex a Claude Code o viceversa, comprobar también los últimos commits y el remoto. Si el árbol está limpio y solo falta avanzar, usar `git pull --ff-only`. Si está sucio, leer el último checkpoint antes de tocar archivos.

## Entorno local confirmado

Raíz usada: `C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git`.

| Componente | Estado confirmado |
| --- | --- |
| Git | `2.54.0.windows.1` |
| Node.js | `24.21.0` |
| npm | `11.19.0` |
| Codex CLI | `0.158.0`, `C:\Users\bymar\AppData\Roaming\npm\codex.cmd` |
| Unity | `6000.6.3f1` (`45d8eee7de74`) |
| Editor | `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe` |
| Build Windows | x64 Mono disponible y utilizado |
| URP | 17.6.0 |
| Input System | 1.20.0 |
| uGUI | 2.6.0 |
| Test Framework | 1.8.0 |

Windows IL2CPP no estaba instalado en el entorno comprobado de U1. No es una carencia que haya que resolver automáticamente para U2, porque la build validada es Mono.

Hardware de referencia U1: AMD Ryzen 7 7700X, NVIDIA GeForce RTX 4070 Ti SUPER, gráfica AMD integrada y aproximadamente 32 GB de RAM. Resoluciones de ensayo: 1920×1080 y 2560×1440. Ese equipo no representa requisitos mínimos comerciales.

## U1 cerrado y aprobado

U1 creó el proyecto real en `unity/` con `Assets/`, `Packages/` y `ProjectSettings/`. Incluye un patio técnico con rampas, pendientes y obstáculos; personaje provisional; tercera persona; WASD/ratón; salto variable y deslizamiento; RenderTexture retro 240/360/480; niebla; dither y snap; UI técnica; horda centralizada en arrays/pool/rejilla y render instanciado.

No se adoptó DOTS/ECS. No hay un Rigidbody ni un Update por enemigo.

Verificaciones registradas antes de la aprobación manual:

- creación/importación y compilación C#: correcta;
- Edit Mode final: **11/11**;
- Play Mode: **1/1** con escena/render, pausa, resoluciones y reinicio de pool;
- build Windows x64 Mono normal: correcta y ejecutada con D3D11;
- web: 202/202 tests en 27 ficheros, typecheck y build correctos;
- referencia U0 y test de normalización LF/CRLF correctos;
- `.meta` completos y servicios Unity Analytics/Ads/diagnóstico desactivados.

Ensayo U1: 300/500/750/1000 entidades en 1080p y 1440p, interna 360, VSync 0, render ilimitado. El objetivo 60 FPS con 300 tuvo margen en ese equipo y las cargas superiores también, con picos aislados. La métrica GPU fue inválida/no fiable en varias condiciones y el contador de GC no estuvo disponible: no inventar esos valores ni extrapolar a equipos modestos o al combate completo.

El autor probó U1 después del checkpoint `eb691b5` y lo aprobó. Esa aprobación manual es posterior al texto original de `docs/PROGRESO_U1.md` que todavía pedía revisión.

## U2 aprobado

Único objetivo vigente: **núcleo y combate equivalentes a la web**. Implementado y aprobado por el autor el 29/09/2026; resultados en `docs/PROGRESO_U2.md`.

Portar RNG, estadísticas, fórmulas y reglas de combate; construir el bucle mínimo; completar los dos personajes, seis armas, ocho tomos, doce objetos y seis definiciones de enemigos; comparar contra la referencia U0; crear/usar una escena QA controlada; ejecutar pruebas Unity y web que correspondan; generar y probar build Windows Mono; volver a medir 300 y usar 500/750/1000 como cargas de margen/estrés sin tratarlas como balance ni límites de diseño.

Detalles: `docs/PROGRESO_U2.md`.

Historial de U2: no portaba mundo procedural completo, estructuras, interactuables, oleadas/partida completa, meta, tienda, misiones, transferencia de guardados, audio/pulido final ni servicios Steam. El mundo y la partida completa son ahora el alcance de U3 (`docs/PROGRESO_U3.md`); meta, tienda, misiones y guardado siguen para U4.

## Dirección futura ya confirmada

Después de validar la migración base, el autor quiere: mundo y estructuras mayores; mesetas/rampas y verticalidad; escalada libre por paredes; inicio con menos enemigos y aumento progresivo; más oro de enemigos durante la partida con tiempo/dificultad; visual retro más profesional; ilustraciones/iconos originales; más enemigos, armas, tomos y personajes.

No hay cantidades cerradas. El oro mencionado es oro de partida, no Calderilla del Caos. No imponer resistencia o una trepada corta a la escalada sin consultarlo.

## Cómo retomar en otra herramienta

Desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
git log -8 --oneline
git remote -v
git fetch origin
```

En este relevo no hacer pull, stash ni restaurar ajustes locales. Comparar primero HEAD, remoto y diferencias; conservar el trabajo local.

Codex CLI: iniciar `codex` en la raíz y pedir que lea `AGENTS.md`.

Claude Code: iniciarlo en la raíz; `CLAUDE.md` contiene la entrada equivalente.

Ambos deben leer `docs/CONTINUIDAD_AGENTES.md`, este documento y `docs/PROGRESO_U6.md`, además de los cierres de U3–U5. U3 y U4 están aprobadas; las seis propuestas técnicas de U3 no se consideran confirmadas individualmente. U5 está aprobado manualmente. U6 aprobado manualmente el 05/10/2026: migración cerrada. Leer el plan B0 y esperar autorización expresa antes de implementarlo.

Al terminar una sesión con cambios relevantes, actualizar el checkpoint de continuación `docs/BLENDER_B0.md` antes de entregar el turno; conservar U6 como cierre histórico aprobado.
