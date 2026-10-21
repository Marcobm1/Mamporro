"""Extrae puerta 1 ALT; no convierte trabajo offline en latencia de servicio."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def extract(path):
    root = ET.parse(path).getroot()
    if root.get("result") != "Passed":
        raise ValueError("Suite fallida; no publicar como evaluación válida")
    rows = {"ALT-PREP": [], "ALT-PAIR": [], "ALT-COHORT": []}
    for case in root.iter("test-case"):
        for output in case.findall("output"):
            for line in (output.text or "").splitlines():
                if not line.strip():
                    continue
                tag, *fields = line.split()
                if tag not in rows:
                    continue
                row = {}
                for field in fields:
                    key, value = field.split("=", 1)
                    try:
                        row[key] = int(value)
                    except ValueError:
                        try:
                            row[key] = float(value)
                        except ValueError:
                            row[key] = value
                rows[tag].append(row)
    if [len(rows[k]) for k in rows] != [27, 18, 18]:
        raise ValueError("Se esperaban 27 preparaciones, 18 comparaciones y 18 cohortes")
    for size in (16, 32, 64):
        for k in (2, 4, 8):
            samples = [r for r in rows["ALT-PREP"] if r["size"] == size and r["k"] == k]
            if len(samples) != 3 or len({(r["work"], r["payload"]) for r in samples}) != 1:
                raise ValueError("Preparación no determinista")
    cohorts = []
    for k in (2, 4, 8):
        for distribution in ("concentrated", "dispersed"):
            samples = [r for r in rows["ALT-COHORT"] if r["k"] == k and r["distribution"] == distribution]
            assert len(samples) == 3 and sum(r["consumers"] for r in samples) == 750
            work = sum(r["work"] for r in samples)
            # Referencia HEREDADA: vecino 750, 64 m, mismos orígenes/meta.
            inherited = 57764 if distribution == "concentrated" else 1992760
            cohorts.append({
                "k": k, "distribution": distribution, "work": work,
                "cpuMs": sum(r["cpuMs"] for r in samples),
                "inheritedDemandWork": inherited, "workRatioToInherited": work / inherited,
                "workOnlyTicksAt3000": (work + 2999) // 3000,
            })
    return {"source": str(path), "note": "Puerta offline; sin scheduler, cobertura o latencias de servicio.",
            **rows, "cohorts": cohorts}


if __name__ == "__main__":
    report = extract(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    for row in report["cohorts"]:
        print(json.dumps(row))
