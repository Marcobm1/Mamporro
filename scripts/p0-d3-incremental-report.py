"""Medidas del comparador D3; no acredita persecución ni FPS."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET


def read_report(path):
    root = ET.parse(path).getroot()
    if root.get("result") != "Passed":
        raise ValueError("Suite fallida o incompleta")
    classes, ticks = [], []
    for output in root.iter("output"):
        for line in (output.text or "").splitlines():
            parts = line.split()
            if not parts or parts[0] not in ("D3-incremental", "D3-incremental-total"):
                continue
            row = {}
            for part in parts[1:]:
                key, value = part.split("=", 1)
                row[key] = value if key == "mode" else float(value) if "." in value else int(value)
            for key, value in list(row.items()):
                if value == -1:
                    row[key] = None
            (classes if parts[0] == "D3-incremental" else ticks).append(row)
    expected = {(size, mode, cls) for size in (16, 32, 64)
                for mode in ("cold-fixed", "warm-moving") for cls in range(3)}
    if len(classes) != 18 or {(r["size"], r["mode"], r["class"]) for r in classes} != expected:
        raise ValueError("Corpus de clases incompleto o duplicado")
    if len(ticks) != 6 or {(r["size"], r["mode"]) for r in ticks} != {(s, m) for s, m, _ in expected}:
        raise ValueError("Corpus de ticks incompleto o duplicado")
    return dict(source=str(path), passed=int(root.get("passed")), budgetPerTick=3000,
                simulatedSecondsPerMode=180, classes=classes, ticks=ticks,
                limitations="Editor sin render ni enemigos. Meta móvil alterna dos vecinos cada 12 ticks. "
                "Edad móvil solo tras primera reparación publicada; espera inicial en latencia. "
                "Edad fija N/D: la meta no cambia. Payload excluye grafos, cabeceras y transitorios. "
                "Asignaciones null si falla el testigo. CPU solo Tick, no preparación ni movimiento. "
                "Percentiles fríos incluyen ticks ociosos; activeP95Ms los excluye. "
                "No sustituye comparación física monolítica ni certifica objetivos D3.")


if __name__ == "__main__":
    result = read_report(pathlib.Path(sys.argv[1]))
    target = pathlib.Path(sys.argv[2])
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Informe: {len(result['classes'])} clases, {len(result['ticks'])} escenarios; {target}")
