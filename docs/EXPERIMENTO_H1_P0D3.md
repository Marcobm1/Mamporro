# H0–H1 híbrido P0-D3

## H0: contrato fijado antes de medir — 21/10/2026

Base `563f05e`, HEAD/origin coincidentes tras fetch; nueve protecciones y once referencias comprobadas. Autoriza exclusivamente H0–H1. Arquitectura A, alcance/física exactos y coste aproximado con rodeo p95 ≤1,5× y máximo ≤2×. Bandas 24/32 y 48/60 m, jefe crítico, reacción experimental 0,3/0,5; 0,6/1; 1,5/2 s aprobadas como contrato experimental; comparar también 0,3/0,5 originales. Cuotas y criterios de persecución/colas solo aceptados para estudiar, no validados. H1 no implementa bandas, scheduler, movimiento ni refinamiento cercano (H2+).

Prototipo exclusivamente en ensamblado de tests sobre NavPortalWorld. Grafo de SCC locales existente, todos sus arcos incluidos los internos de región. Árbol inverso por número de saltos, desempate por ID de componente; cada conexión conserva su lista íntegra de aristas originales. Primer testigo: mediana de IDs de arista de esa conexión. Si falla rodeo, segunda variante conserva ese testigo y añade el de mayor distancia XYZ desde él, desempate por ID; selección fijada antes de resultados. No cambiar después de ver medidas.

Reconstrucción offline: Dijkstra inverso limitado a aristas internas de SCC y testigos que siguen el árbol; obtiene simultáneamente rutas físicas desde todos los nodos. Es una herramienta H1 para comprobar el corredor, **no servicio local H2 ni trabajo gratuito durante juego**. Todas las búsquedas y reconstrucciones son offline, sin tick simulado, FPS ni afirmación de cumplir reacción; scheduler/3000 de producción intactos. Una variante de dos testigos solo se ejecuta si la primera incumple rodeo en el caso, posibilidad expresamente autorizada. Si tampoco cumple, detener candidato al terminar diagnóstico/documentación, no optimizar más.

Corpus fijado: A/B y ocho C × 16/32/64, S/M/L. Metas distribuidas por identidad física, una por SCC mundial y metas vecinas de cruces para detectar rodeos de frontera. Para cada meta, alcance de **todos los nodos** contra oráculo monolítico inverso por cola; rutas desde 24 orígenes distribuidos y origen de cruce adyacente. Comparar cada arista recorrida con la identidad física monolítica (clase/capa/coste/tipo/trayectoria). Fixtures adicionales de barrera con salida/reentrada, placas desconectadas, caída unilateral con voladizo, escalada y rampa/paso corporal. Percentil por rango más próximo, sin excluir rutas cortas; coste cero exige ruta vacía. Reportar variante/tamaño/clase por separado y peor par.

Preparación separa circuito original, D1/D2 e índice/corredor H1; no incluir oráculo/aserciones en CPU del solver. Memoria: arrays físicos, topología/portales y arrays propios explícitos; muestras managed/Unity incluyen arnés y no certifican picos/residencia. Carga y memoria se contrastan con PLAN_P0D, sin afirmar cumplimiento global si faltan mediciones. Pruebas de diagnóstico pueden pasar al capturar correctamente una puerta de rendimiento negativa; el informe debe distinguir ambos resultados.

**Alcance comprobado H0→H1:** únicamente topología, rutas offline y evaluación; no EnemyNavigation, H2–H4, D4/D5, mundo mayor, web ni referencias. Detenerse antes de H2 incluso si H1 pasa.

## Resultados

Pendientes de ejecución. No hay aceptación de la puerta ni medidas nuevas todavía.
