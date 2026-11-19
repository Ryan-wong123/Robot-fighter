"""Render a side-by-side phase preview without modifying the source blend file."""
import bpy
from pathlib import Path
from mathutils import Vector
scene=bpy.context.scene
rig=next(o for o in scene.objects if o.type=='ARMATURE')
mesh=next(o for o in scene.objects if o.type=='MESH')
second=rig.copy();second.data=rig.data.copy();scene.collection.objects.link(second)
second.animation_data_clear();second.animation_data_create();second.animation_data.action=bpy.data.actions['MrJack_OutIdle']
other=mesh.copy();other.data=mesh.data.copy();scene.collection.objects.link(other);other.parent=second
for modifier in other.modifiers:
 if modifier.type=='ARMATURE':modifier.object=second
rig.animation_data_create();rig.animation_data.action=bpy.data.actions['MrJack_Idle']
rig.location.x=-1;second.location.x=1
scene.frame_set(15)
scene.world.color=(.12,.12,.16)
bpy.ops.object.light_add(type='AREA',location=(-3,-4,6));bpy.context.object.data.energy=650;bpy.context.object.data.size=5
bpy.context.object.rotation_euler=(Vector((0,0,1))-bpy.context.object.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-9,3.5));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,1.1))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.7;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1280;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.filepath=str(Path(bpy.data.filepath).parent/'MrJack_Preview.png')
bpy.ops.render.render(write_still=True)
