"""Resume H1 offline: éxito del arnés no equivale a aceptación de la puerta."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def extract(path):
    root = ET.parse(path).getroot()
    if root.get("result") != "Passed":
        raise ValueError("Suite fallida: no certificar exactitud")
    rows = {"H1-PREP": [], "H1-GATE": []}
    for case in root.iter("test-case"):
        for output in case.findall("output"):
            for line in (output.text or "").splitlines():
                fields = line.split()
                if not fields or fields[0] not in rows:
                    continue
                row = {}
                for field in fields[1:]:
                    key, value = field.split("=", 1)
                    try:
                        row[key] = int(value)
                    except ValueError:
                        try:
                            row[key] = float(value)
                        except ValueError:
                            row[key] = value
                rows[fields[0]].append(row)
    if len(rows["H1-PREP"]) != 108:
        raise ValueError("Se esperaban 90 preparaciones de corpus y 18 de fixtures")
    final = {}
    for row in rows["H1-GATE"]:
        key = (row["case"], row["size"], row["class"])
        if row["k"] == 2 and (key not in final or final[key]["pass"]):
            raise ValueError("Dos testigos sin fallo previo de uno")
        final[key] = row
    if len(final) != 108:
        raise ValueError("Faltan evaluaciones finales")
    failed = [r for r in final.values() if not r["pass"]]
    preparations = []
    for label, size in sorted({(r["case"], r["size"]) for r in rows["H1-PREP"]}):
        subset = [r for r in rows["H1-PREP"] if (r["case"], r["size"]) == (label, size)]
        if len(subset) != 3:
            raise ValueError("Falta una clase")
        preparations.append({"case": label, "size": size,
            "completeMs": subset[0]["baseMs"] + sum(r["worldMs"] + r["setupMs"] for r in subset),
            "probePayload": sum(r["payload"] for r in subset),
            "physicalTopologyProbeArrays": sum(r["payload"] + r["physical"] + r["topology"] for r in subset)})
    return {"source": str(path), "tests": root.get("passed"), "duration": root.get("duration"),
        "note": "Offline H1; sin scheduler ni FPS. Memoria parcial, no residencia certificada.",
        **rows, "preparations": preparations, "finalQualityFailures": failed,
        "qualityGate": "FAIL" if failed else "PASS", "overallGate": "FAIL" if failed else "NOT_CERTIFIED"}


if __name__ == "__main__":
    report = extract(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"tests": report["tests"], "qualityGate": report["qualityGate"],
        "failedCases": len(report["finalQualityFailures"]),
        "worst": max(report["H1-GATE"], key=lambda r: r["max"])}))
