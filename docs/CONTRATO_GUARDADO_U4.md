# Contrato de progreso U4 — versión 1

04/10/2026. Implementación U4 autorizada por el autor. Política: recuperación revisable **solo si es inequívoca**, normalización e informe en memoria antes de cualquier escritura. Este contrato concreta el paso 1; no afirma que el exportador, importador o persistencia ya existan.

## Procedencia y tres formatos distintos

Base funcional: `0505b1690656d15188860157612455639820fe1f`. Fuentes: `src/save/schema.ts`, `src/save/SaveManager.ts`, `src/systems/meta.ts`, `src/data/meta.ts`, catálogos y `Game.ts`.

Historial comprobado con Git: v1 en `5cee530` (opciones) y `496f9bc` (añade duración sin cambiar versión); v2 en `fde8438` (meta y opciones de audio); v3 en `bba95a5` (partículas reducidas, sacudida, destellos). Las opciones ausentes tienen valores por defecto explícitos; v1 nunca tuvo meta.

| Formato | Identificación | Contenido |
|---|---|---|
| Interno web | `version: 1/2/3`, clave localStorage `mamporro.save` | `settings`; desde v2 también `meta`. Sin partida activa. |
| Transferencia | `format: "mamporro.progress"`, `version: 1` | `source: { platform: "web", saveVersion: 1/2/3 }`, `progress: { settings, meta }` **ya normalizado al contrato 1**, no JSON crudo de localStorage. |
| Persistente Unity | `format: "mamporro.unity-save"`, `version: 1` | `progress: { settings, meta }` normalizado. No envolver el save web ni depender de su versión. |

`format` y `version` son obligatorios en los formatos nuevos; `source` también en transferencia. `source.saveVersion` es procedencia, **no orden de migrar de nuevo `progress`**. No incluir fecha aleatoria, identidad de instalación ni recompensas pendientes: el mismo progreso genera la misma representación canónica persistente.

Unity admite transferencia v1 y un adaptador explícito de importación heredada para los JSON web v1/v2/v3. Detectar primero `format`: si existe y es desconocido/inválido, rechazar; nunca reinterpretar un formato desconocido como web. Sin `format`, exigir versión web numérica entera admitida. El fichero de persistencia Unity solo lo abre su servicio de almacenamiento, no el adaptador web. No hay migraciones de Unity v1 ni transferencia v1 a versiones aún inexistentes; versiones superiores se rechazan.

## DTO normalizado compartido

Contrato lógico C# (sin dependencia UnityEngine ni IO): `ProgressDto { SettingsDto settings; MetaDto meta; }`. DTO es un **resultado validado**, no una garantía de validez obtenida por deserialización permisiva. El parser posterior debe distinguir campos ausentes, null, tipos, enteros y claves duplicadas antes de construirlo.

`SettingsDto` contiene los 14 campos de la tabla siguiente; doubles para volúmenes/sensibilidad, enteros para resolución/duración, booleanos y cadena de idioma. `MetaDto`: `int coins`, `string selected`, arrays de strings `characters/weapons/items/completed`, `ExtraLevelsDto extras` (3 enteros), `MissionProgressDto missions` (8 enteros), `string lastRun`. Los nombres JSON coinciden con la web para facilitar revisión, pero los contenedores y versiones no se comparten.

Todas las propiedades existen y son no nulas después de normalizar. Colecciones son conjuntos sin duplicados, en el orden canónico de los IDs de abajo. Las operaciones de juego pueden añadir al final; canonicalizar al guardar no modifica pertenencia ni concede progreso. Moneda, contadores y extras se validan antes de convertir a `int`; cálculos de economía posteriores usan intermediarios suficientemente amplios.

Informe: `ValidationResult { candidate: ProgressDto|null, issues[], canConfirm }`; incidencias con `path` (JSON Pointer), `code`, `severity` (`fatal`/`recovery`/`info`), acción (`preserve`/`exclude`/`default`/`migrate`/`reject`) y explicación localizable con valores anterior/posterior acotados. La vista previa enumera también los campos conservados. Un fatal deja `candidate=null`, `canConfirm=false`; ninguna escritura. Nunca mostrar texto externo como marcado ejecutable.

## Opciones: dominio, valores por defecto y presencia

| Campo | Dominio | Defecto | Introducido |
|---|---|---|---|
| `language` | `es`, `en` | idioma de contexto admitido | v1 |
| `mouseSensitivity` | número finito [0.2, 3] | 1 | v1 |
| `renderHeight` | 240, 360, 480 | 360 | v1 |
| `vertexSnap` | booleano | true | v1 |
| `dithering` | booleano | true | v1 |
| `slideWithCtrl` | booleano | false | v1 |
| `showFps` | booleano | false | v1 |
| `runMinutes` | 5, 10, 15 | 10 | v1 tardía |
| `musicVolume` | número finito [0, 1] | 0.5 | v2 |
| `effectsVolume` | número finito [0, 1] | 0.7 | v2 |
| `muted` | booleano | false | v2 |
| `reducedParticles` | booleano | false | v3 |
| `cameraShake` | booleano | true | v3 |
| `flashes` | booleano | true | v3 |

Opciones ausentes/incorrectas recuperables con defecto e informe por campo; no clamp de importación (por ejemplo sensibilidad 99 → 1, no 3). Esto difiere deliberadamente del saneamiento permisivo web y protege la transferencia sin modificar el juego web. Al crear/exportar se pasa el idioma actual válido de la aplicación; al importar, el idioma Unity actual válido. Conservar un `language` válido del archivo. El contexto explícito forma parte de cada caso de prueba, nunca depende del sistema donde corre el test.

Contenedor `settings` ausente o que no sea objeto: todos sus campos opcionales se completan con defaults y se informa del contenedor sustituido. La meta válida no se rechaza por opciones rotas; no hay datos de propiedad ni moneda dentro de `settings`. Un ajuste de v2/v3 presente en v1 se conserva si es válido, igual que `parseSave` de la base.

## IDs persistentes exactos y campos derivados

- Personajes/selección: `remedios`, `baguette`; inicial `remedios`.
- Armas (orden de catálogo): `chancla`, `naftalina`, `barra`, `dentaduras`, `jersey`, `fregona`. Iniciales: las cuatro primeras.
- Objetos (orden de catálogo): `gafas`, `zapatillas`, `termo`, `cojin`, `lupa`, `loteria`, `rulos`, `perlas`, `monedero`, `baraja`, `olla`, `bata`. Iniciales: `gafas`, `zapatillas`, `termo`, `cojin`, `lupa`, `loteria`, `rulos`, `monedero`.
- Extras: `rerolls`, `skips`, `banishes`.
- Misiones/completadas: `first`, `kills`, `chests`, `shrines`, `challenge`, `level`, `victory`, `noLife`.
- IDs de compra: `baguette`, `jersey`, `baraja`, `olla`; no se guardan como historial: la propiedad se deriva de sus desbloqueos. Niveles extra sí persisten.
- `lastRun`: identidad opaca de la última partida liquidada, no ID de catálogo. Cadena de 0–100 unidades UTF-16 (límite real del saneamiento web); vacío válido para progreso nuevo. No exigir UUID: las reglas admiten otras identidades.
- Tomos `damage`, `attackSpeed`, `projectiles`, `area`, `moveSpeed`, `vitality`, `luck`, `magnet` no son desbloqueos persistentes. No guardar inventario, seed, duración activa, HP, XP, oro de partida, enemigos ni director.
- Derivados: propiedad de compras, usos iniciales `2 + extras`, filtros de catálogo, textos y recibo de resultados. No persistir datos redundantes que puedan contradecir el progreso.

## Progreso: campos obligatorios e invariantes

V2/v3 web y formatos nuevos requieren `meta` objeto y **todos** sus campos, incluidas las claves de `extras/missions`. Ausencia o tipo incorrecto de un campo crítico es fatal: la web lo reinicia permisivamente, pero no prueba que el usuario tuviera cero progreso. V1 **sin** `meta` tiene migración explícita a meta inicial; v1 con `meta` es contradictorio y fatal (no borrarlo como hace la migración permisiva).

| Campo | Dominio/invariante demostrable |
|---|---|
| `coins` | Entero finito [0, 1 000 000 000], límite del almacenamiento/saneamiento y liquidación web. Importación fuera de rango fatal, nunca saturar. |
| `extras.*` | Enteros 0–3; tres precios reales 80/140/220, máximo 5 usos iniciales contando 2 base. |
| `missions.*` | Enteros entre 0 y objetivo de su misión. |
| `completed` | IDs conocidos significan recompensa ya liquidada; **no pagar nada al importar**. Contador igual al objetivo si y solo si figura completada. Contradicciones fatales, no decidir si se cobró. |
| Desbloqueos | Arrays de strings; incluir contenido inicial; incluir recompensa de misión completada. Ausencia de requisito o recompensa coherente es fatal, no fabricar la compra/desbloqueo. |
| `selected` | ID conocido y presente en `characters`; contradicción fatal, no elegir arbitrariamente. |
| `lastRun` | Cadena obligatoria en v2/v3 y formatos nuevos, no truncar en importación. |

No inferir historial de compras/saldo a partir de desbloqueos: el propio save no contiene ese historial. Tampoco inferir misiones cobradas a partir de un arma poseída. Verificar implicaciones conocidas de misiones completadas, sin inventar la implicación inversa.

| Misión | Objetivo | Premio Calderilla | Desbloqueo |
|---|---:|---:|---|
| `first` | 1 | 30 | — |
| `kills` | 1000 | 60 | arma `fregona` |
| `chests` | 10 | 40 | — |
| `shrines` | 3 | 40 | — |
| `challenge` | 1 | 50 | objeto `perlas` |
| `level` | 20 | 50 | — |
| `victory` | 1 | 80 | — |
| `noLife` | 1 | 100 | objeto `bata` |

Tienda: Baguette 220, Jersey 140, Baraja 160, Olla 180. Liquidación: `floor(kills/20) + min(60, floor(time/15)) + (victoria ? 60 : 0) + misiones nuevas`; operaciones exactas de `settleRun`, sin cambiar balance. Misiones acumulativas salvo nivel máximo; `noLife` exige victoria sin tomo de vida. Abandono no llama a liquidación; debug no cambia meta ni `lastRun`. Repetir la última liquidación no paga; el orquestador debe impedir callbacks tardíos/repetidos de partidas anteriores. Importación nunca llama a esa función.

## Errores, migración y límites defensivos

**Fatales:** JSON ilegible, raíz no objeto, versión ausente/no numérica/no entera/desconocida, formato incompatible, campos críticos ausentes/null/tipo erróneo/fuera de rango, claves JSON duplicadas, inconsistencias anteriores, límites de entrada excedidos o cualquier interpretación ambigua. No aceptar NaN/Infinity ni conversiones desde strings. Un archivo vacío no es «partida nueva»; ausencia de guardado local sí lo es.

**Recuperables con informe:** opciones ausentes/inválidas; IDs string desconocidos en colecciones extensibles (excluir solo esos IDs); duplicados de IDs de conjuntos (eliminar duplicado sin cambiar pertenencia); propiedades adicionales (excluir/reportar); migraciones explícitas. Un elemento no string en una colección de IDs es fatal: no adivinar cuál era. IDs desconocidos de selección no son una colección extensible: fatal. Campos desconocidos de `missions/extras` se excluyen y reportan, manteniendo obligatorias las claves conocidas.

Migraciones: web v1 → meta inicial + opciones anteriores conservadas; v2 → meta conservada exactamente + tres opciones v3 por defecto; v3 → sin migración de progreso. Opciones anteriores ausentes se completan según sus defaults documentados. Las reparaciones arbitrarias de `sanitizeMeta` **no** son migraciones autorizadas. Transferencia v1 lleva ya el DTO completo: no aplicar de nuevo migración web por `source.saveVersion`.

Límites defensivos de entrada (no reglas de juego): **256 KiB** de UTF-8 antes de parsear; profundidad **16**; **4096** nodos/valores; máximo **128** propiedades por objeto y **128** elementos por array; cadenas/nombres de propiedad **1024** unidades UTF-16, IDs conocidos/desconocidos **64**. Rechazar excedidos; no recortar ni asignar según un número del archivo. Son márgenes amplios frente a 2/6/12 desbloqueos, 8 misiones y archivos legítimos de pocos KiB. Validar también ramas desconocidas y limitar el informe a esos presupuestos. Rechazar claves duplicadas incluso si sus valores coinciden, números no finitos al convertir y UTF-8 inválido; admitir BOM UTF-8 al inicio de archivo. Las pruebas de frontera se generarán por recetas pequeñas, sin versionar archivos enormes.

## Exportación, confirmación y escritura (contrato para pasos posteriores)

Exportación explícita: leer una instantánea del save existente, validar con política estricta, aplicar migraciones demostradas en memoria y reglas de opciones; usar `parseSave` únicamente sobre un candidato ya validado, nunca aceptar su `reset` ni sus clamps como recuperación de progreso. Informar reparaciones antes de descargar. No llamar a SaveManager si su construcción puede persistir migraciones/reset; usar las funciones puras. Si no existe guardado y la aplicación tiene progreso válido en memoria, exportar ese snapshot identificándolo; error de lectura no equivale a ausencia. No modificar localStorage, backup, progreso, partida ni recompensas al exportar.

Importación: `leer → parsear → validar versión → migrar → normalizar → informe → confirmar → backup → escribir → verificar`. Solo menú sin partida activa; confirmar el candidato exacto mostrado. Si cambió el guardado local desde la vista previa, invalidarla y volver a confirmar; nunca sustituir una versión distinta sin avisar.

Estrategia a implementar/probar en paso 3: temporal en el mismo directorio, cerrar/flush, releer/validar candidato; después de confirmación copiar el último guardado válido a backup y verificar copia, sustituir con la operación adecuada del sistema, releer/validar archivo final y solo entonces publicar éxito/estado en memoria. Ante fallo, conservar o recuperar el último válido; no sobrescribir un backup válido con datos corruptos. Inicio examina principal/backup/temporal con prioridades documentadas, sin promover un temporal que nunca fue confirmado. No prometer atomicidad ni durabilidad ante corte eléctrico sin verificar las garantías reales de APIs/volumen Windows. Pruebas IO separadas de lógica pura.

## Corpus y verificaciones del paso 1

- `unity/Docs/Reference/u4-progress.json`: saves y secuencias de meta calculados **exclusivamente por la web aprobada**, con base y hashes. `node scripts/u4-reference.mjs` compara; `--export-once` solo permite crear un fichero inexistente. No recalcular desde C# ni cambiar esperados por fallos del port.
- `unity/Docs/Reference/u4-import-cases.json`: corpus contractual independiente, con clasificación y cambios esperados según la política del autor. Reutiliza candidatos del corpus web; datos extremos mediante recetas acotadas. No atribuir estos rechazos estrictos al saneamiento web.
- `node --test scripts/u4-contract.test.mjs`: verifica integridad, invariantes y cobertura del contrato/corpus. **No prueba un importador aún inexistente.** Paso 2 consumirá los resultados meta en C#; pasos 3–5 ejecutarán todos los casos contra los validadores reales y ampliarán fallos IO/confirmación/repetición.
- U0/U2/U3 permanecen intactas. Tras la futura excepción de exportación web, ejecutar verificadores guardados sobre una instantánea aislada de la base aprobada, sin relajar sus guardas.
