"""Capturas de revisión B0 desde la fuente (Blender en background, Workbench, color de vértice).

Vistas giratorias (frente, ¾, lado, espalda) y fotogramas de cada clip; no guarda la fuente.
Uso: blender --background --factory-startup --python b0_capture.py -- <id> <carpeta>
"""
import math
import pathlib
import sys

import bpy
from mathutils import Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import b0_contract as contract  # noqa: E402


def setup(lo, hi):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = scene.render.resolution_y = 512
    scene.render.film_transparent = False
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "VERTEX"
    shading.show_object_outline = False
    scene.world = scene.world or bpy.data.worlds.new("B0_Captura")
    scene.world.color = (0.18, 0.2, 0.24)
    center = Vector([(a + b) / 2 for a, b in zip(lo, hi)])
    size = max(b - a for a, b in zip(lo, hi))
    cam = bpy.data.objects.new("B0_Captura_Cam", bpy.data.cameras.new("B0_Captura_Cam"))
    scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = size * 1.25
    scene.camera = cam
    return scene, cam, center, size


def aim(cam, center, size, yaw_deg, pitch_deg=15):
    yaw, pitch = math.radians(yaw_deg), math.radians(pitch_deg)
    # yaw 0 = mirando al frente del asset (+Y): cámara en +Y mirando hacia −Y.
    offset = Vector((math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * size * 3
    cam.location = center + offset
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    asset, out = argv[0], pathlib.Path(argv[1])
    manifest_path, data = contract.load_manifest()
    entry = next(a for a in data["assets"] if a["id"] == asset)
    bpy.ops.wm.open_mainfile(filepath=str(manifest_path.parent / entry["source"]), load_ui=False)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in meshes:
        o.data.color_attributes.active_color_index = o.data.color_attributes.render_color_index = 0
    lo, hi = contract.world_bounds(meshes)
    scene, cam, center, size = setup(lo, hi)
    out.mkdir(parents=True, exist_ok=True)
    for name, yaw in (("frente", 0), ("tres-cuartos", 35), ("derecha", 90), ("espalda", 180)):
        aim(cam, center, size, yaw)
        scene.render.filepath = str(out / f"{asset}-{name}.png")
        bpy.ops.render.render(write_still=True)
    arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
    for clip in entry.get("clips", []):
        arm.animation_data_create()
        arm.animation_data.action = bpy.data.actions[clip["action"]]
        # Acciones con slots (Blender ≥ 4.4): asegurar el slot de la armadura.
        action = arm.animation_data.action
        if getattr(arm.animation_data, "action_slot", 1) is None and len(action.slots):
            arm.animation_data.action_slot = action.slots[0]
        aim(cam, center, size, 35)
        steps = 6
        for k in range(steps):
            frame = clip["start"] + round((clip["end"] - clip["start"]) * k / steps)
            scene.frame_set(frame)
            scene.render.filepath = str(out / f"{asset}-{clip['name']}-{frame:03d}.png")
            bpy.ops.render.render(write_still=True)
    print(f"B0 capture: {asset} → {out}")


main()
