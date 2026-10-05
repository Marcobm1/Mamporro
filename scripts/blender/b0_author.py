"""Crea las fuentes .blend iniciales de B0 a partir de código (Blender en background).

Solo genera una fuente si no existe (o con --force): una vez creada, el .blend es la
fuente editable y manda sobre este script. Todo el contenido es original de MAMPORRO.
Uso: blender --background --factory-startup --python b0_author.py -- <id> [--force]
"""
import math
import pathlib
import sys

import bmesh
import bpy
from mathutils import Vector

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


def material(name, srgb):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    mat.diffuse_color = (*lin, 1)
    return mat


class Builder:
    """Malla por partes con color de vértice y slot por parte, UV por proyección de caja."""

    def __init__(self, name, materials):
        self.name = name
        self.bm = bmesh.new()
        self.colors = self.bm.loops.layers.color.new(contract.COLOR_NAME)
        self.uv = self.bm.loops.layers.uv.new(contract.UV_NAME)
        self.materials = materials

    def _finish_faces(self, faces, srgb, slot, smooth, uv_scale):
        for f in faces:
            f.material_index = slot
            f.smooth = smooth
            n = f.normal
            axis = max(range(3), key=lambda i: abs(n[i]))
            u_axis, v_axis = [(1, 2), (0, 2), (0, 1)][axis]
            for loop in f.loops:
                co = loop.vert.co
                loop[self.colors] = (*srgb, 1.0)
                loop[self.uv].uv = (co[u_axis] / uv_scale + 0.5, co[v_axis] / uv_scale + 0.5)

    def box(self, lo, hi, srgb, slot=0, smooth=False, uv_scale=5.0):
        lo, hi = Vector(lo), Vector(hi)
        res = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = res["verts"]
        for v in verts:
            v.co = Vector((lo.x if v.co.x < 0 else hi.x, lo.y if v.co.y < 0 else hi.y, lo.z if v.co.z < 0 else hi.z))
        faces = list({f for v in verts for f in v.link_faces})
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._finish_faces(faces, srgb, slot, smooth, uv_scale)
        return verts

    def cylinder(self, center, radius, depth, segments, srgb, slot=0, smooth=True, uv_scale=5.0, scale=(1, 1, 1)):
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius, depth=depth)
        verts = res["verts"]
        for v in verts:
            v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + Vector(center)
        faces = list({f for v in verts for f in v.link_faces})
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._finish_faces(faces, srgb, slot, smooth, uv_scale)
        return verts

    def sphere(self, center, radius, srgb, slot=0, segments=8, rings=6, scale=(1, 1, 1), smooth=True, uv_scale=5.0):
        res = bmesh.ops.create_uvsphere(self.bm, u_segments=segments, v_segments=rings, radius=radius)
        verts = res["verts"]
        for v in verts:
            v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + Vector(center)
        faces = list({f for v in verts for f in v.link_faces})
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._finish_faces(faces, srgb, slot, smooth, uv_scale)
        return verts

    def build(self):
        me = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        # La capa de color de bmesh es BYTE_COLOR en esquinas y guarda sRGB (comprobado en 5.2.2).
        self.bm.to_mesh(me)
        self.bm.free()
        for mat in self.materials:
            me.materials.append(mat)
        obj = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(obj)
        return obj


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
    b.cylinder((0, 0, 2.2), 0.1, 0.4, 8, (0.5, 0.5, 0.5), slot=0, smooth=True)
    b.build()


AUTHORS = {"B0_Fixture": author_fixture}


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
