"""Package reviewed native Dragon art without changing the source image pixels."""
from pathlib import Path
import copy
import hashlib
import json
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Planning/Story/Dragon-v1/Production'
MANIFEST = ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json'
DEST = 'Assets/Game/Art/Presentation/Clips/Dragon-v1'
UNIT = 6.2 / 720
ALIASES = {
    'trail_slime': 'slime', 'grass_slime': 'slime', 'horn_rabbit': 'horn_rabbit',
    'pebble_slime': 'slime', 'great_horn_rabbit': 'great_horn_rabbit',
    'young_boar': 'boar', 'puddle_slime': 'slime', 'stone_throw_rabbit': 'horn_rabbit',
    'ridge_boar': 'boar', 'great_tusk': 'great_tusk',
}
# Pixel scale is integral at the 1280x720 reference camera; sizes stay below the HUD.
SCALES = {'slime': 10, 'horn_rabbit': 9, 'boar': 9, 'great_horn_rabbit': 10, 'great_tusk': 10, 'hero_on': 6}
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))


def player_actor(package):
    if package.get('actors'):
        return package['actors'][0]
    c = package['canonical']
    actor = dict(id='hero_on', width=c['canvas'][0], height=c['canvas'][1],
                 pivotTopLeft=c['feetPivotTopLeft'], referenceHeight=c['referenceOpaqueHeight'], facing='east', clips={})
    for item in package['clips']:
        if not item.get('frames'):
            continue
        clip = copy.deepcopy(item)
        clip['frames'] = [str((ART / 'Player' / p).relative_to(ROOT)).replace('\\', '/') for p in item['frames']]
        clip['sourceIndices'] = list(range(item['frameCount']))
        clip['contactFrame'] = item.get('contactFrame') if item.get('contactFrame') is not None else -1
        clip['effectOriginTopLeft'] = item.get('contactPoint') or c['feetPivotTopLeft']
        actor['clips'][item['runtimeBinding']] = clip
    return actor


def main():
    monsters = read(ART / 'Monsters/manifest.json')
    player = read(ART / 'Player/manifest.json')
    for package in (monsters, player):
        if 'pending' in package['status'] or 'partial' in package['status']:
            raise ValueError('Wait for the complete reviewed package: ' + package['status'])
    before = read(MANIFEST)
    old_actors = [a for a in before['actors'] if not a['id'].startswith('dragon_')]
    old_textures = [t for t in before['textures'] if not t['path'].startswith(DEST + '/')]
    actors = {a['id']: a for a in monsters['actors']}
    actors['hero_on'] = player_actor(player)
    assert {'Idle', 'Run', 'Attack', 'Hit', 'Stun', 'DodgeUp', 'DodgeBack', 'DodgeDown', 'DodgeForward'} <= set(actors['hero_on']['clips'])
    textures, templates, audit = {}, {}, []
    for actor_id, source in actors.items():
        template = dict(id=actor_id, facesRight=source['facing'] == 'east',
                        height=source['referenceHeight'] * SCALES[actor_id] * UNIT,
                        referenceHeightPixels=source['referenceHeight'], clips=[])
        for motion, clip in source['clips'].items():
            paths, hashes = [], set()
            for index, original in enumerate(clip['frames']):
                src = ROOT / original
                if not src.is_file():
                    src = ART / ('Player' if actor_id == 'hero_on' else 'Monsters') / original
                with Image.open(src) as image:
                    image = image.convert('RGBA')
                    assert set(image.getchannel('A').getdata()) <= {0, 255}, src
                    width, height = image.size
                    hashes.add(hashlib.sha256(image.tobytes()).hexdigest())
                target = f'{DEST}/{actor_id}/{motion}/{index:02}.png'
                dst = ROOT / 'Client' / target
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src, dst)
                pivot = clip.get('pivotTopLeft', clip.get('feetPivotTopLeft', source['pivotTopLeft']))
                textures[target] = dict(path=target, source=str(src.relative_to(ROOT)).replace('\\', '/'),
                                        sha256=sha(src), width=width, height=height, pivot=dict(x=pivot[0], y=pivot[1]))
                paths.append(target)
            assert len(paths) >= 6 and len(hashes) >= 3, (actor_id, motion, len(paths), len(hashes))
            contact = clip.get('contactFrame', -1)
            contact = -1 if contact is None else contact
            origin = clip.get('effectOriginTopLeft') or source.get('effectOriginTopLeft') or source['pivotTopLeft']
            result = dict(motion=motion, sourceId=f'dragon-v1-{actor_id}-{motion}',
                          sourceJob=clip.get('sourceJobId', ''), frames=paths,
                          sourceFrames=clip.get('sourceIndices', list(range(len(paths)))),
                          fps=clip['fps'], loop=clip['loop'], contactFrame=contact,
                          effectOrigin=dict(x=origin[0], y=origin[1]),
                          note='Reviewed PixelLab native source; gameplay timing remains session-owned.')
            if motion == 'Run':
                assert len(paths) == 8
                result['frameDurationsMs'] = [40, 40, 60, 60, 40, 40, 60, 60]
                result['fps'] = 20
            elif actor_id != 'hero_on' and motion == 'Attack':
                result['frameDurationsMs'] = clip.get('frameDurationsMs') or [round(1000 / clip['fps'])] * len(paths)
            face = clip.get('faceAnchorTopLeft', source.get('faceAnchorTopLeft'))
            if face:
                result['faceAnchors'] = [dict(x=face[0], y=face[1]) for _ in paths]
            template['clips'].append(result)
            audit.append(dict(template=actor_id, motion=motion, frames=len(paths), uniquePixelFrames=len(hashes), contactFrame=contact))
        templates[actor_id] = template
    additions = []
    for actor_id, template_id in {'hero_on': 'hero_on', **ALIASES}.items():
        actor = copy.deepcopy(templates[template_id])
        actor['id'] = 'dragon_' + actor_id
        additions.append(actor)
    result = copy.deepcopy(before)
    result['actors'] = old_actors + additions
    result['textures'] = old_textures + list(textures.values())
    result['scope'] = 'Dragon v1: native On + five monster templates mapped across stages 1–10. Existing legacy actors/clips preserved, including playable stages 11–20.'
    assert [a for a in result['actors'] if not a['id'].startswith('dragon_')] == old_actors
    MANIFEST.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    report = dict(actors=len(additions), nativeTemplates=len(templates), textures=len(textures), clips=audit,
                  stageTemplateReuse=ALIASES, preservedLegacyActors=len(old_actors), manifestSha256=sha(MANIFEST))
    output = ROOT / 'Planning/Production/dragon-v1-art-bindings.json'
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Packaged {len(textures)} source PNGs; {len(additions)} mappings; {len(templates)} native templates. Legacy art preserved.')


if __name__ == '__main__':
    main()
