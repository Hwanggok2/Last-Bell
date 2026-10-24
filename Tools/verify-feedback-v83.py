import hashlib,json,pathlib,re,xml.etree.ElementTree as ET
from html.parser import HTMLParser
root=pathlib.Path(__file__).resolve().parents[1]
evidence=root/'Planning/Evidence/Feedback-v83'
def read(path):return json.loads((root/path).read_text(encoding='utf-8'))
before=read('Planning/Evidence/Feedback-v83/before/catalog.json');after=read('GameData/mvp-catalog.json')
for key in ['combat','growth','economy','transitions','onboarding','stories','characters','relics']:assert before[key]==after[key],key
for a,b in zip(before['stages'],after['stages']):
    for key in ['number','kind','maxHp','damage','sequenceWeights']:assert a[key]==b[key],(a['number'],key)
    for x,y in zip(a['patternSteps'],b['patternSteps']):
        for key in ['telegraphMs','intervalMs','comboStep']:assert x[key]==y[key],(a['number'],key)
for number in [10,20]:assert before['stages'][number-1]==after['stages'][number-1]
artBefore=read('Planning/Evidence/Feedback-v83/before/art-manifest.json');art=read('Client/Assets/Game/Art/Presentation/manifest.json')
for old in artBefore['actors']:assert old==next(a for a in art['actors'] if a['id']==old['id']),old['id']
for tex in art['textures']:
    if '/monster-v83/' in tex['path']:
        assert hashlib.sha256((root/tex['source']).read_bytes()).hexdigest()==tex['sha256']
    assert hashlib.sha256((root/'Client'/tex['path']).read_bytes()).hexdigest()==tex['sha256']
page=root/'Planning/Reference/TimingHero-20261023/feedback-v83.html'
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key not in ['src','href'] or not value or value.startswith(('https:','#')):continue
            target=(page.parent/value.split('#')[0]).resolve()
            if target.name=='verification.json':continue
            assert target.exists(),str(target)
Links().feed(page.read_text(encoding='utf-8'))
checks={}
for name in ['editmode','playmode']:
    r=ET.parse(root/f'Logs/Feedback-v83/{name}-final.xml').getroot()
    checks[name]={k:r.attrib[k] for k in ['total','passed','failed','result']};assert r.attrib['failed']=='0'
for size in ['desktop','mobile']:
    result=read(f'Planning/Evidence/Feedback-v83/{size}/result.json');assert result['passed'];checks[size]=result
log=(root/'Logs/Feedback-v83/build-final.log').read_text(encoding='utf-8')
assert 'LAST_BELL_BUILD: Succeeded' in log and '0 errors' in log
checks['build']=re.search(r'LAST_BELL_BUILD: ([^\n]+)',log).group(1)
checks['preserved']=['all previous actor definitions and selected texture hashes','approved player run','Stage 10 and 20 bosses','HP, damage, upgrade costs, rewards, combat timings','save schema 7 and checkpoint 11']
checks['dataVersion']=after['dataVersion'];checks['bundleVersion']='0.8.3'
checks['limitations']=['Automated Windows renders at 960x540 and 640x360; no physical phone touch test.','Game-feel proposals in the report are not balance/input implementations.']
checks['sourceSha256']=read('Client/Assets/Game/Data/Generated/mvp-catalog.json')['sourceSha256']
checks['artManifestSha256']=hashlib.sha256((root/'Client/Assets/Game/Art/Presentation/manifest.json').read_bytes()).hexdigest()
for name in ['Builds/Windows/LastBellMvp_Data/Managed/LastBell.Runtime.dll','Builds/LastBellMvp-Windows.zip']:
    path=root/name;checks[name]={'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
(evidence/'verification.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True,indent=2))
