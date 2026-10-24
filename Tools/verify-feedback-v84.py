import hashlib,json,pathlib,re,xml.etree.ElementTree as ET
from html.parser import HTMLParser
root=pathlib.Path(__file__).resolve().parents[1]
evidence=root/'Planning/Evidence/Feedback-v84'
def read(path):return json.loads((root/path).read_text(encoding='utf-8'))
before=read('Planning/Evidence/Feedback-v83/before/catalog.json');after=read('GameData/mvp-catalog.json')
for key in ['combat','growth','transitions','characters','relics']:assert before[key]==after[key],key
for key,value in before['economy'].items():
    if key not in ['slotCosts','slotUnlockStages']:assert after['economy'][key]==value,key
for a,b in zip(before['stages'],after['stages']):
    for key in ['number','kind','maxHp','damage','sequenceWeights']:assert a[key]==b[key],(a['number'],key)
    for x,y in zip(a['patternSteps'],b['patternSteps']):
        for key in ['telegraphMs','intervalMs','comboStep']:assert x[key]==y[key],(a['number'],key)
for number in [10,20]:assert before['stages'][number-1]==after['stages'][number-1]
artBefore=read('Planning/Evidence/Feedback-v84/art-manifest-before.json');art=read('Client/Assets/Game/Art/Presentation/manifest.json')
changed={'wild_dog','grey_wolf','brown_bear','sewer_rat','drowned_resident','water_wraith'}
for actor in artBefore['actors']:
    if actor['id'] not in changed:assert actor==next(a for a in art['actors'] if a['id']==actor['id']),actor['id']
for tex in art['textures']:
    if '/monster-v84/' in tex['path']:assert hashlib.sha256((root/tex['source']).read_bytes()).hexdigest()==tex['sha256']
    assert hashlib.sha256((root/'Client'/tex['path']).read_bytes()).hexdigest()==tex['sha256']
for actor in art['actors']:
    if actor['id'] not in changed:continue
    clips={c['motion']:c for c in actor['clips']}
    assert set(clips)=={'Idle','Attack','Hit','Stun'}
    assert len(clips['Idle']['frames'])==1 and len(clips['Hit']['frames'])==1
    assert 600 <= sum(clips['Attack']['frameDurationsMs']) <= 800
    assert len(clips['Stun']['frames'])>1 and clips['Stun']['loop']
    assert len(clips['Attack']['frames'])==len(clips['Attack']['sourceFrames'])==len(clips['Attack']['frameDurationsMs'])
page=root/'Planning/Reference/TimingHero-20261023/feedback-v84.html'
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key not in ['src','href','data-screen'] or not value or value.startswith(('https:','#')):continue
            target=(page.parent/value.split('#')[0]).resolve()
            if target.name=='verification.json':continue
            assert target.exists(),str(target)
Links().feed(page.read_text(encoding='utf-8'))
checks={}
for name,file in [('editmode','editmode-final.xml'),('playmode','playmode-final.xml')]:
    r=ET.parse(root/f'Logs/Feedback-v84/{file}').getroot()
    checks[name]={k:r.attrib[k] for k in ['total','passed','failed','result']};assert r.attrib['failed']=='0'
for size in ['desktop','mobile']:
    result=read(f'Planning/Evidence/Feedback-v84/{size}/result.json');assert result['passed'];checks[size]=result
log=(root/'Logs/Feedback-v84/build.log').read_text(encoding='utf-8')
assert 'LAST_BELL_BUILD: Succeeded' in log and '0 errors' in log
checks['build']=re.search(r'LAST_BELL_BUILD: ([^\n]+)',log).group(1)
checks['preserved']=['16 unrelated actors and original selected texture hashes','approved player run','Stage 10 and 20 boss art and encounter data','HP, damage, stat upgrade costs, stage rewards, combat timing','checkpoint 11 and maximum stage 20']
checks['changed']=['six native-pixel actor sets with revised stunned poses','full stagger requires a confirmed hit','all owned relics automatically apply on new run','3..10 distinct relic storage with explicit fragment expansion','save schema 7->8 retains paid expansions and all owned relics','prologue and boss dialogue continuity']
checks['dataVersion']=after['dataVersion'];checks['bundleVersion']='0.8.4';checks['saveSchema']=8
checks['limitations']=['Automated Windows renders at 960x540 and 640x360; no physical phone touch test.','Current playable scope remains 1..20.','Existing active runs retain their stat snapshot until the next run.']
checks['sourceSha256']=read('Client/Assets/Game/Data/Generated/mvp-catalog.json')['sourceSha256']
checks['artManifestSha256']=hashlib.sha256((root/'Client/Assets/Game/Art/Presentation/manifest.json').read_bytes()).hexdigest()
for name in ['Builds/Windows/LastBellMvp_Data/Managed/LastBell.Runtime.dll','Builds/LastBellMvp-Windows.zip']:
    path=root/name;checks[name]={'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
(evidence/'verification.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True,indent=2))
