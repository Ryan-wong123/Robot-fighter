"""Generate a replaceable Blobby rig blockout in a clean Blender session."""
from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.render.fps = 60
bpy.ops.object.armature_add()
rig = bpy.context.object
rig.name = "ARM_Blobby"
bpy.ops.object.mode_set(mode="EDIT")
bones = rig.data.edit_bones
root = bones[0]
root.name = "Root"
root.head, root.tail = (0, 0, 0), (0, 0, .35)
root.use_deform = False

def add(name, head, tail, parent):
    bone = bones.new(name)
    bone.head, bone.tail, bone.parent = head, tail, bones[parent]

add("Pelvis", (0, 0, .65), (0, 0, .9), "Root")
add("Spine", (0, 0, .9), (0, 0, 1.28), "Pelvis")
add("Head", (0, 0, 1.28), (0, 0, 1.78), "Spine")
for suffix, side in ((".L", 1), (".R", -1)):
    add("Upper_Arm"+suffix, (side*.38, 0, 1.23), (side*.68, 0, 1.08), "Spine")
    add("Lower_Arm"+suffix, (side*.68, 0, 1.08), (side*.84, 0, .95), "Upper_Arm"+suffix)
    add("Hand"+suffix, (side*.84, 0, .95), (side*.97, 0, .9), "Lower_Arm"+suffix)
    add("Upper_Leg"+suffix, (side*.22, 0, .66), (side*.26, 0, .43), "Pelvis")
    add("Lower_Leg"+suffix, (side*.26, 0, .43), (side*.3, 0, .2), "Upper_Leg"+suffix)
    add("Foot"+suffix, (side*.3, 0, .2), (side*.3, -.2, .09), "Lower_Leg"+suffix)
bpy.ops.object.mode_set(mode="OBJECT")

gel = bpy.data.materials.new("Blobby_Body_Albedo")
gel.diffuse_color = (.24, .75, .38, 1)
gel.use_nodes = True
gel.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = gel.diffuse_color

def mesh(name, bone, location, scale):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.materials.append(gel)
    for face in obj.data.polygons:
        face.use_smooth = True
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, "REPLACE")
    modifier = obj.modifiers.new("Skin", "ARMATURE")
    modifier.object = rig
    obj.parent = rig

mesh("SK_Body", "Spine", (0, 0, 1.05), (.51, .39, .58))
mesh("SK_Head", "Head", (0, -.04, 1.48), (.42, .35, .36))
for suffix, side in ((".L", 1), (".R", -1)):
    for name, x, z, scale in (
        ("Upper_Arm", .52, 1.16, (.21, .16, .18)),
        ("Lower_Arm", .75, 1.03, (.19, .15, .16)),
        ("Hand", .91, .93, (.16, .16, .17)),
        ("Upper_Leg", .23, .53, (.19, .19, .19)),
        ("Lower_Leg", .28, .32, (.17, .16, .18)),
        ("Foot", .3, .12, (.2, .24, .12)),
    ):
        mesh("SK_" + name.replace("_", "") + suffix[-1], name + suffix,
             (side*x, -.07 if name == "Foot" else 0, z), scale)

errors = validate(scene)
if errors:
    raise RuntimeError("\n".join(errors))

arguments = sys.argv[sys.argv.index("--") + 1:]
source, export = map(Path, arguments)
if source.exists() or export.exists():
    raise FileExistsError("Choose unused output paths to preserve existing art.")
for path in (source, export):
    path.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()), axis_forward="-Z", axis_up="Y",
                         add_leaf_bones=False, object_types={"ARMATURE", "MESH"})
