"""Check the exact recorded six-item Blender exports, atlas and synthesized SFX."""
import hashlib
import json
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[2]
source = root / 'Art/_Source/NoveltyItems'
report = json.loads((source / 'NoveltyItems.validation.json').read_text(encoding='utf-8'))
provenance = json.loads((source / 'provenance.json').read_text(encoding='utf-8'))
required = {'CreamPie', 'Boomerang', 'PogoStick', 'BubbleWand', 'WindUpCar', 'BoxingJack'}
if set(report['items']) != required or report['materials'] != 1 or report['validation_errors']:
    raise SystemExit('The six validated item models are incomplete.')
if any(item['triangles'] > 4000 for item in report['items'].values()):
    raise SystemExit('An item exceeds its 4k triangle allocation.')
for name, expected in provenance['files'].items():
    data = (root / name).read_bytes()
    if name.endswith('.py'):
        data = data.replace(b'\r\n', b'\n')
    if hashlib.sha256(data).hexdigest() != expected:
        raise SystemExit('Verified item content changed: ' + name)
atlas = root / 'Assets/_Project/Art/Items/Novelty/NoveltyItems_Atlas.png'
if struct.unpack('>II', atlas.read_bytes()[16:24]) != (1024, 1024):
    raise SystemExit('Expected the shared 1024 atlas.')
for name in ('ItemPie', 'ItemBoomerang', 'ItemPogo', 'ItemBubble', 'ItemCar', 'ItemJack'):
    with wave.open(str(root / 'Assets/_Project/Art/Audio/Resources/Audio' / (name + '.wav'))) as clip:
        if (clip.getnchannels(), clip.getsampwidth(), clip.getframerate()) != (1, 2, 24000):
            raise SystemExit('Invalid item cue PCM format: ' + name)
        duration = .45 if name in ('ItemBoomerang', 'ItemCar') else .3
        if abs(clip.getnframes() / clip.getframerate() - duration) > .001:
            raise SystemExit('Invalid item cue duration: ' + name)
print('Six additional pickup exports, one atlas, source hashes and six original SFX passed.')
