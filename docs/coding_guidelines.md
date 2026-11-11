# Coding Guidelines — WOBBLE BRAWL

Applies to all C#, shaders, Blender scripts, and assets in this repo. Companion to [`architecture.md`](architecture.md) (design) and [`AGENTS.md`](../AGENTS.md) (agent workflow).
These rules target a **mobile, 60 FPS, data-driven** Unity project. Target: **Unity 6.3+ with URP**. Where a rule is **[VERIFY]**, confirm it against the exact installed version.

## 1. Principles
1. **Clarity over cleverness.** Code is read far more than written.
2. **Data over code.** Tuning numbers, moves, items, and stages live in ScriptableObjects, not in `if` chains.
3. **Decouple.** Systems talk through interfaces and events. No hidden global state.
4. **Mobile-safe by default.** Think about allocations, physics cost, and draw calls while writing, not after.
5. **Small, reviewable changes.** One concern per commit.

## 2. Naming & Formatting (C#)
| Item | Style | Example |
|---|---|---|
| Classes, structs, enums, methods, properties, events | `PascalCase` | `DamageResolver`, `OnHitResolved` |
| Interfaces | `IPascalCase` | `IFighterInput` |
| Private/protected fields | `_camelCase` | `_currentState` |
| Public fields | avoid; use properties. (Serialized data classes may use `PascalCase` fields.) | |
| Locals, parameters | `camelCase` | `hitInfo` |
| Constants | `PascalCase` | `MaxComboHits` |
| Enums | singular type, `PascalCase` members | `HurtZone.Legs` |
| Files | one top-level type per file, filename = type name | `Fighter.cs` |

- 4-space indent, braces on their own line (Allman), UTF-8 **without BOM**, LF line endings.
- One statement per line; always use braces on `if/else/for/while`.
- Line length: soft limit 120.
- Namespaces: `WobbleBrawl.<Layer>.<Feature>`, e.g. `WobbleBrawl.Gameplay.Combat`.
- Use `var` only when the type is obvious on the right side.
- Name booleans as questions: `IsBlocking`, `CanJump`, `HasItem`.
- Avoid abbreviations except well-known ones (HP, UI, AI, VFX, SFX).
- The prototype has typos in names (`GameMangaer`, `TakingHIt`). Do not copy them into new code.

## 3. C# Conventions
- Prefer `readonly`, `const`, `static readonly` where applicable; prefer immutable structs for small data (`HitInfo`).
- Prefer composition over inheritance. Use a base class only for genuine shared behavior (avoid deep hierarchies).
- No public mutable state exposed across layers; expose read-only properties and events.
- Events: use C# `event Action<T>` (or the project `EventBus`) and **always unsubscribe** in `OnDisable`/`OnDestroy`.
- Avoid `null` as a signal: use explicit states or `TryGet...` patterns. Null-check Unity objects with `== null` / `bool` conversion semantics in mind.
- Avoid LINQ, `foreach` over non-array collections, boxing, string concatenation, and lambdas capturing locals in gameplay hot paths (see section 5).
- Throw/assert on programmer errors at boundaries; fail loudly in development builds (`Debug.Assert`, custom guard helpers) rather than swallowing.
- Use `[DisallowMultipleComponent]`, `[RequireComponent]`, and `[SerializeField]` appropriately.

## 4. Unity Rules (hard rules)
**Forbidden in new code:**
- `GameObject.Find`, `FindObjectOfType`, `FindWithTag`, `transform.Find` by name at runtime (other than one-time editor tooling). Use serialized references, injection, or a bootstrapper. *(Prototype violations: `GameManager`, `PlayerBehaviour`, `Throwables`, `UI` lookups.)*
- `SendMessage`, `SendMessageUpwards`, `BroadcastMessage`. Use interfaces, events, or direct references. *(Prototype: `TakeHitOpponent`/`TakeHitPlayer`.)*
- Matching gameplay behavior on **object names** (`c.name == "Opponent_Body"`). Use components (`Hurtbox`) and enums.
- Matching on **tags/strings** for gameplay logic when a component or layer works.
- Comparing floats with `==` (e.g., `eulerAngles.y == 90`). Use an explicit facing value or `Mathf.Approximately`.
- `throw new NotImplementedException()` left in shipping paths. *(Prototype: the `*Btn` pointer handlers.)*
- Per-frame `Debug.Log`. Use the logging wrapper (section 6) with levels, stripped from release builds.

**Required:**
- Cache component references in `Awake`/`Start`; never call `GetComponent` per frame.
- Expose tunables via `[SerializeField] private` fields or ScriptableObjects, with `[Tooltip]` and `[Range]` where useful.
- Physics work and Rigidbody changes in `FixedUpdate`; input sampling and visuals in `Update`/`LateUpdate`. Do not mix `Time.deltaTime` into `FixedUpdate` code; use `Time.fixedDeltaTime` (or neither).
- Use `Rigidbody.MovePosition`/forces/velocity for physics bodies, not `transform.Translate` (**[VERIFY]** against the final movement approach). Note: the prototype's `Translate`-based player bypasses physics.
- Use `Animator.StringToHash` constants for animator parameters. The animator is a **view**; gameplay logic must not depend on animator transition timing.
- Use animation events **only** for: enabling/disabling hitboxes, SFX, VFX cues. Name the receiving methods clearly.
- Use `Layers` + the collision matrix for filtering. Never rely on name or tag checks as the main filter.
- Prefabs for everything instantiated. Pool anything spawned repeatedly.
- No logic in `OnGUI`. Use Unity UI.
- `ScriptableObject` assets are **read-only data at runtime**. Never mutate shared SO assets in play mode; copy to a runtime instance.

## 5. Performance & Mobile Rules
- **No allocations in per-frame gameplay paths.** Use `NonAlloc` physics queries, reuse buffers, avoid `new` for reference types in `Update`, cache strings and delegates.
- Pool VFX, comic-text popups, items, and projectiles (`Pool<T>`). Do not `Instantiate`/`Destroy` in combat paths.
- Cap simultaneous particles and ragdolls (see [`architecture.md`](architecture.md) section 14).
- Keep the physics layer collision matrix minimal. Use simple primitive colliders on ragdoll bones. Keep solver iterations low. Let bodies sleep.
- Avoid `Camera.main` in loops; cache it. Avoid per-frame `Find`-style lookups (see section 4).
- Textures: atlas where possible, compress per platform (ASTC on mobile), power-of-two for non-ASTC. Mesh: LODs unnecessary at the target tri counts; use GPU instancing/SRP batcher **[VERIFY]**.
- Use object **visibility/culling** and disable off-screen behaviours where relevant.
- Measure before optimizing: attach Profiler captures to any perf-related change. Test on a real low-end device each milestone.

## 6. Data, Constants & Logging
- **No magic numbers.** Gameplay values (HP, damage, speeds, timers) come from `CharacterDef`/`MoveDef`/config assets. Genuine invariants go in named `const`s. *(Prototype: hard-coded `200f`, `10f`, `0.5f`, `36f`...)*
- Numbers marked **(start)** in [`PRD.md`](PRD.md) are defaults for data assets, expected to change by tuning.
- ScriptableObject definitions are validated in `OnValidate` (e.g., Active frames within clip length; ids unique).
- Stable string ids (`Id` fields) for save data and analytics; never use display names as keys.
- **Logging:** use a thin `Log` wrapper (levels: Debug/Info/Warn/Error) compiled out of release builds via `[Conditional]`. No spam: no per-frame logs.
- **Save data:** versioned JSON in `Application.persistentDataPath`, atomic write (temp file then rename), migrations for old versions, safe defaults on corruption. Never use `PlayerPrefs` for structured progress.

## 7. Architecture Rules (see [`architecture.md`](architecture.md))
- **Layering:** `Core` <- `Data` <- `Gameplay` <- `Flow` <- `UI`. Lower layers never reference higher layers. Enforce with assembly definitions.
- **Input:** gameplay reads an `IFighterInput`/`FighterIntent`, never `UnityEngine.Input` or the joystick directly.
- **Combat rules** live in pure C# (`DamageResolver`, `GuardMeter` logic) with no `UnityEngine` dependency when possible. Components are thin adapters.
- **Ragdoll:** logical hitboxes follow the animated pose; ragdoll is visual and positional feedback, never the source of truth for hit detection.
- **UI** reads gameplay state through events/view-models. Gameplay never references UI.
- Do not add singletons with mutable global state. Use the bootstrapper or explicit service references.
- Third-party code (`ThirdParty/`, currently `Joystick Pack`) is not edited; wrap it.

### 7.1 Simulation-ready combat code (for the later Photon Quantum port)
- Combat rules (damage, block, guard, combos, round state) use **integers/frame counts** and a seeded RNG owned by the match, not `float` seconds, `Time`, `Random`, or physics results.
- No `UnityEngine` types in combat rule classes (use own small structs for vectors if needed).
- Durations in `MoveDef` are frames at 60 Hz.
- Keep rule classes free of side effects so they can be replayed from a snapshot.

## 8. Testing
- **EditMode tests** (NUnit via Unity Test Framework) for all pure logic: damage/block rules, combo scaling, guard meter, state transitions, round/match flow, item weighting, save migration.
- **PlayMode smoke tests** in a `Sandbox_Match` scene: two scripted AI fighters, run N seconds, assert a round always resolves.
- Tests are deterministic: seed RNGs, no real-time sleeps (use fixed ticks).
- Every bug fix adds a regression test when practical.
- Test names: `Method_Scenario_ExpectedResult`, e.g. `Resolve_BlockedBodyHit_Deals25PercentDamage`.
- A change that alters combat numbers must update/expand tests and note it in the commit message.

## 9. Blender & Art Pipeline
Characters are modeled, textured, rigged, and animated in Blender, then exported to Unity.

**Files**
- Sources: `Art/_Source/<Character>/<Character>.blend` (not imported by Unity; keep out of `Assets` if possible, or exclude from import). Large binaries via Git LFS.
- Exports: `Art/Characters/<Character>/` containing `<Character>.fbx` (or glTF), `Textures/`, `Materials/`.
- Helper scripts: `Tools/Blender/*.py`, runnable headless (`blender -b file.blend --python script.py`), idempotent, no hard-coded absolute paths.

**Naming**
- Objects: `PascalCase` with a prefix by type where useful: `SM_` static mesh, `SK_` skinned mesh, `ARM_` armature.
- Bones: `Pascal_Case.L/.R` Blender convention in the source; the export must produce **consistent, predictable** names that match the rig template (see below). Never rename bones after animations exist without re-exporting everything.
- Animation actions: `<Character>_<Name>` (e.g., `Blobby_Punch1`); one action per clip; names match `MoveDef.AnimTrigger`/clip ids.
- Materials/textures: `<Character>_<Part>_<Map>` (e.g., `Blobby_Body_Albedo`, `_Normal`, `_Mask`).

**Rig & mesh requirements**
- Scale 1 Blender unit = 1 m, character about 1.7-2.0 m tall (stylized proportions allowed, but consistent across the cast).
- Apply all transforms (location/rotation/scale) before export; origin at the feet, character faces one agreed forward axis in Blender. The export script fixes the forward/up axis settings so the character faces **+Z** in Unity (verify on the first exported fighter and record the result here).
- **One armature**, <= 30 deform bones, a single root bone, no non-uniform scale on bones.
- Ragdoll-ready hierarchy: pelvis, spine chain, head, upper/lower arms and legs, hands/feet. Joint limits are set in Unity but the **bone orientation must be consistent** (twist axis along the bone).
- Budget: <= 8-12k tris, 1-2 materials, one 1024 texture atlas per character **(start)**. Weights normalized, max 4 influences per vertex.
- A shared **base rig/template** blend file is the starting point for humanoid-ish fighters so animations can be reused; fighters with unique anatomy extend the template and document deviations.
- **Required clips per fighter:** idle, walk, jump (rise/fall/land), block (loop), punch 1-4, kick 1-2, special, hit-react light/heavy, knockdown, get-up, win, taunt. Looping clips must loop cleanly.
- Attack animations are authored with the **frame data** from `MoveDef` in mind (startup/active/recovery) and have marked frames for hitbox enable/disable animation events.
- Animation is authored **in place** (no root motion) unless a move explicitly requires displacement, which is then data-defined.
- **Art style:** flat toon shading with outlines through one shared URP toon shader (Shader Graph): see [`characters.md`](characters.md) section 3.4. Paint flat colors with no baked lighting; keep normals consistent so the outline pass does not break at hard edges.
- Squash-and-stretch is allowed in animation; keep deformations moderate so hitboxes remain readable.

**Pre-export validation** (script in `Tools/Blender/validate_export.py`): checks scale, applied transforms, single armature, bone count/naming, tri budget, UVs present, materials count, orphan data. Export fails with a readable report if any check fails.

**Unity import settings**
- Rig type per the rig template (Humanoid where it fits, Generic for unusual anatomy). Avatar masks for upper-body attacks as needed.
- Animation compression: keyframe reduction on; check that loops close.
- Textures: compression per platform; no uncompressed textures on mobile.

## 10. Git & Commits
- Branch from the assigned branch only; do not push elsewhere.
- Commit messages: imperative mood, <= 72-char subject, blank line, then a body explaining *why* when non-obvious. Examples: `Add DamageResolver with block reduction`, `Remove revive code from PlayerBehaviour`.
- One logical change per commit. Keep refactors, bug fixes, and features separate.
- **Tasks:** after implementing a [`task.md`](task.md) item, verify it and tick it with `python3 Tools/tasks/tick_tasks.py --complete <ID>`; it ticks only if the task's checks in `Tools/tasks/verify.json` pass. Commit [`task.md`](task.md) with the code. Never edit checkboxes by hand or weaken a check.
- Do not commit: `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, builds, IDE folders, secrets, or personal data.
- Do commit: all `.meta` files, `ProjectSettings/`, `Packages/manifest.json` and `packages-lock.json`.
- Scene/prefab merges are painful: avoid two people editing the same scene; prefer prefabs and additive scenes. Request owner coordination for scene edits.
- Large binaries (FBX, textures, audio, `.blend`) go through **Git LFS** (decided). Track patterns in `.gitattributes`; do not add large binaries to a commit before LFS is configured.
- **Secrets:** Photon app ids, LevelPlay/Unity Ads keys, and any license keys live in git-ignored config, never in the repo.

## 11. Comments & Documentation
- Comment **why**, not what. Prefer self-explanatory names over narration comments.
- XML doc comments (`///`) on public APIs of Core/Data/Gameplay.
- Remove dead and commented-out code; version control remembers it. *(Prototype: many commented-out blocks.)*
- **Keep the docs current.** Every change, implementation, fix or tuning updates every doc it affects in the same commit: [`PRD.md`](PRD.md) (rules, numbers, decisions), [`architecture.md`](architecture.md) (structure, repo layout), [`characters.md`](characters.md), [`maps.md`](maps.md) and [`vfx_style.md`](vfx_style.md) (content), and these guidelines. Owner decisions made in chat are recorded too. See `AGENTS.md` section 3.2 ([`../AGENTS.md`](../AGENTS.md)).
- Keep cross-links valid and run `python3 Tools/tasks/check_docs.py` after editing docs (the task tool runs it automatically).
- TODOs include an owner or issue reference: `// TODO(name): ...` and are tracked, not abandoned.

## 12. Review Checklist
Before considering a change done:
- [ ] Follows layering and uses interfaces/events instead of `Find`/`SendMessage`
- [ ] No name/tag-string gameplay logic, no float `==`, no magic numbers
- [ ] No per-frame allocations or logs in hot paths; pooling used for spawned objects
- [ ] Tunables are in data assets; new content needs no code
- [ ] Unsubscribes from events; cleans up in `OnDisable`/`OnDestroy`
- [ ] Pure logic has EditMode tests; bug fixes have regression tests
- [ ] `.meta` files intact; no stray scene/prefab YAML edits
- [ ] Every affected doc updated in this commit (or "no doc change needed" stated); `check_docs.py` passes
- [ ] Editor wiring steps listed for the owner
- [ ] Commit is focused and the message explains why
