"""Build the production-candidate Blobby shell on the animated source rig."""
from pathlib import Path
import math
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate

source, export, atlas = map(Path, sys.argv[sys.argv.index("--") + 1:])
if any(path.exists() for path in (source, export, atlas)):
    raise FileExistsError("Choose unused output paths.")
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
parts = [obj for obj in rig.children if obj.type == "MESH"]

# Fuse the head, body and limb joints into a continuous jelly shell.
for position, scale in (
    ((0, 0, 1.25), (.42, .35, .48)),
    ((.62, 0, 1.09), (.24, .16, .17)),
    ((-.62, 0, 1.09), (.24, .16, .17)),
):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, location=position)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    parts.append(obj)

bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[-1]
bpy.ops.object.join()
shell = bpy.context.object
shell.name = "SK_Blobby"
shell.vertex_groups.clear()
remesh = shell.modifiers.new("Continuous Shell", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = .036
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth = shell.modifiers.new("Jelly Surface", "SMOOTH")
smooth.factor = .7
smooth.iterations = 5
bpy.ops.object.modifier_apply(modifier=smooth.name)
shell.data.calc_loop_triangles()
if len(shell.data.loop_triangles) > 11500:
    decimate = shell.modifiers.new("Mobile Triangle Budget", "DECIMATE")
    decimate.ratio = 11500 / len(shell.data.loop_triangles)
    bpy.ops.object.modifier_apply(modifier=decimate.name)

bones = [bone for bone in rig.data.bones if bone.use_deform]
groups = {bone.name: shell.vertex_groups.new(name=bone.name) for bone in bones}

def distance(point, bone):
    axis = bone.tail_local - bone.head_local
    factor = max(0, min(1, (point - bone.head_local).dot(axis) / axis.length_squared))
    return (point - (bone.head_local + factor * axis)).length_squared

for vertex in shell.data.vertices:
    candidates = sorted(bones, key=lambda bone: distance(vertex.co, bone))[:4]
    weights = [1 / (.012 + distance(vertex.co, bone)) ** 3 for bone in candidates]
    for bone, weight in zip(candidates, weights):
        groups[bone.name].add([vertex.index], weight / sum(weights), "REPLACE")

uv = shell.data.uv_layers.new(name="BlobbyAtlas")
for face in shell.data.polygons:
    face.use_smooth = True
    for loop in face.loop_indices:
        point = shell.data.vertices[shell.data.loops[loop].vertex_index].co
        u = .25 + point.x / 5.2 if face.normal.y < -.10 else .75 + point.x / 5.2
        uv.data[loop].uv = (u, min(.99, max(.01, point.z / 1.95)))

size = 1024
image = bpy.data.images.new("Blobby_Body_Albedo", width=size, height=size, alpha=False)
pixels = []
for row in range(size):
    z = 1.95 * (row + .5) / size
    for col in range(size):
        x = ((col + .5) / size - .25) * 5.2
        color = (.70, .93, .17, 1)
        eye = ((abs(x) - .125) / .039) ** 2 + ((z - 1.58) / .065) ** 2
        mouth_x = x / .245
        upper = 1.466 + .072 * mouth_x ** 2
        lower = 1.315 + .223 * mouth_x ** 2
        if eye < 1 or (abs(mouth_x) < 1 and lower < z < upper):
            color = (.055, .17, .045, 1)
            if eye >= 1 and abs(mouth_x) < .91 and lower + .017 < z < upper - .014:
                color = (.97, 1, .91, 1)
        pixels.extend(color)
image.pixels.foreach_set(pixels)
atlas.parent.mkdir(parents=True, exist_ok=True)
image.filepath_raw = str(atlas.resolve())
image.file_format = "PNG"
image.save()
image.pack()

mat = bpy.data.materials.new("Blobby_Jelly")
mat.diffuse_color = (.70, .93, .17, 1)
mat.use_nodes = True
nodes = mat.node_tree.nodes
shader = nodes.get("Principled BSDF")
shader.inputs["Roughness"].default_value = .26
texture = nodes.new("ShaderNodeTexImage")
texture.image = image
mat.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
shell.data.materials.clear()
shell.data.materials.append(mat)
shell.parent = rig
skin = shell.modifiers.new("Skin", "ARMATURE")
skin.object = rig

problems = validate(bpy.context.scene)
if problems:
    raise RuntimeError("\n".join(problems))

bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()), axis_forward="-Z", axis_up="Y",
                         add_leaf_bones=False, object_types={"ARMATURE", "MESH"},
                         bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_simplify_factor=0, path_mode="COPY", embed_textures=True)
