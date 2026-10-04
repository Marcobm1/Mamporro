# Checkpoint U6 — adoptar Unity como versión principal

Estado: **SOLO PLANIFICACIÓN AUTORIZADA, 04/10/2026. IMPLEMENTACIÓN NO AUTORIZADA.** U1–U5 aprobados; aprobación manual U5 registrada en `4fcbcdf7707f4b22594164ae27552e62629228ac` y publicada. Último cierre técnico U5: `5ef42b6083ea15251308dcebac60cb851ca766ea`. Este documento es una propuesta, no trabajo implementado ni una aprobación de cambios de configuración.

## Cómo retomar

- **Punto de partida:** cierre/aprobación U5 publicado en `4fcbcdf`; plan U6 en «Prepara el plan de adopción de Unity para U6» (hash: `git log -1 --format="%H %s" -- docs/PROGRESO_U6.md`).
- **Paso actual:** esperar respuesta del autor al plan y a las dos decisiones al final. No programar U6 todavía.
- **Terminado:** migración funcional U1–U5 aprobada; contrato/transferencia/meta, partida completa, audio y feedback. Falta la adopción y entrega de U6, no reimplementar esos sistemas.
- **A medias:** nada de runtime. Solo documentación de planificación en este hito.
- **Sin publicar deliberadamente:** los siete ajustes Unity enumerados debajo; no restaurarlos ni incluirlos para limpiar Git.
- **Pruebas de esta preparación:** únicamente auditoría Git, lectura de documentación/código, revisión de Markdown y comparación SHA-256 de los excluidos. No se han repetido suites, build, visual ni benchmark.
- **Siguiente paso exacto:** recibir decisión sobre destino de la web e identidad de entrega/protección de configuración; registrar la autorización de implementación. Después comenzar el paso 1 propuesto, sin ampliar alcance.

Comprobación CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch --untracked-files=all
git log --oneline --decorate -12
git rev-parse HEAD origin/claude/zen-pasteur-674ik0
```

Sin pull/stash/reset/clean ni force-push. Si remoto cambia inesperadamente, detenerse e informar. Mismo repositorio y rama, sin PR; un paso verificable por commit.

## Objetivo y alcance oficial

Fuente: `MIGRACION_UNITY.md`, «U6. Adoptar Unity como versión principal». Unity pasa a ser la entrada principal para desarrollar, abrir, probar y entregar MAMPORRO. El README raíz describe primero Unity; la web sigue recuperable con sus instrucciones y función de exportación. Se entrega una build Windows autónoma con identidad/rutas claras, guía de uso y problemas conocidos. Tras la aceptación final, la migración queda cerrada y las mejoras futuras se planifican en bloques distintos.

U6 no es un nuevo port de combate/audio/UI, un rediseño artístico ni una publicación comercial. No exige identidad píxel a píxel ni eliminar todas las limitaciones aceptadas en U5. No incluye instalador, firma comercial, Steam ni despliegue web por defecto. Propuesta de entrega: carpeta/ZIP local, con manifiesto de commit, Editor, backend, fecha, archivos y huellas; no subir binarios al repositorio ni publicar una release externa sin instrucción.

### Base que ya queda cerrada

- U0: referencia histórica congelada, vectores, corpus y verificadores.
- U1–U3: movimiento, render retro, arquitectura centralizada, mundo y partida completa.
- U4: progreso/meta, menús ES/EN, opciones, persistencia, exportación e importación revisable, sin duplicados ni fusión.
- U5: síntesis y reproducción, observadores de feedback, partículas, números, cámara, sacudida/destellos, sesiones y equivalencia. Aprobación manual recibida el 04/10/2026, sin inventar resultados individuales de dispositivos.

Conservar las pruebas existentes como regresión. La aceptación de U6 será de entrega/adopción y continuidad del progreso; no invalida aprobaciones anteriores.

## Revisión del repositorio y elementos provisionales

| Elemento real | Propuesta para U6 | Riesgo/tratamiento |
| --- | --- | --- |
| README raíz centrado en web | Entrada principal Unity; guía web histórica separada y enlazada | Conservar instrucciones y commits de referencia; no borrar `src/` |
| `unity/Assets/Mamporro/U3/U3_Partida.unity` | Conservar ruta/GUID y señalarla como escena principal | No renombrar ensamblados/carpetas U1–U5 por estética; evitar referencias rotas |
| `U3Project.Build`, `Builds/U3/Mamporro-U3.exe` | Entrada de entrega estable, propuesta `unity/Builds/Windows/MAMPORRO.exe`; lanzador público claro | Conservar comandos QA U1/U2/U3 y su compatibilidad; no prometer comando nuevo antes de crearlo |
| `Builds/U3Dev` | Seguir separada y rotulada diagnóstico Development | Nunca distribuirla como la build normal ni mezclar medidas |
| `companyName: Mamporro`, `productName: Mamporro U1`, `bundleVersion: 1.0` | Mantener empresa y versión existentes; proponer producto `MAMPORRO` | Cambio de producto sujeto a decisión 2 y a migración probada; no inventar identificador de plataforma/Steam |
| `applicationIdentifier: {}` | Sin cambio por defecto para esta entrega Windows | No añadir configuración móvil ni servicios |
| `Application.persistentDataPath/Progress` | Ruta estable bajo la nueva identidad, si se aprueba | En Windows, ubicación histórica esperada `%USERPROFILE%\AppData\LocalLow\Mamporro\Mamporro U1\Progress`; nueva propuesta `...\MAMPORRO\Progress`. Comprobar rutas reales de build, no asumir migración automática |
| `-u4-save-dir`, override de pruebas | Conservar compatibilidad y aislamiento | Si hay override, no explorar ni migrar el guardado real |
| F3 y acciones de debug | Mantener decisiones existentes: no recompensas con trucos | U6 no elimina debug ya aprobado; atajos históricos QA documentados |
| Siete ajustes locales excluidos | Intactos en esta preparación | No publicar nube/organización ni whitespace. Cualquier excepción futura limitada a identidad necesita decisión expresa |

Revisión basada en `U3Project.cs`, `U3Game.SaveDirectory`, `ProjectSettings.asset`, scripts actuales y checkpoints. No se ha probado todavía un mecanismo nuevo de identidad/build.

## Web histórica y estructura

Propuesta: conservar web y exportador como referencia ejecutable en el mismo repositorio, sin evolución del gameplay ni nuevas publicaciones automáticas. Mantener `src/`, paquete/lockfile, scripts y documentos históricos; crear una guía web específica al reorganizar README. No hay workflow `.github` versionado que demuestre una publicación web actual: su destino público requiere la decisión 1, no se presupone que exista hosting.

- Referencia funcional inmutable: `0505b1690656d15188860157612455639820fe1f`.
- Web con exportador: `b734b49` como base de comparación de `src/` documentada en U5; registrar el hash completo en la guía antes de cerrar U6.
- `baseline.json`, `u2-combat.json`, `u3-world.json` y corpus U4 permanecen intactos. Nunca regenerar esperados para resolver fallos.
- Usar `scripts\verify-historical-reference.ps1`, que ejecuta guardas originales sobre una instantánea aislada. La referencia histórica y el exportador se verifican por vías distintas.
- No mover ahora `src/` a otro árbol ni duplicar el juego: evitar romper hashes/rutas/herramientas por una reorganización innecesaria.

## Continuidad del guardado ante cambio de identidad

Si se aprueba renombrar el producto, implementar y probar primero la transición. Reutilizar `ProgressValidator.Stored`, `ProgressStore`, informes y confirmación existentes; no un segundo parser ni una copia ciega.

1. Resolver ruta actual y ruta histórica de forma explícita; un override QA excluye búsqueda del guardado real.
2. Si existe destino válido, cargarlo sin mezclar ni sustituirlo automáticamente por el antiguo. Si es corrupto, aplicar recuperación/backup existente sin convertirlo silenciosamente en progreso nuevo.
3. Si falta destino y existe progreso histórico, validar, mostrar origen/candidato/informe y pedir confirmación antes de copiar al destino. Nunca borrar/modificar el original histórico.
4. Si el autor elige sustituir un destino existente, usar backup, preparación inmutable, escritura/verificación y recuperación de U4. No sumar moneda, desbloqueos, extras ni recompensas.
5. Repetición, rechazo, fallo de escritura, origen corrupto/backup y dos directorios presentes deben quedar cubiertos. No liquidar partidas durante migración. No cambiar el formato `mamporro.unity-save` v1 si no cambia su esquema.

Esta política aplica acuerdos existentes de validación y sustitución; no se vuelve a preguntar si se permite fusionar, porque está prohibido. El nombre/ruta final y la autorización limitada sobre configuración sí siguen pendientes.

## Pasos propuestos y commits verificables

1. **Entrada principal y referencia web:** README Unity primero, guía web recuperable, Editor `6000.6.3f1`, paquetes fijados, escena principal, comandos vigentes, diferencias aceptadas. Revisar enlaces y ejecutar el verificador histórico si se cambia su uso o preparación. Sin reescribir gameplay.
2. **Transición del guardado:** implementar solo si se renombra identidad; pruebas puras y de archivos temporales separadas, rutas antigua/nueva y confirmación en menú; nunca probar contra el progreso personal del autor. Cerrar antes de cambiar la identidad efectiva.
3. **Identidad y build de entrega:** nombre/ruta acordados, lanzador estable y compatibilidad QA; ajuste mínimo autorizado de producto, si corresponde. Revisar explícitamente que los siete ajustes locales no se incorporan. Build Windows x64 Mono normal, sin Development. Si no puede aislarse la configuración autorizada, detenerse antes de alterar excluidos.
4. **Paquete reproducible:** carpeta completa/ZIP local con datos y DLL, manifiesto, instrucciones y limitaciones; no empaquetar saves, logs personales, cachés, credenciales ni carpetas de desarrollo. Comprobar arranque desde otra carpeta, incluso con espacios.
5. **Regresión de entrega:** ejecutar pruebas pertinentes y comprobaciones de build/guardado/nombres, capturas y sesiones. Registrar comandos, fecha, rutas, resultados exactos y fallos. Repetir benchmark solo si cambia runtime/rendimiento o aparece una regresión; el cambio de nombre no justifica por sí solo un nuevo ensayo largo.
6. **Cierre y prueba del autor:** checkpoint con SHA y huellas del paquete, guía manual y límites. Detenerse para aprobación final de U6; después registrar adopción de Unity/migración terminada. No empezar mejoras posteriores automáticamente.

## Pruebas previstas (no ejecutadas en esta preparación)

- Edit: regresión existente (392 históricas) + casos nuevos de detección/migración/idempotencia, origen/destino presentes o ausentes, corrupción/versiones, backup, rechazo y fallos de escritura; comprobar que no se toca origen ni guardado real con override.
- Play: regresión existente (37 históricas) + flujo de revisión/confirmación/cancelación y recarga, opciones/selección/progreso preservados en la escena principal si se incorpora transición.
- Build: paquete normal Windows x64 Mono desde el commit final; verificar identidad/rutas reales y arranque desde el paquete extraído. Diagnóstico separado si hace falta, sin presentarlo como entrega.
- Web/contrato: typecheck, 232 tests históricos, build web y 5 contractuales, referencias históricas intactas; navegador solo si cambian exportador/instrucciones que lo necesiten. Los totales deberán anotarse como resultados nuevos únicamente después de ejecutar.
- Visual/sesiones: comprobar menú, partida, pausa/opciones, resultados, audio, ES/EN y reinicios desde la build entregable; aprovechar automatización U5 sin alterar reglas.

Comandos actuales de referencia (CMD, Editor cerrado para Unity):

```cmd
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
node --test scripts\u4-contract.test.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
scripts\u3.cmd edit
scripts\u3.cmd play
scripts\u3.cmd build
scripts\u3.cmd visual
```

Los comandos definitivos de entrega se documentarán cuando existan; hoy la build sigue siendo `unity\Builds\U3\Mamporro-U3.exe`.

## Prueba manual final y criterios de cierre

El autor deberá extraer la entrega fuera del repositorio y arrancarla sin Editor/Node; comprobar nombre y ventana, primer inicio con carpeta de prueba, copia/revisión de un progreso anterior (sin tocar el real durante QA), persistencia tras cerrar, exportación/importación compatible, una partida hasta resultados y reinicio, música/opciones/foco e idioma. Usar carpetas temporales y después confirmar la ruta normal real de forma controlada. No pedir que borre sus saves.

U6 termina cuando: identidad y destino web decididos; comandos/documentación coherentes; build reproducible con contenido completo y manifiesto; progreso compatible conservado sin fusión/pérdida silenciosa; regresión pertinente aprobada; diferencias conocidas documentadas; autor aprueba la entrega. No equivale a lanzamiento comercial ni a balance/arte final.

## Diferencias aceptadas y riesgos

`EQUIVALENCIA_U5.md` es la lista vigente: compresor aproximado, inicio inmediato del audio y silencio sin foco, cámara solo contra terreno, sacudida residual limpiada al iniciar, partículas iluminadas y jefe sin destello blanco con paleta fija, presentación provisional aceptada. No convertirlas en nuevas funcionalidades pendientes de U6.

Riesgos específicos: cambiar producto puede ocultar progreso anterior por cambio de ruta; mover escenas/ensamblados puede romper GUID/referencias; omitir datos/DLL rompe la entrega; mezclar Development y normal falsea medidas; publicar settings locales filtra configuración ajena al hito. Reducir cambios y probar fronteras reales, no renombrar todo U3/U4/U5 por estética.

Rendimiento U5 histórico: picos finales 30,8 y 41,9 ms en el perfil/resolución documentados; 58 ms fue anterior. HUD heredado con asignaciones medidas solo en Development. No optimizar especulativamente ni garantizar esos FPS fuera del equipo/condiciones ensayados.

## Fuera de U6

Menos enemigos iniciales, más resistentes individualmente y crecimiento progresivo; bastante más oro **de partida**, no Calderilla; mundo/estructuras mayores, mesetas/rampas/verticalidad, escalada, arte retro profesional, animaciones/feedback adicionales, ilustraciones/iconos originales y expansión de contenido. Sin cantidades ni multiplicadores/curvas nuevos. Cámara contra estructuras, mando/remapeo, métricas adicionales, Steam, telemetría externa, assets de terceros, compras y publicación comercial quedan fuera. No alterar referencias ni la web para ocultar divergencias.

## Decisiones del autor pendientes — una sola tanda

1. **Destino web:** ¿conservarla como referencia ejecutable con exportador dentro del repositorio, congelada y sin nueva publicación web (recomendado), o mantener también una edición web publicada? Si existe publicación que quieras conservar, indicar su destino; no desplegar ni retirar nada por inferencia.
2. **Identidad de entrega:** ¿usar `MAMPORRO.exe`, producto `MAMPORRO`, empresa `Mamporro` y ruta `Builds/Windows`, con revisión/copia segura del progreso histórico? Recomendado. Esto requiere autorizar para U6 únicamente el cambio versionado de `productName` si se implementa en `ProjectSettings.asset`, conservando fuera los ajustes locales existentes. Alternativa sin esa excepción: normalizar ejecutable/ruta y conservar por ahora producto/ruta de guardado históricos. En esta tarea ninguno de los siete archivos se toca.

No hay otras decisiones bloqueantes identificadas. Backend Mono, Editor/paquetes, IDs, formatos, sustitución sin fusión, F3 y límites de alcance ya están resueltos.

## Cambios locales excluidos

Conservar sin publicar/restaurar: `unity/Assets/Mamporro/Generated/RetroPipeline.asset`, `unity/Assets/UniversalRenderPipelineGlobalSettings.asset`, `unity/ProjectSettings/GraphicsSettings.asset`, `unity/ProjectSettings/ProjectAuditorSettings.asset`, `unity/ProjectSettings/ProjectSettings.asset`, `unity/ProjectSettings/PackageManagerSettings.asset` y `unity/ProjectSettings/URPProjectSettings.asset` (los dos últimos sin seguimiento).

## Registro de sesión — 04/10/2026, Codex

- U5 aprobado y publicado en `4fcbcdf7707f4b22594164ae27552e62629228ac`, remoto comprobado. Preparación U6 exclusivamente documental según alcance oficial y código real de build/rutas; entradas de continuidad apuntan a este plan.
- Comprobaciones: Git, diff Markdown/índice, referencias y `src/` sin cambios; siete excluidos con SHA-256 idénticos antes/después. Sin suites nuevas ni build/benchmark.
- Commit del plan: «Prepara el plan de adopción de Unity para U6», fetch/push normal si remoto conserva el cierre anterior y verificación del HEAD. Localizar hash por este archivo.
- Pendiente: respuesta del autor a las dos decisiones y autorización de implementación. No hay código U6 a medias. U6 no está implementado ni terminado.
