"""Static planning/asset checks only; does not run the game or browser."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit,unquote
from PIL import Image
import hashlib,json,re
B=Path(__file__).resolve().parent
class Page(HTMLParser):
    def __init__(self,path):
        super().__init__();self.ids=[];self.refs=[];self.feed(path.read_text(encoding='utf-8-sig'))
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id' in a:self.ids.append(a['id'])
        self.refs.extend(a[k] for k in ['src','href'] if k in a)
errors=[]
def check(ok,msg):
    if not ok:errors.append(msg)
def read(path):return json.loads(path.read_text(encoding='utf-8'))
plan=read(B/'plan.json');comp=read(B/'companions-plan.json')
refs=0
for file in [B/'index.html',B.parent/'index.html']:
    page=Page(file);check(len(page.ids)==len(set(page.ids)),str(file)+': duplicate IDs')
    check(not re.search(r'__[A-Z_]+__',file.read_text(encoding='utf-8')),'unexpanded tokens')
    for ref in page.refs:
        u=urlsplit(ref)
        if u.scheme:continue
        p=(file.parent/unquote(u.path)).resolve() if u.path else file
        if p==B/'validation.json':continue
        refs+=1;check(p.is_file(),'missing '+str(p))
        if p.is_file() and u.fragment and p.suffix=='.html':check(u.fragment in Page(p).ids,'missing fragment '+ref)
for ref in re.findall(r'url\([\'"]?([^)\'\"]+)',(B/'page.css').read_text(encoding='utf-8')):check((B/ref).is_file(),'missing CSS '+ref)
if plan['status']=='unity-implementation-in-progress':
    pass
else:
    # A published implementation must be backed by the completed external QA
    # and release records. This static document check does not create a QA pass.
    root=B.parents[3]
    integration=read(B.parents[2]/'Production/dragon-v5-integration.json')
    check(plan['status'].startswith('v0.9.4 Unity1~20 적용') and integration.get('status')=='saved-assets-applied-windows-packaged',
          'published implementation requires a saved, packaged integration record')
    check(integration.get('playableStages')==[1,20] and integration.get('qaStatus')=='CHANGED_SCOPE_PASS_WITH_LIMITS',
          'published scope must remain 1–20 with bounded QA')
    publication=integration.get('documentationPublication',{})
    check(bool(publication.get('limits')) and bool(publication.get('sourceEvidenceHashes')),'missing bounded publication evidence')
    for path,digest in publication.get('sourceEvidenceHashes',{}).items():
        source=root/path
        check(source.is_file() and hashlib.sha256(source.read_bytes()).hexdigest()==digest,'publication evidence changed: '+path)
dialogue=plan['ui']['dialogue']
check(dialogue['playerPortrait']==[48,384,144,144] and dialogue['npcPortrait']==[1056,384,144,144],'speaker-specific portraits')
check(dialogue['playerNameplate']==[216,440,176,56] and dialogue['npcNameplate']==[48,440,176,56],'speaker-specific nameplates')
check(dialogue['panel']==[24,488,1232,208],'dialogue panel')
check([n['joinAfterStage'] for n in comp['companions']]==[2,12,22,32,42],'NPC route')
check(len({n['id'] for n in comp['companions']})==5,'NPC IDs')
check(sum(len(n['dialogue']) for n in comp['companions'])==20,'encounter pages')
check(comp['recovery']['noFullHealOnBoss'],'boss heal proposal')
check(comp['formation']['initialSlots']==1 and comp['formation']['maxSlots']==3,'party slots')
check([x['gemCost'] for x in comp['formation']['upgrades']]==[4,8],'slot prices')
check(plan['comboTutorial']['fixedSequence'][1]['impactMs']-plan['comboTutorial']['fixedSequence'][0]['impactMs']==750,'combo interval')
check(sum(comp['companionGrowth']['costByTargetLevel'][2:11])==212,'Lv1 to10 price')
for n in comp['companions']:
    check(abs(n['attack']['baseDamageToPlayerBaseAttack']/(n['attack']['periodMs']/1000)-.04)<1e-9,'base DPS differs '+n['id'])
portraits=[]
portrait_paths={pid:B/'portraits'/f'{pid}.png' for pid in ['on','dam','rin','tori','rina','barun','doto','gib','unknown']}
portrait_paths['momo']=B.parent/'Production/Companions-v5/portraits/momo.png'
for pid,path in portrait_paths.items():
    im=Image.open(path).convert('RGBA');colors={p[:3] for p in im.get_flattened_data() if p[3]};alpha=set(im.getchannel('A').get_flattened_data())
    check(im.size==(24,24) and len(colors)<=8 and alpha<={0,255},'portrait spec '+pid)
    portraits.append({'id':pid,'size':list(im.size),'colors':len(colors),'binaryAlpha':alpha<={0,255}})
for name in ['dialogue','dialogue-player','inn','growth']:
    for version in ['draft','wireframe']:check(Image.open(B/f'{name}-{version}.png').size==(1280,720),'wireframe size '+name)
check(Image.open(B/'rhythm-draft.png').size==(1280,720),'rhythm diagram size')
result={'date':'2026-10-25','status':'pass' if not errors else 'fail','scope':'static document links, published evidence hashes, speaker-specific layout contracts, portrait pixel/alpha metadata; no Unity execution by this validator','localReferencesChecked':refs,'portraits':portraits,'encounters':[2,12,22,32,42],'dialoguePages':20,'implementationStatus':plan['status'],'errors':errors,'notPerformed':['browser layout/interaction check','Unity runtime QA or build execution by this static validator; see linked independent QA report','mobile safe area/touch test']}
(B/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'status':result['status'],'references':refs,'portraits':len(portraits),'errors':errors},ensure_ascii=False))
if errors:raise SystemExit(1)
