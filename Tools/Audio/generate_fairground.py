"""Synthesize original carnival music, ambience and fairground hazard cues without recordings."""
import argparse
import math
from pathlib import Path
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from voice_pipeline import write_pcm

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
rate = 24000
durations = {'FairPadWarning': 2, 'FairPump': 2, 'FairBoing': .65, 'FairAmbience': 8, 'FairMusic': 8}
targets = [args.output / (name + '.wav') for name in durations]
if any(target.exists() for target in targets):
    raise FileExistsError('Choose fresh output paths.')
args.output.mkdir(parents=True, exist_ok=True)
rng = random.Random(404)
melody = [523.25, 659.25, 783.99, 659.25, 698.46, 587.33, 493.88, 587.33,
          523.25, 659.25, 880, 783.99, 698.46, 659.25, 587.33, 493.88]
for name, duration in durations.items():
    samples = []
    for index in range(int(duration * rate)):
        t = index / rate
        if name == 'FairPadWarning':
            local = t % .5
            frequency = 660 if int(t * 2) % 2 == 0 else 880
            value = .24 * math.exp(-local * 12) * math.sin(math.tau * frequency * local)
        elif name == 'FairPump':
            pulse = max(0, math.sin(math.tau * t * 2)) ** 2
            value = pulse * (.15 * rng.uniform(-1, 1) + .13 * math.sin(math.tau * (95*t + 25*t*t)))
        elif name == 'FairBoing':
            value = .3 * math.exp(-6*t) * math.sin(math.tau * (180*t + 65*(1-math.exp(-8*t))))
        elif name == 'FairAmbience':
            # A quiet continuous airy fairground bed; periodic tones make a clean loop.
            value = .06 * math.sin(math.tau * 96*t) * (.5 + .5*math.sin(math.tau*t/8))
            value += .025 * math.sin(math.tau * 144*t)
        else:
            beat = int(t * 4)
            local = t % .25
            note = melody[(beat // 2) % len(melody)]
            organ = math.sin(math.tau*note*t) + .25*math.sin(math.tau*note*2*t)
            value = .11*organ*math.sin(math.pi*local/.25)**2
            bass = 130.8125 if (beat // 8) % 2 == 0 else 146.8325
            value += .10 * math.sin(math.tau*bass*local) * math.exp(-local*18)
        # Music uses gated notes; only non-looping cues need the outer fade.
        if name not in ('FairMusic', 'FairAmbience'):
            value *= min(1, t/.005, (duration-t)/.02)
        samples.append(value)
    write_pcm(args.output / (name + '.wav'), samples, rate)
    print('Wrote', name)
