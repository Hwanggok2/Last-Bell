"""Read native evidence; write review summaries and labeled capture contact sheets only."""
import collections
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
CANDIDATE = HERE.parent
ROOT = CANDIDATE.parents[3]
FONT = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 15)
manifest = json.loads((ROOT / 'Client/Assets/Game/Art/Presentation/manifest.json').read_text(encoding='utf-8-sig'))
actors = {a['id']: a for a in manifest['actors']}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def extrema(items, key):
    item = max(items, key=key)
    return {'value': key(item), 'file': item['file'], 'scenario': item['scenario']}


def sample_changes(frames, who, motion=None, maximum=10):
    selected, seen = [], set()
    for f in frames:
        a = f[who]
        if motion is not None and a['motion'] != motion:
            continue
        key = (a['art'], a['motion'], a['clipFrame'])
        if key not in seen:
            selected.append(f)
            seen.add(key)
    if len(selected) > maximum:
        selected = [selected[round(i*(len(selected)-1)/(maximum-1))] for i in range(maximum)]
    return selected


def sheet(folder, name, rows, width, height):
    # Crops preserve native pixels (nearest-neighbor). These are review aids,
    # not replacements for the original full-resolution evidence.
    cw, ch, cols = 270, 220, 10
    canvas = Image.new('RGB', (cw*cols, ch*len(rows)), '#f6f1df')
    draw = ImageDraw.Draw(canvas)
    entries = []
    for row, (label, frames, who, wide) in enumerate(rows):
        for col, f in enumerate(frames[:cols]):
            with Image.open(folder / f['file']) as image:
                scale = width / 1280
                if wide:
                    box = (int(260*scale), int(45*scale), int(1160*scale), int(510*scale))
                else:
                    a = f[who]
                    art = actors[a['art']]
                    body_h = art['height']*height/(2*f['cameraSize'])
                    half = max(135*scale, body_h*.68)
                    x = a['feetScreen']['x']
                    y = height-a['feetScreen']['y']
                    box = tuple(map(round, (x-half, y-body_h-35*scale, x+half, y+30*scale)))
                crop = image.crop(box)
                ratio = min(cw/crop.width, (ch-43)/crop.height)
                crop = crop.resize((round(crop.width*ratio), round(crop.height*ratio)), Image.Resampling.NEAREST)
                canvas.paste(crop, (col*cw+(cw-crop.width)//2, row*ch+42))
            a = f[who]
            text = f"{label} | {a['motion']}:{a['clipFrame']} src:{a['sourceFrame']}"
            draw.text((col*cw+5, row*ch+2), text, fill='#162c32', font=FONT)
            draw.text((col*cw+5, row*ch+21), f"frame {f['frameCount']} | {f['combatMs']:.1f} ms", fill='#162c32', font=FONT)
            entries.append({'row': row, 'column': col, 'label': text, 'file': f['file'], 'crop': box})
    path = HERE / f'{width}x{height}-{name}.png'
    canvas.save(path)
    (path.with_suffix('.json')).write_text(json.dumps(entries, indent=2), encoding='utf-8')
    return str(path.relative_to(CANDIDATE))


summaries = []
for width, height in [(1280,720), (640,360)]:
    folder = CANDIDATE / f'{width}x{height}' / 'fresh'
    if not (folder / 'result.json').exists():
        continue
    result, evidence = read(folder/'result.json'), read(folder/'v3-evidence.json')
    frames = [json.loads(line) for line in (folder/'frames.jsonl').read_text().splitlines()]
    if not frames:
        summaries.append({'resolution':f'{width}x{height}','passed':result['passed'],'completed':result['completed'],
                          'coverage':evidence.get('coverage'),'passChecks':sum(x.startswith('PASS ') for x in result['checks']),
                          'failChecks':sum(x.startswith('FAIL ') for x in result['checks']),'consoleErrors':result['errors'],
                          'writtenFrames':evidence['writtenFrames'],'indexedFrames':0,'storyRestoreUi':evidence.get('storyRestoreUi',[]),
                          'limits':'Targeted story/save tail only. No motion observations in this process.'})
        continue
    groups = collections.defaultdict(list)
    for f in frames:
        groups[f['scenario']].append(f)
    def first_episode(label):
        segment = next((s for s in evidence['segments'] if s['name']==label), None)
        if not segment:
            return []
        return [f for f in groups[label] if segment['firstRealtime'] <= f['realtime'] <= segment['lastRealtime']]
    hashes = []
    for f in frames:
        if hashlib.sha256((folder/f['file']).read_bytes()).hexdigest() != f['sha256']:
            hashes.append(f['file'])
    grounded = [f for f in frames if f['player']['motion'] != 'DodgeUp']
    errors = {}
    for who, relevant in [('player', grounded), ('enemy', frames)]:
        errors[who+'GroundedPivotPx'] = extrema(relevant, lambda f: abs(f[who]['feetScreen']['y']-f[who]['groundScreen']['y']))
        errors[who+'RootWorldUnits'] = extrema(relevant, lambda f: abs(f[who]['rootHeightAboveGround']))
    up = [f for f in frames if f['player']['motion']=='DodgeUp']
    errors['intentionalUpLiftPx'] = extrema(up, lambda f: f['player']['feetScreen']['y']-f['player']['groundScreen']['y']) if up else None
    segments = []
    for label, items in groups.items():
        motions = {}
        for who in ['player','enemy']:
            coverage = collections.defaultdict(set)
            for f in items:
                a = f[who]
                coverage[a['art']+'/'+a['motion']+'/'+a['clipMotion']].add(a['clipFrame'])
            motions[who] = {key: sorted(value) for key,value in coverage.items()}
        segments.append({'name':label,'frames':len(items),'duration':items[-1]['realtime']-items[0]['realtime'],
                         'motions':motions,'first':items[0]['file'],'last':items[-1]['file']})
    sheets = []
    if width == 640:
        for stage in range(11,21):
            idle = groups.get(f'enemy-{stage}-idle', [])
            attack = first_episode(f'enemy-{stage}-attack-hit')
            stun = groups.get(f'enemy-{stage}-stun', [])
            if not idle or not attack or not stun:
                continue
            rows = [('Idle',sample_changes(idle,'enemy','Idle'),'enemy',False),
                    ('Attack / Hit',sample_changes(attack,'enemy'),'enemy',True),
                    ('Stun / recover',sample_changes(stun,'enemy'),'enemy',False)]
            impacts = [i for i in range(1,len(attack)) if attack[i]['playerHp'] < attack[i-1]['playerHp']]
            if impacts:
                hit = impacts[0]
                rows.append(('Impact frames',attack[max(0,hit-3):hit+7],'enemy',True))
            sheets.append(sheet(folder,f'enemy-{stage}',rows,width,height))
        for stage in [1,11]:
            rows = []
            for suffix in ['idle','attack','defense-0','defense-1','defense-2']:
                items = groups.get(f'player-{stage}-{suffix}', [])
                if items:
                    rows.append((suffix,sample_changes(items,'player'),'player',suffix=='attack'))
            if rows:
                sheets.append(sheet(folder,f'player-{stage}',rows,width,height))
        for stage in [15,20]:
            items = groups.get(f'boss-{stage}-intro', [])
            if items:
                chosen = [items[round(i*(len(items)-1)/9)] for i in range(10)]
                sheets.append(sheet(folder,f'boss-{stage}',[('Intro',chosen,'enemy',True)],width,height))
        for stage in [1,10,11,14,15]:
            items = groups.get(f'travel-{stage}-to-{stage+1}', [])
            if items:
                rows=[('Run',sample_changes(items,'player','Run'),'player',False),
                      ('Boundary',[items[round(i*(len(items)-1)/9)] for i in range(10)],'player',True)]
                sheets.append(sheet(folder,f'travel-{stage}',rows,width,height))
    summaries.append({'resolution':f'{width}x{height}','passed':result['passed'],'completed':result['completed'],
                      'passChecks':sum(x.startswith('PASS ') for x in result['checks']),
                      'failChecks':sum(x.startswith('FAIL ') for x in result['checks']),
                      'consoleErrors':result['errors'],'writtenFrames':evidence['writtenFrames'],'indexedFrames':len(frames),
                      'segmentCount':len(evidence['segments']),'uniqueScenarioCount':len(groups),'missingRenderedFrames':sum(x['missingRenderedFrames'] for x in evidence['segments']),
                      'maxRealtimeGap':max(x['maxRealtimeGap'] for x in evidence['segments']),
                      'hashMismatches':hashes,'duplicateFiles':len(frames)-len({f['file'] for f in frames}),
                      'coordinateMetrics':errors,'scenarios':segments,'sheets':sheets,
                      'visualReview':'PENDING_DIRECT_REVIEW','normalSpeedPlayback':'NOT_RUN',
                      'limits':'Pivot projection is distinct from the lowest opaque image pixel; intentional Up lift is excluded from grounded residual. Captures include rendering/readback overhead and do not certify 60fps.'})

output = {'candidate':CANDIDATE.name,'cases':summaries,
          'scope':'Read-only analysis of existing native evidence; no Unity/player or OS input.'}
if (CANDIDATE/'campaign.json').exists():
    output['campaign'] = read(CANDIDATE/'campaign.json')
(HERE/'analysis.json').write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps([{k:v for k,v in case.items() if k not in ['scenarios','sheets']} for case in summaries], ensure_ascii=False, indent=2))
