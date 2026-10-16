"""Resume las medidas de transferencias D3; no acredita persecución ni FPS."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET


def read_report(path):
    root = ET.parse(path).getroot()
    if root.get("result") != "Passed":
        raise ValueError("La suite no terminó correctamente")
    rows = []
    for output in root.iter("output"):
        for line in (output.text or "").splitlines():
            if not line.startswith("D3-transfer "):
                continue
            parts = line.split()
            row = {"case": parts[1]}
            for part in parts[2:]:
                name, value = part.split("=", 1)
                row[name] = float(value) if "." in value else int(value)
            rows.append(row)
    if not rows:
        raise ValueError("No hay medidas de transferencias")
    summaries = []
    for size in (16, 32, 64):
        for scope in ("local", "global"):
            selected = [r for r in rows if r["size"] == size and
                        (r["region"] >= 0) == (scope == "local")]
            if not selected:
                continue
            summary = {"size": size, "scope": scope, "cases": len(selected)}
            for metric in ("work", "ticks", "cpuMs", "warmUs", "payload", "latencySeconds60Hz",
                           "tickP95Ms", "tickMaxMs", "allocated", "initialized"):
                if any(metric not in r for r in selected):
                    continue
                values = [r[metric] for r in selected]
                summary[metric] = {"min": min(values), "max": max(values)}
            summaries.append(summary)
    return {"source": str(path), "passed": int(root.get("passed")),
            "limitations": "Rangos de transferencias desde una fuente por caso/clase; no p95, horda, campos locales, residencia completa ni FPS.",
            "summaries": summaries, "rows": rows}


if __name__ == "__main__":
    report = read_report(pathlib.Path(sys.argv[1]))
    target = pathlib.Path(sys.argv[2])
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["summaries"], ensure_ascii=False, indent=2))
