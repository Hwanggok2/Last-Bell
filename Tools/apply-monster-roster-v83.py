import json,pathlib
root=pathlib.Path(__file__).resolve().parents[1]
path=root/'GameData/mvp-catalog.json'
d=json.loads(path.read_text(encoding='utf-8'))
backup=root/'Planning/Evidence/Feedback-v83/before/catalog.json'
if not backup.exists(): backup.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
names={'wild_dog':'들개','grey_wolf':'회색 늑대','brown_bear':'불곰','sewer_rat':'하수구 거대쥐','drowned_resident':'익사한 주민','water_wraith':'물안개 망령'}
for key,name in names.items():
    if not any(e['id']==key for e in d['enemies']):d['enemies'].append({'id':key,'displayName':name,'artId':key})
roster=['wild_dog','grey_wolf','brown_bear','wild_dog','brown_bear','grey_wolf','wild_dog','brown_bear','grey_wolf','bell_knight',
        'sewer_rat','drowned_resident','water_wraith','sewer_rat','drowned_resident','water_wraith','sewer_rat','drowned_resident','water_wraith','chain_jailer']
pools={'wild_dog':['low_sweep','thrust'],'grey_wolf':['thrust','low_sweep'],'brown_bear':['high_sweep','low_sweep'],
       'sewer_rat':['low_sweep','thrust'],'drowned_resident':['high_sweep','thrust'],'water_wraith':['high_sweep','low_sweep','thrust']}
direction={'low_sweep':'up','high_sweep':'parry','thrust':'back'}
for stage,enemy in zip(d['stages'],roster):
    stage['enemyId']=enemy
    if enemy not in pools:continue
    pool=pools[enemy]
    stage['randomPatterns']=pool[:]
    for i,step in enumerate(stage['patternSteps']):
        if step['danger'] not in pool:
            step['danger']=pool[i%len(pool)];step['requiredDodge']=direction[step['danger']]
    if stage['teachDodge']:stage['teachDodge']=stage['patternSteps'][0]['requiredDodge']
d['dataVersion']='mvp1.11.0';d['sourcePlanningVersion']='feedback-v8.3'
path.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Updated first 20 stage identities; HP, damage, timings, gold and boss IDs preserved.')
