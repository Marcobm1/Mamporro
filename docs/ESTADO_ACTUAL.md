# MAMPORRO — estado actual y punto de continuación

**21/10/2026, cierre de landmarks revalidado:** recuperado trabajo local sin publicar desde `27414c5`; suite nueva **12/12** y contadores deterministas idénticos a los del día 20. **Puerta 1 negativa por trabajo agregado; Puerta 2 no iniciada.** Preparación a 64 m 3,697–3,986 s; mejor cohorte dispersa 9889018 unidades. [Pruebas y publicación](PROGRESO_P0.md), [informe](EXPERIMENTO_LANDMARKS_P0D3.md). Siete ajustes conservados; sin cambios de producción, integración, D4/D5 ni relajación de presupuestos. Detenido para revisión del autor.

**20/10/2026, posterior al preprocesado regional: landmarks dirigidos evaluados, Puerta 1 negativa por trabajo agregado.** Preparación completa a 64 m 3,598–3,905 s; A* con 8 referencias reduce 19,51× las extracciones frente a Dijkstra individual, pero 750 dispersos requieren 9889018 unidades frente a 1992760 heredadas del servicio compartido para los mismos inputs. **Puerta 2 no iniciada, parada expresa para revisión.** Pruebas nuevas 9/9 y 3/3, sin cambios de producción. [Informe y límites](EXPERIMENTO_LANDMARKS_P0D3.md), [checkpoint/publicación](PROGRESO_P0.md). D3 bloqueado; sin integración, D4/D5 ni nueva arquitectura. 3000 y reacción 0,3/0,5 s intactos.

**20/10/2026, preprocesado exacto autorizado y evaluado desde `f355409`: puerta inicial negativa.** Preparación parcial S/M/L de HORDA750 a 16/32/64: 4,413–4,967 s en nueve muestras, por encima del objetivo 4 s, sin tablas almacenadas ni validación; máximo de 6 s no superado. Matriz mundial descartada: 13,80 GiB solo distancias. **Parada antes del prototipo de servicio**, conforme a la condición del autor; D3 bloqueado, sin integración, D4/D5 ni cambio arquitectónico adicional. [Informe y alternativas](EXPERIMENTO_PREPROCESADO_P0D3.md), [pruebas, protecciones y publicación](PROGRESO_P0.md). Presupuesto 3000 y reacción 0,3/0,5 s intactos. Esperar revisión del autor.

**19/10/2026, cierre posterior al experimento de lotes:** consultas exactas bajo demanda autorizadas y evaluadas, código `3aaac6f`, medidas `2940d9b`. **Resultado negativo; D3 bloqueado y parada para revisión.** Específicas nuevas 35/35, carga 3/3 × tres repeticiones y Play 53/53; referencias y huellas intactas. Vecino 750 concentrados: 57764 unidades; dispersos: 1992760. Recorrido móvil disperso: p95 S/M/L 85,017/102,883/0,633 s y cobertura 0,429/0,222/2,722 %. [Informe y alternativas](EXPERIMENTO_DEMANDA_P0D3.md), [checkpoint operativo](PROGRESO_P0.md). Presupuesto 3000 intacto; sin integración en enemigos, cambios de backend ni D4/D5. Esperar revisión antes de otra arquitectura.

**Cierre del 19/10/2026:** experimento de lotes publicado en `9922599`; Edit heredado comprobado **960/960**, Play nuevo **53/53**, referencias históricas nuevas coincidentes y protecciones intactas. Cierre documental y evidencias en [PROGRESO_P0](PROGRESO_P0.md). **Parada para revisión del autor por rendimiento insuficiente**, sin integración en enemigos ni D4/D5. [Alternativas propuestas, todavía no implementadas](EXPERIMENTO_LOTES_P0D3.md#alternativas-para-revisión-del-autor).

**D3, experimento de lotes del 18/10/2026:** autorización ejecutada hasta medición, **resultado negativo**. Agrupación reversible y propagación multiobjetivo exactas en 30+33 pruebas específicas; scheduler 3/3 con tres repeticiones. Mejor vecino (64 m): 1101086 unidades globales, hasta 6,133 s; móvil p95 S/M/L 7,833/6,750/3,700 s. [Informe completo](EXPERIMENTO_LOTES_P0D3.md). No integrar ni seguir construyendo campos sobre este candidato; presupuesto 3000 y objetivos intactos. D3 bloqueado, D4 no iniciado. Regresiones/publicación y siguiente paso exacto en [PROGRESO_P0](PROGRESO_P0.md). Las consultas de autorización inferiores son historial: el experimento ya fue autorizado y evaluado.

**D3, evaluación del 18/10/2026:** el comparador incremental autorizado conserva exactitud, pero falla reacción: primera publicación S/M/L 6,217/5,950/3,633 s; móvil p95 28,067/24,067/16,300 s. Jerarquía regional repetida: cero publicaciones en 180 s simulados. Edit nuevo 894/894; cierre de verificaciones/publicación en [PROGRESO_P0](PROGRESO_P0.md), medidas y alternativas en [REGIONES_P0D](REGIONES_P0D.md). **Parada para decidir el siguiente experimento arquitectónico**, sin adoptar comparador, subir 3000 ni relajar objetivos. Inanición física M/L sin resolver. D3 abierto, D4 no iniciado; partidas normales intactas.

**Continuación D3, 17/10/2026:** tras la consulta motivada por el resultado inferior, el autor responde «sigue». Se evalúa el comparador incremental exacto previsto como alternativa futura, sin adoptar automáticamente otro backend ni cambiar 3000. Edit nuevo del prototipo regional: 878/878; Play y evaluación siguiente en el [checkpoint](PROGRESO_P0.md). D3 no cerrado, D4 no iniciado.

**D3 detenido por medición, 17/10/2026:** transferencias inversas `2ca9c29`, 71/71 específicas nuevas. Prototipo de costes de salida `9e11d37`, 6/6 invariantes/diagnósticos; **cero publicaciones S/M/L en 180 s simulados** en HORDA750 frío, a 16/32/64 m, aun con 1000 unidades por clase/tick. [Medidas y límites](REGIONES_P0D.md). No acredita persecución ni mejora sobre D0; campos locales/integración pendientes. Incumplimiento importante: solicitada decisión sobre comparador incremental exacto, no adoptado. D4 no iniciado. D3/D4 conservan autorización general, pero no avanzar funcionalmente hasta resolver esta decisión; 3000 y protecciones intactos. Regresiones/checkpoint vigente en [PROGRESO_P0](PROGRESO_P0.md).

**Actualización D3 parcial, 16/10/2026:** primera pieza publicada `9575246`: transferencias físicas fraccionadas, caché regional acotada y cuotas deterministas S/M/L con techo 3000. Específicas nuevas 38/38, Edit 839/839 y Play 53/53; referencias históricas coincidentes y protecciones 9/9 intactas. [Medidas y límites](REGIONES_P0D.md). No hay todavía campos locales ni integración en movimiento; inanición en persecución no declarada resuelta. Siguiente: transferencias inversas y campos compartidos con costes globales/dependencias; D3 sigue abierto, D4 después de verificarlo y D5 excluido. Checkpoint operativo en [PROGRESO_P0](PROGRESO_P0.md).

**Actualización D2, 15/10/2026:** portales dirigidos y conectividad global por clase verificados; código `7c50d9f`, 44/44 específicos, Edit 801/801 y Play 53/53 nuevos. Referencias e integridad 9/9 correctas. [Medidas y límites](REGIONES_P0D.md). Siguiente D3 autorizado: transferencias/caché/campos y reparto justo, con inanición M/L aún prioritaria y 3000 conservados; D4 después, D5–D7 excluidos. Asignaciones N/D: contador Mono no detecta testigo; no certificar cero GC.

**Actualización 15/10/2026:** D1 verificado: constructor regional físico optativo en 80/128 m; 28/28 regionales, Edit 757/757 y Play 53/53 nuevos, referencias e integridad correctas. [Resultados y límites](REGIONES_P0D.md). Siguiente D2, después D3/D4 por orden; D5 excluido. Corrección D0 publicada en `5e41406` y repetida sin discrepancias nodo/posición. Inanición M p95 57,6 s / L sin publicación en 180 s sigue prioritaria para D3, 3000 intactos.

**Actualización histórica 14/10/2026:** diagnóstico D0 reproducido; [informe](DIAGNOSTICO_P0D0.md) y cierre en [PROGRESO_P0](PROGRESO_P0.md). El autor autoriza corregir el redondeo double→float en `EnemyNavigation.TryStep`: convertir X/Z antes de validar y almacenar. Cambio acotado en verificación; continuar D1 si pasa sin regresiones. D1–D4 siguen autorizados, D5–D7 excluidos. Inanición M/L prioritaria para D3 con 3000 nodos/tick. Los procesos Claude 20700/20264 pertenecen a otro proyecto por confirmación expresa del autor y no bloquean MAMPORRO.

Corte vigente: **12/10/2026**, U3 **aprobada manualmente por el autor como base funcional**. Último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`. U4 **aprobada manualmente por el autor el 04/10/2026** (último cierre técnico `3754e31`); [cierre](PROGRESO_U4.md#checkpoint-al-terminar-u4). U5 **aprobado manualmente por el autor el 04/10/2026**, tras el cierre técnico `5ef42b6`; cierre, guía de escucha y prueba manual en [PROGRESO_U5](PROGRESO_U5.md#checkpoint-al-terminar-u5); equivalencia en [EQUIVALENCIA_U5](EQUIVALENCIA_U5.md). U6 **implementado, verificado y aprobado manualmente por el autor el 05/10/2026; migración U0–U6 formalmente cerrada**; entrega `unity\Builds\Windows\MAMPORRO.exe`, paquete y guía en [PROGRESO_U6](PROGRESO_U6.md#checkpoint-al-terminar-u6). Después: B0 Blender cerrado ([BLENDER_B0](BLENDER_B0.md)); **B0.1–B0.6 cerrados y aceptados por el autor como pipeline validado técnicamente y validado para proponer adopción**; sin revisión manual de la build ni arte final aprobado. P0-A1–A5 validados técnicamente y **P0-A6 aprobado manualmente por el autor el 11/10/2026**; **P0-B aprobado manualmente por el autor el 12/10/2026**; P0-C aprobado manualmente el 12/10/2026; P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados.

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
- U6: implementado y verificado el 05/10/2026 (Unity principal, transición del guardado, identidad `MAMPORRO`, build `unity\Builds\Windows\MAMPORRO.exe`, paquete con manifiesto); aprobado manualmente por el autor. Unity es la versión principal; web histórica ejecutable, congelada y recuperable con exportador. B0 cerrado y aceptado técnicamente; mejoras posteriores fuera de la migración cerrada.
- Rama de trabajo y por defecto: `claude/zen-pasteur-674ik0`.
- Implementación U1: `abe0a9b7f0b8c53f478b91c870341999df2ea073`.
- Checkpoint U1: `eb691b595eb247118075368430d34ed0a95735d1`.
- Contexto compartido para U2: `2b67ea53a54c2e0e07659562e334d423006d89f7` y documentación posterior de continuidad.
- **Codex CLI y Claude Code son herramientas de trabajo válidas por turnos sobre la misma rama. No deben trabajar simultáneamente.**

## Continuidad entre agentes

El proyecto ya no debe depender del historial de una sola conversación o herramienta.

Codex CLI y Claude Code se alternarán cuando termine la sesión/tokens de uno. El repositorio es la memoria compartida y cada agente debe dejar un relevo suficiente para que el otro continúe sin reconstruir contexto manualmente.

Norma canónica: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md).

El checkpoint de continuación es [PROGRESO_P0](PROGRESO_P0.md), **P0-A y P0-B cerrados (aprobados manualmente el 11/10 y el 12/10/2026); P0-C aprobado manualmente el 12/10/2026; P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados**. [BLENDER_B0](BLENDER_B0.md) conserva el cierre técnico aceptado, publicado en `0b0b68c`. Cierre aprobado U6 en [`PROGRESO_U6.md`](PROGRESO_U6.md), publicado en `39602c8aaba3ca163cde39c654b005f23f9e5f2f`. U5 aprobado y publicado en `4fcbcdf7707f4b22594164ae27552e62629228ac`; cierre en [`PROGRESO_U5.md`](PROGRESO_U5.md). [`PROGRESO_U4.md`](PROGRESO_U4.md) y [`PROGRESO_U3.md`](PROGRESO_U3.md) conservan los cierres aprobados, pruebas e instrucciones manuales. Actualizar el checkpoint vigente antes de cada relevo.

Cada relevo debe registrar trabajo realmente realizado, pruebas ejecutadas, commits/push, limitaciones, cambios locales sin publicar y siguiente paso exacto. No registrar planes como si fueran resultados.

## Qué cambió desde los documentos de traspaso inicial

Los Markdown del traspaso del 28/29 de septiembre decían que Codex CLI estaba pendiente y que U1 no había empezado. Eso ya no es cierto.

El entorno local se comprobó, Codex CLI se instaló/verificó y U1 se implementó realmente en Unity. Unity importó el proyecto, compiló C#, ejecutó Edit/Play Mode, generó una build Windows x64 Mono y la build se ejecutó con D3D11. Tras esa entrega, el autor probó U1 y confirmó que estaba bien; a continuación autorizó U2.

Por tanto, cualquier frase histórica como «no iniciar U2» dentro de un checkpoint anterior debe interpretarse en su fecha, no como restricción vigente. La migración queda cerrada en `docs/PROGRESO_U6.md`; bloque de continuación `docs/PROGRESO_P0.md`: P0-A y P0-B cerrados y aprobados; P0-C aprobado manualmente el 12/10/2026; P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados.

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

Ambos deben leer `docs/CONTINUIDAD_AGENTES.md`, este documento y `docs/PROGRESO_U6.md`, además de los cierres de U3–U5. U3 y U4 están aprobadas; las seis propuestas técnicas de U3 no se consideran confirmadas individualmente. U5 está aprobado manualmente. U6 aprobado manualmente el 05/10/2026: migración cerrada. B0 cerrado y aceptado técnicamente; P0-A/B/C aprobados; implementar D0–D4 con verificaciones y checkpoints; detenerse antes de D5.

Al terminar una sesión con cambios relevantes, actualizar el checkpoint de continuación `docs/PROGRESO_P0.md` antes de entregar el turno; conservar U6 como cierre histórico aprobado.

Punto de continuación vigente: [PROGRESO_P0](PROGRESO_P0.md) — circuito QA aislado de movilidad/verticalidad. **P0-A cerrado:** A1–A5 validados y A6 aprobado manualmente por el autor el 11/10/2026 (código `373620d`, cierre `dc8ab26`). **P0-B cerrado:** aprobado manualmente por el autor el 12/10/2026 (navegación vertical y escalada de todos los enemigos, circuito QA B con F10; código `a9cac92`, cierre técnico `5688e90`). **P0-C aprobado manualmente el 12/10/2026** (circuito QA C procedural con F10/F11, plan [PLAN_P0C](PLAN_P0C.md); código/builds `9a24b6f`, cierre técnico `369fdff`): limitaciones aceptadas provisionalmente y 3000 nodos/tick conservados. **P0-D0–D4 autorizados el 12/10/2026; D5–D7 no autorizados.** B0 cerrado y publicado en `0b0b68c31dda803cf9cf566b3c96f369ed893ce3`.

Plan de continuación: [P0-D — escala y navegación regional](PLAN_P0D.md), **ocho decisiones aprobadas; implementación autorizada únicamente D0–D4**. Cierre formal P0-C publicado en `b755987`; mantener 3000 nodos/tick.
