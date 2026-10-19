"""Extrae evidencias del experimento exacto D3 sin reinterpretarlas como FPS."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET


def read(paths):
    tables = {"D3-groups": [], "D3-batch": [], "D3-batch-total": [], "D3-batch-region": []}
    sources = []
    for path in paths:
        root = ET.parse(path).getroot()
        if root.get("result") != "Passed":
            raise ValueError(f"Suite fallida: {path}")
        sources.append(dict(path=str(path), passed=int(root.get("passed"))))
        for output in root.iter("output"):
            for line in (output.text or "").splitlines():
                parts = line.split()
                if not parts or parts[0] not in tables:
                    continue
                row = {}
                for item in parts[1:]:
                    key, value = item.split("=", 1)
                    row[key] = value if key in ("seed", "mode") else float(value) if "." in value else int(value)
                tables[parts[0]].append(row)
    for name, fields, expected in (("D3-groups", ("seed", "size", "class"), 90),
                                  ("D3-batch", ("size", "repeat", "mode", "class"), 162),
                                  ("D3-batch-total", ("size", "repeat", "mode"), 54)):
        rows = tables[name]
        if rows and (len(rows) != expected or len({tuple(r[k] for k in fields) for r in rows}) != expected):
            raise ValueError(f"Corpus incompleto o duplicado: {name}")
    for row in tables["D3-batch-total"]:
        if row["work"] > 3000 * row["ticks"]:
            raise ValueError("Presupuesto excedido")
    return dict(sources=sources, budget=3000, hz=60, tables=tables,
                limitations="Editor sin enemigos ni render. Payload de arrays, no residencia total. "
                "Asignaciones -1=N/D si falla el testigo. Grupos e instancia se construyen en carga; "
                "índices, inicialización, propagación y publicación se cobran en Advance.")


if __name__ == "__main__":
    result = read([pathlib.Path(p) for p in sys.argv[2:]])
    target = pathlib.Path(sys.argv[1])
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print({key: len(rows) for key, rows in result["tables"].items()})
