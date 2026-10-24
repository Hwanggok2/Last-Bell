'use strict';
(() => {
  const plan=window.ROUTE_PLAN,core=window.RoutePreviewCore,$=id=>document.getElementById(id);
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
  let stage=1,fromStage=1,travel=1,routeIntroUntil=0,routeTutorialPending=false,tutorialCompletedInPreview=false;
  let phase='explain',confirmed=false,running=false,trial=new core.Trial(),elapsed=0,reported=0;
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
  function gateDraw(g){if(g.side==='absent'&&g.x<-300)return;ctx.save();ctx.translate(g.x,442);ctx.scale(g.size,g.size);rect(-22,-326,60,326,'#31526c');rect(180,-326,60,326,'#31526c');rect(25,-326,160,56,'#31526c');rect(2,-346,32,26,'#4e6d82');rect(186,-346,32,26,'#4e6d82');polygon([[35,-267],[35,-292],[70,-318],[150,-318],[180,-292],[180,-267]],'#20394e');if(!g.open){rect(40,-270,134,270,'#102232');for(let x=49;x<175;x+=25)rect(x,-264,6,264,'#60727a');rect(41,-130,133,14,'#826e4d');}else{rect(37,-270,14,270,'#718485');rect(162,-270,14,270,'#718485');rect(16,-151,33,13,'#c0a166');}bell(110,-290,g.open,.8);ctx.restore();}
  function background(s,transition=true){
    const p=transition?ease(travel):1,prev=transition?fromStage:s;
    const residential=s>=11;
    rect(0,0,W,H,residential?'#163441':'#213b59');
    const shift=transition&&travel<1?-travel*192:0;
    for(let i=-1;i<7;i++){const x=i*190+shift*.15;rect(x,190,140,250,'#193148');rect(x+18,160,90,34,'#193148');}
    if(residential){for(let i=-1;i<6;i++)house(i*190+shift,442,155,180+(i%2)*40,s>=20);rect(0,454,W,15,'#285463');}
    else {for(let i=-1;i<8;i++){const x=i*145+shift*.4;rect(x,245,115,197,'#182f46');rect(x,245,115,12,'#3c5670');}if(s===5){rect(520,389,210,25,'#5d655f');rect(544,373,18,69,'#8a785b');rect(684,373,18,69,'#8a785b');}}
    const a=core.gate(prev),b=core.gate(s);
    const g={...b,x:lerp(a.x,b.x,p),size:lerp(a.size,b.size,p),open:s===11?(p>.2):b.open};
    if(prev===11&&s===12&&travel<1)g.side='left-inside';
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
  function enemy(id,x=676,y=442,action=null){
    ctx.save();ctx.translate(x,y);ctx.lineWidth=4;
    if(id.includes('hound')){
      const chain=id==='hook_hound';polygon([[-68,-60],[-37,-99],[45,-90],[72,-57],[43,-35],[-41,-35]],'#8e773f',gold);
      rect(-71,-71,31,29,cream);rect(-44,-35,15,35,gold);rect(34,-37,15,37,gold);
      line(65,-64,94,-88,gold,5);if(chain)line(-72,-64,-91,-31,cream,6);
    }else if(id==='gate_lock'||id==='pendulum_warden'){
      rect(-52,-131,108,101,'#7f754d');ctx.strokeStyle=gold;ctx.strokeRect(-52,-131,108,101);rect(-31,-172,63,40,gold);rect(-42,-28,24,28,gold);rect(22,-28,24,28,gold);
      const low=action==='low',high=action==='high';rect(low?-176:-124,low?-35:-84,90,22,gold);rect(54,high?-188:-116,24,94,gold);if(id==='pendulum_warden'){line(0,-122,0,-37,cream,5);bell(0,-38,true,1.6);}else{rect(-38,-113,77,13,'#192b35');rect(-38,-76,77,13,'#192b35');}
    }else if(id==='bell_knight'){
      polygon([[-51,-171],[-30,-224],[28,-224],[55,-171],[43,-46],[-45,-46]],'#ab8b48',cream);rect(-22,-187,44,29,'#142837');rect(-42,-44,23,44,gold);rect(21,-44,23,44,gold);rect(-100,-156,60,126,'#2e454c');ctx.strokeStyle=gold;ctx.strokeRect(-100,-156,60,126);for(let k=0;k<3;k++)rect(-90+k*16,-145,5,100,gold);line(72,-246,72,-9,gold,8);polygon([[72,-246],[109,-209],[72,-184],[46,-211]],cream);bell(0,-235,true,.8);
    }else if(id==='chain_puppet'||id==='chain_dancer'||id==='chain_jailer'){
      const boss=id==='chain_jailer',h=boss?210:140;line(-30,-h,0,-31,'#af9b67',6);line(30,-h,0,-31,'#af9b67',6);rect(-22,-h-15,44,39,gold);rect(-11,-h-7,22,20,'#172d38');line(-8,-h+31,-87,-h+60,gold,6);line(8,-h+31,76,-h+15,gold,6);
      if(id==='chain_dancer'){polygon([[0,-110],[-65,-29],[63,-29]],'#8c7f58',gold);for(let i=-2;i<=2;i++)bell(i*24,-24,true,.4);}
      else {rect(-33,-h+42,23,38,'#b19565');rect(6,-h+55,25,39,'#aa8d5a');line(-14,-35,-32,0,gold,5);line(14,-35,32,0,gold,5);}
      if(boss){line(-76,-290,-51,-h,'#af9b67',6);line(76,-290,51,-h,'#af9b67',6);}
    }else{
      polygon([[-27,-117],[30,-117],[43,-45],[-35,-45]],'#b28d48',cream);rect(-19,-160,38,36,gold);rect(-12,-149,16,18,'#132938');rect(-26,-43,16,43,gold);rect(12,-43,16,43,gold);
      if(id==='cracked_blade')line(-45,-207,-45,-47,cream,9);else line(-130,-102,80,-102,gold,7);
    }
    ctx.restore();
  }
  function routeDraw(){background(stage);const s=plan.stages[stage-1];actor(320,442,travel<1?'run'+Math.floor(now/70)%8:'idle');if(travel>=1)enemy(s.enemyId,stage===10?648:692);txt('Stage '+stage,30,35,26);txt(s.name,30,70,18,gold);if(stage>10)txt('확장 구간 미리보기',930,35,17,mint,'right');if(travel<1)txt('오른쪽으로 이동 중',480,500,18,mint,'center');}
  function prologueDraw(){
    rect(0,0,W,H,'#0b1622');rect(0,446,W,94,'#101f2d');lantern(90,430,true);
    ctx.save();ctx.globalAlpha=.3;gateDraw({side:'right-closed',x:800,size:.7,open:false});ctx.restore();
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
    background(5,false);const active=['practice','demo','combat'].includes(phase),cue=active?core.cueAt(elapsed,trial.events):null;
    if(cue)range(cue.type,cue.local%240<120?.34:.07);
    const launch=active?trial.events.find(e=>elapsed>=e.impact-120&&elapsed<=e.impact+140):null;
    enemy('gate_lock',674,442,launch?.type||cue?.type);const h=active?pose():{x:320,y:442,key:'idle'};actor(h.x,h.y,h.key);
    if(launch){const f=clamp((elapsed-launch.impact+120)/120);line(650,launch.type==='low'?420:340,lerp(650,320,f),launch.type==='low'?420:340,gold,12);}
    if(active){timingWave();for(const hit of trial.hits){const age=elapsed-hit.time;if(hit.result==='perfect'&&age>=0&&age<480){ctx.save();ctx.globalAlpha=1-age/480;ctx.strokeStyle=mint;ctx.lineWidth=7;ctx.beginPath();ctx.arc(480,270,640*ease(age/480),0,Math.PI*2);ctx.stroke();ctx.restore();}}}
    if(!active&&phase!=='success'&&phase!=='failure'){rect(0,0,W,H,'#071320a8');ctx.save();ctx.strokeStyle=gold;ctx.lineWidth=2;ctx.strokeRect(411,91,138,87);ctx.restore();}
    const remaining=active||['success','failure'].includes(phase)?trial.remaining:2;
    for(let i=0;i<2;i++)bell(458+i*44,132,i>=2-remaining,1.2);
    txt('Stage 5 · 검문쇠 거인',28,35,22);
    if(['explain','ready'].includes(phase))txt('종 두 개 = 다음 공격 두 번',480,209,24,gold,'center');
    if(phase==='success')txt('두 번 모두 대응 성공',480,250,28,mint,'center');
    if(phase==='failure')txt('체력 소모 없이 다시 연습',480,250,25,cream,'center');
    txt(phase==='combat'?'일반 전투 재개':'연습 · HP 유지',930,35,18,mint,'right');
    if(active&&!running)txt('일시정지',480,267,30,gold,'center');
  }
  function status(s){$('mode-status').textContent=s;}
  function setMode(next){mode=next;prologuePlaying=false;running=false;routeTutorialPending=false;for(const b of document.querySelectorAll('[data-mode]'))b.setAttribute('aria-pressed',String(b.dataset.mode===next));
    for(const n of ['prologue','route','tutorial'])$(n+'-panel').hidden=n!==next;
    if(next==='prologue'){prologueTime=0;stanza=0;lastSecond=-1;$('prologue-progress').textContent='0 / 28초';$('prologue-play').textContent='프롤로그 재생';status('프롤로그 · 28초 자동 세로 스크롤 초안');}
    if(next==='route')routeInfo();if(next==='tutorial')resetTutorial();}
  function routeInfo(){const s=plan.stages[stage-1];$('stage-number').textContent=stage;$('stage-range').value=stage;$('stage-caption').textContent=`${s.name} · ${s.enemyName} — ${s.space}`;$('stage-prev').disabled=stage===1;$('stage-next').disabled=stage===20;status(`Stage ${stage} · ${s.expansion?'확장 미리보기':'프로토타입 구간'} · ${s.gate==='absent'?'성문은 화면 밖':s.gate==='left-inside'?'성문은 왼쪽, 들어온 방향':'성문은 오른쪽, 진행 방향'}`);}
  function moveTo(n,animated=true){fromStage=stage;stage=clamp(Number(n),1,20);travel=animated&&!reduced?0:1;routeTutorialPending=animated&&stage===5&&!tutorialCompletedInPreview;routeInfo();}
  function feedback(s){$('practice-feedback').textContent=s;}
  function syncTutorial(){
    const active=['practice','demo','combat'].includes(phase);
    $('count-answers').hidden=phase!=='explain';$('practice-start').disabled=!confirmed||active||phase==='success';$('practice-start').textContent=phase==='failure'?'다시 연습':'연습하기';
    $('practice-pause').disabled=!active;$('practice-pause').textContent=running?'일시정지':'계속';$('practice-continue').hidden=phase!=='success';
    for(const b of document.querySelectorAll('[data-dodge]'))b.disabled=!active||!running||phase==='demo';
    $('tutorial-copy').textContent={explain:'종은 다음 공격이 몇 번 이어지는지 알려줘요. 다음 공격은 몇 번일까요?',ready:'두 방향의 금빛 영역을 순서대로 기억하세요. 파동이 중앙에 맞닿을 때마다 대응해 보세요.',practice:'첫 공격을 피한 뒤에도 다음 파동을 보세요.',demo:'자동 시연 중입니다. 종이 2 → 1 → 0으로 줄어드는 것을 보세요.',success:'두 번 모두 막았어요. 이제 5스테이지 전투를 계속합니다.',failure:'다시 해볼 수 있어요. 두 타를 모두 막으면 연습이 끝납니다.',combat:'5스테이지 일반 전투가 재개됐습니다. 첫 예고부터 다시 시작합니다.'}[phase];
    status('Stage 5 · '+({explain:'전투 정지 / 종 개수 확인',ready:'전투 정지 / 연습 준비',practice:'직접 연습',demo:'자동 시연 / 완료 처리 없음',success:'연습 완료 / 계속 대기',failure:'연습 다시 시도',combat:'일반 전투 재개'}[phase]));
  }
  function resetTutorial(){phase='explain';confirmed=false;running=false;trial=new core.Trial();elapsed=0;reported=0;feedback('위쪽 중앙의 종 두 개를 확인하세요.');syncTutorial();}
  function beginPractice(demo=false){phase=demo?'demo':'practice';trial=new core.Trial();elapsed=0;reported=0;running=true;feedback(demo?'시연은 튜토리얼 완료로 기록되지 않습니다.':'입력은 타격 시점에 판정됩니다. 연습 중에는 체력이 줄지 않습니다.');syncTutorial();canvas.focus();}
  function dodge(direction){if(mode!=='tutorial'||!running||!['practice','combat'].includes(phase))return;const input=trial.input(direction,elapsed);if(input.result==='cooldown')feedback('잠깐 기다렸다가 다음 동작을 입력하세요.');}
  function outcomeText(hit){return ['normal','perfect'].includes(hit.result)?`${hit.index+1}타 ${hit.result==='perfect'?'완벽 대응':'대응 성공'}`:`${hit.index+1}타 ${hit.result==='early'?'입력이 너무 빨랐어요':hit.result==='wrong'?'공격 범위와 다른 방향이에요':'타이밍을 놓쳤어요'}`;}
  function tick(t){const delta=t-last;last=t;now=t;
    if(mode==='prologue'&&prologuePlaying&&!$('reading-mode').checked){prologueTime+=delta;const sec=Math.floor(prologueTime/1000);if(sec!==lastSecond){$('prologue-progress').textContent=`${Math.min(28,sec)} / 28초`;lastSecond=sec;}if(prologueTime>=plan.prologue.durationMs){setMode('route');moveTo(1,false);routeIntroUntil=now+plan.prologue.fadeMs;}}
    if(mode==='route'&&travel<1)travel=clamp(travel+delta/1600);
    if(mode==='route'&&travel>=1&&routeTutorialPending)setMode('tutorial');
    if(mode==='tutorial'&&running){const target=elapsed+delta;
      if(phase==='demo')for(const e of trial.events)if(elapsed<=e.impact-50&&target>=e.impact-50)trial.input(e.direction,e.impact-50);
      elapsed=target;trial.advance(elapsed);
      if(trial.hits.length>reported){reported=trial.hits.length;feedback(trial.hits.map(outcomeText).join(' · '));}
      if(trial.finished){if(phase==='combat'){elapsed=0;reported=0;trial=new core.Trial();}
        else if(phase==='demo'){running=false;phase=confirmed?'ready':'explain';feedback('시연이 끝났습니다. 종 개수를 확인한 뒤 직접 연습해 보세요.');syncTutorial();}
        else {phase=trial.success?'success':'failure';if(trial.success)tutorialCompletedInPreview=true;running=false;if(!trial.success){const bad=trial.hits.find(h=>!['normal','perfect'].includes(h.result));feedback(trial.hits.map(outcomeText).join(' · ')+(bad?.type==='high'?' / 상단 공격은 아래 회피로 검을 쳐냅니다.':' / 하단 범위는 위 회피로 벗어납니다.'));}syncTutorial();}}
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
  for(const b of document.querySelectorAll('[data-stage]'))b.addEventListener('click',()=>moveTo(b.dataset.stage));
  for(const b of document.querySelectorAll('[data-count]'))b.addEventListener('click',()=>{if(Number(b.dataset.count)===2){confirmed=true;phase='ready';feedback('맞아요. 두 번 공격합니다. 한 번 피한 뒤에도 다음 공격이 남아 있어요.');syncTutorial();}else feedback('상단 중앙에 있는 종 두 개를 다시 확인하세요.');});
  $('practice-start').addEventListener('click',()=>{if(confirmed)beginPractice(false);});$('practice-demo').addEventListener('click',()=>beginPractice(true));$('practice-reset').addEventListener('click',resetTutorial);
  $('practice-pause').addEventListener('click',()=>{running=!running;syncTutorial();canvas.focus();});
  $('practice-continue').addEventListener('click',()=>{if(phase!=='success')return;phase='combat';elapsed=0;reported=0;trial=new core.Trial();running=true;feedback('연습 전 상태로 복귀했습니다. Stage 5의 첫 예고가 시작됩니다.');syncTutorial();canvas.focus();});
  for(const b of document.querySelectorAll('[data-dodge]'))b.addEventListener('click',()=>{dodge(b.dataset.dodge);canvas.focus();});
  const keys={ArrowUp:'up',w:'up',ArrowDown:'down',s:'down',ArrowLeft:'left',a:'left',ArrowRight:'right',d:'right'};
  document.addEventListener('keydown',e=>{if(document.activeElement!==canvas||mode!=='tutorial'||e.repeat)return;const direction=keys[e.key]||keys[e.key.toLowerCase()];if(direction){e.preventDefault();dodge(direction);}});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){prologuePlaying=false;if(mode==='tutorial'){running=false;syncTutorial();}$('prologue-play').textContent='계속';}last=performance.now();});
  for(const chunk of plan.prologue.stanzas){const p=document.createElement('p');chunk.forEach((s,i)=>{if(i)p.appendChild(document.createElement('br'));p.appendChild(document.createTextNode(s));});$('prologue-copy').appendChild(p);}
  for(const m of plan.monsters){const a=document.createElement('article');a.className='card';const h=document.createElement('h3');h.textContent=`${m.boardNumber}. ${m.name}`;a.appendChild(h);for(const text of [`Stage ${m.stages.join(' · ')}`,m.silhouette,m.telegraph,'처치: '+m.defeat]){const p=document.createElement('p');p.textContent=text;a.appendChild(p);}$('monster-briefs').appendChild(a);}
  for(const s of plan.stages){const tr=document.createElement('tr');for(const text of [s.stage,s.name+' / '+s.enemyName,s.space]){const td=document.createElement('td');td.textContent=text;tr.appendChild(td);}$('route-table').appendChild(tr);}
  requestAnimationFrame(tick);
})();
