"""Promote the four reviewed retries, retaining full first-pass provenance."""
import json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1]
ART=ROOT/'Planning/Story/Monster-v86/outskirts'
jobs=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))
reviews=json.loads((ART/'review-decisions.json').read_text(encoding='utf-8'))
retries=json.loads((ART/'retry-jobs.json').read_text(encoding='utf-8'))
byid={r['id']:r for r in jobs['jobs']}
for retry in retries:
    row=byid[retry['id']];motion=retry['motion']
    if row[motion+'Job']!=retry['job']:
        row[motion+'Rejected']={'job':row[motion+'Job'],'directory':row.get(motion+'Dir',motion),'request':row.get(motion+'Request'), 'reason':retry['reason']}
    row[motion+'Job']=retry['job'];row[motion+'Dir']=retry['directory']
    row[motion+'OutputFrames']=retry['outputFrames'];row[motion+'Request']=retry['request']
for key in ('lynx','great_stag'):
    reviews[key]['idle']={'status':'approved','sourceFrames':[0,1,2,3,4,3,2,1],'holdsMs':[200]*8,'qa':'Second generated take: all five raw frames inspected. Paw pixels and ground baseline identical in every frame. Subtle ear/tail movement with solid antlers/head shape and fixed body size. Unique pixel poses: '+('5' if key=='lynx' else '4')+'. No white outlines, fragments, or effects. Ping-pong loop 1.6s. First take preserved but not imported.'}
reviews['red_fox']['attack']={'status':'approved','sourceFrames':list(range(17)),'holdsMs':[50]*8+[60]+[30]*8,'contactFrame':8,'contactPoint':{'x':32,'y':55},'contactTimeMs':400,'durationMs':700,'qa':'Second generated take: all 17 frames inspected. Clear open-jaw preparation4-7 followed by closed-mouth bite contact8; clean recovery9-16. Every visible white/cream pixel belongs to teeth or fur; no detached effect. Root cross-review accepted. First take retained but excluded.'}
reviews['wild_boar']['attack']={'status':'approved','sourceFrames':list(range(17)),'holdsMs':[50]*6+[25]*4+[60]+[40]*6,'contactFrame':10,'contactPoint':{'x':28,'y':61},'contactTimeMs':400,'durationMs':700,'qa':'Second generated take: all 17 frames inspected. Lowered head and shoulder compression7-9, short forward snout/tusk drive at10-12, recover13-16. No detached white impact or speed lines, tusks remain attached. Root cross-review accepted. First take retained but excluded.'}
jobs['completedDate']='2026-10-24'
jobs['finalReview']='All bases and all source/selected motion frames reviewed; root cross-reviewed all four retries and contact-frame selection. No more generation required.'
for name,data in [('jobs.json',jobs),('review-decisions.json',reviews)]:
    (ART/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Finalized six actors and 18 approved motions; raw rejected takes preserved.')
