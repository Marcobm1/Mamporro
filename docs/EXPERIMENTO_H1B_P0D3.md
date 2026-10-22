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

Evidencias en `unity/TestResults/P0/H1b-20261022`.

### Puerta A favorable; B en ejecución

Comando real: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.AdaptiveCorridorTests -Category AdaptiveGateA`. **7/7**, 80,131 s, `Regional-20261022T182040/edit.xml`. Intento `180156` sin pruebas por licencia 198; repetición fuera del sandbox. Extractor: `python scripts/p0-h1b-report.py unity/TestResults/P0/Regional-20261022T182040/edit.xml unity/TestResults/P0/H1b-20261022/gate-a.json A`.

21 evaluaciones por variante (Z9K2 S/M/L y seis fixtures S/M/L). Control H1: 21/21 fallan, peor p95 2,42×, máximo 173,82×. Adaptativa: 1/21 falla, peor p95 1,46308×, máximo 2,60597×. Con local: 0/21 fallan, peor p95 1,28717×, máximo 1,93920×. Completa ponderada/local: **0/21 fallan, peor p95 1×, máximo 1,49005×**. Peor par anterior reproducido: H1 8691 frente a óptimo 50; las tres variantes obtienen 50. Las variantes parciales son controles de contribución, no candidatos adoptados por separado.

Cero discrepancias de alcance/identidad de arista/continuidad. Cada padre alcanzable también certifica coste descendente por arista original, no solo las rutas reconstruidas. Máximo cociente de trabajo completo/H1 por evaluación: **1,003584×**, bajo el filtro 2×. Contabilidad comparable de visitas/extracciones/relajaciones; operaciones de heap de H1 no instrumentadas, H1b sí las informa aparte: no interpretar ese cociente como coste total de instrucciones o aceleración CPU.

Preparación registrada Z9K2: **4686,645 ms** (original + D1/D2 + índice S/M/L), arrays del instrumento **8272793 B**, arrays físicos/topología/instrumento **44369052 B**, scratch explícito máximo por construcción de índice **173216 B**. Managed observado máximo 130605056 B, Unity 356156867 B; incluyen arnés/control/oráculos. Sin exceso de los filtros duros observados de A, pero **no certifica p95 ≤4 s, residencia, picos ni GC**. Fixtures excluyen el constructor monolítico usado solo como oráculo; no son carga jugable comparable a C.

Se abre B con el algoritmo y muestras prefijados; no se alteran pesos/radio ni umbrales. Los costes mundiales offline siguen siendo elevados: Z9K2 S, variante completa, hasta **749390** visitas/extracciones/relajaciones por meta, además de heap y salida de rutas. Compartir todos los orígenes evita repetir búsquedas por enemigo, pero no demuestra reacción a 3000 ni servicio móvil viable.
