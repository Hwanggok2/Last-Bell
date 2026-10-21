from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parent.parent/'Evidence/Presentation-v7'
def save(frames, target, durations):
    palette=frames[0].quantize(colors=128)
    # White hit silhouettes must remain white even when absent from frame zero.
    colors=palette.getpalette();colors[765:768]=[255,255,255];palette.putpalette(colors)
    indexed=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    elapsed=0;previous=0;gif_durations=[]
    for ms in durations:
        elapsed+=ms;rounded=round(elapsed/10)*10;gif_durations.append(rounded-previous);previous=rounded
    indexed[0].save(target,save_all=True,append_images=indexed[1:],duration=gif_durations,loop=0,disposal=1,optimize=True)
for kind,ms in [('attack',20),('run',20)]:
    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]
    save(frames,R/f'{kind}.gif',[ms]*(len(frames)-1)+[400 if kind=='attack' else ms])
print('Local rendered animation previews encoded.')

frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/'random').glob('*.png'))]
durations=[350 if i%26==25 else 20 for i in range(len(frames))]
save(frames,R/'attack-random.gif',durations)

for kind,ms in [('transition',25),('hit-flash',20)]:
    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]
    save(frames,R/f'{kind}.gif',[ms]*(len(frames)-1)+[400])
