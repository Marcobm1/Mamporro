"""Resume las mediciones D2 de un XML existente; no ejecuta ni altera pruebas."""
import argparse
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


def read(path):
    root = ET.parse(path).getroot()
    rows = {}
    for case in root.iter("test-case"):
        for line in (case.findtext("output") or "").splitlines():
            if not line.startswith(("D2 ", "D2-query ")):
                continue
            prefix, label, _ = line.split(" ", 2)
            values = {}
            for name, value in re.findall(r"(\w+)=([^ ]+)", line):
                if value in ("True", "False"):
                    values[name] = value == "True"
                else:
                    values[name] = float(value) if "." in value else int(value)
            key = (label, values["class"], values["size"])
            row = rows.setdefault(key, {"fixture": label, "test": case.attrib["name"], "result": case.attrib["result"]})
            row.update(values)
    corpus = [r for r in rows.values() if r["fixture"] in ("A", "B", "P0QA", "MAMPORRO", "COLINAS7", "Z9K2", "MESETA", "ACANTILADO", "RAMPA42", "HORDA750")]
    measures = ("portals", "localScc", "localWeak", "globalScc", "arcs", "buildMs", "topologyMs", "physicalArrays", "topologyArrays", "portalBytes", "queryArrays", "coldReachMaxMs", "coldRouteMaxMs", "warmQueryUs", "managedSampledPeak")
    groups = []
    for size in (16, 32, 64):
        group = [r for r in corpus if r["size"] == size]
        groups.append({"size": size, "samples": len(group), **{m: {"min": min(r[m] for r in group), "max": max(r[m] for r in group)} for m in measures if group and all(m in r for r in group)}})
    return {"source": str(path), "result": root.attrib.get("result"), "passed": root.attrib.get("passed"), "failed": root.attrib.get("failed"), "rows": list(rows.values()), "rangesBySize": groups,
            "limitations": "Editor; tiempos de lógica, no FPS. Pico gestionado muestreado incluye otros objetos. Arrays sin cabeceras/listas/geometría. Asignaciones -1=N/D si falla el testigo. Rangos entre casos, no p95 de repeticiones."}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("xml", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    result = read(args.xml)
    args.output.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k not in ("rows", "limitations")}, ensure_ascii=False, indent=2))
