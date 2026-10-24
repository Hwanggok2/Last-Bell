import json,pathlib
from PIL import Image,ImageDraw
root=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v83'
jobs=json.loads((root/'jobs.json').read_text(encoding='utf-8'))['jobs']
metrics={}
for item in jobs:
    key=item['id'];canvas=Image.new('RGB',(9*132,4*156+28),'#192830');draw=ImageDraw.Draw(canvas)
    draw.text((8,8),key,fill='white');metrics[key]={}
    for motion,row in [('attack',0),('stun',2)]:
        paths=sorted((root/f'raw/{key}/{motion}').glob('*.png'));boxes=[]
        for i,path in enumerate(paths):
            im=Image.open(path).convert('RGBA');box=im.getbbox();boxes.append(box)
            x=(i%9)*132;y=28+(row+i//9)*156
            draw.text((x+4,y+4),motion+' '+str(i),fill='white');canvas.paste(im,(x+2,y+24),im)
        metrics[key][motion]=boxes
    canvas.save(root/f'{key}-review.png')
(root/'frame-bounds.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print('6 contact sheets + frame geometry saved')
