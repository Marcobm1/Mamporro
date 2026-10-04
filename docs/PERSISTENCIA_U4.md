# Persistencia local de U4

Implementada el 04/10/2026 en `Assets/Mamporro/Persistence`. Este servicio no depende de escenas ni de UnityEngine. La aplicación pasa `Path.Combine(Application.persistentDataPath, "Progress")` (sustituible con `U3Game.SaveDirectoryOverride` en pruebas o `-u4-save-dir` en la build). Desde el paso 5 la importación (`ProgressImport.cs`) lo usa desde la pantalla de inicio; la partida aún no lo lee (paso 6). Las pruebas escriben exclusivamente en directorios temporales propios, nunca en el progreso del autor.

## Archivos y lectura

- `progress.json`: formato `mamporro.unity-save`, versión 1.
- `progress.backup.json`: anterior principal válido, conservado antes de sustituirlo.
- `progress.lock`: exclusión mediante apertura con `FileShare.None`; coordina instancias que usan el servicio.
- `progress-<guid>.tmp`: temporal exclusivo de una operación. Nunca se promueve al arrancar.
- `progress.corrupt.<guid>.json`: original conservado literalmente cuando el principal necesita recuperación o es inválido; no se elimina automáticamente.

`Load` devuelve `loaded` si el principal es válido sin recuperación, `review` si requiere normalización revisable, `backup` si falta/es inválido el principal pero la copia es recuperable, `new` si no existe ninguno, `invalid` si no hay candidato válido, o `unavailable` ante fallo de acceso/IO. Leer puede crear directorio y archivo de bloqueo, pero no escribe progreso. Nunca convierte corrupción o acceso denegado en progreso nuevo.

El principal tiene prioridad. Los estados `review` y `backup` requieren presentar el informe y confirmar la recuperación; leer no sustituye archivos. Un principal que excede el límite de 256 KiB, no puede leerse o es un directorio produce `unavailable`: no se intenta sobreescribirlo ni recuperar automáticamente en ese caso. Debe resolverse el acceso/tamaño antes de una operación de recuperación. Los temporales huérfanos quedan ignorados; no se hace limpieza global.

## Preparación y confirmación

1. Validar/migrar la entrada mediante `ProgressValidator.Import`, sin IO. Los errores fatales no producen candidato. La UI de importación y su informe pertenecen al paso 5.
2. Cargar una instantánea del almacenamiento. Preparar el candidato completo con `Prepare`: valida el formato Unity y congela su serialización canónica. Modificar el DTO original o una copia de la vista previa no cambia lo preparado.
3. Confirmar explícitamente con `Confirm`. Cancelar no escribe. El plan pertenece a una instancia y solo puede consumirse una vez.
4. Adquirir bloqueo y comparar los bytes actuales con la instantánea mostrada. Si han cambiado, devolver `storage.stale-preview` y exigir nueva revisión; nunca fusionar estados.
5. Si los bytes ya coinciden con el candidato, terminar sin nueva escritura ni rotación de backup. Importar dos veces no concede saldo, extras, desbloqueos ni recompensas adicionales.
6. Si el principal era válido, copiarlo a un temporal, verificarlo, publicar el backup y verificarlo. Si era inválido o requería recuperación, conservar el original en otro archivo verificado sin sustituir un backup válido.
7. Comprobar otra vez el principal, escribir un temporal nuevo, cerrar con `Flush(true)`, releer y validar. Sustituir el principal con `File.Replace`; si no existe, usar `File.Move`. Releer, comparar bytes y validar antes de devolver éxito y nuevo estado.

El servicio recibe candidatos completos. No liquida partidas, paga misiones, suma saldos ni abre/modifica el archivo externo importado.

## Fallos y garantías reales

Un fallo antes de sustituir el principal deja el anterior intacto. Tras un intento de sustitución fallido o una relectura inválida se intenta restaurar el principal original mediante otro temporal verificado. Si también falla esa restauración, se informa `storage.recovery-required`; la copia válida permite recuperarlo después. No se anuncia éxito mientras haya fallado la verificación.

En una primera escritura sin principal anterior no hay archivo anterior que restaurar: si falla después de publicar, puede quedar un principal que deberá validarse al volver a cargar. Un backup existente se conserva. No se promueve un temporal como prueba de confirmación previa.

Los archivos se crean en el mismo directorio. No existe fallback que borre el principal y luego lo renombre: si la sustitución no está soportada, se informa el error. `Flush(true)` solicita vaciar los buffers y se verifican relecturas; esto **no demuestra durabilidad ante corte eléctrico ni atomicidad universal** en cualquier volumen, controlador o hardware. El bloqueo no impide escrituras externas que ignoren el protocolo. No se garantiza recuperación frente a daño simultáneo de principal y backup.

APIs contrastadas: [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=netframework-4.8), [FileStream.Flush](https://learn.microsoft.com/en-us/dotNet/API/system.io.filestream.flush?view=net-7.0) y [Application.persistentDataPath](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-persistentDataPath.html). Usar la ruta de Unity, no codificar una ruta de usuario de Windows.

## Pruebas

`ProgressStoreTests`: 17 casos con archivos en memoria e inyección determinista de fallos. Cubren preparación/cancelación sin escritura, aislamiento de vista previa, importación repetida, instantánea obsoleta, bloqueo, temporales parciales/corruptos, fallos de publicación, rollback y fallo del propio rollback, recuperación con confirmación y rechazo de reinicio silencioso.

`ProgressFileTests`: 5 casos sobre archivos reales en un directorio temporal único. Cubren guardar/cargar/backup/recuperación, exclusión y límite de lectura, rutas y creación exclusiva, importar dos veces conservando original y backup, y principal bloqueado por otra apertura. La limpieza comprueba directorio absoluto y prefijo antes de eliminar exclusivamente el directorio de esa prueba.

Resultados, comandos, fechas y rutas de evidencia en [PROGRESO_U4](PROGRESO_U4.md). La integración con aplicación/menús y la prueba manual final de U4 siguen pendientes.
