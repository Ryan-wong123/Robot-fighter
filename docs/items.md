# Items — WOBBLE BRAWL

Companion to [PRD section 8](PRD.md), [architecture](architecture.md) and [VFX style](vfx_style.md). The owner approved Cream Pie, Boomerang, Pogo Stick, Bubble Wand, Wind-up Car and Spring Boxing Jack on 2026-11-19, expanding the original six pickups to twelve. Values below are **(start)** tuning, stored in ItemDef/MoveDef assets; damage is before shared block/combo modifiers. The current physics step is about 50 Hz, so 90/120 frames are approximately 1.8/2.4 seconds, excluding fighter hit-stop for status timers.

## Shared rules

One held item per fighter. Walk into a pickup, then press **U** or the touch Throw/Use button; Foam Glove activates on pickup. Explicit fighter facing supplies throw/launch direction. Opponent-only contact excludes the owner and teammates. Spawn selection uses the existing round-seeded weighted pool: the additional six are appended once to authored tables, deduplicated by stable id, equal starting weight one and one prewarmed instance each. No runtime item instantiation/destruction. Items Off prevents spawning; round end/reset returns held, flying and deployed instances. Fighter reset/round end clears pie slowdown and bubble trapping. Existing items retain their behavior and opening lineup.

All additional items use a 300-frame pickup/deployment lifetime (about six seconds). Held items do not expire. Deployed lifetime restarts on use; a bubble capture or jack trigger extends it sufficiently to finish its trap or warning/punch. Misses expire, boomerangs recycle when caught, pogo sticks on landing and single-hit projectiles on contact. Reuse clears ownership, per-leg hit records, warning/action transforms and trap references. Hazards Off does not disable items.

## Original six

| Item | Mode | Starting effect |
|---|---|---|
| Banana Peel | Placed | 0 damage, 18 base stun frames, 4 m/s pushback on the first opposing contact. |
| Foam Glove | On pickup | +5 m/s pushback on the next three unblocked melee hits. |
| Rubber Chicken | Thrown | Speed 5, initial lift 1.5, two ground bounces at 2.3 m/s; 10 base damage, 14 stun frames, pushback 3. |
| Rocket Plunger | Held use | Launch velocity 9 m/s forward and 3.5 m/s up. |
| Whoopee Cushion | Held use | Opponent within 2.4 m: 0 damage, 24 base stun frames, pushback 5. |
| Anvil | Placed | 90-frame warning before dropping; 18 base damage on contact. |

The original assets and art are preserved under their existing paths; M2-02/M2-10 established these six. The following sections define the M4-06 additions.

## Cream Pie (`cream-pie`)

- Look: silver plate, golden crust, cream rosettes and a red cherry. Thrown at 7 m/s, no gravity; consumed on first opposing contact.
- Hit: 6 body/leg damage, guard cost 6, stun 6 frames, pushback 1.2 m/s, no hit-stop or gauge gain.
- An accepted unblocked hit slows voluntary movement to 65% for 90 frames. A successful block prevents the slow and uses normal reduced damage; a guard-breaking pie applies the slow. Wake-up invulnerability rejects the hit/status.
- Repeated slows refresh the longest duration and strongest percentage; they do not multiply with each other. Pie slow multiplies with stage movement modifiers, so pie plus Sinking Sand gives 32.5% voluntary ground speed. Incoming knockback and jump launch are unchanged. HUD: **SLOWED**. Slow persists after the pie recycles and clears on expiry/round boundaries.

## Boomerang (`boomerang`)

- Look: broad yellow V-shaped toy with blue bands. Thrown at 7 m/s for 35 outbound frames (about 4.9 m at the current step), then homes toward the owner's position at 0.85 m height offset, at the same speed. Recycles within roughly 0.35 m of the catch point or on lifetime expiry.
- Each flight leg can hit a given opponent once; duplicate hurtbox contacts on that leg do not add damage. Returning hits are eligible again. The boomerang continues after contact.
- Hit: 8 body/leg damage, guard cost 8, stun 8 frames, pushback 1.8 m/s in the current horizontal flight direction. Block/combo scaling applies. Visual spin is 540 degrees/s; input facing changes do not reverse the outbound path.

## Pogo Stick (`pogo-stick`)

- Look: yellow shaft, red handle, blue footrests, silver coil and dark rubber foot. Held until use, then follows below the fighter during its hop.
- Launch: 4 m/s forward and 10 m/s upward, using the shared short item-launch control lock (22 frames), followed by normal air control. No permanent jump/movement change.
- On actual grounded landing after at least six flight ticks, affects an opposing fighter within 1.8 m horizontally and 1.2 m vertically of the landing area: 0 damage/guard cost, 12 base stun frames and 5.5 m/s pushback away from the landing point. Only one landing pulse; an out-of-range landing simply recycles. Standard hit/defense rules apply. Ground sampling follows the landing location, including platforms.

## Bubble Wand (`bubble-wand`)

- Look: blue handle, pink hoop and yellow cap. Use replaces the held wand with a slow cyan bubble moving at 2.5 m/s. The bubble uses the existing transparent toon surface and a dark outline so the trapped fighter remains visible.
- Capture: 0 damage and no guard drain. Holding block or wake-up invulnerability prevents capture and consumes the bubble. Already trapped targets cannot receive a second trap.
- On capture, traps movement, jump, block, attack and item-use input for up to 120 simulation frames. Gravity continues normally; this is not flight. The bubble follows the target and the HUD shows **BUBBLED!**. Trap ownership is explicit so recycling an old bubble cannot clear a different trap.
- Any subsequent accepted hit breaks the trap before normal hit resolution, or it expires naturally. Recycling the bubble, owner KO/round cleanup or fighter reset releases its target. Input resumes normally afterward; the bubble does not deal a release hit or grant health.

## Wind-up Car (`wind-up-car`)

- Look: red toy car, blue cabin, yellow stripe/key and four dark wheels. Placed 0.9 m ahead, then drives at 4 m/s along sampled ground in the initial facing direction; the model faces the travel direction.
- First opposing contact: 10 body/leg damage, guard cost 10, stun 24 frames, pushback 3 m/s and the opt-in Knockdown/GetUp sequence. A successful block uses reduced damage and prevents knockdown. Shared combo stun limits and wake-up protection apply.
- Recycles on contact or expiry. It does not harm the user and does not reverse/chase. Jump over the low car, block, or move away.

## Spring Boxing Jack (`boxing-jack`)

- Look: pink toy box, yellow lid/rivets, silver spring and red boxing glove, with a separate yellow warning ring. Placed 0.9 m ahead, ground clearance 0.27 m.
- An opponent within 1.5 m horizontally and 1.2 m vertically triggers one 90-frame warning (about 1.8 seconds), with a pulsing ring. The opponent can leave/jump clear before the punch. Triggering extends lifetime to finish the sequence.
- At the end of the warning, checks range again and punches once: 12 body/leg damage, guard cost 12, stun 20 frames, pushback 3 m/s away from the box, and 6 m/s unblocked upward launch. The shared launch containment keeps surviving victims on the arena. A successful block prevents lift and uses normal reduced damage; guard break uses full hit behavior.
- The spring/glove stretches for a 12-frame action, then recycles whether it hit or missed. One trigger per deployment; no repeated proximity hits.

## Art, audio and AI

[Original Blender source notes](../Art/_Source/NoveltyItems/README.md) and [provenance](../Art/_Source/NoveltyItems/provenance.json) record all six exports. Nine meshes total 8,696 triangles, one shared 1024 atlas; the item meshes use the existing opaque toon Shader Graph and outline. Only the bubble uses the existing transparent toon material. Atlas compression is ASTC 6x6 on Android. No external art or sampled recordings were added.

Six short synthesized SFX route through AudioManager's existing eight-source SFX pool: ItemPie, ItemBoomerang, ItemPogo, ItemBubble, ItemCar and ItemJack. Pie splat occurs on contact, boomerang/car on use, pogo on launch/landing, bubble on capture and jack on punch. SFX volume/mute applies. Final mix polish remains M4-09.

The shared AI can pick up and use each item. Authored use ranges are 1.5–4.5 m for pogo, 0.2–2.4 m for the jack and 0.2–4.5 m for the other additions; existing difficulty/mistake probabilities still gate item use. AI turns toward its target before use. Fighter movement/status logic remains independent of item visual transforms.

## Testing and installation

The first Editor import runs **Wobble Brawl > Items > Build Additional Six Items** automatically outside Play Mode. It creates move/definition/prefab assets and a separate `NewItemSandbox` scene, then publishes `Resources/Items/LaunchItems.asset` last. Existing arena scenes, authored tables and original six prefabs are preserved. Restart Play Mode so each spawner discovers the completed catalog. The importer preserves FBX hierarchy and resolves roles through mesh names or a collapsed single-mesh root, including inactive action meshes. Builds guard their own import callbacks; an automatic failure is logged once and requires script reload or an explicit menu retry, preventing repeated failed imports. Partial assets are reused.

Open `Assets/_Project/Scenes/NewItemSandbox.unity` for the ordered new-item lineup: Cream Pie, Boomerang, Pogo Stick, Bubble Wand, Wind-up Car, Spring Boxing Jack. The test scene has no stage hazard. Press Space to pause/resume AI and use U to deploy; start a fresh run/round for a fresh lineup. Normal arenas choose from all twelve through weighted spawning. Verify both facing directions, blocks, jump avoidance, repeated use, misses, expiry, pool reuse, round cleanup, Items Off and an Android match. Full steps are in [M4-06](prompts.md).

Blender source validation and all six FBX round-trips passed. Standalone .NET status checks passed exact slow/trap duration, refresh, hit break and reset. Runtime/editor/test sources compile against the installed Unity libraries. Native Unity tests and gameplay/Android verification remain pending; run EditMode NoveltyItemTests, CombatItemTests, MatchRulesTests and AudioManagerTests. The tracker must not mark these as passed until they run.
