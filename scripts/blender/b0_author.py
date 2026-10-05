"""Crea las fuentes .blend iniciales de B0 a partir de código (Blender en background).

Solo genera una fuente si no existe (o con --force): una vez creada, el .blend es la
fuente editable y manda sobre este script. Todo el contenido es original de MAMPORRO
(siluetas y paleta de los modelos web del propio juego); nada copiado de otros juegos.
Contrato: frente del asset hacia +Y, derecha hacia +X, Z arriba, metros, pivote en el origen.
Uso: blender --background --factory-startup --python b0_author.py -- <id> [--force]
"""
import math
import pathlib
import random
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import b0_contract as contract  # noqa: E402


def empty_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = 30
    return scene


def srgb(hex_value):
    return ((hex_value >> 16) & 255) / 255, ((hex_value >> 8) & 255) / 255, (hex_value & 255) / 255


def material(name, color):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in color]
    mat.diffuse_color = (*lin, 1)
    return mat


def place(at=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    """Matriz traslación · rotación (XYZ, radianes) · escala."""
    return Matrix.Translation(Vector(at)) @ Euler(rot, "XYZ").to_matrix().to_4x4() @ Matrix.Diagonal((*scale, 1))


class Builder:
    """Malla por partes: color de vértice, slot, hueso (peso rígido) y UV por proyección de caja."""

    def __init__(self, name, materials, uv_scale=5.0):
        self.name = name
        self.bm = bmesh.new()
        self.colors = self.bm.loops.layers.color.new(contract.COLOR_NAME)
        self.uv = self.bm.loops.layers.uv.new(contract.UV_NAME)
        self.materials = materials
        self.uv_scale = uv_scale
        self.parts = []  # (verts, hueso)

    def _finish(self, verts, color, slot, smooth, bone):
        faces = list({f for v in verts for f in v.link_faces})
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        for f in faces:
            f.material_index = slot
            f.smooth = smooth
            n = f.normal
            axis = max(range(3), key=lambda i: abs(n[i]))
            u_axis, v_axis = [(1, 2), (0, 2), (0, 1)][axis]
            for loop in f.loops:
                co = loop.vert.co
                loop[self.colors] = (*color, 1.0)
                loop[self.uv].uv = (co[u_axis] / self.uv_scale + 0.5, co[v_axis] / self.uv_scale + 0.5)
        self.parts.append((verts, bone))
        return verts

    def box(self, lo, hi, color, slot=0, smooth=False, bone=None):
        lo, hi = Vector(lo), Vector(hi)
        verts = bmesh.ops.create_cube(self.bm, size=1.0)["verts"]
        for v in verts:
            v.co = Vector((lo.x if v.co.x < 0 else hi.x, lo.y if v.co.y < 0 else hi.y, lo.z if v.co.z < 0 else hi.z))
        return self._finish(verts, color, slot, smooth, bone)

    def cube(self, size, matrix, color, slot=0, bone=None):
        """Caja de tamaño (x, y, z) centrada en el origen y colocada con `matrix`."""
        verts = bmesh.ops.create_cube(self.bm, size=1.0, matrix=matrix @ Matrix.Diagonal((*size, 1)))["verts"]
        return self._finish(verts, color, slot, False, bone)

    def cylinder(self, r_bottom, r_top, depth, segments, matrix, color, slot=0, smooth=False, bone=None):
        verts = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments, radius1=r_bottom, radius2=r_top, depth=depth, matrix=matrix)["verts"]
        return self._finish(verts, color, slot, smooth, bone)

    def cone(self, radius, depth, segments, matrix, color, slot=0, bone=None):
        verts = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=True, segments=segments, radius1=radius, radius2=0.0, depth=depth, matrix=matrix)["verts"]
        return self._finish(verts, color, slot, False, bone)

    def ico(self, radius, subdivisions, matrix, color, slot=0, jitter=0.0, rng=None, smooth=False, bone=None):
        verts = bmesh.ops.create_icosphere(self.bm, subdivisions=subdivisions, radius=radius, matrix=Matrix())["verts"]
        if jitter:
            for v in verts:
                v.co *= 1 + rng.uniform(-jitter, jitter) / radius
        for v in verts:
            v.co = matrix @ v.co
        return self._finish(verts, color, slot, smooth, bone)

    def build(self, armature=None):
        me = bpy.data.meshes.new(self.name)
        self.bm.verts.index_update()
        groups = {}
        for verts, bone in self.parts:
            if bone:
                groups.setdefault(bone, []).extend(v.index for v in verts)
        # La capa de color de bmesh es BYTE_COLOR en esquinas y guarda sRGB (comprobado en 5.2.2).
        self.bm.to_mesh(me)
        self.bm.free()
        # Color de vértice activo y de render: el visor y Workbench muestran la paleta real.
        me.color_attributes.active_color_index = me.color_attributes.render_color_index = 0
        for mat in self.materials:
            me.materials.append(mat)
        obj = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(obj)
        if armature:
            obj.parent = armature
            mod = obj.modifiers.new("Armature", "ARMATURE")
            mod.object = armature
            for bone, indices in groups.items():
                obj.vertex_groups.new(name=bone).add(indices, 1.0, "REPLACE")
        return obj


def make_armature(name, bones):
    """bones: [(nombre, cabeza, cola, padre)]; todos deformantes (Root sin pesos)."""
    data = bpy.data.armatures.new(name)
    arm = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for bone_name, head, tail, parent in bones:
        b = data.edit_bones.new(bone_name)
        b.head, b.tail, b.roll = Vector(head), Vector(tail), 0.0
        b.use_deform = True
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
    return arm


def keyframes(arm, action_name, frames, keys):
    """keys: {frame: {hueso: {"loc": (x,y,z), "rot": (x,y,z), "scale": (x,y,z)}}} en espacio del hueso."""
    action = bpy.data.actions.new(action_name)
    arm.animation_data_create()
    arm.animation_data.action = action
    animated = {bone for pose in keys.values() for bone in pose}
    for frame in sorted(keys):
        for bone in animated:
            pb = arm.pose.bones[bone]
            pose = keys[frame].get(bone, {})
            pb.location = pose.get("loc", (0, 0, 0))
            pb.rotation_euler = pose.get("rot", (0, 0, 0))
            pb.scale = pose.get("scale", (1, 1, 1))
            for path in ("location", "rotation_euler", "scale"):
                pb.keyframe_insert(path, frame=frame)
    for pb in arm.pose.bones:
        pb.location, pb.rotation_euler, pb.scale = (0, 0, 0), (0, 0, 0), (1, 1, 1)
    action.use_fake_user = True
    arm.animation_data.action = None
    return action


# --------------------------------------------------------------------------- fixture
def author_fixture():
    """Fixture geométrica asimétrica de QA: no es arte del juego.

    Cuerpo 1×0,5×2 m centrado en el origen (pivote en el suelo), «nariz» roja hacia el
    frente (+Y en Blender, contrato), brazo verde en el lado derecho del personaje (+X)
    y antena lisa arriba para comprobar normales suaves frente a cajas de aristas duras.
    """
    empty_scene()
    body = material("B0_Body", (0.5, 0.5, 0.5))
    marker = material("B0_Marker", (0.8, 0.2, 0.1))
    b = Builder("B0_Fixture", [body, marker])
    b.box((-0.5, -0.25, 0.0), (0.5, 0.25, 2.0), (0.5, 0.5, 0.5), slot=0)
    b.box((-0.1, 0.25, 1.0), (0.1, 1.25, 1.2), (0.8, 0.2, 0.1), slot=1)
    b.box((0.5, -0.1, 1.5), (1.5, 0.1, 1.7), (0.2, 0.7, 0.3), slot=1)
    b.cylinder(0.1, 0.1, 0.4, 8, place((0, 0, 2.2)), (0.5, 0.5, 0.5), slot=0, smooth=True)
    b.build()


# --------------------------------------------------------------------------- Pelusa
PELUSA, PELUSA_DARK, EYE_WHITE, PUPIL, BROW = 0xB3AEC2, 0x8A8598, 0xF8F8F0, 0x141018, 0x3A3548


def author_pelusa():
    """Pelusa Rebelde (enemigo común): bola de polvo facetada con mechones, ojos enfadados
    de cejas en V, boca recta y dos patitas. Rig mínimo para comparar animación masiva:
    Body (bote, squash/stretch, contoneo) y dos pies; Root sin pesos (sin root motion)."""
    empty_scene()
    rng = random.Random("b0-pelusa")
    arm = make_armature("B0_Pelusa_Rig", [
        ("Root", (0, 0, 0), (0, 0, 0.2), None),
        ("Body", (0, 0, 0.1), (0, 0, 0.9), "Root"),
        ("Foot_L", (-0.16, 0.0, 0.03), (-0.16, 0.2, 0.03), "Root"),
        ("Foot_R", (0.16, 0.0, 0.03), (0.16, 0.2, 0.03), "Root"),
    ])
    b = Builder("B0_Pelusa", [material("B0_Pelusa", srgb(PELUSA))])
    b.ico(0.44, 2, place((0, 0, 0.5)), srgb(PELUSA), jitter=0.05, rng=rng, bone="Body")
    up = Vector((0, 0, 1))
    for i in range(10):
        d = Vector((rng.uniform(-1, 1), rng.uniform(-0.6, 0.3), rng.uniform(-0.15, 1))).normalized()
        rot = up.rotation_difference(d).to_matrix().to_4x4()
        at = Vector((0, 0, 0.5)) + d * 0.4 + d * 0.12
        b.cone(0.09, 0.26, 4, Matrix.Translation(at) @ rot, srgb(PELUSA if i % 2 else PELUSA_DARK), bone="Body")
    for side in (-1, 1):
        b.cube((0.15, 0.05, 0.17), place((side * 0.14, 0.42, 0.58)), srgb(EYE_WHITE), bone="Body")
        b.cube((0.07, 0.03, 0.09), place((side * 0.11, 0.452, 0.56)), srgb(PUPIL), bone="Body")
        b.cube((0.18, 0.04, 0.04), place((side * 0.14, 0.43, 0.71), rot=(0, -side * 0.45, 0)), srgb(BROW), bone="Body")
        b.cube((0.11, 0.17, 0.07), place((side * 0.16, 0.12, 0.035)), srgb(BROW), bone="Foot_L" if side < 0 else "Foot_R")
    b.cube((0.14, 0.03, 0.03), place((0, 0.44, 0.42)), srgb(PUPIL), bone="Body")
    b.build(arm)
    # Paso de 20 fotogramas (0,667 s a 30 fps), en bucle e in-place. Body apunta a +Z: su eje Y
    # local es el vertical (bote y squash), su Z local mira a −Y (contoneo lateral).
    keys = {}
    for f in range(0, 21, 5):
        phase = f / 20 * 2 * math.pi
        lift = abs(math.sin(phase))
        squash = 1 - 0.1 * (1 - lift)
        keys[f] = {
            "Body": {"loc": (0, 0.06 * lift, 0), "rot": (0, 0, 0.1 * math.sin(phase)), "scale": (1 + 0.08 * (1 - lift), squash, 1 + 0.08 * (1 - lift))},
            "Foot_L": {"loc": (0, 0.09 * math.sin(phase), 0.06 * max(0, math.sin(phase)))},
            "Foot_R": {"loc": (0, -0.09 * math.sin(phase), 0.06 * max(0, -math.sin(phase)))},
        }
    keyframes(arm, "Pelusa_Walk", 20, keys)


# --------------------------------------------------------------------------- Doña Remedios
REM = dict(skin=0xF2C6A0, tights=0xD8B48A, hair_grey=0xD9D9E6, hair_lavender=0xC9C1E4, dress=0x9B4F96, dress_dark=0x6F3470,
           apron=0xF1E9D6, pocket=0x8FB8E8, cardigan=0xE8D6A8, shawl=0x5E3D7A, button=0x6B4226, slipper=0x3F7FD6, pompom=0xFF9AC8,
           chancla=0xE8B020, strap=0xC4581C, glasses=0x2A2233, lens=0xD6ECFF, cheek=0xE8909A, gold=0xF0C040, bag=0x8A2E3B)
HIP = 0.46


def author_remedios():
    """Doña Remedios (personaje del jugador), versión representativa del spike: vestido con
    franja, delantal con bolsillo, rebeca con botones, toquilla, permanente gris y lavanda,
    gafas, coloretes, pendientes, zapatillas con pompón, chancla en la derecha y bolso en la
    izquierda. 12 huesos; clips de andar e inactiva in-place."""
    empty_scene()
    c = {k: srgb(v) for k, v in REM.items()}
    arm = make_armature("B0_Remedios_Rig", [
        ("Root", (0, 0, 0), (0, 0, 0.15), None),
        ("Hips", (0, 0, HIP), (0, 0, HIP + 0.2), "Root"),
        ("Spine", (0, 0, HIP + 0.2), (0, 0, HIP + 0.72), "Hips"),
        ("Head", (0, 0, HIP + 0.76), (0, 0, HIP + 1.1), "Spine"),
        ("UpperArm_L", (-0.25, 0, HIP + 0.7), (-0.25, 0, HIP + 0.52), "Spine"),
        ("Forearm_L", (-0.25, 0, HIP + 0.52), (-0.25, 0, HIP + 0.3), "UpperArm_L"),
        ("UpperArm_R", (0.25, 0, HIP + 0.7), (0.25, 0, HIP + 0.52), "Spine"),
        ("Forearm_R", (0.25, 0, HIP + 0.52), (0.25, 0, HIP + 0.3), "UpperArm_R"),
        ("Thigh_L", (-0.1, 0, HIP), (-0.1, 0, 0.25), "Hips"),
        ("Shin_L", (-0.1, 0, 0.25), (-0.1, 0, 0.04), "Thigh_L"),
        ("Thigh_R", (0.1, 0, HIP), (0.1, 0, 0.25), "Hips"),
        ("Shin_R", (0.1, 0, 0.25), (0.1, 0, 0.04), "Thigh_R"),
    ])
    b = Builder("B0_Remedios", [material("B0_Remedios", c["dress"])])
    h = HIP
    # Piernas (medias), zapatillas con pompón: el vestido cubre por encima de la rodilla.
    for side, s in (("L", -1), ("R", 1)):
        x = s * 0.1
        b.box((x - 0.055, -0.055, 0.25), (x + 0.055, 0.055, h), c["tights"], bone=f"Thigh_{side}")
        b.box((x - 0.055, -0.055, 0.07), (x + 0.055, 0.055, 0.25), c["tights"], bone=f"Shin_{side}")
        b.box((x - 0.075, -0.09, 0.0), (x + 0.075, 0.18, 0.08), c["slipper"], bone=f"Shin_{side}")
        b.ico(0.045, 1, place((x, 0.14, 0.1)), c["pompom"], bone=f"Shin_{side}")
    # Vestido (cono de 7 lados) con franja oscura en el bajo, delantal inclinado y bolsillo.
    b.cylinder(0.36, 0.2, 0.56, 7, place((0, 0, h + 0.2)), c["dress"], bone="Hips")
    b.cylinder(0.37, 0.365, 0.07, 7, place((0, 0, h - 0.045)), c["dress_dark"], bone="Hips")
    tilt = math.atan2(0.16, 0.56)
    b.cube((0.32, 0.03, 0.42), place((0, 0.305, h + 0.17), rot=(tilt, 0, 0)), c["apron"], bone="Hips")
    b.cube((0.13, 0.02, 0.08), place((0.04, 0.305 + 0.07 * math.sin(tilt) + 0.02, h + 0.1), rot=(tilt, 0, 0)), c["pocket"], bone="Hips")
    # Cintura, rebeca con botones y toquilla sobre los hombros.
    b.cylinder(0.212, 0.212, 0.04, 7, place((0, 0, h + 0.46)), c["apron"], bone="Spine")
    b.cylinder(0.22, 0.19, 0.34, 7, place((0, 0, h + 0.62)), c["cardigan"], bone="Spine")
    for z in (0.53, 0.61):
        b.cube((0.03, 0.02, 0.03), place((0, 0.212, h + z)), c["button"], bone="Spine")
    b.cylinder(0.27, 0.13, 0.17, 7, place((0, -0.01, h + 0.72)), c["shawl"], bone="Spine")
    # Cabeza: cara hacia +Y, gafas, nariz, coloretes, pendientes, pelo cardado y permanente.
    b.ico(0.17, 1, place((0, 0, h + 0.92)), c["skin"], bone="Head")
    b.ico(0.18, 1, place((0, -0.02, h + 0.98), scale=(1, 1, 0.75)), c["hair_grey"], bone="Head")
    for i, deg in enumerate((65, 110, 155, 205, 250, 295)):
        a = math.radians(deg)
        b.ico(0.065, 1, place((math.sin(a) * 0.165, math.cos(a) * 0.165, h + (0.9 if i % 2 == 0 else 0.99))),
              c["hair_grey"] if i % 2 == 0 else c["hair_lavender"], bone="Head")
    for i, (x, y, z) in enumerate(((0.07, -0.03, 1.07), (-0.07, -0.03, 1.07), (0, 0.05, 1.09), (0, -0.1, 1.06))):
        b.ico(0.07, 1, place((x, y, h + z)), c["hair_lavender"] if i % 2 else c["hair_grey"], bone="Head")
    for s in (-1, 1):
        b.cube((0.1, 0.02, 0.075), place((s * 0.062, 0.16, h + 0.94)), c["glasses"], bone="Head")
        b.cube((0.066, 0.012, 0.045), place((s * 0.062, 0.171, h + 0.94)), c["lens"], bone="Head")
        b.cube((0.018, 0.13, 0.018), place((s * 0.158, 0.09, h + 0.945)), c["glasses"], bone="Head")
        b.cube((0.05, 0.02, 0.03), place((s * 0.095, 0.148, h + 0.865)), c["cheek"], bone="Head")
        b.cube((0.025, 0.025, 0.035), place((s * 0.172, 0.01, h + 0.86)), c["gold"], bone="Head")
    b.cube((0.05, 0.06, 0.06), place((0, 0.18, h + 0.885)), c["skin"], bone="Head")
    # Brazos (mangas de rebeca y mano); chancla en la derecha (+X), bolso en la izquierda.
    for side, s in (("L", -1), ("R", 1)):
        x = s * 0.25
        b.box((x - 0.045, -0.045, h + 0.52), (x + 0.045, 0.045, h + 0.7), c["cardigan"], bone=f"UpperArm_{side}")
        b.box((x - 0.045, -0.045, h + 0.34), (x + 0.045, 0.045, h + 0.52), c["cardigan"], bone=f"Forearm_{side}")
        b.box((x - 0.04, -0.04, h + 0.26), (x + 0.04, 0.04, h + 0.34), c["skin"], bone=f"Forearm_{side}")
    b.cube((0.11, 0.27, 0.03), place((0.25, 0.08, h + 0.25)), c["chancla"], bone="Forearm_R")
    b.cube((0.1, 0.06, 0.035), place((0.25, 0.14, h + 0.275)), c["strap"], bone="Forearm_R")
    b.cube((0.12, 0.018, 0.018), place((-0.25, 0, h + 0.26)), c["bag"], bone="Forearm_L")
    b.cube((0.17, 0.08, 0.13), place((-0.25, 0, h + 0.18)), c["bag"], bone="Forearm_L")
    b.cube((0.04, 0.02, 0.03), place((-0.25, 0.045, h + 0.22)), c["gold"], bone="Forearm_L")
    b.build(arm)
    # Andar: 24 fotogramas (0,8 s) en bucle, in-place. Huesos de piernas/brazos apuntan a −Z:
    # su X local es la X del mundo; girar en X balancea adelante/atrás.
    walk = {}
    for f in range(0, 25, 3):
        p = f / 24 * 2 * math.pi
        swing = math.sin(p)
        bob = 0.03 * abs(math.cos(p))
        walk[f] = {
            "Hips": {"loc": (0, bob - 0.015, 0), "rot": (0, 0.08 * swing, 0)},
            "Spine": {"rot": (0.06, -0.12 * swing, 0)},
            "Head": {"rot": (-0.04, 0.06 * swing, 0)},
            "Thigh_L": {"rot": (0.45 * swing, 0, 0)},
            "Shin_L": {"rot": (-0.5 * max(0, -swing), 0, 0)},
            "Thigh_R": {"rot": (-0.45 * swing, 0, 0)},
            "Shin_R": {"rot": (-0.5 * max(0, swing), 0, 0)},
            "UpperArm_L": {"rot": (-0.35 * swing, 0, 0.1)},
            "Forearm_L": {"rot": (-0.2, 0, 0)},
            "UpperArm_R": {"rot": (0.35 * swing, 0, -0.1)},
            "Forearm_R": {"rot": (-0.35, 0, 0)},
        }
    keyframes(arm, "Remedios_Walk", 24, walk)
    # Inactiva: 60 fotogramas (2 s), respiración y vaivén leve de la chancla.
    idle = {}
    for f in range(0, 61, 10):
        p = f / 60 * 2 * math.pi
        idle[f] = {
            "Hips": {"loc": (0, -0.008 * (1 - math.cos(p)), 0)},
            "Spine": {"rot": (0.03 * math.sin(p), 0, 0)},
            "Head": {"rot": (-0.03 * math.sin(p), 0.05 * math.sin(p), 0)},
            "UpperArm_R": {"rot": (0.1 + 0.08 * math.sin(p), 0, -0.08)},
            "Forearm_R": {"rot": (-0.5 - 0.15 * math.sin(p), 0, 0)},
            "UpperArm_L": {"rot": (0, 0, 0.08)},
        }
    keyframes(arm, "Remedios_Idle", 60, idle)


# --------------------------------------------------------------------------- módulo de pared
STONE_LIGHT, STONE, STONE_DARK, COBBLE_DARK = 0xD2CAB8, 0xB4AB9A, 0x8C8477, 0x857D70


def author_pared():
    """Módulo de muro de sillería, 4 × 3 × 0,5 m. Pivote: esquina inferior izquierda sobre el
    eje del muro (x 0…4, y −0,25…0,25, z 0…3); se repite cada 4 m en X sin solapes. Zócalo,
    cuerpo, albardilla y sillares en relieve con aparejo a matajunta en ambas caras."""
    empty_scene()
    rng = random.Random("b0-pared")
    b = Builder("B0_ParedModulo", [material("B0_Piedra", srgb(STONE))], uv_scale=8.0)
    b.box((0, -0.25, 0.4), (4, 0.25, 2.8), srgb(STONE_DARK))
    b.box((0, -0.3, 0), (4, 0.3, 0.4), srgb(COBBLE_DARK))
    b.box((0, -0.3, 2.8), (4, 0.3, 3.0), srgb(STONE_LIGHT))
    tones = [srgb(STONE_LIGHT), srgb(STONE), srgb(STONE)]
    rows = [(0.45, 1.2), (1.2, 2.0), (2.0, 2.75)]
    for face in (1, -1):
        for r, (z0, z1) in enumerate(rows):
            # Matajunta que continúa al repetir el módulo: filas pares con media junta en los
            # bordes; filas impares con medios sillares sin junta en el borde, que se unen con el
            # módulo vecino en un sillar entero. Los sillares de borde tienen relieve fijo.
            starts = [0, 1, 2, 3] if r % 2 == 0 else [0, 0.5, 1.5, 2.5, 3.5]
            gap, edge_gap = 0.04, (0.02 if r % 2 == 0 else 0.0)
            for i, x0 in enumerate(starts):
                x1 = starts[i + 1] if i + 1 < len(starts) else 4
                at_edge = x0 == 0 or x1 == 4
                depth = 0.05 if at_edge else 0.04 + rng.uniform(0, 0.02)
                lo = (x0 + (gap if x0 > 0 else edge_gap), 0.25 * face, z0 + gap)
                hi = (x1 - (gap if x1 < 4 else edge_gap), (0.25 + depth) * face, z1 - gap)
                b.box((lo[0], min(lo[1], hi[1]), lo[2]), (hi[0], max(lo[1], hi[1]), hi[2]), rng.choice(tones))
    b.build()


AUTHORS = {"B0_Fixture": author_fixture, "B0_Pelusa": author_pelusa, "B0_Remedios": author_remedios, "B0_ParedModulo": author_pared}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    asset, force = argv[0], "--force" in argv
    manifest_path, manifest = contract.load_manifest()
    entry = next((a for a in manifest["assets"] if a["id"] == asset), None)
    if entry is None or asset not in AUTHORS:
        raise SystemExit(f"Asset desconocido: {asset}")
    source = manifest_path.parent / entry["source"]
    if source.exists() and not force:
        print(f"B0 author: {source.name} ya existe; la fuente manda (usar --force para regenerar)")
        return
    AUTHORS[asset]()
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    lo, hi = contract.world_bounds(meshes)
    print("B0 bounds", [round(v, 4) for v in lo], [round(v, 4) for v in hi])
    errors = contract.validate(entry)
    if errors:
        raise SystemExit("La fuente generada incumple el contrato:\n- " + "\n- ".join(errors))
    source.parent.mkdir(parents=True, exist_ok=True)
    # Sin copia .blend1 (preferencias de fábrica en memoria; no se guardan).
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(source), compress=False, relative_remap=True)
    print(f"B0 author: {asset} → {source}")


if __name__ == "__main__":
    main()
