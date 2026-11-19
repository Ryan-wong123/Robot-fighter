# Architecture — WOBBLE BRAWL

> Companion to [`PRD.md`](PRD.md). Describes the target technical design and how today's prototype code maps onto it.
> Status: DRAFT v0.2. Target: **Unity 6.3 or newer, URP** (PRD section 16). Items marked **[VERIFY]** need confirming against the exact installed version.

## 1. Goals
- **Data-driven:** fighters, moves, items, stages, and hazards are `ScriptableObject` assets; adding content needs no code.
- **Decoupled:** input, combat, animation, ragdoll, AI, and UI communicate via small interfaces and events, not `Find`/`SendMessage`.
- **Mobile-safe:** predictable allocations, pooling, capped physics cost.
- **Testable:** combat rules live in plain C# classes that can be unit-tested without a scene.

## 2. Current State (prototype baseline)
| Concern | Today | Problem |
|---|---|---|
| Fighters | `PlayerBehaviour` (552 lines), `EnemyBehaviour` (303 lines) | Near-duplicate hit/damage/gauge code; hard-coded numbers |
| Input | Keyboard + `Joystick` read inside `PlayerBehaviour`; buttons call `Controller` -> `PlayerBehaviour` | Input is welded to the player; AI uses a separate code path |
| Hit detection | Animation events -> `Physics.OverlapSphere` -> `SendMessageUpwards("TakeHitX")` | Stringly-typed, matches colliders by **name** (`"Opponent_Body"`) |
| Match flow | `GameManager` polls state in `FixedUpdate` once per second | Win checks are tick-delayed, tie unhandled |
| Items | `Throwables` with `GameObject.Find` + `Debug.Log` every frame, exact-float rotation checks | Fragile, slow, single-item only |
| UI | `GameObject.Find("PlayerHealth")` etc. | Breaks on rename; logic and UI coupled |
| Repo | Unity project at repo root with `Assets/`, `ProjectSettings/`, and `Packages/` | Prototype code lives in `Assets/_Project/Scripts/Legacy/` and vendor assets live in `Assets/ThirdParty/` |

Nothing here is wasted: the joystick pack, particle prefabs, scene HUD layout, and animation event pattern carry forward.

## 3. High-Level Layers
```
+--------------------------------------------------------------+
|  UI (menus, HUD, results)        Audio        Save/Settings  |
+--------------------------------------------------------------+
|  Game Flow:  AppState -> MatchController -> RoundController   |
+--------------------------------------------------------------+
|  Gameplay:  Fighter  <-  IFighterInput (Touch | Keyboard | AI)|
|             Combat (Hit/Hurt boxes, DamageResolver)           |
|             Ragdoll (PartialRagdoll, FullRagdoll)              |
|             Items, Hazards, Stage                              |
+--------------------------------------------------------------+
|  Data (ScriptableObjects): CharacterDef, MoveDef, ItemDef,    |
|                            HazardDef, StageDef, AIProfile      |
+--------------------------------------------------------------+
|  Core: EventBus, ServiceLocator-lite, Pooling, Math/Utils      |
+--------------------------------------------------------------+
```
Dependencies point **downward only**. UI observes gameplay through events; gameplay never references UI.

## 4. Folder & Assembly Layout (target)
The repository root is a standard Unity project. The layout below describes the target organization within `Assets/`. Graphics settings reference the mobile URP asset from the installed Unity template. Prototype model materials use URP Lit and particle materials use URP Particles Unlit. The mobile pipeline settings originate from the Unity Editor Universal 3D template. Unity 6.3 updated the prototype scripts to use `Rigidbody.linearVelocity`.

```
Assets/
  _Project/
    Art/            (Characters/<Name>/, Environments/, UI/, VFX/, Materials/)
    Audio/
    Animation/      (controllers, shared clips)
    Data/           (Characters/, Moves/, Items/, Hazards/, Stages/, AI/)
    Prefabs/
    Scenes/         (Boot, MainMenu, Match, Sandbox_*)
    Scripts/
      Core/         (events, pooling, services)
      Data/         (ScriptableObject definitions)
      Gameplay/     (Fighter, Combat, Ragdoll, Items, Hazards, AI)
      Flow/         (App, Match, Round)
      UI/
      Audio/
      Save/
    Tests/          (EditMode/, PlayMode/)
  ThirdParty/       (Joystick Pack, etc. — never edited in place)
```
Assembly definitions: `WobbleBrawl.Core`, `.Data`, `.Gameplay`, `.Flow`, `.UI`, plus test assemblies. Each depends only on those below it (enforces the layering and speeds compile times).

The prototype uses `RoundRules` in Flow and `BlockRules` in Gameplay. Legacy adapters handle knockout events, update the timer, and forward touch and keyboard intent. The rules are covered by the EditMode test assembly.

## 5. Fighter Architecture
A fighter is a prefab composed of small components, replacing the two monolithic behaviours.

```
Fighter (prefab root)
 ├─ Fighter                  orchestrator; owns state, references the parts
 ├─ FighterStateMachine      Idle, Walk, Jump, Attack, Block, HitStun, GuardBreak,
 │                           Knockdown, GetUp, KO, Win
 ├─ FighterHealth            HP, damage events, invulnerability windows
 ├─ GuardMeter               block drain/regen, guard-break event
 ├─ SpecialGauge             fill rules, ready event
 ├─ MoveExecutor             plays MoveDef, drives combo window and cancels
 ├─ HitboxController         enables/disables hitboxes per move frames
 ├─ HurtboxSet               body/leg zones (replaces name matching)
 ├─ FighterMotor             movement, jump, facing (Rigidbody, locked to the 2.5D plane)
 ├─ RagdollController        partial/full ragdoll blending
 ├─ ItemHolder               held item, pickup/throw
 └─ IFighterInput (one of)   TouchInput | KeyboardInput | AIInput
```

The M1 implementation introduces shared Fighter, Hitbox and Hurtbox components alongside the M0 scene. CharacterDef and MoveDef define stats and frame windows. RoundController consumes fighter knockout events and resolves timed rounds. MatchController counts round wins and schedules the next round. AIInput reads the opponent's position and attack state and produces the same intent as human input. Open `Assets/_Project/Scenes/CombatSandbox.unity` to exercise these components with Blobby’s imported Blender blockout on both fighters. Use the arrow keys, W to jump, S to block, J to punch, K to kick, and L for special. The sandbox uses keyboard input and the legacy battle scene remains available.

### 5.1 Input abstraction
```csharp
public struct FighterIntent {
    public float Move;         // -1..1
    public bool JumpPressed;
    public bool BlockHeld;
    public bool PunchPressed, KickPressed, SpecialPressed, ItemPressed;
}
public interface IFighterInput { FighterIntent Read(); }
```
`Fighter` consumes a `FighterIntent` each tick. Human and AI differ **only** by which `IFighterInput` is attached. Local Versus is two `TouchInput` instances bound to different screen zones. The on-screen buttons set flags on `TouchInput` (replacing `Controller` and the `*Btn` scripts).

### 5.2 State machine
- Explicit enum + class states (or a lightweight table). One current state; transitions are validated (e.g., cannot block while in `Attack`).
- Animator is a **view** of state: parameters are set from state changes, and gameplay logic does not depend on animator transition timing. Animation events are used only to enable hitboxes and play SFX.

### 5.3 Moves as data
```csharp
[CreateAssetMenu] public class MoveDef : ScriptableObject {
    public string Id; public AnimationClip Clip; public string AnimTrigger;
    public int Startup, Active, Recovery;       // frames @ 60 fps
    public HitboxShape[] Hitboxes;              // bone, offset, size, active frame range
    public float DamageBody, DamageLeg, Hitstun, Blockstun, Knockback;
    public float GaugeGain, GuardDamage;
    public RagdollImpulse Ragdoll;              // how strongly the hit flops the target
    public MoveDef[] NextInCombo;               // combo links
}
```
`CharacterDef` lists a fighter's moves, health, speed, jump, guard-meter size, special, prefab, ragdoll tuning.

## 6. Combat System

### 6.1 Hit / hurt boxes
- `Hurtbox` components on body parts (zone enum `Head/Body/Legs`) replace `c.name == "Opponent_Body"`.
- `Hitbox` components are enabled by `HitboxController` for a move's active frames. Overlap checks use `Physics.OverlapBoxNonAlloc`/trigger callbacks (no allocations).
- Layers: `FighterHurt`, `FighterHit`, `Item`, `Hazard`, `Environment`. The collision matrix is configured so hit/hurt only collide across teams.
- A hit is delivered as a struct, not a string message:
```csharp
public struct HitInfo { Fighter Attacker; MoveDef Move; HurtZone Zone; Vector3 Point, Direction; float RawDamage; }
```

### 6.2 DamageResolver (pure C#, unit-tested)
Takes `HitInfo` + defender state -> `HitResult` (final damage, blocked?, guard-broken?, hitstun, knockback, ragdoll impulse). Rules from the PRD live here and only here:
- Zone multiplier (body 10 / legs 5 start values come from data, not constants).
- **Block:** 75% reduction, pushback, guard-meter drain, guard break at zero.
- Combo scaling and hit-stun caps (no infinite combos).
- Special chip damage through block.
- Wake-up invulnerability.

### 6.3 Hit-stop & game feel

The UI subscribes to `Fighter.HitResolved` and selects an impact cue from `CombatImpactPalette.json`. Each fighter reuses one label and one unlit Canvas mesh to render comic text, bursts, and rings during hit-stop and KO slow motion.
`HitStopService` briefly freezes the two fighters' animators (not global `Time.timeScale`, so the UI/VFX continue) for the move's hit-stop duration. Camera shake and comic-text popups are triggered by `OnHitResolved` events, never by combat code directly.

## 7. Ragdoll System (key differentiator)
Two modes, both built on the Unity ragdoll joints set up on the Blender-exported rig. **[VERIFY]** against the chosen physics version/Unity ragdoll tooling.

1. **Partial ragdoll (hit reactions):** on hit, physics weight is blended into selected bones for 0.2-0.6 s depending on hit strength, then blended back to animation. Implementation: bones driven by animation normally; on hit, kinematic -> dynamic on the hit limb chain with an impulse, then lerp pose back.
2. **Full ragdoll (KO, big launches):** the whole body goes dynamic, with a launch impulse, slow-motion, and a camera punch-in. After KO the body settles, then the get-up/win-pose is played for the next round via a reset.

Rules to keep it fair and cheap:
- **Gameplay hitboxes/hurtboxes are logical and follow the animated pose**, never raw ragdoll positions. The ragdoll is visual feedback plus a position hint for the root.
- Fighter root stays on the fight plane (Z locked); ragdoll bones constrained to a thin Z range.
- Joints/colliders only on ~10-15 major bones; simplified colliders. Hard cap on simultaneous full ragdolls (2 fighters + at most a few prop ragdolls).
- Ragdoll bodies sleep aggressively; solver iterations kept low.
- Characters differ in ragdoll tuning (e.g., Sir Noodle floppier) through `CharacterDef`.

Squash-and-stretch is a separate cosmetic layer: a small `SquashStretch` component (scale spring on the visual root / blendshape / vertex shader) triggered by hit events.

The Blobby sandbox temporarily simulates a jointed torso or leg bone on unblocked hits and blends its pose back into the sampled animation and activates jointed visual rigidbodies on KO. Round reset disables the ragdoll colliders and returns the visual to clip sampling.

## 8. Items & Hazards

The six combat pickups use static meshes from `Art/_Source/Items/CombatPickups.blend`. The Unity prefabs keep their pooled root and trigger collider. An imported FBX child supplies the visuals with shared palette materials driven by `ToonPickupSurface.shadergraph`. The graph uses neutral lighting bands and an albedo-based rim highlight to preserve each pickup's colors.

Hit knockback sets horizontal velocity directly. Hit-stop captures that velocity and restores it after the freeze. The knockback replaces the target's approach velocity so walking toward a trap does not cancel the slide.

### 8.1 Items
```csharp
[CreateAssetMenu] public class ItemDef : ScriptableObject {
    public string Id; public GameObject Prefab; public ItemUseMode Mode; // OnPickup | Thrown | Placed | Held
    public ItemEffect[] Effects;
}
public abstract class ItemEffect : ScriptableObject { public abstract void Apply(ItemContext ctx); }
```
- `ItemSpawner` prewarms a capped pool per weighted table entry in Awake. It starts a seeded sequence for each round, selects among entries with free instances, and returns all borrowed instances when the round ends. Stage transforms mark pickup positions on the fight plane. CombatSandbox uses the permanent twelve-item training rack; actual arenas use weighted spawning with one item per round-seeded 200–400-frame opportunity (center 300, jitter 100). The first spawn is delayed too; legacy opening flags are honored only in CombatSandbox. Each delay and item/point choice uses the same seeded generator; exhausted tables skip an opportunity without catch-up.
- `ItemHolder` on the fighter manages pickup (trigger), held state, and use/throw. The facing direction comes from the fighter's explicit `Facing` value, not rotation float comparisons.
- `PooledItem` handles pickup and deployed contact through fighter hurtboxes. Held items attach to an explicit socket. OnPickup and Held effects receive the user as the target. Thrown and Placed effects run on opponent contact. Effects receive the user, target, world position, and facing through `ItemContext`. `CombatItemEffect` uses the shared hit resolver for direct and area hits, with separate actions for launching the holder and boosting melee knockback. `PooledItem` uses item data for bounces and delayed drops.

M4-06 adds a resource LaunchItemCatalog that appends six installed definitions to each authored spawn table once in Awake, deduplicating stable ids while preserving authored weights/capacities outside training. NoveltyItemsContent creates the original Blender-model prefabs, moves and catalog, and schedules CombinedSandboxContent to upgrade CombatSandbox; actual arenas are not rewritten. PooledItem caches an optional NoveltyItem adapter and delegates deployed ticks/contact/recycle to it; legacy pickups keep their existing path. The adapter implements outbound/return hit caps, target-owned bubble trapping, ground-following cars, pogo landing detection and a warning-before-punch jack. It caches physics/visual references and reuses all meshes without runtime spawning.

Core ItemStatusState owns integer slow/trap timers, strongest/longest slow refresh, hit break and reset. Fighter applies item speed multiplicatively with stage modifiers, consumes trapped input, releases trapping before accepted damage, pauses timers under hit-stop and clears state at round boundaries. Trap ownership tokens prevent a recycled bubble from clearing another trap. MatchHud shows SLOWED/BUBBLED through existing vital events. Optional ItemDef AIUseRange overrides the shared AI's default use distances. Six additional AudioCue ids use the existing eight-source pool. [Item rules](items.md) are the data reference.

### 8.2 Hazards
```csharp
[CreateAssetMenu] public class HazardDef : ScriptableObject { /* telegraph time, active time, cooldown, effect */ }
```
A `HazardController` uses fixed-step timing, emits phase events, and enforces a warning of at least 1.5 seconds. `RoundController` starts and clears the cycle and suspends it in sudden death unless the hazard data allows it. Between cycles, idle time comes from a round-seeded range. The active volume checks hurtboxes and applies at most one accepted attacker-less `HitInfo` per fighter per activation through the shared damage and status pipeline.

### 8.3 Stages
`StageDef` references the scene/prefab, spawn points, item table, hazard list, camera bounds, music.

Bathtub Brawl is a standalone scene wired to the existing match flow. Its `BathtubHazards` component uses the shared hazard clock to alternate water and duck-wave activations. Water sets movement, jump, and ragdoll gravity multipliers on submerged fighters. The moving wave uses the same attacker-less hit resolution as the sandbox hazard.

Kitchen Counter is a standalone scene with a `KitchenHazards` component that alternates toast and mixer activations on the same clock. Toast checks hurtboxes along two authored arcs and uses attacker-less hit resolution. The belt adds signed speed to the grounded fighter motor. The stage component clears that speed on phase changes and round end.

## 9. Match Flow
```
AppStateMachine:  Boot -> MainMenu -> Selection -> Match -> Results -> MainMenu
MatchController:  holds best-of-3 state, creates fighters from CharacterDefs, owns MatchResult
RoundController:  Intro -> Fight -> (KO | TimeUp | SuddenDeath) -> RoundEnd
```
- The `Boot` scene is first in the build. Its app root survives scene loads, and the active match registers its `MatchEnded` event with that root. Results wait for the KO presentation and support rematch or a return to Boot. Opening an arena directly still starts its standalone match.
- The Boot view renders the illustrated Quick Fight menu and keeps its controls within the display safe area. Quick Fight offers the installed Blobby/Sir Noodle/Puffin Pete/Madame Plush roster and the three original entries plus installed StageDef entries. The app passes the selected definitions to both fighters before round one and remembers the arena and roster choices for rematches. Mr. Jack has a boss-only opponent entry with his M4-10 prefab/animation set; player selection rejects boss definitions.
- Sir Noodle's resource definition extends selection to two fighters and a selectable AI opponent. AppStateMachine loads `Resources/Characters/SirNoodle`, and passes both selected definitions to the round before startup. `CharacterDef.VisualPrefab`, `VisualClips`, `AnimationPrefix`, dimensions and partial-ragdoll settings let Fighter replace the visible rig while preserving scene references. `FighterVisual` supplies the new hand socket. The visual keeps manual clip sampling for the existing Generic animation pipeline; authored attack clips follow move-frame time. The linked Animator Controller is also available for clip/transition inspection.
- SirNoodleContent binds the atlas after texture reimport. Its startup repair also fixes an empty `_BaseMap` in already-generated content without rebuilding the fighter; the explicit Repair Sir Noodle Colors menu performs the same color-only operation outside Play Mode.
- `MoveHitSchedule` is pure frame-based C#. MoveDef's optional `HitIntervalFrames` and `AlternateSides` enable four alternating special windows while zero interval preserves existing one-hit moves. `HeavyReaction` selects the heavier reaction clip independently of damage. Hitbox accepts the resolved hit direction. AI derives spacing/threat distance from move reach. Sir Noodle uses twelve physical bones and eight intermediate visual follow bones; reaction strength and duration come from CharacterDef. RoundController plays the win pose only for the actual winner. Landing, guard break/recovery, item throw and the development taunt can use the expanded clip set.
- Puffin Pete uses Core's pure `InflationState` for guard-gated armor, pressure and frame-based recovery. CharacterDef holds optional inflation settings; characters without the flag retain the existing behavior. Fighter integrates integer damage reduction, knockback suppression, a vulnerable Deflated state, deterministic alternating motor motion and control consumption. MoveDef's optional ForwardSpeed/HopSpeed act during active frames for Belly Blimp; existing moves default to zero. Guard breaks cancel inflation and use full damage.
- Madame Plush uses Core's pure integer `StuffingRules` for inclusive health thresholds and the current-health damage bonus. Optional CharacterDef flags leave other fighters unchanged. Fighter applies the bonus only to fighter attacks before DamageResolver; optional MoveDef LaunchSpeed/LaunchBounds/LaunchContainmentFrames provide uppercut lift and temporary surviving-victim containment, including cached hit-stop velocity. The importer removes exported rest blend-shape curves from copied clips so animation sampling cannot close health-driven seams. StuffingVisual runs after fighter pose sampling and caches three morph indices, reveals seams monotonically per round, and emits bounded cotton/audio through VitalsChanged; PresentationReset clears effects and mends only at full-health round reset. VictoryCelebrated drives the winner bell. VisualRagdoll accepts optional launch speeds for uppercut KOs. MatchHud reuses the guard caption for TOUGH!. MadamePlushContent generates Generic clips/controller, prefab, data, toon materials and feedback on first Editor import; Boot loads the resource definition and offers the four-fighter roster without rewriting scenes.
- Bouncy Castle Fair adds resource-discovered StageDef metadata (stable id, scene, name, hazard hint and thumbnail) and a FairgroundDef tuning asset. AppStateMachine appends installed stage choices once during Awake and deduplicates scene ids; existing serialized choices remain intact. BouncyCastleContent authors a copied CombatSandbox scene additively, registers build settings and publishes metadata only after saving. FairgroundHazards adapts the shared seeded HazardController to alternating pads and wall squeeze, using Core FairgroundRules for contact geometry and the squeeze envelope. It runs before Fighter physics, supplies optional jump multipliers/auto-bounce and stage containment, moves kinematic wall bodies and reuses warning meshes/audio without runtime spawning. FairgroundCamera belongs to Flow, reads RoundController and hazard width, and stops writing during KO/results. AudioManager selects the stage's original music loop; ambient/warnings use SettingsAudioSource. Existing stages are unchanged.
- GiantSandboxContent reuses StageDef discovery and additive stage installation. GiantSandboxDef owns layout, paw travel/contact/clearance and sand movement tuning. Core SandboxRules provides pure swept-X contact, defensive gating and patch geometry. GiantSandboxHazards adapts the seeded clock, caches explicit fighters, reuses four hazard meshes and records per-pass contacts. It supplies an independent optional stage movement multiplier before Fighter physics and clears it on every phase transition/reset. MoveDef.KnocksDown is opt-in; FighterState.KnockedDown is appended to preserve existing enum values, consumes input during capped stun, then plays GetUp with shared wake-up immunity without refilling guard. Other moves default off. AI uses IHazardAwareness to guard/jump against the sweep or leave sand. AudioManager selects original SandMusic; ambience/warnings use SettingsAudioSource. Stage-button widths adapt to the four launch arenas plus CombatSandbox.
- `InflationVisual` caches blend-shape bindings and subscribes to InflationPopped/PresentationReset. It interpolates Inflate/Deflate independently of pose clips, respects hit-stop, plays a bounded twenty-particle air burst and an original SFX cue, and clears effects on round boundaries. MatchHud adds a Pop bar only for inflation-capable fighters; fill updates use value types and constant labels instead of per-frame numeric strings. The shared impact popup uses PSSSHT! while deflated. PuffinPeteContent creates Generic clips, morph-enabled prefabs, materials, move/character assets and the third roster entry on first Editor import without editing scenes. Its texture binding follows reimport and rejects a missing atlas.
- Pete's importer derives local item-socket offsets from the nearest grip vertex's morph deltas. InflationVisual blends these cached offsets with the body morphs, keeping held items at the flipper without reading mesh arrays during combat.
- Core's immutable `MatchRules` (`HazardsEnabled`, `ItemsEnabled`; the Classic options, see [`maps.md`](maps.md) section 5) resolves Arcade/boss restrictions without Unity dependencies. `AppStateMachine` owns requested setup options and `AppFlowView` builds two checkboxes on arena selection. Fresh selection restores both on; rematches retain the effective rules. `StageChoice.IsBossStage` locks the UI for boss entries; `OpenSelection(MatchMode.Arcade)` locks Arcade setup. These are hooks for M4 content, not a completed Arcade mode.
- Before round one, `AppStateMachine.AttachMatch` passes the effective rules to `MatchController.ConfigureRules`. The controller also enforces its serialized `mode`/`isBossStage` for standalone scenes and rejects changes after round one. `RoundController` passes the same rules to every `ItemSpawner.BeginRound` and `HazardController.BeginRound`. Disabled items clear the pool and never start a spawn timer; disabled hazards clear stage effects and remain Inactive with no telegraph/audio or timer advancement. Stage geometry and ambient effects stay. Direct arena play defaults both on. Rules must become part of the synchronized match config for online play.
- Round end is evaluated **on the event** (`OnFighterKO`, `OnTimerExpired`), not by polling once a second.
- Tie at timeout enters **sudden death**; there is no unresolved state.
- `MatchController` emits events (`RoundStarted`, `RoundEnded`, `MatchEnded`); HUD/audio/results subscribe.

## 10. AI
- `AIInput : IFighterInput` uses Easy, Normal, and Hard `AIProfile` assets for reaction cadence, attack pacing, aggression, blocks, mistakes, item use, and hazard avoidance. It rolls decisions from a round-seeded sequence. Normal is the default when the input has no assigned profile.
- The AI reads opponent distance and action state, the nearest available pickup, and avoidance directions from the active hazard. It prioritizes avoidance and blocks, then considers items before approaching or attacking. Bathtub wave avoidance uses a timed jump. Kitchen Counter and CombatSandbox use retreat directions. The same input and combat pipeline drive the fighter on all three tiers.
- Randomness is seeded for reproducibility in tests.

## 11. Camera
Evolves `MoveCamera`: frames all fighters, clamps to stage bounds, adds shake, punch-in on KO, and a bounded zoom. (The current `/4` midpoint math looks like a bug and should be rechecked when migrating.)

## 12. UI
- The shared match HUD listens to `Fighter.VitalsChanged`, `RoundController.TimeChanged` and `SuddenDeathStarted`, and `MatchController.RoundStarted` and `RoundEnded`. The round publishes time only when the displayed second changes. The same safe-area layout is wired in all three playable arenas.
- Menus: simple screen manager (stack of screens). Use Unity UI (uGUI) with TextMeshPro **[VERIFY]**; the prototype's legacy `UI.Text` is replaced.
- Touch UI is a prefab with anchors respecting safe areas (notches) and user layout presets.

## 13. Cross-cutting Services
| Service | Responsibility |
|---|---|
| `EventBus` / C# events | Decoupled gameplay events (hit, KO, round end, item picked) |
| `Pool<T>` | VFX, comic text, items, projectiles |
| `AudioManager` | Buses (master/music/SFX), pooled sources, ducking for announcer |
| `SaveService` | Versioned JSON in `persistentDataPath` (coins, unlocks, settings), atomic write |
| `SettingsService` | Volume, haptics, control layout, quality |
| `Haptics` | Thin wrapper over platform APIs, no-ops in editor |
| `TimeService` | Hit-stop, slow-mo, pause without global side effects |

Services are reached via a small explicit locator or injected through `MatchController`/scene bootstrapper. No static singletons with hidden state beyond the `Boot` scene.

AudioManager is attached to the persistent Boot app root. Direct arena play creates a scene-local manager through MatchAudio. It preallocates eight SFX sources, one menu source, and a looping music source. Music changes on scene load and ducks briefly for KO and win cues. MatchAudio subscribes to fighter and match events. Stage warning sources retain their existing cues through SettingsAudioSource. Generated placeholder clips and their generator are recorded in Tools/Audio.

## 14. Performance Budget (mobile)
- Draw calls: < 150; SRP batching/GPU instancing **[VERIFY]**.
- Per fighter: <= 12k tris, <= 30 bones, 1-2 materials, a 1024 atlas.
- Physics: fixed timestep 1/60 (maybe 1/50), low solver iterations, ragdoll bone cap, layers collision matrix trimmed.
- GC: zero per-frame allocations in combat loops (use `NonAlloc`, caches, `Animator.StringToHash`).
- Particles: hard caps on count; mobile-friendly shaders; pooled.
- Profile on a **real low-end device** at the end of every milestone.

## 15. Build, Tooling & Repo Hygiene
- Commit `ProjectSettings/` and `Packages/manifest.json`; add a Unity `.gitignore` (Library, Temp, Logs, obj, UserSettings, builds).
- Force text serialization (YAML) and visible `.meta` files; **never** delete or hand-edit `.meta` GUIDs.
- **Git LFS (decided)** for large binary assets (`.fbx`, `.blend`, textures, audio, `.hdr`, `.tif`). Needs a `.gitattributes` and a decision on migrating already-committed binaries (existing history keeps them as normal blobs).
- Blender sources live under `Art/_Source/` (not imported by Unity); exported FBX/glTF under the character folder.
- **CI (decided): GitHub Actions.**
  - `.github/workflows/unity-ci.yml`: GameCI EditMode and PlayMode tests on every push/PR; Android AAB build on manual dispatch or `v*` tags. Jobs are skipped until `ProjectSettings/ProjectVersion.txt` exists and the `UNITY_LICENSE` secret is set. Unity 6 license activation must be confirmed in the GameCI docs **[VERIFY]**.
  - Task ticking is local and agent-driven: `Tools/tasks/tick_tasks.py --complete <ID>` runs a task's checks (`Tools/tasks/verify.json`) and ticks [`task.md`](task.md) only on success. No CI involvement.
  - Secrets (Unity license, Android keystore, Photon ids) live only in GitHub Secrets.

## 16. Testing Strategy
- **EditMode unit tests:** `DamageResolver`, combo/state-machine rules, guard meter, round/match flow, item weighting, save-data migration. These need no scenes.
- **PlayMode smoke tests:** a `Sandbox_Match` scene with two scripted AI fighters runs N seconds and asserts a round resolves (no soft-locks).
- **Manual:** device playtest checklist per milestone (controls reachability, performance, readability of hazards).

## 17. Migration Plan from the Prototype
Prototype classes map onto the new design in small, reviewable steps (details to be scheduled after PRD sign-off, **no code changes are made by this document**):

| Prototype | Becomes |
|---|---|
| `PlayerBehaviour` / `EnemyBehaviour` | `Fighter` + parts (section 5); duplicated hit logic merged into `DamageResolver` |
| `Controller` + `PunchBtn/KickBtn/ThrowBtn/SpBtn` | `TouchInput` + a single `TouchButton` component |
| `GameManager` | `MatchController` + `RoundController` + `ItemSpawner` |
| `Throwables` / `RightHandPickup` | `ItemDef` + `ItemHolder`; hand socket as an explicit reference |
| `MoveCamera` | `FightCamera` |
| `GameObject.Find(...)` HUD wiring | Event-bound HUD views |
| Revive code | **Deleted** (no revive in this game) |
| Joystick Pack | Moved to `ThirdParty/`, wrapped by `TouchInput` |
| Robot models/animations | Placeholders until Blender fighters land |

Both legacy BattleScene copies serialize their health labels, special gauges, enemy target, and throwable hand socket. Pickup uses the player from the trigger contact to resolve the hand.

## 18. Monetization Architecture (ads + IAP)
- A `MonetizationService` interface (`IAdService`, `IPurchaseService`) hides the SDKs. Gameplay and UI never call an SDK directly, so SDKs can be swapped or stubbed (editor/tests use a fake that always succeeds).
- SDKs: **Unity LevelPlay** (mediation) with **Unity Ads** as a network behind `IAdService`; **Unity IAP** behind `IPurchaseService`. Google Play Billing is reached through Unity IAP. Target store is Google Play only for now; keep store-specific code inside the platform layer so iOS can be added later.
- Ads: rewarded placements are requested from UI buttons and grant their reward through `SaveService`/`EconomyService` only on the success callback. Interstitials are triggered only by `Flow` at safe points (Results screen), with a frequency cap held in settings data. Ad calls are never made during a round.
- IAP: products are defined in a `ProductDef` ScriptableObject (id, type, reward). Purchases are validated per store policy, granted idempotently, and persisted; **Restore Purchases** is mandatory.
- `ConsentService` gates personalization and analytics (age/consent), and runs before the first ad request.
- Economy (coins, unlocks, "Remove Ads" flag) lives in `SaveService`; all grants go through one `EconomyService` so they can be audited and tested.
- All of this is isolated in its own assembly (`WobbleBrawl.Monetization`) above `Flow` in the dependency order, so it can be excluded from development builds.

## 19. Online Multiplayer (final phase, M6): Photon Quantum 3

**Decided:** Photon Quantum 3 (deterministic, rollback), Photon Cloud for connectivity and matchmaking, private + public 1v1 matches, no dedicated servers at first, $0/month initial budget, no free-text chat.

**What this implies (important):**
- Quantum runs the game **simulation** in its own deterministic framework: fixed-point math (not `float`), its own ECS-style entities/systems, and its own physics. The simulation **cannot use** Unity components, `Rigidbody`, `Animator`, `Random`, `Time`, or `UnityEngine` physics. Unity is the **view** layer that renders simulation state.
- Therefore, fighting logic that must be synchronized (movement, hit/hurt boxes, block, guard meter, damage, combos, items, hazards, round flow) must eventually exist as Quantum simulation code. Presentation (ragdoll, VFX, camera, audio, UI) stays in Unity and is **never** part of synchronized state.
- Two ways to get there:
  1. **Offline first, port later (decided by the owner):** build the single-player game in plain Unity now, keeping combat rules in pure C# with integer/frame-based data and no `UnityEngine` dependency (as sections 5-6 already require). In M6, re-implement the combat core as Quantum systems, reusing the data (frame data, damage tables) and tests as the spec. The port is real work, but offline progress is not blocked by Quantum setup.
  2. **Quantum from the start:** build the combat core in Quantum immediately (it also runs offline and for AI). It avoids a port but costs setup and learning time up front, which is risky within one week.
- Design habits that keep the port cheap, starting now: fixed 60 Hz tick; frame-count durations (not seconds); integer or fixed-point-friendly math in combat rules; seeded RNG owned by the simulation; no dependence on animator timing or physics-engine results for hit detection; serializable game state; input as a compact `FighterIntent` per tick.
- Ragdoll is cosmetic and can be non-deterministic, as long as hitboxes come from the simulation's logical pose.
- Rollback caveat: visual effects (hit-stop, VFX, audio) must tolerate re-simulation (predicted events can be cancelled/confirmed), which Quantum supports through its event system.
- **Risks to verify before M6 [VERIFY]:** Quantum 3 pricing and free-tier limits (CCU cap, whether commercial launch is covered), Unity 6.3+ compatibility, how Quantum's licensing/SDK is handled in a public repository (do not commit license keys or app ids; keep them in git-ignored config), and mobile network behavior (rollback needs low latency; test on cellular).
- Safety: no free-text chat; fixed emotes only; a report/block option for public matches is recommended.

## 20. Risks & Mitigations
| Risk | Mitigation |
|---|---|
| Ragdoll physics is unstable or expensive on mobile | Prototype partial ragdoll early (M1) on a real device; keep logical hitboxes independent |
| Chaos makes matches feel unfair | Telegraph everything; playtest early; tune data not code |
| Scope creep (many fighters/stages) | Strict milestone scope; data-driven content so extras are cheap |
| Blender pipeline friction (rig/export mismatches) | Define the base rig and export checklist before the first fighter ([`coding_guidelines.md`](coding_guidelines.md) section 9) |
| Material/shader conversion to URP | Convert prototype materials once at project setup; use URP-compatible shaders for all new assets |
| Quantum port is a large rewrite of combat logic | Keep rules in pure C#/frame data from M0; decide in M5 whether to port or to build Quantum-first (section 19) |
| Quantum free tier may not cover a public launch | Verify pricing before committing to M6; budget is $0/month |
| 5-month solo schedule vs. full-game scope | Milestone plan with explicit cut lines; online is the first cut (PRD section 15.1) |
| Ad/IAP policy and consent issues | Isolate behind interfaces; review store policies in M5 |
| I cannot run the Unity Editor here | Agents write scripts and give editor wiring steps; the owner validates in the Editor (see [`AGENTS.md`](../AGENTS.md)) |

The Combat Sandbox builder generates and reuses the BlobbyFighter prefab for both sides. It assigns team, input, and camera references on the scene instances.

The combat sandbox uses layer 8 for the arena floor and layer 9 for fighter hurtboxes. Empty masks on older shared-fighter instances fall back to these layers at runtime. Direct animation sampling uses the serialized clips even if the imported Animator has no controller.

The partial hit reaction starts with a 40-degree rotation on the selected dynamic bone, then blends its simulated pose back into animation over the 0.48-second reaction.

The app pause overlay uses MatchPause to freeze simulation time during a live round. Results records the final match score and offers rematch and menu navigation. Escape toggles pause.

GameSettings loads volume, haptics, layout and quality before the first scene and saves changes through SaveService. SettingsAudioSource applies a music or SFX multiplier to its original volume. Stage warning sources use SFX. MatchTouchControls mirrors the movement and attack groups within the HUD safe area.

SaveService writes version 2 JSON to persistentDataPath/save.json. It flushes a temporary file before atomically moving or replacing the primary file. The last readable primary becomes a backup. Load falls back to that backup or safe defaults. Version 1 flat settings migrate to nested settings. Newer versions are preserved as read-only. Coins and fighter unlocks round-trip without awarding progression yet.

### Combined development sandbox (M4-12)

Core TrainingSandboxRules defines exact scene identities for debug-key access and menu exclusion. SandboxInput caches shortcut permission in Awake; normal input remains available in real arenas. AppStateMachine filters both current/retired test scene ids from serialized and resource-discovered stage choices; AppBootBuilder no longer authors a sandbox choice.

CombinedSandboxContent performs a versioned Editor migration using TrainingSandboxLayout (half-width 15, spacing 2, four copies, cooldown 25). It expands the floor/camera, wires twelve unique rack transforms and explicit fighter/camera references, enables RoundController training mode, backs up old scenes/metadata and retires NewItemSandbox through AssetDatabase. Unsaved scene edits defer automatic migration. Real-arena template builders explicitly reset floor width to 14, training mode/layout/marker and camera defaults so development settings cannot leak into new arenas.

ItemSpawner prewarms a permanent display plus four usable copies per type only when a training layout is assigned in CombatSandbox. PooledItem's dispenser path bypasses pickup expiry and asks the spawner for a separate copy. Normal thrown/deployed TTL remains intact. Rack cooldowns prevent per-step pickup-effect spam; exhausted pools reclaim a deployed copy while preserving held copies and clearing owned statuses. EndRound leaves displays visible but clears active copies; disabling the spawner clears everything. TrainingSandbox constrains roots and fits the camera while a round is active; Fighter's optional training bounds extend launch/pop containment without mutating character or move assets. RoundController suppresses training timeouts, MatchController repeats KO rounds without a match-end result, and MatchHud labels the timer TRAINING. Native behavior is covered by CombinedSandboxTests and CombatItemTests; no agent Unity test pass is claimed.

### Arena difficulty setup (M4-13)

AppFlowView creates three highlighted difficulty buttons on the arena page. AppStateMachine owns the typed AIDifficulty choice and accepts changes only during arena setup, rejecting invalid values and loading/in-match changes. Fresh setup resets Normal; rematch retains the selection in the persistent app root. AttachMatch forwards it through MatchController and RoundController to the opponent AIInput after character configuration and before round one. AIInput uses the existing read-only AIProfile resources; round resets preserve the profile. Direct scene play retains the authored profile or Normal fallback. No save schema or scene asset changes are required; CombatSandbox shortcut isolation is unchanged.

Boot stage choices without an assigned thumbnail load `StagePreviews/<Scene>` from Resources once during installation. The Boot builder assigns the same previews for newly authored Boot scenes; existing serialized choices work without scene edits. Bouncy Castle Fair and Giant Sandbox keep their StageDef thumbnails.

HandGripContent authors/repairs Blobby and Madame Plush sockets from hand-weighted bind vertices, wiring FighterVisual and ItemHolder through Editor APIs for existing prefabs and scenes. ItemHolder caches the visual socket in Awake, while character swaps use SetSocket. PooledItem preserves world scale when attaching to scaled bones and restores authored pool scale when deployed/recycled; no runtime bone-name search is added. Dirty scenes defer migration.

### Mr. Jack boss combat (M4-10)

CharacterDef optionally references read-only BossCombatDef (second-phase move roots/speed, profile, special range/cooldown/bounds). Pure BossPhaseRules provides the living strict-half threshold; Fighter latches the observed phase per round, chooses new move roots/Out clips and restores phase one on reset. Shared hitbox, guard, items, stun, hit-stop and round/KO rules remain authoritative. Surprise stores a target X at startup and moves the gameplay body once at the active boundary; its single ordinary hitbox remains blockable and interruptible. GroundShockwave move data tags the low lid sweep and its cosmetic ring. Existing characters keep null Boss data and unchanged paths.

AIInput consumes the boss profile, sequences four punch/two spring-crank requests, and requests a charged special at data-defined range/cooldown, retaining item/hazard behavior. Boss difficulty is not overridden by normal profiles. BossPresentation uses cached spring references, bounded velocity bend, preauthored warning/shockwave line renderers and audio sources; pause/hit-stop respect presentation and resets clear it. VisualRagdoll includes the four spring joints in the boss's twelve bodies; collision does not drive gameplay hitboxes. AppStateMachine loads the boss resource for opponent selection, rejects him as a player and forces boss setup rules before round one. MrJackContent imports the Generic FBX, atlas and all 45 clips, authors controller links/moves/prefabs/audio and publishes the resource. First import is automatic outside Play Mode; **Wobble Brawl > Characters > Build Mr. Jack** retries authoring. No real arena is rewritten by M4-10.

### Mr. Jack boss content (M4-10)

CharacterDef optionally references a BossCombatDef with phase-two move roots, speed, Boss AI profile, request cooldown/range and bounded pop-up tuning. BossPhaseRules determines strict living health below half with integer arithmetic; Fighter latches the phase for the round, chooses the corresponding clip/move family and movement speed, and never mutates character assets. Reset restores phase one/full gauge; zero HP cannot newly trigger ejection. Surprise startup snapshots target X, clamps to stage bounds, and teleports once at active start to marked ground plus .02 m clearance. The body remains targetable and hit cancellation uses shared combat. MoveDef.GroundShockwave marks the lid-slam's low hitbox/presentation. The spring dive uses ordinary data-driven ForwardSpeed/HopSpeed.

AIInput chooses the boss profile when configured, requests its six-move chain and full-gauge/cooldown special while retaining item/hazard decisions. Normal difficulty setup cannot replace Boss AI. Boot selects the boss only as an opponent and forces chaos options on, with disabled difficulty buttons and an explanatory caption. BossPresentation caches spring/renderer/audio references, yields to pause/hit-stop/KO and clears warning/audio on reset. VisualRagdoll includes the four Spring bones for a twelve-body boss rig. MrJackContent performs revisioned, idempotent import/asset setup with the existing toon/outline pipeline and publishes its revision after the complete prefab. The source builder refuses overwrites and validation checks real action motion, loops, root motion, hidden special poses and windows.

`Tools/tasks/run_unity_tests.py` is a Windows-compatible equivalent of the shell runner for the M4-10 checks. Missing UNITY_PATH returns 3 (pending verification); configured runs require a passing nonempty Unity result XML. It does not convert a missing runner into a pass.

M4-10 uses `MrJackContent` to import the Generic model, copy 45 clips, author a linked preview controller, bind the 2D atlas/toon materials and build visual/fighter/boss data assets. Its installed revision is written last, so an early partial import is upgraded; a clean Editor reload or returning from Play installs missing/outdated content. BossCombatDef holds second-phase move roots, AI profile, speed, special request cooldown/range and teleport bounds. BossPhaseRules handles the pure strict-half threshold; Fighter latches the entered phase for the round, replaces future move roots and chooses the Out clip family without changing shared data. The box-hidden special telegraphs a fixed target before teleport/hit activation. BossPresentation owns serialized rings, audio and four spring references; updates yield during hit-stop/pause/KO. Boss setup rejects normal difficulty changes and forces chaos options, while the player roster stays four fighters.

The cross-platform `Tools/tasks/run_unity_tests.py` accepts the same mode/filter and UNITY_PATH as the Bash runner. It returns 3 when unavailable and requires a real passing XML result with at least one passed test. This permits Windows task availability checks without launching the Editor when UNITY_PATH is unset.

Mr. Jack's standalone fighter prefab carries AIInput as its input source and opponent team 1. ConfigureRound binds the fighter's explicitly paired ItemOpponent as the AI target (falling back to existing scene serialization when no pair is supplied); no scene search is needed.

`Tools/tasks/Complete-M4.ps1` is the Windows completion entry point for implemented M4 tasks. It resolves the repository/pinned Editor, refuses a locked project, sets UNITY_PATH/UNITY_PROJECT_PATH and invokes the existing dependency-aware ticker. Python check commands use the current interpreter; Windows Unity Bash commands dispatch to the equivalent Python runner with their original test mode/filter. Passing requirements and missing-Unity exit code 3 are preserved. The launcher runs no Git mutation.

Unity verification runs retain their XML and Editor log under unique `Logs/TaskVerification/<UTC timestamp>-<id>/` directories on success or failure. The Python runner prints failed test names, messages and short stack traces, or startup-error context if no result was produced. Complete-M4.ps1 accepts `-Tasks M4-01` for a targeted diagnostic retry while retaining the full default task list. Logs remain ignored by Git.
