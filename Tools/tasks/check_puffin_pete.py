"""Verify the exact recorded M4-02 source/export and synthesized pop cue."""
import hashlib
import json
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[2]
folder = root / "Art/_Source/PuffinPete"
report = json.loads((folder / "PuffinPete.validation.json").read_text(encoding="utf-8"))
provenance = json.loads((folder / "provenance.json").read_text(encoding="utf-8"))
for name, expected in provenance["files"].items():
    if hashlib.sha256((root / name).read_bytes()).hexdigest() != expected:
        raise SystemExit("Source/export differs from the verified snapshot: " + name)
if report["validation_errors"] or report["triangles"] > 12000 or report["deform_bones"] > 30:
    raise SystemExit("Blender skin or budget checks failed.")
if report["materials"] != 1 or not 1.5 <= report["height_metres"] <= 1.6:
    raise SystemExit("Unexpected material count or source height.")
required = {"Idle", "Walk", "Block", "JumpRise", "JumpFall", "JumpLand", "Punch1", "Punch2",
            "Punch3", "Punch4", "Kick1", "Kick2", "Special", "Throw", "HitLight", "HitHeavy",
            "Knockdown", "GetUp", "GuardBreak", "KO", "Win", "Taunt", "Deflate", "Refill"}
if set(report["actions"]) != {"PuffinPete_" + name for name in required}:
    raise SystemExit("Required animation set is incomplete.")
if set(report["shape_keys"]) != {"Basis", "Inflate", "Deflate"}:
    raise SystemExit("Required morphs are missing.")
png = root / "Assets/_Project/Art/Characters/PuffinPete/PuffinPete_Body_Albedo.png"
if struct.unpack(">II", png.read_bytes()[16:24]) != (1024, 1024):
    raise SystemExit("Expected a 1024 atlas.")
with wave.open(str(root / "Assets/_Project/Art/Audio/Resources/Audio/PetePop.wav")) as audio:
    if (audio.getnchannels(), audio.getsampwidth(), audio.getframerate()) != (1, 2, 24000):
        raise SystemExit("Invalid pop cue format.")
    if abs(audio.getnframes() / audio.getframerate() - .8) > .001:
        raise SystemExit("Invalid pop cue duration.")
print("Puffin Pete source/export, 24 clips, morphs, atlas and original pop cue evidence passed.")
