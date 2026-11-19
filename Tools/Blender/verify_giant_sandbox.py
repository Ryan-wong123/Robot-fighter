"""Validate the loaded static sandbox source; no character-rig assumptions."""
import math
import bpy

meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
expected = {'SM_Backdrop', 'SM_Paw', 'SM_Shadow', 'SM_Whiskers', 'SM_Spiral'}
errors = []
if {obj.name for obj in meshes} != expected:
    errors.append('Expected exactly the backdrop and four separate hazard meshes.')
triangles = sum(sum(len(face.vertices) - 2 for face in obj.data.polygons) for obj in meshes)
if triangles > 26000:
    errors.append('Stage art exceeds its 26k triangle allocation.')
for obj in meshes:
    if any(abs(value - 1) > .0001 for value in obj.scale):
        errors.append(obj.name + ': unapplied scale')
    if any(abs(value) > .0001 for value in obj.rotation_euler):
        errors.append(obj.name + ': unapplied rotation')
    if len(obj.data.materials) != 1 or not obj.data.uv_layers:
        errors.append(obj.name + ': expected one material and UVs')
    if any(not math.isfinite(value) for vertex in obj.data.vertices for value in vertex.co):
        errors.append(obj.name + ': invalid vertex')
if any(obj.type == 'ARMATURE' for obj in bpy.context.scene.objects):
    errors.append('Stage art must not have an armature.')
images = [image for image in bpy.data.images if image.name == 'GiantSandbox_Atlas']
if len(images) != 1 or list(images[0].size) != [1024, 1024] or not images[0].packed_file:
    errors.append('The shared 1024 atlas must be packed in the source.')
if errors:
    raise RuntimeError('\n'.join(errors))
print('PASS: sandbox geometry, 5 meshes,', triangles, 'triangles, hazard mesh split, UVs and packed atlas.')
