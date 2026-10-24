import pathlib,json
from PIL import Image,ImageDraw
R=pathlib.Path(__file__).resolve().parents[1];P=R/'Planning/Story/Monster-v86/residential';s=json.loads((P/'existing-snapshot.json').read_text(encoding='utf-8'));T={t['path']:t for t in s['textures']}
out=Image.new('RGB',(1080,560),'#192c36');d=ImageDraw.Draw(out);grid=Image.new('RGB',(900,600),'#192c36');gd=ImageDraw.Draw(grid)
hero=Image.open(R/'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png').convert('RGBA');hs=540/6.2*1.1/84
for n,a in enumerate(s['actors']):
    x=n%3*360;y=n//3*280;idle=next(c for c in a['clips'] if c['motion']=='Idle');im=Image.open(R/'Client'/idle['frames'][0]).convert('RGBA');pivot=T[idle['frames'][0]]['pivot'];scale=540/6.2*a['height']/a['referenceHeightPixels'];baseY=y+235
    d.text((x+12,y+8),a['id']+' / existing height '+str(a['height']),fill='white');d.line((x+10,baseY,x+350,baseY),fill='#506976')
    for sprite,k,px,py,cx in [(hero,hs,60,114,x+100),(im,scale,pivot['x'],pivot['y'],x+260)]:
        up=sprite.resize((round(sprite.width*k),round(sprite.height*k)),Image.Resampling.NEAREST);out.paste(up,(round(cx-px*k),round(baseY-py*k)),up)
    atk=next(c for c in a['clips'] if c['motion']=='Attack');contact=Image.open(R/'Client'/atk['frames'][atk['contactFrame']]).convert('RGBA');gx=n%3*300;gy=n//3*300;up=contact.resize((256,256),Image.Resampling.NEAREST);grid.paste(up,(gx+20,gy+30),up);gd.text((gx+6,gy+6),a['id']+' contact '+str(atk['contactFrame']),fill='white')
    for v in range(0,128,20):
        gd.line((gx+20+v*2,gy+30,gx+20+v*2,gy+286),fill='#49616c');gd.text((gx+22+v*2,gy+31),str(v),fill='white');gd.line((gx+20,gy+30+v*2,gx+276,gy+30+v*2),fill='#49616c');gd.text((gx+21,gy+32+v*2),str(v),fill='white')
out.save(P/'existing-game-scale-review.png');grid.save(P/'existing-contact-coordinate-review.png')
print('Actual camera scale and source contact grids saved without modifying source art.')
