from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
import json,hashlib,subprocess
R=Path(__file__).resolve().parent; REPO=R.parents[2]
class Scan(HTMLParser):
    def __init__(self):super().__init__();self.refs=[];self.ids=[]
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id' in a:self.ids.append(a['id'])
        self.refs.extend(a[k] for k in ['src','href'] if k in a)
missing=[];duplicates=[];count=0
for name in ['index.html','plan-v81.html','preview.html','preview-v81.html']:
    p=Scan();p.feed((R/name).read_text(encoding='utf-8'))
    duplicates += [name+':'+x for x in set(p.ids) if p.ids.count(x)>1]
    for ref in p.refs:
        u=urlparse(ref)
        if u.scheme or u.netloc:continue
        if not u.path:
            if u.fragment not in p.ids:missing.append(name+':'+ref)
            continue
        count+=1;target=R/unquote(u.path)
        if target.name=='verification-v81.json':continue
        if not target.exists():missing.append(name+':'+ref)
assert not missing and not duplicates,(missing,duplicates)
before=json.loads((R/'game-before-v81.json').read_text(encoding='utf-8'))['files']
changed=[rel for rel,digest in before.items() if not (REPO/rel).exists() or hashlib.sha256((REPO/rel).read_bytes()).hexdigest()!=digest]
now={p.relative_to(REPO).as_posix() for folder in ['Client/Assets/Game','GameData','Client/Assets/WebGLTemplates'] for p in (REPO/folder).rglob('*') if p.is_file()}
added=sorted(now-set(before));assert not changed and not added,(changed,added)
assert (R/'index.html').read_bytes()==(R/'plan-v81.html').read_bytes()
assert (R/'preview.html').read_bytes()==(R/'preview-v81.html').read_bytes()
ff=r'C:/Users/JW/AppData/Roaming/vrew/ffmpeg_gpl_vgx_v3/ffmpeg.exe'
subprocess.run([ff,'-v','error','-i',str(R/'combat-v81.mp4'),'-f','null','-'],capture_output=True,check=True)
model=json.loads((R/'model-v81-verification.json').read_text(encoding='utf-8'))
result={'version':'v8.1','date':'2026-10-23','scope':'Planning and preview only','gameFilesUnchanged':len(before),'gameFilesChanged':changed,'gameFilesAdded':added,'localLinksChecked':count,'missingLinks':missing,'canonicalPagesMatchVersionedPages':True,'modelChecks':model['passed'],'renderedScenes':len(json.loads((R/'layout-v81.json').read_text(encoding='utf-8'))),'visualReview':['selection: desktop/mobile, locked hero','upgrade: single/MAX, insufficient funds, max level','prologue: skip only, scroll clipped to parchment','combat: three directions, normal and boss HP below actors, level and name below bar','settlement: run end, milestone notice, selected/unselected relic, final result'],'wireframes':['previews-v81/wire-selection.png','previews-v81/wire-upgrade.png','previews-v81/wire-battle.png'],'motionPreview':{'file':'combat-v81.mp4','decode':'passed','type':'SVG concept animation, not Unity capture'},'limits':['No Unity game, data or build changed','No browser/device touch test performed','Timing numbers are proposed tuning, not recovered reference source constants','Preview combat is automatic presentation, not a full combat simulation','Persistent reward logic is tested in a plain JavaScript planning model, not the real save repository']}
(R/'verification-v81.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'v8.1: {len(before)} game files unchanged; {count} links valid; {len(model["passed"])} model checks; video decoded.')
