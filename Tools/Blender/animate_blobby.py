"""Author in-place actions on an existing Blobby rig."""
from pathlib import Path
import sys

import bpy

source, export = map(Path, sys.argv[sys.argv.index("--") + 1:])
if source.exists() or export.exists():
    raise FileExistsError("Choose new output paths to preserve the original blockout.")
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
rig.animation_data_create()
scene = bpy.context.scene
scene.render.fps = 60

def action(name, duration, poses):
    previous = bpy.data.actions.get("Blobby_" + name)
    if previous is not None:
        bpy.data.actions.remove(previous)
    clip = bpy.data.actions.new("Blobby_" + name)
    clip.use_fake_user = True
    rig.animation_data.action = clip
    channels = {bone for _, pose in poses for bone in pose}
    for frame, pose in poses:
        for bone in channels:
            joint = rig.pose.bones[bone]
            joint.rotation_mode = "XYZ"
            joint.rotation_euler = pose.get(bone, (0, 0, 0))
            joint.keyframe_insert("rotation_euler", frame=frame, group=bone)
    clip.use_frame_range = True
    clip.frame_start, clip.frame_end = 1, duration
    rig.animation_data.action = None
    for joint in rig.pose.bones:
        joint.rotation_euler = (0, 0, 0)

def pose(**joints):
    return {(name[:-2] + "." + name[-1] if name.endswith(("_L", "_R")) else name): angle for name, angle in joints.items()}

action("Idle", 73, [(1, {}), (19, pose(Spine=(.09,0,.09), Head=(-.12,0,-.08))),
                    (45, pose(Spine=(-.06,0,-.08), Head=(.10,0,.11))), (73, {})])

def stride(sign):
    return pose(Pelvis=(.09,0,.10*sign), Spine=(.16,0,-.13*sign),
                Upper_Leg_L=(.75*sign,0,0), Upper_Leg_R=(-.75*sign,0,0),
                Lower_Leg_L=(max(0, .55*sign),0,0), Lower_Leg_R=(max(0,-.55*sign),0,0),
                Upper_Arm_L=(-.55*sign,0,.10), Upper_Arm_R=(.55*sign,0,-.10))
action("Walk", 37, [(1,stride(1)), (10,pose(Spine=(.25,0,0), Lower_Leg_R=(.62,0,0))),
                   (19,stride(-1)), (28,pose(Spine=(.25,0,0), Lower_Leg_L=(.62,0,0))), (37,stride(1))])
guard = pose(Pelvis=(.10,0,-.09), Spine=(.28,.08,.06), Head=(-.19,0,-.08),
             Upper_Arm_L=(.40,-.60,-.12), Upper_Arm_R=(.35,.46,.09),
             Lower_Arm_L=(.48,0,1.42), Lower_Arm_R=(.43,0,-1.32),
             Upper_Leg_L=(-.20,0,.08), Upper_Leg_R=(-.17,0,-.09),
             Lower_Leg_L=(.37,0,0), Lower_Leg_R=(.31,0,0))
action("Block", 31, [
    (1, guard),
    (10, {**guard, **pose(Spine=(.40,.10,.13), Head=(-.27,0,-.13),
                        Lower_Arm_L=(.58,0,1.53), Lower_Leg_L=(.49,0,0))}),
    (21, {**guard, **pose(Spine=(.30,-.06,-.12), Head=(-.19,0,.09),
                        Lower_Arm_R=(.58,0,-1.48), Lower_Leg_R=(.45,0,0))}),
    (31, guard)])

# Frames 7�10 cover the four active frames at 60 Hz.
# Jab: lead shoulder snaps out, rear fist stays up, then the elbow recoils.
action("Punch1", 23, [
    (1, {}), (4, pose(Spine=(-.08,.18,-.16), Upper_Arm_L=(.04,.40,.23),
                     Lower_Arm_L=(.25,0,1.10), Lower_Arm_R=(.44,0,-1.15))),
    (7, pose(Pelvis=(.10,-.12,0), Spine=(.23,-.22,.22), Head=(-.09,.14,-.12),
             Upper_Arm_L=(.12,-1.32,-.20), Lower_Arm_L=(.06,0,.18),
             Lower_Arm_R=(.46,0,-1.16), Lower_Leg_L=(.29,0,0))),
    (10, pose(Spine=(.15,-.20,.20), Upper_Arm_L=(.11,-1.14,-.23),
              Lower_Arm_L=(.15,0,.31), Lower_Arm_R=(.44,0,-1.10))),
    (14, pose(Spine=(-.10,.10,-.08), Upper_Arm_L=(.09,-.36,.08),
              Lower_Arm_L=(.35,0,1.17))), (23,{})])

# Cross: load the rear hip before driving the rear shoulder through the target.
action("Punch2", 23, [
    (1, {}), (5, pose(Pelvis=(.10,-.34,.12), Spine=(-.13,-.35,.25),
                     Upper_Arm_R=(-.25,-.74,-.28), Lower_Arm_R=(.40,0,-1.04),
                     Lower_Arm_L=(.36,0,1.16), Lower_Leg_R=(.39,0,0))),
    (8, pose(Pelvis=(.13,.41,-.12), Spine=(.28,.43,-.37), Head=(-.19,-.34,.15),
             Upper_Arm_R=(.14,1.42,.15), Lower_Arm_R=(.07,0,-.14),
             Lower_Arm_L=(.44,0,1.09), Lower_Leg_L=(.29,0,0))),
    (11, pose(Pelvis=(.07,.42,-.16), Spine=(.30,.42,-.47),
              Upper_Arm_R=(.25,1.05,.37), Lower_Arm_R=(.22,0,-.48))),
    (16, pose(Spine=(-.14,-.13,.14), Lower_Arm_R=(.34,0,-.79))), (23,{})])

# Uppercut: drop into the knees and drive a bent fist upward past the face.
action("Punch3", 23, [
    (1, {}), (5, pose(Pelvis=(.20,-.22,-.12), Spine=(.61,.15,-.21),
                     Head=(-.28,0,.08), Upper_Arm_L=(-.40,.44,.33),
                     Lower_Arm_L=(.28,0,.83), Lower_Arm_R=(.42,0,-1.10),
                     Upper_Leg_L=(-.35,0,0), Lower_Leg_L=(.62,0,0),
                     Upper_Leg_R=(-.30,0,0), Lower_Leg_R=(.52,0,0))),
    (8, pose(Pelvis=(-.12,.15,.10), Spine=(-.31,-.27,.30), Head=(.12,.16,-.13),
             Upper_Arm_L=(1.27,-.75,-.33), Lower_Arm_L=(.66,0,.64),
             Upper_Arm_R=(-.22,.35,.44))),
    (11, pose(Spine=(-.24,-.20,.40), Upper_Arm_L=(1.51,-.52,-.43),
              Lower_Arm_L=(.52,0,.79), Head=(.10,0,-.21))),
    (16, pose(Spine=(.17,.13,-.13), Upper_Arm_L=(.56,-.22,.03),
              Lower_Arm_L=(.38,0,.94))), (23,{})])

# Hook: keep the elbow bent and carry the fist across the chest on the follow-through.
action("Punch4", 23, [
    (1, {}), (5, pose(Pelvis=(.05,-.42,.13), Spine=(-.11,-.51,.36),
                     Upper_Arm_R=(-.23,-.85,.66), Lower_Arm_R=(.38,0,-1.19),
                     Lower_Arm_L=(.41,0,1.26), Lower_Leg_R=(.42,0,0))),
    (7, pose(Pelvis=(.09,.18,-.05), Spine=(.14,.24,-.20),
             Upper_Arm_R=(.23,.91,.78), Lower_Arm_R=(.46,0,-1.27))),
    (9, pose(Pelvis=(.12,.58,-.13), Spine=(.28,.63,-.43), Head=(-.15,-.40,.24),
             Upper_Arm_R=(.13,1.46,.33), Lower_Arm_R=(.48,0,-1.12),
             Lower_Leg_L=(.35,0,0))),
    (12, pose(Pelvis=(.14,.51,-.20), Spine=(.39,.58,-.51),
              Upper_Arm_R=(.20,1.21,.08), Lower_Arm_R=(.39,0,-1.24))),
    (17, pose(Spine=(-.12,-.18,.16), Lower_Arm_R=(.30,0,-.73))), (23,{})])

for index, (side, sign, height) in enumerate(((".L",1,1.10),(".R",-1,1.60)),1):
    hip, knee = "Upper_Leg"+side, "Lower_Leg"+side
    action(f"Kick{index}",23,[(1,{}),(5,{hip:(-.62,0,.15*sign),knee:(1.10,0,0),"Spine":(.30,0,0)}),
           (8,{hip:(-height,0,.34*sign),knee:(.1,0,0),"Spine":(-.42,0,-.22*sign),"Upper_Arm"+side:(.50,0,.36*sign)}),
           (13,{hip:(-.66,0,0),knee:(.72,0,0),"Spine":(-.12,0,0)}),(23,{})])

tuck = pose(Spine=(.27,0,0), Upper_Leg_L=(-.86,0,0), Upper_Leg_R=(-.72,0,0),
            Lower_Leg_L=(.91,0,0), Lower_Leg_R=(.77,0,0), Upper_Arm_L=(.5,0,-.38), Upper_Arm_R=(.5,0,.38))
spread=pose(Upper_Arm_L=(.25,0,-.68), Upper_Arm_R=(.25,0,.68), Lower_Leg_L=(.30,0,0), Lower_Leg_R=(.30,0,0))
action("JumpRise",31,[(1,pose(Spine=(.3,0,0))), (8,tuck),(31,tuck)])
action("JumpFall",31,[(1,tuck),(13,spread),(31,spread)])
action("JumpLand",15,[(1,spread),(5,pose(Spine=(.52,0,0),Lower_Leg_L=(.68,0,0),Lower_Leg_R=(.68,0,0))),(15,{})])
# Compress, throw both fists forward, and overshoot into an elastic recoil.
action("Special", 23, [
    (1, {}), (3, pose(Spine=(-.29,0,0), Head=(.15,0,0),
                     Upper_Arm_L=(.10,.55,-.78), Upper_Arm_R=(.10,-.55,.78))),
    (6, pose(Pelvis=(.32,0,0), Spine=(.78,0,0), Head=(-.54,0,0),
             Upper_Arm_L=(-.25,.66,-.28), Upper_Arm_R=(-.25,-.66,.28),
             Lower_Arm_L=(.40,0,1.25), Lower_Arm_R=(.40,0,-1.25),
             Upper_Leg_L=(-.48,0,0), Upper_Leg_R=(-.48,0,0),
             Lower_Leg_L=(.86,0,0), Lower_Leg_R=(.86,0,0))),
    (8, pose(Pelvis=(-.12,0,0), Spine=(.36,0,0), Head=(-.14,0,0),
             Upper_Arm_L=(.29,-1.44,-.28), Upper_Arm_R=(.29,1.44,.28),
             Lower_Arm_L=(.09,0,.15), Lower_Arm_R=(.09,0,-.15))),
    (10, pose(Spine=(.41,0,0), Head=(-.18,0,0),
              Upper_Arm_L=(.39,-1.30,-.48), Upper_Arm_R=(.39,1.30,.48))),
    (14, pose(Spine=(-.34,0,.13), Head=(.22,0,-.11),
              Upper_Arm_L=(.29,-.52,-.69), Upper_Arm_R=(.16,.48,.66),
              Lower_Arm_L=(.29,0,.78), Lower_Arm_R=(.29,0,-.78))),
    (18, pose(Spine=(.17,0,-.08), Head=(-.12,0,.09),
              Lower_Leg_L=(.24,0,0), Lower_Leg_R=(.24,0,0))), (23,{})])
for name, intensity in (("HitLight",.44),("HitHeavy",.85)):
    action(name,23,[(1,{}),(5,pose(Spine=(-intensity,0,.3),Head=(-.5*intensity,0,-.22))),
                    (12,pose(Spine=(.26*intensity,0,-.18),Head=(.15,0,.14))),(23,{})])
down = pose(Pelvis=(-1.35,0,.18), Spine=(-.14,0,0), Lower_Leg_L=(.4,0,0), Upper_Arm_R=(0,0,.6))
action("Knockdown",29,[(1,{}),(8,spread),(19,down),(29,down)])
action("GetUp",49,[(1,down),(16,pose(Pelvis=(-.70,0,.22),Spine=(.65,0,0),Upper_Arm_L=(.7,0,0))),
                   (33,pose(Spine=(.34,0,-.13),Lower_Leg_R=(.42,0,0))),(49,{})])
cheer = pose(Upper_Arm_L=(.4,0,-1.4),Upper_Arm_R=(.4,0,1.4),Head=(-.26,0,0))
dip = pose(Spine=(.48,0,0),Lower_Leg_L=(.63,0,0),Lower_Leg_R=(.63,0,0))
action("Win",79,[(1,{}),(9,dip),(16,cheer),(27,dip),(34,cheer),(45,dip),(52,cheer),(66,cheer),(79,{})])
action("Taunt",67,[(1,{}),(12,pose(Spine=(0,.3,.32),Head=(0,-.18,-.28),Lower_Arm_L=(.3,0,1))),
                    (26,pose(Spine=(0,-.3,-.32),Head=(0,.18,.28),Lower_Arm_R=(.3,0,-1))),
                    (42,pose(Spine=(0,.35,.25),Head=(-.24,0,-.25),Lower_Arm_L=(.3,0,.85))),(67,{})])

bpy.ops.wm.save_as_mainfile(filepath=str(source.resolve()))
bpy.ops.export_scene.fbx(filepath=str(export.resolve()), axis_forward="-Z", axis_up="Y",
                         add_leaf_bones=False, object_types={"ARMATURE", "MESH"},
                         bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_simplify_factor=0, path_mode="COPY", embed_textures=True)
