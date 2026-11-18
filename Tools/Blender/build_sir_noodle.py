"""Build Sir Noodle's reference-based skinned model and in-place action library.

Run in a clean Blender session; refuses existing outputs to preserve art sources.
"""
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate

source, export, atlas = map(Path, sys.argv[sys.argv.index("--") + 1:])
if any(p.exists() for p in (source, export, atlas)):
    raise FileExistsError("Use fresh output paths; existing art is preserved.")
for p in (source, export, atlas):
    p.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1
scene.render.fps = 60
bpy.ops.object.armature_add()
rig = bpy.context.object
rig.name = "ARM_SirNoodle"
bpy.ops.object.mode_set(mode="EDIT")
bones = rig.data.edit_bones
bones.remove(bones[0])

def bone(name, a, b, parent=None, deform=True):
    item = bones.new(name)
    item.head, item.tail = a, b
    item.use_deform = deform
    if parent:
        item.parent = bones[parent]

bone("Root", (0,0,0), (0,0,.2), deform=False)
bone("Pelvis", (0,0,.9), (0,0,1.12), "Root")
bone("Spine", (0,0,1.12), (0,0,1.4), "Pelvis")
bone("Chest", (0,0,1.4), (0,0,1.62), "Spine")
bone("Head", (0,0,1.62), (0,0,1.94), "Chest")
for suffix, side in ((".L",1),(".R",-1)):
    shoulder = Vector((side*.23,0,1.5))
    elbow = Vector((side*.43,0,1.14))
    wrist = Vector((side*.52,0,.79))
    hip, knee, ankle = Vector((side*.13,0,.95)), Vector((side*.16,0,.53)), Vector((side*.19,0,.11))
    for upper, follow, lower, follow2, tip, a, b, c, d, parent in (
        ("Upper_Arm","Arm_Bend","Lower_Arm","Forearm_Bend","Hand",shoulder,elbow,wrist,
         Vector((side*.54,0,.70)),"Chest"),
        ("Upper_Leg","Thigh_Bend","Lower_Leg","Shin_Bend","Foot",hip,knee,ankle,
         Vector((side*.19,-.15,.06)),"Pelvis")):
        bone(upper+suffix,a,(a+b)/2,parent)
        bone(follow+suffix,(a+b)/2,b,upper+suffix)
        bone(lower+suffix,b,(b+c)/2,follow+suffix)
        bone(follow2+suffix,(b+c)/2,c,lower+suffix)
        bone(tip+suffix,c,d,follow2+suffix)
bpy.ops.object.mode_set(mode="OBJECT")

palette = [(1,.49,.68,1),(.40,.82,.94,1),(.37,.44,.52,1),(.76,.84,.87,1),
           (.73,.48,.25,1),(1,.88,.32,1),(.12,.13,.22,1),(.95,.89,.71,1)]
image = bpy.data.images.new("SirNoodle_Body_Albedo",width=1024,height=1024,alpha=False)
pixels = []
for y in range(1024):
    for x in range(1024):
        color = palette[min(7,x//128)]
        # Narrow ring grooves remain flat color, without baked light or shadow.
        if x//128 in (0,1) and y % 128 < 7:
            color = tuple(v*.77 for v in color[:3])+(1,)
        pixels.extend(color)
image.pixels.foreach_set(pixels)
image.filepath_raw = str(atlas.resolve())
image.file_format = "PNG"
image.save()
image.pack()
image.filepath = "//SirNoodle_Body_Albedo.png"
material = bpy.data.materials.new("SirNoodle_Body_Albedo")
material.use_nodes = True
shader = material.node_tree.nodes.get("Principled BSDF")
shader.inputs["Roughness"].default_value = .8
tex = material.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = image
material.node_tree.links.new(tex.outputs["Color"], shader.inputs["Base Color"])
parts = []

def skin(obj, color, binding):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="Atlas")
    obj.data.uv_layers.active.name = "Atlas"
    uv = obj.data.uv_layers.active.data
    for poly in obj.data.polygons:
        poly.use_smooth = True
        for loop in poly.loop_indices:
            old = uv[loop].uv.copy()
            uv[loop].uv = ((color+.12+old.x*.76)/8,old.y)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    if isinstance(binding,str):
        group = obj.vertex_groups.new(name=binding)
        group.add(list(range(len(obj.data.vertices))),1,"REPLACE")
    else:
        groups={name:obj.vertex_groups.new(name=name) for name in binding}
        for v in obj.data.vertices:
            distances=[]
            for name in binding:
                b=rig.data.bones[name]
                axis=b.tail_local-b.head_local
                t=max(0,min(1,(v.co-b.head_local).dot(axis)/axis.length_squared))
                distances.append(((v.co-b.head_local-axis*t).length_squared,name))
            closest=sorted(distances)[:2]
            weights=[1/(d+.001)**2 for d,_ in closest]
            for (_,name),w in zip(closest,weights):
                groups[name].add([v.index],w/sum(weights),"REPLACE")
    obj.parent=rig
    mod=obj.modifiers.new("Skin","ARMATURE")
    mod.object=rig
    parts.append(obj)
    obj.select_set(False)

def oval(name, position, scale, color, binding):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=position)
    obj=bpy.context.object
    obj.name=name
    obj.scale=scale
    skin(obj,color,binding)

def tube(name,a,b,radius,color,binding,rings=9):
    axis=Vector(b)-Vector(a)
    verts=[]
    tangent=axis.normalized().cross(Vector((0,1,0))).normalized()
    normal=axis.normalized().cross(tangent)
    for ring in range(rings):
        t=ring/(rings-1)
        r=radius*(.94+.06*math.sin(math.pi*t))
        for side in range(12):
            angle=side*math.tau/12
            verts.append(Vector(a)+axis*t+r*(math.cos(angle)*tangent+math.sin(angle)*normal))
    faces=[]
    for ring in range(rings-1):
        for side in range(12):
            a0=ring*12+side;b0=ring*12+(side+1)%12
            faces.append((a0,b0,b0+12,a0+12))
    faces += [tuple(reversed(range(12))),tuple(range((rings-1)*12,rings*12))]
    data=bpy.data.meshes.new(name)
    data.from_pydata(verts,[],faces)
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    layer=data.uv_layers.new(name="Atlas")
    for loop in data.loops:
        layer.data[loop.index].uv=(loop.vertex_index%12/12,loop.vertex_index//12/(rings-1))
    skin(obj,color,binding)

for z,scale,color,binding in ((.99,(.20,.14,.13),1,"Pelvis"),(1.18,(.20,.14,.11),0,"Spine"),
                               (1.39,(.24,.15,.14),1,"Chest"),(1.59,(.12,.11,.08),1,"Head"),
                               (1.76,(.135,.115,.17),0,"Head")):
    oval("Noodle Segment",(0,0,z),scale,color,binding)
for suffix,side in ((".L",1),(".R",-1)):
    for upper,follow,lower,follow2,color,radius in (("Upper_Arm","Arm_Bend","Lower_Arm","Forearm_Bend",0,.065),
                                                  ("Upper_Leg","Thigh_Bend","Lower_Leg","Shin_Bend",0,.067)):
        for start,end,tint in ((upper,lower,color),(lower,"Hand" if "Arm" in upper else "Foot",1)):
            binding=[start+suffix,(follow if start==upper else follow2)+suffix]
            tube(start+suffix,rig.data.bones[start+suffix].head_local,rig.data.bones[end+suffix].head_local,
                 radius,tint,binding)
    oval("Hand"+suffix,(side*.54,0,.73),(.09,.08,.10),0,"Hand"+suffix)
    oval("Boot"+suffix,(side*.19,-.045,.065),(.10,.15,.065),1,"Foot"+suffix)
# Dented cooking-pot helmet: closed foil shell plus an exaggerated rolled rim and handles.
tube("Foil Pot",(0,0,1.86),(0,0,1.985),.18,3,"Head",rings=4)
for obj in parts[-1:]:
    for v in obj.data.vertices:
        v.co.x += .012*math.sin(v.index*1.7)
        v.co.y += .008*math.cos(v.index*2.3)
for x in (-.195,.195):
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=(x,0,1.89),
                                  major_radius=.047,minor_radius=.011,rotation=(math.pi/2,0,0))
    skin(bpy.context.object,2,"Head")
bpy.ops.mesh.primitive_torus_add(major_segments=24,minor_segments=6,location=(0,0,1.86),
                              major_radius=.18,minor_radius=.014)
skin(bpy.context.object,2,"Head")
for x in (-.046,.046):
    oval("Eye",(x,-.109,1.79),(.016,.014,.026),6,"Head")
    tube("Proud Brow",(x-.022,-.121,1.831),(x+.021,-.121,1.818),.008,6,"Head",rings=2)
oval("Nose",(0,-.123,1.77),(.02,.023,.036),0,"Head")
tube("Pompous Mouth",(-.043,-.113,1.718),(.043,-.113,1.724),.007,6,"Head",rings=2)
# Plain cardboard rectangle strapped to the left forearm.
for pos,scale,color in (((.50,-.09,.96),(.115,.023,.17),4),((.50,-.116,.96),(.099,.005,.152),7)):
    bpy.ops.mesh.primitive_cube_add(size=2,location=pos)
    bpy.context.object.scale=scale
    skin(bpy.context.object,color,"Lower_Arm.L")
# Rounded foam sword, blunt tip and crossguard, held in the right hand.
tube("Foam Blade",(-.54,-.01,.70),(-.68,-.01,.19),.042,5,"Hand.R")
for vertex in parts[-1].data.vertices:
    vertex.co.y = -.01 + (vertex.co.y + .01) * .4
oval("Blunt Tip",(-.68,-.01,.19),(.042,.042,.05),5,"Hand.R")
tube("Foam Crossguard",(-.65,-.01,.67),(-.43,-.01,.73),.025,5,"Hand.R",rings=2)

# Join into one weighted renderer, retaining the atlas and per-part weights.
bpy.ops.object.select_all(action="DESELECT")
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
mesh=bpy.context.object
mesh.name="SK_SirNoodle"
while len(mesh.modifiers)>1: mesh.modifiers.remove(mesh.modifiers[-1])

def pose(**values):
    return {name[:-2]+"."+name[-1] if name.endswith(("_L","_R")) else name:value
            for name,value in values.items()}

def action(name,end,keys):
    clip=bpy.data.actions.new("SirNoodle_"+name)
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
action("Block",37,[(1,guard),(12,{**guard,**pose(Spine=(.3,0,.12),Arm_Bend_L=(.15,0,.18))}),
                    (25,{**guard,**pose(Spine=(.16,0,-.16),Head=(-.28,0,-.1))}),(37,guard)])
rise=merged(Upper_Leg_L=(-.55,0,0),Lower_Leg_L=(1.1,0,0),Upper_Leg_R=(-.35,0,0),
            Lower_Leg_R=(.7,0,0),Upper_Arm_L=(.2,0,-.7),Upper_Arm_R=(.2,0,.7))
fall=merged(Upper_Arm_L=(.4,0,-.8),Upper_Arm_R=(.4,0,.8),Lower_Leg_L=(.4,0,0),Lower_Leg_R=(.4,0,0))
action("JumpRise",19,[(1,idle),(8,rise),(19,rise)])
action("JumpFall",25,[(1,fall),(13,{**fall,**pose(Forearm_Bend_L=(0,0,.25),Forearm_Bend_R=(0,0,-.25))}),(25,fall)])
action("JumpLand",15,[(1,merged(Spine=(.4,0,0),Upper_Leg_L=(-.4,0,0),Lower_Leg_L=(.8,0,0),Lower_Leg_R=(.8,0,0))),(15,idle)])
for i,end,active in ((1,27,8),(2,29,10),(3,32,11),(4,39,14)):
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
propeller=merged(Upper_Arm_L=(0,0,-1.15),Upper_Arm_R=(0,0,1.15),Arm_Bend_L=(0,0,-.2),Arm_Bend_R=(0,0,.2))
propeller.update(arm('L',(1,0,0),(1,0,0)))
propeller.update(arm('R',(-1,0,0),(-1,0,0)))
spin=[]
for frame,angle in ((1,0),(7,0),(13,math.pi/2),(19,math.pi),(25,math.pi*1.5),(31,math.tau),(40,math.tau)):
    spin.append((frame,{**propeller,"Root":(0,angle,0)} if frame>7 else idle))
spin += [(49,{**merged(Spine=(.7,0,.25),Head=(-.4,0,-.3)),"Root":(0,math.tau,0)}),
         (67,{**idle,"Root":(0,math.tau,0)})]
clip=action("Special",67,spin)
for frame in (13,19,25,31): clip.pose_markers.new("HitPulse").frame=frame
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
hero=merged(Spine=(-.2,0,.25),Chest=(-.15,0,-.2),Upper_Arm_R=(2.4,0,.2),Hand_R=(1,0,0),
            Upper_Arm_L=(.4,0,.6),Head=(-.25,0,0))
action("Win",91,[(1,idle),(22,hero),(43,{**hero,**pose(Arm_Bend_R=(0,0,.18))}),(67,hero),(91,hero)])
bow=merged(Spine=(.9,0,.15),Chest=(.6,0,-.15),Head=(.3,0,0),Upper_Arm_R=(.3,0,1.4))
action("Taunt",91,[(1,idle),(19,hero),(43,bow),(61,bow),(79,hero),(91,idle)])

problems=validate(scene)
if problems: raise RuntimeError("\n".join(problems))
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()),axis_forward="-Z",axis_up="Y",
    add_leaf_bones=False,object_types={"ARMATURE","MESH"},apply_scale_options="FBX_SCALE_ALL",
    bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
mesh.data.calc_loop_triangles()
report={"triangles":len(mesh.data.loop_triangles),"bones":len(rig.data.bones),
        "deform_bones":sum(b.use_deform for b in rig.data.bones),"materials":len(mesh.data.materials),
        "height_metres":round(mesh.dimensions.z,3),"actions":sorted(a.name for a in bpy.data.actions),
        "validation_errors":problems,"reference":"docs/images/characters/concept/sir_noodle_turnaround.png"}
source.with_suffix(".validation.json").write_text(json.dumps(report,indent=2)+"\n")
print(json.dumps(report))
