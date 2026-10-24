'use strict';
(() => {
  const plan=window.ROUTE_PLAN,core=window.RoutePreviewCore,$=id=>document.getElementById(id);
  const audio=window.LastBellAudio;
  const canvas=$('preview'),ctx=canvas.getContext('2d'),W=960,H=540;
  const gold='#e8bd70',cream='#f2dfb3',mint='#a6e2d3';
  const lerp=(a,b,p)=>a+(b-a)*p,clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v));
  const ease=p=>p*p*(3-2*p),images={};
  const imageBase='../../Client/Assets/Game/Art/Presentation/Clips/';
  const paths={idle:'hero-v5-idle/01.png',up:'hero-dodge-up/04.png',left:'hero-dodge-back/08.png',right:'hero-dodge-front/04.png',down:'hero-v6-slash-up/04.png'};
  for(let i=0;i<8;i++)paths['run'+i]='hero-v12-fast-run/'+String(i).padStart(2,'0')+'.png';
  for(const [key,path] of Object.entries(paths)){const img=new Image();img.onload=()=>{images[key]=img;};img.src=imageBase+path;}
  let mode='prologue',last=performance.now(),now=last;
  let prologueTime=0,prologuePlaying=false,stanza=0,lastSecond=-1;
  let stage=1,fromStage=1,travel=1,routeIntroUntil=0,routeTutorialPending=false,hintSeenInPreview=false;
  let routeMotionTime=0,routeHitAge=-1;
  let routeSoundCycle=-1,lastFootstep=-1;
  const previewEnemyId=s=>{const id=s.candidateEnemyId||s.enemyId;return id==='bell_knight'?'bell_knight_concept':id;};
  const stageVisible=()=>{const r=canvas.getBoundingClientRect();return r.bottom>0&&r.top<innerHeight;};
  const previewSequences=plan.tutorial5.previewSequences;
  let phase='explain',running=false,trial=new core.Trial(),elapsed=0,reported=0,sequenceIndex=0;
  const reduced=matchMedia('(prefers-reduced-motion: reduce)').matches;
  $('reading-mode').checked=reduced;
  function txt(text,x,y,size=22,color=cream,align='left'){
    ctx.fillStyle=color;ctx.font=`${size}px Nanum, sans-serif`;ctx.textAlign=align;ctx.textBaseline='middle';ctx.fillText(text,x,y);
  }
  function rect(x,y,w,h,color){ctx.fillStyle=color;ctx.fillRect(Math.round(x),Math.round(y),Math.round(w),Math.round(h));}
  function polygon(points,fill,stroke){ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.closePath();if(fill){ctx.fillStyle=fill;ctx.fill();}if(stroke){ctx.strokeStyle=stroke;ctx.stroke();}}
  function line(x1,y1,x2,y2,color,width=3){ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();}
  function bell(x,y,active=true,scale=1){ctx.save();ctx.translate(x,y);ctx.scale(scale,scale);ctx.lineWidth=2;polygon([[-13,14],[-9,8],[-9,-7],[-5,-14],[5,-14],[9,-7],[9,8],[13,14]],active?gold:null,active?cream:'#50677a');if(active){rect(-2,15,4,5,cream);rect(-2,-19,4,5,cream);}ctx.restore();}
  function lantern(x,y,lit){line(x,y-60,x,y+27,'#50617a',5);line(x,y-60,x+24,y-60,'#50617a',5);rect(x+14,y-50,20,33,'#152636');ctx.strokeStyle='#927c50';ctx.lineWidth=3;ctx.strokeRect(x+14,y-50,20,33);if(lit){ctx.save();ctx.globalAlpha=.09;ctx.fillStyle=gold;ctx.beginPath();ctx.arc(x+24,y-33,48,0,Math.PI*2);ctx.fill();ctx.restore();rect(x+19,y-44,10,22,gold);}}
  function house(x,y,w,h,lit=false){rect(x,y-h,w,h,'#183246');polygon([[x-8,y-h],[x+w/2,y-h-40],[x+w+8,y-h]],'#122537');rect(x+14,y-h+30,22,36,lit?'#ba9859':'#0e2131');rect(x+w-38,y-h+30,22,36,'#0d2332');rect(x+w/2-13,y-61,26,61,'#091a27');rect(x+w/2-7,y-77,18,10,'#7b745b');line(x+w/2,y-h-16,x+w/2,y-86,'#627e80',3);for(let k=0;k<5;k++)rect(x+w/2-4,y-h+8+k*20,8,10,'#5b797a');}
  function gateDraw(g){if(g.side==='absent')return;ctx.save();ctx.translate(g.x,g.footY);ctx.scale(g.size,g.size);rect(-28,-7,274,7,'#101f2b');rect(-22,-326,60,326,'#31526c');rect(180,-326,60,326,'#31526c');rect(25,-326,160,56,'#31526c');rect(2,-346,32,26,'#4e6d82');rect(186,-346,32,26,'#4e6d82');polygon([[35,-267],[35,-292],[70,-318],[150,-318],[180,-292],[180,-267]],'#20394e');if(!g.open){rect(40,-270,134,270,'#102232');for(let x=49;x<175;x+=25)rect(x,-264,6,264,'#60727a');rect(41,-130,133,14,'#826e4d');}else{rect(37,-270,14,270,'#718485');rect(162,-270,14,270,'#718485');rect(16,-151,33,13,'#c0a166');}bell(110,-290,g.open,.8);ctx.restore();}
  function background(s,transition=true){
    const p=transition?ease(travel):1,prev=transition?fromStage:s;
    const residential=s>=11;
    rect(0,0,W,H,residential?'#163441':'#213b59');
    const a=core.gate(prev),b=core.gate(s);
    const distance=a.side!=='absent'||b.side!=='absent'?a.x-b.x:192;
    const shift=transition&&travel<1?-p*distance:0;
    for(let i=-1;i<7;i++){const x=i*190+shift*.15;rect(x,190,140,250,'#193148');rect(x+18,160,90,34,'#193148');}
    if(residential){for(let i=-1;i<6;i++)house(i*190+shift,442,155,180+(i%2)*40,s>=20);rect(0,454,W,15,'#285463');}
    else {for(let i=-1;i<8;i++){const x=i*145+shift*.4;rect(x,245,115,197,'#182f46');rect(x,245,115,12,'#3c5670');}if(s===5){rect(520,389,210,25,'#5d655f');rect(544,373,18,69,'#8a785b');rect(684,373,18,69,'#8a785b');}}
    const g=core.gateBetween(prev,s,p);
    gateDraw(g);
    for(let i=0;i<4;i++)lantern(90+i*245+shift,420,i===0||s===10||s===20);
    rect(0,442,W,15,'#587083');rect(0,457,W,83,'#0b1928');
    for(let i=-1;i<18;i++)rect(i*64+shift,446,55,12,'#30475c');
  }
  function actor(x,y,key='idle'){
    const image=images[key]||images.idle;
    if(image){ctx.imageSmoothingEnabled=false;ctx.drawImage(image,Math.round(x-60),Math.round(y-114),128,128);}
    else {rect(x-15,y-71,29,48,gold);rect(x-14,y-94,29,24,cream);rect(x-17,y-24,11,24,gold);rect(x+7,y-24,11,24,gold);}
  }
  function enemy(id,x=676,y=442,event=null){
    const sprites=window.MonsterSprites;if(!sprites)return;
    const a=sprites.getActor(id);if(!a)return;
    const motion=event?'Attack':'Idle';
    const clip=sprites.getClip(a,motion);let frame;
    if(event)frame=sprites.frameIndex(clip,Math.max(0,elapsed-event.impact+sprites.frameStart(clip,clip.contactFrame)),false);
    sprites.draw(ctx,id,motion,mode==='tutorial'?elapsed:now,x,y,a.height/6.2*H,frame);
  }
  function routeDraw(){
    background(stage);const s=plan.stages[stage-1];actor(320,442,travel<1?'run'+Math.floor(now/70)%8:'idle');
    if(travel>=1){
      const sprites=window.MonsterSprites,id=previewEnemyId(s),a=sprites.getActor(id),motion=$('route-motion').value,c=sprites.getClip(a,motion);
      const d=sprites.duration(c),t=$('route-repeat').checked?routeMotionTime%(d+(motion==='Attack'?400:0)):routeMotionTime;
      const rest=motion==='Attack'&&t>=d,f=sprites.frameIndex(c,t,false);
      const cycle=Math.floor(routeMotionTime/(d+400));
      if(motion==='Attack'&&!rest&&t>=sprites.frameStart(c,c.contactFrame)&&cycle!==routeSoundCycle){routeSoundCycle=cycle;if(stageVisible())audio?.play('sword-clash');}
      sprites.draw(ctx,id,rest?'Idle':motion,t,stage===10?648:692,442,a.height/6.2*H,rest?0:f,routeHitAge);
      $('route-motion-info').textContent=`${a.name} · ${rest?'다음 공격 대기':motion==='Idle'?'기본 자세':(motion==='Attack'?'공격':'기절')+' '+(f+1)+' / '+c.frames.length}${!rest&&f===c.contactFrame?' · 명중 프레임':''}`;
    }else $('route-motion-info').textContent='다음 스테이지로 이동 중';
    txt('Stage '+stage,30,35,26);txt(s.name,30,70,18,gold);txt('신규 몬스터 · 미리보기',930,35,17,mint,'right');if(travel<1)txt('오른쪽으로 이동 중',480,500,18,mint,'center');
  }
  function prologueDraw(){
    rect(0,0,W,H,'#0b1622');rect(0,446,W,94,'#101f2d');lantern(90,430,true);
    const chunks=plan.prologue.stanzas,read=$('reading-mode').checked;
    ctx.save();ctx.beginPath();ctx.rect(180,90,600,340);ctx.clip();
    if(read){chunks[stanza].forEach((s,i)=>txt(s,480,246+i*44,28,cream,'center'));}
    else if(!prologuePlaying&&prologueTime===0){txt('마지막 불씨',480,230,34,gold,'center');txt('프롤로그 재생을 눌러 시작하세요',480,285,20,cream,'center');}
    else{
      const total=chunks.reduce((n,c)=>n+c.length*40+24,0),progress=clamp(prologueTime/plan.prologue.durationMs);
      let y=430-(total+340)*progress;
      for(const chunk of chunks){for(const s of chunk){ctx.save();ctx.globalAlpha=clamp((y-90)/35)*clamp((430-y)/35);txt(s,480,y,26,cream,'center');ctx.restore();y+=40;}y+=24;}
    }
    ctx.restore();txt('마지막 종지기',480,44,17,'#9b8867','center');
    if(prologueTime>plan.prologue.durationMs-plan.prologue.fadeMs){ctx.save();ctx.globalAlpha=clamp((prologueTime-plan.prologue.durationMs+plan.prologue.fadeMs)/plan.prologue.fadeMs);rect(0,0,W,H,'#0b1622');ctx.restore();}
  }
  function range(type,alpha=.3){ctx.save();ctx.globalAlpha=alpha;ctx.fillStyle=gold;ctx.strokeStyle=cream;ctx.lineWidth=2;const r=type==='low'?[285,403,435,35]:[285,318,435,57];ctx.fillRect(...r);ctx.strokeRect(...r);ctx.restore();}
  function timingWave(){const e=trial.events.find(e=>elapsed<=e.impact+40);if(!e||elapsed<e.impact-600)return;
    const progress=clamp((elapsed-e.impact+600)/600),radius=640*progress;
    ctx.save();ctx.strokeStyle='#f1d197';ctx.lineWidth=6;ctx.globalAlpha=.58;
    for(const x of [-160,1120]){ctx.beginPath();ctx.arc(x,270,radius,0,Math.PI*2);ctx.stroke();}ctx.restore();}
  function pose(){
    const input=trial.pose;if(!input)return {x:320,y:442,key:'idle'};
    const e=trial.events[input.index],valid=['normal','perfect'].includes(input.result),age=elapsed-input.time;
    const hold=valid?e.impact+100:input.time+40;
    const amount=age<50?clamp(age/50):elapsed<hold?1:1-clamp((elapsed-hold)/130);
    if(amount<=0)return {x:320,y:442,key:'idle'};
    return {x:320+(input.direction==='left'?-90:input.direction==='right'?90:0)*amount,y:442-(input.direction==='up'?90:0)*amount,key:input.direction};
  }
  function tutorialDraw(){
    background(5,false);const active=phase==='combat',cue=active?core.cueAt(elapsed,trial.events):null;
    if(cue)range(cue.type,cue.local%240<120?.34:.07);
    const launch=active?trial.events.find(e=>elapsed>=e.impact-120&&elapsed<=e.impact+140):null;
    const id=previewEnemyId(plan.stages[4]),sprites=window.MonsterSprites,clip=sprites.getClip(sprites.getActor(id),'Attack'),contact=sprites.frameStart(clip,clip.contactFrame);
    const attack=active?trial.events.find(e=>elapsed>=e.impact-contact&&elapsed<e.impact+sprites.duration(clip)-contact):null;
    enemy(id,674,442,attack);const h=active?pose():{x:320,y:442,key:'idle'};actor(h.x,h.y,h.key);
    if(launch){const f=clamp((elapsed-launch.impact+120)/120);line(650,launch.type==='low'?420:340,lerp(650,320,f),launch.type==='low'?420:340,gold,12);}
    if(active){timingWave();for(const hit of trial.hits){const age=elapsed-hit.time;if(hit.result==='perfect'&&age>=0&&age<480){ctx.save();ctx.globalAlpha=1-age/480;ctx.strokeStyle=mint;ctx.lineWidth=7;ctx.beginPath();ctx.arc(480,270,640*ease(age/480),0,Math.PI*2);ctx.stroke();ctx.restore();}}}
    if(!active){rect(0,0,W,H,'#071320a8');ctx.save();ctx.strokeStyle=gold;ctx.lineWidth=2;ctx.strokeRect(411,91,138,76);ctx.restore();}
    const total=active?trial.events.length:2,remaining=active?trial.remaining:2;
    for(let i=0;i<total;i++)bell(480-(total-1)*22+i*44,132,i>=total-remaining,1.2);
    txt('Stage 5 · 검문쇠 거인',28,35,22);
    txt(active?'일반 전투':'안내 · 전투 정지',930,35,18,mint,'right');
    if(active&&!running)txt('일시정지',480,267,30,gold,'center');
  }
  function status(s){$('mode-status').textContent=s;}
  function setMode(next){mode=next;prologuePlaying=false;running=false;routeTutorialPending=false;audio?.stopSfx();audio?.pause(false);audio?.setMood(next==='prologue'?'bgm-sanctuary':next==='tutorial'?'bgm-boss':stage%10===0?'bgm-boss':stage>=11?'bgm-quarter':'bgm-road');for(const b of document.querySelectorAll('[data-mode]'))b.setAttribute('aria-pressed',String(b.dataset.mode===next));
    $('tutorial-modal').hidden=true;
    for(const n of ['prologue','route','tutorial'])$(n+'-panel').hidden=n!==next;
    if(next==='prologue'){prologueTime=0;stanza=0;lastSecond=-1;$('prologue-progress').textContent='0 / 28초';$('prologue-play').textContent='프롤로그 재생';status('프롤로그 · 28초 자동 세로 스크롤 초안');}
    if(next==='route')routeInfo();if(next==='tutorial')resetTutorial();}
  function routeInfo(){const s=plan.stages[stage-1],a=window.MonsterSprites.getActor(previewEnemyId(s));$('stage-number').textContent=stage;$('stage-range').value=stage;$('stage-caption').textContent=`${s.name} · ${a.name} — ${s.space}`;$('stage-prev').disabled=stage===1;$('stage-next').disabled=stage===20;status(`Stage ${stage} · 신규 몬스터 미리보기 · ${s.gate==='absent'?'성문은 화면 밖':s.gate==='left-inside'?'성문은 왼쪽, 들어온 방향':'성문은 오른쪽, 진행 방향'}`);}
  function moveTo(n,animated=true){fromStage=stage;stage=clamp(Number(n),1,20);travel=animated&&!reduced?0:1;routeMotionTime=0;routeHitAge=-1;routeSoundCycle=-1;lastFootstep=-1;audio?.stopSfx();audio?.setMood(stage%10===0?'bgm-boss':stage>=11?'bgm-quarter':'bgm-road');routeTutorialPending=animated&&stage===5&&!hintSeenInPreview;routeInfo();}
  function feedback(s){$('combat-feedback').textContent=s;}
  function syncTutorial(){
    const active=phase==='combat';
    $('tutorial-modal').hidden=active||mode!=='tutorial';
    $('combat-pause').disabled=!active;$('combat-pause').textContent=running?'일시정지':'계속';
    for(const b of document.querySelectorAll('[data-dodge]'))b.disabled=!active||!running;
    $('tutorial-copy').textContent=active?'확인 즉시 일반 전투가 이어집니다. 단발과 연속 공격의 종 개수를 확인할 수 있습니다.':'최초 5스테이지 도착 시 중앙 패널이 떠오릅니다. 확인 버튼 하나로 바로 전투를 시작합니다.';
    status('Stage 5 · '+(active?(running?'일반 전투 진행':'일반 전투 일시정지'):'중앙 안내 패널 / 전투 정지'));
  }
  function resetTutorial(){phase='explain';running=false;audio?.stopSfx();audio?.pause(false);trial=new core.Trial();elapsed=0;reported=0;sequenceIndex=0;feedback('');syncTutorial();$('tutorial-confirm').focus();}
  function confirmTutorial(){if(phase!=='explain'||mode!=='tutorial')return;hintSeenInPreview=true;phase='combat';sequenceIndex=0;trial=new core.Trial(previewSequences[0]);elapsed=0;reported=0;last=performance.now();running=true;feedback('Stage 5의 첫 공격 예고가 시작됩니다.');syncTutorial();canvas.focus();}
  function dodge(direction){if(mode!=='tutorial'||!running||phase!=='combat')return;const input=trial.input(direction,elapsed);if(input.result==='cooldown')feedback('잠깐 기다렸다가 다음 동작을 입력하세요.');}
  function outcomeText(hit){return ['normal','perfect'].includes(hit.result)?`${hit.index+1}타 ${hit.result==='perfect'?'완벽 대응':'대응 성공'}`:`${hit.index+1}타 ${hit.result==='early'?'입력이 너무 빨랐어요':hit.result==='wrong'?'공격 범위와 다른 방향이에요':'타이밍을 놓쳤어요'}`;}
  function tick(t){const delta=t-last;last=t;now=t;
    if(mode==='prologue'&&prologuePlaying&&!$('reading-mode').checked){prologueTime+=delta;const sec=Math.floor(prologueTime/1000);if(sec!==lastSecond){$('prologue-progress').textContent=`${Math.min(28,sec)} / 28초`;lastSecond=sec;}if(prologueTime>=plan.prologue.durationMs){setMode('route');moveTo(1,false);routeIntroUntil=now+plan.prologue.fadeMs;}}
    if(mode==='route'&&travel<1)travel=clamp(travel+delta/1600);
    if(mode==='route'&&travel<1){const step=Math.floor(travel*1600/280);if(step!==lastFootstep){lastFootstep=step;audio?.play('footstep-stone');}}
    if(mode==='route'&&travel>=1){const dt=delta*Number($('route-speed').value);routeMotionTime+=dt;if(routeHitAge>=0)routeHitAge+=dt;}
    if(mode==='route'&&travel>=1&&routeTutorialPending)setMode('tutorial');
    if(mode==='tutorial'&&running){const target=elapsed+delta;
      if(stageVisible())for(const e of trial.events){for(const blink of e.blinks)if(blink>=elapsed&&blink<target)audio?.play('cue-low');if(e.impact>=elapsed&&e.impact<target)audio?.play('sword-clash');}
      elapsed=target;trial.advance(elapsed);
      if(trial.hits.length>reported){for(const h of trial.hits.slice(reported))audio?.play(h.result==='perfect'?'perfect-parry':h.result==='normal'?'dodge-air':'hit-sharp');reported=trial.hits.length;feedback(trial.hits.map(outcomeText).join(' · '));}
      if(trial.finished){sequenceIndex=(sequenceIndex+1)%previewSequences.length;elapsed=0;reported=0;trial=new core.Trial(previewSequences[sequenceIndex]);}
    }
    ctx.clearRect(0,0,W,H);if(mode==='prologue')prologueDraw();else if(mode==='route'){routeDraw();if(now<routeIntroUntil){ctx.save();ctx.globalAlpha=clamp((routeIntroUntil-now)/plan.prologue.fadeMs);rect(0,0,W,H,'#0b1622');ctx.restore();}}else tutorialDraw();requestAnimationFrame(tick);
  }
  for(const b of document.querySelectorAll('[data-mode]'))b.addEventListener('click',()=>setMode(b.dataset.mode));
  $('prologue-play').addEventListener('click',()=>{if($('reading-mode').checked){stanza=0;prologueTime=1;}else prologuePlaying=!prologuePlaying;$('prologue-play').textContent=prologuePlaying?'일시정지':prologueTime?'계속':'프롤로그 재생';});
  $('prologue-reset').addEventListener('click',()=>{prologueTime=0;stanza=0;lastSecond=-1;$('prologue-progress').textContent='0 / 28초';prologuePlaying=!$('reading-mode').checked;$('prologue-play').textContent=prologuePlaying?'일시정지':'프롤로그 재생';});
  $('prologue-skip').addEventListener('click',()=>{setMode('route');moveTo(1,false);});
  $('reading-mode').addEventListener('change',()=>{prologuePlaying=false;stanza=0;$('next-stanza').hidden=!$('reading-mode').checked;$('prologue-play').textContent='프롤로그 재생';});
  $('next-stanza').hidden=!reduced;$('next-stanza').addEventListener('click',()=>{if(stanza<plan.prologue.stanzas.length-1)stanza++;else{setMode('route');moveTo(1,false);}});
  $('stage-prev').addEventListener('click',()=>moveTo(stage-1));$('stage-next').addEventListener('click',()=>moveTo(stage+1));$('stage-range').addEventListener('input',()=>moveTo($('stage-range').value,false));
  for(const id of ['route-motion','route-restart'])$(id).addEventListener(id==='route-motion'?'change':'click',()=>{routeMotionTime=0;routeHitAge=-1;routeSoundCycle=-1;audio?.stopSfx();});
  $('route-hit').addEventListener('click',()=>{routeHitAge=0;audio?.play('hit-sharp');});
  for(const b of document.querySelectorAll('[data-stage]'))b.addEventListener('click',()=>moveTo(b.dataset.stage));
  $('tutorial-confirm').addEventListener('click',confirmTutorial);$('tutorial-reset').addEventListener('click',resetTutorial);
  $('combat-pause').addEventListener('click',()=>{running=!running;audio?.pause(!running);last=performance.now();syncTutorial();canvas.focus();});
  for(const b of document.querySelectorAll('[data-dodge]'))b.addEventListener('click',()=>{dodge(b.dataset.dodge);canvas.focus();});
  const keys={ArrowUp:'up',w:'up',ArrowDown:'down',s:'down',ArrowLeft:'left',a:'left',ArrowRight:'right',d:'right'};
  document.addEventListener('keydown',e=>{if(mode==='tutorial'&&phase==='explain'){if(e.key==='Tab'){e.preventDefault();$('tutorial-confirm').focus();}return;}if(document.activeElement!==canvas||mode!=='tutorial'||e.repeat)return;const direction=keys[e.key]||keys[e.key.toLowerCase()];if(direction){e.preventDefault();dodge(direction);}});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){prologuePlaying=false;if(mode==='tutorial'){running=false;audio?.pause(true);syncTutorial();}$('prologue-play').textContent='계속';}last=performance.now();});
  for(const chunk of plan.prologue.stanzas){const p=document.createElement('p');chunk.forEach((s,i)=>{if(i)p.appendChild(document.createElement('br'));p.appendChild(document.createTextNode(s));});$('prologue-copy').appendChild(p);}
  for(const m of plan.monsters){const a=document.createElement('article');a.className='card';const h=document.createElement('h3');h.textContent=`${m.boardNumber}. ${m.name}`;a.appendChild(h);for(const text of [`Stage ${m.stages.join(' · ')}`,m.silhouette,m.telegraph,'처치: '+m.defeat]){const p=document.createElement('p');p.textContent=text;a.appendChild(p);}$('monster-briefs').appendChild(a);}
  for(const s of plan.stages){const tr=document.createElement('tr');for(const text of [s.stage,s.name+' / '+window.MonsterSprites.getActor(previewEnemyId(s)).name,s.space]){const td=document.createElement('td');td.textContent=text;tr.appendChild(td);}$('route-table').appendChild(tr);}
  const initialStage=Number(new URLSearchParams(location.search).get('stage'));
  if(Number.isInteger(initialStage)&&initialStage>=1&&initialStage<=20){setMode('route');moveTo(initialStage,false);}
  else audio?.setMood('bgm-sanctuary');
  requestAnimationFrame(tick);
})();
