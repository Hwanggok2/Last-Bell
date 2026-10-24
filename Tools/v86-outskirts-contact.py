"""Magnify specified source contact candidates with pixel-coordinate grid."""
from PIL import Image,ImageDraw
from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Planning/Story/Monster-v86/outskirts'
jobs={r['id']:r for r in json.loads((ART/'jobs.json').read_text(encoding='utf-8'))['jobs']}
for arg in sys.argv[1:]:
    parts=arg.split(':');key,index=parts[:2];index=int(index)
    directory=parts[2] if len(parts)>2 else jobs[key].get('attackDir','attack')
    im=Image.open(ART/key/directory/f'{index:02}.png').convert('RGBA')
    tile=Image.new('RGB',(818,626),'#20343d')
    zoom=im.resize((768,576),Image.Resampling.NEAREST);tile.paste(zoom,(32,32),zoom);d=ImageDraw.Draw(tile)
    for x in range(0,129,8):
        d.line((32+x*6,32,32+x*6,608),fill='#38505b');d.text((32+x*6,14),str(x),fill='white')
    for y in range(0,97,8):
        d.line((32,32+y*6,800,32+y*6),fill='#38505b');d.text((4,32+y*6),str(y),fill='white')
    tile.save(ART/f'{key}-contact-grid.png')
print('Contact coordinate grids created.')
