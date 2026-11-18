"""Generate original synthesized placeholder loops and cues without external samples."""
from pathlib import Path
import math
import random
import struct
import wave

OUTPUT = Path("Assets/_Project/Art/Audio/Resources/Audio")
RATE = 22050
OUTPUT.mkdir(parents=True, exist_ok=True)


def write(name, values):
    peak = max(1, max(abs(v) for v in values))
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as audio:
        audio.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        audio.writeframes(struct.pack("<%dh" % len(values),
                         *(round(max(-1, min(1, v / peak)) * 26000) for v in values)))


def music(name, root, speed, bright):
    beat = 60 / speed
    length = beat * 16
    notes = [0, 7, 12, 7, 4, 9, 12, 9, 5, 12, 17, 12, 7, 14, 19, 14]
    values = []
    for sample in range(round(RATE * length)):
        t = sample / RATE
        step = int(t / beat)
        local = t % beat
        frequency = root * 2 ** (notes[step % 16] / 12)
        envelope = math.exp(-local * 10) * min(1, local * 450)
        melody = (math.sin(math.tau * frequency * local)
                  + .25 * math.sin(math.tau * frequency * 2 * local)) * envelope * .19
        bass_time = t % (beat * 2)
        bass = math.sin(math.tau * root * .5 * bass_time) * math.exp(-bass_time * 5) * .13
        kick_time = t % beat
        kick = math.sin(math.tau * (75 * kick_time - 80 * kick_time ** 2)) * math.exp(-kick_time * 34) * .10
        tick_time = t % (beat / 2)
        tick = math.sin(math.tau * (2100 if bright else 1350) * tick_time) * math.exp(-tick_time * 110) * .025
        edge = min(1, t * 200, (length - t) * 200)
        values.append((melody + bass + kick + tick) * max(0, edge))
    write(name, values)


def cue(name, duration, frequency, sweep, noise):
    generator = random.Random(18)
    values = []
    for sample in range(round(RATE * duration)):
        t = sample / RATE
        envelope = math.exp(-t * 7 / duration) * min(1, t * 1000)
        pitch = math.sin(math.tau * (frequency * t + sweep * t * t))
        values.append((pitch * (1-noise) + generator.uniform(-1, 1) * noise) * envelope * .65)
    write(name, values)


music("MenuMusic", 220, 112, False)
music("BathtubMusic", 261.63, 120, False)
music("KitchenMusic", 196, 128, True)
music("SandboxMusic", 246.94, 116, True)
for parameters in (
    ("Click", .09, 750, 1700, .02), ("Hit", .18, 135, -200, .42),
    ("Heavy", .28, 88, -110, .52), ("Block", .22, 930, -700, .12),
    ("GuardBreak", .45, 520, -500, .54), ("Special", .48, 210, 950, .3),
    ("Knockout", .65, 180, -95, .26), ("Round", .3, 530, 420, .02),
    ("Win", .65, 440, 430, .02)):
    cue(*parameters)
