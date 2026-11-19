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
colors = ['E6BF79','C58D50','F4D898','AB7045','D94549','3D94CA','599E59','2B5943','F4BE37','F0CFAB','C98F73','755449','E69CAC','A3BF61','9FC9CF','EEDBAE']
texture = bpy.data.images.new('GiantSandbox_Atlas', width=1024, height=1024)
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
material = bpy.data.materials.new('GiantSandbox_Atlas')
material.use_nodes = True
nodes = material.node_tree.nodes
sample = nodes.new('ShaderNodeTexImage')
sample.image = texture
material.node_tree.links.new(sample.outputs['Color'], nodes['Principled BSDF'].inputs['Base Color'])
nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
groups = {'Backdrop': [], 'Paw': [], 'Shadow': [], 'Whiskers': [], 'Spiral': []}


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
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, location=xyz(position))
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
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=(b - a).length,
                                      location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, color, group)


def triangle(name, points, color, group='Backdrop'):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([xyz(point) for point in points], [], [(0, 2, 1)])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new('Flag thickness', 'SOLIDIFY')
    modifier.thickness = .015
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, color, group)


# Warm clear fighting strip with a wood frame and background garden.
box('Packed sand', (0,-.22,0), (14,.44,4.5),0)
for sign in (-1,1):
 box('Side timber', (sign*7.25,.12,0),(.5,.55,4.9),3)
box('Back timber', (0,.15,2.5),(15,.6,.5),3)
box('Front timber', (0,-.3,-2.5),(15,.5,.45),1)
box('Backyard grass',(0,-.65,7),(36,.6,28),7)
box('Garden sky',(0,9,15),(42,24,.2),14,bevel=0)
for x in range(-9,10,2):
 sphere('Garden bush',(x,2.5+(x%3)*.35,10),(4,5,2),7)
 sphere('Bright hedge',(x+1,2.8,11),(3.8,4,2),6)
for x in range(-9,10):
 box('Fence slat',(x,1.45,5),(.82,2.8,.18),6)
 sphere('Fence rounded cap',(x,2.85,5),(.82,.8,.18),6)
for height in (.8,2):
 box('Fence rail',(0,height,5.2),(20,.25,.18),13)
# Castle remains behind the fight plane: no hidden shelter or obstructing collider.
box('Sandcastle base',(1, .45,3.3),(2.5,.9,1.2),1,bevel=.15)
for x in (-.1,2.1):
 pipe('Sand tower',(x,0,3.3),(x,1.55,3.3),.52,0)
 sphere('Packed turret top',(x,1.55,3.3),(1.04,.25,1.04),2)
box('Castle keep',(1,1.45,3.5),(1.55,2.3,.9),0,bevel=.1)
for x in (.4,1,1.6):
 box('Castle battlement',(x,2.7,3.5),(.35,.35,.9),0)
sphere('Arched castle doorway',(1,.45,2.68),(.48,.9,.07),3)
# Open toy bucket assembled as a tapered ring; rim and handle read at phone size.
bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=.65, radius2=.86, depth=1.57,
                               end_fill_type='NOTHING', location=xyz((-4.4,.965,3)))
bucket=bpy.context.object
wall=bucket.modifiers.new('Plastic thickness','SOLIDIFY');wall.thickness=.09
bpy.ops.object.modifier_apply(modifier=wall.name)
finish(bucket,'Open plastic bucket',4,'Backdrop')
pipe('Bucket base',(-4.4,.18,3),(-4.4,.24,3),.65,4)
for i in range(24):
 a=i*math.tau/24;b=(i+1)*math.tau/24
 pipe('Bucket rolled rim',(-4.4+.86*math.cos(a),1.75,3+.86*math.sin(a)),
      (-4.4+.86*math.cos(b),1.75,3+.86*math.sin(b)),.08,4)
 if i<12:
  pipe('Bucket curved handle',(-4.4+math.cos(a),1.5+.8*math.sin(a),2.85),
       (-4.4+math.cos(b),1.5+.8*math.sin(b),2.85),.06,6)
pipe('Shovel shaft',(-3.35,.35,2.9),(-3.9,2.3,2.9),.09,5)
box('Shovel blade',(-3.25,.3,2.9),(.7,.65,.13),8,bevel=.15)
for x in (-4.15,-3.65):
 pipe('Shovel handle side',(x,2.3,2.9),(x,2.75,2.9),.08,5)
pipe('Shovel handle grip',(-4.15,2.75,2.9),(-3.65,2.75,2.9),.08,5)
# Colored blocks and rake at the opposite edge, all outside the fight strip.
for i,c in enumerate((4,8,5)):
 box('Toy block',(4.8+i*.55,.35,3),(.55,.65,.55),c)
pipe('Toy rake handle',(5.5,.25,3.4),(6.2,2.25,3.4),.07,5)
box('Rake head',(5.4,.22,3.4),(.9,.18,.12),8)
for i in range(5):
 box('Rake tooth',(5.05+i*.18,.08,3.4),(.08,.3,.12),8)
for i in range(28):
 x=-6.5+(i*1.17)%13;z=.85+(i%4)*.32
 sphere('Small sand clump',(x,.025,z),(.22,.06,.14),1 if i%2 else 2)
for sign in (-1,1):
 for i in range(5):
  x=sign*(7.9+i*.45)
  pipe('Garden grass blade',(x,0,4),(x+.15, .8+i%3*.2,4),.045,6)
# A soft cartoon paw and forearm; the pink pads face the fight camera.
sphere('Cat forearm',(0,.55,1.05),(1.5,1,2.6),9,'Paw')
sphere('Cat palm',(0,.45,0),(1.75,.9,1.55),9,'Paw')
sphere('Palm pad',(0,.37,-.62),(.95,.5,.25),12,'Paw')
for i in range(4):
 x=-.72+i*.48
 sphere('Round cat toe',(x,.28,-.65),(.62,.55,.8),9,'Paw')
 sphere('Pink toe pad',(x,.25,-.98),(.36,.3,.14),12,'Paw')
sphere('Paw shadow',(0,0,0),(2.05,.018,1.5),11,'Shadow')
for i in range(4):
 sphere('Toe shadow',(-.72+i*.48,0,-.65),(.6,.018,.8),11,'Shadow')
for i in range(3):
 pipe('Cat whisker',(0,.05*i,0),(.9,.28*(i-1),0),.022,15,'Whiskers')
# Reusable low-poly swirl with an outlined sand-colored ring and spiral ridge.
for i in range(64):
 a=i*math.tau/64;b=(i+1)*math.tau/64
 pipe('Swirl border',(1.35*math.cos(a),0,1.35*math.sin(a)),
      (1.35*math.cos(b),0,1.35*math.sin(b)),.035,1,'Spiral')
for i in range(72):
 a=i*math.tau*2.5/72;b=(i+1)*math.tau*2.5/72
 r=.12+1.12*i/72;rr=.12+1.12*(i+1)/72
 pipe('Sand spiral ridge',(r*math.cos(a),.01,r*math.sin(a)),
      (rr*math.cos(b),.01,rr*math.sin(b)),.035,3,'Spiral')

meshes = []
for group, parts in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = 'SM_' + group
    scene.cursor.location = (0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    meshes.append(obj)
triangles = sum(sum(len(p.vertices) - 2 for p in obj.data.polygons) for obj in meshes)
assert triangles < 65000, triangles
report = {'triangles': triangles, 'meshes': [o.name for o in meshes], 'materials': 1,
          'atlas': [1024, 1024], 'half_width': 7, 'sand_centers': [-3, 0, 3], 'validation_errors': []}
source.with_suffix('.validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:
    obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(export.resolve()), use_selection=True, object_types={'MESH'},
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_anim=False, use_mesh_modifiers=True)
# Hide hazard props only for the preview; saved source/export retains all five meshes.
for obj in meshes:
    obj.hide_render = obj.name != 'SM_Backdrop'
# Render a 16:9 thumbnail with no borrowed imagery.
world = scene.world or bpy.data.worlds.new('Garden daylight')
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (.55, .65, .7, 1)
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
print('Giant Sandbox: original 5-mesh environment, one atlas,', triangles, 'triangles.')
