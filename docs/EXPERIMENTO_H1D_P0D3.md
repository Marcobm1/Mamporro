# H1d — reutilización exacta de grafos validados

## Contrato previo — 23/10/2026

Autorización exclusiva del autor desde `4795500`, HEAD/origin comprobados tras fetch. Solo tests, scripts y documentación, sin investigación avanzada. Conservar H1b, producción, generador, geometría, validaciones, 3000 globales/tick, siete ajustes Unity, nueve protecciones y once referencias. No servicios nuevos ni H2–H4/D4/D5. Parada para revisión al terminar, incluso si favorable.

**A, antes de B:** vista regional que referencia los NavGraph originales; propiedad única y orden regional de nodos, aristas físicas con referencias a trayectorias originales, portales completos, índices y SCC. No construir NavGraph en el constructor de la vista ni depender del control D1/D2 para producir datos. Comparar contra D1/D2 contemporáneo A/B/ocho C ×16/32/64×S/M/L y seis fixtures (barrera/reentrada, capas, caída unilateral, escalada, rampa diagonal y paso por tamaño) en los tres tamaños. Igualdad biyectiva de nodos, capas/alturas, todas las aristas con multiplicidad, coste/tipo/trayectoria, propiedad, portales y particiones/arcos SCC. La igualdad del grafo dirigido certifica el mismo alcance; añadir consultas contra oráculo monolítico y controles físicos. Un fallo real de equivalencia detiene A y no abre B; no cambiar el oráculo.

**Compatibilidad H1b:** conservar fuente original. Adaptación mecánica solo del tipo de mundo en una copia de tests del solver, sin cambiar algoritmo, pesos, radio ni desempates. Verificar dicha transformación byte a byte normalizando saltos de línea. Comparar distancias/potenciales y padres físicos con H1b contemporáneo, y calidad frente a oráculo con las mismas metas/orígenes del corpus H1b, incluidos Z9K2 62→10514 y RAMPA42 12654→5393. No usar la unión local fallida de H1c.

**B, solo tras A favorable:** diez arranques completos independientes de cada alternativa por semilla crítica MAMPORRO/Z9K2/RAMPA42 ×16/32/64. Son los tres casos H1c que requieren dos tandas originales por rechazo/reparación y fallan 4 s en todos los tamaños. Alternar el orden control/prototipo por repetición. Cada arranque crea su propio mapa/circuito, validación y tres grafos; el prototipo añade propiedad/SCC/portales/índice H1b. El control añade D1/D2/índice H1b. Conservar los tres mundos/índices simultáneamente en ambos; no atribuir al prototipo los datos ya creados por el control. Comparación H1c heredada identificada aparte.

Cronómetro exterior real con generación, comprobación previa, grafos acumulados de todos los intentos, validación, proyección y solver; residual explícito. Separar mediciones instrumentales y verificaciones del coste de carga, informar ambos y no sumar CPU de thread a tiempo de pared. Recoger bytes asignados del hilo, GC, payload de arrays compartidos una sola vez, memoria managed/Unity observada y muestreo de pico managed; distinguir transitorios observados de cota/medición incompleta. No convertir muestreo en garantía del pico ni payload en residencia certificada. Objetivos 4 s p95/6 s máximo; navegación 160 MiB, transitorios 96 MiB, managed QA 256 MiB, Unity 512 MiB, instrumento 20 MiB. Si no puede certificarse una medida, marcarla sin certificar; no adoptar automáticamente.

Percentil de rango más próximo, por semilla/tamaño/alternativa; con n=10 p95 coincide con máximo observado y no es garantía poblacional. Registrar ahorros ejecutados de cargas completas, sin restar trabajo hipotético. Persisten costes mundiales por meta de H1b y no hay prueba de servicio móvil/FPS.

## Resultados

Pendientes. Evidencias locales: `unity/TestResults/P0/H1d-20261023`. Próximo paso: implementar y evaluar A; B bloqueada hasta equivalencia favorable.
