"""Resume la jerarquía fría D3; no mide persecución, movimiento ni FPS."""
import json
import pathlib
import sys
import xml.etree.ElementTree as ET


def read_report(path):
    root = ET.parse(path).getroot()
    if root.get("result") != "Passed":
        raise ValueError("La suite no terminó correctamente")
    rows, ticks = [], []
    for output in root.iter("output"):
        for line in (output.text or "").splitlines():
            parts = line.split()
            if not parts or parts[0] not in ("D3-hierarchy", "D3-hierarchy-total"):
                continue
            row = {}
            for part in parts[1:]:
                name, value = part.split("=", 1)
                row[name] = float(value) if "." in value else int(value)
            if parts[0] == "D3-hierarchy":
                row["publicationSeconds60Hz"] = (
                    row["publishedTick"] / 60 if row["version"] else None
                )
                rows.append(row)
            else:
                ticks.append(row)
    if len(rows) != 9 or len(ticks) != 3:
        raise ValueError("Se requieren tres tamaños y tres clases sin duplicados")
    if {(r["size"], r["class"]) for r in rows} != {
        (size, cls) for size in (16, 32, 64) for cls in range(3)
    }:
        raise ValueError("Corpus incompleto")
    return {
        "source": str(path), "passed": int(root.get("passed")),
        "simulatedSeconds": 180, "budgetPerTick": 3000,
        "limitations": "Editor sin render; una meta fija por clase en HORDA750, caché fría. No mide campos locales, enemigos, llegada, memoria residente completa ni FPS. Una publicación ausente es censura a 180 s, no latencia cero.",
        "classes": rows, "ticks": ticks,
    }


if __name__ == "__main__":
    report = read_report(pathlib.Path(sys.argv[1]))
    target = pathlib.Path(sys.argv[2])
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
