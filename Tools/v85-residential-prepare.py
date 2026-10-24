"""Transparent canvas room only; preserve every source pixel unchanged."""
import pathlib,json
from PIL import Image
source=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v85/residential'
(source/'prepared').mkdir(exist_ok=True)
proof=[]
for key in ['sewer_rat','drowned_resident','water_wraith']:
    src=Image.open(source/'base'/f'{key}.png').convert('RGBA');dst=Image.new('RGBA',(192,160));dst.paste(src,(32,16))
    assert dst.crop((32,16,160,144)).tobytes()==src.tobytes()
    dst.save(source/'prepared'/f'{key}.png');proof.append({'id':key,'size':[192,160],'offset':[32,16],'sourcePixelsUnchanged':True,'bounds':dst.getbbox()})
(source/'padding-proof.json').write_text(json.dumps(proof,indent=2),encoding='utf-8')
print('192x160 motion canvases prepared; no source resampling')
