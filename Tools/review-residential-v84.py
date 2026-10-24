import pathlib,json
from PIL import Image,ImageDraw
root=pathlib.Path(__file__).resolve().parents[1]
source=root/'Planning/Story/Monster-v84/residential'
ids=['sewer_rat','drowned_resident','water_wraith']
jobs={j['id']:j for j in json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']}
decisions=json.loads((source/'review-decisions.json').read_text(encoding='utf-8')) if (source/'review-decisions.json').exists() else {}
canvas=Image.new('RGB',(4*270,320),'#182834');d=ImageDraw.Draw(canvas)
for n,id in enumerate(['hero']+ids):
    im=Image.open(root/'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png' if id=='hero' else source/f'{id}.png').convert('RGBA')
    if id=='hero': im=im.resize((64,64),Image.Resampling.NEAREST)
    im=im.resize((256,256),Image.Resampling.NEAREST)
    canvas.paste(im,(n*270,28),im);d.text((n*270+5,8),id,fill='white')
canvas.save(source/'bases-review.png')
metrics={}
for key in ids:
    canvas=Image.new('RGB',(6*260,5*225),'#182834');d=ImageDraw.Draw(canvas)
    metrics[key]={}
    for motion,startrow in [('attack',0),('stun',3)]:
        paths=sorted((source/key/jobs[key].get(motion+'Dir',motion)).glob('*.png'))
        if not paths:continue
        frames=[];measurements=[]
        for i,path in enumerate(paths):
            im=Image.open(path).convert('RGBA');box=im.getbbox()
            assert im.size==(128,96),(path,im.size)
            assert box is not None,path
            rgba=set(im.getdata());opaque={c[:3] for c in rgba if c[3]}
            measurements.append({'frame':i,'bbox':box,'colors':len(opaque),'alphaValues':sorted({c[3] for c in rgba}),'touchesCanvasEdge':box[0]==0 or box[1]==0 or box[2]==128 or box[3]==96})
            x=i%6*260;y=(startrow+i//6)*225
            excluded=key in decisions and i not in decisions[key][motion]['sourceFrames']
            d.text((x+6,y+5),f'{motion} {i:02}'+(' REJECTED' if excluded else ''),fill='#e97b68' if excluded else 'white')
            enlarged=im.resize((256,192),Image.Resampling.NEAREST);canvas.paste(enlarged,(x+2,y+25),enlarged)
            frame=Image.new('RGB',(512,384),'#182834');frame.paste(im.resize((512,384),Image.Resampling.NEAREST),(0,0),im.resize((512,384),Image.Resampling.NEAREST));frames.append(frame)
        metrics[key][motion]=measurements
        selected=decisions.get(key,{}).get(motion)
        if selected:
            frames=[frames[i] for i in selected['sourceFrames']];holds=selected['holdsMs'].copy()
            if motion=='attack':holds[-1]+=900
        else:holds=[45 if motion=='attack' else 85]*len(frames)
        frames[0].save(source/f'{key}-{motion}.gif',save_all=True,append_images=frames[1:],duration=holds,loop=0)
    canvas.save(source/f'{key}-review.png')
(source/'frame-metrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print('Base comparisons, all-frame contact sheets, GIF previews and geometry/color metrics saved')
for key,contact in [('sewer_rat',8),('drowned_resident',8),('water_wraith',7)]:
    path=source/key/jobs[key]['attackDir']/f'{contact:02}.png'
    if not path.exists():continue
    im=Image.open(path).convert('RGBA').resize((768,576),Image.Resampling.NEAREST)
    panel=Image.new('RGB',(768,600),'#182834');panel.paste(im,(0,24),im);draw=ImageDraw.Draw(panel)
    for x in range(0,128,10):draw.line((x*6,24,x*6,599),fill='#34505b');draw.text((x*6+2,8),str(x),fill='white')
    for y in range(0,96,10):draw.line((0,y*6+24,767,y*6+24),fill='#34505b');draw.text((2,y*6+26),str(y),fill='white')
    panel.save(source/f'{key}-contact-grid.png')
