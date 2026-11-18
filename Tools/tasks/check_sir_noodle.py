"""Verify the recorded Blender evidence and exact exported files for M4-01."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
folder = root / "Art/_Source/SirNoodle"
report = json.loads((folder / "SirNoodle.validation.json").read_text(encoding="utf-8"))
provenance = json.loads((folder / "provenance.json").read_text(encoding="utf-8"))
for path, expected in provenance["files"].items():
    if hashlib.sha256((root / path).read_bytes()).hexdigest() != expected:
        raise SystemExit("Source/export differs from verified snapshot: " + path)
if report["validation_errors"] or report["triangles"] > 12000 or report["deform_bones"] > 30:
    raise SystemExit("Blender skin or budget checks failed.")
if report["materials"] != 1 or not 1.95 <= report["height_metres"] <= 2.05:
    raise SystemExit("Unexpected material count or height.")
required = {"Idle", "Walk", "Block", "JumpRise", "JumpFall", "JumpLand", "Punch1", "Punch2",
            "Punch3", "Punch4", "Kick1", "Kick2", "Special", "Throw", "HitLight", "HitHeavy",
            "Knockdown", "GetUp", "GuardBreak", "KO", "Win", "Taunt"}
if set(report["actions"]) != {"SirNoodle_" + name for name in required}:
    raise SystemExit("The exported action set is incomplete.")
print("Sir Noodle verified Blender snapshot and complete 22-action export passed.")
