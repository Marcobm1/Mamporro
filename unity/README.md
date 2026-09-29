# MAMPORRO en Unity

**U0 en preparación.** Esta carpeta contiene la referencia y el plan del futuro
proyecto. Todavía no es un proyecto que Unity Hub pueda abrir: no se han creado
`Assets/`, `Packages/` ni `ProjectSettings/`.

- Destino aprobado: Windows de escritorio, con objetivo de publicación en Steam
  y plataformas similares. Web queda fuera del primer destino de la migración.
- Editor del autor: Unity 6.6 (6000.6.3f1), confirmado por captura y metadatos
  locales. Soporte Windows Mono detectado; Windows IL2CPP ausente en esta instalación.
  Arranque, licencia operativa y builds pendientes. U1 requiere aprobación previa;
  no crear aún `ProjectVersion.txt` ni fijar versiones de paquetes.
- Referencia web aprobada: `0505b1690656d15188860157612455639820fe1f`.
- Mantener el progreso compatible mediante exportación/importación validada.
- Mismo repositorio y rama `claude/zen-pasteur-674ik0`, sin PR.

## Documentos

- [Estado y criterios de U0](Docs/U0_REFERENCIA.md).
- [Mejoras del juego y recomendaciones](Docs/HOJA_DE_RUTA.md).
- [Datos de referencia](Docs/Reference/baseline.json).
- [Plan de migración U0–U6](../docs/MIGRACION_UNITY.md).
- [Checkpoint del entorno local y cómo retomar](../docs/ENTORNO_LOCAL_CODEX.md).

Desde la raíz del repositorio, en CMD:

```cmd
npm ci
node scripts\unity-reference.mjs
```

El comando compara los datos actuales con la referencia aprobada. No usa Unity.
El ejecutable del Editor está localizado en Windows, pero todavía no se ha
arrancado en estas comprobaciones. Compilar, ejecutar escenas y medir una build
siguen pendientes; la presencia de los archivos no acredita esas pruebas.
