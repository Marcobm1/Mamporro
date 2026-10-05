"""Contrato de assets B0 (Blender -> FBX -> Unity).

Se usa desde Blender en background. Valida una escena contra la entrada de su
manifiesto y devuelve errores explicados; nunca corrige la fuente en silencio.
"""
import hashlib
import json
import math
import pathlib
import re

import bpy

CONTRACT_VERSION = 1
BLENDER_VERSION = (5, 2, 2)
NAME = re.compile(r"^[A-Za-z][A-Za-z0-9_]*$")
EPS = 1e-4
MAX_INFLUENCES = 4
UV_NAME = "UVMap"
COLOR_NAME = "Col"

# Ajustes FBX cerrados por el contrato (comprobados en B0.2 con la fixture asimétrica).
# Ejes nativos de Blender (Z arriba, sin conversión en el exportador): Unity hornea Z→Y en
# mallas y animaciones (bakeAxisConversion) y mapea Blender (x, y, z) → Unity (x, z, y).
# Por eso el frente del asset mira a +Y en Blender y su derecha a +X: llega mirando a +Z.
# Ensayos descartados: -Z/Y deja 90° en el nodo; -Y/Z sin space transform no hace ida y vuelta.
FBX_SETTINGS = dict(
    use_selection=True,
    object_types={"MESH", "ARMATURE"},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_space_transform=True,
    bake_space_transform=False,
    axis_forward="Y",
    axis_up="Z",
    use_mesh_modifiers=True,
    mesh_smooth_type="OFF",
    colors_type="LINEAR",
    prioritize_active_color=False,
    use_triangles=True,
    use_tspace=False,
    use_custom_props=False,
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    use_armature_deform_only=True,
    armature_nodetype="NULL",
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    path_mode="STRIP",
    embed_textures=False,
    use_metadata=False,
)


def repo_root():
    return pathlib.Path(__file__).resolve().parents[2]


def load_manifest(path=None):
    path = pathlib.Path(path) if path else repo_root() / "art" / "blender" / "b0" / "manifest.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("contract") != CONTRACT_VERSION:
        raise ValueError(f"Manifiesto con contrato {data.get('contract')}; se esperaba {CONTRACT_VERSION}")
    return path, data


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def _transform_is_identity(obj, allow_location=False):
    loc_ok = allow_location or all(abs(v) < EPS for v in obj.location)
    rot_ok = all(abs(v) < EPS for v in obj.rotation_euler) and obj.rotation_mode in {"XYZ", "QUATERNION"}
    if obj.rotation_mode == "QUATERNION":
        q = obj.rotation_quaternion
        rot_ok = abs(q.w - 1) < EPS and abs(q.x) < EPS and abs(q.y) < EPS and abs(q.z) < EPS
    scale_ok = all(abs(v - 1) < EPS for v in obj.scale)
    return loc_ok and rot_ok and scale_ok


def validate(entry):
    """Comprueba la escena abierta contra una entrada del manifiesto. Devuelve lista de errores."""
    errors = []
    scene = bpy.context.scene
    us = scene.unit_settings
    if us.system != "METRIC" or abs(us.scale_length - 1.0) > EPS:
        errors.append(f"Unidades: se exige sistema métrico con escala 1.0 (hay {us.system} × {us.scale_length})")
    names = entry["objects"]
    for name in names:
        if not NAME.match(name):
            errors.append(f"Nombre no ASCII/identificador: {name!r}")
        if name not in bpy.data.objects:
            errors.append(f"Falta el objeto del manifiesto: {name}")
    if errors:
        return errors
    objs = [bpy.data.objects[n] for n in names]
    for obj in objs:
        if obj.type not in {"MESH", "ARMATURE"}:
            errors.append(f"{obj.name}: tipo {obj.type} no exportable (solo MESH/ARMATURE)")
    meshes = [o for o in objs if o.type == "MESH"]
    armatures = [o for o in objs if o.type == "ARMATURE"]
    kind = entry["kind"]
    if kind in {"static", "modular"} and armatures:
        errors.append("Un asset estático no puede exportar armaduras")
    if kind == "rigged" and len(armatures) != 1:
        errors.append(f"Un asset con rig exige exactamente una armadura (hay {len(armatures)})")
    for obj in objs:
        # Mallas y armadura en el origen, sin rotación ni escala: el pivote es el origen de la fuente.
        if obj.parent is None and not _transform_is_identity(obj):
            errors.append(f"{obj.name}: transform sin aplicar (loc {tuple(round(v, 4) for v in obj.location)}, rot {tuple(round(v, 4) for v in obj.rotation_euler)}, escala {tuple(round(v, 4) for v in obj.scale)})")
        if obj.parent is not None and (obj.parent.name not in names or not _transform_is_identity(obj)):
            errors.append(f"{obj.name}: padre fuera del manifiesto o transform local no identidad")
    expected_slots = entry["materials"]
    for obj in meshes:
        me = obj.data
        if me.name != obj.name:
            errors.append(f"{obj.name}: la malla se llama {me.name!r}; debe llamarse como su objeto")
        for mod in obj.modifiers:
            if mod.type != "ARMATURE":
                errors.append(f"{obj.name}: modificador {mod.type} sin aplicar (solo Armature permitido)")
        slots = [s.material.name if s.material else None for s in obj.material_slots]
        if slots != expected_slots:
            errors.append(f"{obj.name}: materiales {slots} != manifiesto {expected_slots}")
        for name in slots:
            if name and not NAME.match(name):
                errors.append(f"{obj.name}: material con nombre inválido {name!r}")
        if len(me.polygons) == 0:
            errors.append(f"{obj.name}: malla vacía")
        uv_names = [uv.name for uv in me.uv_layers]
        if uv_names != [UV_NAME]:
            errors.append(f"{obj.name}: UV {uv_names}; se exige exactamente [{UV_NAME!r}]")
        colors = [c.name for c in me.color_attributes]
        if colors != [COLOR_NAME]:
            errors.append(f"{obj.name}: color de vértice {colors}; se exige exactamente [{COLOR_NAME!r}]")
        elif me.color_attributes[COLOR_NAME].domain != "CORNER" or me.color_attributes[COLOR_NAME].data_type != "BYTE_COLOR":
            errors.append(f"{obj.name}: {COLOR_NAME} debe ser BYTE_COLOR en esquinas")
        for poly in me.polygons:
            if poly.area < 1e-8:
                errors.append(f"{obj.name}: cara degenerada {poly.index}")
                break
        if kind == "rigged":
            errors += _validate_skin(obj, armatures[0] if armatures else None, entry)
    bounds = entry.get("bounds")
    if bounds and meshes:
        lo, hi = world_bounds(meshes)
        tol = entry.get("boundsTolerance", 0.01)
        for axis, (a, b) in enumerate(zip(bounds["min"], bounds["max"])):
            if abs(lo[axis] - a) > tol or abs(hi[axis] - b) > tol:
                errors.append(f"Bounds eje {'XYZ'[axis]}: [{lo[axis]:.4f}, {hi[axis]:.4f}] fuera de [{a}, {b}] ± {tol}")
    errors += _validate_clips(entry, armatures)
    return errors


def _validate_skin(obj, arm, entry):
    errors = []
    if arm is None:
        return errors
    mods = [m for m in obj.modifiers if m.type == "ARMATURE"]
    if len(mods) != 1 or mods[0].object != arm:
        errors.append(f"{obj.name}: necesita un único modificador Armature apuntando a {arm.name}")
    bones = {b.name for b in arm.data.bones if b.use_deform}
    for b in arm.data.bones:
        if not NAME.match(b.name):
            errors.append(f"Hueso con nombre inválido {b.name!r}")
    max_bones = entry.get("maxBones", 32)
    if len(bones) > max_bones:
        errors.append(f"{arm.name}: {len(bones)} huesos deformantes > {max_bones}")
    groups = {g.index: g.name for g in obj.vertex_groups}
    for g in obj.vertex_groups:
        if g.name not in bones:
            errors.append(f"{obj.name}: grupo {g.name} sin hueso deformante")
    unassigned = over = unnormalized = 0
    for v in obj.data.vertices:
        ws = [g.weight for g in v.groups if g.group in groups and g.weight > 0]
        if not ws:
            unassigned += 1
            continue
        if len(ws) > MAX_INFLUENCES:
            over += 1
        if abs(sum(ws) - 1) > 1e-3:
            unnormalized += 1
    if unassigned:
        errors.append(f"{obj.name}: {unassigned} vértices sin peso")
    if over:
        errors.append(f"{obj.name}: {over} vértices con más de {MAX_INFLUENCES} influencias")
    if unnormalized:
        errors.append(f"{obj.name}: {unnormalized} vértices con pesos no normalizados")
    return errors


def _validate_clips(entry, armatures):
    errors = []
    clips = entry.get("clips", [])
    actions = {a.name: a for a in bpy.data.actions}
    for clip in clips:
        if not NAME.match(clip["name"]):
            errors.append(f"Clip con nombre inválido {clip['name']!r}")
        a = actions.get(clip["action"])
        if a is None:
            errors.append(f"Falta la acción {clip['action']} del clip {clip['name']}")
            continue
        start, end = a.frame_range
        if int(round(start)) != clip["start"] or int(round(end)) != clip["end"]:
            errors.append(f"{a.name}: rango {start}-{end} != manifiesto {clip['start']}-{clip['end']}")
    extra = set(actions) - {c["action"] for c in clips}
    if extra:
        errors.append(f"Acciones fuera del manifiesto (se exportarían): {sorted(extra)}")
    if clips and bpy.context.scene.render.fps != entry.get("fps", 30):
        errors.append(f"FPS de escena {bpy.context.scene.render.fps} != {entry.get('fps', 30)}")
    if clips and not armatures:
        errors.append("Clips sin armadura")
    return errors


def world_bounds(objs):
    lo = [math.inf] * 3
    hi = [-math.inf] * 3
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objs:
        ev = obj.evaluated_get(deps)
        me = ev.to_mesh()
        try:
            for v in me.vertices:
                w = ev.matrix_world @ v.co
                for i in range(3):
                    lo[i] = min(lo[i], w[i])
                    hi[i] = max(hi[i], w[i])
        finally:
            ev.to_mesh_clear()
    return lo, hi


def semantic_digest(objs):
    """Resumen estable del contenido exportado (independiente de metadatos FBX variables)."""
    h = hashlib.sha256()
    info = {"objects": [], "triangles": 0, "vertices": 0}
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in sorted(objs, key=lambda o: o.name):
        entry = {"name": obj.name, "type": obj.type}
        h.update(obj.name.encode())
        if obj.type == "MESH":
            ev = obj.evaluated_get(deps)
            me = ev.to_mesh()
            try:
                me.calc_loop_triangles()
                tris = len(me.loop_triangles)
                entry.update(triangles=tris, vertices=len(me.vertices), materials=[s.material.name if s.material else None for s in obj.material_slots])
                info["triangles"] += tris
                info["vertices"] += len(me.vertices)
                for v in me.vertices:
                    w = ev.matrix_world @ v.co
                    h.update(("%.5f %.5f %.5f;" % tuple(w)).encode())
                for t in me.loop_triangles:
                    h.update(("%d %d %d %d;" % (*t.vertices, t.material_index)).encode())
                col = me.color_attributes.get(COLOR_NAME)
                if col:
                    for d in col.data:
                        h.update(("%.3f %.3f %.3f;" % tuple(d.color_srgb[:3])).encode())
                uv = me.uv_layers.get(UV_NAME)
                if uv:
                    for d in uv.data:
                        h.update(("%.5f %.5f;" % tuple(d.uv)).encode())
            finally:
                ev.to_mesh_clear()
        elif obj.type == "ARMATURE":
            bones = [b.name for b in obj.data.bones]
            entry["bones"] = bones
            for b in obj.data.bones:
                h.update(("%s %.5f %.5f %.5f;" % (b.name, *b.head_local)).encode())
        info["objects"].append(entry)
    info["actions"] = sorted(a.name for a in bpy.data.actions)
    for a in sorted(bpy.data.actions, key=lambda a: a.name):
        h.update(("%s %.1f %.1f;" % (a.name, *a.frame_range)).encode())
    info["digest"] = h.hexdigest()
    return info
