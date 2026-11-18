"""Verify the loaded Sir Noodle source, including real action motion and loop closure."""
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
            "GuardBreak", "Knockdown", "GetUp", "KO", "Win", "Taunt"}
actual = {a.name.removeprefix("SirNoodle_") for a in bpy.data.actions}
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
    if clip.name.removeprefix("SirNoodle_") in {"Idle", "Walk", "Block", "JumpFall"}:
        if any(abs(a-b) > .001 for va,vb in zip(start,end) for a,b in zip(va,vb)):
            errors.append(clip.name + ": loop does not close")
    samples = [snapshot(frame) for frame in range(first,last+1,3)]
    if not any(abs(a-b) > .02 for sample in samples for va,vb in zip(start,sample) for a,b in zip(va,vb)):
        errors.append(clip.name + ": no meaningful pose motion")
    if any(not math.isfinite(value) for sample in samples for values in sample for value in values):
        errors.append(clip.name + ": non-finite pose")
    if clip.name == "SirNoodle_Special":
        if [marker.frame for marker in clip.pose_markers] != [13,19,25,31]:
            errors.append("Special pulse markers differ from frame data")
rig.animation_data.action = None
if errors:
    raise RuntimeError("\n".join(errors))
print("Sir Noodle: skin/budget checks, all 22 moving actions, four closing loops and special markers passed.")
