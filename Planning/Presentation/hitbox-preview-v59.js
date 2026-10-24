'use strict';
(() => {
 const spec=window.HITBOX_PREVIEW_SPEC,$=id=>document.getElementById(id),canvas=$('battle-preview'),ctx=canvas.getContext('2d');
 const embedded=new URLSearchParams(location.search).has('embed');if(embedded)document.documentElement.classList.add('embed');
 const base='../../Client/Assets/Game/Art/Presentation/',images={},gold=spec.color,cream='#efdfb8',mint='#9ee3d4',dark='#102035';
 const files={hero:'PixelLab/inputs/hero.png',up:base+'Clips/hero-dodge-up/04.png',down:base+'Clips/hero-dodge-down/04.png',back:base+'Clips/hero-dodge-back/08.png',forward:base+'Clips/hero-dodge-front/04.png'};
 for(const name of ['sky','far','mid','near','floor'])files[name]=base+'Background/gate-'+name+'.png';
 for(let i=0;i<17;i++)files['enemy'+i]=base+'Clips/knight-attack-retry/'+String(i).padStart(2,'0')+'.png';
 const attackFrames=[0,1,2,3,4,5,6,7,8,9,10,17,23,24];for(const i of attackFrames)files['attack'+i]=base+'Clips/hero-v6-slash-up/'+String(i).padStart(2,'0')+'.png';
 let practiceInputs=[],lastPracticeInput=-Infinity;
 let selected=spec.attacks.find(a=>a.id==='high'),duration=spec.defaultImpactMs,time=0,playing=true,ready=false,last=0,manualCounter=null;
 const reduced=matchMedia('(prefers-reduced-motion: reduce)');$('reduce').checked=reduced.matches;if(reduced.matches)playing=false;
 const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x)),ease=x=>1-(1-clamp(x))**3,lerp=(a,b,p)=>a+(b-a)*p;
 const patterns={'front-rear':['thrust','rear'],'rear-front':['rear','thrust'],'low-high':['low','high'],'high-low':['high','low'],'triple':['thrust','rear','low'],'high-high':['high','high']};
 function sequence(){
  const ids=patterns[$('pattern').value]||[selected.id],slot=duration,beat=duration/3,gap=2*beat,flight=beat/2;
  return ids.map((id,i)=>({attack:spec.attacks.find(a=>a.id===id),index:i,cueStart:i*slot,cueEnd:(i+1)*slot,slotEnd:(i+1)*slot,
   launch:ids.length*slot+i*gap-flight,impact:ids.length*slot+i*gap,beat,flight}));
 }
 function timingState(t=time){
  const q=sequence(),event=q.find(e=>t<=e.impact),previous=event&&q[event.index-1];
  if(!event)return null;
  const start=event.impact-event.beat*2.5;
  if(t<start)return null;
  const progress=clamp((t-start)/(event.impact-start)),offset=canvas.width*spec.timingAssist.sourceOffsetWidth;
  const radius=(canvas.width/2+offset)*progress,origins=[[-offset,canvas.height/2],[canvas.width+offset,canvas.height/2]];
  return {index:event.index,start,impact:event.impact,progress,origin:[canvas.width/2,canvas.height/2],origins,radius,
   visible:radius>=offset,leftX:origins[0][0]+radius,rightX:origins[1][0]-radius,
   perfectWindow:t>=event.impact-60&&t<=event.impact};
 }
 function bellState(){const q=sequence();return {total:q.length,remaining:q.filter(e=>time<e.impact).length,
  visible:q.filter(e=>time<e.impact).map(e=>({index:e.index,x:canvas.width/2+(e.index-(q.length-1)/2)*44*canvas.height/540,y:canvas.height/3}))};}
 function practiceResult(e){const input=practiceInputs.find(p=>p.index===e.index);return input?input.result:'miss';}
 function practiceInput(action){
  if(!$('practice').checked||!playing)return;
  const now=performance.now();time=clamp(time+Math.max(0,now-last)*Number($('speed').value),0,total());last=now;
  if(action!=='attack'&&time-lastPracticeInput<260){$('practice-result').textContent='회피 재사용 대기 중';return;}
  const e=sequence().find(e=>time<=e.impact);
  if(!e){$('practice-result').textContent='이번 공격은 끝났습니다. 처음부터 다시 확인하세요.';return;}
  const early=e.impact-time,expected=e.attack.dodge;
  const result=action!==expected?'wrong':early<=60?'perfect':early<=160?'normal':'early';
  if(action!=='attack')lastPracticeInput=time;
  practiceInputs=practiceInputs.filter(p=>p.index!==e.index);practiceInputs.push({index:e.index,time,early,result,action});
  $('practice-result').textContent=`${e.index+1}타 입력 기록 · 타격 후 결과 표시`;
  draw();
 }
 function practiceStatus(){
  if(!$('practice').checked)return;
  const e=[...sequence()].reverse().find(e=>time>=e.impact);
  if(!e)return;
  const input=practiceInputs.find(p=>p.index===e.index),result=practiceResult(e);
  $('practice-result').textContent=`${e.index+1}타 · ${result==='perfect'?'완벽':result==='normal'?(e.attack.response==='parry'?'쳐냄':'일반 회피'):result==='early'?'너무 빠름':result==='wrong'?'입력 종류가 다름':'입력 없음 / 타이밍 놓침'}${input?' · '+Math.round(input.early)+'ms 전 입력':''}`;
 }
 function practiceDodge(direction='up'){practiceInput(direction);}
 function practiceDown(){practiceInput('down');}
 function practiceAttack(){practiceInput('attack');}
 function parryInput(e){
  if(e.attack.response!=='parry')return null;
  if($('practice').checked)return practiceInputs.find(p=>p.index===e.index&&p.action==='down'&&['normal','perfect'].includes(p.result))||null;
  return $('auto-dodge').checked?{index:e.index,time:e.impact-50,early:50,result:'perfect',action:'down'}:null;
 }
 function isParried(e){return !!parryInput(e);}
 function parryPose(t){
  for(const e of [...sequence()].reverse()){
   const input=$('practice').checked?practiceInputs.find(p=>p.index===e.index&&p.action==='down'):parryInput(e);
   const hit=input&&['normal','perfect'].includes(input.result)?e.impact:input?input.time+160:0;
   if(input&&t>=input.time&&t<hit+180)return {event:e,hit,age:t-input.time,impactAge:t-hit,input};
  }return null;
 }
 function total(){return sequence().at(-1).impact+900;}
 function stage(t=time){
  const q=sequence(),cue=q.find(e=>t>=e.cueStart&&t<e.cueEnd);
  if(cue){const phase=(t-cue.cueStart)/((cue.cueEnd-cue.cueStart)/3),index=Math.floor(phase);return {kind:'cue',event:cue,blink:index+1,bright:phase-index<.5,fraction:phase-index};}
  const flight=q.find(e=>t>=e.launch&&t<e.impact);if(flight)return {kind:'flight',event:flight,progress:(t-flight.launch)/flight.flight};
  const hit=[...q].reverse().find(e=>t>=e.impact&&t<e.impact+260);if(hit)return {kind:'impact',event:hit,progress:(t-hit.impact)/260};
  const wait=q.find(e=>t<e.launch);return {kind:wait?'between':'recover',event:wait||q.at(-1)};
 }
 function dodge(t=time){
  if($('practice').checked){
   const input=[...practiceInputs].reverse().find(p=>t>=p.time);
   if(!input)return {amount:0,event:sequence()[0]};
   let event=sequence()[input.index];if(!['up','back','forward'].includes(input.action))return {event,amount:0};event={...event,attack:{...event.attack,dodge:input.action}};const enter=Math.min(50,Math.max(1,event.impact-input.time));
   const valid=input.result==='perfect'||input.result==='normal',holdEnd=valid?event.impact+100:input.time+30;
   const amount=valid&&t>=event.impact&&t<holdEnd?1:t<input.time+enter?ease((t-input.time)/enter):t<holdEnd?1:1-ease((t-holdEnd)/130);
   return {event,amount};
  }
  if(!$('auto-dodge').checked)return {amount:0,event:sequence()[0]};
  const e=[...sequence()].reverse().find(e=>t>=e.impact-50)||sequence()[0],age=t-e.impact;
  return {event:e,amount:e.attack.response==='parry'?0:age< -50?0:age<0?ease((age+50)/50):age<100?1:1-ease((age-100)/130)};
 }
 function counterStart(){if(manualCounter!==null)return manualCounter;return $('auto-counter').checked&&$('auto-dodge').checked&&!$('practice').checked?sequence().at(-1).impact+90:Infinity;}
 function heroState(t=time){
  const parry=parryPose(t);if(parry&&!(t>=counterStart()&&t<counterStart()+500))return {x:375,y:442,amount:0,direction:'back',counter:false,parrying:true,parry};
  const d=dodge(t),off=spec.dodgeOffsets[d.event.attack.dodge],age=t-counterStart();
  if(age>=0&&age<500)return {x:age<120?lerp(375,540,ease(age/120)):age<230?540:lerp(540,375,ease((age-230)/270)),y:442,amount:0,direction:d.event.attack.dodge,counter:true,counterAge:age};
  return {x:375+off[0]*d.amount,y:442+off[1]*d.amount,amount:d.amount,direction:d.event.attack.dodge,counter:false,counterAge:age};
 }
 function impactHero(e){
  // An accepted counter at T occurs after T's avoidance confirmation, never before it.
  if(counterStart()===e.impact){const d=dodge(e.impact),off=spec.dodgeOffsets[e.attack.dodge];return {x:375+off[0]*d.amount,y:442+off[1]*d.amount,direction:e.attack.dodge,amount:d.amount};}
  return heroState(e.impact);
 }
 function bodyAtImpact(e){const h=impactHero(e),r=[...spec.playerBody];if(h.direction==='down')for(let i=0;i<4;i++)r[i]+=(spec.crouchedBody[i]-r[i])*h.amount;r[0]+=h.x-375;r[1]+=h.y-442;return r;}
 const overlaps=(a,b)=>a[0]<b[0]+b[2]&&a[0]+a[2]>b[0]&&a[1]<b[1]+b[3]&&a[1]+a[3]>b[1];
 const isPerfect=e=>e.attack.response==='parry'?parryInput(e)?.result==='perfect':($('practice').checked?practiceResult(e)==='perfect':$('auto-dodge').checked)&&impactHero(e).amount>.99&&!overlaps(bodyAtImpact(e),e.attack.rect);
 function fill(x,y,w,h,color,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.fillStyle=color;ctx.fillRect(Math.round(x),Math.round(y),Math.round(w),Math.round(h));ctx.globalAlpha=1;}
 function line(x1,y1,x2,y2,color,width=2,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();ctx.globalAlpha=1;}
 function ring(x,y,rx,ry,color,width=2,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.ellipse(x,y,Math.max(1,rx),Math.max(1,ry),0,0,Math.PI*2);ctx.stroke();ctx.globalAlpha=1;}
 function sprite(image,x,y,scale,pivotX=60,pivotY=114){ctx.drawImage(image,Math.round(x-pivotX*scale),Math.round(y-pivotY*scale),Math.round(128*scale),Math.round(128*scale));}
 function triangle(x,y,size,direction,color){ctx.save();ctx.translate(x,y);ctx.rotate([0,-Math.PI/2,Math.PI,Math.PI/2][direction]);ctx.fillStyle=color;ctx.beginPath();ctx.moveTo(0,-size);ctx.lineTo(size*.85,size*.75);ctx.lineTo(-size*.85,size*.75);ctx.closePath();ctx.fill();ctx.restore();}
 function box(e,state){
  if(time<e.cueStart||time>=e.impact)return;const active=state.kind==='cue'&&state.event.index===e.index,low=$('reduce').checked,r=e.attack.rect;
  fill(...r,gold,active?(low?.12:state.bright?.2:.04):$('exposure').value==='original'&&time>=e.launch?.07:0);ctx.globalAlpha=active?(low?.46:state.bright?.62:.2):.14;ctx.strokeStyle=gold;ctx.lineWidth=2;ctx.strokeRect(r[0]+1,r[1]+1,r[2]-2,r[3]-2);ctx.globalAlpha=1;
  const length=active&&low?8+10*(1-state.fraction):10;for(const [x,y,sx,sy] of [[r[0],r[1],1,1],[r[0]+r[2],r[1],-1,1],[r[0],r[1]+r[3],1,-1],[r[0]+r[2],r[1]+r[3],-1,-1]]){line(x,y,x+length*sx,y,gold,2,active?.5:.2);line(x,y,x,y+length*sy,gold,2,active?.5:.2);}
 }
 const noise=i=>{const n=Math.sin(i*127.1+311.7)*43758.5453;return n-Math.floor(n);};
 function burst(x,y,age,color,count=24,dust=false){
  const p=age/(dust?340:260);if(p<0||p>=1)return;if($('reduce').checked){ring(x,y,18,18,color,2,.4*(1-p));return;}
  for(let i=0;i<count;i++){const a=dust?Math.PI+noise(i+8)*Math.PI:noise(i+8)*Math.PI*2,s=(dust?40:45)+noise(i+33)*(dust?60:120),dx=Math.cos(a)*s*p,dy=Math.sin(a)*s*p+(dust?55:40)*p*p,size=dust?3+noise(i)*5:2+noise(i)*3;
   if(dust)fill(x+dx,y+dy,size,size,'#9c937d',.3*(1-p));else line(x+dx,y+dy,x+dx-Math.cos(a)*(6+noise(i)*9),y+dy-Math.sin(a)*(6+noise(i)*9),i%3?color:cream,size*.55,(1-p)**1.4);}
 }
 function warmup(state){if(state.kind!=='cue'||$('reduce').checked)return;for(let i=0;i<8;i++){const p=(time*.0018+i/8)%1,a=i*2.4;fill(624+Math.cos(a)*28*(1-p),335+Math.sin(a)*23*(1-p),2,2,gold,.35*p);}}
 function attackPoint(e,p){const id=e.attack.id;return id==='rear'?{x:lerp(624,345,p),y:330-225*Math.sin(p*Math.PI)+96*p}:{x:lerp(624,id==='high'&&isParried(e)?420:375,p),y:id==='low'?423:id==='high'?332:374};}
 function projectile(e){
  const age=time-e.launch;if(age<0||age>460)return;const hitAge=time-e.impact,flight=age<e.flight,p=flight?(age/e.flight)**1.35:1,pt=attackPoint(e,p),low=$('reduce').checked;
  if(flight){
   if(!low)for(let i=5;i>=1;i--){const a=attackPoint(e,Math.max(0,p-i*.045));line(a.x+45,a.y,a.x,a.y,gold,10-i,.10+(5-i)*.045);}
   if(e.attack.id==='thrust'){line(pt.x+110,pt.y,pt.x+12,pt.y,dark,10);line(pt.x+110,pt.y,pt.x+12,pt.y,'#c49a56',6);line(pt.x+100,pt.y-2,pt.x+16,pt.y-2,cream,2);ctx.fillStyle=cream;ctx.beginPath();ctx.moveTo(pt.x,pt.y);ctx.lineTo(pt.x+24,pt.y-10);ctx.lineTo(pt.x+17,pt.y);ctx.lineTo(pt.x+24,pt.y+10);ctx.fill();if(!low)for(let i=0;i<4;i++)line(pt.x+35,pt.y-38+i*25,pt.x+125,pt.y-38+i*25,gold,2,.35);}
   else if(e.attack.id==='rear')for(const dx of [-52,0,52]){line(pt.x+dx+14,pt.y-65,pt.x+dx,pt.y,gold,8);line(pt.x+dx+13,pt.y-57,pt.x+dx,pt.y,cream,3);}
   else{ctx.strokeStyle=gold;ctx.lineWidth=10;ctx.beginPath();ctx.ellipse(pt.x+14,pt.y,23,e.attack.id==='low'?15:24,0,Math.PI*.62,Math.PI*1.38);ctx.stroke();ctx.strokeStyle=cream;ctx.lineWidth=3;ctx.stroke();line(pt.x+8,pt.y,pt.x+105,pt.y,gold,3,.7);}
  }else{
   const pt=attackPoint(e,1);
   if(e.attack.response==='parry'&&isParried(e)){
    if(hitAge<100){line(400,311,442,353,cream,4,1-hitAge/100);line(400,353,442,311,cream,4,1-hitAge/100);}
    burst(420,332,hitAge,cream,18);
    if(!low&&hitAge<140){const p=hitAge/140;ctx.strokeStyle=gold;ctx.lineWidth=5;ctx.globalAlpha=1-p;ctx.beginPath();ctx.arc(420+p*100,332-p*120,22,Math.PI*.7,Math.PI*1.5);ctx.stroke();ctx.globalAlpha=1;}
    if(!isPerfect(e)&&hitAge<220){ctx.fillStyle=cream;ctx.font='bold 18px Nanum, sans-serif';ctx.fillText('쳐냄',395,298);}
    return;
   }
   if(hitAge<45&&!low){ring(pt.x,pt.y,12+hitAge*.6,7+hitAge*.3,cream,6,.8*(1-hitAge/45));line(pt.x-28,pt.y,pt.x+28,pt.y,cream,3,1-hitAge/45);}
   burst(pt.x,pt.y,hitAge,gold,e.attack.id==='rear'?30:24);burst(pt.x,442,hitAge,gold,14,true);if(!low&&hitAge<300)ring(pt.x,442,12+hitAge*.28,3+hitAge*.07,gold,2,.55*(1-hitAge/300));
   if(e.attack.id==='rear'&&!low&&hitAge<140)for(let i=0;i<5;i++)line(265+i*28,440,265+i*28,440-(1-hitAge/140)*(24+noise(i)*45),'#bea16a',3,.5*(1-hitAge/140));
  }
 }
 function perfectWaveState(e){
  const age=time-e.impact;if(age<0||age>=spec.perfect.ringMs||!isPerfect(e))return null;
  const x=canvas.width/2,y=canvas.height/2,maxRadius=Math.hypot(x,y)+spec.perfect.outerMarginPx;
  const reduced=$('reduce').checked;
  const rings=reduced?[{radius:28,alpha:.75,index:0}]:spec.perfect.ringDelayMs.flatMap((delay,index)=>{
   if(age<delay)return [];const p=clamp((age-delay)/(spec.perfect.ringMs-delay));
   // Keep a visible front until it has crossed the corners; no filled screen flash.
   return [{radius:12+(maxRadius-12)*Math.pow(p,.9),alpha:(index?.6:.82)*(1-.35*p),index}];
  });
  return {origin:[x,y],maxRadius,age,reduced,rings};
 }
 function perfect(e){
  const wave=perfectWaveState(e);if(!wave)return;const [x,y]=wave.origin;
  for(const r of wave.rings){
   ctx.beginPath();ctx.arc(x,y,r.radius,0,Math.PI*2);ctx.strokeStyle=r.index?cream:mint;
   if(!wave.reduced){ctx.lineWidth=r.index?10:16;ctx.globalAlpha=r.alpha*.12;ctx.stroke();}
   ctx.lineWidth=r.index?3:5;ctx.globalAlpha=r.alpha*(timingState()?.index>e.index?.45:1);ctx.stroke();
  }
  ctx.globalAlpha=1;
  if(wave.age<320){ctx.fillStyle=mint;ctx.font='bold 22px Nanum, sans-serif';ctx.textAlign='center';ctx.fillText(e.attack.response==='parry'?'완벽 쳐내기':'완벽',x,y+34*canvas.height/540);ctx.textAlign='left';}
 }
 function drawHero(h){
  if(h.parrying){
   const p=h.parry,progress=p.impactAge<0?clamp((time-p.input.time)/Math.max(1,p.hit-p.input.time)):1;
   const frame=p.impactAge<0?Math.floor(progress*7):Math.min(13,7+Math.floor(p.impactAge/180*7));
   sprite(images['attack'+attackFrames[frame]],375,442,1.25);
   if(p.impactAge>=-50&&p.impactAge<100){ctx.strokeStyle=cream;ctx.lineWidth=4;ctx.beginPath();ctx.ellipse(414,356,46,52,-.3,Math.PI*.85,Math.PI*1.85);ctx.stroke();}
   return;
  }
  if(!h.counter){sprite(h.amount>.25?images[h.direction]:images.hero,h.x,h.y,1.25);return;}
  const age=h.counterAge,i=age<120?0:age<200?Math.min(6,Math.floor((age-120)/80*7)):Math.min(13,7+Math.floor((age-200)/300*7));sprite(images['attack'+attackFrames[i]],h.x,h.y,1.25);
  if(age<80&&!$('reduce').checked){const d=dodge(counterStart()),off=spec.dodgeOffsets[d.event.attack.dodge];line(375+off[0]*d.amount,405+off[1]*d.amount,375,405,mint,2,.5*(1-age/80));}
  if(age>=200&&age<460)burst(620,365,age-200,mint,22);
  if(age>=160&&age<260&&!$('reduce').checked){const p=(age-160)/100;ctx.strokeStyle=mint;ctx.lineWidth=5;ctx.beginPath();ctx.ellipse(580,365,66,56,-.5,Math.PI*.8+p*.4,Math.PI*1.5+p*.9);ctx.stroke();}
 }
 function drawEnemy(state){
  const e=[...sequence()].reverse().find(e=>time>=e.launch),low=$('reduce').checked;let index=0,x=675,y=442;
  if(e){const age=time-e.launch;if(age<e.flight){index=Math.min(9,4+Math.floor(age/e.flight*6));x-=low?0:ease(age/e.flight)*10;}else if(age<e.flight+260){index=Math.min(16,10+Math.floor((age-e.flight)/260*7));x-=low?0:10*(1-clamp((age-e.flight)/200));}}
  else if(state.kind==='cue'){index=state.blink<3?0:3;y+=low?0:state.blink*1.2;}else if(time>=sequence().at(-1).cueEnd&&time<sequence()[0].launch)index=3;sprite(images['enemy'+index],x,y,1.9,78);
 }
 function drawTimingAssist(){
  const state=timingState(),scale=canvas.height/540,cx=canvas.width/2,cy=canvas.height/2;
  const recent=sequence().some(e=>time>=e.impact&&time<e.impact+45),color=spec.timingAssist.color;
  if(!state&&!recent)return;
  ctx.save();
  const fronts=state?[{...state,alpha:1}]:[];
  for(const event of sequence())if(time>event.impact&&time<event.impact+spec.timingAssist.contactAfterglowMs)
   fronts.push({...timingState(event.impact),alpha:1-(time-event.impact)/spec.timingAssist.contactAfterglowMs});
  for(const front of fronts){
   if(front.radius<=0)continue;
   const reduced=$('reduce').checked;
   for(const [sourceX,sourceY] of front.origins){
    // Off-screen emission points never move. Growing full circles create the visible wavefront.
    for(let echo=reduced?0:1;echo>=0;echo--){
     const radius=front.radius-echo*spec.timingAssist.echoRadiusGapPx*scale;if(radius<=0)continue;
     ctx.beginPath();ctx.arc(sourceX,sourceY,radius,0,Math.PI*2);ctx.strokeStyle=color;
     if(!reduced&&echo===0){ctx.lineWidth=spec.timingAssist.haloStrokePx*scale;ctx.globalAlpha=.09*front.alpha;ctx.stroke();}
     ctx.lineWidth=(echo?2:spec.timingAssist.primaryStrokePx)*scale;ctx.globalAlpha=(echo?.18:.86)*front.alpha;ctx.stroke();
    }
   }
  }
  // A target/contact mark describes timing. Only confirmed perfect() shows success.
  ctx.globalAlpha=1;ctx.lineWidth=2*scale;ctx.strokeStyle=color;
  ctx.beginPath();ctx.moveTo(cx,cy-7*scale);ctx.lineTo(cx+7*scale,cy);ctx.lineTo(cx,cy+7*scale);ctx.lineTo(cx-7*scale,cy);ctx.closePath();ctx.stroke();
  if(state?.perfectWindow||recent){
   for(const [sx,sy] of [[-1,-1],[1,-1],[-1,1],[1,1]]){
    line(cx+sx*13*scale,cy+sy*7*scale,cx+sx*13*scale,cy+sy*13*scale,color,2*scale);
    line(cx+sx*13*scale,cy+sy*13*scale,cx+sx*7*scale,cy+sy*13*scale,color,2*scale);
   }
  }
  ctx.restore();
 }
 function drawBells(){
  const bells=bellState(),scale=canvas.height/540,active=timingState();ctx.save();
  for(const bell of bells.visible){
   ctx.save();ctx.translate(bell.x,bell.y);ctx.scale(scale,scale);
   const next=active?.index===bell.index;
   // Stepped silhouette, crown, flared skirt, and hanging clapper: readable without a numeral.
   ctx.fillStyle='#142638';ctx.fillRect(-19,-23,38,46);
   ctx.beginPath();ctx.moveTo(-3,-17);ctx.lineTo(3,-17);ctx.lineTo(3,-13);ctx.lineTo(8,-11);
   ctx.lineTo(10,-7);ctx.lineTo(10,5);ctx.lineTo(14,9);ctx.lineTo(14,12);ctx.lineTo(-14,12);
   ctx.lineTo(-14,9);ctx.lineTo(-10,5);ctx.lineTo(-10,-7);ctx.lineTo(-8,-11);ctx.lineTo(-3,-13);ctx.closePath();
   ctx.fillStyle=next?cream:gold;ctx.fill();ctx.fillStyle=next?'#fff2d2':'#f6d493';ctx.fillRect(-3,14,6,3);
   ctx.fillStyle='#a27d40';ctx.fillRect(-7,-6,3,11);
   if(next){line(-18,-14,-18,10,cream,2,.9);line(18,-14,18,10,cream,2,.9);}
   ctx.restore();
  }
  ctx.restore();
 }
 function controls(){for(const [x,y,dir] of [[120,344,0],[48,416,1],[120,488,2],[192,416,3]])triangle(x,y,22,dir,cream);fill(836,410,104,104,'#21394d');ctx.strokeStyle='#b49350';ctx.lineWidth=2;ctx.strokeRect(837,411,102,102);line(868,482,910,438,cream,7);line(866,468,881,482,cream,5);fill(896,12,52,52,'#162a3f');ctx.strokeStyle='#b49350';ctx.strokeRect(897,13,50,50);line(918,29,918,47,cream,3);line(926,29,926,47,cream,3);}
 function canCounter(){return sequence().some(e=>time>=e.impact&&time<e.impact+500&&isPerfect(e))&&time<counterStart();}
 function draw(){
  if(!ready)return;ctx.clearRect(0,0,canvas.width,canvas.height);ctx.save();ctx.scale(canvas.width/960,canvas.height/540);ctx.imageSmoothingEnabled=false;const state=stage(),q=sequence(),h=heroState();ctx.save();
  if(!$('reduce').checked){const impact=q.find(e=>time>=e.impact&&time<e.impact+90);if(impact){const p=(time-impact.impact)/90;ctx.translate(Math.sin(p*24)*3*(1-p),Math.cos(p*19)*1.5*(1-p));}}
  for(const name of ['sky','far','mid','near','floor'])ctx.drawImage(images[name],0,0,images[name].width*1.5,540);for(const e of q)box(e,state);drawEnemy(state);drawHero(h);warmup(state);for(const e of q)projectile(e);ctx.restore();ctx.restore();for(const e of q)perfect(e);drawTimingAssist();drawBells();ctx.save();ctx.scale(canvas.width/960,canvas.height/540);
  fill(335,448,80,9,'#23394c');fill(335,448,80,7,cream);fill(635,448,80,9,'#23394c');fill(635,448,80,7,'#de9d45');fill(245,83,470,18,'#273f56');fill(245,83,235,18,'#d7a343');ctx.fillStyle=cream;ctx.font='24px Nanum, sans-serif';ctx.fillText('Stage 8',20,40);controls();ctx.restore();practiceStatus();for(const id of ['dodge-test','back-test','parry-test','forward-test'])$(id).disabled=!$('practice').checked||!playing;$('auto-dodge').disabled=$('practice').checked;$('auto-counter').disabled=$('practice').checked;
  $('elapsed').value=`${Math.round(time)} / ${total()}ms`;$('scrub').value=Math.round(time);$('phase').value=h.parrying?'제자리에서 올려 베기 → 상단 공격 쳐내기':h.counter?'즉시 원점 복귀 → 접근 → 반격':state.kind==='cue'?`${state.event.index+1}번 ${state.event.attack.name} · ${state.blink}/3 · ${state.bright?'밝음':'흐림'}`:state.kind==='flight'?`${state.event.index+1}번 공격 비행`:state.kind==='impact'?`${state.event.index+1}번 타격`:'다음 동작 대기';$('counter').disabled=$('practice').checked?!playing:!canCounter();$('parry-test').disabled=!$('practice').checked||!playing;$('motion-note').textContent=$('reduce').checked?'연출 줄이기 · 큰 원형 타이밍 파동은 유지, 번짐·잔상·성공 확산·흔들림 제거':'화면 밖 두 원점에서 퍼진 큰 원이 중앙에 맞닿을 때 회피 · 금빛 종 = 남은 공격 · 청록 확산 = 완벽 회피 성공';$('bell-count').textContent=`남은 종 ${bellState().remaining} / ${q.length}`;
 }
 function describe(){
  const q=sequence();$('attack-description').textContent=q.map(e=>e.attack.name).join(' → ');$('shape-description').textContent=q.map(e=>e.attack.anchorRule).join(' ');for(const b of document.querySelectorAll('[data-attack]'))b.setAttribute('aria-pressed',String(q.length===1&&b.dataset.attack===selected.id));
  const points=new Set([0,total()]);for(const e of q){for(let i=0;i<6;i++)points.add(e.cueStart+i*e.beat/2);points.add(e.cueEnd);points.add(e.launch);points.add(e.impact);points.add(e.impact+40);}
  const times=[...points].sort((a,b)=>a-b);let html='';
  for(let i=0;i<times.length-1;i++){
   const start=times[i],end=times[i+1],mid=(start+end)/2,cue=q.find(e=>mid>=e.cueStart&&mid<e.cueEnd),flight=q.find(e=>mid>=e.launch&&mid<e.impact),hit=q.find(e=>mid>=e.impact&&mid<e.impact+40);
   const blink=cue?Math.floor((mid-cue.cueStart)/cue.beat)+1:0,bright=cue&&((mid-cue.cueStart)/cue.beat)%1<.5;
   const type=hit?'hit':flight?'flight':bright?'on':cue?'off':'rest',label=hit?'타격':flight?'발사':bright?`${cue.index+1}·${blink}`:'';
   html+=`<span class="${type}" style="flex:${end-start}">${label}</span>`;
  }
  $('timeline').innerHTML=html;$('timeline').setAttribute('aria-label',q.map(e=>`${e.attack.name} 점멸 ${e.cueStart},${e.cueStart+e.beat},${e.cueStart+e.beat*2}ms · 타격 ${e.impact}ms`).join('; '));$('scrub').max=total();
  $('exposure-note').textContent=`${q[0].beat}ms 박자 · 3회 점멸 뒤 다음 박자에 접촉 · 연타 ${q[0].beat*2}ms 간격`;

 }
 function setPlaying(v){playing=v;$('play').textContent=v?'일시정지':'재생';last=performance.now();}
 function setTime(v){time=clamp(Number(v),0,total());draw();}
 function reset(){practiceInputs=[];lastPracticeInput=-Infinity;$('practice-result').textContent='상단은 아래 회피(S/↓), 이동 회피는 W/A/D·방향키. Space는 공격이며 쳐내기를 하지 않습니다.';time=0;manualCounter=null;describe();draw();}
 function select(id){selected=spec.attacks.find(a=>a.id===id)||selected;$('pattern').value='single';reset();}
 function attack(){if($('practice').checked){practiceAttack();return;}if(canCounter()){manualCounter=time;draw();}}
 function showWave(){$('practice').checked=false;practiceInputs=[];manualCounter=null;$('auto-dodge').checked=true;$('reduce').checked=false;time=sequence().at(-1).impact;setPlaying(true);draw();}
 function tick(now){if(playing){time+=Math.max(0,now-last)*Number($('speed').value);if(time>total()){if($('repeat').checked)reset();else{time=total();setPlaying(false);}}}last=now;draw();requestAnimationFrame(tick);}
 $('play').onclick=()=>setPlaying(!playing);$('restart').onclick=()=>{reset();setPlaying(true);};$('duration').onchange=()=>{duration=Number($('duration').value);reset();};$('pattern').onchange=reset;$('exposure').onchange=reset;$('scrub').oninput=()=>{setPlaying(false);manualCounter=null;practiceInputs=[];lastPracticeInput=-Infinity;setTime($('scrub').value);};$('auto-dodge').onchange=()=>{manualCounter=null;draw();};$('auto-counter').onchange=()=>{manualCounter=null;draw();};$('reduce').onchange=draw;$('counter').onclick=attack;$('wave-demo').onclick=showWave;
 $('speed').onchange=()=>{last=performance.now();};
 $('practice').onchange=()=>{if($('practice').checked){$('auto-dodge').checked=false;$('auto-counter').checked=false;}else{$('auto-dodge').checked=true;}reset();setPlaying(true);if($('practice').checked)canvas.focus?.({preventScroll:true});};
 $('dodge-test').onclick=()=>practiceDodge('up');$('back-test').onclick=()=>practiceDodge('back');$('parry-test').onclick=practiceDown;$('forward-test').onclick=()=>practiceDodge('forward');
 $('timing-demo').onclick=()=>{reset();time=sequence()[0].impact-duration/3*2;setPlaying(true);draw();};
 $('back-demo').onclick=()=>{select('thrust');$('auto-dodge').checked=true;$('auto-counter').checked=false;$('practice').checked=false;time=duration-80;setPlaying(true);draw();};
 $('attack-demo').onclick=()=>{select('thrust');$('auto-dodge').checked=true;$('auto-counter').checked=true;$('practice').checked=false;time=duration;setPlaying(true);draw();};
 $('viewport').onchange=()=>{const [w,h]=$('viewport').value.split('x').map(Number);canvas.width=w;canvas.height=h;draw();};
 document.addEventListener('keydown',e=>{const keys={KeyW:'up',ArrowUp:'up',KeyA:'back',ArrowLeft:'back',KeyS:'down',ArrowDown:'down',KeyD:'forward',ArrowRight:'forward',Space:'attack'};const action=keys[e.code];if(action&&$('practice').checked&&!e.repeat&&!['SELECT','INPUT','BUTTON','TEXTAREA','A'].includes(e.target?.tagName)){e.preventDefault();practiceInput(action);}});
 canvas.addEventListener('pointerdown',e=>{const b=canvas.getBoundingClientRect(),x=(e.clientX-b.left)/b.width*960,y=(e.clientY-b.top)/b.height*540;if(x>=836&&y>=410&&y<=514)attack();else if($('practice').checked){for(const [cx,cy,dir] of [[120,344,'up'],[48,416,'back'],[120,488,'down'],[192,416,'forward']])if(Math.abs(x-cx)<=32&&Math.abs(y-cy)<=32){practiceInput(dir);break;}}});for(const b of document.querySelectorAll('[data-attack]'))b.onclick=()=>select(b.dataset.attack);document.addEventListener('visibilitychange',()=>{if(document.hidden)setPlaying(false);last=performance.now();});
 window.hitboxPreview={select,showWave,practiceDodge,practiceDown,practiceAttack,setTime(v){setPlaying(false);setTime(v);},setPattern(v){$('pattern').value=v;reset();},attack,pause(){setPlaying(false);},play(){setPlaying(true);},getState(){const q=sequence(),s=stage(),h=heroState();return {ready,version:spec.version,time,duration,playing,exposure:$('exposure').value,timingGauge:false,timingAssist:timingState(),bells:bellState(),practiceInputs:practiceInputs.map(p=>({...p})),crouchControl:false,downParryControl:true,perfectWaves:q.map(perfectWaveState).filter(Boolean),sequence:q.map(e=>({attack:e.attack.id,cueStart:e.cueStart,cueEnd:e.cueEnd,slotEnd:e.slotEnd,launch:e.launch,impact:e.impact,rect:e.attack.rect,response:e.attack.response,parried:isParried(e),intersectsAtImpact:overlaps(bodyAtImpact(e),e.attack.rect)})),kind:s.kind,blink:s.blink,bright:s.bright,activeIndex:s.event.index,hero:h,counterStart:counterStart(),canCounter:canCounter(),reduced:$('reduce').checked,fillAlpha:$('reduce').checked?.12:s.bright?.2:.04};}};
 Promise.all(Object.entries(files).map(([name,url])=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{images[name]=im;resolve();};im.onerror=()=>reject(new Error(url));im.src=url;}))).then(()=>{ready=true;describe();setPlaying(playing);draw();requestAnimationFrame(tick);if(embedded){const report=()=>parent.postMessage({type:'lastbell-hitbox-preview-height',height:document.documentElement.scrollHeight},'*');new ResizeObserver(report).observe(document.querySelector('main'));report();}}).catch(error=>{$('phase').textContent='이미지를 불러오지 못했습니다.';console.error(error);});
})();
