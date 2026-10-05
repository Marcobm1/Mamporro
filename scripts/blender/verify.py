"""Comprobación del entorno; sin crear modelos ni guardar preferencias."""
import json
import pathlib
import sys
import bpy

assert bpy.app.version == (5, 2, 2), "Versión distinta de la validada: diagnosticar antes de actualizar el contrato"
assert bpy.app.background, "La comprobación automatizada debe ejecutarse en background"
exporter = bpy.ops.export_scene.fbx.get_rna_type()
assert exporter.identifier == "EXPORT_SCENE_OT_fbx", "Exportador FBX no disponible"
path = pathlib.Path(sys.argv[sys.argv.index("--") + 1])
path.write_text(json.dumps({
    "version": bpy.app.version_string,
    "build": bpy.app.build_hash.decode("ascii"),
    "binary": bpy.app.binary_path,
    "python": sys.version.split()[0],
    "background": bpy.app.background,
    "fbx": True,
}, indent=2), encoding="utf-8")
print("B0: Blender/Python/FBX verificados; sin assets generados")
