# P0-D — construcción y navegación regional

Implementación optativa autorizada D1–D4; D5 excluido. Mapas de prueba 80/128 m; sin integración en partidas normales ni cambios de generador, balance o presupuesto de 3000 nodos/tick.

## D1: constructor físico acotado (verificado el 15/10/2026)

`NavRegionGraph` construye una baldosa de 16/32/64 m y contexto de nodos. Coordenadas absolutas, alineadas a la rejilla mundial de 0,5 m. Todas las capas de una celda pertenecen a la misma baldosa. El nodo de origen posee cada arista y toda su trayectoria; los nodos de halo no generan aristas. La unión por celda mundial/capa/clase permite reconstruir la misma topología física sin duplicar propietarios.

El halo inicial es al menos 10 m: supera la búsqueda `SurfaceBeyond` de L (8,05 m), la caída por eje (6,5 m) y los vecinos Walk. No se supone suficiente para cualquier pared inclinada. Las búsquedas de destinos de escalada que salen del recorte, pero permanecen en el mundo, marcan contexto insuficiente; se duplica el halo y se reconstruye antes de devolver la región. Como límite se usa el mundo completo. Las consultas de cuerpo, apoyo, pared y barrido siempre usan la geometría mundial, por lo que una trayectoria no se recorta en la costura.

Las tapas se enumeran mediante candidatos espaciales ordenados, copiados a un búfer local antes de consultas que reutilizan el búfer de `VerticalQueries`. La construcción secuencial comparte las consultas mundiales, sin copiar su tabla de terreno por baldosa. No es todavía un constructor paralelo ni un sistema de residencia/prefeteo. No compartir el mismo búfer de consultas concurrentemente.

El constructor monolítico público conserva su recorrido y sigue siendo el control de A/B/C. No se activa el constructor regional desde el juego. El único cambio heredado de movimiento es la corrección aprobada y publicada en `5e41406`, ver [DIAGNOSTICO_P0D0](DIAGNOSTICO_P0D0.md).

## Validación

`RegionalGraphTests` compara nodos, alturas, identidad de capa/terreno, cada arista dirigida, coste, destino y cada punto/normal de escalada o caída contra el monolítico. Exige propietario único, cobertura completa y ausencia de aristas en los nodos de contexto. Comprueba alcance desde origen tras reconstruir la unión, y el fixture de costuras también se construye en orden inverso.

Corpus: A/B a 80 m; ocho semillas aprobadas C a 128 m; tres tamaños de baldosa y tres clases. Fixture adicional con rampa diagonal, capas superpuestas, esquinas, estructura mayor que la baldosa y paso estrecho. Los payloads informados son arrays directos de los grafos (incluidos halos), no memoria residente completa; los tiempos de construcción excluyen las aserciones de comparación. Suite regional del 15/10/2026: 28/28, 268,795 s, XML/log en `unity/TestResults/P0/Regional-20261015T173655`. Regresiones completas nuevas: Edit 757/757 y Play 53/53; XML/log en `unity/TestResults/P0/D1-20261015`. Referencias históricas coincidentes y nueve huellas protegidas intactas.

## Pendientes

D2: portales dirigidos y conectividad global, sin confundir todas las capas/componentes de una baldosa. D3: campos locales, invalidación y reparto justo, con la inanición M/L de D0 como problema prioritario; nunca aumentar 3000 para ocultarla. D4: cruces físicos y residencia/prefeteo. Render/mundo ampliado e integración normal excluidos.

## Medidas D1 del 15/10/2026

Comando: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\p0-regional-tests.ps1`. Ejecución nueva en Editor, sin render. El intento heredado del 14/10 falló por licencia 198 y no generó XML; no cuenta como prueba. CSV derivado del XML: `unity/TestResults/P0/D1-20261015/regions.csv`.

| Baldosa | Construcción por clase, ocho semillas C (ms mín.–máx.) | Arrays de todas las regiones por clase (B mín.–máx.) | Mayor región (B) |
| --- | --- | --- | --- |
| 16 m | 843,286–1881,051 | 5 952 053–12 149 305 | 270 907 |
| 32 m | 720,214–1232,990 | 4 498 858–9 695 945 | 827 390 |
| 64 m | 619,110–1038,254 | 4 015 447–8 818 172 | 2 307 305 |

HORDA750, suma de S/M/L: 16 m = 3444,024 ms / 28 492 587 B; 32 m = 2737,105 ms / 22 290 275 B; 64 m = 2371,721 ms / 20 242 963 B. Control monolítico: 19 258 640 B en sus arrays directos. Los halos aumentan el total de datos aunque reducen el tamaño de cada pieza. No se afirma ahorro de residencia global, p95, coste de carga completo ni cumplimiento de D3/D4; faltan campos, portales, listas internas, geometría y transitorios. No se fija aún tamaño definitivo: las regiones menores también ofrecen campos locales menores, todavía sin medir.

La igualdad exacta de todos los nodos/aristas y puntos de trayectoria acredita equivalencia física con el oráculo en este corpus. No demuestra cobertura universal ni sustituye las pruebas dinámicas de cruces y residencia de D4. Se conservan clases, envolventes, trayectoria completa, generador y presupuesto 3000. El controlador de enemigos continúa usando el backend histórico.
