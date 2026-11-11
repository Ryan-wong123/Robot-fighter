# art_prompts.md — Prompts for the Reference Art

Ready-to-use prompts for producing the **real concept art and reference images** listed in the "Reference images" sections of [`characters.md`](characters.md), [`maps.md`](maps.md) and [`vfx_style.md`](vfx_style.md) (task D-11). Use them with an image-generation tool of your choice, or hand them to an illustrator as a written brief.

> Claude cannot create raster artwork in this setup, so this file is the brief. Nothing here is final art. The SVG diagrams in `docs/images/` are only layout and proportion diagrams, not reference art.
> The fighter, stage and effect descriptions below come from the approved docs. Change the prompts if the docs change (keep-docs-current rule in [`../AGENTS.md`](../AGENTS.md)).

## 1. How to use this
1. Pick a tool. Before using it, check its terms: **commercial use, who owns the output, and any restrictions** **[VERIFY]**. Also check Google Play's policy on AI-generated content (same check as task D-08 for the voice model).
2. Generate in this order so the style stays consistent: **turnaround sheet first** for each fighter, then expressions and poses using the turnaround as the image reference (use the tool's reference-image or image-to-image feature). Fix one seed or style reference once you like the look and reuse it.
3. Keep every prompt's **style block** (section 2) unchanged and add the subject text after it.
4. Pick the best result, clean it up if needed (remove stray text, fix hands), and export as **PNG, at least 2048 px wide**. These sheets are references for modeling in Blender, so upscaled downloads are fine; record the real source size in `SOURCES.md`.
5. Save it using the file names in the Reference images tables, under `docs/images/<area>/concept/`, then link it from the matching sheet.
6. Record provenance in a `SOURCES.md` next to the images: tool and version, the exact prompt, settings, seed, date, and the license/terms note. This matters for the Play Store and for regenerating.
7. Do not put real people, living artists' names, or existing characters, brands or franchises in prompts. Keep the content family-friendly (cartoon violence only: no blood or gore).

## 2. Shared style block (put this at the start of every prompt)
```
flat toon shading with bold, clean dark outlines (outline color is a dark tint of each fill color), saturated cheerful colors, 2-3 tone cel shading with a simple rim light, simple big shapes, soft rounded cartoon proportions, playful slapstick tone, family-friendly mobile fighting game concept art, no text, no watermark
```
**Negative prompt** (if the tool supports it):
```
photorealistic, realistic skin, smooth gradients, soft airbrushed shading, painterly brush noise, 3d render, extra limbs, extra fingers, blurry, text, letters, logo, watermark, gore, blood, scary, dark horror mood
```

## 3. Fighters
Use one prompt per sheet. Replace `<STYLE>` with the style block. The fighter descriptions mirror [`characters.md`](characters.md) section 4.

### 3.1 Turnaround sheet (do this first, one per fighter)
Template:
```
<STYLE>. Character turnaround model sheet of <FIGHTER DESCRIPTION>. Three full-body views on one sheet: front view, side view, back view, same scale, neutral standing A-pose, arms slightly away from the body, plain light grey background, even lighting, consistent proportions across all views.
```

| Fighter | `<FIGHTER DESCRIPTION>` | File name |
|---|---|---|
| Blobby | a cheerful jelly-bean person with a glossy lime-green translucent-looking body (faked with a flat lighter inner color and a rim light), stubby arms and legs, oversized round hands and feet, a permanent big grin, about 1.7 m tall | `blobby_turnaround.png` |
| Sir Noodle | a tall, thin, pompous knight made of segmented pastel pink and light-blue pool noodles with very long bendy arms and legs, a dented tin-foil pot helmet, a small cardboard shield and a foam sword, about 2.0 m tall | `sir_noodle_turnaround.png` |
| Puffin Pete | a laid-back inflatable bouncy-castle penguin made of primary-blue shiny vinyl with a white belly, visible seam lines, an air valve on his back, a repair patch on one flipper, orange beak and feet, short and very round, about 1.55 m tall | `puffin_pete_turnaround.png` |
| Madame Plush | a worn but tough teddy-bear boxer made of brown fabric with visible stitches and mismatched patches, button eyes, big red boxing gloves and a red sash belt, large head with round ears, short legs, about 1.8 m tall | `madame_plush_turnaround.png` |
| Mr. Jack (boss) | a jack-in-the-box jester on a coiled yellow spring rising out of a purple box with a crank handle on its side, a wide painted grin, a purple and yellow jester hat with two points, a slightly creepy but cute look, about 2.3 m tall including the spring | `mr_jack_turnaround.png` |

### 3.2 Expression sheet
```
<STYLE>. Expression sheet of <FIGHTER DESCRIPTION>. Five head-and-shoulders close-ups in one row on a plain light grey background: neutral, hurt (wincing), angry, dizzy (spiral eyes, tongue out), happy (victory grin). Same character design as the reference image.
```
File: `<name>_expressions.png`.

### 3.3 Key poses
```
<STYLE>. Action pose sheet of <FIGHTER DESCRIPTION>, same design as the reference image. Six full-body poses on one sheet on a plain light grey background: relaxed idle, throwing a punch, doing a kick, blocking with arms raised, knocked down on the floor, victory pose. Exaggerated squash and stretch, clear readable silhouettes.
```
File: `<name>_poses.png`. Fighter-specific additions to append:
- Blobby: "his arm stretches long on the punch, his body squashes flat when knocked down"
- Sir Noodle: "his noodle limbs bend dramatically, a sweeping long-reach kick"
- Puffin Pete: "one pose shows him fully inflated and bigger, one pose shows him deflating after being popped"
- Madame Plush: "one pose shows cotton stuffing puffing out of a seam"
- Mr. Jack: "phase 1: only his top half pops out of the box; phase 2: fully sprung out on the spring"

### 3.4 Silhouette and materials (derive from the turnaround, no new prompt needed)
- **Silhouette test** (`<name>_silhouette.png`): fill the front view solid black at about 3 cm height on a phone-sized canvas. It must still read as that character.
- **Materials sheet** (`<name>_materials.png`): the turnaround plus color callouts (base, shade, outline, accent) with hex values. A simple paint-over is fine.
- **Moodboard** (`<name>_moodboard.png`): 4-8 reference photos/illustrations of the real material (jelly, pool noodle, vinyl inflatable, plush fabric, jack-in-the-box). Use only images you have the right to use, or your own photos. Record sources in `SOURCES.md`.

## 4. Stages (color key paintings)
One image per stage (`<stage>_colorkey.png`). They show mood, palette, materials and where the fight happens. Match the layouts and hazards in [`maps.md`](maps.md).

Template:
```
<STYLE>. Side-view 2D fighting game stage background, wide 16:9 landscape, <SETTING>. A clear flat empty floor strip across the lower third where two fighters will stand, no characters in the image, props kept to the background and the edges so the center stays readable, depth with a darker, lower-contrast background and a few foreground props at the edges.
```

| Stage | `<SETTING>` | File name |
|---|---|---|
| Bathtub Brawl | the inside of a giant bathtub seen from a toy's point of view: white porcelain floor, pastel bars of soap as platforms, a big rubber duck floating in the back, bubbles, a huge chrome faucet at the upper left, pale blue tiles on the wall, bright cyan, white and yellow palette | `bathtub_brawl_colorkey.png` |
| Kitchen Counter | a giant kitchen countertop seen from toy scale: warm wood counter, a chrome toaster at the left, a red stand mixer at the right, a cutting board in the middle, spilled cereal, a fruit bowl and cabinets in the background, warm yellow, red and wood palette | `kitchen_counter_colorkey.png` |
| Bouncy Castle Fair | an inflatable bouncy castle on a fairground at dusk: striped inflatable towers left and right, bunting flags, colorful balloons, a ferris wheel and a ticket booth in the background, party lights, vivid primary colors | `bouncy_castle_fair_colorkey.png` |
| Giant Sandbox | a backyard sandbox seen from toy scale: packed sand floor, a plastic bucket and shovel, a half-built sandcastle in the middle, a plastic fence and garden in the background, warm sand tones with bright toy plastic colors | `giant_sandbox_colorkey.png` |
| Toy Chest Showdown (boss) | the inside of a giant open wooden toy chest: wooden floor, scattered alphabet blocks, a teddy bear peeking over the edge, the open lid overhead, a music box in the corner, deep warm purple and wood palette with bright toy accents, slightly eerie but cute | `toy_chest_showdown_colorkey.png` |

Stage moodboards (`<stage>_moodboard.png`) are collected by you (real photos or illustrations of the setting at toy scale), with sources recorded.

## 5. VFX references
Match [`vfx_style.md`](vfx_style.md): flat fills, dark-tinted outlines, no soft glows. Use a plain dark or light-grey background so the shapes are easy to cut out.

| Sheet | Prompt (after `<STYLE>`) | File name |
|---|---|---|
| Hit effect frames | `game VFX sprite sheet of cartoon hit effects, each shown as 4 animation frames in a row: a small yellow star burst, a big orange 8-point impact burst with a white core, a blue shockwave ring expanding, a puff of dust. Flat colors, thick outlines, plain dark grey background.` | `hit_light_heavy_frames.png` |
| Comic text samples | `set of 8 cartoon comic-book onomatopoeia words, chunky bold lettering with thick dark outlines inside spiky yellow burst shapes: BONK, POW, SPLAT, BOING, HONK, FWAP, SQUEAK, KO. Varied sizes and slight rotations, plain light grey background.` | `comic_text_samples.png` |
| KO sequence | `4-frame cartoon knockout effect storyboard: radial speed lines, a big KO burst with stars, a slow-motion pose of a floppy character launched backwards, dizzy stars, vignette in the corners. Flat colors, thick outlines.` | `ko_sequence.png` |
| Hazard telegraphs | `5 game UI mock-ups of ground warning markers for stage hazards, flat style with thick outlines: a pulsing red-orange warning circle with a triangle icon, a yellow clock-ring marker, rain-of-blocks round shadows on a wooden floor, a swirling purple spiral on sand, a creeping cat-paw shadow. Each also shows a small icon so it works without color.` | `hazard_telegraph_examples.png` |
| Moodboard | collected by you: 6-12 references for cartoon-chaos effect styles (comic impacts, cartoon smoke, confetti, speed lines). Record sources. | `moodboard.png` |

## 6. Review checklist (before adding an image to the docs)
- [ ] Matches the doc description (shape, colors, materials, gimmick)
- [ ] Flat toon style with outlines: no realistic shading, no text or watermarks
- [ ] Same design across all views/sheets of a fighter (compare against the turnaround)
- [ ] Silhouette is readable at phone size (fighters); effects are readable without color alone
- [ ] Plain background, saved as PNG at least 2048 px wide (upscaled sources are fine for reference art; record the real size). Images are stored directly in the repo (no Git LFS for now), so keep each under about 5 MB
- [ ] `SOURCES.md` updated with tool, prompt, settings, seed, date and license note
- [ ] Linked from the matching doc, and `python3 Tools/tasks/check_docs.py` passes

## 7. Open Questions [OPEN]
1. Which image tool or illustrator will be used (and its commercial-use terms)?
2. Is AI-assisted concept art acceptable for the final in-game art, or only for reference? (The Blender models and textures are made by hand in Blender either way.)
