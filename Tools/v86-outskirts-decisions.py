"""Record manually inspected choices; never synthesize or edit sprite pixels."""
import json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1];ART=ROOT/'Planning/Story/Monster-v86/outskirts'
jobs=json.loads((ART/'jobs.json').read_text(encoding='utf-8'))
path=ART/'review-decisions.json'
reviews=json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}
for row in jobs['jobs']:
    reviews.setdefault(row['id'],{'base':'Approved native64 candidate beside direct original hero, complete outline and anatomy. Transparent padding only.','idle':{'status':'pending'},'attack':{'status':'pending'},'stun':{'status':'pending'},'hit':'Static base with existing runtime white flash. No death clip.'})
idle={
 'red_fox':([0,1,2,4,5,6,7,8],'All selected feet pixels match. Gentle tail/ear motion only; source 3 tiny mouth extension excluded for consistency.'),
 'wild_boar':([0,4,5,6,8,6,5,4],'All selected feet pixels match. Small chest/ear/tail motion. Stray flecks in original sources 1,2,3,7 excluded.'),
 'mountain_goat':([0,3,4,6,7,6,4,3],'All selected feet pixels match. Gentle tail motion, fixed horn shape and body size. Sources 2,5 had a changed hoof pixel; source 1 back-outline noise excluded.')
}
for key,(indices,note) in idle.items():reviews[key]['idle']={'status':'approved','sourceFrames':indices,'holdsMs':[200]*len(indices),'qa':note}
reviews['badger']['attack']={'status':'approved','sourceFrames':list(range(9))+[13,14,15,16],'holdsMs':[50]*8+[60]*5,'contactFrame':8,'contactPoint':{'x':96,'y':69},'contactTimeMs':400,'durationMs':700,'qa':'Reviewed all 17 raw frames. Raised forepaw wind-up, short low forward rake at source8, recover via13-16. Reject9-12: generated detached white line/projectile, already covered by runtime effects. No other anatomy/clipping defects in selected frames.'}
reviews['badger']['idle']={'status':'approved','sourceFrames':[0,3,7,8,7,3],'holdsMs':[300,250,250,300,250,250],'qa':'Four clean native poses in a 1.6s loop. All paws exactly fixed. Sources1,2,4,5,6 with contour lines or detached pixels excluded.'}
reviews['lynx']['attack']={'status':'approved','sourceFrames':[0,1,2,3,4,9,10,11,12,13,14,15,16],'holdsMs':[80]*5+[60]+[35]*2+[34]*5,'contactFrame':5,'contactPoint':{'x':33,'y':75},'contactTimeMs':400,'durationMs':700,'qa':'Retain compression and raised body wind-up0-4, contact on extended front paw/claws at source9, recover10-16. Exclude5-8 with detached white arc. Physical small claw tips at contact are attached to the paw.'}
reviews['mountain_goat']['attack']={'status':'approved','sourceFrames':list(range(9))+[12,13,14,15,16],'holdsMs':[50]*8+[60]+[48]*5,'contactFrame':8,'contactPoint':{'x':95,'y':47},'contactTimeMs':400,'durationMs':700,'qa':'Original horn-down head contact at source8, then recover12-16. Exclude9-11 generated white ring and debris. Horn count and shape retained; no silhouette clipping.'}
reviews['great_stag']['attack']={'status':'approved','sourceFrames':[0,1,2,3]+list(range(5,17)),'holdsMs':[100]*4+[60]+[22]*9+[21]*2,'contactFrame':4,'contactPoint':{'x':23,'y':42},'contactTimeMs':400,'durationMs':700,'qa':'One short antler/body thrust with clean source5 contact. Exclude source4 white speed streak. Retain all other wind-up and recovery frames; no extra antler branches or clipping.'}
for key in reviews:
    reviews[key]['stun']={'status':'approved','sourceFrames':list(range(9)),'holdsMs':[170]*9,'qa':'All nine original frames visually reviewed. Lowered head, drooping ears/shoulders distinguish exhausted state from alert Idle. Tiny weak movement on identical foot-band pixels throughout; no death collapse or effects.'}
path.write_text(json.dumps(reviews,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Saved inspected selection decisions.')
