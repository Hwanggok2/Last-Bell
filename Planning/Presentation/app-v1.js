/* Browser-only art/sequence study. No combat logic or Unity assets are changed. */
const P=window.PROPOSAL,A=window.ATLAS,$=id=>document.getElementById(id);
const C={dark:'#0c172b',old:'#ac7833',gold:'#dca947',cream:'#fff0b4',line:'#30465f'};
const images={},paths={hero:'../../Client/Assets/Game/Art/Source/on-poses-v1.png',enemy:'../../Client/Assets/Game/Art/Source/gate-enemies-v1.png',bg:'../../Client/Assets/Game/Art/Source/gate-background-v1.png',lobby:'../Evidence/Unity/Smoke/09-lobby.png'};
const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x)),mix=(a,b,t)=>a+(b-a)*clamp(t),ease=t=>{t=clamp(t);return t*t*(3-2*t)},px=x=>Math.round(x/2)*2;
function clear(ctx){ctx.setTransform(1,0,0,1,0,0);ctx.clearRect(0,0,1280,720);ctx.imageSmoothingEnabled=false}
function rect(ctx,x,y,w,h,color){ctx.fillStyle=color;ctx.fillRect(px(x),px(y),Math.max(1,px(w)),Math.max(1,px(h)))}
function text(ctx,s,x,y,size=24,color=C.cream,align='left'){ctx.fillStyle=color;ctx.font=`${size}px Bell, sans-serif`;ctx.textAlign=align;ctx.fillText(s,x,y)}
function background(ctx,offset=0,wire=false){
 if(wire){rect(ctx,0,0,1280,720,'#111821');rect(ctx,0,600,1280,120,'#27313e');ctx.strokeStyle='#546273';ctx.lineWidth=1;for(let x=0;x<1280;x+=80){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,720);ctx.stroke()}for(let y=40;y<720;y+=80){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(1280,y);ctx.stroke()}return}
 ctx.drawImage(images.bg,-px(offset),0,1280,720);if(offset>0)ctx.drawImage(images.bg,1280-px(offset),0,1280,720);
 // The world art's ground is slightly above the conceptual foot baseline; a stone lip joins it.
 rect(ctx,0,596,1280,8,'#192b43');for(let x=-offset%48;x<1280;x+=48){rect(ctx,x,596,42,4,'#405a7a')}
}
function sprite(ctx,name,x,foot,height,alpha=1,vanish=0,wire=false){
 const a=A[name],h=height*116.13,w=h*a.w/a.h,left=x-w*a.px,top=foot-h;
 ctx.save();ctx.globalAlpha=clamp(alpha);
 if(wire){rect(ctx,left,top,w,h,'#34465b');ctx.strokeStyle='#c5cdd6';ctx.lineWidth=2;ctx.strokeRect(left,top,w,h);ctx.restore();return {left,top,w,h}}
 if(vanish>0){ctx.beginPath();for(let row=0;row<Math.ceil(h/8);row++)for(let col=0;col<Math.ceil(w/8);col++){const rand=((row*73+col*31+row*col*13)%101)/101;if(rand>vanish)ctx.rect(left+col*8,top+row*8,8,8)}ctx.clip()}
 ctx.drawImage(images[a.sheet],a.x,a.y,a.w,a.h,px(left),px(top),px(w),px(h));ctx.restore();return {left,top,w,h};
}
function debris(ctx,box,p,reduced=false){if(p<=0||p>=1.5)return;for(let i=0;i<(reduced?18:36);i++){const u=((i*37)%101)/101,v=((i*61)%97)/97,delay=u*.55,k=clamp((p-delay)/.8);if(k<=0)continue;ctx.globalAlpha=1-k;const x=box.left+u*box.w+(u-.5)*k*65,y=box.top+v*box.h+100*k*k;rect(ctx,x,y,i%3===0?8:4,i%3===0?8:4,[C.cream,C.gold,'#23344c'][i%3])}ctx.globalAlpha=1}
function meter(ctx,x,y,w,h,p,fill=C.gold){rect(ctx,x,y,w,h,'#15283b');rect(ctx,x,y,w*p,h,fill)}
function bell(ctx,x,y,color=C.cream){rect(ctx,x-6,y-13,12,5,color);rect(ctx,x-10,y-8,20,19,color);rect(ctx,x-14,y+9,28,5,color);rect(ctx,x-3,y+16,6,4,color)}
function controls(ctx,alpha=1){ctx.save();ctx.globalAlpha=alpha;[[200,421,0],[93,528,1],[200,635,2],[307,528,3]].forEach(([x,y,d])=>{ctx.save();ctx.translate(x,y);ctx.rotate(d*Math.PI/2-Math.PI*(d===1?1:0)+Math.PI*(d===3?1:0));ctx.fillStyle='#f0d59d';ctx.beginPath();ctx.moveTo(0,-23);ctx.lineTo(24,23);ctx.lineTo(-24,23);ctx.closePath();ctx.fill();ctx.restore()});rect(ctx,1118,566,133,117,'#213b50');ctx.save();ctx.translate(1184,625);ctx.rotate(Math.PI/4);rect(ctx,-4,-38,8,65,'#f0d59d');rect(ctx,-18,20,36,6,'#f0d59d');rect(ctx,-4,25,8,17,'#f0d59d');ctx.restore();ctx.restore()}
function hud(ctx,{stage=3,boss=false,bossAlpha=1,active=true,showBell=true,enemyHp=true,playerHp=true}={}){text(ctx,'Stage '+stage,12,57,32);rect(ctx,1168,16,96,96,'#21394c');rect(ctx,1205,51,6,30,C.cream);rect(ctx,1221,51,6,30,C.cream);if(showBell){bell(ctx,292,117);meter(ctx,326,110,627,24,.35)}if(boss){ctx.save();ctx.globalAlpha=bossAlpha;meter(ctx,213,36,928,21,1,'#f0d59d');ctx.restore()}if(playerHp)meter(ctx,495,601,106,11,.8,'#f0d59d');if(enemyHp&&!boss)meter(ctx,755,601,106,11,.68);controls(ctx,active?1:.28)}
function world(ctx,{enemy='gate_02',enemyHeight=1.5,heroX=548,enemyX=808,heroAlpha=1,enemyAlpha=1,heroVanish=0,enemyVanish=0,run=0,offset=0,wire=false}={}){background(ctx,offset,wire);const hero=sprite(ctx,'on_00',heroX,590-Math.abs(Math.sin(run*22))*5,1.1,heroAlpha,heroVanish,wire),monster=sprite(ctx,enemy,enemyX,590,enemyHeight,enemyAlpha,enemyVanish,wire);return{hero,monster}}
function signalPoints(id){const r=P.telegraph.regions.find(r=>r.id===id),[x,y,w,h]=r.bounds;let out=[];if(id==='low_sweep'||id==='high_sweep'){for(let xx=x+4;xx<=x+w-4;xx+=8)out.push([xx,y+h/2+((xx-x)%24===4?-2:0)])}else if(id==='thrust'){for(let yy=y+4;yy<=y+h-4;yy+=8)out.push([x+w/2+(Math.floor(yy/8)%3===0?2:0),yy])}else{for(let xx=x+w-4;xx>=x+12;xx-=8)out.push([xx,y+4]);out.push([x+8,y+8]);for(let yy=y+16;yy<=y+h-16;yy+=8)out.push([x+4,yy]);out.push([x+8,y+h-8]);for(let xx=x+12;xx<=x+w-4;xx+=8)out.push([xx,y+h-4])}return out}
function signal(ctx,id,p,mode='brass',next=false){
 const r=P.telegraph.regions.find(r=>r.id===id),[x,y,w,h]=r.bounds;
 if(mode==='wire'){ctx.strokeStyle=next?'#7b8693':'#e7ebef';ctx.lineWidth=2;ctx.setLineDash(next?[6,6]:[]);ctx.strokeRect(x,y,w,h);ctx.setLineDash([]);return}
 if(mode==='current'){
  const X=548,Y=519,W=70,H=76;let points=id==='high_sweep'?[[X-W,Y-H+16],[X-W,Y-H],[X+W,Y-H],[X+W,Y-H+16]]:id==='low_sweep'?[[X-W,Y+H-18],[X-W*.6,Y+H],[X-W*.2,Y+H-18],[X+W*.2,Y+H],[X+W*.6,Y+H-18],[X+W,Y+H]]:id==='thrust'?[[X+W*.55,Y-H],[X+W,Y],[X+W*.55,Y+H]]:[[X-W*.55,Y-H],[X-W,Y],[X-W*.55,Y+H]];
  ctx.save();ctx.globalAlpha=next?.4:1;ctx.beginPath();points.forEach(([a,b],i)=>i?ctx.lineTo(a,b):ctx.moveTo(a,b));ctx.strokeStyle=C.dark;ctx.lineWidth=13;ctx.stroke();ctx.strokeStyle=C.old;ctx.lineWidth=9;ctx.stroke();ctx.strokeStyle=C.gold;ctx.lineWidth=6;ctx.setLineDash([p*300,300]);ctx.stroke();ctx.restore();return
 }
 let points=signalPoints(id);const dx=next?(id==='thrust'?16:id==='outer_arc'?-16:0):0,dy=next?(id==='high_sweep'?-16:id==='low_sweep'?16:0):0;
 ctx.save();ctx.globalAlpha=next?.55:1;
 if(mode==='ornament'){ctx.strokeStyle=C.old;ctx.lineWidth=4;ctx.strokeRect(x-7+dx,y-7+dy,w+14,h+14);for(const xx of [x-8,x+w+4])for(const yy of [y-8,y+h+4])rect(ctx,xx+dx,yy+dy,6,6,C.cream)}
 if(next){points.forEach(([xx,yy],i)=>{if(i%2)return;rect(ctx,xx-4+dx,yy-4+dy,8,8,C.dark);rect(ctx,xx-2+dx,yy-2+dy,4,4,C.gold)})}
 else {
  points.forEach(([xx,yy])=>rect(ctx,xx-7,yy-7,14,14,C.dark));
  points.forEach(([xx,yy],i)=>rect(ctx,xx-5,yy-5,10,10,i/points.length<p?'#f2d184':C.gold));
  points.forEach(([xx,yy],i)=>{if(i%5===3)rect(ctx,xx,yy-5,2,6,C.old)});
  const index=Math.min(points.length-1,Math.floor(p*points.length)),q=points[index];rect(ctx,q[0]-8,q[1]-8,16,16,C.dark);rect(ctx,q[0]-6,q[1]-6,12,12,C.cream);
 }ctx.restore();
}
function warningFrame(canvas,id='low_sweep',progress=.72,mode='brass',showNext=false,mono=false){const ctx=canvas.getContext('2d');clear(ctx);ctx.save();if(mono)ctx.filter='grayscale(1)';world(ctx,{wire:mode==='wire'});if(showNext)signal(ctx,id,.1,mode,true);signal(ctx,id,progress,mode);hud(ctx);if(mode==='wire'){ctx.strokeStyle='#8395aa';ctx.lineWidth=2;[[152,373,96,96],[45,480,96,96],[152,587,96,96],[259,480,96,96],[1118,566,133,117]].forEach(b=>ctx.strokeRect(...b))}ctx.restore()}
function scaleFrame(canvas,height){const ctx=canvas.getContext('2d');clear(ctx);world(ctx,{enemyHeight:height});hud(ctx,{showBell:false});}
function duration(key,repeated=false){return repeated&&P.sequences[key].repeatMs?P.sequences[key].repeatMs:P.sequences[key].totalMs}
function bossTimes(key,repeated){if(repeated)return key==='mini'?[180,420,600,800]:[220,550,800,1000];return key==='mini'?[350,800,1200,1400]:[450,1100,1600,1800]}
function lantern(ctx,reveal){const x=520,y=130;rect(ctx,x-2,0,4,y-25,'#58472c');rect(ctx,x-19,y-26,38,8,C.old);rect(ctx,x-17,y-18,34,44,'#382c1c');rect(ctx,x-12,y-14,24,34,reveal>.2?C.gold:'#71512a');rect(ctx,x-5,y-7,10,22,C.cream);rect(ctx,x-20,y+25,40,8,C.old);rect(ctx,x-17,y-15,5,40,C.dark);rect(ctx,x+12,y-15,5,40,C.dark)}
function lobbyReveal(ctx,p){ctx.drawImage(images.lobby,0,0,1280,720);const radius=25+ease(p)*1000;for(let i=0;i<6;i++){ctx.fillStyle=`rgba(3,7,13,${.68*(1-p)})`;ctx.beginPath();ctx.rect(0,0,1280,720);ctx.moveTo(520+radius+i*42,130);ctx.arc(520,130,radius+i*42,0,Math.PI*2);ctx.fill('evenodd')}lantern(ctx,p)}
function sequenceFrame(canvas,key,time,reduced=false,repeated=false){
 const ctx=canvas.getContext('2d');clear(ctx);const d=duration(key,repeated),t=clamp(time,0,d);let caption='';
 if(key==='normal'){
  const broken=clamp(t/550),run=clamp((t-550)/650),settled=t>=1200;
  const f=world(ctx,{enemy:t<550?'gate_02':'gate_00',enemyHeight:1.5,enemyX:t<550?808:reduced?808:mix(1350,808,ease(run)),enemyAlpha:t<550?1:reduced?run:1,enemyVanish:t<550?broken:0,heroX:reduced?548:548+Math.sin(run*Math.PI)*38,run:reduced?0:run,offset:reduced?0:run*140});
  if(t<550)debris(ctx,f.monster,broken,reduced);hud(ctx,{stage:settled?4:3,active:t>=d,showBell:t>=d,enemyHp:t>=d});if(t>=d)signal(ctx,'outer_arc',0);caption=t<550?'적의 실루엣이 도트 조각으로 빠져나갑니다.':t<1200?'온은 앞으로, 배경은 뒤로. 다음 적을 만납니다.':t<d?'발과 카메라가 안정됩니다. 전투 입력은 아직 잠겨 있습니다.':'Stage 4 · 조작 재개와 함께 첫 예고가 처음부터 시작됩니다.';
 }else if(key==='death'){
  const p=clamp(t/550);if(t<850){const f=world(ctx,{heroVanish:p,enemy:'gate_01',enemyHeight:1.5});debris(ctx,f.hero,p,reduced);hud(ctx,{stage:11,active:false,showBell:false,playerHp:false});ctx.fillStyle=`rgba(3,7,13,${clamp((t-250)/450)})`;ctx.fillRect(0,0,1280,720)}else lobbyReveal(ctx,clamp((t-850)/750));caption=t<550?'온의 몸에서 작은 금빛 도트가 떨어집니다.':t<700?'전투 화면을 천천히 어둠이 덮습니다.':t<850?'짧게 숨을 고르는 어둠.':t<1600?'등불의 불씨가 퍼지며 로비를 드러냅니다.':t<1800?'로비 전체의 밝기가 안정됩니다.':'로비 · 강화와 Stage 출전 입력을 엽니다.';
 }else{
  const [zoomIn,roarEnd,zoomOut,end]=bossTimes(key,repeated),max=key==='mini'?1.12:1.18;
  const zoom=reduced?1:t<zoomIn?mix(1,max,ease(t/zoomIn)):t<roarEnd?max:mix(max,1,ease((t-roarEnd)/(zoomOut-roarEnd)));
  ctx.save();ctx.translate(760,480);ctx.scale(zoom,zoom);ctx.translate(-760,-480);
  world(ctx,{enemy:key==='mini'?'gate_03':'gate_04',enemyHeight:key==='mini'?1.8:2.1,enemyAlpha:clamp(t/(zoomIn*.65))});
  if(t>=zoomIn&&t<roarEnd){const f=(t-zoomIn)/(roarEnd-zoomIn);rect(ctx,791,429,22,22,C.cream);ctx.strokeStyle=C.gold;ctx.lineWidth=6;for(let j=0;j<2;j++){const radius=22+((f+j*.36)%1)*100;ctx.globalAlpha=1-((f+j*.36)%1);ctx.beginPath();for(let k=0;k<9;k++){const a=k*Math.PI/4;const xx=802+Math.cos(a)*radius,yy=440+Math.sin(a)*radius*.65;k?ctx.lineTo(px(xx),px(yy)):ctx.moveTo(px(xx),px(yy))}ctx.stroke()}ctx.globalAlpha=1}
  ctx.restore();hud(ctx,{stage:key==='mini'?5:10,boss:true,bossAlpha:clamp((t-zoomOut)/200),active:t>=end,showBell:t>=end,playerHp:t>=end});if(t>=end)signal(ctx,'low_sweep',0);caption=t<zoomIn?'적에게 시선이 가까워집니다. HUD는 카메라와 분리합니다.':t<roarEnd?'가슴빛과 각진 잔향으로 괴성을 표현합니다. 음원은 제작 전입니다.':t<zoomOut?'줌아웃 · 원래 전투 구도로 돌아옵니다.':t<end?'정착 · 보스 HP가 실제값으로 나타납니다.':'조작 재개 · 온전한 첫 공격 예고를 시작합니다.';
 }return caption;
}
let raf=0,playing=false,startClock=0,startTime=0,ready=false;
function refreshWarning(){if(!ready)return;const val=+$('signalTime').value;$('signalReadout').textContent=val+'%';warningFrame($('warning'),$('direction').value,val/100,$('visualMode').value,$('nextSignal').checked,$('mono').checked)}
function getState(){return{key:$('sequenceType').value,reduced:$('reduced').checked,repeated:$('repeat').checked,time:+$('sequenceTime').value}}
function renderTime(){if(!ready)return;const s=getState();$('sequenceReadout').textContent=(s.time/1000).toFixed(2)+'s';$('sequenceCaption').textContent=sequenceFrame($('sequenceCanvas'),s.key,s.time,s.reduced,s.repeated);document.querySelectorAll('#timeline span').forEach(el=>el.classList.toggle('active',s.time>=+el.dataset.start&&s.time<+el.dataset.end))}
function stop(){playing=false;cancelAnimationFrame(raf);$('play').textContent='▶ 재생';$('play').setAttribute('aria-pressed','false')}
function refreshSequence(){stop();const s=getState(),total=duration(s.key,s.repeated);$('repeat').disabled=!P.sequences[s.key].repeatMs;$('sequenceTime').max=total;$('sequenceTime').value=0;let segments=P.sequences[s.key].segments;if(s.repeated&&P.sequences[s.key].repeatMs){const z=bossTimes(s.key,true);segments=[[0,z[0],'짧은 줌인'],[z[0],z[1],'괴성'],[z[1],z[2],'줌아웃'],[z[2],z[3],'정착']]}
 $('timeline').innerHTML=segments.map(([start,end,label])=>`<span data-start="${start}" data-end="${end}">${(start/1000).toFixed(2)}–${(end/1000).toFixed(2)}s<br>${label}</span>`).join('');
 const samples=s.key==='normal'?[200,450,850,1400]:s.key==='death'?[300,700,1100,1800]:[0,total*.3,total*.67,total];$('storyStrip').innerHTML='';samples.forEach((time,i)=>{const fig=document.createElement('figure'),canvas=document.createElement('canvas'),cap=document.createElement('figcaption');canvas.width=1280;canvas.height=720;canvas.setAttribute('aria-label',`${P.sequences[s.key].name} ${(time/1000).toFixed(2)}초`);fig.append(canvas,cap);$('storyStrip').append(fig);const label=sequenceFrame(canvas,s.key,time,s.reduced,s.repeated);cap.textContent=`${(time/1000).toFixed(2)}s · ${label}`});renderTime()}
async function init(){try{await Promise.all(Object.entries(paths).map(([key,src])=>new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>{images[key]=image;resolve()};image.onerror=()=>reject(new Error('이미지 로드 실패: '+src));image.src=src})));await document.fonts.ready;ready=true;scaleFrame($('scaleBefore'),1);scaleFrame($('scaleAfter'),1.5);$('boundsRows').innerHTML=P.telegraph.regions.map(r=>`<tr><td>${r.name}</td><td>${r.bounds.join(', ')}</td><td>${r.dodge}</td><td>${r.shape}</td></tr>`).join('');P.telegraph.regions.forEach(r=>{const fig=document.createElement('figure'),can=document.createElement('canvas'),caption=document.createElement('figcaption');can.width=1280;can.height=720;can.setAttribute('aria-label',r.name+' 예고');warningFrame(can,r.id,.8);caption.textContent=`${r.name} · 대응 ${r.dodge}`;fig.append(can,caption);$('directionStrip').append(fig)});$('reduced').checked=matchMedia('(prefers-reduced-motion: reduce)').matches;refreshWarning();refreshSequence();$('loadState').textContent='시안 로드 완료.';window.reviewReady=true}catch(error){$('loadState').textContent=error.message;$('loadState').className='error';console.error(error)}}
['direction','visualMode','nextSignal','mono','signalTime'].forEach(id=>$(id).addEventListener('input',refreshWarning));['sequenceType','reduced','repeat'].forEach(id=>$(id).addEventListener('change',refreshSequence));$('sequenceTime').addEventListener('input',()=>{stop();renderTime()});$('reset').addEventListener('click',()=>{stop();$('sequenceTime').value=0;renderTime()});$('play').addEventListener('click',()=>{if(!ready)return;if(playing){stop();return}playing=true;$('play').textContent='Ⅱ 멈춤';$('play').setAttribute('aria-pressed','true');const total=+$('sequenceTime').max;startTime=+$('sequenceTime').value;if(startTime>=total)startTime=0;startClock=performance.now();function frame(now){$('sequenceTime').value=Math.min(total,startTime+now-startClock);renderTime();if(+$('sequenceTime').value>=total){stop();return}raf=requestAnimationFrame(frame)}raf=requestAnimationFrame(frame)});document.addEventListener('visibilitychange',()=>{if(document.hidden)stop()});init();
