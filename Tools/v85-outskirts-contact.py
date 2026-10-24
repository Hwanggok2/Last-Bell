"""Magnify approved contact candidates with original source pixel coordinates."""
from PIL import Image,ImageDraw
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Planning/Story/Monster-v85/outskirts'
for key,index in [('wild_dog',8),('grey_wolf',6),('brown_bear',8)]:
    im=Image.open(ART/key/'attack'/f'{index:02}.png').convert('RGBA')
    tile=Image.new('RGB',(128*6+50,96*6+50),'#20343d')
    zoom=im.resize((768,576),Image.Resampling.NEAREST)
    tile.paste(zoom,(32,32),zoom)
    d=ImageDraw.Draw(tile)
    for x in range(0,129,8):
        d.line((32+x*6,32,32+x*6,608),fill='#38505b')
        d.text((32+x*6,14),str(x),fill='white')
    for y in range(0,97,8):
        d.line((32,32+y*6,800,32+y*6),fill='#38505b')
        d.text((4,32+y*6),str(y),fill='white')
    tile.save(ART/f'{key}-contact-grid.png')
print('Saved grids without changing source pixels.')
