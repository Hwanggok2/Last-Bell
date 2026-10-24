"""Static planning validation; does not launch or test Unity or a browser."""
import json
import re
import struct
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote,urlsplit

base=Path(__file__).resolve().parent
class Page(HTMLParser):
    def __init__(self,text):
        super().__init__();self.ids=[];self.refs=[];self.feed(text)
    def handle_starttag(self,tag,attrs):
        d=dict(attrs)
        if 'id' in d:self.ids.append(d['id'])
        for key in ['src','href']:
            if key in d:self.refs.append(d[key])

errors=[]
text=(base/'index.html').read_text(encoding='utf-8')
page=Page(text)
if len(page.ids)!=len(set(page.ids)):errors.append('duplicate HTML IDs')
for ref in page.refs:
    url=urlsplit(ref)
    if url.scheme:continue
    file=(base/unquote(url.path)).resolve() if url.path else base/'index.html'
    if file==base/'validation.json':continue
    if not file.is_file():errors.append('missing link: '+ref);continue
    if url.fragment and file.suffix=='.html':
        target=Page(file.read_text(encoding='utf-8-sig'))
        if url.fragment not in target.ids:errors.append('missing anchor: '+ref)
for ref in re.findall(r"url\(['\"]?([^)'\"]+)",text):
    if not (base/ref).is_file():errors.append('missing CSS asset: '+ref)
images={}
for name in ['combat','lobby','upgrade','relic','story','wireframe']:
    file=base/'images'/f'{name}.png'
    data=file.read_bytes();size=struct.unpack('>II',data[16:24])
    images[name]=list(size)
    if size!=(1280,720):errors.append('incorrect size: '+name)
story=json.loads((base/'story.json').read_text(encoding='utf-8'))
assert [z['range'] for z in story['zones']]==[[1,10],[11,20],[21,30],[31,40],[41,50]]
assert len(story['opening']['sentences'])==len(story['ending']['sentences'])==3
assert all(len(z['dialogue'])==2 for z in story['zones'])
for file in ['ui-spec.json','generation.json']:json.loads((base/file).read_text(encoding='utf-8'))
entries={}
for file in [base.parents[1]/'index.html',base.parents[1]/'Presentation/index.html',base.parent/'story-world-v2.html']:
    content=file.read_text(encoding='utf-8-sig');entries[file.name if file.parent==base.parents[1] else file.parent.name+'/'+file.name]=content.count('id="latest-bright-v1"')
    if content.count('id="latest-bright-v1"')!=1:errors.append('missing or duplicate entry: '+str(file))
result={'date':'2026-10-24','scope':'planning and static mockups only','status':'pass' if not errors else 'fail','htmlLocalReferences':len(page.refs),'htmlIds':len(page.ids),'mockupDimensions':images,'mainPageEntries':entries,'storyChecks':['five consecutive 10-stage zones','three opening sentences','three ending sentences','two lines per boss'],'visualReview':['root and art/UI agents inspected PNGs','corrected feet alignment, unsupported glyphs, HUD contrast, cropped background gap','current upgrade quote/purchase and automatic relic application preserved'],'notPerformed':['browser rendering/real interaction QA','Unity import, runtime tests or build','physical device touch and motion contrast checks'],'limitations':['selected background is a concept, not production tiles or separated parallax layers','zones 2–5 are story and palette/layout specifications, not completed raster backgrounds','boss story uses existing idle pose; new stop/exit animations are future work'],'errors':errors}
(base/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'status':result['status'],'references':len(page.refs),'mockups':len(images),'errors':errors},ensure_ascii=False))
if errors:raise SystemExit(1)
