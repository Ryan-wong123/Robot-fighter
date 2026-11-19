# Placeholder audio

The base placeholder WAV files under Assets/_Project/Art/Audio/Resources/Audio are created by Tools/Audio/generate_placeholders.py. They use mathematical oscillators and seeded noise, with no sampled recordings or external libraries. PetePop.wav uses the separate generator described below. No third-party sample license is required.

PetePop.wav is the original 0.8-second mono 24 kHz air-pop/hiss cue created by `python Tools/Audio/generate_pete_pop.py`. It uses seeded noise and an oscillator without samples; its source/export hashes are recorded in [Puffin Pete provenance](../../Art/_Source/PuffinPete/provenance.json). The generator refuses an existing output. It accompanies the M4-02 pop mechanic; final audio polish remains M4-09.

Run the generator from the repository root with Python. The four loops provide menu, bathtub, kitchen, and sandbox themes. The other files provide button, impact, block, guard-break, special, KO, round-start, and win cues. M4-09 replaces or polishes these placeholders. Fighter voice generation remains M3-09.

## Fighter voice pipeline

The manifest voice_lines.json contains 48 event lines across five fighters and the announcer, with two candidate variants per line. These are audition scripts. Blobby and Pete's nonverbal sounds need particular attention during listening.

### Setup and generation

Use a separate Python 3.10–3.12 environment. Install voxcpm using the upstream installation guide and download a VoxCPM2 model snapshot to a local directory outside Assets. Record its full commit SHA. The generator loads that directory and records the installed package version. It uses voice descriptions with no reference audio and disables model compilation for the verified Windows configuration. The tested upstream revision and dependency workaround are recorded in [D08_REVIEW.md](D08_REVIEW.md).

Inspect a plan without loading the model.

~~~powershell
python Tools/Audio/voice_pipeline.py plan --line blobby_attack_light
~~~

Generate into a fresh staging directory. Replace the model path and revision with the downloaded snapshot.

~~~powershell
python Tools/Audio/voice_pipeline.py generate --model-dir C:/Models/VoxCPM2 --revision "<40-character-snapshot-SHA>" --line blobby_attack_light --output Logs/VoiceStaging/audition-01
~~~

Each WAV has a JSON record with line, actor, event, style, seed, model snapshot, package version, parameters, date, SHA-256, and code/weights license sources. The script refuses existing outputs. Keep each record beside its WAV.

### Edit and export

Listen to several candidates before choosing one. Check intelligibility, character fit, spoken stage directions, clicks, noise and clipped syllables. Short grunts and giggles can fail even when spoken phrases work. Use Audacity for manual trim, formant processing or character effects. Preserve the original candidate and record manual edits in the final sidecar.

The export command requires ffmpeg. It trims silence at both ends, optionally changes pitch while preserving timing, normalizes to -18 LUFS with a -1.5 dB true-peak target, and writes 24 kHz mono 16-bit PCM. A duration outside the line's allowed range fails instead of cutting the spoken line.

~~~powershell
python Tools/Audio/voice_pipeline.py export Logs/VoiceStaging/audition-01/blobby_attack_light_v01.wav Logs/VoiceReviewed/blobby_attack_light.wav --pitch 2
~~~

Listen again after export. Set the final record's review field to approved only after listening. For a manual edit, update the source hash and add an edit description, keeping the original generation record for provenance.

The owner's D-08 shortening request is reproducible with `python Tools/Audio/shorten_d08.py` into fresh `shortened_sources` and `shortened` directories under `Art/_Source/Audio/D08`. It preserves the original candidates and refuses to overwrite existing edits. The final five D-08 audition exports have owner listening approval; see the review record for exact files.

Copy approved WAVs and sidecars to Assets/_Project/Art/Audio/Voices/<fighter>/. In Unity set Force To Mono, Preload Audio Data, Decompress On Load, and Vorbis quality around 60 as a starting point. Check build size against the approximately 5 MB launch voice budget. M4-09 connects final voices to events and handles throttling and announcer priority.

### Source and review status

The code and weights license, commercial-use statement and Google Play policy were reviewed, and five real samples were generated on the local RTX 4070 Laptop GPU. Two original phrase exports and three shortened vocalizations passed export limits and received owner listening approval. See [D08_REVIEW.md](D08_REVIEW.md) for pinned versions, Windows setup limitations, timings, sources and recorded owner acceptance. The remaining manifest lines still require generation and review in the content pass. The unit tests separately verify the file workflow with a synthetic backend and ffmpeg.

[Upstream installation](https://github.com/OpenBMB/VoxCPM) · [Model card](https://huggingface.co/openbmb/VoxCPM2) · [License](https://github.com/OpenBMB/VoxCPM/blob/main/LICENSE)

Run tests with python -m unittest discover -s Tools/Audio -p test_voice_pipeline.py.

## Madame Plush cues

`python Tools/Audio/generate_plush_cues.py --output Logs/PlushAudio` writes a 0.25 s cloth creak and 0.6 s sweet winner bell into a fresh directory. The deterministic generator uses noise/oscillators and no external recording. Committed mono PCM16 24 kHz exports are `PlushCreak.wav` and `PlushWinBell.wav` under `Assets/_Project/Art/Audio/Resources/Audio/`. [Source provenance](../../Art/_Source/MadamePlush/provenance.json) records exact hashes. Both route through SettingsAudioSource SFX volume; winner audio follows the actual winner event. Final voices remain M4-09.

## Bouncy Castle Fair cues

`python Tools/Audio/generate_fairground.py --output Logs/FairAudioNew` writes original FairMusic/FairAmbience loops (8 s each), FairPadWarning/FairPump warnings (2 s each) and FairBoing (0.65 s) into fresh outputs. Synthesized from oscillators/noise with a fixed seed, with no sampled recordings. Mono PCM16 24 kHz exports live under `Assets/_Project/Art/Audio/Resources/Audio/`; [provenance](../../Art/_Source/BouncyCastleFair/provenance.json) records hashes. Music routes through AudioManager's music bus; ambience/warnings/boings respect SFX settings and pause. Final audio polish remains M4-09.
