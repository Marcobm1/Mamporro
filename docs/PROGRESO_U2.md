# Checkpoint U2 — núcleo y combate equivalentes

Estado: **AUTORIZADO por el autor el 29/09/2026; no implementado aún en el corte de este documento.**

U1 fue probado y aprobado manualmente. U2 es el único bloque autorizado. **No empezar U3.**

Este archivo es además el **checkpoint vivo de continuidad entre Codex CLI y Claude Code** mientras U2 siga activo. Debe actualizarse después de avances relevantes y siempre antes de un relevo de herramienta. Protocolo: [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md).

## Estado del relevo actual

- Herramientas autorizadas para ejecutar U2: **Codex CLI** y **Claude Code**, por turnos, nunca simultáneamente.
- Último estado funcional publicado antes del trabajo U2: U1 aprobado.
- U2 aún no tiene implementación registrada en este checkpoint.
- Próximo agente: cualquiera de los dos, después de comprobar Git y el árbol local.
- Primer paso obligatorio: auditar `git status --short --branch`, últimos commits y posibles cambios generados por Unity que quedaron fuera del checkpoint U1.
- No existe ninguna decisión nueva de diseño de U2 que deba inferirse de una conversación privada: el alcance de este documento es el autorizado.

## Registro de sesiones y relevos de U2

Añadir las entradas nuevas **de más antigua a más reciente**. No borrar entradas de otro agente salvo corrección factual explícita.

### 29/09/2026 — preparación documental del relevo — ChatGPT

- No se implementó código de U2.
- Se estableció que Codex CLI y Claude Code trabajarán alternándose sobre la misma rama y no en paralelo.
- Se creó `docs/CONTINUIDAD_AGENTES.md` como protocolo de handoff.
- Se actualizaron las entradas de Codex/Claude y la documentación compartida para obligar a registrar trabajo, pruebas, cambios locales y siguiente paso antes del relevo.
- No se ejecutaron pruebas Unity/web porque este cambio es exclusivamente documental.
- Siguiente paso de implementación: auditar el árbol local e iniciar la base determinista/datos/pruebas de U2 siguiendo el plan autorizado de este checkpoint.

### Plantilla para cada sesión posterior

```text
### AAAA-MM-DD — <objetivo breve> — <Codex|Claude Code>

- Punto de partida/commit:
- Trabajo realizado:
- Archivos/sistemas principales:
- Decisiones nuevas:
- Pruebas realmente ejecutadas y resultado:
- Pruebas pendientes/no ejecutadas:
- Commits creados:
- Push realizado: sí/no
- Estado del árbol al terminar:
- Cambios locales no incluidos:
- Errores/limitaciones conocidas:
- Decisiones pendientes del autor:
- Siguiente paso exacto:
```

No escribir resultados previstos en el apartado de pruebas. Si no se ejecutó una comprobación, indicar expresamente que no se ejecutó.

## Punto de partida

- Rama: `claude/zen-pasteur-674ik0`.
- Base web de referencia: `0505b1690656d15188860157612455639820fe1f`.
- Implementación U1 publicada: `abe0a9b7f0b8c53f478b91c870341999df2ea073`.
- Checkpoint U1 publicado: `eb691b595eb247118075368430d34ed0a95735d1`.
- Unity 6000.6.3f1, Windows x64 Mono, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0.
- U1 dispone de movimiento, cámara, render retro, escena técnica y horda centralizada/instanciada.
- `unity/Docs/Reference/` y `scripts/unity-reference.mjs` conservan los valores esperados de U0.

Antes de editar, comprobar `git status --short --branch`. El cierre de U1 dejó constancia de posibles cambios locales posteriores generados por Unity (14 modificados y dos ajustes nuevos). Preservarlos; no resetear ni limpiar el árbol para iniciar U2.

Al recibir un relevo, revisar también `git log -8 --oneline`, `git fetch origin` y la entrada más reciente de este registro antes de modificar archivos. Si el árbol está limpio y solo falta avanzar, usar `git pull --ff-only`.

## Objetivo de U2

Reproducir en Unity las reglas y capacidades de **núcleo + combate** de la versión web aprobada, sin ampliar todavía el mundo ni la meta. La escena de U1 puede evolucionar o complementarse con una escena QA controlada, pero la equivalencia de reglas debe poder probarse sin depender de presentación visual.

## Alcance autorizado

1. **RNG y determinismo**
   - portar el RNG/hash y las derivaciones por subsistema;
   - conservar operaciones/IDs y vectores esperados de U0;
   - no reemplazar la lógica equivalente por `UnityEngine.Random`.

2. **Estadísticas y fórmulas**
   - estadísticas base y bonificaciones;
   - daño, crítico, supercrítico y armadura;
   - experiencia/niveles y topes vigentes;
   - rareza/suerte y reglas de ofertas necesarias para probar progresión de combate.

3. **Bucle mínimo de partida**
   - jugador con vida;
   - un enemigo y un arma conectados primero como vertical slice;
   - daño al enemigo y al jugador;
   - muerte, XP, subida de nivel, derrota y reinicio limpio;
   - ningún estado/objeto/proyectil residual al reiniciar.

4. **Catálogo de combate equivalente**
   - 2 personajes: Doña Remedios y Sir Baguette, con arma inicial y pasiva;
   - 6 armas: Chancla, Naftalina, Barra, Dentaduras, Jersey y Suelo/Fregona;
   - 8 tomos;
   - 12 objetos y sus efectos/sinergias de combate;
   - 6 definiciones de enemigos de la referencia: cuatro normales, Rata élite y Pelusa Madre jefe;
   - proyectiles propios/enemigos y reglas necesarias de sus comportamientos.

5. **Arquitectura y rendimiento**
   - reutilizar la base de U1: sistemas centralizados, arrays/pools/rejilla e instanciación donde corresponda;
   - no introducir un `Update`/`Rigidbody` por enemigo por defecto;
   - evitar asignaciones por frame en bucles calientes;
   - DOTS/ECS no es requisito; solo reconsiderarlo si un perfilado real demuestra necesidad.

6. **QA y equivalencia**
   - portar pruebas de reglas útiles de Vitest a pruebas C#;
   - comparar RNG/enteros/IDs de forma exacta y doubles con las tolerancias de U0;
   - usar los valores guardados en U0 como esperados: no recalcular el «resultado correcto» con el mismo código que se prueba;
   - disponer de una escena QA controlada para activar armas, enemigos, daño, nivel, pasivas y reinicio sin depender del futuro mapa U3.

## Fuera de alcance

No implementar como parte de U2:

- mapa procedural completo, edificios y colocación del mundo;
- baúles, mesas camilla, tótems, portal, director de oleadas completo, enjambre y bucle de partida U3;
- menús/meta, Calderilla del Caos, tienda, ocho misiones o traslado de guardados U4;
- audio final, pulido global y equivalencia completa de UI U5;
- Steamworks, logros, Steam Cloud, publicación o compras;
- mejoras posmigración: escalada libre, mundo mayor, nueva curva de hordas/oro, arte final e incremento de catálogo.

No rebalancear estadísticas, economía ni contenido para «aprovechar» el port. Registrar diferencias intencionadas y pedir decisión si una equivalencia no es razonable.

## Plan de implementación recomendado dentro del bloque ya autorizado

1. Auditar el estado local y separar cualquier cambio Unity posterior a U1.
2. Portar utilidades deterministas y datos base con pruebas contra `baseline.json`.
3. Portar estadísticas/daño/XP/ofertas y pruebas unitarias.
4. Conectar vertical slice arma + enemigo + jugador + derrota/reinicio.
5. Incorporar progresivamente armas, personajes/pasivas, tomos, objetos y enemigos, con pruebas por comportamiento.
6. Montar o ampliar escena QA sin introducir sistemas de U3.
7. Ejecutar Edit Mode y Play Mode; corregir fugas/estado residual.
8. Generar build Windows x64 Mono y probarla realmente.
9. Medir 300 enemigos con combate activo; 500/750 como margen y 1000 como estrés cuando la arquitectura lo permita. Registrar CPU/frame/memoria y GPU solo si la métrica es fiable. Comparar con U1 sin prometer igualdad de coste.
10. Verificar que la web/referencia no se ha alterado; ejecutar sus tests si se toca código web o herramientas compartidas.
11. Actualizar documentación, commits pequeños, push; detenerse para prueba manual del autor.

El bloque ya está autorizado: no volver a pedir un «sí» general para estos pasos. Sí hay que preguntar antes de una decisión de diseño importante no cubierta por la referencia o por este alcance.

## Criterios de salida

U2 puede proponerse como cerrado cuando:

- los vectores RNG/IDs y las fórmulas cubiertas coinciden con U0 dentro de sus reglas de tolerancia;
- el bucle mínimo llega a derrota y reinicia sin arrastrar entidades/efectos/estado;
- los 2 personajes, 6 armas, 8 tomos, 12 objetos y 6 enemigos tienen su comportamiento de combate portado dentro del alcance U2 y pruebas razonables;
- Edit Mode y Play Mode del bloque pasan;
- una build Windows x64 Mono se genera y ejecuta;
- el combate con 300 enemigos mantiene el objetivo de 60 FPS en el equipo de referencia o, si no lo hace, se perfila y corrige/explica antes de pasar a U3;
- 500/750/1000 se documentan como margen/estrés cuando se ensayen, no como requisito de diseño;
- no se ha regenerado la referencia U0 para ocultar divergencias;
- la base web sigue recuperable y no se han introducido cambios de U3+;
- el autor recibe instrucciones de prueba y aprueba U2 antes de iniciar U3.

## Verificaciones heredadas que no deben reinterpretarse

U1 pasó 11 Edit Mode y 1 Play Mode y ejecutó ocho condiciones de benchmark técnico. Eso valida la base técnica de U1, **no** el coste del combate U2. Los 202 tests web siguen siendo evidencia de la base web mientras no se cambie; si se modifica, repetir las comprobaciones correspondientes.

## Checkpoint al terminar U2

Registrar aquí:

- commits publicados;
- archivos/sistemas portados;
- decisiones nuevas;
- pruebas exactas ejecutadas y sus resultados;
- build y condiciones de rendimiento;
- limitaciones/errores conocidos;
- cambios locales no publicados;
- instrucciones de prueba manual;
- aprobación del autor o pendientes;
- siguiente paso propuesto (U3), sin empezarlo automáticamente.

Al cerrar U2, trasladar las decisiones permanentes a `docs/DECISIONES.md`, actualizar `docs/ESTADO_ACTUAL.md` y las referencias de entrada para que Codex y Claude Code apunten al checkpoint del siguiente bloque autorizado.
