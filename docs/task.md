# task.md — Project Task Tracker

Single source of truth for what is done. **Do not tick boxes by hand.** A box is ticked by the agent only after it verifies the implementation works (see "How ticking works").

## How ticking works
A task is ticked **only when the agent has verified that its implementation works**. Nobody ticks boxes by hand.
- Each task can have checks in `Tools/tasks/verify.json`: static checks (`Tools/tasks/checks.py`) or Unity tests (`Tools/tasks/run_unity_tests.sh`). All must exit 0.
- After implementing a task, the agent (Claude Code / Codex) runs `python3 Tools/tasks/tick_tasks.py --complete <ID>`. The checks run, and `[x]` is set only if they all pass. The agent commits [`task.md`](task.md) together with the code.
- Check fails: nothing is ticked and the command exits 2. The agent fixes the code and reruns.
- Check cannot run here (for example, Unity is not installed) or the task has no automated check: the task becomes `[~]` at most, and the owner confirms it.
- **Order:** a task may end with `(after: M0-01, M0-05)`. The tool refuses to complete it until every prerequisite is `[x]` (a `[~]` prerequisite is not enough). The list order follows the intended build order, and `tick_tasks.py --check` rejects unknown ids and cycles.
- **Starting a task:** tell the agent "Do `<ID>`". [`prompts.md`](prompts.md) has the standard prompt plus per-task notes (what to read, what is owner-only, what "done" means).
- A task never goes backwards automatically. Task IDs are permanent; never renumber. Add new tasks with the next free id, their `after:` list, their checks in `verify.json` and a [`prompts.md`](prompts.md) entry, all in the same commit.
- Milestones and cut lines: [`PRD.md`](PRD.md) section 15.1.

Legend: `[ ]` open, `[~]` code done but not verified here, `[x]` done and verified

## Planning & Docs
- [x] **D-01** PRD.md written
- [x] **D-02** architecture.md written
- [x] **D-03** AGENTS.md written
- [x] **D-04** coding_guidelines.md written
- [x] **D-05** Owner decisions recorded (Unity 6.3+, URP, LevelPlay/Unity Ads, Unity IAP, Google Play, Photon Quantum 3, LFS, no free chat)
- [x] **D-07** characters.md, vfx_style.md and maps.md written
- [x] **D-09** Docs link check tool and the "Keep Docs Current" rule added (AGENTS.md section 3.2, guidelines checklist)
- [x] **D-10** Schematic diagrams (stage layouts, cast lineup, ragdoll bodies, VFX sheets) generated and embedded in characters.md, maps.md and vfx_style.md
- [x] **D-11** Owner supplies concept art and reference images (fighter turnarounds, stage color keys, VFX references) into docs/images/*/concept/ with sources (owner)
- [x] **D-12** art_prompts.md written (prompt pack for the fighter, stage and VFX reference art)
- [ ] **D-08** Verify VoxCPM2: model and weights license, commercial use and output rights, hardware needs, sample quality, and Google Play AI-content policy (owner)
- [ ] **D-06** Verify Photon Quantum 3 licensing, free-tier limits and Unity 6.3+ compatibility (owner)

## CI & Tooling
- [x] **CI-01** task.md tracker script (verify, then tick)
- [x] **CI-02** Unity CI workflow (EditMode/PlayMode tests, Android build; gated until the Unity project exists)
- [ ] **CI-03** Add GameCI secrets in GitHub (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`) and confirm the Unity 6 activation method (owner) (after: M0-01)
- [ ] **CI-04** Android signing: keystore stored as a GitHub secret, release build workflow verified (owner) (after: CI-03)
- [ ] **CI-05** First green Unity test run in CI (after: CI-03, M0-16)

## M0 — Foundations (weeks 1-2)
- [x] **M0-01** Unity 6.3+ project set up in this repo (`ProjectSettings/`, `Packages/`, pinned version) (owner, Editor)
- [x] **M0-02** `.gitignore` for Unity added (after: M0-01)
- [x] **M0-03** Git LFS configured (`.gitattributes`) for fbx/blend/textures/audio (after: M0-01)
- [x] **M0-04** Convert project to URP and convert materials (owner, Editor) (after: M0-01)
- [ ] **M0-18** Toon shader (Shader Graph) with outline method chosen and set up in URP; test material on a fighter placeholder (after: M0-04)
- [ ] **M0-19** Measure the arena and jump in Unity; set fight-plane width (about 14 m), ceiling, gravity and jump height as data; update maps.md (owner, Editor) (after: M0-01)
- [x] **M0-05** Move vendor assets to `ThirdParty/` and adopt the target folder layout + assembly definitions (after: M0-01)
- [x] **M0-16** EditMode test assembly set up with a first passing test (also used by CI) (after: M0-05)
- [x] **M0-06** Remove revive code (after: M0-05)
- [x] **M0-07** Fix: AI uses all four attacks (`Kick_Left` was unreachable) (after: M0-05)
- [x] **M0-08** Fix: remove `NotImplementedException` from button handlers (after: M0-05)
- [x] **M0-09** Fix: jump check (no double jump bug) and floor reset (after: M0-05)
- [x] **M0-10** Fix: throw direction via explicit facing, not float `==` (after: M0-05)
- [x] **M0-11** Fix: crate/item spawn timer (after: M0-05)
- [x] **M0-12** Fix: sudden death for tied timeouts; event-based round end (after: M0-16)
- [x] **M0-13** Real block: held state, 75% reduction, pushback, guard meter and guard break (after: M0-16)
- [x] **M0-14** `IFighterInput`/`FighterIntent` + `TouchInput` (replaces `Controller` and `*Btn`) + keyboard input for editor (after: M0-05)
- [x] **M0-15** New M0 code (input, block, round flow) has no `GameObject.Find`/`SendMessage`/name matching (after: M0-13, M0-14)
- [ ] **M0-17** Prototype plays correctly on an Android device (owner) (after: M0-04, M0-06, M0-07, M0-08, M0-09, M0-10, M0-11, M0-12, M0-13, M0-14, M0-15, M0-16)

## M1 — Combat slice (weeks 3-5)
- [ ] **M1-01** `Fighter` base + parts (state machine, health, guard meter, special gauge, motor) (after: M0-17)
- [ ] **M1-02** `DamageResolver` (pure C#, frame/integer based) + tests (after: M0-16)
- [ ] **M1-03** `MoveDef` / `CharacterDef` ScriptableObjects; combos from data (after: M1-02)
- [ ] **M1-04** `Hitbox`/`Hurtbox` components and layers; replace name-based hit detection (after: M1-01)
- [ ] **M1-05** Hit-stop service (after: M1-01)
- [ ] **M1-06** Squash-and-stretch component + comic-text popups (after: M1-05)
- [ ] **M1-07** Match/Round controllers (best-of-3) and events (after: M1-01, M1-02)
- [ ] **M1-08** Shared Fighter used by AI (`AIInput`) with a basic AI (after: M1-01)
- [ ] **M1-09** Partial ragdoll on hit (after: M1-01, M1-11)
- [ ] **M1-10** Full ragdoll KO with slow-mo and camera punch-in (after: M1-09)
- [ ] **M1-11** Blobby modeled, textured and rigged in Blender (owner/art pipeline) (after: M0-01, M0-18)
- [ ] **M1-12** Blobby animation set (idle, walk, jump, block, 4 punch, 2 kick, special, hit-react, knockdown, get-up, win, taunt) (after: M1-11)
- [ ] **M1-13** Blender export validation script + import settings documented (after: M1-11)
- [ ] **M1-14** Blobby playable 1v1 vs AI at 60 FPS on a mid-range phone (owner) (after: M1-07, M1-08, M1-10, M1-12, M1-13)

## M2 — Chaos layer (weeks 6-8)
- [ ] **M2-01** `ItemDef`/`ItemEffect`, `ItemHolder`, seeded `ItemSpawner`, pooling (after: M1-01, M1-04)
- [ ] **M2-02** Six items implemented (banana peel, foam glove, rubber chicken, rocket plunger, whoopee cushion, anvil) (after: M2-01)
- [ ] **M2-03** `HazardDef` + `HazardController` (telegraph, active, cooldown) (after: M1-07)
- [ ] **M2-04** Stage 1: Bathtub Brawl (art + hazard) (after: M2-03)
- [ ] **M2-05** Stage 2: Kitchen Counter (art + hazard) (after: M2-03)
- [ ] **M2-06** Comic/impact VFX set (after: M1-06)
- [ ] **M2-07** AI difficulty tiers (`AIProfile`) and item/hazard awareness (after: M1-08, M2-01, M2-03)
- [ ] **M2-08** Playtest: chaos feels fair (owner) (after: M2-02, M2-05, M2-07)
- [ ] **M2-09** No `GameObject.Find`/`FindObjectOfType`/`SendMessage` left anywhere in project scripts, prototype code included (after: M1-01, M1-07, M2-01)

## M3 — Game shell (weeks 9-10)
- [ ] **M3-01** App state flow and boot scene (after: M1-07)
- [ ] **M3-02** Main menu (after: M3-01)
- [ ] **M3-03** Fighter select and stage select (after: M3-02, M1-03)
- [ ] **M3-04** Match HUD (event-driven health, guard, special, timer, round pips) (after: M1-07)
- [ ] **M3-05** Results screen and pause menu (after: M3-04)
- [ ] **M3-06** Settings (volume, haptics, control layout) (after: M3-02)
- [ ] **M3-07** `SaveService` (versioned JSON, atomic write) + tests (after: M0-16)
- [ ] **M3-08** `AudioManager` with buses; placeholder SFX/music (after: M3-01)
- [ ] **M3-09** Voice pipeline: clip list, VoxCPM2 generation script, edit/export steps and per-clip license record in `Tools/Audio/` (after: D-08, M3-01)
- [ ] **M3-10** Match rules: Hazards on/off and Items on/off toggles in match setup (Classic options) (after: M2-01, M2-03, M3-03)

## M4 — Content (weeks 11-14)
- [ ] **M4-01** Sir Noodle (model, rig, animations, data) (after: M1-14)
- [ ] **M4-02** Puffin Pete (model, rig, animations, data) (after: M1-14)
- [ ] **M4-03** Madame Plush (model, rig, animations, data) (after: M1-14)
- [ ] **M4-10** Mr. Jack boss fighter (model, rig, two-phase animations, boss AI profile, voice barks) (after: M1-14, M2-07)
- [ ] **M4-04** Stage 3: Bouncy Castle Fair (after: M2-03)
- [ ] **M4-05** Stage 4: Giant Sandbox (after: M2-03)
- [ ] **M4-11** Boss stage: Toy Chest Showdown (art + Block Rain and Music Box Tilt hazards) (after: M2-03)
- [ ] **M4-06** Items up to 12 (after: M2-02)
- [ ] **M4-07** Arcade Ladder mode (after: M3-03, M1-08, M4-10, M4-11)
- [ ] **M4-08** Unlocks and coin economy (`EconomyService`) (after: M3-07)
- [ ] **M4-09** Music, SFX and fighter voice barks final pass (licenses recorded) (after: M3-08, M3-09)

## M5 — Monetization & launch (weeks 15-16)
- [ ] **M5-01** `IAdService` + LevelPlay integration (Unity Ads network), editor fake (after: M3-01)
- [ ] **M5-02** Rewarded ad placements; capped interstitials on results screen only (after: M5-01, M5-04, M3-05)
- [ ] **M5-03** `IPurchaseService` + Unity IAP; products, Remove Ads, Restore Purchases (after: M3-07, M4-08)
- [ ] **M5-04** `ConsentService` (consent/age gate before first ad request) (after: M5-01)
- [ ] **M5-05** Google Play: data-safety form, content rating questionnaire, target-audience declaration (owner) (after: M5-03)
- [ ] **M5-06** Analytics and crash reporting (after: M3-01)
- [ ] **M5-07** Performance pass on a low-end device (draw calls, GC, physics, thermals) (after: M4-05, M4-10, M4-11)
- [ ] **M5-08** Store listing assets (icon, screenshots, feature graphic, description) (owner) (after: M4-05, M4-10, M4-11)
- [ ] **M5-09** Closed testing on Google Play (owner) (after: M5-02, M5-03, M5-07)
- [ ] **M5-10** Release candidate AAB signed and uploaded (owner) (after: M5-09, CI-04)

## M6 — Online (stretch, weeks 17-19)
- [ ] **M6-01** Decide port path and confirm Quantum licensing (see D-06) (after: D-06, M5-10)
- [ ] **M6-02** Quantum project set up; combat core ported to Quantum systems using M1 data and tests as spec (after: M6-01, M1-02)
- [ ] **M6-03** Photon Cloud connection, private room codes (friends) (after: M6-02)
- [ ] **M6-04** Public matchmaking (random opponent) (after: M6-03)
- [ ] **M6-05** Reconnect handling, fixed emotes only (no free text) (after: M6-04)
- [ ] **M6-06** Online playtest on cellular (owner) (after: M6-05)

## Release
- [ ] **R-01** Submitted to Google Play production (week 20) (owner) (after: M5-10)
