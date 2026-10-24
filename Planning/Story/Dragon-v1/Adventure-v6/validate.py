"""Static planning artifact checks; this does not test Unity or run the page."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import unquote,urlsplit
import hashlib,json,re,xml.etree.ElementTree as ET
from datetime import datetime,timezone
from PIL import Image
B=Path(__file__).resolve().parent
failures=[];checks=[]
def check(ok,label,detail=None):
 checks.append({'name':label,'pass':bool(ok),**({'detail':detail} if detail is not None else {})})
 if not ok:failures.append(label)
class Page(HTMLParser):
 def __init__(self):super().__init__();self.links=[];self.ids=[]
 def handle_starttag(self,tag,attrs):
  attrs=dict(attrs)
  if 'id' in attrs:self.ids.append(attrs['id'])
  for k in ('href','src'):
   if k in attrs:self.links.append(attrs[k])
plan=json.loads((B/'plan.json').read_text(encoding='utf-8'))
page=(B/'index.html').read_text(encoding='utf-8');parser=Page();parser.feed(page)
check(not re.search(r'__[A-Z_]+__',page),'all template tokens expanded')
check(len(parser.ids)==len(set(parser.ids)),'unique HTML IDs')
bad=[]
for value in parser.links+plan['sources']:
 parts=urlsplit(value)
 if parts.scheme or value.startswith('data:'):continue
 if not parts.path:
  if parts.fragment and parts.fragment not in parser.ids:bad.append(value)
 else:
  target=(B/unquote(parts.path)).resolve()
  if target.name!='validation.json' and not target.is_file():bad.append(value)
check(not bad,'local HTML and source links resolve',bad)
assetbad=[]
for file in [B/'page.css',*B.glob('*.svg')]:
 for url in re.findall(r'url\([\"\']?([^\"\')]+)',file.read_text(encoding='utf-8')):
  if not urlsplit(url).scheme and not (file.parent/url).resolve().is_file():assetbad.append(str(file.name)+':'+url)
check(not assetbad,'CSS and SVG font assets resolve',assetbad)
for name in ('growth-layout','companions-layout','hud-spec'):
 svg=ET.parse(B/(name+'.svg')).getroot()
 check(svg.attrib.get('viewBox')=='0 0 1280 720' and Image.open(B/(name+'.png')).size==(1280,720),name+' SVG/PNG at1280×720')
g=plan['ui']['growth'];c=plan['ui']['companions'];h=plan['ui']['pixelHud']
check(g['companionsButton']==[1096,8,160,96] and g['statsPanel']==[20,112,320,480] and g['upgrade']==[560,608,176,96],'restored growth layout and upper-right companions')
check(c['detailPanel']==[424,112,832,584] and c['skillChanceRelative']==[32,302,768,36],'companion current→next layout')
buttons=[g[k] for k in ('back','companionsButton','relicOrExchange','upgrade','stage')]
check(all(w>=96 and ht>=96 and x>=0 and y>=0 and x+w<=1280 and y+ht<=720 for x,y,w,ht in buttons),'1280 minimum96 /640 minimum48 touch bounds')
def overlap(a,b):return a[0]<b[0]+b[2] and b[0]<a[0]+a[2] and a[1]<b[1]+b[3] and b[1]<a[1]+a[3]
check(not any(overlap(a,b) for i,a in enumerate(buttons) for b in buttons[i+1:]),'primary growth buttons do not overlap')
check(h['playerHpSize']==[208,44] and h['enemyHpSize']==[224,44] and h['bossHpSize']==[560,44] and h['bossHpCenter']==[640,38] and h['bossNameY']==84 and h['bossNameCenter']==[900,84] and h['bossNameSize']==[440,40],'root HP dimensions and rhythm-bar clearance')
check(h['damageBitmap']==[5,7] and [h['normalCellPx'],h['criticalCellPx'],h['damageLifetimeMs'],h['damageLanes']]==[6,8,850,3],'root damage grid/lifetime/lanes')
p=plan['playerSkill'];growth=plan['companionGrowth']
check(p['cooldownMs']==20000 and p['contactDelayMs']==200 and p['attackIndependent'] and p['dodgeIndependent'],'independent player skill200ms/20s')
check([x['damagePercent'] for x in p['skills']]==[160,115,125],'player multipliers match Client contract')
catalog=json.loads((B.parents[3]/'GameData/mvp-catalog.json').read_text(encoding='utf-8'))
byid={x['characterId']:x for x in catalog['skills']}
check(catalog['dataVersion']==plan['targetDataVersion'] and all(x['skill']==byid[x['id']]['displayName'] and x['skillId']==byid[x['id']]['id'] and x['damagePercent']==byid[x['id']]['damagePercent'] for x in p['skills']),'final Client catalog skill names/stableIDs/data version')
check([x['skillDamagePercent'] for x in plan['companions']]==[180,170,200,180,170],'companion skill multipliers match Client contract')
chance=lambda lvl:min(growth['chanceCapPercent'],growth['chanceBasePercent']+(lvl-1)*growth['chancePerLevelPercentagePoints'])
ratio=lambda lvl:max(growth['periodFloorPercent'],100-(lvl-1)*growth['periodReductionPerLevelPercent'])
check([chance(lvl) for lvl in (1,10,20,30)]==[12,21,31,41] and [ratio(lvl) for lvl in (1,10,20,30)]==[100,91,81,71],'growth milestone table matches structured formulas')
check(growth['maxExpectedPartyDpsPercentOfBaseAttackPerSecond']==24 and growth['gameplayHitsPerReservation']==1,'24% expectedDPS cap /one replacement hit')
trail=h['healthTrail']
check(trail['holdMs']==120 and trail['linearLerpMs']==550 and trail['actualColor']=='#51A441' and trail['trailColor']=='#EF8F35' and not trail['aggregateHits'],'one-hit HP trail timing/colors / no aggregation')
if plan['status']=='applying-validating':
 check(plan['qa']['status']=='pending-root-integration' and plan['qa']['captures']==[],'pending integration status / no claimed final QA captures')
else:
 check(plan['status']=='applied' and plan['qa']['status']=='bounded-observation-complete' and bool(plan['qa']['captures']) and
       all((B/x).is_file() for x in plan['qa']['captures']) and (B/plan['qa']['report']).is_file(),
       'published status has actual captures and a bounded QA report')
baseline=json.loads((B/'v5-source-hashes.json').read_text(encoding='utf-8'))
changed=[]
for relative,digest in baseline.items():
 path=B.parent/'Adventure-v5'/relative
 if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=digest:changed.append(relative)
check(not changed,'existing v5 source and evidence hashes unchanged',changed)
result={'status':'pass' if not failures else 'fail','scope':'planning-static-only; no browser or Unity execution','checkedUtc':datetime.now(timezone.utc).isoformat(),'checkCount':len(checks),'checks':checks,'failures':failures,'visualReview':'PNG diagrams must also be inspected by the author; static checks do not prove game rendering.'}
(B/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'status':result['status'],'checks':len(checks),'failures':failures},ensure_ascii=False))
raise SystemExit(bool(failures))
