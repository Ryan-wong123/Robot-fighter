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
| Repo | Repo root == Assets contents; no `ProjectSettings`/`.gitignore` | Unity project settings unversioned; no layout standard |

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
Move into a standard Unity project (`Assets/` root, `ProjectSettings/`, `Packages/`) **[VERIFY: depends on how the owner keeps the project]**.

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

## 8. Items & Hazards

### 8.1 Items
```csharp
[CreateAssetMenu] public class ItemDef : ScriptableObject {
    public string Id; public GameObject Prefab; public ItemUseMode Mode; // OnPickup | Thrown | Placed | Held
    public ItemEffect[] Effects;
}
public abstract class ItemEffect : ScriptableObject { public abstract void Apply(ItemContext ctx); }
```
- `ItemSpawner` picks from a weighted table with a **seeded RNG**; spawn points come from the stage.
- `ItemHolder` on the fighter manages pickup (trigger), held state, and use/throw. The facing direction comes from the fighter's explicit `Facing` value, not rotation float comparisons.
- Items are pooled.

### 8.2 Hazards
```csharp
[CreateAssetMenu] public class HazardDef : ScriptableObject { /* telegraph time, active time, cooldown, effect */ }
```
A `HazardController` runs a Telegraph -> Active -> Cooldown cycle with events, so UI/audio/VFX can warn the player (>= 1.5 s). Hazards apply effects through the same `HitInfo`/status pipeline as attacks so damage rules remain centralized.

### 8.3 Stages
`StageDef` references the scene/prefab, spawn points, item table, hazard list, camera bounds, music.

## 9. Match Flow
```
AppStateMachine:  Boot -> MainMenu -> Selection -> Match -> Results -> MainMenu
MatchController:  holds best-of-3 state, creates fighters from CharacterDefs, owns MatchResult
RoundController:  Intro -> Fight -> (KO | TimeUp | SuddenDeath) -> RoundEnd
```
- `MatchRules` (`HazardsEnabled`, `ItemsEnabled`; the Classic options, see [`maps.md`](maps.md) section 5) is part of the match setup and is read by `HazardController` and `ItemSpawner`. It must be part of the synchronized match config for online play.
- Round end is evaluated **on the event** (`OnFighterKO`, `OnTimerExpired`), not by polling once a second.
- Tie at timeout enters **sudden death**; there is no unresolved state.
- `MatchController` emits events (`RoundStarted`, `RoundEnded`, `MatchEnded`); HUD/audio/results subscribe.

## 10. AI
- `AIInput : IFighterInput` driven by an `AIProfile` ScriptableObject (reaction time, aggression, block chance, mistake rate, item-use weight).
- Decision layer: a lightweight utility/state selection (approach, poke, block, punish, use item, avoid hazard) sampling the world through a read-only `FighterView` (distance, opponent state, active hazards). No direct access to the opponent's internals beyond what a human could perceive.
- Randomness is seeded for reproducibility in tests.

## 11. Camera
Evolves `MoveCamera`: frames all fighters, clamps to stage bounds, adds shake, punch-in on KO, and a bounded zoom. (The current `/4` midpoint math looks like a bug and should be rechecked when migrating.)

## 12. UI
- HUD is driven by **events/ViewModels**: `HealthBarView` listens to `FighterHealth.Changed`; no `GameObject.Find`.
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
