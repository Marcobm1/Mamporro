# Checkpoint U4 — meta, UI, opciones y traslado de guardados

Estado: **PLANIFICACIÓN AUTORIZADA el 04/10/2026. IMPLEMENTACIÓN NO INICIADA.** Esperar la siguiente respuesta del autor antes de programar U4. U3 aprobada manualmente como base funcional el mismo día; último cierre técnico previo: `83246ebfd873b0c5d23611a1d557f83096180217`.

## Cómo retomar

- **Último punto técnico publicado:** `83246eb` (U3). Este plan y su aprobación manual se publican en el commit «Registra la aprobación de U3 y prepara el plan de U4»; localizar su hash con `git log -1 --format="%H %s" -- docs/PROGRESO_U4.md`.
- **Paso actual:** 0, auditoría y planificación. No hay runtime U4 implementado.
- **Terminado:** auditoría Git; lectura del guardado, migraciones, meta, tienda, misiones, opciones, i18n y selección web; inventario Unity reutilizable; cierre documental de U3 y plan siguiente.
- **A medias:** ninguna pieza de código. Pendiente de respuesta del autor: autorización de implementación y política de importación parcialmente inválida descrita abajo.
- **Sin commit a propósito:** los siete ajustes Unity listados en «Cambios locales excluidos». No restaurar ni publicar.
- **Siguiente paso exacto:** leer la respuesta del autor y registrar la política de importación. Solo entonces comenzar el paso 1. Auditar Git de nuevo y conservar cualquier WIP nuevo antes de editar.
- **No confundir pruebas históricas con nuevas:** U3 registró 167/167 Edit Mode y 17/17 Play Mode. Esta sesión documental no ha ejecutado Unity, build, navegador ni pruebas web.

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

## Única duda de comportamiento previa a implementación

Para un JSON de versión admitida que mezcle progreso válido con tipos/valores inválidos o IDs desconocidos: **¿rechazar la importación entera, o permitir recuperar lo compatible tras mostrar cada corrección/exclusión y pedir confirmación explícita?**

Recomendación: recuperación revisable y explícita; cancelar mantiene el guardado Unity intacto. Nunca importar silenciosamente el saneamiento de la web. JSON ilegible o versión futura se rechazan, conservando el archivo original; sin inventar recompensas para datos ambiguos. La elección determina validación, vista previa y pruebas. No hay más dudas de diseño bloqueantes identificadas.

## Plan propuesto por pasos/commits verificables

Cada fila es una unidad coherente; dividir si tamaño o pruebas lo exigen. No implementada todavía.

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

## Exportación/importación propuesta, sin formato definitivo aún

Los datos reales revisados permiten diseñar el contrato en el paso 1. Propuesta: transporte JSON identificable y versionado que contenga el progreso compatible; conservar versiones de origen y separar validación del transporte, migración de save y reglas meta. Admitir casos v1/v2/v3 mediante adaptadores explícitos; concretar si el lector recibe también sus JSON originales además del exportado. No confiar solo en una extensión de archivo ni usar deserialización que convierta tipos incorrectos silenciosamente.

Exportar una instantánea coherente del progreso web; no tocar el archivo descargado después. Importar como sustitución validada, nunca como suma. Respaldar el guardado Unity existente antes de confirmar/escribir; si falla la copia o escritura, abortar conservando el anterior y explicar cómo recuperar. Repetir el mismo archivo no concede moneda ni desbloqueos extra. La UI indica consecuencias y cancelación. No guardar una partida activa como parte de la transferencia.

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

## Checkpoint al terminar U4

Pendiente: U4 no se ha implementado. Completar al cerrar con commits, pruebas exactas/fecha/ruta, build, instrucciones manuales, límites y aprobación del autor. No marcarlo como verificado por haber aprobado U3.
