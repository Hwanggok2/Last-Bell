"""Final audit of shipped roster, animation, licensed sound and Unity evidence."""
import hashlib,json,re,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
EVIDENCE=ROOT/'Planning/Evidence/Feedback-v86'
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
before=read('Planning/Evidence/Feedback-v86/catalog-before.json')
data=read('GameData/mvp-catalog.json')
for key in ['combat','growth','economy','relicRules','characters','relics','transitions','stories','onboarding']:assert data[key]==before[key],key
for old,new in zip(before['stages'],data['stages']):assert {k:v for k,v in old.items() if k!='enemyId'}=={k:v for k,v in new.items() if k!='enemyId'}
art=read('Client/Assets/Game/Art/Presentation/manifest.json')
old_art=read('Planning/Evidence/Feedback-v86/art-manifest-before.json')
restored={'ash_wisp','chain_thrall','hook_hound','chain_puppet','cracked_blade','chain_dancer'}
for t in old_art['textures']:assert digest(ROOT/'Client'/t['path'])==t['sha256'],t['path']
for old in old_art['actors']:
    new=next(a for a in art['actors'] if a['id']==old['id'])
    if old['id'] in restored:
        assert {k:v for k,v in old.items() if k!='clips'}=={k:v for k,v in new.items() if k!='clips'}
        assert [c for c in old['clips'] if c['motion']!='Idle']==[c for c in new['clips'] if c['motion']!='Idle']
    else:assert old==new,old['id']
roster=[]
for stage in data['stages'][:20]:
    enemy=next(e for e in data['enemies'] if e['id']==stage['enemyId'])
    actor=next(a for a in art['actors'] if a['id']==enemy['artId'])
    idle=next(c for c in actor['clips'] if c['motion']=='Idle')
    assert idle['loop'] and len({digest(ROOT/'Client'/f) for f in idle['frames']})>1
    roster.append(dict(stage=stage['number'],enemyId=enemy['id'],artId=enemy['artId'],name=enemy['displayName'],idleFrames=len(idle['frames'])))
assert len({r['enemyId'] for r in roster})==len({r['artId'] for r in roster})==20
for t in art['textures']:
    if '/monster-v86/' in t['path']:
        assert digest(ROOT/'Client'/t['path'])==digest(ROOT/t['source'])==t['sha256']
audio=read('Client/Assets/Game/Audio/Combat-v86/manifest.json')
for r in audio['files']:
    assert digest(ROOT/'Client/Assets/Game/Audio/Combat-v86'/r['file'])==r['sha256']
    assert r['license']=='CC0-1.0' and r['peakDbfs']<-2
for r in audio['sourceEvidence']:assert digest(ROOT/r['file'])==r['sha256']
assert audio==read('Client/Assets/StreamingAssets/AudioLicenses/manifest.json')
checks={}
for kind in ['editmode','playmode']:
    report=ET.parse(ROOT/f'Logs/Feedback-v86/{kind}.xml').getroot()
    checks[kind]={k:report.get(k) for k in ['total','passed','failed','result']}
    assert report.get('failed')=='0' and report.get('result')=='Passed'
for size in ['desktop','mobile']:
    checks[size]=read(f'Planning/Evidence/Feedback-v86/{size}/result.json');assert checks[size]['passed']
log=(ROOT/'Logs/Feedback-v86/build.log').read_text(encoding='utf-8')
assert 'LAST_BELL_BUILD: Succeeded' in log and '0 errors' in log
checks['build']=re.search(r'LAST_BELL_BUILD: ([^\n]+)',log).group(1)
checks['bundleVersion']='0.8.6';checks['roster']=roster
checks['audio']=[{k:r[k] for k in ['file','durationSeconds','peakDbfs','license']} for r in audio['files']]
checks['preserved']=['Combat/economy/timing/checkpoint/save rules','Every original non-Idle clip and texture','Existing UI layout, hero art, character selection Idle','Both boss designs and animations']
checks['changed']=['20 unique enemies across 20 playable stages','Six additional animals, six restored residents with new Idle','Immediate accepted-input dodge sound including perfect dodge and down-key parry','Short CC0 blade impact, four CC0 chiptune tracks','Bell playback removed; both serialized bell sources disabled']
checks['limitations']=['Desktop and mobile-sized Windows renders, no physical phone test','No web hosting update','Local HTML browser automation unavailable; source frames and Unity renders reviewed']
checks['artManifestSha256']=digest(ROOT/'Client/Assets/Game/Art/Presentation/manifest.json')
for name in ['Builds/Windows/LastBellMvp_Data/Managed/LastBell.Runtime.dll','Builds/LastBellMvp-Windows.zip']:
    p=ROOT/name;checks[name]=dict(bytes=p.stat().st_size,sha256=digest(p))
(EVIDENCE/'verification.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:checks[k] for k in ['editmode','playmode','desktop','mobile','build','bundleVersion']},indent=2))
