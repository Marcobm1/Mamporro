# MAMPORRO

**Unity es la versión principal. La migración U0–U6 está terminada y formalmente cerrada.** U6 probado y aprobado manualmente por el autor el 05/10/2026; entrega `unity\Builds\Windows\MAMPORRO.exe`. [Cierre U6](docs/PROGRESO_U6.md).

Roguelike 3D de supervivencia contra hordas, tercera persona, humor propio y estética retro. Dos personajes, seis armas, ocho tomos, doce objetos, mundo procedural, jefe, progresión permanente, español e inglés. La base migrada conserva el catálogo y balance aprobados; las mejoras se abordarán en bloques posteriores.

## Abrir y jugar en Unity

Editor fijado: **Unity 6.6 / 6000.6.3f1**, Windows x64 Mono, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0 y Test Framework 1.8.0. Añade la carpeta `unity` a Unity Hub y abre `Assets/Mamporro/U3/U3_Partida.unity`. No abrir dos editores sobre el mismo proyecto.

La escena/rutas U3 siguen siendo la implementación principal; no se duplican por cambiar de hito. [Guía Unity, controles y QA](unity/README.md). WASD y ratón, Espacio para saltar, Mayús/C para deslizarse, E para interactuar, Esc para pausa. F3 muestra debug; usar sus acciones invalida recompensas de esa partida.

Con el Editor cerrado, desde la raíz del repositorio en CMD:

```cmd
scripts\u3.cmd edit
scripts\u3.cmd play
scripts\u3.cmd build
```

Edit Mode prepara transferencias web de prueba: requiere Node 22.12 o superior y dependencias instaladas mediante `npm.cmd ci`. Unity puede jugarse sin Node desde una build autónoma.

**Build de entrega:** `scripts\u3.cmd build` genera `unity\Builds\Windows\MAMPORRO.exe` (producto `MAMPORRO`, empresa `Mamporro`, Windows x64 Mono normal). El progreso se guarda en `%USERPROFILE%\AppData\LocalLow\Mamporro\MAMPORRO\Progress`; el de versiones anteriores (`...\Mamporro\Mamporro U1\Progress`) se ofrece para revisar y copiar, nunca se mueve ni se fusiona.

Para probar la build sin usar el guardado personal:

```cmd
start "" "unity\Builds\Windows\MAMPORRO.exe" -monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -u4-save-dir "%TEMP%\MamporroU6Prueba"
```

Paquete local de entrega (sin publicar nada): `scripts\mamporro.cmd package` crea en `unity\Builds\Paquete\` la carpeta `MAMPORRO-Windows-x64-<commit>` (build sin la carpeta de copia de seguridad de Unity, `LEEME.txt` y `MANIFIESTO.json` con tamaño y SHA-256 de cada archivo), su ZIP determinista y el `.sha256`; `scripts\mamporro.cmd verify` lo extrae en una carpeta temporal con espacios, comprueba todas las huellas y arranca `MAMPORRO.exe -u6-smoke` con un guardado temporal.

Conservar toda la carpeta de build, incluidos datos y DLL. Builds, resultados y capturas no se versionan. Development es diagnóstico separado, no entrega final.

## Progreso y opciones

U4 ya implementa persistencia versionada, backup/recuperación, compras, ocho misiones, selección y 14 opciones. No guarda partidas activas. Oro de partida y Calderilla del Caos son monedas distintas. Partidas con trucos y abandonos no dan meta; no se duplican liquidaciones.

Web → Unity: exportar desde Opciones de la web y abrir el JSON desde Opciones → Importar progreso web en Unity; revisar y confirmar sustitución, nunca fusión. [Exportación](docs/EXPORTACION_U4.md), [contrato](docs/CONTRATO_GUARDADO_U4.md), [persistencia](docs/PERSISTENCIA_U4.md). La transición segura del progreso histórico ya forma parte de Unity: revisión y confirmación antes de copiar, original intacto, backup del destino y sin fusión ni duplicados.

## Referencia web conservada

La web permanece ejecutable en el repositorio, congelada con su exportador, sin desarrollo nuevo de gameplay ni despliegue web dentro de U6. [Guía web completa](docs/WEB_REFERENCIA.md). No borrar `src/`, historial ni referencias.

- Base funcional aprobada: `0505b1690656d15188860157612455639820fe1f`.
- El exportador U4 es una excepción documentada sobre esa base; los corpus congelados no se regeneran.
- Verificación histórica aislada, con dependencias Node instaladas:

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-historical-reference.ps1
```

## Estado, límites y trabajo futuro

[Equivalencia U5](docs/EQUIVALENCIA_U5.md) y [cierre aprobado U5](docs/PROGRESO_U5.md). Compresor aproximado, partículas iluminadas, jefe sin destello blanco y presentación provisional son limitaciones aceptadas. Los benchmarks solo describen el hardware y condiciones documentados.

U6 no añade balance, mundo ampliado, escalada, arte/animación final, contenido, mando ni Steam. [Hoja de ruta](unity/Docs/HOJA_DE_RUTA.md). [B0 Blender](docs/BLENDER_B0.md) está **autorizado para implementación por pasos**: instalación y verificador comprobados; exportación, assets e integración siguen el checkpoint.

[Instrucciones del proyecto](docs/INSTRUCCIONES_PROYECTO.md), [continuidad](docs/CONTINUIDAD_AGENTES.md), [decisiones](docs/DECISIONES.md) y [estado actual](docs/ESTADO_ACTUAL.md). Rama única `claude/zen-pasteur-674ik0`; preservar cambios locales y referencias. Tras cada bloque verificable, checkpoint y commit; U6 ya aprobado; B0 autorizado por pasos; detenerse para revisión manual antes de adopción masiva o mejoras posteriores.
