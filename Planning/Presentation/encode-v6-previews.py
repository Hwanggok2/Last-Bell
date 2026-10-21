from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parent.parent/'Evidence/Presentation-v6'
def save(frames, target, durations):
    palette=frames[0].quantize(colors=128)
    indexed=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    indexed[0].save(target,save_all=True,append_images=indexed[1:],duration=durations,loop=0,disposal=1,optimize=True)
for kind,ms in [('attack',20),('run',20)]:
    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]
    save(frames,R/f'{kind}.gif',[ms]*(len(frames)-1)+[400 if kind=='attack' else ms])
print('Local rendered animation previews encoded.')

frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/'random').glob('*.png'))]
durations=[350 if i%26==25 else 20 for i in range(len(frames))]
save(frames,R/'attack-random.gif',durations)
