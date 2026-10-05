"""Pruebas del contrato y del exportador B0 (Blender en background).

Nunca guarda las fuentes: las variantes rotas se crean en memoria o como copias temporales.
Uso: blender --background --factory-startup --python b0_test.py -- <informe.json>
"""
import json
import pathlib
import shutil
import sys
import tempfile
import traceback

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import b0_contract as contract  # noqa: E402
import b0_export as exporter  # noqa: E402

results = []


def case(name):
    def wrap(fn):
        try:
            fn()
            results.append({"case": name, "ok": True})
        except Exception as e:  # noqa: BLE001 - se registra cualquier fallo del caso
            results.append({"case": name, "ok": False, "error": f"{type(e).__name__}: {e}", "trace": traceback.format_exc(limit=3)})
        return fn
    return wrap


MANIFEST_PATH, MANIFEST = contract.load_manifest()
SOURCES = {a["id"]: a for a in MANIFEST["assets"]}


def open_source(asset_id):
    entry = SOURCES[asset_id]
    bpy.ops.wm.open_mainfile(filepath=str(MANIFEST_PATH.parent / entry["source"]), load_ui=False)
    return entry


def expect_error(entry, fragment, mutate):
    mutate()
    errors = contract.validate(entry)
    assert any(fragment in e for e in errors), f"se esperaba un error con {fragment!r}; hubo {errors}"


def fixture_case(name, fragment, mutate, entry_patch=None):
    @case(f"contrato rechaza: {name}")
    def _():
        entry = dict(open_source("B0_Fixture"))
        if entry_patch:
            entry.update(entry_patch)
        expect_error(entry, fragment, mutate)


# ---------------------------------------------------------------- fuentes válidas
for asset_id in SOURCES:
    @case(f"fuente válida: {asset_id}")
    def _(asset_id=asset_id):
        entry = open_source(asset_id)
        errors = contract.validate(entry)
        assert not errors, errors


# ---------------------------------------------------------------- contrato roto (en memoria)
def obj():
    return bpy.data.objects["B0_Fixture"]


fixture_case("escala sin aplicar", "transform sin aplicar", lambda: setattr(obj(), "scale", (2, 2, 2)))
fixture_case("rotación sin aplicar", "transform sin aplicar", lambda: setattr(obj().rotation_euler, "z", 0.5))
fixture_case("pivote desplazado", "transform sin aplicar", lambda: setattr(obj(), "location", (0.3, 0, 0)))
fixture_case("unidades en centímetros", "Unidades", lambda: setattr(bpy.context.scene.unit_settings, "scale_length", 0.01))
fixture_case("nombre con sufijo .001", "Nombre no ASCII", lambda: None, {"objects": ["B0_Fixture.001"]})
fixture_case("nombre no ASCII", "Falta el objeto", lambda: setattr(obj(), "name", "B0_Fixtüre"))
fixture_case("sin UV", "UV", lambda: obj().data.uv_layers.remove(obj().data.uv_layers[0]))
fixture_case("segunda capa de color", "color de vértice", lambda: obj().data.color_attributes.new("Extra", "BYTE_COLOR", "CORNER"))
fixture_case("slot de material cambiado", "materiales", lambda: setattr(obj().material_slots[1], "material", bpy.data.materials.new("Otro")))
fixture_case("malla con otro nombre", "debe llamarse como su objeto", lambda: setattr(obj().data, "name", "Malla"))
fixture_case("modificador sin aplicar", "modificador SUBSURF", lambda: obj().modifiers.new("S", "SUBSURF"))
fixture_case("acción fuera del manifiesto", "Acciones fuera del manifiesto", lambda: bpy.data.actions.new("Suelta"))


def _camera_in_manifest():
    cam = bpy.data.objects.new("B0_Camara", bpy.data.cameras.new("B0_Camara"))
    bpy.context.scene.collection.objects.link(cam)


fixture_case("cámara en el manifiesto", "tipo CAMERA", _camera_in_manifest, {"objects": ["B0_Fixture", "B0_Camara"]})


def _scale_mesh_100():
    for v in obj().data.vertices:
        v.co *= 100


fixture_case("factor 100 aplicado en la malla", "Bounds", _scale_mesh_100)


# ---------------------------------------------------------------- exportador
TMP = pathlib.Path(tempfile.mkdtemp(prefix="MAMPORRO B0 prueba "))
OUT = TMP / "Unity Assets"
ENTRY = SOURCES["B0_Fixture"]
SOURCE = MANIFEST_PATH.parent / ENTRY["source"]
SOURCE_SHA = contract.sha256(SOURCE)
state = {}


@case("exporta a una ruta con espacios y reimporta")
def _():
    r = exporter.export_asset(MANIFEST_PATH, ENTRY, OUT)
    assert r["status"] == "exportado" and r["reimport"]["triangles"] == r["triangles"], r
    fbx = OUT / ENTRY["output"]
    assert fbx.is_file() and fbx.with_suffix(".b0.json").is_file()
    assert not list(fbx.parent.glob("*.tmp")), "quedó un temporal"
    state["first"] = r


@case("repetir sin cambios conserva la salida")
def _():
    r = exporter.export_asset(MANIFEST_PATH, ENTRY, OUT)
    assert r["status"] == "sin cambios", r
    assert r["fbxSha256"] == state["first"]["fbxSha256"]


@case("forzar reexporta con el mismo contenido semántico")
def _():
    r = exporter.export_asset(MANIFEST_PATH, ENTRY, OUT, force=True)
    assert r["status"] == "exportado" and r["digest"] == state["first"]["digest"], r
    state["forced"] = r


@case("la fuente .blend no cambia al exportar")
def _():
    assert contract.sha256(SOURCE) == SOURCE_SHA


def _variant(name, mutate):
    """Copia temporal de la fuente con una alteración; la fuente real no se toca."""
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False)
    mutate()
    path = TMP / "fuentes" / f"{name}.blend"
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(path), copy=True)
    return path


def _manifest_with(source):
    data = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    entry = next(a for a in data["assets"] if a["id"] == "B0_Fixture")
    entry["source"] = str(source)
    path = TMP / "manifest.json"
    path.write_text(json.dumps(data), encoding="utf-8")
    return path, entry


@case("no depende de selección, visibilidad ni objetos ajenos")
def _():
    def mutate():
        cam = bpy.data.objects.new("Camara_ajena", bpy.data.cameras.new("Camara_ajena"))
        bpy.context.scene.collection.objects.link(cam)
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        cam.select_set(True)
        bpy.context.view_layer.objects.active = cam
        bpy.data.objects["B0_Fixture"].hide_set(True)
    source = _variant("seleccion-ajena", mutate)
    manifest, entry = _manifest_with(source)
    r = exporter.export_asset(manifest, entry, TMP / "Otra salida", force=True)
    assert r["digest"] == state["first"]["digest"], "el contenido cambió con la selección"


@case("un fallo de contrato conserva la última salida válida")
def _():
    fbx = OUT / ENTRY["output"]
    before = fbx.read_bytes()
    source = _variant("rota", lambda: setattr(bpy.data.objects["B0_Fixture"], "scale", (1, 1, 2)))
    manifest, entry = _manifest_with(source)
    try:
        exporter.export_asset(manifest, entry, OUT)
        raise AssertionError("la exportación debía fallar")
    except exporter.ExportError as e:
        assert "transform sin aplicar" in str(e), e
    assert fbx.read_bytes() == before, "se sobrescribió la salida válida"
    assert not list(fbx.parent.glob("*.tmp")), "quedó un temporal"


@case("fuente ausente falla sin tocar la salida")
def _():
    fbx = OUT / ENTRY["output"]
    before = fbx.read_bytes()
    manifest, entry = _manifest_with(TMP / "no-existe.blend")
    try:
        exporter.export_asset(manifest, entry, OUT)
        raise AssertionError("la exportación debía fallar")
    except exporter.ExportError as e:
        assert "Fuente ausente" in str(e)
    assert fbx.read_bytes() == before


def main():
    report = pathlib.Path(sys.argv[sys.argv.index("--") + 1])
    report.parent.mkdir(parents=True, exist_ok=True)
    failed = [r for r in results if not r["ok"]]
    report.write_text(json.dumps({"passed": len(results) - len(failed), "total": len(results), "results": results, "temp": str(TMP)}, indent=2, ensure_ascii=False), encoding="utf-8")
    for r in results:
        print(("OK   " if r["ok"] else "FALLO ") + r["case"] + ("" if r["ok"] else " → " + r["error"]))
    print(f"B0 test: {len(results) - len(failed)}/{len(results)} correctas")
    if failed:
        sys.exit(1)
    shutil.rmtree(TMP, ignore_errors=True)


main()
