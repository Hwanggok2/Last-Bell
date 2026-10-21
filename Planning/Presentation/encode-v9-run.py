from pathlib import Path
from PIL import Image, ImageSequence
import json
R=Path(__file__).resolve().parent.parent/'Evidence/Presentation-v9'
report={}
for kind,ms,expected in [('run',20,2000),('transition',25,3650)]:
    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]
    palette=frames[0].quantize(colors=192)
    indexed=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    durations=[ms]*len(frames)
    if kind=='transition':durations[-1]=400
    elapsed=0;previous=0;rounded=[]
    for duration in durations:
        elapsed+=duration;point=round(elapsed/10)*10;rounded.append(point-previous);previous=point
    target=R/f'{kind}.gif';indexed[0].save(target,save_all=True,append_images=indexed[1:],duration=rounded,loop=0,disposal=1,optimize=True)
    im=Image.open(target);total=sum(f.info['duration'] for f in ImageSequence.Iterator(im));assert total==expected,(kind,total)
    report[kind]={'durationMs':total,'frames':im.n_frames,'bytes':target.stat().st_size}
(R/'preview-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
