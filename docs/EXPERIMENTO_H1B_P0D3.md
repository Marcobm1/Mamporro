# H1b — cruces adaptativos y corredores ponderados

## Contrato previo a medir — 22/10/2026

Autorización exclusiva H1b desde `9154ec8`. H1 permanece conservado y rechazado. No investigación avanzada, producción, EnemyNavigation, generador, H2–H4, D4/D5 ni ampliación. Techo de juego intacto: 3000 unidades globales/tick. Nueve huellas y once referencias verificadas; siete ajustes locales fuera de commits.

Instrumento offline en tests sobre D1/D2. Tres variantes acumulativas fijadas antes de resultados:

1. **Adaptativa:** conserva árbol por saltos H1, pero permite todos los portales físicos de cada conexión elegida. Dijkstra inverso restringido calcula el coste real desde cada origen; el cruce elegido depende del origen y la meta, sin testigos fijos.
2. **Local:** añade las aristas originales cuyos dos extremos están a ≤32 m XYZ de la meta. Es un dominio físico local, no una conexión recta inventada; las rutas pueden seguir usando el corredor fuera del dominio. No presume que un radio certifique inaccesibilidad.
3. **Ponderada + local:** potencial macro inverso con peso por conexión = mínimo de distancia centroide-origen de portal + coste original + distancia destino de portal-centroide, escalado con NavGraph.Scale y mínimo 1. Centros medios por SCC y clase. Permite todos los arcos que descienden estrictamente el potencial, además del dominio local. La estimación no es una cota admisible ni una distancia física garantizada; solo selecciona corredores, cuyo coste se resuelve después con aristas originales.

Alcance: existe al menos el árbol de descenso hacia la meta para toda SCC alcanzable; las SCC conservan sus recorridos internos. Verificar contra monolítico, incluyendo arcos internos de región, alturas, S/M/L y trayectorias completas. El campo físico final tiene padres de coste descendente incluso si el dominio local permite subir potencial macro. Sin matrices de transferencias ni consulta global por consumidor: una búsqueda inversa por clase/meta comparte resultado entre todos los orígenes. Es instrumentación mundial offline O(N+E), no servicio H2, caché, cuotas ni movimiento. No ocultar su coste ni afirmar que está amortizado con metas móviles.

**Puerta A:** Z9K2 64 m S/M/L (reproducción explícita S 62→10514: H1 dos testigos 8691, óptimo 50), más seis fixtures H1 a 16 m (barrera/reentrada, tejados/capas, caída unilateral, escalada, rampa diagonal, paso S/M excluido de L). Mismas metas/orígenes H1, añadiendo el peor par; control contemporáneo H1 dos testigos. Contrastar alcance desde todos los nodos para cada meta y reconstruir las rutas muestreadas, incluidas las adyacentes, cero y desconectadas. No eliminar resultados negativos de las variantes.

A favorable exige exactitud, variante completa con p95 ≤1,5× y máximo ≤2× en cada evaluación pequeña, peor par corregido, sin exceder preparación máxima 6 s ni arrays contados de navegación 160 MiB o instrumento 20 MiB. Comparar trabajo/CPU con H1 contemporáneo; un incremento >2× del trabajo por caso impide abrir B. Es un filtro conservador de viabilidad, no certificación de los presupuestos completos. Mantener objetivos p95 preparación ≤4 s, residencia ≤160 MiB, transitorios ≤96 MiB, managed QA ≤256 MiB, Unity ≤512 MiB; muestras parciales no certifican residencia ni picos. Ante fallo de A, completar diagnóstico de esa matriz pequeña y detenerse, sin modificar pesos/radio después de medir para forzar aprobación.

**Puerta B, solo si A favorable:** A/B/ocho C ×16/32/64×S/M/L; mismo oráculo, percentiles y muestras H1, conservando fixtures y peor par. Exigir 1,5×/2×, preparación/memoria compatibles y estudiar compartición real. Certificación de p95 temporal exige repeticiones suficientes; no convertir una muestra en garantía. Parada ante fallo y siempre antes de H2.

Registrar preparación original, D1/D2 e índice por separado; arrays propios, físicos/topología y memoria observada del arnés. Trabajo: inicialización, arcos, extracciones, aristas físicas, relajaciones y operaciones/comparaciones de heap separadas; reconstrucción/copias separadas. Oráculos/aserciones fuera del tiempo solver. Ningún cociente trabajo/3000 se presentará como latencia o FPS.

## Resultados

Pendientes de ejecución. Evidencias en `unity/TestResults/P0/H1b-20261022`.
