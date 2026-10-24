"""Assemble review GIFs from the unmodified generated frames (not game assets)."""
import json,pathlib
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v83'
reviews=json.loads((root/'review.json').read_text(encoding='utf-8'))
for item in reviews:
    key=item['id']
    for motion in ['attack','stun']:
        frames=[]
        for file in sorted((root/f'raw/{key}/{motion}').glob('*.png')):
            im=Image.open(file).convert('RGBA');bg=Image.new('RGB',im.size,'#192830');bg.paste(im,(0,0),im);frames.append(bg)
        holds=[40]*len(frames) if motion=='attack' else [85]*len(frames)
        if motion=='attack':holds[item['attackContactFrame']]=60;holds[-1]+=900
        frames[0].save(root/f'{key}-{motion}.gif',save_all=True,append_images=frames[1:],duration=holds,loop=0,optimize=False)
print('12 native-frame preview GIFs created')
