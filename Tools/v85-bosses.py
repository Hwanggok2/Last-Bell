"""Download/review/package only the two existing bosses' new Idle candidates.

Does not write Client assets or the shared art manifest. Use the Pillow runtime.
"""
from pathlib import Path
from copy import deepcopy
import argparse
import concurrent.futures
import hashlib
import json
import urllib.request

from PIL import Image, ImageDraw, ImageChops

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Planning/Story/Monster-v85/bosses'
MANIFEST = ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json'
FONT = ROOT / 'Client/Assets/Game/Art/Fonts/NeoDunggeunmoPro-Regular.ttf'
CONFIG = {
    'bell_knight': {
        'actorId': 'bell_knight_concept', 'stage': 10, 'name': '종의 기사',
        'jobId': '165250f1-63fa-4141-be11-1e6be6134d3a', 'seed': 8510,
        'description': 'A very restrained seamless breathing idle loop. Both feet remain planted at the exact same pixels throughout. The rigid armoured chest lifts only one pixel and settles, the lowered heavy polearm and shield drift at most one pixel with the breathing, and only the lowest cloth hem gently shifts one or two pixels. Keep the same ready stance, facing, perspective, proportions, silhouette, gear, pixel clusters and limited colours as the first frame. No step, no leap, no attack, no turning, no extra limb, no glow, no particles, no camera movement. Finish exactly in the original pose.'},
    'chain_jailer': {
        'actorId': 'chain_jailer', 'stage': 20, 'name': '매달린 집행관',
        'jobId': '066b3666-0330-4b69-91dc-e29bb19189af', 'seed': 8520,
        'description': "A restrained seamless waiting idle loop for a chain-bound hanging guardian. Keep the character's lowest foot and robe hem anchor at the exact same location; no horizontal travel. The torso rises and settles only one pixel, hanging chain tips and cloth hems make a tiny one-to-two-pixel weight shift. Retain every chain, arm, weapon and masked face in its original place and appearance. Preserve the first frame's facing, perspective, proportions, angular pixel clusters and limited colours. No walking, bouncing, attack, rotation, added glow or particles. End exactly at the starting pose."},
}

def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8-sig'))
    actors = []
    inputs = []
    jobs = []
    for key, settings in CONFIG.items():
        actor = next(a for a in manifest['actors'] if a['id'] == settings['actorId'])
        idle = next(c for c in actor['clips'] if c['motion'] == 'Idle')
        assert len(idle['frames']) == 1, 'Do not snapshot already-replaced Idle'
        texture = next(t for t in manifest['textures'] if t['path'] == idle['frames'][0])
        path = ROOT / 'Client' / texture['path']
        assert sha(path) == texture['sha256']
        actors.append(deepcopy(actor))
        inputs.append(deepcopy(texture))
        jobs.append({**settings, 'id': key, 'provider': 'PixelLab animate_image_pixminimax',
                     'requestedFrames': 8, 'enhancePrompt': False, 'direction': 'west',
                     'transparent': True, 'firstFrame': str(path.relative_to(ROOT)).replace('\\', '/'),
                     'lastFrame': str(path.relative_to(ROOT)).replace('\\', '/'),
                     'firstAndLastSha256': sha(path), 'requestedCostGenerations': 1,
                     'status': 'submitted', 'initialPose': 'The exact grounded ready pose from the provided sprite. Feet and lower robe anchor remain fixed.'})
    write('original-manifest-fragment.json', {'actors': actors, 'textures': inputs})
    write('jobs.json', {'date': '2026-10-23', 'purpose': 'Idle only; preserve all original body art, Attack, Stun, facing, pivot, world and canonical heights.', 'jobs': jobs,
                        'license': 'Generated through PixelLab from existing project-owned input sprites; not represented as CC0 or a new third-party download.',
                        'serviceTerms': 'https://www.pixellab.ai/termsofservice',
                        'postprocessing': 'No sprite artwork retouching, rescaling, palette replacement, or pivot change. Contact sheets and GIFs are QA previews only.'})

def download(count, only=None):
    tasks = []
    for key, setting in CONFIG.items():
        if only and key != only:
            continue
        for index in range(count):
            tasks.append((key, index, setting['jobId']))
    def fetch(task):
        key, index, job = task
        path = OUT / key / 'raw' / f'{index:02}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        if path.exists():
            return
        url = f'https://api.pixellab.ai/mcp/images/{job}/download?index={index}'
        raw = urllib.request.urlopen(url, timeout=45).read()
        assert raw.startswith(b'\x89PNG\r\n\x1a\n'), url
        path.write_bytes(raw)
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        list(pool.map(fetch, tasks))
    print(f'Downloaded {len(tasks)} native PNG frames')

def metrics(image, reference):
    pixels = list(image.getdata())
    alpha = image.getchannel('A')
    solid = alpha.point(lambda x: 255 if x >= 128 else 0)
    original_solid = reference.getchannel('A').point(lambda x: 255 if x >= 128 else 0)
    xor = ImageChops.difference(solid, original_solid)
    union = ImageChops.lighter(solid, original_solid)
    changed_mask = sum(p > 0 for p in xor.getdata())
    union_count = sum(p > 0 for p in union.getdata())
    changes = [a != b for a, b in zip(pixels, reference.getdata())]
    bbox = image.getbbox()
    return {'size': list(image.size), 'bbox': bbox, 'solidBbox': solid.getbbox(),
            'opaqueColors': len({p[:3] for p in pixels if p[3]}),
            'alphaValueCount': len({p[3] for p in pixels}),
            'changedPixelsFromBase': sum(changes),
            'silhouetteDifferenceRatio': changed_mask / max(1, union_count),
            'canvasClipping': bbox[0] == 0 or bbox[1] == 0 or bbox[2] == image.width or bbox[3] == image.height}

def review(only=None):
    original = json.loads((OUT / 'original-manifest-fragment.json').read_text(encoding='utf-8'))
    measured = {}
    for key, setting in CONFIG.items():
        if only and key != only:
            continue
        tex = next(t for t in original['textures'] if t['path'].endswith(f'/{key}.png'))
        base = Image.open(ROOT / 'Client' / tex['path']).convert('RGBA')
        paths = sorted((OUT / key / 'raw').glob('*.png'))
        assert paths, key
        frames = [Image.open(path).convert('RGBA') for path in paths]
        assert all(im.size == base.size for im in frames)
        measured[key] = {'input': metrics(base, base), 'pivot': tex['pivot'], 'frames': []}
        panels = [('source', base)] + [(path.stem, frame) for path, frame in zip(paths, frames)]
        columns = 5
        rows = (len(panels) + columns - 1) // columns
        sheet = Image.new('RGB', (columns * 266, rows * 288), '#172733')
        draw = ImageDraw.Draw(sheet)
        for i, (label, frame) in enumerate(panels):
            x, y = i % columns * 266, i // columns * 288
            draw.text((x + 6, y + 6), f'{key} {label}', fill='#d9e4dd')
            zoom = frame.resize((256, 256), Image.Resampling.NEAREST)
            sheet.paste(zoom, (x + 5, y + 25), zoom)
            # Fixed guide is a review overlay, never burned into source sprites.
            draw.line((x + 5, y + 25 + 114 * 2, x + 260, y + 25 + 114 * 2), fill='#aa7258')
        sheet.save(OUT / f'{key}-contactsheet.png')
        gif_frames = []
        for path, frame in zip(paths, frames):
            measured[key]['frames'].append({'index': int(path.stem), 'sha256': sha(path), **metrics(frame, base)})
            canvas = Image.new('RGB', (512, 512), '#172733')
            zoom = frame.resize((512, 512), Image.Resampling.NEAREST)
            canvas.paste(zoom, (0, 0), zoom)
            gif_frames.append(canvas)
        gif_frames[0].save(OUT / f'{key}-idle.gif', save_all=True, append_images=gif_frames[1:], duration=[200] * len(gif_frames), loop=0)
    metrics_path = OUT / 'frame-metrics.json'
    prior = json.loads(metrics_path.read_text(encoding='utf-8')) if metrics_path.exists() else {}
    prior.update(measured)
    write('frame-metrics.json', prior)
    print('Wrote native-frame metrics, contact sheets, 200ms/frame GIF previews')

def package():
    decisions = json.loads((OUT / 'review-decisions.json').read_text(encoding='utf-8'))
    snapshot = json.loads((OUT / 'original-manifest-fragment.json').read_text(encoding='utf-8'))
    output = {'version': 'v85-boss-idle', 'status': 'reviewed-source-fragment-not-yet-Unity-imported',
              'ownership': 'Root integration copies native textures then replaces these actors. Only Idle differs from the original snapshot.',
              'textures': [], 'actors': [], 'qa': 'review-decisions.json', 'provenance': 'jobs.json'}
    for key, setting in CONFIG.items():
        decision = decisions[key]
        if decision['verdict'] != 'accept':
            continue
        actor = deepcopy(next(a for a in snapshot['actors'] if a['id'] == setting['actorId']))
        old_actor = deepcopy(actor)
        old_texture = next(t for t in snapshot['textures'] if t['path'].endswith(f'/{key}.png'))
        idle = next(c for c in actor['clips'] if c['motion'] == 'Idle')
        targets = []
        for index in decision['sourceFrames']:
            path = OUT / key / 'raw' / f'{index:02}.png'
            image = Image.open(path)
            assert image.size == (old_texture['width'], old_texture['height'])
            target = f'Assets/Game/Art/Presentation/Clips/monster-v85/bosses/{key}/idle/{index:02}.png'
            pivot = decision.get('pivots', {}).get(str(index), old_texture['pivot'])
            output['textures'].append({'path': target, 'source': path.relative_to(ROOT).as_posix(), 'sha256': sha(path),
                                       'width': image.width, 'height': image.height, 'pivot': deepcopy(pivot)})
            targets.append(target)
        idle.update({'frames': targets, 'sourceFrames': decision['sourceFrames'], 'frameDurationsMs': decision['holdsMs'],
                     'fps': 1000 * len(targets) / sum(decision['holdsMs']), 'loop': True, 'contactFrame': -1,
                     'staticFallback': False, 'sourceId': f'v85-{key}-idle', 'sourceJob': setting['jobId'],
                     'note': 'Reviewed native PixelLab idle only. Original Attack/Stun, facing, actor height 4 and canonical height 92 remain unchanged. Baseline pivot (78,114); chain_jailer frames4-7 use pivot.x79 to cancel one-pixel global horizontal drift without editing PNGs. Small vertical float is intentional for the hanging boss.'})
        assert [c for c in actor['clips'] if c['motion'] != 'Idle'] == [c for c in old_actor['clips'] if c['motion'] != 'Idle']
        assert {k: v for k, v in actor.items() if k != 'clips'} == {k: v for k, v in old_actor.items() if k != 'clips'}
        output['actors'].append(actor)
    write('manifest-fragment.json', output)
    print(f'Packaged {len(output["actors"])} accepted Idle-only replacements; no Client writes')

def final_review():
    decisions = json.loads((OUT / 'review-decisions.json').read_text(encoding='utf-8'))
    snapshot = json.loads((OUT / 'original-manifest-fragment.json').read_text(encoding='utf-8'))
    report = {}
    for key, setting in CONFIG.items():
        decision = decisions[key]
        if decision['verdict'] != 'accept':
            continue
        texture = next(t for t in snapshot['textures'] if t['path'].endswith(f'/{key}.png'))
        original = Image.open(ROOT / 'Client' / texture['path']).convert('RGBA')
        rendered = []
        details = []
        for index in decision['sourceFrames']:
            path = OUT / key / 'raw' / f'{index:02}.png'
            raw = Image.open(path).convert('RGBA')
            pivot = decision.get('pivots', {}).get(str(index), texture['pivot'])
            offset = (texture['pivot']['x'] - pivot['x'], texture['pivot']['y'] - pivot['y'])
            registered = Image.new('RGBA', raw.size)
            registered.paste(raw, offset)
            rendered.append(registered)
            details.append({'sourceFrame': index, 'pivot': pivot, 'previewDrawOffset': list(offset),
                            'rawPngUnmodifiedSha256': sha(path), 'registeredBbox': registered.getbbox()})
        sheet = Image.new('RGB', (5 * 266, 2 * 288), '#172733')
        draw = ImageDraw.Draw(sheet)
        for i, (label, frame) in enumerate([('original', original)] + [(f'{n:02}', frame) for n, frame in zip(decision['sourceFrames'], rendered)]):
            x, y = i % 5 * 266, i // 5 * 288
            draw.text((x + 6, y + 6), f'{key} {label}', fill='#d9e4dd')
            zoom = frame.resize((256, 256), Image.Resampling.NEAREST)
            sheet.paste(zoom, (x + 5, y + 25), zoom)
            draw.line((x + 5, y + 25 + 114 * 2, x + 260, y + 25 + 114 * 2), fill='#aa7258')
            draw.line((x + 5 + 78 * 2, y + 25 + 106 * 2, x + 5 + 78 * 2, y + 25 + 120 * 2), fill='#708e91')
        sheet.save(OUT / f'{key}-contactsheet.png')
        gif_frames = []
        for frame in rendered:
            canvas = Image.new('RGB', (512, 512), '#172733')
            zoom = frame.resize((512, 512), Image.Resampling.NEAREST)
            canvas.paste(zoom, (0, 0), zoom)
            gif_frames.append(canvas)
        gif_frames[0].save(OUT / f'{key}-idle.gif', save_all=True, append_images=gif_frames[1:], duration=decision['holdsMs'], loop=0)
        last_mask = rendered[-1].getchannel('A').point(lambda a: 255 if a >= 128 else 0)
        first_mask = rendered[0].getchannel('A').point(lambda a: 255 if a >= 128 else 0)
        diff = ImageChops.difference(last_mask, first_mask)
        report[key] = {'frames': details, 'durationMs': sum(decision['holdsMs']),
                       'loopBoundaryMaskChangedPixels': sum(p > 0 for p in diff.getdata()),
                       'horizontalDriftRemovedByMetadata': key == 'chain_jailer',
                       'groundContactFixed': key == 'bell_knight',
                       'intentionalVerticalFloatPixels': 3 if key == 'chain_jailer' else 0,
                       'sourcePngsResampledOrRepainted': False}
    write('final-review-metrics.json', report)
    print('Wrote selected 1.6s GIFs and contact sheets with the actual per-frame pivots applied')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'download', 'review', 'package', 'final-review'])
    parser.add_argument('--count', type=int, default=9)
    parser.add_argument('--only', choices=list(CONFIG))
    args = parser.parse_args()
    if args.action == 'prepare':
        prepare()
    elif args.action == 'download':
        download(args.count, args.only)
    elif args.action == 'review':
        review(args.only)
    elif args.action == 'package':
        package()
    else:
        final_review()
