# Sir Noodle

Original procedural Blender model, atlas and animation authored for Wobble Brawl from the owner's approved [turnaround and poses](../../../docs/characters.md#42-sir-noodle). No external meshes, textures or motion capture were added. Reference provenance remains in the existing [concept SOURCES](../../../docs/images/characters/concept/SOURCES.md).

## Delivered source and export

- `SirNoodle.blend`: packed atlas, one armature, skinned mesh and 22 actions.
- `SirNoodle.validation.json`: measured mesh/rig budget and action inventory.
- `provenance.json`: exact script, blend, FBX and atlas hashes for the verified snapshot.
- `Poses.png`: Blender pose contact sheet, not a Unity render.
- Unity FBX and 1024 atlas: `Assets/_Project/Art/Characters/SirNoodle/`.

Measured: 6,616 triangles, one material, 25 bones (24 deform), normalized weights with at most two influences, 1.985 m source height. Unity normalizes the visual to 2.0 m. Geometry includes alternating foam segments and ring grooves, eyes/brows/mouth, dented pot shell and handles, plain cardboard shield, and a blunt flattened yellow foam blade with crossguard.

## Rig deviation and physics

Each arm/leg has four deform segments: two standard bones plus two follow bones. Hand/foot bones finish the chain. This gives bendy noodles within the 30-bone budget. Root/Pelvis/Spine/Chest/Head retain the shared hierarchy. Twelve bones drive visual physics: Pelvis, Spine, Chest, Head and both upper/lower arms/legs. The eight intermediate bend bones follow their parents and receive no rigid bodies. Root motion is disabled. The left hand carries the pickup socket; the shield is bound to the left forearm and sword to the right hand.

## Animations and timing

Actions are authored at 60 FPS, in place, with all rig channels keyed. Idle, Walk, Block and JumpFall close at their endpoints. The shared runtime samples the actual Sir Noodle clips and synchronizes attacks to move frames rather than assuming the Animator controls gameplay.

All 22 actions: Idle, Walk, JumpRise, JumpFall, JumpLand, Block, Punch1, Punch2, Punch3, Punch4, Kick1, Kick2, Special, Throw, HitLight, HitHeavy, GuardBreak, Knockdown, GetUp, KO, Win, Taunt. Four punches progress through a whip jab, cross, rising strike and foam-sword lunge. Kicks are a forward poke and sweeping kick. Special spins upright with arms extended, followed by a vulnerable stumble. T previews the bow/flourish taunt during the fight. KO presentation uses full physics; the KO action also remains available for clip review.

| Move | Startup / active / recovery frames | Body / leg damage |
|---|---|---|
| Punch1 | 7 / 4 / 15 | 9 / 5 |
| Punch2 | 9 / 4 / 15 | 9 / 5 |
| Punch3 | 10 / 4 / 17 | 9 / 5 |
| Punch4 | 13 / 4 / 21 | 9 / 5 |
| Kick1 | 10 / 4 / 18 | 11 / 5 |
| Kick2 | 13 / 4 / 21 | 11 / 5 |
| Helicopter Joust | 12 / 24 / 30 | 7 / 5 per pulse |

Joust has four six-frame windows, alternating front/back/front/back: two chances per side, at most one successful hit per window. It costs a full gauge. Its pushback is 0.25 to keep the second pass in reach, with 7 stun frames and 3 hit-stop frames per landed pulse. Normal hitbox forward extent is 2.03 m, exactly 1.4 times Blobby's current 1.45 m extent. Reach is baked into move assets; damage approximates x0.9 with integer rounding. HP 170, guard 60, speed 3.2, jump 10. Partial reaction strength is 1.25 and duration 0.6 s. Other meter/guard rules remain shared.

## Unity installation

[SirNoodleContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/SirNoodleContent.cs) runs once after import if content is missing. It uses Unity APIs to create the Generic avatar settings, 22 standalone clips, linked preview controller, toon/outline materials, visual prefab, shared Fighter prefab, move assets and `Resources/Characters/SirNoodle.asset`. It preserves existing asset GUIDs when the explicit **Wobble Brawl → Characters → Build Sir Noodle** command is used again. Automatic installation skips existing completed content so it does not overwrite owner edits. Rebuilding explicitly restores the authored defaults for generated clips, moves and prefabs.

The surface uses the existing shared URP toon Shader Graph; the outline is dark purple with width 0.009. The atlas uses ASTC 6x6 for Android. No scenes are rewritten. Boot loads the roster entry; choose Sir Noodle or Blobby and use **AI: ...** to choose the opponent. Rematch retains both choices. Direct arena play keeps its original Blobby setup. The runtime replaces only the visual and character-sized body/hurtbox, preserving scene HUD, input, AI, camera and match references. Items bind to the new hand socket.

The color binding is assigned after the texture finishes its 2D/platform reimport. An empty `_BaseMap` previously produced a white toon surface. Existing completed content with an empty slot is repaired automatically outside Play Mode; **Wobble Brawl → Characters → Repair Sir Noodle Colors** reapplies the atlas without rebuilding the fighter. The toon material's `_BaseMap` points to SirNoodle_Body_Albedo; the legacy `_MainTex` binding is corrected too. The regression test covers recovery of an already-created white material. The owner verified the repaired colors.

## Rebuild and verification

Use Blender 4.5 in background mode from the repository root, choosing fresh outputs:

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_sir_noodle.py -- Logs/Rebuild/SirNoodle.blend Logs/Rebuild/SirNoodle.fbx Logs/Rebuild/SirNoodle_Body_Albedo.png
blender --factory-startup -b Art/_Source/SirNoodle/SirNoodle.blend --python-exit-code 1 --python Tools/Blender/verify_sir_noodle.py
```

The Blender verification passed skin weights/budgets, all 22 moving actions, four closing loops and four special markers. Runtime assemblies, the importer and new test source compiled outside Unity; the pure frame schedule passed a standalone .NET check. The owner subsequently confirmed Unity import/appearance, Play Mode deformation/hit timing, toon result, all animation/gameplay checks, Android controls/performance and both named EditMode suites. These are owner-reported passes; the agent did not run Unity. Run EditMode `SirNoodleTests` and `AppFlowTests`, then the manual M4-01 checklist in [prompts.md](../../../docs/prompts.md). This source/export evidence does not claim a Unity visual or performance pass.
