import pathlib,json,hashlib
from PIL import Image,ImageDraw
R=pathlib.Path(__file__).resolve().parents[1]
P=R/'Planning/Story/Monster-v86/residential';P.mkdir(parents=True,exist_ok=True)
ids=['chain_thrall','ash_wisp','cracked_blade','chain_dancer','chain_puppet','hook_hound']
d=json.loads((R/'Client/Assets/Game/Art/Presentation/manifest.json').read_text(encoding='utf-8'))
actors=[a for key in ids for a in d['actors'] if a['id']==key]
paths={f for a in actors for c in a['clips'] for f in c['frames']}
snap={'actors':actors,'textures':[t for t in d['textures'] if t['path'] in paths]}
(P/'existing-snapshot.json').write_text(json.dumps(snap,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
sheet=Image.new('RGB',(900,660),'#192c36');draw=ImageDraw.Draw(sheet)
for n,a in enumerate(actors):
    idle=next(c for c in a['clips'] if c['motion']=='Idle');im=Image.open(R/'Client'/idle['frames'][0]).convert('RGBA');x=n%3*300;y=n//3*330
    draw.text((x+10,y+10),a['id'],fill='white');up=im.resize((256,256),Image.Resampling.NEAREST);sheet.paste(up,(x+20,y+40),up)
    folder=P/a['id'];folder.mkdir(exist_ok=True);im.save(folder/'base.png')
    for c in a['clips']:
        fs=c['frames']
        if not fs:continue
        out=Image.new('RGB',(5*204,((len(fs)+4)//5)*190+30),'#192c36');dr=ImageDraw.Draw(out);dr.text((8,8),a['id']+' / '+c['motion']+' EXISTING',fill='white')
        for i,f in enumerate(fs):
            image=Image.open(R/'Client'/f).convert('RGBA');bx=i%5*204;by=i//5*190+30;dr.text((bx+6,by+3),str(i)+(' CONTACT' if i==c['contactFrame'] else ''),fill='#f9d296');out.paste(image,(bx+6,by+24),image)
        out.save(P/(a['id']+'-'+c['motion'].lower()+'-existing.png'))
sheet.save(P/'existing-bases-review.png')
print('Existing source snapshot and all native frames exported for inspection; no runtime assets modified.')
