"""Original Mr. Jack source/atlas/rig/actions, based on the owner's jester sheet."""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate
source, export, atlas = map(Path, sys.argv[sys.argv.index('--')+1:])
if any(p.exists() for p in (source, export, atlas)):
    raise FileExistsError('Use fresh paths; existing sources are never overwritten.')
for path in (source,export,atlas): path.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=60
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='ARM_MrJack'
bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(name,head,tail,parent=None,deform=True):
    b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail;b.use_deform=deform
    if parent:b.parent=rig.data.edit_bones[parent]
bone('Root',(0,0,0),(0,0,.15),deform=True)
for i in range(4):bone('Spring'+str(i+1),(0,0,.25+i*.2),(0,0,.45+i*.2),'Root' if i==0 else 'Spring'+str(i))
bone('Pelvis',(0,0,1.05),(0,0,1.18),'Spring4')
bone('Spine',(0,0,1.18),(0,0,1.42),'Pelvis');bone('Chest',(0,0,1.42),(0,0,1.62),'Spine')
bone('Head',(0,0,1.62),(0,0,1.99),'Chest');bone('Hat',(0,0,1.99),(0,0,2.28),'Head')
bone('Lid',(0,.35,.52),(0,.35,.9),'Root');bone('Crank',(.52,0,.27),(.73,0,.27),'Root')
for side,suffix in ((1,'.L'),(-1,'.R')):
    bone('Upper_Arm'+suffix,(side*.24,0,1.54),(side*.48,0,1.34),'Chest')
    bone('Lower_Arm'+suffix,(side*.48,0,1.34),(side*.70,0,1.22),'Upper_Arm'+suffix)
    bone('Hand'+suffix,(side*.70,0,1.22),(side*.82,0,1.16),'Lower_Arm'+suffix)
bpy.ops.object.mode_set(mode='OBJECT')
colors=[(.38,.10,.62,1),(.98,.72,.08,1),(.83,.18,.34,1),(1,.91,.69,1),(.13,.035,.20,1),(.98,.98,.90,1),(.65,.29,.75,1),(.61,.44,.12,1)]
image=bpy.data.images.new('MrJack_Body_Albedo',1024,1024,alpha=False)
image.pixels.foreach_set([v for y in range(1024) for x in range(1024) for v in colors[x//128]])
image.filepath_raw=str(atlas.resolve());image.file_format='PNG';image.save();image.pack()
mat=bpy.data.materials.new('MrJack_Palette');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
parts=[]
def skin(obj,color,binding):
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if not obj.data.uv_layers:obj.data.uv_layers.new()
    for poly in obj.data.polygons:
        poly.use_smooth=True
        for loop in poly.loop_indices:
            uv=obj.data.uv_layers.active.data[loop].uv.copy()
            obj.data.uv_layers.active.data[loop].uv=((color+.15+uv.x*.7)/8,uv.y)
    obj.data.materials.clear();obj.data.materials.append(mat)
    names=[binding] if isinstance(binding,str) else binding
    groups={name:obj.vertex_groups.new(name=name) for name in names}
    for vertex in obj.data.vertices:
        candidates=sorted(((vertex.co-rig.data.bones[name].head_local).length_squared,name) for name in names)[:2]
        weights=[1/(d+.002)**2 for d,n in candidates];total=sum(weights)
        for (_,name),weight in zip(candidates,weights):groups[name].add([vertex.index],weight/total,'REPLACE')
    obj.parent=rig;modifier=obj.modifiers.new('Skin','ARMATURE');modifier.object=rig
    parts.append(obj);obj.select_set(False)
def oval(name,pos,scale,color,binding,segments=12,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=pos)
    o=bpy.context.object;o.name=name;o.scale=scale;skin(o,color,binding)
def box(name,pos,scale,color,binding):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.scale=scale
    bevel=o.modifiers.new('Soft toy corners','BEVEL');bevel.width=.045;bevel.segments=2
    bpy.ops.object.modifier_apply(modifier=bevel.name);skin(o,color,binding)
def line(name,points,width,color,binding):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=width;curve.bevel_resolution=1
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for point,co in zip(spline.points,points):point.co=(*co,1)
    o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.convert(target='MESH');skin(o,color,binding)
# Hollow-looking toy chest, inset face, bevels, corner rivets and working side crank.
box('Purple box',(0,0,.27),(1.02,.77,.54),0,'Root')
box('Dark box opening',(0,0,.548),(.80,.56,.016),4,'Root')
box('Inset front',(0,-.395,.27),(.82,.035,.37),6,'Root')
for x in (-.46,.46):
 for z in (.07,.47):oval('Gold rivet',(x,-.402,z),(.035,.025,.035),1,'Root',8,6)
line('Keyhole',[(0,-.423,.23),(0,-.423,.34)],.015,4,'Root')
box('Hinged lid',(0,.33,.82),(1.03,.12,.6),0,'Lid')
line('Crank',[(.50,0,.27),(.65,0,.27),(.65,0,.43),(.80,0,.43)],.028,7,'Crank')
oval('Crank grip',(.82,0,.43),(.1,.06,.06),6,'Crank')
points=[]
for i in range(145):
 t=i/144;angle=t*math.tau*4.5;points.append((.21*math.cos(angle),.21*math.sin(angle),.52+t*.55))
line('Golden helical spring',points,.035,1,['Spring1','Spring2','Spring3','Spring4'])
oval('Purple jester coat',(0,0,1.34),(.29,.22,.32),0,['Pelvis','Spine','Chest'])
oval('Yellow chest panel',(.10,-.177,1.34),(.16,.07,.26),1,['Spine','Chest'])
for z in (1.18,1.32,1.46):oval('Coat button',(0,-.238,z),(.03,.02,.03),2,'Spine',8,6)
for side,suffix in ((1,'.L'),(-1,'.R')):
 oval('Puffed sleeve'+suffix,(side*.37,0,1.44),(.21,.135,.14),2 if side>0 else 0,'Upper_Arm'+suffix)
 oval('Forearm sleeve'+suffix,(side*.59,0,1.28),(.17,.10,.10),0 if side>0 else 2,'Lower_Arm'+suffix)
 oval('Gold cuff'+suffix,(side*.70,0,1.22),(.075,.12,.10),1,'Hand'+suffix)
 oval('Cream glove'+suffix,(side*.79,-.015,1.17),(.13,.09,.09),3,'Hand'+suffix)
 for finger in range(3):
  oval('Glove finger'+suffix,(side*(.85+finger*.012),-.065+finger*.045,1.12),(.065,.025,.035),3,'Hand'+suffix,8,6)
 # Pointed collar with bell tips.
 line('Collar'+suffix,[(0,-.02,1.63),(side*.33,-.04,1.54),(side*.19,-.11,1.48)],.035,1,'Chest')
 oval('Collar bell'+suffix,(side*.19,-.11,1.48),(.04,.04,.04),1,'Chest',8,6)
# Two tapered drooping jester-hat horns with gold bell tips.
for side,color in ((1,1),(-1,0)):
    vertices=[];faces=[]
    path=[(side*.12,0,2.02),(side*.20,0,2.18),(side*.31,0,2.23),(side*.41,0,2.16),(side*.47,0,2.03)]
    for ring,(x,y,z) in enumerate(path):
        radius=.17*(1-ring/5)+.012
        for sector in range(12):
            a=sector*math.tau/12;vertices.append((x+math.cos(a)*radius,y+math.sin(a)*radius,z))
    for ring in range(4):
        for sector in range(12):
            a=ring*12+sector;b=ring*12+(sector+1)%12;faces.append((a,b,b+12,a+12))
    data=bpy.data.meshes.new('Jester horn');data.from_pydata(vertices,[],faces)
    obj=bpy.data.objects.new('Hat horn',data);scene.collection.objects.link(obj);skin(obj,color,'Hat')
    oval('Hat bell',(side*.47,-.01,1.98),(.075,.075,.075),1,'Hat',10,6)
oval('Purple hood',(0,0,1.85),(.30,.245,.32),0,'Head')
oval('Painted cream face',(0,-.188,1.82),(.245,.10,.245),3,'Head')
# Readable grin, teeth, cheek dots, long purple eyes and a red nose.
oval('Dark grin',(0,-.273,1.73),(.18,.031,.09),4,'Head')
oval('White teeth',(0,-.299,1.75),(.16,.018,.047),5,'Head')
for side in (-1,1):
 oval('Eye',(side*.095,-.276,1.88),(.027,.025,.058),4,'Head')
 oval('Cheek',(side*.18,-.267,1.79),(.034,.017,.031),2,'Head')
 line('Arched eyebrow',[(side*.05,-.27,1.975),(side*.095,-.28,1.99),(side*.14,-.265,1.965)],.013,4,'Head')
oval('Red nose',(0,-.31,1.82),(.055,.045,.05),2,'Head')
# Combine the weighted parts into one skin and atlas.
bpy.ops.object.select_all(action='DESELECT')
for part in parts:part.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name='SK_MrJack'
while len(mesh.modifiers)>1:mesh.modifiers.remove(mesh.modifiers[-1])
# All animation is in-place. Four spring joints shorten phase one and extend for phase two.
names=['Idle','Walk','JumpRise','JumpFall','JumpLand','Block','Punch1','Punch2','Punch3','Punch4','Kick1','Kick2','Special','HitLight','HitHeavy','Knockdown','GetUp','Win','Taunt','GuardBreak','Throw','KO']
def action(name,phase,kind):
 duration={'Idle':61,'Walk':37,'Block':49,'Win':85,'Taunt':73,'Special':101,'GetUp':51}.get(kind,49)
 clip=bpy.data.actions.new('MrJack_'+name);clip.use_fake_user=True;rig.animation_data_create();rig.animation_data.action=clip
 if kind.startswith(('Punch','Kick')):duration=(49 if not phase else 39)+int(kind[-1])*4
 impact=(19+int(kind[-1])*3) if kind.startswith('Punch') else (25+int(kind[-1])*3) if kind.startswith('Kick') else 76 if kind=='Special' else duration//2
 if phase:impact=max(12,impact-7) if kind!='Special' else 76
 frames=sorted(set([1, max(2,impact-8),impact,min(duration-1,impact+5),duration]))
 if kind=='Special':frames=sorted(set(frames+[impact-1]))
 if kind in ('Idle','Walk','Block'):frames=[1,duration//4,duration//2,duration*3//4,duration]
 for frame in frames:
  t=(frame-1)/(duration-1);wave=math.sin(t*math.tau)
  for b in rig.pose.bones:
   b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
  for i in range(4):
   b=rig.pose.bones['Spring'+str(i+1)];b.rotation_euler.x=wave*(.07 if phase else .025);b.rotation_euler.z=wave*.035
   if i>0:b.location.y=-.09 if not phase else 0
  rig.pose.bones['Pelvis'].location.y=-.09 if not phase else 0
  rig.pose.bones['Lid'].rotation_euler.x=-.30-wave*.04
  rig.pose.bones['Upper_Arm.L'].rotation_euler.z=.18+wave*.08
  rig.pose.bones['Upper_Arm.R'].rotation_euler.z=-.18-wave*.08
  rig.pose.bones['Head'].rotation_euler.y=wave*.12
  if kind=='Walk':
   rig.pose.bones['Spine'].rotation_euler.x=wave*.16;rig.pose.bones['Crank'].rotation_euler.x=wave*.8
  elif kind=='Block':
   rig.pose.bones['Upper_Arm.L'].rotation_euler.x=1.2;rig.pose.bones['Upper_Arm.R'].rotation_euler.x=1.2
   rig.pose.bones['Lower_Arm.L'].rotation_euler.z=.8;rig.pose.bones['Lower_Arm.R'].rotation_euler.z=-.8
  elif kind in ('JumpRise','JumpFall','JumpLand','HitLight','HitHeavy','Knockdown','KO','GuardBreak','GetUp'):
   strength={'HitLight':.35,'HitHeavy':.65,'Knockdown':1.2,'KO':1.5,'GuardBreak':1.1,'GetUp':1-t,'JumpRise':-.3,'JumpFall':.3,'JumpLand':.5}.get(kind,.4)
   pulse=math.sin(math.pi*t) if kind not in ('GetUp','KO','Knockdown') else 1
   rig.pose.bones['Spine'].rotation_euler.x=strength*pulse;rig.pose.bones['Head'].rotation_euler.z=-strength*.5*pulse
  elif kind.startswith('Punch') or kind.startswith('Kick'):
   active=frame>=impact and frame<=impact+5
   wind=frame>1 and frame<impact
   number=int(kind[-1]);amount=1 if active else -.45 if wind else 0
   rig.pose.bones['Spine'].rotation_euler.x=amount*(.48 if number==2 else .2)
   rig.pose.bones['Chest'].rotation_euler.y=amount*(.55 if number==3 else -.15)
   if kind.startswith('Punch'):
    for side in ('L','R'):
     upper=rig.pose.bones['Upper_Arm.'+side];lower=rig.pose.bones['Lower_Arm.'+side]
     upper.rotation_euler.x=amount*(1.5 if number in (1,3) else .7)
     upper.rotation_euler.z=amount*(.55 if side=='L' else -.55)
     lower.rotation_euler.x=amount*-.6
    if number==2:rig.pose.bones['Head'].rotation_euler.x=amount*.8
    if number==4:rig.pose.bones['Lid'].rotation_euler.x=-1.5 if wind else .65 if active else -.3
   else:
    rig.pose.bones['Pelvis'].rotation_euler.y=amount*(2.1 if number==2 else .7)
    rig.pose.bones['Spine'].rotation_euler.x=amount*.75
    rig.pose.bones['Crank'].rotation_euler.x=amount*math.tau
    for i in range(4):rig.pose.bones['Spring'+str(i+1)].rotation_euler.x=amount*.10
  elif kind=='Special':
   hidden=frame<impact
   rig.pose.bones['Spring1'].scale=(.02,.02,.02) if hidden else (1,1,1)
   for i in range(4):rig.pose.bones['Spring'+str(i+1)].location.y=-.16 if hidden else .035 if frame<=impact+5 else (-.09 if not phase and i>0 else 0)
   rig.pose.bones['Pelvis'].location.y=-.18 if hidden else 0
   rig.pose.bones['Head'].rotation_euler.x=.5 if hidden else -.25
   rig.pose.bones['Lid'].rotation_euler.x=.8 if hidden else -1.4
   rig.pose.bones['Upper_Arm.L'].rotation_euler.z=1.2 if not hidden else -.5
   rig.pose.bones['Upper_Arm.R'].rotation_euler.z=-1.2 if not hidden else .5
  elif kind=='Throw':
   p=math.exp(-((frame-impact)/7)**2)
   rig.pose.bones['Upper_Arm.L'].rotation_euler.x=1.6*p-.5*(1-p)
   rig.pose.bones['Lower_Arm.L'].rotation_euler.z=.5*(1-p)
   rig.pose.bones['Chest'].rotation_euler.y=-.4*p
  elif kind=='PhaseChange':
   for i in range(1,4):rig.pose.bones['Spring'+str(i+1)].location.y=-.09*(1-t)+.05*math.sin(math.pi*t)
   rig.pose.bones['Pelvis'].location.y=-.09*(1-t)
   rig.pose.bones['Lid'].rotation_euler.x=-.3-math.sin(math.pi*t)
  elif kind in ('Win','Taunt'):
   rig.pose.bones['Upper_Arm.L'].rotation_euler.z=.8+wave*.6;rig.pose.bones['Upper_Arm.R'].rotation_euler.z=-.8-wave*.6
   rig.pose.bones['Crank'].rotation_euler.x=t*math.tau*3;rig.pose.bones['Head'].rotation_euler.x=wave*.25
  if name=='PhaseChange':
   for i in range(1,4):rig.pose.bones['Spring'+str(i+1)].location.y=-.09*(1-t)
   rig.pose.bones['Pelvis'].location.y=-.09*(1-t);rig.pose.bones['Lid'].rotation_euler.x=-1.2*math.sin(math.pi*t)
  for b in rig.pose.bones:
   for prop in ('rotation_euler','location','scale'):b.keyframe_insert(prop,frame=frame,group=b.name)
 clip.use_frame_range=True;clip.frame_start=1;clip.frame_end=duration
 if kind.startswith(('Punch','Kick')) or kind=='Special':
  clip.pose_markers.new('HitboxOn').frame=impact;clip.pose_markers.new('HitboxOff').frame=impact+5
 rig.animation_data.action=None
for phase in (0,1):
 for kind in names:action(('Out' if phase else '')+kind,phase,kind)
action('PhaseChange',1,'PhaseChange')
for b in rig.pose.bones:b.location=(0,0,0);b.rotation_euler=(0,0,0)
scene.frame_set(1)
problems=validate(scene)
if problems:raise RuntimeError('\n'.join(problems))
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()),axis_forward='-Z',axis_up='Y',add_leaf_bones=False,object_types={'ARMATURE','MESH'},apply_scale_options='FBX_SCALE_ALL',bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
mesh.data.calc_loop_triangles()
report=dict(triangles=len(mesh.data.loop_triangles),bones=len(rig.data.bones),actions=sorted(a.name for a in bpy.data.actions),validation_errors=problems,reference='docs/images/characters/concept/mr_jack_turnaround.png')
source.with_suffix('.validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
