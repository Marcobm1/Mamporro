# Hito 6: punto de continuidad

Actualizado: 28/09/2026. Rama: `claude/zen-pasteur-674ik0`. Sin PR.
Base aprobada por el autor: `5e7b95a` (hito 5 completo).

## Acuerdos aprobados

- Música arcade cómica y enérgica, con variante intensa para jefe/enjambre.
- Efectos de intensidad moderada; opciones para reducir partículas y desactivar
  sacudidas y destellos. Mantener legibles los avisos de ataques.
- Autorizados ajustes de balance justificados por pruebas y documentados.
- Todo procedural, sin assets externos, TypeScript estricto y textos ES/EN.
- Comentar avances en la conversación y guardar puntos estables con commits.
- Tras terminar y validar este pulido, **antes de añadir más contenido**, plantear
  migración a Unity. Preparar `docs/MIGRACION_UNITY.md` con plan y fuentes oficiales;
  no crear aún proyecto Unity ni empezar a portar el juego.

## Bloques

| Bloque | Estado | Próximo paso |
| --- | --- | --- |
| Acuerdos y continuidad | Registrados | Mantener este documento en cada bloque |
| Audio y música | Pendiente | Motor WebAudio, límites/prioridades, eventos y tests |
| Pulido visual y opciones | Pendiente | Presupuesto de partículas, pasivas, guardado v3 |
| Balance | Pendiente | Comparar personajes/duraciones y justificar ajustes |
| Verificación final | Pendiente | Typecheck, tests, build, navegador y rendimiento |
| Documentación y Unity | Pendiente | Guía de contenido y migración por fases |

## Cómo retomar

1. Leer `CLAUDE.md`, este archivo y `docs/DECISIONES.md`.
2. Comprobar `git status` y `git log` y comparar con la rama remota antes de editar.
3. Conservar cambios en curso; ejecutar los tests del bloque que se esté retomando.
4. Seguir el primer bloque pendiente. El hito está autorizado: no volver a pedir
   aprobación por decisiones ya recogidas aquí.
5. Tras cada bloque estable, actualizar esta tabla y las pruebas, hacer commit
   pequeño en español y subirlo. Cada commit debe compilar y pasar los tests.
6. Al entregar el hito, detenerse para que el autor lo pruebe. Unity es el siguiente
   proyecto propuesto, no una ampliación automática del alcance de este hito.

## Entorno y subida

- Node 24; `npm ci`, `npm run typecheck`, `npm test`, `npm run build`.
- Usar `?test` y Chromium con SwiftShader. La captura real del ratón y los FPS
  del equipo del autor requieren su comprobación.
- El push por terminal puede carecer de credenciales. En el hito 5 se usó la
  conexión GitHub para árboles/commits y avance de rama sin force. Comparar el
  SHA del árbol con el local antes de publicar y verificar luego la rama.
- No depender de `/tmp`: es temporal. Los resultados necesarios para retomar
  deben quedar resumidos aquí y los scripts reutilizables en el repositorio.

## Verificación registrada

- Base: 188 tests / 24 ficheros; typecheck/build y navegador correctos en hito 5.
- Hito 6: implementación todavía no iniciada en este punto de control.
