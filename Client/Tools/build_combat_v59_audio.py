"""Original quiet 70ms metallic beat, sampled once per gold flash. No source samples."""
from pathlib import Path
import wave,math,struct
target=Path(__file__).resolve().parents[1]/'Assets/Game/Audio/Presentation/enemy-beat-v59.wav'
rate=44100
samples=[]
for n in range(int(rate*.07)):
    t=n/rate
    envelope=min(1,t/.002)*math.exp(-t*65)*min(1,(.07-t)/.01)
    value=(math.sin(2*math.pi*880*t)+.4*math.sin(2*math.pi*2376*t))*envelope*.22
    samples.append(struct.pack('<h',round(max(-1,min(1,value))*32767)))
with wave.open(str(target),'wb') as w:
    w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(b''.join(samples))
print(target)
