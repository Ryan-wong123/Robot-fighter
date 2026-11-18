import importlib.util
import json
import math
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest

spec = importlib.util.spec_from_file_location("voice_pipeline", Path(__file__).with_name("voice_pipeline.py"))
pipeline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pipeline)


class VoicePipelineTests(unittest.TestCase):
    def test_manifest_has_all_fighters_and_deterministic_variants(self):
        manifest = pipeline.read_manifest(pipeline.DEFAULT_MANIFEST)
        self.assertEqual(len(manifest["lines"]), 48)
        self.assertEqual(len(pipeline.plan(manifest)), 96)
        self.assertEqual(pipeline.plan(manifest), pipeline.plan(manifest))
        self.assertEqual({line["fighter"] for line in manifest["lines"]},
                         {"Blobby", "SirNoodle", "PuffinPete", "MadamePlush", "MrJack", "Announcer"})

    def test_invalid_ids_cannot_escape_staging(self):
        manifest = pipeline.read_manifest(pipeline.DEFAULT_MANIFEST)
        manifest["lines"][0]["id"] = "../escape"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "manifest.json"
            path.write_text(json.dumps(manifest))
            with self.assertRaises(ValueError):
                pipeline.read_manifest(path)

    def test_generation_writes_provenance_and_rejects_overwrite(self):
        class FakeModel:
            tts_model = SimpleNamespace(sample_rate=24000)
            def generate(self, **kwargs):
                self.settings = kwargs
                return [.2 * math.sin(index * .1) for index in range(12000)]
        manifest = pipeline.read_manifest(pipeline.DEFAULT_MANIFEST)
        candidate = pipeline.plan(manifest)[0]
        model = FakeModel()
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            wav = pipeline.generate_one(model, candidate, manifest, folder, "a" * 40, "test-backend")
            record = json.loads(wav.with_suffix(".json").read_text())
            self.assertEqual(record["sha256"], pipeline.digest(wav))
            self.assertEqual(record["seed"], candidate["seed"])
            self.assertIsNone(record["reference_audio"])
            self.assertEqual(model.settings["seed"], candidate["seed"])
            self.assertIn(candidate["style"], model.settings["text"])
            with self.assertRaises(FileExistsError):
                pipeline.generate_one(model, candidate, manifest, folder, "a" * 40, "test-backend")

    def test_export_validates_hash_and_generates_mono_mobile_wav(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "source.wav"
            target = Path(directory) / "final.wav"
            pipeline.write_pcm(source, [.2 * math.sin(index * .1) for index in range(24000)], 24000)
            pipeline.write_record(source.with_suffix(".json"),
                                  {"sha256":pipeline.digest(source),"max_seconds":1.5})
            pipeline.export_clip(source, target, pitch=2)
            with pipeline.wave.open(str(target), "rb") as audio:
                self.assertEqual(audio.getnchannels(), 1)
                self.assertEqual(audio.getframerate(), 24000)
                self.assertEqual(audio.getsampwidth(), 2)
            provenance = json.loads(target.with_suffix(".json").read_text())
            self.assertEqual(provenance["sha256"], pipeline.digest(target))
            with self.assertRaises(FileExistsError):
                pipeline.export_clip(source, target)
            pipeline.write_record(source.with_suffix(".json"), {"sha256":"invalid","max_seconds":1.5})
            with self.assertRaises(ValueError):
                pipeline.export_clip(source, Path(directory) / "other.wav")


if __name__ == "__main__":
    unittest.main()
