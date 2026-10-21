/* v2 drawing studies only. VFX never performs a damage roll or changes game state. */
function actor(id){return P.actors.find(a=>a.id===id)}
function sizeFrame(canvas,id='bell_knight',proposed=true){const ctx=canvas.getContext('2d');clear(ctx);const a=actor(id);world(ctx,{enemy:a.sprite,enemyHeight:proposed?a.proposed:a.current});hud(ctx,{stage:id==='bell_knight'?10:id==='gate_warden'?5:3,boss:id==='bell_knight'||id==='gate_warden'});}
function line(ctx,points,color,width=4){ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(px(x),px(y)):ctx.moveTo(px(x),px(y)));ctx.strokeStyle=color;ctx.lineWidth=width;ctx.lineJoin='miter';ctx.stroke()}
function spark(ctx,x,y,p=.5,big=false){for(let i=0;i<8;i++){const a=i*Math.PI/4,r=(big?42:24)*(1+p);const xx=x+Math.cos(a)*r,yy=y+Math.sin(a)*r,w=i%2?4:8;rect(ctx,xx-2,yy-2,w+4,w+4,C.dark);rect(ctx,xx,yy,w,w,i%2?C.gold:C.cream)}rect(ctx,x-11,y-11,22,22,C.dark);rect(ctx,x-7,y-7,14,14,C.cream)}
function rune(ctx,x,y,p){const r=12+24*(1-clamp(p));line(ctx,[[x-r,y-4],[x-r,y+4],[x-6,y+4]],C.gold,4);line(ctx,[[x+r,y-4],[x+r,y+4],[x+6,y+4]],C.gold,4);rect(ctx,x-3,y-3,6,6,C.cream)}
function spike(ctx,x,bottom,height,alpha=1){ctx.save();ctx.globalAlpha=alpha;for(let y=0;y<height;y+=8){const w=Math.max(4,28*(1-y/height));rect(ctx,x-w/2-2,bottom-y-8,w+4,8,C.dark);rect(ctx,x-w/2,bottom-y-8,w,8,C.gold);rect(ctx,x-2,bottom-y-8,4,8,C.cream)}ctx.restore()}
function weapon(ctx,x,tipY,type=0,alpha=1){ctx.save();ctx.globalAlpha=alpha;const len=type===2?118:148;rect(ctx,x-4,tipY-len,8,len-12,C.dark);rect(ctx,x-2,tipY-len,4,len-12,C.old);for(let k=0;k<5;k++){const w=4+k*4;rect(ctx,x-w/2,tipY-8-k*8,w,8,k<2?C.cream:C.gold)}if(type===0){rect(ctx,x-18,tipY-65,36,6,C.gold);rect(ctx,x-5,tipY-len,10,12,C.cream)}if(type===2){line(ctx,[[x-12,tipY-len+4],[x,tipY-len+20],[x+12,tipY-len+4]],C.gold,6)}ctx.restore()}
function dangerEffect(ctx,id,time,duration=650){const before=time<duration,p=clamp(time/duration),after=time-duration,life=before?1:clamp(1-after/140);ctx.save();
 if(id==='low_sweep'){
  line(ctx,[[430,588],[454,584],[478,590],[502,582],[526,588],[554,584],[580,588]],C.dark,10);line(ctx,[[430,588],[454,584],[478,590],[502,582],[526,588],[554,584],[580,588]],C.gold,5);
  if(before){[444,482,520,558].forEach(x=>{rune(ctx,x,584,p);rect(ctx,x-3,573,6,6,C.gold)});}else if(life>0){[50,68,80,60].forEach((h,i)=>spike(ctx,444+i*38,590,h*(.45+.55*clamp(after/40)),life));}
 }else if(id==='outer_arc'){
  [404,450,496].forEach((x,i)=>{if(before){weapon(ctx,x,mix(330-i*14,590,clamp((time-duration+P.telegraph.rearFallMs)/P.telegraph.rearFallMs)**2),i,.35+.65*p);rune(ctx,x,585,p);line(ctx,[[x,390],[x,450],[x,510]],'#827552',2);}else if(life>0){weapon(ctx,x,590,i,life);spark(ctx,x,585,clamp(after/140),false)}});
 }else if(id==='high_sweep'){
  if(before){line(ctx,[[430,485],[480,481],[525,485],[580,479],[620,483]],C.dark,10);line(ctx,[[430,485],[480,481],[525,485],[580,479],[620,483]],C.gold,4);rune(ctx,mix(620,450,p),480,p)}
  else if(life>0){ctx.globalAlpha=life;line(ctx,[[810,476],[750,466],[665,466],[610,474],[555,484],[493,489],[430,482]],C.dark,25);line(ctx,[[810,476],[750,466],[665,466],[610,474],[555,484],[493,489],[430,482]],C.gold,17);line(ctx,[[780,470],[665,470],[555,488],[450,488]],C.cream,5)}
 }else{
  if(before){line(ctx,[[810,525],[735,525],[660,525],[585,525]],C.old,3);rune(ctx,mix(790,565,p),525,p)}else if(life>0){ctx.globalAlpha=life;line(ctx,[[820,525],[555,525]],C.dark,20);line(ctx,[[820,525],[555,525]],C.old,10);for(let i=0;i<7;i++)rect(ctx,536+i*8,523-i*2,8,4+i*4,i<2?C.cream:C.gold);spark(ctx,545,525,clamp(after/140))}
 }
 ctx.restore();return before?'준비 자세 + 위험 재료 · '+Math.round(p*100)+'%':after<10?'T · 판정 한 번, 타격 VFX 시작':after<140?'타격 뒤 잔해 · 추가 피해 없음':'잔해 퇴장 · 다음 동작 준비';
}
function warningFrame(canvas,id='low_sweep',time=690,duration=650,success=true,mono=false,wire=false){const ctx=canvas.getContext('2d');clear(ctx);ctx.save();if(mono)ctx.filter='grayscale(1)';background(ctx);const after=time-duration,impact=after>=0&&after<220;let hx=500,foot=590,pose='on_00';
 if(success&&impact){if(id==='low_sweep'){pose='on_04';foot-=after<140?100:100*(1-clamp((after-140)/80))}if(id==='high_sweep')pose='on_05';if(id==='thrust'){pose='on_06';hx-=70}if(id==='outer_arc'){pose='on_07';hx+=70}}
 sprite(ctx,pose,hx,foot,1.1);sprite(ctx,time<duration?'gate_07':'gate_02',900,590,2.0);
 // Placeholder enemy hand/source light ties the effect to the caster's held pose.
 if(time<duration){rune(ctx,832,464,clamp(time/duration));line(ctx,[[830,463],[790,453],[766,463]],C.gold,3)}
 const label=dangerEffect(ctx,id,time,duration);hud(ctx,{stage:3});if(wire){ctx.strokeStyle='#bfcde0';ctx.lineWidth=2;ctx.setLineDash([8,8]);ctx.strokeRect(...P.telegraph.regions.find(r=>r.id===id).bounds);ctx.setLineDash([]);ctx.strokeRect(447,608,106,10)}ctx.restore();return label;
}
const DIGITS={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['111','001','111','100','111'],'3':['111','001','111','001','111'],'4':['101','101','111','001','001'],'5':['111','100','111','001','111'],'6':['111','100','111','101','111'],'7':['111','001','010','010','010'],'8':['111','101','111','101','111'],'9':['111','101','111','001','111']};
function damageNumber(ctx,n,x,y,scale){String(n).split('').forEach((v,i)=>DIGITS[v].forEach((row,yy)=>[...row].forEach((b,xx)=>{if(b==='1'){rect(ctx,x+(i*4+xx)*scale-2,y+yy*scale-2,scale+4,scale+4,C.dark);rect(ctx,x+(i*4+xx)*scale,y+yy*scale,scale,scale,C.cream)}})))}
function playerFrame(canvas,time=130,critical=true){const ctx=canvas.getContext('2d');clear(ctx);background(ctx);const hit=time>=100&&time<220,lunge=time<100?80*ease(time/100):80*(1-clamp((time-140)/140));sprite(ctx,time<100?'on_02':time<280?'on_03':'on_00',500+lunge,590,1.1);const target=sprite(ctx,'gate_00',900,590,2.0);
 if(hit){ctx.save();ctx.globalAlpha=clamp((220-time)/40);const p=(time-100)/120;line(ctx,[[580,483],[620,474],[654,486],[676,510],[682,544],[662,570],[627,579]],C.dark,18);line(ctx,[[580,483],[620,474],[654,486],[676,510],[682,544],[662,570],[627,579]],C.gold,12);line(ctx,[[590,480],[624,480],[653,494],[670,518],[675,542]],C.cream,5);const [tx,ty]=P.playerAttack.contactReference.position;line(ctx,[[665,531],[700,485],[735,452],[775,447],[810,452],[tx,ty]],C.gold,critical?9:5);spark(ctx,tx,ty,p,critical);debris(ctx,{left:tx-10,top:ty-22,w:36,h:55},p+.1,false);ctx.restore()}
 if(time>=100&&time<450){ctx.globalAlpha=clamp((450-time)/100);damageNumber(ctx,critical?128:64,805,285-(time-100)*.08,critical?9:6);ctx.globalAlpha=1}hud(ctx,{stage:1});return time<100?'준비':time<220?'100ms 명중 사건 · 검흔/접점/피해 동기화':time<280?'검흔 소멸 · 자세 회수':'500ms 다음 공격 준비';}
function arch(ctx,x,y,w,h,color){const t=20;rect(ctx,x,y+40,t,h-40,color);rect(ctx,x+w-t,y+40,t,h-40,color);rect(ctx,x+t,y+20,w-2*t,20,color);rect(ctx,x+2*t,y,w-4*t,20,color);rect(ctx,x,y+35,40,20,color);rect(ctx,x+w-40,y+35,40,20,color)}
function layerScene(ctx,travel=0,guides=false){
 rect(ctx,0,0,1280,720,'#233d60');const factors=P.parallax.layers.map(l=>l.factor);
 // Moon/sky layer.
 rect(ctx,1040-travel*factors[0],90,36,56,'#d8ac55');rect(ctx,1054-travel*factors[0],82,36,56,'#233d60');
 for(let i=-1;i<10;i++){const x=i*180-travel*factors[1],top=270+(i*53%110);rect(ctx,x,top,90,600-top,'#1d3452');rect(ctx,x+25,top-45,40,45,'#1d3452');rect(ctx,x+38,top-75,14,35,'#1d3452');}
 for(let i=-1;i<5;i++)arch(ctx,i*380-travel*factors[2],180,280,420,'#14293f');
 for(let i=-1;i<4;i++){const x=i*660+60-travel*factors[3];rect(ctx,x,150,44,450,'#0b1b2c');rect(ctx,x-12,132,68,18,'#0b1b2c');rect(ctx,x+6,40,32,92,'#0b1b2c');}
 rect(ctx,0,600,1280,120,'#091523');for(let i=-1;i<30;i++){const x=i*56-travel*factors[4];rect(ctx,x,590,50,12,'#3f5775');rect(ctx,x+4,604,44,20,'#122238')}
 if(guides)P.parallax.layers.forEach((layer,i)=>{const y=122+i*93,x=210-travel*layer.factor;line(ctx,[[210,y],[x,y]],['#ffe0a0','#6f94c0','#7399b7','#9aabbd','#f2d184'][i],4);rect(ctx,x-4,y-4,8,8,C.cream);text(ctx,layer.name+'  ×'+layer.factor,24,y-14,18,C.cream)});
}
function parallaxFrame(canvas,travel=0,guides=true){const ctx=canvas.getContext('2d');clear(ctx);layerScene(ctx,travel,guides);sprite(ctx,'on_00',500,590,1.1);return travel}
function duration(key,repeated=false){const seq=P.sequences[key];return repeated&&seq.repeatMs?seq.repeatMs:seq.totalMs}
function bossTimes(key,repeated){const seq=P.sequences[key],seg=repeated?seq.repeatSegments:seq.segments;return seg.map(s=>s[1])}
function cameraPose(key,t,repeated=false,reduced=false){const [inEnd,holdEnd,outEnd,end]=bossTimes(key,repeated),max=P.sequences[key].maxZoom;const amount=reduced?0:t<inEnd?ease(t/inEnd):t<holdEnd?1:1-ease((t-holdEnd)/(outEnd-holdEnd));return{zoom:mix(1,max,amount),amount,inEnd,holdEnd,outEnd,end}}
function lantern(ctx,p){const x=520,y=130;rect(ctx,x-2,0,4,y-25,C.old);rect(ctx,x-19,y-26,38,8,C.old);rect(ctx,x-17,y-18,34,44,'#382c1c');rect(ctx,x-12,y-14,24,34,p>.2?C.gold:'#71512a');rect(ctx,x-5,y-7,10,22,C.cream);rect(ctx,x-20,y+25,40,8,C.old)}
function lobbyReveal(ctx,p){ctx.drawImage(images.lobby,0,0,1280,720);const radius=25+ease(p)*1000;for(let i=0;i<6;i++){ctx.fillStyle=`rgba(3,7,13,${.68*(1-p)})`;ctx.beginPath();ctx.rect(0,0,1280,720);ctx.moveTo(520+radius+i*42,130);ctx.arc(520,130,radius+i*42,0,Math.PI*2);ctx.fill('evenodd')}lantern(ctx,p)}
function sequenceFrame(canvas,key='boss',time=850,reduced=false,repeated=false){const ctx=canvas.getContext('2d');clear(ctx);const total=duration(key,repeated),t=clamp(time,0,total);let caption='';
 if(key==='normal'){const v=clamp(t/550),run=clamp((t-550)/650);layerScene(ctx,reduced?0:160*run);sprite(ctx,'on_00',500+(reduced?0:Math.sin(run*Math.PI)*35),590-(reduced?0:Math.abs(Math.sin(run*22))*4),1.1);const b=sprite(ctx,t<550?'gate_02':'gate_00',t<550?900:reduced?900:mix(1350,900,ease(run)),590,2,t<550?1:reduced?run:1,t<550?v:0);if(t<550)debris(ctx,b,v,reduced);hud(ctx,{stage:t<1200?3:4,active:t>=total,showBell:t>=total,enemyHp:t>=total});if(t===total)dangerEffect(ctx,'outer_arc',0,650);caption=t<550?'몸체 분해':t<1200?'전진 · 원경은 느리게, 바닥은 빠르게':t<1400?'정착 · 전투 시계 정지':'Stage 4 · 온전한 첫 예고 시작';
 }else if(key==='death'){const p=clamp(t/550);if(t<850){const f=world(ctx,{heroVanish:p,enemy:'gate_01',enemyHeight:2});debris(ctx,f.hero,p,reduced);hud(ctx,{stage:11,active:false,showBell:false,playerHp:false});ctx.fillStyle=`rgba(3,7,13,${clamp((t-250)/450)})`;ctx.fillRect(0,0,1280,720)}else lobbyReveal(ctx,clamp((t-850)/750));caption=t<550?'온의 도트 분해':t<850?'어둠':t<1600?'등불이 로비를 드러냄':t<1800?'로비 정착':'Stage 11 로비 · 조작 재개';
 }else{const cam=cameraPose(key,t,repeated,reduced);const anchor=P.camera.closeupAnchors[key],focus=[...anchor.world,...anchor.screen],a=cam.amount;
  ctx.save();ctx.translate(mix(640,focus[2],a),mix(360,focus[3],a));ctx.scale(cam.zoom,cam.zoom);ctx.translate(-mix(640,focus[0],a),-mix(360,focus[1],a));world(ctx,{enemy:key==='mini'?'gate_03':'gate_04',enemyHeight:key==='mini'?3.2:4,enemyAlpha:1});
  if(t>=cam.inEnd&&t<cam.holdEnd){const k=(t-cam.inEnd)/(cam.holdEnd-cam.inEnd),cx=key==='mini'?875:880,cy=key==='mini'?355:330;rect(ctx,cx-13,cy-13,26,26,C.cream);for(let j=0;j<2;j++){const r=36+((k+j*.35)%1)*90;ctx.globalAlpha=1-((k+j*.35)%1);const points=Array.from({length:9},(_,i)=>[cx+Math.cos(i*Math.PI/4)*r,cy+Math.sin(i*Math.PI/4)*r*.7]);line(ctx,points,C.gold,5)}ctx.globalAlpha=1}
  ctx.restore();hud(ctx,{stage:key==='mini'?5:10,boss:true,bossAlpha:clamp((t-cam.outEnd)/200),active:t>=cam.end,showBell:t>=cam.end,playerHp:t>=cam.end});if(t>=cam.end)dangerEffect(ctx,'low_sweep',0,900);caption=t<cam.inEnd?'큰 줌으로 들어갑니다.':t<cam.holdEnd?`${cam.zoom.toFixed(2)}배 고정 · 포효를 마칠 때까지 카메라 유지`:t<cam.outEnd?'포효 후 줌아웃 · 아직 전투하지 않습니다.':t<cam.end?'원래 구도로 정착 · 실제 HP 공개':'입력 재개 · 첫900ms 예고 시작';
 }return caption;
}
