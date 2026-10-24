"""One final static check of the planning package. No Unity/browser execution."""
import json,re,struct
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit,unquote
B=Path(__file__).resolve().parent
class Page(HTMLParser):
    def __init__(self,source):
        super().__init__();self.ids=[];self.refs=[];self.feed(source)
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id' in a:self.ids.append(a['id'])
        for name in ['href','src']:
            if name in a:self.refs.append(a[name])
read=lambda x:json.loads((B/x).read_text(encoding='utf-8'))
C,S,D=read('cast.json'),read('story.json'),read('design.json')
source=(B/'index.html').read_text(encoding='utf-8');page=Page(source)
errors=[]
check=lambda ok,message:errors.append(message) if not ok else None
check(len(page.ids)==len(set(page.ids)),'duplicate IDs')
for ref in page.refs:
    u=urlsplit(ref)
    if u.scheme:continue
    p=(B/unquote(u.path)).resolve() if u.path else B/'index.html'
    if p==B/'validation.json':continue
    if not p.is_file():errors.append('missing '+ref);continue
    if u.fragment and p.suffix=='.html':check(u.fragment in Page(p.read_text(encoding='utf-8-sig')).ids,'missing anchor '+ref)
for ref in re.findall(r"url\(['\"]?([^)'\"]+)",(B/'board.css').read_text(encoding='utf-8')):check((B/ref).is_file(),'missing CSS asset '+ref)
check(len(C['coreCast'])==10,'cast count')
check({x['id'] for x in C['coreCast']}=={x['id'] for x in S['characters']},'cast IDs differ')
check([x['stage'] for x in S['stages']]==list(range(1,51)),'stage sequence')
check(len({x['enemyId'] for x in S['stages']})==50,'duplicate stage enemy IDs')
check([z['range'] for z in S['zones']]==[[1,10],[11,20],[21,30],[31,40],[41,50]],'zone ranges')
check([x['name'] for x in C['bossSequence']]==[z['boss']['name'] for z in S['zones']],'boss names')
check([x['zone'] for x in C['bossSequence']]==[z['name'] for z in S['zones']],'zone names')
check(len(S['opening']['sentences'])==len(S['ending']['sentences'])==3,'opening/ending count')
check([len(z['bossDialogue']) for z in S['zones']]==[3,6,3,3,5],'dialogue count')
check(S['terminologyProposal']['premiumCurrency']['after']=='보석','currency name')
check(S['terminologyProposal']['specialStat']['after']=='기세','stat name')
check('별수정' not in source,'stale currency in HTML')
check('직업 선택</span>' not in source,'premature class selection')
ground,skills=read('stage-ground-v1.json'),read('character-skills-v2.json')
check(len(ground['stages'])==50 and len(ground['regions'])==5,'ground plan coverage')
check({s['characterId'] for s in skills['skills']}=={'on','dam','rin'} and all(s['cooldownMs']==20000 for s in skills['skills']),'manual signature skill scope')
images={}
for name in ['cast-concept','journey-concept','meadow-concept','monster-pixel-v2','actors-pixel-v3','meadow-pixel-v3','battle-normal-clean-v3','battle-normal-telegraph-v3','battle-boss-clean-v3','battle-boss-telegraph-v3','battle-wireframe-v3','battle-normal-clean-v4','battle-normal-telegraph-v4','battle-boss-clean-v4','battle-boss-telegraph-v4','battle-wireframe-v4','ui-components-v4']:
    data=(B/'images'/f'{name}.png').read_bytes();images[name]=list(struct.unpack('>II',data[16:24]))
for name in ['cast-generation.json','environment-generation.json','monster-pixel-v2.json','actors-pixel-v3-generation.json','pixel-v3-generation.json','ui-pixel-v4.json']:read(name)
entryFiles=[B.parents[1]/'index.html',B.parents[1]/'Presentation/index.html',B.parent/'Bright-v1/index.html',B.parent/'story-world-v2.html']
for p in entryFiles:check(p.read_text(encoding='utf-8-sig').count('id="latest-dragon-v1"')==1,'entry banner '+str(p))
result={'date':'2026-10-24','status':'pass' if not errors else 'fail','scope':'planning, source consistency, generated concepts','counts':{'cast':8,'zones':5,'stages':50,'baseEnemyArchetypes':len(S['productionNotes']['uniqueEnemyArchetypes']),'localReferences':len(page.refs),'entryPoints':len(entryFiles)},'images':images,'review':['cast and story agents agreed characters before writing the final stage route','cast/story IDs, region and boss names, currency and stat names cross-checked','art agent and root visually inspected three original GPT concepts and the monster pixel revision','integrated source review corrected construct terminology and premature class-selection label'],'notPerformed':['browser layout/real interaction QA','Unity changes, game runtime tests or builds','production sprite animation and parallax layers'],'limitations':['generated artwork is concept art with more detail than final target pixel density','50 stage entries reuse11 body archetypes and require variant-specific silhouettes/motions','title is a working title, not a store listing or repository rename'],'errors':errors}
if (B/'tab-validation.json').is_file():result['tabController']=read('tab-validation.json')
if (B/'ui-pixel-v4-validation.json').is_file():result['pixelUi']=read('ui-pixel-v4-validation.json')
if (B/'pixel-ui-v4-review.md').is_file():result['review'].append('UI reviewer inspected v4 battle composites and component states; see pixel-ui-v4-review.md for scope and limitations')
result['scope']='Static planning links, source consistency and concept image metadata only'
result['notPerformed']=['Browser layout/interaction inspection; local file navigation was blocked','Runtime execution by this static script; see the separate integration and QA records']
result['counts']['cast']=len(C['coreCast'])
result['currentProduction']='../../Production/dragon-v6-integration.json'
result['planning']={'groundStages':50,'groundRegions':5,'signatureSkills':3,'skillsImplemented':True,'playableStages':20,'portraits':10,'companionSkills':5,'healthTrail':'single actual damage >= maximum HP/3;120ms hold +550ms linear'}
result['review'].append('V4 manual skill contract, 3 playable actors and per-speaker portraits supersede the earlier charge-skill proposal')
result['review'].append('Player-v3 and Forest-v1 source art and Dragon-v3 plan have separate authored/independent review records linked in the board')
(B/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'status':result['status'],'counts':result['counts'],'errors':errors},ensure_ascii=False))
if errors:raise SystemExit(1)
