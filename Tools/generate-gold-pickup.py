"""Create the original short pickup tick (no sampled or third-party audio)."""
from pathlib import Path
import math, struct, wave
rate=44100
path=Path(__file__).resolve().parents[1]/'Client/Assets/Game/Audio/Reviewed-v4/gold-pickup.wav'
samples=[]
for i in range(round(rate*.085)):
    t=i/rate
    envelope=min(1,t/.002)*math.exp(-t/0.018)*(1-t/.085)
    phase=2*math.pi*(1650*t+1050*t*t)
    value=envelope*(.52*math.sin(phase)+.2*math.sin(phase*1.5)+.12*math.sin(phase*2.4))
    samples.append(struct.pack('<h',int(max(-1,min(1,value))*24000)))
with wave.open(str(path),'wb') as f:
    f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(b''.join(samples))
print(path)
