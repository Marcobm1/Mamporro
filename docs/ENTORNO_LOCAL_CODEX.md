# Checkpoint del entorno local de MAMPORRO

Fecha: **29/09/2026**. Preparación del entorno dentro de U0. **U1 no ha
comenzado ni está aprobado**. No hay proyecto Unity abrible: no se han creado
`Assets/`, `Packages/` ni `ProjectSettings/`.

## Repositorio y punto de partida

- Raíz real: `C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git`.
- Rama autorizada: `claude/zen-pasteur-674ik0`.
- Remoto `origin`: `https://github.com/Marcobm1/Mamporro.git`.
- Commit de partida de este bloque: `3d32fd2ca0638420d3de0246221176f7c7ed1e05`.
- Al comenzar, árbol limpio y rama remota comprobada con `git ls-remote` en ese
  mismo commit. Conservar cambios ajenos; sin force-push ni PR.
- Base web aprobada, distinta del commit de partida documental:
  `0505b1690656d15188860157612455639820fe1f`.

Codex local ha arrancado y ha leído el repositorio: `CLAUDE.md`, especificación,
decisiones, README, plan de migración, README de Unity, referencia U0 y hoja de
ruta. Se incorpora `AGENTS.md` como entrada a esos documentos y este checkpoint.
Las decisiones posteriores prevalecen; los pendientes históricos de instalación
de Codex y localización del Editor quedan sustituidos por esta comprobación.
No se han leído ni registrado credenciales, tokens o archivos de autenticación.

## Verificado: herramientas y rutas

Versiones obtenidas ejecutando únicamente sus consultas de versión:

| Herramienta | Versión | Ruta detectada |
| --- | --- | --- |
| Git | `2.54.0.windows.1` | `C:\Program Files\Git\cmd\git.exe` |
| Node.js | `24.21.0` | `C:\Program Files\nodejs\node.exe` |
| npm | `11.19.0` | `C:\Program Files\nodejs\npm.cmd` |
| Codex CLI | `0.158.0` | `C:\Users\bymar\AppData\Roaming\npm\codex.cmd` |

Editor localizado sin arrancarlo:

`C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`

- Unity **6000.6.3f1**, confirmado por captura y metadatos locales.
- `ProductVersion`: `6000.6.3f1_45d8eee7de74`.
- `FileVersion`: `6000.6.3.4577518`; fabricante Unity Technologies.
- En la raíz de esa instalación existen `metadata.hub.json` y `modules.json`.
  Se inspeccionaron junto a `Editor\Data\PlaybackEngines`.

## Verificado: soporte y herramientas de compilación

- **Windows Mono detectado:** en
  `Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations` existen
  variantes x86, x64 y ARM64, de desarrollo y normales, con `WindowsPlayer.exe`,
  `UnityPlayer.dll` y `MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll`.
- **Windows IL2CPP ausente en las variantes de esta instalación.** La entrada
  `windows-il2cpp` de `modules.json` es catálogo y muestra `selected: false`;
  aparecer en ese catálogo no demuestra instalación. Existen las herramientas
  generales `Editor\Data\il2cpp\build\deploy\il2cpp.exe` y
  `Editor\Data\il2cpp\libil2cpp`, pero no acreditan soporte Windows IL2CPP.
- Existe `WebGLSupport`; no se ha validado su integridad.
- **Herramientas C++ y Windows SDK no detectados.** No existen las rutas
  habituales de Visual Studio en `C:\Program Files` y `C:\Program Files (x86)`,
  `C:\BuildTools`, ni Windows Kits en ambas carpetas de programas.
  Tampoco se encontró
  `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`.
- Se consultaron las claves de Windows Kits/Installed Roots, VisualStudio/SxS/VS7
  y Microsoft SDKs/Windows en HKLM, incluidas las vistas WOW6432Node, y Windows
  Kits en HKCU: ausentes. En los registros de programas instalados de HKLM/HKCU
  no se detectaron Build Tools ni SDK; sí Visual Studio Code y redistribuibles
  de Visual C++. Estos últimos no acreditan un compilador.
- `cl`, `msbuild`, `clang`, `gcc`, `g++` y `rc` no se encontraron en el `PATH`.

**Límites:** búsqueda en rutas habituales, PATH y registros indicados; no se
recorrieron todos los discos. Una instalación portable o personalizada sin
registrar no es verificable con estas consultas. No se probó la integridad ni
el funcionamiento completo del Editor o de sus módulos. No hubo bloqueos del
aislamiento. PowerShell no pudo analizar `modules.json` por las claves
`preSelected`/`preselected`; se leyó correctamente en memoria con Node, sin editarlo.

## Pendiente

- Arranque real del Editor y licencia operativa.
- Importación/resolución de paquetes y comprobación de su versión efectiva.
- Creación, apertura y ejecución de escenas; compilación C# y build Windows.
- Validación visual, controles, audio y rendimiento en el equipo del autor.

La captura de Hub, los metadatos y los archivos presentes no acreditan estas
operaciones. En esta sesión no se han ejecutado pruebas de navegador ni Unity.

## Propuesta para U1, aún no aprobada

**Windows x64 Mono, URP 17.6 e Input System 1.20.0.** Es una propuesta documental,
no una configuración instalada ni autorización para empezar U1. La revisión
exacta de URP queda pendiente de comprobar; no se fija `17.6.x` en un manifiesto.

Fuentes oficiales consultadas el **29/09/2026**:

- [URP para Unity 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.render-pipelines.universal.html):
  enlaza la familia 17.6 y señala que el paquete está ligado a la versión del Editor.
- [Input System para Unity 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html):
  publica 1.20.0 para esta versión del Editor.
- [Requisitos Windows](https://docs.unity3d.com/6000.6/Documentation/Manual/windows-requirements-and-compatibility.html):
  IL2CPP requiere Visual Studio 2019 o posterior con herramientas C++ y Windows
  SDK 10.0.19041.0 o posterior. Su cumplimiento local no está acreditado.

## Cómo retomar desde CMD

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
codex
```

Dentro de Codex, pedir:

> Lee AGENTS.md y docs/ENTORNO_LOCAL_CODEX.md, junto con los documentos canónicos
> que indican. Comprueba rama y cambios locales. Resume pendientes y propón el
> siguiente bloque. No empieces U1 ni crees el proyecto Unity sin mi aprobación.

**Reparto del trabajo:** Codex local lee y modifica archivos del repositorio y
ejecuta comandos y comprobaciones autorizadas. Unity Editor importa paquetes y
assets, compila C#, abre escenas, ejecuta el juego y genera builds. Más adelante
Codex podrá invocar comprobaciones del Editor autorizadas y leer sus resultados;
escribir código no equivale a haberlo validado en Unity. La valoración jugable,
visual y de audio corresponde también al autor.

## Comprobaciones de este bloque documental

Comandos del bloque, ejecutados por separado con las dependencias existentes
y sin `--write`:

```cmd
npm run typecheck
npm test
npm run build
node scripts\unity-reference.mjs
git diff --check
```

Resultados del primer intento, antes de corregir el verificador:

- `npm run typecheck`: correcto, salida 0.
- `npm test`: correcto, 202 tests de 27 ficheros, salida 0.
- `npm run build`: correcto, salida 0; build web generada en `dist/` (ignorado).
- `node scripts\unity-reference.mjs`: **fallo, salida 1**:
  `AssertionError [ERR_ASSERTION]: La referencia difiere: no regenerar para ocultar una regresión.`
  La salida mostraba diferencias CRLF/LF y de `sourceHashes`, diagnosticadas
  posteriormente como se detalla abajo. No se ha modificado la referencia ni usado
  `--write`. El proceso también informó de reoptimización de dependencias de Vite.
- `git diff --check`: correcto, salida 0; avisos de conversión futura LF a CRLF
  en los Markdown modificados, sin errores de espacios.

El primer intento se detuvo antes del commit/push, conforme a la instrucción
del autor. Después se autorizó la corrección mínima del verificador dentro de U0.

## Diagnóstico y corrección LF/CRLF (29/09/2026)

- `core.autocrlf=true` en la configuración de sistema; `core.eol` sin definir.
  Los atributos text/eol/filter/working-tree-encoding de las 127 fuentes no están
  definidos. Git muestra LF en el índice y CRLF en el árbol de trabajo.
- Los hashes originales locales coincidían en 0/127 casos; normalizando solo
  CRLF a LF coincidían en 127/127, igual que los blobs del commit base. Los bytes
  normalizados también eran idénticos a esos blobs.
- Referencia calculada en memoria: 127 diferencias estructurales, todas en
  `sourceHashes`; cero diferencias restantes en catálogo, traducciones, RNG,
  fórmulas, guardados, mundos o audio. Cero diferencias numéricas, sin tolerancias.
- El texto de `baseline.json` tenía 5022 CRLF. Normalizando esos finales y los
  hashes fuente, la comparación textual completa coincidía exactamente.
- Medios: los dos PNG y los dos WAV coincidían byte a byte con sus huellas.
  `report.json` también coincidía al normalizar únicamente CRLF a LF.

La corrección usa `scripts/unity-reference-bytes.mjs`: clasificación explícita
de `.ts` (incluido `.d.ts`) y `.css` bajo `src/`, `package-lock.json`, y las rutas
exactas de `baseline.json` y `report.json` bajo `unity/Docs/Reference/`.
Solo elimina el byte CR inmediatamente anterior a LF en esos textos. PNG, WAV
y cualquier archivo no clasificado se verifican sin transformación; `media.json`
no se modifica ni se clasifica para normalizar su huella.

Se mantiene la comprobación Git contra la base aprobada y los hashes se calculan
sobre archivos locales. No se sustituyen por blobs Git, no se eliminan espacios,
no se reformatea JSON ni se añaden tolerancias. No se cambian la configuración
de Git, las referencias o los medios; no se usa `--write`. Un nuevo tipo textual
necesitará clasificación explícita. Las diferencias reales siguen siendo errores.

Pruebas aisladas reproducibles, solo con buffers sintéticos y sin dependencias:

```cmd
node --test scripts\unity-reference-bytes.test.mjs
```

Resultado: 4 pruebas correctas. Cubren equivalencia LF/CRLF, rechazo de cambios
reales, conservación de espacios/formato/CR aislado/bytes no UTF-8 y rechazo de
alteraciones en binarios o archivos no clasificados. No alteran originales.

Verificaciones completas posteriores a la corrección, todas con salida 0:

- `npm run typecheck`: correcto.
- `npm test`: 202 tests de 27 ficheros correctos.
- `npm run build`: build web correcta en `dist/` (ignorado por Git).
- `node scripts\unity-reference.mjs`: «Referencia U0 verificada: datos y medios coinciden».
- `git diff --check`: sin errores de espacios; avisos de conversión futura LF/CRLF.

El fallo inicial queda resuelto sin reinstalar dependencias ni modificar la base
de referencia. La entrega documental y la corrección quedan habilitadas para
commit/push en la rama autorizada tras revisar el diff y el remoto.
No se han ejecutado nuevas pruebas de navegador ni Unity. U1 sigue sin empezar.
