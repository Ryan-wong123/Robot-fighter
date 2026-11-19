"""Verify the active Mr. Jack source including action motion, loops and root motion."""
import bpy
import json
import math
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from validate_export import validate
errors=validate(bpy.context.scene)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
if len([o for o in bpy.context.scene.objects if o.type=='MESH'])!=1:errors.append('Expected one combined skin')
base={'Idle','Walk','JumpRise','JumpFall','JumpLand','Block','Punch1','Punch2','Punch3','Punch4','Kick1','Kick2','Special','HitLight','HitHeavy','Knockdown','GetUp','Win','Taunt','GuardBreak','Throw','KO'}
expected={"MrJack_"+prefix+name for prefix in ('','Out') for name in base}|{'MrJack_PhaseChange'}
if {a.name for a in bpy.data.actions}!=expected:errors.append('Wrong action set')
frames={}
for action in bpy.data.actions:
 curves=list(action.fcurves)
 moving=[c for c in curves if max(p.co.y for p in c.keyframe_points)-min(p.co.y for p in c.keyframe_points)>.015]
 if not moving:errors.append('No real motion: '+action.name)
 if action.name.removeprefix('MrJack_').removeprefix('Out') in ('Idle','Walk','Block','JumpFall'):
  if any(abs(c.evaluate(1)-c.evaluate(action.frame_end))>.0001 for c in curves):errors.append('Loop does not close: '+action.name)
 for curve in curves:
  if 'pose.bones["Root"]' in curve.data_path and not curve.data_path.endswith('scale') and any(abs(p.co.y)>.0001 for p in curve.keyframe_points):errors.append('Root motion: '+action.name)
 markers={m.name:int(m.frame) for m in action.pose_markers}
 if 'HitboxOn' in markers:
  frames[action.name]={'startup':markers['HitboxOn']-1,'active':markers['HitboxOff']-markers['HitboxOn'],'recovery':int(action.frame_end)-markers['HitboxOff']}
 if any(p.co.x<1 or p.co.x>action.frame_end for c in curves for p in c.keyframe_points):errors.append('Key outside authored range: '+action.name)
for name in ('MrJack_Special','MrJack_OutSpecial'):
 curves=[c for c in bpy.data.actions[name].fcurves if c.data_path=='pose.bones["Spring1"].scale']
 if len(curves)!=3 or any(c.evaluate(75)>.03 or c.evaluate(76)<.99 for c in curves):errors.append('Special must hide until the marked pop: '+name)
report={'actions':len(bpy.data.actions),'loops':8,'frame_data':frames,'errors':errors}
Path(bpy.data.filepath).with_suffix('.animation_validation.json').write_text(json.dumps(report,indent=2)+'\n')
if errors:raise RuntimeError('\n'.join(errors))
print('PASS: 45 moving actions, eight closed loops, in-place root, marked attack windows')
