"""Validate both reviewed packages before importing Player-v3 and ten forest actors."""
from pathlib import Path
import copy
import hashlib
import json
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'Planning/Story/Dragon-v1/Production'
MANIFEST = ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json'
DEST = 'Assets/Game/Art/Presentation/Clips/Dragon-v3'
FOREST = {'goblin_scout', 'goblin_slinger', 'forest_mushroom', 'goblin_shield',
          'mushroom_captain', 'goblin_spear', 'spore_mushroom', 'moss_mushroom', 'goblin_guard', 'gib'}
UNIT = 6.2 / 720
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))


def main():
    old = read(MANIFEST)
    additions, textures, copies, audit, packages = [], [], [], [], []
    for folder in ('Player-v3', 'Forest-v1'):
        package_path = BASE / folder / 'manifest.json'
        package = read(package_path)
        if package['status'] not in ('ready', 'reviewed-ready-for-integration'):
            raise ValueError('Wait for reviewed source: ' + folder)
        ids = {a['id'] for a in package['actors']}
        assert ids == ({'hero_on'} if folder == 'Player-v3' else FOREST), (folder, ids)
        packages.append(dict(path=package_path.relative_to(ROOT).as_posix(), sha256=sha(package_path)))
        for source in package['actors']:
            is_player = source['id'] == 'hero_on'
            required = {'Idle', 'Run', 'Attack', 'DodgeUp', 'DodgeBack', 'DodgeDown', 'Hit'} if is_player else {'Idle', 'Attack', 'Stun'}
            assert required <= set(source['clips']), source['id']
            scale = source['recommendedIntegerScale']
            assert int(scale) == scale and 4 <= scale <= 12
            actor = dict(id='dragon_' + source['id'], facesRight=source['facing'] == 'east',
                         height=source['referenceHeight'] * scale * UNIT,
                         referenceHeightPixels=source['referenceHeight'], clips=[])
            actor_audit = dict(id=actor['id'], referenceHeight=source['referenceHeight'],
                               scale720p=scale, height720p=source['referenceHeight'] * scale, clips=[])
            for motion, clip in source['clips'].items():
                paths, hashes, colors = [], set(), set()
                pivot = clip.get('pivotTopLeft') or clip.get('feetPivotTopLeft') or source['pivotTopLeft']
                for index, original in enumerate(clip['frames']):
                    src = ROOT / original
                    if not src.is_file():
                        src = BASE / folder / original
                    with Image.open(src) as im:
                        rgba = im.convert('RGBA')
                        assert rgba.size == (source['width'], source['height']), str(src)
                        pixels = list(rgba.getdata())
                        assert all(p[3] in (0, 255) for p in pixels), str(src)
                        colors.update(p[:3] for p in pixels if p[3])
                        hashes.add(hashlib.sha256(rgba.tobytes()).hexdigest())
                    target = f'{DEST}/{source["id"]}/{motion}/{index:02}.png'
                    copies.append((src, ROOT / 'Client' / target))
                    paths.append(target)
                    textures.append(dict(path=target, source=src.relative_to(ROOT).as_posix(), sha256=sha(src),
                                         width=source['width'], height=source['height'], pivot=dict(x=pivot[0], y=pivot[1])))
                assert len(paths) >= 6 and len(hashes) >= 3, (actor['id'], motion, len(hashes))
                assert len(colors) <= 16, (actor['id'], motion, len(colors))
                contact = clip.get('contactFrame')
                if contact is None:
                    contact = -1
                assert -1 <= contact < len(paths)
                if motion in ('Attack', 'DodgeUp', 'DodgeBack', 'DodgeDown'):
                    assert contact >= 0
                origin = clip.get('effectOriginTopLeft') or source.get('effectOriginTopLeft') or pivot
                result = dict(motion=motion, sourceId=f'dragon-v3-{source["id"]}-{motion}',
                              sourceJob=clip.get('sourceJobId', ''), frames=paths,
                              sourceFrames=clip.get('sourceIndices', list(range(len(paths)))),
                              fps=clip['fps'], loop=clip['loop'], contactFrame=contact,
                              effectOrigin=dict(x=origin[0], y=origin[1]),
                              note='Reviewed native source; common clip pivot. Session owns gameplay clocks.')
                if clip.get('frameDurationsMs'):
                    holds = clip['frameDurationsMs']
                    assert len(holds) == len(paths) and min(holds) > 0
                    result['frameDurationsMs'] = holds
                if is_player and motion == 'Run':
                    assert result.get('frameDurationsMs') == [40, 40, 60, 60, 40, 40, 60, 60]
                face = clip.get('faceAnchorTopLeft') or source.get('faceAnchorTopLeft')
                faces = clip.get('faceAnchorsTopLeft') or ([face] * len(paths) if face else [])
                assert faces or is_player, (actor['id'], 'face anchor required')
                if faces:
                    assert len(faces) == len(paths), (actor['id'], motion, 'face anchor count')
                    assert all(0 <= x <= source['width'] and 0 <= y <= source['height'] for x, y in faces)
                    result['faceAnchors'] = [dict(x=x, y=y) for x, y in faces]
                actor['clips'].append(result)
                actor_audit['clips'].append(dict(motion=motion, frames=len(paths), uniquePixelFrames=len(hashes),
                                                colors=len(colors), pivotTopLeft=pivot, contactFrame=contact))
            additions.append(actor)
            audit.append(actor_audit)
    ids = {a['id'] for a in additions}
    result = copy.deepcopy(old)
    result['actors'] = [a for a in old['actors'] if a['id'] not in ids] + additions
    result['textures'] = [t for t in old['textures'] if not t['path'].startswith(DEST + '/')] + textures
    result['scope'] = 'Dragon v3: Player-v3 adventurer, forest actors11–20; existing meadow and all other legacy actors preserved.'
    preserved = [a for a in old['actors'] if a['id'] not in ids]
    assert preserved == [a for a in result['actors'] if a['id'] not in ids]
    for src, dst in copies:
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)
    MANIFEST.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    report = dict(packages=packages, actors=audit, textureReferences=len(textures),
                  preservedOtherActors=len(preserved), manifestSha256=sha(MANIFEST))
    (ROOT / 'Planning/Production/dragon-v3-art-bindings.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Imported {len(additions)} reviewed actors and {len(textures)} frame references; preserved {len(preserved)} other actors.')


if __name__ == '__main__':
    main()
