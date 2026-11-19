# Mr. Jack — M4-10

![Compressed and extended phases](MrJack_Preview.png)

Original 7,256-triangle model, palette atlas, 18-bone rig and in-place actions authored for this project from the owner's [turnaround](../../../docs/images/characters/concept/mr_jack_turnaround.png), expressions and pose sheets. No external mesh/texture samples. The default is a purple/gold jack-in-the-box jester with red sleeves, cream gloves/painted grin, drooping bell-tipped hat, inset toy box, hinged lid and side crank.

![Both phases in Blender](MrJack_Preview.png)

## Source and export

`MrJack.blend`: one skinned mesh, **7,148 triangles**, **18 deform bones**, one 1024 palette atlas; one root, four spring joints, pelvis/spine/chest/head/hat, arms/hands, lid and crank. The physics presentation adds twelve bodies (four spring joints, torso/head and arms), with the unskinned gameplay collider independent of physics. The box remains at the fighter's base while the jester can flop from it. Bone rest transforms are unit scale; animation is in place. Imported forward is +Z; Fighter supplies the fight-facing rotation. The Unity visual normalizes the rest bounding height to 2.3 m. The final combined skin contains 7,256 triangles.

`Tools/Blender/build_mr_jack.py` runs headless and refuses existing output paths. Build into fresh staging paths; preserve a backup before replacing a source/export. Example:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.5/blender.exe' --factory-startup -b --python-exit-code 1 --python Tools/Blender/build_mr_jack.py -- Logs/JackNew/MrJack.blend Logs/JackNew/MrJack.fbx Logs/JackNew/MrJack_Body_Albedo.png
```

`verify_mr_jack.py` validates actual weights/budgets, all **45 moving actions**, eight closed loops, root motion and marked windows. The validation reports and exact source/export/audio hashes are recorded alongside this README. `render_mr_jack.py` renders the two phase poses without saving source changes.

## Moves and phases

Phase one is compressed inside the box. Living health **strictly below 160/320** activates phase two: the spring extends, speed rises **2.8 → 4.2 m/s**, and phase-two clips/move definitions replace future attacks. At exactly 160 HP phase one remains active. KO at zero does not trigger an ejection; round reset restores phase one and the full special gauge. No runtime ScriptableObject mutation or extra phase health bar.

Each row is startup / active / recovery in authored simulation frames. At current 50 Hz physics, gameplay duration is frames / 50; clips are authored at 60 FPS and attack sampling follows the move frame clock.

| Move | In box | Out of box | Body / leg damage |
|---|---|---|---|
| Punch1, double-fist slam | 21 / 5 / 26 | 14 / 5 / 23 | 11 / 6 |
| Punch2, headbutt | 24 / 5 / 27 | 17 / 5 / 24 | 11 / 6 |
| Punch3, rising punch | 27 / 5 / 28 | 20 / 5 / 25 | 11 / 6 |
| Punch4, lid-slam sweep | 30 / 5 / 29 | 23 / 5 / 26 | 11 / 6 |
| Kick1, spring dive | 27 / 5 / 20 | 20 / 5 / 17 | 13 / 7 |
| Kick2, spinning crank | 30 / 5 / 21 | 23 / 5 / 18 | 13 / 7 |
| Surprise! | 75 / 5 / 20 | 75 / 5 / 20 | 22 / 11 |

Normal attack reach is 1.74 m (forward .84 + half-width .9). Punch4 is a ground-height lid-slam shockwave: one 3.2 × .5 m hitbox centered on the boss at .25 m height, a brief orange ring and 4 m/s pushback; jump clear or block it. Kick1 adds an active spring dive at 2 m/s in phase one, 3.5 m/s in phase two and a 2.5 m/s hop. Specials spend full gauge, give an **orange floor ring and music-box warning for 75 steps (1.5 s)**, and lock the opponent's initial X position. The jester hides down inside the box, then pops up at marked ground + .02 m clearance at the marked X, clamped to ±5.7 m in actual arenas (expanded training bounds when configured). Moving clear avoids the hit; the marker does not track after startup. The box/body remain vulnerable during startup; interruption, KO, pause/round reset and scene unload cancel or suspend presentation appropriately. The special uses one 2 × 1.8 m hitbox at forward 0/height .9, guard cost 25, authored stun 20, hit-stop 6, pushback 6 m/s and unblocked knockdown/upward launch 5 m/s. Generic block and wake-up rules remain effective. Kick1 adds a 2.5 m/s active hop and forward speed 2 m/s in phase one or 3.5 m/s in phase two.

Stats: HP **320**, guard **120**, jump **12**, phase-one speed **2.8**. Boss AI uses reaction **3** frames, attack interval **12**, aggression **.95**, block **.75**, mistake **.08**, item use **.90**, hazard avoidance **.92**, pickup range **3.8 m**. Special requests require full gauge, 1.5–6 m spacing and 250-frame request cooldown. In melee, the AI requests four punches then two kicks, using shared data combos. Phase two keeps this profile and uses faster moves/speed. Values are starting tuning.

## Animation set

Both unprefixed and `Out` variants exist for Idle, Walk, JumpRise, JumpFall, JumpLand, Block, Punch1–4, Kick1–2, Special, HitLight, HitHeavy, Knockdown, GetUp, Win, Taunt, GuardBreak, Throw and KO. One additional `PhaseChange` clip ejects the jester. Four spring joints animate and receive a bounded velocity-driven visual bend in play. The Generic Animator Controller shows all state links; shared Fighter samples the clips directly, as for the existing cast.

## Unity installation and test access

The first clean Editor import installs content automatically. If needed, use **Wobble Brawl → Characters → Build Mr. Jack** outside Play Mode. It authors clips, linked controller, toon material/outline with explicit atlas binding, boss profile/data, visual and fighter prefabs, and the resource character definition. Texture import uses 2D, 1024 and Android ASTC 6×6; Generic animation, no root motion, camera/light import off. New assets have fresh GUIDs; existing asset GUIDs are preserved on rebuild.

Restart from Boot, choose your player, then cycle the **AI** opponent button to **Mr. Jack**. He has no player selection button. His fighter prefab carries AIInput, the boss profile and opponent team 1; round setup supplies the paired fighter reference explicitly. The arena page labels Boss AI, disables normal difficulty changes and forces Items/Hazards on for this boss test encounter. Existing real arenas are usable for M4-10 checks; Toy Chest Showdown is **M4-11**, Arcade Ladder **M4-07**.

## Audio and provenance

The laugh reuses the owner's approved 1.141 s edited win audition; [audio provenance](audio_provenance.json) points to the retained source and D-08 rights review. Attack barks are throttled to one per two real seconds; phase change and victory get priority. Phase two uses a modest playback pitch increase. Music-box warning is original deterministic oscillator synthesis (`Tools/Audio/generate_jack_cues.py`), mono PCM16 24 kHz, 1.5 s. Both use the existing SFX volume/pause adapter. No ungenerated dialogue is claimed. The broader voice bus/content mix remains M4-09.

## Verification status

Blender export and action validation passed; all phase thresholds passed standalone C# checks. Runtime, Editor and test source compile checks are recorded in docs/prompts.md. Native Unity import execution/tests, Play Mode appearance/combat and Android performance have not been verified by the agent.
