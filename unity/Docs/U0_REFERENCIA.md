# U0: referencia para la migración

Estado: **referencia preparada; cierre pendiente de revisión exacta del Editor**.
Hito 6 probado y aprobado por el autor el 28/09/2026.

## Acuerdos confirmados

- Plataforma inicial: Windows de escritorio; objetivo comercial Steam y similares.
- El autor tiene Unity 6.6. Falta la cadena completa del Editor mostrada en Hub
  (incluidos revisión y sufijo). No se ha elegido una revisión distinta ni se ha
  dado por hecho que «6.6» signifique LTS o que sea una edición final.
- Conservar cuanto progreso sea compatible: moneda meta, desbloqueos, misiones,
  usos extra y selección. Validar/migrar opciones equivalentes; conservar una
  copia del original para valores que no tengan correspondencia.
- El traslado no incluye una partida en curso, que la web tampoco guarda.
- Proyecto futuro en `unity/`, rama actual, sin PR. Mejoras en
  [HOJA_DE_RUTA.md](HOJA_DE_RUTA.md); aún sin implementación.

La política oficial distingue LTS, Update y versiones preliminares. Revisaremos
la versión exacta instalada antes de fijar Editor y paquetes; no es necesario
cambiar de versión solo por tener una Update final.
[Fuente: soporte de Unity 6](https://unity.com/releases/unity-6/support),
consultada el 28/09/2026.

## Base fijada

Commit completo: `0505b1690656d15188860157612455639820fe1f`.
No se ha creado una rama nueva ni se ha modificado el gameplay del hito 6.

Inventario exportado en [Reference/baseline.json](Reference/baseline.json):

- 2 personajes, 6 armas, 8 tomos y 12 objetos.
- 6 definiciones de enemigos: 4 normales, 1 élite y 1 jefe.
- 8 misiones, tienda, desbloqueos iniciales, precios y límites.
- Configuración del mundo, jugador, cámara, oleadas e interactuables.
- Tablas ES/EN completas y opciones por defecto.
- Vectores del RNG/hash, fórmulas de daño, crítico, armadura, XP, precio de baúles
  y director para las tres duraciones.
- Casos de guardado nuevo/corrupto/v1/v2/v3/futuro con resultados esperados.
- Tres semillas de mundo con construcciones, interactuables y muestras de altura.
- Datos de referencia PCM y huellas SHA-256 de las fuentes originales.

Las semillas se pasan directamente a las funciones de generación; la entrada
manual del menú normaliza y recorta a 12 caracteres. Para comparar capturas usa
`HITO6QA`; `PULIDO-REFERENCIA` es la semilla cruda del ensayo automatizado.

## Uso de la referencia

Desde la raíz del repositorio, CMD:

```cmd
node scripts\unity-reference.mjs
npm run typecheck
npm test
npm run build
```

El exportador falla si `src/` o el lockfile difieren de la base aprobada. Sin
argumentos solo compara. `--write` se utilizó para crear esta referencia y no debe
usarse para ocultar regresiones. Cuando el port C# esté listo, sus tests leerán
los valores guardados; no recalcularán los valores esperados con su propio código.

- RNG/IDs/enteros: igualdad exacta. C# debe conservar operaciones de 32 bits,
  desplazamientos sin signo, caracteres UTF-16 y los 12 pasos iniciales del RNG.
- Fórmulas double: tolerancia absoluta inicial 1e-9; el redondeo de XP debe
  reproducir el de JavaScript, no depender del predeterminado de C#.
- Terreno/PCM: acordar tolerancias por algoritmo y plataforma. No exigir igualdad
  binaria de floats. El mundo ampliado tendrá comparaciones diferentes y deberá
  registrar el cambio de diseño explícitamente.
- Guardado: los casos capturan la conducta actual de la web. El importador Unity
  debe rechazar y explicar formatos futuros/corruptos, conservando el archivo
  original; no resetear silenciosamente el progreso existente. Importar dos veces
  no suma saldos ni premios.

## Evidencia conservada y reproducible

La base pasó 202 tests/27 ficheros, typecheck/build y QA de navegador ES/EN sin
errores. Se conserva una selección de capturas, el informe y muestras de audio
procedural en `Reference/`. El resto de pantallas se puede regenerar con
`scripts/verify-browser.cjs` contra la base indicada; ver instrucciones CMD en
README. Las capturas usan `?test` y trucos, por lo que no validan recompensas meta.

Condiciones del ensayo de balance: semilla `PULIDO-REFERENCIA`, quieto y orientado
al norte, elección de la primera carta, hasta muerte/120 segundos. Valores y
limitaciones están en `docs/DECISIONES.md`. No es una comparación suficiente de
habilidad, movimiento o equilibrio entre personajes.

Rendimiento histórico del hito 6: 1,08 ms/tick con ~499 enemigos; 2,01 con 500 y
cuatro armas; 2,77 con 750 y jefe/proyectiles. No incluye GPU ni acredita 60 FPS
en Windows. Para U1 hará falta conocer CPU, GPU, RAM y resolución del equipo del
autor y perfilar una build, no solo el Editor.

## Siguiente bloque

1. Recibir revisión exacta del Editor y comprobar que es una versión final
   soportada; fijar también versiones compatibles de paquetes.
2. Proponer U1: plantilla URP, Input System, cámara/controlador, escena de
   pendientes/obstáculos y ensayo de 300+ enemigos. Elegir UI cuando toque su
   prototipo; no llenar ahora el proyecto de paquetes innecesarios.
3. Tras aprobación, crear el proyecto y comprobarlo en Unity del autor. Aquí no
   se ha encontrado Editor de Unity; no declarar compilación/escena verificada
   sin ejecutarla. Unity Hub aún no puede abrir esta carpeta como proyecto.

Verificación de esta preparación: 202 tests/27 ficheros, typecheck y build
correctos; comprobación de referencia y huellas de medios correcta. El build web
resultante es idéntico al aprobado. No se ha compilado ni ejecutado código Unity.

## Continuidad en Proyecto de ChatGPT y Codex CLI

El 28/09/2026 el autor confirma escalada libre, más oro de los enemigos durante
la partida que aumente con dificultad/tiempo, y arte original en archivos.
El siguiente paso inmediato pasa a ser preparar un Proyecto de ChatGPT y, en su
primera conversación, instalar/verificar Codex CLI en Windows para trabajar con
el repositorio y Unity. No se ha instalado Codex en el equipo del autor desde aquí.

El autor menciona una captura del Editor, pero no llegó una imagen legible a esta
conversación. La revisión exacta sigue pendiente; no deducirla de «Unity 6.6».
Los documentos exportados para el Proyecto de ChatGPT son una instantánea; el
estado posterior debe consultarse en el repositorio y guardarse en estos Markdown.
