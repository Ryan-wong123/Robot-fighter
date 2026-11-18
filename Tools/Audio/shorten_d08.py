"""Create shorter D-08 audition edits while preserving original recordings."""
import json
from pathlib import Path
import subprocess

from voice_pipeline import digest, export_clip, write_record


def main():
    root = Path(__file__).resolve().parents[2] / "Art/_Source/Audio/D08"
    # The heavy sample is Blobby's Oof hit reaction, not the Boop attack line.
    edits = [("blobby_attack_light", 1.65, None),
             ("blobby_hit_heavy", 1.0, .40),
             ("mrjack_win", 2.5, 3.05)]
    staging = root / "shortened_sources"
    output = root / "shortened"
    staging.mkdir(exist_ok=True)
    output.mkdir(exist_ok=True)
    for name, speed, end in edits:
        source = root / (name + "_v01.wav")
        target = staging / source.name
        final = output / source.name
        if any(p.exists() for p in (target, target.with_suffix(".json"),
                                   final, final.with_suffix(".json"))):
            raise FileExistsError("Audition already exists: " + name)
        record = json.loads(source.with_suffix(".json").read_text(encoding="utf-8"))
        if digest(source) != record["sha256"]:
            raise ValueError("Original source hash differs: " + name)
        filters = []
        if end is not None:
            filters += [f"atrim=end={end}", "asetpts=PTS-STARTPTS"]
        # atempo preserves pitch; chain factors to stay within 0.5–2 per stage.
        factors = [speed] if speed <= 2 else [2, speed / 2]
        filters += [f"atempo={factor}" for factor in factors]
        fade_start = (end or 1.92) / speed - .025
        filters += ["afade=t=in:d=0.005", f"afade=t=out:st={fade_start}:d=0.025"]
        subprocess.run(["ffmpeg", "-nostdin", "-v", "error", "-n", "-i", str(source),
                        "-af", ",".join(filters), "-c:a", "pcm_s16le", str(target)], check=True)
        record.update({"original_sha256": record["sha256"], "sha256": digest(target),
                       "manual_edit": {"source": source.name, "keep_start_seconds": 0,
                                       "keep_end_seconds": end, "tempo": speed,
                                       "fade_in_seconds": .005, "fade_out_seconds": .025},
                       "review": "audition_required"})
        write_record(target.with_suffix(".json"), record)
        export_clip(target, final)
        print("Exported " + str(final))


if __name__ == "__main__":
    main()
