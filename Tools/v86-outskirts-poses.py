"""Compare exact native base and exhausted pose; no source pixel edits."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Planning/Story/Monster-v86/outskirts'
rows=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))['jobs']
out=Image.new('RGB',(3*520,2*270),'#1b2d3d');d=ImageDraw.Draw(out)
for i,row in enumerate(rows):
    key=row['id'];x,y=(i%3)*520,(i//3)*270
    d.text((x+6,y+5),key+' / base | exhausted pose',fill='#f9d296')
    for j,src in enumerate(('animation-input.png','stun-input.png')):
        p=ART/key/src
        if not p.exists():continue
        im=Image.open(p).convert('RGBA').resize((256,192),Image.Resampling.NEAREST)
        out.paste(im,(x+j*260,y+35),im)
out.save(ART/'stun-pose-review.png')
print('Compared source base and stun poses.')
