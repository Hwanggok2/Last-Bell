"""Compare retained and new wildlife side by side at real world-size ratios."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Planning/Story/Monster-v86/outskirts'
old=ROOT/'Planning/Story/Monster-v85/outskirts'
jobs=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))
prior=json.loads((old/'jobs.json').read_text(encoding='utf-8'))
rows=[{**r,'stage':i+1,'root':old} for i,r in enumerate(prior['jobs'])]+[{**r,'root':ART} for r in jobs['jobs']]
hero=Image.open(ROOT/jobs['style']['reference']).convert('RGBA');hs=1.1*540/6.2/84
hp=hero.resize((round(128*hs),round(128*hs)),Image.Resampling.NEAREST)
out=Image.new('RGB',(3*420,3*280),'#1b2d3d');d=ImageDraw.Draw(out)
for i,row in enumerate(rows):
    x,y=(i%3)*420,(i//3)*280;im=Image.open(row['root']/row['id']/'animation-input.png').convert('RGBA');b=im.getbbox();ref=b[3]-b[1]
    scale=row['height']*540/6.2/ref;im=im.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if row['facesRight'] else im
    zoom=im.resize((round(128*scale),round(96*scale)),Image.Resampling.NEAREST);floor=y+238
    d.text((x+10,y+10),f"STAGE {row['stage']}   {row['id']}"+('   MIDBOSS' if row['stage']==5 else ''),fill='#f9d296')
    d.text((x+10,y+30),'retained v85' if row['stage']<=3 else 'new v86',fill='#8fa7a2')
    out.paste(hp,(x+10,floor-round(114*hs)),hp);out.paste(zoom,(x+270-round(64*scale),floor-round(80*scale)),zoom)
    d.line((x+10,floor,x+400,floor),fill='#34505b')
out.save(ART/'stage-lineup.png')
print('Stage 1-9 lineup comparison created.')
