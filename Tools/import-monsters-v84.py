"""Merge the two independently reviewed native-pixel actor fragments; keep old actors."""
import hashlib
import json
import pathlib
import shutil
from PIL import Image

root = pathlib.Path(__file__).resolve().parents[1]
manifest = root / 'Client/Assets/Game/Art/Presentation/manifest.json'
evidence = root / 'Planning/Evidence/Feedback-v84'
evidence.mkdir(parents=True, exist_ok=True)
before = evidence / 'art-manifest-before.json'
if not before.exists():
    shutil.copy2(manifest, before)
data = json.loads(manifest.read_text(encoding='utf-8'))
old = json.loads(before.read_text(encoding='utf-8'))
replaced = []
for region in ('outskirts', 'residential'):
    path = root / f'Planning/Story/Monster-v84/{region}/manifest-fragment.json'
    fragment = json.loads(path.read_text(encoding='utf-8'))
    for item in fragment['textures']:
        src, dest = root / item['source'], root / 'Client' / item['path']
        assert hashlib.sha256(src.read_bytes()).hexdigest() == item['sha256'], src
        with Image.open(src) as image:
            assert image.size == (item['width'], item['height']), src
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)
        data['textures'] = [x for x in data['textures'] if x['path'] != item['path']]
        data['textures'].append(item)
    for actor in fragment['actors']:
        assert actor['id'] not in ('hero_on', 'bell_knight', 'chain_jailer')
        assert {c['motion'] for c in actor['clips']} == {'Idle', 'Hit', 'Attack', 'Stun'}
        assert len(next(c for c in actor['clips'] if c['motion'] == 'Idle')['frames']) == 1
        data['actors'] = [a for a in data['actors'] if a['id'] != actor['id']]
        data['actors'].append(actor)
        replaced.append(actor['id'])
assert len(set(replaced)) == 6
for actor in old['actors']:
    if actor['id'] not in replaced:
        assert actor == next(a for a in data['actors'] if a['id'] == actor['id']), actor['id']
manifest.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'replaced': replaced, 'preservedActors': len(old['actors'])-6, 'manifestSha256': hashlib.sha256(manifest.read_bytes()).hexdigest()}))
