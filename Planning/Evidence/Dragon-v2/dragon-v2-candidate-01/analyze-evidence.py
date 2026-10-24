"""Read this candidate's existing capture files; never launch Unity or the game."""
from pathlib import Path
import hashlib
import json
import math
from collections import defaultdict
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'visual-review'
OUT.mkdir(exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 13)
summary = {'videoPlaybackPerformed': False, 'reviewMethod': 'Original PNGs, ordered rendered-pose contact sheets, and recorded timestamps. No interpolated frames.', 'cases': []}

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def save_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

for case in ('v2-1280x720', 'v2-640x360'):
    folder = ROOT / case
    records = [json.loads(line) for line in (folder / 'frames.jsonl').read_text(encoding='utf-8-sig').splitlines()]
    evidence = read(folder / 'v2-evidence.json')
    result = read(folder / 'result.json')
    width, height = result['width'], result['height']
    scale = height / 720
    groups = defaultdict(list)
    for record in records:
        assert (folder / record['file']).is_file(), record['file']
        assert hashlib.sha256((folder / record['file']).read_bytes()).hexdigest() == record['sha256'], record['file']
        groups[record['scenario']].append(record)
    assert len({x['file'] for x in records}) == len(records)
    assert len(list(folder.glob('v2-frame-*.png'))) == len(records) == evidence['writtenFrames']
    info = {'case': case, 'automaticPassed': result['passed'], 'errors': result['errors'], 'indexedPngs': len(records), 'allHashesMatch': True, 'segments': [], 'hpRaster': []}
    for scenario, frames in groups.items():
        assert all(b['frameCount'] == a['frameCount'] + 1 for a, b in zip(frames, frames[1:])), scenario
        gaps = [(b['realtime'] - a['realtime']) * 1000 for a, b in zip(frames, frames[1:])]
        observed = defaultdict(set)
        selected, seen = [], set()
        for frame in frames:
            observed[frame['motion']].add(frame['clipFrame'])
            key = (frame['motion'], frame['clipFrame'])
            if key not in seen:
                selected.append(frame)
                seen.add(key)
        # Preserve sequence start/end and transition boundaries, in addition to every distinct rendered pose.
        selected.extend([frames[0], frames[-1]])
        for left, right in zip(frames, frames[1:]):
            if left['motion'] != right['motion']:
                selected.extend([left, right])
        selected = sorted({x['file']: x for x in selected}.values(), key=lambda x: x['frameCount'])
        tile_w, tile_h, columns = 280, 298, 6
        sheet = Image.new('RGB', (columns * tile_w, 40 + math.ceil(len(selected)/columns) * tile_h), '#edf0ed')
        draw = ImageDraw.Draw(sheet)
        draw.text((8, 8), f'{case} / {scenario} / ordered distinct rendered poses + boundaries; 640 crops shown 2x nearest', fill='black', font=FONT)
        for i, frame in enumerate(selected):
            sx = width / 2 + frame['root']['x'] * height / 6.2
            sy = height / 2 - (frame['root']['y'] + 1.04) * height / 6.2
            box = tuple(round(x) for x in (sx-130*scale, sy-226*scale, sx+142*scale, sy+26*scale))
            with Image.open(folder / frame['file']) as original:
                crop = original.crop(box)
            if scale == .5:
                crop = crop.resize((crop.width*2, crop.height*2), Image.Resampling.NEAREST)
            x, y = (i % columns)*tile_w, 40+(i // columns)*tile_h
            sheet.paste(crop, (x, y+36))
            draw.text((x+3, y), f"f{frame['frameCount']} {frame['motion']}:{frame['clipFrame']}", fill='black', font=FONT)
            draw.text((x+3, y+16), f"t={frame['realtime']:.3f} core={frame['combatMs']:.1f}ms", fill='black', font=FONT)
        sheet_name = f'{case}-{scenario}.png'
        sheet.save(OUT / sheet_name)
        save_json(OUT / f'{case}-{scenario}-selection.json', selected)
        info['segments'].append({'scenario': scenario, 'frames': len(frames), 'renderFrameGaps': 0,
            'duration': frames[-1]['realtime']-frames[0]['realtime'], 'meanCaptureRate': (len(frames)-1)/(frames[-1]['realtime']-frames[0]['realtime']),
            'maxGapMs': max(gaps), 'p95GapMs': sorted(gaps)[int((len(gaps)-1)*.95)],
            'observedClipFrames': {key: sorted(value) for key, value in observed.items()}, 'sheet': sheet_name})
    # Compare full raster pixels independently of the probe's Canvas mesh count.
    hp_sheet = Image.new('RGB', (5*340, 40+3*138), '#edf0ed')
    draw = ImageDraw.Draw(hp_sheet)
    draw.text((8, 8), f'{case} HP: 0 / tiny / 25 / 50 / 100; original raster crops, 640 shown 2x nearest', fill='black', font=FONT)
    for sample in evidence['hp']:
        row = ['player', 'enemy', 'boss'].index(sample['role'])
        col = 1 if 0 < sample['value'] < .001 else [0, .000001, .25, .5, 1].index(sample['value'])
        bounds = sample['bounds']
        x, y, w, h = bounds['x'], height-bounds['y']-bounds['height'], bounds['width'], bounds['height']
        with Image.open(folder / sample['file']) as original:
            bar = original.crop(tuple(round(v) for v in (x, y, x+w, y+h))).convert('RGB')
            crop = original.crop(tuple(round(v) for v in (x-20*scale, y-12*scale, x+w+20*scale, y+h+54*scale)))
        bright = (114,185,155) if sample['role']=='player' else (241,140,103)
        fill = (36,118,109) if sample['role']=='player' else (184,66,50)
        colors = list(bar.get_flattened_data())
        counts = {'bright': colors.count(bright), 'fill': colors.count(fill)}
        expected_bright = round(sample['expected']*(4*scale)**2)
        pixel_ok = counts['bright'] == expected_bright and counts['fill'] == expected_bright*4
        bright_columns = len({px for py in range(bar.height) for px in range(bar.width) if bar.getpixel((px, py)) == bright})
        info['hpRaster'].append({**sample, 'pixels': counts, 'expectedBrightPixels': expected_bright, 'rasterMatches': pixel_ok,
            'brightPixelColumns': bright_columns, 'filledColumnsMatch': bright_columns == round(sample['expected']*4*scale)})
        if scale == .5:
            crop = crop.resize((crop.width*2, crop.height*2), Image.Resampling.NEAREST)
        px, py = col*340, 40+row*138
        hp_sheet.paste(crop, (px, py+24))
        draw.text((px+4, py), f"{sample['role']} {sample['value']:g} -> {sample['expected']}/{sample['capacity']}", fill='black', font=FONT)
    hp_sheet.save(OUT / f'{case}-hp-15-states.png')
    info['hpRasterAllMatch'] = all(x['rasterMatches'] for x in info['hpRaster'])
    info['hpFilledColumnsAllMatch'] = all(x['filledColumnsMatch'] for x in info['hpRaster'])
    summary['cases'].append(info)
save_json(ROOT / 'evidence-analysis.json', summary)
print(json.dumps([{k: case[k] for k in ('case','automaticPassed','indexedPngs','allHashesMatch','hpRasterAllMatch')} for case in summary['cases']], indent=2))
