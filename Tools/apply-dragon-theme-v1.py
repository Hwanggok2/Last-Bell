"""Change approved display/art bindings only; preserve balance, IDs and saved progress."""
from pathlib import Path
import json
import hashlib

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'GameData/mvp-catalog.json'
story=json.loads((ROOT/'Planning/Story/Dragon-v1/story.json').read_text(encoding='utf-8'))
data=json.loads(source.read_text(encoding='utf-8-sig'))
protected=['combat','growth','economy','relicRules','stages','transitions','mvpLastStage','boundaryStage']
before={k:data[k] for k in protected}
roster={e['id']:e for e in data['enemies']}
mapping=[]
for stage,planned in zip(data['stages'][:10],story['stages'][:10]):
    enemy=roster[stage['enemyId']]
    enemy['displayName']=planned['name']
    enemy['artId']='dragon_'+planned['enemyId']
    mapping.append({'stage':stage['number'],'stableEnemyId':enemy['id'],'displayName':enemy['displayName'],'artId':enemy['artId']})
for c in data['characters']:
    if c['id']=='on':c.update(displayName='온 · 검사',artId='dragon_hero_on')
    elif c['id']=='dam':c['displayName']='담 · 수호기사'
    elif c['id']=='rin':c['displayName']='린 · 마법사'
for r,name in zip(data['relics'],['전사의 반지','생명의 목걸이','바위 부적','바람 깃털','사냥꾼 부적','수호 망토','행운 주머니','튼튼한 팔찌','집중의 반지','용기의 문장']):r['displayName']=name
data['onboarding']['prologueTitle']='용 잡으러 갑니다'
data['onboarding']['prologueParagraphs']=story['opening']['sentences']
data['onboarding']['comboHint']='적들은 가끔 연속 공격을 합니다.\n방패 표식을 살피세요!'
# Keep the existing three-page dialogue protocol by splitting the approved first line.
data['stories'][0]['lines']=[{'speaker':'토리','text':'용은 저 봉우리에 있어.','responses':[]},{'speaker':'토리','text':'숲길로 가면 빨라.','responses':[]},{'speaker':'온','text':'좋아. 아직 해도 높네.','responses':[]}]
data['dataVersion']='mvp1.14.0'
data['sourcePlanningVersion']='dragon-v1-pixel-ui-v4'
assert all(before[k]==data[k] for k in protected),'Theme migration changed gameplay values'
source.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
record={'version':'dragon-v1','scope':'1–10 new art/theme; existing 11–20 content remains available','mapping':mapping,'stableIdsPreserved':True,'preservedDomains':protected,'preservedSha256':hashlib.sha256(json.dumps(before,sort_keys=True).encode()).hexdigest(),'storyAdaptation':'Stage10 first approved Tori sentence split into two pages, same wording. Existing three-page protocol preserved.'}
output=ROOT/'Planning/Production/dragon-v1-data-bindings.json'
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Updated display/art bindings; gameplay values and stable IDs unchanged.')
