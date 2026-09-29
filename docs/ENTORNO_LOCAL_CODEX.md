# Entorno local de MAMPORRO — estado confirmado

Actualizado: **29/09/2026**, después de completar U1.

La instalación/verificación de Codex CLI y del Editor ya no es un pendiente. El estado de trabajo actual está en [`ESTADO_ACTUAL.md`](ESTADO_ACTUAL.md) y el bloque autorizado en [`PROGRESO_U2.md`](PROGRESO_U2.md).

## Repositorio

- Raíz usada: `C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git`.
- Remoto: `https://github.com/Marcobm1/Mamporro.git`.
- Rama: `claude/zen-pasteur-674ik0`.
- Base web aprobada: `0505b1690656d15188860157612455639820fe1f`.

Antes de trabajar:

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
git log -5 --oneline
```

No limpiar ni resetear diferencias locales. El checkpoint U1 registró modificaciones posteriores generadas por Unity fuera de los commits publicados; comprobar si siguen presentes y conservarlas.

## Herramientas comprobadas

| Herramienta | Versión | Ruta/nota |
| --- | --- | --- |
| Git | `2.54.0.windows.1` | `C:\Program Files\Git\cmd\git.exe` |
| Node.js | `24.21.0` | `C:\Program Files\nodejs\node.exe` |
| npm | `11.19.0` | `C:\Program Files\nodejs\npm.cmd` |
| Codex CLI | `0.158.0` | `C:\Users\bymar\AppData\Roaming\npm\codex.cmd` |
| Unity | `6000.6.3f1` | `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe` |

Codex CLI fue arrancado desde el repositorio y pudo leer sus documentos. La autenticación la realiza el usuario; no almacenar ni pedir tokens en documentación o chat.

## Unity comprobado en U1

- Editor `6000.6.3f1` (`45d8eee7de74`) con licencia operativa.
- Soporte Windows x64 Mono utilizado con éxito.
- Windows IL2CPP no estaba instalado en la instalación comprobada; no es requisito automático para U2.
- URP/Core/ShaderGraph 17.6.0.
- Input System 1.20.0.
- uGUI 2.6.0.
- Test Framework 1.8.0.
- Proyecto real en `unity/`, importado y compilado.
- Build Windows x64 Mono generada y ejecutada con D3D11.

U1 no instaló software adicional. Burst/Collections aparecen como dependencias transitivas de URP y no significan que el proyecto adopte DOTS/ECS.

## Hardware usado para U1

- CPU: AMD Ryzen 7 7700X 8-Core Processor.
- GPU: NVIDIA GeForce RTX 4070 Ti SUPER; también gráfica AMD integrada.
- RAM: aproximadamente 32 GB.
- Salidas probadas: 1920×1080 y 2560×1440.

Es hardware de referencia del autor, no requisito mínimo comercial.

## Particularidades conocidas

- En este Windows, PowerShell bloqueó `npm.ps1`; desde CMD usar `npm.cmd` evita cambiar la política del sistema.
- El verificador de referencia se corrigió para normalizar **solo CRLF→LF** en los archivos de texto explícitamente clasificados. Binarios se comparan byte a byte. No usar `--write` para hacer pasar diferencias.
- Ejecutar el verificador con Unity cerrado evita que Vite/watchers choquen con `UnityLockfile`.

Comandos de referencia:

```cmd
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
node --test scripts\unity-reference-bytes.test.mjs
node scripts\unity-reference.mjs
```

U1:

```cmd
scripts\u1.cmd edit
scripts\u1.cmd play
scripts\u1.cmd build
scripts\u1.cmd benchmark
node scripts\u1-report.mjs
```

Los resultados/builds/cachés de Unity son locales e ignorados por Git cuando corresponde. No confundir archivos locales de benchmark con artefactos publicados.

## Cómo iniciar Codex ahora

```cmd
cd /d "C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git"
git status --short --branch
codex
```

Instrucción recomendada dentro de Codex:

> Lee `AGENTS.md`, `docs/ESTADO_ACTUAL.md` y `docs/PROGRESO_U2.md`, además de los documentos que enlazan. Conserva cualquier cambio local. U1 está aprobado y U2 está autorizado; no empieces U3. Antes de programar U2, resume el plan y señala únicamente dudas materiales todavía no resueltas.

No es necesario volver a verificar la existencia de Codex o pedir la versión de Unity salvo que un error indique que el entorno ha cambiado.
