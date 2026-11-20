"""Render original layered menu/stage loops and combat cues without samples."""
import argparse
import math
import random
import struct
import wave
from pathlib import Path

RATE = 24000

def tone(hz, age, decay, overtones):
    return min(1, age * 480) * math.exp(-age * decay) * sum(
        gain * math.sin(math.tau * hz * multiple * age) for multiple, gain in overtones)

def write(output, name, samples, peak):
    gain = peak / max(abs(sample) for sample in samples)
    pcm = [round(sample * gain * 32767) for sample in samples]
    with wave.open(str(output / (name + ".wav")), "wb") as target:
        target.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        target.writeframes(struct.pack("<%dh" % len(pcm), *pcm))

def track(output, name, bpm, root, melody, color):
    beat = 60 / bpm
    count = round(RATE * beat * 16)
    rng = random.Random(401)
    notes = (0, 5, 9, 7)
    layers = []
    for n in range(count):
        t = n / RATE
        bar = int(t / beat / 4) % 4
        semiquaver = int(t / beat * 2)
        age = t % (beat / 2)
        hz = root * 2 ** ((notes[bar] + melody[semiquaver % 8]) / 12)
        lead = tone(hz, age, 11, color) * .15
        low = tone(root * .25 * 2 ** (notes[bar] / 12), t % beat, 6, ((1, 1), (2, .16))) * .16
        kick = tone(61, t % beat, 23, ((1, 1), (2, .2))) * .12
        hat = rng.uniform(-1, 1) * math.exp(-age * 68) * min(1, age * 600) * .035
        chord_age = t % (beat * 2)
        chord = sum(tone(root * .5 * 2 ** ((notes[bar] + interval) / 12),
                         chord_age, 2.8, ((1, 1), (2, .15))) for interval in (0, 4, 7)) * .037
        edge = min(1, n / 96, (count - n - 1) / 96)
        layers.append((lead + low + kick + hat + chord) * edge)
    write(output, name, layers, .66)

def cue(output, name, duration, hz, bend, noise, peak):
    rng = random.Random(409)
    values = []
    for n in range(round(duration * RATE)):
        t = n / RATE
        env = min(1, t * 600, (duration - t) * 200) * math.exp(-t * 5 / duration)
        phase = math.tau * (hz * t + bend * t * t)
        pitched = math.sin(phase) + .28 * math.sin(phase * 2.07) + .12 * math.sin(phase * 3)
        values.append(env * ((1 - noise) * pitched + noise * rng.uniform(-1, 1)))
    write(output, name, values, peak)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    output = parser.parse_args().output
    output.mkdir(parents=True, exist_ok=True)
    track(output, "MenuMusic", 108, 261.63, (0, 7, 12, 4, 9, 7, 4, 2), ((1, 1), (2, .18)))
    track(output, "BathtubMusic", 114, 293.66, (0, 12, 7, 16, 7, 4, 9, 4), ((1, 1), (2.01, .3), (3, .09)))
    track(output, "KitchenMusic", 126, 196, (12, 0, 15, 7, 12, 19, 10, 7), ((1, 1), (2, .34), (3, .13)))
    track(output, "TrainingMusic", 116, 220, (0, 4, 7, 12, 16, 12, 9, 7), ((1, 1), (3, .2)))
    for params in (
        ("Click", .075, 820, 1200, .08, .38), ("Hit", .14, 145, -230, .28, .62),
        ("Heavy", .25, 92, -115, .41, .78), ("Block", .18, 990, -480, .09, .61),
        ("GuardBreak", .36, 470, -430, .43, .73), ("Special", .41, 225, 900, .18, .67),
        ("Knockout", .51, 168, -95, .25, .77), ("Round", .23, 590, 340, .02, .48),
        ("Win", .52, 490, 290, .02, .54),
    ):
        cue(output, *params)

if __name__ == "__main__":
    main()
