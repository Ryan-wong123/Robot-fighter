"""Original deterministic air-pop cue; no sampled recordings or external content."""
import math
from pathlib import Path
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from voice_pipeline import write_pcm

path = Path("Assets/_Project/Art/Audio/Resources/Audio/PetePop.wav")
if path.exists():
    raise FileExistsError("Preserve existing audio; choose a fresh output before regenerating.")
path.parent.mkdir(parents=True, exist_ok=True)
rate = 24000
rng = random.Random(402)
samples = []
filtered = 0.0
for index in range(int(rate * .8)):
    time = index / rate
    filtered = .65 * filtered + .35 * rng.uniform(-1, 1)
    attack = min(1, time / .006)
    decay = (1 - time / .8) ** 1.6
    chirp = math.sin(math.tau * (650 * time - 270 * time * time)) * math.exp(-time * 14)
    samples.append(attack * decay * (.5 * filtered + .13 * chirp))
write_pcm(path, samples, rate)
print("Wrote " + str(path))
