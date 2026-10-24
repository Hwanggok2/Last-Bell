"""Review generated source art at current and intended gameplay scales; never changes Unity art."""
import pathlib,json
from PIL import Image,ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
source=ROOT/'Planning/Story/Monster-v85/residential'
hero=Image.open(ROOT/'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png').convert('RGBA')
ids=['sewer_rat','drowned_resident','water_wraith']
def actor(path,height):
    im=Image.open(path).convert('RGBA');b=im.getbbox();scale=height/(b[3]-b[1]);im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.NEAREST)
    return im,tuple(round(x*scale) for x in b)
def draw_actor(canvas,path,x,floor,height):
    im,b=actor(path,height);canvas.paste(im,(x-b[0],floor-b[3]),im)
for key in ids:
    candidates=sorted((source/'candidates'/key).glob('*.png'))
    if not candidates:continue
    panel=Image.new('RGB',(4*320,430),'#182834');d=ImageDraw.Draw(panel)
    for n,path in enumerate(candidates):
        x=n*320;d.text((x+10,8),f'{key} / candidate {n}',fill='white')
        im=Image.open(path).convert('RGBA');im=im.resize((256,256),Image.Resampling.NEAREST);panel.paste(im,(x+30,30),im)
        draw_actor(panel,ROOT/'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png',x+35,405,96)
        height={'sewer_rat':70,'drowned_resident':112,'water_wraith':122}[key]
        draw_actor(panel,path,x+135,405,height)
        d.text((x+10,410),'Hero96px / candidate play scale',fill='#d4a04a')
    panel.save(source/f'{key}-candidate-review.png')
if all((source/'base'/f'{key}.png').exists() for key in ids):
    panel=Image.new('RGB',(1120,600),'#182834');d=ImageDraw.Draw(panel)
    for row,scale in enumerate([1,2]):
        floor=230+row*330;d.text((18,floor-210),'Gameplay scale x'+str(scale)+' / same ground line',fill='white')
        for n,(key,wh) in enumerate([('hero',1.1),('sewer_rat',.8),('drowned_resident',1.28),('water_wraith',1.4)]):
            path=ROOT/'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png' if key=='hero' else source/'base'/f'{key}.png'
            draw_actor(panel,path,35+n*275,floor,round(96*wh/1.1*scale));d.text((35+n*275,floor+8),key,fill='white')
    panel.save(source/'base-game-scale-review.png')
print('Review comparisons saved')
