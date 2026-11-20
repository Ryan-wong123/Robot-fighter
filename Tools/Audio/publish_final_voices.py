"""Stage verified VoxCPM2 clips and preserve their source and edit records."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import wave

from voice_pipeline import digest, export_clip

APPROVED = {
    "announcer_fight_v01": "exports/announcer_fight_v01.wav",
    "sirnoodle_taunt_v01": "exports/sirnoodle_taunt_v01.wav",
    "blobby_attack_light_v02": "shortened/blobby_attack_light_v02.wav",
    "blobby_hit_heavy_v01": "shortened/blobby_hit_heavy_v01.wav",
    "mrjack_win_v01": "shortened/mrjack_win_v01.wav",
}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("staging", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    records = {}
    for key, relative in APPROVED.items():
        source = Path("Art/_Source/Audio/D08") / relative
        target = args.output / (key + ".wav")
        shutil.copyfile(source, target)
        record = json.loads(source.with_suffix(".json").read_text(encoding="utf-8"))
        records[key] = {**record, "source": source.as_posix(), "sha256": digest(target),
                        "review": "owner-approved D-08 audition"}

    for folder in args.staging:
        for raw in sorted(folder.glob("*.wav")):
            if raw.stem in records: continue
            target = args.output / raw.name
            source = json.loads(raw.with_suffix(".json").read_text(encoding="utf-8"))
            if digest(raw) != source["sha256"]:
                raise ValueError(f"Source hash mismatch: {raw}")
            with tempfile.TemporaryDirectory() as temporary:
                temp = Path(temporary) / raw.name
                try:
                    export_clip(raw, temp)
                    record = json.loads(temp.with_suffix(".json").read_text(encoding="utf-8"))
                except ValueError:
                    # Try a pitch-preserving tempo edit, never truncate a phrase.
                    filters = ["silenceremove=start_periods=1:start_duration=0.01:start_threshold=-45dB",
                               "areverse", "silenceremove=start_periods=1:start_duration=0.01:start_threshold=-45dB",
                               "areverse", "atempo=1.5", "loudnorm=I=-18:TP=-1.5:LRA=7", "aresample=24000"]
                    subprocess.run(["ffmpeg", "-nostdin", "-y", "-v", "error", "-i", str(raw),
                                    "-af", ",".join(filters), "-ac", "1", "-c:a", "pcm_s16le", str(temp)],
                                   check=True)
                    record = {**source, "source_sha256": source["sha256"],
                              "edit": {"tempo": 1.5, "sample_rate": 24000, "channels": 1,
                                       "normalization_lufs": -18, "trim_threshold_db": -45}}
                with wave.open(str(temp), "rb") as clip:
                    duration = clip.getnframes() / clip.getframerate()
                if not .2 <= duration <= source["max_seconds"]:
                    print(f"Skipped {raw.name}: {duration:.2f} seconds after edit")
                    continue
                shutil.copyfile(temp, target)
                records[raw.stem] = {**record, "source": "VoxCPM2 text-only generation from Tools/Audio/voice_lines.json",
                                     "sha256": digest(target), "review": "audition_required"}

    groups = {}
    for key in records:
        groups.setdefault(key.rsplit("_v", 1)[0], []).append(key)
    for group, takes in sorted(groups.items()):
        if len(takes) != 1:
            continue
        original = takes[0]
        variant = group + "_v03"
        source = args.output / (original + ".wav")
        target = args.output / (variant + ".wav")
        # Supply an edited alternative when only one generated take passed the length gate.
        subprocess.run(["ffmpeg", "-nostdin", "-y", "-v", "error", "-i", str(source),
                        "-af", "asetrate=25200,aresample=24000,atempo=0.952381,alimiter=limit=0.84:level=0",
                        "-c:a", "pcm_s16le", str(target)], check=True)
        with wave.open(str(target), "rb") as clip:
            seconds = clip.getnframes() / clip.getframerate()
        if not .2 <= seconds <= 1.5:
            target.unlink()
            continue
        records[variant] = {**records[original], "source": original + ".wav",
                            "derived_from": original, "source_sha256": digest(source),
                            "variant_edit": "5% pitch increase with tempo compensation",
                            "sha256": digest(target), "review": "audition_required"}

    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(records, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"Staged {len(records)} clips with provenance")

if __name__ == "__main__":
    main()
