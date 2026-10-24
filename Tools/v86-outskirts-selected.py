"""Render the exact selected timeline, including native-frame ping-pong loops."""
import json,pathlib
from PIL import Image,ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/'Planning/Story/Monster-v86/outskirts'
jobs=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))
reviews=json.loads((ART/'review-decisions.json').read_text(encoding='utf-8'))
for row in jobs['jobs']:
    key=row['id']
    for motion in ('idle','attack','stun'):
        decision=reviews[key][motion]
        if decision.get('status')!='approved':continue
        indices=decision['sourceFrames']
        sheet=Image.new('RGB',(6*260,((len(indices)+5)//6)*224+30),'#20343d');d=ImageDraw.Draw(sheet)
        d.text((8,7),key+' '+motion+' SELECTED / native source x2 / '+str(sum(decision['holdsMs']))+'ms',fill='white')
        for i,n in enumerate(indices):
            im=Image.open(ART/key/row.get(motion+'Dir',motion)/f'{n:02}.png').convert('RGBA')
            x,y=i%6*260,30+(i//6)*224;zoom=im.resize((256,192),Image.Resampling.NEAREST)
            sheet.paste(zoom,(x+2,y+22),zoom)
            label=f'{i:02} source {n:02}'+(' CONTACT' if i==decision.get('contactFrame',-1) else '')
            d.text((x+5,y+3),label,fill='#f9d296')
        sheet.save(ART/f'{key}-{motion}-selected.png')
print('Selected timeline sheets generated.')
