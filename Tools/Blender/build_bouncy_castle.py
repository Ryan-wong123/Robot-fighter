"""Author original atlas-based fairground scenery from the approved stage color key."""
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

source, export, atlas, thumbnail = map(Path, sys.argv[sys.argv.index('--') + 1:])
if any(path.exists() for path in (source, export, atlas, thumbnail)):
    raise FileExistsError('Choose fresh output paths; sources are never overwritten.')
for path in (source, export, atlas, thumbnail):
    path.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
colors = ['3552A0', 'E95663', 'F7CE58', '57AAD9', '8BBF64', 'AB76BF', '242D48', 'F5E9C9',
          '445274', '6B719D', 'D99E83', '35665E', 'EBA36C', '5373B4', 'BD4565', '98DDD2']
texture = bpy.data.images.new('BouncyCastle_Atlas', width=1024, height=1024)
pixels = []
for y in range(1024):
    for x in range(1024):
        color = colors[(y // 256) * 4 + x // 256]
        pixels.extend([int(color[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1])
texture.pixels = pixels
texture.filepath_raw = str(atlas.resolve())
texture.file_format = 'PNG'
texture.save()
texture.pack()
material = bpy.data.materials.new('BouncyCastle_Atlas')
material.use_nodes = True
nodes = material.node_tree.nodes
sample = nodes.new('ShaderNodeTexImage')
sample.image = texture
material.node_tree.links.new(sample.outputs['Color'], nodes['Principled BSDF'].inputs['Base Color'])
nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
groups = {'Backdrop': [], 'LeftWall': [], 'RightWall': []}


def xyz(position):
    return (position[0], -position[2], position[1])


def finish(obj, name, color, group):
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
    uv.name = 'UVMap'
    for corner in uv.data:
        corner.uv = ((color % 4 + .5) / 4, (color // 4 + .5) / 4)
    for face in obj.data.polygons:
        face.use_smooth = True
    groups[group].append(obj)
    return obj


def sphere(name, position, scale, color, group='Backdrop'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=xyz(position))
    obj = bpy.context.object
    obj.scale = (scale[0] / 2, scale[2] / 2, scale[1] / 2)
    return finish(obj, name, color, group)


def box(name, position, scale, color, group='Backdrop', bevel=.06):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz(position))
    obj = bpy.context.object
    obj.scale = (scale[0], scale[2], scale[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Inflatable rounded edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, color, group)


def pipe(name, first, second, radius, color, group='Backdrop'):
    a, b = Vector(xyz(first)), Vector(xyz(second))
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=(b - a).length,
                                      location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, color, group)


def triangle(name, points, color):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([xyz(point) for point in points], [], [(0, 2, 1)])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new('Flag thickness', 'SOLIDIFY')
    modifier.thickness = .015
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, color, 'Backdrop')


# Clear fight plane; padded ribs sit below the physical floor.
for index in range(14):
    box('Inflatable floor rib', (-6.5 + index, -.22, 0), (.98, .44, 4.4), 0)
box('Front yellow bumper', (0, -.25, -2.3), (14.6, .48, .35), 2)
box('Ground', (0, -.65, 4), (38, .6, 28), 11)
box('Dusk sky', (0, 9, 14), (42, 23, .25), 8, bevel=0)
for x in (-10, -7, 0, 7, 11):
    sphere('Distant rounded hedge', (x, 1.5, 10), (5, 4, 1.5), 11)
for sign, group in ((-1, 'LeftWall'), (1, 'RightWall')):
    # The whole rail and its two posts move as one kinematic wall.
    for index in range(3):
        box('Inflatable side rail', (sign * 7.5, .3 + index * .48, 0), (1, .5, 4.7),
            (1, 2, 3)[index], group, .18)
    for depth in (-2, 2):
        for band in range(5):
            pipe('Striped castle tower', (sign * 7.5, band * .64, depth),
                 (sign * 7.5, (band + 1) * .64, depth), .52, (1, 2, 3, 2, 1)[band], group)
        sphere('Tower cap', (sign * 7.5, 3.5, depth), (1.2, 1, 1.2), 4 if sign < 0 else 5, group)
        pipe('Flagpole', (sign * 7.5, 3.9, depth), (sign * 7.5, 4.7, depth), .045, 7, group)
# Back castle at toy scale, framing the battle without obscuring the center.
for x in (-5.8, 5.8):
    for band in range(5):
        pipe('Back striped tower', (x, band * .75, 4), (x, (band + 1) * .75, 4), .64,
             (3, 2, 1, 3, 2)[band])
    sphere('Back tower crown', (x, 4.1, 4), (1.55, 1.5, 1.5), 5 if x < 0 else 4)
for index in range(12):
    x1 = -5.8 + index * 11.6 / 12
    x2 = -5.8 + (index + 1) * 11.6 / 12
    y1 = 3.2 + 1.4 * math.sin(math.pi * index / 12)
    y2 = 3.2 + 1.4 * math.sin(math.pi * (index + 1) / 12)
    pipe('Rainbow back arch', (x1, y1, 4.2), (x2, y2, 4.2), .3, (1, 2, 4, 3)[index % 4])
# Low ticket booth on the right and carnival wheel behind it.
box('Ticket booth', (4, .9, 7), (1.8, 1.8, 1), 1)
box('Ticket booth window', (4, 1.15, 6.48), (1.2, .8, .04), 6)
box('Ticket counter', (4, .7, 6.35), (1.4, .12, .45), 2)
for i in range(6):
    box('Striped booth awning', (3.25 + i * .3, 1.95, 6.7), (.29, .18, 1.7), 2 if i % 2 else 3)
center = Vector((3.3, 4.5, 9.5))
for i in range(24):
    angle = i * math.tau / 24
    nxt = (i + 1) * math.tau / 24
    first = center + Vector((math.cos(angle) * 3, math.sin(angle) * 3, 0))
    second = center + Vector((math.cos(nxt) * 3, math.sin(nxt) * 3, 0))
    pipe('Wheel rim', first, second, .07, 9)
    if i % 3 == 0:
        pipe('Wheel spoke', center, first, .045, 9)
        box('Wheel gondola', first - Vector((0, .2, 0)), (.6, .45, .45), (1, 2, 3, 4)[i % 4])
pipe('Wheel left support', (.9, 0, 9.7), center, .1, 9)
pipe('Wheel right support', (5.7, 0, 9.7), center, .1, 9)
sphere('Wheel hub', center, (.65, .65, .3), 2)
# String bunting, party bulbs and tied balloon clusters.
for i in range(20):
    x = -10 + i
    height = 6.2 - .8 * math.cos(x / 10 * math.pi / 2)
    pipe('Bunting rope', (x, height, 3), (x + 1, height + .03, 3), .022, 6)
    triangle('Bunting flag', [(x + .05, height, 3), (x + .8, height, 3),
                             (x + .42, height - .65, 3)], 1 + i % 5)
    sphere('Party bulb', (x + .85, height - .12, 3), (.12, .17, .12), 7)
for sign in (-1, 1):
    for i in range(4):
        position = (sign * (8.5 + i * .3), 2.7 + i * .55, 5 + i % 2 * .3)
        sphere('Balloon', position, (.65, .85, .65), 1 + i % 5)
        pipe('Balloon string', (sign * 8.5, .7, 5), (position[0], position[1] - .4, position[2]), .012, 7)
# Visible trampoline sections at the documented pad locations.
for x in (-3, 3):
    box('Trampoline yellow rim', (x, -.025, 0), (2, .07, 1.5), 2)
    box('Trampoline blue fabric', (x, .015, 0), (1.65, .025, 1.22), 13)
    for z in (-.45, 0, .45):
        box('Pad stitch', (x, .031, z), (1.4, .01, .025), 15, bevel=0)

meshes = []
for group, parts in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = 'SM_' + group
    scene.cursor.location = xyz(((-7.5 if group == 'LeftWall' else 7.5) if group != 'Backdrop' else 0, 0, 0))
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    meshes.append(obj)
triangles = sum(sum(len(p.vertices) - 2 for p in obj.data.polygons) for obj in meshes)
assert triangles < 65000, triangles
report = {'triangles': triangles, 'meshes': [o.name for o in meshes], 'materials': 1,
          'atlas': [1024, 1024], 'half_width': 7, 'pad_centers': [-3, 3], 'validation_errors': []}
source.with_suffix('.validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:
    obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(export.resolve()), use_selection=True, object_types={'MESH'},
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_anim=False, use_mesh_modifiers=True)
# Render a 16:9 thumbnail with no borrowed imagery.
world = scene.world or bpy.data.worlds.new('Fair dusk')
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (.25, .3, .45, 1)
world.node_tree.nodes['Background'].inputs[1].default_value = .5
bpy.ops.object.light_add(type='AREA', location=xyz((-3, 10, -6)))
bpy.context.object.data.energy = 2300
bpy.context.object.data.shape = 'DISK'
bpy.context.object.data.size = 12
bpy.context.object.rotation_euler = (Vector(xyz((0, 0, 2))) - bpy.context.object.location).to_track_quat('-Z', 'Y').to_euler()
bpy.ops.object.camera_add(location=xyz((0, 5, -19)))
camera = bpy.context.object
camera.rotation_euler = (Vector(xyz((0, 2.3, 2))) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 20
scene.camera = camera
camera.scale.x = -1
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.filepath = str(thumbnail.resolve())
bpy.ops.render.render(write_still=True)
print('Bouncy Castle Fair: original 3-mesh environment, one atlas,', triangles, 'triangles.')
