"""Synthesize original guitar/bongo music, backyard ambience and sandbox hazard cues without recordings."""
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
durations = {'SandPurr': 2, 'SandSwirl': 2, 'SandThump': .3, 'SandAmbience': 8, 'SandMusic': 8}
targets = [args.output / (name + '.wav') for name in durations]
if any(target.exists() for target in targets):
    raise FileExistsError('Choose fresh output paths.')
args.output.mkdir(parents=True, exist_ok=True)
rng = random.Random(405)
melody = [523.25, 659.25, 783.99, 659.25, 698.46, 587.33, 493.88, 587.33,
          523.25, 659.25, 880, 783.99, 698.46, 659.25, 587.33, 493.88]
for name, duration in durations.items():
    samples = []
    for index in range(int(duration * rate)):
        t = index / rate
        if name == 'SandPurr':
            pulse=.5+.5*math.sin(math.tau*26*t)
            value=.13*pulse*(math.sin(math.tau*105*t)+.3*math.sin(math.tau*210*t))
        elif name == 'SandSwirl':
            envelope=math.sin(math.pi*t/2)**2
            value=envelope*(.13*rng.uniform(-1,1)+.05*math.sin(math.tau*(160*t+30*t*t)))
        elif name == 'SandThump':
            value=.3*math.exp(-18*t)*(math.sin(math.tau*75*t)+.25*rng.uniform(-1,1))
        elif name == 'SandAmbience':
            value=.025*math.sin(math.tau*2400*t)*max(0,math.sin(math.tau*2*t))**8
            value+=.025*math.sin(math.tau*144*t)*(.5+.5*math.sin(math.tau*t/8))
        else:
            beat=int(t*4);local=t%.25
            note=melody[(beat//2)%len(melody)]*.5
            pluck=sum(math.sin(math.tau*note*h*local)/h**1.8 for h in range(1,5))
            value=.16*pluck*math.exp(-local*13)
            drum=95 if beat%2 else 145
            value+=.09*math.sin(math.tau*(drum*local+6*(1-math.exp(-30*local))))*math.exp(-local*30)
        # Music uses gated notes; only non-looping cues need the outer fade.
        if name not in ('SandMusic', 'SandAmbience'):
            value *= min(1, t/.005, (duration-t)/.02)
        samples.append(value)
    write_pcm(args.output / (name + '.wav'), samples, rate)
    print('Wrote', name)
