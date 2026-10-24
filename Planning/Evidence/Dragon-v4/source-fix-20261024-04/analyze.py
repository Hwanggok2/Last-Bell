"""Read a completed V4 candidate; never start Unity/player or alter native evidence."""
import argparse
import array
import collections
import csv
import hashlib
import json
import math
from pathlib import Path
import wave


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def lines(path):
    return [json.loads(line) for line in path.read_text(encoding='utf-8-sig').splitlines()] if path.exists() else []


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')


def pcm_metrics(path):
    with wave.open(str(path), 'rb') as source:
        if source.getsampwidth() != 2:
            return {'file':path.name,'status':'UNSUPPORTED_PCM_WIDTH','listening':'NOT_RUN'}
        channels,rate = source.getnchannels(),source.getframerate()
        samples = array.array('h', source.readframes(source.getnframes()))
    peak = max((abs(x) for x in samples),default=0)/32768
    rms = math.sqrt(sum((x/32768)**2 for x in samples)/max(1,len(samples)))
    size = max(1,round(rate*channels*.01))
    envelope = [max(abs(x) for x in samples[i:i+size])/32768 for i in range(0,len(samples),size)]
    return {'file':path.name,'sampleRate':rate,'channels':channels,'samples':len(samples),'durationSeconds':len(samples)/max(1,rate*channels),
            'peak':peak,'rms':rms,'envelope10ms':envelope,'listening':'NOT_RUN',
            'limits':'Source-filter PCM only. Nonzero/zero signal does not independently prove device mute, distinct voice count, no overlap or perceived sound quality.'}


def make_sheet(folder, dest, selections):
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',14)
    cols = min(6,len(selections));cw,ch=320,210
    canvas = Image.new('RGB',(cw*cols,ch*math.ceil(len(selections)/cols)),'#f4eedc');draw=ImageDraw.Draw(canvas)
    index=[]
    for i,(label,file) in enumerate(selections):
        x,y=i%cols*cw,i//cols*ch
        with Image.open(folder/file) as source:
            source.thumbnail((cw,180),Image.Resampling.NEAREST);canvas.paste(source,(x+(cw-source.width)//2,y+30))
        draw.text((x+3,y+3),label,font=font,fill='#142d32');index.append({'label':label,'file':file})
    canvas.save(dest);write(dest.with_suffix('.json'),index)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate',required=True,type=Path)
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[3]
    candidate=args.candidate.resolve();allowed=(root/'Planning/Evidence/Dragon-v4').resolve()
    if not candidate.is_relative_to(allowed) or not (candidate/'campaign.json').is_file():
        raise SystemExit('Require an existing completed V4 campaign beneath Planning/Evidence/Dragon-v4')
    review=candidate/'review';review.mkdir(exist_ok=True)
    campaign=read(candidate/'campaign.json');cases=[];motion_rows=[]
    for run in campaign['runs']:
        relative=f"{run['width']}x{run['height']}"+('' if run['mode']=='v4-resume' else '/dash' if run['mode']=='v4-dash' else '/fresh')
        folder=candidate/relative
        required=['result.json','v4-evidence.json','v4-frame-evidence.json']
        absent=[name for name in required if not (folder/name).is_file()]
        if absent:
            cases.append({'case':relative,'nativeExit':run['exitCode'],'timedOut':run['timedOut'],'passed':False,'missingReports':absent,'visualReview':'NOT_RUN','audioListening':'NOT_RUN'})
            continue
        result,evidence,frame_evidence=read(folder/'result.json'),read(folder/'v4-evidence.json'),read(folder/'v4-frame-evidence.json')
        frames=lines(folder/'frames.jsonl');states=lines(folder/'v4-state.jsonl')
        groups=collections.defaultdict(list)
        for frame in frames:groups[frame['scenario']].append(frame)
        contacts=[];sheets=[]
        for label,items in groups.items():
            if label.startswith('v4-enemy-'):
                hits=[e for e in evidence['events'] if e['scenario']==label and e['kind'] in ['PlayerDamaged','SkillShieldAbsorbed']]
                for event in hits:
                    at=min(range(len(items)),key=lambda i:abs(items[i]['combatMs']-event['combatMs']))
                    first=items[at];after=items[min(at+1,len(items)-1)]
                    contacts.append({'scenario':label,'impactMs':event['combatMs'],'contactFile':first['file'],'nextFile':after['file'],
                        'contactClockOffsetMs':first['combatMs']-event['combatMs'],'capturedFrameGapMs':(after['realtime']-first['realtime'])*1000,
                        'postContactRootStepPx':(after['enemy']['root']['x']-first['enemy']['root']['x'])*run['height']/(2*after['cameraSize'])})
            picks=[items[round(i*(len(items)-1)/max(1,min(11,len(items)-1)))] for i in range(min(12,len(items)))]
            name=f"{run['width']}x{run['height']}-{label}.png"
            make_sheet(folder,review/name,[(f"f{f['frameCount']} {f['combatMs']:.1f}ms",f['file']) for f in picks]);sheets.append(name)
            for f in items:
                motion_rows.append({'case':relative,'scenario':label,'file':f['file'],'frameCount':f['frameCount'],'realtime':f['realtime'],'combatMs':f['combatMs'],
                    'stage':f['stage'],'motion':f['enemy']['motion'],'clipFrame':f['enemy']['clipFrame'],'rootX':f['enemy']['root']['x'],'rootY':f['enemy']['root']['y'],'playerHp':f['playerHp']})
        for character in ['on','dam','rin']:
            for stage in [2,7,10,15,20]:
                pages=[s for s in states if s['tag'].startswith(f'v4-story-{character}-{stage}-page-')]
                if pages:
                    name=f"{run['width']}x{run['height']}-story-{character}-{stage}.png"
                    make_sheet(folder,review/name,[(f"page {s['line']} / portrait {s['portraitId']}",s['file']) for s in pages]);sheets.append(name)
        mismatch=[f['file'] for f in frames if hashlib.sha256((folder/f['file']).read_bytes()).hexdigest()!=f['sha256']]
        missing=[s['file'] for s in states if s.get('file') and not (folder/s['file']).is_file()]
        case={'case':relative,'nativeExit':run['exitCode'],'timedOut':run['timedOut'],'passed':result['passed'],'completed':result['completed'],
            'passChecks':sum(x.startswith('PASS ') for x in result['checks']),'failures':[x for x in result['checks'] if x.startswith('FAIL ')],'consoleErrors':result['errors'],
            'indexedFrames':len(frames),'writtenFrames':frame_evidence['writtenFrames'],'segments':frame_evidence['segments'],
            'missingRenderedFrames':sum(s['missingRenderedFrames'] for s in frame_evidence['segments']),'duplicateFiles':len(frames)-len({f['file'] for f in frames}),
            'hashMismatches':mismatch,'missingStateCaptures':missing,'stateRecords':len(states),'contacts':contacts,
            'maxGroundedPivotYErrorPx':{who:max((abs(f[who]['feetScreen']['y']-f[who]['groundScreen']['y']) for f in frames),default=None) for who in ['player','enemy']},
            'audio':evidence['audio'],'dashes':evidence.get('dashes',[]),'waveforms':[pcm_metrics(folder/a['file']) for a in evidence['audio'] if a.get('file')],
            'sheets':sheets,'visualReview':'NOT_RUN','audioListening':'NOT_RUN','voiceMultiplicity':'NOT_CERTIFIED'}
        cases.append(case)
    if motion_rows:
        with (review/'motion-timeline.csv').open('w',encoding='utf-8',newline='') as target:
            writer=csv.DictWriter(target,fieldnames=list(motion_rows[0]));writer.writeheader();writer.writerows(motion_rows)
    output={'candidate':candidate.name,'campaign':campaign,'cases':cases,'prefs':read(candidate/'playerprefs-restored.json'),
        'syntheticLegacyUnchanged':read(candidate/'protected-saves-final.json')['matches'],'userDefaultProfile':'NOT_ACCESSED','osInput':'DEFERRED',
        'limits':'Computed measurements and contact sheets are review aids. No automatic visual/audio acceptance; no normal-speed video/device performance claim. Core tests are reused from Client evidence.'}
    write(review/'analysis.json',output)
    print(json.dumps([{'case':c['case'],'passed':c['passed'],'checks':c.get('passChecks'),'frames':c.get('indexedFrames'),'hashMismatches':len(c.get('hashMismatches',[])),'missingReports':c.get('missingReports',[])} for c in cases]))


if __name__=='__main__':main()
