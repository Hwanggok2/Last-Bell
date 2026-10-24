from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
import hashlib,json,subprocess
R=Path(__file__).resolve().parent; REPO=R.parents[2]
before=json.loads((R/'game-before.json').read_text(encoding='utf-8'))['files']
changed=[]
for rel,digest in before.items():
    p=REPO/rel
    if not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=digest:changed.append(rel)
roots=['Client/Assets/Game','GameData','Client/Assets/WebGLTemplates']
now={p.relative_to(REPO).as_posix() for folder in roots for p in (REPO/folder).rglob('*') if p.is_file()}
added=sorted(now-set(before))
class Links(HTMLParser):
    def __init__(self):super().__init__();self.refs=[];self.ids=[]
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id' in a:self.ids.append(a['id'])
        for k in ['src','href']:
            if k in a:self.refs.append(a[k])
missing=[];duplicate_ids=[];local_count=0
for file in ['index.html','preview.html']:
    p=Links();p.feed((R/file).read_text(encoding='utf-8'))
    duplicate_ids += [f'{file}#{x}' for x in set(p.ids) if p.ids.count(x)>1]
    for ref in p.refs:
        u=urlparse(ref)
        if u.scheme or u.netloc:continue
        if not u.path:
            if u.fragment and u.fragment not in p.ids:missing.append(file+':'+ref)
            continue
        local_count+=1
        target=R/unquote(u.path)
        if target.name=='verification.json':continue
        if not target.exists():missing.append(file+':'+ref)
model=json.loads((R/'model-verification.json').read_text(encoding='utf-8'))
d=json.loads((R/'analysis.json').read_text(encoding='utf-8'))
observed_cost=sum(x['price'] for x in d['referenceGrowth'][:19])
assert observed_cost==3135
c=d['catalog']; gold=[]
for start in [0,10]:
    gold.append(sum(c['economy']['normalGoldPerStage'][s['number']]*(3 if s['kind']=='miniboss' else 5 if s['kind']=='boss' else 1) for s in c['stages'][start:start+10]))
assert gold==[1374,16422],gold
assert not changed and not added,{'changed':changed,'added':added}
assert not missing and not duplicate_ids,{'missing':missing,'duplicate':duplicate_ids}
result={'date':'2026-10-23','protectedFilesUnchanged':len(before),'protectedFilesChanged':changed,'protectedFilesAdded':added,'localLinksChecked':local_count,'missingLinks':missing,'duplicateHtmlIds':duplicate_ids,'modelChecks':model['passed'],'visualReview':{'renderer':'Shared SVG model rasterized with sharp; reviewed PNG output','screens':['prologue','lobby','relic selection','collection','battle','result'],'sizes':[[960,540],[640,360],[1200,540]],'states':['insufficient gold','max level','unselected relic','selected relic','MAX duplicate','committed relic','perfect dodge','upper parry','damage'],'corrections':['640px purchase notice overlapped lower buttons: repositioned','Compact row prices added beside value','Background scaled height left transparent gaps: fixed','Reused current chain enemy artwork and display name','Upper evade uses sword parry pose','Enemy lunge reaches contact at scheduled impact','Follow-up dodge takes priority over counter demonstration']},'scopeLimits':['No Unity runtime changed or launched for this planning task','No browser or mobile device execution validation','Interactive page is a concept/state model, not real combat','Reference HP/damage progression is partially unobservable; estimates labeled','Proposed 480ms wave needs beginner playtest against current 600ms'],'media':['combat-preview.mp4','clips/reference-normal.mp4','clips/reference-combo.mp4','clips/reference-upgrade.mp4'],'baselineGoldByZone':gold}
(R/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(before)} protected files unchanged; {local_count} local links valid; {len(model["passed"])} model/layout checks passed.')
