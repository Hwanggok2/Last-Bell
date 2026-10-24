"""One distinct enemy/design per playable stage; preserve all combat/economy values."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'GameData/mvp-catalog.json'
data=json.loads(path.read_text(encoding='utf-8-sig'))
before=json.loads((ROOT/'Planning/Evidence/Feedback-v86/catalog-before.json').read_text(encoding='utf-8-sig'))
new=[('red_fox','붉은 여우'),('wild_boar','성난 멧돼지'),('badger','굴 오소리'),('lynx','숲 스라소니'),('mountain_goat','바위 산양'),('great_stag','큰뿔 사슴')]
for id,name in new:
    if not any(e['id']==id for e in data['enemies']):data['enemies'].append(dict(id=id,displayName=name,artId=id))
roster=['wild_dog','grey_wolf','brown_bear',*[id for id,_ in new],'bell_knight','sewer_rat','drowned_resident','water_wraith','ash_wisp','chain_thrall','hook_hound','chain_puppet','cracked_blade','chain_dancer','chain_jailer']
for stage,id in zip(data['stages'],roster):stage['enemyId']=id
data['dataVersion']='mvp1.13.0'
data['sourcePlanningVersion']='feedback-v8.6'
assert len(set(roster))==20
assert len({next(e['artId'] for e in data['enemies'] if e['id']==id) for id in roster})==20
for old,new_stage in zip(before['stages'],data['stages']):
    assert {k:v for k,v in old.items() if k!='enemyId'}=={k:v for k,v in new_stage.items() if k!='enemyId'}
for key in ['combat','growth','economy','relicRules','characters','relics','transitions','stories','onboarding']:assert data[key]==before[key],key
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('20 unique enemies and art IDs; stage balance, save IDs and timings preserved')
