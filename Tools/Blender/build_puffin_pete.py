"""Author Puffin Pete's original vinyl penguin, morphs and Generic animation library.

Run with --factory-startup. Existing output files are never overwritten.
"""
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate

source, export, atlas = map(Path, sys.argv[sys.argv.index("--")+1:])
if any(path.exists() for path in (source,export,atlas)):
    raise FileExistsError("Choose fresh output paths to preserve existing art.")
for path in (source,export,atlas): path.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
scene.render.fps=60
bpy.ops.object.armature_add()
rig=bpy.context.object
rig.name='ARM_PuffinPete'
bpy.ops.object.mode_set(mode='EDIT')
bones=rig.data.edit_bones
bones.remove(bones[0])

def bone(name,a,b,parent=None,deform=True):
    item=bones.new(name);item.head=a;item.tail=b;item.use_deform=deform
    if parent: item.parent=bones[parent]

bone('Root',(0,0,0),(0,0,.15),deform=False)
bone('Pelvis',(0,0,.25),(0,0,.48),'Root')
bone('Spine',(0,0,.48),(0,0,.82),'Pelvis')
bone('Chest',(0,0,.82),(0,0,1.03),'Spine')
bone('Head',(0,0,1.03),(0,0,1.48),'Chest')
bone('Tail',(0,.22,.30),(0,.53,.24),'Pelvis')
for suffix,side in (('.L',1),('.R',-1)):
    bone('Upper_Arm'+suffix,(side*.39,0,1.0),(side*.57,0,.72),'Chest')
    bone('Lower_Arm'+suffix,(side*.57,0,.72),(side*.69,0,.47),'Upper_Arm'+suffix)
    bone('Hand'+suffix,(side*.69,0,.47),(side*.73,0,.40),'Lower_Arm'+suffix)
    bone('Flipper_Tip'+suffix,(side*.73,0,.40),(side*.76,0,.34),'Hand'+suffix)
    bone('Upper_Leg'+suffix,(side*.23,0,.32),(side*.23,0,.21),'Pelvis')
    bone('Lower_Leg'+suffix,(side*.23,0,.21),(side*.23,0,.10),'Upper_Leg'+suffix)
    bone('Foot'+suffix,(side*.23,0,.10),(side*.23,-.2,.065),'Lower_Leg'+suffix)
bpy.ops.object.mode_set(mode='OBJECT')
palette=[(.035,.26,.82,1),(.94,.98,1,1),(1,.44,.035,1),(.015,.025,.07,1),
         (.18,.63,1,1),(.25,.82,.19,1),(.50,.62,.72,1),(.025,.09,.28,1)]
image=bpy.data.images.new('PuffinPete_Body_Albedo',width=1024,height=1024,alpha=False)
pixels=[]
for row in range(1024):
    for col in range(1024): pixels.extend(palette[col//128])
image.pixels.foreach_set(pixels)
image.filepath_raw=str(atlas.resolve());image.file_format='PNG';image.save();image.pack()
image.filepath='//PuffinPete_Body_Albedo.png'
material=bpy.data.materials.new('PuffinPete_Vinyl')
material.use_nodes=True
shader=material.node_tree.nodes.get('Principled BSDF')
shader.inputs['Roughness'].default_value=.3
texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image
material.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
parts=[]

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
    obj.parent=rig;mod=obj.modifiers.new('Skin','ARMATURE');mod.object=rig
    parts.append(obj);obj.select_set(False)

def oval(name,position,scale,color,binding,rotation=(0,0,0),segments=20,rings=12):
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

oval('Vinyl Body',(0,0,.65),(.53,.38,.52),0,['Pelvis','Spine','Chest'],segments=28,rings=18)
panel('White Belly',0,.65,.405,.455,.65,.53,.38,.52,['Pelvis','Spine','Chest'])
oval('Round Head',(0,0,1.12),(.42,.355,.43),0,'Head',segments=24,rings=16)
for side in (-1,1):
    panel('White Face',side*.17,1.19,.185,.285,1.12,.42,.355,.43,'Head')
    oval('Eye',(side*.15,-.322,1.28),(.047,.025,.075),3,'Head',segments=16,rings=10)
    oval('Eye Catchlight',(side*.15-.012,-.344,1.305),(.012,.009,.018),1,'Head',segments=12,rings=8)
oval('Orange Beak',(0,-.405,1.08),(.21,.20,.105),2,'Head')
line('Smile',[(-.16,-.51,1.085),(-.10,-.565,1.045),(0,-.585,1.03),(.10,-.565,1.045),(.16,-.51,1.085)],.007,3,'Head')
for suffix,side in (('.L',1),('.R',-1)):
    oval('Flipper'+suffix,(side*.56,0,.73),(.125,.12,.365),0,
         ['Upper_Arm'+suffix,'Lower_Arm'+suffix,'Hand'+suffix,'Flipper_Tip'+suffix],rotation=(0,side*-.32,0))
    for dx in (-.09,0,.09):
        oval('Toe'+suffix,(side*.23+dx,-.11,.07),(.09,.21,.07),2,'Foot'+suffix,segments=16,rings=10)
    for y in (-.18,-.07):
        line('Toe Crease'+suffix,[(side*.23-.05,y,.126),(side*.23+.05,y,.126)],.004,3,'Foot'+suffix)
oval('Tail',(0,.365,.28),(.19,.23,.10),0,'Tail')
# Vinyl seam stitches remain readable without a normal map.
line('Head Seam',[(0,-.05,1.55),(0,-.22,1.50),(0,-.31,1.40)],.005,7,'Head')
line('Back Seam',[(0,.35,.39),(0,.386,.6),(0,.36,.82),(0,.30,.98)],.005,7,'Spine')
line('Belly Seam',[(0,-.40,.30),(0,-.416,.6),(0,-.40,.85)],.004,6,'Spine')
panel('Vinyl Highlight',-.19,1.44,.075,.018,1.12,.42,.355,.43,'Head',color=4)
# Repair patch on the visible left flipper, with a dark stitched edge.
bpy.ops.mesh.primitive_cube_add(size=2,location=(.61,-.119,.66))
obj=bpy.context.object;obj.scale=(.07,.009,.09);skin(obj,5,'Lower_Arm.L')
for z in (.59,.63,.67,.71,.75):
    line('Patch Stitch',[(.535,-.131,z),(.557,-.131,z)],.004,3,'Lower_Arm.L')
    line('Patch Stitch',[(.665,-.131,z),(.687,-.131,z)],.004,3,'Lower_Arm.L')
# Back air valve and cap, visibly different from a tail.
bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=6,location=(0,.39,.54),
    major_radius=.057,minor_radius=.013,rotation=(math.pi/2,0,0))
skin(bpy.context.object,6,'Spine')
oval('Valve Cap',(0,.414,.54),(.037,.014,.045),7,'Spine',segments=12,rings=8)
line('Valve Tab',[(0,.433,.51),(0,.433,.57)],.01,6,'Spine')

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
mesh=bpy.context.object;mesh.name='SK_PuffinPete'
while len(mesh.modifiers)>1:mesh.modifiers.remove(mesh.modifiers[-1])
mesh.shape_key_add(name='Basis')
inflate=mesh.shape_key_add(name='Inflate')
inflate.slider_max=1.5
deflate=mesh.shape_key_add(name='Deflate')
for vertex,up,down in zip(mesh.data.vertices,inflate.data,deflate.data):
    co=vertex.co
    factor=max(0,min(1,(co.z-.10)/.35))
    up.co=(co.x*(1+.28*factor),co.y*(1+.30*factor),.12+(co.z-.12)*(1+.12*factor))
    down.co=(co.x*(1+.20*factor),co.y*(1+.22*factor),.08+(co.z-.08)*(1-.70*factor))

# The action library below uses the same in-place naming contract as the other fighters.
def pose(**values):
    return {name[:-2]+"."+name[-1] if name.endswith(("_L","_R")) else name:value
            for name,value in values.items()}

def action(name,end,keys):
    clip=bpy.data.actions.new("PuffinPete_"+name)
    clip.use_fake_user=True
    rig.animation_data_create()
    rig.animation_data.action=clip
    for frame,values in keys:
        for joint in rig.pose.bones:
            joint.rotation_mode="XYZ"
            joint.rotation_euler=values.get(joint.name,(0,0,0))
            joint.keyframe_insert("rotation_euler",frame=frame,group=joint.name)
            if joint.name == "Root":
                joint.location=(0,0,0)
                joint.keyframe_insert("location",frame=frame,group=joint.name)
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
for i,end,active in ((1,27,8),(2,29,10),(3,32,11),(4,39,14)):
    side="L" if i%2 else "R"
    wind=merged(Spine=(-.12,(-1)**i*.28,(-1)**i*.18))
    strike=merged(Spine=(.18,(-1)**i*-.45,(-1)**i*-.25))
    strike.update({"Upper_Arm."+side:(1.4,0,(-1)**i*.5),"Arm_Bend."+side:(.15,0,(-1)**i*.2),
                   "Lower_Arm."+side:(-.12,0,0),"Forearm_Bend."+side:(-.15,0,0)})
    strike.update(arm(side,(.08 if side=='L' else -.08,-1,.05),(.02,-1,.05)))
    if i==3:
        strike=merged(Spine=(.45,0,0),Chest=(.2,0,0),Head=(.65,0,0),
                      Upper_Arm_L=(0,0,-.5),Upper_Arm_R=(0,0,.5))
    if i==4:
        strike=merged(Spine=(.55,0,0),Chest=(.25,0,0),Head=(-.3,0,0),
                      Upper_Arm_L=(0,0,-.7),Upper_Arm_R=(0,0,.7))
    clip=action("Punch"+str(i),end,[(1,idle),(active-3,wind),(active,strike),(active+4,strike),(end-5,wind),(end,idle)])
    clip.pose_markers.new("HitboxOn").frame=active
    clip.pose_markers.new("HitboxOff").frame=active+4
for i,end,active in ((1,33,11),(2,39,14)):
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
# Belly Blimp is a slow forward bumper with a visual roll and recovery wobble.
roll=[]
for frame,angle in ((1,0),(13,-.2),(19,0),(25,math.pi/2),(31,math.pi),(37,math.pi*1.5),(43,math.tau),(55,math.tau),(73,math.tau)):
    roll.append((frame,{"Upper_Arm.L":(0,0,-.35),"Upper_Arm.R":(0,0,.35),"Root":(angle,0,0)}))
clip=action("Special",73,roll)
rig.animation_data.action=clip
for frame in range(1,74):
    scene.frame_set(frame)
    root=rig.pose.bones['Root']
    angle=root.rotation_euler.x
    phase=max(0,min(1,(frame-19)/24))
    height=.22*math.sin(math.pi*phase)
    # Root local Y is world up; local Z is world -Y. Rotate around the belly center.
    root.location=(0,.9*(1-math.cos(angle))+height,-.9*math.sin(angle))
    root.keyframe_insert('location',frame=frame,group='Root')
rig.pose.bones['Root'].location=(0,0,0)
rig.animation_data.action=None
clip.pose_markers.new('HitboxOn').frame=19
clip.pose_markers.new('HitboxOff').frame=43
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
hero=merged(Spine=(.55,0,0),Upper_Arm_L=(0,0,-1.1),Upper_Arm_R=(0,0,1.1),Lower_Leg_L=(.8,0,0),Lower_Leg_R=(.8,0,0))
action("Win",91,[(1,idle),(19,hero),(37,{**hero,**pose(Head=(-.2,0,.2))}),(61,hero),(91,hero)])
tap_left=merged(Upper_Arm_L=(1.0,0,.4),Lower_Arm_L=(.8,0,.3),Spine=(.1,0,.12))
tap_right=merged(Upper_Arm_R=(1.0,0,-.4),Lower_Arm_R=(.8,0,-.3),Spine=(.1,0,-.12))
action("Taunt",73,[(1,idle),(13,tap_left),(25,tap_right),(37,tap_left),(49,tap_right),(61,tap_left),(73,idle)])
flat=merged(Spine=(.25,0,.15),Upper_Arm_L=(0,0,-1),Upper_Arm_R=(0,0,1))
action("Deflate",37,[(1,flat),(10,{**flat,**pose(Head=(.15,0,-.3))}),(24,{**flat,**pose(Head=(-.15,0,.3))}),(37,flat)])
action("Refill",25,[(1,flat),(13,merged(Spine=(-.2,0,-.15))),(25,idle)])

problems=validate(scene)
if problems:raise RuntimeError("\n".join(problems))
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()),axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
    object_types={"ARMATURE","MESH"},apply_scale_options="FBX_SCALE_ALL",bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,use_mesh_modifiers=False)
mesh.data.calc_loop_triangles()
report={"triangles":len(mesh.data.loop_triangles),"bones":len(rig.data.bones),"deform_bones":sum(b.use_deform for b in rig.data.bones),
 "materials":len(mesh.data.materials),"height_metres":round(mesh.dimensions.z,3),"actions":sorted(a.name for a in bpy.data.actions),
 "shape_keys":[key.name for key in mesh.data.shape_keys.key_blocks],"validation_errors":problems,
 "reference":"docs/images/characters/concept/puffin_pete_turnaround.png"}
source.with_suffix('.validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
