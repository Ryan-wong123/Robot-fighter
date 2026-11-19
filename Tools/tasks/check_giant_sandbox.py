"""Verify the recorded Blender environment, atlas, thumbnail and original audio exports."""
import hashlib
import json
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[2]
source = root / 'Art/_Source/GiantSandbox'
report = json.loads((source / 'GiantSandbox.validation.json').read_text(encoding='utf-8'))
provenance = json.loads((source / 'provenance.json').read_text(encoding='utf-8'))
for name, expected in provenance['files'].items():
    data = (root / name).read_bytes()
    if name.endswith('.py'):
        data = data.replace(b'\r\n', b'\n')
    if hashlib.sha256(data).hexdigest() != expected:
        raise SystemExit('Validated content changed: ' + name)
if report['validation_errors'] or report['triangles'] > 26000 or report['materials'] != 1:
    raise SystemExit('Environment budget/validation failed.')
if set(report['meshes']) != {'SM_Backdrop', 'SM_Paw', 'SM_Shadow', 'SM_Whiskers', 'SM_Spiral'}:
    raise SystemExit('The four reusable hazard meshes must be separate from the backdrop.')
for name, size in [('GiantSandbox_Atlas.png', (1024, 1024)), ('GiantSandbox_Thumbnail.png', (960, 540))]:
    data = (root / 'Assets/_Project/Art/GiantSandbox' / name).read_bytes()
    if struct.unpack('>II', data[16:24]) != size:
        raise SystemExit('Invalid stage texture size: ' + name)
for name, duration in {'SandMusic': 8, 'SandAmbience': 8, 'SandPurr': 2, 'SandSwirl': 2, 'SandThump': .3}.items():
    with wave.open(str(root / 'Assets/_Project/Art/Audio/Resources/Audio' / (name + '.wav'))) as audio:
        if (audio.getnchannels(), audio.getsampwidth(), audio.getframerate()) != (1, 2, 24000):
            raise SystemExit('Invalid PCM format: ' + name)
        if abs(audio.getnframes() / audio.getframerate() - duration) > .001:
            raise SystemExit('Invalid cue duration: ' + name)
print('Giant Sandbox source/export, hazard meshes, one atlas, thumbnail and five original audio cues passed.')
