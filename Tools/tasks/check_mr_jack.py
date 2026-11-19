"""Check the exact authored boss content snapshot and offline validation evidence."""
import hashlib
import json
import re
from pathlib import Path
import struct
import wave
root=Path(__file__).resolve().parents[2]
folder=root/'Art/_Source/MrJack'
provenance=json.loads((folder/'provenance.json').read_text(encoding='utf-8'))
for name,expected in provenance['files'].items():
 data=(root/name).read_bytes()
 if name.endswith('.py'):data=data.replace(b'\r\n',b'\n')
 if hashlib.sha256(data).hexdigest()!=expected:raise SystemExit('Changed authored snapshot: '+name)
report=json.loads((folder/'MrJack.validation.json').read_text(encoding='utf-8'))
animation=json.loads((folder/'MrJack.animation_validation.json').read_text(encoding='utf-8'))
assert not report['validation_errors'] and report['triangles']<=12000 and report['bones']<=30
assert len(report['actions'])==45 and animation['actions']==45 and not animation['errors']
assert struct.unpack('>II',(root/'Assets/_Project/Art/Characters/MrJack/MrJack_Body_Albedo.png').read_bytes()[16:24])==(1024,1024)
for cue in ('MrJackLaugh','MrJackMusicBox'):
 with wave.open(str(root/('Assets/_Project/Art/Audio/Resources/Audio/'+cue+'.wav'))) as audio:
  assert (audio.getnchannels(),audio.getsampwidth(),audio.getframerate())==(1,2,24000)
  assert audio.getnframes()/audio.getframerate()<=1.5
source=json.loads((folder/'audio_provenance.json').read_text(encoding='utf-8'))
assert hashlib.sha256((root/'Assets/_Project/Art/Audio/Resources/Audio/MrJackLaugh.wav').read_bytes()).hexdigest()==source['laugh']['sha256']
editor=(root/'Assets/_Project/Scripts/Legacy/Editor/MrJackContent.cs').read_text(encoding='utf-8')
chains=list(re.finditer(r'Chain\("((?:Out)?(?:Punch|Kick))",\s*new\[\]\s*\{([^}]+)\},\s*new\[\]\s*\{([^}]+)\}',editor))
assert len(chains)==4
for chain in chains:
 starts=[int(value) for value in chain[2].split(',')]
 recoveries=[int(value) for value in chain[3].split(',')]
 for index,(startup,recovery) in enumerate(zip(starts,recoveries),1):
  evidence=animation['frame_data']['MrJack_'+chain[1]+str(index)]
  assert evidence=={'startup':startup,'active':5,'recovery':recovery},chain[1]+str(index)
print('PASS: boss source/export, 45 actions, loop/window evidence, atlas and licensed audio snapshot')
