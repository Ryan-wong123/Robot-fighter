"""Generate staged VoxCPM2 voice candidates and export reviewed Unity audio."""
import argparse
import array
from datetime import datetime, timezone
import hashlib
import importlib.metadata
import json
import math
from pathlib import Path
import re
import shutil
import subprocess
import sys
import wave

DEFAULT_MANIFEST = Path(__file__).with_name("voice_lines.json")


def read_manifest(path):
    manifest = json.loads(Path(path).read_text(encoding="utf-8"))
    if manifest.get("schema_version") != 1 or not manifest.get("lines"):
        raise ValueError("Expected a schema_version 1 manifest with lines.")
    seen = set()
    for line in manifest["lines"]:
        key = line["id"]
        if not re.fullmatch(r"[a-z][a-z0-9_]*", key) or key in seen:
            raise ValueError("Invalid or duplicate line id: " + key)
        seen.add(key)
        if not all(isinstance(line.get(field), str) and line[field].strip()
                   for field in ("fighter", "event", "text", "style")):
            raise ValueError("Each line needs fighter, event, text and style.")
        if not 1 <= line["variants"] <= 8 or not .2 <= line["max_seconds"] <= 5:
            raise ValueError("Invalid variant count or duration: " + key)
    return manifest


def plan(manifest, selected=None, seed=42):
    candidates = []
    for number, line in enumerate(manifest["lines"]):
        if selected and line["id"] not in selected:
            continue
        for variant in range(line["variants"]):
            candidates.append({**line, "candidate": f'{line["id"]}_v{variant + 1:02}',
                               "seed": seed + number * 100 + variant})
    if not candidates:
        raise ValueError("No matching voice lines.")
    return candidates


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write_record(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def write_pcm(path, samples, sample_rate):
    pcm = array.array("h")
    for sample in samples:
        value = float(sample)
        if not math.isfinite(value):
            raise ValueError("Generated audio contains non-finite samples.")
        pcm.append(round(max(-1, min(1, value)) * 32767))
    if not pcm or max(abs(value) for value in pcm) < 20:
        raise ValueError("Generated audio is empty or silent.")
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(path), "wb") as audio:
        audio.setparams((1, 2, sample_rate, 0, "NONE", "not compressed"))
        audio.writeframes(pcm.tobytes())


def generate_one(model, candidate, manifest, output, revision, package):
    stem = output / candidate["candidate"]
    wav = stem.with_suffix(".wav")
    record = stem.with_suffix(".json")
    if wav.exists() or record.exists():
        raise FileExistsError("Choose a new staging directory: " + str(stem))
    text = "(" + candidate["style"] + ")" + candidate["text"]
    samples = model.generate(text=text, cfg_value=2.0, inference_timesteps=10, seed=candidate["seed"])
    # The documented API returns a mono waveform.
    samples = samples.reshape(-1) if hasattr(samples, "reshape") else samples
    temporary = stem.with_suffix(".tmp.wav")
    try:
        write_pcm(temporary, samples, model.tts_model.sample_rate)
        temporary.replace(wav)
        write_record(record, {**candidate, "model": manifest["model"], "model_revision": revision,
            "package_version": package, "generation": {"cfg_value":2.0,"inference_timesteps":10},
            "generated_utc": datetime.now(timezone.utc).isoformat(),
            "code_license": manifest["code_license"], "weights_license": manifest["weights_license"],
            "license_source": manifest["license_source"], "weights_source": manifest["weights_source"],
            "reference_audio": None, "license_note": "Voice design; no reference recording. D-08 review remains open.",
            "review": "pending", "sha256": digest(wav)})
    finally:
        temporary.unlink(missing_ok=True)
    return wav


def export_clip(source, target, pitch=0, ffmpeg="ffmpeg"):
    source, target = Path(source), Path(target)
    if target.suffix.lower() != ".wav":
        raise ValueError("Export destination must end in .wav.")
    record_path = source.with_suffix(".json")
    record = json.loads(record_path.read_text(encoding="utf-8"))
    if record.get("sha256") != digest(source):
        raise ValueError("Source differs from its generation record.")
    if target.exists() or target.with_suffix(".json").exists():
        raise FileExistsError(target)
    if not -12 <= pitch <= 12:
        raise ValueError("Pitch must be between -12 and 12 semitones.")
    executable = shutil.which(ffmpeg)
    if not executable:
        raise RuntimeError("Install ffmpeg and add it to PATH.")
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(target.stem + ".tmp.wav")
    if temporary.exists():
        raise FileExistsError(temporary)
    ratio = 2 ** (pitch / 12)
    filters = ["silenceremove=start_periods=1:start_duration=0.01:start_threshold=-45dB",
               "areverse", "silenceremove=start_periods=1:start_duration=0.01:start_threshold=-45dB", "areverse"]
    if pitch:
        filters += ["aresample=48000", f"asetrate={48000 * ratio}", f"atempo={1 / ratio}"]
    filters += ["loudnorm=I=-18:TP=-1.5:LRA=7", "aresample=24000"]
    command = [executable, "-nostdin", "-v", "error", "-n", "-i", str(source),
               "-af", ",".join(filters), "-ac", "1", "-c:a", "pcm_s16le", str(temporary)]
    try:
        subprocess.run(command, check=True, capture_output=True, text=True)
        with wave.open(str(temporary), "rb") as audio:
            seconds = audio.getnframes() / audio.getframerate()
            if audio.getnchannels() != 1 or audio.getsampwidth() != 2 or not .2 <= seconds <= record["max_seconds"]:
                raise ValueError(f"Export duration {seconds:.2f}s needs manual editing before import.")
        temporary.replace(target)
        record.update({"review":"audition_required", "source_sha256":record["sha256"],
                       "sha256":digest(target), "edit":{"pitch_semitones":pitch,"sample_rate":24000,
                       "channels":1,"normalization_lufs":-18,"trim_threshold_db":-45},
                       "exported_utc":datetime.now(timezone.utc).isoformat()})
        write_record(target.with_suffix(".json"), record)
    finally:
        temporary.unlink(missing_ok=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("plan", "generate"):
        sub = commands.add_parser(name)
        sub.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
        sub.add_argument("--line", action="append")
        sub.add_argument("--seed", type=int, default=42)
        if name == "generate":
            sub.add_argument("--model-dir", type=Path, required=True)
            sub.add_argument("--revision", required=True, help="Downloaded model snapshot commit SHA.")
            sub.add_argument("--output", type=Path, required=True)
    export = commands.add_parser("export")
    export.add_argument("source", type=Path)
    export.add_argument("target", type=Path)
    export.add_argument("--pitch", type=float, default=0)
    args = parser.parse_args()
    if args.command == "export":
        export_clip(args.source, args.target, args.pitch)
        return
    manifest = read_manifest(args.manifest)
    candidates = plan(manifest, args.line, args.seed)
    if args.command == "plan":
        print(json.dumps(candidates, indent=2))
        return
    if not args.model_dir.is_dir():
        raise FileNotFoundError("Download the model snapshot first: " + str(args.model_dir))
    if not re.fullmatch(r"[a-fA-F0-9]{40}", args.revision):
        raise ValueError("Record the full model snapshot commit SHA.")
    # Keep inference dependencies optional for planning and export.
    from voxcpm import VoxCPM
    package = importlib.metadata.version("voxcpm")
    args.output.mkdir(parents=True, exist_ok=True)
    model = VoxCPM.from_pretrained(str(args.model_dir.resolve()), load_denoiser=False)
    for candidate in candidates:
        print(generate_one(model, candidate, manifest, args.output, args.revision, package))


if __name__ == "__main__":
    main()
