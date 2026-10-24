"""Import reviewed new animals and restored residents; preserve every existing attack."""
from pathlib import Path
import hashlib,json,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'Client/Assets/Game/Art/Presentation/manifest.json'
before=json.loads((ROOT/'Planning/Evidence/Feedback-v86/art-manifest-before.json').read_text(encoding='utf-8'))
data=json.loads(path.read_text(encoding='utf-8'))
added={'red_fox','wild_boar','badger','lynx','mountain_goat','great_stag'}
restored={'ash_wisp','chain_thrall','hook_hound','chain_puppet','cracked_blade','chain_dancer'}
seen=set()
for region in ['outskirts','residential']:
    fragment=json.loads((ROOT/f'Planning/Story/Monster-v86/{region}/manifest-fragment.json').read_text(encoding='utf-8'))
    for texture in fragment['textures']:
        src=ROOT/texture['source'];dst=ROOT/'Client'/texture['path']
        assert hashlib.sha256(src.read_bytes()).hexdigest()==texture['sha256'],src
        with Image.open(src) as im:assert im.size==(texture['width'],texture['height'])
        dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
        data['textures']=[t for t in data['textures'] if t['path']!=texture['path']]+[texture]
    for actor in fragment['actors']:
        id=actor['id'];assert id in added|restored and id not in seen
        clips={c['motion']:c for c in actor['clips']};idle=clips['Idle']
        assert idle['loop'] and len(idle['frames'])>1
        assert len({hashlib.sha256((ROOT/'Client'/p).read_bytes()).hexdigest() for p in idle['frames']})>1
        if id in restored:
            original=next(a for a in before['actors'] if a['id']==id)
            assert {k:v for k,v in actor.items() if k!='clips'}=={k:v for k,v in original.items() if k!='clips'}
            assert [c for c in actor['clips'] if c['motion']!='Idle']==[c for c in original['clips'] if c['motion']!='Idle'],id
        data['actors']=[a for a in data['actors'] if a['id']!=id]+[actor];seen.add(id)
assert seen==added|restored
for actor in before['actors']:
    if actor['id'] not in restored:assert actor==next(a for a in data['actors'] if a['id']==actor['id'])
data['scope']='v8.6: twenty distinct playable stage designs; six new animals and six restored residents with reviewed Idle. Original hero, bosses, textures and existing non-Idle clips preserved.'
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Imported six new animals and six restored natural Idles; all old non-Idle clips preserved')
