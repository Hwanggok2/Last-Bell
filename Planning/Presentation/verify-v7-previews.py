from pathlib import Path
from PIL import Image,ImageSequence
import json
R=Path(__file__).resolve().parent.parent/'Evidence/Presentation-v7';report={}
for name,expected in [('run',1000),('transition',2650),('hit-flash',1380)]:
    im=Image.open(R/f'{name}.gif');frames=[(f.convert('RGB').copy(),f.info.get('duration',0)) for f in ImageSequence.Iterator(im)]
    total=sum(ms for _,ms in frames);assert total==expected,(name,total,expected)
    white=max(sum(n for n,c in f.getcolors(f.width*f.height) if c==(255,255,255)) for f,_ in frames)
    if name=='hit-flash':assert white>100
    report[name]={'frames':len(frames),'durationMs':total,'maxExactWhitePixels':white,'bytes':(R/f'{name}.gif').stat().st_size}
(R/'preview-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
