"""Generate original short toy-item sounds without sampled recordings."""
import argparse, math, random, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from voice_pipeline import write_pcm
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args();names=['ItemPie','ItemBoomerang','ItemPogo','ItemBubble','ItemCar','ItemJack']
if any((args.output/(n+'.wav')).exists() for n in names):raise FileExistsError('Choose fresh outputs.')
args.output.mkdir(parents=True,exist_ok=True);rng=random.Random(406);rate=24000
for kind,name in enumerate(names):
 duration=.45 if kind in (1,4) else .3
 samples=[]
 for i in range(int(rate*duration)):
  t=i/rate;u=t/duration;envelope=math.sin(math.pi*u)**2
  if kind==0:value=.27*rng.uniform(-1,1)*math.exp(-10*t)+.1*math.sin(math.tau*95*t)
  elif kind==1:value=.15*rng.uniform(-1,1)*(.5+.5*math.sin(math.tau*18*t))
  elif kind in (2,5):value=.3*math.sin(math.tau*(150*t+45*(1-math.exp(-12*t))))*math.exp(-5*t)
  elif kind==3:value=.3*math.sin(math.tau*(700*t-650*t*t))
  else:value=.12*(math.sin(math.tau*180*t)+.25*rng.uniform(-1,1))
  samples.append(envelope*value)
 write_pcm(args.output/(name+'.wav'),samples,rate)
 print('Wrote',name)
