import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
evidence=root/'Planning/Evidence/Feedback-v85'
def read(path): return json.loads((root/path).read_text(encoding='utf-8'))
assert read('Planning/Evidence/Feedback-v85/catalog-before.json')==read('GameData/mvp-catalog.json'),'Gameplay catalog changed'
before=read('Planning/Evidence/Feedback-v85/art-manifest-before.json')
art=read('Client/Assets/Game/Art/Presentation/manifest.json')
creatures={'wild_dog','grey_wolf','brown_bear','sewer_rat','drowned_resident','water_wraith'}
bosses={'bell_knight_concept','chain_jailer'}
for t in before['textures']:
    assert hashlib.sha256((root/'Client'/t['path']).read_bytes()).hexdigest()==t['sha256'],t['path']
for a in before['actors']:
    now=next(x for x in art['actors'] if x['id']==a['id'])
    if a['id'] in bosses:
        assert {k:v for k,v in a.items() if k!='clips'}=={k:v for k,v in now.items() if k!='clips'}
        assert [c for c in a['clips'] if c['motion']!='Idle']==[c for c in now['clips'] if c['motion']!='Idle']
    elif a['id'] not in creatures: assert now==a,a['id']
clips={}
for a in art['actors']:
    if a['id'] not in creatures|bosses: continue
    idle=next(c for c in a['clips'] if c['motion']=='Idle')
    assert idle['loop'] and len(idle['frames'])>1
    assert len(idle['frames'])==len(idle['frameDurationsMs'])
    assert len({hashlib.sha256((root/'Client'/p).read_bytes()).hexdigest() for p in idle['frames']})>1
    clips[a['id']]={'frames':len(idle['frames']),'durationMs':sum(idle['frameDurationsMs'])}
for t in art['textures']:
    if '/monster-v85/' not in t['path']: continue
    for p in (root/t['source'],root/'Client'/t['path']):
        assert hashlib.sha256(p.read_bytes()).hexdigest()==t['sha256'],p
checks={}
for kind in ('editmode','playmode'):
    report=ET.parse(root/f'Logs/Feedback-v85/{kind}.xml').getroot()
    checks[kind]={k:report.attrib[k] for k in ('total','passed','failed','result')}
    assert report.attrib['failed']=='0' and report.attrib['result']=='Passed'
for size in ('desktop','mobile'):
    checks[size]=read(f'Planning/Evidence/Feedback-v85/{size}/result.json')
    assert checks[size]['passed']
log=(root/'Logs/Feedback-v85/build.log').read_text(encoding='utf-8')
assert 'LAST_BELL_BUILD: Succeeded' in log and '0 errors' in log
checks['build']=re.search(r'LAST_BELL_BUILD: ([^\n]+)',log).group(1)
checks['bundleVersion']='0.8.5'
checks['idle']=clips
checks['preserved']=['Complete gameplay catalog including economy, combat timing, stage data and story','Original hero art and clips','Boss designs, scale, pivots, attacks and stuns','Existing UI layout and fonts','Save schema 8']
checks['changed']=['Selected character idle instead of a static portrait','Six creatures redesigned against the original hero','Animated idle for six creatures and both active bosses','Idle lead-in before authored attack anticipation']
checks['preview']={'file':'Planning/Reference/TimingHero-20261023/feedback-v85.html','javascriptSyntax':'passed node --check','localLinks':'passed','browserRender':'not performed: browser security policy blocks file:// URLs; no workaround attempted'}
checks['limitations']=['Windows renders at 960x540 and 640x360, no physical phone test','Unreleased characters without approved art remain unavailable','No web hosting update in this revision','Preview browser automation is blocked for local file:// URLs; source frames and actual Unity renders were reviewed']
checks['artManifestSha256']=hashlib.sha256((root/'Client/Assets/Game/Art/Presentation/manifest.json').read_bytes()).hexdigest()
for name in ('Builds/Windows/LastBellMvp_Data/Managed/LastBell.Runtime.dll','Builds/LastBellMvp-Windows.zip'):
    p=root/name;checks[name]={'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
(evidence/'verification.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks,ensure_ascii=False,indent=2))
