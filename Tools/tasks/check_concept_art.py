#!/usr/bin/env python3
"""Check the core concept-art set (task D-11).

For each expected image (the sheets generated from docs/art_prompts.md) it verifies that
  - the file exists and is a real PNG at least 2048 px wide,
  - it has a "## <file name>" entry in the SOURCES.md next to it,
  - it is linked from one of the docs (docs/*.md).
Optional extras (silhouettes, materials sheets, moodboards) are not required.
Exit code: 0 = the core set is complete, 1 = something is missing (listed on stdout).
"""
import glob, os, struct, sys

FIGHTERS = ['blobby', 'sir_noodle', 'puffin_pete', 'madame_plush', 'mr_jack']
EXPECTED = {
    'characters': ['%s_%s.png' % (n, k) for n in FIGHTERS for k in ('turnaround', 'expressions', 'poses')],
    'maps': ['%s_colorkey.png' % n for n in ('bathtub_brawl', 'kitchen_counter', 'bouncy_castle_fair', 'giant_sandbox', 'toy_chest_showdown')],
    'vfx': ['hit_light_heavy_frames.png', 'comic_text_samples.png', 'ko_sequence.png', 'hazard_telegraph_examples.png'],
}
MIN_WIDTH = 2048


def png_size(path):
    with open(path, 'rb') as f:
        head = f.read(24)
    if head[:8] != b'\x89PNG\r\n\x1a\n' or head[12:16] != b'IHDR':
        return None
    return struct.unpack('>II', head[16:24])


def problems(root='.', expected=EXPECTED):
    out = []
    docs = ''.join(open(p, encoding='utf-8').read() for p in glob.glob(os.path.join(root, 'docs', '*.md')))
    for area, files in expected.items():
        folder = os.path.join(root, 'docs', 'images', area, 'concept')
        sources_path = os.path.join(folder, 'SOURCES.md')
        sources = open(sources_path, encoding='utf-8').read() if os.path.exists(sources_path) else None
        for name in files:
            path = os.path.join(folder, name)
            if not os.path.exists(path):
                out.append('missing file: docs/images/%s/concept/%s' % (area, name))
                continue
            size = png_size(path)
            if size is None:
                out.append('not a valid PNG: docs/images/%s/concept/%s' % (area, name))
            elif size[0] < MIN_WIDTH:
                out.append('too narrow (%d px, need %d): docs/images/%s/concept/%s' % (size[0], MIN_WIDTH, area, name))
            if sources is None or ('## ' + name) not in sources:
                out.append('no SOURCES.md entry for %s' % name)
            if ('images/%s/concept/%s' % (area, name)) not in docs:
                out.append('not linked from any doc: %s' % name)
    return out


def main():
    found = problems()
    for p in found:
        print(p)
    total = sum(len(v) for v in EXPECTED.values())
    print('concept art: %d expected images, %d problem(s)' % (total, len(found)))
    return 1 if found else 0


if __name__ == '__main__':
    sys.exit(main())
