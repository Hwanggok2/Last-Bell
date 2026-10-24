"""Analyze completed recovery captures without changing native evidence."""
import collections
import csv
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
CANDIDATE = HERE.parent
FONT = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 16)

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def sheet(folder, name, rows, width, height):
    cw, ch = 280, 270
    canvas = Image.new('RGB', (cw*10, ch*len(rows)), '#f6f1df')
    draw = ImageDraw.Draw(canvas)
    entries = []
    for row, (label, frames, contact) in enumerate(rows):
        for col, f in enumerate(frames):
            with Image.open(folder/f['file']) as source:
                # Same wide crop for every frame preserves visible movement.
                box = tuple(round(x*width/1280) for x in (340, 50, 1180, 515))
                crop = source.crop(box)
                scale = min(cw/crop.width, (ch-62)/crop.height)
                crop = crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.NEAREST)
                canvas.paste(crop,(col*cw+(cw-crop.width)//2,row*ch+62))
            a = f['enemy']
            draw.text((col*cw+4,row*ch+2),f"{label} {a['motion']}:{a['clipFrame']}",font=FONT,fill='#162c32')
            draw.text((col*cw+4,row*ch+22),f"f{f['frameCount']} {f['combatMs']-contact:+.1f}ms",font=FONT,fill='#162c32')
            draw.text((col*cw+4,row*ch+42),f"rootX {a['root']['x']:.4f}",font=FONT,fill='#162c32')
            entries.append({'row':row,'column':col,'file':f['file'],'frameCount':f['frameCount'],'combatMs':f['combatMs']})
    path = HERE/f'{width}x{height}-{name}.png'
    canvas.save(path)
    write(path.with_suffix('.json'), entries)
    return str(path.relative_to(CANDIDATE))

cases, timeline = [], []
for width,height in [(1280,720),(640,360)]:
    folder = CANDIDATE/f'{width}x{height}'/'fresh'
    result,evidence = read(folder/'result.json'),read(folder/'v3-evidence.json')
    frames = [json.loads(line) for line in (folder/'frames.jsonl').read_text(encoding='utf-8-sig').splitlines()]
    groups = collections.defaultdict(list)
    for f in frames:
        groups[f['scenario']].append(f)
    with (folder/'events.csv').open(encoding='utf-8-sig',newline='') as source:
        events = list(csv.DictReader(source))
    actors = []
    for stage,reach in [(19,296),(20,211)]:
        idle = groups[f'recheck-{stage}-automatic-idle']
        combo = groups[f'recheck-{stage}-actual-combo-recovery']
        hits = [e for e in events if e['scenario']==f'recheck-{stage}-actual-combo-recovery' and e['event']=='PlayerDamaged']
        home = idle[0]['enemy']['root']['x']
        metrics,rows = [],[]
        for number,e in enumerate(hits):
            contact = float(e['combatMs'])
            # The exact contact pose is captured at ClockMs==ImpactAtMs.
            # Damage dispatch can occur on the next update; do not use its frame
            # number to silently skip that pose in the before/after comparison.
            at = min(range(len(combo)),key=lambda i:abs(combo[i]['combatMs']-contact))
            first,after = combo[at],combo[at+1]
            recovery = [f for f in combo if contact<=f['combatMs']<=contact+300]
            returned = next(f for f in combo[at:] if abs(f['enemy']['root']['x']-home)<1e-5)
            errors = []
            for f in recovery:
                progress = max(0,1-(f['combatMs']-contact)/280)
                progress = progress*progress*(3-2*progress)
                measured = (home-f['enemy']['root']['x'])*height/(2*f['cameraSize'])
                errors.append(abs(measured-reach*width/1280*progress))
            metrics.append({'contactMs':contact,'contactFile':first['file'],'nextFile':after['file'],
                'contactPoseFrameCount':first['frameCount'],'damageEventFrameCount':int(e['frame']),
                'firstCapturedAfterContactMs':first['combatMs']-contact,
                'firstPostContactRootStepPx':(after['enemy']['root']['x']-first['enemy']['root']['x'])*height/(2*after['cameraSize']),
                'firstPostContactFrameGapMs':(after['realtime']-first['realtime'])*1000,
                'firstPostContactFrames':[first['enemy']['clipFrame'],after['enemy']['clipFrame']],
                'firstPostContactIsHome':abs(after['enemy']['root']['x']-home)<1e-5,
                'recoveryFrames':len(recovery),'recoveryMonotonic':all(b['enemy']['root']['x']>=a['enemy']['root']['x']-1e-5 for a,b in zip(recovery,recovery[1:])),
                'firstCapturedHomeAfterContactMs':returned['combatMs']-contact,
                'maxExisting280msSmoothReturnResidualPx':max(errors),
                'recoveryClipFrames':sorted({f['enemy']['clipFrame'] for f in recovery if f['enemy']['clipMotion']=='Attack'})})
            offsets = [-40,0,40,80,140,220,280,320,450,550]
            selected = [min(combo,key=lambda f:abs(f['combatMs']-(contact+t))) for t in offsets]
            rows.append((f'Hit {number+1}',selected,contact))
        idle_selected = [next(f for f in idle if f['enemy']['clipFrame']==index) for index in sorted({f['enemy']['clipFrame'] for f in idle})]
        sheets = [sheet(folder,f'enemy-{stage}-recovery',rows,width,height),sheet(folder,f'enemy-{stage}-automatic-idle',[('Idle',idle_selected,0)],width,height)]
        actors.append({'stage':stage,'idleMotion':sorted({f['enemy']['motion'] for f in idle}),'idleFrames':sorted({f['enemy']['clipFrame'] for f in idle}),
            'idleDurationSeconds':idle[-1]['realtime']-idle[0]['realtime'],'idleCapturedFrames':len(idle),
            'actualAttackFrames':sorted({f['enemy']['clipFrame'] for f in combo if f['enemy']['clipMotion']=='Attack'}),
            'contactCount':len(hits),'contacts':metrics,'sheets':sheets})
        for f in combo:
            timeline.append({'resolution':f'{width}x{height}','stage':stage,'file':f['file'],'frameCount':f['frameCount'],
                'realtime':f['realtime'],'combatMs':f['combatMs'],'motion':f['enemy']['motion'],'clipFrame':f['enemy']['clipFrame'],
                'sourceFrame':f['enemy']['sourceFrame'],'rootX':f['enemy']['root']['x'],'rootY':f['enemy']['root']['y'],
                'screenX':f['enemy']['feetScreen']['x'],'screenY':f['enemy']['feetScreen']['y'],'playerHp':f['playerHp']})
    adjacent = groups['enemy-18-attack-hit']
    selected = [adjacent[round(i*(len(adjacent)-1)/9)] for i in range(10)]
    regression_sheet = sheet(folder,'enemy-18-regression',[('Stage18',selected,0)],width,height)
    cases.append({'resolution':f'{width}x{height}','passed':result['passed'],'completed':result['completed'],
        'passChecks':sum(x.startswith('PASS ') for x in result['checks']),'failures':[x for x in result['checks'] if x.startswith('FAIL ')],
        'consoleErrors':result['errors'],'writtenFrames':evidence['writtenFrames'],'indexedFrames':len(frames),
        'segments':evidence['segments'],'duplicateFiles':len(frames)-len({f['file'] for f in frames}),
        'missingRenderedFrames':sum(s['missingRenderedFrames'] for s in evidence['segments']),
        'hashMismatches':[f['file'] for f in frames if hashlib.sha256((folder/f['file']).read_bytes()).hexdigest()!=f['sha256']],
        'maxRealtimeGap':max(s['maxRealtimeGap'] for s in evidence['segments']),
        'maxGroundedPivotYErrorPx':{who:max(abs(f[who]['feetScreen']['y']-f[who]['groundScreen']['y']) for f in frames) for who in ['player','enemy']},
        'actors':actors,'stage18RegressionSheet':regression_sheet,'visualReview':'PENDING_DIRECT_REVIEW'})

with (HERE/'recovery-timeline.csv').open('w',encoding='utf-8',newline='') as target:
    writer = csv.DictWriter(target,fieldnames=list(timeline[0]));writer.writeheader();writer.writerows(timeline)
output = {'candidate':CANDIDATE.name,'cases':cases,'campaign':read(CANDIDATE/'campaign.json'),
    'prefs':read(CANDIDATE/'playerprefs-restored.json'),'syntheticLegacyUnchanged':read(CANDIDATE/'protected-saves-final.json')['matches'],
    'osInput':'DEFERRED','defaultProfile':'NOT_ACCESSED','normalSpeedPlayback':'NOT_RUN',
    'limits':'Arrival Idle is a separate static-session presentation fixture. Actual combat uses automatic rendering; no forced Attack frames. Root movement excludes camera shake. Frame readback does not certify normal-speed performance.'}
if (HERE/'direct-visual-review.json').exists():
    direct = read(HERE/'direct-visual-review.json')
    for case in cases:
        case['visualReview'] = direct['status']+': see direct-visual-review.json'
write(HERE/'analysis.json',output)
print(json.dumps([{k:v for k,v in c.items() if k not in ['segments','actors']} for c in cases],ensure_ascii=True,indent=2))
