# H1c — preparación y reutilización de navegación

## Contrato previo a medir — 22/10/2026

Base real `b5b343d`, HEAD/origin coincidentes tras fetch. Autoriza exclusivamente diagnóstico técnico, instrumentación, pruebas y scripts. H1b se conserva íntegro como referencia de calidad; sin investigación avanzada, cambios de producción/generador/EnemyNavigation/U3/web/economía/guardados, servicio nuevo, H2–H4 ni D4/D5. 3000 globales/tick, siete ajustes, nueve huellas y once referencias intactos. Parada al finalizar H1c incluso si hay resultados positivos.

**Preparación:** P0QA, MAMPORRO, HORDA750, Z9K2 y RAMPA42 ×16/32/64×tres arranques. Usar los cronómetros existentes de ProceduralMap (generación, comprobación previa, grafos, validación, total) y NavPortalWorld/NavGraph (D1 físico, D2 topología, nodos/escalada/caída). Medir envoltura de circuito e índice H1b. No omitir reparaciones, intentos rechazados ni validaciones. Registrar suma de componentes, residual y total real. Tres muestras describen variación; no certifican p95 de producción. Mantener objetivos 4 s p95 / 6 s máximo.

Diagnóstico de duplicación: nodos de halo, intentos regionales, trayectorias y correspondencia completa de aristas con los grafos originales ya validados. Construir solo en tests un índice de región/SCC/arcos sobre el grafo original, sin reconstruir geometría ni eliminar validaciones; contrastar su partición con D2. No crear adaptador de producción ni sustituir NavPortalWorld. Tiempo de índice/certificación medido aparte. Cualquier total que sustituya D1/D2 por esta proyección es **estimación de una preparación futura incompleta**, no ahorro de carga ejecutado; informar también todo el trabajo real del diagnóstico, incluido el control D1/D2.

**Reutilización:** mismos cinco mapas y tamaños, S/M/L. Anclas distribuidas por identidad física, ancla elevada y peor par Z9K2; metas vecinas, a ~8/~24 m y lejanas dentro de la misma SCC. Controles de cambio de SCC obligan a invalidar reutilización. Comparar el potencial macro completo H1b antes/después, sin modificar H1b. El potencial depende solo de la SCC objetivo y la revisión geométrica; la máscara local de 32 m depende además del nodo real.

Sonda offline de refinamiento compartido: dominio = SCC objetivo completa más nodos a ≤32 m XYZ de cualquiera de las dos metas. Dijkstra inverso solo con aristas originales internas al dominio. Conectar el prefijo del campo H1b anterior con el primer nodo resuelto del campo local nuevo; no seguir hasta el ancla si antes hay una unión válida. Prefijo y sufijo usan aristas físicas, sin línea recta nueva. Dentro de una misma SCC, la meta anterior debe llegar a la nueva por aristas internas; comprobarlo. Si cambia SCC, no usar la unión como fallback universal. No publicar campos/versiones ni ejecutar scheduler/cuerpos: es un cálculo diagnóstico por pareja, no servicio H2.

Cobrar creación del índice inverso, barrido/inicialización mundial del diagnóstico local, aristas examinadas, extracciones, relajaciones, heap y reconstrucción por separado. Incluir el campo frío anterior, no llamarlo gratuito. Contrastar alcance y rutas contra oráculo monolítico, calidad p95 ≤1,5× / máximo ≤2× por caso/clase, conservar fallos, rutas cortas y metas imposibles. Comparación con H1b actualizado contemporáneo. El ahorro del solver local es medido; ahorro por evitar máscaras/listas/índices mediante caché no implementada es estimado. No convertir operaciones offline en unidades scheduler, latencias o FPS.

## Resultados

Pendientes. Evidencias en `unity/TestResults/P0/H1c-20261022`.
