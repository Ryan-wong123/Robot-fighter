"""Original fabric-creak and victory-bell cues, with no sampled recordings."""
import argparse
import math
from pathlib import Path
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from voice_pipeline import write_pcm

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--output", type=Path, default=Path("Assets/_Project/Art/Audio/Resources/Audio"))
args = parser.parse_args()
targets = [args.output / "PlushCreak.wav", args.output / "PlushWinBell.wav"]
if any(path.exists() for path in targets):
    raise FileExistsError("Choose fresh outputs to preserve existing audio.")
args.output.mkdir(parents=True, exist_ok=True)
rate = 24000
rng = random.Random(403)
creak = []
for index in range(int(rate * .25)):
    time = index / rate
    envelope = math.sin(math.pi * time / .25) ** 2
    creak.append(envelope * (.11 * rng.uniform(-1, 1) + .14 * math.sin(math.tau * (180*time+240*time*time))))
bell = []
for index in range(int(rate * .6)):
    time = index / rate
    attack = min(1, time / .005)
    bell.append(attack * math.exp(-time * 7) * (.20*math.sin(math.tau*1046.5*time) +
        .08*math.sin(math.tau*2093*time) + .04*math.sin(math.tau*3139.5*time)))
for target, samples in zip(targets, (creak, bell)):
    write_pcm(target, samples, rate)
    print("Wrote " + str(target))
