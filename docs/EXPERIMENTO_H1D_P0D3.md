# H1d — reutilización exacta de grafos validados

## Contrato previo — 23/10/2026

Autorización exclusiva del autor desde `4795500`, HEAD/origin comprobados tras fetch. Solo tests, scripts y documentación, sin investigación avanzada. Conservar H1b, producción, generador, geometría, validaciones, 3000 globales/tick, siete ajustes Unity, nueve protecciones y once referencias. No servicios nuevos ni H2–H4/D4/D5. Parada para revisión al terminar, incluso si favorable.

**A, antes de B:** vista regional que referencia los NavGraph originales; propiedad única y orden regional de nodos, aristas físicas con referencias a trayectorias originales, portales completos, índices y SCC. No construir NavGraph en el constructor de la vista ni depender del control D1/D2 para producir datos. Comparar contra D1/D2 contemporáneo A/B/ocho C ×16/32/64×S/M/L y seis fixtures (barrera/reentrada, capas, caída unilateral, escalada, rampa diagonal y paso por tamaño) en los tres tamaños. Igualdad biyectiva de nodos, capas/alturas, todas las aristas con multiplicidad, coste/tipo/trayectoria, propiedad, portales y particiones/arcos SCC. La igualdad del grafo dirigido certifica el mismo alcance; añadir consultas contra oráculo monolítico y controles físicos. Un fallo real de equivalencia detiene A y no abre B; no cambiar el oráculo.

**Compatibilidad H1b:** conservar fuente original. Adaptación mecánica solo del tipo de mundo en una copia de tests del solver, sin cambiar algoritmo, pesos, radio ni desempates. Verificar dicha transformación byte a byte normalizando saltos de línea. Comparar distancias/potenciales y padres físicos con H1b contemporáneo, y calidad frente a oráculo con las mismas metas/orígenes del corpus H1b, incluidos Z9K2 62→10514 y RAMPA42 12654→5393. No usar la unión local fallida de H1c.

**B, solo tras A favorable:** diez arranques completos independientes de cada alternativa por semilla crítica MAMPORRO/Z9K2/RAMPA42 ×16/32/64. Son los tres casos H1c que requieren dos tandas originales por rechazo/reparación y fallan 4 s en todos los tamaños. Alternar el orden control/prototipo por repetición. Cada arranque crea su propio mapa/circuito, validación y tres grafos; el prototipo añade propiedad/SCC/portales/índice H1b. El control añade D1/D2/índice H1b. Conservar los tres mundos/índices simultáneamente en ambos; no atribuir al prototipo los datos ya creados por el control. Comparación H1c heredada identificada aparte.

Cronómetro exterior real con generación, comprobación previa, grafos acumulados de todos los intentos, validación, proyección y solver; residual explícito. Separar mediciones instrumentales y verificaciones del coste de carga, informar ambos y no sumar CPU de thread a tiempo de pared. Recoger bytes asignados del hilo, GC, payload de arrays compartidos una sola vez, memoria managed/Unity observada y muestreo de pico managed; distinguir transitorios observados de cota/medición incompleta. No convertir muestreo en garantía del pico ni payload en residencia certificada. Objetivos 4 s p95/6 s máximo; navegación 160 MiB, transitorios 96 MiB, managed QA 256 MiB, Unity 512 MiB, instrumento 20 MiB. Si no puede certificarse una medida, marcarla sin certificar; no adoptar automáticamente.

Percentil de rango más próximo, por semilla/tamaño/alternativa; con n=10 p95 coincide con máximo observado y no es garantía poblacional. Registrar ahorros ejecutados de cargas completas, sin restar trabajo hipotético. Persisten costes mundiales por meta de H1b y no hay prueba de servicio móvil/FPS.

## Resultados

Evidencias locales: `unity/TestResults/P0/H1d-20261023`.

### Puerta A favorable

Contrato publicado `a6dd6aa`. Primera ejecución `Regional-20261023T190923`: **24/48**, 74,549 s. El comparador inicial exigía igualdad de coste por ordinal de arista y falló en las ocho semillas C. `NavGraph.BuildEdges` ordena por origen/destino, sin desempate de paralelas; el orden entre aristas físicas paralelas puede variar al ordenar el grafo mundial o regional. Se conserva ese XML y `gate-a.json`. No se alteran prototipo, generador, geometría ni oráculo para corregirlo.

Comparador corregido: biyección consumible una sola vez entre aristas con mismo origen/destino, coste, tipo y trayectoria física completa (puntos, normal, plano y salida), con igual cardinalidad y sin descartar duplicados. Los portales se emparejan usando esa misma biyección. No equivale a ignorar diferencias de coste: cada coste debe existir exactamente con su multiplicidad y trayectoria. El ordinal de arista deja de confundirse con identidad física; los padres del solver se comprueban por aristas físicas válidas y descenso de coste, no por igualdad de ordinal entre representaciones.

Repetición `Regional-20261023T191239`: **48/48**, 127,782 s, **144 evaluaciones** A/B/ocho C/seis fixtures ×16/32/64×S/M/L. **3621138 nodos, 27772185 aristas, 705479 portales** comprobados; 440 correspondencias de arista con distinto ordinal. Propiedad única, claves/capas/alturas, consultas de nodo, universos completos con multiplicidad, SCC locales/globales, componentes débiles y arcos iguales. Biyección completa del grafo dirigido: alcance idéntico para cualquier pareja; además controles de reentrada, caída sin retorno, tejados superpuestos, escalada y paso S/M frente a L. No es una simulación de cuerpos.

El prototipo `ReusedRegionalWorld` no construye `NavGraph`; crea propiedad/índices/portales sobre el grafo validado y conserva referencias a sus trayectorias. El kernel de topología se copia sin cambios de D2 a tests. `ReusedCorridorProbe` es una copia mecánica de H1b sustituyendo únicamente dos nombres de tipos. Verificación reproducible: `python scripts/p0-h1d-report.py --verify-solver`; fuente H1b original normalizada SHA256 `c63d5aecaf797f9b973cbb1b01c795ce9201a4f9452dea36668d758a2b51aa88`. Ninguna clase nueva se usa en producción.

Comando A: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/p0-regional-tests.ps1 -Filter Mamporro.Tests.GraphReuseTests -Category GraphReuseA`. Intento `190918` sin tests por licencia 198 en sandbox, repetido fuera. Extractor: `python scripts/p0-h1d-report.py unity/TestResults/P0/Regional-20261023T191239/edit.xml unity/TestResults/P0/H1d-20261023/gate-a-bijection.json`. A favorable abre B; no acredita todavía tiempos, memoria ni rodeos de la vista.

### Puerta B

En curso. Compatibilidad de rutas primero, cargas independientes después. No adopción ni H2.
