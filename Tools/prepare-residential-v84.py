"""Add transparent motion room without resizing or changing one source pixel."""
import pathlib,json
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v84/residential'
(root/'prepared').mkdir(exist_ok=True)
proof=[]
for key in ['sewer_rat','drowned_resident','water_wraith']:
    src=Image.open(root/f'{key}.png').convert('RGBA')
    dst=Image.new('RGBA',(128,96));dst.paste(src,(32,16))
    assert dst.crop((32,16,96,80)).tobytes()==src.tobytes()
    dst.save(root/'prepared'/f'{key}.png')
    proof.append({'id':key,'originalSize':list(src.size),'preparedSize':list(dst.size),'offset':[32,16],'sourcePixelsUnchanged':True,'originalBounds':src.getbbox(),'preparedBounds':dst.getbbox()})
(root/'padding-proof.json').write_text(json.dumps(proof,indent=2),encoding='utf-8')
print('Transparent padding only; all original source pixels verified identical')
