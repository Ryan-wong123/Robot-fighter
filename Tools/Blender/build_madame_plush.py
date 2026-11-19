"""Build Madame Plush's original stitched bear, stuffing morphs and in-place actions."""
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from validate_export import validate
source,export,atlas=map(Path,sys.argv[sys.argv.index('--')+1:])
if any(path.exists() for path in (source,export,atlas)):raise FileExistsError('Choose fresh output paths.')
for path in (source,export,atlas):path.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=60
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='ARM_MadamePlush'
bpy.ops.object.mode_set(mode='EDIT');bones=rig.data.edit_bones;bones.remove(bones[0])
def bone(name,a,b,parent=None,deform=True):
 item=bones.new(name);item.head=a;item.tail=b;item.use_deform=deform
 if parent:item.parent=bones[parent]
bone('Root',(0,0,0),(0,0,.2),deform=False)
bone('Pelvis',(0,0,.42),(0,0,.65),'Root')
bone('Spine',(0,0,.65),(0,0,.97),'Pelvis')
bone('Chest',(0,0,.97),(0,0,1.16),'Spine')
bone('Head',(0,0,1.16),(0,0,1.68),'Chest')
for suffix,side in (('.L',1),('.R',-1)):
 bone('Upper_Arm'+suffix,(side*.31,0,1.14),(side*.47,0,.92),'Chest')
 bone('Lower_Arm'+suffix,(side*.47,0,.92),(side*.61,0,.73),'Upper_Arm'+suffix)
 bone('Hand'+suffix,(side*.61,0,.73),(side*.73,0,.62),'Lower_Arm'+suffix)
 bone('Upper_Leg'+suffix,(side*.18,0,.49),(side*.22,0,.30),'Pelvis')
 bone('Lower_Leg'+suffix,(side*.22,0,.30),(side*.24,0,.12),'Upper_Leg'+suffix)
 bone('Foot'+suffix,(side*.24,0,.12),(side*.24,-.18,.08),'Lower_Leg'+suffix)
bpy.ops.object.mode_set(mode='OBJECT')
palette=[(.43,.21,.08,1),(.72,.45,.23,1),(.85,.035,.055,1),(.10,.035,.018,1),
 (.52,.40,.69,1),(.42,.61,.22,1),(.98,.94,.84,1),(.8,.51,.44,1)]
image=bpy.data.images.new('MadamePlush_Body_Albedo',width=1024,height=1024,alpha=False)
pixels=[]
for row in range(1024):
 for col in range(1024):
  color=palette[col//128];weave=.96 if (row//3+col//3)%2==0 and col//128 in (0,1,4,5,7) else 1
  pixels.extend(tuple(value*weave for value in color[:3])+(1,))
image.pixels.foreach_set(pixels);image.filepath_raw=str(atlas.resolve());image.file_format='PNG';image.save();image.pack();image.filepath='//MadamePlush_Body_Albedo.png'
material=bpy.data.materials.new('MadamePlush_Fabric');material.use_nodes=True
shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=1
texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image
material.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
parts=[];current_stage=0
def skin(obj,color,binding):
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if not obj.data.uv_layers: obj.data.uv_layers.new(name='Atlas')
    obj.data.uv_layers.active.name='Atlas'
    for poly in obj.data.polygons:
        poly.use_smooth=True
        for loop in poly.loop_indices:
            uv=obj.data.uv_layers.active.data[loop].uv.copy()
            obj.data.uv_layers.active.data[loop].uv=((color+.15+uv.x*.7)/8,uv.y)
    obj.data.materials.clear();obj.data.materials.append(material)
    names=[binding] if isinstance(binding,str) else binding
    groups={name:obj.vertex_groups.new(name=name) for name in names}
    for vertex in obj.data.vertices:
        choices=[]
        for name in names:
            b=rig.data.bones[name];axis=b.tail_local-b.head_local
            t=max(0,min(1,(vertex.co-b.head_local).dot(axis)/axis.length_squared))
            choices.append(((vertex.co-b.head_local-axis*t).length_squared,name))
        closest=sorted(choices)[:2];weights=[1/(d+.003)**2 for d,_ in closest]
        for (_,name),weight in zip(closest,weights): groups[name].add([vertex.index],weight/sum(weights),'REPLACE')
    marker=obj.data.attributes.new(name='StuffingStage',type='INT',domain='POINT')
    for value in marker.data:value.value=current_stage
    obj.parent=rig;mod=obj.modifiers.new('Skin','ARMATURE');mod.object=rig
    parts.append(obj);obj.select_set(False)

def oval(name,position,scale,color,binding,rotation=(0,0,0),segments=16,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=position)
    obj=bpy.context.object;obj.name=name;obj.scale=scale;obj.rotation_euler=rotation;skin(obj,color,binding)

def line(name,points,radius,color,binding):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=radius;curve.bevel_resolution=0
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for point,co in zip(spline.points,points):point.co=(*co,1)
    obj=bpy.data.objects.new(name,curve);scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.convert(target='MESH');skin(bpy.context.object,color,binding)

def panel(name,cx,cz,rx,rz,body_z,body_rx,body_ry,body_rz,binding,color=1):
    # A painted vinyl panel follows the shell instead of protruding as a second head.
    vertices=[]
    for ring in range(9):
        radius=ring/8
        for sector in range(24):
            angle=sector*math.tau/24
            x=cx+rx*radius*math.cos(angle);z=cz+rz*radius*math.sin(angle)
            q=(x/body_rx)**2+((z-body_z)/body_rz)**2
            if q>.96:
                factor=math.sqrt(.96/q);x*=factor;z=body_z+(z-body_z)*factor
                q=.96
            y=-body_ry*math.sqrt(max(.001,1-q))-.012
            vertices.append((x,y,z))
    faces=[]
    for ring in range(8):
        for sector in range(24):
            a=ring*24+sector;b=ring*24+(sector+1)%24
            faces.append((a,a+24,b+24,b))
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces)
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);skin(obj,color,binding)


oval('Body',(0,0,.85),(.42,.28,.43),0,['Pelvis','Spine','Chest'],segments=24,rings=14)
oval('Hips',(0,0,.48),(.32,.23,.24),0,'Pelvis')
panel('Tan Belly',0,.86,.29,.34,.85,.42,.28,.43,['Pelvis','Spine','Chest'],color=1)
oval('Head',(0,0,1.37),(.34,.29,.34),0,'Head',segments=24,rings=14)
oval('Muzzle',(0,-.263,1.29),(.17,.065,.14),1,'Head')
oval('Nose',(0,-.331,1.34),(.055,.029,.035),3,'Head',segments=12,rings=8)
line('Mouth',[(0,-.336,1.32),(0,-.338,1.26),(-.065,-.32,1.23)],.006,3,'Head')
line('Smile',[(0,-.338,1.26),(.065,-.32,1.23)],.006,3,'Head')
for suffix,side in (('.L',1),('.R',-1)):
 oval('Ear'+suffix,(side*.29,0,1.66),(.14,.08,.14),0,'Head',segments=16,rings=10)
 oval('Inner Ear'+suffix,(side*.29,-.068,1.66),(.09,.022,.09),1,'Head',segments=12,rings=8)
 oval('Button Eye'+suffix,(side*.13,-.268,1.44),(.06,.027,.065),3,'Head',segments=16,rings=10)
 for sign in (-1,1):
  line('Button Thread',[(side*.13-.023,-.297,1.44-sign*.023),(side*.13+.023,-.297,1.44+sign*.023)],.005,1,'Head')
 oval('Upper Arm'+suffix,(side*.39,0,1.04),(.13,.13,.21),0,'Upper_Arm'+suffix,rotation=(0,-side*.4,0))
 oval('Forearm'+suffix,(side*.54,0,.82),(.13,.13,.18),0,'Lower_Arm'+suffix,rotation=(0,-side*.4,0))
 oval('Boxing Glove'+suffix,(side*.70,-.025,.65),(.18,.16,.20),2,'Hand'+suffix,segments=16,rings=12)
 oval('Glove Thumb'+suffix,(side*.58,-.10,.68),(.075,.08,.10),2,'Hand'+suffix,segments=12,rings=8)
 bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=(side*.60,0,.74),major_radius=.12,minor_radius=.025,rotation=(0,-side*.5,0))
 skin(bpy.context.object,2,'Lower_Arm'+suffix)
 oval('Thigh'+suffix,(side*.20,0,.37),(.16,.17,.20),0,'Upper_Leg'+suffix)
 oval('Shin'+suffix,(side*.24,0,.20),(.145,.155,.16),0,'Lower_Leg'+suffix)
 oval('Foot'+suffix,(side*.24,-.075,.11),(.17,.23,.11),0,'Foot'+suffix)
 for dx in (-.05,.05):
  line('Toe Stitch',[(side*.24+dx,-.23,.12),(side*.24+dx,-.26,.06)],.005,3,'Foot'+suffix)
oval('Tail',(0,.24,.5),(.11,.105,.11),0,'Pelvis',segments=16,rings=10)
bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=6,location=(0,0,.60),major_radius=.33,minor_radius=.045)
skin(bpy.context.object,2,'Pelvis')
oval('Sash Knot',(0,-.32,.61),(.09,.045,.07),2,'Pelvis',segments=12,rings=8)
for side in (-1,1):
 bpy.ops.mesh.primitive_cube_add(size=2,location=(side*.055,-.30,.46));obj=bpy.context.object;obj.scale=(.05,.018,.12);obj.rotation_euler=(0,side*.12,0);skin(obj,2,'Pelvis')
# Conforming square patches and hand-sewn edge stitches.
def patch(name,cx,cz,half,color,binding,body_z,rx,ry,rz,offset=.02):
 def surface(x,z):return -ry*math.sqrt(max(.015,1-(x/rx)**2-((z-body_z)/rz)**2))-offset
 verts=[(cx+(i-2)*half/2,surface(cx+(i-2)*half/2,cz+(j-2)*half/2),cz+(j-2)*half/2) for j in range(5) for i in range(5)]
 faces=[(j*5+i,j*5+i+1,(j+1)*5+i+1,(j+1)*5+i) for j in range(4) for i in range(4)]
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);skin(obj,color,binding)
 for position in (-.8,-.3,.3,.8):
  x=cx+position*half
  for edge in (-1,1):
   z=cz+edge*half
   line('Patch Thread',[(x,surface(x,z)-.005,z-.012),(x,surface(x,z)-.005,z+.012)],.004,3,binding)
  z=cz+position*half
  for edge in (-1,1):
   x=cx+edge*half
   line('Patch Thread',[(x-.012,surface(x,z)-.005,z),(x+.012,surface(x,z)-.005,z)],.004,3,binding)
patch('Forehead Patch',-.17,1.58,.043,1,'Head',1.37,.34,.29,.34)
patch('Cheek Patch',.22,1.27,.042,7,'Head',1.37,.34,.29,.34)
patch('Green Belly Patch',-.17,.84,.065,5,'Spine',.85,.42,.28,.43,offset=.03)
# A purple arm and shin patch stay local to their stuffed limb pieces.
for pos,scale,color,binding in (((-.42,-.125,1.04),(.055,.012,.065),4,'Upper_Arm.R'),((.24,-.15,.22),(.06,.012,.06),7,'Lower_Leg.L')):
 bpy.ops.mesh.primitive_cube_add(size=2,location=pos);bpy.context.object.scale=scale;skin(bpy.context.object,color,binding)
 for dz in (-.04,0,.04):line('Patch Thread',[(pos[0]-.065,pos[1]-.02,pos[2]+dz),(pos[0]-.045,pos[1]-.02,pos[2]+dz)],.004,3,binding)
line('Head Center Seam',[(0,-.03,1.71),(0,-.19,1.64),(0,-.26,1.55)],.005,3,'Head')
line('Back Seam',[(0,.25,.68),(0,.285,.87),(0,.23,1.10)],.005,3,'Spine')
for z in (.7,.77,.84,.91,1.0):line('Back Stitch',[(-.018,.286,z),(.018,.286,z)],.004,3,'Spine')
# Hidden inner cotton and dark seam openings emerge independently at each threshold.
for current_stage,anchor,binding in ((1,(-.19,-.08,.96),'Spine'),(2,(.39,.02,1.04),'Upper_Arm.L'),(3,(-.18,0,.44),'Pelvis')):
 x,y,z=anchor
 oval('Seam Opening',(x,y,z),(.12,.012,.065),3,binding,segments=16,rings=8)
 for dx,dz,radius in ((-.045,0,.055),(.025,.025,.06),(.055,-.035,.05)):
  oval('Cotton',(x+dx,y-.025,z+dz),(radius,radius*.8,radius),6,binding,segments=12,rings=8)
current_stage=0
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
mesh=bpy.context.object;mesh.name='SK_MadamePlush'
while len(mesh.modifiers)>1:mesh.modifiers.remove(mesh.modifiers[-1])
mesh.shape_key_add(name='Basis')
marker=mesh.data.attributes['StuffingStage']
for stage,name in ((1,'Stuffing75'),(2,'Stuffing50'),(3,'Stuffing25')):
 key=mesh.shape_key_add(name=name)
 for vertex,data in zip(mesh.data.vertices,key.data):
  if marker.data[vertex.index].value==stage:data.co.y-=.26
def pose(**values):
    return {name[:-2]+"."+name[-1] if name.endswith(("_L","_R")) else name:value
            for name,value in values.items()}

def action(name,end,keys):
    clip=bpy.data.actions.new("MadamePlush_"+name)
    clip.use_fake_user=True
    rig.animation_data_create()
    rig.animation_data.action=clip
    for frame,values in keys:
        for joint in rig.pose.bones:
            joint.rotation_mode="XYZ"
            joint.rotation_euler=values.get(joint.name,(0,0,0))
            joint.keyframe_insert("rotation_euler",frame=frame,group=joint.name)
    clip.use_frame_range=True
    clip.frame_start,clip.frame_end=1,end
    rig.animation_data.action=None
    for joint in rig.pose.bones: joint.rotation_euler=(0,0,0)
    return clip

idle=pose(Chest=(-.12,0,0),Upper_Arm_L=(.12,0,.18),Lower_Arm_L=(.1,0,.3),
          Upper_Arm_R=(.1,0,-.12),Head=(-.08,0,0))
def merged(**values): return {**idle,**pose(**values)}

def aim(name, direction, parent_delta=None):
    """Convert an intended world-space limb direction into the bone's local axes."""
    rest = rig.data.bones[name]
    delta = (rest.tail_local-rest.head_local).rotation_difference(Vector(direction).normalized())
    rotation = rest.matrix_local.to_quaternion()
    local = rotation.inverted() @ (parent_delta.inverted() @ delta if parent_delta else delta) @ rotation
    return tuple(local.to_euler('XYZ')), delta

def arm(side, upper_direction, lower_direction):
    upper, delta = aim('Upper_Arm.'+side, upper_direction)
    lower, _ = aim('Lower_Arm.'+side, lower_direction, delta)
    return {'Upper_Arm.'+side:upper, 'Lower_Arm.'+side:lower,
            'Arm_Bend.'+side:(0,0,0), 'Forearm_Bend.'+side:(0,0,0)}
action("Idle",73,[(1,idle),(19,merged(Spine=(.07,.06,.08),Head=(-.16,0,-.1))),
                   (43,merged(Spine=(-.04,-.04,-.08),Head=(-.05,0,.08))),(73,idle)])
action("Walk",37,[(1,merged(Upper_Leg_L=(.6,0,0),Upper_Leg_R=(-.6,0,0),Upper_Arm_R=(.5,0,0))),
                   (10,merged(Lower_Leg_R=(.85,0,0),Shin_Bend_R=(-.25,0,0))),
                   (19,merged(Upper_Leg_L=(-.6,0,0),Upper_Leg_R=(.6,0,0),Upper_Arm_L=(.5,0,0))),
                   (28,merged(Lower_Leg_L=(.85,0,0),Shin_Bend_L=(-.25,0,0))),
                   (37,merged(Upper_Leg_L=(.6,0,0),Upper_Leg_R=(-.6,0,0),Upper_Arm_R=(.5,0,0)))])
guard=merged(Spine=(.2,0,-.12),Upper_Arm_L=(1.05,0,.2),Lower_Arm_L=(.75,0,.6),
             Upper_Arm_R=(.55,0,-.15),Forearm_Bend_L=(.1,0,.25),Head=(-.2,0,.12))
guard.update(arm('L',(.18,-.18,-.12),(-.05,-.12,.35)))
guard.update(arm('R',(-.18,-.18,-.12),(.05,-.12,.35)))
action("Block",37,[(1,guard),(12,{**guard,**pose(Spine=(.3,0,.12),Arm_Bend_L=(.15,0,.18))}),
                    (25,{**guard,**pose(Spine=(.16,0,-.16),Head=(-.28,0,-.1))}),(37,guard)])
rise=merged(Upper_Leg_L=(-.55,0,0),Lower_Leg_L=(1.1,0,0),Upper_Leg_R=(-.35,0,0),
            Lower_Leg_R=(.7,0,0),Upper_Arm_L=(.2,0,-.7),Upper_Arm_R=(.2,0,.7))
fall=merged(Upper_Arm_L=(.4,0,-.8),Upper_Arm_R=(.4,0,.8),Lower_Leg_L=(.4,0,0),Lower_Leg_R=(.4,0,0))
action("JumpRise",19,[(1,idle),(8,rise),(19,rise)])
action("JumpFall",25,[(1,fall),(13,{**fall,**pose(Lower_Arm_L=(0,0,.25),Lower_Arm_R=(0,0,-.25))}),(25,fall)])
action("JumpLand",15,[(1,merged(Spine=(.4,0,0),Upper_Leg_L=(-.4,0,0),Lower_Leg_L=(.8,0,0),Lower_Leg_R=(.8,0,0))),(15,idle)])
for i,end,active in ((1,33,10),(2,39,13),(3,43,15),(4,51,19)):
    side="L" if i%2 else "R"
    wind=merged(Spine=(-.12,(-1)**i*.28,(-1)**i*.18))
    strike=merged(Spine=(.18,(-1)**i*-.45,(-1)**i*-.25))
    strike.update({"Upper_Arm."+side:(1.4,0,(-1)**i*.5),"Arm_Bend."+side:(.15,0,(-1)**i*.2),
                   "Lower_Arm."+side:(-.12,0,0),"Forearm_Bend."+side:(-.15,0,0)})
    strike.update(arm(side,(.08 if side=='L' else -.08,-1,.05),(.02,-1,.05)))
    if i==3:
        strike.update(pose(Spine=(-.32,0,.22)))
        strike.update(arm('L',(.12,-.6,.75),(0,-.4,.9)))
    if i==4:
        strike.update(pose(Hand_R=(0,0,0),Chest=(.2,.1,0)))
        strike.update(arm('R',(-.05,-1,-.02),(-.05,-1,-.02)))
    clip=action("Punch"+str(i),end,[(1,idle),(active-3,wind),(active,strike),(active+4,strike),(end-5,wind),(end,idle)])
    clip.pose_markers.new("HitboxOn").frame=active
    clip.pose_markers.new("HitboxOff").frame=active+4
for i,end,active in ((1,43,15),(2,51,19)):
    strike=merged(Spine=(-.3,0,.15),Upper_Leg_L=(1.45,0,0),Lower_Leg_L=(-.3,0,0),Shin_Bend_L=(-.15,0,0))
    if i==2: strike=merged(Pelvis=(0,0,.25),Spine=(.2,0,-.3),Upper_Leg_R=(1.5,0,-.8),Lower_Leg_R=(-.2,0,.1))
    side='L' if i==1 else 'R'
    direction=(.1 if i==1 else -.5,-1,.25)
    upper, delta=aim('Upper_Leg.'+side,direction)
    lower, _=aim('Lower_Leg.'+side,direction,delta)
    strike.update({'Upper_Leg.'+side:upper,'Lower_Leg.'+side:lower,'Shin_Bend.'+side:(0,0,0)})
    clip=action("Kick"+str(i),end,[(1,idle),(active-4,rise),(active,strike),(active+4,strike),(end,idle)])
    clip.pose_markers.new("HitboxOn").frame=active
    clip.pose_markers.new("HitboxOff").frame=active+4
# Stitch Uppercut loads a deep crouch, then drives a gloved fist upward.
wind=merged(Pelvis=(.18,0,.1),Spine=(.65,0,-.18),Head=(-.25,0,0),Lower_Leg_L=(.45,0,0),Lower_Leg_R=(.4,0,0))
strike=merged(Spine=(-.25,0,.22),Head=(.12,0,-.1),Upper_Arm_L=(.3,0,-.2))
strike.update(arm('R',(-.08,-.5,.85),(0,-.15,1)))
action('Special',65,[(1,idle),(12,wind),(22,wind),(25,strike),(29,strike),(41,merged(Spine=(.25,0,.15),Upper_Arm_R=(.8,0,.3))),(65,idle)])
clip=bpy.data.actions['MadamePlush_Special'];clip.pose_markers.new('HitboxOn').frame=25;clip.pose_markers.new('HitboxOff').frame=30
for name,amount in (("HitLight",.45),("HitHeavy",.85)):
    action(name,25,[(1,idle),(5,merged(Spine=(-amount,0,.4),Chest=(-.3,0,-.3),Head=(-.4,0,0))),
                    (14,merged(Spine=(amount*.4,0,-.25),Arm_Bend_L=(.3,0,.3))),(25,idle)])
down=merged(Pelvis=(-1.3,0,.15),Spine=(-.2,0,.1),Upper_Arm_L=(0,0,-1),Upper_Arm_R=(0,0,1))
action("Knockdown",31,[(1,idle),(8,fall),(22,down),(31,down)])
action("KO",43,[(1,idle),(12,fall),(28,down),(43,down)])
action("GetUp",61,[(1,down),(22,merged(Pelvis=(-.6,0,0),Spine=(.9,0,0),Upper_Arm_L=(1,0,0))),
                    (43,merged(Spine=(.5,0,0),Lower_Leg_L=(.6,0,0))),(61,idle)])
action("GuardBreak",61,[(1,guard),(9,merged(Spine=(-.8,0,.4),Upper_Arm_L=(0,0,-1.2))),
                         (31,down),(61,down)])
action("Throw",29,[(1,idle),(8,merged(Upper_Arm_L=(-.8,0,-.4))),
                    (13,merged(Upper_Arm_L=(1.5,0,-.2),Forearm_Bend_L=(.3,0,0))),(29,idle)])
hug=merged(Spine=(-.1,0,0),Head=(-.12,0,0))
hug.update(arm('L',(.6,-.8,.1),(-.5,-.6,.1)))
hug.update(arm('R',(-.6,-.8,.1),(.5,-.6,.1)))
belt=merged(Spine=(-.1,0,0))
belt.update(arm('L',(.2,-.4,-.5),(-.1,-.4,-.2)))
belt.update(arm('R',(-.2,-.4,-.5),(.1,-.4,-.2)))
action('Win',91,[(1,idle),(15,belt),(31,belt),(49,hug),(71,hug),(91,hug)])
fix=merged(Spine=(.05,0,.1),Head=(.25,0,0))
fix.update(arm('R',(.1,-.6,-.2),(.3,-.3,.1)))
crack=merged(Spine=(.1,0,0))
crack.update(arm('L',(.03,-.85,-.1),(-.4,-.5,.2)))
crack.update(arm('R',(-.03,-.85,-.1),(.4,-.5,.2)))
action('Taunt',79,[(1,idle),(19,fix),(37,fix),(53,crack),(65,crack),(79,idle)])

problems=validate(scene)
if problems:raise RuntimeError('\n'.join(problems))
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()),axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
 object_types={'ARMATURE','MESH'},apply_scale_options='FBX_SCALE_ALL',bake_anim_use_all_actions=True,
 bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,use_mesh_modifiers=False)
mesh.data.calc_loop_triangles()
report={'triangles':len(mesh.data.loop_triangles),'bones':len(rig.data.bones),'deform_bones':sum(b.use_deform for b in rig.data.bones),
 'materials':len(mesh.data.materials),'height_metres':round(mesh.dimensions.z,3),'actions':sorted(a.name for a in bpy.data.actions),
 'shape_keys':[key.name for key in mesh.data.shape_keys.key_blocks],'validation_errors':problems,
 'reference':'docs/images/characters/concept/madame_plush_turnaround.png'}
source.with_suffix('.validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
