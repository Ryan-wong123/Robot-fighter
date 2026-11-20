"""Validate the final game audio bank against its per-clip provenance."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import wave

ROOT = Path(__file__).resolve().parents[2]
AUDIO = ROOT / "Assets/_Project/Art/Audio/Resources"
catalog = json.loads((ROOT / "Tools/Audio/final_audio_catalog.json").read_text(encoding="utf-8"))
actual = {path.relative_to(AUDIO).as_posix() for path in AUDIO.rglob("*.wav")}
assert actual == set(catalog), "Audio bank differs from the provenance inventory"
voice_bytes = 0
voice_groups = Counter()
for name, record in catalog.items():
    path = AUDIO / name
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record["sha256"], name
    assert record.get("source"), name
    with wave.open(str(path)) as stream:
        assert stream.getnchannels() == 1 and stream.getsampwidth() == 2, name
        if name.startswith("Voices/"):
            voice_bytes += path.stat().st_size
            voice_groups[path.stem.rsplit("_v", 1)[0]] += 1
            seconds = stream.getnframes() / stream.getframerate()
            assert stream.getframerate() == 24000 and .2 <= seconds <= 1.5, name
            assert record.get("license_source") and record.get("model_revision") and record.get("seed") is not None, name
        else:
            assert record.get("license_note"), name
assert all(count >= 2 for count in voice_groups.values()), "Missing voice variant"
assert voice_bytes < 5_000_000, "Voice budget exceeded before compression"
for actor in ("blobby", "sirnoodle", "puffinpete", "madameplush", "mrjack"):
    for event in ("attack_light", "attack_heavy", "hit_light", "hit_heavy", "taunt", "win"):
        assert f"{actor}_{event}" in voice_groups, f"Missing {actor} {event}"
for event in ("fight", "round_two", "round_three", "ko", "win", "draw", "sudden_death"):
    assert f"announcer_{event}" in voice_groups, event
print(f"PASS: {len(actual)} recorded audio files, {len(voice_groups)} voice groups, {voice_bytes:,} uncompressed voice bytes")
