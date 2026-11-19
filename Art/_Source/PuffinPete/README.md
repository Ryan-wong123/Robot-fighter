# Puffin Pete

Original procedural Blender geometry, atlas, animation and synthesized pop audio, based on the owner's approved [fighter sheet](../../../docs/characters.md#43-puffin-pete). No external meshes, textures, motion capture or sampled audio were added. The owner-provided reference provenance remains in [concept SOURCES](../../../docs/images/characters/concept/SOURCES.md).

## Source and measured budgets

- `PuffinPete.blend`: packed 1024 atlas, one armature, one skinned mesh, 24 actions, Inflate and Deflate shape keys.
- `PuffinPete.validation.json`: 8,236 triangles, 20 bones (19 deform), one source material, source height 1.555 m.
- `provenance.json`: exact verified source/export and audio hashes.
- `Poses.png`: Blender normal/back/morph/action previews; not a Unity render.
- FBX and atlas: `Assets/_Project/Art/Characters/PuffinPete/`.

Pete has blue vinyl, a white belly and face panels, orange beak/toes, large eyes, seams, a green flipper repair patch and a back air valve. Painted panels conform to the blue shell. The body has two morph targets: Inflate enlarges it, Deflate flattens it below half normal height. Unity interpolates morphs without swapping/destroying meshes. Shared URP toon shading and a navy outline are assigned by the importer; Android atlas compression is ASTC 6x6.

## Rig and actions

The shared Root/Pelvis/Spine/Chest/Head and upper/lower limb rig adds two Flipper_Tip bones and Tail. Twelve standard bones receive visual physics; hands, feet, flipper tips and tail follow animation/parents. Skin weights are normalized with at most two influences. Unity imports a Generic avatar and normalizes the visual to 1.55 m. Root motion is disabled.

All 24 actions: Idle, Walk, JumpRise, JumpFall, JumpLand, Block, Punch1–4, Kick1–2, Special, Throw, HitLight, HitHeavy, GuardBreak, Knockdown, GetUp, KO, Win, Taunt, Deflate and Refill. Idle, Walk, Block, JumpFall and Deflate close cleanly. Punches use two flipper strikes, a headbutt and a belly bump. Taunt pats the belly; Win uses a forward slide-like pose. Special rotates the entire rig around the belly center, avoiding torso skin tearing. Its pivot-compensating root-bone curves are visual only; the fighter motor supplies actual displacement. Attack pose time follows gameplay move frames; the source is authored at 60 FPS while the current physics clock is about 50 Hz.

## Rules and starting data

HP 220, guard 100, special capacity 100, speed 2.6, jump speed 8. Normal forward hitbox extent is 1.305 m, 0.9 times Blobby's 1.45 m. Punches deal 10 body / 5 leg damage; kicks deal 12 / 6. Punch startup/active/recovery frames: 7/4/15, 9/4/15, 10/4/17, 13/4/21. Kicks: 10/4/18 and 13/4/21. Partial-ragdoll reaction strength is 0.65 and duration 0.28 s.

Inflate enters while grounded and blocking with full guard. It remains latched as guard drains until release, pop, guard break or round end. Inflated damage receives an additional 30% reduction after normal block/combo resolution, rounded down with a minimum of one for positive damage. Zero-damage hits stay zero. Guard-breaking hits bypass this extra armor and cancel inflation; KO always uses full ragdoll. Inflated survivors resist hit knockback and partial-ragdoll launch.

Every accepted hit while inflated adds 25 to a capacity-100 pop meter: the fourth hit pops Pete. Releasing block lets pressure decay by one per simulation frame. Pop lasts 75 simulation frames (about 1.5 s at the current physics step, excluding hit-stop). It consumes movement/attack/jump input, removes armor, and zips at 5 m/s with direction changes every 12 frames. It turns inward at X bounds -6.2/+6.2 m, sized for the current 14 m arenas. Incoming damage still applies; further hits do not restart recovery. Afterward pressure resets and normal size/control return. Guard refills through the existing shared recovery rule; release block to recharge before inflating again.

The HUD adds a Pop bar only for inflation-capable fighters, with POPPED! during helplessness. Morphs obey hit-stop. Pop emits a bounded burst of twenty air streaks for 0.8 s, PSSSHT! impact text and the original `PetePop.wav` hiss. Audio respects SFX settings. Round end/reset clears pressure, morphs, particles and audio.

Belly Blimp costs a full gauge. Startup/active/recovery are 18/24/30 frames. Only active frames move forward at 2.2 m/s; a 2.5 m/s upward hop starts on the first active frame. Its 2.1 × 1.4 × 1 m hitbox is centered 0.6 m forward and 0.75 m high. It hits once for 18 body / 10 leg damage, with 14 stun frames, 6 hit-stop frames and 4 pushback. Visual inflation reaches 150% morph weight. Wind-up and recovery are stationary relative to conveyors and can be punished. Blocking armor does not apply during the special.

## Unity installation and checks

[PuffinPeteContent.cs](../../../Assets/_Project/Scripts/Legacy/Editor/PuffinPeteContent.cs) automatically builds missing Generic import settings, 24 clips/controller, toon materials, visual/Fighter prefabs, effects, move assets and `Resources/Characters/PuffinPete.asset`. Existing completed content is preserved; **Wobble Brawl → Characters → Build Puffin Pete** explicitly rebuilds authored defaults. Texture binding happens after 2D import and rejects a missing atlas. **Repair Puffin Pete Colors** repairs an empty atlas binding without rebuilding. No scenes are rewritten.

Boot → Quick Fight offers Blobby, Sir Noodle and Puffin Pete. AI selection cycles all three; rematches retain choices. Direct arena play keeps the existing Blobby setup. The left flipper's hand bone supplies the item socket. The importer samples its nearby morph displacement into socket offsets so held items follow inflation and flattening without runtime mesh reads. Final music/voice/sound polish remains M4-09.

```powershell
blender --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_puffin_pete.py -- Logs/Rebuild/PuffinPete.blend Logs/Rebuild/PuffinPete.fbx Logs/Rebuild/PuffinPete_Body_Albedo.png
blender --factory-startup -b Art/_Source/PuffinPete/PuffinPete.blend --python-exit-code 1 --python Tools/Blender/verify_puffin_pete.py
```

The builder refuses existing outputs. Blender checks passed weights/budgets, all 24 moving actions, five closing loops, morphs and special markers. Standalone C# checks passed guard gating, damage rounding, latch/release, four-hit pop, 75-frame recovery, pressure decay and reset. Runtime/editor/test sources compiled against installed Unity libraries. Unity import execution, native deformation/contact timing, UI, tests and Android performance remain to verify with EditMode `PuffinPeteTests`, `SirNoodleTests` and `AppFlowTests`, plus the [manual M4-02 checklist](../../../docs/prompts.md). No agent Unity test pass is claimed.
