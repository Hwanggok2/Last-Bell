"""Import reviewed v8.5 frames while preserving hero, encounters and boss actions."""
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image

root = Path(__file__).resolve().parents[1]
manifest = root / 'Client/Assets/Game/Art/Presentation/manifest.json'
before_path = root / 'Planning/Evidence/Feedback-v85/art-manifest-before.json'
before = json.loads(before_path.read_text(encoding='utf-8'))
data = json.loads(manifest.read_text(encoding='utf-8'))
creatures = {'wild_dog', 'grey_wolf', 'brown_bear', 'sewer_rat', 'drowned_resident', 'water_wraith'}
bosses = {'bell_knight_concept', 'chain_jailer'}
replaced = set()
for region in ('outskirts', 'residential', 'bosses'):
    fragment = json.loads((root / f'Planning/Story/Monster-v85/{region}/manifest-fragment.json').read_text(encoding='utf-8'))
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
        name = actor['id']
        assert name in creatures | bosses and name not in replaced, name
        clips = {c['motion']: c for c in actor['clips']}
        idle = clips['Idle']
        assert len(idle['frames']) > 1 and idle['loop']
        assert len(idle['frames']) == len(idle['frameDurationsMs'])
        assert all(t > 0 for t in idle['frameDurationsMs'])
        assert len({hashlib.sha256((root/'Client'/p).read_bytes()).hexdigest() for p in idle['frames']}) > 1, name+' static idle'
        if name in bosses:
            original = next(a for a in before['actors'] if a['id'] == name)
            assert {k:v for k,v in actor.items() if k != 'clips'} == {k:v for k,v in original.items() if k != 'clips'}, name
            assert [c for c in actor['clips'] if c['motion'] != 'Idle'] == [c for c in original['clips'] if c['motion'] != 'Idle'], name
        else:
            assert set(clips) == {'Idle', 'Hit', 'Attack', 'Stun'}
            attack = clips['Attack']
            assert 0 < attack['contactFrame'] < len(attack['frames'])
            assert len(attack['frames']) == len(attack['frameDurationsMs'])
        data['actors'] = [a for a in data['actors'] if a['id'] != name]
        data['actors'].append(actor)
        replaced.add(name)
assert replaced == creatures | bosses
for actor in before['actors']:
    if actor['id'] not in replaced:
        assert actor == next(a for a in data['actors'] if a['id'] == actor['id']), actor['id']
manifest.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'updated': sorted(replaced), 'untouchedActors': len(before['actors'])-len(replaced)}))
