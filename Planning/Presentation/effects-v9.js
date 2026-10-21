/* v3 drawing studies only. VFX never performs a damage roll or changes game state. */
function actor(id){return P.actors.find(a=>a.id===id)}
function sizeFrame(canvas,id='bell_knight',proposed=true){const ctx=canvas.getContext('2d');clear(ctx);const a=actor(id);world(ctx,{enemy:a.sprite,enemyHeight:proposed?a.proposed:a.current});hud(ctx,{stage:id==='bell_knight'?10:id==='gate_warden'?5:3,boss:id==='bell_knight'||id==='gate_warden'});}
function line(ctx,points,color,width=4){ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(px(x),px(y)):ctx.moveTo(px(x),px(y)));ctx.strokeStyle=color;ctx.lineWidth=width;ctx.lineJoin='miter';ctx.stroke()}
function spark(ctx,x,y,p=.5,big=false){for(let i=0;i<8;i++){const a=i*Math.PI/4,r=(big?42:24)*(1+p);const xx=x+Math.cos(a)*r,yy=y+Math.sin(a)*r,w=i%2?4:8;rect(ctx,xx-2,yy-2,w+4,w+4,C.dark);rect(ctx,xx,yy,w,w,i%2?C.gold:C.cream)}rect(ctx,x-11,y-11,22,22,C.dark);rect(ctx,x-7,y-7,14,14,C.cream)}
function rune(ctx,x,y,p){const r=12+24*(1-clamp(p));line(ctx,[[x-r,y-4],[x-r,y+4],[x-6,y+4]],C.gold,4);line(ctx,[[x+r,y-4],[x+r,y+4],[x+6,y+4]],C.gold,4);rect(ctx,x-3,y-3,6,6,C.cream)}
function spike(ctx,x,bottom,height,alpha=1){ctx.save();ctx.globalAlpha=alpha;const lean=P.telegraph.spikes.leanLeftPx,heightMax=Math.max(...P.telegraph.spikes.heightPx);for(let pass=0;pass<3;pass++)for(let y=0;y<height;y+=4){const f=y/height,w=Math.max(3,P.telegraph.spikes.baseWidthPx*(1-f)),cx=x-lean*f*height/heightMax;if(pass===0)rect(ctx,cx-w/2-3,bottom-y-4,w+6,6,C.dark);if(pass===1)rect(ctx,cx-w/2,bottom-y-4,w,4,C.gold);if(pass===2)rect(ctx,cx-w*.28,bottom-y-4,4,4,C.cream)}ctx.restore()}
function arrowGeometry(index,time,duration){const a=P.telegraph.rearArrows,p=clamp((time-duration+P.telegraph.rearFallMs)/P.telegraph.rearFallMs),fall=p*p;return{x:a.landingX[index]+a.originOffset[0]*(1-fall),y:a.landingY+a.originOffset[1]*(1-fall),p};}
function greatArrow(ctx,x,y,alpha=1){const a=P.telegraph.rearArrows,len=Math.hypot(...a.originOffset),ux=-a.originOffset[0]/len,uy=-a.originOffset[1]/len,nx=-uy,ny=ux,tail=[x-ux*a.lengthPx,y-uy*a.lengthPx];ctx.save();ctx.globalAlpha=alpha;line(ctx,[tail,[x-ux*20,y-uy*20]],C.dark,a.shaftPx+6);line(ctx,[tail,[x-ux*20,y-uy*20]],C.old,a.shaftPx);line(ctx,[tail,[x-ux*24,y-uy*24]],C.cream,3);const head=[[x,y],[x-ux*54+nx*a.headWidthPx/2,y-uy*54+ny*a.headWidthPx/2],[x-ux*42,y-uy*42],[x-ux*54-nx*a.headWidthPx/2,y-uy*54-ny*a.headWidthPx/2],[x,y]];ctx.beginPath();head.forEach(([xx,yy],i)=>i?ctx.lineTo(px(xx),px(yy)):ctx.moveTo(px(xx),px(yy)));ctx.fillStyle=C.gold;ctx.fill();line(ctx,head,C.dark,4);line(ctx,[[x-ux*8,y-uy*8],[x-ux*43,y-uy*43]],C.cream,5);for(const sign of [-1,1])line(ctx,[[tail[0]+nx*sign*22,tail[1]+ny*sign*22],[tail[0]+ux*28,tail[1]+uy*28]],C.gold,8);ctx.restore();}
function dangerEffect(ctx,id,time,duration=650){const before=time<duration,p=clamp(time/duration),after=time-duration,life=before?1:clamp(1-after/140);ctx.save();
 if(id==='high_sweep')ctx.translate(0,P.telegraph.highSweepOffsetY||0);
 if(id==='low_sweep'){
  line(ctx,[[430,588],[454,584],[478,590],[502,582],[526,588],[554,584],[580,588]],C.dark,10);line(ctx,[[430,588],[454,584],[478,590],[502,582],[526,588],[554,584],[580,588]],C.gold,5);
  if(before){P.telegraph.spikes.basesX.forEach(x=>{rune(ctx,x,584,p);rect(ctx,x-3,573,6,6,C.gold)});}else if(life>0){P.telegraph.spikes.heightPx.forEach((h,i)=>spike(ctx,P.telegraph.spikes.basesX[i],590,h*(.45+.55*clamp(after/40)),life));}
 }else if(id==='outer_arc'){
  P.telegraph.rearArrows.landingX.forEach((x,i)=>{if(before){rune(ctx,x,585,p);line(ctx,[[x+38,522],[x+16,559]],C.gold,3);const a=arrowGeometry(i,time,duration);if(a.p>0){line(ctx,[[a.x+34,a.y-66],[a.x+70,a.y-136]],'#71849a',3);greatArrow(ctx,a.x,a.y)}}else if(life>0){greatArrow(ctx,x,590,life);spark(ctx,x,585,clamp(after/140),false)}});
 }else if(id==='high_sweep'){
  if(before){line(ctx,[[430,485],[480,481],[525,485],[580,479],[620,483]],C.dark,10);line(ctx,[[430,485],[480,481],[525,485],[580,479],[620,483]],C.gold,4);rune(ctx,mix(620,450,p),480,p)}
  else if(life>0){ctx.globalAlpha=life;line(ctx,[[810,476],[750,466],[665,466],[610,474],[555,484],[493,489],[430,482]],C.dark,25);line(ctx,[[810,476],[750,466],[665,466],[610,474],[555,484],[493,489],[430,482]],C.gold,17);line(ctx,[[780,470],[665,470],[555,488],[450,488]],C.cream,5)}
 }else{
  if(before){line(ctx,[[810,525],[735,525],[660,525],[585,525]],C.old,3);rune(ctx,mix(790,565,p),525,p)}else if(life>0){ctx.globalAlpha=life;line(ctx,[[820,525],[555,525]],C.dark,20);line(ctx,[[820,525],[555,525]],C.old,10);for(let i=0;i<7;i++)rect(ctx,536+i*8,523-i*2,8,4+i*4,i<2?C.cream:C.gold);spark(ctx,545,525,clamp(after/140))}
 }
 ctx.restore();return before?'준비 자세 + 위험 재료 · '+Math.round(p*100)+'%':after<10?'T · 판정 한 번, 타격 VFX 시작':after<140?'타격 뒤 잔해 · 추가 피해 없음':'잔해 퇴장 · 다음 동작 준비';
}
function warningFrame(canvas,id='low_sweep',time=690,duration=650,success=true,mono=false,wire=false){const ctx=canvas.getContext('2d');clear(ctx);ctx.save();if(mono)ctx.filter='grayscale(1)';background(ctx);const after=time-duration,impact=after>=0&&after<220;let hx=500,foot=590,pose='on_00';
 if(success&&impact){if(id==='low_sweep'){pose='on_04';foot-=after<140?P.telegraph.successfulUpDodgeVisual.footLiftPx:P.telegraph.successfulUpDodgeVisual.footLiftPx*(1-clamp((after-140)/80))}if(id==='high_sweep')pose='on_05';if(id==='thrust'){pose='on_06';hx-=70}if(id==='outer_arc'){pose='on_07';hx+=P.telegraph.rearArrows.forwardDodgeOffsetPx}}
 const dodgeId=selectedDodge({low_sweep:'up',high_sweep:'down',thrust:'back',outer_arc:'front'}[id]);
 if(success&&impact&&clipAvailable(dodgeId)){let index=clipIndex(dodgeId,Math.max(0,after)+60,280);if(id==='high_sweep'){const order=after<140?PL_BY_ID[dodgeId].lowPoseFrames:PL_BY_ID[dodgeId].recoverFrames;if(order)index=order[Math.min(order.length-1,Math.floor(clamp((after<140?after:after-140)/(after<140?140:80))*order.length))]}drawClip(ctx,dodgeId,index,hx,foot,1.1);}else if(!success&&impact)HitFlashV7.drawSprite(ctx,'on_00',hx,foot,1.1,after);else sprite(ctx,'on_00',hx,foot,1.1);
 const enemyClip=id==='outer_arc'?'sentinel-attack':'wisp-attack';
 if(clipAvailable(enemyClip))drawClip(ctx,enemyClip,strikeIndex(enemyClip,time,duration,duration+240),900,590,2.0);else sprite(ctx,id==='outer_arc'?'gate_00':'gate_02',900,590,2.0);
 // Placeholder enemy hand/source light ties the effect to the caster's held pose.
 if(time<duration){rune(ctx,832,464,clamp(time/duration));line(ctx,[[830,463],[790,453],[766,463]],C.gold,3)}
 const label=dangerEffect(ctx,id,time,duration);hud(ctx,{stage:id==='outer_arc'?4:3});if(wire){ctx.strokeStyle='#bfcde0';ctx.lineWidth=2;ctx.setLineDash([8,8]);ctx.strokeRect(...P.telegraph.regions.find(r=>r.id===id).bounds);ctx.setLineDash([]);ctx.strokeRect(447,608,106,10)}ctx.restore();return label;
}
const DIGITS={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['111','001','111','100','111'],'3':['111','001','111','001','111'],'4':['101','101','111','001','001'],'5':['111','100','111','001','111'],'6':['111','100','111','101','111'],'7':['111','001','010','010','010'],'8':['111','101','111','101','111'],'9':['111','101','111','001','111']};
function damageNumber(ctx,n,x,y,scale){String(n).split('').forEach((v,i)=>DIGITS[v].forEach((row,yy)=>[...row].forEach((b,xx)=>{if(b==='1'){rect(ctx,x+(i*4+xx)*scale-2,y+yy*scale-2,scale+4,scale+4,C.dark);rect(ctx,x+(i*4+xx)*scale,y+yy*scale,scale,scale,C.cream)}})))}
function playerFrame(canvas,time=220,critical=true,targetId='gate_sentinel',variantIndex=strikeVariantIndex){
 const ctx=canvas.getContext('2d');clear(ctx);background(ctx);const contact=P.playerAttack.windupMs,after=time-contact,g=slashGeometry(targetId,variantIndex);ctx.save();
 if(critical&&after>=0&&after<80){const strength=3*(1-after/80);ctx.translate(Math.round(Math.sin(after*.3)*strength),Math.round(Math.cos(after*.3)*strength))}
 drawMonsterHit(ctx,targetId,after);const pose=drawPlayerAttack(ctx,time,targetId,variantIndex);drawDiagonalSlash(ctx,after+25,critical,targetId,variantIndex);
 if(after>=0&&after<250){const p=after/250;spark(ctx,...g.hit,p,critical);debris(ctx,{left:g.hit[0]-20,top:g.hit[1]-20,w:40,h:65},p+.1,false)}ctx.restore();
 if(after>=0&&after<350){ctx.globalAlpha=clamp((350-after)/100);damageNumber(ctx,critical?128:64,g.hit[0]-65,Math.min(285,g.hit[1]-90)-after*.08,critical?9:6);ctx.globalAlpha=1}
 hud(ctx,{stage:targetId==='bell_knight'?10:targetId==='gate_warden'?5:1,boss:['bell_knight','gate_warden'].includes(targetId),playerHp:false});meter(ctx,pose.rootX-53,608,106,10,.8,C.cream);
 return{phase:pose.phase,variant:g.variant.id,rootX:pose.rootX,targetId,hit:g.hit};
}
function arch(ctx,x,y,w,h,color){const t=20;rect(ctx,x,y+40,t,h-40,color);rect(ctx,x+w-t,y+40,t,h-40,color);rect(ctx,x+t,y+20,w-2*t,20,color);rect(ctx,x+2*t,y,w-4*t,20,color);rect(ctx,x,y+35,40,20,color);rect(ctx,x+w-40,y+35,40,20,color)}
function layerScene(ctx,travel=0,guides=false){
 rect(ctx,0,0,1280,720,'#233d60');const factors=P.parallax.layers.map(l=>l.factor);
 // Moon/sky layer.
 rect(ctx,1040-travel*factors[0],90,36,56,'#d8ac55');rect(ctx,1054-travel*factors[0],82,36,56,'#233d60');
 for(let i=-1;i<10;i++){const x=i*180-travel*factors[1],top=270+(i*53%110);rect(ctx,x,top,90,600-top,'#1d3452');rect(ctx,x+25,top-45,40,45,'#1d3452');rect(ctx,x+38,top-75,14,35,'#1d3452');}
 for(let i=-1;i<5;i++)arch(ctx,i*380-travel*factors[2],180,280,420,'#14293f');
 for(let i=-1;i<4;i++){const x=i*660+60-travel*factors[3];rect(ctx,x,150,44,450,'#0b1b2c');rect(ctx,x-12,132,68,18,'#0b1b2c');rect(ctx,x+6,40,32,92,'#0b1b2c');}
 rect(ctx,0,600,1280,120,'#091523');for(let i=-1;i<Math.ceil((1280+travel)/56)+1;i++){const x=i*56-travel*factors[4];rect(ctx,x,590,50,12,'#3f5775');rect(ctx,x+4,604,44,20,'#122238')}
 if(guides)P.parallax.layers.forEach((layer,i)=>{const y=122+i*93,x=210-travel*layer.factor;line(ctx,[[210,y],[x,y]],['#ffe0a0','#6f94c0','#7399b7','#9aabbd','#f2d184'][i],4);rect(ctx,x-4,y-4,8,8,C.cream);text(ctx,layer.name+'  ×'+layer.factor,24,y-14,18,C.cream)});
}
function parallaxFrame(canvas,travel=0,guides=true){const ctx=canvas.getContext('2d');clear(ctx);layerScene(ctx,travel,guides);drawRun(ctx,500,590,travel/P.parallax.baseTravelPx*P.parallax.travelMs);return travel}
function duration(key,repeated=false){const seq=P.sequences[key];return repeated&&seq.repeatMs?seq.repeatMs:seq.totalMs}
function bossTimes(key,repeated){const seq=P.sequences[key],seg=repeated?seq.repeatSegments:seq.segments;return seg.slice(-4).map(s=>s[1])}
function cameraPose(key,t,repeated=false,reduced=false){const [inEnd,holdEnd,outEnd,end]=bossTimes(key,repeated),max=P.sequences[key].maxZoom;const start=P.stageBanner.totalMs,amount=reduced||t<start?0:t<inEnd?ease((t-start)/(inEnd-start)):t<holdEnd?1:1-ease((t-holdEnd)/(outEnd-holdEnd));return{zoom:mix(1,max,amount),amount,inEnd,holdEnd,outEnd,end}}
function lantern(ctx,p){const x=520,y=130;rect(ctx,x-2,0,4,y-25,C.old);rect(ctx,x-19,y-26,38,8,C.old);rect(ctx,x-17,y-18,34,44,'#382c1c');rect(ctx,x-12,y-14,24,34,p>.2?C.gold:'#71512a');rect(ctx,x-5,y-7,10,22,C.cream);rect(ctx,x-20,y+25,40,8,C.old)}
function lobbyReveal(ctx,p){ctx.drawImage(images.lobby,0,0,1280,720);const radius=25+ease(p)*1000;for(let i=0;i<6;i++){ctx.fillStyle=`rgba(3,7,13,${.68*(1-p)})`;ctx.beginPath();ctx.rect(0,0,1280,720);ctx.moveTo(520+radius+i*42,130);ctx.arc(520,130,radius+i*42,0,Math.PI*2);ctx.fill('evenodd')}lantern(ctx,p)}
function sequenceFrame(canvas,key='boss',time=850,reduced=false,repeated=false){const ctx=canvas.getContext('2d');clear(ctx);const total=duration(key,repeated),t=clamp(time,0,total);let caption='';
 if(key==='normal'){
  const dissolveEnd=550,runEnd=dissolveEnd+P.parallax.travelMs,bannerStart=runEnd+200;
  const v=clamp(t/dissolveEnd),run=clamp((t-dissolveEnd)/P.parallax.travelMs),travel=P.parallax.baseTravelPx*run;
  layerScene(ctx,reduced?0:travel);
  if(t<300)drawPlayerAttack(ctx,200+t);else if(t>=dissolveEnd&&t<runEnd&&!reduced)drawRun(ctx,500,590,t-dissolveEnd);else sprite(ctx,'on_00',500,590,1.1);
  const b=sprite(ctx,t<dissolveEnd?'gate_00':'gate_01',t<dissolveEnd?900:reduced?900:P.parallax.nextEnemyStartX-travel,590,2,t<dissolveEnd?1:reduced?run:1,t<dissolveEnd?v:0);
  if(t<dissolveEnd)debris(ctx,b,v,reduced);if(t<225)drawDiagonalSlash(ctx,t+25,false);
  hud(ctx,{stage:t<runEnd?1:2,showStage:t<bannerStart||t>=total,active:t>=total,showBell:t>=total,enemyHp:t>=total});
  drawStageBanner(ctx,2,t-bannerStart,reduced);if(t===total)dangerEffect(ctx,'low_sweep',0,650);
  caption=t<dissolveEnd?'적 도트 분해':t<runEnd?'2초 이동 · 온 x500 고정 · 바닥960px 전진':t<bannerStart?'월드 정지 · 새 적과 조우':t<total?'도트 조각이 모여 Stage 2 완성':'Stage 2 · 온전한 첫 예고 시작';
 }else if(key==='death'){if(t<850){background(ctx);sprite(ctx,'gate_01',900,590,2);heroMotion(ctx,'death',t,550,reduced);hud(ctx,{stage:11,active:false,showBell:false,playerHp:false});ctx.fillStyle=`rgba(3,7,13,${clamp((t-250)/450)})`;ctx.fillRect(0,0,1280,720)}else lobbyReveal(ctx,clamp((t-850)/750));caption=t<200?'온이 무릎을 꿇고 쓰러짐':t<550?'온의 도트 분해':t<850?'어둠':t<1600?'등불이 로비를 드러냄':t<1800?'로비 정착':'Stage 11 로비 · 조작 재개';
 }else{const cam=cameraPose(key,t,repeated,reduced);const anchor=P.camera.closeupAnchors[key],focus=[...anchor.world,...anchor.screen],a=cam.amount;
  ctx.save();ctx.translate(mix(640,focus[2],a),mix(360,focus[3],a));ctx.scale(cam.zoom,cam.zoom);ctx.translate(-mix(640,focus[0],a),-mix(360,focus[1],a));background(ctx);sprite(ctx,'on_00',500,590,1.1);drawBossRoar(ctx,key,t,cam);
  ctx.restore();hud(ctx,{stage:key==='mini'?5:10,showStage:t>=P.stageBanner.totalMs,boss:true,bossAlpha:clamp((t-cam.outEnd)/200),active:t>=cam.end,showBell:t>=cam.end,playerHp:t>=cam.end});drawStageBanner(ctx,key==='mini'?5:10,t,reduced);if(t>=cam.end)dangerEffect(ctx,'low_sweep',0,900);caption=t<P.stageBanner.totalMs?'도트 Stage 표시':t<cam.inEnd?'큰 줌으로 들어갑니다.':t<cam.holdEnd?`${cam.zoom.toFixed(2)}배 고정 · 포효를 마칠 때까지 카메라 유지`:t<cam.outEnd?'포효 후 줌아웃 · 아직 전투하지 않습니다.':t<cam.end?'원래 구도로 정착 · 실제 HP 공개':'입력 재개 · 첫900ms 예고 시작';
 }return caption;
}
