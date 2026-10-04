# Checkpoint U4 — meta, UI, opciones y traslado de guardados

Estado: **IMPLEMENTACIÓN AUTORIZADA Y EN CURSO desde el 04/10/2026.** Política de recuperación revisable y estricta aprobada por el autor. Contrato en [CONTRATO_GUARDADO_U4](CONTRATO_GUARDADO_U4.md). U3 aprobada manualmente como base funcional el mismo día; último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`.

## Cómo retomar

- **Paso 1 publicado:** `fabaf2a64f5ff6c34d3592a25a6594f066130208`. Paso 2 en «Porta las reglas meta y la liquidación de U4»; localizar hash con `git log -1 --format="%H %s" -- unity/Assets/Mamporro/Core/MetaRules.cs`.
- **Paso actual:** 3, validación/migración estricta y persistencia Unity; no iniciado.
- **Terminado:** pasos 1–2: contrato/corpus y DTO/reglas meta C# puras sobre estado válido. Edit Mode 184/184, Play Mode 17/17 y build Windows x64 Mono correctos en esta sesión. Ver registro para comandos, fecha y rutas.
- **A medias:** ninguna pieza de runtime. No existe aún exportador, importador ni persistencia Unity U4.
- **Sin commit a propósito:** siete ajustes Unity de «Cambios locales excluidos».
- **Siguiente paso exacto:** paso 3: parser acotado y validador/migraciones puros que ejecuten los 94 casos contractuales; después servicio de archivo temporal/backup/sustitución/verificación con tests IO separados. No deserializar entrada externa directamente con JsonUtility ni reutilizar los clamps de números de partida para importación.
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

Plan aprobado. Cada fila es una unidad coherente; dividir si tamaño o pruebas lo exigen. Pasos 1–2 terminados; pasos 3–9 pendientes.

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

## Checkpoint al terminar U4

Pendiente: U4 no se ha implementado. Completar al cerrar con commits, pruebas exactas/fecha/ruta, build, instrucciones manuales, límites y aprobación del autor. No marcarlo como verificado por haber aprobado U3.
