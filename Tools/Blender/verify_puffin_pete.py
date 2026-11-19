"""Verify the loaded Puffin Pete source, including real action motion and loop closure."""
import math
from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_export import validate

errors = validate(bpy.context.scene)
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
required = {"Idle", "Walk", "JumpRise", "JumpFall", "JumpLand", "Block", "Punch1", "Punch2",
            "Punch3", "Punch4", "Kick1", "Kick2", "Special", "Throw", "HitLight", "HitHeavy",
            "GuardBreak", "Knockdown", "GetUp", "KO", "Win", "Taunt", "Deflate", "Refill"}
actual = {a.name.removeprefix("PuffinPete_") for a in bpy.data.actions}
if actual != required:
    errors.append("Action set differs: " + str(actual ^ required))
for clip in bpy.data.actions:
    rig.animation_data.action = clip
    first, last = map(int, clip.frame_range)
    def snapshot(frame):
        bpy.context.scene.frame_set(frame)
        return [tuple(joint.rotation_euler) for joint in rig.pose.bones]
    start = snapshot(first)
    end = snapshot(last)
    if clip.name.removeprefix("PuffinPete_") in {"Idle", "Walk", "Block", "JumpFall", "Deflate"}:
        if any(abs(a-b) > .001 for va,vb in zip(start,end) for a,b in zip(va,vb)):
            errors.append(clip.name + ": loop does not close")
    samples = [snapshot(frame) for frame in range(first,last+1,3)]
    if not any(abs(a-b) > .02 for sample in samples for va,vb in zip(start,sample) for a,b in zip(va,vb)):
        errors.append(clip.name + ": no meaningful pose motion")
    if any(not math.isfinite(value) for sample in samples for values in sample for value in values):
        errors.append(clip.name + ": non-finite pose")
    if clip.name == "PuffinPete_Special":
        if [marker.frame for marker in clip.pose_markers] != [19,43]:
            errors.append("Special pulse markers differ from frame data")
        bpy.context.scene.frame_set(31)
        if abs(rig.pose.bones['Root'].rotation_euler.x) < math.pi*.5:
            errors.append('Belly Blimp must roll the full rig')
        if abs(rig.pose.bones['Spine'].rotation_euler.x) > .4:
            errors.append('Belly Blimp twists the weighted torso instead of the full rig')
mesh=next(obj for obj in bpy.context.scene.objects if obj.type=='MESH')
keys=mesh.data.shape_keys.key_blocks if mesh.data.shape_keys else []
if {key.name for key in keys} != {'Basis','Inflate','Deflate'}:
    errors.append('Required inflation morphs are missing')
else:
    basis=keys['Basis']
    base_height=max(v.co.z for v in basis.data)-min(v.co.z for v in basis.data)
    up=keys['Inflate'];down=keys['Deflate']
    if max(v.co.z for v in up.data) <= max(v.co.z for v in basis.data)*1.05:
        errors.append('Inflate morph must visibly enlarge the body')
    if max(v.co.z for v in down.data)-min(v.co.z for v in down.data) >= base_height*.5:
        errors.append('Deflate morph must flatten the body')
rig.animation_data.action = None
if errors:
    raise RuntimeError("\n".join(errors))
print("Puffin Pete: skin/budget checks, all 24 moving actions, five closing loops, morphs and special markers passed.")
