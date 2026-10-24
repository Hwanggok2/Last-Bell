"""Package reviewed PixelLab companion sprites; no Unity edits or authored pixels."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent
NAMES={'tori':'토리','barun':'바룬','doto':'도토','rina':'리나','momo':'모모'}
PIVOT={'tori':16,'barun':11,'doto':16,'rina':14,'momo':16}
CONTACT={'tori':3,'barun':4,'doto':2,'rina':2,'momo':3}
WEAPON={'tori':'bow-arrow','barun':'greatsword','doto':'hammer','rina':'wind-bubble','momo':'seed-pouch'}
ORIGINS={'tori':[23,17],'barun':[39,24],'doto':[39,25],'rina':[29,13],'momo':[25,17]}
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def measure(p):
 im=Image.open(p).convert('RGBA');px=list(im.getdata());cols=sorted({'#%02X%02X%02X'%v[:3] for v in px if v[3]});alpha=sorted({v[3] for v in px});bounds=list(im.getbbox())
 assert alpha==[0,255] and len(cols)<=8,(p,cols,alpha)
 return dict(file=p.relative_to(B).as_posix(),size=list(im.size),boundsTopLeft=bounds,palette=cols,colorCount=len(cols),alphaValues=alpha,sha256=sha(p))
def main():
 actors=[];all_frames=[];motion=json.loads((B/'requests/motion.json').read_text());canonical=json.loads((B/'requests/canonical.json').read_text());wide=json.loads((B/'requests/attack-wide.json').read_text());quant=json.loads((B/'requests/quantize.json').read_text());wideq=json.loads((B/'requests/attack-wide-quantize.json').read_text())
 run=json.loads((B/'requests/run.json').read_text());runq=json.loads((B/'requests/run-quantize.json').read_text())
 old_hash=json.loads((B/'requests/run-existing-frames-sha256.json').read_text())
 assert all(sha(B/name)==value for name,value in old_hash.items()),'Existing Idle/Attack frames changed'
 review=Image.new('RGB',(8*196,15*154),'#DBCFAE');draw=ImageDraw.Draw(review)
 for actor_index,(id,name) in enumerate(NAMES.items()):
  clips=[]
  for clip_index,clip in enumerate(['Idle','Attack','Run']):
   use_wide=clip in ['Attack','Run'] and id in ['barun','doto'];folder='AttackWide' if use_wide and clip=='Attack' else clip;paths=sorted((B/id/'frames'/folder).glob('*.png'));expected=4 if clip=='Idle' else (6 if id=='doto' and clip=='Attack' else 8)
   if id=='doto' and clip=='Attack':paths=paths[:6] # Last two generated frames repeat the recovery pose exactly.
   assert len(paths)==expected,(id,clip,len(paths));measurements=[measure(p) for p in paths]
   assert len({Image.open(p).convert('RGBA').tobytes() for p in paths})==expected,(id,clip,'Repeated poses')
   assert all((27<=row['boundsTopLeft'][3]<=29) if clip=='Run' else row['boundsTopLeft'][3]==29 for row in measurements),(id,clip,'Foot baseline drift')
   assert all(0<row['boundsTopLeft'][0] and row['boundsTopLeft'][2]<row['size'][0] for row in measurements),(id,clip,'Clipped side edge')
   # Contact time is a runtime animation event, independent of attack cadence.
   k=CONTACT[id];durations=[125]*4 if clip=='Idle' else ([62,63]*4 if clip=='Run' else [180//k]*k+[(600-180)//(expected-k)]*(expected-k))
   assert sum(durations)==(500 if clip in ['Idle','Run'] else 600)
   pivot=[PIVOT[id]+(8 if use_wide else 0),29]
   source=next(r for r in (run if clip=='Run' else wide if use_wide else motion) if r['id']==id and (use_wide or r['motion']==clip.lower()))
   q=next(r for r in (runq if clip=='Run' else wideq if use_wide else quant) if r['id']==id)
   entry=dict(name=clip,frames=[p.relative_to(B).as_posix() for p in paths],frameCount=expected,canvas=measurements[0]['size'],pivotTopLeft=pivot,pivotUnityNormalized=[pivot[0]/measurements[0]['size'][0],1-pivot[1]/32],durationMs=durations,totalMs=sum(durations),nominalFps=8 if clip=='Idle' else (16 if clip=='Run' else round(1000*expected/600,5)),loop=clip in ['Idle','Run'],sourceGenerationJob=source['jobId'],sourceFrameIndices=list(range(1,expected+1)),quantizeJob=q['jobId'],contactFrame=CONTACT[id] if clip=='Attack' else None,contactMs=180 if clip=='Attack' else None)
   clips.append(entry);all_frames.extend(measurements)
   for n,p in enumerate(paths):
    im=Image.open(p).convert('RGBA');big=im.resize((im.width*4,128),Image.Resampling.NEAREST);x=n*196;y=(actor_index*3+clip_index)*154;review.paste(big,(x,y+20),big);draw.text((x,y),f'{id} {clip} {n}'+(' CONTACT' if clip=='Attack' and n==CONTACT[id] else ''),fill='#202D38');draw.line((x,y+20+116,x+192,y+20+116),fill='#609287')
  actors.append(dict(id='companion_'+id,npcId=id,name=name,canonical=f'{id}/canonical.png',canonicalGenerationJob=next(r for r in canonical if r['id']==id)['result']['structuredContent']['job_id'],nativePixelHeight=32,canonicalPivotTopLeft=[PIVOT[id],29],referenceBodyHeightPx=measure(B/id/'canonical.png')['boundsTopLeft'][3]-measure(B/id/'canonical.png')['boundsTopLeft'][1],recommendedIntegerScale=4,relativeToPlayer='Luka uses7x; companions4x preserve smaller silhouettes',facing='east',weapon=WEAPON[id],contactOriginTopLeft=ORIGINS[id],projectileInSprite=False,clips=clips))
 portrait=measure(B/'portraits/momo.png');assert portrait['size']==[24,24]
 manifest=dict(schemaVersion=1,revision='Dragon-Companions-v5-run',status='source-art-reviewed-ready-for-root-import',scope='Source art only. Root owns Unity import, runtime, catalog and dialogue binding.',actors=actors,portrait=dict(id='momo',**portrait,integerScale=6,background='none'),coordinates='All coordinates top-left pixels. Ground is immediately below pixel row28, at y29. Unity pivot y=1-29/height.',rendering=dict(filter='Point',compression='None',mipmaps=False,alpha='binary',maxColorsPerActor=8),timingContract=dict(attackWindupMs=180,attackTotalMs=600,contactOnce=True,phase='contactFrame entry at180ms; frame-specific durations are authoritative',cadence='Owned by Adventure-v5 companions-plan.json. Do not add animation duration to cadence.'),review='review.png',animatedReview='review.html',validation='validation.json',license='license.json',rejectedSources=['barun/frames/Attack: cropped32pxweapon; use AttackWide','doto/frames/Attack: cropped32pxweapon; use AttackWide','tori/raw/RunRejected: first run bow clipped right edge; replaced with tighter in-place gait'],notes=['Every Idle has4 distinct generated poses; Attacks have8 distinct generated poses; Doto has6, with two identical recovery holds removed.','Barun and Doto48x32 Attack/Run have8px extra left/right transparent margin, with unchanged native pixel scale.','Run has8 distinct generated in-place poses per actor,500ms loop/16fps, fixed existing foot pivot and4xdisplay.','Small weapon motion streaks remain in melee swing frames; detached ranged projectiles removed.','Companion root x remains fixed. Groundpivot y29; Run has1–2px flight intervals while contactfeet land at y29. Weight shifts may change individual boot pixels.'])
 (B/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');review.save(B/'review.png')
 validation=dict(status='passed',actors=5,idleFrames=20,attackFrames=38,runFrames=40,totalFrames=98,idleAttackHashesUnchanged=True,distinctPosesPerClip=True,binaryAlpha=True,maxColors=8,constantGroundY=29,noSideEdgeClipping=True,manualReview=['Canonical identity and every source frame inspected at4x.','32px melee weapon clipping rejected;48x32 rework reviewed.','Momo24px portrait reviewed.','Every generated run frame reviewed for arm/leg gait, costume/weapon continuity and fixed pivot. First Tori run rejected and replaced due to bow clipping.','Runtime projectiles removed from four ranged frames via guarded PixelLab edits.'],frames=all_frames,portrait=portrait)
 (B/'validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 data=json.dumps(manifest,ensure_ascii=False)
 html='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>동료 모션 원본 검수</title><style>body{background:#e8ddbd;color:#202d38;font:16px system-ui;margin:24px}h1{font-size:24px}main{display:grid;grid-template-columns:repeat(5,minmax(180px,1fr));gap:16px}.card{padding:16px;background:#d2d7ad;border:3px solid #355d42}canvas{width:192px;height:128px;image-rendering:pixelated;display:block;background:#e2d7ad}button{font:inherit;margin:3px;padding:5px 9px}.note{font-size:13px;line-height:1.7}a{color:#287780}</style><h1>동료 · Idle / 공격 / 달리기</h1><p>대기8fps · 달리기16fps/500ms · 공격600ms / 판정180ms · 플레이어보다작은4배도트</p><main></main><p class="note">바룬과도토는무기가잘리지않도록공격캔버스에투명여백만추가했습니다. 발접지 y29. 투사체는게임에서한번발생시킵니다.</p><a href="review.png">전체프레임</a> · <a href="manifest.json">임포트명세</a><script>const DATA=__DATA__;const main=document.querySelector('main');for(const a of DATA.actors){const card=document.createElement('section');card.className='card';card.innerHTML=`<b>${a.name}</b><canvas width="192" height="128"></canvas><div><button data-m="Idle">대기</button><button data-m="Attack">공격</button><button data-m="Run">달리기</button></div><small></small>`;main.append(card);const cvs=card.querySelector('canvas'),ctx=cvs.getContext('2d');ctx.imageSmoothingEnabled=false;let clip=a.clips[0],start=performance.now();const images=new Map();for(const c of a.clips)for(const file of c.frames){const i=new Image;i.src=file;images.set(file,i)}for(const b of card.querySelectorAll('button'))b.onclick=()=>{clip=a.clips.find(c=>c.name===b.dataset.m);start=performance.now()};function paint(t){const elapsed=(t-start)% (clip.totalMs+(clip.loop?0:500));let frame=clip.frames.length-1,at=0;for(let n=0;n<clip.frames.length;n++){at+=clip.durationMs[n];if(elapsed<at){frame=n;break}}ctx.clearRect(0,0,192,128);ctx.fillStyle='#609287';ctx.fillRect(0,116,192,2);const i=images.get(clip.frames[frame]);if(i.complete)ctx.drawImage(i,96-clip.pivotTopLeft[0]*4,116-clip.pivotTopLeft[1]*4,i.width*4,i.height*4);card.querySelector('small').textContent=clip.name+' '+frame+(frame===clip.contactFrame?' · 판정':'');requestAnimationFrame(paint)}requestAnimationFrame(paint)}</script></html>'''.replace('__DATA__',data)
 (B/'review.html').write_text(html,encoding='utf-8');print('Validated5actors,98uniqueclipframes,24pxMomo portrait; sourcepackage ready.')
if __name__=='__main__':main()
