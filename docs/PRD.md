# PRD — WOBBLE BRAWL (working title)

> Status: DRAFT v0.1. Numbers marked **(start)** are tuning starting points, not commitments.
> Items marked **[PROPOSED]** are my suggestions awaiting owner approval. Items marked **[OPEN]** are unanswered questions (see section 16).

## 1. Summary
A mobile-first, 2.5D brawler in the spirit of Street Fighter, rebuilt around **cartoon chaos**: floppy, ragdoll-driven fighters, exaggerated hit reactions, and a stream of **absurd items and stage hazards** that make every match unpredictable. It replaces the current robot prototype ("Robot-fighter"), which is a single-round mock-up against one AI opponent.

## 2. Vision & Pillars
1. **Floppy is funny.** Every hit should look and feel ridiculous: squash-and-stretch, ragdoll flops, comic text, hit-stop. A KO should be the best part of the match.
2. **Chaos with fairness.** Items and hazards create wild swings, but the core fighting (spacing, blocking, combos) stays readable and skill-based. Chaos must be telegraphed, never random-unfair.
3. **Thumb-first.** Everything is playable one-thumb-per-side on a phone. Matches are short (1-3 minutes) and replayable.
4. **Data-driven content.** New fighters, moves, items, and stages are added by authoring data and assets, not by rewriting code.

## 3. Target Audience & Platform
- **Audience:** casual-to-mid-core mobile players, 10+, who like party/brawler games (think "Stick Fight", "Party Animals", "Brawlhalla"-lite) and short sessions.
- **Platforms:** Android and iOS **(primary)**. Editor/desktop with keyboard/gamepad is a **dev convenience**, not a shipping target.
- **Orientation:** landscape only.
- **Performance targets (start):** 60 FPS on a mid-range 2021 phone, 30 FPS floor on low-end; <= 150 MB install size for base game; cold start < 5 s.

## 4. Theme & Tone
- **Tone:** goofy, bouncy, slapstick, colorful. Cartoon violence only: no blood, no gore. Targeting a family-friendly rating.
- **Characters (approved):** "wobble toys": fighters made of materials that look great when they flop and stretch, so the ragdoll is the art style rather than a gimmick. Starter cast concept:
  | Fighter | Concept | Gimmick |
  |---|---|---|
  | **Blobby** | Jelly-bean person | Bouncy; moves rebound off walls |
  | **Sir Noodle** | Pool-noodle knight | Very long reach, very weak guard |
  | **Puffin Pete** | Inflatable bouncy-castle penguin | Inflates for armor, can be popped |
  | **Madame Plush** | Stitched teddy-bear boxer | Heavy hitter, loses stuffing as health drops |
- **Setting:** an absurd toy-box world: arenas are places like a bathtub, a kitchen counter, a bouncy-castle fair, and a giant sandbox.
- This replaces the current robot art, UI fonts (`space age.ttf` etc.), and sci-fi sprites.
- **Full fighter sheets (look, stats, moves, ragdoll, rig notes) are in [`characters.md`](characters.md).**

## 5. Core Loop
`Menu -> pick fighter -> pick stage -> best-of-3 rounds (each ~60 s) -> items/hazards escalate -> KO or timeout -> results (coins, unlocks) -> repeat`

## 6. Game Modes
| Mode | Phase | Description |
|---|---|---|
| **Quick Fight** | MVP | 1 player vs AI, best-of-3, selectable fighter/stage/difficulty |
| **Arcade Ladder** | Alpha | 6-8 escalating AI fights with a final boss, one fighter per run |
| **Local Versus** | Beta | 2 players on one device (split touch zones) |
| **Daily Chaos** | Post-launch | Fixed seed with modifiers (low gravity, giant heads...) |
| **Online Versus** | Final phase (M6) | In scope, built last, after the single-device game is complete and stable (see [`architecture.md`](architecture.md) section 19) |

## 7. Combat Design

### 7.1 Rules
- Best-of-3 rounds; a round lasts 60 s **(start)**; a round ends on KO or timeout.
- Timeout: higher health wins. Equal health triggers **sudden death**: 10 s with both fighters at 1 HP **(start)**. A second tied timeout ends the prototype round as a draw and enables restart.
- Max health 200 **(start)**, matching the prototype.

### 7.2 Moves
Carried over from the prototype and generalized into data:
- **Punch chain** (up to 4 hits), **Kick chain** (up to 2 hits), a 0.5 s combo window **(start)**.
- **Jump** (single, optional double jump per fighter), **Block**, **Throw item**, **Special**.
- Damage tiers (start): body hit 10, leg hit 5, special 15, thrown item 10. Each character overrides via data.
- Sir Noodle's starting overrides: HP 170, guard 60, speed 3.2, jump 10; punches 9/5 body/leg, kicks 11/5 and 1.4x hitbox reach. Helicopter Joust uses four alternating front/back hit windows (two per side) at 7/5 damage each, startup/active/recovery 12/24/30 frames, full gauge cost, and 0.25 pushback. Detailed windows and frame data are in [his source notes](../Art/_Source/SirNoodle/README.md). These overrides follow the approved fighter sheet; Blobby's current sandbox speed remains 4.0 and is not retuned by M4-01.
- Puffin Pete's starting overrides: HP 220, guard 100, speed 2.6, jump 8, 0.9x reach; punches 10/5 and kicks 12/6 body/leg damage. Full-guard grounded block enters Inflate: additional 30% damage reduction after block (integer floor, minimum one for positive damage), and hit knockback resistance. Every accepted hit adds 25/100 pop pressure. At full pressure he loses armor and control for 75 simulation frames, zipping at 5 m/s with 12-frame turns inside ±6.2 m. Pressure decays one per frame while released. Guard-breaking hits bypass inflated armor; KO always launches. Belly Blimp costs full gauge, uses 18/24/30 startup/active/recovery frames and one 18/10 hit; active speed 2.2 m/s, initial active hop 2.5 m/s, hitbox 2.1 × 1.4 × 1 m at forward 0.6/height 0.75 m, stun 14, hit-stop 6 and pushback 4. Wind-up/recovery remain stationary relative to conveyors. [Detailed source notes](../Art/_Source/PuffinPete/README.md) record frame data and feedback. Values are **(start)** tuning; the current 50 Hz physics step makes 75 frames about 1.5 seconds, excluding hit-stop.
- Madame Plush's starting overrides: HP 240, guard 100, speed 2.4, jump 9, reach 0.9x; punches 12/6, kicks 14/7 body/leg. Stuffing seams open at or below 180/120/60 HP and persist until round reset. While alive at or below 60 HP, melee/special damage gains 10%, nearest-integer rounding before combo/block; items and hazards are unchanged. The HUD shows TOUGH!. Stitch Uppercut spends full gauge: 24/5/35 startup/active/recovery frames, one 24/12 hit, guard cost 25, stun 18, hit-stop 8, pushback 8 m/s and unblocked upward launch 7 m/s. Hitbox 1.35 × 1.5 × 1 m at forward 0.63/height 1.15 m. Surviving victims stay within X ±6.2 m for 90 simulation frames after launch; KO ragdolls can fly out. [Source notes](../Art/_Source/MadamePlush/README.md) detail frame data and bounded feedback. Values are **(start)** tuning. Coin unlock gating remains M4-08; all four fighters are available during content testing.

### 7.3 Block (required)
- Block is a **held state**, not a one-shot animation.
- Blocked hits do **reduced damage: 75% reduction (start)** and push the defender back.
- Blocked hits drain the **guard meter**. When guard is depleted, the breaking hit deals full damage and the fighter is stunned for 1 s **(start)**. Prototype guard starts at 100 and recovers at 20 per second when released.
- Block cannot be held during attacks, and does **not** stop thrown items from knocking the fighter over **[PROPOSED]** (items are a counter to turtling).
- Specials deal **chip damage** through block (50% of base damage in the prototype).

### 7.4 Special Gauge
- Gauge fills with hits landed and (smaller) hits taken **[PROPOSED]**.
- At full gauge, the Special button lights up. Each fighter has a unique special.

### 7.5 Ragdoll Feel
- **Hit reactions:** hits blend physics into animation (partial ragdoll) so limbs flop in the direction of the hit. Stronger hits mean more ragdoll influence and longer recovery.
- **KO:** full ragdoll launch with slow-motion and a camera punch-in.
- **Knockdown / get-up:** heavy hits and guard breaks put the fighter on the floor; getting up takes ~1 s with brief invulnerability to prevent infinite combos.
- **Squash and stretch:** impact frames, hit-stop (~60-100 ms **(start)**), comic text ("BONK!", "SPLAT!"), screen shake.
- **Rule:** ragdoll must never break gameplay readability or fairness: hitboxes are driven by gameplay logic, not by raw physics positions.

### 7.6 Anti-frustration Rules
- No infinite combos: combo scaling plus a hit-stun cap.
- Wake-up invulnerability after knockdown.
- Stage hazards are always telegraphed (>= 1.5 s warning **(start)**).

## 8. Items (Absurd Pickups)
Spawn from the sky/stage on a timer and are picked up by walking over them (replaces the single crate). One item held at a time; thrown via the Throw button or used on pickup depending on type.
| Item (examples) | Effect |
|---|---|
| Banana peel | Placed on the floor. The first opponent to step on it takes 18 frames of base stun and 4 m/s knockback. |
| Giant foam glove | Activates on pickup and adds 5 m/s knockback to the next three unblocked melee hits. |
| Rubber chicken | A throw that can bounce twice. Opponent contact deals a base 10 damage and consumes the chicken. |
| Rocket plunger | Launches the holder in their facing direction at 9 m/s horizontally and 3.5 m/s upward. |
| Whoopee cushion | Activates around the holder. Opponents within 2.4 m take 24 frames of base stun and 5 m/s knockback. |
| Anvil | Marks the floor for 90 physics steps (about 1.8 seconds), then drops and deals a base 18 damage to an opponent it hits. |
- Each item is a data asset (see [`architecture.md`](architecture.md)). Target at launch: **12 items**.
- Spawn logic is deterministic from a seed so Daily Chaos and tests are reproducible.

## 9. Stage Hazards
> Full stage sheets, layouts and hazard timings are in [`maps.md`](maps.md).
Each stage has 1-2 signature hazards that cycle during the round, always telegraphed.
| Stage | Hazard |
|---|---|
| **Bathtub Brawl** | Rising and draining water slows wet fighters and boosts their jump launch speed. A rubber duck wave knocks fighters back without damage. |
| **Kitchen Counter** | Toaster ejects pop-up toast; conveyor belt of a stand mixer |
| **Bouncy Castle Fair** | Trampoline floor sections; inflating/deflating walls |
| **Giant Sandbox** | Cat-paw (tail) sweep; sinking sand patches |
- Launch target: **4 stages**, each with 1 stage-specific music track.

## 10. Controls (Mobile)
- **Left thumb:** virtual joystick (reuse the existing Joystick Pack): left/right = move, up = jump, down = block.
- **Right thumb:** Punch, Kick, Special, Throw/Use-item buttons. Layout is **customizable** (size/position) in settings **[PROPOSED]**.
- Touch targets >= 9 mm physical size; buttons give pressed-state and haptic feedback.
- **Dev/Editor:** keyboard and gamepad mapped to the same input abstraction.

## 11. UI / Flow
`Splash -> Main Menu -> (Quick Fight | Arcade | Versus | Collection | Settings) -> Fighter Select -> Stage Select -> Match -> Results`
- In-match HUD: health bars, special gauge, guard meter, round timer, round pips, pause button.
- Win/lose screens and the Restart button from the prototype become the Results screen.
- Pause menu: resume, settings, quit-to-menu.
- Settings: master/music/SFX volume, haptics on/off, Standard or Mirrored touch control layout, graphics quality, language **(later)**. Settings apply immediately and are saved locally between sessions.
- Quick Fight arena selection includes independent **Hazards** and **Items** checkboxes. Both default on for a fresh setup; choices carry through all rounds and Rematch. Returning to a fresh setup restores both on. With both off, only the core fight runs. Arcade and boss encounters force both on and lock the setup controls. Opening an arena directly uses both on.
- Fighter selection offers Blobby, Sir Noodle, Puffin Pete and Madame Plush in a two-column menu, with an **AI: ...** button cycling the available opponents. Both fighter choices carry through Rematch. Development keyboard **T** previews a fighter's taunt where available. Inflation-capable fighters have a dedicated Pop bar on the match HUD.

## 12. Progression & Economy [PROPOSED]
- **Coins** earned per match; unlock new fighters, costumes ("skins" made from alternate materials/textures), and taunts.
- **Monetization (decided): ads + in-app purchases.**
  - **Ads:** rewarded ads only for player-initiated benefits (e.g., double coins after a match, a free daily item box); optional interstitials between matches with a frequency cap (never mid-match, never right after a KO celebration). Never block the fight flow.
  - **IAP:** coin packs, cosmetic skins/costumes/taunts, a "Remove Ads" purchase, and optional fighter unlock bundles. Cosmetic-first; no pay-to-win stat advantages.
  - Required: restore purchases, parental-friendly flow (audience is 10+), consent/age handling for ad personalization (GDPR/COPPA-style requirements), and clear pricing. SDKs are decided (LevelPlay + Unity Ads network, Unity IAP); Google Play policy checks (Billing, ads, data safety form, target-audience declaration) happen in M5.
- Local save data only for MVP (see [`architecture.md`](architecture.md)).

## 13. AI Opponents
- Replaces the prototype's "walk to player, attack every 2 s at random".
- Behavior-driven: spacing, blocking, punishing whiffs, item use, hazard awareness.
- 3 difficulty tiers via data (reaction time, aggression, block probability, mistake rate), not separate code.
- Initial Easy, Normal, and Hard reaction intervals are 16, 8, and 4 physics steps. Hard also uses items and avoids stage hazards more consistently. Sandbox scenes support **1**, **2**, and **3** for live comparison.
- Bosses (Arcade finale) get unique behavior sets.

## 14. Art, Animation & Audio

### 14.1 Characters
- **Style: flat toon shading with outlines** (decided; [`characters.md`](characters.md) section 3.4).
- **Pipeline:** modeled, textured, rigged, and animated in **Blender**, exported as FBX/glTF into Unity (see [`coding_guidelines.md`](coding_guidelines.md) section 9 for conventions).
- Low-poly, stylized, saturated colors; **target <= 8-12k tris per fighter** and <= 30 bones **(start)**, to keep many ragdolls cheap on mobile.
- A shared humanoid-ish **base rig** across fighters when possible, so animations can be reused or retargeted. Fighters with unique anatomy (Sir Noodle, Puffin Pete) get their own rigs.
- Each fighter ships with: idle, walk, run/dash (optional), jump/fall/land, block, 4 punch, 2 kick, special, hit-react (light/heavy), knockdown, get-up, win, taunt.
- Each rig includes ragdoll-ready setup: a collider/joint-friendly bone hierarchy, with proportions that read well when floppy.

### 14.2 Environments & VFX
> Detail lives in [`maps.md`](maps.md) (stages and hazards) and [`vfx_style.md`](vfx_style.md) (effect catalogue, game feel, budgets).
- Toy-box stages built from modular props; baked lighting where possible.
- VFX: comic-text pops, impact stars, dust puffs, confetti. Reuse the existing particle prefabs where they fit; re-skin the rest.

### 14.3 Audio
- Silly SFX (boing, squeak, honk, thwack), **short voice barks per fighter where they fit** (grunts, shouts, taunts, KO and win lines), a cartoonish announcer, and upbeat music per stage. See [`characters.md`](characters.md) section 6.
- Everything routed through an audio manager with volume buses (see [`architecture.md`](architecture.md)).

## 15. Roadmap
| Milestone | Scope |
|---|---|
| **M0 — Foundations** | Project hygiene (ProjectSettings/.gitignore), Fighter base class, input abstraction, real block, removal of revive, bug fixes from the prototype review. **Uses placeholder robots.** |
| **M1 — Combat Vertical Slice** | Data-driven moves, hit/hurt boxes, hitstop, guard meter, round/match flow, 1 new Blender fighter, partial ragdoll on hit, full ragdoll KO |
| **M2 — Chaos Layer** | Item system (6 items), 2 stages with hazards, comic VFX, AI with difficulty tiers |
| **M3 — Game Shell** | Menus, fighter/stage select, best-of-3, results, settings, save data, audio manager |
| **M4 — Content** | 4 fighters + boss, 4 stages + boss stage, 12 items, Arcade Ladder, unlocks, voice barks |
| **M5 — Monetization & Launch** | LevelPlay (+ Unity Ads network) and Unity IAP integration, Google Play data-safety and target-audience declarations, consent flow, perf pass on target devices, localization hooks, analytics, store assets, QA |
| **M6 — Online (final phase)** | Online Versus with Photon Quantum 3 (rollback) + Photon Cloud: private friend matches, public matchmaking, reconnect, emotes only |
| **Post-launch** | Local Versus polish, Daily Chaos, live-ops events |

Each milestone ends with a playable build and a short playtest.

### 15.1 Five-month schedule (solo developer) [PROPOSED]
Deadline: **5 months (about 20 weeks)**, solo. Dates are relative to the start. The plan front-loads the riskiest work (ragdoll feel, one real Blender fighter) and keeps a buffer.

| Weeks | Milestone | Focus | Exit check |
|---|---|---|---|
| 1-2 | **M0 Foundations** | Unity 6.3 project setup (ProjectSettings, `.gitignore`, Git LFS, URP conversion). Remove revive. Fix prototype bugs. Real block + guard meter. Sudden death. Touch input abstraction. | Prototype plays correctly on an Android device |
| 3-5 | **M1 Combat slice** | Fighter refactor (shared by player and AI), data-driven moves, hit/hurt boxes, hit-stop, round/match flow (best-of-3), partial ragdoll on hit, full ragdoll KO. **First Blender fighter (Blobby) end to end** to prove the pipeline. | Fun 1v1 vs. AI with the real character, 60 FPS on a mid-range phone |
| 6-8 | **M2 Chaos layer** | Item system (6 items), 2 stages with telegraphed hazards, comic-text VFX, AI difficulty tiers | Matches feel chaotic but fair in a playtest |
| 9-10 | **M3 Game shell** | Main menu, fighter/stage select, results, settings, pause, save data, audio manager | Complete menu-to-results loop |
| 11-14 | **M4 Content** | 3 more fighters (Sir Noodle, Puffin Pete, Madame Plush), boss (Mr. Jack), 2 more stages + boss stage, items up to 12, Arcade Ladder, unlocks, voice barks | Full offline game content-complete |
| 15-16 | **M5 Monetization & launch prep** | LevelPlay (+ Unity Ads) and Unity IAP, consent flow, Google Play data-safety/target-audience forms, perf pass, analytics, store assets, closed testing | Release candidate for Google Play |
| 17-19 | **M6 Online (stretch)** | Photon Quantum 3: port the combat core, private + public 1v1, reconnect, emotes | Online match works between two devices |
| 20 | **Buffer / polish** | Bug fixing, store submission | Submitted |

**Cut lines if the schedule slips** (in this order):
1. Online (M6) moves to a post-launch update. The game ships complete offline. Online is the riskiest item (large combat-core port, Quantum licensing unverified).
2. Fighters 3 and 4, and the 4th stage, move to post-launch content. The boss becomes a recolored, buffed existing fighter and the boss stage reuses an existing stage (see [`characters.md`](characters.md) 4.5 and [`maps.md`](maps.md) 3.5).
3. Arcade Ladder is reduced to a short 3-fight run.
4. Item count drops from 12 to 8.

**Solo-dev warning:** all art (4 characters, animation sets, 4 stages, VFX, UI) plus code is a heavy load. Use placeholders and simple stylized shapes first, then polish. Re-check the plan at the end of each milestone.

## 16. Decisions & Open Questions

### Decided by the owner
1. **Unity version:** Unity 6.3 LTS (6000.3.11f1).
2. **Render pipeline:** **URP** (confirmed by the owner).
3. **Theme and cast:** approved ("wobble toys": Blobby, Sir Noodle, Puffin Pete, Madame Plush).
4. **Third-party ragdoll tooling:** allowed (check license and mobile cost per package).
5. **Monetization:** ads + in-app purchases.
   - **Ads:** **Unity LevelPlay** for mediation, with **Unity Ads** as an ad network (the owner has used Unity Ads before).
   - **IAP:** **Unity IAP**.
6. **Target store:** **Google Play (Android) only for now**. Apple App Store is a far-future possibility, so nothing should hard-code Android-only assumptions beyond the platform layer.
7. **Online:** in scope, built **last** (M6). **Photon Quantum 3** with deterministic **rollback** netcode for 1v1; **Photon Cloud** for connectivity and matchmaking; **private matches with friends** and **public matches vs. random players**. No dedicated match servers in the initial release.
8. **Server budget:** **$0/month** initially on Photon's free tiers; review as concurrent players grow. **[VERIFY]** that the free tier covers a public launch (see risks in [`architecture.md`](architecture.md) section 19).
9. **Deliverable at 5 months:** a **fully deployable game on the Google Play Store** (not a demo). Online is the only stretch item; everything else in M0-M5 is required for release.
10. **Team and schedule:** **solo developer; the deadline is 5 months** (see section 15.1 for the schedule and cut lines).
11. **Age rating:** above the Google Play "child" audience: the game is **not** targeted at children, so it is not enrolled in the Families program. Expected rating is set via the Play Console content-rating (IARC) questionnaire for cartoon violence **[VERIFY]**. Consent and ad-personalization handling still apply.
12. **Chat:** **no free-text chat**, ever. Online communication is limited to fixed emotes/taunts if anything.
13. **Git LFS:** yes, for large binaries (`.fbx`, `.blend`, textures, audio).
14. **Art style:** **flat toon shading with outlines** for characters, props, stages and VFX (see [`characters.md`](characters.md) 3.4).
15. **Cast, gimmicks and stage hazards:** approved (see [`characters.md`](characters.md), [`maps.md`](maps.md)).
16. **Boss:** Arcade Ladder needs a **boss fighter** and a **boss stage** (concepts in [`characters.md`](characters.md) 4.5 and [`maps.md`](maps.md) 3.5).
17. **Voice:** fighters have **voice barks and SFX** where they fit the character, plus an announcer ([`characters.md`](characters.md) 6).
18. **Voice source:** the clips are generated with the open-source **VoxCPM2** text-to-speech model, then edited (trim, pitch, effects). License, output-use and hardware findings are recorded in [the D-08 review](../Tools/Audio/D08_REVIEW.md); owner quality acceptance of the five final audition exports is recorded. The owner requested shorter Blobby light-attack/heavy-hit and Mr. Jack win auditions; revised exports pass the 1.5-second pipeline limit and have owner listening approval. A second requested light-attack shortening brings its audition to 0.548 seconds.
19. **Mr. Jack:** **AI-only at launch**; making him playable is revisited after release.
20. **Classic mode:** included at launch as two separate match toggles, **Hazards on/off** and **Items on/off** ([`maps.md`](maps.md) section 5).
21. **Fight-plane width:** about **14 m** for now (from the prototype arena); confirm in Unity and adjust the camera bounds if fighters look too small. The prototype's jump height and ceiling must be measured and set as data (task M0-19).

## 17. Out of Scope (for now)
Story mode/cutscenes, dialogue or story voice acting (short voice barks and the announcer are in scope), console/PC release, user-generated content, 3+ player free-for-all.

## 18. Success Metrics (post-launch)
- D1 retention >= 35%, D7 >= 12% **(start targets)**.
- Average session 5-10 min; median match under 3 min.
- Crash-free sessions >= 99.5%.
- Playtest: a first-time player lands a combo and uses an item within their first match.
