"""Resume trazas D0 nuevas; no ejecuta ni modifica la simulación ni los datos fuente."""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path


def summarize(path):
    with path.open(encoding="utf-8-sig", newline="") as handle:
        rows = list(csv.DictReader(handle))
    dt = 1 / 60
    phases = defaultdict(lambda: {"seconds": 0, "movingSeconds": 0, "distance": 0})
    blocked = Counter()
    accesses = []
    last_region = None
    moved_seconds = ramp_seconds = ramp_distance = 0
    inconsistent_cells = []
    no_field_ticks = 0
    episodes = []
    start = None
    previous = None
    for row in rows:
        moved = float(row["moved"])
        no_field_ticks += row["version"] == "0"
        if "nodeCell" in row and row["navState"] == "0" and row["nodeCell"] != row["positionCell"]:
            inconsistent_cells.append({k: row[k] for k in ("tick", "node", "nodeCell", "positionCell", "x", "y", "z", "geometry", "detourTime")})
        phase = phases[row["phase"]]
        phase["seconds"] += dt
        phase["movingSeconds"] += dt if moved >= 1e-5 else 0
        phase["distance"] += moved
        moved_seconds += dt if moved >= 1e-5 else 0
        if previous and row["navState"] == "0" and abs(float(row["y"]) - float(previous["y"])) > 1e-5:
            ramp_seconds += dt
            ramp_distance += moved
        stopped_geometry = row["geometry"] == "True" and moved < 1e-5
        if stopped_geometry:
            blocked[(row["node"], row["parent"], row["x"], row["z"], row["combatState"])] += 1
            if start is None:
                start = int(row["tick"])
        elif start is not None:
            episodes.append({"start": start * dt, "end": int(row["tick"]) * dt, "seconds": (int(row["tick"]) - start) * dt})
            start = None
        if row["region"] != last_region:
            accesses.append({k: row[k] for k in ("tick", "region", "node", "x", "y", "z", "navState")})
            last_region = row["region"]
        previous = row
    if start is not None:
        episodes.append({"start": start * dt, "end": len(rows) * dt, "seconds": (len(rows) - start) * dt})
    return {
        "source": str(path), "ticks": len(rows), "duration": len(rows) * dt,
        "movingSeconds": moved_seconds, "stationarySeconds": len(rows) * dt - moved_seconds,
        "withoutPublishedFieldSeconds": no_field_ticks * dt,
        "nodePositionCellMismatchSeconds": len(inconsistent_cells) * dt,
        "firstNodePositionCellMismatches": inconsistent_cells[:10],
        "walkWithVerticalChangeSeconds": ramp_seconds, "walkWithVerticalChangeDistance": ramp_distance,
        "phases": phases, "regionTransitions": accesses,
        "longestGeometryStops": sorted(episodes, key=lambda item: -item["seconds"])[:10],
        "frequentBlockedStates": [{"node": key[0], "parent": key[1], "x": key[2], "z": key[3], "combatState": key[4], "seconds": count * dt} for key, count in blocked.most_common(10)],
        "final": {k: rows[-1][k] for k in ("x", "y", "z", "px", "py", "pz", "horizontal", "vertical", "surfaceError", "stable", "region", "node", "version", "fieldDistance")},
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path, help="Carpeta Diagnosis de una ejecución D0")
    args = parser.parse_args()
    for path in sorted(args.folder.rglob("[0-2].csv")):
        result = summarize(path)
        destination = path.with_name(path.stem + "-analysis.json")
        destination.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f'{path.parent.name}/{path.stem}: duración={result["duration"]:.3f}s movimiento={result["movingSeconds"]:.3f}s distanciaFinal={result["final"]["horizontal"]}m')
