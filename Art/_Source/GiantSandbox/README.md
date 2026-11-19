# Giant Sandbox

Original procedural Blender scenery and synthesized audio based on the owner's approved [color key](../../../docs/images/maps/concept/giant_sandbox_colorkey.png). Reference provenance remains in [SOURCES](../../../docs/images/maps/concept/SOURCES.md). No external mesh, texture, motion capture or sampled recording was added.

## Source, export and budgets

- `GiantSandbox.blend`: five meshes, one packed 1024 palette atlas, no armature.
- `GiantSandbox.validation.json`: 24,592 total source triangles, below the 26k environment allocation. The backdrop plus one active hazard is rendered; the shared outline adds a second pass. Actual complete-scene draw calls, triangles and performance still require device measurement.
- `Preview.png`: 960 × 540 Blender preview, not a Unity screenshot.
- `provenance.json`: exact binary hashes and LF-normalized generator hashes.
- FBX, atlas and selection thumbnail: `Assets/_Project/Art/GiantSandbox/`.

Packed sand, timber edging, an open red bucket, shovel, rake, blocks, a half-built sandcastle, plastic fence, grass and a muted garden backdrop match the supplied toy-scale reference. Decorative props remain behind the clear 14 m fight plane. The sandcastle's proposed shelter mechanic has not been approved and is not active. `SM_Paw`, `SM_Shadow`, `SM_Whiskers` and `SM_Spiral` are independent reusable meshes, with origin zero and scale one. The cartoon paw has cream fur, round toes and pink pads; no claws or gore. Unity assigns the shared opaque toon Shader Graph and navy outline (0.025 width), with no scenery shadow casting. The single atlas uses ASTC 6x6 on Android. FBX exports Forward -Z / Up Y without animation.

## Starting rules and timing

`GiantSandboxDef` stores half-width 7 m, paw travel endpoints ±8.5 m, sweep half-width 0.95 m, jump clearance 0.85 m and sweep duration 3 s. The shared seeded HazardController waits 15 s, warns for 2 s, activates for 4 s, cools down for 2 s and chooses an 8–14 s idle. Paw and sand alternate without overlap, with paw first each round. Paw entry alternates left/right on successive paw passes. Sand patches cycle through X -3/0/+3 m; only one is active each sand turn. These sequences reset each round; the shared clock's idle timings use its round seed.

The paw warning creeps in as a flat paw shadow with twitching whiskers and an original purr. During activation the paw crosses over 3 s, then remains hidden for the final second. Swept-X contact prevents tunneling. Each fighter can be hit once per pass. Holding block avoids contact entirely, without chip, guard drain or stun. Feet at/above 0.85 m also avoid it. Otherwise a zero-damage HitInfo applies pushback 3.5 m/s and a Knockdown animation, followed by GetUp and the shared 20-frame wake-up protection. The authored stun is 30 simulation frames (about 0.6 s at the current physics step), subject to the shared anti-combo stun cap. The shared health, armor and inflation/pop rules still apply. The optional MoveDef.KnocksDown flag defaults off on existing moves; it does not change their behavior.

The sand patch is a rotating spiral of radius 1.35 m. It warns with a 1.5 Hz scale pulse and a swirl cue; rotation is 90 degrees/s. During activation, fighters with feet within 0.15 m of the floor move at 0.5 times ordinary ground speed inside its X range. Leaving or jumping clear restores normal movement immediately. Jump launch, damage, guard, special meter and incoming knockback are unchanged. Stage movement multipliers are independent of water/conveyor modifiers and reset at round boundaries. Physical edge colliders and shared stage containment retain fighters even during high launches. AI can move away from sand and guard/jump against the paw through IHazardAwareness.

Three item points at X -4.5/0/+4.5 m use the shared deterministic pooled spawner, with one opening pickup and no six-item testing lineup. Round end, sudden death, component disable and Hazards Off hide hazard meshes, stop warning audio and clear slowdown. Every new round starts again with paw after the opening delay. KO camera/slow motion remains the shared presentation.

## Audio and ambient effects

Original mono PCM16 24 kHz `SandMusic` and `SandAmbience` loops are 8 s each. The music uses plucked string harmonics and bongo-like percussion; ambience uses quiet insect/airy tones. `SandPurr` and `SandSwirl` warnings last 2 s, and `SandThump` lasts 0.3 s. No recordings were sampled. Music uses AudioManager's music bus; ambience and cues use SettingsAudioSource's SFX multiplier and pause handling. Twelve low-poly sand motes are capped and restricted to the background. Final audio polish remains M4-09, and coin gating remains M4-08; the stage is available for testing now.

## Unity installation and verification

[GiantSandboxContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/GiantSandboxContent.cs) installs missing content outside Play Mode. It copies CombatSandbox through AssetDatabase, opens the copy additively, authors toon materials, hazard data, bounds, item/audio/ambient wiring, saves, restores the previous active scene and enables the scene in build settings. It does not save the owner's open scene. The completed StageDef resource is published last. Existing completed content is preserved. **Wobble Brawl > Stages > Build Giant Sandbox** runs the same installation. Boot discovers the new metadata and thumbnail without a scene rewrite; stage button widths accommodate the four launch arenas plus CombatSandbox. Direct play of GiantSandbox also works.

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_giant_sandbox.py -- Logs/Rebuild/Sand.blend Logs/Rebuild/Sand.fbx Logs/Rebuild/SandAtlas.png Logs/Rebuild/SandThumbnail.png
blender --factory-startup -b Art/_Source/GiantSandbox/GiantSandbox.blend --python-exit-code 1 --python Tools/Blender/verify_giant_sandbox.py
python Tools/Audio/generate_sandbox.py --output Logs/Rebuild/SandAudio
```

Generators refuse existing outputs. Blender validation passed mesh separation, transforms, UVs, atlas and budget. Standalone .NET checks passed 1,001 bidirectional sweep samples, swept collision, block/jump avoidance and patch contact edges. Runtime/editor/test sources compile against installed Unity libraries. Native Unity import/tests, animation recovery, gameplay and Android remain pending. Run EditMode GiantSandboxTests, BouncyCastleTests, SandboxHazardTests, AppFlowTests and AudioManagerTests, then the [M4-05 manual checklist](../../../docs/prompts.md).
