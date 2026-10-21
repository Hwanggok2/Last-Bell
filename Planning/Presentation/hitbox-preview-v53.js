'use strict';
(() => {
 const spec=window.HITBOX_PREVIEW_SPEC,$=id=>document.getElementById(id),canvas=$('battle-preview'),ctx=canvas.getContext('2d');
 const embedded=new URLSearchParams(location.search).has('embed');if(embedded)document.documentElement.classList.add('embed');
 const base='../../Client/Assets/Game/Art/Presentation/',images={},gold=spec.color,cream='#efdfb8',mint='#9ee3d4',dark='#102035';
 const files={hero:'PixelLab/inputs/hero.png',up:base+'Clips/hero-dodge-up/04.png',down:base+'Clips/hero-dodge-down/04.png',back:base+'Clips/hero-dodge-back/08.png',forward:base+'Clips/hero-dodge-front/04.png'};
 for(const name of ['sky','far','mid','near','floor'])files[name]=base+'Background/gate-'+name+'.png';
 for(let i=0;i<17;i++)files['enemy'+i]=base+'Clips/knight-attack-retry/'+String(i).padStart(2,'0')+'.png';
 const attackFrames=[0,1,2,3,4,5,6,7,8,9,10,17,23,24];for(const i of attackFrames)files['attack'+i]=base+'Clips/hero-v6-slash-up/'+String(i).padStart(2,'0')+'.png';
 let selected=spec.attacks[0],duration=900,time=0,playing=true,ready=false,last=0,manualCounter=null;
 const reduced=matchMedia('(prefers-reduced-motion: reduce)');$('reduce').checked=reduced.matches;if(reduced.matches)playing=false;
 const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x)),ease=x=>1-(1-clamp(x))**3,lerp=(a,b,p)=>a+(b-a)*p;
 const patterns={'front-rear':['thrust','rear'],'rear-front':['rear','thrust'],'low-high':['low','high'],'high-low':['high','low']};
 function sequence(){const ids=patterns[$('pattern').value]||[selected.id],cue=duration-120,gap=duration===900?360:320;return ids.map((id,i)=>({attack:spec.attacks.find(a=>a.id===id),index:i,cueStart:i*cue,cueEnd:(i+1)*cue,launch:ids.length*cue+i*gap,impact:ids.length*cue+i*gap+120}));}
 function total(){return sequence().at(-1).impact+900;}
 function stage(t=time){
  const q=sequence(),cue=q.find(e=>t>=e.cueStart&&t<e.cueEnd);
  if(cue){const phase=(t-cue.cueStart)/((cue.cueEnd-cue.cueStart)/3),index=Math.floor(phase);return {kind:'cue',event:cue,blink:index+1,bright:phase-index<.5,fraction:phase-index};}
  const flight=q.find(e=>t>=e.launch&&t<e.impact);if(flight)return {kind:'flight',event:flight,progress:(t-flight.launch)/120};
  const hit=[...q].reverse().find(e=>t>=e.impact&&t<e.impact+260);if(hit)return {kind:'impact',event:hit,progress:(t-hit.impact)/260};
  const wait=q.find(e=>t<e.launch);return {kind:wait?'between':'recover',event:wait||q.at(-1)};
 }
 function dodge(t=time){
  if(!$('auto-dodge').checked)return {amount:0,event:sequence()[0]};
  const e=[...sequence()].reverse().find(e=>t>=e.impact-50)||sequence()[0],age=t-e.impact;
  return {event:e,amount:age< -50?0:age<0?ease((age+50)/50):age<100?1:1-ease((age-100)/130)};
 }
 function counterStart(){if(manualCounter!==null)return manualCounter;return $('auto-counter').checked&&$('auto-dodge').checked?sequence().at(-1).impact+90:Infinity;}
 function heroState(t=time){
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
 const isPerfect=e=>$('auto-dodge').checked&&impactHero(e).amount>.99&&!overlaps(bodyAtImpact(e),e.attack.rect);
 function fill(x,y,w,h,color,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.fillStyle=color;ctx.fillRect(Math.round(x),Math.round(y),Math.round(w),Math.round(h));ctx.globalAlpha=1;}
 function line(x1,y1,x2,y2,color,width=2,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();ctx.globalAlpha=1;}
 function ring(x,y,rx,ry,color,width=2,alpha=1){ctx.globalAlpha=clamp(alpha);ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.ellipse(x,y,Math.max(1,rx),Math.max(1,ry),0,0,Math.PI*2);ctx.stroke();ctx.globalAlpha=1;}
 function sprite(image,x,y,scale,pivotX=60,pivotY=114){ctx.drawImage(image,Math.round(x-pivotX*scale),Math.round(y-pivotY*scale),Math.round(128*scale),Math.round(128*scale));}
 function triangle(x,y,size,direction,color){ctx.save();ctx.translate(x,y);ctx.rotate([0,-Math.PI/2,Math.PI,Math.PI/2][direction]);ctx.fillStyle=color;ctx.beginPath();ctx.moveTo(0,-size);ctx.lineTo(size*.85,size*.75);ctx.lineTo(-size*.85,size*.75);ctx.closePath();ctx.fill();ctx.restore();}
 function box(e,state){
  if(time<e.cueStart||time>=e.impact)return;const active=state.kind==='cue'&&state.event.index===e.index,low=$('reduce').checked,r=e.attack.rect;
  fill(...r,gold,active?(low?.12:state.bright?.2:.04):time>=e.launch?.07:0);ctx.globalAlpha=active?(low?.46:state.bright?.62:.2):.14;ctx.strokeStyle=gold;ctx.lineWidth=2;ctx.strokeRect(r[0]+1,r[1]+1,r[2]-2,r[3]-2);ctx.globalAlpha=1;
  const length=active&&low?8+10*(1-state.fraction):10;for(const [x,y,sx,sy] of [[r[0],r[1],1,1],[r[0]+r[2],r[1],-1,1],[r[0],r[1]+r[3],1,-1],[r[0]+r[2],r[1]+r[3],-1,-1]]){line(x,y,x+length*sx,y,gold,2,active?.5:.2);line(x,y,x,y+length*sy,gold,2,active?.5:.2);}
 }
 const noise=i=>{const n=Math.sin(i*127.1+311.7)*43758.5453;return n-Math.floor(n);};
 function burst(x,y,age,color,count=24,dust=false){
  const p=age/(dust?340:260);if(p<0||p>=1)return;if($('reduce').checked){ring(x,y,18,18,color,2,.4*(1-p));return;}
  for(let i=0;i<count;i++){const a=dust?Math.PI+noise(i+8)*Math.PI:noise(i+8)*Math.PI*2,s=(dust?40:45)+noise(i+33)*(dust?60:120),dx=Math.cos(a)*s*p,dy=Math.sin(a)*s*p+(dust?55:40)*p*p,size=dust?3+noise(i)*5:2+noise(i)*3;
   if(dust)fill(x+dx,y+dy,size,size,'#9c937d',.3*(1-p));else line(x+dx,y+dy,x+dx-Math.cos(a)*(6+noise(i)*9),y+dy-Math.sin(a)*(6+noise(i)*9),i%3?color:cream,size*.55,(1-p)**1.4);}
 }
 function warmup(state){if(state.kind!=='cue'||$('reduce').checked)return;for(let i=0;i<8;i++){const p=(time*.0018+i/8)%1,a=i*2.4;fill(624+Math.cos(a)*28*(1-p),335+Math.sin(a)*23*(1-p),2,2,gold,.35*p);}}
 function attackPoint(e,p){const id=e.attack.id;return id==='rear'?{x:lerp(624,345,p),y:330-225*Math.sin(p*Math.PI)+96*p}:{x:lerp(624,375,p),y:id==='low'?423:id==='high'?332:374};}
 function projectile(e){
  const age=time-e.launch;if(age<0||age>460)return;const hitAge=time-e.impact,flight=age<120,p=flight?(age/120)**1.35:1,pt=attackPoint(e,p),low=$('reduce').checked;
  if(flight){
   if(!low)for(let i=5;i>=1;i--){const a=attackPoint(e,Math.max(0,p-i*.045));line(a.x+45,a.y,a.x,a.y,gold,10-i,.10+(5-i)*.045);}
   if(e.attack.id==='thrust'){line(pt.x+110,pt.y,pt.x+12,pt.y,dark,10);line(pt.x+110,pt.y,pt.x+12,pt.y,'#c49a56',6);line(pt.x+100,pt.y-2,pt.x+16,pt.y-2,cream,2);ctx.fillStyle=cream;ctx.beginPath();ctx.moveTo(pt.x,pt.y);ctx.lineTo(pt.x+24,pt.y-10);ctx.lineTo(pt.x+17,pt.y);ctx.lineTo(pt.x+24,pt.y+10);ctx.fill();if(!low)for(let i=0;i<4;i++)line(pt.x+35,pt.y-38+i*25,pt.x+125,pt.y-38+i*25,gold,2,.35);}
   else if(e.attack.id==='rear')for(const dx of [-52,0,52]){line(pt.x+dx+14,pt.y-65,pt.x+dx,pt.y,gold,8);line(pt.x+dx+13,pt.y-57,pt.x+dx,pt.y,cream,3);}
   else{ctx.strokeStyle=gold;ctx.lineWidth=10;ctx.beginPath();ctx.ellipse(pt.x+14,pt.y,23,e.attack.id==='low'?15:24,0,Math.PI*.62,Math.PI*1.38);ctx.stroke();ctx.strokeStyle=cream;ctx.lineWidth=3;ctx.stroke();line(pt.x+8,pt.y,pt.x+105,pt.y,gold,3,.7);}
  }else{
   const pt=attackPoint(e,1);if(hitAge<45&&!low){ring(pt.x,pt.y,12+hitAge*.6,7+hitAge*.3,cream,6,.8*(1-hitAge/45));line(pt.x-28,pt.y,pt.x+28,pt.y,cream,3,1-hitAge/45);}
   burst(pt.x,pt.y,hitAge,gold,e.attack.id==='rear'?30:24);burst(pt.x,442,hitAge,gold,14,true);if(!low&&hitAge<300)ring(pt.x,442,12+hitAge*.28,3+hitAge*.07,gold,2,.55*(1-hitAge/300));
   if(e.attack.id==='rear'&&!low&&hitAge<140)for(let i=0;i<5;i++)line(265+i*28,440,265+i*28,440-(1-hitAge/140)*(24+noise(i)*45),'#bea16a',3,.5*(1-hitAge/140));
  }
 }
 function perfect(e){
  const age=time-e.impact;if(age<0||age>=360||!isPerfect(e))return;const body=bodyAtImpact(e),x=body[0]+body[2]/2,y=body[1]+body[3]/2;
  if($('reduce').checked){ctx.strokeStyle=mint;ctx.lineWidth=2;ctx.strokeRect(x-20,y-20,40,40);}else{for(let i=0;i<2;i++){const p=clamp((age-i*45)/(360-i*45));if(p>0)ring(x,y,8+ease(p)*spec.perfect.ringRadiiPx[i],8+ease(p)*spec.perfect.ringRadiiPx[i],i?cream:mint,i?2:4,(1-p)*.7);}burst(x,y,age,mint,12);}
  if(age<320){ctx.fillStyle=mint;ctx.font='bold 22px Nanum, sans-serif';ctx.textAlign='center';ctx.fillText('완벽',x,y-76);ctx.textAlign='left';}
 }
 function drawHero(h){
  if(!h.counter){sprite(h.amount>.25?images[h.direction]:images.hero,h.x,h.y,1.25);return;}
  const age=h.counterAge,i=age<120?0:age<200?Math.min(6,Math.floor((age-120)/80*7)):Math.min(13,7+Math.floor((age-200)/300*7));sprite(images['attack'+attackFrames[i]],h.x,h.y,1.25);
  if(age<80&&!$('reduce').checked){const d=dodge(counterStart()),off=spec.dodgeOffsets[d.event.attack.dodge];line(375+off[0]*d.amount,405+off[1]*d.amount,375,405,mint,2,.5*(1-age/80));}
  if(age>=200&&age<460)burst(620,365,age-200,mint,22);
  if(age>=160&&age<260&&!$('reduce').checked){const p=(age-160)/100;ctx.strokeStyle=mint;ctx.lineWidth=5;ctx.beginPath();ctx.ellipse(580,365,66,56,-.5,Math.PI*.8+p*.4,Math.PI*1.5+p*.9);ctx.stroke();}
 }
 function drawEnemy(state){
  const e=[...sequence()].reverse().find(e=>time>=e.launch),low=$('reduce').checked;let index=0,x=675,y=442;
  if(e){const age=time-e.launch;if(age<120){index=Math.min(9,4+Math.floor(age/120*6));x-=low?0:ease(age/120)*10;}else if(age<380){index=Math.min(16,10+Math.floor((age-120)/260*7));x-=low?0:10*(1-clamp((age-120)/200));}}
  else if(state.kind==='cue'){index=state.blink<3?0:3;y+=low?0:state.blink*1.2;}sprite(images['enemy'+index],x,y,1.9,78);
 }
 function controls(){for(const [x,y,dir] of [[120,344,0],[48,416,1],[120,488,2],[192,416,3]])triangle(x,y,22,dir,cream);fill(836,410,104,104,'#21394d');ctx.strokeStyle='#b49350';ctx.lineWidth=2;ctx.strokeRect(837,411,102,102);line(868,482,910,438,cream,7);line(866,468,881,482,cream,5);fill(896,12,52,52,'#162a3f');ctx.strokeStyle='#b49350';ctx.strokeRect(897,13,50,50);line(918,29,918,47,cream,3);line(926,29,926,47,cream,3);}
 function canCounter(){return sequence().some(e=>time>=e.impact&&time<e.impact+500&&isPerfect(e))&&time<counterStart();}
 function draw(){
  if(!ready)return;ctx.clearRect(0,0,960,540);ctx.imageSmoothingEnabled=false;const state=stage(),q=sequence(),h=heroState();ctx.save();
  if(!$('reduce').checked){const impact=q.find(e=>time>=e.impact&&time<e.impact+90);if(impact){const p=(time-impact.impact)/90;ctx.translate(Math.sin(p*24)*3*(1-p),Math.cos(p*19)*1.5*(1-p));}}
  for(const name of ['sky','far','mid','near','floor'])ctx.drawImage(images[name],0,0,images[name].width*1.5,540);for(const e of q)box(e,state);drawEnemy(state);drawHero(h);warmup(state);for(const e of q)projectile(e);for(const e of q)perfect(e);ctx.restore();
  fill(335,448,80,9,'#23394c');fill(335,448,80,7,cream);fill(635,448,80,9,'#23394c');fill(635,448,80,7,'#de9d45');fill(245,83,470,18,'#273f56');fill(245,83,235,18,'#d7a343');ctx.fillStyle=cream;ctx.font='24px Nanum, sans-serif';ctx.fillText('Stage 8',20,40);controls();
  $('elapsed').value=`${Math.round(time)} / ${total()}ms`;$('scrub').value=Math.round(time);$('phase').value=h.counter?'즉시 원점 복귀 → 접근 → 반격':state.kind==='cue'?`${state.event.index+1}번 ${state.event.attack.name} · ${state.blink}/3 · ${state.bright?'밝음':'흐림'}`:state.kind==='flight'?`${state.event.index+1}번 공격 비행`:state.kind==='impact'?`${state.event.index+1}번 타격`:'다음 동작 대기';$('counter').disabled=!canCounter();
 }
 function describe(){
  const q=sequence();$('attack-description').textContent=q.map(e=>e.attack.name).join(' → ');$('shape-description').textContent=q.map(e=>e.attack.anchorRule).join(' ');for(const b of document.querySelectorAll('[data-attack]'))b.setAttribute('aria-pressed',String(q.length===1&&b.dataset.attack===selected.id));
  let html=q.map(e=>Array.from({length:3},(_,i)=>`<span class="on" style="flex:${(duration-120)/6}">${e.index+1}·${i+1}</span><span class="off" style="flex:${(duration-120)/6}"></span>`).join('')).join('');for(const e of q){html+=`<span class="flight" style="flex:120">${e.index+1} 발사</span><span class="hit" style="flex:50">타격</span>`;if(e.index<q.length-1)html+=`<span class="rest" style="flex:${q[e.index+1].launch-e.impact-50}"></span>`;}html+='<span class="rest" style="flex:850">회피 · 반격</span>';$('timeline').innerHTML=html;$('timeline').setAttribute('aria-label',q.map(e=>`${e.attack.name} 세 번 점멸 ${e.cueStart}~${e.cueEnd}ms, 타격${e.impact}ms`).join('; '));$('scrub').max=total();
 }
 function setPlaying(v){playing=v;$('play').textContent=v?'일시정지':'재생';last=performance.now();}
 function setTime(v){time=clamp(Number(v),0,total());draw();}
 function reset(){time=0;manualCounter=null;describe();draw();}
 function select(id){selected=spec.attacks.find(a=>a.id===id)||selected;$('pattern').value='single';reset();}
 function attack(){if(canCounter()){manualCounter=time;draw();}}
 function tick(now){if(playing){time+=Math.max(0,Math.min(100,now-last));if(time>total()){if($('repeat').checked)reset();else{time=total();setPlaying(false);}}}last=now;draw();requestAnimationFrame(tick);}
 $('play').onclick=()=>setPlaying(!playing);$('restart').onclick=()=>{reset();setPlaying(true);};$('duration').onchange=()=>{duration=Number($('duration').value);reset();};$('pattern').onchange=reset;$('scrub').oninput=()=>{setPlaying(false);manualCounter=null;setTime($('scrub').value);};$('auto-dodge').onchange=()=>{manualCounter=null;draw();};$('auto-counter').onchange=()=>{manualCounter=null;draw();};$('reduce').onchange=draw;$('counter').onclick=attack;
 canvas.addEventListener('pointerdown',e=>{const b=canvas.getBoundingClientRect(),x=(e.clientX-b.left)/b.width*960,y=(e.clientY-b.top)/b.height*540;if(x>=836&&y>=410&&y<=514)attack();});for(const b of document.querySelectorAll('[data-attack]'))b.onclick=()=>select(b.dataset.attack);document.addEventListener('visibilitychange',()=>{last=performance.now();});
 window.hitboxPreview={select,setTime(v){setPlaying(false);setTime(v);},setPattern(v){$('pattern').value=v;reset();},attack,pause(){setPlaying(false);},play(){setPlaying(true);},getState(){const q=sequence(),s=stage(),h=heroState();return {ready,version:spec.version,time,duration,playing,timingGauge:false,sequence:q.map(e=>({attack:e.attack.id,cueStart:e.cueStart,cueEnd:e.cueEnd,launch:e.launch,impact:e.impact,rect:e.attack.rect,intersectsAtImpact:overlaps(bodyAtImpact(e),e.attack.rect)})),kind:s.kind,blink:s.blink,bright:s.bright,activeIndex:s.event.index,hero:h,counterStart:counterStart(),canCounter:canCounter(),reduced:$('reduce').checked,fillAlpha:$('reduce').checked?.12:s.bright?.2:.04};}};
 Promise.all(Object.entries(files).map(([name,url])=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{images[name]=im;resolve();};im.onerror=()=>reject(new Error(url));im.src=url;}))).then(()=>{ready=true;describe();setPlaying(playing);draw();requestAnimationFrame(tick);if(embedded){const report=()=>parent.postMessage({type:'lastbell-hitbox-preview-height',height:document.documentElement.scrollHeight},'*');new ResizeObserver(report).observe(document.querySelector('main'));report();}}).catch(error=>{$('phase').textContent='이미지를 불러오지 못했습니다.';console.error(error);});
})();
