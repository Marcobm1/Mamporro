# Checkpoint U6 — adoptar Unity como versión principal

Estado: **AUTORIZADO Y EN CURSO, 04/10/2026.** U1–U5 aprobados; aprobación manual U5 registrada en `4fcbcdf7707f4b22594164ae27552e62629228ac` y publicada. Último cierre técnico U5: `5ef42b6083ea15251308dcebac60cb851ca766ea`. Plan y decisiones aprobados. Solo se autoriza publicar productName, preservando las demás diferencias locales de configuración.

## Cómo retomar

- **Punto de partida:** cierre/aprobación U5 publicado en `4fcbcdf`; plan U6 en «Prepara el plan de adopción de Unity para U6» (hash: `git log -1 --format="%H %s" -- docs/PROGRESO_U6.md`).
- **Paso actual:** pasos 1–5 terminados; siguiente, paso 6 (cierre y prueba del autor).
- **Terminado:** migración funcional U1–U5 aprobada; contrato/transferencia/meta, partida completa, audio y feedback. Falta la adopción y entrega de U6, no reimplementar esos sistemas.
- **A medias:** nada. Paso 1 `7f3760c` (Codex); paso 2 en «Revisa y copia el progreso Unity anterior antes del cambio de identidad» (hash: `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Persistence/ProgressLocations.cs`). Paso 2 `5719db4`; paso 3 en «Fija la identidad MAMPORRO y la build de entrega» (hash: `git log -1 --format="%H %s" -- unity/Assets/Mamporro/U3/DeliverySmoke.cs`). Paso 3 `25b14b1`; paso 4 en «Empaqueta y verifica la entrega Windows de MAMPORRO» (hash: `git log -1 --format="%H %s" -- scripts/mamporro.ps1`). Paso 4 `dd45b45`; ajuste de la medida de audio `daaa626`; paso 5 en «Registra la regresión de entrega de U6» (hash: `git log -1 --format="%H %s" -- docs/PROGRESO_U6.md`). Regresión final del paso 5 (05/10/2026): web 232/232, contrato 5/5, verificador histórico, Edit 405/405, Play 39/39, build, visual (audio, 5 partidas seguidas, 19 + 44 capturas), paquete y verificación correctos.
- **Sin publicar deliberadamente:** los siete ajustes Unity enumerados debajo; no restaurarlos ni incluirlos para limpiar Git.
- **Pruebas de esta preparación:** únicamente auditoría Git, lectura de documentación/código, revisión de Markdown y comparación SHA-256 de los excluidos. No se han repetido suites, build, visual ni benchmark.
- **Siguiente paso exacto:** paso 6: «Checkpoint al terminar U6» (commits y SHA, huellas del paquete, transición del guardado, estado de los siete ajustes, límites, guía de prueba manual) y estado al día en README, DECISIONES, ESTADO_ACTUAL, MIGRACION, HOJA_DE_RUTA y continuidad (B0 solo documentado). Después, detenerse.

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
| `companyName: Mamporro`, `productName: Mamporro U1`, `bundleVersion: 1.0` | Mantener empresa y versión existentes; proponer producto `MAMPORRO` | Cambio mínimo de producto autorizado, sujeto a migración probada; no inventar identificador de plataforma/Steam |
| `applicationIdentifier: {}` | Sin cambio por defecto para esta entrega Windows | No añadir configuración móvil ni servicios |
| `Application.persistentDataPath/Progress` | Ruta estable bajo la nueva identidad aprobada | En Windows, ubicación histórica esperada `%USERPROFILE%\AppData\LocalLow\Mamporro\Mamporro U1\Progress`; nueva propuesta `...\MAMPORRO\Progress`. Comprobar rutas reales de build, no asumir migración automática |
| `-u4-save-dir`, override de pruebas | Conservar compatibilidad y aislamiento | Si hay override, no explorar ni migrar el guardado real |
| F3 y acciones de debug | Mantener decisiones existentes: no recompensas con trucos | U6 no elimina debug ya aprobado; atajos históricos QA documentados |
| Siete ajustes locales excluidos | Intactos en esta preparación | No publicar nube/organización ni whitespace. Excepción expresa: únicamente productName |

Revisión basada en `U3Project.cs`, `U3Game.SaveDirectory`, `ProjectSettings.asset`, scripts actuales y checkpoints. No se ha probado todavía un mecanismo nuevo de identidad/build.

## Web histórica y estructura

Decisi?n aprobada: conservar web y exportador como referencia ejecutable en el mismo repositorio, sin evolución del gameplay ni nuevas publicaciones automáticas. Mantener `src/`, paquete/lockfile, scripts y documentos históricos; crear una guía web específica al reorganizar README. No hay workflow `.github` versionado que demuestre una publicación web actual: no se desplegará ni retirará hosting ni se preparará una nueva publicación.

- Referencia funcional inmutable: `0505b1690656d15188860157612455639820fe1f`.
- Web con exportador: `b734b49` como base de comparación de `src/` documentada en U5; registrar el hash completo en la guía antes de cerrar U6.
- `baseline.json`, `u2-combat.json`, `u3-world.json` y corpus U4 permanecen intactos. Nunca regenerar esperados para resolver fallos.
- Usar `scripts\verify-historical-reference.ps1`, que ejecuta guardas originales sobre una instantánea aislada. La referencia histórica y el exportador se verifican por vías distintas.
- No mover ahora `src/` a otro árbol ni duplicar el juego: evitar romper hashes/rutas/herramientas por una reorganización innecesaria.

## Continuidad del guardado ante cambio de identidad

Renombrado aprobado: implementar y probar primero la transición. Reutilizar `ProgressValidator.Stored`, `ProgressStore`, informes y confirmación existentes; no un segundo parser ni una copia ciega.

1. Resolver ruta actual y ruta histórica de forma explícita; un override QA excluye búsqueda del guardado real.
2. Si existe destino válido, cargarlo sin mezclar ni sustituirlo automáticamente por el antiguo. Si es corrupto, aplicar recuperación/backup existente sin convertirlo silenciosamente en progreso nuevo.
3. Si falta destino y existe progreso histórico, validar, mostrar origen/candidato/informe y pedir confirmación antes de copiar al destino. Nunca borrar/modificar el original histórico.
4. Si el autor elige sustituir un destino existente, usar backup, preparación inmutable, escritura/verificación y recuperación de U4. No sumar moneda, desbloqueos, extras ni recompensas.
5. Repetición, rechazo, fallo de escritura, origen corrupto/backup y dos directorios presentes deben quedar cubiertos. No liquidar partidas durante migración. No cambiar el formato `mamporro.unity-save` v1 si no cambia su esquema.

Esta política aplica acuerdos existentes de validación y sustitución; no se vuelve a preguntar si se permite fusionar, porque está prohibido. Nombre/ruta final y excepción limitada de configuración aprobados el 04/10/2026.

## Pasos autorizados y commits verificables

1. **Entrada principal y referencia web:** README Unity primero, guía web recuperable, Editor `6000.6.3f1`, paquetes fijados, escena principal, comandos vigentes, diferencias aceptadas. Revisar enlaces y ejecutar el verificador histórico si se cambia su uso o preparación. Sin reescribir gameplay.
2. **Transición del guardado:** implementar antes de renombrar identidad; pruebas puras y de archivos temporales separadas, rutas antigua/nueva y confirmación en menú; nunca probar contra el progreso personal del autor. Cerrar antes de cambiar la identidad efectiva.
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

## Decisiones del autor resueltas — 04/10/2026

1. Web ejecutable y exportador conservados, congelados como referencia histórica; sin evolución de gameplay, nueva publicación ni acciones sobre servicios externos.
2. `MAMPORRO.exe`, producto `MAMPORRO`, empresa existente `Mamporro`, builds normales `unity/Builds/Windows`, Development separada. Únicamente se autoriza publicar la línea productName; todas las otras diferencias locales se conservan fuera del índice. Transición revisable del progreso primero, sin fusión, pérdida del original ni acceso al progreso real desde QA.

No hay decisiones bloqueantes pendientes. [B0 Blender](BLENDER_B0.md) solo documentado como futuro; no forma parte de U6 ni está autorizado para implementación.

## Cambios locales excluidos

Conservar sin publicar/restaurar: `unity/Assets/Mamporro/Generated/RetroPipeline.asset`, `unity/Assets/UniversalRenderPipelineGlobalSettings.asset`, `unity/ProjectSettings/GraphicsSettings.asset`, `unity/ProjectSettings/ProjectAuditorSettings.asset`, `unity/ProjectSettings/ProjectSettings.asset` (excepto exclusivamente productName autorizado), `unity/ProjectSettings/PackageManagerSettings.asset` y `unity/ProjectSettings/URPProjectSettings.asset` (los dos últimos sin seguimiento).

## Registro de planificación — 04/10/2026, Codex

- U5 aprobado y publicado en `4fcbcdf7707f4b22594164ae27552e62629228ac`. Plan U6 publicado en `09d5d80b091711894837f86606a506b455092a6b`.
- Auditoría Git, diff documental y siete huellas excluidas; sin suites ni build nuevas. Las dos decisiones entonces pendientes ya están aprobadas.

## Sesión 04/10/2026 — paso 1, entrada principal (Codex)

- Punto de partida: `09d5d80b091711894837f86606a506b455092a6b`, local/remoto coincidentes tras fetch. Solo siete ajustes excluidos preexistentes.
- Autorización: pasos 1–6 y ambas decisiones resueltas por el autor; aprobación manual final todavía necesaria.
- Trabajo: README principal Unity; guía web histórica recuperable separada, sin modificar src ni referencias. Continuidad y estados vigentes actualizados. Objetivos completos B0 en BLENDER_B0 y enlaces desde hoja de ruta/decisiones; no instalación ni scripts Blender.
- Verificación: revisión de diff y enlaces locales; comparación de siete SHA-256 excluidos y ausencia de cambios en src/referencias. Sin suites ni build nuevas, porque esta pieza solo cambia documentación y no modifica verificadores.
- Commit: «Establece Unity como entrada principal y registra B0» (localizar SHA en git log de este archivo). Publicación: fetch y push normal tras comprobar el remoto; verificar HEAD remoto antes de continuar.
- Estado final previsto: únicamente los siete ajustes excluidos, sin runtime a medias. No se ha tocado productName.
- Siguiente paso: transición segura de guardado con pruebas en carpetas temporales.

## Sesión 05/10/2026 — relevo y paso 2, transición del guardado (Claude Code)

- **Relevo:** el repositorio no coincidía con el mensaje de relevo: además de `09d5d80`, Codex había publicado el paso 1 (`7f3760c`, «Establece Unity como entrada principal y registra B0») y dejado **sin publicar ni documentar** trabajo del paso 2 (`ProgressLocations.cs`, `ImportReview/ReviewLegacy`, aviso en Opciones y al arrancar, textos `legacy.*`, `LegacyProgressTests.cs`, `U6LegacySceneTests.cs`). Se informó al autor, que eligió retomar ese trabajo. Local/remoto 0/0 en `7f3760c`; siete ajustes excluidos presentes; `productName` sin tocar.
- **Trabajo revisado y aceptado (sin cambios de código):** `Persistence/ProgressLocations.cs`: resolución pura de rutas (no abre archivos). La ruta actual es `persistentDataPath/Progress`; solo en Windows y si el producto es `Mamporro\MAMPORRO` se propone la histórica `Mamporro\Mamporro U1\Progress`. El override de pruebas o `-u4-save-dir` anulan la búsqueda histórica; un `-u4-save-dir` sin carpeta lanza error en vez de volver al progreso personal. `ProgressImporter.ReviewLegacy`: lee el principal histórico con `ReadExternal` (límite del parser, sin `Load`/bloqueo/escritura en el origen) y lo valida con `ProgressValidator.Stored` (sobre `mamporro.unity-save` v1, mismo parser); si el principal no es utilizable ofrece `progress.backup.json` **identificado como copia**; después usa la revisión, `Prepare`/`Confirm`, copia de seguridad del destino y comprobación de cambios de U4 (`Unchanged`, destino modificado tras revisar → rechazado). La pantalla de importación U4 se reutiliza en modo «progreso anterior» (origen y destino visibles, ruta fija, sin Examinar). Al arrancar, solo si el destino es nuevo y existe progreso histórico, se abre esa revisión; con destino existente, botón «Revisar progreso anterior» en Opciones, nunca sustitución automática. Nunca se fusiona ni se suma nada; el original no se modifica ni se borra; no hay liquidaciones durante la transición; el formato no cambia.
- **Pruebas (Edit 13 nuevas, `LegacyProgressTests`):** rutas puras y descubrimiento solo con la identidad nueva en Windows; overrides (estático y `-u4-save-dir`) sin ruta histórica; override mal formado rechazado; revisar, cancelar, confirmar y repetir (idempotente) conservando el original y todos los campos, sin archivos nuevos en el origen; destino existente con confirmación y copia de seguridad, sin mezclar; origen corrupto y versión incompatible rechazados con la copia ofrecida explícitamente; origen ausente no es candidato; origen por encima del límite; destino cambiado tras la revisión no se sobrescribe; fallo de escritura con ambos lados intactos y reintento. **Play (2 nuevas, `U6LegacySceneTests`):** en la escena real con carpetas temporales, el override impide explorar el progreso personal; revisión, cancelación, confirmación (Calderilla, idioma y personaje adoptados), repetición sin cambios y recarga; origen inválido nunca sustituye el destino y la copia se identifica.
- **Pruebas ejecutadas (05/10/2026, Europe/Madrid):** `scripts\u3.cmd edit` **405/405** (02:24; 392 anteriores + 13); `scripts\u3.cmd play` **39/39** (10:00; 37 + 2). Una pasada previa (02:25) dio 38/39 por un **tiempo límite de 180 s** en `U3ScreensTests.TitleStartsTheChosenRunAndResultsCloseIt` (prueba de U3 que espera < 3 s, sin excepciones en el registro, ejecución de madrugada); no se reprodujo y se anota como inestabilidad de entorno. `scripts\u3.cmd build` correcta (10:02). Ninguna prueba toca el guardado real (carpetas temporales con comprobación de prefijo al borrar).
- **Commit/push:** «Revisa y copia el progreso Unity anterior antes del cambio de identidad»; fetch previo y push normal si el remoto sigue en `7f3760c`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 3 (ver «Cómo retomar»).

## Sesión 05/10/2026 — paso 3, identidad y build de entrega (Claude Code)

- **Punto de partida:** `5719db4` (paso 2 publicado); siete ajustes excluidos intactos.
- **Identidad:** `productName: Mamporro U1` → `MAMPORRO` (empresa `Mamporro` y versión `1.0` sin cambios). El índice se preparó a partir del contenido **publicado** del archivo (`git show HEAD:…` → sustitución de esa única línea → `git hash-object -w` → `git update-index --cacheinfo`), y la misma línea se cambió en la copia local sin tocar nada más. Comprobado antes del commit: `git diff --cached` de `ProjectSettings.asset` = 1 línea (`productName`); la copia local conserva las mismas 124/124 diferencias del autor sin publicar y sin diferencia en `productName`; la build de Unity no reescribió el archivo.
- **Build de entrega:** `U3Project.Build` (lo que ejecuta `scripts\u3.cmd build`) genera ahora la build normal Windows x64 Mono en una carpeta limpia `unity\Builds\Windows\MAMPORRO.exe` (`MAMPORRO_Data`, `MonoBleedingEdge`, `UnityPlayer.dll`, etc.). `Builds\U3` deja de generarse; la de diagnóstico Development sigue aparte en `Builds\U3Dev` (`scripts\u3.cmd devdiag`). `visual` y `benchmark` usan la build de entrega. Al arrancar, el reproductor (no el Editor) registra producto, versión y rutas de progreso. `U3/DeliverySmoke.cs` (`-u6-smoke`): identidad, rutas por defecto calculadas sin abrir nada, escena, audio y 2 s de partida; escribe `smoke-report.json` y sale.
- **Humo en la build real (05/10/2026, con `-u4-save-dir` temporal en una ruta con espacios):** producto `MAMPORRO`, empresa `Mamporro`, versión 1.0, Unity 6000.6.3f1, `WindowsPlayer`, no Development; `persistentDataPath` = `%USERPROFILE%\AppData\LocalLow\Mamporro\MAMPORRO`; ruta por defecto `…\MAMPORRO\Progress` y anterior `…\Mamporro U1\Progress` (cálculo puro); con el override el progreso fue a la carpeta temporal y **no** se ofreció el anterior; escena `U3_Partida`, 48 kHz, 18 fuentes, partida de 2,0 s. Unity crea la carpeta vacía del producto en `LocalLow` al arrancar (comportamiento del motor); no se leyó ni se escribió ningún guardado personal.
- **Pruebas ejecutadas:** `scripts\u3.cmd build` correcta (10:06; una pasada previa falló por una referencia ambigua a `Debug` en `U3Game`, corregida); humo correcto (exit 0). La regresión completa se hace en el paso 5.
- **Commit/push:** «Fija la identidad MAMPORRO y la build de entrega»; fetch previo y push normal si el remoto sigue en `5719db4`.
- **Árbol al terminar:** siete ajustes excluidos; `ProjectSettings.asset` sigue modificado en local solo por las diferencias del autor.
- **Siguiente paso exacto:** paso 4 (ver «Cómo retomar»).

## Sesión 05/10/2026 — paso 4, paquete reproducible (Claude Code)

- **Punto de partida:** `25b14b1` (paso 3 publicado); siete ajustes excluidos intactos.
- **Trabajo:** `scripts\mamporro.cmd` / `scripts\mamporro.ps1` (PowerShell 5, CMD, sin rutas personales). `package`: exige `unity\Builds\Windows\MAMPORRO.exe`; copia la build a `unity\Builds\Paquete\MAMPORRO-Windows-x64-<commit>` **sin** `*_BackUpThisFolder_ButDontShipItWithYourGame`, `*_BurstDebugInformation_DoNotShip`, `*.pdb` ni `*.log`; añade `LEEME.txt` (arranque, ubicación del progreso y del anterior, prueba aislada con `-u4-save-dir`, controles, limitaciones) y `MANIFIESTO.json` (producto, empresa, versión, ejecutable, plataforma, Mono, tipo normal, Editor de `ProjectVersion.txt`, commit, rama, fecha del commit, cambios locales distintos de los siete protegidos, exclusiones y cada archivo con tamaño y SHA-256 en orden ordinal); ZIP **determinista** (entradas en orden ordinal, fecha fija = la del commit, misma compresión) y su `.sha256`. `verify`: comprueba el `.sha256`, extrae en `%TEMP%\MAMPORRO entrega <id>\Carpeta con espacios\`, comprueba que cada archivo tenga el tamaño y la huella del manifiesto y que no falte ni sobre ninguno, que no haya progreso ni registros dentro, arranca `MAMPORRO.exe -u6-smoke` con guardado temporal y comprueba que se ejecutó esa copia (identidad `MAMPORRO`, no Development, partida jugada); después borra solo esa carpeta temporal. No publica nada ni toca el guardado personal.
- **Pruebas ejecutadas (05/10/2026, sobre la build de `25b14b1`):** `package` → 188 archivos, 100 MB sin comprimir, ZIP 37,7 MB, SHA-256 `46ea78e10008684ad1133c347f1094f42788502f0fa4c006e8fff9be9adb0125`; un segundo `package` dio **el mismo SHA-256** (reproducible a partir de la misma build). `verify` → 188 huellas correctas, humo correcto desde la copia extraída en ruta con espacios (partida 2,0 s, progreso en la carpeta temporal). Ese paquete es de prueba: el definitivo se genera en el paso 5 desde el commit final. Una primera ejecución falló porque la función auxiliar `Git` del script se llamaba a sí misma (PowerShell no distingue mayúsculas); renombrada a `Invoke-Git`.
- **Limitación:** la reproducibilidad garantizada es la del empaquetado a partir de una misma build; Unity no promete binarios idénticos entre dos builds, por eso el manifiesto fija las huellas de cada archivo entregado.
- **Commit/push:** «Empaqueta y verifica la entrega Windows de MAMPORRO»; fetch previo y push normal si el remoto sigue en `25b14b1`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 5 (ver «Cómo retomar»).

## Sesión 05/10/2026 — paso 5, regresión de entrega (Claude Code)

- **Punto de partida:** `dd45b45` (paso 4 publicado); siete ajustes excluidos intactos.
- **Web y referencias (sobre `dd45b45`, 10:11):** `npm.cmd run typecheck` correcto; `npm.cmd test` **232/232** (28 ficheros); `npm.cmd run build` correcto; `node --test scripts\u4-contract.test.mjs` **5/5**; `scripts\verify-historical-reference.ps1` correcto (U0 datos y medios, `u2-combat.json`, `u3-world.json`, U4 8 guardados y 6 secuencias; instantánea `qa-results/reference-7c1b16af46014edeac2da12f6784bf36`); `git diff --exit-code b734b49 -- src` y `git diff --exit-code 3754e31 -- unity/Docs/Reference` sin cambios.
- **Unity:** `scripts\u3.cmd edit` **405/405** (10:11); `scripts\u3.cmd play` **39/39** (10:12); `scripts\u3.cmd build` correcta (10:14 y 10:15) en `unity\Builds\Windows\MAMPORRO.exe`.
- **Visual y sesiones desde la build de entrega:** dos pasadas fallaron (10:14, 10:15) con todo el audio a 0 y `"focused": false`: Windows no dio el primer plano a la ventana (el equipo se estaba usando) y MAMPORRO se silencia sin foco por diseño. No es un fallo del juego, pero la comprobación no debe depender del escritorio: `daaa626` activa el foco del audio **solo durante la medida** cuando falta, lo anota (`focusForced`) y lo devuelve al estado real; el silencio sin foco sigue cubierto por `U5AudioSceneTests` y por la prueba manual con Alt+Tab. Pasada correcta (10:16, con foco real, `focusForced: false`): audio menú RMS 0,0122, silencio 0, sin música 0, efecto 0,0536, restaurado 0,0141, pico 0,114 (48 kHz); 5 partidas seguidas correctas (18 fuentes, 1 escucha, 1 música, sin partículas/números/pausa arrastrados, opciones intactas, Calderilla 30 → 30 con recibos 0 porque el guardado de la comprobación ya tenía cobrada la primera partida, memoria 18,7 MB / 241 MiB estable); 19 capturas técnicas y **44 ES/EN** (1280×720 y 1920×1080) generadas desde `Builds\Windows`.
- **Paquete final:** `scripts\mamporro.cmd package` sobre `daaa626` (build de 10:15, del mismo código): `unity\Builds\Paquete\MAMPORRO-Windows-x64-daaa626\` y `MAMPORRO-Windows-x64-daaa626.zip` (37,7 MB), **SHA-256 `58030a6ba69913886661c1cb14396c3a1337c5036b5362f6aa17e3f994bacd53`**, 188 archivos, 104 822 484 bytes sin comprimir, manifiesto con commit `daaa626c4fe9cead609a522cacb8dd63bc2c0dd7` y 0 cambios locales distintos de los protegidos; SHA-256 de `MAMPORRO.exe` `96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873`. `scripts\mamporro.cmd verify`: 188 huellas correctas; humo desde la copia extraída en `%TEMP%\MAMPORRO entrega <id>\Carpeta con espacios\`, producto `MAMPORRO` 1.0, partida 2,0 s, progreso solo en la carpeta temporal.
- **No ejecutado:** ensayo de rendimiento ni diagnóstico Development: U6 no cambia el runtime de combate ni el render (solo identidad, rutas del guardado, revisión del progreso anterior y comprobaciones de QA); según el plan, el cambio de nombre no justifica un ensayo largo. Referencia vigente: ensayo final de U5 (picos 30,8 y 41,9 ms en 2560×1440 con cuatro armas).
- **Commit/push:** «Registra la regresión de entrega de U6»; fetch previo y push normal si el remoto sigue en `daaa626`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 6 (ver «Cómo retomar»).
