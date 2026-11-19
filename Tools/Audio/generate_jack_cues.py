"""Regenerate original Mr. Jack music-box warning and copy his approved laugh."""
import array, math, shutil, wave
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'Assets/_Project/Art/Audio/Resources/Audio'
out.mkdir(parents=True,exist_ok=True)
shutil.copy2(root/'Art/_Source/Audio/D08/shortened/mrjack_win_v01.wav',out/'MrJackLaugh.wav')
samples=[];rate=24000
for i in range(36000):
 t=i/rate;step=int(t/.25);local=t%.25
 freq=[523.25,659.25,783.99,659.25,587.33,783.99][min(step,5)]
 envelope=math.exp(-local*12)*min(1,local/.004)
 sample=.21*envelope*(math.sin(math.tau*freq*t)+.25*math.sin(math.tau*freq*2*t))
 samples.append(int(max(-1,min(1,sample))*32767))
with wave.open(str(out/'MrJackMusicBox.wav'),'wb') as audio:
 audio.setparams((1,2,rate,0,'NONE','not compressed'));audio.writeframes(array.array('h',samples).tobytes())
