"""Extrae las medidas D3 bajo demanda; no transforma pendientes en éxitos."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET


def read(paths):
    tables = {name: [] for name in ("D3-demand", "D3-demand-total", "D3-demand-neighbor")}
    sources = []
    for run, path in enumerate(paths):
        root = ET.parse(path).getroot()
        if root.get("result") != "Passed":
            raise ValueError(f"Suite fallida: {path}")
        sources.append(dict(path=str(path), passed=int(root.get("passed"))))
        for output in root.iter("output"):
            for line in (output.text or "").splitlines():
                parts = line.split()
                if not parts or parts[0] not in tables:
                    continue
                row = {"run": run}
                for item in parts[1:]:
                    key, value = item.split("=", 1)
                    row[key] = value if key in ("distribution", "mode") else float(value) if "." in value else int(value)
                tables[parts[0]].append(row)
    for name, count in (("D3-demand", 162), ("D3-demand-total", 54), ("D3-demand-neighbor", 18)):
        rows = tables[name]
        keys = ("run", "size", "population", "distribution") + (() if name.endswith("neighbor") else ("mode",)) + (("class",) if name == "D3-demand" else ())
        if len(rows) != count * len(paths) or len({tuple(row[k] for k in keys) for row in rows}) != count * len(paths):
            raise ValueError(f"Corpus incompleto/duplicado: {name}, {len(rows)}")
    for row in tables["D3-demand"]:
        if row["work"] != sum(row[k] for k in ("maintenance", "init", "index", "pops", "edges", "seeds", "publish", "control")):
            raise ValueError("Trabajo omitido")
    for row in tables["D3-demand-total"]:
        if row["work"] > 10800 * 3000:
            raise ValueError("Techo global excedido")
    deterministic = {}
    for row in tables["D3-demand"]:
        key = tuple(row[k] for k in ("size", "population", "distribution", "mode", "class"))
        values = {k: v for k, v in row.items() if k not in ("run", "cpuMs")}
        if key in deterministic and deterministic[key] != values:
            raise ValueError(f"Repeticiones no deterministas: {key}")
        deterministic[key] = values
    return dict(sources=sources, budget=3000, hz=60, seconds=180, tables=tables,
                deterministic_repetitions=True,
                limitations="Demandas sintéticas con orígenes fijos, no persecución ni FPS. "
                "Percentiles de respuesta solo de publicaciones completadas; revisar pendientes, "
                "cobertura y publicaciones obsoletas. Edad cronológica no es retraso en meta fija. "
                "Payload parcial; heap incluye Editor/arnés. allocated=-1 indica contador inválido.")


if __name__ == "__main__":
    report = read([pathlib.Path(p) for p in sys.argv[2:]])
    pathlib.Path(sys.argv[1]).write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print({key: len(value) for key, value in report["tables"].items()})
