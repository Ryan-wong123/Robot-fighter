# D-08 voice source verification

Reviewed 19 November 2026.

## License and output use

The upstream repository identifies both code and weights as Apache-2.0 and states they can be used commercially. The model card also declares Apache-2.0. The planned workflow creates invented voices from text descriptions and supplies no reference recordings.

The Apache license covers the distributed code and weights. The reviewed model card has no separate output-ownership grant or output-specific warranty. The project's intended use is commercially generated, pre-rendered short voice assets, based on the upstream commercial-use statement. Keep the model snapshot, package version, seed and license sources in each asset's provenance record. Retain the upstream license and notices if distributing upstream software or weights.

[Code and license statement](https://github.com/OpenBMB/VoxCPM) · [Apache license](https://github.com/OpenBMB/VoxCPM/blob/main/LICENSE) · [Weights model card](https://huggingface.co/openbmb/VoxCPM2)

## Google Play

Google's AI-generated-content policy covers apps that generate content from user prompts and requires those apps to provide reporting for generated offensive content. Wobble Brawl performs generation offline during development and ships fixed audio clips. Applying this distinction, the current game does not expose the prompt-driven generation feature described by that policy. The fixed audio must still comply with the normal Play content, deception, intellectual-property and age-rating rules. Revisit this assessment if generation becomes an in-app feature.

[Google Play AI-generated content policy](https://support.google.com/googleplay/android-developer/answer/13985936)

## Local verification

The machine has an NVIDIA RTX 4070 Laptop GPU with 8 GB VRAM. The official model card estimates approximately 8 GB for inference. The isolated Python 3.11 environment and downloaded snapshot are under the ignored Logs directory. No model weights or environment binaries belong in the game repository.

Pinned weights snapshot: `7936080b41163874f38610f46f438e2c0757f9fd`. Upstream code revision: `300e4260b509066151d623dc9c0aa922cf82232b`. Installed package version: `0.0.post1+g300e4260b`; PyTorch `2.11.0+cu128`. Model load took 63.86 seconds; peak PyTorch allocated GPU memory was 5.86 GB (other GPU allocations are excluded).

Windows setup required the upstream source because the available PyPI 1.5.0 package did not support these weights. The upstream dependency resolver also required torchcodec, which had no Windows wheel. Installing the pinned upstream checkout with `--no-deps` over the resolved PyPI dependencies worked for text-only voice design. Reference-audio loading was not tested. No upstream code was modified. The pipeline disables compilation with `optimize=False`; denoising is disabled.

The reproducible evidence is under [Art/_Source/Audio/D08](../../Art/_Source/Audio/D08/metrics.json): raw WAVs and provenance sidecars, generation timings, and export results. These are audition assets outside Unity, not approved shipping voices.

| Candidate | Raw duration | Generation time | Export result |
|---|---:|---:|---|
| Announcer: Fight! | 0.64 s | 11.34 s | Passed format and duration |
| Sir Noodle: Have at thee! | 1.44 s | 15.15 s | Passed format and duration |
| Blobby: Whee! | 1.92 s | 23.40 s | Rejected: 1.55 s after trimming |
| Blobby: Oof! | 4.48 s | 41.24 s | Rejected: 4.46 s after trimming |
| Mr. Jack: Ha ha! | 11.52 s | 67.59 s | Rejected: 4.30 s after trimming |

The export guard correctly rejected candidates beyond the 1.5-second limit without truncating speech. Spoken phrases are technically viable on this hardware; short grunts and laughter need another candidate or manual editing. These timings are offline development measurements, not mobile runtime costs.

## Remaining owner review

The owner requested shorter Blobby light/heavy samples and Mr. Jack's win laugh. The reviewed heavy sample is `blobby_hit_heavy` (Oof), rather than the separate `blobby_attack_heavy` (Boop) manifest line. [shorten_d08.py](shorten_d08.py) preserves the raw takes and writes documented edits plus normalized exports under `Art/_Source/Audio/D08/shortened`. Light attack uses pitch-preserving 1.65x tempo (0.921 s); heavy hit keeps the first isolated grunt with a fade (0.351 s); the laugh keeps the first 3.05 s and uses pitch-preserving 2.5x tempo (1.141 s). All three passed the existing 1.5-second duration gate and mono 24 kHz PCM export checks. Listening approval of these edits remains pending. No Unity voice integration is claimed.

Owner audition: pending. No listening-quality pass is claimed. Listen to all five raw samples and the two successful exports. Check that the intended words are spoken, descriptions are not read aloud, the voice fits the fighter, and there are no repeated syllables, clicks or unwanted speech. Judge whether the grunt and laugh output is usable after editing or should be regenerated. Record acceptance in `Art/_Source/Audio/D08/review.json` only after an actual owner response. D-08 and M3-09 remain incomplete until sample quality is accepted.

The owner requested a second shortening of Blobby light attack. `shortened/blobby_attack_light_v02.wav` is 0.548 seconds, using an additional pitch-preserving 1.6x tempo pass and a 25 ms end fade. The original and first edit remain available. The second edit passed the existing mono 24 kHz PCM and duration checks; owner listening acceptance remains pending. Its sidecar records the parent hash and edit.
