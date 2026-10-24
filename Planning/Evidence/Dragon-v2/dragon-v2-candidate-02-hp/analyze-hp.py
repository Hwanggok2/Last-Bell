"""Offline verification of the HP-only rerun. Does not launch Unity or the player."""
from pathlib import Path
from collections import Counter
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OLD = ROOT.parent / 'dragon-v2-candidate-01'
OUT = ROOT / 'visual-review'
OUT.mkdir(exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 13)
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
summary = {'defect': 'DQA2-001', 'scope': 'HP 30 states only', 'motionRerun': False, 'playModeRerun': False, 'cases': []}
for size in ('1280x720', '640x360'):
    folder = ROOT / ('v2-hp-' + size)
    evidence = read(folder / 'v2-evidence.json')
    original = read(OLD / ('v2-' + size) / 'v2-evidence.json')
    result = read(folder / 'result.json')
    width, height = result['width'], result['height']
    scale = height / 720
    assert not evidence['segments'] and evidence['writtenFrames'] == 0
    assert not list(folder.glob('v2-frame-*.png')) and not (folder / 'frames.jsonl').exists()
    assert len(evidence['hp']) == 15 and len(list(folder.glob('v2-hp-*.png'))) == 10
    case = {'case': folder.name, 'automaticPassed': result['passed'], 'checks': len(result['checks']), 'errors': result['errors'], 'samples': []}
    sheet = Image.new('RGB', (1700, 454), '#edf0ed')
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 8), f'HP fix {size}: 0 / tiny / 25 / 50 / 100; original raster crops; 640 shown 2x nearest', fill='black', font=FONT)
    for sample in evidence['hp']:
        b = sample['bounds']
        prior = next(x for x in original['hp'] if x['role'] == sample['role'] and x['value'] == sample['value'])['bounds']
        x, y, w, h = b['x'], height-b['y']-b['height'], b['width'], b['height']
        with Image.open(folder / sample['file']) as original_image:
            bar = original_image.crop(tuple(round(v) for v in (x,y,x+w,y+h))).convert('RGB')
            context = original_image.crop(tuple(round(v) for v in (x-20*scale,y-12*scale,x+w+20*scale,y+h+54*scale)))
        fill = (36,118,109) if sample['role']=='player' else (184,66,50)
        bright = (114,185,155) if sample['role']=='player' else (241,140,103)
        palette = {(39,61,69),(89,100,99),(137,145,138),(255,241,209),fill,bright}
        counts = Counter(bar.get_flattened_data())
        mixed = {str(color): count for color, count in counts.items() if color not in palette}
        expected_bright = round(sample['expected']*(4*scale)**2)
        integer_bounds = all(abs(v-round(v)) < .0001 for v in (x,y,w,h))
        preserved_size = b['width'] == prior['width'] and b['height'] == prior['height']
        shift = {'x': b['x']-prior['x'], 'yBottom': b['y']-prior['y']}
        passed = not mixed and integer_bounds and preserved_size and max(abs(v) for v in shift.values()) <= .501 and counts[bright] == expected_bright and counts[fill] == expected_bright*4
        case['samples'].append({**sample, 'sha256': hashlib.sha256((folder/sample['file']).read_bytes()).hexdigest(), 'mixedPixels': mixed,
            'brightPixels': counts[bright], 'fillPixels': counts[fill], 'expectedBrightPixels': expected_bright, 'integerBounds': integer_bounds,
            'sizePreserved': preserved_size, 'shiftFromCandidate01': shift, 'rasterPassed': passed})
        row = ['player','enemy','boss'].index(sample['role'])
        col = 1 if 0 < sample['value'] < .001 else [0,.000001,.25,.5,1].index(sample['value'])
        if scale == .5:
            context = context.resize((context.width*2,context.height*2),Image.Resampling.NEAREST)
        px, py = col*340, 40+row*138
        sheet.paste(context,(px,py+24))
        draw.text((px+4,py),f"{sample['role']} {sample['value']:g}: {sample['expected']}/{sample['capacity']} mixed={sum(mixed.values())}",fill='black',font=FONT)
    sheet.save(OUT / f'{size}-hp-15-states.png')
    case['rasterPassed'] = all(s['rasterPassed'] for s in case['samples'])
    summary['cases'].append(case)
summary['all30RasterPassed'] = all(c['rasterPassed'] for c in summary['cases'])
(ROOT/'hp-raster-verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps([{k:c[k] for k in ('case','automaticPassed','checks','errors','rasterPassed')} for c in summary['cases']],indent=2))
