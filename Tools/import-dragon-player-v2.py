"""Import the reviewed adventurer only; preserve all enemy and legacy actor bindings."""
from pathlib import Path
import copy
import hashlib
import json
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Planning/Story/Dragon-v1/Production/Player-v2'
MANIFEST = ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json'
DEST = 'Assets/Game/Art/Presentation/Clips/Dragon-v2/hero_on'
UNIT = 6.2 / 720
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    package = read(SOURCE / 'manifest.json')
    if package['status'] not in ('ready', 'reviewed-ready-for-integration'):
        raise ValueError('The adventurer package has not finished review.')
    source = package['actors'][0]
    required = {'Idle', 'Run', 'Attack', 'DodgeUp', 'DodgeBack', 'DodgeDown', 'Hit'}
    if not required <= set(source['clips']):
        raise ValueError('Missing player motions: ' + str(required - set(source['clips'])))
    scale = source['recommendedIntegerScale']
    if int(scale) != scale or not 6 <= scale <= 10:
        raise ValueError('Use a reviewed integer display scale at 720p.')
    old = read(MANIFEST)
    actor = dict(id='dragon_hero_on', facesRight=source['facing'] == 'east',
                 height=source['referenceHeight'] * scale * UNIT,
                 referenceHeightPixels=source['referenceHeight'], clips=[])
    copies, textures, audit = [], [], []
    for motion, clip in source['clips'].items():
        paths, pixel_hashes, colors = [], set(), set()
        for index, original in enumerate(clip['frames']):
            src = ROOT / original
            if not src.is_file():
                src = SOURCE / original
            with Image.open(src) as image:
                image = image.convert('RGBA')
                if image.size != (source['width'], source['height']):
                    raise ValueError('Frame canvas differs: ' + str(src))
                pixels = list(image.getdata())
                if any(p[3] not in (0, 255) for p in pixels):
                    raise ValueError('Anti-aliased source alpha: ' + str(src))
                colors.update(p[:3] for p in pixels if p[3])
                pixel_hashes.add(hashlib.sha256(image.tobytes()).hexdigest())
            target = f'{DEST}/{motion}/{index:02}.png'
            pivot = clip.get('pivotTopLeft', source['pivotTopLeft'])
            textures.append(dict(path=target, source=src.relative_to(ROOT).as_posix(),
                                 sha256=sha(src), width=source['width'], height=source['height'],
                                 pivot=dict(x=pivot[0], y=pivot[1])))
            copies.append((src, ROOT / 'Client' / target))
            paths.append(target)
        if not paths or len(colors) > 12:
            raise ValueError(f'Empty clip or unexpected palette: {motion}, {len(colors)} colours')
        contact = clip.get('contactFrame', -1)
        contact = -1 if contact is None else contact
        if contact >= len(paths):
            raise ValueError('Invalid contact pose: ' + motion)
        if motion in ('Attack', 'DodgeUp', 'DodgeBack', 'DodgeDown') and contact < 0:
            raise ValueError('Define the reviewed impact/hold pose: ' + motion)
        origin = clip.get('effectOriginTopLeft') or source['pivotTopLeft']
        result = dict(motion=motion, sourceId=f'dragon-v2-hero_on-{motion}',
                      sourceJob=clip.get('sourceJobId', ''), frames=paths,
                      sourceFrames=clip.get('sourceIndices', list(range(len(paths)))),
                      fps=clip['fps'], loop=clip['loop'], contactFrame=contact,
                      effectOrigin=dict(x=origin[0], y=origin[1]),
                      note='Reviewed adventurer appearance with legacy motion references; session owns gameplay timing.')
        if clip.get('frameDurationsMs'):
            durations = clip['frameDurationsMs']
            if len(durations) != len(paths) or min(durations) <= 0:
                raise ValueError('Invalid frame hold durations: ' + motion)
            result['frameDurationsMs'] = durations
        if motion == 'Run' and result.get('frameDurationsMs') != [40, 40, 60, 60, 40, 40, 60, 60]:
            raise ValueError('Preserve the approved 400ms run rhythm.')
        face = clip.get('faceAnchorTopLeft', source.get('faceAnchorTopLeft'))
        if face:
            result['faceAnchors'] = [dict(x=face[0], y=face[1]) for _ in paths]
        actor['clips'].append(result)
        audit.append(dict(motion=motion, frames=len(paths), uniquePixelFrames=len(pixel_hashes),
                          colors=len(colors), contactFrame=contact))

    # Every source passes before writing any new runtime asset or changing the catalog.
    result = copy.deepcopy(old)
    if sum(a['id'] == actor['id'] for a in old['actors']) != 1:
        raise ValueError('Expected one existing Dragon player binding.')
    result['actors'] = [actor if a['id'] == actor['id'] else a for a in old['actors']]
    result['textures'] = [t for t in old['textures'] if not t['path'].startswith(DEST + '/')] + textures
    result['scope'] = 'Dragon first-ten-stage theme with Player v2 adventurer. Enemy and legacy bindings unchanged.'
    for src, dst in copies:
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)
    MANIFEST.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    report = dict(package=SOURCE.relative_to(ROOT).as_posix(), actor=actor['id'],
                  nativeBodyHeight=source['referenceHeight'], integerScale720p=scale,
                  screenBodyHeight720p=source['referenceHeight'] * scale, clips=audit,
                  preservedOtherActors=len(old['actors']) - 1, manifestSha256=sha(MANIFEST),
                  sourceManifestSha256=sha(SOURCE / 'manifest.json'))
    (ROOT / 'Planning/Production/dragon-v2-art-bindings.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Packaged {len(textures)} adventurer frames; all other actor bindings preserved.')


if __name__ == '__main__':
    main()
