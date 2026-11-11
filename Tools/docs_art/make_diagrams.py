#!/usr/bin/env python3
"""Generate the schematic SVG diagrams used by docs/characters.md, docs/maps.md and docs/vfx_style.md.

These are to-scale SCHEMATICS (layouts, proportions, palettes, timing), not final art.
Re-run after changing layout numbers, palettes or timings in the docs:
    python3 Tools/docs_art/make_diagrams.py
"""
import math, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'docs', 'images')
FONT = "font-family='Verdana, Arial, sans-serif'"
INK = '#2b2b3a'


def write(rel, w, h, body, bg='#fdfaf2'):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 %d %d' width='%d' height='%d'>\n" % (w, h, w, h))
        f.write("<rect width='%d' height='%d' fill='%s'/>\n" % (w, h, bg))
        f.write(body)
        f.write('</svg>\n')


def t(x, y, s, size=14, fill=INK, anchor='start', weight='normal'):
    return "<text x='%s' y='%s' font-size='%s' fill='%s' text-anchor='%s' font-weight='%s' %s>%s</text>\n" % (x, y, size, fill, anchor, weight, FONT, s)


def star(cx, cy, r1, r2, n, fill, stroke=INK, sw=3, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = math.radians(rot + i * 180.0 / n)
        pts.append('%.1f,%.1f' % (cx + r * math.cos(a), cy + r * math.sin(a)))
    return "<polygon points='%s' fill='%s' stroke='%s' stroke-width='%s' stroke-linejoin='round'/>\n" % (' '.join(pts), fill, stroke, sw)


# ------------------------------------------------------------------ stages
PX = 80.0          # pixels per metre
CX = 600           # x of world 0
GY = 450           # ground y


def X(m): return CX + m * PX
def Y(m): return GY - m * PX


def figure(cx_m, color, flip=False):
    """1.8 m schematic fighter."""
    x = X(cx_m)
    return ("<g stroke='%s' stroke-width='3' stroke-linejoin='round'>"
            "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' rx='10' fill='%s'/>"
            "<circle cx='%.1f' cy='%.1f' r='%.1f' fill='%s'/></g>\n") % (
        INK, x - 14, Y(1.35), 28, 1.35 * PX, color, x, Y(1.55), 0.22 * PX, color)


def stage(name, subtitle, sky, floor, props, legend, spawns=(-4.5, -1.5, 1.5, 4.5), extra_bounds=None):
    b = ''
    b += "<rect x='20' y='60' width='1160' height='%d' rx='14' fill='%s' stroke='%s' stroke-width='4'/>\n" % (GY - 60 + 10, sky, INK)
    b += props('back')
    b += "<rect x='20' y='%d' width='1160' height='%d' fill='%s' stroke='%s' stroke-width='4'/>\n" % (GY, 70, floor, INK)
    b += props('front')
    for m in (-7, 7):
        b += "<line x1='%.1f' y1='70' x2='%.1f' y2='%d' stroke='#d33' stroke-width='3' stroke-dasharray='10 8'/>\n" % (X(m), X(m), GY + 70)
    b += t(X(-7) + 6, 88, 'bound -7 m', 12, '#d33') + t(X(7) - 6, 88, 'bound +7 m', 12, '#d33', 'end')
    if extra_bounds:
        b += extra_bounds
    b += figure(-3, '#9fd0ff') + figure(3, '#ffb3a7')
    b += t(X(-3), Y(1.9) + 0, 'P1', 12, INK, 'middle', 'bold') + t(X(3), Y(1.9) + 0, 'P2 / AI', 12, INK, 'middle', 'bold')
    for s in spawns:
        b += star(X(s), Y(1.0), 14, 6, 4, '#ffd23f', INK, 2)
    b += t(X(spawns[0]), GY + 38, 'item spawn points', 11, INK, 'middle')
    # scale bar
    b += "<line x1='40' y1='%d' x2='%d' y2='%d' stroke='%s' stroke-width='4'/>\n" % (GY + 56, 40 + PX, GY + 56, INK)
    b += t(40 + PX + 8, GY + 61, '1 m (fight plane 14 m wide, fighter 1.8 m)', 12)
    b += t(30, 36, name, 26, INK, 'start', 'bold') + t(30 + len(name) * 17, 36, subtitle, 14, '#666')
    # legend
    ly = GY + 100
    for i, (col, label) in enumerate(legend):
        lx = 40 + (i % 3) * 380
        yy = ly + (i // 3) * 26
        b += "<rect x='%d' y='%d' width='18' height='18' rx='4' fill='%s' stroke='%s' stroke-width='2'/>\n" % (lx, yy - 14, col, INK)
        b += t(lx + 26, yy, label, 13)
    h = ly + 26 * ((len(legend) + 2) // 3) + 20
    return b, h


def rect(m_x, m_y, m_w, m_h, fill, sw=3, rx=6, op=1):
    return "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' rx='%d' fill='%s' stroke='%s' stroke-width='%d' opacity='%s'/>\n" % (
        X(m_x), Y(m_y + m_h), m_w * PX, m_h * PX, rx, fill, INK, sw, op)


def ell(m_x, m_y, rx, ry, fill, sw=3, op=1):
    return "<ellipse cx='%.1f' cy='%.1f' rx='%.1f' ry='%.1f' fill='%s' stroke='%s' stroke-width='%d' opacity='%s'/>\n" % (X(m_x), Y(m_y), rx * PX, ry * PX, fill, INK, sw, op)


def arrow(x1, y1, x2, y2, col='#d33'):
    ang = math.atan2(y2 - y1, x2 - x1)
    hx, hy = x2 - 12 * math.cos(ang), y2 - 12 * math.sin(ang)
    return ("<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='%s' stroke-width='5'/>"
            "<polygon points='%.1f,%.1f %.1f,%.1f %.1f,%.1f' fill='%s'/>\n") % (
        x1, y1, hx, hy, col, x2, y2, hx + 8 * math.sin(ang), hy - 8 * math.cos(ang), hx - 8 * math.sin(ang), hy + 8 * math.cos(ang), col)


def hazard_zone(m_x, m_w, label, col='#ff5a36', top=2.6):
    return ("<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' fill='%s' opacity='0.22' stroke='%s' stroke-width='3' stroke-dasharray='8 6'/>\n" % (
        X(m_x), Y(top), m_w * PX, top * PX, col, col)) + t(X(m_x + m_w / 2.0), Y(top) + 18, label, 12, '#a31', 'middle', 'bold')


def bathtub(layer):
    s = ''
    if layer == 'back':
        for i in range(0, 14):
            for j in range(0, 3):
                s += "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' fill='%s' stroke='#bfe9f2' stroke-width='2'/>\n" % (X(-7) + i * PX, 80 + j * PX, PX, PX, '#e9fbff' if (i + j) % 2 == 0 else '#d3f3fb')
        s += "<rect x='%.1f' y='80' width='60' height='28' rx='8' fill='#b8c4cc' stroke='%s' stroke-width='3'/><rect x='%.1f' y='106' width='22' height='40' fill='#b8c4cc' stroke='%s' stroke-width='3'/>\n" % (X(-6.2), INK, X(-6.2) + 19, INK)
        s += t(X(-6.2) + 30, 74, 'faucet', 11, INK, 'middle')
        s += ell(0.5, 2.1, 0.7, 0.45, '#ffe14d') + "<polygon points='%.1f,%.1f %.1f,%.1f %.1f,%.1f' fill='#ff9d2e' stroke='%s' stroke-width='3'/>\n" % (X(1.2), Y(2.15), X(1.6), Y(2.05), X(1.2), Y(1.95), INK)
        s += t(X(0.5), Y(2.8), 'rubber duck', 11, INK, 'middle')
    else:
        s += rect(-5.9, 0, 1.4, 0.45, '#ff9ec7') + rect(4.7, 0, 1.4, 0.45, '#9ee7ff')
        s += t(X(-5.2), Y(0.6), 'soap (platform)', 11, INK, 'middle') + t(X(5.4), Y(0.6), 'soap', 11, INK, 'middle')
        s += "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' fill='#4fb3ff' opacity='0.28'/>\n" % (X(-7), Y(1.2), 14 * PX, 1.2 * PX)
        s += "<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='#1b7fd1' stroke-width='3' stroke-dasharray='12 6'/>\n" % (X(-7), Y(1.2), X(7), Y(1.2))
        s += t(X(6.9), Y(1.2) - 6, 'water high (move x0.8, jump x1.2)', 11, '#1b5f9e', 'end')
        s += arrow(X(7), Y(1.6), X(3.6), Y(1.6))
        s += t(X(5.3), Y(1.75), 'duck wave (push, no damage)', 11, '#a31', 'middle')
    return s


def kitchen(layer):
    s = ''
    if layer == 'back':
        for i in range(0, 14):
            s += "<rect x='%.1f' y='80' width='%.1f' height='%.1f' fill='%s' stroke='%s' stroke-width='2'/>\n" % (X(-7) + i * PX, PX, 3.6 * PX, '#fff1c9' if i % 2 == 0 else '#ffe7a8', '#e6c76b')
        s += ell(5.6, 3.1, 0.9, 0.4, '#ff6b4a') + ell(5.0, 3.35, 0.45, 0.35, '#ffd23f') + t(X(5.4), Y(3.8), 'fruit bowl', 11, INK, 'middle')
    else:
        s += rect(-6.4, 0, 1.4, 1.0, '#c9ced6') + "<rect x='%.1f' y='%.1f' width='14' height='28' fill='#2b2b3a'/><rect x='%.1f' y='%.1f' width='14' height='28' fill='#2b2b3a'/>\n" % (X(-6.1), Y(1.0) + 8, X(-5.65), Y(1.0) + 8)
        s += t(X(-5.7), Y(1.15), 'toaster', 11, INK, 'middle')
        s += rect(-1.0, 0, 2.0, 0.28, '#d9a066') + t(X(0), Y(0.28) - 8, 'cutting board', 11, INK, 'middle')
        s += rect(4.8, 0, 1.5, 1.6, '#e8434b') + ell(5.55, 1.75, 0.5, 0.2, '#b9bfc8') + t(X(5.55), Y(1.9) - 14, 'stand mixer', 11, INK, 'middle')
        s += "<rect x='%.1f' y='%d' width='%.1f' height='14' fill='#555' stroke='%s' stroke-width='3'/>\n" % (X(2.0), GY, 2.4 * PX, INK)
        s += t(X(3.2), GY + 12, '&lt;&lt;&lt; conveyor &gt;&gt;&gt;', 11, '#fff', 'middle')
        s += "<path d='M %.1f %.1f Q %.1f %.1f %.1f %.1f' fill='none' stroke='#d33' stroke-width='4' stroke-dasharray='8 6'/>\n" % (X(-5.7), Y(1.2), X(-3.5), Y(4.2), X(-1.6), Y(0.15))
        s += star(X(-1.6), Y(0.1), 10, 4, 4, '#ff5a36') + t(X(-2.4), Y(2.8), 'toast arc + landing marker', 11, '#a31', 'middle')
    return s


def castle(layer):
    s = ''
    if layer == 'back':
        s += "<circle cx='%.1f' cy='%.1f' r='60' fill='none' stroke='#9aa' stroke-width='5'/><line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='#9aa' stroke-width='4'/>\n" % (X(4.2), Y(3.4), X(4.2), Y(3.4), X(4.2), Y(0.5))
        for i, c in enumerate(['#ff5a8a', '#ffd23f', '#5ad1ff']):
            s += "<ellipse cx='%.1f' cy='%.1f' rx='20' ry='26' fill='%s' stroke='%s' stroke-width='3'/><line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='%s' stroke-width='2'/>\n" % (X(-2 + i * 1.8), Y(3.4 - (i % 2) * 0.6), c, INK, X(-2 + i * 1.8), Y(3.4 - (i % 2) * 0.6) + 26, X(-2 + i * 1.8), Y(2.4), INK)
    else:
        for side in (-1, 1):
            xm = side * 6.3 - (0.7 if side < 0 else 0.7)
            s += rect(xm, 0, 1.4, 2.8, '#ff7ab0' if side < 0 else '#6ec6ff')
            s += "<polygon points='%.1f,%.1f %.1f,%.1f %.1f,%.1f' fill='#ffd23f' stroke='%s' stroke-width='3'/>\n" % (X(xm), Y(2.8), X(xm + 1.4), Y(2.8), X(xm + 0.7), Y(3.6), INK)
        s += t(X(-6.3), Y(1.4), 'tower', 11, INK, 'middle') + t(X(6.3), Y(1.4), 'tower', 11, INK, 'middle')
        for px in (-1.9, 0.7):
            s += rect(px, 0, 1.2, 0.16, '#ff5a36')
            s += arrow(X(px + 0.6), Y(0.3), X(px + 0.6), Y(2.2), '#ff5a36')
            s += t(X(px + 0.6), Y(2.45), 'trampoline pad', 10, '#a31', 'middle')
    return s


def sandbox(layer):
    s = ''
    if layer == 'back':
        for i in range(0, 14):
            s += "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' fill='#d9b27a' stroke='%s' stroke-width='3'/>\n" % (X(-7) + i * PX + 6, Y(2.8), PX - 12, 2.4 * PX, INK)
        s += t(X(0), Y(3.1), 'plastic fence + garden', 11, INK, 'middle')
    else:
        s += rect(-0.9, 0, 1.8, 1.1, '#f3d28b') + t(X(0), Y(1.2), 'sandcastle (safe zone)', 11, INK, 'middle')
        s += rect(-5.8, 0, 0.9, 0.9, '#ff7a3d') + t(X(-5.35), Y(1.0), 'bucket', 11, INK, 'middle')
        s += "<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='#3b8fe0' stroke-width='8' stroke-linecap='round'/>\n" % (X(4.9), Y(0.8), X(5.6), Y(0.1))
        s += t(X(5.2), Y(1.0), 'shovel', 11, INK, 'middle')
        s += "<ellipse cx='%.1f' cy='%d' rx='55' ry='14' fill='#b78a4d' stroke='%s' stroke-width='3'/><path d='M %.1f %d q 15 -8 30 0 q -15 8 -30 0' fill='none' stroke='%s' stroke-width='3'/>\n" % (X(-1.9), GY + 6, INK, X(-1.9) - 15, GY + 6, INK)
        s += t(X(-1.9), GY + 40, 'sinking sand (x0.5)', 11, '#a31', 'middle')
        s += arrow(X(-6.8), Y(0.8), X(5.5), Y(0.8))
        s += t(X(-1.0), Y(0.8) - 12, 'cat paw sweep path (shadow telegraphs from the left, &gt;= 1.5 s; safe behind the sandcastle)', 11, '#a31', 'middle')
    return s


def toychest(layer):
    s = ''
    if layer == 'back':
        s += "<rect x='%.1f' y='%.1f' width='%.1f' height='%.1f' fill='#5d3b8a' stroke='%s' stroke-width='3'/>\n" % (X(-5.5), 120, 11 * PX, 30, INK)
        s += t(X(0), 141, 'open lid overhead', 12, '#fff', 'middle')
        s += rect(5.2, 0, 1.4, 0.9, '#caa06a') + t(X(5.9), Y(1.05), 'music box (tilt)', 11, INK, 'middle')
    else:
        s += rect(3.6, 0, 1.6, 1.6, '#7b3fa8') + "<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='#ffd23f' stroke-width='8'/>\n" % (X(4.4), Y(1.6), X(4.4), Y(2.5))
        s += t(X(4.4), Y(2.65), "Mr. Jack's box (boss spawn)", 11, INK, 'middle')
        for bx, ch, col in ((-4.6, 'A', '#ff6b6b'), (-0.2, 'B', '#4ecdc4'), (1.2, 'C', '#ffd23f')):
            s += "<ellipse cx='%.1f' cy='%d' rx='28' ry='9' fill='#2b2b3a' opacity='0.4'/>\n" % (X(bx), GY + 4)
            s += rect(bx - 0.4, 2.6, 0.8, 0.8, col) + t(X(bx), Y(3.0) + 6, ch, 18, INK, 'middle', 'bold')
            s += "<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='#d33' stroke-width='3' stroke-dasharray='6 6'/>\n" % (X(bx), Y(2.6), X(bx), GY)
        s += t(X(-2.4), Y(2.2), 'block rain: shadows telegraph &gt;= 1.5 s', 11, '#a31', 'middle')
        s += "<line x1='%.1f' y1='%d' x2='%.1f' y2='%d' stroke='#d33' stroke-width='4' stroke-dasharray='10 6'/>\n" % (X(-7), GY + 22, X(7), GY + 40)
        s += t(X(-2), GY + 56, 'floor tilts gently (music box tilt)', 11, '#a31', 'middle')
    return s


STAGES = [
    ('maps/bathtub_brawl.svg', 'Bathtub Brawl', 'Stage 1 (default)', '#c9f0ff', '#f5f5f5', bathtub,
     [('#ff9ec7', 'soap bars: platforms'), ('#4fb3ff', 'rising water level'), ('#ff3b30', 'duck wave direction (arrow)')]),
    ('maps/kitchen_counter.svg', 'Kitchen Counter', 'Stage 2', '#ffe9b0', '#d9a066', kitchen,
     [('#ff5a36', 'toast launch + landing'), ('#555555', 'mixer belt (reverses)'), ('#d9a066', 'cutting board')]),
    ('maps/bouncy_castle_fair.svg', 'Bouncy Castle Fair', 'Stage 3', '#bfe3ff', '#ffe36e', castle,
     [('#ff5a36', 'trampoline pads'), ('#ff7ab0', 'inflating towers (walls squeeze up to 25%)'), ('#ffd23f', 'item spawns')]),
    ('maps/giant_sandbox.svg', 'Giant Sandbox', 'Stage 4', '#dff3c4', '#e8c98c', sandbox,
     [('#ff5a36', 'cat paw sweep zone'), ('#b78a4d', 'sinking sand'), ('#f3d28b', 'sandcastle safe zone')]),
    ('maps/toy_chest_showdown.svg', 'Toy Chest Showdown', 'Boss stage', '#8c6bb8', '#c79a64', toychest,
     [('#ff5a36', 'block rain'), ('#c79a64', 'tilting floor'), ('#7b3fa8', 'boss spawn')]),
]


def make_stages():
    for rel, name, sub, sky, floor, props, legend in STAGES:
        extra = None
        if 'castle' in rel:
            extra = ("<line x1='%.1f' y1='70' x2='%.1f' y2='%d' stroke='#ff5a36' stroke-width='3' stroke-dasharray='4 6'/><line x1='%.1f' y1='70' x2='%.1f' y2='%d' stroke='#ff5a36' stroke-width='3' stroke-dasharray='4 6'/>\n" % (
                X(-5.25), X(-5.25), GY, X(5.25), X(5.25), GY)) + t(X(-5.2), 104, 'squeezed bound -5.25 m', 11, '#ff5a36')
        spawns = (-4.6, -1.4, 1.6) if 'toy_chest' in rel else (-4.5, -1.5, 1.5, 4.5)
        body, h = stage(name, sub, sky, floor, props, legend, spawns=spawns, extra_bounds=extra)
        write(rel, 1200, h, body)


# ------------------------------------------------------------------ characters
def outline(fill_dark): return fill_dark


PALETTE = {
    'Blobby': dict(base='#7ED957', shade='#4FB339', ink='#2E6B2A', acc='#C9FFB0'),
    'Sir Noodle': dict(base='#FF8FB1', shade='#8FD3FF', ink='#6B2A44', acc='#D9D9E8'),
    'Puffin Pete': dict(base='#3A8DFF', shade='#FFFFFF', ink='#1D4A94', acc='#FFB52E'),
    'Madame Plush': dict(base='#B9794A', shade='#E8C07A', ink='#5A3318', acc='#E8433F'),
    'Mr. Jack (boss)': dict(base='#7B3FA8', shade='#FFE9C9', ink='#3A1A55', acc='#FFD23F'),
}
K = 120.0  # px per metre for the lineup
LG = 400   # ground y


def face(cx, cy, ink, scale=1.0, mouth='smile'):
    s = "<circle cx='%.1f' cy='%.1f' r='%.1f' fill='#fff' stroke='%s' stroke-width='2'/><circle cx='%.1f' cy='%.1f' r='%.1f' fill='#fff' stroke='%s' stroke-width='2'/>" % (
        cx - 10 * scale, cy, 6 * scale, ink, cx + 10 * scale, cy, 6 * scale, ink)
    s += "<circle cx='%.1f' cy='%.1f' r='%.1f' fill='%s'/><circle cx='%.1f' cy='%.1f' r='%.1f' fill='%s'/>" % (cx - 9 * scale, cy, 2.5 * scale, ink, cx + 11 * scale, cy, 2.5 * scale, ink)
    s += "<path d='M %.1f %.1f q %.1f %.1f %.1f 0' fill='none' stroke='%s' stroke-width='3' stroke-linecap='round'/>\n" % (cx - 9 * scale, cy + 12 * scale, 9 * scale, 9 * scale, 18 * scale, ink)
    return s


def lineup():
    b = t(30, 40, 'Cast lineup (to scale) and palettes', 24, INK, 'start', 'bold')
    b += t(30, 62, 'Schematic silhouettes, not final art. Flat toon fills with dark-tinted outlines.', 13, '#666')
    xs = [150, 340, 540, 740, 960]
    heights = [1.7, 2.0, 1.55, 1.8, 2.3]
    names = list(PALETTE)
    roles = ['all-rounder', 'reach / poke', 'defense / counter', 'heavy hitter', 'boss, AI-only']
    for i, name in enumerate(names):
        p = PALETTE[name]; x = xs[i]; H = heights[i] * K; ink = p['ink']
        g = "<g stroke='%s' stroke-width='4' stroke-linejoin='round' stroke-linecap='round'>" % ink
        if name == 'Blobby':
            g += "<ellipse cx='%d' cy='%.1f' rx='24' ry='14' fill='%s'/><ellipse cx='%d' cy='%.1f' rx='24' ry='14' fill='%s'/>" % (x - 24, LG - 10, p['shade'], x + 24, LG - 10, p['shade'])
            g += "<ellipse cx='%d' cy='%.1f' rx='56' ry='%.1f' fill='%s'/>" % (x, LG - H * 0.52, H * 0.46, p['base'])
            g += "<ellipse cx='%d' cy='%.1f' rx='22' ry='%.1f' fill='%s' stroke='none' opacity='0.6'/>" % (x - 24, LG - H * 0.66, H * 0.18, p['acc'])
            g += "<ellipse cx='%d' cy='%.1f' rx='16' ry='20' fill='%s'/><ellipse cx='%d' cy='%.1f' rx='16' ry='20' fill='%s'/>" % (x - 66, LG - H * 0.4, p['shade'], x + 66, LG - H * 0.4, p['shade'])
            g += face(x, LG - H * 0.66, ink, 1.1)
        elif name == 'Sir Noodle':
            for j in range(4):
                c = p['base'] if j % 2 == 0 else p['shade']
                g += "<line x1='%d' y1='%.1f' x2='%d' y2='%.1f' stroke='%s' stroke-width='18'/>" % (x - 16, LG - j * H * 0.11, x - 16, LG - (j + 1) * H * 0.11, c)
                g += "<line x1='%d' y1='%.1f' x2='%d' y2='%.1f' stroke='%s' stroke-width='18'/>" % (x + 16, LG - j * H * 0.11, x + 16, LG - (j + 1) * H * 0.11, c)
            g += "<rect x='%d' y='%.1f' width='52' height='%.1f' rx='16' fill='%s'/>" % (x - 26, LG - H * 0.72, H * 0.3, p['base'])
            g += "<path d='M %d %.1f q -50 40 -40 120' fill='none' stroke='%s' stroke-width='14'/><path d='M %d %.1f q 50 40 40 120' fill='none' stroke='%s' stroke-width='14'/>" % (x - 26, LG - H * 0.68, p['shade'], x + 26, LG - H * 0.68, p['base'])
            g += "<circle cx='%d' cy='%.1f' r='24' fill='#ffe3c4'/><path d='M %d %.1f a 28 28 0 0 1 56 0 z' fill='%s'/><rect x='%d' y='%.1f' width='6' height='20' fill='%s'/>" % (x, LG - H * 0.85, x - 28, LG - H * 0.89, p['acc'], x - 3, LG - H * 0.89 - 28, p['acc'])
            g += face(x, LG - H * 0.84, ink, 0.8)
        elif name == 'Puffin Pete':
            g += "<ellipse cx='%d' cy='%d' rx='18' ry='8' fill='%s'/><ellipse cx='%d' cy='%d' rx='18' ry='8' fill='%s'/>" % (x - 24, LG - 6, p['acc'], x + 24, LG - 6, p['acc'])
            g += "<ellipse cx='%d' cy='%.1f' rx='70' ry='%.1f' fill='%s'/><ellipse cx='%d' cy='%.1f' rx='42' ry='%.1f' fill='%s' stroke-width='3'/>" % (x, LG - H * 0.47, H * 0.45, p['base'], x, LG - H * 0.42, H * 0.34, p['shade'])
            g += "<ellipse cx='%d' cy='%.1f' rx='16' ry='36' fill='%s' transform='rotate(25 %d %.1f)'/><ellipse cx='%d' cy='%.1f' rx='16' ry='36' fill='%s' transform='rotate(-25 %d %.1f)'/>" % (x - 74, LG - H * 0.5, p['base'], x - 74, LG - H * 0.5, x + 74, LG - H * 0.5, p['base'], x + 74, LG - H * 0.5)
            g += "<polygon points='%d,%.1f %d,%.1f %d,%.1f' fill='%s'/><circle cx='%d' cy='%.1f' r='6' fill='%s'/>" % (x - 12, LG - H * 0.7, x + 12, LG - H * 0.7, x, LG - H * 0.64, p['acc'], x, LG - H * 0.93, '#ff5a36')
            g += face(x, LG - H * 0.76, ink, 1.0)
        elif name == 'Madame Plush':
            g += "<ellipse cx='%d' cy='%.1f' rx='22' ry='14' fill='%s'/><ellipse cx='%d' cy='%.1f' rx='22' ry='14' fill='%s'/>" % (x - 26, LG - 10, p['base'], x + 26, LG - 10, p['base'])
            g += "<ellipse cx='%d' cy='%.1f' rx='54' ry='%.1f' fill='%s'/><polygon points='%d,%.1f %d,%.1f %d,%.1f %d,%.1f' fill='%s'/>" % (x, LG - H * 0.38, H * 0.3, p['base'], x - 50, LG - H * 0.28, x + 50, LG - H * 0.42, x + 50, LG - H * 0.34, x - 50, LG - H * 0.2, p['acc'])
            g += "<circle cx='%d' cy='%.1f' r='%.1f' fill='%s'/><circle cx='%d' cy='%.1f' r='18' fill='%s'/><circle cx='%d' cy='%.1f' r='18' fill='%s'/>" % (x, LG - H * 0.8, 40, p['base'], x - 32, LG - H * 0.95, p['base'], x + 32, LG - H * 0.95, p['base'])
            g += "<ellipse cx='%d' cy='%.1f' rx='16' ry='12' fill='%s'/>" % (x, LG - H * 0.76, p['shade'])
            g += "<circle cx='%d' cy='%.1f' r='24' fill='%s'/><circle cx='%d' cy='%.1f' r='24' fill='%s'/>" % (x - 78, LG - H * 0.42, p['acc'], x + 78, LG - H * 0.42, p['acc'])
            g += "<circle cx='%d' cy='%.1f' r='5' fill='#222'/><circle cx='%d' cy='%.1f' r='5' fill='#222'/>" % (x - 14, LG - H * 0.84, x + 14, LG - H * 0.84)
        else:
            g += "<rect x='%d' y='%d' width='110' height='84' rx='6' fill='%s'/><line x1='%d' y1='%d' x2='%d' y2='%d' stroke='%s' stroke-width='6'/><circle cx='%d' cy='%d' r='9' fill='%s'/>" % (x - 55, LG - 84, p['base'], x + 55, LG - 50, x + 88, LG - 50, p['acc'], x + 92, LG - 50, p['acc'])
            top = LG - H * 0.84 + 34
            pts = ' '.join('%d,%.1f' % (x + (14 if k % 2 == 0 else -14), LG - 84 + (top - (LG - 84)) * k / 8.0) for k in range(0, 9))
            g += "<polyline points='%s' fill='none' stroke='%s' stroke-width='8'/>" % (pts, p['acc'])
            g += "<circle cx='%d' cy='%.1f' r='36' fill='%s'/><polygon points='%d,%.1f %d,%.1f %d,%.1f' fill='%s'/><polygon points='%d,%.1f %d,%.1f %d,%.1f' fill='%s'/>" % (x, LG - H * 0.84, p['shade'], x - 34, LG - H * 0.9, x - 60, LG - H * 1.0, x - 8, LG - H * 0.98, p['base'], x + 34, LG - H * 0.9, x + 60, LG - H * 1.0, x + 8, LG - H * 0.98, p['acc'])
            g += face(x, LG - H * 0.84, ink, 0.9)
        g += '</g>\n'
        b += g
        b += t(x, LG + 30, name, 15, INK, 'middle', 'bold') + t(x, LG + 48, '%s, %.2f m' % (roles[i], heights[i]), 11, '#666', 'middle')
        for k, key in enumerate(('base', 'shade', 'ink', 'acc')):
            b += "<rect x='%d' y='%d' width='34' height='22' rx='4' fill='%s' stroke='%s' stroke-width='2'/>" % (x - 72 + k * 37, LG + 62, p[key], INK)
            b += t(x - 55 + k * 37, LG + 100, p[key], 8, '#666', 'middle')
    b += "<line x1='40' y1='%d' x2='1130' y2='%d' stroke='%s' stroke-width='3'/>\n" % (LG, LG, INK)
    b += "<line x1='40' y1='%d' x2='40' y2='%.1f' stroke='%s' stroke-width='3'/><text x='46' y='%.1f' font-size='11' %s>2 m</text>\n" % (LG, LG - 2 * K, INK, LG - 2 * K + 4, FONT)
    b += t(30, LG + 125, 'Palette chips: base, shade, outline, accent (proposed values; change them in the art, then re-run Tools/docs_art/make_diagrams.py).', 11, '#666')
    write('characters/cast_lineup.svg', 1160, LG + 140, b)


def ragdoll():
    b = t(30, 36, 'Ragdoll bodies (12 rigid bodies per fighter)', 22, INK, 'start', 'bold')
    b += t(30, 58, 'Hands and feet follow their parent bone. Hitboxes follow the animated pose, never these bodies.', 12, '#666')
    cx = 380
    parts = {'Head': (cx, 130, 30, 34), 'Chest': (cx, 215, 44, 40), 'Spine': (cx, 280, 38, 28), 'Pelvis': (cx, 335, 40, 24),
             'UpperArm.L': (cx - 78, 215, 14, 38), 'LowerArm.L': (cx - 84, 285, 12, 36), 'UpperArm.R': (cx + 78, 215, 14, 38), 'LowerArm.R': (cx + 84, 285, 12, 36),
             'UpperLeg.L': (cx - 26, 408, 16, 44), 'LowerLeg.L': (cx - 26, 490, 14, 42), 'UpperLeg.R': (cx + 26, 408, 16, 44), 'LowerLeg.R': (cx + 26, 490, 14, 42)}
    rows = {'Head': ('R', 110), 'Chest': ('R', 165), 'UpperArm.R': ('R', 220), 'Spine': ('R', 270), 'LowerArm.R': ('R', 320), 'Pelvis': ('R', 365),
            'UpperLeg.R': ('R', 420), 'LowerLeg.R': ('R', 490),
            'UpperArm.L': ('L', 190), 'LowerArm.L': ('L', 290), 'UpperLeg.L': ('L', 420), 'LowerLeg.L': ('L', 490)}
    cols = ['#9fd0ff', '#ffd23f', '#9ee79a', '#ff9ec7', '#c7a6ff', '#ffb27a']
    lines = labels = bodies = ''
    for i, (n, (x, y, rx, ry)) in enumerate(parts.items()):
        side, ly = rows[n]
        lx = 620 if side == 'R' else 140
        lines += "<line x1='%d' y1='%d' x2='%d' y2='%d' stroke='#999' stroke-width='1.5'/>\n" % (x, y, lx, ly)
        labels += t(lx + (8 if side == 'R' else -8), ly + 4, n, 13, INK, 'start' if side == 'R' else 'end')
        bodies += "<ellipse cx='%d' cy='%d' rx='%d' ry='%d' fill='%s' stroke='%s' stroke-width='3'/>\n" % (x, y, rx, ry, cols[i % 6], INK)
    write('characters/ragdoll_bodies.svg', 740, 560, b + lines + bodies + labels)


# ------------------------------------------------------------------ vfx
def vfx_sheets():
    b = t(30, 38, 'Telegraph colour code and hit palette', 22, INK, 'start', 'bold')
    b += t(30, 60, 'Never rely on colour alone: every telegraph also has a shape/icon, motion and a sound cue.', 12, '#666')
    items = [('#ff3b30', 'DANGER', 'red/orange pulse + warning triangle', 'tri'), ('#ffd23f', 'SOON', 'yellow pulse + clock ring', 'ring'),
             ('#2ed8a3', 'SAFE / PICKUP', 'green-cyan glow + sparkle', 'star'), ('#9b5de5', 'SPECIAL / CHAOS', 'purple swirl + spiral', 'spiral')]
    for i, (col, name, desc, shape) in enumerate(items):
        x = 40 + i * 280
        b += "<rect x='%d' y='90' width='250' height='150' rx='14' fill='%s' stroke='%s' stroke-width='4'/>\n" % (x, col, INK)
        cx, cy = x + 125, 150
        if shape == 'tri':
            b += "<polygon points='%d,%d %d,%d %d,%d' fill='#fff' stroke='%s' stroke-width='4'/>%s" % (cx, cy - 34, cx + 36, cy + 28, cx - 36, cy + 28, INK, t(cx, cy + 20, '!', 36, INK, 'middle', 'bold'))
        elif shape == 'ring':
            b += "<circle cx='%d' cy='%d' r='32' fill='#fff' stroke='%s' stroke-width='4'/><line x1='%d' y1='%d' x2='%d' y2='%d' stroke='%s' stroke-width='5'/><line x1='%d' y1='%d' x2='%d' y2='%d' stroke='%s' stroke-width='5'/>\n" % (cx, cy, INK, cx, cy, cx, cy - 20, INK, cx, cy, cx + 14, cy, INK)
        elif shape == 'star':
            b += star(cx, cy, 36, 14, 4, '#fff')
        else:
            pts = ' '.join('%.1f,%.1f' % (cx + (3 + k * 0.75) * math.cos(k * 0.55), cy + (3 + k * 0.75) * math.sin(k * 0.55)) for k in range(0, 44))
            b += "<polyline points='%s' fill='none' stroke='#fff' stroke-width='5'/>\n" % pts
        b += t(x + 125, 208, name, 16, INK, 'middle', 'bold') + t(x + 125, 226, desc, 10, INK, 'middle')
    b += t(30, 290, 'Hit effects: warm (impact)  /  buff effects: cool', 14, INK, 'start', 'bold')
    for i, c in enumerate(['#fff6a8', '#ffd23f', '#ff9d2e', '#ff5a36', '#ffffff', '#9ee7ff', '#4fb3ff']):
        b += "<rect x='%d' y='300' width='60' height='34' rx='6' fill='%s' stroke='%s' stroke-width='3'/>%s" % (40 + i * 70, c, INK, t(70 + i * 70, 352, c, 9, '#666', 'middle'))
    write('vfx/telegraph_colors.svg', 1140, 372, b)

    b = t(30, 38, 'Hit-effect shape sheet', 22, INK, 'start', 'bold')
    b += t(30, 60, 'Flat fills, dark-tinted outlines, no soft glows. Each shape is a pooled sprite or small particle system.', 12, '#666')
    cells = [('Star burst (light)', 'star4'), ('Big burst (heavy)', 'star8'), ('Shockwave ring', 'ring'), ('Speed lines (KO)', 'lines'),
             ('Dust poof', 'poof'), ('Confetti (win)', 'conf'), ('Dizzy stars (guard break)', 'dizzy'), ('Comic text pop', 'text')]
    for i, (name, kind) in enumerate(cells):
        cx = 150 + (i % 4) * 290
        cy = 190 + (i // 4) * 250
        b += "<rect x='%d' y='%d' width='260' height='210' rx='14' fill='#fff' stroke='%s' stroke-width='3'/>\n" % (cx - 130, cy - 90, INK)
        if kind == 'star4': b += star(cx, cy, 56, 20, 4, '#ffd23f', INK, 4)
        elif kind == 'star8': b += star(cx, cy, 70, 38, 8, '#ff9d2e', INK, 4) + star(cx, cy, 36, 20, 8, '#fff6a8', INK, 3)
        elif kind == 'ring':
            for r, w in ((30, 8), (50, 6), (68, 4)):
                b += "<circle cx='%d' cy='%d' r='%d' fill='none' stroke='%s' stroke-width='%d' opacity='%.2f'/>\n" % (cx, cy, r, '#4fb3ff', w, 1 - r / 90.0)
        elif kind == 'lines':
            for a in range(0, 360, 20):
                r1, r2 = 36, 76
                b += "<line x1='%.1f' y1='%.1f' x2='%.1f' y2='%.1f' stroke='%s' stroke-width='4' stroke-linecap='round'/>\n" % (cx + r1 * math.cos(math.radians(a)), cy + r1 * math.sin(math.radians(a)), cx + r2 * math.cos(math.radians(a)), cy + r2 * math.sin(math.radians(a)), INK)
        elif kind == 'poof':
            for dx, dy, r in ((-30, 10, 24), (0, -6, 30), (32, 10, 22), (4, 26, 20)):
                b += "<circle cx='%d' cy='%d' r='%d' fill='#e9e4d8' stroke='%s' stroke-width='3'/>\n" % (cx + dx, cy + dy, r, INK)
        elif kind == 'conf':
            cols = ['#ff5a8a', '#ffd23f', '#4ecdc4', '#9b5de5']
            for k in range(26):
                a = k * 2.4
                x2, y2 = cx + (k % 9 - 4) * 15, cy + ((k * 7) % 11 - 5) * 11
                b += "<rect x='%d' y='%d' width='12' height='7' fill='%s' stroke='%s' stroke-width='1.5' transform='rotate(%d %d %d)'/>\n" % (x2, y2, cols[k % 4], INK, a * 20, x2 + 6, y2 + 3)
        elif kind == 'dizzy':
            b += "<ellipse cx='%d' cy='%d' rx='60' ry='16' fill='none' stroke='#999' stroke-width='2' stroke-dasharray='4 4'/>\n" % (cx, cy)
            for k in range(4):
                a = math.radians(k * 90 + 20)
                b += star(cx + 60 * math.cos(a), cy + 16 * math.sin(a), 14, 6, 5, '#ffd23f', INK, 2)
        else:
            b += "<g transform='rotate(-8 %d %d)'>" % (cx, cy) + star(cx, cy - 8, 66, 42, 10, '#ffd23f', INK, 4) + t(cx, cy + 2, 'BONK!', 28, INK, 'middle', 'bold') + "</g>\n"
            b += t(cx, cy + 82, 'scale 0.6 -> 1.3 -> 1.0', 10, '#666', 'middle')
        b += t(cx, cy + 96, name, 13, INK, 'middle', 'bold')
    write('vfx/hit_effect_sheet.svg', 1180, 720, b)

    b = t(30, 38, 'Hit timing (heavy hit, in milliseconds)', 22, INK, 'start', 'bold')
    b += t(30, 60, 'Start values; tuned in data (VfxDef). Hit-stop freezes fighters only, UI and VFX keep animating.', 12, '#666')
    X0, W = 270, 930
    def tx(ms): return X0 + (ms + 200) / 900.0 * W
    rows = [('Anticipation (wind-up)', -150, 0, '#9ee7ff'), ('Impact flash (white, 2 frames)', 0, 33, '#ffffff'), ('Hit-stop (fighters frozen)', 0, 100, '#ff9d2e'),
            ('Star burst + comic text', 0, 500, '#ffd23f'), ('Screen shake (decays)', 0, 350, '#ff7a6b'), ('Squash and stretch (spring)', 0, 300, '#9ee79a'),
            ('Haptic tap', 0, 40, '#c7a6ff'), ('Fade out / return to pool', 400, 650, '#d9d9d9')]
    bottom = 100 + len(rows) * 46
    for ms in range(-200, 701, 100):
        b += "<line x1='%.1f' y1='86' x2='%.1f' y2='%d' stroke='%s' stroke-width='%d'/>%s" % (tx(ms), tx(ms), bottom, '#d33' if ms == 0 else '#ccc', 3 if ms == 0 else 1, t(tx(ms), 80, ('HIT' if ms == 0 else '%d' % ms), 11, '#d33' if ms == 0 else '#666', 'middle'))
    for i, (name, s0, s1, col) in enumerate(rows):
        y = 100 + i * 46
        b += t(30, y + 22, name, 13, INK)
        b += "<rect x='%.1f' y='%d' width='%.1f' height='28' rx='6' fill='%s' stroke='%s' stroke-width='3'/>\n" % (tx(s0), y, max(tx(s1) - tx(s0), 6), col, INK)
    write('vfx/hit_timing.svg', 1240, bottom + 30, b)


def generate():
    make_stages()
    lineup()
    ragdoll()
    vfx_sheets()


if __name__ == '__main__':
    import sys
    if '--check' in sys.argv:
        # Regenerate into a temp dir and compare with the committed SVGs (exit 1 if stale or missing).
        import filecmp, tempfile
        real = OUT
        OUT = tempfile.mkdtemp()
        generate()
        stale = []
        for root, _, files in os.walk(OUT):
            for f in files:
                new = os.path.join(root, f)
                old = os.path.join(real, os.path.relpath(new, OUT))
                if not os.path.exists(old) or not filecmp.cmp(new, old, shallow=False):
                    stale.append(os.path.relpath(new, OUT))
        for f in stale:
            print('stale or missing diagram: docs/images/' + f + '  (run: python3 Tools/docs_art/make_diagrams.py)')
        print('diagrams in sync' if not stale else '%d diagram(s) out of date' % len(stale))
        sys.exit(1 if stale else 0)
    generate()
    print('SVGs written to', os.path.normpath(OUT))
