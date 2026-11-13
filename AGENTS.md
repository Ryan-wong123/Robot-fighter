# AGENTS.md — Instructions for AI Coding Agents

This file tells AI agents (Claude Code and similar) how to work in this repository. Humans can read it too. If anything here conflicts with a direct instruction from the owner, the owner wins; flag the conflict.

## 1. Read First (in this order)
1. [`docs/PRD.md`](docs/PRD.md) — what we are building and why (mobile-first whacky ragdoll brawler).
2. [`docs/architecture.md`](docs/architecture.md) — target design, and how the prototype code maps onto it.
3. [`docs/coding_guidelines.md`](docs/coding_guidelines.md) — the rules your code and assets must follow.
   - For art, content and game-feel work also read [`docs/characters.md`](docs/characters.md) (fighters), [`docs/vfx_style.md`](docs/vfx_style.md) (effects and game feel) and [`docs/maps.md`](docs/maps.md) (stages and hazards), and [`docs/art_prompts.md`](docs/art_prompts.md) when producing or reviewing reference art.
4. This file.

Do not start implementing before you have read the sections relevant to your task.

## 2. Project Snapshot
- **Engine:** Unity **6.3 or newer** with **URP** (pinned to 6000.3.11f1; see [`docs/PRD.md`](docs/PRD.md) section 16). C#.
- **Genre:** 2.5D mobile fighting game; the prototype is a robot-vs-robot mock-up being rebuilt into **WOBBLE BRAWL** (working title).
- **Repo layout today:** the repository root is the Unity project. Prototype assets and their metadata are under `Assets/`. Unity configuration lives in `ProjectSettings/` and `Packages/`.
- **Third party:** `Joystick Pack/` is a vendor asset. Do not edit it in place.
- **Prototype scripts** (`Assets/_Project/Scripts/Legacy/*.cs`) are the legacy baseline. They contain known bugs (see [`docs/architecture.md`](docs/architecture.md) section 2). They are scheduled for replacement, not extension.

## 3. Hard Rules
1. **Branching:** work only on the branch you were assigned (currently `codex/m1-combat-slice`). Never push elsewhere without explicit permission.
2. **Commits:** small, focused, imperative messages. Push only when the owner asks or the task says to. Do not open pull requests unless asked.
3. **Scope discipline:** do exactly what the task says. Note unrelated problems in your summary; do not fix them silently. Refactors and bug fixes are separate commits from features.
4. **No surprise sweeps:** do not mass-rename, reformat, or move files (especially anything with a `.meta` file) unless that is the task.
5. **`.meta` files and GUIDs:** every asset has a `.meta`. Never delete one, never edit a GUID, and when moving or renaming an asset move its `.meta` with it (use `git mv`). Prefer leaving moves to the Unity Editor.
6. **Scenes and prefabs are YAML:** do not hand-edit `.unity`, `.prefab`, `.controller`, `.anim`, or `.mat` files unless the task explicitly requires it and you can state exactly what changed. Prefer C# changes plus clear editor-wiring instructions for the owner.
7. **Never claim you tested in Unity.** You cannot run the Editor in the cloud environment (see section 7).
8. **Secrets and personal data:** never commit secrets, keys, or personal information (including the owner's email). Do not put any model identifier in commits, code comments, or docs.
9. **Third-party content:** do not add assets or packages with unclear licenses. Record the source and license of anything added.
10. **Do not delete** art, animation, or scene assets as "cleanup" without owner approval.

## 3.1 Task Tracking and Self-Verification (required)
- [`docs/task.md`](docs/task.md) is the single source of truth for progress. **Never tick boxes by hand.**
- After implementing a task, **verify it yourself, then tick it with the tool**:
  1. Make sure the task has checks in `Tools/tasks/verify.json` (add them if missing, in the same commit). Prefer checks that prove behavior: EditMode tests for logic (`Tools/tasks/run_unity_tests.sh EditMode <filter>`, needs `UNITY_PATH`), and static checks (`Tools/tasks/checks.py`) for removals and rules.
  2. Run `python3 Tools/tasks/tick_tasks.py --complete <ID>`. It runs the checks and ticks `[x]` only if they all pass.
  3. Exit code 2 means a check failed: fix the code and rerun. Do not edit `verify.json` to weaken a check.
  4. If it reports that the check cannot run here (no Unity) or that no check exists, the task becomes `[~]`. Say so in your summary and give the owner the verification steps.
  5. Commit the code and the updated [`docs/task.md`](docs/task.md) together.
- When the owner says "Do `<ID>`", follow [`docs/prompts.md`](docs/prompts.md) for that task: read the listed docs, confirm every `after:` prerequisite is `[x]` (the tool also enforces this), then implement and verify.
- If new work has no task, add one to [`docs/task.md`](docs/task.md) with the next free id (never renumber), its `after:` list, its checks in `verify.json`, and a [`docs/prompts.md`](docs/prompts.md) entry.
- Never claim a task is verified when its check did not run.

## 3.2 Keep Docs Current (required)
The docs in `docs/` are the project's memory. **In the same commit as any change, implementation, fix or tuning, update every doc it affects.** Do not leave docs for later.
- **Gameplay rules, numbers and decisions** -> [`docs/PRD.md`](docs/PRD.md). Tuned values (damage, block reduction, timers, speeds) must match the docs; update the "(start)" numbers when data changes.
- **Code structure, systems, interfaces, repo layout** -> [`docs/architecture.md`](docs/architecture.md), and the "Repo layout today" and prototype notes in this file when they stop being true.
- **Fighters, effects, stages, hazards, audio** -> [`docs/characters.md`](docs/characters.md), [`docs/vfx_style.md`](docs/vfx_style.md), [`docs/maps.md`](docs/maps.md).
- **Rules and conventions** -> [`docs/coding_guidelines.md`](docs/coding_guidelines.md) and this file.
- **Owner decisions made in chat** are recorded in the relevant doc (and in the PRD decisions list) in the same commit; tasks they create or change go into [`docs/task.md`](docs/task.md), [`Tools/tasks/verify.json`](Tools/tasks/verify.json) and [`docs/prompts.md`](docs/prompts.md).
- **Diagrams:** the schematic SVGs in `docs/images/` are generated by `Tools/docs_art/make_diagrams.py`. When layout numbers, palettes, timings or proportions change, update the generator and re-run it (`python3 Tools/docs_art/make_diagrams.py`); `--check` verifies they are in sync and runs automatically before any task is ticked. Concept art in `docs/images/*/concept/` is owner-provided (prompts to generate it are in [`docs/art_prompts.md`](docs/art_prompts.md)): never invent or overwrite it (the core set is checked by `python3 Tools/tasks/check_concept_art.py`), and never link a file that does not exist.
- Remove or rewrite text that is no longer true instead of adding a contradicting note. Move resolved questions from "open" to "decided".
- Keep cross-links valid. Docs link to each other with relative markdown links; run `python3 Tools/tasks/check_docs.py` after editing docs. `tick_tasks.py --complete` runs this check first and ticks nothing if a link is broken.
- If nothing in the docs is affected, say so in your summary ("docs: no change needed").

## 4. Decisions Already Made by the Owner (do not relitigate)
- Mobile-first. Landscape. Touch controls with joystick.
- Block is a real mechanic and **reduces damage** (PRD 7.3).
- **No revive** — remove it where encountered, never add anything like it.
- Theme: whacky; **ragdoll / cartoon chaos + absurd items and hazards**; characters are **not robots**.
- New characters will be modeled, textured, rigged, and animated in **Blender**.
- The prototype is a mock-up; the goal is a full game.
- Theme and the four-fighter cast are approved. Third-party ragdoll tooling is allowed (check license and mobile cost).
- Solo developer, **5-month deadline**: follow the schedule and cut lines in [`docs/PRD.md`](docs/PRD.md) section 15.1. Prefer placeholders over polish early, and flag scope that threatens the schedule.
- Networking approach is decided: build offline first in plain Unity, then port the combat core to Photon Quantum in M6.
- Voice barks are generated with the open-source **VoxCPM2** model (license and quality still being verified, task D-08). Mr. Jack is AI-only at launch. Classic options (Hazards/Items on-off) ship at launch. Fight-plane width is about 14 m.
- Google Play only for now. Ads: **Unity LevelPlay + Unity Ads network**. IAP: **Unity IAP**.
- Online: **Photon Quantum 3** (deterministic rollback) + Photon Cloud, no free-text chat. Keep combat logic free of `UnityEngine`, frame-based and seeded ([`docs/architecture.md`](docs/architecture.md) section 19).
- CI is GitHub Actions: `unity-ci.yml` (tests + Android build via GameCI; skipped until the Unity project and license secrets exist). Task ticking is local, not CI.
- Git LFS is used for large binaries. Never commit Photon app ids or license keys.
- Monetization is **ads + in-app purchases** (cosmetic-first, no pay-to-win); behind interfaces, never during a round.
- **Do not begin bug fixes or refactors until the owner says go**: the docs come first.

## 5. Workflow for a Task
1. **Restate** the task and which doc sections apply, in 2-3 lines.
2. **Inspect** the actual code. Do not rely on memory of the prototype; file contents change.
3. **Plan** only if the change touches more than ~3 files or alters an architecture boundary; keep it short.
4. **Implement** following [`docs/coding_guidelines.md`](docs/coding_guidelines.md). Keep diffs minimal.
5. **Verify** what you can without the Editor (section 7).
6. **Summarize**: what changed, why, how you verified, what you could not verify, and any editor steps the owner must do (exact component/field names).
7. **Update the docs** your change affects (section 3.2) and run `python3 Tools/tasks/check_docs.py`. If you find a design gap or contradiction, fix the doc in the same change or flag it to the owner.

## 6. Definition of Done
A change is done when:
- It matches the docs, and every affected doc is updated in the same change (section 3.2); `python3 Tools/tasks/check_docs.py` passes.
- C# compiles in principle (no obvious syntax, namespace, or API errors; see section 7).
- No new `GameObject.Find`, `SendMessage`, per-frame allocations, name-string matching, magic numbers, or `Debug.Log` spam (guidelines section 4-6).
- Gameplay rules are in pure C# where possible and covered by an EditMode test when the logic is non-trivial.
- New content is data (ScriptableObject), not hard-coded.
- Editor wiring needed from the owner is listed step by step.
- Nothing outside the task was changed.

## 7. What You Can and Cannot Verify
- **Can:** read and edit C#, markdown, JSON; run static checks, formatters, or standalone .NET compile checks on pure-logic classes (code with no `UnityEngine` dependency); run Blender in **background mode** if available (`blender -b ... --python script.py`) for export and validation scripts; grep for forbidden patterns.
- **Cannot:** open the Unity Editor, enter Play mode, run Unity's test runner, or visually inspect scenes. Say so explicitly. Do not present untested behavior as confirmed.
- When Unity is required, provide a **verification checklist** for the owner (what to click, what to expect).

## 8. Suggested Agent Roles (for parallel/delegated work)
Use one role per task to avoid conflicting edits. Roles must not edit each other's owned areas without coordination.

| Role | Owns | Typical tasks |
|---|---|---|
| **Gameplay** | `Scripts/Gameplay`, `Scripts/Data` | Fighter parts, state machine, DamageResolver, moves, block, guard meter |
| **Flow & UI** | `Scripts/Flow`, `Scripts/UI`, `Scripts/Save` | Match/round flow, HUD views, menus, settings, save data |
| **Chaos (Items/Hazards)** | items, hazards, stage data | ItemDef/Effects, ItemSpawner, HazardController |
| **AI** | `Scripts/Gameplay/AI` | AIInput, profiles, difficulty tiers |
| **Art Pipeline** | Blender scripts, export validation, import settings docs | bpy scripts, naming/scale checks, rig templates |
| **QA/Tests** | `Tests/` | EditMode/PlayMode tests, sandbox scenes, checklists |
| **Docs** | `*.md` | Keeping the PRD/architecture/guidelines current |

Rules for multi-agent work:
- Define interfaces first (e.g., `IFighterInput`, `HitInfo`), then let roles implement against them.
- One agent edits a given file at a time. If two tasks need the same file, serialize them.
- Hand-offs are written: what was done, what the next agent needs, open questions.

## 9. Blender Pipeline Notes for Agents
Authoritative conventions live in [`docs/coding_guidelines.md`](docs/coding_guidelines.md) section 9. In short:
- Scripts go in `Tools/Blender/` and must run headless and idempotently.
- Never overwrite the owner's `.blend` sources without a backup or explicit instruction; write exports to the character's export folder.
- Always validate before export: scale 1, applied transforms, forward/up axes, bone naming, poly/bone budget, single armature.
- Procedural modeling/texturing by script is acceptable for blockouts and placeholders; final-quality art direction decisions stay with the owner.

## 10. When to Ask the Owner
Ask (concisely, with a recommendation) when:
- A choice changes the game design (rules, numbers beyond "start" values, monetization).
- You need an unrecorded fact: exact Unity patch version, input package, target devices.
- A change would touch scenes/prefabs/animator controllers in a way you cannot do safely from code.
- Docs conflict with each other or with the code.

Otherwise, pick the conventional default, state it in your summary, and continue.

## 11. Communication Style
- Be concise and concrete. Lead with the result.
- Report failures and skipped steps honestly, with output.
- Reference code as `path/to/file.cs:line`.
- Prefer a recommendation over a menu of options.
