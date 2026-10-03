# Migración propuesta a Unity

> Estado vigente en [ESTADO_ACTUAL](ESTADO_ACTUAL.md): U1–U3 aprobados; aprobación manual U3 el 04/10/2026. [U4 autorizado solo para planificación](PROGRESO_U4.md); implementación pendiente de la siguiente respuesta del autor. Las autorizaciones antiguas que figuran debajo son históricas.

## Historial de preparación (sustituido por el checkpoint U2)

**Actualización 29/09/2026:** U1 aprobado expresamente, en implementación.
[Checkpoint vigente](PROGRESO_U1.md). Se autorizan Windows x64 Mono, URP 17.6.0
e Input System 1.20.0 tras comprobarlos en el Editor instalado. Las menciones
inferiores a aprobación pendiente corresponden al plan histórico de U0.
U1 incluye cargas de 300/500/750/1000, salida 1080p/1440p e interna 240/360/480;
no modifica balance, progreso compatible ni el alcance posterior de U2–U6.

Estado: **U0 autorizado y en preparación** (28/09/2026). Hito 6 aprobado por el
autor. Windows de escritorio confirmado, con objetivo Steam/plataformas similares;
conservar el progreso compatible. Unity 6.6 (6000.6.3f1) está confirmado por
captura y metadatos locales (29/09/2026). Aún no se ha creado el proyecto de
Editor ni una build Unity; U1 sigue pendiente de aprobación.
La versión Three.js seguirá siendo la referencia hasta aprobar la sustitución.
Diagnóstico local y pendientes en [ENTORNO_LOCAL_CODEX.md](ENTORNO_LOCAL_CODEX.md).

## Objetivo y alcance

Trasladar MAMPORRO conservando su identidad: hordas, tercera persona, estética PS1,
humor, contenido procedural, personajes, progresión, economía y textos ES/EN.
Después podremos pulirlo en Unity con escenas, Inspector, herramientas de perfilado
y builds nativas. Cambiar de motor no garantiza por sí solo mejor rendimiento ni
mejor jugabilidad: hay que demostrar ambas cosas en una versión ejecutable.

La primera versión Unity debe reproducir lo que ya funciona. Durante el port no
se añaden armas, personajes, mapas ni sistemas nuevos, ni se aprovecha para cambiar
el balance. Las mejoras que surjan se anotan para después.

No hay una conversión directa de Three.js/TypeScript/HTML a Unity/C#/UI. Se
reutilizan el diseño, los valores, las fórmulas, los algoritmos, los textos y los
casos de prueba; se reescriben sus implementaciones e integraciones con el motor.

## Decisiones que confirmar al arrancar

Acuerdos actualizados y asuntos pendientes. Detalle operativo en
[`unity/Docs/U0_REFERENCIA.md`](../unity/Docs/U0_REFERENCIA.md). Mejoras solicitadas
por el autor y recomendaciones en
[`unity/Docs/HOJA_DE_RUTA.md`](../unity/Docs/HOJA_DE_RUTA.md).

- **Plataforma confirmada:** Windows de escritorio, con futuro lanzamiento en
  Steam/plataformas similares. La edición web no es el primer objetivo Unity.
- **Editor confirmado:** Unity 6.6 (6000.6.3f1), por captura y metadatos locales.
  No volver a pedir la revisión. Quedan arranque, licencia operativa e importación
  de paquetes; no fijar una revisión de URP sin comprobarla. La propuesta de U1
  (Windows x64 Mono, URP 17.6 e Input System 1.20.0) aún no está aprobada.
- **Render:** probar URP con una escena representativa antes de comprometer toda
  la migración. Mantener 240/360/480 píxeles de altura interna, filtrado puntual,
  paleta, niebla, geometría sencilla, dither y cuantización de vértices.
- **UI:** prototipo con uGUI para HUD/menús; confirmar legibilidad, navegación y
  fuente procedural. Valorar UI Toolkit si resuelve mejor las necesidades reales,
  sin implementar ambos sistemas a la vez.
- **Repositorio:** conservar el proyecto web y crear el proyecto Unity en
  `unity/`, sobre la rama actual salvo instrucción posterior. No borrar ni
  sobrescribir la referencia web. Versionar `Assets/` con sus `.meta`,
  `Packages/` y `ProjectSettings/`; excluir cachés y builds de Unity.
- **Guardado confirmado:** conservar todo el progreso compatible mediante
  exportación web/importación Unity validada. Ante un guardado Unity existente,
  propuesta: copia de seguridad y sustitución explícita, nunca sumar saldos ni
  fusionar recompensas. No se importa automáticamente el localStorage del navegador.

## Correspondencia de sistemas

| Actual | Destino propuesto | Qué conservar y qué verificar |
| --- | --- | --- |
| `src/data/` | Datos C# y ScriptableObjects de configuración | IDs estables, tablas, límites y fórmulas; no guardar progreso del jugador en estos assets |
| `core/Run.ts`, `systems/`, `weapons/` | Núcleo C# sin dependencias de escena | Paso fijo, orden de sistemas, ofertas, daño, pasivas, economía y resultados |
| `core/rng.ts`, generación del mundo | RNG propio C# y generadores equivalentes | Vectores de prueba, semillas, derivación por subsistema; no reemplazar por `UnityEngine.Random` |
| `playerPhysics`, colisiones, `CameraRig` | Controlador y cámara propios adaptados | Saltos, pendientes, deslizamiento, horda, obstáculos, sensibilidad y pausa |
| Arrays e `InstancedMesh` | Arrays/pools e instanciación de render | Medir 300+ enemigos; no crear un Rigidbody ni un Update por enemigo por defecto |
| Geometría/texturas/fuente procedural | Generadores C# de Mesh/Texture y atlas de fuente | Comparar capturas; decidir qué generar en Editor y qué en ejecución |
| Materiales/shaders PS1 | Shaders/pases URP y RenderTexture | Portar GLSL a la solución de Unity; revisar APIs de la versión fijada |
| UI HTML/CSS e i18n | HUD/menús Unity y tablas ES/EN | Todas las pantallas, textos, tamaños, foco y opciones |
| WebAudio procedural | PCM original y AudioClip/AudioSource/AudioMixer | Separación música/efectos, prioridades, límite de voces, silencio y pausa |
| `save/`, `systems/meta.ts` | DTO JSON versionado, migraciones y almacenamiento local | Validación, copias de seguridad, recompensas únicas y exclusión de trucos |
| Vitest y `?test` | Pruebas de núcleo/Edit Mode/Play Mode y escena de QA | Portar casos útiles y crear pruebas de integración; no exponer trucos en builds finales |

Los ScriptableObjects son contenedores editables de datos compartidos [1]. La
ruta persistente depende de la plataforma [2]; no equivale al `localStorage` de
la web. Unity permite crear clips desde muestras PCM [3], por lo que el sonido
puede seguir siendo original y procedural. El Input System [4] se evaluará con
teclado/ratón primero. RenderTexture [5] sirve de base para la resolución interna;
los efectos PS1 adicionales requieren su implementación y validación visual.

## Fases y criterios para avanzar

Un bloque a la vez: plan breve, dudas importantes juntas, aprobación, implementación,
pruebas, commit en español y demostración al autor. No empezar la siguiente fase
sin validar la anterior. Las estimaciones se harán tras el prototipo; no se promete
una duración total sin medir el coste real del port.

### U0. Congelar la referencia y preparar el trabajo

- Resolver los comentarios finales del hito 6 y registrar su commit aprobado.
- Inventariar contenido, controles, opciones, desbloqueos, misiones y limitaciones.
- Guardar capturas ES/EN, semillas de referencia, muestras de audio, casos de
  guardado y métricas con las condiciones de medida.
- Registrar decisiones de plataforma, Editor y repositorio de la sección anterior.
- Preparar comparaciones del RNG y fórmulas con valores esperados exportados
  desde TypeScript; el port del motor no debe redefinir lo que significa un test.

**Salida:** referencia reproducible, lista de equivalencia y plan aprobado.

### U1. Prototipo técnico de movimiento, imagen y horda — aceptado

- Crear proyecto mínimo y build Windows arrancable.
- Terreno pequeño con pendientes y obstáculos, un personaje, cámara y controles.
- Resolución interna PS1, niebla y un material representativo con dither/snap.
- Ensayo de 300+ enemigos con arrays/pools e instanciación, aunque aún no tengan
  todos los comportamientos. Capturar CPU, GPU, memoria y asignaciones por frame.

**Salida:** el autor valida aspecto y sensación de movimiento; rendimiento medido
fuera del Editor en su equipo. Si el prototipo no cumple, ajustar arquitectura
antes de portar el contenido. DOTS/ECS no es requisito inicial: evaluarlo solo
si el perfilador demuestra que hace falta.

### U2. Núcleo y combate equivalentes — aprobado

Alcance operativo en [PROGRESO_U2](PROGRESO_U2.md). Los seis comportamientos
enemigos se prueban en escenas controladas, incluidos élite y jefe. Director,
portal, oleadas, enjambre, mundo, interactuables y victoria integrada quedan
para U3. Los objetos se conceden mediante QA sin persistencia.

- Portar RNG, estadísticas, daño/críticos, progresión, colisiones y rejilla espacial.
- Conectar una partida completa mínima: una arma, un enemigo, experiencia, subida
  de nivel, daño al jugador, derrota y reinicio.
- Completar las seis armas, enemigos, proyectiles, pasivas, tomos y objetos.
- Portar las pruebas de reglas y comparar salidas con la versión web.

**Salida:** las reglas y capacidades coinciden, no hay acumulación de objetos al
reiniciar y el combate mantiene el presupuesto de rendimiento.

### U3. Mundo y partida completa

- Portar mapa procedural, estructuras, interactuables, oleadas, élites, enjambre,
  portal y jefe; duraciones 5/10/15 minutos.
- Reproducir avisos de ataques, minimapa, desafíos, elecciones, pausa y victoria.
- Validar semillas y posiciones de referencia, además de recorridos jugables.

**Salida:** partida de inicio a resultados con ambos personajes y las tres
duraciones; inventario de diferencias aprobado. No exigir identidad de cada bit:
JavaScript usa números de doble precisión y Unity suele usar `float` en geometría;
el ruido procedural y la física también pueden divergir. Definir tolerancias,
conservar enteros/operaciones del RNG y no prometer mapas idénticos sin pruebas.

### U4. Meta, UI, opciones y traslado de guardados

**04/10/2026:** solo planificación autorizada. Plan y criterios de seguridad en [PROGRESO_U4](PROGRESO_U4.md). Validar, respaldar y pedir sustitución explícita si existe guardado Unity; nunca fusionar progresos. Excepción web limitada al exportador, sin tocar reglas/balance ni las referencias congeladas.

- Rehacer menú, preparación, personajes, tienda, ocho misiones y resultados ES/EN.
- Portar las reglas de moneda, compras, desbloqueos y liquidación única.
- Añadir exportación JSON validada a la web y un importador Unity con versión,
  límites, IDs admitidos, copia de seguridad y confirmación de sustitución.
- Guardar en una ubicación apropiada por plataforma y manejar fallos de escritura.
- Probar datos vacíos, antiguos, corruptos, futuros, valores extremos, progreso
  parcial, compras completas e importación repetida. Nunca duplicar monedas.
- No trasladar partidas en curso: el guardado actual tampoco las conserva.

**Salida:** se conserva el progreso acordado, sin pérdida silenciosa ni premios
extra; todas las pantallas y opciones se usan en una build Windows real.

### U5. Audio, pulido y validación de equivalencia

- Portar síntesis y reproducción, presupuestos de voces/partículas, cámara y
  opciones de reducción de efectos; conservar avisos de combate legibles.
- Pruebas unitarias e integración, sesiones de principio a fin y capturas ES/EN
  a 1280×720 y otra resolución; prueba de audio y dispositivos en Windows.
- Perfilar una build representativa: 300+ enemigos, armas simultáneas, proyectiles,
  gemas y partículas. Objetivo 60 FPS en el equipo de referencia acordado;
  registrar resolución, hardware y tiempos CPU/GPU, sin confundir Editor o
  render por software con rendimiento final.
- Probar varias partidas seguidas, pausa/foco, cambio de opciones, cierre y carga.

**Salida:** lista de equivalencia completa, diferencias aceptadas y aprobación
jugable del autor. Corregir regresiones antes de añadir contenido nuevo.

### U6. Adoptar Unity como versión principal

- Actualizar README y guía de trabajo con Editor exacto, apertura, pruebas y build.
- Conservar una referencia recuperable a la última versión web aprobada y sus
  instrucciones. Decidir explícitamente si se mantiene publicada o se archiva.
- Preparar build de entrega y registrar sus limitaciones y problemas conocidos.
- Solo después de esta aceptación, proponer nuevas mejoras y contenido en Unity.

## Cómo colaborar desde esta conversación

Podemos mantener código C#, datos, shaders, pruebas, documentación y commits aquí.
Para comprobar escenas, importar assets y producir/ejecutar builds hace falta un
Editor Unity disponible con sus módulos y licencia válidos. No asumir que este
entorno tiene Editor, GPU o acceso al equipo Windows del autor.

Al iniciar U0 comprobaremos esas capacidades. Si el Editor no está disponible
aquí, el autor abrirá el proyecto y ejecutará las comprobaciones locales con una
lista breve; compartirá resultados y logs para continuar. Las pruebas automatizadas
por línea de comandos se prepararán para **CMD**, usando la ruta real del Editor.
No declarar una escena o build verificada solo porque el código se haya escrito.

## Fuentes oficiales consultadas

Consulta: 28/09/2026. Los enlaces sin versión fija pueden cambiar; al iniciar U0,
consultar los correspondientes al Editor y paquetes elegidos.

1. [Unity: ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html).
2. [Unity: Application.persistentDataPath](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).
3. [Unity: AudioClip.Create](https://docs.unity3d.com/ScriptReference/AudioClip.Create.html).
4. [Unity: Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/).
5. [Unity: Render Texture](https://docs.unity3d.com/Manual/class-RenderTexture.html).
6. [Unity: archivo de versiones del Editor](https://unity.com/releases/editor/archive).
