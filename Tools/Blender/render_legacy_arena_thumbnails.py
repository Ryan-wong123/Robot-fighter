"""Render the two original arena previews from their authored Unity primitives.
Run with Python (requires PyYAML) and --blender PATH. Unity scenes are read only.
Blender runs headless; intermediate scene data stays in ignored Logs.
"""
import argparse
import json
from pathlib import Path
import re
import struct
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
STAGES = ("BathtubBrawl", "KitchenCounter")
OUT = ROOT / "Assets/_Project/Art/Resources/StagePreviews"


def extract(stage):
    import yaml
    source = (ROOT / "Assets/_Project/Scenes" / (stage + ".unity")).read_text(encoding="utf-8")
    records = {}
    for block in re.split(r"^--- !u!", source, flags=re.M)[1:]:
        header, body = block.split("\n", 1)
        class_id, file_id = header.split(" &")
        if "stripped" in file_id:
            continue
        records[int(file_id)] = (int(class_id), next(iter(yaml.safe_load(body).values())))
    transforms = {str(key): value for key, (kind, value) in records.items() if kind == 4}
    renderers = {value["m_GameObject"]["fileID"]: value for kind, value in records.values() if kind == 23}
    colors = {}
    for material in (ROOT / "Assets/_Project/Art" / stage).glob("*.mat"):
        guid = re.search(r"guid: (\w+)", material.with_suffix(".mat.meta").read_text(encoding="utf-8"))[1]
        match = re.search(r"_BaseColor: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)", material.read_text(encoding="utf-8"))
        if match:
            colors[guid] = list(map(float, match.groups()))
    meshes = []
    for kind, value in records.values():
        if kind != 33:
            continue
        owner = value["m_GameObject"]["fileID"]
        renderer = renderers.get(owner)
        mesh = value["m_Mesh"]
        if renderer is None or not renderer.get("m_Enabled", 1) or mesh.get("fileID") not in (10202, 10206, 10207, 10208, 10209, 10210):
            continue
        transform = by_object[owner]
        ancestor = transform
        visible = True
        while ancestor:
            t = transforms[str(ancestor)]
            visible &= bool(objects[t["m_GameObject"]["fileID"]].get("m_IsActive", 1))
            ancestor = t["m_Father"]["fileID"]
        if not visible:
            continue
        mats = renderer.get("m_Materials", [])
        color = colors.get(mats[0].get("guid") if mats else None, [.8, .86, .92, 1])
        meshes.append(dict(transform=transform, primitive=mesh["fileID"], color=color))
    return dict(transforms=transforms, meshes=meshes)


def render(data, destination):
    import bpy
    from mathutils import Matrix, Quaternion, Vector
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    basis = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
    cache = {}
    def world(key):
        key = str(key)
        if key in cache:
            return cache[key]
        t = data["transforms"][key]
        position, rotation, scale = t["m_LocalPosition"], t["m_LocalRotation"], t["m_LocalScale"]
        matrix = Matrix.LocRotScale(Vector(tuple(position[a] for a in "xyz")),
            Quaternion(tuple(rotation[a] for a in "wxyz")), Vector(tuple(scale[a] for a in "xyz")))
        parent = t["m_Father"]["fileID"]
        cache[key] = world(parent) @ matrix if parent else matrix
        return cache[key]
    materials = {}
    for entry in data["meshes"]:
        primitive = entry["primitive"]
        if primitive == 10202:
            bpy.ops.mesh.primitive_cube_add(size=1)
        elif primitive in (10206, 10207):
            bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=.5)
        elif primitive == 10208:
            bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=.5, depth=2, rotation=(1.5707963268, 0, 0))
            bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        else:
            bpy.ops.mesh.primitive_plane_add(size=10 if primitive == 10209 else 1, rotation=(1.5707963268, 0, 0) if primitive == 10209 else (0, 0, 0))
            bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        obj = bpy.context.object
        if primitive == 10206:
            obj.scale.y = 2
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        obj.matrix_world = basis @ world(entry["transform"])
        key = tuple(entry["color"][:3])
        if key not in materials:
            mat = bpy.data.materials.new("Arena palette")
            mat.diffuse_color = (*key, 1)
            materials[key] = mat
        obj.data.materials.append(materials[key])
        if primitive in (10206, 10207, 10208):
            for polygon in obj.data.polygons:
                polygon.use_smooth = True
    scene = bpy.context.scene
    scene.world.color = (.32, .38, .48)
    bpy.ops.object.light_add(type="AREA", location=(-3, 6, 10))
    light = bpy.context.object
    light.data.energy = 2300
    light.data.size = 12
    light.rotation_euler = (Vector((0, -2, 1)) - light.location).to_track_quat("-Z", "Y").to_euler()
    bpy.ops.object.camera_add(location=(0, 19, 5))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, -1, 2.2)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 18
    camera.scale.x = -1
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.render.resolution_x, scene.render.resolution_y = 960, 540
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(destination)
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--blender")
    parser.add_argument("--render-data")
    parser.add_argument("--destination")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else None)
    if args.render_data:
        render(json.loads(Path(args.render_data).read_text(encoding="utf-8")), Path(args.destination))
    elif args.check:
        for stage in STAGES:
            path = OUT / (stage + ".png")
            raw = path.read_bytes()
            assert raw[:8] == b"\x89PNG\r\n\x1a\n" and struct.unpack(">II", raw[16:24]) == (960, 540), path
        print("PASS: both arena previews are 960 x 540 PNGs")
    else:
        if not args.blender:
            parser.error("--blender is required to render")
        OUT.mkdir(parents=True, exist_ok=True)
        logs = ROOT / "Logs/ArenaPreviews"
        logs.mkdir(parents=True, exist_ok=True)
        for stage in STAGES:
            data = logs / (stage + ".json")
            data.write_text(json.dumps(extract(stage)), encoding="utf-8")
            subprocess.run([args.blender, "--factory-startup", "-b", "--python-exit-code", "1", "--python", str(Path(__file__).resolve()), "--", "--render-data", str(data), "--destination", str(OUT / (stage + ".png"))], check=True)
