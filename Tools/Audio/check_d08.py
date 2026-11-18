"""Validate D-08 sample evidence; listening approval is an owner decision."""
import hashlib
import json
from pathlib import Path
import sys
import wave


def main():
    root = Path(__file__).resolve().parents[2] / "Art/_Source/Audio/D08"
    metrics = json.loads((root / "metrics.json").read_text(encoding="utf-8"))
    expected = {"announcer_fight", "sirnoodle_taunt", "blobby_attack_light",
                "blobby_hit_heavy", "mrjack_win"}
    if {clip["id"] for clip in metrics["clips"]} != expected:
        raise ValueError("Missing phrase, grunt or laughter evidence.")
    for clip in metrics["clips"]:
        source = root / (clip["id"] + "_v01.wav")
        record = json.loads(source.with_suffix(".json").read_text(encoding="utf-8"))
        if hashlib.sha256(source.read_bytes()).hexdigest() != record["sha256"]:
            raise ValueError("Sample hash mismatch: " + source.name)
        if record["reference_audio"] is not None:
            raise ValueError("D-08 samples must use invented voices without reference audio.")
        with wave.open(str(source), "rb") as audio:
            duration = audio.getnframes() / audio.getframerate()
            if abs(duration - clip["audio_seconds"]) > .001:
                raise ValueError("Duration differs from measured evidence.")
        if clip["generation_seconds"] <= 0:
            raise ValueError("Missing generation timing.")
    review = json.loads((root / "review.json").read_text(encoding="utf-8"))
    if review["sample_quality_approved"] is not True:
        print("D-08 technical sample evidence passed; owner listening approval is pending.")
        return 3
    print("D-08 sample evidence and recorded owner acceptance passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
