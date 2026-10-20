"""Extrae la puerta de viabilidad; nunca convierte un corte en tiempo completo."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def extract(path):
    root = ET.parse(path).getroot()
    if root.attrib.get("result") != "Passed":
        raise ValueError(f"Suite no correcta: {path}")
    rows = {"counts": [], "gates": []}
    for case in root.iter("test-case"):
        for output in case.findall("output"):
            for line in (output.text or "").splitlines():
                if not line.startswith(("PRECOUNT ", "PREGATE ")):
                    continue
                tag, *fields = line.split()
                row = {}
                for field in fields:
                    key, value = field.split("=", 1)
                    row[key] = (value == "True") if value in ("True", "False") else (
                        float(value) if "." in value else int(value))
                rows["counts" if tag == "PRECOUNT" else "gates"].append(row)
    if len(rows["counts"]) != 27 or len(rows["gates"]) != 9:
        raise ValueError("Se esperaban 27 recuentos y nueve puertas")
    for size in (16, 32, 64):
        for cls in range(3):
            samples = [r for r in rows["counts"] if r["size"] == size and r["class"] == cls]
            stable = [{k: v for k, v in r.items() if k != "rep"} for r in samples]
            if len(samples) != 3 or any(r != stable[0] for r in stable):
                raise ValueError("Recuentos no deterministas")
    for row in rows["gates"]:
        counts = [r for r in rows["counts"] if (r["size"], r["rep"]) == (row["size"], row["rep"])]
        assert row["sources"] == sum(r["exits"] for r in counts)
        assert row["localDistanceBytes"] == 4 * sum(r["localPairs"] for r in counts)
        assert row["nodeExitDistanceBytes"] == 4 * sum(r["nodeExitPairs"] for r in counts)
        assert row["globalDistanceBytes"] == 4 * sum(r["nodes"] ** 2 for r in counts)
        assert 0 <= row["completed"] <= row["sources"]
        assert row["stopped"] or row["completed"] == row["sources"]
        row["partialPreparationMs"] = row["buildMs"] + row["prepMs"]
        # Modelo de un barrido denso, NO cota inferior de toda consulta exacta.
        row["denseOverlayScanEntries"] = sum(r["localPairs"] + r["portals"] for r in counts)
        row["denseScanOnlyTicks3000"] = (row["denseOverlayScanEntries"] + 2999) // 3000
    return {"source": str(path), "note": "Solo viabilidad, no equivalencia ni carga de consumidores.", **rows}


if __name__ == "__main__":
    report = extract(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    for gate in report["gates"]:
        print(json.dumps(gate))
