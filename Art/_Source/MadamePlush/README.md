# Madame Plush

Original procedural geometry, atlas, animation and synthesized audio, based on the owner's approved [fighter sheet](../../../docs/characters.md#44-madame-plush). No external mesh, texture, motion capture or audio samples were added. Reference provenance remains in [concept SOURCES](../../../docs/images/characters/concept/SOURCES.md).

## Source and measured budgets

- `MadamePlush.blend`: packed 1024 atlas, one skinned mesh, one armature, 22 actions and three independent stuffing shape keys.
- `MadamePlush.validation.json`: 11,928 triangles, 17 bones (16 deform), one source material, height 1.8 m.
- `provenance.json`: exact validated source/export and original audio hashes.
- `Poses.png`: Blender previews, not a Unity render.
- FBX and atlas: `Assets/_Project/Art/Characters/MadamePlush/`.

Brown woven cloth, tan muzzle/belly, stitched patches, crossed button eyes, red boxing gloves and sash follow the supplied sheet. The shared Root/Pelvis/Spine/Chest/Head and limb rig uses normalized weights with at most two influences. Twelve standard bones receive visual physics. Generic import, root motion disabled, shared URP toon graph and brown outline (width 0.012), ASTC 6x6 atlas on Android. The item socket is parented to Hand.L and placed outside the glove.

## Actions and starting data

All 22 actions: Idle, Walk, Block, JumpRise, JumpFall, JumpLand, Punch1–4, Kick1–2, Special, Throw, HitLight, HitHeavy, GuardBreak, Knockdown, GetUp, KO, Win and Taunt. Idle, Walk, Block and JumpFall close cleanly. Heavy hooks/crosses use weight shifts and recovery; kicks are short stomps. Block raises both gloves. Taunt fixes stitches then cracks knuckles; Win holds the sash then hugs forward. Pose playback follows gameplay frames. Source authoring is 60 FPS; the current simulation step is about 50 Hz.

HP 240, guard 100, special capacity 100, speed 2.4, jump 9. Forward normal reach 1.305 m (0.9 times Blobby). Punches deal 12 body / 6 leg damage, kicks 14 / 7. Punch startup/active/recovery frames: 9/4/19, 12/4/22, 14/4/24, 18/4/28. Kicks: 14/4/24 and 18/4/28. Partial-ragdoll reaction scale 0.85, duration 0.4 s.

At health 180/120/60 or below, a separate seam opens and cotton emerges through Stuffing75/50/25 morphs. Reveals persist through healing and KO until round reset. A newly crossed threshold emits one bounded 16-particle, 0.6 s cotton burst and a 0.25 s cloth creak; skipping thresholds reveals all crossed seams but emits only one burst. Copied Unity clips omit exported rest blend-shape curves so they cannot overwrite health-driven seams. Morphs update after fighter pose sampling, blend at 500 weight units/s and pause during hit-stop. No gore. Round reset restores seams and clears feedback. The winner plays an original 0.6 s bell through the SFX volume setting.

While alive at or below 60 HP, melee/special damage gains 10%, rounded to nearest integer before combo/block resolution. The HUD shows **TOUGH!**. Healing above that threshold removes the bonus; items and hazards do not inherit it. Base special body damage 24 becomes 26.

Stitch Uppercut spends the full gauge: 24/5/35 startup/active/recovery frames, one 24 body / 12 leg hit, guard cost 25, stun 18, hit-stop 8, horizontal pushback 8 m/s and unblocked vertical launch 7 m/s. Hitbox size 1.35 × 1.5 × 1 m, forward 0.63 / height 1.15 m. This is the largest fighter special knockback. Surviving launch victims are contained at X ±6.2 m for 90 simulation frames so they remain on the current 14 m stages; KO ragdolls may fly out. Block and inflated armor retain shared defense rules. Final voice/audio polish remains M4-09; coin unlock gating remains M4-08.

## Unity installation and verification

[MadamePlushContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/MadamePlushContent.cs) automatically authors missing clips, linked preview controller, materials, visual/Fighter prefabs, effects, moves and `Resources/Characters/MadamePlush.asset` after import outside Play Mode. Completed content is preserved. **Wobble Brawl → Characters → Build Madame Plush** explicitly rebuilds defaults; **Repair Madame Plush Colors** repairs an empty atlas binding. No scenes are rewritten.

Boot → Quick Fight offers all four playable fighters, with a two-column fighter menu. AI selection cycles all four and Rematch retains choices. Direct arena play keeps the existing Blobby setup.

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_madame_plush.py -- Logs/Rebuild/MadamePlush.blend Logs/Rebuild/MadamePlush.fbx Logs/Rebuild/MadamePlush_Body_Albedo.png
blender --factory-startup -b Art/_Source/MadamePlush/MadamePlush.blend --python-exit-code 1 --python Tools/Blender/verify_madame_plush.py
```

The builder refuses existing outputs. Blender validation passed skin/budgets, 22 moving actions, four closed loops, independent morphs and uppercut markers. Standalone .NET checks passed all 241 health values, threshold boundaries and damage rounding. Runtime/editor/test sources compile against the installed Unity libraries. Native Unity import execution, tests, gameplay and Android remain pending: run EditMode `MadamePlushTests`, `PuffinPeteTests`, `SirNoodleTests` and `AppFlowTests`, then the [manual M4-03 checklist](../../../docs/prompts.md).
