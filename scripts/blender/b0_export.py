"""Exportación reproducible B0: .blend fuente → FBX para Unity (Blender en background).

Por cada asset del manifiesto: valida la fuente contra el contrato, exporta solo los objetos
enumerados a un temporal fuera de Assets, reimporta ese FBX en una escena vacía y comprueba
nombres, triángulos y bounds, y solo entonces sustituye la salida (copia .tmp + reemplazo
atómico; Unity ignora *.tmp). Si el contenido semántico no cambió, conserva la salida
existente. Nunca guarda la fuente: comprueba su SHA-256 antes y después.
Uso: blender --background --factory-startup --python b0_export.py -- <id> <informe.json> [--force] [--out <raíz>] [--manifest <ruta>]
"""
import hashlib
import json
import os
import pathlib
import sys
import tempfile
import time

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import b0_contract as contract  # noqa: E402


class ExportError(Exception):
    pass


def _select_only(names):
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for name in names:
        obj = bpy.data.objects[name]
        if obj.name not in bpy.context.view_layer.objects:
            raise ExportError(f"{name} no está en la capa de vista activa")
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = bpy.data.objects[names[0]]


def _reimport_check(fbx, entry, digest):
    """Reimporta el FBX en una escena vacía: mismos objetos, triángulos y bounds que la fuente."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx), use_custom_normals=True, ignore_leaf_bones=False, automatic_bone_orientation=False)
    imported = {o.name: o for o in bpy.data.objects}
    expected = set(entry["objects"])
    if set(imported) != expected:
        raise ExportError(f"Reimportación: objetos {sorted(imported)} != {sorted(expected)}")
    meshes = [o for o in imported.values() if o.type == "MESH"]
    tris = 0
    for o in meshes:
        o.data.calc_loop_triangles()
        tris += len(o.data.loop_triangles)
        if [m.name if m else None for m in o.data.materials] != entry["materials"]:
            raise ExportError(f"Reimportación: materiales {[m.name for m in o.data.materials]} != {entry['materials']}")
        if [uv.name for uv in o.data.uv_layers] != [contract.UV_NAME]:
            raise ExportError(f"Reimportación: UV {[uv.name for uv in o.data.uv_layers]}")
        if not o.data.color_attributes:
            raise ExportError("Reimportación: sin color de vértice")
    if tris != digest["triangles"]:
        raise ExportError(f"Reimportación: {tris} triángulos != {digest['triangles']} de la fuente")
    if entry.get("bounds"):
        lo, hi = contract.world_bounds(meshes)
        tol = entry.get("boundsTolerance", 0.01)
        for axis, (a, b) in enumerate(zip(entry["bounds"]["min"], entry["bounds"]["max"])):
            if abs(lo[axis] - a) > tol or abs(hi[axis] - b) > tol:
                raise ExportError(f"Reimportación: bounds {'XYZ'[axis]} [{lo[axis]:.4f}, {hi[axis]:.4f}] != [{a}, {b}] (¿escala o ejes?)")
    actions = sorted(a.name for a in bpy.data.actions)
    return {"triangles": tris, "actions": actions}


def sidecar(entry, digest, fbx_sha):
    """Datos que Unity necesita del manifiesto, junto al FBX. Sin fechas: estable entre exportaciones."""
    return {
        "contract": contract.CONTRACT_VERSION,
        "id": entry["id"],
        "kind": entry["kind"],
        "purpose": entry.get("purpose", "spike"),
        "materials": entry["materials"],
        "objects": entry["objects"],
        "clips": [{"name": c["name"], "take": c["action"], "start": c["start"], "end": c["end"], "loop": c.get("loop", False)} for c in entry.get("clips", [])],
        "fps": entry.get("fps", 30),
        # Coordenadas de Blender (Z arriba, frente −Y): Unity aplica la conversión del contrato.
        "boundsMin": (entry.get("bounds") or {}).get("min", []),
        "boundsMax": (entry.get("bounds") or {}).get("max", []),
        "boundsTolerance": entry.get("boundsTolerance", 0.01),
        "probes": [{"name": k, "position": v, "srgb": entry.get("colors", {}).get(k, [])} for k, v in sorted(entry.get("probes", {}).items())],
        "triangles": digest["triangles"],
        "vertices": digest["vertices"],
        "digest": digest["digest"],
        "fbxSha256": fbx_sha,
    }


def _write_atomic(path, data):
    tmp = path.with_name(path.name + ".tmp")
    if isinstance(data, (bytes, bytearray)):
        tmp.write_bytes(data)
    else:
        tmp.write_text(data, encoding="utf-8", newline="\n")
    os.replace(tmp, path)


def export_asset(manifest_path, entry, out_root, force=False):
    started = time.time()
    source = (manifest_path.parent / entry["source"]).resolve()
    if not source.is_file():
        raise ExportError(f"Fuente ausente: {source}")
    out = (pathlib.Path(out_root) / entry["output"]).resolve()
    side = out.with_suffix(".b0.json")
    before = contract.sha256(source)
    bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
    if bpy.app.version != contract.BLENDER_VERSION:
        raise ExportError(f"Blender {bpy.app.version_string} distinto del validado {contract.BLENDER_VERSION}")
    errors = contract.validate(entry)
    if errors:
        raise ExportError("Contrato incumplido:\n- " + "\n- ".join(errors))
    objs = [bpy.data.objects[n] for n in entry["objects"]]
    digest = contract.semantic_digest(objs)
    _select_only(entry["objects"])
    with tempfile.TemporaryDirectory(prefix="mamporro-b0-") as tmpdir:
        tmp_fbx = pathlib.Path(tmpdir) / out.name
        result = bpy.ops.export_scene.fbx(filepath=str(tmp_fbx), **contract.FBX_SETTINGS)
        if result != {"FINISHED"} or not tmp_fbx.is_file() or tmp_fbx.stat().st_size == 0:
            raise ExportError(f"El exportador FBX no produjo salida ({result})")
        check = _reimport_check(tmp_fbx, entry, digest)
        if contract.sha256(source) != before:
            raise ExportError("La fuente .blend cambió durante la exportación")
        new_bytes = tmp_fbx.read_bytes()
    fbx_sha = hashlib.sha256(new_bytes).hexdigest()
    previous = json.loads(side.read_text(encoding="utf-8")) if side.is_file() else None
    unchanged = (not force and previous is not None and out.is_file() and previous.get("digest") == digest["digest"]
                 and previous.get("fbxSha256") == contract.sha256(out))
    status = "sin cambios"
    if not unchanged:
        out.parent.mkdir(parents=True, exist_ok=True)
        _write_atomic(out, new_bytes)
        _write_atomic(side, json.dumps(sidecar(entry, digest, fbx_sha), indent=2, ensure_ascii=False) + "\n")
        status = "exportado"
    return {
        "id": entry["id"], "status": status, "source": str(source.relative_to(manifest_path.parent)),
        "sourceSha256": before, "output": str(out), "fbxSha256": contract.sha256(out), "newExportSha256": fbx_sha,
        "fbxBytes": out.stat().st_size, "sourceBytes": source.stat().st_size,
        "triangles": digest["triangles"], "vertices": digest["vertices"], "digest": digest["digest"],
        "reimport": check, "seconds": round(time.time() - started, 3), "blender": bpy.app.version_string,
    }


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    asset, report_path = argv[0], pathlib.Path(argv[1])
    force = "--force" in argv
    manifest = argv[argv.index("--manifest") + 1] if "--manifest" in argv else None
    manifest_path, data = contract.load_manifest(manifest)
    out_root = argv[argv.index("--out") + 1] if "--out" in argv else contract.repo_root() / data["unityRoot"]
    entry = next((a for a in data["assets"] if a["id"] == asset), None)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        if entry is None:
            raise ExportError(f"Asset {asset} no está en el manifiesto")
        report = export_asset(manifest_path, entry, out_root, force)
        report["ok"] = True
    except ExportError as e:
        report = {"id": asset, "ok": False, "error": str(e)}
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))
    if not report["ok"]:
        print("B0 export: ERROR " + report["error"], file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
