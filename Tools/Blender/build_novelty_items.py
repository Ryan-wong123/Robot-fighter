"""Author the six approved additional toy pickups with a shared palette atlas."""
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
export.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
colors = ['E6BF79','C58D50','F4D898','AB7045','D94549','3D94CA','599E59','2B5943','F4BE37','F0CFAB','C98F73','755449','E69CAC','A3BF61','9FC9CF','EEDBAE']
texture = bpy.data.images.new('NoveltyItems_Atlas', width=1024, height=1024)
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
material = bpy.data.materials.new('NoveltyItems_Atlas')
material.use_nodes = True
nodes = material.node_tree.nodes
sample = nodes.new('ShaderNodeTexImage')
sample.image = texture
material.node_tree.links.new(sample.outputs['Color'], nodes['Principled BSDF'].inputs['Base Color'])
nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
groups = {name: [] for name in ('CreamPie_Body','Boomerang_Body','PogoStick_Body','BubbleWand_Body','BubbleWand_Action','WindUpCar_Body','BoxingJack_Body','BoxingJack_Action','BoxingJack_Warning')}


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


def sphere(name, position, scale, color, group='CreamPie_Body'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, location=xyz(position))
    obj = bpy.context.object
    obj.scale = (scale[0] / 2, scale[2] / 2, scale[1] / 2)
    return finish(obj, name, color, group)


def box(name, position, scale, color, group='CreamPie_Body', bevel=.06):
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


def pipe(name, first, second, radius, color, group='CreamPie_Body'):
    a, b = Vector(xyz(first)), Vector(xyz(second))
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=(b - a).length,
                                      location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, color, group)


def triangle(name, points, color, group='CreamPie_Body'):
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



def ring(name,center,radius,color,group,plane='xy',count=16):
 for i in range(count):
  a=i*math.tau/count;b=(i+1)*math.tau/count
  def point(t):
   x,y,z=center
   return (x+radius*math.cos(t),y+radius*math.sin(t),z) if plane=='xy' else (x+radius*math.cos(t),y,z+radius*math.sin(t))
  pipe(name,point(a),point(b),.025,color,group)

# Cream-filled pie in a fluted silver plate.
sphere('Pie plate',(0,-.08,0),(.6,.12,.55),14)
sphere('Golden crust',(0,-.02,0),(.55,.16,.5),1)
sphere('Whipped cream',(0,.06,0),(.48,.17,.43),15)
for i in range(7):
 a=i*math.tau/7
 sphere('Cream rosette',(.16*math.cos(a),.13,.14*math.sin(a)),(.13,.13,.13),15)
sphere('Cherry',(0,.22,0),(.12,.12,.12),4)
# A broad, curved, two-armed toy boomerang.
g='Boomerang_Body'
points=[(-.38,-.18,0),(-.22,.10,0),(0,.23,0),(.22,.10,0),(.38,-.18,0)]
for a,b in zip(points,points[1:]):pipe('Boomerang arm',a,b,.075,8,g)
for p in points:sphere('Smooth boomerang bend',p,(.15,.15,.15),8,g)
for x in (-.23,.23):box('Blue boomerang stripe',(x,.07,-.071),(.1,.09,.025),5,g,bevel=.01)
# Spring pogo with handle and rubber foot.
g='PogoStick_Body'
pipe('Pogo shaft',(0,-.28,0),(0,.42,0),.045,8,g)
pipe('Pogo handle',(-.22,.42,0),(.22,.42,0),.045,4,g)
box('Pogo foot',(0,-.45,0),(.25,.09,.23),7,g,bevel=.035)
for x in (-.13,.13):box('Foot rest',(x,-.1,0),(.18,.045,.15),5,g,bevel=.015)
for i in range(32):
 a=i*math.tau*4/32;b=(i+1)*math.tau*4/32
 pipe('Pogo spring',(.095*math.cos(a),-.31+i*.22/32,.095*math.sin(a)),
      (.095*math.cos(b),-.31+(i+1)*.22/32,.095*math.sin(b)),.018,14,g)
# Bubble wand, with a separate reusable bubble mesh.
g='BubbleWand_Body'
pipe('Wand handle',(0,-.30,0),(0,.08,0),.045,5,g)
ring('Bubble hoop',(0,.25,0),.17,12,g)
sphere('Wand cap',(0,-.3,0),(.11,.11,.11),8,g)
sphere('Bubble',(0,0,0),(1,1,1),14,'BubbleWand_Action')
# Four-wheel wind-up car with a visible winding key.
g='WindUpCar_Body'
box('Car body',(0,0,0),(.62,.18,.35),4,g,bevel=.06)
box('Car cabin',(-.07,.14,0),(.32,.19,.29),5,g,bevel=.055)
box('Car stripe',(.19,.105,0),(.13,.025,.29),8,g,bevel=.01)
for x in (-.2,.2):
 for z in (-.19,.19):sphere('Black rubber wheel',(x,-.06,z),(.18,.18,.09),7,g)
pipe('Winding spindle',(-.36,.08,0),(-.45,.08,0),.025,14,g)
ring('Winding key',(-.47,.13,0),.07,8,g,count=12)
# Boxing jack: star-decorated base, spring/glove action, warning ring.
g='BoxingJack_Body'
box('Jack box',(0,-.12,0),(.48,.3,.42),12,g,bevel=.04)
box('Jack lid',(0,.045,0),(.52,.045,.46),8,g,bevel=.015)
for side in (-1,1):
 sphere('Toy rivet',(side*.17,-.12,-.22),(.05,.05,.025),8,g)
g='BoxingJack_Action'
for i in range(32):
 a=i*math.tau*4/32;b=(i+1)*math.tau*4/32
 pipe('Boxing spring',(.08*math.cos(a),.07+i*.24/32,.08*math.sin(a)),
      (.08*math.cos(b),.07+(i+1)*.24/32,.08*math.sin(b)),.018,14,g)
sphere('Red boxing glove',(0,.39,0),(.35,.3,.3),4,g)
sphere('Glove thumb',(.16,.33,-.02),(.13,.17,.16),4,g)
ring('Telegraph ring',(0,-.25,0),.65,8,'BoxingJack_Warning',plane='xz')

meshes=[]
for group,parts in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for obj in parts:obj.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
 obj=bpy.context.object;obj.name='SM_'+group
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);meshes.append(obj)
report={'materials':1,'atlas':[1024,1024],'items':{},'validation_errors':[]}
items=['CreamPie','Boomerang','PogoStick','BubbleWand','WindUpCar','BoxingJack']
for name in items:
 selected=[o for o in meshes if o.name.startswith('SM_'+name+'_')]
 triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in selected)
 assert triangles<=4000,(name,triangles)
 report['items'][name]={'triangles':triangles,'meshes':[o.name for o in selected]}
 bpy.ops.object.select_all(action='DESELECT')
 for obj in selected:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str((export/(name+'.fbx')).resolve()),use_selection=True,
  object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False)
source.with_suffix('.validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
# A source-only product lineup; hazard/bubble props remain in exports but are hidden here.
for i,name in enumerate(items):
 for obj in meshes:
  if obj.name.startswith('SM_'+name+'_'):
   obj.location.x=(i-2.5)*1.1
   obj.hide_render=obj.name.endswith('_Warning') or obj.name=='SM_BubbleWand_Action'
world=scene.world or bpy.data.worlds.new('Toy studio');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.3,.38,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.6
bpy.ops.object.light_add(type='AREA',location=xyz((-2,5,-4)))
bpy.context.object.data.energy=700;bpy.context.object.data.size=7
bpy.context.object.rotation_euler=(Vector((0,0,0))-bpy.context.object.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=xyz((0,2.8,-8)))
camera=bpy.context.object;camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=7;camera.scale.x=-1;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1400;scene.render.resolution_y=400;scene.render.resolution_percentage=100
scene.render.filepath=str(thumbnail.resolve());bpy.ops.render.render(write_still=True)
print(json.dumps(report))
