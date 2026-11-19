"""Verify the exact recorded M4-03 source/export and synthesized cloth/bell cues."""
import hashlib
import json
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[2]
folder = root / "Art/_Source/MadamePlush"
report = json.loads((folder / "MadamePlush.validation.json").read_text(encoding="utf-8"))
provenance = json.loads((folder / "provenance.json").read_text(encoding="utf-8"))
for name, expected in provenance["files"].items():
    data = (root / name).read_bytes()
    if name.endswith(".py"):
        data = data.replace(b"\r\n", b"\n")
    if hashlib.sha256(data).hexdigest() != expected:
        raise SystemExit("Source/export differs from the verified snapshot: " + name)
if report["validation_errors"] or report["triangles"] > 12000 or report["deform_bones"] > 30:
    raise SystemExit("Blender skin or budget checks failed.")
if report["materials"] != 1 or not 1.79 <= report["height_metres"] <= 1.81:
    raise SystemExit("Unexpected material count or source height.")
required = {"Idle", "Walk", "Block", "JumpRise", "JumpFall", "JumpLand", "Punch1", "Punch2",
            "Punch3", "Punch4", "Kick1", "Kick2", "Special", "Throw", "HitLight", "HitHeavy",
            "Knockdown", "GetUp", "GuardBreak", "KO", "Win", "Taunt"}
if set(report["actions"]) != {"MadamePlush_" + name for name in required}:
    raise SystemExit("Required animation set is incomplete.")
if set(report["shape_keys"]) != {"Basis", "Stuffing75", "Stuffing50", "Stuffing25"}:
    raise SystemExit("Required morphs are missing.")
png = root / "Assets/_Project/Art/Characters/MadamePlush/MadamePlush_Body_Albedo.png"
if struct.unpack(">II", png.read_bytes()[16:24]) != (1024, 1024):
    raise SystemExit("Expected a 1024 atlas.")
for cue, duration in (("PlushCreak", .25), ("PlushWinBell", .6)):
    with wave.open(str(root / ("Assets/_Project/Art/Audio/Resources/Audio/" + cue + ".wav"))) as audio:
        if (audio.getnchannels(), audio.getsampwidth(), audio.getframerate()) != (1, 2, 24000):
            raise SystemExit("Invalid cue format: " + cue)
        if abs(audio.getnframes() / audio.getframerate() - duration) > .001:
            raise SystemExit("Invalid cue duration: " + cue)
print("Madame Plush source/export, 22 clips, stuffing morphs, atlas and original audio evidence passed.")
