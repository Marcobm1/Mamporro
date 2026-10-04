# Checkpoint U4 — meta, UI, opciones y traslado de guardados

Estado: **IMPLEMENTACIÓN AUTORIZADA Y EN CURSO desde el 04/10/2026.** Política de recuperación revisable y estricta aprobada por el autor. Contrato en [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md). U3 aprobada manualmente como base funcional el mismo día; último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`.

## Cómo retomar

- **Paso 1 publicado:** `fabaf2a64f5ff6c34d3592a25a6594f066130208`. Paso 2 en «Porta las reglas meta y la liquidación de U4»; localizar hash con `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Core/MetaRules.cs`.
- **Paso actual:** 5, importación Unity del archivo exportado (sin empezar).
- **Terminado:** pasos 1–4. Paso 4 en «Exporta el progreso web para Unity» (hash: `git log -1 --format="%H %s" -- src/save/exportProgress.ts`); guía en [EXPORTACION_U4](EXPORTACION_U4.md). Contrato `fabaf2a`, meta `dd994b8`, validador `d3927ff`; almacenamiento en «Guarda el progreso de U4 con copia y recuperación» (hash: `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Persistence/ProgressStore.cs`). Último Edit Mode 317/317 y Play Mode 17/17 (paso 4, 04/10/2026 11:53–11:54); web 232/232, typecheck y build web correctos; descarga real ES/EN en Edge.
- **A medias:** ninguna pieza rota. Persistencia probada como servicio, aún no conectada a aplicación/menús. Existe la exportación web; no existe todavía la UI de importación Unity.
- **Sin commit a propósito:** siete ajustes Unity de «Cambios locales excluidos».
- **Siguiente paso exacto:** paso 5, importación Unity: servicio de aplicación sin UnityEngine que lea el archivo elegido con el límite de 256 KiB, llame a `ProgressValidator.Import`, prepare con `ProgressStore.Prepare` sobre la instantánea cargada, muestre informe/vista previa y solo tras confirmar ejecute `Confirm` (backup, sustitución, relectura); cancelación sin escritura, importación repetida sin cambios, sin fusionar ni liquidar. UI técnica uGUI mínima en la escena U3 para elegir archivo, revisar y confirmar; los menús completos son el paso 7.
- **Autorización:** continuar los pasos 2–9 sin nuevas confirmaciones generales. Detenerse solo ante decisión nueva importante y, al terminar U4, para prueba manual del autor.

Comprobación desde CMD:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git fetch origin
git status --short --branch --untracked-files=all
git log --oneline --decorate -12
git remote -v
```

Sin pull/stash automáticos ni limpieza. Un paso verificable cada vez; checkpoint, commit pequeño en español, fetch y push normal. Si el remoto cambia inesperadamente, detenerse e informar. Sin PR. Al terminar U4, detenerse para prueba manual del autor.

## Alcance y límites

Menú principal, preparación, selección, tienda, ocho misiones, resultados completos, español/inglés, opciones equivalentes, Calderilla del Caos, compras, desbloqueos, filtros, liquidación única, guardado local Unity versionado y transferencia web → Unity con validación, migraciones, copia y recuperación.

No guardar partidas activas ni fusionar progresos. Con guardado Unity existente: validar primero, hacer copia de seguridad y pedir sustitución explícita; nunca sumar saldos/recompensas. No modificar el original exportado. Preservar progreso compatible y los IDs existentes. Mostrar cualquier dato no transferible; no perder datos válidos silenciosamente.

No incluye mundo/estructuras mayores, nuevo terreno, escalada, animación/arte/audio finales, expansión de contenido, balance, Steam, telemetría externa ni mando/remapeo nuevo. La dirección futura aprobada está en `unity/Docs/HOJA_DE_RUTA.md` y empieza después de terminar y aprobar U4–U6.

Excepción web autorizada para U4: modificar `src/` solo para exportación JSON validada del progreso, sin alterar lógica ni balance. `0505b1690656d15188860157612455639820fe1f` sigue siendo referencia funcional. No modificar/regenerar `baseline.json`, `u2-combat.json` ni `u3-world.json`.

## Revisión de la implementación existente

### Web

- `src/save/schema.ts`: v3 `{ version, settings, meta }`. V1 conserva opciones y recibe meta inicial; v2 conserva meta y completa opciones mediante saneamiento. Versiones futuras, JSON corrupto o versión inválida provocan reinicio al cargar en la web. **Ese reinicio no debe usarse como importación válida en Unity.**
- `src/save/SaveManager.ts`: claves `mamporro.save` y `mamporro.save.backup`, copia del texto inválido al reiniciar, fallback en memoria si almacenamiento falla. No hay exportador/importador actual.
- `src/systems/meta.ts`: saneamiento, compras y liquidación. Moneda entera acotada a 1e9; extras de 0 a 3 por acción, añadidos a los 2 usos base. Desbloqueos/selección limitados a IDs conocidos. `completed` restablece objetivo y desbloqueo, sin volver a conceder moneda. Compras representadas por desbloqueos y extras; no inventar un historial de compras ausente.
- `src/data/meta.ts`: personajes `remedios`/`baguette`; misiones `first`, `kills`, `chests`, `shrines`, `challenge`, `level`, `victory`, `noLife`; extras `rerolls`, `skips`, `banishes`. Tienda: `baguette`, `jersey`, `baraja`, `olla`. Mantener precios, objetivos, premios e IDs exactos del código.
- `lastRun` evita volver a liquidar la última partida; `Game.runSettled` impide callbacks repetidos. Las partidas con trucos no conceden meta. Importar no llama a liquidación ni vuelve a reclamar misiones.
- `Game.ts`, `Run.ts`, `ui/MetaScreens.ts`, `ui/components.ts`: selección persistida y solo desbloqueada, filtros de nuevas armas/objetos, extras copiados al iniciar, resultados con desglose, abandono sin recompensa y confirmación. `usedLifeTome` alimenta la misión correspondiente.
- Opciones: volúmenes música/efectos, silencio, partículas reducidas, sacudida, destellos, idioma, sensibilidad, resolución interna 240/360/480, vertex snap, dithering, deslizar con Ctrl, FPS y duración 5/10/15. I18n ES/EN con sustituciones y detección inicial de idioma; todos los menús deben refrescar idioma, no solo los textos creados después.
- Se revisaron también `save/save.test.ts` y `systems/meta.test.ts`. Son el punto de partida de la matriz U4, sin sustituir pruebas específicas del importador.

### Unity reutilizable

- `WorldRun`, `CombatRun`, director, físicas, arrays/pools/rejilla e instancing: conservar la única implementación del combate y la partida.
- `U3Game`, `RunScreens`, HUD/cartas/render y reinicio: reutilizar el flujo de sesión y presentación; añadir la capa de aplicación/meta y adaptar pantallas, sin duplicar combate.
- `Core/Offers.cs` ya acepta filtro de armas en `Generate`, pero las llamadas actuales no lo conectan; `Replace` y selección de objetos necesitan filtros coherentes. Mantener las escenas QA U1–U3 con su catálogo desbloqueado explícito para no cambiar las referencias.
- `U2/CombatText.cs` y recursos ES/EN permiten reutilizar textos y catálogos. Falta actualización integral de etiquetas al cambiar idioma.
- No hay servicio de guardado/meta equivalente. Reutilizar contadores de combate, baúles, santuarios, desafíos y marca de trucos; añadir seguimiento equivalente de tomo de vida, identidad y recibo de partida. No confundir oro de partida con Calderilla.

## Seis propuestas técnicas U3: clasificación, sin confirmación individual

| Propuesta | Tratamiento recomendado para U4 | ¿Decisión bloqueante? |
|---|---|---|
| `CombatRun.HoldWhileChoosing` | Registro de la separación ya implementada entre pausa de aplicación y lógica; conservar. | No. |
| Márgenes de totales tras 120 s | Registro de metodología existente; no ampliar tolerancias ni modificar referencias en U4. | No. |
| Mapa inicial fijo `MAMPORRO` | Provisional de U3: conservar para QA; inicio normal U4 equivalente al mapa aleatorio web y semilla editable. | No. |
| Pausa sin opciones | Provisional: sustituir por opciones equivalentes en U4. | No. |
| Metodología de ensayo U3 | Registro reproducible del ensayo realizado, con sus límites de medida. | No. |
| Presentación técnica de señales/baúles/mesas/fuente | Provisional; pulido de equivalencia U5. Arte/animaciones nuevos tras migración. | No. |

Las propuestas 1, 2 y 5 pueden cerrarse como registro técnico tal cual; 3, 4 y 6 tienen sustitución prevista. La aprobación funcional de U3 no se registra como aceptación individual de estas seis propuestas. No es necesario resolverlas para planificar U4.

## Política resuelta por el autor — 04/10/2026

Recuperar solo lo inequívoco. Producir candidato normalizado e informe en memoria antes de escribir. Mostrar qué se conserva/excluye/sustituye y por qué; confirmar explícitamente. JSON ilegible, formato/versión desconocidos y corrupción crítica ambigua son fatales. Opciones inválidas/ausentes e IDs desconocidos en colecciones extensibles permiten recuperación informada. No clamps, saldos inventados, compras inferidas ni suposiciones sobre recompensas cobradas.

Sustitución completa, nunca fusión: validar → informe → confirmar → backup → escritura segura → relectura/validación → éxito. Original exportado intacto; importación repetida no concede nada. Temporal/backup/recuperación se probarán con fallos IO; no prometer atomicidad sin garantías verificadas. Contrato preciso: [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md).

## Plan propuesto por pasos/commits verificables

Plan aprobado. Cada fila es una unidad coherente; dividir si tamaño o pruebas lo exigen. Pasos 1–4 terminados; pasos 5–9 pendientes.

| Paso | Resultado y límites | Verificación principal |
|---|---|---|
| 1. Contrato y casos de transferencia | Inventario exacto de save/IDs/opciones y corpus de casos válidos/inválidos. Concretar envoltorio y versión de transferencia después de registrar la respuesta; distinguir formato de transporte y save interno. | Casos derivados del código web; comprobar que referencias congeladas y reglas no cambian. |
| 2. Meta y liquidación | Port puro C# de saneamiento, moneda, compras, extras, ocho misiones, desbloqueos y recibo único. | Pruebas de paridad, límites, insuficiencia de saldo, compras repetidas, misiones completas/parciales, debug y doble liquidación. |
| 3. Persistencia Unity | Guardado versionado, migración, validación antes de reemplazar, escritura segura, copia/recuperación y aviso de fallo. Mantener estado anterior ante error. | Almacenamiento sustituible para fallos de lectura/escritura, interrupción, disco/permisos, copia y carga. |
| 4. Exportación web | Botón y descarga de progreso JSON validado; original intacto, sin cambiar partida ni balance. | Typecheck, tests, build y navegador `?test`; descarga/relectura, datos equivalentes y errores de almacenamiento. |
| 5. Importación Unity | Selección local, límites de lectura, validación/migración, vista previa, copia y confirmación de sustitución; no fusionar ni liquidar. | Matriz de versiones/errores, cancelación, reimportación doble y fallo durante sustitución. |
| 6. Conexión meta ↔ partida | Selección, filtros en cartas/reroll/banish/objetos, extras iniciales, seguimiento de misión, resultados y liquidación persistente una vez. | Edit + Play: nueva partida, debug sin premios, victoria/derrota, abandono, reinicio, recarga sin duplicar. |
| 7. Menús completos | Inicio, preparación, personajes, tienda, ocho misiones, resultados, importación y avisos de guardado ES/EN. Reutilizar sesión U3. | Play: navegación, saldo, bloqueos, compras, selección, seis combinaciones personaje/duración con fixtures QA. |
| 8. Opciones y pausa | Aplicar/persistir opciones equivalentes, idioma inmediato, entrada y render; conservar opciones de audio para U5 sin implementar audio final. | Play y build: límites, cambio en pausa, foco, recarga, ES/EN sin textos obsoletos. Documentar efectos todavía dependientes de U5. |
| 9. Cierre U4 | Regresión completa, build Windows x64 Mono, recorrido manual preparado, resultados/rutas exactos y limitaciones. | Pruebas descritas abajo; publicar checkpoint y detenerse para prueba/aprobación del autor. |

## Exportación/importación — contrato fijado en el paso 1

Transferencia `mamporro.progress` v1 y persistencia `mamporro.unity-save` v1, con DTO de progreso normalizado; adaptador explícito de JSON web v1/v2/v3. Versión de origen no vuelve a migrar un DTO ya normalizado. Contrato, invariantes y política fatal/recuperable en [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md).

Las guardas actuales de `scripts/unity-reference*.mjs` prohíben cambios de `src/` y el baseline incluye hashes de fuentes. Al añadir el exportador, conservar esas guardas y comprobar la referencia sobre una instantánea aislada de la base aprobada; comprobar el exportador y la ausencia de cambios funcionales por separado sobre la rama actual. No afirmar que la web sigue byte a byte intacta ni relajar esperados para acomodar el exportador. Documentar el procedimiento reproducible al implementarlo.

## Plan de pruebas de cierre (pendiente de ejecución)

- **Guardado/transferencia:** sin guardado; v1/v2/v3; futura; JSON corrupto; campos ausentes; tipos incorrectos; IDs desconocidos; negativos y extremos; parcial y todo desbloqueado; misiones parciales/completadas/recompensas reclamadas; mismo archivo dos veces; sin/con guardado Unity; cancelación; escritura fallida; copia y recuperación. Comparar campos compatibles antes/después, archivo original intacto y cero recompensas extra.
- **Meta:** ocho misiones, precios/fórmulas exactos, personaje seleccionado bloqueado, extras máximos, desbloqueos en todos los caminos de ofertas/objetos, compra doble, saldo insuficiente, tomo de vida, trucos, callbacks/reload de resultados, abandono sin recompensa.
- **Play Mode:** navegación ES/EN, opciones actualizadas en pantallas abiertas, importación/confirmación/cancelación/avisos, persistencia al recargar, pausa/foco y reinicio, 2 personajes × 5/10/15 mediante datos de prueba; conservar regresión U1–U3.
- **Comandos existentes:** `scripts\u3.cmd edit`, `scripts\u3.cmd play`; build/visual U3 si se altera lo compartido. Crear/documentar lanzadores U4 solo cuando exista escena U4: Edit, Play, build Windows x64 Mono y visual. No citar ahora un lanzador U4 como existente.
- **Web al implementar exportación:** `npm.cmd run typecheck`, `npm.cmd test`, `npm.cmd run build`, navegador `?test` y descarga/importación real. Referencias: `node scripts\unity-reference.mjs`, `node scripts\unity-reference-u2.mjs`, `node scripts\unity-reference-u3.mjs`, usando la base aislada cuando exista la excepción exportadora.
- **Manual en build:** menú y preparación; personaje inicial y compra de Baguette; ocho misiones y recibo; derrota/victoria normal y debug sin premios; reinicio; ES/EN; opciones/pausa; salir y abrir conservando progreso; exportación web real, importación en instalación vacía y existente, cancelación, importación repetida, errores y recuperación. Audio final permanece U5. Registrar resolución, semilla, duración, personaje, comandos, fecha y rutas de resultados.

## Cambios locales excluidos

Conservar sin publicar ni restaurar:

- `unity/ProjectSettings/ProjectSettings.asset` (identificadores locales de nube).
- `unity/ProjectSettings/ProjectAuditorSettings.asset` (espacios/EOL).
- `unity/ProjectSettings/PackageManagerSettings.asset` (sin seguimiento).
- `unity/ProjectSettings/URPProjectSettings.asset` (sin seguimiento).
- Espacios/EOL en `unity/Assets/Mamporro/Generated/RetroPipeline.asset`, `unity/Assets/UniversalRenderPipelineGlobalSettings.asset` y `unity/ProjectSettings/GraphicsSettings.asset`.

## Registro de sesiones y relevos

### 04/10/2026 — aprobación de U3 y planificación de U4 — Codex

- **Punto de partida:** local y remoto `83246ebfd873b0c5d23611a1d557f83096180217`, ahead 0 / behind 0; sin commits inéditos ni WIP aparte de los siete ajustes Unity excluidos.
- **Trabajo:** lectura de documentos y código descritos arriba; actualización documental de aprobación U3 y autorización limitada de U4; registro de dirección futura, clasificación de las seis propuestas y plan de pasos/pruebas/transferencia.
- **Archivos/sistemas:** solo Markdown: puertas de entrada, continuidad, estado, decisiones, plan de migración, checkpoints U3/U4, hoja de ruta y README. Sin runtime, web ni referencias modificados.
- **Decisiones nuevas del autor:** aprobación funcional U3 el 04/10/2026, prioridad migración U4–U6 antes de mejoras y excepción limitada para exportar progreso web. La política de importación parcialmente inválida se propone, no se considera aprobada.
- **Comprobaciones ejecutadas (04/10/2026):** auditoría Git y revisión del diff; `git diff --check -- "*.md"` correcto; `git diff --exit-code 0505b1690656d15188860157612455639820fe1f -- src` y `git diff --exit-code -- unity/Docs/Reference` sin diferencias. Resultados por consola, sin XML de pruebas nuevo. Ninguna suite de juego ejecutada; 167/167 y 17/17 son cifras históricas.
- **Commit/push:** esta unidad se publica como «Registra la aprobación de U3 y prepara el plan de U4»; consultar el hash con el comando de «Cómo retomar». Fetch previo y push normal solo si el remoto conserva el punto de partida; confirmar HEAD remoto después.
- **Árbol al entregar:** únicamente los siete ajustes Unity deliberadamente excluidos tras publicar la documentación.
- **Limitaciones:** picos U3 aislados de 62–106 ms en el minuto 5, causa no aislada; no bloquean. Presentación técnica aprobada como base; mejoras posteriores, no regresiones.
- **Pendiente/siguiente paso exacto:** esperar respuesta del autor a este plan y a la única pregunta; no implementar U4 todavía.

### 04/10/2026 — paso 1: contrato y corpus — Codex

- **Punto de partida:** local/remoto `799f28415599d24f784ade4a0312b7000a664ba8`; ahead 0 / behind 0, mismos siete ajustes locales Unity.
- **Trabajo:** contrastados esquemas históricos v1 (`5cee530`/`496f9bc`), v2 (`fde8438`) y v3 (`bba95a5`); contrato de tres formatos, 14 opciones, IDs persistentes exactos, campos obligatorios/opcionales/derivados, invariantes, errores y presupuestos de entrada. DTO definido como contrato lógico; clases C# llegarán con paso 2.
- **Corpus:** `u4-progress.json`, 8 guardados y 6 secuencias meta generados una sola vez desde la web aprobada; `u4-import-cases.json`, 94 casos contractuales según la política estricta del autor. No cambiar esperados desde C#.
- **Archivos principales:** `CONTRATO_GUARDADO_U4.md`, ambos corpus nuevos, `scripts/u4-reference.mjs`, `scripts/u4-contract.test.mjs`; documentos vigentes actualizados a implementación autorizada. Sin cambios de `src/`, runtime ni referencias anteriores.
- **Decisiones del autor:** recuperación revisable e inequívoca, crítica ambigua fatal; normalizar/informar sin IO; confirmar antes de backup/sustitución/verificación; nunca fusionar ni liquidar al importar. Registradas en `DECISIONES.md`.
- **Pruebas realmente ejecutadas (04/10/2026):** `node --test scripts/u4-contract.test.mjs`: **5/5**, 94 casos revisados estructuralmente, log `unity/TestResults/U4/contract.log`. `node scripts/u4-reference.mjs`: **8/8 guardados y 6/6 secuencias** coinciden, log `unity/TestResults/U4/reference.log`. `--export-once` se usó únicamente para la creación inicial del nuevo corpus web.
- **Referencias ejecutadas:** `node scripts/unity-reference.mjs`, `node scripts/unity-reference-u2.mjs`, `node scripts/unity-reference-u3.mjs`: **correctas**, salida en consola. Hubo avisos de puerto HMR compartido al verificarlas simultáneamente; finalizaron con código 0 y coincidencia completa. Preferir secuencial en futuras verificaciones Vite.
- **Otras verificaciones:** `git diff --check` acotado a la pieza correcto; `git diff --exit-code 0505b1690656d15188860157612455639820fe1f -- src` sin diferencias; U0/U2/U3 congeladas sin diferencias.
- **Pendientes:** tests del importador/persistencia aún no existen; las cinco pruebas actuales solo validan contrato/corpus. No se ejecutaron Unity Edit/Play/build/visual en este paso sin cambios C#.
- **Commit/push:** «Define el contrato y congela el corpus de progreso de U4», fetch previo, push normal y comprobación del HEAD remoto. Hash consultable por `git log -1 --format="%H %s" -- scripts/u4-reference.mjs`.
- **Árbol/exclusiones:** únicamente los siete ajustes Unity deliberadamente excluidos tras publicar esta pieza. Ningún código a medias.
- **Siguiente paso exacto:** paso 2, DTO y reglas meta C# puras con pruebas contra las secuencias congeladas; después persistencia. No hace falta nueva autorización.

### 04/10/2026 — paso 2: reglas meta puras y liquidación — Codex

- **Punto de partida:** `fabaf2a64f5ff6c34d3592a25a6594f066130208`, paso 1 publicado; siete ajustes Unity excluidos conservados.
- **Trabajo:** DTO C# de progreso/opciones/meta, defaults ES/EN, compras, selección desbloqueada, extras, ocho misiones, recibo y liquidación equivalentes a la web, protección de última partida/debug e instantáneas de filtros. Sin IO, sin UnityEngine en el núcleo y sin conectar todavía a la escena.
- **Archivos principales:** `Core/ProgressData.cs`, `Core/MetaRules.cs`, `Tests/Core/MetaProgressTests.cs` y sus metas, dentro de ensamblados existentes.
- **Pruebas nuevas:** 17 casos Edit Mode: seis secuencias web congeladas (cada operación, recibo y estado), defaults ES/EN, catálogo meta contra U0, selección, tres extras, filtros copiados y ofertas de armas, independencia de estados, extremos de números internos de partida y duplicación tras roundtrip del DTO. Abandono se representa como ausencia de liquidación igual que Game web; su flujo real en escena y persistencia se probará en pasos 3/6.
- **Aclaración contractual:** un contenedor `settings` mal formado es secundario y recuperable con defaults/informe, sin rechazar meta válida. Corregidos explícitamente esos dos casos en el corpus de política antes de implementar el validador; no se usaron resultados C# ni se modificó el corpus de resultados web. Coincide con la política del autor de no rechazar progreso por opciones rotas.
- **Pruebas ejecutadas el 04/10/2026, Europe/Madrid:** `scripts\u3.cmd edit`: **184/184**, XML 02:17:01–02:17:12 (17 nuevas + 167 anteriores). `scripts\u3.cmd play`: **17/17**, XML 02:18:11–02:19:16. Originales `unity/TestResults/U3/edit.xml` y `play.xml`; copias de evidencia y logs en `unity/TestResults/U4/step2-edit.xml/.log`, `step2-play.xml/.log`.
- **Build:** `scripts\u3.cmd build`, **Windows x64 Mono correcta**, `unity/Builds/U3/Mamporro-U3.exe`; log copiado a `unity/TestResults/U4/step2-build.log`. No es una build de UI U4 ni prueba manual nueva.
- **Contrato:** `node --test scripts/u4-contract.test.mjs`, **5/5** después de aclarar los dos casos de opciones; `unity/TestResults/U4/contract.log`.
- **Incidencia de entorno:** primera ejecución Edit Mode dentro del sandbox salió con 198 por acceso a licencia; relanzada con acceso al entorno local. Una pasada intermedia pasó 183/183; se añadió después la prueba de roundtrip y la final pasó 184/184.
- **Comprobaciones:** diff de web y referencias U0/U2/U3/`u4-progress.json` sin cambios; `git diff --cached --check` correcto. Referencias web verificadas en paso 1 de esta misma sesión, sin cambios posteriores de fuentes. No se ejecutó visual: no cambian escena ni render.
- **Commit/push:** «Porta las reglas meta y la liquidación de U4», fetch previo, push normal y verificación del remoto. Hash consultable por la ruta de MetaRules indicada en «Cómo retomar».
- **Estado/exclusiones:** no hay runtime a medias; solo siete ajustes Unity excluidos tras publicar. Núcleo nuevo opera sobre DTO válido de fábrica o del futuro validador: **no es un importador ni un saneador de entrada externa**.
- **Pendientes:** validación estricta, migraciones y IO reales (paso 3), exportación/importación (4–5), conexión completa de filtros/recompensas/abandono a sesión (6), menús/opciones (7–8), cierre/manual (9).
- **Siguiente paso exacto:** implementar el parser/validador puro del paso 3 según contrato, hacer pasar el corpus sin modificar esperados para acomodar el port y seguir con persistencia. No requiere nueva autorización.

### 04/10/2026 — paso 3, primera pieza: validación pura — Codex

- **Punto de partida:** `dd994b886d2e64ed4eaf2954ca7c18e5def0de58`, paso 2 publicado y verificado en remoto; sin WIP aparte de siete ajustes Unity excluidos.
- **Trabajo:** `ProgressJson.cs`, parser JSON sin dependencias, con presupuestos, UTF-8 estricto, rechazo de claves duplicadas y tokens numéricos conservados para no aceptar fracciones redondeadas como enteros críticos. Serializador canónico de árboles propios.
- **Validación:** `ProgressValidation.cs`, entradas Import/Stored separadas; web v1/v2/v3, transferencia v1 y almacenamiento Unity v1; candidato/informe en memoria, defaults opcionales informados, exclusiones inequívocas, invariantes críticas y migraciones explícitas. `ProgressTree` serializa DTO sin reflexión ni estado activo.
- **Pruebas nuevas:** `ProgressValidationTests.cs`, 94 entradas del corpus y 9 casos adicionales (103 en total): resultados/candidatos/diagnósticos contra esperados congelados, precisión de cantidades, fronteras de tamaño, escapes/UTF-8, cultura ES, original intacto, separación de formatos e idempotencia del candidato. El caso sin guardado comprueba el DTO nuevo; la existencia de archivos y la idempotencia persistente corresponden a la pieza IO pendiente.
- **Pruebas ejecutadas:** `scripts\u3.cmd edit`, **287/287**, 04/10/2026 10:47:04–10:47:15 Europe/Madrid. XML/log originales en U3; copias `unity/TestResults/U4/step3-validation-edit.xml` y `step3-validation-edit.log`.
- **Validación Git:** índice de esta pieza sin errores de whitespace. El diff global sigue mostrando whitespace en los ajustes Unity excluidos; no se limpian. No cambios en web, corpus esperados ni referencias anteriores.
- **No ejecutado de nuevo:** Play Mode/build/visual en esta pieza pura y aún no conectada; última regresión de esos dos primeros comandos es la del paso 2. El paso 3 completo necesita pruebas IO y build antes de cerrarse.
- **Commit/push:** «Valida y migra el progreso de U4 sin escribir archivos», fetch previo, push normal y verificación del remoto. Localizar hash con `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Core/ProgressValidation.cs`.
- **Estado/exclusiones:** pieza compilada y probada, solo siete ajustes Unity excluidos tras publicar. No se ha escrito ningún guardado de usuario.
- **Siguiente paso exacto:** almacenamiento con temporal/backup/sustitución/relectura y recuperación, reutilizando este validador; no marcar paso 3 terminado todavía.

### 04/10/2026 — paso 3, cierre: almacenamiento y recuperación — Codex

- **Punto de partida:** `d3927ff5cdd8d25d7c57075a7b3e3ae7e3fb982b`, parser/validador puro publicado y verificado en remoto. Se conservan los siete ajustes Unity locales.
- **Trabajo:** ensamblado `Mamporro.Persistence` sin UnityEngine; almacenamiento acotado, bloqueo entre instancias, preparación inmutable, comprobación de instantánea, confirmación, backup verificado, temporal, sustitución y relectura. Rollback al principal anterior y recuperación revisable desde backup. Repetir el mismo candidato es una operación sin nueva escritura; nunca fusiona ni liquida recompensas.
- **Archivos:** `Persistence/ProgressFiles.cs`, `ProgressStore.cs`, ensamblado/metas; `Tests/Persistence/ProgressStoreTests.cs`, `ProgressFileTests.cs`, ensamblado/metas. Garantías y límites detallados en `docs/PERSISTENCIA_U4.md`; contrato y estado actualizados.
- **Pruebas específicas:** 22 nuevas: 17 deterministas en memoria con fallos inyectados y 5 sobre archivos temporales reales de Windows. Preparación/cancelación, vista previa aislada, doble importación, cambio del principal, plan consumido, fallo de lectura/bloqueo/escritura/publicación/verificación/rollback, corrupción, backup, límites y rutas. Archivo externo intacto tras importar dos veces; principal bloqueado conserva progreso y copia. No se usó el directorio de guardado del autor.
- **Edit Mode:** `scripts\u3.cmd edit`, **309/309**, 04/10/2026 11:01:13–11:01:25 Europe/Madrid. Una pasada intermedia fue 307/307; después se añadieron dos casos de archivos reales. Evidencias `unity/TestResults/U4/step3-edit.xml` y `step3-edit.log`.
- **Play Mode:** `scripts\u3.cmd play`, **17/17**, 04/10/2026 11:05:29–11:06:34 Europe/Madrid. Evidencias `unity/TestResults/U4/step3-play.xml` y `step3-play.log`.
- **Build:** `scripts\u3.cmd build`, **Windows x64 Mono correcta**, log terminado el 04/10/2026 11:07:27 Europe/Madrid. `unity/Builds/U3/Mamporro-U3.exe`, evidencia `unity/TestResults/U4/step3-build.log`. Sin visual nuevo: no se modifica escena/render ni se conecta todavía esta capa a U3.
- **Referencias/web:** ejecutados `git diff --exit-code 0505b1690656d15188860157612455639820fe1f -- src` y `git diff --exit-code -- unity/Docs/Reference`, sin diferencias. Verificadores U0/U2/U3/U4 ya pasaron en el paso 1 de esta sesión; no se repiten aquí ni se regeneran referencias. Índice revisado con `git diff --cached --check`; el whitespace global procede de los ajustes Unity excluidos.
- **Decisiones/limitaciones:** no hay nueva decisión de diseño. Se usa `File.Replace` para sustitución y `Flush(true)` con relectura; no se promete atomicidad universal ni resistencia a corte eléctrico. Archivo inaccesible/sobredimensionado produce `unavailable`, sin reset ni sustitución. Principal recuperable/corrupto se conserva literalmente al reemplazarlo; no pisa backup válido. Los temporales huérfanos nunca se promueven. UI de recuperación y conexión a `Application.persistentDataPath` pendientes de aplicación.
- **Commit/push:** «Guarda el progreso de U4 con copia y recuperación». Fetch previo; publicar solo si el remoto sigue en `d3927ff`, push normal y comprobación del HEAD remoto. Hash consultable por la ruta de `ProgressStore.cs` en «Cómo retomar».
- **Árbol/exclusiones:** tras publicar, solo los siete ajustes Unity deliberadamente excluidos. Sin WIP de runtime pendiente. Logs/builds son evidencia local ignorada por Git.
- **Siguiente paso exacto:** paso 4, exportación web explícita validada, pruebas web y equivalencia sin mutar el progreso. Pasos 1–3 cerrados; U4 aún no completo. No iniciar U5 ni mejoras posteriores.

### 04/10/2026 — paso 4: exportación web — Codex (implementación) y Claude Code (verificación y publicación)

- **Punto de partida:** `777bba85e648d2d803f64d0a86fad3ae512abe15`, local y remoto iguales (ahead 0 / behind 0). Codex implementó el paso 4 y agotó su sesión sin commit, push ni entrada en este checkpoint. Claude Code lo encontró como WIP local el 04/10/2026, lo auditó sin pull/stash/reset/clean y lo conservó íntegro, junto con los siete ajustes Unity excluidos.
- **Trabajo de Codex (WIP recuperado):** `src/save/exportProgress.ts` (instantánea acotada sin getters, validación estricta según el contrato, opciones con default e informe sin clamp, exclusión de IDs desconocidos/duplicados, críticos fatales, migración v1/v2 explícita, `parseSave` solo como comprobación, serialización canónica y relectura), `src/ui/ProgressExport.ts` (panel en Opciones del menú principal, vista previa con informe, descarga Blob `mamporro-progreso.json`, aviso de copia en memoria si no hay almacenamiento), lector en `Game.ts`, colocación en `UI.ts`, 17 textos ES/EN, `exportProgress.test.ts` (30 pruebas), `scripts/u4-export-fixtures.mjs` (entradas reales, nunca esperados), `WebExportTests.cs` (8 casos contra `ProgressValidator.Import` y `u4-progress.json`), preparación automática en `scripts/u3.ps1 edit`, `scripts/verify-historical-reference.ps1`, `scripts/verify-u4-export-browser.cjs`, `docs/EXPORTACION_U4.md` y secciones de README raíz y de Unity.
- **Revisión de Claude Code:** código contrastado con el contrato y con la política del autor; sin cambios necesarios. Diff web frente a `0505b16` limitado a `Game.ts` (1 línea), `UI.ts` (3), `es.ts`/`en.ts` (17 textos cada uno) y los tres archivos nuevos; sin cambios en combate, RNG, balance, meta, precios, misiones, mundo, armas, enemigos ni economía. Referencias congeladas y corpus U4 intactos.
- **Pruebas ejecutadas por Claude Code (04/10/2026, Europe/Madrid):** `npm.cmd run typecheck` correcto; `npm.cmd test` **232/232** (11:51); `npm.cmd run build` correcto; `node --test scripts\u4-contract.test.mjs` **5/5**; `powershell -File scripts\verify-historical-reference.ps1` correcto en la instantánea `qa-results/reference-35e9ea57b12f4333a727a0d38bee9145` (U0, U2, U3 y U4: 8 guardados y 6 secuencias coinciden, 11:52); `scripts\u3.cmd edit` **317/317** (309 + 8 `WebExportTests`, 11:53, incluye `u4-export-fixtures.mjs`: 8 transferencias verificadas); `scripts\u3.cmd play` **17/17** (11:54). Evidencias `unity/TestResults/U4/step4-claude-edit.xml/.log` y `step4-claude-play.xml/.log`.
- **Navegador real (Claude Code, 11:55):** Vite en `127.0.0.1:5173` y `node scripts\verify-u4-export-browser.cjs` con Edge 154 instalado y Playwright externo en `%LOCALAPPDATA%\MamporroQA`: ES y EN, pulsar exportar, evento de descarga real, `mamporro-progreso.json` guardado y releído, `mamporro.progress` v1, contenido exacto, dos descargas con bytes idénticos, estado y `localStorage` sin cambios, exportación desde memoria con almacenamiento bloqueado, fallo de Blob avisado y progreso crítico inválido sin botón de descarga. Los archivos descargados (`es-0.json`, `en-0.json`) son **idénticos byte a byte** a `Exports/v3-partial.json` y `Exports/v3-all.json`, que `WebExportTests` acepta con `ProgressValidator.Import`. Evidencias `unity/TestResults/U4/BrowserExport/` (JSON, capturas ES/EN y `report.json`).
- **No ejecutado:** build Unity y visual (sin cambios de escena ni runtime Unity; solo una prueba Edit Mode nueva).
- **Decisiones nuevas:** ninguna.
- **Observaciones:** cada ejecución del verificador histórico crea un worktree desacoplado nuevo en `qa-results/` (ignorado por Git); quedan dos (`reference-850a…` de Codex y `reference-35e9…`). No se eliminan automáticamente; pueden retirarse con `git worktree remove` cuando ya no hagan falta. Los mensajes de descarga dicen «solicitada»: el navegador no permite confirmar que el usuario guardó el archivo.
- **Commit/push:** «Exporta el progreso web para Unity»; fetch previo y push normal si el remoto sigue en `777bba8`.
- **Árbol al terminar:** solo los siete ajustes Unity excluidos.
- **Siguiente paso exacto:** paso 5 (ver «Cómo retomar»).

## Checkpoint al terminar U4

Pendiente: U4 todavía no está completo. Completar al cerrar con commits, pruebas exactas/fecha/ruta, build, instrucciones manuales, límites y aprobación del autor. No marcarlo como verificado por haber aprobado U3.
