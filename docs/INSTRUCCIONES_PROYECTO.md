# Instrucciones compartidas del proyecto MAMPORRO

Este documento contiene las reglas estables que deben seguir ChatGPT, Codex CLI y Claude Code. `AGENTS.md` y `CLAUDE.md` deben apuntar aquí en vez de mantener versiones contradictorias de estas reglas.

## Proyecto

MAMPORRO es un roguelike 3D de supervivencia contra hordas con humor propio y estética PS1. La versión web aprobada usa Three.js + TypeScript estricto + Vite. Se está migrando a Unity para Windows de escritorio con intención de publicación futura en Steam/plataformas similares.

Repositorio: `https://github.com/Marcobm1/Mamporro`

Rama de trabajo y por defecto: `claude/zen-pasteur-674ik0`.

El autor autoriza trabajar y hacer push directamente a esa rama. No abrir Pull Requests salvo petición explícita. No hacer force-push. No borrar, resetear ni sobrescribir cambios locales o ajenos sin consultarlo.

## Herramientas de trabajo y continuidad

El proyecto puede ser trabajado por **Codex CLI** o por **Claude Code**, pero no simultáneamente. Cuando termine la sesión/tokens de uno, el otro continuará sobre la misma rama.

El repositorio y sus Markdown son la memoria compartida entre herramientas. Ningún agente debe depender del historial privado de su propia conversación para que el siguiente pueda continuar.

El protocolo completo de relevo está en [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md). Es obligatorio leerlo al comenzar una sesión y respetarlo al ceder el trabajo.

Cada sesión que produzca cambios relevantes debe dejar documentado, como mínimo, qué se hizo, qué archivos/sistemas cambiaron, qué pruebas se ejecutaron realmente, qué queda pendiente, qué commits se publicaron, qué cambios locales quedan fuera y cuál es el siguiente paso exacto.

Solo un agente escribe en la rama a la vez. No crear trabajo paralelo para Codex y Claude Code salvo autorización expresa del autor.

## Fuentes de verdad

Al iniciar una sesión, leer en este orden:

1. `docs/ESTADO_ACTUAL.md`.
2. `docs/CONTINUIDAD_AGENTES.md`.
3. El checkpoint del bloque vigente (`docs/PROGRESO_U4.md`: planificación, sin implementar todavía).
4. `docs/DECISIONES.md` y cualquier continuación posterior indicada por el estado actual.
5. `docs/ESPECIFICACION.md` como especificación histórica de partida.
6. `docs/MIGRACION_UNITY.md`.
7. `README.md`, `unity/README.md`, `unity/Docs/U0_REFERENCIA.md` y `unity/Docs/HOJA_DE_RUTA.md` cuando se trabaje en Unity.
8. `AGENTS.md` o `CLAUDE.md` como entrada específica de la herramienta.

Las decisiones posteriores concretan o sustituyen la especificación original. Una instrucción explícita nueva del autor puede cambiar el alcance. Si dos documentos se contradicen en el estado temporal, manda `docs/ESTADO_ACTUAL.md` y después el checkpoint más reciente del bloque.

## Forma de trabajo

Trabajar un hito o bloque cada vez. Antes de programar, presentar un plan breve y reunir en una sola tanda las dudas importantes que realmente sigan abiertas. No volver a preguntar decisiones ya confirmadas. Una autorización previa del bloque no necesita repetirse.

Durante trabajos largos, comunicar avances y dejar checkpoints Markdown para poder continuar desde otro chat o herramienta. No depender exclusivamente del historial de conversación.

En bloques largos, actualizar el checkpoint también en puntos intermedios cuando cambie de forma útil el punto de continuación. Si se prevé un relevo Codex ↔ Claude Code, priorizar dejar un estado recuperable antes de iniciar una pieza nueva.

Al cerrar un bloque:

- actualizar la documentación de estado y decisiones;
- ejecutar y registrar las comprobaciones que correspondan;
- hacer commits pequeños en español, cada uno coherente y verificable;
- hacer push a `claude/zen-pasteur-674ik0` sin PR;
- resumir cambios, cómo probarlos, decisiones técnicas, límites de verificación y siguiente paso;
- detenerse para que el autor pruebe antes de iniciar el siguiente bloque.

No afirmar una prueba que no se haya ejecutado. No confundir pruebas de lógica con FPS reales, Play Mode con una build final ni una captura con validación jugable.

## Checkpoints de bloque (complemento a CONTINUIDAD_AGENTES)

Decidido por el autor el 29/09/2026 para U2 y todos los bloques futuros. Este documento es la fuente de las reglas comunes; [`CONTINUIDAD_AGENTES.md`](CONTINUIDAD_AGENTES.md) desarrolla el protocolo de relevo. Además de lo que allí se indica:

- Cada checkpoint de bloque (`docs/PROGRESO_U2.md`, `PROGRESO_U3.md`…, mismo formato) empieza con una sección **«Cómo retomar»**: último commit publicado, paso actual, qué está terminado y verificado, qué está a medias, qué hay sin commit y por qué, siguiente paso exacto y comandos CMD para comprobar el estado. Se actualiza después de cada paso completado, no solo al final.
- Cada prueba ejecutada se registra con comando exacto, fecha, resultado (p. ej. 37/37) y ruta del XML o log. Lo no ejecutado figura como pendiente, nunca como correcto.
- Si el código queda sin compilar o con pruebas rotas, el checkpoint lo dice con el error y el archivo.
- Las decisiones nuevas van a `docs/DECISIONES.md` con fecha y quién las tomó (autor o propuesta aprobada por el autor).
- `CLAUDE.md` y `AGENTS.md` son equivalentes, apuntan a este documento y no se contradicen.
- Ni en commits ni en código aparecen nombres de modelos de IA, tampoco líneas `Co-Authored-By` de herramientas.

## Git y protección de cambios

Antes de trabajar:

```cmd
git status --short --branch
git log -5 --oneline
git remote -v
```

Cuando una sesión empiece después de un relevo, comprobar además el remoto antes de editar. Si el árbol está limpio, actualizar solo mediante avance seguro (`git pull --ff-only`) cuando corresponda.

Preservar el árbol local. No usar `git reset --hard`, `git clean -fd`, force-push ni reclonar para eliminar diferencias. Si Unity ha escrito ajustes locales, revisar su intención y separarlos del bloque si no pertenecen a él.

Si hay cambios locales al recibir el relevo, no asumir que son basura: pueden pertenecer al autor, a Unity o a la sesión anterior. Consultar el checkpoint y describir cualquier discrepancia antes de sobrescribir archivos.

## Windows y herramientas

El usuario trabaja en Windows; los comandos que deba ejecutar deben funcionar en CMD. No suponer acceso remoto a su ordenador. No pedir claves, tokens o credenciales en el chat; la autenticación la realiza el usuario.

Entorno confirmado el 29/09/2026:

- raíz del repo: `C:\Users\bymar\Desktop\Varios\Proyectos\Mamporro-git`;
- Git 2.54.0.windows.1;
- Node 24.21.0;
- npm 11.19.0;
- Codex CLI 0.158.0;
- Unity 6.6 `6000.6.3f1` en `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`;
- Windows x64 Mono disponible; IL2CPP Windows no estaba instalado durante U1;
- URP 17.6.0, Input System 1.20.0, uGUI 2.6.0 y Test Framework 1.8.0 efectivos.

No presupongas un «CLI de Unity» independiente ni plugins de terceros. Usar el Editor/ejecutable de Unity por línea de comandos cuando proceda. Consultar documentación oficial vigente si una instalación, versión o API puede haber cambiado.

## Web aprobada

Los seis hitos web están implementados y aprobados. Base congelada para comparar el port: `0505b1690656d15188860157612455639820fe1f`.

Mantener la web recuperable y no alterarla para ocultar diferencias de Unity. Verificación web cuando corresponda: typecheck, tests, build y navegador `?test`. La evidencia histórica de navegador no sustituye una prueba nueva si se cambia la web.

## Unity y migración

U0 conserva referencia reproducible. U1 creó y validó el proyecto técnico y fue aprobado manualmente por el autor el 29/09/2026. U2 fue aprobado por el autor el 29/09/2026. **U3 aprobada manualmente el 04/10/2026; U4 autorizado solo para planificación.** El plan está en `docs/PROGRESO_U4.md`; esperar la siguiente respuesta del autor antes de implementarlo.

Durante el port, conservar IDs, reglas, datos, traducciones y progresión compatible. No rebalancear ni añadir contenido nuevo para «mejorar» mientras se valida equivalencia, salvo autorización expresa.

La transferencia futura de guardado debe conservar de forma compatible moneda meta, desbloqueos, misiones, usos extra, selección y opciones equivalentes. No se guardan partidas activas. Validar formatos, evitar duplicaciones, proteger el original y no sumar dos guardados automáticamente.

## Mejoras confirmadas para después de validar la base Unity

- mundo y estructuras mayores;
- colinas más cuadradas/mesetas, rampas y mayor verticalidad;
- escalada libre por paredes; no imponer trepada breve ni resistencia sin consultarlo;
- menos enemigos al principio y crecimiento progresivo;
- más oro de enemigos **durante la partida**, aumentando con tiempo y dificultad; no confundir con Calderilla del Caos;
- acabado retro más profesional;
- ilustraciones e iconos originales en archivos permitidos;
- más enemigos, armas, tomos y personajes.

No hay cantidades finales cerradas. Primero validar la migración; después aplicar mejoras por bloques, salvo prototipo aislado aprobado.

Cámara contra estructuras, mando/remapeo, métricas locales y servicios Steam son propuestas o fases posteriores. No añadir telemetría externa por defecto, no comprar/publicar nada y no copiar arte de otros juegos.
