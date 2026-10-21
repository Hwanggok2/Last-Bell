'use strict';
(() => {
 const spec=window.HITBOX_PREVIEW_SPEC;
 const $=id=>document.getElementById(id),canvas=$('battle-preview'),ctx=canvas.getContext('2d');
 const embedded=new URLSearchParams(location.search).has('embed');
 if(embedded)document.documentElement.classList.add('embed');
 const images={},base='../../Client/Assets/Game/Art/Presentation/';
 const files={hero:'PixelLab/inputs/hero.png',enemy:'PixelLab/inputs/knight.png',up:base+'Clips/hero-dodge-up/04.png',down:base+'Clips/hero-dodge-down/04.png',back:base+'Clips/hero-dodge-back/08.png',forward:base+'Clips/hero-dodge-front/04.png'};
 for(const name of ['sky','far','mid','near','floor'])files[name]=base+'Background/gate-'+name+'.png';
 let selected=spec.attacks[0],duration=spec.defaultImpactMs,time=0,playing=true,ready=false,last=0;
 const reduced=matchMedia('(prefers-reduced-motion: reduce)');$('reduce').checked=reduced.matches;
 if(reduced.matches)playing=false;
 const gold=spec.color,cream='#efdfb8',dark='#102035',mint='#9ee3d4';
 const clamp=(x,min=0,max=1)=>Math.max(min,Math.min(max,x));
 const ease=x=>1-(1-clamp(x))**2;
 function stage(t=time,d=duration){
  const launch=d-spec.flightMs,period=launch/spec.blinkCount;
  if(t<launch){const index=Math.min(2,Math.floor(t/period)),fraction=(t-index*period)/period;
   return {kind:'cue',blink:index+1,bright:fraction<spec.brightFraction,fraction,launch,period};}
  if(t<d)return {kind:'flight',blink:3,bright:false,progress:(t-launch)/spec.flightMs,launch,period};
  return {kind:t<d+spec.impactEffectMs?'impact':'recover',blink:3,bright:false,progress:(t-d)/spec.impactEffectMs,launch,period};
 }
 function dodgeAmount(t=time){
  if(!$('auto-dodge').checked)return 0;
  const age=t-(duration-50);
  return age<0?0:age<50?ease(age/50):age<160?1:1-ease((age-160)/150);
 }
 function body(t=time){
  const amount=dodgeAmount(t),r=[...spec.playerBody],off=spec.dodgeOffsets[selected.dodge];
  if(selected.dodge==='down'){const c=spec.crouchedBody;for(let i=0;i<4;i++)r[i]+=(c[i]-r[i])*amount;}
  r[0]+=off[0]*amount;r[1]+=off[1]*amount;return r;
 }
 function overlaps(a,b){return a[0]<b[0]+b[2]&&a[0]+a[2]>b[0]&&a[1]<b[1]+b[3]&&a[1]+a[3]>b[1];}
 function fill(x,y,w,h,color,alpha=1){ctx.globalAlpha=alpha;ctx.fillStyle=color;ctx.fillRect(Math.round(x),Math.round(y),Math.round(w),Math.round(h));ctx.globalAlpha=1;}
 function line(x1,y1,x2,y2,color,width=2,alpha=1){ctx.globalAlpha=alpha;ctx.strokeStyle=color;ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();ctx.globalAlpha=1;}
 function triangle(x,y,size,direction,color){
  ctx.save();ctx.translate(x,y);ctx.rotate([0,-Math.PI/2,Math.PI,Math.PI/2][direction]);ctx.fillStyle=color;ctx.beginPath();ctx.moveTo(0,-size);ctx.lineTo(size*.85,size*.75);ctx.lineTo(-size*.85,size*.75);ctx.closePath();ctx.fill();ctx.restore();
 }
 function box(state){
  if(time>=duration)return;
  const rect=selected.rect,low=$('reduce').checked;
  const alpha=low?.12:state.bright?spec.fillAlpha.bright:spec.fillAlpha.dim;
  fill(...rect,gold,alpha);
  ctx.strokeStyle=gold;ctx.globalAlpha=low?.46:state.bright?spec.borderAlpha.bright:spec.borderAlpha.dim;ctx.lineWidth=spec.borderPx;
  ctx.strokeRect(rect[0]+1,rect[1]+1,rect[2]-2,rect[3]-2);ctx.globalAlpha=1;
  // Corners remain legible when the low-alpha interior overlaps the scene.
  const length=low&&state.kind==='cue'?8+10*(1-state.fraction):10;
  for(const [x,y,sx,sy] of [[rect[0],rect[1],1,1],[rect[0]+rect[2],rect[1],-1,1],[rect[0],rect[1]+rect[3],1,-1],[rect[0]+rect[2],rect[1]+rect[3],-1,-1]]){
   line(x,y,x+length*sx,y,gold,2,.5);line(x,y,x,y+length*sy,gold,2,.5);
  }
 }
 function sprite(image,x,y,scale,pivotX=60,pivotY=114){ctx.drawImage(image,Math.round(x-pivotX*scale),Math.round(y-pivotY*scale),Math.round(128*scale),Math.round(128*scale));}
 function lance(x,y,length=115){
  line(x+length,y,x+12,y,dark,9);line(x+length,y,x+12,y,'#a88143',5);line(x+length,y-1,x+12,y-1,cream,2);
  ctx.fillStyle=gold;ctx.beginPath();ctx.moveTo(x,y);ctx.lineTo(x+23,y-10);ctx.lineTo(x+17,y);ctx.lineTo(x+23,y+10);ctx.closePath();ctx.fill();
 }
 function projectile(state){
  if(state.kind==='cue'||state.kind==='recover')return;
  const p=state.kind==='flight'?clamp(state.progress):1,life=state.kind==='impact'?1-clamp(state.progress):1;
  ctx.save();ctx.globalAlpha=life;
  if(selected.id==='thrust'){
   const x=spec.enemyFrontX+(spec.playerAnchor[0]-spec.enemyFrontX)*p;
   for(let i=0;i<3;i++)line(x+25,344+i*30,Math.min(spec.enemyFrontX,x+110),344+i*30,gold,2,.28*life);
   lance(x,374);
  }else if(selected.id==='low'||selected.id==='high'){
   const y=selected.id==='low'?423:350,x=spec.enemyFrontX+(spec.playerAnchor[0]-spec.enemyFrontX)*p;
   line(x,y,Math.min(spec.enemyFrontX,x+118),y,gold,9,life);line(x+8,y-3,Math.min(spec.enemyFrontX,x+93),y-3,cream,3,life);
   line(x+6,y-12,x-3,y+11,gold,4,life);
  }else{
   for(const target of [280,335,389]){
    const x=(1-p)**2*spec.enemyFrontX+2*(1-p)*p*(target+40)+p*p*target;
    const y=(1-p)**2*330+2*(1-p)*p*45+p*p*430;
    line(x+12,y-40,x,y,gold,6,life);line(x+13,y-40,x+1,y-3,cream,2,life);
    ctx.fillStyle=gold;ctx.beginPath();ctx.moveTo(x,y+7);ctx.lineTo(x-8,y-10);ctx.lineTo(x+12,y-7);ctx.closePath();ctx.fill();
   }
  }
  if(state.kind==='impact'){
   const x=selected.id==='rear'?365:375,y=selected.id==='low'?425:selected.id==='high'?349:374;
   for(let i=0;i<6;i++){const a=i*Math.PI/3,r=10+state.progress*22;line(x+Math.cos(a)*r,y+Math.sin(a)*r,x+Math.cos(a)*(r+9),y+Math.sin(a)*(r+9),cream,2,life);}
  }
  ctx.restore();
 }
 function controls(){
  const buttons=[[120,344,0],[48,416,1],[120,488,2],[192,416,3]];
  for(const [x,y,dir] of buttons)triangle(x,y,22,dir,cream);
  fill(836,410,104,104,'#21394d');ctx.strokeStyle='#b49350';ctx.lineWidth=2;ctx.strokeRect(837,411,102,102);
  line(868,482,910,438,cream,7);line(866,468,881,482,cream,5);
  fill(896,12,52,52,'#162a3f');ctx.strokeStyle='#b49350';ctx.strokeRect(897,13,50,50);line(918,29,918,47,cream,3);line(926,29,926,47,cream,3);
 }
 function draw(){
  if(!ready)return;ctx.clearRect(0,0,960,540);ctx.imageSmoothingEnabled=false;
  for(const name of ['sky','far','mid','near','floor'])ctx.drawImage(images[name],0,0,images[name].width*1.5,540);
  const state=stage(),amount=dodgeAmount(),off=spec.dodgeOffsets[selected.dodge];
  sprite(images.enemy,675,442,1.9,64);
  const hero=amount>.25?images[selected.dodge]:images.hero;
  sprite(hero,375+off[0]*amount,442+off[1]*amount,1.25);
  box(state);projectile(state);
  fill(335,448,80,9,'#23394c');fill(335,448,80,7,cream);
  fill(635,448,80,9,'#23394c');fill(635,448,80,7,'#de9d45');
  fill(245,83,470,18,'#273f56');fill(245,83,235,18,'#d7a343');
  ctx.fillStyle=cream;ctx.font='24px Nanum, sans-serif';ctx.fillText('Stage 8',20,40);
  const [x,y,w,h]=spec.gaugeBounds;
  if(time<=duration){
   fill(x-2,y-2,w+4,h+4,dark);fill(x,y,w,h,'#2a4055');
   fill(x+w*(1-160/duration),y,w*100/duration,h,'#665337');fill(x+w*(1-60/duration),y,w*60/duration,h,'#336c65');
   fill(x,y+6,w*clamp(time/duration),4,cream);line(x+w,y-4,x+w,y+h+4,cream,2);
  }
  if(time>=duration&&time<duration+280&&$('auto-dodge').checked&&!overlaps(body(duration),selected.rect)){
   ctx.fillStyle=mint;ctx.font='22px Nanum, sans-serif';ctx.textAlign='center';ctx.fillText('완벽',375+off[0],218);ctx.textAlign='left';
  }
  controls();
  $('elapsed').value=`${Math.round(time)} / ${duration}ms`;$('scrub').value=Math.round(time);
  $('phase').value=state.kind==='cue'?`${state.blink}번째 · ${state.bright?'밝음':'흐림'}`:state.kind==='flight'?'세 번 완료 · 공격 비행':state.kind==='impact'?'타격 시점':'공격 종료';
 }
 function describe(){
  $('attack-description').textContent=selected.reviewLabel;
  $('shape-description').textContent=`${selected.anchorRule} ${selected.effect}.`;
  for(const button of document.querySelectorAll('[data-attack]'))button.setAttribute('aria-pressed',String(button.dataset.attack===selected.id));
  const launch=duration-spec.flightMs,period=launch/3,total=duration+450;
  $('timeline').innerHTML=Array.from({length:3},(_,i)=>`<span class="on" style="flex:${period/2}">${i+1}</span><span class="off" style="flex:${period/2}" aria-label="흐림"></span>`).join('')+`<span class="flight" style="flex:120">비행</span><span class="hit" style="flex:100">타격</span><span class="rest" style="flex:350">종료</span>`;
  $('timeline').setAttribute('aria-label',`밝음${period/2}ms 흐림${period/2}ms 세 번, 비행120ms, ${duration}ms에 타격`);
  $('scrub').max=total;
 }
 function setPlaying(value){playing=value;$('play').textContent=playing?'일시정지':'재생';last=performance.now();}
 function setTime(value){time=clamp(Number(value),0,duration+450);draw();}
 function select(id){selected=spec.attacks.find(a=>a.id===id)||selected;time=0;describe();draw();}
 function tick(now){if(playing){time+=Math.max(0,Math.min(100,now-last));if(time>duration+450){if($('repeat').checked)time=0;else{time=duration+450;setPlaying(false);}}}last=now;draw();requestAnimationFrame(tick);}
 $('play').onclick=()=>setPlaying(!playing);$('restart').onclick=()=>{time=0;setPlaying(true);draw();};
 $('duration').onchange=()=>{duration=Number($('duration').value);time=0;describe();draw();};
 $('scrub').oninput=()=>{setPlaying(false);setTime($('scrub').value);};
 $('auto-dodge').onchange=draw;$('reduce').onchange=draw;
 for(const button of document.querySelectorAll('[data-attack]'))button.onclick=()=>select(button.dataset.attack);
 document.addEventListener('visibilitychange',()=>{last=performance.now();});
 window.hitboxPreview={setTime(value){setPlaying(false);setTime(value);},select,pause(){setPlaying(false);},play(){setPlaying(true);},getState(){return {ready,attack:selected.id,duration,time,...stage(),body:body(),intersectsAtImpact:overlaps(body(duration),selected.rect),fillAlpha:$('reduce').checked?.12:stage().bright?spec.fillAlpha.bright:spec.fillAlpha.dim,rect:[...selected.rect],playing};}};
 Promise.all(Object.entries(files).map(([name,url])=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>{images[name]=img;resolve();};img.onerror=()=>reject(new Error(url));img.src=url;}))).then(()=>{
  ready=true;describe();setPlaying(playing);draw();requestAnimationFrame(tick);
  if(embedded){const report=()=>parent.postMessage({type:'lastbell-hitbox-preview-height',height:document.documentElement.scrollHeight},'*');new ResizeObserver(report).observe(document.querySelector('main'));report();}
 }).catch(error=>{$('phase').textContent='이미지를 불러오지 못했습니다.';$('phase').classList.add('error');console.error(error);});
})();
