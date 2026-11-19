# Bouncy Castle Fair

Original Blender environment and synthesized audio based on the owner's approved [color key](../../../docs/images/maps/concept/bouncy_castle_fair_colorkey.png). Reference provenance remains in [SOURCES](../../../docs/images/maps/concept/SOURCES.md). No external mesh, texture or sampled recording was added.

## Assets and budgets

- `BouncyCastleFair.blend`: three meshes, one packed 1024 palette atlas, no armature.
- `BouncyCastleFair.validation.json`: 21,160 source triangles; two independent moving wall meshes and one combined backdrop. This is below the 65k environment allocation and leaves room within the 100k visible-stage target for the fighters and effects.
- `Preview.png`: 16:9 Blender render, not a Unity screenshot.
- `provenance.json`: exact binary hashes and LF-normalized generator hashes.
- FBX, palette atlas and selection thumbnail live in `Assets/_Project/Art/BouncyCastleFair/`.

The set includes inflated floor ribs, primary-color striped towers and side rails, a rainbow arch, bunting flags, party bulbs, balloons, a ticket booth and Ferris wheel against a muted dusk backdrop. The center fight plane stays clear. `SM_LeftWall` and `SM_RightWall` have origins at X -7.5/+7.5 m; physics walls are 1 m thick, giving inner bounds -7/+7 m. The source coordinates are authored from Unity-space dimensions; FBX uses Forward -Z / Up Y and no animation. The palette has no baked lighting. Unity assigns the existing shared toon Shader Graph and tinted outline (0.025 width), with shadow casting disabled on scenery and ASTC 6x6 textures on Android.

## Starting gameplay data

`FairgroundDef` owns half-width 7 m, width reduction 25%, ramp fraction 0.25, pad centers -3/+3 m, pad half-width 1 m, contact height 0.15 m and jump launch multiplier 1.8. Three item points are X -3/0/+3 m, height 0.65 m. They remain accessible at the narrowest width. The template's six-item opening lineup is disabled; the shared seeded pooled spawner remains enabled on its existing interval, with one opening pickup.

The shared seeded HazardController waits 15 s before the first two-second warning, activates for four seconds, cools down for two seconds, then chooses an 8–14 s idle interval. Pads and Wall Squeeze alternate without overlap, starting with pads each round. Sudden death, round end, disable and Hazards Off stop warnings/audio, remove boosts and restore walls.

Pads show upward arrows with a 1.5 Hz scale pulse and a chime. Standing/walking/landing on an active pad automatically launches at 1.8 times the character's normal jump speed; pressing jump on activation receives the same boost. Upward velocity prevents repeat firing while ascending. Airborne fighters above the contact band receive no extra boost. Blocking inflation is released when bounced. Hits/attacks retain their normal rules. A short original boing plays per bounce.

Wall Squeeze warns with inward arrows, a five-percent visual bulge pulse and a pump/creak cue. During four seconds of activation, inner bounds move from ±7 to ±5.25 over the first second, hold for two seconds, then expand over the final second. Kinematic wall colliders and per-fighter bounds constrain both sides, including high jumps and hit-stop; neither hazard deals damage. FairgroundCamera fits both fighters vertically during boosted jumps, narrows framing with wall bounds, and yields to the existing KO camera when a round ends. Normal full width returns next round.

`FairMusic.wav` is an original eight-second carnival loop routed through AudioManager's music bus. `FairAmbience.wav` is an eight-second background loop; warnings, ambience and boings respect SFX volume and pause. All cues are mono PCM16 24 kHz, generated without recordings. Broader final audio polish remains M4-09; coin unlock gating remains M4-08. The arena is available during content testing.

## Unity installation

[BouncyCastleContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/BouncyCastleContent.cs) automatically installs missing content outside Play Mode. It copies CombatSandbox through AssetDatabase, opens the copy additively, authors tuning/hazard/physics/camera/audio/item wiring, saves it and restores the previously active scene. It does not save or replace the owner's open scene. It registers the arena in build settings and publishes `Resources/Stages/BouncyCastleFair.asset` last. Existing completed content is preserved. **Wobble Brawl > Stages > Build Bouncy Castle Fair** performs the same installation if needed. Boot discovers installed StageDef entries, avoids duplicate scene entries, and shows the new thumbnail and hazard hint. Direct play of the generated BouncyCastleFair scene is also supported.

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_bouncy_castle.py -- Logs/Rebuild/Fair.blend Logs/Rebuild/Fair.fbx Logs/Rebuild/FairAtlas.png Logs/Rebuild/FairThumbnail.png
blender --factory-startup -b Art/_Source/BouncyCastleFair/BouncyCastleFair.blend --python-exit-code 1 --python Tools/Blender/verify_bouncy_castle.py
python Tools/Audio/generate_fairground.py --output Logs/Rebuild/FairAudio
```

Generators refuse existing outputs. Blender validation passed the mesh split, origins, transforms, UVs and budget. Standalone .NET checks passed 1,001 squeeze-envelope samples, width limits, symmetry, restored endpoints and pad contact edges. Runtime/editor/test code compiles against the installed Unity libraries. Native Unity import and test execution, contacts, warnings, framing and Android performance still need verification using EditMode BouncyCastleTests, SandboxHazardTests, AppFlowTests and AudioManagerTests and the [M4-04 checklist](../../../docs/prompts.md).
