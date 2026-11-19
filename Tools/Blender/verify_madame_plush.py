"""Verify the loaded Madame Plush source, including real action motion and loop closure."""
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
actual = {a.name.removeprefix("MadamePlush_") for a in bpy.data.actions}
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
    if clip.name.removeprefix("MadamePlush_") in {"Idle", "Walk", "Block", "JumpFall"}:
        if any(abs(a-b) > .001 for va,vb in zip(start,end) for a,b in zip(va,vb)):
            errors.append(clip.name + ": loop does not close")
    samples = [snapshot(frame) for frame in range(first,last+1,3)]
    if not any(abs(a-b) > .02 for sample in samples for va,vb in zip(start,sample) for a,b in zip(va,vb)):
        errors.append(clip.name + ": no meaningful pose motion")
    if any(not math.isfinite(value) for sample in samples for values in sample for value in values):
        errors.append(clip.name + ": non-finite pose")
    if clip.name == "MadamePlush_Special":
        if [marker.frame for marker in clip.pose_markers] != [25,30]:
            errors.append("Special pulse markers differ from frame data")
mesh=next(obj for obj in bpy.context.scene.objects if obj.type=='MESH')
if mesh.data.shape_keys is None:errors.append('Stuffing shapes missing')
else:
 keys=mesh.data.shape_keys.key_blocks
 if {key.name for key in keys}!={'Basis','Stuffing75','Stuffing50','Stuffing25'}:errors.append('Wrong stuffing shape set')
 affected=[]
 for name in ('Stuffing75','Stuffing50','Stuffing25'):
  selected={i for i,(base,value) in enumerate(zip(keys['Basis'].data,keys[name].data)) if (value.co-base.co).length>.01}
  if len(selected)<100:errors.append(name+': no visible cotton extrusion')
  if any(selected & other for other in affected):errors.append('Stuffing stages overlap')
  affected.append(selected)
rig.animation_data.action = None
if errors:
    raise RuntimeError("\n".join(errors))
print("Madame Plush: skin/budget checks, all 22 moving actions, four closing loops, independent stuffing morphs and special markers passed.")
