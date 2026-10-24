"""Inspect every retry frame without overwriting first-pass source sheets."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Planning/Story/Monster-v86/outskirts'
jobs=json.loads((ART/'retry-jobs.json').read_text(encoding='utf-8'));metrics={}
for row in jobs:
    paths=sorted((ART/row['id']/row['directory']).glob('*.png'))
    if not paths:continue
    sheet=Image.new('RGB',(1560,((len(paths)+5)//6)*224+30),'#20343d');d=ImageDraw.Draw(sheet)
    d.text((8,7),row['id']+' '+row['motion']+' RETRY / source native x2',fill='#f9d296');records=[]
    for i,path in enumerate(paths):
        im=Image.open(path).convert('RGBA');x,y=(i%6)*260,30+(i//6)*224
        zoom=im.resize((256,192),Image.Resampling.NEAREST);sheet.paste(zoom,(x+2,y+22),zoom);d.text((x+5,y+3),f'{i:02}',fill='white')
        records.append({'frame':i,'bounds':im.getbbox(),'footHash':hashlib.sha256(im.crop((0,76,128,82)).tobytes()).hexdigest(),'pixelHash':hashlib.sha256(im.tobytes()).hexdigest(),'whitePixels':sum(p[3]>0 and min(p[:3])>230 for p in im.getdata()),'colors':len(set(p[:3] for p in im.getdata() if p[3]))})
    metrics[row['id']+'-'+row['motion']]=records
    sheet.save(ART/(row['id']+'-'+row['motion']+'-retry-review.png'))
(ART/'retry-frame-metrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print('Retry frame sheets and pixel metrics created.')
