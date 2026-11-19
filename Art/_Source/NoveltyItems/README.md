# Additional six launch items

Original Blender geometry and atlas authored from the owner's approved item descriptions on 2026-11-19. No third-party meshes, textures or sampled recordings. This source supplements the original six pickups without overwriting their sources or assets. [Item rules and data](../../../docs/items.md) are authoritative for gameplay.

`NoveltyItems.blend` contains nine static meshes and one packed 1024 palette atlas. Six FBXs and the atlas are exported to `Assets/_Project/Art/Items/Novelty/`. `NoveltyItems.validation.json` records 8,696 total triangles: Cream Pie 1,848; Boomerang 1,168; Pogo Stick 1,276; Bubble Wand plus bubble 812; Wind-up Car 1,360; Boxing Jack/base/action/warning 2,232. Each is below its 4k allocation. `Preview.png` is a Blender lineup, not a Unity screenshot. `provenance.json` hashes exact binaries and LF-normalized generators.

All objects have applied rotation/scale, zero-centered pivots, finite geometry, UVs and one material. No rig or baked animation; runtime motion reuses the pooled mesh transforms. FBX uses Forward -Z / Up Y. Unity assigns the shared opaque toon Shader Graph, atlas and outline (0.012 width), with ASTC 6x6 on Android. The bubble alone uses the existing transparent toon water surface at cyan alpha 0.22, keeping its target visible.

The six silhouettes are a cream/cherry pie, a yellow/blue V boomerang, a spring pogo, a blue/pink bubble wand, a red wind-up car and a pink/yellow spring boxing jack. Separate bubble and jack action/warning meshes are reused during deployment and reset on recycling. Six short oscillator/noise SFX use the existing AudioManager pool and SFX volume bus. No additional audio sources are spawned during combat.

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_novelty_items.py -- Logs/Rebuild/Items.blend Logs/Rebuild/Exports Logs/Rebuild/Atlas.png Logs/Rebuild/Preview.png
blender --factory-startup -b Art/_Source/NoveltyItems/NoveltyItems.blend --python-exit-code 1 --python Tools/Blender/verify_novelty_items.py
python Tools/Audio/generate_novelty_cues.py --output Logs/Rebuild/ItemAudio
```

Generators refuse existing outputs. Source validation passed transforms, pivots, UVs, packed atlas and budgets. All six FBX round-trips retained their mesh splits and UVs. Standalone .NET status checks passed exact slow/trap durations, refresh, hit break and reset. Runtime/editor/test sources compile against installed Unity libraries. Native Editor execution, contacts, trajectory timing, model facing, reset and Android checks are pending; run NoveltyItemTests, CombatItemTests, MatchRulesTests and AudioManagerTests, plus [the manual M4-06 checklist](../../../docs/prompts.md).

[NoveltyItemsContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/NoveltyItemsContent.cs) automatically authors missing data/prefabs/materials and a separate six-item NewItemSandbox fixture, then publishes the LaunchItems resource catalog. Existing completed content is preserved. The fixture has no stage hazard, uses the new items in their approved order and supports the existing Space AI toggle. Restart Play Mode after import to let spawners discover the six new entries. Normal authored six-item tables then offer all twelve; the fixture's table already contains the additions and is deduplicated.
